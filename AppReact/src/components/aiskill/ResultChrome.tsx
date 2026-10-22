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
    const polished = (obj.polishedSpec || obj.PolishedSpec) as PolishedSpecClient | null;
    const filterOptions = (obj.filterOptions || obj.FilterOptions || {}) as Record<string, string[]>;
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
        polished: polished || null,
        filterOptions: filterOptions || {},
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
    // Allow images/CSS to settle briefly before the system print dialog.
    setTimeout(runPrint, 350);
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

    const filterFields = polished?.bindings?.filterFields ?? [];
    const searchField = filterFields.find(
        f => (f.control || f.Control || '').toLowerCase() === 'search'
            || (f.name || f.Name) === '_search'
    );
    const searchFields = searchField?.searchFields || searchField?.SearchFields
        || polished?.bindings?.fields?.map(f => f.name).filter(Boolean) as string[]
        || [];
    const multiFields = filterFields.filter(
        f => (f.control || f.Control || '').toLowerCase() === 'multiselect'
            && (f.name || f.Name) && (f.name || f.Name) !== '_search'
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
    const layoutNorm = layout === 'card' ? 'cards' : layout;
    const isCardsLayout = layoutNorm === 'cards' || layoutNorm === 'page';

    const filteredEvent = useMemo((): DataRenderEvent => {
        // Cards/page: keep TemplateContent cardsHtml (never strip for React DataRenderCard fallback).
        if (isCardsLayout || !event.DataJson || filteredRows.length === allRows.length) {
            return event;
        }
        return {
            ...event,
            DataJson: JSON.stringify(filteredRows),
            RowCount: filteredRows.length,
        };
    }, [event, filteredRows, allRows.length, isCardsLayout]);

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

    const exportLayout = layoutNorm === 'cards' || layoutNorm === 'table' || layoutNorm === 'page'
        ? (layoutNorm === 'page' ? 'cards' : layoutNorm)
        : layoutNorm;

    const runExport = async (format: string) => {
        if (!sessionKey || disabled) return;
        setBusy(format);
        setMsg(null);
        setExportOpen(false);
        try {
            const res = await genericAgentSvc.ExportResult({
                skillKey,
                sessionKey,
                format,
                layout: exportLayout,
                title,
                datasetName: undefined,
                ...(cardsPerRow > 0 ? { cardsPerRow } : {}),
                ...(cardRows > 0 ? { cardRows } : {}),
                ...(pageGrid ? { pageGrid } : {}),
            });
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
        if (disabled || !sessionKey) return;
        setBusy('print');
        setMsg(null);
        try {
            // Fitted R×C HTML (same source as PDF), not the screen twin under HtmlPath.
            const res = await genericAgentSvc.ExportResult({
                skillKey,
                sessionKey,
                format: 'html',
                layout: exportLayout === 'table' ? 'table' : 'cards',
                title,
                ...(cardsPerRow > 0 ? { cardsPerRow } : {}),
                ...(cardRows > 0 ? { cardRows } : {}),
                ...(pageGrid ? { pageGrid } : {}),
            });
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
            setMsg(e instanceof Error ? e.message : 'Print failed.');
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
                            disabled={!!disabled || !!busy}
                            onClick={() => void onPrint()}
                        >
                            <i className="fa-solid fa-print mr-1" />
                            {busy === 'print' || busy === 'pdf' ? 'Working…' : 'Print'}
                        </button>
                        <button
                            type="button"
                            className={btnSecondary}
                            disabled={!!disabled || !!busy}
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

                {(searchField || multiFields.length > 0) && (
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
                {msg && <div className={`text-xs ${theme.label}`}>{msg}</div>}
            </div>
            <div className="w-full h-1 flex-auto min-h-0 min-w-0 overflow-auto">
                {children(filteredEvent, polished)}
            </div>
        </div>
    );
};
