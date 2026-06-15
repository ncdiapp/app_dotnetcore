import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useDispatch } from 'react-redux';
import { ComboBox } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import appHelper from '../../helper/appHelper';
import MetaDataTableDesign from '../transaction/metaDataTableDesign';
import MetaDataViewDesign from '../transaction/metaDataViewDesign';
import TableDataPreview from '../transaction/TableDataPreview';

const DatabaseManagement: React.FC = () => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const emAppBuiltInQueryType = useEnumValues('EmAppBuiltInQueryType');

  // Data source selection
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<any[]>([]);
  const [selectedDataSourceId, setSelectedDataSourceId] = useState<number | null>(null);
  const datasourceRegisterDataMapRef = useRef<DataMap | null>(null);
  const dataSourceComboBoxRef = useRef<wjInput.ComboBox | null>(null);

  // Tables and Views
  const [tablesAndViews, setTablesAndViews] = useState<any[]>([]);
  const [selectedTable, setSelectedTable] = useState<any | null>(null);
  const [objectTypeFilter, setObjectTypeFilter] = useState<string>('Tables'); // 'Tables' or 'Views'
  const [tableViewNameFilter, setTableViewNameFilter] = useState<string>('');

  // Query editor
  const [queryText, setQueryText] = useState<string>('');
  const queryTextareaRef = useRef<HTMLTextAreaElement>(null);

  // Query results
  const [queryResults, setQueryResults] = useState<any[]>([]);
  const [queryResultsCV, setQueryResultsCV] = useState<CollectionView | null>(null);
  const [queryRowCount, setQueryRowCount] = useState<number>(0);

  // Panel resizing
  const [leftPanelWidth, setLeftPanelWidth] = useState<number>(300);
  const [queryResultHeight, setQueryResultHeight] = useState<number>(300);
  const [isResizingHorizontal, setIsResizingHorizontal] = useState<boolean>(false);
  const [isResizingVertical, setIsResizingVertical] = useState<boolean>(false);

  // Table / View editor popups
  const [showTableDesignPopup, setShowTableDesignPopup] = useState(false);
  const [tableDesignInfo, setTableDesignInfo] = useState<{ tableName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null } | null>(null);
  const [showViewDesignPopup, setShowViewDesignPopup] = useState(false);
  const [viewDesignInfo, setViewDesignInfo] = useState<{ viewName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null; isNewView: boolean } | null>(null);
  const [showContextMenu, setShowContextMenu] = useState(false);
  const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });
  const [contextMenuItem, setContextMenuItem] = useState<any | null>(null);
  const [showRenamePopup, setShowRenamePopup] = useState(false);
  const [renameTableInfo, setRenameTableInfo] = useState<any | null>(null);
  const [newTableName, setNewTableName] = useState('');
  const [previewTableInfo, setPreviewTableInfo] = useState<{ tableName: string; dataSourceRegisterId: number | null; schemaOwner: string | null } | null>(null);
  const [showPreviewPopup, setShowPreviewPopup] = useState(false);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);

  // Load data sources
  useEffect(() => {
    const loadDataSources = async () => {
      try {
        const dataSources = await adminSvc.getDataSourceRegisterList(false);
        const filtered = dataSources.filter((ds: any) => ds.Id !== 2147483647);
        setDataSourceRegisterList(filtered);
        
        const dataMap = new DataMap(filtered, 'Id', 'DataSourceName');
        datasourceRegisterDataMapRef.current = dataMap;

        // Auto-select first data source if available
        if (filtered.length > 0 && !selectedDataSourceId) {
          setSelectedDataSourceId(filtered[0].Id);
        }
      } catch (error) {
        appHelper.debugLog('Failed to load data sources:', error);
        showError('Failed to load data sources: ' + (error instanceof Error ? error.message : String(error)));
      }
    };
    
    loadDataSources();
  }, []);

  // Load tables and views when data source changes (use cache when available)
  useEffect(() => {
    if (selectedDataSourceId !== null) {
      loadTablesAndViews(true);
    }
  }, [selectedDataSourceId]);

  /** Load tables and views. useCache: true = use global cache when available; false = always fetch from server and update cache (e.g. Refresh button). */
  const loadTablesAndViews = async (useCache: boolean = true) => {
    if (selectedDataSourceId === null) return;

    dispatch(setIsBusy());
    try {
      const result = useCache
        ? await schemaMetadataService.getDataSourceTableAndViewListFromCache(
            selectedDataSourceId,
            null,
            null
          )
        : await schemaMetadataService.getDataSourceTableAndViewList(
            selectedDataSourceId,
            null,
            null,
            { bypassHttpCache: true }
          );

      if (Array.isArray(result)) {
        setTablesAndViews(result);
      } else {
        setTablesAndViews([]);
      }
    } catch (error) {
      appHelper.debugLog('Failed to load tables and views:', error);
      showError('Failed to load tables and views: ' + (error instanceof Error ? error.message : String(error)));
      setTablesAndViews([]);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle data source selection change
  const handleDataSourceChange = (combo: wjInput.ComboBox) => {
    const selectedItem = combo.selectedItem;
    if (selectedItem) {
      setSelectedDataSourceId(selectedItem.Id);
      setSelectedTable(null);
      setTableViewNameFilter('');
      setQueryText('');
      setQueryResults([]);
      setQueryResultsCV(null);
      setQueryRowCount(0);
    } else {
      setSelectedDataSourceId(null);
    }
  };

  // Handle table/view selection
  const handleTableSelect = (table: any) => {
    setSelectedTable(table);
    
    // Generate SELECT query for the table
    const tableName = table.TableName || table.Name || '';
    if (tableName) {
      const selectQuery = `SELECT * FROM [${tableName}]`;
      setQueryText(selectQuery);
      
      // Focus the textarea
      setTimeout(() => {
        if (queryTextareaRef.current) {
          queryTextareaRef.current.focus();
        }
      }, 0);
    }
  };

  // Open design editor for selected object (table or view)
  const handleEditTableOrView = (item: any) => {
    if (selectedDataSourceId === null || !item) return;

    const objectType = String(item.ObjectType || '').toUpperCase();
    const isView = objectType === 'VIEW' || item.IsDbView === true;
    const objectName = item.TableName || item.Name || null;
    const schemaOwner = item.SchemaOwner || null;

    if (isView) {
      setViewDesignInfo({
        viewName: objectName,
        dataSourceRegisterId: selectedDataSourceId,
        schemaOwner,
        applicationId: null,
        isNewView: false
      });
      setShowViewDesignPopup(true);
      closeResultItemContextMenu();
      return;
    }

    setTableDesignInfo({
      tableName: objectName,
      dataSourceRegisterId: selectedDataSourceId,
      schemaOwner,
      applicationId: null
    });
    setShowTableDesignPopup(true);
    closeResultItemContextMenu();
  };

  const getObjectName = (item: any): string => {
    return item?.Name || item?.TableName || '';
  };

  const closeResultItemContextMenu = () => {
    setShowContextMenu(false);
  };

  const openResultItemContextMenu = (event: React.MouseEvent, item: any) => {
    event.stopPropagation();
    setContextMenuItem(item);
    setContextMenuPosition({ x: event.clientX, y: event.clientY });
    setShowContextMenu(true);
  };

  const insertBuiltInQuery = async (item: any, emBuiltInQueryType: number) => {
    if (!item || selectedDataSourceId === null) return;

    try {
      dispatch(setIsBusy());
      const query = await schemaMetadataService.getDatabaseTableBuiltInQuery(
        getObjectName(item),
        selectedDataSourceId,
        item.SchemaOwner || null,
        emBuiltInQueryType
      );
      if (query) {
        setQueryText((prev) => `${prev ? `${prev}\n` : ''}${query}`);
      }
    } catch (error) {
      showError('Failed to get built-in query: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
      closeResultItemContextMenu();
    }
  };

  const handleDropTableOrView = async (item: any) => {
    if (!item || selectedDataSourceId === null) return;
    const objectName = getObjectName(item);
    if (!objectName) return;

    const objectType = String(item.ObjectType || item.ObjType || '').toUpperCase();
    const isView = objectType === 'VIEW' || item.IsDbView === true;
    const itemType = isView ? 'View' : 'Table';
    const confirmed = await showConfirm(`Confirm To Drop ${itemType} ${objectName}`, {
      title: 'Confirm',
      confirmLabel: 'OK',
      cancelLabel: 'Cancel'
    });
    if (!confirmed) {
      return;
    }

    try {
      dispatch(setIsBusy());
      const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
        objectName,
        selectedDataSourceId,
        item.SchemaOwner || null
      );
      tableData.Description = item.ObjType || item.ObjectType || (isView ? 'View' : 'Table');
      await schemaMetadataService.dropDatabaseTable(tableData);
      await loadTablesAndViews(false);
    } catch (error) {
      showError('Failed to drop table/view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
      closeResultItemContextMenu();
    }
  };

  const handleRenameTableOrView = (item: any) => {
    if (!item) return;
    const objectName = getObjectName(item);
    if (!objectName) return;
    setRenameTableInfo(item);
    setNewTableName(objectName);
    setShowRenamePopup(true);
    closeResultItemContextMenu();
  };

  const handleRenameOk = async () => {
    if (!renameTableInfo || selectedDataSourceId === null || !newTableName.trim()) return;
    const originalTableName = getObjectName(renameTableInfo);
    if (!originalTableName) return;

    try {
      dispatch(setIsBusy());
      await schemaMetadataService.renameTable(
        originalTableName,
        newTableName.trim(),
        selectedDataSourceId,
        renameTableInfo.SchemaOwner || null
      );
      setShowRenamePopup(false);
      setRenameTableInfo(null);
      setNewTableName('');
      await loadTablesAndViews(false);
    } catch (error) {
      showError('Failed to rename table/view: ' + (error instanceof Error ? error.message : String(error)));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const previewTableData = (item: any) => {
    if (!item || selectedDataSourceId === null) return;
    const tableName = getObjectName(item);
    if (!tableName) return;
    setPreviewTableInfo({
      tableName,
      dataSourceRegisterId: selectedDataSourceId,
      schemaOwner: item.SchemaOwner || null
    });
    setShowPreviewPopup(true);
    closeResultItemContextMenu();
  };

  // Execute query
  const handleExecuteQuery = async () => {
    if (!queryText.trim()) {
      showError('Please enter a SQL query');
      return;
    }

    if (selectedDataSourceId === null) {
      showError('Please select a data source');
      return;
    }

    dispatch(setIsBusy());
    try {
      const keyValue = {
        Key: selectedDataSourceId,
        Value: queryText
      };
      
      const result = await schemaMetadataService.executeQueryResult(keyValue);
      
      // Check if result has DataRowList
      if (result && result.DataRowList && Array.isArray(result.DataRowList)) {
        setQueryResults(result.DataRowList);
        const cv = new CollectionView(result.DataRowList);
        setQueryResultsCV(cv);
        setQueryRowCount(result.DataRowList.length);
      } else if (Array.isArray(result)) {
        setQueryResults(result);
        const cv = new CollectionView(result);
        setQueryResultsCV(cv);
        setQueryRowCount(result.length);
      } else {
        setQueryResults([]);
        setQueryResultsCV(null);
        setQueryRowCount(0);
      }
    } catch (error) {
      appHelper.debugLog('Failed to execute query:', error);
      showError('Failed to execute query: ' + (error instanceof Error ? error.message : String(error)));
      setQueryResults([]);
      setQueryResultsCV(null);
      setQueryRowCount(0);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Filter tables/views by object type and name
  const filteredTablesAndViews = tablesAndViews.filter((item: any) => {
    const matchesType =
      objectTypeFilter === 'Tables'
        ? item.ObjectType === 'Table' || item.ObjectType === 'TABLE' || !item.ObjectType
        : item.ObjectType === 'View' || item.ObjectType === 'VIEW';

    if (!matchesType) return false;

    const nameFilter = tableViewNameFilter.trim().toLowerCase();
    if (!nameFilter) return true;

    const tableName = String(item.TableName || item.Name || '').toLowerCase();
    return tableName.includes(nameFilter);
  });

  // Horizontal resize handlers (left-right)
  const handleHorizontalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingHorizontal(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleHorizontalResize = useCallback((e: MouseEvent) => {
    if (!isResizingHorizontal) return;
    
    const container = document.querySelector('.database-management-container');
    if (!container) return;
    
    const rect = container.getBoundingClientRect();
    const newWidth = e.clientX - rect.left;
    const minWidth = 200;
    const maxWidth = rect.width - 400; // Leave space for right panel
    
    if (newWidth >= minWidth && newWidth <= maxWidth) {
      setLeftPanelWidth(newWidth);
    }
  }, [isResizingHorizontal]);

  const handleHorizontalResizeEnd = useCallback(() => {
    setIsResizingHorizontal(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Vertical resize handlers (top-bottom)
  const handleVerticalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingVertical(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleVerticalResize = useCallback((e: MouseEvent) => {
    if (!isResizingVertical) return;
    
    // Find the right panel container (parent of the resizer)
    const resizer = document.querySelector('.vertical-resizer');
    if (!resizer || !resizer.parentElement) return;
    
    const rightPanel = resizer.parentElement;
    const rect = rightPanel.getBoundingClientRect();
    
    // Calculate height from bottom of right panel
    const heightFromBottom = rect.bottom - e.clientY;
    const minHeight = 150;
    const maxHeight = rect.height - 200; // Leave space for query editor (min 150px + header)
    
    if (heightFromBottom >= minHeight && heightFromBottom <= maxHeight) {
      setQueryResultHeight(heightFromBottom);
    }
  }, [isResizingVertical]);

  const handleVerticalResizeEnd = useCallback(() => {
    setIsResizingVertical(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Add event listeners for resize
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

  // Initialize ComboBox selection
  useEffect(() => {
    if (dataSourceComboBoxRef.current && selectedDataSourceId !== null) {
      dataSourceComboBoxRef.current.selectedValue = '';
      setTimeout(() => {
        if (dataSourceComboBoxRef.current) {
          dataSourceComboBoxRef.current.selectedValue = selectedDataSourceId;
        }
      }, 0);
    }
  }, [selectedDataSourceId, dataSourceRegisterList]);

  useEffect(() => {
    if (!showContextMenu) return;

    const closeMenu = (event: MouseEvent) => {
      const target = event.target as Node | null;
      if (contextMenuRef.current && target && contextMenuRef.current.contains(target)) {
        return;
      }
      setShowContextMenu(false);
    };
    document.addEventListener('mousedown', closeMenu);
    return () => {
      document.removeEventListener('mousedown', closeMenu);
    };
  }, [showContextMenu]);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden database-management-container">
      {/* Header Toolbar – structure and button pattern match RestApiImportManagement */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>SQL Workbench</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => {
              if (selectedDataSourceId !== null) {
                setTableDesignInfo({
                  tableName: null,
                  dataSourceRegisterId: selectedDataSourceId,
                  schemaOwner: null,
                  applicationId: null
                });
                setShowTableDesignPopup(true);
              }
            }}
            disabled={selectedDataSourceId === null}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:cursor-not-allowed disabled:opacity-60 ${theme.button_default}`}
            title="Add Table"
          >
            <i className="fa-solid fa-table" aria-hidden />
            <span>Add Table</span>
          </button>
          <button
            type="button"
            onClick={() => {
              if (selectedDataSourceId !== null) {
                setViewDesignInfo({
                  viewName: null,
                  dataSourceRegisterId: selectedDataSourceId,
                  schemaOwner: null,
                  applicationId: null,
                  isNewView: true
                });
                setShowViewDesignPopup(true);
              }
            }}
            disabled={selectedDataSourceId === null}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:cursor-not-allowed disabled:opacity-60 ${theme.button_default}`}
            title="Add View"
          >
            <i className="fa-solid fa-table-columns" aria-hidden />
            <span>Add View</span>
          </button>
          <button
            type="button"
            onClick={() => loadTablesAndViews(false)}
            disabled={selectedDataSourceId === null}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:cursor-not-allowed disabled:opacity-60 ${theme.button_default}`}
            title="Refresh All Tables and Views"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh All Tables and Views</span>
          </button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className={`flex-auto h-1 overflow-hidden flex ${theme.mainContentSection}`}>
        {/* Left Panel - Tables and Views */}
        <div
          className="flex flex-col overflow-hidden"
          style={{ width: `${leftPanelWidth}px`, minWidth: '200px' }}
        >
          <div className={`flex flex-col h-full ${theme.mainContentSection} border-r ${t('border_default')}`}>
            {/* Panel Header */}
            <div className={`px-3 py-2 border-b flex items-center ${t('border_default')}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Tables And Views</div>
            </div>

            {/* Data Source Selection */}
            <div className={`px-3 py-2 border-b ${t('border_default')}`}>
              <label className={`text-xs mb-1 block ${theme.label}`}>Data Source:</label>
              <ComboBox
                ref={dataSourceComboBoxRef}
                itemsSource={dataSourceRegisterList}
                displayMemberPath="DataSourceName"
                selectedValuePath="Id"
                selectedValue={selectedDataSourceId}
                selectedIndexChanged={handleDataSourceChange}
                placeholder="Select Data Source"
                className={`w-full h-7 ${theme.inputBox}`}
              />
            </div>

            {/* Object Type Filter */}
            <div className={`px-3 py-2 border-b flex gap-2 ${t('border_default')}`}>
              <button
                type="button"
                onClick={() => setObjectTypeFilter('Tables')}
                className={`px-2 py-1 rounded-[4px] text-xs ${objectTypeFilter === 'Tables' ? theme.tab_active : theme.tab}`}
              >
                Tables
              </button>
              <button
                type="button"
                onClick={() => setObjectTypeFilter('Views')}
                className={`px-2 py-1 rounded-[4px] text-xs ${objectTypeFilter === 'Views' ? theme.tab_active : theme.tab}`}
              >
                Views
              </button>
            </div>

            {/* Name Filter */}
            <div className={`px-3 py-2 border-b ${t('border_default')}`}>
              <div className="relative">
                <input
                  type="text"
                  value={tableViewNameFilter}
                  onChange={(e) => setTableViewNameFilter(e.target.value)}
                  placeholder="Filter tables/views..."
                  className={`w-full h-7 pl-2 pr-7 text-xs border rounded-[4px] ${theme.inputBox}`}
                />
                {tableViewNameFilter && (
                  <button
                    type="button"
                    onClick={() => setTableViewNameFilter('')}
                    className={`absolute right-1 top-1/2 -translate-y-1/2 w-5 h-5 flex items-center justify-center rounded ${theme.button_default}`}
                    title="Clear filter"
                  >
                    <i className="fa-solid fa-xmark text-[10px]" aria-hidden />
                  </button>
                )}
              </div>
            </div>

            {/* Tables/Views List */}
            <div className="flex-auto h-1 overflow-y-auto px-2 py-2">
              <div className={`text-xs mb-2 ${theme.label}`}>
                Object Type: {objectTypeFilter} ({filteredTablesAndViews.length} items)
              </div>
              <div className="space-y-1">
                {filteredTablesAndViews.map((item: any, index: number) => {
                  const tableName = item.TableName || item.Name || `Item ${index}`;
                  const isSelected = selectedTable && (selectedTable.TableName === item.TableName || selectedTable.Name === item.Name);
                  
                  return (
                    <div
                      key={index}
                      onClick={() => handleTableSelect(item)}
                      className={`px-2 py-1 rounded cursor-pointer flex items-center justify-between ${isSelected ? theme.tab_active : theme.contextMenu}`}
                      title={tableName}
                    >
                      <span className="text-xs truncate flex-auto">{tableName}</span>
                      <div className="flex items-center gap-1 ml-2">
                        <button
                          type="button"
                          className={`w-6 h-5 text-xs rounded ${theme.button_default}`}
                          title="More Actions"
                          onClick={(e) => {
                            openResultItemContextMenu(e, item);
                          }}
                        >
                          <i className="fa-solid fa-bars text-[10px]"></i>
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>

        {/* Horizontal Resizer */}
        <div
          className={`w-1 cursor-col-resize ${t('border_default')} ${isResizingHorizontal ? theme.tab_active : t('bg_default')}`}
          onMouseDown={handleHorizontalResizeStart}
        />

        {/* Right Panel - Query Editor and Results */}
        <div
          className="flex-auto w-1 flex flex-col overflow-hidden"
          style={{ width: `calc(100% - ${leftPanelWidth}px - 4px)` }}
        >
          {/* Top Right - Query Editor */}
          <div className="flex flex-col overflow-hidden flex-auto h-1" style={{ minHeight: '150px' }}>
            <div className={`flex flex-col h-full ${theme.mainContentSection}`}>
              {/* Query Editor Header */}
              <div className={`px-3 py-2 border-b flex items-center justify-between ${t('border_default')}`}>
                <div className={`text-sm font-semibold ${theme.title}`}>Query</div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={handleExecuteQuery}
                    disabled={!queryText.trim() || selectedDataSourceId === null}
                    className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:cursor-not-allowed disabled:opacity-60 ${theme.button_default}`}
                    title="Execute Query"
                  >
                    <i className="fa-solid fa-play" aria-hidden />
                    <span>Execute</span>
                  </button>
                </div>
              </div>

              {/* Query Textarea */}
              <div className="flex-auto h-1 overflow-hidden p-3">
                <textarea
                  ref={queryTextareaRef}
                  value={queryText}
                  onChange={(e) => setQueryText(e.target.value)}
                  className={`w-full h-full p-2 border rounded ${theme.inputBox} font-mono text-sm resize-none`}
                  placeholder="Enter SQL query here..."
                  spellCheck={false}
                />
              </div>
            </div>
          </div>

          {/* Vertical Resizer */}
          <div
            className={`vertical-resizer h-1 cursor-row-resize ${t('border_default')} ${isResizingVertical ? theme.tab_active : t('bg_default')}`}
            onMouseDown={handleVerticalResizeStart}
          />

          {/* Bottom Right - Query Results */}
          <div
            className="overflow-hidden"
            style={{ height: `${queryResultHeight}px`, minHeight: '150px', flexShrink: 0 }}
          >
            <div className={`flex flex-col h-full ${theme.mainContentSection}`}>
              {/* Query Results Header */}
              <div className={`px-3 py-2 border-b flex items-center justify-between ${t('border_default')}`}>
                <div className={`text-sm font-semibold ${theme.title}`}>
                  Query Result {queryRowCount > 0 && `(${queryRowCount} Rows)`}
                </div>
              </div>

              {/* Query Results Grid */}
              <div className="flex-auto h-1 overflow-hidden">
                {queryResultsCV ? (
                  <FlexGrid
                    itemsSource={queryResultsCV}
                    headersVisibility="Column"
                    autoGenerateColumns={true}
                    isReadOnly={true}
                    style={{ height: '100%', width: '100%' }}
                  />
                ) : (
                  <div className={`h-full flex items-center justify-center text-gray-500`}>
                    <div className="text-center">
                      <i className="fa-solid fa-table text-4xl mb-2 opacity-50"></i>
                      <div className="text-sm">No query results. Execute a query to see results.</div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Table Design Popup */}
      {tableDesignInfo && (
        <MetaDataTableDesign
          isOpen={showTableDesignPopup}
          onClose={() => {
            setShowTableDesignPopup(false);
            setTableDesignInfo(null);
          }}
          onSave={(_tableData: any, isNewTable: boolean) => {
            if (isNewTable) {
              loadTablesAndViews(false);
              return;
            }
            // Keep list consistent after alter/rename scenarios.
            loadTablesAndViews(false);
          }}
          tableName={tableDesignInfo.tableName}
          dataSourceRegisterId={tableDesignInfo.dataSourceRegisterId}
          schemaOwner={tableDesignInfo.schemaOwner}
          applicationId={tableDesignInfo.applicationId}
        />
      )}

      {/* View Design Popup */}
      {viewDesignInfo && (
        <MetaDataViewDesign
          isOpen={showViewDesignPopup}
          onClose={() => {
            setShowViewDesignPopup(false);
            setViewDesignInfo(null);
          }}
          onSave={() => {
            loadTablesAndViews(false);
          }}
          viewName={viewDesignInfo.viewName}
          dataSourceRegisterId={viewDesignInfo.dataSourceRegisterId}
          schemaOwner={viewDesignInfo.schemaOwner}
          applicationId={viewDesignInfo.applicationId}
          isNewView={viewDesignInfo.isNewView}
        />
      )}

      {/* Context Menu */}
      {showContextMenu && contextMenuItem && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[170px]`}
          style={{ left: contextMenuPosition.x, top: contextMenuPosition.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Select !== undefined) {
                insertBuiltInQuery(contextMenuItem, emAppBuiltInQueryType.Select);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={emAppBuiltInQueryType?.Select === undefined}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Select
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Update !== undefined) {
                insertBuiltInQuery(contextMenuItem, emAppBuiltInQueryType.Update);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={emAppBuiltInQueryType?.Update === undefined}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Update
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Insert !== undefined) {
                insertBuiltInQuery(contextMenuItem, emAppBuiltInQueryType.Insert);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={emAppBuiltInQueryType?.Insert === undefined}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Insert
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Delete !== undefined) {
                insertBuiltInQuery(contextMenuItem, emAppBuiltInQueryType.Delete);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={emAppBuiltInQueryType?.Delete === undefined}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Delete
          </button>
          <div className="border-t my-1"></div>
          <button
            onClick={() => handleEditTableOrView(contextMenuItem)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-pencil mr-2"></i> Alter
          </button>
          <button
            onClick={() => handleDropTableOrView(contextMenuItem)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-trash mr-2"></i> Drop
          </button>
          <button
            onClick={() => handleRenameTableOrView(contextMenuItem)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-pencil mr-2"></i> Rename
          </button>
          <div className="border-t my-1"></div>
          <button
            onClick={() => previewTableData(contextMenuItem)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-eye mr-2"></i> Preview Data
          </button>
        </div>
      )}

      {/* Rename Popup */}
      {showRenamePopup && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className={`rounded p-6 w-[350px] ${theme.mainContentSection}`}>
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold">Rename Table</h3>
              <button onClick={() => setShowRenamePopup(false)} className="text-xl">&times;</button>
            </div>
            <div className="mb-4">
              <label className="block mb-2 text-xs">New Name</label>
              <input
                type="text"
                value={newTableName}
                onChange={(e) => setNewTableName(e.target.value)}
                className={`w-full px-3 py-2 border rounded text-sm ${theme.inputBox}`}
                autoFocus
              />
            </div>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setShowRenamePopup(false)}
                className={`px-3 py-1.5 rounded-[4px] text-sm ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                onClick={handleRenameOk}
                className={`px-3 py-1.5 rounded-[4px] text-sm ${theme.button_default}`}
              >
                Rename
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Preview Popup */}
      {previewTableInfo && (
        <TableDataPreview
          isOpen={showPreviewPopup}
          onClose={() => {
            setShowPreviewPopup(false);
            setPreviewTableInfo(null);
          }}
          tableName={previewTableInfo.tableName}
          dataSourceRegisterId={previewTableInfo.dataSourceRegisterId}
          schemaOwner={previewTableInfo.schemaOwner}
          recordLimit={100}
        />
      )}
    </div>
  );
};

export default DatabaseManagement;
