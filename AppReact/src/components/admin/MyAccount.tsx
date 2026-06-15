import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../webapi/adminsvc';
import FormMasterDetail from '../formMgt/FormMasterDetail';

type UserProfileData = {
  Id?: number | null;
  OrganizationPath?: string;
  Password?: string;
  ConfirmPassword?: string;
  Email?: string;
  LanguageId?: number | null;
  CultureInfoCode?: string | null;
  TimeZoneInfoToken?: string | null;
  DefaultDesktopId?: number | null;
  BusinessUserId?: number | null;
  UserTransactionId?: number | null;
  DictDeletedItemsIds?: Record<string, any>;
  IsModified?: boolean;
};

type SaveResult = {
  IsSuccessful?: boolean;
  Object?: any;
  ValidationResult?: any;
};

const TAB_BASIC = 'BasicInfo';
const TAB_DETAIL = 'UserDetailForm';

const MyAccount: React.FC = () => {
  const dispatch = useDispatch();
  const { t, theme } = useTheme();
  const { showValidationMessages, clearAll } = useErrorMessage();

  const [selectedTab, setSelectedTab] = useState<string>(TAB_BASIC);
  const [userData, setUserData] = useState<UserProfileData | null>(null);
  const [languageCV, setLanguageCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [cultureInfoCV, setCultureInfoCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [timeZoneCV, setTimeZoneCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [refreshKey, setRefreshKey] = useState(0);
  const detailsFormActionApiRef = useRef<{ save: () => Promise<void>; refresh: () => void; reload: () => void } | null>(null);
  const [detailsApiReady, setDetailsApiReady] = useState(false);
  const isRefreshingRef = useRef(false);

  const canShowDetails = useMemo(() => {
    return !!(userData?.UserTransactionId && userData?.Id);
  }, [userData?.Id, userData?.UserTransactionId]);

  const canSave = useMemo(() => {
    if (selectedTab === TAB_DETAIL) return detailsApiReady;
    return Boolean(userData?.Id && userData?.IsModified);
  }, [detailsApiReady, selectedTab, userData?.Id, userData?.IsModified]);

  useEffect(() => {
    // When switching tabs or refreshing the whole profile, reset readiness; Details will set it back when form registers.
    if (selectedTab !== TAB_DETAIL) {
      setDetailsApiReady(false);
    }
  }, [selectedTab]);

  const markModified = useCallback(() => {
    setUserData((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const handleFieldChange = useCallback((field: keyof UserProfileData, value: any) => {
    setUserData((prev) => (prev ? { ...prev, [field]: value, IsModified: true } : prev));
  }, []);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    isRefreshingRef.current = true;
    try {
      clearAll();

      const [massEntityData, timeZoneData, cultureData, currentUser] = await Promise.all([
        adminSvc.getMassEntitiesLookupItem('AppSecurityRegDomain|AppLanguage'),
        adminSvc.retrieveTimeZones(),
        adminSvc.getCultroInfos(),
        adminSvc.retrieveCurrentAppSecurityUserExDto(),
      ]);

      setLanguageCV(new CollectionView<any>(massEntityData?.AppLanguage ?? []));
      setTimeZoneCV(new CollectionView<any>(Array.isArray(timeZoneData) ? timeZoneData : []));
      setCultureInfoCV(new CollectionView<any>(Array.isArray(cultureData) ? cultureData : []));

      if (currentUser) {
        const normalized: UserProfileData = {
          ...currentUser,
          DictDeletedItemsIds: currentUser.DictDeletedItemsIds ?? {},
          ConfirmPassword: currentUser.Password,
          LanguageId: currentUser.LanguageId ?? null,
          CultureInfoCode: currentUser.CultureInfoCode ?? null,
          TimeZoneInfoToken: currentUser.TimeZoneInfoToken ?? null,
          BusinessUserId: currentUser.BusinessUserId ?? null,
          DefaultDesktopId: currentUser.DefaultDesktopId ?? null,
          IsModified: false,
        };
        setUserData(normalized);
      } else {
        setUserData(null);
      }
    } finally {
      dispatch(setIsNotBusy());
      // Let Wijmo finish rebinding itemsSource before we accept user-driven selection changes again.
      setTimeout(() => {
        isRefreshingRef.current = false;
      }, 0);
    }
  }, [clearAll, dispatch]);

  useEffect(() => {
    let cancelled = false;
    const run = async () => {
      try {
        await loadData();
      } catch {
        // errors handled by global error popups in services / validation
      }
    };
    if (!cancelled) run();
    return () => {
      cancelled = true;
    };
  }, [loadData, refreshKey]);

  const handleRefresh = useCallback(() => {
    if (selectedTab === TAB_DETAIL && detailsFormActionApiRef.current) {
      detailsFormActionApiRef.current.refresh();
      return;
    }
    setRefreshKey((x) => x + 1);
  }, [selectedTab]);

  const handleSave = useCallback(async () => {
    if (selectedTab === TAB_DETAIL && detailsFormActionApiRef.current) {
      await detailsFormActionApiRef.current.save();
      return;
    }

    if (!userData?.Id || !userData.IsModified) return;
    dispatch(setIsBusy());
    try {
      clearAll();
      const result = (await adminSvc.saveAppSecurityUserExDto(userData)) as SaveResult;
      showValidationMessages(result?.ValidationResult ?? null);
      if (result?.IsSuccessful && result?.Object) {
        handleRefresh();
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [clearAll, dispatch, handleRefresh, selectedTab, showValidationMessages, userData]);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>My Profile</div>
        <div className="flex items-center gap-2">
          <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleRefresh}>
            <i className="fa-solid fa-rotate mr-2" aria-hidden="true" />
            Refresh
          </button>
          <button
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={handleSave}
            disabled={!canSave}
            title={canSave ? 'Save' : selectedTab === TAB_DETAIL ? 'Form is still loading…' : 'No changes to save'}
          >
            <i className="fa-solid fa-floppy-disk mr-2" aria-hidden="true" />
            Save
          </button>
        </div>
      </div>

      <div className={`flex items-center gap-2 px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <button
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} ${selectedTab === TAB_BASIC ? `font-semibold !${t('text_default_active')}` : ''}`}
          onClick={() => setSelectedTab(TAB_BASIC)}
        >
          Basic Info
        </button>
        {canShowDetails && (
          <button
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} ${selectedTab === TAB_DETAIL ? `font-semibold !${t('text_default_active')}` : ''}`}
            onClick={() => setSelectedTab(TAB_DETAIL)}
          >
            Details
          </button>
        )}
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {selectedTab === TAB_BASIC && (
          <div className="w-full h-full overflow-auto px-4 py-3">
            {userData ? (
              <div className="w-full max-w-3xl">
                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Organization</label>
                  <div className={`text-xs ${theme.label}`}>{userData.OrganizationPath || ''}</div>
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Password</label>
                  <input
                    type="password"
                    autoComplete="new-password"
                    value={userData.Password || ''}
                    onChange={(e) => handleFieldChange('Password', e.target.value)}
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  />
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Confirm Password</label>
                  <input
                    type="password"
                    autoComplete="new-password"
                    value={userData.ConfirmPassword || ''}
                    onChange={(e) => handleFieldChange('ConfirmPassword', e.target.value)}
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  />
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Email</label>
                  <input
                    type="email"
                    autoComplete="off"
                    value={userData.Email || ''}
                    onChange={(e) => handleFieldChange('Email', e.target.value)}
                    className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  />
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Language</label>
                  <div className="flex-auto w-1">
                    <select
                      className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                      value={userData.LanguageId ?? ''}
                      onChange={(e) => {
                        if (isRefreshingRef.current) return;
                        const v = e.target.value === '' ? null : Number(e.target.value);
                        handleFieldChange('LanguageId', Number.isFinite(v as any) ? v : null);
                        markModified();
                      }}
                    >
                      <option value="">(None)</option>
                      {(languageCV?.sourceCollection ?? []).map((it: any) => (
                        <option key={String(it?.Id)} value={String(it?.Id ?? '')}>
                          {it?.Display ?? ''}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Culture Info</label>
                  <div className="flex-auto w-1">
                    <select
                      className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                      value={userData.CultureInfoCode ?? ''}
                      onChange={(e) => {
                        if (isRefreshingRef.current) return;
                        const v = e.target.value === '' ? null : e.target.value;
                        handleFieldChange('CultureInfoCode', v);
                        markModified();
                      }}
                    >
                      <option value="">(None)</option>
                      {(cultureInfoCV?.sourceCollection ?? []).map((it: any) => (
                        <option key={String(it?.Id)} value={String(it?.Id ?? '')}>
                          {it?.Display ?? ''}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="flex items-center py-1">
                  <label className={`w-32 text-xs ${theme.label} mr-2`}>Time Zone</label>
                  <div className="flex-auto w-1">
                    <select
                      className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                      value={userData.TimeZoneInfoToken ?? ''}
                      onChange={(e) => {
                        if (isRefreshingRef.current) return;
                        const v = e.target.value === '' ? null : e.target.value;
                        handleFieldChange('TimeZoneInfoToken', v);
                        markModified();
                      }}
                    >
                      <option value="">(None)</option>
                      {(timeZoneCV?.sourceCollection ?? []).map((it: any) => (
                        <option key={String(it?.Id)} value={String(it?.Id ?? '')}>
                          {it?.Display ?? ''}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            ) : (
              <div className={`text-xs ${theme.label}`}>No user profile data.</div>
            )}
          </div>
        )}

        {selectedTab === TAB_DETAIL && canShowDetails && userData?.Id != null && userData?.UserTransactionId != null && (
          <div className="w-full h-full overflow-hidden flex flex-col">
            <FormMasterDetail
              key={`myprofile-detail-${userData.Id}-${userData.UserTransactionId}`}
              embedded={{
                embeddedTransactionId: userData.UserTransactionId as number,
                embeddedRootPrimaryKeyValue: userData.Id,
                embeddedParam2: { isEmbeddedByOtherPage: true, isHideHeaderAndFooter: true },
              }}
              onFormActionApiReady={(api) => {
                detailsFormActionApiRef.current = api;
                setDetailsApiReady(true);
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
};

export default MyAccount;