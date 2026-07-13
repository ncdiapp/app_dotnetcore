/**
 * Search Editor – migrated from AngularJS searchEditorCtrl / SearchEditor.cshtml.
 * Phase 1: Load/save search, Name/Description/Dataset, Views list, Create/Edit/Delete view, Test Run, Save As.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch, useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { setUserMenu } from '../../redux/features/admin/userSessionSlice';
import { closeTab } from '../../redux/features/ui/navigation/tabnavSlice';
import type { RootState } from '../../redux/store';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import appHelper from '../../helper/appHelper';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import SearchFilterEditor from './SearchFilterEditor';
import SearchViewEditor from './SearchViewEditor';
import ClusterAnalysisViewLayoutEditor, {
  CLUSTER_ANALYSIS_VIEW_TYPE,
  createDefaultClusterFlexLayout,
  ensureClusterFlexIdsDeep,
} from './ClusterAnalysisViewLayoutEditor';
import { resolveAppViewTypeDisplay } from './emAppViewTypeEditorOptions';
import DataSetEditor from '../dbmgt/DataSetEditor';

interface SearchEditorParam {
  id?: string | number | null;
  param1?: string | null;
  param2?: string | null;
}

interface ViewItem {
  Id: number;
  Name?: string;
  Description?: string;
  ViewType?: number;
}

function ensureClusterChildWidgetCollectionsDeep(items: any[]): any[] {
  const list = Array.isArray(items) ? items : [];
  const walk = (node: any): any => {
    if (!node || typeof node !== 'object') return node;
    const next = { ...node };
    if (next.DesktopWidget && typeof next.DesktopWidget === 'object' && Number(next.DesktopWidget.WidgetItemType) === 9) {
      const w = { ...next.DesktopWidget };
      if (!Array.isArray(w.AppSearchViewFieldList)) w.AppSearchViewFieldList = [];
      if (!Array.isArray(w.DeletedItemsIds)) w.DeletedItemsIds = [];
      next.DesktopWidget = w;
    }
    if (Array.isArray(next.ChildDesktopItems)) {
      next.ChildDesktopItems = next.ChildDesktopItems.map((c: any) => walk(c));
    }
    return next;
  };
  return list.map((it) => walk(it));
}

function normalizeClusterViewFromApi(viewData: any) {
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
    data.OtherSettingsDto.FlexLayoutItems = ensureClusterChildWidgetCollectionsDeep(
      ensureClusterFlexIdsDeep(data.OtherSettingsDto.FlexLayoutItems)
    );
  }
  return data;
}

function newClusterViewDraft(dataSetId: number, saasApplicationId: number | null) {
  return {
    Name: 'New Cluster Analysis View',
    Description: '',
    DataSetId: dataSetId,
    ViewType: CLUSTER_ANALYSIS_VIEW_TYPE,
    AppSearchViewFieldList: [],
    DeletedItemsIds: [],
    OtherSettingsDto: { FlexLayoutItems: createDefaultClusterFlexLayout() },
    IsModified: false,
    SaasApplicationId: saasApplicationId,
  };
}

interface DataSetItem {
  Id: number;
  Name?: string;
  DataSourceFrom?: number | null;
}

const PAGE_TITLE = 'Report & View Editor';
// Match Angular EmAppDataServiceType (subset used here)
const EmAppDataServiceType = {
  QueryText: 1,
  StoredProcedure: 2,
  PluginWebApiCall: 3,
  IntegrationWebApiCall: 4
} as const;

/** Default view type for auto-created search view (GridView). */
const WIZARD_DEFAULT_VIEW_TYPE = 1;

type NewSearchWizardPhase = 'off' | 'report_name' | 'dataset' | 'saving';

export type SearchEditorProps = {
  /** When true, do not read route param; use `searchId` prop instead. */
  ignoreRouteParam?: boolean;
  /** Search Id to load when `ignoreRouteParam` is true. */
  searchId?: number | null;
  /** Optional close handler for embedded/popup usage. */
  onClose?: () => void;
};

