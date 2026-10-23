import type { DataRenderChartConfig, DataRenderColumn, DataRenderMeta } from './types';
import { parseJsonSafe } from './types';
import { normalizeCellFormat } from './cellFormat';
import { imageUrlKind } from './imageValue';

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

/** Prefer ColumnsJson; else SpecJson.columnsJson / polishedSpec.bindings.columns (LLM short headers). */
function resolveColumnDefs(
    columnsJson?: string | null,
    specJson?: string | null,
): DataRenderColumn[] {
    const fromColumns = parseJsonSafe<DataRenderColumn[]>(columnsJson, []);
    if (Array.isArray(fromColumns) && fromColumns.length > 0 && fromColumns.some(c => c?.field))
        return fromColumns;

    const spec = parseJsonSafe<Record<string, unknown>>(specJson, {});
    const nested = (spec.columnsJson ?? spec.ColumnsJson) as unknown;
    if (Array.isArray(nested) && nested.length > 0)
        return nested as DataRenderColumn[];

    const polished = (spec.polishedSpec ?? spec.PolishedSpec) as Record<string, unknown> | undefined;
    const bindings = (polished?.bindings ?? polished?.Bindings) as Record<string, unknown> | undefined;
    const cols = (bindings?.columns ?? bindings?.Columns) as unknown;
    if (!Array.isArray(cols)) return [];

    const out: DataRenderColumn[] = [];
    for (const c of cols) {
        if (typeof c === 'string' && c.trim()) {
            out.push({ field: c.trim() });
            continue;
        }
        if (c && typeof c === 'object') {
            const o = c as Record<string, unknown>;
            const field = String(o.field ?? o.Field ?? o.name ?? o.Name ?? '').trim();
            if (!field) continue;
            const header = String(o.header ?? o.Header ?? o.headerName ?? o.HeaderName ?? '').trim() || undefined;
            const dataType = String(o.format ?? o.Format ?? o.dataType ?? o.DataType ?? '').trim() || undefined;
            out.push({ field, header, headerName: header, dataType });
        }
    }
    return out;
}

