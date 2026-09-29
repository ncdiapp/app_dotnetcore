import React, { useEffect, useMemo, useState } from 'react';
import {
    agentSkillSetSvc,
    AppAgentChildMappingDto,
    AppAgentSkillSetDto,
} from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

export interface AgentChildAgentTabProps {
    parentSkillKey: string;
    allAgents: AppAgentSkillSetDto[];
    mappings: AppAgentChildMappingDto[];
    onMappingsChanged: () => Promise<void> | void;
    onOpenAgent: (skillKey: string) => void;
    onError: (message: string | null) => void;
    theme: Theme;
    borderCls: string;
    btn: string;
    inp: string;
    lbl: string;
}

const AgentChildAgentTab: React.FC<AgentChildAgentTabProps> = ({
    parentSkillKey,
    allAgents,
    mappings,
    onMappingsChanged,
    onOpenAgent,
    onError,
    theme,
    borderCls,
    btn,
    inp,
    lbl,
}) => {
    const [showPicker, setShowPicker] = useState(false);
    const [showCreate, setShowCreate] = useState(false);
    const [pickerSearch, setPickerSearch] = useState('');
    const [hideAlreadyInTeam, setHideAlreadyInTeam] = useState(true);
    const [selectedKeys, setSelectedKeys] = useState<Set<string>>(new Set());
    const [createCode, setCreateCode] = useState('');
    const [createName, setCreateName] = useState('');
    const [createMode, setCreateMode] = useState('Deterministic');
    const [busy, setBusy] = useState(false);

    const children = useMemo(() => {
        const rows = mappings.filter(m => m.ParentSkillKey === parentSkillKey);
        return rows.sort((a, b) => {
            const aa = allAgents.find(x => x.SkillKey === a.ChildSkillKey);
            const bb = allAgents.find(x => x.SkillKey === b.ChildSkillKey);
            const an = (a.ChildDisplayName || aa?.DisplayName || a.ChildSkillKey).toLowerCase();
            const bn = (b.ChildDisplayName || bb?.DisplayName || b.ChildSkillKey).toLowerCase();
            const byName = an.localeCompare(bn);
            if (byName !== 0) return byName;
            return a.ChildSkillKey.localeCompare(b.ChildSkillKey);
        });
    }, [mappings, parentSkillKey, allAgents]);

    const childKeySet = useMemo(() => new Set(children.map(c => c.ChildSkillKey)), [children]);

    const agentByKey = useMemo(() => {
        const m = new Map<string, AppAgentSkillSetDto>();
        allAgents.forEach(a => m.set(a.SkillKey, a));
        return m;
    }, [allAgents]);

    useEffect(() => {
        setSelectedKeys(new Set());
        setPickerSearch('');
    }, [parentSkillKey, showPicker]);

    const pickerRows = useMemo(() => {
        const q = pickerSearch.trim().toLowerCase();
        return allAgents
            .filter(a => !a.SkillKey.startsWith('tmpl-'))
            .filter(a => a.SkillKey !== parentSkillKey)
            .filter(a => !(hideAlreadyInTeam && childKeySet.has(a.SkillKey)))
            .filter(a => {
                if (!q) return true;
                return `${a.SkillKey} ${a.DisplayName}`.toLowerCase().includes(q);
            })
            .slice()
            .sort((a, b) => {
                const an = (a.DisplayName || a.SkillKey).toLowerCase();
                const bn = (b.DisplayName || b.SkillKey).toLowerCase();
                return an.localeCompare(bn) || a.SkillKey.localeCompare(b.SkillKey);
            });
    }, [allAgents, parentSkillKey, hideAlreadyInTeam, childKeySet, pickerSearch]);

    const removeChild = async (childSkillKey: string) => {
        setBusy(true);
        onError(null);
        try {
            const res = await agentSkillSetSvc.RemoveChildAgent(parentSkillKey, childSkillKey);
            if (!res.IsSuccessful || res.Object === false) {
                onError(res.ValidationResult?.Items?.[0]?.Message || 'Remove failed.');
                return;
            }
            await onMappingsChanged();
        } catch (e: unknown) {
            onError(e instanceof Error ? e.message : String(e));
        } finally {
            setBusy(false);
        }
    };

    const addSelected = async () => {
        const keys = Array.from(selectedKeys);
        if (keys.length === 0) return;
        setBusy(true);
        onError(null);
        try {
            const res = await agentSkillSetSvc.AddChildAgents(parentSkillKey, keys);
            if (!res.IsSuccessful || res.Object === false) {
                onError(res.ValidationResult?.Items?.[0]?.Message || 'Add failed.');
                return;
            }
            setShowPicker(false);
            await onMappingsChanged();
        } catch (e: unknown) {
            onError(e instanceof Error ? e.message : String(e));
        } finally {
            setBusy(false);
        }
    };

    const createAndAdd = async () => {
        const code = createCode.trim();
        if (!code) {
            onError('Agent Code is required.');
            return;
        }
        setBusy(true);
        onError(null);
        try {
            const dto: AppAgentSkillSetDto = {
                SkillKey: code,
                DisplayName: createName.trim() || code,
                Description: '',
                SystemPrompt: '',
                CapabilityFlags: 3,
                IsActive: false,
                SortOrder: 0,
                Version: 1,
                MaxHistoryTokens: 80000,
                SummarizeThreshold: 60000,
                MaxToolResultChars: 4000,
                RecentWindowSize: 10,
                MaxIterations: 40,
                ExecutionMode: createMode || 'Deterministic',
                AgentUi: 1,
                AllowAgentFirstTurn: false,
            };
            const upsert = await agentSkillSetSvc.UpsertSkillSet(dto);
            if (!upsert.IsSuccessful && upsert.Object === false) {
                onError(upsert.ValidationResult?.Items?.[0]?.Message || 'Create agent failed.');
                return;
            }
            const add = await agentSkillSetSvc.AddChildAgents(parentSkillKey, [code]);
            if (!add.IsSuccessful || add.Object === false) {
                onError(add.ValidationResult?.Items?.[0]?.Message || 'Created agent but failed to add as Child-Agent.');
                return;
            }
            setShowCreate(false);
            setCreateCode('');
            setCreateName('');
            await onMappingsChanged();
            onOpenAgent(code);
        } catch (e: unknown) {
            onError(e instanceof Error ? e.message : String(e));
        } finally {
            setBusy(false);
        }
    };

    if (!parentSkillKey.trim()) {
        return <div className={`px-4 py-3 text-xs ${theme.label}`}>Save the agent first to manage Child-Agents.</div>;
    }

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            <div className={`shrink-0 flex items-center gap-2 px-3 py-2 border-b ${borderCls}`}>
                <span className={`text-xs font-semibold ${theme.title}`}>Child-Agents of this Orchestrator</span>
                <span className={`text-xs ${theme.label}`}>({children.length})</span>
                <div className="w-1 flex-auto" />
                <button type="button" className={btn} disabled={busy} onClick={() => setShowPicker(true)}>
                    <i className="fa-solid fa-plus mr-1" />Add from Agents
                </button>
                <button type="button" className={btn} disabled={busy} onClick={() => setShowCreate(true)}>
                    <i className="fa-solid fa-file-circle-plus mr-1" />Create &amp; Add
                </button>
            </div>

            <div className="w-full h-1 flex-auto overflow-auto px-3 py-2">
                {children.length === 0 ? (
                    <div className={`text-xs ${theme.label}`}>
                        No Child-Agents yet. Add existing agents or create a new Child-Agent.
                        Only registered Child-Agents can be invoked via <span className="font-mono">call_agent</span>.
                    </div>
                ) : (
                    <table className="w-full text-xs">
                        <thead>
                            <tr className={`border-b ${borderCls} ${theme.label}`}>
                                <th className="text-left py-1 pr-2 font-semibold">Display Name</th>
                                <th className="text-left py-1 pr-2 font-semibold">Agent Code</th>
                                <th className="text-left py-1 pr-2 font-semibold w-20">Mode</th>
                                <th className="text-left py-1 pr-2 font-semibold w-16">Active</th>
                                <th className="text-right py-1 font-semibold w-40">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {children.map((c) => {
                                const agent = agentByKey.get(c.ChildSkillKey);
                                const name = c.ChildDisplayName || agent?.DisplayName || c.ChildSkillKey;
                                const mode = c.ChildExecutionMode || agent?.ExecutionMode || '—';
                                const active = c.ChildIsActive ?? agent?.IsActive;
                                return (
                                    <tr key={c.ChildSkillKey} className={`border-b ${borderCls}`}>
                                        <td className={`py-1.5 pr-2 ${theme.title}`}>{name}</td>
                                        <td className={`py-1.5 pr-2 font-mono ${theme.label}`}>{c.ChildSkillKey}</td>
                                        <td className={`py-1.5 pr-2 ${theme.label}`}>{mode === 'Deterministic' ? 'Det' : mode === 'Interactive' ? 'Int' : mode}</td>
                                        <td className={`py-1.5 pr-2 ${theme.label}`}>{active ? 'Yes' : 'No'}</td>
                                        <td className="py-1.5 text-right">
                                            <button type="button" className={`${btn} mr-1`} onClick={() => onOpenAgent(c.ChildSkillKey)}>Open</button>
                                            <button type="button" className={btn} disabled={busy} onClick={() => removeChild(c.ChildSkillKey)}>Remove</button>
                                        </td>
                                    </tr>
                                );
                            })}
                        </tbody>
                    </table>
                )}
            </div>

            {showPicker && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50" onClick={e => { if (e.target === e.currentTarget) setShowPicker(false); }}>
                    <div className={`flex flex-col rounded shadow-2xl overflow-hidden ${theme.mainContentSection}`} style={{ width: 520, maxHeight: '80vh' }}>
                        <div className={`flex items-center px-4 py-2 border-b ${borderCls}`}>
                            <span className={`text-sm font-semibold ${theme.title} w-1 flex-auto`}>Add Child-Agents</span>
                            <button type="button" className={btn} onClick={() => setShowPicker(false)}><i className="fa-solid fa-xmark" /></button>
                        </div>
                        <div className="px-4 py-2 flex flex-col gap-2">
                            <input className={inp} placeholder="Search…" value={pickerSearch} onChange={e => setPickerSearch(e.target.value)} />
                            <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
                                <input type="checkbox" checked={hideAlreadyInTeam} onChange={e => setHideAlreadyInTeam(e.target.checked)} />
                                Hide agents already in this team
                            </label>
                        </div>
                        <div className="w-full h-1 flex-auto overflow-auto px-2 pb-2" style={{ minHeight: 240 }}>
                            {pickerRows.map(a => (
                                <label key={a.SkillKey} className={`flex items-center gap-2 px-2 py-1 text-xs cursor-pointer hover:opacity-80 ${theme.label}`}>
                                    <input
                                        type="checkbox"
                                        disabled={childKeySet.has(a.SkillKey)}
                                        checked={selectedKeys.has(a.SkillKey)}
                                        onChange={e => {
                                            setSelectedKeys(prev => {
                                                const next = new Set(prev);
                                                if (e.target.checked) next.add(a.SkillKey); else next.delete(a.SkillKey);
                                                return next;
                                            });
                                        }}
                                    />
                                    <span className="font-semibold">{a.DisplayName || a.SkillKey}</span>
                                    <span className="font-mono opacity-60">{a.SkillKey}</span>
                                    <span className="ml-auto opacity-50">{a.IsActive ? 'Active' : 'Inactive'} · {a.ExecutionMode === 'Deterministic' ? 'Det' : 'Int'}</span>
                                </label>
                            ))}
                        </div>
                        <div className={`flex justify-end gap-2 px-4 py-2 border-t ${borderCls}`}>
                            <button type="button" className={btn} onClick={() => setShowPicker(false)}>Cancel</button>
                            <button type="button" className={btn} disabled={busy || selectedKeys.size === 0} onClick={addSelected}>
                                Add selected ({selectedKeys.size})
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {showCreate && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-40 z-50" onClick={e => { if (e.target === e.currentTarget) setShowCreate(false); }}>
                    <div className={`flex flex-col rounded shadow-2xl overflow-hidden ${theme.mainContentSection}`} style={{ width: 420 }}>
                        <div className={`flex items-center px-4 py-2 border-b ${borderCls}`}>
                            <span className={`text-sm font-semibold ${theme.title} w-1 flex-auto`}>Create &amp; Add Child-Agent</span>
                            <button type="button" className={btn} onClick={() => setShowCreate(false)}><i className="fa-solid fa-xmark" /></button>
                        </div>
                        <div className="px-4 py-3 flex flex-col gap-2">
                            <div className="flex items-center">
                                <label className={lbl}>Agent Code *</label>
                                <input className={inp} value={createCode} onChange={e => setCreateCode(e.target.value)} />
                            </div>
                            <div className="flex items-center">
                                <label className={lbl}>Display Name</label>
                                <input className={inp} value={createName} onChange={e => setCreateName(e.target.value)} />
                            </div>
                            <div className="flex items-center">
                                <label className={lbl}>Execution Mode</label>
                                <select className={inp} value={createMode} onChange={e => setCreateMode(e.target.value)}>
                                    <option value="Deterministic">Deterministic</option>
                                    <option value="Interactive">Interactive</option>
                                </select>
                            </div>
                            <p className={`text-xs opacity-60 ${theme.label}`}>Defaults: Active = off (hidden from chat menu). Opens the new agent after create.</p>
                        </div>
                        <div className={`flex justify-end gap-2 px-4 py-2 border-t ${borderCls}`}>
                            <button type="button" className={btn} onClick={() => setShowCreate(false)}>Cancel</button>
                            <button type="button" className={btn} disabled={busy} onClick={createAndAdd}>Create &amp; Add</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AgentChildAgentTab;
