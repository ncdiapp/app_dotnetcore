import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../redux/hooks/useTheme';
import appHelper from '../../helper/appHelper';
import { refreshUserTreeMenu } from '../../helper/userMenuHelper';
import {
  appConfigPackSvc,
  AppConfigPackExecuteResultDto,
  AppConfigPackPreviewItemDto,
} from '../../webapi/appConfigPackSvc';
import { readCursorWorkspaceFile } from '../../webapi/cursoragentsvc';

export interface CursorAgentStartBuildDialogProps {
  isOpen: boolean;
  sessionId: string | null;
  packPath: string | null;
  saasApplicationId: number | undefined;
  onClose: () => void;
}

const CursorAgentStartBuildDialog: React.FC<CursorAgentStartBuildDialogProps> = ({
  isOpen,
  sessionId,
  packPath,
  saasApplicationId,
  onClose,
}) => {
  const { theme } = useTheme();
  const [pack, setPack] = useState<any>(null);
  const packRef = useRef<any>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [previewItems, setPreviewItems] = useState<AppConfigPackPreviewItemDto[]>([]);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const [validationWarnings, setValidationWarnings] = useState<string[]>([]);
  const [lastExecuteResult, setLastExecuteResult] = useState<AppConfigPackExecuteResultDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isValidating, setIsValidating] = useState(false);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);
  const [progressPercent, setProgressPercent] = useState(0);
  const [progressMessage, setProgressMessage] = useState<string | null>(null);
  const [previewCv] = useState(() => new CollectionView<AppConfigPackPreviewItemDto>([]));

  const isBusy = isLoading || isValidating || isPreviewing || isExecuting;
  packRef.current = pack;

  const packSummary = useMemo(() => {
    if (!pack) return null;
    const tableCount = pack.Tables?.length ?? pack.tables?.length ?? 0;
    const viewCount = pack.Views?.length ?? pack.views?.length ?? 0;
    const txCount = pack.Transactions?.length ?? pack.transactions?.length ?? 0;
    const searchCount = pack.Searches?.length ?? pack.searches?.length ?? 0;
    return `${tableCount} table(s) · ${viewCount} view(s) · ${txCount} transaction(s) · ${searchCount} search(es)`;
  }, [pack]);

  const resetState = useCallback(() => {
    setPack(null);
    packRef.current = null;
    setLoadError(null);
    setPreviewItems([]);
    setValidationErrors([]);
    setValidationWarnings([]);
    setLastExecuteResult(null);
    setIsLoading(false);
    setIsValidating(false);
    setIsPreviewing(false);
    setIsExecuting(false);
    setProgressPercent(0);
    setProgressMessage(null);
  }, []);

  useEffect(() => {
    previewCv.sourceCollection = previewItems;
    previewCv.refresh();
  }, [previewCv, previewItems]);

  const runValidateAndPreview = useCallback(async (
    packOverride?: any,
    options?: { cancelled?: () => boolean }
  ) => {
    const targetPack = packOverride ?? packRef.current;
    if (!targetPack || !saasApplicationId) return;
    const isCancelled = () => options?.cancelled?.() === true;
    setIsValidating(true);
    setIsPreviewing(true);
    setLastExecuteResult(null);
    setProgressPercent(10);
    setProgressMessage('Validating pack…');
    try {
      const validateResult = await appConfigPackSvc.Validate(targetPack);
      if (isCancelled()) return;
      const validation = validateResult.Object;
      const errors = validation?.Errors ?? [];
      const warnings = validation?.Warnings ?? [];
      setValidationErrors(errors);
      setValidationWarnings(warnings);
      if (validation && validation.IsValid === false && errors.length > 0) {
        setProgressPercent(0);
        setProgressMessage(null);
        setPreviewItems([]);
        return;
      }
      setProgressPercent(45);
      setProgressMessage('Building preview…');
      const previewResult = await appConfigPackSvc.Preview(targetPack, saasApplicationId);
      if (isCancelled()) return;
      const preview = previewResult.Object;
      if (!preview?.IsSuccess) {
        setProgressPercent(0);
        setProgressMessage(null);
        setLoadError(preview?.ErrorMessage || 'Preview failed.');
        setPreviewItems([]);
        return;
      }
      setPreviewItems(preview.Items ?? []);
      setProgressPercent(100);
      setProgressMessage('Validate & preview complete.');
    } catch (err: any) {
      if (isCancelled()) return;
      setProgressPercent(0);
      setProgressMessage(null);
      setLoadError(err?.message || 'Validate failed.');
    } finally {
      if (!isCancelled()) {
        setIsValidating(false);
        setIsPreviewing(false);
      }
    }
  }, [saasApplicationId]);

  useEffect(() => {
    if (!isOpen || !sessionId || !packPath) {
      resetState();
      return;
    }
    let cancelled = false;
    const load = async () => {
      setIsLoading(true);
      setLoadError(null);
      setPack(null);
      packRef.current = null;
      setPreviewItems([]);
      setValidationErrors([]);
      setValidationWarnings([]);
      setLastExecuteResult(null);
      setProgressPercent(10);
      setProgressMessage('Loading pack…');
      try {
        const text = await readCursorWorkspaceFile(sessionId, packPath);
        const loaded = await appConfigPackSvc.Load(text);
        if (cancelled) return;
        const next = loaded?.Object;
        if (!loaded?.IsSuccessful || !next) {
          const msg = loaded?.ValidationResult?.Items?.map(i => i.Message).join('; ')
            || 'Could not load the config pack.';
          setLoadError(msg);
          setProgressPercent(0);
          setProgressMessage(null);
          return;
        }
        setPack(next);
        packRef.current = next;
        setIsLoading(false);
        if (!saasApplicationId) {
          setProgressPercent(100);
          setProgressMessage('Pack loaded. Application context missing — cannot validate.');
          return;
        }
        await runValidateAndPreview(next, { cancelled: () => cancelled });
      } catch (err: any) {
        if (cancelled) return;
        setLoadError(err?.message || 'Failed to load pack.');
        setProgressPercent(0);
        setProgressMessage(null);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };
    void load();
    return () => { cancelled = true; };
  }, [isOpen, packPath, resetState, runValidateAndPreview, saasApplicationId, sessionId]);

  const runExecute = useCallback(async () => {
    if (!pack || !saasApplicationId) return;
    if (validationErrors.length > 0) {
      setLoadError('Fix validation errors before execute.');
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
          || result.ValidationResult?.Items?.map(i => i.Message).join('; ')
          || 'Execute failed.';
        setProgressPercent(0);
        setProgressMessage(null);
        setLoadError(msg);
        return;
      }
      try { await refreshUserTreeMenu(); } catch { /* non-blocking */ }
      const inserted = exec.TransactionsInserted ?? 0;
      const updated = exec.TransactionsUpdated ?? 0;
      setProgressPercent(100);
      setProgressMessage(`Complete — TX inserted ${inserted}, updated ${updated}.`);
      setLoadError(null);
    } catch (err: any) {
      setProgressPercent(0);
      setProgressMessage(null);
      setLoadError(err?.message || 'Execute failed.');
    } finally {
      setIsExecuting(false);
    }
  }, [pack, saasApplicationId, validationErrors.length]);

  if (!isOpen || !packPath) return null;

  const actionBtnClass = (disabled: boolean) => {
    const base = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    return disabled ? `${base} opacity-40 cursor-not-allowed` : base;
  };
  const showProgress = isBusy || (progressMessage != null && progressPercent > 0);

  return (
    <div
      className="fixed inset-0 flex items-center justify-center bg-black/30"
      style={{ zIndex: appHelper.getGlobalOverlayZIndex() }}
      onClick={e => e.stopPropagation()}
    >
      <div
        className={`${theme.mainContentSection} rounded-[4px] shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '920px', maxWidth: '95vw', height: '80vh', maxHeight: '860px' }}
        onClick={e => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <div className="min-w-0 w-1 flex-auto">
            <div className={`text-md font-semibold truncate ${theme.title}`}>Start Build</div>
            <div className={`text-xs truncate ${theme.label}`} title={packPath}>{packPath}</div>
          </div>
          <div className="flex items-center space-x-2 shrink-0 ml-2">
            <button
              type="button"
              className={actionBtnClass(isBusy || !pack || !saasApplicationId)}
              disabled={isBusy || !pack || !saasApplicationId}
              onClick={() => void runValidateAndPreview()}
            >
              <i className={`fa-solid ${isValidating || isPreviewing ? 'fa-spinner fa-spin' : 'fa-check'} mr-1`} />
              Validate Preview
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} ${
                isBusy || !pack || !saasApplicationId ? 'opacity-40 cursor-not-allowed' : ''
              }`}
              disabled={isBusy || !pack || !saasApplicationId}
              onClick={() => void runExecute()}
            >
              <i className={`fa-solid ${isExecuting ? 'fa-spinner fa-spin' : 'fa-play'} mr-1`} />
              Execute
            </button>
            <button
              type="button"
              className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
              title="Close"
              onClick={onClose}
            >
              <i className="fa-solid fa-xmark" />
            </button>
          </div>
        </div>

        <div className={`w-full h-1 flex-auto overflow-hidden flex flex-col p-4 gap-3 ${theme.mainContentSection}`}>
          {packSummary && (
            <div className={`text-xs ${theme.label}`}>{packSummary}</div>
          )}

          {showProgress && (
            <div className={`flex-none rounded border px-3 py-2 ${theme.inputBox}`}>
              <div className={`flex items-center justify-between gap-2 text-xs mb-1.5 ${theme.label}`}>
                <div className="flex items-center gap-2 min-w-0">
                  {isBusy && <i className="fa-solid fa-spinner fa-spin" />}
                  {!isBusy && progressPercent >= 100 && (
                    <i className="fa-solid fa-circle-check" />
                  )}
                  <span className="truncate">{progressMessage}</span>
                </div>
                <span className="shrink-0">{progressPercent}%</span>
              </div>
              <div className={`h-1.5 w-full rounded overflow-hidden ${theme.mainContentSection}`}>
                <div
                  className={`h-full ${theme.button_secondary}`}
                  style={{ width: `${Math.min(100, Math.max(0, progressPercent))}%` }}
                />
              </div>
            </div>
          )}

          {loadError && (
            <div className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
              <i className="fa-solid fa-circle-xmark mr-1" />
              {loadError}
            </div>
          )}

          {(validationErrors.length > 0 || validationWarnings.length > 0) && (
            <div className="flex flex-col gap-2 max-h-28 overflow-auto">
              {validationErrors.map(msg => (
                <div key={`err-${msg}`} className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
                  <i className="fa-solid fa-circle-xmark mr-1" />
                  {msg}
                </div>
              ))}
              {validationWarnings.map(msg => (
                <div key={`warn-${msg}`} className={`text-xs px-2 py-1 rounded border ${theme.inputBox} ${theme.label}`}>
                  <i className="fa-solid fa-triangle-exclamation mr-1" />
                  {msg}
                </div>
              ))}
            </div>
          )}

          {lastExecuteResult?.IsSuccess && !isBusy && (
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
        </div>
      </div>
    </div>
  );
};

export default CursorAgentStartBuildDialog;
