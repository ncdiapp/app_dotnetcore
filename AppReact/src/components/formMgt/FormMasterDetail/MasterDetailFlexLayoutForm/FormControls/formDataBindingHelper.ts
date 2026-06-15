/**
 * Resolve sibling unit id: field.TransactionUnitId or layoutItem.GridTransactionUnitId (Angular parity).
 */
function getSiblingUnitId(currentFormData: any, fieldDto: any, layoutItemExDto?: any): number | null | undefined {
  const rootUnitId = currentFormData?.RootUnitId;
  const unitId = fieldDto?.TransactionUnitId ?? layoutItemExDto?.GridTransactionUnitId;
  if (unitId == null || unitId === rootUnitId) return null;
  return unitId;
}

/**
 * Helper for form one-to-one field binding (Angular parity).
 * Root unit fields: currentFormData.DictOneToOneFields[fieldName]
 * Sibling unit fields: currentFormData.DictSiblingOneToOneFields[unitId][fieldName]
 */
export function getOneToOneFieldValue(
  currentFormData: any,
  fieldDto: any,
  fieldName: string,
  defaultValue: string | number | boolean = '',
  layoutItemExDto?: any
): string | number | boolean | null | undefined {
  if (!currentFormData) return defaultValue;
  const rootUnitId = currentFormData.RootUnitId;
  const unitId = getSiblingUnitId(currentFormData, fieldDto, layoutItemExDto);
  if (unitId != null && currentFormData.DictSiblingOneToOneFields?.[unitId]) {
    const v = currentFormData.DictSiblingOneToOneFields[unitId][fieldName];
    return v !== undefined && v !== null ? v : defaultValue;
  }
  const v = currentFormData.DictOneToOneFields?.[fieldName];
  return v !== undefined && v !== null ? v : defaultValue;
}

/**
 * Build updated currentFormData with the given one-to-one field value (root or sibling).
 */
function walkTransactionUnits(units: any[] | null | undefined, visit: (unit: any) => void) {
  (units || []).forEach((unit) => {
    visit(unit);
    walkTransactionUnits(unit.AppTransactionUnitList ?? unit.Children, visit);
  });
}

/** Apply search/form link-target defaults (e.g. FolderId on create from folder navigation). */
export function applyLinkTargetValueMappingToFormData(
  formData: any,
  transactionExDto: any,
  linkTargetValueMapping: Record<string, unknown> | null | undefined,
): any {
  if (!formData || !linkTargetValueMapping || typeof linkTargetValueMapping !== 'object') {
    return formData;
  }

  let next = { ...formData };
  const rootUnitId = next.RootUnitId;

  const findUnitForField = (fieldName: string): number | null | undefined => {
    let matchedUnitId: number | null | undefined;
    walkTransactionUnits(transactionExDto?.AppTransactionUnitList, (unit) => {
      if (matchedUnitId != null) return;
      const fields = unit.AppTransactionFieldList ?? [];
      const hasField = fields.some(
        (f: any) => String(f.DataBaseFieldName ?? '').toLowerCase() === fieldName.toLowerCase(),
      );
      if (hasField) matchedUnitId = unit.Id ?? null;
    });
    return matchedUnitId;
  };

  Object.entries(linkTargetValueMapping).forEach(([fieldName, value]) => {
    if (!fieldName || value == null || value === '') return;
    const unitId = findUnitForField(fieldName);
    if (unitId != null && unitId !== rootUnitId) {
      next = {
        ...next,
        DictSiblingOneToOneFields: {
          ...(next.DictSiblingOneToOneFields ?? {}),
          [unitId]: {
            ...(next.DictSiblingOneToOneFields?.[unitId] ?? {}),
            [fieldName]: value,
          },
        },
      };
      return;
    }
    next = {
      ...next,
      DictOneToOneFields: {
        ...(next.DictOneToOneFields ?? {}),
        [fieldName]: value,
      },
    };
  });

  return { ...next, IsDirty: true };
}

export function buildFormDataWithOneToOneValue(
  currentFormData: any,
  fieldDto: any,
  fieldName: string,
  newValue: any,
  layoutItemExDto?: any
): any {
  if (!currentFormData) return currentFormData;
  const unitId = getSiblingUnitId(currentFormData, fieldDto, layoutItemExDto);
  if (unitId != null) {
    const sibling = currentFormData.DictSiblingOneToOneFields?.[unitId] || {};
    return {
      ...currentFormData,
      DictSiblingOneToOneFields: {
        ...currentFormData.DictSiblingOneToOneFields,
        [unitId]: {
          ...sibling,
          [fieldName]: newValue
        }
      },
      IsDirty: true
    };
  }
  return {
    ...currentFormData,
    DictOneToOneFields: {
      ...currentFormData.DictOneToOneFields,
      [fieldName]: newValue
    },
    IsDirty: true
  };
}
