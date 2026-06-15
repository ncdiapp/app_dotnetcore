import React, { useMemo, useState, useEffect, useRef } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { appTransactionService } from '../../../../../webapi/apptransactionsvc';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';
import appHelper from '../../../../../helper/appHelper';
import { useEnumValues } from '../../../../../hooks/useEnumDictionary';

interface DDLControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const DDLControl: React.FC<DDLControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange
}) => {
  const { theme } = useTheme();
  const controlTypeEnum = useEnumValues('EmAppControlType');
  const [itemsSource, setItemsSource] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [isLoading, setIsLoading] = useState(false);
  const [queryText, setQueryText] = useState<string>('');
  const [isSelectorOpen, setIsSelectorOpen] = useState(false);
  const [selectorPos, setSelectorPos] = useState<{ top: number; left: number } | null>(null);
  const [selectorItemsCv, setSelectorItemsCv] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [selectorLoading, setSelectorLoading] = useState(false);
  const comboBoxRef = useRef<wjInput.ComboBox | null>(null);
  const dataModelRef = useRef<any>(dataModel);
  const pendingSelectedValueRef = useRef<any>(undefined);
  const isProgrammaticSelectionRef = useRef(false);
  const lastCascadeKeyRef = useRef<string>('');
  const cascadingInFlightRef = useRef(false);
  const lastLoadKeyRef = useRef<string>('');
  const lastUserSelectedValueRef = useRef<any>(undefined);
  const userInteractedRef = useRef(false);
  const suppressNextSearchTextChangeRef = useRef(false);
  const interactionCleanupRef = useRef<(() => void) | null>(null);
  const selectorHostRef = useRef<HTMLDivElement | null>(null);
  const selectorGridRef = useRef<any>(null);

  const fieldName = fieldDto.DataBaseFieldName;
  const fieldValue = getOneToOneFieldValue(dataModel.currentFormData, fieldDto, fieldName, undefined, layoutItemExDto);

  const selectedDisplay = useMemo(() => {
    const idStr = String(fieldValue ?? '');
    if (!idStr) return '';

    const candidates: any[] = [
      ...(Array.isArray(itemsSource?.items) ? itemsSource.items : []),
      ...(Array.isArray(selectorItemsCv?.items) ? selectorItemsCv.items : []),
    ];
    const hit = candidates.find((x) => x != null && String(x.Id ?? '') === idStr);
    return hit?.Display ?? '';
  }, [fieldValue, itemsSource, selectorItemsCv]);
  
  // Check if field is read-only
  const isReadOnly = fieldDto.IsFormLayoutReadOnly === true || 
                    dataModel.currentFormData?.IsLockTransaction === true;
  
  // Get required mark (UI only). Note: Wijmo ComboBox `isRequired` is forced false so user can clear to null.
  const isRequired = fieldDto.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;

  // UI required validation message (set by FormMainMenus on Save)
  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null && fieldDto?.TransactionUnitId != null && String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;
  
  // Get label
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldName;
  
  // Get tooltip
  const tooltip = fieldDto.ToolTip || fieldDto.LabelDisplayBinding || '';

  const controlType = fieldDto?.ControlType;
  const isSearchAbleDDL = useMemo(() => {
    return controlType === controlTypeEnum?.SearchAbleDDL;
  }, [controlType, controlTypeEnum?.SearchAbleDDL]);
  const isSearchable = useMemo(() => {
    return (
      controlType === controlTypeEnum?.SearchAbleDDL ||
      controlType === controlTypeEnum?.AutoComplete
    );
  }, [controlType, controlTypeEnum?.AutoComplete, controlTypeEnum?.SearchAbleDDL]);

  const buildLookupItemsCv = (lookupItemsRaw: any[] | null | undefined) => {
    const raw = (Array.isArray(lookupItemsRaw) ? lookupItemsRaw : []).filter((x) => x != null);
    const allowEmpty = fieldDto?.IsAllowEmpty !== false;
    const withEmpty = allowEmpty ? [{ Id: null, Display: '' }, ...raw] : raw;
    return new CollectionView(withEmpty);
  };

  const buildLookupItemsCvForSelector = (lookupItemsRaw: any[] | null | undefined) => {
    const raw = (Array.isArray(lookupItemsRaw) ? lookupItemsRaw : []).filter((x) => x != null);
    return new CollectionView(raw);
  };

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

  useEffect(() => {
    dataModelRef.current = dataModel;
  }, [dataModel]);

  const mergeCascadingRootResponse = (baseFormData: any, cascadingResponseFormData: any) => {
    // Angular parity: cascading response is used mainly to update DictCascadingFiledDataSource and related flags.
    // Do NOT replace DictOneToOneFields/DictSiblingOneToOneFields wholesale, or we may overwrite user-edited values.
    if (!cascadingResponseFormData) return baseFormData;
    return {
      ...baseFormData,
      DictCascadingFiledDataSource: cascadingResponseFormData.DictCascadingFiledDataSource ?? baseFormData?.DictCascadingFiledDataSource,
      IsChangedNeedToCascadingFiedIds: cascadingResponseFormData.IsChangedNeedToCascadingFiedIds ?? baseFormData?.IsChangedNeedToCascadingFiedIds,
      IsUsedCascadingDataSourceFiedIds: cascadingResponseFormData.IsUsedCascadingDataSourceFiedIds ?? baseFormData?.IsUsedCascadingDataSourceFiedIds,
      CurrentCascadingFieldId: cascadingResponseFormData.CurrentCascadingFieldId ?? baseFormData?.CurrentCascadingFieldId,
      CurrentCascadingUnitId: cascadingResponseFormData.CurrentCascadingUnitId ?? baseFormData?.CurrentCascadingUnitId,
    };
  };

  const fetchItemSource = async (q: string | null) => {
    const currentFormData = dataModelRef.current?.currentFormData;
    const transFieldId = fieldDto?.Id ?? layoutItemExDto?.Id;
    if (!currentFormData || transFieldId == null) return;

    const loadKey = `${String(currentFormData?.TransactionId ?? '')}|${String(transFieldId)}|${String(q ?? '')}`;
    if (lastLoadKeyRef.current === loadKey) return;
    lastLoadKeyRef.current = loadKey;

    const payload = {
      ...currentFormData,
      CurrentAutoCompleteFieldId: String(transFieldId),
      CurrentAutoCompleteFieldQueryText: q,
      CurrentEditRowDto: null,
    };

    const shouldShowLoading = itemsSource == null || (itemsSource as any)?.items?.length === 0;
    if (shouldShowLoading) setIsLoading(true);
    try {
      const lookupItems = await appTransactionService.RetrieveAutoCompleteDDLEntityItemSource(payload);
      setItemsSource(buildLookupItemsCv(lookupItems));
    } catch (e) {
      appHelper.debugLog('DDLControl: load itemSource failed', { fieldId: transFieldId, q, e });
      setItemsSource((prev) => (prev && (prev as any)?.items?.length > 0 ? prev : buildLookupItemsCv([])));
    } finally {
      if (shouldShowLoading) setIsLoading(false);
    }
  };

  const clampPopupPos = (top: number, left: number, width: number, height: number) => {
    const maxTop = Math.max(0, window.innerHeight - height - 10);
    const maxLeft = Math.max(0, window.innerWidth - width - 10);
    return {
      top: Math.min(Math.max(0, top), maxTop),
      left: Math.min(Math.max(0, left), maxLeft),
    };
  };

  const openSelectorPopup = async (clickEvent: React.MouseEvent) => {
    if (!isSearchAbleDDL) return;
    const currentFormData = dataModelRef.current?.currentFormData;
    const transFieldId = fieldDto?.Id ?? layoutItemExDto?.Id;
    if (!currentFormData || transFieldId == null) return;

    setIsSelectorOpen(true);
    const w = 500;
    const h = 700;
    const pos = clampPopupPos(clickEvent.clientY + 15, clickEvent.clientX - 20, w, h);
    setSelectorPos(pos);

    setSelectorLoading(true);
    try {
      const payload = {
        ...currentFormData,
        CurrentAutoCompleteFieldId: String(transFieldId),
        CurrentAutoCompleteFieldQueryText: null,
        CurrentEditRowDto: null,
      };
      const lookupItems = await appTransactionService.RetrieveAutoCompleteDDLEntityItemSource(payload);
      const cv = buildLookupItemsCvForSelector(
        (Array.isArray(lookupItems) ? lookupItems : []).map((x: any) => ({
          ...x,
          isSelected: String(x?.Id ?? '') === String(fieldValue ?? ''),
        })),
      );
      setSelectorItemsCv(cv);
    } catch (e) {
      appHelper.debugLog('DDLControl: selector popup load failed', { transFieldId, e });
      setSelectorItemsCv(buildLookupItemsCvForSelector([]));
    } finally {
      setSelectorLoading(false);
    }
  };

  const closeSelectorPopup = () => {
    setIsSelectorOpen(false);
  };

  const applySelectorItem = (item: any) => {
    if (!item) return;
    const currentFormData = dataModelRef.current?.currentFormData;
    const newValue = item?.Id ?? null;

    // Angular parity: after applying from selector, the field's lookup source becomes the selector list.
    setItemsSource(buildLookupItemsCv(selectorItemsCv?.items ?? []));

    // Prevent a follow-up auto-complete fetch from the text update after apply.
    suppressNextSearchTextChangeRef.current = true;

    const nextFormData = buildFormDataWithOneToOneValue(currentFormData, fieldDto, fieldName, newValue, layoutItemExDto);
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

    closeSelectorPopup();
  };

  const clearSearchAbleDDLValue = () => {
    const currentFormData = dataModelRef.current?.currentFormData;
    const nextFormData = buildFormDataWithOneToOneValue(currentFormData, fieldDto, fieldName, null, layoutItemExDto);
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
  };

  const applySelectorSelectedAndClose = () => {
    const items = Array.isArray(selectorItemsCv?.items) ? selectorItemsCv.items : [];
    const item = items.find((x) => x != null && x.isSelected === true) ?? null;
    if (!item) return;
    applySelectorItem(item);
  };

  // Initial load (DDL / SearchAbleDDL / AutoComplete).
  useEffect(() => {
    const hasInline = fieldDto?.ItemSource && Array.isArray(fieldDto.ItemSource);
    if (hasInline) {
      setItemsSource(buildLookupItemsCv(fieldDto.ItemSource));
      return;
    }

    // Cascading datasource (Angular parity): if server already provided DictCascadingFiledDataSource for this field, use it directly.
    if (cascadedItemsForThisField) {
      setItemsSource(buildLookupItemsCv(cascadedItemsForThisField));
      return;
    }

    // Standalone datasource (Angular parity): for normal DDLs (including cascading parents),
    // item source is provided by structure: DictStandAloneEntityDataSource[fieldId].
    if (standAloneItemsForThisField) {
      setItemsSource(buildLookupItemsCv(standAloneItemsForThisField));
      return;
    }

    // Angular parity:
    // - On form open/refresh, DDL item sources should come from server-provided dictionaries
    //   (e.g., DictCascadingFiledDataSource) and should NOT trigger per-DDL RetrieveAutoComplete... calls.
    // - RetrieveAutoCompleteDDLEntityItemSource is only used on user interaction for SearchAbleDDL/AutoComplete.
    //
    // So here we intentionally do nothing when no inline/cascaded items exist.
    // Items will be populated later via cascading API response or other explicit user actions.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    fieldDto?.Id,
    fieldDto?.ItemSource,
    dataModel?.currentFormData?.TransactionId,
    cascadedItemsForThisField,
    standAloneItemsForThisField,
    isSearchable,
  ]);

  // Debounced search (SearchAbleDDL / AutoComplete).
  useEffect(() => {
    // Angular parity + requested UX: SearchAbleDDL "search" is done via the selector popup,
    // not by typing in the field. Only AutoComplete uses text-based querying.
    if (!isSearchable || isSearchAbleDDL) return;
    const trimmed = queryText?.trim?.() ?? '';
    if (!trimmed) return;
    const h = window.setTimeout(() => {
      void fetchItemSource(trimmed);
    }, 250);
    return () => window.clearTimeout(h);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isSearchable, isSearchAbleDDL, queryText]);

  // Handle value change
  const handleSelectedIndexChanged = (sender: wjInput.ComboBox) => {
    const newValue = sender.selectedValue;

    if (isProgrammaticSelectionRef.current) return;
    if (!userInteractedRef.current) return;
    // Only treat as user change when control has focus (prevents programmatic selection updates from triggering cascade).
    if (!(sender as any)?.containsFocus) return;
    const currentFormData = dataModelRef.current?.currentFormData;
    const currentRaw = getOneToOneFieldValue(currentFormData, fieldDto, fieldName, undefined, layoutItemExDto);
    if (String(currentRaw ?? '') === String(newValue ?? '')) return;

    lastUserSelectedValueRef.current = newValue;

    // Angular parity: selecting a row should not trigger a follow-up auto-complete fetch
    // based on the selected item's display text.
    if (isSearchable) {
      suppressNextSearchTextChangeRef.current = true;
    }

    const nextFormData = buildFormDataWithOneToOneValue(currentFormData, fieldDto, fieldName, newValue, layoutItemExDto);
    const nextDataModel = {
      ...dataModelRef.current,
      uiValidationErrors:
        errorText && (dataModelRef.current as any)?.uiValidationErrors
          ? (() => {
              const copy = { ...(((dataModelRef.current as any).uiValidationErrors ?? {}) as any) };
              delete copy[errorKey];
              return copy;
            })()
          : (dataModelRef.current as any)?.uiValidationErrors,
      currentFormData: nextFormData
    };
    onDataModelChange(nextDataModel);

    // Root cascading (Angular: if field is in IsChangedNeedToCascadingFiedIds then trigger GetRootUnitFieldTriggerCascadingDataSource).
    const fieldId = fieldDto?.Id;
    const changedList: any[] = currentFormData?.IsChangedNeedToCascadingFiedIds ?? [];
    const needCascade = fieldId != null && Array.isArray(changedList) && changedList.map(String).includes(String(fieldId));
    if (!needCascade) return;

    const cascadeKey = `${String(fieldId)}|${String(newValue ?? '')}`;
    if (cascadingInFlightRef.current && lastCascadeKeyRef.current === cascadeKey) return;
    lastCascadeKeyRef.current = cascadeKey;

    const unitId = fieldDto?.TransactionUnitId ?? currentFormData?.RootUnitId ?? null;
    const payload = {
      ...nextFormData,
      CurrentCascadingFieldId: String(fieldId),
      CurrentCascadingUnitId: unitId != null ? String(unitId) : null,
      DictCascadingFiledDataSource: null,
    };

    void (async () => {
      cascadingInFlightRef.current = true;
      try {
        const cascadingResp = await appTransactionService.GetRootUnitFieldTriggerCascadingDataSource(payload);
        if (cascadingResp) {
          const latestDm = dataModelRef.current;
          const latestBaseFormData = latestDm?.currentFormData;
          const baseWithUserValue = buildFormDataWithOneToOneValue(latestBaseFormData, fieldDto, fieldName, lastUserSelectedValueRef.current, layoutItemExDto);
          const mergedFormData = mergeCascadingRootResponse(baseWithUserValue, cascadingResp);
          onDataModelChange({
            ...latestDm,
            currentFormData: mergedFormData,
          });
        }
      } catch (e) {
        appHelper.debugLog('DDLControl: cascading failed', { fieldId, unitId, e });
      } finally {
        cascadingInFlightRef.current = false;
        // release the UI lock after cascading finishes
        window.setTimeout(() => {
          lastUserSelectedValueRef.current = undefined;
        }, 0);
      }
    })();
  };

  const resolveDbFieldNameByFieldId = (structure: any, targetFieldIdStr: string): { unitId: number | null; dbFieldName: string | null } => {
    const st = structure ?? {};
    const dictUnitNameToId = st.DictTransactionUnitIdFiledNameFiledID ?? {};
    const dictFieldUnit = st.DictFieldIdUnitId ?? {};
    const unitIdRaw = dictFieldUnit?.[targetFieldIdStr] ?? dictFieldUnit?.[String(Number(targetFieldIdStr))];
    const unitId = unitIdRaw != null && unitIdRaw !== '' ? Number(unitIdRaw) : null;
    for (const uid of Object.keys(dictUnitNameToId || {})) {
      const nameToId = dictUnitNameToId[uid];
      if (!nameToId || typeof nameToId !== 'object') continue;
      for (const [dbName, fid] of Object.entries(nameToId)) {
        if (String(fid ?? '') === targetFieldIdStr) {
          return { unitId, dbFieldName: String(dbName) };
        }
      }
    }
    return { unitId, dbFieldName: null };
  };

  /** When clearing a cascading parent, clear child DDL values and blank their cascading item sources (Angular parity). */
  const clearCascadingChildrenForParent = (baseFormData: any, parentFieldIdStr: string): any => {
    const structure = dataModelRef.current?.currentFormStructure ?? {};
    const dictCascadedParent = structure.DictCascadedIdParentField ?? {};
    const dict = { ...(baseFormData?.DictCascadingFiledDataSource ?? {}) };
    let nextFormData = baseFormData;

    const clearOneField = (childFieldIdStr: string) => {
      // Clear lookup items for child so UI doesn't keep stale options
      dict[childFieldIdStr] = [];

      // Clear child value in root/sibling dictionaries
      const resolved = resolveDbFieldNameByFieldId(structure, childFieldIdStr);
      if (!resolved.dbFieldName) return;

      const rootUnitId = nextFormData?.RootUnitId;
      const isSibling = resolved.unitId != null && rootUnitId != null && Number(resolved.unitId) !== Number(rootUnitId);
      if (isSibling) {
        const sibUnitKey = String(resolved.unitId);
        const sib = (nextFormData.DictSiblingOneToOneFields?.[sibUnitKey] ?? {}) as any;
        nextFormData = {
          ...nextFormData,
          DictSiblingOneToOneFields: {
            ...(nextFormData.DictSiblingOneToOneFields ?? {}),
            [sibUnitKey]: {
              ...sib,
              [resolved.dbFieldName]: null,
            },
          },
          IsDirty: true,
        };
      } else {
        nextFormData = {
          ...nextFormData,
          DictOneToOneFields: {
            ...(nextFormData.DictOneToOneFields ?? {}),
            [resolved.dbFieldName]: null,
          },
          IsDirty: true,
        };
      }
    };

    const clearRecursive = (pId: string) => {
      const children = Object.keys(dictCascadedParent || {}).filter((childId) => String(dictCascadedParent[childId] ?? '') === pId);
      children.forEach((childId) => {
        const childStr = String(childId);
        clearOneField(childStr);
        clearRecursive(childStr);
      });
    };

    clearRecursive(parentFieldIdStr);
    return {
      ...nextFormData,
      DictCascadingFiledDataSource: dict,
    };
  };

  const clearComboBoxValue = (sender?: wjInput.ComboBox | null) => {
    if (isReadOnly) return;
    const cb = sender ?? comboBoxRef.current;
    const currentFormData = dataModelRef.current?.currentFormData;
    let nextFormData = buildFormDataWithOneToOneValue(currentFormData, fieldDto, fieldName, null, layoutItemExDto);

    // If this field is a cascading parent, clear dependent child DDLs immediately and blank their item sources.
    const parentFieldId = fieldDto?.Id;
    const changedList: any[] = currentFormData?.IsChangedNeedToCascadingFiedIds ?? [];
    const needCascade = parentFieldId != null && Array.isArray(changedList) && changedList.map(String).includes(String(parentFieldId));
    if (needCascade) {
      nextFormData = clearCascadingChildrenForParent(nextFormData, String(parentFieldId));
    }

    const nextDataModel = {
      ...dataModelRef.current,
      uiValidationErrors:
        errorText && (dataModelRef.current as any)?.uiValidationErrors
          ? (() => {
              const copy = { ...(((dataModelRef.current as any).uiValidationErrors ?? {}) as any) };
              delete copy[errorKey];
              return copy;
            })()
          : (dataModelRef.current as any)?.uiValidationErrors,
      currentFormData: nextFormData
    };
    onDataModelChange(nextDataModel);
    if (cb) applySelectedValue(cb, null);

    // Also trigger cascading API so server can rebuild cascading dictionaries based on null parent.
    if (needCascade && parentFieldId != null) {
      const unitId = fieldDto?.TransactionUnitId ?? currentFormData?.RootUnitId ?? null;
      const payload = {
        ...nextFormData,
        CurrentCascadingFieldId: String(parentFieldId),
        CurrentCascadingUnitId: unitId != null ? String(unitId) : null,
        DictCascadingFiledDataSource: null,
      };
      void (async () => {
        cascadingInFlightRef.current = true;
        lastCascadeKeyRef.current = `${String(parentFieldId)}|`;
        try {
          const cascadingResp = await appTransactionService.GetRootUnitFieldTriggerCascadingDataSource(payload);
          if (cascadingResp) {
            const latestDm = dataModelRef.current;
            const latestBaseFormData = latestDm?.currentFormData;
            const baseWithUserValue = buildFormDataWithOneToOneValue(latestBaseFormData, fieldDto, fieldName, null, layoutItemExDto);
            const mergedFormData = mergeCascadingRootResponse(baseWithUserValue, cascadingResp);
            onDataModelChange({
              ...latestDm,
              currentFormData: mergedFormData,
            });
          }
        } catch (e) {
          appHelper.debugLog('DDLControl: cascading clear failed', { parentFieldId, unitId, e });
        } finally {
          cascadingInFlightRef.current = false;
          window.setTimeout(() => {
            lastUserSelectedValueRef.current = undefined;
          }, 0);
        }
      })();
    }
  };

  const canApplySelection = (sender: wjInput.ComboBox | null) => {
    const cb: any = sender;
    return !!cb && !!cb.collectionView && Array.isArray(cb.collectionView?.items);
  };

  const applySelectedValue = (sender: wjInput.ComboBox | null, value: any) => {
    if (!sender) return;
    if (value === undefined) return;

    // If ComboBox internal collectionView isn't ready yet, defer to avoid Wijmo crashing in _getDisplayText.
    if (!canApplySelection(sender)) {
      pendingSelectedValueRef.current = value;
      return;
    }

    // Wijmo selection stabilization (Angular parity / cascading-safe).
    window.setTimeout(() => {
      if (!canApplySelection(sender)) return;
      isProgrammaticSelectionRef.current = true;
      try {
        sender.selectedValue = value ?? null;
      } finally {
        window.setTimeout(() => {
          isProgrammaticSelectionRef.current = false;
        }, 0);
      }
    }, 0);
  };

  // Initialize ComboBox
  const initializeComboBox = (sender: wjInput.ComboBox) => {
    comboBoxRef.current = sender;
    if (sender) {
      // IMPORTANT: allow clearing to null even if field metadata says required.
      // Wijmo ComboBox will otherwise reject null and snap back.
      try {
        (sender as any).isRequired = false;
      } catch {
        // ignore
      }
      sender.selectedIndexChanged.addHandler(handleSelectedIndexChanged);

      // Clear on Backspace/Delete (Angular parity: clearing a DDL should set null)
      try {
        const host = (sender as any).hostElement as HTMLElement | null;
        if (host) {
          host.addEventListener('keydown', (e: any) => {
            if (isReadOnly) return;
            if (e?.key === 'Backspace' || e?.key === 'Delete') {
              e.preventDefault();
              e.stopPropagation();
              clearComboBoxValue(sender);
            }
          });
        }
      } catch {
        // ignore
      }

      if (isSearchable && !isSearchAbleDDL) {
        sender.textChanged.addHandler((s: any) => {
          const cb = (s?.control ?? s) as wjInput.ComboBox;
          if (isProgrammaticSelectionRef.current) return;
          if (!userInteractedRef.current) return;
          if (!(cb as any)?.containsFocus) return;
          if (suppressNextSearchTextChangeRef.current) {
            suppressNextSearchTextChangeRef.current = false;
            return;
          }
          const text = cb?.text ?? '';
          setQueryText(text);
          if (text && text.trim().length > 0 && cb) {
            cb.isDroppedDown = true;
          }
        });
      }

      // Mark "user interacted" only on real user input (mouse/keyboard).
      // This prevents initial binding/programmatic selection from triggering cascading/search calls.
      if (!interactionCleanupRef.current && sender.hostElement) {
        const el = sender.hostElement as HTMLElement;
        const onMouseDown = () => {
          userInteractedRef.current = true;
        };
        const onKeyDown = () => {
          userInteractedRef.current = true;
        };
        el.addEventListener('mousedown', onMouseDown, { passive: true });
        el.addEventListener('keydown', onKeyDown);
        interactionCleanupRef.current = () => {
          el.removeEventListener('mousedown', onMouseDown);
          el.removeEventListener('keydown', onKeyDown);
        };
      }

      // Defer initial value until wijmo internal collectionView is ready.
      applySelectedValue(sender, fieldValue);
    }
  };

  useEffect(() => {
    return () => {
      if (interactionCleanupRef.current) interactionCleanupRef.current();
      interactionCleanupRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (!isSelectorOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closeSelectorPopup();
    };
    const onMouseDown = (e: MouseEvent) => {
      const host = selectorHostRef.current;
      if (!host) return;
      const target = e.target as Node | null;
      if (target && !host.contains(target)) closeSelectorPopup();
    };
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('mousedown', onMouseDown);
    return () => {
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('mousedown', onMouseDown);
    };
  }, [isSelectorOpen]);

  // Keep selection in sync if value changes externally.
  useEffect(() => {
    if (!comboBoxRef.current) return;
    // While cascading is in-flight, keep the user's last selection to prevent UI "revert" flashes.
    const preferredValue =
      cascadingInFlightRef.current && lastUserSelectedValueRef.current !== undefined
        ? lastUserSelectedValueRef.current
        : fieldValue;
    applySelectedValue(comboBoxRef.current, preferredValue);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fieldValue]);

  // Apply any pending value after itemsSource updates/binds.
  useEffect(() => {
    const cb = comboBoxRef.current;
    if (!cb) return;
    if (!canApplySelection(cb)) return;
    if (pendingSelectedValueRef.current === undefined) return;
    const v = pendingSelectedValueRef.current;
    pendingSelectedValueRef.current = undefined;
    applySelectedValue(cb, v);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [itemsSource]);

  // Get validation class (TODO: implement validation)
  const validationClass = ''; // TODO: Add validation CSS class

  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  // Only show loading UI when user-triggered searchable fetch is running.
  if (isSearchable && isLoading && (!itemsSource || itemsSource.items?.length === 0)) {
    return (
      <div className="w-full flex items-start gap-2">
        {!isHideLabel && (
          <div className="flex-shrink-0 min-w-[120px]">
            <label className={`text-xs font-semibold ${theme.title}`}>
              {label} {requiredMark}
            </label>
          </div>
        )}
        <div className={`w-1 flex-auto h-[30px] px-2 flex items-center border rounded ${theme.inputBox}`}>
          <div className="text-xs text-gray-400">Loading...</div>
        </div>
      </div>
    );
  }

  // Parse style string to object
  const styleObject: React.CSSProperties = {};
  if (layoutItemExDto.StyleLayoutInfo) {
    const styles = layoutItemExDto.StyleLayoutInfo.split(';').filter((s: string) => s.trim());
    styles.forEach((style: string) => {
      const [key, value] = style.split(':').map((s: string) => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  return (
    <div 
      className={`w-full flex items-start gap-2 ${validationClass}`}
      style={styleObject}
      title={tooltip}
    >
      {!isHideLabel && (
        <div className="flex-shrink-0 min-w-[120px]">
          <label className={`text-xs font-semibold ${theme.title}`}>
            {label} {requiredMark}
          </label>
        </div>
      )}
        <div className="w-1 flex-auto">
        <div className="w-full flex items-center">
          <div className="w-1 flex-auto">
            {isSearchAbleDDL ? (
              <div className="w-full flex items-center">
                <input
                  className={`w-1 flex-auto h-[30px] px-2 text-xs border rounded-l ${theme.inputBox}`}
                  readOnly={true}
                  value={selectedDisplay ?? ''}
                  onKeyDown={(e) => {
                    if (isReadOnly) return;
                    if (e.key === 'Backspace' || e.key === 'Delete') {
                      e.preventDefault();
                      e.stopPropagation();
                      clearSearchAbleDDLValue();
                    }
                  }}
                />
                {!isReadOnly && (
                  <button
                    type="button"
                    className={`w-[34px] h-[30px] border border-l-0 rounded-r ${theme.inputBox}`}
                    onClick={openSelectorPopup}
                    title="Select from list"
                  >
                    <i className="fa-solid fa-magnifying-glass text-xs" />
                  </button>
                )}
              </div>
            ) : (
              <ComboBox
                itemsSource={itemsSource}
                displayMemberPath="Display"
                selectedValuePath="Id"
                // Keep null as null (do not coerce to ''), so clearing truly becomes null.
                selectedValue={fieldValue ?? null}
                isRequired={false}
                isEditable={isSearchable}
                isReadOnly={isReadOnly}
                initialized={initializeComboBox}
                style={{ height: '30px', fontSize: '11px', width: '100%' }}
              />
            )}
          </div>
        </div>

        {isSelectorOpen && (
          <div className="fixed inset-0 z-[5000]" style={{ backgroundColor: 'rgba(0,0,0,0.15)' }}>
            <div
              ref={selectorHostRef}
              className={`absolute rounded-md overflow-hidden border ${theme.mainContentSection}`}
              style={{
                top: selectorPos?.top ?? 80,
                left: selectorPos?.left ?? 80,
                width: 500,
                height: 700,
              }}
            >
              <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
                <div className={`text-sm font-semibold ${theme.title}`}>Select {label}</div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                    onClick={applySelectorSelectedAndClose}
                    disabled={selectorLoading}
                    title="Select & Close"
                  >
                    Select & Close
                  </button>
                  <button
                    type="button"
                    className={`px-2 py-1 text-sm rounded-[4px] ${theme.button_default}`}
                    onClick={closeSelectorPopup}
                    title="Close"
                  >
                    ×
                  </button>
                </div>
              </div>
              <div className="w-full h-[calc(100%-44px)] p-3 overflow-hidden">
                {selectorLoading ? (
                  <div className="w-full h-full flex items-center justify-center text-xs">Loading...</div>
                ) : (
                  <FlexGrid
                    itemsSource={selectorItemsCv}
                    headersVisibility="Column"
                    isReadOnly={true}
                    selectionMode="Row"
                    allowSorting={false}
                    className="w-full h-full"
                    initialized={(g: any) => {
                      selectorGridRef.current = g;
                    }}
                    selectionChanged={(s: any) => {
                      const flex = s?.control ?? s;
                      const rowIndex = flex?.selection?.row;
                      const item = rowIndex != null ? flex?.rows?.[rowIndex]?.dataItem : null;
                      const items = Array.isArray(selectorItemsCv?.items) ? selectorItemsCv.items : [];
                      items.forEach((x: any) => {
                        if (!x) return;
                        x.isSelected = item != null && String(x.Id ?? '') === String(item?.Id ?? '');
                      });
                      try {
                        selectorItemsCv?.refresh?.();
                      } catch {
                        // ignore
                      }
                      try {
                        (selectorGridRef.current?.control ?? selectorGridRef.current)?.invalidate?.();
                      } catch {
                        // ignore
                      }
                    }}
                  >
                    <FlexGridFilter />
                    <FlexGridColumn header="Select" binding="" width={80} allowSorting={false} isReadOnly={false}>
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(cell: any) => (
                          <div className="w-full h-full flex items-center justify-center">
                            <input
                              type="radio"
                              checked={cell?.item?.isSelected === true}
                              onChange={() => {
                                const items = Array.isArray(selectorItemsCv?.items) ? selectorItemsCv.items : [];
                                items.forEach((x: any) => {
                                  if (!x) return;
                                  x.isSelected = String(x.Id ?? '') === String(cell?.item?.Id ?? '');
                                });
                                try {
                                  selectorItemsCv?.refresh?.();
                                } catch {
                                  // ignore
                                }
                                try {
                                  const flex = selectorGridRef.current?.control ?? selectorGridRef.current;
                                  if (flex && cell?.item) {
                                    const idx = flex.rows?.findIndex?.((r: any) => r?.dataItem === cell.item) ?? -1;
                                    if (idx >= 0) flex.select(idx, 0);
                                  }
                                  flex?.invalidate?.();
                                } catch {
                                  // ignore
                                }
                              }}
                            />
                          </div>
                        )}
                      />
                    </FlexGridColumn>
                    <FlexGridColumn binding="Id" header="Id" width={120} isReadOnly={true} />
                    <FlexGridColumn binding="Display" header="Display" width="*" isReadOnly={true} />
                  </FlexGrid>
                )}
              </div>
            </div>
          </div>
        )}

        {errorText && <div className="text-xs text-red-600 mt-0.5">{errorText}</div>}
      </div>
    </div>
  );
};

export default DDLControl;

