/**
 * Data Model Condition Lock (Conditional Lock or Hide)
 * Ported from AngularJS: TransactionConditionLockEditor.cshtml + transactionConditionLockEditorCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionConditionLockEditor.cshtml
 *            C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Transaction\transactionConditionLockEditorCtrl.js
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { appMessageService } from '../../../webapi/appmessagesvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import ConditionFormulaDialog from './ConditionFormulaDialog';

const CHECKBOX_CONTROL_TYPE_ID = 13;

export interface TransactionConditionLockEditorProps {
  transactionId: number | null;
  transactionName?: string | null;
  onRefresh?: () => void;
}

const TransactionConditionLockEditor: React.FC<TransactionConditionLockEditorProps> = ({
  transactionId,
  transactionName: transactionNameProp,
  onRefresh,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [transactionName, setTransactionName] = useState(transactionNameProp || '');
  const [conditionActionList, setConditionActionList] = useState<any[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<number[]>([]);
  const [conditionFieldDataMap, setConditionFieldDataMap] = useState<DataMap | null>(null);
  const [lockingFieldDataMap, setLockingFieldDataMap] = useState<DataMap | null>(null);
  const [lockingUnitDataMap, setLockingUnitDataMap] = useState<DataMap | null>(null);
  const [notificationTemplateDataMap, setNotificationTemplateDataMap] = useState<DataMap | null>(null);
  const [transactionUnitList, setTransactionUnitList] = useState<any[]>([]);

  const [formulaDialogState, setFormulaDialogState] = useState<{
    isOpen: boolean;
    rowItem: any;
    initialText: string;
  }>({ isOpen: false, rowItem: null, initialText: '' });

  const gridRef = useRef<any>(null);
  const formulaEditRowRef = useRef<any>(null);

  const conditionActionCV = React.useMemo(() => {
    const cv = new CollectionView(conditionActionList);
    return cv;
  }, [conditionActionList]);

  const loadData = useCallback(async () => {
    if (!transactionId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    setDeletedItemIds([]);
    try {
      const [transactionData, conditionActionData, messageTemplateData] = await Promise.all([
        appTransactionService.getOneAppTransactionData(String(transactionId)),
        appTransactionService.retrieveAllAppConditionalActionListByTransactionId(transactionId),
        appMessageService.retrieveAllPredefinedMessageTemplates(),
      ]);

      if (transactionData) {
        setTransactionName(transactionData.TransactionName || transactionNameProp || '');
        const unitList = transactionData.AppTransactionUnitList || [];
        setTransactionUnitList(unitList);

        const allTransactionFieldList: any[] = [];
        const checkBoxFieldList: any[] = [];

        unitList.forEach((unit: any) => {
          const isRootUnit = !unit.ParentTransactionUnitId;
          (unit.AppTransactionFieldList || []).forEach((field: any) => {
            const aField = {
              Id: field.Id,
              Display: (unit.UnitDisplayName || unit.DisplayName || '') + ' : ' + (field.DisplayName || ''),
              unitId: unit.Id,
              parentUnitId: unit.ParentTransactionUnitId,
              isOnRootUnit: isRootUnit,
            };
            allTransactionFieldList.push(aField);
            if (field.ControlType === CHECKBOX_CONTROL_TYPE_ID) {
              checkBoxFieldList.push(aField);
            }
          });
        });

        setConditionFieldDataMap(new DataMap(checkBoxFieldList, 'Id', 'Display'));
        setLockingFieldDataMap(new DataMap(allTransactionFieldList, 'Id', 'Display'));
        setLockingUnitDataMap(new DataMap(unitList, 'Id', 'UnitDisplayName'));
      }

      if (messageTemplateData) {
        const list = Array.isArray(messageTemplateData) ? messageTemplateData : messageTemplateData?.Items ?? [];
        setNotificationTemplateDataMap(new DataMap(list, 'Id', 'Subject'));
      }

      const list = Array.isArray(conditionActionData) ? conditionActionData : conditionActionData?.Items ?? [];
      setConditionActionList(list);
    } catch (e: any) {
      showError(e?.message || 'Failed to load data');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, transactionNameProp, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [transactionId]);

  const findTransactionFieldById = useCallback(
    (fieldId: number) => {
      for (const unit of transactionUnitList) {
        for (const field of unit.AppTransactionFieldList || []) {
          if (field.Id === fieldId) return field;
        }
      }
      return null;
    },
    [transactionUnitList]
  );

  const prepareSaveData = useCallback(() => {
    const list = [...conditionActionList];
    let isValid = true;
    const warningMessages: string[] = [];
    let rowCount = 1;

    list.forEach((aItem: any) => {
      aItem.IsModified = true;
      if (!aItem.BooleanConditionFieldId && !aItem.BooleanConditionFormulaDisplay) {
        warningMessages.push(`Condition Field or Condition Formula is required (row ${rowCount})`);
        isValid = false;
      } else {
        const isOnRootUnit = transactionUnitList.some(
          (u: any) => (u.AppTransactionFieldList || []).some((f: any) => f.Id === aItem.BooleanConditionFieldId && !u.ParentTransactionUnitId)
        );
        if (aItem.IsLockingTransaction) {
          aItem.LockingTransactionFieldId = null;
          aItem.LockingTransactionUnitId = null;
          if (!isOnRootUnit) {
            warningMessages.push(`Condition Field must be on root unit in order to lock the transaction (row ${rowCount})`);
            isValid = false;
          }
        } else if (aItem.LockingTransactionUnitId) {
          aItem.LockingTransactionFieldId = null;
        }
      }
      rowCount++;
    });

    if (!isValid) {
      showError(warningMessages.join('\n'));
      return null;
    }
    return {
      TransactionId: transactionId,
      DeletedItemIds: deletedItemIds,
      ConditionalActionSet: list,
    };
  }, [conditionActionList, deletedItemIds, transactionId, transactionUnitList, showError]);

  const handleSave = useCallback(async () => {
    const payload = prepareSaveData();
    if (!payload) return;
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAllAppConditionalActionExDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.IsSuccessful) {
        showInfo('Conditional actions saved.');
        loadData();
        onRefresh?.();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [prepareSaveData, dispatch, showError, showValidationMessages, showInfo, loadData, onRefresh]);

  const handleAdd = useCallback(() => {
    const newItem = {
      TransactionID: transactionId,
      Name: '',
      BooleanConditionFieldId: null,
      LockingTransactionFieldId: null,
      LockingTransactionUnitId: null,
      IsLockingTransaction: false,
      IsLockForSpecailEditPrivilege: false,
    };
    setConditionActionList((prev) => [...prev, newItem]);
  }, [transactionId]);

  const handleRemove = useCallback(() => {
    const flex = gridRef.current;
    if (!flex?.selection) return;
    const rowIndex = flex.selection.row;
    const row = flex.rows?.[rowIndex]?.dataItem;
    if (!row) return;
    if (row.Id) {
      setDeletedItemIds((prev) => [...prev, row.Id]);
    }
    setConditionActionList((prev) => prev.filter((x) => x !== row));
  }, []);

  const handleCreateNotificationTemplate = useCallback(() => {
    showInfo('Open Message Editor from application navigation to create a notification template for this transaction.');
  }, [showInfo]);

  const handleCellEditBeginning = useCallback((sender: any, e: any) => {
    const col = sender.columns?.[e.col];
    const rowData = sender.rows?.[e.row]?.dataItem;
    if (!rowData || !col?.binding) return;
    if (col.binding === 'BooleanConditionFormulaDisplay') {
      e.cancel = true;
      formulaEditRowRef.current = rowData;
      setFormulaDialogState({
        isOpen: true,
        rowItem: rowData,
        initialText: rowData.BooleanConditionFormulaDisplay || '',
      });
    }
  }, []);

  const handleFormulaConfirm = useCallback((formulaText: string) => {
    const row = formulaEditRowRef.current;
    if (row) {
      row.BooleanConditionFormulaDisplay = formulaText;
      setConditionActionList((prev) => [...prev]);
    }
    formulaEditRowRef.current = null;
    setFormulaDialogState({ isOpen: false, rowItem: null, initialText: '' });
  }, []);

  const handleRefresh = useCallback(() => {
    loadData();
    onRefresh?.();
  }, [loadData, onRefresh]);

  if (!transactionId) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Data Model Condition Lock</div>
        </div>
        <div className={`w-full flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Select a data model first to configure Conditional Lock or Hide.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Data Model Condition Lock {transactionName ? `: ${transactionName}` : ''}
        </div>
        <div className="flex items-center flex-wrap space-x-2">
          <button type="button" onClick={handleRefresh} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-refresh mr-1" aria-hidden="true" /> Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-save mr-1" aria-hidden="true" /> Save
          </button>
          <button type="button" onClick={handleAdd} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-plus mr-1" aria-hidden="true" /> Add
          </button>
          <button type="button" onClick={handleRemove} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-minus mr-1" aria-hidden="true" /> Remove
          </button>
          <button type="button" onClick={handleCreateNotificationTemplate} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-envelope mr-1" aria-hidden="true" /> Create Notification Template
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {loading ? (
          <div className="p-4 flex items-center justify-center">
            <div className="busyLoader w-12 h-12" />
          </div>
        ) : (
          <FlexGrid
            ref={gridRef}
            itemsSource={conditionActionCV}
            isReadOnly={false}
            selectionMode="Row"
            frozenColumns={2}
            beginningEdit={handleCellEditBeginning}
            headersVisibility="Column"
          >
            <FlexGridColumn binding="BooleanConditionFieldId" header="Condition Field" width={400} dataMap={conditionFieldDataMap} />
            <FlexGridColumn binding="BooleanConditionFormulaDisplay" header="Condition Formula" width={400} isReadOnly={true} />
            <FlexGridColumn binding="NeedToHideTransactionFieldId" header="Need To Hide Data Model Field" width={400} dataMap={lockingFieldDataMap} />
            <FlexGridColumn binding="LockingTransactionFieldId" header="Need To Lock Data Model Field" width={400} dataMap={lockingFieldDataMap} />
            <FlexGridColumn binding="LockingTransactionUnitId" header="Need To Lock Child Unit" width={400} dataMap={lockingUnitDataMap} />
            <FlexGridColumn binding="IsLockingTransaction" header="Is Lock Data Model" width={200} dataType="Boolean" />
            <FlexGridColumn width="*" isReadOnly={true} />
          </FlexGrid>
        )}
      </div>

      <ConditionFormulaDialog
        isOpen={formulaDialogState.isOpen}
        initialFormulaText={formulaDialogState.initialText}
        onClose={() => setFormulaDialogState({ isOpen: false, rowItem: null, initialText: '' })}
        onConfirm={handleFormulaConfirm}
      />
    </div>
  );
};

export default TransactionConditionLockEditor;
