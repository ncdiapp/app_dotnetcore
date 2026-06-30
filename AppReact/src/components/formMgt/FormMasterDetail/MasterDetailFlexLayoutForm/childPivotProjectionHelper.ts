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
  Visible?: boolean;
}

export interface ProjColumnGroup {
  Header: string;
  ComboId: string;
  ColValue?: any;
  Columns: ProjLeafColumn[];
}

export interface ChildPivotProjectionModel {
  HostColumns?: ProjColumn[];
  ColumnGroups?: ProjColumnGroup[];
  WideRows?: any[];
  ColumnKeyFieldName?: string;
  ColumnSourceFieldName?: string;
  ColumnSourceFieldId?: number | null;
  ColumnSourceUnitId?: number | null;
  GrandchildUnitId?: number | null;
  IsConfigured?: boolean;
  ChildRowCount?: number;
  SourceRowCount?: number;
}

export interface GrandchildFieldDefault {
  DataBaseFieldName?: string;
  DefaultValue?: string | null;
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
  const columnGroups = model.ColumnGroups ?? [];
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

    for (const g of columnGroups) {
      const vals: Record<string, any> = {};
      let hasValue = false;
      for (const leaf of g.Columns ?? []) {
        const v = wide[leaf.Binding] ?? null;
        vals[leaf.DataBaseFieldName] = v;
        if (hasNonEmptyValue(v)) hasValue = true;
      }

      if (g.ComboId in byCol) {
        const existing = byCol[g.ComboId];
        const nextGcFields = { ...(existing.DictOneToOneFields ?? {}) };
        for (const [k, v] of Object.entries(vals)) nextGcFields[k] = v;
        nextGcFields[columnKeyFieldName] = g.ColValue;
        const gcIdx = gcRows.indexOf(existing);
        const nextGc = { ...existing, DictOneToOneFields: nextGcFields, IsDirty: true };
        if (gcIdx >= 0) gcRows[gcIdx] = nextGc;
        byCol[g.ComboId] = nextGc;
      } else if (hasValue) {
        const blank = buildBlankGrandchildRow(grandchildFieldDefaults);
        for (const [k, v] of Object.entries(vals)) blank.DictOneToOneFields[k] = v;
        blank.DictOneToOneFields[columnKeyFieldName] = g.ColValue;
        blank.IsDirty = true;
        blank.IsNew = true;
        gcRows.push(blank);
        byCol[g.ComboId] = blank;
      }
    }

    cr.DictOneToManyFields = { ...(cr.DictOneToManyFields ?? {}), [grandchildUnitId]: gcRows };
    cr.IsDirty = true;
  }

  return nextChildRows;
}
