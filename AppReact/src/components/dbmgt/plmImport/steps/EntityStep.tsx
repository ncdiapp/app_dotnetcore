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
import type { PlmImportEntityStepUiState, PlmImportWizardState } from '../types';
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
const TERMINAL_JOB_STATUSES = ['Completed', 'Failed', 'Cancelled'];

type PipelinePhase =
  | 'idle'
  | 'listTables'
  | 'importTables'
  | 'listSysEntities'
  | 'importSysEntities'
  | 'listUserEntities'
  | 'importUserEntities'
  | 'done';

const PHASE_LABEL: Record<Exclude<PipelinePhase, 'idle' | 'done'>, string> = {
  listTables: 'Listing PLM system tables…',
  importTables: 'Importing PLM tables into tenant database…',
  listSysEntities: 'Listing system define entities…',
  importSysEntities: 'Importing system define entity metadata…',
  listUserEntities: 'Listing user define entities…',
  importUserEntities: 'Importing user define entities…',
};

const phaseBasePercent: Record<PipelinePhase, number> = {
  idle: 0,
  listTables: 3,
  importTables: 8,
  listSysEntities: 38,
  importSysEntities: 43,
  listUserEntities: 72,
  importUserEntities: 77,
  done: 100,
};

type CollapsibleSectionProps = {
  title: string;
  summary: string | null;
  expanded: boolean;
  onToggle: () => void;
  children: React.ReactNode;
  theme: ReturnType<typeof useTheme>['theme'];
};

