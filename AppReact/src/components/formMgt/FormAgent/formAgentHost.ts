import { createContext, type MutableRefObject } from 'react';
import { buildFormDataWithOneToOneValue } from '../FormMasterDetail/MasterDetailFlexLayoutForm/FormControls/formDataBindingHelper';

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

function pick(obj: any, ...keys: string[]) {
  for (const key of keys) {
    if (obj?.[key] != null && obj[key] !== '') return obj[key];
  }
  return undefined;
}

function unitIdOf(unit: any) {
  return unit?.Id ?? unit?.TransactionUnitId;
}

function fieldDbName(field: any): string {
  return String(pick(field, 'DataBaseFieldName', 'dataBaseFieldName', 'FieldName', 'fieldName') ?? '');
}

function fieldLabel(field: any): string {
  return String(pick(field, 'DisplayName', 'displayName', 'LabelDisplayBinding', 'FieldName', 'fieldName', 'longDisplay') ?? '');
}

function fieldIdOf(field: any) {
  return pick(field, 'Id', 'id', 'TransactionFieldId', 'transactionFieldId');
}

function fieldUnitId(field: any, fallback?: any) {
  return pick(field, 'TransactionUnitId', 'transactionUnitId') ?? fallback;
}

function norm(value: unknown) {
  return String(value ?? '').trim().toLowerCase();
}

function walkUnits(units: any[] | undefined, visit: (unit: any) => void) {
  const seen = new Set<string>();
  const rec = (list: any[] | undefined) => {
    (list || []).forEach((unit) => {
      if (!unit) return;
      const id = String(unitIdOf(unit) ?? '');
      if (id && seen.has(id)) return;
      if (id) seen.add(id);
      visit(unit);
      rec(unit.Children);
      rec(unit.AppTransactionUnitList);
    });
  };
  rec(units);
}

function dictForUnit(form: any, unitId: any, rootUnitId: any): Record<string, any> {
  if (unitId == null || String(unitId) === String(rootUnitId ?? '')) {
    return form?.DictOneToOneFields || {};
  }
  return form?.DictSiblingOneToOneFields?.[unitId]
    ?? form?.DictSiblingOneToOneFields?.[String(unitId)]
    ?? {};
}

function isGridUnit(form: any, unitId: any) {
  const dict = form?.DictOneToManyFields || {};
  return Array.isArray(dict[unitId]) || Array.isArray(dict[String(unitId)]);
}

function fieldSummary(field: any, unitId?: any) {
  return {
    fieldId: fieldIdOf(field),
    unitId: fieldUnitId(field, unitId),
    dbName: fieldDbName(field),
    label: fieldLabel(field),
    type: pick(field, 'ControlType', 'controlType', 'FieldType', 'fieldType'),
  };
}

function valuesFor(fields: any[], dict: Record<string, any> | undefined, unitId?: any) {
  return (fields || []).slice(0, 80).map((field) => {
    const db = fieldDbName(field);
    const value = db ? (dict?.[db] ?? dict?.[String(db)]) : undefined;
    return { ...fieldSummary(field, unitId), value };
  });
}

function collectOneToOneFields(transaction: any, form: any): any[] {
  const rootUnitId = form?.RootUnitId;
  const found: any[] = [];
  walkUnits(transaction?.AppTransactionUnitList || transaction?.AppTransactionUnitTree, (unit) => {
    const uid = unitIdOf(unit);
    if (isGridUnit(form, uid)) return;
    for (const field of unit.AppTransactionFieldList || []) {
      found.push({
        ...field,
        TransactionUnitId: fieldUnitId(field, uid),
        DataBaseFieldName: fieldDbName(field) || field.DataBaseFieldName,
      });
    }
  });
  if (found.length === 0 && rootUnitId != null) {
    return [];
  }
  return found;
}

function matchPatchFields(fields: any[], key: string): any[] {
  const n = norm(key);
  if (!n) return [];
  const byId = fields.filter((field) => norm(fieldIdOf(field)) === n);
  if (byId.length) return byId;
  const byDb = fields.filter((field) => norm(fieldDbName(field)) === n);
  if (byDb.length) return byDb;
  return fields.filter((field) => norm(fieldLabel(field)) === n);
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
  const rootUnitId = form.RootUnitId ?? unitIdOf(root);
  const actions: any[] = [];
  walkUnits(units, (unit) => {
    const unitId = unitIdOf(unit);
    for (const ls of unit.AppTransactionUnitLinkedSearchList || []) actions.push(actionSummary('linkedSearch', ls, unitId));
    for (const lt of unit.AppFormLinkTargetList || []) actions.push(actionSummary('linkTarget', lt, unitId));
  });
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
    fields: (() => {
      const out: any[] = [];
      walkUnits(units, (unit) => {
        const uid = unitIdOf(unit);
        if (isGridUnit(form, uid)) return;
        out.push(...valuesFor(unit.AppTransactionFieldList || [], dictForUnit(form, uid, rootUnitId), uid));
      });
      return out.slice(0, 120);
    })(),
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

export function applySetFields(formData: any, args: any, mode: 'master' | 'list', transaction?: any): { next: any; applied: string[]; missed: string[] } {
  const patch = parseFields(args?.fieldsJson);
  const keys = Object.keys(patch);
  if (mode === 'list') {
    const rows = Array.isArray(formData?.ListData) ? [...formData.ListData] : [];
    const key = args?.rowKey != null ? String(args.rowKey) : null;
    const index = key
      ? rows.findIndex((row: any, i: number) => String(row?.UIId ?? row?.Id ?? i) === key)
      : 0;
    if (index < 0 || !rows[index]) return { next: formData, applied: [], missed: keys };
    rows[index] = {
      ...rows[index],
      IsDirty: true,
      DictOneToOneFields: { ...(rows[index].DictOneToOneFields || {}), ...patch },
    };
    return { next: { ...formData, IsDirty: true, ListData: rows }, applied: keys, missed: [] };
  }
  const fields = collectOneToOneFields(transaction, formData);
  let next = formData;
  const applied: string[] = [];
  const missed: string[] = [];
  for (const key of keys) {
    const matches = matchPatchFields(fields, key);
    if (matches.length === 0) {
      const n = norm(key);
      let hit = false;
      const rootHit = Object.keys(next?.DictOneToOneFields || {}).find((k) => norm(k) === n);
      if (rootHit) {
        next = buildFormDataWithOneToOneValue(next, { TransactionUnitId: next?.RootUnitId }, rootHit, patch[key]);
        hit = true;
      }
      Object.entries(next?.DictSiblingOneToOneFields || {}).forEach(([uid, dict]: [string, any]) => {
        const sibHit = Object.keys(dict || {}).find((k) => norm(k) === n);
        if (!sibHit) return;
        next = buildFormDataWithOneToOneValue(next, { TransactionUnitId: uid }, sibHit, patch[key]);
        hit = true;
      });
      if (hit) applied.push(key);
      else missed.push(key);
      continue;
    }
    for (const field of matches) {
      const db = fieldDbName(field);
      if (!db) continue;
      next = buildFormDataWithOneToOneValue(next, field, db, patch[key]);
      applied.push(db);
    }
  }
  return { next, applied, missed };
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
