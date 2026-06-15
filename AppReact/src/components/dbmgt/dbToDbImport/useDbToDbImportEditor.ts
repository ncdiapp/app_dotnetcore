import { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch } from 'react-redux';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../../webapi/searchSvc';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { adminSvc } from '../../../webapi/adminsvc';
import {
  EM_DB2DB_SOURCE_DATABASE_TABLE,
  EM_IMPORT_STATUS_DRAFT,
  EM_IMPORT_STATUS_RELEASED,
  USAGE_DB2DB_TABLE_IMPORT,
  addOneColumnToTable,
  autoMapExistingTableColumns,
  columnFromSourceForNewTable,
  ensureDbToDbShape,
  executeRemoveOneTable,
  extractProcessError,
  filterImportTableColumns,
  getForeignMatrixKeyDisplay,
  hydrateImportSettingLevels,
  isSettingCommitted,
  prepareNewTableData,
  prepareSaveData,
  removeOneColumnFromTable,
  updateAllSourceColumnsSelectedStatus,
} from './dbToDbImportEditorModel';

export type TableSelectorContext = {
  level: number;
  parentTableDto: any | null;
  isFromUpdateMappingByFkTableSetting?: boolean;
};

export function useDbToDbImportEditor(dataSetId: number | null) {
  const dispatch = useDispatch();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const { addTabAndNavigate } = useTabNavigation();
  const emAppDataType = useEnumValues('EmAppDataType');
  const emSystemTokenField = useEnumValues('EmBLFiledMappingSystemTokenField');

  const [dto, setDto] = useState<any>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const [dictTableUiIdAndDto, setDictTableUiIdAndDto] = useState<Record<string, any>>({});
  const [currentEditTableUiId, setCurrentEditTableUiId] = useState<string | null>(null);
  const [dataSources, setDataSources] = useState<any[]>([]);
  const [allEntities, setAllEntities] = useState<any[]>([]);
  const [fromDataSetName, setFromDataSetName] = useState('');

  const [tableSelectorOpen, setTableSelectorOpen] = useState(false);
  const [tableSelectorCtx, setTableSelectorCtx] = useState<TableSelectorContext | null>(null);

  const [tableDesignOpen, setTableDesignOpen] = useState(false);
  const [tableDesignCtx, setTableDesignCtx] = useState<{
    level: number;
    parentTableDto: any | null;
    tableName: string | null;
  } | null>(null);

  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewTable, setPreviewTable] = useState<{
    tableName: string;
    dataSourceRegisterId: number | null;
    schemaOwner: string | null;
  } | null>(null);

  const [matrixPopupOpen, setMatrixPopupOpen] = useState(false);
  const [matrixOptions, setMatrixOptions] = useState<{ UiId: string; Name: string; isSelected: boolean }[]>([]);
  const [matrixTableDto, setMatrixTableDto] = useState<any | null>(null);

  const [fkMappingOpen, setFkMappingOpen] = useState(false);
  const [fkMappingCtx, setFkMappingCtx] = useState<any | null>(null);

  const [entityPopupOpen, setEntityPopupOpen] = useState(false);
  const [entitySelectionColumn, setEntitySelectionColumn] = useState<any | null>(null);

  const bump = useCallback(() => setRevision((r) => r + 1), []);

  const tip = dto?.OtherSettingsDto?.TableImportSettingDto;
  const levelOneTable = tip?.levelOneTable ?? null;
  const levelTwoTables: any[] = tip?.levelTwoTables ?? [];
  const levelThreeTables: any[] = tip?.levelThreeTables ?? [];
  const isMultiTable = !!tip?.IsSpilitToMultipleTables;
  const committed = isSettingCommitted(tip);
  const isFinalized = !!tip?.IsFinalized;
  const isDataImported = !!tip?.IsDataImported;
  const needToUpdateTransactionId = tip?.NeedToUpdateTransactionId ?? null;

  const dataTypeDataMap = useMemo(() => {
    const list = Object.entries(emAppDataType ?? {}).map(([Display, Id]) => ({ Display, Id }));
    return list.length ? new DataMap(list, 'Display', 'Display') : null;
  }, [emAppDataType]);

  const systemTokenDataMap = useMemo(() => {
    const list = Object.entries(emSystemTokenField ?? {}).map(([k, Id]) => ({ Display: k, Id }));
    return list.length ? new DataMap(list, 'Id', 'Display') : null;
  }, [emSystemTokenField]);

  const sourceColumnsCV = useMemo(() => {
    const src = tip?.SourceColumns;
    return new CollectionView(Array.isArray(src) ? src : []);
  }, [tip?.SourceColumns, revision]);

  const sourceColumnDataMap = useMemo(() => {
    const src = tip?.SourceColumns;
    return Array.isArray(src) && src.length ? new DataMap(src, 'Name', 'Name') : null;
  }, [tip?.SourceColumns, revision]);

  const getParentColumnDataMap = useCallback(
    (parentUiId: string | null | undefined) => {
      if (!parentUiId) return null;
      const parent = dictTableUiIdAndDto[parentUiId];
      if (!parent?.Columns?.length) return null;
      return new DataMap(parent.Columns, 'Name', 'Name');
    },
    [dictTableUiIdAndDto, revision],
  );

  const getEntityCodeById = useCallback(
    (id: string | number | null | undefined) => {
      if (id == null || id === '') return '';
      const ent = allEntities.find((e) => String(e.Id) === String(id));
      return ent?.EntityCode ?? ent?.Name ?? String(id);
    },
    [allEntities],
  );

  const dataSourceName = useCallback(
    (id: number | null | undefined) => {
      if (id == null) return '';
      const ds = dataSources.find((d) => Number(d.Id) === Number(id));
      return ds?.DataSourceName ?? String(id);
    },
    [dataSources],
  );

  const fromDatabaseName = tip?.SourceDataSourceFrom != null ? dataSourceName(tip.SourceDataSourceFrom) : '';
  const toDatabaseName = dto?.DataSourceFrom != null ? dataSourceName(dto.DataSourceFrom) : '';
  const fromTableName = tip?.SourceTableName ?? '';

  const loadData = useCallback(async () => {
    if (!dataSetId) {
      setLoadError('Missing import setting id.');
      setDto(null);
      return;
    }
    dispatch(setIsBusy());
    try {
      const [data, dsList, entities] = await Promise.all([
        searchSvc.retrieveOneAppDataSetExDto(String(dataSetId), false),
        adminSvc.getDataSourceRegisterList(false),
        adminSvc.retrieveAllAppEntityInfoDto(false),
      ]);
      setDataSources(Array.isArray(dsList) ? dsList : []);
      setAllEntities(Array.isArray(entities) ? entities : []);

      if (data && typeof data === 'object') {
        if (Number(data.UsageTypeId) !== USAGE_DB2DB_TABLE_IMPORT) {
          setLoadError(`This dataset is not a DB-to-DB import setting (UsageTypeId=${data.UsageTypeId ?? 'n/a'}).`);
          setDto(null);
          return;
        }
        const cloned = JSON.parse(JSON.stringify(data));
        ensureDbToDbShape(cloned);
        const tipInner = cloned.OtherSettingsDto.TableImportSettingDto;
        const hydrated = hydrateImportSettingLevels(tipInner);
        setDictTableUiIdAndDto(hydrated.dictTableUiIdAndDto);
        setCurrentEditTableUiId(hydrated.levelOneTable?.UiId ?? null);
        setLoadError(null);
        setDto(cloned);

        if (tipInner?.SourceDataSetId) {
          try {
            const srcDs = await searchSvc.retrieveOneAppDataSetExDto(String(tipInner.SourceDataSetId), false);
            setFromDataSetName(srcDs?.Name ?? String(tipInner.SourceDataSetId));
          } catch {
            setFromDataSetName(String(tipInner.SourceDataSetId));
          }
        } else {
          setFromDataSetName('');
        }
        bump();
      } else {
        setLoadError('Import setting not found.');
        setDto(null);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load import setting');
      setDto(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSetId, dispatch, showError, bump]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const saveDraftSetting = useCallback(async () => {
    if (!dto) return;
    dispatch(setIsBusy());
    try {
      const saveData = prepareSaveData(dto, dictTableUiIdAndDto);
      const result = await schemaMetadataService.saveDraftTableImportSetting(saveData);
      if (result?.IsSuccessful && result.Object) {
        const fresh = JSON.parse(JSON.stringify(result.Object));
        ensureDbToDbShape(fresh);
        const t = fresh.OtherSettingsDto.TableImportSettingDto;
        const hydrated = hydrateImportSettingLevels(t);
        setDictTableUiIdAndDto(hydrated.dictTableUiIdAndDto);
        setDto(fresh);
        bump();
        showInfo('Saved');
      } else {
        showWarning(extractProcessError(result));
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save setting');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dto, dictTableUiIdAndDto, dispatch, showError, showInfo, showWarning, bump]);

  const runImport = useCallback(
    async (finalize: boolean) => {
      if (!dto || needToUpdateTransactionId) return;
      const ok = await showConfirm(
        finalize
          ? 'This will apply the import to destination database. Continue?'
          : 'This will simulate import to validate mapping. Continue?',
        { title: finalize ? 'Release Import' : 'Simulate Import Data', confirmLabel: 'Continue' },
      );
      if (!ok) return;
      dispatch(setIsBusy());
      try {
        const saveData = prepareSaveData(dto, dictTableUiIdAndDto);
        saveData.OtherSettingsDto.TableImportSettingDto.Status = finalize
          ? EM_IMPORT_STATUS_RELEASED
          : EM_IMPORT_STATUS_DRAFT;
        const result = await schemaMetadataService.createTableImportSettingAndProcessImport(saveData);
        if (result?.IsSuccessful && result.Object) {
          const fresh = JSON.parse(JSON.stringify(result.Object));
          ensureDbToDbShape(fresh);
          const t = fresh.OtherSettingsDto.TableImportSettingDto;
          const hydrated = hydrateImportSettingLevels(t);
          setDictTableUiIdAndDto(hydrated.dictTableUiIdAndDto);
          setDto(fresh);
          bump();
          showInfo(finalize ? 'Import released' : 'Import simulation completed');
        } else {
          showWarning(extractProcessError(result));
        }
      } catch (e: any) {
        showError(e?.message || 'Import failed');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dto, dictTableUiIdAndDto, needToUpdateTransactionId, dispatch, showConfirm, showError, showInfo, showWarning, bump],
  );

  const refreshSourceColumns = useCallback(async () => {
    if (!dto) return;
    dispatch(setIsBusy());
    try {
      const saveData = prepareSaveData(dto, dictTableUiIdAndDto);
      const result = await schemaMetadataService.resetDbToDbImportSourceColumns(saveData);
      if (result?.IsSuccessful && result.Object) {
        await loadData();
        showInfo('Source columns refreshed');
      } else {
        showWarning(extractProcessError(result));
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to refresh source columns');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dto, dictTableUiIdAndDto, dispatch, loadData, showError, showInfo, showWarning]);

  const changeCurrentEditTable = useCallback((tableDto: any) => {
    if (tableDto?.UiId) setCurrentEditTableUiId(tableDto.UiId);
  }, []);

  const setImportToMultipleTable = useCallback(() => {
    if (!tip || committed) return;
    tip.IsSpilitToMultipleTables = true;
    setDto({ ...dto });
    bump();
  }, [tip, dto, committed, bump]);

  const setImportToSingleTable = useCallback(async () => {
    if (!tip || committed) return;
    const ok = await showConfirm('Please confirm to remove all level 2 and level 3 tables.', {
      title: 'Import To Single Table',
      confirmLabel: 'Confirm',
    });
    if (!ok) return;
    const toRemove = [...(tip.levelTwoTables || []), ...(tip.levelThreeTables || [])];
    for (const t of toRemove) executeRemoveOneTable(t, tip, dictTableUiIdAndDto);
    tip.IsSpilitToMultipleTables = false;
    tip.levelTwoTables = [];
    tip.levelThreeTables = [];
    setDictTableUiIdAndDto({ ...dictTableUiIdAndDto });
    setCurrentEditTableUiId(tip.levelOneTable?.UiId ?? null);
    setDto({ ...dto });
    bump();
  }, [tip, dto, dictTableUiIdAndDto, committed, showConfirm, bump]);

  const addExistingTableFromSchema = useCallback(
    async (tableName: string, schemaOwner: string, ctx: TableSelectorContext, callback?: (t: any) => void) => {
      if (!dto || !tip) return;
      dispatch(setIsBusy());
      try {
        const tableDto = await schemaMetadataService.getOneDatabaseTableSchema(
          tableName,
          dto.DataSourceFrom,
          schemaOwner || '',
        );
        tableDto.UiId = tableDto.Name;
        tableDto.Tag = String(ctx.level);
        tableDto.IsImportToExistingTable = true;
        tableDto.DictExistingTableColumnNameAndImportMappingDto = {};
        for (const columnDto of tableDto.Columns || []) {
          columnDto.NetName = '';
          columnDto.IsLogicKey = false;
          columnDto.LinkToParentTablePkColumnName = '';
          tableDto.DictExistingTableColumnNameAndImportMappingDto[columnDto.Name] = {
            ColumnName: columnDto.Name,
            MapToSourceColumnName: columnDto.MapToSourceColumnName ?? null,
          };
        }
        if (ctx.parentTableDto) tableDto.parentTableUiId = ctx.parentTableDto.UiId;
        if (!Array.isArray(tip.Tables)) tip.Tables = [];
        tip.Tables.push(tableDto);
        if (ctx.level === 1) tip.levelOneTable = tableDto;
        else if (ctx.level === 2) {
          if (!Array.isArray(tip.levelTwoTables)) tip.levelTwoTables = [];
          tip.levelTwoTables.push(tableDto);
        } else if (ctx.level === 3) {
          if (!Array.isArray(tip.levelThreeTables)) tip.levelThreeTables = [];
          tip.levelThreeTables.push(tableDto);
        }
        dictTableUiIdAndDto[tableDto.UiId] = tableDto;
        setDictTableUiIdAndDto({ ...dictTableUiIdAndDto });
        setCurrentEditTableUiId(tableDto.UiId);
        setDto({ ...dto });
        bump();
        callback?.(tableDto);
      } catch (e: any) {
        showError(e?.message || 'Failed to load table schema');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dto, tip, dictTableUiIdAndDto, dispatch, showError, bump],
  );

  const createNewTable = useCallback(
    (level: number, parentTableDto: any | null) => {
      if (!tip || committed) return;
      setTableDesignCtx({ level, parentTableDto, tableName: null });
      setTableDesignOpen(true);
    },
    [tip, committed],
  );

  const onTableDesignSaved = useCallback(
    (dbTable: any) => {
      if (!tableDesignCtx || !dbTable?.Name) return;
      if (tableDesignCtx.tableName) {
        setTableDesignOpen(false);
        setTableDesignCtx(null);
        bump();
        return;
      }
      const ctx: TableSelectorContext = {
        level: tableDesignCtx.level,
        parentTableDto: tableDesignCtx.parentTableDto,
      };
      addExistingTableFromSchema(dbTable.Name, dbTable.SchemaOwner ?? '', ctx, (tableDto) => {
        if (dbTable.Columns) {
          for (const createdCol of dbTable.Columns) {
            if (!createdCol.IsAutoNumber && createdCol.srcColumnName) {
              for (const targetCol of tableDto.Columns || []) {
                if (String(targetCol.Name).toLowerCase() === String(createdCol.Name).toLowerCase()) {
                  targetCol.MapToSourceColumnName = createdCol.srcColumnName;
                }
              }
            }
          }
          setDto({ ...dto });
          bump();
        }
      });
      setTableDesignOpen(false);
      setTableDesignCtx(null);
    },
    [tableDesignCtx, addExistingTableFromSchema, dto, bump],
  );

  const addNewMatrixTable = useCallback(() => {
    if (!tip || committed || !tip.levelOneTable) return;
    const tableDto = prepareNewTableData(2, tip.levelOneTable, tip, dictTableUiIdAndDto);
    tableDto.IsMatrixTable = true;
    tableDto.foreignMatrixKeyTableUiIdList = [];
    tableDto.ForeignMatrixKeyTableNameList = [];
    setDictTableUiIdAndDto({ ...dictTableUiIdAndDto });
    setCurrentEditTableUiId(tableDto.UiId);
    setDto({ ...dto });
    bump();
  }, [tip, dto, dictTableUiIdAndDto, committed, bump]);

  const openDatabaseTableSelector = useCallback((level: number, parentTableDto: any | null, isFk = false) => {
    setTableSelectorCtx({ level, parentTableDto, isFromUpdateMappingByFkTableSetting: isFk });
    setTableSelectorOpen(true);
  }, []);

  const onDatabaseTableSelected = useCallback(
    async (tableName: string, schemaOwner: string) => {
      if (!tableSelectorCtx || !dto) return;
      const ctx = tableSelectorCtx;
      setTableSelectorOpen(false);
      setTableSelectorCtx(null);

      if (ctx.isFromUpdateMappingByFkTableSetting) {
        const fkTable = await schemaMetadataService.getOneDatabaseTableSchema(
          tableName,
          dto.DataSourceFrom,
          schemaOwner || '',
        );
        setFkMappingCtx((prev: any) => ({
          ...prev,
          FkTableName: fkTable?.Name ?? tableName,
          FkTableSchema: fkTable?.SchemaOwner ?? schemaOwner,
          orgValueColumnCV: new CollectionView(fkTable?.Columns ?? []),
          newValueColumnCV: new CollectionView(fkTable?.Columns ?? []),
        }));
        return;
      }
      await addExistingTableFromSchema(tableName, schemaOwner, ctx);
    },
    [tableSelectorCtx, dto, addExistingTableFromSchema],
  );

  const editDatabaseTable = useCallback((tableDto: any) => {
    if (!tableDto?.Name) return;
    setTableDesignCtx({ level: parseInt(String(tableDto.Tag), 10) || 1, parentTableDto: null, tableName: tableDto.Name });
    setTableDesignOpen(true);
  }, []);

  const removeOneTable = useCallback(
    async (tableDto: any) => {
      if (!tableDto || committed || !tip) return;
      const ok = await showConfirm(
        `Please confirm to remove table [${tableDto.Name}] and its child level tables.`,
        { title: 'Remove Table', confirmLabel: 'Confirm' },
      );
      if (!ok) return;
      executeRemoveOneTable(tableDto, tip, dictTableUiIdAndDto);
      updateAllSourceColumnsSelectedStatus(tip);
      setDictTableUiIdAndDto({ ...dictTableUiIdAndDto });
      setCurrentEditTableUiId(tip.levelOneTable?.UiId ?? null);
      setDto({ ...dto });
      bump();
    },
    [tip, dto, dictTableUiIdAndDto, committed, showConfirm, bump],
  );

  const dropSourceColumnsOnTable = useCallback(
    (tableUiId: string, sourceNames: string[]) => {
      const tableDto = dictTableUiIdAndDto[tableUiId];
      if (!tableDto || tableDto.IsImportToExistingTable || committed || !tip?.SourceColumns) return;
      changeCurrentEditTable(tableDto);
      const byName = new Map((tip.SourceColumns as any[]).map((c: any) => [c.Name, c]));
      for (const name of sourceNames) {
        const src = byName.get(name);
        if (src) addOneColumnToTable(tableDto, columnFromSourceForNewTable(src));
      }
      setDto({ ...dto });
      bump();
    },
    [dictTableUiIdAndDto, tip, dto, committed, changeCurrentEditTable, bump],
  );

  const moveSelectedColumnsBetweenTables = useCallback(
    (fromUiId: string, toUiId: string) => {
      if (committed || fromUiId === toUiId) return;
      const fromTable = dictTableUiIdAndDto[fromUiId];
      const toTable = dictTableUiIdAndDto[toUiId];
      if (!fromTable || !toTable || toTable.IsImportToExistingTable) return;
      const selected = (fromTable.Columns || []).filter(
        (c: any) => c.isSelected && !c.IsPrimaryKey && !c.IsForeignKey,
      );
      for (const col of selected) {
        removeOneColumnFromTable(fromTable, col);
        addOneColumnToTable(toTable, { ...col, isSelected: false });
      }
      setDto({ ...dto });
      bump();
    },
    [dictTableUiIdAndDto, dto, committed, bump],
  );

  const mapSourceToExistingColumn = useCallback(
    (tableDto: any, columnDto: any, sourceName: string) => {
      if (!tableDto || !columnDto || committed) return;
      columnDto.MapToSourceColumnName = sourceName;
      if (tableDto.DictExistingTableColumnNameAndImportMappingDto?.[columnDto.Name]) {
        tableDto.DictExistingTableColumnNameAndImportMappingDto[columnDto.Name].MapToSourceColumnName = sourceName;
      }
      setDto({ ...dto });
      bump();
    },
    [dto, committed, bump],
  );

  const dropColumnsToSource = useCallback(
    (payload: { fromTableUiId: string }) => {
      if (committed) return;
      const fromTable = dictTableUiIdAndDto[payload.fromTableUiId];
      if (fromTable) {
        const selected = (fromTable.Columns || []).filter(
          (c: any) => c.isSelected && !c.IsPrimaryKey && !c.IsForeignKey,
        );
        for (const col of selected) removeOneColumnFromTable(fromTable, col);
        setDto({ ...dto });
        bump();
      }
    },
    [dictTableUiIdAndDto, dto, committed, bump],
  );

  const autoMapColumns = useCallback(
    (tableDto: any) => {
      autoMapExistingTableColumns(tableDto, tip?.SourceColumns ?? []);
      setDto({ ...dto });
      bump();
    },
    [tip, dto, bump],
  );

  const toggleTableFullScreen = useCallback(
    (tableDto: any) => {
      tableDto.isEditOnFullScreen = !tableDto.isEditOnFullScreen;
      setDto({ ...dto });
      bump();
    },
    [dto, bump],
  );

  const previewTableData = useCallback(
    (tableDto?: any) => {
      if (tableDto?.Name) {
        setPreviewTable({
          tableName: tableDto.Name,
          dataSourceRegisterId: dto?.DataSourceFrom ?? null,
          schemaOwner: tableDto.SchemaOwner ?? null,
        });
        setPreviewOpen(true);
        return;
      }
      const srcDb = tip?.SourceDataSourceFrom;
      if (Number(tip?.SourceDataSourceType) === EM_DB2DB_SOURCE_DATABASE_TABLE && tip?.SourceTableName && srcDb) {
        setPreviewTable({ tableName: tip.SourceTableName, dataSourceRegisterId: Number(srcDb), schemaOwner: null });
        setPreviewOpen(true);
      }
    },
    [tip, dto],
  );

  const editFromDataSet = useCallback(() => {
    if (tip?.SourceDataSetId) {
      addTabAndNavigate('dataset-editor', 'DataSet Editor', { id: tip.SourceDataSetId }, true);
    }
  }, [tip, addTabAndNavigate]);

  const openMatrixKeyPopup = useCallback(
    (tableDto: any) => {
      const options = (tip?.levelTwoTables || [])
        .filter((p: any) => !p.IsMatrixTable)
        .map((p: any) => ({
          UiId: p.UiId,
          Name: p.Name,
          isSelected: (tableDto.foreignMatrixKeyTableUiIdList || []).includes(p.UiId),
        }));
      setMatrixOptions(options);
      setMatrixTableDto(tableDto);
      setMatrixPopupOpen(true);
    },
    [tip],
  );

  const toggleMatrixOption = useCallback((uiId: string, selected: boolean) => {
    setMatrixOptions((opts) => opts.map((o) => (o.UiId === uiId ? { ...o, isSelected: selected } : o)));
  }, []);

  const applyMatrixKeys = useCallback(() => {
    if (matrixTableDto) {
      matrixTableDto.foreignMatrixKeyTableUiIdList = matrixOptions.filter((o) => o.isSelected).map((o) => o.UiId);
      matrixTableDto.ForeignMatrixKeyTableNameList = matrixTableDto.foreignMatrixKeyTableUiIdList
        .map((id: string) => dictTableUiIdAndDto[id]?.Name)
        .filter(Boolean);
      setDto({ ...dto });
      bump();
    }
    setMatrixPopupOpen(false);
    setMatrixTableDto(null);
  }, [matrixTableDto, matrixOptions, dictTableUiIdAndDto, dto, bump]);

  const openFkMappingPopup = useCallback(
    (tableDto: any, columnDto: any) => {
      const existing = tableDto.DictColumnNameAndUpdateMappingDto?.[columnDto.Name];
      setFkMappingCtx({
        tableDto,
        columnDto,
        FkTableName: existing?.FkTableName ?? '',
        FkTableSchema: existing?.FkTableSchema ?? '',
        OrgValueColumnName: existing?.OrgValueColumnName ?? null,
        NewValueColumnName: existing?.NewValueColumnName ?? null,
        orgValueColumnCV: new CollectionView<any>([]),
        newValueColumnCV: new CollectionView<any>([]),
      });
      if (existing?.FkTableName && dto) {
        schemaMetadataService
          .getOneDatabaseTableSchema(existing.FkTableName, dto.DataSourceFrom, existing.FkTableSchema || '')
          .then((fkTable) => {
            setFkMappingCtx((prev: any) => ({
              ...prev,
              orgValueColumnCV: new CollectionView(fkTable?.Columns ?? []),
              newValueColumnCV: new CollectionView(fkTable?.Columns ?? []),
            }));
          })
          .catch(() => undefined);
      }
      setFkMappingOpen(true);
    },
    [dto],
  );

  const patchFkMapping = useCallback((patch: Partial<any>) => {
    setFkMappingCtx((prev: any) => (prev ? { ...prev, ...patch } : prev));
  }, []);

  const applyFkMapping = useCallback(() => {
    if (!fkMappingCtx?.tableDto || !fkMappingCtx?.columnDto) return;
    const { tableDto, columnDto, FkTableName, FkTableSchema, OrgValueColumnName, NewValueColumnName } = fkMappingCtx;
    tableDto.DictColumnNameAndUpdateMappingDto = tableDto.DictColumnNameAndUpdateMappingDto || {};
    let mappingDisplay = '';
    if (FkTableName && OrgValueColumnName && NewValueColumnName) {
      mappingDisplay = `[${FkTableName}]: [${OrgValueColumnName}] -> [${NewValueColumnName}]`;
    }
    tableDto.DictColumnNameAndUpdateMappingDto[columnDto.Name] = {
      ColumnName: columnDto.Name,
      FkTableSchema: FkTableSchema || '',
      FkTableName,
      OrgValueColumnName,
      NewValueColumnName,
      MappingDisplay: mappingDisplay,
    };
    setFkMappingOpen(false);
    setFkMappingCtx(null);
    setDto({ ...dto });
    bump();
  }, [fkMappingCtx, dto, bump]);

  const openEntitySelector = useCallback((_tableDto: any, columnDto: any) => {
    setEntitySelectionColumn(columnDto);
    setEntityPopupOpen(true);
  }, []);

  const onEntitySelected = useCallback(
    (entityId: string | number) => {
      if (entitySelectionColumn) {
        entitySelectionColumn.EntityId = entityId;
        entitySelectionColumn.EntityColumnName = '';
        const ent = allEntities.find((e) => String(e.Id) === String(entityId));
        if (ent?.EntityType === 1 && ent?.OtherSettingsDto?.IdentityColumnDataType) {
          entitySelectionColumn.EntityColumnName = ent.DisplayFiled1 || '';
          entitySelectionColumn.Tag = ent.OtherSettingsDto.IdentityColumnDataType;
        } else if (ent?.EntityType === 2) {
          entitySelectionColumn.EntityColumnName = 'Code';
          entitySelectionColumn.Tag = 'Integer';
        }
        setDto({ ...dto });
        bump();
      }
      setEntityPopupOpen(false);
      setEntitySelectionColumn(null);
    },
    [entitySelectionColumn, allEntities, dto, bump],
  );

  const clearEntity = useCallback(
    (columnDto: any) => {
      columnDto.EntityId = null;
      columnDto.EntityColumnName = '';
      setDto({ ...dto });
      bump();
    },
    [dto, bump],
  );

  const onTableFieldChange = useCallback(() => {
    setDto({ ...dto });
    bump();
  }, [dto, bump]);

  const onSelectAllTableColumns = useCallback((tableDto: any, checked: boolean) => {
    tableDto.isSelectAllColumn = checked;
    for (const col of filterImportTableColumns(tableDto.Columns)) {
      if (!col.IsPrimaryKey && !col.IsForeignKey) col.isSelected = checked;
    }
    setDto({ ...dto });
    bump();
  }, [dto, bump]);

  const onColumnCheckbox = useCallback(
    (tableDto: any, columnDto: any) => {
      columnDto.isSelected = !columnDto.isSelected;
      setDto({ ...dto });
      bump();
    },
    [dto, bump],
  );

  const onColumnCellEdit = useCallback(
    (_tableDto: any, _columnDto: any, _binding: string) => {
      setDto({ ...dto });
      bump();
    },
    [dto, bump],
  );

  const editorApi = {
    levelOneTable,
    levelTwoTables,
    levelThreeTables,
    isMultiTable,
    committed,
    needToUpdateTransactionId,
    currentEditTableUiId,
    revision,
    sourceColumnDataMap,
    getParentColumnDataMap,
    dataTypeDataMap,
    systemTokenDataMap,
    getEntityCodeById,
    getForeignMatrixKeyDisplay: (t: any) => getForeignMatrixKeyDisplay(t, dictTableUiIdAndDto),
    changeCurrentEditTable,
    setImportToMultipleTable,
    setImportToSingleTable,
    openDatabaseTableSelector,
    createNewTable,
    addNewMatrixTable,
    dropSourceColumnsOnTable,
    moveSelectedColumnsBetweenTables,
    mapSourceToExistingColumn,
    removeSelectedColumnsFromTable: (tableDto: any) => {
      const selected = (tableDto.Columns || []).filter(
        (c: any) => c.isSelected && !c.IsPrimaryKey && !c.IsForeignKey,
      );
      for (const col of selected) removeOneColumnFromTable(tableDto, col);
      setDto({ ...dto });
      bump();
    },
    autoMapColumns,
    editDatabaseTable,
    previewTableData,
    toggleTableFullScreen,
    removeOneTable,
    openMatrixKeyPopup,
    openFkMappingPopup,
    openEntitySelector,
    clearEntity,
    onTableFieldChange,
    onSelectAllTableColumns,
    onColumnCheckbox,
    onColumnCellEdit,
    dropColumnsToSource,
  };

  return {
    dto,
    tip,
    loadError,
    loadData,
    saveDraftSetting,
    runImport,
    refreshSourceColumns,
    sourceColumnsCV,
    dataTypeDataMap,
    fromDatabaseName,
    toDatabaseName,
    fromTableName,
    fromDataSetName,
    isFinalized,
    isDataImported,
    needToUpdateTransactionId,
    committed,
    editorApi,
    tableSelectorOpen,
    setTableSelectorOpen,
    onDatabaseTableSelected,
    tableDesignOpen,
    setTableDesignOpen,
    tableDesignCtx,
    onTableDesignSaved,
    previewOpen,
    setPreviewOpen,
    previewTable,
    matrixPopupOpen,
    setMatrixPopupOpen,
    matrixOptions,
    toggleMatrixOption,
    applyMatrixKeys,
    fkMappingOpen,
    setFkMappingOpen,
    fkMappingCtx,
    patchFkMapping,
    applyFkMapping,
    openDatabaseTableSelectorForFk: () => openDatabaseTableSelector(0, null, true),
    entityPopupOpen,
    setEntityPopupOpen,
    allEntities,
    onEntitySelected,
    editFromDataSet,
    updateDtoField: (mutate: (d: any, t: any) => void) => {
      if (!dto) return;
      mutate(dto, tip);
      setDto({ ...dto });
      bump();
    },
  };
}
