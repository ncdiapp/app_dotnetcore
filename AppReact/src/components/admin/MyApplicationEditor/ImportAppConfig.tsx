import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, DataType } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { refreshUserTreeMenu } from '../../../helper/userMenuHelper';
import {
  appConfigPackSvc,
  AppConfigPackExecuteResultDto,
  AppConfigPackPreviewItemDto
} from '../../../webapi/appConfigPackSvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import appHelper from '../../../helper/appHelper';

interface ImportAppConfigProps {
  menuId: string | null;
}

type PageMode = 'import' | 'export';

interface ExportCandidate {
  Id: number;
  Name: string;
  Kind: string;
  Selected: boolean;
}

const buildExportSelectionSummary = (rows: ExportCandidate[]): string => {
  const txTotal = rows.filter((r) => r.Kind === 'Transaction').length;
  const searchTotal = rows.filter((r) => r.Kind === 'Search').length;
  const tx = rows.filter((r) => r.Kind === 'Transaction' && r.Selected).length;
  const search = rows.filter((r) => r.Kind === 'Search' && r.Selected).length;
  return `${tx} of ${txTotal} transaction(s), ${search} of ${searchTotal} search(es)`;
};

const stripUnselectedSearchesFromPackJson = (jsonText: string, selectedSearchIds: number[]): string => {
  if (selectedSearchIds.length > 0) return jsonText;
  try {
    const pack = JSON.parse(jsonText);
    const searchKey = Array.isArray(pack.searches) ? 'searches' : (Array.isArray(pack.Searches) ? 'Searches' : null);
    if (searchKey && pack[searchKey].length > 0) {
      pack[searchKey] = [];
      return JSON.stringify(pack, null, 2);
    }
  } catch {
    return jsonText;
  }
  return jsonText;
};

