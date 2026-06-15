import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';

const STAGES = {
  transaction: { id: 1, label: 'Please Select a Navigation Form' },
  rootFolder: { id: 2, label: 'Select or Create Folder Tree' },
  viewFields: { id: 3, label: 'Select View Fields' },
} as const;

interface TransactionFolderNavigationQuickBuilderProps {
  isOpen: boolean;
  onClose: () => void;
  onSaved?: () => void;
  applicationId?: string | number | null;
  transactionId?: number | string | null;
  transactionList?: any[];
}

const TransactionFolderNavigationQuickBuilder: React.FC<TransactionFolderNavigationQuickBuilderProps> = ({
  isOpen,
  onClose,
  onSaved,
  applicationId,
  transactionId: initialTransactionId,
  transactionList,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showValidationMessages } = useErrorMessage();
  const flexRef = useRef<any>(null);

  const [stage, setStage] = useState<(typeof STAGES)[keyof typeof STAGES]>(STAGES.transaction);
  const [availableTransactions, setAvailableTransactions] = useState<any[]>(transactionList ?? []);
  const [rootFolderList, setRootFolderList] = useState<any[]>([]);
  const [rootFolderCV, setRootFolderCV] = useState<CollectionView | null>(null);
  const [currentTransaction, setCurrentTransaction] = useState<any | null>(null);
  const [currentRootFolder, setCurrentRootFolder] = useState<any | null>(null);
  const [databaseViewUpdateDto, setDatabaseViewUpdateDto] = useState<any | null>(null);
  const [viewFieldCV, setViewFieldCV] = useState<CollectionView | null>(null);
  const [isSelectAllFields, setIsSelectAllFields] = useState(true);

  const isApplicationMode = applicationId != null && applicationId !== '' && !initialTransactionId;

  const loadRootFolders = useCallback(async () => {
    const list = await appTransactionService.retrieveAllRootFolderDtoList(1);
    setRootFolderList(list);
    setRootFolderCV(new CollectionView(list));
  }, []);

  const selectTransaction = useCallback(
    async (txId: number | string, txDto?: any) => {
      dispatch(setIsBusy());
      try {
        let tx = txDto;
        if (!tx) {
          tx = await appTransactionService.getOneAppTransactionData(String(txId));
        }
        setCurrentTransaction(tx);
        const data = await appTransactionService.buildAdvancedDBViewDtoFromTransaction(txId, true);
        const payload = data?.Object ?? data;
        if (data?.IsSuccessful === false && data?.ValidationResult) {
          showValidationMessages(data.ValidationResult, true);
          return;
        }
        if (!payload?.OrgViewDto) {
          showValidationMessages(data?.ValidationResult ?? null, true);
          return;
        }
        const viewDto = payload.OrgViewDto;
        if (viewDto.ErrorMessage) {
          window.alert(viewDto.ErrorMessage);
          return;
        }
        setDatabaseViewUpdateDto(payload);
        setViewFieldCV(new CollectionView(viewDto.SelectedColumnsList ?? []));
        setStage(STAGES.rootFolder);
      } catch (e) {
        window.alert(e instanceof Error ? e.message : 'Failed to load transaction view');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, showValidationMessages],
  );

  useEffect(() => {
    if (!isOpen) return;
    const init = async () => {
      dispatch(setIsBusy());
      try {
        await loadRootFolders();
        if (isApplicationMode) {
          setStage(STAGES.transaction);
          if (!transactionList?.length && applicationId) {
            const list = await appTransactionService.retrieveSaasApplicationTransactionList(applicationId);
            const masterDetail = (Array.isArray(list) ? list : []).filter(
              (t: any) => t.TransactionOrganizedType === 1 && !t.OtherOptions?.IsApiIntegrationTransaction,
            );
            setAvailableTransactions(masterDetail);
          }
        } else if (initialTransactionId) {
          setStage(STAGES.rootFolder);
          await selectTransaction(initialTransactionId);
        }
      } finally {
        dispatch(setIsNotBusy());
      }
    };
    init();
  }, [isOpen, isApplicationMode, initialTransactionId, applicationId, transactionList, loadRootFolders, selectTransaction, dispatch]);

  const selectRootFolder = (folder: any) => {
    setCurrentRootFolder(folder);
    setDatabaseViewUpdateDto((prev: any) => (prev ? { ...prev, RootFolderId: folder.Id } : prev));
    setStage(STAGES.viewFields);
  };

  const createNewRootFolder = async () => {
    if (!currentTransaction) return;
    dispatch(setIsBusy());
    try {
      const newRootFolder: any = { Name: currentTransaction.TransactionName || currentTransaction.Name || 'Root' };
      newRootFolder.FolderType = currentTransaction.EmAppTransBusinessType ?? 1;
      const data = await appFolderNavigationService.saveAppSeFolder(newRootFolder);
      if (data?.IsSuccessful && data?.Object) {
        await loadRootFolders();
        selectRootFolder(data.Object);
      } else {
        showValidationMessages(data?.ValidationResult ?? null, true);
      }
    } catch (e) {
      window.alert(e instanceof Error ? e.message : 'Failed to create root folder');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleCellEditEnded = (sender: any, e: any) => {
    const flex = sender?.control ?? sender;
    const rowData = flex?.rows?.[e.row]?.dataItem;
    const col = flex?.columns?.[e.col];
    if (!rowData || !col || col.binding !== 'IsFolderIdKeyField') return;
    if (rowData.IsFolderIdKeyField) {
      rowData.IsUsedAsViewField = true;
      const cols = databaseViewUpdateDto?.OrgViewDto?.SelectedColumnsList ?? [];
      cols.forEach((item: any) => {
        if (item !== rowData) item.IsFolderIdKeyField = false;
      });
      flex.invalidate();
      setViewFieldCV(new CollectionView([...cols]));
    }
  };

  const toggleAllFields = () => {
    const cols = databaseViewUpdateDto?.OrgViewDto?.SelectedColumnsList ?? [];
    const next = !isSelectAllFields;
    cols.forEach((item: any) => {
      item.IsUsedAsViewField = next;
    });
    setIsSelectAllFields(next);
    setViewFieldCV(new CollectionView([...cols]));
  };

  const handleSave = async () => {
    if (!databaseViewUpdateDto) return;
    dispatch(setIsBusy());
    try {
      const dto = { ...databaseViewUpdateDto, IsEnableFolderSecurity: false };
      const data = await schemaMetadataService.saveDataSetAndCreateFolderViewNavigation(dto);
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful) {
        onSaved?.();
        onClose();
      }
    } catch (e) {
      window.alert(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center bg-black/40" onClick={onClose}>
      <div
        className={`w-[520px] max-h-[90vh] flex flex-col rounded-[4px] shadow-xl border ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Folder Tree Navigation Quick Builder</div>
          <button type="button" className={`px-2 py-1 text-xs ${theme.button_default}`} onClick={onClose}>
            <i className="fa-solid fa-xmark" />
          </button>
        </div>

        <div className={`px-4 py-2 text-xs ${theme.label} border-b`}>{stage.label}</div>

        <div className="h-[480px] flex-auto overflow-auto p-4">
          {stage.id === STAGES.transaction.id && (
            <div className="flex flex-col gap-2">
              {availableTransactions.map((tx) => (
                <button
                  key={tx.Id}
                  type="button"
                  className={`text-left px-3 py-2 text-xs rounded-[4px] ${theme.contextMenu}`}
                  onClick={() => selectTransaction(tx.Id, tx)}
                >
                  {tx.TransactionName || tx.Name} ({tx.Id})
                </button>
              ))}
              {availableTransactions.length === 0 && (
                <p className={`text-xs ${theme.label}`}>No Master Detail transactions found.</p>
              )}
            </div>
          )}

          {stage.id === STAGES.rootFolder.id && (
            <div className="h-full flex flex-col gap-2">
              <button
                type="button"
                className={`self-start px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                onClick={createNewRootFolder}
              >
                <i className="fa-solid fa-plus mr-1" />
                Create New Root Folder
              </button>
              {rootFolderCV && (
                <FlexGrid
                  itemsSource={rootFolderCV}
                  selectionMode="Row"
                  isReadOnly
                  headersVisibility="Column"
                  style={{ height: 360 }}
                  selectionChanged={(s: any) => {
                    const flex = s?.control ?? s;
                    const row = flex?.selection?.row;
                    const item = row != null ? flex?.rows?.[row]?.dataItem : null;
                    if (item) selectRootFolder(item);
                  }}
                >
                  <FlexGridColumn binding="Name" header="Root Folder" width="*" />
                  <FlexGridColumn binding="Id" header="ID" width={80} />
                </FlexGrid>
              )}
            </div>
          )}

          {stage.id === STAGES.viewFields.id && viewFieldCV && (
            <div className="h-full flex flex-col gap-2">
              <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
                <input type="checkbox" checked={isSelectAllFields} onChange={toggleAllFields} />
                Select All Fields
              </label>
              <FlexGrid
                ref={flexRef}
                itemsSource={viewFieldCV}
                selectionMode="Cell"
                headersVisibility="Column"
                style={{ height: 380 }}
                cellEditEnded={handleCellEditEnded}
              >
                <FlexGridColumn binding="ColumnDisplayName" header="Field" width={180} isReadOnly />
                <FlexGridColumn binding="IsUsedAsViewField" header="Use" width={60} dataType="Boolean" />
                <FlexGridColumn binding="IsFolderIdKeyField" header="Folder Id Key" width={100} dataType="Boolean" />
                <FlexGridColumn binding="SearchViewDisplayName" header="Display Name" width="*" />
              </FlexGrid>
            </div>
          )}
        </div>

        <div className={`flex justify-end gap-2 px-4 py-3 border-t ${theme.mainContentSection}`}>
          {stage.id === STAGES.viewFields.id && (
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleSave}>
              <i className="fa-solid fa-floppy-disk mr-1" />
              Save
            </button>
          )}
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
};

export default TransactionFolderNavigationQuickBuilder;
