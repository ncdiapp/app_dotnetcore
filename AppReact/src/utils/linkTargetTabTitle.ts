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
