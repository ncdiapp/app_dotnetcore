import React, { useMemo, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentSvc, type DataRenderEvent } from '../../webapi/genericAgentSvc';
import { parseRows } from './dataRender/adapters';
import { parseJsonSafe } from './dataRender/types';

export type PolishedFilterField = {
    name?: string;
    Name?: string;
    control?: string;
    Control?: string;
    searchFields?: string[];
    SearchFields?: string[];
};

export type PolishedSpecClient = {
    layout?: string;
    title?: string;
    bindings?: {
        primaryImageField?: string;
        fields?: Array<{ name?: string; role?: string }>;
        columns?: string[];
        filterFields?: PolishedFilterField[];
    };
    layoutHints?: { cardsPerRow?: number; cardRows?: number; statusColorMap?: Record<string, string> };
};

/** Normalize polish bindings so PascalCase (server JObject.FromObject) still drives Filter UI. */
function normalizePolished(raw: Record<string, unknown> | null | undefined): PolishedSpecClient | null {
    if (!raw || typeof raw !== 'object') return null;
    const bindingsRaw = (raw.bindings || raw.Bindings) as Record<string, unknown> | undefined;
    const hintsRaw = (raw.layoutHints || raw.LayoutHints) as Record<string, unknown> | undefined;
    const fieldsArr = (bindingsRaw?.fields || bindingsRaw?.Fields) as Array<Record<string, unknown>> | undefined;
    const filterArr = (bindingsRaw?.filterFields || bindingsRaw?.FilterFields) as Array<Record<string, unknown>> | undefined;
    const columnsArr = (bindingsRaw?.columns || bindingsRaw?.Columns) as unknown;
    return {
        layout: String(raw.layout ?? raw.Layout ?? '') || undefined,
        title: String(raw.title ?? raw.Title ?? '') || undefined,
        bindings: bindingsRaw
            ? {
                primaryImageField: String(bindingsRaw.primaryImageField ?? bindingsRaw.PrimaryImageField ?? '') || undefined,
                fields: Array.isArray(fieldsArr)
                    ? fieldsArr.map(f => ({
                        name: String(f?.name ?? f?.Name ?? '') || undefined,
                        role: String(f?.role ?? f?.Role ?? '') || undefined,
                    }))
                    : undefined,
                columns: Array.isArray(columnsArr) ? columnsArr.map(String) : undefined,
                filterFields: Array.isArray(filterArr)
                    ? filterArr.map(f => ({
                        name: String(f?.name ?? f?.Name ?? '') || undefined,
                        Name: String(f?.Name ?? f?.name ?? '') || undefined,
                        control: String(f?.control ?? f?.Control ?? '') || undefined,
                        Control: String(f?.Control ?? f?.control ?? '') || undefined,
                        searchFields: (f?.searchFields || f?.SearchFields) as string[] | undefined,
                        SearchFields: (f?.SearchFields || f?.searchFields) as string[] | undefined,
                    }))
                    : undefined,
            }
            : undefined,
        layoutHints: hintsRaw
            ? {
                cardsPerRow: Number(hintsRaw.cardsPerRow ?? hintsRaw.CardsPerRow) || undefined,
                cardRows: Number(hintsRaw.cardRows ?? hintsRaw.CardRows) || undefined,
                statusColorMap: (hintsRaw.statusColorMap || hintsRaw.StatusColorMap) as Record<string, string> | undefined,
            }
            : undefined,
    };
}

