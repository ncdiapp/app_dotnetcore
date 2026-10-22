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
    layoutHints?: { cardsPerRow?: number; statusColorMap?: Record<string, string> };
};

function parseSpecBlob(event: DataRenderEvent | null | undefined): {
    polished: PolishedSpecClient | null;
    filterOptions: Record<string, string[]>;
    htmlPath: string | null;
} {
    const raw = event?.SpecJson || event?.MetaJson || null;
    const obj = parseJsonSafe<Record<string, unknown>>(raw, {});
    const polished = (obj.polishedSpec || obj.PolishedSpec) as PolishedSpecClient | null;
    const filterOptions = (obj.filterOptions || obj.FilterOptions || {}) as Record<string, string[]>;
    const htmlPath = event?.HtmlPath || (obj.htmlPath as string) || null;
    return { polished: polished || null, filterOptions: filterOptions || {}, htmlPath };
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

interface Props {
    event: DataRenderEvent;
    skillKey: string;
    sessionKey: string | null;
    disabled?: boolean;
    children: (filteredEvent: DataRenderEvent, polished: PolishedSpecClient | null) => React.ReactNode;
}

/**
 * React Result chrome: Filter / Print / Export (design redesign).
 * Filters client-side over DataJson; Print uses none-interactive twin HtmlPath or export_pdf.
 */
export const ResultChrome: React.FC<Props> = ({
    event,
    skillKey,
    sessionKey,
    disabled,
    children,
}) => {
    const { theme } = useTheme();
    const { polished, filterOptions, htmlPath } = useMemo(() => parseSpecBlob(event), [event]);
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

    const filteredEvent = useMemo((): DataRenderEvent => {
        if (!event.DataJson || filteredRows.length === allRows.length) return event;
        // Drop pre-merged cardsHtml so React rebinds filtered rows with polish field selection.
        let specJson = event.SpecJson;
        try {
            if (specJson) {
                const o = JSON.parse(specJson) as Record<string, unknown>;
                delete o.cardsHtml;
                delete o.CardsHtml;
                o.clientFiltered = true;
                specJson = JSON.stringify(o);
            }
        } catch { /* keep */ }
        return {
            ...event,
            DataJson: JSON.stringify(filteredRows),
            RowCount: filteredRows.length,
            SpecJson: specJson,
            MetaJson: specJson || event.MetaJson,
        };
    }, [event, filteredRows, allRows.length]);

    const title = polished?.title || event.Title || 'Result';
    const total = allRows.length > 0 ? allRows.length : (event.RowCount || 0);
    const shown = allRows.length > 0 ? filteredRows.length : total;

    const btnPrimary = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const btnSecondary = `px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`;
    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`;

    const toggleMulti = (field: string, value: string) => {
        setMulti(prev => {
            const cur = prev[field] ?? [];
            const next = cur.includes(value) ? cur.filter(v => v !== value) : [...cur, value];
            return { ...prev, [field]: next };
        });
    };

    const layout = (event.Layout || event.Ui || 'table').toLowerCase();
    const layoutNorm = layout === 'card' ? 'cards' : layout;

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
                layout: layoutNorm === 'cards' || layoutNorm === 'table' || layoutNorm === 'page'
                    ? (layoutNorm === 'page' ? 'cards' : layoutNorm)
                    : layoutNorm,
                title,
                datasetName: undefined,
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
        if (disabled) return;
        setBusy('print');
        setMsg(null);
        try {
            if (htmlPath && sessionKey) {
                const html = await genericAgentSvc.ReadAgentFileText(skillKey, sessionKey, htmlPath);
                if (html) {
                    const w = window.open('', '_blank');
                    if (w) {
                        w.document.open();
                        w.document.write(html);
                        w.document.close();
                        w.focus();
                        setTimeout(() => { try { w.print(); } catch { /* ignore */ } }, 300);
                        setBusy(null);
                        return;
                    }
                }
            }
            // Fallback: export PDF via server GENERATE
            await runExport('pdf');
        } catch (e: unknown) {
            setMsg(e instanceof Error ? e.message : 'Print failed.');
            setBusy(null);
        }
    };

    const showChrome = !!(polished || allRows.length > 0 || htmlPath);

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
                            {busy === 'print' || busy === 'pdf' ? 'Working…' : 'Print / Save as PDF'}
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
                            <div className={`absolute right-0 top-full mt-1 z-20 min-w-[140px] rounded-[4px] border shadow-md py-1 ${theme.mainContentSection}`}>
                                {(['pdf', 'docx', 'pptx', 'html', 'xlsx'] as const).map(fmt => (
                                    <button
                                        key={fmt}
                                        type="button"
                                        className={`block w-full text-left px-3 py-1.5 text-xs ${theme.label} hover:opacity-80`}
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
