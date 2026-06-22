/**
 * Excel table import settings – Angular parity: toolbar (Refresh, Import New Table From Excel, Filter),
 * grid columns, row menu (Open setting, View data, Update from Excel, Delete).
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { searchSvc } from '../../webapi/searchSvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { uploadFileToDataImage } from '../../webapi/dataImageUploadSvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import TableDataPreview from '../transaction/TableDataPreview';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 240;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 280;

/** Schema owner for staging table lookup / GetOneDatabaseTableSchema. */
async function resolveStagingTableSchemaOwner(dataSourceRegisterId: number, tempTableName: string): Promise<string> {
  try {
    const list = await schemaMetadataService.getDataSourceTableAndViewList(dataSourceRegisterId, null, null, {
      bypassHttpCache: true,
    });
    if (!Array.isArray(list)) return '';
    const key = tempTableName.toLowerCase();
    const row = list.find((o: any) => String(o?.Name ?? '').toLowerCase() === key);
    return row?.SchemaOwner != null ? String(row.SchemaOwner) : '';
  } catch {
    return '';
  }
}

const EM_IMPORT_STATUS_DRAFT = 1;
const EM_IMPORT_STATUS_RELEASED = 2;

/** Match backend ExcelImportExportBL: sanitized stem for default target table name. */
function sanitizeFileStemToTableBaseName(fileName: string): string {
  const base = fileName.replace(/\.[^.]+$/i, '');
  const s = base.replace(/[^0-9a-zA-Z]+/g, '_').replace(/^_+|_+$/g, '');
  return s || 'Import';
}

function columnExDtoFromSchemaColumn(col: any, tableName: string): any {
  return {
    Name: col.Name,
    TableName: tableName,
    DbDataType: col.DbDataType ?? 'nvarchar',
    DataType: col.DataType ?? col.Tag ?? 'String',
    Nullable: col.Nullable !== false,
    Length: col.Length,
    Precision: col.Precision,
    Scale: col.Scale,
    IsPrimaryKey: !!col.IsPrimaryKey,
    IsForeignKey: !!col.IsForeignKey,
    IsAutoNumber: !!col.IsAutoNumber,
    SchemaOwner: col.SchemaOwner ?? '',
    NetName: col.NetName,
    Tag: col.Tag,
    DefaultValue: col.DefaultValue,
    IsLogicKey: false,
  };
}

/** Initial AppDataSetExDto for a new Excel staging import (UsageType ExcelTableImportSetting = 4). */
function buildNewExcelTableImportAppDataSet(arg: {
  fileName: string;
  tempTableName: string;
  dataSourceRegisterId: number;
  schemaOwner: string;
  dbTable: any;
  columns: any[];
}): any {
  const targetTableName = sanitizeFileStemToTableBaseName(arg.fileName);
  const sourceColumns = arg.columns.map((c) => columnExDtoFromSchemaColumn(c, arg.tempTableName));
  const orgSourceColumns = JSON.parse(JSON.stringify(sourceColumns));

  const targetColumns = arg.columns.map((c) => columnExDtoFromSchemaColumn(c, targetTableName));
  // Angular parity: include a technical identity PK even when UI focuses on business columns.
  targetColumns.unshift({
    Name: `${targetTableName}Id`,
    TableName: targetTableName,
    DbDataType: 'int',
    DataType: null,
    Tag: 'Integer',
    Nullable: false,
    Length: null,
    Precision: null,
    Scale: null,
    IsPrimaryKey: true,
    IsForeignKey: false,
    IsAutoNumber: true,
    IsIdentity: true,
    IsLogicKey: false,
    SchemaOwner: arg.schemaOwner || arg.dbTable?.SchemaOwner || '',
    NetName: null,
    DefaultValue: null,
  });
  let logicSet = false;
  for (const tc of targetColumns) {
    if (!tc.IsPrimaryKey && !tc.IsForeignKey) {
      if (!logicSet) {
        tc.IsLogicKey = true;
        logicSet = true;
      }
    }
  }
  if (!logicSet && targetColumns.length > 0) {
    targetColumns[0].IsLogicKey = true;
  }

  const dictColMap: Record<string, string> = {};
  for (const tc of targetColumns) {
    if (!tc.IsPrimaryKey && !tc.IsForeignKey) {
      dictColMap[tc.Name] = tc.Name;
    }
  }

  const importName = `Import: [${arg.fileName}]`;
  const schemaOwn = arg.schemaOwner || arg.dbTable?.SchemaOwner || '';

  return {
    Name: importName,
    Description: '',
    UsageTypeId: 4,
    DataSourceFrom: arg.dataSourceRegisterId,
    QueryText: '',
    OtherSettingsDto: {
      DatabaseDiagramInfo: {
        IsErDiagram: true,
        DataSourceRegisterId: arg.dataSourceRegisterId,
        DictTables: {},
      },
      TableImportSettingDto: {
        Status: EM_IMPORT_STATUS_DRAFT,
        DataSourceRegisterId: arg.dataSourceRegisterId,
        ImportFileName: arg.fileName,
        TempTableName: arg.tempTableName,
        OrgTempTableName: arg.tempTableName,
        IsNeedToCreateImportApi: false,
        IsFlatSingleTableImport: false,
        SourceColumns: sourceColumns,
        OrgSourceColumns: orgSourceColumns,
        Tables: [
          {
            Name: targetTableName,
            Tag: 1,
            SchemaOwner: schemaOwn,
            IsNewTable: true,
            Columns: targetColumns,
            TransformCondition: '',
          },
        ],
        DictTableNameColumnNameAndSourceColumnNameMapping: {
          [targetTableName]: dictColMap,
        },
      },
    },
  };
}

