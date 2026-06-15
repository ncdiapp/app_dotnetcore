/**
 * Normalize integration setting parameter payload so backend accepts it.
 * Backend fails when APIConfigParameters / ApiconfigParameters (string) have null for
 * ResponseObjectMapToEnvionmentVariable or ResponseHeaderNeedToSetCookieNames (expects {} and []).
 * JSON strings are pretty-printed (2-space indent) per JsonEditingStandards.
 */

/** Format any JSON value for display in UI (2-space indent + newlines). */
export function prettyPrintJsonForDisplay(value: unknown): string {
  if (value == null) return '';
  if (typeof value === 'string') {
    const s = value.trim();
    if (!s) return value;
    try {
      return JSON.stringify(JSON.parse(s), null, 2);
    } catch {
      return value;
    }
  }
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

const DEFAULT_APICONFIG_OBJECT_KEYS = {
  ResponseObjectMapToEnvionmentVariable: {} as Record<string, unknown>,
  ResponseHeaderNeedToSetCookieNames: [] as string[],
};

/**
 * Normalize payload for SaveAppIntergrationSettingParameterExDto.
 * Ensures APIConfigParameters object and ApiconfigParameters string never have null for the two keys above.
 */
export function normalizeIntegrationSettingParameterForSave(data: any): any {
  if (data == null || typeof data !== 'object') return data;
  const out = { ...data };

  if (out.APIConfigParameters != null && typeof out.APIConfigParameters === 'object') {
    out.APIConfigParameters = {
      ...out.APIConfigParameters,
      ...DEFAULT_APICONFIG_OBJECT_KEYS,
      ResponseObjectMapToEnvionmentVariable:
        out.APIConfigParameters.ResponseObjectMapToEnvionmentVariable ?? DEFAULT_APICONFIG_OBJECT_KEYS.ResponseObjectMapToEnvionmentVariable,
      ResponseHeaderNeedToSetCookieNames:
        out.APIConfigParameters.ResponseHeaderNeedToSetCookieNames ?? DEFAULT_APICONFIG_OBJECT_KEYS.ResponseHeaderNeedToSetCookieNames,
    };
  }

  if (out.ApiconfigParameters != null && typeof out.ApiconfigParameters === 'string') {
    try {
      const obj = JSON.parse(out.ApiconfigParameters);
      if (obj && typeof obj === 'object') {
        const normalized = {
          ...obj,
          ResponseObjectMapToEnvionmentVariable: obj.ResponseObjectMapToEnvionmentVariable ?? {},
          ResponseHeaderNeedToSetCookieNames: obj.ResponseHeaderNeedToSetCookieNames ?? [],
        };
        out.ApiconfigParameters = JSON.stringify(normalized, null, 2);
      }
    } catch {
      /* leave as-is */
    }
  }

  if (out.JsonSampleData != null && typeof out.JsonSampleData === 'string' && out.JsonSampleData.trim()) {
    try {
      out.JsonSampleData = JSON.stringify(JSON.parse(out.JsonSampleData), null, 2);
    } catch {
      /* leave as-is */
    }
  }

  return out;
}
