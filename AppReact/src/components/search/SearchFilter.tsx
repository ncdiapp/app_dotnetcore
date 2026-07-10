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
  // Parent will call this getter on SEARCH click to pull live criteria values (Angular ddlControl parity).
  onRegisterLiveCriteriaOverrides?: (getter: () => Record<string, any>) => void;
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

/**
 * Prefer parent dictDcuValue overrides (Angular parity); fall back to criteria.Values.
 * Ignore empty object overrides like `{ values: [] }` when criteria still has server Default Values —
 * those empties are often produced by MultiSelect before isLookupItemSelected sync (do not wipe defaults).
 */
const resolveValuesForCriteria = (
  criteria: any,
  dictDcuValue?: Record<string, any>,
  options?: { allowEmptyOverride?: boolean },
) => {
  const criteriaValues = Array.isArray(criteria?.Values) ? normalizeStringArray(criteria.Values) : [];
  const dcuId = criteria?.SearcDCUID;
  const override = dcuId != null ? dictDcuValue?.[dcuId] : undefined;
  if (override !== undefined && override !== null) {
    let overrideValues: string[] | undefined;
    if (Array.isArray(override)) {
      overrideValues = normalizeStringArray(override);
    } else if (typeof override === "object" && Array.isArray(override.values)) {
      overrideValues = normalizeStringArray(override.values);
    }
    if (overrideValues !== undefined) {
      const allowEmpty = options?.allowEmptyOverride === true;
      if (overrideValues.length > 0 || allowEmpty || criteriaValues.length === 0) {
        return overrideValues;
      }
    }
  }
  return criteriaValues;
};

const valuesEqualAsIds = (a: any, b: any) => String(a ?? "") === String(b ?? "");

/** Angular searchViewHelper.initialSearchDtoCriterias: mark isLookupItemSelected from Values. */
const applyLookupItemSelectedFlags = (items: any[], selection: any[]) => {
  const selected = normalizeStringArray(selection);
  items.forEach((lookupItem: any) => {
    lookupItem.isLookupItemSelected = false;
    if (!selected.length) return;
    for (let i = 0; i < selected.length; i++) {
      if (selected[i] && valuesEqualAsIds(selected[i], lookupItem.Id)) {
        lookupItem.isLookupItemSelected = true;
        break;
      }
    }
  });
};

