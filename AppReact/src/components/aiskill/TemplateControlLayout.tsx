import React, { useEffect, useMemo } from 'react';

function ensureStylesheet(id: string, href: string) {
    if (typeof document === 'undefined') return;
    if (document.getElementById(id)) return;
    const link = document.createElement('link');
    link.id = id;
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
}

/** Strip legacy blur layers + drop fixed --cards-per-row so CSS flex wrap can size columns. */
export function normalizeCardsHtmlForResult(html: string): string {
    if (!html?.trim() || typeof document === 'undefined') return html;
    if (!/agent-card/i.test(html)) return html;
    const wrap = document.createElement('div');
    wrap.innerHTML = html;
    wrap.querySelectorAll('.agent-card-hero-blur').forEach(el => el.remove());
    wrap.querySelectorAll('.agent-cards-grid, [data-role="cards"]').forEach(grid => {
        (grid as HTMLElement).style.removeProperty('--cards-per-row');
        if ((grid as HTMLElement).getAttribute('style')?.trim() === '')
            grid.removeAttribute('style');
        grid.classList.add('agent-cards-grid');
    });
    return wrap.innerHTML;
}

/**
 * Hosts TemplateContent control LAYOUT (HTML + CSS) inside React Result.
 * Cards / KPI — not iframe Wijmo.
 */
const TemplateControlLayout: React.FC<{ html: string; className?: string }> = ({ html, className }) => {
    useEffect(() => {
        if (!html) return;
        if (/agent-cards\.css/i.test(html) || /agent-card/i.test(html)) {
            ensureStylesheet('agent-cards-css', '/agent-ui/agent-cards.css?v=251');
        }
        if (/agent-kpi\.css/i.test(html) || /agent-kpi-/i.test(html)) {
            ensureStylesheet('agent-kpi-css', '/agent-ui/agent-kpi.css');
        }
    }, [html]);

    const enriched = useMemo(
        () => (/agent-card/i.test(html || '') ? normalizeCardsHtmlForResult(html) : html),
        [html],
    );

    if (!enriched?.trim()) return null;

    const board = /agent-cards-root/i.test(enriched || '') ? 'agent-cards-board' : '';
    const isCards = !!board || /agent-cards-grid/i.test(enriched);
    return (
        <div
            className={`agent-template-host min-w-0 max-w-full w-full ${board} ${isCards ? 'flex flex-col' : ''} ${className || ''}`}
            dangerouslySetInnerHTML={{ __html: enriched }}
        />
    );
};

export function parseCardsHtmlFromSpec(specJson?: string | null): string | null {
    if (!specJson?.trim()) return null;
    try {
        const o = JSON.parse(specJson) as { cardsHtml?: string; CardsHtml?: string };
        const h = o.cardsHtml ?? o.CardsHtml;
        return typeof h === 'string' && h.trim() ? h : null;
    } catch {
        return null;
    }
}

export type DashboardLayoutSpec = {
    kpiHtml?: string | null;
    chartHostHtml?: string | null;
    gridHostHtml?: string | null;
};

export function parseDashboardLayoutsFromSpec(specJson?: string | null): DashboardLayoutSpec {
    if (!specJson?.trim()) return {};
    try {
        const o = JSON.parse(specJson) as {
            kpiHtml?: string; KpiHtml?: string;
            chartHostHtml?: string; ChartHostHtml?: string;
            gridHostHtml?: string; GridHostHtml?: string;
        };
        return {
            kpiHtml: o.kpiHtml ?? o.KpiHtml ?? null,
            chartHostHtml: o.chartHostHtml ?? o.ChartHostHtml ?? null,
            gridHostHtml: o.gridHostHtml ?? o.GridHostHtml ?? null,
        };
    } catch {
        return {};
    }
}

/**
 * Keep only cards whose id/title (or full text) matches a cell value in filtered rows.
 */
