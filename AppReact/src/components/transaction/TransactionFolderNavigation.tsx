import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { searchSvc } from '../../webapi/searchSvc';
import { SearchView } from '../search/SearchView';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import {
  buildFormGroupOpenPayload,
  cacheFormGroupSession,
  EmAppLinkTargetActionType,
  buildFolderNavigationFormGroupCreatePayload,
  openFolderNavigationFormMasterDetail,
} from '../../utils/folderNavigationHelper';
import appHelper from '../../helper/appHelper';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';

interface FolderDto {
  Id: number;
  Name?: string;
  Children?: FolderDto[];
  Level?: number;
  FolderPath?: string;
  ParentId?: number | null;
  TransactionId?: number;
  FolderType?: number;
  DefaultViewId?: number | null;
  IsFolderReadonly?: boolean;
}

interface TransactionFolderNavigationProps {
  transactionId: number | string;
}

const FOLDER_CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const FOLDER_CONTEXT_MENU_ESTIMATED_HEIGHT = 280;

const TransactionFolderNavigation: React.FC<TransactionFolderNavigationProps> = ({ transactionId }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const { showValidationMessages } = useErrorMessage();
  const folderGridRef = useRef<any>(null);
  const folderContextMenuRef = useRef<HTMLDivElement>(null);
  const folderContentRequestRef = useRef(0);
  const suppressFolderSelectionRef = useRef(false);
  const currentFolderIdRef = useRef<number | null>(null);
  const currentViewIdRef = useRef<number | null>(null);

  const [folders, setFolders] = useState<FolderDto[]>([]);
  const [foldersCV, setFoldersCV] = useState<CollectionView | null>(null);
  const [currentFolderId, setCurrentFolderId] = useState<number | null>(null);
  const [viewList, setViewList] = useState<any[]>([]);
  const [currentViewId, setCurrentViewId] = useState<number | null>(null);
  const [currentViewDto, setCurrentViewDto] = useState<any | null>(null);
  const [viewDataList, setViewDataList] = useState<any[]>([]);
  const [selectedRows, setSelectedRows] = useState<any[]>([]);
  const [runtimeContext, setRuntimeContext] = useState<any>(null);
  const [cutFolderId, setCutFolderId] = useState<number | null>(null);
  const [folderContextMenu, setFolderContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    folder: FolderDto | null;
  }>({ visible: false, x: 0, y: 0, folder: null });
  const [folderEditPopup, setFolderEditPopup] = useState<{
    visible: boolean;
    mode: 'create' | 'rename' | 'defaultView';
    folder: Partial<FolderDto> | null;
    parentId: number | null;
  }>({ visible: false, mode: 'rename', folder: null, parentId: null });
  const folderTreeWidth = 280;

  const isTemplateMode = Boolean(runtimeContext?.IsTemplateMode);

  const flattenFolders = useCallback((list: FolderDto[], level = 0, parentId: number | null = null): FolderDto[] => {
    const out: FolderDto[] = [];
    (list || []).forEach((f) => {
      out.push({ ...f, Level: level, ParentId: f.ParentId ?? parentId ?? undefined });
      if (f.Children?.length) {
        out.push(...flattenFolders(f.Children, level + 1, f.Id));
      }
    });
    return out;
  }, []);

  const loadViewDefinition = useCallback(async (viewId: number) => {
    const [referenceView, linkTargets, linkedSearches, viewEx] = await Promise.all([
      searchSvc.retrieveOneReferenceViewDto(String(viewId)),
      searchSvc.retrieveOneSearchViewLinkTargetList(String(viewId), 1),
      searchSvc.retrieveOneAppViewLinkedSeaechOrUrlExDto(String(viewId)),
      searchSvc.retrieveOneAppSearchViewExDto(String(viewId)).catch(() => null),
    ]);
    const linkedList = Array.isArray(linkedSearches)
      ? linkedSearches
      : linkedSearches?.ObjectList ?? linkedSearches?.AppViewLinkedSeaechOrUrlDtoList ?? [];
    const fields = viewEx?.AppSearchViewFieldList ?? referenceView?.Columns ?? [];
    const folderField = fields.find((f: any) => f.IsFileFoderId);
    return {
      ...referenceView,
      Id: referenceView?.Id ?? viewId,
      AppSearchViewFieldList: viewEx?.AppSearchViewFieldList ?? referenceView?.AppSearchViewFieldList,
      AppFormLinkTargetList: Array.isArray(linkTargets) ? linkTargets : [],
      AppViewLinkedSeaechOrUrlDtoList: Array.isArray(linkedList) ? linkedList : [],
      FolderIdColumnId: folderField?.Id ?? viewEx?.FolderIdColumnId ?? referenceView?.FolderIdColumnId,
    };
  }, []);

  const loadFolderContent = useCallback(
    async (folderId: number, viewId: number) => {
      const requestId = ++folderContentRequestRef.current;
      dispatch(setIsBusy());
      try {
        const contentData = await appFolderNavigationService.getTransactionFolderViewList(
          String(folderId),
          String(viewId),
          String(transactionId),
        );
        if (requestId !== folderContentRequestRef.current) return;
        const list = Array.isArray(contentData) ? contentData : contentData?.ResultDataList ?? [];
        setViewDataList(list);
      } catch (e) {
        if (requestId !== folderContentRequestRef.current) return;
        appHelper.debugLog('TransactionFolderNavigation loadFolderContent', e);
        setViewDataList([]);
      } finally {
        if (requestId === folderContentRequestRef.current) {
          dispatch(setIsNotBusy());
        }
      }
    },
    [transactionId, dispatch],
  );

  const applyView = useCallback(
    async (viewSummary: any, folderId?: number | null) => {
      if (!viewSummary?.Id) return;
      const viewId = Number(viewSummary.Id);
      setCurrentViewId(viewId);
      currentViewIdRef.current = viewId;
      const fullView = await loadViewDefinition(viewId);
      setCurrentViewDto(fullView);
      const folderToLoad = folderId ?? currentFolderIdRef.current;
      if (folderToLoad != null) {
        await loadFolderContent(folderToLoad, viewId);
      }
    },
    [loadViewDefinition, loadFolderContent],
  );

  const changeView = useCallback(
    async (viewSummary: any) => {
      await applyView(viewSummary);
    },
    [applyView],
  );

  const changeFolder = useCallback(
    async (folderId: number) => {
      if (folderId === currentFolderIdRef.current) return;
      setCurrentFolderId(folderId);
      currentFolderIdRef.current = folderId;
      const viewId = currentViewIdRef.current;
      if (viewId != null) {
        await loadFolderContent(folderId, viewId);
      }
    },
    [loadFolderContent],
  );

  const refreshFolderNavigation = useCallback(
    async (preserveSelection = true) => {
      dispatch(setIsBusy());
      try {
        const [navDto, ctx] = await Promise.all([
          appFolderNavigationService.getFormDefaultTransactionFolderNavigation(String(transactionId)),
          appTransactionService.resolveFolderNavigationRuntimeContext(transactionId),
        ]);
        setRuntimeContext(ctx);
        const roots = navDto?.HairarchyFolderRootList ?? navDto?.hairarchyFolderRootList ?? [];
        setFolders(roots);
        setFoldersCV(new CollectionView(flattenFolders(roots)));
        const views = navDto?.ViewList ?? navDto?.viewList ?? [];
        setViewList(views);

        const preservedFolderId = preserveSelection ? currentFolderIdRef.current : null;
        const firstFolder = roots[0];
        const folderId =
          preservedFolderId != null && flattenFolders(roots).some((f) => f.Id === preservedFolderId)
            ? preservedFolderId
            : firstFolder?.Id ?? navDto?.TransMgtRootFolderId ?? null;

        if (folderId != null) {
          setCurrentFolderId(Number(folderId));
          currentFolderIdRef.current = Number(folderId);
        }

        const defaultView =
          views.find((v: any) => v.Id === currentViewIdRef.current) ??
          views.find((v: any) => v.IsDefaultView) ??
          views[0];

        if (defaultView) {
          await applyView(defaultView, folderId != null ? Number(folderId) : null);
        }
      } catch (e) {
        appHelper.debugLog('TransactionFolderNavigation refreshFolderNavigation', e);
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [transactionId, dispatch, flattenFolders, applyView],
  );

  useEffect(() => {
    currentFolderIdRef.current = currentFolderId;
  }, [currentFolderId]);

  useEffect(() => {
    currentViewIdRef.current = currentViewId;
  }, [currentViewId]);

  useEffect(() => {
    refreshFolderNavigation(false);
  }, [transactionId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    const flex = folderGridRef.current?.control;
    if (!flex || !foldersCV || currentFolderId == null) return;
    const items = foldersCV.items as FolderDto[];
    const rowIndex = items.findIndex((f) => f.Id === currentFolderId);
    if (rowIndex < 0 || flex.selection?.row === rowIndex) return;
    suppressFolderSelectionRef.current = true;
    flex.select(rowIndex, 0);
    suppressFolderSelectionRef.current = false;
  }, [foldersCV, currentFolderId]);

  useEffect(() => {
    if (!folderContextMenu.visible) return;
    const handler = (e: MouseEvent) => {
      if (folderContextMenuRef.current?.contains(e.target as Node)) return;
      setFolderContextMenu({ visible: false, x: 0, y: 0, folder: null });
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [folderContextMenu.visible]);

  const closeFolderContextMenu = useCallback(() => {
    setFolderContextMenu({ visible: false, x: 0, y: 0, folder: null });
  }, []);

  const openFolderContextMenu = useCallback((e: React.MouseEvent, folder: FolderDto) => {
    e.preventDefault();
    e.stopPropagation();
    if (folder.IsFolderReadonly) return;
    const { x, y } = clampContextMenuPosition(
      e.clientX,
      e.clientY,
      FOLDER_CONTEXT_MENU_ESTIMATED_WIDTH,
      FOLDER_CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setFolderContextMenu({ visible: true, x, y, folder });
  }, []);

  useRefineContextMenuField(folderContextMenu.visible, folderContextMenuRef, setFolderContextMenu);

  const openFolderEditPopup = useCallback(
    (mode: 'create' | 'rename' | 'defaultView', folder: Partial<FolderDto> | null, parentId: number | null) => {
      setFolderEditPopup({
        visible: true,
        mode,
        folder: folder ?? { Name: 'New Folder', DefaultViewId: null },
        parentId,
      });
      closeFolderContextMenu();
    },
    [closeFolderContextMenu],
  );

  const closeFolderEditPopup = useCallback(() => {
    setFolderEditPopup({ visible: false, mode: 'rename', folder: null, parentId: null });
  }, []);

  const createFolderFromMenu = useCallback(() => {
    const parent = folderContextMenu.folder;
    openFolderEditPopup(
      'create',
      {
        Name: 'New Folder',
        ParentId: parent?.Id ?? undefined,
        TransactionId: Number(transactionId),
        FolderType: parent?.FolderType,
      },
      parent?.Id ?? null,
    );
  }, [folderContextMenu.folder, openFolderEditPopup, transactionId]);

  const editFolderFromMenu = useCallback(
    async (isDefaultViewOnly?: boolean) => {
      const folder = folderContextMenu.folder;
      if (!folder?.Id) return;
      dispatch(setIsBusy());
      try {
        const data = await appFolderNavigationService.retrieveOneAppSeFolderExDto(String(folder.Id));
        if (data) {
          openFolderEditPopup(isDefaultViewOnly ? 'defaultView' : 'rename', data, folder.ParentId ?? null);
        }
      } catch (e) {
        appHelper.debugLog('TransactionFolderNavigation editFolder', e);
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [folderContextMenu.folder, openFolderEditPopup, dispatch],
  );

  const saveFolderFromPopup = useCallback(async () => {
    const { folder, mode, parentId } = folderEditPopup;
    if (!folder) return;
    dispatch(setIsBusy());
    try {
      const payload: any = {
        ...folder,
        ParentId: mode === 'create' ? (parentId ?? undefined) : folder.ParentId,
        TransactionId: Number(transactionId),
      };
      const data = await appFolderNavigationService.saveAppSeFolder(payload);
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful !== false) {
        closeFolderEditPopup();
        await refreshFolderNavigation(true);
      }
    } catch (e) {
      appHelper.debugLog('TransactionFolderNavigation saveFolder', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderEditPopup, closeFolderEditPopup, refreshFolderNavigation, transactionId, dispatch, showValidationMessages]);

  const deleteFolderFromMenu = useCallback(async () => {
    const folderId = folderContextMenu.folder?.Id;
    if (!folderId) return;
    if (!window.confirm('Are you sure you want to delete this folder?')) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.deleteAppSeFolder(String(folderId));
      closeFolderContextMenu();
      if (currentFolderId === folderId) {
        const parentId = folderContextMenu.folder?.ParentId ?? null;
        if (parentId != null) await changeFolder(parentId);
      }
      await refreshFolderNavigation(true);
    } catch (e) {
      appHelper.debugLog('TransactionFolderNavigation deleteFolder', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderContextMenu.folder, currentFolderId, closeFolderContextMenu, refreshFolderNavigation, changeFolder, dispatch]);

  const cutFolderFromMenu = useCallback(() => {
    const folderId = folderContextMenu.folder?.Id;
    if (folderId) setCutFolderId(folderId);
    closeFolderContextMenu();
  }, [folderContextMenu.folder, closeFolderContextMenu]);

  const pasteFolderFromMenu = useCallback(async () => {
    const targetFolderId = folderContextMenu.folder?.Id;
    if (!targetFolderId || !cutFolderId) return;
    dispatch(setIsBusy());
    try {
      await appFolderNavigationService.pasteAppSeFolder(String(cutFolderId), String(targetFolderId));
      setCutFolderId(null);
      closeFolderContextMenu();
      await refreshFolderNavigation(true);
    } catch (e) {
      appHelper.debugLog('TransactionFolderNavigation pasteFolder', e);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folderContextMenu.folder, cutFolderId, closeFolderContextMenu, refreshFolderNavigation, dispatch]);

  const handleCreate = () => {
    const folderId = currentFolderIdRef.current ?? currentFolderId;
    if (folderId == null || !currentViewDto) return;
    if (isTemplateMode) {
      const payload = buildFolderNavigationFormGroupCreatePayload(currentViewDto, folderId, viewDataList);
      if (!payload) {
        window.alert('No Create link target configured on template view.');
        return;
      }
      cacheFormGroupSession(dispatch, payload.sessionKey, payload.sessionData);
      addTabAndNavigate('TransactionFormGroup', payload.tabTitle, {
        id: payload.sessionKey,
        preserveLinkTabTitle: true,
        param2: payload.param2,
      });
      return;
    }
    openFolderNavigationFormMasterDetail(addTabAndNavigate, transactionId, currentViewDto, folderId, 'Create');
  };

  const handleOpenSelected = () => {
    const row = selectedRows[0];
    if (!row || !currentViewDto) return;
    if (isTemplateMode) {
      const editTarget =
        currentViewDto.AppFormLinkTargetList?.find(
          (lt: any) => lt.ActionType === EmAppLinkTargetActionType.Edit,
        ) ?? currentViewDto.AppFormLinkTargetList?.[0];
      if (!editTarget) {
        window.alert('No Edit link target configured.');
        return;
      }
      const { tabTitle, sessionKey, param2, sessionData } = buildFormGroupOpenPayload(
        editTarget,
        row,
        currentViewDto,
        viewDataList,
      );
      cacheFormGroupSession(dispatch, sessionKey, sessionData);
      addTabAndNavigate('TransactionFormGroup', tabTitle, {
        id: sessionKey,
        preserveLinkTabTitle: true,
        param2,
      });
      return;
    }
    const editTarget = currentViewDto.AppFormLinkTargetList?.find(
      (lt: any) => lt.ActionType === EmAppLinkTargetActionType.Edit,
    );
    if (editTarget) {
      openFolderNavigationFormMasterDetail(addTabAndNavigate, transactionId, currentViewDto, currentFolderId, 'Edit', row, editTarget);
    }
  };

  const dataModel = useMemo(
    () => ({
      currentSearchName: runtimeContext?.TemplateSearchName ?? currentViewDto?.Name,
      dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue: {},
      forceTransactionFormGroup: isTemplateMode,
    }),
    [runtimeContext, currentViewDto, isTemplateMode],
  );

  return (
    <div className="w-full h-full flex overflow-hidden">
      <div className="flex flex-col mr-1 relative flex-none" style={{ width: folderTreeWidth, minWidth: 200 }}>
        <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
          <div className={`text-xs font-semibold ${theme.title}`}>
            <i className="fa-solid fa-folder-open mr-1" />
            Folders
          </div>
          <button type="button" className={`w-6 h-6 text-xs ${theme.button_default}`} onClick={() => refreshFolderNavigation(true)} title="Refresh">
            <i className="fa-solid fa-rotate" />
          </button>
        </div>
        <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
          {foldersCV && (
            <FlexGrid
              ref={folderGridRef}
              itemsSource={foldersCV}
              selectionMode="Row"
              headersVisibility="Column"
              autoGenerateColumns={false}
              isReadOnly
              style={{ height: '100%' }}
              selectionChanged={(s: any) => {
                if (suppressFolderSelectionRef.current) return;
                const flex = s?.control ?? s;
                const row = flex?.selection?.row;
                const item = row != null ? flex?.rows?.[row]?.dataItem : null;
                if (item?.Id && item.Id !== currentFolderIdRef.current) {
                  changeFolder(item.Id);
                }
              }}
            >
              <FlexGridColumn binding="Name" header="Folder" width="*">
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item as FolderDto;
                    const isSelected = item.Id === currentFolderId;
                    const showContextBtn = !item.IsFolderReadonly;
                    return (
                      <div
                        className={`flex items-center justify-between py-1 group ${isSelected ? 'font-semibold' : ''}`}
                        style={{ paddingLeft: (item.Level || 0) * 16 }}
                        onContextMenu={showContextBtn ? (e) => openFolderContextMenu(e, item) : undefined}
                      >
                        <div className="flex items-center min-w-0 w-1 flex-auto">
                          <i className="fa-solid fa-folder mr-2 text-yellow-500 flex-none" />
                          <span className="truncate text-xs">{item.Name}</span>
                        </div>
                        {showContextBtn && (
                          <button
                            type="button"
                            className={`flex-none w-6 h-6 opacity-0 group-hover:opacity-100 ${theme.button_default} rounded px-1`}
                            onClick={(e) => openFolderContextMenu(e, item)}
                            title="More Options"
                          >
                            <i className="fa-solid fa-ellipsis-vertical text-xs" aria-hidden />
                          </button>
                        )}
                      </div>
                    );
                  }}
                />
              </FlexGridColumn>
            </FlexGrid>
          )}
        </div>
      </div>

      <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
        <div className={`flex items-center gap-2 px-3 py-2 border-b ${theme.mainContentSection}`}>
          {viewList.length > 1 && (
            <select
              className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              value={currentViewId ?? ''}
              onChange={(e) => {
                const v = viewList.find((x) => String(x.Id) === e.target.value);
                if (v) changeView(v);
              }}
            >
              {viewList.map((v) => (
                <option key={v.Id} value={v.Id}>
                  {v.Name || v.Display || `View ${v.Id}`}
                </option>
              ))}
            </select>
          )}
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleCreate}>
            <i className="fa-solid fa-plus mr-1" />
            Create
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={handleOpenSelected}
            disabled={selectedRows.length === 0}
          >
            <i className="fa-solid fa-folder-open mr-1" />
            Open
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => currentFolderId != null && currentViewId != null && loadFolderContent(currentFolderId, currentViewId)}
          >
            <i className="fa-solid fa-rotate mr-1" />
            Refresh
          </button>
          {isTemplateMode && runtimeContext?.TemplateSearchName && (
            <span className={`text-xs ml-auto ${theme.label}`}>Template: {runtimeContext.TemplateSearchName}</span>
          )}
        </div>
        <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
          {currentViewDto ? (
            <SearchView
              viewDto={currentViewDto}
              viewDataList={viewDataList}
              dataModel={dataModel}
              onExecuteSearch={async () => {
                if (currentFolderId != null && currentViewId != null) {
                  await loadFolderContent(currentFolderId, currentViewId);
                }
              }}
              onSelectionChanged={(rows) => setSelectedRows(rows)}
            />
          ) : (
            <p className={`p-4 text-xs ${theme.label}`}>Loading view...</p>
          )}
        </div>
      </div>

      {folderContextMenu.visible && folderContextMenu.folder && (
        <div
          ref={folderContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[180px]`}
          style={{ left: folderContextMenu.x, top: folderContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={createFolderFromMenu}
          >
            <i className="fa-solid fa-plus w-5" />
            <span className="ml-2">Create Folder</span>
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => editFolderFromMenu(false)}
          >
            <i className="fa-solid fa-pen w-5" />
            <span className="ml-2">Rename Folder</span>
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={deleteFolderFromMenu}
          >
            <i className="fa-solid fa-times w-5" />
            <span className="ml-2">Delete Folder</span>
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={cutFolderFromMenu}
          >
            <i className="fa-solid fa-scissors w-5" />
            <span className="ml-2">Cut Folder</span>
          </button>
          {cutFolderId != null && (
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={pasteFolderFromMenu}
            >
              <i className="fa-solid fa-clipboard w-5" />
              <span className="ml-2">Paste Folder</span>
            </button>
          )}
          <div className={`border-t my-1 ${theme.mainContentSection}`} />
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => editFolderFromMenu(true)}
          >
            <i className="fa-solid fa-tag w-5" />
            <span className="ml-2">Set Default View</span>
          </button>
        </div>
      )}

      {folderEditPopup.visible && folderEditPopup.folder && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/30">
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl w-[350px] max-w-[95vw] p-4 border`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3">
              <span className={`text-sm font-semibold ${theme.title}`}>
                {folderEditPopup.mode === 'create'
                  ? 'Create Folder'
                  : folderEditPopup.mode === 'defaultView'
                    ? 'Set Default View'
                    : 'Rename Folder'}
              </span>
              <button type="button" className={`${theme.button_default} w-6 h-6 rounded`} onClick={closeFolderEditPopup}>
                &times;
              </button>
            </div>
            {folderEditPopup.mode !== 'defaultView' && (
              <div className="mb-3">
                <label className={`block text-xs ${theme.label} mb-1`}>Folder Name</label>
                <input
                  type="text"
                  value={folderEditPopup.folder.Name ?? ''}
                  onChange={(e) =>
                    setFolderEditPopup((p) => ({ ...p, folder: { ...p.folder!, Name: e.target.value } }))
                  }
                  className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox}`}
                />
              </div>
            )}
            {folderEditPopup.mode === 'defaultView' && (
              <div className="mb-3">
                <label className={`block text-xs ${theme.label} mb-1`}>Default View</label>
                <select
                  value={folderEditPopup.folder.DefaultViewId ?? ''}
                  onChange={(e) =>
                    setFolderEditPopup((p) => ({
                      ...p,
                      folder: { ...p.folder!, DefaultViewId: e.target.value ? Number(e.target.value) : null },
                    }))
                  }
                  className={`w-full h-8 px-2 text-sm border rounded ${theme.inputBox}`}
                >
                  <option value="">Default</option>
                  {viewList.map((v) => (
                    <option key={v.Id} value={v.Id}>
                      {v.Name || v.Display || `View ${v.Id}`}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <div className="flex justify-end gap-2 mt-3">
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeFolderEditPopup}>
                Cancel
              </button>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={saveFolderFromPopup}>
                <i className="fa-solid fa-floppy-disk mr-1" />
                Save
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default TransactionFolderNavigation;