function formatAppDate(v: unknown): string {
  if (v == null || v === '') return '';
  try {
    const d = typeof v === 'string' ? new Date(v) : v instanceof Date ? v : new Date(String(v));
    if (Number.isNaN(d.getTime())) return String(v);
    return d.toISOString().slice(0, 10);
  } catch {
    return String(v);
  }
}

function getDataSourceId(ds: any): number | null {
  const raw = ds?.DataSourceRegisterId ?? ds?.Id ?? null;
  if (raw == null || raw === '') return null;
  const n = Number(raw);
  return Number.isNaN(n) ? null : n;
}

function mapExcelListItem(raw: any): any {
  const tip = raw?.OtherSettingsDto?.TableImportSettingDto;
  const st = tip?.Status;
  const statusLabel = st === EM_IMPORT_STATUS_RELEASED ? 'Released' : st === EM_IMPORT_STATUS_DRAFT ? 'Draft' : '';
  const tables = tip?.Tables;
  const dataModel =
    Array.isArray(tables) && tables.length > 0
      ? tables
          .map((t: any) => t?.Name)
          .filter(Boolean)
          .join(', ')
      : '';
  return {
    ...raw,
    OriginalFile: tip?.ImportFileName ?? '',
    ImportStatus: statusLabel,
    DataModel: dataModel,
    DefaultTransactionId: tip?.DefaultTransactionId ?? null,
    ModifiedDateDisplay: formatAppDate(raw?.AppModifiedDate),
    CreatedDateDisplay: formatAppDate(raw?.AppCreatedDate),
  };
}

function getPrimaryImportedTableName(row: any): string | null {
  const tables = row?.OtherSettingsDto?.TableImportSettingDto?.Tables;
  if (!Array.isArray(tables) || tables.length === 0) return null;
  const n = tables[0]?.Name;
  return n ? String(n) : null;
}

function isImportReleased(row: any): boolean {
  const st = row?.OtherSettingsDto?.TableImportSettingDto?.Status;
  return st === EM_IMPORT_STATUS_RELEASED;
}

type FilterStatus = 'all' | 'draft' | 'released';

export type ExcelDataImportManagementProps = {
  isUsedAsSelector?: boolean;
  onConfirmSelection?: (item: any) => void;
  onRequestClose?: () => void;
  onOpenImportEditor?: (importSettingId: number) => void;
};

