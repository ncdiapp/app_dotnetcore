import React, { useEffect, useState } from 'react';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import appHelper from '../../helper/appHelper';

type ProvisionForm = {
  CompanyName: string;
  DomainToken: string;
  AdminEmail: string;
  AdminLoginName: string;
  AdminPassword: string;
  TemplateId: string;
};

type ProvisionResult = {
  Success: boolean;
  CompanyId?: number | null;
  DatabaseName?: string;
  LoginUrl?: string;
  MigrationsApplied?: number;
  SeededFromTemplate?: string;
  ErrorMessage?: string;
};

type MigrationResult = Record<string, number>;

const emptyForm = (): ProvisionForm => ({
  CompanyName: '',
  DomainToken: '',
  AdminEmail: '',
  AdminLoginName: '',
  AdminPassword: '',
  TemplateId: '',
});

const TenantProvisioning: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError } = useErrorMessage();

  const [form, setForm] = useState<ProvisionForm>(emptyForm());
  const [templateOptions, setTemplateOptions] = useState<any[]>([]);
  const [provisionResult, setProvisionResult] = useState<ProvisionResult | null>(null);
  const [migrationResult, setMigrationResult] = useState<MigrationResult | null>(null);
  const [migrationError, setMigrationError] = useState<string>('');
  const [repairResult, setRepairResult] = useState<{ RowsFixed: number } | null>(null);
  const [repairError, setRepairError] = useState<string>('');

  useEffect(() => {
    adminSvc.getDataSourceRegisterList(false).then((list: any[]) => {
      if (Array.isArray(list)) setTemplateOptions(list);
    }).catch(() => { /* template list is optional */ });
  }, []);

  const handleChange = (field: keyof ProvisionForm, value: string) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const handleProvision = async () => {
    if (!form.CompanyName.trim() || !form.DomainToken.trim() || !form.AdminEmail.trim() ||
        !form.AdminLoginName.trim() || !form.AdminPassword.trim()) {
      showError('Please fill in all required fields.');
      return;
    }
    dispatch(setIsBusy());
    setProvisionResult(null);
    try {
      const payload: any = {
        CompanyName: form.CompanyName,
        DomainToken: form.DomainToken,
        AdminEmail: form.AdminEmail,
        AdminLoginName: form.AdminLoginName,
        AdminPassword: form.AdminPassword,
      };
      if (form.TemplateId) payload.TemplateId = form.TemplateId;
      const result: ProvisionResult = await adminSvc.provisionTenant(payload);
      appHelper.debugLog('TenantProvisioning result', result);
      setProvisionResult(result);
      if (result.Success) setForm(emptyForm());
    } catch (err: any) {
      showError(err?.message ?? 'Provisioning failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleRepairAdminUsers = async () => {
    dispatch(setIsBusy());
    setRepairResult(null);
    setRepairError('');
    try {
      const result = await adminSvc.repairTenantAdminUsers();
      appHelper.debugLog('RepairAdminUsers result', result);
      setRepairResult(result);
    } catch (err: any) {
      setRepairError(err?.message ?? 'Repair failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleRunMigrations = async () => {
    dispatch(setIsBusy());
    setMigrationResult(null);
    setMigrationError('');
    try {
      const result: MigrationResult = await adminSvc.runMigrationsOnAllTenants();
      appHelper.debugLog('RunMigrations result', result);
      setMigrationResult(result);
    } catch (err: any) {
      setMigrationError(err?.message ?? 'Migration failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const inputCls = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
  const labelCls = `w-36 text-xs ${theme.label} mr-2 shrink-0`;
  const btnCls = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Tenant Provisioning</div>
      </div>

      {/* Scrollable body */}
      <div className={`w-full h-1 flex-auto overflow-auto px-5 py-5 ${theme.mainContentSection}`}>

        {/* --- Provision New Tenant --- */}
        <div className={`mb-6 p-4 border rounded-md ${theme.mainContentSection}`} style={{ borderColor: 'var(--border-color, #d1d5db)' }}>
          <div className={`text-sm font-semibold mb-3 ${theme.title}`}>Provision New Tenant</div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Company Name *</span>
            <input
              className={inputCls}
              value={form.CompanyName}
              onChange={e => handleChange('CompanyName', e.target.value)}
              autoComplete="off"
              placeholder="Acme Corp"
            />
          </div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Domain Token *</span>
            <input
              className={inputCls}
              value={form.DomainToken}
              onChange={e => handleChange('DomainToken', e.target.value)}
              autoComplete="off"
              placeholder="acme"
            />
          </div>
          <div className="flex items-start py-1">
            <span className={`${labelCls} pt-0.5`}></span>
            <span className="text-xs text-gray-400">Used as subdomain / tenant identifier (no spaces)</span>
          </div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Admin Email *</span>
            <input
              className={inputCls}
              type="email"
              value={form.AdminEmail}
              onChange={e => handleChange('AdminEmail', e.target.value)}
              autoComplete="off"
              placeholder="admin@acme.com"
            />
          </div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Admin Login Name *</span>
            <input
              className={inputCls}
              value={form.AdminLoginName}
              onChange={e => handleChange('AdminLoginName', e.target.value)}
              autoComplete="off"
              placeholder="admin"
            />
          </div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Admin Password *</span>
            <input
              className={inputCls}
              type="password"
              value={form.AdminPassword}
              onChange={e => handleChange('AdminPassword', e.target.value)}
              autoComplete="new-password"
            />
          </div>

          <div className="flex items-center py-1">
            <span className={labelCls}>Template DB</span>
            <select
              className={inputCls}
              value={form.TemplateId}
              onChange={e => handleChange('TemplateId', e.target.value)}
            >
              <option value="">(None — blank tenant)</option>
              {templateOptions.map((t: any) => (
                <option key={t.Id ?? t.id} value={t.Id ?? t.id}>
                  {t.DataSourceName ?? t.dataSourceName ?? t.Id}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-start py-1">
            <span className={`${labelCls} pt-0.5`}></span>
            <span className="text-xs text-gray-400">Seed the new tenant DB with app definitions from an existing TenantDB (e.g. AppTenantTemplateDB)</span>
          </div>

          <div className="flex items-center py-2 mt-2">
            <span className="w-36 mr-2" />
            <button className={btnCls} onClick={handleProvision}>
              <i className="fa-solid fa-circle-plus mr-1" />
              Provision Tenant
            </button>
          </div>

          {/* Provision result */}
          {provisionResult && (
            <div className={`mt-3 p-3 rounded-md text-xs ${provisionResult.Success ? 'bg-green-50 border border-green-300' : 'bg-red-50 border border-red-300'}`}>
              {provisionResult.Success ? (
                <>
                  <div className="font-semibold text-green-700 mb-1">
                    <i className="fa-solid fa-circle-check mr-1" />Tenant provisioned successfully
                  </div>
                  <div>Company ID: <strong>{provisionResult.CompanyId}</strong></div>
                  <div>Database: <strong>{provisionResult.DatabaseName}</strong></div>
                  {provisionResult.LoginUrl && (
                    <div>Login URL: <strong>{provisionResult.LoginUrl}</strong></div>
                  )}
                  <div>Migrations applied: <strong>{provisionResult.MigrationsApplied}</strong></div>
                  {provisionResult.SeededFromTemplate && (
                    <div>Seeded from: <strong>{provisionResult.SeededFromTemplate}</strong></div>
                  )}
                </>
              ) : (
                <div className="text-red-700">
                  <i className="fa-solid fa-circle-xmark mr-1" />
                  {provisionResult.ErrorMessage || 'Provisioning failed'}
                </div>
              )}
            </div>
          )}
        </div>

        {/* --- Repair Tenant Admin Users --- */}
        <div className={`mb-6 p-4 border rounded-md ${theme.mainContentSection}`} style={{ borderColor: 'var(--border-color, #d1d5db)' }}>
          <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Repair Tenant Admin Users</div>
          <p className="text-xs text-gray-500 mb-3">
            Back-fills <code>IsRegisterCompleted</code> and <code>MyOwnCompnanyId</code> for any tenant admin accounts
            that were provisioned before these fields were set. Safe to run multiple times — only touches incomplete rows.
          </p>
          <button className={btnCls} onClick={handleRepairAdminUsers}>
            <i className="fa-solid fa-wrench mr-1" />
            Repair Admin Users
          </button>

          {repairError && (
            <div className="mt-3 p-3 rounded-md text-xs bg-red-50 border border-red-300 text-red-700">
              <i className="fa-solid fa-circle-xmark mr-1" />{repairError}
            </div>
          )}
          {repairResult && !repairError && (
            <div className="mt-3 p-3 rounded-md text-xs bg-green-50 border border-green-300">
              <div className="font-semibold text-green-700">
                <i className="fa-solid fa-circle-check mr-1" />
                {repairResult.RowsFixed === 0
                  ? 'No users needed repair — all tenant admins are already complete.'
                  : `${repairResult.RowsFixed} tenant admin user${repairResult.RowsFixed !== 1 ? 's' : ''} repaired successfully.`}
              </div>
            </div>
          )}
        </div>

        {/* --- Run Migrations --- */}
        <div className={`p-4 border rounded-md ${theme.mainContentSection}`} style={{ borderColor: 'var(--border-color, #d1d5db)' }}>
          <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Run Schema Migrations</div>
          <p className="text-xs text-gray-500 mb-3">
            Applies any pending schema migrations to every registered tenant database. Safe to run on every deployment — idempotent.
          </p>
          <button className={btnCls} onClick={handleRunMigrations}>
            <i className="fa-solid fa-rotate mr-1" />
            Run Migrations on All Tenants
          </button>

          {/* Migration result */}
          {migrationError && (
            <div className="mt-3 p-3 rounded-md text-xs bg-red-50 border border-red-300 text-red-700">
              <i className="fa-solid fa-circle-xmark mr-1" />{migrationError}
            </div>
          )}
          {migrationResult && !migrationError && (
            <div className="mt-3 p-3 rounded-md text-xs bg-green-50 border border-green-300">
              <div className="font-semibold text-green-700 mb-2">
                <i className="fa-solid fa-circle-check mr-1" />Migrations completed
              </div>
              {Object.keys(migrationResult).length === 0 ? (
                <div className="text-gray-500">No tenants found or no migrations pending.</div>
              ) : (
                <table className="w-full text-xs">
                  <thead>
                    <tr className="text-left">
                      <th className="pr-4 pb-1 font-semibold">Tenant / Database</th>
                      <th className="pb-1 font-semibold">Migrations Applied</th>
                    </tr>
                  </thead>
                  <tbody>
                    {Object.entries(migrationResult).map(([db, count]) => (
                      <tr key={db}>
                        <td className="pr-4 py-0.5">{db}</td>
                        <td className="py-0.5">{count}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}
        </div>

      </div>
    </div>
  );
};

export default TenantProvisioning;
