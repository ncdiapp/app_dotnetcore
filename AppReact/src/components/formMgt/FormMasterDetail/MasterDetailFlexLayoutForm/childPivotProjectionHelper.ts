// Type definitions for the "Child Unit Pivot Columns" projection
// (EmAppTransactionGridDisplayType.ChildUnitPivotColumns).
//
// Build (wide rows + column groups) runs on the C# server (AppChildPivotProjectionBL):
//   POST /webapi/AppTransaction/BuildChildPivotProjection
// Fold-back on cell edit runs **client-side** (foldWideRowsIntoChildRows) so each keystroke
// does not round-trip the server. Logic mirrors AppChildPivotProjectionBL.FoldWideRows.

export interface ProjColumn {
  Header: string;
  Binding: string; // host field DataBaseFieldName
  FieldId?: number | null;
  ControlType?: number | null;
  Nbdecimal?: number | null;
  IsReadOnly?: boolean;
  Visible?: boolean;
}

export interface ProjLeafColumn {
  Header: string;
  Binding: string; // `pv_${comboId}_${grandchildFieldName}`
  ComboId: string;
  DataBaseFieldName: string;
  FieldId?: number | null;
  ControlType?: number | null;
  Nbdecimal?: number | null;
  Visible?: boolean;
}

export interface ProjColumnGroup {
  Header: string;
  /** IsPivotRow (Column Group) field id for static parent headers. */
  FieldId?: number | null;
  ComboId?: string;
  ColValue?: any;
  /** Leaf value columns (data-bearing groups). */
  Columns?: ProjLeafColumn[];
  /** Nested child groups (IsPivotRow parents and/or per-comboId groups). */
  ChildGroups?: ProjColumnGroup[];
}

export interface ChildPivotProjectionModel {
  HostColumns?: ProjColumn[];
  ColumnGroups?: ProjColumnGroup[];
  WideRows?: any[];
  ColumnKeyFieldName?: string;
  ColumnSourceFieldName?: string;
  ColumnSourceFieldId?: number | null;
  ColumnSourceUnitId?: number | null;
  /** Source-grid boolean field; when set, only true-ish rows become pivot columns. */
  ColumnSourceVisibleFieldName?: string | null;
  ColumnSourceVisibleFieldId?: number | null;
  GrandchildUnitId?: number | null;
  IsConfigured?: boolean;
  /**
   * True when grandchild has at least one IsPivotRow field (not IsPivotColumn).
   * UI should render FlexGridColumnGroup; otherwise flat FlexGridColumn.
   */
  IsNeedPivotColumnGroup?: boolean;
  ChildRowCount?: number;
  SourceRowCount?: number;
}

export interface GrandchildFieldDefault {
  DataBaseFieldName?: string;
  DefaultValue?: string | null;
}

/** Walk nested ColumnGroups; return data-bearing groups (those with Columns / value leaves). */
export function enumerateLeafColumnGroups(groups: ProjColumnGroup[] | null | undefined): ProjColumnGroup[] {
  const result: ProjColumnGroup[] = [];
  if (!groups?.length) return result;
  for (const g of groups) {
    if (!g) continue;
    if (g.ChildGroups && g.ChildGroups.length > 0) {
      result.push(...enumerateLeafColumnGroups(g.ChildGroups));
    } else if (g.Columns && g.Columns.length > 0) {
      result.push(g);
    }
  }
  return result;
}

/** Count visible leaf value columns under a (possibly nested) group tree. */
export function countVisibleValueColumns(groups: ProjColumnGroup[] | null | undefined): number {
  return enumerateLeafColumnGroups(groups).reduce(
    (n, g) => n + (g.Columns ?? []).filter((c) => c.Visible !== false).length,
    0
  );
}

function resolveRowIndex(wide: Record<string, any>, fallback: number): number {
  const idx = wide?.__rowIndex;
  if (idx != null) {
    const n = Number(idx);
    if (Number.isFinite(n)) return n;
  }
  return fallback;
}

function getColKey(gc: any, columnKeyFieldName: string): string | null {
  if (!gc?.DictOneToOneFields || !columnKeyFieldName) return null;
  const v = gc.DictOneToOneFields[columnKeyFieldName];
  if (v == null) return null;
  return String(v);
}

function buildBlankGrandchildRow(fieldDefs?: GrandchildFieldDefault[]): any {
  const row: any = {
    DictOneToOneFields: {} as Record<string, any>,
    DictOneToManyFields: {} as Record<string, any[]>,
  };
  for (const f of fieldDefs ?? []) {
    const db = f?.DataBaseFieldName;
    if (!db) continue;
    row.DictOneToOneFields[db] = f.DefaultValue ?? null;
  }
  return row;
}

function hasNonEmptyValue(v: any): boolean {
  if (v == null) return false;
  if (typeof v === 'string') return v.length > 0;
  return true;
}

/**
 * Fold edited wide rows back into child rows' nested grandchild collections.
 * Mirrors AppChildPivotProjectionBL.FoldWideRows (server).
 */
