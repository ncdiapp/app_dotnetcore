import React, { useCallback, useEffect, useState, useMemo } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { dashboardService } from '../../../webapi/dashboardsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

type Props = {
  groupId: string | number | null;
  groupUsage: number;
  companyId?: string | number | null;
  onClose: () => void;
  onSaved: () => void;
};

type GroupData = {
  Id?: number | null;
  GroupName: string;
  Description: string;
  GroupUsage: number;
  IsBuiltIn: boolean;
  IsAllowedToAddAvailableUser: boolean;
  RoleUserTypeId?: number | null;
  BusinessPartnerId?: string | number | null;
  AppCreatedByCompanyId?: string | number | null;
  DefaultDesktopId?: number | null;
  AppSecurityGroupMemberList?: Array<{ Id?: number; UserId: number }>;
  DictDeletedItemsIds?: { AppSecurityGroupMemberList: number[] };
};

type UserItem = {
  Id: number;
  Display: string;
  UserTypeName?: string;
};

const USER_TYPE_OPTIONS = [
  { Id: null, Display: 'All Users' },
  { Id: 2, Display: 'Employee' },
  { Id: 3, Display: 'Customer' },
  { Id: 4, Display: 'Supplier' },
  { Id: 5, Display: 'Client Agent' },
  { Id: 9, Display: 'Supplier Agent' },
];

const USER_TYPE_MAP: Record<number, string> = {
  2: 'Employee',
  3: 'Customer',
  4: 'Supplier',
  5: 'Client Agent',
  9: 'Supplier Agent',
};

