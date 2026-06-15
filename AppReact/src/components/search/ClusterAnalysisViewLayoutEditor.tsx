/**
 * Cluster Analysis View layout designer — flex grid + palette of child views (charts, grid, card, pivot, map).
 * Persists to AppSearchViewExDto.OtherSettingsDto.FlexLayoutItems; leaf DesktopWidget uses WidgetItemType 9
 * (EmAppDashboardWidgetItemType.ClusterAnalysisViewItem), matching backend AppSearchViewConfigBL.PrepareClusterChildViewItems.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../redux/hooks/useTheme';
import appHelper from '../../helper/appHelper';
import { GridViewLayout } from './searchViewLayout/GridViewLayout';
import { CardViewLayout } from './searchViewLayout/CardViewLayout';
import { PivotViewLayout } from './searchViewLayout/PivotViewLayout';
import { ChartViewLayout } from './searchViewLayout/ChartViewLayout';
import { CalendarViewLayout } from './searchViewLayout/CalendarViewLayout';
import { GanttViewLayout } from './searchViewLayout/GanttViewLayout';
import { SchedulerViewLayout } from './searchViewLayout/SchedulerViewLayout';

export const CLUSTER_ANALYSIS_VIEW_TYPE = 25;
const WIDGET_CLUSTER = 9;
const WT_RESPONSIVE = 100;
const WT_ROW = 101;
const WT_COL = 102;

type RowContainer = {
  Id: string;
  WidgetItemType: 101;
  RowHeight?: number | string;
  StyleLayoutInfo?: string;
  ChildDesktopItems: ColumnContainer[];
};

type ColumnContainer = {
  Id: string;
  WidgetItemType: 102;
  ColSpanValue?: number;
  ColumnSpan?: number;
  MinWidth?: number;
  StyleLayoutInfo?: string;
  DesktopWidget?: any | null;
  ChildDesktopItems?: any[];
  __editorMerge?: { dir: 'up' | 'down'; targetRowId: string; targetColId: string };
  __editorGhost?: { fromRowId: string; fromColId: string };
};

type ResponsiveContainer = {
  Id: string;
  WidgetItemType: 100;
  StyleLayoutInfo?: string;
  ChildDesktopItems: RowContainer[];
};

type PathSeg = { rowId: string; colId: string };
type ContainerPath = PathSeg[];
type CellAddress = { path: ContainerPath; rowId: string; colId: string };
type DragPayloadV2 =
  | { kind: 'palette'; paletteId: string }
  | { kind: 'widget'; from: CellAddress; widget: any };

// Cluster editor follows Angular behavior: column spans are relative units per-row (e.g. 1/2, then 2/3+1/3),
// not a fixed 24-grid. So we never normalize spans to 24.

export function newFlexItemId(prefix: string): string {
  try {
    const g: any = globalThis as any;
    if (g?.crypto?.randomUUID) return `${prefix}_${g.crypto.randomUUID()}`;
  } catch {
    /* ignore */
  }
  return `${prefix}_${Date.now()}_${Math.random().toString(16).slice(2)}`;
}

export function createFlexRowsCluster(rowsCount: number, colsPerRow: number): any[] {
  const rows = Math.max(1, Math.min(100, Math.floor(rowsCount) || 1));
  const cols = Math.max(1, Math.min(24, Math.floor(colsPerRow) || 1));
  const rowsOut: any[] = [];
  for (let r = 0; r < rows; r++) {
    const cells: any[] = [];
    for (let c = 0; c < cols; c++) {
      cells.push({
        Id: newFlexItemId('col'),
        WidgetItemType: WT_COL,
        ColSpanValue: 1,
        DesktopWidget: null,
      });
    }
    rowsOut.push({
      Id: newFlexItemId('row'),
      WidgetItemType: WT_ROW,
      RowHeight: 'auto',
      ChildDesktopItems: cells,
    });
  }
  return rowsOut;
}

export function createDefaultClusterFlexLayout(): any[] {
  return [
    {
      Id: newFlexItemId('responsive'),
      WidgetItemType: WT_RESPONSIVE,
      ChildDesktopItems: createFlexRowsCluster(2, 2),
    },
  ];
}

function getResponsiveRoot(flexItems: any[]): any | null {
  if (!Array.isArray(flexItems)) return null;
  return flexItems.find((x) => x && Number(x.WidgetItemType) === WT_RESPONSIVE) ?? null;
}

function getNestedResponsiveRoot(col: ColumnContainer | any): ResponsiveContainer | null {
  const list = Array.isArray(col?.ChildDesktopItems) ? col.ChildDesktopItems : [];
  const root = list.find((x: any) => x && Number(x.WidgetItemType) === WT_RESPONSIVE) ?? null;
  return root as any;
}

function needsNewId(id: any): boolean {
  if (id == null) return true;
  const s = String(id);
  return s.trim() === '' || s === '0';
}

export function ensureClusterFlexIdsDeep(items: any[]): any[] {
  const list = Array.isArray(items) ? items : [];
  const root = getResponsiveRoot(list) as any;
  if (!root) return list;

  const fixResponsive = (responsive: any): any => {
    const nextRoot = { ...(responsive || {}) };
    if (needsNewId(nextRoot.Id)) nextRoot.Id = newFlexItemId('responsive');
    const rows = Array.isArray(nextRoot.ChildDesktopItems) ? nextRoot.ChildDesktopItems : [];
    nextRoot.ChildDesktopItems = rows.map((r: any) => {
      const row = { ...(r || {}) };
      if (needsNewId(row.Id)) row.Id = newFlexItemId('row');
      row.WidgetItemType = WT_ROW;
      const cols = Array.isArray(row.ChildDesktopItems) ? row.ChildDesktopItems : [];
      row.ChildDesktopItems = cols.map((c: any) => {
        const col = { ...(c || {}) };
        if (needsNewId(col.Id)) col.Id = newFlexItemId('col');
        col.WidgetItemType = WT_COL;
        // Fix nested responsive container (vertical merge)
        const nested = getNestedResponsiveRoot(col as any);
        if (nested) {
          col.DesktopWidget = null;
          col.ChildDesktopItems = [fixResponsive(nested)];
        } else {
          const w = col.DesktopWidget;
          if (w && typeof w === 'object') {
            const nextW = { ...w };
            if (needsNewId(nextW.Id)) nextW.Id = newFlexItemId('cw');
            if (Number(nextW.WidgetItemType) === WIDGET_CLUSTER) {
              if (needsNewId(nextW.UiId)) nextW.UiId = newFlexItemId('ui');
            }
            col.DesktopWidget = nextW;
          }
          if (!Array.isArray(col.ChildDesktopItems)) col.ChildDesktopItems = [];
        }
        return col;
      });
      return row;
    });
    return nextRoot;
  };

  const fixedRoot = fixResponsive(root);
  return list.map((it: any) =>
    Number(it?.WidgetItemType) === WT_RESPONSIVE && String(it?.Id ?? '') === String(root?.Id ?? '')
      ? fixedRoot
      : it
  );
}

function createDefaultFlexRows(): RowContainer[] {
  return createFlexRowsCluster(1, 1) as any;
}

function createDefaultResponsiveRoot(rows: RowContainer[] = createDefaultFlexRows()): ResponsiveContainer {
  return {
    Id: newFlexItemId('responsive'),
    WidgetItemType: WT_RESPONSIVE,
    ChildDesktopItems: rows,
  } as any;
}

function setClusterWidgetAt(items: any[], rowId: string, colId: string, widget: any | null): any[] {
  const root = getResponsiveRoot(items);
  if (!root) return items;
  const rootId = String(root.Id ?? '');
  const nextRows = (root.ChildDesktopItems || []).map((row: any) => {
    if (String(row.Id) !== String(rowId)) return row;
    const nextCols = (row.ChildDesktopItems || []).map((col: any) =>
      String(col.Id) === String(colId) ? { ...col, DesktopWidget: widget } : col
    );
    return { ...row, ChildDesktopItems: nextCols };
  });
  const nextRoot = { ...root, ChildDesktopItems: nextRows };
  return items.map((it: any) =>
    Number(it?.WidgetItemType) === WT_RESPONSIVE && String(it?.Id ?? '') === rootId ? nextRoot : it
  );
}

function swapClusterWidgets(
  items: any[],
  a: { rowId: string; colId: string },
  b: { rowId: string; colId: string }
): any[] {
  const root = getResponsiveRoot(items);
  if (!root) return items;
  const findWidget = (rowId: string, colId: string) => {
    const row = (root.ChildDesktopItems || []).find((r: any) => String(r.Id) === String(rowId));
    const col = (row?.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(colId));
    return col?.DesktopWidget ?? null;
  };
  const wa = findWidget(a.rowId, a.colId);
  const wb = findWidget(b.rowId, b.colId);
  let next = setClusterWidgetAt(items, a.rowId, a.colId, wb);
  next = setClusterWidgetAt(next, b.rowId, b.colId, wa);
  return next;
}

