import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';
import { integrationService } from '../../../../webapi/integrationsvc';
import { JsonCodeEditor } from '../../../common/JsonCodeEditor';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import DbToDbImportEditor from '../../../dbmgt/DbToDbImportEditor';
import DbToDbImportManagement from '../../../dbmgt/DbToDbImportManagement';
import JsonFileTableImportManagement from '../../../dbmgt/JsonFileTableImportManagement';
import ExcelDataImportManagement from '../../../dbmgt/ExcelDataImportManagement';
import RestApiImportManagement from '../../../dbmgt/RestApiImportManagement';
import RestApiImportEditor from '../../../dbmgt/RestApiImportEditor';
import ExcelTableImportEditor from '../../../dbmgt/ExcelTableImportEditor';
import { FilePathAndFtpSection } from './fileOps/FilePathAndFtpSection';

const IMPORT_SELECTOR_POPUP_TITLE =
  'Please select an import setting, and then click the "Confirm Selection & Close" button.';

export const EmAppTransactionCommandTypeImportToDatabaseTableFromJson = 66;
export const EmAppTransactionCommandTypeImportToDatabaseTableFromExcel = 67;
export const EmAppTransactionCommandTypeImportToDatabaseTableFromRestApiImportSetting = 71;
export const EmAppTransactionCommandTypeImportToDatabaseTableFromDbToDbImportSetting = 72;
export const EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleJsonFiles = 76;
export const EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleExcelFiles = 77;
export const EmAppTransactionCommandTypeDownloadFileToServerFolder = 73;
export const EmAppTransactionCommandTypeConvertFromXmlToJson = 81;
export const EmAppTransactionCommandTypeConvertBackFromJsonToXml = 82;
export const EmAppTransactionCommandTypeExecuteExternalExeProcess = 68;

type PickerItem = { Id: number; Display: string };

/** Angular: TransactionCommandSingleEditorPopup.cshtml — ConvertFromXmlToJson source FilePath */
const CONVERT_XML_TO_JSON_SOURCE_EXAMPLES = [
  'Example 1: http://www.sitename.com/filename.xml',
  'Example 2: ftp://ftp.sitename.com/filename.xml',
  'Example 3: c:\\temp\\filename.xml',
];

/** Angular: ConvertBackFromJsonToXml source FilePath */
const CONVERT_JSON_TO_XML_SOURCE_EXAMPLES = [
  'Example 1: http://www.sitename.com/filename.json',
  'Example 2: ftp://ftp.sitename.com/filename.json',
  'Example 3: c:\\temp\\filename.json',
];

/** Angular: DownloadFileToServerFolder source FilePath */
const DOWNLOAD_FILE_SOURCE_EXAMPLES = [
  'Example 1: http://www.sitename.com/filename.txt',
  'Example 2: ftp://ftp.sitename.com/filename.json',
  'Example 3: c:\\temp\\filename.txt',
];

/** Angular: DownloadFileToServerFolder destination DistinationFilePath */
const DOWNLOAD_FILE_DEST_EXAMPLES = [
  'Example 1: c:\\ServerFileFolder\\',
  'Example 2: c:\\ServerFileFolder\\ModifiedFileName.txt',
];

/** Angular: ImportToDatabaseTableFromJson/Excel FilePath */
const IMPORT_JSON_FILE_PATH_EXAMPLES = [
  'Example 1: c:\\MyJsonFileFolder\\',
  'Example 2: http://www.sitename.com/filename.json',
  'Example 3: ftp://ftp.sitename.com/filename.json',
];
const IMPORT_EXCEL_FILE_PATH_EXAMPLES = [
  'Example 1: c:\\MyExcelFileFolder\\',
  'Example 2: http://www.sitename.com/filename.xlsx',
  'Example 3: ftp://ftp.sitename.com/filename.xlsx',
];

/** Angular: ImportToDatabaseTablesFromMultipleJsonFiles/ExcelFiles FilePath */
const IMPORT_MULTI_JSON_ROOT_EXAMPLES = ['Example 1: c:\\MyJsonFileFolder\\'];
const IMPORT_MULTI_EXCEL_ROOT_EXAMPLES = ['Example 1: c:\\MyExcelFileFolder\\'];

