import React, { useMemo, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { parsePageViews, type DataRenderEvent } from '../../webapi/genericAgentSvc';
import { DataRenderPanel } from './dataRender';
import { parseRows } from './dataRender/adapters';
import { parseJsonSafe } from './dataRender/types';
import TemplateControlLayout, {
    filterCardsHtmlBySearch,
    parseCardsHtmlFromSpec,
} from './TemplateControlLayout';

interface Props {
    event: DataRenderEvent;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

/** Child panels must not inherit page SpecJson; omit rather than assign undefined (TS excess checks). */
function toChildEvent(
    event: DataRenderEvent,
    patch: Partial<DataRenderEvent> & Pick<DataRenderEvent, 'Ui'>,
): DataRenderEvent {
    const { SpecJson: _omitSpec, MetaJson: _omitMeta, Html: _h, HtmlPath: _hp, HtmlUrl: _hu, ...rest } = event;
    return { ...rest, ...patch };
}

function filterRowsBySearch(
    rows: Record<string, unknown>[],
    search: string,
): Record<string, unknown>[] {
    const q = search.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter(r =>
        Object.values(r).some(v => {
            if (v == null || typeof v === 'object') return false;
            return String(v).toLowerCase().includes(q);
        }),
    );
}

function parseStatusOptions(specJson?: string | null): { field: string; options: string[] } | null {
    const root = parseJsonSafe<Record<string, unknown>>(specJson, {});
    const polished = (root.polishedSpec || root.PolishedSpec) as {
        bindings?: { filterFields?: Array<{ name?: string; Name?: string; control?: string; Control?: string }> };
        Bindings?: { FilterFields?: Array<{ Name?: string; Control?: string }> };
    } | null;
    const fields = polished?.bindings?.filterFields
        || (polished as { Bindings?: { FilterFields?: Array<{ Name?: string; Control?: string }> } })?.Bindings?.FilterFields
        || [];
    const multi = (fields as Array<Record<string, unknown>>).find(f => {
        const c = String(f.control ?? f.Control ?? '').toLowerCase();
        const n = String(f.name ?? f.Name ?? '');
        return (c === 'multiselect' || c === 'multi-select') && n && n !== '_search';
    });
    if (!multi) return null;
    const field = String(multi.name ?? multi.Name ?? '').trim();
    if (!field) return null;
    const optsMap = (root.filterOptions || root.FilterOptions || {}) as Record<string, string[]>;
    const options = Array.isArray(optsMap[field]) ? optsMap[field] : [];
    return { field, options };
}

/**
 * Interactive Result page: React owns stack/collapse; table = app Wijmo; cards = TemplateContent LAYOUT.
 * Each section has its own Search (and optional status chips) — not a shared ResultChrome filter.
 */
const InteractivePagePanel: React.FC<Props> = ({ event, disabled, onAction }) => {
    const { theme } = useTheme();
    const { views } = useMemo(
        () => parsePageViews(event.SpecJson),
        [event.SpecJson],
    );
    const allRows = useMemo(() => parseRows(event.DataJson), [event.DataJson]);
    const fullCardsHtml = useMemo(() => parseCardsHtmlFromSpec(event.SpecJson), [event.SpecJson]);
    const statusFilter = useMemo(() => parseStatusOptions(event.SpecJson), [event.SpecJson]);

    const [gridSearch, setGridSearch] = useState('');
    const [cardSearch, setCardSearch] = useState('');
    const [gridStatus, setGridStatus] = useState<string[]>([]);
    const [cardStatus, setCardStatus] = useState<string[]>([]);

    const gridRows = useMemo(() => {
        let rows = filterRowsBySearch(allRows, gridSearch);
        if (statusFilter && gridStatus.length > 0) {
            const set = new Set(gridStatus.map(s => s.toLowerCase()));
            const f = statusFilter.field;
            rows = rows.filter(r => set.has(String(r[f] ?? '').toLowerCase()));
        }
        return rows;
    }, [allRows, gridSearch, gridStatus, statusFilter]);

    const cardsHtml = useMemo(() => {
        if (!fullCardsHtml) return null;
        let html = filterCardsHtmlBySearch(fullCardsHtml, cardSearch);
        if (statusFilter && cardStatus.length > 0) {
            html = filterCardsHtmlByStatus(html, statusFilter.field, cardStatus);
        }
        return html;
    }, [fullCardsHtml, cardSearch, cardStatus, statusFilter]);

    const gridEvent = useMemo(
        () => toChildEvent(event, {
            Ui: 'grid',
            Layout: 'table',
            Title: '',
            DataJson: JSON.stringify(gridRows),
            RowCount: gridRows.length,
        }),
        [event, gridRows],
    );

    const sectionCount = (views.includes('table') ? 1 : 0) + (views.includes('cards') ? 1 : 0);
    const useCollapse = sectionCount >= 2;
    const inp = `h-7 px-2 text-xs border rounded-[4px] w-48 max-w-full ${theme.inputBox}`;

    return (
        <div className="w-full h-full min-w-0 max-w-full overflow-auto flex flex-col gap-2 p-1 box-border">
            {views.includes('table') && (
                <PageSection
                    title="Grid view"
                    collapse={useCollapse}
                    toolbar={(
                        <SectionFilterBar
                            search={gridSearch}
                            onSearch={setGridSearch}
                            disabled={disabled}
                            inpClass={inp}
                            statusField={statusFilter?.field}
                            statusOptions={statusFilter?.options}
                            statusSelected={gridStatus}
                            onStatusChange={setGridStatus}
                            themeLabel={theme.label}
                            themeBtn={theme.button_default}
                        />
                    )}
                >
                    {/*
                      Do not use fillHeight here: parent only had maxHeight, so Wijmo h-1 flex-auto
                      collapsed to a ~toolbar strip when Card view shared the page.
                    */}
                    <div className="min-w-0 max-w-full overflow-auto border border-gray-200 rounded-[4px]">
                        <DataRenderPanel
                            event={gridEvent}
                            disabled={disabled}
                            onAction={onAction}
                            fillHeight={false}
                        />
                    </div>
                </PageSection>
            )}
            {views.includes('cards') && (
                <PageSection
                    title="Card view"
                    collapse={useCollapse}
                    toolbar={(
                        <SectionFilterBar
                            search={cardSearch}
                            onSearch={setCardSearch}
                            disabled={disabled}
                            inpClass={inp}
                            statusField={statusFilter?.field}
                            statusOptions={statusFilter?.options}
                            statusSelected={cardStatus}
                            onStatusChange={setCardStatus}
                            themeLabel={theme.label}
                            themeBtn={theme.button_default}
                        />
                    )}
                >
                    <div className="min-w-0 max-w-full overflow-x-auto">
                        {cardsHtml ? (
                            <TemplateControlLayout html={cardsHtml} />
                        ) : (
                            <div className={`text-xs px-1 ${theme.label}`}>
                                Card view requires TemplateContent (cardsHtml). Re-run render with polish.
                            </div>
                        )}
                    </div>
                </PageSection>
            )}
        </div>
    );
};

const SectionFilterBar: React.FC<{
    search: string;
    onSearch: (v: string) => void;
    disabled?: boolean;
    inpClass: string;
    statusField?: string;
    statusOptions?: string[];
    statusSelected: string[];
    onStatusChange: (v: string[]) => void;
    themeLabel: string;
    themeBtn: string;
}> = ({
    search, onSearch, disabled, inpClass,
    statusField, statusOptions, statusSelected, onStatusChange,
    themeLabel, themeBtn,
}) => {
    const toggle = (value: string) => {
        onStatusChange(
            statusSelected.includes(value)
                ? statusSelected.filter(s => s !== value)
                : [...statusSelected, value],
        );
    };
    return (
        <div
            className="flex flex-wrap items-center gap-2 w-full"
            onClick={e => e.stopPropagation()}
            onKeyDown={e => e.stopPropagation()}
        >
            <div className="flex items-center gap-1 min-w-[160px]">
                <i className={`fa-solid fa-magnifying-glass text-xs ${themeLabel}`} aria-hidden />
                <input
                    className={inpClass}
                    placeholder="Search this view…"
                    value={search}
                    disabled={!!disabled}
                    onChange={e => onSearch(e.target.value)}
                />
            </div>
            {statusField && statusOptions && statusOptions.length > 0 && (
                <div className="flex flex-wrap items-center gap-1">
                    <button
                        type="button"
                        className={`px-2 py-0.5 text-xs rounded-full border ${statusSelected.length === 0 ? themeBtn : 'opacity-70'} ${themeLabel}`}
                        onClick={() => onStatusChange([])}
                    >
                        All
                    </button>
                    {statusOptions.map(v => {
                        const on = statusSelected.includes(v);
                        return (
                            <button
                                key={v}
                                type="button"
                                className={`px-2 py-0.5 text-xs rounded-full border ${on ? themeBtn : 'opacity-60'} ${themeLabel}`}
                                disabled={!!disabled}
                                onClick={() => toggle(v)}
                            >
                                {v}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
};

const PageSection: React.FC<{
    title: string;
    collapse: boolean;
    toolbar?: React.ReactNode;
    children: React.ReactNode;
}> = ({ title, collapse, toolbar, children }) => {
    if (!collapse) {
        return (
            <div className="min-w-0 max-w-full mb-2 flex flex-col gap-2">
                <div className="flex flex-wrap items-center justify-between gap-2 px-1">
                    <span className="text-sm font-semibold">{title}</span>
                    {toolbar}
                </div>
                {children}
            </div>
        );
    }
    return (
        <details open className="min-w-0 max-w-full mb-2 rounded-[4px] border border-gray-300 bg-white">
            <summary className="cursor-pointer select-none px-3 py-2 text-sm font-semibold list-none flex flex-wrap items-center justify-between gap-2">
                <span>{title}</span>
                {toolbar}
            </summary>
            <div className="px-3 pb-3 min-w-0 max-w-full overflow-hidden">{children}</div>
        </details>
    );
};

/** Status multi-filter for cards: keep cards that contain any selected status text. */
function filterCardsHtmlByStatus(html: string, _field: string, selected: string[]): string {
    if (!html?.trim() || !selected.length || typeof document === 'undefined') return html;
    const wrap = document.createElement('div');
    wrap.innerHTML = html;
    const cards = Array.from(
        wrap.querySelectorAll<HTMLElement>('article.agent-card, [data-role="card"]'),
    );
    const set = new Set(selected.map(s => s.toLowerCase()));
    for (const card of cards) {
        const statusEl = card.querySelector('[data-role="statusPill"], .agent-card-status');
        const status = (statusEl?.textContent || '').trim().toLowerCase();
        const keep = status && set.has(status);
        if (!keep) card.remove();
    }
    return wrap.innerHTML;
}

export default InteractivePagePanel;
