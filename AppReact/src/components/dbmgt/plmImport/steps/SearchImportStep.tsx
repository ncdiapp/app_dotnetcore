import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import {
  plmMigrationSvc,
  PlmSearchImportBlueprintDto,
  PlmSearchImportPreviewItemDto,
} from '../../../../webapi/plmMigrationSvc';
import { refreshUserTreeMenu } from '../../../../helper/userMenuHelper';
import type { PlmImportSearchImportStepUiState, PlmImportWizardState } from '../types';
import { buildPlmImportStepStateJson } from '../types';

export type SearchImportStepProps = {
  state: PlmImportWizardState;
  searchImportStepUi: PlmImportSearchImportStepUiState;
  onSearchImportStepUiChange: (patch: Partial<PlmImportSearchImportStepUiState>) => void;
  onSessionSaved: () => void;
};

const SearchImportStep: React.FC<SearchImportStepProps> = ({
  state,
  searchImportStepUi,
  onSearchImportStepUiChange,
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
  } = searchImportStepUi;

  const [previewCv] = useState(() => new CollectionView<PlmSearchImportPreviewItemDto>([]));
  const [progressPercent, setProgressPercent] = useState(0);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);

  const sessionId = state.session?.SessionId ?? null;
  const isBusy = isValidating || isPreviewing || isExecuting;

  const blueprintSummary = useMemo(() => {
    if (!blueprint) return null;
    const searchName = blueprint.Search?.Name ?? '—';
    const integrationId = blueprint.Search?.IntegrationId ?? '—';
    const criteria = blueprint.Coverage?.Criteria;
    const view = blueprint.Coverage?.View;
    const criteriaText = criteria
      ? `${criteria.Mapped ?? 0}/${criteria.Total ?? 0} criteria`
      : `${blueprint.CriteriaFields?.length ?? 0} criteria`;
    const viewText = view
      ? `${view.Mapped ?? 0}/${view.Total ?? 0} view cols`
      : `${blueprint.SearchView?.Fields?.length ?? 0} view cols`;
    return `${searchName} (${integrationId}) · ${criteriaText} · ${viewText}`;
  }, [blueprint]);

  useEffect(() => {
    previewCv.sourceCollection = previewItems;
    previewCv.refresh();
  }, [previewCv, previewItems]);

  const saveStepState = useCallback(async () => {
    if (!sessionId) return;
    await plmMigrationSvc.saveImportSession({
      SessionId: sessionId,
      SessionGuid: state.session?.SessionGuid ?? null,
      CompanyId: state.targetCompanyId,
      SaasApplicationId: state.saasApplicationId,
      CurrentStepCode: 'SearchImport',
      PlmConnectionString: state.plmConnectionString || undefined,
      StepStateJson: buildPlmImportStepStateJson({
        connectionTested: state.connectionTested,
        systemDefineTablesComplete: state.systemDefineTablesComplete,
        systemDefineEntitiesComplete: state.systemDefineEntitiesComplete,
        userDefineEntitiesComplete: state.userDefineEntitiesComplete,
        templatesComplete: state.templatesComplete,
        tablePrefix: state.tablePrefix,
      }),
      DataSourceDiscoveryJson: state.session?.DataSourceDiscoveryJson ?? undefined,
    });
    onSessionSaved();
  }, [onSessionSaved, sessionId, state]);

  const applyLoadedBlueprint = useCallback((
    dto: PlmSearchImportBlueprintDto,
    sourceLabel: string,
    jsonText?: string | null,
  ) => {
    onSearchImportStepUiChange({
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
    showInfo(`Loaded search blueprint: ${sourceLabel}`, true);
  }, [onSearchImportStepUiChange, showInfo]);

  const handleFileChange = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const loadResult = await plmMigrationSvc.loadSearchImportBlueprint(text);
      if (!loadResult.IsSuccessful || !loadResult.Object) {
        const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Failed to parse search blueprint JSON.';
        showError(msg);
        return;
      }
      applyLoadedBlueprint(loadResult.Object, file.name, text);
    } catch (err: any) {
      showError(err?.message || 'Failed to read blueprint file.');
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }, [applyLoadedBlueprint, showError]);

  const requireBlueprint = useCallback((): PlmSearchImportBlueprintDto | null => {
    if (!blueprint) {
      showError('Upload PlmSearch_ImportBlueprint.json first.');
      return null;
    }
    return blueprint;
  }, [blueprint, showError]);

  const runValidateAndPreview = useCallback(async () => {
    const bp = requireBlueprint();
    if (!bp) return;

    onSearchImportStepUiChange({
      isValidating: true,
      validationErrors: [],
      validationWarnings: [],
      previewItems: [],
      lastExecuteResult: null,
    });
    setProgressPercent(10);
    setProgressMessage('Validating search blueprint…');

    try {
      const validateResult = await plmMigrationSvc.validateSearchImportBlueprint(bp);
      const validation = validateResult.Object;
      const errors = validation?.Errors ?? [];
      const warnings = validation?.Warnings ?? [];
      onSearchImportStepUiChange({ validationErrors: errors, validationWarnings: warnings });

      if (errors.length > 0) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(errors.join('; '));
        return;
      }
      if (warnings.length > 0) {
        showWarning(`${warnings.length} warning(s). Review before execute.`);
      }

      onSearchImportStepUiChange({ isPreviewing: true });
      setProgressPercent(45);
      setProgressMessage('Building preview…');
      const previewResult = await plmMigrationSvc.previewSearchBlueprintConfig(bp);
      const preview = previewResult.Object;
      if (!preview?.IsSuccess) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(preview?.ErrorMessage || 'Preview failed.');
        return;
      }
      onSearchImportStepUiChange({ previewItems: preview.Items ?? [] });
      setProgressPercent(100);
      setProgressMessage('Validate & preview complete.');
      if (!warnings.length) {
        showInfo('Search blueprint validated. Preview list updated.', true);
      }
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Validate failed.');
    } finally {
      onSearchImportStepUiChange({ isValidating: false, isPreviewing: false });
    }
  }, [onSearchImportStepUiChange, requireBlueprint, showError, showInfo, showWarning]);

  const runExecute = useCallback(async () => {
    const bp = requireBlueprint();
    if (!bp) return;

    if (validationErrors.length > 0) {
      showError('Fix validation errors before execute.');
      return;
    }

    onSearchImportStepUiChange({ isExecuting: true, lastExecuteResult: null });
    setProgressPercent(15);
    setProgressMessage('Executing search blueprint…');
    try {
      const result = await plmMigrationSvc.executeSearchBlueprintConfig({
        Blueprint: bp,
        SaasApplicationId: state.saasApplicationId,
      });

      const exec = result.Object;
      onSearchImportStepUiChange({ lastExecuteResult: exec ?? null });

      if (!result.IsSuccessful || !exec?.IsSuccess) {
        const msg = exec?.ErrorMessage
          || result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Execute failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        showError(msg);
        return;
      }

      await saveStepState();
      if (bp.Menu?.RegisterInMainMenu) {
        try {
          await refreshUserTreeMenu();
        } catch {
          // non-blocking
        }
      }

      setProgressPercent(100);
      setProgressMessage('Search import complete.');
      const messages = exec.Messages?.length
        ? ` ${exec.Messages.join(' ')}`
        : '';
      showInfo(
        `Search import completed. Search #${exec.SearchId ?? '—'}, View #${exec.SearchViewId ?? '—'}.${messages}`,
        true,
      );
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Execute failed.');
    } finally {
      onSearchImportStepUiChange({ isExecuting: false });
    }
  }, [
    onSearchImportStepUiChange,
    requireBlueprint,
    saveStepState,
    showError,
    showInfo,
    state.saasApplicationId,
    validationErrors.length,
  ]);

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const showProgress = isBusy || (progressMessage != null && progressPercent > 0);

  return (
    <div className={`flex flex-col h-full overflow-hidden p-4 gap-3 ${theme.mainContentSection}`}>
      <h2 className={`text-sm font-semibold ${theme.label}`}>Step 4 — PLM Search Import</h2>
      <p className={`text-xs ${theme.menu_secondary}`}>
        Upload <code className="text-[11px]">PlmSearch_ImportBlueprint.json</code> from ImportPLMSearchView output.
        Creates search shell, criteria, grid view, link targets, and optional main menu entry.
      </p>

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
        {blueprintFileName && (
          <span className={`text-xs ${theme.menu_secondary}`}>
            <i className="fa-solid fa-file-code mr-1" />
            {blueprintFileName}
          </span>
        )}
        {blueprintSummary && (
          <span className={`text-xs ml-auto ${theme.menu_secondary}`}>{blueprintSummary}</span>
        )}
        {lastExecuteResult?.IsSuccess && (
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
          Last run: Search #{lastExecuteResult.SearchId ?? '—'}
          {lastExecuteResult.SearchViewId ? ` · View #${lastExecuteResult.SearchViewId}` : ''}
          {lastExecuteResult.DataSetId ? ` · DataSet #${lastExecuteResult.DataSetId}` : ''}
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
          <FlexGridColumn header="Detail" binding="Detail" width={180} />
          <FlexGridColumn header="" binding="" width="*" />
        </FlexGrid>
      </div>
    </div>
  );
};

export default SearchImportStep;
