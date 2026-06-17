import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmImportJobDto,
  PlmSystemDefineEntityPreviewItemDto,
  PlmTableExportPlanItemDto,
  PlmUserDefineEntityPreviewItemDto,
} from '../../../../webapi/plmMigrationSvc';
import type {
  PlmImportEntityStepUiState,
  PlmImportWizardState,
  PlmSystemDefineWorkflowStep,
} from '../types';
import { buildPlmImportStepStateJson } from '../types';

export type EntityStepProps = {
  state: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onEntityStepUiChange: (patch: Partial<PlmImportEntityStepUiState>) => void;
  onSessionSaved: () => void;
};

const TABLE_EXPORT_JOB = 'PlmTableExport';
const ENTITY_IMPORT_JOB = 'SystemDefineEntityImport';
const USER_DEFINE_IMPORT_JOB = 'UserDefineEntityImport';

type WorkflowStepDef = {
  step: PlmSystemDefineWorkflowStep;
  title: string;
  listLabel: string;
  executeLabel: string;
  hint: string;
};

const WORKFLOW_STEPS: WorkflowStepDef[] = [
  {
    step: 1,
    title: 'Import PLM Entity Tables',
    listLabel: 'List PLM Importing Tables',
    executeLabel: 'Execute Import Tables',
    hint: 'List PLM tables and review Source OK, then execute import into the tenant database.',
  },
  {
    step: 2,
    title: 'Import PLM Entities',
    listLabel: 'List PLM Importing Entities',
    executeLabel: 'Execute Import Entities',
    hint: 'List entity metadata from pdmEntity. Only Status = Ready rows will be imported.',
  },
];

