import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmImportJobDto,
  PlmSketchImportPreviewDto,
  PlmSketchImportResultDto,
} from '../../../../webapi/plmMigrationSvc';
import type { PlmImportWizardState } from '../types';

type OtherDataStepProps = {
  state: PlmImportWizardState;
};

const TERMINAL_JOB_STATUSES = ['Completed', 'Failed', 'Cancelled'];

const formatNumber = (value?: number | null) => (value ?? 0).toLocaleString();

const parseSketchResult = (job?: PlmImportJobDto | null): PlmSketchImportResultDto | null => {
  if (!job?.ResultJson) return null;
  try {
    return JSON.parse(job.ResultJson) as PlmSketchImportResultDto;
  } catch {
    return null;
  }
};

const OtherDataStep: React.FC<OtherDataStepProps> = ({ state }) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const [preview, setPreview] = useState<PlmSketchImportPreviewDto | null>(null);
  const [activeJob, setActiveJob] = useState<PlmImportJobDto | null>(null);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  const sessionId = state.session?.SessionId ?? null;
  const sketchResult = parseSketchResult(activeJob);

  const isJobRunning = Boolean(activeJob?.JobId && !TERMINAL_JOB_STATUSES.includes(activeJob.Status || ''));
  const canRun = Boolean(sessionId) && !isPreviewing && !isExecuting && !isJobRunning;

  const handlePreview = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before previewing sketch import.');
      return;
    }

    setIsPreviewing(true);
    try {
      const result = await plmMigrationSvc.previewPlmSketchImport(sessionId);
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'Failed to preview PLM sketch import.');
        return;
      }
      setPreview(result.Object);
      showInfo('PLM sketch import preview completed.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to preview PLM sketch import.');
    } finally {
      setIsPreviewing(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleExecute = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before importing sketches.');
      return;
    }

    if (!window.confirm(
      'Import all PLM tblSketch rows into AppFile?\n\nThis keeps AppFile.FileID equal to tblSketch.SketchID and skips existing AppFile IDs.',
    )) return;

    setIsExecuting(true);
    try {
      const result = await plmMigrationSvc.executePlmSketchImport(sessionId);
      if (!result.Object?.JobId) {
        showError(result.ValidationResult?.Items?.[0]?.Message || 'Failed to start PLM sketch import.');
        return;
      }
      finalizedJobIdsRef.current.delete(result.Object.JobId);
      setActiveJob(result.Object);
      showInfo('PLM sketch import job started.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to start PLM sketch import.');
    } finally {
      setIsExecuting(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleCancel = useCallback(async () => {
    if (!activeJob?.JobId) return;
    try {
      await plmMigrationSvc.cancelImportJob(activeJob.JobId);
      showInfo('Cancel requested for sketch import job.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to cancel sketch import job.');
    }
  }, [activeJob?.JobId, showError, showInfo]);

  useEffect(() => {
    if (!activeJob?.JobId || TERMINAL_JOB_STATUSES.includes(activeJob.Status || '')) return undefined;

    const timer = window.setInterval(async () => {
      try {
        const result = await plmMigrationSvc.getImportJob(activeJob.JobId);
        if (!result.Object) return;
        setActiveJob(result.Object);

        if (TERMINAL_JOB_STATUSES.includes(result.Object.Status || '')
          && !finalizedJobIdsRef.current.has(result.Object.JobId)) {
          finalizedJobIdsRef.current.add(result.Object.JobId);
          if (result.Object.Status === 'Completed') {
            showInfo('PLM sketch import completed.', true);
          } else {
            showError(result.Object.ErrorMessage || `PLM sketch import ${result.Object.Status}.`);
          }
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to refresh sketch import job.');
      }
    }, 2000);

    return () => window.clearInterval(timer);
  }, [activeJob?.JobId, activeJob?.Status, showError, showInfo]);

  const renderMetric = (label: string, value?: number | null) => (
    <div className={`rounded border px-3 py-2 ${theme.mainContentSection}`}>
      <div className={`text-[10px] uppercase tracking-wide ${theme.menu_secondary}`}>{label}</div>
      <div className={`text-lg font-semibold ${theme.title}`}>{formatNumber(value)}</div>
    </div>
  );

  const buttonClass = `inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
  const secondaryButtonClass = `inline-flex items-center gap-2 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`;

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div>
          <h2 className={`text-sm font-semibold ${theme.title}`}>Step 4 — Other Data</h2>
          <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
            Import PLM tblSketch binary data into AppFile while keeping FileID equal to SketchID.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={secondaryButtonClass}
            onClick={handlePreview}
            disabled={!canRun}
            title="Count tblSketch rows and AppFile conflicts"
          >
            <i className={`fa-solid ${isPreviewing ? 'fa-spinner fa-spin' : 'fa-magnifying-glass'}`} />
            Preview Sketches
          </button>
          <button
            type="button"
            className={buttonClass}
            onClick={handleExecute}
            disabled={!canRun}
            title="Create AppFile rows and write image files"
          >
            <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-play'}`} />
            Execute Import
          </button>
          {isJobRunning && (
            <button
              type="button"
              className={secondaryButtonClass}
              onClick={handleCancel}
              title="Request cancellation"
            >
              <i className="fa-solid fa-ban" />
              Cancel
            </button>
          )}
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-auto px-5 py-5 ${theme.mainContentSection}`}>
        <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
          <div className={`text-xs font-semibold mb-2 ${theme.label}`}>PLM Sketch Import Rules</div>
          <div className={`text-xs leading-6 ${theme.menu_secondary}`}>
            <div>Images: OriginalImage → <code>/original/&lbrace;guid&rbrace;</code>, SketchImage → <code>/regular/&lbrace;guid&rbrace;</code>, Thumbnail → <code>/thumbnail/&lbrace;guid&rbrace;</code>.</div>
            <div>Files: non-image rows store the available binary in <code>AppFile.FileContent</code>.</div>
            <div>IDs: <code>AppFile.FileID</code> is inserted explicitly from <code>tblSketch.SketchID</code>; existing FileIDs are skipped.</div>
          </div>
        </div>

        {preview && (
          <div className="mb-4">
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Preview</div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {renderMetric('Source Sketches', preview.SourceSketchCount)}
              {renderMetric('With Binary', preview.SourceWithBinaryCount)}
              {renderMetric('Images', preview.ImageCount)}
              {renderMetric('Files', preview.FileCount)}
              {renderMetric('Existing AppFiles', preview.ExistingAppFileCount)}
              {renderMetric('Ready To Import', preview.ReadyToImportCount)}
              {renderMetric('Missing Binary', preview.MissingBinaryCount)}
            </div>
            {(preview.Warnings?.length ?? 0) > 0 && (
              <div className={`mt-3 text-xs ${theme.menu_secondary}`}>
                {preview.Warnings?.map((warning) => <div key={warning}>{warning}</div>)}
              </div>
            )}
          </div>
        )}

        {activeJob && (
          <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
            <div className="flex items-center justify-between mb-2">
              <div className={`text-xs font-semibold ${theme.label}`}>
                Job #{activeJob.JobId} · {activeJob.Status || 'Queued'}
              </div>
              <div className={`text-xs ${theme.menu_secondary}`}>{activeJob.ProgressPercent || 0}%</div>
            </div>
            <div className={`w-full h-2 rounded overflow-hidden border ${theme.inputBox}`}>
              <div
                className="h-2 bg-current"
                style={{ width: `${Math.max(0, Math.min(100, activeJob.ProgressPercent || 0))}%` }}
              />
            </div>
            <div className={`text-xs mt-2 ${theme.menu_secondary}`}>
              {activeJob.ProgressMessage || activeJob.ErrorMessage || 'Waiting for job update...'}
            </div>
          </div>
        )}

        {sketchResult && (
          <div>
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Last Result</div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {renderMetric('Inserted', sketchResult.InsertedCount)}
              {renderMetric('Image Inserted', sketchResult.ImageInsertedCount)}
              {renderMetric('File Inserted', sketchResult.FileInsertedCount)}
              {renderMetric('Skipped Existing', sketchResult.SkippedExistingCount)}
              {renderMetric('Missing Binary', sketchResult.SkippedMissingBinaryCount)}
              {renderMetric('Failed', sketchResult.FailedCount)}
            </div>
            {(sketchResult.Errors?.length ?? 0) > 0 && (
              <div className={`mt-3 rounded border p-3 text-xs ${theme.inputBox}`}>
                {sketchResult.Errors?.slice(0, 20).map((error) => <div key={error}>{error}</div>)}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default OtherDataStep;
