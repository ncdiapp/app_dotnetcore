/** Shared field lookup builder — Angular transactionCommandSingleEditorPopupCtrl.prepareTransactionData */

export type CommandFieldLookupResult = {
  rootLevelTransFieldLookUpList: any[];
  rootLevelConditionTransFieldLookUpList: any[];
  rootLevelSwitchConditionTransFieldLookUpList: any[];
  transactionFieldLookUpList: any[];
  rootLevelAllFieldLookUpList: any[];
  globalTransFieldLookUpList: any[];
};

export function buildCommandFieldLookups(
  transactionData: any,
  controlTypes: { checkbox: number; ddl: number },
  rootWorkflowTransactionData?: any | null,
): CommandFieldLookupResult {
  const rootLevelList: any[] = [];
  const rootLevelConditionList: any[] = [];
  const rootLevelSwitchConditionList: any[] = [];
  const allTransactionFields: any[] = [];
  const transId = transactionData?.Id;

  const buildFormulaDisplayName = (field: any, aUnit: any, opts: { isChildGrid: boolean }) => {
    if (field?.FormulaDisplayName) return String(field.FormulaDisplayName);
    const isTemp = !!field?.IsTempVariable;
    const dbField = field?.DataBaseFieldName || field?.DisplayName || `Field_${field?.Id ?? ''}`;
    const unitTable = aUnit?.DataBaseTableName || aUnit?.UnitDisplayName || '';
    if (opts.isChildGrid || aUnit?.IsMasterSiblingUnit) {
      const prefix = unitTable ? `${unitTable}.` : '';
      return '[' + prefix + dbField + (isTemp ? ']' : '_' + String(field?.Id ?? '') + ']');
    }
    return '[' + dbField + (isTemp ? ']' : '_' + String(field?.Id ?? '') + ']');
  };

  const pushLookup = (field: any, aUnit: any, opts: { isChildGrid: boolean; parentUnit?: any }) => {
    const { isChildGrid, parentUnit } = opts;
    let pathDisplay: string;
    let shortDisplay: string;
    if (isChildGrid) {
      pathDisplay = `Trans_${transId}.${aUnit.DataBaseTableName || aUnit.UnitDisplayName}.${field.DataBaseFieldName || field.DisplayName}`;
      shortDisplay = `Grid:${parentUnit?.DataBaseTableName || ''}.${field.DataBaseFieldName || field.DisplayName}`;
    } else if (aUnit?.IsMasterSiblingUnit) {
      pathDisplay = `Trans_${transId}.${aUnit.DataBaseTableName || aUnit.UnitDisplayName}.${field.DataBaseFieldName || field.DisplayName}`;
      shortDisplay = `${aUnit.DataBaseTableName || aUnit.UnitDisplayName}.${field.DataBaseFieldName || field.DisplayName}`;
    } else {
      pathDisplay = `Trans_${transId}.${field.DataBaseFieldName || field.DisplayName}`;
      shortDisplay = field.DataBaseFieldName || field.DisplayName || String(field.Id);
    }
    const dbField = field.DataBaseFieldName || field.DisplayName || `Field_${field.Id ?? ''}`;
    const item = {
      Id: field.Id,
      Display: `[${pathDisplay}]`,
      ShortDisplay: shortDisplay,
      FormulaDisplayName: buildFormulaDisplayName(field, aUnit, { isChildGrid }),
      MessageTemplateDisplay: `[TF_${field.Id}_${dbField}]`,
      ControlType: field.ControlType,
      EntityId: field.EntityId ?? null,
      UnitId: aUnit.Id,
    };
    allTransactionFields.push(item);
    if (!isChildGrid) {
      rootLevelList.push(item);
      if (Number(field.ControlType) === controlTypes.checkbox) {
        rootLevelConditionList.push(item);
      }
      if (Number(field.ControlType) === controlTypes.ddl) {
        rootLevelSwitchConditionList.push(item);
      }
    }
  };

  (transactionData?.AppTransactionUnitList || []).forEach((aUnit: any) => {
    (aUnit.AppTransactionFieldList || []).forEach((field: any) => pushLookup(field, aUnit, { isChildGrid: false }));
    (aUnit.Children || []).forEach((childUnit: any) => {
      (childUnit.AppTransactionFieldList || []).forEach((field: any) =>
        pushLookup(field, childUnit, { isChildGrid: true, parentUnit: aUnit }),
      );
    });
  });

  const globalTransFieldLookUpList = buildGlobalTransFieldLookUpList(rootWorkflowTransactionData);

  return {
    rootLevelTransFieldLookUpList: rootLevelList,
    rootLevelConditionTransFieldLookUpList: rootLevelConditionList,
    rootLevelSwitchConditionTransFieldLookUpList: rootLevelSwitchConditionList,
    transactionFieldLookUpList: allTransactionFields,
    rootLevelAllFieldLookUpList: rootLevelList,
    globalTransFieldLookUpList,
  };
}

/** Angular prepareRootWorkflowTransactionData — workflow parameter (global) fields. */
export function buildGlobalTransFieldLookUpList(rootWorkflowTransactionData: any | null | undefined): any[] {
  if (!rootWorkflowTransactionData?.AppTransactionUnitList?.[0]) return [];
  const transactionData = rootWorkflowTransactionData;
  const aUnit = transactionData.AppTransactionUnitList[0];
  const transId = transactionData.Id;
  const list: any[] = [];

  (aUnit.AppTransactionFieldList || []).forEach((field: any) => {
    const dbField = field.DataBaseFieldName || field.DisplayName || `Field_${field.Id ?? ''}`;
    let shortDisplay: string;
    let pathDisplay: string;
    if (aUnit.IsMasterSiblingUnit) {
      pathDisplay = `GlobalTrans_${transId}.${aUnit.DataBaseTableName}.${dbField}`;
      shortDisplay = `Global Field: ${aUnit.DataBaseTableName}.${dbField}`;
    } else {
      pathDisplay = `GlobalTrans_${transId}.${dbField}`;
      shortDisplay = `Global Field: ${dbField}`;
    }
    list.push({
      Id: field.Id,
      Display: `[GlobalTF_${pathDisplay}]`,
      ShortDisplay: shortDisplay,
      MessageTemplateDisplay: `[GlobalTF_${field.Id}_${dbField}]`,
      ControlType: field.ControlType,
    });
  });

  return list;
}
