import React, { useMemo, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import type { DataRenderEvent } from '../../webapi/genericAgentSvc';
import { DataRenderChartWijmo } from './dataRender/DataRenderChartWijmo';
import { DataRenderGridWijmo } from './dataRender/DataRenderGridWijmo';
import { DataRenderKpiRow } from './dataRender/DataRenderKpiRow';
import TemplateControlLayout, {
    parseDashboardLayoutsFromSpec,
    useReboundKpiHtml,
} from './TemplateControlLayout';
import TemplateHostMount from './TemplateHostMount';

type KpiSpec = { label?: string; Label?: string; field?: string; Field?: string; op?: string; Op?: string };
type ChartSpec = {
    type?: string; Type?: string;
    xField?: string; XField?: string;
    yField?: string; YField?: string;
    title?: string; Title?: string;
};
type Spec = {
    title?: string;
    kpis?: KpiSpec[];
    chart?: ChartSpec;
    charts?: ChartSpec[];
    blocks?: { type?: string; Type?: string; chart?: ChartSpec; Chart?: ChartSpec; xField?: string; XField?: string; title?: string }[];
    availableChartTypes?: string;
};

export const CHART_TYPES = ['bar', 'hbar', 'line', 'scatter', 'pie', 'donut', 'area'] as const;

function parseJson<T>(raw: string | null | undefined, fallback: T): T {
    if (!raw?.trim()) return fallback;
    try {
        return JSON.parse(raw) as T;
    } catch {
        return fallback;
    }
}

function distinctCount(rows: Record<string, unknown>[], field: string): number {
    const s = new Set<string>();
    for (const r of rows) {
        const v = r?.[field];
        if (v != null && String(v).trim() !== '') s.add(String(v));
    }
    return s.size;
}

function avgLabelLen(rows: Record<string, unknown>[], field: string): number {
    const labels: string[] = [];
    const seen = new Set<string>();
    for (const r of rows) {
        const v = r?.[field];
        if (v == null) continue;
        const s = String(v).trim();
        if (!s || seen.has(s)) continue;
        seen.add(s);
        labels.push(s);
        if (labels.length > 40) break;
    }
    if (!labels.length) return 0;
    return labels.reduce((a, b) => a + b.length, 0) / labels.length;
}

/** Fallback when LLM omitted chart.type — mirrors C# SuggestChartType. */
export function suggestChartType(rows: Record<string, unknown>[], xField: string): string {
    const name = (xField || '').toLowerCase();
    if (/date|time|period|month|year|week|day|season|_dt$/.test(name)) return 'line';
    const d = distinctCount(rows, xField);
    const avg = avgLabelLen(rows, xField);
    if (d <= 5 && avg <= 24) return d <= 4 ? 'donut' : 'pie';
    if (d >= 8 || avg >= 14) return 'hbar';
    return 'bar';
}

function normalizeType(t: string | undefined | null): string {
    const x = String(t || '').trim().toLowerCase();
    if (!x || x === 'chart' || x === 'kpi') return '';
    if (x === 'column' || x === 'vbar' || x === 'bar_v') return 'bar';
    if (x === 'bar_h' || x === 'horizontal' || x === 'horizontalbar') return 'hbar';
    if (x === 'point' || x === 'dots') return 'scatter';
    if (x === 'doughnut') return 'donut';
    return CHART_TYPES.indexOf(x as typeof CHART_TYPES[number]) >= 0 ? x : '';
}

/** StyleCount → Style Count; empty label specs fall back to this. */
function humanizeKpiField(field: string): string {
    const s = (field || '').trim();
    if (!s) return '';
    return s
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/[_-]+/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}

function kpiValue(rows: Record<string, unknown>[], k: KpiSpec): string {
    const op = String(k.op ?? k.Op ?? 'sum').toLowerCase();
    const field = String(k.field ?? k.Field ?? '').trim();
    if (op === 'count' || op === 'rows') return String(rows.length);
    if (!field) return '—';
    if (op === 'distinct' || op === 'nunique' || op === 'unique') return String(distinctCount(rows, field));
    const nums: number[] = [];
    for (const r of rows) {
        const t = r?.[field];
        if (t == null) continue;
        const n = Number(t);
        if (!Number.isNaN(n) && !/[A-Za-z]/.test(String(t))) nums.push(n);
    }
    if (!nums.length) return String(distinctCount(rows, field));
    let v = 0;
    if (op === 'avg' || op === 'average' || op === 'mean') v = nums.reduce((a, b) => a + b, 0) / nums.length;
    else if (op === 'min') v = Math.min(...nums);
    else if (op === 'max') v = Math.max(...nums);
    else v = nums.reduce((a, b) => a + b, 0);
    return Math.abs(v) >= 1000 ? Math.round(v).toLocaleString() : String(Math.round(v * 100) / 100);
}

function isNumericField(rows: Record<string, unknown>[], field: string): boolean {
    let n = 0;
    for (const r of rows) {
        const t = r?.[field];
        if (t == null || String(t).trim() === '') continue;
        if (/[A-Za-z]/.test(String(t))) return false;
        if (Number.isNaN(Number(t))) return false;
        n++;
        if (n >= 8) break;
    }
    return n > 0;
}

/** True when each x category appears ~once (SQL GROUP BY result already aggregated). */
function looksPreAggregated(rows: Record<string, unknown>[], xField: string): boolean {
    if (!rows.length || rows.length > 200) return false;
    const d = distinctCount(rows, xField);
    return d >= 2 && d === rows.length;
}

/**
 * Prefer an explicit measure (TotalCount, Count, …) over row-count when the dataset
 * is already GROUP BY'd — otherwise every bar is height 1.
 */
function inferYField(
    rows: Record<string, unknown>[],
    xField: string,
    preferred?: string | null,
): string | null {
    const pref = String(preferred || '').trim();
    if (pref && !/^_?count$/i.test(pref) && isNumericField(rows, pref)) return pref;

    if (!looksPreAggregated(rows, xField) && rows.length > 20) return null;

    const keys = rows[0] ? Object.keys(rows[0]).filter(k => k !== xField) : [];
    const preferredNames = [
        'TotalCount', 'total_count', 'RecordCount', 'record_count',
        'Count', 'Cnt', 'Qty', 'Quantity', 'Amount', 'Sum', 'Total',
    ];
    for (const name of preferredNames) {
        const hit = keys.find(k => k.toLowerCase() === name.toLowerCase());
        if (hit && isNumericField(rows, hit)) return hit;
    }
    const countish = keys.find(k => /count|total|sum|qty|amount/i.test(k) && isNumericField(rows, k));
    if (countish) return countish;
    if (looksPreAggregated(rows, xField)) {
        const nums = keys.filter(k => isNumericField(rows, k));
        if (nums.length === 1) return nums[0];
    }
    return null;
}

function seriesFromRows(
    rows: Record<string, unknown>[],
    xField: string,
    yField: string | null,
): Record<string, unknown>[] {
    if (!yField) {
        const map = new Map<string, number>();
        for (const r of rows) {
            const x = r?.[xField];
            if (x == null || String(x).trim() === '') continue;
            const key = String(x);
            map.set(key, (map.get(key) ?? 0) + 1);
        }
        return Array.from(map.entries())
            .map(([x, y]) => ({ x, y }))
            .sort((a, b) => (b.y as number) - (a.y as number))
            .slice(0, 40);
    }
    const map = new Map<string, number>();
    for (const r of rows) {
        const x = r?.[xField];
        if (x == null || String(x).trim() === '') continue;
        const key = String(x);
        const n = Number(r?.[yField]);
        if (Number.isNaN(n)) continue;
        map.set(key, (map.get(key) ?? 0) + n);
    }
    return Array.from(map.entries())
        .map(([x, y]) => ({ x, y }))
        .sort((a, b) => (b.y as number) - (a.y as number))
        .slice(0, 40);
}

type ResolvedChart = { field: string; yField: string | null; label: string; type: string };

function resolveCharts(spec: Spec, rows: Record<string, unknown>[]): ResolvedChart[] {
    const out: ResolvedChart[] = [];
    const seen = new Set<string>();

    const push = (fieldRaw: string, label: string, typeRaw?: string, yRaw?: string) => {
        const field = String(fieldRaw || '').trim();
        if (!field || seen.has(field)) return;
        const d = distinctCount(rows, field);
        if (d < 2 || d > 40) return;
        if (rows.length >= 20 && d >= rows.length * 0.8) return;
        seen.add(field);
        const typed = normalizeType(typeRaw);
        const yField = inferYField(rows, field, yRaw);
        out.push({
            field,
            yField,
            label: label || field,
            type: typed || suggestChartType(rows, field),
        });
    };

    for (const c of spec.charts ?? []) {
        push(
            String(c.xField ?? c.XField ?? ''),
            String(c.title ?? c.Title ?? ''),
            c.type ?? c.Type,
            String(c.yField ?? c.YField ?? ''),
        );
        if (out.length >= 3) return out;
    }
    if (spec.chart) {
        push(
            String(spec.chart.xField ?? spec.chart.XField ?? ''),
            String(spec.chart.title ?? spec.chart.Title ?? ''),
            spec.chart.type ?? spec.chart.Type,
            String(spec.chart.yField ?? spec.chart.YField ?? ''),
        );
    }
    for (const b of spec.blocks ?? []) {
        const t = String(b.type ?? b.Type ?? '').toLowerCase();
        if (t !== 'chart') continue;
        const c = b.chart ?? b.Chart;
        push(
            String(c?.xField ?? c?.XField ?? b.xField ?? b.XField ?? ''),
            String(c?.title ?? c?.Title ?? b.title ?? ''),
            c?.type ?? c?.Type,
            String(c?.yField ?? c?.YField ?? ''),
        );
        if (out.length >= 3) return out;
    }
    if (out.length > 0) return out;

    for (const k of spec.kpis ?? []) {
        const op = String(k.op ?? k.Op ?? '').toLowerCase();
        if (op === 'count' || op === 'rows') continue;
        push(String(k.field ?? k.Field ?? ''), String(k.label ?? k.Label ?? ''));
        if (out.length >= 3) break;
    }
    return out;
}

interface Props {
    event: DataRenderEvent;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

/**
 * Interactive KPI/dashboard — React owns stack/filter/Wijmo; each control LAYOUT from TemplateContent
 * (kpiHtml / chartHostHtml / gridHostHtml), same pattern as Interactive page.
 */
const InteractiveDashboardPanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme } = useTheme();
    const rows = useMemo(
        () => parseJson<Record<string, unknown>[]>(event.DataJson, []),
        [event.DataJson],
    );
    const spec = useMemo(() => parseJson<Spec>(event.SpecJson, {}), [event.SpecJson]);
    const layouts = useMemo(() => parseDashboardLayoutsFromSpec(event.SpecJson), [event.SpecJson]);
    const charts = useMemo(() => resolveCharts(spec, rows), [spec, rows]);
    const [dimFilter, setDimFilter] = useState('');

    const filterField = charts[0]?.field ?? '';

    const filteredRows = useMemo(() => {
        if (!dimFilter || !filterField) return rows;
        return rows.filter(r => String(r?.[filterField] ?? '') === dimFilter);
    }, [rows, dimFilter, filterField]);

    const kpiItems = useMemo(
        () =>
            (spec.kpis ?? []).slice(0, 12).map(k => {
                const field = String(k.field ?? k.Field ?? '').trim();
                // Empty string label must not block field fallback (?? only skips null/undefined).
                const rawLabel = String(k.label || k.Label || '').trim();
                const label = rawLabel || humanizeKpiField(field) || field || 'KPI';
                return {
                    label,
                    value: kpiValue(filteredRows, k),
                    field,
                };
            }),
        [spec.kpis, filteredRows],
    );

    const kpiHtml = useReboundKpiHtml(layouts.kpiHtml, kpiItems);

    const filterOptions = useMemo(() => {
        if (!filterField) return [] as string[];
        const s = new Set<string>();
        for (const r of rows) {
            const v = r?.[filterField];
            if (v != null && String(v).trim() !== '') s.add(String(v));
        }
        return Array.from(s).sort();
    }, [rows, filterField]);

    const polishedColumns = useMemo(() => {
        const root = parseJson<Record<string, unknown>>(event.SpecJson, {});
        const polished = (root.polishedSpec || root.PolishedSpec) as {
            bindings?: { columns?: string[]; selectAllColumns?: boolean };
            selectAllColumns?: boolean;
        } | null;
        const cols = polished?.bindings?.columns;
        if (!Array.isArray(cols) || cols.length === 0) return null;
        if (polished?.selectAllColumns || polished?.bindings?.selectAllColumns) return null;
        return cols.map(String);
    }, [event.SpecJson]);

    const colDefs = useMemo(() => {
        const first = filteredRows[0];
        if (!first) return [];
        const keys = polishedColumns?.length
            ? polishedColumns.filter(k => k in first)
            : Object.keys(first);
        const useKeys = keys.length > 0 ? keys : Object.keys(first);
        return useKeys.map(field => ({ field, headerName: field }));
    }, [filteredRows, polishedColumns]);

    const btn = `px-2 py-0.5 text-xs rounded-[4px] border ${theme.button_default}`;
    // Title + Print/Export live in ResultChrome (same as page/cards Result).
    const sectionCount =
        (kpiItems.length > 0 ? 1 : 0) + charts.length + 1; /* grid always */
    const useCollapse = sectionCount >= 2;

    const chartBody = (cf: ResolvedChart) => {
        const yLabel = cf.yField || 'count';
        const chartTitle = cf.label.includes(yLabel) || /count/i.test(cf.label)
            ? cf.label
            : `${cf.label} (${yLabel})`;
        const chart = (
            <DataRenderChartWijmo
                meta={{ title: chartTitle }}
                chartConfig={{
                    type: cf.type,
                    xField: 'x',
                    yField: 'y',
                    allowedTypes: [...CHART_TYPES],
                }}
                data={seriesFromRows(filteredRows, cf.field, cf.yField)}
            />
        );
        if (layouts.chartHostHtml) {
            return (
                <TemplateHostMount html={layouts.chartHostHtml} hostId="chartHost">
                    {chart}
                </TemplateHostMount>
            );
        }
        return chart;
    };

    const gridBody = (
        <div style={{ maxHeight: 'min(80vh, 800px)', overflow: 'auto' }}>
            <DataRenderGridWijmo
                colDefs={colDefs}
                rowData={filteredRows}
                title="Data"
                meta={{ total: filteredRows.length }}
            />
        </div>
    );

    return (
        <div className={`w-full h-full overflow-auto p-2 ${theme.mainContentSection}`}>
            {filterField && (
                <div className="flex flex-wrap items-center gap-2 mb-3">
                    <label className={`text-xs flex items-center gap-1 ${theme.label}`}>
                        Filter {filterField}:
                        <select
                            className={`text-xs border rounded px-1 py-0.5 ${theme.inputBox}`}
                            value={dimFilter}
                            disabled={disabled}
                            onChange={e => setDimFilter(e.target.value)}
                        >
                            <option value="">(all)</option>
                            {filterOptions.map(o => (
                                <option key={o} value={o}>{o}</option>
                            ))}
                        </select>
                    </label>
                </div>
            )}

            {kpiItems.length > 0 && (
                <DashboardSection title="KPIs" collapse={useCollapse}>
                    {kpiHtml ? (
                        <TemplateControlLayout html={kpiHtml} />
                    ) : (
                        <DataRenderKpiRow items={kpiItems} />
                    )}
                </DashboardSection>
            )}

            {charts.map(cf => {
                const yLabel = cf.yField || 'count';
                const chartTitle = cf.label.includes(yLabel) || /count/i.test(cf.label)
                    ? cf.label
                    : `${cf.label} (${yLabel})`;
                return (
                    <DashboardSection key={cf.field} title={chartTitle} collapse={useCollapse}>
                        {chartBody(cf)}
                    </DashboardSection>
                );
            })}

            <DashboardSection title="Grid" collapse={useCollapse}>
                {layouts.gridHostHtml ? (
                    <TemplateHostMount html={layouts.gridHostHtml} hostId="gridHost">
                        {gridBody}
                    </TemplateHostMount>
                ) : (
                    gridBody
                )}
            </DashboardSection>

            {event.ActionsJson && onAction && (
                <div className="flex flex-wrap gap-1 mt-3">
                    {parseJson<{ id?: string; Id?: string; label?: string; Label?: string }[]>(event.ActionsJson, []).map(a => {
                        const id = a.id ?? a.Id;
                        if (!id) return null;
                        return (
                            <button
                                key={id}
                                type="button"
                                className={btn}
                                disabled={disabled}
                                onClick={() => onAction(id)}
                            >
                                {a.label ?? a.Label ?? id}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
};

/** Platform chrome (not TemplateContent): collapse when stacking 2+ controls. */
const DashboardSection: React.FC<{
    title: string;
    collapse: boolean;
    children: React.ReactNode;
}> = ({ title, collapse, children }) => {
    if (!collapse) {
        return <div className="mb-3">{children}</div>;
    }
    return (
        <details open className="mb-3 rounded-[4px] border border-gray-300 bg-white">
            <summary className="cursor-pointer select-none px-3 py-2 text-sm font-semibold list-none">
                {title}
            </summary>
            <div className="px-3 pb-3">{children}</div>
        </details>
    );
};

export default InteractiveDashboardPanel;

export function isInteractiveDashboard(event: DataRenderEvent | null | undefined): boolean {
    if (!event) return false;
    const mode = (event.Mode || '').toLowerCase();
    if (mode !== 'interactive') return false;
    // chart-only → TemplateContent iframe; kpi/dashboard → React + TemplateContent control LAYOUTs.
    // Persisted/reload may only have Ui=kpi_dashboard when Layout was omitted.
    const layout = (event.Layout || '').toLowerCase();
    const ui = (event.Ui || '').toLowerCase();
    const isDash = layout === 'kpi' || layout === 'dashboard'
        || ui === 'kpi_dashboard' || ui === 'kpi' || ui === 'dashboard';
    if (!isDash) return false;
    return !!(event.DataJson && event.SpecJson);
}
