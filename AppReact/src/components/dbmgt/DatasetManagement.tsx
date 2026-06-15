import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';

// Query Type enum matching AngularJS EmAppDataServiceType
const EmAppDataServiceType = {
  QueryText: 1,
  StoredProcedure: 2,
  PluginWebApiCall: 3,
  IntegrationWebApiCall: 4
};

// Query type display names (match Angular: Query Service, Stored Procedure Service, etc.)
const queryTypeDisplayNames: Record<number, string> = {
  [EmAppDataServiceType.QueryText]: 'Query Service',
  [EmAppDataServiceType.StoredProcedure]: 'Stored Procedure Service',
  [EmAppDataServiceType.PluginWebApiCall]: 'Internal Web Api Call Service',
  [EmAppDataServiceType.IntegrationWebApiCall]: 'Integration Web Api Call Service'
};

interface DataSetItem {
  Id: number;
  Name: string;
  Description?: string;
  QueryType: number;
  QueryText?: string;
  DataSourceFrom?: number;
  BaseDataSetId?: number;
  SaasApplicationId?: number;
  AppModifiedDate?: string;
  AppCreatedDate?: string;
}

interface DataSourceRegisterItem {
  Id: number;
  DataSourceName: string;
}

const DatasetManagement: React.FC = () => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();

  // State
  const [dataSets, setDataSets] = useState<DataSetItem[]>([]);
  const [dataSetsCV, setDataSetsCV] = useState<CollectionView | null>(null);
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<DataSourceRegisterItem[]>([]);
  const [selectedDataSet, setSelectedDataSet] = useState<DataSetItem | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Context menu state
  const [contextMenuVisible, setContextMenuVisible] = useState(false);
  const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });

  // Create dropdown state
  const [createDropdownVisible, setCreateDropdownVisible] = useState(false);
  const [filterDropdownVisible, setFilterDropdownVisible] = useState(false);
  const [selectedDataSourceId, setSelectedDataSourceId] = useState<number | null>(null);
  const [filterQueryType, setFilterQueryType] = useState<number | null>(null); // null = all


  // Refs
  const flexGridRef = useRef<wjGrid.FlexGrid | null>(null);
  const dataSourceDataMapRef = useRef<DataMap | null>(null);
  const queryTypeDataMapRef = useRef<DataMap | null>(null);
  const createDropdownRef = useRef<HTMLDivElement>(null);
  const filterDropdownRef = useRef<HTMLDivElement>(null);

  // Query type options for DataMap
  const queryTypeOptions = Object.entries(queryTypeDisplayNames).map(([id, display]) => ({
    Id: parseInt(id),
    Display: display
  }));

  // Initialize DataMaps
  useEffect(() => {
    queryTypeDataMapRef.current = new DataMap(queryTypeOptions, 'Id', 'Display');
  }, []);

  useEffect(() => {
    if (dataSourceRegisterList.length > 0) {
      dataSourceDataMapRef.current = new DataMap(dataSourceRegisterList, 'Id', 'DataSourceName');
    }
  }, [dataSourceRegisterList]);

  // Load data
  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      // Load datasets
      const data = await searchSvc.retrieveAllAppDataSetEntityDto();

      // Filter to only base datasets (no BaseDataSetId)
      const baseDataSetList = Array.isArray(data)
        ? data.filter((ds: DataSetItem) => !ds.BaseDataSetId)
        : [];

      setDataSets(baseDataSetList);

      // Create CollectionView with sorting
      const cv = new CollectionView(baseDataSetList);
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      setDataSetsCV(cv);

      // Load data source register list
      const dsRegisterList = await adminSvc.getDataSourceRegisterList(false);
      setDataSourceRegisterList(dsRegisterList || []);

      // Set default data source if available
      if (dsRegisterList && dsRegisterList.length > 0 && !selectedDataSourceId) {
        setSelectedDataSourceId(dsRegisterList[0].Id);
      }
    } catch (error) {
      showError('Failed to load datasets: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError, selectedDataSourceId]);

  useEffect(() => {
    loadData();
  }, []);

  // Apply filter to grid
  useEffect(() => {
    if (!dataSetsCV) return;
    dataSetsCV.filter = (item: DataSetItem) =>
      filterQueryType == null || (item as DataSetItem).QueryType === filterQueryType;
  }, [dataSetsCV, filterQueryType]);

  // Refresh handler
  const handleRefresh = () => {
    loadData();
  };

  // Handle grid selection change
  const handleSelectionChanged = (grid: wjGrid.FlexGrid) => {
    const sel = grid.selection;
    if (sel.row >= 0 && grid.rows[sel.row]) {
      setSelectedDataSet(grid.rows[sel.row].dataItem);
    } else {
      setSelectedDataSet(null);
    }
  };

  // Open editor in new tab (create new dataset)
  const openEditorForCreate = (queryType: number) => {
    if (!selectedDataSourceId) {
      showWarning('Please select a data source first');
      return;
    }
    const label = `New ${queryTypeDisplayNames[queryType]}`;
    addTabAndNavigate('dataset-editor', label, {
      queryType,
      dataSourceRegisterId: selectedDataSourceId
    }, true);
    setCreateDropdownVisible(false);
  };

  // Edit dataset – open in new tab
  const handleEditDataSet = (dataSet?: DataSetItem) => {
    const targetDataSet = dataSet || selectedDataSet;
    if (!targetDataSet) {
      showWarning('Please select a dataset to edit');
      return;
    }
    const label = targetDataSet.Name || `Dataset ${targetDataSet.Id}`;
    addTabAndNavigate('dataset-editor', label, {
      id: targetDataSet.Id,
      queryType: targetDataSet.QueryType,
      dataSourceRegisterId: targetDataSet.DataSourceFrom ?? selectedDataSourceId ?? undefined
    }, true);
    closeContextMenu();
  };

  // Double-click row – open editor in new tab
  const handleRowDoubleClick = (grid: wjGrid.FlexGrid) => {
    const row = grid.selection?.row ?? -1;
    if (row >= 0 && grid.rows[row]) {
      const item = grid.rows[row].dataItem as DataSetItem;
      if (item) handleEditDataSet(item);
    }
  };

  // Delete dataset
  const handleDeleteDataSet = async (dataSet?: DataSetItem) => {
    const targetDataSet = dataSet || selectedDataSet;
    if (!targetDataSet || !targetDataSet.Id) {
      showWarning('Please select a dataset to delete');
      return;
    }

    const confirmed = window.confirm(`Confirm to delete: ${targetDataSet.Name} (${targetDataSet.Id})`);
    if (!confirmed) return;

    dispatch(setIsBusy());
    try {
      const result = await searchSvc.deleteOneAppDataSetEntityDto(targetDataSet.Id.toString());

      if (result?.ValidationResult?.IsValid !== false) {
        showInfo('Dataset deleted successfully');
        await loadData();
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to delete dataset';
        showError(errorMsg);
      }
    } catch (error) {
      showError('Failed to delete dataset: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
      closeContextMenu();
    }
  };

  // Context menu handlers
  const handleContextMenu = (e: React.MouseEvent, dataItem: DataSetItem) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedDataSet(dataItem);
    setContextMenuPosition({ x: e.clientX, y: e.clientY });
    setContextMenuVisible(true);
  };

  const closeContextMenu = () => {
    setContextMenuVisible(false);
  };

  // Close context menu and dropdowns when clicking outside
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      if (
        createDropdownRef.current?.contains(target) ||
        filterDropdownRef.current?.contains(target)
      ) return;
      setContextMenuVisible(false);
      setCreateDropdownVisible(false);
      setFilterDropdownVisible(false);
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  // Get query type display name
  const getQueryTypeDisplay = (queryType: number): string => {
    return queryTypeDisplayNames[queryType] || 'Unknown';
  };

  // Get query type icon
  const getQueryTypeIcon = (queryType: number): string => {
    switch (queryType) {
      case EmAppDataServiceType.QueryText:
        return 'fa-solid fa-code';
      case EmAppDataServiceType.StoredProcedure:
        return 'fa-solid fa-database';
      case EmAppDataServiceType.PluginWebApiCall:
        return 'fa-solid fa-plug';
      case EmAppDataServiceType.IntegrationWebApiCall:
        return 'fa-solid fa-cloud';
      default:
        return 'fa-solid fa-question';
    }
  };

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Top Menu – style as image: title left, Refresh / Create / Filter right */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Dataset List:</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:opacity-60 ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh</span>
          </button>

          {/* Create Dropdown */}
          <div className="relative" ref={createDropdownRef}>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setCreateDropdownVisible(!createDropdownVisible);
                setFilterDropdownVisible(false);
              }}
              disabled={isLoading}
              className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
              title="Create new dataset"
            >
              <i className="fa-solid fa-plus" aria-hidden />
              <span>Create</span>
              <i className="fa-solid fa-caret-down text-xs" aria-hidden />
            </button>

            {createDropdownVisible && (
              <div
                className={`absolute right-0 mt-1 w-64 rounded shadow-lg z-50 border ${t('border_default')} ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                {/* Data Source Selection */}
                <div className={`px-3 py-2 border-b ${t('border_default')}`}>
                  <label className={`text-xs block mb-1 ${theme.label}`}>Data Source:</label>
                  <select
                    value={selectedDataSourceId || ''}
                    onChange={(e) => setSelectedDataSourceId(parseInt(e.target.value))}
                    className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  >
                    <option value="">Select Data Source</option>
                    {dataSourceRegisterList.map((ds) => (
                      <option key={ds.Id} value={ds.Id}>{ds.DataSourceName}</option>
                    ))}
                  </select>
                </div>

                {/* Create Options - fixed icon width so labels align */}
                <div className="py-1">
                  <button
                    onClick={() => openEditorForCreate(EmAppDataServiceType.QueryText)}
                    className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu}`}
                  >
                    <span className="w-5 flex justify-center shrink-0" aria-hidden>
                      <i className="fa-solid fa-code"></i>
                    </span>
                    <span>Query Service</span>
                  </button>
                  <button
                    onClick={() => openEditorForCreate(EmAppDataServiceType.StoredProcedure)}
                    className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu}`}
                  >
                    <span className="w-5 flex justify-center shrink-0" aria-hidden>
                      <i className="fa-solid fa-database"></i>
                    </span>
                    <span>Stored Procedure Service</span>
                  </button>
                  <button
                    onClick={() => openEditorForCreate(EmAppDataServiceType.PluginWebApiCall)}
                    className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu}`}
                  >
                    <span className="w-5 flex justify-center shrink-0" aria-hidden>
                      <i className="fa-solid fa-plug"></i>
                    </span>
                    <span>Internal Web Api Call Service</span>
                  </button>
                  <button
                    onClick={() => openEditorForCreate(EmAppDataServiceType.IntegrationWebApiCall)}
                    className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu}`}
                  >
                    <span className="w-5 flex justify-center shrink-0" aria-hidden>
                      <i className="fa-solid fa-cloud"></i>
                    </span>
                    <span>Integration Web Api Call Service</span>
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Filter Dropdown */}
          <div className="relative" ref={filterDropdownRef}>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setFilterDropdownVisible(!filterDropdownVisible);
                setCreateDropdownVisible(false);
              }}
              className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
              title="Filter list"
            >
              <i className="fa-solid fa-filter" aria-hidden />
              <span>Filter</span>
              <i className="fa-solid fa-caret-down text-xs" aria-hidden />
            </button>
            {filterDropdownVisible && (
              <div
                className={`absolute right-0 mt-1 min-w-[160px] rounded shadow-lg z-50 border py-1 ${t('border_default')} ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <button
                  type="button"
                  onClick={() => {
                    setFilterQueryType(null);
                    setFilterDropdownVisible(false);
                  }}
                  className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu} ${filterQueryType === null ? theme.tab_active : ''}`}
                >
                  All Types
                </button>
                {queryTypeOptions.map((opt) => (
                  <button
                    key={opt.Id}
                    type="button"
                    onClick={() => {
                      setFilterQueryType(opt.Id);
                      setFilterDropdownVisible(false);
                    }}
                    className={`w-full px-3 py-2 text-left text-sm flex items-center gap-2 ${theme.contextMenu} ${filterQueryType === opt.Id ? theme.tab_active : ''}`}
                  >
                    {opt.Display}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Grid – first column = context menu trigger (like RestApiImportManagement), no button column on right */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          itemsSource={dataSetsCV || undefined}
          autoGenerateColumns={false}
          selectionMode="Row"
          headersVisibility="Column"
          isReadOnly={true}
          initialized={(grid: wjGrid.FlexGrid) => {
            flexGridRef.current = grid;
            grid.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(grid));
          }}
          selectionChanged={(grid: wjGrid.FlexGrid) => handleSelectionChanged(grid)}
          className="w-full h-full"
        >
          <FlexGridColumn isReadOnly width={60} header="Actions">
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => (
                <div className="flex items-center justify-center w-full">
                  <button
                    type="button"
                    className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                    title="More Options"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleContextMenu(e, ctx.item as DataSetItem);
                    }}
                  >
                    <i className="fa-solid fa-pencil text-xs" aria-hidden />
                    <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn header="Id" binding="Id" width={70} isReadOnly={true} />
            <FlexGridColumn header="Name" binding="Name" width={200} isReadOnly={true} />
            <FlexGridColumn header="Description" binding="Description" width={250} isReadOnly={true} />
            <FlexGridColumn
              header="Type"
              binding="QueryType"
              width={150}
              isReadOnly={true}
            >
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const dataItem = cell.item;
                  const queryType = dataItem?.QueryType;
                  return (
                    <div className="flex items-center gap-2">
                      <i className={`${getQueryTypeIcon(queryType)} text-xs`}></i>
                      <span>{getQueryTypeDisplay(queryType)}</span>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn
              header="Data Source From"
              binding="DataSourceFrom"
              width={150}
              isReadOnly={true}
              dataMap={dataSourceDataMapRef.current || undefined}
            />
            <FlexGridColumn header="Query Text" binding="QueryText" width={250} isReadOnly={true} />
            <FlexGridColumn header="Modified Date" binding="AppModifiedDate" width={120} isReadOnly={true} />
            <FlexGridColumn header="Created Date" binding="AppCreatedDate" width={120} isReadOnly={true} />
          </FlexGrid>
      </div>

      {/* Context menu – floating popup like RestApiImportManagement (Edit, Edit Extract View, Delete) */}
      {contextMenuVisible && selectedDataSet && (
        <div
          className={`fixed z-50 py-1 min-w-[200px] rounded-[4px] shadow-lg border ${t('border_default')} ${theme.mainContentSection}`}
          style={{ left: contextMenuPosition.x, top: contextMenuPosition.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`}
            onClick={() => handleEditDataSet()}
          >
            <i className="fa-solid fa-pen-to-square" aria-hidden />
            Edit
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`}
            onClick={() => {
              showInfo(`Would open Extract View Management for: ${selectedDataSet?.Name}`);
              closeContextMenu();
            }}
          >
            <i className="fa-solid fa-table-columns" aria-hidden />
            Edit Extract View
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs flex items-center gap-2 ${theme.contextMenu}`}
            onClick={() => handleDeleteDataSet()}
          >
            <i className="fa-solid fa-trash" aria-hidden />
            Delete
          </button>
        </div>
      )}

    </div>
  );
};

export default DatasetManagement;
