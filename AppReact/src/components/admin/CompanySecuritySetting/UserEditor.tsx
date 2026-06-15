import React, { useCallback, useEffect, useState, useMemo } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { dashboardService } from '../../../webapi/dashboardsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import FormMasterDetail from '../../formMgt/FormMasterDetail';

type Props = {
  userId: string | number | null;
  domainId?: number | null;
  companyId?: string | number | null;
  organizationId?: string | number | null;
  onClose: () => void;
  onSaved: () => void;
};

type UserData = {
  Id?: number | null;
  UserName: string;
  LoginName: string;
  Password?: string;
  ConfirmPassword?: string;
  Email?: string;
  DomainId: number | null;
  LanguageId?: number | null;
  CultureInfoCode?: string;
  TimeZoneInfoToken?: string;
  IsActive?: boolean;
  AppCreatedByCompanyId?: string | number | null;
  OrganizationId?: string | number | null;
  DefaultDesktopId?: number | null;
  /** When set, Tab "User Detail Info" shows and embeds the app-configured Transaction form (see Application Setting). */
  UserTransactionId?: number | null;
  PartnerDtoInCurrentCompany?: { DisplayName?: string } | null;
  AppSecurityGroupMemberList?: Array<{ Id?: number; GroupId: number }>;
  DictDeletedItemsIds?: { AppSecurityGroupMemberList: number[] };
  IsModified?: boolean;
};

type ContactDto = {
  Id?: number;
  UserId?: number;
  ContactType: number;
  ContactFormat: string;
  AdditionalContactInfo?: boolean;
  IsNew?: boolean;
};

type GroupItem = {
  Id: number;
  Display: string;
  isNotAllowChange?: boolean;
};

const CONTACT_TYPES = [
  { Id: 1, Display: 'Email' },
  { Id: 2, Display: 'Phone' },
  { Id: 3, Display: 'Mobile' },
  { Id: 4, Display: 'Fax' },
  { Id: 5, Display: 'WeChat' },
];

/** Tab 0: Angular "Roles & Contacts" – read-only User/Login Name, optional Partner, User Roles, User Contacts. */
const TAB_ROLES_CONTACTS = 'RolesContacts';
/** Tab 1: Angular "User Detail Info" – only visible when userData.UserTransactionId is set; content is the app-configured Transaction form (embedded), not hardcoded login fields. */
const TAB_USER_DETAIL = 'UserDetailForm';

