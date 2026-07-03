import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { dynamicLayoutService } from '../../webapi/dynamiclayoutsvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { fileRegularUrl } from '../../webapi/fileEndpoints';

function toDisplayText(v: any): string {
  if (v == null) return '';
  if (typeof v === 'boolean') return v ? 'Yes' : 'No';
  if (v instanceof Date) return v.toISOString();
  return String(v);
}

function isPrintFieldItem(item: any): boolean {
  return Boolean(item?.ForeignAppTransactionFieldExDto ?? item?.foreignAppTransactionFieldExDto);
}

function isPrintGridItem(item: any, layoutItemTypeEnum: any): boolean {
  const da = item?.DomAttribute ?? item?.domAttribute ?? {};
  const displayType = da?.WidgetDisplayType ?? da?.widgetDisplayType;
  return displayType === layoutItemTypeEnum?.Grid;
}

function getLayoutDisplayType(item: any): any {
  const da = item?.DomAttribute ?? item?.domAttribute ?? {};
  return da?.WidgetDisplayType ?? da?.widgetDisplayType;
}

function getLayoutChildren(item: any): any[] {
  const rows = item?.AppFormLayoutItem_List ?? item?.appFormLayoutItem_List ?? [];
  return Array.isArray(rows) ? rows : [];
}

function collectFieldItemsDeep(items: any[], layoutItemTypeEnum: any, stopAtSections: boolean): any[] {
  const out: any[] = [];
  const visit = (node: any) => {
    if (!node) return;
    if (isPrintFieldItem(node)) {
      out.push(node);
      return;
    }
    const t = getLayoutDisplayType(node);
    if (t === layoutItemTypeEnum?.Grid) return;
    if (stopAtSections && t === layoutItemTypeEnum?.Section) return;
    const children = getLayoutChildren(node);
    for (const c of children) visit(c);
  };
  for (const it of items || []) visit(it);
  return out;
}

function findUnitById(units: any[] | undefined, unitId: number): any | null {
  for (const u of units || []) {
    if (u?.Id != null && Number(u.Id) === Number(unitId)) return u;
    const nested = findUnitById(u?.Children, unitId);
    if (nested) return nested;
  }
  return null;
}

function normalizeBool(v: any): boolean {
  return v === true || v === 1 || v === '1' || v === 'true' || v === 'True';
}

/** Grandchild unit definitions for a grid unit (parity with DataGridLayout.grandChildUnitList). */
function getGrandChildUnitsForPrint(parentUnit: any, transactionExDto: any): any[] {
  const filterByVisibility = (u: any) => u && (u.IsFormLayoutVisible == null || normalizeBool(u.IsFormLayoutVisible));
  const directChildren = (parentUnit?.Children ?? []).filter(filterByVisibility);
  if (directChildren.length > 0) return directChildren;

  const roots: any[] = transactionExDto?.AppTransactionUnitList ?? transactionExDto?.AppTransactionUnitTree ?? [];
  const allUnits: any[] = [];
  const visit = (u: any) => {
    if (!u) return;
    allUnits.push(u);
    (u.Children ?? []).forEach((c: any) => visit(c));
  };
  roots.forEach((r: any) => visit(r));
  const parentId = Number(parentUnit?.Id ?? NaN);
  return allUnits
    .filter((u: any) => Number(u?.ParentTransactionUnitId ?? NaN) === parentId)
    .filter(filterByVisibility);
}

function collectOneToManyRowsDeep(formData: any, unitId: string): { rows: any[]; depth: number } {
  // 0 = root DictOneToManyFields, 1+ = nested under a row's DictOneToManyFields (grandchild, etc.)
  const root = formData?.DictOneToManyFields ?? {};
  if (root && Object.prototype.hasOwnProperty.call(root, unitId) && Array.isArray(root[unitId])) {
    return { rows: root[unitId], depth: 0 };
  }

  const visited = new Set<any>();
  const dfsRows = (rows: any[], depth: number): any[] => {
    const out: any[] = [];
    for (const r of rows || []) {
      if (!r || typeof r !== 'object') continue;
      if (visited.has(r)) continue;
      visited.add(r);
      const childDict = r?.DictOneToManyFields ?? r?.dictOneToManyFields ?? null;
      if (childDict && typeof childDict === 'object') {
        const direct = (childDict as any)[unitId];
        if (Array.isArray(direct) && direct.length > 0) out.push(...direct);
        for (const k of Object.keys(childDict)) {
          const arr = (childDict as any)[k];
          if (Array.isArray(arr) && arr.length > 0) out.push(...dfsRows(arr, depth + 1));
        }
      }
    }
    return out;
  };

  const seedArrays = Object.keys(root || {})
    .map((k) => (root as any)[k])
    .filter((v) => Array.isArray(v) && v.length > 0) as any[];
  const nested = seedArrays.length > 0 ? dfsRows(seedArrays.flat(), 1) : [];
  return { rows: nested, depth: nested.length > 0 ? 1 : 0 };
}

function isMeaningfulContainerTitle(t: string): boolean {
  const title = String(t || '').trim();
  if (!title) return false;
  const lower = title.toLowerCase();
  if (lower === 'stack container' || lower === 'container' || lower === 'section') return false;
  return true;
}

type PrintFieldKV = { label: string; valueNode: React.ReactNode; valueText: string };

function getFieldDtoFromLayoutItem(layoutItemExDto: any): any | null {
  return layoutItemExDto?.ForeignAppTransactionFieldExDto ?? layoutItemExDto?.foreignAppTransactionFieldExDto ?? null;
}

function isImageControlType(controlType: any, controlTypeEnum: any): boolean {
  return Boolean(
    controlType != null &&
      (controlType === controlTypeEnum?.Image ||
        controlType === controlTypeEnum?.ExternalImageUrl ||
        controlType === controlTypeEnum?.ImageBinary)
  );
}

function isMemoControlType(controlType: any, controlTypeEnum: any): boolean {
  return Boolean(controlType != null && (controlType === controlTypeEnum?.Memo || controlType === controlTypeEnum?.RichText));
}

