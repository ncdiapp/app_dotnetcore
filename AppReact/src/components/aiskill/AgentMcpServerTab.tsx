import React, { useEffect, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { agentSkillSetSvc, AppAgentMcpServerDto, McpTestResult } from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';
import McpKeyValueEditor from './McpKeyValueEditor';

interface Props {
    theme: Theme;
    /** MCP servers are owned by a Tool Library; SkillKey holds this LibraryKey. */
    libraryKey: string;
    /** e.g. "Domain › Library" — shown as the full path above the edit form */
    pathPrefix?: string;
}

const emptyMcp = (libraryKey: string): AppAgentMcpServerDto => ({
    McpServerId: 0, SkillKey: libraryKey, ServerName: '', ServerType: 'streamable-http',
    ServerUrl: '', Command: '', IsActive: true,
    BearerTokenEnvVar: '', Headers: '', HeadersFromEnv: '',
});

const AgentMcpServerTab: React.FC<Props> = ({ theme, libraryKey, pathPrefix }) => {
    const dispatch = useDispatch();
    const [mcpCV] = useState(() => new CollectionView<AppAgentMcpServerDto>([]));
    const [selected, setSelected] = useState<AppAgentMcpServerDto | null>(null);
    const [editItem, setEditItem] = useState<AppAgentMcpServerDto>(emptyMcp(libraryKey));
    const [isEditing, setIsEditing] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDelete, setConfirmDelete] = useState(false);
    const [testing, setTesting] = useState(false);
    const [testResult, setTestResult] = useState<McpTestResult | null>(null);
    const [resetKey, setResetKey] = useState(0);
    const isHttp = editItem.ServerType !== 'stdio';

    const load = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllMcpServers();
            mcpCV.sourceCollection = (res.Object ?? []).filter(s => s.SkillKey === libraryKey);
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
        setTestResult(null); setResetKey(k => k + 1);
    };

    const handleTest = async () => {
        if (!editItem.ServerUrl.trim()) { setError('Server URL is required.'); return; }
        setTesting(true); setTestResult(null); setError(null);
        try {
            const res = await agentSkillSetSvc.TestMcpServer(editItem);
            setTestResult(res.Object ?? { Success: false, Message: 'No response from server.', ToolCount: 0, ToolNames: [] });
        } catch (e: unknown) {
            setTestResult({ Success: false, Message: e instanceof Error ? e.message : String(e), ToolCount: 0, ToolNames: [] });
        } finally { setTesting(false); }
    };

    // Stores the server's tool list in the tool catalog so "AI Generate Agent Design" can recommend its tools.
    const handleSyncTools = async () => {
        setTesting(true); setTestResult(null); setError(null);
        try {
            const res = await agentSkillSetSvc.SyncMcpTools(editItem);
            setTestResult(res.Object ?? { Success: false, Message: 'No response from server.', ToolCount: 0, ToolNames: [] });
        } catch (e: unknown) {
            setTestResult({ Success: false, Message: e instanceof Error ? e.message : String(e), ToolCount: 0, ToolNames: [] });
        } finally { setTesting(false); }
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
            setSelected(null); setEditItem(emptyMcp(libraryKey)); setIsEditing(false);
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
                    <button className={btn} onClick={() => { setSelected(null); setEditItem(emptyMcp(libraryKey)); setIsEditing(true); setIsDirty(false); setTestResult(null); setResetKey(k => k + 1); }}>
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
                        {pathPrefix && (
                            <div className={`px-3 py-1 text-xs border-b border-gray-200 ${theme.label}`}>
                                <i className="fa-solid fa-sitemap mr-2 opacity-60" />{pathPrefix} › MCP Servers › <span className="font-semibold">{editItem.ServerName || 'New Server'}</span>
                            </div>
                        )}
                        <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-3">
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
                            {isHttp && (
                                <>
                                    <div className="flex items-center py-1">
                                        <label className={lbl}>Bearer token env var</label>
                                        <input className={inp} value={editItem.BearerTokenEnvVar} onChange={e => update('BearerTokenEnvVar', e.target.value)} autoComplete="off" placeholder="MCP_BEARER_TOKEN (env var name, must start with MCP_)" />
                                    </div>
                                    <McpKeyValueEditor key={`h-${editItem.McpServerId}-${resetKey}`} theme={theme} label="Headers" value={editItem.Headers}
                                        keyPlaceholder="Header name" valuePlaceholder="Value (stored encrypted)" addLabel="Add header" maskValues onChange={v => update('Headers', v)} />
                                    <McpKeyValueEditor key={`e-${editItem.McpServerId}-${resetKey}`} theme={theme} label="Headers from env vars" value={editItem.HeadersFromEnv}
                                        keyPlaceholder="Header name" valuePlaceholder="MCP_… env var name" addLabel="Add variable" onChange={v => update('HeadersFromEnv', v)} />
                                </>
                            )}
                            <div className="flex items-center py-1">
                                <label className={lbl}>Active</label>
                                <input type="checkbox" checked={editItem.IsActive} onChange={e => update('IsActive', e.target.checked)} />
                            </div>
                            {testResult && (
                                <div className={`px-3 py-2 text-xs rounded border ${testResult.Success ? 'text-green-700 bg-green-50 border-green-200' : 'text-red-600 bg-red-50 border-red-200'}`}>
                                    <div>{testResult.Message}</div>
                                    {testResult.Success && testResult.ToolNames.length > 0 && (
                                        <div className="mt-1 break-words">{testResult.ToolNames.join(', ')}</div>
                                    )}
                                </div>
                            )}
                        </div>
                        <div className="flex items-center gap-2 px-3 py-2 border-t border-gray-200">
                            <button className={btn} onClick={handleSave} disabled={!isDirty}><i className="fa-solid fa-floppy-disk mr-1" />Save</button>
                            <button className={btn} onClick={() => { if (selected) { setEditItem({ ...selected }); setIsDirty(false); setTestResult(null); setResetKey(k => k + 1); } else { setIsEditing(false); } }} disabled={!isDirty}>Cancel</button>
                            {isHttp && (
                                <button className={btn} onClick={handleTest} disabled={testing}>
                                    <i className={`fa-solid ${testing ? 'fa-spinner fa-spin' : 'fa-plug'} mr-1`} />Test Connection
                                </button>
                            )}
                            {isHttp && (
                                <button
                                    className={btn}
                                    onClick={handleSyncTools}
                                    disabled={testing || editItem.McpServerId <= 0 || isDirty}
                                    title={editItem.McpServerId <= 0 || isDirty ? 'Save the server first, then sync its tools' : 'Read the tool list from the server into the tool catalog (used by AI agent design)'}
                                >
                                    <i className="fa-solid fa-rotate mr-1" />Sync tools
                                </button>
                            )}
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
