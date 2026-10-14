/**
 * Api Config Parameters form model.
 * The database still stores one JSON string. The form splits and joins BaseUrl + Url
 * and keeps every JSON key the form does not edit.
 */

export const SESSION_HEADER_KEY = 'CurrentUserSessionId';

export type KvRow = { key: string; value: string };

export type ApiConfigFormModel = {
  apiUrl: string;
  headers: KvRow[];
  queryParams: KvRow[];
  pathParams: KvRow[];
  postProcessMethodName: string;
  responseToEnv: KvRow[];
  cookieHeaderNames: string[];
  /** Original JSON object. Unknown keys are copied through on save. */
  raw: Record<string, unknown>;
};

const KEPT_KEYS = new Set([
  'BaseUrl',
  'Url',
  'Headers',
  'QueryParams',
  'PathParams',
  'PostProcessMethodName',
  'ResponseObjectMapToEnvionmentVariable',
  'ResponseHeaderNeedToSetCookieNames',
]);

function asObject(value: unknown): Record<string, unknown> {
  if (value && typeof value === 'object' && !Array.isArray(value)) return { ...(value as Record<string, unknown>) };
  return {};
}

function displayValue(value: unknown): string {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value);
  } catch {
    return String(value);
  }
}

function rowsFromDict(value: unknown): KvRow[] {
  const obj = asObject(value);
  return Object.keys(obj).map((key) => ({ key, value: displayValue(obj[key]) }));
}

