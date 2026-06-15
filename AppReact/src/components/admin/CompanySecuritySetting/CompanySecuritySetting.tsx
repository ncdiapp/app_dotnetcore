import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../../redux/store';
import { isMasterSysAdminFromContext } from '../../../helper/adminPermissionHelper';
import { TAB, type CompanyDto } from './types';
import CompanyEditor from './CompanyEditor';
import TenantAdminUsersTab from './TenantAdminUsersTab';
import CompanyUsersTab from './CompanyUsersTab';
import CompanyRoleSetup from './CompanyRoleSetup';
import CompanyMenuRoleSetup from './CompanyMenuRoleSetup';
import CompanyContactGroupSetup from './CompanyContactGroupSetup';
import DomainAndUserMenuManagement from './DomainAndUserMenuManagement';
import CompanyPrivilegeManagement from './CompanyPrivilegeManagement';
import CompanyIntegrationTokenManagement from './CompanyIntegrationTokenManagement';
import CompanyDashboardTab from './CompanyDashboardTab';

// All sections available to a tenant admin.
const ALL_SECTIONS = [
  { id: TAB.CompanyInfo,      name: 'Company Info',          icon: 'fa-building' },
  { id: TAB.User,             name: 'Users',                 icon: 'fa-users' },
  { id: TAB.Role,             name: 'Security Groups',       icon: 'fa-user-shield' },
  { id: TAB.MenuRole,         name: 'Menu Groups',           icon: 'fa-list-check' },
  { id: TAB.ContactGroup,     name: 'Contact Groups',        icon: 'fa-address-book' },
  { id: TAB.ApplicationMenu,  name: 'Menu By User Group',    icon: 'fa-sitemap' },
  { id: TAB.Dashboard,        name: 'Domain Dashboard',      icon: 'fa-chart-pie' },
  { id: TAB.Privilege,        name: 'Application Privileges',icon: 'fa-key' },
  { id: TAB.IntegrationToken, name: 'Integration Tokens',    icon: 'fa-plug' },
];

// SysAdmin manages platform infrastructure, not tenant-level roles/menus/dashboards.
const SYSADMIN_SECTIONS = [
  { id: TAB.CompanyInfo,      name: 'Company Info', icon: 'fa-building' },
  { id: TAB.TenantAdminUsers, name: 'Users',        icon: 'fa-users' },
];

