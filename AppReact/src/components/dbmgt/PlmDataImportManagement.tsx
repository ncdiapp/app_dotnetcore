import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { RootState } from '../../redux/store';
import { isAdminUserFromContext, isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';
import { plmMigrationSvc } from '../../webapi/plmMigrationSvc';
import PlmImportWizard from './plmImport/PlmImportWizard';
import type { PlmImportStepCode, PlmImportWizardState } from './plmImport/types';

const createInitialState = (): PlmImportWizardState => ({
  session: null,
  currentStepCode: 'Connect',
  targetCompanyId: null,
  saasApplicationId: null,
  plmConnectionString: '',
  connectionTested: false,
  systemDefineComplete: false,
});

const PlmDataImportManagement: React.FC = () => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);

  const isAdmin = isAdminUserFromContext(userContext);
  const isSysAdmin = isMasterSysAdminFromContext(userContext);

  const [state, setState] = useState<PlmImportWizardState>(createInitialState);
  const [isLoading, setIsLoading] = useState(true);
  const [resumePromptOpen, setResumePromptOpen] = useState(false);

  const defaultCompanyId = useMemo(() => {
    const id = userContext?.CurrentWorkingCompanyId ?? userContext?.currentWorkingCompanyId;
    return typeof id === 'number' ? id : (id ? Number(id) : null);
  }, [userContext]);

  const patchState = useCallback((patch: Partial<PlmImportWizardState>) => {
    setState((prev) => ({ ...prev, ...patch }));
  }, []);

  const applySession = useCallback((session: PlmImportWizardState['session']) => {
    if (!session) return;
    let stepState: { connectionTested?: boolean; systemDefineComplete?: boolean } = {};
    if (session.StepStateJson) {
      try {
        stepState = JSON.parse(session.StepStateJson);
      } catch {
        stepState = {};
      }
    }
    patchState({
      session,
      saasApplicationId: session.SaasApplicationId ?? null,
      targetCompanyId: session.CompanyId ?? null,
      currentStepCode: (session.CurrentStepCode as PlmImportStepCode) || 'Connect',
      plmConnectionString: session.PlmConnectionString ?? '',
      connectionTested: Boolean(stepState.connectionTested),
      systemDefineComplete: Boolean(stepState.systemDefineComplete),
    });
  }, [patchState]);

  const loadActiveSession = useCallback(async (companyId?: number | null) => {
    setIsLoading(true);
    try {
      const result = await plmMigrationSvc.getActiveImportSession(companyId ?? undefined);
      if (result.Object) {
        applySession(result.Object);
        setResumePromptOpen(true);
      } else {
        patchState({
          session: null,
          targetCompanyId: isSysAdmin ? (companyId ?? null) : defaultCompanyId,
        });
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load import session.');
    } finally {
      setIsLoading(false);
    }
  }, [applySession, defaultCompanyId, isSysAdmin, patchState, showError]);

  useEffect(() => {
    if (!isAdmin) {
      setIsLoading(false);
      return;
    }
    loadActiveSession(isSysAdmin ? null : defaultCompanyId);
  }, [isAdmin, isSysAdmin, defaultCompanyId, loadActiveSession]);

  const handleDiscardAndRestart = useCallback(async () => {
    try {
      await plmMigrationSvc.discardImportSession(
        state.session?.SessionId ?? null,
        state.targetCompanyId ?? defaultCompanyId,
      );
      setResumePromptOpen(false);
      setState({
        ...createInitialState(),
        targetCompanyId: isSysAdmin ? state.targetCompanyId : defaultCompanyId,
      });
      showInfo('Previous session discarded.', true);
    } catch (e: any) {
      showError(e?.message || 'Failed to discard session.');
    }
  }, [defaultCompanyId, isSysAdmin, showError, showInfo, state.session?.SessionId, state.targetCompanyId]);

  const handleContinueSession = useCallback(() => {
    setResumePromptOpen(false);
  }, []);

  if (!isAdmin) {
    return (
      <div className={`flex items-center justify-center h-full p-6 ${theme.mainContentSection}`}>
        <div className="text-center max-w-md">
          <i className="fa-solid fa-lock text-3xl mb-3" />
          <p className={`text-sm ${theme.label}`}>PLM Data Import requires SaasCompanyAdmin or SysAdmin.</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className={`flex items-center justify-center h-full ${theme.mainContentSection}`}>
        <i className="fa-solid fa-spinner fa-spin text-2xl" />
      </div>
    );
  }

  return (
    <div className="h-full w-full overflow-hidden flex flex-col">
      {resumePromptOpen && state.session && (
        <div className={`flex-none mx-4 mt-3 p-3 rounded border text-xs ${theme.inputBox}`}>
          <div className={`font-semibold mb-1 ${theme.label}`}>Resume in-progress import?</div>
          <p className={theme.menu_secondary}>
            Session #{state.session.SessionId} was saved on{' '}
            {state.session.UpdatedAt ? new Date(state.session.UpdatedAt).toLocaleString() : '—'}.
          </p>
          <div className="flex gap-2 mt-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handleContinueSession}
            >
              Continue
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
              onClick={handleDiscardAndRestart}
            >
              Discard &amp; start fresh
            </button>
          </div>
        </div>
      )}

      <div className="h-1 flex-auto overflow-hidden">
        <PlmImportWizard
          state={state}
          isSysAdmin={isSysAdmin}
          onStateChange={patchState}
          onReloadSession={() => loadActiveSession(state.targetCompanyId ?? defaultCompanyId)}
        />
      </div>
    </div>
  );
};

export default PlmDataImportManagement;
