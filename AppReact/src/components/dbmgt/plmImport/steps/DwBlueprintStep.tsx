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
    includeTransactionGroup,
    includeSearchView,
    includeNavigation,
  } = dwBlueprintStepUi;

  const [previewCv] = useState(() => new CollectionView<PlmDwBlueprintPreviewItemDto>([]));

  const sessionId = state.session?.SessionId ?? null;
  const tablePrefix = state.tablePrefix;
  const isBusy = isValidating || isPreviewing || isExecuting;

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

  const handleLoadFromTenant = useCallback(async () => {
    try {
      const loadResult = await plmMigrationSvc.loadDwImportBlueprintFromTable(tablePrefix);
      if (!loadResult.IsSuccessful || !loadResult.Object) {
        const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Blueprint not found in tenant table. Run PlmDw_ImportBlueprint.sql first.';
        showError(msg);
        return;
      }
      applyLoadedBlueprint(
        loadResult.Object,
        `${tablePrefix}ImportBlueprint (default)`,
      );
    } catch (err: any) {
      showError(err?.message || 'Failed to load blueprint from tenant database.');
    }
  }, [applyLoadedBlueprint, showError, tablePrefix]);

  const requireBlueprint = useCallback((): PlmDwImportBlueprintDto | null => {
    if (!blueprint) {
      showError('Upload or load PlmDw_ImportBlueprint.json first.');
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

    try {
      const validateResult = await plmMigrationSvc.validateDwImportBlueprint(bp);
      const validation = validateResult.Object;
      const errors = validation?.Errors ?? [];
      const warnings = validation?.Warnings ?? [];
      onDwBlueprintStepUiChange({ validationErrors: errors, validationWarnings: warnings });

      if (errors.length > 0) {
        showError(errors.join('; '));
        return;
      }
      if (warnings.length > 0) {
        showWarning(`${warnings.length} warning(s). Review before execute.`);
      }

      onDwBlueprintStepUiChange({ isPreviewing: true });
      const previewResult = await plmMigrationSvc.previewDwBlueprintConfig(bp);
      const preview = previewResult.Object;
      if (!preview?.IsSuccess) {
        showError(preview?.ErrorMessage || 'Preview failed.');
        return;
      }
      onDwBlueprintStepUiChange({ previewItems: preview.Items ?? [] });
      if (!warnings.length) {
        showInfo('Blueprint validated. Preview list updated.', true);
      }
    } catch (err: any) {
      showError(err?.message || 'Validate failed.');
    } finally {
      onDwBlueprintStepUiChange({ isValidating: false, isPreviewing: false });
    }
  }, [onDwBlueprintStepUiChange, requireBlueprint, showError, showInfo, showWarning]);

  const runExecute = useCallback(async (mode: 'Insert' | 'Update') => {
    const bp = requireBlueprint();
    if (!bp) return;

    if (validationErrors.length > 0) {
      showError('Fix validation errors before execute.');
      return;
    }

    onDwBlueprintStepUiChange({ isExecuting: true, lastExecuteResult: null });
    try {
      const result = await plmMigrationSvc.executeDwBlueprintConfig({
        Blueprint: bp,
        SaasApplicationId: state.saasApplicationId,
        Mode: mode,
        IncludeTransactionGroup: includeTransactionGroup,
        IncludeSearchView: includeSearchView,
        IncludeNavigation: includeNavigation,
      });

      const exec = result.Object;
      onDwBlueprintStepUiChange({ lastExecuteResult: exec ?? null });

      if (!result.IsSuccessful || !exec?.IsSuccess) {
        const msg = exec?.ErrorMessage
          || result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Execute failed.';
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
      showInfo(
        `DW Blueprint execute (${mode}) completed. Inserted: ${inserted}, Updated: ${updated}.`,
        true,
      );
    } catch (err: any) {
      showError(err?.message || 'Execute failed.');
    } finally {
      onDwBlueprintStepUiChange({ isExecuting: false });
    }
  }, [
    includeNavigation,
    includeSearchView,
    includeTransactionGroup,
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

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const secondaryBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  return (
    <div className={`flex flex-col h-full overflow-hidden p-4 gap-3 ${theme.mainContentSection}`}>
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 3 — DW Blueprint</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Phase D: apply
          {' '}
          <span className="font-mono">PlmDw_ImportBlueprint.json</span>
          {' '}
          after tenant tables, field mapping, and data import (Phase C) are in place.
        </p>
      </div>

      <div className={`rounded border p-3 flex flex-wrap items-end gap-3 ${theme.inputBox}`}>
        <div className="flex flex-col gap-1">
          <span className={`text-xs ${theme.label}`}>Blueprint JSON file</span>
          <input
            ref={fileInputRef}
            type="file"
            accept=".json,application/json"
            className="hidden"
            onChange={handleFileChange}
          />
          <button
            type="button"
            className={secondaryBtnClass(isBusy)}
            disabled={isBusy}
            onClick={() => fileInputRef.current?.click()}
          >
            <i className="fa-solid fa-upload mr-1" />
            Upload JSON
          </button>
        </div>
        <button
          type="button"
          className={secondaryBtnClass(isBusy)}
          disabled={isBusy}
          onClick={handleLoadFromTenant}
          title={`Load from dbo.${tablePrefix}ImportBlueprint`}
        >
          <i className="fa-solid fa-database mr-1" />
          Load from tenant DB
        </button>
        {blueprintFileName && (
          <span className={`text-xs ${theme.menu_secondary}`}>
            <i className="fa-solid fa-file-code mr-1" />
            {blueprintFileName}
          </span>
        )}
        {blueprintSummary && (
          <span className={`text-xs ml-auto ${theme.label}`}>{blueprintSummary}</span>
        )}
      </div>

      <div className={`flex flex-wrap items-center gap-4 text-xs ${theme.label}`}>
        <label className="inline-flex items-center gap-1.5 cursor-pointer">
          <input
            type="checkbox"
            checked={includeTransactionGroup}
            disabled={isBusy}
            onChange={(e) => onDwBlueprintStepUiChange({ includeTransactionGroup: e.target.checked })}
          />
          Transaction Group
        </label>
        <label className="inline-flex items-center gap-1.5 cursor-pointer">
          <input
            type="checkbox"
            checked={includeSearchView}
            disabled={isBusy}
            onChange={(e) => onDwBlueprintStepUiChange({ includeSearchView: e.target.checked })}
          />
          Search / View
        </label>
        <label className="inline-flex items-center gap-1.5 cursor-pointer">
          <input
            type="checkbox"
            checked={includeNavigation}
            disabled={isBusy}
            onChange={(e) => onDwBlueprintStepUiChange({ includeNavigation: e.target.checked })}
          />
          Main menu navigation
        </label>
        {state.templatesComplete && (
          <span className={`ml-auto text-xs ${theme.label}`}>
            <i className="fa-solid fa-circle-check mr-1" />
            Blueprint config applied
          </span>
        )}
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          className={actionBtnClass(isBusy || !blueprint)}
          disabled={isBusy || !blueprint}
          onClick={runValidateAndPreview}
        >
          <i className={`fa-solid ${isValidating || isPreviewing ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
          Validate &amp; Preview
        </button>
        <button
          type="button"
          className={actionBtnClass(isBusy || !blueprint)}
          disabled={isBusy || !blueprint}
          onClick={() => runExecute('Insert')}
        >
          <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-plus'} mr-1`} />
          Execute Insert
        </button>
        <button
          type="button"
          className={secondaryBtnClass(isBusy || !blueprint)}
          disabled={isBusy || !blueprint}
          onClick={() => runExecute('Update')}
        >
          <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-rotate'} mr-1`} />
          Execute Update
        </button>
      </div>

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

      {lastExecuteResult?.IsSuccess && (
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
