import { unwrapQuoted } from './imageValue';

/** ISO-like date/datetime strings from APIs (with or without time / Z). */
const ISO_DATE =
    /^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:?\d{2})?)?$/;

export function isDateColumnName(name: string | null | undefined): boolean {
    const n = (name || '').trim();
    if (!n) return false;
    if (/delta|count|qty|amount|price|id$/i.test(n) && !/date|time/i.test(n)) return false;
    return /(^|[^a-z])(date|time|datetime|timestamp|target|actual|adopt|ship|cancel)([^a-z]|$)/i.test(n)
        || /_(dt|date|time)$/i.test(n);
}

export function parseDateValue(value: unknown): Date | null {
    if (value instanceof Date && !Number.isNaN(value.getTime())) return value;
    if (typeof value === 'number' && Number.isFinite(value)) {
        const d = new Date(value);
        return Number.isNaN(d.getTime()) ? null : d;
    }
    const text = unwrapQuoted(value);
    if (!text || !ISO_DATE.test(text)) return null;
    const d = new Date(text);
    return Number.isNaN(d.getTime()) ? null : d;
}

/** Locale short date, e.g. Jun 4, 2026. Null if not a date. */
export function formatDateValue(value: unknown): string | null {
    const d = parseDateValue(value);
    if (!d) return null;
    return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}
