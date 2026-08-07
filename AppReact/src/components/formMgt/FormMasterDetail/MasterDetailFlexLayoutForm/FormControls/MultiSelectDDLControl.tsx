import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { MultiSelect } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';
import appHelper from '../../../../../helper/appHelper';

const DELIMITER = '|';

const normalizeIdList = (raw: unknown): string[] => {
  if (raw == null || raw === '') return [];
  const text = String(raw).trim();
  if (!text) return [];
  return text
    .split(/[|,]/)
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
};

const joinIdList = (ids: string[]): string | null => {
  const cleaned = ids.map((s) => String(s).trim()).filter((s) => s.length > 0);
  return cleaned.length > 0 ? cleaned.join(DELIMITER) : null;
};

const applyLookupItemSelectedFlags = (items: any[], selection: string[]) => {
  const selected = new Set(selection.map(String));
  items.forEach((item: any) => {
    item.isLookupItemSelected = selected.has(String(item?.Id ?? ''));
  });
};

interface MultiSelectDDLControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

/**
 * MultiSelectDDL: Wijmo MultiSelect with DDL-like layout (label left, 30px control).
 * Persists pipe-delimited LookupItem Ids on a root/sibling nvarchar field.
 * Empty / null = no selection stored (callers treat as "show all" where applicable).
 */