const CollapsibleSection: React.FC<CollapsibleSectionProps> = ({
  title,
  summary,
  expanded,
  onToggle,
  children,
  theme,
}) => (
  <div className={`rounded border overflow-hidden ${theme.inputBox}`}>
    <button
      type="button"
      className={`w-full flex items-center gap-2 px-3 py-2 text-left border-b ${theme.mainContentSection}`}
      onClick={onToggle}
    >
      <i className={`fa-solid fa-chevron-${expanded ? 'down' : 'right'} text-[10px] w-3 ${theme.menu_secondary}`} />
      <span className={`text-xs font-semibold ${theme.label}`}>{title}</span>
      {summary && (
        <span className={`ml-auto text-xs truncate ${theme.menu_secondary}`}>{summary}</span>
      )}
    </button>
    {expanded && <div className="flex flex-col min-h-[140px] h-64 overflow-hidden">{children}</div>}
  </div>
);

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
    systemSectionExpanded,
    userSectionExpanded,
    planItems,
    entityPlanItems,
    userDefinePlanItems,
    activeJob,
    isExporting,
    isEntityImporting,
    isUserDefineImporting,
  } = entityStepUi;

  const [tableCv] = useState<CollectionView>(() => new CollectionView<PlmTableExportPlanItemDto>([]));
  const [entityCv] = useState<CollectionView>(() => new CollectionView<PlmSystemDefineEntityPreviewItemDto>([]));
  const [userDefineCv] = useState<CollectionView>(() => new CollectionView<PlmUserDefineEntityPreviewItemDto>([]));

  const [isPipelineRunning, setIsPipelineRunning] = useState(false);
  const [pipelinePhase, setPipelinePhase] = useState<PipelinePhase>('idle');
  const [pipelineMessage, setPipelineMessage] = useState('');
  const [pipelinePercent, setPipelinePercent] = useState(0);

  const sessionId = state.session?.SessionId ?? null;
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());
  const pipelineRunningRef = useRef(false);

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
        templatesComplete: merged.templatesComplete,
        tablePrefix: merged.tablePrefix,
      }),
      DataSourceDiscoveryJson: state.session?.DataSourceDiscoveryJson ?? undefined,
    });
    onSessionSaved();
  }, [onSessionSaved, sessionId, state]);

  const applyJobSuccess = useCallback(async (job: PlmImportJobDto) => {
    if (job.JobType === TABLE_EXPORT_JOB) {
      onStateChange({ systemDefineTablesComplete: true });
      if (!state.systemDefineTablesComplete) {
        await saveStepState({ systemDefineTablesComplete: true });
      }
    } else if (job.JobType === ENTITY_IMPORT_JOB) {
      onStateChange({ systemDefineEntitiesComplete: true });
      if (!state.systemDefineEntitiesComplete) {
        await saveStepState({ systemDefineEntitiesComplete: true });
      }
    } else if (job.JobType === USER_DEFINE_IMPORT_JOB) {
      onStateChange({ userDefineEntitiesComplete: true });
      if (!state.userDefineEntitiesComplete) {
        await saveStepState({ userDefineEntitiesComplete: true });
      }
    }
    onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
  }, [
    onEntityStepUiChange,
    onStateChange,
    saveStepState,
    state.systemDefineEntitiesComplete,
    state.systemDefineTablesComplete,
    state.userDefineEntitiesComplete,
  ]);

  const finalizeImportJob = useCallback(async (job: PlmImportJobDto, showCompletionToast: boolean) => {
    if (finalizedJobIdsRef.current.has(job.JobId)) {
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
      return job.Status === 'Completed';
    }
    finalizedJobIdsRef.current.add(job.JobId);

    if (job.Status === 'Completed') {
      await applyJobSuccess(job);
      if (showCompletionToast && !pipelineRunningRef.current) {
        if (job.JobType === TABLE_EXPORT_JOB) showInfo('PLM table import completed.', true);
        else if (job.JobType === ENTITY_IMPORT_JOB) showInfo('System Define entity metadata import completed.', true);
        else if (job.JobType === USER_DEFINE_IMPORT_JOB) showInfo('User Define entity import completed.', true);
      }
      return true;
    }

    if (job.Status === 'Failed' || job.Status === 'Cancelled') {
      if (!pipelineRunningRef.current) {
        showError(job.ErrorMessage || job.ProgressMessage || 'Import failed.');
      }
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false, activeJob: null });
      return false;
    }
    return false;
  }, [applyJobSuccess, onEntityStepUiChange, showError, showInfo]);

  const waitForJob = useCallback(async (jobId: number, phase: PipelinePhase): Promise<PlmImportJobDto> => {
    for (;;) {
      const job = await pollJob(jobId);
      if (!job) throw new Error('Import job not found.');
      if (pipelineRunningRef.current) {
        setPipelineMessage(job.ProgressMessage || PHASE_LABEL[phase as keyof typeof PHASE_LABEL] || '');
        const jp = job.ProgressPercent ?? 0;
        if (phase === 'importTables') setPipelinePercent(8 + Math.round(jp * 0.28));
        else if (phase === 'importSysEntities') setPipelinePercent(43 + Math.round(jp * 0.26));
        else if (phase === 'importUserEntities') setPipelinePercent(77 + Math.round(jp * 0.22));
      }
      if (TERMINAL_JOB_STATUSES.includes(job.Status || '')) {
        const ok = await finalizeImportJob(job, false);
        if (!ok) throw new Error(job.ErrorMessage || job.ProgressMessage || 'Import failed.');
        return job;
      }
      await new Promise((r) => window.setTimeout(r, 2000));
    }
  }, [finalizeImportJob, pollJob]);

  useEffect(() => {
    if (!activeJob?.JobId || pipelineRunningRef.current) return undefined;
    if (TERMINAL_JOB_STATUSES.includes(activeJob.Status || '')) {
      finalizeImportJob(activeJob, true);
      return undefined;
    }

    const timer = window.setInterval(async () => {
      try {
        const job = await pollJob(activeJob.JobId);
        if (!job) return;
        if (TERMINAL_JOB_STATUSES.includes(job.Status || '')) {
          await finalizeImportJob(job, true);
        }
      } catch {
        // keep polling
      }
    }, 2000);

    return () => window.clearInterval(timer);
  }, [activeJob, finalizeImportJob, pollJob]);

  const loadTablePreview = useCallback(async (): Promise<{ tables: PlmTableExportPlanItemDto[]; importable: number }> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    const result = await plmMigrationSvc.previewPlmTableExportPlan(sessionId);
    const tables = result.Object?.Tables ?? [];
    if (!result.Object?.IsSuccess || tables.length === 0) {
      throw new Error(
        result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Table preview failed.',
      );
    }
    onEntityStepUiChange({ planItems: tables });
    const importable = tables.filter((t) => t.SourceTableExists).length;
    const missingCount = result.Object.MissingSourceTableCount
      ?? tables.filter((t) => !t.SourceTableExists).length;
    if (missingCount > 0) {
      const detail = result.Object.ErrorMessage
        || result.Object.Issues?.slice(0, 3).map((issue) =>
          `${issue.SchemaOwner}.${issue.TableName}`,
        ).join('; ');
      showWarning(detail || `${missingCount} table(s) missing in PLM source.`);
    }
    return { tables, importable };
  }, [onEntityStepUiChange, sessionId, showWarning]);

  const startTableImport = useCallback(async (): Promise<number> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    onEntityStepUiChange({ isExporting: true, activeJob: null });
    const result = await plmMigrationSvc.executePlmTableExport(sessionId);
    if (!result.Object?.JobId) {
      onEntityStepUiChange({ isExporting: false });
      throw new Error(
        result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start table import.',
      );
    }
    onEntityStepUiChange({ activeJob: result.Object });
    return result.Object.JobId;
  }, [onEntityStepUiChange, sessionId]);

  const loadSysEntityPreview = useCallback(async (): Promise<{ entities: PlmSystemDefineEntityPreviewItemDto[]; ready: number }> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    const result = await plmMigrationSvc.previewSystemDefineEntityImport(sessionId);
    const entities = result.Object?.Entities ?? [];
    if (!result.Object?.IsSuccess) {
      throw new Error(
        result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'System define entity preview failed.',
      );
    }
    onEntityStepUiChange({ entityPlanItems: entities });
    const ready = result.Object.ReadyCount ?? entities.filter((e) => e.ImportStatus === 'Ready').length;
    const blockers = result.Object.BlockerCount ?? 0;
    if (blockers > 0) {
      showWarning(`${blockers} entity blocker(s). Those rows will be skipped.`);
    }
    return { entities, ready };
  }, [onEntityStepUiChange, sessionId, showWarning]);

  const startSysEntityImport = useCallback(async (): Promise<number> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    onEntityStepUiChange({ isEntityImporting: true, activeJob: null });
    const result = await plmMigrationSvc.executeSystemDefineEntityImport(sessionId);
    if (!result.Object?.JobId) {
      onEntityStepUiChange({ isEntityImporting: false });
      throw new Error(
        result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start system define entity import.',
      );
    }
    onEntityStepUiChange({ activeJob: result.Object });
    return result.Object.JobId;
  }, [onEntityStepUiChange, sessionId]);

  const loadUserDefinePreview = useCallback(async (): Promise<{ entities: PlmUserDefineEntityPreviewItemDto[]; ready: number }> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    const result = await plmMigrationSvc.previewUserDefineEntityImport(sessionId);
    const entities = result.Object?.Entities ?? [];
    if (!result.Object?.IsSuccess) {
      throw new Error(
        result.Object?.ErrorMessage
          || result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'User define entity preview failed.',
      );
    }
    onEntityStepUiChange({ userDefinePlanItems: entities });
    const ready = result.Object.ReadyCount ?? entities.filter((e) => e.ImportStatus === 'Ready').length;
    return { entities, ready };
  }, [onEntityStepUiChange, sessionId]);

  const startUserDefineImport = useCallback(async (): Promise<number> => {
    if (!sessionId) throw new Error('Save a connection session before importing.');
    onEntityStepUiChange({ isUserDefineImporting: true, activeJob: null });
    const result = await plmMigrationSvc.executeUserDefineEntityImport(sessionId);
    if (!result.Object?.JobId) {
      onEntityStepUiChange({ isUserDefineImporting: false });
      throw new Error(
        result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start user define entity import.',
      );
    }
    onEntityStepUiChange({ activeJob: result.Object });
    return result.Object.JobId;
  }, [onEntityStepUiChange, sessionId]);

  const updatePipelineProgress = useCallback((
    phase: PipelinePhase,
    jobPercent?: number,
    message?: string,
  ) => {
    setPipelinePhase(phase);
    let percent = phaseBasePercent[phase];
    if (phase === 'importTables' && jobPercent != null) {
      percent = 8 + Math.round(jobPercent * 0.28);
    } else if (phase === 'importSysEntities' && jobPercent != null) {
      percent = 43 + Math.round(jobPercent * 0.26);
    } else if (phase === 'importUserEntities' && jobPercent != null) {
      percent = 77 + Math.round(jobPercent * 0.22);
    }
    setPipelinePercent(percent);
    if (message) setPipelineMessage(message);
    else if (phase !== 'idle' && phase !== 'done') {
      setPipelineMessage(PHASE_LABEL[phase]);
    }
  }, []);

  const runImportPipeline = useCallback(async () => {
    if (!sessionId) {
      showError('Complete Step 1 Connect & Discover first.');
      return;
    }
    if (isPipelineRunning) return;

    pipelineRunningRef.current = true;
    setIsPipelineRunning(true);
    onEntityStepUiChange({ systemSectionExpanded: true });

    try {
      updatePipelineProgress('listTables');
      const { importable: tableCount } = await loadTablePreview();

      if (tableCount > 0) {
        updatePipelineProgress('importTables');
        const tableJobId = await startTableImport();
        await waitForJob(tableJobId, 'importTables');
      } else {
        showWarning('No importable PLM tables — skipping table copy.');
      }

      updatePipelineProgress('listSysEntities');
      const { ready: sysReady } = await loadSysEntityPreview();

      if (sysReady > 0) {
        updatePipelineProgress('importSysEntities');
        const sysJobId = await startSysEntityImport();
        await waitForJob(sysJobId, 'importSysEntities');
      } else {
        showWarning('No ready system define entities — skipping metadata import.');
        await saveStepState({ systemDefineEntitiesComplete: true });
        onStateChange({ systemDefineEntitiesComplete: true });
      }

      onEntityStepUiChange({ userSectionExpanded: true });
      updatePipelineProgress('listUserEntities');
      const { ready: userReady } = await loadUserDefinePreview();

      if (userReady > 0) {
        updatePipelineProgress('importUserEntities');
        const userJobId = await startUserDefineImport();
        await waitForJob(userJobId, 'importUserEntities');
      } else {
        showWarning('No ready user define entities — skipping user define import.');
        await saveStepState({ userDefineEntitiesComplete: true });
        onStateChange({ userDefineEntitiesComplete: true });
      }

      updatePipelineProgress('done');
      setPipelineMessage('All entity imports completed.');
      setPipelinePercent(100);
      showInfo('Entity import pipeline completed.', true);
    } catch (e: any) {
      setPipelineMessage(e?.message || 'Import failed.');
      showError(e?.message || 'Import failed.');
    } finally {
      pipelineRunningRef.current = false;
      setIsPipelineRunning(false);
      onEntityStepUiChange({ isExporting: false, isEntityImporting: false, isUserDefineImporting: false });
    }
  }, [
    isPipelineRunning,
    loadSysEntityPreview,
    loadTablePreview,
    loadUserDefinePreview,
    onEntityStepUiChange,
    onStateChange,
    saveStepState,
    sessionId,
    showError,
    showInfo,
    showWarning,
    startSysEntityImport,
    startTableImport,
    startUserDefineImport,
    updatePipelineProgress,
    waitForJob,
  ]);

  const isAnyJobRunning = isPipelineRunning || isExporting || isEntityImporting || isUserDefineImporting
    || (activeJob?.JobId != null && !TERMINAL_JOB_STATUSES.includes(activeJob.Status || ''));

  const systemSummary = useMemo(() => {
    const parts: string[] = [];
    if (planItems.length > 0) {
      const ok = planItems.filter((t) => t.SourceTableExists).length;
      parts.push(`${planItems.length} tables · ${ok} importable`);
    }
    if (entityPlanItems.length > 0) {
      const ready = entityPlanItems.filter((e) => e.ImportStatus === 'Ready').length;
      parts.push(`${entityPlanItems.length} entities · ${ready} ready`);
    }
    return parts.length > 0 ? parts.join(' · ') : null;
  }, [entityPlanItems, planItems]);

  const userSummary = useMemo(() => {
    if (userDefinePlanItems.length === 0) return null;
    const ready = userDefinePlanItems.filter((e) => e.ImportStatus === 'Ready').length;
    return `${userDefinePlanItems.length} entities · ${ready} ready`;
  }, [userDefinePlanItems]);

  const liveJobPercent = useMemo(() => {
    if (!isPipelineRunning || !activeJob?.JobId) return pipelinePercent;
    const phase = pipelinePhase;
    const jp = activeJob.ProgressPercent ?? 0;
    if (phase === 'importTables') return 8 + Math.round(jp * 0.28);
    if (phase === 'importSysEntities') return 43 + Math.round(jp * 0.26);
    if (phase === 'importUserEntities') return 77 + Math.round(jp * 0.22);
    return pipelinePercent;
  }, [activeJob, isPipelineRunning, pipelinePercent, pipelinePhase]);

  const progressLabel = useMemo(() => {
    if (!isPipelineRunning && pipelinePhase === 'done') {
      return pipelineMessage || 'Completed';
    }
    if (isPipelineRunning) {
      const msg = pipelineMessage || (pipelinePhase !== 'idle' && pipelinePhase !== 'done'
        ? PHASE_LABEL[pipelinePhase as keyof typeof PHASE_LABEL]
        : 'Working…');
      return `${liveJobPercent}% — ${msg}`;
    }
    if (state.userDefineEntitiesComplete) {
      return 'Entity import completed for this session.';
    }
    return null;
  }, [isPipelineRunning, liveJobPercent, pipelineMessage, pipelinePhase, state.userDefineEntitiesComplete]);

  return (
    <div className="flex flex-col gap-3 p-4 h-full overflow-hidden">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 2 — Entity Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Import PLM system define tables and entity metadata, then user define entities — one click runs all steps in order.
        </p>
      </div>

      <div className="flex-none flex flex-wrap items-center gap-3">
        <button
          type="button"
          className={`inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold rounded-[4px] ${theme.button_default}`}
          onClick={runImportPipeline}
          disabled={isAnyJobRunning || !sessionId}
          title={!sessionId ? 'Complete Step 1 first.' : undefined}
        >
          <i className={`fa-solid ${isAnyJobRunning ? 'fa-spinner fa-spin' : 'fa-file-import'}`} />
          {isAnyJobRunning ? 'Importing…' : 'Import Entities'}
        </button>
        {state.userDefineEntitiesComplete && !isAnyJobRunning && (
          <span className={`text-xs ${theme.menu_secondary}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            Session complete — safe to proceed to next step
          </span>
        )}
      </div>

      {progressLabel && (
        <div className={`flex-none rounded border px-3 py-2 ${theme.inputBox}`}>
          <div className={`flex items-center gap-2 text-xs mb-1.5 ${theme.menu_secondary}`}>
            {isAnyJobRunning && <i className="fa-solid fa-spinner fa-spin" />}
            {!isAnyJobRunning && pipelinePhase === 'done' && (
              <i className="fa-solid fa-circle-check" />
            )}
            <span>{progressLabel}</span>
          </div>
          {(isAnyJobRunning || pipelinePhase === 'done') && (
            <div className={`h-1.5 w-full rounded overflow-hidden ${theme.mainContentSection}`}>
              <div
                className={`h-full transition-all duration-300 ${theme.button_default}`}
                style={{ width: `${Math.min(100, Math.max(0, liveJobPercent))}%` }}
              />
            </div>
          )}
        </div>
      )}

      <div className="h-1 flex-auto flex flex-col gap-2 overflow-auto min-h-0">
        <CollapsibleSection
          title="PLM System Defined Entity"
          summary={systemSummary}
          expanded={systemSectionExpanded}
          onToggle={() => onEntityStepUiChange({ systemSectionExpanded: !systemSectionExpanded })}
          theme={theme}
        >
          {planItems.length > 0 && (
            <div className="h-1 flex-auto min-h-[80px] overflow-hidden border-b">
              <div className={`px-2 py-1 text-[10px] font-semibold ${theme.menu_secondary}`}>Physical tables</div>
              <FlexGrid itemsSource={tableCv} headersVisibility="Column" isReadOnly className="h-full w-full">
                <FlexGridColumn header="Source" binding="TableName" width="*" />
                <FlexGridColumn header="Target" binding="TargetTableName" width="*" />
                <FlexGridColumn header="OK" binding="SourceTableExists" width={50} />
                <FlexGridColumn header="" binding="" width="*" />
              </FlexGrid>
            </div>
          )}
          <div className="h-1 flex-auto min-h-[80px] overflow-hidden relative">
            {entityPlanItems.length === 0 && !isPipelineRunning && (
              <div className={`absolute inset-0 flex items-center justify-center text-xs px-4 text-center ${theme.menu_secondary}`}>
                Run Import Entities to list system define entity metadata.
              </div>
            )}
            <FlexGrid itemsSource={entityCv} headersVisibility="Column" isReadOnly className="h-full w-full">
              <FlexGridColumn header="Entity code" binding="TargetEntityCode" width="*" />
              <FlexGridColumn header="Table" binding="TableName" width={120} />
              <FlexGridColumn header="Status" binding="ImportStatus" width={70} />
              <FlexGridColumn header="Skip reason" binding="SkipReason" width="*" />
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>
        </CollapsibleSection>

        <CollapsibleSection
          title="User Defined"
          summary={userSummary}
          expanded={userSectionExpanded}
          onToggle={() => onEntityStepUiChange({ userSectionExpanded: !userSectionExpanded })}
          theme={theme}
        >
          <div className="h-full overflow-hidden relative">
            {userDefinePlanItems.length === 0 && !isPipelineRunning && (
              <div className={`absolute inset-0 flex items-center justify-center text-xs px-4 text-center ${theme.menu_secondary}`}>
                User define entities appear here during import.
              </div>
            )}
            <FlexGrid itemsSource={userDefineCv} headersVisibility="Column" isReadOnly className="h-full w-full">
              <FlexGridColumn header="Entity code" binding="TargetEntityCode" width="*" />
              <FlexGridColumn header="Type" binding="AppTargetType" width={120} />
              <FlexGridColumn header="Cols" binding="ColumnCount" width={50} />
              <FlexGridColumn header="Rows" binding="PlmRowCount" width={50} />
              <FlexGridColumn header="Status" binding="ImportStatus" width={70} />
              <FlexGridColumn header="Skip reason" binding="SkipReason" width="*" />
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>
        </CollapsibleSection>
      </div>
    </div>
  );
};

export default EntityStep;
