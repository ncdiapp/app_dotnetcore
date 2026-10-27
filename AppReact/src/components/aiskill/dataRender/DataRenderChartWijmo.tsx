import React, { useEffect, useMemo, useState } from 'react';
import { FlexChart, FlexChartSeries, FlexPie } from '@mescius/wijmo.react.chart';
import { ChartElement, ChartType, Position } from '@mescius/wijmo.chart';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { DataRenderChartConfig, DataRenderMeta } from './types';

interface Props {
    meta?: DataRenderMeta;
    chartConfig: DataRenderChartConfig;
    data: Record<string, unknown>[];
}

const PALETTE = [
    '#2563eb', '#dc2626', '#059669', '#d97706', '#7c3aed',
    '#0891b2', '#db2777', '#65a30d', '#ea580c', '#4f46e5',
];

function mapTypeToWijmo(type: string): ChartType {
    const t = (type || 'bar').toLowerCase();
    if (t === 'line') return ChartType.Line;
    if (t === 'area') return ChartType.Area;
    if (t === 'hbar' || t === 'bar_h') return ChartType.Bar;
    if (t === 'scatter' || t === 'point') return ChartType.Scatter;
    // bar / column → vertical
    return ChartType.Column;
}

const AXIS_LABEL_SIZE = '9px';
const AXIS_LABEL_COLOR = '#6b7280';
const AXIS_LABEL_MAX = 22;
/** Pie/donut legend — ~2 sizes below default (~12–13px). */
const PIE_LEGEND_FONT_SIZE = '9px';

function shortLabel(text: unknown, max = AXIS_LABEL_MAX): string {
    const s = text == null ? '' : String(text);
    if (s.length <= max) return s;
    return s.slice(0, Math.max(1, max - 1)) + '…';
}

/** Axis has no fontSize prop — set size/color on the render engine in itemFormatter. */
function applyAxisLabels(chart: any) {
    if (!chart) return;
    for (const ax of [chart.axisX, chart.axisY].filter(Boolean)) {
        try { ax.labelPadding = 2; } catch { /* ignore */ }
        ax.itemFormatter = (engine: any, label: any) => {
            if (engine) {
                engine.fontSize = AXIS_LABEL_SIZE;
                engine.textFill = AXIS_LABEL_COLOR;
            }
            if (label && label.text != null) label.text = shortLabel(label.text);
            return label;
        };
    }
}