function parseFileIdList(v: any): number[] {
  if (v == null) return [];
  if (typeof v === 'number' && Number.isFinite(v)) return [v];
  const s = String(v).trim();
  if (!s) return [];
  return s
    .split(/[,\s;|]+/g)
    .map((x) => Number(String(x).trim()))
    .filter((n) => Number.isFinite(n) && n > 0);
}

function getSessionIdForPrint(): string | null {
  try {
    return typeof window !== 'undefined' ? (localStorage.getItem('sessionId') as string | null) : null;
  } catch {
    return null;
  }
}

function buildFileImageUrl(fileId: number, _sessionId: string | null): string {
  // Regular-size image served by the Core app (session appended by the helper).
  return fileRegularUrl(fileId);
}

function resolveDecimalDigits(fieldDto: any): number {
  const raw =
    fieldDto?.Nbdecimal ??
    fieldDto?.NbDecimal ??
    fieldDto?.NumberOfDecimal ??
    fieldDto?.Scale ??
    fieldDto?.DecimalDigits ??
    fieldDto?.Decimals ??
    fieldDto?.Digit ??
    null;
  const n = typeof raw === 'number' ? raw : typeof raw === 'string' ? Number(raw) : NaN;
  if (Number.isFinite(n) && n >= 0) return Math.min(10, Math.floor(n));
  return fieldDto?.IsDecimal ? 2 : 0;
}

function isDdLikeControlType(controlType: any, controlTypeEnum: any): boolean {
  const ctl = controlType != null ? Number(controlType) : NaN;
  return (
    ctl === Number(controlTypeEnum?.DDL) ||
    ctl === Number(controlTypeEnum?.SearchAbleDDL) ||
    ctl === Number(controlTypeEnum?.AutoComplete)
  );
}

function getStandaloneLookupItems(formStructure: any, fieldIdStr: string): any[] {
  const dictEntity = formStructure?.DictStandAloneEntityDataSource ?? {};
  const dictMapping = formStructure?.DictStandAloneFiledIDMappingEntityID ?? {};
  const entityId = dictMapping[fieldIdStr];
  if (entityId == null) return [];
  const items = dictEntity[String(entityId)] ?? dictEntity[entityId] ?? [];
  return Array.isArray(items) ? items : [];
}

function getForeignMasterLookupItems(formData: any, field: any): any[] {
  const fu = field?.TransactionUnitId;
  const fid = field?.Id;
  if (fu == null || fid == null) return [];
  return formData?.DictForeignUnitMasterFieldlIdLookupItem?.[String(fu)]?.[String(fid)] ?? [];
}

function resolveCascadingLookupItemsForPrint(
  formData: any,
  formStructure: any,
  gridUnitIdStr: string,
  rowData: any,
  fieldIdStr: string
): any[] | null {
  const rootForm = formData;
  const rootDict = rootForm?.DictCascadingFiledDataSource ?? {};
  const rowDict = rowData?.DictCascadingFiledDataSource ?? {};
  const rootItems = rootDict[fieldIdStr];
  const rowItems = rowDict[fieldIdStr];

  const dictCascadedParent = formStructure?.DictCascadedIdParentField ?? {};
  const dictFieldUnit = formStructure?.DictFieldIdUnitId ?? {};
  const parentFieldIdRaw = dictCascadedParent[fieldIdStr];
  const parentFieldIdStr = parentFieldIdRaw != null && parentFieldIdRaw !== '' ? String(parentFieldIdRaw) : '';

  if (parentFieldIdStr) {
    const parentUnitRaw = dictFieldUnit[parentFieldIdStr] ?? dictFieldUnit[String(Number(parentFieldIdStr))];
    const parentUnitIdStr = parentUnitRaw != null ? String(parentUnitRaw) : '';

    if (parentUnitIdStr && parentUnitIdStr === gridUnitIdStr) {
      if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
      if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
      return null;
    }
    if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
    if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
    return null;
  }

  if (Array.isArray(rowItems) && rowItems.length > 0) return rowItems;
  if (Array.isArray(rootItems) && rootItems.length > 0) return rootItems;
  return null;
}

function resolveDdlLookupItemsForPrint(
  field: any,
  formData: any,
  formStructure: any,
  gridUnitIdStr: string | null,
  rowData: any | null
): any[] {
  const fieldIdStr = field?.Id != null ? String(field.Id) : '';
  const usedIds = formData?.IsUsedCascadingDataSourceFiedIds;
  const usedSet = Array.isArray(usedIds) ? new Set(usedIds.map((x: any) => String(x))) : null;

  if (fieldIdStr && usedSet?.has(fieldIdStr) && gridUnitIdStr) {
    const cascaded = resolveCascadingLookupItemsForPrint(formData, formStructure, gridUnitIdStr, rowData, fieldIdStr);
    if (cascaded && cascaded.length > 0) return cascaded;
  }

  const itemSrc = field?.ItemSource ?? field?.itemSource;
  if (Array.isArray(itemSrc) && itemSrc.length > 0) return itemSrc;

  const standalone = getStandaloneLookupItems(formStructure, fieldIdStr);
  if (standalone.length > 0) return standalone;

  const foreign = getForeignMasterLookupItems(formData, field);
  if (foreign.length > 0) return foreign;

  return [];
}

function lookupDisplayFromItems(items: any[], rawValue: any): string {
  if (rawValue == null || rawValue === '') return '';
  const idStr = String(rawValue);
  const hit = (items || []).find((x: any) => x != null && String(x.Id ?? '') === idStr);
  return hit?.Display != null && hit?.Display !== '' ? String(hit.Display) : '';
}

function parseIsoDate(v: any): Date | null {
  if (v instanceof Date && !Number.isNaN(v.getTime())) return v;
  const s = String(v ?? '').trim();
  if (!s) return null;
  const d = new Date(s);
  return Number.isNaN(d.getTime()) ? null : d;
}

function pad2(n: number): string {
  return String(n).padStart(2, '0');
}

function formatDateForPrint(value: any, controlType: any, controlTypeEnum: any): string {
  const d = parseIsoDate(value);
  if (!d) return value == null ? '' : String(value);
  const y = d.getFullYear();
  const m = pad2(d.getMonth() + 1);
  const day = pad2(d.getDate());
  if (controlType === controlTypeEnum?.DateTimeDetail) {
    const hh = pad2(d.getHours());
    const mm = pad2(d.getMinutes());
    return `${y}-${m}-${day} ${hh}:${mm}`;
  }
  if (controlType === controlTypeEnum?.Time) {
    return `${pad2(d.getHours())}:${pad2(d.getMinutes())}`;
  }
  return `${y}-${m}-${day}`;
}

