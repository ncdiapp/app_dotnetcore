import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { Provider, useSelector } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridDetail } from '@mescius/wijmo.react.grid.detail';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { DataMap, GroupRow } from '@mescius/wijmo.grid';
import { useTheme } from '../../../../redux/hooks/useTheme';
import FlexGridAddOn from '../../../common/FlexGridAddOn';
import RgbColorSwatch from '../../../common/RgbColorSwatch';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { fileThumbnailUrl, fileLatestUrl, downloadFileById } from '../../../../webapi/fileEndpoints';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';
import { store, type RootState } from '../../../../redux/store';
import { setIsBusy, setIsNotBusy } from '../../../../redux/features/ui/feedback/busyLoaderSlice';
import { addErrorMessage, MessageType } from '../../../../redux/features/ui/feedback/errorMessageSlice';
import { appFileService } from '../../../../webapi/appfilesvc';
import FileUploader from '../../../common/FileUploader';
import { FolderNavigation } from '../../../folderNavigation';
import AppSearch, { type AppSearchHandle } from '../../../search/AppSearch';
import FormMasterDetail from '../../FormMasterDetail';
import FormListEdit from '../../FormListEdit';
import appHelper from '../../../../helper/appHelper';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../../hooks/useClampedContextMenuPosition';
import { buildLinkTargetTabTitle } from '../../../../utils/linkTargetTabTitle';
import { EmbeddedLinkedPopupFrame } from '../../EmbeddedLinkedPopupFrame';
import {
  applyLinkTargetMasterDataSelectionToRow,
  isLinkedSearchGridSingleSelection,
  isLinkedSearchPopupConfirmClose,
  shouldLinkedSearchApplyUpdateHostRow,
  type MasterDataPickerContext,
} from '../../linkedSearchUtils';
import PivotEditGridPanel from './PivotEditGridPanel';
import MatrixPivotEditGrid from './MatrixPivotEditGrid';
import ChildPivotProjectionGrid, { ProjectionImageCellContext } from './ChildPivotProjectionGrid';
import { ChildPivotProjectionModel, foldWideRowsIntoChildRows } from './childPivotProjectionHelper';
import {
  enrichTransactionFieldFromDict,
  isRuntimeTransactionFieldVisible,
} from './flexLayoutItemHelper';

interface DataGridLayoutProps {
  unitExDto: any;
  unitId: number | string;
  dataModel: any;
  controllerModel: any;
  transactionExDto?: any;
  onDataModelChange: (dataModel: any) => void;
  /** Where to show Add/Delete buttons in the grid header (default: 'right'). */
  actionButtonsPosition?: 'left' | 'right';
  /**
   * Max height for the internal grid content area.
   * Use `100%` when this grid is rendered inside a row-detail template.
   */
  gridContentMaxHeight?: string;
  /**
   * When this grid is nested under FlexGridDetail, pass the live parent row (`ctx.item._originalData`).
   * Shallow-copied `dataModel.currentFormData` pins `DictOneToManyFields` to a stale reference after
   * master updates — Add Row then reads [] / wrong array. Reads/writes use this host row instead.
   */
  dictOneToManyHostRow?: any;
  /**
   * Transaction root `currentFormData` (full AppMasterDetailDto shape) for grandchild grids.
   * When `dataModel.currentFormData` is a child-row snapshot, cascading uses root for
   * `IsUsedCascadingDataSourceFiedIds`, `IsChangedNeedToCascadingFiedIds`, and root `DictCascadingFiledDataSource`.
   */
  masterDetailFormData?: any;
  /**
   * Child unit id whose `DictOneToManyFields` on the root holds `dictOneToManyHostRow` (middle tier).
   * Required with `masterDetailFormData` to build a full `MasterDetailDataDto` for grandchild cascade API.
   */
  parentOneToManyUnitId?: number | string;
  /** Row index of `dictOneToManyHostRow` in `masterDetailFormData.DictOneToManyFields[parentOneToManyUnitId]` (fallback if reference identity fails). */
  parentHostRowIndex?: number;
}

/**
 * Redux/Immer (or other code) may freeze `currentFormData` / nested rows — non-writable props throw on assign.
 * Optimistic host mutations are best-effort; `deferDataModelChange` still applies the same logical update.
 */
function tryMutateInPlace(mutator: () => void): void {
  try {
    mutator();
  } catch {
    // ignore
  }
}

/** Collect selected row `dataItem`s from a Wijmo FlexGrid (MultiRange / ListBox). */
function collectSelectedDataItemsFromFlexGrid(flex: any): any[] {
  if (!flex) return [];
  const selectedDataItems: any[] = [];
  const sr = flex.selectedRows;
  if (Array.isArray(sr) && sr.length > 0) {
    for (let i = 0; i < sr.length; i++) {
      const row = sr[i];
      if (row?.dataItem) selectedDataItems.push(row.dataItem);
    }
  }
  if (selectedDataItems.length === 0 && flex.rows) {
    for (let i = 0; i < flex.rows.length; i++) {
      const row = flex.rows[i];
      if (row?.isSelected && row.dataItem && !(row instanceof GroupRow)) {
        selectedDataItems.push(row.dataItem);
      }
    }
  }
  if (selectedDataItems.length === 0 && flex.selection != null) {
    const rh = flex.selection.row;
    if (typeof rh === 'number' && rh >= 0 && rh < flex.rows.length) {
      const r = flex.rows[rh];
      if (r?.dataItem && !(r instanceof GroupRow)) selectedDataItems.push(r.dataItem);
    }
  }
  return selectedDataItems;
}

function mergeFlatRowInto(prevRow: any, nextRow: any): void {
  if (!prevRow || !nextRow) return;
  (prevRow as any)._originalData = nextRow._originalData;
  (prevRow as any)._rowIndex = nextRow._rowIndex;
  for (const k of Object.keys(nextRow)) {
    if (k === '_originalData' || k === '_rowIndex') continue;
    (prevRow as any)[k] = (nextRow as any)[k];
  }
  for (const k of Object.keys(prevRow)) {
    if (k === '_originalData' || k === '_rowIndex') continue;
    if (!(k in nextRow)) delete (prevRow as any)[k];
  }
}

/**
 * Keep FlexGrid `dataItem` / `sourceCollection` row objects stable when possible.
 * - Same row count: merge in place.
 * - Exactly one row appended at bottom (Add Row): merge existing rows, then push the new flat row (no full array replace).
 * - Exactly one row removed from bottom when backing rows still align: merge + pop.
 * Otherwise replace `sourceCollection` (add/delete middle, bulk changes).
 * Full replace rebuilds rows and tears down nested row-detail React grids → Wijmo updateProps null.selectionMode.
 */
function syncCollectionViewFlatRows(prevCv: CollectionView<any>, nextFlatRows: any[]): void {
  // Wijmo may use ObservableArray for sourceCollection — it is not Array.isArray; still has .length, [], push, pop.
  const prevItems = (prevCv as any).sourceCollection as any;
  const assignAll = () => {
    (prevCv as any).sourceCollection = nextFlatRows;
    prevCv.sortDescriptions.clear();
    prevCv.refresh();
  };

  if (prevItems == null || typeof prevItems.length !== 'number') {
    assignAll();
    return;
  }

  const nPrev = prevItems.length;
  const nNext = nextFlatRows.length;

  const finish = () => {
    prevCv.sortDescriptions.clear();
    prevCv.refresh();
  };

  if (nPrev === nNext) {
    if (nNext === 0) {
      finish();
      return;
    }
    for (let i = 0; i < nNext; i++) {
      mergeFlatRowInto(prevItems[i], nextFlatRows[i]);
    }
    finish();
    return;
  }

  // Append one row at bottom (typical Add Row)
  if (nNext === nPrev + 1) {
    let aligned = true;
    for (let i = 0; i < nPrev; i++) {
      if (prevItems[i]?._originalData !== nextFlatRows[i]?._originalData) {
        aligned = false;
        break;
      }
    }
    if (aligned) {
      for (let i = 0; i < nPrev; i++) {
        mergeFlatRowInto(prevItems[i], nextFlatRows[i]);
      }
      prevItems.push(nextFlatRows[nNext - 1]);
      finish();
      return;
    }
  }

  // Remove one row from bottom only if first nNext backing rows still line up (not a middle delete)
  if (nNext === nPrev - 1) {
    let aligned = true;
    for (let i = 0; i < nNext; i++) {
      if (prevItems[i]?._originalData !== nextFlatRows[i]?._originalData) {
        aligned = false;
        break;
      }
    }
    if (aligned) {
      for (let i = 0; i < nNext; i++) {
        mergeFlatRowInto(prevItems[i], nextFlatRows[i]);
      }
      prevItems.pop();
      finish();
      return;
    }
  }

  assignAll();
}

/** Read a field from a transaction row (PascalCase / camelCase `DictOneToOneFields`, or flattened keys). */
function readTransactionRowField(row: any, dbName: string): any {
  if (!row || !dbName) return undefined;
  const dict = row.DictOneToOneFields ?? row.dictOneToOneFields;
  if (dict && typeof dict === 'object') {
    if (Object.prototype.hasOwnProperty.call(dict, dbName)) return dict[dbName];
    const alt = Object.keys(dict).find((k) => k.toLowerCase() === dbName.toLowerCase());
    if (alt != null && Object.prototype.hasOwnProperty.call(dict, alt)) return dict[alt];
  }
  if (Object.prototype.hasOwnProperty.call(row as object, dbName)) return (row as any)[dbName];
  return undefined;
}

