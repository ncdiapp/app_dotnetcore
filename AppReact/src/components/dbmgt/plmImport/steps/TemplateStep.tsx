import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmImportJobDto,
  PlmTemplatePreviewItemDto,
} from '../../../../webapi/plmMigrationSvc';
import type { PlmImportTemplateStepUiState, PlmImportWizardState } from '../types';
import { buildPlmImportStepStateJson } from '../types';

export type TemplateStepProps = {
  state: PlmImportWizardState;
  templateStepUi: PlmImportTemplateStepUiState;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onTemplateStepUiChange: (patch: Partial<PlmImportTemplateStepUiState>) => void;
  onSessionSaved: () => void;
};

const TEMPLATE_IMPORT_JOB = 'TemplateImport';
const TERMINAL_JOB_STATUSES = ['Completed', 'Failed', 'Cancelled'];

const TemplateStep: React.FC<TemplateStepProps> = ({
  state,
  templateStepUi,
  onStateChange,
  onTemplateStepUiChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const { previewItems, previewSummary, activeJob, isImporting } = templateStepUi;
  const [previewCv] = useState<CollectionView>(() => new CollectionView<PlmTemplatePreviewItemDto>([]));
  const [isPipelineRunning, setIsPipelineRunning] = useState(false);
  const [pipelineMessage, setPipelineMessage] = useState('');
  const [pipelinePercent, setPipelinePercent] = useState(0);

  const sessionId = state.session?.SessionId ?? null;
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    previewCv.sourceCollection = previewItems;
    previewCv.refresh();
  }, [previewCv, previewItems]);

  useEffect(() => {
    if (isImporting && !activeJob?.JobId) {
      onTemplateStepUiChange({ isImporting: false });
    }
  }, [activeJob?.JobId, isImporting, onTemplateStepUiChange]);

  const pollJob = useCallback(async (jobId: number) => {
    const result = await plmMigrationSvc.getImportJob(jobId);
    if (result.Object) onTemplateStepUiChange({ activeJob: result.Object });
    return result.Object;
  }, [onTemplateStepUiChange]);

  const saveStepState = useCallback(async (patch: Partial<PlmImportWizardState>) => {
    if (!sessionId) return;
    const merged = { ...state, ...patch };
    await plmMigrationSvc.saveImportSession({
      SessionId: sessionId,
      SessionGuid: state.session?.SessionGuid ?? null,
      CompanyId: state.targetCompanyId,
      SaasApplicationId: state.saasApplicationId,
      CurrentStepCode: 'Template',
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

  const finalizeImportJob = useCallback(async (job: PlmImportJobDto) => {
    if (finalizedJobIdsRef.current.has(job.JobId)) return;
    finalizedJobIdsRef.current.add(job.JobId);

    if (job.Status === 'Completed') {
      onStateChange({ templatesComplete: true });
      if (!state.templatesComplete) {
        await saveStepState({ templatesComplete: true });
      }
      showInfo('Template structure import completed.', true);
    } else if (job.Status === 'Failed') {
      showError(job.ErrorMessage || job.ProgressMessage || 'Template import failed.');
    } else if (job.Status === 'Cancelled') {
      showWarning('Template import was cancelled.');
    }

    onTemplateStepUiChange({ isImporting: false, activeJob: null });
    setIsPipelineRunning(false);
    setPipelinePercent(0);
    setPipelineMessage('');
  }, [
    onStateChange,
    onTemplateStepUiChange,
    saveStepState,
    showError,
    showInfo,
    showWarning,
    state.templatesComplete,
  ]);

  useEffect(() => {
    if (!activeJob?.JobId || !TERMINAL_JOB_STATUSES.includes(activeJob.Status ?? '')) return;
    finalizeImportJob(activeJob);
  }, [activeJob, finalizeImportJob]);

  useEffect(() => {
    if (!activeJob?.JobId || TERMINAL_JOB_STATUSES.includes(activeJob.Status ?? '')) return undefined;

    const timer = window.setInterval(async () => {
      try {
        const job = await pollJob(activeJob.JobId);
        if (job) {
          setPipelinePercent(job.ProgressPercent ?? 0);
          setPipelineMessage(job.ProgressMessage ?? '');
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to poll import job.');
      }
    }, 2000);

    return () => window.clearInterval(timer);
  }, [activeJob?.JobId, activeJob?.Status, pollJob, showError]);

  const runPreview = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return null;
    }
    const result = await plmMigrationSvc.previewTemplateMapping(sessionId);
    if (!result.Object?.IsSuccess) {
      const msg = result.Object?.ErrorMessage
        || result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
        || 'Template preview failed.';
      showError(msg);
      return null;
    }
    const preview = result.Object;
    const items = preview.Tabs ?? [];
    const summary = `${preview.TemplateCount} template(s) · ${preview.ReadyCount} ready tab(s) · ${preview.SkippedCount} skipped · ${preview.WarningCount} warning(s)`;
    onTemplateStepUiChange({ previewItems: items, previewSummary: summary });

    if (preview.BlockerCount && preview.BlockerCount > 0) {
      showWarning(`${preview.BlockerCount} blocker(s) found. Resolve before import.`);
    } else if (preview.WarningCount && preview.WarningCount > 0) {
      showWarning(`${preview.WarningCount} warning(s) — see import log after preview.`);
    }
    return preview;
  }, [onTemplateStepUiChange, sessionId, showError, showWarning]);

  const runImport = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return;
    }
    onTemplateStepUiChange({ isImporting: true });
    const result = await plmMigrationSvc.executeTemplateImport(sessionId);
    if (!result.Object?.JobId) {
      onTemplateStepUiChange({ isImporting: false });
      const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
        || 'Failed to start template import.';
      showError(msg);
      return;
    }
    onTemplateStepUiChange({ activeJob: result.Object });
    setPipelinePercent(result.Object.ProgressPercent ?? 0);
    setPipelineMessage(result.Object.ProgressMessage ?? 'Template import started…');
  }, [onTemplateStepUiChange, sessionId, showError]);

  const handleImportTemplates = useCallback(async () => {
    if (!sessionId || isPipelineRunning) return;
    setIsPipelineRunning(true);
    setPipelineMessage('Listing PLM templates…');
    setPipelinePercent(2);
    try {
      const preview = await runPreview();
      if (!preview || (preview.BlockerCount ?? 0) > 0) {
        setIsPipelineRunning(false);
        setPipelineMessage('');
        setPipelinePercent(0);
        return;
      }
      if ((preview.ReadyCount ?? 0) === 0) {
        showWarning('No ready tabs to import.');
        setIsPipelineRunning(false);
        setPipelineMessage('');
        setPipelinePercent(0);
        return;
      }
      setPipelineMessage('Starting template structure import…');
      setPipelinePercent(5);
      await runImport();
    } catch (e: any) {
      showError(e?.message || 'Template import failed.');
      setIsPipelineRunning(false);
      onTemplateStepUiChange({ isImporting: false });
      setPipelineMessage('');
      setPipelinePercent(0);
    }
  }, [isPipelineRunning, onTemplateStepUiChange, runImport, runPreview, sessionId, showError, showWarning]);

  const busy = isPipelineRunning || isImporting;

  return (
    <div className="flex flex-col h-full overflow-hidden p-4 gap-3">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 3 — Template Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Map PLM template layout to Data Model Template (AppSearch), tab transactions, units, and physical tables.
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <button
          type="button"
          className={`inline-flex items-center gap-2 px-4 py-2 text-sm font-semibold rounded-[4px] ${theme.button_secondary} disabled:opacity-40`}
          onClick={handleImportTemplates}
          disabled={busy || !sessionId}
        >
          <i className={`fa-solid ${busy ? 'fa-spinner fa-spin' : 'fa-file-import'}`} />
          Import Templates
        </button>
        {previewSummary && (
          <span className={`text-xs ${theme.menu_secondary}`}>{previewSummary}</span>
        )}
        {state.templatesComplete && (
          <span className={`text-xs font-semibold ${theme.label}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            Templates imported
          </span>
        )}
      </div>

      {(busy || pipelineMessage) && (
        <div className={`rounded border px-3 py-2 text-xs ${theme.inputBox}`}>
          <div className={`mb-1 ${theme.menu_secondary}`}>{pipelineMessage || 'Working…'}</div>
          <div className={`h-2 rounded overflow-hidden ${theme.mainContentSection}`}>
            <div
              className={`h-full transition-all duration-300 ${theme.button_secondary}`}
              style={{ width: `${Math.min(100, Math.max(0, pipelinePercent))}%` }}
            />
          </div>
        </div>
      )}

      <div className={`h-1 flex-auto min-h-[200px] rounded border overflow-hidden ${theme.inputBox}`}>
        <FlexGrid
          itemsSource={previewCv}
          headersVisibility="Column"
          selectionMode="Row"
          isReadOnly
          className="h-full"
        >
          <FlexGridColumn header="Template" binding="PlmTemplateName" width={120} />
          <FlexGridColumn header="Tab" binding="PlmTabName" width={120} />
          <FlexGridColumn header="Type" binding="TabType" width={90} />
          <FlexGridColumn header="Status" binding="ImportStatus" width={70} />
          <FlexGridColumn header="Action" binding="ImportAction" width={70} />
          <FlexGridColumn header="Sibling Table" binding="SiblingTableName" width={140} />
          <FlexGridColumn header="Child Tables" binding="ChildTableNames" width="*" />
          <FlexGridColumn header="Fields" binding="SiblingFieldCount" width={55} />
          <FlexGridColumn header="Grid Cols" binding="GridFieldCount" width={65} />
          <FlexGridColumn header="Warn" binding="WarningCount" width={50} />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
    </div>
  );
};

export default TemplateStep;
