import type { DataRenderChartConfig, DataRenderColumn, DataRenderMeta } from './types';
import { parseJsonSafe } from './types';

function toHeader(field: string): string {
    return field
        .split('_')
        .map(w => (w ? w.charAt(0).toUpperCase() + w.slice(1) : w))
        .join(' ');
}

export function parseRows(dataJson: string | null | undefined): Record<string, unknown>[] {
    const data = parseJsonSafe<unknown>(dataJson, []);
    if (Array.isArray(data)) return data as Record<string, unknown>[];
    if (data && typeof data === 'object') {
        const obj = data as Record<string, unknown>;
        if (Array.isArray(obj.items)) return obj.items as Record<string, unknown>[];
        if (Array.isArray(obj.rows)) return obj.rows as Record<string, unknown>[];
        if (Array.isArray(obj.data)) return obj.data as Record<string, unknown>[];
    }
    return [];
}

export function adaptToGrid(args: {
    dataJson?: string | null;
    columnsJson?: string | null;
    metaJson?: string | null;
    title?: string | null;
}) {
    const rows = parseRows(args.dataJson);
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    const columns = parseJsonSafe<DataRenderColumn[]>(args.columnsJson, []);

    let colDefs: Array<{
        field: string;
        headerName: string;
        width: number;
        dataType?: string;
        hide?: boolean;
    }>;

    if (Array.isArray(columns) && columns.length > 0) {
        colDefs = columns.map(col => ({
            field: col.field,
            headerName: col.header ?? col.headerName ?? toHeader(col.field),
            width: col.width ?? 150,
            dataType: col.dataType,
            hide: col.hide ?? false,
        }));
    } else if (rows.length > 0) {
        colDefs = Object.keys(rows[0]).map(field => ({
            field,
            headerName: toHeader(field),
            width: 150,
        }));
    } else {
        colDefs = [];
    }

    return {
        colDefs,
        rowData: rows,
        title: args.title ?? meta.title,
        meta,
    };
}

export function adaptToCard(args: {
    dataJson?: string | null;
    metaJson?: string | null;
    title?: string | null;
}) {
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    const data = parseJsonSafe<unknown>(args.dataJson, null);

    if (data && typeof data === 'object' && !Array.isArray(data)) {
        const obj = data as Record<string, unknown>;
        if (Array.isArray(obj.fields)) {
            return {
                title: args.title ?? meta.title,
                status: typeof meta.subtitle === 'string' ? meta.subtitle : undefined,
                fields: (obj.fields as Array<{ label?: string; value?: unknown }>).map(f => ({
                    label: String(f.label ?? ''),
                    value: f.value != null ? String(f.value) : '',
                })),
                meta,
            };
        }
        const fields = Object.entries(obj).map(([key, value]) => ({
            label: toHeader(key),
            value: value != null ? String(value) : '',
        }));
        return {
            title: args.title ?? meta.title,
            status: typeof meta.subtitle === 'string' ? meta.subtitle : undefined,
            fields,
            meta,
        };
    }

    if (Array.isArray(data) && data.length > 0 && typeof data[0] === 'object') {
        const obj = data[0] as Record<string, unknown>;
        const fields = Object.entries(obj).map(([key, value]) => ({
            label: toHeader(key),
            value: value != null ? String(value) : '',
        }));
        return {
            title: args.title ?? meta.title,
            status: typeof meta.subtitle === 'string' ? meta.subtitle : undefined,
            fields,
            meta,
        };
    }

    return {
        title: args.title ?? meta.title,
        status: typeof meta.subtitle === 'string' ? meta.subtitle : undefined,
        fields: [] as Array<{ label: string; value: string }>,
        meta,
    };
}

export function adaptToChart(args: {
    dataJson?: string | null;
    chartConfigJson?: string | null;
    metaJson?: string | null;
    title?: string | null;
}) {
    const rows = parseRows(args.dataJson);
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    if (args.title && !meta.title) meta.title = args.title;
    const config = parseJsonSafe<DataRenderChartConfig>(args.chartConfigJson, {});

    const sample = rows[0] ?? {};
    const keys = Object.keys(sample);
    const xField = config.xField ?? keys.find(k => typeof sample[k] === 'string') ?? keys[0] ?? 'period';
    const yField = config.yField ?? keys.find(k => typeof sample[k] === 'number') ?? keys[1] ?? 'value';
    const groupBy =
        config.groupBy
        ?? (keys.length > 2 ? keys.find(k => k !== xField && k !== yField) : undefined);

    return {
        meta,
        chartConfig: {
            type: config.type ?? 'bar',
            xField,
            yField,
            groupBy,
            allowedTypes: config.allowedTypes ?? ['bar', 'line', 'area'],
        },
        data: rows,
    };
}
