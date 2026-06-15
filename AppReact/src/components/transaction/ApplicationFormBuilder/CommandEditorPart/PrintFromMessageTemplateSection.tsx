import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { appMessageService } from '../../../../webapi/appmessagesvc';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import { PopupModalOverlay } from '../../../formMgt/PopupModalOverlay';
import MessageEditor from '../../../message/MessageEditor';

const EmAppTransactionCommandTypePrintFromMessageTemplate = 74;

export function PrintFromMessageTemplateSection(props: {
  transactionId: number | null;
  hierarchy: any;
  action: any;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const { transactionId, hierarchy, action, onMarkChange } = props;

  const [isMessageTemplatePickerOpen, setIsMessageTemplatePickerOpen] = useState(false);
  const [messageTemplatePickerFilter, setMessageTemplatePickerFilter] = useState('');
  const [messageTemplateList, setMessageTemplateList] = useState<any[]>([]);
  const [messageTemplateDisplayCache, setMessageTemplateDisplayCache] = useState<Record<string, string>>({});
  const [messageTemplateEditorParam, setMessageTemplateEditorParam] = useState<{ id?: any; param1?: any; param2?: any } | null>(null);

  const ensureMessageTemplateListLoaded = useCallback(async () => {
    if (messageTemplateList.length > 0) return;
    const tid = hierarchy?.Id ?? transactionId;
    if (!tid) return;
    const raw = await appMessageService.retrieveTransactionMessageTemplates(String(tid));
    setMessageTemplateList(Array.isArray(raw) ? raw : []);
  }, [messageTemplateList.length, hierarchy?.Id, transactionId]);

  // Ensure the display text is available even before opening picker.
  useEffect(() => {
    if (!action) return;
    if (Number(action.ActionType) !== EmAppTransactionCommandTypePrintFromMessageTemplate) return;
    const id = action?.MessageTemplateId ?? null;
    if (!id) return;
    const tid = String(id);
    const exists = messageTemplateList.find((x: any) => String(x?.Id ?? '') === tid);
    if (exists) return;
    if (messageTemplateDisplayCache[tid]) return;
    (async () => {
      try {
        await ensureMessageTemplateListLoaded();
      } catch {
        // ignore
      }
    })();
  }, [action, action?.ActionType, action?.MessageTemplateId, messageTemplateList, messageTemplateDisplayCache, ensureMessageTemplateListLoaded]);

  const messageTemplateDisplay = useMemo(() => {
    const id = action?.MessageTemplateId ?? null;
    if (!id) return '(None)';
    const tid = String(id);
    const dto = messageTemplateList.find((x: any) => String(x?.Id ?? '') === tid);
    if (dto) {
      const base = String(dto?.Bcclist ?? dto?.Subject ?? dto?.Display ?? tid);
      return base ? `${base} (${tid})` : tid;
    }
    return messageTemplateDisplayCache[tid] ?? tid;
  }, [action?.MessageTemplateId, messageTemplateList, messageTemplateDisplayCache]);

  if (!action || Number(action.ActionType) !== EmAppTransactionCommandTypePrintFromMessageTemplate) return null;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Message Template</label>
          <div className="flex items-center gap-2 min-w-0">
            <div className="w-72">
              <div className="flex items-stretch w-full">
                <div
                  className={`w-1 flex-auto min-w-0 h-7 px-2 text-[11px] border border-r-0 rounded-l-[4px] ${theme.inputBox} flex items-center overflow-hidden`}
                  title="Select message template"
                >
                  <div className={`w-full truncate ${theme.label}`}>{messageTemplateDisplay}</div>
                </div>
                <button
                  type="button"
                  className={`w-7 h-7 border rounded-none ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                  title="Select Message Template"
                  onClick={async () => {
                    try {
                      await ensureMessageTemplateListLoaded();
                    } catch (e: any) {
                      showError(e?.message || 'Failed to load message templates');
                    }
                    setIsMessageTemplatePickerOpen(true);
                  }}
                >
                  <i className="fa-solid fa-chevron-down text-[10px]" aria-hidden />
                </button>
                {action?.MessageTemplateId ? (
                  <>
                    <button
                      type="button"
                      className={`w-7 h-7 border border-l-0 rounded-none ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                      title="Edit Message Template"
                      onClick={() => {
                        setMessageTemplateEditorParam({
                          id: action?.MessageTemplateId ?? '',
                          param1: 'edit',
                          param2: {},
                        });
                      }}
                    >
                      <i className="fa-solid fa-pen-to-square text-[10px]" aria-hidden />
                    </button>
                    <button
                      type="button"
                      className={`w-7 h-7 border border-l-0 rounded-r-[4px] ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                      title="Clear Message Template"
                      onClick={() => {
                        action.MessageTemplateId = null;
                        onMarkChange();
                      }}
                    >
                      <i className="fa-solid fa-xmark text-[10px]" aria-hidden />
                    </button>
                  </>
                ) : (
                  <button
                    type="button"
                    className={`w-7 h-7 border border-l-0 rounded-r-[4px] ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                    title="Create Message Template"
                    onClick={() => {
                      const tid = hierarchy?.Id ?? transactionId;
                      setMessageTemplateEditorParam({
                        id: '',
                        param1: '',
                        param2: {
                          IsPredefinedTemplate: true,
                          MessgaeScopeType: 99,
                          TransactionId: tid,
                          newMessageSubject: 'Message Template',
                        },
                      });
                    }}
                  >
                    <i className="fa-solid fa-plus text-[10px]" aria-hidden />
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>

      {isMessageTemplatePickerOpen && (
        <PopupModalOverlay className="p-4">
          <div
            className={`flex max-h-[75vh] w-full max-w-2xl flex-col overflow-hidden rounded-md border shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Select message template</span>
              <button
                type="button"
                onClick={() => setIsMessageTemplatePickerOpen(false)}
                className={`px-2 py-1 text-lg leading-none rounded-[4px] ${theme.button_default}`}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="p-2 flex items-center gap-2">
              <input
                type="text"
                autoComplete="off"
                value={messageTemplatePickerFilter}
                onChange={(e) => setMessageTemplatePickerFilter(e.target.value)}
                placeholder="Filter..."
                className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
              />
            </div>
            <div className="min-h-0 flex-auto overflow-auto">
              {(messageTemplateList || [])
                .filter((x: any) => {
                  const f = String(messageTemplatePickerFilter ?? '').trim().toLowerCase();
                  if (!f) return true;
                  return String(x?.Bcclist ?? x?.Subject ?? x?.Display ?? x?.Id ?? '').toLowerCase().includes(f);
                })
                .map((x: any) => (
                  <button
                    key={String(x?.Id ?? '')}
                    type="button"
                    className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.mainContentSection}`}
                    onClick={() => {
                      action.MessageTemplateId = x?.Id ?? null;
                      setMessageTemplateDisplayCache((prev) => ({
                        ...prev,
                        [String(x?.Id ?? '')]: `${String(x?.Bcclist ?? x?.Subject ?? x?.Display ?? x?.Id ?? '')} (${String(x?.Id ?? '')})`,
                      }));
                      setIsMessageTemplatePickerOpen(false);
                      onMarkChange();
                    }}
                  >
                    {String(x?.Bcclist ?? x?.Subject ?? x?.Display ?? x?.Id ?? '')} ({String(x?.Id ?? '')})
                  </button>
                ))}
            </div>
          </div>
        </PopupModalOverlay>
      )}

      {messageTemplateEditorParam && (
        <EmbeddedLinkedPopupFrame
          title="Message Template"
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setMessageTemplateEditorParam(null)}>
              Close
            </button>
          }
        >
          <MessageEditor
            variant="templateCode"
            embeddedParamObj={messageTemplateEditorParam}
            onRequestClose={() => setMessageTemplateEditorParam(null)}
            onSaved={(savedDto: any) => {
              if (!savedDto) return;
              const id = savedDto?.Id ?? savedDto?.id;
              if (!id) return;
              action.MessageTemplateId = Number(id);
              onMarkChange();
              setMessageTemplateDisplayCache((prev) => ({
                ...prev,
                [String(id)]: (() => {
                  const base = String(savedDto?.Bcclist ?? savedDto?.Subject ?? savedDto?.Display ?? id);
                  return base ? `${base} (${id})` : String(id);
                })(),
              }));
              const tid = hierarchy?.Id ?? transactionId;
              if (tid) {
                appMessageService
                  .retrieveTransactionMessageTemplates(String(tid))
                  .then((raw) => setMessageTemplateList(Array.isArray(raw) ? raw : []))
                  .catch(() => void 0);
              }
            }}
          />
        </EmbeddedLinkedPopupFrame>
      )}
    </>
  );
}

