import React, { useCallback, useEffect, useState } from 'react';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

type Props = {
  userId: string | number | null;
  domainId?: number | null;
  companyId?: string | number | null;
  partnerId?: string | number | null;
  organizationId?: string | number | null;
  onClose: () => void;
  onSaved: () => void;
};

type UserData = {
  Id?: number | null;
  UserName: string;
  LoginName: string;
  Password: string;
  ConfirmPassword?: string;
  Email: string;
  DomainId: number | null;
  LanguageId: number | null;
  CultureInfoCode: string;
  TimeZoneInfoToken: string;
  IsActive: boolean;
  AppCreatedByCompanyId?: string | number | null;
  OrganizationId?: string | number | null;
  InvitedByPartnerId?: string | number | null;
  MappingExternalEmployeeAccountId?: string;
  MenuSetting?: number | null;
  IsModified?: boolean;
};

const UserLoginInfoEditor: React.FC<Props> = ({
  userId,
  domainId,
  companyId,
  partnerId,
  organizationId,
  onClose,
  onSaved,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [userData, setUserData] = useState<UserData | null>(null);
  const [domainCV, setDomainCV] = useState<CollectionView | null>(null);
  const [languageCV, setLanguageCV] = useState<CollectionView | null>(null);
  const [timezoneCV, setTimezoneCV] = useState<CollectionView | null>(null);
  const [cultureCV, setCultureCV] = useState<CollectionView | null>(null);
  const [loading, setLoading] = useState(true);
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;
    const loadData = async () => {
      setLoading(true);
      setErrors([]);
      try {
        const [massResult, timezoneResult, cultureResult] = await Promise.allSettled([
          adminSvc.getMassEntitiesLookupItem('AppSecurityRegDomain|AppLanguage'),
          adminSvc.retrieveTimeZones(),
          adminSvc.getCultroInfos(),
        ]);

        if (cancelled) return;

        const massData = massResult.status === 'fulfilled' ? massResult.value : null;
        const timezones = timezoneResult.status === 'fulfilled' ? timezoneResult.value : [];
        const cultures = cultureResult.status === 'fulfilled' ? cultureResult.value : [];

        setDomainCV(new CollectionView(massData?.AppSecurityRegDomain ?? []));
        setLanguageCV(new CollectionView(massData?.AppLanguage ?? []));
        setTimezoneCV(new CollectionView(Array.isArray(timezones) ? timezones : []));
        setCultureCV(new CollectionView(Array.isArray(cultures) ? cultures : []));

        if (userId) {
          const user = await adminSvc.RetrieveMasterDBUserLoginInfo(String(userId));
          if (cancelled) return;
          if (user) {
            setUserData({
              Id: user.Id,
              UserName: user.UserName ?? '',
              LoginName: user.LoginName ?? '',
              Password: user.Password || 'password',
              ConfirmPassword: user.Password || 'password',
              Email: user.Email ?? '',
              DomainId: user.DomainId || null,
              LanguageId: user.LanguageId || null,
              CultureInfoCode: user.CultureInfoCode ?? '',
              TimeZoneInfoToken: user.TimeZoneInfoToken ?? '',
              IsActive: user.IsActive ?? true,
              AppCreatedByCompanyId: user.AppCreatedByCompanyId,
              OrganizationId: user.OrganizationId ?? null,
              MenuSetting: user.MenuSetting,
              MappingExternalEmployeeAccountId: user.MappingExternalEmployeeAccountId ?? '',
            });
          }
        } else {
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
            MappingExternalEmployeeAccountId: '',
          });
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
  }, [userId, domainId, companyId, organizationId]);

  const handleFieldChange = useCallback((field: keyof UserData, value: any) => {
    setUserData((prev) => prev ? { ...prev, [field]: value, IsModified: true } : null);
  }, []);

  const validate = useCallback((): string[] => {
    const errs: string[] = [];
    if (!userData) return ['No user data'];
    if (!userData.UserName?.trim()) errs.push('User Name is required');
    if (!userData.LoginName?.trim()) errs.push('Login Name is required');
    if (!userData.Email?.trim()) errs.push('Email is required');
    if (!userData.Password) errs.push('Password is required');
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
      let response: any;
      if (userData.Id) {
        response = await adminSvc.updateMasterDBUserLoginInfo(userData);
      } else {
        const saveData = {
          ...userData,
          InvitedByPartnerId: partnerId,
        };
        response = await adminSvc.InviteSaasComapnyNewUser(saveData);
      }

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
  }, [userData, partnerId, validate, dispatch, onSaved, onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl w-[500px] max-w-[90vw] flex flex-col min-h-[520px] max-h-[90vh]`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-md font-semibold ${theme.title}`}>
            {userId ? 'Edit User Login Info' : 'Create New User'}
          </span>
          <button type="button" className="text-lg leading-none w-6 h-6" onClick={onClose}>&times;</button>
        </div>

        {/* Body */}
        <div className="h-1 flex-auto overflow-auto px-5 py-4">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : userData ? (
            <div className="space-y-2">
              {errors.length > 0 && (
                <div className="text-red-600 text-xs mb-2">
                  {errors.map((msg, i) => <div key={i}>{msg}</div>)}
                </div>
              )}
              {/* User Name */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>User Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={userData.UserName}
                  onChange={(e) => handleFieldChange('UserName', e.target.value)}
                />
              </div>

              {/* Login Name */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Login Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={userData.LoginName}
                  onChange={(e) => handleFieldChange('LoginName', e.target.value)}
                />
              </div>

              {/* Email */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Email</label>
                <input
                  type="email"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={userData.Email}
                  onChange={(e) => handleFieldChange('Email', e.target.value)}
                />
              </div>

              {/* Password */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Password</label>
                <input
                  type="password"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={userData.Password}
                  onChange={(e) => handleFieldChange('Password', e.target.value)}
                />
              </div>

              {/* Confirm Password */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Confirm Password</label>
                <input
                  type="password"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={userData.ConfirmPassword ?? ''}
                  onChange={(e) => handleFieldChange('ConfirmPassword', e.target.value)}
                />
              </div>

              {/* Domain */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>User Type</label>
                {domainCV && (
                  <ComboBox
                    itemsSource={domainCV}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={userData.DomainId}
                    selectedIndexChanged={(s) => handleFieldChange('DomainId', s.selectedValue)}
                    isRequired={false}
                    placeholder="Select user type..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>

              {/* Language */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Language</label>
                {languageCV && (
                  <ComboBox
                    itemsSource={languageCV}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={userData.LanguageId}
                    selectedIndexChanged={(s) => handleFieldChange('LanguageId', s.selectedValue)}
                    isRequired={false}
                    placeholder="Select language..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>

              {/* Timezone */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Timezone</label>
                {timezoneCV && (
                  <ComboBox
                    itemsSource={timezoneCV}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={userData.TimeZoneInfoToken}
                    selectedIndexChanged={(s) => handleFieldChange('TimeZoneInfoToken', s.selectedValue ?? '')}
                    isRequired={false}
                    placeholder="Select timezone..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>

              {/* Culture */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Culture</label>
                {cultureCV && (
                  <ComboBox
                    itemsSource={cultureCV}
                    displayMemberPath="Display"
                    selectedValuePath="Id"
                    selectedValue={userData.CultureInfoCode}
                    selectedIndexChanged={(s) => handleFieldChange('CultureInfoCode', s.selectedValue ?? '')}
                    isRequired={false}
                    placeholder="Select culture..."
                    style={{ flex: 1, height: 28 }}
                  />
                )}
              </div>

              {/* Active */}
              <div className="flex items-center">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Active</label>
                <input
                  type="checkbox"
                  checked={userData.IsActive}
                  onChange={(e) => handleFieldChange('IsActive', e.target.checked)}
                  className="w-4 h-4"
                />
              </div>

            </div>
          ) : (
            <div>
              {errors.length > 0 ? (
                <div className="text-red-600 text-xs">
                  {errors.map((msg, i) => <div key={i}>{msg}</div>)}
                </div>
              ) : (
                <p className={`text-xs ${theme.label}`}>No user data available.</p>
              )}
            </div>
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

export default UserLoginInfoEditor;
