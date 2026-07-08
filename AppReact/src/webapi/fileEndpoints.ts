/**
 * Central helpers for file/image serving URLs.
 *
 * All file endpoints are served by the Core app (which owns the FileRepository and has the
 * permission-checked handlers under /api/files/*). URLs MUST be relative to BASE_URL so the
 * request resolves same-origin:
 *   - dev: through the CRA proxy (:3000 -> :52740)
 *   - prod: the Core app that serves the SPA
 * An absolute URL (buildEndpointUrl) would target the legacy IIS app at port 80 and fail.
 *
 * Browser navigations (window.open / <a href>) to a relative API path are intercepted by the
 * CRA dev server and return index.html (the SPA home) instead of the file. For downloads use
 * {@link downloadFileById}, which fetches the bytes via XHR (correctly proxied) and saves them.
 */
import { endpoints } from './endpoints';
import { store } from '../redux/store';

function getSessionId(): string | null {
  const fromStore = (store.getState() as any)?.userSession?.userContext?.SessionId;
  if (fromStore) return String(fromStore);
  if (typeof window !== 'undefined') return localStorage.getItem('sessionId');
  return null;
}

function sessionQuery(): string {
  const sid = getSessionId();
  return sid ? `?CurrentUserSessionId=${encodeURIComponent(sid)}` : '';
}

/** Thumbnail image (small). Replaces GetThumbnailImage.aspx. */
export function fileThumbnailUrl(fileId: number | string): string {
  return `${endpoints.BASE_URL}/api/files/thumbnail/${fileId}${sessionQuery()}`;
}

/**
 * Resolve a static resource path from search results (e.g. /api/resources/Company_1/Image/thumbnail/guid).
 */
export function fileResourceUrl(apiResourcePath: string): string {
  if (!apiResourcePath) return apiResourcePath;
  if (apiResourcePath.startsWith('http://') || apiResourcePath.startsWith('https://')) {
    return apiResourcePath;
  }
  const path = apiResourcePath.startsWith('/') ? apiResourcePath : `/${apiResourcePath}`;
  return `${endpoints.BASE_URL}${path}`;
}

/**
 * Resolve search thumbnail src. When the search row includes DictThumbnailUrl (new API),
 * only use server-provided resource URLs — never fall back to /api/files/thumbnail (avoids hang on missing files).
 * Legacy rows without DictThumbnailUrl still use the FileId thumbnail API.
 */
export function resolveSearchThumbnailUrl(
  fileId: number | string | null | undefined,
  resourceUrl?: string | null,
  searchUsesThumbnailUrls = false,
): string | null {
  if (resourceUrl) return fileResourceUrl(resourceUrl);
  if (searchUsesThumbnailUrls) return null;
  const numericId = Number(fileId);
  if (!Number.isFinite(numericId) || numericId <= 0) return null;
  return fileThumbnailUrl(numericId);
}

/** Original full-size image. Replaces GetImage.aspx / original image use. */
export function fileImageUrl(fileId: number | string): string {
  return `${endpoints.BASE_URL}/api/files/image/${fileId}${sessionQuery()}`;
}

/** Regular (medium) size image. Replaces GetRegularImage.aspx. */
export function fileRegularUrl(fileId: number | string): string {
  return `${endpoints.BASE_URL}/api/files/regular/${fileId}${sessionQuery()}`;
}

/** Latest-version file for inline view/preview/open. Replaces GetLatestFile.aspx (no IsDownload). */
export function fileLatestUrl(fileId: number | string): string {
  return `${endpoints.BASE_URL}/api/files/latest/${fileId}${sessionQuery()}`;
}

/** Force-download URL. Replaces GetLatestFile.aspx?...&IsDownload=true. Prefer downloadFileById. */
export function fileDownloadUrl(fileId: number | string): string {
  return `${endpoints.BASE_URL}/api/files/stream/${fileId}${sessionQuery()}`;
}

function parseFileNameFromContentDisposition(header: string | null): string | null {
  if (!header) return null;
  const utf8 = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (utf8?.[1]) {
    try { return decodeURIComponent(utf8[1].replace(/"/g, '').trim()); } catch { /* ignore */ }
  }
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain?.[1]?.trim() ?? null;
}

/**
 * Download a file by fetching its bytes (XHR, correctly proxied in dev) and triggering a save.
 * Fixes the case where window.open of a relative API path lands on the SPA home page in dev.
 */
export async function downloadFileById(fileId: number | string, fallbackName?: string): Promise<void> {
  const url = fileDownloadUrl(fileId);
  const resp = await fetch(url, { credentials: 'include' });
  if (!resp.ok) {
    throw new Error(`Download failed (${resp.status})`);
  }
  const blob = await resp.blob();
  const name =
    parseFileNameFromContentDisposition(resp.headers.get('Content-Disposition')) ||
    fallbackName ||
    `file_${fileId}`;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
}
