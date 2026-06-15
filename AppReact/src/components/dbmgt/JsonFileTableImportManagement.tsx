/**
 * JSON File Import settings list – mirrors Angular jsonFileTableImportManagementCtrl +
 * JsonFileTableImportManagement.cshtml.
 * List: RetrieveAllJsonFileTableImportSettingDtoList.
 * New table from uploaded file: CreateJsonFileDatabaseTableImportSettingByFileId.
 * Optional: CreateJsonDatabaseTableImportSettingFromJsonText (sample JSON + datasource).
 * Edit: rest-api-import-editor with jsonFileImport param (JsonFileTableImportEditor view parity).
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useDispatch, useSelector } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import type { RootState } from '../../redux/store';
import FileUploader from '../common/FileUploader';
import type { DataImageUploadResult } from '../../webapi/dataImageUploadSvc';
import { useAlertConfirm } from '../common/AlertConfirmProvider';

type JsonImportSettingItem = {
  Id: number;
  ActionCode: string;
  ActionDescription?: string;
  DataSourceId?: number;
  ExternalFieldName?: string;
  GeneratedTableNameList?: string[];
  ProviderName?: string;
  ParentOperationName?: string;
};

function extractValidationMessages(validationResult: any): string[] {
  if (!validationResult) return [];
  const items =
    validationResult?.Items ??
    validationResult?.Errors ??
    (Array.isArray(validationResult) ? validationResult : []);
  return (items as any[])
    .map((item) => item?.LocalizedMessage ?? item?.ErrorMessage ?? item?.Message ?? '')
    .filter(Boolean);
}

export type JsonFileTableImportManagementProps = {
  isUsedAsSelector?: boolean;
  onConfirmSelection?: (item: JsonImportSettingItem) => void;
  onRequestClose?: () => void;
  onOpenImportEditor?: (importSettingId: number) => void;
};

const JsonFileTableImportManagement: React.FC<JsonFileTableImportManagementProps> = (props) => {
  const { isUsedAsSelector = false, onConfirmSelection, onRequestClose, onOpenImportEditor } = props;
  const { theme } = useTheme();
  const { showError, showWarning, showInfo } = useErrorMessage();
  const { showAlert } = useAlertConfirm();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();

  const publicFileFolderId = useSelector((state: RootState) => {
    const raw = state.userSession?.userContext?.DictAppSetup?.PublicFileFolderId;
    return raw != null && raw !== '' ? Number(raw) : undefined;
  });

  const [isLoading, setIsLoading] = useState(false);
  const [importSettingsCV, setImportSettingsCV] = useState<CollectionView<JsonImportSettingItem> | null>(null);
  const [dataSourceList, setDataSourceList] = useState<any[]>([]);
  const [dataSourceMap, setDataSourceMap] = useState<DataMap | null>(null);

  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [selectedRowData, setSelectedRowData] = useState<JsonImportSettingItem | null>(null);

  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [createJsonText, setCreateJsonText] = useState('[\n  { "example": "value" }\n]');
  const [createDataSourceId, setCreateDataSourceId] = useState<number | null>(null);

  const importNewDropdownRef = useRef<HTMLDivElement | null>(null);
  const [importNewDropdownOpen, setImportNewDropdownOpen] = useState(false);
  const [fileUploadOpen, setFileUploadOpen] = useState(false);
  const uploadIntentRef = useRef<{ kind: 'new'; dsId: number } | { kind: 'staging'; settingId: number } | null>(null);
  const flexRef = useRef<wjGrid.FlexGrid | null>(null);

  const openImportEditor = useCallback(
    (id: number, label?: string) => {
      if (onOpenImportEditor) {
        onOpenImportEditor(id);
        return;
      }
      const lbl = label || `JSON Import (${id})`;
      addTabAndNavigate('rest-api-import-editor', lbl, { id, jsonFileImport: true }, true);
    },
    [addTabAndNavigate, onOpenImportEditor],
  );

  const confirmSelectionAndClose = useCallback(() => {
    const flex = flexRef.current;
    if (!flex || !onConfirmSelection) return;
    const rowIndex = flex.selection?.row;
    if (rowIndex == null || rowIndex < 0) return;
    const dataItem = flex.rows[rowIndex]?.dataItem as JsonImportSettingItem | undefined;
    if (!dataItem?.Id) return;
    onConfirmSelection(dataItem);
    onRequestClose?.();
  }, [onConfirmSelection, onRequestClose]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [importSettingList, dataSourceRegisterList] = await Promise.all([
        integrationService.retrieveAllJsonFileTableImportSettingDtoList(),
        adminSvc.getDataSourceRegisterList(false),
      ]);

      const safeImportList: JsonImportSettingItem[] = Array.isArray(importSettingList) ? importSettingList : [];
      const cv = new CollectionView<JsonImportSettingItem>(safeImportList);
      setImportSettingsCV(cv);

      const dsList = Array.isArray(dataSourceRegisterList) ? dataSourceRegisterList : [];
      setDataSourceList(dsList);
      if (dsList.length > 0) {
        setDataSourceMap(new DataMap(dsList, 'Id', 'DataSourceName'));
        setCreateDataSourceId((prev) => prev ?? dsList[0].Id);
      } else {
        setDataSourceMap(null);
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to load JSON file import settings');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (importNewDropdownRef.current && !importNewDropdownRef.current.contains(e.target as Node)) {
        setImportNewDropdownOpen(false);
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, []);

  const closeContextMenu = useCallback(() => {
    setContextMenuOpen(false);
    setSelectedRowData(null);
  }, []);

  const openContextMenu = useCallback((e: React.MouseEvent, item: JsonImportSettingItem) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedRowData(item);
    setContextMenuPos({ x: e.clientX, y: e.clientY });
    setContextMenuOpen(true);
  }, []);

  useEffect(() => {
    const handler = () => closeContextMenu();
    if (contextMenuOpen) {
      document.addEventListener('click', handler);
      return () => document.removeEventListener('click', handler);
    }
  }, [contextMenuOpen, closeContextMenu]);

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const row = flex.selection?.row;
      if (row == null || row < 0) return;
      const dto = flex.rows[row]?.dataItem as JsonImportSettingItem | undefined;
      if (dto) {
        const label = dto.ActionCode || `JSON Import (${dto.Id})`;
        openImportEditor(dto.Id, label);
      }
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
    const label = dto.ActionCode || `JSON Import (${dto.Id})`;
    openImportEditor(dto.Id, label);
  }, [selectedRowData, closeContextMenu, openImportEditor]);

  const contextMenuDelete = useCallback(() => {
    if (!selectedRowData?.Id) {
      closeContextMenu();
      return;
    }
    const ok = window.confirm(`Please confirm to delete:\n${selectedRowData.ActionCode ?? 'Import Setting'}`);
    if (ok) {
      deleteById(selectedRowData.Id);
    } else {
      closeContextMenu();
    }
  }, [selectedRowData, deleteById, closeContextMenu]);

  const handleCreateFromJsonText = useCallback(async () => {
    const text = createJsonText?.trim();
    if (!text) {
      showWarning('Paste a JSON sample (object or array).');
      return;
    }
    try {
      JSON.parse(text);
    } catch {
      showWarning('Invalid JSON. Fix the sample and try again.');
      return;
    }
    if (createDataSourceId == null) {
      showWarning('Select a target database (data source).');
      return;
    }
    try {
      dispatch(setIsBusy());
      const result = await integrationService.createJsonDatabaseTableImportSettingFromJsonText({
        JsonSampleData: text,
        DataSourceId: createDataSourceId,
      });
      if (result?.IsSuccessful && result.Object?.Id) {
        showInfo('Import setting created.');
        setCreateModalOpen(false);
        const dto = result.Object as JsonImportSettingItem;
        const label = dto.ActionCode || `JSON Import (${dto.Id})`;
        openImportEditor(dto.Id, label);
        await loadData();
      } else {
        const validation =
          result?.ValidationResult?.ErrorMessage ||
          result?.ValidationResult?.Message ||
          (result?.ValidationResult?.Items?.[0] as any)?.LocalizedMessage;
        showWarning(validation || 'Failed to create import setting from JSON.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to create import setting');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    createJsonText,
    createDataSourceId,
    dispatch,
    loadData,
    openImportEditor,
    showError,
    showInfo,
    showWarning,
  ]);

  const openNewImportUploader = useCallback((dsId: number) => {
    uploadIntentRef.current = { kind: 'new', dsId };
    setImportNewDropdownOpen(false);
    setFileUploadOpen(true);
  }, []);

  const contextMenuUpdateStagingFromJson = useCallback(() => {
    const row = selectedRowData;
    closeContextMenu();
    if (!row?.Id) return;
    const tables = row.GeneratedTableNameList;
    if (!Array.isArray(tables) || tables.length === 0) {
      showWarning('Synchronize table structure first so generated tables exist.');
      return;
    }
    uploadIntentRef.current = { kind: 'staging', settingId: row.Id };
    setFileUploadOpen(true);
  }, [selectedRowData, closeContextMenu, showWarning]);

  const handleJsonFileUploaded = useCallback(
    async (result: DataImageUploadResult) => {
      const fileId = result.FileId;
      if (fileId == null) {
        showWarning('Upload did not return a file id.');
        uploadIntentRef.current = null;
        return;
      }
      const intent = uploadIntentRef.current;
      uploadIntentRef.current = null;
      setFileUploadOpen(false);

      if (!intent) return;

      if (intent.kind === 'new') {
        await showAlert(
          'The file has been uploaded for database table(s) import.\n\nPlease click OK to continue the import process.',
          { title: 'Message' },
        );
        dispatch(setIsBusy());
        try {
          const data = await integrationService.createJsonFileDatabaseTableImportSettingByFileId(
            String(fileId),
            String(intent.dsId),
            false,
          );
          if (data?.IsSuccessful && data.Object?.Id) {
            const obj = data.Object as JsonImportSettingItem;
            const label = obj.ActionCode || `JSON Import (${obj.Id})`;
            openImportEditor(obj.Id, label);
            await loadData();
          } else {
            const msgs = extractValidationMessages(data?.ValidationResult);
            showWarning(msgs.length ? msgs.join('\n') : 'Failed to create import setting from JSON file.');
          }
        } catch (error: any) {
          showError(error?.message || 'Failed to create import setting from file');
        } finally {
          dispatch(setIsNotBusy());
        }
        return;
      }

      await showAlert(
        'The file has been uploaded for staging table data update.\n\nPlease click OK to continue the update process.',
        { title: 'Message' },
      );
      dispatch(setIsBusy());
      try {
        const data = await integrationService.updateStagingTableDataFromJsonUpload(String(intent.settingId), String(fileId));
        const msgs = extractValidationMessages(data?.ValidationResult);
        if (data?.IsSuccessful) {
          if (msgs.length) showInfo(msgs.join('\n'));
          else showInfo('Staging table data update completed.');
        } else if (msgs.length) {
          showWarning(msgs.join('\n'));
        } else {
          showWarning('Update staging table from JSON failed.');
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to update staging data from JSON');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, openImportEditor, showAlert, showError, showInfo, showWarning],
  );

  const dataSourceDisplayName = useCallback((ds: any) => {
    const id = ds?.Id;
    const name = ds?.DataSourceName ?? '';
    if (id === 2147483647) return `On Master DB (${id})`;
    return `On ${name} (${id})`;
  }, []);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {isUsedAsSelector ? 'JSON File Import Setting Selector:' : 'JSON File Import Setting Management'}
        </div>
        <div className="flex items-center gap-2 flex-wrap">
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
            onClick={() => loadData()}
            disabled={isLoading}
            className={`px-3 py-1.5 inline-flex items-center gap-1.5 rounded-[4px] text-sm disabled:opacity-60 ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            <span>Refresh</span>
          </button>

          {dataSourceList.length >= 2 ? (
            <div className="relative" ref={importNewDropdownRef}>
              <button
                type="button"
                onClick={() => setImportNewDropdownOpen((v) => !v)}
                disabled={isLoading || dataSourceList.length === 0}
                className="px-3 py-1.5 text-sm rounded-[4px] text-white bg-green-500 hover:bg-green-600 disabled:opacity-60 inline-flex items-center gap-1"
                title="Import to new table from uploaded JSON file"
              >
                <i className="fa-solid fa-file-import" aria-hidden />
                <span>Import To New Table</span>
                <i className="fa-solid fa-caret-down" aria-hidden />
              </button>
              {importNewDropdownOpen && (
                <div
                  className={`absolute right-0 top-full mt-1 py-1 min-w-[240px] max-h-[400px] overflow-y-auto rounded-[4px] border shadow-lg z-20 text-xs ${theme.mainContentSection}`}
                  onClick={(e) => e.stopPropagation()}
                >
                  {dataSourceList.map((ds: any) => (
                    <button
                      key={ds.Id}
                      type="button"
                      className={`w-full text-left px-3 py-2 text-xs ${theme.contextMenu} hover:bg-opacity-80`}
                      onClick={() => openNewImportUploader(ds.Id)}
                    >
                      {dataSourceDisplayName(ds)}
                    </button>
                  ))}
                </div>
              )}
            </div>
          ) : (
            <button
              type="button"
              onClick={() => dataSourceList[0] && openNewImportUploader(dataSourceList[0].Id)}
              disabled={isLoading || dataSourceList.length !== 1}
              className="px-3 py-1.5 text-sm rounded-[4px] text-white bg-green-500 hover:bg-green-600 disabled:opacity-60 inline-flex items-center gap-1"
              title="Import to new table from uploaded JSON file"
            >
              <i className="fa-solid fa-file-import" aria-hidden />
              <span>Import To New Table</span>
            </button>
          )}

          <button
            type="button"
            onClick={() => setCreateModalOpen(true)}
            disabled={isLoading || dataSourceList.length === 0}
            className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-60 inline-flex items-center gap-1 ${theme.button_default}`}
            title="Create import setting from sample JSON (pasted text)"
          >
            <i className="fa-solid fa-plus" aria-hidden />
            <span>New from JSON sample</span>
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
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
          <FlexGridColumn binding="ExternalFieldName" header="Original File" width={250} />
          <FlexGridColumn
            binding="DataSourceId"
            header="Import To Database"
            width={200}
            dataMap={dataSourceMap ?? undefined}
          />
          <FlexGridColumn binding="" header="" width="*" isReadOnly />
        </FlexGrid>
      </div>

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
            <i className="fa-solid fa-pen-to-square mr-2 shrink-0" aria-hidden /> Edit Import Setting
          </button>
          {selectedRowData &&
            Array.isArray(selectedRowData.GeneratedTableNameList) &&
            selectedRowData.GeneratedTableNameList.length > 0 && (
              <button
                type="button"
                className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
                onClick={contextMenuUpdateStagingFromJson}
              >
                <i className="fa-solid fa-upload mr-2 shrink-0" aria-hidden /> Update Staging Table From Json
              </button>
            )}
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuDelete}
          >
            <i className="fa-solid fa-trash mr-2 shrink-0" aria-hidden /> Delete Import Setting
          </button>
        </div>
      )}

      {createModalOpen && (
        <div
          className="fixed inset-0 z-[10002] flex items-center justify-center bg-black/50"
          onClick={() => setCreateModalOpen(false)}
        >
          <div
            className={`flex flex-col rounded-lg shadow-xl border max-w-2xl w-full max-h-[90vh] mx-4 ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
              <h3 className={`text-base font-semibold ${theme.title}`}>New JSON table import</h3>
              <button
                type="button"
                onClick={() => setCreateModalOpen(false)}
                className={`p-1.5 rounded ${theme.button_default}`}
                aria-label="Close"
              >
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className={`px-4 py-3 space-y-3 overflow-y-auto ${theme.mainContentSection}`}>
              <div>
                <label className={`block text-xs mb-1 ${theme.label}`}>Target database</label>
                <select
                  value={createDataSourceId ?? ''}
                  onChange={(e) => setCreateDataSourceId(e.target.value ? Number(e.target.value) : null)}
                  className={`w-full h-8 px-2 text-xs border ${theme.inputBox}`}
                >
                  {dataSourceList.map((ds: any) => (
                    <option key={ds.Id} value={ds.Id}>
                      {ds.DataSourceName}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className={`block text-xs mb-1 ${theme.label}`}>Sample JSON (object or array)</label>
                <textarea
                  value={createJsonText}
                  onChange={(e) => setCreateJsonText(e.target.value)}
                  rows={12}
                  className={`w-full font-mono text-xs p-2 border rounded ${theme.inputBox}`}
                  spellCheck={false}
                />
              </div>
            </div>
            <div className={`flex justify-end gap-2 px-4 py-3 border-t ${theme.mainContentSection}`}>
              <button
                type="button"
                onClick={() => setCreateModalOpen(false)}
                className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleCreateFromJsonText}
                className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Create and open editor
              </button>
            </div>
          </div>
        </div>
      )}

      <FileUploader
        isOpen={fileUploadOpen}
        onClose={() => {
          setFileUploadOpen(false);
          uploadIntentRef.current = null;
        }}
        mode="appFile"
        appFileCallingFrom="File"
        targetFolderId={publicFileFolderId}
        accept=".json,application/json,text/json,*/*"
        title="Upload JSON file"
        onUploaded={handleJsonFileUploaded}
      />
    </div>
  );
};

export default JsonFileTableImportManagement;
