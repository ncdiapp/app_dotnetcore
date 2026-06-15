/**
 * Data Model Data Transfer Management (Internal Data Transfer Data Model)
 * Ported from AngularJS: TransactionDataTransferSettingMgt.cshtml + transactionDataTransferSettingMgtCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionDataTransferSettingMgt.cshtml
 *            C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Transaction\transactionDataTransferSettingMgtCtrl.js
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { PropertyGroupDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import TransactionDataTransferSettingEditor from './TransactionDataTransferSettingEditor';

// EmAppTransactionDataTransferType from Angular/AppEnums
const EmAppTransactionDataTransferType = {
  FromDataModelToMessageTemplate: 1,
  FromDataModelToDataModel: 2,
  FromApiToDataModel: 3,
  FromDataModelToApi: 4,
};

const TRANSFER_TYPE_LIST = [
  { Id: EmAppTransactionDataTransferType.FromDataModelToMessageTemplate, Display: 'From Data Model To Message Template' },
  { Id: EmAppTransactionDataTransferType.FromDataModelToDataModel, Display: 'From Data Model To Data Model' },
  { Id: EmAppTransactionDataTransferType.FromApiToDataModel, Display: 'From Api To Data Model' },
  { Id: EmAppTransactionDataTransferType.FromDataModelToApi, Display: 'From Data Model To Api' },
];

export interface TransactionDataTransferSettingMgtProps {
  transactionId: number | null;
  transactionName?: string | null;
  /** When provided, only show these transfer types (e.g. [3] for Consume, [4] for Provide). */
  filterTransferTypeIds?: number[] | null;
  /** Optional title override for the header area. */
  titleOverride?: string | null;
  onRefresh?: () => void;
}

