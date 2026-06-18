/**
 * Data Model Template Editor – migrated from AngularJS TransactionGroupEditor (full).
 * Search-based: Name, Dataset, Description; tabs Template Items | Template View Fields | Template Filters.
 * Dataset selector: modal. Template Main Items: form-style rows (no FlexGrid), labels above fields.
 * See TransactionGroupEditor.DIFF.md for React vs Angular diff.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { setUserMenu } from '../../redux/features/admin/userSessionSlice';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import SearchFilterEditor from '../search/SearchFilterEditor';
import DataSetEditor from '../dbmgt/DataSetEditor';
import TemplateFolderNavigationSection from './TemplateFolderNavigationSection';

const LINK_TARGET_USAGE_FORM = 1;
const LINK_TARGET_USAGE_SEARCH = 3;
/** TemplateItemType: 1 = MainItem, 2 = TemplateHeader (Shared) */
const TEMPLATE_ITEM_TYPE_MAIN = 1;
const TEMPLATE_ITEM_TYPE_HEADER = 2;

interface TransactionGroupEditorParam {
  id?: number | string | null;
  param1?: string | number | null;
  param2?: string | Record<string, unknown> | null;
}

interface DataSetItem {
  Id: number;
  Name?: string;
}

interface CurrentSearch {
  Id?: number;
  Name?: string;
  Description?: string;
  DataSetId?: number | null;
  SearchViewId?: number | null;
  Type?: number;
  SaasApplicationId?: number | string | null;
  AppSearchFieldList?: any[];
  DeletedItemsIds?: number[];
  IsModified?: boolean;
}

/** One row in Template Main Items: Data Model (usage 1) or Search (usage 3) link target. */
interface TemplateMainItemRow {
  Id?: number;
  Sort: number;
  linkTargetUsageType?: number;
  /** Data Model type (1): transaction id */
  TransactionId?: number | null;
  TransactionName?: string;
  NavigationActionName?: string;
  SourceViewColumnId1?: string | null;
  TargetColumn1?: string | null;
  /** Search type (3): target search id */
  LinkTargetSearchId?: number | null;
  DisplayText?: string;
  SourceViewColumnId2?: string | null;
  SourceViewColumnId3?: string | null;
  TargetSearchFieldId1?: number | null;
  TargetSearchFieldId2?: number | null;
  TargetSearchFieldId3?: number | null;
}

const PAGE_TITLE = 'Data Model Template Editor';
const TAB_ITEMS = 0;
const TAB_VIEW_FIELDS = 1;
const TAB_FILTERS = 2;
const TAB_FOLDER_NAV = 3;
const EmAppDataServiceType = {
  QueryText: 1,
  StoredProcedure: 2,
  PluginWebApiCall: 3,
  IntegrationWebApiCall: 4
} as const;
const queryTypeDisplayNames: Record<number, string> = {
  [EmAppDataServiceType.QueryText]: 'Query Service',
  [EmAppDataServiceType.StoredProcedure]: 'Stored Procedure Service',
  [EmAppDataServiceType.PluginWebApiCall]: 'Internal Web Api Call Service',
  [EmAppDataServiceType.IntegrationWebApiCall]: 'Integration Web Api Call Service'
};
const LINK_TARGET_SOURCE_COLUMN_SEARCH_VIEW_FIELD = 1;
const LINK_TARGET_ACTION_EDIT = 1;
const LINK_TARGET_ACTION_CREATE = 2;

function buildLinkTargetDtoFromTemplateItem(
  item: TemplateMainItemRow,
  searchViewId: number,
  templateItemType: number
): Record<string, unknown> {
  const isSearch = item.linkTargetUsageType === LINK_TARGET_USAGE_SEARCH;
  return {
    Id: item.Id ?? null,
    IsModified: true,
    SearchViewId: searchViewId,
    TransactionUnitId: null,
    SourceColumn1: null,
    SourceColumn2: null,
    SourceColumn3: null,
    TargetColumn1: isSearch ? null : item.TargetColumn1 ?? null,
    TargetColumn2: null,
    TargetColumn3: null,
    NavigationActionName: isSearch ? null : item.NavigationActionName ?? null,
    DisplayText: isSearch ? item.DisplayText ?? null : null,
    ActionType: isSearch ? null : LINK_TARGET_ACTION_EDIT,
    LinkTargetTransactionId: isSearch ? null : item.TransactionId ?? null,
    LinkTargetSearchId: isSearch ? item.LinkTargetSearchId ?? null : null,
    LinkTargetUsageType: isSearch ? LINK_TARGET_USAGE_SEARCH : LINK_TARGET_USAGE_FORM,
    SourceColumnType: LINK_TARGET_SOURCE_COLUMN_SEARCH_VIEW_FIELD,
    SourceViewColumnId1: item.SourceViewColumnId1 ?? null,
    SourceViewColumnId2: item.SourceViewColumnId2 ?? null,
    SourceViewColumnId3: item.SourceViewColumnId3 ?? null,
    TargetSearchFieldId1: isSearch ? item.TargetSearchFieldId1 ?? null : null,
    TargetSearchFieldId2: isSearch ? item.TargetSearchFieldId2 ?? null : null,
    TargetSearchFieldId3: isSearch ? item.TargetSearchFieldId3 ?? null : null,
    Sort: item.Sort ?? null,
    IsPopup: isSearch,
    PopupWidth: 1200,
    PopupHeight: 700,
    OtherSettingsDto: { TemplateItemType: templateItemType }
  };
}

function collectTemplateItemsForSave(
  mainItems: TemplateMainItemRow[],
  sharedItems: TemplateMainItemRow[],
  searchViewId: number
): { formItems: Record<string, unknown>[]; searchItems: Record<string, unknown>[] } {
  const formItems: Record<string, unknown>[] = [];
  const searchItems: Record<string, unknown>[] = [];

  const appendItems = (items: TemplateMainItemRow[], templateItemType: number) => {
    items.forEach((item) => {
      const dto = buildLinkTargetDtoFromTemplateItem(item, searchViewId, templateItemType);
      if (item.linkTargetUsageType === LINK_TARGET_USAGE_SEARCH) {
        searchItems.push(dto);
      } else {
        formItems.push(dto);
      }
    });
  };

  appendItems(mainItems, TEMPLATE_ITEM_TYPE_MAIN);
  appendItems(sharedItems, TEMPLATE_ITEM_TYPE_HEADER);
  return { formItems, searchItems };
}

