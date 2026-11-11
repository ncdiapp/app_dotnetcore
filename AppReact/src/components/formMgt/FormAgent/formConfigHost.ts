export const FORM_CONFIG_SKILL_KEY = 'app-configurator';

/** Slim unit list for HostContext (ids/names/tables only — full fields via tools). */
function slimUnits(tx: any): any[] {
  const unitsRaw: any[] = tx.AppTransactionUnitList || tx.AppTransactionUnitTree || [];
  const units: any[] = [];
  const walk = (unit: any) => {
    if (!unit) return;
    units.push({
      unitId: unit.Id ?? unit.id,
      name: unit.UnitDisplayName ?? unit.unitDisplayName ?? unit.DataBaseTableName,
      table: unit.DataBaseTableName ?? unit.dataBaseTableName,
      fieldCount: (unit.AppTransactionFieldList || []).length,
    });
    const kids = [...(unit.Children || []), ...(unit.AppTransactionUnitList || [])];
    kids.forEach(walk);
  };
  unitsRaw.forEach(walk);
  return units;
}

/** Single-form HostContext (no FormData). Prefer slim units — use find_fields / get_* for details. */
export function buildFormConfigContext(args: {
  mode: 'master' | 'list';
  transaction: any;
  formRole?: string;
}): unknown {
  const tx = args.transaction || {};
  const commands = (tx.CommandActionList || []).map((c: any) => ({
    id: c.Id ?? c.id,
    name: c.Name ?? c.DisplayName ?? c.name,
  }));

  return {
    surface: 'form',
    mode: 'configurator',
    multiTransaction: false,
    formKind: args.mode === 'list' ? 'list' : 'master',
    formRole: args.formRole,
    transactionId: tx.TransactionId ?? tx.Id ?? tx.transactionId,
    transactionName: tx.TransactionName ?? tx.transactionName,
    saasApplicationId: tx.SaasApplicationId ?? tx.saasApplicationId,
    units: slimUnits(tx),
    commands,
  };
}

/**
 * Template / form-group with multiple open transactions.
 * HostContext only needs ids/names; find_fields loads metadata from DB and groups by transaction → unit.
 */
export function buildFormConfigMultiContext(
  forms: Array<{
    formRole?: string;
    formName?: string;
    transactionId: number | string;
    transactionName?: string;
  }>,
): unknown {
  const list = (forms || [])
    .map((f) => {
      const transactionId = f.transactionId;
      if (transactionId == null || String(transactionId).trim() === '') return null;
      return {
        formRole: f.formRole,
        formName: f.formName ?? f.transactionName,
        transactionId: Number(transactionId),
        transactionName: f.transactionName ?? f.formName,
      };
    })
    .filter(Boolean) as Array<{
      formRole?: string;
      formName?: string;
      transactionId: number;
      transactionName?: string;
    }>;

  return {
    surface: 'form',
    mode: 'configurator',
    multiTransaction: list.length > 1,
    forms: list,
    transactionId: list.length === 1 ? list[0].transactionId : undefined,
  };
}
