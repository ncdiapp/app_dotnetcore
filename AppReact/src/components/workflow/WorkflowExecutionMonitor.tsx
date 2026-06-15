/**
 * Workflow Execution Monitor – migrated from Angular workflowExecutionMonitorCtrl + WorkflowExecutionMonitor.cshtml.
 * Route: workflow-execution-monitor/:param (param JSON: { id: workflowTransactionId, param1: rootPrimaryKeyValue })
 * Or embedded in FormMainMenus popup via embedded prop.
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { CellRange } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
export type WorkflowExecutionMonitorEmbeddedProps = {
  workflowTransactionId: number;
  rootPrimaryKeyValue: string | number;
};

export type WorkflowExecutionMonitorProps = {
  embedded?: WorkflowExecutionMonitorEmbeddedProps;
};

function parseRouteParam(raw: string | undefined): Record<string, any> | null {
  if (!raw) return null;
  try {
    return JSON.parse(decodeURIComponent(raw));
  } catch {
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }
}

function formatDateTime(value: unknown): string {
  if (value == null || value === '') return '';
  if (value instanceof Date) return value.toLocaleString();
  return String(value);
}

function formatRuntimeFieldValue(value: unknown): string {
  if (value == null) return '';
  if (value instanceof Date) return value.toLocaleString();
  if (typeof value === 'object') {
    try {
      return JSON.stringify(value);
    } catch {
      return String(value);
    }
  }
  return String(value);
}

function buildParameterRows(transactionFields: any[] | undefined, runtime: any): any[] {
  const dict = runtime?.DictOneToOneFields || {};
  const fieldList = Array.isArray(transactionFields) ? transactionFields : [];
  const source =
    fieldList.length > 0
      ? fieldList
      : Object.keys(dict).map((key) => ({
          DisplayName: key,
          DataBaseFieldName: key,
        }));
  return source.map((field) => {
    const dbName = field?.DataBaseFieldName ?? field?.DisplayName ?? '';
    return {
      ...field,
      RuntimeValue: formatRuntimeFieldValue(dbName ? dict[dbName] : ''),
    };
  });
}

export default function WorkflowExecutionMonitor({ embedded }: WorkflowExecutionMonitorProps) {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();
  const { param } = useParams<{ param?: string }>();
  const parsed = parseRouteParam(param);

  const workflowTransactionId = embedded?.workflowTransactionId ?? Number(parsed?.id ?? 0);
  const rootPrimaryKeyValue = embedded?.rootPrimaryKeyValue ?? parsed?.param1 ?? null;

  const [workflowRuntimeData, setWorkflowRuntimeData] = useState<any>(null);
  const [transactionName, setTransactionName] = useState('');
  const [parameterCV, setParameterCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [commandNodeTreeCV, setCommandNodeTreeCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [transactionNameById, setTransactionNameById] = useState<Record<number, string>>({});
  const [transactionFields, setTransactionFields] = useState<any[]>([]);

  const commandGridRef = useRef<any>(null);
  const parameterGridRef = useRef<any>(null);
  const executingNodeLogicIdRef = useRef<string | null>(null);
  const runtimeFieldsRef = useRef<Record<string, unknown>>({});

  const getCommandDataModelDisplay = useCallback(
    (externalTransactionId: number | null | undefined) => {
      if (externalTransactionId != null) {
        return transactionNameById[Number(externalTransactionId)] ?? String(externalTransactionId);
      }
      return transactionName;
    },
    [transactionName, transactionNameById],
  );

  const applyParameterGrid = useCallback((fields: any[], runtime: any) => {
    const rows = buildParameterRows(fields, runtime);
    runtimeFieldsRef.current = (runtime?.DictOneToOneFields || {}) as Record<string, unknown>;
    setParameterCV(new CollectionView(rows));
    setTimeout(() => {
      const grid = parameterGridRef.current?.control ?? parameterGridRef.current;
      grid?.invalidate?.();
      grid?.refresh?.();
    }, 0);
  }, []);

  const selectExecutingTreeNode = useCallback((executingNodeLogicId: string | null) => {
    executingNodeLogicIdRef.current = executingNodeLogicId;
    if (!executingNodeLogicId) return;
    const grid = commandGridRef.current?.control ?? commandGridRef.current;
    if (!grid?.rows?.length) return;
    for (let i = 0; i < grid.rows.length; i++) {
      const rowItem = grid.rows[i]?.dataItem;
      if (rowItem?.TreeNodeLogicId === executingNodeLogicId) {
        grid.select(new CellRange(i, 0, i, 0), false);
        grid.invalidate();
        break;
      }
    }
  }, []);

  const loadHierarchyAndRuntime = useCallback(async () => {
    if (!workflowTransactionId || rootPrimaryKeyValue == null || String(rootPrimaryKeyValue).trim() === '') {
      return;
    }
    dispatch(setIsBusy());
    try {
      const [hierarchy, runtime] = await Promise.all([
        appTransactionService.getOneHierarchyTransaction(String(workflowTransactionId), false, '', '', '', false, ''),
        appTransactionService.getWorkflowAutomationRuntimeProgressData(workflowTransactionId, rootPrimaryKeyValue),
      ]);

      const unit = hierarchy?.AppTransactionUnitList?.[0];
      const fields = [...(unit?.AppTransactionFieldList || [])].sort(
        (a, b) => (a?.SortOrder ?? 0) - (b?.SortOrder ?? 0),
      );

      if (hierarchy) {
        setTransactionName(hierarchy.TransactionName || '');
        setTransactionFields(fields);
      }

      if (runtime) {
        setWorkflowRuntimeData(runtime);
        const tree = runtime.WorkflowCommandNodeTree || [];
        setCommandNodeTreeCV(new CollectionView(tree));
        applyParameterGrid(fields, runtime);
        setTimeout(() => selectExecutingTreeNode(runtime.ExecutingNodeLogicId ?? null), 0);
      } else if (fields.length > 0) {
        applyParameterGrid(fields, null);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load workflow execution monitor');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    dispatch,
    rootPrimaryKeyValue,
    applyParameterGrid,
    selectExecutingTreeNode,
    showError,
    workflowTransactionId,
  ]);

  useEffect(() => {
    void loadHierarchyAndRuntime();
  }, [loadHierarchyAndRuntime]);

  useEffect(() => {
    void appTransactionService.retrieveAllAppTransactions(null, null, true).then((list) => {
      const map: Record<number, string> = {};
      (list || []).forEach((t: any) => {
        if (t?.Id != null) map[Number(t.Id)] = t.TransactionName || String(t.Id);
      });
      setTransactionNameById(map);
    });
  }, []);

  const refresh = useCallback(async () => {
    if (!workflowTransactionId || rootPrimaryKeyValue == null) return;
    dispatch(setIsBusy());
    try {
      const runtime = await appTransactionService.getWorkflowAutomationRuntimeProgressData(
        workflowTransactionId,
        rootPrimaryKeyValue,
      );
      if (runtime) {
        setWorkflowRuntimeData(runtime);
        const tree = runtime.WorkflowCommandNodeTree || [];
        setCommandNodeTreeCV(new CollectionView(tree));
        applyParameterGrid(transactionFields, runtime);
        setTimeout(() => selectExecutingTreeNode(runtime.ExecutingNodeLogicId ?? null), 0);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to refresh workflow execution');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    applyParameterGrid,
    dispatch,
    rootPrimaryKeyValue,
    selectExecutingTreeNode,
    showError,
    transactionFields,
    workflowTransactionId,
  ]);

  const forceStopWorkflow = useCallback(async () => {
    const batchNumber = workflowRuntimeData?.BatchNumber;
    if (!batchNumber || workflowRuntimeData?.EndTime) return;
    const confirmed = window.confirm(
      'Do you want to force abort the workflow execution?\nClick OK to confirm abort.',
    );
    if (!confirmed) return;
    dispatch(setIsBusy());
    try {
      const data = await appTransactionService.forceStopWorkflowByBatchNumber(
        String(batchNumber),
        rootPrimaryKeyValue ?? '',
      );
      if (data?.ValidationResult) showValidationMessages(data.ValidationResult);
      if (data?.IsSuccessful) {
        setTimeout(() => void refresh(), 3000);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to force stop workflow');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    dispatch,
    refresh,
    rootPrimaryKeyValue,
    showError,
    showValidationMessages,
    workflowRuntimeData?.BatchNumber,
    workflowRuntimeData?.EndTime,
  ]);

  runtimeFieldsRef.current = (workflowRuntimeData?.DictOneToOneFields || {}) as Record<string, unknown>;
  const executingNodeLogicId = workflowRuntimeData?.ExecutingNodeLogicId ?? null;
  const showForceAbort =
    !!workflowRuntimeData?.BatchNumber &&
    !workflowRuntimeData?.EndTime &&
    workflowRuntimeData?.WorkflowProgressStatus === 'Started';

  if (!workflowTransactionId || rootPrimaryKeyValue == null) {
    return (
      <div className={`w-full h-full flex items-center justify-center p-4 ${theme.mainContentSection}`}>
        <p className={`text-sm ${theme.label}`}>Missing workflow transaction or record id.</p>
      </div>
    );
  }

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between flex-wrap gap-2 px-3 py-2 border-b ${theme.mainContentSection}`}>
        <div className={`flex flex-wrap items-center gap-3 text-sm ${theme.title}`}>
          <span className="font-semibold">Workflow Execution Monitor</span>
          <span className={`flex items-center gap-1 text-xs ${theme.label}`}>
            <span>Status</span>
            <input
              className={`w-28 h-7 px-2 text-xs border ${theme.inputBox}`}
              value={workflowRuntimeData?.WorkflowProgressStatus ?? ''}
              readOnly
              disabled
            />
          </span>
          <span className={`flex items-center gap-1 text-xs ${theme.label}`}>
            <span>Start Time</span>
            <input
              className={`w-40 h-7 px-2 text-xs border ${theme.inputBox}`}
              value={formatDateTime(workflowRuntimeData?.StartTime)}
              readOnly
              disabled
            />
          </span>
          <span className={`flex items-center gap-1 text-xs ${theme.label}`}>
            <span>End Time</span>
            <input
              className={`w-40 h-7 px-2 text-xs border ${theme.inputBox}`}
              value={formatDateTime(workflowRuntimeData?.EndTime)}
              readOnly
              disabled
            />
          </span>
        </div>
        <div className="flex items-center gap-2">
          {showForceAbort ? (
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => void forceStopWorkflow()}
            >
              <i className="fa-solid fa-circle-stop mr-1" aria-hidden />
              Force Abort
            </button>
          ) : null}
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => void refresh()}
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden />
            Refresh
          </button>
        </div>
      </div>

      <div className="w-full h-1 flex-auto flex gap-2 p-2 overflow-hidden min-h-0 min-w-0">
        <div className={`w-[60%] min-w-[300px] h-full min-h-0 flex flex-col border rounded overflow-hidden ${theme.mainContentSection}`}>
          <div className={`px-2 py-1.5 text-sm font-semibold border-b shrink-0 ${theme.title}`}>Workflow Operation Tasks</div>
          <div className="h-1 w-full flex-auto min-h-0 pl-5 overflow-hidden">
            <FlexGrid
              ref={commandGridRef}
              itemsSource={commandNodeTreeCV}
              childItemsPath="WorkflowChildTreeNodes"
              headersVisibility="Column"
              selectionMode="Row"
              allowSorting={false}
              className="h-full w-full"
              style={{ height: '100%', width: '100%' }}
            >
              <FlexGridFilter />
              <FlexGridCellTemplate
                cellType="RowHeader"
                template={(cell: any) => {
                  const item = cell.item;
                  if (item?.TreeNodeLogicId !== executingNodeLogicId) return null;
                  return (
                    <div className="w-full h-full flex items-center justify-center px-1">
                      <i className="fa-solid fa-arrow-right text-[#555]" aria-hidden />
                    </div>
                  );
                }}
              />
              <FlexGridColumn binding="DisplayName" header="Name" width={300} isReadOnly={true} />
              <FlexGridColumn header="Data Model" width={150} isReadOnly={true}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => (
                    <span>{getCommandDataModelDisplay(cell.item?.ExternalTransactionId)}</span>
                  )}
                />
              </FlexGridColumn>
              <FlexGridColumn binding="ProgressStatus" header="Progress Status" width={150} isReadOnly={true} />
              <FlexGridColumn binding="ErrorMessage" header="Status Details" width="*" isReadOnly={true} />
              <FlexGridColumn header="" binding="" width={25} isReadOnly={true} />
            </FlexGrid>
          </div>
        </div>

        <div className={`w-[40%] min-w-[300px] h-full min-h-0 flex flex-col border rounded overflow-hidden ${theme.mainContentSection}`}>
          <div className={`px-2 py-1.5 text-sm font-semibold border-b shrink-0 ${theme.title}`}>
            Parameter Runtime Values
          </div>
          <div className="h-1 w-full flex-auto min-h-0 overflow-hidden">
            <FlexGrid
              ref={parameterGridRef}
              itemsSource={parameterCV}
              headersVisibility="Column"
              allowSorting={false}
              className="h-full w-full"
              style={{ height: '100%', width: '100%' }}
            >
              <FlexGridFilter />
              <FlexGridColumn binding="DisplayName" header="Name" width={200} isReadOnly={true} />
              <FlexGridColumn header="Current Value" binding="RuntimeValue" width="*" isReadOnly={true}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const field = cell.item;
                    const dbName = field?.DataBaseFieldName ?? field?.DisplayName ?? '';
                    const val = dbName ? runtimeFieldsRef.current[dbName] : field?.RuntimeValue;
                    return <span className="px-1">{formatRuntimeFieldValue(val)}</span>;
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn header="" binding="" width={25} isReadOnly={true} />
            </FlexGrid>
          </div>
        </div>
      </div>
    </div>
  );
}
