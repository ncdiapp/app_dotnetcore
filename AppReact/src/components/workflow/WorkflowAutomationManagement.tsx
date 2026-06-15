import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';

type WorkflowAutomationItem = {
  Id: number;
  TransactionName?: string;
  Name?: string;
  AppModifiedDate?: string;
  AppCreatedDate?: string;
  Description?: string;
  SaasApplicationId?: number | null;
};

const WorkflowAutomationManagement: React.FC = () => {
  const { theme } = useTheme();
  const { showError, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();
  const { showConfirm } = useAlertConfirm();
  const { addTabAndNavigate } = useTabNavigation();

  const [workflowCV, setWorkflowCV] = useState<any>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [contextMenu, setContextMenu] = useState<{ visible: boolean; x: number; y: number; item: WorkflowAutomationItem | null }>({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  const gridRef = useRef<HTMLDivElement>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const list = await appTransactionService.retrieveSaasApplicationWorkflowAutomationList(null);
      const safeList = Array.isArray(list) ? list : [];
      const cv = new CollectionView(safeList);
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      setWorkflowCV(cv);
    } catch (error: any) {
      showError(error?.message || 'Failed to load workflow automation list');
      setWorkflowCV([]);
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handler = () => setContextMenu((prev) => (prev.visible ? { ...prev, visible: false, item: null } : prev));
    if (contextMenu.visible) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenu.visible]);

  const handleEdit = useCallback(() => {
    const item = contextMenu.item;
    setContextMenu({ visible: false, x: 0, y: 0, item: null });
    if (!item?.Id) return;
    const modelName = item.TransactionName || item.Name || 'Workflow Automation Editor';
    addTabAndNavigate('workflow-automation-editor', modelName, {
      id: null,
      isCreateNewItem: false,
      transactionId: item.Id,
      transactionType: null,
      dataSourceRegisterId: null,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: item.TransactionName || item.Name || null
    }, true);
  }, [contextMenu.item, addTabAndNavigate]);

  const handleCreate = useCallback(() => {
    setContextMenu({ visible: false, x: 0, y: 0, item: null });
    addTabAndNavigate('workflow-automation-editor', 'Workflow Automation Editor', {
      id: null,
      isCreateNewItem: true,
      transactionType: null,
      dataSourceRegisterId: null,
      isCreateDtoDataModel: false,
      isCreateApiDataModel: false,
      isCreateDataModelView: false,
      modelName: null
    }, true);
  }, [addTabAndNavigate]);

  const handleDelete = useCallback(async () => {
    const item = contextMenu.item;
    setContextMenu({ visible: false, x: 0, y: 0, item: null });
    if (!item?.Id) return;
    const name = item.TransactionName || item.Name || 'this item';
    const confirmed = await showConfirm(`Please confirm to delete: ${name}`, { title: 'Confirm' });
    if (!confirmed) return;
    try {
      dispatch(setIsBusy());
      const result = await appTransactionService.deleteOneAppTransaction(String(item.Id));
      if (result?.ValidationResult && !result.ValidationResult.IsValid) {
        showValidationMessages(result.ValidationResult, true);
      } else {
        await loadData();
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to delete workflow automation');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [contextMenu.item, loadData, dispatch, showConfirm, showError, showValidationMessages]);

  const openContextMenu = (e: React.MouseEvent, item: WorkflowAutomationItem) => {
    e.stopPropagation();
    const rect = (e.target as HTMLElement).closest('button')?.getBoundingClientRect();
    if (rect) {
      setContextMenu({ visible: true, x: rect.right, y: rect.top, item });
    }
  };

  const formatDate = (val: any) => {
    if (val == null) return '';
    if (typeof val === 'string') return val.split('T')[0] || val;
    return String(val);
  };

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Workflow Automation</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={loadData}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-rotate-right mr-1" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={handleCreate}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-plus mr-1" aria-hidden /> Create Workflow
          </button>
        </div>
      </div>
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`} ref={gridRef}>
        {isLoading ? (
          <div className="flex items-center justify-center h-full">Loading…</div>
        ) : (
          <FlexGrid
            itemsSource={workflowCV}
            selectionMode="Row"
            isReadOnly
            headersVisibility="Column"
            style={{ height: '100%', width: '100%' }}
          >
            <FlexGridFilter />
            <FlexGridColumn width={60} header="Actions" isReadOnly>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => (
                  <div className="flex items-center justify-center w-full">
                    <button
                      type="button"
                      className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                      title="More Options"
                      onClick={(e) => openContextMenu(e, cell.item)}
                    >
                      <i className="fa-solid fa-pencil text-xs" aria-hidden />
                      <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                    </button>
                  </div>
                )}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="ID" width={100} format="d" />
            <FlexGridColumn binding="TransactionName" header="Name" width={300} />
            <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => <span>{formatDate(cell.item?.AppModifiedDate)}</span>}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="AppCreatedDate" header="Created Date" width={150}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => <span>{formatDate(cell.item?.AppCreatedDate)}</span>}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Description" header="Description" width="*" />
          </FlexGrid>
        )}
      </div>

      {contextMenu.visible && contextMenu.item && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={(e) => {
              e.stopPropagation();
              handleEdit();
            }}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={handleDelete}
          >
            <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete
          </button>
        </div>
      )}
    </div>
  );
};

export default WorkflowAutomationManagement;
