import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmImportJobDto,
  PlmTemplateImportSettingDto,
  PlmTemplateMappingGridRowDto,
} from '../../../../webapi/plmMigrationSvc';
import type { PlmImportTemplateStepUiState, PlmImportWizardState } from '../types';
import { buildPlmImportStepStateJson } from '../types';
import TemplateTabWarningDialog from './TemplateTabWarningDialog';

export type TemplateStepProps = {
  state: PlmImportWizardState;
  templateStepUi: PlmImportTemplateStepUiState;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onTemplateStepUiChange: (patch: Partial<PlmImportTemplateStepUiState>) => void;
  onSessionSaved: () => void;
};

const TERMINAL_JOB_STATUSES = ['Completed', 'Failed', 'Cancelled'];

const buildSettingFromRows = (
  rows: PlmTemplateMappingGridRowDto[],
  base: PlmTemplateImportSettingDto | null,
): PlmTemplateImportSettingDto => ({
  Rows: rows.map((r) => ({
    PlmTemplateId: r.PlmTemplateId,
    PlmTabId: r.PlmTabId,
    TransactionGroupName: r.TransactionGroupName,
    TransactionName: r.TransactionName,
    IntegrationId: r.IntegrationId,
    SiblingTableName: r.SiblingTableName,
    ImportStatus: r.ImportStatus,
  })),
  TabSharedTableGroups: base?.TabSharedTableGroups ? [...base.TabSharedTableGroups] : [],
  BlockStorageOverrides: base?.BlockStorageOverrides ? [...base.BlockStorageOverrides] : [],
});

