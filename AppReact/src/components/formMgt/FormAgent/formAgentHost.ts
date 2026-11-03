import { createContext, type MutableRefObject } from 'react';

export const FORM_AGENT_SKILL_KEY = 'app-transaction-form-agent';
export const LIST_COMMUNICATION_ROOT = '__list__';

export type FormAgentHostSlots = {
  getContext?: () => unknown;
  setFields?: (args: any) => Promise<unknown>;
  updateGrid?: (args: any) => Promise<unknown>;
  openLinkedSearch?: (args: any) => Promise<unknown>;
  openLinkTarget?: (args: any) => Promise<unknown>;
  runCommand?: (args: any) => Promise<unknown>;
  describeAction?: (args: any) => unknown;
};

export const FormAgentHostContext = createContext<MutableRefObject<FormAgentHostSlots> | null>(null);

const MAX_ROWS = 20;

function fieldSummary(field: any) {
  return {
    fieldId: field?.Id ?? field?.TransactionFieldId,
    dbName: field?.DataBaseFieldName ?? field?.FieldName,
    label: field?.DisplayName ?? field?.FieldName ?? field?.longDisplay,
    type: field?.ControlType ?? field?.FieldType,
  };
}

function valuesFor(fields: any[], dict: Record<string, any> | undefined) {
  return (fields || []).slice(0, 80).map((field) => {
    const db = field?.DataBaseFieldName ?? field?.FieldName;
    return { ...fieldSummary(field), value: db ? dict?.[db] : undefined };
  });
}

function actionSummary(kind: string, item: any, unitId: any) {
  return {
    kind,
    id: item?.Id,
    name: item?.Name ?? item?.NavigationActionName ?? item?.CommandName ?? item?.DisplayName,
    unitId,
    action: item?.Action ?? item?.ActionType,
  };
}

export function buildFormAgentContext(args: {
  mode: 'master' | 'list';
  transaction: any;
  formData: any;
  openedFrom?: unknown;
}): unknown {
  const tx = args.transaction || {};
  const form = args.formData || {};
  const units: any[] = tx.AppTransactionUnitList || tx.AppTransactionUnitTree || [];
  const root = units[0];
  const rootFields = root?.AppTransactionFieldList || [];
  const actions: any[] = [];
  const walk = (unit: any) => {
    if (!unit) return;
    const unitId = unit.Id ?? unit.TransactionUnitId;
    for (const ls of unit.AppTransactionUnitLinkedSearchList || []) actions.push(actionSummary('linkedSearch', ls, unitId));
    for (const lt of unit.AppFormLinkTargetList || []) actions.push(actionSummary('linkTarget', lt, unitId));
    (unit.Children || []).forEach(walk);
  };
  units.forEach(walk);
  for (const cmd of tx.CommandActionList || []) actions.push(actionSummary('command', cmd, root?.Id));

  if (args.mode === 'list') {
    const rows = Array.isArray(form.ListData) ? form.ListData : [];
    return {
      mode: 'list',
      transactionId: tx.Id ?? tx.TransactionId ?? form.TransactionId,
      name: tx.TransactionName,
      isDirty: Boolean(form.IsDirty),
      openedFrom: args.openedFrom ?? null,
      columns: rootFields.slice(0, 80).map(fieldSummary),
      rowCount: rows.length,
      rowsTruncated: rows.length > MAX_ROWS,
      rows: rows.slice(0, MAX_ROWS).map((row: any, index: number) => ({
        rowKey: String(row?.UIId ?? row?.Id ?? index),
        values: row?.DictOneToOneFields ?? {},
      })),
      actions,
    };
  }

  const grids = Object.entries(form.DictOneToManyFields || {}).map(([unitId, rows]) => {
    const list = Array.isArray(rows) ? rows : [];
    return {
      unitId,
      rowCount: list.length,
      rowsTruncated: list.length > MAX_ROWS,
      rows: list.slice(0, MAX_ROWS).map((row: any, index: number) => ({
        rowKey: String(row?.UIId ?? row?.Id ?? index),
        values: row?.DictOneToOneFields ?? {},
      })),
    };
  });

  return {
    mode: 'master',
    transactionId: tx.Id ?? tx.TransactionId,
    name: tx.TransactionName,
    rootPk: form.RootPrimaryKeyValue ?? null,
    isNew: Boolean(form.IsNew),
    isDirty: Boolean(form.IsDirty),
    openedFrom: args.openedFrom ?? null,
    fields: valuesFor(rootFields, form.DictOneToOneFields),
    grids,
    actions,
  };
}

