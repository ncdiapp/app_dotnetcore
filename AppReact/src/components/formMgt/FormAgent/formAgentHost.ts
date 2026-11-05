import { createContext, type MutableRefObject } from 'react';
import { buildFormDataWithOneToOneValue } from '../FormMasterDetail/MasterDetailFlexLayoutForm/FormControls/formDataBindingHelper';

export const FORM_AGENT_SKILL_KEY = 'app-transaction-form-agent';
export const LIST_COMMUNICATION_ROOT = '__list__';

export type FormAgentHostSlots = {
  getContext?: () => unknown;
  applyTool?: (toolName: string, args: any) => Promise<unknown>;
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

function unitName(unit: any): string {
  return String(pick(unit, 'TransactionName', 'Display', 'DisplayName', 'DataBaseTableName', 'TableName', 'Name', 'longDisplay') ?? '');
}

function isSiblingUnit(form: any, unitId: any) {
  const s = form?.DictSiblingOneToOneFields || {};
  return s[unitId] != null || s[String(unitId)] != null;
}

function getManyRows(dict: Record<string, any> | undefined, unitId: any): any[] {
  if (!dict) return [];
  const rows = dict[unitId] ?? dict[String(unitId)];
  return Array.isArray(rows) ? rows : [];
}

function collectGridUnits(transaction: any, form: any): Array<{ unit: any; uid: any; rows: any[] }> {
  const rootUnitId = form?.RootUnitId;
  const found: Array<{ unit: any; uid: any; rows: any[] }> = [];
  const byId = new Map<string, { unit: any; uid: any; rows: any[] }>();
  const consider = (unit: any) => {
    const uid = unitIdOf(unit);
    if (uid == null || String(uid) === String(rootUnitId ?? '')) return;
    if (isSiblingUnit(form, uid)) return;
    const key = String(uid);
    const next = { unit, uid, rows: getManyRows(form?.DictOneToManyFields, uid) };
    const prev = byId.get(key);
    const nextLen = (unit?.AppTransactionFieldList || unit?.appTransactionFieldList || []).length;
    const prevLen = (prev?.unit?.AppTransactionFieldList || prev?.unit?.appTransactionFieldList || []).length;
    if (!prev || nextLen > prevLen) byId.set(key, next);
  };
  walkUnits(transaction?.AppTransactionUnitList, consider);
  walkUnits(transaction?.AppTransactionUnitTree, consider);
  walkUnits(form?.AppTransactionUnitList, consider);
  byId.forEach((item) => found.push(item));
  return found;
}

function resolveGridUnit(transaction: any, form: any, rawId: unknown) {
  const grids = collectGridUnits(transaction, form);
  const n = norm(rawId);
  if (!grids.length) return null;
  if (n) {
    const byId = grids.find((g) => String(g.uid) === String(rawId) || norm(g.uid) === n);
    if (byId) return byId;
    const byName = grids.find((g) => {
      const name = norm(unitName(g.unit));
      return name === n || name.includes(n) || n.includes(name);
    });
    if (byName) return byName;
  }
  if (grids.length === 1) return grids[0];
  return null;
}

function fieldSummary(field: any, unitId?: any, entitySourceByFieldId?: Record<string, any[]> | null) {
  const dbName = fieldDbName(field);
  const items = lookupItemsForField(field, entitySourceByFieldId);
  const isLookup = items.length > 0 || /id$/i.test(dbName);
  return {
    fieldId: fieldIdOf(field),
    unitId: fieldUnitId(field, unitId),
    dbName,
    writeKey: dbName,
    label: fieldLabel(field),
    type: pick(field, 'ControlType', 'controlType', 'FieldType', 'fieldType'),
    lookup: isLookup,
    lookupHint: isLookup ? 'Write the Id in fieldsJson[dbName] (e.g. ColorId: 2501). Do not write Color Name.' : undefined,
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

function compact(value: unknown) {
  return norm(value).replace(/[^a-z0-9]/g, '');
}

function stemKey(value: unknown) {
  let c = compact(value);
  for (const suf of ['name', 'code', 'id', 'no', 'number', 'desc', 'description', 'text', 'value', 'title']) {
    if (c.length > suf.length && c.endsWith(suf)) return c.slice(0, -suf.length);
  }
  return c;
}

function isLookupishField(field: any, entitySourceByFieldId?: Record<string, any[]> | null) {
  if (lookupItemsForField(field, entitySourceByFieldId).length > 0) return true;
  if (/id$/i.test(fieldDbName(field))) return true;
  return /dropdown|combo|lookup|entity|select/i.test(String(pick(field, 'ControlType', 'controlType') ?? ''));
}

function pickPreferredField(cands: any[], key: string, entitySourceByFieldId?: Record<string, any[]> | null): any[] {
  if (cands.length <= 1) return cands;
  const sk = stemKey(key);
  const ck = compact(key);
  const exact = cands.filter((field) => {
    const db = compact(fieldDbName(field));
    const label = compact(fieldLabel(field));
    return db === ck || label === ck || db === sk || label === sk;
  });
  const pool = exact.length ? exact : cands;
  const idField = pool.find((field) => /id$/i.test(fieldDbName(field)));
  if (idField) return [idField];
  const lookups = pool.filter((field) => isLookupishField(field, entitySourceByFieldId));
  if (lookups.length === 1) return lookups;
  const shortest = [...pool].sort((a, b) => fieldDbName(a).length - fieldDbName(b).length)[0];
  return shortest ? [shortest] : [pool[0]];
}

function matchPatchFields(fields: any[], key: string, entitySourceByFieldId?: Record<string, any[]> | null): any[] {
  const n = norm(key);
  if (!n) return [];
  const byId = fields.filter((field) => norm(fieldIdOf(field)) === n);
  if (byId.length) return pickPreferredField(byId, key, entitySourceByFieldId);
  const byDb = fields.filter((field) => norm(fieldDbName(field)) === n);
  if (byDb.length) return pickPreferredField(byDb, key, entitySourceByFieldId);
  const byLabel = fields.filter((field) => norm(fieldLabel(field)) === n);
  if (byLabel.length) return pickPreferredField(byLabel, key, entitySourceByFieldId);
  const ck = compact(key);
  const byCompact = fields.filter((field) => compact(fieldDbName(field)) === ck || compact(fieldLabel(field)) === ck);
  if (byCompact.length) return pickPreferredField(byCompact, key, entitySourceByFieldId);
  const sk = stemKey(key);
  const byStem = fields.filter((field) => {
    const db = compact(fieldDbName(field));
    const label = compact(fieldLabel(field));
    return stemKey(db) === sk || stemKey(label) === sk || db === sk || label === sk;
  });
  if (byStem.length) return pickPreferredField(byStem, key, entitySourceByFieldId);
  const byContains = fields.filter((field) => {
    const db = compact(fieldDbName(field));
    const label = compact(fieldLabel(field));
    if (!sk || sk.length < 3) return false;
    return db.includes(sk) || label.includes(sk) || (ck.length >= 3 && (db.includes(ck) || ck.includes(db)));
  });
  if (byContains.length) return pickPreferredField(byContains, key, entitySourceByFieldId);
  return [];
}

function lookupItemsForField(field: any, entitySourceByFieldId?: Record<string, any[]> | null) {
  if (!entitySourceByFieldId) return [];
  const id = fieldIdOf(field);
  const items = entitySourceByFieldId[String(id)] ?? entitySourceByFieldId[id as any];
  return Array.isArray(items) ? items : [];
}

function coerceLookupValue(field: any, value: unknown, entitySourceByFieldId?: Record<string, any[]> | null) {
  if (value == null || value === '') return value;
  const items = lookupItemsForField(field, entitySourceByFieldId);
  if (!items.length) return value;
  const n = String(value).trim();
  const idOf = (item: any) => item?.Id ?? item?.id;
  const dispOf = (item: any) => item?.Display ?? item?.display ?? item?.Name ?? item?.name;
  const byId = items.find((item) => String(idOf(item)) === n);
  if (byId) return idOf(byId);
  const byDisp = items.find((item) => norm(dispOf(item)) === norm(n) || compact(dispOf(item)) === compact(n));
  if (byDisp) return idOf(byDisp);
  if (/^-?\d+(\.\d+)?$/.test(n)) return Number(n);
  return value;
}

function preferMappedValue(prev: unknown, next: unknown) {
  if (prev == null || prev === '') return next;
  if (next == null || next === '') return prev;
  const prevNum = typeof prev === 'number' || /^-?\d+$/.test(String(prev));
  const nextNum = typeof next === 'number' || /^-?\d+$/.test(String(next));
  if (prevNum && !nextNum) return prev;
  return next;
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
  entitySourceByFieldId?: Record<string, any[]> | null;
}): unknown {
  const tx = args.transaction || {};
  const form = args.formData || {};
  const entitySourceByFieldId = args.entitySourceByFieldId ?? form?.DictCascadingFiledDataSource ?? null;
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

  const catalogField = (field: any) => {
    const db = fieldDbName(field);
    if (!db) return null;
    return { db, label: fieldLabel(field) };
  };
  const catalogUnits: any[] = [];
  const walkCatalog = (unit: any, parentPath: string, parentId: any, parentKind: string) => {
    if (!unit) return;
    const uid = unitIdOf(unit);
    const name = unitName(unit) || 'Unit';
    const isRoot = uid != null && String(uid) === String(rootUnitId ?? '');
    const isSibling = Boolean(unit.IsMasterSiblingUnit) || isSiblingUnit(form, uid);
    let kind = 'one';
    if (!isRoot && !isSibling) {
      kind = parentKind === 'grid' || parentKind === 'subgrid' ? 'subgrid' : 'grid';
    }
    const path = isRoot ? 'Header' : (parentPath ? `${parentPath} / ${name}` : name);
    const level = kind === 'subgrid' ? 3 : kind === 'grid' ? 2 : 1;
    const entry: any = {
      unitId: uid,
      path,
      level,
      fields: (unit.AppTransactionFieldList || []).map(catalogField).filter(Boolean).slice(0, 80),
    };
    if (!isRoot && parentId != null) entry.parentUnitId = parentId;
    if (level === 2) {
      entry.rowCount = getManyRows(form?.DictOneToManyFields, uid).length;
    }
    catalogUnits.push(entry);
    const kids = [...(unit.Children || []), ...(unit.AppTransactionUnitList || [])];
    kids.forEach((child: any) => walkCatalog(child, path, uid, kind));
  };
  units.forEach((unit: any) => walkCatalog(unit, '', null, 'one'));

  if (args.mode === 'list') {
    const rows = Array.isArray(form.ListData) ? form.ListData : [];
    const ctx: any = {
      form: tx.TransactionName,
      transactionId: tx.TransactionId ?? tx.Id ?? form.TransactionId,
      units: [{
        unitId: unitIdOf(root),
        path: tx.TransactionName || 'List',
        level: 2,
        rowCount: rows.length,
        fields: rootFields.map(catalogField).filter(Boolean).slice(0, 80),
      }],
    };
    if (actions.length) ctx.actions = actions;
    return ctx;
  }

  const ctx: any = {
    form: tx.TransactionName,
    pk: form.RootPrimaryKeyValue ?? null,
    transactionId: tx.TransactionId ?? tx.Id ?? form.TransactionId,
    units: catalogUnits,
  };
  if (actions.length) ctx.actions = actions;
  return ctx;
}

function unwrapJson(raw: unknown): unknown {
  let value: unknown = raw;
  for (let i = 0; i < 4; i += 1) {
    if (typeof value !== 'string') break;
    const text = value.trim();
    if (!text) break;
    try {
      value = JSON.parse(text);
    } catch {
      break;
    }
  }
  return value;
}

function parseFields(raw: unknown): Record<string, unknown> {
  const value = unwrapJson(raw);
  if (!value) return {};
  if (typeof value === 'object' && !Array.isArray(value)) return value as Record<string, unknown>;
  return {};
}

function pickArg(args: any, ...keys: string[]) {
  if (!args) return undefined;
  for (const key of keys) {
    if (args[key] != null && args[key] !== '') return args[key];
  }
  return undefined;
}

function asCellValue(value: unknown) {
  if (typeof value === 'string' && /^-?\d+$/.test(value.trim())) return Number(value.trim());
  return value;
}

export function applySetFields(formData: any, args: any, mode: 'master' | 'list', transaction?: any): { next: any; applied: string[]; missed: string[] } {
  const slice = normalizedSlice(args, hostTransactionId(formData, transaction));
  if (slice) {
    const writes = Array.isArray(slice?.fieldWrites) ? slice.fieldWrites : [];
    if (writes.length === 0) return { next: formData, applied: [], missed: ['normalized'] };
    if (mode === 'list') {
      const patch: Record<string, unknown> = {};
      writes.forEach((w: any) => { if (w?.dbName) patch[String(w.dbName)] = w.value; });
      const rows = Array.isArray(formData?.ListData) ? [...formData.ListData] : [];
      const key = args?.rowKey != null ? String(args.rowKey) : null;
      const index = key
        ? rows.findIndex((row: any, i: number) => String(row?.UIId ?? row?.Id ?? i) === key)
        : 0;
      if (index < 0 || !rows[index]) return { next: formData, applied: [], missed: Object.keys(patch) };
      rows[index] = {
        ...rows[index],
        IsDirty: true,
        DictOneToOneFields: { ...(rows[index].DictOneToOneFields || {}), ...patch },
      };
      return { next: { ...formData, IsDirty: true, ListData: rows }, applied: Object.keys(patch), missed: [] };
    }
    let next = formData;
    const applied: string[] = [];
    writes.forEach((w: any) => {
      if (!w?.dbName) return;
      next = writeOneToOne(next, w.unitId, String(w.dbName), w.value);
      applied.push(String(w.dbName));
    });
    return { next, applied, missed: [] };
  }
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

function hostTransactionId(formData: any, transaction?: any) {
  return transaction?.TransactionId ?? transaction?.Id ?? formData?.TransactionId ?? formData?.transactionId;
}

function normalizedSlice(args: any, transactionId: any): any | null | undefined {
  const forms = args?.normalizedForms;
  if (!Array.isArray(forms)) return undefined;
  if (forms.length === 0) return null;
  const mine = forms.find((form: any) => String(form?.transactionId) === String(transactionId ?? ''));
  if (mine) return mine;
  return forms.length === 1 ? forms[0] : null;
}

function sameUnit(formData: any, unitId: any) {
  const root = formData?.RootUnitId;
  return unitId == null || root == null || String(unitId) === String(root);
}

function writeOneToOne(formData: any, unitId: any, dbName: string, value: unknown) {
  const bucket = sameUnit(formData, unitId)
    ? (formData?.DictOneToOneFields || {})
    : (formData?.DictSiblingOneToOneFields?.[unitId] || formData?.DictSiblingOneToOneFields?.[String(unitId)] || {});
  const existing = Object.keys(bucket).find((key) => norm(key) === norm(dbName));
  const key = existing || dbName;
  return buildFormDataWithOneToOneValue(
    formData,
    { TransactionUnitId: sameUnit(formData, unitId) ? formData?.RootUnitId : unitId },
    key,
    value,
  );
}

function normalizedRows(slice: any): Record<string, unknown>[] {
  return (Array.isArray(slice?.rows) ? slice.rows : [])
    .filter((row: any) => row && typeof row === 'object' && !Array.isArray(row))
    .map((row: any) => row as Record<string, unknown>);
}

function parseRowPatches(raw: unknown): Record<string, unknown>[] {
  const value = unwrapJson(raw);
  if (Array.isArray(value)) {
    if (value.length && value.every((row) => row != null && typeof row !== 'object')) {
      return value.map((cell) => ({ __value: cell }));
    }
    return value.map((row) => parseFields(row)).filter((row) => Object.keys(row).length > 0);
  }
  if (value && typeof value === 'object' && Array.isArray((value as any).rows)) {
    return (value as any).rows.map((row: unknown) => parseFields(row)).filter((row: Record<string, unknown>) => Object.keys(row).length > 0);
  }
  const one = parseFields(value);
  return Object.keys(one).length ? [one] : [];
}

function primaryLookupWriteKey(fields: any[], entitySourceByFieldId?: Record<string, any[]> | null): string {
  const lookups = (fields || []).filter((field) => isLookupishField(field, entitySourceByFieldId));
  const pickField = lookups.find((field) => /id$/i.test(fieldDbName(field))) ?? lookups[0] ?? (fields || [])[0];
  return pickField ? fieldDbName(pickField) : '';
}

function mapPatchToUnitFields(
  patch: Record<string, unknown>,
  fields: any[],
  entitySourceByFieldId?: Record<string, any[]> | null,
): Record<string, unknown> {
  const out: Record<string, unknown> = {};
  const leftover: unknown[] = [];
  Object.entries(patch).forEach(([key, value]) => {
    const cell = asCellValue(value);
    if (key === '__value') {
      leftover.push(cell);
      return;
    }
    const matches = matchPatchFields(fields, key, entitySourceByFieldId);
    if (matches.length) {
      const field = matches[0];
      const db = fieldDbName(field);
      if (!db) return;
      const coerced = asCellValue(coerceLookupValue(field, cell, entitySourceByFieldId));
      out[db] = Object.prototype.hasOwnProperty.call(out, db) ? preferMappedValue(out[db], coerced) : coerced;
      return;
    }
    leftover.push(cell);
  });
  if (leftover.length && !Object.keys(out).length) {
    const db = primaryLookupWriteKey(fields, entitySourceByFieldId);
    if (db) {
      const field = (fields || []).find((f: any) => fieldDbName(f) === db);
      out[db] = asCellValue(coerceLookupValue(field, leftover[0], entitySourceByFieldId));
    }
  }
  return out;
}

function newGridRow(patch: Record<string, unknown>, fields: any[]): any {
  const dict: Record<string, unknown> = {};
  (fields || []).forEach((field) => {
    const db = fieldDbName(field);
    if (!db) return;
    const hasDefault = field.DefaultValue !== undefined && field.DefaultValue !== null && field.DefaultValue !== '';
    dict[db] = hasDefault ? field.DefaultValue : null;
  });
  Object.assign(dict, patch);
  return {
    UIId: `ai-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    IsNew: true,
    IsDirty: true,
    DictOneToOneFields: dict,
    DictOneToManyFields: {},
  };
}

export function applyUpdateGrid(
  formData: any,
  args: any,
  mode: 'master' | 'list',
  transaction?: any,
  entitySourceByFieldId?: Record<string, any[]> | null,
): { next: any; ok: boolean; unitId?: any; added?: number; error?: string; writeKeys?: string[] } {
  const op = String(pickArg(args, 'op', 'Op') || 'add').toLowerCase();
  const slice = normalizedSlice(args, hostTransactionId(formData, transaction));
  const serverRows = slice ? normalizedRows(slice) : null;
  const patches = serverRows ?? parseRowPatches(pickArg(args, 'fieldsJson', 'FieldsJson', 'rowsJson', 'RowsJson'));
  const key = pickArg(args, 'rowKey', 'RowKey') != null ? String(pickArg(args, 'rowKey', 'RowKey')) : '';
  const rawUnitId = slice?.unitId ?? pickArg(args, 'unitId', 'UnitId');
  const useServerRows = serverRows != null;
  if (mode === 'list' || slice?.list === true) {
    let rows = Array.isArray(formData?.ListData) ? [...formData.ListData] : [];
    if (op === 'add') {
      patches.forEach((patch) => {
        rows = [...rows, newGridRow(patch, [])];
      });
    } else {
      rows = rows.flatMap((row: any, i: number) => {
        const rowKey = String(row?.UIId ?? row?.Id ?? i);
        if (rowKey !== key) return [row];
        if (op === 'delete') return [];
        return [{ ...row, IsDirty: true, DictOneToOneFields: { ...(row.DictOneToOneFields || {}), ...patches[0] } }];
      });
    }
    return { next: { ...formData, IsDirty: true, ListData: rows }, ok: true, added: patches.length };
  }
  const hit = resolveGridUnit(transaction, formData, rawUnitId)
    ?? (slice?.unitId != null
      ? { unit: { AppTransactionFieldList: [] }, uid: slice.unitId, rows: getManyRows(formData?.DictOneToManyFields, slice.unitId) }
      : null);
  if (!hit) {
    return { next: formData, ok: false, error: 'Grid unit was not found. Use a unitId or name from HostContextJson.grids.' };
  }
  const fields = hit.unit?.AppTransactionFieldList || hit.unit?.appTransactionFieldList || [];
  const writeKeys = fields.map((f: any) => fieldDbName(f)).filter(Boolean);
  if (op === 'add' && patches.length === 0) {
    return {
      next: formData,
      ok: false,
      unitId: hit.uid,
      added: 0,
      writeKeys,
      error: `fieldsJson must be a JSON array of row objects using dbName keys (${writeKeys.join(', ') || 'see grids[].writeKeys'}). Example: [{"ColorId":2501}]. Do not call update_grid once per row.`,
    };
  }
  const sources: Record<string, any[]> = {
    ...(formData?.DictCascadingFiledDataSource || {}),
    ...(entitySourceByFieldId || {}),
  };
  const dict = { ...(formData?.DictOneToManyFields || {}) };
  const dictKey = dict[hit.uid] != null ? hit.uid : String(hit.uid);
  let rows = [...getManyRows(dict, hit.uid)];
  let added = 0;
  if (op === 'add') {
    patches.forEach((patch) => {
      const mapped = useServerRows ? patch : mapPatchToUnitFields(patch, fields, sources);
      if (!Object.keys(mapped).length) return;
      rows = [...rows, newGridRow(mapped, fields)];
      added += 1;
    });
    if (added === 0) {
      return {
        next: formData,
        ok: false,
        unitId: hit.uid,
        added: 0,
        writeKeys,
        error: `Could not map fieldsJson onto grid columns. Use writeKeys: ${writeKeys.join(', ')}.`,
      };
    }
  } else {
    rows = rows.flatMap((row: any, i: number) => {
      const rowKey = String(row?.UIId ?? row?.Id ?? i);
      if (rowKey !== key) return [row];
      if (op === 'delete') return [];
      return [{ ...row, IsDirty: true, DictOneToOneFields: { ...(row.DictOneToOneFields || {}), ...(useServerRows ? (patches[0] || {}) : mapPatchToUnitFields(patches[0] || {}, fields, sources)) } }];
    });
  }
  dict[dictKey] = rows;
  return { next: { ...formData, IsDirty: true, DictOneToManyFields: dict }, ok: true, unitId: hit.uid, added, writeKeys };
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
