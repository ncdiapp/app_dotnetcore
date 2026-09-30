import React, { useMemo, useState } from 'react';
import {
    Area,
    AreaChart,
    Bar,
    BarChart,
    CartesianGrid,
    Legend,
    Line,
    LineChart,
    ResponsiveContainer,
    Tooltip,
    XAxis,
    YAxis,
} from 'recharts';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { DataRenderChartConfig, DataRenderMeta } from './types';

const CHART_COLORS = ['#3b82f6', '#4ade80', '#fbbf24', '#f87171', '#a78bfa', '#22d3ee', '#f472b6', '#a3e635'];

interface Props {
    meta?: DataRenderMeta;
    chartConfig: DataRenderChartConfig;
    data: Record<string, unknown>[];
}

export const DataRenderChart: React.FC<Props> = ({ meta, chartConfig, data }) => {
    const { theme } = useTheme();
    const allowedTypes = chartConfig.allowedTypes ?? ['bar', 'line', 'area'];
    const [chartType, setChartType] = useState(chartConfig.type ?? allowedTypes[0] ?? 'bar');

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

    const formatValue = (val: unknown) => {
        if (val == null) return '';
        const measure = String(meta?.measure ?? yField);
        if (['net_price', 'gross_profit', 'extended_cost', 'value'].includes(measure)) {
            return `$${Number(val).toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`;
        }
        return Number(val).toLocaleString();
    };

    const ChartComponent = chartType === 'line' ? LineChart : chartType === 'area' ? AreaChart : BarChart;
    const btn = `px-2 py-0.5 text-xs rounded-[4px] border ${theme.button_default}`;

    return (
        <div className={`border rounded-[4px] p-3 ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between gap-2 mb-2 flex-wrap">
                <div>
                    {meta?.title && <div className={`text-sm font-medium ${theme.label}`}>{String(meta.title)}</div>}
                    {meta?.measure && (
                        <div className={`text-xs opacity-70 ${theme.label}`}>Measure: {String(meta.measure)}</div>
                    )}
                </div>
                {allowedTypes.length > 1 && (
                    <div className="flex gap-1">
                        {allowedTypes.map(t => (
                            <button
                                key={t}
                                type="button"
                                className={`${btn}${chartType === t ? ' font-semibold' : ''}`}
                                onClick={() => setChartType(t)}
                            >
                                {t.charAt(0).toUpperCase() + t.slice(1)}
                            </button>
                        ))}
                    </div>
                )}
            </div>
            {data.length === 0 ? (
                <div className={`text-xs text-center py-8 ${theme.label}`}>No data available.</div>
            ) : (
                <div style={{ width: '100%', height: 280 }}>
                    <ResponsiveContainer width="100%" height="100%">
                        <ChartComponent data={chartData}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis dataKey={xField} tick={{ fontSize: 11 }} />
                            <YAxis tickFormatter={formatValue} tick={{ fontSize: 11 }} width={70} />
                            <Tooltip formatter={(val) => formatValue(val)} />
                            {seriesKeys.length > 1 && <Legend />}
                            {seriesKeys.map((key, i) => {
                                const color = CHART_COLORS[i % CHART_COLORS.length];
                                if (chartType === 'line') {
                                    return <Line key={key} type="monotone" dataKey={key} stroke={color} strokeWidth={2} dot={false} />;
                                }
                                if (chartType === 'area') {
                                    return <Area key={key} type="monotone" dataKey={key} stroke={color} fill={color} fillOpacity={0.15} />;
                                }
                                return <Bar key={key} dataKey={key} fill={color} radius={[4, 4, 0, 0]} />;
                            })}
                        </ChartComponent>
                    </ResponsiveContainer>
                </div>
            )}
        </div>
    );
};