const SearchEditor: React.FC<SearchEditorProps> = ({ ignoreRouteParam = false, searchId = null, onClose }) => {
  const { param } = useParams<{ param: string }>();
  const navigate = useNavigate();
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((state: RootState) => state.tabnav.previousActiveTabKey);
  const { showError, showWarning, showInfo } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const [paramObj, setParamObj] = useState<SearchEditorParam>({});
  const [currentSearch, setCurrentSearch] = useState<any>({
    Name: '',
    Description: '',
    DataSetId: null,
    SearchViewId: null,
    AppSearchFieldList: [],
    DeletedItemsIds: [],
    Type: 1,
    SaasApplicationId: null,
    IsAutoExecute: true,
    IsModified: false
  });
  const [currentDataSet, setCurrentDataSet] = useState<DataSetItem | null>(null);
  const [allDataSet, setAllDataSet] = useState<DataSetItem[]>([]);
  const [searchViewList, setSearchViewList] = useState<ViewItem[]>([]);
  const [selectedViewId, setSelectedViewId] = useState<number | null>(null);
  const [creatingViewMode, setCreatingViewMode] = useState(false);
  const [creatingViewSeed, setCreatingViewSeed] = useState(0);
  /** When creating from "Cluster View", embedded editor seeds ViewType 25 and default flex layout. */
  const [pendingNewViewType, setPendingNewViewType] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [advancedOptionsOpen, setAdvancedOptionsOpen] = useState(false);
  const [dataSetColumnsList, setDataSetColumnsList] = useState<any[]>([]);
  const [whereUsedSearchList, setWhereUsedSearchList] = useState<any[]>([]);
  const [entityListForFilter, setEntityListForFilter] = useState<{ Id: number; Display?: string; Name?: string; Code?: string }[]>([]);
  const [entitySelectionItem, setEntitySelectionItem] = useState<any | null>(null);
  const [datasourceSelectorSelectedId, setDatasourceSelectorSelectedId] = useState<number | null>(null);
  const [cascadingConfig, setCascadingConfig] = useState<any | null>(null);
  const [innerRelationConfig, setInnerRelationConfig] = useState<any | null>(null);
  const [ddlFieldLookupList, setDdlFieldLookupList] = useState<{ Id: number; Display: string }[]>([]);
  const [databaseSchemaData, setDatabaseSchemaData] = useState<any[]>([]);
  const [isLoadingCascadingSchema, setIsLoadingCascadingSchema] = useState(false);
  const [relationTableColumns, setRelationTableColumns] = useState<string[]>([]);
  const [innerMasterFieldOptions, setInnerMasterFieldOptions] = useState<{ Id: number; Display: string }[]>([]);
  const [subscribeInnerEntityColumns, setSubscribeInnerEntityColumns] = useState<{ Display: string }[]>([]);
  const [dictSearchFieldById, setDictSearchFieldById] = useState<Record<string, any>>({});
  const [apiMenuOpen, setApiMenuOpen] = useState(false);
  const apiMenuRef = useRef<HTMLDivElement | null>(null);
  /** Sub-tabs when dataset is set: Filters vs Report Views (full-height each). */
  const [editorSubTab, setEditorSubTab] = useState<'filters' | 'views'>('views');
  /** Views list: which row's action menu is open (chevron). */
  const [viewRowMenuOpenId, setViewRowMenuOpenId] = useState<number | null>(null);
  const viewRowMenuWrapRef = useRef<HTMLDivElement | null>(null);

  /** New Report wizard (no saved Id, no initial dataset in URL). */
  const [newSearchWizardPhase, setNewSearchWizardPhase] = useState<NewSearchWizardPhase>('off');
  const [wizardDraftName, setWizardDraftName] = useState('');
  const [wizardDatasetPickerOpen, setWizardDatasetPickerOpen] = useState(false);
  const [wizardDatasetPreviewId, setWizardDatasetPreviewId] = useState<number | null>(null);
  const [wizardCreateDatasetOpen, setWizardCreateDatasetOpen] = useState(false);
  const wizardBootstrappedRef = useRef(false);
  const [filterColumnPickerOpen, setFilterColumnPickerOpen] = useState(false);
  const filterColumnPickerGridRef = useRef<any>(null);
  const activeViewSaveActionRef = useRef<(() => Promise<boolean>) | null>(null);
  const suppressViewSavedAutoSelectRef = useRef(false);

  /** Resolved view type for the selected list row (API list may omit ViewType). */
  const [selectedSearchMeta, setSelectedSearchMeta] = useState<{ id: number; viewType: number } | null>(null);
  const [clusterViewState, setClusterViewState] = useState<any | null>(null);
  const [clusterDesignPreviewRows, setClusterDesignPreviewRows] = useState<any[]>([]);

  const emAppViewType = useEnumValues('EmAppViewType');
  const emAppSearchUsageType = useEnumValues('EmAppSearchUsageType');
  const emAppControlType = useEnumValues('EmAppControlType');
  const emAppCriteriaOperatorType = useEnumValues('EmAppCriteriaOperatorType');
  const emAppSearchFieldSubControlType = useEnumValues('EmAppSearchFieldSubControlType');
  const emInternalCodeRegistration = useEnumValues('EmInternalCodeRegistration');
  const emAppChartViewType = useEnumValues('EmAppChartViewType');
  const chartTypeListForCluster = React.useMemo(
    () => (emAppChartViewType ? Object.entries(emAppChartViewType).map(([k, id]) => ({ Id: id, Display: k })) : []),
    [emAppChartViewType]
  );

  const searchTypeList = React.useMemo(() => {
    if (!emAppSearchUsageType) return [];
    return Object.entries(emAppSearchUsageType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppSearchUsageType]);
  const controlTypeList = React.useMemo(() => {
    if (!emAppControlType) return [];
    return Object.entries(emAppControlType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppControlType]);
  const criteriaOperatorList = React.useMemo(() => {
    if (!emAppCriteriaOperatorType) return [];
    return Object.entries(emAppCriteriaOperatorType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppCriteriaOperatorType]);

  const searchTypeDataMap = React.useMemo(() => new DataMap(searchTypeList, 'Id', 'Display'), [searchTypeList]);
  const controlTypeDataMap = React.useMemo(() => new DataMap(controlTypeList, 'Id', 'Display'), [controlTypeList]);
  const criteriaOperatorDataMap = React.useMemo(() => new DataMap(criteriaOperatorList, 'Id', 'Display'), [criteriaOperatorList]);
  const dataSetColumnsDataMap = React.useMemo(() => new DataMap(dataSetColumnsList, 'Id', 'Id'), [dataSetColumnsList]);
  const subControlTypeList = useMemo(() => (emAppSearchFieldSubControlType ? Object.entries(emAppSearchFieldSubControlType).map(([key, id]) => ({ Id: id, Display: key })) : []), [emAppSearchFieldSubControlType]);
  const internalCodeList = useMemo(() => (emInternalCodeRegistration ? Object.entries(emInternalCodeRegistration).map(([key, id]) => ({ Id: id, Display: key })) : []), [emInternalCodeRegistration]);
  const subControlTypeDataMap = useMemo(() => (subControlTypeList.length ? new DataMap(subControlTypeList, 'Id', 'Display') : null), [subControlTypeList]);
  const internalCodeDataMap = useMemo(() => (internalCodeList.length ? new DataMap(internalCodeList, 'Id', 'Display') : null), [internalCodeList]);

  const controlTypeIds = React.useMemo(() => ({
    DDL: emAppControlType?.DDL ?? 1,
    AutoComplete: emAppControlType?.AutoComplete,
    Date: emAppControlType?.Date,
    Time: emAppControlType?.Time,
    DateTimeDetail: emAppControlType?.DateTimeDetail
  }), [emAppControlType]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (apiMenuRef.current && !apiMenuRef.current.contains(e.target as Node)) {
        setApiMenuOpen(false);
      }
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  // Load dataset columns when DataSetId is set
  useEffect(() => {
    if (!currentSearch?.DataSetId) {
      setDataSetColumnsList([]);
      return;
    }
    searchSvc.retrieveQueryColumnList(String(currentSearch.DataSetId)).then((cols) => {
      setDataSetColumnsList(Array.isArray(cols) ? cols : []);
    }).catch(() => setDataSetColumnsList([]));
  }, [currentSearch?.DataSetId]);

  // Build dictSearchFieldIdAndObj and ddlFieldLookupList whenever filter list changes
  useEffect(() => {
    const list = currentSearch?.AppSearchFieldList ?? [];
    const dict: Record<string, any> = {};
    const ddlList: { Id: number; Display: string }[] = [];
    list.forEach((f: any) => {
      if (f.Id != null) {
        dict[String(f.Id)] = f;
        if (f.ControlType === controlTypeIds.DDL) {
          ddlList.push({
            Id: f.Id,
            Display: f.SysTableFiledPath || f.DisplayText || String(f.Id)
          });
        }
      }
    });
    setDictSearchFieldById(dict);
    setDdlFieldLookupList(ddlList);
  }, [currentSearch?.AppSearchFieldList, controlTypeIds.DDL]);

  // Load AppSearch list for Embedded Child Search
  useEffect(() => {
    adminSvc.getMassEntitiesLookupItem('AppSearch').then((data: any) => {
      const list = data?.AppSearch ?? [];
      setWhereUsedSearchList(Array.isArray(list) ? list : []);
    }).catch(() => setWhereUsedSearchList([]));
  }, []);

  // Load full entity list for Filters / Datasource Selector (includes imported PLM entities)
  useEffect(() => {
    if (!currentSearch?.SearchViewId && !currentSearch?.DataSetId) return;
    adminSvc.retrieveAllAppEntityInfoDto(false).then((data: any) => {
      const arr = Array.isArray(data) ? data : [];
      setEntityListForFilter(
        arr
          .map((e: any) => {
            const idNum = e.Id != null ? Number(e.Id) : NaN;
            return {
              Id: Number.isFinite(idNum) ? idNum : e.Id,
              Display: e.EntityCode ?? e.Display ?? e.Name ?? e.Code ?? (Number.isFinite(idNum) ? String(idNum) : String(e.Id ?? '')),
            };
          })
          .filter((e: any) => e.Id != null && e.Id !== '')
          .sort((a: any, b: any) => String(a.Display ?? '').localeCompare(String(b.Display ?? ''), undefined, { sensitivity: 'base' }))
      );
    }).catch(() => setEntityListForFilter([]));
  }, [currentSearch?.SearchViewId, currentSearch?.DataSetId]);

  useEffect(() => {
    if (ignoreRouteParam) {
      setParamObj({ id: searchId != null ? Number(searchId) : null });
      return;
    }
    let p: SearchEditorParam = {};
    if (param) {
      try {
        p = JSON.parse(decodeURIComponent(param));
      } catch {
        p = { id: param };
      }
    }
    setParamObj(p);
  }, [param, ignoreRouteParam, searchId]);

  const markChange = useCallback(() => {
    setCurrentSearch((prev: any) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const getNextFilterPosition = useCallback((list: any[]) => {
    const maxLinearIndex = (list ?? []).reduce((acc: number, f: any) => {
      const row = Number(f?.PositionRow ?? 0);
      const col = Number(f?.PositionColumn ?? 0);
      if (!Number.isFinite(row) || !Number.isFinite(col) || row <= 0 || col <= 0) return acc;
      const linear = (row - 1) * 4 + (col - 1);
      return Math.max(acc, linear);
    }, -1);
    const nextLinear = maxLinearIndex + 1;
    return {
      PositionRow: Math.floor(nextLinear / 4) + 1,
      PositionColumn: (nextLinear % 4) + 1
    };
  }, []);

  const addSearchFilter = useCallback(() => {
    if (dataSetColumnsList.length > 0) {
      setFilterColumnPickerOpen(true);
      return;
    }
    const list = [...(currentSearch?.AppSearchFieldList ?? [])];
    const sort = Math.max(0, ...list.map((f: any) => (f.Sort ?? 0))) + 1;
    const nextPos = getNextFilterPosition(list);
    list.push({
      Sort: sort,
      SysTableFiledPath: null,
      DisplayText: '',
      ControlType: 2,
      EntityId: null,
      OperationId: 0,
      DefaultValue: '',
      PositionRow: nextPos.PositionRow,
      PositionColumn: nextPos.PositionColumn,
      IsVisible: true,
      IsReadOnly: false
    });
    markChange();
    setCurrentSearch((p: any) => (p ? { ...p, AppSearchFieldList: list } : p));
  }, [currentSearch?.AppSearchFieldList, markChange, dataSetColumnsList.length, getNextFilterPosition]);

  const filterColumnPickerAvailableList = useMemo(() => {
    const existing = new Set(
      (currentSearch?.AppSearchFieldList ?? [])
        .map((f: any) => f?.SysTableFiledPath)
        .filter(Boolean)
        .map((x: any) => String(x))
    );
    return (dataSetColumnsList ?? []).filter((c: any) => {
      const id = c?.Id ?? c?.ColumnId ?? String(c);
      return !existing.has(String(id));
    });
  }, [dataSetColumnsList, currentSearch?.AppSearchFieldList]);

  const filterColumnPickerCV = useMemo(() => {
    if (!filterColumnPickerAvailableList.length) return null;
    return new CollectionView(filterColumnPickerAvailableList);
  }, [filterColumnPickerAvailableList]);

  const addSearchFiltersFromColumns = useCallback((columnIds: string[]) => {
    if (!columnIds.length) return;
    const list = [...(currentSearch?.AppSearchFieldList ?? [])];
    let sort = Math.max(0, ...list.map((f: any) => (f.Sort ?? 0)));
    let nextPos = getNextFilterPosition(list);
    columnIds.forEach((id) => {
      sort += 1;
      list.push({
        Sort: sort,
        SysTableFiledPath: id,
        DisplayText: id,
        ControlType: 2,
        EntityId: null,
        OperationId: 0,
        DefaultValue: '',
        PositionRow: nextPos.PositionRow,
        PositionColumn: nextPos.PositionColumn,
        IsVisible: true,
        IsReadOnly: false
      });
      const currentLinear = (nextPos.PositionRow - 1) * 4 + (nextPos.PositionColumn - 1);
      const followingLinear = currentLinear + 1;
      nextPos = {
        PositionRow: Math.floor(followingLinear / 4) + 1,
        PositionColumn: (followingLinear % 4) + 1
      };
    });
    markChange();
    setCurrentSearch((p: any) => (p ? { ...p, AppSearchFieldList: list } : p));
  }, [currentSearch?.AppSearchFieldList, markChange, getNextFilterPosition]);

  const handleFilterColumnPickerOk = useCallback(() => {
    const flex = filterColumnPickerGridRef.current?.control ?? filterColumnPickerGridRef.current;
    if (!flex?.selectedRows?.length) {
      showWarning('Please select at least one field');
      return;
    }
    const selectedSet = new Set(
      flex.selectedRows
        .map((r: any) => {
          const item = r.dataItem;
          return String(item?.Id ?? item?.ColumnId ?? item);
        })
        .filter(Boolean)
    );
    const orderedIds = filterColumnPickerAvailableList
      .map((c: any) => String(c?.Id ?? c?.ColumnId ?? c))
      .filter((id: string) => selectedSet.has(id));
    addSearchFiltersFromColumns(orderedIds);
    setFilterColumnPickerOpen(false);
  }, [addSearchFiltersFromColumns, filterColumnPickerAvailableList, showWarning]);

  const removeSearchFilter = useCallback((item: any) => {
    const list = (currentSearch?.AppSearchFieldList ?? []).filter((f: any) => f !== item);
    const prevDeleted = [...(currentSearch?.DeletedItemsIds ?? [])];
    if (item?.Id != null && item.Id !== '') {
      const idStr = String(item.Id);
      if (!prevDeleted.some((x: any) => String(x) === idStr)) {
        prevDeleted.push(item.Id);
      }
    }
    markChange();
    setCurrentSearch((p: any) =>
      p
        ? {
            ...p,
            AppSearchFieldList: list,
            DeletedItemsIds: prevDeleted,
          }
        : p
    );
  }, [currentSearch?.AppSearchFieldList, currentSearch?.DeletedItemsIds, markChange]);

  const getEntityCodeById = useCallback((entityId: number | null | undefined) => {
    if (entityId == null) return '';
    const idNum = Number(entityId);
    if (!Number.isFinite(idNum)) return String(entityId);
    const found = entityListForFilter.find((e) => Number(e.Id) === idNum);
    return found?.Display ?? found?.Name ?? found?.Code ?? String(idNum);
  }, [entityListForFilter]);

  const openEntityInfoPopup = useCallback((item: any) => {
    if (item?.ControlType === 1) {
      setEntitySelectionItem(item);
      setDatasourceSelectorSelectedId(item?.EntityId ?? null);
    }
  }, []);

  const openEntityDataEditorPopup = useCallback((entityId: number | null | undefined) => {
    if (entityId != null) addTabAndNavigate('entity-data-preview', 'Entity Data Preview', { id: String(entityId) }, true);
  }, [addTabAndNavigate]);

  const datasourceSelectedHandler = useCallback((entityId: number | null) => {
    if (entitySelectionItem) {
      entitySelectionItem.EntityId = entityId;
      entitySelectionItem.IsModified = true;
      markChange();
      setCurrentSearch((p: any) => (p ? { ...p, AppSearchFieldList: [...(p.AppSearchFieldList ?? [])], IsModified: true } : p));
    }
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
  }, [entitySelectionItem, markChange]);

  const closeDatasourceSelectorPopup = useCallback(() => {
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
  }, []);

  const getSearchFieldNameById = useCallback((searchFieldId: number | null | undefined) => {
    if (searchFieldId == null) return '';
    const fieldObj = dictSearchFieldById[String(searchFieldId)];
    if (!fieldObj) return '';
    return fieldObj.SysTableFiledPath || fieldObj.DisplayText || String(searchFieldId);
  }, [dictSearchFieldById]);

  const emAppDateTimePropertiesList = useMemo(() => ([
    { Id: 1, Display: 'FullDate' },
    { Id: 2, Display: 'YearNumber' },
    { Id: 3, Display: 'MonthNumber' },
    { Id: 4, Display: 'DayOfMonthNumber' },
    { Id: 5, Display: 'DayOfWeekNumber' },
    { Id: 6, Display: 'MonthName' },
    { Id: 7, Display: 'DayOfWeekName' }
  ]), []);

  const buildSubscribeColumnsForMasterField = useCallback(async (masterField: any) => {
    if (!masterField) {
      setSubscribeInnerEntityColumns([]);
      return;
    }
    if (masterField.ControlType === controlTypeIds.DDL || masterField.ControlType === controlTypeIds.AutoComplete) {
      const parentEntityId = masterField.EntityId;
      if (parentEntityId) {
        const entityData = await adminSvc.retrieveOneAppEntityInfoExDto(String(parentEntityId), true);
        if (entityData?.TableName) {
          const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
            entityData.TableName,
            entityData.DataSourceFrom ?? null,
            entityData.SchemaOwner ?? null
          );
          const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
          setSubscribeInnerEntityColumns(cols.map((c: any) => ({ Display: c.Name })));
          return;
        }
      }
      setSubscribeInnerEntityColumns([]);
    } else if (
      masterField.ControlType === controlTypeIds.Date ||
      masterField.ControlType === controlTypeIds.Time ||
      masterField.ControlType === controlTypeIds.DateTimeDetail
    ) {
      setSubscribeInnerEntityColumns(emAppDateTimePropertiesList.map((p) => ({ Display: p.Display })));
    } else {
      setSubscribeInnerEntityColumns([]);
    }
  }, [controlTypeIds.DDL, controlTypeIds.AutoComplete, controlTypeIds.Date, controlTypeIds.Time, controlTypeIds.DateTimeDetail, emAppDateTimePropertiesList]);

  const openCascadingConfigPopup = useCallback(async (item: any) => {
    if (!item || item.ControlType !== controlTypeIds.DDL || !currentDataSet) return;
    try {
      let tableList = databaseSchemaData;
      // Load table/view list on demand when user opens cascading popup.
      if (!tableList.length) {
        setIsLoadingCascadingSchema(true);
        tableList = await loadDatabaseSchemaData(currentDataSet);
      }
      let relationTableObj: any = null;
      if (item.CascadingRelationTable) {
        relationTableObj = tableList.find((t: any) => t.Name === item.CascadingRelationTable) ?? null;
      }

      let columns: string[] = [];
      if (relationTableObj?.Name) {
        const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
          relationTableObj.Name,
          currentDataSet.DataSourceFrom ?? null,
          relationTableObj.SchemaOwner ?? null
        );
        const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
        columns = cols.map((c: any) => c.Name);
      }
      setRelationTableColumns(columns);
      setCascadingConfig({
        currentField: item,
        ParentFieldId: item.ParentFieldId ?? null,
        relationTableObj,
        CascadingRelationTableParentKeyField: item.CascadingRelationTableParentKeyField ?? '',
        CascadingRelationTableChildKeyField: item.CascadingRelationTableChildKeyField ?? ''
      });
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load cascading config');
    } finally {
      setIsLoadingCascadingSchema(false);
    }
  }, [controlTypeIds.DDL, currentDataSet, databaseSchemaData, showError]);

  const applyCascadingConfig = useCallback(() => {
    if (!cascadingConfig) return;
    const { currentField, ParentFieldId, relationTableObj, CascadingRelationTableParentKeyField, CascadingRelationTableChildKeyField } = cascadingConfig;
    markChange();
    setCurrentSearch((prev: any) => {
      if (!prev) return prev;
      const list = [...(prev.AppSearchFieldList ?? [])];
      const idx = list.indexOf(currentField);
      if (idx >= 0) {
        list[idx] = {
          ...list[idx],
          ParentFieldId: ParentFieldId || null,
          CascadingRelationTable: relationTableObj?.Name ?? null,
          CascadingRelationTableParentKeyField: CascadingRelationTableParentKeyField || null,
          CascadingRelationTableChildKeyField: CascadingRelationTableChildKeyField || null,
          IsModified: true
        };
      }
      return { ...prev, AppSearchFieldList: list, IsModified: true };
    });
    setCascadingConfig(null);
  }, [cascadingConfig, markChange]);

  const openInnerRelationPopup = useCallback(async (item: any) => {
    if (!item) return;
    try {
      const list = (currentSearch?.AppSearchFieldList ?? []).filter((f: any) =>
        f.Id !== item.Id &&
        (f.ControlType === controlTypeIds.DDL ||
          f.ControlType === controlTypeIds.AutoComplete ||
          f.ControlType === controlTypeIds.Date ||
          f.ControlType === controlTypeIds.Time ||
          f.ControlType === controlTypeIds.DateTimeDetail)
      ).map((f: any) => ({
        Id: f.Id,
        Display: f.DisplayText || f.SysTableFiledPath || String(f.Id)
      }));
      setInnerMasterFieldOptions(list);

      const config: any = {
        currentField: item,
        MasterEntityFieldlId: item.MasterEntityFieldlId ?? null,
        InnerEntitySubscribeFiled: item.InnerEntitySubscribeFiled ?? ''
      };

      setInnerRelationConfig(config);

      if (item.MasterEntityFieldlId != null) {
        const masterField = dictSearchFieldById[String(item.MasterEntityFieldlId)];
        if (masterField) {
          await buildSubscribeColumnsForMasterField(masterField);
        } else {
          setSubscribeInnerEntityColumns([]);
        }
      } else {
        setSubscribeInnerEntityColumns([]);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load inner relationship');
    }
  }, [buildSubscribeColumnsForMasterField, controlTypeIds.AutoComplete, controlTypeIds.DDL, controlTypeIds.Date, controlTypeIds.Time, controlTypeIds.DateTimeDetail, currentSearch?.AppSearchFieldList, dictSearchFieldById, showError]);

  const handleInnerRelationMasterFieldChange = useCallback(async (masterFieldId: number | null) => {
    setInnerRelationConfig((prev: any) => (prev ? { ...prev, MasterEntityFieldlId: masterFieldId, InnerEntitySubscribeFiled: '' } : prev));
    if (masterFieldId == null) {
      setSubscribeInnerEntityColumns([]);
      return;
    }
    const masterField = dictSearchFieldById[String(masterFieldId)];
    await buildSubscribeColumnsForMasterField(masterField);
  }, [buildSubscribeColumnsForMasterField, dictSearchFieldById]);

  const applyInnerRelationConfig = useCallback(() => {
    if (!innerRelationConfig) return;
    const { currentField, MasterEntityFieldlId, InnerEntitySubscribeFiled } = innerRelationConfig;
    markChange();
    setCurrentSearch((prev: any) => {
      if (!prev) return prev;
      const list = [...(prev.AppSearchFieldList ?? [])];
      const idx = list.indexOf(currentField);
      if (idx >= 0) {
        list[idx] = {
          ...list[idx],
          MasterEntityFieldlId: MasterEntityFieldlId ?? null,
          InnerEntitySubscribeFiled: InnerEntitySubscribeFiled || null,
          IsModified: true
        };
      }
      return { ...prev, AppSearchFieldList: list, IsModified: true };
    });
    setInnerRelationConfig(null);
  }, [innerRelationConfig, markChange]);

  const clearInnerRelationConfig = useCallback(() => {
    if (!innerRelationConfig) {
      setInnerRelationConfig(null);
      return;
    }
    const { currentField } = innerRelationConfig;
    markChange();
    setCurrentSearch((prev: any) => {
      if (!prev) return prev;
      const list = [...(prev.AppSearchFieldList ?? [])];
      const idx = list.indexOf(currentField);
      if (idx >= 0) {
        list[idx] = {
          ...list[idx],
          MasterEntityFieldlId: null,
          InnerEntitySubscribeFiled: null,
          IsModified: true
        };
      }
      return { ...prev, AppSearchFieldList: list, IsModified: true };
    });
    setInnerRelationConfig(null);
  }, [innerRelationConfig, markChange]);

  const loadViewsForDataSet = useCallback(async (dataSetId: number): Promise<ViewItem[]> => {
    try {
      const list = await searchSvc.retrieveStatciSearchAvailableViewWithSameQueryBL(String(dataSetId));
      const normalized = Array.isArray(list) ? list : [];
      setSearchViewList(normalized);
      return normalized;
    } catch (e) {
      appHelper.debugLog('SearchEditor loadViewsForDataSet error', e);
      setSearchViewList([]);
      return [];
    }
  }, []);

  const loadDatabaseSchemaData = useCallback(async (dataSet: any): Promise<any[]> => {
    if (!dataSet || !dataSet.DataSourceFrom) {
      setDatabaseSchemaData([]);
      return [];
    }
    try {
      const list = await schemaMetadataService.getDataSourceTableAndViewListFromCache(
        dataSet.DataSourceFrom,
        null,
        null
      );
      const decorated = (Array.isArray(list) ? list : []).map((t: any) => ({
        ...t,
        Display: t.SchemaOwner ? `${t.SchemaOwner}.${t.Name}` : t.Name
      }));
      setDatabaseSchemaData(decorated);
      return decorated;
    } catch (e) {
      appHelper.debugLog('SearchEditor loadDatabaseSchemaData error', e);
      setDatabaseSchemaData([]);
      return [];
    }
  }, []);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    setIsLoading(true);
    try {
      const [allDataSets, dataSourceRegisterList] = await Promise.all([
        searchSvc.retrieveAllAppDataSetEntityDto(),
        adminSvc.getDataSourceRegisterList(false)
      ]);
      const allList = Array.isArray(allDataSets) ? allDataSets : [];
      setAllDataSet(allList);

      const effectiveSearchId =
        ignoreRouteParam ? (searchId != null ? String(searchId) : null) : (paramObj.id != null ? String(paramObj.id) : null);
      const applicationId = paramObj.param1 ?? null;
      let param2Obj: any = {};
      if (paramObj.param2) {
        try {
          param2Obj = typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
        } catch { /* ignore */ }
      }

      if (effectiveSearchId) {
        const searchData = await searchSvc.retrieveOneAppSearchExDto(effectiveSearchId);
      if (searchData) {
          if (searchData.DataSetId) {
            await loadViewsForDataSet(searchData.DataSetId);
            const ds = allList.find((d: any) => d.Id === searchData.DataSetId) ?? null;
            setCurrentDataSet(ds ?? null);
            setDatabaseSchemaData([]);
            setRelationTableColumns([]);
          } else {
            setSearchViewList([]);
            setCurrentDataSet(null);
            setDatabaseSchemaData([]);
            setRelationTableColumns([]);
          }
          setCurrentSearch({
            ...searchData,
            AppSearchFieldList: searchData.AppSearchFieldList || [],
            DeletedItemsIds: searchData.DeletedItemsIds || [],
            IsModified: false
          });
        }
      } else {
        setCurrentSearch({
          Name: param2Obj.initialSearchName || '',
          Description: '',
          DataSetId: param2Obj.initialDataSetId ?? null,
          SearchViewId: param2Obj.initialDefualtSearchViewId ?? null,
          AppSearchFieldList: [],
          DeletedItemsIds: [],
          Type: 1,
          SaasApplicationId: applicationId,
          IsAutoExecute: true,
          IsModified: false
        });
        if (param2Obj.initialDataSetId) {
          await loadViewsForDataSet(param2Obj.initialDataSetId);
          const ds = allList.find((d: any) => d.Id === param2Obj.initialDataSetId);
          setCurrentDataSet(ds ?? null);
          setDatabaseSchemaData([]);
          setRelationTableColumns([]);
        } else {
          setSearchViewList([]);
          setCurrentDataSet(null);
          setDatabaseSchemaData([]);
          setRelationTableColumns([]);
        }
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [
    ignoreRouteParam,
    searchId,
    paramObj.id,
    paramObj.param1,
    paramObj.param2,
    dispatch,
    showError,
    loadViewsForDataSet,
    loadDatabaseSchemaData,
  ]);

  const completeWizardWithDataset = useCallback(
    async (dataSetId: number) => {
      const reportName = (wizardDraftName || currentSearch?.Name || '').trim();
      if (!reportName) {
        showWarning('Enter a report name.');
        setNewSearchWizardPhase('report_name');
        return;
      }
      setNewSearchWizardPhase('saving');
      dispatch(setIsBusy());
      try {
        let allList: any[] = allDataSet;
        try {
          const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
          allList = Array.isArray(raw) ? raw : [];
          setAllDataSet(allList);
        } catch {
          /* keep cached list */
        }

        const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));
        let viewId: number | null = null;
        let lastViewValidationError: string | null = null;

        // Dataset metadata/views might not be immediately available right after creation.
        // Retry view resolution/creation a few times before giving up.
        for (let attempt = 0; attempt < 3; attempt++) {
          const rawViews = await searchSvc.retrieveStatciSearchAvailableViewWithSameQueryBL(String(dataSetId));
          const views = Array.isArray(rawViews) ? rawViews : [];
          if (views.length > 0) {
            viewId = views[0].Id;
            break;
          }

          const viewPayload: any = {
            Name: reportName,
            Description: '',
            DataSetId: dataSetId,
            ViewType: WIZARD_DEFAULT_VIEW_TYPE,
            AppSearchViewFieldList: [],
            DeletedItemsIds: [],
            IsModified: true,
            SaasApplicationId: currentSearch?.SaasApplicationId ?? null
          };
          const vd = await searchSvc.saveAppSearchViewExDto(viewPayload);
          if (vd?.ValidationResult?.IsValid === false) {
            const msgs = vd?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
            lastViewValidationError = msgs.length ? msgs.join('; ') : 'Failed to save default view';
          } else {
            viewId = vd?.Id != null ? Number(vd.Id) : null;
          }

          if (viewId != null) break;
          if (attempt < 2) await sleep(800 * (attempt + 1));
        }

        if (viewId == null) {
          showError(lastViewValidationError || 'Failed to create default view.');
          setNewSearchWizardPhase('dataset');
          return;
        }

        const searchPayload = {
          ...currentSearch,
          Name: reportName,
          DataSetId: dataSetId,
          SearchViewId: viewId,
          AppSearchFieldList: currentSearch?.AppSearchFieldList || [],
          DeletedItemsIds: currentSearch?.DeletedItemsIds || [],
          IsModified: true
        };
        const sd = await searchSvc.saveAppSearchExDto(searchPayload);
        if (sd?.ValidationResult?.IsValid === false) {
          const msgs = sd?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
          showError(msgs.length ? msgs.join('; ') : 'Failed to save search');
          setNewSearchWizardPhase('dataset');
          return;
        }
        const newSearchId = sd?.Id ?? sd?.Object?.Id;
        setParamObj((p) => ({ ...p, id: newSearchId ?? p.id }));
        let searchData: any = sd?.Object ?? null;
        if (newSearchId && (!searchData || !Array.isArray(searchData.AppSearchFieldList))) {
          try {
            searchData = await searchSvc.retrieveOneAppSearchExDto(String(newSearchId));
          } catch {
            searchData = null;
          }
        }
        if (searchData) {
          setCurrentSearch({
            ...searchData,
            AppSearchFieldList: searchData.AppSearchFieldList || [],
            DeletedItemsIds: searchData.DeletedItemsIds || [],
            IsModified: false
          });
        } else if (newSearchId) {
          setCurrentSearch((prev: any) =>
            prev
              ? {
                  ...prev,
                  Id: newSearchId,
                  Name: reportName,
                  DataSetId: dataSetId,
                  SearchViewId: viewId,
                  IsModified: false
                }
              : prev
          );
        }
        const ds = allList.find((d: any) => d.Id === dataSetId) ?? null;
        setCurrentDataSet(ds);
        setDatabaseSchemaData([]);
        setRelationTableColumns([]);
        await loadViewsForDataSet(dataSetId);
        setNewSearchWizardPhase('off');
        wizardBootstrappedRef.current = false;
        showInfo('Report saved');
      } catch (e) {
        showError(e instanceof Error ? e.message : 'Failed to complete setup');
        setNewSearchWizardPhase('dataset');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [
      wizardDraftName,
      currentSearch,
      allDataSet,
      dispatch,
      showError,
      showInfo,
      showWarning,
      loadDatabaseSchemaData,
      loadViewsForDataSet
    ]
  );

  const handleWizardNameNext = useCallback(() => {
    const trimmed = wizardDraftName.trim();
    if (!trimmed) {
      showWarning('Enter a report name.');
      return;
    }
    setCurrentSearch((p: any) => (p ? { ...p, Name: trimmed } : p));
    setNewSearchWizardPhase('dataset');
  }, [wizardDraftName, showWarning]);

  const handleWizardCancelClose = useCallback(() => {
    setWizardDatasetPickerOpen(false);
    setWizardDatasetPreviewId(null);
    setWizardCreateDatasetOpen(false);
    if (ignoreRouteParam) {
      onClose?.();
      return;
    }
    if (activeTabKey == null) {
      navigate('/home');
      return;
    }
    const remainingTabs = tabs.filter((t) => t.tabKey !== activeTabKey);
    const prevActive = previousActiveTabKey
      ? remainingTabs.find((t) => t.tabKey === previousActiveTabKey)
      : undefined;
    const fallbackPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(activeTabKey));
    navigate(fallbackPath);
  }, [activeTabKey, previousActiveTabKey, tabs, dispatch, navigate, ignoreRouteParam, onClose]);

  useEffect(() => {
    if (isLoading) return;
    const searchId = paramObj.id != null && String(paramObj.id) !== '' ? String(paramObj.id) : '';
    if (searchId !== '') {
      setNewSearchWizardPhase('off');
      wizardBootstrappedRef.current = false;
      return;
    }
    if (currentSearch?.Id) {
      setNewSearchWizardPhase('off');
      wizardBootstrappedRef.current = false;
      return;
    }
    let hasInitialDs = false;
    if (paramObj.param2) {
      try {
        const raw = typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
        if (raw?.initialDataSetId != null) hasInitialDs = true;
      } catch {
        /* ignore */
      }
    }
    if (hasInitialDs) {
      setNewSearchWizardPhase('off');
      return;
    }
    if (!wizardBootstrappedRef.current) {
      wizardBootstrappedRef.current = true;
      setNewSearchWizardPhase('report_name');
    }
  }, [isLoading, paramObj.id, paramObj.param2, currentSearch?.Id]);

  useEffect(() => {
    if (newSearchWizardPhase !== 'report_name') return;
    setWizardDraftName((prev) => {
      if (prev.trim() !== '') return prev;
      return (currentSearch?.Name && String(currentSearch.Name).trim()) || '';
    });
  }, [newSearchWizardPhase, currentSearch?.Name]);

  useEffect(() => {
    if (ignoreRouteParam) {
      loadData();
      return;
    }
    if (param === undefined) {
      loadData();
    }
  }, [ignoreRouteParam]);
  useEffect(() => {
    if (ignoreRouteParam) return;
    if (param !== undefined && (paramObj.id !== undefined || paramObj.param1 !== undefined || paramObj.param2 !== undefined)) {
      loadData();
    }
  }, [param, paramObj.id, paramObj.param1, paramObj.param2, ignoreRouteParam]);

  const refresh = () => loadData();

  const save = useCallback(async () => {
    if (!currentSearch?.IsModified && !activeViewSaveActionRef.current) return;
    dispatch(setIsBusy());
    try {
      if (activeViewSaveActionRef.current) {
        const ok = await activeViewSaveActionRef.current();
        if (!ok) return;
      }
      if (!currentSearch?.IsModified) {
        showInfo('Saved');
        return;
      }
      const payload: any = {
        ...currentSearch,
        AppSearchFieldList: currentSearch.AppSearchFieldList || [],
        DeletedItemsIds: currentSearch.DeletedItemsIds || []
      };
      // Align with Angular/backend expectations: new items send Id as null, not 0.
      if (payload.Id === 0) payload.Id = null;
      const data = await searchSvc.saveAppSearchExDto(payload);
      if (data?.ValidationResult?.IsValid !== false) {
        setCurrentSearch((prev: any) => (prev ? { ...prev, Id: data?.Id ?? prev.Id, IsModified: false } : prev));
        if (data?.Id && !paramObj.id) {
          setParamObj((p) => ({ ...p, id: data.Id }));
        }
        showInfo('Saved');
        await loadData();
      } else {
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
        showError(msgs.length ? msgs.join('; ') : 'Save failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch, paramObj.id, dispatch, loadData, showError, showInfo]);

  const saveAs = useCallback(async () => {
    if (!currentSearch?.Id) return;
    try {
      const data = await searchSvc.saveAsSearch(String(currentSearch.Id));
      if (data?.IsSuccessful && data?.Object) {
        addTabAndNavigate('search-editor', data.Object.Name || 'Report', { id: data.Object.Id }, true);
        showInfo('Saved as new Report');
      } else {
        showError('Save As failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save As failed');
    }
  }, [currentSearch?.Id, addTabAndNavigate, showError, showInfo]);

  const openTestRun = useCallback(() => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
      showWarning('Save the search and set a default view first');
      return;
    }
    // Align with Angular: open current search (not saved-search instance)
    addTabAndNavigate('masterdatamanagement', 'Search', { searchId: currentSearch.Id, isSavedSearch: false }, true);
  }, [currentSearch?.Id, currentSearch?.SearchViewId, addTabAndNavigate, showWarning]);

  const handleCreateDDLEntity = useCallback(() => {
    const ds: any = currentDataSet;
    if (!currentSearch?.Id || !ds?.DataSourceFrom) {
      showWarning('Save the search and select a dataset first.');
      return;
    }
    const applicationId = currentSearch.SaasApplicationId ?? ds.SaasApplicationId ?? null;
    const param2 = JSON.stringify({ applicationId });
    addTabAndNavigate('entity-info-edit', 'New Entity', { param1: ds.DataSourceFrom, param2 }, true);
  }, [currentSearch?.Id, currentSearch?.SaasApplicationId, currentDataSet, addTabAndNavigate, showWarning]);

  const openApiSettings = useCallback((mode: 'Consume' | 'Provide' | null) => {
    if (!currentSearch?.Id) {
      showWarning('Save the search first.');
      return;
    }
    const param2 = JSON.stringify({
      saasApplicationId: currentSearch.SaasApplicationId ?? null,
      apiConsumeOrProvideType: mode ?? null,
    });
    addTabAndNavigate(
      'search-api-setting',
      'Search API Setting',
      {
        id: currentSearch.Id,
        param1: currentSearch.Name ?? '',
        param2,
      },
      true,
    );
  }, [currentSearch?.Id, currentSearch?.Name, currentSearch?.SaasApplicationId, addTabAndNavigate, showWarning]);

  const handleAddToMainMenu = useCallback(async () => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
      showWarning('Save the search and set a default view first.');
      return;
    }
    try {
      dispatch(setIsBusy());

      const searchDto: any = currentSearch;
      const saasAppId = searchDto.SaasApplicationId;
      const menuData = await adminSvc.retrieveListMenuHairarchyDto(false, saasAppId != null ? String(saasAppId) : '');

      if (!Array.isArray(menuData) || !menuData.length) {
        showError('No main menu root found for this application.');
        return;
      }

      const rootMenu = menuData[0];
      let existSearchMenu: any = null;
      let maxSort = 0;

      if (Array.isArray(rootMenu.AppListMenu_List)) {
        rootMenu.AppListMenu_List.forEach((aMenu: any) => {
          if (typeof aMenu.Sort === 'number' && aMenu.Sort > maxSort) {
            maxSort = aMenu.Sort;
          }
          if (
            aMenu.RouteCode === 'MasterDataManagement' &&
            String(aMenu.Link) === String(searchDto.Id)
          ) {
            existSearchMenu = aMenu;
          }
        });
      }

      if (existSearchMenu) {
        showInfo(
          `Current search is already in main menu.\nMenu path: ${rootMenu.Name} / ${existSearchMenu.Name}`,
        );
        return;
      }

      const menuDto: any = {
        IsNew: true,
        EmAppMenuItemCategory: 1,
        EmDeviceMenuShowMode: 3,
        LinkType: 1,
        RouteCode: 'MasterDataManagement',
        IconName: '',
        Sort: maxSort + 1,
        Name: searchDto.Name,
        Description: searchDto.Description,
        Link: searchDto.Id,
        ParentId: rootMenu.Id,
      };

      const result = await adminSvc.saveOneAppListMenuTreeNode(menuDto);
      if (result?.IsSuccessful && result?.Object) {
        showInfo(
          `Current search has been added to main menu.\nMenu path: ${rootMenu.Name} / ${menuDto.Name}`,
        );

        // Refresh left main menu by reloading user menu from server (similar to navigationSvc.scope().refreshMenus() in AngularJS)
        try {
          const userMenu = await adminSvc.retrieveUserTreeMenu();
          dispatch(setUserMenu(userMenu));
        } catch (menuError: any) {
          // Don't show error to user, just log it
          // eslint-disable-next-line no-console
          console.error('Error refreshing user menu after adding search to main menu:', menuError);
        }
      } else {
        showError('Failed to add to main menu');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to add to main menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch, dispatch, showInfo, showWarning, showError]);

  const setDefaultView = useCallback((viewId: number) => {
    markChange();
    setCurrentSearch((prev: any) => (prev ? { ...prev, SearchViewId: viewId } : prev));
  }, [markChange]);

  const createRegularSearchView = useCallback(() => {
    if (!currentSearch?.DataSetId) {
      showWarning('Select a dataset first');
      return;
    }
    setCreatingViewSeed((v) => v + 1);
    setCreatingViewMode(true);
    setPendingNewViewType(null);
    setSelectedViewId(null);
    setViewRowMenuOpenId(null);
    setEditorSubTab('views');
  }, [currentSearch?.DataSetId, showWarning]);

  const createClusterSearchView = useCallback(() => {
    if (!currentSearch?.DataSetId) {
      showWarning('Select a dataset first');
      return;
    }
    setCreatingViewSeed((v) => v + 1);
    setCreatingViewMode(true);
    setPendingNewViewType(CLUSTER_ANALYSIS_VIEW_TYPE);
    setSelectedViewId(null);
    setViewRowMenuOpenId(null);
    setEditorSubTab('views');
  }, [currentSearch?.DataSetId, showWarning]);

  const deleteSearchView = useCallback(async (view: ViewItem) => {
    if (!view?.Id) return;
    if (!window.confirm(`Confirm to delete view: ${view.Name || view.Id}`)) return;
    dispatch(setIsBusy());
    try {
      const data = await searchSvc.deleteAppSearchView(String(view.Id));
      if (data?.ValidationResult?.IsValid) {
        showInfo('View deleted');
        if (currentSearch?.SearchViewId === view.Id) {
          setCurrentSearch((prev: any) => (prev ? { ...prev, SearchViewId: null, IsModified: true } : prev));
        }
        await loadViewsForDataSet(currentSearch!.DataSetId);
      } else {
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
        showError(msgs.length ? msgs.join('; ') : 'Delete failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Delete failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch?.DataSetId, currentSearch?.SearchViewId, dispatch, loadViewsForDataSet, showError, showInfo]);

  useEffect(() => {
    if (!searchViewList.length) {
      setSelectedViewId(null);
      return;
    }
    if (creatingViewMode) return;
    setSelectedViewId((prev) => {
      if (prev != null && searchViewList.some((v) => v.Id === prev)) return prev;
      const def = currentSearch?.SearchViewId;
      if (def != null && searchViewList.some((v) => v.Id === def)) return def;
      return searchViewList[0].Id;
    });
  }, [searchViewList, currentSearch?.SearchViewId, creatingViewMode]);

  useEffect(() => {
    if (viewRowMenuOpenId == null) return;
    const onDoc = (e: MouseEvent) => {
      const el = viewRowMenuWrapRef.current;
      if (el && !el.contains(e.target as Node)) setViewRowMenuOpenId(null);
    };
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, [viewRowMenuOpenId]);

  useEffect(() => {
    if (editorSubTab !== 'views') setViewRowMenuOpenId(null);
  }, [editorSubTab]);

  const viewEditorParam2 = useMemo(
    () =>
      JSON.stringify({
        callBackTabKey: null,
        saasApplicationId: currentSearch?.SaasApplicationId || null,
        searchId: currentSearch?.Id ?? null
      }),
    [currentSearch?.SaasApplicationId, currentSearch?.Id]
  );

  useEffect(() => {
    let cancelled = false;
    (async () => {
      if (!currentSearch?.DataSetId) {
        setSelectedSearchMeta(null);
        setClusterViewState(null);
        return;
      }
      if (creatingViewMode && pendingNewViewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
        setSelectedSearchMeta(null);
        setClusterViewState(newClusterViewDraft(currentSearch.DataSetId, currentSearch?.SaasApplicationId ?? null));
        return;
      }
      if (creatingViewMode) {
        setSelectedSearchMeta(null);
        setClusterViewState(null);
        return;
      }
      if (selectedViewId == null || selectedViewId < 0) {
        setSelectedSearchMeta(null);
        setClusterViewState(null);
        return;
      }

      const listItem = searchViewList.find((v) => v.Id === selectedViewId);
      if (!listItem) {
        setSelectedSearchMeta(null);
        setClusterViewState(null);
        return;
      }

      if (listItem.ViewType != null) {
        setSelectedSearchMeta({ id: selectedViewId, viewType: listItem.ViewType });
        if (listItem.ViewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
          try {
            const data = await searchSvc.retrieveOneAppSearchViewExDto(String(selectedViewId));
            if (cancelled) return;
            if (data) setClusterViewState(normalizeClusterViewFromApi(data));
            else setClusterViewState(null);
          } catch {
            if (!cancelled) setClusterViewState(null);
          }
        } else {
          setClusterViewState(null);
        }
        return;
      }

      setSelectedSearchMeta(null);
      try {
        const data = await searchSvc.retrieveOneAppSearchViewExDto(String(selectedViewId));
        if (cancelled || !data) {
          if (!cancelled) {
            setSelectedSearchMeta({ id: selectedViewId, viewType: 1 });
            setClusterViewState(null);
          }
          return;
        }
        const vt = data.ViewType ?? 1;
        setSelectedSearchMeta({ id: selectedViewId, viewType: vt });
        if (vt === CLUSTER_ANALYSIS_VIEW_TYPE) {
          setClusterViewState(normalizeClusterViewFromApi(data));
        } else {
          setClusterViewState(null);
        }
      } catch {
        if (!cancelled) {
          setSelectedSearchMeta({ id: selectedViewId, viewType: 1 });
          setClusterViewState(null);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [
    creatingViewMode,
    pendingNewViewType,
    selectedViewId,
    searchViewList,
    currentSearch?.DataSetId,
    currentSearch?.SaasApplicationId,
  ]);

  const handleViewSaved = useCallback(async (savedViewId?: number | null) => {
    if (!currentSearch?.DataSetId) return;
    const list = await loadViewsForDataSet(currentSearch.DataSetId);
    setCreatingViewMode(false);
    setPendingNewViewType(null);
    if (suppressViewSavedAutoSelectRef.current) return;
    if (savedViewId != null) {
      setSelectedViewId(Number(savedViewId));
      return;
    }
    // Fallback when API does not return view id: prefer newest entry.
    if (list.length) {
      const maxId = list.reduce((m: number, v: any) => Math.max(m, Number(v?.Id ?? 0)), 0);
      if (maxId > 0) setSelectedViewId(maxId);
    }
  }, [currentSearch?.DataSetId, loadViewsForDataSet]);

  const saveClusterView = useCallback(async (): Promise<boolean> => {
    if (!clusterViewState) return true;
    if (!clusterViewState.IsModified) return true;
    dispatch(setIsBusy());
    try {
      const payload: any = { ...clusterViewState, IsUpdateClusterMainViewItemSource: true };
      if (payload.Id === 0) payload.Id = null;
      payload.OtherSettingsDto = {
        ...(payload.OtherSettingsDto || {}),
        FlexLayoutItems: ensureClusterChildWidgetCollectionsDeep(
          ensureClusterFlexIdsDeep(payload?.OtherSettingsDto?.FlexLayoutItems || [])
        ),
      };
      const data = await searchSvc.saveAppSearchViewExDto(payload);
      if (data?.ValidationResult?.IsValid !== false) {
        const savedViewId = data?.Id ?? data?.Object?.Id ?? clusterViewState?.Id ?? null;
        setClusterViewState((prev: any) => (prev ? { ...prev, Id: savedViewId ?? prev.Id, IsModified: false } : prev));
        showInfo('Saved');
        await handleViewSaved(savedViewId);
        return true;
      }
      const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
      showError(msgs.length ? msgs.join('; ') : 'Save failed');
      return false;
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
      return false;
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [clusterViewState, dispatch, handleViewSaved, showError, showInfo]);

  const refreshClusterView = useCallback(async () => {
    if (!clusterViewState) return;
    dispatch(setIsBusy());
    try {
      if (!clusterViewState.Id) {
        setClusterViewState(
          newClusterViewDraft(currentSearch!.DataSetId!, currentSearch?.SaasApplicationId ?? null)
        );
        return;
      }
      const data = await searchSvc.retrieveOneAppSearchViewExDto(String(clusterViewState.Id));
      if (data) setClusterViewState(normalizeClusterViewFromApi(data));
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Refresh failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [clusterViewState, currentSearch?.DataSetId, currentSearch?.SaasApplicationId, dispatch, showError]);

  const previewClusterSearchView = useCallback(() => {
    if (!currentSearch?.Id) {
      showInfo('Save the search to preview this Cluster Analysis View.');
      return;
    }
    if (!clusterViewState?.Id) {
      showInfo('Save the view first to preview.');
      return;
    }
    addTabAndNavigate('masterdatamanagement', 'Cluster Analysis Preview', {
      searchId: String(currentSearch.Id),
      isSavedSearch: false,
      initialViewId: clusterViewState.Id,
    }, true);
  }, [addTabAndNavigate, clusterViewState?.Id, currentSearch?.Id, showInfo]);

  const rightEditorKind = useMemo(() => {
    if (!(selectedViewId != null || creatingViewMode) || !currentSearch?.DataSetId) return 'empty' as const;
    if (creatingViewMode && pendingNewViewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
      return clusterViewState ? ('cluster' as const) : ('loading' as const);
    }
    if (creatingViewMode) return 'searchView' as const;
    if (selectedViewId == null || selectedViewId < 0) return 'empty' as const;
    if (!selectedSearchMeta || selectedSearchMeta.id !== selectedViewId) return 'loading' as const;
    if (selectedSearchMeta.viewType === CLUSTER_ANALYSIS_VIEW_TYPE) {
      return clusterViewState != null && Number(clusterViewState.Id) === Number(selectedViewId)
        ? ('cluster' as const)
        : ('loading' as const);
    }
    return 'searchView' as const;
  }, [
    clusterViewState,
    creatingViewMode,
    currentSearch?.DataSetId,
    pendingNewViewType,
    selectedSearchMeta,
    selectedViewId,
  ]);

  useEffect(() => {
    if (rightEditorKind !== 'cluster') return;
    activeViewSaveActionRef.current = saveClusterView;
    return () => {
      activeViewSaveActionRef.current = null;
    };
  }, [rightEditorKind, saveClusterView]);

  useEffect(() => {
    let cancelled = false;
    if (rightEditorKind !== 'cluster') {
      setClusterDesignPreviewRows([]);
      return;
    }
    if (!currentSearch?.Id || !clusterViewState?.Id) {
      setClusterDesignPreviewRows([]);
      return;
    }
    (async () => {
      try {
        const searchDto = await searchSvc.retrieveOneSearch(String(currentSearch.Id), false);
        const searchRevisedDto = {
          ...searchDto,
          ReferenceViewDefinitionDto: {
            ...(clusterViewState || {}),
            Id: clusterViewState.Id,
            IsMassUpdate: false,
            ViewType: CLUSTER_ANALYSIS_VIEW_TYPE,
          },
        };
        const searchResult = await searchSvc.retrieveSearchResult(searchRevisedDto);
        if (cancelled) return;
        setClusterDesignPreviewRows(Array.isArray(searchResult?.SearchResultRowList) ? searchResult.SearchResultRowList : []);
      } catch {
        if (!cancelled) setClusterDesignPreviewRows([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [rightEditorKind, currentSearch?.Id, clusterViewState?.Id]);

  const handleSelectView = useCallback(async (viewId: number, isCreatingItem: boolean) => {
    if (isCreatingItem) return;
    if (!creatingViewMode && selectedViewId === viewId) {
      setViewRowMenuOpenId(null);
      return;
    }
    if (activeViewSaveActionRef.current) {
      suppressViewSavedAutoSelectRef.current = true;
      try {
        const ok = await activeViewSaveActionRef.current();
        if (!ok) return;
      } finally {
        suppressViewSavedAutoSelectRef.current = false;
      }
    }
    setCreatingViewMode(false);
    setSelectedViewId(viewId);
    setViewRowMenuOpenId(null);
  }, [creatingViewMode, selectedViewId]);

  const viewTypeLabel = useCallback(
    (vt?: number) => resolveAppViewTypeDisplay(vt, emAppViewType),
    [emAppViewType]
  );
  const viewListForRender = useMemo(() => {
    if (!creatingViewMode) return searchViewList;
    const placeholder: ViewItem = {
      Id: -1,
      Name: pendingNewViewType === CLUSTER_ANALYSIS_VIEW_TYPE ? 'New Cluster Analysis View' : 'New View',
      ViewType: pendingNewViewType ?? WIZARD_DEFAULT_VIEW_TYPE,
    };
    return [...searchViewList, placeholder];
  }, [searchViewList, creatingViewMode, pendingNewViewType]);

  const openDatasetEditor = useCallback(() => {
    if (!currentDataSet?.Id) return;
    addTabAndNavigate('dataset-editor', currentDataSet.Name || 'Dataset', { id: currentDataSet.Id }, true);
  }, [currentDataSet, addTabAndNavigate]);

  const renderSearchPropertiesRow = (opts?: { tightBottom?: boolean }) => (
    <div
      className={`flex flex-wrap items-center gap-4 px-4 ${theme.mainContentSection} ${
        opts?.tightBottom ? 'pt-2 pb-0' : 'py-2'
      }`}
    >
      <div className="flex items-center gap-2">
        <label className={`w-24 text-xs whitespace-nowrap ${theme.label}`}>Report Name</label>
        <input
          type="text"
          value={currentSearch?.Name ?? ''}
          onChange={(e) => {
            markChange();
            setCurrentSearch((p: any) => (p ? { ...p, Name: e.target.value } : p));
          }}
          className={`w-48 h-7 px-2 text-xs border ${theme.inputBox}`}
        />
      </div>
      <div className="flex items-center gap-2">
        <label className={`w-20 text-xs ${theme.label}`}>Description</label>
        <input
          type="text"
          value={currentSearch?.Description ?? ''}
          onChange={(e) => {
            markChange();
            setCurrentSearch((p: any) => (p ? { ...p, Description: e.target.value } : p));
          }}
          className={`w-48 h-7 px-2 text-xs border ${theme.inputBox}`}
        />
      </div>
      
      <div className="flex items-center gap-2">
        <label className={`w-24 text-xs whitespace-nowrap ${theme.label}`}>Default View</label>
        <select
          value={currentSearch?.SearchViewId ?? ''}
          onChange={(e) => {
            const raw = e.target.value;
            if (!raw) {
              markChange();
              setCurrentSearch((p: any) => (p ? { ...p, SearchViewId: null } : p));
              return;
            }
            setDefaultView(Number(raw));
          }}
          className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
          disabled={!searchViewList.length}
        >
          <option value="">—</option>
          {searchViewList.map((v) => (
            <option key={v.Id} value={v.Id}>{v.Name || `View ${v.Id}`}</option>
          ))}
        </select>
      </div>

      <div className="flex items-center gap-2">
        <label className={`w-20 text-xs ${theme.label}`}>Dataset</label>
        <div className="flex items-center gap-1">
          {currentSearch?.Id ? (
            currentDataSet ? (
              <button type="button" onClick={openDatasetEditor} className={`text-xs underline ${theme.title}`}>
                <i className="fa-solid fa-pen-to-square mr-1" aria-hidden /> {currentDataSet.Name}
              </button>
            ) : (
              <span className={`text-xs ${theme.label}`}>—</span>
            )
          ) : (
            <select
              value={currentSearch?.DataSetId ?? ''}
              onChange={async (e) => {
                const id = e.target.value ? Number(e.target.value) : null;
                markChange();
                setCurrentSearch((p: any) => (p ? { ...p, DataSetId: id, SearchViewId: null } : p));
                if (id) {
                  const ds = allDataSet.find((d) => d.Id === id) ?? null;
                  setCurrentDataSet(ds);
                  setDatabaseSchemaData([]);
                  setRelationTableColumns([]);
                  await loadViewsForDataSet(id);
                } else {
                  setCurrentDataSet(null);
                  setSearchViewList([]);
                  setDatabaseSchemaData([]);
                  setRelationTableColumns([]);
                }
              }}
              className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`}
            >
              <option value="">Select dataset</option>
              {allDataSet.map((d) => (
                <option key={d.Id} value={d.Id}>{d.Name ?? d.Id}</option>
              ))}
            </select>
          )}
        </div>
      </div>
    </div>
  );

  const renderEditorSubTabs = () => (
    <div className={`flex items-center gap-1 px-3 pt-1 shrink-0 border-b ${theme.mainContentSection}`}>
      <button
        type="button"
        onClick={() => setEditorSubTab('filters')}
        className={`px-3 py-1.5 text-sm rounded-t-[4px] border-b-2 ${
          editorSubTab === 'filters'
            ? `${theme.title} border-current font-semibold`
            : `${theme.label} border-transparent`
        }`}
        aria-selected={editorSubTab === 'filters'}
        role="tab"
      >
        Filters
      </button>
      <button
        type="button"
        onClick={() => setEditorSubTab('views')}
        className={`px-3 py-1.5 text-sm rounded-t-[4px] border-b-2 ${
          editorSubTab === 'views'
            ? `${theme.title} border-current font-semibold`
            : `${theme.label} border-transparent`
        }`}
        aria-selected={editorSubTab === 'views'}
        role="tab"
      >
        Report Views
      </button>
    </div>
  );

  const showNewSearchWizard = newSearchWizardPhase !== 'off';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden relative">
      <div className={`flex items-center justify-between px-3 mb-0.5 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold truncate min-w-0 ${theme.title}`}>{PAGE_TITLE}</div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={refresh} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={save}
            disabled={!currentSearch?.IsModified}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save
          </button>
          <button
            type="button"
            onClick={() => setAdvancedOptionsOpen(true)}
            disabled={!currentSearch?.Id}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
            title="Advanced Options"
          >
            <i className="fa-solid fa-gears" aria-hidden /> Advanced Options
          </button>
          <button
            type="button"
            onClick={saveAs}
            disabled={!currentSearch?.Id}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-window-restore" aria-hidden /> Save As
          </button>
          {currentSearch?.Id && (currentDataSet as any)?.DataSourceFrom && (
            <button
              type="button"
              onClick={handleCreateDDLEntity}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            >
              <i className="fa-solid fa-plus" aria-hidden /> Create DDL Entity
            </button>
          )}
          {currentSearch?.Id && (
            <div className="relative" ref={apiMenuRef}>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setApiMenuOpen((v) => !v);
                }}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                <i className="fa-solid fa-gears" aria-hidden /> API Settings <i className="fa-solid fa-caret-down text-xs ml-1" aria-hidden />
              </button>
              {apiMenuOpen && (
                <div className={`absolute right-0 mt-1 min-w-[260px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}>
                  {(currentDataSet as any)?.QueryType === EmAppDataServiceType.IntegrationWebApiCall && (
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        setApiMenuOpen(false);
                        openApiSettings('Consume');
                      }}
                      className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                    >
                      Edit Consume API: Call 3rd Part API
                    </button>
                  )}
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      setApiMenuOpen(false);
                      openApiSettings('Provide');
                    }}
                    className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                  >
                    Edit Provide API: Provide API To 3rd Part
                  </button>
                </div>
              )}
            </div>
          )}
          <button
            type="button"
            onClick={handleAddToMainMenu}
            disabled={!currentSearch?.Id || !currentSearch?.SearchViewId}
            className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-plus" aria-hidden /><i className="fa-solid fa-bars text-[10px] relative -left-1 top-0.5" aria-hidden /> Add To Main Menu
          </button>
          <button
            type="button"
            onClick={openTestRun}
            disabled={!currentSearch?.Id || !currentSearch?.SearchViewId}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50`}
          >
            <i className="fa-solid fa-window-maximize" aria-hidden /> Test Run
          </button>
        </div>
      </div>

      {showNewSearchWizard ? (
        <div className="w-full h-1 flex-auto flex flex-col min-h-0 items-center justify-start py-10 px-4 overflow-auto">
          {newSearchWizardPhase === 'report_name' && (
            <div className={`w-full max-w-md rounded-md border p-6 shadow-sm ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-4 ${theme.title}`}>New report — step 1 of 2</div>
              <label className={`block text-xs mb-1 ${theme.label}`} htmlFor="wizard-report-name">
                Report Name
              </label>
              <input
                id="wizard-report-name"
                type="text"
                value={wizardDraftName}
                onChange={(e) => setWizardDraftName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleWizardNameNext();
                }}
                className={`w-full h-9 px-2 text-sm border rounded-[4px] mb-4 ${theme.inputBox}`}
                placeholder="Enter report name"
                autoFocus
              />
              <div className="flex items-center justify-between w-full max-w-[208px]">
                <button
                  type="button"
                  onClick={handleWizardCancelClose}
                  className="w-[100px] h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-red-500 hover:bg-red-600"
                >
                  Cancel <i className="fa-solid fa-xmark" aria-hidden />
                </button>
                <button
                  type="button"
                  onClick={handleWizardNameNext}
                  className="w-[100px] h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
                >
                  Next <i className="fa-solid fa-arrow-right" aria-hidden />
                </button>
              </div>
            </div>
          )}
          {newSearchWizardPhase === 'dataset' && (
            <div className={`w-full max-w-lg rounded-md border p-6 shadow-sm ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Choose dataset — step 2 of 2</div>
              <p className={`text-xs mb-4 ${theme.label}`}>
                Report name: <span className={`font-medium ${theme.title}`}>{wizardDraftName.trim() || '—'}</span>
              </p>
              <p className={`text-xs mb-4 ${theme.label}`}>
                Select an existing dataset or create a new one. After you choose or create, the report will be saved and opened for editing.
              </p>
              <div className="flex flex-wrap gap-2 mb-2">
                <button
                  type="button"
                  onClick={() => setWizardDatasetPickerOpen(true)}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                >
                  Choose dataset…
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setWizardCreateDatasetOpen(true);
                  }}
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                >
                  Create new dataset…
                </button>
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setNewSearchWizardPhase('report_name')}
                  className="w-[100px] h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
                >
                  <i className="fa-solid fa-arrow-left" aria-hidden /> Back
                </button>
                <button
                  type="button"
                  onClick={handleWizardCancelClose}
                  className="w-[100px] h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-red-500 hover:bg-red-600"
                >
                  Cancel <i className="fa-solid fa-xmark" aria-hidden />
                </button>
              </div>
            </div>
          )}
          {newSearchWizardPhase === 'saving' && (
            <div className={`text-sm ${theme.label}`}>Creating report and default view…</div>
          )}
        </div>
      ) : (
        <>
      {!currentSearch?.DataSetId && (
        <div className="flex w-full shrink-0 flex-col">
          {renderSearchPropertiesRow()}
        </div>
      )}

      {currentSearch?.DataSetId && (
        <div className="w-full h-1 flex-auto flex flex-col min-h-0 overflow-hidden">
          {renderSearchPropertiesRow({ tightBottom: true })}
          {renderEditorSubTabs()}
          {editorSubTab === 'filters' && (
            <div className="min-h-0 flex-1 flex flex-col overflow-hidden">
              {currentSearch?.SearchViewId ? (
                <SearchFilterEditor
                  compactFilterHeader
                  flexFillParent
                  sectionMinHeightPx={100}
                  filterList={currentSearch?.AppSearchFieldList ?? []}
                  onAddFilter={addSearchFilter}
                  onRemoveFilter={removeSearchFilter}
                  onMarkChange={markChange}
                  filterColumnsDataMap={dataSetColumnsList.length ? dataSetColumnsDataMap : null}
                  controlTypeDataMap={controlTypeDataMap}
                  criteriaOperatorDataMap={criteriaOperatorDataMap}
                  enableAdvancedOptions={true}
                  entityListForFilter={entityListForFilter}
                  getEntityCodeById={getEntityCodeById}
                  onOpenDatasourceSelector={openEntityInfoPopup}
                  onOpenEntityDataPreview={openEntityDataEditorPopup}
                  subControlTypeDataMap={subControlTypeDataMap}
                  internalCodeDataMap={internalCodeDataMap}
                  getSearchFieldNameById={getSearchFieldNameById}
                  onOpenCascadingConfig={openCascadingConfigPopup}
                  onOpenInnerRelationConfig={openInnerRelationPopup}
                  showWarning={showWarning}
                  minHeight={0}
                />
              ) : (
                <div className={`flex-1 flex items-center justify-center text-sm px-4 text-center ${theme.label}`}>
                  Set a Default View in Report Settings to edit filters.
                </div>
              )}
            </div>
          )}
          {editorSubTab === 'views' && (
              <div className="w-full h-1 flex-auto flex flex-row gap-2 min-h-0 overflow-hidden">
            <div className={`w-72 pl-4 shrink-0 flex flex-col min-h-0 overflow-hidden ${theme.mainContentSection}`}>
              <div className={`flex flex-col gap-1.5 px-2 py-2 border-b shrink-0 ${theme.mainContentSection}`}>
                <div className={`text-xs font-semibold ${theme.title}`}>Views</div>
                <div className="flex flex-wrap gap-1">
                  <button type="button" onClick={createRegularSearchView} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                    <i className="fa-solid fa-plus" aria-hidden /> Regular View
                  </button>
                  <button type="button" onClick={createClusterSearchView} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                    <i className="fa-solid fa-plus" aria-hidden /> Cluster View
                  </button>
                </div>
              </div>
              <div className="flex-1 min-h-0 overflow-y-auto">
                {viewListForRender.map((v) => {
                  const isCreatingItem = creatingViewMode && v.Id === -1;
                  const isSel = isCreatingItem ? true : v.Id === selectedViewId;
                  const isDef = currentSearch?.SearchViewId === v.Id;
                  return (
                    <div
                      key={v.Id}
                      role="button"
                      tabIndex={0}
                      aria-selected={isSel}
                      onClick={() => {
                        void handleSelectView(v.Id, isCreatingItem);
                      }}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          void handleSelectView(v.Id, isCreatingItem);
                        }
                      }}
                      className={`w-full text-left px-2 py-2 border-b last:border-b-0 cursor-pointer text-xs flex items-center gap-1.5 border-l-[3px] ${
                        isSel ? `${t('bg_default_hover')} ${t('border_default_active')}` : `border-transparent ${theme.mainContentSection}`
                      }`}
                    >
                      <div className="min-w-0 w-1 flex-auto">
                        <div className={`font-medium truncate ${theme.title} flex items-center gap-1.5 min-w-0`}>
                          <span className={`shrink-0 w-3 text-center ${theme.title}`} aria-hidden>
                            {isDef ? '*' : ''}
                          </span>
                          <span className="truncate min-w-0">{v.Name || `View ${v.Id}`}</span>
                        </div>
                      </div>
                      <div
                        ref={!isCreatingItem && viewRowMenuOpenId === v.Id ? viewRowMenuWrapRef : undefined}
                        className="relative shrink-0"
                      >
                        <button
                          type="button"
                          disabled={isCreatingItem}
                          className={`w-7 h-7 inline-flex items-center justify-center rounded-[4px] text-xs ${theme.button_default}`}
                          title="More Options"
                          aria-label="More Options"
                          aria-expanded={!isCreatingItem && viewRowMenuOpenId === v.Id}
                          onClick={(e) => {
                            e.stopPropagation();
                            if (isCreatingItem) return;
                            setViewRowMenuOpenId((id) => (id === v.Id ? null : v.Id));
                          }}
                        >
                          <i className="fa-solid fa-bars text-xs leading-none" aria-hidden />
                        </button>
                        {viewRowMenuOpenId === v.Id && !isCreatingItem && (
                          <div
                            className={`absolute right-0 top-full mt-0.5 z-[60] min-w-[11rem] rounded-md border shadow-md py-1 ${theme.mainContentSection}`}
                            role="menu"
                          >
                            <button
                              type="button"
                              role="menuitem"
                              disabled={isDef}
                              className={`w-full px-3 py-2 text-left text-xs disabled:opacity-50 disabled:cursor-not-allowed ${theme.contextMenu}`}
                              onClick={(e) => {
                                e.stopPropagation();
                                if (!isDef) setDefaultView(v.Id);
                                setViewRowMenuOpenId(null);
                              }}
                            >
                              Set as default view
                            </button>
                            <button
                              type="button"
                              role="menuitem"
                              className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                              onClick={(e) => {
                                e.stopPropagation();
                                setViewRowMenuOpenId(null);
                                void deleteSearchView(v);
                              }}
                            >
                              Delete view
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
            <div className={`h-full w-1 flex-auto flex flex-col overflow-hidden rounded-md border min-w-0 ${theme.mainContentSection}`}>
              {(selectedViewId != null || creatingViewMode) && currentSearch?.DataSetId ? (
                rightEditorKind === 'loading' ? (
                  <div className={`w-full h-full flex items-center justify-center text-sm ${theme.label}`}>Loading view…</div>
                ) : rightEditorKind === 'cluster' && clusterViewState ? (
                  <ClusterAnalysisViewLayoutEditor
                    key={clusterViewState.Id ?? `new-cluster-${creatingViewSeed}`}
                    flexLayoutItems={clusterViewState?.OtherSettingsDto?.FlexLayoutItems ?? []}
                    onFlexLayoutItemsChange={(items) => {
                      setClusterViewState((p: any) =>
                        p
                          ? {
                              ...p,
                              OtherSettingsDto: { ...(p.OtherSettingsDto || {}), FlexLayoutItems: items },
                              IsModified: true,
                            }
                          : p
                      );
                    }}
                    chartTypeList={chartTypeListForCluster}
                    dataSetColumns={dataSetColumnsList}
                    mainViewFields={clusterViewState?.AppSearchViewFieldList ?? []}
                    previewDataRows={clusterDesignPreviewRows}
                    onRefresh={() => void refreshClusterView()}
                    onSave={() => void saveClusterView()}
                    onPreviewClusterSearchView={previewClusterSearchView}
                    viewName={clusterViewState?.Name ?? ''}
                    viewDescription={clusterViewState?.Description ?? ''}
                    onViewNameChange={(v) => {
                      setClusterViewState((p: any) => (p ? { ...p, Name: v, IsModified: true } : p));
                    }}
                    onViewDescriptionChange={(v) => {
                      setClusterViewState((p: any) => (p ? { ...p, Description: v, IsModified: true } : p));
                    }}
                  />
                ) : rightEditorKind === 'searchView' ? (
                  <SearchViewEditor
                    key={selectedViewId ?? `new-view-${creatingViewSeed}`}
                    embedded
                    embedParam={{
                      id: creatingViewMode ? undefined : selectedViewId,
                      param1: String(currentSearch.DataSetId),
                      param2: creatingViewMode
                        ? JSON.stringify({
                            callBackTabKey: null,
                            saasApplicationId: currentSearch?.SaasApplicationId || null,
                            searchId: currentSearch?.Id ?? null,
                            initialViewName: 'New View',
                            ...(pendingNewViewType != null ? { initialViewType: pendingNewViewType } : {}),
                          })
                        : viewEditorParam2
                    }}
                    onRegisterSaveAction={(saveAction) => {
                      activeViewSaveActionRef.current = saveAction;
                    }}
                    onSaved={handleViewSaved}
                  />
                ) : (
                  <div className={`w-full h-full flex items-center justify-center text-sm px-4 text-center ${theme.label}`}>
                    Select a view from the list.
                  </div>
                )
              ) : (
                <div className={`w-full h-full flex items-center justify-center text-sm px-4 text-center ${theme.label}`}>
                  {searchViewList.length ? 'Select a view from the list.' : 'No views. Create one with Single or Cluster on the left.'}
                </div>
              )}
            </div>
              </div>
          )}
        </div>
      )}
        </>
      )}

      {wizardDatasetPickerOpen && (
        <div
          className="fixed inset-0 z-[120] flex items-center justify-center bg-black/40 p-4"
          onClick={() => setWizardDatasetPickerOpen(false)}
        >
          <div
            className={`flex max-h-[75vh] w-full max-w-lg flex-col overflow-hidden rounded-md border shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Select dataset</span>
              <button
                type="button"
                onClick={() => setWizardDatasetPickerOpen(false)}
                className={`px-2 py-1 text-lg leading-none rounded-[4px] ${theme.button_default}`}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="min-h-0 flex-1 overflow-y-auto p-2">
              {allDataSet.length === 0 ? (
                <div className={`p-4 text-sm ${theme.label}`}>No datasets available. Create a new dataset.</div>
              ) : (
                allDataSet.map((d) => (
                  <div
                    key={d.Id}
                    className={`mb-1 flex items-center gap-2 rounded border px-2 py-2 ${theme.mainContentSection}`}
                  >
                    <button
                      type="button"
                      className={`min-w-0 flex-1 truncate text-left text-sm ${theme.title}`}
                      onClick={() => {
                        setWizardDatasetPickerOpen(false);
                        void completeWizardWithDataset(d.Id);
                      }}
                    >
                      {d.Name ?? `Dataset ${d.Id}`}
                    </button>
                    <button
                      type="button"
                      className={`shrink-0 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                      onClick={(e) => {
                        e.stopPropagation();
                        setWizardDatasetPreviewId(d.Id);
                      }}
                    >
                      Details
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      )}

      {wizardDatasetPreviewId != null && (
        <div className="fixed inset-0 z-[130] flex flex-col bg-black/50 p-2">
          <div className={`flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border ${theme.mainContentSection}`}>
            <div className={`flex shrink-0 justify-end border-b px-2 py-1 ${theme.mainContentSection}`}>
              <button
                type="button"
                onClick={() => setWizardDatasetPreviewId(null)}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Close
              </button>
            </div>
            <div className="min-h-0 flex-1 overflow-hidden">
              <DataSetEditor
                ignoreRouteParam
                dataSetId={wizardDatasetPreviewId}
                onClose={() => setWizardDatasetPreviewId(null)}
              />
            </div>
          </div>
        </div>
      )}

      {wizardCreateDatasetOpen && (
        <div className="fixed inset-0 z-[130] flex flex-col bg-black/50 p-2">
          <div className={`flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border ${theme.mainContentSection}`}>
            <DataSetEditor
              ignoreRouteParam
              dataSetId={null}
              queryType={1}
              initialName={(wizardDraftName || '').trim()}
              onConfirmAndClose={(obj: any) => {
                const id = obj?.Id ?? obj?.id;
                setWizardCreateDatasetOpen(false);
                if (id != null) void completeWizardWithDataset(Number(id));
              }}
              onClose={() => {
                setWizardCreateDatasetOpen(false);
              }}
              onSave={() => {
                // Keep popup open on normal Save (only Confirm and Close should close).
              }}
            />
          </div>
        </div>
      )}

      {filterColumnPickerOpen && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setFilterColumnPickerOpen(false)}>
          <div
            className={`rounded shadow-xl w-full max-w-lg flex flex-col ${theme.mainContentSection}`}
            style={{ height: '500px' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Field List</div>
              <button type="button" onClick={() => setFilterColumnPickerOpen(false)} className="text-lg leading-none">&times;</button>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden p-2" style={{ minHeight: 0 }}>
              {filterColumnPickerCV ? (
                <FlexGrid
                  ref={filterColumnPickerGridRef}
                  itemsSource={filterColumnPickerCV}
                  isReadOnly={true}
                  selectionMode="ListBox"
                  className="w-full h-full"
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
                <div className={`text-sm ${theme.label} py-4`}>All columns are already added.</div>
              )}
            </div>
            <div className="flex justify-end gap-2 p-3 border-t">
              <button type="button" onClick={() => setFilterColumnPickerOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
              <button type="button" onClick={handleFilterColumnPickerOk} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Ok</button>
            </div>
          </div>
        </div>
      )}

      {/* Advanced Options popup */}
      {advancedOptionsOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/30" onClick={() => setAdvancedOptionsOpen(false)}>
          <div
            className={`min-w-[440px] max-h-[90vh] overflow-auto rounded shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Advanced Options</span>
              <button type="button" onClick={() => setAdvancedOptionsOpen(false)} className="text-lg leading-none px-2 py-1">&times;</button>
            </div>
            <div className="p-3 space-y-3">
              <div className="flex items-center gap-3">
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Usage Type</label>
                <select
                  value={currentSearch?.Type ?? 1}
                  onChange={(e) => {
                    markChange();
                    setCurrentSearch((p: any) => (p ? { ...p, Type: Number(e.target.value) } : p));
                  }}
                  className={`w-52 shrink-0 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  {searchTypeList.map((o) => (
                    <option key={o.Id} value={o.Id}>{o.Display}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Embedded Child Search</label>
                <select
                  value={currentSearch?.WhereUsedSearchId ?? ''}
                  onChange={(e) => {
                    markChange();
                    setCurrentSearch((p: any) => (p ? { ...p, WhereUsedSearchId: e.target.value ? Number(e.target.value) : null } : p));
                  }}
                  className={`w-52 shrink-0 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {whereUsedSearchList.map((s: any) => (
                    <option key={s.Id} value={s.Id}>{s.Display ?? s.Name ?? s.Id}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Filter By Current User Field</label>
                <select
                  value={currentSearch?.FilterByCurrentUserMappingField ?? ''}
                  onChange={(e) => {
                    markChange();
                    setCurrentSearch((p: any) => (p ? { ...p, FilterByCurrentUserMappingField: e.target.value || null } : p));
                  }}
                  className={`w-52 shrink-0 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {dataSetColumnsList.map((c: any) => (
                    <option key={c.Id ?? c} value={c.Id ?? c}>{c.Id ?? c}</option>
                  ))}
                </select>
              </div>
              <div className={`flex items-center gap-3 ${theme.label}`}>
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap`}>Is Builtin</label>
                <div className="w-52 shrink-0 flex items-center">
                  <input
                    type="checkbox"
                    checked={!!currentSearch?.IsBuiltIn}
                    onChange={(e) => {
                      markChange();
                      setCurrentSearch((p: any) => (p ? { ...p, IsBuiltIn: e.target.checked } : p));
                    }}
                  />
                </div>
              </div>
              <div className={`flex items-center gap-3 ${theme.label}`}>
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap`}>Is Default</label>
                <div className="w-52 shrink-0 flex items-center">
                  <input type="checkbox" checked={!!currentSearch?.IsDefault} onChange={(e) => { markChange(); setCurrentSearch((p: any) => (p ? { ...p, IsDefault: e.target.checked } : p)); }} />
                </div>
              </div>
              <div className={`flex items-center gap-3 ${theme.label}`}>
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap`}>Is Auto Execute</label>
                <div className="w-52 shrink-0 flex items-center">
                  <input type="checkbox" checked={!!currentSearch?.IsAutoExecute} onChange={(e) => { markChange(); setCurrentSearch((p: any) => (p ? { ...p, IsAutoExecute: e.target.checked } : p)); }} />
                </div>
              </div>
              <div className={`flex items-center gap-3 ${theme.label}`}>
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap`}>Is Hide All Toolbar Buttons</label>
                <div className="w-52 shrink-0 flex items-center">
                  <input type="checkbox" checked={!!currentSearch?.IsHideAllToolsBar} onChange={(e) => { markChange(); setCurrentSearch((p: any) => (p ? { ...p, IsHideAllToolsBar: e.target.checked } : p)); }} />
                </div>
              </div>
              <div className={`flex items-center gap-3 ${theme.label}`}>
                <label className={`w-52 shrink-0 text-xs whitespace-nowrap`}>Is For Public Access</label>
                <div className="w-52 shrink-0 flex items-center">
                  <input type="checkbox" checked={!!currentSearch?.IsForPublicAcesss} onChange={(e) => { markChange(); setCurrentSearch((p: any) => (p ? { ...p, IsForPublicAcesss: e.target.checked } : p)); }} />
                </div>
              </div>
            </div>
            <div className="flex justify-end p-3 border-t">
              <button type="button" onClick={() => setAdvancedOptionsOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>OK</button>
            </div>
          </div>
        </div>
      )}

      {entitySelectionItem != null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true" aria-labelledby="search-datasource-selector-title">
          <div className={`max-w-lg w-full max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`} id="search-datasource-selector-title">
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
              <button type="button" onClick={closeDatasourceSelectorPopup} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Close</button>
              <button type="button" onClick={() => datasourceSelectedHandler(datasourceSelectorSelectedId ?? null)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>OK</button>
            </div>
          </div>
        </div>
      )}

      {cascadingConfig && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true">
          <div className={`w-full max-w-lg max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`}>
              <span className="text-md font-semibold">Cascading Setting</span>
              <button type="button" onClick={() => setCascadingConfig(null)} className={`p-1 rounded ${theme.button_default}`} aria-label="Close">
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-auto p-3 space-y-2 text-xs">
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Current Field</label>
                <input
                  type="text"
                  disabled
                  value={cascadingConfig.currentField?.SysTableFiledPath ?? ''}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                />
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Parent Field</label>
                <select
                  value={cascadingConfig.ParentFieldId ?? ''}
                  onChange={(e) => setCascadingConfig((prev: any) => prev ? { ...prev, ParentFieldId: e.target.value ? Number(e.target.value) : null } : prev)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {ddlFieldLookupList.map((o) => (
                    <option key={o.Id} value={o.Id}>{o.Display}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Relation Table</label>
                <select
                  value={cascadingConfig.relationTableObj?.Name ?? ''}
                  disabled={isLoadingCascadingSchema}
                  onChange={async (e) => {
                    const tableName = e.target.value || '';
                    const relationTableObj = databaseSchemaData.find((t: any) => t.Name === tableName) ?? null;
                    let columns: string[] = [];
                    if (relationTableObj && currentDataSet) {
                      const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                        relationTableObj.Name,
                        currentDataSet.DataSourceFrom ?? null,
                        relationTableObj.SchemaOwner ?? null
                      );
                      const cols = Array.isArray(tableData?.Columns) ? tableData.Columns : [];
                      columns = cols.map((c: any) => c.Name);
                    }
                    setRelationTableColumns(columns);
                    setCascadingConfig((prev: any) => prev ? {
                      ...prev,
                      relationTableObj,
                      CascadingRelationTableParentKeyField: '',
                      CascadingRelationTableChildKeyField: ''
                    } : prev);
                  }}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">{isLoadingCascadingSchema ? 'Loading table list...' : '—'}</option>
                  {databaseSchemaData.map((t: any) => (
                    <option key={t.Name} value={t.Name}>{t.Display ?? t.Name}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Relation Parent Key</label>
                <select
                  value={cascadingConfig.CascadingRelationTableParentKeyField ?? ''}
                  onChange={(e) => setCascadingConfig((prev: any) => prev ? { ...prev, CascadingRelationTableParentKeyField: e.target.value || '' } : prev)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {relationTableColumns.map((name) => (
                    <option key={name} value={name}>{name}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Relation Child Key</label>
                <select
                  value={cascadingConfig.CascadingRelationTableChildKeyField ?? ''}
                  onChange={(e) => setCascadingConfig((prev: any) => prev ? { ...prev, CascadingRelationTableChildKeyField: e.target.value || '' } : prev)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {relationTableColumns.map((name) => (
                    <option key={name} value={name}>{name}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={() => setCascadingConfig(null)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Close</button>
              <button type="button" onClick={applyCascadingConfig} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>OK</button>
            </div>
          </div>
        </div>
      )}

      {innerRelationConfig && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true">
          <div className={`w-full max-w-lg max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`}>
              <span className="text-md font-semibold">Inner Relationship</span>
              <button type="button" onClick={() => setInnerRelationConfig(null)} className={`p-1 rounded ${theme.button_default}`} aria-label="Close">
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-auto p-3 space-y-2 text-xs">
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Current Field</label>
                <input
                  type="text"
                  disabled
                  value={innerRelationConfig.currentField?.DisplayText || innerRelationConfig.currentField?.SysTableFiledPath || ''}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                />
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Master Field</label>
                <select
                  value={innerRelationConfig.MasterEntityFieldlId ?? ''}
                  onChange={(e) => handleInnerRelationMasterFieldChange(e.target.value ? Number(e.target.value) : null)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {innerMasterFieldOptions.map((o) => (
                    <option key={o.Id} value={o.Id}>{o.Display}</option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-3">
                <label className={`w-32 shrink-0 ${theme.label}`}>Subscribe Property</label>
                <select
                  value={innerRelationConfig.InnerEntitySubscribeFiled ?? ''}
                  onChange={(e) => setInnerRelationConfig((prev: any) => prev ? { ...prev, InnerEntitySubscribeFiled: e.target.value || '' } : prev)}
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                >
                  <option value="">—</option>
                  {subscribeInnerEntityColumns.map((c) => (
                    <option key={c.Display} value={c.Display}>{c.Display}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={clearInnerRelationConfig} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Clear</button>
              <button type="button" onClick={() => setInnerRelationConfig(null)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Close</button>
              <button type="button" onClick={applyInnerRelationConfig} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>OK</button>
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

export default SearchEditor;