function parseSpecBlob(event: DataRenderEvent | null | undefined): {
    polished: PolishedSpecClient | null;
    filterOptions: Record<string, string[]>;
    htmlPath: string | null;
    cardsPerRow: number;
    cardRows: number;
    pageGrid: string | null;
} {
    const raw = event?.SpecJson || event?.MetaJson || null;
    const obj = parseJsonSafe<Record<string, unknown>>(raw, {});
    const polished = normalizePolished(
        (obj.polishedSpec || obj.PolishedSpec) as Record<string, unknown> | undefined,
    );
    const filterOptionsRaw = (obj.filterOptions || obj.FilterOptions || {}) as Record<string, string[]>;
    const filterOptions = filterOptionsRaw && typeof filterOptionsRaw === 'object' ? filterOptionsRaw : {};
    const htmlPath = event?.HtmlPath || (obj.htmlPath as string) || null;
    const hintCpr = polished?.layoutHints?.cardsPerRow;
    const hintCr = polished?.layoutHints?.cardRows;
    const rawCpr = typeof obj.cardsPerRow === 'number' ? obj.cardsPerRow
        : typeof obj.CardsPerRow === 'number' ? obj.CardsPerRow : 0;
    const rawCr = typeof obj.cardRows === 'number' ? obj.cardRows
        : typeof obj.CardRows === 'number' ? obj.CardRows : 0;
    const pageGridRaw = (obj.pageGrid || obj.PageGrid) as string | undefined;
    let cardsPerRow = rawCpr > 0 ? rawCpr : (hintCpr && hintCpr > 0 ? hintCpr : 0);
    let cardRows = rawCr > 0 ? rawCr : (hintCr && hintCr > 0 ? hintCr : 0);
    let pageGrid = typeof pageGridRaw === 'string' && pageGridRaw.trim() ? pageGridRaw.trim() : null;
    if (pageGrid) {
        const m = pageGrid.match(/(\d+)\s*[xX×*]\s*(\d+)/);
        if (m) {
            if (cardsPerRow <= 0) cardsPerRow = Math.min(6, Math.max(1, parseInt(m[1], 10)));
            if (cardRows <= 0) cardRows = Math.min(20, Math.max(1, parseInt(m[2], 10)));
        }
    }
    // Prefer SpecJson / polish; otherwise omit forced grid (server uses tool/defaults).
    if (!pageGrid && cardsPerRow > 0 && cardRows > 0) pageGrid = `${cardsPerRow}x${cardRows}`;
    return {
        polished,
        filterOptions,
        htmlPath,
        cardsPerRow,
        cardRows,
        pageGrid,
    };
}

function applyFilters(
    rows: Record<string, unknown>[],
    search: string,
    searchFields: string[],
    multi: Record<string, string[]>
): Record<string, unknown>[] {
    let out = rows;
    const q = search.trim().toLowerCase();
    if (q && searchFields.length > 0) {
        out = out.filter(r =>
            searchFields.some(f => String(r[f] ?? '').toLowerCase().includes(q))
        );
    }
    for (const [field, selected] of Object.entries(multi)) {
        if (!selected || selected.length === 0) continue;
        const set = new Set(selected.map(s => s.toLowerCase()));
        out = out.filter(r => set.has(String(r[field] ?? '').toLowerCase()));
    }
    return out;
}

/** Export/Print must not re-POST Result SpecJson.cardsHtml (can be MBs for large catalogs). */
function slimExportSpecJson(raw: string | null | undefined, layout: string): string | undefined {
    if (!raw?.trim()) return undefined;
    try {
        const obj = JSON.parse(raw) as Record<string, unknown>;
        const polished = obj.polishedSpec ?? obj.PolishedSpec;
        const slim: Record<string, unknown> = {};
        if (polished) slim.polishedSpec = polished;
        const fo = obj.filterOptions ?? obj.FilterOptions;
        if (fo) slim.filterOptions = fo;
        const title = obj.title ?? obj.Title;
        if (title != null) slim.title = title;
        const cpr = obj.cardsPerRow ?? obj.CardsPerRow;
        const cr = obj.cardRows ?? obj.CardRows;
        const pg = obj.pageGrid ?? obj.PageGrid;
        if (typeof cpr === 'number' && cpr > 0) slim.cardsPerRow = cpr;
        if (typeof cr === 'number' && cr > 0) slim.cardRows = cr;
        if (typeof pg === 'string' && pg.trim()) slim.pageGrid = pg.trim();
        // Dashboard / KPI / chart need structural spec (not painted HTML).
        if (layout === 'kpi' || layout === 'dashboard' || layout === 'chart') {
            if (obj.kpis != null || obj.Kpis != null) slim.kpis = obj.kpis ?? obj.Kpis;
            if (obj.chart != null || obj.Chart != null) slim.chart = obj.chart ?? obj.Chart;
            if (obj.charts != null || obj.Charts != null) slim.charts = obj.charts ?? obj.Charts;
            if (obj.blocks != null || obj.Blocks != null) slim.blocks = obj.blocks ?? obj.Blocks;
            if (obj.availableChartTypes != null) slim.availableChartTypes = obj.availableChartTypes;
        }
        if (layout === 'page' && (obj.views || obj.Views))
            slim.views = obj.views ?? obj.Views;
        return JSON.stringify(slim);
    } catch {
        return undefined;
    }
}

