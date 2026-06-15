/**
 * Workflow Automation Editor – migrated from AngularJS workflowAutomationEditorCtrl + WorkflowAutomationEditor.cshtml.
 * Route: workflow-automation-editor/:param (param = JSON with id, transactionId, isCreateNewItem, modelName, etc.)
 * Header: Workflow Setting Name, Description, Refresh, Save, Save As New Workflow, Add To App Menu, Windows Scheduler, Config Parameters, Close.
 * Left: Operation Tasks (tree grid). Right: Current Edit Operation Task (form for selected task).
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useParams, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { CellRange, CellType } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import type { RootState } from '../../redux/store';
import { closeTab, updateActiveTabPath, updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { setUserMenu } from '../../redux/features/admin/userSessionSlice';
import { buildRoutePathFromParamObj, getReactPathForRouteCode } from '../../helper/navigationHelper';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { searchSvc } from '../../webapi/searchSvc';
import WorkflowAutomationAgentPanel from './WorkflowAutomationAgentPanel';
import WorkflowWindowsSchedulerTaskPopup, {
  type WorkflowSchedulerTaskDto,
} from './WorkflowWindowsSchedulerTaskPopup';
import { CommandEditor } from '../transaction/ApplicationFormBuilder/CommandEditor';
import {
  clearCommandEditCache,
  copyCommandInPlace,
  stashCommand,
  type CommandEditCache,
} from '../transaction/ApplicationFormBuilder/commandEditCache';
import { buildCommandFieldLookups, type CommandFieldLookupResult } from '../transaction/ApplicationFormBuilder/commandFieldLookup';
import { getWorkflowEmbeddedCommandActionTypeOptions } from '../transaction/transactionCommandActionTypes';
import TableColumnSelectorDialog from '../transaction/ApplicationFormBuilder/TableColumnSelectorDialog';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';

type RouteParams = { param?: string };

export type WorkflowAutomationEditorEmbeddedProps = {
  transactionId: number;
  saasApplicationId?: string | number | null;
  modelName?: string | null;
  onClose?: () => void;
};

export type WorkflowAutomationEditorProps = {
  embedded?: WorkflowAutomationEditorEmbeddedProps;
};

/**
 * QuickGenerate uses existing search.SaasApplicationId when navigation already exists.
 * Re-align search + dataset to the selected application root menu id before generating menu.
 */
async function syncWorkflowNavigationToTargetApplication(
  transactionId: number,
  targetApplicationId: number,
): Promise<void> {
  let nav: any;
  try {
    nav = await appTransactionService.retrieveTransactionDefaultNavigationDto(transactionId, false);
  } catch {
    return;
  }
  const searchDto = nav?.DefaultSearchDto;
  const searchId = searchDto?.Id ?? nav?.QuickSearchId;
  if (!searchId) return;

  let searchEx: any;
  try {
    searchEx = await searchSvc.retrieveOneAppSearchExDto(String(searchId));
  } catch {
    return;
  }
  if (!searchEx) return;

  const currentAppId =
    searchEx.SaasApplicationId != null
      ? Number(searchEx.SaasApplicationId)
      : searchDto?.SaasApplicationId != null
        ? Number(searchDto.SaasApplicationId)
        : null;
  if (currentAppId === targetApplicationId) return;

  if (searchEx.DataSetId) {
    try {
      const ds = await searchSvc.retrieveOneAppDataSetExDto(String(searchEx.DataSetId), false);
      if (ds && Number(ds.SaasApplicationId) !== targetApplicationId) {
        ds.SaasApplicationId = targetApplicationId;
        ds.IsModified = true;
        await searchSvc.saveOneAppDataSetEntityDto({
          ...ds,
          DeletedItemsIds: ds.DeletedItemsIds || [],
        });
      }
    } catch {
      /* non-blocking */
    }
  }

  await searchSvc.saveAppSearchExDto({
    ...searchEx,
    SaasApplicationId: targetApplicationId,
    AppSearchFieldList: searchEx.AppSearchFieldList || [],
    DeletedItemsIds: searchEx.DeletedItemsIds || [],
    IsModified: true,
  });
}

function buildParsedFromEmbedded(embedded: WorkflowAutomationEditorEmbeddedProps) {
  return {
    id:
      embedded.saasApplicationId != null && String(embedded.saasApplicationId).trim() !== ''
        ? String(embedded.saasApplicationId)
        : null,
    transactionId: embedded.transactionId,
    isCreateNewItem: false,
    transactionType: null as number | null,
    dataSourceRegisterId: null as number | null,
    modelName: embedded.modelName ?? null,
  };
}

/** transactionId from management / tab route (existing workflow always passes this). */
function routeWorkflowTransactionId(parsed: { transactionId?: number | string | null } | null): number | null {
  if (!parsed || parsed.transactionId == null) return null;
  const n = Number(parsed.transactionId);
  return Number.isNaN(n) || n <= 0 ? null : n;
}

/** Angular prepareData: command with IsWorkflowRootCommand, else composition command, else first command. */
function findWorkflowRootCommand(commandList: any[], compositionActionType: number): any | null {
  if (!Array.isArray(commandList) || commandList.length === 0) return null;
  for (const c of commandList) {
    if (c?.ActionAttribute?.IsWorkflowRootCommand) return c;
  }
  for (const c of commandList) {
    if (Number(c?.ActionType) === compositionActionType) return c;
  }
  return commandList[0] ?? null;
}

/** Prefer root from CommandActionList — rootCommand state can be stale after auto-save / prepareData. */
function resolveWorkflowParentRoot(
  transaction: any,
  parentHint: any | null | undefined,
  compositionActionType: number,
): any | null {
  const list = transaction?.CommandActionList || [];
  if (!list.length) return parentHint ?? null;
  if (parentHint?.Id != null) {
    const n = Number(parentHint.Id);
    if (!Number.isNaN(n) && n > 0) {
      const byId = list.find((c: any) => Number(c?.Id) === n);
      if (byId) return byId;
    }
  }
  const fromList = findWorkflowRootCommand(list, compositionActionType);
  if (fromList) return fromList;
  if (
    parentHint &&
    list.some((c: any) => c === parentHint || (parentHint.Id != null && Number(c?.Id) === Number(parentHint.Id)))
  ) {
    return parentHint;
  }
  return parentHint ?? null;
}

function ensureWorkflowRootCommandShape(root: any): any {
  if (!root) return root;
  root.ActionAttribute = root.ActionAttribute || {};
  if (root.ActionAttribute.ChildActionList == null) {
    root.ActionAttribute.ChildActionList = [];
  }
  root.ActionAttribute.IsWorkflowRootCommand = true;
  return root;
}

/** Angular addRootAction: new commands have no Id (null), never Id=0 — 0 is treated as UPDATE and root never persists. */
function normalizeWorkflowCommandIdForSave(commandId: unknown): number | null {
  if (commandId == null || commandId === '') return null;
  const n = Number(commandId);
  if (Number.isNaN(n) || n <= 0) return null;
  return n;
}

/**
 * Prepare CommandActionList for SaveOneWorkflowAutomation / SaveOneTransactionCommandActionList (Angular parity).
 * - Root composition command: IsWorkflowRootCommand, ChildActionList, Name = transaction name, ActionType = composition.
 * - New commands: Id null (not 0).
 */
function prepareWorkflowCommandActionListForSave(
  commandList: any[] | undefined,
  transactionName: string,
  compositionActionType: number,
): any[] {
  const list = (commandList || []).map((cmd) => {
    const normalizedId = normalizeWorkflowCommandIdForSave(cmd?.Id);
    const attr = cmd?.ActionAttribute
      ? {
          ...cmd.ActionAttribute,
          ChildActionList: [...(cmd.ActionAttribute.ChildActionList || [])],
        }
      : { ChildActionList: [] as any[] };
    return {
      ...cmd,
      Id: normalizedId,
      ActionAttribute: attr,
    };
  });

  let rootIndex = list.findIndex((c) => c?.ActionAttribute?.IsWorkflowRootCommand);
  if (rootIndex < 0) {
    rootIndex = list.findIndex((c) => Number(c?.ActionType) === compositionActionType);
  }

  // Repair workflows saved from React without a persisted composition root (Angular requires one).
  if (rootIndex < 0 && list.length > 0) {
    const childRefs: any[] = [];
    const existingCommands: any[] = [];
    for (const cmd of list) {
      const cmdId = normalizeWorkflowCommandIdForSave(cmd?.Id);
      if (cmdId != null) {
        existingCommands.push(cmd);
        childRefs.push({
          Sort: childRefs.length + 1,
          CommandId: cmdId,
          CommandDisplay: cmd.Display ?? cmd.Name ?? '',
        });
      }
    }
    if (childRefs.length > 0) {
      const compositionRoot = ensureWorkflowRootCommandShape({
        Name: (transactionName || 'RootCommand').trim(),
        ActionFlowOrder: 1,
        NextTransactionId: null,
        ActionType: compositionActionType,
        ActionAttribute: {
          LinkToUI: true,
          IsShowOnTopMenu: true,
          ChildActionList: childRefs,
        },
      });
      return prepareWorkflowCommandActionListForSave(
        [compositionRoot, ...existingCommands],
        transactionName,
        compositionActionType,
      );
    }
  }

  if (rootIndex >= 0) {
    const root = ensureWorkflowRootCommandShape(list[rootIndex]);
    root.Name = (transactionName || root.Name || 'RootCommand').trim();
    root.ActionType = compositionActionType;
    list[rootIndex] = root;
    list.forEach((cmd, i) => {
      if (i !== rootIndex && cmd?.ActionAttribute) {
        cmd.ActionAttribute.IsWorkflowRootCommand = false;
      }
    });
  }

  return list;
}

const EXECUTE_SQL_COMMAND_ACTION_TYPE = 42;

/** SqlStatementSection reads NotificationMessage (DB column); hydrate legacy ActionAttribute.SqlStatement in workflow only. */
function ensureWorkflowCommandSqlNotificationMessage(
  cmd: any,
  executeSqlActionType = EXECUTE_SQL_COMMAND_ACTION_TYPE,
): void {
  if (!cmd || Number(cmd.ActionType) !== executeSqlActionType) return;
  const msg = cmd.NotificationMessage;
  const attrSql = cmd.ActionAttribute?.SqlStatement;
  if ((!msg || !String(msg).trim()) && attrSql && String(attrSql).trim()) {
    cmd.NotificationMessage = String(attrSql);
  }
}

function hydrateWorkflowCommandActionList(commandList: any[] | undefined, executeSqlActionType: number): void {
  for (const cmd of commandList || []) {
    ensureWorkflowCommandSqlNotificationMessage(cmd, executeSqlActionType);
  }
}

function pickNonEmptyCommandText(primary: unknown, fallback: unknown): unknown {
  const a = primary != null ? String(primary).trim() : '';
  const b = fallback != null ? String(fallback).trim() : '';
  if (a) return primary;
  if (b) return fallback;
  return primary ?? fallback;
}

/** Copy in-flight editor edits onto CommandActionList before Save (Monaco debounce may not have flushed). */
function flushCommandDraftToTransactionList(transaction: any, draft: any, commandId: number): void {
  if (!transaction || !draft || !commandId) return;
  const canonical = (transaction.CommandActionList || []).find(
    (c: any) => Number(c?.Id) === Number(commandId),
  );
  if (canonical && canonical !== draft) {
    copyCommandInPlace(canonical, draft);
  }
}

/**
 * Before workflow Save: apply every stashed task draft + current editor to CommandActionList.
 * Only flushing the active task drops SQL on other tasks that were edited then deselected.
 */
function flushAllWorkflowCommandEditsToTransactionList(
  transaction: any,
  cache: CommandEditCache,
  currentDraft: any | null,
  executeSqlActionType: number,
): void {
  if (!transaction?.CommandActionList) return;
  const currentId =
    currentDraft?.Id != null && !Number.isNaN(Number(currentDraft.Id)) && Number(currentDraft.Id) > 0
      ? Number(currentDraft.Id)
      : null;

  for (const canonical of transaction.CommandActionList) {
    const id = canonical?.Id != null ? Number(canonical.Id) : NaN;
    if (!id || id <= 0) continue;

    if (currentId === id && currentDraft) {
      flushCommandDraftToTransactionList(transaction, currentDraft, id);
    } else {
      const cached = cache.get(id);
      if (cached) {
        copyCommandInPlace(canonical, cached);
      }
    }
    ensureWorkflowCommandSqlNotificationMessage(canonical, executeSqlActionType);
  }
}

/** Restore cached draft without overwriting newer SQL already on the list row. */
function restoreWorkflowCommandFromCache(cache: CommandEditCache, cmd: any): boolean {
  if (cmd?.Id == null) return false;
  const cached = cache.get(Number(cmd.Id));
  if (!cached) return false;
  const listNotificationMessage = cmd.NotificationMessage;
  copyCommandInPlace(cmd, cached);
  cmd.NotificationMessage = pickNonEmptyCommandText(listNotificationMessage, cached.NotificationMessage);
  return true;
}

/** API may return a bare array or a transaction object; tree lives on WorkflowCommandNodeTree. */
function extractWorkflowCommandNodeTree(synced: unknown): any[] {
  if (Array.isArray(synced)) return synced;
  if (synced && typeof synced === 'object') {
    const obj = synced as Record<string, unknown>;
    if (Array.isArray(obj.WorkflowCommandNodeTree)) return obj.WorkflowCommandNodeTree as any[];
    if (Array.isArray(obj.workflowCommandNodeTree)) return obj.workflowCommandNodeTree as any[];
  }
  return [];
}

/**
 * Save response CommandActionList can omit newly created child commands; merge so tree sync can resolve CommandId.
 */
function mergeWorkflowCommandActionLists(clientList: any[] | undefined, serverList: any[] | undefined): any[] {
  const byId = new Map<number, any>();
  const addOrMerge = (cmd: any) => {
    if (!cmd || cmd.Id == null) return;
    const id = Number(cmd.Id);
    if (!id) return;
    const existing = byId.get(id);
    if (!existing) {
      byId.set(id, cmd);
      return;
    }
    const clientAttr = cmd.ActionAttribute || {};
    const serverAttr = existing.ActionAttribute || {};
    byId.set(id, {
      ...existing,
      ...cmd,
      NotificationMessage: pickNonEmptyCommandText(cmd.NotificationMessage, existing.NotificationMessage),
      ActionAttribute: {
        ...serverAttr,
        ...clientAttr,
        SqlStatement: pickNonEmptyCommandText(clientAttr.SqlStatement, serverAttr.SqlStatement),
        ChildActionList:
          serverAttr.ChildActionList?.length > 0
            ? serverAttr.ChildActionList
            : clientAttr.ChildActionList || [],
        IsWorkflowRootCommand:
          !!serverAttr.IsWorkflowRootCommand || !!clientAttr.IsWorkflowRootCommand,
      },
    });
  };
  (serverList || []).forEach(addOrMerge);
  (clientList || []).forEach(addOrMerge);
  return Array.from(byId.values());
}

/** Payload for SyncronizeWorkflowCommandNodeTreeFromActionList (requires BusinessScopeId + full command list). */
function buildTransactionForWorkflowTreeSync(
  savedObject: any,
  savedSource: any,
  workflowId: number | null,
  compositionActionType: number,
  workflowBusinessScopeId: number,
): any {
  const commandList = mergeWorkflowCommandActionLists(
    savedSource?.CommandActionList,
    savedObject?.CommandActionList,
  );
  const merged = {
    ...(savedObject || {}),
    ...(workflowId != null ? { Id: workflowId } : {}),
    CommandActionList:
      commandList.length > 0 ? commandList : savedObject?.CommandActionList || savedSource?.CommandActionList || [],
    TransactionName: savedObject?.TransactionName ?? savedSource?.TransactionName,
    BusinessScopeId: Number(
      savedObject?.BusinessScopeId ?? savedSource?.BusinessScopeId ?? workflowBusinessScopeId,
    ),
  };
  const root = findWorkflowRootCommand(merged.CommandActionList || [], compositionActionType);
  if (root) {
    ensureWorkflowRootCommandShape(root);
  }
  return merged;
}

/**
 * Client-side mirror of AppTransactionCommandBL.PrepareOneWorkflowCommandTreeNode for in-workflow children.
 * Used when server sync returns empty (e.g. wrong BusinessScopeId in DB) but ChildActionList is present.
 */
