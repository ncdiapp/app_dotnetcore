import React, { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import TableDataPreview from './TableDataPreview';
import MetaDataTableDesign from './metaDataTableDesign';
import MetaDataViewDesign from './metaDataViewDesign';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 320;
const ADD_MENU_ESTIMATED_WIDTH = 170;
const ADD_MENU_ESTIMATED_HEIGHT = 120;

// Cache for table and view list data (similar to angular.dictFilterKeyAndDbTableViewList)
const dictFilterKeyAndDbTableViewList: { [key: string]: any[] } = {};

/** Call after creating a new DB object (e.g. Excel staging table) so the next SQL Workbench open refetches the list. */
export function clearMetaDataManagementTableListClientCache(): void {
  Object.keys(dictFilterKeyAndDbTableViewList).forEach((k) => {
    delete dictFilterKeyAndDbTableViewList[k];
  });
}

const MetaDataManagement: React.FC = () => {
  const dispatch = useDispatch();
  const { theme, t } = useTheme();
  const { showValidationMessages, showError, showInfo } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const emAppBuiltInQueryType = useEnumValues('EmAppBuiltInQueryType');

  const { param } = useParams<{ param: string }>();

  // Parse params
  let paramObj: any = {};
  if (param) {
    try {
      const decodedParam = decodeURIComponent(param);
      paramObj = JSON.parse(decodedParam);
    } catch (error) {
      console.error('Error parsing param JSON:', error);
    }
  }

  const applicationId = paramObj.applicationId || null;
  const initDataSourceRegId = paramObj.initDataSourceRegId || null;
  const initTableName = paramObj.initTableName || null;
  const initSchema = paramObj.initSchema || '';

  // Refs
  const flexGridSchemaRef = useRef<any>(null);
  const flexQueryResultRef = useRef<any>(null);
  const ddlDataSourceFromRef = useRef<wjInput.ComboBox | null>(null);
  const queryTextAreaRef = useRef<HTMLTextAreaElement>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);
  const addMenuRef = useRef<HTMLDivElement | null>(null);

  // State
  const [dataModel, setDataModel] = useState<{
    dataSourceRegisterList: any[];
    dataSourceFromCV: CollectionView | null;
    currentDataSourceFrom: number | null;
    databaseSchemaData: CollectionView | null;
    queryText: string;
    queryResult: any[];
    isFilterByApplicationId: boolean;
    isBusy: boolean;
    currentSourceTableObj: any | null;
    selectedDataRow: any | null;
    currentRenamingTable: any | null;
    newTableName: string;
    columnSelectorTableDto: any | null;
    tableColumnSelectorCV: CollectionView | null;
  }>({
    dataSourceRegisterList: [],
    dataSourceFromCV: null,
    currentDataSourceFrom: null,
    databaseSchemaData: null,
    queryText: '',
    queryResult: [],
    isFilterByApplicationId: false,
    isBusy: false,
    currentSourceTableObj: null,
    selectedDataRow: null,
    currentRenamingTable: null,
    newTableName: '',
    columnSelectorTableDto: null,
    tableColumnSelectorCV: null
  });

  const [showRenamePopup, setShowRenamePopup] = useState(false);
  const [showColumnSelector, setShowColumnSelector] = useState(false);
  const [showContextMenu, setShowContextMenu] = useState(false);
  const [contextMenuPosition, setContextMenuPosition] = useState({ x: 0, y: 0 });
  const [showAddMenu, setShowAddMenu] = useState(false);
  const [addMenuPosition, setAddMenuPosition] = useState({ x: 0, y: 0 });
  const [columnPopupPosition, setColumnPopupPosition] = useState({ x: 0, y: 0 });
  const [isDraggingPopup, setIsDraggingPopup] = useState(false);
  const [popupDragOffset, setPopupDragOffset] = useState({ x: 0, y: 0 });
  const [showPreviewPopup, setShowPreviewPopup] = useState(false);
  const [previewTableInfo, setPreviewTableInfo] = useState<{ tableName: string; dataSourceRegisterId: number | null; schemaOwner: string | null } | null>(null);
  const [showTableDesignPopup, setShowTableDesignPopup] = useState(false);
  const [tableDesignInfo, setTableDesignInfo] = useState<{ tableName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null } | null>(null);
  const [showViewDesignPopup, setShowViewDesignPopup] = useState(false);
  const [viewDesignInfo, setViewDesignInfo] = useState<{ viewName: string | null; dataSourceRegisterId: number | null; schemaOwner: string | null; applicationId: number | null; isNewView: boolean } | null>(null);

  // Resizable panel sizes
  const [leftPanelWidth, setLeftPanelWidth] = useState(350);
  const [queryResultHeight, setQueryResultHeight] = useState(200);
  const [isResizingHorizontal, setIsResizingHorizontal] = useState(false);
  const [isResizingVertical, setIsResizingVertical] = useState(false);

  // Load tables by data source with caching
  const loadTablesByDataSourceFrom = useCallback(async (isForceRefreshCache: boolean = false, dataSourceId?: number | null) => {
    const targetDataSourceId = dataSourceId !== undefined ? dataSourceId : dataModel.currentDataSourceFrom;
    if (!targetDataSourceId) return;

    try {
      dispatch(setIsBusy());
      setDataModel(prev => ({ ...prev, isBusy: true }));

      let saasFilterOption = null;
      let filterByApplicationId = null;

      if (dataModel.isFilterByApplicationId && applicationId) {
        saasFilterOption = 1; // EmAppSaasTableFilterOption.ByApplication
        filterByApplicationId = applicationId;
      }

      // Create cache key (same format as AngularJS)
      const filterKey = `${targetDataSourceId || ''}|${saasFilterOption || ''}|${filterByApplicationId || ''}`;

      let data: any[];

      // Check cache first (unless forcing refresh)
      if (!isForceRefreshCache && dictFilterKeyAndDbTableViewList[filterKey] && dictFilterKeyAndDbTableViewList[filterKey].length > 0) {
        data = dictFilterKeyAndDbTableViewList[filterKey];
      } else {
        // Fetch from API
        data = await schemaMetadataService.getDataSourceTableAndViewList(
          targetDataSourceId,
          saasFilterOption,
          filterByApplicationId
        );
        // Store in cache
        dictFilterKeyAndDbTableViewList[filterKey] = data;
      }

      const databaseSchemaData = new CollectionView(data);
      databaseSchemaData.groupDescriptions.push(new PropertyGroupDescription('ObjType'));

      setDataModel(prev => ({
        ...prev,
        databaseSchemaData,
        isBusy: false
      }));

      // Select initial table if specified
      if (initTableName && targetDataSourceId === initDataSourceRegId && flexGridSchemaRef.current) {
        setTimeout(() => {
          const flex = flexGridSchemaRef.current;
          if (flex && flex.rows) {
            for (let i = 0; i < flex.rows.length; i++) {
              const rowData = flex.rows[i].dataItem;
              if (rowData && rowData.Name === initTableName && rowData.SchemaOwner === initSchema) {
                flex.select(new (flex.constructor as any).CellRange(i, 0, i, flex.columns.length), true);
                break;
              }
            }
          }
        }, 100);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load tables');
      setDataModel(prev => ({ ...prev, isBusy: false }));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel.currentDataSourceFrom, dataModel.isFilterByApplicationId, applicationId, initTableName, initDataSourceRegId, initSchema, dispatch, showError]);

  // Load data source register list
  const loadDataFromServer = useCallback(async () => {
    try {
      dispatch(setIsBusy());
      const dataSourceRegisterList = await adminSvc.getDataSourceRegisterList(false);

      const dataSourceFromCV = new CollectionView(dataSourceRegisterList);

      let currentDataSourceFrom = initDataSourceRegId;
      if (!dataSourceRegisterList.find((ds: any) => ds.Id === initDataSourceRegId)) {
        currentDataSourceFrom = dataSourceRegisterList[0]?.Id || null;
      }

      setDataModel(prev => ({
        ...prev,
        dataSourceRegisterList,
        dataSourceFromCV,
        currentDataSourceFrom
      }));

      // Pass currentDataSourceFrom directly to avoid stale state issue
      await loadTablesByDataSourceFrom(false, currentDataSourceFrom);
    } catch (error: any) {
      showError(error.message || 'Failed to load data source register list');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [initDataSourceRegId, dispatch, showError, loadTablesByDataSourceFrom]);

  // Handle data source change
  const handleDataSourceFromChange = useCallback((sender: wjInput.ComboBox) => {
    if (sender.selectedValue !== dataModel.currentDataSourceFrom) {
      setDataModel(prev => ({
        ...prev,
        currentDataSourceFrom: sender.selectedValue
      }));
      setTimeout(() => {
        loadTablesByDataSourceFrom(false);
      }, 100);
    }
  }, [dataModel.currentDataSourceFrom, loadTablesByDataSourceFrom]);

  // Initialize data source dropdown
  const initializeDataSourceFromDdl = useCallback((sender: wjInput.ComboBox) => {
    ddlDataSourceFromRef.current = sender;
    if (sender) {
      sender.selectedIndexChanged.addHandler((s: wjInput.ComboBox) => {
        handleDataSourceFromChange(s);
      });
    }
  }, [handleDataSourceFromChange]);

  // Get selected query text from textarea
  const getSelectedQueryText = useCallback((): string => {
    if (!queryTextAreaRef.current) return '';
    
    const textarea = queryTextAreaRef.current;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    
    if (start !== end && start >= 0 && end >= 0) {
      // Text is selected
      return dataModel.queryText.substring(start, end);
    }
    
    return '';
  }, [dataModel.queryText]);

  // Execute query
  const executeQuery = useCallback(async () => {
    if (!dataModel.queryText?.trim() || !dataModel.currentDataSourceFrom) {
      showError('Please enter a query and select a data source');
      return;
    }

    // Get selected text if available, otherwise use full query text
    let queryText = getSelectedQueryText();
    if (!queryText || !queryText.trim()) {
      queryText = dataModel.queryText;
    }

    try {
      dispatch(setIsBusy());
      const keyValue = {
        Key: dataModel.currentDataSourceFrom,
        Value: queryText
      };

      const result = await schemaMetadataService.executeQueryResult(keyValue);

      setDataModel(prev => ({
        ...prev,
        queryResult: result.DataRowList || []
      }));

      if (result.ErrorMessage) {
        if (result.ErrorMessage === 'Query completed successfully') {
          showInfo(result.ErrorMessage);
        } else {
          showValidationMessages({
            Items: [{
              ItemType: 2, // Warning
              LocalizedMessage: result.ErrorMessage
            }]
          });
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to execute query');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel.queryText, dataModel.currentDataSourceFrom, dispatch, showError, showInfo, showValidationMessages, getSelectedQueryText]);

  // Refresh tables
  const refresh = useCallback(() => {
    loadTablesByDataSourceFrom(true);
  }, [loadTablesByDataSourceFrom]);

  // Set current source table object
  const setCurrentSourceTableObj = useCallback((item: any) => {
    setDataModel(prev => ({ ...prev, currentSourceTableObj: item }));
  }, []);

  // Insert table name into query
  const clickTableNameTokenButton = useCallback((token: string) => {
    if (token && queryTextAreaRef.current) {
      const textarea = queryTextAreaRef.current;
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const text = dataModel.queryText;
      const newText = text.substring(0, start) + ' ' + token + ' ' + text.substring(end);
      setDataModel(prev => ({ ...prev, queryText: newText }));
      setTimeout(() => {
        textarea.focus();
        textarea.setSelectionRange(start + token.length + 2, start + token.length + 2);
      }, 0);
    }
  }, [dataModel.queryText]);

  // Handle drag start for table/view
  const handleTableDragStart = useCallback((e: React.DragEvent, tableItem: any) => {
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('application/json', JSON.stringify({
      type: 'table',
      name: tableItem.Name,
      schemaOwner: tableItem.SchemaOwner
    }));
    setCurrentSourceTableObj(tableItem);
  }, [setCurrentSourceTableObj]);

  // Handle drag start for columns (from header)
  const handleColumnHeaderDragStart = useCallback((e: React.DragEvent) => {
    e.dataTransfer.effectAllowed = 'copy';
    e.dataTransfer.setData('application/json', JSON.stringify({ type: 'columns' }));
  }, []);

  // Handle drag start for individual column
  // When dragging a column, add all checked columns, not just the one being dragged
  const handleColumnDragStart = useCallback((e: React.DragEvent, columnName: string) => {
    e.stopPropagation();
    e.dataTransfer.effectAllowed = 'copy';
    // Always use 'columns' type to insert all checked columns
    e.dataTransfer.setData('application/json', JSON.stringify({ 
      type: 'columns'
    }));
  }, []);

  // Handle popup header drag start (for moving popup)
  const handlePopupHeaderMouseDown = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0) return; // Only handle left mouse button
    
    const popupElement = e.currentTarget.closest('.column-popup-container') as HTMLElement;
    if (!popupElement) return;

    const rect = popupElement.getBoundingClientRect();
    setPopupDragOffset({
      x: e.clientX - rect.left,
      y: e.clientY - rect.top
    });
    setIsDraggingPopup(true);
  }, []);

  // Handle popup dragging
  useEffect(() => {
    if (!isDraggingPopup) return;

    const handleMouseMove = (e: MouseEvent) => {
      setColumnPopupPosition({
        x: e.clientX - popupDragOffset.x,
        y: e.clientY - popupDragOffset.y
      });
    };

    const handleMouseUp = () => {
      setIsDraggingPopup(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDraggingPopup, popupDragOffset]);


  // Handle drop on query editor
  const handleQueryEditorDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (!queryTextAreaRef.current) return;

    try {
      const data = JSON.parse(e.dataTransfer.getData('application/json'));
      const textarea = queryTextAreaRef.current;
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const text = dataModel.queryText;

      let insertText = '';

      if (data.type === 'table') {
        // Insert table name
        insertText = ' ' + data.name + ' ';
      } else if (data.type === 'columns') {
        // Insert selected columns
        const tableData = dataModel.columnSelectorTableDto;
        if (tableData && tableData.Columns) {
          let columnText = '';
          tableData.Columns.forEach((column: any, index: number) => {
            if (column.isSelected) {
              if (columnText) {
                columnText += '    ,';
              } else {
                columnText += '\n    ';
              }
              columnText += '[' + column.Name + ']\n';
            }
          });
          if (columnText) {
            insertText = ' ' + columnText + ' ';
          }
        }
      }

      if (insertText) {
        const newText = text.substring(0, start) + insertText + text.substring(end);
        setDataModel(prev => ({ ...prev, queryText: newText }));
        setTimeout(() => {
          textarea.focus();
          const newCursorPos = start + insertText.length;
          textarea.setSelectionRange(newCursorPos, newCursorPos);
        }, 0);
      }
    } catch (error) {
      console.error('Error handling drop:', error);
    }
  }, [dataModel.queryText, dataModel.columnSelectorTableDto]);

  // Handle drag over query editor
  const handleQueryEditorDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    e.dataTransfer.dropEffect = 'copy';
  }, []);

  // Open table column popup
  const openTableColumnPopup = useCallback(async (event: React.MouseEvent, tableObj: any) => {
    event.stopPropagation();
    if (!tableObj?.Name || !dataModel.currentDataSourceFrom) return;

    // Calculate popup position to the left of the clicked button
    const buttonRect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const popupWidth = 320;
    const popupHeight = 500;
    const offsetX = 10; // Space between button and popup
    
    let popupX = buttonRect.left - popupWidth - offsetX;
    let popupY = buttonRect.top;
    
    // Ensure popup stays within viewport
    if (popupX < 0) {
      popupX = buttonRect.right + offsetX; // Show to the right if no space on left
    }
    if (popupY + popupHeight > window.innerHeight) {
      popupY = window.innerHeight - popupHeight - 10; // Adjust if too low
    }
    if (popupY < 0) {
      popupY = 10; // Adjust if too high
    }

    try {
      dispatch(setIsBusy());
      const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
        tableObj.Name,
        dataModel.currentDataSourceFrom,
        tableObj.SchemaOwner || null
      );

      if (tableData) {
        tableData.isSelectAllTableColumn = false;
        // Set all columns to unselected by default
        if (tableData.Columns) {
          tableData.Columns.forEach((column: any) => {
            column.isSelected = false;
          });
        }
        const tableColumnSelectorCV = new CollectionView(tableData.Columns || []);
        
        // Set popup position before showing
        setColumnPopupPosition({ x: popupX, y: popupY });
        
        setDataModel(prev => ({
          ...prev,
          columnSelectorTableDto: tableData,
          tableColumnSelectorCV
        }));
        setShowColumnSelector(true);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load table columns');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel.currentDataSourceFrom, dispatch, showError]);

  // Close table column popup
  const closeTableColumnPopup = useCallback(() => {
    setShowColumnSelector(false);
    setDataModel(prev => ({
      ...prev,
      tableColumnSelectorCV: null,
      columnSelectorTableDto: null
    }));
    // Reset position for next open
    setColumnPopupPosition({ x: 0, y: 0 });
  }, []);

  // Handle select all table columns
  const isSelectAllTableColumnChanged = useCallback(() => {
    const tableData = dataModel.columnSelectorTableDto;
    if (tableData && tableData.Columns) {
      tableData.Columns.forEach((column: any) => {
        column.isSelected = tableData.isSelectAllTableColumn || false;
      });
      if (dataModel.tableColumnSelectorCV) {
        dataModel.tableColumnSelectorCV.refresh();
      }
    }
  }, [dataModel.columnSelectorTableDto, dataModel.tableColumnSelectorCV]);

  // Open context menu
  const openResultItemContextMenu = useCallback((event: React.MouseEvent, dataItem: any) => {
    event.stopPropagation();
    setDataModel(prev => ({ ...prev, selectedDataRow: dataItem }));
    const { x, y } = clampContextMenuPosition(
      event.clientX,
      event.clientY,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setContextMenuPosition({ x, y });
    setShowContextMenu(true);
  }, []);

  // Close context menu
  const closeResultItemContextMenu = useCallback(() => {
    setShowContextMenu(false);
  }, []);

  // Open add menu
  const openAddMenu = useCallback((event: React.MouseEvent) => {
    event.stopPropagation();
    const { x, y } = clampContextMenuPosition(
      event.clientX,
      event.clientY,
      ADD_MENU_ESTIMATED_WIDTH,
      ADD_MENU_ESTIMATED_HEIGHT
    );
    setAddMenuPosition({ x, y });
    setShowAddMenu(true);
  }, []);

  // Close add menu
  const closeAddMenu = useCallback(() => {
    setShowAddMenu(false);
  }, []);

  // Insert built-in query
  const insertBuiltInQuery = useCallback(async (tableObj: any, emBuiltInQueryType: number) => {
    if (!tableObj || !dataModel.currentDataSourceFrom) return;

    try {
      dispatch(setIsBusy());
      const query = await schemaMetadataService.getDatabaseTableBuiltInQuery(
        tableObj.Name,
        dataModel.currentDataSourceFrom,
        tableObj.SchemaOwner || null,
        emBuiltInQueryType
      );

      if (query) {
        setDataModel(prev => ({
          ...prev,
          queryText: prev.queryText + '\n' + query
        }));
      }
    } catch (error: any) {
      showError(error.message || 'Failed to get built-in query');
    } finally {
      dispatch(setIsNotBusy());
      closeResultItemContextMenu();
    }
  }, [dataModel.currentDataSourceFrom, dispatch, showError, closeResultItemContextMenu]);

  // Drop table
  const dropTable = useCallback(async (selectedDataRow: any) => {
    if (!selectedDataRow?.Name || !dataModel.currentDataSourceFrom) return;

    // Check if it's a view or table
    const isView = selectedDataRow.IsDbView || selectedDataRow.ObjType === 'View';
    const itemType = isView ? 'View' : 'Table';
    const confirmed = await showConfirm(
      `Confirm To Drop ${itemType} ${selectedDataRow.Name}`,
      {
        title: 'Confirm',
        confirmLabel: 'OK',
        cancelLabel: 'Cancel'
      }
    );
    if (!confirmed) {
      return;
    }

    try {
      dispatch(setIsBusy());
      const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
        selectedDataRow.Name,
        dataModel.currentDataSourceFrom,
        selectedDataRow.SchemaOwner || null
      );

      tableData.Description = selectedDataRow.ObjType;
      await schemaMetadataService.dropDatabaseTable(tableData);
      showInfo('Table dropped successfully');
      refresh();
    } catch (error: any) {
      showError(error.message || 'Failed to drop table');
    } finally {
      dispatch(setIsNotBusy());
      closeResultItemContextMenu();
    }
  }, [dataModel.currentDataSourceFrom, dispatch, showError, showInfo, refresh, closeResultItemContextMenu, showConfirm]);

  // Rename table
  const renameTable = useCallback((selectedDataRow: any) => {
    if (!selectedDataRow?.Name) return;
    setDataModel(prev => ({
      ...prev,
      currentRenamingTable: selectedDataRow,
      newTableName: selectedDataRow.Name
    }));
    setShowRenamePopup(true);
    closeResultItemContextMenu();
  }, [closeResultItemContextMenu]);

  // Save rename
  const renameOk = useCallback(async () => {
    const { currentRenamingTable, newTableName, currentDataSourceFrom } = dataModel;
    if (!currentRenamingTable || !newTableName || !currentDataSourceFrom) return;

    try {
      dispatch(setIsBusy());
      await schemaMetadataService.renameTable(
        currentRenamingTable.Name,
        newTableName,
        currentDataSourceFrom,
        currentRenamingTable.SchemaOwner || null
      );
      showInfo('Table renamed successfully');
      setShowRenamePopup(false);
      setDataModel(prev => ({
        ...prev,
        currentRenamingTable: null,
        newTableName: ''
      }));
      refresh();
    } catch (error: any) {
      showError(error.message || 'Failed to rename table');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel, dispatch, showError, showInfo, refresh]);

  // Preview table data
  const previewTableData = useCallback((selectedDataRow: any) => {
    if (!selectedDataRow || !selectedDataRow.Name) {
      // If no row provided, try to get from selected row
      if (dataModel.selectedDataRow && dataModel.selectedDataRow.Name) {
        selectedDataRow = dataModel.selectedDataRow;
      } else {
        return;
      }
    }

    if (selectedDataRow.Name && dataModel.currentDataSourceFrom) {
      setPreviewTableInfo({
        tableName: selectedDataRow.Name,
        dataSourceRegisterId: dataModel.currentDataSourceFrom,
        schemaOwner: selectedDataRow.SchemaOwner || null
      });
      setShowPreviewPopup(true);
    }
    closeResultItemContextMenu();
  }, [dataModel.selectedDataRow, dataModel.currentDataSourceFrom, closeResultItemContextMenu]);

  // Toggle filter by application
  const toggleFilterByApplication = useCallback((filter: boolean) => {
    setDataModel(prev => ({ ...prev, isFilterByApplicationId: filter }));
    setTimeout(() => {
      loadTablesByDataSourceFrom(true);
    }, 100);
  }, [loadTablesByDataSourceFrom]);

  // Handle horizontal resize (left-right divider)
  const handleHorizontalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingHorizontal(true);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleHorizontalResize = useCallback((e: MouseEvent) => {
    if (!isResizingHorizontal) return;
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      const newWidth = e.clientX - containerRect.left;
      if (newWidth >= 200 && newWidth <= containerRect.width - 200) {
        setLeftPanelWidth(newWidth);
      }
    }
  }, [isResizingHorizontal]);

  const handleHorizontalResizeEnd = useCallback(() => {
    setIsResizingHorizontal(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Handle vertical resize (top-bottom divider)
  const handleVerticalResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizingVertical(true);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const handleVerticalResize = useCallback((e: MouseEvent) => {
    if (!isResizingVertical) return;
    const container = document.querySelector('.main-content-area') as HTMLElement;
    if (container) {
      const containerRect = container.getBoundingClientRect();
      const containerBottom = containerRect.bottom;
      const newHeight = containerBottom - e.clientY;
      if (newHeight >= 100 && newHeight <= containerRect.height - 100) {
        setQueryResultHeight(newHeight);
      }
    }
  }, [isResizingVertical]);

  const handleVerticalResizeEnd = useCallback(() => {
    setIsResizingVertical(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  }, []);

  // Set up resize event listeners
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

  // Initialize
  useEffect(() => {
    loadDataFromServer();
  }, []);

  // Update query text when data source changes
  useEffect(() => {
    if (dataModel.currentDataSourceFrom && ddlDataSourceFromRef.current) {
      ddlDataSourceFromRef.current.selectedValue = '';
      setTimeout(() => {
        if (ddlDataSourceFromRef.current) {
          ddlDataSourceFromRef.current.selectedValue = dataModel.currentDataSourceFrom;
        }
      }, 0);
    }
  }, [dataModel.currentDataSourceFrom]);

  // Close add menu when clicking outside
  useEffect(() => {
    if (showAddMenu) {
      const handleClickOutside = (e: MouseEvent) => {
        const target = e.target as HTMLElement;
        if (!target.closest('.add-menu-container') && !target.closest('button[title="Add Table or View"]')) {
          closeAddMenu();
        }
      };
      document.addEventListener('click', handleClickOutside);
      return () => {
        document.removeEventListener('click', handleClickOutside);
      };
    }
  }, [showAddMenu, closeAddMenu]);

  useRefineContextMenuPosition(showContextMenu, contextMenuRef, setContextMenuPosition);
  useRefineContextMenuPosition(showAddMenu, addMenuRef, setAddMenuPosition);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      {/* Header Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>SQL Workbench</div>
        <div className="flex items-center space-x-2">
          {dataModel.dataSourceRegisterList.length >= 2 && dataModel.dataSourceFromCV && (
            <div className="flex items-center gap-2 mr-2">
              <span className="text-xs">Datasource:</span>
              <div style={{ width: '180px' }}>
                <ComboBox
                  itemsSource={dataModel.dataSourceFromCV}
                  displayMemberPath="DataSourceName"
                  selectedValuePath="Id"
                  selectedValue={dataModel.currentDataSourceFrom}
                  isRequired={false}
                  initialized={initializeDataSourceFromDdl}
                  style={{ height: '24px', fontSize: '11px' }}
                />
              </div>
            </div>
          )}

          {applicationId && (
            <div className="flex items-center gap-1 mr-2">
              <span className="text-xs">Filter:</span>
              <button
                onClick={() => toggleFilterByApplication(true)}
                className={`px-2 py-1 text-xs rounded ${dataModel.isFilterByApplicationId ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
              >
                By App
              </button>
              <button
                onClick={() => toggleFilterByApplication(false)}
                className={`px-2 py-1 text-xs rounded ${!dataModel.isFilterByApplicationId ? 'bg-blue-500 text-white' : 'bg-gray-200'}`}
              >
                All
              </button>
            </div>
          )}

          
        </div>
      </div>

      {/* Main Content Area */}
      <div className={`w-full h-[200px] flex-auto overflow-hidden flex main-content-area`}>
        {/* Left Panel - Tables and Views */}
        <div className={`flex flex-col border-r ${theme.mainContentSection}`} style={{ width: `${leftPanelWidth}px`, minWidth: '200px' }}>
          <div className={`px-2 py-1 border-b ${t('border_mainContentSection')}`}>
            <div className="flex items-center justify-between">
              <h3 className={`text-xs font-semibold ${theme.title}`}>Tables and Views</h3>
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => loadTablesByDataSourceFrom(true)}
                  className="w-6 h-5 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                  title="Reload Tables From Database"
                >
                  <i className="fa-solid fa-rotate"></i>
                </button>
                <button
                  onClick={openAddMenu}
                  className="w-6 h-5 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600"
                  title="Add Table or View"
                >
                  <i className="fa-solid fa-plus"></i>
                </button>
              </div>

            </div>
          </div>

          <div className="h-1 flex-auto overflow-hidden p-2">
            {dataModel.databaseSchemaData && (
              <FlexGrid
                ref={flexGridSchemaRef}
                itemsSource={dataModel.databaseSchemaData}
                isReadOnly={true}
                selectionMode="Row"
                style={{ height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
              >
                <FlexGridFilter />
                <FlexGridColumn header="Edit" width={50}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      if (!cell.item) {
                        return <div></div>;
                      }
                      return (
                        <div className="absolute inset-0 flex items-center justify-center">
                          <button
                            onClick={(e) => openResultItemContextMenu(e, cell.item)}
                            className={`${theme.menu_default}`}
                            style={{ width: '30px' }}
                            title="More Options"
                          >
                            <i className="fa-solid fa-pencil" aria-hidden="true" style={{ fontSize: '12px' }}></i>
                            <i className="fa-solid fa-bars" aria-hidden="true" style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }}></i>
                          </button>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn
                  width="*"
                  header="Drag Table To Query"
                  binding="Name"
                >
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      if (!cell.item) {
                        return <div></div>;
                      }
                      return (
                        <div
                          className={`flex items-center justify-between p-1 cursor-move ${t('bg_default_hover')}`}
                          draggable
                          onDragStart={(e) => handleTableDragStart(e, cell.item)}
                          onClick={() => setCurrentSourceTableObj(cell.item)}
                          onDoubleClick={() => clickTableNameTokenButton(cell.item.Name)}
                          title={cell.item.uiDisplayName || cell.item.Name}
                        >
                          <span className="w-1 flex-auto truncate">{cell.item.Name}</span>
                          <div className="flex gap-1">
                            <button
                              onClick={(e) => openTableColumnPopup(e, cell.item)}
                              className="p-1 text-green-600 hover:bg-green-100"
                              title="Show Table Columns"
                            >
                              <i className="fa-solid fa-table"></i>
                            </button>
                            <button
                              onClick={() => previewTableData(cell.item)}
                              className="p-1 text-green-600 hover:bg-green-100"
                              title="Preview Table Data"
                            >
                              <i className="fa-solid fa-eye"></i>
                            </button>
                          </div>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="ObjType" header="Type" width={0} visible={false} />
              </FlexGrid>
            )}
          </div>
        </div>

        {/* Horizontal Resizer */}
        <div
          onMouseDown={handleHorizontalResizeStart}
          className="w-1 cursor-col-resize hover:bg-blue-400 transition-colors flex-shrink-0"
          style={{ backgroundColor: isResizingHorizontal ? '#60a5fa' : 'transparent' }}
          title="Drag to resize"
        >
          <div className="w-full h-full flex items-center justify-center">
            <div className={`w-1 h-12 border-l border-r ${t('border_mainContentSection')}`}></div>
          </div>
        </div>

        {/* Right Panel - Query Editor and Results */}
        <div className="w-1 flex-auto flex flex-col overflow-hidden" style={{ width: `calc(100% - ${leftPanelWidth}px - 4px)` }}>
          {/* Query Editor  */}
          <div className={`w-full h-1 flex-auto flex flex-col ${theme.mainContentSection}`}>
            {/* Query Editor Toolbar */}
            <div className={`px-2 py-1 border-b ${t('border_mainContentSection')}`}>
              <div className="flex items-center justify-between">
                <h3 className={`text-xs font-semibold ${theme.title}`}>Query</h3>
                <div className="flex items-center space-x-2">
                  <button
                    onClick={executeQuery}
                    className="w-6 h-5 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500"
                    title="Execute Query"
                  >
                    <i className="fa-solid fa-bolt"></i>
                  </button>
                  <button
                    onClick={() => {
                      if (dataModel.currentDataSourceFrom) {
                        setViewDesignInfo({
                          viewName: null,
                          dataSourceRegisterId: dataModel.currentDataSourceFrom,
                          schemaOwner: null,
                          applicationId: applicationId,
                          isNewView: true
                        });
                        setShowViewDesignPopup(true);
                      }
                    }}
                    disabled={!dataModel.currentDataSourceFrom}
                    className="w-6 h-5 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Design Query"
                  >
                    <i className="fa-solid fa-pen-to-square"></i>
                  </button>
                </div>
              </div>
            </div>

            {/* Query Editor Body */}
            <div className="h-1 flex-auto w-full p-2 overflow-hidden">
              <textarea
                ref={queryTextAreaRef}
                value={dataModel.queryText}
                onChange={(e) => setDataModel(prev => ({ ...prev, queryText: e.target.value }))}
                onDrop={handleQueryEditorDrop}
                onDragOver={handleQueryEditorDragOver}
                className={`w-full h-full p-2 border rounded font-mono text-xs ${theme.inputBox} focus:outline-none`}
                placeholder="Enter SQL query here... (Drag tables/columns here)"
                style={{ resize: 'none' }}
              />
            </div>
          </div>
          {/* Vertical Resizer */}
          <div
            onMouseDown={handleVerticalResizeStart}
            className="h-1 cursor-row-resize hover:bg-blue-400 transition-colors flex-shrink-0"
            style={{ backgroundColor: isResizingVertical ? '#60a5fa' : 'transparent' }}
            title="Drag to resize"
          >
            <div className="h-full w-full flex items-center justify-center">
              <div className={`h-1 w-12 border-t border-b ${t('border_mainContentSection')}`}></div>
            </div>
          </div>

          {/* Query Results */}
          <div className={`flex flex-col ${theme.mainContentSection}`} style={{ height: `${queryResultHeight}px`, minHeight: '100px' }}>
            <div className={`px-2 py-1 border-b ${theme.mainContentSection}`}>
              <h3 className={`text-xs font-semibold ${theme.title}`}>
                Query Result ({dataModel.queryResult.length} Rows)
              </h3>
            </div>
            <div className="h-1 flex-auto overflow-hidden">
              {dataModel.queryResult.length > 0 && (
                <FlexGrid
                  ref={flexQueryResultRef}
                  itemsSource={new CollectionView(dataModel.queryResult)}
                  isReadOnly={true}
                  style={{ height: '100%', borderLeft: 'none', borderRight: 'none', borderTop: 'none', borderBottom: 'none' }}
                >
                  <FlexGridFilter />
                </FlexGrid>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Add Menu */}
      {showAddMenu && (
        <div
          ref={addMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px] add-menu-container`}
          style={{ left: addMenuPosition.x, top: addMenuPosition.y }}
          onMouseLeave={closeAddMenu}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            onClick={() => {
              closeAddMenu();
              if (dataModel.currentDataSourceFrom) {
                setTableDesignInfo({
                  tableName: null,
                  dataSourceRegisterId: dataModel.currentDataSourceFrom,
                  schemaOwner: null,
                  applicationId: applicationId
                });
                setShowTableDesignPopup(true);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-table mr-2"></i> Add Table
          </button>
          <button
            onClick={() => {
              closeAddMenu();
              if (dataModel.currentDataSourceFrom) {
                setViewDesignInfo({
                  viewName: null,
                  dataSourceRegisterId: dataModel.currentDataSourceFrom,
                  schemaOwner: null,
                  applicationId: applicationId,
                  isNewView: true
                });
                setShowViewDesignPopup(true);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!dataModel.currentDataSourceFrom}
          >
            <i className="fa-solid fa-eye mr-2"></i> Add View
          </button>
        </div>
      )}

      {/* Context Menu */}
      {showContextMenu && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{ left: contextMenuPosition.x, top: contextMenuPosition.y }}
          onMouseLeave={closeResultItemContextMenu}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Select !== undefined) {
                insertBuiltInQuery(dataModel.selectedDataRow, emAppBuiltInQueryType.Select);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!emAppBuiltInQueryType?.Select}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Select
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Update !== undefined) {
                insertBuiltInQuery(dataModel.selectedDataRow, emAppBuiltInQueryType.Update);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!emAppBuiltInQueryType?.Update}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Update
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Insert !== undefined) {
                insertBuiltInQuery(dataModel.selectedDataRow, emAppBuiltInQueryType.Insert);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!emAppBuiltInQueryType?.Insert}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Insert
          </button>
          <button
            onClick={() => {
              if (emAppBuiltInQueryType?.Delete !== undefined) {
                insertBuiltInQuery(dataModel.selectedDataRow, emAppBuiltInQueryType.Delete);
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!emAppBuiltInQueryType?.Delete}
          >
            <i className="fa-solid fa-file-lines mr-2"></i> Query: Delete
          </button>
          <div className="border-t my-1"></div>
          <button
            onClick={() => {
              if (dataModel.selectedDataRow && dataModel.currentDataSourceFrom) {
                closeResultItemContextMenu();
                // Check IsDbView property (matching AngularJS designTableOrView logic)
                if (dataModel.selectedDataRow.IsDbView) {
                  // For views, open view design
                  setViewDesignInfo({
                    viewName: dataModel.selectedDataRow.Name,
                    dataSourceRegisterId: dataModel.currentDataSourceFrom,
                    schemaOwner: dataModel.selectedDataRow.SchemaOwner || null,
                    applicationId: applicationId,
                    isNewView: false
                  });
                  setShowViewDesignPopup(true);
                } else {
                  // For tables, open table design
                  setTableDesignInfo({
                    tableName: dataModel.selectedDataRow.Name,
                    dataSourceRegisterId: dataModel.currentDataSourceFrom,
                    schemaOwner: dataModel.selectedDataRow.SchemaOwner || null,
                    applicationId: applicationId
                  });
                  setShowTableDesignPopup(true);
                }
              }
            }}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
            disabled={!dataModel.selectedDataRow || !dataModel.currentDataSourceFrom}
          >
            <i className="fa-solid fa-pencil mr-2"></i> Alter
          </button>
          <button
            onClick={() => dropTable(dataModel.selectedDataRow)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-trash mr-2"></i> Drop
          </button>
          <button
            onClick={() => renameTable(dataModel.selectedDataRow)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-pencil mr-2"></i> Rename
          </button>
          <div className="border-t my-1"></div>
          <button
            onClick={() => previewTableData(dataModel.selectedDataRow)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center ${t('bg_default_hover')}`}
          >
            <i className="fa-solid fa-eye mr-2"></i> Preview Data
          </button>
        </div>
      )}

      {/* Rename Table Popup */}
      {showRenamePopup && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className={`rounded p-6 w-[350px] ${theme.mainContentSection}`}>
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold">Rename Table</h3>
              <button onClick={() => setShowRenamePopup(false)} className="text-xl">&times;</button>
            </div>
            <div className="mb-4">
              <label className="block mb-2">Original Name</label>
              <input
                type="text"
                value={dataModel.currentRenamingTable?.Name || ''}
                readOnly
                className={`w-full p-2 border rounded ${theme.inputBox}`}
              />
            </div>
            <div className="mb-4">
              <label className="block mb-2">New name</label>
              <input
                type="text"
                value={dataModel.newTableName}
                onChange={(e) => setDataModel(prev => ({ ...prev, newTableName: e.target.value }))}
                className={`w-full p-2 border rounded ${theme.inputBox}`}
              />
            </div>
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setShowRenamePopup(false)}
                className={`px-4 w-24 py-1 rounded border ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                onClick={renameOk}
                className={`px-4 w-24 py-1 rounded border ${theme.button_default}`}
              >
                Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Table Column Selector Popup */}
      {showColumnSelector && dataModel.columnSelectorTableDto && (
        <div 
          className="fixed inset-0 z-50 w-[320px] h-[500px]"
          style={{ pointerEvents: 'none' }}
        >
          <div 
            className={`column-popup-container rounded p-4 w-[320px] h-[500px] flex flex-col ${theme.mainContentSection}`}
            style={{ 
              pointerEvents: 'auto',
              boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06), 0 0 0 1px rgba(0, 0, 0, 0.1)',
              position: 'fixed',
              left: `${columnPopupPosition.x}px`,
              top: `${columnPopupPosition.y}px`,
              cursor: isDraggingPopup ? 'grabbing' : 'default'
            }}
          >
            <div 
              className="flex justify-between items-center mb-2 cursor-grab active:cursor-grabbing"
              onMouseDown={handlePopupHeaderMouseDown}
              draggable={false}
              title="Drag header to move popup"
            >
              <h3 className="text-sm font-semibold truncate w-1 flex-auto">
                Table: {dataModel.columnSelectorTableDto.Name}
              </h3>
              <div className="flex items-center gap-2">
                
                <button onClick={closeTableColumnPopup} className="text-xl">&times;</button>
              </div>
            </div>
            <div className="text-xs mb-2 p-2">
              Drag & Drop Selected Column To Query Editor
            </div>
            <div className="h-1 flex-auto overflow-hidden">
              {dataModel.tableColumnSelectorCV && (
                <FlexGrid
                  itemsSource={dataModel.tableColumnSelectorCV}
                  headersVisibility="Column"
                  isReadOnly={false}
                  style={{ height: '100%' }}
                  className="column-selector-grid"
                >
                  <FlexGridFilter filterColumns={['Name']} />
                  <FlexGridColumn 
                    width={40} 
                    header="" 
                    binding="isSelected" 
                    dataType="Boolean" 
                    allowSorting={false} 
                    isReadOnly={false}
                  >
                    <FlexGridCellTemplate
                      cellType="ColumnHeader"
                      template={() => (
                        <input
                          type="checkbox"
                          checked={dataModel.columnSelectorTableDto?.isSelectAllTableColumn || false}
                          onChange={(e) => {
                            if (dataModel.columnSelectorTableDto) {
                              dataModel.columnSelectorTableDto.isSelectAllTableColumn = e.target.checked;
                              isSelectAllTableColumnChanged();
                            }
                          }}
                          style={{ cursor: 'pointer' }}
                        />
                      )}
                    />
                  </FlexGridColumn>
                  <FlexGridColumn width="*" header="Available Columns" binding="Name" isReadOnly={true}>
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        if (!cell.item) {
                          return <div></div>;
                        }
                        const column = cell.item;
                        const isSelected = column.isSelected || false;
                        return (
                          <div
                            className={`flex items-center p-1 cursor-move ${t('bg_default_hover')} ${isSelected ? 'bg-blue-50' : ''}`}
                            draggable={true}
                            onDragStart={(e) => {
                              // Check if any columns are selected
                              const tableData = dataModel.columnSelectorTableDto;
                              const hasSelectedColumns = tableData?.Columns?.some((col: any) => col.isSelected) || false;
                              if (!hasSelectedColumns) {
                                e.preventDefault();
                                return;
                              }
                              handleColumnDragStart(e, column.Name);
                            }}
                            title={column.isSelected ? `Drag to insert all checked columns` : 'Select columns first, then drag any column to insert all checked columns'}
                          >
                            <span className="w-1 flex-auto">{column.Name}</span>
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                </FlexGrid>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Table Data Preview Popup */}
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

      {/* Table Design Popup */}
      {tableDesignInfo && (
        <MetaDataTableDesign
          isOpen={showTableDesignPopup}
          onClose={() => {
            setShowTableDesignPopup(false);
            setTableDesignInfo(null);
          }}
          onSave={(tableData: any, isNewTable: boolean) => {
            // Refresh tables list after save
            if (isNewTable) {
              loadTablesByDataSourceFrom(true);
            }
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
            // Refresh tables list after save
            loadTablesByDataSourceFrom(true);
          }}
          viewName={viewDesignInfo.viewName}
          dataSourceRegisterId={viewDesignInfo.dataSourceRegisterId}
          schemaOwner={viewDesignInfo.schemaOwner}
          applicationId={viewDesignInfo.applicationId}
          isNewView={viewDesignInfo.isNewView}
        />
      )}
    </div>
  );
};

export default MetaDataManagement;

