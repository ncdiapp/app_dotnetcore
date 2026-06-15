import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { GroupPanel } from '@mescius/wijmo.react.grid.grouppanel';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import { CellRange, DataMap, GroupRow } from '@mescius/wijmo.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { DataType } from '@mescius/wijmo';
import type { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import { appMessageService } from '../../webapi/appmessagesvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import MessageDisplayPanel from './MessageDisplayPanel';
import MessageTemplateManagement from './MessageTemplateManagement';
import {
  MESSAGE_SCOPE_SIDEBAR_FULL,
  MESSAGE_SCOPE_SIDEBAR_BUSINESS_PARTNER,
  MESSAGE_TEMPLATE_SCOPE_TYPE,
} from './messageScopeConstants';
import { requestUnreadMessageCountRefresh } from './messageUnreadRefresh';

const COMPANY_PUBLIC_SCOPE = 10;

function normalizeMessageDates(items: any[]) {
  for (const m of items) {
    const raw = m.AppCreatedDate ?? m.appCreatedDate;
    if (raw != null && !(raw instanceof Date)) {
      const d = new Date(raw);
      if (!isNaN(d.getTime())) m.AppCreatedDate = d;
    }
  }
}

function normalizeMessageReadFlags(items: any[]) {
  for (const m of items) {
    // Backend sometimes returns `isRead` instead of `IsRead`
    if (m.IsRead === undefined && m.isRead !== undefined) m.IsRead = m.isRead;
  }
}

function isUnreadFlag(v: any): boolean {
  // Angular parity: `if (!message.IsRead)` => treat null/undefined as unread.
  if (v == null) return true;
  if (v === false || v === 0) return true;
  if (typeof v === 'string') {
    const s = v.trim().toLowerCase();
    if (s === 'false' || s === '0' || s === 'no') return true;
    if (s === 'true' || s === '1' || s === 'yes') return false;
  }
  return false;
}

function findFirstMessageRowIndex(flex: any): number {
  for (let i = 0; i < flex.rows.length; i++) {
    const row = flex.rows[i];
    if (row instanceof GroupRow) continue;
    const di = row?.dataItem;
    if (di != null && (di.Id != null || di.id != null)) return i;
  }
  return -1;
}

function parseRouteParam(param: string | undefined): {
  transactionId: string;
  transctionRid: string;
  messageScopeType: number | null;
} {
  if (!param) return { transactionId: '', transctionRid: '', messageScopeType: null };
  try {
    const o = JSON.parse(decodeURIComponent(param));
    return {
      transactionId: o.transactionId != null ? String(o.transactionId) : '',
      transctionRid: o.transctionRid != null ? String(o.transctionRid) : '',
      messageScopeType: o.messageScopeType != null ? Number(o.messageScopeType) : null,
    };
  } catch {
    return { transactionId: '', transctionRid: '', messageScopeType: null };
  }
}

const MessageManagement: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const { addTabAndNavigate } = useTabNavigation();
  const { showValidationMessages } = useErrorMessage();
  const userContext = useSelector((s: RootState) => s.userSession.userContext);
  const emScope = useEnumValues('EmAppMessgaeScopeType');
  const emPost = useEnumValues('EmAppMessgaePostType');

  const routeCtx = useMemo(() => parseRouteParam(param), [param]);

  const [transactionId] = useState(() => routeCtx.transactionId);
  const [transctionRid] = useState(() => routeCtx.transctionRid);

  const defaultScope = useMemo(() => {
    if (routeCtx.messageScopeType != null) return routeCtx.messageScopeType;
    return emScope?.Global ?? emScope?.global ?? 1;
  }, [routeCtx.messageScopeType, emScope]);

  const [currentScope, setCurrentScope] = useState<number>(defaultScope);
  const [currentBoxType, setCurrentBoxType] = useState<'Inbox' | 'Sent' | 'Deleted' | 'Draft'>('Inbox');
  const [messages, setMessages] = useState<any[]>([]);
  const [messageCV, setMessageCV] = useState(() => new CollectionView<any>([]));
  const messageCvRef = useRef(messageCV);
  const [flexGridControl, setFlexGridControl] = useState<any>(null);
  const [isSelectAllRows, setIsSelectAllRows] = useState(false);
  const [isMessageDetailVisible, setIsMessageDetailVisible] = useState(false);
  const [currentMessage, setCurrentMessage] = useState<any | null>(null);
  const [userdataMap, setUserdataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const [workflowDataMap, setWorkflowDataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const [taskDataMap, setTaskDataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const [scopeTypeDataMap, setScopeTypeDataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const [postTypeDataMap, setPostTypeDataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const [moreOpen, setMoreOpen] = useState(false);

  useEffect(() => {
    // Keep a stable ref for async callbacks and one-time DOM event handlers.
    messageCvRef.current = messageCV;
  }, [messageCV]);

  const flexRef = useRef<any>(null);
  const fetchBoxRef = useRef<((box: 'Inbox' | 'Sent' | 'Deleted' | 'Draft', scope: number) => Promise<void>) | null>(null);
  const [listWidthPx, setListWidthPx] = useState<number>(520);
  const isResizingRef = useRef(false);
  const themeParamRef = useRef<any>((theme as any)?.param ?? null);
  const boxTypeRef = useRef<typeof currentBoxType>('Inbox');

  const showErrorsOnly = useCallback(
    (validationResult: any) => {
      const items = validationResult?.Items ?? validationResult?.items;
      if (!Array.isArray(items) || items.length === 0) return;
      const errorItems = items.filter((it: any) => Number(it?.ItemType ?? it?.itemType) === 1);
      if (errorItems.length === 0) return;
      showValidationMessages({ ...validationResult, Items: errorItems }, true);
    },
    [showValidationMessages]
  );

  const isBusinessPartner = !!(userContext && (userContext as any).BusinessUserId);
  const sidebarScopes = useMemo(
    () => (isBusinessPartner ? MESSAGE_SCOPE_SIDEBAR_BUSINESS_PARTNER : MESSAGE_SCOPE_SIDEBAR_FULL).slice().sort((a, b) => a.SortOrder - b.SortOrder),
    [isBusinessPartner]
  );

  const currentScopeLabel = useMemo(
    () => sidebarScopes.find((s) => s.Id === currentScope)?.Display ?? 'Messages',
    [sidebarScopes, currentScope]
  );

  useEffect(() => {
    messageCvRef.current = messageCV;
  }, [messageCV]);

  useEffect(() => {
    // FlexGrid event handlers may keep the first render's closure;
    // use a ref so grid formatting always reads the latest theme tokens.
    themeParamRef.current = (theme as any)?.param ?? null;
    const flex = flexRef.current?.control ?? flexRef.current;
    if (flex?.invalidate) flex.invalidate();
  }, [theme]);

  useEffect(() => {
    // Same issue for runtime state: keep current box type in a ref for FlexGrid callbacks.
    boxTypeRef.current = currentBoxType;
    const flex = flexRef.current?.control ?? flexRef.current;
    if (flex?.invalidate) flex.invalidate();
  }, [currentBoxType]);

  useEffect(() => {
    const scopeList: { Id: number; Display: string }[] = [];
    if (emScope) {
      for (const k of Object.keys(emScope)) {
        scopeList.push({ Id: (emScope as any)[k], Display: k });
      }
    }
    if (scopeList.length) setScopeTypeDataMap(new DataMap(scopeList, 'Id', 'Display'));
    const postList: { Id: number; Display: string }[] = [];
    if (emPost) {
      for (const k of Object.keys(emPost)) {
        postList.push({ Id: (emPost as any)[k], Display: k });
      }
    }
    if (postList.length) setPostTypeDataMap(new DataMap(postList, 'Id', 'Display'));
  }, [emScope, emPost]);

  const rebuildCv = useCallback(
    (data: any[], box: typeof currentBoxType, scope: number) => {
      normalizeMessageDates(data);
      normalizeMessageReadFlags(data);
      const cv = new CollectionView<any>(data);
      cv.groupDescriptions.clear();
      if (scope === (emScope?.Workflow ?? emScope?.workflow ?? 2)) {
        cv.groupDescriptions.push(new PropertyGroupDescription('ProjectId'));
        cv.groupDescriptions.push(new PropertyGroupDescription('ProjectActivityId'));
      }
      setMessageCV(cv);
      setMessages(data);
      dispatch(updateCurrentTabLabel(`Message ${box}`));
      return cv;
    },
    [dispatch, emScope]
  );

  const loadLookups = useCallback(async () => {
    const data = await adminSvc.getMassEntitiesLookupItem('AppSecurityUser|AppWorkFlow|AppProjectWorkFlowTask');
    setUserdataMap(new DataMap(data?.AppSecurityUser || [], 'Id', 'Display'));
    setWorkflowDataMap(new DataMap(data?.AppWorkFlow || [], 'Id', 'Display'));
    setTaskDataMap(new DataMap(data?.AppProjectWorkFlowTask || [], 'Id', 'Display'));
  }, []);

  useEffect(() => {
    return () => {
      const flex = flexRef.current?.control ?? flexRef.current;
      const host = flex?.hostElement as HTMLElement | undefined;
      const handler = (flex as any)?.__msgClickHandler as any;
      if (host && handler) host.removeEventListener('click', handler);
      if (flex) (flex as any).__msgClickHandler = null;
    };
  }, []);

  const reloadCurrentBox = useCallback(() => {
    const fn = fetchBoxRef.current;
    if (!fn) return;
    if (currentScope === COMPANY_PUBLIC_SCOPE) {
      void fn('Inbox', currentScope);
    } else {
      void fn(currentBoxType, currentScope);
    }
  }, [currentScope, currentBoxType]);

  useEffect(() => {
    loadLookups().catch(() => {});
  }, [loadLookups]);

  useEffect(() => {
    const fn = fetchBoxRef.current;
    if (!fn) return;
    if (currentScope === COMPANY_PUBLIC_SCOPE) {
      void fn('Inbox', currentScope);
    } else {
      void fn(currentBoxType, currentScope);
    }
  }, []);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!isResizingRef.current) return;
      const x = e.clientX;
      const next = Math.max(260, Math.min(860, x - 140)); // 140 ~= sidebar + padding; clamp for safety
      setListWidthPx(next);
    };
    const onUp = () => {
      if (!isResizingRef.current) return;
      isResizingRef.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
    return () => {
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };
  }, []);

  const isMessageTemplateScope = currentScope === MESSAGE_TEMPLATE_SCOPE_TYPE;
  const isShowFromColumn = currentScope !== COMPANY_PUBLIC_SCOPE;
  const isShowFromEmailColumn = currentScope === COMPANY_PUBLIC_SCOPE;
  const showInboxBar = currentScope !== COMPANY_PUBLIC_SCOPE && !isMessageTemplateScope;

  const checkOrUncheckAll = (checked: boolean) => {
    setIsSelectAllRows(checked);
    messages.forEach((m) => {
      m.isSelected = checked;
    });
    messageCV.refresh();
  };

  const getSelectedIds = () => messages.filter((m) => m.isSelected && m.Id).map((m) => m.Id);

  const updateReadState = async (isRead: boolean, ids: number[]) => {
    if (currentScope === COMPANY_PUBLIC_SCOPE || !ids.length) return;
    const idSet = new Set(ids.map((x) => Number(x)));
    const res = await appMessageService.setMessageReadState({ IsRead: isRead, MessgeIdList: ids });
    if (res?.IsSuccessful && res?.Object) {
      const src = (messageCvRef.current?.sourceCollection as any[]) ?? messages;
      src.forEach((m) => {
        if (idSet.has(Number(m?.Id ?? m?.id))) {
          m.IsRead = isRead;
          m.isRead = isRead;
        }
      });
      messageCvRef.current?.refresh?.();
      requestUnreadMessageCountRefresh();
      const flex = flexRef.current?.control ?? flexRef.current;
      if (flex?.invalidate) flex.invalidate();
    }
    if (res?.ValidationResult) showErrorsOnly(res.ValidationResult);
  };

  const loadMessageDetailById = useCallback(
    async (messageId: number, cvForRefresh?: CollectionView<any>) => {
      setIsMessageDetailVisible(true);
      const curId = currentMessage?.Id ?? currentMessage?.id;
      if (curId != null && Number(curId) === Number(messageId)) {
        (cvForRefresh ?? messageCvRef.current).refresh();
        return;
      }
      dispatch(setIsBusy());
      try {
        const data = await appMessageService.retrieveOneAppMessageExDto(String(messageId));
        setCurrentMessage(data);
        (cvForRefresh ?? messageCvRef.current).refresh();
        if (currentScope !== COMPANY_PUBLIC_SCOPE && data && !(data.IsRead ?? data.isRead)) {
          // Use the requested id (not dto.Id) to avoid type mismatch (string/number).
          await updateReadState(true, [Number(messageId)]);
        }
      } catch (e: any) {
        showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [currentMessage, currentScope, dispatch, showValidationMessages, updateReadState]
  );

  const displayDetail = useCallback(
    async (messageId?: number) => {
      let id = messageId;
      if (id == null) {
        const flex = flexRef.current?.control ?? flexRef.current;
        const row = flex?.selection?.row;
        if (row != null && row >= 0 && flex.rows?.[row]?.dataItem) {
          const item = flex.rows[row].dataItem;
          id = item.Id ?? item.id;
        }
      }
      if (id == null) return;
      await loadMessageDetailById(Number(id));
    },
    [loadMessageDetailById]
  );

  const displayDetailRef = useRef(displayDetail);
  useEffect(() => {
    displayDetailRef.current = displayDetail;
  }, [displayDetail]);

  const fetchBox = useCallback(
    async (box: typeof currentBoxType, scope: number) => {
      dispatch(setIsBusy());
      try {
        let data: any[] = [];
        const tid = transactionId || '';
        const rid = transctionRid || '';
        if (box === 'Inbox') {
          data = await appMessageService.retrieveCurrentUserInComeMessages(tid, rid, String(scope));
        } else if (box === 'Sent') {
          data = await appMessageService.retrieveCurrentUserOutComeMessages(tid, rid, String(scope));
        } else if (box === 'Deleted') {
          data = await appMessageService.retrieveCurrentUserDeletedMessages(tid, rid, String(scope));
        } else {
          data = await appMessageService.retrieveCurrentUserDraftMessages(tid, rid, String(scope));
        }
        if (!Array.isArray(data)) data = [];
        const cv = rebuildCv(data, box, scope);
        if (box === 'Inbox') requestUnreadMessageCountRefresh();
        if (data.length > 0) {
          const fid = data[0].Id ?? data[0].id;
          if (fid != null) {
            window.requestAnimationFrame(() => {
              const flex = flexRef.current?.control ?? flexRef.current;
              if (flex) {
                const ri = findFirstMessageRowIndex(flex);
                if (ri >= 0) {
                  try {
                    flex.select(new CellRange(ri, 0));
                  } catch {
                    /* ignore */
                  }
                }
              }
              void loadMessageDetailById(Number(fid), cv);
            });
          }
        } else {
          setCurrentMessage(null);
          setIsMessageDetailVisible(false);
        }
      } catch (e: any) {
        showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Failed to load messages' }] }, true);
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, rebuildCv, showValidationMessages, transactionId, transctionRid, loadMessageDetailById]
  );
  fetchBoxRef.current = fetchBox;

  const hideDetail = () => {
    setIsMessageDetailVisible(false);
    messageCV.refresh();
  };

  const deleteMessages = async (ids: number[], isInbox: boolean) => {
    if (!ids.length) return;
    const res = await appMessageService.deleteUserMessages({
      MessgeIdList: ids,
      IsDeleteReceivedMessage: isInbox,
    });
    if (res?.IsSuccessful && res?.Object) {
      reloadCurrentBox();
      hideDetail();
      setCurrentMessage(null);
      requestUnreadMessageCountRefresh();
    }
    if (res?.ValidationResult) showErrorsOnly(res.ValidationResult);
  };

  const openEditor = (heading: string, payload: Record<string, any>) => {
    addTabAndNavigate('message-editor', heading, payload);
  };

  const changeScope = (scopeId: number) => {
    if (scopeId === MESSAGE_TEMPLATE_SCOPE_TYPE) {
      if (scopeId !== currentScope) {
        setCurrentScope(scopeId);
        setIsMessageDetailVisible(false);
        setCurrentMessage(null);
      }
      return;
    }
    if (scopeId !== currentScope) {
      setCurrentScope(scopeId);
      setIsMessageDetailVisible(false);
      setCurrentMessage(null);
      if (scopeId === COMPANY_PUBLIC_SCOPE) {
        setCurrentBoxType('Inbox');
        fetchBox('Inbox', scopeId);
      } else {
        fetchBox(currentBoxType, scopeId);
      }
    }
  };

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.default}`}>
      {!isMessageTemplateScope ? (
        <div className={`flex items-center px-3 py-2 mb-1 gap-3 ${theme.mainContentSection}`}>
          <h1 className={`text-md font-semibold m-0 ${theme.title}`}>
            {currentBoxType} Messages ({messages.length})
          </h1>
          <div className="flex flex-wrap items-center gap-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => openEditor('New Message', {})}
            >
              <i className="fa-solid fa-pen-to-square mr-1" aria-hidden="true" />
              Compose
            </button>
            {(currentBoxType === 'Inbox' || currentBoxType === 'Sent') && (
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => deleteMessages(getSelectedIds(), currentBoxType === 'Inbox')}
              >
                <i className="fa-solid fa-xmark mr-1" aria-hidden="true" />
                Delete
              </button>
            )}
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => reloadCurrentBox()}
            >
              <i className="fa-solid fa-rotate mr-1" aria-hidden="true" />
              Refresh
            </button>
            {currentBoxType === 'Inbox' && (
              <div className="relative">
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={() => setMoreOpen((v) => !v)}
                >
                  <i className="fa-solid fa-ellipsis mr-1" aria-hidden="true" />
                  More
                </button>
                {moreOpen ? (
                  <div
                    className={`absolute right-0 mt-1 z-20 min-w-[180px] rounded-[4px] border shadow ${theme.mainContentSection}`}
                    onMouseLeave={() => setMoreOpen(false)}
                  >
                    <button
                      type="button"
                      className={`block w-full text-left px-3 py-2 text-xs ${theme.button_default}`}
                      onClick={() => {
                        setMoreOpen(false);
                        updateReadState(true, getSelectedIds());
                      }}
                    >
                      Mark as Read
                    </button>
                    <button
                      type="button"
                      className={`block w-full text-left px-3 py-2 text-xs ${theme.button_default}`}
                      onClick={() => {
                        setMoreOpen(false);
                        updateReadState(false, getSelectedIds());
                      }}
                    >
                      Mark as Unread
                    </button>
                    <hr className="opacity-30" />
                    <button
                      type="button"
                      className={`block w-full text-left px-3 py-2 text-xs ${theme.button_default}`}
                      onClick={() => {
                        setMoreOpen(false);
                        setCurrentBoxType('Draft');
                        fetchBox('Draft', currentScope);
                      }}
                    >
                      Show Draft Messages
                    </button>
                  </div>
                ) : null}
              </div>
            )}
          </div>
        </div>
      ) : null}

      <div className="flex w-full min-h-0 flex-auto overflow-hidden">
        <div className={`w-[100px] shrink-0 p-1 overflow-y-auto border-r ${theme.inputBox}`}>
          {sidebarScopes.map((s) => (
            <button
              key={s.Id}
              type="button"
              onClick={() => changeScope(s.Id)}
              className={`w-full mb-2 p-2 rounded text-center text-[11.5px] ${theme.button_default} ${
                currentScope === s.Id ? 'font-semibold ring-1 ring-gray-400' : ''
              }`}
            >
              <div className="text-lg">
                <i className={`fa-regular ${currentScope === s.Id ? 'fa-folder-open' : 'fa-folder'}`} aria-hidden="true" />
              </div>
              <div>{s.Display}</div>
            </button>
          ))}
        </div>

        <section
          className="min-h-0 w-1 flex-auto flex flex-col overflow-hidden min-w-0"
          aria-label={isMessageTemplateScope ? 'Message templates' : undefined}
          aria-labelledby={isMessageTemplateScope ? undefined : 'message-workspace-heading'}
        >
          {!isMessageTemplateScope ? (
            <h2
              id="message-workspace-heading"
              className={`text-sm font-semibold m-0 px-2 py-1.5 shrink-0 border-b ${theme.mainContentSection} ${theme.title}`}
            >
              {currentScopeLabel} · {currentBoxType}
              <span className={`ml-2 text-xs font-normal ${theme.label}`}>({messages.length})</span>
            </h2>
          ) : null}
          {isMessageTemplateScope ? (
            <div className="min-h-0 w-full flex-auto flex flex-col overflow-hidden">
              <MessageTemplateManagement embedded transactionId={transactionId || null} />
            </div>
          ) : (
            <>
              {showInboxBar ? (
                <div className={`flex h-[50px] shrink-0 items-center gap-2 px-2 py-0.5 ${theme.mainContentSection}`}>
                  <div className="h-full flex shrink-0 gap-1">
                    {(['Inbox', 'Sent', 'Deleted'] as const).map((b) => (
                      <button
                        key={b}
                        type="button"
                        onClick={() => {
                          setCurrentBoxType(b);
                          setIsMessageDetailVisible(false);
                          setCurrentMessage(null);
                          fetchBox(b, currentScope);
                        }}
                        className={`w-[72px] h-full text-xs rounded-[4px] border ${theme.button_default} ${
                          currentBoxType === b ? 'bg-neutral-400 text-white border-neutral-500' : ''
                        }`}
                      >
                        <div className="text-md h-[18px]">
                          {b === 'Inbox' ? (
                            <i className="fa-regular fa-envelope" aria-hidden="true" />
                          ) : b === 'Sent' ? (
                            <i className="fa-regular fa-paper-plane" aria-hidden="true" />
                          ) : (
                            <i className="fa-solid fa-trash" aria-hidden="true" />
                          )}
                        </div>
                        <div className="text-[11px]">{b}</div>
                      </button>
                    ))}
                  </div>
                  <div className="w-2.5 shrink-0" aria-hidden="true" />
                  <div className={`flex h-full min-h-0 w-1 flex-auto min-w-0 flex-col overflow-hidden rounded border ${theme.inputBox}`}>
                    <div className={`shrink-0 px-2 pt-0.5 text-[9px] leading-none ${theme.label}`}>
                      Group By: Drag Columns Here To Create Groups
                    </div>
                    <div className="min-h-0 w-full flex-auto overflow-hidden">
                      {flexGridControl ? (
                        <GroupPanel grid={flexGridControl} placeholder="" maxGroups={6} className="h-full w-full min-h-0 text-[9px]" />
                      ) : null}
                    </div>
                  </div>
                </div>
              ) : null}

              <div className="flex w-full min-h-0 flex-auto gap-1 overflow-hidden p-1">
                <div className="flex h-full min-h-[200px] shrink-0 flex-col overflow-hidden" style={{ width: listWidthPx }}>
                  <FlexGrid
                ref={flexRef}
                className="w-full h-full"
                style={{ width: '100%', height: '100%', minHeight: 200 }}
                itemsSource={messageCV}
                selectionMode="Row"
                headersVisibility="Column"
                isReadOnly={false}
                formatItem={(s: any, e: any) => {
                  const flex = s?.control ?? s;
                  if (!flex || !e) return;
                  if (e.panel !== flex.cells) return;

                  const col = flex.columns?.[e.col];
                  const binding = col?.binding;
                  if (!binding) return;

                  // Only style key text columns, and always reset (Wijmo may recycle cells)
                  const isKey =
                    binding === 'AppCreatedById' || binding === 'FromEmail' || binding === 'Subject' || binding === 'AppCreatedDate';
                  if (!isKey) return;

                  const item = flex.rows?.[e.row]?.dataItem;
                  const isUnread = !!(
                    item &&
                    boxTypeRef.current === 'Inbox' &&
                    isUnreadFlag(item.IsRead ?? item.isRead)
                  );

                  if (isUnread) {
                    // High-contrast text color from theme (light: darker; dark: lighter)
                    const p = themeParamRef.current ?? {};
                    const hi =
                      p.text_title_heavy || '';
                    if (hi) e.cell.style.setProperty('color', hi, 'important');
                    e.cell.style.fontWeight = '600';
                  } else {
                    e.cell.style.removeProperty('color');
                    e.cell.style.fontWeight = '';
                  }
                }}
                initialized={(flex: any) => {
                  setFlexGridControl(flex);
                  const host = flex?.hostElement as HTMLElement | null;
                  if (!host) return;
                  if ((flex as any).__msgClickHandler) return;
                  const handler = (e: MouseEvent) => {
                    const ht = flex.hitTest(e);
                    if (ht?.cellType !== wjGrid.CellType.Cell) return;
                    const item = flex.rows?.[ht.row]?.dataItem;
                    const id = item?.Id ?? item?.id;
                    if (id == null) return;
                    displayDetailRef.current?.(Number(id));
                  };
                  (flex as any).__msgClickHandler = handler;
                  host.addEventListener('click', handler);
                }}
                onSelectionChanged={(s: any) => {
                  const flex = s?.control ?? s;
                  const row = flex?.selection?.row;
                  if (row == null || row < 0) return;
                  const item = flex.rows?.[row]?.dataItem;
                  if (!item) return;
                  const id = item.Id ?? item.id;
                  if (id == null) return;
                  displayDetailRef.current?.(Number(id));
                }}
              >
                <FlexGridFilter />
                <FlexGridColumn binding="isSelected" header="Select" width={60} dataType={DataType.Boolean} isReadOnly={false} allowSorting={false}>
                  <FlexGridCellTemplate cellType="ColumnHeader" template={() => (
                    <input
                      type="checkbox"
                      checked={isSelectAllRows}
                      onChange={(e) => checkOrUncheckAll(e.target.checked)}
                    />
                  )} />
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => (
                      <input
                        type="checkbox"
                        checked={!!cell.item.isSelected}
                        onChange={(ev) => {
                          cell.item.isSelected = ev.target.checked;
                          messageCV.refresh();
                        }}
                      />
                    )}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="Id" header="Id" width={80} visible={false} isReadOnly />
                <FlexGridColumn binding="MessgaeScopeType" header="Scope" width={120} visible={false} dataMap={scopeTypeDataMap} isReadOnly />
                <FlexGridColumn binding="MessagePostType" header="Type" width={120} visible={false} dataMap={postTypeDataMap} isReadOnly />
                <FlexGridColumn
                  binding="AppCreatedById"
                  header="From"
                  width={100}
                  dataMap={userdataMap}
                  isReadOnly
                  visible={isShowFromColumn}
                />
                <FlexGridColumn binding="FromEmail" header="FromEmail" width={180} isReadOnly visible={isShowFromEmailColumn} />
                <FlexGridColumn binding="Subject" header="Subject" width="*" minWidth={120} isReadOnly />
                <FlexGridColumn
                  binding="AppCreatedDate"
                  header="Date"
                  width={200}
                  dataType={DataType.Date}
                  format="g"
                  isReadOnly
                />
                <FlexGridColumn binding="ProjectId" header="Workflow" width={100} visible={false} dataMap={workflowDataMap} isReadOnly />
                <FlexGridColumn binding="ProjectActivityId" header="Task" width={100} visible={false} dataMap={taskDataMap} isReadOnly />
                <FlexGridColumn binding="" header="" width="*" minWidth={8} isReadOnly />
                  </FlexGrid>
                </div>

                <div
                  className={`w-[6px] shrink-0 cursor-col-resize border-l border-r ${theme.inputBox} ${theme.mainContentSection}`}
                  role="separator"
                  aria-orientation="vertical"
                  aria-label="Resize message list"
                  onMouseDown={() => {
                    isResizingRef.current = true;
                    document.body.style.cursor = 'col-resize';
                    document.body.style.userSelect = 'none';
                  }}
                />

                <div
                  className={`relative flex h-full min-h-[200px] w-1 min-w-0 flex-auto flex-col overflow-hidden border ${theme.inputBox}`}
                  aria-label="Message reading area"
                >
                  {isMessageDetailVisible && currentMessage ? (
                    <>
                      <button
                        type="button"
                        title="Hide Message Detail"
                        className={`absolute top-2 right-2 z-10 px-2 py-1 text-xs rounded ${theme.button_default}`}
                        onClick={hideDetail}
                      >
                        <i className="fa-solid fa-chevron-left" aria-hidden="true" />
                      </button>
                      <MessageDisplayPanel
                        mode="management"
                        currentMessage={currentMessage}
                        currentBoxType={currentBoxType}
                        isSystemNotification={(currentMessage.MessagePostType ?? currentMessage.messagePostType) === 1}
                        onClose={hideDetail}
                        onDeleteCurrent={() => {
                          const mid = currentMessage?.Id ?? currentMessage?.id;
                          if (mid && (currentBoxType === 'Inbox' || currentBoxType === 'Sent')) {
                            deleteMessages([mid], currentBoxType === 'Inbox');
                          }
                        }}
                        onOpenReply={() =>
                          currentMessage?.Id &&
                          openEditor(`Reply: ${currentMessage.Subject || ''}`, {
                            id: currentMessage.Id,
                            param1: 'reply',
                          })
                        }
                        onOpenReplyAll={() =>
                          currentMessage?.Id &&
                          openEditor(`Reply: ${currentMessage.Subject || ''}`, {
                            id: currentMessage.Id,
                            param1: 'replyall',
                          })
                        }
                        onOpenForward={() =>
                          currentMessage?.Id &&
                          openEditor(`Forward: ${currentMessage.Subject || ''}`, {
                            id: currentMessage.Id,
                            param1: 'forward',
                          })
                        }
                        onOpenDraft={() =>
                          currentMessage?.Id &&
                          openEditor(currentMessage.Subject || 'Draft', { id: currentMessage.Id, param1: 'edit' })
                        }
                      />
                    </>
                  ) : (
                    <div
                      className={`flex h-full min-h-[160px] w-full flex-col items-center justify-center gap-2 px-4 text-center text-sm ${theme.label}`}
                    >
                      <i className="fa-regular fa-envelope-open text-3xl opacity-40" aria-hidden="true" />
                      <p className="m-0 max-w-sm">Select a row in the list to open the message here.</p>
                    </div>
                  )}
                </div>
              </div>
            </>
          )}
        </section>
      </div>
    </div>
  );
};

export default MessageManagement;