export function joinApiUrl(baseUrl: unknown, url: unknown): string {
  const base = displayValue(baseUrl).trim();
  const path = displayValue(url).trim();
  if (!base && !path) return '';
  if (!base) return path;
  if (!path) return base;
  return `${base.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
}

export function isAbsoluteHttpUrl(apiUrl: string): boolean {
  return /^https?:\/\//i.test((apiUrl ?? '').trim());
}

function parseQueryString(query: string): { rows: KvRow[]; unparsed: boolean } {
  if (!query) return { rows: [], unparsed: false };
  const rows: KvRow[] = [];
  for (const part of query.split('&')) {
    if (!part) continue;
    const eq = part.indexOf('=');
    if (eq <= 0) return { rows: [], unparsed: true };
    const key = decodeURIComponent(part.slice(0, eq));
    const value = decodeURIComponent(part.slice(eq + 1));
    if (!key) return { rows: [], unparsed: true };
    rows.push({ key, value });
  }
  return { rows, unparsed: false };
}

/** Split one API URL into BaseUrl, Url, and any query pairs found in the box. */
export function splitApiUrl(apiUrl: string): { baseUrl: string; url: string; queryFromUrl: KvRow[] | null } {
  const text = (apiUrl ?? '').trim();
  if (!text) return { baseUrl: '', url: '', queryFromUrl: null };
  const match = /^(https?:\/\/[^/\s?#]+)\/?(.*)$/i.exec(text);
  if (!match) return { baseUrl: '', url: text, queryFromUrl: null };
  const baseUrl = match[1];
  let rest = match[2] ?? '';
  const q = rest.indexOf('?');
  if (q < 0) return { baseUrl, url: rest.replace(/^\/+/, ''), queryFromUrl: null };
  const path = rest.slice(0, q).replace(/^\/+/, '');
  const parsed = parseQueryString(rest.slice(q + 1));
  if (parsed.unparsed) return { baseUrl, url: rest.replace(/^\/+/, ''), queryFromUrl: null };
  return { baseUrl, url: path, queryFromUrl: parsed.rows };
}

function mergeQueryRows(existing: KvRow[], fromUrl: KvRow[] | null): KvRow[] {
  if (!fromUrl) return existing;
  const map = new Map<string, string>();
  for (const row of existing) {
    if (row.key.trim()) map.set(row.key, row.value);
  }
  for (const row of fromUrl) map.set(row.key, row.value);
  return Array.from(map.entries()).map(([key, value]) => ({ key, value }));
}

function ensureSessionRow(rows: KvRow[]): KvRow[] {
  const rest = rows.filter((row) => row.key !== SESSION_HEADER_KEY);
  const current = rows.find((row) => row.key === SESSION_HEADER_KEY);
  return [{ key: SESSION_HEADER_KEY, value: current?.value ?? '' }, ...rest];
}

export function emptyApiConfigForm(): ApiConfigFormModel {
  return {
    apiUrl: '',
    headers: [{ key: SESSION_HEADER_KEY, value: '' }],
    queryParams: [],
    pathParams: [],
    postProcessMethodName: '',
    responseToEnv: [],
    cookieHeaderNames: [],
    raw: {},
  };
}

export function parseApiConfig(source: unknown): ApiConfigFormModel {
  let raw: Record<string, unknown> = {};
  if (typeof source === 'string') {
    const text = source.trim();
    if (text) {
      try {
        raw = asObject(JSON.parse(text));
      } catch {
        raw = {};
      }
    }
  } else {
    raw = asObject(source);
  }
  return {
    apiUrl: joinApiUrl(raw.BaseUrl, raw.Url),
    headers: ensureSessionRow(rowsFromDict(raw.Headers)),
    queryParams: rowsFromDict(raw.QueryParams),
    pathParams: rowsFromDict(raw.PathParams),
    postProcessMethodName: raw.PostProcessMethodName == null ? '' : displayValue(raw.PostProcessMethodName),
    responseToEnv: rowsFromDict(raw.ResponseObjectMapToEnvionmentVariable),
    cookieHeaderNames: Array.isArray(raw.ResponseHeaderNeedToSetCookieNames)
      ? raw.ResponseHeaderNeedToSetCookieNames.map((name) => displayValue(name)).filter((name) => name !== '')
      : [],
    raw,
  };
}

function dictFromRows(rows: KvRow[], sessionNull: boolean): Record<string, string | null> {
  const dict: Record<string, string | null> = {};
  for (const row of rows) {
    const key = row.key.trim();
    if (!key) continue;
    if (sessionNull && key === SESSION_HEADER_KEY && row.value.trim() === '') dict[key] = null;
    else dict[key] = row.value;
  }
  if (sessionNull && !Object.prototype.hasOwnProperty.call(dict, SESSION_HEADER_KEY)) {
    dict[SESSION_HEADER_KEY] = null;
  }
  return dict;
}

export function serializeApiConfig(model: ApiConfigFormModel): { json: string; object: Record<string, unknown>; model: ApiConfigFormModel } {
  const split = splitApiUrl(model.apiUrl);
  const queryParams = mergeQueryRows(model.queryParams, split.queryFromUrl);
  const next: ApiConfigFormModel = {
    ...model,
    apiUrl: joinApiUrl(split.baseUrl, split.url),
    queryParams,
    headers: ensureSessionRow(model.headers),
  };
  const object: Record<string, unknown> = {};
  for (const key of Object.keys(model.raw)) {
    if (!KEPT_KEYS.has(key)) object[key] = model.raw[key];
  }
  object.BaseUrl = split.baseUrl;
  object.Url = split.url;
  object.Headers = dictFromRows(next.headers, true);
  object.QueryParams = dictFromRows(queryParams, false);
  object.PathParams = dictFromRows(model.pathParams, false);
  object.PostProcessMethodName = model.postProcessMethodName.trim() === '' ? null : model.postProcessMethodName;
  object.ResponseObjectMapToEnvionmentVariable = dictFromRows(model.responseToEnv, false);
  object.ResponseHeaderNeedToSetCookieNames = model.cookieHeaderNames.map((name) => name.trim()).filter((name) => name !== '');
  return { json: JSON.stringify(object, null, 2), object, model: { ...next, raw: object } };
}

export function queryParamValue(model: ApiConfigFormModel, key: string): string {
  const row = model.queryParams.find((item) => item.key === key);
  return row?.value?.trim() ?? '';
}

export function missingPathPlaceholders(model: ApiConfigFormModel): string[] {
  const split = splitApiUrl(model.apiUrl);
  return model.pathParams
    .map((row) => row.key.trim())
    .filter((key) => key && !split.url.includes(`{{PathParams.${key}}}`));
}

/** Add CurrentUserSessionId: null when the header is absent. Keep an existing value. */
export function ensureSessionHeader(config: Record<string, unknown> | null | undefined): Record<string, unknown> {
  const cfg = { ...(config ?? {}) };
  const headers = asObject(cfg.Headers);
  if (!Object.prototype.hasOwnProperty.call(headers, SESSION_HEADER_KEY)) headers[SESSION_HEADER_KEY] = null;
  cfg.Headers = headers;
  return cfg;
}

export function isUsedByMcpValue(value: unknown): boolean {
  return value === true;
}
