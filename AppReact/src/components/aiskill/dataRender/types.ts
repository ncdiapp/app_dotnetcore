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
