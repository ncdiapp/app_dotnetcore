/**
 * Stored Procedure App API Editor.
 * Layout: form header + left INPUT (Parameter List | Input JSON) / right OUTPUT.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import * as wjGrid from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch, useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { endpoints, toApiDisplayUrl } from '../../webapi/endpoints';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { getHeaders } from '../../helper/apiServiceHelper';
import { prettyPrintJsonForDisplay } from '../../helper/integrationPayloadHelper';
import { ensureSessionHeader, isUsedByMcpValue } from '../../helper/apiConfigForm';
import { IsUsedByMcpCheckbox } from './ApiConfigParametersForm';
import { ApiEditorWorkArea } from './ApiEditorWorkArea';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import { JsonCodeViewer } from '../common/JsonCodeViewer';
import type { RootState } from '../../redux/store';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;

type SpParamRow = {
  Name?: string;
  Type?: string;
  Direction?: string;
  MaxLength?: number | null;
  Ordinal?: number;
  HasDefault?: boolean;
  DefaultValue?: string | null;
};

function ensureApiConfig(op: any): any {
  const cfg = ensureSessionHeader({ ...(op?.APIConfigParameters ?? {}) });
  if (!cfg.IsStoredProcedureApi) cfg.IsStoredProcedureApi = true;
  if (!Array.isArray(cfg.SpParameters)) cfg.SpParameters = [];
  return {
    ...op,
    IntergrationSettingId: op?.IntergrationSettingId ?? API_BUILDER_INTEGRATION_SETTING_ID,
    HttpMethd: op?.HttpMethd || 'Post',
    IsSimpleQuery: false,
    APIConfigParameters: cfg,
    ApiconfigParameters: JSON.stringify(cfg),
  };
}

function isOutputOnly(direction?: string): boolean {
  const d = (direction ?? 'IN').toUpperCase();
  return d === 'OUT' || d === 'OUTPUT';
}

function coerceDefaultToken(v: unknown): unknown {
  if (v == null || v === '' || String(v).toLowerCase() === 'null') return null;
  if (typeof v === 'boolean' || typeof v === 'number') return v;
  const s = String(v);
  if (s.toLowerCase() === 'true') return true;
  if (s.toLowerCase() === 'false') return false;
  if (/^-?\d+$/.test(s)) return Number(s);
  if (/^-?\d+\.\d+$/.test(s)) return Number(s);
  return s;
}

/** Build POST body from parameter DefaultValues (IN / INOUT). Parameters sit on the object itself. */
function paramsToInputJson(params: SpParamRow[]): string {
  const body: Record<string, unknown> = {};
  for (const p of params ?? []) {
    if (!p?.Name || isOutputOnly(p.Direction)) continue;
    const bare = p.Name.replace(/^[@:]/, '');
    body[bare] = coerceDefaultToken(p.DefaultValue);
  }
  return JSON.stringify(body, null, 2);
}

/** Apply Input JSON args onto matching parameter DefaultValues. */
function applyInputJsonToParams(jsonText: string, params: SpParamRow[]): SpParamRow[] {
  const parsed = JSON.parse(jsonText);
  const argsObj = (parsed?.args && typeof parsed.args === 'object' ? parsed.args : parsed) as Record<string, unknown>;
  if (!argsObj || typeof argsObj !== 'object' || Array.isArray(argsObj)) {
    throw new Error('Input JSON must be an object.');
  }
  return (params ?? []).map((p) => {
    if (!p?.Name || isOutputOnly(p.Direction)) return { ...p };
    const bare = p.Name.replace(/^[@:]/, '');
    const key = Object.keys(argsObj).find(
      (k) => k.replace(/^[@:]/, '').toLowerCase() === bare.toLowerCase(),
    );
    if (!key) return { ...p };
    const v = argsObj[key];
    return { ...p, DefaultValue: v == null ? '' : String(v) };
  });
}

function buildArgsFromParams(params: SpParamRow[]): Record<string, unknown> {
  const args: Record<string, unknown> = {};
  for (const p of params ?? []) {
    if (!p?.Name) continue;
    if (isOutputOnly(p.Direction) && (p.DefaultValue == null || p.DefaultValue === '')) continue;
    if (p.HasDefault && (p.DefaultValue == null || p.DefaultValue === '')) continue;
    const bare = p.Name.replace(/^[@:]/, '');
    args[bare] = coerceDefaultToken(p.DefaultValue);
  }
  return args;
}

