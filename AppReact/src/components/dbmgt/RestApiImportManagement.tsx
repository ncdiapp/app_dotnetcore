import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';

type ApiOperationItem = {
  Id: number;
  ActionCode: string;
  actionDisplay?: string;
};

type ApiImportSettingItem = {
  Id: number;
  ActionCode: string;
  ActionDescription?: string;
  DataSourceId?: number;
  ProviderName?: string;
  ParentOperationName?: string;
};

export type RestApiImportManagementProps = {
  isUsedAsSelector?: boolean;
  onConfirmSelection?: (item: ApiImportSettingItem) => void;
  onRequestClose?: () => void;
  onOpenImportEditor?: (importSettingId: number) => void;
};

const RestApiImportManagement: React.FC<RestApiImportManagementProps> = (props) => {
  const { isUsedAsSelector = false, onConfirmSelection, onRequestClose, onOpenImportEditor } = props;
  const { theme } = useTheme();
  const { showError, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();

  const [isLoading, setIsLoading] = useState(false);
  const [integrationSettings, setIntegrationSettings] = useState<any[]>([]);
  const [importSettingsCV, setImportSettingsCV] = useState<CollectionView<ApiImportSettingItem> | null>(null);
  const [dataSourceList, setDataSourceList] = useState<any[]>([]);
  const [dataSourceMap, setDataSourceMap] = useState<DataMap | null>(null);

  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [selectedRowData, setSelectedRowData] = useState<ApiImportSettingItem | null>(null);

  const dropdownRef = useRef<HTMLDivElement | null>(null);
  const [createDropdownOpen, setCreateDropdownOpen] = useState(false);
  const [hoveredProviderId, setHoveredProviderId] = useState<string | number | null>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);

  const apiOperationsFlat = useMemo(() => {
    const list: { providerName: string; operation: ApiOperationItem }[] = [];
    integrationSettings.forEach((setting: any) => {
      const provider = setting.Name || '(No Provider Name)';
      const operations: ApiOperationItem[] = setting.AppIntergrationSettingParameterList || [];
      operations.forEach((op) => {
        const actionDisplay = setting.Name ? `${setting.Name} · ${op.ActionCode}` : op.ActionCode;
        list.push({ providerName: provider, operation: { ...op, actionDisplay } });
      });
    });
    return list;
  }, [integrationSettings]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [integrationList, importSettingList, dataSourceRegisterList] = await Promise.all([
        integrationService.retrieveAllAppIntegrationSettingDto(false),
        integrationService.retrieveAllApiStagingTableImportSettingDtoList(),
        adminSvc.getDataSourceRegisterList(false),
      ]);

      const safeIntegrationList = Array.isArray(integrationList) ? integrationList : [];
      setIntegrationSettings(safeIntegrationList);

      const safeImportList: ApiImportSettingItem[] = Array.isArray(importSettingList) ? importSettingList : [];
      const cv = new CollectionView<ApiImportSettingItem>(safeImportList);
      cv.groupDescriptions.push(new PropertyGroupDescription('ProviderName'));
      cv.groupDescriptions.push(new PropertyGroupDescription('ParentOperationName'));
      setImportSettingsCV(cv);

      const dsList = Array.isArray(dataSourceRegisterList) ? dataSourceRegisterList : [];
      setDataSourceList(dsList);
      if (dsList.length > 0) {
        const map = new DataMap(dsList, 'Id', 'DataSourceName');
        setDataSourceMap(map);
      } else {
        setDataSourceMap(null);
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to load REST API import settings');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const closeContextMenu = useCallback(() => {
    setContextMenuOpen(false);
    setSelectedRowData(null);
  }, []);

  const openContextMenu = useCallback(
    (e: React.MouseEvent, item: ApiImportSettingItem) => {
      e.preventDefault();
      e.stopPropagation();
      setSelectedRowData(item);
      setContextMenuPos({ x: e.clientX, y: e.clientY });
      setContextMenuOpen(true);
    },
    [],
  );

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenuOpen) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenuOpen, closeContextMenu]);

  const openImportEditor = useCallback(
    (dto: ApiImportSettingItem | null | undefined) => {
      if (!dto?.Id) return;
      if (onOpenImportEditor) {
        onOpenImportEditor(Number(dto.Id));
        return;
      }
      const label = dto.ActionCode || `Import Setting (${dto.Id})`;
      addTabAndNavigate('rest-api-import-editor', label, { id: dto.Id }, true);
    },
    [addTabAndNavigate, onOpenImportEditor],
  );

  const confirmSelectionAndClose = useCallback(() => {
    const flex = flexRef.current;
    if (!flex || !onConfirmSelection) return;
    const rowIndex = flex.selection?.row;
    if (rowIndex == null || rowIndex < 0) return;
    const dataItem = flex.rows[rowIndex]?.dataItem as ApiImportSettingItem | undefined;
    if (!dataItem?.Id) return;
    onConfirmSelection(dataItem);
    onRequestClose?.();
  }, [onConfirmSelection, onRequestClose]);

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const row = flex.selection?.row;
      if (row == null || row < 0) return;
      const dto = flex.rows[row]?.dataItem as ApiImportSettingItem | undefined;
      openImportEditor(dto);
    },
    [openImportEditor],
  );

  const deleteById = useCallback(
    async (id: number) => {
      try {
        dispatch(setIsBusy());
        const result = await integrationService.deleteOneAppIntegrationSettingParameter(String(id));
        if (result?.IsSuccessful) {
          await loadData();
        } else {
          const msg =
            (result?.ValidationResult && (result.ValidationResult.ErrorMessage || result.ValidationResult.Message)) ||
            'Failed to delete import setting';
          showWarning(msg);
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to delete import setting');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showError, showWarning],
  );

  const contextMenuEdit = useCallback(() => {
    if (!selectedRowData) {
      closeContextMenu();
      return;
    }
    const dto = selectedRowData;
    closeContextMenu();
    openImportEditor(dto);
  }, [selectedRowData, closeContextMenu, openImportEditor]);

  const contextMenuDelete = useCallback(() => {
    if (!selectedRowData?.Id) {
      closeContextMenu();
      return;
    }
    const confirmMsg = `Please confirm to delete:\n${selectedRowData.ActionCode ?? 'Import Setting'}`;
    // Simple browser confirm; for richer UX we could use AlertConfirmProvider later
    const ok = window.confirm(confirmMsg);
    if (ok) {
      deleteById(selectedRowData.Id);
    } else {
      closeContextMenu();
    }
  }, [selectedRowData, deleteById, closeContextMenu]);

  const createImportSettingFromApiOperation = useCallback(
    async (operationId: number) => {
      if (!operationId) return;
      try {
        dispatch(setIsBusy());
        const result = await integrationService.createStagingTableImportSettingFromApiOperation(String(operationId), false);
        if (result?.IsSuccessful && result.Object) {
          const dto = result.Object as ApiImportSettingItem;
          openImportEditor(dto);
          await loadData();
        } else if (result?.ValidationResult) {
          const validation =
            result.ValidationResult.ErrorMessage ||
            result.ValidationResult.Message ||
            JSON.stringify(result.ValidationResult);
          showWarning(validation);
        } else {
          showWarning('Failed to create import setting from API operation');
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to create import setting');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, openImportEditor, showError, showWarning],
  );

  const handleRefresh = useCallback(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setCreateDropdownOpen(false);
        setHoveredProviderId(null);
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, []);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header Toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {isUsedAsSelector ? 'REST API Table Import Setting Selector:' : 'API Operation Import Setting Management'}
        </div>
        <div className="flex items-center space-x-2">
          {isUsedAsSelector ? (
            <button
              type="button"
              onClick={confirmSelectionAndClose}
              disabled={isLoading}
              className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:opacity-60 ${theme.button_default}`}
              title="Confirm Selection & Close"
            >
              <i className="fa-solid fa-check" aria-hidden />
              <span>Confirm Selection &amp; Close</span>
            </button>
          ) : null}
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className="px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh</span>
          </button>

          {/* Import To New Table – menu/submenu matching Angular RestApiImportManagement.cshtml */}
          <div className="relative" ref={dropdownRef}>
            <button
              type="button"
              onClick={() => {
                setCreateDropdownOpen((v) => !v);
                setHoveredProviderId(null);
              }}
              disabled={isLoading || integrationSettings.length === 0}
              className="px-3 py-1.5 text-xs rounded-[4px] text-white bg-green-500 hover:bg-green-600 disabled:opacity-60 inline-flex items-center gap-1"
              title="Import To New Table From API Operation"
            >
              <i className="fa-solid fa-plus" aria-hidden />
              <span>Import To New Table</span>
              <i className="fa-solid fa-caret-down" aria-hidden />
            </button>
            {createDropdownOpen && (
              <div
                className={`absolute right-0 top-full mt-1 py-1 min-w-[250px] max-h-[500px] overflow-visible rounded-[4px] border shadow-lg z-10 text-xs ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <ul className="list-none m-0 p-0">
                  {integrationSettings.map((setting: any) => {
                    const operations: ApiOperationItem[] = setting.AppIntergrationSettingParameterList || [];
                    const hasOps = operations.length > 0;
                    const isHovered = hoveredProviderId === setting.Id;
                    const providerName = setting.Name || `Provider (${setting.Id})`;
                    return (
                      <li
                        key={setting.Id}
                        className="relative"
                        onMouseEnter={() => setHoveredProviderId(setting.Id)}
                        onMouseLeave={() => setHoveredProviderId(null)}
                      >
                        <div
                          className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 hover:bg-opacity-80 cursor-pointer ${hasOps ? '' : 'opacity-70'}`}
                        >
                          <i className="fa-solid fa-angle-left font-bold flex-shrink-0" aria-hidden />
                          <span className="truncate">{providerName}</span>
                        </div>
                        {hasOps && isHovered && (
                          <ul
                            className={`absolute right-full top-0 mr-0 py-1 min-w-[250px] max-h-[500px] overflow-y-auto list-none m-0 rounded-[4px] border shadow-lg z-20 text-xs ${theme.mainContentSection}`}
                            onMouseEnter={() => setHoveredProviderId(setting.Id)}
                          >
                            {operations.map((op) => (
                              <li key={op.Id}>
                                <button
                                  type="button"
                                  className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} hover:bg-opacity-80 block`}
                                  onClick={() => {
                                    setCreateDropdownOpen(false);
                                    setHoveredProviderId(null);
                                    createImportSettingFromApiOperation(op.Id);
                                  }}
                                >
                                  {op.ActionCode ?? String(op.Id)}
                                </button>
                              </li>
                            ))}
                          </ul>
                        )}
                      </li>
                    );
                  })}
                </ul>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Grid Content */}
      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="w-full h-full">
          <FlexGrid
            ref={flexRef}
            itemsSource={importSettingsCV ?? undefined}
            autoGenerateColumns={false}
            selectionMode="Row"
            isReadOnly
            allowDelete={false}
            initialized={(flex: wjGrid.FlexGrid) => {
              flexRef.current = flex;
              flex.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(flex));
            }}
            className="w-full h-full !border-0"
          >
            <FlexGridColumn isReadOnly width={100} header="Actions">
              <FlexGridCellTemplate
                cellType="Cell"
                template={(ctx: any) => (
                  <div className="flex justify-center w-full">
                    <button
                      type="button"
                      className={theme.menu_default}
                      style={{ width: '30px' }}
                      title="More Options"
                      onClick={(e) => openContextMenu(e, ctx.item)}
                    >
                      <i className="fa-solid fa-pencil" aria-hidden style={{ fontSize: '12px' }} />
                      <i
                        className="fa-solid fa-bars"
                        aria-hidden
                        style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }}
                      />
                    </button>
                  </div>
                )}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="ID" width={80} />
            <FlexGridColumn binding="ActionCode" header="Import Setting Name" width={240} />
            <FlexGridColumn binding="ActionDescription" header="Description" width={260} />
            <FlexGridColumn
              binding="DataSourceId"
              header="Import To Database"
              width={200}
              dataMap={dataSourceMap ?? undefined}
            />
            <FlexGridColumn binding="ProviderName" header="Provider" width={200} />
            <FlexGridColumn binding="ParentOperationName" header="API Operation" width={220} />
            <FlexGridColumn binding="" header="" width="*" isReadOnly />
          </FlexGrid>
        </div>
      </div>

      {/* Context menu */}
      {contextMenuOpen && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[200px]`}
          style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuEdit}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit Import Setting
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuDelete}
          >
            <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete Import Setting
          </button>
        </div>
      )}
    </div>
  );
};

export default RestApiImportManagement;
