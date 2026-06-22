import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataType } from '@mescius/wijmo';
import { CellRange } from '@mescius/wijmo.grid';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appMessageService } from '../../webapi/appmessagesvc';
import MessageDisplayPanel from './MessageDisplayPanel';
import { MESSAGE_TEMPLATE_SCOPE_TYPE } from './messageScopeConstants';
import MessageEditor from './MessageEditor';
import MessageTemplateLayoutEditor from './MessageTemplateLayoutEditor';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 200;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 240;

function parseParam(param: string | undefined): { transactionId: string | null } {
  if (!param) return { transactionId: null };
  try {
    const o = JSON.parse(decodeURIComponent(param));
    return { transactionId: o.transactionId != null ? String(o.transactionId) : null };
  } catch {
    return { transactionId: null };
  }
}

export type MessageTemplateManagementProps = {
  /** When true, render as a child of Message Inbox (no extra outer chrome). */
  embedded?: boolean;
  /** Overrides route param when embedded in MessageManagement. */
  transactionId?: string | null;
};

const MessageTemplateManagement: React.FC<MessageTemplateManagementProps> = ({
  embedded = false,
  transactionId: transactionIdProp,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const { addTabAndNavigate } = useTabNavigation();
  const { showValidationMessages } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const parsedTx = parseParam(param).transactionId;
  const transactionId = transactionIdProp !== undefined ? transactionIdProp : parsedTx;

  const [messages, setMessages] = useState<any[]>([]);
  const [messageCV, setMessageCV] = useState(() => new CollectionView<any>([]));
  const [isSelectAllRows, setIsSelectAllRows] = useState(false);
  const [isMessageDetailVisible, setIsMessageDetailVisible] = useState(false);
  const [currentMessage, setCurrentMessage] = useState<any | null>(null);
  const [embeddedMode, setEmbeddedMode] = useState<'none' | 'editCode' | 'designLayout'>(embedded ? 'none' : 'none');
  const [embeddedTarget, setEmbeddedTarget] = useState<any | null>(null);
  const flexRef = useRef<any>(null);
  const suppressOpenRef = useRef(false);
  const [listWidthPx, setListWidthPx] = useState<number>(420);
  const isResizingRef = useRef(false);
  const [contextMenu, setContextMenu] = useState<{ visible: boolean; x: number; y: number; item: any }>({
    visible: false,
    x: 0,
    y: 0,
    item: null,
  });
  const contextMenuRef = useRef<HTMLDivElement | null>(null);

  const rebuild = useCallback((data: any[]) => {
    const cv = new CollectionView<any>(data);
    cv.sortDescriptions.push(new SortDescription('Bcclist', true));
    setMessageCV(cv);
    setMessages(data);
  }, []);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const data = transactionId
        ? await appMessageService.retrieveTransactionMessageTemplates(transactionId)
        : await appMessageService.retrieveAllPredefinedMessageTemplates();
      const list = Array.isArray(data) ? data : [];
      rebuild(list);
      setIsMessageDetailVisible(false);
      setCurrentMessage(null);
      if (embedded && list.length > 0) {
        const first = list[0];
        setEmbeddedTarget(first);
        setEmbeddedMode('editCode');
        window.requestAnimationFrame(() => {
          const flex = flexRef.current?.control ?? flexRef.current;
          if (!flex) return;
          try {
            flex.select(new CellRange(0, 2));
          } catch {
            /* ignore */
          }
        });
      }
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, rebuild, showValidationMessages, transactionId, embedded]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    return () => {
      const flex = flexRef.current?.control ?? flexRef.current;
      const host = flex?.hostElement as HTMLElement | undefined;
      const handler = (flex as any)?.__tmplClickHandler as any;
      if (host && handler) host.removeEventListener('click', handler);
      if (flex) (flex as any).__tmplClickHandler = null;
    };
  }, []);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!isResizingRef.current) return;
      const x = e.clientX;
      // Conservative clamp for typical layouts; user can still resize via splitter.
      const next = Math.max(240, Math.min(900, x - 140));
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

  const checkOrUncheckAll = (checked: boolean) => {
    setIsSelectAllRows(checked);
    messages.forEach((m) => {
      m.isSelected = checked;
    });
    messageCV.refresh();
  };

  const getSelectedIds = () => messages.filter((m) => m.isSelected && m.Id).map((m) => Number(m.Id));

  const deleteSelected = async (ids: number[]) => {
    if (!ids.length) return;
    const res = await appMessageService.deleteMessagesByIdList(ids);
    if (res?.IsSuccessful && res?.Object) loadData();
    if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
  };

  useEffect(() => {
    const onDocClick = () => {
      if (contextMenu.visible) setContextMenu({ visible: false, x: 0, y: 0, item: null });
    };
    if (contextMenu.visible) {
      document.addEventListener('click', onDocClick);
      return () => document.removeEventListener('click', onDocClick);
    }
  }, [contextMenu.visible]);

  useRefineContextMenuField(contextMenu.visible, contextMenuRef, setContextMenu);

  const openTemplateCodeEditor = (item: any) => {
    if (!item?.Id) return;
    if (embedded) {
      setEmbeddedTarget(item);
      setEmbeddedMode('editCode');
      return;
    }
    addTabAndNavigate('message-template-code-editor', item.Subject || 'Template', {
      id: item.Id,
      param1: 'edit',
      param2: { IsPredefinedTemplate: true, MessgaeScopeType: MESSAGE_TEMPLATE_SCOPE_TYPE },
    });
  };

  const openTemplateCodeEditorRef = useRef(openTemplateCodeEditor);
  useEffect(() => {
    openTemplateCodeEditorRef.current = openTemplateCodeEditor;
  }, [openTemplateCodeEditor]);

  const openTemplateLayoutEditor = (item: any) => {
    if (!item?.Id) return;
    if (embedded) {
      setEmbeddedTarget(item);
      setEmbeddedMode('designLayout');
      return;
    }
    addTabAndNavigate('message-template-layout-editor', item.Subject || 'Template', { id: item.Id });
  };

  const deleteOneTemplate = async (item: any) => {
    const id = Number(item?.Id ?? item?.id);
    if (!id) return;
    const ok = await showConfirm(`Please confirm to delete the template: ${item?.Subject || id}`);
    if (!ok) return;
    await deleteSelected([id]);
    if ((currentMessage?.Id ?? currentMessage?.id) === id) {
      setCurrentMessage(null);
      setIsMessageDetailVisible(false);
    }
  };

  const displayDetail = async (messageId?: number) => {
    // Embedded behavior: clicking a row opens the template editor (same as "Edit Code").
    if (embedded) {
      const flex = flexRef.current?.control ?? flexRef.current;
      const row = flex?.selection?.row;
      const item = row != null && row >= 0 ? flex?.rows?.[row]?.dataItem : null;
      if (item) openTemplateCodeEditor(item);
      return;
    }
    let id = messageId;
    if (id == null) {
      const flex = flexRef.current?.control ?? flexRef.current;
      const row = flex?.selection?.row;
      if (row != null && flex.rows?.[row]?.dataItem) id = flex.rows[row].dataItem.Id;
    }
    if (id == null) return;
    setIsMessageDetailVisible(true);
    if (currentMessage?.Id === id) return;
    dispatch(setIsBusy());
    try {
      const data = await appMessageService.retrieveOneAppMessageExDto(String(id));
      setCurrentMessage(data);
      messageCV.refresh();
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

  return (
    <div
      className={`w-full h-full min-h-0 flex flex-col overflow-hidden ${theme.default} ${
        embedded ? '' : 'rounded-t-md rounded-b-md'
      }`}
    >
      <div className={`flex items-center px-3 py-2 mb-1 gap-3 shrink-0 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Message Templates ({messages.length})</div>
        <div className="flex flex-wrap items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => loadData()}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden="true" />
            Refresh
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() =>
              addTabAndNavigate('message-template-code-editor', 'New Message Template', {
                param2: {
                  IsPredefinedTemplate: true,
                  MessgaeScopeType: MESSAGE_TEMPLATE_SCOPE_TYPE,
                  newMessageSubject: 'Message Template',
                },
              })
            }
          >
            <i className="fa-solid fa-pen-to-square mr-1" aria-hidden="true" />
            Create
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto flex min-h-0 gap-1 ${embedded ? 'p-0' : 'p-1'}`}>
        <div className="h-full min-h-0 shrink-0 flex flex-col overflow-hidden" style={{ width: listWidthPx }}>
          <FlexGrid
            ref={flexRef}
            className="w-full h-full"
            itemsSource={messageCV}
            selectionMode="Row"
            headersVisibility="Column"
            isReadOnly={false}
            initialized={(flex: any) => {
              const host = flex?.hostElement as HTMLElement | null;
              if (!host) return;
              if ((flex as any).__tmplClickHandler) return;
              const handler = (e: MouseEvent) => {
                const ht = flex.hitTest(e);
                if (ht?.cellType !== 1 /* CellType.Cell */) return;
                // 0: Actions, 1: Select. Avoid opening editor from those columns.
                if (ht.col != null && ht.col < 2) return;
                const item = flex.rows?.[ht.row]?.dataItem;
                if (!item) return;
                openTemplateCodeEditorRef.current?.(item);
              };
              (flex as any).__tmplClickHandler = handler;
              host.addEventListener('click', handler);
            }}
            onSelectionChanged={(s: any) => {
              const flex = s?.control ?? s;
              const row = flex?.selection?.row;
              if (row == null || row < 0) return;
              if (suppressOpenRef.current) {
                suppressOpenRef.current = false;
                return;
              }
              if (embedded) {
                const item = flex?.rows?.[row]?.dataItem;
                if (item) openTemplateCodeEditor(item);
              } else {
                void displayDetail();
              }
            }}
          >
            <FlexGridFilter />
            <FlexGridColumn width={60} header="Actions" isReadOnly allowSorting={false} allowResizing={false}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => (
                  <div className="flex items-center justify-center w-full">
                    <button
                      type="button"
                      className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                      title="More Options"
                      onClick={(e) => {
                        e.stopPropagation();
                        suppressOpenRef.current = true;
                        const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                        const { x, y } = clampContextMenuPosition(
                          rect.right,
                          rect.top,
                          CONTEXT_MENU_ESTIMATED_WIDTH,
                          CONTEXT_MENU_ESTIMATED_HEIGHT
                        );
                        setContextMenu({ visible: true, x, y, item: cell.item });
                      }}
                    >
                      <i className="fa-solid fa-pencil text-xs" aria-hidden="true" />
                      <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden="true" />
                    </button>
                  </div>
                )}
              />
            </FlexGridColumn>
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
                    onClick={(e) => {
                      e.stopPropagation();
                      suppressOpenRef.current = true;
                    }}
                    onChange={(ev) => {
                      cell.item.isSelected = ev.target.checked;
                      messageCV.refresh();
                    }}
                  />
                )}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="Id" width={80} visible={false} isReadOnly />
            <FlexGridColumn binding="Bcclist" header="Name" width={160} isReadOnly />
            <FlexGridColumn binding="Subject" header="Subject" width="*" minWidth={120} isReadOnly />
            <FlexGridColumn binding="AppCreatedDate" header="Date" width={100} format="MM/dd/yyyy" isReadOnly />
            <FlexGridColumn binding="" header="" width="*" minWidth={8} isReadOnly />
          </FlexGrid>
        </div>

        <div
          className={`w-[6px] shrink-0 cursor-col-resize border-l border-r ${theme.inputBox} ${theme.mainContentSection}`}
          role="separator"
          aria-orientation="vertical"
          aria-label="Resize template list"
          onMouseDown={() => {
            isResizingRef.current = true;
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
          }}
        />

        {embedded ? (
          <div className={`w-1 flex-auto min-w-0 h-full min-h-0 overflow-hidden relative border ${theme.inputBox}`}>
            {embeddedMode === 'editCode' && embeddedTarget ? (
              <MessageEditor
                variant="templateCode"
                embeddedParamObj={{
                  id: embeddedTarget.Id,
                  param1: 'edit',
                  param2: { IsPredefinedTemplate: true, MessgaeScopeType: MESSAGE_TEMPLATE_SCOPE_TYPE },
                }}
                onRequestClose={() => {
                  setEmbeddedMode('none');
                  setEmbeddedTarget(null);
                }}
              />
            ) : embeddedMode === 'designLayout' && embeddedTarget ? (
              <MessageTemplateLayoutEditor />
            ) : (
              <div className={`flex h-full w-full items-center justify-center text-sm ${theme.label}`}>
                Select a row to edit the template.
              </div>
            )}
          </div>
        ) : isMessageDetailVisible && currentMessage ? (
          <div className={`w-[70%] h-1 min-h-0 flex-auto overflow-hidden relative border ${theme.inputBox}`}>
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
              onClose={hideDetail}
              onOpenTemplateCodeEditor={() =>
                currentMessage?.Id &&
                openTemplateCodeEditor(currentMessage)
              }
              onOpenTemplateLayoutEditor={() =>
                currentMessage?.Id &&
                openTemplateLayoutEditor(currentMessage)
              }
            />
          </div>
        ) : null}
      </div>

      {contextMenu.visible && contextMenu.item ? (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
            onClick={() => {
              openTemplateCodeEditor(contextMenu.item);
              setContextMenu({ visible: false, x: 0, y: 0, item: null });
            }}
          >
            <i className="fa-solid fa-pen-to-square" aria-hidden="true" />
            Edit Code
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
            onClick={() => {
              openTemplateLayoutEditor(contextMenu.item);
              setContextMenu({ visible: false, x: 0, y: 0, item: null });
            }}
          >
            <i className="fa-solid fa-table-columns" aria-hidden="true" />
            Design Layout
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap gap-2`}
            onClick={() => {
              void deleteOneTemplate(contextMenu.item);
              setContextMenu({ visible: false, x: 0, y: 0, item: null });
            }}
          >
            <i className="fa-solid fa-trash" aria-hidden="true" />
            Delete
          </button>
        </div>
      ) : null}
    </div>
  );
};

export default MessageTemplateManagement;
