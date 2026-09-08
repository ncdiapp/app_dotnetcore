import React, { useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import {
    agentSkillSetSvc,
    AppAgentSkillSetDto, AppAgentToolDomainDto, AppAgentToolLibraryDto, AppAgentPromptHistoryDto,
} from '../../webapi/agentSkillSetSvc';
import AgentToolRegisterTab from './AgentToolRegisterTab';
import AgentMcpServerTab from './AgentMcpServerTab';
import AgentLibraryTab from './AgentLibraryTab';
import GenericAgentChat from './GenericAgentChat';

type Tab = 'skills' | 'mcp' | 'libraries';

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
});

const AgentSkillSetManagement: React.FC = () => {
    const { theme } = useTheme();
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
    const [showToolsModal, setShowToolsModal] = useState(false);

    // Templates
    const [templates, setTemplates] = useState<AppAgentSkillSetDto[]>([]);
    const [showTemplateMenu, setShowTemplateMenu] = useState(false);

    // Prompt history
    const [promptHistory, setPromptHistory] = useState<AppAgentPromptHistoryDto[]>([]);
    const [showHistory, setShowHistory] = useState(false);

    // Library subscription state
    const [allDomains, setAllDomains] = useState<AppAgentToolDomainDto[]>([]);
    const [allLibraries, setAllLibraries] = useState<AppAgentToolLibraryDto[]>([]);
    const [subscribedKeys, setSubscribedKeys] = useState<Set<string>>(new Set());
    const [libSearch, setLibSearch] = useState('');
    const [subsChanged, setSubsChanged] = useState(false);

    const load = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllSkillSets();
            skillsCV.sourceCollection = res.Object ?? [];
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
    }, []);

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item); setEditItem({ ...item }); setIsEditing(true); setIsDirty(false);
        setSubsChanged(false); setShowHistory(false);
        loadSubscriptions(item.SkillKey);
        loadHistory(item.SkillKey);
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
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertSkillSet(editItem);
            if (subsChanged) {
                await agentSkillSetSvc.SetSubscriptions(editItem.SkillKey, Array.from(subscribedKeys));
                setSubsChanged(false);
            }
            setIsDirty(false);
            await load();
            setSelected(editItem);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleDelete = async () => {
        if (!selected) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteSkillSet(selected.SkillKey);
            setSelected(null); setEditItem(emptySkillSet()); setIsEditing(false); setIsDirty(false);
            setConfirmDelete(false); await load();
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const tabCls = (t: Tab) =>
        `px-3 py-1.5 text-xs rounded-[4px] cursor-pointer mr-1 ${theme.button_default}${activeTab === t ? ' border-b-2 font-semibold' : ''}`;
    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-32 text-xs ${theme.label} mr-2`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            <div className={`flex items-center gap-1 px-3 py-2 ${theme.mainContentSection}`}>
                <span className={`text-md font-semibold mr-3 ${theme.title}`}>Agent Management</span>
                <button className={tabCls('skills')} onClick={() => setActiveTab('skills')}>Agent Setting</button>
                <button className={tabCls('mcp')} onClick={() => setActiveTab('mcp')}>MCP Servers</button>
                <button className={tabCls('libraries')} onClick={() => setActiveTab('libraries')}>Tool Libraries</button>
            </div>
            {error && (
                <div className="px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 mx-2 mb-1 rounded">
                    {error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                </div>
            )}
            <div className="w-full h-1 flex-auto overflow-hidden">
                <div className={`w-full h-full flex gap-2 px-2 pb-2 overflow-hidden${activeTab === 'skills' ? '' : ' hidden'}`}>
                        <div className={`w-56 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                            <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                                {/* +New with template dropdown */}
                                <div className="relative">
                                    <div className="flex">
                                        <button className={`${btn} rounded-r-none border-r-0`} onClick={() => { setSelected(null); setEditItem(emptySkillSet()); setIsEditing(true); setIsDirty(false); setTestSkillKey(null); setShowTemplateMenu(false); }}>
                                            <i className="fa-solid fa-plus mr-1" />New
                                        </button>
                                        {templates.length > 0 && (
                                            <button className={`${btn} rounded-l-none px-1.5`} onClick={() => setShowTemplateMenu(o => !o)} title="New from template">
                                                <i className="fa-solid fa-chevron-down text-xs" />
                                            </button>
                                        )}
                                    </div>
                                    {showTemplateMenu && (
                                        <div className={`absolute top-full left-0 z-40 mt-1 rounded shadow-lg border border-gray-200 min-w-52 ${theme.mainContentSection}`}>
                                            <div className={`px-2 py-1 text-xs font-semibold opacity-50 ${theme.label} border-b border-gray-100`}>New from template</div>
                                            {templates.map(t => (
                                                <button key={t.SkillKey} className={`w-full text-left px-3 py-1.5 text-xs hover:opacity-80 ${theme.label}`}
                                                    onClick={() => {
                                                        setEditItem({ ...emptySkillSet(), SystemPrompt: t.SystemPrompt, CapabilityFlags: t.CapabilityFlags, MaxHistoryTokens: t.MaxHistoryTokens, MaxToolResultChars: t.MaxToolResultChars, MaxIterations: t.MaxIterations });
                                                        setSelected(null); setIsEditing(true); setIsDirty(true); setTestSkillKey(null); setShowTemplateMenu(false);
                                                    }}>
                                                    <div className="font-semibold">{t.DisplayName}</div>
                                                    <div className="opacity-50 truncate">{t.Description}</div>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                </div>
                                {selected && <button className={btn} onClick={() => setConfirmDelete(true)}><i className="fa-solid fa-trash mr-1" />Delete</button>}
                                {selected && <button className={btn} onClick={() => setTestSkillKey(selected.SkillKey)}><i className="fa-solid fa-play mr-1" />Run</button>}
                            </div>
                            <div className="w-full h-1 flex-auto overflow-hidden">
                                <FlexGrid className="w-full h-full" itemsSource={skillsCV} isReadOnly headersVisibility="Column" selectionChanged={onGridSelectionChanged}>
                                    <FlexGridColumn header="Agent Code" binding="SkillKey" width="*" />
                                    <FlexGridColumn header="Active" binding="IsActive" width={55} />
                                    <FlexGridColumn header="" binding="" width="*" />
                                </FlexGrid>
                            </div>
                        </div>
                        <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}
                             onClick={() => { setShowTemplateMenu(false); setShowHistory(false); }}>
                            {testSkillKey ? (
                                <div className="w-full h-full flex flex-col overflow-hidden">
                                    <div className={`flex items-center px-3 py-1 border-b border-gray-200 ${theme.mainContentSection}`}>
                                        <i className="fa-solid fa-play mr-2 text-green-500" />
                                        <span className={`text-xs font-semibold ${theme.title} flex-auto`}>Testing: {testSkillKey}</span>
                                        <button className={btn} onClick={() => setTestSkillKey(null)}><i className="fa-solid fa-xmark" /></button>
                                    </div>
                                    <div className="w-full h-1 flex-auto overflow-hidden">
                                        <GenericAgentChat skillKey={testSkillKey} />
                                    </div>
                                </div>
                            ) : isEditing ? (
                                <div className="h-full flex flex-col overflow-hidden">
                                    <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-3">
                                        <div className="flex items-center py-1">
                                            <label className={lbl}>Agent Code *</label>
                                            <input className={inp} value={editItem.SkillKey} onChange={e => update('SkillKey', e.target.value)} autoComplete="off" />
                                        </div>
                                        <div className="flex items-center py-1">
                                            <label className={lbl}>Display Name</label>
                                            <input className={inp} value={editItem.DisplayName} onChange={e => update('DisplayName', e.target.value)} autoComplete="off" />
                                        </div>
                                        <div className="flex items-start py-1">
                                            <label className={`${lbl} mt-1`}>Description</label>
                                            <textarea className={`flex-auto w-32 px-2 py-1 text-xs border ${theme.inputBox}`} rows={2} value={editItem.Description} onChange={e => update('Description', e.target.value)} />
                                        </div>
                                        <div className="flex items-start py-1">
                                            <div className={`${lbl} mt-1 flex flex-col gap-1`}>
                                                <span>System Prompt</span>
                                                {promptHistory.length > 0 && (
                                                    <div className="relative">
                                                        <button
                                                            className={`text-xs px-1.5 py-0.5 rounded ${theme.button_default} flex items-center gap-1`}
                                                            onClick={() => setShowHistory(o => !o)}
                                                            title="Restore a previous version"
                                                        >
                                                            <i className="fa-solid fa-clock-rotate-left" />
                                                            History
                                                        </button>
                                                        {showHistory && (
                                                            <div className={`absolute top-full left-0 z-40 mt-1 rounded shadow-lg border border-gray-200 w-72 ${theme.mainContentSection}`} style={{ maxHeight: 320, overflowY: 'auto' }}>
                                                                <div className={`px-2 py-1 text-xs font-semibold opacity-50 ${theme.label} border-b border-gray-100 sticky top-0 ${theme.mainContentSection}`}>
                                                                    Previous versions — click to restore
                                                                </div>
                                                                {promptHistory.map(h => (
                                                                    <button key={h.HistoryId} className={`w-full text-left px-3 py-2 text-xs hover:opacity-80 border-b border-gray-100 ${theme.label}`}
                                                                        onClick={() => { update('SystemPrompt', h.SystemPrompt); setShowHistory(false); }}>
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
                                                )}
                                            </div>
                                            <textarea className={`flex-auto w-32 px-2 py-1 text-xs border font-mono ${theme.inputBox}`} rows={5} value={editItem.SystemPrompt} onChange={e => update('SystemPrompt', e.target.value)} />
                                        </div>
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
                                        <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                                            {([['MaxHistoryTokens', 'Max History Tokens'], ['SummarizeThreshold', 'Summarize Threshold'], ['MaxToolResultChars', 'Max Tool Result Chars'], ['RecentWindowSize', 'Recent Window Size'], ['MaxIterations', 'Max Iterations']] as const).map(([field, label]) => (
                                                <div key={field} className="flex items-center">
                                                    <label className={`w-40 text-xs ${theme.label} mr-2`}>{label}</label>
                                                    <input className={`w-24 h-7 px-2 text-xs border ${theme.inputBox}`} type="number" value={editItem[field]} onChange={e => update(field, parseInt(e.target.value) || 0)} />
                                                </div>
                                            ))}
                                        </div>
                                        <div className="flex items-center py-1">
                                            <label className={lbl}>Active</label>
                                            <input type="checkbox" checked={editItem.IsActive} onChange={e => update('IsActive', e.target.checked)} />
                                        </div>

                                        {/* Private Tools */}
                                        <div className="flex items-center py-1">
                                            <label className={lbl}>Private Tools</label>
                                            <button
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

                                        {/* Tool Library Subscriptions */}
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
                                                <div className={`border rounded p-2 flex flex-col gap-1 ${theme.mainContentSection}`}>
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
                                    <div className="flex items-center gap-2 px-3 py-2 border-t border-gray-200">
                                        <button className={btn} onClick={handleSave} disabled={!isDirty}><i className="fa-solid fa-floppy-disk mr-1" />Save</button>
                                        <button className={btn} onClick={() => { if (selected) { setEditItem({ ...selected }); setIsDirty(false); } else { setIsEditing(false); } }} disabled={!isDirty}>Cancel</button>
                                        {isDirty && <span className="text-xs text-orange-500 ml-2">Unsaved changes</span>}
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
                        <div className={`flex items-center px-4 py-2 border-b border-gray-200 ${theme.mainContentSection}`}>
                            <i className="fa-solid fa-screwdriver-wrench mr-2 text-blue-500" />
                            <span className={`text-sm font-semibold ${theme.title} flex-auto`}>
                                Private Tools — <span className="font-mono">{editItem.SkillKey}</span>
                            </span>
                            <span className={`text-xs mr-4 opacity-50 ${theme.label}`}>Tools owned exclusively by this agent</span>
                            <button className={`${btn} ml-auto`} onClick={() => setShowToolsModal(false)}>
                                <i className="fa-solid fa-xmark mr-1" />Close
                            </button>
                        </div>
                        <div className="w-full h-1 flex-auto overflow-hidden">
                            <AgentToolRegisterTab selectedSkillKey={editItem.SkillKey} theme={theme} />
                        </div>
                    </div>
                </div>
            )}

            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>Delete skill set "{selected?.SkillKey}"?</div>
                        <div className="flex gap-2">
                            <button className={btn} onClick={handleDelete}>Delete</button>
                            <button className={btn} onClick={() => setConfirmDelete(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AgentSkillSetManagement;