const MultiSelectDDLControl: React.FC<MultiSelectDDLControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange,
}) => {
  const { theme } = useTheme();
  const multiSelectRef = useRef<wjInput.MultiSelect | null>(null);
  const dataModelRef = useRef(dataModel);
  const isSyncingRef = useRef(false);

  const fieldName = fieldDto?.DataBaseFieldName;
  const fieldValue = getOneToOneFieldValue(
    dataModel.currentFormData,
    fieldDto,
    fieldName,
    '',
    layoutItemExDto,
  );
  const selection = useMemo(() => normalizeIdList(fieldValue), [fieldValue]);

  const isReadOnly =
    fieldDto?.IsFormLayoutReadOnly === true || dataModel.currentFormData?.IsLockTransaction === true;
  const isRequired = fieldDto?.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;
  const label = fieldDto?.DisplayName || fieldDto?.LabelDisplayBinding || fieldName;
  const tooltip = fieldDto?.ToolTip || fieldDto?.LabelDisplayBinding || '';
  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null &&
    fieldDto?.TransactionUnitId != null &&
    String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;

  const fieldIdStr = useMemo(() => {
    const fid = fieldDto?.Id;
    return fid != null ? String(fid) : '';
  }, [fieldDto?.Id]);

  const cascadedItemsForThisField = useMemo(() => {
    if (!fieldIdStr) return null;
    const dict = dataModel?.currentFormData?.DictCascadingFiledDataSource;
    const items = dict?.[fieldIdStr];
    return Array.isArray(items) ? items : null;
  }, [dataModel?.currentFormData?.DictCascadingFiledDataSource, fieldIdStr]);

  const standAloneItemsForThisField = useMemo(() => {
    if (!fieldIdStr) return null;
    const dictEntityItems = dataModel?.currentFormStructure?.DictStandAloneEntityDataSource;
    const dictFieldToEntityId = dataModel?.currentFormStructure?.DictStandAloneFiledIDMappingEntityID;
    const entityId = dictFieldToEntityId?.[fieldIdStr];
    const items = entityId != null ? dictEntityItems?.[String(entityId)] : null;
    return Array.isArray(items) ? items : null;
  }, [
    dataModel?.currentFormStructure?.DictStandAloneEntityDataSource,
    dataModel?.currentFormStructure?.DictStandAloneFiledIDMappingEntityID,
    fieldIdStr,
  ]);

  const itemsSource = useMemo(() => {
    const raw =
      (Array.isArray(fieldDto?.ItemSource) ? fieldDto.ItemSource : null) ??
      cascadedItemsForThisField ??
      standAloneItemsForThisField ??
      [];
    const items = raw
      .filter((x: any) => x != null && x.Id != null && String(x.Id) !== '')
      .map((item: any) => ({
        ...item,
        Id: item.Id,
        Display: item.Display ?? String(item.Id),
        isLookupItemSelected: false,
      }));
    applyLookupItemSelectedFlags(items, selection);
    return items;
    // selection applied in sync effect — keep itemsSource stable on selection-only changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fieldDto?.ItemSource, cascadedItemsForThisField, standAloneItemsForThisField]);

  useEffect(() => {
    dataModelRef.current = dataModel;
  }, [dataModel]);

  const commitSelection = useCallback(
    (ids: string[]) => {
      const nextValue = joinIdList(ids);
      const currentFormData = dataModelRef.current?.currentFormData;
      const currentRaw = getOneToOneFieldValue(
        currentFormData,
        fieldDto,
        fieldName,
        '',
        layoutItemExDto,
      );
      const currentNorm = joinIdList(normalizeIdList(currentRaw));
      if (String(currentNorm ?? '') === String(nextValue ?? '')) return;

      const nextFormData = buildFormDataWithOneToOneValue(
        currentFormData,
        fieldDto,
        fieldName,
        nextValue,
        layoutItemExDto,
      );
      onDataModelChange({
        ...dataModelRef.current,
        uiValidationErrors:
          errorText && (dataModelRef.current as any)?.uiValidationErrors
            ? (() => {
                const copy = { ...(((dataModelRef.current as any).uiValidationErrors ?? {}) as any) };
                delete copy[errorKey];
                return copy;
              })()
            : (dataModelRef.current as any)?.uiValidationErrors,
        currentFormData: nextFormData,
      });
    },
    [errorKey, errorText, fieldDto, fieldName, layoutItemExDto, onDataModelChange],
  );

  const syncSelectionToControl = useCallback(
    (selectionToSync: string[]) => {
      applyLookupItemSelectedFlags(itemsSource, selectionToSync);
      const ctl = multiSelectRef.current;
      if (!ctl) return;

      const desired = itemsSource.filter((item: any) => item.isLookupItemSelected === true);
      if (selectionToSync.length > 0 && desired.length === 0) {
        // Stored ids not in current ItemsSource yet (cascading still loading) — keep control as-is.
        return;
      }

      isSyncingRef.current = true;
      try {
        ctl.checkedItems = desired;
      } catch (e) {
        appHelper.debugLog('MultiSelectDDLControl: sync checkedItems failed', e);
      } finally {
        setTimeout(() => {
          isSyncingRef.current = false;
        }, 0);
      }
    },
    [itemsSource],
  );

  useEffect(() => {
    syncSelectionToControl(selection);
  }, [selection, syncSelectionToControl]);

  useEffect(() => {
    syncSelectionToControl(selection);
  }, [itemsSource, selection, syncSelectionToControl]);

  // Parse style string to object (parity with DDLControl)
  const styleObject: React.CSSProperties = {};
  if (layoutItemExDto?.StyleLayoutInfo) {
    const styles = String(layoutItemExDto.StyleLayoutInfo)
      .split(';')
      .filter((s: string) => s.trim());
    styles.forEach((style: string) => {
      const [key, value] = style.split(':').map((s: string) => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  return (
    <div className="w-full flex items-start gap-2" style={styleObject} title={tooltip}>
      {!isHideLabel && (
        <div className="flex-shrink-0 min-w-[120px]">
          <label className={`text-xs font-semibold ${theme.title}`}>
            {label} {requiredMark}
          </label>
        </div>
      )}
      <div className="w-1 flex-auto">
        <MultiSelect
          initialized={(sender: wjInput.MultiSelect) => {
            multiSelectRef.current = sender;
            // Collapsed look: single-line input like ComboBox / DDL.
            sender.showSelectAllCheckbox = true;
            syncSelectionToControl(selection);
          }}
          itemsSource={itemsSource}
          displayMemberPath="Display"
          checkedMemberPath="isLookupItemSelected"
          placeholder=""
          isDisabled={isReadOnly}
          style={{ height: '30px', fontSize: '11px', width: '100%' }}
          maxDropDownHeight={220}
          showDropDownButton={true}
          showSelectAllCheckbox={true}
          checkedItemsChanged={(sender: wjInput.MultiSelect) => {
            if (isSyncingRef.current || isReadOnly) return;
            const ids = (sender.checkedItems ?? []).map((item: any) => String(item?.Id ?? '')).filter(Boolean);
            commitSelection(ids);
          }}
          isDroppedDownChanged={(sender: wjInput.MultiSelect) => {
            if (isSyncingRef.current || isReadOnly) return;
            if (!sender.isDroppedDown) {
              const ids = (sender.checkedItems ?? []).map((item: any) => String(item?.Id ?? '')).filter(Boolean);
              commitSelection(ids);
            }
          }}
        />
        {errorText ? <div className="text-[11px] text-red-500 mt-0.5">{errorText}</div> : null}
      </div>
    </div>
  );
};

export default MultiSelectDDLControl;
