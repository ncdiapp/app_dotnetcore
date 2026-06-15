const TRANSACTION_FIELD_UI_PREFIX = 'Trans_';

export type TransactionFieldLookup = {
  Id: number;
  TransactionDisplayName: string;
  UnitDisplay: string;
  Display: string;
  ShortDisplay: string;
  ExpressionDisplay: string;
  MessageTemplateDisplay: string;
};

export function buildTransactionFieldLookupsFromHierarchy(transactionData: any): {
  transFieldLookUpList: TransactionFieldLookup[];
  rootLevelTransFieldLookUpList: TransactionFieldLookup[];
  dictChildGridUnitIdAndTransFieldLookUpList: Record<number, TransactionFieldLookup[]>;
  childUnitLookupList: { Id: number; Display: string }[];
} {
  const transFieldLookUpList: TransactionFieldLookup[] = [];
  const rootLevelTransFieldLookUpList: TransactionFieldLookup[] = [];
  const dictChildGridUnitIdAndTransFieldLookUpList: Record<number, TransactionFieldLookup[]> = {};
  const childUnitLookupList: { Id: number; Display: string }[] = [];

  if (!transactionData?.AppTransactionUnitList) {
    return {
      transFieldLookUpList,
      rootLevelTransFieldLookUpList,
      dictChildGridUnitIdAndTransFieldLookUpList,
      childUnitLookupList,
    };
  }

  for (const aUnit of transactionData.AppTransactionUnitList) {
    for (const field of aUnit.AppTransactionFieldList || []) {
      const transactionFieldLookup: TransactionFieldLookup = {
        Id: field.Id,
        TransactionDisplayName: `${transactionData.TransactionName}(#${transactionData.Id})`,
        UnitDisplay: 'Root Level Field',
        Display: '',
        ShortDisplay: '',
        ExpressionDisplay: '',
        MessageTemplateDisplay: '',
      };

      if (aUnit.IsMasterSiblingUnit) {
        transactionFieldLookup.Display = `${TRANSACTION_FIELD_UI_PREFIX}${transactionData.Id}.${aUnit.DataBaseTableName}.${field.DataBaseFieldName}`;
        transactionFieldLookup.ShortDisplay = `${aUnit.DataBaseTableName}.${field.DataBaseFieldName}`;
      } else {
        transactionFieldLookup.Display = `${TRANSACTION_FIELD_UI_PREFIX}${transactionData.Id}.${field.DataBaseFieldName}`;
        transactionFieldLookup.ShortDisplay = field.DataBaseFieldName;
      }
      transactionFieldLookup.ExpressionDisplay = `[${transactionFieldLookup.Display}]`;
      transactionFieldLookup.Display = transactionFieldLookup.ExpressionDisplay;
      transactionFieldLookup.MessageTemplateDisplay = `[TF_${transactionFieldLookup.Id}_${field.DataBaseFieldName}]`;
      transFieldLookUpList.push(transactionFieldLookup);
      rootLevelTransFieldLookUpList.push(transactionFieldLookup);
    }

    for (const aChildUnit of aUnit.Children || []) {
      childUnitLookupList.push({
        Id: aChildUnit.Id,
        Display: `${transactionData.TransactionName}(${transactionData.Id}).${aChildUnit.DataBaseTableName}(${aChildUnit.Id})`,
      });
      dictChildGridUnitIdAndTransFieldLookUpList[aChildUnit.Id] = [];

      for (const field of aChildUnit.AppTransactionFieldList || []) {
        const transactionFieldLookup: TransactionFieldLookup = {
          Id: field.Id,
          TransactionDisplayName: `${transactionData.TransactionName}(#${transactionData.Id})`,
          UnitDisplay: `Child Grid: ${aChildUnit.DataBaseTableName}(${aChildUnit.Id})`,
          Display: `${TRANSACTION_FIELD_UI_PREFIX}${transactionData.Id}.${aChildUnit.DataBaseTableName}.${field.DataBaseFieldName}`,
          ShortDisplay: `Grid:${aUnit.DataBaseTableName}.${field.DataBaseFieldName}`,
          ExpressionDisplay: '',
          MessageTemplateDisplay: '',
        };
        transactionFieldLookup.ExpressionDisplay = `[${transactionFieldLookup.Display}]`;
        transactionFieldLookup.Display = transactionFieldLookup.ExpressionDisplay;
        transactionFieldLookup.MessageTemplateDisplay = `[TF_${transactionFieldLookup.Id}_${field.DataBaseFieldName}]`;
        transFieldLookUpList.push(transactionFieldLookup);
        dictChildGridUnitIdAndTransFieldLookUpList[aChildUnit.Id].push(transactionFieldLookup);
      }
    }
  }

  return {
    transFieldLookUpList,
    rootLevelTransFieldLookUpList,
    dictChildGridUnitIdAndTransFieldLookUpList,
    childUnitLookupList,
  };
}
