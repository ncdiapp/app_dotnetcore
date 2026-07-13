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

const isSiblingModeJson = (text: string): boolean => {
  try {
    const parsed = JSON.parse(text);
    const mode = String(parsed?.mode ?? parsed?.Mode ?? '');
    return mode.toLowerCase() === 'siblingviewenrichdataset';
  } catch {
    return false;
  }
};

const isMassUpdateModeJson = (text: string): boolean => {
  try {
    const parsed = JSON.parse(text);
    const mode = String(parsed?.mode ?? parsed?.Mode ?? '');
    return mode.toLowerCase() === 'massupdateviewattach';
  } catch {
    return false;
  }
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
    importMode,
    blueprint,
    siblingBlueprint,
    massUpdateBlueprint,
    blueprintFileName,
    previewItems,
    validationErrors,
    validationWarnings,
    lastExecuteResult,
    lastSiblingExecuteResult,
    lastMassUpdateExecuteResult,
    isValidating,
    isPreviewing,
    isExecuting,
  } = searchImportStepUi;

  const [previewCv] = useState(() => new CollectionView<PlmSearchImportPreviewItemDto>([]));
  const [progressPercent, setProgressPercent] = useState(0);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);

  const sessionId = state.session?.SessionId ?? null;
  const isBusy = isValidating || isPreviewing || isExecuting;
  const isSibling = importMode === 'sibling';
  const isMassUpdate = importMode === 'massUpdate';
  const hasBlueprint = isMassUpdate
    ? Boolean(massUpdateBlueprint)
    : isSibling
      ? Boolean(siblingBlueprint)
      : Boolean(blueprint);

  const blueprintSummary = useMemo(() => {
    if (isMassUpdate && massUpdateBlueprint) {
      const viewName = massUpdateBlueprint.SearchView?.Name ?? '—';
      const target = massUpdateBlueprint.Target?.AppSearchIntegrationId ?? '—';
      const muId = massUpdateBlueprint.Source?.PlmMassUpdateViewId ?? '—';
      const appMode = massUpdateBlueprint.MassUpdate?.AppMode ?? '—';
      const leAction = massUpdateBlueprint.ListEditCreate?.Action ?? '';
      return `Mass Update · target ${target} · "${viewName}" (PLM MU #${muId}) · ${appMode}${leAction ? ` · ListEdit ${leAction}` : ''}`;
    }
    if (isSibling && siblingBlueprint) {
      const viewName = siblingBlueprint.SearchView?.Name ?? '—';
      const target = siblingBlueprint.Target?.AppSearchIntegrationId ?? '—';
      const plmView = siblingBlueprint.Source?.PlmReferenceViewId ?? '—';
      return `Sibling · target ${target} · View "${viewName}" (PLM #${plmView})`;
    }
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
  }, [blueprint, isMassUpdate, isSibling, massUpdateBlueprint, siblingBlueprint]);

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

  const handleFileChange = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const massUpdate = isMassUpdateModeJson(text);
      const sibling = !massUpdate && isSiblingModeJson(text);

      if (massUpdate) {
        const loadResult = await plmMigrationSvc.loadSearchMassUpdateViewBlueprint(text);
        if (!loadResult.IsSuccessful || !loadResult.Object) {
          const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
            || 'Failed to parse mass update view blueprint JSON.';
          showError(msg);
          return;
        }
        onSearchImportStepUiChange({
          importMode: 'massUpdate',
          massUpdateBlueprint: loadResult.Object,
          siblingBlueprint: null,
          blueprint: null,
          blueprintFileName: file.name,
          blueprintJsonText: text,
          previewItems: [],
          validationErrors: [],
          validationWarnings: [],
          lastExecuteResult: null,
          lastSiblingExecuteResult: null,
          lastMassUpdateExecuteResult: null,
        });
        setProgressPercent(0);
        setProgressMessage(null);
        showInfo(`Loaded mass update view blueprint: ${file.name}`, true);
      } else if (sibling) {
        const loadResult = await plmMigrationSvc.loadSearchSiblingViewBlueprint(text);
        if (!loadResult.IsSuccessful || !loadResult.Object) {
          const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
            || 'Failed to parse sibling view blueprint JSON.';
          showError(msg);
          return;
        }
        onSearchImportStepUiChange({
          importMode: 'sibling',
          siblingBlueprint: loadResult.Object,
          massUpdateBlueprint: null,
          blueprint: null,
          blueprintFileName: file.name,
          blueprintJsonText: text,
          previewItems: [],
          validationErrors: [],
          validationWarnings: [],
          lastExecuteResult: null,
          lastSiblingExecuteResult: null,
          lastMassUpdateExecuteResult: null,
        });
        setProgressPercent(0);
        setProgressMessage(null);
        showInfo(`Loaded sibling view blueprint: ${file.name}`, true);
      } else {
        const loadResult = await plmMigrationSvc.loadSearchImportBlueprint(text);
        if (!loadResult.IsSuccessful || !loadResult.Object) {
          const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
            || 'Failed to parse search blueprint JSON.';
          showError(msg);
          return;
        }
        onSearchImportStepUiChange({
          importMode: 'main',
          blueprint: loadResult.Object as PlmSearchImportBlueprintDto,
          siblingBlueprint: null,
          massUpdateBlueprint: null,
          blueprintFileName: file.name,
          blueprintJsonText: text,
          previewItems: [],
          validationErrors: [],
          validationWarnings: [],
          lastExecuteResult: null,
          lastSiblingExecuteResult: null,
          lastMassUpdateExecuteResult: null,
        });
        setProgressPercent(0);
        setProgressMessage(null);
        showInfo(`Loaded search blueprint: ${file.name}`, true);
      }
    } catch (err: any) {
      showError(err?.message || 'Failed to read blueprint file.');
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }, [onSearchImportStepUiChange, showError, showInfo]);

  const runValidateAndPreview = useCallback(async () => {
    if (isMassUpdate) {
      if (!massUpdateBlueprint) {
        showError('Upload 3_PlmSearch_MassUpdateView_*.json first.');
        return;
      }
      onSearchImportStepUiChange({
        isValidating: true,
        validationErrors: [],
        validationWarnings: [],
        previewItems: [],
        lastMassUpdateExecuteResult: null,
      });
      setProgressPercent(10);
      setProgressMessage('Validating mass update view blueprint…');
      try {
        const validateResult = await plmMigrationSvc.validateSearchMassUpdateViewBlueprint(massUpdateBlueprint);
        const errors = validateResult.Object?.Errors ?? [];
        const warnings = validateResult.Object?.Warnings ?? [];
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
        setProgressMessage('Building mass update preview…');
        const previewResult = await plmMigrationSvc.previewSearchMassUpdateViewConfig(massUpdateBlueprint);
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
          showInfo('Mass update view blueprint validated.', true);
        }
      } catch (err: any) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(err?.message || 'Validate failed.');
      } finally {
        onSearchImportStepUiChange({ isValidating: false, isPreviewing: false });
      }
      return;
    }

    if (isSibling) {
      if (!siblingBlueprint) {
        showError('Upload 2_PlmSearch_SiblingView_*.json first.');
        return;
      }
      onSearchImportStepUiChange({
        isValidating: true,
        validationErrors: [],
        validationWarnings: [],
        previewItems: [],
        lastSiblingExecuteResult: null,
      });
      setProgressPercent(10);
      setProgressMessage('Validating sibling view blueprint…');
      try {
        const validateResult = await plmMigrationSvc.validateSearchSiblingViewBlueprint(siblingBlueprint);
        const errors = validateResult.Object?.Errors ?? [];
        const warnings = validateResult.Object?.Warnings ?? [];
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
        setProgressMessage('Building sibling preview…');
        const previewResult = await plmMigrationSvc.previewSearchSiblingViewConfig(siblingBlueprint);
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
          showInfo('Sibling view blueprint validated.', true);
        }
      } catch (err: any) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(err?.message || 'Validate failed.');
      } finally {
        onSearchImportStepUiChange({ isValidating: false, isPreviewing: false });
      }
      return;
    }

    if (!blueprint) {
      showError('Upload PlmSearch_ImportBlueprint.json first.');
      return;
    }

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
      const validateResult = await plmMigrationSvc.validateSearchImportBlueprint(blueprint);
      const errors = validateResult.Object?.Errors ?? [];
      const warnings = validateResult.Object?.Warnings ?? [];
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
      const previewResult = await plmMigrationSvc.previewSearchBlueprintConfig(blueprint);
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
  }, [
    blueprint,
    isMassUpdate,
    isSibling,
    massUpdateBlueprint,
    onSearchImportStepUiChange,
    showError,
    showInfo,
    showWarning,
    siblingBlueprint,
  ]);

  const runExecute = useCallback(async () => {
    if (validationErrors.length > 0) {
      showError('Fix validation errors before execute.');
      return;
    }

    if (isMassUpdate) {
      if (!massUpdateBlueprint) {
        showError('Upload mass update view blueprint first.');
        return;
      }
      onSearchImportStepUiChange({ isExecuting: true, lastMassUpdateExecuteResult: null });
      setProgressPercent(15);
      setProgressMessage('Executing mass update view import…');
      try {
        const result = await plmMigrationSvc.executeSearchMassUpdateViewConfig({
          Blueprint: massUpdateBlueprint,
          SaasApplicationId: state.saasApplicationId,
        });
        const exec = result.Object;
        onSearchImportStepUiChange({ lastMassUpdateExecuteResult: exec ?? null });
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
        setProgressPercent(100);
        setProgressMessage('Mass update view import complete.');
        const listPart = exec.ListEditTransactionId
          ? `, ListEdit #${exec.ListEditTransactionId}`
          : '';
        showInfo(
          `Mass Update View saved. Search #${exec.SearchId ?? '—'}, MU View #${exec.MassUpdateSearchViewId ?? '—'}, DataSet #${exec.DataSetId ?? '—'}${listPart}.`,
          true,
        );
      } catch (err: any) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(err?.message || 'Execute failed.');
      } finally {
        onSearchImportStepUiChange({ isExecuting: false });
      }
      return;
    }

    if (isSibling) {
      if (!siblingBlueprint) {
        showError('Upload sibling view blueprint first.');
        return;
      }
      onSearchImportStepUiChange({ isExecuting: true, lastSiblingExecuteResult: null });
      setProgressPercent(15);
      setProgressMessage('Executing sibling view import…');
      try {
        const result = await plmMigrationSvc.executeSearchSiblingViewConfig({
          Blueprint: siblingBlueprint,
          SaasApplicationId: state.saasApplicationId,
        });
        const exec = result.Object;
        onSearchImportStepUiChange({ lastSiblingExecuteResult: exec ?? null });
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
        setProgressPercent(100);
        setProgressMessage('Sibling view import complete.');
        showInfo(
          `Sibling view saved. Search #${exec.SearchId ?? '—'}, Sibling View #${exec.SiblingSearchViewId ?? '—'}, DataSet #${exec.DataSetId ?? '—'}.`,
          true,
        );
      } catch (err: any) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(err?.message || 'Execute failed.');
      } finally {
        onSearchImportStepUiChange({ isExecuting: false });
      }
      return;
    }

    if (!blueprint) {
      showError('Upload PlmSearch_ImportBlueprint.json first.');
      return;
    }

    onSearchImportStepUiChange({ isExecuting: true, lastExecuteResult: null });
    setProgressPercent(15);
    setProgressMessage('Executing search blueprint…');
    try {
      const result = await plmMigrationSvc.executeSearchBlueprintConfig({
        Blueprint: blueprint,
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
      if (blueprint.Menu?.RegisterInMainMenu) {
        try {
          await refreshUserTreeMenu();
        } catch {
          // non-blocking
        }
      }

      setProgressPercent(100);
      setProgressMessage('Search import complete.');
      showInfo(
        `Search import completed. Search #${exec.SearchId ?? '—'}, View #${exec.SearchViewId ?? '—'}.`,
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
    blueprint,
    isMassUpdate,
    isSibling,
    massUpdateBlueprint,
    onSearchImportStepUiChange,
    saveStepState,
    showError,
    showInfo,
    siblingBlueprint,
    state.saasApplicationId,
    validationErrors.length,
  ]);

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const showProgress = isBusy || (progressMessage != null && progressPercent > 0);
  const appliedOk = isMassUpdate
    ? Boolean(lastMassUpdateExecuteResult?.IsSuccess)
    : isSibling
      ? Boolean(lastSiblingExecuteResult?.IsSuccess)
      : Boolean(lastExecuteResult?.IsSuccess);

  const modeLabel = isMassUpdate
    ? 'Mass Update View (attach / ListEdit)'
    : isSibling
      ? 'Sibling View (enrich DataSet)'
      : 'Main Search import';

  return (
    <div className={`flex flex-col h-full overflow-hidden p-4 gap-3 ${theme.mainContentSection}`}>
      <h2 className={`text-sm font-semibold ${theme.label}`}>Step 4 — PLM Search Import</h2>
      <p className={`text-xs ${theme.menu_secondary}`}>
        Upload main <code className="text-[11px]">1_PlmSearch_ImportBlueprint.json</code>
        {', '}sibling <code className="text-[11px]">2_PlmSearch_SiblingView_*.json</code>
        {', or mass update '}
        <code className="text-[11px]">3_PlmSearch_MassUpdateView_*.json</code>
        {' '}(mode auto-detected). Mass Update may create a ListEdit Transaction then attach an IsMassUpdateView SearchView.
      </p>
      {hasBlueprint && (
        <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
          Mode: {modeLabel}
        </div>
      )}

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
          className={actionBtnClass(isBusy || !hasBlueprint)}
          disabled={isBusy || !hasBlueprint}
          onClick={runValidateAndPreview}
        >
          <i className={`fa-solid ${isValidating || isPreviewing ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
          Validate Preview
        </button>
        <button
          type="button"
          className={actionBtnClass(isBusy || !hasBlueprint)}
          disabled={isBusy || !hasBlueprint}
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
        {appliedOk && (
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

      {!isSibling && !isMassUpdate && lastExecuteResult?.IsSuccess && !showProgress && (
        <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
          Last run: Search #{lastExecuteResult.SearchId ?? '—'}
          {lastExecuteResult.SearchViewId ? ` · View #${lastExecuteResult.SearchViewId}` : ''}
          {lastExecuteResult.DataSetId ? ` · DataSet #${lastExecuteResult.DataSetId}` : ''}
        </div>
      )}

      {isSibling && lastSiblingExecuteResult?.IsSuccess && !showProgress && (
        <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
          Last sibling run: Search #{lastSiblingExecuteResult.SearchId ?? '—'}
          {lastSiblingExecuteResult.SiblingSearchViewId
            ? ` · Sibling View #${lastSiblingExecuteResult.SiblingSearchViewId}`
            : ''}
          {lastSiblingExecuteResult.DefaultSearchViewId
            ? ` · Default View #${lastSiblingExecuteResult.DefaultSearchViewId}`
            : ''}
          {lastSiblingExecuteResult.DataSetId ? ` · DataSet #${lastSiblingExecuteResult.DataSetId}` : ''}
        </div>
      )}

      {isMassUpdate && lastMassUpdateExecuteResult?.IsSuccess && !showProgress && (
        <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
          Last mass-update run: Search #{lastMassUpdateExecuteResult.SearchId ?? '—'}
          {lastMassUpdateExecuteResult.MassUpdateSearchViewId
            ? ` · MU View #${lastMassUpdateExecuteResult.MassUpdateSearchViewId}`
            : ''}
          {lastMassUpdateExecuteResult.ListEditTransactionId
            ? ` · ListEdit #${lastMassUpdateExecuteResult.ListEditTransactionId}`
            : ''}
          {lastMassUpdateExecuteResult.DataSetId
            ? ` · DataSet #${lastMassUpdateExecuteResult.DataSetId}`
            : ''}
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