/** Default Wijmo chart implementation for data_render (licensed via AppReact license.js). */
export const DataRenderChartWijmo: React.FC<Props> = ({ meta, chartConfig, data }) => {
    const { theme, t } = useTheme();
    const allowedTypes = chartConfig.allowedTypes ?? ['bar', 'line', 'area', 'pie'];
    // chartConfig.type = LLM / suggested default; user can switch via allowedTypes picker
    const [chartType, setChartType] = useState(chartConfig.type ?? allowedTypes[0] ?? 'bar');

    useEffect(() => {
        if (chartConfig.type) setChartType(chartConfig.type);
    }, [chartConfig.type]);

    const effectiveType = chartType;

    const xField = chartConfig.xField ?? 'period';
    const yField = chartConfig.yField ?? 'value';
    const groupBy = chartConfig.groupBy;

    const seriesKeys = useMemo(() => {
        if (!groupBy) return [yField];
        return Array.from(new Set(data.map(d => String(d[groupBy] ?? '')))).filter(Boolean);
    }, [data, groupBy, yField]);

    const chartData = useMemo(() => {
        if (!groupBy) return data;
        const acc: Record<string, Record<string, unknown>> = {};
        for (const row of data) {
            const key = String(row[xField] ?? '');
            if (!acc[key]) acc[key] = { [xField]: key };
            const series = String(row[groupBy] ?? '');
            acc[key][series] = (Number(acc[key][series] ?? 0) || 0) + Number(row[yField] ?? 0);
        }
        return Object.values(acc);
    }, [data, groupBy, xField, yField]);

    const btn = `px-2 py-0.5 text-xs rounded-[4px] border ${theme.button_default}`;
    const tLower = (effectiveType || '').toLowerCase();
    const isPie = tLower === 'pie' || tLower === 'donut';
    const chartHostStyle: React.CSSProperties = {
        width: '100%',
        height: '100%',
        border: 'none',
        outline: 'none',
    };

    const onChartInitialized = (s: any) => {
        const chart = s?.hostElement ? s : s;
        if (!chart || isPie) return;
        try {
            chart.palette = PALETTE;
            applyAxisLabels(chart);
            // Single series: color each point/bar
            chart.itemFormatter = (engine: any, ht: any, defaultRender: () => void) => {
                if (ht && ht.pointIndex >= 0 && ht.chartElement === ChartElement.SeriesSymbol) {
                    const c = PALETTE[ht.pointIndex % PALETTE.length];
                    engine.fill = c;
                    engine.stroke = c;
                }
                defaultRender();
            };
            chart.invalidate();
        } catch { /* ignore */ }
    };

    const onPieInitialized = (s: any) => {
        try {
            const pie = s?.hostElement ? s : s;
            if (!pie) return;
            pie.palette = PALETTE;
            if (tLower === 'donut') pie.innerRadius = 0.5;
            // Pie/donut: no slice labels — legend only (color + name).
            if (pie.dataLabel) {
                try { pie.dataLabel.position = Position.None; } catch { /* ignore */ }
                pie.dataLabel.connectingLine = false;
                pie.dataLabel.content = '';
            }
            if (pie.legend) {
                try { pie.legend.position = Position.Right; } catch { /* ignore */ }
            }
            const shrinkLegend = () => {
                const host = pie.hostElement as HTMLElement | undefined;
                if (!host) return;
                // No slice labels → remaining SVG text is the side legend.
                host.querySelectorAll('text').forEach(node => {
                    const el = node as SVGTextElement;
                    el.setAttribute('font-size', '9');
                    el.style.fontSize = PIE_LEGEND_FONT_SIZE;
                });
            };
            try {
                if (pie.rendered && typeof pie.rendered.addHandler === 'function')
                    pie.rendered.addHandler(shrinkLegend);
            } catch { /* ignore */ }
            pie.invalidate?.();
            setTimeout(shrinkLegend, 0);
        } catch { /* ignore */ }
    };

    return (
        <div className={`rounded-[4px] p-3 border ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
            {/* Pie/donut legend labels ~2 sizes smaller than Wijmo default */}
            <style>{`
                .agent-flexpie-legend .wj-flexchart .wj-legend text,
                .agent-flexpie-legend .wj-legend text,
                .agent-flexpie-legend svg .wj-legend text {
                    font-size: ${PIE_LEGEND_FONT_SIZE} !important;
                }
            `}</style>
            <div className="flex items-center justify-between gap-2 mb-2 flex-wrap">
                <div>
                    {meta?.title && <div className={`text-sm font-medium ${theme.label}`}>{String(meta.title)}</div>}
                    {meta?.measure && (
                        <div className={`text-xs opacity-70 ${theme.label}`}>Measure: {String(meta.measure)}</div>
                    )}
                </div>
                {allowedTypes.length > 1 && (
                    <div className="flex gap-1 flex-wrap">
                        {allowedTypes.map(typeKey => (
                            <button
                                key={typeKey}
                                type="button"
                                className={`${btn}${effectiveType === typeKey ? ' font-semibold' : ''}`}
                                title={typeKey === chartConfig.type ? 'Default type' : undefined}
                                onClick={() => setChartType(typeKey)}
                            >
                                {typeKey.charAt(0).toUpperCase() + typeKey.slice(1)}
                            </button>
                        ))}
                    </div>
                )}
            </div>
            {data.length === 0 ? (
                <div className={`text-xs text-center py-8 ${theme.label}`}>No data available.</div>
            ) : (
                <div
                    className={`w-full${isPie ? ' agent-flexpie-legend' : ''}`}
                    style={{ height: 280 }}
                >
                    {isPie ? (
                        <FlexPie
                            className="w-full h-full"
                            style={chartHostStyle}
                            itemsSource={chartData}
                            binding={yField}
                            bindingName={xField}
                            palette={PALETTE as any}
                            legend={{ position: 'Right' } as any}
                            initialized={onPieInitialized as any}
                        />
                    ) : (
                        <FlexChart
                            className="w-full h-full"
                            style={chartHostStyle}
                            itemsSource={chartData}
                            bindingX={xField}
                            chartType={mapTypeToWijmo(effectiveType)}
                            palette={PALETTE as any}
                            legend={{ position: seriesKeys.length > 1 ? 'Right' : 'None' } as any}
                            initialized={onChartInitialized as any}
                        >
                            {seriesKeys.map(key => (
                                <FlexChartSeries key={key} binding={key} name={key} />
                            ))}
                        </FlexChart>
                    )}
                </div>
            )}
        </div>
    );
};
