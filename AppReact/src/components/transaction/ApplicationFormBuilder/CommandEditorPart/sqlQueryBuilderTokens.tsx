/** Angular: SQLExpressionBuiltInTokenList */
export const SQL_EXPRESSION_BUILTIN_TOKENS = [
  '[CurrentUserId]',
  '[CurrentPartnerId]',
  '[CurrentUserName]',
  'GETDATE()',
  'GETUTCDATE()',
];

export function resolveTransactionFieldToken(field: any): string {
  const token = field?.MessageTemplateDisplay;
  if (token) return String(token);
  const id = field?.Id;
  const dbField = field?.DataBaseFieldName || field?.DisplayName || `Field_${id ?? ''}`;
  if (id != null) return `[TF_${id}_${dbField}]`;
  return String(field?.Display ?? field?.ShortDisplay ?? '');
}

/** Angular: dictChildGridUnitIdAndTransFieldLookUpList grouped by child unit. */
export function buildChildGridFieldGroups(
  hierarchy: any,
  transactionFieldLookUpList: any[],
): Array<{ unitName: string; fields: any[] }> {
  const byUnitId = new Map<number, any[]>();
  (transactionFieldLookUpList || []).forEach((f: any) => {
    const short = String(f?.ShortDisplay ?? '');
    if (!short.startsWith('Grid:')) return;
    const unitId = f?.UnitId;
    if (unitId == null) return;
    const list = byUnitId.get(unitId) ?? [];
    list.push(f);
    byUnitId.set(unitId, list);
  });

  const groups: Array<{ unitName: string; fields: any[] }> = [];
  (hierarchy?.AppTransactionUnitList || []).forEach((unit: any) => {
    (unit.Children || []).forEach((childUnit: any) => {
      const fields = byUnitId.get(childUnit.Id);
      if (!fields?.length) return;
      fields.sort((a, b) => String(a?.ShortDisplay ?? '').localeCompare(String(b?.ShortDisplay ?? '')));
      groups.push({
        unitName: childUnit.UnitDisplayName || childUnit.DataBaseTableName || 'Grid',
        fields,
      });
    });
  });
  return groups;
}

export function insertTextIntoMonacoEditor(editor: any, text: string): boolean {
  if (!editor?.executeEdits) return false;
  try {
    let range = editor.getSelection?.();
    if (!range && editor.getPosition) {
      const pos = editor.getPosition();
      if (pos) {
        range = {
          startLineNumber: pos.lineNumber,
          startColumn: pos.column,
          endLineNumber: pos.lineNumber,
          endColumn: pos.column,
        };
      }
    }
    if (!range) return false;
    const op = { identifier: { major: 1, minor: 1 }, range, text, forceMoveMarkers: true };
    editor.executeEdits('sql-token', [op]);
    return true;
  } catch {
    return false;
  }
}