export function adaptToGrid(args: {
    dataJson?: string | null;
    columnsJson?: string | null;
    metaJson?: string | null;
    specJson?: string | null;
    title?: string | null;
}) {
    const rows = parseRows(args.dataJson);
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    const columns = resolveColumnDefs(args.columnsJson, args.specJson);

    let colDefs: Array<{
        field: string;
        headerName: string;
        width: number;
        dataType?: string;
        hide?: boolean;
    }>;

    if (Array.isArray(columns) && columns.length > 0) {
        // dataType/format must come from AI polish (field + sample values) — no name heuristics here.
        colDefs = columns.map(col => ({
            field: col.field,
            headerName: col.header ?? col.headerName ?? col.field,
            width: col.width ?? 150,
            dataType: normalizeCellFormat(col.dataType) ?? undefined,
            hide: col.hide ?? false,
        }));
    } else if (rows.length > 0) {
        // No polish columns: raw field names, no typed cell formats.
        colDefs = Object.keys(rows[0]).map(field => ({
            field,
            headerName: toHeader(field),
            width: 150,
            dataType: undefined,
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

export type DataRenderCardField = { label: string; value: string; role?: string };
export type DataRenderCardFields = DataRenderCardField[];

function fieldsFromObject(obj: Record<string, unknown>): DataRenderCardFields {
    // Prefer image column + up to 5 text fields (name + non-empty), not a full dump.
    const entries = Object.entries(obj);
    // Card fallback: detect by URL value shape only (not English field-name keywords).
    const img = entries.find(([, v]) => imageUrlKind(v) != null);
    const texts = entries
        .filter(([k, v]) => k !== img?.[0] && v != null && String(v).trim() !== '')
        .slice(0, 5);
    const out: DataRenderCardFields = [];
    if (img) {
        out.push({ label: img[0], value: String(img[1] ?? ''), role: 'primaryImage' });
    }
    for (const [key, value] of texts) {
        out.push({
            label: key,
            value: value != null ? String(value) : '',
            role: 'body',
        });
    }
    return promoteCardFieldRoles(out);
}

function fieldsFromLabeled(raw: unknown): DataRenderCardFields | null {
    if (!Array.isArray(raw)) return null;
    return raw.map((f: { label?: string; value?: unknown; role?: string }) => ({
        label: String(f?.label ?? ''),
        value: f?.value != null ? String(f.value) : '',
        role: f?.role || 'body',
    }));
}

/** 1–6 cards per row. Missing or invalid values use 1. */
export function cardsPerRowFromMeta(meta?: DataRenderMeta | null): number {
    const raw = meta?.cardsPerRow;
    const n = typeof raw === 'number' ? raw : parseInt(String(raw ?? ''), 10);
    if (!Number.isFinite(n) || n < 1) return 1;
    return Math.min(6, Math.floor(n));
}

export type PolishedCardBindings = {
    primaryImageField?: string;
    fields?: Array<{ name?: string; role?: string }>;
};

/** Map body/empty roles → gallery roles (parity with CardSheet.PromoteBodyRolesToGallery). */
export function promoteCardFieldRoles(fields: DataRenderCardFields): DataRenderCardFields {
    const text = fields.filter(f => (f.role || '').toLowerCase() !== 'primaryimage');
    const has = (role: string) => text.some(f => (f.role || '').toLowerCase() === role.toLowerCase());
    const bodies = text.filter(f => {
        const r = (f.role || 'body').toLowerCase();
        return !r || r === 'body';
    });
    if (bodies.length === 0) return fields;
    let slot = 0;
    const take = (role: string, allowMany = false) => {
        if (!allowMany && has(role)) return;
        if (slot >= bodies.length) return;
        bodies[slot].role = role;
        slot++;
    };
    take('idLink');
    take('title');
    take('colorSwatch');
    take('statusPill');
    while (slot < bodies.length) take('meta', true);
    return fields;
}

function fieldsFromPolish(obj: Record<string, unknown>, bindings: PolishedCardBindings): DataRenderCardFields {
    const out: DataRenderCardFields = [];
    const img = bindings.primaryImageField;
    if (img && obj[img] != null && String(obj[img]).length > 0) {
        out.push({ label: img, value: String(obj[img]), role: 'primaryImage' });
    }
    for (const f of bindings.fields || []) {
        const name = f?.name;
        if (!name || (img && name.toLowerCase() === img.toLowerCase())) continue;
        if (!(name in obj)) continue;
        out.push({
            label: name,
            value: obj[name] != null ? String(obj[name]) : '',
            role: f.role || 'body',
        });
    }
    const selected = out.length > 0 ? out : fieldsFromObject(obj);
    return promoteCardFieldRoles(selected);
}

export function adaptToCard(args: {
    dataJson?: string | null;
    metaJson?: string | null;
    title?: string | null;
    polishedBindings?: PolishedCardBindings | null;
}) {
    const meta = parseJsonSafe<DataRenderMeta>(args.metaJson, {});
    const data = parseJsonSafe<unknown>(args.dataJson, null);
    const title = args.title ?? meta.title;
    const status = typeof meta.subtitle === 'string' ? meta.subtitle : undefined;
    const cardsPerRow = cardsPerRowFromMeta(meta);
    const cards: DataRenderCardFields[] = [];
    const polish = args.polishedBindings;

    if (Array.isArray(data)) {
        for (const item of data) {
            if (!item || typeof item !== 'object' || Array.isArray(item)) continue;
            const obj = item as Record<string, unknown>;
            const labeled = fieldsFromLabeled(obj.fields);
            if (labeled) cards.push(labeled);
            else if (polish) cards.push(fieldsFromPolish(obj, polish));
            else cards.push(fieldsFromObject(obj));
        }
    } else if (data && typeof data === 'object') {
        const obj = data as Record<string, unknown>;
        const labeled = fieldsFromLabeled(obj.fields);
        if (labeled) cards.push(labeled);
        else if (polish) cards.push(fieldsFromPolish(obj, polish));
        else cards.push(fieldsFromObject(obj));
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