function buildWorkflowCommandNodeTreeClientSide(
  commandList: any[],
  parentCommand: any,
  compositionActionType: number,
  parentLogicIdPrefix = '',
): any[] {
  if (!parentCommand?.ActionAttribute?.ChildActionList?.length) return [];
  const treeNodes: any[] = [];
  const children = [...parentCommand.ActionAttribute.ChildActionList]
    .filter((c: any) => c?.CommandId != null)
    .sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));
  let childCount = 0;
  for (const childRef of children) {
    if (childRef.ExternalTransactionId != null) continue;
    const childCmd = commandList.find((o) => Number(o.Id) === Number(childRef.CommandId));
    if (!childCmd) continue;
    childCount++;
    const treeNodeLogicId = `${parentLogicIdPrefix}|${childCount}_${childCmd.Id}`;
    const node: any = {
      ...childCmd,
      DisplayName: childCmd.DisplayName ?? childCmd.Name ?? '',
      ActionFlowOrder: childRef.Sort,
      ParentTreeNodeCommandId: Number(parentCommand.Id) || null,
      TreeNodeLogicId: treeNodeLogicId,
      ProgressStatus: childCmd.ProgressStatus ?? '',
      ErrorMessage: childCmd.ErrorMessage ?? '',
      WorkflowChildTreeNodes: [],
    };
    if (
      Number(childCmd.ActionType) === compositionActionType &&
      childCmd.ActionAttribute?.ChildActionList?.length
    ) {
      node.WorkflowChildTreeNodes = buildWorkflowCommandNodeTreeClientSide(
        commandList,
        childCmd,
        compositionActionType,
        treeNodeLogicId,
      );
    }
    treeNodes.push(node);
  }
  return treeNodes;
}

function resolveWorkflowCommandNodeTree(
  transactionData: any,
  compositionActionType: number,
): any[] {
  const fromServer = extractWorkflowCommandNodeTree(transactionData?.WorkflowCommandNodeTree);
  if (fromServer.length > 0) return fromServer;
  const commandList = transactionData?.CommandActionList || [];
  const root = findWorkflowRootCommand(commandList, compositionActionType);
  if (!root) return [];
  ensureWorkflowRootCommandShape(root);
  return buildWorkflowCommandNodeTreeClientSide(commandList, root, compositionActionType);
}

function findWorkflowTreeNodeByCommandId(nodes: any[] | undefined, commandId: number): any | null {
  if (!nodes?.length) return null;
  for (const node of nodes) {
    if (Number(node?.Id) === commandId || Number(node?.CommandId) === commandId) return node;
    const children = node.WorkflowChildTreeNodes ?? node.Children ?? node.children ?? [];
    const found = findWorkflowTreeNodeByCommandId(children, commandId);
    if (found) return found;
  }
  return null;
}

/** Angular createNewChildAction: push command, link under root ChildActionList, then save(). */
function applyNewCommandToWorkflowTransaction(
  transaction: any,
  parentRoot: any,
  commandExDto: any,
): { transaction: any; root: any; dictChild: Record<string, any> } {
  const commandList = [...(transaction.CommandActionList || [])];
  if (!commandList.some((c) => Number(c?.Id) === Number(commandExDto?.Id))) {
    commandList.push(commandExDto);
  }
  const rootIndex = commandList.findIndex(
    (c) =>
      c === parentRoot ||
      (parentRoot?.Id != null && parentRoot.Id !== 0 && Number(c?.Id) === Number(parentRoot.Id)),
  );
  let root = rootIndex >= 0 ? { ...commandList[rootIndex] } : { ...parentRoot };
  root = ensureWorkflowRootCommandShape(root);
  const childList = [...(root.ActionAttribute?.ChildActionList || [])];
  const maxSort = Math.max(0, ...childList.map((c: any) => c.Sort || 0));
  const childDto = {
    Sort: maxSort + 1,
    CommandId: commandExDto?.Id ?? null,
    CommandDisplay: commandExDto?.Display ?? commandExDto?.Name ?? '',
  };
  childList.push(childDto);
  root = {
    ...root,
    ActionAttribute: {
      ...root.ActionAttribute,
      ChildActionList: childList,
      IsWorkflowRootCommand: true,
    },
  };
  if (rootIndex >= 0) {
    commandList[rootIndex] = root;
  } else {
    commandList.push(root);
  }
  const dictChild: Record<string, any> = {};
  childList.forEach((child: any, idx: number) => {
    dictChild['|' + (idx + 1) + '_' + child.CommandId] = child;
  });
  return {
    transaction: {
      ...transaction,
      CommandActionList: commandList,
      isModified: true,
      IsModified: true,
    },
    root,
    dictChild,
  };
}

type SaveWorkflowOptions = {
  /** After save + tree sync, select the new/edited command in the operation task tree (Angular activeCommandTreeNode_Id). */
  reselectCommandId?: number;
  /** Auto-save before add-task: do not show validation toasts (avoids double "Saved Successfully"). */
  silent?: boolean;
  /** Do not clear the command editor when save succeeds without reselectCommandId. */
  keepEditorSelection?: boolean;
};

type CommandTreeContextMenuState = {
  visible: boolean;
  x: number;
  y: number;
  item: any | null;
};

