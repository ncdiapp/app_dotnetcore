import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

type Props = {
  groupUsage: number;
  companyId?: string | number | null;
  groupId: string | null;
  onClose: () => void;
  onSaved: () => void;
};

/** Role User Type options — align with Angular SecurityGroupEditor emAppUserTypeList */
const ROLE_USER_TYPE_LIST = [
  { Id: null as number | null, Display: 'All Users' },
  { Id: 2, Display: 'Employee' },
  { Id: 3, Display: 'Customer' },
  { Id: 4, Display: 'Supplier' },
  { Id: 5, Display: 'Client Agent' },
  { Id: 9, Display: 'Supplier Agent' },
];

const USER_TYPE_ID_TO_DISPLAY: Record<number, string> = {
  2: 'Employee',
  3: 'Customer',
  4: 'Supplier',
  5: 'Client Agent',
  9: 'Supplier Agent',
};

type UserItem = { Id: number; Display: string; UserTypeName: string };

const RoleEditorModal: React.FC<Props> = ({ groupUsage, companyId, groupId, onClose, onSaved }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [groupName, setGroupName] = useState('');
  const [description, setDescription] = useState('');
  const [roleUserTypeId, setRoleUserTypeId] = useState<number | null>(null);
  const [loading, setLoading] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [allUserItems, setAllUserItems] = useState<UserItem[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<number[]>([]);
  const [initialMemberList, setInitialMemberList] = useState<Array<{ Id?: number; UserId: number }>>([]);
  const availableGridRef = useRef<any>(null);
  const selectedGridRef = useRef<any>(null);

  const loadAllUsers = useCallback(async () => {
    try {
      const allUsers = await adminSvc.retrieveAllAppSecurityUserDto();
      const list = Array.isArray(allUsers) ? allUsers : [];
      const items: UserItem[] = list.map((u: any) => ({
        Id: u.Id,
        Display: (u.OrganizationPath ? u.OrganizationPath + '/ ' : '') + (u.UserName || u.LoginName || String(u.Id)),
        UserTypeName: USER_TYPE_ID_TO_DISPLAY[u.DomainId] ?? 'All Types',
      }));
      setAllUserItems(items);
      return items;
    } catch {
      setAllUserItems([]);
      return [];
    }
  }, []);

  const loadGroup = useCallback(async () => {
    if (groupId) {
      setLoading(true);
      try {
        const data: any = await adminSvc.retrieveOneAppSecurityGroupExDto(groupId);
        setGroupName(data?.GroupName ?? '');
        setDescription(data?.Description ?? '');
        setRoleUserTypeId(data?.RoleUserTypeId ?? null);
        const members = data?.AppSecurityGroupMemberList ?? [];
        setInitialMemberList(members);
        setSelectedUserIds(members.map((m: any) => m.UserId));
      } catch {
        setGroupName('');
        setDescription('');
        setRoleUserTypeId(null);
        setSelectedUserIds([]);
        setInitialMemberList([]);
      } finally {
        setLoading(false);
      }
    } else {
      setGroupName('New Group');
      setDescription('');
      setRoleUserTypeId(null);
      setSelectedUserIds([]);
      setInitialMemberList([]);
    }
  }, [groupId]);

  const refresh = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      await loadAllUsers();
      await loadGroup();
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [loadAllUsers, loadGroup, dispatch]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      const users = await loadAllUsers();
      if (cancelled) return;
      await loadGroup();
      if (cancelled) return;
      // Build available/selected CVs after state updates
    })();
    return () => { cancelled = true; };
  }, [groupId, loadAllUsers, loadGroup]);

  const availableItems = useMemo(
    () => allUserItems.filter((u) => !selectedUserIds.includes(u.Id)),
    [allUserItems, selectedUserIds]
  );
  const selectedItems = useMemo(
    () => allUserItems.filter((u) => selectedUserIds.includes(u.Id)),
    [allUserItems, selectedUserIds]
  );

  // Stable CV refs so FlexGrid never gets new itemsSource reference
  const availableCV = useRef(new CollectionView<UserItem>([])).current;
  const selectedCV = useRef(new CollectionView<UserItem>([])).current;
  // Defer CV refresh to next frame so FlexGrid does not call finishEditing(null) during update
  useEffect(() => {
    availableCV.sourceCollection = availableItems;
    const id = requestAnimationFrame(() => {
      try {
        availableCV.refresh();
      } catch {
        // ignore Wijmo refresh/finishEditing race
      }
    });
    return () => cancelAnimationFrame(id);
  }, [availableItems, availableCV]);
  useEffect(() => {
    selectedCV.sourceCollection = selectedItems;
    const id = requestAnimationFrame(() => {
      try {
        selectedCV.refresh();
      } catch {
        // ignore Wijmo refresh/finishEditing race
      }
    });
    return () => cancelAnimationFrame(id);
  }, [selectedItems, selectedCV]);

  // Only mount the two FlexGrids two frames after content is ready so CV refresh has run and FlexGrid won't hit finishEditing(null)
  const [gridsReady, setGridsReady] = useState(false);
  useEffect(() => {
    if (loading) {
      setGridsReady(false);
      return;
    }
    let id1: number | undefined;
    const id2 = requestAnimationFrame(() => {
      id1 = requestAnimationFrame(() => setGridsReady(true));
    });
    return () => {
      cancelAnimationFrame(id2);
      if (id1 != null) cancelAnimationFrame(id1);
    };
  }, [loading]);

  const moveToSelected = useCallback(() => {
    const grid = availableGridRef.current?.control ?? availableGridRef.current;
    if (!grid?.selectedRows?.length) return;
    const idsToAdd: number[] = [];
    for (let i = 0; i < grid.selectedRows.length; i++) {
      const row = grid.selectedRows[i];
      const item = row?.dataItem as UserItem | undefined;
      if (item?.Id != null) idsToAdd.push(item.Id);
    }
    if (idsToAdd.length === 0) return;
    setSelectedUserIds((prev) => {
      const combined = prev.concat(idsToAdd);
      return combined.filter((id, i) => combined.indexOf(id) === i);
    });
  }, []);
  const moveToAvailable = useCallback(() => {
    const grid = selectedGridRef.current?.control ?? selectedGridRef.current;
    if (!grid?.selectedRows?.length) return;
    const idsToRemove: number[] = [];
    for (let i = 0; i < grid.selectedRows.length; i++) {
      const row = grid.selectedRows[i];
      const item = row?.dataItem as UserItem | undefined;
      if (item?.Id != null) idsToRemove.push(item.Id);
    }
    if (idsToRemove.length === 0) return;
    setSelectedUserIds((prev) => prev.filter((id) => !idsToRemove.includes(id)));
  }, []);

  const handleSave = useCallback(async () => {
    if (!groupName.trim()) {
      setErrors(['Group name is required.']);
      return;
    }
    dispatch(setIsBusy());
    try {
      const initialByUserId = new Map(initialMemberList.map((m) => [m.UserId, m]));
      const deletedIds: number[] = [];
      initialMemberList.forEach((m) => {
        if (m.Id != null && !selectedUserIds.includes(m.UserId)) deletedIds.push(m.Id);
      });
      const appSecurityGroupMemberList: Array<{ Id?: number; UserId: number }> = selectedUserIds.map((userId) => {
        const existing = initialByUserId.get(userId);
        return existing?.Id != null ? { Id: existing.Id, UserId: userId } : { UserId: userId };
      });

      const payload: any = {
        GroupName: groupName.trim(),
        Description: description.trim(),
        RoleUserTypeId: roleUserTypeId,
        GroupUsage: groupUsage,
        AppSecurityGroupMemberList: appSecurityGroupMemberList,
        DictDeletedItemsIds: { AppSecurityGroupMemberList: deletedIds },
      };

      if (groupId) payload.Id = groupId;
      payload.AppCreatedByCompanyId = companyId;

      const data = await adminSvc.saveAppSecurityGroupExDto(payload);
      const errs = data?.ValidationResult
        ? (Array.isArray(data.ValidationResult) ? data.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean) : [])
        : [];
      setErrors(errs);
      if (data?.IsSuccessful) {
        onSaved();
        onClose();
      }
    } catch (e) {
      setErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    groupName,
    description,
    roleUserTypeId,
    groupUsage,
    groupId,
    companyId,
    initialMemberList,
    selectedUserIds,
    dispatch,
    onSaved,
    onClose,
  ]);

  const userTypeCV = React.useMemo(() => new CollectionView(ROLE_USER_TYPE_LIST), []);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl flex flex-col`}
        style={{ width: 600, maxWidth: '95vw', height: 600, maxHeight: '90vh' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-md font-semibold ${theme.title}`}>Group Editor</span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={refresh}
              disabled={loading}
            >
              Refresh
            </button>
            <button
              type="button"
              className="px-3 py-1.5 text-sm rounded-[4px] bg-orange-400 text-white hover:bg-orange-500 disabled:opacity-50"
              onClick={handleSave}
              disabled={loading}
            >
              Save
            </button>
            <button type="button" className="text-lg leading-none w-6 h-6" onClick={onClose} aria-label="Close">
              &times;
            </button>
          </div>
        </div>
        <div className="flex flex-col overflow-auto px-4 py-3 h-1 flex-auto min-h-0">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : !gridsReady ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : (
            <>
              <div className="flex flex-wrap gap-x-4 gap-y-2 mb-3">
                <div className="flex items-center">
                  <label className={`w-24 text-xs ${theme.label} mr-2 shrink-0`}>Group Name</label>
                  <input
                    type="text"
                    className={`w-48 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    value={groupName}
                    onChange={(e) => setGroupName(e.target.value)}
                  />
                </div>
                <div className="flex items-center">
                  <label className={`w-24 text-xs ${theme.label} mr-2 shrink-0`}>Description</label>
                  <input
                    type="text"
                    className={`w-48 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                  />
                </div>
                {groupUsage !== 4 && (
                  <div className="flex items-center">
                    <label className={`w-24 text-xs ${theme.label} mr-2 shrink-0`}>Role User Type</label>
                    <ComboBox
                      itemsSource={userTypeCV}
                      displayMemberPath="Display"
                      selectedValuePath="Id"
                      selectedValue={roleUserTypeId}
                      selectedIndexChanged={(s: any) => setRoleUserTypeId(s.selectedValue)}
                      isRequired={false}
                      placeholder="All Users"
                      style={{ width: 140, height: 28 }}
                    />
                  </div>
                )}
              </div>
              {errors.length > 0 && (
                <div className="text-red-600 text-xs mb-2">
                  {errors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
              <div className={`text-xs ${theme.label} mb-1`}>Available User</div>
              <div className="flex gap-2 h-1 flex-auto min-h-[200px]">
                <div className={`w-1 flex-auto border ${theme.inputBox} overflow-hidden rounded-[4px]`}>
                  <FlexGrid
                    ref={availableGridRef}
                    itemsSource={availableCV}
                    selectionMode="ListBox"
                    headersVisibility="Column"
                    isReadOnly={true}
                    style={{ height: '100%' }}
                  >
                    <FlexGridColumn binding="Display" header="User" width="*" />
                    <FlexGridColumn binding="UserTypeName" header="Type" width={80} />
                  </FlexGrid>
                </div>
                <div className="flex flex-col justify-center gap-1">
                  <button
                    type="button"
                    className={`w-8 h-7 rounded-[4px] ${theme.button_default}`}
                    title="Add selected to Selected User"
                    onClick={moveToSelected}
                  >
                    <i className="fa-solid fa-chevron-right" aria-hidden />
                  </button>
                  <button
                    type="button"
                    className={`w-8 h-7 rounded-[4px] ${theme.button_default}`}
                    title="Remove selected to Available User"
                    onClick={moveToAvailable}
                  >
                    <i className="fa-solid fa-chevron-left" aria-hidden />
                  </button>
                </div>
                <div className={`w-1 flex-auto border ${theme.inputBox} overflow-hidden rounded-[4px] flex flex-col`}>
                  <div className={`text-xs ${theme.label} px-1 py-0.5 shrink-0`}>Selected User</div>
                  <div className="h-1 flex-auto min-h-0">
                    <FlexGrid
                      ref={selectedGridRef}
                      itemsSource={selectedCV}
                      selectionMode="ListBox"
                      headersVisibility="Column"
                      isReadOnly={true}
                      style={{ height: '100%' }}
                    >
                      <FlexGridColumn binding="Display" header="User" width="*" />
                      <FlexGridColumn binding="UserTypeName" header="Type" width={80} />
                    </FlexGrid>
                  </div>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default RoleEditorModal;