const EntityStep: React.FC<EntityStepProps> = ({
  state,
  entityStepUi,
  onStateChange,
  onEntityStepUiChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const {
    activeTab,
    viewingWorkflowStep,
    planItems,
    entityPlanItems,
    userDefinePlanItems,
    activeJob,
    isExporting,
    isEntityImporting,
    isUserDefineImporting,
  } = entityStepUi;
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [isEntityPreviewLoading, setIsEntityPreviewLoading] = useState(false);
  const [isUserDefinePreviewLoading, setIsUserDefinePreviewLoading] = useState(false);
  const [tableCv] = useState<CollectionView>(() => new CollectionView<PlmTableExportPlanItemDto>([]));
  const [entityCv] = useState<CollectionView>(() => new CollectionView<PlmSystemDefineEntityPreviewItemDto>([]));
  const [userDefineCv] = useState<CollectionView>(() => new CollectionView<PlmUserDefineEntityPreviewItemDto>([]));

  const sessionId = state.session?.SessionId ?? null;
  const userDefineLocked = !state.systemDefineEntitiesComplete;
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    tableCv.sourceCollection = planItems;
    tableCv.refresh();
  }, [planItems, tableCv]);

  useEffect(() => {
    entityCv.sourceCollection = entityPlanItems;
    entityCv.refresh();
  }, [entityCv, entityPlanItems]);

  useEffect(() => {
    userDefineCv.sourceCollection = userDefinePlanItems;
    userDefineCv.refresh();
  }, [userDefineCv, userDefinePlanItems]);

  // Clear stale exporting flags restored from tab cache without an active job.
  useEffect(() => {
    if ((isExporting || isEntityImporting || isUserDefineImporting) && !activeJob?.JobId) {
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false });
    }
  }, [activeJob?.JobId, isEntityImporting, isExporting, isUserDefineImporting, onEntityStepUiChange]);

  const pollJob = useCallback(async (jobId: number) => {
    const result = await plmMigrationSvc.getImportJob(jobId);
    if (result.Object) onEntityStepUiChange({ activeJob: result.Object });
    return result.Object;
  }, [onEntityStepUiChange]);

  const saveStepState = useCallback(async (patch: Partial<PlmImportWizardState>) => {
    if (!sessionId) return;
    const merged = { ...state, ...patch };
    await plmMigrationSvc.saveImportSession({
      SessionId: sessionId,
      SessionGuid: state.session?.SessionGuid ?? null,
      CompanyId: state.targetCompanyId,
      SaasApplicationId: state.saasApplicationId,
      CurrentStepCode: 'Entity',
      PlmConnectionString: state.plmConnectionString || undefined,
      StepStateJson: buildPlmImportStepStateJson({
        connectionTested: merged.connectionTested,
        systemDefineTablesComplete: merged.systemDefineTablesComplete,
        systemDefineEntitiesComplete: merged.systemDefineEntitiesComplete,
        userDefineEntitiesComplete: merged.userDefineEntitiesComplete,
        tablePrefix: merged.tablePrefix,
      }),
      DataSourceDiscoveryJson: state.session?.DataSourceDiscoveryJson ?? undefined,
    });
    onSessionSaved();
  }, [onSessionSaved, sessionId, state]);

  const terminalJobStatuses = useMemo(() => ['Completed', 'Failed', 'Cancelled'], []);

  const isTableJobRunning = useMemo(() => {
    if (isExporting) return true;
    if (!activeJob?.JobId || activeJob.JobType !== TABLE_EXPORT_JOB) return false;
    return !terminalJobStatuses.includes(activeJob.Status || '');
  }, [activeJob, isExporting, terminalJobStatuses]);

  const isEntityJobRunning = useMemo(() => {
    if (isEntityImporting) return true;
    if (!activeJob?.JobId || activeJob.JobType !== ENTITY_IMPORT_JOB) return false;
    return !terminalJobStatuses.includes(activeJob.Status || '');
  }, [activeJob, isEntityImporting, terminalJobStatuses]);

  const isUserDefineJobRunning = useMemo(() => {
    if (isUserDefineImporting) return true;
    if (!activeJob?.JobId || activeJob.JobType !== USER_DEFINE_IMPORT_JOB) return false;
    return !terminalJobStatuses.includes(activeJob.Status || '');
  }, [activeJob, isUserDefineImporting, terminalJobStatuses]);

  const isAnyJobRunning = isTableJobRunning || isEntityJobRunning || isUserDefineJobRunning;

  const importableTableCount = useMemo(
    () => planItems.filter((t) => t.SourceTableExists).length,
    [planItems],
  );

  const importableEntityCount = useMemo(
    () => entityPlanItems.filter((e) => e.ImportStatus === 'Ready').length,
    [entityPlanItems],
  );

  const importableUserDefineCount = useMemo(
    () => userDefinePlanItems.filter((e) => e.ImportStatus === 'Ready').length,
    [userDefinePlanItems],
  );

  const currentWorkflowStep = useMemo((): PlmSystemDefineWorkflowStep | 'done' => {
    if (isTableJobRunning) return 1;
    if (isEntityJobRunning) return 2;
    if (!state.systemDefineTablesComplete) return 1;
    if (!state.systemDefineEntitiesComplete) return 2;
    return 'done';
  }, [
    isEntityJobRunning,
    isTableJobRunning,
    state.systemDefineEntitiesComplete,
    state.systemDefineTablesComplete,
  ]);

  const stepCompleted = useCallback((step: PlmSystemDefineWorkflowStep): boolean => {
    switch (step) {
      case 1: return state.systemDefineTablesComplete;
      case 2: return state.systemDefineEntitiesComplete;
      default: return false;
    }
  }, [state.systemDefineEntitiesComplete, state.systemDefineTablesComplete]);

  const stepUnlocked = useCallback((step: PlmSystemDefineWorkflowStep): boolean => {
    switch (step) {
      case 1: return true;
      case 2: return state.systemDefineTablesComplete;
      default: return false;
    }
  }, [state.systemDefineTablesComplete]);

  const finalizeImportJob = useCallback(async (job: PlmImportJobDto, showCompletionToast: boolean) => {
    if (finalizedJobIdsRef.current.has(job.JobId)) {
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
      return;
    }
    finalizedJobIdsRef.current.add(job.JobId);

    const isTableJob = job.JobType === TABLE_EXPORT_JOB;
    const isEntityJob = job.JobType === ENTITY_IMPORT_JOB;
    const isUserDefineJob = job.JobType === USER_DEFINE_IMPORT_JOB;

    if (job.Status === 'Completed') {
      if (isTableJob) {
        if (showCompletionToast && !state.systemDefineTablesComplete) {
          showInfo('PLM table import completed.', true);
        }
        onStateChange({ systemDefineTablesComplete: true });
        if (!state.systemDefineTablesComplete) {
          await saveStepState({ systemDefineTablesComplete: true });
        }
        onEntityStepUiChange({ viewingWorkflowStep: 2 });
      } else if (isEntityJob) {
        if (showCompletionToast && !state.systemDefineEntitiesComplete) {
          showInfo('System Define entity metadata import completed.', true);
        }
        onStateChange({ systemDefineEntitiesComplete: true });
        if (!state.systemDefineEntitiesComplete) {
          await saveStepState({ systemDefineEntitiesComplete: true });
        }
      } else if (isUserDefineJob) {
        if (showCompletionToast && !state.userDefineEntitiesComplete) {
          showInfo('User Define entity import completed.', true);
        }
        onStateChange({ userDefineEntitiesComplete: true });
        if (!state.userDefineEntitiesComplete) {
          await saveStepState({ userDefineEntitiesComplete: true });
        }
      }
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
      return;
    }

    if (job.Status === 'Failed' || job.Status === 'Cancelled') {
      showError(job.ErrorMessage || job.ProgressMessage || 'Import failed.');
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
    }
  }, [
    onEntityStepUiChange,
    onStateChange,
    saveStepState,
    showError,
    showInfo,
    state.systemDefineEntitiesComplete,
    state.systemDefineTablesComplete,
    state.userDefineEntitiesComplete,
  ]);

  useEffect(() => {
    if (!activeJob?.JobId) return undefined;
    if (terminalJobStatuses.includes(activeJob.Status || '')) {
      finalizeImportJob(activeJob, false);
      return undefined;
    }

    const timer = window.setInterval(async () => {
      try {
        const job = await pollJob(activeJob.JobId);
        if (!job) return;
        if (terminalJobStatuses.includes(job.Status || '')) {
          await finalizeImportJob(job, true);
        }
      } catch {
        // keep polling
      }
    }, 2000);

    return () => window.clearInterval(timer);
  }, [activeJob, finalizeImportJob, pollJob, terminalJobStatuses]);

  const handleTablePreview = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing tables.');
      return;
    }
    setIsPreviewLoading(true);
    try {
      const result = await plmMigrationSvc.previewPlmTableExportPlan(sessionId);
      const tables = result.Object?.Tables ?? [];
      if (result.Object?.IsSuccess && tables.length > 0) {
        onEntityStepUiChange({ planItems: tables });
        const missingCount = result.Object.MissingSourceTableCount
          ?? tables.filter((t) => !t.SourceTableExists).length;
        if (missingCount > 0) {
          const detail = result.Object.ErrorMessage
            || result.Object.Issues?.map((issue) =>
              `${issue.SchemaOwner}.${issue.TableName}: EntityID=${issue.EntityId}, EntityCode=${issue.EntityCode ?? ''}`,
            ).join('; ')
            || result.ValidationResult?.Items
              ?.filter((i: { Type?: string }) => i.Type === 'Warning')
              ?.map((i: { Message: string }) => i.Message)
              .join('; ');
          showWarning(detail || `${missingCount} table(s) not found in PLM source. See Source OK column.`);
        } else {
          showInfo(`Found ${tables.length} PLM table(s) ready to import.`, true);
        }
      } else {
        onEntityStepUiChange({ planItems: [] });
        const msg = result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Preview failed.';
        showError(msg);
      }
    } catch (e: any) {
      showError(e?.message || 'Preview failed.');
    } finally {
      setIsPreviewLoading(false);
    }
  }, [onEntityStepUiChange, sessionId, showError, showInfo, showWarning]);

  const handleTableImport = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing tables.');
      return;
    }
    onEntityStepUiChange({ isExporting: true, activeJob: null });
    try {
      const result = await plmMigrationSvc.executePlmTableExport(sessionId);
      if (result.Object?.JobId) {
        onEntityStepUiChange({ activeJob: result.Object });
      } else {
        onEntityStepUiChange({ isExporting: false });
        const msg = result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start import job.';
        showError(msg);
      }
    } catch (e: any) {
      onEntityStepUiChange({ isExporting: false });
      showError(e?.message || 'Failed to start import.');
    }
  }, [onEntityStepUiChange, sessionId, showError]);

  const handleEntityPreview = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing entity metadata.');
      return;
    }
    setIsEntityPreviewLoading(true);
    try {
      const result = await plmMigrationSvc.previewSystemDefineEntityImport(sessionId);
      const entities = result.Object?.Entities ?? [];
      if (result.Object?.IsSuccess) {
        onEntityStepUiChange({ entityPlanItems: entities });
        const ready = result.Object.ReadyCount ?? entities.filter((e) => e.ImportStatus === 'Ready').length;
        const skipped = result.Object.SkippedCount ?? entities.filter((e) => e.ImportStatus === 'Skipped').length;
        const blockers = result.Object.BlockerCount ?? entities.filter((e) => e.ImportStatus === 'Blocked').length;
        if (blockers > 0) {
          const detail = result.Object.Blockers?.map((b) =>
            `${b.TargetEntityCode}: ${b.Issue}`,
          ).join('; ')
            || result.ValidationResult?.Items
              ?.filter((i: { Type?: string }) => i.Type === 'Warning')
              ?.map((i: { Message: string }) => i.Message)
              .join('; ');
          showWarning(detail || `${blockers} entity blocker(s) found.`);
        } else if (skipped > 0) {
          showWarning(`${ready} ready, ${skipped} skipped. Review Status and Skip reason columns.`);
        } else {
          showInfo(`${ready} entity(ies) ready to import.`, true);
        }
      } else {
        onEntityStepUiChange({ entityPlanItems: [] });
        const msg = result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Preview failed.';
        showError(msg);
      }
    } catch (e: any) {
      showError(e?.message || 'Preview failed.');
    } finally {
      setIsEntityPreviewLoading(false);
    }
  }, [onEntityStepUiChange, sessionId, showError, showInfo, showWarning]);

  const handleEntityImport = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing entity metadata.');
      return;
    }
    onEntityStepUiChange({ isEntityImporting: true, activeJob: null });
    try {
      const result = await plmMigrationSvc.executeSystemDefineEntityImport(sessionId);
      if (result.Object?.JobId) {
        onEntityStepUiChange({ activeJob: result.Object });
      } else {
        onEntityStepUiChange({ isEntityImporting: false });
        const msg = result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start entity import job.';
        showError(msg);
      }
    } catch (e: any) {
      onEntityStepUiChange({ isEntityImporting: false });
      showError(e?.message || 'Failed to start entity import.');
    }
  }, [onEntityStepUiChange, sessionId, showError]);

  const handleUserDefinePreview = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing User Define entities.');
      return;
    }
    setIsUserDefinePreviewLoading(true);
    try {
      const result = await plmMigrationSvc.previewUserDefineEntityImport(sessionId);
      const entities = result.Object?.Entities ?? [];
      if (result.Object?.IsSuccess) {
        onEntityStepUiChange({ userDefinePlanItems: entities });
        const ready = result.Object.ReadyCount ?? entities.filter((e) => e.ImportStatus === 'Ready').length;
        const skipped = result.Object.SkippedCount ?? entities.filter((e) => e.ImportStatus === 'Skipped').length;
        const blockers = result.Object.BlockerCount ?? entities.filter((e) => e.ImportStatus === 'Blocked').length;
        if (blockers > 0) {
          const detail = result.Object.Blockers?.map((b) =>
            `${b.TargetEntityCode}: ${b.Issue}`,
          ).join('; ')
            || result.ValidationResult?.Items
              ?.filter((i: { Type?: string }) => i.Type === 'Warning')
              ?.map((i: { Message: string }) => i.Message)
              .join('; ');
          showWarning(detail || `${blockers} entity blocker(s) found.`);
        } else if (skipped > 0) {
          showWarning(`${ready} ready, ${skipped} skipped. Review Status and Skip reason columns.`);
        } else {
          showInfo(`${ready} User Define entity(ies) ready to import.`, true);
        }
      } else {
        onEntityStepUiChange({ userDefinePlanItems: [] });
        const msg = result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Preview failed.';
        showError(msg);
      }
    } catch (e: any) {
      showError(e?.message || 'Preview failed.');
    } finally {
      setIsUserDefinePreviewLoading(false);
    }
  }, [onEntityStepUiChange, sessionId, showError, showInfo, showWarning]);

  const handleUserDefineImport = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before importing User Define entities.');
      return;
    }
    onEntityStepUiChange({ isUserDefineImporting: true, activeJob: null });
    try {
      const result = await plmMigrationSvc.executeUserDefineEntityImport(sessionId);
      if (result.Object?.JobId) {
        onEntityStepUiChange({ activeJob: result.Object });
      } else {
        onEntityStepUiChange({ isUserDefineImporting: false });
        const msg = result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start User Define import job.';
        showError(msg);
      }
    } catch (e: any) {
      onEntityStepUiChange({ isUserDefineImporting: false });
      showError(e?.message || 'Failed to start User Define import.');
    }
  }, [onEntityStepUiChange, sessionId, showError]);

  const activeStepNumber = currentWorkflowStep === 'done' ? 2 : currentWorkflowStep;
  const isWorkflowComplete = currentWorkflowStep === 'done';

  const jobProgress = useMemo(() => {
    if (!activeJob?.JobId || !isAnyJobRunning) return null;
    return `${activeJob.ProgressPercent ?? 0}% — ${activeJob.ProgressMessage || activeJob.Status || ''}`;
  }, [activeJob, isAnyJobRunning]);

  const viewingStepDef = WORKFLOW_STEPS.find((s) => s.step === viewingWorkflowStep) ?? WORKFLOW_STEPS[0];
  const showTableGrid = viewingWorkflowStep === 1;
  const isResultLoading = showTableGrid ? isPreviewLoading : isEntityPreviewLoading;
  const resultEmpty = showTableGrid ? planItems.length === 0 : entityPlanItems.length === 0;

  const resultSummary = useMemo(() => {
    if (showTableGrid) {
      if (planItems.length === 0) return null;
      const ok = planItems.filter((t) => t.SourceTableExists).length;
      return `${planItems.length} table(s) · ${ok} importable`;
    }
    if (entityPlanItems.length === 0) return null;
    const ready = entityPlanItems.filter((e) => e.ImportStatus === 'Ready').length;
    const skipped = entityPlanItems.filter((e) => e.ImportStatus === 'Skipped').length;
    const blocked = entityPlanItems.filter((e) => e.ImportStatus === 'Blocked').length;
    return `${entityPlanItems.length} entity(ies) · ${ready} ready · ${skipped} skipped · ${blocked} blocked`;
  }, [entityPlanItems, planItems, showTableGrid]);

  const showJobProgress = Boolean(jobProgress) && (
    (isTableJobRunning && viewingWorkflowStep === 1)
    || (isEntityJobRunning && viewingWorkflowStep === 2)
  );

  const listButtonDisabled = showTableGrid
    ? isPreviewLoading || isAnyJobRunning
    : isEntityPreviewLoading || isAnyJobRunning;

  const executeButtonDisabled = showTableGrid
    ? isAnyJobRunning || isTableJobRunning || importableTableCount === 0
    : isAnyJobRunning || isEntityJobRunning || importableEntityCount === 0;

  const executeButtonTitle = showTableGrid
    ? (importableTableCount === 0
      ? 'Run List PLM Importing Tables first and ensure at least one Source OK row.'
      : (state.systemDefineTablesComplete ? 'Re-import tables into the tenant database.' : undefined))
    : (importableEntityCount === 0
      ? 'Run List PLM Importing Entities first and ensure at least one Ready row.'
      : (state.systemDefineEntitiesComplete ? 'Re-import entity metadata.' : undefined));

  const executeButtonLabel = showTableGrid
    ? (isTableJobRunning ? 'Importing…' : viewingStepDef.executeLabel)
    : (isEntityJobRunning ? 'Importing…' : viewingStepDef.executeLabel);

  const listButtonLabel = showTableGrid
    ? (isPreviewLoading ? 'Loading…' : viewingStepDef.listLabel)
    : (isEntityPreviewLoading ? 'Loading…' : viewingStepDef.listLabel);

  const handleWorkflowStepClick = useCallback((step: PlmSystemDefineWorkflowStep) => {
    if (!stepUnlocked(step)) return;
    onEntityStepUiChange({ viewingWorkflowStep: step });
  }, [onEntityStepUiChange, stepUnlocked]);

  const handleListClick = showTableGrid ? handleTablePreview : handleEntityPreview;
  const handleExecuteClick = showTableGrid ? handleTableImport : handleEntityImport;

  const subTabClass = (isActive: boolean, isDisabled = false) => {
    const base = 'inline-flex items-center gap-1.5 px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors';
    if (isDisabled) {
      return `${base} border-transparent opacity-40 cursor-not-allowed ${theme.tab}`;
    }
    if (isActive) {
      return `${base} ${theme.tab_active}`;
    }
    return `${base} border-transparent hover:opacity-90 cursor-pointer ${theme.tab}`;
  };

  const workflowStepClass = (
    isViewing: boolean,
    isCurrent: boolean,
    completed: boolean,
    unlocked: boolean,
  ) => {
    const base = 'inline-flex items-center gap-1.5 shrink-0 px-2 py-1 rounded-[4px] text-xs transition-colors';
    if (!unlocked) {
      return `${base} opacity-40 cursor-not-allowed ${theme.menu_secondary}`;
    }
    if (isViewing) {
      return `${base} cursor-pointer font-semibold ${theme.tab_active}`;
    }
    if (isCurrent) {
      return `${base} cursor-pointer font-semibold ${theme.label}`;
    }
    if (completed) {
      return `${base} cursor-pointer hover:opacity-90 ${theme.tab}`;
    }
    return `${base} cursor-pointer opacity-70 ${theme.tab}`;
  };

  const resultPanelTitle = showTableGrid
    ? 'Need To Import PLM System Defined Entity Tables'
    : 'Need To Import PLM System Defined Entities';

  return (
    <div className="flex flex-col gap-3 p-4 h-full overflow-hidden">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 2 — Entity Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Import System Define PLM tables, then AppEntityInfo metadata — follow steps 1 → 2 in order.
        </p>
      </div>

      <div className={`flex-none rounded-t border ${theme.inputBox}`}>
        <div className={`flex border-b ${theme.mainContentSection}`}>
          <button
            type="button"
            className={subTabClass(activeTab === 'system')}
            onClick={() => onEntityStepUiChange({ activeTab: 'system' })}
          >
            <i className="fa-solid fa-table-cells" />
            System Define
          </button>
          <button
            type="button"
            disabled={userDefineLocked}
            className={subTabClass(activeTab === 'user', userDefineLocked)}
            onClick={() => !userDefineLocked && onEntityStepUiChange({ activeTab: 'user' })}
            title={userDefineLocked ? 'Complete System Define entity metadata import first.' : undefined}
          >
            <i className={`fa-solid ${userDefineLocked ? 'fa-lock' : 'fa-user-pen'}`} />
            User Define
          </button>
        </div>
      </div>

      {activeTab === 'system' && (
        <div className="flex flex-col gap-3 h-1 flex-auto overflow-hidden">
          <div className={`flex-none rounded border p-3 ${theme.inputBox}`}>
            <div className={`text-xs font-semibold mb-3 ${theme.label}`}>
              <i className="fa-solid fa-route mr-1.5" />
              System Define workflow
              {isWorkflowComplete && (
                <span className={`ml-2 font-normal ${theme.menu_secondary}`}>
                  <i className="fa-solid fa-circle-check mr-1" />
                  All steps complete — User Define tab unlocked
                </span>
              )}
            </div>

            <div className="flex flex-wrap items-center gap-x-1 gap-y-2">
              {WORKFLOW_STEPS.map((def, index) => {
                const completed = stepCompleted(def.step);
                const unlocked = stepUnlocked(def.step);
                const isCurrent = activeStepNumber === def.step && !isWorkflowComplete;
                const isViewing = viewingWorkflowStep === def.step;

                return (
                  <React.Fragment key={def.step}>
                    {index > 0 && (
                      <span className={`text-[10px] px-0.5 opacity-40 select-none ${theme.menu_secondary}`} aria-hidden>
                        ›
                      </span>
                    )}
                    <button
                      type="button"
                      className={workflowStepClass(isViewing, isCurrent, completed, unlocked)}
                      disabled={!unlocked}
                      onClick={() => handleWorkflowStepClick(def.step)}
                      title={!unlocked ? 'Complete table import first.' : def.title}
                    >
                      <span className={`inline-flex items-center justify-center w-4 h-4 rounded-full text-[9px] font-bold shrink-0 ${
                        completed ? theme.button_default : `border ${theme.label}`
                      }`}>
                        {completed ? <i className="fa-solid fa-check text-[8px]" /> : def.step}
                      </span>
                      <span>{def.title}</span>
                      {isCurrent && !isWorkflowComplete && (
                        <span className={`text-[9px] opacity-80 ${theme.menu_secondary}`}>now</span>
                      )}
                    </button>
                  </React.Fragment>
                );
              })}
            </div>

            <p className={`mt-3 text-xs ${theme.menu_secondary}`}>{viewingStepDef.hint}</p>

            {isWorkflowComplete && viewingWorkflowStep === 2 && (
              <div className={`mt-3 text-xs ${theme.menu_secondary}`}>
                <i className="fa-solid fa-circle-check mr-1" />
                System Define import finished. Switch to the User Define tab to continue.
              </div>
            )}

            {showJobProgress && (
              <div className={`mt-2 flex items-center gap-2 text-xs px-2 py-1.5 rounded border ${theme.inputBox} ${theme.menu_secondary}`}>
                <i className="fa-solid fa-spinner fa-spin" />
                {jobProgress}
              </div>
            )}
          </div>

          <div className={`h-1 flex-auto flex flex-col overflow-hidden min-h-[200px] rounded border ${theme.inputBox}`}>
            <div className={`flex-none flex flex-wrap items-center justify-between gap-2 px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className="flex flex-wrap items-center gap-3 min-w-0">
                <span className={`text-xs font-semibold shrink-0 ${theme.label}`}>{resultPanelTitle}</span>
                <button
                  type="button"
                  className={`inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
                  onClick={handleListClick}
                  disabled={listButtonDisabled}
                >
                  <i className="fa-solid fa-list" />
                  {listButtonLabel}
                </button>
                <button
                  type="button"
                  className={`inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={handleExecuteClick}
                  disabled={executeButtonDisabled}
                  title={executeButtonTitle}
                >
                  <i className={`fa-solid ${showTableGrid ? 'fa-file-import' : 'fa-database'}`} />
                  {executeButtonLabel}
                </button>
              </div>
              {resultSummary && (
                <span className={`text-xs truncate ${theme.menu_secondary}`}>{resultSummary}</span>
              )}
            </div>
            <div className="h-1 flex-auto overflow-hidden relative w-full">
              {resultEmpty && !isResultLoading && (
                <div className={`absolute inset-0 z-10 flex flex-col items-center justify-center gap-2 pointer-events-none ${theme.mainContentSection} bg-opacity-80`}>
                  <i className={`fa-solid ${showTableGrid ? 'fa-table' : 'fa-sitemap'} text-2xl opacity-30 ${theme.menu_secondary}`} />
                  <p className={`text-xs text-center max-w-md px-4 ${theme.menu_secondary}`}>
                    {showTableGrid
                      ? `No tables listed yet. Click "${WORKFLOW_STEPS[0].listLabel}" above.`
                      : `No entities listed yet. Click "${WORKFLOW_STEPS[1].listLabel}" above.`}
                  </p>
                </div>
              )}
              {showTableGrid ? (
                <FlexGrid itemsSource={tableCv} headersVisibility="Column" isReadOnly className="h-full w-full">
                  <FlexGridColumn header="Schema" binding="SchemaOwner" width={80} />
                  <FlexGridColumn header="Source table" binding="TableName" width="*" />
                  <FlexGridColumn header="Target table" binding="TargetTableName" width="*" />
                  <FlexGridColumn header="Entities" binding="PlmEntityCount" width={80} />
                  <FlexGridColumn header="Source OK" binding="SourceTableExists" width={80} />
                  <FlexGridColumn header="" binding="" width="*" />
                </FlexGrid>
              ) : (
                <FlexGrid itemsSource={entityCv} headersVisibility="Column" isReadOnly className="h-full w-full">
                  <FlexGridColumn header="Entity code" binding="TargetEntityCode" width="*" />
                  <FlexGridColumn header="Table" binding="TableName" width={120} />
                  <FlexGridColumn header="DS" binding="PlmDataSourceFrom" width={40} />
                  <FlexGridColumn header="Action" binding="ImportAction" width={70} />
                  <FlexGridColumn header="Status" binding="ImportStatus" width={70} />
                  <FlexGridColumn header="Skip reason" binding="SkipReason" width="*" />
                  <FlexGridColumn header="" binding="" width="*" />
                </FlexGrid>
              )}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'user' && (
        <div className="flex flex-col gap-3 h-1 flex-auto overflow-hidden">
          <p className={`text-xs ${theme.menu_secondary}`}>
            Import User Define entities (SimpleValueList and wide entity tables). List first, then execute — re-import updates by IntegrationId.
          </p>

          {state.userDefineEntitiesComplete && (
            <div className={`text-xs ${theme.menu_secondary}`}>
              <i className="fa-solid fa-circle-check mr-1" />
              User Define import completed for this session.
            </div>
          )}

          {Boolean(jobProgress) && isUserDefineJobRunning && (
            <div className={`flex items-center gap-2 text-xs px-2 py-1.5 rounded border ${theme.inputBox} ${theme.menu_secondary}`}>
              <i className="fa-solid fa-spinner fa-spin" />
              {jobProgress}
            </div>
          )}

          <div className={`h-1 flex-auto flex flex-col overflow-hidden min-h-[200px] rounded border ${theme.inputBox}`}>
            <div className={`flex-none flex flex-wrap items-center justify-between gap-2 px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className="flex flex-wrap items-center gap-3 min-w-0">
                <span className={`text-xs font-semibold shrink-0 ${theme.label}`}>
                  Need To Import PLM User Defined Entities
                </span>
                <button
                  type="button"
                  className={`inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
                  onClick={handleUserDefinePreview}
                  disabled={isUserDefinePreviewLoading || isAnyJobRunning}
                >
                  <i className="fa-solid fa-list" />
                  {isUserDefinePreviewLoading ? 'Loading…' : 'List PLM Importing Entities'}
                </button>
                <button
                  type="button"
                  className={`inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                  onClick={handleUserDefineImport}
                  disabled={isAnyJobRunning || isUserDefineJobRunning || importableUserDefineCount === 0}
                  title={importableUserDefineCount === 0
                    ? 'Run List PLM Importing Entities first and ensure at least one Ready row.'
                    : undefined}
                >
                  <i className="fa-solid fa-database" />
                  {isUserDefineJobRunning ? 'Importing…' : 'Execute Import Entities'}
                </button>
              </div>
              {userDefinePlanItems.length > 0 && (
                <span className={`text-xs truncate ${theme.menu_secondary}`}>
                  {userDefinePlanItems.length} entity(ies) · {importableUserDefineCount} ready
                </span>
              )}
            </div>
            <div className="h-1 flex-auto overflow-hidden relative w-full">
              {userDefinePlanItems.length === 0 && !isUserDefinePreviewLoading && (
                <div className={`absolute inset-0 z-10 flex flex-col items-center justify-center gap-2 pointer-events-none ${theme.mainContentSection} bg-opacity-80`}>
                  <i className={`fa-solid fa-user-pen text-2xl opacity-30 ${theme.menu_secondary}`} />
                  <p className={`text-xs text-center max-w-md px-4 ${theme.menu_secondary}`}>
                    No entities listed yet. Click &quot;List PLM Importing Entities&quot; above.
                  </p>
                </div>
              )}
              <FlexGrid itemsSource={userDefineCv} headersVisibility="Column" isReadOnly className="h-full w-full">
                <FlexGridColumn header="Entity code" binding="TargetEntityCode" width="*" />
                <FlexGridColumn header="Type" binding="AppTargetType" width={120} />
                <FlexGridColumn header="Cols" binding="ColumnCount" width={50} />
                <FlexGridColumn header="Rows" binding="PlmRowCount" width={50} />
                <FlexGridColumn header="Order" binding="ImportOrder" width={50} visible={false} />
                <FlexGridColumn header="Action" binding="ImportAction" width={70} />
                <FlexGridColumn header="Status" binding="ImportStatus" width={70} />
                <FlexGridColumn header="Skip reason" binding="SkipReason" width="*" />
                <FlexGridColumn header="" binding="" width="*" />
              </FlexGrid>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default EntityStep;
