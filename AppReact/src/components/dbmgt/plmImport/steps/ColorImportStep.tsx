import React, { useCallback, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmColorImportExecuteResultDto,
  PlmColorImportPreviewDto,
} from '../../../../webapi/plmMigrationSvc';
import { refreshUserTreeMenu } from '../../../../helper/userMenuHelper';
import type { PlmImportWizardState } from '../types';

type ColorImportStepProps = {
  state: PlmImportWizardState;
};

const formatNumber = (value?: number | null) => (value ?? 0).toLocaleString();

const ColorImportStep: React.FC<ColorImportStepProps> = ({ state }) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();

  const [preview, setPreview] = useState<PlmColorImportPreviewDto | null>(null);
  const [executeResult, setExecuteResult] = useState<PlmColorImportExecuteResultDto | null>(null);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);

  const sessionId = state.session?.SessionId ?? null;
  const isBusy = isPreviewing || isExecuting;
  const canRun = Boolean(sessionId) && !isBusy;

  const handlePreview = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before previewing COLOR IMPORT.');
      return;
    }

    setIsPreviewing(true);
    try {
      const result = await plmMigrationSvc.previewPlmColorImport(sessionId);
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'Failed to preview COLOR IMPORT.');
        setPreview(result.Object ?? null);
        return;
      }
      setPreview(result.Object);
      setExecuteResult(null);
      showInfo('COLOR IMPORT preview completed.', true);
    } catch (error: any) {
      showError(error?.message || 'Failed to preview COLOR IMPORT.');
    } finally {
      setIsPreviewing(false);
    }
  }, [sessionId, showError, showInfo, showWarning]);

  const handleExecute = useCallback(async () => {
    if (!sessionId) {
      showWarning('Connect to PLM and save the import session before running COLOR IMPORT.');
      return;
    }

    if (!window.confirm(
      'Create or update RGB Color transaction, searches, folder navigation, and menus?\n\n'
      + 'Recommended order: Entity Import → Folder Import → COLOR IMPORT.',
    )) {
      return;
    }

    setIsExecuting(true);
    try {
      const result = await plmMigrationSvc.executePlmColorImport({
        SessionId: sessionId,
        SaasApplicationId: state.saasApplicationId,
      });
      if (!result.Object?.IsSuccess) {
        showError(result.Object?.ErrorMessage || 'COLOR IMPORT failed.');
        setExecuteResult(result.Object ?? null);
        return;
      }

      setExecuteResult(result.Object);
      await refreshUserTreeMenu();
      showInfo('COLOR IMPORT completed. Refresh the app menu if needed.', true);
    } catch (error: any) {
      showError(error?.message || 'COLOR IMPORT failed.');
    } finally {
      setIsExecuting(false);
    }
  }, [sessionId, showError, showInfo, showWarning, state.saasApplicationId]);

  const buttonClass = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
  const secondaryButtonClass = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`;

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex-none px-4 py-3 border-b ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>COLOR IMPORT</div>
        <div className={`text-xs mt-1 ${theme.label}`}>
          Creates RGB Color editor, list search, and folder navigation configuration.
        </div>
      </div>

      <div className="h-1 flex-auto overflow-auto px-4 py-3 space-y-4">
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
            {isExecuting ? 'Executing…' : 'Execute COLOR IMPORT'}
          </button>
        </div>

        {preview && (
          <div className={`rounded border p-3 ${theme.inputBox}`}>
            <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Preview</div>
            <div className={`text-xs space-y-1 ${theme.label}`}>
              <div>RGB table rows: {formatNumber(preview.RgbColorRowCount)}</div>
              <div>Color-folder links: {formatNumber(preview.ColorGroupDetailRowCount)}</div>
              <div>PLM color root folders: {formatNumber(preview.PlmColorRootFolderCount)}</div>
              <div>Root strategy: {preview.RootFolderStrategy || '—'}</div>
              <div>
                Resolved APP root folder:
                {' '}
                {preview.ResolvedAppRootFolderId
                  ? `${preview.ResolvedAppRootFolderName || 'Folder'} (#${preview.ResolvedAppRootFolderId})`
                  : '—'}
              </div>
              {preview.ExistingTransactionId ? (
                <div>Existing transaction: #{preview.ExistingTransactionId}</div>
              ) : null}
            </div>

            {preview.PlmColorRootFolders && preview.PlmColorRootFolders.length > 0 && (
              <div className="mt-3">
                <div className={`text-xs font-semibold mb-1 ${theme.title}`}>PLM Color Roots</div>
                <ul className={`text-xs list-disc pl-5 ${theme.label}`}>
                  {preview.PlmColorRootFolders.map((row) => (
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
            )}

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
              <div>Transaction: {executeResult.TransactionId ?? '—'}</div>
              <div>List search: {executeResult.ListSearchId ?? '—'}</div>
              <div>Folder template search: {executeResult.FolderTemplateSearchId ?? '—'}</div>
              <div>APP root folder: {executeResult.AppRootFolderId ?? '—'}</div>
              <div>Root strategy: {executeResult.RootFolderStrategy || '—'}</div>
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

export default ColorImportStep;
