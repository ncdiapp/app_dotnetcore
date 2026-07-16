/**
 * Operation Task (Transaction Command) — Application Form Builder section.
 * Ported from AngularJS ApplicationFormBuilder → TransactionCommandActionSetting (master-detail / list-edit data models).
 * API: GetOneHierarchyTransaction, SaveOneTransactionCommandActionList, CreateOneTransactionCommand.
 * Workflow automation (BusinessScopeId === EmAppTransactionScopeUsage.WorkflowAutomation) uses the dedicated Workflow Automation Editor (tree + composition).
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import { CommandEditor } from './CommandEditor';
import {
  clearCommandEditCache,
  removeCommandFromCache,
  restoreCommand,
  stashCommand,
} from './commandEditCache';
import {
  getApplicationFormBuilderCommandActionTypeOptions,
  getTransactionCommandActionTypeDisplay,
} from '../transactionCommandActionTypes';
import { buildCommandFieldLookups } from './commandFieldLookup';
import { tw } from '../../../helper/themeHelper';
import appHelper from '../../../helper/appHelper';

const EmAppTransactionScopeUsageWorkflowAutomation = 2;
const EmAppTransactionCommandTypePrintFromMessageTemplate = 74;
const EmAppTransactionCommandTypeOpenLinkedSearch = 53;
const EmAppTransactionCommandTypeOpenFormCreationWindow = 51;
const EmAppTransactionCommandTypeOpenFormEditWindow = 55;

function normalizeCommandList(list: any[]): any[] {
  const arr = Array.isArray(list) ? [...list] : [];
  arr.forEach((c: any) => {
    if (!c.ActionAttribute) {
      c.ActionAttribute = { ChildActionList: [] };
    }
    if (!Array.isArray(c.ActionAttribute.ChildActionList)) {
      c.ActionAttribute.ChildActionList = [];
    }
  });
  return arr.sort((a: any, b: any) => (Number(a.ActionFlowOrder) || 0) - (Number(b.ActionFlowOrder) || 0));
}

export interface TransactionCommandActionEditorProps {
  transactionId: number | null;
  applicationId: string | null;
  transactionName?: string | null;
  /** EmTransactionOrganizedType — Angular uses List vs MasterDetail to pick command type list */
  transactionType?: number | null;
  /** Select this command after hierarchy load (e.g. external child command editor). */
  initialSelectedCommandId?: number | null;
  onRefresh?: () => void;
}

