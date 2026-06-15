import React, { useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { GridViewLayout } from './GridViewLayout';
import { CardViewLayout } from './CardViewLayout';
import { CalendarViewLayout } from './CalendarViewLayout';
import { GanttViewLayout } from './GanttViewLayout';
import { PivotViewLayout } from './PivotViewLayout';
import { ChartViewLayout } from './ChartViewLayout';
import { SchedulerViewLayout } from './SchedulerViewLayout';

type ClusterAnalysisViewLayoutProps = {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
};

const FLEX_WIDGET_TYPES = {
  responsive: 100,
  row: 101,
  col: 102,
} as const;

// Backend: EmAppDashboardWidgetItemType.ClusterAnalysisViewItem = 9
const CLUSTER_CHILD_WIDGET_TYPE = 9;

function getResponsiveRoot(flexItems: any[] | null | undefined) {
  const list = Array.isArray(flexItems) ? flexItems : [];
  return list.find((x) => x && Number(x.WidgetItemType) === FLEX_WIDGET_TYPES.responsive) ?? null;
}

function getNestedResponsiveRoot(col: any) {
  const list = Array.isArray(col?.ChildDesktopItems) ? col.ChildDesktopItems : [];
  return list.find((x: any) => x && Number(x.WidgetItemType) === FLEX_WIDGET_TYPES.responsive) ?? null;
}

function getSpan(col: any): number {
  const raw = Number(col?.ColSpanValue ?? col?.ColumnSpan ?? 1);
  if (!Number.isFinite(raw)) return 1;
  return Math.max(1, Math.min(24, raw));
}

function sumRowColSpan(row: any): number {
  const cols = Array.isArray(row?.ChildDesktopItems) ? row.ChildDesktopItems : [];
  return cols.reduce((acc: number, c: any) => acc + getSpan(c), 0);
}

function rowHeightStyle(row: any): React.CSSProperties {
  const raw = row?.RowHeight ?? 'auto';
  if (raw == null) return {};
  if (typeof raw === 'number') return { height: `${Math.max(0, raw)}px` };
  const s = String(raw).trim().toLowerCase();
  if (s === '' || s === 'auto') return {};
  const asNum = Number(s);
  if (Number.isFinite(asNum) && !Number.isNaN(asNum)) return { height: `${Math.max(0, asNum)}px` };
  return { height: String(raw) };
}

function normalizeColumns(cols: any[]): any[] {
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
}

function extractChildRows(viewDataList: any[], childUiId: string | undefined | null) {
  if (!childUiId) return [];
  const normalizeOne = (rowLike: any) => {
    if (!rowLike || typeof rowLike !== 'object') return null;
    if (rowLike.DictViewColumnIDKeyValue) return rowLike;
    if (rowLike.Row && rowLike.Row.DictViewColumnIDKeyValue) return rowLike.Row;
    if (rowLike.Data && rowLike.Data.DictViewColumnIDKeyValue) return rowLike.Data;
    return rowLike;
  };
  const out: any[] = [];
  for (const parentRow of Array.isArray(viewDataList) ? viewDataList : []) {
    let dict: any = parentRow?.DictClusterChildViewUiIdAndResultRowJsonDto;
    if (typeof dict === 'string') {
      try {
        dict = JSON.parse(dict);
      } catch {
        dict = null;
      }
    }
    if (!dict || typeof dict !== 'object') continue;
    const byKey = dict?.[String(childUiId)];
    const fallback = byKey ?? Object.values(dict)[0];
    const normalized = normalizeOne(fallback);
    if (normalized) out.push(normalized);
  }
  return out;
}

function projectParentRowsToChildRows(parentRows: any[], childColumns: any[], mainColumns: any[]) {
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
}

function adaptRowsForChildView(rows: any[], childColumns: any[], mainColumns: any[]) {
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
}

function renderClusterWidget(opts: {
  widget: any;
  cellKey: string;
  widthPct: number;
  viewDataList: any[];
  mainViewColumns?: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
}) {
  const { widget, cellKey, widthPct, viewDataList, mainViewColumns, dataModel, onExecuteSearch, onSelectionChanged } = opts;
  const widgetType = widget?.WidgetItemType != null ? Number(widget.WidgetItemType) : null;
  if (!widget || widgetType !== CLUSTER_CHILD_WIDGET_TYPE) {
    return (
      <div
        key={cellKey}
        className="h-full min-h-[120px] flex flex-col rounded-md border border-dashed border-slate-300 dark:border-slate-700 items-center justify-center flex-auto"
        style={{ width: `${widthPct}%` }}
      >
        <div className={`text-[11px] text-slate-500 dark:text-slate-400`}>Drop / empty</div>
      </div>
    );
  }

  const childViewType = widget?.ViewType != null ? Number(widget.ViewType) : 1;
  const childUiId = widget?.UiId;
  const childColumns = normalizeColumns(widget?.AppSearchViewFieldList ?? widget?.Columns ?? widget?.ClusterViewItemColumns ?? []);
  const childRowsByUi = extractChildRows(viewDataList, childUiId);
  const childViewDataListRaw = childRowsByUi.length
    ? childRowsByUi
    : projectParentRowsToChildRows(viewDataList, childColumns, mainViewColumns ?? []);
  const childViewDataList = adaptRowsForChildView(childViewDataListRaw, childColumns, mainViewColumns ?? []);
  const childViewDto = {
    ...widget,
    ViewType: childViewType,
    ChartType: widget?.ChartType ?? null,
    Columns: childColumns,
    IsClusterChildView: true,
    UiId: childUiId,
  };

  switch (childViewType) {
    case 1:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <GridViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} onSelectionChanged={onSelectionChanged} />
        </div>
      );
    case 2:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <CardViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onSelectionChanged={onSelectionChanged} />
        </div>
      );
    case 5:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <PivotViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} />
        </div>
      );
    case 6:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <CalendarViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} />
        </div>
      );
    case 7:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <ChartViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} onSelectionChanged={onSelectionChanged} />
        </div>
      );
    case 15:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <GanttViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} />
        </div>
      );
    case 16:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <SchedulerViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} />
        </div>
      );
    default:
      return (
        <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden" style={{ width: `${widthPct}%` }}>
          <GridViewLayout viewDto={childViewDto} viewDataList={childViewDataList} dataModel={dataModel} onExecuteSearch={onExecuteSearch} onSelectionChanged={onSelectionChanged} />
        </div>
      );
  }
}