const TransactionGroupEditor: React.FC = () => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  const emAppSearchUsageType = useEnumValues('EmAppSearchUsageType');
  const dataModelTemplateType = emAppSearchUsageType?.DataModelTemplate ?? 17;
  const emAppViewType = useEnumValues('EmAppViewType');
  const emAppControlType = useEnumValues('EmAppControlType');
  const emAppCriteriaOperatorType = useEnumValues('EmAppCriteriaOperatorType');

  const viewTypeList = useMemo(() => {
    if (!emAppViewType) return [];
    return Object.entries(emAppViewType).map(([key, id]) => ({ Id: id, Display: key }));
  }, [emAppViewType]);

  useEffect(() => {
    setTemplateViewTypeDataMap(viewTypeList.length ? new DataMap(viewTypeList, 'Id', 'Display') : null);
  }, [viewTypeList]);

  useEffect(() => {
    if (!emAppControlType) return setControlTypeList([]);
    setControlTypeList(Object.entries(emAppControlType).map(([key, id]) => ({ Id: id, Display: key })));
  }, [emAppControlType]);
  useEffect(() => {
    if (!emAppCriteriaOperatorType) return setCriteriaOperatorList([]);
    setCriteriaOperatorList(Object.entries(emAppCriteriaOperatorType).map(([key, id]) => ({ Id: id, Display: key })));
  }, [emAppCriteriaOperatorType]);

  const [paramObj, setParamObj] = useState<TransactionGroupEditorParam>({});
  const [currentSearch, setCurrentSearch] = useState<CurrentSearch>(() => ({
    Name: '',
    Description: '',
    DataSetId: null,
    SearchViewId: null,
    Type: 17,
    SaasApplicationId: null,
    AppSearchFieldList: [],
    IsModified: false
  }));
  const [allDataSet, setAllDataSet] = useState<DataSetItem[]>([]);
  const [dataSetCV, setDataSetCV] = useState<CollectionView<any> | null>(null);
  const [activeTabIndex, setActiveTabIndex] = useState(TAB_ITEMS);
  const [isLoading, setIsLoading] = useState(false);
  const [datasetListModalOpen, setDatasetListModalOpen] = useState(false);
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<{ Id: number; DataSourceName?: string }[]>([]);
  const [createDatasetDropdownOpen, setCreateDatasetDropdownOpen] = useState(false);
  const [createDatasetSubmenuType, setCreateDatasetSubmenuType] = useState<number | null>(null);
  const [createDatasetMenuPos, setCreateDatasetMenuPos] = useState<{ left: number; top: number } | null>(null);
  const [dataSetEditorOpen, setDataSetEditorOpen] = useState(false);
  const [dataSetEditorQueryType, setDataSetEditorQueryType] = useState<number>(EmAppDataServiceType.QueryText);
  const [dataSetEditorDataSourceId, setDataSetEditorDataSourceId] = useState<number | null>(null);
  const dataSetEditorLastSavedIdRef = useRef<number | null>(null);
  const createDatasetMenuRef = useRef<HTMLDivElement>(null);
  const createDatasetPlusBtnRef = useRef<HTMLButtonElement>(null);
  const [mainItems, setMainItems] = useState<TemplateMainItemRow[]>([]);
  const [sharedItems, setSharedItems] = useState<TemplateMainItemRow[]>([]);
  const [deletedLinkTargetIds, setDeletedLinkTargetIds] = useState<number[]>([]);
  const preservedCreateLinkTargetsRef = useRef<any[]>([]);
  const [transactionList, setTransactionList] = useState<{ Id: number; TransactionName?: string }[]>([]);
  const [viewFields, setViewFields] = useState<{ Id: number | string; Display?: string }[]>([]);
  const [transactionPkFieldsByTransactionId, setTransactionPkFieldsByTransactionId] = useState<Record<number, { DataBaseFieldName: string }[]>>({});
  const [appSearchList, setAppSearchList] = useState<{ Id: number; Display?: string }[]>([]);
  const [searchFieldsBySearchId, setSearchFieldsBySearchId] = useState<Record<number, { Id: number; Display?: string }[]>>({});
  const datasetGridRef = useRef<any>(null);

  // Template View Fields tab: views for current dataset
  const [templateViewList, setTemplateViewList] = useState<{ Id: number; Name?: string; Description?: string; ViewType?: number }[]>([]);
  const [templateViewCV, setTemplateViewCV] = useState<CollectionView<any> | null>(null);
  const [templateViewTypeDataMap, setTemplateViewTypeDataMap] = useState<DataMap | null>(null);
  const flexTemplateViewsRef = useRef<any>(null);

  // Template Filters tab: filter columns and grid
  const [filterColumnsList, setFilterColumnsList] = useState<any[]>([]);
  const [controlTypeList, setControlTypeList] = useState<{ Id: number; Display: string }[]>([]);
  const [criteriaOperatorList, setCriteriaOperatorList] = useState<{ Id: number; Display: string }[]>([]);
  const controlTypeDataMap = useMemo(() => (controlTypeList.length ? new DataMap(controlTypeList, 'Id', 'Display') : null), [controlTypeList]);
  const criteriaOperatorDataMap = useMemo(() => (criteriaOperatorList.length ? new DataMap(criteriaOperatorList, 'Id', 'Display') : null), [criteriaOperatorList]);
  const filterColumnsDataMap = useMemo(() => (filterColumnsList.length ? new DataMap(filterColumnsList, 'Id', 'Id') : null), [filterColumnsList]);
  const [entityListForFilter, setEntityListForFilter] = useState<{ Id: number; Display?: string; Name?: string; Code?: string }[]>([]);
  const entityListDataMap = useMemo(() => (entityListForFilter.length ? new DataMap(entityListForFilter, 'Id', 'Display') : null), [entityListForFilter]);
  const emAppSearchFieldSubControlType = useEnumValues('EmAppSearchFieldSubControlType');
  const emInternalCodeRegistration = useEnumValues('EmInternalCodeRegistration');
  const subControlTypeList = useMemo(() => (emAppSearchFieldSubControlType ? Object.entries(emAppSearchFieldSubControlType).map(([key, id]) => ({ Id: id, Display: key })) : []), [emAppSearchFieldSubControlType]);
  const internalCodeList = useMemo(() => (emInternalCodeRegistration ? Object.entries(emInternalCodeRegistration).map(([key, id]) => ({ Id: id, Display: key })) : []), [emInternalCodeRegistration]);
  const subControlTypeDataMap = useMemo(() => (subControlTypeList.length ? new DataMap(subControlTypeList, 'Id', 'Display') : null), [subControlTypeList]);
  const internalCodeDataMap = useMemo(() => (internalCodeList.length ? new DataMap(internalCodeList, 'Id', 'Display') : null), [internalCodeList]);
  /** Datasource Selector popup: which filter row is selecting entity; when set, modal is open */
  const [entitySelectionItem, setEntitySelectionItem] = useState<any | null>(null);
  const [datasourceSelectorSelectedId, setDatasourceSelectorSelectedId] = useState<number | null>(null);

  useEffect(() => {
    let p: TransactionGroupEditorParam = {};
    if (param) {
      try {
        p = JSON.parse(decodeURIComponent(param));
      } catch {
        p = { id: param };
      }
    }
    setParamObj(p);
    if (p.param2 != null) {
      try {
        const extra = typeof p.param2 === 'string' ? JSON.parse(p.param2) : p.param2;
        if (extra?.activeTab === TAB_FOLDER_NAV) {
          setActiveTabIndex(TAB_FOLDER_NAV);
        }
      } catch {
        /* ignore */
      }
    }
  }, [param]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    setIsLoading(true);
    try {
      const [dataSets, txData, appSearchResp, dsRegisterList] = await Promise.all([
        searchSvc.retrieveAllAppDataSetEntityDto(),
        appTransactionService.retrieveAllAppTransactions(false, '1', false),
        adminSvc.getMassEntitiesLookupItem('AppSearch'),
        adminSvc.getDataSourceRegisterList(false)
      ]);
      setDataSourceRegisterList(Array.isArray(dsRegisterList) ? dsRegisterList : []);
      const dsList = Array.isArray(dataSets) ? dataSets : [];
      setAllDataSet(dsList);
      setDataSetCV(new CollectionView<any>(dsList));
      setTransactionList(Array.isArray(txData) ? txData : []);
      const searchList = Array.isArray(appSearchResp) ? appSearchResp : (appSearchResp?.AppSearch ?? []);
      setAppSearchList(searchList);

      const groupId = paramObj.id != null && paramObj.id !== '' ? paramObj.id : null;
      const applicationId = paramObj.param1 ?? null;

      if (groupId) {
        const data = await appTransactionService.retrieveOneAppTransactionGroupExDto(groupId);
        if (data) {
          let appSearchFieldList = data.AppSearchFieldList ?? [];
          if ((!appSearchFieldList || appSearchFieldList.length === 0) && data.Id != null) {
            try {
              const searchData = await searchSvc.retrieveOneAppSearchExDto(String(data.Id));
              if (searchData?.AppSearchFieldList?.length) appSearchFieldList = searchData.AppSearchFieldList;
            } catch {
              /* Group and Search may be different entities; keep [] */
            }
          }
          setCurrentSearch({
            ...data,
            Name: data.Name ?? data.GroupName ?? '',
            Description: data.Description ?? '',
            DataSetId: data.DataSetId ?? null,
            SearchViewId: data.SearchViewId ?? null,
            Type: data.Type ?? dataModelTemplateType,
            SaasApplicationId: data.SaasApplicationId ?? null,
            AppSearchFieldList: appSearchFieldList,
            IsModified: false
          });
          const searchViewId = data.SearchViewId ?? null;
          if (searchViewId != null) {
            try {
              const [linkToFormData, linkToSearchRaw] = await Promise.all([
                searchSvc.retrieveOneSearchViewLinkTargetList(String(searchViewId), LINK_TARGET_USAGE_FORM),
                searchSvc.retrieveOneAppViewLinkedSeaechOrUrlExDto(String(searchViewId))
              ]);
              const formList = Array.isArray(linkToFormData) ? linkToFormData : [];
              preservedCreateLinkTargetsRef.current = formList.filter(
                (lt: any) => lt?.ActionType === LINK_TARGET_ACTION_CREATE,
              );
              const templateFormList = formList.filter(
                (lt: any) => lt?.ActionType !== LINK_TARGET_ACTION_CREATE,
              );
              let searchList: any[] = [];
              if (Array.isArray(linkToSearchRaw)) {
                searchList = linkToSearchRaw;
              } else if (linkToSearchRaw && typeof linkToSearchRaw === 'object') {
                searchList = linkToSearchRaw.ObjectList ?? linkToSearchRaw.AppViewLinkedSeaechOrUrlDtoList ?? linkToSearchRaw.Items ?? linkToSearchRaw.AppViewLinkedSearchOrUrlDtoList ?? [];
                if (!Array.isArray(searchList)) searchList = [];
              }
              const mainFormRows: TemplateMainItemRow[] = templateFormList
                .filter((lt: any) => (lt.OtherSettingsDto?.TemplateItemType ?? 1) === TEMPLATE_ITEM_TYPE_MAIN)
                .map((lt: any) => ({
                  Id: lt.Id,
                  Sort: lt.Sort ?? 0,
                  linkTargetUsageType: LINK_TARGET_USAGE_FORM,
                  TransactionId: lt.LinkTargetTransactionId ?? null,
                  NavigationActionName: lt.NavigationActionName ?? '',
                  SourceViewColumnId1: lt.SourceViewColumnId1 != null ? String(lt.SourceViewColumnId1) : null,
                  TargetColumn1: lt.TargetColumn1 != null ? String(lt.TargetColumn1) : null
                }));
              const mainSearchRows: TemplateMainItemRow[] = searchList
                .filter((lt: any) => lt != null && (lt.LinkTargetSearchId != null || lt.LinkTargetSearchId === 0))
                .filter((lt: any) => !lt.OtherSettingsDto?.TemplateItemType || lt.OtherSettingsDto.TemplateItemType === TEMPLATE_ITEM_TYPE_MAIN)
                .map((lt: any) => ({
                  Id: lt.Id,
                  Sort: lt.Sort ?? 0,
                  linkTargetUsageType: LINK_TARGET_USAGE_SEARCH,
                  LinkTargetSearchId: lt.LinkTargetSearchId ?? null,
                  DisplayText: lt.DisplayText ?? '',
                  SourceViewColumnId1: lt.SourceViewColumnId1 != null ? String(lt.SourceViewColumnId1) : null,
                  SourceViewColumnId2: lt.SourceViewColumnId2 != null ? String(lt.SourceViewColumnId2) : null,
                  SourceViewColumnId3: lt.SourceViewColumnId3 != null ? String(lt.SourceViewColumnId3) : null,
                  TargetSearchFieldId1: lt.TargetSearchFieldId1 ?? null,
                  TargetSearchFieldId2: lt.TargetSearchFieldId2 ?? null,
                  TargetSearchFieldId3: lt.TargetSearchFieldId3 ?? null
                }));
              const main = [...mainFormRows, ...mainSearchRows].sort((a, b) => (a.Sort ?? 0) - (b.Sort ?? 0));
              setMainItems(main);

              const sharedFormRows: TemplateMainItemRow[] = templateFormList
                .filter((lt: any) => lt.OtherSettingsDto?.TemplateItemType === TEMPLATE_ITEM_TYPE_HEADER)
                .map((lt: any) => ({
                  Id: lt.Id,
                  Sort: lt.Sort ?? 0,
                  linkTargetUsageType: LINK_TARGET_USAGE_FORM,
                  TransactionId: lt.LinkTargetTransactionId ?? null,
                  NavigationActionName: lt.NavigationActionName ?? '',
                  SourceViewColumnId1: lt.SourceViewColumnId1 != null ? String(lt.SourceViewColumnId1) : null,
                  TargetColumn1: lt.TargetColumn1 != null ? String(lt.TargetColumn1) : null
                }));
              const sharedSearchRows: TemplateMainItemRow[] = searchList
                .filter((lt: any) => lt != null && (lt.LinkTargetSearchId != null || lt.LinkTargetSearchId === 0))
                .filter((lt: any) => lt.OtherSettingsDto?.TemplateItemType === TEMPLATE_ITEM_TYPE_HEADER)
                .map((lt: any) => ({
                  Id: lt.Id,
                  Sort: lt.Sort ?? 0,
                  linkTargetUsageType: LINK_TARGET_USAGE_SEARCH,
                  LinkTargetSearchId: lt.LinkTargetSearchId ?? null,
                  DisplayText: lt.DisplayText ?? '',
                  SourceViewColumnId1: lt.SourceViewColumnId1 != null ? String(lt.SourceViewColumnId1) : null,
                  SourceViewColumnId2: lt.SourceViewColumnId2 != null ? String(lt.SourceViewColumnId2) : null,
                  SourceViewColumnId3: lt.SourceViewColumnId3 != null ? String(lt.SourceViewColumnId3) : null,
                  TargetSearchFieldId1: lt.TargetSearchFieldId1 ?? null,
                  TargetSearchFieldId2: lt.TargetSearchFieldId2 ?? null,
                  TargetSearchFieldId3: lt.TargetSearchFieldId3 ?? null
                }));
              const shared = [...sharedFormRows, ...sharedSearchRows].sort((a, b) => (a.Sort ?? 0) - (b.Sort ?? 0));
              setSharedItems(shared);
              setDeletedLinkTargetIds([]);
            } catch {
              setMainItems([]);
              setSharedItems([]);
              setDeletedLinkTargetIds([]);
            }
          } else {
            setMainItems([]);
            setSharedItems([]);
            setDeletedLinkTargetIds([]);
          }
        }
      } else {
        setCurrentSearch({
          Name: '',
          Description: '',
          DataSetId: null,
          SearchViewId: null,
          Type: dataModelTemplateType,
          SaasApplicationId: applicationId,
          AppSearchFieldList: [],
          IsModified: false
        });
        setMainItems([]);
        setSharedItems([]);
        setDeletedLinkTargetIds([]);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [paramObj.id, paramObj.param1, dataModelTemplateType, dispatch, showError]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (!createDatasetDropdownOpen) return;
      const menuEl = createDatasetMenuRef.current;
      const btnEl = createDatasetPlusBtnRef.current;
      const target = e.target as Node;
      if (menuEl?.contains(target) || btnEl?.contains(target)) return;
      setCreateDatasetDropdownOpen(false);
      setCreateDatasetSubmenuType(null);
      setCreateDatasetMenuPos(null);
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [createDatasetDropdownOpen]);

  useEffect(() => {
    if (param === undefined) loadData();
  }, []);
  useEffect(() => {
    if (param !== undefined && (paramObj.id !== undefined || paramObj.param1 !== undefined)) {
      loadData();
    }
  }, [param, paramObj.id, paramObj.param1]);

  useEffect(() => {
    if (currentSearch?.SearchViewId != null) {
      searchSvc
        .retrieveOneAppSearchViewExDto(String(currentSearch.SearchViewId))
        .then((view) => {
          const list = view?.AppSearchViewFieldList ?? [];
          const items = list.map((f: any) => ({
            Id: f.Id ?? f.SysTableFiledPath,
            Display: `${f.SysTableFiledPath ?? ''} (${f.DisplayText ?? f.Id ?? ''})`
          }));
          setViewFields(items);
        })
        .catch(() => setViewFields([]));
    } else if (currentSearch?.DataSetId != null) {
      searchSvc
        .retrieveQueryColumnList(String(currentSearch.DataSetId))
        .then((r) => {
          const arr = Array.isArray(r) ? r : [];
          setViewFields(arr.map((x: any) => ({ Id: x.Id ?? x.ColumnId ?? String(x), Display: x.Name ?? x.Display ?? x.ColumnName ?? String(x.Id ?? x) })));
        })
        .catch(() => setViewFields([]));
    } else {
      setViewFields([]);
    }
  }, [currentSearch?.SearchViewId, currentSearch?.DataSetId]);

  const loadTransactionPkFields = useCallback(
    async (transactionId: number) => {
      if (transactionPkFieldsByTransactionId[transactionId] != null) return;
      try {
        const data = await appTransactionService.getOneHierarchyTransaction(
          String(transactionId),
          false,
          '',
          '',
          '',
          false,
          ''
        );
        const fields: { DataBaseFieldName: string }[] = [];
        if (data?.AppTransactionUnitList?.[0]?.AppTransactionFieldList) {
          fields.push(...data.AppTransactionUnitList[0].AppTransactionFieldList);
        }
        setTransactionPkFieldsByTransactionId((prev) => ({ ...prev, [transactionId]: fields }));
      } catch {
        setTransactionPkFieldsByTransactionId((prev) => ({ ...prev, [transactionId]: [] }));
      }
    },
    [transactionPkFieldsByTransactionId]
  );

  useEffect(() => {
    [...mainItems, ...sharedItems].forEach((item) => {
      if (item.TransactionId != null) loadTransactionPkFields(item.TransactionId);
    });
  }, [mainItems, sharedItems, loadTransactionPkFields]);

  const loadSearchFields = useCallback(
    async (searchId: number) => {
      if (searchFieldsBySearchId[searchId] != null) return;
      try {
        const data = await searchSvc.retrieveOneAppSearchExDto(String(searchId));
        const list = data?.AppSearchFieldList ?? [];
        const items = list.map((f: any) => ({
          Id: f.Id,
          Display: `${f.SysTableFiledPath ?? ''} (${f.DisplayText ?? f.Id ?? ''})`
        }));
        setSearchFieldsBySearchId((prev) => ({ ...prev, [searchId]: items }));
      } catch {
        setSearchFieldsBySearchId((prev) => ({ ...prev, [searchId]: [] }));
      }
    },
    [searchFieldsBySearchId]
  );

  useEffect(() => {
    [...mainItems, ...sharedItems].forEach((item) => {
      if (item.linkTargetUsageType === LINK_TARGET_USAGE_SEARCH && item.LinkTargetSearchId != null) {
        loadSearchFields(item.LinkTargetSearchId);
      }
    });
  }, [mainItems, sharedItems, loadSearchFields]);

  // Load views for Template View Fields tab when tab active and dataset set
  useEffect(() => {
    if (activeTabIndex !== TAB_VIEW_FIELDS || !currentSearch?.DataSetId) {
      if (activeTabIndex !== TAB_VIEW_FIELDS) return;
      setTemplateViewList([]);
      setTemplateViewCV(new CollectionView<any>([]));
      return;
    }
    let cancelled = false;
    searchSvc.retrieveStatciSearchAvailableViewWithSameQueryBL(String(currentSearch.DataSetId)).then((list) => {
      if (cancelled) return;
      const arr = Array.isArray(list) ? list : [];
      setTemplateViewList(arr);
      setTemplateViewCV(new CollectionView<any>(arr));
    }).catch(() => {
      if (!cancelled) {
        setTemplateViewList([]);
        setTemplateViewCV(new CollectionView<any>([]));
      }
    });
    return () => { cancelled = true; };
  }, [activeTabIndex, currentSearch?.DataSetId]);

  // Load dataset columns for Template Filters tab when tab active and dataset set
  useEffect(() => {
    if (activeTabIndex !== TAB_FILTERS || !currentSearch?.DataSetId) {
      if (activeTabIndex !== TAB_FILTERS) return;
      setFilterColumnsList([]);
      return;
    }
    searchSvc.retrieveQueryColumnList(String(currentSearch.DataSetId)).then((cols) => {
      setFilterColumnsList(Array.isArray(cols) ? cols : []);
    }).catch(() => setFilterColumnsList([]));
  }, [activeTabIndex, currentSearch?.DataSetId]);

  // Load entity list for Template Filters "Entity List of Value" column when Filters tab active
  useEffect(() => {
    if (activeTabIndex !== TAB_FILTERS) return;
    adminSvc.getMassEntitiesLookupItem('AppEntityInfo').then((data: any) => {
      const raw = data?.AppEntityInfo ?? data;
      const arr = Array.isArray(raw) ? raw : [];
      setEntityListForFilter(arr.map((e: any) => ({ Id: e.Id, Display: e.Display ?? e.Name ?? e.Code ?? String(e.Id ?? '') })));
    }).catch(() => setEntityListForFilter([]));
  }, [activeTabIndex]);

  const updateMainItem = useCallback((idx: number, patch: Partial<TemplateMainItemRow>) => {
    setMainItems((prev) => prev.map((it, i) => (i === idx ? { ...it, ...patch } : it)));
    setCurrentSearch((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const handleAddDataModel = useCallback(() => {
    const nextSort = mainItems.length === 0 ? 1 : Math.max(...mainItems.map((m) => m.Sort), 0) + 1;
    setMainItems([
      ...mainItems,
      {
        Sort: nextSort,
        linkTargetUsageType: LINK_TARGET_USAGE_FORM,
        TransactionId: null,
        NavigationActionName: '',
        SourceViewColumnId1: null,
        TargetColumn1: null
      }
    ]);
  }, [mainItems]);
  const handleAddSearch = useCallback(() => {
    const nextSort = mainItems.length === 0 ? 1 : Math.max(...mainItems.map((m) => m.Sort), 0) + 1;
    setMainItems([
      ...mainItems,
      {
        Sort: nextSort,
        linkTargetUsageType: LINK_TARGET_USAGE_SEARCH,
        LinkTargetSearchId: null,
        DisplayText: '',
        SourceViewColumnId1: null,
        SourceViewColumnId2: null,
        SourceViewColumnId3: null,
        TargetSearchFieldId1: null,
        TargetSearchFieldId2: null,
        TargetSearchFieldId3: null
      }
    ]);
  }, [mainItems]);
  const handleDeleteMainItem = useCallback(
    (rowIndex: number) => {
      const item = mainItems[rowIndex];
      if (item?.Id != null) {
        setDeletedLinkTargetIds((prev) => (prev.includes(item.Id!) ? prev : [...prev, item.Id!]));
      }
      setMainItems(mainItems.filter((_, i) => i !== rowIndex));
      setCurrentSearch((prev) => (prev ? { ...prev, IsModified: true } : prev));
    },
    [mainItems]
  );

  const updateSharedItem = useCallback((idx: number, patch: Partial<TemplateMainItemRow>) => {
    setSharedItems((prev) => prev.map((it, i) => (i === idx ? { ...it, ...patch } : it)));
    setCurrentSearch((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const handleAddDataModelShared = useCallback(() => {
    const nextSort = sharedItems.length === 0 ? 1 : Math.max(...sharedItems.map((m) => m.Sort), 0) + 1;
    setSharedItems([
      ...sharedItems,
      {
        Sort: nextSort,
        linkTargetUsageType: LINK_TARGET_USAGE_FORM,
        TransactionId: null,
        NavigationActionName: '',
        SourceViewColumnId1: null,
        TargetColumn1: null
      }
    ]);
  }, [sharedItems]);

  const handleAddSearchShared = useCallback(() => {
    const nextSort = sharedItems.length === 0 ? 1 : Math.max(...sharedItems.map((m) => m.Sort), 0) + 1;
    setSharedItems([
      ...sharedItems,
      {
        Sort: nextSort,
        linkTargetUsageType: LINK_TARGET_USAGE_SEARCH,
        LinkTargetSearchId: null,
        DisplayText: '',
        SourceViewColumnId1: null,
        SourceViewColumnId2: null,
        SourceViewColumnId3: null,
        TargetSearchFieldId1: null,
        TargetSearchFieldId2: null,
        TargetSearchFieldId3: null
      }
    ]);
  }, [sharedItems]);

  const handleDeleteSharedItem = useCallback(
    (rowIndex: number) => {
      const item = sharedItems[rowIndex];
      if (item?.Id != null) {
        setDeletedLinkTargetIds((prev) => (prev.includes(item.Id!) ? prev : [...prev, item.Id!]));
      }
      setSharedItems(sharedItems.filter((_, i) => i !== rowIndex));
      setCurrentSearch((prev) => (prev ? { ...prev, IsModified: true } : prev));
    },
    [sharedItems]
  );

  const refresh = () => loadData();

  const createTemplateView = useCallback(() => {
    if (!currentSearch?.DataSetId) {
      showWarning('Select a dataset first');
      return;
    }
    const param2 = JSON.stringify({ callBackTabKey: null, saasApplicationId: currentSearch.SaasApplicationId || null });
    addTabAndNavigate('search-view-editor', 'New View', { param1: String(currentSearch.DataSetId), param2 }, true);
  }, [currentSearch?.DataSetId, currentSearch?.SaasApplicationId, addTabAndNavigate, showWarning]);

  const editTemplateView = useCallback((view: { Id: number; Name?: string }) => {
    if (!view?.Id || !currentSearch?.DataSetId) return;
    const param2 = JSON.stringify({ callBackTabKey: null, saasApplicationId: currentSearch.SaasApplicationId || null });
    addTabAndNavigate('search-view-editor', view.Name || `View ${view.Id}`, { id: view.Id, param1: String(currentSearch.DataSetId), param2 }, true);
  }, [currentSearch?.DataSetId, currentSearch?.SaasApplicationId, addTabAndNavigate]);

  const deleteTemplateView = useCallback(async (view: { Id: number }) => {
    if (!view?.Id) return;
    try {
      dispatch(setIsBusy());
      const data = await searchSvc.deleteAppSearchView(String(view.Id));
      if (data?.IsSuccessful !== false) {
        showInfo('View deleted');
        if (currentSearch?.SearchViewId === view.Id) {
          setCurrentSearch((prev) => (prev ? { ...prev, SearchViewId: null, IsModified: true } : prev));
        }
        if (currentSearch?.DataSetId) {
          const list = await searchSvc.retrieveStatciSearchAvailableViewWithSameQueryBL(String(currentSearch.DataSetId));
          const arr = Array.isArray(list) ? list : [];
          setTemplateViewList(arr);
          setTemplateViewCV(new CollectionView<any>(arr));
        }
      } else {
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
        showError(msgs.length ? msgs.join('; ') : 'Delete failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Delete failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch?.DataSetId, currentSearch?.SearchViewId, dispatch, showError, showInfo]);

  const setDefaultTemplateView = useCallback((viewId: number) => {
    setCurrentSearch((prev) => (prev ? { ...prev, SearchViewId: viewId, IsModified: true } : prev));
  }, []);

  const addTemplateFilter = useCallback(() => {
    const list = [...(currentSearch?.AppSearchFieldList ?? [])];
    const sort = Math.max(0, ...list.map((f: any) => (f.Sort ?? 0))) + 1;
    const nbPositionColumns = 2;
    const arrayLength = list.length;
    list.push({
      Sort: sort,
      SysTableFiledPath: null,
      DisplayText: '',
      ControlType: 2,
      EntityId: null,
      OperationId: 0,
      DefaultValue: '',
      PositionRow: Math.floor(arrayLength / nbPositionColumns) + 1,
      PositionColumn: (arrayLength % nbPositionColumns) + 1,
      IsVisible: true,
      IsReadOnly: false
    });
    setCurrentSearch((prev) => (prev ? { ...prev, AppSearchFieldList: list, IsModified: true } : prev));
  }, [currentSearch?.AppSearchFieldList]);

  const removeTemplateFilter = useCallback((item: any) => {
    const list = (currentSearch?.AppSearchFieldList ?? []).filter((f: any) => f !== item);
    setCurrentSearch((prev) => (prev ? { ...prev, AppSearchFieldList: list, IsModified: true } : prev));
  }, [currentSearch?.AppSearchFieldList]);

  const markTemplateFilterChange = useCallback(() => {
    setCurrentSearch((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  /** Display text for Entity List of Value by entity Id (from entityListForFilter). */
  const getEntityCodeById = useCallback(
    (entityId: number | null | undefined) => {
      if (entityId == null) return '';
      const found = entityListForFilter.find((e) => e.Id === entityId);
      return found?.Display ?? found?.Name ?? found?.Code ?? String(entityId);
    },
    [entityListForFilter]
  );

  /** Open Datasource Selector popup for this filter row (only when ControlType is DDL). */
  const openEntityInfoPopup = useCallback((item: any) => {
    if (item?.ControlType === 1) {
      setEntitySelectionItem(item);
      setDatasourceSelectorSelectedId(item?.EntityId ?? null);
    }
  }, []);

  /** Open Entity Data Preview in a new tab (entity-info-edit with Id). */
  const openEntityDataEditorPopup = useCallback(
    (entityId: number | null | undefined) => {
      if (entityId != null) {
        addTabAndNavigate('entity-data-preview', 'Entity Data Preview', { id: String(entityId) }, true);
      }
    },
    [addTabAndNavigate]
  );

  const datasourceSelectedHandler = useCallback(
    (entityId: number | null) => {
      if (entitySelectionItem) {
        entitySelectionItem.EntityId = entityId;
        entitySelectionItem.IsModified = true;
        setCurrentSearch((prev) => (prev ? { ...prev, AppSearchFieldList: [...(prev.AppSearchFieldList ?? [])], IsModified: true } : prev));
      }
      setEntitySelectionItem(null);
      setDatasourceSelectorSelectedId(null);
    },
    [entitySelectionItem]
  );

  const closeDatasourceSelectorPopup = useCallback(() => {
    setEntitySelectionItem(null);
    setDatasourceSelectorSelectedId(null);
  }, []);

  /** Angular saveLinkTarget: persist Template Main/Shared items before saveAppSearchExDto. */
  const saveLinkTargets = useCallback(
    async (searchViewId: number): Promise<boolean> => {
      const { formItems, searchItems } = collectTemplateItemsForSave(mainItems, sharedItems, searchViewId);
      const createNavItems = preservedCreateLinkTargetsRef.current.map((lt: any) => ({
        ...lt,
        SearchViewId: searchViewId,
        IsModified: false,
      }));
      const deletedIds = deletedLinkTargetIds;

      const linkTargetSaveObj = {
        SearchViewId: searchViewId,
        DeletedItemIds: deletedIds,
        AppFormLinkTargetDtoSet: [...formItems, ...createNavItems]
      };
      const linkedSearchSaveObj = {
        SearchViewId: searchViewId,
        DeletedItemIds: deletedIds,
        AppViewLinkedSeaechOrUrlDtoSet: searchItems
      };

      const [linkTargetsData, linkedSearchData] = await Promise.all([
        searchSvc.saveOneSearchViewLinkTargetList(linkTargetSaveObj),
        searchSvc.saveAllAppViewLinkedSeaechOrUrlEntityDto(linkedSearchSaveObj)
      ]);

      if (linkTargetsData?.ValidationResult) showValidationMessages(linkTargetsData.ValidationResult, true);
      if (linkedSearchData?.ValidationResult) showValidationMessages(linkedSearchData.ValidationResult, true);

      const linkTargetsOk = linkTargetsData?.IsSuccessful !== false;
      const linkedSearchOk = linkedSearchData?.IsSuccessful !== false;
      if (!linkTargetsOk || !linkedSearchOk) {
        const msgs = [
          ...(linkTargetsData?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage ?? i.LocalizedMessage).filter(Boolean) ?? []),
          ...(linkedSearchData?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage ?? i.LocalizedMessage).filter(Boolean) ?? [])
        ];
        showError(msgs.length ? msgs.join('; ') : 'Failed to save template items');
        return false;
      }
      return true;
    },
    [mainItems, sharedItems, deletedLinkTargetIds, showError, showValidationMessages]
  );

  const save = useCallback(async () => {
    if (!currentSearch) return;
    if (!currentSearch.Name?.trim()) {
      showWarning('Please enter a name');
      return;
    }
    dispatch(setIsBusy());
    try {
      const searchViewId = currentSearch.SearchViewId;
      const hasTemplateItemChanges =
        mainItems.length > 0 || sharedItems.length > 0 || deletedLinkTargetIds.length > 0;
      if (hasTemplateItemChanges && searchViewId == null) {
        showWarning('Select a default view in the Template View Fields tab before saving template items.');
        return;
      }
      if (searchViewId != null) {
        const linkTargetsSaved = await saveLinkTargets(searchViewId);
        if (!linkTargetsSaved) return;
      }

      const payload = {
        ...currentSearch,
        Name: currentSearch.Name.trim(),
        AppSearchFieldList: currentSearch.AppSearchFieldList ?? [],
        DeletedItemsIds: currentSearch.DeletedItemsIds ?? []
      };
      const data = await searchSvc.saveAppSearchExDto(payload);
      const savedId = data?.Object?.Id ?? data?.Id;
      if (data?.IsSuccessful && savedId != null) {
        showValidationMessages(data.ValidationResult, true);
        setCurrentSearch((prev: CurrentSearch) => (prev ? { ...prev, Id: savedId, IsModified: false } : prev));
        if (!paramObj.id) setParamObj((p) => ({ ...p, id: savedId }));
        setDeletedLinkTargetIds([]);
        await loadData();
      } else {
        if (data?.ValidationResult) showValidationMessages(data.ValidationResult, true);
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage ?? i.LocalizedMessage).filter(Boolean) ?? [];
        showError(msgs.length ? msgs.join('; ') : 'Save failed');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch, mainItems.length, sharedItems.length, deletedLinkTargetIds.length, paramObj.id, dispatch, loadData, saveLinkTargets, showError, showValidationMessages, showWarning]);

  const handleAdvancedOptions = useCallback(() => {
    if (!currentSearch?.Id) {
      showWarning('Save the template first to use Advanced Options.');
      return;
    }
    showInfo('Advanced Options – to be implemented (Usage Type, Embedded Child Search, etc.).');
  }, [currentSearch?.Id, showInfo, showWarning]);

  const handleSaveAs = useCallback(() => {
    if (!currentSearch?.Id) {
      showWarning('Save the template first to use Save As.');
      return;
    }
    addTabAndNavigate('transaction-group-editor', `${currentSearch.Name} (Copy)`, { param1: currentSearch.SaasApplicationId }, true);
    showInfo('Open new tab to save as new template. Change Name and Save.');
  }, [currentSearch?.Id, currentSearch?.Name, currentSearch?.SaasApplicationId, addTabAndNavigate, showInfo, showWarning]);

  const handleAddToMainMenu = useCallback(async () => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
      showWarning('Save the template and set a default view first.');
      return;
    }
    try {
      dispatch(setIsBusy());

      const searchDto = currentSearch;
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
        try {
          const userMenu = await adminSvc.retrieveUserTreeMenu();
          dispatch(setUserMenu(userMenu));
        } catch {
          // Menu refresh is best-effort after add
        }
      } else {
        showError('Failed to add to main menu');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to add to main menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentSearch, dispatch, showError, showInfo, showWarning]);

  const handleTestRun = useCallback(() => {
    if (!currentSearch?.Id || !currentSearch?.SearchViewId) {
      showWarning('Save the template and create a view first.');
      return;
    }
    addTabAndNavigate('masterdatamanagement', currentSearch.Name || 'Search', { searchId: currentSearch.Id, isSavedSearch: false }, true);
  }, [currentSearch?.Id, currentSearch?.SearchViewId, currentSearch?.Name, addTabAndNavigate, showWarning]);

  const currentDataSet = currentSearch?.DataSetId != null ? allDataSet.find((d) => d.Id === currentSearch.DataSetId) : null;

  const refreshDataSetList = useCallback(async () => {
    const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
    const dsList = Array.isArray(raw) ? raw : [];
    setAllDataSet(dsList);
    setDataSetCV(new CollectionView<any>(dsList));
    return dsList;
  }, []);

  const applyDataSetSelection = useCallback(
    async (dataSetId: number) => {
      await refreshDataSetList();
      setCurrentSearch((prev: CurrentSearch) => {
        if (!prev || prev.DataSetId === dataSetId) return prev;
        return { ...prev, DataSetId: dataSetId, SearchViewId: null, IsModified: true };
      });
      setMainItems([]);
      setSharedItems([]);
      setDeletedLinkTargetIds([]);
    },
    [refreshDataSetList]
  );

  const handleEditDataSet = useCallback(() => {
    if (currentDataSet?.Id != null) {
      addTabAndNavigate('dataset-editor', currentDataSet.Name || 'Dataset', { id: currentDataSet.Id }, true);
    }
  }, [currentDataSet, addTabAndNavigate]);

  /** Angular: plus shown when one data source, or multiple sources on unsaved template only. */
  const showCreateDatasetButton =
    dataSourceRegisterList.length === 1 ||
    (dataSourceRegisterList.length >= 2 && !currentSearch?.Id);

  const formatDataSourceLabel = useCallback((ds: { Id: number; DataSourceName?: string }) => {
    const name = ds.Id === 2147483647 ? 'Master DB' : ds.DataSourceName ?? String(ds.Id);
    return `On ${name} (${ds.Id})`;
  }, []);

  const openCreateDatasetEditor = useCallback(
    (queryType: number, dataSourceRegisterId: number | null) => {
      if (!currentSearch?.Name?.trim()) {
        showWarning("Please fill the 'Name' field first.");
        return;
      }
      if (dataSourceRegisterId == null) {
        showWarning('Please select a data source first.');
        return;
      }
      setCreateDatasetDropdownOpen(false);
      setCreateDatasetSubmenuType(null);
      setCreateDatasetMenuPos(null);
      setDataSetEditorQueryType(queryType);
      setDataSetEditorDataSourceId(dataSourceRegisterId);
      dataSetEditorLastSavedIdRef.current = null;
      setDataSetEditorOpen(true);
    },
    [currentSearch?.Name, showWarning]
  );

  const toggleCreateDatasetMenu = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    if (createDatasetDropdownOpen) {
      setCreateDatasetDropdownOpen(false);
      setCreateDatasetSubmenuType(null);
      setCreateDatasetMenuPos(null);
      return;
    }
    const rect = createDatasetPlusBtnRef.current?.getBoundingClientRect();
    if (rect) {
      setCreateDatasetMenuPos({ left: rect.right, top: rect.bottom + 2 });
    }
    setCreateDatasetDropdownOpen(true);
  }, [createDatasetDropdownOpen]);

  const renderCreateDatasetMenuItem = (queryType: number, withSubmenu: boolean) => {
    const label = queryTypeDisplayNames[queryType];
    if (!withSubmenu) {
      const dsId = dataSourceRegisterList[0]?.Id ?? null;
      return (
        <button
          key={queryType}
          type="button"
          onClick={() => openCreateDatasetEditor(queryType, dsId)}
          className={`w-full text-left px-3 py-2 text-xs whitespace-nowrap ${theme.contextMenu}`}
        >
          {label}
        </button>
      );
    }
    if (queryType === EmAppDataServiceType.IntegrationWebApiCall) {
      return (
        <button
          key={queryType}
          type="button"
          onClick={() => openCreateDatasetEditor(queryType, dataSourceRegisterList[0]?.Id ?? null)}
          className={`w-full text-left px-3 py-2 text-xs whitespace-nowrap ${theme.contextMenu}`}
        >
          {label}
        </button>
      );
    }
    return (
      <div key={queryType} className="relative">
        <button
          type="button"
          className={`w-full text-left px-3 py-2 text-xs flex items-center justify-between gap-2 whitespace-nowrap ${theme.contextMenu}`}
          onMouseEnter={() => setCreateDatasetSubmenuType(queryType)}
          onMouseLeave={() => setCreateDatasetSubmenuType((prev) => (prev === queryType ? null : prev))}
        >
          <span>{label}</span>
          <i className="fa-solid fa-angle-right text-[10px] shrink-0" aria-hidden />
        </button>
        {createDatasetSubmenuType === queryType && (
          <div
            className={`absolute left-full top-0 ml-1 min-w-[220px] rounded shadow-lg border z-[210] ${theme.mainContentSection}`}
            onMouseEnter={() => setCreateDatasetSubmenuType(queryType)}
            onMouseLeave={() => setCreateDatasetSubmenuType(null)}
          >
            <div className="py-1">
              {dataSourceRegisterList.map((ds) => (
                <button
                  key={`${queryType}-${ds.Id}`}
                  type="button"
                  onClick={() => openCreateDatasetEditor(queryType, ds.Id)}
                  className={`w-full text-left px-3 py-2 text-xs whitespace-nowrap ${theme.contextMenu}`}
                >
                  {formatDataSourceLabel(ds)}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  };

  const handleDataSetEditorSaved = useCallback(async (savedId: number) => {
    await applyDataSetSelection(savedId);
    dataSetEditorLastSavedIdRef.current = null;
    setDataSetEditorOpen(false);
  }, [applyDataSetSelection]);

  const closeDataSetEditor = useCallback(async () => {
    const lastId = dataSetEditorLastSavedIdRef.current;
    if (lastId != null && Number.isFinite(lastId)) {
      await applyDataSetSelection(lastId);
    }
    dataSetEditorLastSavedIdRef.current = null;
    setDataSetEditorOpen(false);
  }, [applyDataSetSelection]);

  if (isLoading && paramObj.id != null && currentSearch?.Id == null) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-4">Loading...</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>{PAGE_TITLE}</div>
        <div className="flex items-center gap-2 flex-wrap">
          <button type="button" onClick={refresh} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={save} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}>
            <i className="fa-solid fa-save" aria-hidden /> Save
          </button>
          <button type="button" onClick={handleAdvancedOptions} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}>
            <i className="fa-solid fa-cogs" aria-hidden /> Advanced Options
          </button>
          <button type="button" onClick={handleSaveAs} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}>
            <i className="fa-solid fa-window-restore" aria-hidden /> Save As
          </button>
          <button type="button" onClick={handleAddToMainMenu} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}>
            <i className="fa-solid fa-plus" aria-hidden /><i className="fa-solid fa-bars text-[10px] relative -left-1 top-0.5" aria-hidden /> Add To Main Menu
          </button>
          <button type="button" onClick={handleTestRun} className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}>
            <i className="fa-solid fa-play" aria-hidden /> Test Run
          </button>
        </div>
      </div>

      <div className={`flex flex-wrap items-center gap-4 px-5 py-5 ${theme.mainContentSection}`}>
        <div className="flex items-center py-1">
          <label className={`w-32 text-xs ${theme.label} mr-2 shrink-0`}>Name</label>
          <input
            type="text"
            autoComplete="off"
            className={`flex-auto min-w-[200px] h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={currentSearch?.Name ?? ''}
            onChange={(e) => setCurrentSearch((prev: CurrentSearch) => (prev ? { ...prev, Name: e.target.value, IsModified: true } : prev))}
          />
          {!currentSearch?.Name?.trim() && <span className={`text-xs ml-2 ${theme.label}`}>Please enter a name</span>}
        </div>
        <div className="flex items-center py-1">
          <label className={`w-32 text-xs ${theme.label} mr-2 shrink-0`}>Dataset</label>
          <div className={`flex items-stretch min-w-[200px] h-7 rounded-[4px] border ${theme.inputBox}`}>
            <div
              className={`flex items-center gap-1.5 flex-1 min-w-0 pl-2.5 pr-2 text-xs rounded-l-[4px] ${theme.inputBox} ${theme.title} overflow-hidden text-ellipsis whitespace-nowrap bg-transparent`}
              title={currentDataSet?.Name ?? 'Select dataset'}
            >
              {currentDataSet ? (
                <button
                  type="button"
                  onClick={handleEditDataSet}
                  className={`text-left truncate flex items-center gap-1.5 min-w-0 flex-1 py-0.5 rounded-sm hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset ${theme.title} underline decoration-1 underline-offset-2`}
                >
                  <i className="fa-solid fa-pencil shrink-0 opacity-80" aria-hidden />
                  <span className="truncate">{currentDataSet.Name}</span>
                </button>
              ) : (
                <span className="truncate">Select dataset</span>
              )}
            </div>
            <button
              type="button"
              onClick={() => setDatasetListModalOpen(true)}
              title="Select dataset"
              className={`w-9 shrink-0 flex items-center justify-center border-l ${theme.inputBox} ${theme.button_default} hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset min-w-0`}
              aria-label="Select dataset"
            >
              <i className="fa-solid fa-chevron-down text-[10px] opacity-80" aria-hidden />
            </button>
            {showCreateDatasetButton && (
              <button
                ref={createDatasetPlusBtnRef}
                type="button"
                onClick={toggleCreateDatasetMenu}
                title="Create dataset"
                className={`w-9 shrink-0 flex items-center justify-center border-l rounded-r-[4px] ${theme.inputBox} ${theme.button_default} hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset min-w-0`}
                aria-label="Create dataset"
                aria-expanded={createDatasetDropdownOpen}
              >
                <i className="fa-solid fa-plus text-[10px] opacity-80" aria-hidden />
              </button>
            )}
          </div>
          {currentSearch?.Name?.trim() && currentSearch?.DataSetId == null && (
            <span className={`text-xs ml-2 ${theme.label}`}>Please select or create a dataset</span>
          )}
        </div>
        <div className="flex items-center py-1">
          <label className={`w-32 text-xs ${theme.label} mr-2 shrink-0`}>Description</label>
          <input
            type="text"
            autoComplete="off"
            className={`flex-auto min-w-[200px] h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={currentSearch?.Description ?? ''}
            onChange={(e) => setCurrentSearch((prev: CurrentSearch) => (prev ? { ...prev, Description: e.target.value, IsModified: true } : prev))}
          />
        </div>
      </div>

      {currentSearch?.DataSetId && (
        <div className={`w-full h-1 flex-auto flex flex-col min-h-0 overflow-hidden px-5 py-5 ${theme.mainContentSection}`}>
          <div className={`flex border-b-2 ${theme.mainContentSection}`}>
            <button
              type="button"
              onClick={() => setActiveTabIndex(TAB_ITEMS)}
              className={`px-5 py-2.5 text-sm font-semibold border-b-2 -mb-0.5 ${
                activeTabIndex === TAB_ITEMS ? theme.tab_active : `opacity-70 hover:opacity-100 ${theme.label} border-transparent`
              }`}
            >
              Template Items
            </button>
            <button
              type="button"
              onClick={() => setActiveTabIndex(TAB_VIEW_FIELDS)}
              className={`px-5 py-2.5 text-sm font-semibold border-b-2 -mb-0.5 ${
                activeTabIndex === TAB_VIEW_FIELDS ? theme.tab_active : `opacity-70 hover:opacity-100 ${theme.label} border-transparent`
              }`}
            >
              Template View Fields
            </button>
            <button
              type="button"
              onClick={() => setActiveTabIndex(TAB_FILTERS)}
              className={`px-5 py-2.5 text-sm font-semibold border-b-2 -mb-0.5 ${
                activeTabIndex === TAB_FILTERS ? theme.tab_active : `opacity-70 hover:opacity-100 ${theme.label} border-transparent`
              }`}
            >
              Template Filters
            </button>
            <button
              type="button"
              onClick={() => setActiveTabIndex(TAB_FOLDER_NAV)}
              className={`px-5 py-2.5 text-sm font-semibold border-b-2 -mb-0.5 ${
                activeTabIndex === TAB_FOLDER_NAV ? theme.tab_active : `opacity-70 hover:opacity-100 ${theme.label} border-transparent`
              }`}
            >
              Folder Navigation
            </button>
          </div>

          <div className="h-1 flex-auto min-h-0 overflow-auto flex flex-col w-full">
          {activeTabIndex === TAB_ITEMS && (
            <>
              <section className={`w-full border-b ${theme.inputBox}`}>
                <div className={`flex items-center justify-between py-3 ${theme.mainContentSection}`}>
                  <div className={`text-sm font-semibold ${theme.title}`}>Template Main Items</div>
                  <div className="flex items-center gap-4">
                    <button type="button" onClick={handleAddDataModel} className={`text-sm underline ${theme.title} hover:opacity-90`}>
                      + Add Data Model
                    </button>
                    <button type="button" onClick={handleAddSearch} className={`text-sm underline ${theme.title} hover:opacity-90`}>
                      + Add Search
                    </button>
                  </div>
                </div>
                <div className="space-y-4 min-w-max pb-4">
                  {mainItems.map((item, idx) => {
                    const isSearchType = item.linkTargetUsageType === LINK_TARGET_USAGE_SEARCH;
                    const searchFields = item.LinkTargetSearchId != null ? (searchFieldsBySearchId[item.LinkTargetSearchId] ?? []) : [];
                    return (
                      <div
                        key={idx}
                        className={
                          isSearchType
                            ? `grid py-3 pr-2 border-b ${theme.mainContentSection}`
                            : `flex flex-nowrap items-end gap-0 py-3 pr-2 border-b ${theme.mainContentSection}`
                        }
                        style={
                          isSearchType
                            ? {
                                gridTemplateColumns: '60px 140px 250px 40px 250px 40px 250px 40px 250px',
                                gridTemplateRows: 'auto auto',
                                rowGap: '12px',
                                columnGap: 0
                              }
                            : undefined
                        }
                      >
                        <div
                          className={
                            isSearchType
                              ? 'flex items-center justify-center self-center'
                              : 'flex items-center justify-center w-[60px] shrink-0 pb-1'
                          }
                          style={isSearchType ? { gridColumn: 1, gridRow: '1 / -1' } : undefined}
                        >
                          <button
                            type="button"
                            onClick={() => handleDeleteMainItem(idx)}
                            className={`p-1.5 rounded ${theme.button_default}`}
                            aria-label="Delete"
                          >
                            <i className={`fa-solid fa-trash text-xs ${theme.label}`} aria-hidden />
                          </button>
                        </div>
                        <div
                          className={
                            isSearchType
                              ? 'flex flex-col self-center'
                              : 'flex flex-col w-[140px] shrink-0'
                          }
                          style={isSearchType ? { gridColumn: 2, gridRow: '1 / -1' } : undefined}
                        >
                          <label className={`text-xs ${theme.label} mb-1`}>Sort Order</label>
                          <div className="flex items-center border rounded overflow-hidden h-7 w-full max-w-[100px]">
                            <button
                              type="button"
                              onClick={() => updateMainItem(idx, { Sort: Math.max(1, (item.Sort ?? 1) - 1) })}
                              className={`w-6 h-full flex items-center justify-center border-r ${theme.button_default} text-xs`}
                            >
                              -
                            </button>
                            <input
                              type="number"
                              min={1}
                              value={item.Sort ?? 1}
                              onChange={(e) => updateMainItem(idx, { Sort: parseInt(String(e.target.value), 10) || 1 })}
                              className={`w-12 h-full text-center text-xs border-0 focus:outline-none focus:ring-1 focus:ring-inset ${theme.inputBox}`}
                            />
                            <button
                              type="button"
                              onClick={() => updateMainItem(idx, { Sort: (item.Sort ?? 1) + 1 })}
                              className={`w-6 h-full flex items-center justify-center border-l ${theme.button_default} text-xs`}
                            >
                              +
                            </button>
                          </div>
                        </div>
                        {isSearchType ? (
                          <>
                            {/* Row 1: Search, View Field 1, View Field 2, View Field 3 */}
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 3, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Search</label>
                              <select
                                value={item.LinkTargetSearchId ?? ''}
                                onChange={(e) =>
                                  updateMainItem(idx, {
                                    LinkTargetSearchId: e.target.value ? Number(e.target.value) : null,
                                    TargetSearchFieldId1: null,
                                    TargetSearchFieldId2: null,
                                    TargetSearchFieldId3: null
                                  })
                                }
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {appSearchList.map((s) => (
                                  <option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 5, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 1</label>
                              <select
                                value={item.SourceViewColumnId1 ?? ''}
                                onChange={(e) => updateMainItem(idx, { SourceViewColumnId1: e.target.value || null })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {viewFields.map((c) => (
                                  <option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 7, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 2</label>
                              <select
                                value={item.SourceViewColumnId2 ?? ''}
                                onChange={(e) => updateMainItem(idx, { SourceViewColumnId2: e.target.value || null })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {viewFields.map((c) => (
                                  <option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 9, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 3</label>
                              <select
                                value={item.SourceViewColumnId3 ?? ''}
                                onChange={(e) => updateMainItem(idx, { SourceViewColumnId3: e.target.value || null })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {viewFields.map((c) => (
                                  <option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>
                                ))}
                              </select>
                            </div>
                            {/* Row 2: Display Name, Mapping 1, Mapping 2, Mapping 3 */}
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 3, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Display Name</label>
                              <input
                                type="text"
                                value={item.DisplayText ?? ''}
                                onChange={(e) => updateMainItem(idx, { DisplayText: e.target.value })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                                autoComplete="off"
                              />
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 5, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 1</label>
                              <select
                                value={item.TargetSearchFieldId1 ?? ''}
                                onChange={(e) =>
                                  updateMainItem(idx, { TargetSearchFieldId1: e.target.value ? Number(e.target.value) : null })
                                }
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {searchFields.map((f) => (
                                  <option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 7, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 2</label>
                              <select
                                value={item.TargetSearchFieldId2 ?? ''}
                                onChange={(e) =>
                                  updateMainItem(idx, { TargetSearchFieldId2: e.target.value ? Number(e.target.value) : null })
                                }
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {searchFields.map((f) => (
                                  <option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 9, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 3</label>
                              <select
                                value={item.TargetSearchFieldId3 ?? ''}
                                onChange={(e) =>
                                  updateMainItem(idx, { TargetSearchFieldId3: e.target.value ? Number(e.target.value) : null })
                                }
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {searchFields.map((f) => (
                                  <option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>
                                ))}
                              </select>
                            </div>
                          </>
                        ) : (
                          <>
                            <div className="flex flex-col w-[250px] shrink-0">
                              <label className={`text-xs ${theme.label} mb-1`}>Data Model</label>
                              <select
                                value={item.TransactionId ?? ''}
                                onChange={(e) => {
                                  const id = e.target.value ? Number(e.target.value) : null;
                                  updateMainItem(idx, { TransactionId: id, TargetColumn1: null });
                                }}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {transactionList.map((t) => (
                                  <option key={t.Id} value={t.Id}>{t.TransactionName ?? (t as { Display?: string }).Display ?? t.Id}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>Display Name</label>
                              <input
                                type="text"
                                value={item.NavigationActionName ?? ''}
                                onChange={(e) => updateMainItem(idx, { NavigationActionName: e.target.value })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                                autoComplete="off"
                              />
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>View Field Mapping To Data Model PK</label>
                              <select
                                value={item.SourceViewColumnId1 ?? ''}
                                onChange={(e) => updateMainItem(idx, { SourceViewColumnId1: e.target.value || null })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {viewFields.map((c) => (
                                  <option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>
                                ))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>Data Model PK</label>
                              <select
                                value={item.TargetColumn1 ?? ''}
                                onChange={(e) => updateMainItem(idx, { TargetColumn1: e.target.value || null })}
                                className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}
                              >
                                <option value="">—</option>
                                {(item.TransactionId != null ? (transactionPkFieldsByTransactionId[item.TransactionId] ?? []) : []).map((f) => (
                                  <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                                ))}
                              </select>
                            </div>
                          </>
                        )}
                      </div>
                    );
                  })}
                </div>
              </section>
              <section className="w-full">
                <div className={`flex items-center justify-between py-3 ${theme.mainContentSection}`}>
                  <div className={`text-sm font-semibold ${theme.title}`}>Template Shared Items</div>
                  <div className="flex items-center gap-4">
                    <button type="button" onClick={handleAddDataModelShared} className={`text-sm underline ${theme.title} hover:opacity-90`}>
                      + Add Data Model
                    </button>
                    <button type="button" onClick={handleAddSearchShared} className={`text-sm underline ${theme.title} hover:opacity-90`}>
                      + Add Search
                    </button>
                  </div>
                </div>
                <div className="space-y-4 min-w-max pb-4">
                  {sharedItems.map((item, idx) => {
                    const isSearchType = item.linkTargetUsageType === LINK_TARGET_USAGE_SEARCH;
                    const searchFields = item.LinkTargetSearchId != null ? (searchFieldsBySearchId[item.LinkTargetSearchId] ?? []) : [];
                    return (
                      <div
                        key={idx}
                        className={
                          isSearchType
                            ? `grid py-3 pr-2 border-b ${theme.mainContentSection}`
                            : `flex flex-nowrap items-end gap-0 py-3 pr-2 border-b ${theme.mainContentSection}`
                        }
                        style={
                          isSearchType
                            ? {
                                gridTemplateColumns: '60px 140px 250px 40px 250px 40px 250px 40px 250px',
                                gridTemplateRows: 'auto auto',
                                rowGap: '12px',
                                columnGap: 0
                              }
                            : undefined
                        }
                      >
                        <div
                          className={isSearchType ? 'flex items-center justify-center self-center' : 'flex items-center justify-center w-[60px] shrink-0 pb-1'}
                          style={isSearchType ? { gridColumn: 1, gridRow: '1 / -1' } : undefined}
                        >
                          <button type="button" onClick={() => handleDeleteSharedItem(idx)} className={`p-1.5 rounded ${theme.button_default}`} aria-label="Delete">
                            <i className={`fa-solid fa-trash text-xs ${theme.label}`} aria-hidden />
                          </button>
                        </div>
                        <div
                          className={isSearchType ? 'flex flex-col self-center' : 'flex flex-col w-[140px] shrink-0'}
                          style={isSearchType ? { gridColumn: 2, gridRow: '1 / -1' } : undefined}
                        >
                          <label className={`text-xs ${theme.label} mb-1`}>Sort Order</label>
                          <div className="flex items-center border rounded overflow-hidden h-7 w-full max-w-[100px]">
                            <button type="button" onClick={() => updateSharedItem(idx, { Sort: Math.max(1, (item.Sort ?? 1) - 1) })} className={`w-6 h-full flex items-center justify-center border-r ${theme.button_default} text-xs`}>-</button>
                            <input type="number" min={1} value={item.Sort ?? 1} onChange={(e) => updateSharedItem(idx, { Sort: parseInt(String(e.target.value), 10) || 1 })} className={`w-12 h-full text-center text-xs border-0 focus:outline-none focus:ring-1 focus:ring-inset ${theme.inputBox}`} />
                            <button type="button" onClick={() => updateSharedItem(idx, { Sort: (item.Sort ?? 1) + 1 })} className={`w-6 h-full flex items-center justify-center border-l ${theme.button_default} text-xs`}>+</button>
                          </div>
                        </div>
                        {isSearchType ? (
                          <>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 3, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Search</label>
                              <select value={item.LinkTargetSearchId ?? ''} onChange={(e) => updateSharedItem(idx, { LinkTargetSearchId: e.target.value ? Number(e.target.value) : null, TargetSearchFieldId1: null, TargetSearchFieldId2: null, TargetSearchFieldId3: null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {appSearchList.map((s) => (<option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 5, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 1</label>
                              <select value={item.SourceViewColumnId1 ?? ''} onChange={(e) => updateSharedItem(idx, { SourceViewColumnId1: e.target.value || null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {viewFields.map((c) => (<option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 7, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 2</label>
                              <select value={item.SourceViewColumnId2 ?? ''} onChange={(e) => updateSharedItem(idx, { SourceViewColumnId2: e.target.value || null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {viewFields.map((c) => (<option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 9, gridRow: 1 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>View Field 3</label>
                              <select value={item.SourceViewColumnId3 ?? ''} onChange={(e) => updateSharedItem(idx, { SourceViewColumnId3: e.target.value || null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {viewFields.map((c) => (<option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 3, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Display Name</label>
                              <input type="text" value={item.DisplayText ?? ''} onChange={(e) => updateSharedItem(idx, { DisplayText: e.target.value })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`} autoComplete="off" />
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 5, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 1</label>
                              <select value={item.TargetSearchFieldId1 ?? ''} onChange={(e) => updateSharedItem(idx, { TargetSearchFieldId1: e.target.value ? Number(e.target.value) : null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {searchFields.map((f) => (<option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 7, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 2</label>
                              <select value={item.TargetSearchFieldId2 ?? ''} onChange={(e) => updateSharedItem(idx, { TargetSearchFieldId2: e.target.value ? Number(e.target.value) : null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {searchFields.map((f) => (<option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px]" style={{ gridColumn: 9, gridRow: 2 }}>
                              <label className={`text-xs ${theme.label} mb-1`}>Mapping To Search Field 3</label>
                              <select value={item.TargetSearchFieldId3 ?? ''} onChange={(e) => updateSharedItem(idx, { TargetSearchFieldId3: e.target.value ? Number(e.target.value) : null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {searchFields.map((f) => (<option key={f.Id} value={f.Id}>{f.Display ?? f.Id}</option>))}
                              </select>
                            </div>
                          </>
                        ) : (
                          <>
                            <div className="flex flex-col w-[250px] shrink-0">
                              <label className={`text-xs ${theme.label} mb-1`}>Data Model</label>
                              <select value={item.TransactionId ?? ''} onChange={(e) => { const id = e.target.value ? Number(e.target.value) : null; updateSharedItem(idx, { TransactionId: id, TargetColumn1: null }); }} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {transactionList.map((t) => (<option key={t.Id} value={t.Id}>{t.TransactionName ?? (t as { Display?: string }).Display ?? t.Id}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>Display Name</label>
                              <input type="text" value={item.NavigationActionName ?? ''} onChange={(e) => updateSharedItem(idx, { NavigationActionName: e.target.value })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`} autoComplete="off" />
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>View Field Mapping To Data Model PK</label>
                              <select value={item.SourceViewColumnId1 ?? ''} onChange={(e) => updateSharedItem(idx, { SourceViewColumnId1: e.target.value || null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {viewFields.map((c) => (<option key={String(c.Id)} value={String(c.Id)}>{c.Display ?? String(c.Id)}</option>))}
                              </select>
                            </div>
                            <div className="flex flex-col w-[250px] shrink-0 ml-[40px]">
                              <label className={`text-xs ${theme.label} mb-1`}>Data Model PK</label>
                              <select value={item.TargetColumn1 ?? ''} onChange={(e) => updateSharedItem(idx, { TargetColumn1: e.target.value || null })} className={`h-7 px-2 text-xs border rounded w-full ${theme.inputBox} focus:outline-none`}>
                                <option value="">—</option>
                                {(item.TransactionId != null ? (transactionPkFieldsByTransactionId[item.TransactionId] ?? []) : []).map((f) => (<option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>))}
                              </select>
                            </div>
                          </>
                        )}
                      </div>
                    );
                  })}
                </div>
              </section>
            </>
          )}

          {activeTabIndex === TAB_VIEW_FIELDS && (
            <div className="w-full h-1 flex-auto flex flex-col min-h-0 overflow-hidden">
              {!currentSearch?.DataSetId ? (
                <div className={`py-4 ${theme.title}`}>Select a dataset above to manage views.</div>
              ) : (
                <>
                  <div className={`flex items-center justify-between mb-1 px-3 ${theme.mainContentSection}`}>
                    <div className={`text-sm font-semibold ${theme.title}`}>Views</div>
                    <div className="flex items-center gap-2">
                      <button type="button" onClick={createTemplateView} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                        <i className="fa-solid fa-plus mr-1" aria-hidden /> Create View
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          const flex = flexTemplateViewsRef.current?.control ?? flexTemplateViewsRef.current;
                          const row = flex?.selection?.row ?? -1;
                          const item = row >= 0 && flex?.rows?.[row] ? flex.rows[row].dataItem : null;
                          if (item) editTemplateView(item);
                          else showWarning('Select a view to edit');
                        }}
                        className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                      >
                        <i className="fa-solid fa-pen-to-square mr-1" aria-hidden /> Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          const flex = flexTemplateViewsRef.current?.control ?? flexTemplateViewsRef.current;
                          const row = flex?.selection?.row ?? -1;
                          const item = row >= 0 && flex?.rows?.[row] ? flex.rows[row].dataItem : null;
                          if (item) deleteTemplateView(item);
                          else showWarning('Select a view to delete');
                        }}
                        className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                      >
                        <i className="fa-solid fa-trash mr-1" aria-hidden /> Delete
                      </button>
                    </div>
                  </div>
                  <div className={`w-full flex-1 min-h-[120px] overflow-hidden ${theme.mainContentSection}`}>
                    {templateViewCV && (
                      <FlexGrid
                        ref={flexTemplateViewsRef}
                        itemsSource={templateViewCV}
                        selectionMode="Row"
                        isReadOnly={true}
                        style={{ height: '100%', border: 'none' }}
                      >
                        <FlexGridColumn width={50}>
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item;
                              if (!item) return null;
                              return (
                                <div className="flex justify-center">
                                  <button type="button" onClick={() => editTemplateView(item)} className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`} title="Edit">
                                    <i className="fa-solid fa-pen-to-square" aria-hidden />
                                  </button>
                                </div>
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn width={70} header="Default">
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item;
                              if (!item) return null;
                              const isDefault = currentSearch?.SearchViewId === item.Id;
                              return (
                                <div className="flex justify-center cursor-pointer" onClick={() => setDefaultTemplateView(item.Id)} role="button" tabIndex={0} onKeyDown={(e) => e.key === 'Enter' && setDefaultTemplateView(item.Id)}>
                                  <i className={`fa-solid ${isDefault ? 'fa-check-square' : 'fa-square'}`} aria-hidden />
                                </div>
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn binding="Id" header="Id" width={60} visible={false} />
                        <FlexGridColumn binding="Name" header="View Name" width={220} />
                        <FlexGridColumn binding="ViewType" header="View Type" width={150} dataMap={templateViewTypeDataMap ?? undefined} />
                        <FlexGridColumn binding="Description" header="Description" width="*" />
                      </FlexGrid>
                    )}
                  </div>
                </>
              )}
            </div>
          )}

          {activeTabIndex === TAB_FOLDER_NAV && (
            <div className="w-full flex-1 flex flex-col min-h-[200px] overflow-auto p-2">
              {!currentSearch?.Id && (
                <div className={`py-2 text-sm ${theme.label}`}>Save the template before configuring folder navigation.</div>
              )}
              {currentSearch?.Id && (
                <TemplateFolderNavigationSection
                  templateSearchId={currentSearch.Id}
                  templateSearchName={currentSearch.Name}
                  searchViewId={currentSearch.SearchViewId}
                  dataSetId={currentSearch.DataSetId}
                  applicationId={currentSearch.SaasApplicationId}
                  mainItemTransactionIds={mainItems.map((m) => m.TransactionId).filter((id): id is number => id != null)}
                />
              )}
            </div>
          )}

          {activeTabIndex === TAB_FILTERS && (
            <div className="w-full flex-1 flex flex-col min-h-[280px] overflow-hidden">
              {!currentSearch?.SearchViewId && (
                <div className={`py-2 px-3 text-sm ${theme.label} ${theme.mainContentSection} border-b`}>
                  Select a default view in the Template View Fields tab to configure filters.
                </div>
              )}
              <SearchFilterEditor
                filterList={currentSearch?.AppSearchFieldList ?? []}
                onAddFilter={addTemplateFilter}
                onRemoveFilter={removeTemplateFilter}
                onMarkChange={markTemplateFilterChange}
                filterColumnsDataMap={filterColumnsDataMap}
                controlTypeDataMap={controlTypeDataMap}
                criteriaOperatorDataMap={criteriaOperatorDataMap}
                enableAdvancedOptions={true}
                entityListForFilter={entityListForFilter}
                getEntityCodeById={getEntityCodeById}
                onOpenDatasourceSelector={openEntityInfoPopup}
                onOpenEntityDataPreview={openEntityDataEditorPopup}
                subControlTypeDataMap={subControlTypeDataMap}
                internalCodeDataMap={internalCodeDataMap}
                showWarning={showWarning}
                emptyMessage='No filters. Click "Add Filter" to add one.'
                minHeight={120}
              />
            </div>
          )}
          </div>
        </div>
      )}

      {!currentSearch?.DataSetId && (
        <div className={`w-full h-1 flex-auto overflow-auto px-5 py-5 ${theme.mainContentSection}`}>
          <p className={`text-sm ${theme.label}`}>Select a dataset above to configure Template Items, View Fields, and Filters.</p>
        </div>
      )}

      {datasetListModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true" aria-labelledby="dataset-list-title">
          <div className={`max-w-2xl w-full max-h-[80vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`} id="dataset-list-title">
              <span className="text-md font-semibold">Dataset List</span>
              <button type="button" onClick={() => setDatasetListModalOpen(false)} className={`p-1 rounded ${theme.button_default}`} aria-label="Close">
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className="flex-1 min-h-0 overflow-hidden p-2">
              {dataSetCV && (
                <FlexGrid
                  ref={datasetGridRef}
                  itemsSource={dataSetCV}
                  selectionMode="Row"
                  headersVisibility="Column"
                  className="h-full"
                >
                  <FlexGridColumn binding="Id" header="ID" width={80} />
                  <FlexGridColumn binding="Name" header="Name" width={300} />
                </FlexGrid>
              )}
            </div>
            <div className={`flex justify-end gap-2 px-4 py-2 border-t ${theme.mainContentSection}`}>
              <button type="button" onClick={() => setDatasetListModalOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button
                type="button"
                onClick={() => {
                  const grid = datasetGridRef.current?.control ?? datasetGridRef.current;
                  const row = grid?.selection?.row ?? -1;
                  const item = row >= 0 && grid?.rows?.[row] ? (grid.rows[row] as any).dataItem : null;
                  if (item?.Id != null) {
                    void applyDataSetSelection(item.Id);
                    setDatasetListModalOpen(false);
                  }
                }}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}

      {createDatasetDropdownOpen &&
        createDatasetMenuPos &&
        createPortal(
          <div
            ref={createDatasetMenuRef}
            className={`fixed z-[200] min-w-[240px] rounded shadow-lg border py-1 ${theme.mainContentSection}`}
            style={{
              left: createDatasetMenuPos.left,
              top: createDatasetMenuPos.top,
              transform: 'translateX(-100%)'
            }}
            onClick={(e) => e.stopPropagation()}
            role="menu"
          >
            {(
              [
                EmAppDataServiceType.QueryText,
                EmAppDataServiceType.StoredProcedure,
                EmAppDataServiceType.PluginWebApiCall,
                EmAppDataServiceType.IntegrationWebApiCall
              ] as const
            ).map((queryType) => renderCreateDatasetMenuItem(queryType, dataSourceRegisterList.length >= 2))}
          </div>,
          document.body
        )}

      {dataSetEditorOpen && (
        <div className="fixed inset-0 z-[130] flex flex-col bg-black/50 p-2">
          <div className={`flex min-h-0 flex-1 flex-col overflow-hidden rounded-md border ${theme.mainContentSection}`}>
            <DataSetEditor
              ignoreRouteParam
              dataSetId={null}
              queryType={dataSetEditorQueryType}
              dataSourceRegisterId={dataSetEditorDataSourceId}
              initialName={currentSearch?.Name?.trim() || 'New Dataset'}
              onSave={(saved: any) => {
                const id = saved?.Id != null ? Number(saved.Id) : null;
                if (id != null && Number.isFinite(id)) {
                  dataSetEditorLastSavedIdRef.current = id;
                }
              }}
              onConfirmAndClose={async (saved: any) => {
                const id = saved?.Id != null ? Number(saved.Id) : null;
                if (id != null && Number.isFinite(id)) {
                  await handleDataSetEditorSaved(id);
                } else {
                  setDataSetEditorOpen(false);
                }
              }}
              onClose={() => {
                void closeDataSetEditor();
              }}
            />
          </div>
        </div>
      )}

      {/* Datasource Selector popup – choose entity for Entity List of Value (when Control Type is DDL) */}
      {entitySelectionItem != null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" role="dialog" aria-modal="true" aria-labelledby="datasource-selector-title">
          <div className={`max-w-lg w-full max-h-[85vh] flex flex-col rounded-md overflow-hidden ${theme.mainContentSection} shadow-lg`}>
            <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.title}`} id="datasource-selector-title">
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
                onClick={() => {
                  datasourceSelectedHandler(datasourceSelectorSelectedId ?? null);
                }}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                OK
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default TransactionGroupEditor;
