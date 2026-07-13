import React, { useState, useEffect, useRef, useMemo, useCallback } from "react";
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { DataMap, GroupRow } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import { fetchAuthenticatedImageBlobUrl, resolveSearchThumbnailUrl } from "../../../webapi/fileEndpoints";
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { preserveTabInitialPath, getCurrentActiveTab } from '../../../redux/features/ui/navigation/tabnavSlice';
import { isTransactionFormGroupPath } from '../../../helper/navigationHelper';
import {
  buildFormGroupOpenPayload,
  cacheFormGroupSession,
  EmAppLinkTargetActionType,
  expandBusinessTemplateForFormGroupOpen,
  filterRowContextMenuFormLinkTargets,
  filterRowContextMenuLinkedSearches,
  shouldOpenAsFormGroup,
} from '../../../utils/transactionFormGroupHelper';
import { buildLinkTargetTabTitle, getDictViewColumnValue } from '../../../utils/linkTargetTabTitle';
import RgbColorSwatch from '../../common/RgbColorSwatch';
import { searchSvc } from '../../../webapi/searchSvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';

/**
 * Search API / Immer-frozen rows make DictViewColumnIDKeyValue read-only.
 * Mass Update needs mutable copies so Wijmo nested binding can write (Angular mutates plain objects).
 */
function ensureMutableMassUpdateRows(rows: any[]): any[] {
  if (!Array.isArray(rows)) return [];
  return rows.map((row) => {
    if (!row || typeof row !== 'object') return row;
    const dict = row.DictViewColumnIDKeyValue;
    const mutableDict =
      dict && typeof dict === 'object' ? { ...(dict as Record<string, unknown>) } : {};
    return {
      ...row,
      DictViewColumnIDKeyValue: mutableDict,
      IsChanged: false,
      ChangedColumnIds: Array.isArray(row.ChangedColumnIds) ? [...row.ChangedColumnIds] : [],
    };
  });
}

interface ViewDto {
  Id: string;
  Display: string;
  Columns: any[];
  DictEntityLookupItemDto?: { [key: string]: any[] };
  ViewType?: number;
  FreezeColumnCount?: number;
  IsClusterChildView?: boolean;
  UiId?: string;
  GridOutputMode?: number;
  IsMassUpdate?: boolean;
  IsAllowAddRow?: boolean;
  IsAllowDeleteRow?: boolean;
  IsAllowAdvancedUpdate?: boolean;
  AppFormLinkTargetList?: any[];
  AppViewLinkedSeaechOrUrlDtoList?: any[];
}

/** Angular massUpdateHelper API surface exposed to SearchView toolbar. */
export interface MassUpdateGridApi {
  addRow: () => void;
  removeSelectedRow: () => any | null;
  getItems: () => any[];
  getChangedItems: () => any[];
  /** Commit in-cell editor before Save (avoids lost edits / missed IsChanged). */
  finishEditing: () => void;
  /** Rows covered by the current FlexGrid selection (for Advanced Update "Filter By Selected Rows"). */
  getSelectedItems: () => any[];
  markRowChanged: (rowData: any, columnId?: string | number) => void;
  refresh: () => void;
}

function snapshotMassUpdateRowDict(dict: any): string {
  try {
    return JSON.stringify(dict ?? {});
  } catch {
    return '';
  }
}

interface GridViewLayoutProps {
  viewDto: ViewDto;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
  /** Exposes the underlying Wijmo FlexGrid control to the parent (e.g. for FlexGridAddOn). */
  onGridControlReady?: (grid: any) => void;
  /** Mass Update: expose add/remove/getChanged for SearchView toolbar (Angular massUpdateHelper). */
  onMassUpdateApiReady?: (api: MassUpdateGridApi | null) => void;
}

// Aggregation function types enum
const EmAppAggregationFunctionType = {
  None: 0,
  AVG: 1,
  BooleanSum: 2,
  Max: 3,
  Min: 4,
  RowCount: 5,
  SUM: 6
};

// Control types enum
const EmAppControlType = {
  DDL: 1,
  TextBox: 2,
  Memo: 4,
  Image: 5,
  Grid: 6,
  Date: 7,
  File: 9,
  Label: 10,
  CheckBox: 13,
  Empty: 17,
  Numeric: 20,
  AutoGeneration: 23,
  RGBColorDisplay: 24,
  Video: 25,
  RichText: 26,
  DateTimeDetail: 27,
  Audio: 28,
  PieChart: 29,
  DoughnutChart: 30,
  LinearChart: 31,
  BarChart: 32,
  SearchAndView: 33,
  Time: 34,
  RetrieveData: 35,
  FolderTree: 36,
  ImageBinary: 37,
  AutoComplete: 38,
  RadioButtons: 39,
  DateRange: 40,
  FolderPathDisplay: 41,
  Progress: 42,
  BarCode: 43,
  GoogleMap: 44,
  GoogleAddress: 45,
  Rating: 46,
  TextDisplay: 47,
  SearchAbleDDL: 48,
  YoutubeVideo: 49,
  ExternalImageUrl: 50,
  HtmlContent: 51,
  JsonObject: 52,
  InvalidControlType: 99
};

