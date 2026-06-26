// Generic pivot-edit transform helpers.
//
// Faithful port of the AngularJS `flexGridHelper.convertflexGridToPivotEdit` /
// `convertPivotToFlat` logic (Scripts1x/Helper/flexGridHelper.js), adapted to a
// framework-agnostic, side-effect-free shape so it can drive a React/Wijmo grid.
//
// Concepts (mirrors server `AppPivotDto`):
//   - PivotRowFields   → fixed left descriptor columns (one column each).
//   - PivotColumnFields → dimension fields; each distinct value combination becomes
//                         a dynamic column group.
//   - PivotValueFields  → cell content fields (server also injects PK / link-to-parent
//                         fields here so they round-trip even when hidden).
//
// A "flat" row is an `AppChildDataDto`-like object: { DictOneToOneFields, DictOneToManyFields }.
// A "wide" row is a plain object keyed by row-field binding + `${comboId}_${valueFieldName}`.

export interface PivotFieldDef {
  Id?: any;
  DataBaseFieldName: string;
  DisplayName?: string;
  EntityId?: any;
  ControlType?: number;
  IsVisible?: boolean;
}

export interface PivotDtoLike {
  PivotRowFields?: PivotFieldDef[];
  PivotColumnFields?: PivotFieldDef[];
  PivotValueFields?: PivotFieldDef[];
  IsPivotEdit?: boolean;
}

export interface PivotRowColumn {
  header: string;
  binding: string; // DataBaseFieldName
  fieldId?: any;
  controlType?: number;
}

export interface PivotLeafColumn {
  header: string; // value field display name
  binding: string; // `${comboId}_${dataBaseFieldName}`
  comboId: string;
  dataBaseFieldName: string;
  fieldId?: any;
  controlType?: number;
  visible: boolean;
}

export interface PivotColumnGroup {
  header: string; // combined display of the column-key field values
  comboId: string;
  colKeyValues: Record<string, any>; // columnFieldName -> raw value (id)
  columns: PivotLeafColumn[];
}

export interface PivotBuildResult {
  rowColumns: PivotRowColumn[];
  columnGroups: PivotColumnGroup[];
  valueFields: PivotFieldDef[];
  wideRows: any[];
}

/** Resolver returning a human-readable display for a column-key field value (DDL lookup). */
export type PivotDisplayResolver = (field: PivotFieldDef, value: any) => string;

/** Flatten AppChildDataDto[] → plain { fieldName: value } objects (copy of DictOneToOneFields). */
export function flattenPivotRows(rows: any[]): any[] {
  return (rows ?? []).map((r) => ({ ...((r?.DictOneToOneFields ?? r?.dictOneToOneFields) ?? {}) }));
}

/** Join the given fields' values from a data row into an underscore-delimited key. */
function comboKeyOf(fields: PivotFieldDef[], dataRow: any): string {
  return (fields ?? [])
    .map((f) => {
      const v = dataRow?.[f.DataBaseFieldName];
      return v == null ? '' : String(v);
    })
    .join('_');
}

/** Return the first representative data row for each distinct combination of `fields`. */
function distinctCombos(fields: PivotFieldDef[], dataItemList: any[]): any[] {
  const seen = new Set<string>();
  const out: any[] = [];
  for (const row of dataItemList) {
    const key = comboKeyOf(fields, row);
    if (!seen.has(key)) {
      seen.add(key);
      out.push(row);
    }
  }
  return out;
}

/**
 * Build the pivot model (row columns, dynamic column groups and wide-format rows)
 * from flat AppChildDataDto rows. Mirrors Angular `convertflexGridToPivotEdit`.
 */