const CompanySecuritySetting: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const isSysAdmin = isMasterSysAdminFromContext(userContext);
  const sections = isSysAdmin ? SYSADMIN_SECTIONS : ALL_SECTIONS;
  const [companyList, setCompanyList] = useState<any[]>([]);
  const [companyCV, setCompanyCV] = useState<CollectionView | null>(null);
  const [currentCompany, setCurrentCompany] = useState<CompanyDto | null>(null);
  const [selectedTab, setSelectedTab] = useState<number>(TAB.CompanyInfo);
  const [userSubTab, setUserSubTab] = useState(0);
  const [dashboardSubTab, setDashboardSubTab] = useState(2);
  const [showNewCompanyPopup, setShowNewCompanyPopup] = useState(false);
  const [newCompany, setNewCompany] = useState<{ Code: string; ShortName: string; FullName: string }>({ Code: '', ShortName: '', FullName: '' });
  const [newCompanyErrors, setNewCompanyErrors] = useState<string[]>([]);
  const innerSaveRef = useRef<(() => void) | null>(null);
  const innerRefreshRef = useRef<(() => void) | null>(null);

  const loadCompanies = useCallback(async () => {
    dispatch(setIsBusy());
    const timeoutMs = 20000;
    const timeoutPromise = new Promise<never>((_, reject) =>
      setTimeout(() => reject(new Error('Request timeout')), timeoutMs)
    );
    try {
      if (!isSysAdmin) {
        const company = await Promise.race([
          adminSvc.retrieveCurrentUserCompanyExDto(),
          timeoutPromise,
        ]);
        if (company?.Id != null) {
          setCompanyList([company]);
          setCompanyCV(null);
          setCurrentCompany(company);
        } else {
          setCompanyList([]);
          setCompanyCV(null);
          setCurrentCompany(null);
        }
        return;
      }

      const list = await Promise.race([
        adminSvc.retrieveAllRootCompanyDtoList(),
        timeoutPromise,
      ]);
      const arr = Array.isArray(list) ? list : [];
      setCompanyList(arr);
      const cv = new CollectionView(arr);
      setCompanyCV(cv);
      if (arr.length > 0) {
        setCurrentCompany((prev) => prev ?? arr[0]);
        cv.moveCurrentToFirst();
      }
    } catch (e) {
      setCompanyList([]);
      setCompanyCV(null);
      setCurrentCompany(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, isSysAdmin]);

  useEffect(() => {
    loadCompanies();
    return () => {
      dispatch(setIsNotBusy());
    };
  }, [loadCompanies, dispatch]);

  const handleCompanySelect = useCallback((cv: CollectionView) => {
    const sel = cv.currentItem;
    if (sel) setCurrentCompany(sel);
  }, []);

  useEffect(() => {
    if (!companyCV) return;
    const handler = () => handleCompanySelect(companyCV);
    companyCV.currentChanged.addHandler(handler);
    return () => companyCV.currentChanged.removeHandler(handler);
  }, [companyCV, handleCompanySelect]);

  const synchronizeCompany = useCallback((company: CompanyDto) => {
    setCurrentCompany((prev) => (prev && prev.Id === company.Id ? { ...prev, ...company, IsModified: false } : prev));
    setCompanyList((prev) => {
      const next = prev.map((c) => (c.Id === company.Id ? { ...c, ...company, IsModified: false } : c));
      if (!isSysAdmin) {
        return next.length > 0 ? next : [{ ...company, IsModified: false }];
      }
      if (companyCV) {
        const prevId = (companyCV.currentItem as any)?.Id;
        companyCV.sourceCollection = [...next];
        companyCV.refresh();
        if (prevId != null) {
          const idx = (companyCV.sourceCollection as any[]).findIndex((c) => c.Id === prevId);
          if (idx >= 0) companyCV.moveCurrentToPosition(idx);
        }
      }
      return next;
    });
  }, [companyCV, isSysAdmin]);

  const handleRefresh = useCallback(() => {
    if (innerRefreshRef.current) {
      innerRefreshRef.current();
    } else {
      loadCompanies();
    }
  }, [loadCompanies]);

  const handleSave = useCallback(() => {
    if (innerSaveRef.current) innerSaveRef.current();
  }, []);

  const openNewCompanyPopup = useCallback(() => {
    setNewCompany({ Code: '', ShortName: '', FullName: '' });
    setNewCompanyErrors([]);
    setShowNewCompanyPopup(true);
  }, []);

  const handleDeleteCompany = useCallback(async () => {
    if (!currentCompany?.Id) return;
    if (!window.confirm(`Delete company "${currentCompany.ShortName || currentCompany.Code}"? This cannot be undone.`)) return;
    dispatch(setIsBusy());
    try {
      const res = await adminSvc.deleteOneAppCompany(currentCompany.Id);
      const items = res?.ValidationResult?.Items;
      const errs = Array.isArray(items) ? items.map((e: any) => e?.Message || '').filter(Boolean) : [];
      if (errs.length > 0) {
        alert(errs.join('\n'));
      } else if (res?.IsSuccessful !== false) {
        setCurrentCompany(null);
        await loadCompanies();
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to delete company');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentCompany, dispatch, loadCompanies]);

  const closeNewCompanyPopup = useCallback(() => {
    setShowNewCompanyPopup(false);
    setNewCompany({ Code: '', ShortName: '', FullName: '' });
    setNewCompanyErrors([]);
  }, []);

  const saveNewCompany = useCallback(async () => {
    if (!newCompany.Code?.trim()) {
      setNewCompanyErrors(['Company Code is required.']);
      return;
    }
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveOneAppCompanyExDto({ ...newCompany, Id: undefined });
      const errs = data?.ValidationResult ? (Array.isArray(data.ValidationResult) ? data.ValidationResult.map((e: any) => e?.ErrorMessage || e?.Message || '').filter(Boolean) : []) : [];
      setNewCompanyErrors(errs);
      if (data?.IsSuccessful && data?.Object) {
        closeNewCompanyPopup();
        await loadCompanies();
        setCurrentCompany(data.Object);
      }
    } catch (e) {
      setNewCompanyErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [newCompany, dispatch, loadCompanies, closeNewCompanyPopup]);

  const companyId = currentCompany?.Id ?? null;
  const isModified = currentCompany?.IsModified ?? false;

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className="flex flex-1 min-h-0">
        {/* Left: Company list (SysAdmin only) */}
        {isSysAdmin && companyCV && (
          <div
            className="flex flex-col border-r shrink-0"
            style={{ width: 300 }}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>Companies</span>
              <div className="flex gap-1">
                <button
                  type="button"
                  className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600"
                  onClick={openNewCompanyPopup}
                  title="New Company"
                >
                  <i className="fa fa-plus" aria-hidden="true" />
                </button>
                <button
                  type="button"
                  className="w-8 h-6 bg-red-500 text-white rounded-[4px] text-xs hover:bg-red-600 disabled:opacity-40 disabled:cursor-not-allowed"
                  onClick={handleDeleteCompany}
                  disabled={!currentCompany?.Id}
                  title="Delete Selected Company"
                >
                  <i className="fa-solid fa-trash" aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="flex-1 min-h-0">
              <FlexGrid
                itemsSource={companyCV}
                selectionMode="Row"
                headersVisibility="Column"
                isReadOnly={true}
                selectionChanged={(s) => {
                    const row = s?.selection?.row ?? -1;
                    const item = row >= 0 ? s.rows[row]?.dataItem : null;
                    if (item) setCurrentCompany(item);
                  }}
                style={{ height: '100%' }}
              >
                <FlexGridColumn binding="Code" header="Code" width={120} />
                <FlexGridColumn binding="ShortName" header="Short Name" width="*" />
              </FlexGrid>
            </div>
          </div>
        )}

        {/* Center: Horizontal tabs + Content */}
        <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
          {/* Horizontal tab bar */}
          <div className={`flex items-center border-b shrink-0 ${theme.mainContentSection}`}>
            {!isSysAdmin && currentCompany && (
              <span className={`px-4 py-2 text-sm font-semibold shrink-0 ${theme.title}`}>
                Company: {currentCompany.ShortName || currentCompany.Code || currentCompany.FullName}
              </span>
            )}
            {sections.map((section) => (
              <button
                key={section.id}
                type="button"
                onClick={() => setSelectedTab(section.id)}
                className={`
                  px-4 py-2 text-sm flex items-center gap-1.5 border-b-2 transition-colors
                  ${selectedTab === section.id
                    ? `border-blue-500 ${theme.tab_active} font-medium`
                    : `border-transparent ${theme.tab}`}
                `}
              >
                <i className={`fa-solid ${section.icon} text-sm`} />
                <span>{section.name}</span>
              </button>
            ))}
          </div>

          {/* Content area */}
          <div className={`h-1 flex-auto min-h-0 overflow-hidden ${theme.mainContentSection}`}>
            {!currentCompany ? (
              <div className="w-full h-full overflow-auto px-5 py-5">
                <p className={`text-xs ${theme.label}`}>
                  {isSysAdmin
                    ? companyList.length === 0
                      ? 'No companies found. Load companies or create one.'
                      : 'Select a company from the list.'
                    : 'Unable to load your company. Check that your account is linked to a company.'}
                </p>
              </div>
            ) : (
              <div className="w-full h-full overflow-hidden">
                {selectedTab === TAB.CompanyInfo && (
                  <CompanyEditor
                    companyId={companyId}
                    isEmbedded
                    onSynchronize={synchronizeCompany}
                    onMarkModified={(m) =>
                      setCurrentCompany((p) => (p ? { ...p, IsModified: m } : null))
                    }
                  />
                )}

                {selectedTab === TAB.User && (
                  <CompanyUsersTab
                    companyId={companyId}
                    isModified={isModified}
                    currentUserSubTab={userSubTab}
                    onUserSubTabChange={setUserSubTab}
                  />
                )}

                {selectedTab === TAB.Role && (
                  <CompanyRoleSetup companyId={companyId} isEmbedded />
                )}

                {selectedTab === TAB.MenuRole && (
                  <CompanyMenuRoleSetup companyId={companyId} isEmbedded />
                )}

                {selectedTab === TAB.ContactGroup && (
                  <CompanyContactGroupSetup companyId={companyId} isEmbedded />
                )}

                {selectedTab === TAB.ApplicationMenu && (
                  <DomainAndUserMenuManagement isEmbedded />
                )}

                {selectedTab === TAB.Dashboard && (
                  <CompanyDashboardTab
                    companyId={companyId}
                    currentDashboardSubTab={dashboardSubTab}
                    onDashboardSubTabChange={setDashboardSubTab}
                    isModified={isModified}
                  />
                )}

                {selectedTab === TAB.Privilege && (
                  <CompanyPrivilegeManagement companyId={companyId} isEmbedded />
                )}

                {selectedTab === TAB.IntegrationToken && (
                  <CompanyIntegrationTokenManagement companyId={companyId} isEmbedded />
                )}

                {selectedTab === TAB.TenantAdminUsers && companyId && (
                  <TenantAdminUsersTab companyId={companyId} />
                )}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* New Company popup */}
      {showNewCompanyPopup && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={(e) => e.stopPropagation()}>
          <div
            className={`${theme.mainContentSection} rounded shadow-xl w-[360px] max-w-[90vw] flex flex-col`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 mb-1 border-b ${theme.mainContentSection}`}>
              <span className={`text-md font-semibold ${theme.title}`}>New Company</span>
              <button type="button" className="text-lg leading-none w-6 h-6" onClick={closeNewCompanyPopup}>&times;</button>
            </div>
            <div className="h-full w-full overflow-auto px-5 py-5">
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Company Code</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newCompany.Code}
                  onChange={(e) => setNewCompany((p) => ({ ...p, Code: e.target.value }))}
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Short Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newCompany.ShortName}
                  onChange={(e) => setNewCompany((p) => ({ ...p, ShortName: e.target.value }))}
                />
              </div>
              <div className="flex items-center py-1">
                <label className={`w-32 text-xs ${theme.label} mr-2`}>Full Name</label>
                <input
                  type="text"
                  className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={newCompany.FullName}
                  onChange={(e) => setNewCompany((p) => ({ ...p, FullName: e.target.value }))}
                />
              </div>
              {newCompanyErrors.length > 0 && (
                <div className="text-red-600 text-xs mt-2">
                  {newCompanyErrors.map((msg, i) => (
                    <div key={i}>{msg}</div>
                  ))}
                </div>
              )}
            </div>
            <div className="px-5 pb-5 flex justify-end">
              <button type="button" className="w-8 h-6 bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500" onClick={saveNewCompany}>
                Ok
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CompanySecuritySetting;
