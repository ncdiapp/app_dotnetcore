import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { createPortal } from 'react-dom';
import { useDispatch, useSelector } from 'react-redux';
import { useParams, useNavigate } from 'react-router-dom';
import type { RootState } from '../../redux/store';
import { closeTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { integrationService } from '../../webapi/integrationsvc';
import { QueryAgentPanel } from './QueryAgentPanel';

// Query Type enum matching AngularJS EmAppDataServiceType
const EmAppDataServiceType = {
  QueryText: 1,
  StoredProcedure: 2,
  PluginWebApiCall: 3,
  IntegrationWebApiCall: 4
};

// Query type display names for grid/list (match Angular)
const queryTypeDisplayNames: Record<number, string> = {
  [EmAppDataServiceType.QueryText]: 'Query Service',
  [EmAppDataServiceType.StoredProcedure]: 'Stored Procedure Service',
  [EmAppDataServiceType.PluginWebApiCall]: 'Internal Web Api Call Service',
  [EmAppDataServiceType.IntegrationWebApiCall]: 'Integration Web Api Call Service'
};

// Editor page titles (match Angular .cshtml labels)
const editorTitleNames: Record<number, string> = {
  [EmAppDataServiceType.QueryText]: 'Query Data Service Editor',
  [EmAppDataServiceType.StoredProcedure]: 'Stored Procedure Data Service Editor',
  [EmAppDataServiceType.PluginWebApiCall]: 'Internal Web Api Call Data Service Editor',
  [EmAppDataServiceType.IntegrationWebApiCall]: 'Integration Web Api Call Data Service Editor'
};

// Transform Type (UsageTypeId) – match Angular EmAppDataSetUsageType
const EmAppDataSetUsageType = { Default: 1, ConvertSimpleObjectToList: 2 } as const;
const usageTypeOptions: { id: number; display: string }[] = [
  { id: EmAppDataSetUsageType.Default, display: 'Default' },
  { id: EmAppDataSetUsageType.ConvertSimpleObjectToList, display: 'ConvertSimpleObjectToList' }
];

// Uniform property layout for all dataset types: align rows vertically
const PROP_LABEL_WIDTH = 220;
const PROP_INPUT_WIDTH = 250;

/** When used as route (dataset-editor/:param), param is parsed from URL and onClose closes tab. Props are optional when param is present. */
interface DataSetEditorProps {
  dataSetId?: number | null;
  queryType?: number;
  dataSourceRegisterId?: number | null;
  onSave?: (dataSet: any) => void;
  onClose?: () => void;
  /** Optional: default dataset name when creating a new dataset in embedded mode. */
  initialName?: string;
  /** Optional: save first, then parent handles close + follow-up flow. */
  onConfirmAndClose?: (savedDataSet: any) => void | Promise<void>;
  /** When embedded in another routed page (e.g. Search Editor), ignore URL `param` so search tab JSON is not mistaken for dataset id. */
  ignoreRouteParam?: boolean;
}

interface DataSetParameter {
  Id?: number;
  ParameterName: string;
  DbDataType?: string;
  DefaultValue?: string;
  Description?: string;
}

function flattenFetchNodes(nodes: any[], out: { AbsolutePath?: string; Display?: string }[] = []): { AbsolutePath?: string; Display?: string }[] {
  (nodes || []).forEach((n: any) => {
    out.push({ AbsolutePath: n.AbsolutePath, Display: n.Display });
    if (n.Children?.length) flattenFetchNodes(n.Children, out);
  });
  return out;
}

/** Build node tree from plain JSON – Angular style: only keys whose value is an object (not primitive/array) become nodes. */
function buildNodeTreeFromJson(obj: any, pathPrefix: string = ''): { Display: string; AbsolutePath: string; Children: any[] }[] {
  if (obj == null || typeof obj !== 'object' || Array.isArray(obj)) return [];
  return Object.keys(obj)
    .filter((key) => {
      const value = obj[key];
      return value != null && typeof value === 'object' && !Array.isArray(value);
    })
    .map((key) => {
      const absolutePath = pathPrefix ? `${pathPrefix}.${key}` : key;
      const value = obj[key];
      const children = buildNodeTreeFromJson(value, absolutePath);
      return {
        Display: key,
        AbsolutePath: absolutePath,
        Children: children
      };
    });
}

const DataSetEditor: React.FC<DataSetEditorProps> = (props) => {
  const { onSave, onClose: onCloseProp, initialName, onConfirmAndClose, ignoreRouteParam = false } = props;
  const { param } = useParams<{ param?: string }>();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((state: RootState) => state.tabnav.previousActiveTabKey);

  const parsed = useMemo(() => {
    if (ignoreRouteParam || !param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const id = obj.id ?? obj.Id;
      return {
        dataSetId: id != null ? Number(id) : null,
        queryType: Number(obj.queryType) || 1,
        dataSourceRegisterId: obj.dataSourceRegisterId != null ? Number(obj.dataSourceRegisterId) : null
      };
    } catch {
      return null;
    }
  }, [param, ignoreRouteParam]);

  const dataSetId = parsed?.dataSetId ?? props.dataSetId ?? null;
  const queryType = parsed?.queryType ?? props.queryType ?? 1;
  const dataSourceRegisterId = parsed?.dataSourceRegisterId ?? props.dataSourceRegisterId ?? null;

  const handleClose = useCallback(() => {
    if (onCloseProp) {
      onCloseProp();
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
    const newPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(activeTabKey));
    navigate(newPath);
  }, [onCloseProp, activeTabKey, previousActiveTabKey, tabs, dispatch, navigate]);

  const { theme, t } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  // Core state
  const [currentDataSet, setCurrentDataSet] = useState<any>({
    Id: null,
    Name: '',
    Description: '',
    QueryType: queryType,
    QueryText: '',
    DataSourceFrom: dataSourceRegisterId || null,
    AppDataSetParameterList: [],
    BaseTableName: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [isConfirmAndCloseBusy, setIsConfirmAndCloseBusy] = useState(false);

  // Data source state
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<any[]>([]);

  // SQL Query specific state
  const [tablesAndViews, setTablesAndViews] = useState<any[]>([]);
  const [queryResults, setQueryResults] = useState<any[]>([]);
  const [queryResultsCV, setQueryResultsCV] = useState<CollectionView | null>(null);

  // Stored Procedure specific state
  const [storedProcedures, setStoredProcedures] = useState<string[]>([]);
  const [spSearchText, setSpSearchText] = useState('');
  const [parametersCV, setParametersCV] = useState<CollectionView | null>(null);
  const [showSpSelector, setShowSpSelector] = useState(false);

  // Internal Web Api Call (Plugin) specific state: editable parameter list
  const [internalWebApiParameterList, setInternalWebApiParameterList] = useState<DataSetParameter[]>([]);
  const internalWebApiParametersCV = useMemo(
    () => new CollectionView(internalWebApiParameterList),
    [internalWebApiParameterList]
  );
  const internalWebApiGridRef = useRef<wjGrid.FlexGrid | null>(null);

  // Integration Web Api Call: settings (for two-level menu) + flat list (for selected display) + fetch nodes
  const [integrationSettingList, setIntegrationSettingList] = useState<any[]>([]);
  const [apiOperationList, setApiOperationList] = useState<{ id: string; actionDisplay: string }[]>([]);
  const [apiFetchNodeList, setApiFetchNodeList] = useState<{ AbsolutePath?: string; Display?: string }[]>([]);
  const [apiFetchNodeTree, setApiFetchNodeTree] = useState<any[]>([]);
  const [apiFetchNodesLoading, setApiFetchNodesLoading] = useState(false);
  const [apiFetchNodesError, setApiFetchNodesError] = useState(false);
  const [apiFetchNodesRetryTrigger, setApiFetchNodesRetryTrigger] = useState(0);
  const [showApiOperationDropdown, setShowApiOperationDropdown] = useState(false);
  const [hoveredIntegrationId, setHoveredIntegrationId] = useState<string | number | null>(null);
  const [showFetchNodeDropdown, setShowFetchNodeDropdown] = useState(false);
  const apiOperationDropdownRef = useRef<HTMLDivElement>(null);
  const apiOperationTriggerRef = useRef<HTMLDivElement>(null);
  const fetchNodeDropdownRef = useRef<HTMLDivElement>(null);
  const fetchNodeTriggerRef = useRef<HTMLDivElement>(null);
  const API_OP_SUBMENU_CLOSE_DELAY_MS = 350;
  const apiOpSubmenuCloseTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [apiOpDropdownPosition, setApiOpDropdownPosition] = useState<{ top: number; left: number } | null>(null);
  const [apiOpSubmenuPosition, setApiOpSubmenuPosition] = useState<{ top: number; left: number } | null>(null);
  const [fetchNodeDropdownPosition, setFetchNodeDropdownPosition] = useState<{ top: number; left: number } | null>(null);
  const [showCreateInDropdown, setShowCreateInDropdown] = useState(false);
  const [createInDropdownPosition, setCreateInDropdownPosition] = useState<{ top: number; left: number } | null>(null);
  const createInTriggerRef = useRef<HTMLButtonElement>(null);
  const createInDropdownRef = useRef<HTMLDivElement>(null);

  // Panel sizes (resizable)
  const [leftPanelWidth, setLeftPanelWidth] = useState(250);
  const [bottomPanelHeight, setBottomPanelHeight] = useState(200);
  const [isResizingHorizontal, setIsResizingHorizontal] = useState(false);
  const [isResizingVertical, setIsResizingVertical] = useState(false);

  // Data Model Selector modal (Generate Query From Data Model)
  const [showDataModelSelector, setShowDataModelSelector] = useState(false);
  const [dataModelListCV, setDataModelListCV] = useState<CollectionView | null>(null);
  const dataModelSelectorGridRef = useRef<wjGrid.FlexGrid | null>(null);

  // Query AI Agent
  const [showQueryAgent, setShowQueryAgent] = useState(false);
  const [selectedTablesForAI, setSelectedTablesForAI] = useState<Set<string>>(new Set());

  // Refs
  const queryTextareaRef = useRef<HTMLTextAreaElement>(null);

  // Get editor title (match Angular: show dataset name when editing, else editor title)
  const { addTabAndNavigate } = useTabNavigation();

  const getEditorTitle = (): string => {
    if (dataSetId && currentDataSet.Name) {
      return currentDataSet.Name;
    }
    return editorTitleNames[queryType] ?? 'Data Service Editor';
  };

  // Load initial data
  useEffect(() => {
    loadInitialData();
  }, [dataSetId, queryType]);

  // Close Integration dropdowns on outside click
  useEffect(() => {
    if (!showApiOperationDropdown && !showFetchNodeDropdown && !showCreateInDropdown) return;
    const onDocClick = (e: MouseEvent) => {
      const t = e.target as Node;
      if (
        apiOperationDropdownRef.current?.contains(t) ||
        fetchNodeDropdownRef.current?.contains(t) ||
        createInDropdownRef.current?.contains(t)
      ) return;
      setShowApiOperationDropdown(false);
      setShowFetchNodeDropdown(false);
      setShowCreateInDropdown(false);
      setApiOpDropdownPosition(null);
      setApiOpSubmenuPosition(null);
      setFetchNodeDropdownPosition(null);
      setCreateInDropdownPosition(null);
    };
    document.addEventListener('click', onDocClick);
    return () => document.removeEventListener('click', onDocClick);
  }, [showApiOperationDropdown, showFetchNodeDropdown, showCreateInDropdown]);

  // Load fetch data node structure when Integration API operation (QueryText) changes
  const apiFetchQueryTextRef = useRef<string | null>(null);
  useEffect(() => {
    if (queryType !== EmAppDataServiceType.IntegrationWebApiCall || !currentDataSet.QueryText) {
      if (!currentDataSet.QueryText) {
        setApiFetchNodeList([]);
        setApiFetchNodeTree([]);
        setApiFetchNodesLoading(false);
        setApiFetchNodesError(false);
      }
      return;
    }
    const queryTextForFetch = String(currentDataSet.QueryText);
    apiFetchQueryTextRef.current = queryTextForFetch;
    setApiFetchNodesError(false);
    // Clear immediately so UI does not show stale tree when switching API or reopening
    setApiFetchNodeList([]);
    setApiFetchNodeTree([]);
    setApiFetchNodesLoading(true);

    let cancelled = false;
    integrationService.retrieveOneAppIntegrationSettingParameterExDto(queryTextForFetch, true).then((dto: any) => {
      if (cancelled) return;
      // Only apply result if this is still the current API operation (user may have switched)
      if (apiFetchQueryTextRef.current !== queryTextForFetch) return;
      setApiFetchNodesLoading(false);
      const nodes =
        dto?.ApiAvailableFetcheDataJsonNodeStructure
        ?? dto?.ApiAvailableFetchDataJsonNodeStructure
        ?? dto?.ApiAvailableFetchDataNodeStructure
        ?? dto?.ApiFetchDataJsonNodeStructure
        ?? [];
      let nodeArray = Array.isArray(nodes) ? nodes : nodes ? [nodes] : [];
      let sampleStrForDisplay = '';
      if (nodeArray.length === 0) {
        const sampleRawForTree = dto?.JsonSampleData ?? dto?.JsonSample ?? dto?.SampleResponse ?? dto?.ResponseJsonData;
        if (sampleRawForTree != null && sampleRawForTree !== '') {
          try {
            const parsed = typeof sampleRawForTree === 'string' ? JSON.parse(sampleRawForTree) : sampleRawForTree;
            if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
              nodeArray = buildNodeTreeFromJson(parsed);
              sampleStrForDisplay = typeof sampleRawForTree === 'string' ? JSON.stringify(parsed, null, 2) : JSON.stringify(parsed, null, 2);
            }
          } catch (_e) {
            /* ignore parse error */
          }
        }
      }
      const flat = flattenFetchNodes(nodeArray);
      setApiFetchNodeList(flat);
      setApiFetchNodeTree(nodeArray);
      if (sampleStrForDisplay) {
        setCurrentDataSet((prev: any) => ({ ...prev, OtherSettings: prev?.OtherSettings?.trim() || sampleStrForDisplay }));
      }
      // Init Fetch Data Parent Node Path to first meaningful node when we have nodes and current value is empty (skip root "{}")
      if (flat.length > 0) {
        const firstMeaningful = flat.find((n) => {
          const path = n.AbsolutePath ?? '';
          const display = n.Display ?? '';
          return path !== '' && display !== '{}';
        });
        const firstPath = firstMeaningful?.AbsolutePath ?? firstMeaningful?.Display ?? flat[0]?.AbsolutePath ?? flat[0]?.Display ?? '';
        if (firstPath && firstPath !== '{}') {
          setCurrentDataSet((prev: any) => {
            const current = prev?.BaseTableName ?? '';
            if (current) return prev;
            return { ...prev, BaseTableName: firstPath };
          });
        }
      }
      // Populate Json Sample Data from integration parameter DTO when we have sample (if not already set above from tree fallback)
      const sampleRaw = dto?.JsonSampleData ?? dto?.JsonSample ?? dto?.SampleResponse ?? dto?.ResponseJsonData ?? '';
      if (sampleRaw != null && sampleRaw !== '' && !sampleStrForDisplay) {
        let sampleStr: string;
        if (typeof sampleRaw === 'string') {
          try {
            const parsed = JSON.parse(sampleRaw);
            sampleStr = JSON.stringify(parsed, null, 2);
          } catch {
            sampleStr = sampleRaw;
          }
        } else {
          sampleStr = JSON.stringify(sampleRaw, null, 2);
        }
        setCurrentDataSet((prev: any) => ({ ...prev, OtherSettings: (prev?.OtherSettings ?? '').trim() || sampleStr }));
      }
    }).catch(() => {
      if (!cancelled && apiFetchQueryTextRef.current === queryTextForFetch) {
        setApiFetchNodeList([]);
        setApiFetchNodeTree([]);
        setApiFetchNodesError(true);
      }
      setApiFetchNodesLoading(false);
    });
    return () => {
      cancelled = true;
      setApiFetchNodesLoading(false);
    };
  }, [queryType, currentDataSet.QueryText, apiFetchNodesRetryTrigger]);

  // When API did not return node structure but we have Json Sample Data (OtherSettings), derive node tree from it
  useEffect(() => {
    if (queryType !== EmAppDataServiceType.IntegrationWebApiCall || !currentDataSet.QueryText) return;
    if (apiFetchNodeList.length > 0) return;
    const raw = (currentDataSet.OtherSettings ?? '').trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        const derived = buildNodeTreeFromJson(parsed);
        if (derived.length > 0) {
          setApiFetchNodeList(flattenFetchNodes(derived));
          setApiFetchNodeTree(derived);
        }
      }
    } catch (_e) {
      /* ignore */
    }
  }, [queryType, currentDataSet.QueryText, currentDataSet.OtherSettings, apiFetchNodeList.length]);

  const loadInitialData = async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      // Load data sources
      const dsRegisterList = await adminSvc.getDataSourceRegisterList(false);
      setDataSourceRegisterList(dsRegisterList || []);

      // Load existing dataset if editing
      if (dataSetId) {
        const dataSet = await searchSvc.retrieveOneAppDataSetExDto(dataSetId.toString(), false);
        if (dataSet) {
          const otherSettingsForDisplay =
            dataSet.OtherSettings
            ?? dataSet.JsonSampleData
            ?? dataSet.OtherSettingsDto?.JsonSampleData
            ?? (typeof dataSet.OtherSettingsDto === 'string' ? dataSet.OtherSettingsDto : '');
          const normalizedOther = typeof otherSettingsForDisplay === 'string'
            ? otherSettingsForDisplay
            : (otherSettingsForDisplay != null ? JSON.stringify(otherSettingsForDisplay, null, 2) : '');
          setCurrentDataSet({ ...dataSet, OtherSettings: normalizedOther || dataSet.OtherSettings || '' });

          // Load type-specific data
          if (dataSet.QueryType === EmAppDataServiceType.QueryText) {
            await loadTablesAndViews(dataSet.DataSourceFrom);
          } else if (dataSet.QueryType === EmAppDataServiceType.StoredProcedure) {
            await loadStoredProcedures(dataSet.DataSourceFrom);
            if (dataSet.AppDataSetParameterList) {
              setParametersCV(new CollectionView(dataSet.AppDataSetParameterList));
            }
          } else if (dataSet.QueryType === EmAppDataServiceType.PluginWebApiCall) {
            setInternalWebApiParameterList(dataSet.AppDataSetParameterList || []);
          } else if (dataSet.QueryType === EmAppDataServiceType.IntegrationWebApiCall) {
            // Do not set node list/tree from DTO here – useEffect will fetch fresh from API by QueryText so parent node path is stable when reopening editor
          }
        }
      } else {
        // New dataset - set default data source
        const dsId = dataSourceRegisterId || (dsRegisterList?.length > 0 ? dsRegisterList[0].Id : null);
        if (dsId) {
          setCurrentDataSet((prev: any) => ({ ...prev, DataSourceFrom: dsId }));
          if (queryType === EmAppDataServiceType.QueryText) {
            await loadTablesAndViews(dsId);
          } else if (queryType === EmAppDataServiceType.StoredProcedure) {
            await loadStoredProcedures(dsId);
          } else if (queryType === EmAppDataServiceType.PluginWebApiCall) {
            setInternalWebApiParameterList([]);
          }
        }
      }
      // Load Integration API operations when type is Integration (new or edit)
      if (queryType === EmAppDataServiceType.IntegrationWebApiCall) {
        try {
          const settings = await integrationService.retrieveAllAppIntegrationSettingDto(false);
          setIntegrationSettingList(settings || []);
          const flat: { id: string; actionDisplay: string }[] = [];
          (settings || []).forEach((s: any) => {
            (s.AppIntergrationSettingParameterList || s.AppIntegrationSettingParameterList || []).forEach((p: any) => {
              flat.push({
                id: String(p.Id),
                actionDisplay: s.Name ? `${s.Name} . ${p.ActionCode}` : p.ActionCode
              });
            });
          });
          setApiOperationList(flat);
        } catch (_e) {
          setIntegrationSettingList([]);
          setApiOperationList([]);
        }
      }
    } catch (error) {
      showError('Failed to load data: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  };

  // Load tables and views for SQL Query type
  const loadTablesAndViews = async (dsId: number | null) => {
    if (!dsId) return;
    try {
      const result = await schemaMetadataService.getDataSourceTableAndViewList(dsId, null, null);
      setTablesAndViews(Array.isArray(result) ? result : []);
    } catch (error) {
      console.error('Failed to load tables and views:', error);
    }
  };

  // Load stored procedures
  const loadStoredProcedures = async (dsId: number | null) => {
    if (!dsId) return;
    try {
      const result = await schemaMetadataService.getSysStoredProcedureNameList('', dsId);
      setStoredProcedures(Array.isArray(result) ? result : []);
    } catch (error) {
      console.error('Failed to load stored procedures:', error);
    }
  };

  // Handle data source change
  const handleDataSourceChange = async (newDataSourceId: number) => {
    setCurrentDataSet((prev: any) => ({ ...prev, DataSourceFrom: newDataSourceId }));
    setIsModified(true);

    if (queryType === EmAppDataServiceType.QueryText) {
      await loadTablesAndViews(newDataSourceId);
    } else if (queryType === EmAppDataServiceType.StoredProcedure) {
      await loadStoredProcedures(newDataSourceId);
    }
  };

  // Handle field changes
  const handleFieldChange = (field: string, value: any) => {
    setCurrentDataSet((prev: any) => ({ ...prev, [field]: value }));
    setIsModified(true);
  };

  // Execute SQL query
  const handleExecuteQuery = async () => {
    const queryText = currentDataSet.QueryText?.trim();
    if (!queryText) {
      showWarning('Please enter a SQL query');
      return;
    }
    if (!currentDataSet.DataSourceFrom) {
      showWarning('Please select a data source');
      return;
    }

    dispatch(setIsBusy());
    try {
      const keyValue = { Key: currentDataSet.DataSourceFrom, Value: queryText };
      const result = await schemaMetadataService.executeQueryResult(keyValue);

      // API may return both ErrorMessage "Query completed successfully" and DataRowList; only treat as error when not success message (see Angular/AppJsonQueryApiEditor)
      const isSuccessMessage = result?.ErrorMessage === 'Query completed successfully';
      if (result?.ErrorMessage && !isSuccessMessage) {
        showError(result.ErrorMessage);
        setQueryResults([]);
        setQueryResultsCV(null);
      } else {
        const rows = Array.isArray(result?.DataRowList)
          ? result.DataRowList
          : Array.isArray(result)
            ? result
            : [];
        setQueryResults(rows);
        setQueryResultsCV(rows.length > 0 ? new CollectionView(rows) : null);
      }
    } catch (error) {
      showError(error instanceof Error ? error.message : String(error));
      setQueryResults([]);
      setQueryResultsCV(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle stored procedure selection
  const handleSelectStoredProcedure = async (spName: string) => {
    setCurrentDataSet((prev: any) => ({ ...prev, QueryText: spName }));
    setIsModified(true);
    setShowSpSelector(false);

    // Load parameters for the stored procedure
    if (currentDataSet.DataSourceFrom) {
      try {
        const params = await schemaMetadataService.getStoredProcedureParameterList(
          spName,
          null,
          currentDataSet.DataSourceFrom
        );
        setCurrentDataSet((prev: any) => ({ ...prev, AppDataSetParameterList: params || [] }));
        setParametersCV(new CollectionView(params || []));
      } catch (error) {
        console.error('Failed to load SP parameters:', error);
      }
    }
  };

  // Save dataset
  const handleSave = useCallback(async (): Promise<any | null> => {
    if (!currentDataSet.Name?.trim()) {
      showWarning('Please enter a dataset name');
      return null;
    }

    dispatch(setIsBusy());
    try {
      const dataToSave = { ...currentDataSet };
      if (dataToSave.QueryType === EmAppDataServiceType.QueryText) {
        dataToSave.AppDataSetParameterList = [];
      } else if (dataToSave.QueryType === EmAppDataServiceType.PluginWebApiCall) {
        dataToSave.AppDataSetParameterList = internalWebApiParameterList;
      }

      const result = await searchSvc.saveOneAppDataSetEntityDto(dataToSave);

      if (result?.IsSuccessful && result?.Object) {
        showInfo('Dataset saved successfully');
        setCurrentDataSet(result.Object);
        if (result.Object.QueryType === EmAppDataServiceType.PluginWebApiCall) {
          setInternalWebApiParameterList(result.Object.AppDataSetParameterList || []);
        }
        setIsModified(false);
        onSave?.(result.Object);
        return result.Object;
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to save dataset';
        showError(errorMsg);
        return null;
      }
    } catch (error) {
      showError('Failed to save dataset: ' + (error instanceof Error ? error.message : String(error)));
      return null;
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentDataSet, internalWebApiParameterList, dispatch, onSave, showError, showInfo, showWarning]);

  useEffect(() => {
    if (!dataSetId && initialName?.trim()) {
      setCurrentDataSet((prev: any) => {
        if (!prev) return prev;
        if (prev.Id) return prev;
        if ((prev.Name || '').trim()) return prev;
        return { ...prev, Name: initialName.trim() };
      });
    }
  }, [dataSetId, initialName]);

  const handleConfirmAndClose = useCallback(async () => {
    if (!onConfirmAndClose) return;
    setIsConfirmAndCloseBusy(true);
    try {
      const saved = await handleSave();
      if (!saved) return;
      await onConfirmAndClose(saved);
    } finally {
      setIsConfirmAndCloseBusy(false);
    }
  }, [onConfirmAndClose, handleSave]);

  // Refresh: reload dataset dto from server, then reload tables/views (bypass cache)
  const handleRefresh = useCallback(async () => {
    const effectiveId = dataSetId ?? currentDataSet?.Id;
    dispatch(setIsBusy());
    try {
      if (effectiveId) {
        const dataSet = await searchSvc.retrieveOneAppDataSetExDto(String(effectiveId), false);
        if (dataSet) {
          setCurrentDataSet(dataSet);
          if (dataSet.QueryType === EmAppDataServiceType.QueryText && dataSet.DataSourceFrom) {
            schemaMetadataService.clearTableListCache();
            const result = await schemaMetadataService.getDataSourceTableAndViewList(dataSet.DataSourceFrom, null, null, { bypassHttpCache: true });
            setTablesAndViews(Array.isArray(result) ? [...result] : []);
          } else if (dataSet.QueryType === EmAppDataServiceType.StoredProcedure && dataSet.DataSourceFrom) {
            await loadStoredProcedures(dataSet.DataSourceFrom);
            if (dataSet.AppDataSetParameterList?.length) {
              setParametersCV(new CollectionView(dataSet.AppDataSetParameterList));
            }
          } else if (dataSet.QueryType === EmAppDataServiceType.PluginWebApiCall) {
            setInternalWebApiParameterList(dataSet.AppDataSetParameterList || []);
          }
        }
      } else {
        if (queryType === EmAppDataServiceType.IntegrationWebApiCall) {
          // No saved dataset: refresh integration list and re-fetch fetch-node tree for current Api Operation
          try {
            const settings = await integrationService.retrieveAllAppIntegrationSettingDto(false);
            setIntegrationSettingList(settings || []);
            const flat: { id: string; actionDisplay: string }[] = [];
            (settings || []).forEach((s: any) => {
              (s.AppIntergrationSettingParameterList || s.AppIntegrationSettingParameterList || []).forEach((p: any) => {
                flat.push({
                  id: String(p.Id),
                  actionDisplay: s.Name ? `${s.Name} . ${p.ActionCode}` : p.ActionCode
                });
              });
            });
            setApiOperationList(flat);
            setApiFetchNodesRetryTrigger((t) => t + 1);
          } catch (_e) {
            setIntegrationSettingList([]);
            setApiOperationList([]);
          }
        } else {
          const dsId = currentDataSet.DataSourceFrom;
          if (dsId) {
            schemaMetadataService.clearTableListCache();
            const result = await schemaMetadataService.getDataSourceTableAndViewList(dsId, null, null, { bypassHttpCache: true });
            setTablesAndViews(Array.isArray(result) ? [...result] : []);
          }
        }
      }
    } catch (error) {
      showError('Failed to refresh: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSetId, queryType, currentDataSet?.Id, currentDataSet.DataSourceFrom, dispatch, showError]);

  // Open Data Model Selector modal and load dataset list (base datasets only)
  const handleOpenDataModelSelector = useCallback(async () => {
    setShowDataModelSelector(true);
    dispatch(setIsBusy());
    try {
      const data = await searchSvc.retrieveAllAppDataSetEntityDto();
      const baseList = Array.isArray(data) ? data.filter((ds: any) => !ds.BaseDataSetId) : [];
      const cv = new CollectionView(baseList);
      setDataModelListCV(cv);
    } catch (error) {
      showError('Failed to load data models: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  // Generate query from selected data model and close modal.
  // Use CollectionView.currentItem (synced when user selects a row in grid), same as FolderNavigation/CompanySecuritySetting.
  const handleDataModelSelectorOk = useCallback(async () => {
    const cv = dataModelListCV;
    const selected = cv?.currentItem as { Id: number; Name?: string } | null | undefined;
    if (!cv || !selected || selected.Id == null) {
      showWarning('Please select a data model');
      return;
    }
    const dataSetId = selected.Id;
    setShowDataModelSelector(false);
    dispatch(setIsBusy());
    try {
      const [dto, columns] = await Promise.all([
        searchSvc.retrieveOneAppDataSetExDto(String(dataSetId), false),
        searchSvc.retrieveQueryColumnList(String(dataSetId))
      ]);
      const colList = Array.isArray(columns) ? columns : [];
      // RetrieveQueryColumnList returns LookupItemDto: Id = column name (object), Display = data type
      const columnName = (c: any) => (c?.Id != null ? String(c.Id) : c?.Display != null ? String(c.Display) : '');
      const selectCols = colList.length > 0
        ? colList.map((c: any) => {
            const name = columnName(c);
            return name ? `[${name}]` : null;
          }).filter(Boolean).join(',\n') || '*'
        : '*';
      // Full table: prefer FROM clause from QueryText ([dbo].[HvacService] or [HvacService]), else BaseTableName
      let fromClause = '';
      if (dto?.QueryText) {
        const fromMatch = /FROM\s+(\[[^\]]+\](?:\.\[[^\]]+\])?|\S+)/i.exec(dto.QueryText);
        if (fromMatch) fromClause = fromMatch[1].trim();
      }
      if (!fromClause && dto?.BaseTableName) {
        fromClause = dto.BaseTableName.includes('[') ? dto.BaseTableName : `[${dto.BaseTableName}]`;
      }
      if (!fromClause) fromClause = '[Table]';
      const generatedQuery = `SELECT\n${selectCols}\nFROM ${fromClause}`;
      handleFieldChange('QueryText', generatedQuery);
    } catch (error) {
      showError('Failed to generate query: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError, showWarning, handleFieldChange, dataModelListCV]);

  // Insert table name into query
  const handleInsertTable = (tableName: string) => {
    if (queryTextareaRef.current) {
      const textarea = queryTextareaRef.current;
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const text = currentDataSet.QueryText || '';
      const newText = text.substring(0, start) + `[${tableName}]` + text.substring(end);
      handleFieldChange('QueryText', newText);

      // Restore cursor position
      setTimeout(() => {
        textarea.focus();
        textarea.selectionStart = textarea.selectionEnd = start + tableName.length + 2;
      }, 0);
    }
  };

  // Filter stored procedures by search text
  const filteredStoredProcedures = storedProcedures.filter(sp =>
    sp.toLowerCase().includes(spSearchText.toLowerCase())
  );

  // Horizontal resize (left-right) – same pattern as DatabaseManagement
  const handleHorizontalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingHorizontal(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleHorizontalResize = useCallback((e: MouseEvent) => {
    if (!isResizingHorizontal) return;
    const container = document.querySelector('.data-set-editor-query-container');
    if (!container) return;
    const rect = container.getBoundingClientRect();
    const newWidth = e.clientX - rect.left;
    const minWidth = 200;
    const maxWidth = rect.width - 400;
    if (newWidth >= minWidth && newWidth <= maxWidth) {
      setLeftPanelWidth(newWidth);
    }
  }, [isResizingHorizontal]);

  const handleHorizontalResizeEnd = useCallback(() => {
    setIsResizingHorizontal(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Vertical resize (top-bottom in right panel) – same pattern as DatabaseManagement
  const handleVerticalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingVertical(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleVerticalResize = useCallback((e: MouseEvent) => {
    if (!isResizingVertical) return;
    const resizer = document.querySelector('.data-set-editor-vertical-resizer');
    if (!resizer?.parentElement) return;
    const rightPanel = resizer.parentElement;
    const rect = rightPanel.getBoundingClientRect();
    const heightFromBottom = rect.bottom - e.clientY;
    const minHeight = 150;
    const maxHeight = rect.height - 200;
    if (heightFromBottom >= minHeight && heightFromBottom <= maxHeight) {
      setBottomPanelHeight(heightFromBottom);
    }
  }, [isResizingVertical]);

  const handleVerticalResizeEnd = useCallback(() => {
    setIsResizingVertical(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  useEffect(() => {
    if (isResizingHorizontal) {
      document.addEventListener('mousemove', handleHorizontalResize);
      document.addEventListener('mouseup', handleHorizontalResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleHorizontalResize);
        document.removeEventListener('mouseup', handleHorizontalResizeEnd);
      };
    }
  }, [isResizingHorizontal, handleHorizontalResize, handleHorizontalResizeEnd]);

  useEffect(() => {
    if (isResizingVertical) {
      document.addEventListener('mousemove', handleVerticalResize);
      document.addEventListener('mouseup', handleVerticalResizeEnd);
      return () => {
        document.removeEventListener('mousemove', handleVerticalResize);
        document.removeEventListener('mouseup', handleVerticalResizeEnd);
      };
    }
  }, [isResizingVertical, handleVerticalResize, handleVerticalResizeEnd]);

  // Render SQL Query editor
  const renderQueryTextEditor = () => (
    <div className="flex h-full data-set-editor-query-container">
      {/* Left panel - Tables/Views */}
      <div className={`flex flex-col border-r flex-shrink-0 ${t('border_default')}`} style={{ width: leftPanelWidth }}>
        <div className={`px-2 py-2 border-b font-semibold text-sm flex items-center justify-between ${t('border_default')} ${theme.title}`}>
          <span>Tables & Views</span>
          {selectedTablesForAI.size > 0 && (
            <button
              onClick={() => setSelectedTablesForAI(new Set())}
              className={`text-[9px] px-1.5 py-0.5 rounded ${theme.label} opacity-60 hover:opacity-100`}
              title="Clear AI selection"
            >
              <i className="fa-solid fa-xmark mr-0.5" />
              {selectedTablesForAI.size}
            </button>
          )}
        </div>
        <div className="h-1 flex-auto overflow-y-auto">
          {tablesAndViews.map((item, index) => {
            const name = item.TableName || item.Name;
            const isSelected = selectedTablesForAI.has(name);
            return (
              <div
                key={index}
                className={`px-2 py-1 text-xs flex items-center gap-1 group ${isSelected ? 'bg-purple-100 dark:bg-purple-900/30' : theme.contextMenu}`}
              >
                {/* AI selection checkbox */}
                <button
                  onClick={() => {
                    setSelectedTablesForAI(prev => {
                      const next = new Set(prev);
                      if (next.has(name)) next.delete(name);
                      else next.add(name);
                      return next;
                    });
                  }}
                  className={`shrink-0 w-4 h-4 flex items-center justify-center rounded border text-[9px] transition-colors ${
                    isSelected
                      ? 'bg-purple-600 border-purple-600 text-white'
                      : `border-gray-300 dark:border-gray-600 ${theme.label} opacity-0 group-hover:opacity-60`
                  }`}
                  title={isSelected ? 'Remove from AI selection' : 'Add to AI selection'}
                >
                  {isSelected && <i className="fa-solid fa-check" />}
                </button>
                {/* Table name — click inserts into query */}
                <div
                  className="flex items-center gap-1 w-1 flex-auto cursor-pointer min-w-0"
                  onClick={() => handleInsertTable(name)}
                  title={`Click to insert [${name}]`}
                >
                  <i className={`fa-solid ${item.ObjectType === 'View' ? 'fa-eye' : 'fa-table'} ${theme.label} shrink-0`}></i>
                  <span className="truncate">{name}</span>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Horizontal resizer */}
      <div
        className={`w-1 flex-shrink-0 cursor-col-resize ${t('border_default')} ${isResizingHorizontal ? theme.tab_active : t('bg_default')}`}
        onMouseDown={handleHorizontalResizeStart}
        title="Drag to resize"
      />

      {/* Right panel - Query editor and results */}
      <div
        className="flex flex-col overflow-hidden flex-shrink-0"
        style={{ width: `calc(100% - ${leftPanelWidth}px - 4px)` }}
      >
        {/* Query editor (top) */}
        <div className="flex flex-col overflow-hidden flex-auto h-1 min-h-[150px]">
          <div className="flex flex-col h-full p-2">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-semibold">SQL Query</span>
            </div>
            <textarea
              ref={queryTextareaRef}
              value={currentDataSet.QueryText || ''}
              onChange={(e) => handleFieldChange('QueryText', e.target.value)}
              className={`w-full h-1 flex-auto p-2 font-mono text-sm border rounded resize-none ${theme.inputBox}`}
              placeholder="Enter SQL query here... Click on tables to insert them."
              spellCheck={false}
            />
          </div>
        </div>

        {/* Vertical resizer */}
        <div
          className={`data-set-editor-vertical-resizer h-1 flex-shrink-0 cursor-row-resize ${t('border_default')} ${isResizingVertical ? theme.tab_active : t('bg_default')}`}
          onMouseDown={handleVerticalResizeStart}
          title="Drag to resize"
        />

        {/* Query results (bottom) */}
        <div
          className={`flex flex-col overflow-hidden flex-shrink-0 border-t ${t('border_default')}`}
          style={{ height: `${bottomPanelHeight}px`, minHeight: 150 }}
        >
          <div className={`px-2 py-1 border-b text-sm font-semibold flex items-center justify-between ${t('border_default')} ${theme.title}`}>
            <span>Query Results {queryResults.length > 0 && `(${queryResults.length} rows)`}</span>
          </div>
          <div className="h-1 flex-auto overflow-hidden min-h-0">
            {queryResultsCV ? (
              <FlexGrid
                itemsSource={queryResultsCV}
                autoGenerateColumns={true}
                isReadOnly={true}
                headersVisibility="Column"
                className="w-full h-full"
              />
            ) : (
              <div className={`flex items-center justify-center h-full text-sm ${theme.label}`}>
                Execute a query to see results
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );

  // Render Stored Procedure editor (Stored Procedure field is in properties section second row)
  const renderStoredProcedureEditor = () => (
    <div className="flex flex-col h-full p-3 gap-3">
      {/* Parameters grid */}
      <div className="h-1 flex-auto flex flex-col">
        <div className="text-sm font-semibold mb-2">Parameters</div>
        <div className="h-1 flex-auto border rounded overflow-hidden">
          <FlexGrid
            itemsSource={parametersCV || undefined}
            autoGenerateColumns={false}
            isReadOnly={true}
            headersVisibility="Column"
            className="w-full h-full"
          >
            <FlexGridColumn header="Parameter Name" binding="ParameterName" width={150} />
            <FlexGridColumn header="Data Type" binding="DbDataType" width={120} />
            <FlexGridColumn header="Default Value" binding="DefaultValue" width={120} />
            <FlexGridColumn header="Description" binding="Description" width="*" />
          </FlexGrid>
        </div>
      </div>

      {/* SP Selector Modal */}
      {showSpSelector && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className={`rounded-lg shadow-lg w-[500px] max-h-[80vh] flex flex-col border ${t('border_default')} ${theme.mainContentSection}`}>
            <div className={`px-4 py-3 border-b flex items-center justify-between ${t('border_default')} ${theme.mainHeader}`}>
              <span className={`font-semibold ${theme.title}`}>Select Stored Procedure</span>
              <button onClick={() => setShowSpSelector(false)} className={`${theme.label}`}>
                <i className="fa-solid fa-times"></i>
              </button>
            </div>
            <div className={`p-3 border-b ${t('border_default')}`}>
              <input
                type="text"
                value={spSearchText}
                onChange={(e) => setSpSearchText(e.target.value)}
                placeholder="Search stored procedures..."
                autoComplete="off"
                className={`w-full h-8 px-2 text-sm border ${theme.inputBox} focus:outline-none`}
              />
            </div>
            <div className="h-1 flex-auto overflow-y-auto p-2" style={{ maxHeight: '400px' }}>
              {filteredStoredProcedures.map((sp, index) => (
                <div
                  key={index}
                  onClick={() => handleSelectStoredProcedure(sp)}
                  className={`px-3 py-2 cursor-pointer text-sm rounded ${theme.contextMenu}`}
                >
                  <i className={`fa-solid fa-code mr-2 ${theme.label}`}></i>
                  {sp}
                </div>
              ))}
              {filteredStoredProcedures.length === 0 && (
                <div className={`text-center py-4 ${theme.label}`}>No stored procedures found</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );

  // Render Internal Web Api Call (Plugin) editor – Web Api Last Point Address is in properties row above
  const renderInternalWebApiEditor = () => (
    <div className="flex flex-col h-full p-3 gap-3">
      <div className="h-1 flex-auto flex flex-col min-h-0">
        <div className={`flex items-center justify-between mb-2 ${theme.mainContentSection}`}>
          <span className="text-sm font-semibold">Web Service Parameters</span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => {
                setInternalWebApiParameterList((prev) => [...prev, { ParameterName: '' }]);
                setIsModified(true);
              }}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            >
              Add
            </button>
            <button
              type="button"
              onClick={() => {
                const grid = internalWebApiGridRef.current;
                if (!grid) return;
                const row = grid.selection?.row;
                if (row === undefined || row < 0) return;
                const item = internalWebApiParametersCV.items[row];
                if (item) {
                  setInternalWebApiParameterList((prev) => prev.filter((p) => p !== item));
                  setIsModified(true);
                }
              }}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            >
              Remove
            </button>
          </div>
        </div>
        <div className="h-1 flex-auto border rounded overflow-hidden min-h-0">
          <FlexGrid
            ref={internalWebApiGridRef}
            itemsSource={internalWebApiParametersCV}
            autoGenerateColumns={false}
            isReadOnly={false}
            headersVisibility="Column"
            selectionMode="Row"
            className="w-full h-full"
            cellEditEnded={() => {
              setInternalWebApiParameterList((prev) => [...prev]);
              setIsModified(true);
            }}
          >
            <FlexGridColumn binding="ParameterName" header="Parameter Name" width={200} />
            <FlexGridColumn binding="" header="" width="*" isReadOnly={true} />
          </FlexGrid>
        </div>
      </div>
    </div>
  );

  // Render Integration Web Api Call editor – Json Sample Data (Api Operation, Fetch Path, Transform Type are in properties section)
  const renderIntegrationWebApiEditor = () => (
    <div className="flex flex-col h-full p-3 gap-3">
      <div className="h-1 flex-auto flex flex-col min-h-0">
        <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Json Sample Data</div>
        <textarea
          value={currentDataSet.OtherSettings || ''}
          onChange={(e) => handleFieldChange('OtherSettings', e.target.value)}
          className={`w-full h-1 flex-auto min-h-[120px] p-2 font-mono text-xs border rounded resize-none ${theme.inputBox}`}
          placeholder='Paste JSON sample response here'
          spellCheck={false}
        />
      </div>
    </div>
  );

  // Render editor content based on query type
  const renderEditorContent = () => {
    switch (queryType) {
      case EmAppDataServiceType.QueryText:
        return renderQueryTextEditor();
      case EmAppDataServiceType.StoredProcedure:
        return renderStoredProcedureEditor();
      case EmAppDataServiceType.PluginWebApiCall:
        return renderInternalWebApiEditor();
      case EmAppDataServiceType.IntegrationWebApiCall:
        return renderIntegrationWebApiEditor();
      default:
        return <div className={`p-4 ${theme.label}`}>Unknown query type</div>;
    }
  };

  // Route-only: URL `param` must parse to a dataset id (or valid new-dataset payload). When embedded with
  // `ignoreRouteParam`, `parsed` is always null even though `param` may be the parent page's tab JSON (e.g. search editor).
  if (!ignoreRouteParam && param !== undefined && parsed === null) {
    return (
      <div className="p-4">
        Invalid or missing parameters. Close this tab to return.
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header HIDE/SHOW: Save=always; Refresh=all types(disabled: Query/SP=no DataSource, Plugin/Integration=no dataSetId);
          Generate Query From Data Model=QueryText only; Query Execute=QueryText only */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {getEditorTitle()}
        </div>
        <div className="flex items-center space-x-2">
          {onConfirmAndClose && (
            <button
              onClick={() => void handleConfirmAndClose()}
              disabled={isLoading || isConfirmAndCloseBusy}
              className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            >
              <i className="fa-solid fa-check"></i>
              <span>Save &amp; Close</span>
            </button>
          )}
          <button
            onClick={handleSave}
            disabled={isLoading || isConfirmAndCloseBusy}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 flex items-center gap-1`}
          >
            <i className="fa-solid fa-save"></i>
            <span>Save</span>
          </button>
          {/* Refresh: always enabled */}
          <button
            onClick={handleRefresh}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} flex items-center gap-1`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate"></i>
            <span>Refresh</span>
          </button>
          {queryType === EmAppDataServiceType.QueryText && (
            <button
              onClick={handleOpenDataModelSelector}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} flex items-center gap-1`}
              title="Generate Query From Data Model"
            >
              <i className="fa-solid fa-database"></i>
              <span>Generate Query From Data Model</span>
            </button>
          )}
          {queryType === EmAppDataServiceType.QueryText && (
            <button
              onClick={handleExecuteQuery}
              disabled={!currentDataSet.QueryText?.trim()}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 flex items-center gap-1`}
              title="Query Execute"
            >
              <i className="fa-solid fa-bolt"></i>
              <span>Query Execute</span>
            </button>
          )}
          {queryType === EmAppDataServiceType.QueryText && (
            <button
              onClick={() => setShowQueryAgent(true)}
              className="px-3 py-1.5 text-sm rounded-[4px] bg-purple-600 hover:bg-purple-700 text-white flex items-center gap-1"
              title="DBA-Genie — AI-powered SQL generation"
            >
              <i className="fa-solid fa-database"></i>
              <span>DBA-Genie</span>
              {selectedTablesForAI.size > 0 && (
                <span className="ml-0.5 bg-white/20 text-white text-[10px] px-1.5 py-0.5 rounded-full">
                  {selectedTablesForAI.size}
                </span>
              )}
            </button>
          )}
          {onCloseProp && (
            <button
              type="button"
              onClick={handleClose}
              className="px-2 py-1 text-lg leading-none"
              aria-label="Close"
              title="Close"
            >
              &times;
            </button>
          )}
        </div>
      </div>

      {/* Properties section – uniform label width PROP_LABEL_WIDTH, input width PROP_INPUT_WIDTH for all types. */}
      <div className={`px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className="flex flex-nowrap items-center gap-x-4 gap-y-2 overflow-x-auto min-w-0">
          <div className="flex items-center shrink-0">
            <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Name:</label>
            <input
              type="text"
              value={currentDataSet.Name || ''}
              onChange={(e) => handleFieldChange('Name', e.target.value)}
              autoComplete="off"
              className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
              style={{ width: PROP_INPUT_WIDTH }}
              placeholder="Dataset name"
            />
          </div>
          <div className="flex items-center shrink-0">
            <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Data Source:</label>
            <select
              value={currentDataSet.DataSourceFrom || ''}
              onChange={(e) => handleDataSourceChange(parseInt(e.target.value))}
              className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
              style={{ width: PROP_INPUT_WIDTH }}
            >
              <option value="">Select Data Source</option>
              {dataSourceRegisterList.map((ds) => (
                <option key={ds.Id} value={ds.Id}>{ds.DataSourceName}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center shrink-0">
            <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Description:</label>
            <input
              type="text"
              value={currentDataSet.Description || ''}
              onChange={(e) => handleFieldChange('Description', e.target.value)}
              className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
              style={{ width: PROP_INPUT_WIDTH }}
              placeholder="Dataset description"
              autoComplete="off"
            />
          </div>
          {queryType === EmAppDataServiceType.PluginWebApiCall && (
            <div className="flex items-center shrink-0">
              <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Web Api Last Point Address:</label>
              <input
                type="text"
                value={currentDataSet.QueryText || ''}
                onChange={(e) => handleFieldChange('QueryText', e.target.value)}
                autoComplete="off"
                className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
                style={{ width: PROP_INPUT_WIDTH }}
                placeholder="API endpoint or path"
              />
            </div>
          )}
          {queryType === EmAppDataServiceType.StoredProcedure && (
            <div className="flex items-center shrink-0">
              <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Stored Procedure:</label>
              <input
                type="text"
                readOnly
                value={currentDataSet.QueryText || ''}
                className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
                style={{ width: PROP_INPUT_WIDTH }}
                placeholder="Click Browse to select..."
              />
              <button
                type="button"
                onClick={() => setShowSpSelector(true)}
                className={`ml-2 px-3 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_default}`}
              >
                <i className="fa-solid fa-search mr-1"></i>
                Browse
              </button>
            </div>
          )}
        </div>
        {/* HIDE/SHOW: Integration row (Api Operation, Fetch Path, Transform Type)=IntegrationWebApiCall only; Plugin row=PluginWebApiCall only; Stored Proc row=StoredProcedure only */}
        {/* Integration Web Api: second row – same label/input widths for vertical alignment */}
        {queryType === EmAppDataServiceType.IntegrationWebApiCall && (
          <div className="flex flex-nowrap items-center gap-x-4 gap-y-2 overflow-x-auto min-w-0 mt-2">
            <div className="flex items-center shrink-0" ref={apiOperationDropdownRef}>
              <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Api Operation:</label>
              <div ref={apiOperationTriggerRef} className="relative shrink-0" style={{ width: PROP_INPUT_WIDTH }}>
                <div className={`flex items-center h-7 border rounded-[4px] overflow-hidden ${theme.inputBox}`}>
                  <div
                    className="flex-1 min-w-0 h-full px-2 text-xs flex items-center truncate"
                    title={apiOperationList.find((o) => o.id === currentDataSet.QueryText)?.actionDisplay || ''}
                  >
                    {apiOperationList.find((o) => o.id === currentDataSet.QueryText)?.actionDisplay || 'Select...'}
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      if (!showApiOperationDropdown) {
                        const rect = apiOperationTriggerRef.current?.getBoundingClientRect();
                        if (rect) setApiOpDropdownPosition({ top: rect.bottom + 4, left: rect.left });
                      } else {
                        setApiOpDropdownPosition(null);
                        setApiOpSubmenuPosition(null);
                      }
                      setShowApiOperationDropdown((v) => !v);
                    }}
                    className={`h-full w-7 flex items-center justify-center shrink-0 border-l ${theme.inputBox}`}
                  >
                    <i className="fa-solid fa-chevron-down text-xs"></i>
                  </button>
                  <button
                    ref={createInTriggerRef}
                    type="button"
                    title="Create in integration"
                    className={`h-full w-7 flex items-center justify-center shrink-0 border-l ${theme.inputBox}`}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (!showCreateInDropdown) {
                        const rect = createInTriggerRef.current?.getBoundingClientRect();
                        if (rect) setCreateInDropdownPosition({ top: rect.bottom + 4, left: rect.left });
                      } else {
                        setCreateInDropdownPosition(null);
                      }
                      setShowCreateInDropdown((v) => !v);
                    }}
                  >
                    <i className="fa-solid fa-plus text-xs"></i>
                  </button>
                </div>
                {showCreateInDropdown && createInDropdownPosition && createPortal(
                  <div
                    ref={createInDropdownRef}
                    className={`fixed z-[9999] min-w-[200px] max-h-96 overflow-y-auto border rounded-[4px] shadow-lg py-1 ${theme.mainContentSection}`}
                    style={{ top: createInDropdownPosition.top, left: createInDropdownPosition.left }}
                    onClick={(e) => e.stopPropagation()}
                  >
                    {integrationSettingList.map((s: any) => (
                      <button
                        key={s.Id}
                        type="button"
                        className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu} hover:opacity-90`}
                        onClick={() => {
                          addTabAndNavigate('third-party-api-editor', s.Name || `Integration ${s.Id}`, { id: s.Id }, true);
                          setShowCreateInDropdown(false);
                          setCreateInDropdownPosition(null);
                        }}
                      >
                        Create In {s.Name || 'Unnamed'}
                      </button>
                    ))}
                    {integrationSettingList.length === 0 && (
                      <div className="px-3 py-2 text-sm opacity-75">No integrations</div>
                    )}
                  </div>,
                  document.body
                )}
                {showApiOperationDropdown && apiOpDropdownPosition && createPortal(
                  <div
                    className={`fixed z-[9999] flex flex-row items-start overflow-visible ${theme.mainContentSection}`}
                    style={{ top: apiOpDropdownPosition.top, left: apiOpDropdownPosition.left }}
                    onMouseLeave={() => {
                      apiOpSubmenuCloseTimerRef.current = setTimeout(() => {
                        setHoveredIntegrationId(null);
                        setApiOpSubmenuPosition(null);
                        apiOpSubmenuCloseTimerRef.current = null;
                      }, API_OP_SUBMENU_CLOSE_DELAY_MS);
                    }}
                  >
                    {/* First level: categories */}
                    <div className={`min-w-[200px] max-h-96 overflow-y-auto border rounded-l shadow-lg py-1 ${theme.mainContentSection}`}>
                      {integrationSettingList.map((s: any) => {
                        const sid = s.Id;
                        const isHovered = hoveredIntegrationId === sid;
                        return (
                          <div
                            key={sid}
                            className={`w-full px-3 py-2 text-left text-sm flex items-center justify-between ${theme.contextMenu} ${isHovered ? (theme.tab_active ?? 'bg-gray-100') : ''}`}
                            onMouseEnter={() => {
                              if (apiOpSubmenuCloseTimerRef.current) {
                                clearTimeout(apiOpSubmenuCloseTimerRef.current);
                                apiOpSubmenuCloseTimerRef.current = null;
                              }
                              setHoveredIntegrationId(sid);
                            }}
                          >
                            <span className="truncate">{s.Name || 'Unnamed'}</span>
                            <i className="fa-solid fa-chevron-right text-xs ml-1 shrink-0" />
                          </div>
                        );
                      })}
                      {integrationSettingList.length === 0 && (
                        <div className="px-3 py-2 text-sm opacity-75">No integrations</div>
                      )}
                    </div>
                    {/* Second level: API operations – same wrapper so mouse never "leaves" between layers */}
                    {hoveredIntegrationId != null && (() => {
                      const setting = integrationSettingList.find((s: any) => s.Id === hoveredIntegrationId);
                      const ops = setting?.AppIntergrationSettingParameterList || setting?.AppIntegrationSettingParameterList || [];
                      return (
                        <div className={`min-w-[240px] max-w-[320px] max-h-96 overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}>
                          {ops.map((p: any) => (
                            <button
                              key={p.Id}
                              type="button"
                              onClick={() => {
                                handleFieldChange('QueryText', String(p.Id));
                                handleFieldChange('BaseTableName', ''); // clear path when switching API operation
                                setShowApiOperationDropdown(false);
                                setApiOpDropdownPosition(null);
                                setHoveredIntegrationId(null);
                                setApiOpSubmenuPosition(null);
                              }}
                              className={`w-full px-3 py-2 text-left text-sm truncate ${theme.contextMenu} hover:opacity-90`}
                            >
                              {p.ActionCode}
                            </button>
                          ))}
                          {ops.length === 0 && (
                            <div className="px-3 py-2 text-sm opacity-75">No operations</div>
                          )}
                        </div>
                      );
                    })()}
                  </div>,
                  document.body
                )}
              </div>
            </div>
            <div className="flex items-center shrink-0" ref={fetchNodeDropdownRef}>
              <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Fetch Data Parent Node Path:</label>
              <div ref={fetchNodeTriggerRef} className="relative shrink-0" style={{ width: PROP_INPUT_WIDTH }}>
                <div className={`flex items-center h-7 border rounded-[4px] overflow-hidden ${theme.inputBox}`}>
                  <div
                    className="flex-1 min-w-0 h-full px-2 text-xs flex items-center truncate"
                    title={currentDataSet.BaseTableName || 'Select...'}
                  >
                    {currentDataSet.BaseTableName || 'Select...'}
                  </div>
                  <button
                    type="button"
                    onClick={() => {
                      if (!showFetchNodeDropdown) {
                        const rect = fetchNodeTriggerRef.current?.getBoundingClientRect();
                        if (rect) setFetchNodeDropdownPosition({ top: rect.bottom + 4, left: rect.left });
                      } else {
                        setFetchNodeDropdownPosition(null);
                      }
                      setShowFetchNodeDropdown((v) => !v);
                    }}
                    className={`h-full w-7 flex items-center justify-center shrink-0 border-l ${theme.inputBox}`}
                  >
                    <i className="fa-solid fa-chevron-down text-xs"></i>
                  </button>
                  <button
                    type="button"
                    title="Clear"
                    onClick={() => handleFieldChange('BaseTableName', '')}
                    className={`h-full w-7 flex items-center justify-center shrink-0 border-l ${theme.inputBox}`}
                  >
                    <i className="fa-solid fa-trash text-xs"></i>
                  </button>
                </div>
                {showFetchNodeDropdown && fetchNodeDropdownPosition && createPortal(
                  <div
                    className={`fixed z-[9999] max-h-60 overflow-y-auto border rounded shadow-lg py-1 ${theme.mainContentSection}`}
                    style={{ top: fetchNodeDropdownPosition.top, left: fetchNodeDropdownPosition.left, minWidth: 280 }}
                  >
                    {apiFetchNodeTree.length === 0 ? (
                      <div className="px-3 py-2 text-sm">
                        {apiFetchNodesLoading ? (
                          <span className="opacity-75">Loading…</span>
                        ) : apiFetchNodesError ? (
                          <div className="flex flex-col gap-1">
                            <span className="opacity-90">Could not load node structure (server error).</span>
                            <button
                              type="button"
                              className={`text-left px-0 py-0.5 text-xs underline ${theme.contextMenu} hover:opacity-90`}
                              onClick={() => setApiFetchNodesRetryTrigger((t) => t + 1)}
                            >
                              Retry
                            </button>
                          </div>
                        ) : currentDataSet.QueryText ? (
                          <span className="opacity-75">No node structure available</span>
                        ) : (
                          <span className="opacity-75">Select Api Operation first</span>
                        )}
                      </div>
                    ) : (
                      (function renderFetchNodeTree(nodes: any[], depth: number = 0): React.ReactNode {
                        return nodes.map((node: any, i: number) => {
                          const hasChildren = node.Children?.length > 0;
                          const label = (node.Display ?? '{}') + (hasChildren ? ' {}' : '');
                          const path = node.AbsolutePath ?? node.Display ?? '';
                          return (
                            <div key={`${depth}-${i}-${path || i}`}>
                              <button
                                type="button"
                                onClick={() => {
                                  handleFieldChange('BaseTableName', path);
                                  setShowFetchNodeDropdown(false);
                                  setFetchNodeDropdownPosition(null);
                                }}
                                className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu} hover:opacity-90`}
                                style={{ paddingLeft: 12 + depth * 12 }}
                              >
                                {label}
                              </button>
                              {hasChildren && renderFetchNodeTree(node.Children, depth + 1)}
                            </div>
                          );
                        });
                      })(apiFetchNodeTree)
                    )}
                  </div>,
                  document.body
                )}
              </div>
            </div>
            <div className="flex items-center shrink-0">
              <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`} style={{ width: PROP_LABEL_WIDTH }}>Transform Type:</label>
              <select
                value={currentDataSet.UsageTypeId ?? EmAppDataSetUsageType.Default}
                onChange={(e) => handleFieldChange('UsageTypeId', parseInt(e.target.value))}
                className={`h-7 px-2 text-xs border shrink-0 ${theme.inputBox} focus:outline-none`}
                style={{ width: PROP_INPUT_WIDTH }}
              >
                {usageTypeOptions.map((opt) => (
                  <option key={opt.id} value={opt.id}>{opt.display}</option>
                ))}
              </select>
            </div>
          </div>
        )}
      </div>

      {/* Main editor content */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {renderEditorContent()}
      </div>

      {/* Data Model Selector modal (Generate Query From Data Model) */}
      {/* Query AI Agent modal */}
      {showQueryAgent && (
        <QueryAgentPanel
          dataSourceId={currentDataSet.DataSourceFrom || 0}
          selectedTables={Array.from(selectedTablesForAI)}
          onQueryGenerated={(sql) => handleFieldChange('QueryText', sql)}
          onClose={() => setShowQueryAgent(false)}
        />
      )}

      {/* Data Model Selector modal (Generate Query From Data Model) */}
      {showDataModelSelector && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50" onClick={(e) => e.stopPropagation()}>
          <div
            className={`rounded-lg shadow-xl border flex flex-col overflow-hidden ${theme.mainContentSection}`}
            style={{ width: '90vw', maxWidth: '560px', maxHeight: '80vh' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`px-4 py-2 border-b flex items-center justify-between ${t('border_default')} ${theme.mainHeader}`}>
              <span className={`font-semibold ${theme.title}`}>Data Model Selector</span>
              <button onClick={() => setShowDataModelSelector(false)} className={`${theme.label} hover:opacity-80`} aria-label="Close">
                <i className="fa-solid fa-times"></i>
              </button>
            </div>
            <div className="flex-auto min-h-0 p-2" style={{ height: '400px' }}>
              {dataModelListCV ? (
                <FlexGrid
                  ref={dataModelSelectorGridRef}
                  initialized={(grid: wjGrid.FlexGrid) => { dataModelSelectorGridRef.current = grid; }}
                  itemsSource={dataModelListCV}
                  selectionMode="Row"
                  headersVisibility="Column"
                  isReadOnly={true}
                  className="w-full h-full"
                >
                  <FlexGridFilter />
                  <FlexGridColumn header="ID" binding="Id" width={100} />
                  <FlexGridColumn header="Name" binding="Name" width="*" />
                </FlexGrid>
              ) : (
                <div className={`flex items-center justify-center h-full text-sm ${theme.label}`}>Loading...</div>
              )}
            </div>
            <div className={`px-4 py-2 border-t flex justify-end gap-2 ${t('border_default')} ${theme.mainContentSection}`}>
              <button onClick={() => setShowDataModelSelector(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button onClick={handleDataModelSelectorOk} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Ok
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DataSetEditor;
