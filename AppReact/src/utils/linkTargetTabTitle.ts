/**
 * Read a search row cell from `DictViewColumnIDKeyValue` when the API uses string vs number keys.
 */
export function getDictViewColumnValue(
  dict: Record<string | number, unknown> | null | undefined,
  columnId: string | number | null | undefined
): string | undefined {
  if (dict == null || columnId === null || columnId === undefined) return undefined;
  const tryKeys: (string | number)[] = [columnId, String(columnId)];
  const n = Number(columnId);
  if (!Number.isNaN(n)) tryKeys.push(n);
  for (const k of tryKeys) {
    const v = (dict as Record<string | number, unknown>)[k];
    if (v !== undefined && v !== null && String(v).trim() !== '') {
      return String(v);
    }
  }
  return undefined;
}

/**
 * Angular parity: "Link to data model" tab titles use the row display column when
 * configured and non-empty; otherwise the root primary key (same value passed as param1).
 */
export function buildLinkTargetTabTitle(
  navigationActionName: string | null | undefined,
  rowDisplayIsConfigured: boolean,
  rowDisplayValue: string | number | null | undefined,
  rootPrimaryKeyValue: string | number | null | undefined
): string {
  const base = (navigationActionName && String(navigationActionName).trim()) || 'Open';
  const pk =
    rootPrimaryKeyValue !== null && rootPrimaryKeyValue !== undefined && String(rootPrimaryKeyValue).trim() !== ''
      ? String(rootPrimaryKeyValue)
      : null;

  let suffix: string | null = null;
  if (rowDisplayIsConfigured) {
    if (rowDisplayValue != null && String(rowDisplayValue).trim() !== '') {
      suffix = String(rowDisplayValue);
    } else if (pk) {
      suffix = pk;
    }
  } else if (pk) {
    suffix = pk;
  }

  return suffix ? `${base}: ${suffix}` : base;
}

/** Angular parity: order link targets / linked searches by `Sort` ascending for runtime buttons. */
export function sortBySortOrder<T extends { Sort?: number | null }>(items: T[] | null | undefined): T[] {
  if (!Array.isArray(items) || items.length === 0) return [];
  return [...items].sort((a, b) => (Number(a?.Sort) || 0) - (Number(b?.Sort) || 0));
}

/**
 * Resolve a link-target SourceColumn value.
 * - `RootUnit.Field` → root DictOneToOneFields
 * - `RootSibling.{unitId}.Field` → DictSiblingOneToOneFields[unitId]
 * - plain Field → row dict, else root dict
 */
export function resolveLinkTargetSourceColumnValue(
  sourceCol: string,
  opts: {
    rowDict?: Record<string, any> | null;
    rootDict?: Record<string, any> | null;
    siblingDict?: Record<string, any> | null;
  }
): any {
  const sourceColumnStr = String(sourceCol || '');
  if (!sourceColumnStr) return undefined;

  if (sourceColumnStr.indexOf('RootUnit.') === 0) {
    return opts.rootDict?.[sourceColumnStr.substring(9).trim()];
  }

  if (sourceColumnStr.indexOf('RootSibling.') === 0) {
    const rest = sourceColumnStr.substring('RootSibling.'.length);
    const dot = rest.indexOf('.');
    if (dot <= 0) return undefined;
    const unitIdKey = rest.substring(0, dot).trim();
    const fieldName = rest.substring(dot + 1).trim();
    if (!unitIdKey || !fieldName) return undefined;
    const sib = opts.siblingDict;
    if (!sib || typeof sib !== 'object') return undefined;
    const unitBag =
      sib[unitIdKey] ??
      sib[Number(unitIdKey) as any] ??
      Object.entries(sib).find(([k]) => String(k) === String(unitIdKey))?.[1];
    return unitBag?.[fieldName];
  }

  return opts.rowDict?.[sourceColumnStr] ?? opts.rootDict?.[sourceColumnStr];
}

/**
 * Build linkTargetValueMapping from Source/Target Field 1–3.
 * Field 1 was historically only used as Edit PK (param1); Create "Using Current Data" still
 * configures Field 1 in the editor, so include it when both Source and Target are set
 * (Search View Create already maps column 1).
 * Supports RootUnit.* and RootSibling.{unitId}.* source prefixes.
 */
export function buildLinkTargetValueMapping(opts: {
  linkTarget: any;
  /** Selected child/list row DictOneToOneFields (preferred for plain field sources). */
  rowDict?: Record<string, any> | null;
  /** Root form DictOneToOneFields (used for RootUnit.* sources and root-unit menus). */
  rootDict?: Record<string, any> | null;
  /** Root sibling 1:1 units: DictSiblingOneToOneFields[unitId][field]. */
  siblingDict?: Record<string, any> | null;
}): Record<string, any> {
  const { linkTarget, rowDict, rootDict, siblingDict } = opts;
  const mapping: Record<string, any> = {};
  if (!linkTarget) return mapping;

  const pairs: Array<[unknown, unknown]> = [
    [linkTarget.SourceColumn1, linkTarget.TargetColumn1],
    [linkTarget.SourceColumn2, linkTarget.TargetColumn2],
    [linkTarget.SourceColumn3, linkTarget.TargetColumn3],
  ];

  for (const [sourceCol, targetCol] of pairs) {
    if (!sourceCol || !targetCol) continue;
    mapping[String(targetCol)] = resolveLinkTargetSourceColumnValue(String(sourceCol), {
      rowDict,
      rootDict,
      siblingDict,
    });
  }

  return mapping;
}
