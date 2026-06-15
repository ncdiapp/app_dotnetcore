/**
 * Excel Import Data Update API Editor (create/edit)
 * Load/save via integrationService. Reference: Angular ExcelImportDataUpdateApiEditor.
 * Features: Import Name, Calling API Url, Test API, Post Payload, API Response, View Updated Tables.
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { endpoints } from '../../webapi/endpoints';
import { getHeaders } from '../../helper/apiServiceHelper';
import { JsonCodeEditor } from '../common/JsonCodeEditor';
import { JsonCodeViewer } from '../common/JsonCodeViewer';

const API_BUILDER_INTEGRATION_SETTING_ID = 1;

/** Build minimal new-API DTO like Angular initNewApiData(). */
function initNewExcelImportApiDto(dataSourceId: number | null): any {
  return {
    IsSimpleQuery: true,
    IntergrationSettingId: API_BUILDER_INTEGRATION_SETTING_ID,
    DataSourceId: dataSourceId,
    ActionCode: 'NewExcelImportAPI',
    HttpMethd: 'Post',
    ActionDescription: '',
    ApiconfigParameters: '',
    JsonSampleData: '',
    APIConfigParameters: null,
    ImportDataSetDto: null,
  };
}

const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
};

const ExcelImportDataUpdateApiEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const settingParameterId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentOperation, setCurrentOperation] = useState<any>(null);
  const [apiResponseText, setApiResponseText] = useState('');
  const latestOperationRef = useRef<any>(null);
  useEffect(() => {
    latestOperationRef.current = currentOperation;
  }, [currentOperation]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      if (settingParameterId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(settingParameterId), false);
        if (settingData) {
          setCurrentOperation(settingData);
        } else {
          setCurrentOperation(initNewExcelImportApiDto(null));
        }
      } else {
        setCurrentOperation(initNewExcelImportApiDto(null));
      }
      setApiResponseText('');
      setIsModified(false);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [settingParameterId, dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const handleSave = useCallback(async () => {
    const op = latestOperationRef.current ?? currentOperation;
    if (!op) return;
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const payload = { ...op, IntergrationSettingId: op.IntergrationSettingId ?? API_BUILDER_INTEGRATION_SETTING_ID };
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        errorMessage.showInfo('Saved successfully.');
        setIsModified(false);
        if (result?.Object?.Id != null) {
          navigate(`/excel-import-data-update-api-editor/${result.Object.Id}`, { replace: true });
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
  }, [currentOperation, dispatch, errorMessage, loadData, navigate]);

  const apiUrl = currentOperation?.ActionCode
    ? `${endpoints.BASE_URL}/webapi/DataIntegration/${currentOperation.ActionCode}`
    : '';

  const handleTestApi = useCallback(async () => {
    const op = currentOperation;
    if (!op?.Id || !op?.ActionCode) {
      errorMessage.showWarning('Save the API first to test.');
      return;
    }
    const url = `${endpoints.BASE_URL}/webapi/DataIntegration/${op.ActionCode}`;
    setApiResponseText('');
    dispatch(setIsBusy());
    try {
      if (op.HttpMethd === 'Get') {
        const response = await fetch(url, { headers: getHeaders() });
        if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        const data = await response.json();
        setApiResponseText(JSON.stringify(data ?? '', null, 2));
      } else {
        const body = op.JsonSampleData?.trim() ? JSON.parse(op.JsonSampleData) : {};
        const response = await fetch(url, {
          method: 'POST',
          headers: getHeaders(),
          body: JSON.stringify(body),
        });
        if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        const data = await response.json();
        setApiResponseText(JSON.stringify(data ?? '', null, 2));
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      setApiResponseText(`Error: ${msg}`);
      errorMessage.showError(msg);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentOperation, dispatch, errorMessage]);

  const tableNameDisplay = currentOperation?.ImportDataSetDto?.OtherSettingsDto?.TableImportSettingDto?.TableNameDisplay ?? '';

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
        <div className={`text-md font-semibold ${theme.title}`}>API: {op.ActionCode ?? '—'}</div>
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
            <input type="text" autoComplete="off" value={op.ActionCode ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionCode: e.target.value } : prev));
              setIsModified(true);
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Description</label>
            <input type="text" autoComplete="off" value={op.ActionDescription ?? ''} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, ActionDescription: e.target.value } : prev));
              setIsModified(true);
            }} className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Http Method</label>
            <select value={op.HttpMethd ?? 'Post'} onChange={(e) => {
              setCurrentOperation((prev: any) => (prev ? { ...prev, HttpMethd: e.target.value } : prev));
              setIsModified(true);
            }} className="w-32 px-2 py-1 border rounded-[4px] text-xs">
              <option value="Post">Post</option>
              <option value="Get">Get</option>
              <option value="Put">Put</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-24 text-xs ${theme.label}`}>Import Name</label>
            <input type="text" readOnly value={op.ImportDataSetDto?.Name ?? ''} title={op.ImportDataSetDto?.Name ?? ''}
              className="w-[200px] px-2 py-1 border rounded-[4px] text-xs" />
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2 mb-3 text-xs">
          <span className="font-semibold">Calling API Url:</span>
          <span className="text-gray-600 select-all" title={apiUrl}>{apiUrl || '—'}</span>
          <button type="button" onClick={handleTestApi} disabled={!op.Id}
            className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50">
            <i className="fa fa-bolt mr-1" aria-hidden /> Test API
          </button>
        </div>
        {tableNameDisplay && (
          <div className="flex flex-wrap items-center gap-2 mb-3 text-xs">
            <span className="font-semibold">Updating Tables:</span>
            <span className="text-gray-600 select-all">{tableNameDisplay}</span>
          </div>
        )}

        <div className="flex-1 flex gap-4 min-h-0">
          {(op.HttpMethd === 'Post' || op.HttpMethd === 'Put') && (
            <div className={`flex-1 flex flex-col border rounded min-h-0 ${theme.mainContentSection}`}>
              <div className="flex items-center justify-between px-2 py-1.5 border-b bg-gray-100">
                <span className="text-xs font-semibold">Post Payload Json Data</span>
                <button type="button" onClick={handleTestApi} disabled={!op.Id}
                  className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50">
                  <i className="fa fa-bolt mr-1" aria-hidden /> Test API
                </button>
              </div>
              <div className="flex-1 p-2 min-h-0">
                <div className="w-full h-full min-h-[200px] border rounded overflow-hidden bg-white">
                  <JsonCodeEditor
                    value={op.JsonSampleData ?? ''}
                    onChange={(next) => {
                      setCurrentOperation((prev: any) => (prev ? { ...prev, JsonSampleData: next } : prev));
                      setIsModified(true);
                    }}
                    placeholder='e.g. {"key": "value"}'
                    className="w-full h-full"
                  />
                </div>
              </div>
            </div>
          )}
          <div className={`flex-1 flex flex-col border rounded min-h-0 min-w-0 ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between px-2 py-1.5 border-b bg-gray-100">
              <span className="text-xs font-semibold">Response Json Data</span>
              <div className="flex gap-1">
                {op.HttpMethd === 'Get' && (
                  <button type="button" onClick={handleTestApi} disabled={!op.Id}
                    className="h-6 px-2 rounded text-xs border bg-white disabled:opacity-50">
                    <i className="fa fa-bolt mr-1" aria-hidden /> Test API
                  </button>
                )}
                {op.HttpMethd === 'Post' && tableNameDisplay && (
                  <button type="button" className="h-6 px-2 rounded text-xs border bg-white" title="View updated tables data (preview not implemented in React)">
                    <i className="fa fa-database mr-1" aria-hidden /> View Updated Tables
                  </button>
                )}
              </div>
            </div>
            <div className="flex-1 p-2 min-h-0">
              <JsonCodeViewer
                text={apiResponseText}
                placeholder="Response will appear after Test API"
                className="w-full h-full min-h-[260px] p-2 border rounded font-mono text-xs bg-gray-50"
              />
            </div>
          </div>
        </div>

        <div className={`flex-1 flex flex-col border rounded min-h-0 mt-4 ${theme.mainContentSection}`}>
          <div className="px-2 py-1.5 border-b bg-gray-100">
            <span className="text-xs font-semibold">Api Config Parameters</span>
          </div>
          <div className="p-2 min-h-0">
            <textarea
              value={op.ApiconfigParameters ?? ''}
              onChange={(e) => {
                setCurrentOperation((prev: any) => (prev ? { ...prev, ApiconfigParameters: e.target.value } : prev));
                setIsModified(true);
              }}
              className="w-full min-h-[200px] p-2 border rounded font-mono text-xs"
              spellCheck={false}
            />
          </div>
        </div>
      </div>
    </div>
  );
};

export default ExcelImportDataUpdateApiEditor;
