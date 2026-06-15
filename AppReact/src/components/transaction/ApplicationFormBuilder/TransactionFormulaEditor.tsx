/**
 * Calculation Validation Flow (Transaction Formula Editor)
 * Ported from AngularJS: TransactionFormulaEditor.cshtml + transactionFormulaEditorCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionFormulaEditor.cshtml
 *            C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Transaction\transactionFormulaEditorCtrl.js
 */

import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import FieldFormulaDialog from '../../formMgt/FormDesign/FieldFormulaDialog';

// EmAppFormularType (from APP.Components.Dto AppEnums.cs)
const EmAppFormularType = {
  Assignment: 1,
  BooleanExpressionWarning: 2,
  BooleanExpressionError: 3,
  SubscribeFromGridColumnAggregation: 4,
  SubscribeFromParentLevelField: 5,
  BooleanExpressionDeleteRow: 6,
  SqlScarlarAssignment: 7,
  SqlTupleAssignment: 8,
};

// Operation type options per unit (Angular: appFormularTypeList, appFormularTypeList_NoChildUnit, etc.)
const APP_FORMULAR_TYPE_LIST = [
  { Id: 1, Display: 'Assignment' },
  { Id: 7, Display: 'SQL Scalar Assignment' },
  { Id: 8, Display: 'SQL Tuple Assignment' },
  { Id: 2, Display: 'Boolean Expression Warning' },
  { Id: 3, Display: 'Boolean Expression Error' },
  { Id: 4, Display: 'Aggregate From Grid Column' },
  { Id: 5, Display: 'Subscribe from Parent Level Field' },
  { Id: 6, Display: 'Boolean Expression Delete Row' },
];
const APP_FORMULAR_TYPE_LIST_NO_CHILD = [
  { Id: 1, Display: 'Assignment' },
  { Id: 7, Display: 'SQL Scalar Assignment' },
  { Id: 8, Display: 'SQL Tuple Assignment' },
  { Id: 2, Display: 'Boolean Expression Warning' },
  { Id: 3, Display: 'Boolean Expression Error' },
  { Id: 5, Display: 'Subscribe from Parent Level Field' },
  { Id: 6, Display: 'Boolean Expression Delete Row' },
];
const APP_FORMULAR_TYPE_LIST_NO_PARENT = [
  { Id: 1, Display: 'Assignment' },
  { Id: 7, Display: 'SQL Scalar Assignment' },
  { Id: 8, Display: 'SQL Tuple Assignment' },
  { Id: 2, Display: 'Boolean Expression Warning' },
  { Id: 3, Display: 'Boolean Expression Error' },
  { Id: 4, Display: 'Aggregate From Grid Column' },
];
const APP_FORMULAR_TYPE_LIST_NO_PARENT_NOR_CHILD = [
  { Id: 1, Display: 'Assignment' },
  { Id: 7, Display: 'SQL Scalar Assignment' },
  { Id: 8, Display: 'SQL Tuple Assignment' },
  { Id: 2, Display: 'Boolean Expression Warning' },
  { Id: 3, Display: 'Boolean Expression Error' },
];

function getFormularTypeListForUnit(unit: any): Array<{ Id: number; Display: string }> {
  if (!unit) return APP_FORMULAR_TYPE_LIST_NO_PARENT_NOR_CHILD;
  const hasChildren = unit.Children && unit.Children.length > 0;
  const hasParent = !!unit.parent;
  if (hasChildren && hasParent) return APP_FORMULAR_TYPE_LIST;
  if (hasChildren && !hasParent) return APP_FORMULAR_TYPE_LIST_NO_PARENT;
  if (!hasChildren && hasParent) return APP_FORMULAR_TYPE_LIST_NO_CHILD;
  return APP_FORMULAR_TYPE_LIST_NO_PARENT_NOR_CHILD;
}

function findMaxFormulaSort(formulaList: any[]): number {
  if (!formulaList || formulaList.length === 0) return 0;
  let max = 0;
  formulaList.forEach((f: any) => {
    const s = typeof f.CaculationFlowSort === 'number' ? f.CaculationFlowSort : parseInt(f.CaculationFlowSort, 10);
    if (!isNaN(s) && s > max) max = s;
  });
  return max;
}

function applyFormulaSort(
  cv: CollectionView | null | undefined,
  opts?: {
    /** Used to place Aggregate/Subscribe rows next to their grid placeholder row */
    getPlaceholderSortByChildUnitId?: (childUnitId: number) => number | null;
    /** Used to find cross-relation settings for aggregate/subscribe */
    getCrossSettingByAssignToFieldId?: (assignToFieldId: number) => any | null;
    /** Fallback: derive unitId from a transactionFieldId */
    getUnitIdByFieldId?: (fieldId: number) => number | null;
  }
) {
  if (!cv) return;
  const getPlaceholderSortByChildUnitId = opts?.getPlaceholderSortByChildUnitId;
  const getCrossSettingByAssignToFieldId = opts?.getCrossSettingByAssignToFieldId;
  const getUnitIdByFieldId = opts?.getUnitIdByFieldId;

  const parseNumericSort = (v: any): number | null => {
    if (typeof v === 'number' && Number.isFinite(v)) return v;
    const n = parseInt(String(v ?? ''), 10);
    return Number.isFinite(n) ? n : null;
  };

  // Write a stable numeric sort key onto each item, then sort by it.
  const buildTempSort = (item: any): number => {
    // Placeholder rows (grid calculation rows)
    if (item?.ChildTransactionUnitId) {
      const base = parseNumericSort(item.CaculationFlowSort);
      return base ?? Number.MAX_SAFE_INTEGER;
    }

    // Aggregate-from-grid-column and Subscribe-from-parent use textual flow markers.
    // We must place them relative to the grid placeholder row (Angular behavior).
    const op = item?.OperationType;
    const assignTo = item?.AssignToTransFieldId;
    const cross = assignTo && getCrossSettingByAssignToFieldId ? getCrossSettingByAssignToFieldId(Number(assignTo)) : null;

    // Prefer placing relative to the SubscribeToUnitId placeholder row.
    const subscribeToUnitId =
      cross?.SubscribeToUnitId != null
        ? Number(cross.SubscribeToUnitId)
        : cross?.SubscribeToTransFieldId != null && getUnitIdByFieldId
          ? getUnitIdByFieldId(Number(cross.SubscribeToTransFieldId))
          : null;
    const placeholderBase =
      subscribeToUnitId && getPlaceholderSortByChildUnitId ? getPlaceholderSortByChildUnitId(subscribeToUnitId) : null;

    // If it's an aggregate row, place it just BELOW the grid placeholder row.
    // If it's a subscribe-parent row, place it just ABOVE the grid placeholder row.
    if (op === EmAppFormularType.SubscribeFromGridColumnAggregation) {
      const base = placeholderBase ?? parseNumericSort(item.CaculationFlowSort) ?? Number.MAX_SAFE_INTEGER;
      return base + 0.5;
    }
    if (op === EmAppFormularType.SubscribeFromParentLevelField) {
      const base = placeholderBase ?? parseNumericSort(item.CaculationFlowSort) ?? Number.MAX_SAFE_INTEGER;
      return base - 0.5;
    }

    // Normal numeric sort rows
    const base = parseNumericSort(item?.CaculationFlowSort);
    return base ?? Number.MAX_SAFE_INTEGER;
  };

  try {
    (cv.sourceCollection as any[])?.forEach?.((it: any) => {
      if (!it) return;
      it.__tempSort = buildTempSort(it);
    });

    cv.sortDescriptions.clear();
    cv.sortDescriptions.push(new SortDescription('__tempSort', true));
    cv.refresh();
  } catch {
    // ignore
  }
}

function prepareConditionFieldList(unit: any, parent: any): Array<{ Id: number; Display: string }> {
  const list: Array<{ Id: number; Display: string }> = [];
  if (!unit) return list;
  (unit.AppTransactionFieldList || []).forEach((field: any) => {
    if (field.ControlType === 13) {
      list.push({
        Id: field.Id,
        Display: (unit.DataBaseTableName || '') + '::' + (field.DataBaseFieldName || ''),
      });
    }
  });
  if (parent) {
    const parentList = prepareConditionFieldList(parent, parent.parent);
    list.push(...parentList);
  }
  return list;
}

export interface TransactionFormulaEditorProps {
  transactionId: number | null;
  applicationId?: string | null;
  onRefresh?: () => void;
  /** When embedded in ApplicationFormBuilder, parent can call these */
  innerRefreshFunctionRef?: React.MutableRefObject<(() => void) | null>;
  innerSaveFunctionRef?: React.MutableRefObject<((callback?: () => void) => void) | null>;
}

