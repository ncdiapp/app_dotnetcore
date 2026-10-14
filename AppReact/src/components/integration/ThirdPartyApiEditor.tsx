/**
 * Third Party API Editor (API Operation – create/edit)
 * For Simple Query type: full UI matching Angular IntegrationSettingParameterEditor (tabs: API JSON Query, API Call Test;
 * Send Request, Refresh, Save Setting; Query Parameters, SQL/JSON Query, Execute Query, JSON Sample Data).
 * For other types: minimal form + note to use Angular for full schema mapping editor.
 * Reference: Angular integrationSettingParameterEditorCtrl.js, IntegrationSettingParameterEditor.cshtml
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { updateActiveTabPath, updateCurrentTabLabel } from '../../redux/features/ui/navigation/tabnavSlice';
import { endpoints, toApiDisplayUrl } from '../../webapi/endpoints';
import { useApiServerRoot } from '../../redux/hooks/useApiServerRoot';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { normalizeIntegrationSettingParameterForSave, prettyPrintJsonForDisplay } from '../../helper/integrationPayloadHelper';
import {
  isAbsoluteHttpUrl,
  isUsedByMcpValue,
  missingPathPlaceholders,
  parseApiConfig,
  serializeApiConfig,
  type ApiConfigFormModel,
} from '../../helper/apiConfigForm';
import { ApiConfigParametersForm, IsUsedByMcpCheckbox, type ApiConfigFormHandle } from './ApiConfigParametersForm';
import { ApiEditorWorkArea } from './ApiEditorWorkArea';
import { JsonCodeViewer } from '../common/JsonCodeViewer';
import { JsonCodeEditor } from '../common/JsonCodeEditor';

const HTTP_METHODS = ['Get', 'Post', 'Put', 'Delete'];

/** Default Api Config Parameters (match Angular initNewIntegrationSettingData) */
const DEFAULT_APICONFIG_PARAMETERS = `{
  "BaseUrl": "",
  "Url": "",
  "Headers": {},
  "QueryParams": {},
  "PathParams": {},
  "PostProcessMethodName": null,
  "ResponseObjectMapToEnvionmentVariable": {},
  "ResponseHeaderNeedToSetCookieNames": []
}`;

/** Payload data type (match Angular EmAppApiPayloadDataType) */
const PAYLOAD_DATA_TYPES = [
  { Id: 1, Display: 'JSON String' },
  { Id: 2, Display: 'Binary File' },
  { Id: 3, Display: 'Server File Path, Web File Url, Or Ftp File Path' },
];

type QueryParameterItem = { ParameterName: string; defaultValue?: string };
type EnvVarItem = { Key: string; Value: string };
const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
};

const ThirdPartyApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const [searchParams] = useSearchParams();
  const param2Str = searchParams.get('param2');
  const idQuery = searchParams.get('Id'); // When creating from ThirdPartyApiProviderEditor, Id is the integration setting id
  const settingIdFromQuery = idQuery ? (isNaN(Number(idQuery)) ? null : Number(idQuery)) : (param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.integrationSettingId ?? p.IntegrationSettingId ?? null; } catch { return null; } })() : null);
  const dataSourceIdFromQuery = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.dataSourceId ?? null; } catch { return null; } })() : null;

  // Param may be a plain number (e.g. "123") or JSON from addTabAndNavigate (e.g. {"id":123}) – e.g. RestApiImportEditor "From API Operation" edit
  const settingParameterId = (() => {
    if (!idParam) return null;
    const asNum = Number(idParam);
    if (!Number.isNaN(asNum)) return asNum;
    try {
      const decoded = decodeURIComponent(idParam);
      const obj = typeof decoded === 'string' && decoded.startsWith('{') ? JSON.parse(decoded) : null;
      const id = obj?.id ?? obj?.Id ?? null;
      return id != null ? Number(id) : null;
    } catch {
      return null;
    }
  })();
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const serverRoot = useApiServerRoot();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [integrationSettingId, setIntegrationSettingId] = useState<number | null>(settingIdFromQuery ?? null);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [queryParameterList, setQueryParameterList] = useState<QueryParameterItem[]>([]);
  const [queryParameterCV, setQueryParameterCV] = useState<CollectionView<QueryParameterItem> | null>(null);
  const [dataSourceMap, setDataSourceMap] = useState<Record<number, any>>({});
  const [queryResultText, setQueryResultText] = useState('');
  const [queryResultError, setQueryResultError] = useState('');
  const [activeTabIndex, setActiveTabIndex] = useState(0); // 0 = API JSON Query, 1 = API Call Test
  const [environmentVariableList, setEnvironmentVariableList] = useState<EnvVarItem[]>([]);
  const [responseJsonData, setResponseJsonData] = useState(''); // REST mode: response after Send Request
  const [envVarDropdownOpen, setEnvVarDropdownOpen] = useState(false);
  const [pathParamDropdownOpen, setPathParamDropdownOpen] = useState(false);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const configFormRef = useRef<ApiConfigFormHandle | null>(null);
  const writtenConfigRef = useRef<string | null>(null);
  const jsonEditingRef = useRef(false);
  const [jsonDraft, setJsonDraft] = useState('');
  const [jsonError, setJsonError] = useState('');
  const [configModel, setConfigModel] = useState<ApiConfigFormModel>(() => parseApiConfig(DEFAULT_APICONFIG_PARAMETERS));
  const configModelRef = useRef(configModel);
  useEffect(() => {
    configModelRef.current = configModel;
  }, [configModel]);
  useEffect(() => {
    if (!currentOperation) return;
    const source = currentOperation.ApiconfigParameters ?? '';
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
    writtenConfigRef.current = saved.json;
    setConfigModel(canonical ? saved.model : model);
    setCurrentOperation((prev: any) => (prev ? { ...prev, ApiconfigParameters: saved.json, APIConfigParameters: null } : prev));
    if (!jsonEditingRef.current) {
      setJsonDraft(saved.json);
      setJsonError('');
    }
  }, []);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const dataSourceList = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
      const list = Array.isArray(dataSourceList) ? dataSourceList : [];
      const dict: Record<number, any> = {};
      list.forEach((ds: any) => { dict[ds.Id] = ds; });
      setDataSourceMap(dict);

      let intSettingIdForEnv: number | null = settingIdFromQuery ?? null;

      if (settingParameterId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(settingParameterId), true);
        if (settingData) {
          intSettingIdForEnv = (settingData as any).IntergrationSettingId ?? settingIdFromQuery ?? null;
          setIntegrationSettingId(settingData.IntergrationSettingId ?? settingIdFromQuery ?? null);
          // Keep full server DTO so save payload matches Angular (all fields present)
          setCurrentOperation(normalizeIntegrationSettingParameterForSave({ ...settingData }));
          const names = settingData.SimpleQueryParameterNameList ?? [];
          const params = names.filter((n: string) => n && n !== '@').map((n: string) => ({ ParameterName: n, defaultValue: '' }));
          setQueryParameterList(params);
          setQueryParameterCV(new CollectionView(params));
          // Response: GET uses JsonSampleData; POST/PUT use PostResponseDto.ResponseJsonData
          const d = settingData as any;
          const methodLoad = (d.HttpMethd ?? '').toString().toLowerCase();
          const isPostOrPutLoad = methodLoad === 'post' || methodLoad === 'put';
          const rawResponse = isPostOrPutLoad
            ? (d.PostResponseDto?.ResponseJsonData ?? '')
            : (d.JsonSampleData ?? d.ResponseJsonData ?? d.Object?.JsonSampleData ?? '');
          setResponseJsonData(prettyPrintJsonForDisplay(rawResponse));
        }
      } else {
        const dataSourceId = dataSourceIdFromQuery ?? (list[0] as any)?.Id ?? null;
        const intSettingId = settingIdFromQuery ?? null;
        setIntegrationSettingId(intSettingId);
        // Third Party API = REST API type only (no Simple Query type)
        setCurrentOperation({
          Id: null,
          ActionCode: 'NewOperation',
          HttpMethd: 'Get',
          ActionDescription: '',
          DataSourceId: dataSourceId,
          IntergrationSettingId: intSettingId,
          JsonQuery: '',
          JsonSampleData: '',
          SimpleQueryParameterNameList: [],
          IsSimpleQuery: false,
          IsUsedByMcp: false,
          ApiconfigParameters: DEFAULT_APICONFIG_PARAMETERS,
          OtherSettingsDto: { PayloadDataType: 1, IsNeedToGenerateStagingTables: false },
        });
        setQueryParameterList([]);
        setQueryParameterCV(new CollectionView<QueryParameterItem>([]));
      }
      setQueryResultText('');
      setQueryResultError('');
      if (settingParameterId == null) setResponseJsonData('');
      setIsModified(false);

      if (intSettingIdForEnv != null) {
        try {
          const providerData = await integrationService.retrieveOneAppIntegrationSettingExDto(String(intSettingIdForEnv));
          const dict = providerData?.OtherSettingsDto?.DictEnvironmentVariable ?? {};
          const envList: EnvVarItem[] = Object.entries(dict).map(([Key, Value]) => ({ Key, Value: typeof Value === 'string' ? Value : String(Value ?? '') }));
          setEnvironmentVariableList(envList);
        } catch {
          setEnvironmentVariableList([]);
        }
      } else {
        setEnvironmentVariableList([]);
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, settingIdFromQuery, dataSourceIdFromQuery, dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const syncQueryParamsFromCV = useCallback(() => {
    if (!queryParameterCV) return;
    const names = (queryParameterCV.items as QueryParameterItem[]).map((p) => (p.ParameterName || '').trim()).filter((n) => n && n !== '@');
    setCurrentOperation((prev: any) => (prev ? { ...prev, SimpleQueryParameterNameList: names } : prev));
    setIsModified(true);
  }, [queryParameterCV]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);
  const markChange = useCallback(() => setIsModified(true), []);

  const handleSave = useCallback(async () => {
    const op = currentOperation;
    if (!op) return;
    if (op.IntergrationSettingId == null) {
      errorMessage.showWarning('Integration Setting is required. Open from the 3rd Part API Provider editor.');
      return;
    }
    const paramNames = (queryParameterCV?.items as QueryParameterItem[] ?? queryParameterList)
      .map((p) => (p.ParameterName || '').trim())
      .filter((n) => n && n !== '@');
    const savedConfig = serializeApiConfig(configModelRef.current);
    if (!isAbsoluteHttpUrl(savedConfig.model.apiUrl)) {
      errorMessage.showWarning('API URL must start with http:// or https://.');
      return;
    }
    const missing = missingPathPlaceholders(savedConfig.model);
    if (missing.length) {
      errorMessage.showWarning(`Path param ${missing.join(', ')} is not in the API URL.`);
    }
    const methodSave = (op.HttpMethd ?? '').toString().toLowerCase();
    const isPostOrPut = methodSave === 'post' || methodSave === 'put';
    const configPayload = {
      ...op,
      ApiconfigParameters: savedConfig.json,
      APIConfigParameters: null,
      IsUsedByMcp: isUsedByMcpValue(op.IsUsedByMcp),
      SimpleQueryParameterNameList: paramNames,
    };
    const payload = isPostOrPut
      ? { ...configPayload, PostResponseDto: { ...(op as any).PostResponseDto, ResponseJsonData: responseJsonData } }
      : { ...configPayload, JsonSampleData: responseJsonData };
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        errorMessage.showInfo('Saved successfully.');
        setIsModified(false);
        const saved = result?.Object;
        const savedId = saved?.Id != null ? Number(saved.Id) : null;
        if (savedId != null && !Number.isNaN(savedId)) {
          const newPath = `/third-party-api-editor/${savedId}`;
          const tabLabel = saved?.ActionCode ? `API: ${saved.ActionCode}` : `API (${savedId})`;
          // Must update tab path before/with navigate; otherwise useTabNavigation syncs
          // the URL back to the create URL (no operation id) and remounts empty.
          dispatch(updateActiveTabPath(newPath));
          dispatch(updateCurrentTabLabel(tabLabel));
          if (savedId !== settingParameterId) {
            navigate(newPath, { replace: true });
          } else {
            await loadData();
          }
        } else {
          await loadData();
        }
      } else if (messages.length) {
        messages.forEach((msg) => errorMessage.showError(msg));
      } else {
        errorMessage.showError('Failed to save.');
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsSaving(false);
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, queryParameterCV, queryParameterList, responseJsonData, dispatch, errorMessage, loadData, navigate, settingParameterId]);

  const addQueryParameter = useCallback(() => {
    const next = [...queryParameterList, { ParameterName: '' }];
    setQueryParameterList(next);
    setQueryParameterCV(new CollectionView(next));
    setIsModified(true);
  }, [queryParameterList]);

  const removeQueryParameter = useCallback(() => {
    const flex = flexRef.current;
    if (!flex || !queryParameterCV) return;
    const sel = flex.selection;
    if (sel?.row == null || sel.row < 0) {
      errorMessage.showWarning('Select a row to remove.');
      return;
    }
    const items = (queryParameterCV.items as QueryParameterItem[]).slice();
    items.splice(sel.row, 1);
    setQueryParameterList(items);
    setQueryParameterCV(new CollectionView(items));
    setIsModified(true);
  }, [queryParameterCV, errorMessage]);

  const executeQuery = useCallback(async () => {
    const op = currentOperation;
    if (!op?.DataSourceId || !op?.JsonQuery?.trim()) {
      errorMessage.showWarning('Set Data Source and JSON Query first.');
      return;
    }
    setQueryResultError('');
    setQueryResultText('');
    dispatch(setIsBusy());
    try {
      const result = await schemaMetadataService.executeQueryResult({
        Key: op.DataSourceId,
        Value: op.JsonQuery.trim(),
      });
      if (result?.DataRowList?.length === 1 && result.DataRowList[0]['JSON']) {
        setQueryResultText(result.DataRowList[0]['JSON'] || '');
      } else {
        setQueryResultText(JSON.stringify(result ?? {}, null, 2));
      }
      if (result?.ErrorMessage) {
        setQueryResultError(result.ErrorMessage);
        if (result.ErrorMessage !== 'Query completed successfully') {
          errorMessage.showWarning(result.ErrorMessage);
        }
      }
    } catch (error) {
      const msg = error instanceof Error ? error.message : String(error);
      setQueryResultError(msg);
      errorMessage.showError(msg);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, dispatch, errorMessage]);

  const pathParamsList = configModel.pathParams.map((row) => row.key.trim()).filter(Boolean);

  const insertEnvironmentVariable = useCallback((key: string) => {
    if (!key) return;
    configFormRef.current?.insertIntoApiUrl(`{{Env.${key}}}`);
    setEnvVarDropdownOpen(false);
    markChange();
  }, [markChange]);

  const insertPathParameter = useCallback((paramName: string) => {
    if (!paramName) return;
    configFormRef.current?.insertIntoApiUrl(`{{PathParams.${paramName}}}`);
    setPathParamDropdownOpen(false);
    markChange();
  }, [markChange]);

  const sendRequestRest = useCallback(async () => {
    const op = currentOperation;
    if (!op?.Id) {
      errorMessage.showWarning('Save the operation first, then use Send Request.');
      return;
    }
    if (!isAbsoluteHttpUrl(configModelRef.current.apiUrl)) {
      errorMessage.showWarning('API URL must start with http:// or https://.');
      return;
    }
    setResponseJsonData('');
    dispatch(setIsBusy());
    try {
      const result = await integrationService.generateSampleJsonDataFromApiConfig(op);
      if (result?.IsSuccessful && result?.Object) {
        const method = (op?.HttpMethd ?? '').toString().toLowerCase();
        const isPostOrPutSend = method === 'post' || method === 'put';
        const obj = result.Object as any;
        const raw = isPostOrPutSend
          ? (obj.PostResponseDto?.ResponseJsonData ?? obj.JsonSampleData ?? obj)
          : (obj.JsonSampleData ?? obj);
        const formatted = prettyPrintJsonForDisplay(raw);
        setResponseJsonData(formatted);
        setCurrentOperation((prev: any) => {
          if (!prev) return prev;
          if (isPostOrPutSend) return { ...prev, PostResponseDto: { ...prev.PostResponseDto, ResponseJsonData: formatted } };
          return { ...prev, JsonSampleData: formatted };
        });
      } else {
        const messages = extractValidationMessages(result?.ValidationResult);
        if (messages.length) messages.forEach((m) => errorMessage.showError(m));
        else errorMessage.showError('Send Request failed.');
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, dispatch, errorMessage]);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  const op = currentOperation;


  const isPostOrPut = op?.HttpMethd === 'Post' || op?.HttpMethd === 'Put';
  const payloadDataType = op?.OtherSettingsDto?.PayloadDataType ?? 1;
  const displayApiUrl = (() => {
    if (!op?.ActionCode) return '';
    const base = `${endpoints.BASE_URL}/webapi/DataIntegration/${op.ActionCode}`;
    const method = String(op.HttpMethd ?? 'Get');
    if (method !== 'Get' && method !== 'Delete') return toApiDisplayUrl(serverRoot, base);
    try {
      const cfg = JSON.parse(op.ApiconfigParameters || '{}');
      const queryParams = cfg?.QueryParams;
      if (!queryParams || typeof queryParams !== 'object') return toApiDisplayUrl(serverRoot, base);
      const pairs = Object.entries(queryParams)
        .filter(([key, value]) => key && value != null && String(value) !== '')
        .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`);
      const path = pairs.length ? `${base}?${pairs.join('&')}` : base;
      return toApiDisplayUrl(serverRoot, path);
    } catch {
      return toApiDisplayUrl(serverRoot, base);
    }
  })();


  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>API: {op?.ActionCode ?? '—'}</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={sendRequestRest} disabled={isSaving || !op?.Id}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-amber-500 hover:bg-amber-600" title="Send Request">
            <i className="fa fa-bolt" aria-hidden /> Send Request
          </button>
          <button type="button" onClick={handleRefresh} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa fa-refresh" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleSave} disabled={!isModified || isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600" title="Save Setting">
            <i className="fa fa-save" aria-hidden /> Save Setting
          </button>
        </div>
      </div>

      <div className={`h-1 flex-auto flex flex-col min-h-0 overflow-hidden p-3 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-4 mb-3 flex-shrink-0">
          <div className="flex items-center gap-2 flex-shrink-0">
            <label className={`flex-shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Operation Code</label>
            <input type="text" value={op?.ActionCode ?? ''} onChange={(e) => { setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev)); markChange(); }} className={`w-[200px] flex-shrink-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} />
          </div>
          <div className="flex items-center gap-2 flex-shrink-0">
            <label className={`flex-shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Http Method</label>
            <select value={op?.HttpMethd ?? 'Get'} onChange={(e) => { setCurrentOperation((prev: any) => (prev ? { ...prev, HttpMethd: e.target.value } : prev)); markChange(); }} className={`w-[200px] flex-shrink-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}>
              {HTTP_METHODS.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
            <IsUsedByMcpCheckbox
              checked={isUsedByMcpValue(op?.IsUsedByMcp)}
              onChange={(checked) => {
                setCurrentOperation((prev: any) => (prev ? { ...prev, IsUsedByMcp: checked } : prev));
                markChange();
              }}
            />
          </div>
          <div className="flex items-center gap-2 w-1 flex-auto min-w-0">
            <label className={`flex-shrink-0 text-xs whitespace-nowrap ${theme.label}`}>Description</label>
            <input type="text" value={op?.ActionDescription ?? ''} onChange={(e) => { setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev)); markChange(); }} className={`w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`} />
          </div>
        </div>

        <ApiEditorWorkArea
          apiUrl={displayApiUrl}
          showPayload={isPostOrPut}
          formExtra={(
            <>
              {environmentVariableList.length > 0 && (
                <div className="relative">
                  <button type="button" onClick={() => { setEnvVarDropdownOpen((v) => !v); setPathParamDropdownOpen(false); }} className={`h-6 px-2 rounded-[4px] text-xs border ${theme.button_default}`}>
                    <i className="fa-solid fa-file-code mr-1" aria-hidden /> Insert Environment Variable <i className="fa-solid fa-caret-down ml-1" aria-hidden />
                  </button>
                  {envVarDropdownOpen && (
                    <div className="absolute right-0 top-full mt-1 z-10 min-w-[160px] border rounded shadow-lg bg-white py-1 max-h-48 overflow-auto">
                      {environmentVariableList.filter((e) => e.Key).map((e) => (
                        <button key={e.Key} type="button" className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100 flex items-center" onClick={() => insertEnvironmentVariable(e.Key)} title={`${e.Key}: ${e.Value}`}>
                          <i className="fa-solid fa-copy mr-2" aria-hidden /> {e.Key}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}
              {pathParamsList.length > 0 && (
                <div className="relative">
                  <button type="button" onClick={() => { setPathParamDropdownOpen((v) => !v); setEnvVarDropdownOpen(false); }} className={`h-6 px-2 rounded-[4px] text-xs border ${theme.button_default}`}>
                    <i className="fa-solid fa-file-code mr-1" aria-hidden /> Insert Path Parameter <i className="fa-solid fa-caret-down ml-1" aria-hidden />
                  </button>
                  {pathParamDropdownOpen && (
                    <div className="absolute right-0 top-full mt-1 z-10 min-w-[140px] border rounded shadow-lg bg-white py-1">
                      {pathParamsList.map((name) => (
                        <button key={name} type="button" className="w-full text-left px-3 py-1.5 text-xs hover:bg-gray-100 flex items-center" onClick={() => insertPathParameter(name)}>
                          <i className="fa-solid fa-copy mr-2" aria-hidden /> {name}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </>
          )}
          form={(
            <ApiConfigParametersForm
              ref={configFormRef}
              model={configModel}
              onChange={(model) => {
                jsonEditingRef.current = false;
                applyConfigModel(model, false);
                markChange();
              }}
              onApiUrlBlur={() => {
                jsonEditingRef.current = false;
                applyConfigModel(configModelRef.current, true);
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
            if (!jsonError) setJsonDraft(serializeApiConfig(configModelRef.current).json);
          }}
          payload={(
            <div className="flex flex-col h-full min-h-0 p-2">
              <div className="flex items-center flex-wrap gap-2 mb-2 flex-shrink-0">
                <span className={`text-xs ${theme.label}`}>Type</span>
                <select
                  value={payloadDataType}
                  onChange={(e) => {
                    const v = Number(e.target.value);
                    setCurrentOperation((prev: any) => (prev ? { ...prev, OtherSettingsDto: { ...prev.OtherSettingsDto, PayloadDataType: v } } : prev));
                    markChange();
                  }}
                  className={`h-7 px-2 border rounded-[4px] text-xs w-[280px] ${theme.inputBox}`}
                >
                  {PAYLOAD_DATA_TYPES.map((t) => (
                    <option key={t.Id} value={t.Id}>{t.Display}</option>
                  ))}
                </select>
                {(payloadDataType === 2 || payloadDataType === 3) && (
                  <label className={`flex items-center gap-1 text-xs ${theme.label}`}>
                    <input type="checkbox" checked={!!op?.OtherSettingsDto?.IsMultipartFormDataContent} onChange={(e) => { setCurrentOperation((prev: any) => (prev ? { ...prev, OtherSettingsDto: { ...prev.OtherSettingsDto, IsMultipartFormDataContent: e.target.checked } } : prev)); markChange(); }} />
                    Is Multipart Form Data Content
                  </label>
                )}
              </div>
              <div className="h-1 flex-auto min-h-0">
                {payloadDataType === 1 && (
                  <div className="w-full h-full border rounded overflow-hidden bg-white">
                    <JsonCodeEditor
                      value={op?.JsonSampleData ?? ''}
                      onChange={(next) => {
                        setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: next } : prev));
                        markChange();
                      }}
                      placeholder='e.g. {"key": "value"}'
                      className="w-full h-full"
                    />
                  </div>
                )}
                {payloadDataType === 2 && <div className={`p-2 text-xs ${theme.label}`}>Use Angular app or attach file for Binary File payload.</div>}
                {payloadDataType === 3 && <div className={`p-2 text-xs ${theme.label}`}>Use Angular app for Server File Path / Web URL / Ftp.</div>}
              </div>
            </div>
          )}
          result={(
            <JsonCodeViewer
              text={responseJsonData}
              placeholder="Response will appear after Send Request"
              className="w-full h-1 flex-auto min-h-0 p-2 border rounded font-mono text-xs bg-gray-50"
            />
          )}
        />
      </div>
    </div>
  );

};

export default ThirdPartyApiEditor;
