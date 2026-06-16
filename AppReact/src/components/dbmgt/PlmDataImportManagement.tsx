import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { RootState } from '../../redux/store';
import { isAdminUserFromContext, isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';
import { plmMigrationSvc } from '../../webapi/plmMigrationSvc';
import PlmImportWizard from './plmImport/PlmImportWizard';
import type {
  PlmImportEntityStepUiState,
  PlmImportPageCache,
  PlmImportStepCode,
  PlmImportWizardState,
} from './plmImport/types';
import { createInitialEntityStepUi, PLM_IMPORT_PAGE_CACHE_SUFFIX } from './plmImport/types';

const createInitialWizardState = (): PlmImportWizardState => ({
  session: null,
  currentStepCode: 'Connect',
  targetCompanyId: null,
  saasApplicationId: null,
  plmConnectionString: '',
  connectionTested: false,
  systemDefineComplete: false,
});

const getPlmImportCacheKey = (): string | null => {
  const tabKey = getCurrentActiveTab()?.tabKey ?? null;
  return tabKey ? `${tabKey}${PLM_IMPORT_PAGE_CACHE_SUFFIX}` : null;
};

const readCachedPageState = (): { page: PlmImportPageCache; fromCache: boolean } => {
  const cacheKey = getPlmImportCacheKey();
  if (cacheKey) {
    const cached = getDataModelFromCache(cacheKey) as PlmImportPageCache | null;
    if (cached?.wizardState) {
      return {
        page: {
          wizardState: cached.wizardState,
          entityStepUi: cached.entityStepUi ?? createInitialEntityStepUi(),
        },
        fromCache: true,
      };
    }
  }
  return {
    page: {
      wizardState: createInitialWizardState(),
      entityStepUi: createInitialEntityStepUi(),
    },
    fromCache: false,
  };
};

const PlmDataImportManagement: React.FC = () => {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);

  const isAdmin = isAdminUserFromContext(userContext);
  const isSysAdmin = isMasterSysAdminFromContext(userContext);

  const cachedInit = useRef(readCachedPageState());
  const [state, setState] = useState<PlmImportWizardState>(() => cachedInit.current.page.wizardState);
  const [entityStepUi, setEntityStepUi] = useState<PlmImportEntityStepUiState>(
    () => cachedInit.current.page.entityStepUi,
  );
  const [isLoading, setIsLoading] = useState(() => !cachedInit.current.fromCache);

  const cacheKey = useMemo(() => getPlmImportCacheKey(), []);

  const pageCache = useMemo((): PlmImportPageCache => ({
    wizardState: state,
    entityStepUi,
  }), [entityStepUi, state]);

  useTabDataAutoCache(pageCache, cacheKey ?? undefined);

  const defaultCompanyId = useMemo(() => {
    const id = userContext?.CurrentWorkingCompanyId ?? userContext?.currentWorkingCompanyId;
    return typeof id === 'number' ? id : (id ? Number(id) : null);
  }, [userContext]);

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
    setState((prev) => ({
      ...prev,
      session,
      saasApplicationId: session.SaasApplicationId ?? null,
      targetCompanyId: session.CompanyId ?? null,
      currentStepCode: (session.CurrentStepCode as PlmImportStepCode) || prev.currentStepCode,
      plmConnectionString: session.PlmConnectionString?.trim()
        || (session.HasPlmConnection ? prev.plmConnectionString : ''),
      connectionTested: Boolean(stepState.connectionTested),
      systemDefineComplete: Boolean(stepState.systemDefineComplete),
    }));
  }, []);

  const patchState = useCallback((patch: Partial<PlmImportWizardState>) => {
    setState((prev) => ({ ...prev, ...patch }));
  }, []);

  const patchEntityStepUi = useCallback((patch: Partial<PlmImportEntityStepUiState>) => {
    setEntityStepUi((prev) => ({ ...prev, ...patch }));
  }, []);

  const loadActiveSession = useCallback(async (companyId?: number | null, options?: { silent?: boolean }) => {
    if (!options?.silent) setIsLoading(true);
    try {
      const result = await plmMigrationSvc.getActiveImportSession(companyId ?? undefined);
      if (result.Object) {
        applySession(result.Object);
      } else {
        patchState({
          session: null,
          targetCompanyId: isSysAdmin ? (companyId ?? null) : defaultCompanyId,
        });
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load import session.');
    } finally {
      if (!options?.silent) setIsLoading(false);
    }
  }, [applySession, defaultCompanyId, isSysAdmin, patchState, showError]);

  useEffect(() => {
    if (!isAdmin) {
      setIsLoading(false);
      return;
    }
    if (cachedInit.current.fromCache) {
      setIsLoading(false);
      return;
    }
    loadActiveSession(isSysAdmin ? null : defaultCompanyId);
  }, [isAdmin, isSysAdmin, defaultCompanyId, loadActiveSession]);

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
      <div className="h-1 flex-auto overflow-hidden">
        <PlmImportWizard
          state={state}
          entityStepUi={entityStepUi}
          isSysAdmin={isSysAdmin}
          onStateChange={patchState}
          onEntityStepUiChange={patchEntityStepUi}
          onReloadSession={() => loadActiveSession(state.targetCompanyId ?? defaultCompanyId, { silent: true })}
        />
      </div>
    </div>
  );
};

export default PlmDataImportManagement;
