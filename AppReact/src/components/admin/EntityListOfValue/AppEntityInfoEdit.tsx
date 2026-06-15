/**
 * Entity Info Edit (Create From Database) – migrated from AngularJS appEntityInfoEditCtrl / AppEntityInfoEdit.cshtml.
 * Used by "Create From Database" from Entity List Of Value. Edit entity from a database table (System Define Table).
 * Table/View Name cascading (Angular initialTablesCV / changeAvailableTableFilterOption / loadAvailableTableData):
 * - Table list is for current datasource; when user changes filter (Show All / From Current Application) or reloads
 *   tables, selection is cleared so the dropdown matches the new list.
 * - When user selects a table in the dropdown, columns are loaded and column DDLs updated (onOwnerTableNameChange).
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../../webapi/adminsvc';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import TableDataPreview from '../../transaction/TableDataPreview';

const EmAppEntityType = { SystemDefineTable: 1, SimpleQuery: 3 };
const EmTableFilterByOption = { AllTable: 1, ByApplication: 3 };

function getOwnerTableName(schemaOwner: string | null | undefined, tableName: string | null | undefined): string {
  if (!tableName) return '';
  if (schemaOwner) return `${schemaOwner.toLowerCase()}.${tableName.toLowerCase()}`;
  return tableName;
}

interface RouteParam {
  id?: string | number | null;
  param1?: string | number | null;
  param2?: string | null;
}

interface TableItem {
  Name?: string;
  SchemaOwner?: string;
  ownerTableName?: string;
  ObjType?: string;
}

interface ColumnItem {
  Name?: string;
}

interface CurrentEntity {
  Id?: number;
  EntityCode?: string;
  Description?: string;
  EntityType?: number;
  TableName?: string;
  SchemaOwner?: string;
  ownerTableName?: string;
  DataSourceFrom?: number | null;
  SaasApplicationId?: number | string | null;
  IdentityField?: string;
  DisplayFiled1?: string;
  DisplayFiled2?: string;
  DisplayFiled3?: string;
  PartnerFilterFiled?: string;
  OtherSettingsDto?: {
    IdentityColumnDataType?: string;
    ListEditTransactionId?: number | null;
    ItemDetailFormTransactionId?: number | null;
  };
  IsModified?: boolean;
}

interface TransactionItem {
  Id: number;
  TransactionName?: string;
  TransactionOrganizedType?: number;
}

export interface AppEntityInfoEditProps {
  /** Optional: render inside DIV popup without changing URL. */
  paramOverride?: string;
}

