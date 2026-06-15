const BASE_URL = "/appai";

const trimTrailingSlash = (value: string) =>
  value.length > 1 && value.endsWith("/") ? value.slice(0, -1) : value;

const ensureLeadingSlash = (value: string) =>
  value.startsWith("/") ? value : `/${value}`;

/**
 * Origin to use for API and backend links (e.g. download).
 * In dev (port 3000) use protocol+hostname so links go to proxy target (e.g. http://localhost).
 * In production use current origin.
 */
const getApiOrigin = (): string => {
  if (typeof window === "undefined" || !window.location) return "";
  const { protocol, hostname, port, origin } = window.location;
  // In dev the frontend usually runs on a different port (3000/5173/etc) than the backend.
  // For backend links (download/image) we must hit the API host, not the SPA dev server.
  // Strip non-standard ports to ensure we target the backend origin.
  if (port && port !== "80" && port !== "443") return `${protocol}//${hostname}`;
  return origin;
};

export const buildEndpointUrl = (path: string = "") => {
  const origin = getApiOrigin();
  const normalizedBase = trimTrailingSlash(BASE_URL);
  const normalizedPath = ensureLeadingSlash(path);
  return `${origin}${normalizedBase}${normalizedPath}`;
};

/**
 * Converts a relative resource path (e.g. from API) to a full URL for the current app.
 * Use for image URLs like LogoImageUrl or BackgroundImageUrlList that the backend returns as
 * e.g. "/FileRepository/Company_4026/Image/Background/1-2.png".
 */
export const toAbsoluteResourceUrl = (relativeOrAbsolute: string): string => {
  if (!relativeOrAbsolute) return relativeOrAbsolute;
  if (
    relativeOrAbsolute.startsWith("http://") ||
    relativeOrAbsolute.startsWith("https://")
  ) {
    return relativeOrAbsolute;
  }
  const base = trimTrailingSlash(buildEndpointUrl(""));
  const path = relativeOrAbsolute.startsWith("/")
    ? relativeOrAbsolute.slice(1)
    : relativeOrAbsolute;
  return `${base}/${path}`;
};

export const endpoints = {
  BASE_URL,
  buildEndpointUrl,
  toAbsoluteResourceUrl,
};