export function filterCardsHtmlByRows(
    html: string,
    rows: Record<string, unknown>[],
): string {
    if (!html?.trim() || typeof document === 'undefined') return html;
    // Fast path: unfiltered full set — skip DOM parse (critical for resize smoothness).
    const cardApprox = (html.match(/<article\b/gi) || []).length;
    if (cardApprox > 0 && rows.length >= cardApprox) return html;

    const wrap = document.createElement('div');
    wrap.innerHTML = html;
    const cards = Array.from(
        wrap.querySelectorAll<HTMLElement>('article.agent-card, [data-role="card"]'),
    );
    if (!cards.length) return html;

    const valueSet = new Set<string>();
    for (const r of rows) {
        for (const v of Object.values(r)) {
            if (v == null || typeof v === 'object') continue;
            const s = String(v).trim().toLowerCase();
            if (s) valueSet.add(s);
        }
    }

    for (const card of cards) {
        const idEl = card.querySelector(
            '[data-role="idLink"], .agent-card-id, [data-role="title"], .agent-card-name',
        );
        const id = (idEl?.textContent || '').trim().toLowerCase();
        let keep = false;
        if (rows.length === 0) {
            keep = false;
        } else if (id && valueSet.has(id)) {
            keep = true;
        } else {
            const texts = Array.from(card.querySelectorAll('[data-role]'))
                .map(el => (el.textContent || '').trim().toLowerCase())
                .filter(t => t.length >= 2);
            keep = texts.some(t => valueSet.has(t));
        }
        if (!keep) card.remove();
    }
    return wrap.innerHTML;
}

/** Card-section Search: keep cards whose visible text contains the query (all roles). */
export function filterCardsHtmlBySearch(html: string, search: string): string {
    if (!html?.trim() || typeof document === 'undefined') return html;
    const q = search.trim().toLowerCase();
    if (!q) return html;
    const wrap = document.createElement('div');
    wrap.innerHTML = html;
    const cards = Array.from(
        wrap.querySelectorAll<HTMLElement>('article.agent-card, [data-role="card"]'),
    );
    for (const card of cards) {
        const text = (card.textContent || '').toLowerCase();
        if (!text.includes(q)) card.remove();
    }
    return wrap.innerHTML;
}

/** Rebind KPI card values after client filter — preserves TemplateContent classes/structure. */
export function rebindKpiHtml(
    baseHtml: string,
    items: Array<{ label: string; value: string; field?: string }>,
): string {
    if (!baseHtml?.trim() || typeof document === 'undefined') return baseHtml;
    const wrap = document.createElement('div');
    wrap.innerHTML = baseHtml;
    const cards = Array.from(
        wrap.querySelectorAll<HTMLElement>('[data-role="kpi-card"], .agent-kpi-card, .kpi-card'),
    );
    if (!cards.length) return baseHtml;

    const row =
        wrap.querySelector('[data-role="kpi"], .agent-kpi-row, .kpi-row')
        || cards[0].parentElement;
    if (!row) return baseHtml;

    const proto = cards[0].cloneNode(true) as HTMLElement;
    row.innerHTML = '';
    for (const it of items) {
        const card = proto.cloneNode(true) as HTMLElement;
        if (it.field) card.setAttribute('data-kpi-field', it.field);
        const labelEl = card.querySelector('[data-role="label"], .agent-kpi-label, .kpi-label');
        const valueEl = card.querySelector('[data-role="value"], .agent-kpi-value, .kpi-value');
        if (labelEl) {
            const text = (it.label || '').trim() || (it.field || '').trim() || '—';
            labelEl.textContent = text;
        }
        if (valueEl) valueEl.textContent = it.value || '—';
        row.appendChild(card);
    }
    return wrap.innerHTML;
}

export function useReboundKpiHtml(
    baseHtml: string | null | undefined,
    items: Array<{ label: string; value: string; field?: string }>,
): string | null {
    return useMemo(() => {
        if (!baseHtml?.trim()) return null;
        if (!items.length) return baseHtml;
        return rebindKpiHtml(baseHtml, items);
    }, [baseHtml, items]);
}

export default TemplateControlLayout;