/** Angular: ExecuteExternalExeProcess FilePath */
const EXE_FILE_PATH_EXAMPLES = ['Example: c:\\FolderPath\\filename.exe'];

function normalizePickerList(list: any[], displayFields: string[]): PickerItem[] {
  const arr = Array.isArray(list) ? list : [];
  return arr.map((x: any) => {
    const id = Number(x?.Id ?? x?.id ?? 0);
    let display = '';
    for (const f of displayFields) {
      if (x?.[f]) {
        display = String(x[f]);
        break;
      }
    }
    if (!display) display = String(id);
    return { Id: id, Display: display };
  });
}

function SimplePicker(props: {
  label: string;
  valueId: number | null;
  displayText: string;
  items: PickerItem[];
  onPick: (id: number) => void;
  onEdit?: () => void;
  /** Angular parity: chevron opens full selector popup (with New Import) instead of inline list. */
  onOpenSelector?: () => void;
}) {
  const { theme } = useTheme();
  const { label, valueId, displayText, items, onPick, onEdit, onOpenSelector } = props;

  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
      <label className={`text-xs ${theme.label}`}>{label}</label>
      <div className={`w-72 h-7 flex border rounded-[4px] overflow-hidden ${theme.inputBox}`}>
        <button
          type="button"
          className="w-1 flex-auto min-w-0 h-full px-2 text-xs flex items-center truncate text-left"
          title={displayText}
          onClick={() => {
            if (valueId != null && onEdit) onEdit();
          }}
          disabled={!(valueId != null && onEdit)}
        >
          {valueId != null ? <i className="fa-solid fa-pencil mr-1 opacity-70" aria-hidden /> : null}
          <span className={valueId != null && onEdit ? 'underline' : ''}>
            {displayText || (valueId != null ? String(valueId) : '')}
          </span>
        </button>
        <button
          ref={triggerRef}
          type="button"
          title="Select"
          className={`h-full w-7 shrink-0 border-l flex items-center justify-center ${theme.inputBox}`}
          onClick={() => {
            if (onOpenSelector) {
              onOpenSelector();
              return;
            }
            if (!open) {
              const rect = triggerRef.current?.getBoundingClientRect();
              if (rect) setPos({ top: rect.bottom + 4, left: rect.left });
            } else {
              setPos(null);
            }
            setOpen((v) => !v);
          }}
        >
          <i className="fa-solid fa-chevron-down text-xs" aria-hidden />
        </button>
      </div>

      {open && pos
        ? createPortal(
            <div
              className={`fixed z-[9999] min-w-[260px] max-h-[420px] overflow-y-auto border rounded shadow-lg py-1 ${theme.mainContentSection}`}
              style={{ top: pos.top, left: pos.left }}
            >
              {items.map((it) => (
                <button
                  key={it.Id}
                  type="button"
                  className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                  onClick={() => {
                    onPick(it.Id);
                    setOpen(false);
                    setPos(null);
                  }}
                >
                  {it.Display}
                </button>
              ))}
              {items.length === 0 ? <div className="px-3 py-2 text-xs opacity-70">No items</div> : null}
            </div>,
            document.body,
          )
        : null}
    </div>
  );
}