function printHtmlInHiddenIframe(html: string): void {
    const prev = document.getElementById('agent-result-print-frame');
    if (prev) prev.remove();
    const iframe = document.createElement('iframe');
    iframe.id = 'agent-result-print-frame';
    iframe.setAttribute('aria-hidden', 'true');
    iframe.style.cssText = 'position:fixed;right:0;bottom:0;width:0;height:0;border:0;opacity:0;pointer-events:none;';
    document.body.appendChild(iframe);
    const doc = iframe.contentDocument || iframe.contentWindow?.document;
    if (!doc) {
        iframe.remove();
        throw new Error('Print frame unavailable.');
    }
    doc.open();
    doc.write(html);
    doc.close();
    const runPrint = () => {
        try {
            iframe.contentWindow?.focus();
            iframe.contentWindow?.print();
        } finally {
            setTimeout(() => { try { iframe.remove(); } catch { /* ignore */ } }, 1000);
        }
    };
    // Wait for embedded/remote images before the system print dialog (data-URIs settle quickly).
    const imgs = Array.from(doc.images || []);
    const ready = imgs.length === 0
        ? Promise.resolve()
        : Promise.all(imgs.map(img => {
            if (img.complete) return Promise.resolve();
            return new Promise<void>(resolve => {
                img.onload = () => resolve();
                img.onerror = () => resolve();
            });
        }));
    void ready.then(() => setTimeout(runPrint, 80));
}

interface Props {
    event: DataRenderEvent;
    skillKey: string;
    sessionKey: string | null;
    disabled?: boolean;
    children: (filteredEvent: DataRenderEvent, polished: PolishedSpecClient | null) => React.ReactNode;
}

/**
 * React Result chrome: Filter / Print / Export (design redesign).
 * Cards Result host = TemplateContent/cardsHtml only (not React DataRenderCard).
 * Print regenerates fitted R×C HTML via ExportResult (hidden iframe — no blank tab).
 */
