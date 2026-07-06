import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmFolderImportPreviewDto,
  PlmFolderImportResultDto,
  PlmFolderPlacementPreviewDto,
  PlmFolderPlacementResultDto,
  PlmImportJobDto,
} from '../../../../webapi/plmMigrationSvc';
import type { PlmImportWizardState } from '../types';

type FolderImportStepProps = {
  state: PlmImportWizardState;
};

const TERMINAL_JOB_STATUSES = ['Completed', 'Failed', 'Cancelled'];

const formatNumber = (value?: number | null) => (value ?? 0).toLocaleString();

const parseJobResult = <T,>(job?: PlmImportJobDto | null): T | null => {
  if (!job?.ResultJson) return null;
  try {
    return JSON.parse(job.ResultJson) as T;
  } catch {
    return null;
  }
};

const FolderImportStep: React.FC<FolderImportStepProps> = ({ state }) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const [importPreview, setImportPreview] = useState<PlmFolderImportPreviewDto | null>(null);
  const [placementPreview, setPlacementPreview] = useState<PlmFolderPlacementPreviewDto | null>(null);
  const [activeJob, setActiveJob] = useState<PlmImportJobDto | null>(null);
  const [isPreviewingImport, setIsPreviewingImport] = useState(false);
  const [isExecutingImport, setIsExecutingImport] = useState(false);
  const [isPreviewingPlacement, setIsPreviewingPlacement] = useState(false);
  const [isExecutingPlacement, setIsExecutingPlacement] = useState(false);
  const finalizedJobIdsRef = useRef<Set<number>>(new Set());

  const sessionId = state.session?.SessionId ?? null;
  const importResult = parseJobResult<PlmFolderImportResultDto>(activeJob?.JobType === 'PlmFolderImport' ? activeJob : null);
  const placementResult = parseJobResult<PlmFolderPlacementResultDto>(activeJob?.JobType === 'PlmFolderPlacement' ? activeJob : null);

  const isJobRunning = Boolean(activeJob?.JobId && !TERMINAL_JOB_STATUSES.includes(activeJob.Status || ''));
  const isBusy = isPreviewingImport || isExecutingImport || isPreviewingPlacement || isExecutingPlacement || isJobRunning;
  const canRun = Boolean(sessionId) && !isBusy;

  const handlePreviewImport = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before previewing folder import.');
      return;
    }

    setIsPreviewingImport(true);
    try {
      const result = await plmMigrationSvc.previewPlmFolderImport(sessionId);
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'Failed to preview PLM folder import.');
        return;
      }
      setImportPreview(result.Object);
      showInfo('PLM folder import preview completed.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to preview PLM folder import.');
    } finally {
      setIsPreviewingImport(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleExecuteImport = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before importing folders.');
      return;
    }

    if (!window.confirm(
      'Import PLM folder trees (Product, Color, Sketch) and color-folder links (pdmColorGroupDetail)?\n\nThis creates AppSEFolder rows and AppPlmFolderMap entries.',
    )) return;

    setIsExecutingImport(true);
    try {
      const result = await plmMigrationSvc.executePlmFolderImport(sessionId);
      if (!result.Object?.JobId) {
        showError(result.ValidationResult?.Items?.[0]?.Message || 'Failed to start PLM folder import.');
        return;
      }
      finalizedJobIdsRef.current.delete(result.Object.JobId);
      setActiveJob(result.Object);
      showInfo('PLM folder import job started.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to start PLM folder import.');
    } finally {
      setIsExecutingImport(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handlePreviewPlacement = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before previewing folder placement.');
      return;
    }

    setIsPreviewingPlacement(true);
    try {
      const result = await plmMigrationSvc.previewPlmFolderPlacement(sessionId);
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'Failed to preview PLM folder placement.');
        return;
      }
      setPlacementPreview(result.Object);
      showInfo('PLM folder placement preview completed.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to preview PLM folder placement.');
    } finally {
      setIsPreviewingPlacement(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleExecutePlacement = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before placing objects into folders.');
      return;
    }

    if (!window.confirm(
      'Place imported products, color-folder links, and images into their APP folders?\n\nRun this after folder import (and image import for AppFile rows).',
    )) return;

    setIsExecutingPlacement(true);
    try {
      const result = await plmMigrationSvc.executePlmFolderPlacement(sessionId);
      if (!result.Object?.JobId) {
        showError(result.ValidationResult?.Items?.[0]?.Message || 'Failed to start PLM folder placement.');
        return;
      }
      finalizedJobIdsRef.current.delete(result.Object.JobId);
      setActiveJob(result.Object);
      showInfo('PLM folder placement job started.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to start PLM folder placement.');
    } finally {
      setIsExecutingPlacement(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleCancel = useCallback(async () => {
    if (!activeJob?.JobId) return;
    try {
      await plmMigrationSvc.cancelImportJob(activeJob.JobId);
      showInfo('Cancel requested for folder job.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to cancel folder job.');
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
            showInfo(`PLM ${result.Object.JobType || 'folder'} job completed.`, true);
          } else {
            showError(result.Object.ErrorMessage || `PLM folder job ${result.Object.Status}.`);
          }
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to refresh folder job.');
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
          <h2 className={`text-sm font-semibold ${theme.title}`}>Step 4 — Folder Import</h2>
          <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
            Import PLM folder trees and pdmColorGroupDetail links, then place products, colors, and images.
          </p>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-auto px-5 py-5 ${theme.mainContentSection}`}>
        <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
          <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Phase 1 — Folder Tree + Color Links</div>
          <div className={`text-xs leading-6 mb-3 ${theme.menu_secondary}`}>
            Imports pdmSEFolder (Product, Color, Sketch) into AppSEFolder with AppPlmFolderMap, and copies pdmColorGroupDetail into Plm_pdmColorGroupDetail (multi-folder per color).
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button type="button" className={secondaryButtonClass} onClick={handlePreviewImport} disabled={!canRun}>
              <i className={`fa-solid ${isPreviewingImport ? 'fa-spinner fa-spin' : 'fa-magnifying-glass'}`} />
              Preview Import
            </button>
            <button type="button" className={buttonClass} onClick={handleExecuteImport} disabled={!canRun}>
              <i className={`fa-solid ${isExecutingImport ? 'fa-spinner fa-spin' : 'fa-play'}`} />
              Execute Import
            </button>
          </div>
        </div>

        {importPreview && (
          <div className="mb-4">
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Import Preview</div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-3">
              {renderMetric('Color Links (PLM)', importPreview.ColorDetailSourceCount)}
              {renderMetric('Color Links Ready', importPreview.ColorDetailReadyToImport)}
              {renderMetric('Color Links Existing', importPreview.ColorDetailExistingCount)}
            </div>
            {(importPreview.Scopes?.length ?? 0) > 0 && (
              <div className={`rounded border overflow-hidden ${theme.inputBox}`}>
                <table className="w-full text-xs">
                  <thead className={theme.mainContentSection}>
                    <tr>
                      <th className="text-left px-2 py-1">Type</th>
                      <th className="text-right px-2 py-1">PLM Folders</th>
                      <th className="text-right px-2 py-1">To Create</th>
                      <th className="text-right px-2 py-1">Mapped</th>
                    </tr>
                  </thead>
                  <tbody>
                    {importPreview.Scopes?.map((scope) => (
                      <tr key={scope.PlmFolderType} className="border-t">
                        <td className="px-2 py-1">{scope.PlmFolderTypeName || scope.PlmFolderType}</td>
                        <td className="text-right px-2 py-1">{formatNumber(scope.TotalPlmFolders)}</td>
                        <td className="text-right px-2 py-1">{formatNumber(scope.ToCreateCount)}</td>
                        <td className="text-right px-2 py-1">{formatNumber(scope.ExistingMappedFolders)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
          <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Phase 2 — Placement</div>
          <div className={`text-xs leading-6 mb-3 ${theme.menu_secondary}`}>
            Updates Plm_ReferenceBasicInfo.FolderId, Plm_pdmColorGroupDetail.AppFolderId, and AppFile.FolderID using AppPlmFolderMap.
          </div>
          <div className="flex flex-wrap items-center gap-2">
            <button type="button" className={secondaryButtonClass} onClick={handlePreviewPlacement} disabled={!canRun}>
              <i className={`fa-solid ${isPreviewingPlacement ? 'fa-spinner fa-spin' : 'fa-magnifying-glass'}`} />
              Preview Placement
            </button>
            <button type="button" className={buttonClass} onClick={handleExecutePlacement} disabled={!canRun}>
              <i className={`fa-solid ${isExecutingPlacement ? 'fa-spinner fa-spin' : 'fa-play'}`} />
              Execute Placement
            </button>
            {isJobRunning && (
              <button type="button" className={secondaryButtonClass} onClick={handleCancel}>
                <i className="fa-solid fa-ban" />
                Cancel
              </button>
            )}
          </div>
        </div>

        {placementPreview && (
          <div className="mb-4">
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Placement Preview</div>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
              {renderMetric('Products Ready', placementPreview.ProductReadyCount)}
              {renderMetric('Products Missing Map', placementPreview.ProductMissingFolderMapCount)}
              {renderMetric('Color Links Ready', placementPreview.ColorDetailReadyCount)}
              {renderMetric('Color Links Missing Map', placementPreview.ColorDetailMissingFolderMapCount)}
              {renderMetric('Images Ready', placementPreview.ImageReadyCount)}
              {renderMetric('Images Missing Map', placementPreview.ImageMissingFolderMapCount)}
            </div>
          </div>
        )}

        {activeJob && (
          <div className={`rounded border p-4 mb-4 ${theme.inputBox}`}>
            <div className="flex items-center justify-between mb-2">
              <div className={`text-xs font-semibold ${theme.label}`}>
                Job #{activeJob.JobId} · {activeJob.JobType} · {activeJob.Status || 'Queued'}
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

        {importResult && (
          <div className="mb-4">
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Last Import Result</div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {renderMetric('Folders Created', importResult.FoldersCreated)}
              {renderMetric('Mappings Written', importResult.MappingsWritten)}
              {renderMetric('Color Links Inserted', importResult.ColorDetailsInserted)}
              {renderMetric('Skipped Existing', importResult.FoldersSkippedExisting)}
            </div>
          </div>
        )}

        {placementResult && (
          <div>
            <div className={`text-xs font-semibold mb-2 ${theme.label}`}>Last Placement Result</div>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {renderMetric('Products Updated', placementResult.ProductsUpdated)}
              {renderMetric('Color Links Updated', placementResult.ColorDetailsUpdated)}
              {renderMetric('Images Updated', placementResult.AppFilesUpdated)}
              {renderMetric('Skipped (No Map)', placementResult.SkippedNoMapping)}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default FolderImportStep;
