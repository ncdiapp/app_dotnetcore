import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { appMessageService } from '../../../../webapi/appmessagesvc';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import MessageEditor from '../../../message/MessageEditor';
import {
  EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress,
  EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId,
  EmAppTransactionCommandTypeSendMessageToTransFieldUserId,
  EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber,
  SendMessageDestinationFields,
} from './SendMessageSections';

/** Angular: ExpressionBuiltInTokenList */
const NOTIFICATION_BUILTIN_TOKENS = [
  '[Today]',
  '[CurrentDatetime]',
  '[CurrentUserName]',
  '[CurrentUserId]',
  '[CurrentPartnerId]',
  '[CurrentPkValue]',
  '[ApplicationURL]',
];

function insertAtTextareaCursor(textarea: HTMLTextAreaElement, text: string) {
  const start = textarea.selectionStart ?? textarea.value.length;
  const end = textarea.selectionEnd ?? textarea.value.length;
  const v = textarea.value ?? '';
  const next = v.slice(0, start) + text + v.slice(end);
  textarea.value = next;
  const caret = start + text.length;
  textarea.setSelectionRange(caret, caret);
}

function usesNotificationMessageSection(actionType: number): boolean {
  return (
    actionType === EmAppTransactionCommandTypeSendMessageToTransFieldEmailAddress ||
    actionType === EmAppTransactionCommandTypeSendMessageToTransFieldPartnerId ||
    actionType === EmAppTransactionCommandTypeSendMessageToTransFieldUserId ||
    actionType === EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber
  );
}

/** Angular: isShowNotificationExpression — rich text template, subject, attach files (not SMS). */
function isEmailStyleNotificationAction(actionType: number): boolean {
  return usesNotificationMessageSection(actionType) && actionType !== EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber;
}

function resolveMessageTemplateId(dto: any): number | null {
  if (dto == null) return null;
  if (typeof dto === 'number') return Number.isFinite(dto) ? dto : null;
  if (typeof dto === 'string' && dto.trim() !== '') {
    const n = Number(dto);
    return Number.isFinite(n) ? n : null;
  }
  const raw = dto.Id ?? dto.id ?? dto.MessageId ?? dto.messageId;
  if (raw == null || raw === '') return null;
  const n = Number(raw);
  return Number.isFinite(n) ? n : null;
}

