/**
 * Data Load Settings (Transaction Data Load Management)
 * Ported from AngularJS: TransactionDataLoadManagement.cshtml + transactionDataLoadManagementCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionDataLoadManagement.cshtml
 *            C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Transaction\transactionDataLoadManagementCtrl.js
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import TransactionDataLoadEditor from './TransactionDataLoadEditor';

export interface TransactionDataLoadManagementProps {
  transactionId: number | null;
  /** Optional: when provided, show data loads for a single unit (Angular: controllerModel.transactionUnitId). */
  transactionUnitId?: number | null;
  transactionName?: string | null;
  onRefresh?: () => void;
}

const TransactionDataLoadManagement: React.FC<TransactionDataLoadManagementProps> = ({
  transactionId,
  transactionUnitId,
  transactionName: transactionNameProp,
  onRefresh,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [transactionName, setTransactionName] = useState<string>(transactionNameProp || '');
  const [transactionDataLoadList, setTransactionDataLoadList] = useState<any[]>([]);
  const [transactionData, setTransactionData] = useState<any>(null);
  const [unitDataMap, setUnitDataMap] = useState<DataMap | null>(null);
  const [rootUnitId, setRootUnitId] = useState<number | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; row: any } | null>(null);
  const [editorState, setEditorState] = useState<{
    isOpen: boolean;
    mode: 'create' | 'edit';
    dataLoadId?: number;
    transactionUnitId?: number;
    transactionId?: number;
  }>({ isOpen: false, mode: 'create' });

  const gridRef = useRef<any>(null);
  const dataLoadCV = React.useMemo(() => {
    const cv = new CollectionView(transactionDataLoadList);
    cv.sortDescriptions.push(new SortDescription('LoadOrder', true));
    return cv;
  }, [transactionDataLoadList]);

  const loadData = useCallback(async () => {
    if (!transactionId && !transactionUnitId) {
      setLoading(false);
      return;
    }

    dispatch(setIsBusy());
    try {
      // Angular logic:
      // - If transactionUnitId is provided → RetrievOneAppTransactionUnitDataLoadDto(unitId)
      // - Else → RetrievOneAppTransactionDataLoadDto(transactionId) + getOneAppTransactionData
      let list: any[] = [];

      if (transactionUnitId) {
        const dataLoadList = await appTransactionService.retrievOneAppTransactionUnitDataLoadDto(
          String(transactionUnitId)
        );
        list = Array.isArray(dataLoadList)
          ? dataLoadList
          : dataLoadList?.Items
          ? Array.from(dataLoadList.Items as Iterable<any>)
          : [];
        setTransactionDataLoadList(list);

        // For unit-scoped view we keep header name from props; transaction structure is not required here.
        setTransactionData(null);
        setRootUnitId(null);
        setUnitDataMap(null);
      } else if (transactionId) {
        const [dataLoadList, txnData] = await Promise.all([
          appTransactionService.retrievOneAppTransactionDataLoadDto(String(transactionId)),
          appTransactionService.getOneHierarchyTransaction(
            String(transactionId),
            false,
            '',
            '',
            '',
            false,
            ''
          ),
        ]);

        list = Array.isArray(dataLoadList)
          ? dataLoadList
          : dataLoadList?.Items
          ? Array.from(dataLoadList.Items as Iterable<any>)
          : [];
        setTransactionDataLoadList(list);

        if (txnData) {
          setTransactionName(txnData.TransactionName || transactionNameProp || '');
          setTransactionData(txnData);
          const unitList = txnData.AppTransactionUnitList || [];
          if (unitList.length > 0) {
            setRootUnitId(unitList[0].Id);
            setUnitDataMap(new DataMap(unitList, 'Id', 'UnitDisplayName'));
          } else {
            setRootUnitId(null);
            setUnitDataMap(null);
          }
        }
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load data load list');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, transactionUnitId, transactionNameProp, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [transactionId]);

  const handleCreate = useCallback(() => {
    const unitId = transactionUnitId ?? rootUnitId;
    if (!unitId) {
      showError('No data model unit available. Save the data model first.');
      return;
    }
    setEditorState({
      isOpen: true,
      mode: 'create',
      transactionUnitId: unitId,
      transactionId: transactionId ?? undefined,
    });
  }, [transactionUnitId, rootUnitId, transactionId, showError]);

  const handleRefresh = useCallback(() => {
    loadData();
    onRefresh?.();
  }, [loadData, onRefresh]);

  const handleSaveAll = useCallback(async () => {
    if (transactionDataLoadList.length === 0) return;
    dispatch(setIsBusy());
    try {
      const payload = Array.isArray(transactionDataLoadList) ? transactionDataLoadList : [...transactionDataLoadList];
      const result = await appTransactionService.saveAllAppTransactionDataLoadDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.IsSuccessful) {
        showInfo('Load orders and names updated.');
        loadData();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionDataLoadList, dispatch, showError, showValidationMessages, showInfo, loadData]);

  const handleCellEditEnded = useCallback((sender: any, e: any) => {
    const rowData = sender.rows?.[e.row]?.dataItem;
    if (rowData) rowData.IsModified = true;
  }, []);

  const openContextMenu = useCallback((ev: React.MouseEvent, item: any) => {
    ev.preventDefault();
    ev.stopPropagation();
    setContextMenu({ x: ev.clientX, y: ev.clientY, row: item });
  }, []);

  const closeContextMenu = useCallback(() => {
    setContextMenu(null);
  }, []);

  useEffect(() => {
    if (!contextMenu) return;
    const onClose = () => setContextMenu(null);
    window.addEventListener('click', onClose);
    return () => window.removeEventListener('click', onClose);
  }, [contextMenu]);

  const handleEdit = useCallback((row?: any) => {
    const item = row ?? (gridRef.current?.selection != null && gridRef.current?.rows?.[gridRef.current.selection.row]?.dataItem);
    if (!item?.Id || !item?.TransactionUnitId) return;
    setEditorState({
      isOpen: true,
      mode: 'edit',
      dataLoadId: item.Id,
      transactionUnitId: item.TransactionUnitId,
      transactionId: item.TransactionId ?? transactionId ?? undefined,
    });
    setContextMenu(null);
  }, [transactionId]);

  const handleDelete = useCallback(async (row?: any) => {
    const item = row ?? (gridRef.current?.selection != null && gridRef.current?.rows?.[gridRef.current.selection.row]?.dataItem);
    if (!item) return;
    const confirmed = await showConfirm('Confirm To Delete', { title: 'Delete Data Load' });
    if (!confirmed) {
      setContextMenu(null);
      return;
    }
    setContextMenu(null);
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.deleteAppTransactionDataLoad(item.Id);
      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful !== false) {
        showInfo('Data load deleted.');
        loadData();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to delete');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [showConfirm, dispatch, showError, showValidationMessages, showInfo, loadData]);

  const handleEditorClose = useCallback(() => {
    setEditorState({ isOpen: false, mode: 'create' });
  }, []);

  const handleEditorSaved = useCallback(() => {
    setEditorState({ isOpen: false, mode: 'create' });
    loadData();
  }, [loadData]);

  if (!transactionId && !transactionUnitId) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Data Load Settings</div>
        </div>
        <div className={`w-full flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Select a data model first to configure Data Load Settings.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Data Load Settings: {transactionName || '—'}
        </div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={handleCreate} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Create">
            <i className="fa fa-plus" aria-hidden="true" /> Create
          </button>
          <button type="button" onClick={handleRefresh} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Refresh">
            <i className="fa fa-refresh" aria-hidden="true" /> Refresh
          </button>
          <button type="button" onClick={handleSaveAll} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Update Load Orders and Names">
            <i className="fa fa-save" aria-hidden="true" /> Update Load Orders and Names
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="h-full w-full overflow-auto px-4 py-4">
        {loading ? (
          <p className={`text-xs ${theme.label}`}>Loading...</p>
        ) : (
          <FlexGrid
            ref={gridRef}
            itemsSource={dataLoadCV}
            isReadOnly={false}
            selectionMode="Row"
            cellEditEnded={handleCellEditEnded}
            headersVisibility="Column"
          >
            <FlexGridColumn width={60} isReadOnly={true}>
              <FlexGridCellTemplate cellType="Cell" template={(props: any) => (
                <div className="flex items-center justify-center w-full">
          <button
            type="button"
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
            title="More Options"
            onClick={(e) => openContextMenu(e, props.item)}
          >
                    <i className="fa fa-pencil mr-0.5" aria-hidden="true" />
                    <i className="fa fa-navicon text-[10px] relative -left-1 top-0.5" aria-hidden="true" />
                  </button>
                </div>
              )} />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="ID" width={60} isReadOnly={true} />
            <FlexGridColumn binding="LoadOrder" header="Load Order" width={100} />
            <FlexGridColumn binding="LoadName" header="Load Name" width={300} />
            <FlexGridColumn binding="Description" header="Description" width={300} />
            {!transactionDataLoadList.some((r: any) => r.TransactionUnitId != null) ? null : (
              <FlexGridColumn binding="TransactionUnitId" header="Data Model Unit" width={300} dataMap={unitDataMap} isReadOnly={true} />
            )}
            <FlexGridColumn width="*" isReadOnly={true} />
          </FlexGrid>
        )}
        </div>
      </div>

      {contextMenu && (
        <div
          className={`fixed z-[1000] rounded-[4px] shadow-lg py-1 min-w-[200px] ${theme.mainContentSection} border`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
            onClick={() => handleEdit(contextMenu.row)}
          >
            <i className="fa fa-edit" aria-hidden="true" /> Edit
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 text-red-600`}
            onClick={() => handleDelete(contextMenu.row)}
          >
            <i className="fa fa-trash-o" aria-hidden="true" /> Delete
          </button>
        </div>
      )}

      {editorState.isOpen && editorState.transactionUnitId != null && (editorState.transactionId ?? transactionId) != null && (
        <TransactionDataLoadEditor
          isOpen={true}
          mode={editorState.mode}
          transactionId={(editorState.transactionId ?? transactionId) as number}
          transactionUnitId={editorState.transactionUnitId as number}
          dataLoadId={editorState.dataLoadId}
          onClose={handleEditorClose}
          onSaved={handleEditorSaved}
        />
      )}
    </div>
  );
};

export default TransactionDataLoadManagement;
