import { endpoints } from '../../../webapi/endpoints';

const RESOURCE_IMAGE = /\/api\/resources\/\S+\/image\//i;
const FILE_IMAGE = /\/api\/files\/(?:regular|image|thumbnail|latest)\/\d+/i;
const EXT_IMAGE = /^https?:\/\/\S+\.(?:png|jpe?g|gif|webp|svg|bmp)(?:\?\S*)?$/i;

/** App file/resource image, or an external URL with an image extension. */
export function imageUrlKind(value: unknown): 'resource' | 'external' | null {
    const text = typeof value === 'string' ? value.trim() : '';
    if (!text) return null;
    if (RESOURCE_IMAGE.test(text) || FILE_IMAGE.test(text)) return 'resource';
    if (EXT_IMAGE.test(text)) return 'external';
    return null;
}

/** Path passed to fetchAuthenticatedImageBlobUrl (it prefixes BASE_URL). */
export function toAppResourcePath(url: string): string {
    let path = url.trim();
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
