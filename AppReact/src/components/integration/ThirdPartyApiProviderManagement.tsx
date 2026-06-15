import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { addTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { integrationService } from '../../webapi/integrationsvc';

type IntegrationSettingItem = {
  Id?: number | null;
  Name?: string;
  InternalCode?: string;
  Description?: string;
  DataSourceRegisterId?: number | null;
  AppModifiedDate?: string | null;
  AppCreatedDate?: string | null;
};

const extractValidationMessages = (validationResult: any): string[] => {
  if (!validationResult) return [];
  const items = validationResult?.Items ?? validationResult?.Errors ?? (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? item?.Description ?? '')
    .filter(Boolean);
};

const ThirdPartyApiProviderManagement: React.FC = () => {
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();
  const { showConfirm } = useAlertConfirm();

  const [isLoading, setIsLoading] = useState(false);
  const [, setList] = useState<IntegrationSettingItem[]>([]);
  const [collectionView, setCollectionView] = useState<CollectionView | null>(null);
  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState({ x: 0, y: 0 });
  const [selectedRowData, setSelectedRowData] = useState<IntegrationSettingItem | null>(null);
  const contextMenuRef = useRef<HTMLDivElement>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.retrieveAllAppIntegrationSettingDto(false);
      const items = Array.isArray(result) ? result : (result?.ObjectList ?? result?.Object ?? []) ?? [];
      const normalized = items as IntegrationSettingItem[];
      setList(normalized);
      const cv = new CollectionView(normalized);
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

  const handleRefresh = useCallback(() => {
    loadData();
  }, [loadData]);

  const handleCreate = useCallback(() => {
    const path = '/third-party-api-provider-editor';
    const label = 'New API Provider';
    dispatch(addTab({ tabPath: path, label, isClosable: true }));
    navigate(path);
  }, [dispatch, navigate]);

  const handleEdit = useCallback(
    (dto: IntegrationSettingItem | null) => {
      if (!dto) return;
      const id = dto.Id;
      const path = id != null ? `/third-party-api-provider-editor/${id}` : '/third-party-api-provider-editor';
      const label = dto.Name ? `Provider: ${dto.Name}` : (id != null ? `Provider (${id})` : 'New API Provider');
      dispatch(addTab({ tabPath: path, label, isClosable: true }));
      navigate(path);
    },
    [dispatch, navigate],
  );

  const openContextMenu = useCallback((e: React.MouseEvent, dataItem: IntegrationSettingItem) => {
    e.stopPropagation();
    setSelectedRowData(dataItem);
    setContextMenuPos({ x: e.clientX - 20, y: e.clientY - 5 });
    setContextMenuOpen(true);
  }, []);

  const closeContextMenu = useCallback(() => {
    setSelectedRowData(null);
    setContextMenuOpen(false);
  }, []);

  const contextMenuEdit = useCallback(() => {
    if (selectedRowData) {
      handleEdit(selectedRowData);
      closeContextMenu();
    }
  }, [selectedRowData, handleEdit, closeContextMenu]);

  const deleteById = useCallback(
    async (integrationSettingId: number) => {
      try {
        const result = await integrationService.deleteOneAppIntegrationSetting(String(integrationSettingId));
        if (result?.IsSuccessful) {
          errorMessage.showInfo('Deleted successfully.');
          loadData();
        } else {
          const messages = extractValidationMessages(result?.ValidationResult);
          if (messages.length) {
            messages.forEach((msg) => errorMessage.showError(msg));
          } else {
            errorMessage.showError('Failed to delete.');
          }
        }
      } catch (error) {
        errorMessage.showError(error instanceof Error ? error.message : String(error));
      }
    },
    [errorMessage, loadData],
  );

  const contextMenuDelete = useCallback(async () => {
    if (!selectedRowData) {
      closeContextMenu();
      return;
    }
    const id = selectedRowData.Id;
    closeContextMenu();
    if (id == null) return;
    const confirmed = await showConfirm(
      `Please confirm to delete:\n${selectedRowData.Name ?? 'Item'}`,
      { title: 'Delete API Provider' },
    );
    if (confirmed) {
      await deleteById(id);
    }
  }, [selectedRowData, showConfirm, deleteById, closeContextMenu]);

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const row = flex.selection?.row;
      if (row == null || row < 0) return;
      const dto = flex.rows[row]?.dataItem as IntegrationSettingItem | undefined;
      if (dto) handleEdit(dto);
    },
    [handleEdit],
  );

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenuOpen) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenuOpen, closeContextMenu]);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>API Provider List (3rd Part API Provider)</div>
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
          <button
            type="button"
            onClick={handleCreate}
            disabled={isLoading}
            className="w-8 h-6 inline-flex items-center justify-center rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
            title="Create"
          >
            <i className="fa fa-plus" aria-hidden />
          </button>
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
            showGroups={true}
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
            <FlexGridColumn binding="Id" header="ID" width={100} format="d" isReadOnly />
            <FlexGridColumn binding="Name" header="Name" width={300} />
            <FlexGridColumn binding="InternalCode" header="Internal Code" width={150} />
            <FlexGridColumn binding="Description" header="Description" width={150} />
            <FlexGridColumn binding="DataSourceRegisterId" header="DataSource Register Id" width={150} />
            <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150} dataType="Date" />
            <FlexGridColumn binding="AppCreatedDate" header="Created Date" width={150} dataType="Date" visible={false} />
            <FlexGridColumn binding="Description" header="Description" width="*" />
          </FlexGrid>
        </div>
      </div>

      {contextMenuOpen && (
        <div
          ref={contextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[150px]`}
          style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuEdit}
          >
            <i className="fa fa-edit mr-2" aria-hidden /> Edit
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

export default ThirdPartyApiProviderManagement;