function clusterLeafLabel(w: any, chartTypeList: { Id: number; Display: string }[]): string {
  if (!w || Number(w.WidgetItemType) !== WIDGET_CLUSTER) return '';
  if (w.DisplayTitle) return String(w.DisplayTitle);
  const vt = w.ViewType;
  if (vt === 7 && w.ChartType != null) {
    const hit = chartTypeList.find((c) => c.Id === w.ChartType);
    if (hit) return hit.Display;
  }
  const map: Record<number, string> = {
    1: 'Grid View',
    2: 'Card View',
    5: 'Pivot View',
    7: 'Chart View',
    17: 'Google Map View',
  };
  return map[Number(vt)] || `Child view (${vt})`;
}

type DragPayload =
  | { kind: 'palette'; paletteId: string }
  | { kind: 'clusterWidget'; rowId: string; colId: string };

function tryParseDragPayload(raw: string): DragPayload | null {
  if (!raw) return null;
  try {
    const o = JSON.parse(raw);
    if (o?.kind === 'palette' && typeof o.paletteId === 'string') return o;
    if (o?.kind === 'clusterWidget' && typeof o.rowId === 'string' && typeof o.colId === 'string') return o;
  } catch {
    /* ignore */
  }
  return null;
}

function tryParseDragPayloadV2(raw: string): DragPayloadV2 | null {
  if (!raw) return null;
  try {
    const o = JSON.parse(raw);
    if (o?.kind === 'palette' && typeof o.paletteId === 'string') return o;
    if (o?.kind === 'widget' && o?.from && o?.widget) return o as DragPayloadV2;
  } catch {
    /* ignore */
  }
  return null;
}

const CHILD_VIEW_TYPES: { Id: number; Display: string }[] = [
  { Id: 1, Display: 'Grid View' },
  { Id: 2, Display: 'Card View' },
  { Id: 7, Display: 'Chart View' },
  { Id: 5, Display: 'Pivot View' },
  { Id: 17, Display: 'Google Map View' },
];

function buildClusterWidgetFromPalette(
  paletteId: string,
  chartTypeList: { Id: number; Display: string }[]
): any {
  const uiId = newFlexItemId('ui');
  const id = newFlexItemId('cw');
  if (paletteId === 'palette_child_grid') {
    return { Id: id, WidgetItemType: WIDGET_CLUSTER, UiId: uiId, ViewType: 1, DisplayTitle: 'Grid View' };
  }
  if (paletteId === 'palette_child_card') {
    return { Id: id, WidgetItemType: WIDGET_CLUSTER, UiId: uiId, ViewType: 2, DisplayTitle: 'Card View' };
  }
  if (paletteId === 'palette_child_pivot') {
    return { Id: id, WidgetItemType: WIDGET_CLUSTER, UiId: uiId, ViewType: 5, DisplayTitle: 'Pivot View' };
  }
  if (paletteId === 'palette_child_map') {
    return { Id: id, WidgetItemType: WIDGET_CLUSTER, UiId: uiId, ViewType: 17, DisplayTitle: 'Google Map View' };
  }
  if (paletteId.startsWith('palette_chart_')) {
    const chartType = Number(paletteId.slice('palette_chart_'.length));
    const label = chartTypeList.find((c) => c.Id === chartType)?.Display ?? 'Chart';
    return {
      Id: id,
      WidgetItemType: WIDGET_CLUSTER,
      UiId: uiId,
      ViewType: 7,
      ChartType: chartType,
      DisplayTitle: label,
    };
  }
  return {
    Id: id,
    WidgetItemType: WIDGET_CLUSTER,
    UiId: uiId,
    ViewType: 1,
    DisplayTitle: 'Grid View',
  };
}

export interface ClusterAnalysisViewLayoutEditorProps {
  flexLayoutItems: any[];
  onFlexLayoutItemsChange: (items: any[]) => void;
  chartTypeList: { Id: number; Display: string }[];
  dataSetColumns?: any[];
  mainViewFields?: any[];
  previewDataRows?: any[];
  onRefresh: () => void;
  onSave: () => void | Promise<void>;
  onPreviewClusterSearchView?: () => void;
  viewName: string;
  viewDescription: string;
  onViewNameChange: (v: string) => void;
  onViewDescriptionChange: (v: string) => void;
}

