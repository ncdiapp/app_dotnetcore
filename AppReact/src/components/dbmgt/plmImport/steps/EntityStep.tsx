import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { plmMigrationSvc, PlmImportJobDto, PlmTableExportPlanItemDto } from '../../../../webapi/plmMigrationSvc';
import type { PlmImportEntityStepUiState, PlmImportWizardState } from '../types';

export type EntityStepProps = {
  state: PlmImportWizardState;
  entityStepUi: PlmImportEntityStepUiState;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onEntityStepUiChange: (patch: Partial<PlmImportEntityStepUiState>) => void;
  onSessionSaved: () => void;
};

const EntityStep: React.FC<EntityStepProps> = ({
  state,
  entityStepUi,
  onStateChange,
  onEntityStepUiChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const { activeTab, planItems, activeJob, isExporting } = entityStepUi;
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [cv] = useState<CollectionView>(() => new CollectionView<PlmTableExportPlanItemDto>([]));

  const sessionId = state.session?.SessionId ?? null;
  const userDefineLocked = !state.systemDefineComplete;
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    cv.sourceCollection = planItems;
    cv.refresh();
  }, [cv, planItems]);

  const pollJob = useCallback(async (jobId: number) => {
    const result = await plmMigrationSvc.getImportJob(jobId);
    if (result.Object) onEntityStepUiChange({ activeJob: result.Object });
    return result.Object;
  }, [onEntityStepUiChange]);

  const saveSystemDefineComplete = useCallback(async () => {
    if (!sessionId) return;
    await plmMigrationSvc.saveImportSession({
      SessionId: sessionId,
      SessionGuid: state.session?.SessionGuid ?? null,
      CompanyId: state.targetCompanyId,
      SaasApplicationId: state.saasApplicationId,
      CurrentStepCode: 'Entity',
      PlmConnectionString: state.plmConnectionString || undefined,
      StepStateJson: JSON.stringify({
        connectionTested: state.connectionTested,
        systemDefineComplete: true,
      }),
    });
    onSessionSaved();
  }, [
    onSessionSaved,
    sessionId,
    state.connectionTested,
    state.plmConnectionString,
    state.saasApplicationId,
    state.session?.SessionGuid,
    state.targetCompanyId,
  ]);

  const finalizeImportJob = useCallback(async (job: PlmImportJobDto, showCompletionToast: boolean) => {
    if (finalizedJobIdsRef.current.has(job.JobId)) {
      onEntityStepUiChange({ isExporting: false, activeJob: null });
      return;
    }
    finalizedJobIdsRef.current.add(job.JobId);

    if (job.Status === 'Completed') {
      if (showCompletionToast && !state.systemDefineComplete) {
        showInfo('PLM table import completed.', true);
      }
      onStateChange({ systemDefineComplete: true });
      if (!state.systemDefineComplete) {
        await saveSystemDefineComplete();
      }
      onEntityStepUiChange({ isExporting: false, activeJob: null });
      return;
    }
    if (job.Status === 'Failed' || job.Status === 'Cancelled') {
      showError(job.ErrorMessage || job.ProgressMessage || 'Import failed.');
      onEntityStepUiChange({ isExporting: false, activeJob: null });
    }
  }, [
    onEntityStepUiChange,
    onStateChange,
    saveSystemDefineComplete,
    showError,
    showInfo,
    state.systemDefineComplete,
  ]);

  const terminalJobStatuses = useMemo(() => ['Completed', 'Failed', 'Cancelled'], []);

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
  }, [
    activeJob,
    finalizeImportJob,
    pollJob,
    terminalJobStatuses,
  ]);

  const importableTableCount = useMemo(
    () => planItems.filter((t) => t.SourceTableExists).length,
    [planItems],
  );

  const handlePreview = useCallback(async () => {
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

  const handleImport = useCallback(async () => {
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

  const isImportJobRunning = useMemo(() => {
    if (isExporting) return true;
    if (!activeJob?.JobId) return false;
    return !terminalJobStatuses.includes(activeJob.Status || '');
  }, [activeJob, isExporting, terminalJobStatuses]);

  const importProgress = useMemo(() => {
    if (!isImportJobRunning || !activeJob) return null;
    return `${activeJob.ProgressPercent ?? 0}% — ${activeJob.ProgressMessage || activeJob.Status || ''}`;
  }, [activeJob, isImportJobRunning]);

  const systemStatusText = useMemo(() => {
    if (state.systemDefineComplete) return 'Table import completed';
    if (isImportJobRunning) return 'Table import in progress…';
    if (importableTableCount > 0) return `${importableTableCount} table(s) ready to import`;
    if (planItems.length > 0) return 'No importable tables (all missing in PLM source)';
    return 'Not started — run step 1 below';
  }, [importableTableCount, isImportJobRunning, planItems.length, state.systemDefineComplete]);

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

  return (
    <div className="flex flex-col gap-3 p-4 h-full overflow-hidden">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 2 — Entity Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Import System Define PLM tables into the tenant database first, then import entity metadata.
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
            title={userDefineLocked ? 'Complete System Define import first.' : undefined}
          >
            <i className={`fa-solid ${userDefineLocked ? 'fa-lock' : 'fa-user-pen'}`} />
            User Define
          </button>
        </div>
      </div>

      {activeTab === 'system' && (
        <div className="flex flex-col gap-3 h-1 flex-auto overflow-hidden">
          <div className={`flex-none rounded border p-3 ${theme.inputBox}`}>
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>
              <i className="fa-solid fa-list-ol mr-1.5" />
              System Define — table import workflow
            </div>
            <ol className={`text-xs space-y-1 mb-3 pl-4 list-decimal ${theme.menu_secondary}`}>
              <li>Click <span className={theme.label}>1. Preview import plan</span> to load PLM tables in the grid.</li>
              <li>Review <span className={theme.label}>Source OK</span> — only checked rows can be imported.</li>
              <li>Click <span className={theme.label}>2. Import PLM Tables</span> to copy them into the tenant database.</li>
            </ol>
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className={`inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-[4px] border min-w-[200px] justify-center ${theme.button_secondary}`}
                onClick={handlePreview}
                disabled={isPreviewLoading || isImportJobRunning}
              >
                <span className={`inline-flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold border ${theme.label}`}>1</span>
                <i className="fa-solid fa-magnifying-glass" />
                {isPreviewLoading ? 'Loading…' : 'Preview import plan'}
              </button>
              <i className={`fa-solid fa-arrow-right text-xs px-1 ${theme.menu_secondary}`} aria-hidden />
              <button
                type="button"
                className={`inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-[4px] border min-w-[200px] justify-center ${theme.button_default}`}
                onClick={handleImport}
                disabled={isImportJobRunning || importableTableCount === 0}
                title={importableTableCount === 0 ? 'Run step 1 first and ensure at least one Source OK row.' : undefined}
              >
                <span className={`inline-flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold border ${theme.label}`}>2</span>
                <i className="fa-solid fa-file-import" />
                {isImportJobRunning ? 'Importing…' : 'Import PLM Tables'}
              </button>
            </div>
          </div>

          {importProgress && (
            <div className={`flex-none flex items-center gap-2 text-xs px-2 py-1.5 rounded border ${theme.inputBox} ${theme.menu_secondary}`}>
              <i className="fa-solid fa-spinner fa-spin" />
              {importProgress}
            </div>
          )}

          <div className={`h-1 flex-auto overflow-hidden min-h-[160px] rounded border ${theme.inputBox} relative`}>
            {planItems.length === 0 && !isPreviewLoading && (
              <div className={`absolute inset-0 z-10 flex flex-col items-center justify-center gap-2 pointer-events-none ${theme.mainContentSection} bg-opacity-80`}>
                <i className={`fa-solid fa-table text-2xl opacity-30 ${theme.menu_secondary}`} />
                <p className={`text-xs text-center max-w-sm ${theme.menu_secondary}`}>
                  No tables loaded yet. Click <span className={theme.label}>1. Preview import plan</span> above.
                </p>
              </div>
            )}
            <FlexGrid
              itemsSource={cv}
              headersVisibility="Column"
              isReadOnly
              className="h-full"
            >
              <FlexGridColumn header="Schema" binding="SchemaOwner" width={80} />
              <FlexGridColumn header="Table" binding="TableName" width="*" />
              <FlexGridColumn header="Entities" binding="PlmEntityCount" width={80} />
              <FlexGridColumn header="Source OK" binding="SourceTableExists" width={80} />
              <FlexGridColumn header="" binding="" width="*" />
            </FlexGrid>
          </div>

          <div className={`flex-none flex items-center gap-2 text-xs px-2 py-1.5 rounded border ${theme.inputBox}`}>
            <i className={`fa-solid ${state.systemDefineComplete ? 'fa-circle-check' : 'fa-circle-info'} ${theme.menu_secondary}`} />
            <span className={theme.label}>Status:</span>
            <span className={theme.menu_secondary}>{systemStatusText}</span>
          </div>
        </div>
      )}

      {activeTab === 'user' && (
        <div className={`flex-auto rounded border p-4 text-xs ${theme.inputBox}`}>
          <div className={`font-semibold mb-2 ${theme.label}`}>
            <i className="fa-solid fa-user-pen mr-1.5" />
            User Define entities
          </div>
          <p className={theme.menu_secondary}>
            User Define entity preview and import will be available in Phase 4.
            Complete System Define table import on the previous tab first.
          </p>
        </div>
      )}
    </div>
  );
};

export default EntityStep;
