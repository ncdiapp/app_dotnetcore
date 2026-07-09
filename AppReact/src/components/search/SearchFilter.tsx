import React, { useCallback, useEffect, useMemo, useState } from "react";
import { ComboBox, MultiSelect } from "@mescius/wijmo.react.input";
import * as wjInput from "@mescius/wijmo.input";
import { useTheme } from "../../redux/hooks/useTheme";

interface SearchFilterProps {
  searchDto: any;
  onCriteriaValueChanged?: (searchFieldId: string, value: any) => void;
  dictDcuValue?: { [key: string]: any };
  dictFieldEntityDataSource?: { [key: string]: any };
  // Parent will call this getter on SEARCH click to pull the latest raw TEXT input.
  onRegisterTextCriteriaOverrides?: (getter: () => Record<string, any>) => void;
  // Fired when the top-level "Clear" button is clicked.
  clearSignal?: number;
}

const EmAppCriteriaType = Object.freeze({
  Text: 0,
  Entity: 1,
  Date: 2,
  Numeric: 3,
  Boolean: 4,
  Media: 5,
  Integer: 6,
});

const MIN_COLUMN_WIDTH = 220;

const normalizeStringArray = (arr: any[] | undefined | null) =>
  (arr ?? []).map((v) => String(v));

const areStringArraysEqualIgnoringOrder = (a: any[] | undefined | null, b: any[] | undefined | null) => {
  const aa = normalizeStringArray(a).slice().sort();
  const bb = normalizeStringArray(b).slice().sort();
  if (aa.length !== bb.length) return false;
  for (let i = 0; i < aa.length; i++) {
    if (aa[i] !== bb[i]) return false;
  }
  return true;
};

