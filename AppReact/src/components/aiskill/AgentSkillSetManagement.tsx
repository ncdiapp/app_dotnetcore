import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { agentSkillSetSvc, AppAgentSkillSetDto } from '../../webapi/agentSkillSetSvc';
import AgentToolRegisterTab from './AgentToolRegisterTab';
import AgentMcpServerTab from './AgentMcpServerTab';
import AgentUiChatHost from './AgentUiChatHost';
import { AGENT_UI_OPTIONS, EmAppAgentUi, resolveAgentUi } from './agentUiTypes';

type Tab = 'skills' | 'tools' | 'mcp';

const SPLIT_LEFT_DEFAULT_PX = 400;
const SPLIT_LEFT_MIN_PX = 300;
const SPLIT_RIGHT_MIN_PX = 280;

const CAP_FLAGS = [
    { label: 'StreamTokens',    value: 1 },
    { label: 'MultiTurn',       value: 2 },
    { label: 'PlanGate',        value: 4 },
    { label: 'SchemaGate',      value: 8 },
    { label: 'InjectMemory',    value: 16 },
    { label: 'InjectSchema',    value: 32 },
    { label: 'ExternalBackend', value: 64 },
];

const emptySkillSet = (): AppAgentSkillSetDto => ({
    SkillKey: '', DisplayName: '', Description: '', SystemPrompt: '', CapabilityFlags: 3,
    IsActive: true, SortOrder: 0, Version: 1,
    MaxHistoryTokens: 80000, SummarizeThreshold: 60000, MaxToolResultChars: 4000, RecentWindowSize: 10, MaxIterations: 40,
    ExecutionMode: 'Interactive',
    AgentUi: EmAppAgentUi.GenericChat,
});

