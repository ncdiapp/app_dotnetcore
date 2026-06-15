import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { adminSvc } from '../../webapi/adminsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import TransactionFolderNavigationQuickBuilder from './TransactionFolderNavigationQuickBuilder';

interface TransactionNavigationEditorProps {
  embedTransactionId?: number | string | null;
  onRefresh?: () => void;
}

const TransactionNavigationEditor: React.FC<TransactionNavigationEditorProps> = ({
  embedTransactionId,
  onRefresh,
}) => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showValidationMessages, showWarning } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const { showConfirm } = useAlertConfirm();

  const folderFlexRef = useRef<any>(null);
  const searchFlexRef = useRef<any>(null);

  const [transactionId, setTransactionId] = useState<number | string | null>(embedTransactionId ?? null);
  const [transactionName, setTransactionName] = useState('');
  const [transactionData, setTransactionData] = useState<any>(null);
  const [searchDataMap, setSearchDataMap] = useState<DataMap | null>(null);
  const [searchViewDataMap, setSearchViewDataMap] = useState<DataMap | null>(null);
  const [folderNavigations, setFolderNavigations] = useState<any[]>([]);
  const [searchNavigations, setSearchNavigations] = useState<any[]>([]);
  const [folderNavigationCV, setFolderNavigationCV] = useState<CollectionView | null>(null);
  const [searchNavigationCV, setSearchNavigationCV] = useState<CollectionView | null>(null);
  const [deletedFolderIds, setDeletedFolderIds] = useState<number[]>([]);
  const [deletedSearchIds, setDeletedSearchIds] = useState<number[]>([]);
  const [rootFolderList, setRootFolderList] = useState<any[]>([]);
  const [selectedRootFolderId, setSelectedRootFolderId] = useState<number | null>(null);
  const [isEnableFolderSecurity, setIsEnableFolderSecurity] = useState(false);
  const [showQuickBuilder, setShowQuickBuilder] = useState(false);

  useEffect(() => {
    if (embedTransactionId != null) {
      setTransactionId(embedTransactionId);
      return;
    }
    if (!param) return;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      setTransactionId(obj.id ?? obj.transactionId ?? null);
    } catch {
      setTransactionId(param);
    }
  }, [param, embedTransactionId]);

  const loadLookupMaps = useCallback(async () => {
    const mass = await adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView|AppTransaction');
    const searches = mass?.AppSearch ?? [];
    const searchViews = mass?.AppSearchView ?? [];
    setSearchDataMap(new DataMap(searches, 'Id', 'Display'));
    setSearchViewDataMap(new DataMap(searchViews, 'Id', 'Display'));
  }, []);

  const loadTransactionAndRootFolders = useCallback(async () => {
    if (!transactionId) return;
    const tx = await appTransactionService.getOneAppTransactionData(String(transactionId));
    setTransactionData(tx);
    setTransactionName(tx?.TransactionName ?? '');
    setSelectedRootFolderId(tx?.MgtRootFolderId ?? null);
    setIsEnableFolderSecurity(Boolean(tx?.IsEnableFolderSecurity));
    const roots = await appTransactionService.retrieveAllRootFolderDtoList(1);
    setRootFolderList(roots);
  }, [transactionId]);

  const loadFolderNavigations = useCallback(async () => {
    if (!transactionId) return;
    const list = await appTransactionService.retrieveFolderViewByTransactionId(transactionId);
    setFolderNavigations(list);
    setFolderNavigationCV(new CollectionView(list));
  }, [transactionId]);

  const loadSearchNavigations = useCallback(async () => {
    if (!transactionId) return;
    const list = await appTransactionService.retrieveQuickSearchByTransactionId(transactionId);
    setSearchNavigations(list);
    setSearchNavigationCV(new CollectionView(list));
  }, [transactionId]);

  const refreshAll = useCallback(async () => {
    if (!transactionId) return;
    dispatch(setIsBusy());
    try {
      await Promise.all([loadLookupMaps(), loadTransactionAndRootFolders(), loadFolderNavigations(), loadSearchNavigations()]);
      onRefresh?.();
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionId, dispatch, loadLookupMaps, loadTransactionAndRootFolders, loadFolderNavigations, loadSearchNavigations, onRefresh]);

  useEffect(() => {
    refreshAll();
  }, [refreshAll]);

  const addFolderRow = () => {
    if (!transactionId) return;
    const newRow = {
      TransactionId: transactionId,
      QuickSearchId: null,
      FolderViewId: null,
      IsDefaultView: false,
      IsNew: true,
    };
    const next = [...folderNavigations, newRow];
    setFolderNavigations(next);
    setFolderNavigationCV(new CollectionView(next));
  };

  const removeFolderRow = async (row?: any) => {
    let selecedDataRow = row;
    if (!selecedDataRow) {
      const flex = folderFlexRef.current?.control ?? folderFlexRef.current;
      const sel = flex?.selection;
      selecedDataRow = flex?.rows?.[sel?.row]?.dataItem;
    }
    if (!selecedDataRow) return;
    const ok = await showConfirm('Please Confirm To Delete This Folder Navigation');
    if (!ok) return;
    if (selecedDataRow.Id) {
      setDeletedFolderIds((prev) => [...prev, selecedDataRow.Id]);
    }
    const next = folderNavigations.filter((n) => n !== selecedDataRow);
    setFolderNavigations(next);
    setFolderNavigationCV(new CollectionView(next));
    await saveFolderNavigation(next);
  };

  const saveFolderNavigation = async (listOverride?: any[]) => {
    if (!transactionId) return;
    if (!selectedRootFolderId && (listOverride ?? folderNavigations).length > 0) {
      showWarning('Please select or create a root folder.');
      return;
    }
    dispatch(setIsBusy());
    try {
      const data = await appTransactionService.saveFolderViewNavigationListExDto({
        TransactionId: Number(transactionId),
        TransactionNavigationExDtoSet: listOverride ?? folderNavigations,
        DeletedItemIds: deletedFolderIds,
        MgtRootFolderId: selectedRootFolderId,
        IsEnableFolderSecurity: isEnableFolderSecurity,
      });
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful && data?.ObjectList) {
        setFolderNavigations(data.ObjectList);
        setFolderNavigationCV(new CollectionView(data.ObjectList));
        setDeletedFolderIds([]);
        await loadTransactionAndRootFolders();
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const saveSearchNavigation = async () => {
    if (!transactionId) return;
    dispatch(setIsBusy());
    try {
      const data = await appTransactionService.saveQuickSearchNavigationListExDto({
        TransactionId: Number(transactionId),
        TransactionNavigationExDtoSet: searchNavigations,
        DeletedItemIds: deletedSearchIds,
      });
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful && data?.ObjectList) {
        setSearchNavigations(data.ObjectList);
        setSearchNavigationCV(new CollectionView(data.ObjectList));
        setDeletedSearchIds([]);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const createRootFolder = async () => {
    const newRootFolder: any = { Name: `Root ${transactionName}` };
    if (transactionData) {
      newRootFolder.FolderType = transactionData.EmAppTransBusinessType;
    }
    dispatch(setIsBusy());
    try {
      const data = await appFolderNavigationService.saveAppSeFolder(newRootFolder);
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful && data?.Object) {
        await loadTransactionAndRootFolders();
        setSelectedRootFolderId(data.Object.Id);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const editSearchView = (viewDto: any, isEditFolderContentView?: boolean) => {
    if (!viewDto?.FolderViewId) return;
    const param2 = JSON.stringify({ isEditFolderContentView: Boolean(isEditFolderContentView) });
    addTabAndNavigate('search-view-editor', 'View Editor', { id: viewDto.FolderViewId, param2 }, true);
  };

  const previewFolderNavigation = () => {
    if (!transactionId) return;
    addTabAndNavigate('transaction-folder-navigation', 'Folder Navigation Preview', { transactionId }, true);
  };

  const rootFolderOptions = useMemo(
    () => rootFolderList.map((f) => ({ value: f.Id, label: f.Name || `Folder ${f.Id}` })),
    [rootFolderList],
  );

  if (!transactionId) {
    return <div className={`p-4 text-xs ${theme.label}`}>Transaction ID is required.</div>;
  }

  return (
    <div className="h-full flex flex-col gap-2 overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          Transaction Navigation Editor {transactionName ? `- ${transactionName}` : ''}
        </div>
        <div className="flex items-center gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={refreshAll}>
            <i className="fa-solid fa-rotate mr-1" />
            Refresh
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => saveFolderNavigation()}>
            <i className="fa-solid fa-floppy-disk mr-1" />
            Save All
          </button>
        </div>
      </div>

      <div className={`px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Quick Search Navigation</div>
        <div className="flex gap-2 mb-2">
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={loadSearchNavigations}>
            Refresh
          </button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={saveSearchNavigation}>
            Save Search Navigation
          </button>
        </div>
        {searchNavigationCV && (
          <FlexGrid ref={searchFlexRef} itemsSource={searchNavigationCV} style={{ height: 140 }} headersVisibility="Column">
            <FlexGridColumn binding="QuickSearchId" header="Search" width={220} dataMap={searchDataMap ?? undefined} />
            <FlexGridColumn binding="IsDefaultView" header="Default" width={80} dataType="Boolean" />
          </FlexGrid>
        )}
      </div>

      <div className={`flex-auto h-1 flex flex-col px-3 py-2 overflow-hidden ${theme.mainContentSection}`}>
        <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Folder Tree Navigation</div>
        <div className="flex flex-wrap items-center gap-3 mb-2">
          <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
            <span className="w-24">Root Folder</span>
            <select
              className={`flex-auto h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              value={selectedRootFolderId ?? ''}
              onChange={(e) => setSelectedRootFolderId(e.target.value ? Number(e.target.value) : null)}
            >
              <option value="">-- Select --</option>
              {rootFolderOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </label>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={createRootFolder}>
            Create Root Folder
          </button>
          <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
            <input type="checkbox" checked={isEnableFolderSecurity} onChange={(e) => setIsEnableFolderSecurity(e.target.checked)} />
            Enable Folder Security
          </label>
        </div>
        <div className="flex flex-wrap gap-2 mb-2">
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={() => setShowQuickBuilder(true)}>
            Folder Tree Quick Builder
          </button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={addFolderRow}>
            Add Row
          </button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={() => removeFolderRow()}>
            Remove Selected
          </button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={previewFolderNavigation}>
            Preview
          </button>
          <button type="button" className={`px-3 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={loadFolderNavigations}>
            Refresh
          </button>
        </div>
        {folderNavigationCV && (
          <div className="h-1 flex-auto overflow-hidden">
            <FlexGrid ref={folderFlexRef} itemsSource={folderNavigationCV} style={{ height: '100%' }} headersVisibility="Column">
              <FlexGridColumn binding="FolderViewId" header="Folder View" width={260} dataMap={searchViewDataMap ?? undefined} />
              <FlexGridColumn binding="IsDefaultView" header="Default" width={80} dataType="Boolean" />
              <FlexGridColumn header="Actions" width={120} isReadOnly>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    if (!item) return null;
                    return (
                      <div className="flex gap-1">
                        <button
                          type="button"
                          className={`px-2 py-0.5 text-xs ${theme.button_default}`}
                          onClick={() => editSearchView(item, true)}
                          title="Edit View"
                        >
                          <i className="fa-solid fa-pencil" />
                        </button>
                        <button
                          type="button"
                          className={`px-2 py-0.5 text-xs ${theme.button_default}`}
                          onClick={() => removeFolderRow(item)}
                          title="Delete"
                        >
                          <i className="fa-solid fa-trash" />
                        </button>
                      </div>
                    );
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>
        )}
      </div>

      {showQuickBuilder && (
        <TransactionFolderNavigationQuickBuilder
          isOpen={showQuickBuilder}
          onClose={() => setShowQuickBuilder(false)}
          onSaved={() => {
            setShowQuickBuilder(false);
            refreshAll();
          }}
          transactionId={transactionId}
        />
      )}
    </div>
  );
};

export default TransactionNavigationEditor;