function buildArgsFromInputJson(jsonText: string, fallbackParams: SpParamRow[]): Record<string, unknown> {
  try {
    const parsed = JSON.parse(jsonText || '{}');
    const argsObj = parsed?.args && typeof parsed.args === 'object' ? parsed.args : parsed;
    if (argsObj && typeof argsObj === 'object' && !Array.isArray(argsObj)) {
      const out: Record<string, unknown> = {};
      for (const [k, v] of Object.entries(argsObj as Record<string, unknown>)) {
        out[k.replace(/^[@:]/, '')] = v;
      }
      return out;
    }
  } catch {
    /* fall through */
  }
  return buildArgsFromParams(fallbackParams);
}

function buildApiUrl(op: any): string {
  if (!op?.ActionCode) return '';
  return `${endpoints.BASE_URL}/webapi/DataIntegration/${op.ActionCode}`;
}

const AppStoredProcedureApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const settingParameterId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo, showWarning } = useErrorMessage();
  const serverRoot = useApiServerRoot();
  const isAiConfigured = useSelector(
    (s: RootState) => !!s.userSession?.userContext?.IsAiConfigured,
  );

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isGeneratingDesc, setIsGeneratingDesc] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [dataSourceLabel, setDataSourceLabel] = useState('');
  const [apiResponseText, setApiResponseText] = useState('');
  const [configJsonText, setConfigJsonText] = useState('{}');
  const [configJsonError, setConfigJsonError] = useState('');
  const configJsonEditingRef = useRef(false);
  const payloadJsonActiveRef = useRef(false);
  const [payloadTab, setPayloadTab] = useState<'parameters' | 'json'>('parameters');
  const [paramRows, setParamRows] = useState<SpParamRow[]>([]);
  const [inputJsonText, setInputJsonText] = useState('{}');
  const [inputJsonError, setInputJsonError] = useState('');
  const latestOperationRef = useRef<any>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const syncingRef = useRef(false);

  const paramCV = useMemo(() => new CollectionView<SpParamRow>(paramRows), [paramRows]);

  useEffect(() => {
    latestOperationRef.current = currentOperation;
  }, [currentOperation]);

  useEffect(() => {
    if (configJsonEditingRef.current) return;
    const cfg = currentOperation?.APIConfigParameters;
    if (!cfg) return;
    setConfigJsonText(JSON.stringify(cfg, null, 2));
    setConfigJsonError('');
  }, [currentOperation?.APIConfigParameters]);

  const getCurrentParams = useCallback((): SpParamRow[] => {
    const flex = flexRef.current;
    return (flex?.collectionView?.items ?? paramRows) as SpParamRow[];
  }, [paramRows]);

  const applyParamsToOp = useCallback((op: any, items: SpParamRow[]) => {
    const cfg = ensureSessionHeader({
      ...(op?.APIConfigParameters ?? {}),
      IsStoredProcedureApi: true,
      SpParameters: items.map((x) => ({ ...x })),
    });
    return {
      ...op,
      APIConfigParameters: cfg,
      ApiconfigParameters: JSON.stringify(cfg),
    };
  }, []);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      if (settingParameterId == null) {
        showError('Stored Procedure API editor requires an existing API Id.');
        setCurrentOperation(null);
        return;
      }

      const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(
        String(settingParameterId),
        false,
      );
      if (!settingData) {
        showError('API not found.');
        setCurrentOperation(null);
        return;
      }

      const op = ensureApiConfig(settingData);
      const params = [...(op.APIConfigParameters?.SpParameters ?? [])] as SpParamRow[];
      setCurrentOperation(op);
      setParamRows(params);
      setInputJsonText(paramsToInputJson(params));
      setInputJsonError('');
      // Response panel is bound to JsonSampleData (persisted sample).
      setApiResponseText(prettyPrintJsonForDisplay(op.JsonSampleData ?? '') || '');

      try {
        const list = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
        const ds = (Array.isArray(list) ? list : []).find(
          (x: any) => Number(x?.Id ?? x?.id) === Number(op.DataSourceId),
        );
        const name = ds?.DataSourceName ?? ds?.dataSourceName ?? ds?.Display ?? '';
        setDataSourceLabel(name ? `${name} (${op.DataSourceId})` : String(op.DataSourceId ?? ''));
      } catch {
        setDataSourceLabel(String(op.DataSourceId ?? ''));
      }

      setIsModified(false);
    } catch (error) {
      showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);
  const markChange = useCallback(() => setIsModified(true), []);

  /** Parameter List → Input JSON */
  const syncJsonFromParams = useCallback(
    (items?: SpParamRow[]) => {
      const params = items ?? getCurrentParams();
      syncingRef.current = true;
      setInputJsonText(paramsToInputJson(params));
      setInputJsonError('');
      syncingRef.current = false;
    },
    [getCurrentParams],
  );

  /** Input JSON → Parameter List */
  const syncParamsFromJson = useCallback(
    (jsonText: string): SpParamRow[] | null => {
      try {
        const next = applyInputJsonToParams(jsonText, getCurrentParams());
        setParamRows(next.map((x) => ({ ...x })));
        setCurrentOperation((prev: any) => (prev ? applyParamsToOp(prev, next) : prev));
        setInputJsonError('');
        return next;
      } catch (e) {
        setInputJsonError(e instanceof Error ? e.message : String(e));
        return null;
      }
    },
    [getCurrentParams, applyParamsToOp],
  );

  const syncParamsFromGrid = useCallback(() => {
    const items = getCurrentParams().map((x) => ({ ...x }));
    setParamRows(items);
    setCurrentOperation((prev: any) => (prev ? applyParamsToOp(prev, items) : prev));
    syncJsonFromParams(items);
    markChange();
  }, [getCurrentParams, applyParamsToOp, syncJsonFromParams, markChange]);

  const handleInputJsonChange = useCallback(
    (next: string) => {
      if (syncingRef.current) return;
      setInputJsonText(next);
      markChange();
      try {
        const updated = applyInputJsonToParams(next, getCurrentParams());
        setParamRows(updated.map((x) => ({ ...x })));
        setCurrentOperation((prev: any) => (prev ? applyParamsToOp(prev, updated) : prev));
        setInputJsonError('');
      } catch (e) {
        setInputJsonError(e instanceof Error ? e.message : String(e));
      }
    },
    [getCurrentParams, applyParamsToOp, markChange],
  );

  const handleResetParameters = useCallback(async () => {
    const op = latestOperationRef.current ?? currentOperation;
    const cfg = op?.APIConfigParameters ?? {};
    const dataSourceId = Number(op?.DataSourceId);
    const spName = (cfg.SpName as string) || '';
    const schema = (cfg.SpSchema as string) || undefined;
    if (!dataSourceId || !spName) {
      showWarning('Missing Data Source or Stored Procedure name.');
      return;
    }

    dispatch(setIsBusy());
    try {
      const fresh = await integrationService.getStoredProcedureForApiBuilder(dataSourceId, spName, schema);
      const params = ((fresh?.Parameters ?? fresh?.parameters ?? []) as any[]).map((x) => ({
        Name: x.Name ?? x.name,
        Type: x.Type ?? x.type,
        Direction: x.Direction ?? x.direction,
        MaxLength: x.MaxLength ?? x.maxLength,
        Ordinal: x.Ordinal ?? x.ordinal,
        HasDefault: x.HasDefault ?? x.hasDefault,
        DefaultValue: x.DefaultValue ?? x.defaultValue ?? '',
      })) as SpParamRow[];

      setParamRows(params);
      setInputJsonText(paramsToInputJson(params));
      setInputJsonError('');
      setCurrentOperation((prev: any) => (prev ? applyParamsToOp(prev, params) : prev));
      markChange();
      showInfo('Parameters and defaults reset from stored procedure.', true);
    } catch (e) {
      showError(e instanceof Error ? e.message : String(e));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, applyParamsToOp, dispatch, markChange, showError, showInfo, showWarning]);

  const handleSave = useCallback(
    async (
      afterSave?: (saved: any) => void,
      opts?: { manageBusy?: boolean; skipReloadAfterSave?: boolean },
    ): Promise<any | null> => {
      const manageBusy = opts?.manageBusy ?? true;
      const skipReloadAfterSave = opts?.skipReloadAfterSave ?? false;

      // Prefer latest from active input tab
      if (configJsonError) {
        showWarning('API Config JSON is invalid.');
        return null;
      }
      let items = getCurrentParams();
      if (payloadJsonActiveRef.current) {
        const fromJson = syncParamsFromJson(inputJsonText);
        if (!fromJson) {
          showWarning(inputJsonError || 'Input JSON is invalid.');
          return null;
        }
        items = fromJson;
      }

      let op = ensureApiConfig(latestOperationRef.current ?? currentOperation);
      if (!op) return null;
      op = applyParamsToOp(op, items);
      op = { ...op, IsUsedByMcp: isUsedByMcpValue(op.IsUsedByMcp) };
      if (!op.ActionCode?.trim()) {
        showWarning('You must enter an Operation Code.');
        return null;
      }

      setIsSaving(true);
      if (manageBusy) dispatch(setIsBusy());
      try {
        const result = await integrationService.saveAppIntegrationSettingParameterExDto(op);
        if (result?.ValidationResult) {
          showValidationMessages(result.ValidationResult, true);
        }
        if (result?.IsSuccessful) {
          setIsModified(false);
          const saved = result?.Object;
          if (saved?.Id != null) {
            navigate(`/stored-procedure-api-editor/${saved.Id}`, { replace: true });
          }
          if (afterSave && saved) {
            afterSave(saved);
          } else if (!skipReloadAfterSave) {
            await loadData();
          }
          return saved ?? null;
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to save');
      } finally {
        setIsSaving(false);
        if (manageBusy) dispatch(setIsNotBusy());
      }
      return null;
    },
    [
      currentOperation,
      getCurrentParams,
      configJsonError,
      inputJsonError,
      inputJsonText,
      syncParamsFromJson,
      applyParamsToOp,
      dispatch,
      showError,
      showValidationMessages,
      showWarning,
      loadData,
      navigate,
    ],
  );

  const callPostApi = useCallback(async (apiUrl: string, body: Record<string, unknown>): Promise<any> => {
    const response = await fetch(apiUrl, {
      method: 'POST',
      headers: getHeaders(),
      credentials: 'include',
      body: JSON.stringify(body),
    });
    const text = await response.text();
    let data: any = text;
    try {
      data = JSON.parse(text);
    } catch {
      /* keep text */
    }
    if (!response.ok) {
      const detail = typeof data === 'string' ? data : JSON.stringify(data);
      throw new Error(`HTTP ${response.status}: ${detail || response.statusText}`);
    }
    return data;
  }, []);

  const handleSendRequest = useCallback(() => {
    const op = currentOperation;
    if (!op) return;

    setApiResponseText('');
    dispatch(setIsBusy());
    (async () => {
      try {
        const savedOp = await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true });
        if (!savedOp) return;

        const merged = ensureApiConfig(savedOp);
        const items = (merged.APIConfigParameters?.SpParameters ?? getCurrentParams()) as SpParamRow[];
        const withParams = applyParamsToOp(merged, items);
        setParamRows(items.map((x) => ({ ...x })));
        setInputJsonText(paramsToInputJson(items));

        const apiUrl = toApiDisplayUrl(serverRoot, buildApiUrl(withParams));
        const args = buildArgsFromInputJson(inputJsonText, items);
        const data = await callPostApi(apiUrl, args);
        const jsonStr = prettyPrintJsonForDisplay(data) || JSON.stringify(data ?? '', null, 2);
        setApiResponseText(jsonStr);

        // Persist response sample as JsonSampleData (same field used by other API editors).
        const withSample = { ...withParams, JsonSampleData: jsonStr };
        latestOperationRef.current = withSample;
        setCurrentOperation(withSample);

        const persist = await integrationService.saveAppIntegrationSettingParameterExDto(withSample);
        if (persist?.ValidationResult) {
          showValidationMessages(persist.ValidationResult, true);
        }
        if (persist?.IsSuccessful) {
          setIsModified(false);
          const savedSample = prettyPrintJsonForDisplay(persist?.Object?.JsonSampleData ?? jsonStr);
          setApiResponseText(savedSample || jsonStr);
          setCurrentOperation((prev: any) =>
            prev
              ? {
                  ...prev,
                  Id: persist?.Object?.Id ?? prev.Id,
                  JsonSampleData: persist?.Object?.JsonSampleData ?? jsonStr,
                }
              : prev,
          );
          showInfo('Request completed. Response sample saved.', true);
        } else {
          setIsModified(true);
          showWarning('Request succeeded but saving response sample failed. Click Save Setting.');
        }
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        setApiResponseText(`Error: ${msg}`);
        showError(msg);
      } finally {
        dispatch(setIsNotBusy());
      }
    })();
  }, [
    currentOperation,
    serverRoot,
    handleSave,
    getCurrentParams,
    applyParamsToOp,
    inputJsonText,
    callPostApi,
    dispatch,
    showError,
    showInfo,
    showWarning,
    showValidationMessages,
  ]);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  const op = currentOperation;
  if (!op) return null;

  const cfg = op.APIConfigParameters ?? {};
  const spFullName = [cfg.SpSchema, cfg.SpName].filter(Boolean).join('.') || op.JsonQuery || '—';
  const apiUrl = toApiDisplayUrl(serverRoot, buildApiUrl(op));

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Stored Procedure API: {op.ActionCode ?? '—'}</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={handleSendRequest}
            disabled={isSaving}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-amber-500 hover:bg-amber-600"
            title="Save and send request"
          >
            <i className="fa-solid fa-bolt" aria-hidden /> Send Request
          </button>
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={() => handleSave()}
            disabled={!isModified || isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
            title="Save Setting"
          >
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save Setting
          </button>
        </div>
      </div>

      <div className={`h-1 flex-auto flex flex-col min-h-0 overflow-hidden p-3 ${theme.mainContentSection}`}>
        <div className="w-full max-w-full min-w-0 mb-3 flex-shrink-0 border rounded-[4px] p-3 box-border">
          <div className="w-full max-w-full min-w-0 grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-x-4 gap-y-2">
            <div className="flex items-center min-w-0">
              <label className={`w-28 shrink-0 text-xs mr-2 ${theme.label}`}>Operation Code</label>
              <input
                type="text"
                autoComplete="off"
                value={op.ActionCode ?? ''}
                onChange={(e) => {
                  setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev));
                  markChange();
                }}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              />
            </div>
            <div className="flex items-center min-w-0">
              <label className={`w-28 shrink-0 text-xs mr-2 ${theme.label}`}>Stored Proc</label>
              <input
                type="text"
                readOnly
                value={spFullName}
                title={spFullName}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] opacity-80 ${theme.inputBox}`}
              />
            </div>
            <div className="flex items-center min-w-0">
              <label className={`w-28 shrink-0 text-xs mr-2 ${theme.label}`}>Data Source</label>
              <input
                type="text"
                readOnly
                value={dataSourceLabel}
                title={dataSourceLabel}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] opacity-80 ${theme.inputBox}`}
              />
            </div>
            <div className="flex items-center min-w-0">
              <label className={`w-28 shrink-0 text-xs mr-2 ${theme.label}`}>Http Method</label>
              <input
                type="text"
                readOnly
                value={op.HttpMethd ?? 'Post'}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] opacity-80 ${theme.inputBox}`}
              />
              <IsUsedByMcpCheckbox
                checked={isUsedByMcpValue(op.IsUsedByMcp)}
                onChange={(checked) => {
                  setCurrentOperation((prev: any) => (prev ? { ...prev, IsUsedByMcp: checked } : prev));
                  markChange();
                }}
              />
            </div>
            <div className="flex items-center min-w-0 col-span-1 md:col-span-2 xl:col-span-4 gap-2">
              <label className={`w-28 shrink-0 text-xs ${theme.label}`}>Description</label>
              <input
                type="text"
                autoComplete="off"
                value={op.ActionDescription ?? ''}
                onChange={(e) => {
                  setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev));
                  markChange();
                }}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              />
              {isAiConfigured && (
                <button
                  type="button"
                  disabled={isGeneratingDesc || isSaving}
                  title="Generate English API description with AI (max 500 characters)"
                  className={`h-7 px-2 shrink-0 rounded-[4px] text-xs border inline-flex items-center gap-1 disabled:opacity-60 ${theme.button_default}`}
                  onClick={async () => {
                    const cfg = op.APIConfigParameters ?? {};
                    const dataSourceId = Number(op.DataSourceId);
                    const spName = (cfg.SpName as string) || '';
                    if (!dataSourceId || !spName) {
                      showWarning('Missing Data Source or Stored Procedure name.');
                      return;
                    }
                    setIsGeneratingDesc(true);
                    dispatch(setIsBusy());
                    try {
                      const result = await integrationService.generateStoredProcedureApiDescription({
                        DataSourceId: dataSourceId,
                        Schema: cfg.SpSchema ?? null,
                        SpName: spName,
                        Parameters: getCurrentParams(),
                        ExistingDescription: op.ActionDescription ?? null,
                      });
                      if (result?.ValidationResult) {
                        showValidationMessages(result.ValidationResult, true);
                      }
                      const text = result?.Object?.description ?? result?.Object?.Description;
                      if (result?.IsSuccessful !== false && text) {
                        setCurrentOperation((prev: any) =>
                          prev ? { ...prev, ActionDescription: String(text) } : prev,
                        );
                        markChange();
                        showInfo('API Description generated.', true);
                      } else if (!text) {
                        showError('AI did not return a description.');
                      }
                    } catch (e) {
                      showError(e instanceof Error ? e.message : String(e));
                    } finally {
                      setIsGeneratingDesc(false);
                      dispatch(setIsNotBusy());
                    }
                  }}
                >
                  <i className="fa-solid fa-wand-magic-sparkles" aria-hidden />
                  {isGeneratingDesc ? 'AI…' : 'AI Generate'}
                </button>
              )}
            </div>
          </div>
        </div>

        <ApiEditorWorkArea
          apiUrl={toApiDisplayUrl(serverRoot, apiUrl) || apiUrl}
          showPayload
          onOuterTabChange={(next, previous) => {
            if (previous === 'payload' && next === 'config' && payloadJsonActiveRef.current) syncParamsFromJson(inputJsonText);
            if (previous === 'config' && next === 'payload' && payloadJsonActiveRef.current) syncJsonFromParams();
          }}
          form={(
            <div className="flex items-center gap-2">
              <label className={`w-48 shrink-0 text-xs ${theme.label}`}>CurrentUserSessionId</label>
              <input
                type="text"
                autoComplete="off"
                spellCheck={false}
                value={(() => {
                  const session = op.APIConfigParameters?.Headers?.CurrentUserSessionId;
                  return session == null ? '' : String(session);
                })()}
                onChange={(e) => {
                  const value = e.target.value;
                  configJsonEditingRef.current = false;
                  setCurrentOperation((prev: any) => {
                    if (!prev) return prev;
                    const cfg = { ...(prev.APIConfigParameters ?? {}) };
                    const headers = { ...(cfg.Headers ?? {}) };
                    headers.CurrentUserSessionId = value.trim() === '' ? null : value;
                    cfg.Headers = headers;
                    return { ...prev, APIConfigParameters: cfg, ApiconfigParameters: JSON.stringify(cfg) };
                  });
                  markChange();
                }}
                className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              />
            </div>
          )}
          jsonText={configJsonText}
          jsonError={configJsonError}
          onJsonChange={(text) => {
            configJsonEditingRef.current = true;
            setConfigJsonText(text);
            try {
              const parsed = JSON.parse(text);
              if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
                throw new Error('JSON must be an object.');
              }
              const params = (Array.isArray(parsed.SpParameters) ? parsed.SpParameters : getCurrentParams()) as SpParamRow[];
              const headers = { ...(parsed.Headers ?? {}) };
              if (!Object.prototype.hasOwnProperty.call(headers, 'CurrentUserSessionId')) headers.CurrentUserSessionId = null;
              const cfg = { ...parsed, IsStoredProcedureApi: true, Headers: headers, SpParameters: params.map((x) => ({ ...x })) };
              setParamRows(params.map((x) => ({ ...x })));
              setInputJsonText(paramsToInputJson(params));
              setInputJsonError('');
              setCurrentOperation((prev: any) => (prev ? { ...prev, APIConfigParameters: cfg, ApiconfigParameters: JSON.stringify(cfg) } : prev));
              setConfigJsonError('');
              markChange();
            } catch (e) {
              setConfigJsonError(e instanceof Error ? e.message : 'Invalid JSON');
            }
          }}
          onJsonBlur={() => {
            if (configJsonError) return;
            configJsonEditingRef.current = false;
            const cfg = latestOperationRef.current?.APIConfigParameters;
            if (cfg) setConfigJsonText(JSON.stringify(cfg, null, 2));
          }}
          payload={(
            <div className="flex flex-col h-full min-h-0">
              <div className="flex items-center justify-between border-b flex-shrink-0">
                <div className="flex gap-0">
                  <button
                    type="button"
                    className={`px-3 py-1.5 text-xs rounded-t-[4px] -mb-px ${payloadTab === 'parameters' ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}
                    onClick={() => {
                      if (payloadTab === 'parameters') return;
                      syncParamsFromJson(inputJsonText);
                      payloadJsonActiveRef.current = false;
                      setPayloadTab('parameters');
                    }}
                  >
                    Parameters
                  </button>
                  <button
                    type="button"
                    className={`px-3 py-1.5 text-xs rounded-t-[4px] -mb-px ${payloadTab === 'json' ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}
                    onClick={() => {
                      if (payloadTab === 'json') return;
                      syncJsonFromParams();
                      payloadJsonActiveRef.current = true;
                      setPayloadTab('json');
                    }}
                  >
                    JSON
                  </button>
                </div>
                {payloadTab === 'parameters' ? (
                  <button
                    type="button"
                    onClick={handleResetParameters}
                    className={`h-6 px-2 mr-2 rounded-[4px] text-xs border ${theme.button_default}`}
                    title="Reload parameters and default values from the stored procedure. Keeps CurrentUserSessionId."
                  >
                    <i className="fa-solid fa-rotate-left mr-1" aria-hidden /> Reset Parameters
                  </button>
                ) : null}
              </div>
              {payloadTab === 'parameters' ? (
                <div className="h-1 flex-auto min-h-0 overflow-hidden p-2">
                  <FlexGrid
                    itemsSource={paramCV}
                    autoGenerateColumns={false}
                    selectionMode="Row"
                    isReadOnly={false}
                    headersVisibility="Column"
                    className="w-full h-full !border-0"
                    initialized={(s: any) => {
                      flexRef.current = s?.control ?? s;
                    }}
                    cellEditEnded={() => syncParamsFromGrid()}
                  >
                    <FlexGridColumn binding="Name" header="Name" width={120} isReadOnly />
                    <FlexGridColumn binding="Type" header="Type" width={100} isReadOnly />
                    <FlexGridColumn binding="Direction" header="Direction" width={80} isReadOnly />
                    <FlexGridColumn binding="HasDefault" header="HasDefault" width={90} isReadOnly />
                    <FlexGridColumn binding="DefaultValue" header="DefaultValue" width={140} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                </div>
              ) : (
                <div className="h-1 flex-auto flex flex-col min-h-0 p-2">
                  {inputJsonError ? <div className="mb-1 text-xs text-red-600 flex-shrink-0">{inputJsonError}</div> : null}
                  <div className="w-full h-1 flex-auto min-h-0 border rounded overflow-hidden bg-white">
                    <JsonCodeEditor
                      value={inputJsonText}
                      onChange={handleInputJsonChange}
                      placeholder='{ "paramName": "value" }'
                      className="w-full h-full"
                    />
                  </div>
                </div>
              )}
            </div>
          )}
          result={(
            <JsonCodeViewer
              text={apiResponseText || prettyPrintJsonForDisplay(op.JsonSampleData ?? '')}
              placeholder="Response will appear after Send Request"
              className="w-full h-1 flex-auto min-h-0 p-2 border rounded font-mono text-xs bg-gray-50"
            />
          )}
        />
      </div>
    </div>
  );
};

export default AppStoredProcedureApiEditor;
