import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { closeTab, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import type { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import { appMessageService } from '../../webapi/appmessagesvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { useEnumEntry } from '../../hooks/useEnumDictionary';
import FileUploader from '../common/FileUploader';
import MessageFilePreviewModal from './MessageFilePreviewModal';
import { buildTransactionFieldLookupsFromHierarchy, type TransactionFieldLookup } from './messageTransactionFieldTokens';
import { MESSAGE_TEMPLATE_SCOPE_TYPE } from './messageScopeConstants';
import RichTextEditor from '../common/RichTextEditor';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import EmailAddressSelectorModal from './EmailAddressSelectorModal';

const BUILT_IN_TOKENS = [
  '[Today]',
  '[CurrentDatetime]',
  '[CurrentUserName]',
  '[WorkflowtName]',
  '[TaskName]',
  '[TaskStatus]',
  '[CurrentPkValue]',
  '[ApplicationURL]',
];

const EMPTY_PARAM2: Record<string, unknown> = {};

function parseParamJson(param: string | undefined): { id?: string; param1?: string; param2?: any } {
  if (!param) return {};
  try {
    return JSON.parse(decodeURIComponent(param));
  } catch {
    return {};
  }
}

/** SaveOneAppMessageDto returns MessageId in Object (number), not a full DTO — Angular reloads then callbacks. */
function mergeSavedMessageDto(draftDto: any, resObject: any): any {
  if (resObject == null) return draftDto ?? null;
  if (typeof resObject === 'object' && !Array.isArray(resObject)) {
    return { ...(draftDto || {}), ...resObject };
  }
  const id = Number(resObject);
  if (!Number.isFinite(id)) return draftDto ?? null;
  return { ...(draftDto || {}), Id: id };
}

function buildToListFromUserIds(allUsers: any[], userIdList: any[]): string {
  if (!userIdList?.length || !allUsers?.length) return '';
  const dict: Record<string, any> = {};
  allUsers.forEach((u) => {
    dict[String(u.Id)] = u;
  });
  let toList = '';
  userIdList.forEach((userId) => {
    const userDto = dict[String(userId)];
    if (userDto?.Email?.trim()) {
      toList += `${userDto.UserName} [${userDto.Email.trim()}];`;
    }
  });
  return toList;
}

type MessageEditorProps = {
  variant?: 'mail' | 'templateCode';
  /** When set, MessageEditor runs embedded (no tab close/navigation). */
  embeddedParamObj?: { id?: any; param1?: any; param2?: any } | null;
  onRequestClose?: () => void;
  onSaved?: (savedDto: any) => void;
};

const MessageEditor: React.FC<MessageEditorProps> = ({ variant: variantProp, embeddedParamObj, onRequestClose, onSaved }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const { showValidationMessages } = useErrorMessage();
  const { showAlert, showConfirm } = useAlertConfirm();
  const userContext = useSelector((s: RootState) => s.userSession.userContext);
  const activeTabKey = useSelector((s: RootState) => s.tabnav.activeTabKey);
  const previousActiveTabKey = useSelector((s: RootState) => s.tabnav.previousActiveTabKey);
  const tabs = useSelector((s: RootState) => s.tabnav.tabs);

  const variant: 'mail' | 'templateCode' =
    variantProp ?? (location.pathname.includes('message-template-code-editor') ? 'templateCode' : 'mail');

  const parsed = useMemo(() => {
    if (embeddedParamObj) return embeddedParamObj;
    return parseParamJson(param);
  }, [embeddedParamObj, param]);
  const messageId = parsed.id != null && parsed.id !== '' ? String(parsed.id) : '';
  const replyType = parsed.param1 || '';
  const p2 = parsed.param2 != null ? parsed.param2 : EMPTY_PARAM2;

  const emPostUserNotification = useEnumEntry('EmAppMessgaePostType', 'UserNotification');
  const emScopeGlobal = useEnumEntry('EmAppMessgaeScopeType', 'Global');
  const emScopeTemplate = useEnumEntry('EmAppMessgaeScopeType', 'MessageTemplate') ?? MESSAGE_TEMPLATE_SCOPE_TYPE;

  const [isBusy, setBusy] = useState(false);
  const [newMessageDto, setNewMessageDto] = useState<any>(null);
  const [messageHtml, setMessageHtml] = useState<string>('');
  const [availableEmailUsers, setAvailableEmailUsers] = useState<any[] | null>(null);
  const [emailSelectorOpen, setEmailSelectorOpen] = useState(false);
  const [emailSelectorTarget, setEmailSelectorTarget] = useState<'to' | 'cc'>('to');
  const [transactions, setTransactions] = useState<any[]>([]);
  const [transFieldList, setTransFieldList] = useState<TransactionFieldLookup[]>([]);
  const [showTransactionToken, setShowTransactionToken] = useState(false);
  const [attachOpen, setAttachOpen] = useState(false);
  const [preview, setPreview] = useState<{ fileId: number; fileName: string } | null>(null);
  const bodyRef = useRef<HTMLDivElement | null>(null);
  const bodySeedRef = useRef<string | null>(null);
  const bodyInitializedRef = useRef(false);
  const templateTextAreaRef = useRef<HTMLTextAreaElement | null>(null);
  const templateCaretRef = useRef<number>(0);
  const templateDragCaretRef = useRef<number | null>(null);
  const templateCaretRafRef = useRef<number | null>(null);
  const mirrorDivRef = useRef<HTMLDivElement | null>(null);

  const isPredefined = !!(p2.IsPredefinedTemplate ?? p2.isPredefinedTemplate);
  const templateScopeNum = Number(emScopeTemplate);

  const isTemplateScopeDto = (dto: any) =>
    Number(dto?.MessgaeScopeType ?? dto?.messgaeScopeType) === templateScopeNum;

  const selfTabKeyRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    // Capture the tabKey that owns this editor instance (avoid closing the wrong tab after async UI changes).
    if (!selfTabKeyRef.current) {
      selfTabKeyRef.current = activeTabKey ?? getCurrentActiveTab()?.tabKey;
    }
  }, [activeTabKey]);

  const closeSelfTab = useCallback(() => {
    if (onRequestClose) {
      onRequestClose();
      return;
    }
    const key = selfTabKeyRef.current ?? getCurrentActiveTab()?.tabKey ?? activeTabKey ?? null;
    if (key == null) {
      navigate('/home');
      return;
    }
    const remainingTabs = tabs.filter((t) => t.tabKey !== key);
    const prevActive = previousActiveTabKey ? remainingTabs.find((t) => t.tabKey === previousActiveTabKey) : undefined;
    const fallbackPath = (prevActive?.path ?? remainingTabs[remainingTabs.length - 1]?.path) ?? '/home';
    dispatch(closeTab(key));
    navigate(fallbackPath);
  }, [activeTabKey, previousActiveTabKey, tabs, dispatch, navigate, onRequestClose]);

  const loadHierarchyTokens = useCallback(async (transactionId: string | number | null | undefined) => {
    if (!transactionId) {
      setTransFieldList([]);
      setShowTransactionToken(false);
      return;
    }
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
      const { transFieldLookUpList } = buildTransactionFieldLookupsFromHierarchy(transactionData);
      setTransFieldList(transFieldLookUpList);
      setShowTransactionToken(transFieldLookUpList.length > 0);
    } catch {
      setTransFieldList([]);
      setShowTransactionToken(false);
    }
  }, []);

  const buildEmptyDto = useCallback(
    async (allUsersForTo: any[]) => {
      const scope = p2.MessgaeScopeType ?? p2.messgaeScopeType ?? emScopeGlobal ?? 1;
      const dto: any = {
        Subject: p2.newMessageSubject || '',
        Message: p2.newMessageBody || '',
        ToList: '',
        Cclist: '',
        Bcclist: '',
        MessgaeScopeType: scope,
        MessagePostType: emPostUserNotification ?? 0,
        TransactionId: p2.TransactionId ?? p2.transactionId ?? null,
        TransactionRootValueId: p2.TransactionRootValueId ?? p2.transactionRootValueId ?? null,
        ProjectActivityId: p2.ProjectActivityId ?? null,
        ProjectTeamId: p2.ProjectTeamId ?? null,
        ProjectId: p2.ProjectId ?? null,
        IsPredefinedTemplate: isPredefined,
        DictAttachmentFileIdAndDisplay: {},
      };
      const initialTo = p2.ToUserIdList ?? p2.toUserIdList;
      if (initialTo?.length && allUsersForTo.length) {
        dto.ToList = buildToListFromUserIds(allUsersForTo, initialTo);
      }
      return dto;
    },
    [p2, emPostUserNotification, emScopeGlobal, isPredefined]
  );

  const loadDataFromServer = useCallback(async () => {
    setBusy(true);
    dispatch(setIsBusy());
    bodyInitializedRef.current = false;
    try {
      let allUsers: any[] = [];
      if (variant === 'templateCode') {
        const mass = await adminSvc.getMassEntitiesLookupItem('AppTransaction');
        setTransactions(mass?.AppTransaction || []);
        allUsers = await adminSvc.retrieveAllAppSecurityUserDto();
      } else {
        allUsers = await adminSvc.retrieveCurrentUserAvailableEmailToUsers();
      }
      setAvailableEmailUsers(Array.isArray(allUsers) ? allUsers : []);

      if (messageId) {
        if (replyType === 'reply') {
          const data = await appMessageService.getMessageReplyDto(messageId);
          setNewMessageDto(data);
          bodySeedRef.current = data?.Message ?? '';
        } else if (replyType === 'replyall') {
          const data = await appMessageService.getMessageReplyAllDto(messageId);
          setNewMessageDto(data);
          bodySeedRef.current = data?.Message ?? '';
        } else if (replyType === 'forward') {
          const data = await appMessageService.getMessageForwardDto(messageId);
          setNewMessageDto(data);
          bodySeedRef.current = data?.Message ?? '';
        } else {
          const data = await appMessageService.retrieveOneAppMessageExDto(messageId);
          data.Subject = data.Subject || '';
          data.Message = data.Message || '';
          data.ToList = data.ToList || '';
          data.Cclist = data.Cclist || '';
          data.Bcclist = data.Bcclist || '';
          setNewMessageDto(data);
          bodySeedRef.current = data?.Message ?? '';
          if (isTemplateScopeDto(data) && data.TransactionId) {
            await loadHierarchyTokens(data.TransactionId);
          } else {
            setShowTransactionToken(false);
            setTransFieldList([]);
          }
        }
        return;
      }

      const tid = p2.TransactionId ?? p2.transactionId;
      const rid = p2.TransactionRootValueId ?? p2.transactionRootValueId;
      let dtoAfter: any;
      if (tid && rid) {
        const data = await appTransactionService.convertMasterDetaiFormDataToNotificationDto(String(tid), String(rid));
        dtoAfter = data || (await buildEmptyDto(allUsers));
      } else {
        dtoAfter = await buildEmptyDto(allUsers);
      }
      setNewMessageDto(dtoAfter);
      bodySeedRef.current = dtoAfter?.Message ?? '';

      if (variant === 'templateCode' || isTemplateScopeDto(dtoAfter)) {
        if (dtoAfter.TransactionId) await loadHierarchyTokens(dtoAfter.TransactionId);
      }
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      setBusy(false);
      dispatch(setIsNotBusy());
    }
  }, [
    buildEmptyDto,
    dispatch,
    loadHierarchyTokens,
    messageId,
    p2.TransactionId,
    p2.TransactionRootValueId,
    p2.transactionId,
    p2.transactionRootValueId,
    replyType,
    showValidationMessages,
    variant,
  ]);

  useEffect(() => {
    loadDataFromServer();
  }, [loadDataFromServer]);

  useEffect(() => {
    return () => {
      if (templateCaretRafRef.current) {
        window.cancelAnimationFrame(templateCaretRafRef.current);
        templateCaretRafRef.current = null;
      }
      if (mirrorDivRef.current?.parentElement) {
        mirrorDivRef.current.parentElement.removeChild(mirrorDivRef.current);
      }
      mirrorDivRef.current = null;
    };
  }, []);

  useEffect(() => {
    if (variant === 'templateCode' && newMessageDto?.TransactionId) {
      loadHierarchyTokens(newMessageDto.TransactionId);
    }
  }, [variant, newMessageDto?.TransactionId, loadHierarchyTokens]);

  useEffect(() => {
    if (variant === 'templateCode') return;
    // Important: do NOT re-seed the rich text editor on every DTO change (editing To/Cc/Subject creates a new dto object).
    if (bodyInitializedRef.current) return;
    const seed = bodySeedRef.current;
    if (seed != null) {
      setMessageHtml(seed);
      bodySeedRef.current = null;
      bodyInitializedRef.current = true;
      return;
    }
    if (newMessageDto?.Message != null) {
      setMessageHtml(String(newMessageDto.Message || ''));
      bodyInitializedRef.current = true;
    }
  }, [newMessageDto?.Message, variant]);

  const syncMirrorStyles = (ta: HTMLTextAreaElement, d: HTMLDivElement) => {
    const cs = window.getComputedStyle(ta);
    d.style.fontFamily = cs.fontFamily;
    d.style.fontSize = cs.fontSize;
    d.style.fontWeight = cs.fontWeight;
    d.style.fontStyle = cs.fontStyle;
    d.style.letterSpacing = cs.letterSpacing;
    d.style.lineHeight = cs.lineHeight;
    d.style.padding = cs.padding;
    d.style.border = cs.border;
    d.style.boxSizing = cs.boxSizing;
    d.style.textTransform = cs.textTransform;
    d.style.tabSize = (cs as any).tabSize ?? (cs as any).MozTabSize ?? '8';
    // IMPORTANT: template textarea uses `whitespace-pre` (no soft wrap).
    // The mirror must match the textarea's wrapping behavior, otherwise x/y mapping drifts.
    d.style.whiteSpace = cs.whiteSpace;
    d.style.overflowWrap = cs.overflowWrap;
    d.style.wordBreak = cs.wordBreak;
    d.style.wordWrap = (cs as any).wordWrap ?? (cs as any).overflowWrap ?? 'normal';
  };

  const ensureMirrorDiv = (ta: HTMLTextAreaElement) => {
    if (!mirrorDivRef.current) {
      const d = document.createElement('div');
      d.style.position = 'fixed';
      d.style.left = '-9999px';
      d.style.top = '0';
      d.style.visibility = 'hidden';
      d.style.whiteSpace = 'pre-wrap';
      d.style.wordWrap = 'break-word';
      d.style.overflow = 'hidden';
      document.body.appendChild(d);
      mirrorDivRef.current = d;
    }
    syncMirrorStyles(ta, mirrorDivRef.current);
    return mirrorDivRef.current;
  };

  const getCaretIndexFromPoint = (ta: HTMLTextAreaElement, clientX: number, clientY: number): number => {
    const rect = ta.getBoundingClientRect();
    const value = ta.value ?? '';
    if (!value) return 0;

    // Target point in "full content" coordinates (content box origin + scroll).
    // Note: clientX/Y are relative to the border box; subtract clientLeft/Top (borders) to match content box.
    const localX = (clientX - rect.left) - (ta.clientLeft || 0);
    const localY = (clientY - rect.top) - (ta.clientTop || 0);
    const cs = window.getComputedStyle(ta);
    const padLeft = Number.parseFloat(cs.paddingLeft || '0') || 0;
    const padTop = Number.parseFloat(cs.paddingTop || '0') || 0;
    // Convert to content-start coordinates (exclude padding), then add scroll.
    const targetX = Math.max(0, localX - padLeft) + (ta.scrollLeft || 0);
    const targetY = Math.max(0, localY - padTop) + (ta.scrollTop || 0);

    // Fast path: monospace + no soft wrap (our template textarea uses `whitespace-pre`).
    // This avoids mirror inaccuracies and makes horizontal tracking precise.
    const whiteSpace = (cs.whiteSpace || '').toLowerCase();
    const noSoftWrap = whiteSpace === 'pre' || whiteSpace === 'nowrap' || whiteSpace === 'pre-line';
    if (noSoftWrap) {
      const font = `${cs.fontStyle} ${cs.fontVariant} ${cs.fontWeight} ${cs.fontSize} / ${cs.lineHeight} ${cs.fontFamily}`;
      // Measure monospace char width (use 'M' as typical max-width glyph).
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      const charWidth = (() => {
        if (!ctx) return 8;
        ctx.font = font;
        return Math.max(1, ctx.measureText('M').width);
      })();
      const lineHeight = (() => {
        const lh = Number.parseFloat(cs.lineHeight || '');
        if (!Number.isFinite(lh) || lh <= 0) return Number.parseFloat(cs.fontSize || '12') * 1.2;
        return lh;
      })();

      const rawLines = String(value).split('\n');
      const lineIndex = Math.max(0, Math.min(rawLines.length - 1, Math.floor(targetY / lineHeight)));
      const line = rawLines[lineIndex] ?? '';

      const col = Math.max(0, Math.floor(targetX / charWidth));

      // Handle tab stops (rare, but keeps mapping reasonable).
      const tabSize = Number.parseInt((cs as any).tabSize ?? (cs as any).MozTabSize ?? '8', 10) || 8;
      let visualCol = 0;
      let idxInLine = 0;
      while (idxInLine < line.length) {
        const ch = line[idxInLine];
        if (ch === '\t') {
          const next = visualCol + (tabSize - (visualCol % tabSize));
          if (next > col) break;
          visualCol = next;
          idxInLine++;
          continue;
        }
        if (visualCol >= col) break;
        visualCol++;
        idxInLine++;
      }

      // Convert (lineIndex, idxInLine) back to absolute string index.
      let abs = 0;
      for (let i = 0; i < lineIndex; i++) abs += rawLines[i].length + 1; // + '\n'
      abs += Math.min(idxInLine, line.length);
      return Math.max(0, Math.min(value.length, abs));
    }

    const d = ensureMirrorDiv(ta);
    // Keep width in sync (important for wrapping).
    // Use clientWidth (content+padding) to avoid border/scrollbar width skew.
    d.style.width = `${ta.clientWidth}px`;

    const getCaretXY = (pos: number) => {
      d.innerHTML = '';
      const pre = document.createElement('span');
      pre.textContent = value.slice(0, pos);
      const marker = document.createElement('span');
      // Use a real glyph so width/left are measurable across browsers.
      // Make it invisible but keep layout width.
      marker.textContent = '.';
      marker.style.opacity = '0';
      d.appendChild(pre);
      d.appendChild(marker);
      const mr = marker.getBoundingClientRect();
      const dr = d.getBoundingClientRect();
      // Convert to content-start coordinates (exclude border+padding) to match targetX/targetY.
      const dcs = window.getComputedStyle(d);
      const dPadLeft = Number.parseFloat(dcs.paddingLeft || '0') || 0;
      const dPadTopNum = Number.parseFloat(dcs.paddingTop || '0') || 0;
      const x = (mr.left - dr.left) - (d.clientLeft || 0) - dPadLeft;
      const y = (mr.top - dr.top) - (d.clientTop || 0) - dPadTopNum;
      return { x, y };
    };

    // Binary search index where caret crosses (x,y)
    let lo = 0;
    let hi = value.length;
    while (lo < hi) {
      const mid = Math.floor((lo + hi) / 2);
      const { x, y } = getCaretXY(mid);
      if (y < targetY || (Math.abs(y - targetY) < 0.75 && x < targetX)) {
        lo = mid + 1;
      } else {
        hi = mid;
      }
    }
    return Math.max(0, Math.min(value.length, lo));
  };

  const insertIntoTemplateAt = (t: string, caretIndex: number | null | undefined) => {
    setNewMessageDto((prev: any) => {
      if (!prev) return prev;
      const cur = String(prev.Message ?? '');
      const idx = Math.max(0, Math.min(cur.length, caretIndex ?? cur.length));
      return { ...prev, Message: `${cur.slice(0, idx)}${t}${cur.slice(idx)}` };
    });
  };

  const appendToken = (t: string) => {
    if (variant === 'templateCode') {
      const ta = templateTextAreaRef.current;
      const idx = templateDragCaretRef.current ?? (ta ? ta.selectionStart : null) ?? templateCaretRef.current ?? null;
      insertIntoTemplateAt(t, idx);
      // Move caret after inserted token (best-effort)
      window.requestAnimationFrame(() => {
        const el = templateTextAreaRef.current;
        if (!el) return;
        const start = Math.max(0, Math.min((el.value ?? '').length, (idx ?? 0) + t.length));
        try {
          el.focus();
          el.setSelectionRange(start, start);
        } catch {
          /* ignore */
        }
        templateCaretRef.current = start;
      });
    } else {
      setMessageHtml((prev) => `${prev || ''}${t}`);
    }
  };

  const appendFieldToken = (f: TransactionFieldLookup) => {
    appendToken(f.MessageTemplateDisplay || '');
  };

  const buildAttachmentToken = (dto: any) => {
    const dict = dto?.DictAttachmentFileIdAndDisplay || {};
    const ids = Object.keys(dict);
    if (!ids.length) return '';
    return ids.join('|');
  };

  const save = async (asDraft: boolean) => {
    if (!newMessageDto) return;
    const mailFieldsVisible = !(isPredefined || isTemplateScopeDto(newMessageDto));
    if (!asDraft && mailFieldsVisible) {
      const to = String(newMessageDto.ToList ?? '').trim();
      const cc = String(newMessageDto.Cclist ?? '').trim();
      const subject = String(newMessageDto.Subject ?? '').trim();
      const missing: string[] = [];
      if (!to && !cc) missing.push('To or Cc');
      if (!subject) missing.push('Subject');
      if (missing.length) {
        await showAlert(
          `Please fill in the following before sending: ${missing.join(', ')}.`,
          { title: 'Cannot send message' }
        );
        return;
      }
    }
    const dto = {
      ...newMessageDto,
      IsDraft: asDraft,
      Message: variant === 'templateCode' ? newMessageDto.Message : messageHtml,
    };
    dto.AttachmentFileToken = buildAttachmentToken(dto);
    setBusy(true);
    dispatch(setIsBusy());
    try {
      const res = await appMessageService.saveOneAppMessageDto(dto);
      if (res?.IsSuccessful && res?.Object != null) {
        // Angular parity: when editing/saving Message Template code, do not show "sent" dialogs.
        // Only show the toast/dialog for actual message sending flows.
        const isTemplateSaveFlow = variant === 'templateCode' || isPredefined || isTemplateScopeDto(dto);
        if (!isTemplateSaveFlow) {
          await showAlert('Message has been sent.');
        }
        const savedDto = mergeSavedMessageDto(newMessageDto, res.Object);
        if (savedDto && typeof savedDto === 'object') {
          const resolvedId = (savedDto as any).Id ?? (savedDto as any).id;
          if (resolvedId != null && (savedDto as any).Id == null) {
            (savedDto as any).Id = resolvedId;
          }
        }
        try {
          onSaved?.(savedDto);
        } catch {
          /* ignore */
        }
        if (!isTemplateSaveFlow) {
          closeSelfTab();
        } else {
          // Keep editor open after saving template.
          setNewMessageDto((prev: any) => mergeSavedMessageDto(prev, res.Object));
        }
      }
      if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Save failed' }] }, true);
    } finally {
      setBusy(false);
      dispatch(setIsNotBusy());
    }
  };

  const onUploadedAttach = async (result: any) => {
    const fileId = result?.FileId ?? result?.fileId;
    const name = result?.ResultMessage || result?.FileName || 'file';
    if (!fileId) return;
    const next = { ...newMessageDto, DictAttachmentFileIdAndDisplay: { ...(newMessageDto?.DictAttachmentFileIdAndDisplay || {}) } };
    next.DictAttachmentFileIdAndDisplay[String(fileId)] = name;
    setNewMessageDto(next);
    try {
      const res = await appMessageService.updateMessageAttachedFiles(next);
      if (res?.IsSuccessful && res?.Object) {
        setNewMessageDto((prev: any) => ({ ...prev, ...res.Object }));
      } else if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
    } catch {
      /* ignore */
    }
    setAttachOpen(false);
  };

  const applySelectedEmails = useCallback(
    async (userIdList: string[]) => {
      const chunk = buildToListFromUserIds(availableEmailUsers || [], userIdList);
      if (!chunk) return;
      setNewMessageDto((prev: any) => {
        if (!prev) return prev;
        if (emailSelectorTarget === 'to') {
          const prevText = String(prev.ToList ?? '');
          const sep = prevText.trim() && !prevText.trim().endsWith(';') ? ';' : '';
          return { ...prev, ToList: `${prevText}${sep}${chunk}` };
        }
        const prevText = String(prev.Cclist ?? '');
        const sep = prevText.trim() && !prevText.trim().endsWith(';') ? ';' : '';
        return { ...prev, Cclist: `${prevText}${sep}${chunk}` };
      });
    },
    [availableEmailUsers, emailSelectorTarget]
  );

  if (!newMessageDto) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.default}`}>
        <div className={`text-sm ${theme.label}`}>{isBusy ? 'Loading…' : 'No data'}</div>
      </div>
    );
  }

  const showMailFields = !(isPredefined || isTemplateScopeDto(newMessageDto));
  const defaultFolderId = userContext?.DefaultFileFolderId ?? userContext?.defaultFileFolderId;
  const attachCount = Object.keys(newMessageDto.DictAttachmentFileIdAndDisplay || {}).length;
  const headerHeightPx = attachCount > 0 ? 130 : 90;

  const openEmailSelector = (target: 'to' | 'cc') => {
    setEmailSelectorTarget(target);
    setEmailSelectorOpen(true);
  };

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.default}`}>
      <div className={`flex items-center px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title} mr-3`}>Message</div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            disabled={isBusy}
            onClick={() => save(false)}
          >
            {isPredefined || isTemplateScopeDto(newMessageDto) ? 'Save' : 'Send'}
          </button>
          {(isPredefined || isTemplateScopeDto(newMessageDto) || variant === 'templateCode') && (
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => loadDataFromServer()}
            >
              Refresh
            </button>
          )}
          {showMailFields && (
            <>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => setAttachOpen(true)}
              >
                Attach File
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={async () => {
                  const ok = await showConfirm('Please confirm to cancel this message');
                  if (ok) closeSelfTab();
                }}
              >
                Cancel
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => save(true)}
              >
                Save Draft
              </button>
            </>
          )}
        </div>
      </div>

      <div className={`w-full min-h-0 flex-auto overflow-hidden flex flex-col ${theme.mainContentSection}`}>
        {/* Header fields area (Angular parity: 90px or 130px with attachments) */}
        <div className="w-full shrink-0 p-1" style={{ height: headerHeightPx }}>
          {showMailFields ? (
            <>
              <div className="flex items-center gap-2 h-[30px]">
                <div className={`w-20 text-xs pl-3 ${theme.label}`}>To</div>
                <div className="w-1 flex-auto min-w-0 flex items-center gap-1">
                  <button
                    type="button"
                    className={`h-[22px] w-[22px] shrink-0 rounded-full border text-[10px] ${theme.button_default}`}
                    title="Select email addresses (To)"
                    onClick={() => openEmailSelector('to')}
                    disabled={isBusy}
                  >
                    <i className="fa-solid fa-plus" aria-hidden="true" />
                  </button>
                  <textarea
                    rows={1}
                    className={`min-h-0 h-[22px] w-1 flex-auto min-w-0 px-3 py-0.5 text-xs border resize-none overflow-hidden ${theme.inputBox}`}
                    value={newMessageDto.ToList ?? ''}
                    onChange={(e) => setNewMessageDto({ ...newMessageDto, ToList: e.target.value })}
                  />
                </div>
              </div>
              <div className="flex items-center gap-2 h-[30px]">
                <div className={`w-20 text-xs pl-3 ${theme.label}`}>Cc</div>
                <div className="w-1 flex-auto min-w-0 flex items-center gap-1">
                  <button
                    type="button"
                    className={`h-[22px] w-[22px] shrink-0 rounded-full border text-[10px] ${theme.button_default}`}
                    title="Select email addresses (Cc)"
                    onClick={() => openEmailSelector('cc')}
                    disabled={isBusy}
                  >
                    <i className="fa-solid fa-plus" aria-hidden="true" />
                  </button>
                  <textarea
                    rows={1}
                    className={`min-h-0 h-[22px] w-1 flex-auto min-w-0 px-3 py-0.5 text-xs border resize-none overflow-hidden ${theme.inputBox}`}
                    value={newMessageDto.Cclist ?? ''}
                    onChange={(e) => setNewMessageDto({ ...newMessageDto, Cclist: e.target.value })}
                  />
                </div>
              </div>
            </>
          ) : (
            <div className="flex items-center gap-2 h-[30px]">
              <div className={`w-20 text-xs pl-3 ${theme.label}`}>Name</div>
              <input
                type="text"
                className={`min-h-0 h-[22px] w-1 flex-auto min-w-0 px-2 text-xs border ${theme.inputBox}`}
                value={newMessageDto.Bcclist ?? ''}
                onChange={(e) => setNewMessageDto({ ...newMessageDto, Bcclist: e.target.value })}
              />
            </div>
          )}

          <div className="flex items-center gap-2 h-[30px]">
            <div className={`w-20 text-xs pl-3 ${theme.label}`}>Subject</div>
            <input
              type="text"
              className={`min-h-0 h-[22px] w-1 flex-auto min-w-0 px-3 text-xs border ${theme.inputBox}`}
              value={newMessageDto.Subject ?? ''}
              onChange={(e) => setNewMessageDto({ ...newMessageDto, Subject: e.target.value })}
            />
          </div>

          {attachCount > 0 ? (
            <div className="flex items-start gap-2 h-[40px] overflow-hidden">
              <div className={`w-20 text-xs pl-3 ${theme.label}`}>Attachment</div>
              <div className="w-1 flex-auto min-w-0 overflow-x-auto whitespace-nowrap text-xs">
                {Object.entries(newMessageDto.DictAttachmentFileIdAndDisplay || {}).map(([fid, name]) => (
                  <span key={fid} className="inline-flex items-center gap-1 mr-3">
                    <button
                      type="button"
                      className={`px-1 ${theme.button_default}`}
                      title="Remove"
                      onClick={() => {
                        const next = { ...newMessageDto.DictAttachmentFileIdAndDisplay };
                        delete (next as any)[fid];
                        setNewMessageDto({ ...newMessageDto, DictAttachmentFileIdAndDisplay: next });
                      }}
                    >
                      <i className="fa-solid fa-xmark" aria-hidden="true" />
                    </button>
                    <button
                      type="button"
                      className={`underline ${theme.button_default}`}
                      onClick={() => setPreview({ fileId: Number(fid), fileName: String(name) })}
                    >
                      {String(name)}
                    </button>
                    ;
                  </span>
                ))}
              </div>
            </div>
          ) : null}
        </div>

        <div className="w-full min-h-0 flex-auto flex gap-2 p-1 overflow-hidden">
          <div
            className={`h-full min-h-0 w-1 flex-auto min-w-0 overflow-hidden border rounded-[4px] ${theme.inputBox}`}
          >
            {variant === 'templateCode' ? (
              <textarea
                ref={templateTextAreaRef}
                className="w-full h-full min-h-0 px-2 py-1 text-xs border-0 bg-transparent outline-none focus:outline-none font-mono whitespace-pre resize-none"
                value={newMessageDto.Message ?? ''}
                onChange={(e) => {
                  templateCaretRef.current = e.target.selectionStart ?? templateCaretRef.current;
                  setNewMessageDto({ ...newMessageDto, Message: e.target.value });
                }}
                onSelect={(e) => {
                  const t = e.target as HTMLTextAreaElement;
                  templateCaretRef.current = t.selectionStart ?? templateCaretRef.current;
                }}
                onKeyUp={(e) => {
                  const t = e.currentTarget as HTMLTextAreaElement;
                  templateCaretRef.current = t.selectionStart ?? templateCaretRef.current;
                }}
                onDragOver={(e) => {
                  e.preventDefault();
                  const ta = templateTextAreaRef.current;
                  if (!ta) return;
                  if (templateCaretRafRef.current) return;
                  templateCaretRafRef.current = window.requestAnimationFrame(() => {
                    templateCaretRafRef.current = null;
                    try {
                      ta.focus();
                    } catch {
                      /* ignore */
                    }
                    const idx = getCaretIndexFromPoint(ta, e.clientX, e.clientY);
                    templateDragCaretRef.current = idx;
                    try {
                      ta.setSelectionRange(idx, idx);
                    } catch {
                      /* ignore */
                    }
                  });
                }}
                onDragLeave={() => {
                  templateDragCaretRef.current = null;
                }}
                onDrop={(e) => {
                  e.preventDefault();
                  const ta = templateTextAreaRef.current;
                  const token = e.dataTransfer?.getData('text/plain') || '';
                  if (!token) return;
                  const idx = ta ? getCaretIndexFromPoint(ta, e.clientX, e.clientY) : null;
                  templateDragCaretRef.current = null;
                  appendToken(token);
                  // Prefer drop caret (appendToken will re-read refs, but we force)
                  if (idx != null) templateCaretRef.current = idx + token.length;
                }}
              />
            ) : (
              <RichTextEditor value={messageHtml} onChange={setMessageHtml} />
            )}
          </div>

          {showTransactionToken && (isTemplateScopeDto(newMessageDto) || variant === 'templateCode') ? (
            <div className={`w-[300px] shrink-0 h-full min-h-0 p-2 overflow-y-auto border ${theme.mainContentSection}`}>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Built-in Token</div>
              <div className="flex flex-wrap gap-1 mb-3">
                {BUILT_IN_TOKENS.map((t) => (
                  <button
                    key={t}
                    type="button"
                    className={`px-2 py-0.5 rounded border text-[11px] ${theme.button_default}`}
                    draggable
                    onDragStart={(e) => {
                      e.dataTransfer?.setData('text/plain', ` ${t} `);
                      e.dataTransfer.effectAllowed = 'copy';
                    }}
                    onClick={() => appendToken(` ${t} `)}
                  >
                    {t}
                  </button>
                ))}
              </div>
              <div className={`text-xs font-semibold mb-2 ${theme.title}`}>Form Fields</div>
              <div className="flex flex-col gap-1">
                {transFieldList.map((f) => (
                  <button
                    key={`${f.Id}-${f.MessageTemplateDisplay}`}
                    type="button"
                    className={`text-left px-2 py-1 rounded border text-[11px] ${theme.button_default}`}
                    title={f.MessageTemplateDisplay}
                    onClick={() => appendFieldToken(f)}
                    draggable
                    onDragStart={(e) => {
                      e.dataTransfer?.setData('text/plain', f.MessageTemplateDisplay || '');
                      e.dataTransfer.effectAllowed = 'copy';
                    }}
                  >
                    {f.ShortDisplay ?? f.MessageTemplateDisplay}
                  </button>
                ))}
              </div>
            </div>
          ) : null}
        </div>
      </div>

      <FileUploader
        isOpen={attachOpen}
        onClose={() => setAttachOpen(false)}
        mode="appFile"
        targetFolderId={defaultFolderId != null ? Number(defaultFolderId) : undefined}
        onUploaded={onUploadedAttach}
      />

      <EmailAddressSelectorModal
        open={emailSelectorOpen}
        onClose={() => setEmailSelectorOpen(false)}
        rawUsers={availableEmailUsers}
        initialFollowerUserIds={[]}
        onApply={applySelectedEmails}
        isBusy={isBusy}
      />

      {preview ? <MessageFilePreviewModal {...preview} onClose={() => setPreview(null)} /> : null}
    </div>
  );
};

export default MessageEditor;
