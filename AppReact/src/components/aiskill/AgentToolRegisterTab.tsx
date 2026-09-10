import React, { useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import {
    agentSkillSetSvc, AppAgentToolRegisterDto, LibraryToolPreviewDto,
} from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';
import { endpoints } from '../../webapi/endpoints';
import { getHeaders } from '../../helper/apiServiceHelper';

interface Props {
    selectedSkillKey: string | null;
    theme: Theme;
    hideHeader?: boolean;
}

const TOOL_CONFIG_TEMPLATES: Record<string, string> = {
    BuiltIn:      '{\n  "TypeName": "",\n  "MethodName": ""\n}',
    SqlQuery:     '{\n  "SqlBody": "SELECT TOP 50 Col1, Col2 FROM dbo.YourTable WHERE Col1 = @param1",\n  "ReturnType": "json"\n}',
    HttpRest:     '{\n  "Url": "https://api.example.com/endpoint/{param1}",\n  "Method": "GET",\n  "Headers": {\n    "Authorization": "Bearer YOUR_TOKEN_HERE"\n  }\n}',
    DynamicCSharp:'{\n  "ScriptBody": "// Write C# here. Return a string.\\nreturn \\"result\\";",\n  "AllowedNamespaces": ["System", "System.Linq", "System.Collections.Generic"],\n  "TimeoutSeconds": 10\n}',
    ExternalDll:  '{\n  "AssemblyPath": "plugins/MyPlugin.dll",\n  "TypeName": "MyPlugin.MyClass",\n  "MethodName": "Execute"\n}',
    PowerShell:   '{\n  "ScriptPath": "scripts/myscript.ps1",\n  "TimeoutSeconds": 30\n}',
};

const emptyTool = (skillKey: string): AppAgentToolRegisterDto => ({
    Id: 0, SkillKey: skillKey, ToolName: '', Description: '',
    ToolType: 'BuiltIn', ToolConfig: TOOL_CONFIG_TEMPLATES['BuiltIn'], IsActive: true, SortOrder: 0,
});

interface TableInfo { name: string; schema: string; }

const AgentToolRegisterTab: React.FC<Props> = ({ selectedSkillKey, theme, hideHeader = false }) => {
    const dispatch = useDispatch();
    const [toolsCV] = useState(() => new CollectionView<AppAgentToolRegisterDto>([]));
    const suppressSelectRef = useRef(false);
    const [selected, setSelected] = useState<AppAgentToolRegisterDto | null>(null);
    const [editItem, setEditItem] = useState<AppAgentToolRegisterDto>(emptyTool(''));
    const [showModal, setShowModal] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDelete, setConfirmDelete] = useState(false);

    // BuiltIn picker
    const [builtInTools, setBuiltInTools] = useState<LibraryToolPreviewDto[]>([]);
    const [builtInFilter, setBuiltInFilter] = useState('');

    // Schema browser for SqlQuery
    const [schemaTables, setSchemaTables] = useState<TableInfo[]>([]);
    const [schemaOpen, setSchemaOpen] = useState(false);
    const [schemaFilter, setSchemaFilter] = useState('');
    const [schemaLoaded, setSchemaLoaded] = useState(false);

    const load = async (skillKey: string) => {
        suppressSelectRef.current = true;
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetToolsBySkillKey(skillKey);
            toolsCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally {
            dispatch(setIsNotBusy());
            // Clear suppress after Wijmo has had time to fire its auto-selection event
            setTimeout(() => { suppressSelectRef.current = false; }, 150);
        }
    };

    const loadBuiltInTools = async () => {
        if (builtInTools.length > 0) return;
        try {
            const res = await agentSkillSetSvc.GetAvailableBuiltInTools();
            setBuiltInTools(res.Object ?? []);
        } catch { /* non-critical */ }
    };

    const loadSchema = async () => {
        if (schemaLoaded) return;
        try {
            const res = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetDataSourceTableAndViewList?dataSourceRegisterId=&saasFilterOption=&filterByApplicationId=`, { headers: getHeaders() });
            if (!res.ok) return;
            const data = await res.json();
            const raw: Array<{ Name?: string; SchemaOwner?: string }> = data.Object ?? data ?? [];
            setSchemaTables(raw.map(t => ({ name: t.Name ?? '', schema: t.SchemaOwner ?? 'dbo' })).filter(t => t.name));
            setSchemaLoaded(true);
        } catch { /* non-critical */ }
    };

    useEffect(() => {
        if (selectedSkillKey) {
            setSelected(null); setShowModal(false); setIsDirty(false);
            toolsCV.sourceCollection = [];
            load(selectedSkillKey);
        }
    }, [selectedSkillKey]);

    useEffect(() => {
        if (editItem.ToolType === 'BuiltIn') loadBuiltInTools();
        if (editItem.ToolType === 'SqlQuery' && schemaOpen) loadSchema();
    }, [editItem.ToolType, schemaOpen]);

    const openModal = (tool: AppAgentToolRegisterDto) => {
        setEditItem({ ...tool });
        setIsDirty(false);
        setSchemaOpen(false);
        setSchemaFilter('');
        setBuiltInFilter('');
        setShowModal(true);
    };

    const onGridSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentToolRegisterDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentToolRegisterDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelected(item);
        // Only open modal for genuine user clicks, not programmatic selection after data load
        if (!suppressSelectRef.current) openModal(item);
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
            setShowModal(false);
            if (selectedSkillKey) await load(selectedSkillKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const handleDelete = async () => {
        if (!selected) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteTool(selected.Id);
            setSelected(null); setShowModal(false); setConfirmDelete(false);
            if (selectedSkillKey) await load(selectedSkillKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const insertAtCursor = (textareaId: string, text: string) => {
        const el = document.getElementById(textareaId) as HTMLTextAreaElement;
        if (!el) return;
        const start = el.selectionStart ?? el.value.length;
        const before = el.value.substring(0, start);
        const after  = el.value.substring(el.selectionEnd ?? start);
        const newVal = before + text + after;
        update('ToolConfig', newVal);
        setTimeout(() => { el.selectionStart = el.selectionEnd = start + text.length; el.focus(); }, 0);
    };

    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-32 text-xs ${theme.label} mr-2`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const btnSm = `px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`;

    const filteredBuiltIn = builtInTools.filter(t =>
        !builtInFilter || t.ToolName.toLowerCase().includes(builtInFilter.toLowerCase())
            || t.ToolDescription.toLowerCase().includes(builtInFilter.toLowerCase()));

    const filteredSchema = schemaTables.filter(t =>
        !schemaFilter || t.name.toLowerCase().includes(schemaFilter.toLowerCase()));

    const wordCount = editItem.Description?.trim().split(/\s+/).filter(Boolean).length ?? 0;

    if (!selectedSkillKey) {
        return (
            <div className="w-full h-full flex items-center justify-center">
                <span className={`text-sm ${theme.label}`}>Select a skill set from the Skill Sets tab first.</span>
            </div>
        );
    }

    return (
        <div className="w-full h-full flex flex-col overflow-hidden px-2 pb-2">
            {/* Tool list */}
            <div className={`w-full h-full flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                {!hideHeader && (
                    <div className={`px-2 py-1 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                        <i className="fa-solid fa-key mr-1 opacity-60" />{selectedSkillKey}
                    </div>
                )}
                <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                    <button className={btn} onClick={() => openModal(emptyTool(selectedSkillKey))}>
                        <i className="fa-solid fa-plus mr-1" />New
                    </button>
                    {selected && (<>
                        <button className={btn} onClick={() => openModal(selected)}>
                            <i className="fa-solid fa-pencil mr-1" />Edit
                        </button>
                        <button className={btn} onClick={() => setConfirmDelete(true)}>
                            <i className="fa-solid fa-trash" />
                        </button>
                    </>)}
                </div>
                <div className="w-full h-1 flex-auto overflow-hidden">
                    <FlexGrid className="w-full h-full" itemsSource={toolsCV} isReadOnly headersVisibility="Column" selectionChanged={onGridSelectionChanged}>
                        <FlexGridColumn header="Tool Name" binding="ToolName" width="*" />
                        <FlexGridColumn header="Type" binding="ToolType" width={100} />
                        <FlexGridColumn header="" binding="" width="*" />
                    </FlexGrid>
                </div>
            </div>

            {/* Tool add / edit modal */}
            {showModal && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50" onClick={() => !isDirty && setShowModal(false)}>
                    <div className={`rounded-lg shadow-xl ${theme.mainContentSection} flex flex-col`} style={{ width: 800, height: '85vh', minWidth: 480, minHeight: 400, maxWidth: '95vw', maxHeight: '95vh', resize: 'both', overflow: 'hidden' }} onClick={e => e.stopPropagation()}>
                        <div className={`px-4 py-3 text-sm font-semibold border-b border-gray-200 ${theme.title} flex items-center justify-between shrink-0`}>
                            <span><i className="fa-solid fa-key mr-2 opacity-70" />{editItem.Id ? `Edit: ${editItem.ToolName}` : 'New Tool'}</span>
                            <button className="opacity-50 hover:opacity-100 text-lg leading-none" onClick={() => setShowModal(false)}>×</button>
                        </div>

                        {error && <div className="mx-4 mt-3 px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 rounded shrink-0">{error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button></div>}

                        <div className="overflow-auto p-4 flex flex-col gap-3">

                            {/* Tool Name */}
                            <div className="flex items-center">
                                <label className={lbl}>Tool Name *</label>
                                <input className={inp} value={editItem.ToolName} onChange={e => update('ToolName', e.target.value)} autoComplete="off" placeholder="e.g. get_open_orders" autoFocus />
                            </div>

                            {/* Description */}
                            <div className="flex items-start">
                                <label className={`${lbl} mt-1`}>Description</label>
                                <div className="flex flex-col flex-auto w-32 gap-0.5">
                                    <textarea
                                        className={`w-full px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none`}
                                        rows={3}
                                        placeholder="Call this tool when [trigger]. Returns [data]. Use it when the user [intent]."
                                        value={editItem.Description}
                                        onChange={e => update('Description', e.target.value)}
                                    />
                                    <span className={`text-xs ${wordCount < 15 ? 'text-orange-500' : 'text-green-600'}`}>
                                        {wordCount} words{wordCount < 15 ? ' — aim for 15+ for reliable tool selection' : ''}
                                    </span>
                                </div>
                            </div>

                            {/* Tool Type */}
                            <div className="flex items-center">
                                <label className={lbl}>Tool Type</label>
                                <select
                                    className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                                    value={editItem.ToolType}
                                    onChange={e => {
                                        const t = e.target.value;
                                        setEditItem(prev => ({ ...prev, ToolType: t, ToolConfig: TOOL_CONFIG_TEMPLATES[t] ?? '{}' }));
                                        setIsDirty(true);
                                        setSchemaOpen(false);
                                    }}
                                >
                                    <option value="BuiltIn">Built-in (C# plugin)</option>
                                    <option value="SqlQuery">SQL Query</option>
                                    <option value="HttpRest">HTTP REST</option>
                                    <option value="DynamicCSharp">Dynamic C# Script</option>
                                    <option value="ExternalDll">External DLL</option>
                                    <option value="PowerShell">PowerShell</option>
                                </select>
                            </div>

                            {/* BuiltIn picker */}
                            {editItem.ToolType === 'BuiltIn' && (
                                <div className={`border rounded p-2 flex flex-col gap-1 ${theme.mainContentSection}`}>
                                    <div className={`text-xs font-semibold ${theme.title} mb-1`}>
                                        <i className="fa-solid fa-puzzle-piece mr-1" />Pick Built-in Method
                                    </div>
                                    <input className={`${inp} mb-1`} placeholder="Search methods..." value={builtInFilter} onChange={e => setBuiltInFilter(e.target.value)} />
                                    <div className="max-h-40 overflow-y-auto flex flex-col gap-0.5">
                                        {filteredBuiltIn.length === 0 && <span className={`text-xs ${theme.label}`}>No built-in methods found.</span>}
                                        {filteredBuiltIn.map(t => (
                                            <button key={t.ToolName} className={`text-left px-2 py-1 rounded text-xs hover:opacity-80 ${theme.button_default}`}
                                                onClick={() => {
                                                    setEditItem(prev => ({ ...prev, ToolName: t.ToolName, Description: t.ToolDescription, ToolConfig: t.ToolConfig || TOOL_CONFIG_TEMPLATES['BuiltIn'] }));
                                                    setIsDirty(true); setBuiltInFilter('');
                                                }}>
                                                <span className="font-mono font-semibold">{t.ToolName}</span>
                                                {t.ToolDescription && <span className={`ml-2 ${theme.label}`}>— {t.ToolDescription.substring(0, 80)}</span>}
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            )}

                            {/* Tool Config */}
                            <div className="flex items-start">
                                <label className={`${lbl} mt-1`}>Tool Config (JSON)</label>
                                <textarea
                                    id="tool-config-textarea"
                                    className={`flex-auto w-32 px-2 py-1 text-xs border font-mono ${theme.inputBox}`}
                                    rows={6}
                                    value={editItem.ToolConfig}
                                    onChange={e => update('ToolConfig', e.target.value)}
                                />
                            </div>

                            {/* Schema browser */}
                            {editItem.ToolType === 'SqlQuery' && (
                                <div className={`border rounded ${theme.mainContentSection}`}>
                                    <button className={`w-full text-left px-3 py-1.5 text-xs font-semibold flex items-center gap-1 ${theme.title}`}
                                        onClick={() => { setSchemaOpen(v => !v); if (!schemaLoaded) loadSchema(); }}>
                                        <i className={`fa-solid fa-chevron-${schemaOpen ? 'down' : 'right'} opacity-60`} />
                                        DB Schema Browser
                                        <span className={`ml-1 font-normal ${theme.label}`}>(click table name → inserts into SQL)</span>
                                    </button>
                                    {schemaOpen && (
                                        <div className="px-2 pb-2 flex flex-col gap-1">
                                            <input className={`${inp} my-1`} placeholder="Filter tables..." value={schemaFilter} onChange={e => setSchemaFilter(e.target.value)} />
                                            <div className="max-h-48 overflow-y-auto flex flex-wrap gap-1">
                                                {filteredSchema.length === 0 && <span className={`text-xs ${theme.label}`}>{schemaLoaded ? 'No tables found.' : 'Loading schema…'}</span>}
                                                {filteredSchema.map(t => (
                                                    <button key={t.name} className={`${btnSm} font-mono`} title={`Insert ${t.schema}.${t.name}`}
                                                        onClick={() => insertAtCursor('tool-config-textarea', `${t.schema}.${t.name}`)}>
                                                        {t.name}
                                                    </button>
                                                ))}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* Sort Order + Active */}
                            <div className="flex items-center">
                                <label className={lbl}>Sort Order</label>
                                <input className={`w-20 h-7 px-2 text-xs border ${theme.inputBox}`} type="number" value={editItem.SortOrder} onChange={e => update('SortOrder', parseInt(e.target.value) || 0)} />
                            </div>
                            <div className="flex items-center">
                                <label className={lbl}>Active</label>
                                <input type="checkbox" checked={editItem.IsActive} onChange={e => update('IsActive', e.target.checked)} />
                            </div>
                        </div>

                        <div className="flex items-center gap-2 px-4 py-3 border-t border-gray-200 shrink-0">
                            <button className={btn} onClick={handleSave}><i className="fa-solid fa-floppy-disk mr-1" />Save</button>
                            <button className={btn} onClick={() => setShowModal(false)}>Cancel</button>
                            {isDirty && <span className="text-xs text-orange-500 ml-2">Unsaved changes</span>}
                        </div>
                    </div>
                </div>
            )}

            {/* Delete confirmation */}
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