const TransactionFormulaEditor: React.FC<TransactionFormulaEditorProps> = ({
  transactionId,
  applicationId,
  onRefresh,
  innerRefreshFunctionRef,
  innerSaveFunctionRef,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [appTransactionData, setAppTransactionData] = useState<any>(null);
  const [rootUnit, setRootUnit] = useState<any>(null);
  const [dictUnitIdUnitFormulaSet, setDictUnitIdUnitFormulaSet] = useState<Record<string, any>>({});
  const [dictUnitIdAndDto, setDictUnitIdAndDto] = useState<Record<number, any>>({});
  const [dictTransactionFieldIdAndDto, setDictTransactionFieldIdAndDto] = useState<Record<number, any>>({});
  const [allowSave, setAllowSave] = useState(false);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const orgTransactionDataRef = useRef<any>(null);

  // Popup editor stack (unitId). Supports child -> grandchild nested popups (Angular-like).
  const [popupUnitStack, setPopupUnitStack] = useState<number[]>([]);

  const emAppFormularFunctionType = useEnumValues('EmAppFormularFunctionType');
  const emAppWarningHighlightPriority = useEnumValues('EmAppWarningHighlightPriority');

  const rootUnitFormulaCV = useMemo(() => {
    return rootUnit?.currentUnitFormulaCV ?? null;
  }, [rootUnit]);

  const openUnitPopup = useCallback((unitId: number) => {
    if (!unitId) return;
    setPopupUnitStack((prev) => prev.concat([unitId]));
  }, []);

  const closeTopPopup = useCallback(() => {
    setPopupUnitStack((prev) => prev.slice(0, Math.max(0, prev.length - 1)));
  }, []);

  const transactionOrganizedTypeList = useMemo(() => {
    const EmTransactionOrganizedType = { MasterDetail: 1, List: 3, FolderList: 5 };
    return [
      { Id: 1, Display: 'MasterDetail' },
      { Id: 3, Display: 'List' },
      { Id: 5, Display: 'FolderList' },
    ];
  }, []);

  const appFormularFunctionTypeList = useMemo(() => {
    if (!emAppFormularFunctionType) return [];
    return Object.entries(emAppFormularFunctionType)
      .filter(([k]) => isNaN(Number(k)))
      .map(([key, value]) => ({ Id: value as number, Display: key }));
  }, [emAppFormularFunctionType]);

  const warningHighlightPriorityList = useMemo(() => {
    if (!emAppWarningHighlightPriority) return [];
    return Object.entries(emAppWarningHighlightPriority)
      .filter(([k]) => isNaN(Number(k)))
      .map(([key, value]) => ({ Id: value as number, Display: key }));
  }, [emAppWarningHighlightPriority]);

  const loadDataFromServer = useCallback(async () => {
    if (!transactionId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    try {
      const transactionData = await appTransactionService.getOneHierarchyTransaction(
        String(transactionId),
        false,
        '',
        '',
        '',
        false,
        ''
      );
      const formularData = await appTransactionService.retrieveAppTransactionUnitFormulaSetDtoList(String(transactionId));

      const formulaSetDict: Record<string, any> = {};
      (formularData || []).forEach((dto: any) => {
        if (dto?.TransactionUnitId != null) formulaSetDict[dto.TransactionUnitId.toString()] = dto;
      });

      const dictField: Record<number, any> = {};
      (transactionData?.AppTransactionUnitList || []).forEach((unit: any) => {
        (unit.AppTransactionFieldList || []).forEach((f: any) => {
          dictField[f.Id] = f;
        });
        (unit.Children || []).forEach((child: any) => {
          (child.AppTransactionFieldList || []).forEach((f: any) => {
            dictField[f.Id] = f;
          });
          (child.Children || []).forEach((grand: any) => {
            (grand.AppTransactionFieldList || []).forEach((f: any) => {
              dictField[f.Id] = f;
            });
          });
        });
      });

      // Ensure FormulaDisplayName exists for ALL fields (needed for aggregate/subscription display reconstruction).
      const initFormulaDisplayNameForUnit = (u: any) => {
        (u?.AppTransactionFieldList || []).forEach((field: any) => {
          const dbField = field?.DataBaseFieldName || field?.DisplayName || '';
          const id = field?.Id;
          const isSibling = !!u?.IsMasterSiblingUnit;
          const table = u?.DataBaseTableName || '';
          let name = isSibling && table ? `[${table}.${dbField}` : `[${dbField}`;
          name += field?.IsTempVariable ? ']' : `_${id}]`;
          field.FormulaDisplayName = name;
        });
        (u?.Children || []).forEach((c: any) => initFormulaDisplayNameForUnit(c));
      };
      (transactionData?.AppTransactionUnitList || []).forEach((u: any) => initFormulaDisplayNameForUnit(u));

      setDictTransactionFieldIdAndDto(dictField);
      setDictUnitIdUnitFormulaSet(formulaSetDict);
      setAppTransactionData(transactionData);
      // Store a JSON-serializable copy for Save (NeedToUpdateTransactionExDto). The in-memory
      // transactionData is mutated with circular refs (e.g. unit.parent) and Wijmo objects.
      try {
        orgTransactionDataRef.current = JSON.parse(JSON.stringify(transactionData));
      } catch {
        orgTransactionDataRef.current = null;
      }

      const dictUnit: Record<number, any> = {};
      const rootLevelUnitFieldList: any[] = [];

      const prepareOneUnit = (aUnit: any, parentUnit: any, level: number) => {
        if (!aUnit?.Id) return;
        dictUnit[aUnit.Id] = aUnit;
        (aUnit.AppTransactionFieldList || []).forEach((field: any) => {
          if (aUnit.IsMasterSiblingUnit) {
            field.FormulaDisplayName = '[' + (aUnit.DataBaseTableName || '') + '.' + (field.DataBaseFieldName || '');
          } else {
            field.FormulaDisplayName = '[' + (field.DataBaseFieldName || '');
          }
          field.FormulaDisplayName += field.IsTempVariable ? ']' : '_' + field.Id + ']';
        });
        const rootUnitObj = transactionData?.AppTransactionUnitList?.[0];
        if (aUnit.IsMasterSiblingUnit && rootUnitObj) {
          rootUnitObj.AppTransactionFieldList = rootUnitObj.AppTransactionFieldList || [];
          rootUnitObj.AppTransactionFieldList = rootUnitObj.AppTransactionFieldList.concat(aUnit.AppTransactionFieldList || []);
        } else {
          aUnit.appTransactionFieldCV = new CollectionView(aUnit.AppTransactionFieldList || []);
        }
        aUnit.parent = parentUnit;
        aUnit.level = level;
        aUnit.transFiledDataMap = new DataMap(aUnit.AppTransactionFieldList || [], 'Id', 'FormulaDisplayName');
        const conditionList = prepareConditionFieldList(aUnit, parentUnit);
        aUnit.conditionFiledDataMap = new DataMap(conditionList, 'Id', 'Display');
        aUnit.appFormularTypeDatamap = new DataMap(getFormularTypeListForUnit(aUnit), 'Id', 'Display');

        const formulaSet = formulaSetDict[aUnit.Id.toString()];
        if (formulaSet?.ListAppTransactionUnitFormula) {
          aUnit.currentUnitFormulaList = formulaSet.ListAppTransactionUnitFormula;
          aUnit.currentUnitFormulaCV = new CollectionView(aUnit.currentUnitFormulaList);
        } else {
          aUnit.currentUnitFormulaList = [];
          aUnit.currentUnitFormulaCV = new CollectionView(aUnit.currentUnitFormulaList);
        }
        const getCrossSettingByAssignToFieldId = (assignToFieldId: number) => {
          const dict = transactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
          return (dict as any)?.[assignToFieldId] ?? (dict as any)?.[String(assignToFieldId)] ?? null;
        };
        const getUnitIdByFieldId = (fieldId: number) => {
          const f = (dictField as any)?.[fieldId] ?? (dictField as any)?.[String(fieldId)] ?? null;
          const unitId = f?.TransactionUnitId;
          return unitId != null ? Number(unitId) : null;
        };
        const getPlaceholderSortByChildUnitId = (childUnitId: number) => {
          const row = (aUnit.currentUnitFormulaList || []).find((x: any) => Number(x?.ChildTransactionUnitId) === Number(childUnitId));
          const n = row ? parseInt(String(row.CaculationFlowSort ?? ''), 10) : NaN;
          return Number.isFinite(n) ? n : null;
        };
        // Rebuild "Aggregate from grid column" / "Subscribe parent field" rows from field cross-relation settings.
        // Backend does not persist these as formula rows; Angular reconstructs them on load.
        const aggLabelFromType = (t: number | null | undefined) => {
          if (t === 1) return 'SUM';
          if (t === 2) return 'AVG';
          if (t === 3) return 'MIN';
          if (t === 4) return 'MAX';
          if (t === 5) return 'COUNT';
          return t != null ? `Agg(${t})` : 'AGG';
        };
        const crossDict = transactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
        const existingCrossRowByFieldId = new Set<number>(
          (aUnit.currentUnitFormulaList || [])
            .filter(
              (x: any) =>
                x?.AssignToTransFieldId &&
                (x?.OperationType === EmAppFormularType.SubscribeFromGridColumnAggregation ||
                  x?.OperationType === EmAppFormularType.SubscribeFromParentLevelField)
            )
            .map((x: any) => x.AssignToTransFieldId as number)
        );
        (aUnit.AppTransactionFieldList || []).forEach((field: any) => {
          const fieldId = field?.Id;
          if (!fieldId || existingCrossRowByFieldId.has(fieldId)) return;
          const cross = (crossDict as any)?.[fieldId] ?? (crossDict as any)?.[String(fieldId)] ?? null;

          if (field.ParentUnitSubscribeChildAggFunctionId) {
            const subFieldIdRaw = cross?.SubscribeToTransFieldId ?? null;
            const subFieldId = subFieldIdRaw != null ? Number(subFieldIdRaw) : null;
            const subFieldDto =
              subFieldId != null ? (dictField as any)[subFieldId] ?? (dictField as any)[String(subFieldId)] ?? null : null;
            const aggType = cross?.AggregationType ?? null;
            const left =
              field.FormulaDisplayName ||
              (field.DataBaseFieldName ? `[${field.DataBaseFieldName}_${fieldId}]` : `[Field_${fieldId}]`);
            const right = subFieldDto?.FormulaDisplayName
              ? `${aggLabelFromType(aggType)}(${subFieldDto.FormulaDisplayName})`
              : `${aggLabelFromType(aggType)}(?)`;
            (aUnit.currentUnitFormulaList as any[]).push({
              TransactionUnitId: aUnit.Id,
              OperationType: EmAppFormularType.SubscribeFromGridColumnAggregation,
              CaculationFlowSort: 'After Grid Calculation',
              AssignToTransFieldId: fieldId,
              FormulaExpression: `${left} = ${right}`
            });
            existingCrossRowByFieldId.add(fieldId);
            return;
          }

          if (field.ChildUnitSubscribeParentFieldId) {
            const parentFieldIdRaw = field.ChildUnitSubscribeParentFieldId;
            const parentFieldId = parentFieldIdRaw != null ? Number(parentFieldIdRaw) : null;
            const parentFieldDto =
              parentFieldId != null ? (dictField as any)[parentFieldId] ?? (dictField as any)[String(parentFieldId)] ?? null : null;
            const left =
              field.FormulaDisplayName ||
              (field.DataBaseFieldName ? `[${field.DataBaseFieldName}_${fieldId}]` : `[Field_${fieldId}]`);
            const right = parentFieldDto?.FormulaDisplayName || `[Field_${parentFieldId}]`;
            (aUnit.currentUnitFormulaList as any[]).push({
              TransactionUnitId: aUnit.Id,
              OperationType: EmAppFormularType.SubscribeFromParentLevelField,
              CaculationFlowSort: 'Before Grid Calculation',
              AssignToTransFieldId: fieldId,
              FormulaExpression: `${left} = ${right}`
            });
            existingCrossRowByFieldId.add(fieldId);
          }
        });
        if (aUnit.currentUnitFormulaCV) aUnit.currentUnitFormulaCV.refresh();

        // Ensure one placeholder row per child grid (Angular adds these for expandable sub-grids)
        // - root unit placeholders represent level-2 child units
        // - level-2 unit placeholders represent level-3 grandchild units
        if ((parentUnit === null || level === 2) && aUnit.Children?.length) {
          aUnit.Children.forEach((child: any) => {
            const hasPlaceholder = (aUnit.currentUnitFormulaList as any[]).some(
              (f: any) => f.ChildTransactionUnitId === child.Id
            );
            if (!hasPlaceholder) {
              const maxSort = (aUnit.currentUnitFormulaList as any[]).reduce((m, f: any) => {
                const s = typeof f.CaculationFlowSort === 'number' ? f.CaculationFlowSort : parseInt(f.CaculationFlowSort, 10);
                return isNaN(s) ? m : Math.max(m, s);
              }, 0);
              (aUnit.currentUnitFormulaList as any[]).push({
                CaculationFlowSort: maxSort + 1,
                TransactionUnitId: aUnit.Id,
                ChildTransactionUnitId: child.Id,
                FormulaExpression: '',
                AssignToTransFieldId: null,
              });
            }
          });
          aUnit.currentUnitFormulaList.sort((a: any, b: any) => {
            const sa = typeof a.CaculationFlowSort === 'number' ? a.CaculationFlowSort : parseInt(a.CaculationFlowSort, 10);
            const sb = typeof b.CaculationFlowSort === 'number' ? b.CaculationFlowSort : parseInt(b.CaculationFlowSort, 10);
            return (isNaN(sa) ? 0 : sa) - (isNaN(sb) ? 0 : sb);
          });
        }

        // Final pass: compute __tempSort and sort so aggregate rows land next to their placeholder row.
        applyFormulaSort(aUnit.currentUnitFormulaCV, { getPlaceholderSortByChildUnitId, getCrossSettingByAssignToFieldId, getUnitIdByFieldId });
      };

      const rootUnitObj = transactionData?.AppTransactionUnitList?.[0];
      if (rootUnitObj) {
        prepareOneUnit(rootUnitObj, null, 1);

        // Root-level units only: root unit + its master sibling unit(s). (Do NOT include child grid units.)
        (transactionData?.AppTransactionUnitList || [])
          .filter((u: any) => u && u !== rootUnitObj && u.IsMasterSiblingUnit)
          .forEach((sibling: any) => {
            // Prepare sibling so FormulaDisplayName is available and it can contribute to the root-level datamap.
            prepareOneUnit(sibling, null, 1);
            (sibling.AppTransactionFieldList || []).forEach((f: any) => rootLevelUnitFieldList.push(f));
          });

        (rootUnitObj.AppTransactionFieldList || []).forEach((f: any) => rootLevelUnitFieldList.push(f));
        (rootUnitObj.Children || []).forEach((child: any) => {
          prepareOneUnit(child, rootUnitObj, 2);
          (child.Children || []).forEach((grand: any) => {
            prepareOneUnit(grand, child, 3);
          });
        });
        rootUnitObj.rootLevelUnitFieldDataMap = new DataMap(rootLevelUnitFieldList, 'Id', 'FormulaDisplayName');
        setRootUnit(rootUnitObj);
      } else {
        setRootUnit(null);
      }
      setDictUnitIdAndDto(dictUnit);
      setAllowSave(false);
      setErrorMessages([]);
    } catch (e: any) {
      showError(e?.message || 'Failed to load calculation validation flow data');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, dispatch, showError]);

  useEffect(() => {
    loadDataFromServer();
  }, [transactionId]);

  const markChange = useCallback((unitDto: any) => {
    if (!unitDto) return;
    setDictUnitIdUnitFormulaSet((prev) => {
      const next = { ...prev };
      const key = unitDto.Id.toString();
      if (!next[key]) {
        next[key] = {
          TransactionUnitId: unitDto.Id,
          ListAppTransactionUnitFormula: unitDto.currentUnitFormulaList || [],
          isFormularModified: true,
        };
      } else {
        next[key] = { ...next[key], isFormularModified: true };
      }
      return next;
    });
    setAllowSave(true);
  }, []);

  const refresh = useCallback(() => {
    loadDataFromServer();
    onRefresh?.();
  }, [loadDataFromServer, onRefresh]);

  const addFormula = useCallback(
    (unitDto: any) => {
      if (!unitDto?.currentUnitFormulaList) return;
      const maxSort = findMaxFormulaSort(unitDto.currentUnitFormulaList);
      const newFormula: any = {
        CaculationFlowSort: maxSort + 1,
        TransactionUnitId: unitDto.Id,
        OperationType: EmAppFormularType.Assignment,
        FormulaExpression: '',
        AssignToTransFieldId: null,
      };
      unitDto.currentUnitFormulaList.push(newFormula);
      if (unitDto.currentUnitFormulaCV) unitDto.currentUnitFormulaCV.refresh();
      applyFormulaSort(unitDto.currentUnitFormulaCV, {
        getPlaceholderSortByChildUnitId: (childUnitId: number) => {
          const list = (unitDto.currentUnitFormulaCV?.sourceCollection as any[]) || (unitDto.currentUnitFormulaList || []);
          const row = list.find((x: any) => Number(x?.ChildTransactionUnitId) === Number(childUnitId));
          const n = row ? parseInt(String(row.CaculationFlowSort ?? ''), 10) : NaN;
          return Number.isFinite(n) ? n : null;
        },
        getCrossSettingByAssignToFieldId: (assignToFieldId: number) => {
          const dict = appTransactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
          return (dict as any)?.[assignToFieldId] ?? (dict as any)?.[String(assignToFieldId)] ?? null;
        },
        getUnitIdByFieldId: (fieldId: number) => {
          const f = (dictTransactionFieldIdAndDto as any)?.[fieldId] ?? (dictTransactionFieldIdAndDto as any)?.[String(fieldId)] ?? null;
          const unitId = f?.TransactionUnitId;
          return unitId != null ? Number(unitId) : null;
        }
      });
      const firstField = unitDto.AppTransactionFieldList?.[0];
      if (firstField) newFormula.AssignToTransFieldId = firstField.Id;
      markChange(unitDto);
      setRootUnit((r: any) => (r ? { ...r } : r));
    },
    [markChange]
  );

  const rootGridRef = useRef<any>(null);
  const childGridRefs = useRef<Record<number, any>>({});
  const lastSelectedFormulaByUnitIdRef = useRef<Record<number, any>>({});
  const deleteFormula = useCallback(
    (unitDto: any) => {
      if (!unitDto?.currentUnitFormulaList) return;
      const gridRef = unitDto === rootUnit ? rootGridRef.current : childGridRefs.current[unitDto.Id];
      const flex = (gridRef as any)?.control ?? gridRef;
      // Grandchild popup may not have captured the grid ref yet; still allow delete via cached selection + CV.

      const selRow = flex?.selection?.row;
      const row = flex && typeof selRow === 'number' && selRow >= 0 ? flex.rows?.[selRow] : null;
      // Selection can be cleared when clicking toolbar buttons; fall back to currentItem/cached item.
      const dataItem =
        row?.dataItem ??
        flex?.collectionView?.currentItem ??
        unitDto?.currentUnitFormulaCV?.currentItem ??
        lastSelectedFormulaByUnitIdRef.current?.[Number(unitDto.Id)] ??
        null;
      if (!dataItem || dataItem.ChildTransactionUnitId) return;

      // If deleting an Aggregate/Subscribe row, clear the cross-relation flags on the field
      // so it won't be reconstructed again after Save/Refresh.
      const assignToId = dataItem.AssignToTransFieldId as number | null | undefined;
      if (
        assignToId &&
        (dataItem.OperationType === EmAppFormularType.SubscribeFromGridColumnAggregation ||
          dataItem.OperationType === EmAppFormularType.SubscribeFromParentLevelField)
      ) {
        const clearCross = (transactionDto: any) => {
          if (!transactionDto) return transactionDto;
          const dict = transactionDto.DictTransFieldIdAndCrossRelationSettingDto || {};
          const prevItem = dict[assignToId] || {};
          return {
            ...transactionDto,
            IsModified: true,
            DictTransFieldIdAndCrossRelationSettingDto: {
              ...dict,
              [assignToId]: {
                ...prevItem,
                SubscribeToUnitId: null,
                SubscribeToTransFieldId: null,
                AggregationType: null,
                ParentUnitSubscribeChildAggFunctionId: null,
                ChildUnitSubscribeParentFieldId: null,
                CurrentUnitId: unitDto?.Id ?? prevItem?.CurrentUnitId,
                IsModified: true
              }
            }
          };
        };
        const clearFieldFlags = (transactionDto: any) => {
          if (!transactionDto) return transactionDto;
          const updateField = (field: any) => {
            if (!field || field.Id !== assignToId) return field;
            return {
              ...field,
              ParentUnitSubscribeChildAggFunctionId: null,
              ChildUnitSubscribeParentFieldId: null,
              IsModified: true
            };
          };
          const updateUnit = (u: any) => {
            if (!u) return u;
            return {
              ...u,
              AppTransactionFieldList: (u.AppTransactionFieldList || []).map(updateField),
              Children: (u.Children || []).map((c: any) => updateUnit(c))
            };
          };
          return {
            ...transactionDto,
            IsModified: true,
            AppTransactionUnitList: (transactionDto.AppTransactionUnitList || []).map((u: any) => updateUnit(u))
          };
        };

        setDictTransactionFieldIdAndDto((prev) => {
          const cur = prev?.[assignToId];
          if (!cur) return prev;
          return {
            ...prev,
            [assignToId]: {
              ...cur,
              ParentUnitSubscribeChildAggFunctionId: null,
              ChildUnitSubscribeParentFieldId: null
            }
          };
        });
        setAppTransactionData((prev: any) => clearFieldFlags(clearCross(prev)));
        if (orgTransactionDataRef.current) {
          orgTransactionDataRef.current = clearFieldFlags(clearCross(orgTransactionDataRef.current));
        }
      }

      // Delete from the live CollectionView backing collection (after refresh/sort, this is the truth for the grid).
      const cv = (flex?.collectionView as CollectionView | undefined) ?? unitDto.currentUnitFormulaCV;
      const liveList = (cv?.sourceCollection as any[]) || unitDto.currentUnitFormulaList;

      let idx = liveList.indexOf(dataItem);
      if (idx < 0) {
        // Fallback matching when CV returns a different object reference (can happen after refresh/sort).
        idx = liveList.findIndex((x: any) => {
          if (!x) return false;
          if (x === dataItem) return true;
          return (
            String(x.AssignToTransFieldId ?? '') === String((dataItem as any).AssignToTransFieldId ?? '') &&
            String(x.ChildTransactionUnitId ?? '') === String((dataItem as any).ChildTransactionUnitId ?? '') &&
            String(x.OperationType ?? '') === String((dataItem as any).OperationType ?? '') &&
            String(x.FormulaExpression ?? '') === String((dataItem as any).FormulaExpression ?? '') &&
            String(x.CaculationFlowSort ?? '') === String((dataItem as any).CaculationFlowSort ?? '')
          );
        });
      }
      if (idx >= 0) {
        liveList.splice(idx, 1);
        // Keep the unit's list reference aligned with what the grid is bound to.
        unitDto.currentUnitFormulaList = liveList;
        if (unitDto.currentUnitFormulaCV) unitDto.currentUnitFormulaCV.refresh();
        applyFormulaSort(unitDto.currentUnitFormulaCV, {
          getPlaceholderSortByChildUnitId: (childUnitId: number) => {
            const list = (unitDto.currentUnitFormulaCV?.sourceCollection as any[]) || (unitDto.currentUnitFormulaList || []);
            const row = list.find((x: any) => Number(x?.ChildTransactionUnitId) === Number(childUnitId));
            const n = row ? parseInt(String(row.CaculationFlowSort ?? ''), 10) : NaN;
            return Number.isFinite(n) ? n : null;
          },
          getCrossSettingByAssignToFieldId: (assignToFieldId: number) => {
            const dict = appTransactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
            return (dict as any)?.[assignToFieldId] ?? (dict as any)?.[String(assignToFieldId)] ?? null;
          },
          getUnitIdByFieldId: (fieldId: number) => {
            const f = (dictTransactionFieldIdAndDto as any)?.[fieldId] ?? (dictTransactionFieldIdAndDto as any)?.[String(fieldId)] ?? null;
            const unitId = f?.TransactionUnitId;
            return unitId != null ? Number(unitId) : null;
          }
        });
        markChange(unitDto);
        setRootUnit((r: any) => (r ? { ...r } : r));
      }
    },
    [rootUnit, markChange]
  );

  const save = useCallback(
    async (callback?: () => void) => {
      const needToSave = Object.values(dictUnitIdUnitFormulaSet).filter(
        (dto: any) => dto?.isFormularModified
      ) as any[];
      if (needToSave.length === 0) {
        callback?.();
        return;
      }
      if (needToSave[0]) {
        needToSave[0].NeedToUpdateTransactionExDto = orgTransactionDataRef.current;
      }
      dispatch(setIsBusy());
      try {
        const result = await appTransactionService.saveAppTransactionFormulas(needToSave);
        if (result?.ValidationResult) {
          showValidationMessages(result.ValidationResult, true);
        }
        if (result?.IsSuccessful) {
          showInfo('Formulas saved successfully');
          setAllowSave(false);
          setDictUnitIdUnitFormulaSet((prev) => {
            const next = { ...prev };
            needToSave.forEach((dto: any) => {
              const key = dto.TransactionUnitId?.toString();
              if (key && next[key]) next[key] = { ...next[key], isFormularModified: false };
            });
            return next;
          });
          refresh();
          callback?.();
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to save formulas');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dictUnitIdUnitFormulaSet, dispatch, showError, showValidationMessages, showInfo, refresh]
  );

  useEffect(() => {
    if (innerRefreshFunctionRef) innerRefreshFunctionRef.current = refresh;
    return () => {
      if (innerRefreshFunctionRef) innerRefreshFunctionRef.current = null;
    };
  }, [innerRefreshFunctionRef, refresh]);

  useEffect(() => {
    if (innerSaveFunctionRef) innerSaveFunctionRef.current = save;
    return () => {
      if (innerSaveFunctionRef) innerSaveFunctionRef.current = null;
    };
  }, [innerSaveFunctionRef, save]);

  const [formulaDialogState, setFormulaDialogState] = useState<{
    isOpen: boolean;
    transFieldId: number;
    formulaDto: any;
    unitDto: any;
    initialFormulaText: string;
  }>({ isOpen: false, transFieldId: 0, formulaDto: null, unitDto: null, initialFormulaText: '' });

  const transactionDataForDialog = useMemo(() => {
    if (!appTransactionData) return null;
    return {
      AppTransactionData: appTransactionData,
      dictTransactionFieldIdAndDto: dictTransactionFieldIdAndDto,
      dictUnitIdAndDto: dictUnitIdAndDto,
      rootLevelUnitFieldList: rootUnit?.AppTransactionFieldList || [],
    };
  }, [appTransactionData, dictTransactionFieldIdAndDto, dictUnitIdAndDto, rootUnit]);

  const handleFormulaDialogConfirm = useCallback(
    (payload: {
      formulaType: 'Calculation' | 'Aggregation' | 'SubscribeParentField';
      formulaText: string;
      aggregationSetting: any;
    }) => {
      const { formulaDto, unitDto } = formulaDialogState;
      if (!formulaDto || !unitDto) return;
      const transFieldId = formulaDialogState.transFieldId;
      const prefix =
        dictTransactionFieldIdAndDto[transFieldId]?.FormulaDisplayName != null
          ? dictTransactionFieldIdAndDto[transFieldId].FormulaDisplayName + ' = '
          : '';
      if (payload.formulaType === 'Calculation' && payload.formulaText != null) {
        formulaDto.FormulaExpression = prefix + payload.formulaText;
      }

      // If switching back to normal assignment expression, clear any previous aggregate/subscribe settings.
      // Otherwise the reconstructed "Aggregate/Subscribe" rows will come back after Save (Angular behavior).
      if (payload.formulaType === 'Calculation' && transFieldId) {
        // Restore numeric calculation flow sort (Aggregate/Subscribe uses text labels).
        if (typeof formulaDto.CaculationFlowSort === 'string') {
          const backup = (formulaDto as any).__numericFlowSortBackup;
          if (typeof backup === 'number' && Number.isFinite(backup)) {
            formulaDto.CaculationFlowSort = backup;
          } else {
            // Fallback: place it at the end of the current unit list.
            const maxSort = findMaxFormulaSort((unitDto.currentUnitFormulaList || []).filter((x: any) => !x?.ChildTransactionUnitId));
            formulaDto.CaculationFlowSort = maxSort + 1;
          }
        }

        const clearCross = (transactionDto: any) => {
          if (!transactionDto) return transactionDto;
          const dict = transactionDto.DictTransFieldIdAndCrossRelationSettingDto || {};
          const prevItem = dict[transFieldId] || {};
          return {
            ...transactionDto,
            IsModified: true,
            DictTransFieldIdAndCrossRelationSettingDto: {
              ...dict,
              [transFieldId]: {
                ...prevItem,
                SubscribeToUnitId: null,
                SubscribeToTransFieldId: null,
                AggregationType: null,
                ParentUnitSubscribeChildAggFunctionId: null,
                ChildUnitSubscribeParentFieldId: null,
                CurrentUnitId: unitDto?.Id ?? prevItem?.CurrentUnitId,
                IsModified: true
              }
            }
          };
        };

        const clearFieldFlags = (transactionDto: any) => {
          if (!transactionDto) return transactionDto;
          const updateField = (field: any) => {
            if (!field || field.Id !== transFieldId) return field;
            return {
              ...field,
              ParentUnitSubscribeChildAggFunctionId: null,
              ChildUnitSubscribeParentFieldId: null,
              IsModified: true
            };
          };
          const updateUnit = (unit: any) => {
            if (!unit) return unit;
            return {
              ...unit,
              AppTransactionFieldList: (unit.AppTransactionFieldList || []).map(updateField),
              Children: (unit.Children || []).map((c: any) => updateUnit(c))
            };
          };
          return {
            ...transactionDto,
            IsModified: true,
            AppTransactionUnitList: (transactionDto.AppTransactionUnitList || []).map((u: any) => updateUnit(u))
          };
        };

        setDictTransactionFieldIdAndDto((prev) => {
          const cur = prev?.[transFieldId];
          if (!cur) return prev;
          return {
            ...prev,
            [transFieldId]: {
              ...cur,
              ParentUnitSubscribeChildAggFunctionId: null,
              ChildUnitSubscribeParentFieldId: null
            }
          };
        });

        setAppTransactionData((prev: any) => clearFieldFlags(clearCross(prev)));
        if (orgTransactionDataRef.current) {
          orgTransactionDataRef.current = clearFieldFlags(clearCross(orgTransactionDataRef.current));
        }
      }

      const applyCrossSettingToTransactionDto = (transactionDto: any) => {
        if (!transactionDto || !payload.aggregationSetting || !transFieldId) return transactionDto;
        const dict = transactionDto.DictTransFieldIdAndCrossRelationSettingDto || {};
        const prevItem = dict[transFieldId] || {};
        return {
          ...transactionDto,
          IsModified: true,
          DictTransFieldIdAndCrossRelationSettingDto: {
            ...dict,
            [transFieldId]: {
              ...prevItem,
              ...payload.aggregationSetting,
              CurrentUnitId: unitDto?.Id ?? prevItem?.CurrentUnitId,
              IsModified: true
            }
          }
        };
      };

      const applyCrossSettingToFieldDtos = (transactionDto: any) => {
        if (!transactionDto || !payload.aggregationSetting || !transFieldId) return transactionDto;
        const aggId = payload.aggregationSetting?.ParentUnitSubscribeChildAggFunctionId ?? null;
        const parentFieldId =
          payload.aggregationSetting?.ChildUnitSubscribeParentFieldId ??
          payload.aggregationSetting?.SubscribeToTransFieldId ??
          null;

        const updateField = (field: any) => {
          if (!field || field.Id !== transFieldId) return field;
          // These two are what the backend persists on the field entity.
          if (payload.formulaType === 'Aggregation') {
            return {
              ...field,
              ParentUnitSubscribeChildAggFunctionId: aggId,
              ChildUnitSubscribeParentFieldId: null,
              IsModified: true
            };
          }
          if (payload.formulaType === 'SubscribeParentField') {
            return {
              ...field,
              ParentUnitSubscribeChildAggFunctionId: null,
              ChildUnitSubscribeParentFieldId: parentFieldId,
              IsModified: true
            };
          }
          return field;
        };

        const updateUnit = (unit: any) => {
          if (!unit) return unit;
          const updatedFields = (unit.AppTransactionFieldList || []).map(updateField);
          const updatedChildren = (unit.Children || []).map((c: any) => updateUnit(c));
          return {
            ...unit,
            AppTransactionFieldList: updatedFields,
            Children: updatedChildren
          };
        };

        return {
          ...transactionDto,
          IsModified: true,
          AppTransactionUnitList: (transactionDto.AppTransactionUnitList || []).map((u: any) => updateUnit(u))
        };
      };

      // Persist cross-relation settings so:
      // - reopening the popup keeps selections
      // - saving persists to DB (SaveAppTransactionExDto uses these)
      if (payload.aggregationSetting && transFieldId) {
        // Keep the dialog's field dictionary in sync (FieldFormulaDialog reads this, not unit lists).
        setDictTransactionFieldIdAndDto((prev) => {
          const cur = prev?.[transFieldId];
          if (!cur) return prev;
          const aggId = payload.aggregationSetting?.ParentUnitSubscribeChildAggFunctionId ?? null;
          const parentFieldId =
            payload.aggregationSetting?.ChildUnitSubscribeParentFieldId ??
            payload.aggregationSetting?.SubscribeToTransFieldId ??
            null;

          if (payload.formulaType === 'Aggregation') {
            return {
              ...prev,
              [transFieldId]: {
                ...cur,
                ParentUnitSubscribeChildAggFunctionId: aggId,
                ChildUnitSubscribeParentFieldId: null
              }
            };
          }
          if (payload.formulaType === 'SubscribeParentField') {
            return {
              ...prev,
              [transFieldId]: {
                ...cur,
                ParentUnitSubscribeChildAggFunctionId: null,
                ChildUnitSubscribeParentFieldId: parentFieldId
              }
            };
          }
          return prev;
        });

        setAppTransactionData((prev: any) => applyCrossSettingToFieldDtos(applyCrossSettingToTransactionDto(prev)));

        // Update the JSON-serializable transaction copy used during Save.
        if (orgTransactionDataRef.current) {
          orgTransactionDataRef.current = applyCrossSettingToFieldDtos(applyCrossSettingToTransactionDto(orgTransactionDataRef.current));
        }
      }

      if (payload.aggregationSetting) {
        if (payload.formulaType === 'SubscribeParentField') {
          // Backup numeric sort before switching to text label.
          if (typeof formulaDto.CaculationFlowSort === 'number') {
            (formulaDto as any).__numericFlowSortBackup = formulaDto.CaculationFlowSort;
          }
          formulaDto.OperationType = EmAppFormularType.SubscribeFromParentLevelField;
          formulaDto.CaculationFlowSort = 'Before Grid Calculation';
          // Show something immediately in the cell (Angular shows a preview string even before save).
          const parentFieldId =
            payload.aggregationSetting?.ChildUnitSubscribeParentFieldId ??
            payload.aggregationSetting?.SubscribeToTransFieldId ??
            null;
          const parentFieldDto = parentFieldId ? dictTransactionFieldIdAndDto[parentFieldId] : null;
          formulaDto.FormulaExpression = parentFieldDto?.FormulaDisplayName
            ? `${prefix}${parentFieldDto.FormulaDisplayName}`
            : formulaDto.FormulaExpression || '';
        } else if (payload.formulaType === 'Aggregation' || payload.aggregationSetting.ParentUnitSubscribeChildAggFunctionId) {
          // Backup numeric sort before switching to text label.
          if (typeof formulaDto.CaculationFlowSort === 'number') {
            (formulaDto as any).__numericFlowSortBackup = formulaDto.CaculationFlowSort;
          }
          formulaDto.OperationType = EmAppFormularType.SubscribeFromGridColumnAggregation;
          formulaDto.CaculationFlowSort = 'After Grid Calculation';
          const subFieldId = payload.aggregationSetting?.SubscribeToTransFieldId ?? null;
          const subFieldDto = subFieldId ? dictTransactionFieldIdAndDto[subFieldId] : null;
          const aggType = payload.aggregationSetting?.AggregationType ?? null;
          const aggLabel =
            aggType === 1 ? 'SUM' :
            aggType === 2 ? 'AVG' :
            aggType === 3 ? 'MIN' :
            aggType === 4 ? 'MAX' :
            aggType === 5 ? 'COUNT' :
            aggType != null ? `Agg(${aggType})` : 'AGG';
          if (subFieldDto?.FormulaDisplayName) {
            formulaDto.FormulaExpression = `${prefix}${aggLabel}(${subFieldDto.FormulaDisplayName})`;
          }
        } else {
          formulaDto.OperationType = EmAppFormularType.Assignment;
        }
      }

      // Ensure Wijmo grids repaint updated cells for nested units (child/grandchild).
      // Root grid often updates due to CV recreation; nested grids need explicit refresh.
      try {
        unitDto.currentUnitFormulaCV?.refresh?.();
      } catch {
        // ignore
      }
      applyFormulaSort(unitDto.currentUnitFormulaCV, {
        getPlaceholderSortByChildUnitId: (childUnitId: number) => {
          const list = (unitDto.currentUnitFormulaCV?.sourceCollection as any[]) || (unitDto.currentUnitFormulaList || []);
          const row = list.find((x: any) => Number(x?.ChildTransactionUnitId) === Number(childUnitId));
          const n = row ? parseInt(String(row.CaculationFlowSort ?? ''), 10) : NaN;
          return Number.isFinite(n) ? n : null;
        },
        getCrossSettingByAssignToFieldId: (assignToFieldId: number) => {
          const dict = appTransactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
          return (dict as any)?.[assignToFieldId] ?? (dict as any)?.[String(assignToFieldId)] ?? null;
        },
        getUnitIdByFieldId: (fieldId: number) => {
          const f = (dictTransactionFieldIdAndDto as any)?.[fieldId] ?? (dictTransactionFieldIdAndDto as any)?.[String(fieldId)] ?? null;
          const unitId = f?.TransactionUnitId;
          return unitId != null ? Number(unitId) : null;
        }
      });
      markChange(unitDto);
      setFormulaDialogState((s) => ({ ...s, isOpen: false }));
      setRootUnit((r: any) => (r ? { ...r } : r));
    },
    [formulaDialogState, dictTransactionFieldIdAndDto, markChange, appTransactionData]
  );

  const handleCellEditBeginning = useCallback(
    (sender: any, e: any, unitDto: any) => {
      const col = sender.columns?.[e.col];
      const rowData = sender.rows?.[e.row]?.dataItem;
      if (!rowData || !col?.binding) return;

      // Placeholder row for child-grid calculation: only allow editing the sort order.
      if (rowData.ChildTransactionUnitId) {
        if (col.binding !== 'CaculationFlowSort') {
          e.cancel = true;
        }
        return;
      }

      if (col.binding === 'FormulaExpression') {
        const op = rowData.OperationType;
        if (
          op === EmAppFormularType.Assignment ||
          op === EmAppFormularType.SqlScarlarAssignment ||
          op === EmAppFormularType.SubscribeFromGridColumnAggregation ||
          op === EmAppFormularType.SubscribeFromParentLevelField
        ) {
          if (rowData.AssignToTransFieldId) {
            let initialText = rowData.FormulaExpression || '';
            const eq = initialText.indexOf('=');
            if (eq >= 0 && initialText.length > eq) initialText = initialText.substring(eq + 1).trim();
            setFormulaDialogState({
              isOpen: true,
              transFieldId: rowData.AssignToTransFieldId,
              formulaDto: rowData,
              unitDto,
              initialFormulaText: initialText,
            });
          }
          e.cancel = true;
        }
      }

      // Capture original values so we can avoid clearing expression when user enters edit but doesn't change value.
      if (col.binding === 'OperationType') {
        (rowData as any).__origOperationType = rowData.OperationType;
      } else if (col.binding === 'AssignToTransFieldId') {
        (rowData as any).__origAssignToTransFieldId = rowData.AssignToTransFieldId;
      }
    },
    []
  );

  const handleCellEditEnded = useCallback(
    (sender: any, e: any, unitDto: any) => {
      const col = sender.columns?.[e.col];
      const rowData = sender.rows?.[e.row]?.dataItem;
      if (!rowData || !col?.binding) return;
      if (col.binding === 'OperationType') {
        const orig = (rowData as any).__origOperationType;
        // Only clear/reset when the value actually changed.
        if (orig !== undefined && String(orig) === String(rowData.OperationType)) {
          // no-op
        } else {
          rowData.AssignToTransFieldId = null;
          rowData.FormulaExpression = '';
          rowData.FunctionType = null;
          rowData.ConditionFieldId = null;
          rowData.SwitchTrueFalseType = false;
          rowData.WarningMessage = '';
          if (rowData.OperationType === EmAppFormularType.SubscribeFromGridColumnAggregation) {
            rowData.CaculationFlowSort = 'After Grid Calculation';
          } else if (rowData.OperationType === EmAppFormularType.SubscribeFromParentLevelField) {
            rowData.CaculationFlowSort = 'Before Grid Calculation';
          }
        }
        try {
          delete (rowData as any).__origOperationType;
        } catch {
          // ignore
        }
      } else if (col.binding === 'AssignToTransFieldId') {
        const orig = (rowData as any).__origAssignToTransFieldId;
        if (orig !== undefined && String(orig) === String(rowData.AssignToTransFieldId)) {
          // no-op
        } else {
          rowData.FormulaExpression = '';
        }
        try {
          delete (rowData as any).__origAssignToTransFieldId;
        } catch {
          // ignore
        }
      }

      if (col.binding === 'CaculationFlowSort') {
        // Defer resort until after Wijmo commits the edited value to the underlying data + CollectionView.
        try {
          const flex = sender?.control ?? sender;
          // Ensure the edit is committed before resorting.
          flex?.finishEditing?.();
          flex?.invalidate?.();
        } catch {
          // ignore
        }

        setTimeout(() => {
          const flex = sender?.control ?? sender;
          const cv = (flex?.collectionView as CollectionView | undefined) ?? unitDto.currentUnitFormulaCV;
          const opts = {
            // IMPORTANT: after Refresh, use the live CV collection (not unitDto.currentUnitFormulaList) so placeholder+aggregate
            // compute together immediately.
            getPlaceholderSortByChildUnitId: (childUnitId: number) => {
              const list = (cv?.sourceCollection as any[]) || [];
              const row = list.find((x: any) => Number(x?.ChildTransactionUnitId) === Number(childUnitId));
              const n = row ? parseInt(String(row.CaculationFlowSort ?? ''), 10) : NaN;
              return Number.isFinite(n) ? n : null;
            },
            getCrossSettingByAssignToFieldId: (assignToFieldId: number) => {
              const dict = appTransactionData?.DictTransFieldIdAndCrossRelationSettingDto || {};
              return (dict as any)?.[assignToFieldId] ?? (dict as any)?.[String(assignToFieldId)] ?? null;
            },
            getUnitIdByFieldId: (fieldId: number) => {
              const f =
                (dictTransactionFieldIdAndDto as any)?.[fieldId] ??
                (dictTransactionFieldIdAndDto as any)?.[String(fieldId)] ??
                null;
              const unitId = f?.TransactionUnitId;
              return unitId != null ? Number(unitId) : null;
            }
          };
          try {
            // After Refresh/reload, ensure the CollectionView commits the edit before resorting.
            // Commit through the grid's CV when possible (more reliable than unitDto ref after refresh).
            cv?.commitEdit?.();
            cv?.refresh?.();
          } catch {
            // ignore
          }
          try {
            applyFormulaSort(cv, opts);
            flex?.invalidate?.();
          } catch {
            // ignore
          }
        }, 0);
      }
      markChange(unitDto);
      setRootUnit((r: any) => (r ? { ...r } : r));
    },
    [markChange, appTransactionData, dictTransactionFieldIdAndDto]
  );

  const rootUnitFieldDataMap = rootUnit?.rootLevelUnitFieldDataMap ?? null;
  const rootUnitFormularTypeDataMap = rootUnit?.appFormularTypeDatamap ?? null;
  const rootUnitConditionDataMap = rootUnit?.conditionFiledDataMap ?? null;
  const appFormularFunctionTypeDataMap = useMemo(
    () => (appFormularFunctionTypeList.length ? new DataMap(appFormularFunctionTypeList, 'Id', 'Display') : null),
    [appFormularFunctionTypeList]
  );
  const warningHighlightDataMap = useMemo(
    () => (warningHighlightPriorityList.length ? new DataMap(warningHighlightPriorityList, 'Id', 'Display') : null),
    [warningHighlightPriorityList]
  );

  if (!transactionId) {
    return (
      <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Calculation Validation Flow</div>
        </div>
        <div className={`w-full h-[200px] flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Select a data model first to configure Calculation Validation Flow.</p>
          </div>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Calculation Validation Flow</div>
        </div>
        <div className={`w-full h-[200px] flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Calculation Validation Flow</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={refresh}
            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden="true" /> Refresh
          </button>
          <button
            type="button"
            onClick={() => save()}
            disabled={!allowSave}
            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed`}
            title="Save"
          >
            <i className="fa fa-save" aria-hidden="true" /> Save
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="h-full w-full overflow-auto px-5 py-5">
        <div className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 mb-4">
          <label className={`text-xs ${theme.label}`}>Data Model Name</label>
          <input
            type="text"
            readOnly
            value={appTransactionData?.TransactionName ?? ''}
            className={`w-full max-w-md h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
          />
          <label className={`text-xs ${theme.label}`}>Description</label>
          <input
            type="text"
            readOnly
            value={appTransactionData?.Description ?? ''}
            className={`w-full max-w-md h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
          />
          <label className={`text-xs ${theme.label}`}>Data Model Type</label>
          <div className="max-w-md">
            <ComboBox
              itemsSource={new CollectionView(transactionOrganizedTypeList)}
              displayMemberPath="Display"
              selectedValuePath="Id"
              selectedValue={appTransactionData?.TransactionOrganizedType ?? null}
              isReadOnly={true}
            />
          </div>
        </div>

        <div className="border rounded overflow-hidden">
          <div className={`flex items-center justify-between px-3 py-1.5 border-b ${theme.mainContentSection}`}>
            <span className={`text-sm font-semibold ${theme.title}`}>Form Field Formula:</span>
            <div className="flex items-center space-x-2">
              <button
                type="button"
                onClick={() => addFormula(rootUnit)}
                className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                title="Add Formula"
              >
                <i className="fa fa-plus" aria-hidden="true" /> Add Formula
              </button>
              <button
                type="button"
                onClick={() => deleteFormula(rootUnit)}
                className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                title="Delete Formula"
              >
                <i className="fa fa-minus" aria-hidden="true" /> Delete Formula
              </button>
            </div>
          </div>
          <div className="overflow-auto px-4 pb-2" style={{ minHeight: 280 }} id="FormularGridContainer">
            {rootUnitFormulaCV && (
              <FlexGrid
                ref={rootGridRef}
                itemsSource={rootUnitFormulaCV}
                isReadOnly={false}
                beginningEdit={(s: any, e: any) => handleCellEditBeginning(s, e, rootUnit)}
                cellEditEnded={(s: any, e: any) => handleCellEditEnded(s, e, rootUnit)}
                selectionChanged={(s: any) => {
                  try {
                    const flex = s?.control ?? s;
                    const rowIndex = flex?.selection?.row;
                    const item = typeof rowIndex === 'number' && rowIndex >= 0 ? flex?.rows?.[rowIndex]?.dataItem : null;
                    if (item && rootUnit?.Id != null) {
                      lastSelectedFormulaByUnitIdRef.current[Number(rootUnit.Id)] = item;
                    }
                  } catch {
                    // ignore
                  }
                }}
                headersVisibility="Column"
                itemFormatter={(panel: any, r: number, c: number, cell: HTMLElement) => {
                  try {
                    if (panel?.cellType !== 1) return; // Cell
                    const col = panel.columns?.[c];
                    if (col?.binding === '__gridOverlay') {
                      // Anchor absolute overlay to the cell itself and allow it to cover following cells.
                      cell.style.position = 'relative';
                      cell.style.overflow = 'visible';
                    }
                  } catch {
                    // ignore
                  }
                }}
              >
                <FlexGridColumn header="" binding="" width={40} allowSorting={false} isReadOnly={true}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell?.item;
                      const childUnitId = item?.ChildTransactionUnitId;
                      if (!childUnitId) return <div className="w-full h-full" />;
                      return (
                        <div className="w-full h-full flex items-center justify-center">
                          <button
                            type="button"
                            className={`px-1.5 py-0.5 text-xs rounded-[4px] ${theme.button_default}`}
                            title="Edit grid formulas"
                            onClick={(e) => {
                              e.preventDefault();
                              e.stopPropagation();
                              openUnitPopup(Number(childUnitId));
                            }}
                          >
                            <i className="fa fa-plus" aria-hidden="true" />
                          </button>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="CaculationFlowSort" header="Calculation Flow" width={180} />
                <FlexGridColumn header="" binding="__gridOverlay" width={1} allowSorting={false} isReadOnly={true}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => {
                      const item = cell?.item;
                      const childUnitId = item?.ChildTransactionUnitId;
                      if (!childUnitId) return <div className="w-full h-full" />;
                      const childUnit = dictUnitIdAndDto?.[Number(childUnitId)];
                      const name = childUnit?.UnitDisplayName || childUnit?.DataBaseTableName || childUnitId;
                      return (
                        <div
                          className={`${theme.mainContentSection}`}
                          style={{
                            position: 'absolute',
                            left: 0,
                            top: 0,
                            height: '100%',
                            width: '9999px',
                            zIndex: 50,
                            display: 'flex',
                            alignItems: 'center',
                            pointerEvents: 'none',
                            paddingLeft: '8px'
                          }}
                        >
                          <i className="fa-solid fa-table-cells mr-2" aria-hidden="true" />
                          {`Grid ${name} Formulars`}
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn
                  binding="OperationType"
                  header="Operation Type"
                  width={230}
                  dataMap={rootUnitFormularTypeDataMap}
                  isReadOnly={false}
                />
                <FlexGridColumn
                  binding="AssignToTransFieldId"
                  header="Assign To Field"
                  width={200}
                  dataMap={rootUnitFieldDataMap}
                  isReadOnly={false}
                />
                <FlexGridColumn binding="FormulaExpression" header="Expression" width={400} isReadOnly={false} />
                <FlexGridColumn
                  binding="FunctionType"
                  header="Function Type"
                  width={120}
                  dataMap={appFormularFunctionTypeDataMap}
                />
                <FlexGridColumn
                  binding="ConditionFieldId"
                  header="Condition Field"
                  width={150}
                  dataMap={rootUnitConditionDataMap}
                />
                <FlexGridColumn binding="SwitchTrueFalseType" header="Condition Toggle" width={120} />
                <FlexGridColumn binding="WarningMessage" header="Warning Message" width={300} />
                <FlexGridColumn
                  binding="WarningHighlightTransFieldId"
                  header="Highlight Field"
                  width={150}
                  dataMap={rootUnitFieldDataMap}
                />
                <FlexGridColumn
                  binding="WarningHighlightStyleId"
                  header="Highlight Priority"
                  width={150}
                  dataMap={warningHighlightDataMap}
                />
              </FlexGrid>
            )}
          </div>
        </div>
        </div>
      </div>

      {/* Unit popup editor (child/grandchild). Uses a stack for nested popups. */}
      {popupUnitStack.length > 0 && (() => {
        const unitId = popupUnitStack[popupUnitStack.length - 1];
        const unitDto = dictUnitIdAndDto?.[unitId];
        const titleName = unitDto?.UnitDisplayName || unitDto?.DataBaseTableName || unitId;

        return (
          <div
            className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-[10000]"
          >
            <div
              className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
              style={{ width: '1100px', height: '720px', maxWidth: '96vw', maxHeight: '96vh' }}
              onClick={(e) => e.stopPropagation()}
            >
              <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
                <div className="flex items-center gap-2">
                  {popupUnitStack.length > 1 && (
                    <button
                      type="button"
                      className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                      onClick={() => closeTopPopup()}
                      title="Back"
                    >
                      Back
                    </button>
                  )}
                  <div className={`text-sm font-semibold ${theme.title}`}>{`Grid ${titleName} Formulars`}</div>
                </div>
                <button
                  type="button"
                  onClick={() => {
                    // Close only the top popup; keep parent popup open (grandchild -> child).
                    if (popupUnitStack.length > 1) closeTopPopup();
                    else setPopupUnitStack([]);
                  }}
                  className={`p-1 ${theme.button_default} rounded`}
                  title="Close"
                >
                  <i className="fa fa-times" aria-hidden="true" />
                </button>
              </div>

              <div className="flex items-center justify-end gap-2 px-4 py-2">
                <button
                  type="button"
                  onMouseDown={(e) => e.stopPropagation()}
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    addFormula(unitDto);
                    setRootUnit((r: any) => (r ? { ...r } : r));
                  }}
                  className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                  title="Add Formula"
                >
                  <i className="fa fa-plus" aria-hidden="true" /> Add Formula
                </button>
                <button
                  type="button"
                  onMouseDown={(e) => e.stopPropagation()}
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    deleteFormula(unitDto);
                    setRootUnit((r: any) => (r ? { ...r } : r));
                  }}
                  className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                  title="Delete Formula"
                >
                  <i className="fa fa-minus" aria-hidden="true" /> Delete Formula
                </button>
              </div>

              <div className="flex-1 overflow-hidden px-4 pb-4">
                {!unitDto && (
                  <div className="h-full w-full flex items-center justify-center">
                    <div className={`text-xs ${theme.label}`}>Loading...</div>
                  </div>
                )}
                {unitDto?.currentUnitFormulaCV && (
                  <div className="h-full w-full overflow-auto">
                    <FlexGrid
                      initialized={(s: any) => {
                        try {
                          if (s != null && unitDto?.Id != null) {
                            const key = Number(unitDto.Id);
                            (childGridRefs.current as Record<number, any>)[key] = (s as any)?.control ?? s;
                          }
                        } catch {
                          // ignore
                        }
                      }}
                      ref={(g: any) => {
                        if (g != null && unitDto?.Id != null) {
                          const key = Number(unitDto.Id);
                          (childGridRefs.current as Record<number, any>)[key] = (g as any)?.control ?? g;
                        }
                      }}
                      itemsSource={unitDto.currentUnitFormulaCV}
                      selectionMode="Row"
                      isReadOnly={false}
                      beginningEdit={(s: any, e: any) => handleCellEditBeginning(s, e, unitDto)}
                      cellEditEnded={(s: any, e: any) => handleCellEditEnded(s, e, unitDto)}
                      selectionChanged={(s: any) => {
                        try {
                          const flex = s?.control ?? s;
                          const rowIndex = flex?.selection?.row;
                          const item = typeof rowIndex === 'number' && rowIndex >= 0 ? flex?.rows?.[rowIndex]?.dataItem : null;
                          if (item && unitDto?.Id != null) {
                            lastSelectedFormulaByUnitIdRef.current[Number(unitDto.Id)] = item;
                            try {
                              if (flex?.collectionView) flex.collectionView.currentItem = item;
                            } catch {
                              // ignore
                            }
                          }
                        } catch {
                          // ignore
                        }
                      }}
                      headersVisibility="Column"
                      className="w-full h-full"
                      itemFormatter={(panel: any, r: number, c: number, cell: HTMLElement) => {
                        try {
                          if (panel?.cellType !== 1) return; // Cell
                          const col = panel.columns?.[c];
                          if (col?.binding === '__gridOverlay') {
                            // Anchor absolute overlay to the cell itself and allow it to cover following cells.
                            cell.style.position = 'relative';
                            cell.style.overflow = 'visible';
                          }
                        } catch {
                          // ignore
                        }
                      }}
                    >
                      <FlexGridColumn header="" binding="" width={40} allowSorting={false} isReadOnly={true}>
                        <FlexGridCellTemplate
                          cellType="Cell"
                          template={(cell: any) => {
                            const item = cell?.item;
                            const childUnitId = item?.ChildTransactionUnitId;
                            if (!childUnitId) return <div className="w-full h-full" />;
                            return (
                              <div className="w-full h-full flex items-center justify-center">
                                <button
                                  type="button"
                                  className={`px-1.5 py-0.5 text-xs rounded-[4px] ${theme.button_default}`}
                                  title="Edit sub grid formulas"
                                  onClick={(e) => {
                                    e.preventDefault();
                                    e.stopPropagation();
                                    openUnitPopup(Number(childUnitId));
                                  }}
                                >
                                  <i className="fa fa-plus" aria-hidden="true" />
                                </button>
                              </div>
                            );
                          }}
                        />
                      </FlexGridColumn>

                      <FlexGridColumn binding="CaculationFlowSort" header="Calculation Flow" width={180} />
                      <FlexGridColumn header="" binding="__gridOverlay" width={1} allowSorting={false} isReadOnly={true}>
                        <FlexGridCellTemplate
                          cellType="Cell"
                          template={(cell: any) => {
                            const item = cell?.item;
                            const childUnitId = item?.ChildTransactionUnitId;
                            if (!childUnitId) return <div className="w-full h-full" />;
                            const childUnit = dictUnitIdAndDto?.[Number(childUnitId)];
                            const name = childUnit?.UnitDisplayName || childUnit?.DataBaseTableName || childUnitId;
                            return (
                              <div
                                className={`${theme.mainContentSection}`}
                                style={{
                                  position: 'absolute',
                                  left: 0,
                                  top: 0,
                                  height: '100%',
                                  width: '9999px',
                                  zIndex: 50,
                                  display: 'flex',
                                  alignItems: 'center',
                                  pointerEvents: 'none',
                                  paddingLeft: '8px'
                                }}
                              >
                                <i className="fa-solid fa-table-cells mr-2" aria-hidden="true" />
                                {`Grid ${name} Formulars`}
                              </div>
                            );
                          }}
                        />
                      </FlexGridColumn>
                      <FlexGridColumn
                        binding="OperationType"
                        header="Operation Type"
                        width={230}
                        dataMap={unitDto.appFormularTypeDatamap}
                      />
                      <FlexGridColumn
                        binding="AssignToTransFieldId"
                        header="Assign To Column"
                        width={220}
                        dataMap={unitDto.transFiledDataMap}
                      />
                      <FlexGridColumn binding="FormulaExpression" header="Expression" width={420} />
                      <FlexGridColumn
                        binding="FunctionType"
                        header="Function Type"
                        width={140}
                        dataMap={appFormularFunctionTypeDataMap}
                      />
                      <FlexGridColumn
                        binding="ConditionFieldId"
                        header="Condition Field"
                        width={170}
                        dataMap={unitDto.conditionFiledDataMap}
                      />
                      <FlexGridColumn binding="SwitchTrueFalseType" header="Condition Toggle" width={140} />
                      <FlexGridColumn binding="WarningMessage" header="Warning Message" width={280} />
                    </FlexGrid>
                  </div>
                )}
              </div>
            </div>
          </div>
        );
      })()}

      {formulaDialogState.isOpen && transactionDataForDialog && (
        <FieldFormulaDialog
          isOpen={true}
          transactionData={transactionDataForDialog}
          currentFieldId={formulaDialogState.transFieldId}
          initialFormulaText={formulaDialogState.initialFormulaText}
          tokenMode={formulaDialogState.unitDto?.level > 1 ? 'unitOnly' : 'default'}
          onClose={() => setFormulaDialogState((s) => ({ ...s, isOpen: false }))}
          onConfirm={handleFormulaDialogConfirm}
        />
      )}
    </div>
  );
};

export default TransactionFormulaEditor;