/** Stable grid thumbnail — loads protected resource URLs via session header (same as WebAPI). */
const SearchGridThumbnailCell = React.memo(({
  fileId,
  thumbUrl,
  searchUsesThumbnailUrls,
}: {
  fileId: number | string;
  thumbUrl?: string | null;
  searchUsesThumbnailUrls?: boolean;
}) => {
  const numericId = Number(fileId);
  const [src, setSrc] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setSrc(null);

    if (thumbUrl) {
      fetchAuthenticatedImageBlobUrl(thumbUrl).then((blobUrl) => {
        if (!cancelled) setSrc(blobUrl);
      });
      return () => { cancelled = true; };
    }

    if (!searchUsesThumbnailUrls) {
      const legacyUrl = resolveSearchThumbnailUrl(numericId, null, false);
      if (!cancelled) setSrc(legacyUrl);
    }

    return () => { cancelled = true; };
  }, [thumbUrl, searchUsesThumbnailUrls, numericId]);

  if (!src) {
    return <div style={{ width: '60px', height: '60px', background: 'rgba(0,0,0,0.04)' }} />;
  }

  return (
    <div style={{ position: 'absolute', top: 0, left: 0, width: '60px', height: '60px', background: 'rgba(0,0,0,0.04)' }}>
      <img
        src={src}
        style={{
          maxHeight: '60px',
          maxWidth: '60px',
          position: 'absolute',
          top: 0,
          left: 0,
          bottom: 0,
          right: 0,
          margin: 'auto',
        }}
        alt=""
        onError={(e) => {
          e.currentTarget.style.display = 'none';
        }}
      />
    </div>
  );
});
SearchGridThumbnailCell.displayName = 'SearchGridThumbnailCell';

