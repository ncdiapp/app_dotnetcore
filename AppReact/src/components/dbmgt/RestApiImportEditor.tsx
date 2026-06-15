/**
 * RestApiImportEditor – full migration from Angular main.RestApiImportEditor
 * (integrationSettingParameterEditorCtrl + RestApiImportEditor.cshtml)
 * Toolbar: Refresh, Save Setting, Reset To Default Mapping, Update/Import Tables dropdown, Preview Tables.
 * Form: Import Setting Name, From API Operation, Import Description, Staging Table Datasource, Force Drop & Re-create.
 * Mapping: API Response/Payload Data Schema To Database Tables (schema tree left, property grid right).
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ComboBox } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { useDispatch, useSelector } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { integrationService } from '../../webapi/integrationsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { normalizeIntegrationSettingParameterForSave } from '../../helper/integrationPayloadHelper';
import TablesDataPreviewModal, { type TablePreviewItem } from '../transaction/TablesDataPreviewModal';
import type { RootState } from '../../redux/store';
import FileUploader from '../common/FileUploader';
import type { DataImageUploadResult } from '../../webapi/dataImageUploadSvc';

type RouteParams = { param?: string };

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

/** Flatten tree of nodes (with Children) for display. */
function flattenMappingNodes(nodes: any[], out: any[] = []): any[] {
  if (!Array.isArray(nodes)) return out;
  for (const node of nodes) {
    out.push(node);
    if (Array.isArray(node.Children) && node.Children.length > 0) {
      flattenMappingNodes(node.Children, out);
    }
  }
  return out;
}

/** Process mode: 0=Ignore, 1=Create New Table, 2=Aggregate To Parent, 3=Serialize To Parent. */
const PROCESS_MODE_LIST = [
  { Id: 0, Display: 'Ignore' },
  { Id: 1, Display: 'Create New Table' },
  { Id: 2, Display: 'Aggregate To Parent' },
  { Id: 3, Display: 'Serialize To Parent' },
];

const PROPERTY_FORMAT_LIST = [
  { Id: 'long', Display: 'Long Integer' },
  { Id: 'max', Display: 'String Max Length' },
  { Id: 'date', Display: 'Date' },
  { Id: 'date-time', Display: 'Datetime' },
];

const PROCESS_MODE_CREATE_TABLE = 1;
const PROCESS_MODE_MAP_TO_EXISTING_TABLE = 4;

export interface RestApiImportEditorProps {
  ignoreRouteParam?: boolean;
  importSettingId?: number | null;
  jsonFileImport?: boolean;
  onClose?: () => void;
}