const EntityMultiSelectValue: React.FC<{
  criteria: any;
  selection: any[];
  inputBoxClass: string;
  buttonClass: string;
  onSelectionChange: (newSelection: string[]) => void;
}> = ({ criteria, selection, inputBoxClass, buttonClass, onSelectionChange }) => {
  const [collapsed, setCollapsed] = useState<boolean>(false);
  const allowMultipleSelect = criteria?.IsAllowMultipleSelect === true;
  const effectiveSelection = allowMultipleSelect
    ? selection
    : // Angular: if IsAllowMultipleSelect=false, keep only the latest picked item.
      (selection?.slice(-1) ?? []);

  // IMPORTANT: hooks must be called unconditionally; this component has an early return below.
  const itemsSource = useMemo(
    () =>
      (criteria.ItemsSource ?? []).map((item: any) => ({
        ...item,
        Id: String(item.Id),
      })),
    [criteria.ItemsSource],
  );

  const selectedItems = useMemo(() => {
    const selectionSet = new Set(normalizeStringArray(effectiveSelection));
    return itemsSource.filter((item: any) => selectionSet.has(String(item.Id)));
  }, [itemsSource, effectiveSelection]);

  if (criteria?.IsOptionFilterChecklist) {
    const items = (criteria.ItemsSource ?? []).map((item: any) => ({
      ...item,
      Id: String(item.Id),
      Display: String(item.Display ?? item.Id ?? ''),
    }));
    const selectedSet = new Set(normalizeStringArray(selection));
    const allSelected = items.length > 0 && items.every((x: any) => selectedSet.has(String(x.Id)));
    return (
      <div className={`w-full min-w-[180px] border rounded overflow-hidden ${inputBoxClass}`}>
        <button
          type="button"
          className="w-full px-2 py-1 text-[11px] font-semibold flex items-center justify-between bg-gray-100"
          onClick={() => setCollapsed((v) => !v)}
        >
          <span className="truncate">{String(criteria?.Display ?? '')}</span>
          <i className={`fa-solid fa-chevron-${collapsed ? 'right' : 'down'} text-[10px]`} aria-hidden="true" />
        </button>
        {!collapsed && (
          <div className="p-2">
            <div className="flex gap-2 mb-2">
              <button
                type="button"
                className={buttonClass}
                onClick={() => onSelectionChange(items.map((x: any) => String(x.Id)))}
                disabled={!items.length}
              >
                Select All
              </button>
              <button
                type="button"
                className={buttonClass}
                onClick={() => onSelectionChange([])}
                disabled={!selectedSet.size}
              >
                Clear
              </button>
            </div>
            <div className="space-y-1">
              {items.map((item: any) => {
                const checked = selectedSet.has(String(item.Id));
                return (
                  <label key={String(item.Id)} className="flex items-center gap-2 cursor-pointer text-[11px]">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(e) => {
                        const on = e.target.checked;
                        const next = on
                          ? Array.from(selectedSet).concat(String(item.Id))
                          : Array.from(selectedSet).filter((x) => x !== String(item.Id));
                        onSelectionChange(next.map(String));
                      }}
                    />
                    <span className="truncate">{item.Display}</span>
                  </label>
                );
              })}
            </div>
          </div>
        )}
      </div>
    );
  }

  const enforceSingleSelectionIfNeeded = (sender: wjInput.MultiSelect) => {
    // Angular behavior: when multi-select is disabled, keep only the last selected item.
    if (allowMultipleSelect) return sender.checkedItems ?? [];
    const checked = sender.checkedItems ?? [];
    // IMPORTANT: when clearing, MultiSelect may still have `selectedItem` stale.
    // If checked items are empty, we must return empty to allow Clear to work.
    if (!checked.length) return [];

    const selectedItem = (sender as any).selectedItem;
    if (selectedItem) return [selectedItem];

    return [checked[checked.length - 1]];
  };

  return (
    <MultiSelect
      itemsSource={itemsSource}
      displayMemberPath="Display"
      selectedValuePath="Id"
      checkedItems={selectedItems}
      placeholder={`Select ${criteria.Display}`}
      disabled={criteria.IsReadOnly === true}
      className={`w-full border text-[11px] ${inputBoxClass}`}
      maxDropDownHeight={220}
      showDropDownButton={true}
      showSelectAllCheckbox={allowMultipleSelect}
      checkedItemsChanged={(sender: wjInput.MultiSelect) => {
        const nextCheckedItems = enforceSingleSelectionIfNeeded(sender);
        const newSelection = (nextCheckedItems ?? []).map((item: any) => String(item.Id));
        onSelectionChange(newSelection);
      }}
    />
  );
};