const UserEditor: React.FC<Props> = ({
  userId,
  domainId,
  companyId,
  organizationId,
  onClose,
  onSaved,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [userData, setUserData] = useState<UserData | null>(null);
  const [contacts, setContacts] = useState<ContactDto[]>([]);
  const [contactsCV, setContactsCV] = useState<CollectionView | null>(null);
  const [allGroups, setAllGroups] = useState<GroupItem[]>([]);
  const [selectedGroupIds, setSelectedGroupIds] = useState<Set<number>>(new Set());
  const [_domainCV, setDomainCV] = useState<CollectionView | null>(null);
  const [_languageCV, setLanguageCV] = useState<CollectionView | null>(null);
  const [_timezoneCV, setTimezoneCV] = useState<CollectionView | null>(null);
  const [_cultureCV, setCultureCV] = useState<CollectionView | null>(null);
  const [_desktopCV, setDesktopCV] = useState<CollectionView | null>(null);
  const [contactTypeCV] = useState<CollectionView>(() => new CollectionView(CONTACT_TYPES));
  const [selectedTab, setSelectedTab] = useState(TAB_ROLES_CONTACTS);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [isFullscreen, setIsFullscreen] = useState(false);

  // Split groups into available and selected
  const availableGroups = useMemo(() => {
    return allGroups.filter((g) => !selectedGroupIds.has(g.Id) && !g.isNotAllowChange);
  }, [allGroups, selectedGroupIds]);

  const selectedGroups = useMemo(() => {
    return allGroups.filter((g) => selectedGroupIds.has(g.Id));
  }, [allGroups, selectedGroupIds]);

  const [availableCV, setAvailableCV] = useState<CollectionView | null>(null);
  const [selectedCV, setSelectedCV] = useState<CollectionView | null>(null);

  useEffect(() => {
    setAvailableCV(new CollectionView(availableGroups));
  }, [availableGroups]);

  useEffect(() => {
    setSelectedCV(new CollectionView(selectedGroups));
  }, [selectedGroups]);

  useEffect(() => {
    setContactsCV(new CollectionView(contacts));
  }, [contacts]);

  useEffect(() => {
    let cancelled = false;
    const loadData = async () => {
      setLoading(true);
      try {
        const [massData, timezones, cultures] = await Promise.all([
          adminSvc.getMassEntitiesLookupItem('AppSecurityRegDomain|AppLanguage'),
          adminSvc.retrieveTimeZones(),
          adminSvc.getCultroInfos(),
        ]);

        if (cancelled) return;

        setDomainCV(new CollectionView(massData?.AppSecurityRegDomain ?? []));
        setLanguageCV(new CollectionView(massData?.AppLanguage ?? []));
        setTimezoneCV(new CollectionView(Array.isArray(timezones) ? timezones : []));
        setCultureCV(new CollectionView(Array.isArray(cultures) ? cultures : []));

        if (userId) {
          // Load existing user first (Angular: then load roles by user.DomainId)
          const [user, desktops, contactData] = await Promise.all([
            adminSvc.retrieveOneAppSecurityUserExDto(String(userId)),
            dashboardService.retrieveDesktopDtoListByUserId(String(userId)),
            adminSvc.retrieveOneUserAllContactEntityDto(String(userId)),
          ]);

          if (cancelled) return;

          setDesktopCV(new CollectionView(Array.isArray(desktops) ? desktops : []));

          const userDomainId = user?.DomainId ?? null;
          const userTypeParam = userDomainId != null ? String(userDomainId) : '';

          // Angular: RetrieveAppSecurityGroupDtoByGroupUsage(SecurityGroup, true, userData.DomainId)
          const groupsData = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage('1', true, userTypeParam);
          const groupItems: GroupItem[] = (Array.isArray(groupsData) ? groupsData : []).map((g: any) => ({
            Id: g.Id,
            Display: ((g.OrganizationPath) ? g.OrganizationPath + '/ ' : '') + g.GroupName,
            isNotAllowChange: g.IsBuiltIn && !g.IsAllowedToAddAvailableUser,
          }));
          if (cancelled) return;
          setAllGroups(groupItems);

          if (user) {
            setUserData({
              ...user,
              ConfirmPassword: user.Password || '',
              DomainId: user.DomainId ?? null,
              LanguageId: user.LanguageId ?? null,
              CultureInfoCode: user.CultureInfoCode ?? '',
              TimeZoneInfoToken: user.TimeZoneInfoToken ?? '',
              DefaultDesktopId: user.DefaultDesktopId ?? null,
              DictDeletedItemsIds: { AppSecurityGroupMemberList: [] },
            });

            const memberGroupIds = (user.AppSecurityGroupMemberList || []).map((m: any) => m.GroupId);
            setSelectedGroupIds(new Set(memberGroupIds));
          }

          const contactList = Array.isArray(contactData) ? contactData : [];
          setContacts(contactList.map((c: any) => ({
            ...c,
            AdditionalContactInfo: c.AdditionalContactInfo ?? false,
          })));
        } else {
          // New user: Angular uses (SecurityGroup, true) - pass domainId as userType when provided
          const userTypeParam = domainId != null ? String(domainId) : '';
          const groupsData = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage('1', true, userTypeParam);
          const groupItems: GroupItem[] = (Array.isArray(groupsData) ? groupsData : []).map((g: any) => ({
            Id: g.Id,
            Display: ((g.OrganizationPath) ? g.OrganizationPath + '/ ' : '') + g.GroupName,
            isNotAllowChange: g.IsBuiltIn && !g.IsAllowedToAddAvailableUser,
          }));
          if (cancelled) return;
          setAllGroups(groupItems);

          const desktops = await dashboardService.getOrganizationDesktopListByOrganizationId(String(organizationId || ''));
          if (cancelled) return;
          setDesktopCV(new CollectionView(Array.isArray(desktops) ? desktops : []));

          setUserData({
            UserName: '',
            LoginName: '',
            Password: '',
            ConfirmPassword: '',
            Email: '',
            DomainId: domainId ?? null,
            LanguageId: null,
            CultureInfoCode: '',
            TimeZoneInfoToken: '',
            IsActive: true,
            AppCreatedByCompanyId: companyId,
            OrganizationId: organizationId ?? null,
            DefaultDesktopId: null,
            AppSecurityGroupMemberList: [],
            DictDeletedItemsIds: { AppSecurityGroupMemberList: [] },
          });
          setSelectedGroupIds(new Set());
          setContacts([]);
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
  }, [userId, domainId, companyId, organizationId, refreshTrigger]);

  const handleFieldChange = useCallback((field: keyof UserData, value: any) => {
    setUserData((prev) => prev ? { ...prev, [field]: value, IsModified: true } : null);
  }, []);

  const addGroup = useCallback((groupId: number) => {
    setSelectedGroupIds((prev) => new Set([...Array.from(prev), groupId]));
    setUserData((prev) => prev ? { ...prev, IsModified: true } : null);
  }, []);

  const removeGroup = useCallback((groupId: number) => {
    setSelectedGroupIds((prev) => {
      const next = new Set(prev);
      next.delete(groupId);
      return next;
    });
    setUserData((prev) => prev ? { ...prev, IsModified: true } : null);
  }, []);

  const addContact = useCallback(() => {
    const newContact: ContactDto = {
      ContactType: 1,
      ContactFormat: '',
      AdditionalContactInfo: false,
      IsNew: true,
    };
    setContacts((prev) => [...prev, newContact]);
    setUserData((prev) => prev ? { ...prev, IsModified: true } : null);
  }, []);

  const removeContact = useCallback((index: number) => {
    setContacts((prev) => prev.filter((_, i) => i !== index));
    setUserData((prev) => prev ? { ...prev, IsModified: true } : null);
  }, []);

  const updateContact = useCallback((index: number, field: keyof ContactDto, value: any) => {
    setContacts((prev) => prev.map((c, i) => i === index ? { ...c, [field]: value } : c));
    setUserData((prev) => prev ? { ...prev, IsModified: true } : null);
  }, []);

  const validate = useCallback((): string[] => {
    const errs: string[] = [];
    if (!userData) return ['No user data'];
    if (!userData.UserName?.trim()) errs.push('User Name is required');
    if (!userData.LoginName?.trim()) errs.push('Login Name is required');
    if (!userData.Id && !userData.Password) errs.push('Password is required for new user');
    if (userData.Password !== userData.ConfirmPassword) errs.push('Passwords do not match');
    return errs;
  }, [userData]);

  const handleSave = useCallback(async () => {
    const validationErrors = validate();
    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }
    if (!userData) return;

    dispatch(setIsBusy());
    try {
      // Prepare group member list changes
      const originalMemberGroupIds = new Set(
        (userData.AppSecurityGroupMemberList || []).map((m) => m.GroupId)
      );
      const deletedMemberIds: number[] = [];
      const newMembers: Array<{ GroupId: number }> = [];

      // Find deleted members
      (userData.AppSecurityGroupMemberList || []).forEach((m) => {
        if (!selectedGroupIds.has(m.GroupId) && m.Id) {
          deletedMemberIds.push(m.Id);
        }
      });

      // Find new members
      selectedGroupIds.forEach((groupId) => {
        if (!originalMemberGroupIds.has(groupId)) {
          newMembers.push({ GroupId: groupId });
        }
      });

      const saveUserData = {
        ...userData,
        AppSecurityGroupMemberList: [
          ...(userData.AppSecurityGroupMemberList || []).filter((m) => selectedGroupIds.has(m.GroupId)),
          ...newMembers,
        ],
        DictDeletedItemsIds: { AppSecurityGroupMemberList: deletedMemberIds },
      };

      const response = await adminSvc.saveAppSecurityUserExDto(saveUserData);

      let errs = response?.ValidationResult
        ? (Array.isArray(response.ValidationResult)
          ? response.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean)
          : [])
        : [];

      if (response?.IsSuccessful && response?.Object) {
        // Save contacts
        const savedUserId = response.Object.Id;
        const contactsWithUserId = contacts.map((c) => ({ ...c, UserId: savedUserId }));
        const contactSetDto = {
          UserId: savedUserId,
          UserContactList: contactsWithUserId,
        };
        const contactResponse = await adminSvc.saveAllAppSecurityUserContactEntityDto(contactSetDto);

        const contactErrs = contactResponse?.ValidationResult
          ? (Array.isArray(contactResponse.ValidationResult)
            ? contactResponse.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean)
            : [])
          : [];
        errs = [...errs, ...contactErrs];

        if (contactResponse?.IsSuccessful) {
          onSaved();
          onClose();
          return;
        }
      }

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
  }, [userData, selectedGroupIds, contacts, validate, dispatch, onSaved, onClose]);

  return (
    <div className={`fixed inset-0 z-50 bg-black/30 ${isFullscreen ? 'flex' : 'flex items-center justify-center'}`}>
      <div
        className={`${theme.mainContentSection} shadow-xl flex flex-col ${
          isFullscreen
            ? 'w-full h-full max-w-full max-h-full rounded-none'
            : 'rounded w-[800px] max-w-[95vw] min-h-[680px] max-h-[90vh]'
        }`}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-md font-semibold ${theme.title}`}>
            {userId
              ? `User${
                userData?.UserName != null && userData.UserName !== ''
                  ? `: ${userData.UserName}${
                    userData?.PartnerDtoInCurrentCompany?.DisplayName
                      ? ` - ${userData.PartnerDtoInCurrentCompany.DisplayName}`
                      : ''
                  }`
                  : ''
              }`
              : 'Create User'}
          </span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              className={`w-8 h-7 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
              onClick={() => setIsFullscreen((v) => !v)}
              title={isFullscreen ? 'Exit full screen' : 'Full screen'}
            >
              <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`} aria-hidden="true" />
            </button>
            <button type="button" className="text-lg leading-none w-6 h-6" onClick={onClose}>&times;</button>
          </div>
        </div>

        {/* Tab header: only show when second tab exists (UserTransactionId set) */}
        {userData?.UserTransactionId != null && (
          <div className={`flex border-b ${theme.mainContentSection}`}>
            <button
              type="button"
              className={`px-4 py-2 text-xs border-b-2 ${selectedTab === TAB_ROLES_CONTACTS ? theme.tab_active : theme.tab}`}
              onClick={() => setSelectedTab(TAB_ROLES_CONTACTS)}
            >
              Roles &amp; Contacts
            </button>
            <button
              type="button"
              className={`px-4 py-2 text-xs border-b-2 ${selectedTab === TAB_USER_DETAIL ? theme.tab_active : theme.tab}`}
              onClick={() => setSelectedTab(TAB_USER_DETAIL)}
            >
              User Detail Info
            </button>
          </div>
        )}

        {/* Body: flex to fill popup */}
        <div className="flex flex-col min-h-0 flex-auto overflow-hidden px-5 py-4">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : userData ? (
            <>
              {selectedTab === TAB_ROLES_CONTACTS && (
                <div className="flex flex-col min-h-0 flex-1 overflow-hidden">
                  {/* Roles & Contacts Info: Angular section with Refresh + Save */}
                  <div className={`flex items-center justify-between mb-2 shrink-0 ${theme.mainContentSection}`}>
                    <div className={`text-xs font-semibold ${theme.title}`}>Roles &amp; Contacts Info</div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        className="px-3 py-1.5 text-xs rounded-[4px] text-white bg-blue-400 hover:bg-blue-500"
                        onClick={() => setRefreshTrigger((t) => t + 1)}
                        title="Refresh"
                      >
                        <i className="fa-solid fa-refresh" aria-hidden="true" />
                      </button>
                      <button
                        type="button"
                        className="px-3 py-1.5 text-xs rounded-[4px] text-white bg-orange-400 hover:bg-orange-500 disabled:opacity-50 disabled:cursor-not-allowed"
                        onClick={handleSave}
                        disabled={loading}
                        title="Save"
                      >
                        <i className="fa-solid fa-save" aria-hidden="true" />
                      </button>
                    </div>
                  </div>

                  {/* Create user only: allow entering User Name / Login Name. (Edit mode shows name in the header.) */}
                  {!userData.Id && (
                    <div className="flex flex-wrap gap-4 items-start shrink-0">
                      <div className="flex items-center min-w-[200px]">
                        <label className={`w-24 text-xs ${theme.label} mr-2`}>User Name</label>
                        <input
                          type="text"
                          className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                          value={userData.UserName}
                          onChange={(e) => handleFieldChange('UserName', e.target.value)}
                        />
                      </div>
                      <div className="flex items-center min-w-[200px]">
                        <label className={`w-24 text-xs ${theme.label} mr-2`}>Login Name</label>
                        <input
                          type="text"
                          className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                          value={userData.LoginName}
                          onChange={(e) => handleFieldChange('LoginName', e.target.value)}
                        />
                      </div>
                    </div>
                  )}
                  {/* New user only: Password / Confirm (required for create). Detail fields (Email, etc.) are in Tab "User Detail Info" when Transaction is configured. */}
                  {!userData.Id && (
                    <div className="flex flex-wrap gap-4 items-center mt-2 shrink-0">
                      <div className="flex items-center min-w-[200px]">
                        <label className={`w-24 text-xs ${theme.label} mr-2`}>Password</label>
                        <input
                          type="password"
                          className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                          value={userData.Password ?? ''}
                          onChange={(e) => handleFieldChange('Password', e.target.value)}
                        />
                      </div>
                      <div className="flex items-center min-w-[200px]">
                        <label className={`w-24 text-xs ${theme.label} mr-2`}>Confirm</label>
                        <input
                          type="password"
                          className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                          value={userData.ConfirmPassword ?? ''}
                          onChange={(e) => handleFieldChange('ConfirmPassword', e.target.value)}
                        />
                      </div>
                    </div>
                  )}

                  {/* Wrapper: Roles and Contacts each get 50% height */}
                  <div className="flex flex-col flex-1 min-h-0 gap-2 mt-2">
                  {/* User Roles: 50% */}
                  <div className="flex flex-col h-1 flex-auto">
                    <div className={`text-xs font-semibold ${theme.title} mb-1 shrink-0`}>User Roles:</div>
                    <div className="flex gap-2 flex-1 min-h-0 overflow-hidden">
                      <div className="flex-1 flex flex-col min-h-0">
                        <div className={`text-xs ${theme.label} mb-1 shrink-0`}>Available Role</div>
                        <div className={`border ${theme.inputBox} flex-1 min-h-0`}>
                          {availableCV && (
                            <FlexGrid
                              itemsSource={availableCV}
                              selectionMode="Row"
                              headersVisibility="Column"
                              isReadOnly={true}
                              style={{ height: '100%' }}
                            >
                              <FlexGridColumn binding="Display" header="Group" width="*" />
                            </FlexGrid>
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col justify-center space-y-1 shrink-0">
                        <button
                          type="button"
                          className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                          onClick={() => {
                            const item = availableCV?.currentItem as GroupItem | null;
                            if (item) addGroup(item.Id);
                          }}
                          title="Add"
                        >
                          <i className="fa-solid fa-chevron-right" />
                        </button>
                        <button
                          type="button"
                          className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
                          onClick={() => {
                            const item = selectedCV?.currentItem as GroupItem | null;
                            if (item) removeGroup(item.Id);
                          }}
                          title="Remove"
                        >
                          <i className="fa-solid fa-chevron-left" />
                        </button>
                      </div>
                      <div className="flex-1 flex flex-col min-h-0">
                        <div className={`text-xs ${theme.label} mb-1 shrink-0`}>Selected Role</div>
                        <div className={`border ${theme.inputBox} flex-1 min-h-0`}>
                          {selectedCV && (
                            <FlexGrid
                              itemsSource={selectedCV}
                              selectionMode="Row"
                              headersVisibility="Column"
                              isReadOnly={true}
                              style={{ height: '100%' }}
                            >
                              <FlexGridColumn binding="Display" header="Group" width="*" />
                            </FlexGrid>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>

                  {/* User Contacts: 50% */}
                  <div className="flex flex-col h-1 flex-auto">
                    <div className={`flex items-center justify-between shrink-0 ${theme.mainContentSection}`}>
                      <div className={`text-xs font-semibold ${theme.title}`}>User Contacts:</div>
                      <div className="flex gap-2">
                        <button
                          type="button"
                          className="px-3 py-1.5 text-xs rounded-[4px] text-white bg-green-500 hover:bg-green-600"
                          onClick={addContact}
                          title="Add"
                        >
                          <i className="fa-solid fa-plus" aria-hidden="true" />
                        </button>
                        <button
                          type="button"
                          className="px-3 py-1.5 text-xs rounded-[4px] text-white bg-blue-400 hover:bg-blue-500"
                          onClick={() => {
                            if (contactsCV && contactsCV.currentItem) {
                              const idx = contacts.indexOf(contactsCV.currentItem as ContactDto);
                              if (idx >= 0) removeContact(idx);
                            }
                          }}
                          title="Remove"
                        >
                          <i className="fa-solid fa-minus" aria-hidden="true" />
                        </button>
                      </div>
                    </div>
                    <div className={`border ${theme.inputBox} flex-1 min-h-0 mt-1 overflow-hidden`}>
                    {contactsCV && (
                      <FlexGrid
                        itemsSource={contactsCV}
                        selectionMode="Row"
                        headersVisibility="Column"
                        isReadOnly={false}
                        allowAddNew={false}
                        allowDelete={false}
                        style={{ height: '100%', maxHeight: '100%' }}
                      >
                        <FlexGridColumn header="" width={40}>
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const index = cell.row.index;
                              return (
                                <button
                                  type="button"
                                  className="text-red-500 hover:text-red-700"
                                  onClick={() => removeContact(index)}
                                  title="Delete"
                                >
                                  <i className="fa-solid fa-trash" />
                                </button>
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn header="Contact Type" width={120}>
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item as ContactDto;
                              const index = cell.row.index;
                              return (
                                <ComboBox
                                  itemsSource={contactTypeCV}
                                  displayMemberPath="Display"
                                  selectedValuePath="Id"
                                  selectedValue={item.ContactType}
                                  selectedIndexChanged={(s) => updateContact(index, 'ContactType', s.selectedValue)}
                                  style={{ width: '100%', height: 24 }}
                                />
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn binding="ContactFormat" header="ContactFormat" width="*" />
                      </FlexGrid>
                    )}
                  </div>
                </div>
                </div>
                </div>
              )}

              {selectedTab === TAB_USER_DETAIL && userData.UserTransactionId != null && userData.Id != null && (
                <div className="w-full h-1 flex-auto min-h-0 overflow-hidden flex flex-col">
                  <FormMasterDetail
                    key={`userdetail-${userData.Id}-${userData.UserTransactionId}`}
                    embedded={{
                      embeddedTransactionId: userData.UserTransactionId as number,
                      embeddedRootPrimaryKeyValue: userData.Id,
                      embeddedParam2: { isEmbeddedByOtherPage: true, isHideHeaderAndFooter: true },
                    }}
                  />
                </div>
              )}

              {/* Errors */}
              {errors.length > 0 && (
                <div className="text-red-600 text-xs mt-2">
                  {errors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
            </>
          ) : (
            <p className={`text-xs ${theme.label}`}>No user data available.</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default UserEditor;