const DataGridLayout: React.FC<DataGridLayoutProps> = ({
  unitExDto,
  unitId,
  dataModel,
  controllerModel,
  transactionExDto,
  onDataModelChange,
  actionButtonsPosition = 'right',
  gridContentMaxHeight,
  dictOneToManyHostRow,
  masterDetailFormData,
  parentOneToManyUnitId,
  parentHostRowIndex,
}) => {
  const { theme, t } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const flexGridRef = useRef<any>(null);
  /** Left "available" grid when EmGridViewDisplayType is AvailableSelectGridPair / MultipleSelectBox */
  const availableFlexGridRef = useRef<any>(null);
  const flexGridDetailRef = useRef<any>(null);
  /** Always non-null so FlexGrid stays mounted; avoids Wijmo React updateProps reading selectionMode on a null control after unmount. */
  const [collectionView, setCollectionView] = useState<CollectionView<any>>(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.clear();
    return cv;
  });
  const [availableCollectionView, setAvailableCollectionView] = useState<CollectionView<any>>(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.clear();
    return cv;
  });
  /** Bumps when source-unit row flags change in place so available-grid flat rows remap. */
  const [availableDataBump, setAvailableDataBump] = useState(0);
  const emAppControlTypeEnum = useEnumValues('EmAppControlType');
  const emAppTransactionGridDisplayTypeEnum = useEnumValues('EmAppTransactionGridDisplayType');
  const emAppGrandChildEditModeEnum = useEnumValues('EmAppGrandChildEditMode');
  const transactionOrganizedTypeEnum = useEnumValues('EmTransactionOrganizedType');
  const cascadingInitValueRef = useRef<any>(null);
  /** When `handleCellEditEnded` awaits cascading API, a later edit must not be overwritten by an older async completion. */
  const cellEditCommitSeqRef = useRef(0);
  /**
   * Root child grid (no dictOneToManyHostRow) only updates parent via deferred `onDataModelChange`.
   * Until React re-renders, `dataModelRef` is reset from stale props each render — a second Add/Delete/Edit
   * would read the old row array. Keep last committed rows here until props catch up.
   */
  const optimisticUnitRowsRef = useRef<any[] | null>(null);
  /** Bumps when optimisticUnitRowsRef changes so `gridData` useMemo recomputes before parent re-renders. */
  const [optimisticRowsBump, setOptimisticRowsBump] = useState(0);
  const isMountedRef = useRef<boolean>(true);
  const timerRefs = useRef<number[]>([]);

  // Refs so Wijmo event handlers always see the latest props (avoids stale closure).
  // Sync in render (not only useEffect): nested row-detail grids often skip a React props pass while
  // master state already updated — Add Row would read stale [] and overwrite grandchild data.
  const dataModelRef = useRef<any>(dataModel);
  const onDataModelChangeRef = useRef<(m: any) => void>(onDataModelChange);
  dataModelRef.current = dataModel;
  onDataModelChangeRef.current = onDataModelChange;

  const masterDetailFormDataRef = useRef(masterDetailFormData);
  masterDetailFormDataRef.current = masterDetailFormData;
  const parentOneToManyUnitIdRef = useRef(parentOneToManyUnitId);
  parentOneToManyUnitIdRef.current = parentOneToManyUnitId;
  const parentHostRowIndexRef = useRef(parentHostRowIndex);
  parentHostRowIndexRef.current = parentHostRowIndex;
  const dictOneToManyHostRowRef = useRef(dictOneToManyHostRow);
  dictOneToManyHostRowRef.current = dictOneToManyHostRow;
  const selfUnitIdRef = useRef(unitId);
  selfUnitIdRef.current = unitId;

  /** Build next `currentFormData` for this unit's grid rows (nested grandchild uses live `dictOneToManyHostRow`). */
  const patchCurrentFormWithUnitRows = (
    dm: any,
    uid: string,
    nextRowsForUnit: any[],
    extra?: { DictDocumentIdFileCode?: any }
  ) => {
    const host = dictOneToManyHostRow ?? dm?.currentFormData;
    return {
      ...host,
      ...(extra?.DictDocumentIdFileCode != null ? { DictDocumentIdFileCode: extra.DictDocumentIdFileCode } : {}),
      DictOneToManyFields: {
        ...(host?.DictOneToManyFields ?? {}),
        [uid]: nextRowsForUnit,
      },
    };
  };

  /**
   * Grandchild cascade API expects full `AppMasterDetailDto` (TransactionId, root fields, nested DictOneToManyFields).
   * Child-only grids: `patchCurrentFormWithUnitRows` is enough.
   */
  const buildMasterDetailDataDtoForCascading = (dm: any, unitUid: string, rowsForThisUnit: any[]): any => {
    const rootMaster = masterDetailFormDataRef.current;
    const parentUnitStr =
      parentOneToManyUnitIdRef.current != null && parentOneToManyUnitIdRef.current !== ''
        ? String(parentOneToManyUnitIdRef.current)
        : '';
    const host = dictOneToManyHostRowRef.current;
    if (rootMaster && parentUnitStr && host) {
      const childRows = [...(rootMaster.DictOneToManyFields?.[parentUnitStr] ?? [])];
      const patchedHost = patchCurrentFormWithUnitRows(dm, unitUid, rowsForThisUnit);
      let idx = childRows.indexOf(host);
      if (idx < 0) {
        const pri = parentHostRowIndexRef.current;
        if (typeof pri === 'number' && pri >= 0 && pri < childRows.length) {
          idx = pri;
        }
      }
      if (idx >= 0 && idx < childRows.length) {
        const nextChildRows = childRows.map((r: any, i: number) => (i === idx ? patchedHost : r));
        return {
          ...rootMaster,
          DictOneToManyFields: {
            ...(rootMaster.DictOneToManyFields ?? {}),
            [parentUnitStr]: nextChildRows,
          },
        };
      }
    }
    return patchCurrentFormWithUnitRows(dm, unitUid, rowsForThisUnit);
  };

  /**
   * Merge child-unit row changes into **root** `currentFormData`. Using `patchCurrentFormWithUnitRows` alone
   * replaces `currentFormData` with a child row when `dictOneToManyHostRow` is set — Save never sees `IsDirty` on root.
   */
  const nextFormDataForUnitRowUpdate = (
    dm: any,
    unitUid: string,
    nextRows: any[],
    extra?: { DictDocumentIdFileCode?: any }
  ) => {
    const base = buildMasterDetailDataDtoForCascading(dm, unitUid, nextRows);
    if (extra?.DictDocumentIdFileCode != null) {
      return { ...base, DictDocumentIdFileCode: extra.DictDocumentIdFileCode };
    }
    return base;
  };

  useEffect(() => {
    return () => {
      isMountedRef.current = false;
      timerRefs.current.forEach((id) => window.clearTimeout(id));
      timerRefs.current = [];
    };
  }, []);

  const deferDataModelChange = useCallback((nextDataModel: any) => {
    // queueMicrotask: parent state updates before the next event/task; setTimeout(0) was losing races with rapid Add/Edit.
    queueMicrotask(() => {
      try {
        if (!isMountedRef.current) return;
        onDataModelChangeRef.current(nextDataModel);
      } catch {
        // ignore unmounted/disposed edge cases in nested row-detail rendering
      }
    });
  }, []);

  const unitIdStr = String(unitId);

  /** Row array for this unit: prefer last committed rows until parent props catch up (fixes multi Add/Edit before re-render). */
  const getGridRowsForThisUnit = useCallback((): any[] => {
    const host = dictOneToManyHostRow ?? dataModelRef.current?.currentFormData;
    const propRows = host?.DictOneToManyFields?.[unitIdStr] || [];
    return optimisticUnitRowsRef.current ?? propRows;
  }, [dictOneToManyHostRow, unitIdStr]);

  useEffect(() => {
    optimisticUnitRowsRef.current = null;
    setOptimisticRowsBump(0);
  }, [unitId]);

  useEffect(() => {
    const host = dictOneToManyHostRow ?? dataModel?.currentFormData;
    const pr = host?.DictOneToManyFields?.[unitIdStr];
    const opt = optimisticUnitRowsRef.current;
    if (opt != null && pr === opt) {
      optimisticUnitRowsRef.current = null;
      setOptimisticRowsBump((n) => n + 1);
    }
  }, [dataModel?.currentFormData, dictOneToManyHostRow, unitIdStr]);

  const commitOptimisticUnitRows = useCallback((rows: any[]) => {
    optimisticUnitRowsRef.current = rows;
    setOptimisticRowsBump((n) => n + 1);
  }, []);

  const normalizeBool = (v: any): boolean => {
    if (v === true) return true;
    if (v === 1) return true;
    if (v === '1') return true;
    if (v === false) return false;
    if (v === 0) return false;
    if (v === '0') return false;
    return String(v).toLowerCase() === 'true';
  };

  // Read-only rules (Angular parity: unit read-only + lock + field read-only).
  const isLockTransaction = dataModel?.currentFormData?.IsLockTransaction === true;
  const dictLockUnitIds = (dataModel?.currentFormData?.DictLockUnitIds ?? {}) as Record<string, any>;
  const isUnitLocked = dictLockUnitIds?.[unitIdStr] === true;
  const isUnitReadOnly = normalizeBool(unitExDto?.IsReadOnly) || isLockTransaction || isUnitLocked;
  const isGridReadOnly = Boolean(controllerModel?.isPreview) || isUnitReadOnly;
  const isEditMode = controllerModel?.formRequestMode === 'Edit';
  const [fetchedUnitLinkTargets, setFetchedUnitLinkTargets] = useState<any[]>([]);
  const [fetchedUnitLinkedSearches, setFetchedUnitLinkedSearches] = useState<any[]>([]);
  const unitLinkTargets = (unitExDto?.AppFormLinkTargetList?.length ? unitExDto.AppFormLinkTargetList : fetchedUnitLinkTargets) || [];
  const unitLinkedSearches =
    (unitExDto?.AppTransactionUnitLinkedSearchList?.length ? unitExDto.AppTransactionUnitLinkedSearchList : fetchedUnitLinkedSearches) || [];
  const hasUnitNavigation = unitLinkTargets.length > 0 || unitLinkedSearches.length > 0;

  useEffect(() => {
    let cancelled = false;
    const unitIdValue = unitId != null ? String(unitId) : '';
    if (!unitIdValue) return;

    // Some runtime DTOs don't include unit navigation lists; fetch as fallback.
    Promise.all([
      appTransactionService.retrieveOneTransactionUnitLinkTargetList(unitIdValue).catch(() => []),
      appTransactionService.retrieveOneAppTransactionUnitLinkedSearchList(unitIdValue).catch(() => []),
    ]).then(([ltList, lsList]) => {
      if (cancelled) return;
      setFetchedUnitLinkTargets(Array.isArray(ltList) ? ltList : []);
      setFetchedUnitLinkedSearches(Array.isArray(lsList) ? lsList : []);
    });

    return () => {
      cancelled = true;
    };
  }, [unitId]);

  const buildFileUrl = useCallback((fileId: number | string | null | undefined) => {
    const id = fileId != null ? Number(fileId) : null;
    if (!id) return null;
    return fileLatestUrl(id);
  }, []);

  type CellMenuKind = 'image' | 'file';
  const CELL_CONTEXT_MENU_ESTIMATED_WIDTH = 190;
  const CELL_CONTEXT_MENU_ESTIMATED_HEIGHT = 180;
  const [cellMenu, setCellMenu] = useState<{
    open: boolean;
    x: number;
    y: number;
    kind: CellMenuKind;
    rowIndex: number;
    fieldName: string;
    fileId: number | null;
    /** Pivot projection wide-row binding (pv_*); when set, updates fold through grandchild. */
    projectionBinding?: string;
  } | null>(null);

  const closeCellMenu = useCallback(() => setCellMenu(null), []);

  const cellMenuRef = useRef<HTMLDivElement | null>(null);
  type CellMenuState = NonNullable<typeof cellMenu>;

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

  // Close cell menu when clicking outside
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

  const [uploadState, setUploadState] = useState<{
    open: boolean;
    kind: CellMenuKind;
    rowIndex: number;
    fieldName: string;
    projectionBinding?: string;
  } | null>(null);
  const [libraryState, setLibraryState] = useState<{
    open: boolean;
    kind: CellMenuKind;
    rowIndex: number;
    fieldName: string;
    projectionBinding?: string;
  } | null>(null);
  const [pendingLibraryFileId, setPendingLibraryFileId] = useState<number | null>(null);
  const [imagePreviewState, setImagePreviewState] = useState<{ open: boolean; fileId: number | null }>({
    open: false,
    fileId: null,
  });
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

  // Unit navigation: header toolbar only (no per-row Nav column — avoids duplicate UI and layout noise).
  const [linkTargetPopupState, setLinkTargetPopupState] = useState<{
    title: string;
    routeBasePath: string;
    paramObj: any;
    width?: number | null;
    height?: number | null;
    popupZIndex: number;
    showConfirmClose?: boolean;
    /** Master data search opened from unit link target (usage type 5): Confirm maps view row → host row. */
    pickerContext?: MasterDataPickerContext | null;
  } | null>(null);
  const [searchPopupState, setSearchPopupState] = useState<{
    title: string;
    width?: number | null;
    height?: number | null;
    paramObj: any;
    linkedSearch?: any;
    showConfirmClose?: boolean;
    popupZIndex: number;
  } | null>(null);
  const searchPopupRef = useRef<AppSearchHandle | null>(null);
  const linkTargetMdSearchRef = useRef<AppSearchHandle | null>(null);

  const fileStorageRootFolderId = dataModel?.currentFormStructure?.FileStorageRootFolderId ?? dataModel?.FileStorageRootFolderId;
  const sysFileTransactionId = userContext?.DictAppSetup?.SystemDefinedFileTransactionId ?? null;
  const defaultCategoryId_company = 3;
  const imagePreviewUrl = imagePreviewState.fileId ? buildFileUrl(imagePreviewState.fileId) : null;

  const normalizeMainPrefixRouteCode = (rc: string) => (rc || '').replace(/^main\./, '');

  const openInTabOrPopup = (opts: {
    routeBasePath: string;
    title: string;
    paramObj: any;
    isPopup: boolean;
    popupWidth?: number | null;
    popupHeight?: number | null;
    showConfirmClose?: boolean;
    pickerContext?: MasterDataPickerContext | null;
  }) => {
    // NAVI button behavior: always use in-page DIV popup (never open a new tab).
    setLinkTargetPopupState({
      title: opts.title,
      routeBasePath: opts.routeBasePath,
      paramObj: opts.paramObj,
      width: opts.popupWidth ?? null,
      height: opts.popupHeight ?? null,
      popupZIndex: appHelper.getNextPopupZIndex(),
      showConfirmClose: opts.showConfirmClose === true,
      pickerContext: opts.pickerContext ?? undefined,
    });
  };

  // TransactionUnitLinkTargetEditor: ids
  const LINK_TARGET_ACTION = {
    CreateBlank: 2,
    CreateFromExistingItem: 13,
    Edit: 1,
    Preview: 5,
    Delete: 3,
  };

  const executeUnitLinkTarget = (linkTarget: any, rowItem: any) => {
    if (!linkTarget || !rowItem) return;

    const rootDictOneToOneFields =
      masterDetailFormDataRef.current?.DictOneToOneFields ??
      dataModelRef.current?.currentFormData?.DictOneToOneFields ??
      {};
    const rowDictOneToOneFields = rowItem?.DictOneToOneFields ?? {};

    // Angular: SourceConditionColumn gates whether this menu item can execute.
    if (linkTarget?.SourceConditionColumn) {
      const v = rowDictOneToOneFields?.[linkTarget.SourceConditionColumn];
      if (v === false || v === 0 || v === '0' || v === 'false' || v === 'False' || v == null) return;
    }

    const usageType = Number(linkTarget?.LinkTargetUsageType ?? 0);
    const tabTitleBase = linkTarget.NavigationActionName ?? 'Action';

    if (usageType === 5) {
      // System Defined Page
      const paramId = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

      const resolveParamBySourceColumn = (sourceColumn: any) => {
        if (!sourceColumn) return null;
        const sourceColumnStr = String(sourceColumn);
        const isRoot = sourceColumnStr.indexOf('RootUnit.') >= 0;
        const colName = isRoot ? sourceColumnStr.substring(9).trim() : sourceColumnStr;
        return isRoot ? rootDictOneToOneFields?.[colName] ?? null : rowDictOneToOneFields?.[colName] ?? null;
      };

      const param1 = resolveParamBySourceColumn(linkTarget.SourceColumn2);
      const param2 = resolveParamBySourceColumn(linkTarget.SourceColumn3);

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

      openInTabOrPopup({
        routeBasePath: routeCode,
        title: tabTitle,
        paramObj,
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: routeLower === 'masterdatamanagement',
        pickerContext: routeLower === 'masterdatamanagement' ? { linkTarget, hostRow: rowItem } : undefined,
      });
      return;
    }

    // Regular transaction link target (FormMasterDetail / FormListEdit)
    const targetPkValue = linkTarget.SourceColumn1 ? rowDictOneToOneFields?.[linkTarget.SourceColumn1] : null;

    const linkTargetValueMapping: Record<string, any> = {};
    if (linkTarget.SourceColumn2 && linkTarget.TargetColumn2) {
      const sourceColumnStr = String(linkTarget.SourceColumn2);
      const fromRoot = sourceColumnStr.indexOf('RootUnit.') >= 0;
      const dbColumnName = fromRoot ? sourceColumnStr.substring(9).trim() : sourceColumnStr;
      linkTargetValueMapping[linkTarget.TargetColumn2] = fromRoot
        ? rootDictOneToOneFields?.[dbColumnName]
        : rowDictOneToOneFields?.[dbColumnName];
    }
    if (linkTarget.SourceColumn3 && linkTarget.TargetColumn3) {
      const sourceColumnStr = String(linkTarget.SourceColumn3);
      const fromRoot = sourceColumnStr.indexOf('RootUnit.') >= 0;
      const dbColumnName = fromRoot ? sourceColumnStr.substring(9).trim() : sourceColumnStr;
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

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Edit) {
      if (!targetPkValue) return;
      openInTabOrPopup({
        routeBasePath,
        title: tabTitle,
        paramObj: {
          id: linkTarget.LinkTargetTransactionId,
          param1: targetPkValue,
          param2: JSON.stringify({ linkTargetValueMapping }),
        },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Preview) {
      if (!targetPkValue) return;
      openInTabOrPopup({
        routeBasePath,
        title: tabTitle,
        paramObj: {
          id: linkTarget.LinkTargetTransactionId,
          param1: targetPkValue,
          param2: JSON.stringify({ linkTargetValueMapping, isPreview: true, isPrint: true }),
        },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (
      linkTarget.ActionType === LINK_TARGET_ACTION.CreateBlank ||
      linkTarget.ActionType === LINK_TARGET_ACTION.CreateFromExistingItem
    ) {
      if (!linkTarget.LinkTargetTransactionId) return;
      const param2Obj: any = {};
      if (linkTarget.ActionType === LINK_TARGET_ACTION.CreateFromExistingItem) {
        param2Obj.linkTargetValueMapping = linkTargetValueMapping;
      }
      if (linkTarget.DataTransferSettingId) {
        param2Obj.newFormLinkTargetPreLoadSettingObj = {
          dataTransferSettingId: linkTarget.DataTransferSettingId,
          srcTransactionRid: controllerModel?.rootPrimaryKeyValue ?? null,
        };
      }
      openInTabOrPopup({
        routeBasePath,
        title: tabTitleBase,
        paramObj: { id: linkTarget.LinkTargetTransactionId, param1: null, param2: JSON.stringify(param2Obj) },
        isPopup: Boolean(linkTarget.IsPopup),
        popupWidth: linkTarget.PopupWidth ?? null,
        popupHeight: linkTarget.PopupHeight ?? null,
        showConfirmClose: false,
      });
      return;
    }

    if (linkTarget.ActionType === LINK_TARGET_ACTION.Delete) {
      // API not yet ported for unit-navigation delete.
      return;
    }

    // CallExternalMethod/EditUserLogin are not ported yet.
  };

  const executeLinkedSearch = (linkedSearch: any, rowItem: any) => {
    if (!linkedSearch) return;

    const rootDictOneToOneFields =
      masterDetailFormDataRef.current?.DictOneToOneFields ??
      dataModelRef.current?.currentFormData?.DictOneToOneFields ??
      {};
    const siblingOneToOne =
      masterDetailFormDataRef.current?.DictSiblingOneToOneFields ??
      dataModelRef.current?.currentFormData?.DictSiblingOneToOneFields ??
      {};
    const rootUnitId = masterDetailFormDataRef.current?.RootUnitId ?? dataModelRef.current?.currentFormData?.RootUnitId ?? null;
    const rowDictOneToOneFields =
      rowItem && typeof rowItem === 'object' ? rowItem.DictOneToOneFields ?? {} : {};

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

      if (sourceUnitId == null || (rootUnitId != null && String(sourceUnitId) === String(rootUnitId))) {
        formValue = rootDictOneToOneFields?.[dataBaseFieldName];
      } else if (String(sourceUnitId) === unitIdStr) {
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
    setSearchPopupState({
      title: tabTitle,
      width: linkedSearch?.PopupWidth ?? null,
      height: linkedSearch?.PopupHeight ?? null,
      paramObj,
      linkedSearch,
      /** Confirm & Close: Action 1 add rows, Action 2 update row/form, or explicit single-row flag. */
      showConfirmClose: isLinkedSearchPopupConfirmClose(linkedSearch),
      popupZIndex: appHelper.getNextPopupZIndex(),
    });
  };

  const applyLinkedSearchAddRows = (selectedResults: any[], linkedSearch: any) => {
    if (!Array.isArray(selectedResults) || selectedResults.length === 0) return;

    const unitIdStrLocal = String(unitId);
    const currentDataModel = dataModelRef.current;
    const currentRows = getGridRowsForThisUnit();

    const fieldIdToDbName = new Map<string, string>();
    fields.forEach((f: any) => {
      if (f?.Id != null && f?.DataBaseFieldName) {
        fieldIdToDbName.set(String(f.Id), String(f.DataBaseFieldName));
      }
    });

    const viewMappings = Array.isArray(linkedSearch?.AppTransactionUnitSearchViewFieldMappingList)
      ? linkedSearch.AppTransactionUnitSearchViewFieldMappingList
      : [];

    const buildBlankRow = () => {
      const row: any = {
        DictOneToOneFields: {},
        DictOneToManyFields: {},
        IsDirty: true,
        IsNew: true,
      };
      fields.forEach((field: any) => {
        const hasDefault = field.DefaultValue !== undefined && field.DefaultValue !== null && field.DefaultValue !== '';
        row.DictOneToOneFields[field.DataBaseFieldName] = hasDefault ? field.DefaultValue : null;
      });
      // Keep child collections non-null so backend formula traversal won't hit null refs.
      const childUnits = Array.isArray(unitExDto?.Children) ? unitExDto.Children : [];
      childUnits.forEach((childUnit: any) => {
        const childUnitId = childUnit?.Id ?? childUnit?.unitId;
        if (childUnitId != null) {
          row.DictOneToManyFields[String(childUnitId)] = [];
        }
      });
      return row;
    };

    const rowsToAdd = selectedResults.map((result: any) => {
      const row = buildBlankRow();
      const dictViewValues = result?.DictViewColumnIDKeyValue ?? {};
      viewMappings.forEach((m: any) => {
        const searchViewFieldId = m?.SearchViewFieldId != null ? String(m.SearchViewFieldId) : '';
        const transactionFieldId = m?.TransactionFieldId != null ? String(m.TransactionFieldId) : '';
        const dbFieldName = fieldIdToDbName.get(transactionFieldId);
        if (!searchViewFieldId || !dbFieldName) return;
        const value =
          dictViewValues?.[searchViewFieldId] ??
          dictViewValues?.[Number(searchViewFieldId)] ??
          null;
        row.DictOneToOneFields[dbFieldName] = value;
      });
      return row;
    });

    const updatedArray = [...currentRows, ...rowsToAdd];

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.IsDirty = true;
        dictOneToManyHostRow.DictOneToManyFields = {
          ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
          [unitIdStrLocal]: updatedArray,
        };
      });
      queueNestedGridRecompute();
    }

    commitOptimisticUnitRows(updatedArray);
    deferDataModelChange({
      ...currentDataModel,
      currentFormData: {
        ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStrLocal, updatedArray),
        IsDirty: true,
      },
    });
  };

  /** Linked search Action 2 / single-row flag: map selected view row into the current grid host row. */
  const applyLinkedSearchUpdateHostRow = (selectedResults: any[], linkedSearch: any) => {
    if (!Array.isArray(selectedResults) || selectedResults.length === 0) return;

    const currentDataModel = dataModelRef.current;
    const host = dictOneToManyHostRow ?? currentDataModel.currentFormData;
    if (!host?.DictOneToOneFields) return;

    const fieldIdToDbName = new Map<string, string>();
    fields.forEach((f: any) => {
      if (f?.Id != null && f?.DataBaseFieldName) {
        fieldIdToDbName.set(String(f.Id), String(f.DataBaseFieldName));
      }
    });

    const viewMappings = Array.isArray(linkedSearch?.AppTransactionUnitSearchViewFieldMappingList)
      ? linkedSearch.AppTransactionUnitSearchViewFieldMappingList
      : [];

    const result = selectedResults[0];
    const dictViewValues = result?.DictViewColumnIDKeyValue ?? {};

    const nextFields = { ...(host.DictOneToOneFields ?? {}) };
    viewMappings.forEach((m: any) => {
      const searchViewFieldId = m?.SearchViewFieldId != null ? String(m.SearchViewFieldId) : '';
      const transactionFieldId = m?.TransactionFieldId != null ? String(m.TransactionFieldId) : '';
      const dbFieldName = fieldIdToDbName.get(transactionFieldId);
      if (!searchViewFieldId || !dbFieldName) return;
      const value =
        dictViewValues?.[searchViewFieldId] ??
        dictViewValues?.[Number(searchViewFieldId)] ??
        null;
      nextFields[dbFieldName] = value;
    });

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.DictOneToOneFields = nextFields;
        dictOneToManyHostRow.IsDirty = true;
      });
      queueNestedGridRecompute();
      const gridDataArray = getGridRowsForThisUnit();
      commitOptimisticUnitRows(gridDataArray);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, gridDataArray),
          IsDirty: true,
        },
      });
    } else {
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...currentDataModel.currentFormData,
          DictOneToOneFields: nextFields,
          IsDirty: true,
        },
      });
    }
  };

  const getSelectedUnitRowItem = useCallback(() => {
    const flex = flexGridRef.current?.control ?? flexGridRef.current;
    if (!flex?.collectionView) return null;

    const pick = (dataItem: any) => {
      if (dataItem == null) return null;
      return dataItem._originalData ?? dataItem;
    };

    const rowIndex = flex.selection?.row;
    if (typeof rowIndex === 'number' && rowIndex >= 0 && rowIndex < flex.rows.length) {
      const r = flex.rows[rowIndex];
      if (r?.dataItem != null && !(r instanceof GroupRow)) {
        const p = pick(r.dataItem);
        if (p) return p;
      }
    }

    // Toolbar clicks clear cell selection; use collection view current item or first data row.
    const cv = flex.collectionView;
    const fromCurrent = pick(cv.currentItem);
    if (fromCurrent) return fromCurrent;

    const sc = cv.sourceCollection as any;
    const n = sc?.length ?? 0;
    for (let i = 0; i < n; i++) {
      const p = pick(sc[i]);
      if (p) return p;
    }

    return null;
  }, []);

  const renderUnitNavigationHeaderButtons = () => {
    if (!hasUnitNavigation) return null;

    return (
      <>
        {unitLinkTargets.map((lt: any) => (
          <button
            key={`unit-linktarget-${lt?.Id ?? lt?.NavigationActionName ?? Math.random()}`}
            type="button"
            className={`px-2 py-1 ${theme.button_default} rounded text-xs hover:shadow-sm flex items-center gap-1`}
            onMouseDown={(e) => e.stopPropagation()}
            onClick={(e) => {
              e.stopPropagation();
              const rowItem = getSelectedUnitRowItem();
              if (!rowItem) return;
              executeUnitLinkTarget(lt, rowItem);
            }}
            title={lt?.NavigationActionName ?? 'Unit Navigate'}
          >
            <i className="fa-solid fa-link" aria-hidden />
            {lt?.NavigationActionName ?? 'Navigate'}
          </button>
        ))}

        {unitLinkedSearches.map((ls: any) => (
          <button
            key={`unit-linkedsearch-${ls?.Id ?? ls?.Name ?? Math.random()}`}
            type="button"
            className={`px-2 py-1 ${theme.button_default} rounded text-xs hover:shadow-sm flex items-center gap-1`}
            onMouseDown={(e) => e.stopPropagation()}
            onClick={(e) => {
              e.stopPropagation();
              // Allow opening when the grid is empty (e.g. AddFormGridRow): criteria from row mappings are empty.
              const rowItem = getSelectedUnitRowItem();
              executeLinkedSearch(ls, rowItem ?? {});
            }}
            title={ls?.Name ?? 'Linked Search'}
          >
            <i className="fa-solid fa-magnifying-glass" aria-hidden />
            {ls?.Name ?? 'Open Search'}
          </button>
        ))}
      </>
    );
  };

  // Nested grandchild: props `dataModel.currentFormData` is often a stale `{...rowItem}` snapshot; read live host row instead.
  const unitIdStrForGrid = String(unitId);
  const [nestedGridBump, setNestedGridBump] = useState(0);
  const queueNestedGridRecompute = useCallback(() => {
    if (dictOneToManyHostRow) {
      queueMicrotask(() => setNestedGridBump((n) => n + 1));
    }
  }, [dictOneToManyHostRow]);

  const gridData = useMemo(() => {
    if (controllerModel?.isDesignMode) {
      return [];
    }

    const formSlice = dictOneToManyHostRow ?? dataModel?.currentFormData;
    const propData = formSlice?.DictOneToManyFields?.[unitIdStrForGrid];
    const data = optimisticUnitRowsRef.current ?? propData;
    if (!formSlice?.DictOneToManyFields && !optimisticUnitRowsRef.current) {
      return [];
    }
    if (!data || !Array.isArray(data)) {
      return [];
    }

    return data.map((row: any, index: number) => {
      const flatRow: any = {
        _rowIndex: index,
        _originalData: row,
      };
      if (row.DictOneToOneFields) {
        Object.keys(row.DictOneToOneFields).forEach((key: string) => {
          flatRow[key] = row.DictOneToOneFields[key];
        });
      }
      // Ensure stable field values even when rows store PK/FK at top-level (e.g. `Id`), not in DictOneToOneFields.
      // Without this, nested grandchild edits/adds that trigger a remap can drop the PK from the flat row and appear "blank".
      // Note: `fields` is declared later; avoid TDZ by reading directly from `unitExDto`.
      (unitExDto?.AppTransactionFieldList ?? []).forEach((f: any) => {
        const dbName = f?.DataBaseFieldName;
        if (!dbName) return;
        if (Object.prototype.hasOwnProperty.call(flatRow, dbName)) return;
        const v = readTransactionRowField(row, dbName);
        if (v !== undefined) flatRow[dbName] = v;
      });
      return flatRow;
    });
    // nestedGridBump: force remap when host rows mutate in place / React skips prop updates from Wijmo detail
    // optimisticRowsBump: root grid rows updated before parent props refresh
    // eslint-disable-next-line react-hooks/exhaustive-deps -- dictOneToManyHostRow branch uses bump + live host slice
  }, [
    controllerModel?.isDesignMode,
    unitIdStrForGrid,
    dictOneToManyHostRow,
    nestedGridBump,
    optimisticRowsBump,
    dictOneToManyHostRow
      ? dictOneToManyHostRow?.DictOneToManyFields?.[unitIdStrForGrid]
      : dataModel?.currentFormData?.DictOneToManyFields?.[unitIdStrForGrid],
    dataModel?.currentFormData?.DictOneToManyFields,
  ]);

  const updateRowFileValue = useCallback(
    (rowIndex: number, fieldName: string, newFileId: number | null, fileName?: string) => {
      const currentDataModel = dataModelRef.current;
      const unitIdStr = unitId.toString();
      const host = dictOneToManyHostRow ?? currentDataModel.currentFormData;
      const gridDataArray = getGridRowsForThisUnit();
      if (rowIndex < 0 || rowIndex >= gridDataArray.length) return;

      const rowData = gridDataArray[rowIndex];
      const nextOneToOne = {
        ...(rowData?.DictOneToOneFields ?? {}),
        [fieldName]: newFileId,
      };
      const nextRowData = {
        ...rowData,
        [fieldName]: newFileId,
        DictOneToOneFields: nextOneToOne,
        IsDirty: true,
      };
      const nextGridDataArray = gridDataArray.map((r: any, idx: number) => (idx === rowIndex ? nextRowData : r));

      // Maintain DictDocumentIdFileCode mapping (Angular parity) — prefer master form when nested
      const docBase =
        currentDataModel.currentFormData?.DictDocumentIdFileCode ??
        host?.DictDocumentIdFileCode ??
        {};
      const nextDictDoc = { ...docBase };
      if (newFileId && fileName) {
        nextDictDoc[String(newFileId)] = fileName;
      }

      // Mutate live host + master doc map so cell templates re-render without waiting for parent.
      tryMutateInPlace(() => {
        host.IsDirty = true;
        host.DictOneToManyFields = {
          ...(host.DictOneToManyFields ?? {}),
          [unitIdStr]: nextGridDataArray,
        };
      });
      if (currentDataModel.currentFormData && currentDataModel.currentFormData !== host) {
        tryMutateInPlace(() => {
          currentDataModel.currentFormData.IsDirty = true;
          currentDataModel.currentFormData.DictDocumentIdFileCode = nextDictDoc;
        });
      } else {
        tryMutateInPlace(() => {
          host.DictDocumentIdFileCode = { ...(host.DictDocumentIdFileCode ?? {}), ...nextDictDoc };
        });
      }

      commitOptimisticUnitRows(nextGridDataArray);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, nextGridDataArray, {
            DictDocumentIdFileCode: nextDictDoc,
          }),
          IsDirty: true,
        },
      });

      queueNestedGridRecompute();

      // Ensure this nested grid redraws file/image cells immediately.
      try {
        const flexGridControl = flexGridRef.current?.control;
        flexGridControl?.invalidate?.();
        flexGridControl?.refresh?.();
      } catch {
        // ignore
      }
    },
    [commitOptimisticUnitRows, deferDataModelChange, dictOneToManyHostRow, getGridRowsForThisUnit, queueNestedGridRecompute, unitId]
  );

  // Get field definitions from unit
  const fields = useMemo(() => {
    if (!unitExDto?.AppTransactionFieldList) {
      return [];
    }

    const dictAll = transactionExDto?.DictAllTransactionField as Record<string | number, any> | undefined;

    // Filter visible fields and sort by SortOrder
    return unitExDto.AppTransactionFieldList
      .map((field: any) => enrichTransactionFieldFromDict(field, dictAll))
      .filter((field: any) => isRuntimeTransactionFieldVisible(field))
      .sort((a: any, b: any) => {
        const sortA = a.SortOrder || 0;
        const sortB = b.SortOrder || 0;
        return sortA - sortB;
      });
  }, [unitExDto, transactionExDto?.DictAllTransactionField]);

  // API / JSON may use PascalCase or camelCase; missing enum dict still uses 5 / 6.
  const rawGridDisplay =
    unitExDto?.EmGridViewDisplayType ?? (unitExDto as any)?.emGridViewDisplayType;
  const gridViewDisplayTypeNum = Number(rawGridDisplay);
  const gridViewDisplayType = Number.isFinite(gridViewDisplayTypeNum) ? gridViewDisplayTypeNum : 1;
  const pairDisplayVal = Number(emAppTransactionGridDisplayTypeEnum?.AvailableSelectGridPair ?? 5);
  const multiBoxDisplayVal = Number(emAppTransactionGridDisplayTypeEnum?.MultipleSelectBox ?? 6);
  const pivotEditVal = Number(emAppTransactionGridDisplayTypeEnum?.PivotEditGrid ?? 3);
  const isAvailableSelectPair = gridViewDisplayType === pairDisplayVal;
  const isMultipleSelectBox = gridViewDisplayType === multiBoxDisplayVal;
  const isAvailableSelectLayout = isAvailableSelectPair || isMultipleSelectBox;
  const isPivotEditGrid = gridViewDisplayType === pivotEditVal;
  // Matrix unit: rows are (re)generated server-side as the Cartesian product of the
  // matrix foreign-key field values (Angular generateMatrix / GenerateMatrix endpoint).
  const isMatrixUnit = Boolean(unitExDto?.IsMatrixUnit ?? (unitExDto as any)?.isMatrixUnit);

  // Pivot edit grid data — only evaluated when EmGridViewDisplayType === 3
  const pivotDto = isPivotEditGrid
    ? (dataModel?.currentFormStructure?.DictUnitIdPivotGrid?.[Number(unitId)] ?? null)
    : null;
  const pivotRows: any[] = isPivotEditGrid
    ? (dataModel?.currentFormData?.DictOneToManyFields?.[String(unitId)] ?? [])
    : [];
  // Primary-key field names for this unit — excluded from pivot row grouping so a unique
  // PK marked as "Is Pivot Row" doesn't split every generated row into its own pivot row.
  const pivotPrimaryKeyFieldNames: string[] = useMemo(() => {
    if (!isPivotEditGrid) return [];
    const list = (unitExDto?.AppTransactionFieldList ?? []) as any[];
    return list
      .filter((f: any) => normalizeBool(f?.IsPrimaryKey))
      .map((f: any) => f?.DataBaseFieldName)
      .filter(Boolean);
  }, [isPivotEditGrid, unitExDto]);
  // Per-field configured column width (DisplayWidth), keyed by field Id — pivot fields from the
  // server don't carry DisplayWidth, so resolve it from the full unit field list.
  const pivotColumnWidthByFieldId = useMemo(() => {
    const m = new Map<string, number>();
    if (!isPivotEditGrid) return m;
    const dictAll = transactionExDto?.DictAllTransactionField as Record<string | number, any> | undefined;
    const list = (unitExDto?.AppTransactionFieldList ?? []) as any[];
    for (const f0 of list) {
      const f = enrichTransactionFieldFromDict(f0, dictAll);
      if (f?.Id == null) continue;
      const raw = (f as any).DisplayWidth;
      const w = typeof raw === 'number' ? raw : raw ? parseInt(String(raw), 10) : NaN;
      if (Number.isFinite(w)) m.set(String(f.Id), Number(w));
    }
    return m;
  }, [isPivotEditGrid, unitExDto, transactionExDto?.DictAllTransactionField]);
  // Base size comes from the parent spec header (DictOneToOneFields on master form)
  const pivotBaseSizeId = isPivotEditGrid
    ? (dataModel?.currentFormData?.DictOneToOneFields?.BaseSizeDetailId ?? null)
    : null;
  // POM grading uses the specialized PivotEditGridPanel (amber base-size column, delta lock).
  // Every other pivot-edit unit uses the generic AppPivotDto-driven MatrixPivotEditGrid.
  const isPomGradingPivot = Boolean(
    pivotDto &&
      (((pivotDto.PivotColumnFields ?? []) as any[]).some(
        (f: any) => f?.DataBaseFieldName === 'SizeRunSizeId'
      ) ||
        ((pivotDto.PivotValueFields ?? []) as any[]).some(
          (f: any) => f?.DataBaseFieldName === 'GradingDelta'
        ))
  );

  const availableSourceUnitId = unitExDto?.AvailableSourceUnitId ?? null;

  const sourceUnitExDto = useMemo(() => {
    if (!isAvailableSelectLayout || availableSourceUnitId == null || !transactionExDto) return null;
    const roots: any[] = transactionExDto.AppTransactionUnitList ?? transactionExDto.AppTransactionUnitTree ?? [];
    const allUnits: any[] = [];
    const visit = (u: any) => {
      if (!u) return;
      allUnits.push(u);
      (u.Children ?? []).forEach((c: any) => visit(c));
    };
    roots.forEach((r: any) => visit(r));
    return allUnits.find((u: any) => String(u?.Id) === String(availableSourceUnitId)) ?? null;
  }, [isAvailableSelectLayout, availableSourceUnitId, transactionExDto]);

  const sourceFields = useMemo(() => {
    if (!sourceUnitExDto?.AppTransactionFieldList) return [];
    const dictAll = transactionExDto?.DictAllTransactionField as Record<string | number, any> | undefined;
    return sourceUnitExDto.AppTransactionFieldList
      .map((field: any) => enrichTransactionFieldFromDict(field, dictAll))
      .filter((field: any) => isRuntimeTransactionFieldVisible(field))
      .sort((a: any, b: any) => (a.SortOrder || 0) - (b.SortOrder || 0));
  }, [sourceUnitExDto, transactionExDto?.DictAllTransactionField]);

  const mappingSubscribeField = useMemo(() => {
    if (!fields.length) return null;
    return (
      fields.find(
        (f: any) =>
          f.MappingToAvailableSourceUnitTransactionFieldId != null ||
          f.MappingToAvailableSourceUnitTransactionFieldExDto != null
      ) ?? null
    );
  }, [fields]);

  const mappingSourceField = useMemo(() => {
    if (!sourceUnitExDto?.AppTransactionFieldList || !mappingSubscribeField) return null;
    const srcId =
      mappingSubscribeField.MappingToAvailableSourceUnitTransactionFieldId ??
      mappingSubscribeField.MappingToAvailableSourceUnitTransactionFieldExDto?.Id;
    if (srcId == null) return null;
    return sourceUnitExDto.AppTransactionFieldList.find((f: any) => String(f.Id) === String(srcId)) ?? null;
  }, [sourceUnitExDto, mappingSubscribeField]);

  const mappingSubscribeDb = mappingSubscribeField?.DataBaseFieldName ?? '';
  const mappingSourceDb = mappingSourceField?.DataBaseFieldName ?? '';

  /** Display label on MultipleSelectBox tiles (Angular: first visible TextBox on source unit, else mapping field). */
  const sourceDisplayFieldDbName = useMemo(() => {
    if (!mappingSourceDb) return '';
    if (!sourceUnitExDto?.AppTransactionFieldList) return mappingSourceDb;
    const textBoxType = Number(emAppControlTypeEnum?.TextBox ?? 2);
    const sorted = [...sourceUnitExDto.AppTransactionFieldList].sort(
      (a: any, b: any) => (a.SortOrder || 0) - (b.SortOrder || 0)
    );
    const visible = sorted
      .map((f: any) => enrichTransactionFieldFromDict(f, transactionExDto?.DictAllTransactionField))
      .filter((f: any) => isRuntimeTransactionFieldVisible(f));
    const firstText = visible.find((f: any) => Number(f.ControlType) === textBoxType);
    return firstText?.DataBaseFieldName ?? mappingSourceDb;
  }, [sourceUnitExDto, mappingSourceDb, emAppControlTypeEnum?.TextBox, transactionExDto?.DictAllTransactionField]);

  const availableSelectConfigOk =
    isAvailableSelectLayout &&
    availableSourceUnitId != null &&
    sourceUnitExDto != null &&
    mappingSubscribeField != null &&
    mappingSourceField != null &&
    Boolean(mappingSubscribeDb) &&
    Boolean(mappingSourceDb);

  const sourceUnitIdStr =
    availableSourceUnitId != null && isAvailableSelectLayout ? String(availableSourceUnitId) : '';

  /** Subscribe-side keys already chosen — hide matching rows from the available pool (Angular shuttle parity). */
  const subscribeUnitIdStrForFilter = String(unitId);
  /**
   * Same row array as `getGridRowsForThisUnit` (includes optimistic overlay). MultipleSelectBox `checked`
   * must use this — otherwise toggles update `optimisticUnitRowsRef` but UI still reads stale props → no visual change.
   */
  const subscribeRowsForKeySet = useMemo((): any[] => {
    const host = dictOneToManyHostRow ?? dataModel?.currentFormData;
    const propRows = host?.DictOneToManyFields?.[subscribeUnitIdStrForFilter] || [];
    return optimisticUnitRowsRef.current ?? propRows;
  }, [
    subscribeUnitIdStrForFilter,
    dictOneToManyHostRow,
    dataModel?.currentFormData?.DictOneToManyFields?.[subscribeUnitIdStrForFilter],
    optimisticRowsBump,
    nestedGridBump,
  ]);

  const selectedKeysAlreadyInSubscribeGrid = useMemo(() => {
    if (!isAvailableSelectLayout || !availableSelectConfigOk || !mappingSubscribeDb) {
      return new Set<string>();
    }
    const s = new Set<string>();
    for (const r of subscribeRowsForKeySet) {
      const v = readTransactionRowField(r, mappingSubscribeDb);
      if (v != null && v !== '') s.add(String(v));
    }
    return s;
  }, [
    isAvailableSelectLayout,
    availableSelectConfigOk,
    mappingSubscribeDb,
    subscribeRowsForKeySet,
  ]);

  const availableGridData = useMemo(() => {
    if (!isAvailableSelectLayout || !sourceUnitIdStr || !availableSelectConfigOk) {
      return [];
    }
    const formSlice = dictOneToManyHostRow ?? dataModel?.currentFormData;
    const propData = formSlice?.DictOneToManyFields?.[sourceUnitIdStr];
    const data = Array.isArray(propData) ? propData : [];
      const filtered = isAvailableSelectPair
      ? data.filter((row: any) => {
          const k = readTransactionRowField(row, mappingSourceDb);
          const ks = k != null && k !== '' ? String(k) : '';
          if (!ks) return true;
          return !selectedKeysAlreadyInSubscribeGrid.has(ks);
        })
      : data;
    return filtered.map((row: any, index: number) => ({
      _rowIndex: index,
      _originalData: row,
      IsSelected: row.IsSelected === true,
      ...(row.DictOneToOneFields ?? {}),
    }));
  }, [
    isAvailableSelectLayout,
    isAvailableSelectPair,
    sourceUnitIdStr,
    availableSelectConfigOk,
    mappingSourceDb,
    selectedKeysAlreadyInSubscribeGrid,
    dictOneToManyHostRow,
    nestedGridBump,
    optimisticRowsBump,
    availableDataBump,
    dictOneToManyHostRow
      ? dictOneToManyHostRow?.DictOneToManyFields?.[sourceUnitIdStr]
      : dataModel?.currentFormData?.DictOneToManyFields?.[sourceUnitIdStr],
    dataModel?.currentFormData?.DictOneToManyFields,
  ]);

  /** Full source pool for MultipleSelectBox tile UI (checkboxes — not filtered like shuttle left grid). */
  const allSourceRowsForMultiSelect = useMemo(() => {
    if (!isMultipleSelectBox || !isAvailableSelectLayout || !sourceUnitIdStr || !availableSelectConfigOk) {
      return [];
    }
    const formSlice = dictOneToManyHostRow ?? dataModel?.currentFormData;
    const propData = formSlice?.DictOneToManyFields?.[sourceUnitIdStr];
    return Array.isArray(propData) ? propData : [];
  }, [
    isMultipleSelectBox,
    isAvailableSelectLayout,
    sourceUnitIdStr,
    availableSelectConfigOk,
    dictOneToManyHostRow,
    dataModel?.currentFormData?.DictOneToManyFields?.[sourceUnitIdStr],
    nestedGridBump,
    optimisticRowsBump,
  ]);

  const grandChildUnitList = useMemo(() => {
    const filterByVisibility = (u: any) =>
      u && (u.IsFormLayoutVisible == null || normalizeBool(u.IsFormLayoutVisible));

    // Primary (expected) source: direct children of this grid unit.
    const directChildren = (unitExDto?.Children ?? []).filter(filterByVisibility);

    if (directChildren.length > 0) return directChildren;

    // Fallback: derive grandchild unit definitions from the full transaction unit tree.
    // This fixes cases where the DTO for this grid unit doesn't include `Children`,
    // but the unit exists elsewhere with `ParentTransactionUnitId`.
    const roots: any[] =
      transactionExDto?.AppTransactionUnitList ??
      transactionExDto?.AppTransactionUnitTree ??
      [];

    const allUnits: any[] = [];
    const visit = (u: any) => {
      if (!u) return;
      allUnits.push(u);
      if (Array.isArray(u.Children)) {
        u.Children.forEach((c: any) => visit(c));
      }
    };
    roots.forEach((r: any) => visit(r));

    const parentId = Number(unitId);
    const derived = allUnits
      .filter((u: any) => Number(u?.ParentTransactionUnitId ?? NaN) === parentId)
      .filter(filterByVisibility);

    return derived;
  }, [unitExDto?.Children, transactionExDto, unitId, normalizeBool]);

  /** Keep grandchild detail template callback stable (List Edit parity) — avoids FlexGridDetail collapse on grandchild edit/add/delete. */
  const grandChildUnitListRef = useRef<any[]>([]);
  grandChildUnitListRef.current = grandChildUnitList;
  const grandChildUnitsForDetailRef = useRef<any[]>([]);

  // ----- Child-unit pivot projection (EmGridViewDisplayType.ChildUnitPivotColumns) -----
  // When a grandchild of THIS unit is configured to project onto the parent (child) grid,
  // we render its rows as dynamic pivot columns here instead of a nested grandchild grid.
  const childUnitPivotColumnsVal = Number(emAppTransactionGridDisplayTypeEnum?.ChildUnitPivotColumns ?? 7);
  const childPivotProjection = useMemo(() => {
    const dict = dataModel?.currentFormStructure?.DictUnitIdPivotGrid;
    if (!dict) return null;
    for (const gc of grandChildUnitList) {
      const gcId = gc?.Id ?? gc?.unitId;
      if (gcId == null) continue;
      const desc = dict[Number(gcId)] ?? dict[String(gcId)];
      if (desc?.IsChildUnitPivotColumns && Number(desc.HostParentUnitId) === Number(unitId)) {
        return { grandchildUnitId: Number(gcId), grandchildUnit: gc, descriptor: desc };
      }
    }
    return null;
  }, [dataModel?.currentFormStructure?.DictUnitIdPivotGrid, grandChildUnitList, unitId]);
  const isChildPivotProjectionHost = Boolean(childPivotProjection);

  // Pivot grandchild is edited via projected columns on this grid — never as a nested row-detail unit.
  const grandChildUnitsForDetail = useMemo(() => {
    if (!isChildPivotProjectionHost || childPivotProjection?.grandchildUnitId == null) {
      return grandChildUnitList;
    }
    const pivotGcId = childPivotProjection.grandchildUnitId;
    return grandChildUnitList.filter((u: any) => {
      const id = u?.Id ?? u?.unitId;
      return Number(id) !== Number(pivotGcId);
    });
  }, [grandChildUnitList, isChildPivotProjectionHost, childPivotProjection?.grandchildUnitId]);

  grandChildUnitsForDetailRef.current = grandChildUnitsForDetail;

  // Per-field configured width (DisplayWidth) for host + grandchild fields, keyed by field Id.
  // (Pure presentation; all data transform happens server-side.)
  const projectionWidthByFieldId = useMemo(() => {
    const m = new Map<string, number>();
    if (!isChildPivotProjectionHost) return m;
    const dictAll = transactionExDto?.DictAllTransactionField as Record<string | number, any> | undefined;
    const collect = (list: any[]) => {
      for (const f0 of list ?? []) {
        const f = enrichTransactionFieldFromDict(f0, dictAll);
        if (f?.Id == null) continue;
        const raw = (f as any).DisplayWidth;
        const w = typeof raw === 'number' ? raw : raw ? parseInt(String(raw), 10) : NaN;
        if (Number.isFinite(w)) m.set(String(f.Id), Number(w));
      }
    };
    collect(unitExDto?.AppTransactionFieldList ?? []);
    collect(childPivotProjection?.grandchildUnit?.AppTransactionFieldList ?? []);
    return m;
  }, [isChildPivotProjectionHost, transactionExDto?.DictAllTransactionField, unitExDto, childPivotProjection]);

  // Server-built projection model (columns + wide rows). Build runs on server; fold-back on edit is client-side.
  const [projectionModel, setProjectionModel] = useState<ChildPivotProjectionModel | null>(null);
  const projectionModelRef = useRef<ChildPivotProjectionModel | null>(null);
  projectionModelRef.current = projectionModel;
  const projectionFlexGridRef = useRef<any>(null);
  const childPivotProjectionRef = useRef(childPivotProjection);
  childPivotProjectionRef.current = childPivotProjection;

  // Pivot grid is ready once the server model is loaded. While loading (or on failure), fall back to the
  // normal child-unit FlexGrid so the page is never blank. When loaded, ColumnGroups may be empty (no source
  // rows) — host columns still render; pivot columns appear when the source grid has data.
  const pivotProjectionModelReady = useMemo(
    () =>
      isChildPivotProjectionHost &&
      projectionModel != null &&
      projectionModel.IsConfigured !== false,
    [isChildPivotProjectionHost, projectionModel],
  );

  // Rebuild the grid ONLY on STRUCTURAL changes — i.e. the source-grid column domain changes, or
  // host rows are added/removed. Plain cell edits must NOT rebuild: a per-edit rebuild would replace
  // WideRows, reset the CollectionView (selection jumps to row 1) and overwrite the value being typed
  // with the server round-trip result ("回档"). Cell values are intentionally excluded from this key.
  const projectionRebuildKey = useMemo(() => {
    if (!isChildPivotProjectionHost) return '';
    const fd = masterDetailFormData ?? dataModel?.currentFormData;
    const desc: any = childPivotProjection?.descriptor;
    const srcUnitId = desc?.ColumnSourceUnitId;
    const srcField = desc?.ColumnSourceFieldName;
    const srcRows: any[] = srcUnitId != null ? fd?.DictOneToManyFields?.[String(srcUnitId)] ?? [] : [];
    const srcSig = srcField
      ? srcRows.map((r: any) => readTransactionRowField(r, srcField) ?? '').join('|')
      : String(srcRows.length);
    const hostRows: any[] = fd?.DictOneToManyFields?.[String(unitId)] ?? [];
    return `${srcSig}#${hostRows.length}`;
  }, [isChildPivotProjectionHost, masterDetailFormData, dataModel?.currentFormData, childPivotProjection, unitId]);

  useEffect(() => {
    if (!isChildPivotProjectionHost) {
      setProjectionModel(null);
      return;
    }
    let cancelled = false;
    const fd = buildMasterDetailDataDtoForCascading(
      dataModelRef.current,
      unitIdStr,
      getGridRowsForThisUnit()
    );
    appTransactionService
      .convertGrandChildDataToPivotColumns(fd, Number(unitId))
      .then((m) => {
        if (!cancelled) setProjectionModel(m ?? null);
      })
      .catch((e) => {
        if (!cancelled) setProjectionModel(null);
        appHelper.debugLog('buildChildPivotProjection failed', e);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isChildPivotProjectionHost, unitId, projectionRebuildKey]);

  // Fold edited wide rows back into nested grandchild structure (client-side, mirrors server BL).
  // No API per cell edit — only BuildChildPivotProjection runs on load / structural rebuild.
  const getProjectionWideRowsFromGrid = (grid: any): any[] => {
    const cv = grid?.itemsSource;
    if (cv?.sourceCollection && Array.isArray(cv.sourceCollection)) {
      return [...cv.sourceCollection];
    }
    if (Array.isArray(cv)) return [...cv];
    return [];
  };

  const handleProjectionWideRowsChange = useCallback(
    (wideRows: any[]) => {
      const currentDataModel = dataModelRef.current;
      const model = projectionModelRef.current;
      if (!model?.IsConfigured) return;

      const currentRows = getGridRowsForThisUnit();
      const fieldDefs = childPivotProjectionRef.current?.grandchildUnit?.AppTransactionFieldList ?? [];
      const updatedRows = foldWideRowsIntoChildRows(currentRows, wideRows, model, fieldDefs);

      const unitIdStrLocal = String(unitId);
      const hostRow = dictOneToManyHostRowRef.current;
      if (hostRow) {
        tryMutateInPlace(() => {
          hostRow.IsDirty = true;
          hostRow.DictOneToManyFields = {
            ...(hostRow.DictOneToManyFields ?? {}),
            [unitIdStrLocal]: updatedRows,
          };
        });
        queueNestedGridRecompute();
      }
      commitOptimisticUnitRows(updatedRows);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStrLocal, updatedRows),
          IsDirty: true,
        },
      });
    },
    [
      unitId,
      commitOptimisticUnitRows,
      deferDataModelChange,
      getGridRowsForThisUnit,
      queueNestedGridRecompute,
    ]
  );

  const updateProjectionFileValue = useCallback(
    (rowIndex: number, binding: string, newFileId: number | null, fileName?: string) => {
      const flex = projectionFlexGridRef.current?.control ?? projectionFlexGridRef.current;
      const baseWideRows = flex
        ? getProjectionWideRowsFromGrid(flex)
        : [...(projectionModelRef.current?.WideRows ?? [])];
      if (rowIndex < 0 || rowIndex >= baseWideRows.length) return;

      const nextWideRows = baseWideRows.map((wr: any, i: number) =>
        i === rowIndex ? { ...wr, [binding]: newFileId } : wr,
      );

      const currentDataModel = dataModelRef.current;
      const host =
        dictOneToManyHostRowRef.current ??
        currentDataModel?.currentFormData;
      const docBase =
        currentDataModel?.currentFormData?.DictDocumentIdFileCode ?? host?.DictDocumentIdFileCode ?? {};
      const nextDictDoc = { ...docBase };
      if (newFileId && fileName) {
        nextDictDoc[String(newFileId)] = fileName;
      }

      if (currentDataModel?.currentFormData) {
        tryMutateInPlace(() => {
          currentDataModel.currentFormData.DictDocumentIdFileCode = nextDictDoc;
          currentDataModel.currentFormData.IsDirty = true;
        });
      }

      handleProjectionWideRowsChange(nextWideRows);
      setProjectionModel((prev) => (prev ? { ...prev, WideRows: nextWideRows } : prev));

      try {
        const cv = flex?.itemsSource;
        if (cv) {
          (cv as any).sourceCollection = nextWideRows;
          cv.refresh?.();
        }
        flex?.invalidate?.();
      } catch {
        // ignore
      }
    },
    [handleProjectionWideRowsChange],
  );

  const assignUploadedFileToCell = useCallback(
    async (
      kind: CellMenuKind,
      rowIndex: number,
      fieldName: string,
      fileId: number,
      fallbackName?: string,
      projectionBinding?: string,
    ) => {
      let fileCode = fallbackName ?? '';
      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(fileId));
        fileCode = String(dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? fallbackName ?? '');
      } catch {
        // keep fallback
      }

      if (projectionBinding) {
        updateProjectionFileValue(rowIndex, projectionBinding, fileId, fileCode || undefined);
      } else {
        updateRowFileValue(rowIndex, fieldName, fileId, fileCode || undefined);
      }
      void kind;
    },
    [updateProjectionFileValue, updateRowFileValue],
  );

  const handleProjectionImageCellMenu = useCallback((ctx: ProjectionImageCellContext) => {
    openCellMenuAt(ctx.clientX, ctx.clientY, {
      kind: 'image',
      rowIndex: ctx.rowIndex,
      fieldName: ctx.dbFieldName,
      fileId: ctx.fileId,
      projectionBinding: ctx.binding,
    });
  }, [openCellMenuAt]);

  const projectionCascadingInitRef = useRef<any>(null);

  const transactionExDtoRef = useRef(transactionExDto);
  transactionExDtoRef.current = transactionExDto;

  const controllerModelRef = useRef(controllerModel);
  controllerModelRef.current = controllerModel;

  const grandChildPopupModeValue = Number(emAppGrandChildEditModeEnum?.Popup ?? 2);
  const currentGrandChildEditMode = Number(transactionExDto?.EmGrandChildEditMode ?? 1);
  const isGrandChildPopupMode = currentGrandChildEditMode === grandChildPopupModeValue;
  const rowHasDetail = useCallback(
    (row: any) => {
      // Wijmo FlexGridDetail: rowHasDetail(row: Row)
      const rowData = row?.dataItem?._originalData ?? row?.dataItem ?? row;
      if (!rowData?.DictOneToManyFields) return false;
      const dict = rowData.DictOneToManyFields ?? {};
      // Fallback-first: if any dict entry is an array, show expand icon.
      // This matches Angular behavior (detail exists when grandchild row collections exist),
      // even if unit definitions are missing/filtered.
      if (Object.values(dict).some((v: any) => Array.isArray(v))) return true;

      return false;
    },
    []
  );

  /**
   * Angular formMasterDetailCtrl: child grid column dataMap uses dictFieldEntityDataSource built from
   * DictStandAloneEntityDataSource for display; on cell edit beginning, column switches to cascading
   * lookup; after cellEditEnded, col.dataMap is restored to the standalone map.
   * Here we read standalone only from formStructure (not dictFieldEntityDataMap, which may prefer cascading).
   */
  const buildStandaloneDataMapFromFormStructure = useCallback((model: any, fieldIdStr: string): DataMap | null => {
    if (!fieldIdStr) return null;
    const dictEntity = model?.currentFormStructure?.DictStandAloneEntityDataSource ?? {};
    const dictMapping = model?.currentFormStructure?.DictStandAloneFiledIDMappingEntityID ?? {};
    const entityId = dictMapping[fieldIdStr];
    if (entityId == null) return null;
    const items = dictEntity[String(entityId)] ?? dictEntity[entityId] ?? [];
    if (!Array.isArray(items)) return null;
    return new DataMap(items, 'Id', 'Display');
  }, []);

  // Stable callbacks for the projection grid so a fold-driven parent re-render (async) does NOT
  // re-render the (memoized) grid and disrupt the cell the user is currently editing.
  const resolveProjectionDataMap = useCallback(
    (fieldId: any) => buildStandaloneDataMapFromFormStructure(dataModelRef.current, String(fieldId)),
    [buildStandaloneDataMapFromFormStructure]
  );
  const resolveProjectionWidth = useCallback(
    (fieldId: any) => projectionWidthByFieldId.get(String(fieldId)),
    [projectionWidthByFieldId]
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

  const restoreStandaloneColumnDataMap = useCallback(
    (gridSender: any, colIndex: number) => {
      const flex = gridSender?.control ?? gridSender;
      const col = flex?.columns?.[colIndex];
      if (!col) return;
      const fieldIdStr = String(col.name ?? '');
      if (!fieldIdStr) return;
      const field = fields.find((f: any) => String(f.Id) === fieldIdStr);
      if (!field || !isDdLikeControlType(field.ControlType)) return;
      const dm = buildStandaloneDataMapFromFormStructure(dataModelRef.current, fieldIdStr);
      if (dm) {
        col.dataMap = dm;
        flex.invalidate?.();
      }
    },
    [buildStandaloneDataMapFromFormStructure, fields, isDdLikeControlType]
  );

  // Bind grid data to a stable CollectionView instance. Replacing `itemsSource` with a new
  // CollectionView on every `gridData` change tears down FlexGrid row details and unmounts
  // nested React roots (e.g. grandchild grid in Master Detail) → "Cannot update an unmounted root".
  // Never set collectionView to null — keep one instance so FlexGrid is not unmounted while Wijmo
  // still dispatches prop sync (fixes null.selectionMode in updateProps/copy).
  // useLayoutEffect: sync before paint so nested row-detail grids are not mid-unmount when Wijmo runs microtasks.
  useLayoutEffect(() => {
    setCollectionView((prevCv) => {
      const nextItems = gridData.length > 0 || fields.length > 0 ? gridData : [];
      syncCollectionViewFlatRows(prevCv, nextItems);
      return prevCv;
    });
  }, [gridData, fields]);

  useLayoutEffect(() => {
    if (!isAvailableSelectPair || !availableSelectConfigOk) return;
    setAvailableCollectionView((prevCv) => {
      const nextItems = availableGridData.length > 0 || sourceFields.length > 0 ? availableGridData : [];
      syncCollectionViewFlatRows(prevCv, nextItems);
      return prevCv;
    });
  }, [availableGridData, sourceFields, isAvailableSelectPair, availableSelectConfigOk]);

  /** Non-edit display: formStructure standalone entity only (Angular: grid uses full entity list for display text). */
  const getDataMapForField = (field: any): DataMap | null => {
    if (!isDdLikeControlType(field?.ControlType)) return null;
    const fieldIdStr = field.Id != null ? String(field.Id) : '';
    if (!fieldIdStr) return null;
    return buildStandaloneDataMapFromFormStructure(dataModel, fieldIdStr);
  };

  const getDataMapForSourceField = (field: any): DataMap | null => {
    if (!isDdLikeControlType(field?.ControlType)) return null;
    const fieldIdStr = field.Id != null ? String(field.Id) : '';
    if (!fieldIdStr) return null;
    return buildStandaloneDataMapFromFormStructure(dataModel, fieldIdStr);
  };

  /** Keep Wijmo column.dataMap in sync with standalone (React prop updates alone are not always applied). */
  useEffect(() => {
    const flexGridControl = flexGridRef.current?.control;
    if (!flexGridControl || !fields?.length) return;

    fields.forEach((field: any) => {
      if (!isDdLikeControlType(field?.ControlType)) return;
      const fieldIdStr = field.Id != null ? String(field.Id) : '';
      if (!fieldIdStr) return;
      const dm = buildStandaloneDataMapFromFormStructure(dataModel, fieldIdStr);
      if (!dm) return;
      const col = flexGridControl.columns?.find((c: any) => String(c?.name ?? '') === String(fieldIdStr));
      if (col) col.dataMap = dm;
    });

    flexGridControl.invalidate();
    flexGridControl.refresh();
  }, [
    buildStandaloneDataMapFromFormStructure,
    dataModel?.currentFormStructure?.DictStandAloneEntityDataSource,
    dataModel?.currentFormStructure?.DictStandAloneFiledIDMappingEntityID,
    dataModel?.currentFormStructure,
    fields,
    emAppControlTypeEnum,
    isDdLikeControlType,
  ]);

  /**
   * Angular formMasterDetailCtrl.childCellEditBeginning:
   * - Parent field in same unit as grid → row DictCascadingFiledDataSource[fieldId]
   * - Parent in another unit (e.g. root) → currentFormData.DictCascadingFiledDataSource[fieldId]
   * Use dataModelRef so we never read stale DictCascadingFiledDataSource after root parent DDL changes.
   */
  const resolveCascadingLookupItemsForCellEdit = (
    currentDataModel: any,
    rowData: any,
    fieldIdStr: string
  ): any[] | null => {
    const rootFormForCascading = masterDetailFormDataRef.current ?? currentDataModel?.currentFormData;
    const rootDict = rootFormForCascading?.DictCascadingFiledDataSource ?? {};
    const rowDict = rowData?.DictCascadingFiledDataSource ?? {};
    const rootItems = rootDict[fieldIdStr];
    const rowItems = rowDict[fieldIdStr];

    const structure = currentDataModel?.currentFormStructure ?? {};
    const dictCascadedParent = structure.DictCascadedIdParentField ?? {};
    const dictFieldUnit = structure.DictFieldIdUnitId ?? {};
    const parentFieldIdRaw = dictCascadedParent[fieldIdStr];
    const parentFieldIdStr =
      parentFieldIdRaw != null && parentFieldIdRaw !== '' ? String(parentFieldIdRaw) : '';

    const childUnitIdStr = String(unitId);

    if (parentFieldIdStr) {
      const parentUnitRaw = dictFieldUnit[parentFieldIdStr] ?? dictFieldUnit[String(Number(parentFieldIdStr))];
      const parentUnitIdStr = parentUnitRaw != null ? String(parentUnitRaw) : '';

      if (parentUnitIdStr && parentUnitIdStr === childUnitIdStr) {
        // Same unit: row first (Angular)
        if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
        if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
        return null;
      }
      // Parent in different unit (e.g. root DDL): root first — row data is often stale after root cascade API
      if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
      if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
      return null;
    }

    // No structure mapping: prefer row then root
    if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
    if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
    return null;
  };

  /** Resolve DB field name for a pivot-projection column (host or grandchild pivot leaf). */
  const getProjectionFieldDbName = useCallback(
    (col: any): string | null => {
      const binding = col?.binding;
      if (!binding) return null;
      const host = projectionModel?.HostColumns?.find((h) => h.Binding === binding);
      if (host) return host.Binding;
      for (const g of projectionModel?.ColumnGroups ?? []) {
        for (const leaf of g.Columns ?? []) {
          if (leaf.Binding === binding) return leaf.DataBaseFieldName;
        }
      }
      return String(binding);
    },
    [projectionModel]
  );

  const restoreProjectionColumnDataMap = useCallback(
    (gridSender: any, colIndex: number) => {
      const flex = gridSender?.control ?? gridSender;
      const col = flex?.columns?.[colIndex];
      if (!col) return;
      const fieldIdStr = String(col.name ?? '');
      if (!fieldIdStr) return;
      const dm = buildStandaloneDataMapFromFormStructure(dataModelRef.current, fieldIdStr);
      if (dm) {
        col.dataMap = dm;
        flex.invalidate?.();
      }
    },
    [buildStandaloneDataMapFromFormStructure]
  );

  /** Refresh cascading lookup buckets on the child row (Angular grandchild: childRow.DictCascadingFiledDataSource). */
  const refreshProjectionRowCascading = useCallback(
    async (childRow: any, triggerFieldId: string | number): Promise<any | null> => {
      if (!childRow || triggerFieldId == null || triggerFieldId === '') return null;
      const currentDataModel = dataModelRef.current;
      const rows = getGridRowsForThisUnit();
      const rowIndex = rows.indexOf(childRow);
      const rowsPatch = rowIndex >= 0 ? rows.map((r, i) => (i === rowIndex ? childRow : r)) : rows;
      const nextCurrentFormData = buildMasterDetailDataDtoForCascading(currentDataModel, unitIdStr, rowsPatch);
      try {
        const cascadingResp = await appTransactionService.GetChildOrGrandChildUnitFieldTriggerCascadingDataSource({
          AppChildDataDto: {
            ...childRow,
            CascadingUnitId: unitId,
            CascadingFieldId: Number(triggerFieldId),
          },
          MasterDetailDataDto: nextCurrentFormData,
        });
        if (!cascadingResp) return null;
        const mergedRow = {
          ...childRow,
          DictOneToOneFields: cascadingResp.DictOneToOneFields ?? childRow.DictOneToOneFields,
          DictCascadingFiledDataSource:
            cascadingResp.DictCascadingFiledDataSource ?? childRow.DictCascadingFiledDataSource,
          CascadingNeedToBeLockedFields:
            cascadingResp.CascadingNeedToBeLockedFields ?? childRow.CascadingNeedToBeLockedFields,
        };
        if (rowIndex >= 0) {
          const nextRows = rows.map((r, i) => (i === rowIndex ? mergedRow : r));
          commitOptimisticUnitRows(nextRows);
          deferDataModelChange({
            ...currentDataModel,
            currentFormData: {
              ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, nextRows),
              IsDirty: true,
            },
          });
        }
        return mergedRow;
      } catch {
        return null;
      }
    },
    [unitId, unitIdStr, commitOptimisticUnitRows, deferDataModelChange, getGridRowsForThisUnit]
  );

  const handleProjectionCellEditBeginning = useCallback(
    async (s: any, e: any) => {
      if (isGridReadOnly) return;
      if (e.row < 0 || e.col < 0) return;

      const col = s.columns?.[e.col];
      const wideItem = s.rows?.[e.row]?.dataItem;
      if (!col || !wideItem) return;

      const fieldId = String(col?.name ?? '');
      const dbFieldName = getProjectionFieldDbName(col);
      if (!fieldId || !dbFieldName) return;

      const rowIndex =
        typeof wideItem.__rowIndex === 'number' && wideItem.__rowIndex >= 0 ? wideItem.__rowIndex : e.row;
      const childRows = getGridRowsForThisUnit();
      let childRow = rowIndex >= 0 && rowIndex < childRows.length ? childRows[rowIndex] : null;
      if (!childRow) return;

      if (
        Array.isArray(childRow.CascadingNeedToBeLockedFields) &&
        childRow.CascadingNeedToBeLockedFields.indexOf(dbFieldName) >= 0
      ) {
        e.cancel = true;
        return;
      }

      projectionCascadingInitRef.current = wideItem[col.binding];

      const currentDataModel = dataModelRef.current;
      const metaFormForCascading =
        masterDetailFormDataRef.current ?? currentDataModel?.currentFormData;
      const usedCascadingFieldIds = metaFormForCascading?.IsUsedCascadingDataSourceFiedIds ?? [];
      if (!Array.isArray(usedCascadingFieldIds) || !usedCascadingFieldIds.map(String).includes(fieldId)) {
        return;
      }

      let lookupItems = resolveCascadingLookupItemsForCellEdit(currentDataModel, childRow, fieldId);

      if (!lookupItems || lookupItems.length === 0) {
        const structure = currentDataModel?.currentFormStructure ?? {};
        const parentFieldIdRaw = structure.DictCascadedIdParentField?.[fieldId];
        const parentFieldIdStr =
          parentFieldIdRaw != null && parentFieldIdRaw !== '' ? String(parentFieldIdRaw) : '';
        if (parentFieldIdStr) {
          const refreshed = await refreshProjectionRowCascading(childRow, parentFieldIdStr);
          if (refreshed) {
            childRow = refreshed;
            lookupItems = resolveCascadingLookupItemsForCellEdit(currentDataModel, childRow, fieldId);
          }
        }
      }

      if (lookupItems && lookupItems.length > 0) {
        col.dataMap = new DataMap(lookupItems, 'Id', 'Display');
      }
    },
    [getProjectionFieldDbName, getGridRowsForThisUnit, isGridReadOnly, refreshProjectionRowCascading]
  );

  const handleProjectionCellEditEnding = useCallback(
    (s: any, e: any) => {
      if (e.col < 0) return;
      restoreProjectionColumnDataMap(s, e.col);
    },
    [restoreProjectionColumnDataMap]
  );

  const handleProjectionCellEditEnded = useCallback(
    async (s: any, e: any) => {
      if (isGridReadOnly) return;
      if (e.row < 0 || e.col < 0) return;

      const col = s.columns?.[e.col];
      const wideItem = s.rows?.[e.row]?.dataItem;
      if (!col || !wideItem) return;

      const fieldId = String(col?.name ?? '');
      const binding = col?.binding;
      const rowIndex =
        typeof wideItem.__rowIndex === 'number' && wideItem.__rowIndex >= 0 ? wideItem.__rowIndex : e.row;
      const newValue = s.getCellData(e.row, e.col, false);
      const valueChanged = newValue !== projectionCascadingInitRef.current;

      const wideRows = getProjectionWideRowsFromGrid(s);
      if (binding && rowIndex >= 0 && rowIndex < wideRows.length) {
        wideRows[rowIndex] = { ...wideRows[rowIndex], [binding]: newValue };
      }

      try {
        handleProjectionWideRowsChange(wideRows);

        const metaFormForCascadeEnd =
          masterDetailFormDataRef.current ?? dataModelRef.current?.currentFormData;
        const changedNeedCascade = metaFormForCascadeEnd?.IsChangedNeedToCascadingFiedIds ?? [];
        if (fieldId && valueChanged && Array.isArray(changedNeedCascade)) {
          const changedSet = new Set(changedNeedCascade.map((x: any) => String(x)));
          if (changedSet.has(fieldId)) {
            const childRows = getGridRowsForThisUnit();
            const childRow = rowIndex >= 0 && rowIndex < childRows.length ? childRows[rowIndex] : null;
            if (childRow) {
              await refreshProjectionRowCascading(childRow, fieldId);
            }
          }
        }
      } finally {
        restoreProjectionColumnDataMap(s, e.col);
      }
    },
    [
      getGridRowsForThisUnit,
      handleProjectionWideRowsChange,
      isGridReadOnly,
      refreshProjectionRowCascading,
      restoreProjectionColumnDataMap,
    ]
  );

  const handleCellEditBeginning = (s: any, e: any) => {
    if (isGridReadOnly) return;
    if (e.row < 0 || e.col < 0) return;

    const col = s.columns?.[e.col];
    const rowData = s.rows?.[e.row]?.dataItem?._originalData;
    if (!col || !rowData) return;

    const fieldId = String(col?.name ?? '');
    const fieldName = col?.binding;
    if (!fieldId || !fieldName) return;

    // Angular parity:
    // If the row indicates this field must be locked, cancel edit immediately.
    if (
      Array.isArray(rowData?.CascadingNeedToBeLockedFields) &&
      rowData.CascadingNeedToBeLockedFields.indexOf(fieldName) >= 0
    ) {
      e.cancel = true;
      return;
    }

    // Angular parity: PK fields are read-only in Edit mode.
    const editedField = fields.find((f: any) => String(f?.Id) === fieldId || String(f?.DataBaseFieldName) === fieldName);
    if (isEditMode && normalizeBool(editedField?.IsPrimaryKey)) {
      e.cancel = true;
      return;
    }

    cascadingInitValueRef.current = rowData?.DictOneToOneFields?.[fieldName];

    const currentDataModel = dataModelRef.current;
    const metaFormForCascading =
      masterDetailFormDataRef.current ?? currentDataModel?.currentFormData;
    const usedCascadingFieldIds = metaFormForCascading?.IsUsedCascadingDataSourceFiedIds ?? [];
    if (!Array.isArray(usedCascadingFieldIds)) return;
    const usedSet = new Set(usedCascadingFieldIds.map((x: any) => String(x)));
    if (!usedSet.has(fieldId)) return;

    const lookupItems = resolveCascadingLookupItemsForCellEdit(currentDataModel, rowData, fieldId);

    if (lookupItems) {
      col.dataMap = new DataMap(lookupItems, 'Id', 'Display');
    }
  };

  /** Angular: restore standalone column dataMap when leaving cell (commit or cancel). */
  const handleCellEditEnding = (s: any, e: any) => {
    if (e.col < 0) return;
    restoreStandaloneColumnDataMap(s, e.col);
  };

  // Handle cell edit — uses refs to avoid stale closure when Wijmo caches the handler
  const handleCellEditEnded = async (s: any, e: any) => {
    if (isGridReadOnly) return;
    if (e.row < 0 || e.col < 0) return;

    const col = s.columns[e.col];
    const fieldName = col?.binding;
    if (!fieldName) return;
    const fieldId = String(col?.name ?? '');
    /** Backing array index: flatRow has `_rowIndex`; do not use `collectionView` fallback (stale closure). */
    const rowDataItem = s.rows?.[e.row]?.dataItem;
    const rowIndex =
      typeof rowDataItem?._rowIndex === 'number' && rowDataItem._rowIndex >= 0
        ? rowDataItem._rowIndex
        : e.row;
    const newValue = s.getCellData(e.row, e.col, false);

    const editedField = fields.find(
      (f: any) => String(f?.Id) === fieldId || String(f?.DataBaseFieldName) === fieldName
    );
    let committedValue: any = newValue;
    if (editedField?.ControlType === emAppControlTypeEnum?.Numeric) {
      const raw = newValue;
      const isEmpty =
        raw === '' ||
        raw === null ||
        raw === undefined ||
        (typeof raw === 'string' && !String(raw).trim());
      if (isEmpty) {
        committedValue = 0;
      } else if (typeof raw === 'string') {
        const t = raw.trim();
        if (t === '-' || t === '.' || t === '-.') {
          committedValue = 0;
        } else {
          const n = Number(t);
          committedValue = Number.isFinite(n) ? n : 0;
        }
      } else if (typeof raw === 'number') {
        committedValue = Number.isFinite(raw) ? raw : 0;
      }
    }

    try {
    const currentDataModel = dataModelRef.current;
    const unitIdStr = unitId.toString();
    const gridDataArray = getGridRowsForThisUnit();

    if (gridDataArray && gridDataArray[rowIndex]) {
      const commitSeq = ++cellEditCommitSeqRef.current;
      const rowData = gridDataArray[rowIndex];
      const nextOneToOne = {
        ...(rowData?.DictOneToOneFields ?? {}),
        [fieldName]: committedValue
      };
      const nextRowData = {
        ...rowData,
        DictOneToOneFields: nextOneToOne,
        IsDirty: true
      };
      const nextGridDataArray = gridDataArray.map((r: any, idx: number) => (idx === rowIndex ? nextRowData : r));
      let finalRowData = nextRowData;

      const valueChanged = committedValue !== cascadingInitValueRef.current;

      // Angular parity: if this parent field controls one-to-many cascading retrieve, clear target child fields.
      const dictDataRetrieve = currentDataModel?.DictRetrieveDataOneToManyCascading_TransUnitId_ParentFieldNameAndChildFieldNames;
      const cascadingChildFields = dictDataRetrieve?.[unitIdStr]?.[fieldName];
      if (Array.isArray(cascadingChildFields) && cascadingChildFields.length > 0 && valueChanged) {
        const nextOneToOneWithClearedChildren = { ...finalRowData.DictOneToOneFields };
        cascadingChildFields.forEach((childFieldName: string) => {
          nextOneToOneWithClearedChildren[childFieldName] = null;
        });
        finalRowData = {
          ...finalRowData,
          DictOneToOneFields: nextOneToOneWithClearedChildren,
        };
      }

      // Angular parity: if edited field is store-proc/query master field, apply dependent field values from selected lookup item.
      const unitMasterFields = currentDataModel?.currentFormStructure?.DictUnitIdStoreProcOrQueryDataSetMasterFiled?.[unitIdStr];
      if (Array.isArray(unitMasterFields) && unitMasterFields.includes(fieldName) && valueChanged) {
        const rowCascadingLookup = finalRowData?.DictCascadingFiledDataSource?.[fieldId];
        if (Array.isArray(rowCascadingLookup)) {
          const selectedLookup = rowCascadingLookup.find((item: any) => String(item?.Id) === String(committedValue));
          if (selectedLookup?.DictDependentFieldValue) {
            finalRowData = {
              ...finalRowData,
              DictOneToOneFields: {
                ...finalRowData.DictOneToOneFields,
                ...selectedLookup.DictDependentFieldValue,
              },
            };
          }
        }
      }

      // Angular parity: child/grandchild cascading data source refresh after parent DDL changes.
      const metaFormForCascadeEnd =
        masterDetailFormDataRef.current ?? currentDataModel?.currentFormData;
      const changedNeedCascade = metaFormForCascadeEnd?.IsChangedNeedToCascadingFiedIds ?? [];
      if (fieldId && Array.isArray(changedNeedCascade)) {
        const changedSet = new Set(changedNeedCascade.map((x: any) => String(x)));
        if (changedSet.has(fieldId) && valueChanged) {
        try {
          const rowsForCascadingPatch = nextGridDataArray.map((r: any, idx: number) =>
            idx === rowIndex ? finalRowData : r
          );
          const nextCurrentFormData = buildMasterDetailDataDtoForCascading(
            currentDataModel,
            unitIdStr,
            rowsForCascadingPatch
          );
          const cascadingPayload = {
            AppChildDataDto: {
              ...finalRowData,
              CascadingUnitId: unitId,
              CascadingFieldId: fieldId,
            },
            MasterDetailDataDto: nextCurrentFormData,
          };
          const cascadingResp = await appTransactionService.GetChildOrGrandChildUnitFieldTriggerCascadingDataSource(cascadingPayload);
          if (cascadingResp) {
            finalRowData = {
              ...finalRowData,
              DictOneToOneFields: cascadingResp.DictOneToOneFields ?? finalRowData.DictOneToOneFields,
              DictCascadingFiledDataSource: cascadingResp.DictCascadingFiledDataSource ?? finalRowData.DictCascadingFiledDataSource,
              CascadingNeedToBeLockedFields: cascadingResp.CascadingNeedToBeLockedFields ?? finalRowData.CascadingNeedToBeLockedFields,
            };
          }
        } catch {
          // Keep user-edited value when cascading API fails.
        }
        }
      }

      if (commitSeq !== cellEditCommitSeqRef.current) {
        return;
      }

      const rowsAfterCellEdit = nextGridDataArray.map((r: any, idx: number) =>
        idx === rowIndex ? finalRowData : r
      );
      if (dictOneToManyHostRow) {
        tryMutateInPlace(() => {
          dictOneToManyHostRow.IsDirty = true;
          dictOneToManyHostRow.DictOneToManyFields = {
            ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
            [unitIdStr]: rowsAfterCellEdit,
          };
        });
        queueNestedGridRecompute();
      }

      commitOptimisticUnitRows(rowsAfterCellEdit);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, rowsAfterCellEdit),
          IsDirty: true,
        },
      });
    }
    } finally {
      restoreStandaloneColumnDataMap(s, e.col);
    }
  };

  // Persist edits made in the generic pivot-edit grid. The child hands back the rebuilt
  // flat AppChildDataDto rows (incl. hidden PK / link fields), mirroring Angular convertPivotToFlat.
  const handlePivotRowsChange = useCallback(
    (flatRows: any[]) => {
      const currentDataModel = dataModelRef.current;
      const unitIdStrLocal = String(unitId);
      if (dictOneToManyHostRow) {
        tryMutateInPlace(() => {
          dictOneToManyHostRow.IsDirty = true;
          dictOneToManyHostRow.DictOneToManyFields = {
            ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
            [unitIdStrLocal]: flatRows,
          };
        });
        queueNestedGridRecompute();
      }
      commitOptimisticUnitRows(flatRows);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStrLocal, flatRows),
          IsDirty: true,
        },
      });
    },
    [
      commitOptimisticUnitRows,
      deferDataModelChange,
      dictOneToManyHostRow,
      queueNestedGridRecompute,
      unitId,
    ]
  );

  // Handle add row - always add at the bottom
  const [isGeneratingMatrix, setIsGeneratingMatrix] = useState(false);

  // Blank clone row template for this unit (used as the matrix generation seed; mirrors handleAddRow).
  const buildBlankCloneRow = useCallback(() => {
    const newRow: any = { DictOneToOneFields: {}, DictOneToManyFields: {} };
    grandChildUnitList.forEach((childUnit: any) => {
      const childUnitId = childUnit?.Id ?? childUnit?.unitId;
      if (childUnitId != null) {
        newRow.DictOneToManyFields[String(childUnitId)] = [];
      }
    });
    (unitExDto?.AppTransactionFieldList ?? []).forEach((field: any) => {
      if (field?.DataBaseFieldName) {
        newRow.DictOneToOneFields[field.DataBaseFieldName] = null;
      }
    });
    return newRow;
  }, [grandChildUnitList, unitExDto]);

  // Matrix grid: regenerate this unit's rows from the foreign-key field values (Angular generateMatrix).
  const handleGenerateMatrix = useCallback(async () => {
    const unitIdStr = unitId.toString();
    const currentDataModel = dataModelRef.current;
    const formData = currentDataModel?.currentFormData;
    if (!formData) return;
    setIsGeneratingMatrix(true);
    store.dispatch(setIsBusy());
    try {
      // Deep clone to build the POST payload without mutating the live data model.
      const payload = JSON.parse(JSON.stringify(formData));
      // EditCloneDictOneToManyFields is the per-unit blank-row clone template the server uses to
      // seed new combinations. Angular fallback: EditCloneDictOneToManyFields || copy(DictOneToManyFields).
      const editClone = payload.EditCloneDictOneToManyFields
        ? { ...payload.EditCloneDictOneToManyFields }
        : (payload.DictOneToManyFields ? JSON.parse(JSON.stringify(payload.DictOneToManyFields)) : {});
      if (!Array.isArray(editClone[unitIdStr]) || editClone[unitIdStr].length === 0) {
        editClone[unitIdStr] = [buildBlankCloneRow()];
      }
      payload.EditCloneDictOneToManyFields = editClone;

      const result = await appTransactionService.generateMatrix(payload);
      if (result) {
        onDataModelChangeRef.current({
          ...currentDataModel,
          currentFormData: { ...result, IsDirty: true },
        });
      }
    } catch (e) {
      appHelper.debugLog('handleGenerateMatrix failed', e);
      store.dispatch(
        addErrorMessage({ message: 'Failed to generate matrix grid data.', type: MessageType.Error }),
      );
    } finally {
      store.dispatch(setIsNotBusy());
      setIsGeneratingMatrix(false);
    }
  }, [unitId, buildBlankCloneRow]);

  const handleAddRow = () => {
    const unitIdStr = unitId.toString();
    const currentDataModel = dataModelRef.current;
    const gridDataArray = getGridRowsForThisUnit();
    
    // Create new blank row
    const newRow: any = {
      DictOneToOneFields: {},
      DictOneToManyFields: {},
      IsDirty: true,
      IsNew: true,
    };

    // Child unit collections must exist so nested row-detail / save paths match Angular (no null refs).
    grandChildUnitList.forEach((childUnit: any) => {
      const childUnitId = childUnit?.Id ?? childUnit?.unitId;
      if (childUnitId != null) {
        newRow.DictOneToManyFields[String(childUnitId)] = [];
      }
    });

    // Initialize EVERY DB field (not just the visible ones) to null; only use DefaultValue if it is
    // a real non-empty value. Hidden fields — especially a hidden primary key like RowId — must still
    // get a key, otherwise the backend save (ClassifyGrandChildUnitDataSet → dictOneToOneFields[pk])
    // throws KeyNotFoundException: "The given key 'RowId' was not present in the dictionary".
    // PK fields stay null (not "") so the backend treats this row as a new insert. Mirrors buildBlankCloneRow.
    (unitExDto?.AppTransactionFieldList ?? []).forEach((field: any) => {
      if (!field?.DataBaseFieldName) return;
      const hasDefault = field.DefaultValue !== undefined && field.DefaultValue !== null && field.DefaultValue !== '';
      newRow.DictOneToOneFields[field.DataBaseFieldName] = hasDefault ? field.DefaultValue : null;
    });
    
    // Always add new row at the bottom (end of array) - use push to ensure it's at the end
    const updatedArray = [...gridDataArray];
    updatedArray.push(newRow);

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.IsDirty = true;
        dictOneToManyHostRow.DictOneToManyFields = {
          ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
          [unitIdStr]: updatedArray,
        };
      });
      queueNestedGridRecompute();
    }

    commitOptimisticUnitRows(updatedArray);
    // Update data model
    deferDataModelChange({
      ...currentDataModel,
      currentFormData: {
        ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, updatedArray),
        IsDirty: true,
      },
    });

    reconcileAvailableSourceSelectionFlags(updatedArray);
    
    // Scroll to the newly added row after a short delay to allow grid to update
    const timerId = window.setTimeout(() => {
      try {
        if (!isMountedRef.current) return;
        const flex = flexGridRef.current?.control ?? flexGridRef.current;
        if (!flex || (flex as any).isDisposed) return;

        // Do not call collectionView.refresh() here — useLayoutEffect sync already refreshed;
        // an extra refresh in row-detail nested grids races Wijmo React updateProps (null.selectionMode).

        // Select the last row (newly added)
        const lastIndex = collectionView.items.length - 1;
        if (lastIndex >= 0) {
          collectionView.moveCurrentToPosition(lastIndex);
          // Scroll to the selected row
          if (flex.rows && flex.rows[lastIndex]) {
            flex.scrollIntoView(lastIndex, 0);
          }
        }
      } catch {
        // ignore disposed/unmounted edge cases in nested row-detail grids
      }
    }, 100);
    timerRefs.current.push(timerId);
  };

  // Handle delete row (single row by index)
  const handleDeleteRow = (rowIndex: number) => {
    const unitIdStr = unitId.toString();
    const currentDataModel = dataModelRef.current;
    const gridDataArray = getGridRowsForThisUnit();
    
    if (rowIndex < 0 || rowIndex >= gridDataArray.length) return;
    
    const rowData = gridDataArray[rowIndex];
    const updatedArray = gridDataArray.filter((_: any, index: number) => index !== rowIndex);
    
    // If row has an Id, mark it for deletion
    if (rowData.Id) {
      // Add to deleted items if needed
      // This would be handled in the save logic
    }

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.IsDirty = true;
        dictOneToManyHostRow.DictOneToManyFields = {
          ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
          [unitIdStr]: updatedArray,
        };
      });
      queueNestedGridRecompute();
    }
    
    commitOptimisticUnitRows(updatedArray);
    // Update data model
    deferDataModelChange({
      ...currentDataModel,
      currentFormData: {
        ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, updatedArray),
        IsDirty: true,
      },
    });

    reconcileAvailableSourceSelectionFlags(updatedArray);
  };

  // Handle delete selected rows (all highlighted rows)
  const handleDeleteSelectedRows = () => {
    if (!flexGridRef.current) return;

    const flexGrid = flexGridRef.current?.control ?? flexGridRef.current;
    if (!flexGrid) return;

    const unitIdStr = unitId.toString();
    const currentDataModel = dataModelRef.current;
    const gridDataArray = getGridRowsForThisUnit();

    // Get selected rows - try multiple methods for MultiRange mode
    const selectedDataItems: any[] = [];

    // Method 1: Use selectedRows property if available
    if (flexGrid.selectedRows && flexGrid.selectedRows.length > 0) {
      for (let i = 0; i < flexGrid.selectedRows.length; i++) {
        const row = flexGrid.selectedRows[i];
        if (row && row.dataItem) {
          selectedDataItems.push(row.dataItem);
        }
      }
    }

    // Method 2: Iterate through all rows and check isSelected
    if (selectedDataItems.length === 0) {
      for (let i = 0; i < flexGrid.rows.length; i++) {
        const row = flexGrid.rows[i];
        if (row && row.isSelected && row.dataItem) {
          selectedDataItems.push(row.dataItem);
        }
      }
    }

    // Method 3: Active cell / selection row (toolbar click often clears multi-range selection)
    if (selectedDataItems.length === 0) {
      const rh = flexGrid.selection?.row;
      if (typeof rh === 'number' && rh >= 0 && rh < flexGrid.rows.length) {
        const r = flexGrid.rows[rh];
        if (r?.dataItem && !(r instanceof GroupRow)) {
          selectedDataItems.push(r.dataItem);
        }
      }
    }

    if (selectedDataItems.length === 0) {
      return;
    }

    // Resolve backing-array indices (nested grids may lose _rowIndex on some Wijmo paths)
    const selectedIndices: number[] = [];
    selectedDataItems.forEach((dataItem: any) => {
      let originalIndex = dataItem._rowIndex;
      if (originalIndex === undefined || originalIndex === null) {
        const wrIdx = flexGrid.rows.findIndex((rr: any) => rr?.dataItem === dataItem);
        if (wrIdx >= 0) {
          const wr = flexGrid.rows[wrIdx];
          originalIndex = typeof wr.dataIndex === 'number' ? wr.dataIndex : wrIdx;
        }
      }
      if (originalIndex === undefined || originalIndex === null) {
        const orig = dataItem._originalData ?? dataItem;
        const idx = gridDataArray.findIndex((row: any) => row === orig);
        if (idx >= 0) originalIndex = idx;
      }
      if (originalIndex !== undefined && originalIndex !== null) {
        selectedIndices.push(originalIndex);
      }
    });

    if (selectedIndices.length === 0) {
      return;
    }
    
    // Sort indices in descending order to delete from end to beginning (avoid index shifting issues)
    const sortedIndices = [...selectedIndices].sort((a, b) => b - a);
    
    // Track rows with Ids for deletion marking
    const rowsToMarkForDeletion: any[] = [];
    
    // Remove selected rows
    const updatedArray = gridDataArray.filter((row: any, index: number) => {
      if (sortedIndices.includes(index)) {
        // If row has an Id, mark it for deletion
        if (row.Id) {
          rowsToMarkForDeletion.push(row);
        }
        return false; // Remove this row
      }
      return true; // Keep this row
    });

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.IsDirty = true;
        dictOneToManyHostRow.DictOneToManyFields = {
          ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
          [unitIdStr]: updatedArray,
        };
      });
      queueNestedGridRecompute();
    }
    
    commitOptimisticUnitRows(updatedArray);
    // Update data model
    deferDataModelChange({
      ...currentDataModel,
      currentFormData: {
        ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, updatedArray),
        IsDirty: true,
      },
    });

    reconcileAvailableSourceSelectionFlags(updatedArray);
    
    // Clear selection after deletion
    const timerId = window.setTimeout(() => {
      // In nested row-detail scenarios the grid may have been disposed/unmounted
      // after the data model update, so keep this selection reset best-effort.
      try {
        if (!isMountedRef.current) return;
        if (flexGrid && !(flexGrid as any).isDisposed && typeof (flexGrid as any).select === 'function') {
          flexGrid.select(-1, -1);
        }
      } catch {
        // ignore (avoid "Cannot read properties of null (reading 'select')" after dispose)
      }
    }, 100);
    timerRefs.current.push(timerId);
  };

  /** Match Angular SetupAvailableSourceUnitSelectedRows: sync `IsSelected` on source pool rows from subscribe keys. */
  const reconcileAvailableSourceSelectionFlags = useCallback(
    (nextSelectedRows: any[]) => {
      if (!isAvailableSelectLayout || !availableSelectConfigOk || !sourceUnitIdStr || !mappingSubscribeDb || !mappingSourceDb) {
        return;
      }
      const currentDataModel = dataModelRef.current;
      const host = dictOneToManyHostRow ?? currentDataModel?.currentFormData;
      const srcRows = host?.DictOneToManyFields?.[sourceUnitIdStr];
      if (!Array.isArray(srcRows)) return;
      const selectedKeys = new Set(
        nextSelectedRows
          .map((r: any) => readTransactionRowField(r, mappingSubscribeDb))
          .filter((v: any) => v != null && v !== '')
          .map((v: any) => String(v))
      );
      tryMutateInPlace(() => {
        srcRows.forEach((r: any) => {
          const k = readTransactionRowField(r, mappingSourceDb);
          const ks = k != null && k !== '' ? String(k) : '';
          r.IsSelected = ks !== '' && selectedKeys.has(ks);
        });
      });
      setAvailableDataBump((n) => n + 1);
    },
    [
      isAvailableSelectLayout,
      availableSelectConfigOk,
      sourceUnitIdStr,
      mappingSubscribeDb,
      mappingSourceDb,
      dictOneToManyHostRow,
    ]
  );

  const handleAddFromAvailable = useCallback(() => {
    if (isGridReadOnly || !availableSelectConfigOk || !mappingSubscribeDb || !mappingSourceDb) return;
    const flex = availableFlexGridRef.current?.control ?? availableFlexGridRef.current;
    if (!flex) return;

    const dataItems = collectSelectedDataItemsFromFlexGrid(flex);
    const picked: any[] = [];
    for (const di of dataItems) {
      const orig = di?._originalData ?? di;
      if (orig) picked.push(orig);
    }
    if (picked.length === 0) return;

    const currentDataModel = dataModelRef.current;
    const unitIdStrLocal = String(unitId);
    const gridDataArray = getGridRowsForThisUnit();
    const existingKeys = new Set(
      gridDataArray
        .map((r: any) => readTransactionRowField(r, mappingSubscribeDb))
        .filter((v: any) => v != null && v !== '')
        .map((v: any) => String(v))
    );

    const toAdd: any[] = [];
    for (const srcRow of picked) {
      const srcKey = readTransactionRowField(srcRow, mappingSourceDb);
      if (srcKey == null || srcKey === '') continue;
      const ks = String(srcKey);
      if (existingKeys.has(ks)) continue;

      const newRow: any = {
        DictOneToOneFields: {},
        DictOneToManyFields: {},
        IsDirty: true,
        IsNew: true,
      };
      grandChildUnitList.forEach((childUnit: any) => {
        const childUnitId = childUnit?.Id ?? childUnit?.unitId;
        if (childUnitId != null) {
          newRow.DictOneToManyFields[String(childUnitId)] = [];
        }
      });
      fields.forEach((field: any) => {
        const hasDefault = field.DefaultValue !== undefined && field.DefaultValue !== null && field.DefaultValue !== '';
        newRow.DictOneToOneFields[field.DataBaseFieldName] = hasDefault ? field.DefaultValue : null;
      });
      newRow.DictOneToOneFields[mappingSubscribeDb] = srcKey;
      toAdd.push(newRow);
      existingKeys.add(ks);
    }

    if (toAdd.length === 0) return;

    const updatedArray = [...gridDataArray, ...toAdd];

    if (dictOneToManyHostRow) {
      tryMutateInPlace(() => {
        dictOneToManyHostRow.IsDirty = true;
        dictOneToManyHostRow.DictOneToManyFields = {
          ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
          [unitIdStrLocal]: updatedArray,
        };
      });
      queueNestedGridRecompute();
    }

    commitOptimisticUnitRows(updatedArray);
    deferDataModelChange({
      ...currentDataModel,
      currentFormData: {
        ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStrLocal, updatedArray),
        IsDirty: true,
      },
    });

    reconcileAvailableSourceSelectionFlags(updatedArray);

    const tId = window.setTimeout(() => {
      try {
        if (!isMountedRef.current) return;
        const g = flexGridRef.current?.control ?? flexGridRef.current;
        if (!g || (g as any).isDisposed) return;
        const lastIndex = collectionView.items.length - 1;
        if (lastIndex >= 0) {
          g.select(lastIndex, 0);
          g.scrollIntoView(lastIndex, 0);
        }
      } catch {
        // ignore
      }
    }, 80);
    timerRefs.current.push(tId);
  }, [
    isGridReadOnly,
    availableSelectConfigOk,
    mappingSubscribeDb,
    mappingSourceDb,
    unitId,
    getGridRowsForThisUnit,
    dictOneToManyHostRow,
    grandChildUnitList,
    fields,
    commitOptimisticUnitRows,
    deferDataModelChange,
    reconcileAvailableSourceSelectionFlags,
    queueNestedGridRecompute,
  ]);

  /** Angular MultipleSelectBox: checkbox tiles; toggling adds/removes subscribe rows by mapping key. */
  const toggleMultipleSelectSourceRow = useCallback(
    (srcRow: any, nextChecked: boolean) => {
      if (isGridReadOnly || !availableSelectConfigOk || !mappingSubscribeDb || !mappingSourceDb) return;
      const srcKey = readTransactionRowField(srcRow, mappingSourceDb);
      if (srcKey == null || srcKey === '') return;
      const ks = String(srcKey);

      const currentDataModel = dataModelRef.current;
      const unitIdStrLocal = String(unitId);
      let gridDataArray = [...getGridRowsForThisUnit()];

      if (nextChecked) {
        const exists =         gridDataArray.some((r: any) => String(readTransactionRowField(r, mappingSubscribeDb) ?? '') === ks);
        if (exists) return;

        const newRow: any = {
          DictOneToOneFields: {},
          DictOneToManyFields: {},
          IsDirty: true,
          IsNew: true,
        };
        grandChildUnitList.forEach((childUnit: any) => {
          const childUnitId = childUnit?.Id ?? childUnit?.unitId;
          if (childUnitId != null) {
            newRow.DictOneToManyFields[String(childUnitId)] = [];
          }
        });
        fields.forEach((field: any) => {
          const hasDefault =
            field.DefaultValue !== undefined && field.DefaultValue !== null && field.DefaultValue !== '';
          newRow.DictOneToOneFields[field.DataBaseFieldName] = hasDefault ? field.DefaultValue : null;
        });
        newRow.DictOneToOneFields[mappingSubscribeDb] = srcKey;
        gridDataArray = [...gridDataArray, newRow];
      } else {
        gridDataArray = gridDataArray.filter(
          (r: any) => String(readTransactionRowField(r, mappingSubscribeDb) ?? '') !== ks
        );
      }

      if (dictOneToManyHostRow) {
        tryMutateInPlace(() => {
          dictOneToManyHostRow.IsDirty = true;
          dictOneToManyHostRow.DictOneToManyFields = {
            ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
            [unitIdStrLocal]: gridDataArray,
          };
        });
        queueNestedGridRecompute();
      }

      commitOptimisticUnitRows(gridDataArray);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStrLocal, gridDataArray),
          IsDirty: true,
        },
      });
      reconcileAvailableSourceSelectionFlags(gridDataArray);
    },
    [
      isGridReadOnly,
      availableSelectConfigOk,
      mappingSubscribeDb,
      mappingSourceDb,
      unitId,
      getGridRowsForThisUnit,
      dictOneToManyHostRow,
      grandChildUnitList,
      fields,
      commitOptimisticUnitRows,
      deferDataModelChange,
      reconcileAvailableSourceSelectionFlags,
      queueNestedGridRecompute,
      nextFormDataForUnitRowUpdate,
    ]
  );

  const applyGrandChildRowUpdate = useCallback(
    (updatedRow: any, parentRowIndex: number, rowItemRef: any) => {
      if (!updatedRow) return;
      const currentDataModel = dataModelRef.current;
      const unitIdStr = unitId.toString();
      const host = dictOneToManyHostRow ?? currentDataModel.currentFormData;
      const gridDataArray = getGridRowsForThisUnit();
      if (!Array.isArray(gridDataArray) || parentRowIndex < 0 || parentRowIndex >= gridDataArray.length) return;

      // List Edit parity: mutate the existing row object so FlexGrid row/detail identity stays stable
      // (spreading into a new object replaces the row ref and Wijmo collapses row detail).
      const targetRow = gridDataArray[parentRowIndex] ?? rowItemRef;
      if (targetRow == null) return;
      let rowForPatch = targetRow;
      try {
        Object.assign(targetRow, updatedRow, { IsDirty: true });
      } catch {
        rowForPatch = {
          ...(typeof targetRow === 'object' && targetRow !== null ? targetRow : {}),
          ...updatedRow,
          IsDirty: true,
        };
      }
      const nextGridDataArray = gridDataArray.map((r: any, idx: number) =>
        idx === parentRowIndex ? rowForPatch : r
      );
      const nextDictDoc = {
        ...(host?.DictDocumentIdFileCode ?? currentDataModel.currentFormData?.DictDocumentIdFileCode ?? {}),
        ...(updatedRow?.DictDocumentIdFileCode ?? {}),
      };

      if (dictOneToManyHostRow) {
        tryMutateInPlace(() => {
          dictOneToManyHostRow.IsDirty = true;
          dictOneToManyHostRow.DictDocumentIdFileCode = {
            ...(dictOneToManyHostRow.DictDocumentIdFileCode ?? {}),
            ...nextDictDoc,
          };
          dictOneToManyHostRow.DictOneToManyFields = {
            ...(dictOneToManyHostRow.DictOneToManyFields ?? {}),
            [unitIdStr]: nextGridDataArray,
          };
        });
        queueNestedGridRecompute();
      }

      commitOptimisticUnitRows(nextGridDataArray);
      deferDataModelChange({
        ...currentDataModel,
        currentFormData: {
          ...nextFormDataForUnitRowUpdate(currentDataModel, unitIdStr, nextGridDataArray, {
            DictDocumentIdFileCode: nextDictDoc,
          }),
          IsDirty: true,
        },
      });

      // Re-expand after defer/layout (same delay band as scroll/selection in nested grids; avoids null.selectionMode races).
      if ((grandChildUnitsForDetailRef.current?.length ?? 0) > 0 && parentRowIndex >= 0) {
        const reopenId = window.setTimeout(() => {
          try {
            if (!isMountedRef.current) return;
            const dp = flexGridDetailRef.current?.control ?? flexGridDetailRef.current;
            if (!dp || (dp as any).isDisposed) return;
            dp.showDetail?.(parentRowIndex, true);
          } catch {
            // ignore
          }
        }, 100);
        timerRefs.current.push(reopenId);
      }
    },
    [
      commitOptimisticUnitRows,
      deferDataModelChange,
      dictOneToManyHostRow,
      getGridRowsForThisUnit,
      queueNestedGridRecompute,
      unitId,
    ]
  );

  const applyGrandChildRowUpdateRef = useRef(applyGrandChildRowUpdate);
  applyGrandChildRowUpdateRef.current = applyGrandChildRowUpdate;

  const pickChildRowFromNestedUpdate = useCallback(
    (updatedNested: any, parentUnitId: number | string | null | undefined, parentRowIndex: number, fallbackRow: any) => {
      const updatedForm = updatedNested?.currentFormData;
      if (!updatedForm || parentUnitId == null) return fallbackRow;
      const dict = updatedForm?.DictOneToManyFields;
      const key = String(parentUnitId);
      const list = dict?.[key];
      if (Array.isArray(list) && parentRowIndex >= 0 && parentRowIndex < list.length) {
        return list[parentRowIndex] ?? fallbackRow;
      }
      return fallbackRow;
    },
    []
  );

  const grandChildDetailTemplate = useCallback(
    (ctx: any) => {
      const rowItem = ctx?.item?._originalData ?? ctx?.item ?? ctx?.row?.dataItem?._originalData ?? ctx?.row?.dataItem;
      if (!rowItem) return null;
      const parentRowIndex = Number(ctx?.row?.dataIndex ?? ctx?.row?.index ?? -1);
      const list = grandChildUnitsForDetailRef.current;
      if (!list || list.length === 0) {
        // No placeholder text: keep detail area blank if unit definitions are missing.
        return (
          <div style={{ width: 'calc(100% - 100px)', height: '300px', overflow: 'hidden' }} />
        );
      }

      const nestedDataModel = {
        ...dataModelRef.current,
        currentFormData: {
          ...rowItem,
          DictDocumentIdFileCode: dataModelRef.current?.currentFormData?.DictDocumentIdFileCode ?? {},
        },
      };

      return (
        <div style={{ width: 'calc(100% - 100px)', height: '300px', overflow: 'hidden' }}>
          {list.map((gc: any) => {
            const gcUnitId = gc.Id ?? gc.unitId;
            if (gcUnitId == null) return null;
            return (
              <div key={String(gcUnitId)} className="w-full h-full">
                <Provider store={store}>
                  <DataGridLayout
                    unitExDto={gc}
                    unitId={gcUnitId}
                    dataModel={nestedDataModel}
                    dictOneToManyHostRow={rowItem}
                    masterDetailFormData={dataModelRef.current?.currentFormData}
                    parentOneToManyUnitId={selfUnitIdRef.current}
                    parentHostRowIndex={parentRowIndex}
                    controllerModel={controllerModelRef.current}
                    transactionExDto={transactionExDtoRef.current}
                    onDataModelChange={(updatedNested: any) => {
                      // Grandchild grids may emit root master-detail `currentFormData` (for cascading).
                      // We must apply only the updated *child row* back to the parent grid row.
                      const updatedChildRow = pickChildRowFromNestedUpdate(
                        updatedNested,
                        selfUnitIdRef.current,
                        parentRowIndex,
                        rowItem
                      );
                      if (!updatedChildRow) return;
                      applyGrandChildRowUpdateRef.current(updatedChildRow, parentRowIndex, rowItem);
                    }}
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
    // Stable like FormListEdit grandChildDetailTemplate — do not depend on controllerModel / unit list / DTO
    // so Wijmo FlexGridDetail does not treat `template` as changed and collapse on nested updates.
    []
  );

  // Format cell value based on control type (reserved for custom cell render)
  const _formatCellValue = (value: any, field: any): string => {
    if (value === null || value === undefined) {
      return '';
    }
    
    switch (field.ControlType) {
      case emAppControlTypeEnum?.CheckBox:
        return value ? 'Yes' : 'No';
      case emAppControlTypeEnum?.Date:
        if (value instanceof Date) {
          return value.toLocaleDateString('en-US');
        }
        return value;
      case emAppControlTypeEnum?.DateTimeDetail:
        if (value instanceof Date) {
          return value.toLocaleString('en-US');
        }
        return value;
      default:
        return value.toString();
    }
  };

  // Delete row cell template
  const deleteRowCellTemplate = (cell: any) => {
    const rowIndex = cell.row.index;
    return (
      <div className="flex items-center justify-center h-full">
        <button
          onClick={(e) => {
            e.stopPropagation();
            handleDeleteRow(rowIndex);
          }}
          className={`p-1 ${theme.button_default} rounded hover:bg-red-100 text-red-600`}
          title="Delete Row"
        >
          <i className="fa fa-trash text-xs"></i>
        </button>
      </div>
    );
  };

  const showAvailableSelectPairSplit = isAvailableSelectPair && availableSelectConfigOk;
  const showMultipleSelectBoxUi = isMultipleSelectBox && availableSelectConfigOk;
  const showAvailableSelectRuntime = showAvailableSelectPairSplit || showMultipleSelectBoxUi;
  const showAvailableSelectConfigWarning = isAvailableSelectLayout && !availableSelectConfigOk;

  // In design mode, show a placeholder for the grid
  if (controllerModel?.isDesignMode) {
    return (
      <div className="w-full border-2 border-dashed border-gray-300 rounded p-4 bg-gray-50">
        <div className="text-center text-gray-500">
          <i className="fa fa-table text-2xl mb-2"></i>
          <div className="text-sm font-semibold">{unitExDto?.UnitDisplayName || 'Data Grid'}</div>
          <div className="text-xs mt-1">
            {fields.length > 0 ? `${fields.length} columns` : 'No columns defined'}
            {isAvailableSelectLayout && (
              <div className="mt-1">
                {isAvailableSelectPair ? 'Layout: AvailableSelectGridPair' : ''}
                {isMultipleSelectBox ? 'Layout: MultipleSelectBox' : ''}
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div
      className={`w-full h-full border flex flex-col rounded ${theme.mainContentSection} ${
        showAvailableSelectRuntime ? 'min-h-[100px]' : ''
      }`}
      style={
        gridContentMaxHeight
          ? { height: gridContentMaxHeight, display: 'flex', flexDirection: 'column', minHeight: 0 }
          : showAvailableSelectRuntime
            ? { display: 'flex', flexDirection: 'column', minHeight: 0, flex: '1 1 auto', height: '100%' }
            : undefined
      }
    >
      {/* Grid Header */}
      <div
        className={
          actionButtonsPosition === 'left'
            ? `flex items-center gap-3 border-b px-3 py-2 ${t('border_mainContentSection')} ${theme.mainContentSection}`
            : `flex items-center justify-between border-b px-3 py-2 ${t('border_mainContentSection')} ${theme.mainContentSection}`
        }
      >
        <div className="flex items-center gap-2">
          <div className={`font-semibold ${theme.title}`}>{unitExDto?.UnitDisplayName || 'Data Grid'}</div>
          <FlexGridAddOn
            gridRef={flexGridRef}
            storageKey={`mdGrid:${unitId}`}
            title="Freeze / Show / Hide columns"
          />
        </div>

        {( !isGridReadOnly || hasUnitNavigation) && (
          <div className="flex items-center gap-2 flex-wrap">
            {!isGridReadOnly && (
              <>
                {/* Matrix grid rows are (re)generated via Generate Matrix, not added/deleted manually. */}
                {!showAvailableSelectRuntime && !isMatrixUnit && (
                <button
                  type="button"
                  onMouseDown={(e) => e.stopPropagation()}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleAddRow();
                  }}
                  className={`px-2 py-1 ${theme.button_default} rounded text-xs hover:shadow-sm`}
                  title="Add Row"
                >
                  <i className="fa fa-plus mr-1" />
                  Add Row
                </button>
                )}
                {!showMultipleSelectBoxUi && !isMatrixUnit && (
                <button
                  type="button"
                  onMouseDown={(e) => e.stopPropagation()}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDeleteSelectedRows();
                  }}
                  className={`px-2 py-1 ${theme.button_default} rounded text-xs hover:shadow-sm`}
                  title="Delete Selected Rows"
                >
                  <i className="fa fa-minus mr-1" />
                  Delete Row
                </button>
                )}
                {isMatrixUnit && (
                <button
                  type="button"
                  disabled={isGeneratingMatrix}
                  onMouseDown={(e) => e.stopPropagation()}
                  onClick={(e) => {
                    e.stopPropagation();
                    handleGenerateMatrix();
                  }}
                  className={`px-2 py-1 ${theme.button_default} rounded text-xs hover:shadow-sm disabled:opacity-50`}
                  title="Generate matrix rows from the foreign-key field values"
                >
                  <i className={`fa ${isGeneratingMatrix ? 'fa-spinner fa-spin' : 'fa-table-cells'} mr-1`} />
                  Generate Matrix
                </button>
                )}
              </>
            )}

            {renderUnitNavigationHeaderButtons()}
          </div>
        )}
      </div>

      {/* Grid Content — Available/Selected pair (EmGridViewDisplayType 5 / 6) or single grid */}
      <div
        className={`h-1 flex-auto ${
          showAvailableSelectPairSplit
            ? 'flex flex-row h-1 flex-auto min-h-0 items-stretch gap-6 overflow-hidden px-4 py-2'
            : showMultipleSelectBoxUi
              ? 'flex h-1 flex-auto flex-col overflow-hidden px-4 py-2'
              : showAvailableSelectConfigWarning
                ? 'flex h-1 flex-auto flex-col gap-1 overflow-auto'
                : ''
        }`}        
      >
        {showAvailableSelectConfigWarning && (
          <div className={`shrink-0 border-b px-2 py-1 text-xs ${t('border_mainContentSection')} ${theme.mainContentSection} ${theme.label}`}>
            Available / Selected: set Available Source Unit on this unit and map &quot;Mapping To Available Source Unit
            Field&quot; to a field on the source unit.
          </div>
        )}

        {showAvailableSelectPairSplit && (
          <>
            <div
              className={`w-1 flex-auto min-w-0 min-h-0 flex flex-col border-r pr-5 ${t('border_mainContentSection')} ${theme.mainContentSection}`}
            >
              <div className={`flex items-center justify-between gap-2 px-1 py-1 text-xs ${theme.label}`}>
                <span className="font-medium">{sourceUnitExDto?.UnitDisplayName ?? 'Available'}</span>
                <span
                  className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wide ${theme.mainContentSection}`}
                >
                  AvailableSelectGridPair
                </span>
              </div>
              <div className="h-1 w-full flex-auto">
                <FlexGrid
                  ref={availableFlexGridRef}
                  className="h-full w-full"
                  itemsSource={availableCollectionView}
                  isReadOnly={false}
                  beginningEdit={(s: any, e: any) => {
                    e.cancel = true;
                  }}
                  selectionMode="MultiRange"
                  preserveOutlineState={true}
                  allowSorting={false}
                  headersVisibility="All"
                  style={{ height: '100%', width: '100%', border: 'none' }}
                >
                  {sourceFields.map((sf: any) => {
                    const dm = getDataMapForSourceField(sf);
                    const w =
                      typeof sf.DisplayWidth === 'number'
                        ? sf.DisplayWidth
                        : sf.DisplayWidth
                          ? parseInt(String(sf.DisplayWidth), 10)
                          : 120;
                    const fw = Number.isNaN(Number(w)) ? 120 : Number(w);
                    return (
                      <FlexGridColumn
                        key={sf.Id || sf.DataBaseFieldName}
                        name={String(sf.Id || '')}
                        header={sf.DisplayName || sf.DataBaseFieldName}
                        binding={sf.DataBaseFieldName}
                        width={fw}
                        dataMap={dm ?? undefined}
                        isReadOnly={true}
                      />
                    );
                  })}
                  <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
                </FlexGrid>
              </div>
            </div>

            <div className="relative z-10 flex shrink-0 flex-col justify-center gap-3 px-3 py-4">
              <button
                type="button"
                disabled={isGridReadOnly}
                title="Add selected available rows to selected list"
                className={`min-h-[36px] min-w-[36px] rounded-[4px] px-3 py-2 text-sm ${theme.button_default} disabled:opacity-50`}
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  handleAddFromAvailable();
                }}
              >
                <i className="fa-solid fa-angle-right" aria-hidden />
              </button>
              <button
                type="button"
                disabled={isGridReadOnly}
                title="Remove selected rows from selected list"
                className={`min-h-[36px] min-w-[36px] rounded-[4px] px-3 py-2 text-sm ${theme.button_default} disabled:opacity-50`}
                onClick={(e) => {
                  e.stopPropagation();
                  e.preventDefault();
                  handleDeleteSelectedRows();
                }}
              >
                <i className="fa-solid fa-angle-left" aria-hidden />
              </button>
            </div>
          </>
        )}

        {showMultipleSelectBoxUi && (
          <div className="h-1 w-full min-h-0 min-w-0 flex-auto flex flex-col overflow-hidden">
            <div className={`flex shrink-0 items-center justify-between gap-2 px-1 py-2 text-xs ${theme.label}`}>
              <span className="font-normal">{sourceUnitExDto?.UnitDisplayName ?? 'Available'}</span>
              <span
                className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] font-normal uppercase tracking-wide opacity-80 ${theme.label}`}
              >
                MultipleSelectBox
              </span>
            </div>
            <div
              className={`min-h-0 flex-auto overflow-auto rounded border px-3 py-3 ${theme.mainContentSection}`}
              role="group"
              aria-label={unitExDto?.UnitDisplayName ?? 'Multi select'}
            >
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {allSourceRowsForMultiSelect.map((srcRow: any, idx: number) => {
                  const srcKey = readTransactionRowField(srcRow, mappingSourceDb);
                  const ks = srcKey != null && srcKey !== '' ? String(srcKey) : '';
                  const checked = ks !== '' && selectedKeysAlreadyInSubscribeGrid.has(ks);
                  const labelRaw = sourceDisplayFieldDbName
                    ? readTransactionRowField(srcRow, sourceDisplayFieldDbName)
                    : srcKey;
                  const label = labelRaw != null && labelRaw !== '' ? String(labelRaw) : ks || '—';
                  return (
                    <label
                      key={ks || `msb-${idx}`}
                      className={`flex cursor-pointer items-center gap-2 rounded px-2 py-1.5 ${theme.inputBox}`}
                    >
                      <input
                        type="checkbox"
                        className="h-3 w-3 shrink-0 accent-current"
                        checked={checked}
                        disabled={isGridReadOnly || !ks}
                        onMouseDown={(e) => e.stopPropagation()}
                        onClick={(e) => e.stopPropagation()}
                        onChange={(e) => {
                          e.stopPropagation();
                          toggleMultipleSelectSourceRow(srcRow, e.target.checked);
                        }}
                      />
                      <span className={`min-w-0 flex-auto truncate text-[11px] font-normal leading-snug ${theme.label}`}>{label}</span>
                    </label>
                  );
                })}
              </div>
            </div>
          </div>
        )}

        {/* Pivot edit grid — EmGridViewDisplayType = 3 */}
        {isPivotEditGrid && pivotDto && isPomGradingPivot && (
          <div className="h-full w-full">
            <PivotEditGridPanel
              pivotDto={pivotDto}
              rows={pivotRows}
              isReadOnly={isGridReadOnly}
              baseSizeId={pivotBaseSizeId}
            />
          </div>
        )}

        {isPivotEditGrid && pivotDto && !isPomGradingPivot && (
          <div className="h-full w-full">
            <MatrixPivotEditGrid
              pivotDto={pivotDto}
              rows={pivotRows}
              isReadOnly={isGridReadOnly}
              primaryKeyFieldNames={pivotPrimaryKeyFieldNames}
              resolveDataMap={(fieldId: any) =>
                buildStandaloneDataMapFromFormStructure(dataModelRef.current, String(fieldId))
              }
              resolveWidth={(fieldId: any) => pivotColumnWidthByFieldId.get(String(fieldId))}
              onRowsChange={handlePivotRowsChange}
            />
          </div>
        )}

        {isPivotEditGrid && !pivotDto && (
          <div className={`flex items-center justify-center h-16 text-xs ${theme.label}`}>
            Pivot grid config not found for unit {String(unitId)}
          </div>
        )}

        {/* Child-unit pivot projection: a grandchild rendered as dynamic columns on this (child) grid.
            The server (AppChildPivotProjectionBL) builds the model and folds edits — UI only renders. */}
        {!isPivotEditGrid && pivotProjectionModelReady && (
          <div className="h-full w-full overflow-hidden">
            <ChildPivotProjectionGrid
              model={projectionModel}
              isReadOnly={isGridReadOnly}
              gridRef={projectionFlexGridRef}
              resolveDataMap={resolveProjectionDataMap}
              resolveWidth={resolveProjectionWidth}
              onImageCellMenu={handleProjectionImageCellMenu}
              onImagePreview={(fileId) => setImagePreviewState({ open: true, fileId })}
              onCellEditBeginning={handleProjectionCellEditBeginning}
              onCellEditEnding={handleProjectionCellEditEnding}
              onCellEditEnded={handleProjectionCellEditEnded}
            />
          </div>
        )}

        {!isPivotEditGrid && !pivotProjectionModelReady && !showMultipleSelectBoxUi && (
        <div
          className={
            showAvailableSelectPairSplit
              ? `w-1 flex-auto min-w-0 min-h-0 flex flex-col overflow-hidden border-l pl-5 ${t('border_mainContentSection')} ${theme.mainContentSection}`
              : showAvailableSelectConfigWarning
                ? 'w-full min-h-0 flex flex-col flex-auto'
                : 'contents'
          }
        >
          {showAvailableSelectPairSplit && (
            <div className={`flex items-center justify-between gap-2 px-1 py-1 text-xs ${theme.label}`}>
              <span className="font-medium">Selected — {unitExDto?.UnitDisplayName ?? ''}</span>
              <span
                className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] uppercase tracking-wide ${theme.mainContentSection}`}
              >
                AvailableSelectGridPair
              </span>
            </div>
          )}
          <div
            className={
              showAvailableSelectPairSplit
                ? 'h-1 flex-auto overflow-auto w-full'
                : showAvailableSelectConfigWarning
                  ? 'h-1 flex-auto w-full overflow-auto'
                  : 'h-1 flex-auto w-full contents'
            }
          >
            <FlexGrid
            ref={flexGridRef}
            itemsSource={collectionView}
            isReadOnly={isGridReadOnly}
            selectionMode="MultiRange"
            preserveOutlineState={true}
            allowSorting={false}
            headersVisibility="All"
            className="w-full h-full"
            style={{ width: '100%', border: 'none' }}
            beginningEdit={handleCellEditBeginning}
            cellEditEnding={handleCellEditEnding}
            cellEditEnded={handleCellEditEnded}
          >
          <FlexGridFilter />
          {!isGrandChildPopupMode && grandChildUnitsForDetail.length > 0 && (
            <FlexGridDetail
              ref={flexGridDetailRef}
              detailVisibilityMode="ExpandSingle"
              template={grandChildDetailTemplate}
            />
          )}
          {isGrandChildPopupMode &&
            grandChildUnitsForDetail.map((gc: any) => {
              const gcUnitId = gc.Id ?? gc.unitId;
              const gcUnitIsReadOnly = normalizeBool(gc?.IsReadOnly);
              if (gcUnitId == null) return null;
              return (
                <FlexGridColumn
                  key={`gc-popup-${String(gcUnitId)}`}
                  binding=""
                  header={gc.UnitDisplayName ?? gc.Name ?? `Unit ${gcUnitId}`}
                  width={130}
                  isReadOnly={true}
                >
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const rowItem = cell?.item?._originalData ?? cell?.item;
                      const parentRowIndex = Number(
                        cell?.item?._rowIndex ?? cell?.row?.dataIndex ?? cell?.row?.index ?? -1
                      );
                      return (
                        <div className="w-full h-full flex items-center justify-center">
                          <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                            disabled={gcUnitIsReadOnly}
                            onMouseDown={(e) => e.stopPropagation()}
                            onClick={(e) => {
                              e.stopPropagation();
                              if (gcUnitIsReadOnly) return;
                              setGrandChildPopupState({
                                open: true,
                                unitId: gcUnitId,
                                rowItem,
                                parentRowIndex,
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

          {/* Field columns */}
          {fields.map((field: any) => {
            const dataMap = getDataMapForField(field);
            // Ensure width is a number
            const width = typeof field.DisplayWidth === 'number' 
              ? field.DisplayWidth 
              : (field.DisplayWidth ? parseInt(field.DisplayWidth.toString(), 10) : 150);
            const finalWidth = isNaN(width) ? 150 : width;
            
            // Determine format string based on control type
            const fileIdToNameMap = dataModel.currentFormData?.DictDocumentIdFileCode ?? {};
            const isImageColumn =
              field.ControlType === emAppControlTypeEnum?.Image ||
              field.ControlType === emAppControlTypeEnum?.ExternalImageUrl ||
              field.ControlType === emAppControlTypeEnum?.ImageBinary;
            const isFileColumn = field.ControlType === emAppControlTypeEnum?.File;
            const isRgbColorColumn = field.ControlType === emAppControlTypeEnum?.RGBColorDisplay;

            const fieldIsReadOnly =
              normalizeBool(field?.IsFormLayoutReadOnly) ||
              normalizeBool(field?.IsReadOnly) ||
              normalizeBool(field?.IsReadonly) ||
              (isEditMode && normalizeBool(field?.IsPrimaryKey));
            const isColumnReadOnly = isGridReadOnly || fieldIsReadOnly;

            let formatString: string | undefined = undefined;
            let dataType: string | undefined = undefined;
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

            if (field.ControlType === emAppControlTypeEnum?.Date) {
              formatString = 'd';
            } else if (field.ControlType === emAppControlTypeEnum?.DateTimeDetail) {
              formatString = 'G';
            } else if (field.ControlType === emAppControlTypeEnum?.Numeric) {
              // Angular uses: format="n@(transFieldDto.Nbdecimal.ToString())"
              // This keeps trailing zeros (e.g. 0.00 / 0.0000).
              dataType = 'Number';
              formatString = `n${resolvedNbdecimal}`;
            }

            // Image column: show thumbnail (Angular formList/grid behavior)
            if (isImageColumn) {
              return (
                <FlexGridColumn
                  key={field.Id || field.DataBaseFieldName}
                  header={field.DisplayName || field.DataBaseFieldName}
                  binding={field.DataBaseFieldName}
                  width={finalWidth}
                  isReadOnly={isColumnReadOnly}
                >
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item as any;
                      const raw = item?.[field.DataBaseFieldName];
                      const fileId = raw != null ? Number(raw) : null;
                      const lockedFields = item?._originalData?.CascadingNeedToBeLockedFields;
                      const isLockedByCascade =
                        Array.isArray(lockedFields) && lockedFields.indexOf(field.DataBaseFieldName) >= 0;
                      const rowIndex = (item?._rowIndex ?? cell?.row?.index) as number;
                      const thumbUrl = fileId ? fileThumbnailUrl(fileId) : null;
                      return (
                        <div className="flex items-center justify-between w-full h-full gap-1">
                          <div className="flex items-center gap-2 min-w-0 flex-auto">
                            {thumbUrl ? (
                              <img
                                src={thumbUrl}
                                alt=""
                                className="max-h-[30px] max-w-[30px] object-contain cursor-pointer flex-shrink-0"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  if (isColumnReadOnly || isLockedByCascade) return;
                                  if (!fileId) return;
                                  setImagePreviewState({ open: true, fileId });
                                }}
                              />
                            ) : (
                              <div className="w-[30px] h-[30px]" />
                            )}
                          </div>
                          {!isColumnReadOnly && !isLockedByCascade && (
                            <button
                              type="button"
                              className={`${theme.button_default} w-7 h-6 rounded-[4px] text-xs flex items-center justify-center flex-shrink-0`}
                              title="Actions"
                              onMouseDown={(e) => e.stopPropagation()}
                              onClick={(e) => {
                                e.stopPropagation();
                                const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                                openCellMenuAt(rect.right, rect.top, {
                                  kind: 'image',
                                  rowIndex,
                                  fieldName: field.DataBaseFieldName,
                                  fileId: fileId && Number.isFinite(fileId) ? fileId : null,
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

            // RGB color swatch column (e.g. "64|255|255")
            if (isRgbColorColumn) {
              return (
                <FlexGridColumn
                  key={field.Id || field.DataBaseFieldName}
                  header={field.DisplayName || field.DataBaseFieldName}
                  binding={field.DataBaseFieldName}
                  width={finalWidth}
                  isReadOnly={true}
                >
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item as any;
                      const raw = item?.[field.DataBaseFieldName];
                      return <RgbColorSwatch value={raw} />;
                    }}
                  />
                </FlexGridColumn>
              );
            }

            // File column: show file name (DictDocumentIdFileCode) + download icon
            if (isFileColumn) {
              return (
                <FlexGridColumn
                  key={field.Id || field.DataBaseFieldName}
                  header={field.DisplayName || field.DataBaseFieldName}
                  binding={field.DataBaseFieldName}
                  width={finalWidth}
                  isReadOnly={isColumnReadOnly}
                >
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item as any;
                      const raw = item?.[field.DataBaseFieldName];
                      const fileId = raw != null ? Number(raw) : null;
                      const lockedFields = item?._originalData?.CascadingNeedToBeLockedFields;
                      const isLockedByCascade =
                        Array.isArray(lockedFields) && lockedFields.indexOf(field.DataBaseFieldName) >= 0;
                      const dictName = fileId ? fileIdToNameMap[String(fileId)] : '';
                      const fallbackText = raw != null && typeof raw === 'string' ? raw : '';
                      const display = dictName || fallbackText || (fileId ? String(fileId) : '');
                      const rowIndex = (item?._rowIndex ?? cell?.row?.index) as number;

                      return (
                        <div className="flex items-center justify-between w-full h-full gap-1 min-w-0">
                          <div
                            className="flex items-center min-w-0 flex-auto cursor-pointer"
                            onClick={(e) => {
                              e.stopPropagation();
                              if (isColumnReadOnly || isLockedByCascade) return;
                              if (!fileId) return;
                              void downloadFileById(fileId);
                            }}
                          >
                            <span className={`truncate block flex-auto text-xs ${display ? '' : theme.label}`}>
                              {display || ''}
                            </span>
                          </div>
                          {!isColumnReadOnly && !isLockedByCascade && (
                            <button
                              type="button"
                              className={`${theme.button_default} w-7 h-6 rounded-[4px] text-xs flex items-center justify-center flex-shrink-0`}
                              title="Actions"
                              onMouseDown={(e) => e.stopPropagation()}
                              onClick={(e) => {
                                e.stopPropagation();
                                const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                                openCellMenuAt(rect.right, rect.top, {
                                  kind: 'file',
                                  rowIndex,
                                  fieldName: field.DataBaseFieldName,
                                  fileId: fileId && Number.isFinite(fileId) ? fileId : null,
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

            return (
              <FlexGridColumn
                key={field.Id || field.DataBaseFieldName}
                name={String(field.Id || '')}
                header={field.DisplayName || field.DataBaseFieldName}
                binding={field.DataBaseFieldName}
                width={finalWidth}
                dataType={field.ControlType === emAppControlTypeEnum?.CheckBox ? 'Boolean' : dataType}
                dataMap={dataMap}
                isRequired={false}
                format={formatString}
                isReadOnly={isColumnReadOnly}
              />
            );
          })}
          {/* Spacer column so Wijmo grid layout stays stable */}
          <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
        </FlexGrid>
          </div>
        </div>
        )}
      </div>

      {grandChildPopupState.open && grandChildPopupState.rowItem && grandChildPopupState.unitId != null && (
        <EmbeddedLinkedPopupFrame
          zIndex={10007}
          title={
            grandChildUnitList.find((u: any) => String(u.Id ?? u.unitId) === String(grandChildPopupState.unitId))
              ?.UnitDisplayName ?? 'Grand Child'
          }
          maximizable={true}
          frameInstanceKey={`${String(grandChildPopupState.unitId)}-${String(grandChildPopupState.parentRowIndex)}`}
          toolbarTrailing={
            <button
              type="button"
              className={`rounded-[4px] p-1.5 ${theme.button_default}`}
              onClick={() => setGrandChildPopupState({ open: false, unitId: null, rowItem: null, parentRowIndex: -1 })}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <div className="w-full h-full overflow-hidden p-2">
            {(() => {
              const popupUnit = grandChildUnitList.find(
                (u: any) => String(u.Id ?? u.unitId) === String(grandChildPopupState.unitId)
              );
              if (!popupUnit) return null;
              // IMPORTANT: Use the live host row reference (not a spread copy), otherwise edits/add/delete
              // may not persist when the popup closes.
              tryMutateInPlace(() => {
                if (grandChildPopupState.rowItem && grandChildPopupState.rowItem.DictDocumentIdFileCode == null) {
                  grandChildPopupState.rowItem.DictDocumentIdFileCode =
                    dataModelRef.current?.currentFormData?.DictDocumentIdFileCode ?? {};
                }
              });
              const popupNestedDataModel = {
                ...dataModelRef.current,
                currentFormData: grandChildPopupState.rowItem,
              };
              return (
                <DataGridLayout
                  unitExDto={popupUnit}
                  unitId={grandChildPopupState.unitId}
                  dataModel={popupNestedDataModel}
                  dictOneToManyHostRow={grandChildPopupState.rowItem}
                  masterDetailFormData={dataModelRef.current?.currentFormData}
                  parentOneToManyUnitId={unitId}
                  parentHostRowIndex={grandChildPopupState.parentRowIndex}
                  controllerModel={controllerModel}
                  transactionExDto={transactionExDto}
                  onDataModelChange={(updatedNested: any) => {
                    // Grandchild popup emits nested updates that can be root master-detail data for cascading.
                    // Extract the updated *child row* by parent unit + index, otherwise we overwrite the row with root data
                    // (which blanks PK fields and drops grandchild additions on reopen).
                    const updatedChildRow = pickChildRowFromNestedUpdate(
                      updatedNested,
                      unitId,
                      grandChildPopupState.parentRowIndex,
                      grandChildPopupState.rowItem
                    );
                    if (!updatedChildRow) return;
                    applyGrandChildRowUpdate(updatedChildRow, grandChildPopupState.parentRowIndex, grandChildPopupState.rowItem);
                  }}
                  actionButtonsPosition="left"
                />
              );
            })()}
          </div>
        </EmbeddedLinkedPopupFrame>
      )}

      {/* Image preview popup (Angular: image click -> preview div) */}
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
            {imagePreviewUrl ? (
              <img src={imagePreviewUrl} alt="" className="max-w-full max-h-full object-contain" />
            ) : (
              <div className={`text-xs ${theme.label}`}>Preview not available.</div>
            )}
          </div>
        </div>
      )}

      {/* Cell context menu */}
      {cellMenu?.open && (
        <div
          ref={cellMenuRef}
          className={`fixed z-[10005] border rounded shadow-lg py-1 ${theme.mainContentSection} ${t('border_mainContentSection')}`}
          style={{ left: cellMenu.x, top: cellMenu.y, minWidth: CELL_CONTEXT_MENU_ESTIMATED_WIDTH }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          <button
            type="button"
            className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}
            onClick={() => {
              setPendingLibraryFileId(null);
              setLibraryState({
                open: true,
                kind: cellMenu.kind,
                rowIndex: cellMenu.rowIndex,
                fieldName: cellMenu.fieldName,
                projectionBinding: cellMenu.projectionBinding,
              });
              closeCellMenu();
            }}
          >
            Select From Library
          </button>
          <button
            type="button"
            className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}
            onClick={() => {
              setUploadState({
                open: true,
                kind: cellMenu.kind,
                rowIndex: cellMenu.rowIndex,
                fieldName: cellMenu.fieldName,
                projectionBinding: cellMenu.projectionBinding,
              });
              closeCellMenu();
            }}
          >
            Upload
          </button>

          {cellMenu.fileId ? <div className={`border-t my-1 ${t('border_mainContentSection')}`} /> : null}

          {cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}
              onClick={() => {
                if (cellMenu.fileId) void downloadFileById(cellMenu.fileId);
                closeCellMenu();
              }}
            >
              Download
            </button>
          ) : null}

          {cellMenu.kind === 'image' && cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}
              onClick={() => {
                setImagePreviewState({ open: true, fileId: cellMenu.fileId });
                closeCellMenu();
              }}
            >
              Preview
            </button>
          ) : null}

          {cellMenu.fileId ? <div className={`border-t my-1 ${t('border_mainContentSection')}`} /> : null}

          {cellMenu.fileId ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs text-red-600 hover:opacity-90`}
              onClick={() => {
                if (cellMenu.projectionBinding) {
                  updateProjectionFileValue(cellMenu.rowIndex, cellMenu.projectionBinding, null);
                } else {
                  updateRowFileValue(cellMenu.rowIndex, cellMenu.fieldName, null);
                }
                closeCellMenu();
              }}
            >
              Clear
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
            await assignUploadedFileToCell(
              uploadState.kind,
              uploadState.rowIndex,
              uploadState.fieldName,
              newId,
              undefined,
              uploadState.projectionBinding,
            );
            setUploadState(null);
          }}
        />
      )}

      {/* Library selector popup for cell */}
      {libraryState?.open && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]" onClick={() => setLibraryState(null)}>
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
                      libraryState.rowIndex,
                      libraryState.fieldName,
                      pendingLibraryFileId,
                      undefined,
                      libraryState.projectionBinding,
                    );
                    setLibraryState(null);
                  }}
                  disabled={!pendingLibraryFileId}
                >
                  Select &amp; Close
                </button>
                <button type="button" className={`p-1 ${theme.button_default} rounded-[4px] text-xs`} onClick={() => setLibraryState(null)} title="Close">
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

      {/* Linked search / NAVI form popups must not live under Wijmo row-detail cells (overflow + stacking). */}
      {searchPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={searchPopupState.popupZIndex}
          title={searchPopupState.title || 'Open Search'}
          frameInstanceKey={searchPopupState.popupZIndex}
          toolbarLeading={
            searchPopupState?.showConfirmClose ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const selectedResults = searchPopupRef.current?.getSelectedResults?.() ?? [];
                  const ls = searchPopupState?.linkedSearch;
                  if (Number(ls?.Action) === 1) {
                    applyLinkedSearchAddRows(selectedResults, ls);
                  } else if (shouldLinkedSearchApplyUpdateHostRow(ls)) {
                    applyLinkedSearchUpdateHostRow(selectedResults, ls);
                  }
                  setSearchPopupState(null);
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
              onClick={() => setSearchPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <AppSearch ref={searchPopupRef} embeddedParamObj={searchPopupState.paramObj} />
        </EmbeddedLinkedPopupFrame>
      )}

      {linkTargetPopupState && (
        <EmbeddedLinkedPopupFrame
          zIndex={linkTargetPopupState.popupZIndex}
          title={linkTargetPopupState.title || 'Navigate'}
          frameInstanceKey={linkTargetPopupState.popupZIndex}
          toolbarLeading={
            linkTargetPopupState.pickerContext ? (
              <button
                type="button"
                className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                onClick={() => {
                  const sel = linkTargetMdSearchRef.current?.getSelectedResults?.() ?? [];
                  const pc = linkTargetPopupState.pickerContext;
                  if (pc && sel.length > 0) {
                    applyLinkTargetMasterDataSelectionToRow(pc.linkTarget, sel[0], pc.hostRow);
                    const dm = dataModelRef.current;
                    if (dictOneToManyHostRow) {
                      tryMutateInPlace(() => {
                        dictOneToManyHostRow.IsDirty = true;
                      });
                      queueNestedGridRecompute();
                    }
                    const gridDataArray = getGridRowsForThisUnit();
                    commitOptimisticUnitRows(gridDataArray);
                    deferDataModelChange({
                      ...dm,
                      currentFormData: {
                        ...nextFormDataForUnitRowUpdate(dm, unitIdStr, gridDataArray),
                        IsDirty: true,
                      },
                    });
                  }
                  setLinkTargetPopupState(null);
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
              onClick={() => setLinkTargetPopupState(null)}
              title="Close"
              aria-label="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          {(() => {
            const route = String(linkTargetPopupState.routeBasePath || '').toLowerCase();
            const paramObj = linkTargetPopupState.paramObj ?? {};
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
              return <AppSearch ref={linkTargetMdSearchRef} embeddedParamObj={paramObj} />;
            }
            if (route === 'formmasterdetail') {
              const embeddedTransactionId = Number(paramObj?.id ?? 0);
              if (!embeddedTransactionId) return <div className="p-3 text-xs">Invalid navigation target.</div>;
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
              if (!embeddedTransactionId) return <div className="p-3 text-xs">Invalid navigation target.</div>;
              return (
                <FormListEdit
                  embedded={{
                    embeddedTransactionId,
                    embeddedParam2: parsedParam2,
                  }}
                />
              );
            }
            return (
              <div className="p-3 text-xs">Unsupported popup target: {String(linkTargetPopupState.routeBasePath || '')}</div>
            );
          })()}
        </EmbeddedLinkedPopupFrame>
      )}
    </div>
  );
};

export default DataGridLayout;
