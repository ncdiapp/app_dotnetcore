/**
 * App Json Query API Editor (SQL Json Query API – create/edit)
 * Reference: Angular apiBuilderEditorCtrl.js, ApiBuilderEditor.cshtml
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
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { endpoints } from '../../webapi/endpoints';
import { getHeaders } from '../../helper/apiServiceHelper';
import { prettyPrintJsonForDisplay } from '../../helper/integrationPayloadHelper';
import { JsonCodeViewer } from '../common/JsonCodeViewer';
import { JsonCodeEditor } from '../common/JsonCodeEditor';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;
const HTTP_METHODS = ['Get', 'Post', 'Put', 'Delete'];

type QueryParameterItem = { ParameterName: string; defaultValue?: string };

/** Ensure param name starts with '@'; return trimmed name or empty. */
function ensureParamNameStartsWithAt(name: string): string {
  const n = (name || '').trim();
  if (!n || n === '@') return n;
  return n.startsWith('@') ? n : '@' + n;
}

/** Build minimal new-API DTO like Angular initNewApiData(). */
function initNewApiDto(dataSourceId: number | null, dataSourceMap: Record<number, any>): any {
  const ds = dataSourceId != null ? dataSourceMap[dataSourceId] : null;
  return {
    IsSimpleQuery: true,
    IntergrationSettingId: API_BUILDER_INTEGRATION_SETTING_ID,
    DataSourceId: dataSourceId,
    HttpMethd: 'Get',
    ActionCode: 'NewAPI',
    JsonQuery: 'SELECT * FROM [MyTableName] FOR JSON PATH',
    SimpleQueryParameterNameList: [] as string[],
    DataSourceType: ds?.DataSourceType ?? null,
  };
}

const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
};

const AppJsonQueryApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const [searchParams] = useSearchParams();
  const param2Str = searchParams.get('param2');
  const settingParameterId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const initialDataSourceId = param2Str ? (() => { try { const p = JSON.parse(param2Str); return p.dataSourceId ?? null; } catch { return null; } })() : null;

  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [queryParameterList, setQueryParameterList] = useState<QueryParameterItem[]>([]);
  const [queryParameterCV, setQueryParameterCV] = useState<CollectionView<QueryParameterItem> | null>(null);
  const [dataSourceMap, setDataSourceMap] = useState<Record<number, { DataSourceName?: string; DatabaseName?: string }>>({});
  const [queryResultText, setQueryResultText] = useState('');
  const [queryResultError, setQueryResultError] = useState('');
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [apiResponseText, setApiResponseText] = useState('');
  const [exampleTabIndex, setExampleTabIndex] = useState(0); // 0 = Select Table To JSON, 1 = Update Table From JSON
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const latestOperationRef = useRef<any>(null);
  const latestQueryParameterCVRef = useRef<CollectionView<QueryParameterItem> | null>(null);

  useEffect(() => {
    latestOperationRef.current = currentOperation;
  }, [currentOperation]);
  useEffect(() => {
    latestQueryParameterCVRef.current = queryParameterCV;
  }, [queryParameterCV]);

  const SQL_JSON_DOCS_URL = 'https://docs.microsoft.com/en-us/sql/relational-databases/json/format-query-results-as-json-with-for-json-sql-server?view=sql-server-ver16';

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const dataSourceList = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
      const list = Array.isArray(dataSourceList) ? dataSourceList : [];
      const dict: Record<number, any> = {};
      list.forEach((ds: any) => { dict[ds.Id] = ds; });
      setDataSourceMap(dict);

      if (settingParameterId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(settingParameterId), false);
        if (settingData) {
          setCurrentOperation(settingData);
          const names = settingData.SimpleQueryParameterNameList ?? [];
          const params = names
            .map((n: string) => ensureParamNameStartsWithAt(n || ''))
            .filter((n: string) => n && n !== '@')
            .map((n: string) => ({ ParameterName: n, defaultValue: '' }));
          setQueryParameterList(params);
          setQueryParameterCV(new CollectionView(params));
        } else {
          const dataSourceId = (list[0] as any)?.Id ?? null;
          setCurrentOperation(initNewApiDto(dataSourceId, dict));
          setQueryParameterList([]);
          setQueryParameterCV(new CollectionView<QueryParameterItem>([]));
        }
      } else {
        const dataSourceId = initialDataSourceId ?? (list[0] as any)?.Id ?? null;
        setCurrentOperation(initNewApiDto(dataSourceId, dict));
        setQueryParameterList([]);
        setQueryParameterCV(new CollectionView<QueryParameterItem>([]));
      }
      setQueryResultText('');
      setQueryResultError('');
      setIsModified(false);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, initialDataSourceId, dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const syncQueryParamsFromCV = useCallback(() => {
    if (!queryParameterCV) return;
    const items = queryParameterCV.items as QueryParameterItem[];
    for (const p of items) {
      const normalized = ensureParamNameStartsWithAt(p.ParameterName || '');
      if (normalized) p.ParameterName = normalized;
    }
    const names = items
      .map((p) => ensureParamNameStartsWithAt(p.ParameterName || ''))
      .filter((n) => n && n !== '@');
    setCurrentOperation((prev: any) => (prev ? { ...prev, SimpleQueryParameterNameList: names } : prev));
    setIsModified(true);
  }, [queryParameterCV]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const handleSave = useCallback(async () => {
    const op = latestOperationRef.current ?? currentOperation;
    if (!op) return;
    const cv = latestQueryParameterCVRef.current ?? queryParameterCV;
    const items = (cv?.items as QueryParameterItem[] | undefined) ?? queryParameterList;
    const paramNames = items
      .map((p) => ensureParamNameStartsWithAt(p.ParameterName || ''))
      .filter((n) => n && n !== '@');
    const queryParams = paramNames.reduce<Record<string, string>>((acc, n) => ({ ...acc, [n]: '' }), {});
    const apiConfigObj = {
      ...(op.APIConfigParameters && typeof op.APIConfigParameters === 'object' ? op.APIConfigParameters : {}),
      QueryParams: queryParams,
    };
    const payload = {
      ...op,
      SimpleQueryParameterNameList: paramNames,
      APIConfigParameters: apiConfigObj,
      ApiconfigParameters: JSON.stringify(apiConfigObj),
    };
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        errorMessage.showInfo('Saved successfully.');
        setIsModified(false);
        if (result?.Object?.Id != null) {
          navigate(`/api-builder-editor/${result.Object.Id}`, { replace: true });
        }
        loadData();
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
  }, [currentOperation, queryParameterCV, queryParameterList, dispatch, errorMessage, loadData, navigate]);

  const addQueryParameter = useCallback(() => {
    const next = [...queryParameterList, { ParameterName: '@' }];
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
    const items = queryParameterCV.items.slice() as QueryParameterItem[];
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

  const buildTestApiUrl = useCallback(() => {
    const op = currentOperation;
    if (!op?.ActionCode) return '';
    const base = `${endpoints.BASE_URL}/webapi/DataIntegration/${op.ActionCode}`;
    const items = (queryParameterCV?.items ?? queryParameterList) as QueryParameterItem[];
    const pairs = items
      .filter((p) => (p.ParameterName || '').trim() !== '')
      .map((p) => {
        const name = (p.ParameterName || '').trim();
        const queryKey = name.startsWith('@') ? name.slice(1) : name;
        const value = String(p.defaultValue ?? '').trim();
        return `${encodeURIComponent(queryKey)}=${encodeURIComponent(value)}`;
      });
    if (pairs.length === 0) return base;
    return `${base}?${pairs.join('&')}`;
  }, [currentOperation, queryParameterCV, queryParameterList]);

  const buildTestApiFullUrl = useCallback(() => {
    const pathWithQuery = buildTestApiUrl();
    if (!pathWithQuery) return '';
    if (pathWithQuery.startsWith('http')) return pathWithQuery;

    const normalizedBase = (endpoints.BASE_URL || '').replace(/\/$/, '');
    const pathAfterBase = normalizedBase && pathWithQuery.startsWith(normalizedBase)
      ? pathWithQuery.slice(normalizedBase.length)
      : pathWithQuery;

    return endpoints.buildEndpointUrl(pathAfterBase);
  }, [buildTestApiUrl]);

  const handleTestApi = useCallback(async () => {
    const op = currentOperation;
    if (!op?.Id || !op?.ActionCode) {
      errorMessage.showWarning('Save the API first to test.');
      return;
    }
    const apiUrl = buildTestApiFullUrl();
    setApiResponseText('');
    dispatch(setIsBusy());
    try {
      const fetchOpts: RequestInit = {
        headers: getHeaders(),
        credentials: 'include',
      };
      if (op.HttpMethd === 'Get' || op.HttpMethd === 'Delete') {
        const response = await fetch(apiUrl, fetchOpts);
        if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        const rawText = await response.text();
        const displayed = rawText.trim() ? prettyPrintJsonForDisplay(rawText) : rawText || '""';
        setApiResponseText(displayed);
      } else {
        const body = op.JsonSampleData?.trim() ? JSON.parse(op.JsonSampleData) : {};
        const response = await fetch(apiUrl, {
          ...fetchOpts,
          method: 'POST',
          body: JSON.stringify(body),
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        const rawText = await response.text();
        const displayed = rawText.trim() ? prettyPrintJsonForDisplay(rawText) : rawText || '""';
        setApiResponseText(displayed);
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      setApiResponseText(`Error: ${msg}`);
      errorMessage.showError(msg);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, buildTestApiFullUrl, dispatch, errorMessage]);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  const databaseName = currentOperation?.DataSourceId != null
    ? dataSourceMap[currentOperation?.DataSourceId as number]?.DatabaseName ?? dataSourceMap[currentOperation?.DataSourceId as number]?.DataSourceName ?? ''
    : '';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>API: {currentOperation?.ActionCode ?? '—'}</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={handleRefresh} disabled={isSaving}
            className="h-6 px-2 inline-flex items-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500" title="Refresh">
            <i className="fa fa-refresh" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleSave} disabled={!isModified || isSaving}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600" title="Save Setting">
            <i className="fa fa-save" aria-hidden /> Save Setting
          </button>
        </div>
      </div>

      <div className={`flex-1 flex flex-col min-h-0 overflow-auto p-3 ${theme.mainContentSection}`}>
        <div className="flex flex-wrap gap-6 mb-3 flex-shrink-0">
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Operation Code</label>
            <input type="text" autoComplete="off" value={currentOperation?.ActionCode ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev));
              setIsModified(true);
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Description</label>
            <input type="text" autoComplete="off" value={currentOperation?.ActionDescription ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev));
              setIsModified(true);
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Http Method</label>
            <select value={currentOperation?.HttpMethd ?? 'Get'} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, HttpMethd: e.target.value } : prev));
              setIsModified(true);
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs">
              {HTTP_METHODS.map((m) => (
                <option key={m} value={m}>{m}</option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Database</label>
            <input type="text" readOnly value={databaseName} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
        </div>

        <div className="flex gap-0 border-b flex-shrink-0 mb-3">
          <button type="button" onClick={() => setActiveTabIndex(0)}
            className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${activeTabIndex === 0 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}>
            API JSON Query
          </button>
          <button type="button" onClick={() => setActiveTabIndex(1)}
            className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${activeTabIndex === 1 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}>
            API Call Test
          </button>
        </div>

        {activeTabIndex === 0 && (
          <div className="flex-1 flex gap-4 min-h-0 min-w-0">
            <div className="flex-1 flex flex-col min-h-0 min-w-0 gap-4">
              <div className={`flex-1 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
                <div className="flex items-center justify-between px-2 py-1.5 border-b bg-gray-100">
                  <span className="text-xs font-semibold">
                    {currentOperation?.HttpMethd === 'Get' || currentOperation?.HttpMethd === 'Delete'
                      ? 'JSON Query - Get Data:'
                      : 'JSON Query - Post Data:'}
                  </span>
                  <div className="flex items-center gap-1">
                    <button type="button" className="h-6 px-2 rounded text-xs border bg-white inline-flex items-center gap-1" title="Design Query">
                      <i className="fa fa-database" aria-hidden /> Design Query
                    </button>
                    <button type="button" onClick={executeQuery} disabled={currentOperation?.HttpMethd !== 'Get'}
                      className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50 inline-flex items-center gap-1" title="Execute Query">
                      <i className="fa fa-exclamation" aria-hidden /> Execute Query
                    </button>
                  </div>
                </div>
                <div className="flex-1 flex gap-4 p-2 min-h-0">
                  <div className="flex flex-col gap-2 min-w-0 w-1/3 flex-shrink-0">
                    <div className="flex items-center justify-between flex-wrap gap-1">
                      <span className="text-xs font-semibold">Query Parameters</span>
                      <div className="flex items-center gap-1">
                        <button type="button" onClick={addQueryParameter} className="h-6 px-2 rounded text-xs border bg-white inline-flex items-center gap-1">
                          <i className="fa fa-plus" aria-hidden /> Add
                        </button>
                        <button type="button" onClick={removeQueryParameter} className="h-6 px-2 rounded text-xs border bg-white inline-flex items-center gap-1">
                          <i className="fa fa-trash-o" aria-hidden /> Remove
                        </button>
                      </div>
                    </div>
                    <p className="text-xs text-gray-600">Copy the parameter into query text.</p>
                    <div className="flex-1 min-h-[80px] border rounded overflow-hidden">
                      <FlexGrid
                        ref={flexRef}
                        itemsSource={queryParameterCV ?? undefined}
                        autoGenerateColumns={false}
                        selectionMode="Row"
                        isReadOnly={false}
                        cellEditEnded={() => syncQueryParamsFromCV()}
                        initialized={(f: wjGrid.FlexGrid) => { flexRef.current = f; }}
                        className="w-full h-full"
                      >
                        <FlexGridColumn binding="ParameterName" header="Parameter Name" width="*" />
                        <FlexGridColumn binding="defaultValue" header="Test Value" width="*" />
                      </FlexGrid>
                    </div>
                  </div>
                  <div className="flex-1 flex flex-col gap-2 min-w-0 min-h-0">
                    <span className="text-xs font-semibold flex-shrink-0">SQL / JSON Query</span>
                    <textarea
                      value={currentOperation?.JsonQuery ?? ''}
                      onChange={(e) => {
                        setCurrentOperation((prev: any) => (prev ? { ...prev, JsonQuery: e.target.value } : prev));
                        setIsModified(true);
                      }}
                      className="w-full flex-1 min-h-0 p-2 border rounded font-mono text-xs resize-none"
                      placeholder="e.g. SELECT * FROM [MyTableName] FOR JSON PATH"
                      spellCheck={false}
                    />
                  </div>
                </div>
              </div>
              {(currentOperation?.HttpMethd === 'Get' || currentOperation?.HttpMethd === 'Delete') && (
                <div className={`flex-1 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
                  <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                    <span className="text-xs font-semibold">Query Result</span>
                  </div>
                  <div className="flex-1 flex flex-col p-2 min-h-0">
                    {queryResultError && (
                      <div className="p-2 rounded text-xs border mb-2 bg-gray-50 flex-shrink-0">{queryResultError}</div>
                    )}
                    <JsonCodeViewer
                      text={queryResultText}
                      className="w-full flex-1 min-h-0 p-2 border rounded font-mono text-xs bg-gray-50 resize-none"
                    />
                  </div>
                </div>
              )}
            </div>
            <div className={`w-[320px] flex-shrink-0 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
              <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                <span className="text-xs font-semibold">Query Examples</span>
              </div>
              <div className="flex gap-0 border-b flex-shrink-0">
                <button
                  type="button"
                  onClick={() => setExampleTabIndex(0)}
                  className={`flex-1 px-2 py-1.5 text-xs rounded-t-[4px] -mb-px ${exampleTabIndex === 0 ? 'border-b-2 border-blue-500 font-medium bg-white' : ''} ${theme.tab}`}
                >
                  Select Table To JSON
                </button>
                <button
                  type="button"
                  onClick={() => setExampleTabIndex(1)}
                  className={`flex-1 px-2 py-1.5 text-xs rounded-t-[4px] -mb-px ${exampleTabIndex === 1 ? 'border-b-2 border-blue-500 font-medium bg-white' : ''} ${theme.tab}`}
                >
                  Update Table From JSON
                </button>
              </div>
              <div className="flex-1 overflow-auto p-3 text-xs">
                <a
                  href={SQL_JSON_DOCS_URL}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-blue-600 hover:underline mb-3 p-1"
                >
                  <i className="fa fa-question-circle-o" aria-hidden />
                  Click For SQL JSON Online Documents.
                </a>
                {exampleTabIndex === 0 && (
                  <div className="space-y-4 pt-2">
                    <div>
                      <p className="font-bold mb-1">Example 1 - Control output with FOR JSON AUTO</p>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-blue-600 mb-1 min-h-[120px]">
{`SELECT name, surname
FROM emp
WHERE Department = @Department
FOR JSON AUTO`}
                      </pre>
                      <div className="font-semibold mb-1">JSON Result:</div>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-gray-600 min-h-[140px]">
{`[{
  "name": "John"
}, {
  "name": "Jane",
  "surname": "Doe"
}]`}
                      </pre>
                    </div>
                    <div>
                      <p className="font-bold mb-1">Example 2 - Control output with FOR JSON PATH</p>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-blue-600 mb-1 min-h-[120px]">
{`SELECT TOP 5
  BusinessEntityID As Id,
  FirstName,
  LastName
FROM Person.Person
FOR JSON PATH`}
                      </pre>
                      <div className="font-semibold mb-1">JSON Result:</div>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-gray-600 min-h-[140px]">
{`[{
  "Id": 1,
  "FirstName": "Ken",
  "LastName": "Sanchez"
}, {
  "Id": 2,
  "FirstName": "Terri",
  "LastName": "Duffy"
}]`}
                      </pre>
                    </div>
                    <div>
                      <p className="font-bold mb-1">Example 3 - Multiple tables, 1 JSON object per table record pair.</p>
                      <p className="text-gray-600 mb-1">If you reference more than one table in a query, FOR JSON PATH nests each column using its alias. The following query creates one JSON object per (OrderHeader, OrderDetails) pair joined in the query.</p>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-blue-600 mb-1 min-h-[120px]">
{`SELECT h.OrderId, h.OrderDate,
  d.ProductId, d.Quantity
FROM OrderHeader h
INNER JOIN OrderDetails d ON d.OrderId = h.OrderId
FOR JSON PATH`}
                      </pre>
                      <div className="font-semibold mb-1">JSON Result:</div>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-gray-600 min-h-[140px]">
{`[{
  "OrderId": 1,
  "OrderDate": "2024-01-15",
  "ProductId": 10,
  "Quantity": 2
}]`}
                      </pre>
                    </div>
                    <div>
                      <p className="font-bold mb-1">Example 4 - Multiple tables, hierarchy JSON objects</p>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-blue-600 mb-1 min-h-[120px]">
{`SELECT h.OrderId, h.OrderDate,
  (SELECT d.ProductId, d.Quantity
   FROM OrderDetails d
   WHERE d.OrderId = h.OrderId
   FOR JSON PATH) AS Details
FROM OrderHeader h
FOR JSON PATH`}
                      </pre>
                      <div className="font-semibold mb-1">JSON Result:</div>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-gray-600 min-h-[140px]">
{`[{
  "OrderId": 1,
  "OrderDate": "2024-01-15",
  "Details": [{
    "ProductId": 10,
    "Quantity": 2
  }]
}]`}
                      </pre>
                    </div>
                  </div>
                )}
                {exampleTabIndex === 1 && (
                  <div className="space-y-4 pt-2">
                    <div>
                      <p className="font-bold mb-1">Example 1 - Update one table from JSON objects</p>
                      <p className="text-gray-600 mb-1">(Rename &apos;YourTableName&apos;, &apos;COLUMN1&apos;, and &apos;COLUMN2&apos; to real names.)</p>
                      <pre className="bg-white p-2 rounded border text-[11px] overflow-x-auto whitespace-pre-wrap break-words font-mono text-blue-600 min-h-[280px]">
{`-- Parse JSON and insert/update into a table
INSERT INTO [YourTableName] (COLUMN1, COLUMN2)
SELECT COLUMN1, COLUMN2
FROM OPENJSON(@JsonInput)
WITH (
  COLUMN1 nvarchar(100) '$.COLUMN1',
  COLUMN2 nvarchar(100) '$.COLUMN2'
)

-- Or use MERGE to update existing rows from JSON
MERGE [YourTableName] AS t
USING (
  SELECT * FROM OPENJSON(@JsonInput)
  WITH (
    Id int '$.Id',
    COLUMN1 nvarchar(100) '$.COLUMN1',
    COLUMN2 nvarchar(100) '$.COLUMN2'
  )
) AS s ON t.Id = s.Id
WHEN MATCHED THEN
  UPDATE SET t.COLUMN1 = s.COLUMN1, t.COLUMN2 = s.COLUMN2
WHEN NOT MATCHED THEN
  INSERT (Id, COLUMN1, COLUMN2) VALUES (s.Id, s.COLUMN1, s.COLUMN2);`}
                      </pre>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        {activeTabIndex === 1 && (
          <div className="flex-1 flex gap-4 min-h-0 min-w-0">
            <div className={`flex flex-col border rounded min-h-0 w-1/3 min-w-0 flex-shrink-0 ${theme.mainContentSection}`}>
              <div className="px-2 py-1.5 border-b bg-gray-100 flex-shrink-0">
                <span className="text-xs font-semibold">Query Parameter</span>
              </div>
              <div className="flex-1 min-h-[120px] overflow-hidden">
                <FlexGrid
                  itemsSource={queryParameterCV ?? undefined}
                  autoGenerateColumns={false}
                  selectionMode="Row"
                  isReadOnly={false}
                  cellEditEnded={() => syncQueryParamsFromCV()}
                  initialized={() => {}}
                  className="w-full h-full"
                >
                  <FlexGridColumn binding="ParameterName" header="Query Parameter" width="*" />
                  <FlexGridColumn binding="defaultValue" header="Test Value" width="*" />
                </FlexGrid>
              </div>
            </div>
            <div className={`flex-1 flex flex-col border rounded min-h-0 min-w-0 ${theme.mainContentSection}`}>
              <div className="flex flex-col gap-3 p-2 flex-1 min-h-0">
                <div className="flex items-center gap-2 flex-wrap flex-shrink-0">
                  <span className="text-xs font-semibold">API Url:</span>
                  <span className="flex-1 min-w-0 text-xs underline select-all text-gray-700 truncate" title={buildTestApiFullUrl()}>{buildTestApiFullUrl() || '—'}</span>
                  <button type="button" onClick={handleTestApi} disabled={!currentOperation?.Id}
                    className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50 inline-flex items-center gap-1 flex-shrink-0">
                    <i className="fa fa-play" aria-hidden /> Click To Test Call API
                  </button>
                </div>
                {(currentOperation?.HttpMethd === 'Post' || currentOperation?.HttpMethd === 'Put') && (
                  <div className="flex flex-col gap-1 flex-shrink-0">
                    <span className="text-xs font-semibold">Post Payload Json Data</span>
                    <div className="w-full min-h-[100px] h-[160px] border rounded overflow-hidden bg-white">
                      <JsonCodeEditor
                        value={currentOperation?.JsonSampleData ?? ''}
                        onChange={(next) => {
                          setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: next } : prev));
                          setIsModified(true);
                        }}
                        placeholder='e.g. {"key": "value"}'
                        className="w-full h-full"
                      />
                    </div>
                  </div>
                )}
                <div className="flex flex-col flex-1 min-h-0">
                  <span className="text-xs font-semibold flex-shrink-0 mb-1">API Response Data:</span>
                  <JsonCodeViewer
                    text={apiResponseText}
                    placeholder="Response will appear after test."
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

export default AppJsonQueryApiEditor;
