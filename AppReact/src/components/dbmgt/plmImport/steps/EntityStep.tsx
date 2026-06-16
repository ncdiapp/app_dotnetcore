import React, { useCallback, useEffect, useMemo, useState } from 'react';
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
  const { showError, showInfo } = useErrorMessage();

  const { activeTab, planItems, activeJob, isExporting } = entityStepUi;
  const [isPreviewLoading, setIsPreviewLoading] = useState(false);
  const [cv] = useState<CollectionView>(() => new CollectionView<PlmTableExportPlanItemDto>([]));

  const sessionId = state.session?.SessionId ?? null;
  const userDefineLocked = !state.systemDefineComplete;

  useEffect(() => {
    cv.sourceCollection = planItems;
    cv.refresh();
  }, [cv, planItems]);

  const pollJob = useCallback(async (jobId: number) => {
    const result = await plmMigrationSvc.getImportJob(jobId);
    if (result.Object) onEntityStepUiChange({ activeJob: result.Object });
    return result.Object;
  }, [onEntityStepUiChange]);

  useEffect(() => {
    if (!activeJob?.JobId) return undefined;
    const terminal = ['Completed', 'Failed', 'Cancelled'];
    if (terminal.includes(activeJob.Status || '')) return undefined;

    const timer = window.setInterval(async () => {
      try {
        const job = await pollJob(activeJob.JobId);
        if (!job) return;
        if (job.Status === 'Completed') {
          showInfo('PLM table export completed.', true);
          onStateChange({ systemDefineComplete: true });
          if (sessionId) {
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
          }
          onEntityStepUiChange({ isExporting: false });
        } else if (job.Status === 'Failed' || job.Status === 'Cancelled') {
          showError(job.ErrorMessage || job.ProgressMessage || 'Export failed.');
          onEntityStepUiChange({ isExporting: false });
        }
      } catch {
        // keep polling
      }
    }, 2000);

    return () => window.clearInterval(timer);
  }, [
    activeJob,
    onEntityStepUiChange,
    onSessionSaved,
    onStateChange,
    pollJob,
    sessionId,
    showError,
    showInfo,
    state.connectionTested,
    state.plmConnectionString,
    state.saasApplicationId,
    state.session?.SessionGuid,
    state.targetCompanyId,
  ]);

  const handlePreview = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before exporting tables.');
      return;
    }
    setIsPreviewLoading(true);
    try {
      const result = await plmMigrationSvc.previewPlmTableExportPlan(sessionId);
      if (result.Object?.IsSuccess && result.Object.Tables) {
        onEntityStepUiChange({ planItems: result.Object.Tables });
        showInfo(`Found ${result.Object.Tables.length} PLM table(s) to export.`, true);
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
  }, [onEntityStepUiChange, sessionId, showError, showInfo]);

  const handleExport = useCallback(async () => {
    if (!sessionId) {
      showError('Save a connection session before exporting tables.');
      return;
    }
    onEntityStepUiChange({ isExporting: true });
    try {
      const result = await plmMigrationSvc.executePlmTableExport(sessionId);
      if (result.Object?.JobId) {
        onEntityStepUiChange({ activeJob: result.Object });
      } else {
        onEntityStepUiChange({ isExporting: false });
        const msg = result.ValidationResult?.Items?.map((i: { Message: string }) => i.Message).join('; ')
          || 'Failed to start export job.';
        showError(msg);
      }
    } catch (e: any) {
      onEntityStepUiChange({ isExporting: false });
      showError(e?.message || 'Failed to start export.');
    }
  }, [onEntityStepUiChange, sessionId, showError]);

  const exportProgress = useMemo(() => {
    if (!activeJob) return null;
    return `${activeJob.ProgressPercent ?? 0}% — ${activeJob.ProgressMessage || activeJob.Status || ''}`;
  }, [activeJob]);

  return (
    <div className="flex flex-col gap-4 p-4 h-full overflow-hidden">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 2 — Entity Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Export System Define PLM tables into the tenant database first, then import entity metadata.
        </p>
      </div>

      <div className="flex gap-2">
        <button
          type="button"
          className={`px-3 py-1 text-xs rounded-[4px] ${activeTab === 'system' ? theme.tab_active : theme.tab}`}
          onClick={() => onEntityStepUiChange({ activeTab: 'system' })}
        >
          System Define
        </button>
        <button
          type="button"
          disabled={userDefineLocked}
          className={`px-3 py-1 text-xs rounded-[4px] ${
            activeTab === 'user' ? theme.tab_active : theme.tab
          } ${userDefineLocked ? 'opacity-40 cursor-not-allowed' : ''}`}
          onClick={() => !userDefineLocked && onEntityStepUiChange({ activeTab: 'user' })}
          title={userDefineLocked ? 'Complete System Define export first.' : undefined}
        >
          User Define
        </button>
      </div>

      {activeTab === 'system' && (
        <div className="flex flex-col gap-3 h-1 flex-auto overflow-hidden">
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handlePreview}
              disabled={isPreviewLoading || isExporting}
            >
              {isPreviewLoading ? 'Loading…' : 'Preview export plan'}
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handleExport}
              disabled={isExporting || planItems.length === 0}
            >
              {isExporting ? 'Exporting…' : 'Export PLM tables'}
            </button>
          </div>

          {exportProgress && (
            <p className={`text-xs ${theme.menu_secondary}`}>{exportProgress}</p>
          )}

          <div className="h-1 flex-auto overflow-hidden min-h-[160px]">
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

          <p className={`text-xs ${theme.menu_secondary}`}>
            Status: {state.systemDefineComplete ? 'Table export complete' : 'Not started'}
          </p>
        </div>
      )}

      {activeTab === 'user' && (
        <div className={`border rounded p-3 text-xs ${theme.inputBox}`}>
          <p className={theme.menu_secondary}>
            User Define entity preview and import will be available in Phase 4.
          </p>
        </div>
      )}
    </div>
  );
};

export default EntityStep;
