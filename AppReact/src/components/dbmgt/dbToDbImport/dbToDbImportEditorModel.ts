/**
 * DB-to-DB import editor model helpers — ported from Angular metaDataTableImportSettingCtrl.
 */

export const USAGE_DB2DB_TABLE_IMPORT = 6;
export const EM_IMPORT_STATUS_DRAFT = 1;
export const EM_IMPORT_STATUS_RELEASED = 2;
export const EM_DB2DB_SOURCE_DATABASE_TABLE = 1;
export const EM_DB2DB_SOURCE_DATASET = 2;

export const IMPORT_LOGICAL_TYPES = [
  'String',
  'Integer',
  'Decimal',
  'Date',
  'Time',
  'DateTime',
  'Boolean',
  'Blob',
];

export const TAG_TO_DB: Record<string, string> = {
  String: 'nvarchar',
  Integer: 'int',
  Decimal: 'decimal',
  DateTime: 'datetime2',
  Date: 'datetime2',
  Time: 'datetime2',
  Boolean: 'bit',
  Blob: 'varbinary',
};

export function newUiId(prefix = ''): string {
  return `${prefix}${Date.now()}_${Math.random().toString(16).slice(2)}`;
}

export function ensureDbToDbShape(dataSetDto: any): void {
  if (!dataSetDto) return;
  if (!dataSetDto.OtherSettingsDto) dataSetDto.OtherSettingsDto = {};
  if (!dataSetDto.OtherSettingsDto.TableImportSettingDto) {
    dataSetDto.OtherSettingsDto.TableImportSettingDto = {};
  }
  const tip = dataSetDto.OtherSettingsDto.TableImportSettingDto;
  if (!Array.isArray(tip.SourceColumns)) tip.SourceColumns = [];
  if (!Array.isArray(tip.Tables)) tip.Tables = [];
  if (!tip.DictTableNameColumnNameAndSourceColumnNameMapping) {
    tip.DictTableNameColumnNameAndSourceColumnNameMapping = {};
  }
  if (!tip.DictTableNameColumnNameAndCascadingDto) tip.DictTableNameColumnNameAndCascadingDto = {};
  if (tip.Status == null) tip.Status = EM_IMPORT_STATUS_DRAFT;
}

export function isSettingCommitted(tip: any): boolean {
  return Number(tip?.Status) === EM_IMPORT_STATUS_RELEASED;
}

export function hydrateImportSettingLevels(tip: any): {
  levelOneTable: any | null;
  levelTwoTables: any[];
  levelThreeTables: any[];
  dictTableUiIdAndDto: Record<string, any>;
} {
  const dictTableUiIdAndDto: Record<string, any> = {};
  let levelOneTable: any | null = null;
  const levelTwoTables: any[] = [];
  const levelThreeTables: any[] = [];

  if (!tip?.Tables || !Array.isArray(tip.Tables)) {
    return { levelOneTable, levelTwoTables, levelThreeTables, dictTableUiIdAndDto };
  }

  for (const tableDto of tip.Tables) {
    prepareExistTableData(tableDto, tip, dictTableUiIdAndDto);
    const level = parseInt(String(tableDto.Tag), 10);
    if (level === 1) levelOneTable = tableDto;
    else if (level === 2) levelTwoTables.push(tableDto);
    else if (level === 3) levelThreeTables.push(tableDto);
  }

  tip.levelOneTable = levelOneTable;
  tip.levelTwoTables = levelTwoTables;
  tip.levelThreeTables = levelThreeTables;

  return { levelOneTable, levelTwoTables, levelThreeTables, dictTableUiIdAndDto };
}

