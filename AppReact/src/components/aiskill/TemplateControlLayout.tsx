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

/**
 * Hosts TemplateContent control LAYOUT (HTML + CSS) inside React Result.
 * Cards / KPI — not iframe Wijmo.
 */
const TemplateControlLayout: React.FC<{ html: string; className?: string }> = ({ html, className }) => {
    useEffect(() => {
        if (!html) return;
        if (/agent-cards\.css/i.test(html)) ensureStylesheet('agent-cards-css', '/agent-ui/agent-cards.css');
        if (/agent-kpi\.css/i.test(html) || /agent-kpi-/i.test(html)) {
            ensureStylesheet('agent-kpi-css', '/agent-ui/agent-kpi.css');
        }
    }, [html]);

    if (!html?.trim()) return null;

    const board = /agent-cards-root/i.test(html || '') ? 'agent-cards-board' : '';
    return (
        <div
            className={`agent-template-host min-w-0 max-w-full ${board} ${className || ''}`}
            dangerouslySetInnerHTML={{ __html: html }}
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
        if (labelEl) labelEl.textContent = it.label || '—';
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
