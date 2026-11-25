/**
 * Data Model Operation (Transaction Command) — Application Form Builder section.
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
import { CommandHelpModal, type CommandHelpSectionId } from './CommandHelpModal';
import { buildCommandHelpAskPrompt } from './commandHelpPrompts';
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
import { filterCommandActionTypeOptionsForPicker } from '../transactionCommandTypeTaxonomy';
import { buildCommandFieldLookups } from './commandFieldLookup';
import type { TransactionEditorConfigBridge } from './TransactionGraphicEditor';
import { tw } from '../../../helper/themeHelper';
import appHelper from '../../../helper/appHelper';

const EmAppTransactionScopeUsageWorkflowAutomation = 2;
const EmAppTransactionCommandTypePrintFromMessageTemplate = 74;
const EmAppTransactionCommandTypeOpenLinkedSearch = 53;
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
  /** Same bridge as Data Model Design — Configurator request_form_reload / dirty gate. */
  onConfigBridgeChange?: (bridge: TransactionEditorConfigBridge | null) => void;
}

const TransactionCommandActionEditor: React.FC<TransactionCommandActionEditorProps> = ({
  transactionId,
  applicationId,
  transactionName,
  transactionType = null,
  initialSelectedCommandId = null,
  onRefresh,
  onConfigBridgeChange,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const { showConfirm, showAlert } = useAlertConfirm();
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
  const [helpOpen, setHelpOpen] = useState(false);
  const [helpSection, setHelpSection] = useState<CommandHelpSectionId>('');
  /** Left operation-list width (px); drag the divider between list and editor. */
  const LIST_WIDTH_DEFAULT = 360;
  const LIST_WIDTH_MIN = 220;
  const LIST_WIDTH_MAX = 640;
  const [listWidthPx, setListWidthPx] = useState(LIST_WIDTH_DEFAULT);
  const splitContainerRef = useRef<HTMLDivElement | null>(null);
  const listResizeDragRef = useRef(false);
  const [helpAskPrompt, setHelpAskPrompt] = useState(() => buildCommandHelpAskPrompt(null));

  /** Per-command unsaved edits while switching list selection; cleared on Refresh / Save+reload. */
  const commandEditCacheRef = useRef<Map<number, any>>(new Map());
  const prevSelectedCommandIdRef = useRef<number | null>(null);

  const isWorkflowAutomation = useMemo(() => {
    const bid = hierarchy?.BusinessScopeId ?? hierarchy?.businessScopeId;
    return Number(bid) === EmAppTransactionScopeUsageWorkflowAutomation || hierarchy?.IsWorkflowAutomation === true;
  }, [hierarchy]);

  const operationTypeOptions = useMemo(
    () =>
      filterCommandActionTypeOptionsForPicker(
        getApplicationFormBuilderCommandActionTypeOptions(
          transactionType ?? hierarchy?.TransactionOrganizedType ?? null
        )
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
      showError(e?.message || 'Failed to load operations');
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

  const handleSave = useCallback(async (): Promise<boolean> => {
    if (!hierarchy?.Id) return false;
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
        return true;
      }
      return false;
    } catch (e: any) {
      showError(e?.message || 'Failed to save operations');
      return false;
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [hierarchy, selectedId, commandCV, dispatch, loadTransaction, onRefresh, showError, showValidationMessages]);

  // Publish Save / Reload for Configurator (same contract as Data Model Design Refresh).
  const handleSaveRef = useRef(handleSave);
  handleSaveRef.current = handleSave;
  const loadTransactionRef = useRef(loadTransaction);
  loadTransactionRef.current = loadTransaction;
  const selectedIdRef = useRef(selectedId);
  selectedIdRef.current = selectedId;
  useEffect(() => {
    if (!onConfigBridgeChange) return;
    onConfigBridgeChange({
      isDirty: isModified,
      save: async () => handleSaveRef.current(),
      reloadFromDb: async () => {
        await loadTransactionRef.current(selectedIdRef.current);
      },
    });
    return () => {
      onConfigBridgeChange(null);
    };
  }, [isModified, onConfigBridgeChange]);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!listResizeDragRef.current || !splitContainerRef.current) return;
      const rect = splitContainerRef.current.getBoundingClientRect();
      const next = e.clientX - rect.left;
      const maxLeft = Math.max(LIST_WIDTH_MIN, rect.width - 320);
      setListWidthPx(Math.min(Math.max(next, LIST_WIDTH_MIN), Math.min(LIST_WIDTH_MAX, maxLeft)));
    };
    const onUp = () => {
      if (!listResizeDragRef.current) return;
      listResizeDragRef.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, []);

  const handleAddCommand = useCallback(async () => {
    if (!dataReady) return;
    if (!hierarchy?.Id) {
      showError('Save the data model first before adding operations.');
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
      Name: 'NewOperation' + (maxOrder + 1),
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
      showError(e?.message || 'Failed to create operation');
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
      showError('Cannot delete the workflow root operation here. Use Workflow Automation Editor.');
      return;
    }
    const confirmed = await showConfirm('Delete this operation?', {
      title: 'Delete operation',
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
      <div className={`p-4 text-sm ${theme.label}`}>Save the data model first, then configure operations.</div>
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
        <div className="flex items-center gap-2">
          <div className={`text-md font-semibold ${theme.title}`}>Data Model Operation</div>
          <button
            type="button"
            onClick={() => {
              setHelpAskPrompt(buildCommandHelpAskPrompt(currentEditAction?.ActionType));
              setHelpSection('');
              setHelpOpen(true);
            }}
            className={`w-7 h-7 inline-flex items-center justify-center rounded-full text-sm ${theme.button_default}`}
            title="Data Model Operation help"
            aria-label="Data Model Operation help"
          >
            <i className="fa-solid fa-circle-question" aria-hidden />
          </button>
        </div>
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
        ref={splitContainerRef}
        className={`w-full h-1 flex-auto flex gap-0 min-h-0 items-stretch px-3 py-3 overflow-hidden ${theme.mainContentSection}`}
      >
        <div
          className={`h-full min-h-0 flex flex-col border border-r-0 rounded-l overflow-hidden shrink-0 ${theme.param.border_mainContentSection ? tw.border(theme.param.border_mainContentSection) : ''}`}
          style={{ width: listWidthPx, minWidth: LIST_WIDTH_MIN }}
        >
          <div
            className={`shrink-0 border-b ${theme.param.border_mainContentSection ? tw.border(theme.param.border_mainContentSection) : ''}`}
          >
            <div className={`text-xs font-semibold px-2 py-1.5 ${theme.title}`}>Data Model Operation</div>
            <div className="px-2 pb-2 flex items-center justify-between gap-2 flex-wrap">
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setTaskTab('public')}
                  className={`px-2.5 py-1 text-xs rounded-[4px] border ${
                    taskTab === 'public'
                      ? `${theme.tab_active} font-semibold`
                      : `${theme.label} opacity-70 hover:opacity-100`
                  }`}
                  aria-pressed={taskTab === 'public'}
                >
                  Public Operation
                </button>
                <button
                  type="button"
                  onClick={() => setTaskTab('private')}
                  className={`px-2.5 py-1 text-xs rounded-[4px] border ${
                    taskTab === 'private'
                      ? `${theme.tab_active} font-semibold`
                      : `${theme.label} opacity-70 hover:opacity-100`
                  }`}
                  aria-pressed={taskTab === 'private'}
                >
                  Private Operation
                </button>
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={handleAddCommand}
                  className={`px-2.5 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-plus mr-1" aria-hidden />
                  Add Operation
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
                return <div className={`p-3 text-xs ${theme.label}`}>No operations.</div>;
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

        <div
          className={`w-1.5 shrink-0 cursor-col-resize self-stretch min-h-0 border-x ${theme.inputBox} hover:opacity-90`}
          role="separator"
          aria-orientation="vertical"
          aria-label="Resize operation list"
          title="Drag to resize"
          onMouseDown={(e) => {
            e.preventDefault();
            listResizeDragRef.current = true;
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
          }}
        />

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
          listReloadToken={editTick}
          panelClassName="border-l-0 rounded-l-none rounded-r"
          onOpenTypeHelp={() => {
            setHelpAskPrompt(buildCommandHelpAskPrompt(currentEditAction?.ActionType));
            setHelpSection(
              getTransactionCommandActionTypeDisplay(currentEditAction?.ActionType) ||
                'Configure by Operation Type'
            );
            setHelpOpen(true);
          }}
        />
      </div>

      <CommandHelpModal
        open={helpOpen}
        initialSection={helpSection}
        onClose={() => setHelpOpen(false)}
        askPrompt={helpAskPrompt}
        onAskConfigurator={async (prompt) => {
          try {
            await navigator.clipboard.writeText(prompt);
            await showAlert(
              'Prefab prompt copied to the clipboard.\n\nPaste it into the Configurator chat (Form design → Configurator panel).',
              { title: 'Ask Configurator' }
            );
          } catch {
            await showAlert(prompt, { title: 'Ask Configurator — copy this prompt' });
          }
        }}
      />
    </div>
  );
};

export default TransactionCommandActionEditor;
