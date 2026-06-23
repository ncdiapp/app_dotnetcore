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
  PlmImportDwBlueprintStepUiState,
  PlmImportWizardState,
} from './plmImport/types';
import {
  createInitialEntityStepUi,
  createInitialDwBlueprintStepUi,
  normalizePlmImportStepCode,
  normalizePlmImportTablePrefix,
  PLM_DEFAULT_TABLE_PREFIX,
  PLM_IMPORT_PAGE_CACHE_SUFFIX,
} from './plmImport/types';

const createInitialWizardState = (): PlmImportWizardState => ({
  session: null,
  currentStepCode: 'Connect',
  targetCompanyId: null,
  saasApplicationId: null,
  plmConnectionString: '',
  tablePrefix: PLM_DEFAULT_TABLE_PREFIX,
  connectionTested: false,
  systemDefineTablesComplete: false,
  systemDefineEntitiesComplete: false,
  userDefineEntitiesComplete: false,
  templatesComplete: false,
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
      const ws = cached.wizardState;
      return {
        page: {
          wizardState: {
            ...createInitialWizardState(),
            ...ws,
            currentStepCode: normalizePlmImportStepCode(ws.currentStepCode),
            tablePrefix: normalizePlmImportTablePrefix(ws.tablePrefix, PLM_DEFAULT_TABLE_PREFIX),
          },
          entityStepUi: cached.entityStepUi
            ? { ...createInitialEntityStepUi(), ...cached.entityStepUi }
            : createInitialEntityStepUi(),
          dwBlueprintStepUi: cached.dwBlueprintStepUi
            ? { ...createInitialDwBlueprintStepUi(), ...cached.dwBlueprintStepUi }
            : cached.templateStepUi
              ? { ...createInitialDwBlueprintStepUi(), ...cached.templateStepUi }
              : createInitialDwBlueprintStepUi(),
        },
        fromCache: true,
      };
    }
  }
  return {
    page: {
      wizardState: createInitialWizardState(),
      entityStepUi: createInitialEntityStepUi(),
      dwBlueprintStepUi: createInitialDwBlueprintStepUi(),
    },
    fromCache: false,
  };
};

const PlmDataImportManagement: React.FC = () => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);

  const isAdmin = isAdminUserFromContext(userContext);
  const isSysAdmin = isMasterSysAdminFromContext(userContext);

  const cachedInit = useRef(readCachedPageState());
  const [state, setState] = useState<PlmImportWizardState>(() => cachedInit.current.page.wizardState);
  const [entityStepUi, setEntityStepUi] = useState<PlmImportEntityStepUiState>(
    () => cachedInit.current.page.entityStepUi,
  );
  const [dwBlueprintStepUi, setDwBlueprintStepUi] = useState<PlmImportDwBlueprintStepUiState>(
    () => cachedInit.current.page.dwBlueprintStepUi,
  );
  const [isLoading, setIsLoading] = useState(() => !cachedInit.current.fromCache);

  const cacheKey = useMemo(() => getPlmImportCacheKey(), []);

  const pageCache = useMemo((): PlmImportPageCache => ({
    wizardState: state,
    entityStepUi,
    dwBlueprintStepUi,
  }), [dwBlueprintStepUi, entityStepUi, state]);

  useTabDataAutoCache(pageCache, cacheKey ?? undefined);

  const defaultCompanyId = useMemo(() => {
    const id = userContext?.CurrentWorkingCompanyId ?? userContext?.currentWorkingCompanyId;
    return typeof id === 'number' ? id : (id ? Number(id) : null);
  }, [userContext]);

  const applySession = useCallback((session: PlmImportWizardState['session']) => {
    if (!session) return;
    let stepState: {
      connectionTested?: boolean;
      systemDefineComplete?: boolean;
      systemDefineTablesComplete?: boolean;
      systemDefineEntitiesComplete?: boolean;
      userDefineEntitiesComplete?: boolean;
      templatesComplete?: boolean;
      tablePrefix?: string;
    } = {};
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
      currentStepCode: normalizePlmImportStepCode(session.CurrentStepCode) || prev.currentStepCode,
      plmConnectionString: session.PlmConnectionString?.trim()
        || (session.HasPlmConnection ? prev.plmConnectionString : ''),
      connectionTested: Boolean(stepState.connectionTested),
      systemDefineTablesComplete: Boolean(
        stepState.systemDefineTablesComplete ?? stepState.systemDefineComplete,
      ),
      systemDefineEntitiesComplete: Boolean(stepState.systemDefineEntitiesComplete),
      userDefineEntitiesComplete: Boolean(stepState.userDefineEntitiesComplete),
      templatesComplete: Boolean(stepState.templatesComplete),
      tablePrefix: normalizePlmImportTablePrefix(stepState.tablePrefix, PLM_DEFAULT_TABLE_PREFIX),
    }));
  }, []);

  const patchState = useCallback((patch: Partial<PlmImportWizardState>) => {
    setState((prev) => ({ ...prev, ...patch }));
  }, []);

  const patchEntityStepUi = useCallback((patch: Partial<PlmImportEntityStepUiState>) => {
    setEntityStepUi((prev) => ({ ...prev, ...patch }));
  }, []);

  const patchDwBlueprintStepUi = useCallback((patch: Partial<PlmImportDwBlueprintStepUiState>) => {
    setDwBlueprintStepUi((prev) => ({ ...prev, ...patch }));
  }, []);

  const resetWizard = useCallback((companyId?: number | null) => {
    setState({
      ...createInitialWizardState(),
      targetCompanyId: isSysAdmin ? (companyId ?? null) : defaultCompanyId,
    });
    setEntityStepUi(createInitialEntityStepUi());
    setDwBlueprintStepUi(createInitialDwBlueprintStepUi());
  }, [defaultCompanyId, isSysAdmin]);

  const discardSession = useCallback(async () => {
    const companyId = state.targetCompanyId ?? defaultCompanyId;
    try {
      const result = await plmMigrationSvc.discardImportSession(
        state.session?.SessionId ?? undefined,
        companyId ?? undefined,
      );
      if (result.Object) {
        resetWizard(companyId);
        showInfo('Import session discarded. You can start a new import.', true);
      } else {
        const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Failed to discard import session.';
        showError(msg);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to discard import session.');
    }
  }, [defaultCompanyId, resetWizard, showError, showInfo, state.session?.SessionId, state.targetCompanyId]);

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
          dwBlueprintStepUi={dwBlueprintStepUi}
          isSysAdmin={isSysAdmin}
          onStateChange={patchState}
          onEntityStepUiChange={patchEntityStepUi}
          onDwBlueprintStepUiChange={patchDwBlueprintStepUi}
          onReloadSession={() => loadActiveSession(state.targetCompanyId ?? defaultCompanyId, { silent: true })}
          onDiscardSession={discardSession}
        />
      </div>
    </div>
  );
};

export default PlmDataImportManagement;
