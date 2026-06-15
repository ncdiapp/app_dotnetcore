import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import appHelper from '../../helper/appHelper';

type Props = {
  securityObjId: string | number | null;
  sharingObjType?: string;
  sharingObjName?: string;
  isNotification?: boolean;
  transactionId?: string | number | null;
  onClose: () => void;
  onSaved?: () => void;
};

type AvailableItem = {
  Id: number;
  Display: string;
  type: 'Group' | 'User';
  GroupId?: number;
  UserId?: number;
  OrganizationId?: number;
  IsCanWrite: boolean;
  IsNeedNotifyUser: boolean;
  isSelected: boolean;
};

type EmailUserItem = {
  Id: number;
  Display: string;
  UserName: string;
  Email: string;
  DomainId?: number;
  contactGroupIds: number[];
  isSelected: boolean;
};

type ContactGroupItem = {
  Id: number;
  Display: string;
};

const FileNavigationSharingEditor: React.FC<Props> = ({
  securityObjId,
  sharingObjType = 'File',
  sharingObjName = '',
  isNotification = false,
  transactionId,
  onClose,
  onSaved,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showInfo, showError } = useErrorMessage();

  // Data state
  const [availableItems, setAvailableItems] = useState<AvailableItem[]>([]);
  const [filterByOrganizationId, setFilterByOrganizationId] = useState<number>(0);
  const [organizationDataMap, setOrganizationDataMap] = useState<any>(null);
  const [organizationFilterList, setOrganizationFilterList] = useState<any[]>([]);
  const [organizationFilterCV, setOrganizationFilterCV] = useState<CollectionView | null>(null);

  // Notification mode state
  const [allUsers, setAllUsers] = useState<any[]>([]);
  const [availableEmailItems, setAvailableEmailItems] = useState<EmailUserItem[]>([]);
  const [availableEmailCV, setAvailableEmailCV] = useState<CollectionView<EmailUserItem> | null>(null);
  const [filterByContactGroupId, setFilterByContactGroupId] = useState<number>(-1);
  const [contactGroupList, setContactGroupList] = useState<ContactGroupItem[]>([{ Id: -1, Display: 'All' }]);
  const [availableEmailFilterText, setAvailableEmailFilterText] = useState('');
  const [isSelectAllUserEmails, setIsSelectAllUserEmails] = useState(false);

  // Sharing mode state
  const [isSelectedColumnSelectAll, setIsSelectedColumnSelectAll] = useState(false);
  const [isCanWriteColumnSelectAll, setIsCanWriteColumnSelectAll] = useState(false);

  // Common state
  const [messageText, setMessageText] = useState('');
  const [isNeedToSendMessageAfterFileSharing, setIsNeedToSendMessageAfterFileSharing] = useState(true);
  const [isAttachFile, setIsAttachFile] = useState(true);
  const [loading, setLoading] = useState(true);
  const mountedRef = useRef(true);

  // Initialize email users CollectionView with filter
  useEffect(() => {
    if (availableEmailItems.length > 0 && isNotification) {
      const cv = new CollectionView<EmailUserItem>(availableEmailItems);
      cv.filter = (item: EmailUserItem) => {
        let isMatchContactGroup = false;
        let isMatchFilterText = true;

        // Filter by text
        if (availableEmailFilterText) {
          const lowerFilter = availableEmailFilterText.toLowerCase();
          if (
            item.Display.toLowerCase().includes(lowerFilter) ||
            item.Email.toLowerCase().includes(lowerFilter)
          ) {
            isMatchFilterText = true;
          } else {
            isMatchFilterText = false;
          }
        }

        // Filter by contact group
        if (!filterByContactGroupId || filterByContactGroupId === -1) {
          isMatchContactGroup = true;
        } else {
          if (item.contactGroupIds && item.contactGroupIds.includes(filterByContactGroupId)) {
            isMatchContactGroup = true;
          }
        }

        return isMatchContactGroup && isMatchFilterText;
      };
      setAvailableEmailCV(cv);
    }
  }, [availableEmailItems, isNotification, filterByContactGroupId, availableEmailFilterText]);

  // Refresh email filter when text or group changes
  useEffect(() => {
    availableEmailCV?.refresh();
  }, [availableEmailFilterText, filterByContactGroupId, availableEmailCV]);

  // Prepare data for sharing mode
  const prepareData = useCallback(
    (
      massEntityData: any,
      availableSecurityGroups: any[],
      availableSecurityUsers: any[],
      shareOtherData: any[]
    ) => {
      try {
        const allOrganizations = massEntityData?.['AppComOrganization'] ?? [];
        if ((window as any).wijmo?.grid?.DataMap) {
          setOrganizationDataMap(new (window as any).wijmo.grid.DataMap(allOrganizations, 'Id', 'Display'));
        }
        const orgFilterList = [{ Id: 0, Display: 'All' }, ...allOrganizations];
        setOrganizationFilterList(orgFilterList);
        setOrganizationFilterCV(new CollectionView(orgFilterList));
      } catch (e) {
        appHelper.debugLog('FileNavigationSharingEditor prepareData org setup:', e);
      }

      // Build available items list
      const allGroupsAndUsers: AvailableItem[] = [];

      (availableSecurityGroups || []).forEach((aGroup: any) => {
        const id = aGroup.Id ?? aGroup.id;
        if (id == null) return;
        allGroupsAndUsers.push({
          Id: Number(id),
          Display: aGroup.GroupName ?? aGroup.groupName ?? aGroup.Display ?? aGroup.display ?? String(id),
          type: 'Group',
          GroupId: Number(id),
          OrganizationId: aGroup.OrganizationId ?? aGroup.organizationId,
          IsCanWrite: false,
          IsNeedNotifyUser: false,
          isSelected: false,
        });
      });

      (availableSecurityUsers || []).forEach((aUser: any) => {
        const id = aUser.Id ?? aUser.id;
        if (id == null) return;
        allGroupsAndUsers.push({
          Id: Number(id),
          Display: aUser.UserName ?? aUser.userName ?? aUser.Display ?? aUser.display ?? String(id),
          type: 'User',
          UserId: Number(id),
          OrganizationId: aUser.OrganizationId ?? aUser.organizationId,
          IsCanWrite: false,
          IsNeedNotifyUser: false,
          isSelected: false,
        });
      });

      // Mark existing shares
      if (shareOtherData && shareOtherData.length > 0) {
        shareOtherData.forEach((aShareDto: any) => {
          let matchItem: AvailableItem | undefined;
          if (aShareDto.ShareToOtherUserId) {
            matchItem = allGroupsAndUsers.find((item) => item.UserId === aShareDto.ShareToOtherUserId);
          } else if (aShareDto.ShareToOtherRoleId) {
            matchItem = allGroupsAndUsers.find((item) => item.GroupId === aShareDto.ShareToOtherRoleId);
          }
          if (matchItem) {
            matchItem.IsCanWrite = aShareDto.IsCanWrite || false;
            matchItem.IsNeedNotifyUser = true;
            matchItem.isSelected = true;
          }
        });
      }

      setAvailableItems(allGroupsAndUsers);
    },
    []
  );

  // Prepare data for notification mode
  const initializeAvailableUserEmails = useCallback((users: any[]) => {
    if (!users || users.length === 0) return;

    const dictContactGroupIdAndDto: Record<number, ContactGroupItem> = {};
    const filterByContactGroupLookupItemList: ContactGroupItem[] = [{ Id: -1, Display: 'All' }];
    const availableSourceItems: EmailUserItem[] = [];

    users.forEach((userDto: any) => {
      if (userDto.Email) {
        const contactGroupIds: number[] = [];
        if (userDto.UserContactGroups) {
          userDto.UserContactGroups.forEach((groupLookupItemDto: any) => {
            contactGroupIds.push(groupLookupItemDto.Id);
            if (!dictContactGroupIdAndDto[groupLookupItemDto.Id]) {
              dictContactGroupIdAndDto[groupLookupItemDto.Id] = groupLookupItemDto;
              filterByContactGroupLookupItemList.push(groupLookupItemDto);
            }
          });
        }

        const userObj: EmailUserItem = {
          Id: userDto.Id,
          Display: `${userDto.UserName} [${userDto.Email}]`,
          UserName: userDto.UserName,
          Email: userDto.Email,
          DomainId: userDto.DomainId,
          contactGroupIds: contactGroupIds,
          isSelected: false,
        };

        availableSourceItems.push(userObj);
      }
    });

    setContactGroupList(filterByContactGroupLookupItemList);
    setAvailableEmailItems(availableSourceItems);
  }, []);

  // Load data from server
  const loadDataFromServer = useCallback(async () => {
    if (!securityObjId) return;

    setLoading(true);
    dispatch(setIsBusy());

    try {
      const [massEntityData, rolesAndUsersResponse] = await Promise.all([
        adminSvc.getMassEntitiesLookupItem('AppComOrganization'),
        appFolderNavigationService.getCurrentUserAvailableShareFileToRolesAndUsers(),
      ]);

      // Unwrap in case API returns { Object: {...} } or { data: {...} }
      const availableRolesAndUsers =
        rolesAndUsersResponse?.Object ?? rolesAndUsersResponse?.data ?? rolesAndUsersResponse;

      // Normalize to arrays (support $values from .NET serialization)
      const rawRoleList = availableRolesAndUsers?.RoleDtoList ?? availableRolesAndUsers?.roleDtoList;
      const rawUserList = availableRolesAndUsers?.UserDtoList ?? availableRolesAndUsers?.userDtoList;
      const roleList = Array.isArray(rawRoleList) ? rawRoleList : rawRoleList?.$values ?? [];
      const userList = Array.isArray(rawUserList) ? rawUserList : rawUserList?.$values ?? [];

      if (!mountedRef.current) return;

      if (availableRolesAndUsers) {
        setAllUsers(userList);

        if (!isNotification) {
          let shareOtherData: any[] = [];
          try {
            const list = await appFolderNavigationService.getCurrentUserFilesToShareOtherDtoList(String(securityObjId));
            shareOtherData = Array.isArray(list) ? list : list?.ObjectList ?? list?.$values ?? [];
          } catch (shareErr) {
            appHelper.debugLog('FileNavigationSharingEditor getCurrentUserFilesToShareOtherDtoList error:', shareErr);
          }
          if (mountedRef.current) prepareData(massEntityData, roleList, userList, shareOtherData);
        } else {
          initializeAvailableUserEmails(userList);
        }
      }
    } catch (e) {
      appHelper.debugLog('FileNavigationSharingEditor loadDataFromServer error:', e);
    } finally {
      if (mountedRef.current) {
        setLoading(false);
        dispatch(setIsNotBusy());
      }
    }
  }, [securityObjId, isNotification, dispatch, prepareData, initializeAvailableUserEmails]);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  useEffect(() => {
    loadDataFromServer();
  }, [loadDataFromServer]);

  // Handle select all for isSelected column
  const handleIsSelectedColumnSelectAllChanged = useCallback(() => {
    const newValue = !isSelectedColumnSelectAll;
    setIsSelectedColumnSelectAll(newValue);

    setAvailableItems((prev) =>
      prev.map((item) => ({
        ...item,
        isSelected: newValue,
        IsNeedNotifyUser: newValue,
        IsCanWrite: newValue ? item.IsCanWrite : false,
      }))
    );
  }, [isSelectedColumnSelectAll]);

  // Handle select all for IsCanWrite column
  const handleIsCanWriteColumnSelectAllChanged = useCallback(() => {
    const newValue = !isCanWriteColumnSelectAll;
    setIsCanWriteColumnSelectAll(newValue);

    setAvailableItems((prev) =>
      prev.map((item) => ({
        ...item,
        IsCanWrite: item.isSelected ? newValue : false,
      }))
    );
  }, [isCanWriteColumnSelectAll]);

  // Handle select all emails
  const handleSelectAllUserEmailsChanged = useCallback(() => {
    const newValue = !isSelectAllUserEmails;
    setIsSelectAllUserEmails(newValue);

    if (availableEmailCV) {
      availableEmailCV.items.forEach((item) => {
        item.isSelected = newValue;
      });
      availableEmailCV.refresh();
    }
  }, [isSelectAllUserEmails, availableEmailCV]);

  // Filter by organization
  const handleFilterByOrganizationIdChanged = useCallback((orgId: number) => {
    setFilterByOrganizationId(orgId);
  }, []);

  // Save/Send handler
  const handleSave = useCallback(async () => {
    if (!securityObjId) return;

    dispatch(setIsBusy());

    try {
      const fileId = securityObjId;

      if (isNotification) {
        // Notification mode - send to selected email users
        const fileOrFolderShareToOtherDtoList: any[] = [];

        if (availableEmailCV) {
          availableEmailCV.items.forEach((item) => {
            if (item.isSelected) {
              fileOrFolderShareToOtherDtoList.push({
                FileId: fileId,
                ShareToOtherUserId: item.Id,
              });
            }
          });
        }

        if (fileOrFolderShareToOtherDtoList.length > 0) {
          const fileMessageTemplate = {
            FileshareOtherList: fileOrFolderShareToOtherDtoList,
            Subject: `File Notification: ${sharingObjName} (${fileId})`,
            Message: messageText || `File Notification: ${sharingObjName} (${fileId})`,
            TransactionId: transactionId,
            IsAttachFile: isAttachFile,
          };

          const result = await appFolderNavigationService.sendFileNotificationFromFileSharingMessageTemplate(
            fileMessageTemplate
          );

          if (result) {
            showInfo('Notification has been sent to the selected users.');
            setMessageText('');
            onSaved?.();
          }
        }
      } else {
        // Sharing mode
        const fileOrFolderShareToOtherDtoList: any[] = [];

        availableItems.forEach((item) => {
          if (item.isSelected) {
            fileOrFolderShareToOtherDtoList.push({
              FileId: fileId,
              ShareToOtherUserId: item.UserId || null,
              ShareToOtherRoleId: item.GroupId || null,
              IsCanWrite: item.IsCanWrite,
              IsNeedNotifyUser: item.IsNeedNotifyUser,
            });
          }
        });

        if (fileOrFolderShareToOtherDtoList.length > 0) {
          const fileMessageTemplate = {
            FileshareOtherList: fileOrFolderShareToOtherDtoList,
            Subject: `File Sharing Notification: ${sharingObjName} (${fileId})`,
            Message: messageText || `File Sharing Notification: ${sharingObjName} (${fileId})`,
            TransactionId: transactionId,
            IsNeedToSendMessageAfterFileSharing: isNeedToSendMessageAfterFileSharing,
            IsAttachFile: isAttachFile,
          };

          const result = await appFolderNavigationService.addFilesToShareOther(fileMessageTemplate);

          if (result) {
            if (isNeedToSendMessageAfterFileSharing) {
              showInfo('File sharing change success. Notification has been sent to the selected users.');
            } else {
              showInfo('File sharing change success.');
            }
            onSaved?.();
            loadDataFromServer();
          }
        } else {
          // Remove all shares
          const result = await appFolderNavigationService.removeFilesToShareOther([fileId]);
          if (result) {
            loadDataFromServer();
          }
        }
      }
    } catch (e) {
      appHelper.debugLog('FileNavigationSharingEditor save error:', e);
      showError('An error occurred while saving.');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    securityObjId,
    isNotification,
    availableItems,
    availableEmailCV,
    sharingObjName,
    messageText,
    transactionId,
    isNeedToSendMessageAfterFileSharing,
    isAttachFile,
    dispatch,
    loadDataFromServer,
    onSaved,
    showInfo,
    showError,
  ]);

  // Derived list for grid: apply organization filter and sort by type then name.
  const filteredAvailableItems = useMemo(() => {
    let list = [...availableItems];
    if (filterByOrganizationId > 0) {
      list = list.filter((item) => item.OrganizationId === filterByOrganizationId);
    }
    list.sort((a, b) => {
      if (a.type === b.type) {
        return (a.Display ?? '').localeCompare(b.Display ?? '');
      }
      return a.type.localeCompare(b.type);
    });
    return list;
  }, [availableItems, filterByOrganizationId]);

  // CollectionView for Roles and Users grid (Wijmo binds reliably to CV)
  const rolesAndUsersCV = useMemo(
    () => new CollectionView<AvailableItem>(filteredAvailableItems),
    [filteredAvailableItems]
  );

  // Toggle item selection
  const toggleItemSelection = useCallback((item: AvailableItem, field: keyof AvailableItem) => {
    setAvailableItems((prev) =>
      prev.map((i) => {
        if (i.Id === item.Id && i.type === item.type) {
          const newValue = !(i as any)[field];
          const updated = { ...i, [field]: newValue };
          if (field === 'isSelected' && !newValue) {
            updated.IsCanWrite = false;
            updated.IsNeedNotifyUser = false;
          }
          return updated;
        }
        return i;
      })
    );
  }, []);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl w-[700px] max-w-[95vw] flex flex-col min-h-[500px] max-h-[90vh]`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection} flex-shrink-0`}>
          <span className={`text-md font-semibold ${theme.title}`} title={sharingObjName}>
            {sharingObjType}: {sharingObjName}
          </span>
          <div className="flex items-center space-x-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handleSave}
              disabled={loading}
            >
              <i className={`fa-solid ${isNotification ? 'fa-paper-plane' : 'fa-floppy-disk'} mr-1`} />
              {isNotification ? 'Send' : 'Save'}
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={loadDataFromServer}
              disabled={loading}
            >
              <i className="fa-solid fa-rotate mr-1" />
              Refresh
            </button>
            <button
              type="button"
              className="text-lg leading-none w-6 h-6"
              onClick={onClose}
            >
              &times;
            </button>
          </div>
        </div>

        {/* Body */}
        <div className="min-h-0 flex-1 flex flex-col overflow-hidden px-3 py-3">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : (
            <>
              {/* Grid Area */}
              <div className="h-1 flex-auto flex flex-col">
                {!isNotification ? (
                  /* Sharing Mode - Roles and Users Grid */
                  <div className="h-full flex flex-col">
                    <div className={`text-xs font-semibold ${theme.label} mb-1 flex-shrink-0`}>Roles and Users</div>
                    <div className={`min-h-0 flex-1 h-full border ${theme.inputBox}`}>
                      <FlexGrid
                        key={`roles-${filteredAvailableItems.length}`}
                        itemsSource={rolesAndUsersCV}
                        selectionMode="Row"
                        headersVisibility="Column"
                        isReadOnly={false}
                        allowSorting={false}
                        className="h-full w-full"
                        style={{ height: '100%' }}
                      >
                          <FlexGridColumn binding="type" header="Type" width={80} isReadOnly={true} visible={false} />
                          <FlexGridColumn binding="Display" header="Name" width="*" isReadOnly={true} />
                          <FlexGridColumn binding="isSelected" header="Selected" width={90} dataType="Boolean">
                            <FlexGridCellTemplate
                              cellType="ColumnHeader"
                              template={() => (
                                <div className="flex items-center">
                                  <input
                                    type="checkbox"
                                    checked={isSelectedColumnSelectAll}
                                    onChange={handleIsSelectedColumnSelectAllChanged}
                                    className="mr-1"
                                  />
                                  <span>Selected</span>
                                </div>
                              )}
                            />
                            <FlexGridCellTemplate
                              cellType="Cell"
                              template={(cell: any) => {
                                const item = cell.item as AvailableItem;
                                return (
                                  <input
                                    type="checkbox"
                                    checked={item.isSelected}
                                    onChange={() => toggleItemSelection(item, 'isSelected')}
                                  />
                                );
                              }}
                            />
                          </FlexGridColumn>
                          <FlexGridColumn binding="IsCanWrite" header="Can Write" width={90} dataType="Boolean">
                            <FlexGridCellTemplate
                              cellType="ColumnHeader"
                              template={() => (
                                <div className="flex items-center">
                                  <input
                                    type="checkbox"
                                    checked={isCanWriteColumnSelectAll}
                                    onChange={handleIsCanWriteColumnSelectAllChanged}
                                    className="mr-1"
                                  />
                                  <span>Can Write</span>
                                </div>
                              )}
                            />
                            <FlexGridCellTemplate
                              cellType="Cell"
                              template={(cell: any) => {
                                const item = cell.item as AvailableItem;
                                return (
                                  <input
                                    type="checkbox"
                                    checked={item.IsCanWrite}
                                    onChange={() => toggleItemSelection(item, 'IsCanWrite')}
                                    disabled={!item.isSelected}
                                  />
                                );
                              }}
                            />
                          </FlexGridColumn>
                          <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                        </FlexGrid>
                    </div>
                  </div>
                ) : (
                  /* Notification Mode - Email Users List */
                  <div className="h-full flex flex-col">
                    {/* Contact Group Filter */}
                    <div className="flex items-center mb-2 flex-shrink-0">
                      <label className={`w-28 text-xs ${theme.label} mr-2`}>Contact Group</label>
                      <ComboBox
                        itemsSource={contactGroupList}
                        displayMemberPath="Display"
                        selectedValuePath="Id"
                        selectedValue={filterByContactGroupId}
                        selectedIndexChanged={(s) => setFilterByContactGroupId(s.selectedValue ?? -1)}
                        style={{ flex: 1, height: 28 }}
                      />
                    </div>

                    {/* Select All & Filter */}
                    <div className="flex items-center mb-2 gap-2 flex-shrink-0">
                      <label className="flex items-center text-xs">
                        <input
                          type="checkbox"
                          checked={isSelectAllUserEmails}
                          onChange={handleSelectAllUserEmailsChanged}
                          className="mr-1"
                        />
                        Select All
                      </label>
                      <div className="h-1 flex-auto relative">
                        <input
                          type="text"
                          className={`w-full h-7 px-7 text-xs border ${theme.inputBox} focus:outline-none`}
                          placeholder="Enter a username or email to filter."
                          value={availableEmailFilterText}
                          onChange={(e) => setAvailableEmailFilterText(e.target.value)}
                        />
                        <i className="fa-solid fa-filter absolute left-2 top-2 text-gray-400" />
                      </div>
                    </div>

                    {/* Email Users List */}
                    <div className={`min-h-0 flex-1 border ${theme.inputBox} overflow-auto`}>
                      {availableEmailCV &&
                        availableEmailCV.items.map((item, idx) => (
                          <div
                            key={`${item.Id}-${idx}`}
                            className="flex items-center px-2 py-1 hover:bg-gray-100 dark:hover:bg-gray-700"
                          >
                            <input
                              type="checkbox"
                              checked={item.isSelected}
                              onChange={() => {
                                item.isSelected = !item.isSelected;
                                availableEmailCV.refresh();
                              }}
                              className="mr-2"
                            />
                            <span
                              className={`text-xs ${theme.label} cursor-pointer`}
                              onClick={() => {
                                item.isSelected = !item.isSelected;
                                availableEmailCV.refresh();
                              }}
                            >
                              {item.Display}
                            </span>
                          </div>
                        ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Message Area */}
              <div className="mt-3 flex-shrink-0">
                <div className="flex items-center mb-1">
                  <label className={`text-xs font-semibold ${theme.label} mr-4`}>Notification Message</label>
                  {!isNotification && (
                    <label className="flex items-center text-xs mr-4">
                      <input
                        type="checkbox"
                        checked={isNeedToSendMessageAfterFileSharing}
                        onChange={(e) => setIsNeedToSendMessageAfterFileSharing(e.target.checked)}
                        className="mr-1"
                      />
                      Send Notification
                    </label>
                  )}
                  {(isNotification || isNeedToSendMessageAfterFileSharing) && (
                    <label className="flex items-center text-xs">
                      <input
                        type="checkbox"
                        checked={isAttachFile}
                        onChange={(e) => setIsAttachFile(e.target.checked)}
                        className="mr-1"
                      />
                      Attach This File
                    </label>
                  )}
                </div>
                <textarea
                  className={`w-full h-20 px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none resize-none`}
                  placeholder={`File ${isNotification ? 'Notification' : 'Sharing Notification'}: ${sharingObjName} (${securityObjId})`}
                  value={messageText}
                  onChange={(e) => setMessageText(e.target.value)}
                />
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default FileNavigationSharingEditor;