function parseFields(raw: unknown): Record<string, unknown> {
  if (!raw) return {};
  if (typeof raw === 'object') return raw as Record<string, unknown>;
  try {
    const parsed = JSON.parse(String(raw));
    return parsed && typeof parsed === 'object' ? parsed : {};
  } catch {
    return {};
  }
}

export function applySetFields(formData: any, args: any, mode: 'master' | 'list'): any {
  const patch = parseFields(args?.fieldsJson);
  if (mode === 'list') {
    const rows = Array.isArray(formData?.ListData) ? [...formData.ListData] : [];
    const key = args?.rowKey != null ? String(args.rowKey) : null;
    const index = key
      ? rows.findIndex((row: any, i: number) => String(row?.UIId ?? row?.Id ?? i) === key)
      : 0;
    if (index < 0 || !rows[index]) return formData;
    rows[index] = {
      ...rows[index],
      IsDirty: true,
      DictOneToOneFields: { ...(rows[index].DictOneToOneFields || {}), ...patch },
    };
    return { ...formData, IsDirty: true, ListData: rows };
  }
  return {
    ...formData,
    IsDirty: true,
    DictOneToOneFields: { ...(formData?.DictOneToOneFields || {}), ...patch },
  };
}

export function applyUpdateGrid(formData: any, args: any, mode: 'master' | 'list'): any {
  const op = String(args?.op || 'add').toLowerCase();
  const patch = parseFields(args?.fieldsJson);
  const key = args?.rowKey != null ? String(args.rowKey) : '';
  if (mode === 'list') {
    let rows = Array.isArray(formData?.ListData) ? [...formData.ListData] : [];
    if (op === 'add') {
      rows = [...rows, { UIId: `ai-${Date.now()}`, IsNew: true, IsDirty: true, DictOneToOneFields: patch, DictOneToManyFields: {} }];
    } else {
      rows = rows.flatMap((row: any, i: number) => {
        const rowKey = String(row?.UIId ?? row?.Id ?? i);
        if (rowKey !== key) return [row];
        if (op === 'delete') return [];
        return [{ ...row, IsDirty: true, DictOneToOneFields: { ...(row.DictOneToOneFields || {}), ...patch } }];
      });
    }
    return { ...formData, IsDirty: true, ListData: rows };
  }
  const unitId = String(args?.unitId || '');
  const dict = { ...(formData?.DictOneToManyFields || {}) };
  let rows = Array.isArray(dict[unitId]) ? [...dict[unitId]] : [];
  if (op === 'add') {
    rows = [...rows, { UIId: `ai-${Date.now()}`, IsNew: true, IsDirty: true, DictOneToOneFields: patch, DictOneToManyFields: {} }];
  } else {
    rows = rows.flatMap((row: any, i: number) => {
      const rowKey = String(row?.UIId ?? row?.Id ?? i);
      if (rowKey !== key) return [row];
      if (op === 'delete') return [];
      return [{ ...row, IsDirty: true, DictOneToOneFields: { ...(row.DictOneToOneFields || {}), ...patch } }];
    });
  }
  dict[unitId] = rows;
  return { ...formData, IsDirty: true, DictOneToManyFields: dict };
}

export function describeFormAction(transaction: any, args: any): unknown {
  const kind = String(args?.kind || '');
  const id = String(args?.id || '');
  const units: any[] = transaction?.AppTransactionUnitList || [];
  const found: any[] = [];
  const walk = (unit: any) => {
    if (!unit) return;
    const lists: Array<[string, any[]]> = [
      ['linkedSearch', unit.AppTransactionUnitLinkedSearchList || []],
      ['linkTarget', unit.AppFormLinkTargetList || []],
    ];
    for (const [listKind, list] of lists) {
      if (kind && listKind !== kind) continue;
      for (const item of list) {
        if (String(item?.Id) === id) found.push({ kind: listKind, unitId: unit.Id, item });
      }
    }
    (unit.Children || []).forEach(walk);
  };
  units.forEach(walk);
  if (!kind || kind === 'command') {
    for (const cmd of transaction?.CommandActionList || []) {
      if (String(cmd?.Id) === id) found.push({ kind: 'command', item: cmd });
    }
  }
  if (found.length === 0) return { ok: false, error: 'Action not found on this form.' };
  return { ok: true, action: found[0] };
}
