import React, { useState, useEffect, useRef, useImperativeHandle } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { useParams } from 'react-router-dom';
import { RootState } from '../../redux/store';
import { searchSvc } from '../../webapi/searchSvc';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateCurrentTabLabel, getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { isAdminUserFromContext } from '../../helper/adminPermissionHelper';
import { integrationService } from '../../webapi/integrationsvc';
import { endpoints } from '../../webapi/endpoints';
import { getHeaders } from '../../helper/apiServiceHelper';

import { SearchFilter } from './SearchFilter';
import { SearchView } from './SearchView';

import { useTheme } from '../../redux/hooks/useTheme';
import appHelper from '../../helper/appHelper';
// Enums matching AngularJS implementation
const EmAppSearchUsageType = {
  Management: 1,
  QuickSearch: 2,
  MyLastModify: 3,
  EshopCategorySearch: 7,
};

const EmAppViewType = {
  GridView: 1,
  CardView: 2,
  PivotView: 5,
  CalendarView: 6,
  ChartView: 7,
  FlatDataSetTreeView: 8,
  EShopOrderListView: 9,
  EShopProductDetailView: 10,
  WorkflowView: 12,
  GanttView: 15,
  SchedulerView: 16,
  RecursiveDataSetTreeView: 18,
  HierarchyMasterDetailView: 23
};

type AppSearchProps = {
  embeddedParamObj?: any;
};

export type AppSearchHandle = {
  getSelectedResults: () => any[];
  executeSearch: () => Promise<void>;
};

