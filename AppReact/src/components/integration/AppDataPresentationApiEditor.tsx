/**
 * App Report & View API Editor (create/edit)
 * Binds to AppSearch (TranscationFieId). Load/save via integrationService.
 * Reference: Angular appDataPresentationApiEditorCtrl.js, AppDataPresentationApiEditor.cshtml
 * Features: Send Request, Reset Config Parameters, Tabs (API Config / API Call Test), Edit Search.
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
import { searchSvc } from '../../webapi/searchSvc';
import { endpoints, toApiDisplayUrl } from '../../webapi/endpoints';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { buildApiTestHeaders } from '../../helper/apiServiceHelper';
import { prettyPrintJsonForDisplay } from '../../helper/integrationPayloadHelper';
import { isUsedByMcpValue } from '../../helper/apiConfigForm';
import { IsUsedByMcpCheckbox, McpUsageFields, SearchApiConfigForm, type ApiConfigEditorDto } from './ApiConfigParametersForm';
import { ApiEditorWorkArea } from './ApiEditorWorkArea';
import { buildRoutePathFromParamObj } from '../../helper/navigationHelper';
import { JsonCodeViewer } from '../common/JsonCodeViewer';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;
const HTTP_METHODS = ['Get', 'Post'];

const DEFAULT_APICONFIG = `{
  "BaseUrl": "",
  "Url": "",
  "Headers": { "CurrentUserSessionId": null },
  "QueryParams": {},
  "PathParams": {},
  "PostProcessMethodName": null,
  "ResponseObjectMapToEnvionmentVariable": {},
  "ResponseHeaderNeedToSetCookieNames": []
}`;

/** Build minimal new-API DTO like Angular initNewIntegrationSettingData(). */
function initNewDataPresentationApiDto(transcationFieId: number | null, dataSourceId: number | null): any {
  return {
    TranscationFieId: transcationFieId,
    HttpMethd: 'Get',
    IntergrationSettingId: API_BUILDER_INTEGRATION_SETTING_ID,
    DataSourceId: dataSourceId,
    ActionCode: 'NewSearchViewAPI',
    ApiconfigParameters: DEFAULT_APICONFIG,
    IsUsedByMcp: false,
    JsonSampleData: '',
    JsonSchema: '',
  };
}

function withFormattedApiConfig(op: any): any {
  if (!op || typeof op !== 'object') return op;
  const raw = typeof op.ApiconfigParameters === 'string' ? op.ApiconfigParameters : op.apiconfigParameters;
  if (typeof raw !== 'string' || !raw.trim()) return op;
  return { ...op, ApiconfigParameters: prettyPrintJsonForDisplay(raw) };
}

function emptySearchEditor(): ApiConfigEditorDto {
  return { CurrentUserSessionId: '', Criteria: [] };
}

function editorFromOperation(op: any): ApiConfigEditorDto {
  const editor = op?.ApiConfigEditor;
  if (editor && Array.isArray(editor.Criteria)) {
    return {
      CurrentUserSessionId: editor.CurrentUserSessionId ?? '',
      Criteria: editor.Criteria.map((row: any) => ({
        Name: row?.Name ?? '',
        DefaultValue: row?.DefaultValue ?? '',
      })),
    };
  }
  try {
    const config = JSON.parse(op?.ApiconfigParameters ?? '{}');
    const query = config?.QueryParams ?? {};
    const session = config?.Headers?.CurrentUserSessionId;
    return {
      CurrentUserSessionId: session == null ? '' : String(session),
      Criteria: Object.entries(query).map(([name, value]) => ({
        Name: name,
        DefaultValue: value == null ? '' : String(value),
      })),
    };
  } catch {
    return emptySearchEditor();
  }
}

function mergeSearchEditorIntoJson(source: string, editor: ApiConfigEditorDto): string {
  let root: any = {};
  try {
    root = JSON.parse(source || '{}');
  } catch {
    root = {};
  }
  if (!root || typeof root !== 'object' || Array.isArray(root)) root = {};
  const headers = { ...(root.Headers ?? {}) };
  const session = (editor.CurrentUserSessionId ?? '').trim();
  headers.CurrentUserSessionId = session === '' ? null : editor.CurrentUserSessionId;
  const query = { ...(root.QueryParams ?? {}) };
  for (const row of editor.Criteria ?? []) {
    if (!row.Name) continue;
    query[row.Name] = row.DefaultValue ?? '';
  }
  root.Headers = headers;
  root.QueryParams = query;
  return JSON.stringify(root, null, 2);
}

