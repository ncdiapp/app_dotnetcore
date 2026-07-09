import React, { useCallback, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmPomImportExecuteResultDto,
  PlmPomImportPreviewDto,
} from '../../../../webapi/plmMigrationSvc';
import { refreshUserTreeMenu } from '../../../../helper/userMenuHelper';
import type { PlmImportWizardState } from '../types';

type PomImportStepProps = {
  state: PlmImportWizardState;
};

const formatNumber = (value?: number | null) => (value ?? 0).toLocaleString();

const PomImportStep: React.FC<PomImportStepProps> = ({ state }) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const [preview, setPreview] = useState<PlmPomImportPreviewDto | null>(null);
  const [executeResult, setExecuteResult] = useState<PlmPomImportExecuteResultDto | null>(null);
  const [importJunctionTables, setImportJunctionTables] = useState(true);
  const [importFoldersIfMissing, setImportFoldersIfMissing] = useState(true);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);

  const sessionId = state.session?.SessionId ?? null;
  const isBusy = isPreviewing || isExecuting;
  const canRun = Boolean(sessionId) && !isBusy;

  const handlePreview = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before previewing POM IMPORT.');
      return;
    }

    setIsPreviewing(true);
    try {
      const result = await plmMigrationSvc.previewPlmPomImport(sessionId);
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'Failed to preview POM IMPORT.');
        setPreview(result.Object ?? null);
        return;
      }
      setPreview(result.Object);
      setExecuteResult(null);
      showInfo('POM IMPORT preview completed.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to preview POM IMPORT.');
    } finally {
      setIsPreviewing(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleExecute = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before running POM IMPORT.');
      return;
    }

    if (!window.confirm(
      'Create or update POM Management and POM Template Management configurations?\n\n'
      + 'Includes transactions, searches, folder navigation, and optional junction table import.\n\n'
      + 'Recommended order: Entity Import → Folder Import → POM IMPORT.',
    )) {
      return;
    }

    setIsExecuting(true);
    try {
      const result = await plmMigrationSvc.executePlmPomImport({
        SessionId: sessionId,
        SaasApplicationId: state.saasApplicationId,
        ImportJunctionTables: importJunctionTables,
        ImportFoldersIfMissing: importFoldersIfMissing,
      });
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'POM IMPORT failed.');
        setExecuteResult(result.Object ?? null);
        return;
      }

      setExecuteResult(result.Object);
      await refreshUserTreeMenu();
      showInfo('POM IMPORT completed. Refresh the app menu if needed.', true);
    } catch (error: any) {
      showError(error?.message || 'POM IMPORT failed.');
    } finally {
      setIsExecuting(false);
    }
  }, [importFoldersIfMissing, importJunctionTables, sessionId, showError, showInfo, showWarning, state.saasApplicationId]);

  const buttonClass = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
  const secondaryButtonClass = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`;
  const checkboxClass = `mr-2 ${theme.label}`;

  const renderRootFolders = (title: string, rows?: PlmPomImportPreviewDto['PlmPomRootFolders']) => {
    if (!rows || rows.length === 0) return null;
    return (
      <div className="mt-3">
        <div className={`text-xs font-semibold mb-1 ${theme.title}`}>{title}</div>
        <ul className={`text-xs list-disc pl-5 ${theme.label}`}>
          {rows.map((row) => (
            <li key={row.PlmFolderId}>
              PLM {row.PlmFolderName} ({row.PlmFolderId})
              {' → '}
              {row.AppFolderId
                ? `APP ${row.AppFolderName} (#${row.AppFolderId})`
                : 'not mapped'}
            </li>
          ))}
        </ul>
      </div>
    );
  };

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex-none px-4 py-3 border-b ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>POM IMPORT</div>
        <div className={`text-xs mt-1 ${theme.label}`}>
          Creates POM Management (FolderType 14) and POM Template Management (FolderType 5) editors,
          list searches, and folder navigation. FolderID is stored directly on each POM row.
        </div>
      </div>

      <div className="h-1 flex-auto overflow-auto px-4 py-3 space-y-4">
        <div className="flex flex-wrap gap-4 items-center">
          <label className={`text-xs flex items-center ${theme.label}`}>
            <input
              type="checkbox"
              className={checkboxClass}
              checked={importJunctionTables}
              onChange={(e) => setImportJunctionTables(e.target.checked)}
              disabled={isBusy}
            />
            Import junction tables (BodyTypeDetail, SpecBodyPartGrading)
          </label>
          <label className={`text-xs flex items-center ${theme.label}`}>
            <input
              type="checkbox"
              className={checkboxClass}
              checked={importFoldersIfMissing}
              onChange={(e) => setImportFoldersIfMissing(e.target.checked)}
              disabled={isBusy}
            />
            Import POM folders (types 5 &amp; 14) if not mapped
          </label>
        </div>

        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            disabled={!canRun}
            onClick={handlePreview}
            className={secondaryButtonClass}
          >
            {isPreviewing ? 'Previewing…' : 'Preview'}
          </button>
          <button
            type="button"
            disabled={!canRun}
            onClick={handleExecute}
            className={buttonClass}
          >
            {isExecuting ? 'Executing…' : 'Execute POM IMPORT'}
          </button>
        </div>

        {preview && (
          <div className={`rounded border p-3 ${theme.inputBox}`}>
            <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Preview</div>
            <div className={`text-xs space-y-1 ${theme.label}`}>
              <div>POM (BodyPart) rows: {formatNumber(preview.BodyPartRowCount)}</div>
              <div>POM Template (BodyType) rows: {formatNumber(preview.BodyTypeRowCount)}</div>
              <div>
                BodyTypeDetail:
                {' '}
                {preview.HasBodyTypeDetailTable
                  ? `${formatNumber(preview.BodyTypeDetailRowCount)} in tenant`
                  : `${formatNumber(preview.BodyTypeDetailSourceRowCount)} in PLM (not imported)`}
              </div>
              <div>
                SpecBodyPartGrading:
                {' '}
                {preview.HasSpecBodyPartGradingTable
                  ? `${formatNumber(preview.SpecBodyPartGradingRowCount)} in tenant`
                  : `${formatNumber(preview.SpecBodyPartGradingSourceRowCount)} in PLM (not imported)`}
              </div>
              <div>
                POM root folder:
                {' '}
                {preview.PomAppRootFolderId
                  ? `${preview.PomAppRootFolderName || 'Folder'} (#${preview.PomAppRootFolderId})`
                  : '—'}
              </div>
              <div>
                POM Template root folder:
                {' '}
                {preview.PomTemplateAppRootFolderId
                  ? `${preview.PomTemplateAppRootFolderName || 'Folder'} (#${preview.PomTemplateAppRootFolderId})`
                  : '—'}
              </div>
            </div>

            {renderRootFolders('PLM POM Roots (FolderType 14)', preview.PlmPomRootFolders)}
            {renderRootFolders('PLM Template Roots (FolderType 5)', preview.PlmPomTemplateRootFolders)}

            {preview.Warnings && preview.Warnings.length > 0 && (
              <div className="mt-3">
                <div className={`text-xs font-semibold mb-1 ${theme.title}`}>Warnings</div>
                <ul className="text-xs list-disc pl-5 text-amber-700">
                  {preview.Warnings.map((warning) => (
                    <li key={warning}>{warning}</li>
                  ))}
                </ul>
              </div>
            )}

            {preview.PlannedActions && preview.PlannedActions.length > 0 && (
              <div className="mt-3">
                <div className={`text-xs font-semibold mb-1 ${theme.title}`}>Planned Actions</div>
                <ul className={`text-xs list-disc pl-5 ${theme.label}`}>
                  {preview.PlannedActions.map((action) => (
                    <li key={action}>{action}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}

        {executeResult && (
          <div className={`rounded border p-3 ${theme.inputBox}`}>
            <div className={`text-sm font-semibold mb-2 ${theme.title}`}>
              {executeResult.IsSuccess ? 'Execute Result' : 'Execute Failed'}
            </div>
            {!executeResult.IsSuccess && executeResult.ErrorMessage ? (
              <div className="text-xs text-red-600 mb-2">{executeResult.ErrorMessage}</div>
            ) : null}
            <div className={`text-xs space-y-1 ${theme.label}`}>
              <div>POM transaction: {executeResult.PomTransactionId ?? '—'}</div>
              <div>POM list search: {executeResult.PomListSearchId ?? '—'}</div>
              <div>POM folder search: {executeResult.PomFolderSearchId ?? '—'}</div>
              <div>POM root folder: {executeResult.PomAppRootFolderId ?? '—'}</div>
              <div>POM Template transaction: {executeResult.PomTemplateTransactionId ?? '—'}</div>
              <div>POM Template list search: {executeResult.PomTemplateListSearchId ?? '—'}</div>
              <div>POM Template folder search: {executeResult.PomTemplateFolderSearchId ?? '—'}</div>
              <div>POM Template root folder: {executeResult.PomTemplateAppRootFolderId ?? '—'}</div>
              <div>Junction rows imported: {formatNumber(executeResult.BodyTypeDetailRowsImported + executeResult.SpecBodyPartGradingRowsImported)}</div>
              <div>Folders imported: {formatNumber(executeResult.FoldersImported)}</div>
            </div>
            {executeResult.Messages && executeResult.Messages.length > 0 && (
              <ul className={`text-xs list-disc pl-5 mt-2 ${theme.label}`}>
                {executeResult.Messages.map((message) => (
                  <li key={message}>{message}</li>
                ))}
              </ul>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default PomImportStep;
