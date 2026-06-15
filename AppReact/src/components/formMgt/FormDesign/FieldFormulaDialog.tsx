import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';

interface FieldFormulaDialogProps {
  isOpen: boolean;
  transactionData: any;
  currentFieldId: number;
  initialFormulaText: string;
  /**
   * default: show root-level tokens + relevant grid tokens (legacy behavior)
   * unitOnly: show ONLY the current unit's field tokens (for grid formulas)
   */
  tokenMode?: 'default' | 'unitOnly';
  onClose: () => void;
  onConfirm: (payload: {
    formulaType: 'Calculation' | 'Aggregation' | 'SubscribeParentField';
    formulaText: string;
    aggregationSetting: any;
  }) => void;
}

type FormulaType = 'Calculation' | 'Aggregation' | 'SubscribeParentField';

function normalizeFormulaText(input: string): string {
  let v = (input ?? '').trim();
  if (v.startsWith('=')) v = v.slice(1).trim();
  return v;
}

type UnitContext = { unit: any; parent: any | null; level: number } | null;

function findUnitContextById(appTransactionUnitList: any[] | undefined, unitId: number): UnitContext {
  if (!appTransactionUnitList || !unitId) return null;

  const walk = (units: any[], parent: any | null, level: number): UnitContext => {
    for (const u of units) {
      if (u?.Id === unitId) return { unit: u, parent, level };
      if (u?.Children?.length) {
        const found = walk(u.Children, u, level + 1);
        if (found) return found;
      }
    }
    return null;
  };

  return walk(appTransactionUnitList, null, 1);
}

function getFormulaDisplayName(field: any, unit: any): string {
  const dbField = field?.DataBaseFieldName || field?.DisplayName || '';
  const id = field?.Id;
  const isSibling = !!unit?.IsMasterSiblingUnit;
  const table = unit?.DataBaseTableName || '';

  let name = isSibling && table ? `[${table}.${dbField}` : `[${dbField}`;

  if (field?.IsTempVariable) {
    name += ']';
  } else {
    name += `_${id}]`;
  }
  return name;
}

