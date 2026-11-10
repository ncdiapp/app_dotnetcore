export const FORM_CONFIG_SKILL_KEY = 'app-configurator';

/** Config catalog for HostContextJson (no FormData values). */
export function buildFormConfigContext(args: {
  mode: 'master' | 'list';
  transaction: any;
}): unknown {
  const tx = args.transaction || {};
  const unitsRaw: any[] = tx.AppTransactionUnitList || tx.AppTransactionUnitTree || [];
  const units: any[] = [];

  const walk = (unit: any) => {
    if (!unit) return;
    const fields = (unit.AppTransactionFieldList || []).map((f: any) => ({
      fieldId: f.Id ?? f.id,
      name: f.DisplayName ?? f.displayName,
      dbName: f.DataBaseFieldName ?? f.DatabaseFieldName ?? f.dbName,
      controlType: f.ControlType ?? f.controlType,
      entityId: f.EntityId ?? f.entityId,
      isVisible: f.IsVisible ?? f.isVisible,
      isReadonly: f.IsReadonly ?? f.isReadonly,
      isAllowEmpty: f.IsAllowEmpty ?? f.isAllowEmpty,
    })).filter((f: any) => f.fieldId != null || f.dbName);
    units.push({
      unitId: unit.Id ?? unit.id,
      name: unit.UnitDisplayName ?? unit.unitDisplayName ?? unit.DataBaseTableName,
      table: unit.DataBaseTableName ?? unit.dataBaseTableName,
      fields,
    });
    const kids = [...(unit.Children || []), ...(unit.AppTransactionUnitList || [])];
    kids.forEach(walk);
  };
  unitsRaw.forEach(walk);

  const commands = (tx.CommandActionList || []).map((c: any) => ({
    id: c.Id ?? c.id,
    name: c.Name ?? c.DisplayName ?? c.name,
  }));

  return {
    surface: 'form',
    mode: 'configurator',
    formKind: args.mode === 'list' ? 'list' : 'master',
    transactionId: tx.TransactionId ?? tx.Id ?? tx.transactionId,
    transactionName: tx.TransactionName ?? tx.transactionName,
    saasApplicationId: tx.SaasApplicationId ?? tx.saasApplicationId,
    units,
    commands,
  };
}