export function prepareExistTableData(
  tableDto: any,
  _tip: any,
  dictTableUiIdAndDto: Record<string, any>,
): void {
  tableDto.UiId = tableDto.UiId || tableDto.Name;
  tableDto.isSelectAllColumn = false;
  tableDto.Columns = tableDto.Columns || [];
  tableDto.parentTableUiId = tableDto.NetName || tableDto.parentTableUiId || null;
  tableDto.foreignMatrixKeyTableUiIdList = tableDto.foreignMatrixKeyTableUiIdList || tableDto.ForeignMatrixKeyTableNameList || [];

  for (const columnDto of tableDto.Columns) {
    if (tableDto.parentTableUiId) {
      columnDto.foreignKeyTableUiId = columnDto.ForeignKeyTableName;
      columnDto.isSelected = false;
    }
    columnDto.EntityId = columnDto.NetName || columnDto.EntityId || null;
  }

  if (tableDto.IsImportToExistingTable) {
    tableDto.DictExistingTableColumnNameAndImportMappingDto =
      tableDto.DictExistingTableColumnNameAndImportMappingDto || {};
    for (const columnDto of tableDto.Columns) {
      if (!tableDto.DictExistingTableColumnNameAndImportMappingDto[columnDto.Name]) {
        tableDto.DictExistingTableColumnNameAndImportMappingDto[columnDto.Name] = {
          ColumnName: columnDto.Name,
          MapToSourceColumnName: columnDto.MapToSourceColumnName ?? null,
        };
      }
      columnDto.MapToSourceColumnName =
        tableDto.DictExistingTableColumnNameAndImportMappingDto[columnDto.Name]?.MapToSourceColumnName ?? columnDto.MapToSourceColumnName;
    }
  }

  dictTableUiIdAndDto[tableDto.UiId] = tableDto;
}

export function prepareNewTableData(
  level: number,
  parentTableDto: any | null,
  tip: any,
  dictTableUiIdAndDto: Record<string, any>,
): any {
  const tableUiId = newUiId('Table_');
  const tableDto: any = {
    Name: tableUiId,
    Columns: [],
    SchemaOwner: '',
    Tag: String(level),
    isSelectAllColumn: false,
    UiId: tableUiId,
    IsImportToExistingTable: false,
  };

  tableDto.Columns.push({
    Name: `${tableUiId}Id`,
    DbDataType: 'int',
    Tag: 'Integer',
    Nullable: false,
    IsPrimaryKey: true,
    IsAutoNumber: true,
    IsLogicKey: false,
    isSelected: false,
  });

  if (parentTableDto) {
    tableDto.parentTableUiId = parentTableDto.UiId;
    const parentPk = parentTableDto.Columns?.[0];
    if (parentPk) {
      tableDto.Columns.push({
        Name: parentPk.Name,
        DbDataType: 'int',
        Tag: 'Integer',
        Nullable: true,
        IsForeignKey: true,
        IsLogicKey: false,
        ForeignKeyTableName: parentTableDto.Name,
        foreignKeyTableUiId: parentTableDto.UiId,
        isSelected: false,
      });
    }
  }

  if (!Array.isArray(tip.Tables)) tip.Tables = [];
  tip.Tables.push(tableDto);

  if (level === 1) tip.levelOneTable = tableDto;
  else if (level === 2) {
    if (!Array.isArray(tip.levelTwoTables)) tip.levelTwoTables = [];
    tip.levelTwoTables.push(tableDto);
  } else if (level === 3) {
    if (!Array.isArray(tip.levelThreeTables)) tip.levelThreeTables = [];
    tip.levelThreeTables.push(tableDto);
  }

  dictTableUiIdAndDto[tableDto.UiId] = tableDto;
  return tableDto;
}

export function filterImportTableColumns(columns: any[] | null | undefined): any[] {
  return (columns || []).filter((c) => !c?.IsPrimaryKey && !c?.IsForeignKey);
}

export function addOneColumnToTable(tableDto: any, columnDto: any): void {
  if (!tableDto || !columnDto) return;
  const exists = (tableDto.Columns || []).some((c: any) => c?.Name === columnDto.Name);
  if (!exists) {
    columnDto.isSelected = false;
    tableDto.Columns = tableDto.Columns || [];
    tableDto.Columns.push(columnDto);
  }
}

export function removeOneColumnFromTable(tableDto: any, columnDto: any): void {
  if (!tableDto?.Columns || !columnDto) return;
  const idx = tableDto.Columns.indexOf(columnDto);
  if (idx >= 0) tableDto.Columns.splice(idx, 1);
}