const RestApiImportEditor: React.FC<RestApiImportEditorProps> = (props) => {
  const { ignoreRouteParam = false, importSettingId: importSettingIdProp = null, jsonFileImport: jsonFileImportProp = false } = props;
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const { param } = useParams<RouteParams>();

  const [importSetting, setImportSetting] = useState<any | null>(null);
  const [dataSourceCV, setDataSourceCV] = useState<CollectionView | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isBusyLocal, setIsBusyLocal] = useState(false);
  const [updateTablesDropdownOpen, setUpdateTablesDropdownOpen] = useState(false);
  const [selectedNodeSetting, setSelectedNodeSetting] = useState<any | null>(null);
  const [selectedNodeDisplayName, setSelectedNodeDisplayName] = useState<string | null>(null);
  const [previewTablesOpen, setPreviewTablesOpen] = useState(false);
  /** Incremented on each load so grids remount and bind to fresh server data, not cached refs */
  const [dataVersion, setDataVersion] = useState(0);
  const [resetImportDropdownOpen, setResetImportDropdownOpen] = useState(false);
  const [jsonEditorModalOpen, setJsonEditorModalOpen] = useState(false);
  const [jsonEditorDraft, setJsonEditorDraft] = useState('');
  const [schemaFileUploadOpen, setSchemaFileUploadOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const resetImportDropdownRef = useRef<HTMLDivElement>(null);
  const flexHierarchyRef = useRef<wjGrid.FlexGrid | null>(null);

  const publicFileFolderId = useSelector((state: RootState) => {
    const raw = state.userSession?.userContext?.DictAppSetup?.PublicFileFolderId;
    return raw != null && raw !== '' ? Number(raw) : undefined;
  });

  const routeParams = React.useMemo(() => {
    if (ignoreRouteParam || !param) return { importSettingId: null as number | null, jsonFileImportFromRoute: false };
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const idValue = obj.id ?? obj.Id ?? null;
      const asNumber = idValue == null ? NaN : Number(idValue);
      const jf = obj.jsonFileImport === true || obj.jsonFileImport === 'true';
      return {
        importSettingId: Number.isNaN(asNumber) ? null : asNumber,
        jsonFileImportFromRoute: jf,
      };
    } catch {
      const asNumber = Number(param);
      return {
        importSettingId: Number.isNaN(asNumber) ? null : asNumber,
        jsonFileImportFromRoute: false,
      };
    }
  }, [param, ignoreRouteParam]);

  const importSettingId = importSettingIdProp ?? routeParams.importSettingId;
  const jsonFileImportFromRoute = jsonFileImportProp || routeParams.jsonFileImportFromRoute;

  const loadData = useCallback(async () => {
    if (importSettingId == null) {
      showWarning(
        jsonFileImportFromRoute
          ? 'Import setting id is missing. Open this editor from JSON File Import Management.'
          : 'Import setting id is missing. Open this editor from REST API Import Management.',
      );
      return;
    }
    dispatch(setIsBusy());
    try {
      const [settingDto, dsList] = await Promise.all([
        integrationService.retrieveOneAppIntegrationSettingParameterExDto(String(importSettingId), true),
        adminSvc.getDataSourceRegisterList(false),
      ]);
      const freshSetting =
        settingDto != null && typeof settingDto === 'object'
          ? (JSON.parse(JSON.stringify(settingDto)) as any)
          : settingDto ?? null;
      setImportSetting(freshSetting);
      setSelectedNodeSetting(null);
      setSelectedNodeDisplayName(null);
      setDataVersion((v) => v + 1);
      const list = Array.isArray(dsList) ? dsList : [];
      setDataSourceCV(new CollectionView(list));
    } catch (error: any) {
      showError(error?.message || 'Failed to load REST API import setting');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, importSettingId, jsonFileImportFromRoute, showError, showWarning]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setUpdateTablesDropdownOpen(false);
      }
      if (resetImportDropdownRef.current && !resetImportDropdownRef.current.contains(e.target as Node)) {
        setResetImportDropdownOpen(false);
      }
    };
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, []);

  const handleFieldChange = (field: string, value: any) => {
    setImportSetting((prev: any) => (prev ? { ...prev, [field]: value } : prev));
  };

  const handleOtherSettingsChange = (field: string, value: any) => {
    setImportSetting((prev: any) => {
      if (!prev) return prev;
      const other = prev.OtherSettingsDto || {};
      return { ...prev, OtherSettingsDto: { ...other, [field]: value } };
    });
  };

  const handleSave = useCallback(async () => {
    if (!importSetting) return;
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const snapshot =
        typeof importSetting === 'object' && importSetting !== null
          ? (JSON.parse(JSON.stringify(importSetting)) as any)
          : importSetting;
      const payload = normalizeIntegrationSettingParameterForSave(snapshot);
      const result = await integrationService.saveAppIntegrationSettingParameterExDto(payload);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        showInfo('Import setting saved.');
        await loadData();
      } else if (messages.length > 0) {
        showWarning(messages.join('\n'));
      } else {
        showWarning('Failed to save import setting.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to save import setting');
    } finally {
      dispatch(setIsNotBusy());
      setIsSaving(false);
    }
  }, [dispatch, importSetting, loadData, showError, showInfo, showWarning]);

  const handleResetToDefaultMapping = useCallback(async () => {
    if (!importSetting?.Id) return;
    setResetImportDropdownOpen(false);
    const confirmed = await showConfirm('Please confirm to reset with current JSON.', {
      title: 'Reset To Default Mapping',
      confirmLabel: 'Yes',
      cancelLabel: 'No',
    });
    if (!confirmed) return;
    setIsBusyLocal(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.generateDefaultSchemaAndDataSetMappingFromSampleJson(importSetting);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        showInfo('Mapping reset. Reloading.');
        await loadData();
      } else if (messages.length > 0) {
        showWarning(messages.join('\n'));
      } else {
        showWarning('Failed to reset mapping.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to reset mapping');
    } finally {
      dispatch(setIsNotBusy());
      setIsBusyLocal(false);
    }
  }, [importSetting, dispatch, loadData, showConfirm, showError, showInfo, showWarning]);

  const handleSynchronizeTableStructure = useCallback(async () => {
    if (!importSetting?.Id) return;
    setUpdateTablesDropdownOpen(false);
    setIsBusyLocal(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.generateTableAndScriptsFromSchemaDataSetMappingDto(importSetting);
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        const msg = result?.ValidationResult?.Items?.[0]?.LocalizedMessage ?? 'Table structure synchronized.';
        showInfo(msg);
        await loadData();
      } else if (messages.length > 0) {
        showWarning(messages.join('\n'));
      } else {
        showWarning('Failed to synchronize table structure.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to synchronize');
    } finally {
      dispatch(setIsNotBusy());
      setIsBusyLocal(false);
    }
  }, [importSetting, dispatch, loadData, showError, showInfo, showWarning]);

  const handleExecuteDataImport = useCallback(async () => {
    if (!importSetting?.Id) return;
    setUpdateTablesDropdownOpen(false);
    setIsBusyLocal(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.executeDataImportOnJsonFileTableImportSetting(String(importSetting.Id));
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        showInfo('Data import executed.');
        await loadData();
      } else if (messages.length > 0) {
        showWarning(messages.join('\n'));
      } else {
        showWarning('Execute data import failed.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to execute data import');
    } finally {
      dispatch(setIsNotBusy());
      setIsBusyLocal(false);
    }
  }, [importSetting, dispatch, loadData, showError, showInfo, showWarning]);

  const handleDropAllStagingTables = useCallback(async () => {
    if (!importSetting?.Id) return;
    const confirmed = await showConfirm(
      'All staging tables on current import mapping will be dropped. Select "Yes" to confirm.',
      { title: 'Force Drop Staging Tables', confirmLabel: 'Yes', cancelLabel: 'No' }
    );
    if (!confirmed) {
      setUpdateTablesDropdownOpen(false);
      return;
    }
    setUpdateTablesDropdownOpen(false);
    setIsBusyLocal(true);
    dispatch(setIsBusy());
    try {
      const result = await integrationService.dropAllStagingTablesByImportSettingId(String(importSetting.Id));
      const messages = extractValidationMessages(result?.ValidationResult);
      if (result?.IsSuccessful) {
        showInfo('Staging tables dropped.');
        await loadData();
      } else if (messages.length > 0) {
        showWarning(messages.join('\n'));
      } else {
        showWarning('Failed to drop staging tables.');
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to drop staging tables');
    } finally {
      dispatch(setIsNotBusy());
      setIsBusyLocal(false);
    }
  }, [importSetting, dispatch, loadData, showConfirm, showError, showInfo, showWarning]);

  const handleOpenParentApi = useCallback(() => {
    if (!importSetting?.OtherSettingsDto?.ParentOperationId) return;
    const parentId = importSetting.OtherSettingsDto.ParentOperationId;
    const label = importSetting.ParentOperationName || `API (${parentId})`;
    addTabAndNavigate('third-party-api-editor', label, { id: parentId }, true);
  }, [addTabAndNavigate, importSetting]);

  const mappingDto = importSetting?.SchemaDataSetMappingDto;
  const hierarchyNodesRaw = mappingDto?.HierachyNodeNameList ?? [];
  const nodeSettings = mappingDto?.NodeSettingDtoList ?? [];
  const tablePrefix = importSetting?.TablePrefix ?? '';

  /** Angular JsonFileTableImportEditor vs RestApiImportEditor: same controller, different template. */
  const isJsonFileImportUi = useMemo(() => {
    if (importSetting == null) return jsonFileImportFromRoute;
    if (jsonFileImportFromRoute) return true;
    return !importSetting?.OtherSettingsDto?.ParentOperationId;
  }, [importSetting, jsonFileImportFromRoute]);

  /** Derived from current mapping so "Generated tables" shows saved table names; server GeneratedTableNameList may not update until Sync */
  const generatedTableNames: string[] = useMemo(() => {
    const list = mappingDto?.NodeSettingDtoList ?? [];
    const prefix = tablePrefix ? `${tablePrefix}___` : '';
    const names: string[] = [];
    list.forEach((n: any) => {
      const mode = Number(n?.ProcessMode ?? n?.processMode);
      if (mode !== PROCESS_MODE_CREATE_TABLE && mode !== PROCESS_MODE_MAP_TO_EXISTING_TABLE) return;
      const name = n?.MappingToTableName ?? n?.NodeName ?? '';
      if (name) names.push(prefix + name);
    });
    return names.length > 0 ? names : (importSetting?.SchemaDataSetMappingDto?.GeneratedTableNameList ?? []);
  }, [mappingDto?.NodeSettingDtoList, tablePrefix, importSetting?.SchemaDataSetMappingDto?.GeneratedTableNameList]);

  const hasGeneratedTables = generatedTableNames.length > 0;

  const canExecuteDataImport = useMemo(() => {
    const top = importSetting?.GeneratedTableNameList;
    if (Array.isArray(top) && top.length > 0) return true;
    const mapList = importSetting?.SchemaDataSetMappingDto?.GeneratedTableNameList;
    if (Array.isArray(mapList) && mapList.length > 0) return true;
    return hasGeneratedTables;
  }, [importSetting, hasGeneratedTables]);

  const handleOpenJsonSourceEditor = useCallback(() => {
    const raw = importSetting?.JsonSampleData;
    setJsonEditorDraft(typeof raw === 'string' ? raw : raw != null ? JSON.stringify(raw, null, 2) : '');
    setJsonEditorModalOpen(true);
    setResetImportDropdownOpen(false);
  }, [importSetting?.JsonSampleData]);

  const handleApplyJsonEditorDraft = useCallback(() => {
    setImportSetting((prev: any) => (prev ? { ...prev, JsonSampleData: jsonEditorDraft } : prev));
    setJsonEditorModalOpen(false);
  }, [jsonEditorDraft]);

  const handleSchemaFileUploaded = useCallback(
    async (result: DataImageUploadResult) => {
      const fileId = result.FileId;
      if (fileId == null || !importSetting?.Id) {
        showWarning('Upload did not return a file id.');
        return;
      }
      setSchemaFileUploadOpen(false);
      const ok = await showConfirm(
        `File has been uploaded for JSON schema update.\n\nClick Yes to continue the update process.`,
        { title: 'Update schema', confirmLabel: 'Yes', cancelLabel: 'No' },
      );
      if (!ok) return;
      setIsBusyLocal(true);
      dispatch(setIsBusy());
      try {
        const data = await integrationService.updateJsonSchemaFromJsonUpload(String(importSetting.Id), String(fileId));
        const messages = extractValidationMessages(data?.ValidationResult);
        if (data?.IsSuccessful) {
          showInfo('Schema updated from file. Reloading.');
          await loadData();
        } else if (messages.length > 0) {
          showWarning(messages.join('\n'));
        } else {
          showWarning('Failed to update schema from JSON file.');
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to update schema from file');
      } finally {
        dispatch(setIsNotBusy());
        setIsBusyLocal(false);
      }
    },
    [dispatch, importSetting?.Id, loadData, showConfirm, showError, showInfo, showWarning],
  );

  const handlePreviewTables = useCallback(() => {
    if (generatedTableNames.length === 0) return;
    setPreviewTablesOpen(true);
  }, [generatedTableNames.length]);

  const dictDataSetMappingKeyAndDto = useMemo(() => {
    const list = mappingDto?.NodeSettingDtoList ?? [];
    const dict: Record<string, any> = {};
    list.forEach((n: any) => {
      if (n?.NodeName) dict[n.NodeName] = n;
    });
    return dict;
  }, [mappingDto?.NodeSettingDtoList]);

  useEffect(() => {
    const raw = mappingDto?.HierachyNodeNameList ?? [];
    if (raw.length === 0 || selectedNodeSetting != null) return;
    const first = raw[0];
    const schemaTypeName = first?.SchemaTypeName ?? first?.NodeName ?? first?.Name;
    const nodeDto = schemaTypeName ? dictDataSetMappingKeyAndDto[schemaTypeName] : null;
    if (nodeDto) {
      setSelectedNodeSetting(nodeDto);
      setSelectedNodeDisplayName(first?.Display ?? first?.SchemaTypeName ?? first?.NodeName ?? first?.Name ?? nodeDto?.NodeName ?? null);
      setTimeout(() => {
        const grid = flexHierarchyRef.current;
        if (grid?.rows?.length) {
          grid.select(0, true);
        }
      }, 0);
    }
  }, [mappingDto?.HierachyNodeNameList, selectedNodeSetting, dictDataSetMappingKeyAndDto]);

  const hierarchyCV = useMemo(() => {
    const raw = mappingDto?.HierachyNodeNameList ?? [];
    return new CollectionView(raw, { trackChanges: false });
  }, [mappingDto?.HierachyNodeNameList]);

  const nodeTypeNameCV = useMemo(() => {
    const list = mappingDto?.NodeTypeNameList ?? [];
    const items = Array.isArray(list)
      ? list.map((x: any) =>
          typeof x === 'string' ? { SchemaTypeName: x, Display: x } : { SchemaTypeName: x?.SchemaTypeName ?? x?.Display ?? '', Display: x?.Display ?? x?.SchemaTypeName ?? '' }
        )
      : [];
    return new CollectionView(items, { trackChanges: false });
  }, [mappingDto?.NodeTypeNameList]);

  const propertyFormatDataMap = useMemo(() => new DataMap(PROPERTY_FORMAT_LIST, 'Id', 'Display'), []);

  const selectedNodePropertiesCV = useMemo((): CollectionView<any> => {
    const props = selectedNodeSetting?.Properties;
    const list = Array.isArray(props) ? props : [];
    return new CollectionView(list, { trackChanges: false });
  }, [selectedNodeSetting?.Properties]);

  const handleMappingDtoChange = useCallback((field: string, value: any) => {
    setImportSetting((prev: any) => {
      if (!prev?.SchemaDataSetMappingDto) return prev;
      return {
        ...prev,
        SchemaDataSetMappingDto: { ...prev.SchemaDataSetMappingDto, [field]: value },
      };
    });
  }, []);

  const isPostOrPut =
    (importSetting?.HttpMethd ?? '').toString().toLowerCase() === 'post' ||
    (importSetting?.HttpMethd ?? '').toString().toLowerCase() === 'put';
  const mappingLabel = isPostOrPut ? 'API Payload' : 'API Response';

  const previewTablesList: TablePreviewItem[] = useMemo(() => {
    const dsId = importSetting?.DataSourceId ?? null;
    return generatedTableNames.map((tableName: string) => ({
      tableName,
      dataSourceId: dsId,
      schemaOwner: null,
    }));
  }, [generatedTableNames, importSetting?.DataSourceId]);

  const pageTitle = importSetting
    ? isJsonFileImportUi
      ? `JSON File Import Setting: ${importSetting.ActionCode ?? ''}`
      : `API Operation Import Setting: ${importSetting.ActionCode ?? ''}`
    : isJsonFileImportUi
      ? 'JSON File Import Setting'
      : 'API Operation Import Setting';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>{pageTitle}</div>
        <div className="flex items-center space-x-2 flex-wrap">
          <button
            type="button"
            onClick={loadData}
            className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default} disabled:opacity-60`}
          >
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save Setting
          </button>
          {isJsonFileImportUi ? (
            <div className="relative" ref={resetImportDropdownRef}>
              <button
                type="button"
                onClick={() => setResetImportDropdownOpen((v) => !v)}
                className={`px-3 py-1.5 text-xs rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
              >
                <i className="fa-solid fa-arrows-rotate" aria-hidden /> Reset Import Setting
                <i className="fa-solid fa-caret-down" aria-hidden />
              </button>
              {resetImportDropdownOpen && (
                <div
                  className={`absolute left-0 top-full mt-1 py-1 min-w-[260px] rounded-[4px] border shadow-lg z-30 text-xs ${theme.mainContentSection}`}
                  onClick={(e) => e.stopPropagation()}
                >
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu}`}
                    onClick={() => {
                      setResetImportDropdownOpen(false);
                      handleOpenJsonSourceEditor();
                    }}
                  >
                    <i className="fa-solid fa-pen-to-square" aria-hidden /> Edit Source JSON
                  </button>
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu}`}
                    onClick={() => {
                      setResetImportDropdownOpen(false);
                      setSchemaFileUploadOpen(true);
                    }}
                  >
                    <i className="fa-solid fa-upload" aria-hidden /> Update Source JSON From New File
                  </button>
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu}`}
                    onClick={handleResetToDefaultMapping}
                  >
                    <i className="fa-solid fa-arrows-rotate" aria-hidden /> Reset Import Setting From Source Json
                  </button>
                </div>
              )}
            </div>
          ) : (
            <button
              type="button"
              onClick={handleResetToDefaultMapping}
              className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            >
              <i className="fa-solid fa-arrows-rotate mr-1" aria-hidden /> Reset To Default Mapping
            </button>
          )}
          <div className="relative" ref={dropdownRef}>
            <button
              type="button"
              onClick={() => setUpdateTablesDropdownOpen((v) => !v)}
              className={`px-3 py-1.5 text-xs rounded-[4px] inline-flex items-center gap-1 ${theme.button_default}`}
            >
              <i className="fa-solid fa-database" aria-hidden /> Update/Import Tables
              <i className="fa-solid fa-caret-down" aria-hidden />
            </button>
            {updateTablesDropdownOpen && (
              <div
                className={`absolute right-0 top-full mt-1 py-1 min-w-[220px] rounded-[4px] border shadow-lg z-20 text-xs ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu}`}
                  onClick={handleSynchronizeTableStructure}
                >
                  <i className="fa-solid fa-table" aria-hidden /> Synchronize Table Structure From Mapping
                </button>
                <button
                  type="button"
                  disabled={!canExecuteDataImport}
                  className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu} disabled:opacity-60`}
                  onClick={handleExecuteDataImport}
                >
                  <i className="fa-solid fa-floppy-disk" aria-hidden /> Execute Data Import
                </button>
                <div className="border-t my-1 border-gray-200" />
                <button
                  type="button"
                  disabled={!canExecuteDataImport}
                  className={`w-full text-left px-4 py-2 flex items-center gap-2 ${theme.contextMenu} disabled:opacity-60`}
                  onClick={handleDropAllStagingTables}
                >
                  <i className="fa-solid fa-trash" aria-hidden /> Force Drop Staging Tables
                </button>
              </div>
            )}
          </div>
          {hasGeneratedTables && (
            <button
              type="button"
              onClick={handlePreviewTables}
              className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            >
              <i className="fa-solid fa-eye mr-1" aria-hidden /> Preview Tables
            </button>
          )}
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-auto ${theme.mainContentSection}`}>
        {!importSetting ? (
          <div className="w-full h-full flex items-center justify-center text-xs text-gray-500 px-4">
            {isJsonFileImportUi
              ? 'Select or create a JSON File Import Setting from JSON File Import Management.'
              : 'Select or create an API Operation Import Setting from the REST API Import Management page.'}
          </div>
        ) : (
          <div className="flex flex-col h-full px-4 py-3 gap-4">
            <div className="flex flex-wrap gap-6 flex-shrink-0">
              <div className="flex flex-col gap-2 min-w-[260px]">
                <div className="flex items-center">
                  <label className={`w-[150px] text-xs ${theme.label} mr-2`}>Import Setting Name</label>
                  <input
                    type="text"
                    autoComplete="off"
                    value={importSetting.ActionCode || ''}
                    onChange={(e) => handleFieldChange('ActionCode', e.target.value)}
                    className={`w-[200px] h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  />
                </div>
                {isJsonFileImportUi ? (
                  <div className="flex items-center">
                    <label className={`w-[150px] text-xs ${theme.label} mr-2`}>Original File</label>
                    <input
                      type="text"
                      autoComplete="off"
                      value={importSetting.ExternalFieldName || ''}
                      onChange={(e) => handleFieldChange('ExternalFieldName', e.target.value)}
                      className={`w-[200px] h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    />
                  </div>
                ) : (
                  <div className="flex items-center">
                    <label className={`w-[150px] text-xs ${theme.label} mr-2`}>From API Operation</label>
                    <div className="w-[200px] h-7 px-2 text-xs border bg-white flex items-center justify-between overflow-hidden rounded-[4px]">
                      <span className="truncate w-1 flex-auto min-w-0" title={importSetting.ParentOperationName}>
                        {importSetting.ParentOperationName || '(Not linked)'}
                      </span>
                      {importSetting.OtherSettingsDto?.ParentOperationId && (
                        <button
                          type="button"
                          onClick={handleOpenParentApi}
                          className="ml-2 w-6 h-5 flex items-center justify-center rounded-[4px] text-[11px] bg-gray-200 hover:bg-gray-300 shrink-0"
                          title="Open parent API operation"
                        >
                          <i className="fa-solid fa-pen-to-square" aria-hidden />
                        </button>
                      )}
                    </div>
                  </div>
                )}
              </div>
              <div className="flex flex-col gap-2 min-w-[260px] w-1 flex-auto">
                <div className="flex items-center">
                  <label className={`w-[160px] text-xs ${theme.label} mr-2`}>
                    {isJsonFileImportUi ? 'Description' : 'Import Description'}
                  </label>
                  <input
                    type="text"
                    autoComplete="off"
                    value={importSetting.ActionDescription || ''}
                    onChange={(e) => handleFieldChange('ActionDescription', e.target.value)}
                    className={`w-1 flex-auto min-w-0 h-7 max-w-[460px] px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  />
                </div>
                <div className="flex flex-wrap gap-4 items-center">
                  <div className="flex items-center">
                    <label className={`w-[160px] text-xs ${theme.label} mr-2`}>Staging Table Datasource</label>
                    <div className="w-[200px] h-7">
                      <ComboBox
                        itemsSource={dataSourceCV ?? undefined}
                        displayMemberPath="DataSourceName"
                        selectedValuePath="Id"
                        selectedValue={importSetting.DataSourceId ?? null}
                        isRequired={false}
                        isEditable={false}
                        onSelectedIndexChanged={(cb: wjInput.ComboBox) => {
                          const val = cb.selectedValue;
                          handleFieldChange('DataSourceId', val != null ? val : undefined);
                        }}
                        className="w-full h-full"
                      />
                    </div>
                  </div>
                  {!isJsonFileImportUi && (
                    <div className="flex items-center">
                      <label className={`text-xs ${theme.label} mr-2`}>Force Drop & Re-create Staging Tables</label>
                      <input
                        type="checkbox"
                        checked={!!importSetting.OtherSettingsDto?.IsForceRecreateStagingTables}
                        onChange={(e) =>
                          handleOtherSettingsChange('IsForceRecreateStagingTables', e.target.checked)
                        }
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="flex flex-col flex-1 min-h-0 border-t pt-3">
              <div className={`text-sm font-semibold mb-2 flex-shrink-0 ${theme.title}`}>
                {isJsonFileImportUi ? (
                  <>Mapping JSON Data Schema To Database Tables</>
                ) : (
                  <>
                    Mapping <span className="px-1">{mappingLabel}</span> Data Schema To Database Tables
                  </>
                )}
                <span className="font-normal pl-2 text-xs text-gray-500">
                  Once mapping completed, use &quot;Update/Import Tables&quot; to synchronize and run.
                </span>
              </div>
              <div className="flex-1 min-h-[280px] border border-gray-300 rounded-md overflow-hidden flex gap-2 p-2">
                <div className="w-[380px] flex-shrink-0 border border-gray-200 rounded overflow-hidden flex flex-col">
                  <div className="flex items-center gap-2 px-2 py-1.5 border-b flex-shrink-0">
                    <label className={`text-xs ${theme.label} whitespace-nowrap`}>Root Node Type Name</label>
                    <div className="w-1 flex-auto min-w-0 h-7">
                      <ComboBox
                        itemsSource={nodeTypeNameCV}
                        displayMemberPath="Display"
                        selectedValuePath="SchemaTypeName"
                        selectedValue={mappingDto?.RootNodeName ?? null}
                        isRequired={false}
                        isEditable={false}
                        onSelectedIndexChanged={(cb: wjInput.ComboBox) => {
                          const val = cb.selectedValue as string | null;
                          handleMappingDtoChange('RootNodeName', val ?? '');
                        }}
                        className="w-full h-full"
                      />
                    </div>
                  </div>
                  <div className="flex-1 min-h-0 overflow-hidden">
                    {hierarchyNodesRaw.length === 0 ? (
                      <div className="text-gray-500 p-2 text-xs">
                        No schema nodes. Use &quot;Reset To Default Mapping&quot; to generate from JSON.
                      </div>
                    ) : (
                      <FlexGrid
                        ref={flexHierarchyRef}
                        key={`hierarchy-${dataVersion}`}
                        itemsSource={hierarchyCV}
                        childItemsPath="Children"
                        selectionMode="Row"
                        headersVisibility="Column"
                        allowAddNew={false}
                        allowDelete={false}
                        selectionChanged={(grid: wjGrid.FlexGrid) => {
                          const row = grid.selection?.row ?? -1;
                          if (row < 0) return;
                          const item = grid.rows[row]?.dataItem;
                          const schemaTypeName = item?.SchemaTypeName ?? item?.NodeName ?? item?.Name;
                          const nodeDto = schemaTypeName ? dictDataSetMappingKeyAndDto[schemaTypeName] : null;
                          if (nodeDto) {
                            setSelectedNodeSetting(nodeDto);
                            setSelectedNodeDisplayName(item?.Display ?? item?.SchemaTypeName ?? item?.NodeName ?? item?.Name ?? nodeDto?.NodeName ?? null);
                          } else {
                            setSelectedNodeSetting(null);
                            setSelectedNodeDisplayName(null);
                          }
                        }}
                        style={{ height: '100%', border: 'none' }}
                      >
                        <FlexGridColumn header="Schema Node" binding="Display" width="*" />
                        <FlexGridColumn header="Process Node Value Mode" width={150}>
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item as any;
                              const key = item?.SchemaTypeName ?? item?.NodeName ?? item?.Name;
                              const nodeDto = key ? dictDataSetMappingKeyAndDto[key] : null;
                              const modeId = nodeDto?.ProcessMode;
                              if (!nodeDto) {
                                const display = PROCESS_MODE_LIST.find((m) => m.Id === modeId)?.Display ?? '—';
                                return <span className="text-xs">{display}</span>;
                              }
                              return (
                                <select
                                  value={modeId ?? ''}
                                  onChange={(e) => {
                                    const val = e.target.value;
                                    const num = val === '' ? undefined : parseInt(val, 10);
                                    nodeDto.ProcessMode = num;
                                    setImportSetting((prev: any) => (prev ? { ...prev } : prev));
                                  }}
                                  onClick={(e) => e.stopPropagation()}
                                  className={`w-full h-full min-h-6 text-xs border bg-transparent ${theme.inputBox} focus:outline-none cursor-pointer`}
                                >
                                  <option value="">—</option>
                                  {PROCESS_MODE_LIST.map((m) => (
                                    <option key={m.Id} value={m.Id}>
                                      {m.Display}
                                    </option>
                                  ))}
                                </select>
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn header="Aggregate Child" width={110}>
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item as any;
                              const key = item?.SchemaTypeName ?? item?.NodeName ?? item?.Name;
                              const nodeDto = key ? dictDataSetMappingKeyAndDto[key] : null;
                              const show = nodeDto && Number(nodeDto.ProcessMode) === PROCESS_MODE_CREATE_TABLE;
                              if (!show) return null;
                              return (
                                <input
                                  type="checkbox"
                                  checked={!!nodeDto?.IsNeedToRollUpAllChild}
                                  onChange={(e) => {
                                    if (nodeDto) {
                                      nodeDto.IsNeedToRollUpAllChild = e.target.checked;
                                      setImportSetting((prev: any) => (prev ? { ...prev } : prev));
                                    }
                                  }}
                                  className="m-auto"
                                />
                              );
                            }}
                          />
                        </FlexGridColumn>
                      </FlexGrid>
                    )}
                  </div>
                </div>

                <div className="flex-1 min-w-0 border border-gray-200 rounded overflow-hidden flex flex-col">
                  {selectedNodeSetting ? (
                    <>
                      <div className="flex flex-wrap items-center gap-2 px-2 py-1.5 border-b flex-shrink-0">
                        <span
                          className={`text-xs font-semibold truncate max-w-[calc(100%-420px)] ${theme.title}`}
                          title={selectedNodeDisplayName ?? selectedNodeSetting.NodeName}
                        >
                          {selectedNodeDisplayName ?? selectedNodeSetting.NodeName}
                          {selectedNodeSetting.IsSingleField && (
                            <span className="font-normal text-gray-500 ml-1">(Single Field)</span>
                          )}
                        </span>
                        {(Number(selectedNodeSetting.ProcessMode) === PROCESS_MODE_CREATE_TABLE ||
                          Number(selectedNodeSetting.ProcessMode) === PROCESS_MODE_MAP_TO_EXISTING_TABLE) && (
                          <>
                            <label className={`text-xs ${theme.label} whitespace-nowrap`}>
                              Mapping To TableName
                            </label>
                            <span className="text-gray-500 text-xs italic">
                              {tablePrefix ? `${tablePrefix}___` : ''}
                            </span>
                            <input
                              type="text"
                              value={selectedNodeSetting.MappingToTableName ?? ''}
                              onChange={(e) => {
                                selectedNodeSetting.MappingToTableName = e.target.value;
                                setImportSetting((prev: any) => (prev ? { ...prev } : prev));
                              }}
                              className={`w-1 flex-auto min-w-0 max-w-[400px] h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                            />
                          </>
                        )}
                      </div>
                      {!selectedNodeSetting.IsSingleField && (
                        <div className="h-1 flex-auto min-h-0 overflow-hidden">
                          <FlexGrid
                            key={`props-${selectedNodeSetting.NodeName}-${Number(selectedNodeSetting.ProcessMode)}-${dataVersion}`}
                            itemsSource={selectedNodePropertiesCV}
                            allowAddNew={false}
                            allowDelete={false}
                            headersVisibility="Column"
                            style={{ height: '100%', border: 'none' }}
                          >
                            <FlexGridColumn header="Property Name" binding="PropertyName" width={200} isReadOnly={true} />
                            <FlexGridColumn header="Default Data Type" binding="Type" width={150} isReadOnly={true} />
                            <FlexGridColumn
                              header="Overwrite Type"
                              binding="OverwirtType"
                              width={150}
                              dataMap={propertyFormatDataMap}
                            />
                            <FlexGridColumn
                              header="Is Logical Key"
                              binding="IsLogicalKey"
                              width={120}
                              dataType="Boolean"
                              visible={
                                (() => {
                                  const mode = Number(selectedNodeSetting.ProcessMode ?? selectedNodeSetting?.processMode);
                                  return mode === PROCESS_MODE_CREATE_TABLE || mode === PROCESS_MODE_MAP_TO_EXISTING_TABLE;
                                })()
                              }
                            />
                            <FlexGridColumn
                              header="Is Create Column"
                              binding="IsCreateColumn"
                              width={120}
                              dataType="Boolean"
                            />
                            <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
                          </FlexGrid>
                        </div>
                      )}
                    </>
                  ) : (
                    <>
                      <div className={`px-2 py-1.5 border-b text-xs ${theme.label}`}>Generated tables</div>
                      <div className="h-1 flex-auto overflow-auto p-2 text-xs">
                        {generatedTableNames.length === 0 ? (
                          <div className="text-gray-500">
                            Select a schema node (left) to edit property mapping, or synchronize table structure to
                            see generated tables.
                          </div>
                        ) : (
                          <ul className="list-disc pl-4">
                            {generatedTableNames.map((t: string, i: number) => (
                              <li key={i}>{t}</li>
                            ))}
                          </ul>
                        )}
                      </div>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {jsonEditorModalOpen && (
        <div
          className="fixed inset-0 z-[10002] flex items-center justify-center bg-black/50"
          onClick={() => setJsonEditorModalOpen(false)}
        >
          <div
            className={`flex flex-col rounded-lg shadow-xl border max-w-3xl w-full max-h-[90vh] mx-4 ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
              <h3 className={`text-base font-semibold ${theme.title}`}>JSON editor</h3>
              <button
                type="button"
                onClick={() => setJsonEditorModalOpen(false)}
                className={`p-1.5 rounded ${theme.button_default}`}
                aria-label="Close"
              >
                <i className="fa-solid fa-times" aria-hidden />
              </button>
            </div>
            <div className={`px-4 py-3 ${theme.mainContentSection}`}>
              <textarea
                value={jsonEditorDraft}
                onChange={(e) => setJsonEditorDraft(e.target.value)}
                rows={18}
                spellCheck={false}
                className={`w-full font-mono text-xs p-2 border rounded min-h-[280px] ${theme.inputBox}`}
              />
            </div>
            <div className={`flex justify-end gap-2 px-4 py-3 border-t ${theme.mainContentSection}`}>
              <button
                type="button"
                onClick={() => setJsonEditorModalOpen(false)}
                className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleApplyJsonEditorDraft}
                className={`px-4 py-2 text-sm rounded-[4px] ${theme.button_default}`}
              >
                Apply & Close
              </button>
            </div>
          </div>
        </div>
      )}

      <FileUploader
        isOpen={schemaFileUploadOpen}
        onClose={() => setSchemaFileUploadOpen(false)}
        mode="appFile"
        appFileCallingFrom="File"
        targetFolderId={publicFileFolderId}
        accept=".json,application/json,text/json,*/*"
        title="Upload JSON file"
        onUploaded={handleSchemaFileUploaded}
      />

      {previewTablesOpen && (
        <TablesDataPreviewModal
          isOpen={previewTablesOpen}
          onClose={() => setPreviewTablesOpen(false)}
          tables={previewTablesList}
        />
      )}
    </div>
  );
};

export default RestApiImportEditor;
