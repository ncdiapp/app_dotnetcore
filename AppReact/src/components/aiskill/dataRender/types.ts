export interface DataRenderAction {
    Id: string;
    Label: string;
}

export interface DataRenderEvent {
    RenderId: string;
    Ui: string;
    Title?: string | null;
    DataJson?: string | null;
    ColumnsJson?: string | null;
    ChartConfigJson?: string | null;
    ActionsJson?: string | null;
    MetaJson?: string | null;
    /** kpi_dashboard: JSON array of blocks */
    BlocksJson?: string | null;
    RowCount?: number;
    Truncated?: boolean;
    Timestamp?: string;
}

export interface DataRenderColumn {
    field: string;
    header?: string;
    headerName?: string;
    dataType?: string;
    width?: number;
    hide?: boolean;
}

export interface DataRenderChartConfig {
    type?: string;
    xField?: string;
    yField?: string;
    groupBy?: string;
    allowedTypes?: string[];
}

export interface DataRenderMeta {
    title?: string;
    subtitle?: string;
    measure?: string;
    total?: number;
    [key: string]: unknown;
}

export interface DataRenderKpiItem {
    label: string;
    value: string;
    hint?: string;
}

export type DataRenderBlockType = 'markdown' | 'kpi' | 'chart' | 'grid' | 'card';

export interface DataRenderBlock {
    id?: string;
    type: DataRenderBlockType;
    title?: string;
    content?: string;
    items?: DataRenderKpiItem[];
    data?: unknown;
    columns?: DataRenderColumn[];
    chartConfig?: DataRenderChartConfig;
    actions?: Array<{ id?: string; Id?: string; label?: string; Label?: string }>;
    meta?: DataRenderMeta;
}

export function parseJsonSafe<T>(raw: string | null | undefined, fallback: T): T {
    if (!raw || !String(raw).trim()) return fallback;
    try {
        return JSON.parse(raw) as T;
    } catch {
        return fallback;
    }
}

export function parseActions(actionsJson: string | null | undefined): DataRenderAction[] {
    const arr = parseJsonSafe<Array<{ id?: string; Id?: string; label?: string; Label?: string }>>(actionsJson, []);
    if (!Array.isArray(arr)) return [];
    return arr
        .map(a => ({
            Id: String(a.Id ?? a.id ?? ''),
            Label: String(a.Label ?? a.label ?? a.Id ?? a.id ?? ''),
        }))
        .filter(a => a.Id);
}

export function parseActionsFromUnknown(raw: unknown): DataRenderAction[] {
    if (!raw) return [];
    if (typeof raw === 'string') return parseActions(raw);
    if (!Array.isArray(raw)) return [];
    return raw
        .map((a: any) => ({
            Id: String(a?.Id ?? a?.id ?? ''),
            Label: String(a?.Label ?? a?.label ?? a?.Id ?? a?.id ?? ''),
        }))
        .filter(a => a.Id);
}

export function parseBlocks(blocksJson: string | null | undefined): DataRenderBlock[] {
    const arr = parseJsonSafe<any[]>(blocksJson, []);
    if (!Array.isArray(arr)) return [];
    return arr
        .map((b, i) => {
            if (!b || typeof b !== 'object') return null;
            const type = String(b.type ?? b.Type ?? '').toLowerCase();
            if (!['markdown', 'kpi', 'chart', 'grid', 'card'].includes(type)) return null;
            const itemsRaw = b.items ?? b.Items;
            const items = Array.isArray(itemsRaw)
                ? itemsRaw.map((it: any) => ({
                    label: String(it?.label ?? it?.Label ?? ''),
                    value: String(it?.value ?? it?.Value ?? ''),
                    hint: it?.hint != null || it?.Hint != null ? String(it.hint ?? it.Hint) : undefined,
                }))
                : undefined;
            return {
                id: String(b.id ?? b.Id ?? `b${i + 1}`),
                type: type as DataRenderBlockType,
                title: b.title ?? b.Title,
                content: b.content ?? b.Content,
                items,
                data: b.data ?? b.Data,
                columns: b.columns ?? b.Columns,
                chartConfig: b.chartConfig ?? b.ChartConfig,
                actions: b.actions ?? b.Actions,
                meta: b.meta ?? b.Meta,
            } as DataRenderBlock;
        })
        .filter(Boolean) as DataRenderBlock[];
}
