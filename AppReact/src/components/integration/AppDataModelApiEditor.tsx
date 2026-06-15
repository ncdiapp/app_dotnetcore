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
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { endpoints } from '../../webapi/endpoints';
import { getHeaders } from '../../helper/apiServiceHelper';
import { JsonCodeViewer } from '../common/JsonCodeViewer';
import { JsonCodeEditor } from '../common/JsonCodeEditor';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;
const HTTP_METHODS = ['Get', 'Post'];

const DEFAULT_APICONFIG = `{
  "BaseUrl": "",
  "Url": "",
  "Headers": { "CurrentUserSessionId": "" },
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
    JsonSampleData: '',
  };
}

/** Build API URL for DataIntegration: /webapi/DataIntegration/{ActionCode}, optional ?id= from ApiconfigParameters.QueryParams.id */
function buildApiUrl(op: any): string {
  const base = `${endpoints.BASE_URL}/webapi/DataIntegration/${op?.ActionCode ?? ''}`;
  if (op?.HttpMethd !== 'Get') return base;
  try {
    const config = JSON.parse(op?.ApiconfigParameters ?? '{}');
    const id = config?.QueryParams?.id;
    if (id != null && id !== '') return `${base}?id=${encodeURIComponent(String(id))}`;
  } catch {
    // ignore
  }
  return base;
}

/** Parse ApiconfigParameters and return Headers object for fetch */
function getApiHeaders(op: any): Record<string, string> {
  try {
    const config = JSON.parse(op?.ApiconfigParameters ?? '{}');
    return config?.Headers ?? {};
  } catch {
    return {};
  }
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

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [transactionList, setTransactionList] = useState<{ Id: number; Display?: string }[]>([]);
  const [mainSectionActiveTabIndex, setMainSectionActiveTabIndex] = useState(0);
  const [apiResponseText, setApiResponseText] = useState('');
  const latestOperationRef = useRef<any>(null);
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
        } else {
          setCurrentOperation(initNewDataModelApiDto(transactionIdFromQuery ?? null, dataSourceIdFromQuery ?? null));
        }
      } else {
        setCurrentOperation(initNewDataModelApiDto(transactionIdFromQuery ?? null, dataSourceIdFromQuery ?? null));
      }
      setApiResponseText('');
      setIsModified(false);
    } catch (error) {
      showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, dataSourceIdFromQuery, transactionIdFromQuery, dispatch, showError]);

  useEffect(() => {
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
    setIsSaving(true);
    if (manageBusy) dispatch(setIsBusy());
    try {
      const payload = { ...op, IntergrationSettingId: op.IntergrationSettingId ?? API_BUILDER_INTEGRATION_SETTING_ID };
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.IsSuccessful) {
        setIsModified(false);
        const saved = result?.Object;
        if (saved?.Id != null) {
          navigate(`/app-data-model-api-editor/${saved.Id}`, { replace: true });
        }
        if (afterSave && saved) {
          afterSave(saved);
        } else if (!skipReloadAfterSave) {
          loadData();
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
  }, [currentOperation, dispatch, showError, showValidationMessages, showWarning, loadData, navigate]);

  const callGetApi = useCallback(async (apiUrl: string, op: any): Promise<any> => {
    const headers = new Headers(getHeaders());
    const customHeaders = getApiHeaders(op);
    Object.entries(customHeaders).forEach(([k, v]) => headers.set(k, String(v)));
    const response = await fetch(apiUrl, { headers });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }, []);

  const callPostApi = useCallback(async (apiUrl: string, op: any): Promise<any> => {
    const headers = new Headers(getHeaders());
    const customHeaders = getApiHeaders(op);
    Object.entries(customHeaders).forEach(([k, v]) => headers.set(k, String(v)));
    const body = op.JsonSampleData?.trim() ? JSON.parse(op.JsonSampleData) : {};
    const response = await fetch(apiUrl, { method: 'POST', headers, body: JSON.stringify(body) });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
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

    setMainSectionActiveTabIndex(1);
    setApiResponseText('');

    dispatch(setIsBusy());
    (async () => {
      try {
        const savedOp = await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true });
        if (!savedOp) return;

        setCurrentOperation(savedOp);

        const apiUrl = buildApiUrl(savedOp);
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
        } else if (savedOp.HttpMethd === 'Post') {
          const data = await callPostApi(apiUrl, savedOp);
          setApiResponseText(JSON.stringify(data ?? '', null, 2));
        }
      } catch (err) {
        const msg = err instanceof Error ? err.message : String(err);
        setApiResponseText(`Error: ${msg}`);
        showError(msg);
      } finally {
        dispatch(setIsNotBusy());
      }
    })();
  }, [currentOperation, handleSave, callGetApi, callPostApi, dispatch, showError, showValidationMessages, showWarning]);

  const handleRegeneratePayload = useCallback(async () => {
    const op = currentOperation;
    if (!op?.ActionCode) return;
    const apiUrl = buildApiUrl(op);
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
  }, [currentOperation, callGetApi, dispatch, showError]);

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

      <div className={`flex-1 flex flex-col min-h-0 overflow-auto p-3 ${theme.mainContentSection}`}>
        <div className="flex flex-wrap gap-6 mb-3 flex-shrink-0">
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Operation Code</label>
            <input type="text" autoComplete="off" value={op.ActionCode ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev));
              markChange();
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Description</label>
            <input type="text" autoComplete="off" value={op.ActionDescription ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev));
              markChange();
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Data Model</label>
            <select value={op.TranscationId ?? ''} onChange={(e) => {
              const v = e.target.value;
              setCurrentOperation((prev: any) => (prev ? { ...prev, TranscationId: v === '' ? null : Number(v) } : prev));
              markChange();
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs">
              <option value="">—</option>
              {transactionList.map((t) => (
                <option key={t.Id} value={t.Id}>{t.Display ?? t.Id}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Http Method</label>
            <select value={op.HttpMethd ?? 'Get'} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, HttpMethd: e.target.value } : prev));
              markChange();
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs">
              {HTTP_METHODS.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </div>
        </div>

        <div className="flex gap-0 border-b flex-shrink-0 mb-3">
          <button type="button" onClick={() => setMainSectionActiveTabIndex(0)}
            className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${mainSectionActiveTabIndex === 0 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}>
            API Config Parameters
          </button>
          <button type="button" onClick={() => setMainSectionActiveTabIndex(1)}
            className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${mainSectionActiveTabIndex === 1 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}>
            API Call Test
          </button>
        </div>

        {mainSectionActiveTabIndex === 0 && (
          <div className="flex-1 flex flex-col min-h-0">
            <div className={`flex-1 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
              <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                <span className="text-xs font-semibold">Api Config Parameters</span>
              </div>
              <div className="flex-1 flex flex-col p-2 min-h-0">
                <textarea
                  value={op.ApiconfigParameters ?? ''}
                  onChange={(e) => {
                    setCurrentOperation((prev: any) => (prev ? { ...prev, ApiconfigParameters: e.target.value } : prev));
                    markChange();
                  }}
                  className="w-full flex-1 min-h-0 p-2 border rounded font-mono text-xs resize-none"
                  spellCheck={false}
                />
              </div>
            </div>
          </div>
        )}

        {mainSectionActiveTabIndex === 1 && (
          <div className="flex-1 flex flex-col gap-3 min-h-0">
            <div className={`flex flex-col border rounded flex-shrink-0 ${theme.mainContentSection}`}>
              <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                <span className="text-xs font-semibold">API Url</span>
              </div>
              <div className="p-2">
                <div className="p-2 rounded font-mono text-xs bg-gray-50 break-all">
                  {(() => {
                    const pathWithQuery = buildApiUrl(op);
                    const pathAfterBase = pathWithQuery.startsWith('http') ? '' : pathWithQuery.replace(/^\/appai\/?/, '') || '/';
                    const fullUrl = pathWithQuery.startsWith('http') ? pathWithQuery : endpoints.buildEndpointUrl(pathAfterBase);
                    return fullUrl || pathWithQuery || '—';
                  })()}
                </div>
              </div>
            </div>
            <div className="flex-1 flex gap-4 min-h-0">
            {(op.HttpMethd === 'Post' || op.HttpMethd === 'Put') && (
              <div className={`flex-1 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
                <div className="flex items-center justify-between px-2 py-1.5 border-b bg-gray-100">
                  <span className="text-xs font-semibold">Post Payload Json Data</span>
                  <button type="button" onClick={handleRegeneratePayload} disabled={!op.Id}
                    className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50" title="Re-generate payload from Get">
                    <i className="fa fa-database mr-1" aria-hidden /> Re-generate Payload
                  </button>
                </div>
                <div className="flex-1 flex flex-col p-2 min-h-0">
                  <div className="w-full flex-1 min-h-0 border rounded overflow-hidden bg-white">
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
              </div>
            )}
            <div className={`flex-1 flex flex-col border rounded min-h-0 min-w-0 ${theme.mainContentSection}`}>
              <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                <span className="text-xs font-semibold">API Response Data</span>
              </div>
              <div className="flex-1 flex flex-col p-2 min-h-0">
                <JsonCodeViewer
                  text={apiResponseText}
                  placeholder="Response will appear after Send Request"
                  className="w-full flex-1 min-h-0 p-2 border rounded font-mono text-xs bg-gray-50 resize-none"
                />
              </div>
            </div>
            </div>
          </div>
        )}

     
      </div>
    </div>
  );
};

export default AppDataModelApiEditor;