const EntityMultiSelectValue: React.FC<{
  criteria: any;
  selection: any[];
  inputBoxClass: string;
  buttonClass: string;
  onSelectionChange: (newSelection: string[]) => void;
  onControlReady?: (control: wjInput.MultiSelect | null) => void;
}> = ({ criteria, selection, inputBoxClass, buttonClass, onSelectionChange, onControlReady }) => {
  const [collapsed, setCollapsed] = useState<boolean>(false);
  const multiSelectRef = React.useRef<wjInput.MultiSelect | null>(null);
  const isSyncingRef = React.useRef(false);
  const allowMultipleSelect = criteria?.IsAllowMultipleSelect === true;
  const effectiveSelection = allowMultipleSelect
    ? selection
    : // Angular: if IsAllowMultipleSelect=false, keep only the latest picked item.
      (selection?.slice(-1) ?? []);

  // Stable item objects (Angular mutates isLookupItemSelected on the same ItemsSource instances).
  // Do NOT rebuild from selection — that remounts MultiSelect and clears checked items.
  const itemsSource = useMemo(() => {
    const items = (criteria.ItemsSource ?? []).map((item: any) => ({
      ...item,
      Id: item.Id,
      isLookupItemSelected: false,
    }));
    applyLookupItemSelectedFlags(items, effectiveSelection);
    return items;
    // eslint-disable-next-line react-hooks/exhaustive-deps -- selection applied in effect below
  }, [criteria.ItemsSource]);

  const resolveCheckedItems = useCallback(
    (sender: wjInput.MultiSelect) => {
      const checked = sender.checkedItems ?? [];
      if (!checked.length) return [];

      if (allowMultipleSelect) return checked;

      const selectedItem = (sender as any).selectedItem;
      if (selectedItem) return [selectedItem];
      return [checked[checked.length - 1]];
    },
    [allowMultipleSelect],
  );

  const commitSelectionFromControl = useCallback(
    (sender: wjInput.MultiSelect) => {
      if (isSyncingRef.current) return;
      const nextCheckedItems = resolveCheckedItems(sender);
      const newSelection = (nextCheckedItems ?? []).map((item: any) => String(item.Id));
      onSelectionChange(newSelection);
    },
    [onSelectionChange, resolveCheckedItems],
  );

  const syncSelectionToControl = useCallback(
    (selectionToSync: any[]) => {
      applyLookupItemSelectedFlags(itemsSource, selectionToSync);
      const ctl = multiSelectRef.current;
      if (!ctl) return;

      const desired = itemsSource.filter((item: any) => item.isLookupItemSelected === true);
      // Defaults may not match ItemsSource yet (cascading / id mismatch). Do not clear the control.
      if (normalizeStringArray(selectionToSync).length > 0 && desired.length === 0) {
        return;
      }

      isSyncingRef.current = true;
      try {
        ctl.checkedItems = desired;
      } finally {
        // Wijmo may raise checkedItemsChanged synchronously or on next tick.
        setTimeout(() => {
          isSyncingRef.current = false;
        }, 0);
      }
    },
    [itemsSource],
  );

  useEffect(() => {
    syncSelectionToControl(effectiveSelection);
  }, [effectiveSelection, syncSelectionToControl]);

  useEffect(() => {
    return () => {
      onControlReady?.(null);
    };
  }, [onControlReady]);

  if (criteria?.IsOptionFilterChecklist) {
    const items = (criteria.ItemsSource ?? []).map((item: any) => ({
      ...item,
      Id: String(item.Id),
      Display: String(item.Display ?? item.Id ?? ''),
    }));
    const selectedSet = new Set(normalizeStringArray(selection));
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

  return (
    <MultiSelect
      initialized={(sender: wjInput.MultiSelect) => {
        multiSelectRef.current = sender;
        onControlReady?.(sender);
        syncSelectionToControl(effectiveSelection);
      }}
      itemsSource={itemsSource}
      displayMemberPath="Display"
      checkedMemberPath="isLookupItemSelected"
      placeholder={`Select ${criteria.Display}`}
      disabled={criteria.IsReadOnly === true}
      className={`w-full border text-[11px] ${inputBoxClass}`}
      maxDropDownHeight={220}
      showDropDownButton={true}
      showSelectAllCheckbox={allowMultipleSelect}
      checkedItemsChanged={(sender: wjInput.MultiSelect) => {
        commitSelectionFromControl(sender);
      }}
      isDroppedDownChanged={(sender: wjInput.MultiSelect) => {
        if (!sender.isDroppedDown) {
          commitSelectionFromControl(sender);
        }
      }}
    />
  );
};

export const SearchFilter: React.FC<SearchFilterProps> = ({
  searchDto,
  onCriteriaValueChanged,
  dictDcuValue,
  onRegisterTextCriteriaOverrides,
  onRegisterLiveCriteriaOverrides,
  clearSignal,
}) => {
  const { theme, t } = useTheme();
  const { CriteriasRowCount = 0, Criterias = [] } = searchDto ?? {};

  const [operatorSelections, setOperatorSelections] = useState<Record<string, string>>({});
  const [valueSelections, setValueSelections] = useState<Record<string, any[]>>({});
  const [textInputs, setTextInputs] = useState<Record<string, string>>({});
  const lastHandledClearSignalRef = React.useRef<number | undefined>(undefined);
  const initializedSearchIdRef = React.useRef<any>(undefined);
  const entityControlRefs = React.useRef<Record<string, wjInput.MultiSelect | null>>({});
  const valueSelectionsRef = React.useRef(valueSelections);
  const operatorSelectionsRef = React.useRef(operatorSelections);
  valueSelectionsRef.current = valueSelections;
  operatorSelectionsRef.current = operatorSelections;

  useEffect(() => {
    const searchId = searchDto?.Id;
    const isFirstLoad = initializedSearchIdRef.current === undefined;
    const isNewSearch = initializedSearchIdRef.current !== searchId;
    // Do not reset local edits when the same search re-renders (e.g. after SEARCH click).
    if (!isFirstLoad && !isNewSearch) return;
    initializedSearchIdRef.current = searchId;

    const operatorMap: Record<string, string> = {};
    const valueMap: Record<string, any[]> = {};
    const textMap: Record<string, string> = {};
    (Criterias || []).forEach((criteria: any) => {
      const opType = criteria?.CriteriaOperator?.OperatorType;
      operatorMap[criteria.SearcDCUID] =
        opType === 0 || opType ? String(opType) : "";
      valueMap[criteria.SearcDCUID] = resolveValuesForCriteria(criteria, dictDcuValue);

      if (criteria.CriteriaType === EmAppCriteriaType.Text) {
        const values = resolveValuesForCriteria(criteria, dictDcuValue);
        textMap[criteria.SearcDCUID] = values.join(",");
      }
    });
    setOperatorSelections(operatorMap);
    setValueSelections(valueMap);
    setTextInputs(textMap);
  }, [searchDto?.Id, Criterias, dictDcuValue]);

  const resolveSelectedOperator = useCallback((criteria: any, operatorSelection: string) => {
    if (operatorSelection === "") return null;
    return (
      criteria.SupportedOperators?.find(
        (op: any) => String(op.OperatorType) === operatorSelection
      ) ?? null
    );
  }, []);

  const readEntityValuesFromControl = useCallback((criteria: any): string[] | undefined => {
    const dcuId = String(criteria.SearcDCUID);
    const hasStateKey = Object.prototype.hasOwnProperty.call(valueSelectionsRef.current, dcuId);
    const stateValues = hasStateKey
      ? normalizeStringArray(valueSelectionsRef.current[dcuId] ?? [])
      : undefined;
    const ctl = entityControlRefs.current[dcuId];

    // Angular: if no ddlControl, leave criteria.Values alone (server Default Value).
    if (!ctl) {
      return stateValues;
    }

    const checked = ctl.checkedItems ?? [];
    if (checked.length > 0) {
      if (criteria?.IsAllowMultipleSelect === true) {
        return checked.map((item: any) => String(item.Id));
      }
      const selectedItem = (ctl as any).selectedItem;
      if (selectedItem) return [String(selectedItem.Id)];
      return [String(checked[checked.length - 1].Id)];
    }

    // Control empty: use React state if initialized (includes Clear → []).
    // If state not initialized yet, return undefined so search keeps criteria.Values defaults.
    return stateValues;
  }, []);

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
      const dcuId = criteria.SearcDCUID;
      let didChange = false;
      setValueSelections((prev) => {
        const current = prev[dcuId] ?? [];
        if (areStringArraysEqualIgnoringOrder(current, newValues)) return prev;
        didChange = true;
        return {
          ...prev,
          [dcuId]: newValues,
        };
      });
      if (!didChange) return;

      const operatorSelection = operatorSelectionsRef.current[dcuId] ?? "";
      const selectedOperator = resolveSelectedOperator(criteria, operatorSelection);

      onCriteriaValueChanged?.(dcuId, {
        operator: selectedOperator ?? null,
        values: newValues,
      });
    },
    [onCriteriaValueChanged, resolveSelectedOperator]
  );

  const renderValueControl = useCallback(
    (criteria: any) => {
      // First paint: valueSelections is still {} until the init effect runs.
      // Resolve defaults from criteria.Values / dictDcuValue so DDL shows Default Value immediately.
      const selection = Object.prototype.hasOwnProperty.call(valueSelections, criteria.SearcDCUID)
        ? (valueSelections[criteria.SearcDCUID] ?? [])
        : resolveValuesForCriteria(criteria, dictDcuValue);
      const commonInputClass = `w-full rounded border px-2 py-1 text-[11px] ${theme.inputBox}`;

      if (criteria.CriteriaType === EmAppCriteriaType.Entity) {
        return (
          <EntityMultiSelectValue
            criteria={criteria}
            selection={selection}
            inputBoxClass={theme.inputBox}
            buttonClass={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
            onSelectionChange={(newSelection) => handleValueChange(criteria, newSelection)}
            onControlReady={(control) => {
              const dcuId = String(criteria.SearcDCUID);
              if (control) entityControlRefs.current[dcuId] = control;
              else delete entityControlRefs.current[dcuId];
            }}
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
    [handleValueChange, theme.inputBox, valueSelections, textInputs, dictDcuValue]
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

  const getLiveCriteriaOverrides = useCallback(() => {
    const overrides: Record<string, any> = {};
    (Criterias || []).forEach((criteria: any) => {
      const dcuId = criteria?.SearcDCUID;
      if (dcuId === undefined || dcuId === null) return;

      const operatorSelection = operatorSelectionsRef.current[dcuId] ?? "";
      const selectedOperator = resolveSelectedOperator(criteria, operatorSelection);

      if (criteria.CriteriaType === EmAppCriteriaType.Text) return;

      if (criteria.CriteriaType === EmAppCriteriaType.Entity) {
        // Angular prepareSearchCriteria: only replace Entity Values when ddlControl exists
        // and we can read a definitive selection. Otherwise keep server Default Values.
        const values = readEntityValuesFromControl(criteria);
        if (values !== undefined) {
          overrides[dcuId] = {
            operator: selectedOperator ?? null,
            values,
          };
        } else if (selectedOperator) {
          overrides[dcuId] = { operator: selectedOperator };
        }
        return;
      }

      const values = normalizeStringArray(valueSelectionsRef.current[dcuId] ?? []);
      overrides[dcuId] = {
        operator: selectedOperator ?? null,
        values,
      };
    });
    return overrides;
  }, [Criterias, readEntityValuesFromControl, resolveSelectedOperator]);

  const flushAndGetLiveCriteriaOverrides = useCallback(() => {
    const overrides = getLiveCriteriaOverrides();

    setValueSelections((prev) => {
      const next = { ...prev };
      let changed = false;
      (Criterias || []).forEach((criteria: any) => {
        const dcuId = criteria?.SearcDCUID;
        if (dcuId === undefined || dcuId === null) return;
        if (criteria.CriteriaType !== EmAppCriteriaType.Entity) return;
        const override = overrides[dcuId];
        // Only sync state when we have an explicit values array (never invent empty wipe).
        if (!override || !Array.isArray(override.values)) return;
        const values = normalizeStringArray(override.values);
        if (!areStringArraysEqualIgnoringOrder(prev[dcuId], values)) {
          next[dcuId] = values;
          changed = true;
        }
      });
      return changed ? next : prev;
    });

    (Criterias || []).forEach((criteria: any) => {
      const dcuId = criteria?.SearcDCUID;
      if (dcuId === undefined || dcuId === null) return;
      if (criteria.CriteriaType !== EmAppCriteriaType.Entity) return;
      const override = overrides[dcuId];
      // Do not push operator-only / missing-values overrides into dictDcuValue —
      // that previously wiped Default Values with `{ values: [] }`.
      if (override && Array.isArray(override.values)) {
        onCriteriaValueChanged?.(dcuId, override);
      }
    });

    return overrides;
  }, [Criterias, getLiveCriteriaOverrides, onCriteriaValueChanged]);

  useEffect(() => {
    onRegisterLiveCriteriaOverrides?.(flushAndGetLiveCriteriaOverrides);
  }, [onRegisterLiveCriteriaOverrides, flushAndGetLiveCriteriaOverrides]);

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

    Object.values(entityControlRefs.current).forEach((ctl) => {
      if (ctl) ctl.checkedItems = [];
    });

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