export function OperationFileAndImportSection(props: {
  action: any;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError, showWarning } = useErrorMessage();
  const { action, onMarkChange } = props;

  const actionType = Number(action?.ActionType);

  const [jsonImportSettings, setJsonImportSettings] = useState<PickerItem[]>([]);
  const [excelImportSettings, setExcelImportSettings] = useState<PickerItem[]>([]);
  const [dbToDbImportSettings, setDbToDbImportSettings] = useState<PickerItem[]>([]);
  const [restApiImportSettings, setRestApiImportSettings] = useState<PickerItem[]>([]);
  const [jsonSelectorOpen, setJsonSelectorOpen] = useState(false);
  const [excelSelectorOpen, setExcelSelectorOpen] = useState(false);
  const [dbToDbSelectorOpen, setDbToDbSelectorOpen] = useState(false);
  const [restApiSelectorOpen, setRestApiSelectorOpen] = useState(false);
  const [jsonEditorPopupId, setJsonEditorPopupId] = useState<number | null>(null);
  const [excelEditorPopupId, setExcelEditorPopupId] = useState<number | null>(null);
  const [dbToDbEditorPopupId, setDbToDbEditorPopupId] = useState<number | null>(null);
  const [restApiEditorPopupId, setRestApiEditorPopupId] = useState<number | null>(null);

  const EM_IMPORT_STATUS_RELEASED = 2;

  const showJsonImportSetting = actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromJson;
  const showExcelImportSetting = actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromExcel;
  const showDbToDbImportSetting = actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromDbToDbImportSetting;
  const showRestApiImportSetting = actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromRestApiImportSetting;

  const showSingleFilePath =
    actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromJson ||
    actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromExcel ||
    actionType === EmAppTransactionCommandTypeExecuteExternalExeProcess;

  const showMultiFileRoot =
    actionType === EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleJsonFiles ||
    actionType === EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleExcelFiles;

  const showDownload = actionType === EmAppTransactionCommandTypeDownloadFileToServerFolder;
  const showConvertXmlToJson = actionType === EmAppTransactionCommandTypeConvertFromXmlToJson;
  const showConvertJsonToXml = actionType === EmAppTransactionCommandTypeConvertBackFromJsonToXml;
  const showExeArguments = actionType === EmAppTransactionCommandTypeExecuteExternalExeProcess;

  const needsAnything =
    showJsonImportSetting ||
    showExcelImportSetting ||
    showDbToDbImportSetting ||
    showRestApiImportSetting ||
    showSingleFilePath ||
    showMultiFileRoot ||
    showDownload ||
    showConvertXmlToJson ||
    showConvertJsonToXml;

  useEffect(() => {
    if (!needsAnything) return;

    const loadLists = async () => {
      try {
        if (showJsonImportSetting) {
          const list = await integrationService.retrieveAllJsonFileTableImportSettingDtoList();
          setJsonImportSettings(normalizePickerList(list, ['ActionCode', 'Name', 'Display']));
        }
        if (showExcelImportSetting) {
          const list = await appTransactionService.retrieveAllExcelTableImportSettingDto(false);
          setExcelImportSettings(normalizePickerList(list, ['Name', 'Display', 'Description']));
        }
        if (showDbToDbImportSetting) {
          const list = await appTransactionService.retrieveAllDbToDbTableImportSettingDto();
          setDbToDbImportSettings(normalizePickerList(list, ['Name', 'Display', 'Description']));
        }
        if (showRestApiImportSetting) {
          const list = await integrationService.retrieveAllApiStagingTableImportSettingDtoList();
          setRestApiImportSettings(normalizePickerList(list, ['ActionCode', 'Name', 'Display']));
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to load import setting list');
      }
    };
    void loadLists();
  }, [needsAnything, showDbToDbImportSetting, showError, showExcelImportSetting, showJsonImportSetting, showRestApiImportSetting]);

  const reloadJsonImportSettings = useCallback(async () => {
    try {
      const list = await integrationService.retrieveAllJsonFileTableImportSettingDtoList();
      setJsonImportSettings(normalizePickerList(list, ['ActionCode', 'Name', 'Display']));
    } catch (e: any) {
      showError(e?.message || 'Failed to load JSON import settings');
    }
  }, [showError]);

  const reloadExcelImportSettings = useCallback(async () => {
    try {
      const list = await appTransactionService.retrieveAllExcelTableImportSettingDto(false);
      setExcelImportSettings(normalizePickerList(list, ['Name', 'Display', 'Description']));
    } catch (e: any) {
      showError(e?.message || 'Failed to load Excel import settings');
    }
  }, [showError]);

  const reloadDbToDbImportSettings = useCallback(async () => {
    try {
      const list = await appTransactionService.retrieveAllDbToDbTableImportSettingDto();
      setDbToDbImportSettings(normalizePickerList(list, ['Name', 'Display', 'Description']));
    } catch (e: any) {
      showError(e?.message || 'Failed to load DB to DB import settings');
    }
  }, [showError]);

  const reloadRestApiImportSettings = useCallback(async () => {
    try {
      const list = await integrationService.retrieveAllApiStagingTableImportSettingDtoList();
      setRestApiImportSettings(normalizePickerList(list, ['ActionCode', 'Name', 'Display']));
    } catch (e: any) {
      showError(e?.message || 'Failed to load REST API import settings');
    }
  }, [showError]);

  const closeJsonEditorPopup = useCallback(() => {
    setJsonEditorPopupId(null);
    void reloadJsonImportSettings();
  }, [reloadJsonImportSettings]);

  const closeExcelEditorPopup = useCallback(() => {
    setExcelEditorPopupId(null);
    void reloadExcelImportSettings();
  }, [reloadExcelImportSettings]);

  const closeDbToDbEditorPopup = useCallback(() => {
    setDbToDbEditorPopupId(null);
    void reloadDbToDbImportSettings();
  }, [reloadDbToDbImportSettings]);

  const closeRestApiEditorPopup = useCallback(() => {
    setRestApiEditorPopupId(null);
    void reloadRestApiImportSettings();
  }, [reloadRestApiImportSettings]);

  const isImportSettingReleased = useCallback((item: any): boolean => {
    const st = item?.OtherSettingsDto?.TableImportSettingDto?.Status ?? item?.ImportStatus;
    if (st === 'Released') return true;
    if (st != null && Number(st) === EM_IMPORT_STATUS_RELEASED) return true;
    return false;
  }, []);

  const pickJsonSetting = useCallback(
    (id: number) => {
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.JsonImportSettingId = id;
      onMarkChange();
    },
    [action, onMarkChange],
  );

  const pickExcelSetting = useCallback(
    (id: number) => {
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.ExcelImportSettingId = id;
      onMarkChange();
    },
    [action, onMarkChange],
  );

  const pickDbToDbSetting = useCallback(
    (id: number) => {
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.DbToDbImportSettingId = id;
      onMarkChange();
    },
    [action, onMarkChange],
  );

  const handleJsonImportSelected = useCallback(
    (selectedItem: any) => {
      const id = selectedItem?.Id != null ? Number(selectedItem.Id) : null;
      if (!id) return;
      pickJsonSetting(id);
      void reloadJsonImportSettings();
    },
    [pickJsonSetting, reloadJsonImportSettings],
  );

  const handleExcelImportSelected = useCallback(
    (selectedItem: any) => {
      const id = selectedItem?.Id != null ? Number(selectedItem.Id) : null;
      if (!id) return;
      pickExcelSetting(id);
      if (!isImportSettingReleased(selectedItem)) {
        showWarning(
          'Warning, this import setting has not been released yet. You may select it for now. But the command will not execute the import process until it is released.',
        );
      }
      void reloadExcelImportSettings();
    },
    [pickExcelSetting, reloadExcelImportSettings, isImportSettingReleased, showWarning],
  );

  const handleDbToDbImportSelected = useCallback(
    (selectedItem: any) => {
      const id = selectedItem?.Id != null ? Number(selectedItem.Id) : null;
      if (!id) return;
      pickDbToDbSetting(id);
      if (!isImportSettingReleased(selectedItem)) {
        showWarning(
          'Warning, this import setting has not been released yet. The Simulate import mode will be executed.',
        );
      }
      void reloadDbToDbImportSettings();
    },
    [pickDbToDbSetting, reloadDbToDbImportSettings, isImportSettingReleased, showWarning],
  );

  const pickRestApiSetting = useCallback(
    (id: number) => {
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.RestApiDbImportSettingId = id;
      onMarkChange();
    },
    [action, onMarkChange],
  );

  const handleRestApiImportSelected = useCallback(
    (selectedItem: any) => {
      const id = selectedItem?.Id != null ? Number(selectedItem.Id) : null;
      if (!id) return;
      pickRestApiSetting(id);
      void reloadRestApiImportSettings();
    },
    [pickRestApiSetting, reloadRestApiImportSettings],
  );

  const jsonSettingId = action?.ActionAttribute?.JsonImportSettingId != null ? Number(action.ActionAttribute.JsonImportSettingId) : null;
  const excelSettingId = action?.ActionAttribute?.ExcelImportSettingId != null ? Number(action.ActionAttribute.ExcelImportSettingId) : null;
  const dbToDbSettingId = action?.ActionAttribute?.DbToDbImportSettingId != null ? Number(action.ActionAttribute.DbToDbImportSettingId) : null;
  const restApiSettingId = action?.ActionAttribute?.RestApiDbImportSettingId != null ? Number(action.ActionAttribute.RestApiDbImportSettingId) : null;

  const jsonSettingDisplay = useMemo(() => jsonImportSettings.find((x) => x.Id === jsonSettingId)?.Display ?? '', [jsonImportSettings, jsonSettingId]);
  const excelSettingDisplay = useMemo(() => excelImportSettings.find((x) => x.Id === excelSettingId)?.Display ?? '', [excelImportSettings, excelSettingId]);
  const dbToDbSettingDisplay = useMemo(() => dbToDbImportSettings.find((x) => x.Id === dbToDbSettingId)?.Display ?? '', [dbToDbImportSettings, dbToDbSettingId]);
  const restApiSettingDisplay = useMemo(() => restApiImportSettings.find((x) => x.Id === restApiSettingId)?.Display ?? '', [restApiImportSettings, restApiSettingId]);

  if (!action || !needsAnything) return null;

  return (
    <>
      {showJsonImportSetting ? (
        <SimplePicker
          label="JSON Import Setting"
          valueId={jsonSettingId}
          displayText={jsonSettingDisplay}
          items={jsonImportSettings}
          onPick={pickJsonSetting}
          onOpenSelector={() => setJsonSelectorOpen(true)}
          onEdit={() => {
            if (jsonSettingId == null) return;
            setJsonEditorPopupId(jsonSettingId);
          }}
        />
      ) : null}

      {showExcelImportSetting ? (
        <SimplePicker
          label="Excel Import Setting"
          valueId={excelSettingId}
          displayText={excelSettingDisplay}
          items={excelImportSettings}
          onPick={pickExcelSetting}
          onOpenSelector={() => setExcelSelectorOpen(true)}
          onEdit={() => {
            if (excelSettingId == null) return;
            setExcelEditorPopupId(excelSettingId);
          }}
        />
      ) : null}

      {showDbToDbImportSetting ? (
        <SimplePicker
          label="Db to Db Import Setting"
          valueId={dbToDbSettingId}
          displayText={dbToDbSettingDisplay}
          items={dbToDbImportSettings}
          onPick={pickDbToDbSetting}
          onOpenSelector={() => setDbToDbSelectorOpen(true)}
          onEdit={() => {
            if (dbToDbSettingId == null) return;
            setDbToDbEditorPopupId(dbToDbSettingId);
          }}
        />
      ) : null}

      {showRestApiImportSetting ? (
        <SimplePicker
          label="Rest Api Table Import Setting"
          valueId={restApiSettingId}
          displayText={restApiSettingDisplay}
          items={restApiImportSettings}
          onPick={pickRestApiSetting}
          onOpenSelector={() => setRestApiSelectorOpen(true)}
          onEdit={() => {
            if (restApiSettingId == null) return;
            setRestApiEditorPopupId(restApiSettingId);
          }}
        />
      ) : null}

      {showSingleFilePath ? (
        <FilePathAndFtpSection
          label={
            actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromJson
              ? 'JSON Files Server Folder Location Or File Url'
              : actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromExcel
                ? 'Excel Files Server Folder Location Or File Url'
                : 'EXE File Server Path'
          }
          action={action}
          filePathProp="FilePath"
          onMarkChange={onMarkChange}
          helpLines={
            actionType === EmAppTransactionCommandTypeExecuteExternalExeProcess
              ? EXE_FILE_PATH_EXAMPLES
              : actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromJson
                ? IMPORT_JSON_FILE_PATH_EXAMPLES
                : actionType === EmAppTransactionCommandTypeImportToDatabaseTableFromExcel
                  ? IMPORT_EXCEL_FILE_PATH_EXAMPLES
                  : undefined
          }
        />
      ) : null}

      {showMultiFileRoot ? (
        <FilePathAndFtpSection
          label={
            actionType === EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleJsonFiles
              ? 'Web Server JSON Files Root Folder'
              : 'Web Server JSON Files Root Folder'
          }
          action={action}
          filePathProp="FilePath"
          onMarkChange={onMarkChange}
          helpLines={
            actionType === EmAppTransactionCommandTypeImportToDatabaseTablesFromMultipleJsonFiles
              ? IMPORT_MULTI_JSON_ROOT_EXAMPLES
              : IMPORT_MULTI_EXCEL_ROOT_EXAMPLES
          }
        />
      ) : null}

      {showDownload ? (
        <>
          <FilePathAndFtpSection
            label="Download File Url, Ftp Path, or Web Server File Path"
            action={action}
            filePathProp="FilePath"
            onMarkChange={onMarkChange}
            helpLines={DOWNLOAD_FILE_SOURCE_EXAMPLES}
          />
          <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Download To Web Server Folder or File Path</label>
            <div className="flex flex-col gap-1">
              <input
                type="text"
                className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                value={action?.ActionAttribute?.DistinationFilePath ?? ''}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.DistinationFilePath = e.target.value;
                  onMarkChange();
                }}
              />
              <div className={`text-[11px] ${theme.label} opacity-80 leading-4 pb-2`}>
                {DOWNLOAD_FILE_DEST_EXAMPLES.map((l) => (
                  <div key={l}>{l}</div>
                ))}
              </div>
            </div>
          </div>
        </>
      ) : null}

      {showConvertXmlToJson ? (
        <>
          <FilePathAndFtpSection
            label="Source XML File Url, Ftp Path, or Web Server File Path"
            action={action}
            filePathProp="FilePath"
            onMarkChange={onMarkChange}
            helpLines={CONVERT_XML_TO_JSON_SOURCE_EXAMPLES}
          />
          <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Convert To JSON File Path</label>
            <div className="flex flex-col gap-1">
              <input
                type="text"
                className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                value={action?.ActionAttribute?.DistinationFilePath ?? ''}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.DistinationFilePath = e.target.value;
                  onMarkChange();
                }}
              />
              <div className={`text-[11px] ${theme.label} opacity-80 leading-4 pb-2`}>
                <div>Example: c:\ServerFileFolder\FileName.json</div>
              </div>
            </div>
          </div>
        </>
      ) : null}

      {showConvertJsonToXml ? (
        <>
          <FilePathAndFtpSection
            label="Source JSON File Url, Ftp Path, or Web Server File Path"
            action={action}
            filePathProp="FilePath"
            onMarkChange={onMarkChange}
            helpLines={CONVERT_JSON_TO_XML_SOURCE_EXAMPLES}
          />
          <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Convert To XML File Path</label>
            <div className="flex flex-col gap-1">
              <input
                type="text"
                className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                value={action?.ActionAttribute?.DistinationFilePath ?? ''}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.DistinationFilePath = e.target.value;
                  onMarkChange();
                }}
              />
              <div className={`text-[11px] ${theme.label} opacity-80 leading-4 pb-2`}>
                <div>Example: c:\ServerFileFolder\FileName.xml</div>
              </div>
            </div>
          </div>
        </>
      ) : null}

      {showExeArguments ? (
        <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>Arguments</label>
          <div className="w-full h-[300px]">
            <div className={`w-full h-full border rounded overflow-hidden ${theme.inputBox}`}>
              <JsonCodeEditor
                value={String(action?.ActionAttribute?.Arguments ?? '{\n\n}')}
                language="json"
                debounceMs={150}
                className="w-full h-full"
                onChange={(next) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.Arguments = next ?? '';
                  onMarkChange();
                }}
              />
            </div>
          </div>
        </div>
      ) : null}

      {jsonSelectorOpen ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10040}
          title={IMPORT_SELECTOR_POPUP_TITLE}
          frameInstanceKey="json-import-selector"
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setJsonSelectorOpen(false)}>
              Close
            </button>
          }
        >
          <JsonFileTableImportManagement
            isUsedAsSelector
            onConfirmSelection={(item) => {
              handleJsonImportSelected(item);
              setJsonSelectorOpen(false);
            }}
            onRequestClose={() => setJsonSelectorOpen(false)}
            onOpenImportEditor={(id) => {
              setJsonSelectorOpen(false);
              setJsonEditorPopupId(id);
            }}
          />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {excelSelectorOpen ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10040}
          title={IMPORT_SELECTOR_POPUP_TITLE}
          frameInstanceKey="excel-import-selector"
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setExcelSelectorOpen(false)}>
              Close
            </button>
          }
        >
          <ExcelDataImportManagement
            isUsedAsSelector
            onConfirmSelection={(item) => {
              handleExcelImportSelected(item);
              setExcelSelectorOpen(false);
            }}
            onRequestClose={() => setExcelSelectorOpen(false)}
            onOpenImportEditor={(id) => {
              setExcelSelectorOpen(false);
              setExcelEditorPopupId(id);
            }}
          />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {dbToDbSelectorOpen ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10040}
          title={IMPORT_SELECTOR_POPUP_TITLE}
          frameInstanceKey="db-to-db-import-selector"
          toolbarTrailing={
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={() => setDbToDbSelectorOpen(false)}
            >
              Close
            </button>
          }
        >
          <DbToDbImportManagement
            isUsedAsSelector
            onConfirmSelection={(item) => {
              handleDbToDbImportSelected(item);
              setDbToDbSelectorOpen(false);
            }}
            onRequestClose={() => setDbToDbSelectorOpen(false)}
            onOpenImportEditor={(id) => {
              setDbToDbSelectorOpen(false);
              setDbToDbEditorPopupId(id);
            }}
          />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {restApiSelectorOpen ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10040}
          title={IMPORT_SELECTOR_POPUP_TITLE}
          frameInstanceKey="rest-api-import-selector"
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setRestApiSelectorOpen(false)}>
              Close
            </button>
          }
        >
          <RestApiImportManagement
            isUsedAsSelector
            onConfirmSelection={(item) => {
              handleRestApiImportSelected(item);
              setRestApiSelectorOpen(false);
            }}
            onRequestClose={() => setRestApiSelectorOpen(false)}
            onOpenImportEditor={(id) => {
              setRestApiSelectorOpen(false);
              setRestApiEditorPopupId(id);
            }}
          />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {jsonEditorPopupId != null ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10050}
          title={`Edit Json Import Setting: ${jsonEditorPopupId}`}
          frameInstanceKey={`json-editor-${jsonEditorPopupId}`}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeJsonEditorPopup}>
              Close
            </button>
          }
        >
          <RestApiImportEditor ignoreRouteParam importSettingId={jsonEditorPopupId} jsonFileImport onClose={closeJsonEditorPopup} />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {excelEditorPopupId != null ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10050}
          title={`Edit Excel Import Setting: ${excelEditorPopupId}`}
          frameInstanceKey={`excel-editor-${excelEditorPopupId}`}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeExcelEditorPopup}>
              Close
            </button>
          }
        >
          <ExcelTableImportEditor ignoreRouteParam dataSetId={excelEditorPopupId} onClose={closeExcelEditorPopup} />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {dbToDbEditorPopupId != null ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10050}
          title={`Edit DB-To-DB Import Setting: ${dbToDbEditorPopupId}`}
          frameInstanceKey={dbToDbEditorPopupId}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeDbToDbEditorPopup}>
              Close
            </button>
          }
        >
          <DbToDbImportEditor ignoreRouteParam dataSetId={dbToDbEditorPopupId} onClose={closeDbToDbEditorPopup} />
        </EmbeddedLinkedPopupFrame>
      ) : null}

      {restApiEditorPopupId != null ? (
        <EmbeddedLinkedPopupFrame
          zIndex={10050}
          title={`Edit Rest Api Database Import Setting: ${restApiEditorPopupId}`}
          frameInstanceKey={`rest-api-editor-${restApiEditorPopupId}`}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeRestApiEditorPopup}>
              Close
            </button>
          }
        >
          <RestApiImportEditor ignoreRouteParam importSettingId={restApiEditorPopupId} onClose={closeRestApiEditorPopup} />
        </EmbeddedLinkedPopupFrame>
      ) : null}
    </>
  );
}

