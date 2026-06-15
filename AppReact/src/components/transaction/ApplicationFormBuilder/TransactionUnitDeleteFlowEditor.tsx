/**
 * Data Model Unit Delete Flow Editor
 * Ported from Angular: TransactionUnitDeleteFlowEditor.cshtml + transactionUnitDeleteFlowEditorCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionUnitDeleteFlowEditor.cshtml
 */
import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import DatabaseTableSelectorDialog from './DatabaseTableSelectorDialog';
import TableColumnSelectorDialog from './TableColumnSelectorDialog';

export interface TransactionUnitDeleteFlowEditorProps {
  isOpen: boolean;
  transactionUnitId: number | null;
  dataSourceRegisterId: number | null;
  unitDisplayName?: string | null;
  applicationId?: string | null;
  onClose: () => void;
  onSaved?: () => void;
}

const TransactionUnitDeleteFlowEditor: React.FC<TransactionUnitDeleteFlowEditorProps> = ({
  isOpen,
  transactionUnitId,
  dataSourceRegisterId,
  unitDisplayName,
  applicationId,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();
  const [loading, setLoading] = useState(true);
  const [deleteFlowList, setDeleteFlowList] = useState<any[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<number[]>([]);
  const [tableSelectorOpen, setTableSelectorOpen] = useState(false);
  const [columnSelectorOpen, setColumnSelectorOpen] = useState(false);
  const [spSelectorOpen, setSpSelectorOpen] = useState(false);
  const [spList, setSpList] = useState<string[]>([]);
  const [currentEditRow, setCurrentEditRow] = useState<any | null>(null);
  const [isValidationSp, setIsValidationSp] = useState(false);
  const gridRef = useRef<any>(null);

  const deleteFlowCV = React.useMemo(() => {
    const cv = new CollectionView(deleteFlowList);
    cv.sortDescriptions.push(new SortDescription('DeleteFlowPriority', true));
    return cv;
  }, [deleteFlowList]);

  const loadData = useCallback(async () => {
    if (!isOpen || !transactionUnitId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    setLoading(true);
    try {
      const data = await appTransactionService.retrieveDeleteFlowListByTransactionUnitId(String(transactionUnitId));
      const list = Array.isArray(data) ? data : [];
      setDeleteFlowList(list);
      setDeletedItemIds([]);
    } catch (e: any) {
      showError(e?.message || 'Failed to load delete flow list');
      setDeleteFlowList([]);
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [isOpen, transactionUnitId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleAddFlow = useCallback(() => {
    if (!transactionUnitId) return;
    const newRow = {
      TransactionUnitId: transactionUnitId,
      RelativeTableName: '',
      RelativeForeignKeyName: '',
      IsForcedDelete: false,
      IsSetEmpty: false,
      IsNotAllowedDeleteWithMsg: false,
      DeleteFlowPriority: deleteFlowList.length,
      WarningMessage: '',
      StoredProcedureName: '',
      DeleteValidationStoredProcedureName: '',
      SpParameterOptions: '',
    };
    setDeleteFlowList((prev) => [...prev, newRow]);
  }, [transactionUnitId, deleteFlowList.length]);

  const handleDeleteFlow = useCallback(() => {
    const grid = gridRef.current?.control;
    if (!grid || !grid.selectedRows || grid.selectedRows.length === 0) {
      showInfo('Select a row to delete');
      return;
    }
    const row = grid.selectedRows[0];
    const item = row?.dataItem;
    if (!item) return;
    if (item.Id) setDeletedItemIds((prev) => [...prev, item.Id]);
    setDeleteFlowList((prev) => prev.filter((r) => r !== item));
  }, [showInfo]);

  const handleSaveFlow = useCallback(async () => {
    if (!transactionUnitId) return;
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAllAppTransactionUnitDeleteFlowExDto({
        TransactionUnitId: transactionUnitId,
        DeletedItemIds: deletedItemIds,
        TransactionUnitDeleteFlowSet: deleteFlowList,
      });
      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful && result?.ObjectList) {
        showInfo('Delete flow saved.');
        setDeleteFlowList(result.ObjectList);
        setDeletedItemIds([]);
        onSaved?.();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionUnitId, deletedItemIds, deleteFlowList, dispatch, showError, showInfo, showValidationMessages, onSaved]);

  const openTableSelector = useCallback((row: any) => {
    setCurrentEditRow(row);
    setTableSelectorOpen(true);
  }, []);

  const handleTableSelected = useCallback(
    (tableName: string, schemaOwner: string) => {
      if (currentEditRow) {
        currentEditRow.RelativeTableName = tableName;
        currentEditRow.SchemaOwner = schemaOwner || null;
        currentEditRow.RelativeForeignKeyName = '';
        currentEditRow.StoredProcedureName = '';
        currentEditRow.DeleteValidationStoredProcedureName = '';
        currentEditRow.SpParameterOptions = '';
        setDeleteFlowList((prev) => [...prev]);
      }
      setCurrentEditRow(null);
      setTableSelectorOpen(false);
    },
    [currentEditRow]
  );

  const openColumnSelector = useCallback((row: any) => {
    if (!row?.RelativeTableName) {
      showInfo('Select Relative Table first.');
      return;
    }
    setCurrentEditRow(row);
    setColumnSelectorOpen(true);
  }, [showInfo]);

  const handleColumnSelected = useCallback(
    (selectedColumns: any[]) => {
      if (currentEditRow && selectedColumns?.length > 0) {
        currentEditRow.RelativeForeignKeyName = selectedColumns[0].Name;
        setDeleteFlowList((prev) => [...prev]);
      }
      setCurrentEditRow(null);
      setColumnSelectorOpen(false);
    },
    [currentEditRow]
  );

  useEffect(() => {
    if (!spSelectorOpen || dataSourceRegisterId == null) return;
    schemaMetadataService.getSysStoredProcedureNameList('', dataSourceRegisterId).then((list) => {
      setSpList(Array.isArray(list) ? list : []);
    });
  }, [spSelectorOpen, dataSourceRegisterId]);

  const openSpSelector = useCallback((row: any, forValidation: boolean) => {
    setCurrentEditRow(row);
    setIsValidationSp(forValidation);
    setSpSelectorOpen(true);
  }, []);

  const handleSpSelected = useCallback(
    async (spName: string) => {
      if (!currentEditRow || dataSourceRegisterId == null) {
        setSpSelectorOpen(false);
        setCurrentEditRow(null);
        return;
      }
      try {
        const params = await schemaMetadataService.getStoredProcedureParameterList(
          spName,
          null,
          dataSourceRegisterId
        );
        if (params && params.length === 3 && params[2]?.ParameterName === '@CurrentUserId') {
          if (isValidationSp) {
            currentEditRow.StoredProcedureName = '';
            currentEditRow.DeleteValidationStoredProcedureName = spName;
          } else {
            currentEditRow.StoredProcedureName = spName;
            currentEditRow.DeleteValidationStoredProcedureName = '';
          }
          currentEditRow.SpParameterOptions = params[0]?.ParameterName ?? '';
          currentEditRow.WarningMessage = params[1]?.ParameterName ?? '';
          currentEditRow.RelativeTableName = '';
          currentEditRow.RelativeForeignKeyName = '';
          setDeleteFlowList((prev) => [...prev]);
        } else {
          showInfo('This is not a valid stored procedure for deleting flow (expected 3 params including @CurrentUserId).');
        }
      } catch (_) {
        showError('Failed to validate stored procedure');
      }
      setSpSelectorOpen(false);
      setCurrentEditRow(null);
    },
    [currentEditRow, isValidationSp, dataSourceRegisterId, showInfo, showError]
  );

  const handleCellEditEnded = useCallback(() => {
    setDeleteFlowList((prev) => [...prev]);
  }, []);

  if (!isOpen) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-[10000] flex items-center justify-center bg-black bg-opacity-50"
      >
        <div
          className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
          style={{ width: '95vw', height: '90vh', maxWidth: 1200, maxHeight: 700 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold ${theme.title}`}>
              Delete Flow: {unitDisplayName || 'Unit'}
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={handleAddFlow}
                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}
              >
                <i className="fa fa-plus mr-1" /> Add
              </button>
              <button
                type="button"
                onClick={handleDeleteFlow}
                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}
              >
                <i className="fa fa-trash mr-1" /> Delete
              </button>
              <button
                type="button"
                onClick={handleSaveFlow}
                className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}
              >
                <i className="fa fa-save mr-1" /> Save
              </button>
              <button
                type="button"
                onClick={onClose}
                className="px-3 py-1 rounded-[4px] text-xs border"
              >
                Close
              </button>
            </div>
          </div>

          <div className="flex-1 min-h-0 overflow-hidden flex flex-col p-2">
            {loading ? (
              <div className={`p-4 ${theme.label}`}>Loading...</div>
            ) : (
              <>
                <div className="flex items-center gap-2 mb-2">
                  <span className={`text-xs ${theme.label}`}>Relative Table:</span>
                  <button
                    type="button"
                    className={`px-2 py-1 text-xs ${theme.button_default} rounded`}
                    onClick={() => {
                      const grid = gridRef.current?.control;
                      const row = grid?.selection?.row;
                      const item = row != null && grid?.rows?.[row] ? grid.rows[row].dataItem : null;
                      if (item) openTableSelector(item);
                      else showInfo('Select a row first.');
                    }}
                  >
                    Pick Table
                  </button>
                  <span className={`text-xs ${theme.label} ml-2`}>Relative Foreign Key:</span>
                  <button
                    type="button"
                    className={`px-2 py-1 text-xs ${theme.button_default} rounded`}
                    onClick={() => {
                      const grid = gridRef.current?.control;
                      const row = grid?.selection?.row;
                      const item = row != null && grid?.rows?.[row] ? grid.rows[row].dataItem : null;
                      if (item) openColumnSelector(item);
                      else showInfo('Select a row first.');
                    }}
                  >
                    Pick Column
                  </button>
                  <button
                    type="button"
                    className={`px-2 py-1 text-xs ${theme.button_default} rounded ml-2`}
                    onClick={() => {
                      const grid = gridRef.current?.control;
                      const row = grid?.selection?.row;
                      const item = row != null && grid?.rows?.[row] ? grid.rows[row].dataItem : null;
                      if (item) openSpSelector(item, false);
                      else showInfo('Select a row first.');
                    }}
                  >
                    Pick Stored Proc
                  </button>
                  <button
                    type="button"
                    className={`px-2 py-1 text-xs ${theme.button_default} rounded`}
                    onClick={() => {
                      const grid = gridRef.current?.control;
                      const row = grid?.selection?.row;
                      const item = row != null && grid?.rows?.[row] ? grid.rows[row].dataItem : null;
                      if (item) openSpSelector(item, true);
                      else showInfo('Select a row first.');
                    }}
                  >
                    Pick Validation SP
                  </button>
                </div>
                <div className="w-full h-1 flex-auto overflow-hidden">
                  <FlexGrid
                    ref={gridRef}
                    itemsSource={deleteFlowCV}
                    isReadOnly={false}
                    selectionMode="Row"
                    style={{ height: '100%', border: 'none' }}
                    cellEditEnded={handleCellEditEnded}
                  >
                    <FlexGridFilter />
                    <FlexGridColumn binding="DeleteFlowPriority" header="Priority" width={70} dataType="Number" />
                    <FlexGridColumn binding="RelativeTableName" header="Relative Table" width={200} />
                    <FlexGridColumn binding="RelativeForeignKeyName" header="Relative ForeignKey" width={200} />
                    <FlexGridColumn binding="IsForcedDelete" header="Is Forced Delete" width={120} dataType="Boolean" />
                    <FlexGridColumn binding="IsSetEmpty" header="Is Set Empty" width={100} dataType="Boolean" />
                    <FlexGridColumn
                      binding="IsNotAllowedDeleteWithMsg"
                      header="Is Not Allowed Delete With Msg"
                      width={200}
                      dataType="Boolean"
                    />
                    <FlexGridColumn binding="WarningMessage" header="Warning Message" width={180} />
                    <FlexGridColumn binding="StoredProcedureName" header="Stored Procedure" width={180} />
                    <FlexGridColumn
                      binding="DeleteValidationStoredProcedureName"
                      header="Validation SP"
                      width={180}
                    />
                    <FlexGridColumn binding="SpParameterOptions" header="Sp Parameter Options" width={150} />
                    <FlexGridColumn header="" width="*" isReadOnly={true} />
                  </FlexGrid>
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {tableSelectorOpen && (
        <DatabaseTableSelectorDialog
          isOpen={true}
          dataSourceRegisterId={dataSourceRegisterId}
          applicationId={applicationId ?? null}
          onClose={() => {
            setTableSelectorOpen(false);
            setCurrentEditRow(null);
          }}
          onSelect={handleTableSelected}
        />
      )}

      {columnSelectorOpen && currentEditRow?.RelativeTableName && (
        <TableColumnSelectorDialog
          isOpen={true}
          tableName={currentEditRow.RelativeTableName}
          schemaOwner={currentEditRow.SchemaOwner ?? null}
          dataSourceRegisterId={dataSourceRegisterId}
          lockedColumnNames={[]}
          onClose={() => {
            setColumnSelectorOpen(false);
            setCurrentEditRow(null);
          }}
          onSelect={handleColumnSelected}
        />
      )}

      {spSelectorOpen && (
        <div
          className="fixed inset-0 z-[10001] flex items-center justify-center bg-black bg-opacity-50"
          onClick={() => {
            setSpSelectorOpen(false);
            setCurrentEditRow(null);
          }}
        >
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl border p-4`}
            style={{ width: 400, maxHeight: 400 }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`text-sm font-semibold ${theme.title} mb-2`}>
              {isValidationSp ? 'Validation Stored Procedure' : 'Stored Procedure'}
            </div>
            <div className="max-h-64 overflow-y-auto space-y-1">
              {spList.map((sp) => (
                <button
                  key={sp}
                  type="button"
                  className={`w-full text-left px-3 py-2 rounded border ${theme.button_default} text-xs`}
                  onClick={() => handleSpSelected(sp)}
                >
                  {sp}
                </button>
              ))}
            </div>
            <button
              type="button"
              className={`mt-3 px-3 py-1 ${theme.button_default} rounded text-xs`}
              onClick={() => {
                setSpSelectorOpen(false);
                setCurrentEditRow(null);
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      )}
    </>
  );
};

export default TransactionUnitDeleteFlowEditor;
