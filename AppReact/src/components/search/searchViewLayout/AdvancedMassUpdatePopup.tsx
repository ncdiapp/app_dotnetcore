/**
 * Advanced Mass Update popup — Angular AdvancedMassUpdatePopup parity
 * (SearchViewLayout.cshtml + massupdated_openAdvancedMassUpdatePopup).
 *
 * Find And Replace: set Update Column = Replace where value equals Find (and optional condition).
 * Replace All: set Update Column = Replace on all matching rows (ignores Find).
 */
import React, { useEffect, useMemo, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import type * as wjInput from '@mescius/wijmo.input';
import { useTheme } from '../../../redux/hooks/useTheme';
import { searchSvc } from '../../../webapi/searchSvc';

/** EmAppCriteriaOperatorType subset used for Advanced Update condition. */
const CONDITION_OPERATORS: { Id: number; Display: string }[] = [
  { Id: 0, Display: 'Equals' },
  { Id: 13, Display: 'Not Equal' },
  { Id: 1, Display: 'Null' },
  { Id: 2, Display: 'Not Null' },
  { Id: 3, Display: 'Greater Than' },
  { Id: 4, Display: 'Greater Than Or Equals' },
  { Id: 5, Display: 'Less Than' },
  { Id: 6, Display: 'Less Than Or Equals' },
  { Id: 7, Display: 'Like' },
  { Id: 10, Display: 'Start With' },
  { Id: 11, Display: 'End With' },
];

const EmAppControlType = {
  DDL: 1,
  TextBox: 2,
  CheckBox: 13,
  Date: 7,
  Numeric: 20,
  DateTimeDetail: 27,
};

export interface AdvancedMassUpdateApplyParams {
  updateColumnId: number;
  findValue: any;
  replaceValue: any;
  isFilterBySelectedRows: boolean;
  conditionColumnId: number | null;
  conditionOperator: number | null;
  conditionValue: any;
  /** When true, only rows whose Update Column equals FindValue are updated. */
  matchFindValue: boolean;
}

interface AdvancedMassUpdatePopupProps {
  isOpen: boolean;
  viewId?: string | number | null;
  columns: any[];
  dictEntityLookupItemDto?: Record<string, any[]> | null;
  onClose: () => void;
  onApply: (params: AdvancedMassUpdateApplyParams) => void;
}

function columnLabel(col: any): string {
  return col?.Name || col?.DisplayName || col?.DisplayText || `Column ${col?.Id}`;
}

function withDisplayName(col: any): any {
  if (!col) return col;
  return { ...col, Name: columnLabel(col) };
}

function getLookupItems(
  column: any | null | undefined,
  dict: Record<string, any[]> | null | undefined,
): any[] {
  if (!column) return [];
  if (Array.isArray(column.DataSource) && column.DataSource.length > 0) {
    return column.DataSource;
  }
  const entityId = column.EntityId;
  if (entityId == null || !dict) return [];
  return dict[String(entityId)] || dict[entityId] || [];
}

function AdvancedUpdateFieldInput({
  column,
  value,
  onChange,
  dictEntityLookupItemDto,
  inputClass,
}: {
  column: any | null;
  value: any;
  onChange: (v: any) => void;
  dictEntityLookupItemDto?: Record<string, any[]> | null;
  inputClass: string;
}) {
  if (!column) {
    return <input type="text" className={inputClass} disabled value="" readOnly />;
  }

  const controlType = Number(column.ControlType);

  if (controlType === EmAppControlType.CheckBox) {
    return (
      <input
        type="checkbox"
        checked={value === true || value === 'true' || value === 1 || value === '1'}
        onChange={(e) => onChange(e.target.checked)}
      />
    );
  }

  if (controlType === EmAppControlType.DDL) {
    const items = [{ Id: null, Display: '' }, ...getLookupItems(column, dictEntityLookupItemDto)];
    return (
      <ComboBox
        itemsSource={items}
        displayMemberPath="Display"
        selectedValuePath="Id"
        selectedValue={value ?? null}
        isEditable={false}
        className={inputClass}
        style={{ width: '100%' }}
        selectedIndexChanged={(sender: wjInput.ComboBox) => {
          if (!sender?.containsFocus?.()) return;
          onChange(sender.selectedValue);
        }}
      />
    );
  }

  if (controlType === EmAppControlType.Numeric) {
    return (
      <input
        type="number"
        className={inputClass}
        value={value ?? ''}
        onChange={(e) => {
          const raw = e.target.value;
          onChange(raw === '' ? null : Number(raw));
        }}
      />
    );
  }

  if (controlType === EmAppControlType.Date || controlType === EmAppControlType.DateTimeDetail) {
    return (
      <input
        type="date"
        className={inputClass}
        value={value ? String(value).slice(0, 10) : ''}
        onChange={(e) => onChange(e.target.value || null)}
      />
    );
  }

  return (
    <input
      type="text"
      className={inputClass}
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

export const AdvancedMassUpdatePopup: React.FC<AdvancedMassUpdatePopupProps> = ({
  isOpen,
  viewId,
  columns,
  dictEntityLookupItemDto,
  onClose,
  onApply,
}) => {
  const { theme } = useTheme();
  const [updateColumnId, setUpdateColumnId] = useState<number | null>(null);
  const [findValue, setFindValue] = useState<any>(null);
  const [replaceValue, setReplaceValue] = useState<any>(null);
  const [isFilterBySelectedRows, setIsFilterBySelectedRows] = useState(false);
  const [conditionColumnId, setConditionColumnId] = useState<number | null>(null);
  const [conditionOperator, setConditionOperator] = useState<number | null>(0);
  const [conditionValue, setConditionValue] = useState<any>(null);
  const [loadedLookup, setLoadedLookup] = useState<Record<string, any[]> | null>(null);

  const effectiveLookup = useMemo(() => {
    if (dictEntityLookupItemDto && Object.keys(dictEntityLookupItemDto).length > 0) {
      return dictEntityLookupItemDto;
    }
    return loadedLookup;
  }, [dictEntityLookupItemDto, loadedLookup]);

  useEffect(() => {
    if (!isOpen || !viewId) return;
    if (dictEntityLookupItemDto && Object.keys(dictEntityLookupItemDto).length > 0) return;
    let cancelled = false;
    searchSvc.retrieveViewDictEntityLookupItemDto(String(viewId))
      .then((data: any) => {
        if (!cancelled && data && typeof data === 'object') {
          setLoadedLookup(data);
        }
      })
      .catch(() => {
        if (!cancelled) setLoadedLookup(null);
      });
    return () => { cancelled = true; };
  }, [isOpen, viewId, dictEntityLookupItemDto]);

  const updateColumns = useMemo(() => {
    const list = Array.isArray(columns) ? columns : [];
    return list
      .filter(
        (c) =>
          c?.IsVisible !== false &&
          c?.IsMassUpdateReadonly !== true &&
          Number(c?.ControlType) !== 5 && // Image
          Number(c?.ControlType) !== 10, // Label
      )
      .map(withDisplayName);
  }, [columns]);

  const conditionColumns = useMemo(() => {
    const list = Array.isArray(columns) ? columns : [];
    return [{ Id: null, Name: '' }, ...list.filter((c) => c?.IsVisible !== false).map(withDisplayName)];
  }, [columns]);

  const updateField = useMemo(
    () => updateColumns.find((c) => Number(c.Id) === Number(updateColumnId)) ?? null,
    [updateColumns, updateColumnId],
  );

  const conditionField = useMemo(
    () =>
      conditionColumnId == null
        ? null
        : (columns || []).find((c: any) => Number(c.Id) === Number(conditionColumnId)) ?? null,
    [columns, conditionColumnId],
  );

  useEffect(() => {
    if (!isOpen) return;
    setFindValue(null);
    setReplaceValue(null);
    setConditionValue(null);
    setIsFilterBySelectedRows(false);
    setConditionOperator(0);
    if (updateColumns.length > 0) {
      setUpdateColumnId(Number(updateColumns[0].Id));
    } else {
      setUpdateColumnId(null);
    }
    setConditionColumnId(null);
  }, [isOpen, updateColumns]);

  useEffect(() => {
    setFindValue(null);
    setReplaceValue(null);
  }, [updateColumnId]);

  useEffect(() => {
    setConditionValue(null);
  }, [conditionColumnId]);

  if (!isOpen) return null;

  const fieldClass = `w-full h-7 px-2 text-xs border box-border ${theme.inputBox}`;
  const labelClass = `w-40 shrink-0 text-xs ${theme.label}`;
  const fieldWrapClass = 'w-1 flex-auto min-w-0';

  const buildParams = (matchFindValue: boolean): AdvancedMassUpdateApplyParams | null => {
    if (updateColumnId == null) return null;
    return {
      updateColumnId: Number(updateColumnId),
      findValue,
      replaceValue,
      isFilterBySelectedRows,
      conditionColumnId: conditionColumnId == null ? null : Number(conditionColumnId),
      conditionOperator: conditionOperator == null ? null : Number(conditionOperator),
      conditionValue,
      matchFindValue,
    };
  };

  return (
    <div
      className="fixed inset-0 z-[1200] flex items-center justify-center bg-black/40"
      role="dialog"
      aria-modal="true"
      aria-labelledby="advanced-mass-update-title"
      onClick={(e) => e.stopPropagation()}
    >
      <div
        className={`w-[500px] max-w-[95vw] h-[600px] max-h-[90vh] flex flex-col rounded-md overflow-hidden shadow-lg ${theme.mainContentSection}`}
      >
        <div className={`shrink-0 flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <div id="advanced-mass-update-title" className={`text-sm font-semibold ${theme.title}`}>
            Advanced Mass Update
          </div>
          <button
            type="button"
            onClick={onClose}
            className={`p-1 rounded ${theme.button_default}`}
            aria-label="Close"
            title="Close"
          >
            <i className="fa-solid fa-xmark" aria-hidden />
          </button>
        </div>

        <div className="min-h-0 h-1 flex-auto overflow-auto p-3 space-y-3 text-xs">
          <div className={`rounded border p-2 space-y-2 ${theme.mainContentSection}`}>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Update Column</label>
              <div className={fieldWrapClass}>
                <ComboBox
                  itemsSource={updateColumns}
                  displayMemberPath="Name"
                  selectedValuePath="Id"
                  selectedValue={updateColumnId}
                  isEditable={false}
                  className={fieldClass}
                  style={{ width: '100%' }}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    if (!sender?.containsFocus?.()) return;
                    const v = sender.selectedValue;
                    setUpdateColumnId(v == null || v === '' ? null : Number(v));
                  }}
                />
              </div>
            </div>
          </div>

          <div className={`rounded border p-2 space-y-2 ${theme.mainContentSection}`}>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Find</label>
              <div className={fieldWrapClass}>
                <AdvancedUpdateFieldInput
                  column={updateField}
                  value={findValue}
                  onChange={setFindValue}
                  dictEntityLookupItemDto={effectiveLookup}
                  inputClass={fieldClass}
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Replace</label>
              <div className={fieldWrapClass}>
                <AdvancedUpdateFieldInput
                  column={updateField}
                  value={replaceValue}
                  onChange={setReplaceValue}
                  dictEntityLookupItemDto={effectiveLookup}
                  inputClass={fieldClass}
                />
              </div>
            </div>
          </div>

          <div className={`rounded border p-2 ${theme.mainContentSection}`}>
            <label className="flex items-center gap-2 cursor-pointer">
              <span className={labelClass}>Filter By Selected Rows</span>
              <input
                type="checkbox"
                checked={isFilterBySelectedRows}
                onChange={(e) => setIsFilterBySelectedRows(e.target.checked)}
              />
            </label>
          </div>

          <div className={`rounded border p-2 space-y-2 ${theme.mainContentSection}`}>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Condition Column</label>
              <div className={fieldWrapClass}>
                <ComboBox
                  itemsSource={conditionColumns}
                  displayMemberPath="Name"
                  selectedValuePath="Id"
                  selectedValue={conditionColumnId}
                  isEditable={false}
                  className={fieldClass}
                  style={{ width: '100%' }}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    if (!sender?.containsFocus?.()) return;
                    const v = sender.selectedValue;
                    setConditionColumnId(v == null || v === '' ? null : Number(v));
                  }}
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Operator</label>
              <div className={fieldWrapClass}>
                <ComboBox
                  itemsSource={CONDITION_OPERATORS}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={conditionOperator}
                  isEditable={false}
                  className={fieldClass}
                  style={{ width: '100%' }}
                  selectedIndexChanged={(sender: wjInput.ComboBox) => {
                    if (!sender?.containsFocus?.()) return;
                    const v = sender.selectedValue;
                    setConditionOperator(v == null || v === '' ? null : Number(v));
                  }}
                />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <label className={labelClass}>Value</label>
              <div className={fieldWrapClass}>
                <AdvancedUpdateFieldInput
                  column={conditionField}
                  value={conditionValue}
                  onChange={setConditionValue}
                  dictEntityLookupItemDto={effectiveLookup}
                  inputClass={fieldClass}
                />
              </div>
            </div>
          </div>

          {updateField && (
            <div className={`${theme.label} opacity-70`}>
              Updating: {columnLabel(updateField)}
            </div>
          )}
        </div>

        <div className={`shrink-0 flex items-center justify-end gap-2 px-3 py-2 border-t ${theme.mainContentSection}`}>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => {
              const p = buildParams(true);
              if (p) onApply(p);
            }}
            disabled={updateColumnId == null}
          >
            Find And Replace
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => {
              const p = buildParams(false);
              if (p) onApply(p);
            }}
            disabled={updateColumnId == null}
          >
            Replace All
          </button>
        </div>
      </div>
    </div>
  );
};

export default AdvancedMassUpdatePopup;
