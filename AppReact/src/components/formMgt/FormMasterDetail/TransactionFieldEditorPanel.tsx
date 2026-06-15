import React, { useCallback, useMemo, useRef, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import FieldFormulaDialog from '../FormDesign/FieldFormulaDialog';
import TransactionFieldCascadingModal from './TransactionFieldCascadingModal';

/** Set to true when Field Layout editing is ready (Angular TransactionFieldEditor parity). */
const SHOW_FIELD_LAYOUT_SECTION = false;

export type TransactionFieldEditorPanelProps = {
  /** Angular `controllerModel.isGrid` — field is a grid column. */
  isGridColumn: boolean;
  currentTransField: any;
  onChangeField: (next: any) => void;
  transactionData: any;
  dictEntityIdAndDto: Record<number, any>;
  orgTransField: any;
  gridFormFieldStyleObj: { width: number; height: number };
  setGridFormFieldStyleObj: React.Dispatch<React.SetStateAction<{ width: number; height: number }>>;
};

type UiFlags = {
  EnableWidth: boolean;
  EnableHeight: boolean;
  EnableBackgroundColor: boolean;
  EnableTextColor: boolean;
  EnableLableWidth: boolean;
  EnableDefaultNbColumns: boolean;
  EnableVisibleExpression: boolean;
};

function buildDictDisplayTypeAndUiPropertyOnOff(em: Record<string, number> | null): Record<number, UiFlags> {
  const dict: Record<number, UiFlags> = {};
  const defaultUi: UiFlags = {
    EnableWidth: true,
    EnableHeight: false,
    EnableBackgroundColor: true,
    EnableTextColor: true,
    EnableLableWidth: false,
    EnableDefaultNbColumns: false,
    EnableVisibleExpression: false,
  };
  if (!em) return dict;
  Object.entries(em).forEach(([k, v]) => {
    if (typeof v === 'number' && isNaN(Number(k))) {
      dict[v] = { ...defaultUi };
    }
  });
  const set = (key: string, partial: Partial<UiFlags>) => {
    const id = em[key];
    if (typeof id === 'number' && dict[id]) Object.assign(dict[id], partial);
  };
  set('Memo', { EnableHeight: true });
  set('Image', { EnableHeight: true, EnableTextColor: false, EnableLableWidth: false });
  set('ExternalImageUrl', { EnableHeight: true, EnableTextColor: false, EnableLableWidth: false });
  set('Video', { EnableHeight: true, EnableTextColor: false, EnableLableWidth: false });
  set('SearchAndView', { EnableHeight: true, EnableLableWidth: false });
  set('GoogleMap', { EnableHeight: true, EnableTextColor: false, EnableLableWidth: true });
  set('Grid', { EnableHeight: true, EnableTextColor: false, EnableBackgroundColor: false, EnableLableWidth: false });
  return dict;
}

function isFlexFormLayout(field: any): boolean {
  return !!(field && field.DomAttribute);
}

function isGridFormLayout(field: any): boolean {
  return !!(field && !field.DomAttribute && field.StyleLayoutInfo);
}

const ddlFamily = (ct: number | undefined, Em: Record<string, number> | null) => {
  if (ct == null || !Em) return false;
  return (
    ct === Em.DDL ||
    ct === Em.SearchAbleDDL ||
    ct === Em.AutoComplete ||
    ct === Em.RadioButtons ||
    ct === Em.Progress
  );
};

const TransactionFieldEditorPanel: React.FC<TransactionFieldEditorPanelProps> = ({
  isGridColumn,
  currentTransField,
  onChangeField,
  transactionData,
  dictEntityIdAndDto,
  orgTransField,
  gridFormFieldStyleObj,
  setGridFormFieldStyleObj,
}) => {
  const { theme } = useTheme();
  const emControl = useEnumValues('EmAppControlType');
  const emEntityType = useEnumValues('EmAppEntityType');
  const dictUi = useMemo(() => buildDictDisplayTypeAndUiPropertyOnOff(emControl as any), [emControl]);

  const controlTypeOptions = useMemo(() => {
    if (!emControl) return [];
    const controlTypes: Array<{ value: number; label: string }> = [];
    Object.entries(emControl).forEach(([key, value]) => {
      if (typeof value === 'number' && isNaN(Number(key))) {
        controlTypes.push({ value: value as number, label: key });
      }
    });
    return controlTypes.sort((a, b) => a.value - b.value);
  }, [emControl]);

  const controlTypeCv = useMemo(() => new CollectionView(controlTypeOptions), [controlTypeOptions]);

  const [entityPickerOpen, setEntityPickerOpen] = useState(false);
  const [cascadingOpen, setCascadingOpen] = useState(false);
  const [formulaOpen, setFormulaOpen] = useState(false);

  const fieldRef = useRef(currentTransField);
  fieldRef.current = currentTransField;

  const flexLayout = isFlexFormLayout(currentTransField);
  const gridFormLayout = isGridFormLayout(currentTransField);
  const uiForType = dictUi[currentTransField?.ControlType] ?? dictUi[0];

  const isAllowEditEntityData = (entityId: number | null | undefined) => {
    if (entityId == null || !dictEntityIdAndDto[entityId]) return false;
    const entityDto = dictEntityIdAndDto[entityId];
    const sysTable = emEntityType?.SystemDefineTable;
    return (
      !entityDto.IsSystemDefine &&
      sysTable != null &&
      entityDto.EntityType === sysTable &&
      !!entityDto.TableName
    );
  };

  const handleControlTypeChange = (newType: number | null | undefined) => {
    const f = fieldRef.current;
    if (newType == null || !f || !emControl) return;
    const fs = f.FieldChangeSetting || {};
    const org = orgTransField;

    let nextFs = { ...fs };
    if (org && nextFs.OrgControlType !== newType) {
      if (nextFs.NewControlType !== newType) {
        nextFs.NewControlType = newType;
        const orgCt = nextFs.OrgControlType;
        nextFs.IsChangeFromOtherTypeToDDL =
          !ddlFamily(orgCt, emControl as any) && ddlFamily(newType, emControl as any);
        nextFs.IsChagneFromDDLToOtherType =
          ddlFamily(orgCt, emControl as any) && !ddlFamily(newType, emControl as any);
        if (nextFs.IsChagneFromDDLToOtherType) {
          nextFs.NewEntityId = null;
        }
        if (nextFs.IsChagneFromDDLToOtherType) {
          const go = window.confirm(
            'You have changed the control type from DDL to another type. Do you want to update the field data based on the mapping entity table column?'
          );
          if (go) {
            /* Re-map uses same flow as footer button — parent should open full mapper; here we only flag. */
          }
        }
        if (nextFs.IsChangeFromOtherTypeToDDL) {
          const go = window.confirm(
            'You have changed the control type to DDL from another type. Do you want to update the field data to entity ID column?'
          );
          if (go) {
            /* Column mapping — user uses Re-map Entity Column in footer when available */
          }
        }
      }
    } else {
      nextFs = {
        ...nextFs,
        NewControlType: null,
        NewEntityId: null,
        IsChangeFromOtherTypeToDDL: false,
        IsChagneFromDDLToOtherType: false,
      };
    }

    onChangeField({
      ...f,
      ControlType: newType,
      FieldChangeSetting: nextFs,
      IsModified: true,
    });
  };

  const applyFormulaPayload = useCallback(
    (payload: { formulaType: string; formulaText: string; aggregationSetting: any }) => {
      const latest = fieldRef.current;
      if (!latest?.Id) return;

      const fieldId: number = latest.Id;
      const normalize = (input: string) => {
        let v = (input ?? '').trim();
        if (v.startsWith('=')) v = v.slice(1).trim();
        return v;
      };
      const normalized = normalize(payload.formulaText);

      const ensureFormulaSet = (unitId: number) => {
        if (!transactionData?.AppTransactionData) return null;
        transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto =
          transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto || {};
        const dict = transactionData.AppTransactionData.DictUnitldIdAndFormulaSetDto;
        if (!dict[unitId]) {
          dict[unitId] = {
            TransactionUnitId: unitId,
            ListAppTransactionUnitFormula: [],
            IsModified: true,
          };
        }
        dict[unitId].ListAppTransactionUnitFormula = dict[unitId].ListAppTransactionUnitFormula || [];
        return dict[unitId];
      };

      const findMaxFormulaSort = (formulaList: any[]): number => {
        if (!formulaList?.length) return 0;
        const max = Math.max(
          ...formulaList.map((x: any) => (typeof x?.CaculationFlowSort === 'number' ? x.CaculationFlowSort : 0))
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

      let nextDomFormula = '';
      let nextDtoFormula = '';

      const incomingDto = { ...latest };

      if (payload.formulaType === 'Calculation') {
        nextDomFormula = normalized;
        nextDtoFormula = normalized;
        incomingDto.ChildUnitSubscribeParentFieldId = null;
        incomingDto.ParentUnitSubscribeChildAggFunctionId = null;

        const transField = transactionData?.dictTransactionFieldIdAndDto?.[fieldId];
        const unitId = transField?.TransactionUnitId;
        const unitDto = transactionData?.dictUnitIdAndDto?.[unitId];
        const prefix = `${getFormulaDisplayName(transField, unitDto)} = `;

        const formulaSetDto = unitId ? ensureFormulaSet(unitId) : null;
        if (formulaSetDto && transField) {
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
                displayText: normalized,
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
      } else if (payload.formulaType === 'Aggregation') {
        nextDomFormula = '';
        nextDtoFormula = '';
        incomingDto.ChildUnitSubscribeParentFieldId = null;
        incomingDto.ParentUnitSubscribeChildAggFunctionId =
          payload.aggregationSetting?.ParentUnitSubscribeChildAggFunctionId ?? null;

        const transField = transactionData?.dictTransactionFieldIdAndDto?.[fieldId];
        const unitId = transField?.TransactionUnitId;
        const formulaSetDto = unitId ? ensureFormulaSet(unitId) : null;
        const existing = transactionData?.dictTransfieldIdAndFormulaDto?.[fieldId];
        if (formulaSetDto && existing) {
          const list: any[] = formulaSetDto.ListAppTransactionUnitFormula || [];
          const idx = list.indexOf(existing);
          if (idx >= 0) list.splice(idx, 1);
          updateFormulaDictFromDto(fieldId, null);
          formulaSetDto.IsModified = true;
        }
      } else if (payload.formulaType === 'SubscribeParentField') {
        nextDomFormula = '';
        nextDtoFormula = '';
        incomingDto.ParentUnitSubscribeChildAggFunctionId = null;
        incomingDto.ChildUnitSubscribeParentFieldId =
          payload.aggregationSetting?.ChildUnitSubscribeParentFieldId ??
          payload.aggregationSetting?.SubscribeToTransFieldId ??
          null;

        const transField = transactionData?.dictTransactionFieldIdAndDto?.[fieldId];
        const unitId = transField?.TransactionUnitId;
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

      if (transactionData?.AppTransactionData) {
        transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto =
          transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto || {};
        transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto[fieldId] = {
          ...(transactionData.AppTransactionData.DictTransFieldIdAndCrossRelationSettingDto[fieldId] || {}),
          ...(payload.aggregationSetting || {}),
        };
      }

      const merged: any = {
        ...incomingDto,
        Formula: nextDtoFormula,
        IsModified: true,
      };
      if (merged.DomAttribute) {
        merged.DomAttribute = { ...merged.DomAttribute, Formula: nextDomFormula };
      }

      onChangeField(merged);
    },
    [onChangeField, transactionData]
  );

  const getTransFieldFormulaDisplay = (): string => {
    const fid = currentTransField?.Id;
    if (!fid || !transactionData?.dictTransactionFieldIdAndDto) return currentTransField?.Formula || '';
    const transFieldDto = transactionData.dictTransactionFieldIdAndDto[fid];
    if (!transFieldDto) return currentTransField?.Formula || '';

    if (transFieldDto.ChildUnitSubscribeParentFieldId) {
      const parentField = transactionData.dictTransactionFieldIdAndDto[transFieldDto.ChildUnitSubscribeParentFieldId];
      if (parentField?.DisplayName) return parentField.DisplayName;
    }

    if (transFieldDto.ParentUnitSubscribeChildAggFunctionId) {
      const cross = transactionData?.AppTransactionData?.DictTransFieldIdAndCrossRelationSettingDto?.[fid];
      if (cross?.SubscribeToTransFieldId && cross?.AggregationType) {
        const subToField = transactionData.dictTransactionFieldIdAndDto[cross.SubscribeToTransFieldId];
        return subToField?.DisplayName ? `${cross.AggregationType}: ${subToField.DisplayName}` : '';
      }
    }

    if (transactionData?.dictTransfieldIdAndFormulaDto?.[fid]) {
      return transactionData.dictTransfieldIdAndFormulaDto[fid].displayText || '';
    }
    return currentTransField?.Formula || '';
  };

  const datasourceSelectedHandler = (entityId: number | null) => {
    const f = fieldRef.current;
    if (!f || entityId == null) return;
    const next = {
      ...f,
      EntityId: entityId,
      DdlQueryText: null,
      WhereClauseExpress: null,
      DdlForeignUnitId: null,
      IsModified: true,
    };
    const fs = { ...(next.FieldChangeSetting || {}) };
    if (orgTransField && fs.OrgEntityId !== entityId && fs.NewEntityId !== entityId) {
      fs.NewEntityId = entityId;
    } else {
      fs.NewEntityId = null;
    }
    next.FieldChangeSetting = fs;
    onChangeField(next);
    setEntityPickerOpen(false);
  };

  const entityRows = useMemo(() => Object.values(dictEntityIdAndDto || {}), [dictEntityIdAndDto]);

  if (!currentTransField) return null;

  const Em = emControl as any;

  return (
    <div className="w-full text-xs">
      <div className={`font-semibold mb-2 ${theme.title}`}>Field Logical Properties:</div>
      <div className="space-y-2 pl-1">
        <div>
          <label className={`block mb-0.5 ${theme.label}`}>Field Display Name</label>
          <input
            type="text"
            className={`w-full h-7 px-2 border ${theme.inputBox}`}
            value={currentTransField.DisplayName ?? ''}
            onChange={(e) => onChangeField({ ...currentTransField, DisplayName: e.target.value, IsModified: true })}
          />
        </div>

        <div>
          <label className={`block mb-0.5 ${theme.label}`}>Control Type</label>
          <ComboBox
            itemsSource={controlTypeCv}
            displayMemberPath="label"
            selectedValuePath="value"
            selectedValue={currentTransField.ControlType}
            isEditable={false}
            selectedIndexChanged={(s: any) => handleControlTypeChange(s.selectedValue)}
            className={`w-full ${theme.inputBox} border`}
            style={{ height: 28 }}
          />
        </div>

        {currentTransField.ControlType === Em?.Numeric && (
          <div>
            <label className={`block mb-0.5 ${theme.label}`}>Number Of Decimal</label>
            <input
              type="number"
              min={0}
              max={32}
              className={`w-full h-7 px-2 border ${theme.inputBox}`}
              value={currentTransField.Nbdecimal ?? 0}
              onChange={(e) =>
                onChangeField({
                  ...currentTransField,
                  Nbdecimal: parseInt(e.target.value, 10) || 0,
                  IsModified: true,
                })
              }
            />
          </div>
        )}

        {ddlFamily(currentTransField.ControlType, emControl as any) && (
          <>
            <div>
              <label className={`block mb-0.5 ${theme.label}`}>DDL Data Source</label>
              <div className="flex w-full h-[26px]">
                <div
                  className={`flex-auto min-w-0 px-1 border border-r-0 flex items-center truncate ${theme.inputBox}`}
                  title={dictEntityIdAndDto[currentTransField.EntityId]?.EntityCode}
                >
                  {isAllowEditEntityData(currentTransField.EntityId) ? (
                    <button
                      type="button"
                      className="underline truncate text-left"
                      onClick={() => {
                        /* Entity editor — optional tab open; Angular opens context menu */
                      }}
                    >
                      <i className="fa-solid fa-pencil mr-1" aria-hidden />
                      {dictEntityIdAndDto[currentTransField.EntityId]?.EntityCode ?? ''}
                    </button>
                  ) : (
                    <span className="truncate">{dictEntityIdAndDto[currentTransField.EntityId]?.EntityCode ?? ''}</span>
                  )}
                </div>
                <button
                  type="button"
                  className={`w-7 shrink-0 border ${theme.button_default}`}
                  title="Select Data Source"
                  onClick={() => setEntityPickerOpen(true)}
                >
                  <span className="wj-glyph-down" />
                </button>
              </div>
            </div>

            <div>
              <label className={`block mb-0.5 ${theme.label}`}>Filter By Field (Cascading)</label>
              <div className="flex w-full h-[26px]">
                <button
                  type="button"
                  className={`flex-auto min-w-0 px-1 border border-r-0 text-left truncate ${theme.inputBox}`}
                  onClick={() => setCascadingOpen(true)}
                >
                  {currentTransField.CascadingRelationTableParentKeyField ? (
                    <span className="underline">
                      <i className="fa-solid fa-pencil mr-1" aria-hidden />
                      {currentTransField.CascadingRelationTableParentKeyField}
                    </span>
                  ) : (
                    <span className={theme.label}>(configure)</span>
                  )}
                </button>
                <button
                  type="button"
                  className={`w-7 shrink-0 border ${theme.button_default}`}
                  title="Filter By Setting"
                  onClick={() => setCascadingOpen(true)}
                >
                  <i className="fa-solid fa-gears" aria-hidden />
                </button>
              </div>
            </div>
          </>
        )}

        <div>
          <label className={`block mb-0.5 ${theme.label}`}>Formula</label>
          <div className="flex w-full h-[26px]">
            <button
              type="button"
              className={`flex-auto min-w-0 px-2 border border-r-0 text-left italic ${theme.inputBox}`}
              title={`= ${getTransFieldFormulaDisplay()}`}
              onClick={() => setFormulaOpen(true)}
            >
              <i className="fa-solid fa-pencil mr-1" aria-hidden />
              Click To Edit Formula
            </button>
            <button
              type="button"
              className={`w-8 shrink-0 border ${theme.button_default}`}
              title="Edit Formula"
              onClick={() => setFormulaOpen(true)}
            >
              <i className="fa-solid fa-superscript" style={{ fontSize: 13 }} aria-hidden />
            </button>
          </div>
        </div>

        <div className="flex flex-wrap gap-4 pt-1">
          <label className="flex items-center gap-1 cursor-pointer">
            <input
              type="checkbox"
              checked={!!currentTransField.IsVisible}
              onChange={(e) => onChangeField({ ...currentTransField, IsVisible: e.target.checked, IsModified: true })}
            />
            <span className={theme.label}>Is Visible</span>
          </label>
          <label className="flex items-center gap-1 cursor-pointer">
            <input
              type="checkbox"
              disabled={!!currentTransField.IsPrimaryKey || !!currentTransField.IsLinkToParentPrimaryKey}
              checked={!!currentTransField.IsReadonly}
              onChange={(e) => onChangeField({ ...currentTransField, IsReadonly: e.target.checked, IsModified: true })}
            />
            <span className={theme.label}>Is Read-only</span>
          </label>
        </div>
      </div>

      {SHOW_FIELD_LAYOUT_SECTION && (flexLayout || isGridColumn) && (
        <>
          <div className={`font-semibold mt-4 mb-2 ${theme.title}`}>Field Layout:</div>
          <div className="space-y-2 pl-1">
            {isGridColumn && (
              <div>
                <label className={`block mb-0.5 ${theme.label}`}>Width (px)</label>
                <input
                  type="number"
                  min={0}
                  className={`w-full h-7 px-2 border ${theme.inputBox}`}
                  value={currentTransField.DisplayWidth ?? 0}
                  onChange={(e) =>
                    onChangeField({
                      ...currentTransField,
                      DisplayWidth: parseInt(e.target.value, 10) || 0,
                      IsModified: true,
                    })
                  }
                />
              </div>
            )}

            {!!currentTransField.DomAttribute && !isGridColumn && (
              <>
                {uiForType?.EnableHeight && (
                  <div>
                    <label className={`block mb-0.5 ${theme.label}`}>Height (px)</label>
                    <input
                      type="number"
                      min={0}
                      max={1000}
                      className={`w-full h-7 px-2 border ${theme.inputBox}`}
                      value={currentTransField.DomAttribute.HeightValue ?? 0}
                      onChange={(e) =>
                        onChangeField({
                          ...currentTransField,
                          DomAttribute: {
                            ...currentTransField.DomAttribute,
                            HeightValue: parseInt(e.target.value, 10) || 0,
                          },
                          IsModified: true,
                        })
                      }
                    />
                  </div>
                )}
                {uiForType?.EnableBackgroundColor && (
                  <div>
                    <label className={`block mb-0.5 ${theme.label}`}>Background Color</label>
                    <input
                      type="color"
                      className={`w-full h-8 border ${theme.inputBox}`}
                      value={currentTransField.DomAttribute.BackgroundColor || '#ffffff'}
                      onChange={(e) =>
                        onChangeField({
                          ...currentTransField,
                          DomAttribute: {
                            ...currentTransField.DomAttribute,
                            BackgroundColor: e.target.value,
                          },
                          IsModified: true,
                        })
                      }
                    />
                  </div>
                )}
                {uiForType?.EnableTextColor && (
                  <div>
                    <label className={`block mb-0.5 ${theme.label}`}>Label Color</label>
                    <input
                      type="color"
                      className={`w-full h-8 border ${theme.inputBox}`}
                      value={currentTransField.DomAttribute.TextColor || '#000000'}
                      onChange={(e) =>
                        onChangeField({
                          ...currentTransField,
                          DomAttribute: {
                            ...currentTransField.DomAttribute,
                            TextColor: e.target.value,
                          },
                          IsModified: true,
                        })
                      }
                    />
                  </div>
                )}
              </>
            )}
          </div>
        </>
      )}

      {SHOW_FIELD_LAYOUT_SECTION && gridFormLayout && (
        <>
          <div className={`font-semibold mt-4 mb-2 ${theme.title}`}>Field Layout:</div>
          <div className="space-y-2 pl-1">
            {!isGridColumn && (
              <div>
                <label className={`block mb-0.5 ${theme.label}`}>Width (px)</label>
                <input
                  type="number"
                  min={0}
                  className={`w-full h-7 px-2 border ${theme.inputBox}`}
                  value={gridFormFieldStyleObj.width}
                  onChange={(e) =>
                    setGridFormFieldStyleObj((g) => ({ ...g, width: parseInt(e.target.value, 10) || 0 }))
                  }
                />
              </div>
            )}
            {uiForType?.EnableHeight && (
              <div>
                <label className={`block mb-0.5 ${theme.label}`}>Height (px)</label>
                <input
                  type="number"
                  min={0}
                  max={1000}
                  className={`w-full h-7 px-2 border ${theme.inputBox}`}
                  value={gridFormFieldStyleObj.height}
                  onChange={(e) =>
                    setGridFormFieldStyleObj((g) => ({ ...g, height: parseInt(e.target.value, 10) || 0 }))
                  }
                />
              </div>
            )}
          </div>
        </>
      )}

      {entityPickerOpen && (
        <div
          data-prevent-field-setting-dismiss
          className="fixed inset-0 z-[7000] flex items-start justify-center pt-20 px-4 bg-black/40"
          onMouseDown={(e) => e.target === e.currentTarget && setEntityPickerOpen(false)}
        >
          <div
            className={`w-full max-w-sm max-h-[70vh] overflow-hidden flex flex-col rounded border shadow-lg ${theme.mainContentSection}`}
            onMouseDown={(e) => e.stopPropagation()}
          >
            <div className={`px-2 py-1 border-b flex justify-between items-center ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Datasource Selector</span>
              <button type="button" className={theme.button_default} onClick={() => setEntityPickerOpen(false)}>
                ×
              </button>
            </div>
            <div className="h-1 flex-auto overflow-y-auto p-2 space-y-1">
              {entityRows.map((e: any) => (
                <button
                  key={e.Id}
                  type="button"
                  className={`w-full text-left px-2 py-1 rounded truncate ${theme.button_default}`}
                  onClick={() => datasourceSelectedHandler(e.Id)}
                >
                  {e.EntityCode ?? e.Display ?? e.Id}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}

      <TransactionFieldCascadingModal
        isOpen={cascadingOpen}
        onClose={() => setCascadingOpen(false)}
        currentField={currentTransField}
        appTransactionData={transactionData?.AppTransactionData}
        onApply={(updated) => onChangeField(updated)}
      />

      <FieldFormulaDialog
        isOpen={formulaOpen}
        onClose={() => setFormulaOpen(false)}
        transactionData={transactionData}
        currentFieldId={currentTransField.Id}
        initialFormulaText={getTransFieldFormulaDisplay()}
        onConfirm={(payload) => {
          applyFormulaPayload(payload);
          setFormulaOpen(false);
        }}
      />
    </div>
  );
};

export default TransactionFieldEditorPanel;