const TransactionCommandActionEditor: React.FC<TransactionCommandActionEditorProps> = ({
  transactionId,
  applicationId,
  transactionName,
  transactionType = null,
  initialSelectedCommandId = null,
  onRefresh,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const { showConfirm } = useAlertConfirm();
  const emAppControlTypeEnum = useEnumValues('EmAppControlType');
  const CONTROL_TYPE_CHECKBOX = Number(emAppControlTypeEnum?.CheckBox ?? 13);
  const CONTROL_TYPE_DDL = Number(emAppControlTypeEnum?.DDL ?? 1);

  const [hierarchy, setHierarchy] = useState<any>(null);
  const [commandCV] = useState(() => new CollectionView<any>([]));
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [isModified, setIsModified] = useState(false);
  const [editTick, setEditTick] = useState(0);
  const [dataReady, setDataReady] = useState(false);
  const [rootLevelTransFieldLookUpList, setRootLevelTransFieldLookUpList] = useState<any[]>([]);
  const [rootLevelConditionTransFieldLookUpList, setRootLevelConditionTransFieldLookUpList] = useState<any[]>([]);
  const [rootLevelSwitchConditionTransFieldLookUpList, setRootLevelSwitchConditionTransFieldLookUpList] = useState<any[]>([]);
  const [transactionFieldLookUpList, setTransactionFieldLookUpList] = useState<any[]>([]);
  const [rootLevelAllFieldLookUpList, setRootLevelAllFieldLookUpList] = useState<any[]>([]);
  const [taskTab, setTaskTab] = useState<'public' | 'private'>('public');

  /** Per-command unsaved edits while switching list selection; cleared on Refresh / Save+reload. */
  const commandEditCacheRef = useRef<Map<number, any>>(new Map());
  const prevSelectedCommandIdRef = useRef<number | null>(null);

  const isWorkflowAutomation = useMemo(() => {
    const bid = hierarchy?.BusinessScopeId ?? hierarchy?.businessScopeId;
    return Number(bid) === EmAppTransactionScopeUsageWorkflowAutomation || hierarchy?.IsWorkflowAutomation === true;
  }, [hierarchy]);

  const operationTypeOptions = useMemo(
    () =>
      getApplicationFormBuilderCommandActionTypeOptions(transactionType ?? hierarchy?.TransactionOrganizedType ?? null).filter(
        (x) =>
          Number(x?.Id) !== EmAppTransactionCommandTypeOpenFormCreationWindow &&
          Number(x?.Id) !== EmAppTransactionCommandTypeOpenFormEditWindow
      ),
    [transactionType, hierarchy?.TransactionOrganizedType]
  );

  const buildFieldLookup = useCallback(
    (transactionData: any) => {
      const lookups = buildCommandFieldLookups(transactionData, {
        checkbox: CONTROL_TYPE_CHECKBOX,
        ddl: CONTROL_TYPE_DDL,
      });
      setRootLevelTransFieldLookUpList(lookups.rootLevelTransFieldLookUpList);
      setRootLevelConditionTransFieldLookUpList(lookups.rootLevelConditionTransFieldLookUpList);
      setRootLevelSwitchConditionTransFieldLookUpList(lookups.rootLevelSwitchConditionTransFieldLookUpList);
      setTransactionFieldLookUpList(lookups.transactionFieldLookUpList);
      setRootLevelAllFieldLookUpList(lookups.rootLevelAllFieldLookUpList);
    },
    [CONTROL_TYPE_CHECKBOX, CONTROL_TYPE_DDL],
  );

  const applyCommandListToGrid = useCallback(
    (list: any[]) => {
      const sorted = normalizeCommandList(list);
      commandCV.sourceCollection = sorted;
      commandCV.sortDescriptions.clear();
      commandCV.sortDescriptions.push(new SortDescription('ActionFlowOrder', true));
      commandCV.refresh();
    },
    [commandCV]
  );

  const loadTransaction = useCallback(async (preferredSelectedId?: number | null) => {
    if (transactionId == null) return;
    setDataReady(false);
    clearCommandEditCache(commandEditCacheRef.current);
    prevSelectedCommandIdRef.current = null;
    dispatch(setIsBusy());
    try {
      const data = await appTransactionService.getOneHierarchyTransaction(
        String(transactionId),
        false,
        '',
        '',
        '',
        false,
        ''
      );
      setHierarchy(data);
      buildFieldLookup(data);
      const raw = data?.CommandActionList || [];
      applyCommandListToGrid(raw);
      setIsModified(false);
      const preferred =
        preferredSelectedId != null && raw.some((c: any) => Number(c?.Id) === Number(preferredSelectedId))
          ? Number(preferredSelectedId)
          : null;
      if (preferred != null) {
        setSelectedId(preferred);
      } else if (raw.length > 0) {
        const first = raw[0];
        const fid = first?.Id ?? null;
        setSelectedId(fid != null ? Number(fid) : null);
      } else {
        setSelectedId(null);
      }
      setEditTick((t) => t + 1);
      setDataReady(true);
    } catch (e: any) {
      showError(e?.message || 'Failed to load operation tasks');
      setDataReady(true);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionId, dispatch, showError, buildFieldLookup, applyCommandListToGrid]);

  useEffect(() => {
    loadTransaction(initialSelectedCommandId ?? null);
  }, [loadTransaction, initialSelectedCommandId]);

  // Preload Monaco once so command switches do not leave SqlStatement stuck on Suspense fallback.
  useEffect(() => {
    void import('@monaco-editor/react');
  }, []);

  const selectCommandById = useCallback(
    (nextId: number | null) => {
      const list = (commandCV.sourceCollection as any[]) || [];
      const prevId = selectedId;

      if (prevId != null && prevId !== nextId) {
        const prevItem = list.find((c: any) => Number(c?.Id) === Number(prevId));
        if (prevItem) stashCommand(commandEditCacheRef.current, prevItem);
      }

      if (nextId != null) {
        const nextItem = list.find((c: any) => Number(c?.Id) === Number(nextId));
        if (nextItem) restoreCommand(commandEditCacheRef.current, nextItem);
      }

      prevSelectedCommandIdRef.current = nextId;
      setSelectedId(nextId);
      setEditTick((t) => t + 1);
      commandCV.refresh();
    },
    [commandCV, selectedId]
  );

  useEffect(() => {
    if (!dataReady) return;
    const nextId = selectedId;
    if (prevSelectedCommandIdRef.current === nextId) return;

    const list = (commandCV.sourceCollection as any[]) || [];
    const prevId = prevSelectedCommandIdRef.current;

    if (prevId != null && prevId !== nextId) {
      const prevItem = list.find((c: any) => Number(c?.Id) === Number(prevId));
      if (prevItem) stashCommand(commandEditCacheRef.current, prevItem);
    }

    if (nextId != null) {
      const nextItem = list.find((c: any) => Number(c?.Id) === Number(nextId));
      if (nextItem) restoreCommand(commandEditCacheRef.current, nextItem);
    }

    prevSelectedCommandIdRef.current = nextId;
    setEditTick((t) => t + 1);
    commandCV.refresh();
  }, [selectedId, dataReady, commandCV]);

  useEffect(() => {
    const list = (commandCV.sourceCollection as any[]) || [];
    const isPublic = taskTab === 'public';
    const filtered = list.filter((c: any) => !!c?.ActionAttribute?.LinkToUI === isPublic);
    if (!filtered.length) {
      if (selectedId != null) setSelectedId(null);
      return;
    }
    const stillExists = selectedId != null && filtered.some((c: any) => Number(c.Id) === Number(selectedId));
    if (!stillExists) {
      const fid = filtered[0]?.Id ?? null;
      setSelectedId(fid != null ? Number(fid) : null);
    }
  }, [commandCV, selectedId, taskTab]);

  const markChange = useCallback(() => {
    setIsModified(true);
    setEditTick((t) => t + 1);
    if (hierarchy) {
      hierarchy.isModified = true;
    }
  }, [hierarchy]);

  const itemsForEdit = (commandCV.sourceCollection as any[]) || [];
  const currentEditAction =
    selectedId == null ? null : itemsForEdit.find((c: any) => Number(c.Id) === Number(selectedId)) ?? null;

  const handleSave = useCallback(async () => {
    if (!hierarchy?.Id) return;
    const keepId = selectedId;
    const list = [...((commandCV.sourceCollection as any[]) || [])];

    const runAutoSaveIfNeeded = (): Promise<void> =>
      new Promise((resolve) => {
        const editAction =
          keepId == null ? null : list.find((c: any) => Number(c.Id) === Number(keepId)) ?? null;
        if (editAction?.autoSaveFunc) {
          editAction.autoSaveFunc(() => resolve());
        } else {
          resolve();
        }
      });

    dispatch(setIsBusy());
    try {
      await runAutoSaveIfNeeded();
      const payload = { Id: hierarchy.Id, CommandActionList: list };
      const result = await appTransactionService.saveOneTransactionCommandActionList(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult);
      }
      if (result?.IsSuccessful) {
        setIsModified(false);
        await loadTransaction(keepId);
        if (onRefresh) onRefresh();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save operation tasks');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [hierarchy, selectedId, commandCV, dispatch, loadTransaction, onRefresh, showError, showValidationMessages]);

  const handleAddCommand = useCallback(async () => {
    if (!dataReady) return;
    if (!hierarchy?.Id) {
      showError('Save the data model first before adding operation tasks.');
      return;
    }
    if (isModified) {
      const ok = await showConfirm('You have unsaved changes.\n\nSave changes before adding a new task?', {
        title: 'Unsaved changes',
        confirmLabel: 'Save & Continue',
        cancelLabel: 'Cancel',
      });
      if (!ok) return;
      await handleSave();
    }
    const list = (commandCV.sourceCollection as any[]) || [];
    const maxOrder = Math.max(0, ...list.map((c: any) => Number(c.ActionFlowOrder) || 0));
    const newAction: any = {
      CommandTransactionId: hierarchy.Id,
      ActionGuid: appHelper.guid(),
      ActionFlowOrder: maxOrder + 1,
      Name: 'NewOperationTask' + (maxOrder + 1),
      NextTransactionId: null,
      ActionType: 42,
      NotificationDestinationUserIdtransactionFiledId: null,
      NotificationDestinationRoleIdtransactionFiledId: null,
      DataLoadId: null,
      CommandConditionTransactionFieldId: null,
      ActionAttribute: {
        LinkToUI: taskTab === 'public',
        IsShowOnTopMenu: true,
        IsShowOnSearchViewEventOptionMenu: false,
        IsAutoExecuteOnFormOpen: false,
        ChildActionList: [],
        SqlStatement: '',
        IsLogCommandStartEnd: false,
        IsLogErrorDetails: true,
        IsBatchCommand: false,
        BatchCommandSourceFromType: 1,
        EmAppValidationResultPreference: 1,
      },
    };
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.createOneTransactionCommand(newAction);
      if (!result?.IsSuccessful || !result?.Object) {
        showValidationMessages(result?.ValidationResult);
        return;
      }
      const created = result.Object;
      setSelectedId(Number(created.Id));
      setIsModified(false);
      await loadTransaction(Number(created.Id));
      if (onRefresh) onRefresh();
    } catch (e: any) {
      showError(e?.message || 'Failed to create operation task');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    dataReady,
    hierarchy,
    commandCV,
    dispatch,
    loadTransaction,
    onRefresh,
    showError,
    showValidationMessages,
    taskTab,
    isModified,
    handleSave,
    showConfirm,
  ]);

  const handleDeleteSelected = useCallback(async () => {
    if (selectedId == null) return;
    const item = currentEditAction;
    if (item?.ActionAttribute?.IsWorkflowRootCommand) {
      showError('Cannot delete the workflow root command here. Use Workflow Automation Editor.');
      return;
    }
    const confirmed = await showConfirm('Delete this operation task?', {
      title: 'Delete operation task',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      confirmButtonStyle: 'bg-red-500 hover:bg-red-600 text-white',
    });
    if (!confirmed) return;
    removeCommandFromCache(commandEditCacheRef.current, selectedId);
    const list = ((commandCV.sourceCollection as any[]) || []).filter((c: any) => Number(c.Id) !== Number(selectedId));
    applyCommandListToGrid(list);
    setSelectedId(list.length ? Number(list[0].Id) : null);
    markChange();
    await handleSave();
  }, [selectedId, currentEditAction, commandCV, applyCommandListToGrid, markChange, handleSave, showError, showConfirm]);

  const openWorkflowEditor = useCallback(() => {
    if (transactionId == null || !applicationId) {
      showError('Missing application or transaction context.');
      return;
    }
    const name = transactionName || hierarchy?.TransactionName || 'Workflow Automation Editor';
    addTabAndNavigate(
      'workflow-automation-editor',
      name,
      {
        id: String(applicationId),
        isCreateNewItem: false,
        transactionId,
        transactionType: null,
        dataSourceRegisterId: null,
        isCreateDtoDataModel: false,
        isCreateApiDataModel: false,
        isCreateDataModelView: false,
        modelName: transactionName || hierarchy?.TransactionName || null,
      },
      true
    );
  }, [transactionId, applicationId, transactionName, hierarchy, addTabAndNavigate, showError]);

  const refreshGridCell = useCallback(() => {
    commandCV.refresh();
  }, [commandCV]);

  const handleCommandAdded = useCallback(
    (commandDto: any) => {
      const list = [...((commandCV.sourceCollection as any[]) || []), commandDto];
      if (hierarchy) {
        hierarchy.CommandActionList = list;
      }
      applyCommandListToGrid(list);
      markChange();
    },
    [commandCV, hierarchy, applyCommandListToGrid, markChange],
  );

  if (transactionId == null) {
    return (
      <div className={`p-4 text-sm ${theme.label}`}>Save the data model first, then configure operation tasks.</div>
    );
  }

  if (hierarchy && isWorkflowAutomation) {
    return (
      <div className={`h-full flex flex-col gap-2 p-4 ${theme.mainContentSection}`}>
        <p className={`text-sm ${theme.label}`}>
          This data model is a <span className="font-semibold">workflow automation</span>. Operation tasks use a composition tree and
          dedicated tools (sync tree, root tasks, external commands).
        </p>
        <div className="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            onClick={openWorkflowEditor}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-diagram-project mr-1" aria-hidden />
            Open Workflow Automation Editor
          </button>
          <span className={`text-xs ${theme.label}`}>Use the same editor as the Workflow Automation menu for full parity with Angular.</span>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full flex flex-col overflow-hidden gap-1">
      <div className={`flex items-center justify-between px-3 py-2 shrink-0 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Operation Task</div>
        <div className="flex items-center gap-2 flex-wrap">
          <button
            type="button"
            onClick={() => {
              (async () => {
                if (isModified) {
                  const ok = await showConfirm('You have unsaved changes.\n\nDiscard changes and refresh?', {
                    title: 'Unsaved changes',
                    confirmLabel: 'Discard & Refresh',
                    cancelLabel: 'Cancel',
                  });
                  if (!ok) return;
                }
                loadTransaction(selectedId);
              })();
            }}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden />
            Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            {isModified ? '* ' : ''}
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden />
            Save
          </button>
        </div>
      </div>

      <div
        className={`w-full h-1 flex-auto flex gap-2 min-h-0 items-stretch px-3 py-3 overflow-hidden ${theme.mainContentSection}`}
      >
        <div
          className={`w-[480px] min-w-[280px] h-full min-h-0 flex flex-col border rounded overflow-hidden shrink-0 ${theme.param.border_mainContentSection ? tw.border(theme.param.border_mainContentSection) : ''}`}
        >
          <div
            className={`shrink-0 border-b ${theme.param.border_mainContentSection ? tw.border(theme.param.border_mainContentSection) : ''}`}
          >
            <div className={`text-xs font-semibold px-2 py-1.5 ${theme.title}`}>Commands</div>
            <div className="px-2 pb-2 flex items-center justify-between gap-2 flex-wrap">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setTaskTab('public')}
                  className={`px-2.5 py-1 text-xs rounded-[4px] border ${taskTab === 'public' ? theme.button_default : theme.inputBox}`}
                >
                  Public Task
                </button>
                <button
                  type="button"
                  onClick={() => setTaskTab('private')}
                  className={`px-2.5 py-1 text-xs rounded-[4px] border ${taskTab === 'private' ? theme.button_default : theme.inputBox}`}
                >
                  Private Task
                </button>
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={handleAddCommand}
                  className={`px-2.5 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-plus mr-1" aria-hidden />
                  Add Task
                </button>
                <button
                  type="button"
                  onClick={handleDeleteSelected}
                  disabled={selectedId == null}
                  className={`px-2.5 py-1 text-xs rounded-[4px] ${theme.button_default} disabled:opacity-50`}
                >
                  <i className="fa-solid fa-trash mr-1" aria-hidden />
                  Delete
                </button>
              </div>
            </div>
          </div>

          <div className="min-h-0 flex-auto overflow-auto">
            {(() => {
              const all = (commandCV.sourceCollection as any[]) || [];
              const isPublic = taskTab === 'public';
              const filtered = all.filter((c: any) => !!c?.ActionAttribute?.LinkToUI === isPublic);
              if (!filtered.length) {
                return <div className={`p-3 text-xs ${theme.label}`}>No tasks.</div>;
              }
              const itemBorderCls = theme.param.border_mainContentSection ? tw.border(theme.param.border_mainContentSection) : '';
              const hoverBgCls = theme.param.bg_default_hover ? tw.hover.bg(theme.param.bg_default_hover) : '';
              const selectedBgCls = theme.param.wijmo_grid_selected_row_background_color
                ? tw.bg(theme.param.wijmo_grid_selected_row_background_color)
                : '';
              return (
                <div className="flex flex-col">
                  {filtered.map((item: any) => {
                    const id = item?.Id != null ? String(item.Id) : '';
                    const name = item?.Name ?? '';
                    const display = id && name ? `${id} ${name}` : id || name;
                    const selected = selectedId != null && Number(item?.Id) === Number(selectedId);
                    const operationTypeDisplay = getTransactionCommandActionTypeDisplay(item?.ActionType);
                    return (
                      <button
                        key={String(item?.Id ?? display)}
                        type="button"
                        onClick={() => {
                          const nid = item?.Id;
                          if (nid != null) selectCommandById(Number(nid));
                        }}
                        className={[
                          'w-full text-left px-3 py-2.5 border-b focus:outline-none',
                          itemBorderCls,
                          hoverBgCls,
                          selected ? selectedBgCls : '',
                        ]
                          .filter(Boolean)
                          .join(' ')}
                      >
                        <div className="flex flex-col gap-0.5">
                          <div className="flex items-center justify-between gap-2">
                            <div className={`text-xs font-semibold ${theme.title} truncate`} title={display}>
                              {display}
                            </div>
                          </div>
                          <div className="flex items-center justify-between gap-2">
                            <div className={`text-[10px] ${theme.label} opacity-50`}>
                              Order: {item?.ActionFlowOrder ?? ''}
                            </div>
                            <div className={`text-[10px] ${theme.label} opacity-50 shrink-0`} title={operationTypeDisplay}>
                              {operationTypeDisplay}
                            </div>
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              );
            })()}
          </div>
        </div>

        <CommandEditor
          key={selectedId != null ? `cmd-${selectedId}` : 'cmd-none'}
          transactionId={transactionId}
          applicationId={applicationId}
          hierarchy={hierarchy}
          currentEditAction={currentEditAction}
          operationTypeOptions={operationTypeOptions}
          rootLevelTransFieldLookUpList={rootLevelTransFieldLookUpList}
          rootLevelConditionTransFieldLookUpList={rootLevelConditionTransFieldLookUpList}
          rootLevelSwitchConditionTransFieldLookUpList={rootLevelSwitchConditionTransFieldLookUpList}
          transactionFieldLookUpList={transactionFieldLookUpList}
          rootLevelAllFieldLookUpList={rootLevelAllFieldLookUpList}
          onMarkChange={markChange}
          onRefreshGridCell={refreshGridCell}
          onFlowOrderChanged={() => applyCommandListToGrid(commandCV.sourceCollection as any[])}
          onSelectChildCommand={(commandId) => selectCommandById(commandId)}
          onCommandAdded={handleCommandAdded}
        />
      </div>
    </div>
  );
};

export default TransactionCommandActionEditor;