export const ResultChrome: React.FC<Props> = ({
    event,
    skillKey,
    sessionKey,
    disabled,
    children,
}) => {
    const { theme } = useTheme();
    const { polished, filterOptions, cardsPerRow, cardRows, pageGrid } = useMemo(
        () => parseSpecBlob(event),
        [event]
    );
    const allRows = useMemo(() => parseRows(event.DataJson), [event.DataJson]);

    const polishFilterFields = polished?.bindings?.filterFields ?? [];
    // Fallback: always offer Search when rows exist but polish omitted filterFields.
    const filterFields = useMemo((): PolishedFilterField[] => {
        if (polishFilterFields.length > 0) return polishFilterFields;
        if (allRows.length === 0) return [];
        const keys = Object.keys(allRows[0] || {}).filter(k => k && !/^image|sketch|photo|thumb/i.test(k)).slice(0, 8);
        if (keys.length === 0) return [];
        return [{ name: '_search', control: 'search', searchFields: keys }];
    }, [polishFilterFields, allRows]);
    const searchField = filterFields.find(
        f => (f.control || f.Control || '').toLowerCase() === 'search'
            || (f.name || f.Name) === '_search'
    );
    const searchFields = searchField?.searchFields || searchField?.SearchFields
        || polished?.bindings?.fields?.map(f => f.name).filter(Boolean) as string[]
        || [];
    const multiFields = filterFields.filter(
        f => {
            const c = (f.control || f.Control || '').toLowerCase();
            const n = (f.name || f.Name || '').trim();
            return (c === 'multiselect' || c === 'multi-select') && !!n && n !== '_search';
        }
    );

    const [search, setSearch] = useState('');
    const [multi, setMulti] = useState<Record<string, string[]>>({});
    const [busy, setBusy] = useState<string | null>(null);
    const [exportOpen, setExportOpen] = useState(false);
    const [msg, setMsg] = useState<string | null>(null);

    const filteredRows = useMemo(
        () => applyFilters(allRows, search, searchFields.filter(Boolean) as string[], multi),
        [allRows, search, searchFields, multi]
    );

    const layout = (event.Layout || event.Ui || 'table').toLowerCase();
    const layoutNorm = layout === 'card' ? 'cards'
        : layout === 'kpi_dashboard' ? 'kpi'
        : layout;

    const filteredEvent = useMemo((): DataRenderEvent => {
        // Apply filter to DataJson (grid / page table / kpi). cardsHtml is static TemplateContent.
        if (!event.DataJson || filteredRows.length === allRows.length) {
            return event;
        }
        return {
            ...event,
            DataJson: JSON.stringify(filteredRows),
            RowCount: filteredRows.length,
        };
    }, [event, filteredRows, allRows.length]);

    const title = polished?.title || event.Title || 'Result';
    const total = allRows.length > 0 ? allRows.length : (event.RowCount || 0);
    const shown = allRows.length > 0 ? filteredRows.length : total;

    // Match FormMasterDetail / SkillSet header bar: solid colorful action buttons (equal ~100px width).
    const btnPrimary =
        'w-[100px] justify-center px-2 h-7 rounded-[4px] text-xs text-white bg-blue-400 hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center gap-1';
    const btnSecondary =
        'w-[100px] justify-center px-2 h-7 rounded-[4px] text-xs text-white bg-green-500 hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center gap-1';
    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`;

    const toggleMulti = (field: string, value: string) => {
        setMulti(prev => {
            const cur = prev[field] ?? [];
            const next = cur.includes(value) ? cur.filter(v => v !== value) : [...cur, value];
            return { ...prev, [field]: next };
        });
    };

    // Preserve page / kpi / dashboard / table / cards for ExportResult (never coerce kpi→cards).
    const exportLayout = layoutNorm === 'card' ? 'cards'
        : layoutNorm === 'kpi_dashboard' ? 'kpi'
        : layoutNorm;

    // Slim SpecJson: polished bindings only — never re-POST cardsHtml (large catalogs).
    const exportSpecJson = useMemo(
        () => slimExportSpecJson(event.SpecJson || event.MetaJson, exportLayout),
        [event.SpecJson, event.MetaJson, exportLayout],
    );
    // Unfiltered → rely on server session dataset (omit MBs of RecordsJson). Filtered → send subset.
    const filtersActive = filteredRows.length !== allRows.length;
    const exportRecordsJson = useMemo(() => {
        if (!filtersActive) return undefined;
        if (filteredRows.length > 0) return JSON.stringify(filteredRows);
        return undefined;
    }, [filtersActive, filteredRows]);
    // Fallback when session cache is gone (chat reopen): full DataJson once.
    const exportRecordsFallback = useMemo(() => {
        if (event.DataJson?.trim()) return event.DataJson.trim();
        if (filteredRows.length > 0) return JSON.stringify(filteredRows);
        return undefined;
    }, [event.DataJson, filteredRows]);

    const buildExportArgs = (format: string, withRecords: boolean) => ({
        skillKey,
        sessionKey: sessionKey!,
        format,
        layout: exportLayout,
        title,
        ...(cardsPerRow > 0 ? { cardsPerRow } : {}),
        ...(cardRows > 0 ? { cardRows } : {}),
        ...(pageGrid ? { pageGrid } : {}),
        ...(exportSpecJson ? { specJson: exportSpecJson } : {}),
        ...(withRecords && (exportRecordsJson || exportRecordsFallback)
            ? { recordsJson: exportRecordsJson || exportRecordsFallback }
            : {}),
    });

    const runExport = async (format: string) => {
        if (!sessionKey) {
            setMsg('Session not ready — reopen this chat or run the search again.');
            return;
        }
        setBusy(format);
        setMsg(null);
        setExportOpen(false);
        try {
            let res = await genericAgentSvc.ExportResult(buildExportArgs(format, filtersActive));
            // Session analyze cache empty after reopen → retry with rows once (SpecJson stays slim).
            if (!res?.path && !filtersActive && exportRecordsFallback) {
                res = await genericAgentSvc.ExportResult(buildExportArgs(format, true));
            }
            if (res?.path) {
                setMsg(`Exported ${format.toUpperCase()}: ${res.path}`);
                await genericAgentSvc.DownloadAgentFile(skillKey, sessionKey, res.path);
            } else {
                setMsg(res?.error || 'Export failed.');
            }
        } catch (e: unknown) {
            setMsg(e instanceof Error ? e.message : 'Export failed.');
        } finally {
            setBusy(null);
        }
    };

    const onPrint = async () => {
        if (!sessionKey) {
            setMsg('Session not ready — reopen this chat or run the search again.');
            return;
        }
        setBusy('print');
        setMsg(null);
        try {
            let res = await genericAgentSvc.ExportResult(buildExportArgs('html', filtersActive));
            if (!res?.path && !filtersActive && exportRecordsFallback) {
                res = await genericAgentSvc.ExportResult(buildExportArgs('html', true));
            }
            if (!res?.path) {
                setMsg(res?.error || 'Print failed.');
                return;
            }
            const html = await genericAgentSvc.ReadAgentFileText(skillKey, sessionKey, res.path);
            if (!html?.trim()) {
                setMsg('Print HTML empty.');
                return;
            }
            printHtmlInHiddenIframe(html);
        } catch (e: unknown) {
            const raw = e instanceof Error ? e.message : 'Print failed.';
            setMsg(/failed to fetch/i.test(raw)
                ? 'Print failed (network). Try again; if it persists, filter fewer rows.'
                : raw);
        } finally {
            setBusy(null);
        }
    };

    const showChrome = !!(polished || allRows.length > 0 || event.HtmlPath || event.SpecJson);

    if (!showChrome) {
        return <>{children(event, polished)}</>;
    }

    return (
        <div className="w-full h-full min-h-0 min-w-0 flex flex-col gap-2">
            <div className="flex flex-col gap-2 shrink-0 px-0.5">
                <div className="flex items-center justify-between gap-2 flex-wrap">
                    <div className={`text-sm font-semibold ${theme.title} truncate`}>
                        {title}
                        <span className={`ml-2 text-xs font-normal ${theme.label}`}>
                            {shown} of {total || shown}
                        </span>
                        {event.Truncated && (
                            <span className={`ml-2 text-xs font-normal ${theme.label}`}>(truncated)</span>
                        )}
                    </div>
                    <div className="flex flex-wrap gap-1 relative">
                        <button
                            type="button"
                            className={btnPrimary}
                            disabled={!!busy}
                            onClick={() => void onPrint()}
                        >
                            <i className="fa-solid fa-print mr-1" />
                            {busy === 'print' || busy === 'pdf' ? 'Working…' : 'Print'}
                        </button>
                        <button
                            type="button"
                            className={btnSecondary}
                            disabled={!!busy}
                            onClick={() => setExportOpen(v => !v)}
                        >
                            <i className="fa-solid fa-file-export mr-1" />
                            Export
                        </button>
                        {exportOpen && (
                            <div className={`absolute right-0 top-full mt-1 z-20 min-w-[140px] rounded-[4px] border shadow-md py-1 bg-white ${theme.mainContentSection}`}>
                                {(['pdf', 'docx', 'pptx', 'html', 'xlsx'] as const).map(fmt => (
                                    <button
                                        key={fmt}
                                        type="button"
                                        className="block w-full text-left px-3 py-1.5 text-xs text-gray-700 hover:bg-blue-50"
                                        disabled={!!busy}
                                        onClick={() => void runExport(fmt)}
                                    >
                                        {fmt.toUpperCase()}
                                    </button>
                                ))}
                            </div>
                        )}
                    </div>
                </div>

                {/* layout=page: each Grid/Card section has its own filter in InteractivePagePanel. */}
                {layoutNorm !== 'page' && (searchField || multiFields.length > 0) && (
                    <div className="flex flex-wrap items-center gap-2">
                        {searchField && (
                            <div className="flex items-center gap-1 min-w-[180px] w-1 flex-auto">
                                <i className={`fa-solid fa-magnifying-glass text-xs ${theme.label}`} />
                                <input
                                    className={inp}
                                    placeholder="Search…"
                                    value={search}
                                    disabled={!!disabled}
                                    onChange={e => setSearch(e.target.value)}
                                />
                            </div>
                        )}
                        {multiFields.map(ff => {
                            const name = (ff.name || ff.Name || '').trim();
                            if (!name) return null;
                            const opts = filterOptions[name] || [];
                            const selected = multi[name] || [];
                            return (
                                <div key={name} className="flex flex-wrap items-center gap-1">
                                    <button
                                        type="button"
                                        className={`px-2 py-0.5 text-xs rounded-full border ${selected.length === 0 ? theme.button_default : 'opacity-70'} ${theme.label}`}
                                        onClick={() => setMulti(prev => ({ ...prev, [name]: [] }))}
                                    >
                                        All
                                    </button>
                                    {opts.map(v => {
                                        const on = selected.includes(v);
                                        return (
                                            <button
                                                key={v}
                                                type="button"
                                                className={`px-2 py-0.5 text-xs rounded-full border ${on ? theme.button_default : 'opacity-60'} ${theme.label}`}
                                                disabled={!!disabled}
                                                onClick={() => toggleMulti(name, v)}
                                            >
                                                {v}
                                            </button>
                                        );
                                    })}
                                </div>
                            );
                        })}
                    </div>
                )}
                {msg && (
                    <div className={`text-xs px-1 py-0.5 rounded-[4px] ${theme.label} bg-amber-50 border border-amber-200`}>
                        {msg}
                    </div>
                )}
            </div>
            <div className="w-full h-1 flex-auto min-h-0 min-w-0 overflow-hidden">
                {children(filteredEvent, polished)}
            </div>
        </div>
    );
};
