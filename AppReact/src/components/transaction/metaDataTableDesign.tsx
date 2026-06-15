import React, { useState, useEffect, useRef, useCallback } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import TableDataPreview from './TableDataPreview';

interface MetaDataTableDesignProps {
  isOpen: boolean;
  onClose: () => void;
  onSave?: (tableData: any, isNewTable: boolean) => void;
  tableName?: string | null;
  dataSourceRegisterId: number | null;
  schemaOwner?: string | null;
  applicationId?: number | null;
  /** When true, open the dialog in fullscreen mode by default */
  defaultFullscreen?: boolean;
  /** When false, Save will not auto-close the dialog */
  closeOnSave?: boolean;
}

const MetaDataTableDesign: React.FC<MetaDataTableDesignProps> = ({
  isOpen,
  onClose,
  onSave,
  tableName = null,
  dataSourceRegisterId,
  schemaOwner = null,
  applicationId = null,
  defaultFullscreen = false,
  closeOnSave = true
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const emAppDataType = useEnumValues('EmAppDataType');
  
  const flexGridColumnsRef = useRef<any>(null);
  const schemaOwnerComboBoxRef = useRef<wjInput.ComboBox | null>(null);
  const specialColumnsDropdownRef = useRef<HTMLDivElement>(null);
  const [showPreviewPopup, setShowPreviewPopup] = useState(false);
  const [previewTableInfo, setPreviewTableInfo] = useState<{ tableName: string; dataSourceRegisterId: number | null; schemaOwner: string | null } | null>(null);
  const [isSpecialColumnsDropdownOpen, setIsSpecialColumnsDropdownOpen] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState<boolean>(defaultFullscreen);

  const [dataModel, setDataModel] = useState<{
    dbtable: any | null;
    tableColumnCV: CollectionView | null;
    schemaOwnerCV: CollectionView | null;
    dictOrgnameOrgDataBasecolumn: { [key: string]: any };
    listDropDatabaseColumn: any[];
  }>({
    dbtable: null,
    tableColumnCV: null,
    schemaOwnerCV: null,
    dictOrgnameOrgDataBasecolumn: {},
    listDropDatabaseColumn: []
  });

  // Create data type map
  const dataTypeDataMap = React.useMemo(() => {
    // Enum dictionary is key->value; UI should display logical enum names (String, Integer, Decimal...).
    const defaultTypeList = ['String', 'Integer', 'Decimal', 'Date', 'Time', 'DateTime', 'Boolean', 'Blob']
      .map((name) => ({ Display: name, Value: name }));
    if (!emAppDataType) return new DataMap(defaultTypeList, 'Display', 'Display');

    const dataTypeList = Object.entries(emAppDataType)
      .filter(([key, value]) => isNaN(Number(key)) && typeof value === 'number')
      .map(([key]) => ({
        Display: key,
        Value: key
      }));

    if (dataTypeList.length === 0) {
      return new DataMap(defaultTypeList, 'Display', 'Display');
    }

    return new DataMap(dataTypeList, 'Display', 'Display');
  }, [emAppDataType]);

  // Initialize column collection view
  const initializeColumnCollectionView = useCallback((dbtable: any) => {
    if (dbtable && dbtable.Columns) {
      const cv = new CollectionView(dbtable.Columns);
      setDataModel(prev => ({ ...prev, tableColumnCV: cv }));
    }
  }, []);

  // Prepare new table data
  const prepareNewTableData = useCallback(async () => {
    const dbtable: any = {
      DataSourceRegisterId: dataSourceRegisterId,
      Name: 'aNewUnitName',
      ApplicationId: applicationId,
      Columns: [],
      isNewTable: true
    };

    // Ask user if they want auto-increment primary key
    const wantsAutoNumber = await showConfirm('Do you want to create a default primary key column of type integer with auto-increment?');
    
    if (wantsAutoNumber) {
      const pkColumn: any = {
        Name: 'Id',
        DbDataType: 'int',
        IsAutoNumber: true,
        IsPrimaryKey: true,
        Nullable: false,
        Tag: 'Integer'
      };
      dbtable.Columns.push(pkColumn);
      dbtable.PkColumn = pkColumn;
    }

    // Load schema owner list
    try {
      const schemaOwnerList = await schemaMetadataService.getDataBaseSchemaOwnerList(dataSourceRegisterId);
      if (schemaOwnerList && schemaOwnerList.length > 0) {
        const schemaOwnerCV = new CollectionView(schemaOwnerList);
        dbtable.SchemaOwner = schemaOwnerList[0];
        setDataModel(prev => ({
          ...prev,
          dbtable,
          schemaOwnerCV,
          dictOrgnameOrgDataBasecolumn: {},
          listDropDatabaseColumn: []
        }));
        initializeColumnCollectionView(dbtable);
      } else {
        setDataModel(prev => ({
          ...prev,
          dbtable,
          schemaOwnerCV: new CollectionView<string>([]),
          dictOrgnameOrgDataBasecolumn: {},
          listDropDatabaseColumn: []
        }));
        initializeColumnCollectionView(dbtable);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load schema owner list');
      setDataModel(prev => ({
        ...prev,
        dbtable,
        schemaOwnerCV: new CollectionView<string>([]),
        dictOrgnameOrgDataBasecolumn: {},
        listDropDatabaseColumn: []
      }));
      initializeColumnCollectionView(dbtable);
    }
  }, [dataSourceRegisterId, applicationId, showError, initializeColumnCollectionView]);

  // Prepare existing table data
  const prepareTableData = useCallback((tableData: any) => {
    if (!tableData) return;

    const dbtable = { ...tableData };
    dbtable.isNewTable = false;
    dbtable.OriginalTableName = dbtable.Name;
    
    const dictOrgnameOrgDataBasecolumn: { [key: string]: any } = {};
    
    if (dbtable.Columns) {
      dbtable.Columns.forEach((column: any) => {
        column.orgName = column.Name;
        dictOrgnameOrgDataBasecolumn[column.Name] = { ...column };
      });
    }

    setDataModel(prev => ({
      ...prev,
      dbtable,
      dictOrgnameOrgDataBasecolumn,
      listDropDatabaseColumn: []
    }));
    initializeColumnCollectionView(dbtable);
  }, [initializeColumnCollectionView]);

  // Load data from server
  const loadDataFromServer = useCallback(async () => {
    if (!dataSourceRegisterId) return;

    try {
      dispatch(setIsBusy());
      
      // Load schema owner list
      const schemaOwnerList = await schemaMetadataService.getDataBaseSchemaOwnerList(dataSourceRegisterId);
      const schemaOwnerCV = new CollectionView(schemaOwnerList || []);
      
      if (tableName) {
        // Load existing table
        const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
          tableName,
          dataSourceRegisterId,
          schemaOwner || null
        );
        if (tableData) {
          tableData.SchemaOwner = schemaOwner || (schemaOwnerList && schemaOwnerList[0]) || null;
          prepareTableData(tableData);
        }
      } else {
        // Create new table
        await prepareNewTableData();
      }
      
      setDataModel(prev => ({ ...prev, schemaOwnerCV }));
    } catch (error: any) {
      showError(error.message || 'Failed to load table data');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [tableName, dataSourceRegisterId, schemaOwner, dispatch, showError, prepareNewTableData, prepareTableData]);

  // Load data when component opens
  useEffect(() => {
    if (isOpen && dataSourceRegisterId) {
      loadDataFromServer();
    }
  }, [isOpen, dataSourceRegisterId, loadDataFromServer]);

  // Handle click outside for special columns dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (specialColumnsDropdownRef.current && !specialColumnsDropdownRef.current.contains(event.target as Node)) {
        setIsSpecialColumnsDropdownOpen(false);
      }
    };

    if (isSpecialColumnsDropdownOpen) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [isSpecialColumnsDropdownOpen]);

  // Add new column
  const addColumn = useCallback(() => {
    if (dataModel.dbtable && dataModel.tableColumnCV) {
      const newColumn: any = {
        Nullable: true,
        Tag: 'String',
        DbDataType: 'nvarchar',
        Length: 255
      };
      dataModel.dbtable.Columns.push(newColumn);
      dataModel.tableColumnCV.refresh();
    }
  }, [dataModel]);

  // Delete selected columns
  const deleteColumn = useCallback(async () => {
    if (!dataModel.dbtable || !flexGridColumnsRef.current) return;

    const flex = flexGridColumnsRef.current.control;
    if (!flex) return;
    
    // Get selected rows - with CellRange mode, selectedRows should work
    let selectedRows: any[] = [];
    
    if (flex.selectedRows) {
      // Convert to array if it's a collection
      if (Array.isArray(flex.selectedRows)) {
        selectedRows = flex.selectedRows;
      } else if (flex.selectedRows.length !== undefined) {
        // It's a collection-like object (wijmo collection)
        for (let i = 0; i < flex.selectedRows.length; i++) {
          selectedRows.push(flex.selectedRows[i]);
        }
      }
    }
    
    // If no rows from selectedRows, try getting from selection range (CellRange mode)
    if (selectedRows.length === 0 && flex.selection) {
      const sel = flex.selection;
      if (sel && flex.rows) {
        // Get unique row indices from the selection range
        const rowIndices = new Set<number>();
        
        // If it's a range, get all rows in the range
        if (sel.topRow !== undefined && sel.bottomRow !== undefined) {
          for (let r = sel.topRow; r <= sel.bottomRow; r++) {
            if (r >= 0 && r < flex.rows.length) {
              rowIndices.add(r);
            }
          }
        } else if (sel.row !== undefined && sel.row !== null) {
          rowIndices.add(sel.row);
        }
        
        // Convert row indices to row objects
        rowIndices.forEach((idx) => {
          const row = flex.rows[idx];
          if (row && row.dataItem) {
            selectedRows.push(row);
          }
        });
      }
    }
    
    if (selectedRows.length === 0) {
      showError('Please select columns to delete');
      return;
    }

    const columnNames: string[] = [];
    selectedRows.forEach((row: any) => {
      const dataItem = row.dataItem;
      if (dataItem && dataItem.Name) {
        columnNames.push(dataItem.Name);
      }
    });

    if (columnNames.length === 0) return;

    const message = `Please confirm to delete these columns:\n${columnNames.map(n => `  [${n}]`).join('\n')}.`;
    
    const confirmed = await showConfirm(message, { title: 'Delete Columns' });
    if (confirmed) {
      const listDropDatabaseColumn: any[] = [];
      
      selectedRows.forEach((row: any) => {
        const dataItem = row.dataItem;
        if (dataItem.orgName) {
          listDropDatabaseColumn.push(dataItem);
        }
        if (dataModel.tableColumnCV) {
          dataModel.tableColumnCV.remove(dataItem);
        }
        const index = dataModel.dbtable.Columns.findIndex((col: any) => col === dataItem);
        if (index >= 0) {
          dataModel.dbtable.Columns.splice(index, 1);
        }
      });

      setDataModel(prev => ({
        ...prev,
        listDropDatabaseColumn: [...prev.listDropDatabaseColumn, ...listDropDatabaseColumn]
      }));
    }
  }, [dataModel, showError, showConfirm]);

  // Add Created By and Modified By columns
  const addCreatedByAndModifiedByColumns = useCallback(() => {
    if (!dataModel.dbtable || !dataModel.tableColumnCV) return;

    const columns = dataModel.dbtable.Columns;
    const hasAppCreatedByID = columns.some((col: any) => col.Name === 'AppCreatedByID');
    const hasAppCreatedDate = columns.some((col: any) => col.Name === 'AppCreatedDate');
    const hasAppModifiedByID = columns.some((col: any) => col.Name === 'AppModifiedByID');
    const hasAppModifiedDate = columns.some((col: any) => col.Name === 'AppModifiedDate');

    if (!hasAppCreatedByID) {
      columns.push({
        Name: 'AppCreatedByID',
        DbDataType: 'int',
        Nullable: true,
        Tag: 'Integer'
      });
    }
    if (!hasAppCreatedDate) {
      columns.push({
        Name: 'AppCreatedDate',
        DbDataType: 'datetime',
        Nullable: true,
        Tag: 'DateTime'
      });
    }
    if (!hasAppModifiedByID) {
      columns.push({
        Name: 'AppModifiedByID',
        DbDataType: 'int',
        Nullable: true,
        Tag: 'Integer'
      });
    }
    if (!hasAppModifiedDate) {
      columns.push({
        Name: 'AppModifiedDate',
        DbDataType: 'datetime',
        Nullable: true,
        Tag: 'DateTime'
      });
    }

    dataModel.tableColumnCV?.refresh();
    setIsSpecialColumnsDropdownOpen(false);
  }, [dataModel]);

  // Add Secure User ID column
  const addSecureUserIdColumn = useCallback(() => {
    if (!dataModel.dbtable || !dataModel.tableColumnCV) return;

    const newColumn: any = {
      Name: 'SecurityUserID',
      DbDataType: 'int',
      Nullable: true,
      Tag: 'Integer'
    };
    dataModel.dbtable.Columns.push(newColumn);
    dataModel.tableColumnCV.refresh();
    setIsSpecialColumnsDropdownOpen(false);
  }, [dataModel]);

  // Handle cell edit beginning
  const handleCellEditBeginning = useCallback((sender: any, e: any) => {
    const col = sender.columns[e.col];
    const rowData = sender.rows[e.row].dataItem;

    if (!rowData.Tag) {
      if (col.binding === 'Length' || col.binding === 'Scale') {
        e.cancel = true;
      }
    } else {
      if (rowData.Tag === 'String') {
        if (col.binding === 'Scale' || col.binding === 'Precision') {
          e.cancel = true;
        }
      } else if (rowData.Tag === 'Integer') {
        if (col.binding === 'Length' || col.binding === 'Scale' || col.binding === 'Precision') {
          e.cancel = true;
        }
      } else if (rowData.Tag === 'Decimal') {
        if (col.binding === 'Length') {
          e.cancel = true;
        }
      } else {
        if (col.binding === 'Length' || col.binding === 'Scale' || col.binding === 'Precision') {
          e.cancel = true;
        }
      }
    }
  }, []);

  // Handle cell edit ended
  const handleCellEditEnded = useCallback((sender: any, e: any) => {
    const rowData = sender.rows[e.row].dataItem;
    if (e.col !== 0) { // If it's modified but not rename
      rowData.isModified = true;
    }
  }, []);

  // Save table
  const save = useCallback(async () => {
    if (!dataModel.dbtable) return;

    try {
      dispatch(setIsBusy());

      if (dataModel.dbtable.isNewTable) {
        // Create new table
        // WebAPI returns OperationCallResult<bool>: Object is success (true/false), not the table DTO.
        const result = await schemaMetadataService.createNewTable(dataModel.dbtable);
        const createdDto =
          result?.Object && typeof result.Object === 'object' ? result.Object : null;
        const createSucceeded = result?.Object === true || createdDto !== null;

        if (createSucceeded) {
          showInfo('Table created successfully');
          const tableSnapshot =
            createdDto != null
              ? {
                  ...dataModel.dbtable,
                  ...createdDto,
                  Name: createdDto.Name ?? dataModel.dbtable.Name,
                  SchemaOwner: createdDto.SchemaOwner ?? dataModel.dbtable.SchemaOwner
                }
              : { ...dataModel.dbtable };
          if (onSave) {
            onSave(tableSnapshot, true);
          }
          // After creating, switch to "existing table" mode so users can continue editing without reopening.
          prepareTableData(tableSnapshot);
          if (closeOnSave) {
            onClose();
          }
        } else if (result && result.ValidationResult) {
          showValidationMessages(result.ValidationResult);
        }
      } else {
        // Save modified table
        const schemaMetaDataDto: any = {
          OriginalTableName: dataModel.dbtable.OriginalTableName,
          NewTableName: dataModel.dbtable.Name,
          SchemaOwner: dataModel.dbtable.SchemaOwner,
          DataSourceRegisterId: dataSourceRegisterId,
          ListNewDatabaseColumn: [],
          ListPairAlterDatabaseColumn: [],
          DictOrgnameNewDataBasecolumn: {},
          ListDropDatabaseColumn: dataModel.listDropDatabaseColumn
        };

        dataModel.dbtable.Columns.forEach((column: any) => {
          if (column.orgName) {
            if (column.Name !== column.orgName) {
              // Need to rename column
              if (!schemaMetaDataDto.DictOrgnameNewDataBasecolumn) {
                schemaMetaDataDto.DictOrgnameNewDataBasecolumn = {};
              }
              schemaMetaDataDto.DictOrgnameNewDataBasecolumn[column.orgName] = column;
            } else if (column.isModified) {
              // Need to alter column
              const modifiedColumn = { ...column };
              modifiedColumn.Name = modifiedColumn.orgName;
              schemaMetaDataDto.ListPairAlterDatabaseColumn.push({
                Key: dataModel.dictOrgnameOrgDataBasecolumn[column.orgName],
                Value: modifiedColumn
              });
            }
          } else {
            // New column
            schemaMetaDataDto.ListNewDatabaseColumn.push(column);
          }
        });

        const result = await schemaMetadataService.saveModifiedTableSchema(schemaMetaDataDto);
        
        if (result && result.Object) {
          showInfo('Table saved successfully');
          prepareTableData(result.Object);
          if (onSave) {
            onSave(result.Object, false);
          }
          if (closeOnSave) {
            onClose();
          }
        } else if (result && result.ValidationResult) {
          showValidationMessages(result.ValidationResult);
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to save table');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataModel, dataSourceRegisterId, dispatch, showError, showInfo, showValidationMessages, onSave, onClose, prepareTableData, closeOnSave]);

  // Refresh
  const refresh = useCallback(() => {
    loadDataFromServer();
  }, [loadDataFromServer]);

  // Open SQL Workbench
  const openSqlWorkbench = useCallback(() => {
    if (dataModel.dbtable && dataModel.dbtable.Name) {
      // This would navigate to SQL Workbench with the table pre-selected
      // For now, just close and let parent handle navigation
      onClose();
    }
  }, [dataModel.dbtable, onClose]);

  // Preview table data
  const previewTableData = useCallback(() => {
    if (!dataModel.dbtable) {
      showError('Table data is not loaded');
      return;
    }
    
    if (!dataModel.dbtable.Name || dataModel.dbtable.Name.trim() === '') {
      showError('Please enter a table name before previewing data');
      return;
    }
    
    // Check if it's a new table that hasn't been saved yet
    if (dataModel.dbtable.isNewTable) {
      showError('Please save the table before previewing data');
      return;
    }
    
    // Check if table name is still the default placeholder
    if (dataModel.dbtable.Name === 'aNewUnitName') {
      showError('Please enter a valid table name before previewing data');
      return;
    }
    
    if (!dataSourceRegisterId) {
      showError('Data source is not available');
      return;
    }
    
    setPreviewTableInfo({
      tableName: dataModel.dbtable.Name,
      dataSourceRegisterId: dataSourceRegisterId,
      schemaOwner: dataModel.dbtable.SchemaOwner || null
    });
    setShowPreviewPopup(true);
  }, [dataModel.dbtable, dataSourceRegisterId, showError]);

  // Update DbDataType based on Tag
  useEffect(() => {
    if (dataModel.dbtable && dataModel.dbtable.Columns) {
      dataModel.dbtable.Columns.forEach((column: any) => {
        if (column.Tag) {
          switch (column.Tag) {
            case 'String':
              if (!column.DbDataType || column.DbDataType === 'int' || column.DbDataType === 'decimal') {
                column.DbDataType = 'nvarchar';
                if (!column.Length) column.Length = 255;
              }
              break;
            case 'Integer':
              column.DbDataType = 'int';
              column.Length = null;
              column.Precision = null;
              column.Scale = null;
              break;
            case 'Decimal':
              column.DbDataType = 'decimal';
              column.Length = null;
              if (!column.Precision) column.Precision = 18;
              if (!column.Scale) column.Scale = 2;
              break;
            case 'Date':
              column.DbDataType = 'date';
              column.Length = null;
              column.Precision = null;
              column.Scale = null;
              break;
            case 'DateTime':
              column.DbDataType = 'datetime';
              column.Length = null;
              column.Precision = null;
              column.Scale = null;
              break;
            case 'Boolean':
              column.DbDataType = 'bit';
              column.Length = null;
              column.Precision = null;
              column.Scale = null;
              break;
          }
        }
      });
      if (dataModel.tableColumnCV) {
        dataModel.tableColumnCV.refresh();
      }
    }
  }, [dataModel.dbtable, dataModel.tableColumnCV]);

  if (!isOpen) return null;

  return (
    <>
      <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10001]">
        <div
          className={`${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-lg'} shadow-xl border flex flex-col overflow-hidden`}
          style={isFullscreen 
            ? { width: '100vw', height: '100vh', position: 'fixed', top: 0, left: 0, zIndex: 10001 }
            : { width: '90vw', height: '90vh', maxWidth: '1600px', maxHeight: '900px' }
          }
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-t-lg'}`}>
            <h3 className={`text-lg font-semibold ${theme.title}`}>
              Table Design: {tableName ? tableName : 'New Table'}
            </h3>
            <div className="flex items-center gap-2">
              <button
                onClick={refresh}
                className="px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1"
                title="Refresh"
              >
                <i className="fa-solid fa-rotate"></i>
                <span className="hidden 2xl:inline">Refresh</span>
              </button>
              <button
                onClick={save}
                className="px-2 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center gap-1"
                title="Save"
              >
                <i className="fa-solid fa-save"></i>
                <span className="hidden 2xl:inline">Save</span>
              </button>
              <button
                onClick={addColumn}
                className="px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1"
                title="Add New Column"
              >
                <i className="fa-solid fa-plus"></i>
                <span className="hidden 2xl:inline">Add New Column</span>
              </button>
              <div className="relative" ref={specialColumnsDropdownRef}>
                <button
                  onClick={() => setIsSpecialColumnsDropdownOpen(!isSpecialColumnsDropdownOpen)}
                  className={`px-2 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center gap-1 ${isSpecialColumnsDropdownOpen ? 'bg-blue-600' : ''}`}
                  title="Add Special Columns"
                >
                  <i className="fa-solid fa-plus"></i>
                  <span className="hidden 2xl:inline">Add Special Columns</span>
                  <i className="fa-solid fa-caret-down text-[10px]"></i>
                </button>
                {isSpecialColumnsDropdownOpen && (
                  <div className={`absolute right-0 mt-1 w-56 rounded-[4px] shadow-lg ${theme.mainContentSection} border border-gray-300 z-50`}>
                    <div className="py-1" role="menu" aria-orientation="vertical">
                      <button
                        onClick={addCreatedByAndModifiedByColumns}
                        className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                        role="menuitem"
                      >
                        <span>Created By & Modified By</span>
                      </button>
                      <button
                        onClick={addSecureUserIdColumn}
                        className="flex w-full items-center px-4 py-2 text-xs hover:bg-gray-100 dark:hover:bg-gray-700"
                        role="menuitem"
                      >
                        <span>Secure UserID</span>
                      </button>
                    </div>
                  </div>
                )}
              </div>
              <button
                onClick={deleteColumn}
                className="px-2 h-6 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center gap-1"
                title="Delete Column"
              >
                <i className="fa-solid fa-trash"></i>
                <span className="hidden 2xl:inline">Delete Column</span>
              </button>
              <button
                onClick={openSqlWorkbench}
                className="px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1"
                title="Open On SQL Workbench"
              >
                <i className="fa-solid fa-database"></i>
                <span className="hidden 2xl:inline">Open On SQL Workbench</span>
              </button>
              <button
                onClick={previewTableData}
                className="px-2 h-6 bg-gray-400 text-white rounded-[4px] text-xs hover:bg-gray-500 flex items-center gap-1"
                title="Preview Data"
              >
                <i className="fa-solid fa-eye"></i>
                <span className="hidden 2xl:inline">Preview Data</span>
              </button>
              <div className="ml-4 flex items-center gap-1">
                <button
                  onClick={() => setIsFullscreen(!isFullscreen)}
                  className="text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center"
                  title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}
                >
                  <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>
                </button>
                <button
                  onClick={onClose}
                  className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
                  title="Close"
                >
                  &times;
                </button>
              </div>
            </div>
          </div>

          {/* Body */}
          <div className="h-1 flex-auto overflow-y-auto p-4 flex flex-col">
            {/* Table Info Form */}
            <div className="flex flex-wrap gap-4 mb-4">
              <div className="flex items-center gap-2">
                <label className="text-xs font-semibold w-24">Table Name:</label>
                <input
                  type="text"
                  value={dataModel.dbtable?.Name || ''}
                  onChange={(e) => {
                    if (dataModel.dbtable) {
                      setDataModel(prev => ({
                        ...prev,
                        dbtable: { ...prev.dbtable, Name: e.target.value }
                      }));
                    }
                  }}
                  disabled={!dataModel.dbtable?.isNewTable}
                  className={`px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                  style={{ width: '200px' }}
                  maxLength={200}
                />
              </div>
              <div className="flex items-center gap-2">
                <label className="text-xs font-semibold w-20">Schema:</label>
                {dataModel.schemaOwnerCV && (
                  <div style={{ width: '150px' }}>
                    <ComboBox
                      itemsSource={dataModel.schemaOwnerCV}
                      selectedValue={dataModel.dbtable?.SchemaOwner || ''}
                      selectedValueChanged={(sender: wjInput.ComboBox) => {
                        if (dataModel.dbtable) {
                          setDataModel(prev => ({
                            ...prev,
                            dbtable: { ...prev.dbtable, SchemaOwner: sender.selectedValue }
                          }));
                        }
                      }}
                      isRequired={false}
                      disabled={!!tableName}
                      style={{ height: '24px', fontSize: '11px' }}
                    />
                  </div>
                )}
              </div>
              {dataModel.dbtable?.isNewTable && dataModel.dbtable?.PkColumn && (
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    checked={dataModel.dbtable.PkColumn.IsAutoNumber || false}
                    onChange={(e) => {
                      if (dataModel.dbtable && dataModel.dbtable.PkColumn) {
                        setDataModel(prev => ({
                          ...prev,
                          dbtable: {
                            ...prev.dbtable!,
                            PkColumn: { ...prev.dbtable!.PkColumn, IsAutoNumber: e.target.checked }
                          }
                        }));
                      }
                    }}
                    className="cursor-pointer"
                  />
                  <label className="text-xs">Set Primary Key As Auto-Number</label>
                </div>
              )}
            </div>

            {/* Columns Grid */}
            <div className="h-1 flex-auto w-full">
              {dataModel.tableColumnCV && (
                <FlexGrid
                  ref={flexGridColumnsRef}
                  itemsSource={dataModel.tableColumnCV}
                  isReadOnly={false}
                  selectionMode="CellRange"
                  beginningEdit={handleCellEditBeginning}
                  cellEditEnded={handleCellEditEnded}
                  style={{ height: '100%', width: '100%' }}
                >
                  <FlexGridFilter />
                  <FlexGridColumn header="Column Name" binding="Name" width={200} isRequired={false} />
                  <FlexGridColumn header="IsPrimaryKey" binding="IsPrimaryKey" width={100} dataType="Boolean" isRequired={false} />
                  <FlexGridColumn header="Tag" binding="Tag" width={100} dataMap={dataTypeDataMap} isRequired={false} />
                  <FlexGridColumn header="Max Characters (Length)" binding="Length" width={180} dataType="Number" isRequired={false} />
                  <FlexGridColumn header="Total Digits (Precision)" binding="Precision" width={180} dataType="Number" isRequired={false} />
                  <FlexGridColumn header="Number of Decimal (Scale)" binding="Scale" width={180} dataType="Number" isRequired={false} />
                  <FlexGridColumn 
                    header="IsAutoNumber" 
                    binding="IsAutoNumber" 
                    width={100} 
                    dataType="Boolean"
                    isReadOnly={!dataModel.dbtable?.isNewTable}
                    isRequired={false}
                  />
                  <FlexGridColumn header="Nullable" binding="Nullable" width={100} dataType="Boolean" isRequired={false} />
                  <FlexGridColumn header="Default Value" binding="DefaultValue" width={150} isRequired={false} />
                  <FlexGridColumn header="DbDataType" binding="DbDataType" width={120} isReadOnly={true} isRequired={false} />
                  <FlexGridColumn width="*" header="" binding="" isReadOnly={true} isRequired={false} />
                </FlexGrid>
              )}
            </div>
          </div>
        </div>
      </div>

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
    </>
  );
};

export default MetaDataTableDesign;