export function columnFromSourceForNewTable(src: any): any {
  const tag = src?.Tag != null ? String(src.Tag) : 'String';
  const normalizedTag = IMPORT_LOGICAL_TYPES.includes(tag) ? tag : 'String';
  return {
    Name: src?.Name,
    DbDataType: src?.DbDataType || TAG_TO_DB[normalizedTag] || 'nvarchar',
    Tag: normalizedTag,
    Nullable: true,
    IsLogicKey: false,
    isNew: true,
    isSelected: false,
  };
}

export function updateAllSourceColumnsSelectedStatus(tip: any): Record<string, boolean> {
  const dict: Record<string, boolean> = {};
  for (const tableDto of tip?.Tables || []) {
    for (const columnDto of tableDto.Columns || []) {
      if (!columnDto?.IsPrimaryKey && !columnDto?.IsForeignKey && columnDto?.Name) {
        dict[columnDto.Name] = true;
      }
    }
  }
  return dict;
}

export function prepareSaveData(currentDataSetDto: any, dictTableUiIdAndDto: Record<string, any>): any {
  const saveData = JSON.parse(JSON.stringify(currentDataSetDto));
  ensureDbToDbShape(saveData);
  const importSettingDto = saveData.OtherSettingsDto.TableImportSettingDto;

  for (const tableDto of importSettingDto.Tables || []) {
    const liveTable = dictTableUiIdAndDto[tableDto.UiId] || tableDto;
    Object.assign(tableDto, JSON.parse(JSON.stringify(liveTable)));

    if (tableDto.parentTableUiId) {
      const parentTableDto = dictTableUiIdAndDto[tableDto.parentTableUiId];
      if (parentTableDto) tableDto.NetName = parentTableDto.Name;
    }

    if (tableDto.IsMatrixTable) {
      tableDto.ForeignMatrixKeyTableNameList = [];
      for (const parentUiId of tableDto.foreignMatrixKeyTableUiIdList || []) {
        const parent = dictTableUiIdAndDto[parentUiId];
        if (parent) tableDto.ForeignMatrixKeyTableNameList.push(parent.Name);
      }
    }

    for (const columnDto of tableDto.Columns || []) {
      if (columnDto.EntityId) {
        columnDto.NetName = columnDto.EntityId;
        if (
          columnDto.ParentColumn &&
          columnDto.CascadingRelationTable &&
          columnDto.CascadingRelationTableParentKeyField &&
          columnDto.CascadingRelationTableChildKeyField
        ) {
          const dictCascading =
            importSettingDto.DictTableNameColumnNameAndCascadingDto[tableDto.Name] ||
            (importSettingDto.DictTableNameColumnNameAndCascadingDto[tableDto.Name] = {});
          dictCascading[columnDto.Name] = {
            TableName: tableDto.Name,
            ColumnName: columnDto.Name,
            CascadingParentTableName: columnDto.ParentColumn.TableName,
            CascadingParentColumnName: columnDto.ParentColumn.Name,
            CascadingRelationTable: columnDto.CascadingRelationTable,
            CascadingRelationTableParentKeyField: columnDto.CascadingRelationTableParentKeyField,
            CascadingRelationTableChildKeyField: columnDto.CascadingRelationTableChildKeyField,
          };
        }
      }
      if (columnDto.isManuallyAdded) {
        importSettingDto.SourceColumns = importSettingDto.SourceColumns || [];
        importSettingDto.SourceColumns.push(columnDto);
      }
    }
  }

  importSettingDto.levelOneTable = null;
  importSettingDto.levelTwoTables = null;
  importSettingDto.levelThreeTables = null;
  importSettingDto.DictTableNameAndForeignLogicKeyColumns = null;
  importSettingDto.DictTableNameAndDto = null;
  importSettingDto.TableNameDisplay = null;
  importSettingDto.LevelOneTables = null;
  importSettingDto.LevelTwoTables = null;
  importSettingDto.LevelThreeTables = null;
  importSettingDto.DictTableNameAndPkColumnName = null;
  importSettingDto.DictTableNameAndLogicKeyColumnNameList = null;
  importSettingDto.IsHaveConditionFilter = null;

  for (const tableDto of importSettingDto.Tables || []) {
    tableDto.DictDataBaseColumn = null;
    tableDto.PrimaryKeyColumnList = null;
    tableDto.FirstPrimaryKeyColumn = null;
  }

  return saveData;
}