const AgentSkillSetManagement: React.FC = () => {
    const { theme, t } = useTheme();
    const dispatch = useDispatch();
    const [activeTab, setActiveTab] = useState<Tab>('skills');
    const [skillsCV] = useState(() => new CollectionView<AppAgentSkillSetDto>([]));
    const [selected, setSelected] = useState<AppAgentSkillSetDto | null>(null);
    const [editItem, setEditItem] = useState<AppAgentSkillSetDto>(emptySkillSet());
    const [isEditing, setIsEditing] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDelete, setConfirmDelete] = useState(false);
    const [testSkillKey, setTestSkillKey] = useState<string | null>(null);
    const [generalExpanded, setGeneralExpanded] = useState(true);
    const [leftPanelWidthPx, setLeftPanelWidthPx] = useState(SPLIT_LEFT_DEFAULT_PX);
    const splitContainerRef = useRef<HTMLDivElement | null>(null);
    const splitDragRef = useRef(false);
    const flexGridRef = useRef<any>(null);
    const suppressGridSelectionSyncRef = useRef(false);

    const selectGridRowBySkillKey = useCallback((skillKey: string | null | undefined) => {
        const flex = flexGridRef.current?.control ?? flexGridRef.current;
        if (!flex || !skillKey) return;
        const items = (skillsCV.items as AppAgentSkillSetDto[]) ?? [];
        const idx = items.findIndex(x => x.SkillKey === skillKey);
        if (idx < 0) return;
        flex.select(new wjGrid.CellRange(idx, 0, idx, Math.max(0, (flex.columns?.length ?? 1) - 1)));
    }, [skillsCV]);

    const applyListAndKeepSelection = useCallback((list: AppAgentSkillSetDto[], skillKeyToKeep: string | null | undefined, syncEditFromServer: boolean) => {
        suppressGridSelectionSyncRef.current = true;
        skillsCV.sourceCollection = list;

        if (!skillKeyToKeep) {
            window.setTimeout(() => { suppressGridSelectionSyncRef.current = false; }, 0);
            return null;
        }

        const fresh = list.find(x => x.SkillKey === skillKeyToKeep) ?? null;
        if (fresh) {
            setSelected(fresh);
            if (syncEditFromServer) {
                setEditItem({ ...fresh, AgentUi: resolveAgentUi(fresh.AgentUi) });
                setIsEditing(true);
                setIsDirty(false);
            }
            window.setTimeout(() => {
                try {
                    selectGridRowBySkillKey(skillKeyToKeep);
                } finally {
                    suppressGridSelectionSyncRef.current = false;
                }
            }, 0);
        } else {
            setSelected(null);
            if (syncEditFromServer) {
                setEditItem(emptySkillSet());
                setIsEditing(false);
                setIsDirty(false);
            }
            window.setTimeout(() => { suppressGridSelectionSyncRef.current = false; }, 0);
        }
        return fresh;
    }, [skillsCV, selectGridRowBySkillKey]);

    useEffect(() => {
        const onMove = (e: MouseEvent) => {
            if (!splitDragRef.current || !splitContainerRef.current) return;
            const rect = splitContainerRef.current.getBoundingClientRect();
            const w = e.clientX - rect.left;
            const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, rect.width - SPLIT_RIGHT_MIN_PX);
            const clamped = Math.min(Math.max(w, SPLIT_LEFT_MIN_PX), maxLeft);
            setLeftPanelWidthPx(clamped);
        };
        const onUp = () => {
            if (!splitDragRef.current) return;
            splitDragRef.current = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
        return () => {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
        };
    }, []);

    const onSplitDividerMouseDown = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        splitDragRef.current = true;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    }, []);

    const load = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllSkillSets();
            const list = res.Object ?? [];
            const keepKey = (selected?.SkillKey ?? editItem.SkillKey) || null;
            applyListAndKeepSelection(list, keepKey, !!keepKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    useEffect(() => { load(); }, []);

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }) => {
        if (suppressGridSelectionSyncRef.current) return;
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item);
        setEditItem({ ...item, AgentUi: resolveAgentUi(item.AgentUi) });
        setIsEditing(true);
        setIsDirty(false);
        setTestSkillKey(null);
    };

    const update = (field: keyof AppAgentSkillSetDto, value: unknown) => {
        setEditItem(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

    const toggleCap = (flag: number) => {
        setEditItem(prev => ({ ...prev, CapabilityFlags: prev.CapabilityFlags ^ flag }));
        setIsDirty(true);
    };

    const handleSave = async () => {
        if (!editItem.SkillKey.trim()) { setError('Agent Code is required.'); return; }
        const key = editItem.SkillKey.trim();
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertSkillSet(editItem);
            setIsDirty(false);
            const res = await agentSkillSetSvc.GetAllSkillSets();
            const list = res.Object ?? [];
            const fresh = applyListAndKeepSelection(list, key, true);
            if (fresh) {
                setSelected(fresh);
                setEditItem({ ...fresh, AgentUi: resolveAgentUi(fresh.AgentUi) });
            } else {
                setSelected({ ...editItem, SkillKey: key });
            }
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleDelete = async () => {
        if (!selected) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteSkillSet(selected.SkillKey);
            setSelected(null); setEditItem(emptySkillSet()); setIsEditing(false); setIsDirty(false);
            setConfirmDelete(false);
            const res = await agentSkillSetSvc.GetAllSkillSets();
            skillsCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleRefresh = async () => {
        setError(null);
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllSkillSets();
            const list = res.Object ?? [];
            const keepKey = selected?.SkillKey || editItem.SkillKey || null;
            if (keepKey) {
                applyListAndKeepSelection(list, keepKey, true);
            } else {
                skillsCV.sourceCollection = list;
                if (isEditing) {
                    setEditItem(emptySkillSet());
                    setIsDirty(false);
                }
            }
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const beginNew = () => {
        setSelected(null);
        setEditItem(emptySkillSet());
        setIsEditing(true);
        setIsDirty(false);
        setTestSkillKey(null);
    };

    const tabCls = (tab: Tab) =>
        `px-3 py-1.5 text-xs rounded-[4px] cursor-pointer mr-1 ${theme.button_default}${activeTab === tab ? ' border-b-2 font-semibold' : ''}`;
    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-36 text-xs ${theme.label} mr-2 shrink-0`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const iconBtn = 'w-8 h-6 rounded-[4px] text-xs flex items-center justify-center';
    const borderCls = t('border_mainContentSection');
    const sectionTitle = `text-xs font-semibold pb-1 mb-2 border-b ${theme.title} ${borderCls}`;
    const promptChars = (editItem.SystemPrompt || '').length;

    return (
        <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
            <div className={`flex items-center gap-1 px-3 py-2 mb-1 ${theme.mainContentSection}`}>
                <span className={`text-md font-semibold mr-3 ${theme.title}`}>Agent Management</span>
                <button type="button" className={tabCls('skills')} onClick={() => setActiveTab('skills')}>Agent Setting</button>
                <button type="button" className={tabCls('tools')} onClick={() => setActiveTab('tools')}>Tools</button>
                <button type="button" className={tabCls('mcp')} onClick={() => setActiveTab('mcp')}>MCP Servers</button>
            </div>
            {error && (
                <div className={`px-3 py-1 text-xs mx-2 mb-1 rounded border ${theme.mainContentSection} ${theme.label} ${borderCls}`}>
                    {error}
                    <button type="button" className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                </div>
            )}
            <div className="w-full h-1 flex-auto overflow-hidden">
                <div
                    ref={splitContainerRef}
                    className={`w-full h-full flex gap-0 px-2 pb-2 overflow-hidden${activeTab === 'skills' ? '' : ' hidden'}`}
                >
                    <div
                        className={`shrink-0 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}
                        style={{ width: leftPanelWidthPx, minWidth: SPLIT_LEFT_MIN_PX }}
                    >
                        <div className={`flex items-center px-2 py-1 gap-1 border-b ${borderCls}`}>
                            <button type="button" className={btn} onClick={beginNew}>
                                <i className="fa-solid fa-plus mr-1" />New
                            </button>
                            {selected && (
                                <button type="button" className={btn} onClick={() => setConfirmDelete(true)}>
                                    <i className="fa-solid fa-trash mr-1" />Delete
                                </button>
                            )}
                            {selected && (
                                <button type="button" className={btn} onClick={() => setTestSkillKey(selected.SkillKey)}>
                                    <i className="fa-solid fa-play mr-1" />Run
                                </button>
                            )}
                        </div>
                        <div className="w-full h-1 flex-auto overflow-hidden">
                            <FlexGrid
                                ref={flexGridRef}
                                className="w-full h-full"
                                itemsSource={skillsCV}
                                isReadOnly
                                headersVisibility="Column"
                                selectionChanged={onGridSelectionChanged}
                            >
                                <FlexGridColumn header="Agent Code" binding="SkillKey" width="*" minWidth={120} />
                                <FlexGridColumn header="Active" binding="IsActive" width={55} />
                            </FlexGrid>
                        </div>
                    </div>

                    <div
                        role="separator"
                        aria-orientation="vertical"
                        aria-label="Resize agent panels"
                        title="Drag to resize panels"
                        tabIndex={0}
                        onMouseDown={onSplitDividerMouseDown}
                        onKeyDown={(e) => {
                            const el = splitContainerRef.current;
                            if (!el) return;
                            const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, el.getBoundingClientRect().width - SPLIT_RIGHT_MIN_PX);
                            if (e.key === 'ArrowLeft') {
                                e.preventDefault();
                                setLeftPanelWidthPx((w) => Math.max(SPLIT_LEFT_MIN_PX, w - 16));
                            } else if (e.key === 'ArrowRight') {
                                e.preventDefault();
                                setLeftPanelWidthPx((w) => Math.min(maxLeft, w + 16));
                            }
                        }}
                        className={`shrink-0 w-1.5 mx-1 cursor-col-resize border-x self-stretch min-h-0 ${theme.inputBox} hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset`}
                    />

                    <div className={`min-w-0 w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                        {testSkillKey ? (
                            <div className="w-full h-full flex flex-col overflow-hidden">
                                <div className={`flex items-center px-3 py-1 border-b ${borderCls} ${theme.mainContentSection}`}>
                                    <i className="fa-solid fa-play mr-2" />
                                    <span className={`text-xs font-semibold ${theme.title} w-1 flex-auto`}>
                                        Preview: {testSkillKey}
                                        <span className={`ml-2 font-normal ${theme.label}`}>
                                            ({AGENT_UI_OPTIONS.find(o => o.value === resolveAgentUi(editItem.AgentUi))?.label
                                              ?? AGENT_UI_OPTIONS.find(o => o.value === resolveAgentUi(selected?.AgentUi))?.label
                                              ?? 'Generic Chat'})
                                        </span>
                                    </span>
                                    <button type="button" className={btn} onClick={() => setTestSkillKey(null)}>
                                        <i className="fa-solid fa-xmark" />
                                    </button>
                                </div>
                                <div className="w-full h-1 flex-auto overflow-hidden">
                                    <AgentUiChatHost
                                        skillKey={testSkillKey}
                                        agentUi={editItem.SkillKey === testSkillKey ? editItem.AgentUi : selected?.AgentUi}
                                    />
                                </div>
                            </div>
                        ) : isEditing ? (
                            <div className="h-full flex flex-col overflow-hidden">
                                <div className={`flex items-center justify-between px-3 py-2 border-b shrink-0 ${borderCls}`}>
                                    <div className={`text-sm font-semibold truncate pr-3 ${theme.title}`}>
                                        {editItem.SkillKey || 'New Agent'}
                                    </div>
                                    <div className="flex items-center gap-2 shrink-0">
                                        {isDirty && <span className={`text-xs ${theme.label}`}>Unsaved changes</span>}
                                        <button
                                            type="button"
                                            className={`${iconBtn} bg-blue-400 text-white hover:bg-blue-500`}
                                            onClick={handleRefresh}
                                            title="Refresh"
                                        >
                                            <i className="fa-solid fa-rotate" aria-hidden />
                                        </button>
                                        <button
                                            type="button"
                                            className={`${iconBtn} bg-orange-400 text-white hover:bg-orange-500 disabled:opacity-60 disabled:cursor-not-allowed`}
                                            onClick={handleSave}
                                            disabled={!isDirty}
                                            title="Save"
                                        >
                                            <i className="fa-solid fa-floppy-disk" aria-hidden />
                                        </button>
                                    </div>
                                </div>

                                <div
                                    className={`w-full h-1 flex-auto min-h-0 flex flex-col ${
                                        generalExpanded ? 'overflow-auto' : 'overflow-hidden'
                                    }`}
                                >
                                    {/* General: bordered panel so scope is clear */}
                                    <div className={`shrink-0 mx-3 mt-3 mb-2 rounded-[4px] border overflow-hidden ${borderCls}`}>
                                        <button
                                            type="button"
                                            className={`flex items-center gap-2 px-3 py-2 w-full text-left hover:opacity-90 ${t('bg_input_readonly')}`}
                                            onClick={() => setGeneralExpanded(v => !v)}
                                            aria-expanded={generalExpanded}
                                        >
                                            <i className={`fa-solid fa-chevron-${generalExpanded ? 'down' : 'right'} text-[10px] w-3 ${theme.label}`} aria-hidden />
                                            <span className={`text-xs font-semibold ${theme.title}`}>General</span>
                                            <span className={`text-[10px] ml-auto ${theme.label}`}>
                                                {generalExpanded ? 'Collapse' : 'Expand'}
                                            </span>
                                        </button>

                                        {generalExpanded && (
                                            <div className={`border-t px-4 py-3 pl-6 ${borderCls} ${theme.mainContentSection}`}>
                                                <div className="mb-4">
                                                    <div className="grid grid-cols-1 xl:grid-cols-2 gap-x-6 gap-y-1">
                                                        <div className="flex items-center py-1">
                                                            <label className={lbl}>Agent Code *</label>
                                                            <input className={inp} value={editItem.SkillKey} onChange={e => update('SkillKey', e.target.value)} autoComplete="off" />
                                                        </div>
                                                    <div className="flex items-center py-1">
                                                        <label className={lbl}>Display Name</label>
                                                        <input className={inp} value={editItem.DisplayName} onChange={e => update('DisplayName', e.target.value)} autoComplete="off" />
                                                    </div>
                                                    <div className="flex items-center py-1 xl:col-span-2">
                                                        <label className={lbl}>Agent UI</label>
                                                        <select
                                                            className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                                                            value={resolveAgentUi(editItem.AgentUi)}
                                                            onChange={e => update('AgentUi', parseInt(e.target.value, 10) || EmAppAgentUi.GenericChat)}
                                                        >
                                                            {AGENT_UI_OPTIONS.map(o => (
                                                                <option key={o.value} value={o.value}>{o.label}</option>
                                                            ))}
                                                        </select>
                                                    </div>
                                                    </div>
                                                    <div className="flex items-center py-1">
                                                        <label className={`text-xs ${theme.label}`}>Description</label>
                                                        <div className="w-1 flex-auto" />
                                                        <label className={`flex items-center gap-1.5 text-xs ${theme.label} cursor-pointer shrink-0`}>
                                                            <input type="checkbox" checked={editItem.IsActive} onChange={e => update('IsActive', e.target.checked)} />
                                                            Active
                                                        </label>
                                                    </div>
                                                    <textarea
                                                        className={`w-full px-2 py-1 text-xs border resize-none ${theme.inputBox} focus:outline-none`}
                                                        style={{ height: 80 }}
                                                        value={editItem.Description}
                                                        onChange={e => update('Description', e.target.value)}
                                                    />
                                                </div>

                                                <div className="mb-4">
                                                    <div className={sectionTitle}>Behavior</div>
                                                    <div className="flex items-start py-1">
                                                        <label className={lbl}>Capabilities</label>
                                                        <div className="flex flex-wrap gap-x-4 gap-y-1">
                                                            {CAP_FLAGS.map(f => (
                                                                <label key={f.value} className={`flex items-center gap-1 text-xs ${theme.label} cursor-pointer`}>
                                                                    <input type="checkbox" checked={(editItem.CapabilityFlags & f.value) !== 0} onChange={() => toggleCap(f.value)} />
                                                                    {f.label}
                                                                </label>
                                                            ))}
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center py-1">
                                                        <label className={lbl}>Execution Mode</label>
                                                        <div className="flex gap-4">
                                                            {(['Interactive', 'Deterministic'] as const).map(mode => (
                                                                <label key={mode} className={`flex items-center gap-1.5 text-xs ${theme.label} cursor-pointer`}>
                                                                    <input
                                                                        type="radio"
                                                                        name="executionMode"
                                                                        value={mode}
                                                                        checked={(editItem.ExecutionMode || 'Interactive') === mode}
                                                                        onChange={() => update('ExecutionMode', mode)}
                                                                    />
                                                                    {mode}
                                                                </label>
                                                            ))}
                                                        </div>
                                                    </div>
                                                </div>

                                                <div>
                                                    <div className={sectionTitle}>Limits</div>
                                                    <div className="grid grid-cols-1 xl:grid-cols-2 gap-x-6 gap-y-1">
                                                        {([['MaxHistoryTokens', 'Max History Tokens'], ['SummarizeThreshold', 'Summarize Threshold'], ['MaxToolResultChars', 'Max Tool Result Chars'], ['RecentWindowSize', 'Recent Window Size'], ['MaxIterations', 'Max Iterations']] as const).map(([field, label]) => (
                                                            <div key={field} className="flex items-center py-1">
                                                                <label className={lbl}>{label}</label>
                                                                <input
                                                                    className={`w-28 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                                                    type="number"
                                                                    value={editItem[field]}
                                                                    onChange={e => update(field, parseInt(e.target.value) || 0)}
                                                                />
                                                            </div>
                                                        ))}
                                                    </div>
                                                </div>
                                            </div>
                                        )}
                                    </div>

                                    <div
                                        className={`w-full flex flex-col px-5 py-3 ${
                                            generalExpanded
                                                ? 'shrink-0'
                                                : 'h-1 flex-auto min-h-0 overflow-hidden'
                                        }`}
                                        style={generalExpanded ? { minHeight: 600 } : undefined}
                                    >
                                        <div className={`flex items-center pb-1 mb-2 border-b shrink-0 ${borderCls}`}>
                                            <span className={`text-xs font-semibold ${theme.title}`}>System Prompt</span>
                                            <span className={`text-xs ml-auto ${theme.label}`}>{promptChars} chars</span>
                                        </div>
                                        <textarea
                                            className={`w-full px-2 py-2 text-xs border font-mono resize-none ${theme.inputBox} focus:outline-none ${
                                                generalExpanded ? '' : 'h-1 flex-auto min-h-0'
                                            }`}
                                            style={generalExpanded ? { minHeight: 560 } : undefined}
                                            value={editItem.SystemPrompt}
                                            onChange={e => update('SystemPrompt', e.target.value)}
                                            spellCheck={false}
                                        />
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <div className="h-full flex items-center justify-center">
                                <span className={`text-sm ${theme.label}`}>Select a skill set or click + New</span>
                            </div>
                        )}
                    </div>
                </div>
                {activeTab === 'tools' && <AgentToolRegisterTab selectedSkillKey={selected?.SkillKey ?? null} theme={theme} />}
                {activeTab === 'mcp'   && <AgentMcpServerTab theme={theme} />}
            </div>
            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>Delete skill set "{selected?.SkillKey}"?</div>
                        <div className="flex gap-2">
                            <button type="button" className={btn} onClick={handleDelete}>Delete</button>
                            <button type="button" className={btn} onClick={() => setConfirmDelete(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AgentSkillSetManagement;
