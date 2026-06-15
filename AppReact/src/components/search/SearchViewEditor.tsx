/**
 * Search View Editor – migrated from AngularJS searchViewEditorCtrl / SearchViewEditor.cshtml.
 * Phase 2: View Fields grid, Rest Resource Uri, Edit Navigation Menus, Edit Formula, Layout.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { ComboBox, InputNumber } from '@mescius/wijmo.react.input';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { APP_VIEW_TYPE_EDITOR_OPTIONS } from './emAppViewTypeEditorOptions';
import ClusterAnalysisViewLayoutEditor, {
  CLUSTER_ANALYSIS_VIEW_TYPE,
  createDefaultClusterFlexLayout,
} from './ClusterAnalysisViewLayoutEditor';

const PAGE_TITLE = 'Search View Editor';

export type SearchViewEditorEmbedParam = {
  id?: string | number | null;
  param1?: string | null;
  param2?: string | null;
};

export interface SearchViewEditorProps {
  /** When true, read `embedParam` instead of route `param`; compact chrome for embedding in Search Editor. */
  embedded?: boolean;
  embedParam?: SearchViewEditorEmbedParam | null;
  /** After a successful Save in embedded mode. */
  onSaved?: (savedViewId?: number | null) => void | Promise<void>;
  /** Optional: expose save action to parent (e.g. Search save should save view first). */
  onRegisterSaveAction?: (saveAction: (() => Promise<boolean>) | null) => void;
}
const EmAppViewType = {
  GridView: 1,
  CardView: 2,
  ChartView: 7,
  GoogleMapView: 17,
  PivotView: 5,
  WorkflowView: 12,
  RecursiveDataSetTreeView: 18,
  CalendarView: 6,
  GanttView: 15,
  SchedulerView: 16,
  HierarchyMasterDetailView: 23,
  EShopOrderListView: 9,
  EShopProductDetailView: 10,
  FlatDataSetTreeView: 8,
  ClusterAnalysisView: CLUSTER_ANALYSIS_VIEW_TYPE,
};
const EmAppDataServiceType = { IntegrationWebApiCall: 4 };
const CONTROL_TYPE_DDL = 1;

const CALENDAR_REQUIRED = ['EventName', 'EventBody', 'EventStartDate', 'EventEndDate'];
const GANTT_REQUIRED = ['EventName', 'EventBody', 'EventStartDate', 'EventEndDate', 'EventActualStartDate', 'EventActualEndDate', 'EventCompletePercentage'];
const SCHEDULER_REQUIRED = ['EventName', 'EventBody', 'EventStartDate', 'EventEndDate'];
const ALL_EVENT_PROPERTIES = [
  'EventName', 'EventBody', 'EventStartDate', 'EventEndDate', 'EventActualStartDate', 'EventActualEndDate',
  'EventCompletePercentage', 'EventType', 'EventCompletStage', 'EventStatus', 'EventUserId', 'EventDescription1', 'EventDescription2',
  'EventDateId', 'EventTransactionId', 'EventTransactionRId', 'EventGroupById', 'EventColorId'
];

/** Wijmo binds `isRequired` (camelCase); API often returns `IsRequired`. Derive required flags for event rows when missing. */
function normalizeEventViewFieldIsRequired(view: any) {
  const vt = view?.ViewType;
  if (vt !== EmAppViewType.CalendarView && vt !== EmAppViewType.GanttView && vt !== EmAppViewType.SchedulerView) return;
  const requiredList =
    vt === EmAppViewType.CalendarView ? CALENDAR_REQUIRED : vt === EmAppViewType.GanttView ? GANTT_REQUIRED : SCHEDULER_REQUIRED;
  const list = view?.AppSearchViewFieldList;
  if (!Array.isArray(list)) return;
  for (const f of list) {
    if (f == null) continue;
    if (f.isRequired == null && f.IsRequired != null) {
      f.isRequired = Boolean(f.IsRequired);
    }
    const dt = f.DisplayText;
    if (f.isRequired == null && typeof dt === 'string' && requiredList.includes(dt)) {
      f.isRequired = true;
    }
    if (f.isRequired == null) {
      f.isRequired = false;
    }
  }
}

