import React, { useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { RootState } from '../../redux/store';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import {
    agentSkillSetSvc,
    AppAgentLibraryToolDto,
} from '../../webapi/agentSkillSetSvc';

export const UI_RENDER_LIBRARY_KEY = 'platform-ui-render';
export const UI_RENDER_INTERACTIVE_TOOL = 'render_interactive';
export const UI_RENDER_NONE_INTERACTIVE_TOOL = 'render_none_interactive';

const LAYOUT_KEYS = ['table', 'cards', 'kpi', 'chart', 'dashboard', 'page', 'form'] as const;
type LayoutKey = (typeof LAYOUT_KEYS)[number];

type ToolMode = 'interactive' | 'none_interactive';

function isUiRenderTool(toolName: string | undefined | null): boolean {
    const n = (toolName || '').toLowerCase();
    return n === UI_RENDER_INTERACTIVE_TOOL || n === UI_RENDER_NONE_INTERACTIVE_TOOL;
}

export function isUiTemplateEditable(libraryKey: string | null | undefined, toolName: string | null | undefined): boolean {
    return (libraryKey || '') === UI_RENDER_LIBRARY_KEY && isUiRenderTool(toolName);
}

function parseMap(raw: string | null | undefined): Record<string, string> {
    if (!raw || !raw.trim()) return {};
    try {
        const obj = JSON.parse(raw) as Record<string, unknown>;
        const out: Record<string, string> = {};
        for (const [k, v] of Object.entries(obj || {})) {
            if (typeof v === 'string') out[k] = v;
            else if (v != null) out[k] = String(v);
        }
        return out;
    } catch {
        return {};
    }
}

function serializeMap(map: Record<string, string>): string {
    return JSON.stringify(map);
}

interface Props {
    libraryKey: string;
    /** Tool that opened the editor (sets initial Interactive / none-interactive mode). */
    initialToolName: string;
    pathPrefix?: string;
    onClose: () => void;
    onSaved?: () => void;
}

const UiTemplateContentEditor: React.FC<Props> = ({
    libraryKey,
    initialToolName,
    pathPrefix,
    onClose,
    onSaved,
}) => {
    const { theme } = useTheme();
    const dispatch = useDispatch();
    const currentThemeId = useSelector((state: RootState) => state.theme.currentThemeId);
    const monacoTheme = currentThemeId === 'dark' ? 'vs-dark' : 'vs';

    const [tools, setTools] = useState<AppAgentLibraryToolDto[]>([]);
    const [mode, setMode] = useState<ToolMode>(
        (initialToolName || '').toLowerCase() === UI_RENDER_NONE_INTERACTIVE_TOOL
            ? 'none_interactive'
            : 'interactive',
    );
    const [layoutKey, setLayoutKey] = useState<LayoutKey>('table');
    const [maps, setMaps] = useState<{ interactive: Record<string, string>; none: Record<string, string> }>({
        interactive: {},
        none: {},
    });
    const [dirty, setDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [status, setStatus] = useState<string | null>(null);

    const toolName = mode === 'interactive' ? UI_RENDER_INTERACTIVE_TOOL : UI_RENDER_NONE_INTERACTIVE_TOOL;
    const currentTool = useMemo(
        () => tools.find(t => (t.ToolName || '').toLowerCase() === toolName) ?? null,
        [tools, toolName],
    );
    const currentMap = mode === 'interactive' ? maps.interactive : maps.none;
    const editorValue = currentMap[layoutKey] ?? '';

    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const btnSm = `px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`;

    const load = async () => {
        dispatch(setIsBusy());
        setError(null);
        try {
            const res = await agentSkillSetSvc.GetLibraryTools(libraryKey);
            const rows = res.Object ?? [];
            setTools(rows);
            const interactive = rows.find(t => (t.ToolName || '').toLowerCase() === UI_RENDER_INTERACTIVE_TOOL);
            const none = rows.find(t => (t.ToolName || '').toLowerCase() === UI_RENDER_NONE_INTERACTIVE_TOOL);
            setMaps({
                interactive: parseMap(interactive?.TemplateContent),
                none: parseMap(none?.TemplateContent),
            });
            setDirty(false);
            setStatus(null);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    useEffect(() => { void load(); }, [libraryKey]);

    const setBody = (next: string) => {
        setMaps(prev => {
            const key = mode === 'interactive' ? 'interactive' : 'none';
            return { ...prev, [key]: { ...prev[key], [layoutKey]: next } };
        });
        setDirty(true);
        setStatus(null);
    };

    const handleSave = async () => {
        if (!currentTool) {
            setError(`Tool ${toolName} not found in library ${libraryKey}.`);
            return;
        }
        dispatch(setIsBusy());
        setError(null);
        try {
            const payload: AppAgentLibraryToolDto = {
                ...currentTool,
                LibraryKey: libraryKey,
                TemplateContent: serializeMap(currentMap),
            };
            const res = await agentSkillSetSvc.UpsertLibraryTool(payload);
            if (!res.IsSuccessful) {
                const msg = res.ValidationResult?.Items?.map(i => i.Message).filter(Boolean).join('; ');
                throw new Error(msg || 'Save failed.');
            }
            setDirty(false);
            setStatus(`Saved ${toolName} TemplateContent.`);
            await load();
            onSaved?.();
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleResetKey = async () => {
        dispatch(setIsBusy());
        setError(null);
        try {
            const res = await agentSkillSetSvc.GetUiTemplateDefault(layoutKey, mode === 'interactive');
            const body = res.Object ?? '';
            setBody(body);
            setStatus(`Reset ${layoutKey} to platform default (unsaved).`);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleClose = () => {
        if (dirty && !window.confirm('Discard unsaved TemplateContent changes?')) return;
        onClose();
    };

    const switchMode = (next: ToolMode) => {
        if (next === mode) return;
        if (dirty && !window.confirm('Switch tool mode and discard unsaved changes for the current tool?')) return;
        setMode(next);
        setDirty(false);
        setStatus(null);
        // Reload maps from last loaded tools (discard local edits for previous mode).
        const interactive = tools.find(t => (t.ToolName || '').toLowerCase() === UI_RENDER_INTERACTIVE_TOOL);
        const none = tools.find(t => (t.ToolName || '').toLowerCase() === UI_RENDER_NONE_INTERACTIVE_TOOL);
        setMaps({
            interactive: parseMap(interactive?.TemplateContent),
            none: parseMap(none?.TemplateContent),
        });
    };

    const switchLayout = (key: LayoutKey) => {
        if (key === layoutKey) return;
        setLayoutKey(key);
        setStatus(null);
    };

    return (
        <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-[60]" onClick={handleClose}>
            <div
                className={`rounded-lg shadow-xl ${theme.mainContentSection} flex flex-col`}
                style={{ width: '92vw', height: '90vh', minWidth: 640, minHeight: 420, maxWidth: '98vw', maxHeight: '96vh' }}
                onClick={e => e.stopPropagation()}
            >
                <div className={`px-4 py-3 text-sm font-semibold border-b border-gray-200 ${theme.title} flex items-center justify-between shrink-0`}>
                    <span>
                        <i className="fa-solid fa-code mr-2 opacity-70" />
                        Edit TemplateContent — {toolName}
                    </span>
                    <button type="button" className="opacity-50 hover:opacity-100 text-lg leading-none" onClick={handleClose}>×</button>
                </div>

                {(pathPrefix || libraryKey) && (
                    <div className={`px-4 py-1 text-xs border-b border-gray-200 ${theme.label} shrink-0`}>
                        <i className="fa-solid fa-sitemap mr-2 opacity-60" />
                        {pathPrefix ? `${pathPrefix} › ` : ''}{libraryKey} › {toolName} › TemplateContent
                    </div>
                )}

                <div className="flex items-center gap-2 px-4 py-2 border-b border-gray-200 shrink-0 flex-wrap">
                    <span className={`text-xs ${theme.label} mr-1`}>Mode</span>
                    <button type="button" className={btnSm} onClick={() => switchMode('interactive')}
                        style={mode === 'interactive' ? { fontWeight: 700, outline: '1px solid currentColor' } : undefined}>
                        Interactive
                    </button>
                    <button type="button" className={btnSm} onClick={() => switchMode('none_interactive')}
                        style={mode === 'none_interactive' ? { fontWeight: 700, outline: '1px solid currentColor' } : undefined}>
                        None-interactive
                    </button>
                    <span className={`text-xs ${theme.label} mx-2`}>|</span>
                    <button type="button" className={btn} onClick={() => void handleSave()} disabled={!currentTool}>
                        <i className="fa-solid fa-floppy-disk mr-1" />Save
                    </button>
                    <button type="button" className={btn} onClick={() => void handleResetKey()}>
                        <i className="fa-solid fa-rotate-left mr-1" />Reset key
                    </button>
                    <button type="button" className={btn} onClick={() => void load()}>
                        <i className="fa-solid fa-rotate mr-1" />Reload
                    </button>
                    {dirty && <span className="text-xs text-orange-500">Unsaved changes</span>}
                    {status && <span className={`text-xs ${theme.label}`}>{status}</span>}
                </div>

                {error && (
                    <div className="mx-4 mt-2 px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 rounded shrink-0">
                        {error}
                        <button type="button" className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                    </div>
                )}

                {!currentTool && (
                    <div className={`px-4 py-2 text-xs text-orange-600 shrink-0`}>
                        Tool <span className="font-mono">{toolName}</span> is missing from this library. Seed/migrate platform-ui-render first.
                    </div>
                )}

                <div className="w-full h-1 flex-auto flex overflow-hidden min-h-0">
                    <div className={`w-40 shrink-0 border-r border-gray-200 overflow-y-auto flex flex-col py-1`}>
                        <div className={`px-3 py-1 text-xs font-semibold ${theme.title}`}>Layout keys</div>
                        {LAYOUT_KEYS.map(key => {
                            const has = Object.prototype.hasOwnProperty.call(currentMap, key) && (currentMap[key] ?? '').length > 0;
                            const active = key === layoutKey;
                            return (
                                <button
                                    key={key}
                                    type="button"
                                    className={`text-left px-3 py-1.5 text-xs font-mono ${active ? theme.button_default : theme.label} hover:opacity-80`}
                                    style={active ? { fontWeight: 700 } : undefined}
                                    onClick={() => switchLayout(key)}
                                >
                                    {key}{has ? '' : ' ·'}
                                </button>
                            );
                        })}
                        <div className={`px-3 pt-3 text-[10px] leading-snug ${theme.label}`}>
                            HTML / Scriban. Marker <span className="font-mono">{'{{code_default}}'}</span> uses platform built-in at runtime.
                        </div>
                    </div>
                    <div className="w-1 flex-auto min-w-0 min-h-0 flex flex-col">
                        <div className={`px-3 py-1 text-xs border-b border-gray-200 ${theme.label} shrink-0`}>
                            Editing key <span className="font-mono font-semibold">{layoutKey}</span>
                            {(editorValue || '').trim() === '{{code_default}}' && (
                                <span className="ml-2 opacity-70">(code default marker)</span>
                            )}
                        </div>
                        <div className="w-full h-1 flex-auto min-h-0">
                            <JsonCodeEditor
                                language="html"
                                value={editorValue}
                                onChange={setBody}
                                monacoTheme={monacoTheme}
                                debounceMs={120}
                                className="w-full h-full"
                            />
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default UiTemplateContentEditor;