export function buildPivotModel(
  pivotDto: PivotDtoLike,
  rows: any[],
  getDisplay?: PivotDisplayResolver,
): PivotBuildResult {
  const rowFields = pivotDto?.PivotRowFields ?? [];
  const columnFields = pivotDto?.PivotColumnFields ?? [];
  const valueFields = pivotDto?.PivotValueFields ?? [];

  const dataItemList = flattenPivotRows(rows);

  // Index every flat row by (rowKey + colKey) so wide cells can be filled from the source.
  const keyFields = [...rowFields, ...columnFields];
  const dictDataList: Record<string, any> = {};
  for (const dataRow of dataItemList) {
    dictDataList[comboKeyOf(keyFields, dataRow)] = dataRow;
  }

  // Left fixed descriptor columns.
  const rowColumns: PivotRowColumn[] = rowFields.map((f) => ({
    header: f.DisplayName ?? f.DataBaseFieldName,
    binding: f.DataBaseFieldName,
    fieldId: f.Id,
    controlType: f.ControlType,
  }));

  // One column group per distinct column-key combination present in the data.
  const columnGroups: PivotColumnGroup[] = [];
  for (const groupRow of distinctCombos(columnFields, dataItemList)) {
    const colKeyValues: Record<string, any> = {};
    let comboId = '';
    let display = '';
    for (const cf of columnFields) {
      const valueId = groupRow[cf.DataBaseFieldName];
      colKeyValues[cf.DataBaseFieldName] = valueId;
      comboId += (valueId == null ? '' : String(valueId)) + '_';
      const disp = getDisplay ? getDisplay(cf, valueId) : valueId;
      display += (disp == null ? '' : String(disp)) + '_';
    }
    comboId = comboId.slice(0, -1);
    display = display.slice(0, -1);

    // Every value field becomes a leaf column. PK / link fields have IsVisible === false
    // so they stay hidden but still round-trip through the wide row.
    const columns: PivotLeafColumn[] = valueFields.map((vf) => ({
      header: vf.DisplayName ?? vf.DataBaseFieldName,
      // Prefix so the binding is never a bare numeric/leading-digit path (Wijmo binding safety).
      binding: `pv_${comboId}_${vf.DataBaseFieldName}`,
      comboId,
      dataBaseFieldName: vf.DataBaseFieldName,
      fieldId: vf.Id,
      controlType: vf.ControlType,
      visible: vf.IsVisible !== false,
    }));

    columnGroups.push({ header: display, comboId, colKeyValues, columns });
  }

  // Build wide rows: one per distinct row-key combination.
  const wideRows: any[] = [];
  for (const groupRow of distinctCombos(rowFields, dataItemList)) {
    const wide: any = {};
    for (const rc of rowColumns) {
      wide[rc.binding] = groupRow[rc.binding];
    }
    for (const group of columnGroups) {
      const merged = { ...groupRow, ...group.colKeyValues };
      const src = dictDataList[comboKeyOf(keyFields, merged)];
      for (const leaf of group.columns) {
        wide[leaf.binding] = src ? src[leaf.dataBaseFieldName] ?? null : null;
      }
    }
    wideRows.push(wide);
  }

  return { rowColumns, columnGroups, valueFields, wideRows };
}

/**
 * Convert wide-format pivot rows back into flat AppChildDataDto rows.
 * Mirrors Angular `convertPivotToFlat` (one flat row per wide-row × column-group),
 * but writes every column-key field (Angular only kept the last one).
 */
export function wideToFlatRows(
  rowColumns: PivotRowColumn[],
  columnGroups: PivotColumnGroup[],
  wideRows: any[],
): any[] {
  const flat: any[] = [];
  for (const wide of wideRows ?? []) {
    for (const group of columnGroups) {
      const dictOneToOne: Record<string, any> = {};

      // Column-key field values for this group.
      for (const fieldName of Object.keys(group.colKeyValues)) {
        dictOneToOne[fieldName] = group.colKeyValues[fieldName];
      }
      // Value fields (incl. hidden PK / link fields that round-trip).
      for (const leaf of group.columns) {
        dictOneToOne[leaf.dataBaseFieldName] = wide[leaf.binding] ?? null;
      }
      // Row descriptor fields.
      for (const rc of rowColumns) {
        dictOneToOne[rc.binding] = wide[rc.binding] ?? null;
      }

      flat.push({ DictOneToOneFields: dictOneToOne, DictOneToManyFields: {}, IsDirty: true });
    }
  }
  return flat;
}