export const SearchFilter: React.FC<SearchFilterProps> = ({
  searchDto,
  onCriteriaValueChanged,
  onRegisterTextCriteriaOverrides,
  clearSignal,
}) => {
  const { theme, t } = useTheme();
  const { CriteriasRowCount = 0, Criterias = [] } = searchDto ?? {};

  const [operatorSelections, setOperatorSelections] = useState<Record<string, string>>({});
  const [valueSelections, setValueSelections] = useState<Record<string, any[]>>({});
  const [textInputs, setTextInputs] = useState<Record<string, string>>({});
  const lastHandledClearSignalRef = React.useRef<number | undefined>(undefined);

  useEffect(() => {
    const operatorMap: Record<string, string> = {};
    const valueMap: Record<string, any[]> = {};
    const textMap: Record<string, string> = {};
    (Criterias || []).forEach((criteria: any) => {
      const opType = criteria?.CriteriaOperator?.OperatorType;
      operatorMap[criteria.SearcDCUID] =
        opType === 0 || opType ? String(opType) : "";
      valueMap[criteria.SearcDCUID] = Array.isArray(criteria?.Values)
        ? [...criteria.Values]
        : [];

      if (criteria.CriteriaType === EmAppCriteriaType.Text) {
        const values = Array.isArray(criteria?.Values) ? criteria.Values : [];
        textMap[criteria.SearcDCUID] = values.map((v: any) => String(v)).join(",");
      }
    });
    setOperatorSelections(operatorMap);
    setValueSelections(valueMap);
    setTextInputs(textMap);
  }, [searchDto, Criterias]);

  const rows = useMemo(
    () => Array.from({ length: Math.max(CriteriasRowCount, 1) }, (_, idx) => idx + 1),
    [CriteriasRowCount]
  );

  const columnCount = useMemo(() => {
    const indices = (Criterias || []).map((criteria: any) => criteria?.ColumnIndex || 1);
    return indices.length ? Math.max(...indices) : 1;
  }, [Criterias]);

  const cols = useMemo(
    () => Array.from({ length: columnCount }, (_, idx) => idx + 1),
    [columnCount]
  );

  const rowColumnMap = useMemo(() => {
    const map: Record<string, any> = {};
    (Criterias || []).forEach((criteria: any) => {
      const key = `${criteria.RowIndex}_${criteria.ColumnIndex}`;
      map[key] = criteria;
    });
    return map;
  }, [Criterias]);

  const handleOperatorChange = useCallback(
    (criteria: any, operatorValue: string) => {
      setOperatorSelections((prev) => ({
        ...prev,
        [criteria.SearcDCUID]: operatorValue,
      }));

      const selectedOperator = criteria.SupportedOperators?.find(
        (op: any) => String(op.OperatorType) === operatorValue
      );

      if (criteria.CriteriaType === EmAppCriteriaType.Text) {
        onCriteriaValueChanged?.(criteria.SearcDCUID, {
          operator: selectedOperator ?? null,
          valuesText: textInputs[criteria.SearcDCUID] ?? "",
        });
      } else {
        onCriteriaValueChanged?.(criteria.SearcDCUID, {
          operator: selectedOperator ?? null,
          values: valueSelections[criteria.SearcDCUID] ?? [],
        });
      }
    },
    [onCriteriaValueChanged, valueSelections, textInputs]
  );

  const handleValueChange = useCallback(
    (criteria: any, newValues: any[]) => {
      const current = valueSelections[criteria.SearcDCUID] ?? [];
      // Prevent controlled-component feedback loops (MultiSelect can emit redundant change events).
      if (areStringArraysEqualIgnoringOrder(current, newValues)) return;

      setValueSelections((prev) => ({
        ...prev,
        [criteria.SearcDCUID]: newValues,
      }));

      // Keep operator+values together (so executeSearch can build the same payload as Angular).
      const operatorSelection = operatorSelections[criteria.SearcDCUID] ?? "";
      const selectedOperator =
        operatorSelection === ""
          ? null
          : criteria.SupportedOperators?.find(
              (op: any) => String(op.OperatorType) === operatorSelection
            ) ?? null;

      onCriteriaValueChanged?.(criteria.SearcDCUID, {
        operator: selectedOperator ?? null,
        values: newValues,
      });
    },
    [onCriteriaValueChanged, operatorSelections, valueSelections]
  );

  const renderValueControl = useCallback(
    (criteria: any) => {
      const selection = valueSelections[criteria.SearcDCUID] ?? [];
      const commonInputClass = `w-full rounded border px-2 py-1 text-[11px] ${theme.inputBox}`;

      if (criteria.CriteriaType === EmAppCriteriaType.Entity) {
        return (
          <EntityMultiSelectValue
            criteria={criteria}
            selection={selection}
            inputBoxClass={theme.inputBox}
            buttonClass={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
            onSelectionChange={(newSelection) => handleValueChange(criteria, newSelection)}
          />
        );
      }

      if (criteria.CriteriaType === EmAppCriteriaType.Date) {
        const value = selection[0] ?? "";
        return (
          <input
            type="date"
            value={value}
            disabled={criteria.IsReadOnly}
            onChange={(event) => handleValueChange(criteria, [event.target.value])}
            className={commonInputClass}
          />
        );
      }

      if (criteria.CriteriaType === EmAppCriteriaType.Numeric || criteria.CriteriaType === EmAppCriteriaType.Integer) {
        const value = selection[0] ?? "";
        return (
          <input
            type="number"
            value={value}
            disabled={criteria.IsReadOnly}
            onChange={(event) => handleValueChange(criteria, [event.target.value])}
            className={commonInputClass}
          />
        );
      }

      if (criteria.CriteriaType === EmAppCriteriaType.Boolean) {
        const currentValue = selection[0] ?? "";
        const booleanOptions = [
          { id: "", label: "Any" },
          { id: "true", label: "True" },
          { id: "false", label: "False" },
        ];
        return (
          <ComboBox
            itemsSource={booleanOptions}
            displayMemberPath="label"
            selectedValuePath="id"
            selectedValue={currentValue}
            isReadOnly={criteria.IsReadOnly}
            className={commonInputClass}
            isEditable={false}
            placeholder="Any"
            showDropDownButton={true}
            selectedIndexChanged={(sender: wjInput.ComboBox) =>
              handleValueChange(criteria, sender.selectedValue ? [String(sender.selectedValue)] : [])
            }
          />
        );
      }

      if (criteria.CriteriaType === EmAppCriteriaType.Text) {
        const rawValue = textInputs[criteria.SearcDCUID] ?? "";
        return (
          <input
            type="text"
            value={rawValue}
            disabled={criteria.IsReadOnly}
            onChange={(event) => {
              // Do not parse/sync Values[] on every keystroke.
              setTextInputs((prev) => ({
                ...prev,
                [criteria.SearcDCUID]: event.target.value,
              }));
            }}
            className={commonInputClass}
          />
        );
      }

      const value = selection[0] ?? "";
      return (
        <input
          type="text"
          value={value}
          disabled={criteria.IsReadOnly}
          onChange={(event) => handleValueChange(criteria, [event.target.value])}
          className={commonInputClass}
        />
      );
    },
    [handleValueChange, theme.inputBox, valueSelections, textInputs]
  );

  // Expose latest raw TEXT input overrides for parent to parse on SEARCH click.
  const getTextCriteriaOverrides = useCallback(() => {
    const overrides: Record<string, any> = {};
    (Criterias || []).forEach((criteria: any) => {
      if (criteria.CriteriaType !== EmAppCriteriaType.Text) return;
      const ducId = criteria.SearcDCUID;
      if (ducId === undefined || ducId === null) return;

      const operatorSelection = operatorSelections[ducId] ?? "";
      const selectedOperator =
        operatorSelection === ""
          ? null
          : criteria.SupportedOperators?.find(
              (op: any) => String(op.OperatorType) === operatorSelection
            ) ?? null;

      overrides[ducId] = {
        operator: selectedOperator ?? null,
        valuesText: textInputs[ducId] ?? "",
      };
    });
    return overrides;
  }, [Criterias, operatorSelections, textInputs]);

  useEffect(() => {
    onRegisterTextCriteriaOverrides?.(getTextCriteriaOverrides);
  }, [onRegisterTextCriteriaOverrides, getTextCriteriaOverrides]);

  // Clear criteria values (match Angular clearCriteriaValues behavior).
  useEffect(() => {
    if (clearSignal === undefined) return;
    // Avoid running clear logic on initial mount/remount (e.g. collapse->expand),
    // run only when clearSignal actually changes.
    if (lastHandledClearSignalRef.current === undefined) {
      lastHandledClearSignalRef.current = clearSignal;
      return;
    }
    if (lastHandledClearSignalRef.current === clearSignal) return;
    lastHandledClearSignalRef.current = clearSignal;

    const newValueMap: Record<string, any[]> = {};
    const newTextMap: Record<string, string> = {};
    const newOperatorMap: Record<string, string> = {};
    (Criterias || []).forEach((criteria: any) => {
      const ducId = criteria?.SearcDCUID;
      if (ducId === undefined || ducId === null) return;

      newOperatorMap[ducId] = "";
      if (criteria.CriteriaType === EmAppCriteriaType.Text) {
        newTextMap[ducId] = "";
        // Keep valueSelections empty for Text as well.
        newValueMap[ducId] = [];
      } else {
        newValueMap[ducId] = [];
      }
    });

    setValueSelections(newValueMap);
    setOperatorSelections(newOperatorMap);
    setTextInputs((prev) => ({
      ...prev,
      ...newTextMap,
    }));

    // Angular behavior: clear should force criteria.Values to empty in the outgoing payload.
    // Because AppSearch uses `dictDcuValue` overrides, we must send empty overrides too.
    if (onCriteriaValueChanged) {
      (Criterias || []).forEach((criteria: any) => {
        const ducId = criteria?.SearcDCUID;
        if (ducId === undefined || ducId === null) return;

        if (criteria.CriteriaType === EmAppCriteriaType.Text) {
          onCriteriaValueChanged(ducId, {
            operator: null,
            valuesText: "",
          });
        } else {
          onCriteriaValueChanged(ducId, {
            operator: null,
            values: [],
          });
        }
      });
    }
  }, [clearSignal, Criterias]);

  const renderCriteriaCard = useCallback(
    (criteria: any) => {
      const operatorSelection = operatorSelections[criteria.SearcDCUID] ?? "";
      const hasOperators = Array.isArray(criteria.SupportedOperators) && criteria.SupportedOperators.length > 0;

      return (
        <div
          key={criteria.SearcDCUID}
          className={`flex items-center gap-2 rounded-md px-3 py-0`}
        >
          {!criteria?.IsOptionFilterChecklist && (
            <div className="flex min-w-[120px] items-center text-[11px] font-semibold tracking-wide opacity-80">
              {criteria.Display}
            </div>
          )}
          {hasOperators ? (
            <ComboBox
              itemsSource={criteria.SupportedOperators}
              displayMemberPath="Display"
              selectedValuePath="OperatorType"
              selectedValue={operatorSelection === "" ? null : Number(operatorSelection)}
              isEditable={false}
              isReadOnly={criteria.IsReadOnly}
              placeholder="Operator"
              className={`w-28 border text-[11px] ${theme.inputBox}`}
              showDropDownButton={true}
              selectedIndexChanged={(sender: wjInput.ComboBox) =>
                handleOperatorChange(
                  criteria,
                  sender.selectedValue !== null && sender.selectedValue !== undefined
                    ? String(sender.selectedValue)
                    : ""
                )
              }
            />
          ) : (
            <></>
          )}
          <div className="w-1 flex-auto min-w-[160px]">
            {renderValueControl(criteria)}
          </div>
        </div>
      );
    },
    [handleOperatorChange, operatorSelections, renderValueControl, t, theme.mainContentSection, theme.inputBox]
  );

  const gridStyle = useMemo(
    () => ({
      gridTemplateColumns: `repeat(${columnCount}, minmax(${MIN_COLUMN_WIDTH}px, 1fr))`,
    }),
    [columnCount]
  );

  return (
    Criterias.length === 0 ? (<></>) : (
      
        <div className={`w-full overflow-x-auto mb-1 ${theme.mainContentSection}`}>
          <div className="space-y-3 p-4">
            {Criterias.length === 0 ? (
              <div className="rounded-md border px-4 py-6 text-center text-xs opacity-70">
                No criteria available for this search.
              </div>
            ) : (
              rows.map((row) => (
                <div
                  key={`row-${row}`}
                  className="grid gap-3"
                  style={gridStyle}
                >
                  {cols.map((col) => {
                    const criteria = rowColumnMap[`${row}_${col}`];
                    if (!criteria) {
                      return (
                        <div key={`placeholder-${row}-${col}`} className="hidden md:block md:invisible" />
                      );
                    }
                    return renderCriteriaCard(criteria);
                  })}
                </div>
              ))
            )}
          </div>
        </div>
     
    )
  );
};