export function executeRemoveOneTable(
  tableDto: any,
  tip: any,
  dictTableUiIdAndDto: Record<string, any>,
): void {
  if (!tableDto) return;
  const level = String(tableDto.Tag);
  if (level === '1') {
    tip.Tables = [];
    tip.levelOneTable = null;
    tip.levelTwoTables = [];
    tip.levelThreeTables = [];
    Object.keys(dictTableUiIdAndDto).forEach((k) => delete dictTableUiIdAndDto[k]);
  } else if (level === '2') {
    const children = [...(tip.levelThreeTables || [])];
    for (const child of children) {
      if (child.parentTableUiId === tableDto.UiId) {
        executeRemoveOneTable(child, tip, dictTableUiIdAndDto);
      }
    }
    removeTableFromArrays(tableDto, tip, dictTableUiIdAndDto);
  } else if (level === '3') {
    removeTableFromArrays(tableDto, tip, dictTableUiIdAndDto);
  }
}

function removeTableFromArrays(tableDto: any, tip: any, dictTableUiIdAndDto: Record<string, any>): void {
  const idxAll = (tip.Tables || []).indexOf(tableDto);
  if (idxAll >= 0) tip.Tables.splice(idxAll, 1);
  const idx2 = (tip.levelTwoTables || []).indexOf(tableDto);
  if (idx2 >= 0) tip.levelTwoTables.splice(idx2, 1);
  const idx3 = (tip.levelThreeTables || []).indexOf(tableDto);
  if (idx3 >= 0) tip.levelThreeTables.splice(idx3, 1);
  delete dictTableUiIdAndDto[tableDto.UiId];
}

export function autoMapExistingTableColumns(tableDto: any, sourceColumns: any[]): void {
  if (!tableDto?.IsImportToExistingTable || !sourceColumns) return;
  for (const sourceColumn of sourceColumns) {
    for (const targetColumn of tableDto.Columns || []) {
      if (!targetColumn.MapToSourceColumnName && !(targetColumn.IsAutoNumber || targetColumn.SystemToken)) {
        if (String(targetColumn.Name).toLowerCase() === String(sourceColumn.Name).toLowerCase()) {
          targetColumn.MapToSourceColumnName = sourceColumn.Name;
        }
      }
    }
  }
}

export function getForeignMatrixKeyDisplay(tableDto: any, dictTableUiIdAndDto: Record<string, any>): string {
  if (!tableDto?.IsMatrixTable) return '';
  const parts: string[] = [];
  for (const uiId of tableDto.foreignMatrixKeyTableUiIdList || []) {
    const parent = dictTableUiIdAndDto[uiId];
    if (!parent) continue;
    for (const col of parent.Columns || []) {
      if (col.IsLogicKey) parts.push(`${parent.Name}.${col.Name}`);
    }
  }
  return parts.join(', ');
}

export function getValidationMessages(result: any, errorsOnly = false): string[] {
  const items = Array.isArray(result?.ValidationResult?.Items) ? result.ValidationResult.Items : [];
  return items
    .filter((i: any) => (errorsOnly ? i?.ItemType === 1 || !!i?.ErrorMessage : true))
    .map((i: any) => i?.ErrorMessage || i?.LocalizedMessage || i?.Message)
    .filter(Boolean);
}

export function extractProcessError(result: any): string {
  const errorMsgs = getValidationMessages(result, true);
  if (errorMsgs.length) return errorMsgs.join('\n');
  const allMsgs = getValidationMessages(result, false);
  if (allMsgs.length) return allMsgs.join('\n');
  return result?.ValidationResult?.LocalizedResult || result?.ValidationResult?.ErrorMessage || 'Operation failed.';
}