export function NotificationExpressionSection(props: {
  action: any;
  hierarchy: any;
  transactionId: number | null;
  transactionFieldLookUpList: any[];
  rootLevelTransFieldLookUpList: any[];
  childGridFieldGroups: Array<{ unitName: string; fields: any[] }>;
  globalTransFieldLookUpList?: any[];
  rootFieldSectionTitle?: string;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const {
    action,
    hierarchy,
    transactionId,
    transactionFieldLookUpList,
    rootLevelTransFieldLookUpList,
    childGridFieldGroups,
    globalTransFieldLookUpList = [],
    rootFieldSectionTitle = 'Form Fields',
    onMarkChange,
  } = props;

  const actionRef = useRef(action);
  actionRef.current = action;

  const actionType = Number(action?.ActionType);
  const show = !!action && usesNotificationMessageSection(actionType);
  const isSms = actionType === EmAppTransactionCommandTypeSendSmsToTransFieldPhoneNumber;
  const isEmailStyleNotification = isEmailStyleNotificationAction(actionType);

  const messageRef = useRef<HTMLTextAreaElement | null>(null);
  const keypadRef = useRef<HTMLDivElement | null>(null);
  const [keypadOpen, setKeypadOpen] = useState(false);

  const [messageTemplateList, setMessageTemplateList] = useState<any[]>([]);
  const [messageTemplateDisplayCache, setMessageTemplateDisplayCache] = useState<Record<string, string>>({});
  const [messageTemplateId, setMessageTemplateId] = useState<number | null>(() =>
    action?.MessageTemplateId != null ? Number(action.MessageTemplateId) : null,
  );
  const [messageTemplateEditorParam, setMessageTemplateEditorParam] = useState<{ id?: number; param1?: string; param2?: any } | null>(null);

  const attr = action?.ActionAttribute || { ChildActionList: [] };
  const useRichText = !!attr.IsUseRichTextMessageTemplate;

  useEffect(() => {
    setMessageTemplateId(action?.MessageTemplateId != null ? Number(action.MessageTemplateId) : null);
  }, [action?.Id, action?.MessageTemplateId]);

  const sortedGlobalFields = useMemo(() => {
    const list = Array.isArray(globalTransFieldLookUpList) ? [...globalTransFieldLookUpList] : [];
    return list.sort((a, b) => String(a?.ShortDisplay ?? '').localeCompare(String(b?.ShortDisplay ?? '')));
  }, [globalTransFieldLookUpList]);

  const sortedRootFields = useMemo(() => {
    const list = Array.isArray(rootLevelTransFieldLookUpList) ? [...rootLevelTransFieldLookUpList] : [];
    return list.sort((a, b) => String(a?.ShortDisplay ?? '').localeCompare(String(b?.ShortDisplay ?? '')));
  }, [rootLevelTransFieldLookUpList]);

  const examplePlaceholder = useMemo(() => {
    if (!show || action?.NotificationMessage) return '';
    const first = sortedRootFields[0];
    if (!first?.MessageTemplateDisplay) return '';
    return `Example: [CurrentUserName] sent an order request from the Form # ${first.MessageTemplateDisplay}.`;
  }, [action?.NotificationMessage, show, sortedRootFields]);

  const messageTemplateDisplay = useMemo(() => {
    if (messageTemplateId == null) return '';
    const tid = String(messageTemplateId);
    const dto = messageTemplateList.find((x: any) => String(x?.Id ?? '') === tid);
    if (dto) {
      const base = String(dto?.Bcclist ?? dto?.Subject ?? dto?.Display ?? tid);
      return base || tid;
    }
    return messageTemplateDisplayCache[tid] ?? tid;
  }, [messageTemplateId, messageTemplateList, messageTemplateDisplayCache]);

  const applyMessageTemplateToCommand = useCallback(
    (savedDto: any) => {
      const id = resolveMessageTemplateId(savedDto);
      if (id == null) return false;
      const target = actionRef.current;
      if (target) {
        target.MessageTemplateId = id;
      }
      setMessageTemplateId(id);
      setMessageTemplateDisplayCache((prev) => ({
        ...prev,
        [String(id)]: (() => {
          const base = String(savedDto?.Bcclist ?? savedDto?.Subject ?? savedDto?.Display ?? id);
          return base || String(id);
        })(),
      }));
      onMarkChange();
      return true;
    },
    [onMarkChange],
  );

  const ensureMessageTemplateList = useCallback(async () => {
    const tid = hierarchy?.Id ?? transactionId;
    if (!tid) return;
    try {
      const raw = await appMessageService.retrieveTransactionMessageTemplates(String(tid));
      setMessageTemplateList(Array.isArray(raw) ? raw : []);
    } catch (e: any) {
      showError(e?.message || 'Failed to load message templates');
    }
  }, [hierarchy?.Id, showError, transactionId]);

  useEffect(() => {
    if (!show || !useRichText) return;
    void ensureMessageTemplateList();
  }, [ensureMessageTemplateList, show, useRichText]);

  useEffect(() => {
    if (!keypadOpen) return;
    const onDoc = (e: MouseEvent) => {
      const t = e.target as Node;
      if (keypadRef.current?.contains(t)) return;
      if (messageRef.current?.contains(t)) return;
      setKeypadOpen(false);
    };
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, [keypadOpen]);

  const insertIntoNotificationMessage = useCallback(
    (token: string) => {
      if (!action) return;
      const ta = messageRef.current;
      if (ta) {
        insertAtTextareaCursor(ta, token);
        action.NotificationMessage = ta.value;
      } else {
        action.NotificationMessage = (action.NotificationMessage || '') + token;
      }
      onMarkChange();
      ta?.focus();
    },
    [action, onMarkChange],
  );

  const insertFieldToken = useCallback(
    (field: any) => {
      const token = field?.MessageTemplateDisplay;
      if (!token) return;
      insertIntoNotificationMessage(token);
    },
    [insertIntoNotificationMessage],
  );

  const insertBuiltinToken = useCallback(
    (token: string) => {
      insertIntoNotificationMessage(` ${token} `);
    },
    [insertIntoNotificationMessage],
  );

  const openCreateMessageTemplate = useCallback(() => {
    const tid = hierarchy?.Id ?? transactionId;
    setMessageTemplateEditorParam({
      param2: {
        IsPredefinedTemplate: true,
        MessgaeScopeType: 99,
        TransactionId: tid,
        newMessageSubject: 'Message Template',
      },
    });
  }, [hierarchy?.Id, transactionId]);

  const openEditMessageTemplate = useCallback(() => {
    if (messageTemplateId == null) return;
    setMessageTemplateEditorParam({ id: messageTemplateId, param1: 'edit', param2: {} });
  }, [messageTemplateId]);

  if (!show) return null;

  return (
    <>
      <SendMessageDestinationFields
        action={action}
        transactionFieldLookUpList={transactionFieldLookUpList}
        onMarkChange={onMarkChange}
      />

      {isEmailStyleNotification ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Use Rich Text Message Template</label>
          <div>
            <input
              type="checkbox"
              checked={useRichText}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                action.ActionAttribute.IsUseRichTextMessageTemplate = e.target.checked;
                onMarkChange();
              }}
            />
          </div>
        </div>
      ) : null}

      {isEmailStyleNotification && !useRichText ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Notification Subject</label>
          <input
            type="text"
            className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={action.NotificationSubject ?? ''}
            onChange={(e) => {
              action.NotificationSubject = e.target.value;
              onMarkChange();
            }}
          />
        </div>
      ) : null}

      {(isSms || (isEmailStyleNotification && !useRichText)) ? (
        <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
          <label className={`text-xs ${theme.label} pt-1`}>Notification Message</label>
          <div className="w-full min-w-0">
            <textarea
              ref={messageRef}
              className={`w-full h-[150px] px-2 py-1.5 text-xs border ${theme.inputBox} focus:outline-none`}
              spellCheck={false}
              placeholder={examplePlaceholder}
              value={action.NotificationMessage ?? ''}
              onChange={(e) => {
                action.NotificationMessage = e.target.value;
                onMarkChange();
              }}
              onFocus={() => setKeypadOpen(true)}
              onClick={() => setKeypadOpen(true)}
            />
          </div>
        </div>
      ) : null}

      {isEmailStyleNotification && useRichText ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Message Template</label>
          <div className="flex items-center gap-2 flex-wrap">
            {messageTemplateId != null ? (
              <div className={`text-xs truncate max-w-[280px] ${theme.label}`} title={messageTemplateDisplay}>
                {messageTemplateDisplay}
              </div>
            ) : null}
            {messageTemplateId == null ? (
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={openCreateMessageTemplate}>
                <i className="fa-solid fa-plus mr-1" aria-hidden /> Create
              </button>
            ) : (
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={openEditMessageTemplate}>
                <i className="fa-solid fa-pen-to-square mr-1" aria-hidden /> Edit
              </button>
            )}
            <input
              type="text"
              className={`w-24 h-7 px-2 text-xs border ${theme.inputBox}`}
              value={messageTemplateId ?? ''}
              onChange={(e) => {
                const v = e.target.value.trim();
                const nextId = v ? Number(v) : null;
                if (actionRef.current) {
                  actionRef.current.MessageTemplateId = nextId;
                }
                setMessageTemplateId(nextId);
                onMarkChange();
              }}
            />
          </div>
        </div>
      ) : null}

      {isEmailStyleNotification ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Attach Form Files</label>
          <div>
            <input
              type="checkbox"
              checked={!!attr.IsAttachAllFormFilesToMessage}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                action.ActionAttribute.IsAttachAllFormFilesToMessage = e.target.checked;
                onMarkChange();
              }}
            />
          </div>
        </div>
      ) : null}

      {keypadOpen && (isSms || !useRichText) ? (
        <div ref={keypadRef} className={`mt-2 border rounded p-3 ${theme.mainContentSection}`}>
          <div className="flex gap-3 flex-wrap">
            <div className="w-1 min-w-[320px] flex-auto">
              {sortedGlobalFields.length > 0 ? (
                <fieldset className={`border rounded px-2 pt-3 pb-2 relative ${theme.inputBox}`}>
                  <legend className={`px-1 text-[11px] ${theme.label}`}>Global Fields</legend>
                  <div className="flex flex-wrap gap-1">
                    {sortedGlobalFields.map((f: any) => (
                      <button
                        key={`g-${String(f.Id)}`}
                        type="button"
                        title={f.MessageTemplateDisplay}
                        className={`px-2 py-1 text-[11px] rounded border ${theme.button_default}`}
                        style={{ minWidth: '120px', flex: '1 1 auto' }}
                        onClick={() => insertFieldToken(f)}
                      >
                        {f.ShortDisplay ?? f.Display}
                      </button>
                    ))}
                  </div>
                </fieldset>
              ) : null}

              <fieldset className={`border rounded px-2 pt-3 pb-2 relative mt-4 ${theme.inputBox}`}>
                <legend className={`px-1 text-[11px] ${theme.label}`}>{rootFieldSectionTitle}</legend>
                <div className="flex flex-wrap gap-1">
                  {sortedRootFields.map((f: any) => (
                    <button
                      key={String(f.Id)}
                      type="button"
                      title={f.MessageTemplateDisplay}
                      className={`px-2 py-1 text-[11px] rounded border ${theme.button_default}`}
                      style={{ minWidth: '120px', flex: '1 1 auto' }}
                      onClick={() => insertFieldToken(f)}
                    >
                      {f.ShortDisplay ?? f.Display}
                    </button>
                  ))}
                </div>
              </fieldset>

              {childGridFieldGroups.map((g) => (
                <fieldset key={g.unitName} className={`border rounded px-2 pt-3 pb-2 relative mt-4 ${theme.inputBox}`}>
                  <legend className={`px-1 text-[11px] ${theme.label}`}>Grid {g.unitName} Columns</legend>
                  <div className="flex flex-wrap gap-1">
                    {g.fields.map((f: any) => (
                      <button
                        key={String(f.Id)}
                        type="button"
                        title={f.MessageTemplateDisplay}
                        className={`px-2 py-1 text-[11px] rounded border ${theme.button_default}`}
                        style={{ minWidth: '120px', flex: '1 1 auto' }}
                        onClick={() => insertFieldToken(f)}
                      >
                        {f.ShortDisplay}
                      </button>
                    ))}
                  </div>
                </fieldset>
              ))}
            </div>

            <div className="w-[200px] shrink-0">
              <fieldset className={`border rounded px-2 pt-3 pb-2 relative h-full ${theme.inputBox}`}>
                <legend className={`px-1 text-[11px] ${theme.label}`}>Built-in Token</legend>
                <div className="flex flex-col gap-1">
                  {NOTIFICATION_BUILTIN_TOKENS.map((token) => (
                    <button
                      key={token}
                      type="button"
                      className={`w-full px-2 py-1 text-[11px] rounded border text-center ${theme.button_default}`}
                      onClick={() => insertBuiltinToken(token)}
                    >
                      {token}
                    </button>
                  ))}
                </div>
              </fieldset>
            </div>
          </div>
        </div>
      ) : null}

      {messageTemplateEditorParam ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10055}
          title="Message Template"
          frameInstanceKey={messageTemplateEditorParam.id ?? 'new-message-template'}
          toolbarTrailing={
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => setMessageTemplateEditorParam(null)}
            >
              Close
            </button>
          }
        >
          <MessageEditor
            variant="templateCode"
            embeddedParamObj={messageTemplateEditorParam}
            onRequestClose={() => setMessageTemplateEditorParam(null)}
            onSaved={(savedDto: any) => {
              if (applyMessageTemplateToCommand(savedDto)) {
                void ensureMessageTemplateList();
                setMessageTemplateEditorParam(null);
              }
            }}
          />
        </EmbeddedLinkedPopupFrame>
      ) : null}
    </>
  );
}