const SecurityGroupEditor: React.FC<Props> = ({
  groupId,
  groupUsage,
  companyId,
  onClose,
  onSaved,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [groupData, setGroupData] = useState<GroupData | null>(null);
  const [allUsers, setAllUsers] = useState<UserItem[]>([]);
  const [selectedUserIds, setSelectedUserIds] = useState<Set<number>>(new Set());
  const [desktopCV, setDesktopCV] = useState<CollectionView | null>(null);
  const [userTypeCV] = useState<CollectionView>(() => new CollectionView(USER_TYPE_OPTIONS));
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);

  // Split users into available and selected
  const availableUsers = useMemo(() => {
    return allUsers.filter((u) => !selectedUserIds.has(u.Id));
  }, [allUsers, selectedUserIds]);

  const selectedUsers = useMemo(() => {
    return allUsers.filter((u) => selectedUserIds.has(u.Id));
  }, [allUsers, selectedUserIds]);

  const [availableCV, setAvailableCV] = useState<CollectionView | null>(null);
  const [selectedCV, setSelectedCV] = useState<CollectionView | null>(null);

  useEffect(() => {
    setAvailableCV(new CollectionView(availableUsers));
  }, [availableUsers]);

  useEffect(() => {
    setSelectedCV(new CollectionView(selectedUsers));
  }, [selectedUsers]);

  useEffect(() => {
    let cancelled = false;
    const loadData = async () => {
      setLoading(true);
      try {
        // Load all users
        const usersData = await adminSvc.retrieveAllAppSecurityUserDto();
        const userItems: UserItem[] = (usersData || []).map((u: any) => ({
          Id: u.Id,
          Display: ((u.OrganizationPath) ? u.OrganizationPath + '/ ' : '') + u.UserName,
          UserTypeName: USER_TYPE_MAP[u.DomainId] || 'Other',
        }));
        if (cancelled) return;
        setAllUsers(userItems);

        if (groupId) {
          // Load existing group
          const [group, desktops] = await Promise.all([
            adminSvc.retrieveOneAppSecurityGroupExDto(String(groupId)),
            dashboardService.retrieveDesktopDtoListByRoleId(String(groupId)),
          ]);
          if (cancelled) return;

          setDesktopCV(new CollectionView(Array.isArray(desktops) ? desktops : []));

          if (group) {
            setGroupData({
              ...group,
              DictDeletedItemsIds: { AppSecurityGroupMemberList: [] },
              DefaultDesktopId: group.DefaultDesktopId ?? null,
            });

            // Set selected user IDs from existing members
            const memberUserIds = (group.AppSecurityGroupMemberList || []).map((m: any) => m.UserId);
            setSelectedUserIds(new Set(memberUserIds));
          }
        } else {
          // New group (no OrganizationId; desktops loaded after save when role has Id)
          setDesktopCV(new CollectionView<any>([]));
          setGroupData({
            GroupName: 'New Group',
            Description: '',
            GroupUsage: groupUsage,
            IsBuiltIn: false,
            IsAllowedToAddAvailableUser: true,
            RoleUserTypeId: null,
            AppCreatedByCompanyId: companyId ?? null,
            AppSecurityGroupMemberList: [],
            DictDeletedItemsIds: { AppSecurityGroupMemberList: [] },
            DefaultDesktopId: null,
          });
          setSelectedUserIds(new Set());
        }
      } catch (e) {
        if (!cancelled) {
          setErrors([e instanceof Error ? e.message : 'Failed to load data']);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };
    loadData();
    return () => { cancelled = true; };
  }, [groupId, groupUsage, companyId]);

  const handleFieldChange = useCallback((field: keyof GroupData, value: any) => {
    setGroupData((prev) => prev ? { ...prev, [field]: value } : null);
  }, []);

  const addUser = useCallback((userId: number) => {
    setSelectedUserIds((prev) => new Set([...Array.from(prev), userId]));
  }, []);

  const removeUser = useCallback((userId: number) => {
    setSelectedUserIds((prev) => {
      const next = new Set(prev);
      next.delete(userId);
      return next;
    });
  }, []);

  const addAllUsers = useCallback(() => {
    setSelectedUserIds(new Set(allUsers.map((u) => u.Id)));
  }, [allUsers]);

  const removeAllUsers = useCallback(() => {
    setSelectedUserIds(new Set());
  }, []);

  const handleSave = useCallback(async () => {
    if (!groupData) return;
    if (!groupData.GroupName?.trim()) {
      setErrors(['Group Name is required']);
      return;
    }

    dispatch(setIsBusy());
    try {
      // Prepare member list changes
      const originalMemberUserIds = new Set(
        (groupData.AppSecurityGroupMemberList || []).map((m) => m.UserId)
      );
      const deletedMemberIds: number[] = [];
      const newMembers: Array<{ UserId: number }> = [];

      // Find deleted members
      (groupData.AppSecurityGroupMemberList || []).forEach((m) => {
        if (!selectedUserIds.has(m.UserId) && m.Id) {
          deletedMemberIds.push(m.Id);
        }
      });

      // Find new members
      selectedUserIds.forEach((userId) => {
        if (!originalMemberUserIds.has(userId)) {
          newMembers.push({ UserId: userId });
        }
      });

      // Omit OrganizationId if present (e.g. from loaded entity); API does not accept it
      const { OrganizationId: _omitOrg, ...restGroupData } = groupData as GroupData & { OrganizationId?: unknown };
      const saveData = {
        ...restGroupData,
        AppSecurityGroupMemberList: [
          ...(groupData.AppSecurityGroupMemberList || []).filter((m) => selectedUserIds.has(m.UserId)),
          ...newMembers,
        ],
        DictDeletedItemsIds: { AppSecurityGroupMemberList: deletedMemberIds },
      };

      const response = await adminSvc.saveAppSecurityGroupExDto(saveData);

      const errs = response?.ValidationResult
        ? (Array.isArray(response.ValidationResult)
          ? response.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean)
          : [])
        : [];
      setErrors(errs);

      if (response?.IsSuccessful) {
        onSaved();
        onClose();
      }
    } catch (e) {
      setErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [groupData, selectedUserIds, dispatch, onSaved, onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl w-[800px] max-w-[95vw] flex flex-col max-h-[90vh]`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-md font-semibold ${theme.title}`}>
            {groupId ? 'Edit Security Group' : 'Create Security Group'}
          </span>
          <button type="button" className="text-lg leading-none w-6 h-6" onClick={onClose}>&times;</button>
        </div>

        {/* Body */}
        <div className="h-1 flex-auto overflow-auto px-5 py-4">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : groupData ? (
            <div className="space-y-3">
              {/* Group Name */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Group Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={groupData.GroupName}
                  onChange={(e) => handleFieldChange('GroupName', e.target.value)}
                />
              </div>

              {/* Description */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Description</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={groupData.Description}
                  onChange={(e) => handleFieldChange('Description', e.target.value)}
                />
              </div>

              {/* User Type */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>User Type</label>
                <ComboBox
                  itemsSource={userTypeCV}
                  displayMemberPath="Display"
                  selectedValuePath="Id"
                  selectedValue={groupData.RoleUserTypeId}
                  selectedIndexChanged={(s) => handleFieldChange('RoleUserTypeId', s.selectedValue)}
                  isRequired={false}
                  placeholder="All Users"
                  style={{ flex: 1, height: 28 }}
                />
              </div>

              {/* Default Desktop */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Default Desktop</label>
                {desktopCV && (
                  <ComboBox
                    itemsSource={desktopCV}
                    displayMemberPath="Name"
                    selectedValuePath="Id"
                    selectedValue={groupData.DefaultDesktopId}
                    selectedIndexChanged={(s) => handleFieldChange('DefaultDesktopId', s.selectedValue)}
                    isRequired={false}
                    placeholder="Select desktop..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>

              {/* User Picker - Dual Grid */}
              <div className="flex gap-2 mt-3">
                {/* Available Users */}
                <div className="flex-1 flex flex-col">
                  <div className={`text-xs font-semibold ${theme.label} mb-1`}>Available Users</div>
                  <div className={`border ${theme.inputBox} h-[200px]`}>
                    {availableCV && (
                      <FlexGrid
                        itemsSource={availableCV}
                        selectionMode="Row"
                        headersVisibility="Column"
                        isReadOnly={true}
                        style={{ height: '100%' }}
                      >
                        <FlexGridColumn binding="Display" header="User" width="*" />
                        <FlexGridColumn binding="UserTypeName" header="Type" width={80} />
                      </FlexGrid>
                    )}
                  </div>
                </div>

                {/* Transfer Buttons */}
                <div className="flex flex-col justify-center space-y-1">
                  <button
                    type="button"
                    className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                    onClick={() => {
                      const item = availableCV?.currentItem as UserItem | null;
                      if (item) addUser(item.Id);
                    }}
                    title="Add Selected"
                  >
                    <i className="fa-solid fa-chevron-right" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                    onClick={addAllUsers}
                    title="Add All"
                  >
                    <i className="fa-solid fa-angles-right" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                    onClick={() => {
                      const item = selectedCV?.currentItem as UserItem | null;
                      if (item) removeUser(item.Id);
                    }}
                    title="Remove Selected"
                  >
                    <i className="fa-solid fa-chevron-left" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                    onClick={removeAllUsers}
                    title="Remove All"
                  >
                    <i className="fa-solid fa-angles-left" aria-hidden="true" />
                  </button>
                </div>

                {/* Selected Users */}
                <div className="flex-1 flex flex-col">
                  <div className={`text-xs font-semibold ${theme.label} mb-1`}>Selected Users</div>
                  <div className={`border ${theme.inputBox} h-[200px]`}>
                    {selectedCV && (
                      <FlexGrid
                        itemsSource={selectedCV}
                        selectionMode="Row"
                        headersVisibility="Column"
                        isReadOnly={true}
                        style={{ height: '100%' }}
                      >
                        <FlexGridColumn binding="Display" header="User" width="*" />
                        <FlexGridColumn binding="UserTypeName" header="Type" width={80} />
                      </FlexGrid>
                    )}
                  </div>
                </div>
              </div>

              {/* Errors */}
              {errors.length > 0 && (
                <div className="text-red-600 text-xs mt-2">
                  {errors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <p className={`text-xs ${theme.label}`}>No group data available.</p>
          )}
        </div>

        {/* Footer */}
        <div className="px-5 pb-4 flex items-center space-x-2">
          <button
            type="button"
            className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 disabled:opacity-50"
            onClick={handleSave}
            disabled={loading}
            title="Save"
          >
            <i className="fa-solid fa-save" aria-hidden="true" />
          </button>
          <button
            type="button"
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
            onClick={onClose}
            title="Cancel"
          >
            <i className="fa-solid fa-times" aria-hidden="true" />
          </button>
        </div>
      </div>
    </div>
  );
};

export default SecurityGroupEditor;