const TransactionDataTransferSettingMgt: React.FC<TransactionDataTransferSettingMgtProps> = ({
  transactionId,
  transactionName: transactionNameProp,
  filterTransferTypeIds = null,
  titleOverride = null,
  onRefresh,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [transactionName, setTransactionName] = useState(transactionNameProp || '');
  const [dataTransferSettingList, setDataTransferSettingList] = useState<any[]>([]);
  const [transactionDataMap, setTransactionDataMap] = useState<DataMap | null>(null);
  const [createDropdownOpen, setCreateDropdownOpen] = useState(false);
  const [editorState, setEditorState] = useState<{
    isOpen: boolean;
    mode: 'create' | 'edit';
    settingId?: number;
    transferTypeId?: number;
    param2?: { emAppTransactionDataTransferType: number; srcTransactionId: number | null; destinationTransactionId: number | null };
  }>({ isOpen: false, mode: 'create' });

  const gridRef = useRef<any>(null);

  const transferTypeDataMap = React.useMemo(() => new DataMap(TRANSFER_TYPE_LIST, 'Id', 'Display'), []);

  const dataTransferSettingCV = React.useMemo(() => {
    const cv = new CollectionView(dataTransferSettingList);
    cv.groupDescriptions.push(new PropertyGroupDescription('TransferTypeId'));
    return cv;
  }, [dataTransferSettingList]);

  const loadData = useCallback(async () => {
    if (!transactionId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    try {
      const [massEntityData, data] = await Promise.all([
        adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView|AppTransaction'),
        appTransactionService.retrieveAllAppTransactionDataTransferSettingExDto(transactionId, transactionId),
      ]);

      const transactions = massEntityData?.['AppTransaction'];
      if (transactions) {
        setTransactionDataMap(new DataMap(Array.isArray(transactions) ? transactions : [], 'Id', 'Display'));
      } else {
        setTransactionDataMap(null);
      }

      const list = Array.isArray(data) ? data : data?.Items ?? [];
      list.forEach((a: any) => {
        a.TransactionId = a.TransactionId ?? null;
        a.DestinationTransactionId = a.DestinationTransactionId ?? null;
      });
      const filtered = Array.isArray(filterTransferTypeIds) && filterTransferTypeIds.length > 0
        ? list.filter((o: any) => filterTransferTypeIds.includes(o?.TransferTypeId))
        : list;
      setDataTransferSettingList(filtered);
    } catch (e: any) {
      showError(e?.message || 'Failed to load data transfer settings');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, dispatch, showError, filterTransferTypeIds]);

  useEffect(() => {
    loadData();
  }, [transactionId]);

  const handleRefresh = useCallback(() => {
    loadData();
    onRefresh?.();
  }, [loadData, onRefresh]);

  const handleCreate = useCallback(
    (settingTypeLookup: { Id: number; Display: string }) => {
      if (!transactionId || !settingTypeLookup) return;
      const id = settingTypeLookup.Id;
      let param2: { emAppTransactionDataTransferType: number; srcTransactionId: number | null; destinationTransactionId: number | null };
      if (id === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate) {
        setEditorState({
          isOpen: true,
          mode: 'create',
          transferTypeId: id,
          param2: { emAppTransactionDataTransferType: id, srcTransactionId: Number(transactionId), destinationTransactionId: null },
        });
        return;
      }
      if (id === EmAppTransactionDataTransferType.FromDataModelToDataModel) {
        param2 = { emAppTransactionDataTransferType: id, srcTransactionId: Number(transactionId), destinationTransactionId: null };
      } else if (id === EmAppTransactionDataTransferType.FromApiToDataModel) {
        param2 = { emAppTransactionDataTransferType: id, srcTransactionId: null, destinationTransactionId: Number(transactionId) };
      } else if (id === EmAppTransactionDataTransferType.FromDataModelToApi) {
        param2 = { emAppTransactionDataTransferType: id, srcTransactionId: Number(transactionId), destinationTransactionId: null };
      } else {
        return;
      }
      setEditorState({ isOpen: true, mode: 'create', transferTypeId: id, param2 });
      setCreateDropdownOpen(false);
    },
    [transactionId]
  );

  const handleEdit = useCallback(() => {
    const flex = gridRef.current;
    if (!flex?.selection) return;
    const rowIndex = flex.selection.row;
    const row = flex.rows?.[rowIndex]?.dataItem;
    if (!row) return;
    setEditorState({
      isOpen: true,
      mode: 'edit',
      settingId: row.Id,
    });
    setCreateDropdownOpen(false);
  }, []);

  const handleDelete = useCallback(async () => {
    const flex = gridRef.current;
    if (!flex?.selection) return;
    const rowIndex = flex.selection.row;
    const row = flex.rows?.[rowIndex]?.dataItem;
    if (!row?.Id) return;
    const confirmed = await showConfirm('Confirm To Delete', { title: 'Delete Data Transfer Setting' });
    if (!confirmed) return;
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.deleteOneAppTransactionDataTransferSettingExDto(row.Id);
      if (result?.ValidationResult && !result.ValidationResult.IsValid) {
        showValidationMessages(result.ValidationResult, true);
      } else if (result?.Object !== false) {
        showInfo('Data transfer setting deleted.');
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

  useEffect(() => {
    if (!createDropdownOpen) return;
    const onClose = () => setCreateDropdownOpen(false);
    window.addEventListener('click', onClose);
    return () => window.removeEventListener('click', onClose);
  }, [createDropdownOpen]);

  if (!transactionId) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Data Model Data Transfer Management</div>
        </div>
        <div className={`w-full flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Select a data model first to configure Internal Data Transfer.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {titleOverride || `Data Model Data Transfer Management${transactionName ? `: ${transactionName}` : ''}`}
        </div>
        <div className="flex items-center flex-wrap space-x-2">
          <button type="button" onClick={handleRefresh} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-refresh mr-1" aria-hidden="true" /> Refresh
          </button>
          <div className="relative inline-block">
            <button
              type="button"
              onClick={() => setCreateDropdownOpen((v) => !v)}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
            >
              <i className="fa fa-plus mr-1" aria-hidden="true" /> Create <span className="caret" />
            </button>
            {createDropdownOpen && (
              <ul
                className={`absolute left-0 top-full mt-1 py-1 min-w-[200px] rounded-[4px] border shadow-lg z-10 ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                {TRANSFER_TYPE_LIST.map((settingTypeLookup) => (
                  <li key={settingTypeLookup.Id}>
                    <button
                      type="button"
                      className={`w-full text-left px-3 py-1.5 text-xs ${theme.menu_default} flex items-center`}
                      onClick={() => handleCreate(settingTypeLookup)}
                    >
                      {settingTypeLookup.Display}
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
          <button type="button" onClick={handleEdit} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-edit mr-1" aria-hidden="true" /> Edit
          </button>
          <button type="button" onClick={handleDelete} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}>
            <i className="fa fa-trash mr-1" aria-hidden="true" /> Delete
          </button>
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {loading ? (
          <p className={`p-4 text-xs ${theme.label}`}>Loading...</p>
        ) : (
          <FlexGrid
            ref={gridRef}
            itemsSource={dataTransferSettingCV}
            isReadOnly={true}
            selectionMode="Row"
            headersVisibility="Column"
          >
            <FlexGridColumn binding="Id" header="ID" width={60} format="d" />
            <FlexGridColumn binding="Description" header="Description" width={400} />
            <FlexGridColumn binding="TransferTypeId" header="Type" width={150} dataMap={transferTypeDataMap} />
            <FlexGridColumn binding="TransactionId" header="From Data Model" width={200} dataMap={transactionDataMap} />
            <FlexGridColumn binding="DestinationTransactionId" header="To Data Model" width={200} dataMap={transactionDataMap} />
            <FlexGridColumn binding="InternalCode" header="API" width={200} />
            <FlexGridColumn width="*" isReadOnly={true} />
          </FlexGrid>
        )}
      </div>

      {editorState.isOpen && (
        <TransactionDataTransferSettingEditor
          isOpen={true}
          mode={editorState.mode}
          transactionId={transactionId}
          transactionName={transactionName}
          settingId={editorState.settingId}
          transferTypeId={editorState.transferTypeId}
          param2={editorState.param2}
          onClose={handleEditorClose}
          onSaved={handleEditorSaved}
        />
      )}
    </div>
  );
};

export default TransactionDataTransferSettingMgt;