const FieldFormulaDialog: React.FC<FieldFormulaDialogProps> = ({
  isOpen,
  transactionData,
  currentFieldId,
  initialFormulaText,
  tokenMode = 'default',
  onClose,
  onConfirm
}) => {
  const { theme } = useTheme();
  const { showValidationMessages, showError } = useErrorMessage();
  const aggTypeEnum = useEnumValues('EmAppAggregationFunctionType');
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const [formulaType, setFormulaType] = useState<FormulaType>('Calculation');
  const [formulaText, setFormulaText] = useState<string>(initialFormulaText ?? '');
  const [aggregationSetting, setAggregationSetting] = useState<any>({
    SubscribeToUnitId: null,
    SubscribeToTransFieldId: null,
    AggregationType: null
  });

  const [isSaving, setIsSaving] = useState<boolean>(false);

  // autocomplete
  const [isFilterVisible, setIsFilterVisible] = useState<boolean>(false);
  const [filterItems, setFilterItems] = useState<Array<{ Id: string; Display: string }>>([]);
  const [filterSelectedIndex, setFilterSelectedIndex] = useState<number>(0);

  useEffect(() => {
    if (!isOpen) return;

    setFormulaText(initialFormulaText ?? '');

    const fieldDto = transactionData?.dictTransactionFieldIdAndDto?.[currentFieldId];
    const cross = transactionData?.AppTransactionData?.DictTransFieldIdAndCrossRelationSettingDto?.[currentFieldId];

    const initialAgg = cross
      ? { ...cross }
      : {
          SubscribeToUnitId: null,
          SubscribeToTransFieldId: null,
          AggregationType: null
        };

    setAggregationSetting(initialAgg);

    const hasAgg = !!(fieldDto?.ParentUnitSubscribeChildAggFunctionId || initialAgg?.ParentUnitSubscribeChildAggFunctionId);
    const hasSubParent = !!(fieldDto?.ChildUnitSubscribeParentFieldId || initialAgg?.ChildUnitSubscribeParentFieldId);

    if (hasAgg) setFormulaType('Aggregation');
    else if (hasSubParent) setFormulaType('SubscribeParentField');
    else setFormulaType('Calculation');

    setIsFilterVisible(false);
    setFilterItems([]);
    setFilterSelectedIndex(0);
  }, [isOpen, initialFormulaText, currentFieldId, transactionData]);

  const fieldDto = transactionData?.dictTransactionFieldIdAndDto?.[currentFieldId];
  const unitId = fieldDto?.TransactionUnitId;
  const unitCtx: UnitContext = findUnitContextById(transactionData?.AppTransactionData?.AppTransactionUnitList, unitId);
  const currentUnit = unitCtx?.unit;
  const parentUnit = unitCtx?.parent;
  const level = unitCtx?.level || 1;

  const formulaDisplayName = useMemo(() => {
    if (!fieldDto) return `[Field_${currentFieldId}]`;
    return getFormulaDisplayName(fieldDto, transactionData?.dictUnitIdAndDto?.[unitId] || currentUnit);
  }, [fieldDto, currentFieldId, currentUnit, transactionData, unitId]);

  const formulaPrefix = `${formulaDisplayName} = `;
  const placeholder = `${formulaDisplayName} + ${formulaDisplayName}`;

  const allowAggregation = !!currentUnit?.Children?.length;
  const allowSubscribeParent = !!parentUnit;

  const operatorTokenList = useMemo(
    () => ['+', '-', '*', '/', '(', ')', '==', '!=', '>', '<', '>=', '<=', '&&', '||', '!', ':', 'true', 'false'],
    []
  );
  const functionToken = useMemo(
    () => [
      'string.IsNullOrEmpty()',
      'IsNumericHasValue()',
      'IsDDLHasValue()',
      'IsDateHasValue()',
      'ConvertValueToDate()',
      'ConvertValueToBoolean()',
      'ConvertValueToInt()',
      'ConvertValueToDecimal()'
    ],
    []
  );
  const jsonFunctionToken = useMemo(
    () => [
      'GetJsonNodeValueByPath(JsonString, JsonPath)',
      'FindOneItemFromJsonArray(JsonArrayString, PropName, PropValue)',
      'GetOneItemFromJsonArrayByIndex(JsonArrayString, Index)'
    ],
    []
  );

  const tokenGroups = useMemo(() => {
    const groups: Array<{ name: string; fields: any[] }> = [];

    if (tokenMode === 'unitOnly') {
      // For grid formulas:
      // - Child unit (level 2): show parent(root) + current
      // - Grandchild unit (level 3): show root-level fields + parent + current (Angular parity)
      if (level === 3) {
        const rootFields = transactionData?.rootLevelUnitFieldList || [];
        if (rootFields.length) {
          groups.push({ name: 'Root Level Field Tokens', fields: rootFields });
        }
      }
      if (parentUnit?.AppTransactionFieldList?.length) {
        groups.push({
          name: `Parent Level Field Tokens: ${parentUnit.UnitDisplayName || parentUnit.DataBaseTableName || ''}`.trim(),
          fields: parentUnit.AppTransactionFieldList
        });
      }
      if (currentUnit?.AppTransactionFieldList) {
        groups.push({
          name: `Field Tokens: ${currentUnit.UnitDisplayName || currentUnit.DataBaseTableName || ''}`.trim(),
          fields: currentUnit.AppTransactionFieldList
        });
      }
      return groups;
    }

    const rootFields = transactionData?.rootLevelUnitFieldList || [];
    groups.push({ name: 'Root Level Field Tokens', fields: rootFields });

    if (level === 2 && currentUnit?.AppTransactionFieldList) {
      groups.push({
        name: `Child Grid Field Tokens: ${currentUnit.UnitDisplayName || ''}`.trim(),
        fields: currentUnit.AppTransactionFieldList
      });
    } else if (level === 3) {
      if (parentUnit?.AppTransactionFieldList) {
        groups.push({
          name: `Child Grid Field Tokens: ${parentUnit.UnitDisplayName || ''}`.trim(),
          fields: parentUnit.AppTransactionFieldList
        });
      }
      if (currentUnit?.AppTransactionFieldList) {
        groups.push({
          name: `Grandchild Grid Field Tokens: ${currentUnit.UnitDisplayName || ''}`.trim(),
          fields: currentUnit.AppTransactionFieldList
        });
      }
    }

    return groups;
  }, [currentUnit, level, parentUnit, tokenMode, transactionData]);

  const autoCompleteTokenItems = useMemo(
    () => [
      { Id: 'true', Display: 'true' },
      { Id: 'false', Display: 'false' },
      { Id: 'string.IsNullOrEmpty()', Display: 'string.IsNullOrEmpty()' },
      { Id: 'IsNumericHasValue()', Display: 'IsNumericHasValue()' },
      { Id: 'IsDDLHasValue()', Display: 'IsDDLHasValue()' },
      { Id: 'IsDateHasValue()', Display: 'IsDateHasValue()' },
      { Id: 'ConvertValueToDate()', Display: 'ConvertValueToDate()' },
      { Id: 'ConvertValueToBoolean()', Display: 'ConvertValueToBoolean()' },
      { Id: 'ConvertValueToInt()', Display: 'ConvertValueToInt()' },
      { Id: 'ConvertValueToDecimal()', Display: 'ConvertValueToDecimal()' },
      { Id: 'GetJsonNodeValueByPath(JsonString, JsonPath)', Display: 'GetJsonNodeValueByPath(JsonString, JsonPath)' },
      {
        Id: 'FindOneItemFromJsonArray(JsonArrayString, PropName, PropValue)',
        Display: 'FindOneItemFromJsonArray(JsonArrayString, PropName, PropValue)'
      },
      {
        Id: 'GetOneItemFromJsonArrayByIndex(JsonArrayString, Index)',
        Display: 'GetOneItemFromJsonArrayByIndex(JsonArrayString, Index)'
      }
    ],
    []
  );

  const autoCompleteAllItems = useMemo(() => {
    const unitFields: Array<{ Id: string; Display: string }> =
      currentUnit?.AppTransactionFieldList?.map((f: any) => {
        const name = getFormulaDisplayName(f, currentUnit);
        return { Id: name, Display: name };
      }) || [];

    if (tokenMode === 'unitOnly') {
      const parentFields: Array<{ Id: string; Display: string }> =
        (parentUnit?.AppTransactionFieldList || []).map((f: any) => {
          const name = getFormulaDisplayName(f, parentUnit);
          return { Id: name, Display: name };
        }) || [];
      const rootFields: Array<{ Id: string; Display: string }> =
        level === 3
          ? (transactionData?.rootLevelUnitFieldList || []).map((f: any) => {
              const unit = transactionData?.dictUnitIdAndDto?.[f?.TransactionUnitId] || currentUnit || unitCtx?.unit;
              const name = getFormulaDisplayName(f, unit);
              return { Id: name, Display: name };
            })
          : [];
      return unitFields.concat(parentFields).concat(rootFields).concat(autoCompleteTokenItems);
    }

    const rootFields: Array<{ Id: string; Display: string }> =
      (transactionData?.rootLevelUnitFieldList || []).map((f: any) => {
        const unit = transactionData?.dictUnitIdAndDto?.[f?.TransactionUnitId] || currentUnit || unitCtx?.unit;
        const name = getFormulaDisplayName(f, unit);
        return { Id: name, Display: name };
      }) || [];

    return unitFields.concat(rootFields).concat(autoCompleteTokenItems);
  }, [autoCompleteTokenItems, currentUnit, tokenMode, transactionData, unitCtx]);

  const handleBackdropClick = (e: React.MouseEvent) => {
    e.stopPropagation();
  };

  const insertAtCursor = (text: string, padWithSpaces: boolean) => {
    const ta = textareaRef.current;
    if (!ta) {
      setFormulaText((prev) => (padWithSpaces ? `${prev} ${text} ` : `${prev}${text}`));
      return;
    }

    const start = ta.selectionStart ?? 0;
    const end = ta.selectionEnd ?? start;
    const left = formulaText.substring(0, start);
    const right = formulaText.substring(end);
    const insert = padWithSpaces ? ` ${text} ` : text;
    const next = left + insert + right;
    setFormulaText(next);

    requestAnimationFrame(() => {
      try {
        const pos = (left + insert).length;
        ta.focus();
        ta.setSelectionRange(pos, pos);
      } catch {
        // ignore
      }
    });
  };

  const handleFormulaTextChange = (next: string) => {
    setFormulaText(next);

    const idx = next.lastIndexOf(' ');
    const lastItem = next.substring(idx + 1).trim().toLowerCase();
    if (!lastItem) {
      setIsFilterVisible(false);
      setFilterItems([]);
      return;
    }

    const matches = autoCompleteAllItems.filter((it) => (it.Display || '').toLowerCase().includes(lastItem));
    setFilterItems(matches);
    setFilterSelectedIndex(0);
    setIsFilterVisible(matches.length > 0);
  };

  const applyAutoCompleteItem = (selected: { Display: string } | null) => {
    if (!selected) return;
    const text = formulaText || '';
    const idx = text.lastIndexOf(' ');
    const next = text.substring(0, idx + 1) + selected.Display;
    setFormulaText(next);
    setIsFilterVisible(false);
    setFilterItems([]);
    setFilterSelectedIndex(0);

    requestAnimationFrame(() => {
      textareaRef.current?.focus();
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      onClose();
      return;
    }

    if (isFilterVisible && filterItems.length > 0) {
      if (e.key === 'Enter') {
        e.preventDefault();
        applyAutoCompleteItem(filterItems[filterSelectedIndex] || null);
        return;
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        setFilterSelectedIndex((i) => Math.max(0, i - 1));
        return;
      }
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setFilterSelectedIndex((i) => Math.min(filterItems.length - 1, i + 1));
        return;
      }
    } else if (e.key === 'Enter') {
      // Keep Angular behavior: prevent newline in single-line insertion; textarea still allows it but we keep Enter for typing.
      // Do nothing here.
    }

    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      void handleSave();
    }
  };

  const getAggTypeDisplay = (id: number | null | undefined): string => {
    if (!id || !aggTypeEnum) return '';
    for (const [k, v] of Object.entries(aggTypeEnum)) {
      if (typeof v === 'number' && v === id && isNaN(Number(k))) return k;
    }
    return '';
  };

  const handleSave = async () => {
    if (isSaving) return;
    setIsSaving(true);
    try {
      const normalizedText = normalizeFormulaText(formulaText);

      if (formulaType === 'Calculation') {
        // Match Angular: validate on server before applying.
        // Angular builds: { TransactionUnitId, CaculationFlowSort: 1, FormulaExpression: prefix + text, OperationType: Assignment }
        const formularDto = {
          TransactionUnitId: currentUnit?.Id,
          CaculationFlowSort: 1,
          FormulaExpression: `${formulaPrefix}${normalizedText}`,
          // EmAppFormularType.Assignment in Angular is 1
          OperationType: 1
        };

        try {
          const validationResult = await appTransactionService.validateOneFormulaDto(formularDto);
          if (validationResult?.IsValid) {
            onConfirm({
              formulaType,
              formulaText: normalizedText,
              aggregationSetting: {
                SubscribeToUnitId: null,
                SubscribeToTransFieldId: null,
                AggregationType: null,
                ParentUnitSubscribeChildAggFunctionId: null,
                ChildUnitSubscribeParentFieldId: null
              }
            });
            return;
          }

          // Angular shows validation popup; in React we reuse the global validation popup.
          // Backend may return either { ValidationResult: { Items: ... } } or an object shaped like ValidationResult.
          showValidationMessages(validationResult?.ValidationResult ?? validationResult, true);
          return;
        } catch (e: any) {
          showError(e?.message || 'Failed to validate formula');
          return;
        }
      }

      if (formulaType === 'SubscribeParentField') {
        const subToFieldId = aggregationSetting?.SubscribeToTransFieldId || null;
        onConfirm({
          formulaType,
          formulaText: '',
          aggregationSetting: {
            ...aggregationSetting,
            ChildUnitSubscribeParentFieldId: subToFieldId,
            ParentUnitSubscribeChildAggFunctionId: null
          }
        });
        return;
      }

      // Aggregation
      const subscribeToFieldId = aggregationSetting?.SubscribeToTransFieldId || null;
      const aggregationType = aggregationSetting?.AggregationType ?? null;

      let parentUnitSubscribeChildAggFunctionId: number | null = null;
      if (subscribeToFieldId && aggregationType) {
        const subFieldDto = transactionData?.dictTransactionFieldIdAndDto?.[subscribeToFieldId];
        if (subFieldDto) {
          subFieldDto.AppTransactionFieldAggFunction_List = subFieldDto.AppTransactionFieldAggFunction_List || [];
          const existing = subFieldDto.AppTransactionFieldAggFunction_List.find(
            (x: any) => x?.AggregationFunctionType === aggregationType
          );
          if (existing?.Id) {
            parentUnitSubscribeChildAggFunctionId = existing.Id;
          } else {
            // Create via API, matching Angular behavior.
            const setDto = {
              TransactionId: transactionData?.AppTransactionData?.Id,
              TransactionFieldId: subscribeToFieldId,
              ListAppTransactionFieldAggFunction: subFieldDto.AppTransactionFieldAggFunction_List
            };

            // push pending function (no Id yet)
            subFieldDto.AppTransactionFieldAggFunction_List.push({
              AggregationFunctionType: aggregationType,
              TransactionFieldId: subscribeToFieldId
            });

            const saved = await appTransactionService.saveAppTransactionFieldAggFunctionSetDto(setDto);
            if (saved?.ListAppTransactionFieldAggFunction) {
              subFieldDto.AppTransactionFieldAggFunction_List = saved.ListAppTransactionFieldAggFunction;
              const created = subFieldDto.AppTransactionFieldAggFunction_List.find(
                (x: any) => x?.AggregationFunctionType === aggregationType
              );
              if (created?.Id) parentUnitSubscribeChildAggFunctionId = created.Id;
            }
          }
        }
      }

      onConfirm({
        formulaType,
        formulaText: '',
        aggregationSetting: {
          ...aggregationSetting,
          ParentUnitSubscribeChildAggFunctionId: parentUnitSubscribeChildAggFunctionId,
          ChildUnitSubscribeParentFieldId: null
        }
      });
    } finally {
      setIsSaving(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      data-prevent-field-setting-dismiss
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10000]"
      onClick={handleBackdropClick}
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '1000px', height: '700px', maxWidth: '95vw', maxHeight: '95vh' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
          <div className="flex flex-col">
            <h3 className={`text-sm font-semibold ${theme.title}`}>Formula Builder</h3>
          </div>
          <button
            onClick={onClose}
            className={`p-1 ${theme.button_default} rounded transition-all duration-200 hover:bg-opacity-80 active:scale-95`}
            title="Close"
          >
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                clipRule="evenodd"
              />
            </svg>
          </button>
        </div>

        {/* Body */}
        <div className="px-4 py-3 flex-1 overflow-hidden" onClick={() => setIsFilterVisible(false)}>
          {/* Prefix row + type dropdown (Angular-like) */}
          <div className="h-[25px] flex items-center font-semibold" style={{ color: '#41418a' }}>
            <div className="flex items-center gap-1 flex-1">
              <div className="py-[2px]">{formulaPrefix}</div>
              <div className="relative">
                <select
                  className={`text-xs border rounded ${theme.inputBox}`}
                  value={formulaType}
                  onChange={(e) => setFormulaType(e.target.value as FormulaType)}
                  style={{ height: '22px', padding: '0 6px' }}
                >
                  <option value="Calculation">fx Expression</option>
                  {allowAggregation && <option value="Aggregation">Σ Aggregate from Grid Column</option>}
                  {allowSubscribeParent && <option value="SubscribeParentField">Ṧ Subscribe to Parent Level Field</option>}
                </select>
              </div>
            </div>
          </div>

          {/* Content area */}
          <div className="h-[calc(100%-25px)] relative">
            {formulaType === 'Calculation' && (
              <div className="h-full flex flex-col">
                <div style={{ height: '60px' }}>
                  <textarea
                    ref={textareaRef}
                    className={`w-full h-full border ${theme.inputBox}`}
                    style={{
                      resize: 'none',
                      padding: '5px',
                      color: '#000',
                      fontWeight: 500
                    }}
                    spellCheck={false}
                    placeholder={placeholder}
                    value={formulaText}
                    onChange={(e) => handleFormulaTextChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    autoFocus
                  />
                </div>

                <div className="flex-1 pt-2 flex overflow-hidden">
                  {/* Tokens */}
                  <div className="w-1/2 pr-2 overflow-auto">
                    {tokenGroups.map((g) => (
                      <div key={g.name} className="py-2">
                        <div className="border border-[#ddd] relative p-2">
                          <div
                            className="absolute -top-2 left-0 right-0 mx-auto bg-white text-center text-[11px]"
                            style={{ width: '250px' }}
                          >
                            {g.name}
                          </div>
                          <div className="flex flex-wrap">
                            {([...g.fields] as any[])
                              .map((f: any) => ({
                                field: f,
                                display: getFormulaDisplayName(
                                  f,
                                  transactionData?.dictUnitIdAndDto?.[f?.TransactionUnitId] || currentUnit || unitCtx?.unit
                                )
                              }))
                              .sort((a: any, b: any) => (a.display || '').localeCompare(b.display || ''))
                              .map(({ field, display }: any) => (
                                <div key={display} style={{ width: '150px', flex: '1 1 auto', padding: '2px' }}>
                                  <button
                                    className="btn-default"
                                    style={{
                                      textAlign: 'left',
                                      width: '100%',
                                      height: '28px',
                                      borderRadius: '5px',
                                      boxShadow: 'none',
                                      fontWeight: 600,
                                      backgroundColor: '#f7f7f7',
                                      overflow: 'hidden',
                                      textOverflow: 'ellipsis',
                                      fontSize: '11px'
                                    }}
                                    title={display}
                                    onClick={() => insertAtCursor(display, false)}
                                  >
                                    {display}
                                  </button>
                                </div>
                              ))}
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Operators & functions */}
                  <div className="w-1/2 overflow-hidden">
                    <div className="border border-[#ddd] relative p-2 h-full">
                      <div
                        className="absolute -top-2 left-0 right-0 mx-auto bg-white text-center text-[11px]"
                        style={{ width: '160px' }}
                      >
                        Operators and Functions
                      </div>
                      <div className="h-full overflow-y-auto pt-2">
                        <div className="flex flex-wrap">
                          {operatorTokenList.map((t) => (
                            <div key={t} style={{ width: '50px', flex: '1 1 auto', padding: '2px' }}>
                              <button
                                className="btn-default"
                                style={{
                                  textAlign: 'center',
                                  width: '100%',
                                  height: '28px',
                                  borderRadius: '5px',
                                  boxShadow: 'none',
                                  fontWeight: 600,
                                  backgroundColor: '#f7f7f7',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                  fontSize: '12px'
                                }}
                                onClick={() => insertAtCursor(t, true)}
                              >
                                {t}
                              </button>
                            </div>
                          ))}
                        </div>

                        <div className="flex flex-wrap">
                          {functionToken.map((t) => (
                            <div key={t} style={{ width: '150px', flex: '1 1 auto', padding: '2px' }}>
                              <button
                                className="btn-default"
                                style={{
                                  textAlign: 'left',
                                  width: '100%',
                                  height: '28px',
                                  borderRadius: '5px',
                                  boxShadow: 'none',
                                  fontWeight: 600,
                                  backgroundColor: '#f7f7f7',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                  fontSize: '11px'
                                }}
                                title={t}
                                onClick={() => insertAtCursor(t, true)}
                              >
                                {t}
                              </button>
                            </div>
                          ))}
                        </div>

                        <div className="flex flex-wrap">
                          {jsonFunctionToken.map((t) => (
                            <div key={t} style={{ width: '300px', flex: '1 1 auto', padding: '2px' }}>
                              <button
                                className="btn-default"
                                style={{
                                  textAlign: 'left',
                                  width: '100%',
                                  height: '28px',
                                  borderRadius: '5px',
                                  boxShadow: 'none',
                                  fontWeight: 600,
                                  backgroundColor: '#f7f7f7',
                                  overflow: 'hidden',
                                  textOverflow: 'ellipsis',
                                  fontSize: '11px'
                                }}
                                title={t}
                                onClick={() => insertAtCursor(t, true)}
                              >
                                {t}
                              </button>
                            </div>
                          ))}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                {/* Autocomplete popover */}
                {isFilterVisible && filterItems.length > 0 && (
                  <div
                    className="absolute bg-white border shadow"
                    style={{ top: '90px', left: '10px', width: '420px', maxHeight: '400px', overflow: 'auto', zIndex: 2 }}
                    onClick={(e) => e.stopPropagation()}
                  >
                    {filterItems.map((it, idx) => (
                      <div
                        key={`${it.Id}-${idx}`}
                        className="px-2 py-1 text-xs cursor-pointer"
                        style={{ backgroundColor: idx === filterSelectedIndex ? '#e6f0ff' : 'transparent' }}
                        onMouseEnter={() => setFilterSelectedIndex(idx)}
                        onClick={() => applyAutoCompleteItem(it)}
                      >
                        {it.Display}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            {formulaType === 'Aggregation' && (
              <div className="h-full py-2">
                <div className="flex flex-col gap-2" style={{ maxWidth: '650px' }}>
                  <div className="flex items-center gap-2">
                    <label className={`text-xs font-semibold ${theme.label}`} style={{ width: '150px' }}>
                      Grid
                    </label>
                    <select
                      className={`text-xs border rounded ${theme.inputBox} flex-1`}
                      value={aggregationSetting?.SubscribeToUnitId || ''}
                      onChange={(e) =>
                        setAggregationSetting((prev: any) => ({
                          ...prev,
                          SubscribeToUnitId: e.target.value ? parseInt(e.target.value, 10) : null,
                          SubscribeToTransFieldId: null
                        }))
                      }
                    >
                      <option value="">(select)</option>
                      {(currentUnit?.Children || []).map((u: any) => (
                        <option key={u.Id} value={u.Id}>
                          {u.UnitDisplayName || u.DataBaseTableName || u.Id}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="flex items-center gap-2">
                    <label className={`text-xs font-semibold ${theme.label}`} style={{ width: '150px' }}>
                      Grid Column
                    </label>
                    <select
                      className={`text-xs border rounded ${theme.inputBox} flex-1`}
                      value={aggregationSetting?.SubscribeToTransFieldId || ''}
                      onChange={(e) =>
                        setAggregationSetting((prev: any) => ({
                          ...prev,
                          SubscribeToTransFieldId: e.target.value ? parseInt(e.target.value, 10) : null
                        }))
                      }
                      disabled={!aggregationSetting?.SubscribeToUnitId}
                    >
                      <option value="">(select)</option>
                      {(transactionData?.dictUnitIdAndDto?.[aggregationSetting?.SubscribeToUnitId]?.AppTransactionFieldList || []).map(
                        (f: any) => (
                          <option key={f.Id} value={f.Id}>
                            {f.DisplayName || f.DataBaseFieldName || f.Id}
                          </option>
                        )
                      )}
                    </select>
                  </div>

                  <div className="flex items-center gap-2">
                    <label className={`text-xs font-semibold ${theme.label}`} style={{ width: '150px' }}>
                      Aggregation Function
                    </label>
                    <select
                      className={`text-xs border rounded ${theme.inputBox} flex-1`}
                      value={aggregationSetting?.AggregationType || ''}
                      onChange={(e) =>
                        setAggregationSetting((prev: any) => ({
                          ...prev,
                          AggregationType: e.target.value ? parseInt(e.target.value, 10) : null
                        }))
                      }
                    >
                      <option value="">(select)</option>
                      {aggTypeEnum &&
                        Object.entries(aggTypeEnum)
                          .filter(([k, v]) => typeof v === 'number' && isNaN(Number(k)))
                          .map(([k, v]) => (
                            <option key={k} value={v as number}>
                              {k}
                            </option>
                          ))}
                    </select>
                  </div>

                  <div className={`text-[11px] ${theme.label}`}>
                    Preview: <span className="font-semibold">{getAggTypeDisplay(aggregationSetting?.AggregationType)}</span>
                    {aggregationSetting?.SubscribeToTransFieldId
                      ? `: ${transactionData?.dictTransactionFieldIdAndDto?.[aggregationSetting.SubscribeToTransFieldId]?.DisplayName || ''}`
                      : ''}
                  </div>
                </div>
              </div>
            )}

            {formulaType === 'SubscribeParentField' && (
              <div className="h-full py-2">
                <div className="flex flex-col gap-2" style={{ maxWidth: '650px' }}>
                  <div className="flex items-center gap-2">
                    <label className={`text-xs font-semibold ${theme.label}`} style={{ width: '150px' }}>
                      Parent Level Field
                    </label>
                    <select
                      className={`text-xs border rounded ${theme.inputBox} flex-1`}
                      value={aggregationSetting?.SubscribeToTransFieldId || ''}
                      onChange={(e) =>
                        setAggregationSetting((prev: any) => ({
                          ...prev,
                          SubscribeToTransFieldId: e.target.value ? parseInt(e.target.value, 10) : null
                        }))
                      }
                      disabled={!parentUnit}
                    >
                      <option value="">(select)</option>
                      {(parentUnit?.AppTransactionFieldList || []).map((f: any) => (
                        <option key={f.Id} value={f.Id}>
                          {f.DisplayName || f.DataBaseFieldName || f.Id}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Footer (Angular-like: left Save) */}
        <div className={`h-12 px-4 py-2 ${theme.mainContentSection}`}>
          <button
            className={`btn-default border ${theme.button_default}`}
            style={{ width: '100px' }}
            onClick={() => void handleSave()}
            disabled={isSaving}
          >
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default FieldFormulaDialog;

