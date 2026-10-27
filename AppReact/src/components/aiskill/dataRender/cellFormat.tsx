import React from 'react';

/**
 * Canonical cell formats from AI polish (`bindings.columns[].format`).
 * Classification is done by the LLM from field name + value samples — not by this file.
 * Here we only accept the contract vocabulary (and a few token aliases the model may emit).
 */
export type CellFormatKind = 'delta' | 'percent' | 'date' | 'image' | 'number' | 'currency';

const CANONICAL = new Set<CellFormatKind>([
    'delta', 'percent', 'date', 'image', 'number', 'currency',
]);

/** Map LLM format token → canonical kind. Does NOT inspect field/header names. */
export function normalizeCellFormat(raw?: string | null): CellFormatKind | undefined {
    if (!raw) return undefined;
    const s = String(raw).trim().toLowerCase().replace(/[\s-]+/g, '_');
    if (CANONICAL.has(s as CellFormatKind)) return s as CellFormatKind;
    // Token aliases only (same semantic enum the model was asked to use).
    if (s === 'deltadays' || s === 'delta_days' || s === 'signed_delta' || s === 'variance')
        return 'delta';
    if (s === 'percentage' || s === 'pct') return 'percent';
    if (s === 'datetime') return 'date';
    if (s === 'img' || s === 'photo' || s === 'sketch') return 'image';
    if (s === 'money') return 'currency';
    if (s === 'int' || s === 'integer' || s === 'float' || s === 'decimal') return 'number';
    return undefined;
}

function parseSignedNumber(raw: unknown): number | null {
    if (raw == null) return null;
    if (typeof raw === 'number' && Number.isFinite(raw)) return raw;
    let s = String(raw).trim();
    if (!s) return null;
    s = s.replace(/,/g, '').replace(/%/g, '').replace(/d$/i, '').trim();
    if (s.startsWith('+')) s = s.slice(1);
    const n = Number(s);
    return Number.isFinite(n) ? n : null;
}

/** Render a cell when polish already supplied `format` (LLM). */
export function FormattedResultCell(props: {
    value: unknown;
    format?: string | null;
    dateText?: string | null;
}): React.ReactElement {
    const kind = normalizeCellFormat(props.format);
    const raw = props.value;

    if (kind === 'delta') {
        const n = parseSignedNumber(raw);
        if (n == null) return <span>{raw == null ? '' : String(raw)}</span>;
        const neg = n < 0;
        const pos = n > 0;
        const color = neg ? '#dc2626' : pos ? '#16a34a' : '#6b7280';
        const icon = neg ? 'fa-solid fa-arrow-down' : pos ? 'fa-solid fa-arrow-up' : 'fa-solid fa-minus';
        let label = Math.abs(n).toLocaleString(undefined, { maximumFractionDigits: 2 });
        const src = String(raw ?? '');
        if (/d\b|day/i.test(src)) label += 'd';
        const sign = neg ? '-' : pos ? '+' : '';
        return (
            <span className="inline-flex items-center gap-1 whitespace-nowrap font-semibold" style={{ color }}>
                <i className={`${icon} text-[10px]`} aria-hidden />
                <span>{sign}{label}</span>
            </span>
        );
    }

    if (kind === 'percent') {
        const n = parseSignedNumber(raw);
        const text = raw == null || String(raw).trim() === ''
            ? ''
            : String(raw).includes('%')
                ? String(raw)
                : n != null
                    ? `${n.toLocaleString(undefined, { maximumFractionDigits: 2 })}%`
                    : String(raw);
        return (
            <span className="inline-flex items-center gap-1 whitespace-nowrap">
                <i className="fa-solid fa-percent text-[10px] opacity-70" aria-hidden />
                <span>{text}</span>
            </span>
        );
    }

    if (kind === 'date') {
        const text = props.dateText ?? (raw == null ? '' : String(raw));
        if (!text) return <span />;
        return (
            <span className="inline-flex items-center gap-1 whitespace-nowrap">
                <i className="fa-solid fa-calendar-days text-[10px] opacity-60" aria-hidden />
                <span>{text}</span>
            </span>
        );
    }

    if (raw == null || String(raw).trim() === '') return <span />;
    return <span>{String(raw)}</span>;
}
