import React, { useMemo, useState } from 'react';
import { FlexChart, FlexChartSeries, FlexPie } from '@mescius/wijmo.react.chart';
import { ChartType } from '@mescius/wijmo.chart';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { DataRenderChartConfig, DataRenderMeta } from './types';

interface Props {
    meta?: DataRenderMeta;
    chartConfig: DataRenderChartConfig;
    data: Record<string, unknown>[];
}

function mapTypeToWijmo(type: string): ChartType {
    const t = (type || 'bar').toLowerCase();
    if (t === 'line') return ChartType.Line;
    if (t === 'area') return ChartType.Area;
    // bar → Column (vertical bars); Wijmo ChartType.Bar is horizontal
    return ChartType.Column;
}

/** Default Wijmo chart implementation for data_render (bar/line/area/pie). */
export const DataRenderChartWijmo: React.FC<Props> = ({ meta, chartConfig, data }) => {
    const { theme, t } = useTheme();
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

    const btn = `px-2 py-0.5 text-xs rounded-[4px] border ${theme.button_default}`;
    const isPie = (chartType || '').toLowerCase() === 'pie';
    const chartHostStyle: React.CSSProperties = {
        width: '100%',
        height: '100%',
        border: 'none',
        outline: 'none',
    };

    return (
        <div className={`rounded-[4px] p-3 border ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between gap-2 mb-2 flex-wrap">
                <div>
                    {meta?.title && <div className={`text-sm font-medium ${theme.label}`}>{String(meta.title)}</div>}
                    {meta?.measure && (
                        <div className={`text-xs opacity-70 ${theme.label}`}>Measure: {String(meta.measure)}</div>
                    )}
                </div>
                {allowedTypes.length > 1 && (
                    <div className="flex gap-1">
                        {allowedTypes.map(typeKey => (
                            <button
                                key={typeKey}
                                type="button"
                                className={`${btn}${chartType === typeKey ? ' font-semibold' : ''}`}
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
                <div className="w-full" style={{ height: 280 }}>
                    {isPie ? (
                        <FlexPie
                            className="w-full h-full"
                            style={chartHostStyle}
                            itemsSource={chartData}
                            binding={yField}
                            bindingName={xField}
                        />
                    ) : (
                        <FlexChart
                            className="w-full h-full"
                            style={chartHostStyle}
                            itemsSource={chartData}
                            bindingX={xField}
                            chartType={mapTypeToWijmo(chartType)}
                            legend={{ position: seriesKeys.length > 1 ? 'Right' : 'None' } as any}
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