function renderResponsiveLayout(opts: {
  responsive: any;
  viewDataList: any[];
  mainViewColumns?: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
}) {
  const { responsive, viewDataList, mainViewColumns, dataModel, onExecuteSearch, onSelectionChanged } = opts;
  const rows = responsive?.ChildDesktopItems ?? [];
  return (
    <div className="w-full h-full min-h-0 flex flex-col gap-2">
      {rows.map((row: any, rowIndex: number) => {
        const cols = row?.ChildDesktopItems ?? [];
        return (
          <div
            key={`r_${String(row?.Id ?? '_')}_${rowIndex}`}
            className="w-full flex flex-row gap-2 flex-auto min-h-0 overflow-hidden items-stretch"
            style={rowHeightStyle(row)}
          >
              {cols.map((col: any, colIndex: number) => {
              const span = getSpan(col);
              const totalSpan = Math.max(1, sumRowColSpan(row));
              const pct = (span / totalSpan) * 100;
              const cellKey = `${String(row?.Id ?? '_')}_${rowIndex}_${String(col?.Id ?? '_')}_${colIndex}`;
              const nested = getNestedResponsiveRoot(col);
              if (nested) {
                return (
                  <div key={cellKey} className="h-full min-h-[120px] flex-auto overflow-hidden rounded-md border border-slate-200 dark:border-slate-700 p-1" style={{ width: `${pct}%` }}>
                    {renderResponsiveLayout({ responsive: nested, viewDataList, mainViewColumns, dataModel, onExecuteSearch, onSelectionChanged })}
                  </div>
                );
              }
              return renderClusterWidget({
                widget: col?.DesktopWidget ?? null,
                cellKey,
                widthPct: pct,
                viewDataList,
                mainViewColumns,
                dataModel,
                onExecuteSearch,
                onSelectionChanged,
              });
            })}
          </div>
        );
      })}
    </div>
  );
}

export const ClusterAnalysisViewLayout: React.FC<ClusterAnalysisViewLayoutProps> = ({
  viewDto,
  viewDataList,
  dataModel,
  onExecuteSearch,
  onSelectionChanged,
}) => {
  const { theme } = useTheme();

  const flexItems = viewDto?.OtherSettingsDto?.FlexLayoutItems ?? [];
  const responsive = useMemo(() => getResponsiveRoot(flexItems), [flexItems]);

  if (!responsive) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.label} text-sm`}>
        Cluster Analysis View: missing flex layout definition.
      </div>
    );
  }

  return (
    <div className="w-full h-full min-h-0 flex flex-col overflow-hidden">
      <div className="w-full h-full min-h-0 flex flex-col gap-2 p-1">
        {renderResponsiveLayout({
          responsive,
          viewDataList,
          mainViewColumns: viewDto?.AppSearchViewFieldList ?? [],
          dataModel,
          onExecuteSearch,
          onSelectionChanged,
        })}
      </div>
    </div>
  );
};