function editorFromConfigJson(source: string, current: ApiConfigEditorDto): ApiConfigEditorDto {
  const root = JSON.parse(source);
  const session = root?.Headers?.CurrentUserSessionId;
  const query = root?.QueryParams ?? {};
  return {
    CurrentUserSessionId: session == null ? '' : String(session),
    Criteria: (current.Criteria ?? []).map((row) => ({
      Name: row.Name,
      DefaultValue: query?.[row.Name ?? ''] == null ? '' : String(query[row.Name ?? '']),
    })),
  };
}

function buildApiUrl(op: any, editor?: ApiConfigEditorDto | null): string {
  const base = `${endpoints.BASE_URL}/webapi/DataIntegration/${op?.ActionCode ?? ''}`;
  if (op?.HttpMethd !== 'Get') return base;
  const pairs = (editor?.Criteria ?? []).filter((row) => (row.Name ?? '').trim() !== '' && (row.DefaultValue ?? '') !== '');
  if (pairs.length === 0) return base;
  const search = pairs.map((row) => `${encodeURIComponent(row.Name ?? '')}=${encodeURIComponent(String(row.DefaultValue ?? ''))}`).join('&');
  return `${base}?${search}`;
}

function getApiHeaders(editor?: ApiConfigEditorDto | null): Record<string, string> {
  const session = editor?.CurrentUserSessionId;
  return { CurrentUserSessionId: session == null ? '' : String(session) };
}

const AppDataPresentationApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const [searchParams] = useSearchParams();
  const param2Str = searchParams.get('param2');
  const dataSourceIdFromQuery = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.dataSourceId ?? null; } catch { return null; } })() : null;
  const searchIdFromQuery = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.searchId ?? null; } catch { return null; } })() : null;

  const settingParameterId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo, showWarning } = useErrorMessage();
  const serverRoot = useApiServerRoot();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [searchList, setSearchList] = useState<{ Id: number; Display?: string }[]>([]);
  const [apiResponseText, setApiResponseText] = useState('');
  const [jsonDraft, setJsonDraft] = useState(DEFAULT_APICONFIG);
  const [jsonError, setJsonError] = useState('');
  const jsonEditingRef = useRef(false);
  const latestOperationRef = useRef<any>(null);
  const [searchEditor, setSearchEditor] = useState<ApiConfigEditorDto>(emptySearchEditor);
  const searchEditorRef = useRef(searchEditor);
  useEffect(() => {
    searchEditorRef.current = searchEditor;
  }, [searchEditor]);
  const skipNextRouteLoadRef = useRef(false);
  useEffect(() => {
    latestOperationRef.current = currentOperation;
  }, [currentOperation]);

  /** Reset ApiconfigParameters (and JsonSampleData, JsonSchema) from selected search. Called when Data Search changes (cascading) or Reset Config. */
  const resetConfigBySearchId = useCallback(async (searchId: number | null) => {
    dispatch(setIsBusy());
    try {
      if (!searchId) {
        const editor = emptySearchEditor();
        setSearchEditor(editor);
        setJsonDraft(DEFAULT_APICONFIG);
        setJsonError('');
        setCurrentOperation((prev: any) =>
          prev ? { ...prev, ApiconfigParameters: DEFAULT_APICONFIG, ApiConfigEditor: editor, JsonSampleData: '', JsonSchema: '' } : null,
        );
        setIsModified(true);
        return;
      }
      const searchDto = await searchSvc.retrieveOneAppSearchExDto(String(searchId));
      const criteria: { Name: string; DefaultValue: string }[] = [];
      const fields = searchDto?.AppSearchFieldList ?? [];
      for (const f of fields) {
        const path = f?.SysTableFiledPath ?? '';
        if (path) criteria.push({ Name: path, DefaultValue: f?.DefaultValue ?? '' });
      }
      const editor: ApiConfigEditorDto = { CurrentUserSessionId: '', Criteria: criteria };
      const json = mergeSearchEditorIntoJson(DEFAULT_APICONFIG, editor);
      setSearchEditor(editor);
      setJsonDraft(json);
      setJsonError('');
      setCurrentOperation((prev: any) =>
        prev ? { ...prev, ApiconfigParameters: json, ApiConfigEditor: editor, JsonSampleData: '', JsonSchema: '' } : null,
      );
      setIsModified(true);
    } catch (err) {
      showError(err instanceof Error ? err.message : String(err));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const massData = await adminSvc.getMassEntitiesLookupItem('AppSearch');
      const list = massData?.['AppSearch'] ?? [];
      setSearchList(Array.isArray(list) ? list : []);

      if (settingParameterId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(settingParameterId), false);
        if (settingData) {
          setCurrentOperation(settingData);
          const loadedEditor = editorFromOperation(settingData);
          setSearchEditor(loadedEditor);
          setJsonDraft(mergeSearchEditorIntoJson(settingData.ApiconfigParameters ?? DEFAULT_APICONFIG, loadedEditor));
          setJsonError('');
          const method = (settingData.HttpMethd ?? '').toString().toLowerCase();
          const savedResponse = method === 'post' || method === 'put'
            ? (settingData.PostResponseDto?.ResponseJsonData ?? '')
            : (settingData.JsonSampleData ?? '');
          setApiResponseText(savedResponse);
        } else {
          const created = initNewDataPresentationApiDto(searchIdFromQuery ?? null, dataSourceIdFromQuery ?? null);
          setCurrentOperation(created);
          setSearchEditor(emptySearchEditor());
        }
      } else {
        const created = initNewDataPresentationApiDto(searchIdFromQuery ?? null, dataSourceIdFromQuery ?? null);
        setCurrentOperation(created);
        setSearchEditor(emptySearchEditor());
        if (searchIdFromQuery != null) {
          resetConfigBySearchId(searchIdFromQuery);
        }
      }
      if (settingParameterId == null) setApiResponseText('');
      setIsModified(false);
    } catch (error) {
      showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, dataSourceIdFromQuery, searchIdFromQuery, dispatch, showError, resetConfigBySearchId]);

  useEffect(() => {
    if (skipNextRouteLoadRef.current) {
      skipNextRouteLoadRef.current = false;
      return;
    }
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const handleSave = useCallback(async (
    afterSave?: (saved: any) => void,
    opts?: { manageBusy?: boolean; skipReloadAfterSave?: boolean },
  ): Promise<any | null> => {
    const manageBusy = opts?.manageBusy ?? true;
    const skipReloadAfterSave = opts?.skipReloadAfterSave ?? false;
    const op = latestOperationRef.current ?? currentOperation;
    if (!op) return null;
    if (!op.TranscationFieId) {
      showWarning('You must select a data search.');
      return null;
    }
    if (!op.HttpMethd) {
      showWarning('You must select an HTTP method.');
      return null;
    }
    const editor = searchEditorRef.current;
    setIsSaving(true);
    if (manageBusy) dispatch(setIsBusy());
    try {
      const payload = {
        ...op,
        ApiConfigEditor: editor,
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
          const newPath = `/app-data-presentation-api-editor/${savedId}`;
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
        if (saved?.ApiConfigEditor) setSearchEditor(editorFromOperation(saved));
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

  const callGetApi = useCallback(async (apiUrl: string, _op: any): Promise<any> => {
    const response = await fetch(apiUrl, { headers: buildApiTestHeaders(getApiHeaders(searchEditorRef.current)), credentials: 'include' });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }, []);

  const callPostApi = useCallback(async (apiUrl: string, op: any): Promise<any> => {
    const body = op.JsonSampleData?.trim() ? JSON.parse(op.JsonSampleData) : {};
    const response = await fetch(apiUrl, {
      method: 'POST',
      headers: buildApiTestHeaders(getApiHeaders(searchEditorRef.current)),
      credentials: 'include',
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }, []);

  const handleSendRequest = useCallback(() => {
    const op = currentOperation;
    if (!op) return;

    setApiResponseText('');

    dispatch(setIsBusy());
    (async () => {
      try {
        const savedOp = withFormattedApiConfig(await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true }));
        if (!savedOp) return;

        setCurrentOperation(savedOp);
        if (savedOp?.ApiConfigEditor) setSearchEditor(editorFromOperation(savedOp));

        const apiUrl = toApiDisplayUrl(serverRoot, buildApiUrl(savedOp, savedOp?.ApiConfigEditor ?? searchEditorRef.current));
        if (savedOp.HttpMethd === 'Get') {
          const data = await callGetApi(apiUrl, savedOp);
          const jsonStr = JSON.stringify(data ?? '', null, 2);
          setApiResponseText(jsonStr);
          let withSample = { ...savedOp, JsonSampleData: jsonStr };
          const schemaResult = await integrationService.generateDefaultSchemaAndDataSetMappingFromSampleJson(withSample);
          if (schemaResult?.Object) {
            withSample = withFormattedApiConfig({ ...withSample, ...schemaResult.Object, JsonSampleData: jsonStr });
          }
          if (schemaResult?.ValidationResult) {
            showValidationMessages(schemaResult.ValidationResult, true);
          }
          latestOperationRef.current = withSample;
          setCurrentOperation(withSample);
          await handleSave(undefined, { manageBusy: false, skipReloadAfterSave: true });
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
  }, [currentOperation, serverRoot, handleSave, callGetApi, callPostApi, dispatch, showError, showValidationMessages]);

  const handleResetConfigParameters = useCallback(async () => {
    const searchId = currentOperation?.TranscationFieId ?? null;
    if (searchId == null) {
      showWarning('Select a Data Search first.');
      return;
    }
    await resetConfigBySearchId(searchId);
  }, [currentOperation?.TranscationFieId, resetConfigBySearchId, showWarning]);

  const handleEditSearch = useCallback(() => {
    const searchId = currentOperation?.TranscationFieId;
    if (!searchId) return;
    const path = buildRoutePathFromParamObj('MasterDataManagement', { searchId, isSavedSearch: true });
    navigate(path);
  }, [currentOperation?.TranscationFieId, navigate]);

  const markChange = useCallback(() => {
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

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Data Search API: {op.ActionCode ?? '—'}</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={handleSendRequest} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-amber-500 hover:bg-amber-600" title="Send Request">
            <i className="fa fa-bolt" aria-hidden /> Send Request
          </button>
          <button type="button" onClick={handleRefresh} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa fa-refresh" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleResetConfigParameters} disabled={!op.TranscationFieId || isSaving}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs border bg-white disabled:opacity-50 disabled:cursor-not-allowed" title="Reset API Config from selected search">
            <i className="fa fa-arrows-rotate mr-1" aria-hidden /> Reset Config
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
            <label className={`w-48 shrink-0 text-xs ${theme.label}`}>Data Search</label>
            <div className="flex items-center gap-1">
              <select value={op.TranscationFieId ?? ''} onChange={(e) => {
                const v = e.target.value;
                const newSearchId = v === '' ? null : Number(v);
                setCurrentOperation((prev: any) => (prev ? { ...prev, TranscationFieId: newSearchId } : prev));
                markChange();
                resetConfigBySearchId(newSearchId);
              }} className="w-[200px] h-7 px-2 border rounded-[4px] text-xs">
                <option value="">—</option>
                {searchList.map((s) => (
                  <option key={s.Id} value={s.Id}>{s.Display ?? s.Id}</option>
                ))}
              </select>
              {op.TranscationFieId && (
                <button type="button" onClick={handleEditSearch} className="h-6 w-6 flex items-center justify-center rounded-[4px] text-xs border bg-white" title="Edit Search">
                  <i className="fa fa-pen" aria-hidden />
                </button>
              )}
            </div>
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
            const pathWithQuery = buildApiUrl(op, searchEditor);
            return toApiDisplayUrl(serverRoot, pathWithQuery) || pathWithQuery;
          })()}
          showPayload={false}
          form={(
            <SearchApiConfigForm
              editor={searchEditor}
              onChange={(editor) => {
                jsonEditingRef.current = false;
                setSearchEditor(editor);
                const json = mergeSearchEditorIntoJson(latestOperationRef.current?.ApiconfigParameters ?? DEFAULT_APICONFIG, editor);
                setJsonDraft(json);
                setJsonError('');
                setCurrentOperation((prev: any) => (prev ? { ...prev, ApiConfigEditor: editor, ApiconfigParameters: json } : prev));
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
              const next = editorFromConfigJson(text, searchEditorRef.current);
              setSearchEditor(next);
              setCurrentOperation((prev: any) => (prev ? { ...prev, ApiConfigEditor: next, ApiconfigParameters: text } : prev));
              setJsonError('');
              markChange();
            } catch {
              setJsonError('Invalid JSON');
            }
          }}
          onJsonBlur={() => {
            if (jsonError) return;
            jsonEditingRef.current = false;
            const json = mergeSearchEditorIntoJson(jsonDraft, searchEditorRef.current);
            setJsonDraft(json);
            setCurrentOperation((prev: any) => (prev ? { ...prev, ApiconfigParameters: json } : prev));
          }}
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

export default AppDataPresentationApiEditor;