export function foldWideRowsIntoChildRows(
  childRows: any[],
  wideRows: any[],
  model: ChildPivotProjectionModel,
  grandchildFieldDefaults?: GrandchildFieldDefault[]
): any[] {
  if (!model?.IsConfigured || model.GrandchildUnitId == null) {
    return childRows;
  }

  const hostColumns = model.HostColumns ?? [];
  const leafGroups = enumerateLeafColumnGroups(model.ColumnGroups);
  const grandchildUnitId = String(model.GrandchildUnitId);
  const columnKeyFieldName = model.ColumnKeyFieldName ?? '';

  const nextChildRows = childRows.map((cr) => ({
    ...cr,
    DictOneToOneFields: { ...(cr?.DictOneToOneFields ?? {}) },
    DictOneToManyFields: { ...(cr?.DictOneToManyFields ?? {}) },
  }));

  for (let i = 0; i < wideRows.length; i++) {
    const wide = wideRows[i];
    if (!wide) continue;
    const childIndex = resolveRowIndex(wide, i);
    if (childIndex < 0 || childIndex >= nextChildRows.length) continue;

    const cr = nextChildRows[childIndex];
    for (const hc of hostColumns) {
      if (hc.IsReadOnly) continue;
      cr.DictOneToOneFields[hc.Binding] = wide[hc.Binding] ?? null;
    }

    const gcRows = [...(cr.DictOneToManyFields?.[grandchildUnitId] ?? [])];
    const byCol: Record<string, any> = {};
    for (const gc of gcRows) {
      const key = getColKey(gc, columnKeyFieldName);
      if (key != null && !(key in byCol)) byCol[key] = gc;
    }

    for (const g of leafGroups) {
      const vals: Record<string, any> = {};
      let hasValue = false;
      for (const leaf of g.Columns ?? []) {
        const v = wide[leaf.Binding] ?? null;
        vals[leaf.DataBaseFieldName] = v;
        if (hasNonEmptyValue(v)) hasValue = true;
      }

      const comboId = g.ComboId ?? '';
      if (comboId in byCol) {
        const existing = byCol[comboId];
        const nextGcFields = { ...(existing.DictOneToOneFields ?? {}) };
        for (const [k, v] of Object.entries(vals)) nextGcFields[k] = v;
        nextGcFields[columnKeyFieldName] = g.ColValue;
        const gcIdx = gcRows.indexOf(existing);
        const nextGc = { ...existing, DictOneToOneFields: nextGcFields, IsDirty: true };
        if (gcIdx >= 0) gcRows[gcIdx] = nextGc;
        byCol[comboId] = nextGc;
      } else if (hasValue) {
        const blank = buildBlankGrandchildRow(grandchildFieldDefaults);
        for (const [k, v] of Object.entries(vals)) blank.DictOneToOneFields[k] = v;
        blank.DictOneToOneFields[columnKeyFieldName] = g.ColValue;
        blank.IsDirty = true;
        blank.IsNew = true;
        gcRows.push(blank);
        byCol[comboId] = blank;
      }
    }

    cr.DictOneToManyFields = { ...(cr.DictOneToManyFields ?? {}), [grandchildUnitId]: gcRows };
    cr.IsDirty = true;
  }

  return nextChildRows;
}

/** Angular/DataGridLayout: format="n{Nbdecimal}" (keeps trailing zeros, e.g. 12.00). */
export function parseNbdecimal(raw: unknown): number | undefined {
  const n = typeof raw === 'number' ? raw : typeof raw === 'string' ? Number(raw) : NaN;
  if (!Number.isFinite(n) || n < 0) return undefined;
  return Math.min(10, Math.floor(n));
}

export function wijmoColumnTypeAndFormat(
  controlType: number | null | undefined,
  nbdecimal: unknown,
  em?: {
    Numeric?: any;
    TextBox?: any;
    Date?: any;
    DateTimeDetail?: any;
    CheckBox?: any;
  } | null,
): { dataType?: string; format?: string } {
  const ctl = controlType != null ? Number(controlType) : NaN;
  if (ctl === Number(em?.Date)) return { format: 'd' };
  if (ctl === Number(em?.DateTimeDetail)) return { format: 'g' };
  if (ctl === Number(em?.CheckBox)) return { dataType: 'Boolean' };

  const digits = parseNbdecimal(nbdecimal);
  if (ctl === Number(em?.Numeric)) {
    return { dataType: 'Number', format: `n${digits ?? 0}` };
  }
  return {};
}

/** Wijmo `format="nN"` only applies to actual numbers. Used only for Numeric control type. */
export function coerceNumericCellValue(raw: unknown): unknown {
  if (raw == null || raw === '') return raw;
  if (typeof raw === 'number') return Number.isFinite(raw) ? raw : raw;
  if (typeof raw === 'string') {
    const t = raw.trim();
    if (t.length === 0) return raw;
    const n = Number(t);
    return Number.isFinite(n) ? n : raw;
  }
  return raw;
}

function numericBindingKeys(numericBindings: string[]): string[] {
  return Array.from(new Set((numericBindings ?? []).filter(Boolean)));
}

/**
 * Coerce Numeric cells to JS numbers so Wijmo n{N} applies.
 * Always returns a shallow copy: form WideRows are often Immer/RTK-frozen
 * (assigning Sort / other numeric host columns would throw).
 */
export function coerceNumericWideRowsInPlace(rows: any[] | undefined, numericBindings: string[]): any[] {
  const source = rows ?? [];
  const keys = numericBindingKeys(numericBindings);
  const list = source.map((row) => (row && typeof row === 'object' ? { ...row } : row));
  if (list.length === 0 || keys.length === 0) return list;
  for (const row of list) {
    if (!row) continue;
    for (const b of keys) {
      if (!Object.prototype.hasOwnProperty.call(row, b) && !(b in row)) continue;
      row[b] = coerceNumericCellValue(row[b]);
    }
  }
  return list;
}
