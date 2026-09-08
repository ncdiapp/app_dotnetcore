import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import {
    agentSkillSetSvc,
    AppAgentSkillSetDto, AppAgentToolDomainDto, AppAgentToolLibraryDto, AppAgentPromptHistoryDto,
    GenerateAgentResult, LibraryToolPreviewDto,
} from '../../webapi/agentSkillSetSvc';
import AgentToolRegisterTab from './AgentToolRegisterTab';
import AgentMcpServerTab from './AgentMcpServerTab';
import AgentUiChatHost from './AgentUiChatHost';
import { AGENT_UI_OPTIONS, EmAppAgentUi, resolveAgentUi } from './agentUiTypes';
import AgentLibraryTab from './AgentLibraryTab';

type Tab = 'skills' | 'mcp' | 'libraries';

const SPLIT_LEFT_DEFAULT_PX = 400;
const SPLIT_LEFT_MIN_PX = 300;
const SPLIT_RIGHT_MIN_PX = 280;


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
    const [showToolsModal, setShowToolsModal] = useState(false);

    // Templates
    const [templates, setTemplates] = useState<AppAgentSkillSetDto[]>([]);
    const [showTemplateMenu, setShowTemplateMenu] = useState(false);
    const [showSaveAsTemplate, setShowSaveAsTemplate] = useState(false);
    const [tmplName, setTmplName] = useState('');
    const [tmplKey,  setTmplKey]  = useState('');

    // Prompt history
    const [promptHistory, setPromptHistory] = useState<AppAgentPromptHistoryDto[]>([]);
    const [showHistory, setShowHistory] = useState(false);

    // AI Generate
    const [showAiGenerate, setShowAiGenerate]         = useState(false);
    const [aiDescription, setAiDescription]           = useState('');
    const [aiGenerating, setAiGenerating]             = useState(false);
    const [aiResult, setAiResult]                     = useState<GenerateAgentResult | null>(null);
    const [aiAcceptedLibs, setAiAcceptedLibs]         = useState<Set<string>>(new Set());
    const [aiAcceptedBuiltIns, setAiAcceptedBuiltIns] = useState<Set<string>>(new Set());
    const [allBuiltInTools, setAllBuiltInTools]       = useState<LibraryToolPreviewDto[]>([]);

    // Library subscription state
    const [allDomains, setAllDomains] = useState<AppAgentToolDomainDto[]>([]);
    const [allLibraries, setAllLibraries] = useState<AppAgentToolLibraryDto[]>([]);
    const [subscribedKeys, setSubscribedKeys] = useState<Set<string>>(new Set());
    const [libSearch, setLibSearch] = useState('');
    const [subsChanged, setSubsChanged] = useState(false);

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

    const loadHistory = async (skillKey: string) => {
        setPromptHistory([]);
        setShowHistory(false);
        try {
            const res = await agentSkillSetSvc.GetPromptHistory(skillKey);
            setPromptHistory(res.Object ?? []);
        } catch { /* non-critical */ }
    };

    const loadSubscriptions = async (skillKey: string) => {
        try {
            const subRes = await agentSkillSetSvc.GetSubscriptions(skillKey);
            setSubscribedKeys(new Set((subRes.Object ?? []).map(s => s.LibraryKey)));
            setSubsChanged(false);
        } catch { /* non-critical */ }
    };

    useEffect(() => {
        load();
        agentSkillSetSvc.GetAllLibraries().then(r => setAllLibraries(r.Object ?? [])).catch(() => {});
        agentSkillSetSvc.GetAllDomains().then(r => setAllDomains(r.Object ?? [])).catch(() => {});
        agentSkillSetSvc.GetTemplates().then(r => setTemplates(r.Object ?? [])).catch(() => {});
        agentSkillSetSvc.GetAvailableBuiltInTools().then(r => setAllBuiltInTools(r.Object ?? [])).catch(() => {});
    }, []);

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
        setSubsChanged(false);
        setShowHistory(false);
        loadSubscriptions(item.SkillKey);
        loadHistory(item.SkillKey);
    };

    const update = (field: keyof AppAgentSkillSetDto, value: unknown) => {
        setEditItem(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

const handleSave = async () => {
        if (!editItem.SkillKey.trim()) { setError('Agent Code is required.'); return; }
        const key = editItem.SkillKey.trim();
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertSkillSet(editItem);
            if (subsChanged) {
                await agentSkillSetSvc.SetSubscriptions(editItem.SkillKey, Array.from(subscribedKeys));
                setSubsChanged(false);
            }
            setIsDirty(false);
            const res = await agentSkillSetSvc.GetAllSkillSets();
            const list = res.Object ?? [];
            const fresh = applyListAndKeepSelection(list, key, true);
            if (fresh) {
                setSelected(fresh);
                setEditItem({ ...fresh, AgentUi: resolveAgentUi(fresh.AgentUi) });
                loadHistory(key);
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
                loadHistory(keepKey);
                loadSubscriptions(keepKey);
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
        setShowTemplateMenu(false);
        setSubscribedKeys(new Set());
        setSubsChanged(false);
        setPromptHistory([]);
        setShowHistory(false);
    };

    const openSaveAsTemplate = () => {
        const slug = editItem.DisplayName.toLowerCase().replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '');
        setTmplName(editItem.DisplayName);
        setTmplKey(slug);
        setShowSaveAsTemplate(true);
    };

    const handleSaveAsTemplate = async () => {
        const key = tmplKey.trim();
        if (!key) { setError('Template key is required.'); return; }
        const fullKey = key.startsWith('tmpl-') ? key : `tmpl-${key}`;
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertSkillSet({
                ...editItem,
                SkillKey:    fullKey,
                DisplayName: tmplName.trim() || editItem.DisplayName,
                IsActive:    false,
            });
            const res = await agentSkillSetSvc.GetTemplates();
            setTemplates(res.Object ?? []);
            setShowSaveAsTemplate(false);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleAiGenerate = async () => {
        if (!aiDescription.trim()) return;
        setAiGenerating(true);
        try {
            const res = await agentSkillSetSvc.GenerateAgentDesign(aiDescription.trim());
            if (res.IsSuccessful && res.Object) {
                setAiResult(res.Object);
                setAiAcceptedLibs(new Set(
                    (res.Object.RecommendedLibraryKeys ?? []).filter(k => allLibraries.some(l => l.LibraryKey === k))
                ));
                setAiAcceptedBuiltIns(new Set(
                    (res.Object.RecommendedBuiltInToolNames ?? []).filter(n => allBuiltInTools.some(t => t.ToolName === n))
                ));
            } else {
                setError(res.ValidationResult?.Items?.[0]?.Message ?? 'Generation failed');
            }
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { setAiGenerating(false); }
    };

    const handleApplyAiResult = async () => {
        if (!aiResult) return;
        update('SystemPrompt', aiResult.SystemPrompt);
        if (aiAcceptedLibs.size > 0) {
            setSubscribedKeys(prev => {
                const next = new Set(prev);
                aiAcceptedLibs.forEach(k => next.add(k));
                return next;
            });
            setSubsChanged(true);
        }
        if (aiAcceptedBuiltIns.size > 0 && editItem.SkillKey.trim()) {
            const newTools = allBuiltInTools
                .filter(t => aiAcceptedBuiltIns.has(t.ToolName))
                .map(t => ({
                    Id: 0, SkillKey: editItem.SkillKey, ToolName: t.ToolName,
                    Description: t.ToolDescription, ToolType: 'BuiltIn',
                    ToolConfig: t.ToolConfig ?? '', IsActive: true, SortOrder: 0,
                }));
            await Promise.all(newTools.map(t => agentSkillSetSvc.UpsertTool(t).catch(() => {})));
        }
        setShowAiGenerate(false);
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
            <div className={`flex items-center gap-1 px-3 py-2 mb-1 mx-2 ${theme.mainContentSection}`}>
                <span className={`text-md font-semibold mr-3 ${theme.title}`}>Agent Management</span>
                <button type="button" className={tabCls('skills')} onClick={() => setActiveTab('skills')}>Agent Setting</button>
                <button type="button" className={tabCls('mcp')} onClick={() => setActiveTab('mcp')}>MCP Servers</button>
                <button type="button" className={tabCls('libraries')} onClick={() => setActiveTab('libraries')}>Tool Libraries</button>
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
                            <div className="relative">
                                <div className="flex">
                                    <button type="button" className={`${btn} rounded-r-none border-r-0`} onClick={beginNew}>
                                        <i className="fa-solid fa-plus mr-1" />New
                                    </button>
                                    {templates.length > 0 && (
                                        <button
                                            type="button"
                                            className={`${btn} rounded-l-none px-1.5`}
                                            onClick={(e) => { e.stopPropagation(); setShowTemplateMenu(o => !o); }}
                                            title="New from template"
                                        >
                                            <i className="fa-solid fa-chevron-down text-xs" />
                                        </button>
                                    )}
                                </div>
                                {showTemplateMenu && (
                                    <div className={`absolute top-full left-0 z-40 mt-1 rounded shadow-lg border min-w-52 ${theme.mainContentSection} ${borderCls}`}>
                                        <div className={`px-2 py-1 text-xs font-semibold opacity-50 ${theme.label} border-b ${borderCls}`}>New from template</div>
                                        {templates.map(tmpl => (
                                            <button
                                                type="button"
                                                key={tmpl.SkillKey}
                                                className={`w-full text-left px-3 py-1.5 text-xs hover:opacity-80 ${theme.label}`}
                                                onClick={() => {
                                                    setEditItem({
                                                        ...emptySkillSet(),
                                                        SystemPrompt: tmpl.SystemPrompt,
                                                        CapabilityFlags: tmpl.CapabilityFlags,
                                                        MaxHistoryTokens: tmpl.MaxHistoryTokens,
                                                        MaxToolResultChars: tmpl.MaxToolResultChars,
                                                        MaxIterations: tmpl.MaxIterations,
                                                        ExecutionMode: tmpl.ExecutionMode || 'Interactive',
                                                        AgentUi: resolveAgentUi(tmpl.AgentUi),
                                                    });
                                                    setSelected(null);
                                                    setIsEditing(true);
                                                    setIsDirty(true);
                                                    setTestSkillKey(null);
                                                    setShowTemplateMenu(false);
                                                    setSubscribedKeys(new Set());
                                                    setSubsChanged(false);
                                                    setPromptHistory([]);
                                                }}
                                            >
                                                <div className="font-semibold">{tmpl.DisplayName}</div>
                                                <div className="opacity-50 truncate">{tmpl.Description}</div>
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>
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

                    <div
                        className={`min-w-0 w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}
                        onClick={() => { setShowTemplateMenu(false); setShowHistory(false); }}
                    >
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
                                        {selected && !selected.SkillKey.startsWith('tmpl-') && (
                                            <button type="button" className={btn} onClick={openSaveAsTemplate} title="Clone this agent as a reusable template">
                                                <i className="fa-solid fa-star mr-1" />Save as Template
                                            </button>
                                        )}
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

                                                <div className="mb-4">
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

                                                <div className="mb-4">
                                                    <div className={sectionTitle}>Tools</div>
                                                    <div className="flex items-center py-1">
                                                        <label className={lbl}>Private Tools</label>
                                                        <button
                                                            type="button"
                                                            className={`${btn} flex items-center gap-1.5`}
                                                            disabled={!editItem.SkillKey.trim()}
                                                            onClick={() => setShowToolsModal(true)}
                                                            title={editItem.SkillKey.trim() ? undefined : 'Save the agent first to manage its tools'}
                                                        >
                                                            <i className="fa-solid fa-screwdriver-wrench" />
                                                            Manage Agent Tools
                                                        </button>
                                                        <span className={`ml-3 text-xs opacity-50 ${theme.label}`}>Tools private to this agent only</span>
                                                    </div>
                                                    {allLibraries.length > 0 && (() => {
                                                        const q = libSearch.toLowerCase();
                                                        const filtered = allLibraries.filter(l =>
                                                            !q || l.LibraryKey.toLowerCase().includes(q) || l.LibraryName.toLowerCase().includes(q) || l.DomainKey.toLowerCase().includes(q)
                                                        );
                                                        const domainOrder = allDomains.length > 0 ? allDomains : [];
                                                        const grouped = domainOrder
                                                            .map(d => ({ domain: d, libs: filtered.filter(l => l.DomainKey === d.DomainKey) }))
                                                            .filter(g => g.libs.length > 0);
                                                        const ungrouped = filtered.filter(l => !allDomains.some(d => d.DomainKey === l.DomainKey));
                                                        const toggleLib = (libraryKey: string, checked: boolean) => {
                                                            setSubscribedKeys(prev => {
                                                                const next = new Set(prev);
                                                                if (checked) next.add(libraryKey); else next.delete(libraryKey);
                                                                return next;
                                                            });
                                                            setSubsChanged(true); setIsDirty(true);
                                                        };
                                                        const libRow = (l: AppAgentToolLibraryDto) => (
                                                            <label key={l.LibraryKey} className={`flex items-center gap-2 px-2 py-0.5 text-xs cursor-pointer hover:opacity-80 ${theme.label}`}>
                                                                <input type="checkbox" checked={subscribedKeys.has(l.LibraryKey)} onChange={e => toggleLib(l.LibraryKey, e.target.checked)} />
                                                                <span className="font-mono font-semibold">{l.LibraryKey}</span>
                                                                <span className="opacity-70">{l.LibraryName && `— ${l.LibraryName}`}</span>
                                                                {l.ToolCount > 0 && <span className="ml-auto opacity-50">({l.ToolCount})</span>}
                                                            </label>
                                                        );
                                                        return (
                                                            <div className={`border rounded p-2 flex flex-col gap-1 mt-2 ${theme.mainContentSection} ${borderCls}`}>
                                                                <div className={`text-xs font-semibold ${theme.title} mb-1`}>
                                                                    <i className="fa-solid fa-book mr-1" />Tool Libraries
                                                                    <span className={`ml-2 font-normal ${theme.label}`}>({subscribedKeys.size} subscribed)</span>
                                                                </div>
                                                                <input className={`${inp} mb-1`} placeholder="Search libraries..." value={libSearch} onChange={e => setLibSearch(e.target.value)} />
                                                                <div className="max-h-48 overflow-y-auto flex flex-col gap-0">
                                                                    {grouped.map(g => (
                                                                        <div key={g.domain.DomainKey} className="mb-1">
                                                                            <div className={`px-1 py-0.5 text-xs font-semibold uppercase tracking-wide opacity-50 ${theme.label}`}>
                                                                                {g.domain.DomainName || g.domain.DomainKey}
                                                                            </div>
                                                                            {g.libs.map(libRow)}
                                                                        </div>
                                                                    ))}
                                                                    {ungrouped.map(libRow)}
                                                                </div>
                                                            </div>
                                                        );
                                                    })()}
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
                                            <div className="relative ml-2">
                                                <button
                                                    type="button"
                                                    className={`text-xs px-1.5 py-0.5 rounded ${theme.button_default} flex items-center gap-1 ${promptHistory.length === 0 ? 'opacity-40' : ''}`}
                                                    onClick={(e) => { e.stopPropagation(); promptHistory.length > 0 && setShowHistory(o => !o); }}
                                                    title={promptHistory.length > 0
                                                        ? `${promptHistory.length} saved version${promptHistory.length > 1 ? 's' : ''} — click to restore`
                                                        : 'History is saved each time you update the system prompt'}
                                                >
                                                    <i className="fa-solid fa-clock-rotate-left" />
                                                    History{promptHistory.length > 0 && <span className="ml-0.5 opacity-60">({promptHistory.length})</span>}
                                                </button>
                                                <button
                                                    type="button"
                                                    className={`ml-1 text-xs px-1.5 py-0.5 rounded ${theme.button_default} flex items-center gap-1`}
                                                    onClick={(e) => { e.stopPropagation(); setAiDescription(''); setAiResult(null); setShowAiGenerate(true); }}
                                                    title="Generate system prompt and tool recommendations with AI"
                                                >
                                                    <i className="fa-solid fa-wand-magic-sparkles" />AI
                                                </button>
                                                {showHistory && promptHistory.length > 0 && (
                                                    <div
                                                        className={`absolute top-full left-0 z-40 mt-1 rounded shadow-lg border w-72 ${theme.mainContentSection} ${borderCls}`}
                                                        style={{ maxHeight: 320, overflowY: 'auto' }}
                                                        onClick={e => e.stopPropagation()}
                                                    >
                                                        <div className={`px-2 py-1 text-xs font-semibold opacity-50 ${theme.label} border-b sticky top-0 ${theme.mainContentSection} ${borderCls}`}>
                                                            Previous versions — click to restore
                                                        </div>
                                                        {promptHistory.map(h => (
                                                            <button
                                                                type="button"
                                                                key={h.HistoryId}
                                                                className={`w-full text-left px-3 py-2 text-xs hover:opacity-80 border-b ${theme.label} ${borderCls}`}
                                                                onClick={() => { update('SystemPrompt', h.SystemPrompt); setShowHistory(false); }}
                                                            >
                                                                <div className="font-semibold opacity-60 mb-0.5">
                                                                    {new Date(h.SavedAt).toLocaleString()}
                                                                    {h.SavedBy && <span className="ml-1 font-normal">by {h.SavedBy}</span>}
                                                                </div>
                                                                <div className="opacity-50 truncate">{h.SystemPrompt.slice(0, 80)}…</div>
                                                            </button>
                                                        ))}
                                                    </div>
                                                )}
                                            </div>
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
                {activeTab === 'mcp'       && <AgentMcpServerTab theme={theme} />}
                {activeTab === 'libraries' && <AgentLibraryTab />}
            </div>

            {/* Private Tools modal */}
            {showToolsModal && editItem.SkillKey && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50" onClick={e => { if (e.target === e.currentTarget) setShowToolsModal(false); }}>
                    <div className={`flex flex-col rounded shadow-2xl overflow-hidden ${theme.mainContentSection}`} style={{ width: '80vw', height: '80vh' }}>
                        <div className={`flex items-center px-4 py-2 border-b ${borderCls} ${theme.mainContentSection}`}>
                            <i className="fa-solid fa-screwdriver-wrench mr-2" />
                            <span className={`text-sm font-semibold ${theme.title} w-1 flex-auto`}>
                                Private Tools — <span className="font-mono">{editItem.SkillKey}</span>
                            </span>
                            <span className={`text-xs mr-4 opacity-50 ${theme.label}`}>Tools owned exclusively by this agent</span>
                            <button type="button" className={`${btn} ml-auto`} onClick={() => setShowToolsModal(false)}>
                                <i className="fa-solid fa-xmark mr-1" />Close
                            </button>
                        </div>
                        <div className="w-full h-1 flex-auto overflow-hidden">
                            <AgentToolRegisterTab selectedSkillKey={editItem.SkillKey} theme={theme} />
                        </div>
                    </div>
                </div>
            )}

            {/* Save as Template modal */}
            {showSaveAsTemplate && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50"
                     onClick={e => { if (e.target === e.currentTarget) setShowSaveAsTemplate(false); }}>
                    <div className={`flex flex-col rounded shadow-2xl overflow-hidden ${theme.mainContentSection}`} style={{ width: 420 }}>
                        <div className={`flex items-center px-4 py-2 border-b ${borderCls}`}>
                            <i className="fa-solid fa-star mr-2" />
                            <span className={`text-sm font-semibold ${theme.title} w-1 flex-auto`}>Save as Template</span>
                            <button type="button" className={btn} onClick={() => setShowSaveAsTemplate(false)}><i className="fa-solid fa-xmark" /></button>
                        </div>
                        <div className="px-4 py-3 flex flex-col gap-3">
                            <p className={`text-xs opacity-60 ${theme.label}`}>
                                Creates a reusable template from this agent. It will appear in the "+ New ▾" picker.
                                The original agent is unchanged.
                            </p>
                            <div className="flex flex-col gap-1">
                                <label className={`text-xs ${theme.label}`}>Template Name</label>
                                <input className={inp} value={tmplName} onChange={e => setTmplName(e.target.value)} />
                            </div>
                            <div className="flex flex-col gap-1">
                                <label className={`text-xs ${theme.label}`}>Template Key</label>
                                <div className="flex items-center gap-1">
                                    <span className={`text-xs opacity-50 ${theme.label} shrink-0`}>tmpl-</span>
                                    <input className={`${inp}`} value={tmplKey}
                                           onChange={e => setTmplKey(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))} />
                                </div>
                                <span className={`text-xs opacity-40 ${theme.label}`}>Final key: tmpl-{tmplKey || '…'}</span>
                            </div>
                        </div>
                        <div className={`flex items-center justify-end gap-2 px-4 py-2 border-t ${borderCls}`}>
                            <button type="button" className={btn} onClick={() => setShowSaveAsTemplate(false)}>Cancel</button>
                            <button type="button" className={btn} onClick={handleSaveAsTemplate} disabled={!tmplKey.trim()}>
                                <i className="fa-solid fa-star mr-1" />Save as Template
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* AI Generate modal */}
            {showAiGenerate && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50"
                     onClick={e => { if (e.target === e.currentTarget) setShowAiGenerate(false); }}>
                    <div className={`flex flex-col rounded shadow-2xl overflow-hidden ${theme.mainContentSection}`} style={{ width: 520 }}>
                        <div className={`flex items-center px-4 py-2 border-b border-gray-200`}>
                            <i className="fa-solid fa-wand-magic-sparkles mr-2 text-purple-500" />
                            <span className={`text-sm font-semibold ${theme.title} flex-auto`}>
                                {aiResult ? 'Review AI-Generated Design' : 'AI Generate Agent Design'}
                            </span>
                            <button className={btn} onClick={() => setShowAiGenerate(false)}><i className="fa-solid fa-xmark" /></button>
                        </div>

                        {aiResult === null ? (
                            /* Phase 1: description input */
                            <>
                                <div className="px-4 py-3 flex flex-col gap-2">
                                    <p className={`text-xs opacity-60 ${theme.label}`}>
                                        Describe what this agent should do. The AI will generate the system prompt
                                        and recommend relevant tool libraries and built-in tools.
                                    </p>
                                    <textarea
                                        className={`w-full px-2 py-1.5 text-xs border ${theme.inputBox} focus:outline-none resize-none font-mono`}
                                        rows={5}
                                        value={aiDescription}
                                        onChange={e => setAiDescription(e.target.value)}
                                        placeholder={'Example: "An agent that helps warehouse managers query stock levels and outstanding POs. Read-only, formats results as tables."'}
                                        autoFocus
                                    />
                                </div>
                                <div className="flex items-center justify-end gap-2 px-4 py-2 border-t border-gray-200">
                                    <button className={btn} onClick={() => setShowAiGenerate(false)}>Cancel</button>
                                    <button className={btn} onClick={handleAiGenerate} disabled={aiGenerating || !aiDescription.trim()}>
                                        {aiGenerating
                                            ? <><i className="fa-solid fa-spinner fa-spin mr-1" />Generating…</>
                                            : <><i className="fa-solid fa-wand-magic-sparkles mr-1" />Generate</>
                                        }
                                    </button>
                                </div>
                            </>
                        ) : (
                            /* Phase 2: review result */
                            <>
                                <div className="px-4 py-3 flex flex-col gap-3 overflow-y-auto" style={{ maxHeight: '70vh' }}>
                                    <div className="flex flex-col gap-1">
                                        <div className={`text-xs font-semibold ${theme.title}`}>Generated System Prompt:</div>
                                        <textarea
                                            className={`w-full px-2 py-1.5 text-xs border ${theme.inputBox} font-mono resize-none opacity-80`}
                                            rows={8}
                                            readOnly
                                            value={aiResult.SystemPrompt}
                                        />
                                    </div>

                                    <div className="flex flex-col gap-1">
                                        <div className={`text-xs font-semibold ${theme.title}`}>
                                            <i className="fa-solid fa-book mr-1" />Recommended Tool Libraries:
                                        </div>
                                        {aiResult.RecommendedLibraryKeys.length === 0 ? (
                                            <div className={`text-xs opacity-40 ${theme.label} px-1`}>No matching libraries found in the catalog</div>
                                        ) : aiResult.RecommendedLibraryKeys.map(key => {
                                            const lib = allLibraries.find(l => l.LibraryKey === key);
                                            return (
                                                <label key={key} className={`flex items-center gap-2 px-2 py-0.5 text-xs cursor-pointer ${theme.label}`}>
                                                    <input type="checkbox"
                                                        checked={aiAcceptedLibs.has(key)}
                                                        onChange={e => setAiAcceptedLibs(prev => {
                                                            const n = new Set(prev);
                                                            e.target.checked ? n.add(key) : n.delete(key);
                                                            return n;
                                                        })} />
                                                    <span className="font-mono font-semibold">{key}</span>
                                                    {lib && <span className="opacity-60">— {lib.LibraryName}</span>}
                                                    {!lib && <span className="opacity-40 italic">(not in catalog)</span>}
                                                </label>
                                            );
                                        })}
                                    </div>

                                    <div className="flex flex-col gap-1">
                                        <div className={`text-xs font-semibold ${theme.title}`}>
                                            <i className="fa-solid fa-screwdriver-wrench mr-1" />Recommended Built-in Tools:
                                        </div>
                                        {aiResult.RecommendedBuiltInToolNames.length === 0 ? (
                                            <div className={`text-xs opacity-40 ${theme.label} px-1`}>No matching built-in tools found</div>
                                        ) : aiResult.RecommendedBuiltInToolNames.map(name => {
                                            const tool = allBuiltInTools.find(t => t.ToolName === name);
                                            const disabled = !editItem.SkillKey.trim();
                                            return (
                                                <label key={name} className={`flex items-center gap-2 px-2 py-0.5 text-xs cursor-pointer ${theme.label} ${disabled ? 'opacity-40' : ''}`}
                                                       title={disabled ? 'Save the agent first to register built-in tools' : undefined}>
                                                    <input type="checkbox"
                                                        checked={aiAcceptedBuiltIns.has(name)}
                                                        disabled={disabled}
                                                        onChange={e => setAiAcceptedBuiltIns(prev => {
                                                            const n = new Set(prev);
                                                            e.target.checked ? n.add(name) : n.delete(name);
                                                            return n;
                                                        })} />
                                                    <span className="font-mono font-semibold">{name}</span>
                                                    {tool && <span className="opacity-60">— {tool.ToolDescription}</span>}
                                                    {!tool && <span className="opacity-40 italic">(not in catalog)</span>}
                                                </label>
                                            );
                                        })}
                                        {!editItem.SkillKey.trim() && (
                                            <div className={`text-xs opacity-40 ${theme.label} px-1 mt-0.5`}>
                                                <i className="fa-solid fa-circle-info mr-1" />Save the agent first — built-in tools will register on next "Use this"
                                            </div>
                                        )}
                                    </div>

                                    <div className={`text-xs opacity-40 ${theme.label} border-t border-gray-100 pt-2`}>
                                        <i className="fa-solid fa-server mr-1" />MCP Servers need manual configuration — use the MCP Servers tab after saving.
                                    </div>
                                </div>
                                <div className="flex items-center gap-2 px-4 py-2 border-t border-gray-200">
                                    <button className={btn} onClick={() => setAiResult(null)}>
                                        <i className="fa-solid fa-arrow-left mr-1" />Regenerate
                                    </button>
                                    <div className="flex-auto" />
                                    <button className={btn} onClick={() => setShowAiGenerate(false)}>Cancel</button>
                                    <button className={btn} onClick={handleApplyAiResult}>
                                        <i className="fa-solid fa-check mr-1" />Use this
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}

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
