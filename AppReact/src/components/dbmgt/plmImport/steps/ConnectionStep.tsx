import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { adminSvc } from '../../../../webapi/adminsvc';
import { plmMigrationSvc } from '../../../../webapi/plmMigrationSvc';
import type { PlmImportWizardState } from '../types';

type AppMenuItem = {
  Id?: number;
  MenuId?: number;
  Name?: string;
  DisplayName?: string;
};

type CompanyItem = {
  CompanyId?: number;
  CompanyName?: string;
};

export type ConnectionStepProps = {
  state: PlmImportWizardState;
  isSysAdmin: boolean;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onSessionSaved: () => void;
};

const ConnectionStep: React.FC<ConnectionStepProps> = ({
  state,
  isSysAdmin,
  onStateChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showWarning, showInfo } = useErrorMessage();

  const [applications, setApplications] = useState<AppMenuItem[]>([]);
  const [companies, setCompanies] = useState<CompanyItem[]>([]);
  const [isConnecting, setIsConnecting] = useState(false);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const apps = await adminSvc.retrieveSelectedApplicationPackages(false);
        if (!cancelled) setApplications(Array.isArray(apps) ? apps : []);
      } catch {
        if (!cancelled) showError('Failed to load applications.');
      }
    })();
    return () => { cancelled = true; };
  }, [showError]);

  useEffect(() => {
    if (!isSysAdmin) return;
    let cancelled = false;
    (async () => {
      try {
        const list = await adminSvc.retrieveAllSaasCompanyDtoList();
        if (!cancelled) setCompanies(Array.isArray(list) ? list : []);
      } catch {
        if (!cancelled) showError('Failed to load company list.');
      }
    })();
    return () => { cancelled = true; };
  }, [isSysAdmin, showError]);

  const selectedAppName = useMemo(() => {
    const id = state.saasApplicationId;
    if (!id) return '';
    const app = applications.find((a) => (a.Id ?? a.MenuId) === id);
    return app?.DisplayName ?? app?.Name ?? '';
  }, [applications, state.saasApplicationId]);

  const handleConnect = useCallback(async () => {
    if (!state.saasApplicationId) {
      showWarning('Select an import target application first.');
      return;
    }
    if (isSysAdmin && !state.targetCompanyId) {
      showWarning('Select a target company.');
      return;
    }
    if (!state.plmConnectionString?.trim()) {
      showWarning('Enter a PLM connection string first.');
      return;
    }

    setIsConnecting(true);
    try {
      const testResult = await plmMigrationSvc.testPlmConnection({
        ConnectionString: state.plmConnectionString.trim(),
        TargetCompanyId: state.targetCompanyId,
      });

      if (!testResult.Object?.IsSuccess) {
        onStateChange({ connectionTested: false });
        const msg = testResult.Object?.ErrorMessage
          || testResult.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Connection failed.';
        showError(msg);
        return;
      }

      const discoverResult = await plmMigrationSvc.discoverPlmDataSources({
        PlmConnectionString: state.plmConnectionString.trim(),
        SaasApplicationId: state.saasApplicationId,
        TargetCompanyId: state.targetCompanyId,
        SessionId: state.session?.SessionId ?? null,
      });

      if (!discoverResult.Object?.IsSuccess) {
        onStateChange({ connectionTested: false });
        const msg = discoverResult.Object?.ErrorMessage
          || discoverResult.ValidationResult?.Items
            ?.filter((i: { Type?: string }) => i.Type !== 'Warning')
            ?.map((i: { Message: string }) => i.Message)
            .join('; ')
          || 'Data source discovery failed.';
        showError(msg);
        return;
      }

      const saveResult = await plmMigrationSvc.saveImportSession({
        SessionId: state.session?.SessionId ?? null,
        SessionGuid: state.session?.SessionGuid ?? null,
        CompanyId: state.targetCompanyId,
        SaasApplicationId: state.saasApplicationId,
        SaasApplicationName: selectedAppName,
        CurrentStepCode: 'Connect',
        PlmConnectionString: state.plmConnectionString.trim(),
        StepStateJson: JSON.stringify({
          connectionTested: true,
          systemDefineComplete: state.systemDefineComplete,
        }),
        DataSourceDiscoveryJson: discoverResult.Object
          ? JSON.stringify(discoverResult.Object)
          : undefined,
      });

      if (!saveResult.Object) {
        onStateChange({ connectionTested: false });
        const msg = saveResult.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Connected but failed to save session.';
        showError(msg);
        return;
      }

      onStateChange({
        connectionTested: true,
        session: saveResult.Object,
        plmConnectionString: saveResult.Object?.PlmConnectionString?.trim() || state.plmConnectionString,
      });
      onSessionSaved();

      const dbName = testResult.Object.DatabaseName || 'database';
      showInfo(`Connected to ${dbName} and data sources discovered.`, true);
    } catch (e: any) {
      onStateChange({ connectionTested: false });
      showError(e?.message || 'Connect failed.');
    } finally {
      setIsConnecting(false);
    }
  }, [
    isSysAdmin,
    onSessionSaved,
    onStateChange,
    selectedAppName,
    showError,
    showInfo,
    showWarning,
    state,
  ]);

  return (
    <div className="flex flex-col gap-4 p-4 h-full overflow-auto">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 1 — Connect &amp; Discover</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Select the import target application and enter the legacy PLM SQL Server connection string.
        </p>
      </div>

      {isSysAdmin && (
        <div className="flex items-center gap-2">
          <label className={`w-40 text-xs ${theme.label} mr-2`}>Company</label>
          <select
            className={`flex-auto w-64 h-7 px-2 text-xs border ${theme.inputBox}`}
            value={state.targetCompanyId ?? ''}
            onChange={(e) => onStateChange({
              targetCompanyId: e.target.value ? Number(e.target.value) : null,
              connectionTested: false,
            })}
          >
            <option value="">— Select company —</option>
            {companies.map((c) => (
              <option key={c.CompanyId} value={c.CompanyId}>
                {c.CompanyName}
              </option>
            ))}
          </select>
        </div>
      )}

      <div className="flex items-center gap-2">
        <label className={`w-40 text-xs ${theme.label} mr-2`}>Import To Application</label>
        <select
          className={`flex-auto w-64 h-7 px-2 text-xs border ${theme.inputBox}`}
          value={state.saasApplicationId ?? ''}
          onChange={(e) => onStateChange({
            saasApplicationId: e.target.value ? Number(e.target.value) : null,
            connectionTested: false,
          })}
        >
          <option value="">— Select application —</option>
          {applications.map((a) => {
            const id = a.Id ?? a.MenuId;
            return (
              <option key={id} value={id}>
                {a.DisplayName ?? a.Name}
              </option>
            );
          })}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <label className={`text-xs ${theme.label}`}>PLM connection string</label>
        <textarea
          className={`w-full min-h-[80px] px-2 py-1 text-xs border ${theme.inputBox}`}
          value={state.plmConnectionString}
          onChange={(e) => onStateChange({
            plmConnectionString: e.target.value,
            connectionTested: false,
          })}
          placeholder="Data Source=...;Initial Catalog=...;User ID=...;Password=..."
        />
        {state.connectionTested && (
          <p className={`text-xs ${theme.menu_secondary}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            PLM connected successfully.
          </p>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          onClick={handleConnect}
          disabled={isConnecting}
        >
          <i className="fa-solid fa-plug mr-1" />
          {isConnecting ? 'Connecting…' : 'Connect'}
        </button>
      </div>
    </div>
  );
};

export default ConnectionStep;
