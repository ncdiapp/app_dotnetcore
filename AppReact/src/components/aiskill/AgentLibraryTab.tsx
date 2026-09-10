import React, { useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import {
    agentSkillSetSvc,
    AppAgentToolDomainDto,
    AppAgentToolLibraryDto,
} from '../../webapi/agentSkillSetSvc';
import AgentToolRegisterTab from './AgentToolRegisterTab';

const emptyDomain = (): AppAgentToolDomainDto => ({
    DomainKey: '', DomainName: '', Description: '', SortOrder: 0, IsActive: true,
});

const emptyLibrary = (domainKey: string): AppAgentToolLibraryDto => ({
    LibraryKey: '', DomainKey: domainKey, LibraryName: '', Description: '',
    ToolCategory: 'SqlQuery', IsActive: true, ToolCount: 0,
});

type RightMode = 'none' | 'domain' | 'library';

const AgentLibraryTab: React.FC = () => {
    const { theme } = useTheme();
    const dispatch = useDispatch();

    const [domains, setDomains] = useState<AppAgentToolDomainDto[]>([]);
    const [libsCV] = useState(() => new CollectionView<AppAgentToolLibraryDto>([]));
    const [selectedDomain, setSelectedDomain] = useState<AppAgentToolDomainDto | null>(null);
    const [selectedLib, setSelectedLib] = useState<AppAgentToolLibraryDto | null>(null);
    const [editDomain, setEditDomain] = useState<AppAgentToolDomainDto>(emptyDomain());
    const [editLib, setEditLib] = useState<AppAgentToolLibraryDto>(emptyLibrary(''));
    const [rightMode, setRightMode] = useState<RightMode>('none');
    const [domainDirty, setDomainDirty] = useState(false);
    const [libDirty, setLibDirty] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [confirmDeleteDomain, setConfirmDeleteDomain] = useState(false);
    const [confirmDeleteLib, setConfirmDeleteLib] = useState(false);

    const loadDomains = async () => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetAllDomains();
            setDomains(res.Object ?? []);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const loadLibraries = async (domainKey: string) => {
        dispatch(setIsBusy());
        try {
            const res = await agentSkillSetSvc.GetLibrariesByDomain(domainKey);
            libsCV.sourceCollection = res.Object ?? [];
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    useEffect(() => { loadDomains(); }, []);

    const selectDomain = (d: AppAgentToolDomainDto) => {
        setSelectedDomain(d);
        setEditDomain({ ...d });
        setSelectedLib(null);
        setRightMode('domain');
        setDomainDirty(false);
        libsCV.sourceCollection = [];
        loadLibraries(d.DomainKey);
    };

    const onLibSelectionChanged = (s: { control?: { selection?: { row?: number }; rows?: { dataItem: AppAgentToolLibraryDto }[] }; selection?: { row?: number }; rows?: { dataItem: AppAgentToolLibraryDto }[] }) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (!item) return;
        setSelectedLib(item);
        setEditLib({ ...item });
        setRightMode('library');
        setLibDirty(false);
    };

    const updateDomain = (field: keyof AppAgentToolDomainDto, value: unknown) => {
        setEditDomain(prev => ({ ...prev, [field]: value }));
        setDomainDirty(true);
    };

    const updateLib = (field: keyof AppAgentToolLibraryDto, value: unknown) => {
        setEditLib(prev => ({ ...prev, [field]: value }));
        setLibDirty(true);
    };

    const saveDomain = async () => {
        if (!editDomain.DomainKey.trim()) { setError('Domain Key is required.'); return; }
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertDomain(editDomain);
            setDomainDirty(false);
            await loadDomains();
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const deleteDomain = async () => {
        if (!selectedDomain) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteDomain(selectedDomain.DomainKey);
            setSelectedDomain(null); setRightMode('none'); setConfirmDeleteDomain(false);
            libsCV.sourceCollection = [];
            await loadDomains();
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const saveLib = async () => {
        if (!editLib.LibraryKey.trim()) { setError('Library Key is required.'); return; }
        dispatch(setIsBusy()); setError(null);
        try {
            await agentSkillSetSvc.UpsertLibrary(editLib);
            setLibDirty(false);
            if (selectedDomain) await loadLibraries(selectedDomain.DomainKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const deleteLib = async () => {
        if (!selectedLib) return;
        dispatch(setIsBusy());
        try {
            await agentSkillSetSvc.DeleteLibrary(selectedLib.LibraryKey);
            setSelectedLib(null); setRightMode('domain'); setConfirmDeleteLib(false);
            if (selectedDomain) await loadLibraries(selectedDomain.DomainKey);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { dispatch(setIsNotBusy()); }
    };

    const SPLIT_MIN_PX = 120;
    const splitContainerRef = useRef<HTMLDivElement | null>(null);
    const [leftWidthPx, setLeftWidthPx] = useState(110);
    const [midWidthPx, setMidWidthPx] = useState(280);

    const onDivider1MouseDown = (e: React.MouseEvent) => {
        e.preventDefault();
        const startX = e.clientX;
        const startW = leftWidthPx;
        const onMove = (me: MouseEvent) => {
            const w = splitContainerRef.current?.getBoundingClientRect().width ?? 800;
            const max = w - midWidthPx - SPLIT_MIN_PX - 16;
            setLeftWidthPx(Math.max(SPLIT_MIN_PX, Math.min(max, startW + me.clientX - startX)));
        };
        const onUp = () => { document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    };

    const onDivider2MouseDown = (e: React.MouseEvent) => {
        e.preventDefault();
        const startX = e.clientX;
        const startW = midWidthPx;
        const onMove = (me: MouseEvent) => {
            const w = splitContainerRef.current?.getBoundingClientRect().width ?? 800;
            const max = w - leftWidthPx - SPLIT_MIN_PX - 16;
            setMidWidthPx(Math.max(SPLIT_MIN_PX, Math.min(max, startW + me.clientX - startX)));
        };
        const onUp = () => { document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    };

    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-28 text-xs ${theme.label} mr-2`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const borderCls = `border-gray-200`;

    return (
        <div ref={splitContainerRef} className="w-full h-full flex px-2 pb-2 overflow-hidden">
            {/* Left: Domain list */}
            <div className={`shrink-0 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`} style={{ width: leftWidthPx }}>
                <div className={`px-2 py-1 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                    <i className="fa-solid fa-layer-group mr-1 opacity-60" />Domains
                </div>
                <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                    <button className={btn} onClick={() => {
                        setSelectedDomain(null); setEditDomain(emptyDomain()); setRightMode('domain');
                        setDomainDirty(false); libsCV.sourceCollection = [];
                    }}>
                        <i className="fa-solid fa-plus mr-1" />New
                    </button>
                    {selectedDomain && (
                        <button className={btn} onClick={() => setConfirmDeleteDomain(true)}>
                            <i className="fa-solid fa-trash" />
                        </button>
                    )}
                </div>
                <div className="w-full h-1 flex-auto overflow-y-auto py-1">
                    {domains.length === 0 && (
                        <span className={`block text-xs px-2 ${theme.label}`}>No domains yet.</span>
                    )}
                    {domains.map(d => (
                        <button
                            key={d.DomainKey}
                            className={`w-full text-left px-2 py-1.5 text-xs ${selectedDomain?.DomainKey === d.DomainKey ? `font-semibold ${theme.title}` : theme.label} hover:opacity-80`}
                            onClick={() => selectDomain(d)}
                        >
                            <i className="fa-solid fa-folder mr-1 opacity-60" />{d.DomainName || d.DomainKey}
                            {!d.IsActive && <span className="ml-1 opacity-50">(off)</span>}
                        </button>
                    ))}
                </div>
            </div>

            {/* Divider 1 */}
            <div
                role="separator" aria-orientation="vertical" tabIndex={0}
                onMouseDown={onDivider1MouseDown}
                onKeyDown={(e) => {
                    if (e.key === 'ArrowLeft') { e.preventDefault(); setLeftWidthPx(w => Math.max(SPLIT_MIN_PX, w - 16)); }
                    if (e.key === 'ArrowRight') { e.preventDefault(); setLeftWidthPx(w => Math.min(w + 16, (splitContainerRef.current?.getBoundingClientRect().width ?? 800) - midWidthPx - SPLIT_MIN_PX - 16)); }
                }}
                className={`shrink-0 w-px cursor-col-resize border-r self-stretch min-h-0 border-gray-200 hover:border-blue-400 focus:outline-none`}
            />

            {/* Middle: Library list */}
            <div className={`shrink-0 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`} style={{ width: midWidthPx }}>
                <div className={`px-2 py-1 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                    <i className="fa-solid fa-book mr-1 opacity-60" />
                    {selectedDomain ? selectedDomain.DomainName || selectedDomain.DomainKey : 'Libraries'}
                </div>
                <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                    <button
                        className={btn}
                        disabled={!selectedDomain}
                        onClick={() => {
                            if (!selectedDomain) return;
                            setSelectedLib(null);
                            setEditLib(emptyLibrary(selectedDomain.DomainKey));
                            setRightMode('library');
                            setLibDirty(false);
                        }}
                    >
                        <i className="fa-solid fa-plus mr-1" />New
                    </button>
                    {selectedLib && (
                        <button className={btn} onClick={() => setConfirmDeleteLib(true)}>
                            <i className="fa-solid fa-trash mr-1" />Delete
                        </button>
                    )}
                </div>
                <div className="w-full h-1 flex-auto overflow-hidden">
                    {selectedDomain ? (
                        <FlexGrid className="w-full h-full" itemsSource={libsCV} isReadOnly headersVisibility="Column" selectionChanged={onLibSelectionChanged}>
                            <FlexGridColumn header="Library Key" binding="LibraryKey" width="*" />
                            <FlexGridColumn header="Tools" binding="ToolCount" width={50} />
                            <FlexGridColumn header="Active" binding="IsActive" width={55} />
                            <FlexGridColumn header="" binding="" width="*" />
                        </FlexGrid>
                    ) : (
                        <div className="flex items-center justify-center h-full">
                            <span className={`text-xs ${theme.label}`}>Select a domain first</span>
                        </div>
                    )}
                </div>
            </div>

            {/* Divider 2 */}
            <div
                role="separator" aria-orientation="vertical" tabIndex={0}
                onMouseDown={onDivider2MouseDown}
                onKeyDown={(e) => {
                    if (e.key === 'ArrowLeft') { e.preventDefault(); setMidWidthPx(w => Math.max(SPLIT_MIN_PX, w - 16)); }
                    if (e.key === 'ArrowRight') { e.preventDefault(); setMidWidthPx(w => Math.min(w + 16, (splitContainerRef.current?.getBoundingClientRect().width ?? 800) - leftWidthPx - SPLIT_MIN_PX - 16)); }
                }}
                className={`shrink-0 w-px cursor-col-resize border-r self-stretch min-h-0 border-gray-200 hover:border-blue-400 focus:outline-none`}
            />

            {/* Right: Context panel */}
            <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                {error && <div className="px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 mx-2 mt-1 rounded">{error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button></div>}

                {rightMode === 'none' && (
                    <div className="h-full flex items-center justify-center">
                        <span className={`text-sm ${theme.label}`}>Select a domain or library, or click + New</span>
                    </div>
                )}

                {rightMode === 'domain' && (
                    <div className="h-full flex flex-col overflow-hidden">
                        <div className={`px-3 py-1.5 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                            <i className="fa-solid fa-folder mr-1" />{editDomain.DomainKey || 'New Domain'}
                        </div>
                        <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-3">
                            <div className="flex items-center py-1">
                                <label className={lbl}>Domain Key *</label>
                                <input className={inp} value={editDomain.DomainKey} onChange={e => updateDomain('DomainKey', e.target.value)} placeholder="e.g. shopify-apis" autoComplete="off" />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Domain Name</label>
                                <input className={inp} value={editDomain.DomainName} onChange={e => updateDomain('DomainName', e.target.value)} autoComplete="off" />
                            </div>
                            <div className="flex items-start py-1">
                                <label className={`${lbl} mt-1`}>Description</label>
                                <textarea className={`flex-auto w-32 px-2 py-1 text-xs border ${theme.inputBox}`} rows={3} value={editDomain.Description} onChange={e => updateDomain('Description', e.target.value)} />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Sort Order</label>
                                <input className="w-20 h-7 px-2 text-xs border" type="number" value={editDomain.SortOrder} onChange={e => updateDomain('SortOrder', parseInt(e.target.value) || 0)} />
                            </div>
                            <div className="flex items-center py-1">
                                <label className={lbl}>Active</label>
                                <input type="checkbox" checked={editDomain.IsActive} onChange={e => updateDomain('IsActive', e.target.checked)} />
                            </div>
                        </div>
                        <div className="flex items-center gap-2 px-3 py-2 border-t border-gray-200">
                            <button className={btn} onClick={saveDomain}><i className="fa-solid fa-floppy-disk mr-1" />Save</button>
                            <button className={btn} onClick={() => { if (selectedDomain) { setEditDomain({ ...selectedDomain }); setDomainDirty(false); } else { setRightMode('none'); } }} disabled={!domainDirty}>Cancel</button>
                            {domainDirty && <span className="text-xs text-orange-500 ml-2">Unsaved changes</span>}
                        </div>
                    </div>
                )}

                {rightMode === 'library' && (
                    <div className="h-full flex flex-col overflow-hidden">
                        {/* Library form header */}
                        <div className={`px-3 py-1.5 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                            <i className="fa-solid fa-book mr-1" />{editLib.LibraryKey || 'New Library'}
                        </div>
                        {/* Scrollable area: library form + tools panel */}
                        <div className="w-full h-1 flex-auto flex flex-col overflow-hidden">
                            {/* Library form — fixed height */}
                            <div className="flex-none p-3 flex flex-col gap-2 border-b border-gray-200">
                                <div className="flex items-center">
                                    <label className={lbl}>Library Key *</label>
                                    <input className={inp} value={editLib.LibraryKey} onChange={e => updateLib('LibraryKey', e.target.value)} placeholder="e.g. shopify-products" autoComplete="off" />
                                </div>
                                <div className="flex items-center">
                                    <label className={lbl}>Domain</label>
                                    <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={editLib.DomainKey} onChange={e => updateLib('DomainKey', e.target.value)}>
                                        {domains.map(d => <option key={d.DomainKey} value={d.DomainKey}>{d.DomainName || d.DomainKey}</option>)}
                                    </select>
                                </div>
                                <div className="flex items-center">
                                    <label className={lbl}>Library Name</label>
                                    <input className={inp} value={editLib.LibraryName} onChange={e => updateLib('LibraryName', e.target.value)} autoComplete="off" />
                                </div>
                                <div className="flex items-center">
                                    <label className={lbl}>Tool Category</label>
                                    <select className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} value={editLib.ToolCategory} onChange={e => updateLib('ToolCategory', e.target.value)}>
                                        <option value="SqlQuery">SQL Query</option>
                                        <option value="HttpRest">HTTP REST</option>
                                        <option value="BuiltIn">Built-in</option>
                                        <option value="Mixed">Mixed</option>
                                    </select>
                                </div>
                                <div className="flex items-center">
                                    <label className={lbl}>Active</label>
                                    <input type="checkbox" checked={editLib.IsActive} onChange={e => updateLib('IsActive', e.target.checked)} />
                                </div>
                                <div className="flex gap-2 pt-1">
                                    <button className={btn} onClick={saveLib}><i className="fa-solid fa-floppy-disk mr-1" />Save Library</button>
                                    <button className={btn} onClick={() => { if (selectedLib) { setEditLib({ ...selectedLib }); setLibDirty(false); } else { setRightMode(selectedDomain ? 'domain' : 'none'); } }} disabled={!libDirty}>Cancel</button>
                                    {libDirty && <span className="text-xs text-orange-500 self-center">Unsaved</span>}
                                </div>
                            </div>

                            {/* Tool list — only shown when library key is saved (i.e. selectedLib exists) */}
                            <div className="w-full h-1 flex-auto flex flex-col overflow-hidden">
                                {selectedLib?.LibraryKey ? (
                                    <>
                                        <div className={`flex-none px-3 py-1 text-xs font-semibold border-b border-gray-200 ${theme.title}`}>
                                            <i className="fa-solid fa-key mr-1 opacity-60" />Tools in {selectedLib.LibraryKey}
                                        </div>
                                        <div className="w-full h-1 flex-auto overflow-hidden">
                                            <AgentToolRegisterTab selectedSkillKey={selectedLib.LibraryKey} theme={theme} />
                                        </div>
                                    </>
                                ) : (
                                    <div className="h-full flex items-center justify-center">
                                        <span className={`text-xs ${theme.label}`}>Save the library first to manage its tools</span>
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                )}
            </div>

            {/* Delete domain confirmation */}
            {confirmDeleteDomain && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Delete Domain</div>
                        <div className={`text-xs ${theme.label}`}>Delete domain "{selectedDomain?.DomainKey}"? This will not delete its libraries.</div>
                        <div className="flex gap-2">
                            <button className={btn} onClick={deleteDomain}>Delete</button>
                            <button className={btn} onClick={() => setConfirmDeleteDomain(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}

            {/* Delete library confirmation */}
            {confirmDeleteLib && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Delete Library</div>
                        <div className={`text-xs ${theme.label}`}>Delete library "{selectedLib?.LibraryKey}"? All tools in this library will also be deleted.</div>
                        <div className="flex gap-2">
                            <button className={btn} onClick={deleteLib}>Delete</button>
                            <button className={btn} onClick={() => setConfirmDeleteLib(false)}>Cancel</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AgentLibraryTab;
