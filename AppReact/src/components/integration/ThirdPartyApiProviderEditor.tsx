/**
 * Third Party API Provider Editor (create/edit)
 * Reference: Angular integrationSettingEditorCtrl.js, IntegrationSettingEditor.cshtml
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { addTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { clampContextMenuPosition, useRefineContextMenuPosition } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 140;

type IntegrationSettingDto = {
  Id?: number | null;
  Name?: string;
  Description?: string;
  DataSourceRegisterId?: number | null;
  OtherSettingsDto?: {
    DatabaseTablePrefix?: string;
    DictEnvironmentVariable?: Record<string, string>;
    DictCookieSetting?: Record<string, string>;
  };
  AppIntergrationSettingParameterList?: any[];
};

type DataSourceItem = { Id: number; Display?: string; DataSourceName?: string };

const ENV_SYSTEM_VAR_OPTIONS = ['BaseUrl', 'Authorization'];

const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
};

const ThirdPartyApiProviderEditor: React.FC = () => {
  const navigate = useNavigate();
  const { param: idParam } = useParams<{ param: string }>();
  const integrationSettingId = idParam ? (isNaN(Number(idParam)) ? null : Number(idParam)) : null;
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const { showConfirm } = useAlertConfirm();

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isModified, setIsModified] = useState(false);
  const [currentSetting, setCurrentSetting] = useState<IntegrationSettingDto>({
    Name: 'New API Provider',
    Description: '',
    DataSourceRegisterId: null,
    OtherSettingsDto: { DatabaseTablePrefix: '', DictEnvironmentVariable: {}, DictCookieSetting: {} },
  });
  const [dataSources, setDataSources] = useState<DataSourceItem[]>([]);
  const [dataSourceDataMap, setDataSourceDataMap] = useState<DataMap | null>(null);
  const [, setParameterList] = useState<any[]>([]);
  const [parameterCV, setParameterCV] = useState<CollectionView | null>(null);
  type KeyValue = { Key: string; Value: string };
  const [envVarList, setEnvVarList] = useState<KeyValue[]>([]);
  const [cookieList, setCookieList] = useState<KeyValue[]>([]);
  const [activeTabIndex, setActiveTabIndex] = useState(0); // 0=API Operation, 1=Environment Variables, 2=Cookie Setting
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; row: any } | null>(null);
  const contextMenuRef = useRef<HTMLDivElement | null>(null);
  const [envSystemVarDropdownOpen, setEnvSystemVarDropdownOpen] = useState(false);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const envVarFlexRef = useRef<wjGrid.FlexGrid | null>(null);
  const cookieFlexRef = useRef<wjGrid.FlexGrid | null>(null);
  const envSystemVarDropdownRef = useRef<HTMLDivElement>(null);

  const envVarCV = React.useMemo(() => new CollectionView(envVarList), [envVarList]);
  const cookieCV = React.useMemo(() => new CollectionView(cookieList), [cookieList]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const dataSourceList = await adminSvc.retrieveAllAppDataSourceRegisterExDto();
      const sources: DataSourceItem[] = Array.isArray(dataSourceList) ? dataSourceList : [];
      sources.forEach((ds) => ((ds as any).Display = `${(ds as any).DataSourceName ?? ds.Id} (${ds.Id})`));
      setDataSources(sources);
      setDataSourceDataMap(new DataMap(sources, 'Id', 'Display'));

      if (integrationSettingId != null) {
        const settingData = await integrationService.retrieveOneAppIntegrationSettingExDto(String(integrationSettingId));
        if (settingData) {
          setCurrentSetting({
            Id: settingData.Id,
            Name: settingData.Name ?? 'New API Provider',
            Description: settingData.Description ?? '',
            DataSourceRegisterId: settingData.DataSourceRegisterId ?? null,
            OtherSettingsDto: {
              DatabaseTablePrefix: settingData.OtherSettingsDto?.DatabaseTablePrefix ?? '',
              DictEnvironmentVariable: settingData.OtherSettingsDto?.DictEnvironmentVariable ?? {},
              DictCookieSetting: settingData.OtherSettingsDto?.DictCookieSetting ?? {},
            },
            AppIntergrationSettingParameterList: settingData.AppIntergrationSettingParameterList ?? settingData.AppIntegrationSettingParameterList ?? [],
          });
          const params = settingData.AppIntergrationSettingParameterList ?? settingData.AppIntegrationSettingParameterList ?? [];
          setParameterList(Array.isArray(params) ? params : []);
          setParameterCV(new CollectionView<any>(Array.isArray(params) ? params : []));
          setEnvVarList(
            Object.entries(settingData.OtherSettingsDto?.DictEnvironmentVariable ?? {}).map(([Key, Value]) => ({
              Key,
              Value: typeof Value === 'string' ? Value : String(Value ?? ''),
            })),
          );
          setCookieList(
            Object.entries(settingData.OtherSettingsDto?.DictCookieSetting ?? {}).map(([Key, Value]) => ({
              Key,
              Value: typeof Value === 'string' ? Value : String(Value ?? ''),
            })),
          );
        }
      } else {
        let defaultDataSourceId: number | null = null;
        for (const ds of sources) {
          if (ds.Id !== 2147483647 && (ds as any).IsCompanyMasterDb) {
            defaultDataSourceId = ds.Id;
            break;
          }
        }
        setCurrentSetting({
          Id: null,
          Name: 'New API Provider',
          Description: '',
          DataSourceRegisterId: defaultDataSourceId,
          OtherSettingsDto: { DatabaseTablePrefix: '', DictEnvironmentVariable: {}, DictCookieSetting: {} },
          AppIntergrationSettingParameterList: [],
        });
        setParameterList([]);
        setParameterCV(new CollectionView<any>([]));
        setEnvVarList([]);
        setCookieList([]);
      }
      setIsModified(false);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [integrationSettingId, dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const markChange = useCallback(() => setIsModified(true), []);

  const handleSave = useCallback(async () => {
    if (!currentSetting) return;
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const dictEnv = Object.fromEntries(
        envVarList.filter((r) => r.Key != null && String(r.Key).trim() !== '').map((r) => [String(r.Key).trim(), r.Value ?? '']),
      );
      const dictCookie = Object.fromEntries(
        cookieList.filter((r) => r.Key != null && String(r.Key).trim() !== '').map((r) => [String(r.Key).trim(), r.Value ?? '']),
      );
      const payload = {
        ...currentSetting,
        OtherSettingsDto: {
          ...(currentSetting.OtherSettingsDto ?? {}),
          DictEnvironmentVariable: dictEnv,
          DictCookieSetting: dictCookie,
        },
      };
      const result = await integrationService.saveAppIntegrationSettingExDto(payload);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        errorMessage.showInfo('Saved successfully.');
        setIsModified(false);
        if (result?.Object?.Id != null) {
          setCurrentSetting((prev) => ({ ...prev, Id: result.Object.Id }));
          navigate(`/third-party-api-provider-editor/${result.Object.Id}`, { replace: true });
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
  }, [currentSetting, envVarList, cookieList, dispatch, errorMessage, loadData, navigate]);

  const addEnvVar = useCallback(() => {
    setEnvVarList((prev) => [...prev, { Key: '', Value: '' }]);
    markChange();
  }, [markChange]);

  const addSystemEnvVar = useCallback((key: string) => {
    const alreadyExists = envVarList.some((r) => (r.Key || '').trim() === key);
    if (alreadyExists) {
      errorMessage.showWarning(`Variable "${key}" already exists.`);
      setEnvSystemVarDropdownOpen(false);
      return;
    }
    setEnvVarList((prev) => [...prev, { Key: key, Value: '' }]);
    setEnvSystemVarDropdownOpen(false);
    markChange();
  }, [envVarList, markChange, errorMessage]);

  const removeEnvVar = useCallback(() => {
    const flex = envVarFlexRef.current;
    if (!flex) return;
    const row = flex.selection?.row;
    if (row == null || row < 0) return;
    setEnvVarList((prev) => prev.filter((_, i) => i !== row));
    markChange();
  }, [markChange]);

  const addCookie = useCallback(() => {
    setCookieList((prev) => [...prev, { Key: '', Value: '' }]);
    markChange();
  }, [markChange]);

  const removeCookie = useCallback(() => {
    const flex = cookieFlexRef.current;
    if (!flex) return;
    const row = flex.selection?.row;
    if (row == null || row < 0) return;
    setCookieList((prev) => prev.filter((_, i) => i !== row));
    markChange();
  }, [markChange]);

  const openContextMenu = useCallback((e: React.MouseEvent, row: any) => {
    e.stopPropagation();
    const { x, y } = clampContextMenuPosition(
      e.clientX - 20,
      e.clientY - 5,
      CONTEXT_MENU_ESTIMATED_WIDTH,
      CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setContextMenu({ x, y, row });
  }, []);

  const setContextMenuPosition = useCallback((updater: React.SetStateAction<{ x: number; y: number }>) => {
    setContextMenu((prev) => {
      if (!prev) return prev;
      const nextPos = typeof updater === 'function' ? updater({ x: prev.x, y: prev.y }) : updater;
      if (nextPos.x === prev.x && nextPos.y === prev.y) return prev;
      return { ...prev, x: nextPos.x, y: nextPos.y };
    });
  }, []);

  useRefineContextMenuPosition(contextMenu !== null, contextMenuRef, setContextMenuPosition);

  const closeContextMenu = useCallback(() => setContextMenu(null), []);

  const createOperation = useCallback(() => {
    if (currentSetting?.Id == null) {
      errorMessage.showWarning('Save the provider first, then add operations.');
      return;
    }
    const param2 = JSON.stringify({});
    const path = `/third-party-api-editor?Id=${currentSetting.Id}&param2=${encodeURIComponent(param2)}`;
    const label = 'API (New)';
    dispatch(addTab({ tabPath: path, label, isClosable: true }));
    navigate(path);
  }, [currentSetting?.Id, dispatch, navigate, errorMessage]);

  const editParameter = useCallback(
    (row: any) => {
      if (!row?.Id || currentSetting?.Id == null) return;
      closeContextMenu();
      const path = `/third-party-api-editor/${row.Id}`;
      const label = row.ActionCode ? `API: ${row.ActionCode}` : `API (${row.Id})`;
      dispatch(addTab({ tabPath: path, label, isClosable: true }));
      navigate(path);
    },
    [currentSetting?.Id, dispatch, navigate, closeContextMenu],
  );

  const deleteParameter = useCallback(
    async (row: any) => {
      if (!row?.Id) return;
      closeContextMenu();
      const confirmed = await showConfirm(`Delete operation: ${row.ActionCode ?? row.Id}?`, { title: 'Delete Operation' });
      if (!confirmed) return;
      try {
        const result = await integrationService.deleteOneAppIntegrationSettingParameter(String(row.Id));
        if (result?.IsSuccessful) {
          errorMessage.showInfo('Deleted.');
          loadData();
        } else {
          const messages = extractValidationMessages(result?.ValidationResult);
          messages.forEach((msg) => errorMessage.showError(msg));
        }
      } catch (error) {
        errorMessage.showError(error instanceof Error ? error.message : String(error));
      }
    },
    [showConfirm, errorMessage, loadData, closeContextMenu],
  );

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenu) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenu, closeContextMenu]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (envSystemVarDropdownOpen && envSystemVarDropdownRef.current && !envSystemVarDropdownRef.current.contains(e.target as Node)) {
        setEnvSystemVarDropdownOpen(false);
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [envSystemVarDropdownOpen]);

  if (isLoading) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          API Provider: {currentSetting?.Name ?? '—'}
        </div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isSaving}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden />
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={!isModified || isSaving}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
            title="Save"
          >
            <i className="fa fa-save" aria-hidden />
          </button>
        </div>
      </div>

      <div className={`flex-1 overflow-auto p-3 ${theme.mainContentSection} flex flex-col`}>
        {/* Row 1: 3 input fields in one row (Angular layout) */}
        <div className="flex flex-none gap-6 mb-3">
          <div className="flex items-center gap-2">
            <label className={`w-28 text-xs ${theme.label}`}>Name</label>
            <input
              type="text"
              value={currentSetting?.Name ?? ''}
              onChange={(e) => {
                setCurrentSetting((prev) => (prev ? { ...prev, Name: e.target.value } : prev));
                markChange();
              }}
              className="w-[200px] px-2 py-1 border rounded-[4px] text-xs"
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-28 text-xs ${theme.label}`}>Description</label>
            <input
              type="text"
              value={currentSetting?.Description ?? ''}
              onChange={(e) => {
                setCurrentSetting((prev) => (prev ? { ...prev, Description: e.target.value } : prev));
                markChange();
              }}
              className="w-[200px] px-2 py-1 border rounded-[4px] text-xs"
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-28 text-xs ${theme.label}`}>Staging Table Prefix</label>
            <input
              type="text"
              value={currentSetting?.OtherSettingsDto?.DatabaseTablePrefix ?? ''}
              onChange={(e) => {
                setCurrentSetting((prev) =>
                  prev
                    ? {
                        ...prev,
                        OtherSettingsDto: { ...prev.OtherSettingsDto, DatabaseTablePrefix: e.target.value },
                      }
                    : prev,
                );
                markChange();
              }}
              className="w-[200px] px-2 py-1 border rounded-[4px] text-xs"
            />
          </div>
        </div>
       

        {/* Tab menu bar + Tab body (only when editing existing) */}
        {integrationSettingId != null && (
          <div className="flex-1 flex flex-col min-h-0">
            <div className="flex gap-0 border-b flex-none">
              <button
                type="button"
                onClick={() => setActiveTabIndex(1)}
                className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${activeTabIndex === 1 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}
              >
                Environment Variables
              </button>
              <button
                type="button"
                onClick={() => setActiveTabIndex(2)}
                className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${activeTabIndex === 2 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}
              >
                Cookie Setting
              </button>
              <button
                type="button"
                onClick={() => setActiveTabIndex(0)}
                className={`px-4 py-2 text-xs rounded-t-[4px] -mb-px ${activeTabIndex === 0 ? 'border-b-2 border-blue-500 font-medium' : ''} ${theme.tab}`}
              >
                API Operation
              </button>
            </div>

            {/* Tab body: Environment Variables (buttons aligned left, match Angular: + Add System Variable, + Add User Variable, - Remove) */}
            {activeTabIndex === 1 && (
              <div className="flex-1 flex flex-col min-h-0 mt-3">
                <div className="flex items-center gap-2 mb-2">
                  <span className={`text-xs font-semibold ${theme.title}`}>Variables</span>
                  <div className="relative" ref={envSystemVarDropdownRef}>
                    <button
                        type="button"
                        onClick={() => setEnvSystemVarDropdownOpen((v) => !v)}
                        className="h-6 px-2 rounded-[4px] text-xs border border-gray-400 hover:bg-gray-100 underline"
                      >
                        <i className="fa fa-plus mr-1" aria-hidden /> Add System Variable
                      </button>
                    {envSystemVarDropdownOpen && (
                      <div className={`absolute left-0 top-full mt-1 z-10 min-w-[140px] border rounded-[4px] shadow-lg py-1 ${theme.mainContentSection}`}>
                        {ENV_SYSTEM_VAR_OPTIONS.map((key) => {
                          const alreadyAdded = envVarList.some((r) => (r.Key || '').trim() === key);
                          return (
                            <button
                              key={key}
                              type="button"
                              disabled={alreadyAdded}
                              className={`w-full text-left px-3 py-1.5 text-xs flex items-center gap-2 ${theme.contextMenu} ${alreadyAdded ? 'opacity-60 cursor-not-allowed' : 'hover:bg-gray-100'}`}
                              onClick={() => addSystemEnvVar(key)}
                            >
                              {alreadyAdded && <i className="fa fa-check text-green-600" aria-hidden />}
                              {key}
                            </button>
                          );
                        })}
                      </div>
                    )}
                  </div>
                  <button
                    type="button"
                    onClick={addEnvVar}
                    className="h-6 px-2 rounded-[4px] text-xs border border-gray-400 hover:bg-gray-100"
                  >
                    <i className="fa fa-plus mr-1" aria-hidden /> Add User Variable
                  </button>
                  <button
                    type="button"
                    onClick={removeEnvVar}
                    className="h-6 px-2 rounded-[4px] text-xs border border-gray-400 hover:bg-gray-100"
                  >
                    <i className="fa fa-minus mr-1" aria-hidden /> Remove
                  </button>
                </div>
                <div className="flex-1 min-h-[200px]">
                  <FlexGrid
                    ref={envVarFlexRef}
                    itemsSource={envVarCV}
                    autoGenerateColumns={false}
                    selectionMode="Row"
                    allowDelete={false}
                    cellEditEnded={() => markChange()}
                    initialized={(flex: wjGrid.FlexGrid) => {
                      envVarFlexRef.current = flex;
                    }}
                    className="w-full h-full"
                  >
                    <FlexGridColumn binding="Key" header="Variable" width={200} />
                    <FlexGridColumn binding="Value" header="Value" width={800} />
                    <FlexGridColumn binding="" header="" width="*" isReadOnly />
                  </FlexGrid>
                </div>
              </div>
            )}

            {/* Tab body: Cookie Setting (buttons on left, match Angular layout) */}
            {activeTabIndex === 2 && (
              <div className="flex-1 flex flex-col min-h-0 mt-3">
                <div className="flex items-center gap-2 mb-2">
                  <span className={`text-xs font-semibold ${theme.title}`}>Variables</span>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={addCookie}
                      className="h-6 px-2 rounded-[4px] text-xs border border-gray-400 hover:bg-gray-100"
                    >
                      <i className="fa fa-plus mr-1" aria-hidden /> Add
                    </button>
                    <button
                      type="button"
                      onClick={removeCookie}
                      className="h-6 px-2 rounded-[4px] text-xs border border-gray-400 hover:bg-gray-100"
                    >
                      <i className="fa fa-minus mr-1" aria-hidden /> Remove
                    </button>
                  </div>
                </div>
                <div className="flex-1 min-h-[200px]">
                  <FlexGrid
                    ref={cookieFlexRef}
                    itemsSource={cookieCV}
                    autoGenerateColumns={false}
                    selectionMode="Row"
                    allowDelete={false}
                    cellEditEnded={() => markChange()}
                    initialized={(flex: wjGrid.FlexGrid) => {
                      cookieFlexRef.current = flex;
                    }}
                    className="w-full h-full"
                  >
                    <FlexGridColumn binding="Key" header="Cookie Name" width={200} />
                    <FlexGridColumn binding="Value" header="Default Value" width={800} />
                    <FlexGridColumn binding="" header="" width="*" isReadOnly />
                  </FlexGrid>
                </div>
              </div>
            )}

            {/* Tab body: API Operation (button on left, match Angular layout) */}
            {activeTabIndex === 0 && (
              <div className="flex-1 flex flex-col min-h-0 mt-3">
                <div className="flex items-center gap-2 mb-2">
                  <span className={`text-xs font-semibold ${theme.title}`}>API Operation</span>
                  <button
                    type="button"
                    onClick={createOperation}
                    className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white bg-green-500 hover:bg-green-600"
                    title="Create Operation"
                  >
                    <i className="fa fa-plus" aria-hidden />
                  </button>
                </div>
                <div className="flex-1 min-h-[200px]">
                  <FlexGrid
                    ref={flexRef}
                    itemsSource={parameterCV ?? undefined}
                    autoGenerateColumns={false}
                    selectionMode="Row"
                    isReadOnly
                    allowDelete={false}
                    initialized={(flex: wjGrid.FlexGrid) => {
                      flexRef.current = flex;
                    }}
                    className="w-full h-full"
                  >
                    <FlexGridColumn isReadOnly width={80} header="">
                      <FlexGridCellTemplate
                        cellType="Cell"
                        template={(ctx: any) => (
                          <div className="flex justify-center w-full">
                            <button
                              type="button"
                              className={`${theme.menu_default}`}
                              style={{ width: '30px' }}
                              title="More Options"
                              onClick={(e) => openContextMenu(e, ctx.item)}
                            >
                              <i className="fa fa-pencil" aria-hidden style={{ fontSize: '12px' }} />
                              <i className="fa fa-navicon" aria-hidden style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }} />
                            </button>
                          </div>
                        )}
                      />
                    </FlexGridColumn>
                    <FlexGridColumn binding="Id" header="ID" width={80} />
                    <FlexGridColumn binding="HttpMethd" header="Method" width={100} />
                    <FlexGridColumn binding="ActionCode" header="Operation Code" width={300} />
                    <FlexGridColumn binding="ActionDescription" header="Description" width={300} />
                    <FlexGridColumn binding="DataSourceId" header="Data Source" width={200} dataMap={dataSourceDataMap ?? undefined} />
                    <FlexGridColumn binding="" header="" width="*" isReadOnly />
                  </FlexGrid>
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {contextMenu && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => editParameter(contextMenu.row)}
          >
            <i className="fa fa-edit mr-2" aria-hidden /> Edit
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={() => deleteParameter(contextMenu.row)}
          >
            <i className="fa fa-trash mr-2" aria-hidden /> Delete
          </button>
        </div>
      )}
    </div>
  );
};

export default ThirdPartyApiProviderEditor;
