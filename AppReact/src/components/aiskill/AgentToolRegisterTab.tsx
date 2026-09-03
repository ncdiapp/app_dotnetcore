import React, { useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { agentSkillSetSvc, AppAgentToolRegisterDto } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

interface Props {
    selectedSkillKey: string | null;
    theme: Theme;
}

const emptyTool = (skillKey: string): AppAgentToolRegisterDto => ({
    Id: 0, SkillKey: skillKey, ToolName: '', Description: '',
    ToolType: 'BuiltIn', ToolConfig: '{}', IsActive: true, SortOrder: 0,
});

const AgentToolRegisterTab: React.FC<Props> = ({ selectedSkillKey, theme }) => {
    const dispatch = useDispatch();
    const [toolsCV] = useState(() => new CollectionView<AppAgentToolRegisterDto>([]));
    const [selected, setSelected] = useState<AppAgentToolRegisterDto | null>(null);
    const [editItem, setEditItem] = useState<AppAgentToolRegisterDto>(emptyTool(''));
    const [isEditing, setIsEditing] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDelete, setConfirmDelete] = useState(false);

    const load = async (skillKey: string) => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetToolsBySkillKey(skillKey);
            toolsCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    useEffect(() => {
        if (selectedSkillKey) {
            setSelected(null); setIsEditing(false); setIsDirty(false);
            toolsCV.sourceCollection = [];
            load(selectedSkillKey);
        }
    }, [selectedSkillKey]);

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentToolRegisterDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentToolRegisterDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item); setEditItem({ ...item }); setIsEditing(true); setIsDirty(false);
    };

    const update = (field: keyof AppAgentToolRegisterDto, value: unknown) => {
        setEditItem(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

    const handleSave = async () => {
        if (!editItem.ToolName.trim()) { setError('Tool Name is required.'); return; }
        dispatch(setIsBusy()); setError(null);
        try {
            const payload = { ...editItem, SkillKey: selectedSkillKey ?? editItem.SkillKey };
            await agentSkillSetSvc.UpsertTool(payload);
            setIsDirty(false);
            if (selectedSkillKey) await load(selectedSkillKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleDelete = async () => {
        if (!selected) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteTool(selected.Id);
            setSelected(null); setEditItem(emptyTool(selectedSkillKey ?? '')); setIsEditing(false);
            setConfirmDelete(false);
            if (selectedSkillKey) await load(selectedSkillKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-32 text-xs ${theme.label} mr-2`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

    if (!selectedSkillKey) {
        return (
            <div className="w-full h-full flex items-center justify-center">
                <span className={`text-sm ${theme.label}`}>Select a skill set from the Skill Sets tab first.</span>
            </div>
        );
    }

    return (
        <div className="w-full h-full flex gap-2 px-2 pb-2 overflow-hidden">
            <div className={`w-56 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                <div className={`px-2 py-1 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                    <i className="fa-solid fa-key mr-1 opacity-60" />{selectedSkillKey}
                </div>
                <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                    <button className={btn} onClick={() => { setSelected(null); setEditItem(emptyTool(selectedSkillKey)); setIsEditing(true); setIsDirty(false); }}>
                        <i className="fa-solid fa-plus mr-1" />New
                    </button>
                    {selected && (
                        <button className={btn} onClick={() => setConfirmDelete(true)}>
                            <i className="fa-solid fa-trash mr-1" />Delete
                        </button>
                    )}
                </div>
                <div className="w-full h-1 flex-auto overflow-hidden">
                    <FlexGrid className="w-full h-full" itemsSource={toolsCV} isReadOnly headersVisibility="Column" selectionChanged={onGridSelectionChanged}>
                        <FlexGridColumn header="Tool Name" binding="ToolName" width="*" />
                        <FlexGridColumn header="Type" binding="ToolType" width={70} />
                        <FlexGridColumn header="" binding="" width="*" />
                    </FlexGrid>
                </div>
            </div>
            <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                {isEditing ? (
                    <div className="h-full flex flex-col overflow-hidden">
                        {error && <div className="px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 mx-2 mt-1 rounded">{error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button></div>}
                        <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-3">
                            <div className="flex items-center py-1">
                                <label className={lbl}>Tool Name *</label>
                                <input className={inp} value={editItem.ToolName} onChange={e => update('ToolName', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Description</label>
                                <input className={inp} value={editItem.Description} onChange={e => update('Description', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Tool Type</label>
                                <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={editItem.ToolType} onChange={e => update('ToolType', e.target.value)}>
                                    <option value="BuiltIn">BuiltIn</option>
                                    <option value="Plugin">Plugin</option>
                                    <option value="External">External</option>
                                </select>
                            </div>
                            <div className="flex items-start py-1">
                                <label className={`${lbl} mt-1`}>Tool Config (JSON)</label>
                                <textarea className={`flex-auto w-32 px-2 py-1 text-xs border font-mono ${theme.inputBox}`} rows={6} value={editItem.ToolConfig} onChange={e => update('ToolConfig', e.target.value)} />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Sort Order</label>
                                <input className={`w-20 h-7 px-2 text-xs border ${theme.inputBox}`} type="number" value={editItem.SortOrder} onChange={e => update('SortOrder', parseInt(e.target.value) || 0)} />
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
                        <span className={`text-sm ${theme.label}`}>Select a tool or click + New</span>
                    </div>
                )}
            </div>
            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>Delete tool "{selected?.ToolName}"?</div>
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

export default AgentToolRegisterTab;
