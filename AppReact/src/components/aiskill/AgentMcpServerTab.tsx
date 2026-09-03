import React, { useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { agentSkillSetSvc, AppAgentMcpServerDto } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

interface Props {
    theme: Theme;
}

const emptyMcp = (): AppAgentMcpServerDto => ({
    McpServerId: 0, SkillKey: '', ServerName: '', ServerType: 'streamable-http',
    ServerUrl: '', Command: '', IsActive: true,
});

const AgentMcpServerTab: React.FC<Props> = ({ theme }) => {
    const dispatch = useDispatch();
    const [mcpCV] = useState(() => new CollectionView<AppAgentMcpServerDto>([]));
    const [selected, setSelected] = useState<AppAgentMcpServerDto | null>(null);
    const [editItem, setEditItem] = useState<AppAgentMcpServerDto>(emptyMcp());
    const [isEditing, setIsEditing] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDelete, setConfirmDelete] = useState(false);

    const load = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllMcpServers();
            mcpCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    useEffect(() => { load(); }, []);

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentMcpServerDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentMcpServerDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item); setEditItem({ ...item }); setIsEditing(true); setIsDirty(false);
    };

    const update = (field: keyof AppAgentMcpServerDto, value: unknown) => {
        setEditItem(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

    const handleSave = async () => {
        if (!editItem.ServerName.trim()) { setError('Server Name is required.'); return; }
        if (!editItem.ServerUrl.trim()) { setError('Server URL is required.'); return; }
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertMcpServer(editItem);
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
            await agentSkillSetSvc.DeleteMcpServer(selected.McpServerId);
            setSelected(null); setEditItem(emptyMcp()); setIsEditing(false);
            setConfirmDelete(false); await load();
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-32 text-xs ${theme.label} mr-2`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

    return (
        <div className="w-full h-full flex gap-2 px-2 pb-2 overflow-hidden">
            {error && <div className="absolute top-2 left-2 right-2 px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 rounded z-10">{error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button></div>}
            <div className={`w-56 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                    <button className={btn} onClick={() => { setSelected(null); setEditItem(emptyMcp()); setIsEditing(true); setIsDirty(false); }}>
                        <i className="fa-solid fa-plus mr-1" />New
                    </button>
                    {selected && (
                        <button className={btn} onClick={() => setConfirmDelete(true)}>
                            <i className="fa-solid fa-trash mr-1" />Delete
                        </button>
                    )}
                </div>
                <div className="w-full h-1 flex-auto overflow-hidden">
                    <FlexGrid className="w-full h-full" itemsSource={mcpCV} isReadOnly headersVisibility="Column" selectionChanged={onGridSelectionChanged}>
                        <FlexGridColumn header="Server Name" binding="ServerName" width="*" />
                        <FlexGridColumn header="Type" binding="ServerType" width={90} />
                        <FlexGridColumn header="" binding="" width="*" />
                    </FlexGrid>
                </div>
            </div>
            <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                {isEditing ? (
                    <div className="h-full flex flex-col overflow-hidden">
                        <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-3">
                            <div className="flex items-center py-1">
                                <label className={lbl}>Skill Key</label>
                                <input className={inp} value={editItem.SkillKey} onChange={e => update('SkillKey', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Server Name *</label>
                                <input className={inp} value={editItem.ServerName} onChange={e => update('ServerName', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Server Type</label>
                                <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={editItem.ServerType} onChange={e => update('ServerType', e.target.value)}>
                                    <option value="streamable-http">streamable-http</option>
                                    <option value="stdio">stdio</option>
                                    <option value="sse">sse</option>
                                </select>
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Server URL</label>
                                <input className={inp} value={editItem.ServerUrl} onChange={e => update('ServerUrl', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Command</label>
                                <input className={inp} value={editItem.Command} onChange={e => update('Command', e.target.value)} autoComplete="off" placeholder="stdio command (optional)" />
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
                        <span className={`text-sm ${theme.label}`}>Select an MCP server or click + New</span>
                    </div>
                )}
            </div>
            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>Delete MCP server "{selected?.ServerName}"?</div>
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

export default AgentMcpServerTab;
