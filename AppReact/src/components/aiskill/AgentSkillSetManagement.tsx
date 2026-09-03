import React, { useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { agentSkillSetSvc, AppAgentSkillSetDto } from '../../webapi/agentSkillSetSvc';
import AgentToolRegisterTab from './AgentToolRegisterTab';
import AgentMcpServerTab from './AgentMcpServerTab';
import GenericAgentChat from './GenericAgentChat';

type Tab = 'skills' | 'tools' | 'mcp';

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
    MaxHistoryTokens: 80000, SummarizeThreshold: 60000, MaxToolResultChars: 4000, RecentWindowSize: 10,
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

    const load = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllSkillSets();
            skillsCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    useEffect(() => { load(); }, []);

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentSkillSetDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item); setEditItem({ ...item }); setIsEditing(true); setIsDirty(false);
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
        if (!editItem.SkillKey.trim()) { setError('Skill Key is required.'); return; }
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertSkillSet(editItem);
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
                <span className={`text-md font-semibold mr-3 ${theme.title}`}>Agent Skill Set</span>
                <button className={tabCls('skills')} onClick={() => setActiveTab('skills')}>Skill Sets</button>
                <button className={tabCls('tools')} onClick={() => setActiveTab('tools')}>Tool Register</button>
                <button className={tabCls('mcp')} onClick={() => setActiveTab('mcp')}>MCP Servers</button>
            </div>
            {error && (
                <div className="px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 mx-2 mb-1 rounded">
                    {error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                </div>
            )}
            <div className="w-full h-1 flex-auto overflow-hidden">
                {activeTab === 'skills' && (
                    <div className="w-full h-full flex gap-2 px-2 pb-2 overflow-hidden">
                        <div className={`w-56 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                            <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                                <button className={btn} onClick={() => { setSelected(null); setEditItem(emptySkillSet()); setIsEditing(true); setIsDirty(false); setTestSkillKey(null); }}>
                                    <i className="fa-solid fa-plus mr-1" />New
                                </button>
                                {selected && <button className={btn} onClick={() => setConfirmDelete(true)}><i className="fa-solid fa-trash mr-1" />Delete</button>}
                                {selected && <button className={btn} onClick={() => setTestSkillKey(selected.SkillKey)}><i className="fa-solid fa-play mr-1" />Run</button>}
                            </div>
                            <div className="w-full h-1 flex-auto overflow-hidden">
                                <FlexGrid className="w-full h-full" itemsSource={skillsCV} isReadOnly headersVisibility="Column" selectionChanged={onGridSelectionChanged}>
                                    <FlexGridColumn header="Skill Key" binding="SkillKey" width="*" />
                                    <FlexGridColumn header="Active" binding="IsActive" width={55} />
                                    <FlexGridColumn header="" binding="" width="*" />
                                </FlexGrid>
                            </div>
                        </div>
                        <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
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
                                            <label className={lbl}>Skill Key *</label>
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
                                            <label className={`${lbl} mt-1`}>System Prompt</label>
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
                                        <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                                            {([['MaxHistoryTokens', 'Max History Tokens'], ['SummarizeThreshold', 'Summarize Threshold'], ['MaxToolResultChars', 'Max Tool Result Chars'], ['RecentWindowSize', 'Recent Window Size']] as const).map(([field, label]) => (
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
                )}
                {activeTab === 'tools' && <AgentToolRegisterTab selectedSkillKey={selected?.SkillKey ?? null} theme={theme} />}
                {activeTab === 'mcp'   && <AgentMcpServerTab theme={theme} />}
            </div>
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
