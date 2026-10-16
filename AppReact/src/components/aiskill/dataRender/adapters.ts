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

export type DataRenderCardFields = Array<{ label: string; value: string }>;

function fieldsFromObject(obj: Record<string, unknown>): DataRenderCardFields {
    return Object.entries(obj).map(([key, value]) => ({
        label: toHeader(key),
        value: value != null ? String(value) : '',
    }));
}

function fieldsFromLabeled(raw: unknown): DataRenderCardFields | null {
    if (!Array.isArray(raw)) return null;
    return raw.map((f: { label?: string; value?: unknown }) => ({
        label: String(f?.label ?? ''),
        value: f?.value != null ? String(f.value) : '',
    }));
}

/** 1–6 cards per row. Missing or invalid values use 1. */
export function cardsPerRowFromMeta(meta?: DataRenderMeta | null): number {
    const raw = meta?.cardsPerRow;
    const n = typeof raw === 'number' ? raw : parseInt(String(raw ?? ''), 10);
    if (!Number.isFinite(n) || n < 1) return 1;
    return Math.min(6, Math.floor(n));
}

export function adaptToCard(args: {
    dataJson?: string | null;
    metaJson?: string | null;
    title?: string | null;
}) {
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    const data = parseJsonSafe<unknown>(args.dataJson, null);
    const title = args.title ?? meta.title;
    const status = typeof meta.subtitle === 'string' ? meta.subtitle : undefined;
    const cardsPerRow = cardsPerRowFromMeta(meta);
    const cards: DataRenderCardFields[] = [];

    if (Array.isArray(data)) {
        for (const item of data) {
            if (!item || typeof item !== 'object' || Array.isArray(item)) continue;
            const obj = item as Record<string, unknown>;
            const labeled = fieldsFromLabeled(obj.fields);
            cards.push(labeled ?? fieldsFromObject(obj));
        }
    } else if (data && typeof data === 'object') {
        const obj = data as Record<string, unknown>;
        const labeled = fieldsFromLabeled(obj.fields);
        cards.push(labeled ?? fieldsFromObject(obj));
    }

    return { title, status, cardsPerRow, cards, meta };
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
