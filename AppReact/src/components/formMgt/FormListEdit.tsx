/**
 * FormListEdit – List-edit transaction form (TransactionOrganizedType = List).
 * Migrated from AngularJS: formListEditCtrl.js + TransactionForm.cshtml (_ListEditLayoutForm).
 * Reference: C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Form\formListEditCtrl.js
 */
import React, { useState, useEffect, useRef, useCallback, useMemo, Suspense, lazy } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { FlexGridDetail } from '@mescius/wijmo.react.grid.detail';
import { CollectionView } from '@mescius/wijmo';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import ApplicationFormBuilder from '../transaction/ApplicationFormBuilder';
import { updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTheme } from '../../redux/hooks/useTheme';
import FlexGridAddOn from '../common/FlexGridAddOn';
import RgbColorSwatch from '../common/RgbColorSwatch';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { fileThumbnailUrl, fileLatestUrl, downloadFileById } from '../../webapi/fileEndpoints';
import { DataMap } from '@mescius/wijmo.grid';
import { useSelector, Provider } from 'react-redux';
import type { RootState } from '../../redux/store';
import { store } from '../../redux/store';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';
import { appFileService } from '../../webapi/appfilesvc';
import { searchSvc } from '../../webapi/searchSvc';
import FileUploader from '../common/FileUploader';
import { FolderNavigation } from '../folderNavigation';
import appHelper from '../../helper/appHelper';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';
import { isAdminUserFromContext, isEnableConfigurationModeForUser } from '../../helper/adminPermissionHelper';
import { isRuntimeTransactionFieldVisible } from './FormMasterDetail/MasterDetailFlexLayoutForm/flexLayoutItemHelper';
import DataGridLayout from './FormMasterDetail/MasterDetailFlexLayoutForm/DataGridLayout';
import AppSearch, { type AppSearchHandle } from '../search/AppSearch';
import FormMasterDetail from './FormMasterDetail';
import { EmbeddedLinkedPopupFrame } from './EmbeddedLinkedPopupFrame';
import {
  applyLinkTargetMasterDataSelectionToRow,
  isLinkedSearchGridSingleSelection,
  isLinkedSearchPopupConfirmClose,
  type MasterDataPickerContext,
} from './linkedSearchUtils';
import { buildLinkTargetTabTitle } from '../../utils/linkTargetTabTitle';

/** Lazy avoids static circular import when opening List Edit inside List Edit popup. */
const ListEditNavPopupLazy = lazy(() => import('./FormListEdit'));

/** Walk `AppTransactionUnitList` tree so List Edit child grids get full unit metadata (link targets, linked searches). */
function findTransactionUnitById(units: any[] | undefined, idStr: string): any | null {
  if (idStr == null || idStr === '' || !Array.isArray(units)) return null;
  for (const u of units) {
    if (!u) continue;
    const uid = String(u.Id ?? u.TransactionUnitId ?? u.unitId ?? '');
    if (uid === idStr) return u;
    const nested = findTransactionUnitById(u.Children, idStr);
    if (nested) return nested;
  }
  return null;
}

function findTransactionUnitInTransactionExDto(tx: any | null | undefined, idStr: string): any | null {
  if (!tx || idStr == null || idStr === '') return null;
  return (
    findTransactionUnitById(tx.AppTransactionUnitList, idStr) ??
    findTransactionUnitById(tx.AppTransactionUnitTree, idStr)
  );
}

/** Map linked-search grid selection into a list row (IsSingleSelectedRow). */
function applyLinkedSearchResultToListRow(result: any, linkedSearch: any, rowItem: any, unitFields: any[]) {
  if (!result || !rowItem) return;
  const viewMappings = Array.isArray(linkedSearch?.AppTransactionUnitSearchViewFieldMappingList)
    ? linkedSearch.AppTransactionUnitSearchViewFieldMappingList
    : [];
  const fieldIdToDbName = new Map<string, string>();
  unitFields.forEach((f: any) => {
    if (f?.Id != null && f?.DataBaseFieldName) fieldIdToDbName.set(String(f.Id), String(f.DataBaseFieldName));
  });
  const dictViewValues = result?.DictViewColumnIDKeyValue ?? {};
  rowItem.DictOneToOneFields = { ...(rowItem.DictOneToOneFields ?? {}) };
  viewMappings.forEach((m: any) => {
    const searchViewFieldId = m?.SearchViewFieldId != null ? String(m.SearchViewFieldId) : '';
    const transactionFieldId = m?.TransactionFieldId != null ? String(m.TransactionFieldId) : '';
    const dbFieldName = fieldIdToDbName.get(transactionFieldId);
    if (!searchViewFieldId || !dbFieldName) return;
    const value =
      dictViewValues?.[searchViewFieldId] ?? dictViewValues?.[Number(searchViewFieldId)] ?? null;
    rowItem.DictOneToOneFields[dbFieldName] = value;
  });
  rowItem.IsDirty = true;
}

export type FormListEditEmbeddedProps = {
  embeddedTransactionId: number;
  embeddedParam2?: Record<string, unknown>;
  /**
   * Search hierarchical mass update: use MassUpdateAppListDataDto from search result
   * (Angular directivOutercontrol.listEditDataDto) instead of loading all ListEdit rows.
   */
  embeddedListEditDataDto?: any | null;
  /** After ListEdit mass-update save (Angular saveCallBack → re-run search). */
  onMassUpdateSaved?: () => void | Promise<void>;
  /** Angular isHideHeaderAndFooter when embedded in Search Mass Update. */
  hideHeader?: boolean;
};

type FormListEditProps = {
  embedded?: FormListEditEmbeddedProps | null;
};

/** If API returned a flat unit list with ParentTransactionUnitId, rebuild Children for FlexGridDetail. */
function ensureTransactionUnitChildrenTree(tx: any): any {
  if (!tx || !Array.isArray(tx.AppTransactionUnitList) || tx.AppTransactionUnitList.length === 0) {
    return tx;
  }
  const units = tx.AppTransactionUnitList as any[];
  const alreadyNested = units.some((u) => Array.isArray(u?.Children) && u.Children.length > 0);
  if (alreadyNested) return tx;

  const byId = new Map<string, any>();
  units.forEach((u) => {
    if (u?.Id == null) return;
    byId.set(String(u.Id), { ...u, Children: Array.isArray(u.Children) ? [...u.Children] : [] });
  });
  const roots: any[] = [];
  byId.forEach((u) => {
    const parentId = u.ParentTransactionUnitId ?? u.ParentTransactionUnitID;
    const isSibling = u.IsMasterSiblingUnit === true;
    if (!isSibling && parentId != null && byId.has(String(parentId))) {
      byId.get(String(parentId)).Children.push(u);
    } else if (!isSibling) {
      roots.push(u);
    }
  });
  if (roots.length === 0) return tx;
  return { ...tx, AppTransactionUnitList: roots };
}

