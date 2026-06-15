import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../webapi/adminsvc';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import appHelper from '../../helper/appHelper';

type FolderResourceItem = {
  Id?: number | null;
  type: 'Group' | 'User';
  RoleId?: number | null;
  UserId?: number | null;
  Display?: string;
  OrganizationId?: number;
  IsReadOnly?: boolean;
};

type Props = {
  open: boolean;
  onClose: () => void;
  folder: { Id?: number; Name?: string } | null;
  transactionId: string;
  onSaved?: () => void;
};

function extractValidationMessages(vr: any): string[] {
  if (!vr) return [];
  const items = vr.Items ?? (Array.isArray(vr) ? vr : []);
  return (Array.isArray(items) ? items : []).map((i: any) => i?.LocalizedMessage ?? i?.ErrorMessage ?? i?.Message ?? '').filter(Boolean);
}

export default function FolderSecurityEditor({ open, onClose, folder, transactionId, onSaved }: Props) {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [availableItems, setAvailableItems] = useState<FolderResourceItem[]>([]);
  const [selectedItems, setSelectedItems] = useState<FolderResourceItem[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<number[]>([]);
  const [isModified, setIsModified] = useState(false);
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const availableItemsCV = useState(() => new CollectionView<FolderResourceItem>([]))[0];
  const selectedItemsCV = useState(() => new CollectionView<FolderResourceItem>([]))[0];
  const availableGridRef = useRef<any>(null);
  const selectedGridRef = useRef<any>(null);

  const markChange = useCallback(() => setIsModified(true), []);

  const getSelectedDataItems = useCallback((flex: any): FolderResourceItem[] => {
    const items: FolderResourceItem[] = [];
    const ctrl = flex?.control ?? flex;
    if (!ctrl) return items;
    if (ctrl.selectedRows?.length > 0) {
      for (let i = 0; i < ctrl.selectedRows.length; i++) {
        const row = ctrl.selectedRows[i];
        const di = row?.dataItem as FolderResourceItem;
        if (di && (di.RoleId != null || di.UserId != null)) items.push(di);
      }
    }
    if (items.length === 0 && ctrl.rows?.length > 0) {
      for (let i = 0; i < ctrl.rows.length; i++) {
        const row = ctrl.rows[i];
        if (row?.isSelected) {
          const di = row.dataItem as FolderResourceItem;
          if (di && (di.RoleId != null || di.UserId != null)) items.push(di);
        }
      }
    }
    return items;
  }, []);

  const keyOf = (item: FolderResourceItem) =>
    item.RoleId != null ? `r-${item.RoleId}` : item.UserId != null ? `u-${item.UserId}` : '';

  const loadData = useCallback(async () => {
    if (!folder?.Id || !transactionId) return;
    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      const [massData, availData, folderResourceList] = await Promise.all([
        adminSvc.getMassEntitiesLookupItem('AppComOrganization'),
        adminSvc.retrieveTransactionAvailableOrganizationRoleAndUser(transactionId),
        appFolderNavigationService.retrieveOneAppSeFolderResourceList(String(folder.Id)),
      ]);
      const allGroupsAndUsers: FolderResourceItem[] = [];
      const roleList = availData?.RoleList ?? [];
      const userList = availData?.UserList ?? [];
      roleList.forEach((g: any) => {
        allGroupsAndUsers.push({
          type: 'Group',
          RoleId: g.Id,
          Display: g.GroupName ?? g.Display ?? '',
          OrganizationId: g.OrganizationId,
        });
      });
      userList.forEach((u: any) => {
        allGroupsAndUsers.push({
          type: 'User',
          UserId: u.Id,
          Display: u.UserName ?? u.Display ?? '',
          OrganizationId: u.OrganizationId,
        });
      });
      const selected: FolderResourceItem[] = [];
      const resourceList = Array.isArray(folderResourceList) ? folderResourceList : folderResourceList?.ObjectList ?? folderResourceList?.$values ?? [];
      resourceList.forEach((r: any) => {
        const item: FolderResourceItem = {
          Id: r.Id,
          type: r.RoleId != null ? 'Group' : 'User',
          RoleId: r.RoleId ?? undefined,
          UserId: r.UserId ?? undefined,
          Display: r.Display ?? '',
          OrganizationId: r.OrganizationId,
          IsReadOnly: r.IsReadOnly ?? false,
        };
        const found = allGroupsAndUsers.find(
          (a) => (a.RoleId != null && a.RoleId === item.RoleId) || (a.UserId != null && a.UserId === item.UserId)
        );
        if (found) item.Display = found.Display;
        selected.push(item);
      });
      const selectedKeys = new Set(selected.map(keyOf));
      const available = allGroupsAndUsers.filter((a) => !selectedKeys.has(keyOf(a)));
      setAvailableItems(available);
      setSelectedItems(selected);
      setDeletedItemIds([]);
      setIsModified(false);
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Failed to load data']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folder?.Id, transactionId, dispatch]);

  useEffect(() => {
    if (open && folder?.Id && transactionId) loadData();
  }, [open, folder?.Id, transactionId, loadData]);

  useEffect(() => {
    availableItemsCV.sourceCollection = availableItems;
    availableItemsCV.refresh();
  }, [availableItems, availableItemsCV]);
  useEffect(() => {
    selectedItemsCV.sourceCollection = selectedItems;
    selectedItemsCV.refresh();
  }, [selectedItems, selectedItemsCV]);

  const handleAdd = useCallback(() => {
    const flex = availableGridRef.current?.control ?? availableGridRef.current;
    const toAdd = getSelectedDataItems(flex);
    if (toAdd.length === 0) return;
    const keysToRemove = new Set(toAdd.map(keyOf));
    setSelectedItems((prev) => [...prev, ...toAdd.map((x) => ({ ...x, IsReadOnly: false }))]);
    setAvailableItems((prev) => prev.filter((x) => !keysToRemove.has(keyOf(x))));
    markChange();
  }, [getSelectedDataItems, markChange]);

  const handleRemove = useCallback(() => {
    const flex = selectedGridRef.current?.control ?? selectedGridRef.current;
    const toRemove = getSelectedDataItems(flex);
    if (toRemove.length === 0) return;
    const keysToRemove = new Set(toRemove.map(keyOf));
    const newDeleted: number[] = [];
    toRemove.forEach((x) => {
      if (x.Id != null) newDeleted.push(x.Id);
    });
    setDeletedItemIds((prev) => [...prev, ...newDeleted]);
    setAvailableItems((prev) => [...prev, ...toRemove]);
    setSelectedItems((prev) => prev.filter((x) => !keysToRemove.has(keyOf(x))));
    markChange();
  }, [getSelectedDataItems, markChange]);

  const handleSave = useCallback(async () => {
    if (!folder?.Id || !transactionId || !isModified) return;
    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      const payload = {
        FolderId: folder.Id,
        TransactionId: Number(transactionId),
        AppSefolderResourceExDtoSet: selectedItems.map((s) => ({
          Id: s.Id,
          RoleId: s.RoleId,
          UserId: s.UserId,
          Display: s.Display,
          OrganizationId: s.OrganizationId,
          IsReadOnly: s.IsReadOnly ?? false,
        })),
        DeletedItemIds: deletedItemIds,
      };
      const data = await appFolderNavigationService.saveAppSefolderResource(payload);
      const messages = extractValidationMessages(data?.ValidationResult ?? null);
      if (data?.IsSuccessful) {
        onSaved?.();
        setIsModified(false);
        setDeletedItemIds([]);
        await loadData();
      } else {
        setErrorMessages(messages.length ? messages : [data?.Message ?? 'Save failed']);
      }
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Save failed']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folder?.Id, transactionId, selectedItems, deletedItemIds, isModified, dispatch, onSaved, loadData]);

  const handleApplyToSubFolders = useCallback(async () => {
    if (!folder?.Id || !transactionId) return;
    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      const payload = {
        FolderId: folder.Id,
        TransactionId: Number(transactionId),
        AppSefolderResourceExDtoSet: selectedItems.map((s) => ({
          Id: s.Id,
          RoleId: s.RoleId,
          UserId: s.UserId,
          Display: s.Display,
          OrganizationId: s.OrganizationId,
          IsReadOnly: s.IsReadOnly ?? false,
        })),
      };
      const data = await appFolderNavigationService.applySecurityToSubFolders(payload);
      if (data?.IsSuccessful) {
        onSaved?.();
      }
      const messages = extractValidationMessages(data?.ValidationResult ?? null);
      if (messages.length) setErrorMessages(messages);
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Apply failed']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folder?.Id, transactionId, selectedItems, dispatch, onSaved]);

  const handleRemoveFromSubFolders = useCallback(async () => {
    if (!folder?.Id || !transactionId) return;
    if (!window.confirm('Remove security from all subfolders?')) return;
    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      const data = await appFolderNavigationService.removeSecurityFromSubFolders(folder.Id, Number(transactionId));
      if (data?.IsSuccessful) {
        onSaved?.();
      }
      const messages = extractValidationMessages(data?.ValidationResult ?? null);
      if (messages.length) setErrorMessages(messages);
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Remove failed']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [folder?.Id, transactionId, dispatch, onSaved]);

  const handleSelectedCellEditEnded = useCallback(() => {
    selectedItemsCV.refresh();
    markChange();
  }, [selectedItemsCV, markChange]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/30" onClick={onClose}>
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl flex flex-col overflow-hidden border`}
        style={{ width: 1080, maxWidth: '95vw', height: 600 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-sm font-semibold ${theme.title}`}>
            {`Folder Security${folder?.Name ? `: ${folder.Name}` : ''}`}
          </span>
          <div className="flex items-center gap-2">
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={loadData} title="Refresh">
              <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handleSave}
              disabled={!isModified}
              title="Save"
            >
              <i className="fa-solid fa-save mr-1" aria-hidden /> Save
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => { void handleApplyToSubFolders(); }}
              title="Apply Security To Subfolders"
            >
              Apply To Subfolders
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => { void handleRemoveFromSubFolders(); }}
              title="Remove Security From Subfolders"
            >
              Remove From Subfolders
            </button>
            <button type="button" className="text-lg leading-none w-8 h-8" onClick={onClose} aria-label="Close">
              &times;
            </button>
          </div>
        </div>
        {errorMessages.length > 0 && (
          <div className="px-3 py-1 text-xs text-red-600 dark:text-red-400">
            {errorMessages.map((m, i) => (
              <div key={i}>{m}</div>
            ))}
          </div>
        )}
        <div className="flex-1 flex min-h-0 px-3 py-3 gap-2">
          <div className="flex flex-col flex-1 min-w-0">
            <div className={`text-xs ${theme.label} mb-1`}>Available Groups And Users</div>
            <div className="flex-1 min-h-0">
              <FlexGrid
                ref={availableGridRef}
                itemsSource={availableItemsCV}
                selectionMode="MultiRange"
                headersVisibility="Column"
                isReadOnly={true}
                className="h-full w-full"
              >
                <FlexGridColumn header="Type" binding="type" width={80} />
                <FlexGridColumn header="Name" binding="Display" width="*" />
              </FlexGrid>
            </div>
          </div>
          <div className="flex flex-col justify-center gap-1">
            <button type="button" className={`p-2 ${theme.button_default} rounded`} onClick={handleAdd} title="Add selected">
              <i className="fa-solid fa-chevron-right" aria-hidden />
            </button>
            <button type="button" className={`p-2 ${theme.button_default} rounded`} onClick={handleRemove} title="Remove selected">
              <i className="fa-solid fa-chevron-left" aria-hidden />
            </button>
          </div>
          <div className="flex flex-col flex-1 min-w-0">
            <div className={`text-xs ${theme.label} mb-1`}>Selected Groups And Users</div>
            <div className="flex-1 min-h-0">
              <FlexGrid
                ref={selectedGridRef}
                itemsSource={selectedItemsCV}
                selectionMode="MultiRange"
                headersVisibility="Column"
                className="h-full w-full"
                cellEditEnded={handleSelectedCellEditEnded}
              >
                <FlexGridColumn header="Type" binding="type" width={80} isReadOnly={true} />
                <FlexGridColumn header="Name" binding="Display" width="*" isReadOnly={true} />
                <FlexGridColumn header="Readonly" binding="IsReadOnly" width={100} dataType="Boolean" />
              </FlexGrid>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