const AppSearch = React.forwardRef<AppSearchHandle, AppSearchProps>(({ embeddedParamObj }, ref) => {
  const { t, theme } = useTheme();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const activeTab = useSelector((state: RootState) => state.tabnav.tabs.find((tab: { isActive: boolean }) => tab.isActive));
  const { showValidationMessages, showError } = useErrorMessage();
  const isBusy = useSelector((state: RootState) => state.busyLoader.isBusy);

  // Parse URL parameters
  const { param } = useParams<{ param: string }>();
  
  // Parse params in state so it updates when URL changes
  const [paramObj, setParamObj] = useState<any>(() => {
    if (embeddedParamObj && typeof embeddedParamObj === 'object') {
      return embeddedParamObj;
    }
    if (param) {
      try {
        const decodedParam = decodeURIComponent(param);
        return JSON.parse(decodedParam);
      } catch (error) {
        console.error('Error parsing param JSON:', error);
        return {};
      }
    }
    return {};
  });

  // Update paramObj when URL param changes
  useEffect(() => {
    if (embeddedParamObj && typeof embeddedParamObj === 'object') {
      setParamObj(embeddedParamObj);
      return;
    }
    if (param) {
      try {
        const decodedParam = decodeURIComponent(param);
        const parsed = JSON.parse(decodedParam);
        setParamObj(parsed);
      } catch (error) {
        console.error('Error parsing param JSON:', error);
        showError('Invalid URL parameters. ' + error);
        setParamObj({});
      }
    } else {
      setParamObj({});
    }
  }, [param, embeddedParamObj, showError]);

  // Extract parameters
  const searchId = paramObj.searchId || null;
  const isSavedSearch = paramObj.isSavedSearch || false;
  const initialViewId = paramObj.initialViewId ?? paramObj.initialviewId ?? null;
  const isShowCriterias = paramObj.isShowCriterias !== false; // Default to true
  const searchUsageType = paramObj.searchUsageType || EmAppSearchUsageType.Management;

  // Generate unique UI ID
  const uiId = React.useMemo(() => `search_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`, []);

  // Data model - contains all data-related state and UI controls
  const [dataModel, setDataModel] = useState<any>({
    // Search data
    searchDto: null,
    searchViewDto: null,
    searchResultCV: [],
    searchResultDto: null,
    currentSearchName: "Search",
    currentViewDefinition: null,
    viewid: null,
    searchid: null,
    
    // Lookup data
    dictDcuValue: {},
    dictFieldEntityDataSource: {},
    dictSearchViewFlexControl: {},
    savedSearches: [],
    mySearches: [],
    
    // UI Control state
    uiControl: {
      uiId: uiId,
      isShowCriterias: isShowCriterias,
      isSearchCriteriaCollapsed: false,
      searchUsageType: searchUsageType,
      initialSearchId: searchId,
      isInitialSavedSearch: isSavedSearch,
      initialViewId: initialViewId,
      isEnableFormConfigButtons: false,
      isAutoReopenCurrentFormAfterChange: true,
      isWorkflowLogSearch: false,
      workflowTransactionId: null,
      workflowTransactionRId: null,
      workflowLogBatchNumber: null,
      isLinkedSearch: false,
      isSingleSelection: false,
      searchUIHeight: 200,
      searchViewHeight: 0,
      isNotToExecuteSearch: paramObj.isNotToExecuteSearch || false
    }
  });

  // Keep linked-search / workflow-log flags in sync when embedded params change.
  useEffect(() => {
    const linked =
      paramObj?.isLinkedSearch ??
      paramObj?.IsLinkedSearch ??
      false;
    const single =
      paramObj?.isSingleSelection ??
      paramObj?.IsSingleSelection ??
      false;
    const workflowLog =
      paramObj?.isWorkflowLogSearch ??
      paramObj?.IsWorkflowLogSearch ??
      false;
    setDataModel((prev: any) => ({
      ...prev,
      uiControl: {
        ...prev.uiControl,
        isLinkedSearch: Boolean(linked),
        isSingleSelection: Boolean(single),
        isWorkflowLogSearch: Boolean(workflowLog),
        workflowTransactionId:
          paramObj?.workflowTransactionId ?? paramObj?.WorkflowTransactionId ?? null,
        workflowTransactionRId:
          paramObj?.workflowTransactionRId ?? paramObj?.WorkflowTransactionRId ?? null,
        workflowLogBatchNumber:
          paramObj?.workflowLogBatchNumber ?? paramObj?.WorkflowLogBatchNumber ?? null,
        isShowCriterias:
          paramObj?.isShowCriterias ??
          paramObj?.isShowSearchCriteria ??
          prev.uiControl?.isShowCriterias,
      },
    }));
  }, [
    paramObj?.isLinkedSearch,
    paramObj?.IsLinkedSearch,
    paramObj?.isSingleSelection,
    paramObj?.IsSingleSelection,
    paramObj?.isWorkflowLogSearch,
    paramObj?.IsWorkflowLogSearch,
    paramObj?.workflowTransactionId,
    paramObj?.WorkflowTransactionId,
    paramObj?.workflowTransactionRId,
    paramObj?.WorkflowTransactionRId,
    paramObj?.workflowLogBatchNumber,
    paramObj?.WorkflowLogBatchNumber,
    paramObj?.isShowCriterias,
    paramObj?.isShowSearchCriteria,
  ]);

  // SearchFilter -> TEXT criteria overrides getter (pulled on SEARCH click).
  const textCriteriaOverridesGetterRef = useRef<null | (() => Record<string, any>)>(null);
  const liveCriteriaOverridesGetterRef = useRef<null | (() => Record<string, any>)>(null);
  const eshopFilterTextOverridesGetterRef = useRef<null | (() => Record<string, any>)>(null);
  const eshopCardSearchRef = useRef<AppSearchHandle>(null);
  const selectedLinkedRowsRef = useRef<any[]>([]);
  const [eshopCardSearchDtoForFilter, setEshopCardSearchDtoForFilter] = useState<any>(null);
  const [eshopCardCrit, setEshopCardCrit] = useState<Record<string, any>>({});
  const eshopCardCritRef = useRef<Record<string, any>>({});
  const [eshopFilterClearVersion, setEshopFilterClearVersion] = useState(0);
  const [resolvedEshopCardViewId, setResolvedEshopCardViewId] = useState<any>(null);
  const [eshopOptionDisplayByLevel, setEshopOptionDisplayByLevel] = useState<Record<string, string>>({});
  const [eshopOptionLevelMap, setEshopOptionLevelMap] = useState<Record<string, any[]>>({});
  const eshopOptionLevelMapRef = useRef<Record<string, any[]>>({});
  // NOTE: avoid stale params by using ref + immediate executeSearch on click.
  const lastTreeSelectionPatchSigRef = useRef<string>('__UNSET__');
  const lastEshopCardAutoExecuteVersionRef = useRef<number>(0);
  const lastEshopCardAutoExecuteTsRef = useRef<number>(0);
  const [collapsedEshopOptionLevels, setCollapsedEshopOptionLevels] = useState<Record<string, boolean>>({});

  const toggleCollapsedEshopOptionLevel = React.useCallback((level: string) => {
    setCollapsedEshopOptionLevels((prev) => ({ ...(prev ?? {}), [level]: !Boolean(prev?.[level]) }));
  }, []);

  // New result set: row.isSelected and ref would point at stale data items if we kept the old selection.
  useEffect(() => {
    if (dataModel.uiControl?.isLinkedSearch) {
      selectedLinkedRowsRef.current = [];
    }
  }, [dataModel.searchResultCV, dataModel.uiControl?.isLinkedSearch]);

  // Search execution function (internal helper that accepts searchDto and viewDto as parameters).
  // Optional `dictDcuValueForCriteria` uses that object (plus text overrides) instead of dataModel.dictDcuValue — used by calendar navigator range.
  const executeSearchWithDto = async (
    searchDtoParam: any,
    viewDtoParam: any,
    dictDcuValueForCriteria?: Record<string, any>,
  ) => {
    try {
      liveCriteriaOverridesGetterRef.current?.();

      if (!searchDtoParam) {
        // Embedded search can be triggered before retrieveOneSearch finishes.
        // Keep this silent to avoid noisy false-negative messages during initialization.
        if (!paramObj?.searchId) {
          showError('No searchDto available');
        }
        return;
      }

      dispatch(setIsBusy());

      // Prepare criteria for API call - keep all properties like AngularJS version
      const parseDelimitedTextValues = (text: any): string[] => {
        const raw = String(text ?? "");
        if (!raw.trim()) return [];
        return raw
          .split(/[;,]/g)
          .map((s) => s.trim())
          .filter((s) => s.length > 0);
      };

      const externalText =
        typeof paramObj?.getTextCriteriaOverrides === 'function' ? paramObj.getTextCriteriaOverrides() ?? {} : {};
      const textCriteriaOverrides: Record<string, any> = {
        ...externalText,
        ...(textCriteriaOverridesGetterRef.current?.() ?? {}),
      };
      const liveCriteriaOverrides: Record<string, any> =
        liveCriteriaOverridesGetterRef.current?.() ?? {};
      const criteriaOverlay =
        typeof paramObj?.criteriaDictOverride === 'function'
          ? ((paramObj.criteriaDictOverride() ?? {}) as Record<string, any>)
          : paramObj?.criteriaDictOverride && typeof paramObj.criteriaDictOverride === 'object'
            ? (paramObj.criteriaDictOverride as Record<string, any>)
            : {};
      const baseDict = dictDcuValueForCriteria ?? (dataModel.dictDcuValue ?? {});
      // IMPORTANT: overlay must win over baseDict (Angular: tree click overwrites linked search criteria).
      const dictDcuValueOverridesRaw: Record<string, any> = {
        ...baseDict,
        ...criteriaOverlay,
        ...(liveCriteriaOverrides ?? {}),
        ...(textCriteriaOverrides ?? {}),
      };

      // Angular uses dictDcuValue keyed by SearcDCUID. Our Eshop tree mapping often produces keys
      // by SysTableFiledPath (e.g. "Catalog2"). Expand overrides so both keys work reliably.
      const normalizeKey = (k: any) => String(k ?? '').trim().toLowerCase();
      const sysPathToDcuId = new Map<string, string>();
      (searchDtoParam?.Criterias ?? []).forEach((c: any) => {
        const sysPath = String(c?.SysTableFiledPath ?? '').trim();
        const dcu = String(c?.SearcDCUID ?? '').trim();
        if (!sysPath || !dcu) return;
        const nk = normalizeKey(sysPath);
        if (!sysPathToDcuId.has(nk)) sysPathToDcuId.set(nk, dcu);
      });
      const dictDcuValueOverrides: Record<string, any> = { ...dictDcuValueOverridesRaw };
      Object.entries(dictDcuValueOverridesRaw).forEach(([k, v]) => {
        const nk = normalizeKey(k);
        const dcu = sysPathToDcuId.get(nk);
        if (!dcu) return;
        if (dictDcuValueOverrides[dcu] === undefined) {
          dictDcuValueOverrides[dcu] = v;
        }
      });

      // Also build a normalized-key view for SysTableFiledPath lookups (handles casing/whitespace differences).
      const dictDcuValueOverridesNorm: Record<string, any> = {};
      Object.entries(dictDcuValueOverrides).forEach(([k, v]) => {
        const nk = normalizeKey(k);
        if (!nk) return;
        if (dictDcuValueOverridesNorm[nk] === undefined) dictDcuValueOverridesNorm[nk] = v;
      });
      const Criterias: any[] = searchDtoParam.Criterias.map((element: any) => {
        // Keep all properties from the original criteria, don't strip them out.
        const revised = {
          ...element,
          // Ensure Values array is included
          Values: element.Values || [],
        };

        // Apply React user-entered overrides (Angular binds CriteriaOperator + Values).
        const dcuKey = element?.SearcDCUID != null ? String(element.SearcDCUID) : "";
        const sysKey = element?.SysTableFiledPath != null ? String(element.SysTableFiledPath) : "";
        const override =
          (dcuKey ? dictDcuValueOverrides?.[dcuKey as any] : undefined) ??
          (sysKey ? dictDcuValueOverrides?.[sysKey as any] : undefined) ??
          (dcuKey ? dictDcuValueOverridesNorm?.[normalizeKey(dcuKey) as any] : undefined) ??
          (sysKey ? dictDcuValueOverridesNorm?.[normalizeKey(sysKey) as any] : undefined);
        if (override !== undefined && override !== null) {
          if (Array.isArray(override)) {
            revised.Values = override;
          } else if (typeof override === "string") {
            revised.Values = parseDelimitedTextValues(override);
          } else if (typeof override === 'object') {
            if (Array.isArray(override.values)) revised.Values = override.values;
            if (typeof override.valuesText === "string") {
              revised.Values = parseDelimitedTextValues(override.valuesText);
            }
            if (override.operator !== undefined && override.operator !== null) {
              revised.CriteriaOperator = override.operator;
            }
          }
        }

        return revised;
      });

      const ReferenceViewDefinitionDto = {
        ...(viewDtoParam || {}),
        Id: viewDtoParam?.Id,
        IsMassUpdate: viewDtoParam?.IsMassUpdate || false,
        ViewType: viewDtoParam?.ViewType || EmAppViewType.GridView,
      };

      // Use original searchDto structure and overwrite specific properties
      const searchRevisedDto = {
        ...searchDtoParam,
        Criterias,
        ReferenceViewDefinitionDto
      };
      const dtoOverrides =
        typeof paramObj?.getSearchDtoOverrides === 'function' ? paramObj.getSearchDtoOverrides() ?? {} : {};
      let finalSearchDto = { ...searchRevisedDto, ...(dtoOverrides || {}) };

      const ui = dataModel.uiControl;
      const isWorkflowLogSearch = Boolean(
        paramObj?.isWorkflowLogSearch ??
          paramObj?.IsWorkflowLogSearch ??
          ui?.isWorkflowLogSearch,
      );
      if (isWorkflowLogSearch) {
        finalSearchDto = {
          ...finalSearchDto,
          IsWorkflowLogSearch: true,
          WorkflowTransactionId:
            paramObj?.workflowTransactionId ??
            paramObj?.WorkflowTransactionId ??
            ui?.workflowTransactionId ??
            null,
          WorkflowTransactionRId:
            paramObj?.workflowTransactionRId ??
            paramObj?.WorkflowTransactionRId ??
            ui?.workflowTransactionRId ??
            null,
          WorkflowLogBatchNumber:
            paramObj?.workflowLogBatchNumber ??
            paramObj?.WorkflowLogBatchNumber ??
            ui?.workflowLogBatchNumber ??
            null,
        };
      }

      // Call the search service
      const searchResult = await searchSvc.retrieveSearchResult(finalSearchDto);

      // Update the data model with search results
      setDataModel((prev: any) => ({
        ...prev,
        searchResultDto: searchResult,
        searchResultCV: searchResult?.SearchResultRowList || [],
        searchViewDto: prev.searchViewDto ? {
          ...prev.searchViewDto,
          Data: searchResult?.SearchResultRowList || []
        } : null
      }));
      if (typeof paramObj?.onSearchResult === 'function') {
        try {
          paramObj.onSearchResult(searchResult);
        } catch {
          // ignore parent callback errors
        }
      }

      // Update tab label with search name only when this tab is the Search page (not when AppSearch is embedded in Dashboard/Dashboard Editor)
      if (searchDtoParam) {
        const path = (activeTab as { path?: string } | undefined)?.path ?? '';
        const isSearchTab = /masterdatamanagement/i.test(path);
        if (activeTabKey && activeTabKey !== 'home-tab' && isSearchTab) {
          dispatch(updateCurrentTabLabel(searchDtoParam.Display));
        }
      }

    } catch (error) {
      console.error('Error executing search:', error);
      showError('Failed to execute search: ' + (error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Search execution function (public, uses current dataModel state)
  const executeSearch = async () => {
    await executeSearchWithDto(dataModel.searchDto, dataModel.searchViewDto);
  };

  const inlineLinkedSearch = React.useMemo(() => {
    const list = dataModel?.searchViewDto?.AppViewLinkedSeaechOrUrlDtoList;
    if (!Array.isArray(list)) return null;
    return (
      list.find(
        (x: any) =>
          x?.LinkTargetSearchId &&
          (x?.LayoutDisplayMode === 2 || String(x?.LayoutDisplayMode) === '2') &&
          !(x?.IsPopup === true),
      ) ?? null
    );
  }, [dataModel?.searchViewDto?.AppViewLinkedSeaechOrUrlDtoList]);
  const currentViewType = Number(dataModel?.searchViewDto?.ViewType ?? 0);
  const eshopCategoryMapping = dataModel?.searchViewDto?.OtherSettingsDto?.EshopCategorySearchMapping ?? null;
  const isEshopCategorySearch =
    Number(dataModel?.searchDto?.Type ?? 0) === EmAppSearchUsageType.EshopCategorySearch ||
    Number(dataModel?.searchDto?.SearchType ?? 0) === EmAppSearchUsageType.EshopCategorySearch ||
    (currentViewType === EmAppViewType.FlatDataSetTreeView && inlineLinkedSearch?.LinkTargetSearchId != null);
  const eshopCardSearchId =
    dataModel?.searchDto?.EshopCardSearchExDto?.Id ??
    inlineLinkedSearch?.LinkTargetSearchId ??
    eshopCategoryMapping?.LinkTargetSearchId ??
    null;
  const eshopCardSearchViewId =
    dataModel?.searchDto?.EshopCardSearchExDto?.SearchViewId ??
    inlineLinkedSearch?.LinkTargetSearchViewId ??
    resolvedEshopCardViewId ??
    null;

  /** Calendar navigator: merge DCU criteria overrides then search (Angular changeCalendarTimeRange + search). */
  const patchDictDcuValueAndSearch = async (patch: Record<string, any>) => {
    let merged: Record<string, any> = {};
    let sd: any;
    let sv: any;
    setDataModel((prev: any) => {
      merged = { ...(prev.dictDcuValue ?? {}), ...patch };
      sd = prev.searchDto;
      sv = prev.searchViewDto;
      return { ...prev, dictDcuValue: merged };
    });
    if (sd && sv) {
      await executeSearchWithDto(sd, sv, merged);
    }
  };

  const previewDefaultAppProvideApi = async (clickEvent?: React.MouseEvent<HTMLButtonElement>) => {
    if (!dataModel.searchDto?.DefaultAppProvideApiId) return;

    const defaultAppProvideApiId = String(dataModel.searchDto.DefaultAppProvideApiId);

    const parseDelimitedTextValues = (text: any): string[] => {
      const raw = String(text ?? "");
      if (!raw.trim()) return [];
      return raw
        .split(/[;,]/g)
        .map((s) => s.trim())
        .filter((s) => s.length > 0);
    };

    // Build criterias as the same way executeSearch does, so preview uses the same query params.
    const externalTextPreview =
      typeof paramObj?.getTextCriteriaOverrides === 'function' ? paramObj.getTextCriteriaOverrides() ?? {} : {};
    const textCriteriaOverrides: Record<string, any> = {
      ...externalTextPreview,
      ...(textCriteriaOverridesGetterRef.current?.() ?? {}),
    };
    const criteriaOverlayPreview =
      typeof paramObj?.criteriaDictOverride === 'function'
        ? ((paramObj.criteriaDictOverride() ?? {}) as Record<string, any>)
        : paramObj?.criteriaDictOverride && typeof paramObj.criteriaDictOverride === 'object'
          ? (paramObj.criteriaDictOverride as Record<string, any>)
          : {};
    // Same precedence as search(): base then overlay then text.
    const dictDcuValueOverrides: Record<string, any> = {
      ...(dataModel.dictDcuValue ?? {}),
      ...criteriaOverlayPreview,
      ...(textCriteriaOverrides ?? {}),
    };

    const Criterias: any[] = Array.isArray(dataModel.searchDto?.Criterias)
      ? dataModel.searchDto.Criterias.map((element: any) => {
          const revised = {
            ...element,
            Values: element.Values || [],
          };
          const override = dictDcuValueOverrides?.[element.SearcDCUID as any];
          if (override !== undefined && override !== null) {
            if (Array.isArray(override)) {
              revised.Values = override;
            } else if (typeof override === "string") {
              revised.Values = parseDelimitedTextValues(override);
            } else if (typeof override === "object") {
              if (Array.isArray(override.values)) revised.Values = override.values;
              if (typeof override.valuesText === "string") {
                revised.Values = parseDelimitedTextValues(override.valuesText);
              }
              if (override.operator !== undefined && override.operator !== null) {
                revised.CriteriaOperator = override.operator;
              }
            }
          }
          return revised;
        })
      : [];

    // Angular: RetrieveOneAppIntergrationSettingParameterExDto(DefaultAppProvideApiId)
    setApiPreviewOpen(true);
    setApiPreviewLoading(true);
    setApiPreviewResultText("");

    if (clickEvent) {
      const clientX = clickEvent.clientX;
      const clientY = clickEvent.clientY;
      // Roughly mimic "openPopupAtPosition" sizing.
      setApiPreviewPopupPos({ top: clientY - 80, left: clientX - 420 });
    }

    dispatch(setIsBusy());
    try {
      const apiSettingDto = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(
        defaultAppProvideApiId,
        false,
      );
      if (!apiSettingDto) {
        showError("No API setting found for preview");
        return;
      }

      // IMPORTANT: use relative app base URL so dev proxy/session handling matches normal API calls.
      // Using absolute "http://localhost/appai/..." can break fetch in dev (cross-origin/credentials).
      const baseUrl = `${endpoints.BASE_URL}/webapi/DataIntegration/${apiSettingDto.ActionCode}`;

      const prepareApiPreviewUrl = () => {
        let apiUrl = baseUrl;
        if (
          apiSettingDto?.HttpMethd === "Get" &&
          apiSettingDto?.APIConfigParameters?.QueryParams
        ) {
          const queryParams = apiSettingDto.APIConfigParameters.QueryParams ?? {};
          let paramCount = 0;
          const entries = Object.entries(queryParams);
          for (const [key, originalValue] of entries) {
            let value: any = originalValue;

            // Override query param value from criteria.Values by SysTableFiledPath.
            if (Criterias?.length > 0) {
              for (const criteriaDto of Criterias) {
                if (criteriaDto?.SysTableFiledPath === key) {
                  const values = Array.isArray(criteriaDto?.Values)
                    ? criteriaDto.Values
                    : [];
                  if (values && values.length > 0) {
                    const joined = values.filter((v: any) => v !== null && v !== undefined && String(v).trim() !== "").map((v: any) => String(v)).join(",");
                    if (joined.length > 0) value = joined;
                  }
                }
              }
            }

            if (value !== undefined && value !== null && key) {
              apiUrl += (paramCount === 0 ? "?" : "&") + `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`;
              paramCount++;
            }
          }
        }
        return apiUrl;
      };

      const url = prepareApiPreviewUrl();
      setApiPreviewUrl(url);

      const apiHeaders =
        apiSettingDto?.APIConfigParameters?.Headers && typeof apiSettingDto.APIConfigParameters.Headers === "object"
          ? apiSettingDto.APIConfigParameters.Headers
          : {};

      const mergedHeaders = new Headers(getHeaders());
      Object.entries(apiHeaders).forEach(([k, v]) => {
        if (k && v !== undefined && v !== null) {
          mergedHeaders.set(String(k), String(v));
        }
      });

      const res = await fetch(url, {
        method: "GET",
        headers: mergedHeaders,
        credentials: "same-origin",
      });
      const text = await res.text();

      let parsed: any = null;
      try {
        parsed = JSON.parse(text);
      } catch {
        parsed = text;
      }

      const resultText =
        parsed === "" ? "" : typeof parsed === "string" ? parsed : JSON.stringify(parsed || "", null, 2);

      setApiPreviewResultText(resultText);
    } catch (e: any) {
      console.error("API preview error:", e);
      showError(e instanceof Error ? e.message : "Failed to preview API");
      setApiPreviewResultText("");
    } finally {
      dispatch(setIsNotBusy());
      setApiPreviewLoading(false);
    }
  };

  // Load data from server
  const loadDataFromServer = async (dictCreteriaIdValue?: any, dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue?: any) => {
    try {
      // Get current searchId from paramObj (may have changed)
      const currentSearchId = paramObj.searchId || null;
      const currentIsSavedSearch = paramObj.isSavedSearch || false;
      
      if (!currentSearchId) {
        return; // No searchId to load
      }

      dispatch(setIsBusy());

      let searchDto: any = null;
      await searchSvc.retrieveOneSearch(currentSearchId, currentIsSavedSearch).then(function (data) {
        searchDto = data;
        // if (initialViewId) {
        //   searchSvc.retrieveOneReferenceViewDto(initialViewId).then(function (referenceViewDto) {
        //     if (referenceViewDto) {

        //       searchDto.DefaultView = referenceViewDto;
        //       // searchHelper.prepareSearchData(searchDto, $scope);

        //     }
        //   });
        // }
        // else {
        //   //searchHelper.prepareSearchData(searchDto, $scope);
        // }
      });

      if (searchDto) {
        // User views for this search definition (server does not populate searchDto.Views on RetrieveOneSearch).
        const searchDefinitionDto = {
          IsStaticBuiltInSearch: searchDto.IsStaticBuiltInSearch,
          Id: searchDto.Id,
          BlqueryId: searchDto.BlqueryId,
          Display: searchDto.Display,
          IsSavedSearch: searchDto.IsSavedSearch,
          SearchType: searchDto.SearchType,
        };

        let viewsData: any[] = [];
        try {
          const rawViews = await searchSvc.retrieveUserViewsBySearchDefinition(searchDefinitionDto);
          viewsData = Array.isArray(rawViews) ? rawViews : [];
        } catch (error) {
          console.error('Error retrieving user views:', error);
        }

        // Unit linked search / URL param: prefer configured default view on the link; else search editor DefaultView.
        const linkedInitialRaw = paramObj?.initialViewId ?? paramObj?.initialviewId;
        const linkedInitialViewId =
          linkedInitialRaw != null && linkedInitialRaw !== '' ? linkedInitialRaw : null;

        const tryLoadFullReferenceView = async (id: any): Promise<any | null> => {
          if (id == null || id === '') return null;
          try {
            return await searchSvc.retrieveOneReferenceViewDto(String(id));
          } catch {
            return null;
          }
        };

        // GridViewLayout needs viewDto.Columns. Items from retrieveUserViewsBySearchDefinition are
        // lightweight (no Columns); same as handleViewChange — always load full ReferenceViewDto.
        let viewDto: any = null;

        if (linkedInitialViewId != null) {
          viewDto = await tryLoadFullReferenceView(linkedInitialViewId);
        }

        if (!viewDto && searchDto.DefaultView) {
          const dv = searchDto.DefaultView;
          if (Array.isArray(dv.Columns) && dv.Columns.length > 0) {
            viewDto = dv;
          } else if (dv.Id != null) {
            viewDto = (await tryLoadFullReferenceView(dv.Id)) ?? dv;
          } else {
            viewDto = dv;
          }
        }

        if (!viewDto && viewsData.length > 0) {
          const first = viewsData[0];
          if (first?.Id != null) {
            viewDto = (await tryLoadFullReferenceView(first.Id)) ?? first;
          }
        }

        setDataModel((prev: any) => ({
          ...prev,
          dictDcuValue: dictCreteriaIdValue
            ? { ...prev.dictDcuValue, ...dictCreteriaIdValue }
            : prev.dictDcuValue,
          searchDto,
          searchViewDto: viewDto || null,
          searchid: searchDto?.Id || null,
          viewid: viewDto?.Id || null,
          currentSearchName: searchDto?.Display || '',
          currentViewDefinition: viewDto || null,
          views: viewsData,
        }));

        const isAutoExecute = searchDto.IsAutoExecute || searchDto.IsAutoExcute;
        const isNotToExecuteSearch = Boolean(paramObj.isNotToExecuteSearch);

        if (isAutoExecute && !isNotToExecuteSearch && searchDto && viewDto) {
          setTimeout(() => {
            executeSearchWithDto(searchDto, viewDto);
          }, 200);
        }

        // Load saved searches for the current usage type
        // const savedSearches = await searchSvc.getSavedSearches();
        // setDataModel(prev => ({
        //   ...prev,
        //   savedSearches: savedSearches || []
        // }));

      } else {
        showError('No search configuration found');
      }

    } catch (error) {
      console.error('Error loading data:', error);
      showError('Failed to load search data: ' + (error as Error).message);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Load data with caching support
  const loadData = async () => {
    // Get current searchId from paramObj (may have changed)
    const currentSearchId = paramObj.searchId || null;
    
    if (!currentSearchId) {
      return; // No searchId to load
    }

    let cachedDataModel = null;
    let dataModelKey = null;

    if (paramObj.isNavigatedFromTab) {
      dataModelKey = getCurrentActiveTab()?.tabKey || null;
    }

    if (dataModelKey) {
      cachedDataModel = getDataModelFromCache(dataModelKey);
      if (cachedDataModel) {
        // Only use cache if it's for the same searchId
        if (cachedDataModel.searchid === currentSearchId || cachedDataModel.uiControl?.initialSearchId === currentSearchId) {
          setDataModel(cachedDataModel);
          return;
        }
      }
    }

    await loadDataFromServer(
      (paramObj?.dictCreteriaIdValue ?? null) as any,
      (paramObj?.dictViewColumnIdAndValue_For_LinkTargetParameterDefaultValue ?? null) as any
    );
  };

  // UI Control methods
  const _setSearchViewStyle = () => {
    // Implement search view styling logic
    console.log('Setting search view style');
  };

  const _reset = () => {
    setDataModel((prev: any) => ({
      ...prev,
      searchid: null,
      viewid: null
    }));
  };

  const _getSelectedResult = (): any[] => {
    // Linked search (single or multi): GridViewLayout uses FlexGrid Row.isSelected → onSelectionChanged → ref.
    if (dataModel.uiControl?.isLinkedSearch) {
      return selectedLinkedRowsRef.current ?? [];
    }
    if (dataModel.searchResultCV && dataModel.searchResultCV.length > 0) {
      return dataModel.searchResultCV.filter((item: any) => item.IsSelected);
    }
    return [];
  };

  useImperativeHandle(ref, () => ({
    getSelectedResults: () => _getSelectedResult(),
    executeSearch: () => executeSearch(),
  }), [dataModel.searchResultCV, dataModel.searchViewDto, dataModel.searchDto, dataModel.dictDcuValue]);

  const _getAllResult = (): any[] => {
    return dataModel.searchResultCV || [];
  };

  const [criteriaClearVersion, setCriteriaClearVersion] = useState(0);

  // API preview popup state (Angular: ApiCallPreviewPopup + Monaco JSON)
  const [apiPreviewOpen, setApiPreviewOpen] = useState(false);
  const [apiPreviewLoading, setApiPreviewLoading] = useState(false);
  const [apiPreviewUrl, setApiPreviewUrl] = useState('');
  const [apiPreviewResultText, setApiPreviewResultText] = useState('');
  const [apiPreviewPopupPos, setApiPreviewPopupPos] = useState<{ top: number; left: number }>({
    top: 120,
    left: 60,
  });

  // Toggle search criteria visibility
  const toggleSearchCriteria = () => {
    setDataModel((prev: any) => ({
      ...prev,
      uiControl: {
        ...prev.uiControl,
        isSearchCriteriaCollapsed: !prev.uiControl.isSearchCriteriaCollapsed
      }
    }));
  };

  // Clear criteria values
  const clearCriteriaValues = () => {
    setDataModel((prev: any) => ({
      ...prev,
      dictDcuValue: {}
    }));
    setCriteriaClearVersion((v) => v + 1);
  };

  // Get user context for admin check
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  
  // Check if configuration mode is enabled (match Angular AppSecurityUserBL.IsAdminUser behavior)
  const enableConfigurationMode =
    userContext?.DictAppSetup?.EnableConfigurationMode ||
    userContext?.DictAppSetup?.enableConfigurationMode ||
    userContext?.EnableConfigurationMode ||
    userContext?.enableConfigurationMode ||
    false;
  const isAdminUser = isAdminUserFromContext(userContext);
  
  // Check if views exist and have more than 1
  const hasMultipleViews = dataModel.views && dataModel.views.length > 1;
  
  // Check if API Preview should be shown
  const showApiPreview = dataModel.searchDto?.DefaultAppProvideApiId != null;
  
  // Check if criteria section should be shown (for button visibility)
  const isEmbeddedChromeHidden = Boolean(
    paramObj?.isHideHeaderAndFooter ?? paramObj?.IsHideHeaderAndFooter,
  );
  const shouldShowCriteriaButtons = dataModel.uiControl.isShowCriterias && !dataModel.searchDto?.IsHideAllToolsBar;
  
  // State for User Views dropdown
  const [showUserViewsDropdown, setShowUserViewsDropdown] = useState(false);
  const userViewsDropdownRef = useRef<HTMLDivElement>(null);
  
  // Handle view change
  const handleViewChange = async (view: any) => {
    const viewId = view?.Id;
    // Close dropdown immediately for better UX.
    setShowUserViewsDropdown(false);

    if (!viewId) {
      setDataModel((prev: any) => ({
        ...prev,
        searchViewDto: view,
        currentViewDefinition: view,
        viewid: null
      }));
      return;
    }

    try {
      // Angular behavior: switch user view should load the full reference view definition
      // (including Columns) so the grid has columns.
      const referenceViewDto = await searchSvc.retrieveOneReferenceViewDto(String(viewId));
      if (!referenceViewDto) {
        // Fallback: keep the provided view object (may be missing Columns).
        setDataModel((prev: any) => ({
          ...prev,
          searchViewDto: view,
          currentViewDefinition: view,
          viewid: viewId || null,
        }));
        return;
      }

      setDataModel((prev: any) => ({
        ...prev,
        searchViewDto: referenceViewDto,
        currentViewDefinition: referenceViewDto,
        viewid: referenceViewDto?.Id || viewId || null,
      }));

      // Angular behavior: after changing view, re-execute search so grid values refresh.
      // Respect "isNotToExecuteSearch" flag to keep embedded scenarios consistent.
      if (!dataModel.uiControl?.isNotToExecuteSearch && dataModel.searchDto && referenceViewDto) {
        // Small delay to mimic AngularJS $timeout(200) behavior.
        setTimeout(() => {
          executeSearchWithDto(dataModel.searchDto, referenceViewDto);
        }, 200);
      }
    } catch (e) {
      console.error('Failed to load reference view dto:', e);
      showError('Failed to load view definition');
      setDataModel((prev: any) => ({
        ...prev,
        searchViewDto: view,
        currentViewDefinition: view,
        viewid: viewId || null,
      }));
    }
  };
  
  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (userViewsDropdownRef.current && !userViewsDropdownRef.current.contains(event.target as Node)) {
        setShowUserViewsDropdown(false);
      }
    };
    if (showUserViewsDropdown) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [showUserViewsDropdown]);

  // Handle criteria value changes
  const handleCriteriaValueChanged = (searchFieldId: string, value: any) => {
    setDataModel((prev: any) => ({
      ...prev,
      dictDcuValue: {
        ...prev.dictDcuValue,
        [searchFieldId]: value
      }
    }));
  };

  // Keep URL / embedded param snapshot in uiControl (always sync initialViewId — needed for unit linked search popup).
  useEffect(() => {
    setDataModel((prev: any) => ({
      ...prev,
      uiControl: {
        ...prev.uiControl,
        initialSearchId: searchId,
        isInitialSavedSearch: isSavedSearch,
        initialViewId,
        isNotToExecuteSearch: paramObj.isNotToExecuteSearch || false,
        flatDataSetTreeOnHide:
          typeof paramObj.flatDataSetTreeOnHide === 'function' ? paramObj.flatDataSetTreeOnHide : undefined,
      },
    }));
  }, [searchId, isSavedSearch, initialViewId, paramObj.isNotToExecuteSearch, paramObj.flatDataSetTreeOnHide]);

  // Initialize component and reload when searchId changes
  useEffect(() => {
    loadData();
  }, [searchId, isSavedSearch, initialViewId]);

  useEffect(() => {
    if (!isEshopCategorySearch) {
      setEshopCardSearchDtoForFilter(null);
      setEshopCardCrit({});
      eshopCardCritRef.current = {};
      setEshopFilterClearVersion(0);
      setEshopOptionDisplayByLevel({});
      setEshopOptionLevelMap({});
      return;
    }
    const dto =
      dataModel?.searchResultDto?.EshopCategoryViewDto ??
      dataModel?.searchResultDto?.EshopCatalogViewDto ??
      dataModel?.searchResultDto?.EshopCatelogViewDto ??
      {};
    const displayByLevel =
      dto?.DictoptionDisplay ??
      dto?.DictOptionDisplay ??
      dto?.DictoptoinDisplay ??
      dto?.DictOptoinDisplay ??
      {};
    const dictItems =
      dto?.DictoptionItems ??
      dto?.DictOptionItems ??
      dto?.DictoptionItemsArray ??
      dto?.DictOptionItemsArray ??
      {};
    const dictLevel =
      dto?.DictoptionLevel ??
      dto?.DictOptionLevel ??
      dto?.DictoptoinLevel ??
      dto?.DictOptoinLevel ??
      {};
    // Prefer level-based items when available.
    const itemsByLabel = Object.keys(dictLevel ?? {}).length > 0 ? dictLevel : dictItems;
    const pairs: { label: string; items: any[]; order: number }[] = [];
    const displayEntries = Object.entries(displayByLevel ?? {});
    if (displayEntries.length) {
      for (const [k, v] of displayEntries) {
        const label = String(v ?? '').trim();
        if (!label) continue;
        const arr = (itemsByLabel as any)?.[label] ?? (itemsByLabel as any)?.[k];
        if (Array.isArray(arr)) pairs.push({ label, items: arr, order: Number(k) || 0 });
      }
    }
    if (!pairs.length && itemsByLabel && typeof itemsByLabel === 'object') {
      for (const [k, v] of Object.entries(itemsByLabel)) {
        if (!Array.isArray(v)) continue;
        pairs.push({ label: String((displayByLevel as any)?.[k] ?? k), items: v as any[], order: Number(k) || 0 });
      }
    }
    if (!pairs.length) {
      setEshopCardSearchDtoForFilter(null);
      return;
    }
    pairs.sort((a, b) => a.order - b.order);
    const criterias = pairs.map((p, idx) => ({
      SearcDCUID: `eshop_opt_${idx + 1}_${p.label}`,
      Display: p.label,
      RowIndex: 1,
      ColumnIndex: idx + 1,
      CriteriaType: 1,
      IsAllowMultipleSelect: true,
      IsOptionFilterChecklist: true,
      IsReadOnly: false,
      SupportedOperators: [],
      CriteriaOperator: null,
      Values: [],
      ItemsSource: (p.items ?? []).map((x: any) => ({
        Id: String(x?.Id ?? ''),
        Display: String(x?.Display ?? x?.Id ?? ''),
      })),
    }));
    setEshopCardSearchDtoForFilter({
      Criterias: criterias,
      CriteriasRowCount: 1,
    });
  }, [dataModel?.searchResultDto, isEshopCategorySearch]);

  useEffect(() => {
    let cancelled = false;
    if (!isEshopCategorySearch || !eshopCardSearchId || eshopCardSearchViewId) {
      if (!eshopCardSearchId) setResolvedEshopCardViewId(null);
      return;
    }
    (async () => {
      try {
        const cardDto = await searchSvc.retrieveOneSearch(String(eshopCardSearchId), false);
        if (cancelled) return;
        const viewId = cardDto?.DefaultView?.Id ?? cardDto?.DefaultSearchViewExDto?.Id ?? null;
        setResolvedEshopCardViewId(viewId);
      } catch {
        if (!cancelled) setResolvedEshopCardViewId(null as any);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [eshopCardSearchId, eshopCardSearchViewId, isEshopCategorySearch]);

  const normalizedEshopFilterDto = React.useMemo(() => {
    const dto = eshopCardSearchDtoForFilter;
    if (!dto || !Array.isArray(dto.Criterias)) return dto;
    return {
      ...dto,
      Criterias: dto.Criterias.map((c: any) => ({
        ...c,
        CriteriaType: 1,
        IsAllowMultipleSelect: true,
        Values: Array.isArray(c?.Values) ? c.Values : [],
      })),
    };
  }, [eshopCardSearchDtoForFilter]);

  const hasEshopFilterOptions = React.useMemo(() => {
    const hasOptionGroups = Object.keys(eshopOptionDisplayByLevel).length > 0;
    const hasSearchFilterCriterias = Array.isArray(normalizedEshopFilterDto?.Criterias)
      ? normalizedEshopFilterDto.Criterias.length > 0
      : false;
    return hasOptionGroups || hasSearchFilterCriterias;
  }, [eshopOptionDisplayByLevel, normalizedEshopFilterDto]);

  const hasCheckedOption = React.useCallback((level: string) => {
    const arr = eshopOptionLevelMap?.[level];
    if (!Array.isArray(arr)) return false;
    return arr.some((x: any) => Boolean(x?.IsChecked));
  }, [eshopOptionLevelMap]);

  const setAllOptionsChecked = React.useCallback((level: string, checked: boolean) => {
    setEshopOptionLevelMap((prev) => {
      const src = Array.isArray(prev?.[level]) ? prev[level] : [];
      const nextLevel = src.map((x: any) => ({ ...x, IsChecked: checked }));
      const nextMap = { ...prev, [level]: nextLevel };
      eshopOptionLevelMapRef.current = nextMap;
      return nextMap;
    });
    window.setTimeout(() => void eshopCardSearchRef.current?.executeSearch(), 0);
  }, []);

  const toggleOneOption = React.useCallback((level: string, idx: number, checked: boolean) => {
    setEshopOptionLevelMap((prev) => {
      const src = Array.isArray(prev?.[level]) ? prev[level] : [];
      const nextLevel = src.map((x: any, i: number) => (i === idx ? { ...x, IsChecked: checked } : x));
      const nextMap = { ...prev, [level]: nextLevel };
      eshopOptionLevelMapRef.current = nextMap;
      return nextMap;
    });
    window.setTimeout(() => void eshopCardSearchRef.current?.executeSearch(), 0);
  }, []);

  const embeddedEshopCardParam = React.useMemo(() => {
    if (!isEshopCategorySearch || !eshopCardSearchId || !eshopCardSearchViewId) return null;
    if (String(eshopCardSearchId) === String(dataModel?.searchDto?.Id)) return null;
    return {
      searchId: eshopCardSearchId,
      isSavedSearch: false,
      initialViewId: eshopCardSearchViewId,
      isShowCriterias: false,
      isLinkedSearch: false,
      // Use a getter so executeSearch always sees latest override (avoid stale object reference).
      criteriaDictOverride: () => eshopCardCritRef.current,
      getTextCriteriaOverrides: () => eshopFilterTextOverridesGetterRef.current?.() ?? {},
      getSearchDtoOverrides: () => ({
        // Server-side logic does: orgDictOptionLevel != null && orgDictOptionLevel[optionLevel] != null
        // When we pass {}, orgDictOptionLevel is non-null but key may be missing => KeyNotFoundException.
        // Pass null until we have option levels from the first successful search result.
        DictFilterOptionLevelAndLookupList:
          eshopOptionLevelMapRef.current && Object.keys(eshopOptionLevelMapRef.current).length > 0
            ? eshopOptionLevelMapRef.current
            : null,
      }),
      onSearchResult: (searchResult: any) => {
        const dto = searchResult?.EshopCatalogViewDto ?? searchResult?.EshopCategoryViewDto ?? searchResult?.EshopCatelogViewDto ?? {};
        const optionDisplay = dto?.DictOptionDisplay ?? dto?.DictoptionDisplay ?? dto?.DictOptoinDisplay ?? dto?.DictoptoinDisplay ?? {};
        const optionLevel = dto?.DictOptionLevel ?? dto?.DictoptionLevel ?? {};
        if (optionDisplay && typeof optionDisplay === 'object') {
          setEshopOptionDisplayByLevel(optionDisplay as Record<string, string>);
        }
        if (optionLevel && typeof optionLevel === 'object') {
          setEshopOptionLevelMap(optionLevel as Record<string, any[]>);
          eshopOptionLevelMapRef.current = optionLevel as Record<string, any[]>;
        }

        const displayByLevel =
          dto?.DictoptionDisplay ??
          dto?.DictOptionDisplay ??
          dto?.DictoptoinDisplay ??
          dto?.DictOptoinDisplay ??
          {};
        const dictItems =
          dto?.DictoptionItems ??
          dto?.DictOptionItems ??
          dto?.DictoptionItemsArray ??
          dto?.DictOptionItemsArray ??
          {};
        const dictLevel =
          dto?.DictoptionLevel ??
          dto?.DictOptionLevel ??
          dto?.DictoptoinLevel ??
          dto?.DictOptoinLevel ??
          {};
        const itemsByLabel = Object.keys(dictLevel ?? {}).length > 0 ? dictLevel : dictItems;
        const pairs: { label: string; items: any[]; order: number }[] = [];
        const displayEntries = Object.entries(displayByLevel ?? {});
        if (displayEntries.length) {
          for (const [k, v] of displayEntries) {
            const label = String(v ?? '').trim();
            if (!label) continue;
            const arr = (itemsByLabel as any)?.[label] ?? (itemsByLabel as any)?.[k];
            if (Array.isArray(arr)) pairs.push({ label, items: arr, order: Number(k) || 0 });
          }
        }
        if (!pairs.length && itemsByLabel && typeof itemsByLabel === 'object') {
          for (const [k, v] of Object.entries(itemsByLabel)) {
            if (!Array.isArray(v)) continue;
            pairs.push({ label: String((displayByLevel as any)?.[k] ?? k), items: v as any[], order: Number(k) || 0 });
          }
        }
        if (!pairs.length) return;
        pairs.sort((a, b) => a.order - b.order);
        const criterias = pairs.map((p, idx) => ({
          SearcDCUID: `eshop_opt_${idx + 1}_${p.label}`,
          Display: p.label,
          RowIndex: 1,
          ColumnIndex: idx + 1,
          CriteriaType: 1,
          IsAllowMultipleSelect: true,
          IsOptionFilterChecklist: true,
          IsReadOnly: false,
          SupportedOperators: [],
          CriteriaOperator: null,
          Values: [],
          ItemsSource: (p.items ?? []).map((x: any) => ({
            Id: String(x?.Id ?? ''),
            Display: String(x?.Display ?? x?.Id ?? ''),
          })),
        }));
        setEshopCardSearchDtoForFilter({
          Criterias: criterias,
          CriteriasRowCount: 1,
        });
      },
    };
  }, [
    eshopCardSearchId,
    eshopCardSearchViewId,
    isEshopCategorySearch,
    dataModel?.searchDto?.Id,
    eshopOptionLevelMap,
  ]);

  // IMPORTANT: do not auto-execute card search based on state timing.
  // Tree click updates `eshopCardCritRef.current` synchronously then triggers executeSearch immediately,
  // avoiding stale-criteria races when clicking nodes quickly.

  // Auto-cache data for tab navigation
  useTabDataAutoCache(dataModel);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 mb-1 py-2 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-2">
          {!isEmbeddedChromeHidden ? (
          <div className={`text-md font-semibold ${theme.title}`}>
            {dataModel.currentSearchName}
          </div>
          ) : (
          <div className={`text-sm font-semibold ${theme.title}`}>Execution Log</div>
          )}
          {/* Hide/Show Filter collapse button - beside search label - only show if search has criteria */}
          {!isEshopCategorySearch &&
            dataModel.uiControl.isShowCriterias &&
            dataModel.searchDto &&
            dataModel.searchDto.Criterias &&
            dataModel.searchDto.Criterias.length > 0 && (
            <button
              onClick={toggleSearchCriteria}
              className={`w-6 h-6 flex items-center justify-center rounded transition-all ${
                dataModel.uiControl.isSearchCriteriaCollapsed
                  ? 'text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                  : 'text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900'
              } disabled:opacity-60 disabled:cursor-not-allowed`}
              title={dataModel.uiControl.isSearchCriteriaCollapsed ? "Show Filter" : "Hide Filter"}
              disabled={isBusy}
            >
              <i className={`fa fa-chevron-${dataModel.uiControl.isSearchCriteriaCollapsed ? 'down' : 'up'} text-xs`}></i>
            </button>
          )}
        </div>

        {shouldShowCriteriaButtons && (
          <div className="flex items-center gap-2">
            {/* Configuration button - only for admin users with configuration mode */}
            {enableConfigurationMode && isAdminUser && !isEmbeddedChromeHidden && (
              <div className="relative">
                <button
                  onClick={() => {
                    const currentSearchId = dataModel.searchid ?? paramObj.searchId ?? null;
                    if (!currentSearchId) {
                      showError('No searchId available for configuration');
                      return;
                    }
                    addTabAndNavigate(
                      isEshopCategorySearch ? 'eshop-category-search-editor' : 'search-editor',
                      dataModel.currentSearchName || (isEshopCategorySearch ? 'Eshop Category Search Editor' : 'Search Editor'),
                      { id: currentSearchId },
                      true,
                    );
                  }}
                  className="px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1 disabled:opacity-60 disabled:cursor-not-allowed"
                  title="Configuration"
                  disabled={isBusy}
                >
                  <i className="fa fa-gears"></i> Configuration
                </button>
              </div>
            )}

            {/* Eshop category search: keep ONLY Configuration button */}
            {!isEshopCategorySearch && (
              <>
                {/* Search button */}
                <button
                  onClick={executeSearch}
                  className="px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1 disabled:opacity-60 disabled:cursor-not-allowed"
                  title="Search"
                  disabled={isBusy}
                >
                  <i className="fa fa-search"></i> Search
                </button>

                {/* Clear button */}
                <button
                  onClick={clearCriteriaValues}
                  className="px-2 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 flex items-center gap-1 disabled:opacity-60 disabled:cursor-not-allowed"
                  title="Clear"
                  disabled={isBusy}
                >
                  <i className="fa fa-eraser"></i> Clear
                </button>

                {/* User Views dropdown - only show if more than 1 view */}
                {hasMultipleViews && (
                  <div className="relative" ref={userViewsDropdownRef}>
                    <button
                      onClick={() => setShowUserViewsDropdown(!showUserViewsDropdown)}
                      className="px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1 disabled:opacity-60 disabled:cursor-not-allowed"
                      title="User Views"
                      disabled={isBusy}
                    >
                      <i className="fa fa-list"></i> User Views <i className="fa fa-caret-down"></i>
                    </button>
                    {showUserViewsDropdown && (
                      <div className="absolute right-0 mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded shadow-lg z-50 min-w-[260px] max-h-[50vh] overflow-y-auto">
                        {dataModel.views?.map((view: any) => {
                          const isSelected =
                            dataModel.currentViewDefinition?.Id != null &&
                            view?.Id != null &&
                            String(dataModel.currentViewDefinition.Id) === String(view.Id);
                          return (
                            <button
                              key={view.Id}
                              onClick={() => handleViewChange(view)}
                              className={`w-full text-left px-4 py-2 text-sm hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between gap-2 ${isSelected ? 'bg-blue-50 dark:bg-blue-900' : ''}`}
                            >
                              <span className="min-w-0 flex-auto whitespace-nowrap overflow-hidden text-ellipsis">
                                {view.Display}
                              </span>
                              {isSelected && (
                                <i className="fa fa-check text-blue-600 dark:text-blue-400 shrink-0"></i>
                              )}
                            </button>
                          );
                        })}
                      </div>
                    )}
                  </div>
                )}

                {/* API Preview button - only show if DefaultAppProvideApiId exists */}
                {showApiPreview && (
                  <button
                    onClick={(e) => previewDefaultAppProvideApi(e)}
                    className="px-2 h-6 bg-purple-400 text-white rounded-[4px] text-xs hover:bg-purple-500 flex items-center gap-1 disabled:opacity-60 disabled:cursor-not-allowed"
                    title="API Preview"
                    disabled={isBusy}
                  >
                    <i className="fa fa-network-wired"></i> API Preview
                  </button>
                )}
              </>
            )}
          </div>
        )}
      </div>

      {/* Main Content */}
      <div className={`w-full flex-auto h-1 flex flex-col overflow-hidden`}>
        {/* Search Filter Section */}
        {dataModel.uiControl.isShowCriterias && !dataModel.uiControl.isSearchCriteriaCollapsed && dataModel.searchDto && (
          <div className={`w-full`}>
            <SearchFilter
              searchDto={dataModel.searchDto}
              onCriteriaValueChanged={handleCriteriaValueChanged}
              dictDcuValue={dataModel.dictDcuValue}
              dictFieldEntityDataSource={dataModel.dictFieldEntityDataSource}
              onRegisterTextCriteriaOverrides={(getter) => {
                textCriteriaOverridesGetterRef.current = getter;
              }}
              onRegisterLiveCriteriaOverrides={(getter) => {
                liveCriteriaOverridesGetterRef.current = getter;
              }}
              clearSignal={criteriaClearVersion}
            />
          </div>
        )}

        {/* Search Results Section */}
        <div className={`flex-auto h-1 overflow-hidden ${t('bg_mainContentSection')}`}>
          {!isEshopCategorySearch && dataModel.searchViewDto && (
            <SearchView
              viewDto={dataModel.searchViewDto}
              viewDataList={dataModel.searchResultCV}
              dataModel={dataModel}
              onExecuteSearch={executeSearch}
              onPatchDictDcuValueAndSearch={patchDictDcuValueAndSearch}
              onSelectionChanged={(selected) => {
                selectedLinkedRowsRef.current = Array.isArray(selected) ? selected : [];
                if (!(isEshopCategorySearch && currentViewType === EmAppViewType.FlatDataSetTreeView)) return;
                const item = Array.isArray(selected) ? selected[0] : null;
                const mapObj = eshopCategoryMapping ?? {};
                if (!item || !item?.DictViewColumnIDKeyValue || !mapObj) return;
                const patch: Record<string, any> = {};
                const targetKeys: string[] = [];
                for (let i = 1; i <= 5; i++) {
                  const sourceViewColumnId = mapObj?.[`SourceViewColumnId${i}`];
                  const targetFieldPath = mapObj?.[`TargetSearchFieldName${i}`];
                  if (!targetFieldPath) continue;
                  targetKeys.push(String(targetFieldPath));
                  if (!sourceViewColumnId) continue;
                  const sourceValue = item.DictViewColumnIDKeyValue?.[sourceViewColumnId];
                  if (sourceValue == null || String(sourceValue).trim() === '') continue;
                  patch[String(targetFieldPath)] = [String(sourceValue)];
                }

                if (!targetKeys.length) return;
                setEshopCardCrit((prev) => {
                  const next = { ...(prev ?? {}) };
                  for (const k of targetKeys) {
                    if (Object.prototype.hasOwnProperty.call(patch, k)) next[k] = patch[k];
                    else delete next[k];
                  }
                  eshopCardCritRef.current = next;
                  return next;
                });
                // Trigger immediately using latest ref (avoid stale state).
                window.setTimeout(() => void eshopCardSearchRef.current?.executeSearch(), 0);
              }}
            />
          )}
          {isEshopCategorySearch && (
            <div className="w-full h-full flex overflow-hidden">
              <div className="w-[420px] min-w-[420px] border-r overflow-hidden">
                {dataModel.searchViewDto && (
                  <SearchView
                    viewDto={dataModel.searchViewDto}
                    viewDataList={dataModel.searchResultCV}
                    dataModel={dataModel}
                    onExecuteSearch={executeSearch}
                    onPatchDictDcuValueAndSearch={patchDictDcuValueAndSearch}
                    onSelectionChanged={(selected) => {
                      selectedLinkedRowsRef.current = Array.isArray(selected) ? selected : [];
                      if (!(isEshopCategorySearch && currentViewType === EmAppViewType.FlatDataSetTreeView)) return;
                      const item = Array.isArray(selected) ? selected[0] : null;
                      const mapObj = eshopCategoryMapping ?? {};
                      if (!item || !item?.DictViewColumnIDKeyValue || !mapObj) return;
                      const patch: Record<string, any> = {};
                      const targetKeys: string[] = [];
                      for (let i = 1; i <= 5; i++) {
                        const sourceViewColumnId = mapObj?.[`SourceViewColumnId${i}`];
                        const targetFieldPath = mapObj?.[`TargetSearchFieldName${i}`];
                        if (!targetFieldPath) continue;
                        targetKeys.push(String(targetFieldPath));
                        if (!sourceViewColumnId) continue;
                        const sourceValue = item.DictViewColumnIDKeyValue?.[sourceViewColumnId];
                        if (sourceValue == null || String(sourceValue).trim() === '') continue;
                        patch[String(targetFieldPath)] = [String(sourceValue)];
                      }
                      if (!targetKeys.length) return;
                const patchSig = JSON.stringify(Object.keys(patch).sort().map((k) => [k, patch[k]]));
                if (lastTreeSelectionPatchSigRef.current === patchSig) return;
                lastTreeSelectionPatchSigRef.current = patchSig;
                      setEshopCardCrit((prev) => {
                        const next = { ...(prev ?? {}) };
                        for (const k of targetKeys) {
                          if (Object.prototype.hasOwnProperty.call(patch, k)) next[k] = patch[k];
                          else delete next[k];
                        }
                        eshopCardCritRef.current = next;
                        return next;
                      });
                      window.setTimeout(() => void eshopCardSearchRef.current?.executeSearch(), 0);
                    }}
                  />
                )}
              </div>
              <div className="flex-auto min-w-0 overflow-hidden flex">
                {hasEshopFilterOptions ? (
                  <>
                    <div className="w-[360px] min-w-[360px] border-r flex flex-col overflow-hidden">
                      <div className={`px-3 py-1 text-xs border-b ${theme.mainContentSection}`}>Filter Options</div>
                      <div className="flex-auto min-h-0 overflow-auto p-2">
                        {Object.keys(eshopOptionDisplayByLevel).length > 0 ? (
                          <>
                            {Object.entries(eshopOptionDisplayByLevel)
                              .sort((a: any, b: any) => Number(a?.[0] || 0) - Number(b?.[0] || 0))
                              .map(([level, display]) => {
                                const lvl = String(level);
                                const isCollapsed = Boolean(collapsedEshopOptionLevels?.[lvl]);
                                return (
                                  <div key={`opt-level-${lvl}`} className="mb-3 border rounded overflow-hidden">
                                    <button
                                      type="button"
                                      className={`w-full px-2 py-1 text-xs font-semibold border-b ${theme.mainContentSection} flex items-center justify-between`}
                                      onClick={() => toggleCollapsedEshopOptionLevel(lvl)}
                                    >
                                      <span className="truncate">{String(display || lvl)}</span>
                                      <i
                                        className={`fa-solid fa-chevron-${isCollapsed ? 'right' : 'down'} text-xs`}
                                        aria-hidden="true"
                                      />
                                    </button>
                                    {!isCollapsed && (
                                      <>
                                        <div className="px-2 py-1 flex gap-2">
                                          <button
                                            type="button"
                                            className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                                            onClick={() => setAllOptionsChecked(lvl, true)}
                                          >
                                            Select All
                                          </button>
                                          <button
                                            type="button"
                                            className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                                            onClick={() => {
                                              setAllOptionsChecked(lvl, false);
                                              setEshopCardCrit({});
                                              eshopCardCritRef.current = {};
                                              setEshopFilterClearVersion((v) => v + 1);
                                            }}
                                          >
                                            Clear
                                          </button>
                                        </div>
                                        <div className="px-2 pb-2">
                                          {(eshopOptionLevelMap?.[lvl] ?? [])
                                            .filter((x: any) => String(x?.Display ?? '').trim() !== '')
                                            .map((opt: any, idx: number) => (
                                              <label
                                                key={`opt-${lvl}-${idx}`}
                                                className="flex items-center gap-2 py-1 text-xs"
                                              >
                                                <input
                                                  type="checkbox"
                                                  checked={Boolean(opt?.IsChecked)}
                                                  onChange={(e) => toggleOneOption(lvl, idx, e.target.checked)}
                                                />
                                                <span className="truncate">{String(opt?.Display ?? '')}</span>
                                              </label>
                                            ))}
                                        </div>
                                      </>
                                    )}
                                  </div>
                                );
                              })}
                            {/* Refresh/Clear row removed: Clear is now beside Select All */}
                          </>
                        ) : normalizedEshopFilterDto ? (
                          <SearchFilter
                            searchDto={normalizedEshopFilterDto}
                            dictDcuValue={eshopCardCrit}
                            onCriteriaValueChanged={(searchFieldId, value) => {
                              setEshopCardCrit((prev) => ({ ...prev, [searchFieldId]: value }));
                            }}
                            onRegisterTextCriteriaOverrides={(getter) => {
                              eshopFilterTextOverridesGetterRef.current = getter;
                            }}
                            clearSignal={eshopFilterClearVersion}
                          />
                        ) : null}
                      </div>
                    </div>
                    <div className="flex-auto min-w-0 overflow-hidden">
                      {embeddedEshopCardParam ? (
                        <AppSearch ref={eshopCardSearchRef} embeddedParamObj={embeddedEshopCardParam} />
                      ) : (
                        <div className="p-3 text-xs text-gray-600">Card/detail view is not configured.</div>
                      )}
                    </div>
                  </>
                ) : (
                  <div className="flex-auto min-w-0 overflow-hidden">
                    {embeddedEshopCardParam ? (
                      <AppSearch ref={eshopCardSearchRef} embeddedParamObj={embeddedEshopCardParam} />
                    ) : (
                      <div className="p-3 text-xs text-gray-600">Card/detail view is not configured.</div>
                    )}
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* API Call Preview Popup */}
      {apiPreviewOpen && (
        <div
          className="fixed inset-0 z-[10000] bg-black/40 flex items-center justify-center"
          onClick={() => {
            setApiPreviewOpen(false);
          }}
        >
          <div
            className="rounded-[6px] border border-gray-200 bg-white shadow-lg overflow-hidden"
            style={{ width: 900, height: 700 }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-3 py-2 border-b">
              <div className={`text-sm font-semibold ${theme.title}`}>API Call Preview</div>
              <button
                type="button"
                className={`px-2 h-6 rounded-[4px] text-xs ${theme.button_default}`}
                onClick={() => setApiPreviewOpen(false)}
              >
                Close
              </button>
            </div>
            <div className="h-[calc(100%-44px)] p-3 flex flex-col gap-2">
              <div className="text-xs text-gray-600 break-all">
                <span className="font-semibold">URL:</span> {apiPreviewUrl}
              </div>
              <div className="flex-auto overflow-auto">
                {apiPreviewLoading ? (
                  <div className="text-xs">Loading...</div>
                ) : (
                  <pre className="text-[11px] whitespace-pre-wrap break-words">{apiPreviewResultText}</pre>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
});

AppSearch.displayName = 'AppSearch';

export default AppSearch; 