function formatScalarForPrint(
  rawValue: any,
  field: any,
  formData: any,
  formStructure: any,
  controlTypeEnum: any,
  gridUnitIdStr: string | null,
  rowData: any | null
): string {
  if (rawValue === null || rawValue === undefined) return '';
  const controlType = field?.ControlType;

  if (controlType === controlTypeEnum?.CheckBox) {
    return rawValue ? 'Yes' : 'No';
  }

  if (isDdLikeControlType(controlType, controlTypeEnum)) {
    const items = resolveDdlLookupItemsForPrint(field, formData, formStructure, gridUnitIdStr, rowData);
    const disp = lookupDisplayFromItems(items, rawValue);
    return disp || toDisplayText(rawValue);
  }

  if (controlType === controlTypeEnum?.Numeric) {
    const digits = resolveDecimalDigits(field);
    const n = typeof rawValue === 'number' ? rawValue : Number(String(rawValue).trim());
    if (!Number.isFinite(n)) return '';
    return n.toFixed(digits);
  }

  if (controlType === controlTypeEnum?.Date || controlType === controlTypeEnum?.DateTimeDetail || controlType === controlTypeEnum?.Time) {
    return formatDateForPrint(rawValue, controlType, controlTypeEnum);
  }

  return toDisplayText(rawValue);
}

function renderImageCellValue(rawValue: any, sessionId: string | null, variant: 'root' | 'gridThumb'): React.ReactNode {
  const ids = parseFileIdList(rawValue);
  if (ids.length === 0) return '';
  const imgClass = variant === 'gridThumb' ? 'print-img print-img-thumb' : 'print-img';
  return (
    <div className={variant === 'gridThumb' ? 'print-img-list print-img-list--thumb' : 'print-img-list'}>
      {ids.slice(0, 4).map((id: number) => (
        <img key={id} className={imgClass} src={buildFileImageUrl(id, sessionId)} alt="" />
      ))}
    </div>
  );
}

function getFieldLabelValueFromLayoutItem(
  layoutItemExDto: any,
  formData: any,
  controlTypeEnum: any,
  sessionId: string | null,
  printFormStructure?: any | null
): PrintFieldKV | null {
  const fieldDto = getFieldDtoFromLayoutItem(layoutItemExDto);
  if (!fieldDto) return null;
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldDto.DataBaseFieldName || '';
  const db = fieldDto.DataBaseFieldName;
  const rootUnitId = formData?.RootUnitId ?? null;
  const unitId = fieldDto?.TransactionUnitId ?? null;
  const isSibling = rootUnitId != null && unitId != null && String(unitId) !== String(rootUnitId);
  const rawValue = isSibling
    ? formData?.DictSiblingOneToOneFields?.[String(unitId)]?.[db]
    : formData?.DictOneToOneFields?.[db];

  const controlType = fieldDto?.ControlType;
  if (isImageControlType(controlType, controlTypeEnum)) {
    const ids = parseFileIdList(rawValue);
    if (ids.length === 0) return { label: String(label || ''), valueNode: '', valueText: '' };
    const valueNode = renderImageCellValue(rawValue, sessionId, 'root');
    return { label: String(label || ''), valueNode, valueText: ids.join(',') };
  }

  const valueText = formatScalarForPrint(rawValue, fieldDto, formData, printFormStructure ?? null, controlTypeEnum, null, null);
  return { label: String(label || ''), valueNode: valueText, valueText };
}

const PrintFieldTable: React.FC<{
  fieldItems: any[];
  formData: any;
  controlTypeEnum: any;
  sessionId: string | null;
  printFormStructure?: any | null;
  debug?: boolean;
}> = ({ fieldItems, formData, controlTypeEnum, sessionId, printFormStructure = null, debug = false }) => {
  // Data-level 2-column layout in ONE table:
  // Normalize to a dense KV list, then pack two fields per row: (0,1), (2,3), ...
  const kvList = (fieldItems || [])
    .map((it: any) => getFieldLabelValueFromLayoutItem(it, formData, controlTypeEnum, sessionId, printFormStructure))
    .filter((kv: any): kv is PrintFieldKV => {
      return (
        Boolean(kv) &&
        (String(kv.label || '').trim() !== '' || String((kv as any).valueText || '').trim() !== '')
      );
    });

  const rowCount = Math.ceil(kvList.length / 2);

  const renderRows = () =>
    Array.from({ length: rowCount }).map((_, idx) => {
      const l = kvList[idx * 2] ?? null;
      const r = kvList[idx * 2 + 1] ?? null;
      return (
        <tr key={idx}>
          <td className="print-th print-field-th">{l?.label ?? ''}</td>
          <td className="print-td print-field-td">{l?.valueNode ?? ''}</td>
          <td className="print-th print-field-th">{r?.label ?? ''}</td>
          <td className="print-td print-field-td">{r?.valueNode ?? ''}</td>
        </tr>
      );
    });

  return (
    <>
      {debug ? (
        <div className="print-debug">
          <div>
            <b>DEBUG PrintFieldTable</b> fieldItems={fieldItems?.length ?? 0} kvList={kvList.length} rowCount={rowCount}
          </div>
          <div style={{ marginTop: 4 }}>
            firstLabels: {kvList.slice(0, 10).map((x) => x.label).join(' | ')}
          </div>
          <div style={{ marginTop: 4 }}>
            firstPairs:{' '}
            {Array.from({ length: Math.min(6, rowCount) })
              .map((_, i) => {
                const l = kvList[i * 2];
                const r = kvList[i * 2 + 1];
                return `[${i}] L=${l?.label ?? ''} / R=${r?.label ?? ''}`;
              })
              .join(' , ')}
          </div>
        </div>
      ) : null}
      <table className="print-table print-field-table" style={{ width: '100%', tableLayout: 'fixed' }}>
        <tbody>{renderRows()}</tbody>
      </table>
    </>
  );
};