// Floating, draggable, resizable AI chat panel
// Default: top-right anchored to editor content boundary; drag header to move;
// drag left edge to resize width; drag bottom edge to resize height
const WorkflowAIFloatingPanel: React.FC<{
  transactionId:     number;
  onWorkflowChanged: () => void;
  onClose:           () => void;
  initialTop:        number;   // viewport-relative Y where editor content area starts
}> = ({ transactionId, onWorkflowChanged, onClose, initialTop }) => {
  const { theme } = useTheme();
  const panelRef = React.useRef<HTMLDivElement>(null);

  const defaultH = Math.max(300, window.innerHeight - initialTop - 16);
  const [pos,  setPos]  = React.useState<{ right: number; top: number }>({ right: 16, top: initialTop });
  const [size, setSize] = React.useState<{ width: number; height: number }>({ width: 480, height: defaultH });

  // ── Drag header to move ────────────────────────────────────────────────────
  const dragRef = React.useRef<{ sx: number; sy: number; sr: number; st: number } | null>(null);
  const onHeaderMouseDown = (e: React.MouseEvent) => {
    if ((e.target as HTMLElement).closest('button')) return;
    e.preventDefault();
    dragRef.current = { sx: e.clientX, sy: e.clientY, sr: pos.right, st: pos.top };
    const onMove = (ev: MouseEvent) => {
      if (!dragRef.current) return;
      const dx = ev.clientX - dragRef.current.sx;
      const dy = ev.clientY - dragRef.current.sy;
      setPos({
        right: Math.max(0, dragRef.current.sr - dx),
        top:   Math.max(0, dragRef.current.st + dy),
      });
    };
    const onUp = () => { dragRef.current = null; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  };

  // ── Drag left edge to resize width ────────────────────────────────────────
  const leftResizeRef = React.useRef<{ sx: number; sw: number } | null>(null);
  const onLeftEdgeMouseDown = (e: React.MouseEvent) => {
    e.preventDefault(); e.stopPropagation();
    leftResizeRef.current = { sx: e.clientX, sw: size.width };
    const onMove = (ev: MouseEvent) => {
      if (!leftResizeRef.current) return;
      const dx = ev.clientX - leftResizeRef.current.sx;
      setSize(s => ({ ...s, width: Math.max(340, leftResizeRef.current!.sw - dx) }));
    };
    const onUp = () => { leftResizeRef.current = null; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  };

  // ── Drag bottom edge to resize height ─────────────────────────────────────
  const btmResizeRef = React.useRef<{ sy: number; sh: number } | null>(null);
  const onBottomEdgeMouseDown = (e: React.MouseEvent) => {
    e.preventDefault(); e.stopPropagation();
    btmResizeRef.current = { sy: e.clientY, sh: size.height };
    const onMove = (ev: MouseEvent) => {
      if (!btmResizeRef.current) return;
      const dy = ev.clientY - btmResizeRef.current.sy;
      setSize(s => ({ ...s, height: Math.max(300, btmResizeRef.current!.sh + dy) }));
    };
    const onUp = () => { btmResizeRef.current = null; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
  };

  return (
    <div
      ref={panelRef}
      className={`fixed z-50 rounded-lg shadow-2xl flex flex-col overflow-hidden ${theme.mainContentSection}`}
      style={{ right: pos.right, top: pos.top, width: size.width, height: size.height, border: '1px solid rgba(0,0,0,0.12)' }}
    >
      {/* Left-edge resize handle */}
      <div
        onMouseDown={onLeftEdgeMouseDown}
        className="absolute left-0 top-0 bottom-0 w-1.5 cursor-ew-resize z-10 hover:bg-blue-400 hover:opacity-30"
        style={{ background: 'transparent' }}
      />

      {/* Draggable header */}
      <div
        onMouseDown={onHeaderMouseDown}
        className={`flex items-center justify-between px-3 py-2 shrink-0 cursor-move select-none ${theme.mainContentSection}`}
        style={{ borderBottom: '1px solid rgba(0,0,0,0.10)' }}
      >
        <div className="flex items-center gap-2">
          <i className="fa-solid fa-robot text-blue-500" />
          <span className={`text-sm font-semibold ${theme.title}`}>Workflow AI</span>
        </div>
        <button onClick={onClose} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
          <i className="fa-solid fa-xmark" />
        </button>
      </div>

      {/* Panel body — fills remaining height */}
      <div className="w-full h-1 flex-auto overflow-hidden">
        <WorkflowAutomationAgentPanel
          transactionId={transactionId}
          onWorkflowChanged={onWorkflowChanged}
          onClose={onClose}
          hideHeader
        />
      </div>

      {/* Bottom-edge resize handle */}
      <div
        onMouseDown={onBottomEdgeMouseDown}
        className="absolute bottom-0 left-0 right-0 h-1.5 cursor-ns-resize hover:bg-blue-400 hover:opacity-30"
        style={{ background: 'transparent' }}
      />
    </div>
  );
};

const WorkflowAutomationEditor: React.FC<WorkflowAutomationEditorProps> = ({ embedded }) => {
  const isEmbedded = embedded != null;
  const { param } = useParams<RouteParams>();
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { theme, t } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((state: RootState) => state.tabnav.previousActiveTabKey);
  const userMenu = useSelector((state: RootState) => state.userSession.userMenu);

  /** Angular: navigationSvc.scope().threeLevelMenuList — root applications for Save As / Add To App Menu. */
  const applicationList = useMemo(() => {
    if (!userMenu || !Array.isArray(userMenu)) return [];
    return userMenu.filter((item: any) => item && item.Id != null && item.Name != null);
  }, [userMenu]);

  const parsedFromRoute = useMemo(() => {
    if (!param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      return obj as {
        id?: string | null;
        transactionId?: number | string | null;
        isCreateNewItem?: boolean;
        transactionType?: number | null;
        dataSourceRegisterId?: number | null;
        modelName?: string | null;
      };
    } catch {
      return null;
    }
  }, [param]);

  const parsed = useMemo(() => {
    if (embedded) return buildParsedFromEmbedded(embedded);
    return parsedFromRoute;
  }, [embedded, parsedFromRoute]);

  /**
   * Angular: controllerModel.transactionId — from route when editing existing workflow;
   * set from save response only for new workflow (isCreateNewItem).
   */
  const workflowTransactionIdRef = useRef<number | null>(routeWorkflowTransactionId(parsed));
  /** After save+prepareData, skip one loadTransaction triggered by route transactionId update (Angular refresh is explicit). */
  const skipNextLoadTransactionRef = useRef(false);

  useEffect(() => {
    const routeId = routeWorkflowTransactionId(parsed);
    if (routeId != null) {
      workflowTransactionIdRef.current = routeId;
    } else if (parsed?.isCreateNewItem) {
      workflowTransactionIdRef.current = null;
    }
  }, [parsed?.transactionId, parsed?.isCreateNewItem]);

  const [currentTransaction, setCurrentTransaction] = useState<any>(null);
  const [rootCommand, setRootCommand] = useState<any>(null);
  const [commandNodeTreeCV, setCommandNodeTreeCV] = useState<CollectionView | null>(null);
  const [dictActionIdAndDto, setDictActionIdAndDto] = useState<Record<number, any>>({});
  const [dictRootChildNodeLogicIdAndChildActionDto, setDictRootChildNodeLogicIdAndChildActionDto] = useState<Record<string, any>>({});
  const [currentEditAction, setCurrentEditAction] = useState<any>(null);
  const [currentTransactionUnit, setCurrentTransactionUnit] = useState<any>(null);
  const [transactionFieldCV, setTransactionFieldCV] = useState<CollectionView | null>(null);
  const [fieldLookups, setFieldLookups] = useState<CommandFieldLookupResult | null>(null);
  const [commandEditorHierarchy, setCommandEditorHierarchy] = useState<any>(null);
  const [commandEditorLoading, setCommandEditorLoading] = useState(false);
  const [selectedTreeRow, setSelectedTreeRow] = useState<any>(null);
  const [activeCommandTreeNodeLogicId, setActiveCommandTreeNodeLogicId] = useState<string | null>(null);
  const [commandTreeContextMenu, setCommandTreeContextMenu] = useState<CommandTreeContextMenuState>({
    visible: false,
    x: 0,
    y: 0,
    item: null,
  });
  const [childCommandParentAction, setChildCommandParentAction] = useState<any>(null);
  const emAppControlTypeEnum = useEnumValues('EmAppControlType');
  const emAppDataTypeEnum = useEnumValues('EmAppDataType');
  const commandTypeEnum = useEnumValues('EmAppTransactionCommandType');
  const transactionScopeEnum = useEnumValues('EmAppTransactionScopeUsage');
  const CONTROL_TYPE_CHECKBOX = Number(emAppControlTypeEnum?.CheckBox ?? 13);
  const CONTROL_TYPE_DDL = Number(emAppControlTypeEnum?.DDL ?? 1);
  const CONTROL_TYPE_TEXTBOX = Number(emAppControlTypeEnum?.TextBox ?? 2);
  const DATA_TYPE_STRING = Number(emAppDataTypeEnum?.String ?? 1);
  /** Angular: EmAppTransactionCommandType.CompositionCommand (= 200 in AppEnums.cs) */
  const compositionCommandActionType = Number(commandTypeEnum?.CompositionCommand ?? 200);
  /** Angular: EmAppTransactionCommandType.ExecuteSQLStatement (= 42 in AppEnums.cs) */
  const executeSqlCommandActionType = Number(commandTypeEnum?.ExecuteSQLStatement ?? 42);
  /** Angular: EmAppTransactionScopeUsage.WorkflowAutomation (= 2 in AppEnums.cs) */
  const workflowBusinessScopeId = Number(transactionScopeEnum?.WorkflowAutomation ?? 2);

  const operationTypeOptions = useMemo(() => getWorkflowEmbeddedCommandActionTypeOptions(), []);
  const [defaultDataSourceId, setDefaultDataSourceId] = useState<string | null>(null);
  const [isCollapseSessionVariables, setIsCollapseSessionVariables] = useState(true);
  const [applicationCV, setApplicationCV] = useState<CollectionView | null>(null);
  const [saveAsOpen, setSaveAsOpen] = useState(false);
  const [newWorkflowName, setNewWorkflowName] = useState('');
  const [newApplicationId, setNewApplicationId] = useState<string | null>(null);
  const [addExistingDropdownOpen, setAddExistingDropdownOpen] = useState(false);
  const [addToAppMenuPopupOpen, setAddToAppMenuPopupOpen] = useState(false);
  const [windowsSchedulerMenuOpen, setWindowsSchedulerMenuOpen] = useState(false);
  const [windowsSchedulerMenuPos, setWindowsSchedulerMenuPos] = useState<{ top: number; left: number } | null>(
    null,
  );
  const [windowsSchedulerPopupOpen, setWindowsSchedulerPopupOpen] = useState(false);
  const [schedulerTaskDto, setSchedulerTaskDto] = useState<WorkflowSchedulerTaskDto | null>(null);
  const [internalSelectorOpen, setInternalSelectorOpen] = useState(false);
  const [externalSelectorOpen, setExternalSelectorOpen] = useState(false);
  const [availableChildActionList, setAvailableChildActionList] = useState<any[]>([]);
  const [transactionList, setTransactionList] = useState<any[]>([]);
  const [externalTransactionId, setExternalTransactionId] = useState<string | number | null>(null);
  const [externalCommandId, setExternalCommandId] = useState<string | number | null>(null);
  const [externalCommandList, setExternalCommandList] = useState<any[]>([]);
  const [showAgentPanel, setShowAgentPanel] = useState(false);
  const [agentPanelTop,  setAgentPanelTop]  = useState(0);
  const [tableColumnSelectorOpen, setTableColumnSelectorOpen] = useState(false);
  const [paramsSelectedRowIndexes, setParamsSelectedRowIndexes] = useState<number[]>([]);
  /** Bumps on external-command in-place edits so controlled CommandEditor fields re-render */
  const [externalCommandEditorTick, setExternalCommandEditorTick] = useState(0);

  const commandGridRef  = useRef<any>(null);
  const paramsGridRef   = useRef<any>(null);
  const editorHeaderRef = useRef<HTMLDivElement>(null);
  const addExistingDropdownRef = useRef<HTMLDivElement>(null);
  const windowsSchedulerButtonRef = useRef<HTMLButtonElement>(null);
  const windowsSchedulerMenuPortalRef = useRef<HTMLUListElement>(null);
  const dictActionIdAndDtoRef = useRef<Record<number, any>>({});
  const activeCommandTreeNodeLogicIdRef = useRef<string | null>(null);
  const selectCommandForEditRef = useRef<(treeRow: any, action: any) => void>(() => {});
  const openCommandTreeNodeEditorRef = useRef<(treeRow: any, forceReopen?: boolean, reloadAfterPersist?: boolean) => void>(
    () => {},
  );
  const currentTransactionRef = useRef<any>(null);
  const currentTransactionUnitRef = useRef<any>(null);
  const transactionFieldCVRef = useRef<CollectionView | null>(null);
  const commandEditorHierarchyRef = useRef<any>(null);
  const selectedTreeRowRef = useRef<any>(null);
  const currentEditActionRef = useRef<any>(null);
  /** Same as Transaction Command Mgt — preserve SQL / field edits when switching tree rows. */
  const commandEditCacheRef = useRef<Map<number, any>>(new Map());
  const prevEditCommandIdRef = useRef<number | null>(null);
  const [commandEditorRevision, setCommandEditorRevision] = useState(0);

  useEffect(() => {
    currentTransactionRef.current = currentTransaction;
  }, [currentTransaction]);

  useEffect(() => {
    void import('@monaco-editor/react');
  }, []);
  useEffect(() => {
    currentTransactionUnitRef.current = currentTransactionUnit;
  }, [currentTransactionUnit]);
  useEffect(() => {
    transactionFieldCVRef.current = transactionFieldCV;
  }, [transactionFieldCV]);
  useEffect(() => {
    commandEditorHierarchyRef.current = commandEditorHierarchy;
  }, [commandEditorHierarchy]);
  useEffect(() => {
    selectedTreeRowRef.current = selectedTreeRow;
  }, [selectedTreeRow]);
  useEffect(() => {
    currentEditActionRef.current = currentEditAction;
  }, [currentEditAction]);

  const syncWorkflowCommandTree = useCallback(
    async (transactionData: any): Promise<any[]> => {
      if (!transactionData) return [];
      const commandList = transactionData.CommandActionList || [];
      const root = findWorkflowRootCommand(commandList, compositionCommandActionType);
      if (!root) return [];
      ensureWorkflowRootCommandShape(root);
      const payload = {
        ...transactionData,
        BusinessScopeId: Number(transactionData.BusinessScopeId ?? workflowBusinessScopeId),
        CommandActionList: commandList,
      };
      try {
        const synced = await appTransactionService.syncronizeWorkflowCommandNodeTreeFromActionList(payload);
        return extractWorkflowCommandNodeTree(synced);
      } catch {
        return [];
      }
    },
    [compositionCommandActionType, workflowBusinessScopeId],
  );

  const [commandTreeRevision, setCommandTreeRevision] = useState(0);

  const applyWorkflowCommandNodeTree = useCallback((tree: any[]) => {
    const cv = new CollectionView(tree);
    cv.sortDescriptions.clear();
    setCommandNodeTreeCV(cv);
    setCommandTreeRevision((n) => n + 1);
    setCurrentTransaction((prev: any) => (prev ? { ...prev, WorkflowCommandNodeTree: tree } : prev));
    if (currentTransactionRef.current) {
      currentTransactionRef.current = { ...currentTransactionRef.current, WorkflowCommandNodeTree: tree };
    }
    setTimeout(() => {
      const grid = commandGridRef.current?.control ?? commandGridRef.current;
      if (!grid) return;
      grid.itemsSource = cv;
      grid.childItemsPath = 'WorkflowChildTreeNodes';
      grid.refresh();
      grid.invalidate();
      grid.collapseGroupsToLevel?.(100);
    }, 0);
  }, []);

  const loadDataSources = useCallback(async () => {
    const list = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
    if (Array.isArray(list)) {
      let defaultId: string | null = null;
      for (const ds of list) {
        (ds as any).Display = ((ds as any).DataSourceName || '') + ' (' + (ds as any).Id + ')';
        if ((ds as any).Id !== 2147483647 && (ds as any).IsCompanyMasterDb) {
          defaultId = String((ds as any).Id);
          break;
        }
      }
      setDefaultDataSourceId(defaultId);
    }
  }, []);

  const resolveCanonicalCommand = useCallback((commandId: number | null | undefined): any | null => {
    if (commandId == null) return null;
    const n = Number(commandId);
    if (Number.isNaN(n) || n <= 0) return null;
    const list = currentTransactionRef.current?.CommandActionList || [];
    const fromList = list.find((c: any) => Number(c?.Id) === n);
    if (fromList) return fromList;
    return dictActionIdAndDtoRef.current[n] ?? null;
  }, []);

  /** Transaction Command Mgt: clear in-memory task drafts after Save / Refresh (server is source of truth). */
  const clearWorkflowCommandEditCache = useCallback(() => {
    clearCommandEditCache(commandEditCacheRef.current);
    prevEditCommandIdRef.current = null;
  }, []);

  const prepareData = useCallback((transactionData: any) => {
    if (!transactionData) return;
    clearWorkflowCommandEditCache();
    transactionData.BusinessScopeId = Number(transactionData.BusinessScopeId ?? workflowBusinessScopeId);
    transactionData.DictDeletedItemsIds = transactionData.DictDeletedItemsIds || { AppTransactionUnitList: [] };
    const unitList = transactionData.AppTransactionUnitList || [];
    const firstUnit = unitList[0] || null;
    if (firstUnit && firstUnit.AppTransactionFieldList) {
      const cv = new CollectionView([...firstUnit.AppTransactionFieldList]);
      cv.sortDescriptions.push(new SortDescription('SortOrder', true));
      setTransactionFieldCV(cv);
    } else {
      setTransactionFieldCV(null);
    }
    setCurrentTransactionUnit(firstUnit);
    setCurrentTransaction(transactionData);

    const lookups = buildCommandFieldLookups(
      transactionData,
      { checkbox: CONTROL_TYPE_CHECKBOX, ddl: CONTROL_TYPE_DDL },
      transactionData,
    );
    setFieldLookups(lookups);
    setCommandEditorHierarchy(transactionData);

    const commandList = transactionData.CommandActionList || [];
    hydrateWorkflowCommandActionList(commandList, executeSqlCommandActionType);
    const dict: Record<number, any> = {};
    commandList.forEach((a: any) => {
      dict[a.Id] = a;
      a.ActionGuid = a.ActionGuid || 'guid-' + Math.random().toString(36).slice(2);
    });
    setDictActionIdAndDto(dict);

    let root: any = findWorkflowRootCommand(commandList, compositionCommandActionType);
    if (!root && commandList.length === 0) {
      // Angular addRootAction: no Id until first save (insert, not update WorkFlowActionId 0).
      const newRoot: any = {
        Name: 'RootCommand',
        ActionFlowOrder: 1,
        NextTransactionId: null,
        ActionType: compositionCommandActionType,
        NotificationDestinationUserIdtransactionFiledId: null,
        NotificationDestinationRoleIdtransactionFiledId: null,
        DataLoadId: null,
        CommandConditionTransactionFieldId: null,
        ActionAttribute: {
          LinkToUI: true,
          IsShowOnTopMenu: true,
          NotificationDestinationEmailAddressTransactionFiledId: null,
          NotificationDestinationPartnerIdTransactionFiledId: null,
          ChildCommandsSwitchConditionFieldId: null,
          CallBackCommandID: null,
          AssignSqlResultToFiledId: null,
          IsWorkflowRootCommand: true,
          ChildActionList: []
        }
      };
      transactionData.CommandActionList.push(newRoot);
      setRootCommand(newRoot);
      setDictActionIdAndDto({ ...dict });
    } else if (root) {
      ensureWorkflowRootCommandShape(root);
      setRootCommand(root);
    } else {
      setRootCommand(null);
    }

    let tree = resolveWorkflowCommandNodeTree(transactionData, compositionCommandActionType);
    applyWorkflowCommandNodeTree(tree);

    const hasChildLinks =
      !!root?.ActionAttribute?.ChildActionList?.some((c: any) => c?.CommandId != null);
    const serverTree = extractWorkflowCommandNodeTree(transactionData.WorkflowCommandNodeTree);
    if (!serverTree.length && hasChildLinks) {
      void syncWorkflowCommandTree(transactionData).then((syncedTree) => {
        const nextTree =
          syncedTree.length > 0
            ? syncedTree
            : resolveWorkflowCommandNodeTree(
                { ...transactionData, CommandActionList: commandList },
                compositionCommandActionType,
              );
        if (nextTree.length > 0) {
          applyWorkflowCommandNodeTree(nextTree);
        }
      });
    }

    if (root && root.ActionAttribute && root.ActionAttribute.ChildActionList) {
      const childList = root.ActionAttribute.ChildActionList.sort((a: any, b: any) => (a.Sort || 0) - (b.Sort || 0));
      const dictChild: Record<string, any> = {};
      childList.forEach((child: any, idx: number) => {
        const logicId = '|' + (idx + 1) + '_' + child.CommandId;
        dictChild[logicId] = child;
      });
      setDictRootChildNodeLogicIdAndChildActionDto(dictChild);
    } else {
      setDictRootChildNodeLogicIdAndChildActionDto({});
    }

  }, [
    CONTROL_TYPE_CHECKBOX,
    CONTROL_TYPE_DDL,
    applyWorkflowCommandNodeTree,
    compositionCommandActionType,
    executeSqlCommandActionType,
    syncWorkflowCommandTree,
    workflowBusinessScopeId,
    clearWorkflowCommandEditCache,
  ]);

  const loadTransaction = useCallback(async () => {
    if (!parsed) return;
    clearWorkflowCommandEditCache();
    dispatch(setIsBusy());
    try {
      await loadDataSources();
      // Existing workflow: always use transactionId from management/route, never a stale ref.
      const routeId = routeWorkflowTransactionId(parsed);
      const tid =
        routeId != null
          ? String(routeId)
          : workflowTransactionIdRef.current != null
            ? String(workflowTransactionIdRef.current)
            : null;
      if (tid) {
        const data = await appTransactionService.getOneHierarchyTransaction(tid, false, '', '', '', false, '');
        prepareData(data);
      } else {
        const dsId = defaultDataSourceId;
        if (!dsId) {
          const list = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
          let defaultId: string | null = null;
          if (Array.isArray(list)) {
            for (const ds of list) {
              if ((ds as any).Id !== 2147483647 && (ds as any).IsCompanyMasterDb) {
                defaultId = String((ds as any).Id);
                break;
              }
            }
          }
          const data = await appTransactionService.prepareNewWorkflowAutomation(defaultId || '');
          if (data) {
            data.TransactionName = 'New Workflow';
            data.CommandActionList = data.CommandActionList || [];
            // Angular: transactionData.BusinessScopeId = EmAppTransactionScopeUsage.WorkflowAutomation
            data.BusinessScopeId = Number(transactionScopeEnum?.WorkflowAutomation ?? 2);
            data.SaasApplicationId = null;
          }
          prepareData(data);
        } else {
          const data = await appTransactionService.prepareNewWorkflowAutomation(dsId);
          if (data) {
            data.TransactionName = 'New Workflow';
            data.CommandActionList = data.CommandActionList || [];
            data.BusinessScopeId = Number(transactionScopeEnum?.WorkflowAutomation ?? 2);
            data.SaasApplicationId = null;
          }
          prepareData(data);
        }
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load workflow');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [parsed, defaultDataSourceId, loadDataSources, prepareData, dispatch, showError, transactionScopeEnum, clearWorkflowCommandEditCache]);

  useEffect(() => {
    if (parsed) {
      loadDataSources();
    }
  }, [parsed, loadDataSources]);

  useEffect(() => {
    if (!parsed || defaultDataSourceId === undefined) return;
    if (skipNextLoadTransactionRef.current) {
      skipNextLoadTransactionRef.current = false;
      return;
    }
    loadTransaction();
  }, [parsed?.transactionId, parsed?.isCreateNewItem, defaultDataSourceId, loadTransaction, parsed]);

  const markChange = useCallback((scope: 'workflow' | 'external' | 'both' = 'both') => {
    if (scope === 'workflow' || scope === 'both') {
      setCurrentTransaction((prev: any) =>
        prev ? { ...prev, isModified: true, IsModified: true } : prev,
      );
    }
    if (scope === 'external' || scope === 'both') {
      const ext = commandEditorHierarchyRef.current;
      const workflow = currentTransactionRef.current;
      if (
        selectedTreeRowRef.current?.ExternalTransactionId &&
        ext &&
        workflow &&
        Number(ext.Id) !== Number(workflow.Id)
      ) {
        ext.isModified = true;
        ext.IsModified = true;
        // External command edits mutate action in place (Angular parity); force controlled inputs to refresh
        setExternalCommandEditorTick((t) => t + 1);
      }
    }
  }, []);

  const generateFieldGuid = useCallback(
    () => `field_${Date.now()}_${Math.random().toString(36).slice(2)}`,
    [],
  );

  const getWorkflowUnitMaxFieldSortOrder = useCallback((unit: any) => {
    const list = unit?.AppTransactionFieldList;
    if (!Array.isArray(list) || list.length === 0) return 0;
    return Math.max(...list.map((f: any) => Number(f?.SortOrder) || 0));
  }, []);

  const syncWorkflowUnitFields = useCallback(
    (unit: any, fieldList: any[], unitModified = true) => {
      if (!unit) return;
      const updatedUnit = {
        ...unit,
        AppTransactionFieldList: fieldList,
        ...(unitModified ? { IsModified: true } : {}),
      };
      setCurrentTransactionUnit(updatedUnit);
      setCurrentTransaction((prev: any) => {
        if (!prev) return prev;
        const units = [...(prev.AppTransactionUnitList || [])];
        const idx = units.findIndex((u: any) => u?.Id === unit?.Id);
        if (idx >= 0) units[idx] = updatedUnit;
        else if (units.length > 0) units[0] = updatedUnit;
        return { ...prev, AppTransactionUnitList: units, isModified: true };
      });
      if (transactionFieldCV) {
        transactionFieldCV.sourceCollection = fieldList;
        transactionFieldCV.sortDescriptions.clear();
        transactionFieldCV.sortDescriptions.push(new SortDescription('SortOrder', true));
        transactionFieldCV.refresh();
      } else {
        const cv = new CollectionView([...fieldList]);
        cv.sortDescriptions.push(new SortDescription('SortOrder', true));
        setTransactionFieldCV(cv);
      }
      markChange('workflow');
    },
    [transactionFieldCV, markChange],
  );

  const isRemovableWorkflowField = useCallback((field: any) => {
    if (!field) return false;
    return !!field.IsTempVariable || String(field.DataBaseFieldName || '').startsWith('UserDefined');
  }, []);

  const removeOneWorkflowTransactionField = useCallback(
    (field: any) => {
      if (!currentTransactionUnit || !field) return;
      const unitId = currentTransactionUnit.Id;
      const fieldList = [...(currentTransactionUnit.AppTransactionFieldList || [])];
      const idx = fieldList.findIndex(
        (f: any) =>
          (field.Id != null && f.Id === field.Id) ||
          (field.RowIdentityGuid && f.RowIdentityGuid === field.RowIdentityGuid) ||
          f === field,
      );
      if (idx < 0) return;

      if (field.Id) {
        setCurrentTransaction((prev: any) => {
          if (!prev) return prev;
          const dictKey = `AppTransactionFieldList_${unitId}`;
          const dict = { ...(prev.DictDeletedItemsIds || {}) };
          const deleted = [...(dict[dictKey] || [])];
          if (!deleted.includes(field.Id)) deleted.push(field.Id);
          dict[dictKey] = deleted;
          return { ...prev, DictDeletedItemsIds: dict, isModified: true };
        });
      }

      if (field.RowIdentityGuid) {
        setCurrentTransaction((prev: any) => {
          if (!prev?.DictCurrentPKOrFKLinkToParentKeyGuidMap) return prev;
          const map = { ...prev.DictCurrentPKOrFKLinkToParentKeyGuidMap };
          Object.keys(map).forEach((key) => {
            if (map[key] === field.RowIdentityGuid || key === field.RowIdentityGuid) {
              delete map[key];
            }
          });
          return { ...prev, DictCurrentPKOrFKLinkToParentKeyGuidMap: map, isModified: true };
        });
      }

      fieldList.splice(idx, 1);
      syncWorkflowUnitFields(currentTransactionUnit, fieldList);
    },
    [currentTransactionUnit, syncWorkflowUnitFields],
  );

  const addNewWorkflowTransactionField = useCallback(
    (newFieldName: string, isTempField: boolean) => {
      if (!currentTransactionUnit) return null;
      const maxSort = getWorkflowUnitMaxFieldSortOrder(currentTransactionUnit);
      const sortOrder = Math.ceil(maxSort / 10.0) * 10 + 10;
      const newField = {
        uiId: generateFieldGuid(),
        RowIdentityGuid: generateFieldGuid(),
        SortOrder: sortOrder,
        DisplayName: newFieldName || `Field${sortOrder}`,
        DataBaseFieldName: '',
        ControlType: CONTROL_TYPE_TEXTBOX,
        DataType: DATA_TYPE_STRING,
        DisplayWidth: 100,
        Nbdecimal: 0,
        IsVisible: true,
        IsReadonly: false,
        IsGroupBy: false,
        IsGridUseAvailableEntitySource: false,
        IsNeedLog: false,
        IsLogicalDisplay: false,
        IsChangeTrigerNotification: false,
        IsAllowEmpty: true,
        IsConvertToUpperCase: false,
        IsPrimaryKey: false,
        IsLinkToParentPrimaryKey: false,
        IsTempVariable: !!isTempField,
      };
      const fieldList = [...(currentTransactionUnit.AppTransactionFieldList || []), newField];
      syncWorkflowUnitFields(currentTransactionUnit, fieldList);
      return newField;
    },
    [currentTransactionUnit, CONTROL_TYPE_TEXTBOX, generateFieldGuid, getWorkflowUnitMaxFieldSortOrder, syncWorkflowUnitFields],
  );

  const handleAddTempTransactionField = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation();
      addNewWorkflowTransactionField('', true);
    },
    [addNewWorkflowTransactionField],
  );

  const handleAddExistingTransactionField = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation();
      if (currentTransactionUnit?.DataBaseTableName) {
        setTableColumnSelectorOpen(true);
      }
    },
    [currentTransactionUnit?.DataBaseTableName],
  );

  const handleRemoveTransactionField = useCallback(
    (e?: React.MouseEvent) => {
      e?.stopPropagation();
      if (!currentTransactionUnit) return;
      const grid = paramsGridRef.current?.control ?? paramsGridRef.current;
      const selectedRows = grid?.selectedRows;
      if (!selectedRows?.length) return;

      const toRemove: any[] = [];
      selectedRows.forEach((row: any) => {
        const field = row?.dataItem ?? row?.item;
        if (isRemovableWorkflowField(field)) toRemove.push(field);
      });
      if (toRemove.length === 0) return;
      toRemove.forEach((field) => removeOneWorkflowTransactionField(field));
    },
    [currentTransactionUnit, isRemovableWorkflowField, removeOneWorkflowTransactionField],
  );

  const handleResetDefaultUnit = useCallback(
    async (e?: React.MouseEvent) => {
      e?.stopPropagation();
      if (!window.confirm('Confirm To Delete and Reset to Default Unit')) return;
      const dsId = currentTransaction?.DataSourceFrom ?? defaultDataSourceId ?? '';
      dispatch(setIsBusy());
      try {
        const transactionData = await appTransactionService.prepareNewWorkflowAutomation(String(dsId));
        const defaultUnit = transactionData?.AppTransactionUnitList?.[0] ?? null;
        if (!defaultUnit) return;
        setCurrentTransaction((prev: any) => {
          if (!prev) return prev;
          const units = [...(prev.AppTransactionUnitList || [])];
          if (units.length > 0) units[0] = defaultUnit;
          else units.push(defaultUnit);
          return { ...prev, AppTransactionUnitList: units, isModified: true };
        });
        const fieldList = defaultUnit.AppTransactionFieldList || [];
        const cv = new CollectionView([...fieldList]);
        cv.sortDescriptions.push(new SortDescription('SortOrder', true));
        setTransactionFieldCV(cv);
        setCurrentTransactionUnit(defaultUnit);
        markChange();
      } catch (err: any) {
        showError(err?.message || 'Failed to reset workflow parameters');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [currentTransaction?.DataSourceFrom, defaultDataSourceId, dispatch, markChange, showError],
  );

  const handleWorkflowTableColumnsSelected = useCallback(
    async (selectedColumns: any[]) => {
      if (!currentTransactionUnit || !currentTransaction || selectedColumns.length === 0) {
        setTableColumnSelectorOpen(false);
        return;
      }
      const existingNames = new Set(
        (currentTransactionUnit.AppTransactionFieldList || [])
          .filter((f: any) => f.DataBaseFieldName && !f.IsTempVariable)
          .map((f: any) => f.DataBaseFieldName),
      );
      const needToAdd = selectedColumns.filter((col: any) => !existingNames.has(col.Name));
      const duplicates = selectedColumns.filter((col: any) => existingNames.has(col.Name)).map((c: any) => c.Name);
      if (duplicates.length > 0) {
        showInfo(`The following fields already exist and will not be added: ${duplicates.join(', ')}`);
      }
      if (needToAdd.length === 0) {
        setTableColumnSelectorOpen(false);
        return;
      }
      dispatch(setIsBusy());
      try {
        const converterDto = {
          SchemaOwner: currentTransactionUnit.SchemaOwner || '',
          TableName: currentTransactionUnit.DataBaseTableName,
          ParentUnit: null,
          TransactionId: currentTransaction.Id,
          NeedToAddDbColumns: needToAdd,
          DataSourceRegisterId: currentTransaction.DataSourceFrom ?? defaultDataSourceId,
        };
        const transFieldList = await appTransactionService.convertTableColumnsToTransactionFieldExDtoList(converterDto);
        if (!transFieldList?.length) {
          setTableColumnSelectorOpen(false);
          return;
        }
        const maxSort = getWorkflowUnitMaxFieldSortOrder(currentTransactionUnit);
        let nextSort = Math.ceil(maxSort / 10.0) * 10 + 10;
        const newFields = transFieldList.map((fieldDto: any) => {
          const newField = { ...fieldDto, SortOrder: nextSort, uiId: generateFieldGuid() };
          nextSort += 10;
          return newField;
        });
        const fieldList = [...(currentTransactionUnit.AppTransactionFieldList || []), ...newFields];
        syncWorkflowUnitFields(currentTransactionUnit, fieldList);
        setCurrentTransaction((prev: any) => {
          if (!prev?.DictCurrentPKOrFKLinkToParentKeyGuidMap) return prev;
          const map = { ...prev.DictCurrentPKOrFKLinkToParentKeyGuidMap };
          newFields.forEach((field: any) => {
            if (field.ParentPKFieldGuid && field.RowIdentityGuid) {
              map[field.RowIdentityGuid] = field.ParentPKFieldGuid;
            }
          });
          return { ...prev, DictCurrentPKOrFKLinkToParentKeyGuidMap: map, isModified: true };
        });
      } catch (err: any) {
        showError(err?.message || 'Failed to add built-in fields');
      } finally {
        dispatch(setIsNotBusy());
        setTableColumnSelectorOpen(false);
      }
    },
    [
      currentTransaction,
      currentTransactionUnit,
      defaultDataSourceId,
      dispatch,
      generateFieldGuid,
      getWorkflowUnitMaxFieldSortOrder,
      showError,
      showInfo,
      syncWorkflowUnitFields,
    ],
  );

  const handleParamsGridSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const indexes: number[] = [];
    flex?.selectedRows?.forEach((row: any) => {
      if (typeof row?.index === 'number' && row.index >= 0) indexes.push(row.index);
    });
    setParamsSelectedRowIndexes(indexes);
  }, []);

  const workflowParamsLockedColumnNames = useMemo(() => {
    if (!currentTransactionUnit?.AppTransactionFieldList) return [];
    return currentTransactionUnit.AppTransactionFieldList.filter(
      (f: any) => !f.IsTempVariable && f.DataBaseFieldName,
    ).map((f: any) => f.DataBaseFieldName);
  }, [currentTransactionUnit?.AppTransactionFieldList]);

  const showAddBuiltInField = !!(
    currentTransactionUnit?.DataBaseTableName &&
    currentTransactionUnit.DataBaseTableName !== 'App_VirtualView'
  );
  const showAddTempAndRemoveField = !!currentTransactionUnit;

  const refreshCommandTreeCell = useCallback(() => {
    if (!currentEditAction || !commandNodeTreeCV) return;
    const items = commandNodeTreeCV.sourceCollection as any[];
    if (!Array.isArray(items)) return;
    const commandId = currentEditAction.Id;
    let changed = false;
    items.forEach((node: any) => {
      if (node?.Id === commandId || node?.CommandId === commandId) {
        if (node.DisplayName !== currentEditAction.Name) {
          node.DisplayName = currentEditAction.Name;
          changed = true;
        }
      }
    });
    if (changed) commandNodeTreeCV.refresh();
  }, [currentEditAction, commandNodeTreeCV]);

  const resolveCommandEditorContext = useCallback(
    async (treeRow: any, action: any) => {
      if (!treeRow) {
        setCommandEditorHierarchy(null);
        setFieldLookups(null);
        setSelectedTreeRow(null);
        return;
      }
      const externalTxId = treeRow?.ExternalTransactionId;
      if (!externalTxId && !action) {
        setCommandEditorHierarchy(null);
        setFieldLookups(null);
        setSelectedTreeRow(null);
        return;
      }
      setSelectedTreeRow(treeRow);
      if (externalTxId == null) {
        if (currentTransaction) {
          const lookups = buildCommandFieldLookups(
            currentTransaction,
            { checkbox: CONTROL_TYPE_CHECKBOX, ddl: CONTROL_TYPE_DDL },
            currentTransaction,
          );
          setFieldLookups(lookups);
          setCommandEditorHierarchy(currentTransaction);
        }
        return;
      }
      setCommandEditorLoading(true);
      try {
        const extData = await appTransactionService.getOneHierarchyTransaction(
          String(externalTxId),
          false,
          '',
          '',
          '',
          false,
          '',
        );
        const commandId = treeRow?.Id ?? treeRow?.CommandId ?? action?.Id;
        const extAction =
          (extData.CommandActionList || []).find((c: any) => Number(c?.Id) === Number(commandId)) ?? action;
        setCurrentEditAction(extAction);
        const lookups = buildCommandFieldLookups(
          extData,
          { checkbox: CONTROL_TYPE_CHECKBOX, ddl: CONTROL_TYPE_DDL },
          currentTransaction,
        );
        setFieldLookups(lookups);
        setCommandEditorHierarchy(extData);
      } catch (e: any) {
        showError(e?.message || 'Failed to load external command data model');
        setCommandEditorHierarchy(currentTransaction);
      } finally {
        setCommandEditorLoading(false);
      }
    },
    [currentTransaction, CONTROL_TYPE_CHECKBOX, CONTROL_TYPE_DDL, showError],
  );

  const selectCommandForEdit = useCallback(
    (treeRow: any, actionHint: any) => {
      const isExternal = treeRow?.ExternalTransactionId != null;
      const nextIdRaw = actionHint?.Id ?? treeRow?.Id ?? treeRow?.CommandId;
      const nextId =
        nextIdRaw != null && !Number.isNaN(Number(nextIdRaw)) && Number(nextIdRaw) > 0
          ? Number(nextIdRaw)
          : null;

      if (!isExternal && nextId != null) {
        // Already editing this command (e.g. pencil clicked again) — do not remount Monaco / restore cache.
        if (
          prevEditCommandIdRef.current === nextId &&
          currentEditActionRef.current &&
          Number(currentEditActionRef.current.Id) === nextId
        ) {
          return;
        }

        const prevId = prevEditCommandIdRef.current;
        const prevDraft = currentEditActionRef.current;
        if (prevId != null && prevId !== nextId && prevDraft) {
          const prevCanonical = resolveCanonicalCommand(prevId);
          if (prevCanonical && prevCanonical !== prevDraft) {
            copyCommandInPlace(prevCanonical, prevDraft);
          }
          const toStash = prevCanonical ?? prevDraft;
          if (toStash) stashCommand(commandEditCacheRef.current, toStash);
        }

        let action = resolveCanonicalCommand(nextId) ?? actionHint;
        if (action) {
          restoreWorkflowCommandFromCache(commandEditCacheRef.current, action);
          ensureWorkflowCommandSqlNotificationMessage(action, executeSqlCommandActionType);
          dictActionIdAndDtoRef.current[nextId] = action;
          setDictActionIdAndDto((prev) => ({ ...prev, [nextId]: action }));
        }
        prevEditCommandIdRef.current = nextId;
        setCurrentEditAction(action);
        setCommandEditorRevision((t) => t + 1);
        void resolveCommandEditorContext(treeRow, action);
        return;
      }

      prevEditCommandIdRef.current = nextId;
      setCurrentEditAction(actionHint);
      setCommandEditorRevision((t) => t + 1);
      void resolveCommandEditorContext(treeRow, actionHint);
    },
    [executeSqlCommandActionType, resolveCanonicalCommand, resolveCommandEditorContext],
  );

  selectCommandForEditRef.current = selectCommandForEdit;

  const refresh = useCallback(async () => {
    await loadTransaction();
  }, [loadTransaction]);

  const stripUnitUiProps = useCallback((unit: any) => {
    if (!unit) return;
    delete unit.transactionFieldCV;
    delete unit.parent;
  }, []);

  /** Angular workflowAutomationEditorCtrl prepareOneUnitSaveData + prepareSaveData */
  const prepareTransactionSaveData = useCallback(
    (trans: any, unit: any | null, fieldCv: CollectionView | null) => {
      if (!trans) return trans;

      trans.BusinessScopeId = Number(trans.BusinessScopeId ?? workflowBusinessScopeId);
      trans.isModified = true;
      trans.IsModified = true;
      trans.DictDeletedItemsIds = trans.DictDeletedItemsIds || { AppTransactionUnitList: [] };

      hydrateWorkflowCommandActionList(trans.CommandActionList, executeSqlCommandActionType);
      trans.CommandActionList = prepareWorkflowCommandActionListForSave(
        trans.CommandActionList,
        trans.TransactionName || '',
        compositionCommandActionType,
      );

      if (unit && fieldCv) {
        const fieldList = [...((fieldCv.sourceCollection as any[]) || [])];
        unit.AppTransactionFieldList = fieldList;
        unit.IsModified = true;
        const units = trans.AppTransactionUnitList || [];
        const unitIdx = units.findIndex((u: any) => u?.Id === unit?.Id);
        if (unitIdx >= 0) units[unitIdx] = unit;
        else if (units.length > 0) units[0] = unit;
      }

      trans.DictCurrentPKOrFKLinkToParentKeyGuidMap = {};

      const prepareOneUnitSaveData = (aUnit: any) => {
        if (!aUnit) return;
        stripUnitUiProps(aUnit);
        const fields = aUnit.AppTransactionFieldList;
        if (!Array.isArray(fields)) return;
        for (const transField of fields) {
          if (!transField) continue;
          if (!transField.Id) {
            transField.IsNew = true;
            aUnit.IsModified = true;
          }
          if (transField.ParentPKFieldGuid && transField.RowIdentityGuid) {
            trans.DictCurrentPKOrFKLinkToParentKeyGuidMap[transField.RowIdentityGuid] =
              transField.ParentPKFieldGuid;
          }
        }
      };

      const units = trans.AppTransactionUnitList || [];
      for (const aUnit of units) {
        prepareOneUnitSaveData(aUnit);
        for (const child of aUnit.Children || []) {
          prepareOneUnitSaveData(child);
          for (const grandchild of child.Children || []) {
            prepareOneUnitSaveData(grandchild);
          }
        }
      }

      return trans;
    },
    [compositionCommandActionType, executeSqlCommandActionType, stripUnitUiProps, workflowBusinessScopeId],
  );

  const isWorkflowTransactionDto = useCallback((value: any) => {
    return (
      value &&
      typeof value === 'object' &&
      !('nativeEvent' in value) &&
      !(value.target instanceof HTMLElement) &&
      (Array.isArray(value.CommandActionList) ||
        Array.isArray(value.AppTransactionUnitList) ||
        value.TransactionName != null ||
        value.Id != null)
    );
  }, []);

  const runCommandAutoSaveIfNeeded = useCallback((): Promise<void> => {
    return new Promise((resolve) => {
      const action = currentEditActionRef.current;
      if (typeof action?.autoSaveFunc === 'function') {
        action.autoSaveFunc(() => resolve());
      } else {
        resolve();
      }
    });
  }, []);

  /**
   * Angular autoSaveCurrrentExternalCommand — persist external command edits only.
   * Use SaveOneTransactionCommandActionList (same as TransactionCommandMgt), NOT full SaveAppTransaction,
   * which re-validates all units/fields and fails when PK map is not fully hydrated in embedded editor.
   */
  const autoSaveExternalCommandIfNeeded = useCallback(async (): Promise<boolean> => {
    const treeRow = selectedTreeRowRef.current;
    const extHierarchy = commandEditorHierarchyRef.current;
    const workflow = currentTransactionRef.current;
    if (!treeRow?.ExternalTransactionId || !extHierarchy?.Id) return true;
    if (Number(extHierarchy.Id) === Number(workflow?.Id)) return true;
    if (!extHierarchy.isModified && !extHierarchy.IsModified) return true;

    const commandList = extHierarchy.CommandActionList;
    if (!Array.isArray(commandList) || commandList.length === 0) return true;

    const result = await appTransactionService.saveOneTransactionCommandActionList({
      Id: extHierarchy.Id,
      CommandActionList: commandList,
    });
    if (result?.ValidationResult) {
      showValidationMessages(result.ValidationResult);
    }
    if (!result?.IsSuccessful) {
      showError('Failed to save the external data model command. Workflow was not saved.');
      return false;
    }
    extHierarchy.isModified = false;
    extHierarchy.IsModified = false;
    return true;
  }, [showError, showValidationMessages]);

  const save = useCallback(async (transactionOverride?: any, options?: SaveWorkflowOptions) => {
    await runCommandAutoSaveIfNeeded();

    const extSaved = await autoSaveExternalCommandIfNeeded();
    if (!extSaved) return false;

    const source = isWorkflowTransactionDto(transactionOverride)
      ? transactionOverride
      : currentTransactionRef.current;
    if (!source) return false;

    const editingCommandIdAtSaveStart =
      currentEditActionRef.current?.Id != null ? Number(currentEditActionRef.current.Id) : null;

    flushAllWorkflowCommandEditsToTransactionList(
      source,
      commandEditCacheRef.current,
      currentEditActionRef.current,
      executeSqlCommandActionType,
    );

    const trans = prepareTransactionSaveData(
      source,
      currentTransactionUnitRef.current,
      transactionFieldCVRef.current,
    );

    dispatch(setIsBusy());
    try {
      const data = await appTransactionService.saveOneWorkflowAutomation(trans);
      if (data?.ValidationResult && !options?.silent) {
        showValidationMessages(data.ValidationResult);
      }
      if (data?.IsSuccessful) {
        const savedId = data?.Object?.Id ?? data?.Object?.id ?? trans?.Id ?? trans?.id ?? null;
        const idNum =
          savedId != null && savedId !== '' && !Number.isNaN(Number(savedId)) ? Number(savedId) : null;
        if (idNum != null) {
          workflowTransactionIdRef.current = idNum;
          const routeId = routeWorkflowTransactionId(parsed);
          const shouldUpdateRoute = routeId == null || Number(routeId) !== idNum;
          if (parsed && shouldUpdateRoute && !isEmbedded) {
            skipNextLoadTransactionRef.current = true;
            const nextParam = {
              ...parsed,
              transactionId: idNum,
              isCreateNewItem: false,
              modelName: data?.Object?.TransactionName ?? trans?.TransactionName ?? parsed.modelName,
            };
            const reactPath = getReactPathForRouteCode('workflow-automation-editor');
            const newPath = buildRoutePathFromParamObj(reactPath, {
              ...nextParam,
              isNavigatedFromTab: true,
            });
            dispatch(updateActiveTabPath(newPath));
            navigate(newPath, { replace: true });
            const tabLabel = String(nextParam.modelName || 'Workflow Automation Editor').trim();
            if (tabLabel) {
              dispatch(updateCurrentTabLabel(tabLabel));
            }
          }
        }

        const transForTreeSync = buildTransactionForWorkflowTreeSync(
          data?.Object,
          source,
          idNum,
          compositionCommandActionType,
          workflowBusinessScopeId,
        );

        let transAfterSave: any = {
          ...transForTreeSync,
          ...(idNum != null ? { Id: idNum } : {}),
          TransactionName: data?.Object?.TransactionName ?? source.TransactionName,
          isModified: false,
          IsModified: false,
        };

        // Angular: save() then refresh() — reload hierarchy so server builds WorkflowCommandNodeTree.
        if (idNum != null) {
          try {
            const reloaded = await appTransactionService.getOneHierarchyTransaction(
              String(idNum),
              false,
              '',
              '',
              '',
              false,
              '',
            );
            if (reloaded) {
              // Angular save() then refresh(): server hierarchy is source of truth (no client merge).
              transAfterSave = {
                ...reloaded,
                BusinessScopeId: workflowBusinessScopeId,
                isModified: false,
                IsModified: false,
              };
              hydrateWorkflowCommandActionList(
                transAfterSave.CommandActionList,
                executeSqlCommandActionType,
              );
            }
          } catch {
            // Use merged in-memory transaction when reload fails.
          }
        }

        let tree = resolveWorkflowCommandNodeTree(transAfterSave, compositionCommandActionType);
        if (!tree.length) {
          const synced = await syncWorkflowCommandTree(transAfterSave);
          tree =
            synced.length > 0
              ? synced
              : resolveWorkflowCommandNodeTree(transAfterSave, compositionCommandActionType);
        }
        transAfterSave.WorkflowCommandNodeTree = tree;
        hydrateWorkflowCommandActionList(transAfterSave.CommandActionList, executeSqlCommandActionType);

        clearWorkflowCommandEditCache();
        currentTransactionRef.current = transAfterSave;
        prepareData(transAfterSave);

        if (!tree.length) {
          tree = resolveWorkflowCommandNodeTree(transAfterSave, compositionCommandActionType);
          if (tree.length > 0) {
            transAfterSave.WorkflowCommandNodeTree = tree;
            applyWorkflowCommandNodeTree(tree);
          }
        } else {
          applyWorkflowCommandNodeTree(tree);
        }

        const restoreCommandId =
          options?.reselectCommandId != null
            ? Number(options.reselectCommandId)
            : editingCommandIdAtSaveStart;

        if (restoreCommandId != null && !Number.isNaN(restoreCommandId)) {
          const treeNode = findWorkflowTreeNodeByCommandId(tree, restoreCommandId);
          if (treeNode) {
            openCommandTreeNodeEditorRef.current(treeNode, true, true);
          } else {
            const action =
              dictActionIdAndDtoRef.current[restoreCommandId] ??
              resolveCanonicalCommand(restoreCommandId);
            if (action) {
              ensureWorkflowCommandSqlNotificationMessage(action, executeSqlCommandActionType);
              prevEditCommandIdRef.current = null;
              setCurrentEditAction(action);
              setCommandEditorRevision((t) => t + 1);
              void resolveCommandEditorContext(
                { Id: restoreCommandId, CommandId: restoreCommandId },
                action,
              );
            }
          }
        } else if (!options?.keepEditorSelection) {
          setCurrentEditAction(null);
          setCommandEditorHierarchy(null);
          setSelectedTreeRow(null);
          setExternalCommandEditorTick(0);
          setActiveCommandTreeNodeLogicId(null);
        }
        return true;
      }
      if (!data?.ValidationResult?.Items?.length) {
        showError('Workflow save was not successful.');
      }
      return false;
    } catch (e: any) {
      showError(e?.message || 'Failed to save workflow');
      return false;
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    autoSaveExternalCommandIfNeeded,
    dispatch,
    isEmbedded,
    isWorkflowTransactionDto,
    navigate,
    parsed,
    applyWorkflowCommandNodeTree,
    compositionCommandActionType,
    prepareTransactionSaveData,
    prepareData,
    resolveCommandEditorContext,
    runCommandAutoSaveIfNeeded,
    showError,
    showValidationMessages,
    syncWorkflowCommandTree,
    workflowBusinessScopeId,
    clearWorkflowCommandEditCache,
    executeSqlCommandActionType,
  ]);

  const resolveWorkflowId = useCallback((): number | null => {
    const routeId = routeWorkflowTransactionId(parsed);
    if (routeId != null && routeId > 0) return routeId;
    const refId = workflowTransactionIdRef.current;
    if (refId != null && refId > 0) return refId;
    const transId = currentTransactionRef.current?.Id ?? currentTransaction?.Id;
    if (transId != null) {
      const n = Number(transId);
      if (!Number.isNaN(n) && n > 0) return n;
    }
    return null;
  }, [parsed, currentTransaction?.Id]);

  /** New workflow: auto-save first (Angular refresh after save), then return transaction Id. */
  const ensureWorkflowHasId = useCallback(async (): Promise<number | null> => {
    const existing = resolveWorkflowId();
    if (existing != null) return existing;
    const saved = await save(undefined, { silent: true, keepEditorSelection: true });
    if (!saved) return null;
    return resolveWorkflowId();
  }, [resolveWorkflowId, save]);

  const handleSaveAs = useCallback(async () => {
    if (!newWorkflowName || !newApplicationId || !currentTransaction?.Id) return;
    setSaveAsOpen(false);
    dispatch(setIsBusy());
    try {
      await appTransactionService.saveAsOneWorkflowAutomation(
        currentTransaction.Id,
        '',
        newWorkflowName,
        newApplicationId
      );
      refresh();
    } catch (e: any) {
      showError(e?.message || 'Failed to save as new workflow');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentTransaction?.Id, newWorkflowName, newApplicationId, refresh, dispatch, showError]);

  /** Angular generateNavigation: save, then QuickGenerateTransactionDefaultSeachNavigation (menu warning from server). */
  const addToAppMenuForApplication = useCallback(
    async (applicationId: number | string) => {
      setAddToAppMenuPopupOpen(false);
      const tid =
        currentTransaction?.Id ??
        workflowTransactionIdRef.current ??
        routeWorkflowTransactionId(parsed);
      if (tid == null) {
        showError('Workflow must be saved before adding to app menu.');
        return;
      }
      const appIdNum = Number(applicationId);
      if (Number.isNaN(appIdNum) || appIdNum <= 0) return;

      const transWithApp = currentTransaction
        ? {
            ...currentTransaction,
            SaasApplicationId: appIdNum,
            isModified: true,
            IsModified: true,
          }
        : null;
      if (transWithApp) {
        setCurrentTransaction(transWithApp);
        currentTransactionRef.current = transWithApp;
      }

      const saved = await save(transWithApp ?? undefined);
      if (!saved) return;

      dispatch(setIsBusy());
      try {
        await syncWorkflowNavigationToTargetApplication(Number(tid), appIdNum);
        const data = await schemaMetadataService.quickGenerateTransactionDefaultSearchNavigation(Number(tid));
        if (data?.ValidationResult?.Items?.length) {
          const lastItem = data.ValidationResult.Items[data.ValidationResult.Items.length - 1];
          showValidationMessages({ Items: [lastItem] }, true);
        } else if (data?.IsSuccessful) {
          showInfo('Transaction added to app menu successfully');
        }
        if (data?.IsSuccessful) {
          refresh();
          try {
            const userMenu = await adminSvc.retrieveUserTreeMenu();
            dispatch(setUserMenu(userMenu));
          } catch {
            // Non-blocking — editor already refreshed; sidebar may need manual reload if this fails.
          }
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to add to app menu');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [currentTransaction, parsed, save, refresh, dispatch, showError, showValidationMessages, showInfo],
  );

  dictActionIdAndDtoRef.current = dictActionIdAndDto;

  useEffect(() => {
    activeCommandTreeNodeLogicIdRef.current = activeCommandTreeNodeLogicId;
  }, [activeCommandTreeNodeLogicId]);

  const reselectTreeRowByLogicId = useCallback((treeNodeLogicId: string | null, openCommand = false) => {
    if (!treeNodeLogicId) return;
    const grid = commandGridRef.current?.control ?? commandGridRef.current;
    if (!grid?.rows?.length) return;
    for (let i = 0; i < grid.rows.length; i++) {
      const rowItem = grid.rows[i]?.dataItem ?? (grid.rows[i] as any)?.item;
      if (rowItem?.TreeNodeLogicId === treeNodeLogicId) {
        grid.select(new CellRange(i, 0, i, 0), false);
        if (openCommand) {
          const dict = dictActionIdAndDtoRef.current;
          const commandId = rowItem.Id ?? rowItem.CommandId;
          const action =
            rowItem.ExternalTransactionId != null
              ? null
              : dict?.[commandId] ?? dict?.[Number(commandId)];
          selectCommandForEditRef.current(rowItem, action);
        }
        grid.invalidate();
        break;
      }
    }
  }, []);

  useEffect(() => {
    if (!commandNodeTreeCV || !activeCommandTreeNodeLogicIdRef.current) return;
    const timer = setTimeout(() => {
      reselectTreeRowByLogicId(activeCommandTreeNodeLogicIdRef.current, true);
    }, 0);
    return () => clearTimeout(timer);
  }, [commandNodeTreeCV, reselectTreeRowByLogicId]);

  const handleCommandGridInitialized = useCallback((grid: any) => {
    const flex = grid?.control ?? grid;
    if (flex) {
      flex.allowSorting = false;
    }
  }, []);

  const openCommandTreeNodeEditor = useCallback(
    (treeRow: any, forceReopen = false, reloadAfterPersist = false) => {
      if (!treeRow?.Id || !treeRow?.TreeNodeLogicId) return;
      const commandId = Number(treeRow.Id ?? treeRow.CommandId);
      const isAlreadyEditingThisNode =
        !reloadAfterPersist &&
        activeCommandTreeNodeLogicId === treeRow.TreeNodeLogicId &&
        prevEditCommandIdRef.current != null &&
        Number(prevEditCommandIdRef.current) === commandId &&
        !!currentEditActionRef.current;

      if (isAlreadyEditingThisNode) return;
      if (!forceReopen && activeCommandTreeNodeLogicId === treeRow.TreeNodeLogicId) return;

      setActiveCommandTreeNodeLogicId(treeRow.TreeNodeLogicId);
      reselectTreeRowByLogicId(treeRow.TreeNodeLogicId, false);

      if (treeRow.ExternalTransactionId != null) {
        selectCommandForEdit(treeRow, null);
      } else {
        const action =
          dictActionIdAndDtoRef.current[commandId] ?? dictActionIdAndDtoRef.current[Number(commandId)];
        if (action) selectCommandForEdit(treeRow, action);
      }

      const grid = commandGridRef.current?.control ?? commandGridRef.current;
      grid?.invalidate?.();
    },
    [activeCommandTreeNodeLogicId, selectCommandForEdit, reselectTreeRowByLogicId],
  );

  openCommandTreeNodeEditorRef.current = openCommandTreeNodeEditor;

  const handleCommandTreeFormatItem = useCallback(
    (s: any, e: any) => {
      if (e.panel?.cellType !== CellType.Cell) return;
      const row = e.panel?.rows?.[e.row];
      const dataItem = row?.dataItem;
      if (dataItem?.TreeNodeLogicId && dataItem.TreeNodeLogicId === activeCommandTreeNodeLogicId) {
        e.cell.style.backgroundColor = '#e5e5e5';
      } else {
        e.cell.style.backgroundColor = '#ffffff';
      }
    },
    [activeCommandTreeNodeLogicId],
  );

  const closeCommandTreeContextMenu = useCallback(() => {
    setCommandTreeContextMenu({ visible: false, x: 0, y: 0, item: null });
  }, []);

  const openCommandTreeNodeContextMenu = useCallback((e: React.MouseEvent, item: any) => {
    e.stopPropagation();
    const rect = (e.currentTarget as HTMLElement).getBoundingClientRect();
    setCommandTreeContextMenu({ visible: true, x: rect.right, y: rect.top, item });
  }, []);

  useEffect(() => {
    if (!commandTreeContextMenu.visible) return;
    const onDocClick = () => closeCommandTreeContextMenu();
    document.addEventListener('click', onDocClick);
    return () => document.removeEventListener('click', onDocClick);
  }, [commandTreeContextMenu.visible, closeCommandTreeContextMenu]);

  const collapseAll = useCallback(() => {
    const grid = commandGridRef.current?.control ?? commandGridRef.current;
    if (grid?.collapseGroupsToLevel) grid.collapseGroupsToLevel(0);
  }, []);

  const expandAll = useCallback(() => {
    const grid = commandGridRef.current?.control ?? commandGridRef.current;
    if (grid?.collapseGroupsToLevel) grid.collapseGroupsToLevel(100);
  }, []);

  const addChildAction = useCallback(
    (parentAction: any, commandDto?: any, externalTransactionId?: number | string) => {
      if (!parentAction?.ActionAttribute || !currentTransaction) return null;
      const childList = parentAction.ActionAttribute.ChildActionList || [];
      const maxSort = Math.max(0, ...childList.map((c: any) => c.Sort || 0));
      const childDto: any = { Sort: maxSort + 1, CommandId: null as number | null };
      if (commandDto) {
        childDto.CommandId = commandDto.Id ?? null;
        childDto.CommandDisplay = commandDto.Display ?? commandDto.Name ?? '';
      }
      if (externalTransactionId != null) {
        childDto.ExternalTransactionId = externalTransactionId;
        if (commandDto) childDto.CommandDisplay = 'External: ' + (commandDto.Name || '');
      }
      childList.push(childDto);
      parentAction.ActionAttribute.ChildActionList = childList;
      markChange();
      setDictRootChildNodeLogicIdAndChildActionDto((prev) => {
        const logicId = '|' + childList.length + '_' + (childDto.CommandId ?? 'ext');
        return { ...prev, [logicId]: childDto };
      });
      return childDto;
    },
    [currentTransaction, markChange]
  );

  const syncTreeAndSave = useCallback(async () => {
    const trans = currentTransactionRef.current;
    if (!trans) return;
    try {
      const tree = await syncWorkflowCommandTree(trans);
      if (tree.length > 0) {
        const updated = { ...trans, WorkflowCommandNodeTree: tree };
        currentTransactionRef.current = updated;
        setCurrentTransaction(updated);
        applyWorkflowCommandNodeTree(tree);
        await save(updated);
        return;
      }
    } catch {
      // fall through to save current transaction
    }
    await save(trans);
  }, [applyWorkflowCommandNodeTree, save, syncWorkflowCommandTree]);

  const createNewRootTask = useCallback(async () => {
    let trans = currentTransactionRef.current ?? currentTransaction;
    let parentRoot = resolveWorkflowParentRoot(trans, rootCommand, compositionCommandActionType);
    if (!trans?.CommandActionList?.length || !parentRoot) {
      showError('Workflow root command is missing. Save the workflow or refresh the page.');
      return;
    }
    const workflowId = await ensureWorkflowHasId();
    if (workflowId == null || Number.isNaN(workflowId)) {
      return;
    }
    trans = currentTransactionRef.current ?? currentTransaction;
    parentRoot = resolveWorkflowParentRoot(trans, rootCommand, compositionCommandActionType);
    if (!trans || !parentRoot) {
      showError('Workflow root command is missing. Refresh the page and try again.');
      return;
    }
    setRootCommand(parentRoot);
    const childList = parentRoot.ActionAttribute?.ChildActionList || [];
    const maxSort = Math.max(0, ...childList.map((c: any) => c.Sort || 0));
    const newAction: any = {
      CommandTransactionId: workflowId,
      ActionGuid: 'guid-' + Math.random().toString(36).slice(2),
      ActionFlowOrder: maxSort + 1,
      Name: '_ChildCommand' + (maxSort + 1),
      NextTransactionId: null,
      ActionType: executeSqlCommandActionType,
      NotificationDestinationUserIdtransactionFiledId: null,
      NotificationDestinationRoleIdtransactionFiledId: null,
      DataLoadId: null,
      CommandConditionTransactionFieldId: null,
      ActionAttribute: {
        LinkToUI: false,
        IsLogCommandStartEnd: false,
        IsLogErrorDetails: true,
        SqlStatement: '',
      },
    };
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.createOneTransactionCommand(newAction);
      if (!result?.IsSuccessful || !result?.Object) {
        showValidationMessages(result?.ValidationResult);
        return;
      }
      const commandExDto = result.Object;
      const { transaction: updatedTrans, root: updatedRoot, dictChild } = applyNewCommandToWorkflowTransaction(
        trans,
        parentRoot,
        commandExDto,
      );
      setRootCommand(updatedRoot);
      setDictRootChildNodeLogicIdAndChildActionDto(dictChild);
      setDictActionIdAndDto((prev) => ({ ...prev, [commandExDto.Id]: commandExDto }));
      setCurrentTransaction(updatedTrans);
      currentTransactionRef.current = updatedTrans;
      // Angular: addChildAction then $scope.save() — not sync-then-save fire-and-forget
      await save(updatedTrans, { reselectCommandId: commandExDto.Id });
    } catch (e: any) {
      showError(e?.message || 'Failed to create task');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    currentTransaction,
    rootCommand,
    compositionCommandActionType,
    ensureWorkflowHasId,
    save,
    dispatch,
    showError,
    showValidationMessages,
    executeSqlCommandActionType,
  ]);

  const prepareAvailableChildCommandList = useCallback(
    (parentAction?: any) => {
      const parent = parentAction ?? rootCommand;
      if (!currentTransaction?.CommandActionList || !parent) return [];
      const list = currentTransaction.CommandActionList.filter((a: any) => a.Id !== parent.Id);
      return list
        .map((a: any) => ({
          ...a,
          displayName: (a.Name || '') + (a.Description ? ': ' + a.Description : ''),
        }))
        .sort((a: any, b: any) => (a.displayName || '').localeCompare(b.displayName || ''));
    },
    [currentTransaction, rootCommand],
  );

  const openInternalSelector = useCallback(
    async (parentAction?: any) => {
      setAddExistingDropdownOpen(false);
      closeCommandTreeContextMenu();
      const workflowId = await ensureWorkflowHasId();
      if (workflowId == null) return;
      const parent = parentAction ?? childCommandParentAction ?? rootCommand;
      setChildCommandParentAction(parent);
      setAvailableChildActionList(prepareAvailableChildCommandList(parent));
      setInternalSelectorOpen(true);
    },
    [
      childCommandParentAction,
      rootCommand,
      prepareAvailableChildCommandList,
      closeCommandTreeContextMenu,
      ensureWorkflowHasId,
    ],
  );

  const openExternalSelector = useCallback(
    async (parentAction?: any) => {
      setAddExistingDropdownOpen(false);
      closeCommandTreeContextMenu();
      const workflowId = await ensureWorkflowHasId();
      if (workflowId == null) return;
      const parent = parentAction ?? childCommandParentAction ?? rootCommand;
      setChildCommandParentAction(parent);
      setExternalTransactionId(null);
      setExternalCommandId(null);
      setExternalCommandList([]);
      setExternalSelectorOpen(true);
    },
    [childCommandParentAction, rootCommand, closeCommandTreeContextMenu, ensureWorkflowHasId],
  );

  const loadTransactionList = useCallback(async () => {
    try {
      const list = await appTransactionService.retrieveAllAppTransactions(false, '', true);
      setTransactionList(Array.isArray(list) ? list : []);
    } catch (_) {
      setTransactionList([]);
    }
  }, []);

  const loadExternalCommandList = useCallback(async (transactionId: string | number) => {
    try {
      const data = await appTransactionService.getOneHierarchyTransaction(String(transactionId), false, '', '', '', false, '');
      setExternalCommandList(data?.CommandActionList || []);
    } catch (_) {
      setExternalCommandList([]);
    }
  }, []);

  const applyInternalCommand = useCallback(
    (commandDto: any) => {
      const parent = childCommandParentAction ?? rootCommand;
      if (!commandDto || !parent) return;
      addChildAction(parent, commandDto);
      setChildCommandParentAction(null);
      setInternalSelectorOpen(false);
      syncTreeAndSave();
    },
    [childCommandParentAction, rootCommand, addChildAction, syncTreeAndSave],
  );

  const applyExternalCommand = useCallback(() => {
    const parent = childCommandParentAction ?? rootCommand;
    if (externalTransactionId == null || externalCommandId == null || !parent) return;
    const externalCommandDto = externalCommandList.find((c: any) => c.Id === externalCommandId || c.Id === Number(externalCommandId));
    if (!externalCommandDto) return;
    const childDto = addChildAction(parent, undefined, externalTransactionId);
    if (childDto) {
      childDto.CommandId = externalCommandDto.Id;
      childDto.ExternalTransactionId = externalTransactionId;
      childDto.CommandDisplay = 'External: ' + (externalCommandDto.Name || '');
    }
    setChildCommandParentAction(null);
    setExternalSelectorOpen(false);
    setExternalTransactionId(null);
    setExternalCommandId(null);
    setExternalCommandList([]);
    syncTreeAndSave();
  }, [externalTransactionId, externalCommandId, externalCommandList, childCommandParentAction, rootCommand, addChildAction, syncTreeAndSave]);

  const createNewChildAction = useCallback(
    async (parentAction: any) => {
      let trans = currentTransactionRef.current ?? currentTransaction;
      let parent = parentAction
        ? resolveWorkflowParentRoot(trans, parentAction, compositionCommandActionType) ?? parentAction
        : resolveWorkflowParentRoot(trans, rootCommand, compositionCommandActionType);
      if (!trans?.CommandActionList?.length || !parent) {
        showError('Workflow command is missing. Save the workflow or refresh the page.');
        return;
      }
      const workflowId = await ensureWorkflowHasId();
      if (workflowId == null || Number.isNaN(workflowId)) {
        return;
      }
      trans = currentTransactionRef.current ?? currentTransaction;
      parent = parentAction
        ? resolveWorkflowParentRoot(trans, parentAction, compositionCommandActionType) ?? parentAction
        : resolveWorkflowParentRoot(trans, rootCommand, compositionCommandActionType);
      if (!trans || !parent) {
        showError('Workflow command is missing. Refresh the page and try again.');
        return;
      }
      if (!parentAction || Number(parent?.Id) === Number(rootCommand?.Id)) {
        setRootCommand(parent);
      }
      closeCommandTreeContextMenu();
      const childList = parent.ActionAttribute?.ChildActionList || [];
      const maxSort = Math.max(0, ...childList.map((c: any) => c.Sort || 0));
      const newAction: any = {
        CommandTransactionId: workflowId,
        ActionGuid: 'guid-' + Math.random().toString(36).slice(2),
        ActionFlowOrder: maxSort + 1,
        Name: '_ChildCommand' + (maxSort + 1),
        NextTransactionId: null,
        ActionType: executeSqlCommandActionType,
        NotificationDestinationUserIdtransactionFiledId: null,
        NotificationDestinationRoleIdtransactionFiledId: null,
        DataLoadId: null,
        CommandConditionTransactionFieldId: null,
        ActionAttribute: {
          LinkToUI: false,
          IsLogCommandStartEnd: false,
          IsLogErrorDetails: true,
        },
      };
      dispatch(setIsBusy());
      try {
        const result = await appTransactionService.createOneTransactionCommand(newAction);
        if (!result?.IsSuccessful || !result?.Object) {
          showValidationMessages(result?.ValidationResult);
          return;
        }
        const commandExDto = result.Object;
        const { transaction: updatedTrans, root: updatedRoot, dictChild } = applyNewCommandToWorkflowTransaction(
          trans,
          parent,
          commandExDto,
        );
        if (parent === rootCommand || Number(parent?.Id) === Number(updatedRoot?.Id)) {
          setRootCommand(updatedRoot);
          setDictRootChildNodeLogicIdAndChildActionDto(dictChild);
        }
        setDictActionIdAndDto((prev) => ({ ...prev, [commandExDto.Id]: commandExDto }));
        setCurrentTransaction(updatedTrans);
        currentTransactionRef.current = updatedTrans;
        await save(updatedTrans, { reselectCommandId: commandExDto.Id });
      } catch (e: any) {
        showError(e?.message || 'Failed to create child task');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [
      currentTransaction,
      rootCommand,
      ensureWorkflowHasId,
      save,
      dispatch,
      showError,
      executeSqlCommandActionType,
      showValidationMessages,
      closeCommandTreeContextMenu,
      compositionCommandActionType,
    ],
  );

  const removeChildActionFromTree = useCallback(
    (treeNode: any) => {
      if (!treeNode) return;
      closeCommandTreeContextMenu();
      const parentCommandId = treeNode.ParentTreeNodeCommandId;
      const childCommandId = treeNode.Id;
      const childMeta = treeNode.TreeNodeLogicId ? dictRootChildNodeLogicIdAndChildActionDto[treeNode.TreeNodeLogicId] : null;
      const parent = dictActionIdAndDto[parentCommandId] ?? dictActionIdAndDto[Number(parentCommandId)];
      if (!parent?.ActionAttribute?.ChildActionList || childCommandId == null || !childMeta) return;
      const list = parent.ActionAttribute.ChildActionList;
      const idx = list.findIndex(
        (c: any) => Number(c.CommandId) === Number(childCommandId) && Number(c.Sort) === Number(childMeta.Sort),
      );
      if (idx < 0) return;
      list.splice(idx, 1);
      if (treeNode.TreeNodeLogicId === activeCommandTreeNodeLogicId) {
        setActiveCommandTreeNodeLogicId(null);
        setCurrentEditAction(null);
        setSelectedTreeRow(null);
        setCommandEditorHierarchy(null);
      }
      markChange();
      syncTreeAndSave();
    },
    [
      dictActionIdAndDto,
      dictRootChildNodeLogicIdAndChildActionDto,
      activeCommandTreeNodeLogicId,
      closeCommandTreeContextMenu,
      markChange,
      syncTreeAndSave,
    ],
  );

  const debugWorkflowRootChildCommand = useCallback(
    async (treeNode: any) => {
      if (!treeNode?.Id || !currentTransaction?.Id || !rootCommand) return;
      closeCommandTreeContextMenu();
      const debugKey = `Debug_${treeNode.Id}_${new Date().toISOString().slice(0, 19).replace('T', ' ')}`;
      rootCommand.ActionAttribute = rootCommand.ActionAttribute || { ChildActionList: [] };
      rootCommand.ActionAttribute.DebugKeyList = [debugKey];
      markChange();
      await save();
      dispatch(setIsBusy());
      try {
        const data = await appTransactionService.deubgWorkflowOneRootChildCommand(
          Number(currentTransaction.Id),
          Number(treeNode.Id),
          debugKey,
        );
        if (data?.ValidationResult) showValidationMessages(data.ValidationResult);
        if (data?.IsSuccessful) refresh();
      } catch (e: any) {
        showError(e?.message || 'Failed to run test');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [currentTransaction, rootCommand, closeCommandTreeContextMenu, markChange, save, refresh, dispatch, showError, showValidationMessages],
  );

  const editExternalDataModel = useCallback(
    (treeNode: any) => {
      const txId = treeNode?.ExternalTransactionId;
      if (txId == null) return;
      closeCommandTreeContextMenu();
      addTabAndNavigate('application-form-builder', 'Application Builder', { transactionId: Number(txId) }, true);
    },
    [addTabAndNavigate, closeCommandTreeContextMenu],
  );

  useEffect(() => {
    if (externalSelectorOpen && transactionList.length === 0) loadTransactionList();
  }, [externalSelectorOpen, transactionList.length, loadTransactionList]);

  useEffect(() => {
    if (externalSelectorOpen && externalTransactionId != null) loadExternalCommandList(externalTransactionId);
    else setExternalCommandList([]);
  }, [externalSelectorOpen, externalTransactionId, loadExternalCommandList]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (addExistingDropdownRef.current && !addExistingDropdownRef.current.contains(e.target as Node)) {
        setAddExistingDropdownOpen(false);
      }
      const inSchedulerBtn = windowsSchedulerButtonRef.current?.contains(e.target as Node);
      const inSchedulerMenu = windowsSchedulerMenuPortalRef.current?.contains(e.target as Node);
      if (!inSchedulerBtn && !inSchedulerMenu) {
        setWindowsSchedulerMenuOpen(false);
        setWindowsSchedulerMenuPos(null);
      }
    };
    if (addExistingDropdownOpen || windowsSchedulerMenuOpen) document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [addExistingDropdownOpen, windowsSchedulerMenuOpen]);

  const openWindowsSchedulerTaskCreatorPopup = useCallback(() => {
    const tid =
      currentTransaction?.Id ??
      workflowTransactionIdRef.current ??
      routeWorkflowTransactionId(parsed);
    if (tid == null) {
      showError('Workflow must be saved before creating a scheduler task.');
      return;
    }
    setWindowsSchedulerMenuOpen(false);
    setWindowsSchedulerMenuPos(null);
    setSchedulerTaskDto({
      TransactionId: Number(tid),
      TaskName: currentTransaction?.TransactionName ?? '',
      StartTime: '09:00',
      ScheduleType: 'DAILY',
      RepeatEvery: 1,
    });
    setWindowsSchedulerPopupOpen(true);
  }, [currentTransaction?.Id, currentTransaction?.TransactionName, parsed, showError]);

  const createWorkflowWindowsSchedulerTask = useCallback(async () => {
    if (!schedulerTaskDto?.TransactionId) return;
    dispatch(setIsBusy());
    try {
      const payload = {
        ...schedulerTaskDto,
        RepeatEvery:
          schedulerTaskDto.RepeatEvery != null && String(schedulerTaskDto.RepeatEvery).trim() !== ''
            ? String(schedulerTaskDto.RepeatEvery)
            : '',
      };
      const data = await appTransactionService.createWorkflowWindowsSchedulerTask(payload);
      if (data?.ValidationResult?.Items?.length) {
        showValidationMessages(data.ValidationResult, true);
      }
      if (data?.IsSuccessful) {
        setWindowsSchedulerPopupOpen(false);
        setSchedulerTaskDto(null);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to create Windows scheduler task');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [schedulerTaskDto, dispatch, showError, showValidationMessages]);

  /** One app: add immediately. Multiple: small popup — click application row to add (no extra Add step). */
  const handleAddToAppMenuClick = useCallback(() => {
    if (applicationList.length === 0) {
      showError('No applications available.');
      return;
    }
    if (applicationList.length === 1) {
      void addToAppMenuForApplication(applicationList[0].Id);
      return;
    }
    setAddToAppMenuPopupOpen(true);
  }, [applicationList, addToAppMenuForApplication, showError]);

  const handleClose = useCallback(() => {
    if (embedded?.onClose) {
      embedded.onClose();
      return;
    }
    const key = activeTabKey;
    if (key == null) {
      navigate('/home');
      return;
    }
    const remainingTabs = tabs.filter((t) => t.tabKey !== key);
    const prevActive = previousActiveTabKey
      ? remainingTabs.find((t) => t.tabKey === previousActiveTabKey)
      : undefined;
    const newPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(key));
    navigate(newPath);
  }, [activeTabKey, previousActiveTabKey, tabs, dispatch, navigate, embedded]);

  useEffect(() => {
    if (saveAsOpen && !applicationCV) {
      adminSvc.retrieveAllAppDataSourceRegisterExDto().then(() => {});
      // Load applications for Save As dropdown – use a simple list if available
      setApplicationCV(new CollectionView<any>([]));
    }
  }, [saveAsOpen]);

  if (!parsed) {
    return (
      <div className="p-4">
        Invalid or missing parameters. <button type="button" onClick={() => navigate('/home')} className="underline">Go home</button>
      </div>
    );
  }

  const title = parsed.modelName && String(parsed.modelName).trim() ? String(parsed.modelName) : 'Workflow Automation Editor';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header */}
      <div ref={editorHeaderRef} className={`flex items-center justify-between gap-3 px-3 py-2 mb-1 min-w-0 overflow-hidden ${theme.mainContentSection}`}>
        <div className="flex items-center gap-5 min-w-0 flex-auto overflow-hidden flex-nowrap">
          {/* <div className={`text-md font-semibold shrink-0 ${theme.title}`}>{title}</div> */}
          <div className="flex items-center gap-4 min-w-0 flex-auto overflow-hidden flex-nowrap">
            <div className="flex items-center gap-2 w-[400px] max-w-[500px] min-w-[12rem] shrink">
              <label className={`text-xs ${theme.label} shrink-0 whitespace-nowrap`}>Workflow Setting Name</label>
              <input
                type="text"
                autoComplete="off"
                value={currentTransaction?.TransactionName ?? ''}
                onChange={(e) => setCurrentTransaction((p: any) => (p ? { ...p, TransactionName: e.target.value, isModified: true } : p))}
                className={`w-1 min-w-0 flex-auto h-7 px-2 text-xs border box-border ${theme.inputBox} focus:outline-none`}
              />
            </div>
            <div className="flex items-center gap-2 w-[400px] max-w-[500px] min-w-[10rem] shrink">
              <label className={`text-xs ${theme.label} shrink-0 whitespace-nowrap`}>Description</label>
              <input
                type="text"
                autoComplete="off"
                value={currentTransaction?.Description ?? ''}
                onChange={(e) => setCurrentTransaction((p: any) => (p ? { ...p, Description: e.target.value, isModified: true } : p))}
                className={`w-1 min-w-0 flex-auto h-7 px-2 text-xs border box-border ${theme.inputBox} focus:outline-none`}
              />
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0 flex-nowrap justify-end">
          <button type="button" onClick={refresh} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={() => void save()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            {currentTransaction?.isModified ? ' * ' : ''}
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
          </button>
          <button type="button" onClick={() => setSaveAsOpen(true)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-copy mr-1" aria-hidden /> Save As
          </button>
          {!currentTransaction?.DefaultNavigationMenuId && (
            <button
              type="button"
              onClick={handleAddToAppMenuClick}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            >
              <i className="fa-solid fa-list-ul mr-1" aria-hidden /> Add To App Menu
            </button>
          )}
          {resolveWorkflowId() != null ? (
            <>
              <button
                ref={windowsSchedulerButtonRef}
                type="button"
                onClick={() => {
                  if (windowsSchedulerMenuOpen) {
                    setWindowsSchedulerMenuOpen(false);
                    setWindowsSchedulerMenuPos(null);
                    return;
                  }
                  const btn = windowsSchedulerButtonRef.current;
                  if (btn) {
                    const r = btn.getBoundingClientRect();
                    setWindowsSchedulerMenuPos({ top: r.bottom + 4, left: r.right });
                  }
                  setWindowsSchedulerMenuOpen(true);
                }}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              >
                <i className="fa-solid fa-clock mr-1" aria-hidden /> Windows Scheduler <span className="caret" />
              </button>
              {windowsSchedulerMenuOpen &&
                windowsSchedulerMenuPos &&
                typeof document !== 'undefined' &&
                createPortal(
                  <ul
                    ref={windowsSchedulerMenuPortalRef}
                    className={`fixed z-[10050] min-w-[200px] overflow-hidden rounded border py-1 shadow-xl ${theme.mainContentSection} ${t('border_mainContentSection')}`}
                    style={{
                      top: windowsSchedulerMenuPos.top,
                      left: windowsSchedulerMenuPos.left,
                      transform: 'translateX(-100%)',
                    }}
                    role="menu"
                    onMouseDown={(e) => e.stopPropagation()}
                  >
                    <li role="none">
                      <button
                        type="button"
                        role="menuitem"
                        className={`w-full min-w-0 px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                        onClick={openWindowsSchedulerTaskCreatorPopup}
                      >
                        Create Task
                      </button>
                    </li>
                  </ul>,
                  document.body,
                )}
            </>
          ) : null}
          
          <button
            type="button"
            onClick={() => {
              if (!showAgentPanel && editorHeaderRef.current) {
                setAgentPanelTop(editorHeaderRef.current.getBoundingClientRect().bottom);
              }
              setShowAgentPanel((v) => !v);
            }}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-robot mr-1" aria-hidden /> AI
          </button>
          {/* <button type="button" onClick={handleClose} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-xmark mr-1" aria-hidden /> Close
          </button> */}
        </div>
      </div>

      {/* Workflow Parameters (collapsible) – Angular WorkflowAutomationEditor.cshtml */}
      <div className={`px-3 py-2 ${theme.mainContentSection}`}>
        <div
          role="button"
          tabIndex={0}
          className={`flex items-center justify-between gap-2 py-1.5 min-h-[30px] cursor-pointer select-none border border-gray-200 rounded-t ${theme.mainContentSection}`}
          onClick={() => setIsCollapseSessionVariables((v) => !v)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              setIsCollapseSessionVariables((v) => !v);
            }
          }}
        >
          <div className={`flex items-center px-2 gap-2 text-sm font-semibold ${theme.title}`}>
            {isCollapseSessionVariables ? (
              <i className="fa-solid fa-chevron-circle-down" aria-hidden />
            ) : (
              <i className="fa-solid fa-chevron-circle-up" aria-hidden />
            )}
            Workflow Parameters
          </div>
          <div className="flex items-center gap-1 shrink-0 flex-nowrap">
            {isCollapseSessionVariables ? (
              /* Angular: collapsed — single "Config Parameters" label + chevron (header click toggles) */
              <span className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                <span className="pr-1">Config Parameters</span>
                <i className="fa-solid fa-chevron-circle-down" aria-hidden />
              </span>
            ) : (
              <>
                <div className="flex items-center gap-1 flex-nowrap" onClick={(e) => e.stopPropagation()}>
                  {showAddBuiltInField ? (
                    <button
                      type="button"
                      onClick={handleAddExistingTransactionField}
                      className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                    >
                      <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Built-in Field
                    </button>
                  ) : null}
                  {showAddTempAndRemoveField ? (
                    <button
                      type="button"
                      onClick={handleAddTempTransactionField}
                      className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                    >
                      <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Temp Field
                    </button>
                  ) : null}
                  {showAddTempAndRemoveField ? (
                    <button
                      type="button"
                      onClick={handleRemoveTransactionField}
                      className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                    >
                      <i className="fa-solid fa-trash mr-1" aria-hidden /> Remove Field
                    </button>
                  ) : null}
                  <button
                    type="button"
                    onClick={() => void handleResetDefaultUnit()}
                    className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                  >
                    <i className="fa-solid fa-recycle mr-1" aria-hidden /> Test Reset Default Unit
                  </button>
                </div>
                {/* Angular: expanded — chevron-only control at end (no stopPropagation; header toggles) */}
                <span className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                  <i className="fa-solid fa-chevron-circle-up" aria-hidden />
                </span>
              </>
            )}
          </div>
        </div>
        {!isCollapseSessionVariables && transactionFieldCV && (
          <div className="border border-t-0 border-gray-200 rounded-b overflow-hidden" style={{ height: 300 }}>
            <FlexGrid
              ref={paramsGridRef}
              itemsSource={transactionFieldCV}
              headersVisibility="Column"
              selectionMode="ListBox"
              isReadOnly={false}
              allowSorting={false}
              selectionChanged={handleParamsGridSelectionChanged}
              cellEditEnded={() => markChange('workflow')}
              style={{ height: '100%', width: '100%', border: 'none' }}
              className="h-full w-full"
            >
              <FlexGridFilter />
              <FlexGridCellTemplate
                cellType="RowHeader"
                template={(cell: any) => {
                  const item = cell.item;
                  const rowIndex = cell.row?.index;
                  const isSelected = typeof rowIndex === 'number' && paramsSelectedRowIndexes.includes(rowIndex);
                  const isEditable = isRemovableWorkflowField(item);
                  return (
                    <div className="w-full h-full flex items-center justify-center px-1">
                      {isEditable ? (
                        isSelected ? <i className="fa-solid fa-check text-[#808080]" aria-hidden /> : null
                      ) : isSelected ? (
                        <i className="fa-solid fa-lock text-[#ccc]" aria-hidden />
                      ) : null}
                    </div>
                  );
                }}
              />
              <FlexGridColumn binding="DisplayName" header="Name" width={200} allowSorting={false} />
              <FlexGridColumn binding="DefaultValue" header="DefaultValue" width={400} allowSorting={false} />
              <FlexGridColumn binding="IsLogicalDisplay" header="Mapping To Batch Log Description" width={300} dataType="Boolean" allowSorting={false} />
              <FlexGridColumn binding="IsTempVariable" header="Is Temp Variable" width={120} dataType="Boolean" isReadOnly={true} allowSorting={false} />
              <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
            </FlexGrid>
          </div>
        )}
      </div>

      {tableColumnSelectorOpen && currentTransactionUnit?.DataBaseTableName ? (
        <TableColumnSelectorDialog
          isOpen={tableColumnSelectorOpen}
          tableName={currentTransactionUnit.DataBaseTableName}
          schemaOwner={currentTransactionUnit.SchemaOwner ?? null}
          dataSourceRegisterId={currentTransaction?.DataSourceFrom ?? (defaultDataSourceId ? Number(defaultDataSourceId) : null)}
          lockedColumnNames={workflowParamsLockedColumnNames}
          onClose={() => setTableColumnSelectorOpen(false)}
          onSelect={(cols) => void handleWorkflowTableColumnsSelected(cols)}
        />
      ) : null}

      {/* Main: Operation Tasks (left) + Current Edit (center) + AI Panel (right, optional) */}
      <div className={`w-full h-1 flex-auto flex gap-1 px-3 py-3 overflow-hidden min-w-0 ${theme.mainContentSection}`}>
        <div className="w-[800px] min-w-[280px] shrink-0 flex flex-col border border-gray-200 rounded overflow-hidden">
          <div className={`flex items-center justify-between px-2 py-1.5 ${theme.mainContentSection}`}>
            <span className={`text-sm font-semibold ${theme.title}`}>Operation Tasks</span>
            <div className="flex items-center gap-1">
              <button type="button" onClick={collapseAll} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-caret-right mr-1" aria-hidden /> Collapse All
              </button>
              <button type="button" onClick={expandAll} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-caret-down mr-1" aria-hidden /> Expand All
              </button>
              <div className="relative inline-block" ref={addExistingDropdownRef}>
                <button
                  type="button"
                  onClick={() => setAddExistingDropdownOpen((v) => !v)}
                  className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-plus mr-1" aria-hidden /> Add Existing Root Task <span className="caret" />
                </button>
                {addExistingDropdownOpen && (
                  <ul className={`absolute left-0 top-full mt-1 py-1 min-w-[200px] rounded shadow z-50 ${theme.mainContentSection} border border-gray-200`}>
                    <li>
                      <button
                        type="button"
                        className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100"
                        onClick={() => openInternalSelector(rootCommand)}
                      >
                        From Current Data Model
                      </button>
                    </li>
                    <li>
                      <button
                        type="button"
                        className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100"
                        onClick={() => openExternalSelector(rootCommand)}
                      >
                        From External Data Model
                      </button>
                    </li>
                  </ul>
                )}
              </div>
              <button type="button" onClick={createNewRootTask} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-plus mr-1" aria-hidden /> Create New Root Task
              </button>
            </div>
          </div>
          <div className="h-1 min-h-0 flex-auto pl-5 overflow-hidden">
            <FlexGrid
              key={`workflow-command-tree-${commandTreeRevision}`}
              ref={commandGridRef}
              itemsSource={commandNodeTreeCV ?? undefined}
              childItemsPath="WorkflowChildTreeNodes"
              headersVisibility="Column"
              selectionMode="Row"
              allowSorting={false}
              initialized={handleCommandGridInitialized}
              formatItem={handleCommandTreeFormatItem}
              style={{ height: '100%' }}
            >
              <FlexGridCellTemplate
                cellType="RowHeader"
                template={(cell: any) => {
                  const item = cell.item;
                  if (item?.TreeNodeLogicId !== activeCommandTreeNodeLogicId) return null;
                  return (
                    <div className="w-full h-full flex items-center justify-center px-1">
                      <i className="fa-solid fa-pencil text-[#808080] text-[15px]" aria-hidden />
                    </div>
                  );
                }}
              />
              <FlexGridColumn binding="DisplayName" header="Name" width={200} isReadOnly={true} allowSorting={false} />
              <FlexGridColumn header="" binding="" width={66} isReadOnly={true} allowSorting={false}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    const parentId = item?.ParentTreeNodeCommandId;
                    const showContextMenu =
                      parentId != null && !!dictActionIdAndDto[parentId];
                    return (
                      <div className="w-full h-full flex items-center gap-1 px-1">
                        <button
                          type="button"
                          className={`w-7 h-6 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
                          title="Edit Command"
                          onClick={(e) => {
                            e.stopPropagation();
                            openCommandTreeNodeEditor(item, true);
                          }}
                        >
                          <i className="fa-solid fa-pencil text-xs" aria-hidden />
                        </button>
                        {showContextMenu ? (
                          <button
                            type="button"
                            className={`w-7 h-6 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
                            title="Open Context Menu"
                            onClick={(e) => openCommandTreeNodeContextMenu(e, item)}
                          >
                            <i className="fa-solid fa-bars text-xs" aria-hidden />
                          </button>
                        ) : null}
                      </div>
                    );
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn header="Sort" width={66} isReadOnly={false} allowSorting={false}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    const childDto = item?.TreeNodeLogicId ? dictRootChildNodeLogicIdAndChildActionDto[item.TreeNodeLogicId] : null;
                    return <div className="text-center py-1">{childDto?.Sort ?? ''}</div>;
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn binding="TransactionName" header="Data Model" width={150} isReadOnly={true} allowSorting={false} />
              <FlexGridColumn header="" width={290} isReadOnly={false} allowSorting={false}>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    const childDto = item?.TreeNodeLogicId ? dictRootChildNodeLogicIdAndChildActionDto[item.TreeNodeLogicId] : null;
                    if (!childDto) return null;
                    return (
                      <div className="flex items-center gap-4 px-2 text-xs">
                        <label className="flex items-center gap-1">
                          <input
                            type="checkbox"
                            checked={!!childDto.IsSkip}
                            onChange={(e) => {
                              childDto.IsSkip = e.target.checked;
                              markChange();
                            }}
                          />
                          Force Skip
                        </label>
                        <label className="flex items-center gap-1" title="Continue To Run Next Command With Error">
                          <input
                            type="checkbox"
                            checked={!!childDto.IsGoToNextCommandWithError}
                            onChange={(e) => {
                              childDto.IsGoToNextCommandWithError = e.target.checked;
                              markChange();
                            }}
                          />
                          Continue To Next With Error
                        </label>
                      </div>
                    );
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
            </FlexGrid>
          </div>
        </div>

        <div className="w-1 min-w-0 flex-auto min-h-0 h-full flex flex-col overflow-hidden">
          {commandEditorLoading ? (
            <div className={`h-1 flex-auto flex items-center justify-center text-sm ${theme.label}`}>Loading command editor…</div>
          ) : (
            <CommandEditor
              key={
                currentEditAction?.Id != null
                  ? `wf-cmd-${currentEditAction.Id}-${selectedTreeRow?.ExternalTransactionId ?? 'internal'}-r${commandEditorRevision}`
                  : 'wf-cmd-none'
              }
              renderRevision={
                selectedTreeRow?.ExternalTransactionId != null ? externalCommandEditorTick : undefined
              }
              hostContext="workflowAutomation"
              transactionId={commandEditorHierarchy?.Id ?? currentTransaction?.Id ?? null}
              applicationId={commandEditorHierarchy?.SaasApplicationId ?? currentTransaction?.SaasApplicationId ?? null}
              hierarchy={commandEditorHierarchy ?? currentTransaction}
              currentEditAction={currentEditAction}
              operationTypeOptions={operationTypeOptions}
              rootLevelTransFieldLookUpList={fieldLookups?.rootLevelTransFieldLookUpList ?? []}
              rootLevelConditionTransFieldLookUpList={fieldLookups?.rootLevelConditionTransFieldLookUpList ?? []}
              rootLevelSwitchConditionTransFieldLookUpList={fieldLookups?.rootLevelSwitchConditionTransFieldLookUpList ?? []}
              transactionFieldLookUpList={fieldLookups?.transactionFieldLookUpList ?? []}
              rootLevelAllFieldLookUpList={fieldLookups?.rootLevelAllFieldLookUpList ?? []}
              globalTransFieldLookUpList={fieldLookups?.globalTransFieldLookUpList ?? []}
              rootWorkflowTransactionData={currentTransaction}
              onMarkChange={() =>
                markChange(selectedTreeRow?.ExternalTransactionId != null ? 'external' : 'workflow')
              }
              onRefreshGridCell={refreshCommandTreeCell}
              onFlowOrderChanged={() => void 0}
            />
          )}
        </div>

      </div>

      {/* Workflow AI Agent floating popup */}
      {showAgentPanel && currentTransaction?.Id && agentPanelTop > 0 && (
        <WorkflowAIFloatingPanel
          transactionId={currentTransaction.Id}
          onWorkflowChanged={refresh}
          onClose={() => setShowAgentPanel(false)}
          initialTop={agentPanelTop}
        />
      )}

      {/* Internal: From Current Data Model – select existing command to add as root task */}
      {internalSelectorOpen && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setInternalSelectorOpen(false)}>
          <div
            className={`rounded shadow-lg p-4 w-[480px] max-w-[calc(100vw-2rem)] min-w-0 max-h-[80vh] flex flex-col overflow-hidden ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-2">
              <span className={`text-md font-semibold ${theme.title}`}>Available Child Commands</span>
              <button type="button" onClick={() => setInternalSelectorOpen(false)} className="text-lg leading-none">&times;</button>
            </div>
            <p className="text-xs text-gray-500 mb-2">Select a command from the current workflow to add as a root task.</p>
            <div className="h-1 min-h-0 flex-auto overflow-auto border border-gray-200 rounded mb-3">
              {availableChildActionList.length === 0 ? (
                <p className="p-3 text-sm text-gray-500">No other commands in this workflow. Create a new root task or add from external data model.</p>
              ) : (
                <ul className="py-1">
                  {availableChildActionList.map((cmd: any) => (
                    <li key={cmd.Id}>
                      <button
                        type="button"
                        className="w-full text-left px-3 py-2 text-sm hover:bg-gray-100 border-b border-gray-100 last:border-0"
                        onClick={() => applyInternalCommand(cmd)}
                      >
                        {cmd.displayName || cmd.Name || cmd.Id}
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div className="flex justify-end">
              <button type="button" onClick={() => setInternalSelectorOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
            </div>
          </div>
        </div>
      )}

      {/* External: From External Data Model – pick transaction + command */}
      {externalSelectorOpen && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setExternalSelectorOpen(false)}>
          <div
            className={`rounded shadow-lg p-4 w-[400px] max-w-[calc(100vw-2rem)] min-w-0 overflow-hidden ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3 gap-2 min-w-0">
              <span className={`text-md font-semibold ${theme.title} min-w-0`}>Select Command From External Data Model</span>
              <button type="button" onClick={() => setExternalSelectorOpen(false)} className="text-lg leading-none shrink-0">&times;</button>
            </div>
            <div className="grid grid-cols-[9rem_minmax(0,1fr)] items-center gap-x-2 gap-y-3 mb-4 min-w-0">
              <label className={`text-xs ${theme.label}`}>Data Model</label>
              <select
                value={externalTransactionId ?? ''}
                onChange={(e) => setExternalTransactionId(e.target.value ? Number(e.target.value) : null)}
                className={`w-full min-w-0 max-w-full h-7 px-2 text-xs border box-border ${theme.inputBox} focus:outline-none`}
              >
                <option value="">-- Select --</option>
                {transactionList.map((t: any) => (
                  <option key={t.Id} value={t.Id}>{t.TransactionName ?? t.Name ?? t.Id}</option>
                ))}
              </select>
              <label className={`text-xs ${theme.label}`}>Command</label>
              <select
                value={externalCommandId ?? ''}
                onChange={(e) => setExternalCommandId(e.target.value ? Number(e.target.value) : null)}
                className={`w-full min-w-0 max-w-full h-7 px-2 text-xs border box-border ${theme.inputBox} focus:outline-none`}
                disabled={!externalTransactionId}
              >
                <option value="">-- Select --</option>
                {externalCommandList.map((c: any) => (
                  <option key={c.Id} value={c.Id}>{c.Name ?? c.Id}</option>
                ))}
              </select>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setExternalSelectorOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
              <button type="button" onClick={applyExternalCommand} disabled={!externalTransactionId || !externalCommandId} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Apply</button>
            </div>
          </div>
        </div>
      )}

      {/* Operation task tree context menu — Angular CommandTreeNodeContextMenu */}
      {commandTreeContextMenu.visible && commandTreeContextMenu.item ? (
        <div
          className={`fixed z-[10050] ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[280px]`}
          style={{ left: commandTreeContextMenu.x, top: commandTreeContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
          role="menu"
        >
          {!commandTreeContextMenu.item.ExternalTransactionId &&
          Number(commandTreeContextMenu.item.ActionType) === compositionCommandActionType ? (
            <>
              <button
                type="button"
                role="menuitem"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => {
                  const cmd = dictActionIdAndDto[commandTreeContextMenu.item.Id];
                  if (cmd) openInternalSelector(cmd);
                }}
              >
                <i className="fa-solid fa-plus mr-2 flex-shrink-0" aria-hidden />
                Add Child Task From Current Workflow
              </button>
              <button
                type="button"
                role="menuitem"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => {
                  const cmd = dictActionIdAndDto[commandTreeContextMenu.item.Id];
                  if (cmd) openExternalSelector(cmd);
                }}
              >
                <i className="fa-solid fa-plus mr-2 flex-shrink-0" aria-hidden />
                Add Child Task From External Workflow / Data Model
              </button>
              <button
                type="button"
                role="menuitem"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                onClick={() => {
                  const cmd = dictActionIdAndDto[commandTreeContextMenu.item.Id];
                  if (cmd) void createNewChildAction(cmd);
                }}
              >
                <i className="fa-solid fa-plus mr-2 flex-shrink-0" aria-hidden />
                Create New Child Task
              </button>
            </>
          ) : null}
          {commandTreeContextMenu.item.ExternalTransactionId ? (
            <button
              type="button"
              role="menuitem"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => editExternalDataModel(commandTreeContextMenu.item)}
            >
              <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden />
              Edit Data Model
            </button>
          ) : null}
          {rootCommand && Number(commandTreeContextMenu.item.ParentTreeNodeCommandId) === Number(rootCommand.Id) ? (
            <button
              type="button"
              role="menuitem"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => void debugWorkflowRootChildCommand(commandTreeContextMenu.item)}
            >
              <i className="fa-solid fa-bolt mr-2 flex-shrink-0" aria-hidden />
              Test Run This Command
            </button>
          ) : null}
          {commandTreeContextMenu.item.ParentTreeNodeCommandId != null &&
          dictActionIdAndDto[commandTreeContextMenu.item.ParentTreeNodeCommandId] ? (
            <button
              type="button"
              role="menuitem"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
              onClick={() => removeChildActionFromTree(commandTreeContextMenu.item)}
            >
              <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />
              Remove
            </button>
          ) : null}
        </div>
      ) : null}

      {/* Add To App Menu – portal popup (not clipped by editor overflow / embedded frame) */}
      {addToAppMenuPopupOpen &&
        typeof document !== 'undefined' &&
        createPortal(
          <div
            className="fixed inset-0 flex items-center justify-center bg-black/40 px-4"
            style={{ zIndex: 10050 }}
            role="dialog"
            aria-modal="true"
            aria-labelledby="workflow-add-to-app-menu-title"
            onClick={() => setAddToAppMenuPopupOpen(false)}
          >
            <div
              className={`flex w-full max-w-[440px] flex-col overflow-hidden rounded-md border shadow-2xl ${theme.mainContentSection} ${t('border_mainContentSection')}`}
              onClick={(e) => e.stopPropagation()}
              onMouseDown={(e) => e.stopPropagation()}
            >
              <div
                className={`flex shrink-0 items-center gap-2 border-b px-3 py-2.5 ${theme.mainContentSection} ${t('border_mainContentSection')}`}
              >
                <i className={`fa-solid fa-list-ul shrink-0 ${theme.title}`} aria-hidden />
                <span id="workflow-add-to-app-menu-title" className={`min-w-0 flex-auto text-sm font-semibold ${theme.title}`}>
                  Add To App Menu
                </span>
                <button
                  type="button"
                  className={`shrink-0 rounded-[4px] p-1.5 ${theme.button_default}`}
                  onClick={() => setAddToAppMenuPopupOpen(false)}
                  title="Close"
                  aria-label="Close"
                >
                  <i className="fa-solid fa-xmark" aria-hidden />
                </button>
              </div>

              <div className="flex min-h-0 flex-col gap-3 px-3 py-3">
                <p className={`text-xs leading-relaxed ${theme.label}`}>
                  Select an application. This workflow will be added to that application&apos;s navigation menu.
                </p>
                <div
                  className={`overflow-hidden rounded-md border ${theme.mainContentSection} ${t('border_mainContentSection')}`}
                >
                  <ul className="max-h-[min(320px,50vh)] overflow-y-auto" role="listbox" aria-label="Applications">
                    {applicationList.map((app: any, index: number) => (
                      <li
                        key={String(app.Id)}
                        role="option"
                        className={index > 0 ? `border-t ${t('border_mainContentSection')}` : undefined}
                      >
                        <button
                          type="button"
                          className={`flex w-full min-w-0 items-center gap-3 px-3 py-2.5 text-left text-sm ${theme.contextMenu}`}
                          onClick={() => void addToAppMenuForApplication(app.Id)}
                        >
                          <span
                            className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-md border ${theme.mainContentSection} ${t('border_mainContentSection')}`}
                            aria-hidden
                          >
                            <i className={`fa-solid fa-cube text-xs ${theme.title}`} />
                          </span>
                          <span className={`min-w-0 flex-auto truncate font-medium ${theme.title}`}>{app.Name}</span>
                          <i className="fa-solid fa-chevron-right shrink-0 text-[10px] opacity-40" aria-hidden />
                        </button>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>

              <div
                className={`flex shrink-0 items-center justify-end border-t px-3 py-2 ${theme.mainContentSection} ${t('border_mainContentSection')}`}
              >
                <button
                  type="button"
                  className={`rounded-[4px] px-3 py-1.5 text-sm ${theme.button_default}`}
                  onClick={() => setAddToAppMenuPopupOpen(false)}
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>,
          document.body,
        )}

      <WorkflowWindowsSchedulerTaskPopup
        isOpen={windowsSchedulerPopupOpen}
        task={schedulerTaskDto}
        theme={theme}
        borderClass={t('border_mainContentSection')}
        onClose={() => {
          setWindowsSchedulerPopupOpen(false);
          setSchedulerTaskDto(null);
        }}
        onChange={(patch) => setSchedulerTaskDto((prev) => (prev ? { ...prev, ...patch } : prev))}
        onSave={() => void createWorkflowWindowsSchedulerTask()}
      />

      {/* Save As modal */}
      {saveAsOpen && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50" onClick={() => setSaveAsOpen(false)}>
          <div
            className={`rounded shadow-lg p-4 w-[400px] max-w-[calc(100vw-2rem)] min-w-0 overflow-hidden ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-3">
              <span className={`text-md font-semibold ${theme.title}`}>Save As Setting</span>
              <button type="button" onClick={() => setSaveAsOpen(false)} className="text-lg leading-none">&times;</button>
            </div>
            <div className="space-y-3 mb-4 min-w-0">
              <div className="min-w-0">
                <label className={`block text-xs mb-1 ${theme.label}`}>New Workflow Name</label>
                <input
                  type="text"
                  value={newWorkflowName}
                  onChange={(e) => setNewWorkflowName(e.target.value)}
                  className={`w-full min-w-0 max-w-full h-7 px-2 text-xs border box-border ${theme.inputBox}`}
                />
              </div>
              <div className="min-w-0">
                <label className={`block text-xs mb-1 ${theme.label}`}>Target Application</label>
                <input
                  type="text"
                  value={newApplicationId ?? ''}
                  onChange={(e) => setNewApplicationId(e.target.value || null)}
                  className={`w-full min-w-0 max-w-full h-7 px-2 text-xs border box-border ${theme.inputBox}`}
                  placeholder="Application ID"
                />
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button type="button" onClick={() => setSaveAsOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Cancel</button>
              <button type="button" onClick={handleSaveAs} disabled={!newWorkflowName || !newApplicationId} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>Ok</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default WorkflowAutomationEditor;
