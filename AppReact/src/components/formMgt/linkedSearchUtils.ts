/**
 * Linked search / master-data picker helpers (transaction unit linked search + link targets).
 */

/** True when server marks this linked search as single-row update (camelCase + bool-ish). */
export function isTransactionLinkedSearchSingleRow(ls: any): boolean {
  if (ls == null) return false;
  const v = ls.IsSingleSelectedRow ?? ls.isSingleSelectedRow;
  if (v === true || v === 1 || v === '1') return true;
  if (typeof v === 'string' && v.toLowerCase() === 'true') return true;
  return false;
}

/** EmAppLinkedSearchAction.AddFormGridRow */
export function isLinkedSearchAddGridRows(ls: any): boolean {
  return Number(ls?.Action) === 1;
}

/** EmAppLinkedSearchAction.UpdateFormData (grid: update single row; root: update form fields from search). */
export function isLinkedSearchUpdateFormData(ls: any): boolean {
  return Number(ls?.Action) === 2;
}

/**
 * Show Confirm & Close on embedded linked-search popup.
 * Angular: Action 1 = add rows, Action 2 = update from selected search row(s); also honor explicit single-row flag.
 */
export function isLinkedSearchPopupConfirmClose(ls: any): boolean {
  if (ls == null) return false;
  const a = Number(ls.Action);
  if (a === 1 || a === 2) return true;
  return isTransactionLinkedSearchSingleRow(ls);
}

/**
 * Grid linked search: use radio / single selection (not multi checkboxes).
 * Action 1 (add many rows) always multi-select; Action 2 or explicit single-row flag → single.
 */
export function isLinkedSearchGridSingleSelection(ls: any): boolean {
  if (ls == null) return false;
  if (isLinkedSearchAddGridRows(ls)) return false;
  return isLinkedSearchUpdateFormData(ls) || isTransactionLinkedSearchSingleRow(ls);
}

/**
 * Confirm handler: apply selected search row into host (not add-rows).
 * Action 3 (view only) must not run update.
 */
export function shouldLinkedSearchApplyUpdateHostRow(ls: any): boolean {
  if (ls == null) return false;
  const a = Number(ls.Action);
  if (a === 1 || a === 3) return false;
  return isLinkedSearchUpdateFormData(ls) || isTransactionLinkedSearchSingleRow(ls);
}

export type MasterDataPickerContext = { linkTarget: any; hostRow: any };

/**
 * Apply link-target search row → host row using SourceViewColumnId* → TargetColumn* (transaction field names).
 */
export function applyLinkTargetMasterDataSelectionToRow(linkTarget: any, selectedResult: any, hostRow: any): void {
  if (!linkTarget || !selectedResult || !hostRow) return;
  const dict = selectedResult?.DictViewColumnIDKeyValue ?? {};
  hostRow.DictOneToOneFields = { ...(hostRow.DictOneToOneFields ?? {}) };
  const pairs: [any, any][] = [
    [linkTarget.SourceViewColumnId1, linkTarget.TargetColumn1],
    [linkTarget.SourceViewColumnId2, linkTarget.TargetColumn2],
    [linkTarget.SourceViewColumnId3, linkTarget.TargetColumn3],
  ];
  for (const [srcId, tgtCol] of pairs) {
    if (srcId == null || tgtCol == null || String(tgtCol).trim() === '') continue;
    const sk = String(srcId);
    const val = dict[sk] ?? dict[Number(sk)];
    if (val !== undefined) hostRow.DictOneToOneFields[String(tgtCol)] = val;
  }
  hostRow.IsDirty = true;
}