const PrintLayoutItem: React.FC<{
  item: any;
  transactionExDto: any;
  formData: any;
  layoutItemTypeEnum: any;
  controlTypeEnum: any;
  sessionId: string | null;
  printFormStructure?: any | null;
  debug?: boolean;
  depth?: number;
  skipFields?: boolean;
  suppressSectionTitleKey?: string | null;
  suppressSectionTitleText?: string | null;
}> = ({
  item,
  transactionExDto,
  formData,
  layoutItemTypeEnum,
  controlTypeEnum,
  sessionId,
  printFormStructure = null,
  debug = false,
  depth = 0,
  skipFields = false,
  suppressSectionTitleKey = null,
  suppressSectionTitleText = null,
}) => {
  const da = item?.DomAttribute ?? item?.domAttribute ?? {};
  const displayType = da?.WidgetDisplayType ?? da?.widgetDisplayType;

  // Basic visibility gates (print is read-only; keep it simple)
  const visibleExpr = (da?.VisibleExpression ?? da?.visibleExpression) ?? 'true';
  if (visibleExpr === 'false') return null;

  if (displayType === layoutItemTypeEnum?.NewItemAddButton) return null;

  // Section
  if (displayType === layoutItemTypeEnum?.Section) {
    const titleRaw = (da?.DisplayName ?? da?.displayName) || '';
    const title = String(titleRaw || '').trim();
    const isDefaultContainerTitle =
      title === '' ||
      title.toLowerCase() === 'stack container' ||
      title.toLowerCase() === 'container' ||
      title.toLowerCase() === 'section';
    const rows = getLayoutChildren(item);
    const sorted = [...rows].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0));

    // Collect ALL descendant fields under this section (across multi-column container trees),
    // but stop at nested Sections so each section can print its own block.
    const sectionFieldItems = skipFields ? [] : collectFieldItemsDeep(sorted, layoutItemTypeEnum, true);
    const nonFieldItems = sorted.filter((x: any) => !isPrintFieldItem(x));

    const memoFieldItems = sectionFieldItems.filter((it: any) => {
      const dto = getFieldDtoFromLayoutItem(it);
      return dto ? isMemoControlType(dto.ControlType, controlTypeEnum) : false;
    });
    const imageFieldItems = sectionFieldItems.filter((it: any) => {
      const dto = getFieldDtoFromLayoutItem(it);
      return dto ? isImageControlType(dto.ControlType, controlTypeEnum) : false;
    });
    const regularFieldItems = sectionFieldItems.filter((it: any) => {
      const dto = getFieldDtoFromLayoutItem(it);
      if (!dto) return false;
      return !isMemoControlType(dto.ControlType, controlTypeEnum) && !isImageControlType(dto.ControlType, controlTypeEnum);
    });

    // Hide noisy container wrappers/titles; only show section chrome when it has a meaningful title.
    const showChrome = !isDefaultContainerTitle;

    // If this is a default container (e.g. "STACK CONTAINER"), flatten output to avoid blocking 2-column print flow.
    if (!showChrome) {
      return (
        <>
          {regularFieldItems.length > 0 ? (
            <PrintFieldTable
              fieldItems={regularFieldItems}
              formData={formData}
              controlTypeEnum={controlTypeEnum}
              sessionId={sessionId}
              printFormStructure={printFormStructure}
              debug={debug}
            />
          ) : null}

          {memoFieldItems.length > 0 ? (
            <div className="print-memo-blocks">
              {memoFieldItems.map((it: any) => {
                const kv = getFieldLabelValueFromLayoutItem(it, formData, controlTypeEnum, sessionId, printFormStructure);
                if (!kv) return null;
                return (
                  <div key={it?.Id ?? kv.label} className="print-memo-block break-inside-avoid">
                    <div className="print-memo-label">{kv.label}</div>
                    <div className="print-memo-value">{kv.valueText}</div>
                  </div>
                );
              })}
            </div>
          ) : null}

          {imageFieldItems.length > 0 ? (
            <div className="print-images-grid break-inside-avoid">
              {imageFieldItems.map((it: any) => {
                const kv = getFieldLabelValueFromLayoutItem(it, formData, controlTypeEnum, sessionId, printFormStructure);
                if (!kv) return null;
                return (
                  <div key={it?.Id ?? kv.label} className="print-image-block">
                    <div className="print-image-label">{kv.label}</div>
                    <div className="print-image-body">{kv.valueNode}</div>
                  </div>
                );
              })}
            </div>
          ) : null}

          {nonFieldItems.length > 0
            ? nonFieldItems.map((r: any, idx: number) => (
                <PrintLayoutItem
                  key={r?.Id ?? `sec-${idx}`}
                  item={r}
                  transactionExDto={transactionExDto}
                  formData={formData}
                  layoutItemTypeEnum={layoutItemTypeEnum}
                  controlTypeEnum={controlTypeEnum}
                  sessionId={sessionId}
                  printFormStructure={printFormStructure}
                  debug={debug}
                  depth={depth + 1}
                  // If we already printed section fields above, suppress fields in descendants,
                  // except nested Sections (they print their own field blocks).
                  skipFields={
                    skipFields ||
                    (sectionFieldItems.length > 0 && getLayoutDisplayType(r) !== layoutItemTypeEnum?.Section)
                  }
                  suppressSectionTitleKey={suppressSectionTitleKey}
                  suppressSectionTitleText={suppressSectionTitleText}
                />
              ))
            : null}
        </>
      );
    }

    // If we are skipping fields and there are no non-field items, omit this section entirely.
    if (skipFields && nonFieldItems.length === 0) return null;

    const thisSectionKey = String(item?.Id ?? item?.CurrentHostId ?? '');
    const thisTitleNorm = String(title || '').trim().toLowerCase();
    const suppressTitleNorm = String(suppressSectionTitleText || '').trim().toLowerCase();
    // Deterministic: when printing non-field content (skipFields=true), never render section titles.
    // Field container label is rendered exactly once above the global field table.
    const suppressThisTitle =
      skipFields === true ||
      ((
        suppressSectionTitleKey != null &&
        suppressSectionTitleKey !== '' &&
        thisSectionKey !== '' &&
        thisSectionKey === suppressSectionTitleKey
      ) ||
        (suppressTitleNorm !== '' && thisTitleNorm !== '' && thisTitleNorm === suppressTitleNorm));

    return (
      <section className="print-section break-inside-avoid">
        {!suppressThisTitle ? (
          <div className="print-section-title">
            <span>{title}</span>
          </div>
        ) : null}

        {regularFieldItems.length > 0 ? (
          <PrintFieldTable
            fieldItems={regularFieldItems}
            formData={formData}
            controlTypeEnum={controlTypeEnum}
            sessionId={sessionId}
            printFormStructure={printFormStructure}
            debug={debug}
          />
        ) : null}

        {memoFieldItems.length > 0 ? (
          <div className="print-memo-blocks">
            {memoFieldItems.map((it: any) => {
              const kv = getFieldLabelValueFromLayoutItem(it, formData, controlTypeEnum, sessionId, printFormStructure);
              if (!kv) return null;
              return (
                <div key={it?.Id ?? kv.label} className="print-memo-block break-inside-avoid">
                  <div className="print-memo-label">{kv.label}</div>
                  <div className="print-memo-value">{kv.valueText}</div>
                </div>
              );
            })}
          </div>
        ) : null}

        {imageFieldItems.length > 0 ? (
          <div className="print-images-grid break-inside-avoid">
            {imageFieldItems.map((it: any) => {
              const kv = getFieldLabelValueFromLayoutItem(it, formData, controlTypeEnum, sessionId, printFormStructure);
              if (!kv) return null;
              return (
                <div key={it?.Id ?? kv.label} className="print-image-block">
                  <div className="print-image-label">{kv.label}</div>
                  <div className="print-image-body">{kv.valueNode}</div>
                </div>
              );
            })}
          </div>
        ) : null}

        {nonFieldItems.length > 0 ? (
          <div className="print-flow">
            {nonFieldItems.map((r: any, idx: number) => (
              <PrintLayoutItem
                key={r?.Id ?? `sec-${idx}`}
                item={r}
                transactionExDto={transactionExDto}
                formData={formData}
                layoutItemTypeEnum={layoutItemTypeEnum}
                controlTypeEnum={controlTypeEnum}
                sessionId={sessionId}
                printFormStructure={printFormStructure}
                debug={debug}
                depth={depth + 1}
                skipFields={
                  skipFields || (sectionFieldItems.length > 0 && getLayoutDisplayType(r) !== layoutItemTypeEnum?.Section)
                }
                suppressSectionTitleKey={suppressSectionTitleKey}
                suppressSectionTitleText={suppressSectionTitleText}
              />
            ))}
          </div>
        ) : null}
      </section>
    );
  }

  // TabContainer: print all tabs sequentially (no interaction)
  if (displayType === layoutItemTypeEnum?.TabContainer) {
    const tabs = item?.AppFormLayoutItem_List ?? item?.appFormLayoutItem_List ?? [];
    const sortedTabs = Array.isArray(tabs)
      ? [...tabs].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder ?? 0) - (b.FlowOrGridLayoutSortOrder ?? 0))
      : [];
    return (
      <div className="space-y-4">
        {sortedTabs.map((t: any, idx: number) => {
          const tda = t?.DomAttribute ?? t?.domAttribute ?? {};
          const tabTitle = (tda?.DisplayName ?? tda?.displayName) || '';
          return (
            <section key={t?.Id ?? `tab-${idx}`} className="break-inside-avoid">
              {/* No tab labels in print */}
              <PrintLayoutItem
                item={t}
                transactionExDto={transactionExDto}
                formData={formData}
                layoutItemTypeEnum={layoutItemTypeEnum}
                controlTypeEnum={controlTypeEnum}
                sessionId={sessionId}
                printFormStructure={printFormStructure}
                debug={debug}
                depth={1}
                skipFields={skipFields}
                suppressSectionTitleKey={suppressSectionTitleKey}
                suppressSectionTitleText={suppressSectionTitleText}
              />
            </section>
          );
        })}
      </div>
    );
  }

  // Grid: render as simple HTML table
  if (displayType === layoutItemTypeEnum?.Grid) {
    const unitId = item?.GridTransactionUnitId ?? item?.gridTransactionUnitId ?? item?.ForeignAppTransactionUnitExDto?.Id;
    const tx = transactionExDto;
    const unit = unitId != null ? findUnitById(tx?.AppTransactionUnitList, Number(unitId)) : (item?.ForeignAppTransactionUnitExDto ?? null);
    if (!unit) return null;

    const unitTitle = unit?.UnitDisplayName || unit?.DisplayName || unit?.UnitName || '';
    const fields = Array.isArray(unit?.AppTransactionFieldList) ? unit.AppTransactionFieldList : [];
    const visibleFields = fields.filter((f: any) => f && f.IsGridVisible !== false && f.IsFormLayoutVisible !== false);
    const gridUnitIdStr = String(unit.Id);
    const directRows = formData?.DictOneToManyFields?.[gridUnitIdStr];
    const mainRows = Array.isArray(directRows) ? directRows : [];
    const deep = collectOneToManyRowsDeep(formData, gridUnitIdStr);
    const rows = mainRows.length > 0 ? mainRows : Array.isArray(deep.rows) ? deep.rows : [];
    const oneToManyDepth = mainRows.length > 0 ? 0 : deep.rows?.length > 0 ? deep.depth : 0;
    const grandChildUnits = getGrandChildUnitsForPrint(unit, transactionExDto);
    const showNestedSubgrids = mainRows.length > 0 && grandChildUnits.length > 0;

    const renderGridCell = (rowData: any, f: any, gridCtxUnitIdStr: string): React.ReactNode => {
      const db = f.DataBaseFieldName;
      const v = rowData?.DictOneToOneFields?.[db];
      if (isImageControlType(f?.ControlType, controlTypeEnum)) {
        return renderImageCellValue(v, sessionId, 'gridThumb');
      }
      return formatScalarForPrint(v, f, formData, printFormStructure, controlTypeEnum, gridCtxUnitIdStr, rowData);
    };

    return (
      <section className={`print-grid break-inside-avoid ${oneToManyDepth > 0 ? 'print-subgrid' : ''}`}>
        {unitTitle ? <div className="print-grid-title">{unitTitle}</div> : null}
        <div className="print-table-wrap">
          <table className="print-table">
            <thead>
              <tr>
                {visibleFields.map((f: any) => (
                  <th key={f.Id} className="print-th">
                    {f.DisplayName || f.LabelDisplayBinding || f.DataBaseFieldName || f.Id}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {(Array.isArray(rows) ? rows : []).map((r: any, idx: number) => (
                <React.Fragment key={r?.Id ?? `row-${idx}`}>
                  <tr>
                    {visibleFields.map((f: any) => (
                      <td key={`${idx}-${f.Id}`} className="print-td">
                        {renderGridCell(r, f, gridUnitIdStr)}
                      </td>
                    ))}
                  </tr>
                  {showNestedSubgrids ? (
                    <tr className="print-subgrid-tr">
                      <td className="print-td print-subgrid-td" colSpan={Math.max(1, visibleFields.length)}>
                        <div className="print-subgrid-stack">
                          {grandChildUnits.map((gc: any) => {
                            const gcId = String(gc.Id ?? gc.unitId ?? '');
                            const gcTitle = gc.UnitDisplayName || gc.DisplayName || gc.UnitName || gcId;
                            const subRows = r?.DictOneToManyFields?.[gcId] ?? r?.dictOneToManyFields?.[gcId] ?? [];
                            if (!Array.isArray(subRows) || subRows.length === 0) return null;
                            const gcFields = Array.isArray(gc?.AppTransactionFieldList) ? gc.AppTransactionFieldList : [];
                            const gcVisible = gcFields.filter(
                              (f: any) => f && f.IsGridVisible !== false && f.IsFormLayoutVisible !== false
                            );
                            if (gcVisible.length === 0) return null;
                            return (
                              <div key={`${idx}-gc-${gcId}`} className="print-subgrid-block break-inside-avoid">
                                <div className="print-subgrid-title">{gcTitle}</div>
                                <div className="print-table-wrap">
                                  <table className="print-table print-subgrid-table">
                                    <thead>
                                      <tr>
                                        {gcVisible.map((cf: any) => (
                                          <th key={cf.Id} className="print-th">
                                            {cf.DisplayName || cf.LabelDisplayBinding || cf.DataBaseFieldName || cf.Id}
                                          </th>
                                        ))}
                                      </tr>
                                    </thead>
                                    <tbody>
                                      {subRows.map((sr: any, j: number) => (
                                        <tr key={sr?.Id ?? `sr-${idx}-${j}`}>
                                          {gcVisible.map((cf: any) => (
                                            <td key={`${j}-${cf.Id}`} className="print-td">
                                              {renderGridCell(sr, cf, gcId)}
                                            </td>
                                          ))}
                                        </tr>
                                      ))}
                                    </tbody>
                                  </table>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      </td>
                    </tr>
                  ) : null}
                </React.Fragment>
              ))}
              {(!rows || (Array.isArray(rows) && rows.length === 0)) && (
                <tr>
                  <td className="print-td print-empty" colSpan={Math.max(1, visibleFields.length)}>
                    No data.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    );
  }

  // Field
  const fieldDto = getFieldDtoFromLayoutItem(item);
  if (fieldDto) {
    // Fields are rendered in grouped tables by parent containers (4 columns: L,V,L,V).
    // Keep a tiny fallback for unexpected standalone field items.
    if (skipFields) return null;
    const controlType = fieldDto?.ControlType;
    const kv = getFieldLabelValueFromLayoutItem(item, formData, controlTypeEnum, sessionId, printFormStructure);
    if (!kv) return null;

    if (isMemoControlType(controlType, controlTypeEnum)) {
      return (
        <div className="print-memo-block break-inside-avoid">
          <div className="print-memo-label">{kv.label}</div>
          <div className="print-memo-value">{kv.valueText}</div>
        </div>
      );
    }

    if (isImageControlType(controlType, controlTypeEnum)) {
      return (
        <div className="print-image-block break-inside-avoid">
          <div className="print-image-label">{kv.label}</div>
          <div className="print-image-body">{kv.valueNode}</div>
        </div>
      );
    }

    return (
      <PrintFieldTable
        fieldItems={[item]}
        formData={formData}
        controlTypeEnum={controlTypeEnum}
        sessionId={sessionId}
        printFormStructure={printFormStructure}
        debug={debug}
      />
    );
  }

  // Row/container: render children
  const children = item?.AppFormLayoutItem_List ?? item?.appFormLayoutItem_List ?? [];
  if (Array.isArray(children) && children.length > 0) {
    const sorted = [...children].sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0));

    const fieldItems = skipFields ? [] : sorted.filter((x: any) => isPrintFieldItem(x));
    const nonFieldItems = sorted.filter((x: any) => !isPrintFieldItem(x));
    const hasGrid = sorted.some((x: any) => isPrintGridItem(x, layoutItemTypeEnum));
    const allFields = nonFieldItems.length === 0 && fieldItems.length > 0;
    return (
      <>
        {fieldItems.length > 0 ? (
          <PrintFieldTable
            fieldItems={fieldItems}
            formData={formData}
            controlTypeEnum={controlTypeEnum}
            sessionId={sessionId}
            printFormStructure={printFormStructure}
            debug={debug}
          />
        ) : null}

        {/* Flatten non-field items so grids can span both columns */}
        {nonFieldItems.length > 0
          ? nonFieldItems.map((c: any, idx: number) => (
              <PrintLayoutItem
                key={c?.Id ?? `child-${idx}`}
                item={c}
                transactionExDto={transactionExDto}
                formData={formData}
                layoutItemTypeEnum={layoutItemTypeEnum}
                controlTypeEnum={controlTypeEnum}
                sessionId={sessionId}
                printFormStructure={printFormStructure}
                debug={debug}
                depth={depth + 1}
                skipFields={skipFields}
              />
            ))
          : null}
      </>
    );
  }

  return null;
};

const FormMasterDetailPrint: React.FC = () => {
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const controlTypeEnum = useEnumValues('EmAppControlType');

  const { param } = useParams<{ param: string }>();
  const paramObj = useMemo(() => {
    if (!param) return {};
    try {
      return JSON.parse(decodeURIComponent(param));
    } catch (e) {
      return {};
    }
  }, [param]);
  const debug = String((paramObj as any)?.debug ?? '') === '1';
  const sessionId = useMemo(() => getSessionIdForPrint(), []);

  const transactionId = paramObj?.id ? Number(paramObj.id) : null;
  const rootPrimaryKeyValue = paramObj?.param1 ?? null;

  const [transactionExDto, setTransactionExDto] = useState<any>(null);
  const [formData, setFormData] = useState<any>(null);
  const [printFormStructure, setPrintFormStructure] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(false);
  const hasPrintedRef = useRef(false);

  useEffect(() => {
    const load = async () => {
      if (!transactionId) return;
      if (rootPrimaryKeyValue == null || String(rootPrimaryKeyValue).trim() === '') return;
      try {
        dispatch(setIsBusy());
        setIsLoading(true);

        const [tx, structure] = await Promise.all([
          // Print DTO from backend (Angular: AppMasterDetailFormPrintBL)
          dynamicLayoutService.getTransactionForm(
            transactionId,
            undefined,
            String(rootPrimaryKeyValue),
            'true',
            undefined,
            'true'
          ),
          appTransactionService.getFormStructure(transactionId),
        ]);
        setTransactionExDto(tx);
        setPrintFormStructure(structure);

        // Ensure we have values (API parity with normal form)
        const data = await appTransactionService.getFormData({
          transactionId,
          rootPrimaryKeyValue: String(rootPrimaryKeyValue),
          autoExecuteCommandId: undefined,
          selectDataRow: null,
        });
        setFormData(data);
      } catch (e) {
        showError((e as Error)?.message || String(e));
      } finally {
        setIsLoading(false);
        dispatch(setIsNotBusy());
      }
    };
    load();
  }, [dispatch, rootPrimaryKeyValue, showError, transactionId]);

  const formDto = transactionExDto?.ForeignAppFormExDto ?? null;
  const layoutItems: any[] = useMemo(() => {
    const list = formDto?.AppFormLayoutItemList;
    if (!Array.isArray(list)) return [];
    return [...list].filter(Boolean).sort((a: any, b: any) => (a.FlowOrGridLayoutSortOrder || 0) - (b.FlowOrGridLayoutSortOrder || 0));
  }, [formDto?.AppFormLayoutItemList]);

  useEffect(() => {
    if (hasPrintedRef.current) return;
    if (isLoading) return;
    if (!transactionExDto || !formData || !layoutItemTypeEnum) return;
    // Let browser paint first.
    hasPrintedRef.current = true;
    setTimeout(() => {
      try {
        window.print();
      } catch {
        // ignore
      }
    }, 300);
  }, [formData, isLoading, layoutItemTypeEnum, transactionExDto]);

  useEffect(() => {
    // Nice-to-have: update document title for print window.
    const title = transactionExDto?.TransactionName || formData?.TransactionName || 'Form';
    try {
      // Don't include "Print" or timestamps (browser print header/footer can show time).
      document.title = rootPrimaryKeyValue != null && String(rootPrimaryKeyValue).trim() !== '' ? `${title} ${String(rootPrimaryKeyValue)}` : title;
    } catch {
      // ignore
    }
  }, [formData?.TransactionName, transactionExDto?.TransactionName]);

  if (!layoutItemTypeEnum) {
    return <div className="p-4 text-sm text-gray-500">Loading...</div>;
  }

  if (!transactionId || rootPrimaryKeyValue == null || String(rootPrimaryKeyValue).trim() === '') {
    return <div className="p-4 text-sm text-red-600">Invalid print parameters.</div>;
  }

  return (
    <div className="min-h-screen bg-white text-black">
      <style>
        {`
          /* Letter paper */
          @page { size: letter; margin: 0.5in; }

          @media print {
            .no-print { display: none !important; }
            html, body { background: #fff !important; }
            body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
          }

          /* Page container */
          .print-page {
            width: 8.5in;
            max-width: 100%;
            margin: 0 auto;
            padding: 0.5in;
            box-sizing: border-box;
            color: #111;
            font-family: ui-sans-serif, system-ui, -apple-system, Segoe UI, Roboto, Helvetica, Arial, "Apple Color Emoji", "Segoe UI Emoji";
          }

          .print-header {
            display: flex;
            flex-direction: column;
            align-items: center;
            padding-bottom: 10px;
            border-bottom: 1px solid #e5e7eb;
            margin-bottom: 14px;
          }
          .print-title {
            font-size: 20px;
            font-weight: 700;
            line-height: 1.2;
            letter-spacing: 0.04em;
            text-transform: uppercase;
          }
          .print-subtitle {
            font-size: 12px;
            color: #4b5563;
            margin-top: 2px;
          }
          .print-meta { display: none; }

          .print-header-lines {
            display: none;
          }
          .print-line {
            display: grid;
            grid-template-columns: 140px 1fr;
            gap: 8px;
            align-items: baseline;
          }
          .print-line-label {
            font-weight: 600;
            color: #111827;
          }
          .print-line-value {
            border-bottom: 1px solid #9ca3af;
            min-height: 14px;
          }

          .print-section {
            border: none;
            border-radius: 0;
            padding: 0;
            margin: 12px 0;
          }
          .print-section-plain {
            margin: 8px 0;
          }
          .print-section-title {
            display: inline-block;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.04em;
            text-transform: uppercase;
            color: #ffffff;
            background: #111827;
            padding: 4px 8px;
            border-radius: 3px;
            margin-bottom: 10px;
          }

          /* 2-column fields grid (business report feel) */
          .print-fields-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 8px 22px;
          }
          .print-field {
            display: grid;
            grid-template-columns: 140px 1fr;
            gap: 10px;
            align-items: baseline;
            font-size: 12px;
            padding: 3px 0;
            border-bottom: none;
          }
          .print-field-label {
            font-weight: 600;
            color: #111827;
            padding-left: 4px;
          }
          .print-field-value {
            color: #111;
            white-space: pre-wrap;
            word-break: break-word;
          }

          /* Flow container for nested content (avoid flex blocking columns). */
          .print-flow { display: block; }

          /* Report body: single column; field tables already do 4 columns. */
          .print-report-body {
            display: block;
          }
          .print-report-body > * { break-inside: avoid; }

          /* Printed page header: label left, id right (no "Print"). */
          .print-page-header { display: none; }

          /* Field table: 4 fixed columns (L,V,L,V) */
          .print-field-table { width: 100%; table-layout: fixed; }
          .print-field-th { width: 20%; }
          .print-field-td { width: 30%; }
          .print-field-td { word-break: break-word; white-space: pre-wrap; }
          .print-field-th { padding-left: 4px; }
          .print-img-list { display: flex; flex-wrap: wrap; gap: 6px; align-items: flex-start; }
          .print-img { max-width: 100%; height: 180px; object-fit: contain; border: 1px solid #e5e7eb; background: #fff; }
          .print-debug { font-size: 11px; padding: 6px 8px; border: 1px dashed #cbd5e1; margin: 6px 0; background: #f8fafc; }
          .print-memo-blocks { margin-top: 10px; }
          .print-memo-block { border: 1px solid #d1d5db; }
          .print-memo-label {
            padding: 6px 8px;
            border-bottom: 1px solid #d1d5db;
            background: #f3f4f6;
            font-weight: 700;
            font-size: 11px;
          }
          .print-memo-value { padding: 8px; white-space: pre-wrap; word-break: break-word; font-size: 11px; line-height: 1.35; }
          .print-page .print-images-grid {
            margin-top: 10px;
            display: grid !important;
            width: 100% !important;
            max-width: 100% !important;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 10px;
            grid-auto-flow: row dense;
          }
          .print-page .print-images-grid > .print-image-block { border: 1px solid #d1d5db; padding: 8px; min-width: 0; grid-column: span 1; }
          /* If total images is odd, last one spans full row */
          .print-page .print-images-grid > .print-image-block:last-child:nth-child(odd) { grid-column: 1 / -1; }
          .print-image-label {
            font-weight: 700;
            font-size: 11px;
            background: #f3f4f6;
            border: 1px solid #d1d5db;
            padding: 6px 8px;
            margin: -8px -8px 8px -8px;
          }
          .print-image-body { }
          .print-image-body .print-img { width: 100%; height: 220px; max-height: none; }

          /* Grid (table) full-width */
          .print-grid { margin: 14px 0; }
          .print-subgrid {
            margin-left: 18px;
            padding-left: 10px;
            border-left: 2px solid #e5e7eb;
          }
          .print-subgrid .print-grid-title { background: #374151; }
          .print-subgrid-tr .print-subgrid-td { border-top: none; padding-top: 4px; }
          .print-subgrid-stack { margin: 4px 0 2px 0; padding-left: 10px; border-left: 2px solid #cbd5e1; }
          .print-subgrid-block { margin: 10px 0; }
          .print-subgrid-title {
            font-size: 10px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.03em;
            margin-bottom: 4px;
            color: #374151;
          }
          .print-subgrid-table { font-size: 10px; }
          .print-img-list--thumb { gap: 4px; align-items: center; }
          .print-img-thumb { height: 44px !important; max-width: 72px !important; width: auto !important; object-fit: contain; }
          .print-grid-title {
            display: inline-block;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.04em;
            text-transform: uppercase;
            color: #ffffff;
            background: #111827;
            padding: 4px 8px;
            border-radius: 3px;
            margin-bottom: 6px;
          }
          .print-table-wrap { overflow: visible; }
          .print-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 11px;
            /* Prevent global app styles from converting table layout to blocks */
            display: table;
          }
          .print-table tr { display: table-row; }
          .print-table th,
          .print-table td { display: table-cell !important; }
          .print-th {
            text-align: left;
            padding: 6px 8px;
            border: 1px solid #d1d5db;
            background: #f3f4f6;
            font-weight: 700;
          }
          .print-td {
            padding: 6px 8px;
            border: 1px solid #d1d5db;
            vertical-align: top;
          }
          .print-empty { color: #6b7280; }

          @media print {
            .print-page { padding: 0; width: auto; }
            .print-section { break-inside: avoid; }
            .print-grid { break-inside: avoid; }
            .print-table-wrap { overflow: visible; }
            .print-image-body .print-img { height: 200px; }
            .print-img-thumb { height: 40px !important; max-width: 64px !important; }
          }

          /* Responsive tweaks for on-screen viewing only. */
          @media screen and (max-width: 720px) {
            .print-page { padding: 16px; }
            .print-fields-grid { grid-template-columns: 1fr; }
            .print-field { grid-template-columns: 130px 1fr; }
            .print-report-body { display: block; }
            .print-field-th { width: 40%; }
            .print-field-td { width: 60%; }
            .print-page-header { display: none; }
            /* Keep images 2 columns on normal screens */
            .print-page .print-images-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
          }
          @media screen and (max-width: 420px) {
            .print-page .print-images-grid { grid-template-columns: 1fr; }
          }
        `}
      </style>

      <div className="no-print px-4 py-3 border-b flex items-center justify-between">
        <div className="text-sm font-semibold">
          {transactionExDto?.TransactionName || formData?.TransactionName || 'Print'}
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className="px-3 py-1.5 text-sm border rounded"
            onClick={() => window.print()}
            title="Print"
          >
            Print
          </button>
          <button
            type="button"
            className="px-3 py-1.5 text-sm border rounded"
            onClick={() => window.close()}
            title="Close"
          >
            Close
          </button>
        </div>
      </div>

      <div className="print-page">
        {isLoading ? (
          <div className="text-sm text-gray-500">Loading print form...</div>
        ) : !transactionExDto || !formData ? (
          <div className="text-sm text-gray-500">No print data.</div>
        ) : layoutItems.length === 0 ? (
          <div className="text-sm text-gray-500">No print layout.</div>
        ) : (
          <>
            <div className="print-report-body">
              {layoutItems.map((row: any, idx: number) => (
                <PrintLayoutItem
                  key={row?.Id ?? `row-${idx}`}
                  item={row}
                  transactionExDto={transactionExDto}
                  formData={formData}
                  layoutItemTypeEnum={layoutItemTypeEnum}
                  controlTypeEnum={controlTypeEnum}
                  sessionId={sessionId}
                  printFormStructure={printFormStructure}
                  debug={debug}
                />
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default FormMasterDetailPrint;