const ImportAppConfig: React.FC<ImportAppConfigProps> = ({ menuId }) => {
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [mode, setMode] = useState<PageMode>('import');
  const [pack, setPack] = useState<any>(null);
  const [packFileName, setPackFileName] = useState<string | null>(null);
  const [previewItems, setPreviewItems] = useState<AppConfigPackPreviewItemDto[]>([]);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [validationWarnings, setValidationWarnings] = useState<string[]>([]);
  const [lastExecuteResult, setLastExecuteResult] = useState<AppConfigPackExecuteResultDto | null>(null);
  const [isValidating, setIsValidating] = useState(false);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [progressPercent, setProgressPercent] = useState(0);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);

  const [previewCv] = useState(() => new CollectionView<AppConfigPackPreviewItemDto>([]));
  const [exportCv] = useState(() => new CollectionView<ExportCandidate>([]));
  const [isSelectAllExportRows, setIsSelectAllExportRows] = useState(true);
  const [exportSelectionSummary, setExportSelectionSummary] = useState('');

  const saasApplicationId = useMemo(() => {
    const parsed = Number(menuId);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
  }, [menuId]);

  const isBusy = isValidating || isPreviewing || isExecuting || isExporting;

  const packSummary = useMemo(() => {
    if (!pack) return null;
    const tableCount = pack.Tables?.length ?? 0;
    const viewCount = pack.Views?.length ?? 0;
    const txCount = pack.Transactions?.length ?? 0;
    const searchCount = pack.Searches?.length ?? 0;
    return `${tableCount} table(s) · ${viewCount} view(s) · ${txCount} transaction(s) · ${searchCount} search(es)`;
  }, [pack]);

  useEffect(() => {
    previewCv.sourceCollection = previewItems;
    previewCv.refresh();
  }, [previewCv, previewItems]);

  const loadExportCandidates = useCallback(async () => {
    if (!menuId) return;
    try {
      const [txList, searchList] = await Promise.all([
        appTransactionService.retrieveSaasApplicationTransactionList(menuId),
        appTransactionService.retrieveSaasApplicationSearchList(menuId)
      ]);
      const rows: ExportCandidate[] = [];
      (Array.isArray(txList) ? txList : []).forEach((t: any) => {
        rows.push({
          Id: Number(t.Id),
          Name: t.TransactionName || t.Name || `Transaction ${t.Id}`,
          Kind: 'Transaction',
          Selected: true
        });
      });
      (Array.isArray(searchList) ? searchList : []).forEach((s: any) => {
        rows.push({
          Id: Number(s.Id),
          Name: s.Name || `Search ${s.Id}`,
          Kind: 'Search',
          Selected: true
        });
      });
      exportCv.sourceCollection = rows;
      exportCv.refresh();
      setIsSelectAllExportRows(rows.length > 0);
      setExportSelectionSummary(buildExportSelectionSummary(rows));
    } catch (err: any) {
      exportCv.sourceCollection = [];
      exportCv.refresh();
      showError(err?.message || 'Failed to load export candidates.');
    }
  }, [exportCv, menuId, showError]);

  useEffect(() => {
    if (mode === 'export') {
      loadExportCandidates();
    }
  }, [mode, loadExportCandidates]);

  const handleFileChange = useCallback(async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      const text = await file.text();
      const loadResult = await appConfigPackSvc.Load(text);
      if (!loadResult.IsSuccessful || !loadResult.Object) {
        const msg = loadResult.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Failed to parse App Config Pack JSON.';
        showError(msg);
        return;
      }
      setPack(loadResult.Object);
      setPackFileName(file.name);
      setPreviewItems([]);
      setValidationErrors([]);
      setValidationWarnings([]);
      setLastExecuteResult(null);
      setProgressPercent(0);
      setProgressMessage(null);
      showInfo(`Loaded pack: ${file.name}`, true);
    } catch (err: any) {
      showError(err?.message || 'Failed to read JSON file.');
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }, [showError, showInfo]);

  const runValidateAndPreview = useCallback(async () => {
    if (!pack) {
      showError('Upload an App Config Pack JSON file first.');
      return;
    }

    setIsValidating(true);
    setValidationErrors([]);
    setValidationWarnings([]);
    setPreviewItems([]);
    setLastExecuteResult(null);
    setProgressPercent(10);
    setProgressMessage('Validating pack…');

    try {
      const validateResult = await appConfigPackSvc.Validate(pack);
      const validation = validateResult.Object;
      const errors = validation?.Errors ?? [];
      const warnings = validation?.Warnings ?? [];
      setValidationErrors(errors);
      setValidationWarnings(warnings);

      if (errors.length > 0) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(errors.join('; '));
        return;
      }
      if (warnings.length > 0) {
        showWarning(`${warnings.length} warning(s). Review before execute.`);
      }

      setIsPreviewing(true);
      setProgressPercent(45);
      setProgressMessage('Building preview…');
      const previewResult = await appConfigPackSvc.Preview(pack, saasApplicationId);
      const preview = previewResult.Object;
      if (!preview?.IsSuccess) {
        setProgressPercent(0);
        setProgressMessage(null);
        showError(preview?.ErrorMessage || 'Preview failed.');
        return;
      }
      setPreviewItems(preview.Items ?? []);
      setProgressPercent(100);
      setProgressMessage('Validate & preview complete.');
      if (!warnings.length) {
        showInfo('Pack validated. Preview list updated.', true);
      }
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Validate failed.');
    } finally {
      setIsValidating(false);
      setIsPreviewing(false);
    }
  }, [pack, saasApplicationId, showError, showInfo, showWarning]);

  const runExecute = useCallback(async () => {
    if (!pack) {
      showError('Upload an App Config Pack JSON file first.');
      return;
    }
    if (validationErrors.length > 0) {
      showError('Fix validation errors before execute.');
      return;
    }

    setIsExecuting(true);
    setLastExecuteResult(null);
    setProgressPercent(15);
    setProgressMessage('Executing pack…');
    try {
      const result = await appConfigPackSvc.Execute(pack, saasApplicationId);
      const exec = result.Object;
      setLastExecuteResult(exec ?? null);

      if (!result.IsSuccessful || !exec?.IsSuccess) {
        const msg = exec?.ErrorMessage
          || result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Execute failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        showError(msg);
        return;
      }

      try {
        await refreshUserTreeMenu();
      } catch {
        // non-blocking
      }

      const inserted = exec.TransactionsInserted ?? 0;
      const updated = exec.TransactionsUpdated ?? 0;
      setProgressPercent(100);
      setProgressMessage(`Complete — TX inserted ${inserted}, updated ${updated}.`);
      showInfo(
        `Import completed. Tables created: ${exec.TablesCreated ?? 0}, columns added: ${exec.ColumnsAdded ?? 0}, TX inserted: ${inserted}, TX updated: ${updated}, searches inserted: ${exec.SearchesInserted ?? 0}.`,
        true
      );
      appHelper.debugLog('AppConfigPack execute result', exec);
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Execute failed.');
    } finally {
      setIsExecuting(false);
    }
  }, [pack, saasApplicationId, showError, showInfo, validationErrors.length]);

  const getExportRows = useCallback((): ExportCandidate[] => {
    return (exportCv.sourceCollection as ExportCandidate[]) || [];
  }, [exportCv]);

  const syncExportSelectAllState = useCallback(() => {
    const rows = getExportRows();
    setIsSelectAllExportRows(rows.length > 0 && rows.every((r) => r.Selected));
    setExportSelectionSummary(buildExportSelectionSummary(rows));
  }, [getExportRows]);

  const checkOrUncheckAllExport = useCallback((checked: boolean) => {
    setIsSelectAllExportRows(checked);
    const rows = getExportRows();
    rows.forEach((r) => {
      r.Selected = checked;
    });
    exportCv.refresh();
    setExportSelectionSummary(buildExportSelectionSummary(rows));
  }, [exportCv, getExportRows]);

  const runExport = useCallback(async () => {
    if (!saasApplicationId) {
      showError('Current application id is missing.');
      return;
    }

    const rows = getExportRows();
    const selectedTx = rows.filter((r) => r.Kind === 'Transaction' && r.Selected).map((r) => r.Id);
    const selectedSearch = rows.filter((r) => r.Kind === 'Search' && r.Selected).map((r) => r.Id);
    const exportAll = rows.length > 0 && rows.every((r) => r.Selected);

    if (!exportAll && selectedTx.length === 0 && selectedSearch.length === 0) {
      showWarning('Select at least one transaction or search to export.');
      return;
    }

    appHelper.debugLog('AppConfigPack export selection', {
      exportAll,
      selectedTx,
      selectedSearch,
      summary: buildExportSelectionSummary(rows)
    });

    setIsExporting(true);
    setProgressPercent(20);
    setProgressMessage('Exporting application config…');
    try {
      const result = await appConfigPackSvc.Export(
        saasApplicationId,
        selectedTx,
        selectedSearch,
        exportAll
      );
      if (!result.IsSuccessful || !result.Object?.JsonText) {
        const msg = result.ValidationResult?.Items?.map((i) => i.Message).join('; ')
          || 'Export failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        showError(msg);
        return;
      }

      let jsonText = result.Object.JsonText;
      if (!exportAll) {
        jsonText = stripUnselectedSearchesFromPackJson(jsonText, selectedSearch);
      }

      const blob = new Blob([jsonText], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `appConfigPack.${saasApplicationId}.json`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      setProgressPercent(100);
      setProgressMessage('Export downloaded.');
      showInfo('App Config Pack JSON downloaded.', true);
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      showError(err?.message || 'Export failed.');
    } finally {
      setIsExporting(false);
    }
  }, [exportCv, getExportRows, saasApplicationId, showError, showInfo, showWarning]);

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };

  const modeTabClass = (isActive: boolean) =>
    `px-4 py-2 text-sm border-b-2 -mb-px ${
      isActive
        ? `${theme.tab_active} font-medium`
        : `border-transparent opacity-70 hover:opacity-100 ${theme.tab}`
    }`;

  const showProgress = isBusy || (progressMessage != null && progressPercent > 0);

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center px-3 py-2 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {mode === 'import' ? 'Import App Config From JSON' : 'Export App Config To JSON'}
        </div>
      </div>
      <div className={`flex items-center border-b px-3 ${theme.mainContentSection}`} role="tablist" aria-label="App config pack mode">
        <button
          type="button"
          role="tab"
          aria-selected={mode === 'import'}
          className={modeTabClass(mode === 'import')}
          onClick={() => setMode('import')}
        >
          Import
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={mode === 'export'}
          className={modeTabClass(mode === 'export')}
          onClick={() => setMode('export')}
        >
          Export
        </button>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden flex flex-col p-4 gap-3 ${theme.mainContentSection}`}>
        {mode === 'import' && (
          <>
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
                className={actionBtnClass(isBusy || !pack)}
                disabled={isBusy || !pack}
                onClick={runValidateAndPreview}
              >
                <i className={`fa-solid ${isValidating || isPreviewing ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
                Validate Preview
              </button>
              <button
                type="button"
                className={actionBtnClass(isBusy || !pack)}
                disabled={isBusy || !pack}
                onClick={runExecute}
              >
                <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-play'} mr-1`} />
                Execute
              </button>
              {packFileName && (
                <span className={`text-xs ${theme.menu_secondary}`}>
                  <i className="fa-solid fa-file-code mr-1" />
                  {packFileName}
                </span>
              )}
              {packSummary && (
                <span className={`text-xs ml-auto ${theme.menu_secondary}`}>{packSummary}</span>
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
                    className={`h-full ${theme.button_default}`}
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
                Last run: tables {lastExecuteResult.TablesCreated ?? 0}, columns {lastExecuteResult.ColumnsAdded ?? 0},
                {' '}views {lastExecuteResult.ViewsApplied ?? 0},
                {' '}TX inserted {lastExecuteResult.TransactionsInserted ?? 0},
                {' '}updated {lastExecuteResult.TransactionsUpdated ?? 0},
                {' '}searches inserted {lastExecuteResult.SearchesInserted ?? 0}
                {lastExecuteResult.TransactionGroupId ? ` · Group #${lastExecuteResult.TransactionGroupId}` : ''}
              </div>
            )}

            <div className={`h-1 flex-auto min-h-[200px] rounded border overflow-hidden ${theme.inputBox}`}>
              <FlexGrid
                itemsSource={previewCv}
                headersVisibility="Column"
                selectionMode="Row"
                isReadOnly
                className="h-full w-full"
              >
                <FlexGridColumn header="Type" binding="ObjectType" width={120} />
                <FlexGridColumn header="Name" binding="Name" width="*" />
                <FlexGridColumn header="Integration Id" binding="IntegrationId" width={180} />
                <FlexGridColumn header="Action" binding="Action" width={90} />
                <FlexGridColumn header="Existing Id" binding="ExistingId" width={90} />
                <FlexGridColumn header="Detail" binding="Detail" width={220} />
                <FlexGridColumn header="" binding="" width="*" />
              </FlexGrid>
            </div>
          </>
        )}

        {mode === 'export' && (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className={actionBtnClass(isBusy)}
                disabled={isBusy}
                onClick={loadExportCandidates}
              >
                <i className="fa-solid fa-rotate mr-1" />
                Refresh
              </button>
              <button
                type="button"
                className={actionBtnClass(isBusy)}
                disabled={isBusy}
                onClick={runExport}
              >
                <i className={`fa-solid ${isExporting ? 'fa-spinner fa-spin' : 'fa-download'} mr-1`} />
                Download JSON
              </button>
              <span className={`text-xs ${theme.menu_secondary}`}>
                {exportSelectionSummary || 'Leave all selected to export the whole application. Uncheck items to export a subset.'}
              </span>
            </div>

            {showProgress && (
              <div className={`flex-none rounded border px-3 py-2 ${theme.inputBox}`}>
                <div className={`flex items-center justify-between gap-2 text-xs mb-1.5 ${theme.menu_secondary}`}>
                  <span className="truncate">{progressMessage}</span>
                  <span className="flex-none">{progressPercent}%</span>
                </div>
                <div className={`h-1.5 w-full rounded overflow-hidden ${theme.mainContentSection}`}>
                  <div
                    className={`h-full ${theme.button_default}`}
                    style={{ width: `${Math.min(100, Math.max(0, progressPercent))}%` }}
                  />
                </div>
              </div>
            )}

            <div className={`h-1 flex-auto min-h-[200px] rounded border overflow-hidden ${theme.inputBox}`}>
              <FlexGrid
                itemsSource={exportCv}
                headersVisibility="Column"
                selectionMode="Row"
                className="h-full w-full"
              >
                <FlexGridColumn
                  header="Export"
                  binding="Selected"
                  width={70}
                  dataType={DataType.Boolean}
                  isReadOnly={false}
                  allowSorting={false}
                >
                  <FlexGridCellTemplate
                    cellType="ColumnHeader"
                    template={() => (
                      <input
                        type="checkbox"
                        title="Select All"
                        checked={isSelectAllExportRows}
                        onChange={(e) => checkOrUncheckAllExport(e.target.checked)}
                      />
                    )}
                  />
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(cell: any) => (
                      <input
                        type="checkbox"
                        checked={!!cell.item?.Selected}
                        onChange={(ev) => {
                          if (!cell.item) return;
                          cell.item.Selected = ev.target.checked;
                          exportCv.refresh();
                          syncExportSelectAllState();
                        }}
                      />
                    )}
                  />
                </FlexGridColumn>
                <FlexGridColumn header="Kind" binding="Kind" width={120} isReadOnly />
                <FlexGridColumn header="Id" binding="Id" width={90} isReadOnly />
                <FlexGridColumn header="Name" binding="Name" width="*" isReadOnly />
                <FlexGridColumn header="" binding="" width="*" isReadOnly />
              </FlexGrid>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default ImportAppConfig;