const FormListEdit: React.FC<FormListEditProps> = ({ embedded = null }) => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError, getErrorMessage } = useErrorMessage();
  const { param } = useParams<{ param: string }>();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const isEmbedded = embedded != null && embedded.embeddedTransactionId != null;
  const hideHeader = Boolean(embedded?.hideHeader);

  const [paramObj, setParamObj] = useState<{ id?: number; param1?: string; param2?: any }>(() => {
    if (isEmbedded && embedded) {
      return {
        id: embedded.embeddedTransactionId,
        param2: embedded.embeddedParam2 ?? {},
      };
    }
    if (param) {
      try {
        const decoded = decodeURIComponent(param);
        return JSON.parse(decoded);
      } catch {
        return { id: Number(param) || undefined };
      }
    }
    return {};
  });

  useEffect(() => {
    if (isEmbedded && embedded) {
      setParamObj({
        id: embedded.embeddedTransactionId,
        param2: embedded.embeddedParam2 ?? {},
      });
      return;
    }
    if (!param) {
      setParamObj({});
      return;
    }
    try {
      const decoded = decodeURIComponent(param);
      setParamObj(JSON.parse(decoded));
    } catch {
      setParamObj({ id: Number(param) || undefined });
    }
  }, [param, isEmbedded, embedded?.embeddedTransactionId, embedded?.embeddedParam2]);

  const transactionId = paramObj.id != null ? Number(paramObj.id) : null;
  const param2Obj = (() => {
    const p2 = paramObj.param2;
    if (p2 == null) return {};
    if (typeof p2 === 'string') {
      try {
        return JSON.parse(p2);
      } catch {
        return {};
      }
    }
    return p2;
  })();

  const [controllerModel] = useState(() => ({
    transactionId,
    param2Obj,
    isEnableFormConfigButtons: param2Obj?.isEnableFormConfigButtons ?? true,
    isAutoReopenCurrentFormAfterChange: param2Obj?.isAutoReopenCurrentFormAfterChange !== false,
  }));

  const [dataModel, setDataModel] = useState<{
    currentFormData: any;
    currentFormStructure: any;
    transactionExDto: any;
    listDataSource: CollectionView | null;
    errorMessages: { error: string[]; warning: string[]; message: string[] };
    isLoading: boolean;
    dictFieldEntityDataMap: Record<string, any>;
  }>({
    currentFormData: null,
    currentFormStructure: null,
    transactionExDto: null,
    listDataSource: null,
    errorMessages: { error: [], warning: [], message: [] },
    isLoading: false,
    dictFieldEntityDataMap: {},
  });

  const emAppControlTypeEnum = useEnumValues('EmAppControlType');
  const emAppGrandChildEditModeEnum = useEnumValues('EmAppGrandChildEditMode');
  const transactionOrganizedTypeEnum = useEnumValues('EmTransactionOrganizedType');

  type CellMenuKind = 'image' | 'file';
  const CELL_CONTEXT_MENU_ESTIMATED_WIDTH = 190;
  const CELL_CONTEXT_MENU_ESTIMATED_HEIGHT = 180;
  const [cellMenu, setCellMenu] = useState<{
    open: boolean;
    x: number;
    y: number;
    kind: CellMenuKind;
    rowItem: any;
    fieldName: string;
    fileId: number | null;
  } | null>(null);
  type CellMenuState = NonNullable<typeof cellMenu>;
  const cellMenuRef = useRef<HTMLDivElement | null>(null);

  const openCellMenuAt = useCallback(
    (
      anchorX: number,
      anchorY: number,
      menu: Omit<CellMenuState, 'open' | 'x' | 'y'>
    ) => {
      const { x, y } = clampContextMenuPosition(
        anchorX,
        anchorY,
        CELL_CONTEXT_MENU_ESTIMATED_WIDTH,
        CELL_CONTEXT_MENU_ESTIMATED_HEIGHT
      );
      setCellMenu({ open: true, x, y, ...menu });
    },
    []
  );

  const refineCellMenu = useCallback((updater: React.SetStateAction<CellMenuState>) => {
    setCellMenu((prev) => {
      if (!prev?.open) return prev;
      const next = typeof updater === 'function' ? updater(prev) : updater;
      return { ...prev, x: next.x, y: next.y };
    });
  }, []);

  useRefineContextMenuField(!!cellMenu?.open, cellMenuRef, refineCellMenu);

  const [pendingLibraryFileId, setPendingLibraryFileId] = useState<number | null>(null);
  const [imagePreviewState, setImagePreviewState] = useState<{ open: boolean; fileId: number | null }>({
    open: false,
    fileId: null,
  });
  const [uploadState, setUploadState] = useState<{
    open: boolean;
    kind: CellMenuKind;
    rowItem: any;
    fieldName: string;
  } | null>(null);
  const [libraryState, setLibraryState] = useState<{
    open: boolean;
    kind: CellMenuKind;
    rowItem: any;
    fieldName: string;
  } | null>(null);
  const [grandChildPopupState, setGrandChildPopupState] = useState<{
    open: boolean;
    unitId: number | string | null;
    rowItem: any | null;
    parentRowIndex: number;
  }>({
    open: false,
    unitId: null,
    rowItem: null,
    parentRowIndex: -1,
  });

  /** When grand-child edit is in modal popup, do not call FlexGridDetail.showDetail (nothing to reopen). */
  const grandChildPopupOpenRef = useRef(false);

  useEffect(() => {
    grandChildPopupOpenRef.current = Boolean(
      grandChildPopupState.open && grandChildPopupState.rowItem != null && grandChildPopupState.unitId != null
    );
  }, [grandChildPopupState.open, grandChildPopupState.rowItem, grandChildPopupState.unitId]);

  const blankRowRef = useRef<any>(null);
  const flexGridRef = useRef<any>(null);
  const flexGridDetailRef = useRef<any>(null);
  /** Stable empty CV so FlexGrid `itemsSource` is never undefined; avoids unmount during refresh (isLoading) which races Wijmo updateProps → null.selectionMode. */
  const stableEmptyListCvRef = useRef<CollectionView<any> | null>(null);
  if (stableEmptyListCvRef.current == null) {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.clear();
    stableEmptyListCvRef.current = cv;
  }
  const [configPopupOpen, setConfigPopupOpen] = useState(false);

  /** Root list unit: link targets / linked searches (toolbar — Angular list-edit menu parity). */
  const [fetchedListRootLinkTargets, setFetchedListRootLinkTargets] = useState<any[]>([]);
  const [fetchedListRootLinkedSearches, setFetchedListRootLinkedSearches] = useState<any[]>([]);
  const [listRootLinkTargetPopupState, setListRootLinkTargetPopupState] = useState<{
    title: string;
    routeBasePath: string;
    paramObj: any;
    width?: number | null;
    height?: number | null;
    popupZIndex: number;
    /** Master data picker (usage type 5): Confirm maps selection → row. */
    showConfirmClose?: boolean;
    pickerContext?: MasterDataPickerContext | null;
  } | null>(null);
  const [listRootSearchPopupState, setListRootSearchPopupState] = useState<{
    title: string;
    width?: number | null;
    height?: number | null;
    paramObj: any;
    popupZIndex: number;
    /** Link to search — update single row: show Confirm &amp; Close. */
    showConfirmClose?: boolean;
    linkedSearch?: any;
  } | null>(null);
  const listEditLinkedSearchRef = useRef<AppSearchHandle | null>(null);
  const listRootLinkTargetMdRef = useRef<AppSearchHandle | null>(null);

  const listEditRootUnitIdForNavFetch =
    dataModel.transactionExDto?.AppTransactionUnitList?.[0]?.Id ??
    dataModel.currentFormStructure?.AppTransactionUnitList?.[0]?.Id;

  useEffect(() => {
    let cancelled = false;
    const id = listEditRootUnitIdForNavFetch != null ? String(listEditRootUnitIdForNavFetch) : '';
    if (!id) return;

    Promise.all([
      appTransactionService.retrieveOneTransactionUnitLinkTargetList(id).catch(() => []),
      appTransactionService.retrieveOneAppTransactionUnitLinkedSearchList(id).catch(() => []),
    ]).then(([ltList, lsList]) => {
      if (cancelled) return;
      setFetchedListRootLinkTargets(Array.isArray(ltList) ? ltList : []);
      setFetchedListRootLinkedSearches(Array.isArray(lsList) ? lsList : []);
    });

    return () => {
      cancelled = true;
    };
  }, [listEditRootUnitIdForNavFetch]);

  const markChange = useCallback(() => {
    setDataModel((prev) => ({
      ...prev,
      currentFormData: {
        ...prev.currentFormData,
        IsDirty: true,
      },
    }));
  }, []);

  const closeCellMenu = useCallback(() => setCellMenu(null), []);

  // Close cell menu when clicking outside.
  useEffect(() => {
    if (!cellMenu?.open) return;
    const onDocMouseDown = (e: MouseEvent) => {
      const target = e.target as Node | null;
      if (target && cellMenuRef.current && cellMenuRef.current.contains(target)) return;
      setCellMenu(null);
    };
    document.addEventListener('mousedown', onDocMouseDown);
    return () => document.removeEventListener('mousedown', onDocMouseDown);
  }, [cellMenu?.open]);

  const buildFileUrl = useCallback((fileId: number | string | null | undefined) => {
    const id = fileId != null ? Number(fileId) : null;
    if (!id) return null;
    return fileLatestUrl(id);
  }, []);

  const fileStorageRootFolderId =
    dataModel?.currentFormStructure?.FileStorageRootFolderId ?? dataModel?.currentFormData?.FileStorageRootFolderId;
  const sysFileTransactionId = userContext?.DictAppSetup?.SystemDefinedFileTransactionId ?? null;
  const defaultCategoryId_company = 3;

  // Refs so wijmo cell templates always see the latest dataModel.
  const dataModelRef = useRef<any>(dataModel);
  useEffect(() => {
    dataModelRef.current = dataModel;
  }, [dataModel]);

  const updateRowFileValue = useCallback(
    (rowItem: any, fieldName: string, newFileId: number | null, fileName?: string) => {
      const currentDataModel = dataModelRef.current;
      if (!rowItem || !currentDataModel?.currentFormData) return;

      rowItem.DictOneToOneFields = rowItem.DictOneToOneFields ?? {};
      rowItem.DictOneToOneFields[fieldName] = newFileId;
      rowItem[fieldName] = newFileId;
      rowItem.IsDirty = true;

      const nextDictDoc = { ...(currentDataModel.currentFormData?.DictDocumentIdFileCode ?? {}) };
      if (newFileId && fileName) {
        nextDictDoc[String(newFileId)] = fileName;
      }

      setDataModel((prev) => ({
        ...prev,
        currentFormData: {
          ...prev.currentFormData,
          IsDirty: true,
          DictDocumentIdFileCode: nextDictDoc,
        },
      }));

      try {
        currentDataModel.listDataSource?.refresh();
      } catch {
        // ignore
      }

      // Force Wijmo grid to redraw cells.
      try {
        const flex = flexGridRef.current?.control ?? flexGridRef.current;
        flex?.invalidate?.();
        flex?.refresh?.();
      } catch {
        // ignore
      }

      markChange();
    },
    [markChange]
  );

  const assignUploadedFileToCell = useCallback(
    async (kind: CellMenuKind, rowItem: any, fieldName: string, fileId: number, fallbackName?: string) => {
      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(fileId));
        const fileCode = dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? fallbackName ?? '';
        updateRowFileValue(rowItem, fieldName, fileId, fileCode ? String(fileCode) : undefined);
      } catch {
        updateRowFileValue(rowItem, fieldName, fileId, fallbackName);
      }
      void kind; // reserved for future kind-specific behavior
    },
    [updateRowFileValue]
  );

  const loadData = useCallback(
    async (_isRefresh = false) => {
      if (!transactionId) return;
      try {
        dispatch(setIsBusy());
        setDataModel((prev) => ({ ...prev, isLoading: true }));

        const embeddedListDto = embedded?.embeddedListEditDataDto;
        const [formStructureData, listDataResponseRaw, transactionExDtoRaw] = await Promise.all([
          appTransactionService.getFormStructure(transactionId),
          embeddedListDto != null
            ? Promise.resolve(embeddedListDto)
            : appTransactionService.getListEditFormData(transactionId),
          dynamicLayoutService.getTransactionForm(transactionId).catch(() => null),
        ]);

        const listDataResponse = listDataResponseRaw ?? {};
        const listData = listDataResponse?.ListData ?? [];
        const blankRow = listDataResponse?.EditCloneAppChildDataDto ?? null;
        blankRowRef.current = blankRow;

        const dictFieldEntityDataMap: Record<string, any> = {};
        const dictEntityDataSource = formStructureData?.DictStandAloneEntityDataSource ?? {};
        const dictFiledIDMappingEntityID = formStructureData?.DictStandAloneFiledIDMappingEntityID ?? {};
        for (const fieldId in dictFiledIDMappingEntityID) {
          const entityId = dictFiledIDMappingEntityID[fieldId];
          const entityData = dictEntityDataSource[entityId];
          if (entityData && Array.isArray(entityData)) {
            dictFieldEntityDataMap[fieldId] = new DataMap(entityData, 'Id', 'Display');
          }
        }

        const cv = new CollectionView(listData);
        cv.sortDescriptions.clear();

        const currentFormData = {
          ...listDataResponse,
          TransactionId: transactionId,
          ListData: listData,
          IsDirty: false,
          IsMassUpdate: listDataResponse?.IsMassUpdate ?? false,
          MassUpdateViewId: listDataResponse?.MassUpdateViewId,
        };

        const transactionExDto = ensureTransactionUnitChildrenTree(transactionExDtoRaw);
        const currentFormStructure = ensureTransactionUnitChildrenTree(formStructureData);

        const displayName =
          transactionExDto?.TransactionName ?? currentFormStructure?.TransactionName ?? listDataResponse?.TransactionName;

        setDataModel({
          currentFormData,
          currentFormStructure,
          transactionExDto: transactionExDto ?? null,
          listDataSource: cv,
          errorMessages: { error: [], warning: [], message: [] },
          isLoading: false,
          dictFieldEntityDataMap,
        });

        if (displayName && !isEmbedded) dispatch(updateCurrentTabLabel(displayName));
      } catch (err) {
        appHelper.debugLog('FormListEdit loadData error', err);
        showError('Failed to load list edit data: ' + (err as Error).message);
        setDataModel((prev) => ({ ...prev, isLoading: false }));
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [transactionId, dispatch, showError, embedded?.embeddedListEditDataDto, isEmbedded]
  );

  useEffect(() => {
    if (transactionId) loadData();
  }, [transactionId, loadData]);

  const addChildNew = useCallback(() => {
    const blankRow = blankRowRef.current;
    const cv = dataModel.listDataSource;
    if (!cv || !dataModel.currentFormData) return;

    const newItem = blankRow ? JSON.parse(JSON.stringify(blankRow)) : { UIId: appHelper.guid(), DictOneToOneFields: {}, DictOneToManyFields: {} };
    newItem.UIId = newItem.UIId || appHelper.guid();
    if (!newItem.DictOneToOneFields) newItem.DictOneToOneFields = {};
    if (newItem.DictOneToOneFields && typeof newItem.DictOneToOneFields === 'object') {
      newItem.DictOneToOneFields['GUID'] = appHelper.guid();
    }
    if (newItem.DictOneToManyFields && typeof newItem.DictOneToManyFields === 'object') {
      const copy: Record<string, any[]> = {};
      for (const k of Object.keys(newItem.DictOneToManyFields)) {
        copy[k] = Array.isArray(newItem.DictOneToManyFields[k]) ? [...newItem.DictOneToManyFields[k]] : [];
      }
      newItem.DictOneToManyFields = copy;
    }
    (cv as any).sourceCollection.push(newItem);
    cv.refresh();

    setDataModel((prev) => ({
      ...prev,
      currentFormData: prev.currentFormData ? { ...prev.currentFormData, IsDirty: true, ListData: (cv as any).sourceCollection ?? prev.currentFormData.ListData } : prev.currentFormData,
    }));
  }, [dataModel.listDataSource, dataModel.currentFormData]);

  const deleteChild = useCallback(() => {
    const flex = flexGridRef.current?.control ?? flexGridRef.current;
    const cv = dataModel.listDataSource;
    if (!flex || !cv || !dataModel.currentFormData) return;

    const rowIndex = flex.selection?.row ?? -1;
    if (rowIndex < 0 || rowIndex >= flex.rows?.length) return;

    const row = flex.rows[rowIndex];
    const dataItem = row?.dataItem;
    if (!dataItem) return;

    const src = (cv as any).sourceCollection;
    if (Array.isArray(src)) src.splice(rowIndex, 1);
    cv.refresh();

    setDataModel((prev) => ({
      ...prev,
      currentFormData: prev.currentFormData ? { ...prev.currentFormData, IsDirty: true, ListData: (cv as any).sourceCollection ?? prev.currentFormData.ListData } : prev.currentFormData,
      errorMessages: { error: [], warning: [], message: [] },
    }));
  }, [dataModel.listDataSource, dataModel.currentFormData]);

  const saveFormData = useCallback(async () => {
    const formData = dataModel.currentFormData;
    if (!formData) return;

    try {
      dispatch(setIsBusy());
      const cv = dataModel.listDataSource;
      const payload = {
        ...formData,
        ListData: cv ? (cv as any).sourceCollection : formData.ListData,
      };

      let result: any;
      if (formData.IsMassUpdate) {
        // Angular formListEditCtrl: SaveMassUpdateResult + IsListEditSimpleMassUpdate
        result = await searchSvc.saveMassUpdateResult({
          SearchViewId: formData.MassUpdateViewId,
          IsListEditSimpleMassUpdate: true,
          MassUpdateAppListDataDto: payload,
        });
      } else {
        result = await appTransactionService.saveListEditFormData(payload);
      }

      if (result?.IsSuccessful === false) {
        const errMsg = result?.ValidationResult ? getErrorMessage(result.ValidationResult) : null;
        if (errMsg && (errMsg.error?.length || errMsg.warning?.length || errMsg.message?.length)) {
          setDataModel((prev) => ({ ...prev, errorMessages: errMsg }));
        } else {
          showError('Failed to save list edit data.');
        }
        return;
      }

      const savedObject = result?.Object;
      if (savedObject && !formData.IsMassUpdate) {
        const listData = savedObject?.ListData ?? payload.ListData;
        if (cv && Array.isArray(listData)) {
          (cv as any).sourceCollection = listData;
          cv.refresh();
        }
        setDataModel((prev) => ({
          ...prev,
          currentFormData: { ...prev.currentFormData, ...savedObject, ListData: listData, IsDirty: false },
          errorMessages: { error: [], warning: [], message: [] },
        }));
      } else {
        setDataModel((prev) => ({
          ...prev,
          currentFormData: prev.currentFormData
            ? { ...prev.currentFormData, IsDirty: false }
            : prev.currentFormData,
          errorMessages: { error: [], warning: [], message: [] },
        }));
      }

      const errMsg = result?.ValidationResult ? getErrorMessage(result.ValidationResult) : null;
      if (errMsg && (errMsg.error?.length || errMsg.warning?.length || errMsg.message?.length)) {
        setDataModel((prev) => ({ ...prev, errorMessages: errMsg }));
      }

      if (formData.IsMassUpdate && embedded?.onMassUpdateSaved) {
        await embedded.onMassUpdateSaved();
      }
    } catch (err) {
      appHelper.debugLog('FormListEdit saveFormData error', err);
      showError('Failed to save: ' + (err as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    dataModel.currentFormData,
    dataModel.listDataSource,
    dispatch,
    showError,
    getErrorMessage,
    embedded,
  ]);

  const rootUnit =
    dataModel.transactionExDto?.AppTransactionUnitList?.[0] ??
    dataModel.currentFormStructure?.AppTransactionUnitList?.[0];

  const rootUnitMergedForNav = useMemo(() => {
    if (!rootUnit?.Id) return rootUnit;
    const tx = dataModel.transactionExDto ?? dataModel.currentFormStructure;
    const full = findTransactionUnitInTransactionExDto(tx, String(rootUnit.Id));
    return full ? { ...rootUnit, ...full } : rootUnit;
  }, [rootUnit, dataModel.transactionExDto, dataModel.currentFormStructure]);

  const listRootLinkTargets =
    (rootUnitMergedForNav?.AppFormLinkTargetList?.length
      ? rootUnitMergedForNav.AppFormLinkTargetList
      : fetchedListRootLinkTargets) || [];
  const listRootLinkedSearches =
    (rootUnitMergedForNav?.AppTransactionUnitLinkedSearchList?.length
      ? rootUnitMergedForNav.AppTransactionUnitLinkedSearchList
      : fetchedListRootLinkedSearches) || [];
  /** FormMainMenus parity: hide ViewSearchResult (Action === 1). */
  const listRootLinkedSearchesMenu = listRootLinkedSearches.filter((ls: any) => ls.Action !== 1);
  const hasListRootUnitNavigation =
    listRootLinkTargets.length > 0 || listRootLinkedSearchesMenu.length > 0;

  // Prefer unit field list (has ControlType / EntityId / IsVisible). Filter by runtime visibility.
  // IMPORTANT: when every field is IsVisible=false (e.g. RegularGrid MU — MU cols live on child),
  // do NOT fall back to DictTransactionUnitIdFiledNameFiledID — that dumps all physical columns
  // as plain headers with no ControlType/Entity (see Trim Tracking MU first-layer bug).
  const unitFieldListRaw =
    rootUnitMergedForNav?.AppTransactionFieldList ?? rootUnit?.AppTransactionFieldList;
  const hasUnitFieldDefinitions =
    Array.isArray(unitFieldListRaw) && unitFieldListRaw.length > 0;
  const fieldsFromUnit = (unitFieldListRaw ?? [])
    .filter((f: any) => isRuntimeTransactionFieldVisible(f))
    .sort((a: any, b: any) => (a.SortOrder ?? 0) - (b.SortOrder ?? 0));

  const fieldsFromStructure = ((): any[] => {
    const st = dataModel.currentFormStructure;
    if (!st?.DictTransactionUnitIdFiledNameFiledID) return [];
    const unitIds = Object.keys(st.DictTransactionUnitIdFiledNameFiledID || {}).map(Number).filter(Boolean);
    const unitId = unitIds[0];
    if (unitId == null) return [];
    const nameToId = st.DictTransactionUnitIdFiledNameFiledID[unitId];
    const idToDisplay = st.DictTransactionUnitIdFieldIdFieldDisplayName?.[unitId];
    if (!nameToId || typeof nameToId !== 'object') return [];
    const dictAll = st.DictAllTransactionField ?? dataModel.transactionExDto?.DictAllTransactionField;
    return Object.entries(nameToId).map(([dataBaseFieldName, id]) => {
      const fromDict = dictAll?.[id as any] ?? dictAll?.[Number(id)] ?? dictAll?.[String(id)];
      const merged = {
        Id: id,
        DataBaseFieldName: dataBaseFieldName,
        DisplayName:
          fromDict?.DisplayName ??
          (idToDisplay && (idToDisplay as Record<number, string>)[Number(id)]) ??
          dataBaseFieldName,
        SortOrder: fromDict?.SortOrder ?? 0,
        Width: fromDict?.Width ?? fromDict?.DisplayWidth ?? 100,
        ControlType: fromDict?.ControlType,
        EntityId: fromDict?.EntityId,
        IsVisible: fromDict?.IsVisible,
        IsFormLayoutVisible: fromDict?.IsFormLayoutVisible,
        IsReadonly: fromDict?.IsReadonly ?? fromDict?.IsReadOnly,
        IsPrimaryKey: fromDict?.IsPrimaryKey,
        TransactionUnitId: fromDict?.TransactionUnitId,
      };
      return merged;
    }).filter((f: any) => isRuntimeTransactionFieldVisible(f));
  })();

  const fields = hasUnitFieldDefinitions ? fieldsFromUnit : fieldsFromStructure;

  const isReadOnly = rootUnit?.IsReadOnly === true;
  const normalizeBool = (v: any): boolean =>
    v === true || v === 1 || String(v).toLowerCase() === 'true';
  // List Edit runs in edit mode (add/edit rows + Save).
  const isEditMode = true;
  const isAllowAccess = dataModel.transactionExDto?.IsAllowAccess !== false;

  // Angular lockField (GetLockingBindField) parity:
  // lockField = IsLockTransaction || DictOneToOneLockFields[field.Id] || DictLockUnitIds[field.TransactionUnitId]
  const isLockTransaction = dataModel.currentFormData?.IsLockTransaction === true;
  const dictOneToOneLockFields = (dataModel.currentFormData?.DictOneToOneLockFields ?? {}) as Record<string, any>;
  const dictLockUnitIds = (dataModel.currentFormData?.DictLockUnitIds ?? {}) as Record<string, any>;

  const getSelectedListRowItem = useCallback(() => {
    const flex = flexGridRef.current?.control ?? flexGridRef.current;
    if (!flex?.collectionView) return null;
    const rowIndex = flex.selection?.row;
    if (typeof rowIndex === 'number' && rowIndex >= 0 && rowIndex < flex.rows.length) {
      const r = flex.rows[rowIndex];
      if (r?.dataItem != null) return r.dataItem;
    }
    const cv = flex.collectionView;
    const cur = cv?.currentItem;
    if (cur) return cur;
    const sc = cv?.sourceCollection as any;
    const n = sc?.length ?? 0;
    for (let i = 0; i < n; i++) {
      if (sc[i]) return sc[i];
    }
    return null;
  }, []);

  const normalizeMainPrefixRouteCode = (rc: string) => (rc || '').replace(/^main\./, '');

  const openListRootNavPopup = useCallback(
    (opts: {
      routeBasePath: string;
      title: string;
      paramObj: any;
      popupWidth?: number | null;
      popupHeight?: number | null;
      showConfirmClose?: boolean;
      pickerContext?: MasterDataPickerContext | null;
    }) => {
      setListRootLinkTargetPopupState({
        title: opts.title,
        routeBasePath: opts.routeBasePath,
        paramObj: opts.paramObj,
        width: opts.popupWidth ?? null,
        height: opts.popupHeight ?? null,
        popupZIndex: appHelper.getNextPopupZIndex(),
        showConfirmClose: opts.showConfirmClose === true,
        pickerContext: opts.pickerContext ?? undefined,
      });
    },
    []
  );

  const LIST_TARGET_ACTION_LIST_ROOT = {
    CreateBlank: 2,
    CreateFromExistingItem: 13,
    Edit: 1,
    Preview: 5,
    Delete: 3,
  };

  const executeListRootUnitLinkTarget = useCallback(
    (linkTarget: any) => {
      if (!linkTarget) return;
      const rowItem = getSelectedListRowItem();
      if (!rowItem) {
        showError('Select a row first, then use Navigate to Form/Search.');
        return;
      }

      const rootDictOneToOneFields = dataModelRef.current?.currentFormData?.DictOneToOneFields ?? {};
      const rowDictOneToOneFields = rowItem?.DictOneToOneFields ?? {};

      if (linkTarget?.SourceConditionColumn) {
        const v = rowDictOneToOneFields?.[linkTarget.SourceConditionColumn];
        if (v === false || v === 0 || v === '0' || v === 'false' || v === 'False' || v == null) return;
      }

      const usageType = Number(linkTarget?.LinkTargetUsageType ?? 0);
      const tabTitleBase = linkTarget.NavigationActionName ?? 'Action';

      if (usageType === 5) {
        const paramId = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

        const resolveParamBySourceColumn = (sourceColumn: any, rootFallback: any) => {
          if (!sourceColumn) return null;
          let colName = String(sourceColumn);
          if (colName.indexOf('RootUnit.') >= 0) colName = colName.substring(9).trim();
          if (String(sourceColumn).indexOf('RootUnit.') >= 0) return rootFallback?.[colName] ?? null;
          return rowDictOneToOneFields?.[colName] ?? null;
        };

        const param1 = resolveParamBySourceColumn(linkTarget.SourceColumn2, rootDictOneToOneFields);
        const param2 = resolveParamBySourceColumn(linkTarget.SourceColumn3, rootDictOneToOneFields);

        const routeCode = normalizeMainPrefixRouteCode(linkTarget.LinkTargetUrlOrRouteCode ?? '');

        const paramObj: any = {};
        const routeLower = routeCode.toLowerCase();
        if (routeLower === 'masterdatamanagement') {
          paramObj.searchId = paramId ?? null;
          paramObj.initialViewId = param1 ?? null;
          paramObj.param2 = param2 ?? null;
          paramObj.isLinkedSearch = true;
          paramObj.isSingleSelection = true;
        } else {
          if (paramId != null) paramObj.id = paramId;
          if (param1 != null) paramObj.param1 = param1;
          if (param2 != null) paramObj.param2 = param2;
        }

        const tabTitle = buildLinkTargetTabTitle(
          tabTitleBase,
          Boolean(linkTarget.RowDisplayDbField),
          linkTarget.RowDisplayDbField ? rowDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
          paramId ?? undefined
        );

        openListRootNavPopup({
          routeBasePath: routeCode,
          title: tabTitle,
          paramObj,
          popupWidth: linkTarget.PopupWidth ?? null,
          popupHeight: linkTarget.PopupHeight ?? null,
          showConfirmClose: routeLower === 'masterdatamanagement',
          pickerContext: routeLower === 'masterdatamanagement' ? { linkTarget, hostRow: rowItem } : undefined,
        });
        return;
      }

      const targetPkValue = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

      const linkTargetValueMapping: Record<string, any> = {};
      if (linkTarget.SourceColumn2 && linkTarget.TargetColumn2) {
        let dbColumnName = linkTarget.SourceColumn2;
        const fromRoot = String(dbColumnName).indexOf('RootUnit.') >= 0;
        if (fromRoot) dbColumnName = dbColumnName.substring(9).trim();
        linkTargetValueMapping[linkTarget.TargetColumn2] = fromRoot
          ? rootDictOneToOneFields?.[dbColumnName]
          : rowDictOneToOneFields?.[dbColumnName];
      }
      if (linkTarget.SourceColumn3 && linkTarget.TargetColumn3) {
        let dbColumnName = linkTarget.SourceColumn3;
        const fromRoot = String(dbColumnName).indexOf('RootUnit.') >= 0;
        if (fromRoot) dbColumnName = dbColumnName.substring(9).trim();
        linkTargetValueMapping[linkTarget.TargetColumn3] = fromRoot
          ? rootDictOneToOneFields?.[dbColumnName]
          : rowDictOneToOneFields?.[dbColumnName];
      }

      const tabTitle = buildLinkTargetTabTitle(
        tabTitleBase,
        Boolean(linkTarget.RowDisplayDbField),
        linkTarget.RowDisplayDbField ? rowDictOneToOneFields?.[linkTarget.RowDisplayDbField] : undefined,
        targetPkValue ?? undefined
      );

      const targetOrganizedType = linkTarget?.TransactionOrganizedType ?? linkTarget?.LinkTargetTransactionOrganizedType;
      const isTargetListTransaction =
        transactionOrganizedTypeEnum?.List != null &&
        targetOrganizedType != null &&
        Number(targetOrganizedType) === Number(transactionOrganizedTypeEnum.List);

      const routeBasePath = isTargetListTransaction ? 'FormListEdit' : 'FormMasterDetail';

      if (linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.Edit) {
        if (!targetPkValue) return;
        const param2Obj = { linkTargetValueMapping };
        openListRootNavPopup({
          routeBasePath,
          title: tabTitle,
          paramObj: {
            id: linkTarget.LinkTargetTransactionId,
            param1: targetPkValue,
            param2: JSON.stringify(param2Obj),
          },
          popupWidth: linkTarget.PopupWidth ?? null,
          popupHeight: linkTarget.PopupHeight ?? null,
          showConfirmClose: false,
        });
        return;
      }

      if (linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.Preview) {
        if (!targetPkValue) return;
        const param2Obj = { linkTargetValueMapping, isPreview: true, isPrint: true };
        openListRootNavPopup({
          routeBasePath,
          title: tabTitle,
          paramObj: {
            id: linkTarget.LinkTargetTransactionId,
            param1: targetPkValue,
            param2: JSON.stringify(param2Obj),
          },
          popupWidth: linkTarget.PopupWidth ?? null,
          popupHeight: linkTarget.PopupHeight ?? null,
          showConfirmClose: false,
        });
        return;
      }

      if (
        linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.CreateBlank ||
        linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.CreateFromExistingItem
      ) {
        if (!linkTarget.LinkTargetTransactionId) return;
        const param2Obj: any = {};
        if (linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.CreateFromExistingItem) {
          param2Obj.linkTargetValueMapping = linkTargetValueMapping;
        }
        if (linkTarget.DataTransferSettingId) {
          param2Obj.newFormLinkTargetPreLoadSettingObj = {
            dataTransferSettingId: linkTarget.DataTransferSettingId,
            srcTransactionRid:
              dataModelRef.current?.currentFormData?.RootPrimaryKeyValue ?? rowItem?.Id ?? null,
          };
        }
        openListRootNavPopup({
          routeBasePath,
          title: tabTitleBase,
          paramObj: { id: linkTarget.LinkTargetTransactionId, param1: null, param2: JSON.stringify(param2Obj) },
          popupWidth: linkTarget.PopupWidth ?? null,
          popupHeight: linkTarget.PopupHeight ?? null,
          showConfirmClose: false,
        });
        return;
      }

      if (linkTarget.ActionType === LIST_TARGET_ACTION_LIST_ROOT.Delete) {
        showError('Delete action is not implemented for unit navigation yet.');
        return;
      }

      showError('This unit navigation action is not implemented yet.');
    },
    [getSelectedListRowItem, openListRootNavPopup, showError, transactionOrganizedTypeEnum?.List]
  );

  const handleListRootLinkedSearch = useCallback(
    (linkedSearch: any) => {
      if (!linkedSearch || isLockTransaction) return;

      const rowItem = getSelectedListRowItem() ?? {};
      const rootDictOneToOneFields = dataModelRef.current?.currentFormData?.DictOneToOneFields ?? {};
      const siblingOneToOne = dataModelRef.current?.currentFormData?.DictSiblingOneToOneFields ?? {};
      const rootUnitId = dataModelRef.current?.currentFormData?.RootUnitId ?? rootUnit?.Id ?? null;

      const rowDictOneToOneFields = rowItem?.DictOneToOneFields ?? {};

      const targetSearchIdRaw = linkedSearch?.SearchSaveId ?? linkedSearch?.SearchId ?? null;
      const isSavedSearch = linkedSearch?.SearchSaveId != null && linkedSearch?.SearchSaveId !== '';
      const initialViewId = linkedSearch?.SearchViewId ?? null;

      const dictCreteriaIdValue: Record<string, any> = {};
      const mappingList = Array.isArray(linkedSearch?.AppTransactionUnitSearchFieldMappingList)
        ? linkedSearch.AppTransactionUnitSearchFieldMappingList
        : [];

      for (const mapping of mappingList) {
        const searchFieldId = mapping?.SearchFieldId;
        const dataBaseFieldName = mapping?.DataBaseFieldName;
        if (searchFieldId == null || !dataBaseFieldName) continue;

        const sourceUnitId = mapping?.SourceTransactionUnitId;
        let formValue: any = null;

        if (sourceUnitId == null) {
          formValue = rootDictOneToOneFields?.[dataBaseFieldName];
        } else if (rootUnitId != null && String(sourceUnitId) === String(rootUnitId)) {
          formValue = rootDictOneToOneFields?.[dataBaseFieldName];
        } else if (rootUnit?.Id != null && String(sourceUnitId) === String(rootUnit.Id)) {
          formValue = rowDictOneToOneFields?.[dataBaseFieldName];
        } else if (siblingOneToOne?.[sourceUnitId]) {
          formValue = siblingOneToOne?.[sourceUnitId]?.[dataBaseFieldName];
        } else {
          formValue = rootDictOneToOneFields?.[dataBaseFieldName];
        }

        dictCreteriaIdValue[searchFieldId] = formValue;
      }

      const tabTitle = linkedSearch?.Name ?? 'Linked Search';
      const paramObj = {
        searchId: targetSearchIdRaw,
        isSavedSearch,
        initialViewId,
        isShowCriterias: true,
        isLinkedSearch: true,
        isSingleSelection: isLinkedSearchGridSingleSelection(linkedSearch),
        dictCreteriaIdValue,
      };

      setListRootSearchPopupState({
        title: tabTitle,
        width: linkedSearch.PopupWidth ?? null,
        height: linkedSearch.PopupHeight ?? null,
        paramObj,
        popupZIndex: appHelper.getNextPopupZIndex(),
        showConfirmClose: isLinkedSearchPopupConfirmClose(linkedSearch),
        linkedSearch,
      });
    },
    [getSelectedListRowItem, isLockTransaction, rootUnit?.Id]
  );

  const handleCellEditBeginning = useCallback(
    (s: any, e: any) => {
      if (isReadOnly) return;
      if (e?.row < 0 || e?.col < 0) return;

      const col = s?.columns?.[e.col];
      const rowData = s?.rows?.[e.row]?.dataItem;
      if (!col || !rowData) return;

      const binding = String(col?.binding ?? col?.name ?? '');
      // Angular parity: fieldName = col.binding.split(".")[1]
      const fieldNameFromBinding =
        binding.includes('.') ? binding.split('.')[1] : binding.split('.').pop();
      if (!fieldNameFromBinding) return;

      // Angular parity: lock-cascading fields (row-level)
      if (
        Array.isArray(rowData?.CascadingNeedToBeLockedFields) &&
        rowData.CascadingNeedToBeLockedFields.indexOf(fieldNameFromBinding) >= 0
      ) {
        e.cancel = true;
        return;
      }

      // Angular parity: primary key readonly in Edit mode
      const editedField = fields.find(
        (f: any) => String(f?.DataBaseFieldName) === String(fieldNameFromBinding)
      );
      if (isEditMode && normalizeBool(editedField?.IsPrimaryKey)) {
        e.cancel = true;
        return;
      }

      // Respect explicit column/field readonly flags
      const fieldIsReadOnly =
        normalizeBool(editedField?.IsFormLayoutReadOnly) ||
        normalizeBool(editedField?.IsReadonly) ||
        normalizeBool(editedField?.IsReadOnly);
      if (fieldIsReadOnly) {
        e.cancel = true;
      }
    },
    [fields, isEditMode, isReadOnly]
  );

  // List Edit: render grandchild units inside detail rows (Angular: wj-flex-grid-detail).
  const grandChildUnitList = (rootUnit?.Children ?? [])
    .filter((u: any) => u && (u.IsFormLayoutVisible === undefined || u.IsFormLayoutVisible !== false));
  const grandChildPopupModeValue = Number(emAppGrandChildEditModeEnum?.Popup ?? 2);
  const currentGrandChildEditMode = Number(
    dataModel.transactionExDto?.EmGrandChildEditMode ??
      dataModel.currentFormStructure?.EmGrandChildEditMode ??
      rootUnit?.EmGrandChildEditMode ??
      1
  );
  const isGrandChildPopupMode = currentGrandChildEditMode === grandChildPopupModeValue;

  const rowHasDetail = useCallback(
    (row: any) => {
      const rowData = row?.dataItem?._originalData ?? row?.dataItem ?? row;
      if (!rowData?.DictOneToManyFields || grandChildUnitList.length === 0) return false;
      return grandChildUnitList.some((u: any) => {
        const unitIdStr = String(u.Id ?? '');
        const arr = rowData.DictOneToManyFields?.[unitIdStr];
        return Array.isArray(arr);
      });
    },
    [grandChildUnitList]
  );

  const grandChildControllerModel = useMemo(
    () => ({
      isDesignMode: false,
      isPreview: false,
    }),
    []
  );

  const grandChildUnitListRef = useRef<any[]>([]);
  useEffect(() => {
    grandChildUnitListRef.current = grandChildUnitList;
  }, [grandChildUnitList]);

  /** Keep row detail expanded after nested child grid edits (List Edit / Wijmo otherwise collapses detail). */
  const reopenExpandedRowDetail = useCallback((dataIndex: number) => {
    if (typeof dataIndex !== 'number' || dataIndex < 0) return;
    window.setTimeout(() => {
      try {
        const flex = flexGridRef.current?.control ?? flexGridRef.current;
        const dp = flexGridDetailRef.current?.control ?? flexGridDetailRef.current;
        if (!flex || !dp || (flex as any).isDisposed || (dp as any).isDisposed) return;
        let gridRow = -1;
        for (let i = 0; i < flex.rows.length; i++) {
          const r = flex.rows[i];
          if (r && r.dataItem != null && typeof r.dataIndex === 'number' && r.dataIndex === dataIndex) {
            gridRow = i;
            break;
          }
        }
        if (gridRow >= 0) {
          (dp as any).showDetail?.(gridRow, true);
        }
      } catch {
        // ignore
      }
    }, 120);
  }, []);

  const applyGrandChildRowUpdate = useCallback(
    (updatedRow: any, parentDataIndex: number, rowItemRef: any) => {
      if (!updatedRow) return;

      const dm = dataModelRef.current;
      const src = (dm?.listDataSource as any)?.sourceCollection as any[] | undefined;
      const host =
        Array.isArray(src) && parentDataIndex >= 0 && parentDataIndex < src.length
          ? src[parentDataIndex]
          : rowItemRef;
      if (!host) return;
      Object.assign(host, updatedRow);

      setDataModel((prev) => {
        const sourceCollection = prev.listDataSource?.sourceCollection;
        if (Array.isArray(sourceCollection) && parentDataIndex >= 0 && parentDataIndex < sourceCollection.length) {
          sourceCollection[parentDataIndex] = host;
        }
        return {
          ...prev,
          currentFormData: {
            ...prev.currentFormData,
            IsDirty: true,
            DictDocumentIdFileCode:
              updatedRow?.DictDocumentIdFileCode ?? prev.currentFormData?.DictDocumentIdFileCode ?? {},
          },
        };
      });

      if (!grandChildPopupOpenRef.current) {
        reopenExpandedRowDetail(parentDataIndex);
      }
    },
    [reopenExpandedRowDetail]
  );

  const grandChildDetailTemplate = useCallback(
    (ctx: any) => {
      // Match Master Detail / Wijmo: use collection view index, not visual row index.
      const parentDataIndex = Number(ctx?.row?.dataIndex ?? ctx?.row?.index ?? -1);

      const dm = dataModelRef.current;
      const src = (dm?.listDataSource as any)?.sourceCollection as any[] | undefined;
      let rowItem: any = null;
      if (Array.isArray(src) && parentDataIndex >= 0 && parentDataIndex < src.length) {
        rowItem = src[parentDataIndex];
      } else {
        rowItem = ctx?.item?._originalData ?? ctx?.item ?? ctx?.row?.dataItem?._originalData ?? ctx?.row?.dataItem;
      }
      if (!rowItem) return null;

      const list = grandChildUnitListRef.current;
      if (!list || list.length === 0) return null;

      const nestedDataModel = {
        ...dm,
        currentFormData: {
          ...rowItem,
          DictDocumentIdFileCode: dm?.currentFormData?.DictDocumentIdFileCode ?? {},
        },
      };

      const onNestedDataModelChange = (updatedNested: any) => {
        const updatedRow = updatedNested?.currentFormData;
        if (!updatedRow) return;
        applyGrandChildRowUpdate(updatedRow, parentDataIndex, rowItem);
      };

      const tx = dm?.transactionExDto;

      return (
        <div
          style={{
            width: 'calc(100% - 100px)',
            height: '300px',
            overflow: 'hidden',
          }}
        >
          {list.map((gc: any) => {
            const unitId = gc.Id ?? gc.unitId ?? gc.TransactionUnitId;
            if (unitId == null) return null;
            const fullUnit = findTransactionUnitInTransactionExDto(tx, String(unitId));
            const unitExDtoForGrid = fullUnit ? { ...gc, ...fullUnit } : gc;

            return (
              <div key={String(unitId)} className="w-full h-full">
                <Provider store={store}>
                  <DataGridLayout
                    unitExDto={unitExDtoForGrid}
                    unitId={unitId}
                    dataModel={nestedDataModel}
                    dictOneToManyHostRow={rowItem}
                    masterDetailFormData={dataModelRef.current?.currentFormData}
                    parentOneToManyUnitId={rootUnit?.Id}
                    parentHostRowIndex={parentDataIndex}
                    controllerModel={grandChildControllerModel}
                    transactionExDto={dataModelRef.current?.transactionExDto}
                    onDataModelChange={onNestedDataModelChange}
                    actionButtonsPosition="left"
                    gridContentMaxHeight="300px"
                  />
                </Provider>
              </div>
            );
          })}
        </div>
      );
    },
    // Uses refs for list + dataModel; keep function stable to avoid Wijmo detail collapse.
    [applyGrandChildRowUpdate, grandChildControllerModel, rootUnit?.Id]
  );

  const isDdLikeControlType = useCallback(
    (controlType: any): boolean => {
      const ctl = controlType != null ? Number(controlType) : NaN;
      return (
        ctl === Number(emAppControlTypeEnum?.DDL) ||
        ctl === Number(emAppControlTypeEnum?.SearchAbleDDL) ||
        ctl === Number(emAppControlTypeEnum?.AutoComplete)
      );
    },
    [emAppControlTypeEnum?.AutoComplete, emAppControlTypeEnum?.DDL, emAppControlTypeEnum?.SearchAbleDDL]
  );

  if (!transactionId) {
    return (
      <div className={`w-full h-full flex items-center justify-center p-4 ${theme.mainContentSection}`}>
        <div className={`text-sm ${theme.label}`}>No transaction selected. Open Form List Edit with a transaction Id.</div>
      </div>
    );
  }

  if (dataModel.currentFormStructure && !dataModel.isLoading && isAllowAccess === false) {
    return (
      <div className={`w-full h-full flex items-center justify-center p-4 ${theme.mainContentSection}`}>
        <div className="text-red-600">
          Data Model &quot;{dataModel.transactionExDto?.TransactionName ?? dataModel.currentFormStructure?.TransactionName ?? 'List Edit'}&quot;: Access Denied
        </div>
      </div>
    );
  }

  const openFormConfig = () => {
    if (!transactionId) return;
    setConfigPopupOpen(true);
  };

  const closeConfigPopup = () => {
    setConfigPopupOpen(false);
  };

  // Match Angular _FormMainMenus / _ListEditLayoutForm: EnableConfigurationMode + admin, hide for draft.
  const enableConfigurationMode = isEnableConfigurationModeForUser(userContext);
  const isAdminUser = isAdminUserFromContext(userContext);
  const isDraft = Boolean(
    dataModel.currentFormStructure?.IsDraft ?? dataModel.currentFormStructure?.isDraft
  );
  const showConfigurationButton = enableConfigurationMode && isAdminUser && !isDraft;

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.default}`}>
      {hideHeader ? (
        <div className={`flex items-center justify-end gap-2 px-2 py-1 shrink-0 ${theme.mainContentSection}`}>
          <FlexGridAddOn gridRef={flexGridRef} title="Freeze / Show / Hide columns" />
          {!isReadOnly && (
            <>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={addChildNew}
                title="Add row"
              >
                <i className="fa-solid fa-plus-circle mr-1" aria-hidden /> Add
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={deleteChild}
                title="Delete selected row"
              >
                <i className="fa-solid fa-minus-circle mr-1" aria-hidden /> Delete
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={saveFormData}
                title="Save"
                disabled={!dataModel.currentFormData?.IsDirty}
              >
                <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
              </button>
            </>
          )}
        </div>
      ) : (
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {dataModel.transactionExDto?.TransactionName ?? dataModel.currentFormStructure?.TransactionName ?? 'List Edit'}
        </div>
        <div className="flex items-center gap-2">
          <FlexGridAddOn gridRef={flexGridRef} title="Freeze / Show / Hide columns" />
          {showConfigurationButton && (
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={openFormConfig}
              title="Configuration"
            >
              <i className="fa-solid fa-gear mr-1" aria-hidden /> Configuration
            </button>
          )}
          {!isReadOnly && (
            <>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={addChildNew}
                title="Add row"
              >
                <i className="fa-solid fa-plus-circle mr-1" aria-hidden /> Add
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={deleteChild}
                title="Delete selected row"
              >
                <i className="fa-solid fa-minus-circle mr-1" aria-hidden /> Delete
              </button>
            </>
          )}
          {!isReadOnly && (
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={saveFormData}
              title="Save"
              disabled={!dataModel.currentFormData?.IsDirty}
            >
              <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
            </button>
          )}
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => loadData(true)}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          {hasListRootUnitNavigation && !isLockTransaction && (
            <>
              {listRootLinkTargets.map((lt: any) => (
                <button
                  key={lt.Id ?? lt.NavigationActionName}
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  title={lt.NavigationActionName ?? 'Navigate to form'}
                  onClick={() => executeListRootUnitLinkTarget(lt)}
                >
                  <i className="fa-solid fa-route mr-1" aria-hidden />
                  {lt.NavigationActionName ?? 'Link'}
                </button>
              ))}
              {listRootLinkedSearchesMenu.map((ls: any) => (
                  <button
                    key={ls.Id ?? ls.Name}
                    type="button"
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                    title={ls.Name ?? 'Linked search'}
                    onClick={() => handleListRootLinkedSearch(ls)}
                  >
                    <i className="fa-solid fa-magnifying-glass mr-1" aria-hidden />
                    {ls.Name ?? 'Search'}
                  </button>
                ))}
            </>
          )}
        </div>
      </div>
      )}

      {(dataModel.errorMessages?.error?.length > 0 || dataModel.errorMessages?.warning?.length > 0) && (
        <div className={`px-3 py-1 text-xs ${theme.label}`}>
          {dataModel.errorMessages.error?.map((e: string, i: number) => (
            <div key={`err-${i}`} className="text-red-600">{e}</div>
          ))}
          {dataModel.errorMessages.warning?.map((w: string, i: number) => (
            <div key={`warn-${i}`} className="text-amber-600">{w}</div>
          ))}
        </div>
      )}

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection} px-2 pb-2`}>
        {!(dataModel.currentFormStructure || dataModel.listDataSource) && dataModel.isLoading && (
          <div className="flex items-center justify-center h-full">
            <div className={`text-sm ${theme.label}`}>Loading...</div>
          </div>
        )}

        {(dataModel.currentFormStructure || dataModel.listDataSource) && (
          <div className="relative w-full h-full min-h-[200px]">
            {dataModel.isLoading && (
              <div
                className={`absolute inset-0 z-10 flex items-center justify-center ${theme.mainContentSection}`}
                style={{ opacity: 0.92 }}
                aria-busy="true"
                aria-label="Loading"
              >
                <div className={`text-sm ${theme.label}`}>Loading...</div>
              </div>
            )}
            <FlexGrid
              ref={flexGridRef}
              itemsSource={dataModel.listDataSource ?? stableEmptyListCvRef.current!}
              selectionMode="Row"
              allowSorting={true}
              isReadOnly={isReadOnly}
              headersVisibility="All"
              showDropDown={true}
              cellEditEnded={(s: any, e: any) => {
                const row = s.rows[e.row];
                const item = row?.dataItem;
                if (item) {
                  item.IsDirty = true;
                  markChange();
                }
              }}
              className="w-full h-full"
            >
              <FlexGridFilter />
              {!isGrandChildPopupMode && grandChildUnitList.length > 0 && (
                <FlexGridDetail
                  ref={flexGridDetailRef}
                  detailVisibilityMode="ExpandSingle"
                  rowHasDetail={rowHasDetail}
                  template={grandChildDetailTemplate}
                />
              )}
              {isGrandChildPopupMode &&
                grandChildUnitList.map((gc: any) => {
                  const unitId = gc.Id ?? gc.unitId ?? gc.TransactionUnitId;
                  if (unitId == null) return null;
                  return (
                    <FlexGridColumn
                      key={`gc-popup-${String(unitId)}`}
                      binding=""
                      header={gc.UnitDisplayName ?? gc.Name ?? `Unit ${unitId}`}
                      width={130}
                      isReadOnly={true}
                    >
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => {
                          const rowItem = cell.item;
                          return (
                            <div className="w-full h-full flex items-center justify-center">
                              <button
                                type="button"
                                className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                                onMouseDown={(e) => e.stopPropagation()}
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setGrandChildPopupState({
                                    open: true,
                                    unitId,
                                    rowItem,
                                    parentRowIndex: Number(cell?.row?.dataIndex ?? cell?.row?.index ?? -1),
                                  });
                                }}
                                title="Edit grand child"
                              >
                                <i className="fa-solid fa-up-right-from-square mr-1" aria-hidden /> Edit
                              </button>
                            </div>
                          );
                        }}
                      />
                    </FlexGridColumn>
                  );
                })}
              {fields.map((field: any) => {
                const binding = `DictOneToOneFields.${field.DataBaseFieldName}`;
                const header = field.DisplayName ?? field.DataBaseFieldName;
                const colWidth = field.Width ?? field.DisplayWidth ?? 100;

                const isImageColumn =
                  field.ControlType === emAppControlTypeEnum?.Image ||
                  field.ControlType === emAppControlTypeEnum?.ExternalImageUrl ||
                  field.ControlType === emAppControlTypeEnum?.ImageBinary;
                const isFileColumn = field.ControlType === emAppControlTypeEnum?.File;
                const isRgbColorColumn = field.ControlType === emAppControlTypeEnum?.RGBColorDisplay;
                const isNumericColumn = field.ControlType === emAppControlTypeEnum?.Numeric;

                const fieldIsReadOnly =
                  isReadOnly ||
                  normalizeBool(field?.IsFormLayoutReadOnly) ||
                  isLockTransaction ||
                  dictLockUnitIds?.[String(field?.TransactionUnitId ?? '')] === true ||
                  dictOneToOneLockFields?.[String(field?.Id ?? '')] === true ||
                  (isEditMode &&
                    normalizeBool(field?.IsPrimaryKey) &&
                    !normalizeBool(field?.IsReadonly));

                const nbdecimalRaw =
                  field?.Nbdecimal ??
                  field?.NbDecimal ??
                  field?.NumberOfDecimal ??
                  field?.DecimalDigits ??
                  field?.Decimals ??
                  field?.Scale ??
                  null;
                const nbdecimal =
                  typeof nbdecimalRaw === 'number'
                    ? nbdecimalRaw
                    : typeof nbdecimalRaw === 'string'
                      ? Number(nbdecimalRaw)
                      : NaN;
                const resolvedNbdecimal =
                  Number.isFinite(nbdecimal) && nbdecimal >= 0 ? Math.min(10, Math.floor(nbdecimal)) : 0;

                // DDL: use standalone DataMap (Angular uses dictFieldEntityDataMap built from stand-alone entity mappings)
                const dataMap =
                  isDdLikeControlType(field.ControlType) && dataModel.dictFieldEntityDataMap
                    ? dataModel.dictFieldEntityDataMap[String(field.Id)]
                    : undefined;

                if (isImageColumn) {
                  return (
                    <FlexGridColumn
                      key={field.Id ?? field.DataBaseFieldName}
                      binding={binding}
                      header={header}
                      width={typeof colWidth === 'number' ? colWidth : 100}
                      isReadOnly={true}
                    >
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => {
                          const rowItem = cell.item as any;
                          const raw = rowItem?.DictOneToOneFields?.[field.DataBaseFieldName];
                          const fileIdResolved = raw != null ? Number(raw) : null;
                          const fileId = fileIdResolved != null && Number.isFinite(fileIdResolved) ? fileIdResolved : null;

                          const thumbUrl = fileIdResolved ? fileThumbnailUrl(fileIdResolved) : null;

                          return (
                            <div className="flex items-center justify-between w-full h-full gap-1 min-w-0">
                              <div className="flex items-center min-w-0 flex-auto gap-2">
                                {thumbUrl ? (
                                  <img
                                    src={thumbUrl}
                                    alt=""
                                    className="max-h-[30px] max-w-[30px] object-contain cursor-pointer flex-shrink-0"
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      if (fieldIsReadOnly) return;
                                      if (!fileIdResolved) return;
                                      setImagePreviewState({ open: true, fileId: fileIdResolved });
                                    }}
                                  />
                                ) : (
                                  <div className="w-[30px] h-[30px]" />
                                )}
                              </div>
                              {!isReadOnly && !fieldIsReadOnly && (
                                <button
                                  type="button"
                                  className={`${theme.button_default} p-0 w-6 h-5 rounded-[4px] text-[10px] flex items-center justify-center flex-shrink-0`}
                                  title="Actions"
                                  onMouseDown={(e) => e.stopPropagation()}
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                                    openCellMenuAt(rect.right, rect.top, {
                                      kind: 'image',
                                      rowItem,
                                      fieldName: field.DataBaseFieldName,
                                      fileId,
                                    });
                                  }}
                                >
                                  <i className="fa-solid fa-bars" aria-hidden="true" />
                                </button>
                              )}
                            </div>
                          );
                        }}
                      />
                    </FlexGridColumn>
                  );
                }

                if (isRgbColorColumn) {
                  return (
                    <FlexGridColumn
                      key={field.Id ?? field.DataBaseFieldName}
                      binding={binding}
                      header={header}
                      width={typeof colWidth === 'number' ? colWidth : 100}
                      isReadOnly={true}
                    >
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => {
                          const rowItem = cell.item as any;
                          const raw = rowItem?.DictOneToOneFields?.[field.DataBaseFieldName];
                          return <RgbColorSwatch value={raw} />;
                        }}
                      />
                    </FlexGridColumn>
                  );
                }

                if (isFileColumn) {
                  return (
                    <FlexGridColumn
                      key={field.Id ?? field.DataBaseFieldName}
                      binding={binding}
                      header={header}
                      width={typeof colWidth === 'number' ? colWidth : 100}
                      isReadOnly={true}
                    >
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => {
                          const rowItem = cell.item as any;
                          const raw = rowItem?.DictOneToOneFields?.[field.DataBaseFieldName];
                          const fileIdResolved = raw != null ? Number(raw) : null;
                          const fileId = fileIdResolved != null && Number.isFinite(fileIdResolved) ? fileIdResolved : null;

                          const fileIdToNameMap = dataModelRef.current?.currentFormData?.DictDocumentIdFileCode ?? {};
                          const dictName = fileIdResolved ? fileIdToNameMap[String(fileIdResolved)] : '';
                          const fallbackText = raw != null && typeof raw === 'string' ? raw : '';
                          const display = dictName || fallbackText || (fileIdResolved ? String(fileIdResolved) : '');

                          return (
                            <div className="flex items-center justify-between w-full h-full gap-1 min-w-0">
                              <div
                                className="flex items-center min-w-0 flex-auto cursor-pointer"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  if (fieldIsReadOnly) return;
                                  if (!fileIdResolved) return;
                                  void downloadFileById(fileIdResolved);
                                }}
                              >
                                <span className={`truncate block flex-auto text-xs ${display ? '' : theme.label}`}>{display || ''}</span>
                              </div>
                              {!isReadOnly && !fieldIsReadOnly && (
                                <button
                                  type="button"
                                  className={`${theme.button_default} p-0 w-6 h-5 rounded-[4px] text-[10px] flex items-center justify-center flex-shrink-0`}
                                  title="Actions"
                                  onMouseDown={(e) => e.stopPropagation()}
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                                    openCellMenuAt(rect.right, rect.top, {
                                      kind: 'file',
                                      rowItem,
                                      fieldName: field.DataBaseFieldName,
                                      fileId,
                                    });
                                  }}
                                >
                                  <i className="fa-solid fa-bars" aria-hidden="true" />
                                </button>
                              )}
                            </div>
                          );
                        }}
                      />
                    </FlexGridColumn>
                  );
                }

                // Default column types (Text, Checkbox, DDL, Numeric, ...)
                return (
                  <FlexGridColumn
                    key={field.Id ?? field.DataBaseFieldName}
                    binding={binding}
                    header={header}
                    width={typeof colWidth === 'number' ? colWidth : 100}
                    dataMap={dataMap}
                    dataType={isNumericColumn ? 'Number' : undefined}
                    format={isNumericColumn ? `n${resolvedNbdecimal}` : undefined}
                    isReadOnly={fieldIsReadOnly}
                  />
                );
              })}
              <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
            </FlexGrid>
          </div>
        )}
      </div>

      {/* Image preview popup */}
      {imagePreviewState.open && imagePreviewState.fileId && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10006]"
          onClick={() => setImagePreviewState({ open: false, fileId: null })}
        >
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl border flex items-center justify-center p-3`}
            style={{ width: '800px', height: '600px', maxWidth: '95vw', maxHeight: '90vh' }}
            onClick={(e) => e.stopPropagation()}
          >
            {(() => {
              const url = buildFileUrl(imagePreviewState.fileId);
              return url ? (
                <img src={url} alt="" className="max-w-full max-h-full object-contain" />
              ) : (
                <div className={`text-xs ${theme.label}`}>Preview not available.</div>
              );
            })()}
          </div>
        </div>
      )}

      {/* Cell context menu */}
      {cellMenu?.open && (
        <div
          ref={cellMenuRef}
          className={`fixed z-[10005] border rounded shadow-lg py-1 ${theme.contextMenu} ${t(
            'border_mainContentSection'
          )} ${t('bg_mainContentSection')}`}
          style={{ left: cellMenu.x, top: cellMenu.y, minWidth: CELL_CONTEXT_MENU_ESTIMATED_WIDTH, maxWidth: 220 }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          <button
            type="button"
            className={`w-full text-left px-2 py-1.5 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => {
              setPendingLibraryFileId(null);
              setLibraryState({
                open: true,
                kind: cellMenu.kind,
                rowItem: cellMenu.rowItem,
                fieldName: cellMenu.fieldName,
              });
              closeCellMenu();
            }}
          >
            <span className="w-4 flex items-center justify-center">
              <i className="fa-solid fa-folder-open" aria-hidden="true" />
            </span>
            <span className="min-w-0 truncate">Select From Library</span>
          </button>
          <button
            type="button"
            className={`w-full text-left px-2 py-1.5 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => {
              setUploadState({
                open: true,
                kind: cellMenu.kind,
                rowItem: cellMenu.rowItem,
                fieldName: cellMenu.fieldName,
              });
              closeCellMenu();
            }}
          >
            <span className="w-4 flex items-center justify-center">
              <i className="fa-solid fa-upload" aria-hidden="true" />
            </span>
            <span className="min-w-0 truncate">Upload</span>
          </button>

          {cellMenu.fileId ? <div className={`border-t my-1 ${t('border_mainContentSection')}`} /> : null}

          {cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-2 py-1.5 text-xs ${theme.contextMenu} flex items-center gap-2`}
              onClick={() => {
                if (cellMenu.fileId) void downloadFileById(cellMenu.fileId);
                closeCellMenu();
              }}
            >
              <span className="w-4 flex items-center justify-center">
                <i className="fa-solid fa-download" aria-hidden="true" />
              </span>
              <span className="min-w-0 truncate">Download</span>
            </button>
          ) : null}

          {cellMenu.kind === 'image' && cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-2 py-1.5 text-xs ${theme.contextMenu} flex items-center gap-2`}
              onClick={() => {
                setImagePreviewState({ open: true, fileId: cellMenu.fileId });
                closeCellMenu();
              }}
            >
              <span className="w-4 flex items-center justify-center">
                <i className="fa-solid fa-eye" aria-hidden="true" />
              </span>
              <span className="min-w-0 truncate">Preview</span>
            </button>
          ) : null}

          {cellMenu.fileId ? <div className={`border-t my-1 ${t('border_mainContentSection')}`} /> : null}

          {cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-2 py-1.5 text-xs ${theme.contextMenu} flex items-center gap-2 text-red-600 hover:opacity-90`}
              onClick={() => {
                updateRowFileValue(cellMenu.rowItem, cellMenu.fieldName, null);
                closeCellMenu();
              }}
            >
              <span className="w-4 flex items-center justify-center">
                <i className="fa-solid fa-eraser" aria-hidden="true" />
              </span>
              <span className="min-w-0 truncate">Clear</span>
            </button>
          ) : null}
        </div>
      )}

      {/* Upload popup for cell */}
      {uploadState?.open && (
        <FileUploader
          isOpen={true}
          onClose={() => setUploadState(null)}
          mode="appFile"
          appFileCallingFrom={uploadState.kind === 'image' ? 'Image' : 'File'}
          targetFolderId={fileStorageRootFolderId != null ? Number(fileStorageRootFolderId) : undefined}
          accept={uploadState.kind === 'image' ? 'image/jpeg,image/png,image/gif,image/bmp' : '*/*'}
          queueLimit={1}
          title={uploadState.kind === 'image' ? 'Upload Image' : 'Upload File'}
          onUploaded={async (result: any) => {
            const newId = result?.FileId != null ? Number(result.FileId) : null;
            if (!newId) return;
            await assignUploadedFileToCell(uploadState.kind, uploadState.rowItem, uploadState.fieldName, newId);
            setUploadState(null);
          }}
        />
      )}

      {/* Library selector popup for cell */}
      {libraryState?.open && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]"
          onClick={() => setLibraryState(null)}
        >
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
            style={{ width: '950px', height: '600px', maxWidth: '95vw', maxHeight: '90vh' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>File Selector</div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed`}
                  onClick={async () => {
                    if (!pendingLibraryFileId) return;
                    await assignUploadedFileToCell(
                      libraryState.kind,
                      libraryState.rowItem,
                      libraryState.fieldName,
                      pendingLibraryFileId
                    );
                    setLibraryState(null);
                  }}
                  disabled={!pendingLibraryFileId}
                >
                  Select &amp; Close
                </button>
                <button
                  type="button"
                  className={`p-1 ${theme.button_default} rounded-[4px] text-xs`}
                  onClick={() => setLibraryState(null)}
                  title="Close"
                >
                  <i className="fa-solid fa-xmark" aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden">
              {sysFileTransactionId ? (
                <FolderNavigation
                  transactionId={sysFileTransactionId}
                  defaultCategoryId={defaultCategoryId_company}
                  isFileMgt={true}
                  isUseAsSelector={true}
                  onFileSelected={(id) => {
                    if (id == null) setPendingLibraryFileId(null);
                    else setPendingLibraryFileId(Number(id));
                  }}
                />
              ) : (
                <div className="p-3">
                  <div className={`text-xs ${theme.label}`}>File selector is not configured. `SystemDefinedFileTransactionId` is missing.</div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {grandChildPopupState.open && grandChildPopupState.rowItem && grandChildPopupState.unitId != null && (
        <div
          className="fixed inset-0 z-[10003] flex items-center justify-center bg-black/40"
          onClick={() =>
            setGrandChildPopupState({ open: false, unitId: null, rowItem: null, parentRowIndex: -1 })
          }
        >
          <div
            className={`${theme.mainContentSection} rounded-md shadow-xl border flex flex-col overflow-hidden`}
            style={{ width: 'min(1500px, 95vw)', height: 'min(760px, 88vh)' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>
                {grandChildUnitList.find((u: any) => String(u.Id ?? u.unitId) === String(grandChildPopupState.unitId))
                  ?.UnitDisplayName ?? 'Grand Child'}
              </div>
              <button
                type="button"
                className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                onClick={() =>
                  setGrandChildPopupState({ open: false, unitId: null, rowItem: null, parentRowIndex: -1 })
                }
                title="Close"
                aria-label="Close"
              >
                <i className="fa-solid fa-xmark" aria-hidden />
              </button>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden p-2">
              {(() => {
                const popupUnit = grandChildUnitList.find(
                  (u: any) => String(u.Id ?? u.unitId ?? u.TransactionUnitId) === String(grandChildPopupState.unitId)
                );
                if (!popupUnit) return null;
                const dm = dataModelRef.current;
                const fullPu = findTransactionUnitInTransactionExDto(
                  dm?.transactionExDto,
                  String(grandChildPopupState.unitId)
                );
                const popupUnitMerged = fullPu ? { ...popupUnit, ...fullPu } : popupUnit;
                const src = (dm?.listDataSource as any)?.sourceCollection as any[] | undefined;
                const pIdx = grandChildPopupState.parentRowIndex;
                const canonicalRow =
                  Array.isArray(src) && pIdx >= 0 && pIdx < src.length ? src[pIdx] : grandChildPopupState.rowItem;
                const popupNestedDataModel = {
                  ...dm,
                  currentFormData: {
                    ...(canonicalRow ?? grandChildPopupState.rowItem),
                    DictDocumentIdFileCode: dm?.currentFormData?.DictDocumentIdFileCode ?? {},
                  },
                };
                return (
                  <Provider store={store}>
                    <DataGridLayout
                      unitExDto={popupUnitMerged}
                      unitId={grandChildPopupState.unitId}
                      dataModel={popupNestedDataModel}
                      dictOneToManyHostRow={canonicalRow ?? grandChildPopupState.rowItem}
                      masterDetailFormData={dataModelRef.current?.currentFormData}
                      parentOneToManyUnitId={rootUnit?.Id}
                      parentHostRowIndex={grandChildPopupState.parentRowIndex}
                      controllerModel={grandChildControllerModel}
                      transactionExDto={dataModelRef.current?.transactionExDto}
                      onDataModelChange={(updatedNested: any) => {
                        const updatedRow = updatedNested?.currentFormData;
                        if (!updatedRow) return;
                        applyGrandChildRowUpdate(
                          updatedRow,
                          grandChildPopupState.parentRowIndex,
                          canonicalRow ?? grandChildPopupState.rowItem
                        );
                      }}
                      actionButtonsPosition="left"
                    />
                  </Provider>
                );
              })()}
            </div>
          </div>
        </div>
      )}

      {listRootSearchPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={listRootSearchPopupState.popupZIndex}
          title={listRootSearchPopupState.title || 'Open Search'}
          frameInstanceKey={listRootSearchPopupState.popupZIndex}
          toolbarLeading={
            listRootSearchPopupState.showConfirmClose ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const ls = listRootSearchPopupState.linkedSearch;
                  const row = getSelectedListRowItem();
                  const sel = listEditLinkedSearchRef.current?.getSelectedResults?.() ?? [];
                  if (ls && row && sel.length > 0) {
                    applyLinkedSearchResultToListRow(sel[0], ls, row, fields);
                    markChange();
                    const cv = dataModel.listDataSource;
                    if (cv && typeof (cv as any).refresh === 'function') (cv as any).refresh();
                  }
                  setListRootSearchPopupState(null);
                }}
              >
                <i className="fa-solid fa-check mr-1" aria-hidden /> Confirm &amp; Close
              </button>
            ) : undefined
          }
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={() => setListRootSearchPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <AppSearch ref={listEditLinkedSearchRef} embeddedParamObj={listRootSearchPopupState.paramObj} />
        </EmbeddedLinkedPopupFrame>
      )}

      {listRootLinkTargetPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={listRootLinkTargetPopupState.popupZIndex}
          title={listRootLinkTargetPopupState.title || 'Navigate'}
          frameInstanceKey={listRootLinkTargetPopupState.popupZIndex}
          toolbarLeading={
            listRootLinkTargetPopupState.pickerContext ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const sel = listRootLinkTargetMdRef.current?.getSelectedResults?.() ?? [];
                  const pc = listRootLinkTargetPopupState.pickerContext;
                  if (pc && sel.length > 0) {
                    applyLinkTargetMasterDataSelectionToRow(pc.linkTarget, sel[0], pc.hostRow);
                    markChange();
                    const cv = dataModel.listDataSource;
                    if (cv && typeof (cv as any).refresh === 'function') (cv as any).refresh();
                  }
                  setListRootLinkTargetPopupState(null);
                }}
              >
                <i className="fa-solid fa-check mr-1" aria-hidden /> Confirm &amp; Close
              </button>
            ) : undefined
          }
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={() => setListRootLinkTargetPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          {(() => {
            const route = String(listRootLinkTargetPopupState.routeBasePath || '').toLowerCase();
            const paramObj = listRootLinkTargetPopupState.paramObj ?? {};
            const parsedParam2 =
              typeof paramObj?.param2 === 'string'
                ? (() => {
                    try {
                      return JSON.parse(paramObj.param2);
                    } catch {
                      return {};
                    }
                  })()
                : (paramObj?.param2 ?? {});

            if (route === 'masterdatamanagement') {
              return <AppSearch ref={listRootLinkTargetMdRef} embeddedParamObj={paramObj} />;
            }
            if (route === 'formmasterdetail') {
              const embeddedTransactionId = Number(paramObj?.id ?? 0);
              if (!embeddedTransactionId) {
                return <div className={`p-3 text-xs ${theme.label}`}>Invalid navigation target.</div>;
              }
              return (
                <FormMasterDetail
                  embedded={{
                    embeddedTransactionId,
                    embeddedRootPrimaryKeyValue: paramObj?.param1 ?? null,
                    embeddedParam2: parsedParam2,
                  }}
                />
              );
            }
            if (route === 'formlistedit') {
              const embeddedTransactionId = Number(paramObj?.id ?? 0);
              if (!embeddedTransactionId) {
                return <div className={`p-3 text-xs ${theme.label}`}>Invalid navigation target.</div>;
              }
              return (
                <Suspense fallback={<div className={`p-3 text-xs ${theme.label}`}>Loading…</div>}>
                  <ListEditNavPopupLazy
                    embedded={{
                      embeddedTransactionId,
                      embeddedParam2: parsedParam2,
                    }}
                  />
                </Suspense>
              );
            }
            return (
              <div className={`p-3 text-xs ${theme.label}`}>
                Unsupported popup target: {String(listRootLinkTargetPopupState.routeBasePath || '')}
              </div>
            );
          })()}
        </EmbeddedLinkedPopupFrame>
      )}

      {/* Configuration popup: in-app overlay (like context menu / Edit popup) */}
      {configPopupOpen && transactionId && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/30"
          onClick={closeConfigPopup}
          role="presentation"
        >
          <div
            className={`${theme.mainContentSection} rounded-md shadow-xl flex flex-col overflow-hidden`}
            style={{ width: 'min(1600px, 95vw)', height: 'min(900px, 90vh)' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>
                Application Builder: {dataModel.transactionExDto?.TransactionName ?? dataModel.currentFormStructure?.TransactionName ?? 'List Edit'}
              </span>
              <button
                type="button"
                className="w-9 h-9 flex items-center justify-center text-lg leading-none rounded-[4px] hover:opacity-80"
                onClick={closeConfigPopup}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden min-h-0">
              <ApplicationFormBuilder
                isOpen={true}
                onClose={closeConfigPopup}
                applicationId={String(transactionId)}
                transactionId={transactionId}
                transactionType={3}
                defaultSectionCode="TransactionGraphicEditor"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FormListEdit;
