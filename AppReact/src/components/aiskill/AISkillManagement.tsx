import React, { useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { aiSkillSvc, AppAISkillDto, AppAISkillRefDto } from '../../webapi/aiSkillSvc';
import { adminSvc } from '../../webapi/adminsvc';
import appHelper from '../../helper/appHelper';

const emptySkill = (): AppAISkillDto => ({
    SkillId: 0,
    Name: '',
    Description: '',
    SkillContent: '',
    IsActive: true,
    CreatedDate: '',
    References: []
});

const emptyRef = (skillId: number): AppAISkillRefDto => ({
    RefId: 0,
    SkillId: skillId,
    FileName: '',
    FileContent: '',
    SortOrder: 0,
    CreatedDate: ''
});

type Tab = 'general' | 'content' | 'refs';

const AISkillManagement: React.FC = () => {
    const { theme } = useTheme();
    const dispatch = useDispatch();

    const [dataSources, setDataSources] = useState<any[]>([]);
    const [dataSourceId, setDataSourceId] = useState<number | null>(null);
    const dataSourceIdRef = useRef<number | null>(null);

    const [skillsCV] = useState(() => new CollectionView<AppAISkillDto>([]));
    const [selectedSkill, setSelectedSkill] = useState<AppAISkillDto | null>(null);
    const [editSkill, setEditSkill] = useState<AppAISkillDto>(emptySkill());
    const [isEditing, setIsEditing] = useState(false);
    const [activeTab, setActiveTab] = useState<Tab>('general');
    const [isDirty, setIsDirty] = useState(false);
    const [activeRef, setActiveRef] = useState<AppAISkillRefDto | null>(null);
    const [showRefEditor, setShowRefEditor] = useState(false);
    const [confirmDelete, setConfirmDelete] = useState<{ type: 'skill' | 'ref'; id: number } | null>(null);
    const [previewContent, setPreviewContent] = useState(false);
    const [previewRefContent, setPreviewRefContent] = useState(false);
    const [error, setError] = useState<string | null>(null);

    // --- Load data sources and auto-select default ---
    useEffect(() => {
        adminSvc.getDataSourceRegisterList(false).then(dsResult => {
            const list: any[] = dsResult?.ObjectList ?? (Array.isArray(dsResult) ? dsResult : []);
            setDataSources(list);
            if (list.length === 0) return;

            // Try to get default datasource ID; fall back to first in list
            aiSkillSvc.GetDefaultDataSourceId().then(defaultResult => {
                const defaultId = defaultResult?.Object;
                if (defaultId && list.some((ds: any) => (ds.Id ?? ds.DataSourceRegisterId) === defaultId)) {
                    setDataSourceId(defaultId);
                } else {
                    setDataSourceId(list[0].Id ?? list[0].DataSourceRegisterId);
                }
            }).catch(() => {
                setDataSourceId(list[0].Id ?? list[0].DataSourceRegisterId);
            });
        }).catch(() => {});
    }, []);

    // Keep ref in sync so grid callbacks always see the current value
    useEffect(() => { dataSourceIdRef.current = dataSourceId; }, [dataSourceId]);

    // --- Load skill list ---
    const loadSkills = async (dsId: number) => {
        dispatch(setIsBusy());
        setError(null);
        try {
            const res = await aiSkillSvc.GetAll(dsId);
            skillsCV.sourceCollection = res.Object ?? [];
            skillsCV.refresh();
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    useEffect(() => {
        if (dataSourceId) {
            setSelectedSkill(null);
            setEditSkill(emptySkill());
            setIsDirty(false);
            setIsEditing(false);
            skillsCV.sourceCollection = [];
            skillsCV.refresh();
            loadSkills(dataSourceId);
        }
    }, [dataSourceId]);

    // --- Grid row click ---
    const onGridSelectionChanged = async (s: any) => {
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item: AppAISkillDto = flex.rows[row]?.dataItem;
        if (!item) return;

        // Use ref to avoid stale closure — Wijmo fires selectionChanged when data
        // loads, which can race with the async setDataSourceId state update.
        const currentDataSourceId = dataSourceIdRef.current;
        if (!currentDataSourceId) return;

        dispatch(setIsBusy());
        try {
            const res = await aiSkillSvc.GetById(currentDataSourceId, item.SkillId);
            const skill = res.Object;
            setSelectedSkill(skill);
            setEditSkill({ ...skill, References: [...(skill.References ?? [])] });
            setIsDirty(false);
            setIsEditing(true);
            setActiveTab('general');
            setActiveRef(null);
            setShowRefEditor(false);
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // --- New skill ---
    const handleNewSkill = () => {
        appHelper.debugLog('[AISkill] handleNewSkill fired, dataSourceId:', dataSourceId, 'isEditing:', isEditing);
        if (!dataSourceId) { appHelper.debugLog('[AISkill] BLOCKED: dataSourceId is falsy'); return; }
        setSelectedSkill(null);
        setEditSkill(emptySkill());
        setIsDirty(false);
        setIsEditing(true);
        appHelper.debugLog('[AISkill] setIsEditing(true) called');
        setActiveTab('general');
        setActiveRef(null);
        setShowRefEditor(false);
    };

    // --- Field change helpers ---
    const updateField = (field: keyof AppAISkillDto, value: any) => {
        setEditSkill(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

    // --- Save ---
    const handleSave = async () => {
        if (!editSkill.Name.trim()) { setError('Name is required.'); return; }
        if (!dataSourceId) { setError('Please select a data source.'); return; }
        dispatch(setIsBusy());
        setError(null);
        try {
            let skillId = editSkill.SkillId;

            if (skillId === 0) {
                const res = await aiSkillSvc.Create(dataSourceId, editSkill);
                skillId = res.Object;
            } else {
                await aiSkillSvc.Update(dataSourceId, editSkill);
            }

            // Save refs
            for (const ref of editSkill.References) {
                const refWithId = { ...ref, SkillId: skillId };
                if (ref.RefId === 0) {
                    await aiSkillSvc.CreateRef(dataSourceId, refWithId);
                } else {
                    await aiSkillSvc.UpdateRef(dataSourceId, refWithId);
                }
            }

            setIsDirty(false);
            await loadSkills(dataSourceId);

            // Reload the saved skill to get fresh data
            const fresh = await aiSkillSvc.GetById(dataSourceId, skillId);
            setSelectedSkill(fresh.Object);
            setEditSkill({ ...fresh.Object, References: [...(fresh.Object.References ?? [])] });
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    // --- Cancel ---
    const handleCancel = () => {
        if (selectedSkill) {
            setEditSkill({ ...selectedSkill, References: [...(selectedSkill.References ?? [])] });
            setIsDirty(false);
        } else {
            setEditSkill(emptySkill());
            setIsDirty(false);
            setIsEditing(false);
        }
        setActiveRef(null);
        setShowRefEditor(false);
    };

    // --- Delete skill ---
    const handleDeleteSkill = (skillId: number) => {
        setConfirmDelete({ type: 'skill', id: skillId });
    };

    const handleDeleteRef = (refId: number) => {
        setConfirmDelete({ type: 'ref', id: refId });
    };

    const handleConfirmDelete = async () => {
        if (!confirmDelete) return;
        dispatch(setIsBusy());
        try {
            if (confirmDelete.type === 'skill') {
                await aiSkillSvc.Delete(dataSourceId!, confirmDelete.id);
                setSelectedSkill(null);
                setEditSkill(emptySkill());
                setIsDirty(false);
                setIsEditing(false);
                await loadSkills(dataSourceId!);
            } else {
                if (confirmDelete.id === 0) {
                    // Unsaved ref — just remove from local state
                    setEditSkill(prev => ({
                        ...prev,
                        References: prev.References.filter(r => r !== activeRef)
                    }));
                } else {
                    await aiSkillSvc.DeleteRef(dataSourceId!, confirmDelete.id);
                    setEditSkill(prev => ({
                        ...prev,
                        References: prev.References.filter(r => r.RefId !== confirmDelete.id)
                    }));
                }
                setActiveRef(null);
                setShowRefEditor(false);
                setIsDirty(true);
            }
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
            setConfirmDelete(null);
        }
    };

    // --- Reference editing ---
    const handleAddRef = () => {
        const newRef = emptyRef(editSkill.SkillId);
        setActiveRef(newRef);
        setShowRefEditor(true);
    };

    const handleSelectRef = (ref: AppAISkillRefDto) => {
        setActiveRef({ ...ref });
        setShowRefEditor(true);
        setPreviewRefContent(false);
    };

    const handleRefFieldChange = (field: keyof AppAISkillRefDto, value: any) => {
        setActiveRef(prev => prev ? { ...prev, [field]: value } : prev);
        setIsDirty(true);
    };

    const handleSaveRef = () => {
        if (!activeRef) return;
        setEditSkill(prev => {
            const exists = prev.References.findIndex(r => r === activeRef || (r.RefId !== 0 && r.RefId === activeRef.RefId));
            if (exists >= 0) {
                const updated = [...prev.References];
                updated[exists] = activeRef;
                return { ...prev, References: updated };
            }
            return { ...prev, References: [...prev.References, activeRef] };
        });
        setShowRefEditor(false);
        setActiveRef(null);
        setIsDirty(true);
    };

    const moveRef = (index: number, direction: 'up' | 'down') => {
        const refs = [...editSkill.References];
        const swap = direction === 'up' ? index - 1 : index + 1;
        if (swap < 0 || swap >= refs.length) return;
        [refs[index], refs[swap]] = [refs[swap], refs[index]];
        refs.forEach((r, i) => { r.SortOrder = i; });
        setEditSkill(prev => ({ ...prev, References: refs }));
        setIsDirty(true);
    };

    const tabClass = (tab: Tab) =>
        `px-3 py-1.5 text-xs rounded-[4px] cursor-pointer mr-1 ${theme.button_default}${activeTab === tab ? ' border-b-2 font-semibold' : ''}`;

    const hasSelection = isEditing;
    appHelper.debugLog('[AISkill] render: isEditing=', isEditing, 'dataSourceId=', dataSourceId);

    return (
        <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
            {/* Header */}
            <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
                <div className={`text-md font-semibold ${theme.title}`}>AI Skill Management</div>
                <div className="flex items-center gap-2">
                    <label className={`text-xs ${theme.label}`}>Data Source</label>
                    <select
                        className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                        value={dataSourceId ?? ''}
                        onChange={e => setDataSourceId(e.target.value ? Number(e.target.value) : null)}
                    >
                        <option value="">-- Select --</option>
                        {dataSources.map((ds: any) => (
                            <option key={ds.Id ?? ds.DataSourceRegisterId} value={ds.Id ?? ds.DataSourceRegisterId}>
                                {ds.Name ?? ds.DataSourceName}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

            {/* Error bar */}
            {error && (
                <div className="px-3 py-1 text-xs text-red-600 bg-red-50 border border-red-200 mx-2 mb-1 rounded">
                    {error}
                    <button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                </div>
            )}

            {/* Prompt to select data source */}
            {!dataSourceId && (
                <div className={`mx-2 mb-1 px-3 py-2 text-xs rounded ${theme.mainContentSection} ${theme.label}`}>
                    Please select a Data Source to manage skills.
                </div>
            )}

            {/* Main two-panel layout */}
            <div className={`w-full h-1 flex-auto flex overflow-hidden gap-2 px-2 pb-2`}>

                {/* LEFT: Skill list */}
                <div className={`w-64 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                    {/* Toolbar */}
                    <div className="flex items-center px-2 py-1 gap-1 border-b border-gray-200">
                        <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleNewSkill}>
                            <i className="fa-solid fa-plus mr-1" /> New
                        </button>
                        {selectedSkill && (
                            <button
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={() => handleDeleteSkill(selectedSkill.SkillId)}
                            >
                                <i className="fa-solid fa-trash mr-1" /> Delete
                            </button>
                        )}
                    </div>

                    {/* Grid */}
                    <div className="w-full h-1 flex-auto overflow-hidden">
                        <FlexGrid
                            className="w-full h-full"
                            itemsSource={skillsCV}
                            isReadOnly
                            headersVisibility="Column"
                            selectionChanged={onGridSelectionChanged}
                        >
                            <FlexGridColumn header="Name" binding="Name" width="*" />
                            <FlexGridColumn header="Active" binding="IsActive" width={55} />
                            <FlexGridColumn header="" binding="" width="*" />
                        </FlexGrid>
                    </div>
                </div>

                {/* RIGHT: Skill editor */}
                <div className={`w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                {isEditing ? (
                    <div className="h-full flex flex-col overflow-hidden">
                        {/* Tabs */}
                        <div className="flex items-center px-2 py-1 border-b border-gray-200">
                            <button className={tabClass('general')} onClick={() => setActiveTab('general')}>General</button>
                            <button className={tabClass('content')} onClick={() => setActiveTab('content')}>Skill Content</button>
                            <button className={tabClass('refs')} onClick={() => setActiveTab('refs')}>References ({editSkill.References.length})</button>
                        </div>

                        {/* Tab body */}
                        <div className="w-full h-1 flex-auto overflow-auto p-3">

                            {/* --- General tab --- */}
                            {activeTab === 'general' && (
                                <div className="flex flex-col gap-3">
                                    <div className="flex items-center py-1">
                                        <label className={`w-32 text-xs ${theme.label} mr-2`}>Name *</label>
                                        <input
                                            className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                            value={editSkill.Name}
                                            autoComplete="off"
                                            onChange={e => updateField('Name', e.target.value)}
                                        />
                                    </div>
                                    <div className="flex items-start py-1">
                                        <label className={`w-32 text-xs ${theme.label} mr-2 mt-1`}>Description</label>
                                        <textarea
                                            className={`flex-auto w-32 px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none`}
                                            rows={3}
                                            value={editSkill.Description ?? ''}
                                            onChange={e => updateField('Description', e.target.value)}
                                        />
                                    </div>
                                    <div className="flex items-center py-1">
                                        <label className={`w-32 text-xs ${theme.label} mr-2`}>Active</label>
                                        <input
                                            type="checkbox"
                                            checked={editSkill.IsActive}
                                            onChange={e => updateField('IsActive', e.target.checked)}
                                        />
                                    </div>
                                </div>
                            )}

                            {/* --- Skill Content tab --- */}
                            {activeTab === 'content' && (
                                <div className="flex flex-col gap-2 h-full">
                                    <div className="flex items-center gap-2">
                                        <span className={`text-xs ${theme.label}`}>Markdown Editor</span>
                                        <button
                                            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                            onClick={() => setPreviewContent(p => !p)}
                                        >
                                            {previewContent ? 'Edit' : 'Preview'}
                                        </button>
                                    </div>
                                    {previewContent ? (
                                        <div
                                            className={`flex-auto p-3 text-xs border rounded overflow-auto ${theme.inputBox}`}
                                            style={{ minHeight: 400 }}
                                            dangerouslySetInnerHTML={{ __html: simpleMarkdown(editSkill.SkillContent ?? '') }}
                                        />
                                    ) : (
                                        <textarea
                                            className={`flex-auto w-full px-2 py-1 text-xs border font-mono ${theme.inputBox} focus:outline-none`}
                                            style={{ minHeight: 400, resize: 'vertical', whiteSpace: 'pre' }}
                                            value={editSkill.SkillContent ?? ''}
                                            onChange={e => updateField('SkillContent', e.target.value)}
                                        />
                                    )}
                                </div>
                            )}

                            {/* --- Reference Files tab --- */}
                            {activeTab === 'refs' && (
                                <div className="flex flex-col gap-2">
                                    <div className="flex items-center gap-2">
                                        <button
                                            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                            onClick={handleAddRef}
                                        >
                                            <i className="fa-solid fa-plus mr-1" /> Add Reference
                                        </button>
                                    </div>

                                    {/* Ref list */}
                                    {editSkill.References.length === 0 ? (
                                        <div className={`text-xs ${theme.label} py-4`}>No reference files added.</div>
                                    ) : (
                                        <table className="w-full text-xs border-collapse">
                                            <thead>
                                                <tr className={`${theme.mainContentSection}`}>
                                                    <th className="text-left px-2 py-1 border-b">File Name</th>
                                                    <th className="text-left px-2 py-1 border-b w-16">Order</th>
                                                    <th className="px-2 py-1 border-b w-24"></th>
                                                </tr>
                                            </thead>
                                            <tbody>
                                                {editSkill.References.map((ref, idx) => (
                                                    <tr key={ref.RefId || idx} className={`border-b ${theme.mainContentSection}`}>
                                                        <td className="px-2 py-1">
                                                            <button
                                                                className={`text-xs text-left underline ${theme.label}`}
                                                                onClick={() => handleSelectRef(ref)}
                                                            >
                                                                {ref.FileName || '(unnamed)'}
                                                            </button>
                                                        </td>
                                                        <td className="px-2 py-1">{idx}</td>
                                                        <td className="px-2 py-1 flex gap-1">
                                                            <button className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={() => moveRef(idx, 'up')} disabled={idx === 0}>
                                                                <i className="fa-solid fa-arrow-up" />
                                                            </button>
                                                            <button className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={() => moveRef(idx, 'down')} disabled={idx === editSkill.References.length - 1}>
                                                                <i className="fa-solid fa-arrow-down" />
                                                            </button>
                                                            <button className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`} onClick={() => handleDeleteRef(ref.RefId)}>
                                                                <i className="fa-solid fa-trash" />
                                                            </button>
                                                        </td>
                                                    </tr>
                                                ))}
                                            </tbody>
                                        </table>
                                    )}

                                    {/* Inline reference editor */}
                                    {showRefEditor && activeRef && (
                                        <div className={`mt-2 p-3 border rounded ${theme.mainContentSection} flex flex-col gap-2`}>
                                            <div className="flex items-center py-1">
                                                <label className={`w-32 text-xs ${theme.label} mr-2`}>File Name</label>
                                                <input
                                                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                                    value={activeRef.FileName}
                                                    autoComplete="off"
                                                    onChange={e => handleRefFieldChange('FileName', e.target.value)}
                                                />
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <label className={`w-32 text-xs ${theme.label}`}>Content (Markdown)</label>
                                                <button
                                                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                                    onClick={() => setPreviewRefContent(p => !p)}
                                                >
                                                    {previewRefContent ? 'Edit' : 'Preview'}
                                                </button>
                                            </div>
                                            {previewRefContent ? (
                                                <div
                                                    className={`p-3 text-xs border rounded overflow-auto ${theme.inputBox}`}
                                                    style={{ minHeight: 200 }}
                                                    dangerouslySetInnerHTML={{ __html: simpleMarkdown(activeRef.FileContent ?? '') }}
                                                />
                                            ) : (
                                                <textarea
                                                    className={`w-full px-2 py-1 text-xs border font-mono ${theme.inputBox} focus:outline-none`}
                                                    style={{ minHeight: 200, resize: 'vertical', whiteSpace: 'pre' }}
                                                    value={activeRef.FileContent ?? ''}
                                                    onChange={e => handleRefFieldChange('FileContent', e.target.value)}
                                                />
                                            )}
                                            <div className="flex gap-2">
                                                <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleSaveRef}>
                                                    <i className="fa-solid fa-check mr-1" /> Apply
                                                </button>
                                                <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => { setShowRefEditor(false); setActiveRef(null); }}>
                                                    Cancel
                                                </button>
                                            </div>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        {/* Footer: Save / Cancel */}
                        <div className={`flex items-center gap-2 px-3 py-2 border-t border-gray-200`}>
                            <button
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleSave}
                                disabled={!isDirty}
                            >
                                <i className="fa-solid fa-floppy-disk mr-1" /> Save
                            </button>
                            <button
                                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                                onClick={handleCancel}
                                disabled={!isDirty}
                            >
                                Cancel
                            </button>
                            {isDirty && <span className="text-xs text-orange-500 ml-2">Unsaved changes</span>}
                        </div>
                    </div>
                ) : (
                    <div className="h-full flex flex-col items-center justify-center gap-2">
                        <i className={`fa-solid fa-wand-magic-sparkles text-2xl opacity-40 ${theme.title}`}></i>
                        <span className={`text-sm font-medium ${theme.title}`}>Select a skill or click + New to get started.</span>
                    </div>
                )}
                </div>
            </div>

            {/* Confirm delete dialog */}
            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>
                            {confirmDelete.type === 'skill'
                                ? 'This will also delete all reference files. Continue?'
                                : 'Delete this reference file?'}
                        </div>
                        <div className="flex gap-2">
                            <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleConfirmDelete}>
                                Delete
                            </button>
                            <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setConfirmDelete(null)}>
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

// Simple markdown-to-html for preview (headings, bold, italic, code, line breaks)
function simpleMarkdown(md: string): string {
    return md
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/^#{3}\s(.+)$/gm, '<h3>$1</h3>')
        .replace(/^#{2}\s(.+)$/gm, '<h2>$1</h2>')
        .replace(/^#{1}\s(.+)$/gm, '<h1>$1</h1>')
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`(.+?)`/g, '<code>$1</code>')
        .replace(/\n/g, '<br/>');
}

export default AISkillManagement;