const ExcelDataImportManagement: React.FC<ExcelDataImportManagementProps> = (props) => {
  const { isUsedAsSelector = false, onConfirmSelection, onRequestClose, onOpenImportEditor } = props;
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const { showAlert } = useAlertConfirm();

  const [isLoading, setIsLoading] = useState(false);
  const [itemsCV, setItemsCV] = useState<CollectionView<any> | null>(null);
  const [dataSourceMap, setDataSourceMap] = useState<DataMap | null>(null);
  const [dataSourceList, setDataSourceList] = useState<any[]>([]);

  const [flatListOnly, setFlatListOnly] = useState(false);
  const [filterStatus, setFilterStatus] = useState<FilterStatus>('all');
  const [importDropdownOpen, setImportDropdownOpen] = useState(false);
  const [filterDropdownOpen, setFilterDropdownOpen] = useState(false);

  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [selectedRow, setSelectedRow] = useState<any | null>(null);

  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewTable, setPreviewTable] = useState<{ name: string; dataSourceId: number } | null>(null);

  const importDropdownRef = useRef<HTMLDivElement | null>(null);
  const filterDropdownRef = useRef<HTMLDivElement | null>(null);
  const newImportFileRef = useRef<HTMLInputElement | null>(null);
  const updateExcelFileRef = useRef<HTMLInputElement | null>(null);
  const pendingNewDataSourceIdRef = useRef<number | null>(null);
  const pendingUpdateRowRef = useRef<any | null>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [list, dsList] = await Promise.all([
        appTransactionService.retrieveAllExcelTableImportSettingDto(flatListOnly),
        adminSvc.getDataSourceRegisterList(false),
      ]);
      const rows = (Array.isArray(list) ? list : []).map(mapExcelListItem);
      const cv = new CollectionView<any>(rows);
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      setItemsCV(cv);

      const safeDs = Array.isArray(dsList) ? dsList : [];
      setDataSourceList(safeDs);
      if (safeDs.length > 0) {
        setDataSourceMap(new DataMap(safeDs, 'Id', 'DataSourceName'));
      } else {
        setDataSourceMap(null);
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to load Excel import settings');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError, flatListOnly]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    if (!itemsCV) return;
    itemsCV.filter = (item: any) => {
      if (filterStatus === 'all') return true;
      if (filterStatus === 'draft') return item.ImportStatus === 'Draft';
      if (filterStatus === 'released') return item.ImportStatus === 'Released';
      return true;
    };
  }, [itemsCV, filterStatus]);

  useEffect(() => {
    const onDoc = (e: MouseEvent) => {
      const t = e.target as Node;
      if (importDropdownRef.current?.contains(t)) return;
      if (filterDropdownRef.current?.contains(t)) return;
      setImportDropdownOpen(false);
      setFilterDropdownOpen(false);
    };
    document.addEventListener('click', onDoc);
    return () => document.removeEventListener('click', onDoc);
  }, []);

  const closeContextMenu = useCallback(() => {
    setContextMenuOpen(false);
    setSelectedRow(null);
  }, []);

  const openContextMenu = useCallback((e: React.MouseEvent, item: any) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedRow(item);
    const { x, y } = clampContextMenuPosition(
      e.clientX,
      e.clientY,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setContextMenuPos({ x, y });
    setContextMenuOpen(true);
  }, []);

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenuOpen) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenuOpen, closeContextMenu]);

  useRefineContextMenuPosition(contextMenuOpen, contextMenuRef, setContextMenuPos);

  const openImportEditor = useCallback(
    (row: any) => {
      if (!row?.Id) return;
      const id = Number(row.Id);
      if (onOpenImportEditor) {
        onOpenImportEditor(id);
        return;
      }
      const label = row.Name || `Excel import (${id})`;
      addTabAndNavigate('excel-table-import-editor', label, { id }, true);
    },
    [addTabAndNavigate, onOpenImportEditor],
  );

  const confirmSelectionAndClose = useCallback(() => {
    const flex = flexRef.current;
    if (!flex || !onConfirmSelection) return;
    const rowIndex = flex.selection?.row;
    if (rowIndex == null || rowIndex < 0) return;
    const dataItem = flex.rows[rowIndex]?.dataItem;
    if (!dataItem?.Id) return;
    onConfirmSelection(dataItem);
    onRequestClose?.();
  }, [onConfirmSelection, onRequestClose]);

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const row = flex.selection?.row;
      if (row == null || row < 0) return;
      const dto = flex.rows[row]?.dataItem;
      if (dto?.Id) openImportEditor(dto);
    },
    [openImportEditor],
  );

  const contextOpenSetting = useCallback(() => {
    if (!selectedRow) {
      closeContextMenu();
      return;
    }
    const r = selectedRow;
    closeContextMenu();
    openImportEditor(r);
  }, [selectedRow, closeContextMenu, openImportEditor]);

  const contextViewImportedData = useCallback(() => {
    if (!selectedRow) {
      closeContextMenu();
      return;
    }
    const tableName = getPrimaryImportedTableName(selectedRow);
    const dsId = selectedRow.DataSourceFrom;
    if (!tableName || !dsId) {
      showWarning('No imported table is available to preview for this setting.');
      closeContextMenu();
      return;
    }
    setPreviewTable({ name: tableName, dataSourceId: dsId });
    setPreviewOpen(true);
    closeContextMenu();
  }, [selectedRow, closeContextMenu, showWarning]);

  const contextUpdateFromExcel = useCallback(() => {
    if (!selectedRow?.Id || !isImportReleased(selectedRow)) {
      showWarning('Update from Excel is only available when the import status is Released.');
      closeContextMenu();
      return;
    }
    if (!selectedRow.DataSourceFrom) {
      showWarning('Data source is missing for this import setting.');
      closeContextMenu();
      return;
    }
    pendingUpdateRowRef.current = selectedRow;
    closeContextMenu();
    updateExcelFileRef.current?.click();
  }, [selectedRow, closeContextMenu, showWarning]);

  const onUpdateExcelFileChange = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      e.target.value = '';
      const row = pendingUpdateRowRef.current;
      pendingUpdateRowRef.current = null;
      if (!file || !row?.Id || !row.DataSourceFrom) return;

      dispatch(setIsBusy());
      try {
        const uploadResult = await uploadFileToDataImage(file, {
          callingFrom: 'ImportExcelToDatabase',
          extraParams: {
            ImportSettingDataSetId: row.Id,
            DataSourceRegisterId: row.DataSourceFrom,
          },
        });
        const msg = String(uploadResult?.ResultMessage ?? '');
        const tempTable = uploadResult?.ExcelUploadTableName;
        if (!tempTable || msg.toLowerCase().includes('import failed') || msg.toLowerCase().includes('invalid excel')) {
          showError(msg || 'Excel upload failed.');
          return;
        }
        const upd = await schemaMetadataService.updateImportedTableDataFromTempTable(row.Id, tempTable, file.name);
        const updateOk = upd?.IsSuccessful === true || upd?.isSuccessful === true;
        if (updateOk) {
          showInfo('Data updated from Excel.');
          await loadData();
        } else {
          const err =
            upd?.ValidationResult?.Items?.[0]?.ErrorMessage ||
            upd?.ValidationResult?.ErrorMessage ||
            upd?.ValidationResult?.LocalizedResult ||
            'Update from Excel failed.';
          showWarning(err);
        }
      } catch (err: any) {
        showError(err?.message || 'Update from Excel failed.');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showError, showInfo, showWarning],
  );

  const contextDelete = useCallback(async () => {
    if (!selectedRow?.Id) {
      closeContextMenu();
      return;
    }
    const ok = window.confirm(`Confirm delete Excel import setting:\n${selectedRow.Name ?? ''} (${selectedRow.Id})`);
    if (!ok) {
      closeContextMenu();
      return;
    }
    dispatch(setIsBusy());
    try {
      const result = await searchSvc.deleteOneAppDataSetEntityDto(String(selectedRow.Id));
      if (result?.ValidationResult?.IsValid !== false) {
        showInfo('Deleted successfully.');
        await loadData();
      } else {
        const errorMsg = result?.ValidationResult?.Items?.[0]?.ErrorMessage || 'Failed to delete';
        showError(errorMsg);
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to delete');
    } finally {
      dispatch(setIsNotBusy());
      closeContextMenu();
    }
  }, [selectedRow, dispatch, loadData, showError, showInfo, closeContextMenu]);

  const startNewImportForDataSource = useCallback((dsId: number) => {
    pendingNewDataSourceIdRef.current = dsId;
    setImportDropdownOpen(false);
    setTimeout(() => newImportFileRef.current?.click(), 0);
  }, []);

  const onNewImportFileChange = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const file = e.target.files?.[0];
      e.target.value = '';
      const dsId = pendingNewDataSourceIdRef.current;
      pendingNewDataSourceIdRef.current = null;
      if (!file || dsId == null) return;

      let tempTable = '';
      let initSchema = '';

      dispatch(setIsBusy());
      try {
        const uploadResult = await uploadFileToDataImage(file, {
          callingFrom: 'ImportExcelToDatabase',
          extraParams: { DataSourceRegisterId: dsId },
        });
        const msg = String(uploadResult?.ResultMessage ?? '');
        tempTable = String(uploadResult?.ExcelUploadTableName ?? '');
        if (!tempTable || msg.toLowerCase().includes('import failed') || msg.toLowerCase().includes('invalid excel')) {
          showError(msg || 'Excel import failed.');
          return;
        }
        schemaMetadataService.clearTableListCache();
        initSchema = await resolveStagingTableSchemaOwner(dsId, tempTable);
        await loadData();
      } catch (err: any) {
        showError(err?.message || 'Import failed.');
        return;
      } finally {
        dispatch(setIsNotBusy());
      }

      try {
        await showAlert(
          `File ${file.name} has been uploaded for database table(s) import.\n\nPlease click "OK" to continue the import process.`,
          { title: 'Message' },
        );
      } catch {
        return;
      }

      dispatch(setIsBusy());
      try {
        const dbTable = await schemaMetadataService.getOneDatabaseTableSchema(tempTable, dsId, initSchema || null);
        const columns = dbTable?.Columns ?? dbTable?.columns;
        if (!Array.isArray(columns) || columns.length === 0) {
          showError('Could not read columns from the uploaded staging table. Use Refresh, then verify the staging table in SQL Workbench.');
          return;
        }
        const payload = buildNewExcelTableImportAppDataSet({
          fileName: file.name,
          tempTableName: tempTable,
          dataSourceRegisterId: dsId,
          schemaOwner: initSchema,
          dbTable,
          columns,
        });
        const saveResult = await searchSvc.saveOneAppDataSetEntityDto(payload);
        if (saveResult?.IsSuccessful && saveResult?.Object?.Id != null) {
          await loadData();
          openImportEditor({ Id: saveResult.Object.Id, Name: `Import Tables From File ${file.name}` });
        } else {
          const errMsg =
            saveResult?.ValidationResult?.Items?.[0]?.ErrorMessage ||
            saveResult?.ValidationResult?.LocalizedResult ||
            saveResult?.ValidationResult?.ErrorMessage ||
            'Could not create import setting.';
          showError(errMsg);
        }
      } catch (err: any) {
        showError(err?.message || 'Could not create import setting.');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showError, showAlert, openImportEditor],
  );

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <input
        ref={newImportFileRef}
        type="file"
        className="hidden"
        accept=".xlsx,.xls,.csv,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        onChange={onNewImportFileChange}
        aria-hidden
      />
      <input
        ref={updateExcelFileRef}
        type="file"
        className="hidden"
        accept=".xlsx,.xls,.csv,application/vnd.ms-excel,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        onChange={onUpdateExcelFileChange}
        aria-hidden
      />

      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {isUsedAsSelector ? 'Excel Table Import Setting Selector:' : 'Excel Data Import Setting Management:'}
        </div>
        <div className="flex items-center gap-2">
          {isUsedAsSelector ? (
            <button
              type="button"
              onClick={confirmSelectionAndClose}
              disabled={isLoading}
              className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:opacity-60 ${theme.button_default}`}
              title="Confirm Selection & Close"
            >
              <i className="fa-solid fa-check" aria-hidden />
              <span>Confirm Selection &amp; Close</span>
            </button>
          ) : null}
          <button
            type="button"
            onClick={() => loadData()}
            disabled={isLoading}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:opacity-60 ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh</span>
          </button>

          <div className="relative" ref={importDropdownRef}>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setImportDropdownOpen((v) => !v);
                setFilterDropdownOpen(false);
              }}
              disabled={isLoading || dataSourceList.length === 0}
              className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 disabled:opacity-60 ${theme.button_default}`}
              title="Import new table from Excel"
            >
              <i className="fa-solid fa-file-circle-plus" aria-hidden />
              <span>Import New Table From Excel</span>
              <i className="fa-solid fa-caret-down text-xs" aria-hidden />
            </button>
            {importDropdownOpen && (
              <div
                className={`absolute right-0 top-full mt-1 py-1 min-w-[220px] max-h-[400px] overflow-y-auto rounded-[4px] border shadow-lg z-20 text-xs ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <div className={`px-3 py-1.5 text-[10px] uppercase tracking-wide ${theme.label}`}>Target database</div>
                {dataSourceList.map((ds: any) => (
                  <button
                    key={getDataSourceId(ds) ?? String(ds?.DataSourceName ?? Math.random())}
                    type="button"
                    className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}
                    onClick={() => {
                      const dsId = getDataSourceId(ds);
                      if (dsId != null) startNewImportForDataSource(dsId);
                      else showWarning(`Invalid data source id for "${ds?.DataSourceName ?? 'Unknown'}".`);
                    }}
                  >
                    {ds.DataSourceName}
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="relative" ref={filterDropdownRef}>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setFilterDropdownOpen((v) => !v);
                setImportDropdownOpen(false);
              }}
              className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
              title="Filter list"
            >
              <i className="fa-solid fa-filter" aria-hidden />
              <span>Filter</span>
              <i className="fa-solid fa-caret-down text-xs" aria-hidden />
            </button>
            {filterDropdownOpen && (
              <div
                className={`absolute right-0 top-full mt-1 min-w-[220px] rounded-[4px] border shadow-lg z-20 py-1 text-xs ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <div className={`px-3 py-1.5 text-[10px] uppercase tracking-wide ${theme.label}`}>Import status</div>
                {(
                  [
                    { id: 'all' as const, label: 'All' },
                    { id: 'draft' as const, label: 'Draft only' },
                    { id: 'released' as const, label: 'Released only' },
                  ] as const
                ).map((opt) => (
                  <button
                    key={opt.id}
                    type="button"
                    className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} ${filterStatus === opt.id ? theme.tab_active : ''}`}
                    onClick={() => {
                      setFilterStatus(opt.id);
                      setFilterDropdownOpen(false);
                    }}
                  >
                    {opt.label}
                  </button>
                ))}
                <div className={`border-t my-1 ${theme.mainContentSection}`} />
                <div className={`px-3 py-1.5 text-[10px] uppercase tracking-wide ${theme.label}`}>List source</div>
                <button
                  type="button"
                  className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} ${!flatListOnly ? theme.tab_active : ''}`}
                  onClick={() => {
                    setFlatListOnly(false);
                    setFilterDropdownOpen(false);
                  }}
                >
                  All Excel import settings
                </button>
                <button
                  type="button"
                  className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} ${flatListOnly ? theme.tab_active : ''}`}
                  onClick={() => {
                    setFlatListOnly(true);
                    setFilterDropdownOpen(false);
                  }}
                >
                  Flat single-table imports only
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          itemsSource={itemsCV ?? undefined}
          autoGenerateColumns={false}
          selectionMode="Row"
          isReadOnly
          allowDelete={false}
          initialized={(flex: wjGrid.FlexGrid) => {
            flexRef.current = flex;
            flex.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(flex));
          }}
          className="w-full h-full !border-0"
        >
          <FlexGridFilter />
          <FlexGridColumn isReadOnly width={44} binding="">
            <FlexGridCellTemplate
              cellType="ColumnHeader"
              template={() => (
                <div className="flex items-center justify-center w-full">
                  <i className="fa-solid fa-gear text-xs opacity-70" aria-hidden title="Actions" />
                </div>
              )}
            />
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => (
                <div className="flex justify-center w-full">
                  <button
                    type="button"
                    className={theme.menu_default}
                    style={{ width: '30px' }}
                    title="Import setting actions"
                    onClick={(e) => openContextMenu(e, ctx.item)}
                  >
                    <i className="fa-solid fa-pencil text-xs" aria-hidden />
                    <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="Id" header="Id" width={72} />
          <FlexGridColumn binding="Name" header="Name" width={240} />
          <FlexGridColumn binding="Description" header="Description" width={180} />
          <FlexGridColumn binding="OriginalFile" header="Original File" width={200} />
          <FlexGridColumn binding="DataModel" header="Data Model" width={160}>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => {
                const item = ctx.item;
                const canOpen = item?.DefaultTransactionId && item?.SaasApplicationId;
                return (
                  <div className="truncate flex items-center gap-1">
                    <span className="truncate" title={item.DataModel || ''}>
                      {item.DataModel || ''}
                    </span>
                    {canOpen ? (
                      <button
                        type="button"
                        className={`shrink-0 text-[10px] px-1 rounded ${theme.button_default}`}
                        title="Open data model (form)"
                        onClick={(e) => {
                          e.stopPropagation();
                          addTabAndNavigate('application-form-builder', item.Name || 'Data model', {
                            id: item.SaasApplicationId,
                            transactionId: item.DefaultTransactionId,
                            defaultSectionCode: 'TransactionGraphicEditor',
                            isCreateNewItem: false,
                            transactionType: null,
                            dataSourceRegisterId: null,
                            isCreateDtoDataModel: false,
                            isCreateApiDataModel: false,
                            isCreateDataModelView: false,
                            modelName: item.Name ?? null,
                          }, true);
                        }}
                      >
                        <i className="fa-solid fa-external-link" aria-hidden />
                      </button>
                    ) : null}
                  </div>
                );
              }}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="ImportStatus" header="Import Status" width={100} />
          <FlexGridColumn
            binding="DataSourceFrom"
            header="Data Source From"
            width={140}
            dataMap={dataSourceMap ?? undefined}
          />
          <FlexGridColumn binding="ModifiedDateDisplay" header="Modified Date" width={120} />
          <FlexGridColumn binding="CreatedDateDisplay" header="Created Date" width={120} />
          <FlexGridColumn binding="" header="" width="*" isReadOnly />
        </FlexGrid>
      </div>

      {contextMenuOpen && selectedRow && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[240px]`}
          style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={contextOpenSetting}
          >
            <i className="fa-solid fa-gear w-4 shrink-0" aria-hidden /> Open Import Setting
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={contextViewImportedData}
          >
            <i className="fa-solid fa-magnifying-glass w-4 shrink-0" aria-hidden /> View Imported Table Data
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 ${
              !isImportReleased(selectedRow) ? 'opacity-50' : ''
            }`}
            onClick={contextUpdateFromExcel}
          >
            <i className="fa-solid fa-file-import w-4 shrink-0" aria-hidden /> Update Data From Excel
          </button>
          <div className={`border-t my-1 ${theme.mainContentSection}`} />
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 text-red-600 dark:text-red-400`}
            onClick={contextDelete}
          >
            <i className="fa-solid fa-trash w-4 shrink-0" aria-hidden /> Delete Import Setting
          </button>
        </div>
      )}

      {previewOpen && previewTable && (
        <TableDataPreview
          isOpen={previewOpen}
          onClose={() => {
            setPreviewOpen(false);
            setPreviewTable(null);
          }}
          tableName={previewTable.name}
          dataSourceRegisterId={previewTable.dataSourceId}
          schemaOwner={null}
          recordLimit={200}
        />
      )}

    </div>
  );
};

export default ExcelDataImportManagement;
