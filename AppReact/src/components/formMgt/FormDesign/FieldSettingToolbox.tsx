import React, { useState, useMemo, useRef, useEffect } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { ComboBox } from '@mescius/wijmo.react.input';
import appHelper from '../../../helper/appHelper';
import FieldFormulaDialog from './FieldFormulaDialog';

interface FieldSettingToolboxProps {
  currentLayoutItem: any | null;
  formData: any;
  transactionData: any;
  onLayoutItemChange: (layoutItem: any) => void;
}

const FieldSettingToolbox: React.FC<FieldSettingToolboxProps> = ({
  currentLayoutItem,
  formData,
  transactionData,
  onLayoutItemChange
}) => {
  const { theme } = useTheme();
  const [isCollapsed, setIsCollapsed] = useState<boolean>(false);
  const [isFormulaDialogOpen, setIsFormulaDialogOpen] = useState<boolean>(false);
  const layoutItemTypeEnum = useEnumValues('EmAppFormLayoutItemType');
  const controlTypeEnum = useEnumValues('EmAppControlType');
  const aggTypeEnum = useEnumValues('EmAppAggregationFunctionType');
  
  // CRITICAL: Use ref to store the latest currentLayoutItem to avoid stale closure issues
  const currentLayoutItemRef = useRef<any | null>(null);
  
  // Update ref whenever currentLayoutItem changes
  useEffect(() => {
    currentLayoutItemRef.current = currentLayoutItem;
  }, [currentLayoutItem]);

  const normalizeFormula = (input: string) => {
    let v = (input ?? '').trim();
    if (v.startsWith('=')) v = v.slice(1).trim();
    return v;
  };

  const openFormulaEditor = () => {
    setIsFormulaDialogOpen(true);
  };

  const getAggTypeDisplay = (id: number | null | undefined): string => {
    if (!id || !aggTypeEnum) return '';
    for (const [k, v] of Object.entries(aggTypeEnum)) {
      if (typeof v === 'number' && v === id && isNaN(Number(k))) return k;
    }
    return '';
  };

  const handleSaveFormula = (payload: {
    formulaType: 'Calculation' | 'Aggregation' | 'SubscribeParentField';
    formulaText: string;
    aggregationSetting: any;
  }) => {
    const latestLayoutItem = currentLayoutItemRef.current;
    if (!latestLayoutItem) return;

    const fieldId: number | undefined = latestLayoutItem.TransactionFieldId;
    const normalized = normalizeFormula(payload.formulaText);

    const incomingDto = latestLayoutItem.ForeignAppTransactionFieldExDto
      ? {
          ...latestLayoutItem.ForeignAppTransactionFieldExDto
        }
      : undefined;

    // Helpers to mirror Angular's formula storage:
    // AppTransactionData.DictUnitldIdAndFormulaSetDto[unitId].ListAppTransactionUnitFormula
    const ensureFormulaSet = (unitId: number) => {
      if (!transactionData?.AppTransactionData) return null;
      transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto =
        transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto || {};
      const dict = transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto;
      if (!dict[unitId]) {
        dict[unitId] = {
          TransactionUnitId: unitId,
          ListAppTransactionUnitFormula: [],
          IsModified: true
        };
      }
      dict[unitId].ListAppTransactionUnitFormula = dict[unitId].ListAppTransactionUnitFormula || [];
      return dict[unitId];
    };

    const findMaxFormulaSort = (formulaList: any[]): number => {
      if (!formulaList || formulaList.length === 0) return 0;
      const max = Math.max(
        ...formulaList.map((f: any) => (typeof f?.CaculationFlowSort === 'number' ? f.CaculationFlowSort : 0))
      );
      return Number.isFinite(max) ? max : 0;
    };

    const getFormulaDisplayName = (field: any, unit: any) => {
      const dbField = field?.DataBaseFieldName || field?.DisplayName || '';
      const id = field?.Id;
      const isSibling = !!unit?.IsMasterSiblingUnit;
      const table = unit?.DataBaseTableName || '';
      let name = isSibling && table ? `[${table}.${dbField}` : `[${dbField}`;
      if (field?.IsTempVariable) name += ']';
      else name += `_${id}]`;
      return name;
    };

    const updateFormulaDictFromDto = (transFieldId: number, formulaDto: any | null) => {
      transactionData.dictTransfieldIdAndFormulaDto = transactionData.dictTransfieldIdAndFormulaDto || {};
      if (!formulaDto) {
        delete transactionData.dictTransfieldIdAndFormulaDto[transFieldId];
        return;
      }
      const expr = formulaDto?.FormulaExpression || '';
      let displayText = expr;
      const idxEq = expr.indexOf('=');
      if (idxEq >= 0 && expr.length > idxEq) displayText = expr.substring(idxEq + 1);
      formulaDto.displayText = displayText;
      transactionData.dictTransfieldIdAndFormulaDto[transFieldId] = formulaDto;
    };

    // Mirror Angular behavior: different formula types are stored as cross-relation settings.
    // We persist the text formula in `Formula` for Calculation; for others keep Formula empty.
    let nextDomFormula = '';
    let nextDtoFormula = '';

    if (payload.formulaType === 'Calculation') {
      nextDomFormula = normalized;
      nextDtoFormula = normalized;
      if (incomingDto) {
        incomingDto.ChildUnitSubscribeParentFieldId = null;
        incomingDto.ParentUnitSubscribeChildAggFunctionId = null;
      }

      // Update AppTransactionData formulas list (so it loads + saves like Angular)
      if (fieldId && transactionData?.dictTransactionFieldIdAndDto?.[fieldId]) {
        const transField = transactionData.dictTransactionFieldIdAndDto[fieldId];
        const unitId = transField.TransactionUnitId;
        const unitDto = transactionData?.dictUnitIdAndDto?.[unitId];
        const prefix = `${getFormulaDisplayName(transField, unitDto)} = `;

        const formulaSetDto = unitId ? ensureFormulaSet(unitId) : null;
        if (formulaSetDto) {
          const list: any[] = formulaSetDto.ListAppTransactionUnitFormula || [];
          const existing = transactionData?.dictTransfieldIdAndFormulaDto?.[fieldId];

          if (normalized) {
            if (existing) {
              existing.FormulaExpression = prefix + normalized;
              existing.displayText = normalized;
              updateFormulaDictFromDto(fieldId, existing);
            } else {
              const newFormula: any = {
                OperationType: 1,
                CaculationFlowSort: findMaxFormulaSort(list) + 1,
                TransactionUnitId: unitId,
                AssignToTransFieldId: fieldId,
                FormulaExpression: prefix + normalized,
                displayText: normalized
              };
              list.push(newFormula);
              updateFormulaDictFromDto(fieldId, newFormula);
            }
            formulaSetDto.IsModified = true;
          } else if (existing) {
            const idx = list.indexOf(existing);
            if (idx >= 0) list.splice(idx, 1);
            updateFormulaDictFromDto(fieldId, null);
            formulaSetDto.IsModified = true;
          }
        }
      }
    } else if (payload.formulaType === 'Aggregation') {
      nextDomFormula = '';
      nextDtoFormula = '';
      if (incomingDto) {
        incomingDto.ChildUnitSubscribeParentFieldId = null;
        incomingDto.ParentUnitSubscribeChildAggFunctionId =
          payload.aggregationSetting?.ParentUnitSubscribeChildAggFunctionId ?? null;
      }

      // Delete any existing assignment formula for this field (Angular deletes when switching to Aggregation)
      if (fieldId && transactionData?.dictTransactionFieldIdAndDto?.[fieldId]) {
        const transField = transactionData.dictTransactionFieldIdAndDto[fieldId];
        const unitId = transField.TransactionUnitId;
        const formulaSetDto = unitId ? ensureFormulaSet(unitId) : null;
        const existing = transactionData?.dictTransfieldIdAndFormulaDto?.[fieldId];
        if (formulaSetDto && existing) {
          const list: any[] = formulaSetDto.ListAppTransactionUnitFormula || [];
          const idx = list.indexOf(existing);
          if (idx >= 0) list.splice(idx, 1);
          updateFormulaDictFromDto(fieldId, null);
          formulaSetDto.IsModified = true;
        }
      }
    } else if (payload.formulaType === 'SubscribeParentField') {
      nextDomFormula = '';
      nextDtoFormula = '';
      if (incomingDto) {
        incomingDto.ParentUnitSubscribeChildAggFunctionId = null;
        incomingDto.ChildUnitSubscribeParentFieldId =
          payload.aggregationSetting?.ChildUnitSubscribeParentFieldId ??
          payload.aggregationSetting?.SubscribeToTransFieldId ??
          null;
      }

      // Delete any existing assignment formula for this field (Angular deletes when switching to Subscribe)
      if (fieldId && transactionData?.dictTransactionFieldIdAndDto?.[fieldId]) {
        const transField = transactionData.dictTransactionFieldIdAndDto[fieldId];
        const unitId = transField.TransactionUnitId;
        const formulaSetDto = unitId ? ensureFormulaSet(unitId) : null;
        const existing = transactionData?.dictTransfieldIdAndFormulaDto?.[fieldId];
        if (formulaSetDto && existing) {
          const list: any[] = formulaSetDto.ListAppTransactionUnitFormula || [];
          const idx = list.indexOf(existing);
          if (idx >= 0) list.splice(idx, 1);
          updateFormulaDictFromDto(fieldId, null);
          formulaSetDto.IsModified = true;
        }
      }
    }

    // Update cross-relation dict if present (Angular: DictTransFieldIdAndCrossRelationSettingDto)
    if (fieldId && transactionData?.AppTransactionData) {
      transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto =
        transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto || {};
      transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto[fieldId] = {
        ...(transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto[fieldId] || {}),
        ...(payload.aggregationSetting || {})
      };
      // Also mark transaction modified if supported by backend contract
      if (transactionData.AppTransactionData.AssociatedTransactionExDto) {
        transactionData.AppTransactionData.AssociatedTransactionExDto.IsModified = true;
      }
    }

    // CRITICAL: Update DomAttribute.Formula (used by change detection) and also keep field DTO formula in sync.
    const updatedLayoutItem = {
      ...latestLayoutItem,
      DomAttribute: {
        ...latestLayoutItem.DomAttribute,
        Formula: nextDomFormula
      },
      ForeignAppTransactionFieldExDto: incomingDto
        ? {
            ...incomingDto,
            Formula: nextDtoFormula
          }
        : undefined
    };

    // Preserve identifiers (mirrors other update patterns in this file)
    if (latestLayoutItem.TransactionFieldId) {
      updatedLayoutItem.TransactionFieldId = latestLayoutItem.TransactionFieldId;
    }
    if (latestLayoutItem.CurrentHostId) {
      updatedLayoutItem.CurrentHostId = latestLayoutItem.CurrentHostId;
    }

    onLayoutItemChange(updatedLayoutItem);
  };

  // Prepare control type options - must be declared before any early returns
  const controlTypeOptions = useMemo(() => {
    if (!controlTypeEnum) return [];
    const controlTypes: Array<{ value: number; label: string }> = [];
    Object.entries(controlTypeEnum).forEach(([key, value]) => {
      if (typeof value === 'number' && isNaN(Number(key))) {
        // Skip reverse mappings (numeric keys)
        controlTypes.push({ value: value as number, label: key });
      }
    });
    return controlTypes.sort((a, b) => a.value - b.value);
  }, [controlTypeEnum]);

  if (!currentLayoutItem) {
    return null;
  }

  const displayType = currentLayoutItem.DomAttribute?.WidgetDisplayType;
  
  // Don't show field setting toolbox for placeholder buttons (like AngularJS)
  // When placeholder button is selected, field setting should not be displayed
  if (displayType === layoutItemTypeEnum?.NewItemAddButton) {
    return null;
  }
  const isGrid = displayType === layoutItemTypeEnum?.Grid;
  const isBindingToDataField = currentLayoutItem.DomAttribute?.IsBindingToDataField;
  const isPhysicalModelTableCreated = formData?.IsPhysicalModelTableCreated;
  const isGridColumn = currentLayoutItem?.__isGridColumn === true;

  const currentFieldId: number | undefined = currentLayoutItem.TransactionFieldId;
  const _currentFieldDto = currentFieldId ? transactionData?.dictTransactionFieldIdAndDto?.[currentFieldId] : null;
  const currentFormulaDto = currentFieldId ? transactionData?.dictTransfieldIdAndFormulaDto?.[currentFieldId] : null;
  const currentFormulaRaw =
    currentFormulaDto?.displayText ??
    currentLayoutItem.ForeignAppTransactionFieldExDto?.Formula ??
    currentLayoutItem.DomAttribute?.Formula ??
    '';

  const getTransFieldFormulaSettingDisplayText = (): string => {
    if (!currentFieldId || !transactionData?.dictTransactionFieldIdAndDto) return currentFormulaRaw || '';
    const transFieldDto = transactionData.dictTransactionFieldIdAndDto[currentFieldId];
    if (!transFieldDto) return currentFormulaRaw || '';

    // Subscribe to parent field
    if (transFieldDto.ChildUnitSubscribeParentFieldId) {
      const parentField = transactionData.dictTransactionFieldIdAndDto[transFieldDto.ChildUnitSubscribeParentFieldId];
      if (parentField?.DisplayName) return parentField.DisplayName;
    }

    // Aggregate from grid column
    if (transFieldDto.ParentUnitSubscribeChildAggFunctionId) {
      const cross = transactionData?.AppTransactionData?.DictTransFieldIdAndCrossRelationSettingDto?.[currentFieldId];
      if (cross?.SubscribeToTransFieldId && cross?.AggregationType) {
        const subToField = transactionData.dictTransactionFieldIdAndDto[cross.SubscribeToTransFieldId];
        const aggDisplay = getAggTypeDisplay(cross.AggregationType);
        if (aggDisplay && subToField?.DisplayName) return `${aggDisplay}: ${subToField.DisplayName}`;
      }
    }

    // Normal expression
    if (currentFieldId && transactionData?.dictTransfieldIdAndFormulaDto?.[currentFieldId]) {
      return transactionData.dictTransfieldIdAndFormulaDto[currentFieldId].displayText || '';
    }
    return currentFormulaRaw || '';
  };

  const currentFormulaDisplay = getTransFieldFormulaSettingDisplayText();
  
  // Determine if height setting should be displayed (like AngularJS dictDisplayTypeAndUiPropertyOnOff)
  // Height is enabled for: Memo, Image, ExternalImageUrl, Video, SearchAndView, GoogleMap, 
  // Grid, Content, Space, LinkedSearch, HtmlContent
  // But not shown if IsUnlimitedHeight is true (for Grid)
  const shouldShowHeight = (
    displayType === layoutItemTypeEnum?.Memo ||
    displayType === layoutItemTypeEnum?.Image ||
    displayType === layoutItemTypeEnum?.ExternalImageUrl ||
    displayType === layoutItemTypeEnum?.Video ||
    displayType === layoutItemTypeEnum?.SearchAndView ||
    displayType === layoutItemTypeEnum?.GoogleMap ||
    displayType === layoutItemTypeEnum?.Grid ||
    displayType === layoutItemTypeEnum?.Content ||
    displayType === layoutItemTypeEnum?.Space ||
    displayType === layoutItemTypeEnum?.LinkedSearch ||
    displayType === layoutItemTypeEnum?.HtmlContent
  ) && !currentLayoutItem.DomAttribute?.IsUnlimitedHeight;

  const handleDisplayNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newDisplayName = e.target.value;
    // CRITICAL: Use ref to get the latest currentLayoutItem to avoid stale closure
    const latestLayoutItem = currentLayoutItemRef.current;
    if (!latestLayoutItem) {
      console.warn('handleDisplayNameChange: latestLayoutItem is null');
      return;
    }
    
    appHelper.debugLog('handleDisplayNameChange called:', {
      newDisplayName,
      hasForeignAppTransactionFieldExDto: !!latestLayoutItem.ForeignAppTransactionFieldExDto,
      currentHostId: latestLayoutItem.CurrentHostId,
      currentDisplayName: latestLayoutItem.DomAttribute?.DisplayName
    });
    
    // If bound to a transaction field, update field DisplayName (single source of truth for field types).
    // For non-field types (e.g., Literal Content), update DomAttribute.DisplayName.
    if (latestLayoutItem.ForeignAppTransactionFieldExDto) {
      const updatedItem = {
        ...latestLayoutItem,
        ForeignAppTransactionFieldExDto: {
          ...latestLayoutItem.ForeignAppTransactionFieldExDto,
          DisplayName: newDisplayName,
          LabelDisplayBinding: newDisplayName // Also update LabelDisplayBinding as fallback
        }
      };
      appHelper.debugLog('handleDisplayNameChange: Updating field DisplayName', updatedItem);
      onLayoutItemChange(updatedItem);
      return;
    }

    // For non-field types (Content, Space, etc.), update DomAttribute.DisplayName
    const updatedLayoutItem = {
      ...latestLayoutItem,
      DomAttribute: {
        ...latestLayoutItem.DomAttribute,
        DisplayName: newDisplayName
      }
    };
    
    // CRITICAL: Preserve CurrentHostId to identify the correct layout item
    if (latestLayoutItem.CurrentHostId) {
      updatedLayoutItem.CurrentHostId = latestLayoutItem.CurrentHostId;
    }
    
    // CRITICAL: Preserve Id if it exists
    if (latestLayoutItem.Id) {
      updatedLayoutItem.Id = latestLayoutItem.Id;
    }
    
    appHelper.debugLog('handleDisplayNameChange: Updating DomAttribute.DisplayName', updatedLayoutItem);
    onLayoutItemChange(updatedLayoutItem);
  };

  const handleColSpanChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const colSpan = parseInt(e.target.value) || 24;
    onLayoutItemChange({
      ...currentLayoutItem,
      DomAttribute: {
        ...currentLayoutItem.DomAttribute,
        ColSpanValue: colSpan
      }
    });
  };

  const handleHeightChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const height = parseInt(e.target.value) || 20;
    onLayoutItemChange({
      ...currentLayoutItem,
      DomAttribute: {
        ...currentLayoutItem.DomAttribute,
        HeightValue: height
      }
    });
  };

  const handleBackgroundColorChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onLayoutItemChange({
      ...currentLayoutItem,
      DomAttribute: {
        ...currentLayoutItem.DomAttribute,
        BackgroundColor: e.target.value
      }
    });
  };

  const handleTextColorChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onLayoutItemChange({
      ...currentLayoutItem,
      DomAttribute: {
        ...currentLayoutItem.DomAttribute,
        TextColor: e.target.value
      }
    });
  };

  const handleVisibleExpressionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onLayoutItemChange({
      ...currentLayoutItem,
      DomAttribute: {
        ...currentLayoutItem.DomAttribute,
        VisibleExpression: e.target.value
      }
    });
  };

  const _handleGridColumnWidthChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const latestLayoutItem = currentLayoutItemRef.current;
    if (!latestLayoutItem?.ForeignAppTransactionFieldExDto) return;
    const newWidth = parseInt(e.target.value) || 80;
    onLayoutItemChange({
      ...latestLayoutItem,
      ForeignAppTransactionFieldExDto: {
        ...latestLayoutItem.ForeignAppTransactionFieldExDto,
        DisplayWidth: newWidth
      }
    });
  };

  const handleGridColumnWidthStep = (delta: number) => {
    const latestLayoutItem = currentLayoutItemRef.current;
    const dto = latestLayoutItem?.ForeignAppTransactionFieldExDto;
    if (!dto) return;
    const currentWidth = parseInt((dto.DisplayWidth ?? 150).toString(), 10) || 150;
    const next = Math.max(60, Math.min(500, currentWidth + delta));
    onLayoutItemChange({
      ...latestLayoutItem,
      ForeignAppTransactionFieldExDto: {
        ...dto,
        DisplayWidth: next
      }
    });
  };

  return (
    <div className="w-full mb-4">
      <div 
        className={`flex items-center justify-between px-2 py-2 cursor-pointer ${theme.mainContentSection} border rounded-t`}
        onClick={() => setIsCollapsed(!isCollapsed)}
      >
        <span className={`text-sm font-semibold ${theme.title}`}>
          {isGrid ? 'Grid Setting' : 'Field Setting'}
        </span>
        <i 
          className={`fa ${isCollapsed ? 'fa-chevron-circle-down' : 'fa-chevron-circle-up'} text-gray-500`}
        ></i>
      </div>
      
      {!isCollapsed && (
        <div className={`p-2 border-l border-r border-b rounded-b ${theme.mainContentSection}`}>
          {/* Grid Column Setting (when user selects a column in Grid placeholder) */}
          {isGridColumn && currentLayoutItem.ForeignAppTransactionFieldExDto && (
            <div className="mb-5">
              <div className={`text-xs font-semibold ${theme.title} mb-2`}>Grid Column</div>
              <table className="w-full" style={{ tableLayout: 'fixed' }}>
                <tbody>
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Database Column</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full px-2 py-0.5 text-xs border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        readOnly
                        value={currentLayoutItem.ForeignAppTransactionFieldExDto?.DataBaseFieldName || ''}
                      />
                    </td>
                  </tr>
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Column Label</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full px-2 py-0.5 text-xs border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        value={currentLayoutItem.ForeignAppTransactionFieldExDto?.DisplayName || ''}
                        onChange={handleDisplayNameChange}
                      />
                    </td>
                  </tr>
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Column Width</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="flex items-center" style={{ width: '100%', height: '26px' }}>
                        <button
                          className={`btn-default border ${theme.button_default}`}
                          style={{ width: '26px', height: '26px', padding: '0px' }}
                          title="-"
                          onClick={() => handleGridColumnWidthStep(-10)}
                        >
                          -
                        </button>
                        <div
                          className={`flex-1 ${theme.inputBox} border`}
                          style={{
                            height: '26px',
                            backgroundColor: 'white',
                            borderLeft: 'none',
                            borderRight: 'none',
                            fontSize: '12px',
                            padding: '2px 4px',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'flex-end'
                          }}
                        >
                          {parseInt((currentLayoutItem.ForeignAppTransactionFieldExDto?.DisplayWidth ?? 150).toString(), 10) || 150}
                        </div>
                        <button
                          className={`btn-default border ${theme.button_default}`}
                          style={{ width: '26px', height: '26px', padding: '0px' }}
                          title="+"
                          onClick={() => handleGridColumnWidthStep(10)}
                        >
                          +
                        </button>
                      </div>
                    </td>
                  </tr>
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Control Type</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <ComboBox
                        itemsSource={controlTypeOptions}
                        displayMemberPath="label"
                        selectedValuePath="value"
                        selectedValue={currentLayoutItem.ForeignAppTransactionFieldExDto?.ControlType || currentLayoutItem.DomAttribute?.ControlType}
                        isEditable={false}
                        className={`w-full ${theme.inputBox} border`}
                        style={{ height: '26px', borderRadius: '0' }}
                        selectedIndexChanged={(sender: any) => {
                          const newControlType = sender.selectedValue;
                          const latestLayoutItem = currentLayoutItemRef.current;
                          if (newControlType === undefined || newControlType === null || !latestLayoutItem) return;

                          const oldControlType =
                            latestLayoutItem.ForeignAppTransactionFieldExDto?.ControlType || latestLayoutItem.DomAttribute?.ControlType;
                          if (newControlType === oldControlType) return;

                          onLayoutItemChange({
                            ...latestLayoutItem,
                            DomAttribute: {
                              ...latestLayoutItem.DomAttribute,
                              ControlType: newControlType,
                              WidgetDisplayType: newControlType
                            },
                            ForeignAppTransactionFieldExDto: latestLayoutItem.ForeignAppTransactionFieldExDto
                              ? {
                                  ...latestLayoutItem.ForeignAppTransactionFieldExDto,
                                  ControlType: newControlType
                                }
                              : undefined
                          });
                        }}
                      />
                    </td>
                  </tr>
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Formula</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="flex items-center" style={{ width: '100%', height: '26px' }}>
                        <div
                          className={`flex-1 ${theme.inputBox} border`}
                          style={{
                            height: '100%',
                            backgroundColor: 'white',
                            borderRight: 'none',
                            fontSize: '12px',
                            padding: '2px 4px',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center'
                          }}
                          title={`= ${currentFormulaDisplay || ''}`}
                          onClick={() => {
                            openFormulaEditor();
                          }}
                        >
                          <span style={{ whiteSpace: 'nowrap', fontStyle: 'italic' }}>
                            = {currentFormulaDisplay || ''}
                          </span>
                        </div>
                        <div style={{ width: '30px', height: '100%', display: 'flex' }}>
                          <button
                            className={`btn-default border ${theme.button_default}`}
                            style={{ height: '100%', width: '30px', padding: '0px' }}
                            title="Edit Formula"
                            onClick={() => {
                              openFormulaEditor();
                            }}
                          >
                            <i className="fa fa-superscript" style={{ fontSize: '13px' }}></i>
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
              <div className="border-t my-2" />
            </div>
          )}

          {/* Properties Section - hide if all properties are hidden (e.g., for Tab) */}
          {(() => {
            // Grid Column pseudo item: only show the "Grid Column" section above.
            // Hide all other property/settings blocks (Database Table/Column, Control Type, Formula, Workflow Trigger, Width, etc.)
            if (isGridColumn) {
              return null;
            }

            // Check if any property is visible
            const hasLabelText = !isBindingToDataField && displayType !== layoutItemTypeEnum?.Space && !currentLayoutItem.DomAttribute?.IsTab;
            const hasDatabaseTable = isPhysicalModelTableCreated && isBindingToDataField && !isGrid && currentLayoutItem.TransactionFieldId;
            const hasFieldDisplayName = isPhysicalModelTableCreated && isBindingToDataField && !isGrid;
            const hasControlType = isPhysicalModelTableCreated && isBindingToDataField && !isGrid && currentLayoutItem.ForeignAppTransactionFieldExDto;
            const hasFormula = isPhysicalModelTableCreated && isBindingToDataField && !isGrid;
            const hasWorkflowTrigger = isPhysicalModelTableCreated && isBindingToDataField && !isGrid;
            const hasAnyProperty = hasLabelText || hasDatabaseTable || hasFieldDisplayName || hasControlType || hasFormula || hasWorkflowTrigger;
            
            if (!hasAnyProperty) {
              return null; // Hide Properties section if no properties are visible
            }
            
            return (
              <div className="mb-5">
                {/* Properties label removed per user request */}
                {/* Use table layout for compactness (like AngularJS) */}
                <table className="w-full" style={{ tableLayout: 'fixed' }}>
              <tbody>
                {/* Label Text (if not binding to data field or not Space) - hide for Tab */}
                {!isBindingToDataField && 
                 displayType !== layoutItemTypeEnum?.Space &&
                 !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Label Text</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full px-2 py-0.5 text-xs border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        placeholder="Label"
                        value={currentLayoutItem.DomAttribute?.DisplayName || ''}
                        onChange={handleDisplayNameChange}
                      />
                    </td>
                  </tr>
                )}

                {/* Database Table and Column (if binding to data field) */}
                {isPhysicalModelTableCreated && isBindingToDataField && !isGrid && currentLayoutItem.TransactionFieldId && (
                  <>
                    <tr>
                      <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                        <label className={`text-xs ${theme.label}`}>Database Table</label>
                      </td>
                      <td style={{ width: '180px', padding: '2px 0' }}>
                        <div 
                          className={`${theme.inputBox} border truncate`}
                          style={{ 
                            height: '26px', 
                            backgroundColor: 'white',
                            fontSize: '12px',
                            padding: '2px 4px',
                            lineHeight: '22px',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis'
                          }}
                        >
                          {(() => {
                            const fieldDto = currentLayoutItem.ForeignAppTransactionFieldExDto;
                            if (fieldDto && transactionData?.AppTransactionData?.AppTransactionUnitList) {
                              // Find the unit that contains this field
                              for (const unit of transactionData.AppTransactionData.AppTransactionUnitList) {
                                if (unit.AppTransactionFieldList?.some((f: any) => f.Id === currentLayoutItem.TransactionFieldId)) {
                                  return unit.DataBaseTableName || 'N/A';
                                }
                              }
                            }
                            return transactionData?.AppTransactionData?.AppTransactionUnitList?.[0]?.DataBaseTableName || 'N/A';
                          })()}
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                        <label className={`text-xs ${theme.label}`}>
                          Database Column
                          {currentLayoutItem.TransactionFieldId ? (
                            <i className="fa fa-link ml-1 text-blue-500"></i>
                          ) : (
                            <i className="fa fa-chain-broken ml-1 text-gray-400"></i>
                          )}
                        </label>
                      </td>
                      <td style={{ width: '180px', padding: '2px 0' }}>
                        <div 
                          className={`${theme.inputBox} border`}
                          style={{ 
                            height: '26px', 
                            backgroundColor: 'white',
                            fontSize: '12px',
                            padding: '2px 4px',
                            lineHeight: '22px',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis'
                          }}
                        >
                          {currentLayoutItem.ForeignAppTransactionFieldExDto?.DataBaseFieldName || 
                           transactionData?.dictTransactionFieldIdAndDto?.[currentLayoutItem.TransactionFieldId]?.DataBaseFieldName || 
                           'N/A'}
                        </div>
                      </td>
                    </tr>
                  </>
                )}

                {/* Field Display Name (if binding to data field) */}
                {isPhysicalModelTableCreated && isBindingToDataField && !isGrid && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Field Display Name</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full border ${theme.inputBox}`}
                        style={{ 
                          height: '26px',
                          backgroundColor: 'white',
                          fontSize: '12px',
                          padding: '2px 4px'
                        }}
                        placeholder="Display Name"
                        value={currentLayoutItem.ForeignAppTransactionFieldExDto?.DisplayName || ''}
                        onChange={handleDisplayNameChange}
                      />
                    </td>
                  </tr>
                )}

                {/* Control Type (if binding to data field) */}
                {isPhysicalModelTableCreated && isBindingToDataField && !isGrid && currentLayoutItem.ForeignAppTransactionFieldExDto && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>
                        Control Type
                      </label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <ComboBox
                        itemsSource={controlTypeOptions}
                        displayMemberPath="label"
                        selectedValuePath="value"
                        selectedValue={currentLayoutItem.ForeignAppTransactionFieldExDto?.ControlType || currentLayoutItem.DomAttribute?.ControlType}
                        isEditable={false}
                        className={`w-full ${theme.inputBox} border`}
                        style={{ height: '26px', borderRadius: '0' }}
                        selectedIndexChanged={(sender: any) => {
                          const newControlType = sender.selectedValue;
                          // CRITICAL: Use ref to get the latest currentLayoutItem to avoid stale closure
                          const latestLayoutItem = currentLayoutItemRef.current;
                          if (newControlType !== undefined && newControlType !== null && latestLayoutItem) {
                            // Get the old ControlType to check if it's changing
                            const oldControlType = latestLayoutItem.ForeignAppTransactionFieldExDto?.ControlType || 
                                                  latestLayoutItem.DomAttribute?.ControlType;
                            
                            // CRITICAL: Only proceed if ControlType actually changed
                            // ComboBox may fire selectedIndexChanged even when value hasn't changed (e.g., during initialization)
                            if (newControlType === oldControlType) {
                              return; // No change, skip update
                            }
                            
                            // CRITICAL: When ControlType changes, set default height based on new type
                            let newHeightValue = latestLayoutItem.DomAttribute?.HeightValue;
                            if (newControlType !== oldControlType) {
                              // Set default height based on new ControlType
                              if (newControlType === controlTypeEnum?.Memo) {
                                newHeightValue = 150; // Default height for Memo
                              } else if (newControlType === controlTypeEnum?.Grid) {
                                newHeightValue = 400; // Default height for Grid
                              } else if (newControlType === controlTypeEnum?.Image || 
                                         newControlType === controlTypeEnum?.ExternalImageUrl ||
                                         newControlType === controlTypeEnum?.ImageBinary) {
                                newHeightValue = 400; // Default height for Image
                              } else {
                                // For other types (like TextBox), use default or keep current if reasonable
                                // If current height is very large (likely from Memo/Grid/Image), reset to default
                                //if (!newHeightValue || newHeightValue > 100) {
                                  newHeightValue = null; // Default height for TextBox and other controls
                                //}
                              }
                            }
                            
                            // CRITICAL: Use latestLayoutItem's TransactionFieldId to ensure we update the correct field
                            // Create updated layoutItem with new ControlType and height
                            const updatedLayoutItem = {
                              ...latestLayoutItem,
                              DomAttribute: {
                                ...latestLayoutItem.DomAttribute,
                                ControlType: newControlType,
                                HeightValue: newHeightValue
                              },
                              ForeignAppTransactionFieldExDto: latestLayoutItem.ForeignAppTransactionFieldExDto ? {
                                ...latestLayoutItem.ForeignAppTransactionFieldExDto,
                                ControlType: newControlType
                              } : undefined
                            };
                            
                            // CRITICAL: Ensure TransactionFieldId is preserved to identify the correct field
                            if (latestLayoutItem.TransactionFieldId) {
                              updatedLayoutItem.TransactionFieldId = latestLayoutItem.TransactionFieldId;
                            }
                            
                            // CRITICAL: Ensure CurrentHostId is preserved to identify the correct layout item
                            if (latestLayoutItem.CurrentHostId) {
                              updatedLayoutItem.CurrentHostId = latestLayoutItem.CurrentHostId;
                            }
                            
                            onLayoutItemChange(updatedLayoutItem);
                          }
                        }}
                      />
                    </td>
                  </tr>
                )}

                {/* Formula (if binding to data field) */}
                {isPhysicalModelTableCreated && isBindingToDataField && !isGrid && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Formula</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="flex items-center" style={{ width: '100%', height: '26px' }}>
                        <div 
                          className={`flex-1 ${theme.inputBox} border`}
                          style={{ 
                            height: '100%', 
                            backgroundColor: 'white', 
                            borderRight: 'none',
                            fontSize: '12px',
                            padding: '2px 4px',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center'
                          }}
                          title={`= ${currentFormulaDisplay || ''}`}
                          onClick={() => {
                            openFormulaEditor();
                          }}
                        >
                          <span style={{ whiteSpace: 'nowrap', fontStyle: 'italic' }}>
                            = {currentFormulaDisplay || ''}
                          </span>
                        </div>
                        <div style={{ width: '30px', height: '100%', display: 'flex' }}>
                          <button
                            className={`btn-default border ${theme.button_default}`}
                            style={{ height: '100%', width: '30px', padding: '0px' }}
                            title="Edit Formula"
                            onClick={() => {
                              openFormulaEditor();
                            }}
                          >
                            <i className="fa fa-superscript" style={{ fontSize: '13px' }}></i>
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}

                {/* Workflow Trigger (if binding to data field) */}
                {isPhysicalModelTableCreated && isBindingToDataField && !isGrid && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Workflow Trigger</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="flex items-center" style={{ width: '100%', height: '26px' }}>
                        <div 
                          className={`flex-1 ${theme.inputBox} border`}
                          style={{ 
                            height: '100%', 
                            backgroundColor: 'white', 
                            borderRight: 'none',
                            fontSize: '12px',
                            padding: '2px 4px',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            cursor: 'pointer',
                            display: 'flex',
                            alignItems: 'center'
                          }}
                          title={currentLayoutItem.DomAttribute?.WorkflowTrigger || ''}
                          onClick={() => {
                            // TODO: Open workflow selector dialog
                            alert('Workflow Selector - To be implemented');
                          }}
                        >
                          <span style={{ whiteSpace: 'nowrap' }}>
                            {currentLayoutItem.DomAttribute?.WorkflowTrigger || ''}
                          </span>
                        </div>
                        <div style={{ width: '30px', height: '100%', display: 'flex' }}>
                          <button
                            className={`btn-default border ${theme.button_default}`}
                            style={{ height: '100%', width: '30px', padding: '0px' }}
                            title="Edit Workflow Trigger"
                            onClick={() => {
                              // TODO: Open workflow selector dialog
                              alert('Workflow Selector - To be implemented');
                            }}
                          >
                            <span style={{ fontSize: '11px', fontWeight: 600, fontFamily: 'initial' }}>W</span>
                          </button>
                        </div>
                      </div>
                    </td>
                  </tr>
                )}
                  </tbody>
                </table>
              </div>
            );
          })()}

          {/* Layout Section - label removed per user request */}
          <div className="mb-5">
            {/* Use table layout for compactness (like AngularJS) */}
            <table className="w-full" style={{ tableLayout: 'fixed' }}>
              <tbody>
                {/* Tab Display Name (for Tab items) */}
                {currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Tab Name</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full border ${theme.inputBox}`}
                        style={{ 
                          height: '26px',
                          backgroundColor: 'white',
                          fontSize: '12px',
                          padding: '2px 4px',
                          borderRadius: '0'
                        }}
                        placeholder="Tab Name"
                        value={currentLayoutItem.DomAttribute?.DisplayName || ''}
                        onChange={(e) => {
                          onLayoutItemChange({
                            ...currentLayoutItem,
                            DomAttribute: {
                              ...currentLayoutItem.DomAttribute,
                              DisplayName: e.target.value
                            }
                          });
                        }}
                      />
                    </td>
                  </tr>
                )}
                
                {/* Width (ColSpan) */}
                {!currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>
                        {displayType === layoutItemTypeEnum?.Section || 
                         displayType === layoutItemTypeEnum?.TabContainer || 
                         displayType === layoutItemTypeEnum?.TableContainer
                          ? 'Container Width' 
                          : 'Width'}
                      </label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="text-center text-xs text-gray-500" style={{ height: '16px', lineHeight: '16px' }}>
                        {currentLayoutItem.DomAttribute?.ColSpanValue || 24}/24
                      </div>
                      <div style={{ height: '20px', padding: '5px 0' }}>
                        <input
                          type="range"
                          min="2"
                          max="24"
                          step="1"
                          value={currentLayoutItem.DomAttribute?.ColSpanValue || 24}
                          onChange={handleColSpanChange}
                          className="w-full"
                          style={{ height: '10px' }}
                        />
                      </div>
                    </td>
                  </tr>
                )}

                {/* Child Row Total Cells (for Section) - but not for Tab */}
                {displayType === layoutItemTypeEnum?.Section && 
                 !currentLayoutItem.DomAttribute?.IsTab &&
                 currentLayoutItem.DomAttribute?.DefaultNbColumns !== undefined && (
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Child Row Total Cells</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="text-center text-xs text-gray-500" style={{ height: '16px', lineHeight: '16px' }}>
                        {currentLayoutItem.DomAttribute?.DefaultNbColumns || 1}
                      </div>
                      <div style={{ height: '20px', padding: '5px 0' }}>
                        <input
                          type="range"
                          min="1"
                          max="6"
                          step="1"
                          value={currentLayoutItem.DomAttribute?.DefaultNbColumns || 1}
                          onChange={(e) => {
                            const value = parseInt(e.target.value) || 1;
                            onLayoutItemChange({
                              ...currentLayoutItem,
                              DomAttribute: {
                                ...currentLayoutItem.DomAttribute,
                                DefaultNbColumns: value
                              }
                            });
                          }}
                          className="w-full"
                          style={{ height: '10px' }}
                        />
                      </div>
                    </td>
                  </tr>
                )}

                {/* Height (if enabled for this type) - like AngularJS dictDisplayTypeAndUiPropertyOnOff */}
                {/* Don't show height for Tab */}
                {shouldShowHeight && !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Height</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <div className="text-center text-xs text-gray-500" style={{ height: '16px', lineHeight: '16px' }}>
                        {currentLayoutItem.DomAttribute?.HeightValue || 20} px
                      </div>
                      <div style={{ height: '20px', padding: '5px 0' }}>
                        <input
                          type="range"
                          min="20"
                          max="1000"
                          step="5"
                          value={currentLayoutItem.DomAttribute?.HeightValue || 20}
                          onChange={handleHeightChange}
                          className="w-full"
                          style={{ height: '10px' }}
                        />
                      </div>
                    </td>
                  </tr>
                )}

                {/* Background Color */}
                {/* Don't show for Tab */}
                {(displayType === layoutItemTypeEnum?.Space ||
                  displayType === layoutItemTypeEnum?.Section ||
                  displayType === layoutItemTypeEnum?.Memo ||
                  displayType === layoutItemTypeEnum?.Image) &&
                 !isBindingToDataField &&
                 displayType !== layoutItemTypeEnum?.TabContainer &&
                 !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Background Color</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="color"
                        className={`w-full border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        value={currentLayoutItem.DomAttribute?.BackgroundColor || '#ffffff'}
                        onChange={handleBackgroundColorChange}
                      />
                    </td>
                  </tr>
                )}

                {/* Text Color */}
                {/* Don't show for Tab */}
                {displayType !== layoutItemTypeEnum?.Image &&
                 displayType !== layoutItemTypeEnum?.ExternalImageUrl &&
                 displayType !== layoutItemTypeEnum?.Video &&
                 displayType !== layoutItemTypeEnum?.Grid &&
                 displayType !== layoutItemTypeEnum?.Space &&
                 displayType !== layoutItemTypeEnum?.Content &&
                 !isBindingToDataField &&
                 displayType !== layoutItemTypeEnum?.TabContainer &&
                 !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Text Color</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="color"
                        className={`w-full border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        value={currentLayoutItem.DomAttribute?.TextColor || '#000000'}
                        onChange={handleTextColorChange}
                      />
                    </td>
                  </tr>
                )}

                {/* Visible Expression */}
                {/* Don't show for Tab */}
                {(displayType === layoutItemTypeEnum?.Section ||
                  displayType === layoutItemTypeEnum?.Content ||
                  displayType === layoutItemTypeEnum?.Space) &&
                 displayType !== layoutItemTypeEnum?.TabContainer &&
                 !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Visible Expression</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="text"
                        className={`w-full px-2 py-0.5 text-xs border rounded ${theme.inputBox}`}
                        style={{ height: '20px' }}
                        placeholder="e.g., field1 > 0"
                        value={currentLayoutItem.DomAttribute?.VisibleExpression || ''}
                        onChange={handleVisibleExpressionChange}
                      />
                    </td>
                  </tr>
                )}

                {/* Is Collapsible (for Section) - but not for Tab */}
                {displayType === layoutItemTypeEnum?.Section && !currentLayoutItem.DomAttribute?.IsTab && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Is Collapsible</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="checkbox"
                        className="w-3.5 h-3.5"
                        checked={currentLayoutItem.DomAttribute?.IsCollapsible || false}
                        onChange={(e) => {
                          onLayoutItemChange({
                            ...currentLayoutItem,
                            DomAttribute: {
                              ...currentLayoutItem.DomAttribute,
                              IsCollapsible: e.target.checked
                            }
                          });
                        }}
                      />
                    </td>
                  </tr>
                )}

                {/* Default Collapsed (for Section if collapsible) - but not for Tab */}
                {displayType === layoutItemTypeEnum?.Section && 
                 !currentLayoutItem.DomAttribute?.IsTab &&
                 currentLayoutItem.DomAttribute?.IsCollapsible && (
                  <tr>
                    <td style={{ width: '125px', verticalAlign: 'top', padding: '2px 0' }}>
                      <label className={`text-xs ${theme.label}`}>Default Collapsed</label>
                    </td>
                    <td style={{ width: '180px', padding: '2px 0' }}>
                      <input
                        type="checkbox"
                        className="w-3.5 h-3.5"
                        checked={currentLayoutItem.DomAttribute?.IsDefaultCollapsed || false}
                        onChange={(e) => {
                          onLayoutItemChange({
                            ...currentLayoutItem,
                            DomAttribute: {
                              ...currentLayoutItem.DomAttribute,
                              IsDefaultCollapsed: e.target.checked
                            }
                          });
                        }}
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <FieldFormulaDialog
        isOpen={isFormulaDialogOpen}
        transactionData={transactionData}
        currentFieldId={currentFieldId || 0}
        initialFormulaText={currentFormulaRaw || ''}
        onClose={() => setIsFormulaDialogOpen(false)}
        onConfirm={(value) => {
          handleSaveFormula(value);
          setIsFormulaDialogOpen(false);
        }}
      />
    </div>
  );
};

export default FieldSettingToolbox;