const ClusterAnalysisViewLayoutEditor: React.FC<ClusterAnalysisViewLayoutEditorProps> = ({
  flexLayoutItems,
  onFlexLayoutItemsChange,
  chartTypeList,
  dataSetColumns = [],
  mainViewFields = [],
  previewDataRows = [],
  onRefresh,
  onSave,
  onPreviewClusterSearchView,
  viewName,
  viewDescription,
  onViewNameChange,
  onViewDescriptionChange,
}) => {
  const { theme } = useTheme();
  const [toolboxVisible, setToolboxVisible] = useState(true);
  const [initOpen, setInitOpen] = useState(false);
  const [initRows, setInitRows] = useState(2);
  const [initCols, setInitCols] = useState(2);
  const [dragOverKey, setDragOverKey] = useState<string | null>(null);
  const [hoveredCellKey, setHoveredCellKey] = useState<string | null>(null);
  const [selectedCell, setSelectedCell] = useState<{ rowId: string; colId: string } | null>(null);
  const [cellContextMenu, setCellContextMenu] = useState<{ open: true; addr: CellAddress; x: number; y: number } | null>(null);
  const [childConfigurePopup, setChildConfigurePopup] = useState<{ open: true; x: number; y: number } | null>(null);
  const editorWrapRef = useRef<HTMLDivElement>(null);
  const cellMenuRef = useRef<HTMLDivElement>(null);

  const displayItems = useMemo(() => {
    const list = Array.isArray(flexLayoutItems) ? flexLayoutItems : [];
    const base = getResponsiveRoot(list) ? list : createDefaultClusterFlexLayout();
    return ensureClusterFlexIdsDeep(base);
  }, [flexLayoutItems]);

  const root = getResponsiveRoot(displayItems);
  const rows = root?.ChildDesktopItems ?? [];

  const selectedWidget = useMemo(() => {
    if (!selectedCell || !root) return null;
    const row = rows.find((r: any) => String(r.Id) === String(selectedCell.rowId));
    const col = (row?.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(selectedCell.colId));
    const w = col?.DesktopWidget;
    if (!w || Number(w.WidgetItemType) !== WIDGET_CLUSTER) return null;
    return w;
  }, [selectedCell, root, rows]);

  const chartFieldOptions = useMemo(() => {
    const ds = (Array.isArray(dataSetColumns) ? dataSetColumns : []).map((f: any) => ({
      Id: String(f?.Id ?? ''),
      Display: String(f?.Id ?? ''),
    }));
    const cal = (Array.isArray(mainViewFields) ? mainViewFields : [])
      .filter((f: any) => Boolean(f?.IsCalulationField))
      .map((f: any) => {
        const path = String(f?.SysTableFiledPath ?? '');
        const label = String(f?.DisplayText ?? '');
        return {
          Id: path,
          Display: label ? `${path} (${label})` : path,
        };
      })
      .filter((x: any) => x.Id);
    const dedup = new Map<string, { Id: string; Display: string }>();
    [...ds, ...cal].forEach((x) => {
      if (!x.Id || dedup.has(x.Id)) return;
      dedup.set(x.Id, x);
    });
    return Array.from(dedup.values());
  }, [dataSetColumns, mainViewFields]);

  const normalizeChildColumns = useCallback((cols: any[]) => {
    if (!Array.isArray(cols)) return [];
    const used = new Set<string>();
    return cols.map((f: any, idx: number) => {
      const name = f?.Name ?? f?.Display ?? f?.DisplayText ?? f?.SysTableFiledPath ?? String(f?.Id ?? '');
      const rawId = String(f?.Id ?? '').trim();
      let stableId = rawId && rawId !== '_' ? rawId : String(f?.SysTableFiledPath ?? name ?? `col_${idx}`);
      if (!stableId) stableId = `col_${idx}`;
      if (used.has(stableId)) stableId = `${stableId}__${idx}`;
      used.add(stableId);
      return {
        ...f,
        Id: stableId,
        Name: name,
        Display: f?.Display ?? f?.DisplayText ?? name,
        IsVisible: f?.IsVisible ?? true,
        Sort: f?.Sort ?? 0,
      };
    });
  }, []);

  const extractClusterChildRows = useCallback((parentRows: any[], childUiId: any): any[] => {
    if (!Array.isArray(parentRows) || !childUiId) return [];
    const normalizeOne = (rowLike: any) => {
      if (!rowLike || typeof rowLike !== 'object') return null;
      if (rowLike.DictViewColumnIDKeyValue) return rowLike;
      if (rowLike.Row && rowLike.Row.DictViewColumnIDKeyValue) return rowLike.Row;
      if (rowLike.Data && rowLike.Data.DictViewColumnIDKeyValue) return rowLike.Data;
      return rowLike;
    };
    const out: any[] = [];
    for (const parentRow of parentRows) {
      let dict: any = parentRow?.DictClusterChildViewUiIdAndResultRowJsonDto;
      if (typeof dict === 'string') {
        try {
          dict = JSON.parse(dict);
        } catch {
          dict = null;
        }
      }
      if (!dict || typeof dict !== 'object') continue;
      const key = String(childUiId);
      const byKey = dict?.[key];
      const fallback = byKey ?? Object.values(dict)[0];
      const normalized = normalizeOne(fallback);
      if (normalized) out.push(normalized);
    }
    return out;
  }, []);

  const projectParentRowsToChildRows = useCallback((parentRows: any[], childColumns: any[], mainColumns: any[]) => {
    const cols = Array.isArray(childColumns) ? childColumns : [];
    const mains = Array.isArray(mainColumns) ? mainColumns : [];
    const byPath = new Map<string, any>();
    mains.forEach((m: any) => {
      const p = String(m?.SysTableFiledPath ?? '').trim().toLowerCase();
      if (p) byPath.set(p, m);
    });
    return (Array.isArray(parentRows) ? parentRows : []).map((pr: any) => {
      const src = pr?.DictViewColumnIDKeyValue ?? {};
      const numericKeys = Object.keys(src)
        .filter((k) => /^-?\d+$/.test(String(k)))
        .sort((a, b) => Number(a) - Number(b));
      const nextDict: Record<string, any> = {};
      cols.forEach((c: any, idx: number) => {
        const path = String(c?.SysTableFiledPath ?? '').trim().toLowerCase();
        const main = path ? byPath.get(path) : null;
        const mainId = main?.Id;
        let val =
          (mainId != null ? src?.[mainId] : undefined) ??
          (mainId != null ? src?.[String(mainId)] : undefined) ??
          src?.[c?.Id] ??
          src?.[String(c?.Id)] ??
          src?.[c?.SysTableFiledPath];
        if (val === undefined) {
          const nk = numericKeys[idx];
          if (nk != null) val = src?.[nk];
        }
        nextDict[String(c?.Id)] = val;
      });
      return { ...(pr || {}), DictViewColumnIDKeyValue: nextDict };
    });
  }, []);

  const adaptRowsForChildView = useCallback((rows: any[], childColumns: any[], mainColumns: any[]) => {
    const cols = Array.isArray(childColumns) ? childColumns : [];
    const mains = Array.isArray(mainColumns) ? mainColumns : [];
    const byPath = new Map<string, any>();
    mains.forEach((m: any) => {
      const p = String(m?.SysTableFiledPath ?? '').trim().toLowerCase();
      if (p) byPath.set(p, m);
    });
    return (Array.isArray(rows) ? rows : []).map((r: any) => {
      const src = { ...(r?.DictViewColumnIDKeyValue ?? {}) };
      const numericKeys = Object.keys(src)
        .filter((k) => /^-?\d+$/.test(String(k)))
        .sort((a, b) => Number(a) - Number(b));
      const out = { ...src } as Record<string, any>;
      cols.forEach((c: any, idx: number) => {
        const cid = c?.Id;
        const cidStr = String(cid ?? '');
        const path = String(c?.SysTableFiledPath ?? '').trim();
        const pathKey = path.toLowerCase();
        const main = pathKey ? byPath.get(pathKey) : null;
        const mainId = main?.Id;
        let val =
          src?.[cid] ??
          src?.[cidStr] ??
          (path ? src?.[path] : undefined) ??
          (mainId != null ? src?.[mainId] : undefined) ??
          (mainId != null ? src?.[String(mainId)] : undefined);
        if (val === undefined) {
          const nk = numericKeys[idx];
          if (nk != null) val = src?.[nk];
        }
        if (cid != null) out[cid as any] = val;
        if (cidStr) out[cidStr] = val;
        if (path) out[path] = val;
      });
      return { ...(r || {}), DictViewColumnIDKeyValue: out };
    });
  }, []);

  const ensureItemsForMutation = useCallback(() => {
    if (!getResponsiveRoot(flexLayoutItems)) {
      return ensureClusterFlexIdsDeep(createDefaultClusterFlexLayout());
    }
    return ensureClusterFlexIdsDeep(flexLayoutItems);
  }, [flexLayoutItems]);

  const applyItems = useCallback(
    (next: any[]) => {
      onFlexLayoutItemsChange(next);
    },
    [onFlexLayoutItemsChange]
  );

  const getRootForMutation = useCallback((): ResponsiveContainer => {
    const base = ensureItemsForMutation();
    const r = getResponsiveRoot(base) as ResponsiveContainer | null;
    if (r) return r;
    const fallback = createDefaultClusterFlexLayout();
    return getResponsiveRoot(fallback) as any;
  }, [ensureItemsForMutation]);

  const updateRootRows = useCallback(
    (mutator: (rows: RowContainer[]) => RowContainer[] | null | undefined) => {
      const base = ensureItemsForMutation();
      const r = getResponsiveRoot(base) as ResponsiveContainer | null;
      if (!r) return;
      const rid = String((r as any).Id ?? '');
      const nextRows = mutator((r as any).ChildDesktopItems || []);
      if (!nextRows) return;
      const nextRoot = { ...(r as any), ChildDesktopItems: nextRows };
      const next = (base || []).map((it: any) =>
        Number(it?.WidgetItemType) === WT_RESPONSIVE && String(it?.Id ?? '') === rid ? nextRoot : it
      );
      applyItems(next);
    },
    [applyItems, ensureItemsForMutation]
  );

  const findRowIndexIn = (rws: RowContainer[], rowId: string) => (rws || []).findIndex((r) => String(r.Id) === String(rowId));
  const sumRowColSpan = (row: RowContainer) =>
    (row?.ChildDesktopItems || []).reduce((sum, c: any) => sum + Math.max(1, Math.min(24, Number(c?.ColSpanValue ?? c?.ColumnSpan ?? 1) || 1)), 0);

  const rowHeightStyle = useCallback((row: any): React.CSSProperties => {
    const raw = row?.RowHeight ?? 'auto';
    if (raw == null) return {};
    if (typeof raw === 'number') return { height: `${Math.max(0, raw)}px` };
    const s = String(raw).trim().toLowerCase();
    if (s === '' || s === 'auto') return {};
    const asNum = Number(s);
    if (Number.isFinite(asNum) && !Number.isNaN(asNum)) return { height: `${Math.max(0, asNum)}px` };
    // allow css values like '300px'
    return { height: String(raw) };
  }, []);

  const setColumnSpanAt = useCallback(
    (rowId: string, colId: string, span: number) => {
      const nextSpan = Math.max(1, Math.min(24, Number(span) || 1));
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        const nextCols = cols.map((c: any) => ({ ...c }));
        nextCols[cIdx].ColSpanValue = nextSpan;
        row.ChildDesktopItems = nextCols;
        nextRows[rIdx] = row;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const setColumnMinWidthAt = useCallback(
    (rowId: string, colId: string, minWidth: number) => {
      const nextMin = Math.max(0, Number(minWidth) || 0);
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        cols[cIdx] = { ...cols[cIdx], MinWidth: nextMin };
        row.ChildDesktopItems = cols;
        nextRows[rIdx] = row;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const setRowHeightAt = useCallback(
    (rowId: string, rowHeight: number | string) => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        nextRows[rIdx] = { ...nextRows[rIdx], RowHeight: rowHeight } as any;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const insertRowRelativeAt = useCallback(
    (rowId: string, where: 'above' | 'below') => {
      updateRootRows((rws) => {
        const idx = findRowIndexIn(rws, rowId);
        if (idx < 0) return rws;
        const next = [...rws];
        const baseRow = next[idx] as any;
        const colsCount = Math.max(1, (baseRow?.ChildDesktopItems || []).length || 1);
        const newRow = (createFlexRowsCluster(1, colsCount) as any)[0] as any;
        const insertIdx = where === 'above' ? idx : idx + 1;
        next.splice(insertIdx, 0, newRow);
        return next;
      });
    },
    [updateRootRows]
  );

  const deleteRowAt = useCallback(
    (rowId: string) => {
      updateRootRows((rws) => {
        const next = (rws || []).filter((r) => String(r.Id) !== String(rowId));
        return next.length ? next : (createFlexRowsCluster(1, 1) as any);
      });
      setSelectedCell(null);
    },
    [updateRootRows]
  );

  const insertColumnRelativeAt = useCallback(
    (rowId: string, colId: string, where: 'left' | 'right') => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        const newCol: ColumnContainer = {
          Id: newFlexItemId('col'),
          WidgetItemType: WT_COL,
          ColSpanValue: 1,
          MinWidth: 300,
          DesktopWidget: null,
          ChildDesktopItems: [],
        } as any;
        const insertIdx = where === 'left' ? cIdx : cIdx + 1;
        cols.splice(insertIdx, 0, newCol as any);
        row.ChildDesktopItems = cols;
        nextRows[rIdx] = row;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const deleteColumnAt = useCallback(
    (rowId: string, colId: string) => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = (row.ChildDesktopItems || []).filter((c: any) => String(c.Id) !== String(colId));
        const safeCols =
          cols.length
            ? cols
            : [
                {
                  Id: newFlexItemId('col'),
                  WidgetItemType: WT_COL,
                  ColSpanValue: 1,
                  MinWidth: 300,
                  DesktopWidget: null,
                  ChildDesktopItems: [],
                },
              ];
        row.ChildDesktopItems = safeCols;
        nextRows[rIdx] = row;
        return nextRows;
      });
      setSelectedCell(null);
    },
    [updateRootRows]
  );

  const splitColumnAt = useCallback(
    (rowId: string, colId: string, parts: 2 | 3 | 4) => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        const cur = cols[cIdx] as any;
        const span = Math.max(1, Math.min(24, Number(cur.ColSpanValue ?? cur.ColumnSpan ?? 1) || 1));
        const base = Math.max(1, Math.floor(span / parts));
        const remainder = span - base * parts;
        const nextCols: any[] = [];
        for (let i = 0; i < parts; i++) {
          const s = i === parts - 1 ? base + remainder : base;
          nextCols.push({
            Id: i === 0 ? cur.Id : newFlexItemId('col'),
            WidgetItemType: WT_COL,
            ColSpanValue: s,
            MinWidth: cur.MinWidth ?? 300,
            DesktopWidget: i === 0 ? cur.DesktopWidget ?? null : null,
            ChildDesktopItems: i === 0 ? (cur.ChildDesktopItems || []) : [],
          });
        }
        cols.splice(cIdx, 1, ...nextCols);
        row.ChildDesktopItems = cols;
        nextRows[rIdx] = row;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const mergeColumnAt = useCallback(
    (rowId: string, colId: string, dir: 'left' | 'right') => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        const targetIdx = dir === 'left' ? cIdx - 1 : cIdx + 1;
        if (targetIdx < 0 || targetIdx >= cols.length) return rws;

        const cur = cols[cIdx] as any;
        const tgt = cols[targetIdx] as any;
        const curSpan = Math.max(1, Math.min(24, Number(cur.ColSpanValue ?? cur.ColumnSpan ?? 1) || 1));
        const tgtSpan = Math.max(1, Math.min(24, Number(tgt.ColSpanValue ?? tgt.ColumnSpan ?? 1) || 1));
        const mergedWidget = tgt.DesktopWidget ?? cur.DesktopWidget ?? null;
        const mergedChild = (tgt.ChildDesktopItems && tgt.ChildDesktopItems.length ? tgt.ChildDesktopItems : cur.ChildDesktopItems) ?? [];
        cols[targetIdx] = {
          ...tgt,
          ColSpanValue: Math.min(24, tgtSpan + curSpan),
          DesktopWidget: mergedWidget,
          ChildDesktopItems: mergedChild,
        };
        cols.splice(cIdx, 1);
        row.ChildDesktopItems = cols;
        nextRows[rIdx] = row;
        return nextRows;
      });
    },
    [updateRootRows]
  );

  const clearCellContentAt = useCallback(
    (rowId: string, colId: string) => {
      updateRootRows((rws) => {
        const rIdx = findRowIndexIn(rws, rowId);
        if (rIdx < 0) return rws;
        const nextRows = [...rws];
        const row = { ...nextRows[rIdx] } as any;
        const cols = [...(row.ChildDesktopItems || [])];
        const cIdx = cols.findIndex((c: any) => String(c.Id) === String(colId));
        if (cIdx < 0) return rws;
        const prev = cols[cIdx] as any;
        cols[cIdx] = { ...prev, DesktopWidget: null, ChildDesktopItems: [], __editorMerge: undefined, __editorGhost: undefined };
        row.ChildDesktopItems = cols;
        nextRows[rIdx] = row;
        return nextRows;
      });
      setSelectedCell(null);
    },
    [updateRootRows]
  );

  const getColRangeInRow = (row: RowContainer, colId: string): { start: number; end: number } | null => {
    let cursor = 0;
    const cols = row?.ChildDesktopItems || [];
    for (const c of cols as any) {
      const span = Math.max(1, Math.min(24, Number((c as any).ColSpanValue ?? (c as any).ColumnSpan ?? 1) || 1));
      const start = cursor;
      const end = cursor + span;
      if (String((c as any).Id) === String(colId)) return { start, end };
      cursor = end;
    }
    return null;
  };

  const findBestOverlappingColId = (row: RowContainer, start: number, end: number): string | null => {
    let cursor = 0;
    let best: { id: string; overlap: number } | null = null;
    const cols = row?.ChildDesktopItems || [];
    for (const c of cols as any) {
      const span = Math.max(1, Math.min(24, Number((c as any).ColSpanValue ?? (c as any).ColumnSpan ?? 1) || 1));
      const s = cursor;
      const e = cursor + span;
      cursor = e;
      const overlap = Math.max(0, Math.min(end, e) - Math.max(start, s));
      if (!best || overlap > best.overlap) best = { id: String((c as any).Id), overlap };
    }
    return best?.id ?? null;
  };

  const cellContentToRows = (col: ColumnContainer): RowContainer[] => {
    const nested = getNestedResponsiveRoot(col as any);
    if (nested?.ChildDesktopItems?.length) return nested.ChildDesktopItems as any;
    const w = (col as any).DesktopWidget;
    if (!w) return [];
    return [
      {
        Id: newFlexItemId('row'),
        WidgetItemType: WT_ROW,
        RowHeight: 'auto',
        ChildDesktopItems: [
          {
            Id: newFlexItemId('col'),
            WidgetItemType: WT_COL,
            ColSpanValue: 24,
            MinWidth: (col as any).MinWidth ?? 300,
            DesktopWidget: w,
            ChildDesktopItems: [],
          } as any,
        ],
      } as any,
    ];
  };

  const mergeVerticalAt = useCallback(
    (rowId: string, colId: string, dir: 'up' | 'down') => {
      updateRootRows((rws) => {
        const list = [...(rws || [])] as any[];
        const rIdx = list.findIndex((r: any) => String(r.Id) === String(rowId));
        if (rIdx < 0) return rws;
        const targetIdx = dir === 'up' ? rIdx - 1 : rIdx + 1;
        if (targetIdx < 0 || targetIdx >= list.length) return rws;

        const row = list[rIdx] as RowContainer;
        const targetRow = list[targetIdx] as RowContainer;
        const range = getColRangeInRow(row, colId);
        if (!range) return rws;

        const targetColId = findBestOverlappingColId(targetRow, range.start, range.end);
        if (!targetColId) return rws;

        const curCol = (row.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(colId)) as ColumnContainer | undefined;
        const tgtCol = (targetRow.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(targetColId)) as ColumnContainer | undefined;
        if (!curCol || !tgtCol) return rws;

        const curRows = cellContentToRows(curCol);
        const tgtRows = cellContentToRows(tgtCol);
        const mergedRows = dir === 'up' ? [...tgtRows, ...curRows] : [...curRows, ...tgtRows];
        const mergedRoot = createDefaultResponsiveRoot(mergedRows.length ? mergedRows : createDefaultFlexRows());

        const nextRow: RowContainer = {
          ...(row as any),
          ChildDesktopItems: (row.ChildDesktopItems || []).map((c: any) =>
            String(c.Id) === String(colId)
              ? ({ ...c, DesktopWidget: null, ChildDesktopItems: [mergedRoot], __editorMerge: { dir, targetRowId: String((targetRow as any).Id), targetColId: String(targetColId) } } as any)
              : c
          ),
        } as any;

        const nextTargetRow: RowContainer = {
          ...(targetRow as any),
          ChildDesktopItems: (targetRow.ChildDesktopItems || []).map((c: any) =>
            String(c.Id) === String(targetColId)
              ? ({ ...c, DesktopWidget: null, ChildDesktopItems: [], __editorGhost: { fromRowId: String((row as any).Id), fromColId: String(colId) } } as any)
              : c
          ),
        } as any;

        list[rIdx] = nextRow as any;
        list[targetIdx] = nextTargetRow as any;
        return list as any;
      });
    },
    [updateRootRows]
  );

  const openCellContextMenu = useCallback((addr: CellAddress, x: number, y: number) => {
    const { x: clampedX, y: clampedY } = appHelper.clampMenuPositionToViewport({
      x,
      y,
      menuWidth: 300,
      menuHeight: 520,
      margin: 8,
    });
    setCellContextMenu({ open: true, addr, x: clampedX, y: clampedY });
    setSelectedCell({ rowId: addr.rowId, colId: addr.colId });
  }, []);

  const onDropCell = useCallback(
    (rowId: string, colId: string, e: React.DragEvent) => {
      e.preventDefault();
      setDragOverKey(null);
      const raw =
        e.dataTransfer.getData('application/json') ||
        e.dataTransfer.getData('text/plain') ||
        e.dataTransfer.getData('text');
      const payloadV2 = tryParseDragPayloadV2(raw);
      const payloadV1 = payloadV2 ? null : tryParseDragPayload(raw);
      if (!payloadV2 && !payloadV1) return;
      const base = ensureItemsForMutation();
      if (payloadV2?.kind === 'palette') {
        const w = buildClusterWidgetFromPalette(payloadV2.paletteId, chartTypeList);
        applyItems(setClusterWidgetAt(base, rowId, colId, w));
        setSelectedCell({ rowId, colId });
        return;
      }
      if (payloadV2?.kind === 'widget') {
        const p = payloadV2;
        if (String(p.from.rowId) === String(rowId) && String(p.from.colId) === String(colId)) return;

        // If dropping onto a nested container cell, ignore (same as DashboardEditor).
        const rootNow = getResponsiveRoot(base) as any;
        const rowNow = (rootNow?.ChildDesktopItems || []).find((r: any) => String(r.Id) === String(rowId));
        const colNow = (rowNow?.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(colId));
        if (getNestedResponsiveRoot(colNow)) return;

        const targetWidget = colNow?.DesktopWidget ?? null;
        let next = base;
        next = setClusterWidgetAt(next, p.from.rowId, p.from.colId, targetWidget ? targetWidget : null);
        next = setClusterWidgetAt(next, rowId, colId, p.widget);
        applyItems(next);
        setSelectedCell({ rowId, colId });
        return;
      }
      if (payloadV1?.kind === 'palette') {
        const w = buildClusterWidgetFromPalette(payloadV1.paletteId, chartTypeList);
        applyItems(setClusterWidgetAt(base, rowId, colId, w));
        setSelectedCell({ rowId, colId });
        return;
      }
      if (payloadV1?.kind === 'clusterWidget') {
        if (payloadV1.rowId === rowId && payloadV1.colId === colId) return;
        applyItems(swapClusterWidgets(base, { rowId: payloadV1.rowId, colId: payloadV1.colId }, { rowId, colId }));
        setSelectedCell({ rowId, colId });
      }
    },
    [applyItems, chartTypeList, ensureItemsForMutation]
  );

  const removeSelectedWidget = useCallback(() => {
    if (!selectedCell) return;
    const base = ensureItemsForMutation();
    applyItems(setClusterWidgetAt(base, selectedCell.rowId, selectedCell.colId, null));
    setSelectedCell(null);
  }, [applyItems, ensureItemsForMutation, selectedCell]);

  const updateSelectedWidget = useCallback(
    (patch: Record<string, any>) => {
      if (!selectedCell || !selectedWidget) return;
      const base = ensureItemsForMutation();
      const row = (getResponsiveRoot(base)?.ChildDesktopItems || []).find(
        (r: any) => String(r.Id) === String(selectedCell.rowId)
      );
      const col = (row?.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(selectedCell.colId));
      const w = col?.DesktopWidget;
      if (!w) return;
      const nextW = { ...w, ...patch };
      applyItems(setClusterWidgetAt(base, selectedCell.rowId, selectedCell.colId, nextW));
    },
    [applyItems, ensureItemsForMutation, selectedCell, selectedWidget]
  );

  const openChildConfigure = useCallback((rowId: string, colId: string, x: number, y: number) => {
    const pos = appHelper.clampMenuPositionToViewport({ x, y, menuWidth: 420, menuHeight: 340, margin: 8 });
    setSelectedCell({ rowId, colId });
    setChildConfigurePopup({ open: true, x: pos.x, y: pos.y });
  }, []);

  const applyInitLayout = useCallback(() => {
    const base = ensureItemsForMutation();
    const r = getResponsiveRoot(base);
    if (!r) return;
    const rid = String(r.Id ?? '');
    const nextRoot = { ...r, ChildDesktopItems: createFlexRowsCluster(initRows, initCols) };
    const next = base.map((it: any) =>
      Number(it?.WidgetItemType) === WT_RESPONSIVE && String(it?.Id ?? '') === rid ? nextRoot : it
    );
    applyItems(next);
    setInitOpen(false);
    setSelectedCell(null);
  }, [applyItems, ensureItemsForMutation, initCols, initRows]);

  // Avoid Wijmo ComboBox overlays inside the editor (can lock pointer events in some environments).
  const childViewTypeCv = useMemo(() => new CollectionView([...CHILD_VIEW_TYPES]), []);

  useEffect(() => {
    if (!cellContextMenu?.open) return;
    const onDoc = (e: PointerEvent) => {
      const menu = cellMenuRef.current;
      if (menu && menu.contains(e.target as Node)) return;
      setCellContextMenu(null);
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setCellContextMenu(null);
    };
    const onScroll = () => setCellContextMenu(null);
    window.addEventListener('pointerdown', onDoc, true);
    document.addEventListener('keydown', onKey);
    window.addEventListener('scroll', onScroll, true);
    return () => {
      window.removeEventListener('pointerdown', onDoc, true);
      document.removeEventListener('keydown', onKey);
      window.removeEventListener('scroll', onScroll, true);
    };
  }, [cellContextMenu?.open]);

  return (
    <div ref={editorWrapRef} className="w-full h-full min-h-0 flex flex-col overflow-hidden">
      <div
        className={`flex w-full min-w-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 px-3 py-1.5 shrink-0 border-b ${theme.mainContentSection}`}
      >
        <div className="flex shrink-0 items-center">
          <button
            type="button"
            onClick={() => setToolboxVisible((v) => !v)}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            title={toolboxVisible ? 'Hide toolbox' : 'Show toolbox'}
          >
            <i
              className={`fa-solid mr-1.5 ${toolboxVisible ? 'fa-square-minus' : 'fa-square-plus'}`}
              aria-hidden
            />
            {toolboxVisible ? 'Hide Toolbox' : 'Show Toolbox'}
          </button>
        </div>
        <div className={`flex w-1 flex-auto min-w-0 flex-wrap items-center justify-end gap-2`}>
          <div className={`text-sm font-semibold shrink-0 ${theme.title}`}>Cluster Analysis View Editor</div>
          <button type="button" onClick={onRefresh} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={() => void onSave()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save
          </button>
          <button
            type="button"
            onClick={() => setInitOpen(true)}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-table-cells" aria-hidden /> Initialize Layout
          </button>
          <button
            type="button"
            disabled={!onPreviewClusterSearchView}
            onClick={() => onPreviewClusterSearchView?.()}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed`}
            title="Open runtime preview in MasterDataManagement"
          >
            <i className="fa-solid fa-eye" aria-hidden /> Preview View
          </button>
        </div>
      </div>

      <div className="w-full h-1 flex-auto flex flex-row min-h-0 overflow-hidden">
        {toolboxVisible && (
          <div
            className={`w-64 shrink-0 flex flex-col border-r min-h-0 overflow-y-auto ${theme.mainContentSection}`}
          >
            <div className={`px-2 py-2 border-b text-xs font-semibold ${theme.title}`}>Cluster Analysis View Setting</div>
            <div className="px-2 py-2 space-y-2 border-b">
              <div>
                <label className={`text-xs ${theme.label} block mb-0.5`}>View Name</label>
                <input
                  type="text"
                  value={viewName}
                  onChange={(e) => onViewNameChange(e.target.value)}
                  className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                />
              </div>
              <div>
                <label className={`text-xs ${theme.label} block mb-0.5`}>Description</label>
                <textarea
                  value={viewDescription}
                  onChange={(e) => onViewDescriptionChange(e.target.value)}
                  rows={3}
                  className={`w-full px-2 py-1 text-xs border rounded-[4px] ${theme.inputBox}`}
                />
              </div>
            </div>

            <div className={`text-xs font-semibold px-2 py-2 ${theme.title}`}>Drag &amp; Drop New Views To Design Panel</div>
            <div className="px-2 pb-2 space-y-3">
              <div>
                <div className={`text-[10px] uppercase tracking-wide mb-1 ${theme.label}`}>Charts</div>
                <div className="flex flex-col gap-1 max-h-48 overflow-y-auto">
                  {chartTypeList.map((c) => {
                    const pid = `palette_chart_${c.Id}`;
                    return (
                      <div
                        key={pid}
                        draggable
                        onDragStart={(e) => {
                          const data = JSON.stringify({ kind: 'palette', paletteId: pid });
                          e.dataTransfer.setData('text/plain', data);
                          try {
                            e.dataTransfer.setData('application/json', data);
                          } catch {
                            /* ignore */
                          }
                          e.dataTransfer.effectAllowed = 'copy';
                        }}
                        className={`px-2 py-1 text-xs rounded border cursor-move ${theme.inputBox}`}
                      >
                        {c.Display}
                      </div>
                    );
                  })}
                </div>
              </div>
              <div>
                <div className={`text-[10px] uppercase tracking-wide mb-1 ${theme.label}`}>Data views</div>
                {(
                  [
                    ['palette_child_grid', 'Grid View'],
                    ['palette_child_card', 'Card View'],
                    ['palette_child_pivot', 'Pivot View'],
                    ['palette_child_map', 'Google Map View'],
                  ] as const
                ).map(([pid, label]) => (
                  <div
                    key={pid}
                    draggable
                    onDragStart={(e) => {
                      const data = JSON.stringify({ kind: 'palette', paletteId: pid });
                      e.dataTransfer.setData('text/plain', data);
                      try {
                        e.dataTransfer.setData('application/json', data);
                      } catch {
                        /* ignore */
                      }
                      e.dataTransfer.effectAllowed = 'copy';
                    }}
                    className={`px-2 py-1 text-xs rounded border cursor-move mb-1 ${theme.inputBox}`}
                  >
                    {label}
                  </div>
                ))}
              </div>
            </div>

            {selectedWidget && (
              <div className={`mt-auto border-t p-2 space-y-2 ${theme.mainContentSection}`}>
                <div className={`text-xs font-semibold ${theme.title}`}>Selected item</div>
                <div>
                  <label className={`text-xs ${theme.label} block mb-0.5`}>Title</label>
                  <input
                    type="text"
                    value={selectedWidget.DisplayTitle ?? ''}
                    onChange={(e) => updateSelectedWidget({ DisplayTitle: e.target.value })}
                    className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                  />
                </div>
                <div>
                  <label className={`text-xs ${theme.label} block mb-0.5`}>Child view type</label>
                  <select
                    className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                    value={Number(selectedWidget.ViewType ?? 1)}
                    onChange={(e) => {
                      const v = Number(e.target.value);
                      if (!Number.isFinite(v)) return;
                      const patch: any = { ViewType: v };
                      if (v !== 7) patch.ChartType = null;
                      updateSelectedWidget(patch);
                    }}
                  >
                    {(childViewTypeCv?.items ?? CHILD_VIEW_TYPES).map((it: any) => (
                      <option key={String(it.Id)} value={Number(it.Id)}>
                        {String(it.Display)}
                      </option>
                    ))}
                  </select>
                </div>
                {Number(selectedWidget.ViewType) === 7 && (
                  <div>
                    <label className={`text-xs ${theme.label} block mb-0.5`}>Chart type</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                      value={Number(selectedWidget.ChartType ?? chartTypeList[0]?.Id ?? 0)}
                      onChange={(e) => {
                        const v = Number(e.target.value);
                        if (!Number.isFinite(v)) return;
                        const label = chartTypeList.find((c) => Number(c.Id) === v)?.Display;
                        updateSelectedWidget({ ChartType: v, DisplayTitle: label ?? selectedWidget.DisplayTitle });
                      }}
                    >
                      {chartTypeList.map((c) => (
                        <option key={String(c.Id)} value={Number(c.Id)}>
                          {String(c.Display)}
                        </option>
                      ))}
                    </select>
                  </div>
                )}
                <button
                  type="button"
                  onClick={removeSelectedWidget}
                  className={`w-full px-2 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-trash" aria-hidden /> Remove from cell
                </button>
              </div>
            )}
          </div>
        )}

        <div
          className={`h-full w-1 flex-auto min-w-0 overflow-auto p-2 ${theme.mainContentSection}`}
          style={{
            backgroundImage:
              'linear-gradient(rgba(148,163,184,0.12) 1px, transparent 1px), linear-gradient(90deg, rgba(148,163,184,0.12) 1px, transparent 1px)',
            backgroundSize: '16px 16px',
          }}
        >
          <div className="flex flex-col gap-2 min-h-full w-full">
            {rows.length === 0 && (
              <div className={`text-sm ${theme.label} p-4`}>No layout rows. Use Initialize Layout to add a grid.</div>
            )}
            {rows.map((row: any) => {
              const cols = row.ChildDesktopItems || [];
              const totalSpan = Math.max(1, sumRowColSpan(row));
              return (
                <div
                  key={row.Id}
                  className="flex flex-row gap-2 w-full items-stretch"
                  style={rowHeightStyle(row)}
                >
                  {cols.map((col: any) => {
                    const span = Math.max(1, Math.min(24, Number(col.ColSpanValue ?? col.ColumnSpan ?? 1) || 1));
                    const pct = (span / totalSpan) * 100;
                    const w = col.DesktopWidget;
                    const hoverKey = `${row.Id}|${col.Id}`;
                    const isSel =
                      selectedCell &&
                      String(selectedCell.rowId) === String(row.Id) &&
                      String(selectedCell.colId) === String(col.Id);
                    const nestedRoot = getNestedResponsiveRoot(col as any);
                    const isGhost = Boolean((col as any).__editorGhost);
                    return (
                      <div
                        key={col.Id}
                        data-cell-key={hoverKey}
                        className={`relative shrink-0 rounded-md border p-2 min-h-[120px] flex flex-col ${theme.inputBox} ${
                          dragOverKey === hoverKey ? 'ring-2 ring-blue-400 ring-offset-1' : ''
                        } ${isSel ? 'ring-2 ring-amber-500/80' : ''} ${isGhost ? 'opacity-0 pointer-events-none' : ''}`}
                        style={{
                          width: `${pct}%`,
                          minWidth: col?.MinWidth != null ? `${Math.max(0, Number(col.MinWidth) || 0)}px` : undefined,
                          height: 'auto',
                        }}
                        onClick={() => setSelectedCell({ rowId: String(row.Id), colId: String(col.Id) })}
                        onMouseEnter={() => setHoveredCellKey(hoverKey)}
                        onMouseLeave={() => setHoveredCellKey((k) => (k === hoverKey ? null : k))}
                        onDragOver={(e) => {
                          e.preventDefault();
                          e.stopPropagation();
                          if (dragOverKey !== hoverKey) setDragOverKey(hoverKey);
                        }}
                        onDragLeave={(e) => {
                          const related = (e as any).relatedTarget as HTMLElement | null;
                          const stillInside = related?.closest?.(`[data-cell-key="${hoverKey}"]`);
                          if (!stillInside && dragOverKey === hoverKey) setDragOverKey(null);
                        }}
                        onDrop={(e) => onDropCell(String(row.Id), String(col.Id), e)}
                      >
                        {dragOverKey === hoverKey && !isGhost && (
                          <div className="absolute inset-0 rounded-md border-2 border-dashed border-blue-400 pointer-events-none z-20" />
                        )}

                        {!isGhost && hoveredCellKey === hoverKey && (
                          <button
                            type="button"
                            className="absolute bottom-2 right-2 w-7 h-6 bg-slate-500 text-white rounded-[4px] text-xs hover:bg-slate-600 flex items-center justify-center z-30"
                            title="Cell Menu"
                            onClick={(e) => {
                              e.stopPropagation();
                              openCellContextMenu({ path: [], rowId: String(row.Id), colId: String(col.Id) }, e.clientX, e.clientY);
                            }}
                          >
                            <i className="fa-solid fa-ellipsis" aria-hidden />
                          </button>
                        )}

                        {nestedRoot && !isGhost && (
                          <div className={`h-full rounded-md border ${theme.mainContentSection} p-1.5 overflow-hidden`}>
                            <div className="flex flex-col gap-2 min-h-[80px]">
                              {(nestedRoot.ChildDesktopItems || []).map((nRow: any) => {
                                const nCols = nRow.ChildDesktopItems || [];
                                const nTotalSpan = Math.max(1, sumRowColSpan(nRow));
                                return (
                                  <div
                                    key={String(nRow.Id)}
                                    className="flex flex-row gap-2 w-full"
                                  >
                                    {nCols.map((nCol: any) => {
                                      const nSpan = Math.max(1, Math.min(24, Number(nCol.ColSpanValue ?? nCol.ColumnSpan ?? 1) || 1));
                                      const nPct = (nSpan / nTotalSpan) * 100;
                                      const nW = nCol.DesktopWidget;
                                      return (
                                        <div
                                          key={String(nCol.Id)}
                                          className={`rounded-md border p-2 min-h-[80px] flex flex-col ${theme.inputBox}`}
                                          style={{ width: `${nPct}%`, height: 'auto' }}
                                        >
                                          {!nW ? (
                                            <div className={`flex-auto flex items-center justify-center text-[11px] ${theme.label}`}>Empty</div>
                                          ) : (
                                            <div className="flex-auto flex flex-col overflow-hidden">
                                              <div className={`px-2 py-1 text-xs font-semibold border-b ${theme.title}`}>
                                                {clusterLeafLabel(nW, chartTypeList)}
                                              </div>
                                              <div className={`flex-auto flex items-center justify-center text-[11px] px-2 ${theme.label}`}>
                                                Nested cell
                                              </div>
                                            </div>
                                          )}
                                        </div>
                                      );
                                    })}
                                  </div>
                                );
                              })}
                            </div>
                          </div>
                        )}

                        {!w && (
                          <div className={`flex-auto flex items-center justify-center text-xs ${theme.label}`}>
                            Drop view here
                          </div>
                        )}
                        {w && Number(w.WidgetItemType) === WIDGET_CLUSTER && !nestedRoot && (
                          <div
                            className={`flex-auto rounded border flex flex-col min-h-[80px] ${theme.mainContentSection} cursor-move`}
                            draggable
                            onDragStart={(e) => {
                              const payload: DragPayloadV2 = {
                                kind: 'widget',
                                from: { path: [], rowId: String(row.Id), colId: String(col.Id) },
                                widget: w,
                              };
                              const data = JSON.stringify(payload);
                              e.dataTransfer.setData('text/plain', data);
                              try {
                                e.dataTransfer.setData('application/json', data);
                              } catch {
                                /* ignore */
                              }
                              e.dataTransfer.effectAllowed = 'move';
                              e.stopPropagation();
                            }}
                            onClick={(e) => {
                              e.stopPropagation();
                              setSelectedCell({ rowId: String(row.Id), colId: String(col.Id) });
                            }}
                          >
                            <div className={`px-2 py-1 text-xs font-semibold border-b ${theme.title}`}>
                              {clusterLeafLabel(w, chartTypeList)}
                            </div>
                            <div className="flex-auto overflow-hidden">
                              {(() => {
                                const childViewType = Number(w?.ViewType ?? 1);
                                let childColumns = normalizeChildColumns(
                                  w?.AppSearchViewFieldList ?? w?.Columns ?? w?.ClusterViewItemColumns ?? []
                                );
                                // Angular parity: chart child widget source paths come from AynalysisViewFieldX/Y.
                                // In design mode these paths may be missing on field items, so patch them in explicitly.
                                if (childViewType === 7) {
                                  const xPath = String(w?.AynalysisViewFieldX ?? '').trim();
                                  const yPath = String(w?.AynalysisViewFieldY ?? '').trim();
                                  childColumns = childColumns.map((c: any) => {
                                    const next = { ...c };
                                    const isX = Boolean(next?.IsMapToChartX === true || next?.IsMapToChartX === 1);
                                    const isY = Boolean(next?.IsMapToChartY === true || next?.IsMapToChartY === 1);
                                    if (!next.SysTableFiledPath) {
                                      if (isX && xPath) next.SysTableFiledPath = xPath;
                                      if (isY && yPath) next.SysTableFiledPath = yPath;
                                    }
                                    if (isY && (next.ControlType == null || Number(next.ControlType) === 0)) {
                                      next.ControlType = 20; // EmAppControlType.Numeric
                                    }
                                    return next;
                                  });
                                  const byPath = new Map<string, any>();
                                  childColumns.forEach((c: any) => {
                                    const p = String(c?.SysTableFiledPath ?? '').trim().toLowerCase();
                                    if (p) byPath.set(p, c);
                                  });
                                  if (xPath) {
                                    const xKey = xPath.toLowerCase();
                                    const xCol = byPath.get(xKey);
                                    if (xCol) {
                                      xCol.IsMapToChartX = true;
                                    } else {
                                      childColumns.push({
                                        Id: xPath,
                                        Name: xPath,
                                        Display: xPath,
                                        DisplayText: xPath,
                                        SysTableFiledPath: xPath,
                                        ControlType: 2,
                                        IsVisible: true,
                                        IsMapToChartX: true,
                                      });
                                    }
                                  }
                                  if (yPath) {
                                    const yKey = yPath.toLowerCase();
                                    const yCol = byPath.get(yKey);
                                    if (yCol) {
                                      yCol.IsMapToChartY = true;
                                      if (yCol.ControlType == null || Number(yCol.ControlType) === 0) yCol.ControlType = 20;
                                    } else {
                                      childColumns.push({
                                        Id: yPath,
                                        Name: yPath,
                                        Display: yPath,
                                        DisplayText: yPath,
                                        SysTableFiledPath: yPath,
                                        ControlType: 20,
                                        IsVisible: true,
                                        IsMapToChartY: true,
                                      });
                                    }
                                  }
                                  childColumns = normalizeChildColumns(childColumns);
                                }
                                const childViewDto: any = {
                                  ...w,
                                  ViewType: childViewType,
                                  Columns: childColumns,
                                  IsClusterChildView: true,
                                  UiId: w?.UiId,
                                };
                                const parentRows = Array.isArray(previewDataRows) ? previewDataRows : [];
                                const childRows = extractClusterChildRows(parentRows, w?.UiId);
                                const rowsForPreviewRaw = childRows.length
                                  ? childRows
                                  : projectParentRowsToChildRows(parentRows, childColumns, mainViewFields);
                                const rowsForPreview = adaptRowsForChildView(rowsForPreviewRaw, childColumns, mainViewFields);
                                if (!rowsForPreview.length) {
                                  return (
                                    <div className={`w-full h-full flex items-center justify-center text-[11px] px-2 ${theme.label}`}>
                                      No preview data. Use dataset/query then refresh.
                                    </div>
                                  );
                                }
                                if (childViewType === 7) {
                                  return (
                                    <ChartViewLayout
                                      viewDto={childViewDto}
                                      viewDataList={rowsForPreview}
                                      onExecuteSearch={async () => {}}
                                      onSelectionChanged={() => {}}
                                    />
                                  );
                                }
                                if (childViewType === 2) {
                                  return <CardViewLayout viewDto={childViewDto} viewDataList={rowsForPreview} />;
                                }
                                if (childViewType === 5) {
                                  return <PivotViewLayout viewDto={childViewDto} viewDataList={rowsForPreview} onExecuteSearch={async () => {}} />;
                                }
                                if (childViewType === 6) {
                                  return <CalendarViewLayout viewDto={childViewDto} viewDataList={rowsForPreview} onExecuteSearch={async () => {}} />;
                                }
                                if (childViewType === 15) {
                                  return <GanttViewLayout viewDto={childViewDto} viewDataList={rowsForPreview} onExecuteSearch={async () => {}} />;
                                }
                                if (childViewType === 16) {
                                  return <SchedulerViewLayout viewDto={childViewDto} viewDataList={rowsForPreview} onExecuteSearch={async () => {}} />;
                                }
                                return (
                                  <GridViewLayout
                                    viewDto={childViewDto}
                                    viewDataList={rowsForPreview}
                                    onExecuteSearch={async () => {}}
                                    onSelectionChanged={() => {}}
                                  />
                                );
                              })()}
                            </div>
                          </div>
                        )}
                        {!isGhost && w && Number(w.WidgetItemType) === WIDGET_CLUSTER && hoveredCellKey === hoverKey && (
                          <button
                            type="button"
                            className={`absolute bottom-2 right-10 w-7 h-6 rounded-[4px] text-xs ${theme.button_default} z-30`}
                            title="Config View"
                            onClick={(e) => {
                              e.preventDefault();
                              e.stopPropagation();
                              openChildConfigure(String(row.Id), String(col.Id), e.clientX - 10, e.clientY - 10);
                            }}
                          >
                            <i className="fa-solid fa-gear" aria-hidden />
                          </button>
                        )}

                      </div>
                    );
                  })}
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {initOpen && (
        <div
          className="fixed inset-0 z-[140] flex items-center justify-center bg-black/40"
          role="dialog"
          aria-modal="true"
          onClick={() => setInitOpen(false)}
        >
          <div
            className={`w-full max-w-sm rounded-md border shadow-lg p-4 ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Set Dashboard Layout</div>
            <div className="flex items-center gap-4 mb-3">
              <div>
                <label className={`text-xs ${theme.label} block`}>Rows</label>
                <input
                  type="number"
                  min={1}
                  max={20}
                  value={initRows}
                  onChange={(e) => setInitRows(Math.max(1, Math.min(20, Number(e.target.value) || 1)))}
                  className={`w-20 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
              <div>
                <label className={`text-xs ${theme.label} block`}>Columns</label>
                <input
                  type="number"
                  min={1}
                  max={12}
                  value={initCols}
                  onChange={(e) => setInitCols(Math.max(1, Math.min(12, Number(e.target.value) || 1)))}
                  className={`w-20 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                />
              </div>
            </div>
            <div className={`text-xs ${theme.label} mb-2`}>Preview {initRows} x {initCols}</div>
            <div className={`w-full rounded border ${theme.mainContentSection} p-2 mb-3`}>
              <div className="grid gap-2" style={{ gridTemplateColumns: `repeat(${Math.max(1, initCols)}, minmax(0, 1fr))` }}>
                {Array.from({ length: Math.max(1, initRows) * Math.max(1, initCols) }).map((_, i) => (
                  <div key={i} className="border rounded bg-transparent" style={{ height: '64px' }} />
                ))}
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setInitOpen(false)}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={applyInitLayout}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Apply
              </button>
            </div>
          </div>
        </div>
      )}

      {cellContextMenu?.open && (
        <div
          data-cluster-cell-menu="1"
          ref={cellMenuRef}
          className={`fixed z-[160] ${theme.mainContentSection} border rounded-[4px] shadow-lg p-3 w-[300px]`}
          style={{ left: cellContextMenu.x, top: cellContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {(() => {
            const addr = cellContextMenu.addr;
            const rootNow = getRootForMutation();
            const row = (rootNow.ChildDesktopItems || []).find((r: any) => String(r.Id) === String(addr.rowId)) as any;
            const col = (row?.ChildDesktopItems || []).find((c: any) => String(c.Id) === String(addr.colId)) as any;
            const span = Math.max(1, Math.min(24, Number(col?.ColSpanValue ?? col?.ColumnSpan ?? 1) || 1));
            const minW = Math.max(0, Number(col?.MinWidth ?? 0) || 0);
            const totalCols = row ? Math.max(1, sumRowColSpan(row)) : 24;
            const rowHeightRaw: any = row?.RowHeight ?? 'auto';
            const rowHeightNum = Number(rowHeightRaw);
            const rowHeightIsNum = Number.isFinite(rowHeightNum) && !Number.isNaN(rowHeightNum);

            const setSpan = (next: number) => setColumnSpanAt(addr.rowId, addr.colId, next);
            const setMinWidth = (next: number) => setColumnMinWidthAt(addr.rowId, addr.colId, next);

            return (
              <div className="space-y-3">
                <div className={`text-xs font-semibold ${theme.title}`}>Cell</div>

                <div className="flex items-center justify-between">
                  <div className={`text-xs ${theme.label}`}>Cell Width</div>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setSpan(span - 1)}
                      title="Decrease"
                    >
                      -
                    </button>
                    <div className={`w-10 text-center text-xs ${theme.title}`}>{span}</div>
                    <button
                      type="button"
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setSpan(span + 1)}
                      title="Increase"
                    >
                      +
                    </button>
                    <div className={`text-[11px] ${theme.label}`}>/{totalCols}</div>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className={`text-xs ${theme.label}`}>Warp Min Width</div>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setMinWidth(minW - 50)}
                      title="Decrease"
                    >
                      -
                    </button>
                    <input
                      className={`w-20 h-6 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                      value={String(minW)}
                      onChange={(e) => setMinWidth(Number(e.target.value))}
                    />
                    <div className={`text-[11px] ${theme.label}`}>px</div>
                    <button
                      type="button"
                      className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      onClick={() => setMinWidth(minW + 50)}
                      title="Increase"
                    >
                      +
                    </button>
                  </div>
                </div>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Merge Cell</div>
                  <div className="flex items-center gap-1">
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Merge Left" onClick={() => mergeColumnAt(addr.rowId, addr.colId, 'left')}>
                      <i className="fa-solid fa-arrow-left" aria-hidden />
                    </button>
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Merge Right" onClick={() => mergeColumnAt(addr.rowId, addr.colId, 'right')}>
                      <i className="fa-solid fa-arrow-right" aria-hidden />
                    </button>
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Merge Up" onClick={() => mergeVerticalAt(addr.rowId, addr.colId, 'up')}>
                      <i className="fa-solid fa-arrow-up" aria-hidden />
                    </button>
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Merge Down" onClick={() => mergeVerticalAt(addr.rowId, addr.colId, 'down')}>
                      <i className="fa-solid fa-arrow-down" aria-hidden />
                    </button>
                  </div>
                </div>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Vertical Split</div>
                  <div className="flex items-center gap-1">
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Split into 2" onClick={() => splitColumnAt(addr.rowId, addr.colId, 2)}>2</button>
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Split into 3" onClick={() => splitColumnAt(addr.rowId, addr.colId, 3)}>3</button>
                    <button type="button" className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center" title="Split into 4" onClick={() => splitColumnAt(addr.rowId, addr.colId, 4)}>4</button>
                  </div>
                </div>

                <div>
                  <div className={`text-xs ${theme.label} mb-1`}>Horizontal Insert</div>
                  <div className="flex items-center gap-1">
                    <button type="button" className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300" onClick={() => insertColumnRelativeAt(addr.rowId, addr.colId, 'left')}>Left</button>
                    <button type="button" className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300" onClick={() => insertColumnRelativeAt(addr.rowId, addr.colId, 'right')}>Right</button>
                    <button
                      type="button"
                      className="ml-auto w-8 h-7 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center justify-center"
                      title="Delete Cell"
                      onClick={() => {
                        deleteColumnAt(addr.rowId, addr.colId);
                        setCellContextMenu(null);
                      }}
                    >
                      <i className="fa-solid fa-trash" aria-hidden />
                    </button>
                  </div>
                </div>

                <div className="pt-2 border-t border-slate-200 dark:border-slate-700">
                  <div className={`text-xs font-semibold ${theme.title}`}>Row</div>
                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Row Height</div>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                        title="Decrease"
                        onClick={() => {
                          const next = rowHeightIsNum ? rowHeightNum - 50 : 300;
                          setRowHeightAt(addr.rowId, Math.max(50, next));
                        }}
                      >
                        -
                      </button>
                      <input
                        className={`w-20 h-6 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                        value={String(row?.RowHeight ?? '')}
                        onChange={(e) => {
                          const raw = e.target.value;
                          const asNum = Number(raw);
                          const nextVal: any =
                            raw.trim() === '' ? 'auto' : Number.isFinite(asNum) && !Number.isNaN(asNum) ? asNum : raw;
                          setRowHeightAt(addr.rowId, nextVal);
                        }}
                      />
                      <div className={`text-[11px] ${theme.label}`}>px</div>
                      <button
                        type="button"
                        className="w-7 h-6 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                        title="Increase"
                        onClick={() => {
                          const next = rowHeightIsNum ? rowHeightNum + 50 : 400;
                          setRowHeightAt(addr.rowId, next);
                        }}
                      >
                        +
                      </button>
                    </div>
                  </div>

                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Insert Row</div>
                    <div className="flex items-center gap-1">
                      <button type="button" className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300" onClick={() => insertRowRelativeAt(addr.rowId, 'above')}>Above</button>
                      <button type="button" className="px-2 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300" onClick={() => insertRowRelativeAt(addr.rowId, 'below')}>Below</button>
                      <button
                        type="button"
                        className="ml-auto w-8 h-7 bg-red-400 text-white rounded-[4px] text-xs hover:bg-red-500 flex items-center justify-center"
                        title="Delete Row"
                        onClick={() => {
                          deleteRowAt(addr.rowId);
                          setCellContextMenu(null);
                        }}
                      >
                        <i className="fa-solid fa-trash" aria-hidden />
                      </button>
                    </div>
                  </div>
                </div>

                <div className="pt-2 border-t border-slate-200 dark:border-slate-700">
                  <div className={`text-xs font-semibold ${theme.title}`}>Content</div>
                  <div className="flex items-center justify-between mt-2">
                    <div className={`text-xs ${theme.label}`}>Clear Content</div>
                    <button
                      type="button"
                      className="w-8 h-7 bg-gray-200 text-gray-700 rounded-[4px] text-xs hover:bg-gray-300 flex items-center justify-center"
                      title="Clear Content"
                      onClick={() => {
                        clearCellContentAt(addr.rowId, addr.colId);
                        setCellContextMenu(null);
                      }}
                    >
                      <i className="fa-solid fa-rotate" aria-hidden />
                    </button>
                  </div>
                </div>
              </div>
            );
          })()}
        </div>
      )}

      {childConfigurePopup?.open && selectedWidget && (
        <div
          className="fixed inset-0 z-[170] bg-transparent"
          role="dialog"
          aria-modal="true"
          onClick={() => setChildConfigurePopup(null)}
        >
          <div
            className={`fixed w-[420px] rounded-md border shadow-lg p-4 ${theme.mainContentSection}`}
            style={{ left: childConfigurePopup.x, top: childConfigurePopup.y }}
            onClick={(e) => e.stopPropagation()}
          >
            <button
              type="button"
              className="absolute top-1 right-2 text-base leading-none opacity-70 hover:opacity-100"
              aria-label="Close"
              onClick={() => setChildConfigurePopup(null)}
            >
              x
            </button>
            <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Configure Child View</div>
            <div className="space-y-3">
              <div>
                <label className={`text-xs ${theme.label} block mb-0.5`}>Display Title</label>
                <input
                  type="text"
                  value={selectedWidget.DisplayTitle ?? ''}
                  onChange={(e) => updateSelectedWidget({ DisplayTitle: e.target.value })}
                  className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                />
              </div>
              {Number(selectedWidget.ViewType) === 7 && (
                <>
                  <div>
                    <label className={`text-xs ${theme.label} block mb-0.5`}>Chart Label Field</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                      value={String(selectedWidget.AynalysisViewFieldX ?? '')}
                      onChange={(e) => updateSelectedWidget({ AynalysisViewFieldX: e.target.value || null })}
                    >
                      <option value="" />
                      {chartFieldOptions.map((f) => (
                        <option key={`x_${f.Id}`} value={f.Id}>
                          {f.Display}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className={`text-xs ${theme.label} block mb-0.5`}>Chart Value Field</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                      value={String(selectedWidget.AynalysisViewFieldY ?? '')}
                      onChange={(e) => updateSelectedWidget({ AynalysisViewFieldY: e.target.value || null })}
                    >
                      <option value="" />
                      {chartFieldOptions.map((f) => (
                        <option key={`y_${f.Id}`} value={f.Id}>
                          {f.Display}
                        </option>
                      ))}
                    </select>
                  </div>
                </>
              )}
              {Number(selectedWidget.ViewType) !== 7 && (
                <div className={`text-[11px] ${theme.label}`}>
                  For non-chart child views, Angular popup edits View Fields. Use the View Fields area in Search View editor.
                </div>
              )}
            </div>
            <div className="mt-4 flex justify-end gap-2">
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => setChildConfigurePopup(null)}
              >
                Cancel
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => {
                  setChildConfigurePopup(null);
                  void onSave();
                }}
              >
                <i className="fa-solid fa-clone" aria-hidden /> Apply &amp; Close
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ClusterAnalysisViewLayoutEditor;
