import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { DataType } from '@mescius/wijmo';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { appMessageService } from '../../webapi/appmessagesvc';
import { useEnumEntry } from '../../hooks/useEnumDictionary';
import MessageDisplayPanel from './MessageDisplayPanel';
import ConversationEditorEmbedded from './ConversationEditorEmbedded';
import { requestUnreadMessageCountRefresh } from './messageUnreadRefresh';

function parseParam(param: string | undefined): {
  transactionId: string;
  transctionRid: string;
  messageScopeType: number | null;
  isFileMessage?: boolean;
} {
  if (!param) {
    return { transactionId: '', transctionRid: '', messageScopeType: null };
  }
  try {
    const o = JSON.parse(decodeURIComponent(param));
    return {
      transactionId: o.transactionId != null ? String(o.transactionId) : '',
      transctionRid: o.transctionRid != null ? String(o.transctionRid) : '',
      messageScopeType: o.messageScopeType != null ? Number(o.messageScopeType) : null,
      isFileMessage: o.isFileMessage,
    };
  } catch {
    return { transactionId: '', transctionRid: '', messageScopeType: null };
  }
}

const FormMessageManagement: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const { addTabAndNavigate } = useTabNavigation();
  const { showValidationMessages } = useErrorMessage();
  const ctx = parseParam(param);

  const emPostConversation = useEnumEntry('EmAppMessgaePostType', 'Conversaction');

  const [messages, setMessages] = useState<any[]>([]);
  const [messageCV, setMessageCV] = useState(() => new CollectionView<any>([]));
  const [isSelectAllRows, setIsSelectAllRows] = useState(false);
  const [isMessageDetailVisible, setIsMessageDetailVisible] = useState(false);
  const [currentMessage, setCurrentMessage] = useState<any | null>(null);
  const [userdataMap, setUserdataMap] = useState(() => new DataMap([], 'Id', 'Display'));
  const flexRef = useRef<any>(null);

  const rebuild = useCallback((data: any[]) => {
    const cv = new CollectionView<any>(data);
    setMessageCV(cv);
    setMessages(data);
    setIsSelectAllRows(false);
    data.forEach((m) => {
      m.isSelected = false;
    });
  }, []);

  const loadLookups = useCallback(async () => {
    const data = await adminSvc.getMassEntitiesLookupItem('AppSecurityUser|AppWorkFlow|AppProjectWorkFlowTask');
    setUserdataMap(new DataMap(data?.AppSecurityUser || [], 'Id', 'Display'));
  }, []);

  const loadMessages = useCallback(async () => {
    if (!ctx.transactionId || !ctx.transctionRid) return;
    dispatch(setIsBusy());
    try {
      const data = await appMessageService.retrieveTransactionFormMessages(ctx.transactionId, ctx.transctionRid);
      rebuild(Array.isArray(data) ? data : []);
      requestUnreadMessageCountRefresh();
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [ctx.transactionId, ctx.transctionRid, dispatch, rebuild, showValidationMessages]);

  useEffect(() => {
    loadLookups().catch(() => {});
  }, [loadLookups]);

  useEffect(() => {
    loadMessages();
  }, [loadMessages]);

  const checkOrUncheckAll = (checked: boolean) => {
    setIsSelectAllRows(checked);
    messages.forEach((m) => {
      m.isSelected = checked;
    });
    messageCV.refresh();
  };

  const updateReadState = async (ids: number[]) => {
    if (!ids.length) return;
    const res = await appMessageService.setMessageReadState({ IsRead: true, MessgeIdList: ids });
    if (res?.IsSuccessful && res?.Object) {
      messages.forEach((m) => {
        if (ids.includes(m.Id)) m.IsRead = true;
      });
      messageCV.refresh();
      requestUnreadMessageCountRefresh();
    }
    if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
  };

  const displayDetail = async (needId?: number) => {
    let clickDto: any = null;
    const flex = flexRef.current?.control ?? flexRef.current;
    if (needId != null && flex?.rows) {
      for (let i = 0; i < flex.rows.length; i++) {
        const di = flex.rows[i]?.dataItem;
        if (di?.Id === needId) clickDto = di;
      }
    } else if (flex?.selection?.row != null) {
      const row = flex.selection.row;
      clickDto = flex.rows?.[row]?.dataItem;
    }
    if (!clickDto?.Id) return;

    setIsMessageDetailVisible(true);
    const convPost = emPostConversation ?? 2;
    const isConv = Number(clickDto.MessagePostType ?? clickDto.messagePostType) === convPost;

    if (isConv) {
      setCurrentMessage(clickDto);
      return;
    }

    dispatch(setIsBusy());
    try {
      const data = await appMessageService.retrieveOneAppMessageExDto(String(clickDto.Id));
      setCurrentMessage(data);
      messageCV.refresh();
      if (data && !data.IsRead) {
        await updateReadState([data.Id]);
      }
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const hideDetail = () => {
    setIsMessageDetailVisible(false);
    messageCV.refresh();
  };

  const openEditor = (heading: string, payload: Record<string, any>) => {
    addTabAndNavigate('message-editor', heading, payload);
  };

  const scopeTypeForConversation = ctx.messageScopeType ?? 5;

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.default}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Form Messages</div>
        <div className="flex flex-wrap gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => loadMessages()}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden="true" />
            Refresh
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => {
              setIsMessageDetailVisible(true);
              setCurrentMessage({
                Subject: '',
                Message: '',
                MessgaeScopeType: scopeTypeForConversation,
                MessagePostType: emPostConversation,
                TransactionId: Number(ctx.transactionId),
                TransactionRootValueId: ctx.transctionRid,
              });
            }}
          >
            New Conversation
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() =>
              openEditor('New Message', {
                param2: {
                  MessgaeScopeType: scopeTypeForConversation,
                  TransactionId: Number(ctx.transactionId),
                  TransactionRootValueId: ctx.transctionRid,
                },
              })
            }
          >
            Compose
          </button>
        </div>
      </div>

      <div className="w-full h-1 flex-auto flex min-h-0 p-1 gap-1">
        <div className="w-[30%] min-w-[200px] h-full min-h-0">
          <FlexGrid
            ref={flexRef}
            className="w-full h-full"
            itemsSource={messageCV}
            selectionMode="Row"
            headersVisibility="Column"
            isReadOnly={false}
            onSelectionChanged={(s: any) => {
              const flex = s?.control ?? s;
              const col = flex?.selection?.col;
              if (col != null && col >= 0) displayDetail();
            }}
          >
            <FlexGridFilter />
            <FlexGridColumn binding="isSelected" header="Select" width={60} dataType={DataType.Boolean} isReadOnly={false} allowSorting={false}>
              <FlexGridCellTemplate
                cellType="ColumnHeader"
                template={() => (
                  <input type="checkbox" checked={isSelectAllRows} onChange={(e) => checkOrUncheckAll(e.target.checked)} />
                )}
              />
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
            <FlexGridColumn binding="AppCreatedById" header="From" width={100} dataMap={userdataMap} isReadOnly />
            <FlexGridColumn binding="Subject" header="Subject" width="*" minWidth={120} isReadOnly />
            <FlexGridColumn binding="AppCreatedDate" header="Date" width={100} format="MM/dd/yyyy" isReadOnly />
            <FlexGridColumn binding="" header="" width="*" minWidth={8} isReadOnly />
          </FlexGrid>
        </div>

        {isMessageDetailVisible && currentMessage ? (
          <div className={`w-[70%] h-1 min-h-0 flex-auto overflow-hidden relative border ${theme.inputBox}`}>
            <button
              type="button"
              title="Hide Message Detail"
              className={`absolute top-2 right-2 z-10 px-2 py-1 text-xs rounded ${theme.button_default}`}
              onClick={hideDetail}
            >
              <i className="fa-solid fa-chevron-left" aria-hidden="true" />
            </button>
            {Number(currentMessage.MessagePostType ?? currentMessage.messagePostType) === (emPostConversation ?? 2) &&
            currentMessage.Id == null ? (
              <div className="w-full h-full p-2 overflow-auto">
                <ConversationEditorEmbedded
                  scopeType={scopeTypeForConversation}
                  transactionId={ctx.transactionId}
                  transactionRootValueId={ctx.transctionRid}
                />
              </div>
            ) : Number(currentMessage.MessagePostType ?? currentMessage.messagePostType) === (emPostConversation ?? 2) &&
              currentMessage.Id != null ? (
              <div className="w-full h-full p-2 overflow-auto">
                <div className={`text-xs mb-2 ${theme.label}`}>{currentMessage.Subject || 'Conversation'}</div>
                <ConversationEditorEmbedded
                  scopeType={scopeTypeForConversation}
                  transactionId={ctx.transactionId}
                  transactionRootValueId={ctx.transctionRid}
                  defaultMessageSubject={currentMessage.Subject || ''}
                />
              </div>
            ) : (
              <MessageDisplayPanel
                mode="management"
                currentMessage={currentMessage}
                onClose={hideDetail}
                onOpenReply={() =>
                  currentMessage?.Id &&
                  openEditor(`Reply: ${currentMessage.Subject || ''}`, { id: currentMessage.Id, param1: 'reply' })
                }
                onOpenReplyAll={() =>
                  currentMessage?.Id &&
                  openEditor(`Reply: ${currentMessage.Subject || ''}`, { id: currentMessage.Id, param1: 'replyall' })
                }
                onOpenForward={() =>
                  currentMessage?.Id &&
                  openEditor(`Forward: ${currentMessage.Subject || ''}`, { id: currentMessage.Id, param1: 'forward' })
                }
                onOpenDraft={() =>
                  currentMessage?.Id &&
                  openEditor(currentMessage.Subject || 'Draft', { id: currentMessage.Id, param1: 'edit' })
                }
              />
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
};

export default FormMessageManagement;
