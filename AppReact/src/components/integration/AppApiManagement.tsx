/**
 * App API Management (ApiBuilderManagement)
 * Lists built-in API parameters (integrationSettingId = 1) with Create API dropdown and context menu Open/Delete.
 * Reference: Angular apiBuilderManagementCtrl.js, ApiBuilderManagement.cshtml
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, SortDescription, PropertyGroupDescription } from '@mescius/wijmo';
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

const API_BUILDER_INTEGRATION_SETTING_ID = 1;

type IntegrationSettingParameterItem = {
  Id?: number | null;
  ActionCode?: string;
  ActionDescription?: string;
  APIType?: string;
  IsSimpleQuery?: boolean;
  HttpMethd?: string;
  DataSourceId?: number | null;
  TranscationId?: number | null;
  ApplicatoinDisplay?: string;
  TranscationFieId?: number | null;
  APIConfigParameters?: { ExcelDataImportDataSetId?: number } | null;
};

type DataSourceItem = { Id: number; Display?: string; DataSourceName?: string };
type TransactionLookupItem = { Id: number; Display?: string };

const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
};

const AppApiManagement: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const { showConfirm } = useAlertConfirm();

  const [isLoading, setIsLoading] = useState(false);
  const [, setParameterList] = useState<IntegrationSettingParameterItem[]>([]);
  const [collectionView, setCollectionView] = useState<CollectionView | null>(null);
  const [allDataSources, setAllDataSources] = useState<DataSourceItem[]>([]);
  const [defaultDataSourceId, setDefaultDataSourceId] = useState<number | null>(null);
  const [transactionDataMap, setTransactionDataMap] = useState<DataMap | null>(null);
  const [dataSourceDataMap, setDataSourceDataMap] = useState<DataMap | null>(null);
  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState({ x: 0, y: 0 });
  const [selectedRowData, setSelectedRowData] = useState<IntegrationSettingParameterItem | null>(null);
  const [createDropdownOpen, setCreateDropdownOpen] = useState(false);
  const [sqlQuerySubmenuOpen, setSqlQuerySubmenuOpen] = useState(false);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const sqlQuerySubmenuTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [dataSourceList, transactionLookup, settingData] = await Promise.all([
        adminSvc.retrieveAllAppDataSourceRegisterExDto(),
        adminSvc.getMassEntitiesLookupItem('AppTransaction'),
        integrationService.retrieveOneAppIntegrationSettingExDto(String(API_BUILDER_INTEGRATION_SETTING_ID)),
      ]);

      const dataSources: DataSourceItem[] = Array.isArray(dataSourceList) ? dataSourceList : [];
      let defaultId: number | null = null;
      dataSources.forEach((ds) => {
        (ds as any).Display = `${(ds as any).DataSourceName ?? ds.Id} (${ds.Id})`;
        if (ds.Id !== 2147483647 && (ds as any).IsCompanyMasterDb) {
          if (defaultId == null) defaultId = ds.Id;
        }
      });
      setAllDataSources(dataSources);
      setDefaultDataSourceId(defaultId);

      const txMap = transactionLookup?.['AppTransaction'];
      if (Array.isArray(txMap)) {
        setTransactionDataMap(new DataMap(txMap as TransactionLookupItem[], 'Id', 'Display'));
      } else {
        setTransactionDataMap(null);
      }
      setDataSourceDataMap(new DataMap(dataSources, 'Id', 'Display'));

      const params = settingData?.AppIntergrationSettingParameterList ?? settingData?.AppIntegrationSettingParameterList ?? [];
      const list = Array.isArray(params) ? params : [];
      setParameterList(list);
      const cv = new CollectionView(list);
      cv.sortDescriptions.push(new SortDescription('ApplicatoinDisplay', true));
      cv.sortDescriptions.push(new SortDescription('ActionCode', true));
      cv.groupDescriptions.push(new PropertyGroupDescription('ApplicatoinDisplay'));
      setCollectionView(cv);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, errorMessage]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const openContextMenu = useCallback((e: React.MouseEvent, dataItem: IntegrationSettingParameterItem) => {
    e.stopPropagation();
    setSelectedRowData(dataItem);
    setContextMenuPos({ x: e.clientX - 20, y: e.clientY - 5 });
    setContextMenuOpen(true);
  }, []);

  const closeContextMenu = useCallback(() => {
    setSelectedRowData(null);
    setContextMenuOpen(false);
  }, []);

  const deleteById = useCallback(
    async (settingParameterId: number) => {
      try {
        const result = await integrationService.deleteOneAppIntegrationSettingParameter(String(settingParameterId));
        if (result?.IsSuccessful) {
          errorMessage.showInfo('Deleted successfully.');
          loadData();
        } else {
          const messages = extractValidationMessages(result?.ValidationResult);
          if (messages.length) messages.forEach((msg) => errorMessage.showError(msg));
          else errorMessage.showError('Failed to delete.');
        }
      } catch (error) {
        errorMessage.showError(error instanceof Error ? error.message : String(error));
      }
    },
    [errorMessage, loadData],
  );

  const getEditorPathAndLabel = useCallback((dto: IntegrationSettingParameterItem): { path: string; label: string } => {
    const label = dto.ActionCode ? `API: ${dto.ActionCode}` : (dto.Id != null ? `API (${dto.Id})` : 'API (New)');
    if (dto.IsSimpleQuery) {
      const path = dto.Id != null ? `/api-builder-editor/${dto.Id}` : '/api-builder-editor';
      return { path, label };
    }
    if (dto.TranscationId) {
      const path = dto.Id != null ? `/app-data-model-api-editor/${dto.Id}` : '/app-data-model-api-editor';
      return { path, label };
    }
    if (dto.TranscationFieId) {
      const path = dto.Id != null ? `/app-data-presentation-api-editor/${dto.Id}` : '/app-data-presentation-api-editor';
      return { path, label };
    }
    if (dto.APIConfigParameters?.ExcelDataImportDataSetId) {
      const path = dto.Id != null ? `/excel-import-data-update-api-editor/${dto.Id}` : '/excel-import-data-update-api-editor';
      return { path, label };
    }
    const path = dto.Id != null ? `/api-builder-editor/${dto.Id}` : '/api-builder-editor';
    return { path, label };
  }, []);

  const openEditorInNewTab = useCallback(
    (dto: IntegrationSettingParameterItem) => {
      const { path, label } = getEditorPathAndLabel(dto);
      dispatch(addTab({ tabPath: path, label, isClosable: true }));
      navigate(path);
    },
    [dispatch, navigate, getEditorPathAndLabel],
  );

  const contextMenuOpenEditor = useCallback(() => {
    if (!selectedRowData) {
      closeContextMenu();
      return;
    }
    const dto = selectedRowData;
    closeContextMenu();
    openEditorInNewTab(dto);
  }, [selectedRowData, openEditorInNewTab, closeContextMenu]);

  const contextMenuDelete = useCallback(async () => {
    if (!selectedRowData?.Id) {
      closeContextMenu();
      return;
    }
    closeContextMenu();
    const confirmed = await showConfirm(
      `Please confirm to delete:\n${selectedRowData.ActionCode ?? 'Item'}`,
      { title: 'Delete API' },
    );
    if (confirmed) await deleteById(selectedRowData.Id!);
  }, [selectedRowData, showConfirm, deleteById, closeContextMenu]);

  const createSqlQueryApi = useCallback(
    (dataSourceId?: number) => {
      setCreateDropdownOpen(false);
      const param2 = JSON.stringify({ dataSourceId: dataSourceId ?? defaultDataSourceId });
      const path = `/api-builder-editor?param2=${encodeURIComponent(param2)}`;
      dispatch(addTab({ tabPath: path, label: 'API (New)', isClosable: true }));
      navigate(path);
    },
    [dispatch, navigate, defaultDataSourceId],
  );

  const createDataModelApi = useCallback(() => {
    setCreateDropdownOpen(false);
    const param2 = JSON.stringify({ dataSourceId: defaultDataSourceId });
    const path = `/app-data-model-api-editor?param2=${encodeURIComponent(param2)}`;
    dispatch(addTab({ tabPath: path, label: 'API (New)', isClosable: true }));
    navigate(path);
  }, [dispatch, navigate, defaultDataSourceId]);

  const createSearchViewApi = useCallback(() => {
    setCreateDropdownOpen(false);
    const param2 = JSON.stringify({ dataSourceId: defaultDataSourceId });
    const path = `/app-data-presentation-api-editor?param2=${encodeURIComponent(param2)}`;
    dispatch(addTab({ tabPath: path, label: 'API (New)', isClosable: true }));
    navigate(path);
  }, [dispatch, navigate, defaultDataSourceId]);

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const row = flex.selection?.row;
      if (row == null || row < 0) return;
      const dto = flex.rows[row]?.dataItem as IntegrationSettingParameterItem | undefined;
      if (dto) openEditorInNewTab(dto);
    },
    [openEditorInNewTab],
  );

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenuOpen) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenuOpen, closeContextMenu]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setCreateDropdownOpen(false);
        setSqlQuerySubmenuOpen(false);
        if (sqlQuerySubmenuTimeoutRef.current) {
          clearTimeout(sqlQuerySubmenuTimeoutRef.current);
          sqlQuerySubmenuTimeoutRef.current = null;
        }
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, []);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>App API Provider</div>
        <div className="flex items-center space-x-2">
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden />
          </button>
          <div className="relative" ref={dropdownRef}>
            <button
              type="button"
              onClick={() => {
                setCreateDropdownOpen((v) => !v);
                setSqlQuerySubmenuOpen(false);
              }}
              disabled={isLoading}
              className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white bg-green-500 hover:bg-green-600 disabled:opacity-60"
              title="Create API"
            >
              <i className="fa fa-plus" aria-hidden />
            </button>
            {createDropdownOpen && (
              <div
                className={`absolute right-0 top-full mt-1 py-1 whitespace-nowrap pr-2 rounded-[4px] border shadow-lg z-10 text-xs ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                {allDataSources.length > 1 ? (
                  <div
                    className={`relative w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 justify-between group`}
                    onMouseEnter={() => {
                      if (sqlQuerySubmenuTimeoutRef.current) {
                        clearTimeout(sqlQuerySubmenuTimeoutRef.current);
                        sqlQuerySubmenuTimeoutRef.current = null;
                      }
                      setSqlQuerySubmenuOpen(true);
                    }}
                    onMouseLeave={() => {
                      sqlQuerySubmenuTimeoutRef.current = setTimeout(() => setSqlQuerySubmenuOpen(false), 150);
                    }}
                  >
                    <span className="flex items-center gap-2">
                      <i className="fa fa-plus" aria-hidden /> Create SQL Json Query API
                    </span>
                    <i className="fa fa-caret-right" aria-hidden />
                    {sqlQuerySubmenuOpen && (
                      <div
                        className={`absolute right-full top-0 mr-0 py-1 min-w-[180px] rounded-[4px] border shadow-lg z-20 text-xs ${theme.mainContentSection}`}
                        onMouseEnter={() => {
                          if (sqlQuerySubmenuTimeoutRef.current) {
                            clearTimeout(sqlQuerySubmenuTimeoutRef.current);
                            sqlQuerySubmenuTimeoutRef.current = null;
                          }
                          setSqlQuerySubmenuOpen(true);
                        }}
                        onMouseLeave={() => {
                          sqlQuerySubmenuTimeoutRef.current = setTimeout(() => setSqlQuerySubmenuOpen(false), 150);
                        }}
                      >
                        <div className={`px-3 py-1.5 border-b ${theme.label} text-xs`}>On Database:</div>
                        {allDataSources.map((ds) => (
                          <button
                            key={ds.Id}
                            type="button"
                            className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} flex items-center hover:bg-opacity-80`}
                            onClick={() => createSqlQueryApi(ds.Id)}
                          >
                            {ds.DataSourceName ?? ds.Id} ({ds.Id})
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                ) : (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
                    onClick={() => createSqlQueryApi()}
                  >
                    <i className="fa fa-plus" aria-hidden /> Create SQL Json Query API
                  </button>
                )}
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
                  onClick={createDataModelApi}
                >
                  <i className="fa fa-plus" aria-hidden /> Create App Data Model API
                </button>
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2`}
                  onClick={createSearchViewApi}
                >
                  <i className="fa fa-plus" aria-hidden /> Create App Report & View API
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className={`w-full h-[200px] ${theme.mainContentSection} flex-auto overflow-hidden`}>
        <div className="w-full h-full">
          <FlexGrid
            ref={flexRef}
            itemsSource={collectionView ?? undefined}
            autoGenerateColumns={false}
            selectionMode="Row"
            isReadOnly
            allowDelete={false}
            frozenColumns={3}
            initialized={(flex: wjGrid.FlexGrid) => {
              flexRef.current = flex;
              flex.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(flex));
            }}
            className="w-full h-full !border-0"
          >
            <FlexGridColumn isReadOnly width={120} header="Actions">
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
            <FlexGridColumn binding="Id" header="Id" width={80} />
            <FlexGridColumn binding="ActionCode" header="API Code" width={300} />
            <FlexGridColumn binding="ActionDescription" header="Description" width={150} />
            <FlexGridColumn binding="APIType" header="API Type" width={150} />
            <FlexGridColumn binding="HttpMethd" header="Http Method" width={150} />
            <FlexGridColumn binding="DataSourceId" header="Data Source" width={200} dataMap={dataSourceDataMap ?? undefined} />
            <FlexGridColumn binding="TranscationId" header="Data Model" width={200} dataMap={transactionDataMap ?? undefined} />
            <FlexGridColumn binding="ApplicatoinDisplay" header="Application" width={200} />
          </FlexGrid>
        </div>
      </div>

      {contextMenuOpen && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuOpenEditor}
          >
            <i className="fa fa-edit mr-2" aria-hidden /> Open
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuDelete}
          >
            <i className="fa fa-trash mr-2" aria-hidden /> Delete
          </button>
        </div>
      )}
    </div>
  );
};

export default AppApiManagement;
