/**
 * App Data Model API Editor (create/edit)
 * Binds to a Transaction (data model). Load/save via integrationService.
 * Reference: Angular appDataModelApiEditorCtrl.js, AppDataModelApiEditor.cshtml
 * Features: tabs (API Config Parameters / API Call Test), Send Request, Re-generate Payload, Generate Schema.
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateActiveTabPath, updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { endpoints, toApiDisplayUrl } from '../../webapi/endpoints';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { buildApiTestHeaders } from '../../helper/apiServiceHelper';
import { prettyPrintJsonForDisplay } from '../../helper/integrationPayloadHelper';
import {
  isUsedByMcpValue,
  missingPathPlaceholders,
  parseApiConfig,
  queryParamValue,
  serializeApiConfig,
  type ApiConfigFormModel,
} from '../../helper/apiConfigForm';
import { IsUsedByMcpCheckbox, McpUsageFields, TransactionApiConfigForm } from './ApiConfigParametersForm';
import { ApiEditorWorkArea } from './ApiEditorWorkArea';
import { JsonCodeViewer } from '../common/JsonCodeViewer';
import { JsonCodeEditor } from '../common/JsonCodeEditor';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;
const HTTP_METHODS = ['Get', 'Post'];

const DEFAULT_APICONFIG = `{
  "BaseUrl": "",
  "Url": "",
  "Headers": { "CurrentUserSessionId": null },
  "QueryParams": { "id": 1 },
  "PathParams": {},
  "PostProcessMethodName": null,
  "ResponseObjectMapToEnvionmentVariable": {},
  "ResponseHeaderNeedToSetCookieNames": []
}`;

/** Build minimal new-API DTO like Angular initNewIntegrationSettingData(). */
function initNewDataModelApiDto(transcationId: number | null, dataSourceId: number | null): any {
  return {
    TranscationId: transcationId,
    HttpMethd: 'Get',
    IntergrationSettingId: API_BUILDER_INTEGRATION_SETTING_ID,
    DataSourceId: dataSourceId,
    ActionCode: 'NewDataModelAPI',
    ApiconfigParameters: DEFAULT_APICONFIG,
    IsUsedByMcp: false,
    JsonSampleData: '',
  };
}

function withFormattedApiConfig(op: any): any {
  if (!op || typeof op !== 'object') return op;
  const raw = typeof op.ApiconfigParameters === 'string' ? op.ApiconfigParameters : op.apiconfigParameters;
  if (typeof raw !== 'string' || !raw.trim()) return op;
  return { ...op, ApiconfigParameters: prettyPrintJsonForDisplay(raw) };
}

/** Build API URL for DataIntegration: /webapi/DataIntegration/{ActionCode}, optional ?id= from ApiconfigParameters.QueryParams.id */
function buildApiUrl(op: any, includeGetQuery = false, forPayload = false): string {
  const base = `${endpoints.BASE_URL}/webapi/DataIntegration/${op?.ActionCode ?? ''}`;
  let url = base;
  if (includeGetQuery || op?.HttpMethd === 'Get') {
    try {
      const config = JSON.parse(op?.ApiconfigParameters ?? '{}');
      const id = config?.QueryParams?.id;
      if (id != null && id !== '') url = `${base}?id=${encodeURIComponent(String(id))}`;
    } catch {
      // ignore
    }
  }
  if (forPayload) url += `${url.includes('?') ? '&' : '?'}forPayload=1`;
  return url;
}

/** Parse ApiconfigParameters and return Headers object for fetch */
function getApiHeaders(op: any): Record<string, unknown> {
  try {
    const config = JSON.parse(op?.ApiconfigParameters ?? '{}');
    return config?.Headers ?? {};
  } catch {
    return {};
  }
}

async function readFailedResponse(response: Response): Promise<string> {
  const text = await response.text();
  if (!text.trim()) return `HTTP ${response.status}: ${response.statusText}`;
  return text;
}

const AppDataModelApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const [searchParams] = useSearchParams();
  const param2Str = searchParams.get('param2');
  const dataSourceIdFromQuery = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.dataSourceId ?? null; } catch { return null; } })() : null;
  const transactionIdFromQuery = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.transactionId ?? null; } catch { return null; } })() : null;

  const settingParameterId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo, showWarning } = useErrorMessage();
  const serverRoot = useApiServerRoot();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [transactionList, setTransactionList] = useState<{ Id: number; Display?: string }[]>([]);
  const [apiResponseText, setApiResponseText] = useState('');
  const [jsonDraft, setJsonDraft] = useState(DEFAULT_APICONFIG);
  const [jsonError, setJsonError] = useState('');
  const jsonEditingRef = useRef(false);
  const latestOperationRef = useRef<any>(null);
  const writtenConfigRef = useRef<string | null>(null);
  const [configModel, setConfigModel] = useState<ApiConfigFormModel>(() => parseApiConfig(DEFAULT_APICONFIG));
  const configModelRef = useRef(configModel);
  useEffect(() => {
    configModelRef.current = configModel;
  }, [configModel]);
  useEffect(() => {
    const source = currentOperation?.ApiconfigParameters ?? '';
    if (writtenConfigRef.current === source) return;
    const parsed = parseApiConfig(source);
    const saved = serializeApiConfig(parsed);
    writtenConfigRef.current = saved.json;
    setConfigModel(parsed);
    if (!jsonEditingRef.current) {
      setJsonDraft(saved.json);
      setJsonError('');
    }
  }, [currentOperation?.ApiconfigParameters]);

  const applyConfigModel = useCallback((model: ApiConfigFormModel, canonical = false) => {
    const saved = serializeApiConfig(model);
    const nextModel = canonical ? saved.model : model;
    writtenConfigRef.current = saved.json;
    setConfigModel(nextModel);
    setCurrentOperation((prev: any) => (prev ? { ...prev, ApiconfigParameters: saved.json } : prev));
    if (!jsonEditingRef.current) {
      setJsonDraft(saved.json);
      setJsonError('');
    }
  }, []);
  const skipNextRouteLoadRef = useRef(false);
  useEffect(() => {
    latestOperationRef.current = currentOperation;
  }, [currentOperation]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const massData = await adminSvc.getMassEntitiesLookupItem('AppTransaction');
      const txList = massData?.['AppTransaction'] ?? [];
      setTransactionList(Array.isArray(txList) ? txList : []);

      if (settingParameterId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(settingParameterId), false);
        if (settingData) {
          setCurrentOperation(settingData);
          const method = (settingData.HttpMethd ?? '').toString().toLowerCase();
          const savedResponse = method === 'post' || method === 'put'
            ? (settingData.PostResponseDto?.ResponseJsonData ?? '')
            : (settingData.JsonSampleData ?? '');
          setApiResponseText(savedResponse);
        } else {
          setCurrentOperation(initNewDataModelApiDto(transactionIdFromQuery ?? null, dataSourceIdFromQuery ?? null));
        }
      } else {
        setCurrentOperation(initNewDataModelApiDto(transactionIdFromQuery ?? null, dataSourceIdFromQuery ?? null));
      }
      if (settingParameterId == null) setApiResponseText('');
      setIsModified(false);
    } catch (error) {
      showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, dataSourceIdFromQuery, transactionIdFromQuery, dispatch, showError]);

  useEffect(() => {
    if (skipNextRouteLoadRef.current) {
      skipNextRouteLoadRef.current = false;
      return;
    }
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const handleSave = useCallback(async (
    afterSave?: (savedOp: any) => void,
    opts?: { manageBusy?: boolean; skipReloadAfterSave?: boolean },
  ): Promise<any | null> => {
    const manageBusy = opts?.manageBusy ?? true;
    const skipReloadAfterSave = opts?.skipReloadAfterSave ?? false;
    const op = latestOperationRef.current ?? currentOperation;
    if (!op) return null;
    if (!op.TranscationId) {
      showWarning('You must select a data model.');
      return null;
    }
    if (!op.HttpMethd) {
      showWarning('You must select an HTTP method.');
      return null;
    }
    const savedConfig = serializeApiConfig(configModelRef.current);
    if (!queryParamValue(savedConfig.model, 'id')) {
      showWarning('Query Params must include id.');
      return null;
    }
    const missing = missingPathPlaceholders(savedConfig.model);
    if (missing.length) {
      showWarning(`Path param ${missing.join(', ')} is not in the API URL.`);
    }
    setIsSaving(true);
    if (manageBusy) dispatch(setIsBusy());
    try {
      const payload = {
        ...op,
        ApiconfigParameters: savedConfig.json,
        IsUsedByMcp: isUsedByMcpValue(op.IsUsedByMcp),
        IntergrationSettingId: op.IntergrationSettingId ?? API_BUILDER_INTEGRATION_SETTING_ID,
        APIConfigParameters: null,
      };
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.IsSuccessful) {
        setIsModified(false);
        const saved = result?.Object;
        const savedId = saved?.Id != null ? Number(saved.Id) : null;
        const idChanged = savedId != null && !Number.isNaN(savedId) && savedId !== settingParameterId;
        if (savedId != null && !Number.isNaN(savedId)) {
          const newPath = `/app-data-model-api-editor/${savedId}`;
          const tabLabel = saved?.ActionCode ? `API: ${saved.ActionCode}` : `API (${savedId})`;
          // Update tab path before navigate. Otherwise useTabNavigation syncs the URL
          // back to the create route and loadData() with a null id opens a blank New API.
          dispatch(updateActiveTabPath(newPath));
          dispatch(updateCurrentTabLabel(tabLabel));
          if (idChanged) {
            if (skipReloadAfterSave || afterSave) {
              skipNextRouteLoadRef.current = true;
            }
            navigate(newPath, { replace: true });
          }
        }
        if (afterSave && saved) {
          afterSave(saved);
        } else if (!skipReloadAfterSave && !idChanged) {
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
  }, [currentOperation, dispatch, showError, showValidationMessages, showWarning, loadData, navigate, settingParameterId]);

  const callGetApi = useCallback(async (apiUrl: string, op: any): Promise<any> => {
    const headers = buildApiTestHeaders(getApiHeaders(op));
    const response = await fetch(apiUrl, { headers, credentials: 'include' });
    if (!response.ok) throw new Error(await readFailedResponse(response));
    const text = await response.text();
    if (!text.trim()) throw new Error('GET returned an empty body. Set QueryParams.id and try again.');
    return JSON.parse(text);
  }, []);

  const callPostApi = useCallback(async (apiUrl: string, op: any): Promise<any> => {
    const headers = buildApiTestHeaders(getApiHeaders(op));
    const body = op.JsonSampleData?.trim() ? JSON.parse(op.JsonSampleData) : {};
    const response = await fetch(apiUrl, { method: 'POST', headers, credentials: 'include', body: JSON.stringify(body) });
    if (!response.ok) throw new Error(await readFailedResponse(response));
    return response.json();
  }, []);

  const handleSendRequest = useCallback(() => {
    const op = currentOperation;
    if (!op) return;
    if (!op.TranscationId) {
      showWarning('You must select a data model.');
      return;
    }
    if (!op.HttpMethd) {
      showWarning('You must select an HTTP method.');
      return;
    }

    setApiResponseText('');

    dispatch(setIsBusy());
    (async () => {
      try {
        const savedOp = withFormattedApiConfig(await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true }));
        if (!savedOp) return;

        setCurrentOperation(savedOp);

        const apiUrl = toApiDisplayUrl(serverRoot, buildApiUrl(savedOp));
        if (savedOp.HttpMethd === 'Get') {
          const data = await callGetApi(apiUrl, savedOp);
          const jsonStr = JSON.stringify(data ?? '', null, 2);
          setApiResponseText(jsonStr);
          setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: jsonStr } : null));

          const schemaResult = await integrationService.generateDefaultSchemaAndDataSetMappingFromSampleJson({
            ...savedOp,
            JsonSampleData: jsonStr,
          });
          if (schemaResult?.ValidationResult) {
            showValidationMessages(schemaResult.ValidationResult, true);
          }
        } else if (savedOp.HttpMethd === 'Post' || savedOp.HttpMethd === 'Put') {
          const data = await callPostApi(apiUrl, savedOp);
          const jsonStr = JSON.stringify(data ?? '', null, 2);
          setApiResponseText(jsonStr);
          const withResponse = {
            ...savedOp,
            PostResponseDto: { ...(savedOp.PostResponseDto || {}), ResponseJsonData: jsonStr },
          };
          latestOperationRef.current = withResponse;
          setCurrentOperation(withResponse);
          await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true });
        }
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        setApiResponseText(`Error: ${msg}`);
        showError(msg);
      } finally {
        dispatch(setIsNotBusy());
      }
    })();
  }, [currentOperation, serverRoot, handleSave, callGetApi, callPostApi, dispatch, showError, showValidationMessages, showWarning]);

  const handleRegeneratePayload = useCallback(async () => {
    const op = currentOperation;
    if (!op?.ActionCode) return;
    const apiUrl = toApiDisplayUrl(serverRoot, buildApiUrl(op, true, true));
    dispatch(setIsBusy());
    try {
      const data = await callGetApi(apiUrl, op);
      const jsonStr = JSON.stringify(data ?? '', null, 2);
      setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: jsonStr } : null));
      setIsModified(true);
    } catch (err) {
      showError(err instanceof Error ? err.message : String(err));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, serverRoot, callGetApi, dispatch, showError]);

  const markChange = useCallback(() => {
    setCurrentOperation((prev: any) => (prev ? { ...prev, IsModified: true } : null));
    setIsModified(true);
  }, []);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  const op = currentOperation;
  if (!op) return null;

  const apiUrl = buildApiUrl(op);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>API: {op.ActionCode ?? '—'}</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={handleSendRequest} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-amber-500 hover:bg-amber-600" title="Send Request">
            <i className="fa fa-bolt" aria-hidden /> Send Request
          </button>
          <button type="button" onClick={handleRefresh} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa fa-refresh" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={() => handleSave()} disabled={!isModified || isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600" title="Save Setting">
            <i className="fa fa-save" aria-hidden /> Save Setting
          </button>
        </div>
      </div>

      <div className={`h-1 flex-auto flex flex-col min-h-0 overflow-hidden p-3 ${theme.mainContentSection}`}>
        <div className="flex flex-wrap gap-6 mb-3 flex-shrink-0">
          <div className="flex items-center gap-2">
            <label className={`w-48 shrink-0 text-xs ${theme.label}`}>Operation Code</label>
            <input type="text" autoComplete="off" value={op.ActionCode ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev));
              markChange();
            }}             className="w-[200px] h-7 px-2 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-48 shrink-0 text-xs ${theme.label}`}>Description</label>
            <input type="text" autoComplete="off" value={op.ActionDescription ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev));
              markChange();
            }}             className="w-[200px] h-7 px-2 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-48 shrink-0 text-xs ${theme.label}`}>Data Model</label>
            <select value={op.TranscationId ?? ''} onChange={(e) => {
              const v = e.target.value;
              setCurrentOperation((prev: any) => (prev ? { ...prev, TranscationId: v === '' ? null : Number(v) } : prev));
              markChange();
            }} className="w-[200px] h-7 px-2 border rounded-[4px] text-xs">
              <option value="">—</option>
              {transactionList.map((t) => (
                <option key={t.Id} value={t.Id}>{t.Display ?? t.Id}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-48 shrink-0 text-xs ${theme.label}`}>Http Method</label>
            <select value={op.HttpMethd ?? 'Get'} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, HttpMethd: e.target.value } : prev));
              markChange();
            }} className="w-[200px] h-7 px-2 border rounded-[4px] text-xs">
              {HTTP_METHODS.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </div>
          <div className="ml-auto flex items-center">
            <IsUsedByMcpCheckbox
              checked={isUsedByMcpValue(op.IsUsedByMcp)}
              onChange={(checked) => {
                setCurrentOperation((prev: any) => (prev ? { ...prev, IsUsedByMcp: checked } : prev));
                markChange();
              }}
            />
          </div>
        </div>
        {isUsedByMcpValue(op.IsUsedByMcp) && (
          <div className="mb-3">
            <McpUsageFields
              hint={op.McpUiHint ?? ''}
              notes={op.McpUsageNotes ?? ''}
              onHintChange={(value) => {
                setCurrentOperation((prev: any) => (prev ? { ...prev, McpUiHint: value } : prev));
                markChange();
              }}
              onNotesChange={(value) => {
                setCurrentOperation((prev: any) => (prev ? { ...prev, McpUsageNotes: value } : prev));
                markChange();
              }}
            />
          </div>
        )}

        <ApiEditorWorkArea
          apiUrl={(() => {
            const pathWithQuery = buildApiUrl(op);
            return toApiDisplayUrl(serverRoot, pathWithQuery) || pathWithQuery;
          })()}
          showPayload={op.HttpMethd === 'Post' || op.HttpMethd === 'Put'}
          form={(
            <TransactionApiConfigForm
              model={configModel}
              onChange={(model) => {
                jsonEditingRef.current = false;
                applyConfigModel(model, false);
                markChange();
              }}
            />
          )}
          jsonText={jsonDraft}
          jsonError={jsonError}
          onJsonChange={(text) => {
            jsonEditingRef.current = true;
            setJsonDraft(text);
            try {
              JSON.parse(text);
              applyConfigModel(parseApiConfig(text), false);
              setJsonError('');
              markChange();
            } catch {
              setJsonError('Invalid JSON');
            }
          }}
          onJsonBlur={() => {
            jsonEditingRef.current = false;
            if (!jsonError) {
              const saved = serializeApiConfig(configModelRef.current);
              setJsonDraft(saved.json);
            }
          }}
          payload={(
            <div className="h-full flex flex-col min-h-0 p-2">
              <div className="flex items-center justify-end mb-2 flex-shrink-0">
                <button type="button" onClick={handleRegeneratePayload} disabled={!op.Id}
                  className={`h-6 px-2 rounded-[4px] text-xs border ${theme.button_default} disabled:opacity-50`} title="Re-generate payload from Get">
                  <i className="fa-solid fa-database mr-1" aria-hidden /> Re-generate Payload
                </button>
              </div>
              <div className="w-full h-1 flex-auto min-h-0 border rounded overflow-hidden bg-white">
                <JsonCodeEditor
                  value={op.JsonSampleData ?? ''}
                  onChange={(next) => {
                    setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: next } : prev));
                    markChange();
                  }}
                  placeholder='e.g. {"key": "value"}'
                  className="w-full h-full"
                />
              </div>
            </div>
          )}
          result={(
            <JsonCodeViewer
              text={apiResponseText}
              placeholder="Response will appear after Send Request"
              className="w-full h-1 flex-auto min-h-0 p-2 border rounded font-mono text-xs bg-gray-50 resize-none"
            />
          )}
        />

     
      </div>
    </div>
  );
};

export default AppDataModelApiEditor;
