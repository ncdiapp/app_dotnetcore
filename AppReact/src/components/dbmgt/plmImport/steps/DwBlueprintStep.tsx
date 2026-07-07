import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmDwImportBlueprintDto,
  PlmDwBlueprintPreviewItemDto,
} from '../../../../webapi/plmMigrationSvc';
import { refreshUserTreeMenu } from '../../../../helper/userMenuHelper';
import type { PlmImportDwBlueprintStepUiState, PlmImportWizardState } from '../types';
import { buildPlmImportStepStateJson } from '../types';

export type DwBlueprintStepProps = {
  state: PlmImportWizardState;
  dwBlueprintStepUi: PlmImportDwBlueprintStepUiState;
  onStateChange: (patch: Partial<PlmImportWizardState>) => void;
  onDwBlueprintStepUiChange: (patch: Partial<PlmImportDwBlueprintStepUiState>) => void;
  onSessionSaved: () => void;
};

const DwBlueprintStep: React.FC<DwBlueprintStepProps> = ({
  state,
  dwBlueprintStepUi,
  onStateChange,
  onDwBlueprintStepUiChange,
  onSessionSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const {
    blueprint,
    blueprintFileName,
    previewItems,
    validationErrors,
    validationWarnings,
    lastExecuteResult,
    isValidating,
    isPreviewing,
    isExecuting,
  } = dwBlueprintStepUi;

  const [previewCv] = useState(() => new CollectionView<PlmDwBlueprintPreviewItemDto>([]));
  const [progressPercent, setProgressPercent] = useState(0);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);
  const [isRefreshingCaches, setIsRefreshingCaches] = useState(false);

  const sessionId = state.session?.SessionId ?? null;
  const tablePrefix = state.tablePrefix;
  const isBusy = isValidating || isPreviewing || isExecuting || isRefreshingCaches;

  const blueprintSummary = useMemo(() => {
    if (!blueprint) return null;
    const txCount = blueprint.Transactions?.filter(
      (t) => (t.ImportStatus ?? 'Ready') !== 'Skipped',
    ).length ?? 0;
    const groupName = blueprint.TransactionGroup?.Name ?? '—';
    const searchName = blueprint.SearchView?.Search?.Name ?? '—';
    return `${groupName} · ${txCount} transaction(s) · Search: ${searchName}`;
  }, [blueprint]);

  useEffect(() => {
    previewCv.sourceCollection = previewItems;
    previewCv.refresh();
  }, [previewCv, previewItems]);

  const saveStepState = useCallback(async (patch: Partial<PlmImportWizardState>) => {
    if (!sessionId) return;
    const merged = { ...state, ...patch };
    await plmMigrationSvc.saveImportSession({
      SessionId: sessionId,
      SessionGuid: state.session?.SessionGuid ?? null,
      CompanyId: state.targetCompanyId,
      SaasApplicationId: state.saasApplicationId,
      CurrentStepCode: 'DwBlueprint',
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

  const applyLoadedBlueprint = useCallback((
    dto: PlmDwImportBlueprintDto,
    sourceLabel: string,
    jsonText?: string | null,
  ) => {
    onDwBlueprintStepUiChange({
      blueprint: dto,
      blueprintFileName: sourceLabel,
      blueprintJsonText: jsonText ?? null,
      previewItems: [],
      validationErrors: [],
      validationWarnings: [],
      lastExecuteResult: null,
    });
    setProgressPercent(0);
    setProgressMessage(null);
    showInfo(`Loaded blueprint: ${sourceLabel}`, true);
  }, [onDwBlueprintStepUiChange, showInfo]);

  const handleFileChange = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const loadResult = await plmMigrationSvc.loadDwImportBlueprint(text, tablePrefix);
      if (!loadResult.IsSuccessful || !loadResult.Object) {
        const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Failed to parse blueprint JSON.';
        showError(msg);
        return;
      }
      applyLoadedBlueprint(loadResult.Object, file.name, text);
    } catch (err: any) {
      showError(err?.message || 'Failed to read blueprint file.');
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }, [applyLoadedBlueprint, showError, tablePrefix]);

  const requireBlueprint = useCallback((): PlmDwImportBlueprintDto | null => {
    if (!blueprint) {
      showError('Upload PlmDw_ImportBlueprint.json first.');
      return null;
    }
    return blueprint;
  }, [blueprint, showError]);

  const runValidateAndPreview = useCallback(async () => {
    const bp = requireBlueprint();
    if (!bp) return;

    onDwBlueprintStepUiChange({
      isValidating: true,
      validationErrors: [],
      validationWarnings: [],
      previewItems: [],
      lastExecuteResult: null,
    });
    setProgressPercent(10);
    setProgressMessage('Validating blueprint…');

    try {
      const validateResult = await plmMigrationSvc.validateDwImportBlueprint(bp);
      const validation = validateResult.Object;
      const errors = validation?.Errors ?? [];
      const warnings = validation?.Warnings ?? [];
      onDwBlueprintStepUiChange({ validationErrors: errors, validationWarnings: warnings });

      if (errors.length > 0) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(errors.join('; '));
        return;
      }
      if (warnings.length > 0) {
        showWarning(`${warnings.length} warning(s). Review before execute.`);
      }

      onDwBlueprintStepUiChange({ isPreviewing: true });
      setProgressPercent(45);
      setProgressMessage('Building preview…');
      const previewResult = await plmMigrationSvc.previewDwBlueprintConfig(bp);
      const preview = previewResult.Object;
      if (!preview?.IsSuccess) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(preview?.ErrorMessage || 'Preview failed.');
        return;
      }
      onDwBlueprintStepUiChange({ previewItems: preview.Items ?? [] });
      setProgressPercent(100);
      setProgressMessage('Validate & preview complete.');
      if (!warnings.length) {
        showInfo('Blueprint validated. Preview list updated.', true);
      }
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Validate failed.');
    } finally {
      onDwBlueprintStepUiChange({ isValidating: false, isPreviewing: false });
    }
  }, [onDwBlueprintStepUiChange, requireBlueprint, showError, showInfo, showWarning]);

  const runExecute = useCallback(async () => {
    const bp = requireBlueprint();
    if (!bp) return;

    if (validationErrors.length > 0) {
      showError('Fix validation errors before execute.');
      return;
    }

    onDwBlueprintStepUiChange({ isExecuting: true, lastExecuteResult: null });
    setProgressPercent(15);
    setProgressMessage('Executing blueprint…');
    try {
      const result = await plmMigrationSvc.executeDwBlueprintConfig({
        Blueprint: bp,
        SaasApplicationId: state.saasApplicationId,
        Mode: 'Insert',
        IncludeTransactionGroup: true,
        IncludeSearchView: true,
        IncludeNavigation: true,
      });

      const exec = result.Object;
      onDwBlueprintStepUiChange({ lastExecuteResult: exec ?? null });

      if (!result.IsSuccessful || !exec?.IsSuccess) {
        const msg = exec?.ErrorMessage
          || result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Execute failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        showError(msg);
        return;
      }

      onStateChange({ templatesComplete: true });
      if (!state.templatesComplete) {
        await saveStepState({ templatesComplete: true });
      }
      try {
        await refreshUserTreeMenu();
      } catch {
        // non-blocking
      }

      const inserted = exec.TransactionsInserted ?? 0;
      const updated = exec.TransactionsUpdated ?? 0;
      setProgressPercent(100);
      setProgressMessage(`Complete — inserted ${inserted}, updated ${updated}.`);
      showInfo(
        `DW Blueprint execute completed. Inserted: ${inserted}, Updated: ${updated}.`,
        true,
      );
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Execute failed.');
    } finally {
      onDwBlueprintStepUiChange({ isExecuting: false });
    }
  }, [
    onDwBlueprintStepUiChange,
    onStateChange,
    requireBlueprint,
    saveStepState,
    showError,
    showInfo,
    state.saasApplicationId,
    state.templatesComplete,
    validationErrors.length,
  ]);

  const runRefreshCaches = useCallback(async () => {
    const transactionIds = (previewItems ?? [])
      .filter((item) => item.ObjectType === 'Transaction' && (item.ExistingId ?? 0) > 0)
      .map((item) => item.ExistingId as number);
    setIsRefreshingCaches(true);
    setProgressPercent(20);
    setProgressMessage('Refreshing tenant schema and transaction caches…');
    try {
      const result = await plmMigrationSvc.refreshDwImportTenantCaches({
        TransactionIds: transactionIds,
        TableNames: ['Plm_Artwork_BOM_prod', 'Plm_Artwork_BOM_prodGrandColorway'],
      });
      if (!result.IsSuccessful || !result.Object) {
        const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Cache refresh failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        showError(msg);
        return;
      }
      setProgressPercent(100);
      setProgressMessage('Caches refreshed.');
      showInfo('Tenant schema and transaction caches refreshed. Retry opening the BOM form.', true);
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Cache refresh failed.');
    } finally {
      setIsRefreshingCaches(false);
    }
  }, [previewItems, showError, showInfo]);

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const showProgress = isBusy || (progressMessage != null && progressPercent > 0);

  return (
    <div className={`flex flex-col h-full overflow-hidden p-4 gap-3 ${theme.mainContentSection}`}>
      <h2 className={`text-sm font-semibold ${theme.label}`}>Step 3 — Transaction From DW Blueprint</h2>

      <div className="flex flex-wrap items-center gap-2">
        <input
          ref={fileInputRef}
          type="file"
          accept=".json,application/json"
          className="hidden"
          onChange={handleFileChange}
        />
        <button
          type="button"
          className={actionBtnClass(isBusy)}
          disabled={isBusy}
          onClick={() => fileInputRef.current?.click()}
        >
          <i className="fa-solid fa-upload mr-1" />
          Upload JSON
        </button>
        <button
          type="button"
          className={actionBtnClass(isBusy || !blueprint)}
          disabled={isBusy || !blueprint}
          onClick={runValidateAndPreview}
        >
          <i className={`fa-solid ${isValidating || isPreviewing ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
          Validate Preview
        </button>
        <button
          type="button"
          className={actionBtnClass(isBusy || !blueprint)}
          disabled={isBusy || !blueprint}
          onClick={runExecute}
        >
          <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-play'} mr-1`} />
          Execute
        </button>
        <button
          type="button"
          className={actionBtnClass(isBusy)}
          disabled={isBusy}
          onClick={runRefreshCaches}
          title="After running SQL step 6, refresh schema cache so dropped staging columns are not selected"
        >
          <i className={`fa-solid ${isRefreshingCaches ? 'fa-spinner fa-spin' : 'fa-rotate'} mr-1`} />
          Refresh Caches
        </button>
        {blueprintFileName && (
          <span className={`text-xs ${theme.menu_secondary}`}>
            <i className="fa-solid fa-file-code mr-1" />
            {blueprintFileName}
          </span>
        )}
        {blueprintSummary && (
          <span className={`text-xs ml-auto ${theme.menu_secondary}`}>{blueprintSummary}</span>
        )}
        {state.templatesComplete && (
          <span className={`text-xs ${blueprintSummary ? '' : 'ml-auto'} ${theme.label}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            Applied
          </span>
        )}
      </div>

      {showProgress && (
        <div className={`flex-none rounded border px-3 py-2 ${theme.inputBox}`}>
          <div className={`flex items-center justify-between gap-2 text-xs mb-1.5 ${theme.menu_secondary}`}>
            <div className="flex items-center gap-2 min-w-0">
              {isBusy && <i className="fa-solid fa-spinner fa-spin" />}
              {!isBusy && progressPercent >= 100 && (
                <i className="fa-solid fa-circle-check" />
              )}
              <span className="truncate">{progressMessage}</span>
            </div>
            <span className="flex-none">{progressPercent}%</span>
          </div>
          <div className={`h-1.5 w-full rounded overflow-hidden ${theme.mainContentSection}`}>
            <div
              className={`h-full transition-all duration-300 ${theme.button_default}`}
              style={{ width: `${Math.min(100, Math.max(0, progressPercent))}%` }}
            />
          </div>
        </div>
      )}

      {(validationErrors.length > 0 || validationWarnings.length > 0) && (
        <div className="flex flex-col gap-2 max-h-28 overflow-auto">
          {validationErrors.map((msg) => (
            <div key={`err-${msg}`} className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
              <i className="fa-solid fa-circle-xmark mr-1" />
              {msg}
            </div>
          ))}
          {validationWarnings.map((msg) => (
            <div key={`warn-${msg}`} className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.menu_secondary}`}>
              <i className="fa-solid fa-triangle-exclamation mr-1" />
              {msg}
            </div>
          ))}
        </div>
      )}

      {lastExecuteResult?.IsSuccess && !showProgress && (
        <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
          Last run: inserted {lastExecuteResult.TransactionsInserted ?? 0},
          {' '}
          updated {lastExecuteResult.TransactionsUpdated ?? 0}
          {lastExecuteResult.TransactionGroupId ? ` · Group #${lastExecuteResult.TransactionGroupId}` : ''}
          {lastExecuteResult.SearchId ? ` · Search #${lastExecuteResult.SearchId}` : ''}
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
          <FlexGridColumn header="Type" binding="ObjectType" width={120} />
          <FlexGridColumn header="Name" binding="Name" width="*" />
          <FlexGridColumn header="Integration Id" binding="IntegrationId" width={160} />
          <FlexGridColumn header="Action" binding="Action" width={90} />
          <FlexGridColumn header="Existing Id" binding="ExistingId" width={90} />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
    </div>
  );
};

export default DwBlueprintStep;
