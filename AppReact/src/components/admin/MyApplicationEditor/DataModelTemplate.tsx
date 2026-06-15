import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import { appTransactionService } from '../../../webapi/apptransactionsvc';

interface DataModelTemplateProps {
  menuId: string | null;
}

interface TransactionGroupItem {
  Id: number;
  Name?: string;
  Description?: string;
  AppModifiedDate?: string;
  AppCreatedDate?: string;
  DefaultNavigationSearchId?: number | null;
}

const DataModelTemplate: React.FC<DataModelTemplateProps> = ({ menuId }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showWarning, showInfo } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const { showConfirm } = useAlertConfirm();
  const flexGridRef = useRef<any>(null);

  const applicationId = menuId ?? null;
  const [list, setList] = useState<TransactionGroupItem[]>([]);
  const [cv, setCv] = useState<CollectionView | null>(null);
  const [contextMenu, setContextMenu] = useState<{
    visible: boolean;
    x: number;
    y: number;
    item: TransactionGroupItem | null;
  }>({ visible: false, x: 0, y: 0, item: null });

  const loadData = useCallback(async () => {
    if (applicationId == null || applicationId === '') {
      setList([]);
      setCv(new CollectionView<any>([]));
      return;
    }
    dispatch(setIsBusy());
    try {
      const raw = await appTransactionService.retrieveAllAppTransactionGroupDto(applicationId);
      const arr = Array.isArray(raw) ? raw : [];
      setList(arr);
      const collectionView = new CollectionView(arr);
      collectionView.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      collectionView.sortDescriptions.push(new SortDescription('Id', false));
      setCv(collectionView);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load data model templates');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [applicationId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handleClickOutside = () => {
      if (contextMenu.visible) setContextMenu((c) => ({ ...c, visible: false, item: null }));
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [contextMenu.visible]);

  const handleRefresh = () => loadData();

  const handleCreate = useCallback(() => {
    addTabAndNavigate('transaction-group-editor', 'New Data Model Template', { param1: applicationId }, true);
  }, [addTabAndNavigate, applicationId]);

  const handleEdit = useCallback(
    (item?: TransactionGroupItem | null) => {
      setContextMenu((c) => ({ ...c, visible: false, item: null }));
      const row = item ?? (flexGridRef.current?.control ?? flexGridRef.current)?.rows?.[(flexGridRef.current?.control ?? flexGridRef.current)?.selection?.row]?.dataItem;
      if (!row?.Id) {
        showWarning('Please select a template to edit');
        return;
      }
      const name = row.Name || `Template ${row.Id}`;
      addTabAndNavigate('transaction-group-editor', name, { id: row.Id }, true);
    },
    [addTabAndNavigate, showWarning]
  );

  const handleSetupFolderNavigation = useCallback(
    (item?: TransactionGroupItem | null) => {
      setContextMenu((c) => ({ ...c, visible: false, item: null }));
      const row = item ?? contextMenu.item;
      if (!row?.Id) {
        showWarning('Please select a template');
        return;
      }
      const name = row.Name || `Template ${row.Id}`;
      addTabAndNavigate(
        'transaction-group-editor',
        `${name} — Folder Navigation`,
        { id: row.Id, param2: JSON.stringify({ activeTab: 3 }) },
        true,
      );
    },
    [addTabAndNavigate, contextMenu.item, showWarning],
  );

  const handleDelete = useCallback(
    async (item: TransactionGroupItem | null) => {
      setContextMenu((c) => ({ ...c, visible: false, item: null }));
      if (!item?.Id) {
        showWarning('Please select a template to delete');
        return;
      }
      if (item.DefaultNavigationSearchId) {
        showWarning('This template is set as default navigation and cannot be deleted.');
        return;
      }
      const ok = await showConfirm('Confirm delete this data model template?', { title: 'Confirm' });
      if (!ok) return;
      dispatch(setIsBusy());
      try {
        await appTransactionService.deleteOneAppTransactionGroup(item.Id);
        showInfo('Template deleted');
        await loadData();
      } catch (e) {
        showError(e instanceof Error ? e.message : 'Delete failed');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showConfirm, showError, showInfo, showWarning]
  );

  const openContextMenu = (e: React.MouseEvent, rowItem: TransactionGroupItem) => {
    e.stopPropagation();
    const rect = e.currentTarget.getBoundingClientRect();
    setContextMenu({ visible: true, x: rect.right, y: rect.top, item: rowItem });
  };

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Data Model Template</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleRefresh}
            className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1.5 ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            Refresh
          </button>
          <button
            type="button"
            onClick={handleCreate}
            className={`px-3 py-1.5 text-sm rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
          >
            <i className="fa-solid fa-plus" aria-hidden />
            Create
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {applicationId == null || applicationId === '' ? (
          <div className={`p-4 ${theme.title}`}>Select an application to view data model templates.</div>
        ) : cv ? (
          <FlexGrid
            ref={flexGridRef}
            itemsSource={cv}
            selectionMode="Row"
            isReadOnly
            allowDelete={false}
            style={{ height: '100%' }}
          >
            <FlexGridFilter />
            <FlexGridColumn width={60} header="Actions" isReadOnly>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item as TransactionGroupItem;
                  if (!item) return null;
                  return (
                    <div className="flex items-center justify-center w-full">
                      <button
                        type="button"
                        className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                        title="More Options"
                        onClick={(e) => openContextMenu(e, item)}
                      >
                        <i className="fa-solid fa-pencil text-xs" aria-hidden />
                        <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                      </button>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="ID" width={100} format="d" />
            <FlexGridColumn binding="Name" header="Group Name" width={300} />
            <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150} dataType="Date" />
            <FlexGridColumn binding="Description" header="Description" width="*" />
          </FlexGrid>
        ) : null}
      </div>

      {contextMenu.visible && contextMenu.item && (
        <div
          className={`fixed z-50 border rounded-[4px] shadow-lg py-1 min-w-max ${theme.mainContentSection}`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            onClick={() => handleEdit(contextMenu.item ?? undefined)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden />
            Edit
          </button>
          <button
            type="button"
            onClick={() => handleSetupFolderNavigation(contextMenu.item ?? undefined)}
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
          >
            <i className="fa-solid fa-folder-tree mr-2 flex-shrink-0" aria-hidden />
            Setup Folder Navigation
          </button>
          {!contextMenu.item.DefaultNavigationSearchId && (
            <button
              type="button"
              onClick={() => handleDelete(contextMenu.item)}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
            >
              <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />
              Delete
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default DataModelTemplate;