const GridViewLayoutInner: React.FC<GridViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
  onSelectionChanged,
  onGridControlReady,
  onMassUpdateApiReady,
}) => {
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const { theme } = useTheme();
  const flex = useRef<any>(null);
  const [rowMenu, setRowMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    rowItem: any | null;
  }>({ visible: false, x: 0, y: 0, rowItem: null });
  const [entityDataMaps, setEntityDataMaps] = useState<Record<string, DataMap>>({});

  useEffect(() => {
    return () => { onGridControlReady?.(null); };
  }, [onGridControlReady]);

  const isMassUpdate = Boolean(viewDto?.IsMassUpdate);
  // Wijmo React (_oldSyncBehavior) keeps the first cellEdit* handler forever — always read current flag via ref.
  const isMassUpdateRef = useRef(isMassUpdate);
  isMassUpdateRef.current = isMassUpdate;

  // Angular: dictViewEntityDataMap from DictEntityLookupItemDto (needed for Mass Update DDL editing).
  useEffect(() => {
    let cancelled = false;

    const buildMaps = (dict: Record<string, any[] | undefined> | null | undefined) => {
      const next: Record<string, DataMap> = {};
      if (!dict || typeof dict !== 'object') return next;
      Object.entries(dict).forEach(([key, list]) => {
        if (!Array.isArray(list) || list.length === 0) return;
        // Match Angular lookupHelper: DataMap(list, 'Id', 'Display').
        // Mass Update: prepend blank so DDL can be cleared (with column isRequired=false).
        const mapSource = isMassUpdate
          ? [{ Id: null, Display: '' }, ...list]
          : list;
        next[String(key)] = new DataMap(mapSource, 'Id', 'Display');
      });
      return next;
    };

    const fromView = viewDto?.DictEntityLookupItemDto;
    if (fromView && typeof fromView === 'object' && Object.keys(fromView).length > 0) {
      setEntityDataMaps(buildMaps(fromView as any));
      return;
    }

    const viewId = viewDto?.Id;
    if (!viewId || !isMassUpdate) {
      setEntityDataMaps({});
      return;
    }

    searchSvc.retrieveViewDictEntityLookupItemDto(String(viewId))
      .then((data: any) => {
        if (cancelled) return;
        setEntityDataMaps(buildMaps(data));
      })
      .catch(() => {
        if (!cancelled) setEntityDataMaps({});
      });

    return () => { cancelled = true; };
  }, [viewDto?.Id, viewDto?.DictEntityLookupItemDto, isMassUpdate]);

  const filterableBindings = useMemo(() => {
    if (!Array.isArray(viewDto?.Columns)) {
      return [];
    }
    return viewDto.Columns
      .filter((column) => column.IsVisible !== false)
      .map((column) => `DictViewColumnIDKeyValue.${column.Id}`);
  }, [viewDto]);

  const visibleColumns = useMemo(() => {
    if (!Array.isArray(viewDto?.Columns)) return [];
    return [...viewDto.Columns]
      .filter((column) => column.IsVisible !== false)
      .sort((a, b) => (a.Sort || 0) - (b.Sort || 0));
  }, [viewDto?.Columns]);

  // Stable CollectionView — recreating it on every parent render cancels in-cell edits.
  const gridDataCVRef = useRef<CollectionView<any>>(new CollectionView<any>([]));
  const [gridDataCV] = useState<CollectionView<any>>(() => gridDataCVRef.current);
  /** Last `viewDataList` array identity bound into the grid (avoid re-cloning and wiping IsChanged). */
  const boundViewDataListRef = useRef<any[] | null>(null);
  const changedMassUpdateRowsRef = useRef<Set<any>>(new Set());
  /** Baseline DictViewColumnIDKeyValue JSON per row (diff fallback when IsChanged flag is missed). */
  const massUpdateBaselineRef = useRef<WeakMap<any, string>>(new WeakMap());

  const markMassUpdateRowChanged = useCallback((rowData: any, columnId?: string | number) => {
    if (!rowData) return;
    try {
      rowData.IsChanged = true;
      if (columnId != null && columnId !== '') {
        if (!Array.isArray(rowData.ChangedColumnIds)) rowData.ChangedColumnIds = [];
        const idNum = Number(columnId);
        const idVal = Number.isFinite(idNum) ? idNum : columnId;
        if (!rowData.ChangedColumnIds.some((x: any) => String(x) === String(idVal))) {
          rowData.ChangedColumnIds.push(idVal);
        }
      }
    } catch {
      // Frozen row objects — Set-tracking + baseline diff still pick up the edit.
    }
    changedMassUpdateRowsRef.current.add(rowData);
  }, []);

  const onMassUpdateCellEditCommit = useCallback((s: any, e: any) => {
    // Must use isMassUpdateRef: Wijmo React keeps the first cellEdit* handler (does not re-bind).
    if (!isMassUpdateRef.current) return;
    const rowData = s?.rows?.[e?.row]?.dataItem;
    const col = s?.columns?.[e?.col];
    const binding: string | undefined = col?.binding;
    const columnId = binding?.startsWith('DictViewColumnIDKeyValue.')
      ? binding.slice('DictViewColumnIDKeyValue.'.length)
      : undefined;
    markMassUpdateRowChanged(rowData, columnId);
  }, [markMassUpdateRowChanged]);

  useEffect(() => {
    const cv = gridDataCVRef.current;
    const raw = Array.isArray(viewDataList) ? viewDataList : [];
    const grid = flex.current?.control ?? flex.current;
    // Never replace/refresh the itemsSource while a cell editor is open.
    if (grid?.activeEditor || grid?.editRange) {
      return;
    }

    if (!isMassUpdate) {
      boundViewDataListRef.current = raw;
      changedMassUpdateRowsRef.current.clear();
      massUpdateBaselineRef.current = new WeakMap();
      if (cv.sourceCollection !== raw) {
        cv.sourceCollection = raw;
        cv.refresh();
      }
      return;
    }

    // Same search-result array → keep current mutable clones (preserves IsChanged for Save).
    if (raw === boundViewDataListRef.current && Array.isArray(cv.sourceCollection) && cv.sourceCollection !== raw) {
      return;
    }

    boundViewDataListRef.current = raw;
    changedMassUpdateRowsRef.current.clear();
    const next = ensureMutableMassUpdateRows(raw);
    const baseline = new WeakMap<any, string>();
    for (const row of next) {
      baseline.set(row, snapshotMassUpdateRowDict(row?.DictViewColumnIDKeyValue));
    }
    massUpdateBaselineRef.current = baseline;
    cv.sourceCollection = next;
    cv.refresh();
  }, [viewDataList, isMassUpdate]);

  // Expose Mass Update API to SearchView toolbar (Angular massUpdateHelper parity).
  useEffect(() => {
    if (!onMassUpdateApiReady) return;
    if (!isMassUpdate) {
      onMassUpdateApiReady(null);
      return;
    }
    const api: MassUpdateGridApi = {
      addRow: () => {
        const cv = gridDataCV;
        if (!cv?.addNew) return;
        const newRow = cv.addNew();
        newRow.IsChanged = true;
        newRow.IsNew = true;
        newRow.ChangedColumnIds = [];
        newRow.DictLinkTargetParameterValue = {};
        newRow.DictSketchOrFileDisplayCode = {};
        newRow.DictViewColumnIDKeyValue = {};
        changedMassUpdateRowsRef.current.add(newRow);
        massUpdateBaselineRef.current.set(newRow, snapshotMassUpdateRowDict({}));
        cv.commitNew();
      },
      removeSelectedRow: () => {
        const grid = flex.current?.control ?? flex.current;
        const rowIndex = grid?.selection?.row;
        if (rowIndex == null || rowIndex < 0) return null;
        const rowData = grid.rows?.[rowIndex]?.dataItem;
        if (!rowData) return null;
        changedMassUpdateRowsRef.current.delete(rowData);
        gridDataCV.remove(rowData);
        return rowData;
      },
      getItems: () => (gridDataCV?.items ? [...gridDataCV.items] : []),
      getChangedItems: () => {
        const items = gridDataCV?.items ?? [];
        const flagged = items.filter(
          (r: any) => r?.IsChanged || changedMassUpdateRowsRef.current.has(r),
        );
        if (flagged.length > 0) return flagged;
        // Diff vs baseline — covers cases where cellEdit handler missed (e.g. before IsMassUpdate ref fix).
        const baseline = massUpdateBaselineRef.current;
        return items.filter((r: any) => {
          if (!r || r.IsNew) return Boolean(r?.IsNew);
          const before = baseline.get(r);
          if (before == null) return false;
          return snapshotMassUpdateRowDict(r.DictViewColumnIDKeyValue) !== before;
        });
      },
      finishEditing: () => {
        const grid = flex.current?.control ?? flex.current;
        try {
          grid?.finishEditing?.(true);
        } catch {
          // ignore
        }
      },
      getSelectedItems: () => {
        const grid = flex.current?.control ?? flex.current;
        if (!grid?.rows?.length) return [];
        const sel = grid.selection;
        if (!sel || sel.row < 0) return [];
        const top = Math.min(sel.row, sel.row2 ?? sel.row);
        const bottom = Math.max(sel.row, sel.row2 ?? sel.row);
        const picked: any[] = [];
        const seen = new Set<any>();
        for (let r = top; r <= bottom; r++) {
          const rowData = grid.rows[r]?.dataItem;
          if (rowData != null && !seen.has(rowData)) {
            seen.add(rowData);
            picked.push(rowData);
          }
        }
        return picked;
      },
      markRowChanged: (rowData: any, columnId?: string | number) => {
        markMassUpdateRowChanged(rowData, columnId);
      },
      refresh: () => {
        gridDataCV?.refresh?.();
        const grid = flex.current?.control ?? flex.current;
        grid?.invalidate?.();
        grid?.refresh?.(true);
      },
    };
    onMassUpdateApiReady(api);
    return () => onMassUpdateApiReady(null);
  }, [isMassUpdate, gridDataCV, onMassUpdateApiReady, markMassUpdateRowChanged]);

  // Keep isRequired=false after column rebuild (DataMap load / column list change).
  useEffect(() => {
    if (!isMassUpdate) return;
    const g = flex.current?.control ?? flex.current;
    if (!g?.columns) return;
    for (let i = 0; i < g.columns.length; i++) {
      g.columns[i].isRequired = false;
    }
  }, [isMassUpdate, visibleColumns, entityDataMaps]);

  // Find edit and delete link targets from view configuration
  const linkTargets = useMemo(() => {
    if (!viewDto?.AppFormLinkTargetList || !Array.isArray(viewDto.AppFormLinkTargetList)) {
      return { editLinkTarget: null, deleteLinkTarget: null, menuItemCount: 0, menuLinkTargets: [], availableLinkToSearchList: [] };
    }

    const rowFormLinkTargets = filterRowContextMenuFormLinkTargets(viewDto.AppFormLinkTargetList);

    const menuLinkTargets = rowFormLinkTargets.filter((lt: any) =>
      lt.ActionType === EmAppLinkTargetActionType.Edit ||
      lt.ActionType === EmAppLinkTargetActionType.EditOnPopup ||
      lt.ActionType === EmAppLinkTargetActionType.Preview ||
      lt.ActionType === EmAppLinkTargetActionType.CreateFromExistingItem
    );

    const menuItemCount = menuLinkTargets.length;

    const availableLinkToSearchList = filterRowContextMenuLinkedSearches(
      viewDto.AppViewLinkedSeaechOrUrlDtoList?.filter((ls: any) => ls.LinkTargetSearchId) || [],
    );
    const totalMenuItemCount = menuItemCount + availableLinkToSearchList.length;

    const editLinkTarget = rowFormLinkTargets
      .filter((lt: any) =>
        lt.ActionType === EmAppLinkTargetActionType.Edit ||
        lt.ActionType === EmAppLinkTargetActionType.EditOnPopup ||
        lt.ActionType === EmAppLinkTargetActionType.Preview
      )
      .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0))[0] || null;

    const deleteLinkTarget = viewDto.AppFormLinkTargetList
      .filter((lt: any) => lt.ActionType === EmAppLinkTargetActionType.Delete)
      .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0))[0] || null;

    return { editLinkTarget, deleteLinkTarget, menuItemCount: totalMenuItemCount, menuLinkTargets, availableLinkToSearchList };
  }, [viewDto]);

  useEffect(() => {
    // Only update state when the menu is open. Returning the same state object skips re-render.
    // Otherwise every grid click (start edit / DDL pick) re-renders FlexGrid and cancels editing.
    const close = () => setRowMenu((m) => (m.visible ? { ...m, visible: false } : m));
    document.addEventListener('click', close);
    return () => document.removeEventListener('click', close);
  }, []);

  // Check if link target is allowed to execute based on source condition
  const isLinkTargetAllowed = (linkTarget: any, dataItem: any): boolean => {
    if (!linkTarget?.SourceConditionViewColumnId) {
      return true;
    }
    
    const conditionValue = dataItem?.DictViewColumnIDKeyValue?.[linkTarget.SourceConditionViewColumnId];
    return conditionValue && conditionValue !== 'False' && conditionValue !== '0';
  };

  // Execute link target - navigate to FormMasterDetail / TransactionFormGroup
  const executeLinkTarget = async (linkTarget: any, dataItem: any) => {
    if (!linkTarget || !dataItem) return;

    // Check if link target is allowed
    if (!isLinkTargetAllowed(linkTarget, dataItem)) {
      alert(`${linkTarget.NavigationActionName || 'Action'} is not available for current row.`);
      return;
    }

    // Handle system-defined page (LinkTargetUsageType == 5)
    if (linkTarget.LinkTargetUsageType === 5 && linkTarget.LinkTargetUrlOrRouteCode) {
      // Get parameters from data item
      let paramId: string | null = null;
      let param1: string | null = null;
      let _param2: string | null = null;

      if (linkTarget.SourceViewColumnId1) {
        paramId = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
      }
      if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) {
        param1 = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId2) ?? null;
      }
      if (linkTarget.SourceViewColumnId3 && linkTarget.TargetColumn3) {
        _param2 = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId3) ?? null;
      }

      const tabTitle = buildLinkTargetTabTitle(
        linkTarget.NavigationActionName,
        Boolean(linkTarget.RowDisplayViewColumnId),
        linkTarget.RowDisplayViewColumnId
          ? getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
          : undefined,
        paramId ?? undefined
      );

      // Extract route code (remove 'main.' prefix if present)
      let routeCode = linkTarget.LinkTargetUrlOrRouteCode;
      if (routeCode.indexOf('main.') >= 0) {
        routeCode = routeCode.replace('main.', '');
      }

      // Navigate to the route
      const paramObj: any = {};
      if (paramId) paramObj.id = paramId;
      if (param1) paramObj.param1 = param1;
      if (_param2) paramObj.param2 = _param2;

      addTabAndNavigate(routeCode, tabTitle, paramObj);
      return;
    }

    // Data model template / Business Template: open TransactionFormGroup
    if (shouldOpenAsFormGroup(linkTarget, viewDto) || linkTarget.LinkTargetTransactionGroupId) {
      let openLinkTarget = linkTarget;
      let openViewDto: any = viewDto;
      try {
        const expanded = await expandBusinessTemplateForFormGroupOpen(
          linkTarget,
          viewDto,
          (id) => appTransactionService.retrieveOneAppBusinessTemplateGroupExDto(id),
        );
        openLinkTarget = expanded.linkTarget;
        openViewDto = expanded.viewDto;
      } catch (err) {
        console.error('Failed to expand business template before open', err);
      }

      const dictDefaults = dataModel?.dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue || {};
      const { tabTitle, sessionKey, param2, sessionData } = buildFormGroupOpenPayload(
        openLinkTarget,
        dataItem,
        openViewDto,
        viewDataList,
        dictDefaults,
      );

      cacheFormGroupSession(dispatch, sessionKey, sessionData);

      const activeTab = getCurrentActiveTab();
      if (activeTab?.tabKey && activeTab.path && !isTransactionFormGroupPath(activeTab.path)) {
        dispatch(preserveTabInitialPath({ tabKey: activeTab.tabKey, path: activeTab.path }));
      }

      addTabAndNavigate('TransactionFormGroup', tabTitle, {
        id: sessionKey,
        preserveLinkTabTitle: true,
        param2,
      });
      return;
    }

    // Handle regular transaction link target
    if (linkTarget.LinkTargetTransactionId) {
      // Get parameters from data item
      let paramId: string | null = null;
      let param1: string | null = null;
      let _param2: string | null = null;

      // For Edit/Delete actions, use SourceViewColumnId1 as the primary key
      if (linkTarget.SourceViewColumnId1) {
        paramId = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId1) ?? null;
      }
      if (linkTarget.SourceViewColumnId2 && linkTarget.TargetColumn2) {
        param1 = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId2) ?? null;
      }
      if (linkTarget.SourceViewColumnId3 && linkTarget.TargetColumn3) {
        _param2 = getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.SourceViewColumnId3) ?? null;
      }

      const tabTitle = buildLinkTargetTabTitle(
        linkTarget.NavigationActionName,
        Boolean(linkTarget.RowDisplayViewColumnId),
        linkTarget.RowDisplayViewColumnId
          ? getDictViewColumnValue(dataItem.DictViewColumnIDKeyValue, linkTarget.RowDisplayViewColumnId)
          : undefined,
        paramId ?? undefined
      );

      // Navigate to FormMasterDetail with transaction ID (+ Business Template group when configured)
      const paramObj: any = { preserveLinkTabTitle: true };
      if (linkTarget.LinkTargetTransactionId) paramObj.id = linkTarget.LinkTargetTransactionId;
      if (paramId) paramObj.param1 = paramId;
      const param2Payload: Record<string, unknown> = {};
      if (linkTarget.LinkTargetTransactionGroupId) {
        param2Payload.transGroupId = linkTarget.LinkTargetTransactionGroupId;
        param2Payload.LinkTargetTransactionGroupId = linkTarget.LinkTargetTransactionGroupId;
      }
      if (param1) param2Payload.legacyParam1 = param1;
      if (Object.keys(param2Payload).length > 0) {
        paramObj.param2 = param2Payload;
      }

      addTabAndNavigate('FormMasterDetail', tabTitle, paramObj);
      return;
    }
  };

  const executeLinkedSearchMenu = (linkedSearchMenu: any, dataItem: any) => {
    const title = linkedSearchMenu?.NavigationActionName || linkedSearchMenu?.Name || 'Navigate To Search';
    const paramObj: any = {
      searchId: linkedSearchMenu?.LinkTargetSearchId,
      isSavedSearch: false
    };
    if (linkedSearchMenu?.LinkTargetSearchViewId) {
      paramObj.initialViewId = linkedSearchMenu.LinkTargetSearchViewId;
    }
    if (dataItem?.Id != null) {
      paramObj.linkedSourceRowId = dataItem.Id;
    }
    addTabAndNavigate('masterdatamanagement', title, paramObj);
  };

  // Get aggregation type string
  const getAggregationType = (column: any): string => {
    if (!column.AggregationFunctionType) return "None";

    switch (column.AggregationFunctionType) {
      case EmAppAggregationFunctionType.AVG: return "Avg";
      case EmAppAggregationFunctionType.Max: return "Max";
      case EmAppAggregationFunctionType.Min: return "Min";
      case EmAppAggregationFunctionType.RowCount: return "CntAll";
      case EmAppAggregationFunctionType.SUM: return "Sum";
      default: return "None";
    }
  };

  // Get column width
  const getColumnWidth = (column: any): number => {
    if (column.Width && column.Width >= 0) {
      return column.Width;
    }
    return 100; // default width
  };

  // Get data type for column
  const getDataType = (column: any): string => {
    switch (column.ControlType) {
      case EmAppControlType.Numeric: return "Number";
      case EmAppControlType.CheckBox: return "Boolean";
      case EmAppControlType.Date:
      case EmAppControlType.DateTimeDetail: return "Date";
      default: return "String";
    }
  };

  // Action cell template - shows edit/delete buttons based on view configuration
  const actionCellTemplate = (ctx: any) => {
    const { editLinkTarget, deleteLinkTarget, menuItemCount } = linkTargets;
    const dataItem = ctx.item;

    // If no link targets configured, don't show action column
    if (!editLinkTarget && !deleteLinkTarget) {
      return <div></div>;
    }

    const showEditButton = editLinkTarget && isLinkTargetAllowed(editLinkTarget, dataItem);
    // Delete button is shown directly only if menuItemCount === 1 (meaning only delete in context menu)
    const showDeleteButton = deleteLinkTarget && menuItemCount === 1 && isLinkTargetAllowed(deleteLinkTarget, dataItem);
    // Show more options if there are multiple menu items OR if both edit and delete exist
    const showMoreOptions = menuItemCount > 1 || (editLinkTarget && deleteLinkTarget && menuItemCount === 1);

    return (
      <div style={{ 
        position: 'absolute', 
        top: 0, 
        bottom: 0, 
        marginTop: 'auto', 
        marginBottom: 'auto', 
        width: '100%', 
        height: '28px', 
        display: 'inline-flex', 
        alignItems: 'center', 
        justifyContent: 'center',
        gap: '4px'
      }}>
        {showEditButton && (
          <button
            className="btn-GridRowHeaderButton"
            title={editLinkTarget.NavigationActionName || "Edit"}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              executeLinkTarget(editLinkTarget, dataItem);
            }}
            style={{ 
              width: '28px', 
              height: '28px', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              padding: 0,
              border: 'none',
              background: 'transparent',
              cursor: 'pointer'
            }}
          >
            <i className="fa fa-pencil" aria-hidden="true" style={{ fontSize: '12px' }}></i>
          </button>
        )}

        {showDeleteButton && (
          <button
            className="btn-GridRowHeaderButton"
            title={deleteLinkTarget.NavigationActionName || "Delete"}
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              executeLinkTarget(deleteLinkTarget, dataItem);
            }}
            style={{ 
              width: '28px', 
              height: '28px', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              padding: 0,
              border: 'none',
              background: 'transparent',
              cursor: 'pointer'
            }}
          >
            <i className="fa fa-trash-o" aria-hidden="true" style={{ fontSize: '12px' }}></i>
          </button>
        )}

        {showMoreOptions && !showDeleteButton && (
          <button
            className="btn-GridRowHeaderButton"
            title="More Options"
            onClick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              setRowMenu({
                visible: true,
                x: e.clientX,
                y: e.clientY,
                rowItem: dataItem
              });
            }}
            style={{ 
              width: '30px', 
              height: '28px', 
              display: 'flex', 
              alignItems: 'center', 
              justifyContent: 'center',
              padding: 0,
              border: 'none',
              background: 'transparent',
              cursor: 'pointer'
            }}
          >
            <i className="fa fa-navicon" aria-hidden="true" style={{ fontSize: '12px' }}></i>
          </button>
        )}
      </div>
    );
  };

  // Calculate frozen columns
  const frozenColumns = viewDto.FreezeColumnCount && viewDto.FreezeColumnCount > 0
    ? viewDto.FreezeColumnCount + 1
    : 0;

  // Ensure we have one footer row so ColumnFooter templates can render
  useEffect(() => {
    const grid = flex.current?.control ?? flex.current;
    if (grid && grid.columnFooters && grid.columnFooters.rows.length === 0) {
      grid.columnFooters.rows.push(new GroupRow());
    }
  }, []);

  const rowCountText = `${gridDataCV?.items?.length ?? 0} rows`;

  const hasActionColumn = !!(linkTargets.editLinkTarget || linkTargets.deleteLinkTarget || linkTargets.menuItemCount > 0);
  const isLinkedSearchMode = Boolean(dataModel?.uiControl?.isLinkedSearch);
  const isSingleSelection = Boolean(dataModel?.uiControl?.isSingleSelection);

  /** Linked search single-row: Wijmo Row `isSelected` only (same as checkbox column); do not mutate data items. */
  const selectSingleLinkedRow = (rowItem: any) => {
    if (!rowItem) return;
    const grid = flex.current?.control ?? flex.current;
    if (!grid?.rows?.length) return;
    for (let i = 0; i < grid.rows.length; i++) {
      const r = grid.rows[i] as any;
      if (r?.dataItem == null) continue;
      r.isSelected = r.dataItem === rowItem;
    }
    grid.invalidate?.();
    const picked = (grid.rows as any[])
      .filter((r: any) => Boolean(r?.isSelected) && r?.dataItem != null)
      .map((r: any) => r.dataItem);
    onSelectionChanged?.(picked);
  };

  const emitSelectedItems = (grid: any) => {
    if (!(isLinkedSearchMode && !isSingleSelection)) return;
    const rows = grid?.rows ?? [];
    const selectedItems = rows
      .filter((r: any) => Boolean(r?.isSelected))
      .map((r: any) => r?.dataItem)
      .filter(Boolean);
    onSelectionChanged?.(selectedItems);
  };

  const toggleRowSelected = (rowItem: any, checked: boolean) => {
    if (!rowItem) return;
    const grid = flex.current?.control ?? flex.current;
    const selectedRow = grid?.rows?.find((r: any) => r?.dataItem === rowItem);
    if (selectedRow) {
      selectedRow.isSelected = checked;
    }
    emitSelectedItems(grid);
    grid?.invalidate?.();
  };

  return (
    <div className="w-full h-full flex flex-col">
      <div className={`h-1 flex-auto ${theme.mainContentSection} overflow-hidden`}>
        <FlexGrid
          ref={flex}
          initialized={(g: any) => {
            onGridControlReady?.(g);
            // Angular GridViewLayout: is-required="false" on view columns (allow clear DDL / empty).
            if (isMassUpdate && g?.columns) {
              for (let i = 0; i < g.columns.length; i++) {
                g.columns[i].isRequired = false;
              }
            }
          }}
          itemsSource={gridDataCV}
          isReadOnly={false}
          selectionMode={isLinkedSearchMode && !isSingleSelection ? "ListBox" : "CellRange"}
          validateEdits={false}
          frozenColumns={frozenColumns}
          cellEditEnding={onMassUpdateCellEditCommit}
          cellEditEnded={onMassUpdateCellEditCommit}
          selectionChanged={(s: any) => {
            if (!(isLinkedSearchMode && !isSingleSelection)) return;
            emitSelectedItems(s);
          }}
          pinningColumn={(grid, args) => {
            const column = grid.columns[args.col];
            if (!column?.binding) {
              args.cancel = true;
            }
          }}
          style={{ width: '100%', height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
        >
          <FlexGridFilter filterColumns={filterableBindings} />

          {/* Linked-search multi-select checkbox column (Angular parity). */}
          {isLinkedSearchMode && !isSingleSelection && (
            <FlexGridColumn header="" width={42} allowSorting={false} isReadOnly={true}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(ctx: any) => (
                  <div
                    className="w-full h-full flex items-center justify-center"
                    onMouseDown={(e) => {
                      // Prevent FlexGrid from toggling selection on mousedown.
                      e.preventDefault();
                      e.stopPropagation();
                    }}
                  >
                    <input
                      type="checkbox"
                      checked={Boolean(ctx?.row?.isSelected)}
                      onChange={(e) => toggleRowSelected(ctx?.item, e.target.checked)}
                      onMouseDown={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                      }}
                      onClick={(e) => e.stopPropagation()}
                    />
                  </div>
                )}
              />
            </FlexGridColumn>
          )}

          {/* Linked-search single row: radio drives FlexGrid Row.isSelected (built-in), same as multi-select checkboxes. */}
          {isLinkedSearchMode && isSingleSelection && (
            <FlexGridColumn header="" width={42} allowSorting={false} isReadOnly={true}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(ctx: any) => (
                  <div
                    className="w-full h-full flex items-center justify-center"
                    onMouseDown={(e) => {
                      // Prevent FlexGrid from changing selection on mousedown.
                      e.preventDefault();
                      e.stopPropagation();
                    }}
                  >
                    <input
                      type="radio"
                      name={`linked-search-single-${String(viewDto?.Id ?? 'grid')}`}
                      checked={Boolean(ctx?.row?.isSelected)}
                      onChange={() => selectSingleLinkedRow(ctx?.item)}
                      onMouseDown={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                      }}
                      onClick={(e) => e.stopPropagation()}
                      aria-label="Select row"
                    />
                  </div>
                )}
              />
            </FlexGridColumn>
          )}

          {/* Action Column - only show if link targets are configured */}
          {hasActionColumn && (
            <FlexGridColumn header="" width={85} allowSorting={false} isReadOnly={true}>
              <FlexGridCellTemplate cellType="Cell" template={actionCellTemplate} />
              <FlexGridCellTemplate
                cellType="ColumnFooter"
                template={() => (
                  <div style={{ textAlign: 'left', paddingLeft: 4, fontSize: 11 }}>
                    {rowCountText}
                  </div>
                )}
              />
            </FlexGridColumn>
          )}


          {/* Dynamic Columns from viewDto */}
          {visibleColumns
            .map((column, index) => {
              // Angular GridViewLayout.cshtml: binding="DictViewColumnIDKeyValue.@filed.Id"
              const binding = `DictViewColumnIDKeyValue.${column.Id}`;
              const width = getColumnWidth(column);
              const _dataType = getDataType(column);
              const aggregate = getAggregationType(column);
              const isReadOnly = !column.IsUpdatable;
              const ddlDataMap =
                column.ControlType === EmAppControlType.DDL && column.EntityId != null
                  ? entityDataMaps[String(column.EntityId)]
                  : undefined;

              if (column.ControlType === EmAppControlType.Image) {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    aggregate={aggregate}
                    isReadOnly={isReadOnly}
                    visible={column.IsVisible !== false}
                  >
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(ctx: any) => {
                        const fileId = ctx.item.DictViewColumnIDKeyValue?.[column.Id];
                        if (!fileId) {
                          return <div style={{ width: '60px', height: '60px' }} />;
                        }
                        const thumbUrl = ctx.item.DictThumbnailUrl?.[column.Id];
                        const searchUsesThumbnailUrls = ctx.item.DictThumbnailUrl != null;
                        return (
                          <SearchGridThumbnailCell
                            fileId={fileId}
                            thumbUrl={thumbUrl}
                            searchUsesThumbnailUrls={searchUsesThumbnailUrls}
                          />
                        );
                      }}
                    />
                    <FlexGridCellTemplate cellType="Group" template={() => <div></div>} />
                  </FlexGridColumn>
                );
              }

              else if (column.ControlType === EmAppControlType.DDL && ddlDataMap) {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    aggregate={aggregate}
                    isReadOnly={isReadOnly}
                    isRequired={false}
                    dataMap={ddlDataMap}
                    visible={column.IsVisible !== false}
                  >
                    {!hasActionColumn && index === 0 && (
                      <FlexGridCellTemplate
                        cellType="ColumnFooter"
                        template={() => (
                          <div style={{ textAlign: 'left', paddingLeft: 4, fontSize: 11 }}>
                            {rowCountText}
                          </div>
                        )}
                      />
                    )}
                    <FlexGridCellTemplate cellType="Group" template={() => <div></div>} />
                  </FlexGridColumn>
                );
              }

              else if (column.ControlType === EmAppControlType.CheckBox) {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    dataType="Boolean"
                    aggregate={aggregate}
                    isReadOnly={isReadOnly}
                    isRequired={false}
                    visible={column.IsVisible !== false}
                  >
                    {/* <FlexGridCellTemplate cellType="Cell" template={(ctx: any) => (
                      <input 
                        type="checkbox" 
                        checked={ctx.value} 
                        readOnly={isReadOnly}
                        onChange={(e) => {
                          if (!isReadOnly) {
                            ctx.item.DictViewColumnIDKeyValue[column.Id] = e.target.checked;
                          }
                        }}
                      />
                    )} />
                    <FlexGridCellTemplate cellType="Group" template={() => <div></div>} /> */}
                  </FlexGridColumn>
                );
              }

              else if (column.ControlType === EmAppControlType.RGBColorDisplay) {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    aggregate={aggregate}
                    isReadOnly={true}
                    visible={column.IsVisible !== false}
                  >
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(ctx: any) => {
                        const raw = ctx.item.DictViewColumnIDKeyValue?.[column.Id];
                        return <RgbColorSwatch value={raw} />;
                      }}
                    />
                    <FlexGridCellTemplate cellType="Group" template={() => <div></div>} />
                  </FlexGridColumn>
                );
              }

              else if (column.ControlType === EmAppControlType.Date) {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    dataType="Date"
                    aggregate={aggregate}
                    isReadOnly={isReadOnly}
                    isRequired={false}
                    visible={column.IsVisible !== false}
                  >
                    <FlexGridCellTemplate cellType="CellEdit" template={(ctx: any) => (
                      <input
                        type="date"
                        value={ctx.value}
                        readOnly={isReadOnly}
                        onChange={(e) => {
                          if (!isReadOnly) {
                            ctx.item.DictViewColumnIDKeyValue[column.Id] = e.target.value;
                            markMassUpdateRowChanged(ctx.item, column.Id);
                          }
                        }}
                        style={{ width: '100%', height: '100%', boxSizing: 'border-box' }}
                      />
                    )} />
                    <FlexGridCellTemplate cellType="Group" template={() => <div></div>} />
                  </FlexGridColumn>
                );
              }
              else {
                return (
                  <FlexGridColumn
                    key={column.Id}
                    header={column.Name || column.Display}
                    binding={binding}
                    width={width}
                    aggregate={aggregate}
                    isReadOnly={isReadOnly}
                    isRequired={false}
                    wordWrap={column.ControlType === EmAppControlType.Memo}
                    visible={column.IsVisible !== false}
                  >
                    {!hasActionColumn && index === 0 && (
                      <FlexGridCellTemplate
                        cellType="ColumnFooter"
                        template={() => (
                          <div style={{ textAlign: 'left', paddingLeft: 4, fontSize: 11 }}>
                            {rowCountText}
                          </div>
                        )}
                      />
                    )}
                  </FlexGridColumn>
                );
              }
            })}

          {/* Empty column to fill remaining space */}
          <FlexGridColumn isReadOnly={true} width="*"></FlexGridColumn>
        </FlexGrid>
      </div>
      {rowMenu.visible && rowMenu.rowItem && (
        <div
          className={`fixed z-[1200] min-w-[220px] border rounded shadow-lg ${theme.mainContentSection}`}
          style={{ left: rowMenu.x, top: rowMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {linkTargets.menuLinkTargets
            .filter((lt: any) => isLinkTargetAllowed(lt, rowMenu.rowItem))
            .map((lt: any, idx: number) => {
              const isCreate = lt?.ActionType === EmAppLinkTargetActionType.Create;
              return (
                <button
                  key={`lt-${lt?.Id ?? idx}`}
                  type="button"
                  className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                  onClick={() => {
                    executeLinkTarget(lt, rowMenu.rowItem);
                    setRowMenu((m) => ({ ...m, visible: false }));
                  }}
                >
                  {isCreate ? <i className="fa fa-plus mr-1" aria-hidden="true" /> : null}
                  {lt?.NavigationActionName || 'Menu'}
                </button>
              );
            })}
          {linkTargets.availableLinkToSearchList
            .filter((ls: any) => Boolean(ls?.LinkTargetSearchId))
            .map((ls: any, idx: number) => (
              <button
                key={`ls-${ls?.Id ?? idx}`}
                type="button"
                className={`w-full px-3 py-1.5 text-left text-xs ${theme.contextMenu}`}
                onClick={() => {
                  executeLinkedSearchMenu(ls, rowMenu.rowItem);
                  setRowMenu((m) => ({ ...m, visible: false }));
                }}
              >
                {ls?.NavigationActionName || 'Navigate To Search'}
              </button>
            ))}
        </div>
      )}
    </div>
  );
};

export const GridViewLayout = React.memo(GridViewLayoutInner);