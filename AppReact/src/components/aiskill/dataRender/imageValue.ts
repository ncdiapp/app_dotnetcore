import { endpoints } from '../../../webapi/endpoints';

const RESOURCE_API = /\/api\/(?:resources|files)\//i;

/** Strip wrapping quotes from JSON-ish cell text. */
export function unwrapQuoted(value: unknown): string {
    let text = typeof value === 'string' ? value.trim() : value == null ? '' : String(value).trim();
    if (
        (text.startsWith('"') && text.endsWith('"') && text.length >= 2)
        || (text.startsWith("'") && text.endsWith("'") && text.length >= 2)
    ) {
        text = text.slice(1, -1).trim();
    }
    return text;
}

/** Field name means this column holds an image (Image, Sketch, Photo, …). */
export function isImageColumnName(name: string | null | undefined): boolean {
    const n = (name || '').trim();
    if (!n) return false;
    if (/^(image|img|sketch|photo|thumbnail|thumb|picture|pic|artwork|displayimage|display_image)$/i.test(n))
        return true;
    return /(^|[^a-z])(image|sketch|photo|thumb|picture)([^a-z]|$)/i.test(n);
}

/** Value is a URL (http(s) or absolute path). */
export function isUrlValue(value: unknown): boolean {
    const text = unwrapQuoted(value);
    if (!text || text.length > 2048) return false;
    if (/\s/.test(text)) return false;
    if (/^https?:\/\/\S+/i.test(text)) return true;
    if (text.startsWith('/') && text.length > 1) return true;
    return false;
}

/**
 * Image rendering field = field name means image AND value is a URL.
 * App /api/… URLs use session fetch; other http(s) URLs load directly.
 */
export function cellImageKind(value: unknown, columnName?: string | null): 'resource' | 'external' | null {
    if (!isImageColumnName(columnName)) return null;
    const text = unwrapQuoted(value);
    if (!isUrlValue(text)) return null;
    if (RESOURCE_API.test(text) || (text.startsWith('/api/') && /\/(?:image|regular|thumbnail|latest)\b/i.test(text)))
        return 'resource';
    return 'external';
}

/** @deprecated Prefer cellImageKind(value, columnName). Kept for callers without a field name. */
export function imageUrlKind(value: unknown): 'resource' | 'external' | null {
    const text = unwrapQuoted(value);
    if (!isUrlValue(text)) return null;
    if (RESOURCE_API.test(text) || (text.startsWith('/api/') && /\/(?:image|regular|thumbnail|latest)\b/i.test(text)))
        return 'resource';
    if (/\.(?:png|jpe?g|gif|webp|svg|bmp)(?:\?|$)/i.test(text)) return 'external';
    if (/\/image\/\d+/i.test(text)) return 'external';
    return null;
}

/** Path passed to fetchAuthenticatedImageBlobUrl (it prefixes BASE_URL). */
export function toAppResourcePath(url: string): string {
    let path = unwrapQuoted(url);
    if (/^https?:\/\//i.test(path)) {
        try {
            path = new URL(path).pathname;
        } catch {
            return url;
        }
    }
    const base = (endpoints.BASE_URL || '').replace(/\/$/, '');
    if (base && path.toLowerCase().startsWith(`${base.toLowerCase()}/`)) {
        path = path.substring(base.length);
    }
    return path.startsWith('/') ? path : `/${path}`;
}