const TemplateStep: React.FC<TemplateStepProps> = ({
  state,
  templateStepUi,
  onStateChange,
  onTemplateStepUiChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const {
    gridRows,
    gridSummary,
    importSetting,
    blockAnalysis,
    similarTabGroups,
    validationMessage,
    activeJob,
    isAnalyzing,
    isSaving,
    isValidating,
    isImporting,
  } = templateStepUi;

  const [gridCv] = useState<CollectionView>(() => new CollectionView<PlmTemplateMappingGridRowDto>([]));
  const [pipelineMessage, setPipelineMessage] = useState('');
  const [pipelinePercent, setPipelinePercent] = useState(0);
  const [warningDialogRow, setWarningDialogRow] = useState<PlmTemplateMappingGridRowDto | null>(null);

  const sessionId = state.session?.SessionId ?? null;
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    gridCv.sourceCollection = gridRows;
    gridCv.groupDescriptions.clear();
    if (gridRows.length > 0) {
      gridCv.groupDescriptions.push(
        new PropertyGroupDescription('PlmTemplateId', (item: PlmTemplateMappingGridRowDto) => {
          const name = item.PlmTemplateName?.trim();
          return name ? `${name} (ID ${item.PlmTemplateId})` : `Template ID ${item.PlmTemplateId}`;
        }),
      );
    }
    gridCv.refresh();
  }, [gridCv, gridRows]);

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

  const runAnalyze = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return;
    }
    onTemplateStepUiChange({ isAnalyzing: true, validationMessage: null });
    try {
      const result = await plmMigrationSvc.getTemplateTabMappingGrid(sessionId);
      const grid = result.Object;
      if (!grid?.IsSuccess) {
        showError(grid?.ErrorMessage || 'Template analyze failed.');
        return;
      }
      const rows = grid.Rows ?? [];
      const summary = `${grid.TemplateCount} template(s) · ${grid.ReadyCount} ready · ${grid.SkippedCount} skipped · ${grid.WarningCount} warning(s)`;
      const setting = grid.SavedSetting || buildSettingFromRows(rows, null);
      onTemplateStepUiChange({
        gridRows: rows,
        gridSummary: summary,
        importSetting: setting,
        blockAnalysis: grid.Blocks ?? [],
        similarTabGroups: grid.SimilarTabGroups ?? [],
      });
      if (grid.BlockerCount && grid.BlockerCount > 0) {
        showWarning(`${grid.BlockerCount} blocker(s) found. Resolve before import.`);
      }
    } catch (e: any) {
      showError(e?.message || 'Template analyze failed.');
    } finally {
      onTemplateStepUiChange({ isAnalyzing: false });
    }
  }, [onTemplateStepUiChange, sessionId, showError, showWarning]);

  const currentSetting = useMemo(
    () => buildSettingFromRows(gridRows, importSetting),
    [gridRows, importSetting],
  );

  const runSave = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return;
    }
    onTemplateStepUiChange({ isSaving: true });
    try {
      const result = await plmMigrationSvc.saveTemplateMapping(sessionId, currentSetting);
      if (!result.Object) {
        const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ') || 'Save failed.';
        showError(msg);
        return;
      }
      onTemplateStepUiChange({ importSetting: result.Object });
      showInfo('Template mapping saved.', true);
    } catch (e: any) {
      showError(e?.message || 'Save failed.');
    } finally {
      onTemplateStepUiChange({ isSaving: false });
    }
  }, [currentSetting, onTemplateStepUiChange, sessionId, showError, showInfo]);

  const runValidate = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return;
    }
    onTemplateStepUiChange({ isValidating: true });
    try {
      const result = await plmMigrationSvc.validateTemplateMapping(sessionId, currentSetting);
      const validation = result.Object;
      if (!validation) {
        showError('Validation failed.');
        return;
      }
      const parts = [
        ...(validation.Errors || []).map((e) => `Error: ${e}`),
        ...(validation.Warnings || []).map((w) => `Warning: ${w}`),
      ];
      const message = parts.length ? parts.join('\n') : 'Validation passed.';
      onTemplateStepUiChange({ validationMessage: message });
      if (validation.IsValid) {
        showInfo('Validation passed.', true);
      } else {
        showError((validation.Errors || []).join('; ') || 'Validation failed.');
      }
    } catch (e: any) {
      showError(e?.message || 'Validation failed.');
    } finally {
      onTemplateStepUiChange({ isValidating: false });
    }
  }, [currentSetting, onTemplateStepUiChange, sessionId, showError, showInfo]);

  const runImport = useCallback(async () => {
    if (!sessionId) {
      showError('No active import session.');
      return;
    }
    const readyCount = gridRows.filter((r) => r.ImportStatus === 'Ready').length;
    if (readyCount === 0) {
      showWarning('No ready tabs to import. Run Analyze first.');
      return;
    }
    onTemplateStepUiChange({ isImporting: true });
    setPipelineMessage('Starting template structure import…');
    setPipelinePercent(2);
    try {
      const saveResult = await plmMigrationSvc.saveTemplateMapping(sessionId, currentSetting);
      if (!saveResult.Object) {
        const msg = saveResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Save mapping before import failed.';
        showError(msg);
        onTemplateStepUiChange({ isImporting: false });
        return;
      }
      const result = await plmMigrationSvc.executeTemplateImport(sessionId);
      if (!result.Object?.JobId) {
        onTemplateStepUiChange({ isImporting: false });
        const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Failed to start template import.';
        showError(msg);
        return;
      }
      onTemplateStepUiChange({ activeJob: result.Object, importSetting: saveResult.Object });
      setPipelinePercent(result.Object.ProgressPercent ?? 0);
      setPipelineMessage(result.Object.ProgressMessage ?? 'Template import started…');
    } catch (e: any) {
      showError(e?.message || 'Template import failed.');
      onTemplateStepUiChange({ isImporting: false });
    }
  }, [currentSetting, gridRows, onTemplateStepUiChange, sessionId, showError, showWarning]);

  const dialogBlocks = useMemo(() => {
    if (!warningDialogRow) return [];
    return blockAnalysis.filter((b) =>
      (b.ReferencedTabLabels || []).some((l) => l.includes(warningDialogRow.PlmTabName || '')));
  }, [blockAnalysis, warningDialogRow]);

  const dialogSimilarGroups = useMemo(() => {
    if (!warningDialogRow?.SimilarTabGroupId) return [];
    return similarTabGroups.filter((g) => g.GroupId === warningDialogRow.SimilarTabGroupId);
  }, [similarTabGroups, warningDialogRow]);

  const busy = isAnalyzing || isSaving || isValidating || isImporting;

  return (
    <div className="flex flex-col h-full overflow-hidden p-4 gap-3">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 3 — Template Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Analyze template × tab mapping, optionally resolve warnings, save, validate, then import structure.
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-40`}
          onClick={runAnalyze}
          disabled={busy || !sessionId}
        >
          <i className={`fa-solid ${isAnalyzing ? 'fa-spinner fa-spin' : 'fa-magnifying-glass'} mr-1`} />
          Analyze
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-40`}
          onClick={runSave}
          disabled={busy || !sessionId || gridRows.length === 0}
        >
          <i className={`fa-solid ${isSaving ? 'fa-spinner fa-spin' : 'fa-floppy-disk'} mr-1`} />
          Save
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-40`}
          onClick={runValidate}
          disabled={busy || !sessionId || gridRows.length === 0}
        >
          <i className={`fa-solid ${isValidating ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
          Validate
        </button>
        <button
          type="button"
          className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} disabled:opacity-40`}
          onClick={runImport}
          disabled={busy || !sessionId}
        >
          <i className={`fa-solid ${isImporting ? 'fa-spinner fa-spin' : 'fa-file-import'} mr-1`} />
          Run Import
        </button>
        {gridSummary && (
          <span className={`text-xs ${theme.menu_secondary}`}>{gridSummary}</span>
        )}
        {state.templatesComplete && (
          <span className={`text-xs font-semibold ${theme.label}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            Templates imported
          </span>
        )}
      </div>

      {validationMessage && (
        <pre className={`text-xs whitespace-pre-wrap rounded border px-3 py-2 ${theme.inputBox}`}>{validationMessage}</pre>
      )}

      {(isImporting || pipelineMessage) && (
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
          itemsSource={gridCv}
          headersVisibility="All"
          selectionMode="Row"
          className="h-full"
          cellEditEnded={(s: any, e: any) => {
            const flex = s?.control ?? s;
            const item = flex?.rows?.[e.row]?.dataItem as PlmTemplateMappingGridRowDto | undefined;
            if (!item) return;
            const next = gridRows.map((r) => (
              r.PlmTemplateId === item.PlmTemplateId && r.PlmTabId === item.PlmTabId
                ? { ...item }
                : r
            ));
            onTemplateStepUiChange({ gridRows: next });
          }}
        >
          <FlexGridColumn header="Tab" binding="PlmTabName" width={100} isReadOnly />
          <FlexGridColumn header="Txn Group" binding="TransactionGroupName" width={110} isReadOnly />
          <FlexGridColumn header="Transaction" binding="TransactionName" width={100} isReadOnly />
          <FlexGridColumn header="Sibling Table" binding="SiblingTableName" width={140} />
          <FlexGridColumn header="Status" binding="ImportStatus" width={70} isReadOnly />
          <FlexGridColumn header="Action" binding="ImportAction" width={65} isReadOnly />
          <FlexGridColumn header="Warn" binding="ShowTabWarning" width={50} isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: { item: PlmTemplateMappingGridRowDto }) => (
                ctx.item.ShowTabWarning ? (
                  <button
                    type="button"
                    className={`text-amber-500 ${theme.contextMenu}`}
                    title="Open tab warnings"
                    onClick={() => setWarningDialogRow(ctx.item)}
                  >
                    <i className="fa-solid fa-triangle-exclamation" />
                  </button>
                ) : null
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn header="" binding="" width="*" isReadOnly />
        </FlexGrid>
      </div>

      <TemplateTabWarningDialog
        open={!!warningDialogRow}
        tabLabel={warningDialogRow ? `${warningDialogRow.PlmTemplateName} / ${warningDialogRow.PlmTabName}` : ''}
        blocks={dialogBlocks}
        similarTabGroups={dialogSimilarGroups}
        importSetting={currentSetting}
        onClose={() => setWarningDialogRow(null)}
        onApply={(setting) => {
          let rows = [...gridRows];
          for (const group of setting.TabSharedTableGroups || []) {
            if (!group.SharedTableName) continue;
            for (const tabId of group.TabIds || []) {
              rows = rows.map((r) => (
                r.PlmTabId === tabId ? { ...r, SiblingTableName: group.SharedTableName } : r
              ));
            }
          }
          onTemplateStepUiChange({ importSetting: setting, gridRows: rows });
          showInfo('Tab warning overrides applied. Click Save to persist.', true);
        }}
      />
    </div>
  );
};

export default TemplateStep;
