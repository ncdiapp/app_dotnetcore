import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../redux/hooks/useTheme';
import Confirm from '../common/Confirm';
import appHelper from '../../helper/appHelper';
import { useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';
import {
  AppDataIntegrationAgentSessionSummary,
  archiveAppDataIntegrationAgentSessions,
  appDataIntegrationAgentChatTitle,
  deleteAppDataIntegrationAgentSessions,
  listAllAppDataIntegrationAgentSessions,
  renameAppDataIntegrationAgentSession,
  reorderAppDataIntegrationAgentSessions,
} from '../../webapi/appDataIntegrationAgentSvc';

export interface DataIntegrationAgentChatManagementProps {
  isOpen: boolean;
  onClose: () => void;
  onChanged: (deletedSessionIds?: string[]) => void;
}

type ChatRow = AppDataIntegrationAgentSessionSummary & { Title: string; ArchivedText: string };

const toRow = (item: AppDataIntegrationAgentSessionSummary): ChatRow => ({
  ...item,
  Title: appDataIntegrationAgentChatTitle(item),
  ArchivedText: item.IsArchived ? 'Yes' : 'No',
});

export const RenameChatDialog: React.FC<{
  isOpen: boolean;
  initialTitle: string;
  onCancel: () => void;
  onSave: (title: string) => void;
}> = ({ isOpen, initialTitle, onCancel, onSave }) => {
  const { theme } = useTheme();
  const [title, setTitle] = useState(initialTitle);

  useEffect(() => {
    if (isOpen) setTitle(initialTitle);
  }, [isOpen, initialTitle]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 flex items-center justify-center bg-black/30"
      style={{ zIndex: appHelper.getGlobalOverlayZIndex() }}
      onClick={e => e.stopPropagation()}
    >
      <div
        className={`${theme.mainContentSection} rounded-[4px] shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '420px', maxWidth: '90vw' }}
        onClick={e => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Rename chat</div>
          <button type="button" className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`} title="Close" onClick={onCancel}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>
        <div className="px-5 py-5">
          <div className="flex items-center py-1">
            <label className={`w-32 text-xs ${theme.label} mr-2`}>Name</label>
            <input
              type="text"
              value={title}
              onChange={e => setTitle(e.target.value)}
              className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              autoComplete="off"
              autoFocus
            />
          </div>
        </div>
        <div className={`px-5 py-3 flex justify-end space-x-2 ${theme.mainContentSection}`}>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={() => {
              const next = title.trim();
              if (!next) return;
              onSave(next);
            }}
          >
            Save
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onCancel}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};

const DataIntegrationAgentChatManagement: React.FC<DataIntegrationAgentChatManagementProps> = ({ isOpen, onClose, onChanged }) => {
  const { theme } = useTheme();
  const gridRef = useRef<any>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);
  const [cv, setCv] = useState(() => new CollectionView<ChatRow>([]));
  const [contextMenu, setContextMenu] = useState<{ visible: boolean; x: number; y: number; item: ChatRow | null }>({
    visible: false, x: 0, y: 0, item: null,
  });
  const [renameItem, setRenameItem] = useState<ChatRow | null>(null);
  const [deleteIds, setDeleteIds] = useState<string[] | null>(null);

  const closeContextMenu = useCallback(() => {
    setContextMenu({ visible: false, x: 0, y: 0, item: null });
  }, []);

  const load = useCallback(async () => {
    const list = await listAllAppDataIntegrationAgentSessions();
    setCv(new CollectionView<ChatRow>((list || []).map(toRow)));
  }, []);

  useEffect(() => {
    if (isOpen) load().catch(() => setCv(new CollectionView<ChatRow>([])));
  }, [isOpen, load]);

  useEffect(() => {
    if (!contextMenu.visible) return;
    const onDoc = (e: MouseEvent) => {
      if (contextMenuRef.current?.contains(e.target as Node)) return;
      closeContextMenu();
    };
    const t = window.setTimeout(() => document.addEventListener('mousedown', onDoc), 0);
    return () => {
      window.clearTimeout(t);
      document.removeEventListener('mousedown', onDoc);
    };
  }, [closeContextMenu, contextMenu.visible]);

  useRefineContextMenuField(contextMenu.visible, contextMenuRef, setContextMenu);

  const selectedRows = useCallback((): ChatRow[] => {
    const grid = gridRef.current?.control ?? gridRef.current;
    const items: ChatRow[] = [];
    if (grid?.selectedRows?.length) {
      for (let i = 0; i < grid.selectedRows.length; i++) {
        const item = grid.selectedRows[i]?.dataItem as ChatRow | undefined;
        if (item?.SessionGuid) items.push(item);
      }
      return items;
    }
    if (!grid?.rows) return items;
    for (let i = 0; i < grid.rows.length; i++) {
      const row = grid.rows[i];
      if (row?.isSelected && row.dataItem) items.push(row.dataItem as ChatRow);
    }
    return items;
  }, []);

  const selectedGuids = useCallback(() => selectedRows().map(r => r.SessionGuid), [selectedRows]);

  const afterChange = useCallback(async (deleted?: string[]) => {
    await load();
    onChanged(deleted);
  }, [load, onChanged]);

  const handleRename = useCallback(async (title: string) => {
    if (!renameItem) return;
    await renameAppDataIntegrationAgentSession(renameItem.SessionGuid, title);
    setRenameItem(null);
    await afterChange();
  }, [afterChange, renameItem]);

  const handleArchive = useCallback(async (archived: boolean, guids?: string[]) => {
    const ids = guids ?? selectedGuids();
    if (!ids.length) return;
    await archiveAppDataIntegrationAgentSessions(ids, archived);
    closeContextMenu();
    await afterChange();
  }, [afterChange, closeContextMenu, selectedGuids]);

  const handleDeleteConfirm = useCallback(async () => {
    const ids = deleteIds ?? [];
    setDeleteIds(null);
    if (!ids.length) return;
    await deleteAppDataIntegrationAgentSessions(ids);
    closeContextMenu();
    await afterChange(ids);
  }, [afterChange, closeContextMenu, deleteIds]);

  const handleMove = useCallback(async (delta: number) => {
    const items = [...((cv.items as ChatRow[]) || [])];
    const selected = selectedGuids();
    if (selected.length !== 1) return;
    const idx = items.findIndex(i => i.SessionGuid === selected[0]);
    const next = idx + delta;
    if (idx < 0 || next < 0 || next >= items.length) return;
    const tmp = items[idx];
    items[idx] = items[next];
    items[next] = tmp;
    await reorderAppDataIntegrationAgentSessions(items.map(i => i.SessionGuid));
    await afterChange();
  }, [afterChange, cv.items, selectedGuids]);

  if (!isOpen) return null;

  return (
    <>
      <div
        className="fixed inset-0 flex items-center justify-center bg-black/30"
        style={{ zIndex: appHelper.getGlobalOverlayZIndex() }}
        onClick={e => e.stopPropagation()}
      >
        <div
          className={`w-[900px] max-w-[94vw] h-[72vh] flex flex-col overflow-hidden rounded-[4px] border shadow-xl ${theme.mainContentSection}`}
          onClick={e => e.stopPropagation()}
        >
          <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
            <div className={`text-md font-semibold ${theme.title}`}>Chat Management</div>
            <button type="button" className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`} title="Close" onClick={onClose}>
              <i className="fa-solid fa-xmark" />
            </button>
          </div>
          <div className={`flex items-center px-3 py-2 mb-1 space-x-2 ${theme.mainContentSection}`}>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => {
              const rows = selectedRows();
              if (rows.length === 1) setRenameItem(rows[0]);
            }}>Rename</button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleArchive(true)}>Archive</button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleArchive(false)}>Unarchive</button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => {
              const ids = selectedGuids();
              if (ids.length) setDeleteIds(ids);
            }}>Delete</button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Move up" onClick={() => handleMove(-1)}>
              <i className="fa-solid fa-arrow-up mr-1" />Up
            </button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} title="Move down" onClick={() => handleMove(1)}>
              <i className="fa-solid fa-arrow-down mr-1" />Down
            </button>
          </div>
          <div className="min-h-0 h-1 flex-auto overflow-hidden px-3 pb-3">
            <FlexGrid
              ref={gridRef}
              className="w-full h-full"
              itemsSource={cv}
              selectionMode="ListBox"
              headersVisibility="Column"
              isReadOnly
            >
              <FlexGridColumn width={60} header="Actions" isReadOnly>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => (
                    <div className="flex items-center justify-center w-full">
                      <button
                        type="button"
                        className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                        title="More Options"
                        onClick={e => {
                          e.stopPropagation();
                          const rect = e.currentTarget.getBoundingClientRect();
                          setContextMenu({ visible: true, x: rect.right, y: rect.top, item: cell.item });
                        }}
                      >
                        <i className="fa-solid fa-pencil text-xs" aria-hidden />
                        <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                      </button>
                    </div>
                  )}
                />
              </FlexGridColumn>
              <FlexGridColumn header="Name" binding="Title" width={280} />
              <FlexGridColumn header="Status" binding="Status" width={90} />
              <FlexGridColumn header="Archived" binding="ArchivedText" width={80} />
              <FlexGridColumn header="Updated" binding="UpdatedAt" width={160} />
              <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly />
            </FlexGrid>
          </div>
        </div>
      </div>

      {contextMenu.visible && contextMenu.item && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: contextMenu.x, top: contextMenu.y, zIndex: appHelper.getGlobalOverlayZIndex() + 2 }}
          onClick={e => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => { setRenameItem(contextMenu.item); closeContextMenu(); }}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden />Rename
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => handleArchive(!contextMenu.item?.IsArchived, contextMenu.item ? [contextMenu.item.SessionGuid] : [])}
          >
            <i className={`fa-solid ${contextMenu.item.IsArchived ? 'fa-box-open' : 'fa-box-archive'} mr-2 flex-shrink-0`} aria-hidden />
            {contextMenu.item.IsArchived ? 'Unarchive' : 'Archive'}
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => { setDeleteIds(contextMenu.item ? [contextMenu.item.SessionGuid] : []); closeContextMenu(); }}
          >
            <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />Delete
          </button>
        </div>
      )}

      <RenameChatDialog
        isOpen={!!renameItem}
        initialTitle={renameItem ? appDataIntegrationAgentChatTitle(renameItem) : ''}
        onCancel={() => setRenameItem(null)}
        onSave={handleRename}
      />
      <Confirm
        isOpen={!!deleteIds}
        title="Delete chats"
        message={`Permanently delete ${deleteIds?.length ?? 0} chat(s)? This cannot be undone.`}
        confirmLabel="Delete"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteIds(null)}
        confirmButtonStyle={theme.button_default}
      />
    </>
  );
};

export default DataIntegrationAgentChatManagement;
