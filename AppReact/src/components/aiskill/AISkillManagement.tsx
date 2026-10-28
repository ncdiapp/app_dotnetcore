import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch, useSelector } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { RootState } from '../../redux/store';
import { aiSkillSvc, AppAISkillDto, AppAISkillRefDto } from '../../webapi/aiSkillSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import { AgentMarkdown } from './AgentMarkdown';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const SPLIT_LEFT_DEFAULT_PX = 500;
const SPLIT_LEFT_MIN_PX = 400;
const SPLIT_RIGHT_MIN_PX = 320;

/** Starter markdown shown when creating a new skill. */
const DEFAULT_SKILL_CONTENT = `# [Skill title]

## Purpose
Describe when an agent should load this skill (one or two sentences).

## Instructions
- Main rule or step 1
- Main rule or step 2

## Constraints
- What to avoid
- Expected output format or tone

## Examples (optional)
User: ...
Agent: ...
`;

const DEFAULT_REF_CONTENT = `# Reference: [file name]

Supplementary markdown appended to the skill when agents load the composed prompt.

## Content
- Tables, field lists, or domain rules
- Keep one topic per reference file
`;

const emptySkill = (): AppAISkillDto => ({
    SkillId: 0,
    Name: '',
    Description: '',
    SkillContent: DEFAULT_SKILL_CONTENT,
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

type EditorTab = 'content' | 'refs';
type ContentLayout = 'edit' | 'split' | 'preview';

function formatSkillDate(d?: string | null): string {
    if (!d) return '—';
    const dt = new Date(d);
    return Number.isNaN(dt.getTime()) ? d : dt.toLocaleDateString();
}

const AISkillManagement: React.FC = () => {
    const { theme, t } = useTheme();
    const currentThemeId = useSelector((s: RootState) => s.theme.currentThemeId);
    const monacoTheme = currentThemeId === 'dark' ? 'vs-dark' : 'vs';
    const dispatch = useDispatch();

    const [dataSources, setDataSources] = useState<any[]>([]);
    const [dataSourceId, setDataSourceId] = useState<number | null>(null);
    const dataSourceIdRef = useRef<number | null>(null);

    const [skillsCV] = useState(() => new CollectionView<AppAISkillDto>([]));
    const [listSearch, setListSearch] = useState('');
    const flexGridRef = useRef<any>(null);
    const programmaticSelectRef = useRef(false);
    const selectedSkillIdRef = useRef<number | null>(null);

    const [selectedSkill, setSelectedSkill] = useState<AppAISkillDto | null>(null);
    const [editSkill, setEditSkill] = useState<AppAISkillDto>(emptySkill());
    const [isEditing, setIsEditing] = useState(false);
    const [editorTab, setEditorTab] = useState<EditorTab>('content');
    const [isDirty, setIsDirty] = useState(false);
    const [selectedRefIndex, setSelectedRefIndex] = useState<number | null>(null);
    const [confirmDelete, setConfirmDelete] = useState<{ type: 'skill' | 'ref'; id: number; index?: number } | null>(null);
    const [contentLayout, setContentLayout] = useState<ContentLayout>('preview');
    const [refContentLayout, setRefContentLayout] = useState<ContentLayout>('preview');
    const [error, setError] = useState<string | null>(null);
    const [headerDetailsExpanded, setHeaderDetailsExpanded] = useState(false);

    const [pendingDiscard, setPendingDiscard] = useState<{
        message: string;
        onConfirm: () => void;
        afterConfirmSelectSkillId?: number;
    } | null>(null);

    const [leftPanelWidthPx, setLeftPanelWidthPx] = useState(SPLIT_LEFT_DEFAULT_PX);
    const splitContainerRef = useRef<HTMLDivElement | null>(null);
    const splitDragRef = useRef(false);

    const [contextMenuItem, setContextMenuItem] = useState<AppAISkillDto | null>(null);
    const [contextMenuPos, setContextMenuPos] = useState({ x: 0, y: 0 });
    const contextMenuRef = useRef<HTMLDivElement | null>(null);

    const borderCls = t('border_mainContentSection');
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const inp = `flex-auto min-w-0 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const lbl = `w-32 text-xs ${theme.label} mr-2 shrink-0`;
    const editorTabCls = (tab: EditorTab) =>
        `px-3 py-2 text-xs -mb-px border-b-2 transition-opacity ${
            editorTab === tab
                ? `font-semibold ${theme.title} border-current`
                : `border-transparent ${theme.label} hover:opacity-90`
        }`;
    const layoutIconCls = (mode: ContentLayout, active: ContentLayout) =>
        `w-7 h-7 rounded-[3px] flex items-center justify-center shrink-0 ${
            active === mode ? theme.menu_default : `${theme.button_default} opacity-75 hover:opacity-100`
        }`;

    const skillContentChars = (editSkill.SkillContent ?? '').length;
    const monacoEditProps = { monacoTheme, debounceMs: 150, lineNumbers: 'off' as const };

    useEffect(() => { dataSourceIdRef.current = dataSourceId; }, [dataSourceId]);
    useEffect(() => { selectedSkillIdRef.current = selectedSkill?.SkillId ?? null; }, [selectedSkill]);

    useEffect(() => {
        setRefContentLayout('preview');
    }, [selectedRefIndex]);

    useEffect(() => {
        if (editorTab === 'refs' && editSkill.References.length > 0 && selectedRefIndex == null) {
            setSelectedRefIndex(0);
        }
    }, [editorTab, editSkill.References.length, selectedRefIndex]);

    useEffect(() => {
        const q = listSearch.trim().toLowerCase();
        skillsCV.filter = (item) => {
            if (!q) return true;
            const row = item as AppAISkillDto;
            return (row.Name ?? '').toLowerCase().includes(q)
                || (row.Description ?? '').toLowerCase().includes(q);
        };
        skillsCV.refresh();
    }, [listSearch, skillsCV]);

    useEffect(() => {
        const onMove = (e: MouseEvent) => {
            if (!splitDragRef.current || !splitContainerRef.current) return;
            const rect = splitContainerRef.current.getBoundingClientRect();
            const w = e.clientX - rect.left;
            const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, rect.width - SPLIT_RIGHT_MIN_PX);
            setLeftPanelWidthPx(Math.min(Math.max(w, SPLIT_LEFT_MIN_PX), maxLeft));
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

    useEffect(() => {
        if (!contextMenuItem) return;
        const close = () => setContextMenuItem(null);
        document.addEventListener('click', close);
        return () => document.removeEventListener('click', close);
    }, [contextMenuItem]);

    useRefineContextMenuPosition(!!contextMenuItem, contextMenuRef, setContextMenuPos);

    const restoreGridSelection = useCallback(() => {
        const flex = flexGridRef.current;
        const skillId = selectedSkillIdRef.current;
        if (!flex || skillId == null) return;
        const idx = skillsCV.items.findIndex(x => x?.SkillId === skillId);
        if (idx < 0) return;
        programmaticSelectRef.current = true;
        try {
            flex.select(idx, 0);
        } finally {
            programmaticSelectRef.current = false;
        }
    }, [skillsCV]);

    const selectGridRowBySkillId = useCallback((skillId: number) => {
        const flex = flexGridRef.current;
        if (!flex) return;
        const idx = skillsCV.items.findIndex(x => x?.SkillId === skillId);
        if (idx < 0) return;
        programmaticSelectRef.current = true;
        try {
            flex.select(idx, 0);
        } finally {
            programmaticSelectRef.current = false;
        }
    }, [skillsCV]);

    const runOrConfirmDiscard = useCallback((
        message: string,
        onConfirm: () => void,
        options?: { afterConfirmSelectSkillId?: number }
    ) => {
        if (!isDirty) {
            onConfirm();
            if (options?.afterConfirmSelectSkillId != null) {
                selectGridRowBySkillId(options.afterConfirmSelectSkillId);
            }
            return;
        }
        setPendingDiscard({
            message,
            onConfirm,
            afterConfirmSelectSkillId: options?.afterConfirmSelectSkillId,
        });
    }, [isDirty, selectGridRowBySkillId]);

    useEffect(() => {
        adminSvc.getDataSourceRegisterList(false).then(dsResult => {
            const list: any[] = dsResult?.ObjectList ?? (Array.isArray(dsResult) ? dsResult : []);
            setDataSources(list);
            if (list.length === 0) return;

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

    const loadSkills = async (dsId: number) => {
        dispatch(setIsBusy());
        setError(null);
        try {
            const res = await aiSkillSvc.GetAll(dsId);
            programmaticSelectRef.current = true;
            skillsCV.sourceCollection = res.Object ?? [];
            skillsCV.refresh();
            programmaticSelectRef.current = false;
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
            setSelectedRefIndex(null);
            skillsCV.sourceCollection = [];
            skillsCV.refresh();
            loadSkills(dataSourceId);
        }
    }, [dataSourceId]);

    const loadSkillIntoEditor = async (item: AppAISkillDto) => {
        const currentDataSourceId = dataSourceIdRef.current;
        if (!currentDataSourceId) return;

        dispatch(setIsBusy());
        setError(null);
        try {
            const res = await aiSkillSvc.GetById(currentDataSourceId, item.SkillId);
            const skill = res.Object;
            setSelectedSkill(skill);
            setEditSkill({ ...skill, References: [...(skill.References ?? [])] });
            setIsDirty(false);
            setIsEditing(true);
            setEditorTab('content');
            setContentLayout('preview');
            setSelectedRefIndex(null);
            setHeaderDetailsExpanded(!!(skill.Description ?? '').trim());
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const onGridSelectionChanged = async (s: any) => {
        if (programmaticSelectRef.current) return;
        const flex = s?.control ?? s;
        const row = flex.selection?.row;
        if (row == null || row < 0) return;
        const item: AppAISkillDto = flex.rows[row]?.dataItem;
        if (!item || !dataSourceIdRef.current) return;
        if (selectedSkill?.SkillId === item.SkillId && isEditing) return;

        runOrConfirmDiscard(
            'You have unsaved changes. Switch to another skill anyway?',
            () => { void loadSkillIntoEditor(item); },
            { afterConfirmSelectSkillId: item.SkillId },
        );
        if (isDirty) {
            restoreGridSelection();
        }
    };

    const beginNewSkill = () => {
        if (!dataSourceId) return;
        const start = () => {
            setSelectedSkill(null);
            setEditSkill(emptySkill());
            setIsDirty(false);
            setIsEditing(true);
            setEditorTab('content');
            setContentLayout('preview');
            setSelectedRefIndex(null);
            setHeaderDetailsExpanded(false);
            programmaticSelectRef.current = true;
            try {
                flexGridRef.current?.select(-1, -1);
            } catch { /* ignore */ }
            programmaticSelectRef.current = false;
        };
        runOrConfirmDiscard('You have unsaved changes. Create a new skill anyway?', start);
    };

    const handleDataSourceChange = (nextId: number | null) => {
        if (nextId === dataSourceId) return;
        runOrConfirmDiscard('You have unsaved changes. Change data source anyway?', () => {
            setDataSourceId(nextId);
        });
    };

    const updateField = (field: keyof AppAISkillDto, value: any) => {
        setEditSkill(prev => ({ ...prev, [field]: value }));
        setIsDirty(true);
    };

    const updateRefAt = (index: number, patch: Partial<AppAISkillRefDto>) => {
        setEditSkill(prev => {
            const refs = [...prev.References];
            refs[index] = { ...refs[index], ...patch };
            return { ...prev, References: refs };
        });
        setIsDirty(true);
    };

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

            const fresh = await aiSkillSvc.GetById(dataSourceId, skillId);
            setSelectedSkill(fresh.Object);
            setEditSkill({ ...fresh.Object, References: [...(fresh.Object.References ?? [])] });
            setIsEditing(true);
            setHeaderDetailsExpanded(!!(fresh.Object.Description ?? '').trim());
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
        }
    };

    const handleCancel = () => {
        if (selectedSkill) {
            setEditSkill({ ...selectedSkill, References: [...(selectedSkill.References ?? [])] });
            setIsDirty(false);
            setSelectedRefIndex(null);
            setHeaderDetailsExpanded(!!(selectedSkill.Description ?? '').trim());
        } else {
            setEditSkill(emptySkill());
            setIsDirty(false);
            setIsEditing(false);
            setSelectedRefIndex(null);
        }
    };

    const handleConfirmDelete = async () => {
        if (!confirmDelete || !dataSourceId) return;
        dispatch(setIsBusy());
        try {
            if (confirmDelete.type === 'skill') {
                await aiSkillSvc.Delete(dataSourceId, confirmDelete.id);
                setSelectedSkill(null);
                setEditSkill(emptySkill());
                setIsDirty(false);
                setIsEditing(false);
                setSelectedRefIndex(null);
                await loadSkills(dataSourceId);
            } else {
                const idx = confirmDelete.index;
                if (confirmDelete.id === 0 && idx != null) {
                    setEditSkill(prev => ({
                        ...prev,
                        References: prev.References.filter((_, i) => i !== idx)
                    }));
                    setSelectedRefIndex(null);
                } else if (confirmDelete.id !== 0) {
                    await aiSkillSvc.DeleteRef(dataSourceId, confirmDelete.id);
                    setEditSkill(prev => ({
                        ...prev,
                        References: prev.References.filter(r => r.RefId !== confirmDelete.id)
                    }));
                    setSelectedRefIndex(null);
                }
                setIsDirty(true);
            }
        } catch (e: any) {
            setError(e.message);
        } finally {
            dispatch(setIsNotBusy());
            setConfirmDelete(null);
        }
    };

    const handleAddRef = () => {
        const newRef = emptyRef(editSkill.SkillId);
        newRef.SortOrder = editSkill.References.length;
        newRef.FileName = 'reference.md';
        newRef.FileContent = DEFAULT_REF_CONTENT;
        setEditSkill(prev => ({
            ...prev,
            References: [...prev.References, newRef]
        }));
        setSelectedRefIndex(editSkill.References.length);
        setRefContentLayout('preview');
        setIsDirty(true);
        setEditorTab('refs');
    };

    const moveRef = (index: number, direction: 'up' | 'down') => {
        const refs = [...editSkill.References];
        const swap = direction === 'up' ? index - 1 : index + 1;
        if (swap < 0 || swap >= refs.length) return;
        [refs[index], refs[swap]] = [refs[swap], refs[index]];
        refs.forEach((r, i) => { r.SortOrder = i; });
        setEditSkill(prev => ({ ...prev, References: refs }));
        setSelectedRefIndex(swap);
        setIsDirty(true);
    };

    const openContextMenu = (e: React.MouseEvent, item: AppAISkillDto) => {
        e.stopPropagation();
        const rect = e.currentTarget.getBoundingClientRect();
        const pos = clampContextMenuPosition(rect.right, rect.top, 200, 80);
        setContextMenuPos(pos);
        setContextMenuItem(item);
    };

    const contextMenuDeleteSkill = () => {
        const item = contextMenuItem;
        setContextMenuItem(null);
        if (!item?.SkillId) return;
        setConfirmDelete({ type: 'skill', id: item.SkillId });
    };

    const onSplitDividerMouseDown = useCallback((e: React.MouseEvent) => {
        e.preventDefault();
        splitDragRef.current = true;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    }, []);

    const selectedRef = selectedRefIndex != null ? editSkill.References[selectedRefIndex] : null;

    const renderMarkdownPane = (body: string, className?: string) => (
        <div className={`h-full overflow-auto p-3 pt-10 border rounded-[4px] ${theme.inputBox} ${borderCls} ${className ?? ''}`}>
            <AgentMarkdown content={body || '_(empty)_'} className="text-xs" />
        </div>
    );

    const renderMarkdownBody = (
        body: string,
        layout: ContentLayout,
        onChange: (v: string) => void,
        editorMinH: string,
    ) => {
        const editor = (
            <JsonCodeEditor
                className={`w-full h-full ${editorMinH}`}
                language="markdown"
                value={body}
                onChange={onChange}
                {...monacoEditProps}
            />
        );
        const preview = renderMarkdownPane(body);
        if (layout === 'preview') {
            return <div className="w-full h-full min-h-0">{preview}</div>;
        }
        if (layout === 'edit') {
            return (
                <div className={`w-full h-full min-h-0 pt-8 ${editorMinH}`}>
                    {editor}
                </div>
            );
        }
        return (
            <div className="w-full h-full min-h-0 flex gap-2 overflow-hidden pt-8">
                <div className={`w-1 flex-auto min-w-0 min-h-0 ${editorMinH}`}>{editor}</div>
                <div className="w-1 flex-auto min-w-0 min-h-0">{preview}</div>
            </div>
        );
    };

    const MarkdownViewShell: React.FC<{
        layout: ContentLayout;
        setLayout: (mode: ContentLayout) => void;
        topLeft?: React.ReactNode;
        children: React.ReactNode;
    }> = ({ layout, setLayout, topLeft, children }) => (
        <div className="relative w-full h-full min-h-0">
            {topLeft && (
                <div className={`absolute top-2 left-2 z-10 text-[10px] px-1.5 py-0.5 rounded ${theme.mainContentSection} ${theme.label} border ${borderCls} shadow-sm`}>
                    {topLeft}
                </div>
            )}
            <div
                className={`absolute top-2 right-2 z-10 flex items-center gap-0.5 p-0.5 rounded-[4px] border shadow-md ${theme.mainContentSection} ${borderCls}`}
                role="toolbar"
                aria-label="View mode"
            >
                <button
                    type="button"
                    className={layoutIconCls('preview', layout)}
                    title="Preview"
                    aria-label="Preview"
                    aria-pressed={layout === 'preview'}
                    onClick={() => setLayout('preview')}
                >
                    <i className="fa-solid fa-eye text-[11px]" aria-hidden />
                </button>
                <button
                    type="button"
                    className={layoutIconCls('split', layout)}
                    title="Split (edit and preview)"
                    aria-label="Split view"
                    aria-pressed={layout === 'split'}
                    onClick={() => setLayout('split')}
                >
                    <i className="fa-solid fa-table-columns text-[10px]" aria-hidden />
                </button>
                <button
                    type="button"
                    className={layoutIconCls('edit', layout)}
                    title="Edit"
                    aria-label="Edit"
                    aria-pressed={layout === 'edit'}
                    onClick={() => setLayout('edit')}
                >
                    <i className="fa-solid fa-pen-to-square text-[10px]" aria-hidden />
                </button>
            </div>
            {children}
        </div>
    );

    const renderRefBodyEditor = (body: string, onChange: (v: string) => void, layout: ContentLayout) =>
        renderMarkdownBody(body, layout, onChange, 'min-h-[160px]');

    const renderContentEditor = () =>
        renderMarkdownBody(
            editSkill.SkillContent ?? '',
            contentLayout,
            (v) => updateField('SkillContent', v),
            'min-h-[200px]',
        );

    return (
        <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
            <div className={`flex items-center justify-between gap-2 px-3 py-2 mb-1 mx-2 ${theme.mainContentSection}`}>
                <div className={`text-md font-semibold shrink-0 ${theme.title}`}>AI Skill Management</div>
                <div className="flex items-center gap-2 min-w-0 w-1 flex-auto justify-end flex-wrap">
                    <label className={`text-xs shrink-0 ${theme.label}`}>Data Source</label>
                    <select
                        className={`h-7 px-2 text-xs border rounded-[4px] max-w-xs ${theme.inputBox} focus:outline-none`}
                        value={dataSourceId ?? ''}
                        onChange={e => handleDataSourceChange(e.target.value ? Number(e.target.value) : null)}
                    >
                        <option value="">-- Select --</option>
                        {dataSources.map((ds: any) => (
                            <option key={ds.Id ?? ds.DataSourceRegisterId} value={ds.Id ?? ds.DataSourceRegisterId}>
                                {ds.Name ?? ds.DataSourceName}
                            </option>
                        ))}
                    </select>
                    {isEditing && (
                        <>
                            <button type="button" className={btn} onClick={() => void handleSave()} disabled={!isDirty}>
                                <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
                            </button>
                            <button type="button" className={btn} onClick={handleCancel} disabled={!isDirty}>
                                Cancel
                            </button>
                            {isDirty && <span className="text-xs opacity-80">Unsaved changes</span>}
                        </>
                    )}
                </div>
            </div>

            {error && (
                <div className={`px-3 py-1 text-xs mx-2 mb-1 rounded border ${theme.mainContentSection} ${theme.label} ${borderCls}`}>
                    {error}
                    <button type="button" className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                </div>
            )}

            {!dataSourceId && (
                <div className={`mx-2 mb-1 px-3 py-2 text-xs rounded ${theme.mainContentSection} ${theme.label}`}>
                    Please select a Data Source to manage skills.
                </div>
            )}

            <div
                ref={splitContainerRef}
                className={`w-full h-1 flex-auto flex overflow-hidden gap-0 px-2 pb-2${!dataSourceId ? ' opacity-50 pointer-events-none' : ''}`}
            >
                <div
                    className={`shrink-0 flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}
                    style={{ width: leftPanelWidthPx, minWidth: SPLIT_LEFT_MIN_PX }}
                >
                    <div className={`flex flex-col gap-1 px-2 py-1 border-b ${borderCls}`}>
                        <div className="flex items-center gap-1">
                            <button type="button" className={btn} onClick={beginNewSkill}>
                                <i className="fa-solid fa-plus mr-1" aria-hidden /> New
                            </button>
                        </div>
                        <input
                            className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                            placeholder="Search skills…"
                            value={listSearch}
                            onChange={e => setListSearch(e.target.value)}
                            autoComplete="off"
                        />
                    </div>
                    <div className="w-full h-1 flex-auto overflow-hidden">
                        <FlexGrid
                            className="w-full h-full"
                            itemsSource={skillsCV}
                            isReadOnly
                            headersVisibility="Column"
                            selectionMode="Row"
                            selectionChanged={onGridSelectionChanged}
                            initialized={(flex: any) => { flexGridRef.current = flex; }}
                        >
                            <FlexGridColumn header="Actions" width={60} isReadOnly>
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => (
                                        <div className="flex items-center justify-center w-full">
                                            <button
                                                type="button"
                                                className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                                                title="More Options"
                                                onClick={(e) => openContextMenu(e, cell.item)}
                                            >
                                                <i className="fa-solid fa-pencil text-xs" aria-hidden />
                                                <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                                            </button>
                                        </div>
                                    )}
                                />
                            </FlexGridColumn>
                            <FlexGridColumn header="Name" binding="Name" width="*" />
                            <FlexGridColumn header="Active" binding="IsActive" width={48} align="center">
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => (
                                        cell.item?.IsActive
                                            ? <i className={`fa-solid fa-check text-xs ${theme.title}`} aria-hidden title="Active" />
                                            : <span className={`text-xs ${theme.label}`}>—</span>
                                    )}
                                />
                            </FlexGridColumn>
                            <FlexGridColumn header="Updated" binding="UpdatedDate" width={84}>
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => (
                                        <span className={`text-xs ${theme.label}`}>
                                            {formatSkillDate(cell.item?.UpdatedDate ?? cell.item?.CreatedDate)}
                                        </span>
                                    )}
                                />
                            </FlexGridColumn>
                        </FlexGrid>
                    </div>
                </div>

                <div
                    role="separator"
                    aria-orientation="vertical"
                    aria-label="Resize panels"
                    title="Drag to resize panels"
                    tabIndex={0}
                    onMouseDown={onSplitDividerMouseDown}
                    onKeyDown={(e) => {
                        const el = splitContainerRef.current;
                        if (!el) return;
                        const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, el.getBoundingClientRect().width - SPLIT_RIGHT_MIN_PX);
                        if (e.key === 'ArrowLeft') {
                            e.preventDefault();
                            setLeftPanelWidthPx(w => Math.max(SPLIT_LEFT_MIN_PX, w - 16));
                        } else if (e.key === 'ArrowRight') {
                            e.preventDefault();
                            setLeftPanelWidthPx(w => Math.min(maxLeft, w + 16));
                        }
                    }}
                    className={`shrink-0 w-1.5 mx-1 cursor-col-resize border-x self-stretch min-h-0 ${theme.inputBox} hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset`}
                />

                <div className={`min-w-0 w-1 flex-auto flex flex-col overflow-hidden rounded ${theme.mainContentSection}`}>
                    {isEditing ? (
                        <div className="h-full flex flex-col overflow-hidden">
                            <div className={`shrink-0 border-b ${borderCls}`}>
                                <div className="flex items-center gap-2 px-3 py-1.5">
                                    <button
                                        type="button"
                                        className={`w-5 h-5 shrink-0 rounded text-[10px] flex items-center justify-center ${theme.button_default}`}
                                        onClick={() => setHeaderDetailsExpanded(v => !v)}
                                        aria-expanded={headerDetailsExpanded}
                                        title={headerDetailsExpanded ? 'Collapse details' : 'Expand details'}
                                    >
                                        <i className={`fa-solid fa-chevron-${headerDetailsExpanded ? 'down' : 'right'}`} aria-hidden />
                                    </button>
                                    <span className={`text-xs font-semibold truncate min-w-0 w-1 flex-auto ${theme.title}`}>
                                        {editSkill.Name.trim() || '(New skill)'}
                                    </span>
                                    <label className={`flex items-center gap-1 text-xs shrink-0 ${theme.label}`}>
                                        <input
                                            type="checkbox"
                                            checked={editSkill.IsActive}
                                            onChange={e => updateField('IsActive', e.target.checked)}
                                        />
                                        Active
                                    </label>
                                </div>
                                {headerDetailsExpanded && (
                                    <div className="px-3 pb-2 flex flex-col gap-2">
                                        <div className="flex items-center py-0.5">
                                            <label className={lbl}>Name *</label>
                                            <input
                                                className={inp}
                                                value={editSkill.Name}
                                                autoComplete="off"
                                                onChange={e => updateField('Name', e.target.value)}
                                            />
                                        </div>
                                        <div className="flex items-start py-0.5">
                                            <label className={`${lbl} mt-1`}>Description</label>
                                            <textarea
                                                className={`flex-auto min-w-0 px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none`}
                                                rows={2}
                                                value={editSkill.Description ?? ''}
                                                onChange={e => updateField('Description', e.target.value)}
                                            />
                                        </div>
                                    </div>
                                )}
                            </div>

                            <div className={`flex items-center gap-1 px-3 border-b ${borderCls}`}>
                                <button type="button" className={editorTabCls('content')} onClick={() => setEditorTab('content')}>
                                    Skill Content
                                </button>
                                <button type="button" className={editorTabCls('refs')} onClick={() => setEditorTab('refs')}>
                                    References ({editSkill.References.length})
                                </button>
                            </div>

                            <div className="w-full h-1 flex-auto min-h-0 flex flex-col overflow-hidden p-2">
                                {editorTab === 'content' && (
                                    <div className="h-full min-h-0">
                                        <MarkdownViewShell
                                            layout={contentLayout}
                                            setLayout={setContentLayout}
                                            topLeft={`${skillContentChars.toLocaleString()} chars`}
                                        >
                                            {renderContentEditor()}
                                        </MarkdownViewShell>
                                    </div>
                                )}

                                {editorTab === 'refs' && (
                                    <div className="h-full flex flex-col gap-2 min-h-0 overflow-hidden">
                                        <div className={`shrink-0 flex items-center gap-2 flex-wrap px-1 py-1 border-b ${borderCls}`}>
                                            <span className={`text-xs font-semibold ${theme.title}`}>Files</span>
                                            <button type="button" className={`${btn} text-xs py-1`} onClick={handleAddRef}>
                                                <i className="fa-solid fa-plus mr-1" aria-hidden /> Add
                                            </button>
                                            {selectedRef && selectedRefIndex != null && editSkill.References.length > 0 && (
                                                <>
                                                    <span className={`text-xs ${theme.label} opacity-50`}>|</span>
                                                    <button
                                                        type="button"
                                                        className={`${theme.button_default} w-7 h-7 text-xs rounded-[4px]`}
                                                        disabled={selectedRefIndex === 0}
                                                        title="Move up"
                                                        onClick={() => moveRef(selectedRefIndex, 'up')}
                                                    >
                                                        <i className="fa-solid fa-arrow-up" aria-hidden />
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`${theme.button_default} w-7 h-7 text-xs rounded-[4px]`}
                                                        disabled={selectedRefIndex === editSkill.References.length - 1}
                                                        title="Move down"
                                                        onClick={() => moveRef(selectedRefIndex, 'down')}
                                                    >
                                                        <i className="fa-solid fa-arrow-down" aria-hidden />
                                                    </button>
                                                    <button
                                                        type="button"
                                                        className={`${theme.button_default} w-7 h-7 text-xs rounded-[4px]`}
                                                        title="Delete file"
                                                        onClick={() => setConfirmDelete({
                                                            type: 'ref',
                                                            id: selectedRef.RefId,
                                                            index: selectedRefIndex,
                                                        })}
                                                    >
                                                        <i className="fa-solid fa-trash" aria-hidden />
                                                    </button>
                                                </>
                                            )}
                                        </div>

                                        {editSkill.References.length === 0 ? (
                                            <div className={`h-1 flex-auto flex flex-col items-center justify-center gap-2 ${theme.label} text-xs`}>
                                                <span>No reference files yet.</span>
                                                <button type="button" className={btn} onClick={handleAddRef}>
                                                    <i className="fa-solid fa-plus mr-1" aria-hidden /> Add reference
                                                </button>
                                            </div>
                                        ) : (
                                            <>
                                                <div className={`shrink-0 flex gap-1 overflow-x-auto pb-1 ${borderCls}`}>
                                                    {editSkill.References.map((ref, idx) => (
                                                        <button
                                                            key={ref.RefId || `new-${idx}`}
                                                            type="button"
                                                            title={ref.FileName || '(unnamed)'}
                                                            className={`shrink-0 max-w-[200px] truncate px-2 py-1 text-xs rounded-[4px] border ${borderCls} ${
                                                                selectedRefIndex === idx ? theme.menu_default : theme.button_default
                                                            }`}
                                                            onClick={() => setSelectedRefIndex(idx)}
                                                        >
                                                            <i className="fa-solid fa-file-lines mr-1 opacity-60" aria-hidden />
                                                            {ref.FileName || '(unnamed)'}
                                                        </button>
                                                    ))}
                                                </div>

                                                {selectedRef && selectedRefIndex != null ? (
                                                    <div className="w-full h-1 flex-auto min-h-0 flex flex-col gap-2 overflow-hidden">
                                                        <div className="flex items-center gap-2 shrink-0">
                                                            <label className={`text-xs shrink-0 ${theme.label}`}>File name</label>
                                                            <input
                                                                className={`${inp} max-w-md`}
                                                                value={selectedRef.FileName}
                                                                autoComplete="off"
                                                                onChange={e => updateRefAt(selectedRefIndex, { FileName: e.target.value })}
                                                            />
                                                        </div>
                                                        <div className="w-full h-1 flex-auto min-h-0">
                                                            <MarkdownViewShell
                                                                layout={refContentLayout}
                                                                setLayout={setRefContentLayout}
                                                            >
                                                                {renderRefBodyEditor(
                                                                    selectedRef.FileContent ?? '',
                                                                    (v) => updateRefAt(selectedRefIndex, { FileContent: v }),
                                                                    refContentLayout,
                                                                )}
                                                            </MarkdownViewShell>
                                                        </div>
                                                    </div>
                                                ) : (
                                                    <div className={`h-1 flex-auto flex items-center justify-center text-xs ${theme.label}`}>
                                                        Select a file above.
                                                    </div>
                                                )}
                                            </>
                                        )}
                                    </div>
                                )}
                            </div>
                        </div>
                    ) : (
                        <div className="h-full flex flex-col items-center justify-center gap-2">
                            <i className={`fa-solid fa-wand-magic-sparkles text-2xl opacity-40 ${theme.title}`} aria-hidden />
                            <span className={`text-sm font-medium ${theme.title}`}>Select a skill or click New to get started.</span>
                        </div>
                    )}
                </div>
            </div>

            {contextMenuItem && (
                <div
                    ref={contextMenuRef}
                    className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[180px] ${borderCls}`}
                    style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
                    onClick={(e) => e.stopPropagation()}
                >
                    <button
                        type="button"
                        className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
                        onClick={contextMenuDeleteSkill}
                    >
                        <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete skill
                    </button>
                </div>
            )}

            {confirmDelete && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4 border ${borderCls}`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Confirm Delete</div>
                        <div className={`text-xs ${theme.label}`}>
                            {confirmDelete.type === 'skill'
                                ? 'This will also delete all reference files. Continue?'
                                : 'Delete this reference file?'}
                        </div>
                        <div className="flex gap-2">
                            <button type="button" className={btn} onClick={() => void handleConfirmDelete()}>
                                Delete
                            </button>
                            <button type="button" className={btn} onClick={() => setConfirmDelete(null)}>
                                Cancel
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {pendingDiscard && (
                <div className="fixed inset-0 flex items-center justify-center bg-black bg-opacity-30 z-50">
                    <div className={`p-6 rounded shadow-lg ${theme.mainContentSection} flex flex-col gap-4 border ${borderCls}`} style={{ minWidth: 320 }}>
                        <div className={`text-sm font-semibold ${theme.title}`}>Unsaved changes</div>
                        <div className={`text-xs ${theme.label}`}>{pendingDiscard.message}</div>
                        <div className="flex gap-2">
                            <button
                                type="button"
                                className={btn}
                                onClick={() => {
                                    setIsDirty(false);
                                    const action = pendingDiscard.onConfirm;
                                    const skillId = pendingDiscard.afterConfirmSelectSkillId;
                                    setPendingDiscard(null);
                                    action();
                                    if (skillId != null) {
                                        selectGridRowBySkillId(skillId);
                                    }
                                }}
                            >
                                Discard and continue
                            </button>
                            <button type="button" className={btn} onClick={() => { setPendingDiscard(null); restoreGridSelection(); }}>
                                Stay
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default AISkillManagement;