const AppEntityInfoEdit: React.FC<AppEntityInfoEditProps> = ({ paramOverride }) => {
  const { param: routeParam } = useParams<{ param?: string }>();
  const param = paramOverride ?? routeParam;
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();

  const [paramObj, setParamObj] = useState<RouteParam>({});
  const [dataSourceList, setDataSourceList] = useState<any[]>([]);
  const [tableList, setTableList] = useState<TableItem[]>([]);
  const [tableColumns, setTableColumns] = useState<ColumnItem[]>([]);
  const [tableFilterOption, setTableFilterOption] = useState(EmTableFilterByOption.AllTable);
  const [currentEntity, setCurrentEntity] = useState<CurrentEntity>({
    EntityCode: '',
    Description: '',
    EntityType: EmAppEntityType.SystemDefineTable,
    DataSourceFrom: null,
    SaasApplicationId: null,
    IdentityField: '',
    DisplayFiled1: '',
    DisplayFiled2: '',
    DisplayFiled3: '',
    PartnerFilterFiled: '',
    OtherSettingsDto: {},
    IsModified: false
  });
  const [listEditTransactions, setListEditTransactions] = useState<TransactionItem[]>([]);
  const [itemDetailFormTransactions, setItemDetailFormTransactions] = useState<TransactionItem[]>([]);
  const [tableFilterDropdown, setTableFilterDropdown] = useState(false);
  const tableFilterRef = useRef<HTMLDivElement>(null);
  const tableDdlRef = useRef<any>(null);
  const onOwnerTableNameChangeRef = useRef<(ownerTableName: string) => void>(() => {});
  const [isLoading, setIsLoading] = useState(false);
  const [previewTableInfo, setPreviewTableInfo] = useState<{
    tableName: string;
    dataSourceRegisterId: number | null;
    schemaOwner: string | null;
  } | null>(null);

  const emAppDataType = useEnumValues('EmAppDataType');
  const dataTypeList = useMemo(() => {
    if (!emAppDataType) return [{ Id: 'Integer', Display: 'Integer' }, { Id: 'String', Display: 'String' }, { Id: 'Long', Display: 'Long' }, { Id: 'Guid', Display: 'Guid' }];
    return Object.entries(emAppDataType).map(([key, id]) => ({ Id: String(id), Display: key }));
  }, [emAppDataType]);

  useEffect(() => {
    let p: RouteParam = {};
    if (param) {
      try {
        p = JSON.parse(decodeURIComponent(param));
      } catch {
        p = { id: param };
      }
    }
    setParamObj(p);
  }, [param]);

  const entityInfoId = paramObj.id != null ? String(paramObj.id) : null;
  const dataSourceRegisterId = paramObj.param1 != null ? Number(paramObj.param1) : null;
  let applicationId: string | number | null = null;
  let initTableName: string | null = null;
  let initSchemaOwner: string | null = null;
  if (paramObj.param2) {
    try {
      const p2 = typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
      applicationId = p2?.applicationId ?? null;
      initTableName = p2?.initTableName ?? null;
      initSchemaOwner = p2?.initSchemaOwner ?? null;
    } catch {
      /* ignore */
    }
  }

  const databaseTablesCV = useMemo(() => {
    const list = tableList.map((t) => ({
      ...t,
      ownerTableName: t.ownerTableName ?? getOwnerTableName(t.SchemaOwner, t.Name)
    }));
    return new CollectionView(list);
  }, [tableList]);

  // Blank option so null/empty server value shows no selection instead of first item (Wijmo ComboBox).
  const columnListWithBlank = useMemo(() => [{ Name: '' as string }, ...tableColumns], [tableColumns]);
  const dataTypeListWithBlank = useMemo(() => [{ Id: '', Display: '' }, ...dataTypeList], [dataTypeList]);
  const listEditWithBlank = useMemo(() => [{ Id: null as number | null, TransactionName: '' }, ...listEditTransactions], [listEditTransactions]);
  const itemDetailFormWithBlank = useMemo(() => [{ Id: null as number | null, TransactionName: '' }, ...itemDetailFormTransactions], [itemDetailFormTransactions]);

  // Each column dropdown needs its own CollectionView so Wijmo doesn't share currentItem (rule: Wijmo ComboBox Selection / shared itemsSource).
  const identityFieldColumnsCV = useMemo(() => new CollectionView(columnListWithBlank), [columnListWithBlank]);
  const displayField1ColumnsCV = useMemo(() => new CollectionView(columnListWithBlank), [columnListWithBlank]);
  const displayField2ColumnsCV = useMemo(() => new CollectionView(columnListWithBlank), [columnListWithBlank]);
  const displayField3ColumnsCV = useMemo(() => new CollectionView(columnListWithBlank), [columnListWithBlank]);
  const partnerFilterColumnsCV = useMemo(() => new CollectionView(columnListWithBlank), [columnListWithBlank]);
  const dataSourceCV = useMemo(() => new CollectionView(dataSourceList), [dataSourceList]);
  const listEditTransactionCV = useMemo(() => new CollectionView(listEditWithBlank), [listEditWithBlank]);
  const itemDetailFormTransactionCV = useMemo(() => new CollectionView(itemDetailFormWithBlank), [itemDetailFormWithBlank]);
  const dataTypeCV = useMemo(() => new CollectionView(dataTypeListWithBlank), [dataTypeListWithBlank]);

  const loadTables = useCallback(
    async (dsId: number | null, forceRefresh: boolean) => {
      if (dsId == null) return;
      const filterByAppId: number | null =
        tableFilterOption === EmTableFilterByOption.ByApplication && applicationId != null ? Number(applicationId) : null;
      const data = forceRefresh
        ? await schemaMetadataService.getDataSourceTableAndViewList(dsId, tableFilterOption, filterByAppId, { bypassHttpCache: true })
        : await schemaMetadataService.getDataSourceTableAndViewListFromCache(dsId, tableFilterOption, filterByAppId);
      const arr = Array.isArray(data) ? data : [];
      const withOwner = arr.map((t: TableItem) => ({
        ...t,
        ownerTableName: getOwnerTableName(t.SchemaOwner, t.Name)
      }));
      setTableList(withOwner);
    },
    [tableFilterOption, applicationId]
  );

  /** Angular initialTablesCV: when table list is refreshed (filter/reload), clear Table/View Name and column fields so dropdown matches new list */
  const clearTableSelectionAndReloadTables = useCallback(
    (dsIdForLoad: number | null, newFilterOption?: number, forceRefresh?: boolean) => {
      if (newFilterOption != null) setTableFilterOption(newFilterOption);
      setCurrentEntity((prev) =>
        prev
          ? {
              ...prev,
              ownerTableName: '',
              TableName: '',
              SchemaOwner: '',
              IdentityField: '',
              DisplayFiled1: '',
              DisplayFiled2: '',
              DisplayFiled3: '',
              PartnerFilterFiled: '',
              IsModified: true
            }
          : prev
      );
      setTableColumns([]);
      loadTables(dsIdForLoad, forceRefresh === true);
    },
    [loadTables]
  );

  const removeTableDdlChangeHandler = useCallback(() => {
    const ctrl = tableDdlRef.current;
    if (ctrl?.selectedIndexChanged) ctrl.selectedIndexChanged.removeAllHandlers();
  }, []);

  /** Angular initialTableNameChangeEvent: attach selectedIndexChanged only after initial bind; called from loadData finally */
  const attachTableDdlChangeHandler = useCallback(() => {
    const ctrl = tableDdlRef.current;
    if (ctrl?.selectedIndexChanged) {
      ctrl.selectedIndexChanged.removeAllHandlers();
      ctrl.selectedIndexChanged.addHandler((s: any) => {
        const val = s?.selectedValue;
        if (val != null && val !== '') onOwnerTableNameChangeRef.current(val);
      });
    }
  }, []);

  const loadData = useCallback(async () => {
    removeTableDdlChangeHandler();
    dispatch(setIsBusy());
    setIsLoading(true);
    try {
      const [dsList, allTransactions] = await Promise.all([
        adminSvc.getDataSourceRegisterList(false),
        appTransactionService.retrieveAllAppTransactions(false, '', false)
      ]);
      const dsArr = Array.isArray(dsList) ? dsList : [];
      const txArr = Array.isArray(allTransactions) ? allTransactions : [];
      setDataSourceList(dsArr);
      setListEditTransactions(txArr.filter((t: TransactionItem) => t.TransactionOrganizedType === 3));
      setItemDetailFormTransactions(txArr.filter((t: TransactionItem) => t.TransactionOrganizedType === 1));
      const dsId = dataSourceRegisterId ?? dsArr[0]?.Id ?? null;

      if (entityInfoId) {
        const entityData = await adminSvc.retrieveOneAppEntityInfoExDto(entityInfoId, true);
        if (entityData) {
          // Wijmo ComboBox rule: set all DDL selected values to empty first so re-render doesn't select first item; then restore in setTimeout(0) (ConverterAngularJsPage / ApplicationSetting).
          const ownerTableNameValue = getOwnerTableName(entityData.SchemaOwner, entityData.TableName);
          const identityField = entityData.IdentityField ?? '';
          const displayFiled1 = entityData.DisplayFiled1 ?? '';
          const displayFiled2 = entityData.DisplayFiled2 ?? '';
          const displayFiled3 = entityData.DisplayFiled3 ?? '';
          const partnerFilterFiled = entityData.PartnerFilterFiled ?? '';
          const identityColumnDataType = entityData.OtherSettingsDto?.IdentityColumnDataType ?? '';
          const listEditTransactionId = entityData.OtherSettingsDto?.ListEditTransactionId ?? null;
          const itemDetailFormTransactionId = entityData.OtherSettingsDto?.ItemDetailFormTransactionId ?? null;
          setCurrentEntity((prev) => ({
            ...prev,
            ...entityData,
            DataSourceFrom: entityData.DataSourceFrom ?? dsId,
            SaasApplicationId: entityData.SaasApplicationId ?? applicationId,
            ownerTableName: '',
            IdentityField: '',
            DisplayFiled1: '',
            DisplayFiled2: '',
            DisplayFiled3: '',
            PartnerFilterFiled: '',
            OtherSettingsDto: {
              ...entityData.OtherSettingsDto,
              IdentityColumnDataType: '',
              ListEditTransactionId: null,
              ItemDetailFormTransactionId: null
            }
          }));
          if (entityData.TableName && entityData.DataSourceFrom != null) {
            const tableSchema = await schemaMetadataService.getOneDatabaseTableSchema(
              entityData.TableName,
              entityData.DataSourceFrom,
              entityData.SchemaOwner ?? null
            );
            if (tableSchema?.Columns) setTableColumns(tableSchema.Columns);
            setTimeout(() => {
              setCurrentEntity((prev) =>
                prev
                  ? {
                      ...prev,
                      IdentityField: identityField,
                      DisplayFiled1: displayFiled1,
                      DisplayFiled2: displayFiled2,
                      DisplayFiled3: displayFiled3,
                      PartnerFilterFiled: partnerFilterFiled,
                      OtherSettingsDto: {
                        ...prev.OtherSettingsDto,
                        IdentityColumnDataType: identityColumnDataType,
                        ListEditTransactionId: listEditTransactionId,
                        ItemDetailFormTransactionId: itemDetailFormTransactionId
                      }
                    }
                  : prev
              );
            }, 0);
          } else {
            // No table schema (e.g. Query type): still restore DDL values in next tick.
            setTimeout(() => {
              setCurrentEntity((prev) =>
                prev
                  ? {
                      ...prev,
                      IdentityField: identityField,
                      DisplayFiled1: displayFiled1,
                      DisplayFiled2: displayFiled2,
                      DisplayFiled3: displayFiled3,
                      PartnerFilterFiled: partnerFilterFiled,
                      OtherSettingsDto: {
                        ...prev.OtherSettingsDto,
                        IdentityColumnDataType: identityColumnDataType,
                        ListEditTransactionId: listEditTransactionId,
                        ItemDetailFormTransactionId: itemDetailFormTransactionId
                      }
                    }
                  : prev
              );
            }, 0);
          }
          await loadTables(entityData.DataSourceFrom ?? dsId, false);
          // Restore Table/View Name selection after list is loaded so Wijmo doesn't show first item (same ComboBox rule).
          setTimeout(() => {
            setCurrentEntity((prev) => (prev ? { ...prev, ownerTableName: ownerTableNameValue } : prev));
          }, 0);
        }
      } else {
        setCurrentEntity((prev) => ({
          ...prev,
          EntityType: EmAppEntityType.SystemDefineTable,
          DataSourceFrom: dsId,
          SaasApplicationId: applicationId,
          ownerTableName: '',
          TableName: '',
          SchemaOwner: '',
          IdentityField: '',
          DisplayFiled1: '',
          DisplayFiled2: '',
          DisplayFiled3: ''
        }));
        await loadTables(dsId, false);
        if (initTableName && dsId != null) {
          const tableSchema = await schemaMetadataService.getOneDatabaseTableSchema(initTableName, dsId, initSchemaOwner ?? null);
          if (tableSchema?.Columns) {
            setTableColumns(tableSchema.Columns);
            setCurrentEntity((prev) => ({
              ...prev,
              TableName: tableSchema.Name,
              SchemaOwner: tableSchema.SchemaOwner ?? '',
              ownerTableName: getOwnerTableName(tableSchema.SchemaOwner, tableSchema.Name)
            }));
          }
        }
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
      setTimeout(attachTableDdlChangeHandler, 200);
    }
  }, [entityInfoId, dataSourceRegisterId, applicationId, initTableName, initSchemaOwner, dispatch, showError, loadTables, removeTableDdlChangeHandler, attachTableDdlChangeHandler]);

  useEffect(() => {
    if (paramObj.param1 !== undefined || paramObj.id !== undefined) {
      loadData();
    }
  }, [loadData, paramObj.param1, paramObj.id]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (tableFilterDropdown && tableFilterRef.current && !tableFilterRef.current.contains(e.target as Node)) {
        setTableFilterDropdown(false);
      }
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [tableFilterDropdown]);

  const onOwnerTableNameChange = useCallback(
    async (ownerTableName: string) => {
      const tableObj = tableList.find((t) => (t.ownerTableName ?? getOwnerTableName(t.SchemaOwner, t.Name)) === ownerTableName);
      if (!tableObj || !currentEntity.DataSourceFrom) return;
      // Cascade: clear column list and selections first so column DDLs refresh (Wijmo ComboBox itemsSource change)
      setTableColumns([]);
      setCurrentEntity((prev) =>
        prev ? { ...prev, ownerTableName, TableName: tableObj.Name ?? '', SchemaOwner: tableObj.SchemaOwner ?? '', IdentityField: '', DisplayFiled1: '', DisplayFiled2: '', DisplayFiled3: '', PartnerFilterFiled: '', IsModified: true } : prev
      );
      try {
        const schema = await schemaMetadataService.getOneDatabaseTableSchema(tableObj.Name ?? '', currentEntity.DataSourceFrom, tableObj.SchemaOwner ?? null);
        const columns = schema?.Columns ?? [];
        setTableColumns(columns);
        // Wijmo ComboBox rule: after itemsSource (column list) updates, re-apply empty selection in next tick so DDLs don't show first item
        setTimeout(() => {
          setCurrentEntity((prev) =>
            prev
              ? {
                  ...prev,
                  IdentityField: '',
                  DisplayFiled1: '',
                  DisplayFiled2: '',
                  DisplayFiled3: '',
                  PartnerFilterFiled: ''
                }
              : prev
          );
        }, 0);
      } catch {
        setTableColumns([]);
      }
    },
    [tableList, currentEntity.DataSourceFrom]
  );

  onOwnerTableNameChangeRef.current = onOwnerTableNameChange;

  /** Angular: only store control ref on init; do NOT attach selectedIndexChanged yet so first bind does not fire and clear child DDLs */
  const initializeTableDdl = useCallback((sender: any) => {
    const ctrl = sender?.control ?? sender;
    tableDdlRef.current = ctrl;
  }, []);

  const onSelectTableFromGrid = useCallback(
    (tableObj: TableItem) => {
      const ownerName = tableObj.ownerTableName ?? getOwnerTableName(tableObj.SchemaOwner, tableObj.Name);
      if (ownerName) onOwnerTableNameChange(ownerName);
    },
    [onOwnerTableNameChange]
  );

  const previewTableData = useCallback(
    (tableObj: TableItem) => {
      if (!tableObj?.Name) {
        showError('Please select a table to preview.');
        return;
      }
      const dsId = currentEntity.DataSourceFrom ?? dataSourceRegisterId ?? null;
      if (dsId == null) {
        showError('Datasource is required to preview table data.');
        return;
      }
      setPreviewTableInfo({
        tableName: tableObj.Name,
        dataSourceRegisterId: dsId,
        schemaOwner: tableObj.SchemaOwner ?? null
      });
    },
    [currentEntity.DataSourceFrom, dataSourceRegisterId, showError]
  );

  const handleSave = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const payload = { ...currentEntity };
      const data = await adminSvc.saveOneAppEntityInfoDto(payload);
      if (data?.IsSuccessful && data?.Object) {
        setCurrentEntity((prev) => ({ ...prev, ...data.Object, ownerTableName: getOwnerTableName(data.Object?.SchemaOwner, data.Object?.TableName) }));
        if (data.Object?.Id != null) setParamObj((p) => ({ ...p, id: data.Object.Id }));
      }
      if (data?.ValidationResult) showValidationMessages(data.ValidationResult, true);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentEntity, dispatch, showError, showValidationMessages]);

  const dsId = currentEntity.DataSourceFrom ?? dataSourceRegisterId ?? dataSourceList[0]?.Id ?? null;
  const showLeftPanel = currentEntity.EntityType === EmAppEntityType.SystemDefineTable;
  /** Key so column DDLs remount when table/columns change; Wijmo ComboBox does not always refresh itemsSource when prop changes */
  const columnDdlKey = `${currentEntity.ownerTableName ?? ''}-${tableColumns.length}`;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Entity Info Edit</div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={() => loadData()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save
          </button>
        </div>
      </div>
      <div className="flex min-h-0 h-1 flex-auto overflow-hidden">
        {/* Left panel: Dragging Table To Edit Panel */}
        {showLeftPanel && (
          <div className={`w-[350px] flex-shrink-0 flex flex-col overflow-hidden mr-1 ${theme.mainContentSection}`}>
            <div className={`flex items-center justify-between px-2 py-1.5`}>
              <span className={`text-sm font-medium ${theme.title}`}>Dragging Table To Edit Panel</span>
              <div className="flex items-center gap-1">
                {applicationId && (
                  <div className="relative" ref={tableFilterRef}>
                    <button
                      type="button"
                      onClick={() => setTableFilterDropdown(!tableFilterDropdown)}
                      className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                      title="Filter Table By Application"
                    >
                      <i className="fa-solid fa-bars" aria-hidden />
                    </button>
                    {tableFilterDropdown && (
                      <div className={`absolute right-0 mt-1 py-1 min-w-[180px] rounded shadow-lg z-50 border ${theme.mainContentSection}`} onClick={(e) => e.stopPropagation()}>
                        <button type="button" onClick={() => { clearTableSelectionAndReloadTables(dsId, EmTableFilterByOption.AllTable); setTableFilterDropdown(false); }} className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}>Show All {tableFilterOption === EmTableFilterByOption.AllTable && <i className="fa-solid fa-check ml-1" aria-hidden />}</button>
                        <button type="button" onClick={() => { clearTableSelectionAndReloadTables(dsId, EmTableFilterByOption.ByApplication); setTableFilterDropdown(false); }} className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu}`}>From Current Application {tableFilterOption === EmTableFilterByOption.ByApplication && <i className="fa-solid fa-check ml-1" aria-hidden />}</button>
                      </div>
                    )}
                  </div>
                )}
                <button type="button" onClick={() => clearTableSelectionAndReloadTables(dsId, undefined, true)} className={`px-2 py-1 text-xs rounded ${theme.button_default}`} title="Reload Tables From Database">
                  <i className="fa-solid fa-database text-[11px]" aria-hidden /><i className="fa-solid fa-rotate text-[10px] ml-0.5 relative top-0.5" aria-hidden />
                </button>
              </div>
            </div>
            <div className="min-h-0 h-1 flex-auto overflow-hidden">
              <FlexGrid
                className="w-full h-full"
                itemsSource={databaseTablesCV}
                selectionMode="Row"
                isReadOnly
                headersVisibility="Column"
                selectionChanged={(s: any) => {
                  const flex = s?.control ?? s;
                  const row = flex?.selection?.row;
                  if (row != null && flex?.rows?.[row]) {
                    const item = flex.rows[row].dataItem as TableItem;
                    onSelectTableFromGrid(item);
                  }
                }}
              >
                <FlexGridFilter />
                <FlexGridColumn header="Table Name" width="*" isReadOnly>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell.item as TableItem;
                      const ownerName = item?.ownerTableName ?? getOwnerTableName(item?.SchemaOwner, item?.Name);
                      return (
                        <div className="flex items-center w-full">
                          <div className="w-1 flex-auto overflow-hidden truncate" title={ownerName}>{item?.Name ?? ''}</div>
                          <button type="button" onClick={(e) => { e.stopPropagation(); previewTableData(item); }} className={`${theme.button_default} w-7 h-6 flex items-center justify-center flex-shrink-0`} title="Preview Table Data">
                            <i className="fa-solid fa-eye text-xs" aria-hidden />
                          </button>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
              </FlexGrid>
            </div>
          </div>
        )}
        {/* Right panel: Entity Information */}
        <div className={`min-w-0 h-full w-1 flex-auto flex flex-col overflow-auto ${theme.mainContentSection}`}>
          <div className={`px-3 py-1.5`}>
            <span className={`text-sm font-medium ${theme.title}`}>Entity Information</span>
          </div>
          <div className="flex gap-5 px-3 py-2 flex-wrap">
            {/* Column 1: label w-32, control w-52 for alignment */}
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Entity Code</label>
                <input type="text" value={currentEntity.EntityCode ?? ''} onChange={(e) => setCurrentEntity((p) => (p ? { ...p, EntityCode: e.target.value, IsModified: true } : p))} className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`} />
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Description</label>
                <input type="text" value={currentEntity.Description ?? ''} onChange={(e) => setCurrentEntity((p) => (p ? { ...p, Description: e.target.value, IsModified: true } : p))} className={`w-52 h-7 px-2 text-xs border ${theme.inputBox}`} />
              </div>
              <div className="flex items-center gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Entity Type</label>
                <ComboBox itemsSource={new CollectionView([{ Id: EmAppEntityType.SystemDefineTable, Display: 'Database Table' }, { Id: EmAppEntityType.SimpleQuery, Display: 'Query' }])} displayMemberPath="Display" selectedValuePath="Id" selectedValue={currentEntity.EntityType} isDisabled isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
              </div>
              {showLeftPanel && (
                <div className="flex items-center gap-2">
                  <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Table/View Name</label>
                  <div className="flex w-52">
                    <ComboBox itemsSource={databaseTablesCV} displayMemberPath="ownerTableName" selectedValuePath="ownerTableName" selectedValue={currentEntity.ownerTableName ?? ''} initialized={initializeTableDdl} isRequired={false} className={`min-w-0 w-1 flex-auto ${theme.inputBox}`} />
                    <button type="button" onClick={() => currentEntity.ownerTableName && previewTableData(tableList.find((t) => (t.ownerTableName ?? getOwnerTableName(t.SchemaOwner, t.Name)) === currentEntity.ownerTableName)!)} className={`w-7 h-7 shrink-0 border border-l-0 flex items-center justify-center ${theme.inputBox}`} title="Preview Table Data"><i className="fa-solid fa-eye text-xs" aria-hidden /></button>
                  </div>
                </div>
              )}
              <div className="flex items-center gap-2">
                <label className={`w-32 shrink-0 text-xs ${theme.label}`}>Datasource From</label>
                <ComboBox itemsSource={dataSourceCV} displayMemberPath="DataSourceName" selectedValuePath="Id" selectedValue={currentEntity.DataSourceFrom} isDisabled isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
              </div>
            </div>
            {/* Column 2: label w-36, control w-52 for alignment */}
            {showLeftPanel && (
              <div className="flex flex-col gap-2">
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Identity(Id) Field</label>
                  <ComboBox key={`identity-${columnDdlKey}`} itemsSource={identityFieldColumnsCV} displayMemberPath="Name" selectedValuePath="Name" selectedValue={currentEntity.IdentityField ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, IdentityField: s?.selectedValue ?? '', IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Id Field Data Type</label>
                  <ComboBox itemsSource={dataTypeCV} displayMemberPath="Display" selectedValuePath="Display" selectedValue={currentEntity.OtherSettingsDto?.IdentityColumnDataType ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, OtherSettingsDto: { ...p.OtherSettingsDto, IdentityColumnDataType: s?.selectedValue ?? '' }, IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Display Filed 1</label>
                  <ComboBox key={`d1-${columnDdlKey}`} itemsSource={displayField1ColumnsCV} displayMemberPath="Name" selectedValuePath="Name" selectedValue={currentEntity.DisplayFiled1 ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, DisplayFiled1: s?.selectedValue ?? '', IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Display Filed 2</label>
                  <ComboBox key={`d2-${columnDdlKey}`} itemsSource={displayField2ColumnsCV} displayMemberPath="Name" selectedValuePath="Name" selectedValue={currentEntity.DisplayFiled2 ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, DisplayFiled2: s?.selectedValue ?? '', IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Display Filed 3</label>
                  <ComboBox key={`d3-${columnDdlKey}`} itemsSource={displayField3ColumnsCV} displayMemberPath="Name" selectedValuePath="Name" selectedValue={currentEntity.DisplayFiled3 ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, DisplayFiled3: s?.selectedValue ?? '', IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Partner Filter Field</label>
                  <ComboBox key={`partner-${columnDdlKey}`} itemsSource={partnerFilterColumnsCV} displayMemberPath="Name" selectedValuePath="Name" selectedValue={currentEntity.PartnerFilterFiled ?? ''} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, PartnerFilterFiled: s?.selectedValue ?? '', IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>List Edit Data Model</label>
                  <ComboBox itemsSource={listEditTransactionCV} displayMemberPath="TransactionName" selectedValuePath="Id" selectedValue={currentEntity.OtherSettingsDto?.ListEditTransactionId ?? null} isDisabled isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
                <div className="flex items-center gap-2">
                  <label className={`w-36 shrink-0 text-xs ${theme.label}`}>Item Info Data Model</label>
                  <ComboBox itemsSource={itemDetailFormTransactionCV} displayMemberPath="TransactionName" selectedValuePath="Id" selectedValue={currentEntity.OtherSettingsDto?.ItemDetailFormTransactionId ?? null} selectedIndexChanged={(s: any) => { if (isLoading) return; setCurrentEntity((p) => (p ? { ...p, OtherSettingsDto: { ...p.OtherSettingsDto, ItemDetailFormTransactionId: s?.selectedValue ?? null }, IsModified: true } : p)); }} isRequired={false} className={`w-52 h-7 ${theme.inputBox}`} />
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
      {previewTableInfo && (
        <TableDataPreview
          isOpen={!!previewTableInfo}
          onClose={() => setPreviewTableInfo(null)}
          tableName={previewTableInfo.tableName}
          dataSourceRegisterId={previewTableInfo.dataSourceRegisterId}
          schemaOwner={previewTableInfo.schemaOwner}
          recordLimit={100}
        />
      )}
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-black/10">
          <div className="busyLoader w-12 h-12" />
        </div>
      )}
    </div>
  );
};

export default AppEntityInfoEdit;