const SearchViewEditor: React.FC<SearchViewEditorProps> = ({
  embedded = false,
  embedParam = null,
  onSaved,
  onRegisterSaveAction
}) => {
  const { param } = useParams<{ param: string }>();
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const [paramObj, setParamObj] = useState<{ id?: string; param1?: string; param2?: string }>({});
  const [currentSearchView, setCurrentSearchView] = useState<any>({
    Name: '',
    Description: '',
    DataSetId: null,
    ViewType: 1,
    AppSearchViewFieldList: [],
    IsModified: false
  });
  const [viewTypeCV, setViewTypeCV] = useState<CollectionView | null>(null);
  const [searchViewFieldCV, setSearchViewFieldCV] = useState<CollectionView | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [dataSetName, setDataSetName] = useState<string>('');
  const [currentDataSet, setCurrentDataSet] = useState<any>(null);
  const [dataSetColumnList, setDataSetColumnList] = useState<any[]>([]);
  const [allDataSetList, setAllDataSetList] = useState<any[]>([]);
  const [allSearchList, setAllSearchList] = useState<any[]>([]);
  const [transactionList, setTransactionList] = useState<any[]>([]);
  const [transactionUnitList, setTransactionUnitList] = useState<any[]>([]);
  const [massUpdateUnitList, setMassUpdateUnitList] = useState<any[]>([]);
  const [massUpdateFieldList, setMassUpdateFieldList] = useState<any[]>([]);
  const [massUpdateNotesOpen, setMassUpdateNotesOpen] = useState(false);
  /** Embedded in Search Editor: collapse the Name/View type / options form strip (not toolbar or View Fields grid). */
  const [embeddedViewPropsCollapsed, setEmbeddedViewPropsCollapsed] = useState(false);
  const [columnPickerOpen, setColumnPickerOpen] = useState(false);
  const [isHierarchyChild, setIsHierarchyChild] = useState(false);
  const columnPickerGridRef = useRef<any>(null);
  const [navMenuOpen, setNavMenuOpen] = useState(false);
  const [addFieldOpen, setAddFieldOpen] = useState(false);
  const [showAdvancedColumns, setShowAdvancedColumns] = useState(false);
  /** Cluster Analysis View: switch between master View Fields grid and flex layout designer. */
  // Angular cluster editor opens on the layout panel by default.
  const [clusterEditorTab, setClusterEditorTab] = useState<'fields' | 'layout'>('layout');
  // SearchEditor passes this through embedParam so the layout editor can open MasterDataManagement preview.
  const [clusterPreviewSearchId, setClusterPreviewSearchId] = useState<string | null>(null);
  const [entityListForFilter, setEntityListForFilter] = useState<{ Id: number; Display?: string; Name?: string; Code?: string }[]>([]);
  const [entitySelectionItem, setEntitySelectionItem] = useState<any | null>(null);
  const [datasourceSelectorSelectedId, setDatasourceSelectorSelectedId] = useState<number | null>(null);
  const navMenuRef = useRef<HTMLDivElement>(null);
  const addFieldRef = useRef<HTMLDivElement>(null);
  const flexGridRef = useRef<any>(null);
  const suppressViewTypeChangeRef = useRef(false);
  
  const emAppControlType = useEnumValues('EmAppControlType');
  const emAppAggregationFunctionType = useEnumValues('EmAppAggregationFunctionType');
  const emAppSearchViewGridOutputMode = useEnumValues('EmAppSearchViewGridOutputMode');
  const emAppCanlendarMode = useEnumValues('EmAppCanlendarMode');
  const emAppChartViewType = useEnumValues('EmAppChartViewType');
  const emAppWijmoPivotAggregationType = useEnumValues('EmAppWijmoPivotAggregationType');
  const emInternalCodeRegistrationForGoogleMapView = useEnumValues('EmInternalCodeRegistrationForGoogleMapView');

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (navMenuRef.current && !navMenuRef.current.contains(e.target as Node)) setNavMenuOpen(false);
      if (addFieldRef.current && !addFieldRef.current.contains(e.target as Node)) setAddFieldOpen(false);
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);
  /** View Type dropdown — display strings match Angular `searchViewEditorCtrl.js` `appViewTypeList`. */
  const viewTypeList = APP_VIEW_TYPE_EDITOR_OPTIONS;

  const controlTypeList = useMemo(() => {
    if (!emAppControlType) return [];
    return Object.entries(emAppControlType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppControlType]);
  const aggregationFunctionTypeList = useMemo(() => {
    if (!emAppAggregationFunctionType) return [];
    return Object.entries(emAppAggregationFunctionType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppAggregationFunctionType]);
  const gridOutputModeList = useMemo(() => (emAppSearchViewGridOutputMode ? Object.entries(emAppSearchViewGridOutputMode).map(([k, id]) => ({ Id: id, Display: k })) : []), [emAppSearchViewGridOutputMode]);
  const calendarModeList = useMemo(() => (emAppCanlendarMode ? Object.entries(emAppCanlendarMode).map(([k, id]) => ({ Id: id, Display: k })) : []), [emAppCanlendarMode]);
  const chartTypeList = useMemo(() => (emAppChartViewType ? Object.entries(emAppChartViewType).map(([k, id]) => ({ Id: id, Display: k })) : []), [emAppChartViewType]);
  const googleMapInternalCodeRegistrationList = useMemo(
    () =>
      emInternalCodeRegistrationForGoogleMapView
        ? Object.entries(emInternalCodeRegistrationForGoogleMapView).map(([k, id]) => ({ Id: id, Display: k }))
        : [],
    [emInternalCodeRegistrationForGoogleMapView]
  );
  const pivotAggregationList = useMemo(() => {
    if (!emAppWijmoPivotAggregationType) return [];
    return Object.entries(emAppWijmoPivotAggregationType).map(([k, id]) => ({ Id: id, Display: k, longDisplay: k }));
  }, [emAppWijmoPivotAggregationType]);
  const dataSetColumnsDataMap = useMemo(() => new DataMap(dataSetColumnList, 'Id', 'Id'), [dataSetColumnList]);
  const controlTypeDataMap = useMemo(() => new DataMap(controlTypeList, 'Id', 'Display'), [controlTypeList]);
  const aggregationFunctionTypeDataMap = useMemo(() => (aggregationFunctionTypeList.length ? new DataMap(aggregationFunctionTypeList, 'Id', 'Display') : null), [aggregationFunctionTypeList]);
  const massUpdateTransFieldDataMap = useMemo(() => (massUpdateFieldList.length ? new DataMap(massUpdateFieldList, 'Id', 'Display') : null), [massUpdateFieldList]);
  const gridOutputModeDataMap = useMemo(() => (gridOutputModeList.length ? new DataMap(gridOutputModeList, 'Id', 'Display') : null), [gridOutputModeList]);
  const calendarModeDataMap = useMemo(() => (calendarModeList.length ? new DataMap(calendarModeList, 'Id', 'Display') : null), [calendarModeList]);
  const googleMapInternalCodeRegistrationDataMap = useMemo(
    () =>
      googleMapInternalCodeRegistrationList.length
        ? new DataMap(googleMapInternalCodeRegistrationList, 'Id', 'Display')
        : null,
    [googleMapInternalCodeRegistrationList]
  );
  const pivotAggregationDataMap = useMemo(() => (pivotAggregationList.length ? new DataMap(pivotAggregationList, 'Id', 'longDisplay') : null), [pivotAggregationList]);
  const transactionCV = useMemo(() => new CollectionView(transactionList ?? []), [transactionList]);
  const massUpdateUnitCV = useMemo(() => new CollectionView(massUpdateUnitList ?? []), [massUpdateUnitList]);

  /** Same dataset searches for Scheduler "Group Filter Search" — must be stable; new CollectionView each render freezes Wijmo ComboBox. */
  const schedulerGroupFilterSearchCV = useMemo(() => {
    const dsId = currentSearchView?.DataSetId;
    if (dsId == null) return new CollectionView<any>([]);
    return new CollectionView<any>(allSearchList.filter((s: any) => s.DataSetId === dsId));
  }, [allSearchList, currentSearchView?.DataSetId]);

  const transactionUnitPickerList = useMemo(
    () =>
      (transactionUnitList ?? []).map((u: any) => ({
        ...u,
        UnitDisplayName: u.UnitDisplayName ?? u.UnitName ?? u.DataBaseTableName ?? u.Id,
      })),
    [transactionUnitList]
  );
  const transactionUnitPickerCV = useMemo(() => new CollectionView(transactionUnitPickerList), [transactionUnitPickerList]);

  useEffect(() => {
    if (!currentSearchView?.Id && !currentSearchView?.DataSetId) return;
    adminSvc.getMassEntitiesLookupItem('AppEntityInfo').then((data: any) => {
      const raw = data?.AppEntityInfo ?? data;
      const arr = Array.isArray(raw) ? raw : [];
      setEntityListForFilter(arr.map((e: any) => ({
        Id: e.Id,
        Display: e.Display ?? e.Name ?? e.Code ?? String(e.Id ?? '')
      })));
    }).catch(() => setEntityListForFilter([]));
  }, [currentSearchView?.Id, currentSearchView?.DataSetId]);

  const getEntityCodeById = useCallback((entityId: number | null | undefined) => {
    if (entityId == null) return '';
    const found = entityListForFilter.find((e) => e.Id === entityId);
    return found?.Display ?? found?.Name ?? found?.Code ?? String(entityId);
  }, [entityListForFilter]);

  const openEntityInfoPopup = useCallback((item: any) => {
    if (item?.ControlType === CONTROL_TYPE_DDL) {
      setEntitySelectionItem(item);
      setDatasourceSelectorSelectedId(item?.EntityId ?? null);
    }
  }, []);

  const openEntityDataEditorPopup = useCallback((entityId: number | null | undefined) => {
    if (entityId != null) {
      addTabAndNavigate('entity-data-preview', 'Entity Data Preview', { id: String(entityId) }, true);
    }
  }, [addTabAndNavigate]);

  const datasourceSelectedHandler = useCallback((entityId: number | null) => {
    if (entitySelectionItem) {
      entitySelectionItem.EntityId = entityId;
      entitySelectionItem.IsModified = true;
      setCurrentSearchView((prev: any) => (prev ? {
        ...prev,
        AppSearchViewFieldList: [...(prev.AppSearchViewFieldList ?? [])],
        IsModified: true
      } : prev));
    }
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
  }, [entitySelectionItem]);

  const closeDatasourceSelectorPopup = useCallback(() => {
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
  }, []);

  /** All dataset columns (except ItemType 1), including ones already on the view — same column may be added multiple times (Angular parity). */
  const columnPickerAvailableList = useMemo(() => {
    return (dataSetColumnList ?? []).filter((c: any) => c.ItemType !== 1);
  }, [dataSetColumnList]);

  const columnPickerCV = useMemo(() => {
    if (!columnPickerAvailableList.length) return null;
    // Keep original dataset column order (match Angular behavior).
    return new CollectionView(columnPickerAvailableList);
  }, [columnPickerAvailableList]);

  useEffect(() => {
    setViewTypeCV(new CollectionView([...viewTypeList]));
  }, [viewTypeList]);

  useEffect(() => {
    const list = currentSearchView?.AppSearchViewFieldList;
    const arr = Array.isArray(list) ? list : [];
    const cv = new CollectionView(arr);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    setSearchViewFieldCV(cv);
  }, [currentSearchView?.AppSearchViewFieldList]);

  const embedParamKey =
    embedded && embedParam
      ? `${embedParam.id ?? ''}|${embedParam.param1 ?? ''}|${embedParam.param2 ?? ''}`
      : '';

  useEffect(() => {
    let p: any = {};
    if (embedded) {
      if (embedParam) {
        p = {
          id: embedParam.id != null ? String(embedParam.id) : undefined,
          param1: embedParam.param1 != null ? String(embedParam.param1) : undefined,
          param2: embedParam.param2 ?? undefined
        };
      }
    } else if (param) {
      try {
        p = JSON.parse(decodeURIComponent(param));
      } catch {
        p = { id: param };
      }
    }
    setParamObj(p);
  }, [param, embedded, embedParamKey]);

  useEffect(() => {
    if (embedded) setEmbeddedViewPropsCollapsed(false);
  }, [embedded, embedParamKey]);

  useEffect(() => {
    if (embedded) return;
    if (currentSearchView?.ViewType !== CLUSTER_ANALYSIS_VIEW_TYPE) {
      setClusterEditorTab('fields');
    }
  }, [embedded, currentSearchView?.ViewType, paramObj.id, embedParamKey]);

  const markChange = useCallback(() => {
    setCurrentSearchView((prev: any) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const previewClusterSearchView = useCallback(() => {
    if (!clusterPreviewSearchId) {
      showInfo('Save the search to preview this Cluster Analysis View.');
      return;
    }
    if (!currentSearchView?.Id) return;
    addTabAndNavigate('masterdatamanagement', 'Cluster Analysis Preview', {
      searchId: clusterPreviewSearchId,
      isSavedSearch: false,
      initialViewId: currentSearchView.Id
    }, true);
  }, [clusterPreviewSearchId, currentSearchView?.Id, addTabAndNavigate, showInfo]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    setIsLoading(true);
    suppressViewTypeChangeRef.current = true;
    try {
      const searchViewId = paramObj.id != null ? String(paramObj.id) : null;
      let param2Obj: any = {};
      if (paramObj.param2) {
        try {
          param2Obj = typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
        } catch { /* ignore */ }
      }
      setClusterPreviewSearchId(!embedded ? (param2Obj?.searchId ?? null) : null);
      const dataSetId = paramObj.param1 != null ? Number(paramObj.param1) : null;

      if (searchViewId) {
        const viewData = await searchSvc.retrieveOneAppSearchViewExDto(searchViewId);
        if (viewData) {
          const data = { ...viewData, IsModified: false };
          if (!Array.isArray(data.AppSearchViewFieldList)) data.AppSearchViewFieldList = [];
          if (!Array.isArray(data.DeletedItemsIds)) data.DeletedItemsIds = [];
          if (!data.OtherSettingsDto) data.OtherSettingsDto = {};
          if (data.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
            const flex = data.OtherSettingsDto.FlexLayoutItems;
            const hasRoot = Array.isArray(flex) && flex.some((x: any) => Number(x?.WidgetItemType) === 100);
            if (!hasRoot) {
              data.OtherSettingsDto = {
                ...data.OtherSettingsDto,
                FlexLayoutItems: createDefaultClusterFlexLayout(),
              };
            }
          }
          normalizeEventViewFieldIsRequired(data);
          setCurrentSearchView(data);
          if (viewData.DataSetId) {
            try {
              const ds = await searchSvc.retrieveOneAppDataSetExDto(String(viewData.DataSetId), false);
              setDataSetName(ds?.Name ?? '');
              setCurrentDataSet(ds);
            } catch {
              setDataSetName('');
              setCurrentDataSet(null);
            }
          } else {
            setDataSetName('');
            setCurrentDataSet(null);
          }
        }
      } else {
        const initialVt = param2Obj.initialViewType ?? 1;
        const otherDto =
          initialVt === CLUSTER_ANALYSIS_VIEW_TYPE
            ? { FlexLayoutItems: createDefaultClusterFlexLayout() }
            : {};
        setCurrentSearchView({
          Name: param2Obj.initialViewName || 'New View',
          Description: '',
          DataSetId: dataSetId,
          ViewType: initialVt,
          AppSearchViewFieldList: [],
          DeletedItemsIds: [],
          OtherSettingsDto: otherDto,
          IsModified: false
        });
        if (dataSetId) {
          try {
            const ds = await searchSvc.retrieveOneAppDataSetExDto(String(dataSetId), false);
            setDataSetName(ds?.Name ?? '');
            setCurrentDataSet(ds);
          } catch {
            setDataSetName('');
            setCurrentDataSet(null);
          }
        } else {
          setDataSetName('');
          setCurrentDataSet(null);
        }
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
      // ComboBox may still fire selection events after load completes due to async Wijmo init.
      // Keep suppression for one more tick to avoid rebuilding mappings.
      setTimeout(() => {
        suppressViewTypeChangeRef.current = false;
      }, 0);
    }
  }, [paramObj.id, paramObj.param1, paramObj.param2, dispatch, showError, embedded]);

  useEffect(() => {
    loadData();
  }, [paramObj.id, paramObj.param1]);

  useEffect(() => {
    if (!currentSearchView?.DataSetId) {
      setDataSetColumnList([]);
      return;
    }
    searchSvc.retrieveQueryColumnList(String(currentSearchView.DataSetId)).then((cols) => {
      setDataSetColumnList(Array.isArray(cols) ? cols : []);
    }).catch(() => setDataSetColumnList([]));
  }, [currentSearchView?.DataSetId]);

  useEffect(() => {
    searchSvc.retrieveAllAppDataSetEntityDto().then((data) => {
      const list = Array.isArray(data) ? data : [];
      setAllDataSetList(list.map((d: any) => ({ ...d, Display: d.Name })));
    }).catch(() => setAllDataSetList([]));
  }, []);

  useEffect(() => {
    searchSvc.retrieveAllAppSearchDto().then((data) => setAllSearchList(Array.isArray(data) ? data : [])).catch(() => setAllSearchList([]));
    adminSvc.getMassEntitiesLookupItem('AppTransaction').then((data) => {
      const list = Array.isArray(data) ? data : (data?.AppTransaction ?? []);
      const arr = Array.isArray(list) ? list : [];
      setTransactionList(arr.map((t: any) => ({ ...t, Display: t.Display ?? t.Name ?? t.Id })));
    }).catch(() => setTransactionList([]));
  }, []);

  useEffect(() => {
    const p2 = paramObj.param2;
    let obj: any = {};
    if (p2) {
      try { obj = typeof p2 === 'string' ? JSON.parse(p2) : p2; } catch { /* ignore */ }
    }
    setIsHierarchyChild(!!obj.isHierarchyChild);
  }, [paramObj.param2]);

  useEffect(() => {
    if (!currentSearchView?.TransactionId) {
      setTransactionUnitList([]);
      return;
    }
    appTransactionService.getOneAppTransactionData(String(currentSearchView.TransactionId)).then((data) => {
      setTransactionUnitList(data?.AppTransactionUnitList ?? []);
    }).catch(() => setTransactionUnitList([]));
  }, [currentSearchView?.TransactionId]);

  useEffect(() => {
    if (!currentSearchView?.UpdateTransctionId) {
      setMassUpdateUnitList([]);
      return;
    }
    appTransactionService.getOneAppTransactionData(String(currentSearchView.UpdateTransctionId)).then((data) => {
      setMassUpdateUnitList(data?.AppTransactionUnitList ?? []);
    }).catch(() => setMassUpdateUnitList([]));
  }, [currentSearchView?.UpdateTransctionId]);

  useEffect(() => {
    if (!currentSearchView?.UpdateBaseTranscationUnitId) {
      setMassUpdateFieldList([]);
      return;
    }
    appTransactionService.retrieveOneAppTransactionUnitExDto(String(currentSearchView.UpdateBaseTranscationUnitId)).then((data) => {
      const fields = data?.AppTransactionFieldList ?? [];
      setMassUpdateFieldList(fields.map((f: any) => ({ Id: f.Id, Display: f.DataBaseFieldName ?? f.Id })));
    }).catch(() => setMassUpdateFieldList([]));
  }, [currentSearchView?.UpdateBaseTranscationUnitId]);

  const availableDefaultSearchList = useMemo(() => {
    if (!currentSearchView?.DataSetId) return [];
    return allSearchList.filter((s: any) => s.DataSetId === currentSearchView.DataSetId);
  }, [allSearchList, currentSearchView?.DataSetId]);

  const hierarchyFilterSearchCV = useMemo(
    () => new CollectionView<any>(availableDefaultSearchList),
    [availableDefaultSearchList]
  );

  const findMaxSort = useCallback((list: any[]) => {
    if (!list?.length) return 0;
    return Math.max(0, ...list.map((x: any) => x.Sort ?? 0));
  }, []);

  const addSearchViewField = useCallback((sysTableFiledPath?: string, isCalculation = false) => {
    const list = currentSearchView?.AppSearchViewFieldList ?? [];
    const maxSort = findMaxSort(list);
    const newField: any = {
      Sort: maxSort + 1,
      DisplayText: sysTableFiledPath || '',
      SysTableFiledPath: sysTableFiledPath || '',
      ControlType: 2,
      IsVisible: true,
      IsCalulationField: isCalculation
    };
    setCurrentSearchView((p: any) => (p ? {
      ...p,
      AppSearchViewFieldList: [...(p.AppSearchViewFieldList ?? []), newField],
      IsModified: true
    } : p));
  }, [currentSearchView, findMaxSort]);

  const addSearchViewFieldsFromColumns = useCallback((columnIds: string[]) => {
    setCurrentSearchView((p: any) => {
      if (!p) return p;
      const list = Array.isArray(p.AppSearchViewFieldList) ? [...p.AppSearchViewFieldList] : [];
      let maxSort = findMaxSort(list);
      const nextFields = columnIds.map((id) => {
        maxSort += 1;
        return {
          Sort: maxSort,
          DisplayText: id,
          SysTableFiledPath: id,
          ControlType: 2,
          IsVisible: true,
          IsCalulationField: false
        };
      });
      return {
        ...p,
        AppSearchViewFieldList: [...list, ...nextFields],
        IsModified: true
      };
    });
    setColumnPickerOpen(false);
  }, [findMaxSort]);

  const handleColumnPickerOk = useCallback(() => {
    const flex = columnPickerGridRef.current?.control ?? columnPickerGridRef.current;
    if (!flex?.selectedRows?.length) {
      showError('Please select at least one column');
      return;
    }
    const selectedIds = flex.selectedRows.map((r: any) => {
      const item = r.dataItem;
      return item?.Id ?? item?.ColumnId ?? String(item);
    }).filter(Boolean);
    if (!selectedIds.length) return;
    // Keep add order the same as popup grid order (Angular behavior).
    const selectedSet = new Set(selectedIds.map((x: any) => String(x)));
    const orderedIds = columnPickerAvailableList
      .map((c: any) => String(c?.Id ?? c?.ColumnId ?? c))
      .filter((id: string) => selectedSet.has(id));
    if (orderedIds.length) addSearchViewFieldsFromColumns(orderedIds);
  }, [addSearchViewFieldsFromColumns, showError, columnPickerAvailableList]);

  const removeFieldsNotInDataSet = useCallback(() => {
    const list = currentSearchView?.AppSearchViewFieldList ?? [];
    const colIds = new Set(dataSetColumnList.map((c: any) => c.Id ?? c.ColumnId ?? String(c)));
    const deletedIds = [...(currentSearchView?.DeletedItemsIds ?? [])];
    const newList = list.filter((f: any) => {
      const path = f.SysTableFiledPath || '';
      if (f.IsCalulationField) return true;
      if (colIds.has(path)) return true;
      if (f.Id) deletedIds.push(f.Id);
      return false;
    });
    setCurrentSearchView((p: any) => (p ? { ...p, AppSearchViewFieldList: newList, DeletedItemsIds: deletedIds, IsModified: true } : p));
  }, [currentSearchView, dataSetColumnList]);

  const removeSearchViewField = useCallback(() => {
    const flex = flexGridRef.current?.control ?? flexGridRef.current;
    if (!flex?.selectedRows?.length || !currentSearchView?.AppSearchViewFieldList) return;
    const toRemove = flex.selectedRows.map((r: any) => r.dataItem).filter(Boolean);
    const deletedIds = [...(currentSearchView.DeletedItemsIds ?? [])];
    const newList = currentSearchView.AppSearchViewFieldList.filter((f: any) => !toRemove.includes(f));
    toRemove.forEach((f: any) => {
      if (f.Id) deletedIds.push(f.Id);
    });
    setCurrentSearchView((p: any) => (p ? {
      ...p,
      AppSearchViewFieldList: newList,
      DeletedItemsIds: deletedIds,
      IsModified: true
    } : p));
  }, [currentSearchView]);

  const handleCellEditEnded = useCallback((_s: any, e: any) => {
    const flex = _s;
    const rowData = flex?.rows?.[e?.row]?.dataItem;
    if (rowData?.Id) rowData.IsModified = true;
    markChange();
  }, [markChange]);

  const viewTypeChanged = useCallback((newViewType: number) => {
    if (suppressViewTypeChangeRef.current) return;
    if (newViewType === currentSearchView?.ViewType) return;
    markChange();
    if (newViewType === EmAppViewType.CalendarView || newViewType === EmAppViewType.GanttView || newViewType === EmAppViewType.SchedulerView) {
      const orgList = currentSearchView?.AppSearchViewFieldList ?? [];
      const findByDisplay = (list: any[], text: string) => list.find((f: any) => f.DisplayText === text);
      const requiredList = newViewType === EmAppViewType.CalendarView ? CALENDAR_REQUIRED : newViewType === EmAppViewType.GanttView ? GANTT_REQUIRED : SCHEDULER_REQUIRED;
      const newList: any[] = [];
      let maxSort = 0;
      ALL_EVENT_PROPERTIES.forEach((propName) => {
        const existing = findByDisplay(orgList, propName);
        if (existing) {
          existing.Sort = maxSort + 1;
          maxSort++;
          existing.isRequired = requiredList.includes(propName);
          existing.IsGroupBy = propName === 'EventGroupById';
          newList.push(existing);
        } else {
          const sort = maxSort + 1;
          maxSort++;
          newList.push({
            Sort: sort,
            DisplayText: propName,
            IsVisible: true,
            SysTableFiledPath: ' ',
            ControlType: 2,
            EntityId: null,
            IsGroupBy: propName === 'EventGroupById',
            isRequired: requiredList.includes(propName)
          });
        }
      });
      setCurrentSearchView((p: any) => (p ? { ...p, ViewType: newViewType, AppSearchViewFieldList: newList, IsModified: true } : p));
    } else if (newViewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
      setCurrentSearchView((p: any) => {
        if (!p) return p;
        const other = { ...(p.OtherSettingsDto || {}) };
        const flex = other.FlexLayoutItems;
        const hasRoot = Array.isArray(flex) && flex.some((x: any) => Number(x?.WidgetItemType) === 100);
        if (!hasRoot) {
          other.FlexLayoutItems = createDefaultClusterFlexLayout();
        }
        return { ...p, ViewType: newViewType, OtherSettingsDto: other, IsModified: true };
      });
    } else {
      setCurrentSearchView((p: any) => (p ? { ...p, ViewType: newViewType } : p));
    }
  }, [currentSearchView, markChange]);

  const save = useCallback(async (): Promise<boolean> => {
    if (!currentSearchView?.IsModified) return true;
    dispatch(setIsBusy());
    try {
      const payload =
        currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE
          ? { ...currentSearchView, IsUpdateClusterMainViewItemSource: true }
          : currentSearchView;
      const data = await searchSvc.saveAppSearchViewExDto(payload);
      if (data?.ValidationResult?.IsValid !== false) {
        const savedViewId = data?.Id ?? data?.Object?.Id ?? currentSearchView?.Id ?? null;
        setCurrentSearchView((prev: any) => (prev ? { ...prev, Id: savedViewId ?? prev.Id, IsModified: false } : prev));
        if (savedViewId && !paramObj.id) {
          setParamObj((p) => ({ ...p, id: String(savedViewId) }));
        }
        showInfo('Saved');
        if (onSaved) {
          await onSaved(savedViewId);
        }
        return true;
      } else {
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
        showError(msgs.length ? msgs.join('; ') : 'Save failed');
        return false;
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
      return false;
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearchView, paramObj.id, dispatch, showError, showInfo, onSaved]);

  useEffect(() => {
    onRegisterSaveAction?.(save);
    return () => onRegisterSaveAction?.(null);
  }, [onRegisterSaveAction, save]);

  const saveAs = useCallback(async () => {
    if (!currentSearchView?.Id) return;
    try {
      const data = await searchSvc.saveAsSearchView(String(currentSearchView.Id));
      if (data?.Id) {
        setCurrentSearchView((prev: any) => (prev ? { ...prev, Id: data.Id, IsModified: true } : prev));
        setParamObj((p) => ({ ...p, id: String(data.Id) }));
        showInfo('Saved as new view');
      } else {
        showError('Save As failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save As failed');
    }
  }, [currentSearchView?.Id, showError, showInfo]);

  return (
    <div
      className={`w-full h-full flex flex-col overflow-hidden relative ${embedded ? 'min-h-0 rounded-none' : 'rounded-t-md rounded-b-md'}`}
    >
      <div
        className={`flex items-center justify-between px-3 shrink-0 ${theme.mainContentSection} ${embedded ? 'py-1.5 mb-0' : 'py-2 mb-1'}`}
      >
        <div className="flex items-center gap-1.5 min-w-0">
          {embedded && (
            <button
              type="button"
              onClick={() => setEmbeddedViewPropsCollapsed((c) => !c)}
              className={`w-7 h-7 shrink-0 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default}`}
              aria-expanded={!embeddedViewPropsCollapsed}
              title={embeddedViewPropsCollapsed ? 'Expand view properties' : 'Collapse view properties'}
              aria-label={embeddedViewPropsCollapsed ? 'Expand view properties' : 'Collapse view properties'}
            >
              <i
                className={`fa-solid ${embeddedViewPropsCollapsed ? 'fa-chevron-down' : 'fa-chevron-up'}`}
                aria-hidden
              />
            </button>
          )}
          {embedded ? (
            <button
              type="button"
              onClick={() => setEmbeddedViewPropsCollapsed((c) => !c)}
              className={`text-sm font-semibold truncate min-w-0 text-left rounded-[4px] px-1 py-0.5 -mx-1 ${theme.title} hover:opacity-90`}
              aria-expanded={!embeddedViewPropsCollapsed}
              title={embeddedViewPropsCollapsed ? 'Expand view properties' : 'Collapse view properties'}
            >
              {currentSearchView?.Name || PAGE_TITLE}
            </button>
          ) : (
            <div className={`text-md font-semibold truncate min-w-0 ${theme.title}`}>{PAGE_TITLE}</div>
          )}
        </div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => loadData()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={save}
            disabled={!currentSearchView?.IsModified}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save
          </button>
          <button
            type="button"
            onClick={saveAs}
            disabled={!currentSearchView?.Id}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-window-restore" aria-hidden /> Save As
          </button>
          {currentSearchView?.ViewType === EmAppViewType.CardView && (
            <button
              type="button"
              onClick={() => {
                if (!currentSearchView?.Id) {
                  showInfo('Save the view first to design layout');
                  return;
                }
                if (currentSearchView.FormId) {
                  addTabAndNavigate('form-design-grid', 'Layout', { id: currentSearchView.FormId, param2: JSON.stringify({ searchViewId: currentSearchView.Id }) });
                } else {
                  searchSvc.createNewSearchViewForm(String(currentSearchView.Id)).then((data: any) => {
                    if (data?.Id) {
                      setCurrentSearchView((p: any) => (p ? { ...p, FormId: data.Id } : p));
                      addTabAndNavigate('form-design-grid', 'Layout', { id: data.Id, param2: JSON.stringify({ searchViewId: currentSearchView.Id }) });
                    }
                  }).catch((err) => showError(err instanceof Error ? err.message : 'Failed to create form'));
                }
              }}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              title="Layout (Card View)"
            >
              <i className="fa-solid fa-desktop" aria-hidden /> Layout
            </button>
          )}
          <div className="relative" ref={navMenuRef}>
            <button type="button" onClick={() => setNavMenuOpen((o) => !o)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
              <i className="fa-solid fa-bars" aria-hidden /> Edit Navigation Menus <i className="fa-solid fa-caret-down text-xs ml-1" aria-hidden />
            </button>
            {navMenuOpen && (
              <div className={`absolute right-0 mt-1 min-w-[200px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}>
                <button type="button" onClick={() => { setNavMenuOpen(false); if (currentSearchView?.Id) addTabAndNavigate('search-view-navigate-to-form-editor', (currentSearchView.Name || '') + ': Navigate To Data Model', { id: currentSearchView.Id, param1: 1 }); else showInfo('Save the view first'); }} className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}>Navigate To Data Model</button>
                <button type="button" onClick={() => { setNavMenuOpen(false); if (currentSearchView?.Id) addTabAndNavigate('search-view-navigate-to-search-editor', (currentSearchView.Name || '') + ': Navigate To Search', { id: currentSearchView.Id, param1: 3 }); else showInfo('Save the view first'); }} className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}>Navigate To Search</button>
                <button type="button" onClick={() => { setNavMenuOpen(false); if (currentSearchView?.Id) addTabAndNavigate('search-view-navigate-to-built-in-page-editor', (currentSearchView.Name || '') + ': Navigate To Built-In Pages', { id: currentSearchView.Id, param1: 5 }); else showInfo('Save the view first'); }} className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}>Navigate To Built-In Pages</button>
              </div>
            )}
          </div>
          {currentSearchView?.Id && (
            <button
              type="button"
              onClick={() => addTabAndNavigate('search-view-formula-editor', (currentSearchView.Name || '') + ': Formula', { id: currentSearchView.Id })}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              title="Edit Formula"
            >
              <i className="fa-solid fa-file-code" aria-hidden /> Edit Formula
            </button>
          )}
          {currentSearchView?.Id && currentDataSet?.DataSourceFrom && (
            <button
              type="button"
              onClick={() => addTabAndNavigate('entity-info-edit', 'New Entity', { param1: currentDataSet.DataSourceFrom, param2: JSON.stringify({ applicationId: currentSearchView.SaasApplicationId || currentDataSet.SaasApplicationId }) })}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              title="Create DDL Entity"
            >
              <i className="fa-solid fa-plus" aria-hidden /> Create DDL Entity
            </button>
          )}
        </div>
      </div>

      <div className="flex-1 min-h-0 flex flex-col overflow-hidden">
      {(!embedded || !embeddedViewPropsCollapsed) && (
      <div className={`flex flex-wrap gap-x-8 gap-y-2 px-3 shrink-0 ${theme.mainContentSection} ${embedded ? 'py-2' : 'py-4'}`}>
        {/* Column 1: Name, Description, Data Service, View Type, Rest Resource (conditional), Calendar/Calendar options */}
        <div className="flex flex-col gap-0">
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Name</label>
            <input
              type="text"
              value={currentSearchView?.Name ?? ''}
              onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, Name: e.target.value } : p)); }}
              className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Description</label>
            <input
              type="text"
              value={currentSearchView?.Description ?? ''}
              onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, Description: e.target.value } : p)); }}
              className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Data Service</label>
            <span className={`w-52 shrink-0 text-xs ${theme.label}`}>{dataSetName || '—'}</span>
          </div>
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>View Type</label>
            {viewTypeCV && (
              <ComboBox
                itemsSource={viewTypeCV}
                displayMemberPath="Display"
                selectedValuePath="Id"
                selectedValue={currentSearchView?.ViewType ?? 1}
                selectedIndexChanged={(s: any) => {
                  const v = s.selectedValue;
                  if (v == null) return;
                  // On initial load, Wijmo ComboBox may fire change events while API/state are settling.
                  // Ignore those events to avoid rebuilding Calendar/Gantt/Scheduler mappings.
                  if (suppressViewTypeChangeRef.current || isLoading) return;
                  viewTypeChanged(v);
                }}
                style={{ width: '13rem', height: '28px', fontSize: '12px' }}
              />
            )}
          </div>
          {(currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE) && (currentDataSet as any)?.QueryType !== EmAppDataServiceType.IntegrationWebApiCall && (
            <>
              <div className="flex items-center gap-3 py-1">
                <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Rest Resource Uri</label>
                <input
                  type="text"
                  value={currentSearchView?.AppRestResourceUri ?? ''}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, AppRestResourceUri: e.target.value } : p)); }}
                  className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
                />
              </div>
              {currentSearchView?.AppRestResourceUri && (
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Rest Uri Button Text</label>
                  <input
                    type="text"
                    value={currentSearchView?.AppRestResourceUriDisplay ?? ''}
                    onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, AppRestResourceUriDisplay: e.target.value } : p)); }}
                    className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
                  />
                </div>
              )}
            </>
          )}
          {currentSearchView?.ViewType === EmAppViewType.CalendarView && (
            <>
              {calendarModeDataMap && (
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Default Mode</label>
                  <ComboBox
                    itemsSource={calendarModeList}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={currentSearchView?.CanlendarDefaultViewMode ?? 0}
                    selectedIndexChanged={(s: any) => { const v = s.selectedValue; if (v != null) { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, CanlendarDefaultViewMode: v } : p)); } }}
                    style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                  />
                </div>
              )}
            </>
          )}
          {currentSearchView?.ViewType === EmAppViewType.SchedulerView && (
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Disable Client Time Convert</label>
              <div className="w-52 shrink-0 flex items-center">
                <input type="checkbox" checked={!!currentSearchView?.IsDisableClientTimeConvert} onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsDisableClientTimeConvert: e.target.checked } : p)); }} />
              </div>
            </div>
          )}
        </div>
        {/* Between Column 1 and Column 2: Calendar options (match Is For Public Access checkbox styling) */}
        {currentSearchView?.ViewType === EmAppViewType.CalendarView && (
          <div className="flex flex-col gap-0">
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Enable Left Navigator</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="cal-enable-left-nav"
                  checked={!!currentSearchView?.IsEnableCalendarNavigator}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsEnableCalendarNavigator: e.target.checked } : p)); }}
                />
              </div>
            </div>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Enable Month View</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="cal-enable-month"
                  checked={!!currentSearchView?.IsEnableCalendarMonthView}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsEnableCalendarMonthView: e.target.checked } : p)); }}
                />
              </div>
            </div>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Enable Week View</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="cal-enable-week"
                  checked={!!currentSearchView?.IsEnableCalendarWeekView}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsEnableCalendarWeekView: e.target.checked } : p)); }}
                />
              </div>
            </div>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Enable Day View</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="cal-enable-day"
                  checked={!!currentSearchView?.IsEnableCalendarDayView}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsEnableCalendarDayView: e.target.checked } : p)); }}
                />
              </div>
            </div>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Disable Client Time Convert</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="cal-disable-client-time"
                  checked={!!currentSearchView?.IsDisableClientTimeConvert}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsDisableClientTimeConvert: e.target.checked } : p)); }}
                />
              </div>
            </div>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Is For Public Access</label>
              <div className="w-52 shrink-0 flex items-center">
                <input
                  type="checkbox"
                  id="isForPublicAccess"
                  checked={!!currentSearchView?.IsForPublicAcesss}
                  onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsForPublicAcesss: e.target.checked } : p)); }}
                />
              </div>
            </div>
          </div>
        )}
        {/* Column 2: Grid options, Is For Public Access, Chart/Pivot/EShop/Filter options */}
        <div className="flex flex-col gap-0">
          {(currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE) && (
            <>
              {gridOutputModeDataMap && (
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Grid Output Mode</label>
                  <ComboBox
                    itemsSource={gridOutputModeList}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={currentSearchView?.GridOutputMode ?? 1}
                    selectedIndexChanged={(s: any) => { const v = s.selectedValue; if (v != null) { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, GridOutputMode: v } : p)); } }}
                    style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                  />
                </div>
              )}
              <div className="flex items-center gap-3 py-1">
                <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Nb. Frozen Column</label>
                <InputNumber
                  value={currentSearchView?.NbFrozenColumn ?? 0}
                  min={0}
                  step={1}
                  valueChanged={(s: any) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, NbFrozenColumn: s.value ?? 0 } : p)); }}
                  style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                />
              </div>
              <div className="flex items-center gap-3 py-1">
                <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Expand Group Levels</label>
                <InputNumber
                  value={currentSearchView?.RowPerPage ?? 0}
                  min={0}
                  max={10}
                  step={1}
                  valueChanged={(s: any) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, RowPerPage: s.value ?? 0 } : p)); }}
                  style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                />
              </div>
            </>
          )}
          {currentSearchView?.ViewType !== EmAppViewType.CalendarView && (
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Is For Public Access</label>
              <div className="w-52 shrink-0 flex items-center">
                <input type="checkbox" id="isForPublicAccess" checked={!!currentSearchView?.IsForPublicAcesss} onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsForPublicAcesss: e.target.checked } : p)); }} />
              </div>
            </div>
          )}
        {currentSearchView?.ViewType === EmAppViewType.EShopProductDetailView && (
          <>
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Data Model</label>
              <ComboBox
                itemsSource={transactionCV}
                displayMemberPath="Display"
                selectedValuePath="Id"
                selectedValue={currentSearchView?.TransactionId ?? null}
                selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, TransactionId: v, ProductDetaiViewMapUnitId: null } : p)); }}
                style={{ width: '13rem', height: '28px', fontSize: '12px' }}
              />
            </div>
            {currentSearchView?.TransactionId && (
              <div className="flex items-center gap-3 py-1">
                <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Data Model Unit</label>
                <ComboBox
                  itemsSource={transactionUnitPickerCV}
                  displayMemberPath="UnitDisplayName"
                  selectedValuePath="Id"
                  selectedValue={currentSearchView?.ProductDetaiViewMapUnitId ?? null}
                  selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, ProductDetaiViewMapUnitId: v } : p)); }}
                  style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                />
              </div>
            )}
          </>
        )}
        {currentSearchView?.ViewType === EmAppViewType.ChartView && chartTypeList.length > 0 && (
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Default Chart Type</label>
            <ComboBox
              itemsSource={chartTypeList}
              displayMemberPath="Display"
              selectedValuePath="Id"
              selectedValue={currentSearchView?.ChartType ?? 0}
              selectedIndexChanged={(s: any) => { const v = s.selectedValue; if (v != null) { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, ChartType: v } : p)); } }}
              style={{ width: '13rem', height: '28px', fontSize: '12px' }}
            />
          </div>
        )}
        {currentSearchView?.ViewType === EmAppViewType.PivotView && pivotAggregationDataMap && (
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Default Aggregation</label>
            <ComboBox
              itemsSource={pivotAggregationList}
              displayMemberPath="longDisplay"
              selectedValuePath="Id"
              selectedValue={currentSearchView?.ChartType ?? 0}
              selectedIndexChanged={(s: any) => { const v = s.selectedValue; if (v != null) { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, ChartType: v } : p)); } }}
              style={{ width: '13rem', height: '28px', fontSize: '12px' }}
            />
          </div>
        )}
        {isHierarchyChild && currentSearchView?.Id && (
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Filter Search</label>
            <div className="w-52 shrink-0 flex items-center gap-1">
              <ComboBox
                itemsSource={hierarchyFilterSearchCV}
                displayMemberPath="Name"
                selectedValuePath="Id"
                selectedValue={currentSearchView?.FilterSearchId ?? null}
                selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, FilterSearchId: v } : p)); }}
                style={{ width: '10rem', height: '28px', fontSize: '12px' }}
              />
              <button type="button" onClick={() => addTabAndNavigate('search-editor', 'Create Filter Search', { param2: JSON.stringify({ initialSearchName: (currentSearchView?.Name || '') + ' Search', initialDataSetId: currentSearchView?.DataSetId, initialDefualtSearchViewId: currentSearchView?.Id }) })} className={`w-7 h-7 rounded ${theme.button_default} text-sm`} title="Create Filter Search">+</button>
              {currentSearchView?.FilterSearchId && (
                <button type="button" onClick={() => addTabAndNavigate('search-editor', 'Edit Filter Search', { id: currentSearchView.FilterSearchId })} className={`w-7 h-7 rounded ${theme.button_default} text-sm`} title="Edit Filter Search">···</button>
              )}
            </div>
          </div>
        )}
        {currentSearchView?.ViewType === EmAppViewType.SchedulerView && currentSearchView?.DataSetId && (
          <div className="flex items-center gap-3 py-1">
            <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Group Filter Search</label>
            <div className="w-52 shrink-0 flex items-center gap-1">
              <ComboBox
                itemsSource={schedulerGroupFilterSearchCV}
                displayMemberPath="Name"
                selectedValuePath="Id"
                selectedValue={currentSearchView?.CatalogueSearchId ?? null}
                selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, CatalogueSearchId: v } : p)); }}
                style={{ width: '10rem', height: '28px', fontSize: '12px' }}
              />
              <button type="button" onClick={() => addTabAndNavigate('search-editor', 'Create Category Search', { param2: JSON.stringify({ callBackCode: 'updateCatagorySearchId' }) })} className={`w-7 h-7 rounded ${theme.button_default} text-sm`} title="Create">+</button>
              {currentSearchView?.CatalogueSearchId && (
                <button type="button" onClick={() => addTabAndNavigate('search-editor', 'Edit Category Search', { id: currentSearchView.CatalogueSearchId })} className={`w-7 h-7 rounded ${theme.button_default} text-sm`} title="Edit">···</button>
              )}
            </div>
          </div>
        )}
        </div>
        {/* Column 3: Mass Update options (hide for Calendar/Gantt/Scheduler) */}
        {(currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE) && currentSearchView?.ViewType !== EmAppViewType.CalendarView && currentSearchView?.ViewType !== EmAppViewType.GanttView && currentSearchView?.ViewType !== EmAppViewType.SchedulerView && (
          <div className="flex flex-col gap-0">
            <div className="flex items-center gap-3 py-1">
              <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Is Mass Update View</label>
              <div className="w-52 shrink-0 flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isMassUpdate"
                  checked={!!currentSearchView?.IsMassUpdateView}
                  onChange={(e) => {
                    markChange();
                    setCurrentSearchView((p: any) => (p ? {
                      ...p,
                      IsMassUpdateView: e.target.checked,
                      UpdateTransctionId: e.target.checked ? p.UpdateTransctionId : null,
                      UpdateBaseTranscationUnitId: null
                    } : p));
                  }}
                />
                {currentSearchView?.IsMassUpdateView && (
                  <button
                    type="button"
                    onClick={() => setMassUpdateNotesOpen(true)}
                    className={`w-7 h-7 inline-flex items-center justify-center rounded-full text-sm ${theme.button_default}`}
                    title="Mass Update Setting Notes"
                    aria-label="Mass Update Setting Notes"
                  >
                    <i className="fa-solid fa-circle-question" aria-hidden />
                  </button>
                )}
              </div>
            </div>
            {currentSearchView?.IsMassUpdateView && (
              <>
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Update Data Model</label>
                  <ComboBox
                    isRequired={false}
                    itemsSource={transactionCV}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={currentSearchView?.UpdateTransctionId ?? null}
                    selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, UpdateTransctionId: v ?? null, UpdateBaseTranscationUnitId: null } : p)); }}
                    style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                  />
                </div>
                {currentSearchView?.UpdateTransctionId && (
                  <div className="flex items-center gap-3 py-1">
                    <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Update Data Model Unit</label>
                    <ComboBox
                      isRequired={false}
                      itemsSource={massUpdateUnitCV}
                      displayMemberPath="UnitDisplayName"
                      selectedValuePath="Id"
                      selectedValue={currentSearchView?.UpdateBaseTranscationUnitId ?? null}
                      selectedIndexChanged={(s: any) => { const v = s.selectedValue; markChange(); setCurrentSearchView((p: any) => (p ? { ...p, UpdateBaseTranscationUnitId: v ?? null } : p)); }}
                      style={{ width: '13rem', height: '28px', fontSize: '12px' }}
                    />
                  </div>
                )}
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Allow Add Row</label>
                  <div className="w-52 shrink-0 flex items-center">
                    <input type="checkbox" id="allowAddRow" checked={!!currentSearchView?.IsAllowAddRow} onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsAllowAddRow: e.target.checked } : p)); }} />
                  </div>
                </div>
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Allow Delete Row</label>
                  <div className="w-52 shrink-0 flex items-center">
                    <input type="checkbox" id="allowDeleteRow" checked={!!currentSearchView?.IsAllowDeleteRow} onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsAllowDeleteRow: e.target.checked } : p)); }} />
                  </div>
                </div>
                <div className="flex items-center gap-3 py-1">
                  <label className={`w-40 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Allow Advanced Update</label>
                  <div className="w-52 shrink-0 flex items-center">
                    <input type="checkbox" id="allowAdvancedUpdate" checked={!!currentSearchView?.IsAllowUpdateRow} onChange={(e) => { markChange(); setCurrentSearchView((p: any) => (p ? { ...p, IsAllowUpdateRow: e.target.checked } : p)); }} />
                  </div>
                </div>
              </>
            )}
          </div>
        )}
      </div>
      )}

      {massUpdateNotesOpen && (
        <div
          className="fixed inset-0 z-[120] flex items-center justify-center bg-black/40"
          role="dialog"
          aria-modal="true"
          onClick={() => setMassUpdateNotesOpen(false)}
        >
          <div
            className={`max-w-4xl w-full max-h-[80vh] flex flex-col rounded-md overflow-auto shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Mass Update Setting Notes</div>
              <button
                type="button"
                onClick={() => setMassUpdateNotesOpen(false)}
                className={`p-1 rounded ${theme.button_default}`}
                aria-label="Close"
              >
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className="p-4 text-xs">
              <div className="font-semibold mb-2">*Mass Update Setting Notes:</div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <div className="font-semibold">1. If updating single table.</div>
                  <div className="pl-2 mt-1 space-y-1">
                    <div>1) Set Mass Update Type: select &quot;Update Single Table&quot;.</div>
                    <div>2) Set Update Data Model: select a MasterDetail Data Model, which has the update table as root or child unit.</div>
                    <div>3) Set Update Data Model Unit: select the root or child unit related to the update table.</div>
                    <div>4) Search View Field Mapping: Map all the need-to-updated view fields to transaction unit fields, must mapping unit PK.</div>
                    <div>5) The mass update allows create, modify, and delete rows.</div>
                  </div>
                </div>
                <div>
                  <div className="font-semibold">2. If updating 2 hierarchical tables.</div>
                  <div className="pl-2 mt-1 space-y-1">
                    <div>1) Set Mass Update Type: select &quot;Update Hierarchical Tables&quot;.</div>
                    <div>2) Set Update Data Model: select a ListEdit Transaction, which has the hierarchical level of units the same as the update tables.</div>
                    <div>3) Search View Field Mapping: Map only one field: The root table PK field to ListEdit transaction root unit PK field.</div>
                    <div>4) Search result will send PK id list to ListEdit Data Model, to filter the ListEdit Form rows.</div>
                    <div>5) The updating will follow the ListEdit save logic.</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE && !embedded && (
        <div className={`flex shrink-0 items-center gap-1 px-3 py-1.5 border-b ${theme.mainContentSection}`}>
          <button
            type="button"
            onClick={() => setClusterEditorTab('fields')}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${
              clusterEditorTab === 'fields' ? theme.button_default : theme.contextMenu
            }`}
          >
            View Fields
          </button>
          <button
            type="button"
            onClick={() => setClusterEditorTab('layout')}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${
              clusterEditorTab === 'layout' ? theme.button_default : theme.contextMenu
            }`}
          >
            Cluster Layout
          </button>
        </div>
      )}

      {currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE && !embedded && clusterEditorTab === 'layout' && (
        <div className={`w-full h-1 flex-auto flex flex-col min-h-0 overflow-hidden ${theme.mainContentSection}`}>
          <ClusterAnalysisViewLayoutEditor
            flexLayoutItems={currentSearchView?.OtherSettingsDto?.FlexLayoutItems ?? []}
            onFlexLayoutItemsChange={(items) => {
              setCurrentSearchView((p: any) =>
                p
                  ? {
                      ...p,
                      OtherSettingsDto: { ...(p.OtherSettingsDto || {}), FlexLayoutItems: items },
                      IsModified: true,
                    }
                  : p
              );
            }}
            chartTypeList={chartTypeList}
            onRefresh={() => loadData()}
            onSave={() => void save()}
            onPreviewClusterSearchView={previewClusterSearchView}
            viewName={currentSearchView?.Name ?? ''}
            viewDescription={currentSearchView?.Description ?? ''}
            onViewNameChange={(v) => {
              markChange();
              setCurrentSearchView((p: any) => (p ? { ...p, Name: v } : p));
            }}
            onViewDescriptionChange={(v) => {
              markChange();
              setCurrentSearchView((p: any) => (p ? { ...p, Description: v } : p));
            }}
          />
        </div>
      )}

      {/* View Fields section - Calendar/Gantt/Scheduler (different columns) */}
      {(currentSearchView?.ViewType === EmAppViewType.CalendarView || currentSearchView?.ViewType === EmAppViewType.GanttView || currentSearchView?.ViewType === EmAppViewType.SchedulerView) && (
        <div className={`w-full h-1 flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
          <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold ${theme.title}`}>View Fields</div>
          </div>
          <div className="w-full h-1 flex-auto overflow-hidden p-2">
            {searchViewFieldCV && (
              <FlexGrid
                itemsSource={searchViewFieldCV}
                allowDelete={false}
                cellEditEnded={handleCellEditEnded}
                className="w-full h-full"
                headersVisibility="All"
              >
                <FlexGridColumn binding="isRequired" header="Is Required" width={90} dataType="Boolean" isReadOnly />
                <FlexGridColumn binding="DisplayText" header="Event/Task Field" width={200} isReadOnly />
                <FlexGridColumn binding="SysTableFiledPath" header="Mapping To Data Field" width={280} dataMap={dataSetColumnsDataMap} />
                {currentSearchView?.ViewType === EmAppViewType.SchedulerView && <FlexGridColumn binding="IsGroupBy" header="Is Group By" width={100} dataType="Boolean" />}
                {currentSearchView?.ViewType === EmAppViewType.SchedulerView && <FlexGridColumn binding="EntityId" header="Group By Entity" width={150} />}
                <FlexGridColumn binding="" header="" width="*" allowSorting={false} isReadOnly />
              </FlexGrid>
            )}
          </div>
        </div>
      )}

      {/* View Fields section - main content area (Grid, Card, etc.) */}
      {currentSearchView?.ViewType !== EmAppViewType.CalendarView &&
        currentSearchView?.ViewType !== EmAppViewType.GanttView &&
        currentSearchView?.ViewType !== EmAppViewType.SchedulerView &&
        (currentSearchView?.ViewType !== CLUSTER_ANALYSIS_VIEW_TYPE ||
          clusterEditorTab === 'fields' ||
          embedded) && (
        <div className={`w-full h-1 flex-auto flex flex-col overflow-hidden ${theme.mainContentSection}`}>
          <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold ${theme.title}`}>View Fields</div>
            <div className="flex items-center gap-2">
              <div className="relative" ref={addFieldRef}>
                <button type="button" onClick={() => setAddFieldOpen((o) => !o)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                  <i className="fa-solid fa-plus" aria-hidden /> Add <i className="fa-solid fa-caret-down text-xs ml-1" aria-hidden />
                </button>
                {addFieldOpen && (
                  <div className={`absolute left-0 mt-1 min-w-[180px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}>
                    <button type="button" onClick={() => { setAddFieldOpen(false); if (dataSetColumnList.length) setColumnPickerOpen(true); else addSearchViewField(); }} className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}>Add Regular Field</button>
                    <button type="button" onClick={() => { setAddFieldOpen(false); addSearchViewField('', true); }} className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}>Add Calculation Field</button>
                  </div>
                )}
              </div>
              <button type="button" onClick={removeSearchViewField} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-minus" aria-hidden /> Remove
              </button>
              <button type="button" onClick={removeFieldsNotInDataSet} disabled={!dataSetColumnList.length} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}>
                <i className="fa-solid fa-minus" aria-hidden /> Remove All Fields not in Dataset
              </button>
              <div className={`flex items-center gap-1 px-2 py-1 rounded ${theme.contextMenu}`}>
                <input type="checkbox" id="showAdvanced" checked={showAdvancedColumns} onChange={(e) => setShowAdvancedColumns(e.target.checked)} className="mr-1" />
                <label htmlFor="showAdvanced" className="text-xs cursor-pointer">Show Advanced Columns</label>
              </div>
            </div>
          </div>
          <div className="w-full h-1 flex-auto overflow-hidden p-2">
            {searchViewFieldCV && (
              <FlexGrid
                ref={flexGridRef}
                itemsSource={searchViewFieldCV}
                allowDelete={false}
                frozenColumns={2}
                selectionMode="ListBox"
                cellEditEnded={handleCellEditEnded}
                className="w-full h-full"
                headersVisibility="All"
              >
                <FlexGridColumn binding="Sort" header="Sort" width={50} />
                <FlexGridColumn binding="SysTableFiledPath" header="Field Name" width={250} dataMap={dataSetColumnsDataMap} />
                <FlexGridColumn binding="MassUpdateTransactionFieldId" header="Mass Update Mapping To Data Model Field" width={280} dataMap={massUpdateTransFieldDataMap} visible={!!currentSearchView?.IsMassUpdateView} />
                <FlexGridColumn binding="DisplayText" header="Display" width={150} />
                <FlexGridColumn binding="ControlType" header="Control Type" width={100} dataMap={controlTypeDataMap} />
                <FlexGridColumn binding="EntityId" header="Entity" width={220} isReadOnly>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item;
                      if (!item) return null;
                      const isDDL = item.ControlType === CONTROL_TYPE_DDL;
                      const entityDisplay = getEntityCodeById(item.EntityId);
                      return (
                        <div className="flex items-center gap-1 w-full min-w-0">
                          {isDDL && (
                            <button
                              type="button"
                              onClick={(e) => {
                                e.stopPropagation();
                                openEntityInfoPopup(item);
                              }}
                              className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                              title="Select datasource"
                              aria-label="Select datasource"
                            >
                              <i className="fa-solid fa-search" aria-hidden />
                            </button>
                          )}
                          {item.EntityId != null && (
                            <button
                              type="button"
                              onClick={(e) => {
                                e.stopPropagation();
                                openEntityDataEditorPopup(item.EntityId);
                              }}
                              className={`shrink-0 w-7 h-6 flex items-center justify-center rounded ${theme.button_default} text-xs`}
                              title="Entity Data Preview"
                              aria-label="Entity Data Preview"
                            >
                              <i className="fa-solid fa-database" aria-hidden />
                            </button>
                          )}
                          <span className={`truncate text-xs ${theme.label}`} title={entityDisplay}>
                            {entityDisplay || ''}
                          </span>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="EntityId" header="EntityId" width={100} format="d" visible={showAdvancedColumns} />
                <FlexGridColumn binding="IsFilterByCurrentUser" header="Is Filter By Current User" width={150} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsVisible" header="Is Visible" width={90} dataType="Boolean" />
                <FlexGridColumn binding="IsUserDefined3" header="Hide On Card View" width={200} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsUserDefined4" header="Hide Label On Card View" width={200} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.CardView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsGroupBy" header="Is Group By" width={100} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="GroupByLevel" header="Group By Level" width={120} dataType="Number" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="OrderByLevel" header="Order By Level" width={120} dataType="Number" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsDescOrder" header="Is Desc Order" width={120} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsFileFoderId" header="IsFolderId" width={100} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsTransRootId" header="IsTransRootId" width={100} dataType="Boolean" visible={showAdvancedColumns && (currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.WorkflowView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE)} />
                <FlexGridColumn binding="IsPartnerFilterFiled" header="IsPartnerId" width={100} dataType="Boolean" />
                <FlexGridColumn binding="Width" header="Width" width={100} visible={currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === EmAppViewType.RecursiveDataSetTreeView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE} />
                <FlexGridColumn binding="AggregationFunctionType" header="Aggregation Type" width={150} dataMap={aggregationFunctionTypeDataMap} visible={currentSearchView?.ViewType === EmAppViewType.GridView || currentSearchView?.ViewType === EmAppViewType.HierarchyMasterDetailView || currentSearchView?.ViewType === EmAppViewType.EShopOrderListView || currentSearchView?.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE} />
                <FlexGridColumn binding="IsUserDefined1" header="Pivot Row Field" width={140} dataType="Boolean" visible={currentSearchView?.ViewType === EmAppViewType.PivotView} />
                <FlexGridColumn binding="IsUserDefined2" header="Pivot Column Field" width={160} dataType="Boolean" visible={currentSearchView?.ViewType === EmAppViewType.PivotView} />
                <FlexGridColumn binding="IsUserDefined3" header="Pivot Aggregation Field" width={180} dataType="Boolean" visible={currentSearchView?.ViewType === EmAppViewType.PivotView} />
                <FlexGridColumn binding="AggregationFunctionType" header="Aggregation Type (Pivot)" width={200} dataMap={pivotAggregationDataMap} visible={currentSearchView?.ViewType === EmAppViewType.PivotView} />
                <FlexGridColumn binding="IsMapToChartX" header="Is Map To Chart Label" width={150} dataType="Boolean" visible={currentSearchView?.ViewType === EmAppViewType.ChartView} />
                <FlexGridColumn binding="IsMapToChartY" header="Is Map To Chart Value" width={150} dataType="Boolean" visible={currentSearchView?.ViewType === EmAppViewType.ChartView} />
                <FlexGridColumn
                  binding="EmInternalCodeRegistration"
                  header="Internal Code Registration"
                  width={200}
                  dataMap={googleMapInternalCodeRegistrationDataMap ?? undefined}
                  visible={currentSearchView?.ViewType === EmAppViewType.GoogleMapView}
                />
                <FlexGridColumn binding="IsCalulationField" header="Is Calculation Field" width={140} dataType="Boolean" isReadOnly />
                <FlexGridColumn binding="" header="" width="*" allowSorting={false} isReadOnly />
              </FlexGrid>
            )}
          </div>
        </div>
      )}
      </div>

      {/* Column Picker Modal for Add Regular Field - FlexGrid like Angular LookupItemSelector */}
      {columnPickerOpen && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setColumnPickerOpen(false)}>
          <div className={`rounded shadow-xl w-full max-w-lg flex flex-col ${theme.mainContentSection}`} style={{ height: '500px' }} onClick={(e) => e.stopPropagation()}>
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Field List</div>
              <button type="button" onClick={() => setColumnPickerOpen(false)} className="text-lg leading-none">&times;</button>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden p-2" style={{ minHeight: 0 }}>
              {columnPickerCV ? (
                <FlexGrid
                  ref={columnPickerGridRef}
                  itemsSource={columnPickerCV}
                  isReadOnly={true}
                  selectionMode="ListBox"
                  className="w-full h-full"
                  selectionChanged={() => {
                    const flex = columnPickerGridRef.current?.control ?? columnPickerGridRef.current;
                    if (flex?.invalidate) flex.invalidate();
                  }}
                  formatItem={(s: any, e: any) => {
                    if (e.panel.cellType === 0) {
                      const row = e.panel.rows[e.row];
                      const html = row?.isSelected
                        ? '<div style="padding:0 5px;"><i class="fa-solid fa-check" style="color:#808080;"></i></div>'
                        : '<div style="padding:0 5px;"></div>';
                      e.cell.innerHTML = html;
                    }
                  }}
                >
                  <FlexGridFilter />
                  <FlexGridColumn binding="Id" header="Field Name" width="*" />
                  <FlexGridColumn binding="Display" header="Data Type" width={100} />
                </FlexGrid>
              ) : (
                <div className={`text-sm ${theme.label} py-4`}>
                  No dataset columns are available to add.
                </div>
              )}
            </div>
            <div className="flex justify-end gap-2 p-3 border-t">
              <button type="button" onClick={() => setColumnPickerOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
              <button type="button" onClick={handleColumnPickerOk} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Ok</button>
            </div>
          </div>
        </div>
      )}

      {entitySelectionItem != null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true" aria-labelledby="searchview-datasource-selector-title">
          <div className={`max-w-lg w-full max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`} id="searchview-datasource-selector-title">
              <span className="text-md font-semibold">Datasource Selector</span>
              <button type="button" onClick={closeDatasourceSelectorPopup} className={`p-1 rounded ${theme.button_default}`} aria-label="Close">
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-auto p-2">
              <div className={`text-xs mb-2 ${theme.label}`}>Entity Code</div>
              <div className={`border ${theme.inputBox} rounded overflow-hidden`} style={{ minHeight: 200 }}>
                {entityListForFilter.length === 0 ? (
                  <div className="p-3 text-xs">Loading entities...</div>
                ) : (
                  <ul className="divide-y divide-gray-200 dark:divide-gray-600 max-h-[50vh] overflow-auto">
                    {entityListForFilter.map((e) => (
                      <li
                        key={e.Id}
                        role="button"
                        tabIndex={0}
                        onClick={() => setDatasourceSelectorSelectedId(e.Id)}
                        onKeyDown={(ev) => (ev.key === 'Enter' || ev.key === ' ') && setDatasourceSelectorSelectedId(e.Id)}
                        className={`px-3 py-2 text-xs cursor-pointer hover:opacity-80 ${datasourceSelectorSelectedId === e.Id ? theme.mainContentSection : ''}`}
                      >
                        {e.Display ?? e.Name ?? e.Code ?? String(e.Id)}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
            <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={closeDatasourceSelectorPopup} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Close
              </button>
              <button
                type="button"
                onClick={() => datasourceSelectedHandler(datasourceSelectorSelectedId ?? null)}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}

      {isLoading && (
        <div className="absolute inset-0 bg-black/10 flex items-center justify-center z-10">
          <div className="w-12 h-12 border-2 border-t-transparent border-current rounded-full animate-spin" />
        </div>
      )}
    </div>
  );
};

export default SearchViewEditor;
