import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { endpoints } from '../../../../../webapi/endpoints';
import type { RootState } from '../../../../../redux/store';
import { appFileService } from '../../../../../webapi/appfilesvc';
import FileUploader from '../../../../common/FileUploader';
import { FolderNavigation } from '../../../../folderNavigation';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';

// File preview type by extension (matches Angular's docHelper.getFilePreviewType)
const IMAGE_EXT = ['jpg', 'jpeg', 'png', 'gif', 'bmp'];
const DOC_EXT_FOR_GOOGLE_GVIEW = [
  'xls',
  'xlsx',
  'doc',
  'docx',
  'pdf',
  'txt',
  'ppt',
  'pptx',
  'ai',
  'psd',
  'tiff',
  'tif',
  'svg',
  'dwg',
  'eps',
  'ttf',
  'xps',
  'zip',
  'rar',
];
const VIDEO_EXT = ['mp4', 'ogg', 'webm'];
const AUDIO_EXT = ['mp3', 'wav'];

function getFilePreviewType(fileName: string): 'imagePreView' | 'googleGview' | 'videoPreView' | 'audioPreView' | null {
  if (!fileName) return null;
  const ext = fileName.split('.').pop()?.toLowerCase() ?? '';
  if (IMAGE_EXT.includes(ext)) return 'imagePreView';
  if (DOC_EXT_FOR_GOOGLE_GVIEW.includes(ext)) return 'googleGview';
  if (VIDEO_EXT.includes(ext)) return 'videoPreView';
  if (AUDIO_EXT.includes(ext)) return 'audioPreView';
  return null;
}

interface FileControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const FileControl: React.FC<FileControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange,
}) => {
  const { theme, t } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const rootRef = useRef<HTMLDivElement | null>(null);

  const fieldName = fieldDto.DataBaseFieldName;
  const fileId = getOneToOneFieldValue(dataModel.currentFormData, fieldDto, fieldName, undefined, layoutItemExDto);

  const uiId = controllerModel?.uiId || '';
  const fileUrl = useMemo(() => {
    if (!fileId) return null;
    const sessionId = userContext?.SessionId ?? (typeof window !== 'undefined' ? localStorage.getItem('sessionId') : null);
    const sid = sessionId ? `&CurrentUserSessionId=${encodeURIComponent(String(sessionId))}` : '';
    return endpoints.buildEndpointUrl(`/GetLatestFile.aspx?FileId=${fileId}${sid}`);
  }, [fileId, userContext?.SessionId]);

  const downloadUrl = useMemo(() => {
    if (!fileId) return null;
    const sessionId = userContext?.SessionId ?? (typeof window !== 'undefined' ? localStorage.getItem('sessionId') : null);
    const sid = sessionId ? `&CurrentUserSessionId=${encodeURIComponent(String(sessionId))}` : '';
    return endpoints.buildEndpointUrl(`/GetLatestFile.aspx?FileId=${fileId}${sid}&IsDownload=true`);
  }, [fileId, userContext?.SessionId]);

  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [isLibraryOpen, setIsLibraryOpen] = useState(false);
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [selectedFileName, setSelectedFileName] = useState<string>('');
  const [pendingLibraryFileId, setPendingLibraryFileId] = useState<number | null>(() => {
    if (typeof fileId === 'number') return fileId;
    if (typeof fileId === 'string' && fileId) {
      const parsed = Number(fileId);
      return Number.isFinite(parsed) ? parsed : null;
    }
    return null;
  });

  const isReadOnly = fieldDto.IsFormLayoutReadOnly === true || dataModel.currentFormData?.IsLockTransaction === true;
  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  const isRequired = fieldDto.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;
  const tooltip = fieldDto.ToolTip || fieldDto.LabelDisplayBinding || '';
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldName;

  // UI required validation message (set by FormMainMenus on Save)
  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null && fieldDto?.TransactionUnitId != null && String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;

  const fileStorageRootFolderId =
    dataModel?.currentFormStructure?.FileStorageRootFolderId ?? dataModel?.FileStorageRootFolderId;
  const sysFileTransactionId = userContext?.DictAppSetup?.SystemDefinedFileTransactionId ?? null;
  const defaultCategoryId_company = 3;

  const [menuState, setMenuState] = useState<{ open: boolean; x: number; y: number }>({ open: false, x: 0, y: 0 });

  const assignFileIdToModel = useCallback(
    async (newFileId: number | null, fileName?: string) => {
      const newValue = newFileId;
      const updatedFormData: any = buildFormDataWithOneToOneValue(
        dataModel.currentFormData,
        fieldDto,
        fieldName,
        newValue,
        layoutItemExDto
      );

      // Keep fileId->fileName mapping for displaying file columns.
      if (newFileId && fileName) {
        const dict = { ...(updatedFormData.DictDocumentIdFileCode ?? {}) };
        dict[String(newFileId)] = fileName;
        updatedFormData.DictDocumentIdFileCode = dict;
      }
      if (newFileId === null) {
        // If we clear the field, keep existing mapping (safe) but clear selected name.
        // Angular behavior keeps mapping for other fields; we do the same.
      }

      onDataModelChange({
        ...(dataModel ?? {}),
        uiValidationErrors:
          errorText && (dataModel as any)?.uiValidationErrors
            ? (() => {
                const copy = { ...((dataModel as any).uiValidationErrors ?? {}) };
                delete (copy as any)[errorKey];
                return copy;
              })()
            : (dataModel as any)?.uiValidationErrors,
        currentFormData: {
          ...updatedFormData,
          IsDirty: true,
        },
      });
    },
    [dataModel, errorKey, errorText, fieldDto, fieldName, layoutItemExDto, onDataModelChange]
  );

  const handleUploadUploaded = useCallback(
    async (result: any) => {
      const newId = result?.FileId != null ? Number(result.FileId) : null;
      if (!newId) return;

      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(newId));
        const fileCode = dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? '';
        if (fileCode) setSelectedFileName(String(fileCode));
        await assignFileIdToModel(newId, fileCode ? String(fileCode) : undefined);
      } catch {
        await assignFileIdToModel(newId, undefined);
      }

    },
    [assignFileIdToModel]
  );

  const handleLibraryFileSelected = useCallback(
    async (selectedId: string | number) => {
      const newId = selectedId != null ? Number(selectedId) : null;
      if (!newId) return;

      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(newId));
        const fileCode = dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? '';
        if (fileCode) setSelectedFileName(String(fileCode));
        await assignFileIdToModel(newId, fileCode ? String(fileCode) : undefined);
      } catch {
        await assignFileIdToModel(newId, undefined);
      }

      setIsLibraryOpen(false);
    },
    [assignFileIdToModel]
  );

  const handleDownload = useCallback(() => {
    if (!downloadUrl) return;
    window.open(downloadUrl, '_blank', 'noopener,noreferrer');
  }, [downloadUrl]);

  const handlePreview = useCallback(() => {
    if (!fileId) return;
    setIsPreviewOpen(true);
  }, [fileId]);

  const handleClear = useCallback(() => {
    void assignFileIdToModel(null);
    setSelectedFileName('');
    setMenuState((p) => ({ ...p, open: false }));
  }, [assignFileIdToModel]);

  const fileNameFromDict = useMemo(() => {
    const dict = dataModel.currentFormData?.DictDocumentIdFileCode;
    if (!dict || fileId == null) return '';
    const key = String(fileId);
    return dict[key] ?? '';
  }, [dataModel.currentFormData?.DictDocumentIdFileCode, fileId]);

  useEffect(() => {
    if (fileNameFromDict && !selectedFileName) setSelectedFileName(fileNameFromDict);
  }, [fileNameFromDict, selectedFileName]);

  const previewType = useMemo(() => getFilePreviewType(selectedFileName || fileNameFromDict), [selectedFileName, fileNameFromDict]);
  const hasFile = fileId != null && fileId !== '';

  // Close context menu when clicking outside
  useEffect(() => {
    if (!menuState.open) return;
    const onDocMouseDown = (e: MouseEvent) => {
      const target = e.target as Node | null;
      if (!target) return;
      if (rootRef.current && rootRef.current.contains(target)) return;
      setMenuState((p) => ({ ...p, open: false }));
    };
    document.addEventListener('mousedown', onDocMouseDown);
    return () => document.removeEventListener('mousedown', onDocMouseDown);
  }, [menuState.open]);

  const closeAllOverlays = useCallback(() => {
    setMenuState((p) => ({ ...p, open: false }));
  }, []);

  const openMenuAtEvent = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    setMenuState({ open: true, x: e.clientX, y: e.clientY });
  }, []);

  return (
    <div
      className={`w-full flex items-start gap-2`}
      title={tooltip}
      ref={rootRef}
    >
      {!isHideLabel && (
        <div className="flex-shrink-0 min-w-[120px]">
          <label className={`text-xs font-semibold ${theme.title}`}>
            {label} {requiredMark}
          </label>
        </div>
      )}

      {/* File code display (single-line like input) */}
      <div className="w-1 flex-auto">
        <div
          className={`w-full h-[30px] px-2 py-1 text-sm flex items-center ${
            isReadOnly ? `cursor-not-allowed` : ''
          } ${errorText ? 'border border-red-500 rounded' : ''}`}
          title={selectedFileName || fileNameFromDict || ''}
          role={hasFile ? 'button' : undefined}
          tabIndex={hasFile ? 0 : -1}
          onClick={() => {
            if (hasFile) handleDownload();
          }}
          onKeyDown={(e) => {
            if (!hasFile) return;
            if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              handleDownload();
            }
          }}
        >
          {hasFile ? (
            <span className={`truncate block flex-auto ${t('text_mainContentSection')}`}>
              {selectedFileName || fileNameFromDict || String(fileId)}
            </span>
          ) : (
            <span className={`truncate block flex-auto ${theme.label}`}></span>
          )}
        </div>
        {errorText && <div className="text-xs text-red-600 mt-0.5">{errorText}</div>}
      </div>

      {/* Right-side 3 buttons (like ImageControl) */}
      {!isReadOnly && (
        <div className="flex gap-1 flex-shrink-0">
          <button
            type="button"
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs relative`}
            title="Select File"
            onClick={() => {
              closeAllOverlays();
              setIsLibraryOpen(true);
            }}
          >
            <i className="fa-solid fa-folder-open" aria-hidden="true" />
            <i className="fa-solid fa-magnifying-glass absolute text-[8px] right-[6px] top-[2px]" aria-hidden="true" />
          </button>
          <button
            type="button"
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
            title="Upload File"
            onClick={() => {
              closeAllOverlays();
              setIsUploadOpen(true);
            }}
          >
            <i className="fa-solid fa-upload" aria-hidden="true" />
          </button>
          <button
            type="button"
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
            title="Options"
            onClick={openMenuAtEvent}
          >
            <i className="fa-solid fa-bars" aria-hidden="true" />
          </button>
        </div>
      )}

      {/* Context menu (Angular: fileContextMenu_) */}
      {menuState.open && (
        <div
          className={`${theme.mainContentSection} border rounded shadow-lg z-[10005] fixed`}
          style={{ left: menuState.x, top: menuState.y, minWidth: 190 }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.label}`}
            onClick={() => {
              setIsLibraryOpen(true);
              closeAllOverlays();
            }}
          >
            Select From Library
          </button>
          <button
            type="button"
            className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.label}`}
            onClick={() => {
              setIsUploadOpen(true);
              closeAllOverlays();
            }}
          >
            Upload
          </button>
          {hasFile ? <div className="border-t opacity-40" /> : null}
          {hasFile ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.label}`}
              onClick={() => {
                handleDownload();
                closeAllOverlays();
              }}
            >
              Download
            </button>
          ) : null}
          {hasFile ? <div className="border-t opacity-40" /> : null}
          {hasFile ? (
            <button
              type="button"
              className="w-full text-left px-3 py-2 text-xs text-red-600 hover:opacity-90"
              onClick={handleClear}
            >
              Clear
            </button>
          ) : null}
        </div>
      )}

      {/* Upload popup */}
      {!isReadOnly && (
        <FileUploader
          isOpen={isUploadOpen}
          onClose={() => setIsUploadOpen(false)}
          mode="appFile"
          appFileCallingFrom="File"
          targetFolderId={fileStorageRootFolderId != null ? Number(fileStorageRootFolderId) : undefined}
          accept="*/*"
          queueLimit={1}
          title="Upload File"
          onUploaded={handleUploadUploaded}
        />
      )}

      {/* Library selector popup */}
      {isLibraryOpen && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]" onClick={() => setIsLibraryOpen(false)}>
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
            style={{ width: '950px', height: '600px', maxWidth: '95vw', maxHeight: '90vh' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>File Selector</div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-50 disabled:cursor-not-allowed`}
                  onClick={async () => {
                    if (pendingLibraryFileId == null) return;
                    await handleLibraryFileSelected(pendingLibraryFileId);
                  }}
                  disabled={!pendingLibraryFileId}
                >
                  Select &amp; Close
                </button>
                <button type="button" className={`p-1 ${theme.button_default} rounded-[4px] text-xs`} onClick={() => setIsLibraryOpen(false)} title="Close">
                  <i className="fa-solid fa-xmark" aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden">
              {sysFileTransactionId ? (
                <FolderNavigation
                  transactionId={sysFileTransactionId}
                  defaultCategoryId={defaultCategoryId_company}
                  isFileMgt={true}
                  isUseAsSelector={true}
                  onFileSelected={(id) => {
                    if (id == null) setPendingLibraryFileId(null);
                    else setPendingLibraryFileId(Number(id));
                  }}
                />
              ) : (
                <div className="p-3">
                  <div className={`text-xs ${theme.label}`}>File selector is not configured. `SystemDefinedFileTransactionId` is missing.</div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Preview popup */}
      {isPreviewOpen && fileId && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]" onClick={() => setIsPreviewOpen(false)}>
          <div
            className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
            style={{ width: '800px', height: '600px', maxWidth: '95vw', maxHeight: '90vh' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Preview</div>
              <div className="flex items-center gap-2">
                {downloadUrl && (
                  <a className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} href={downloadUrl} target="_blank" rel="noopener noreferrer">
                    <i className="fa-solid fa-download mr-1" aria-hidden="true" />
                    Download
                  </a>
                )}
                <button type="button" className={`p-1 ${theme.button_default} rounded-[4px] text-xs`} onClick={() => setIsPreviewOpen(false)} title="Close">
                  <i className="fa-solid fa-xmark" aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden flex items-center justify-center p-3">
              {previewType === 'imagePreView' && fileUrl ? (
                <img src={fileUrl} alt={selectedFileName || fileNameFromDict} className="max-w-full max-h-full object-contain" />
              ) : previewType === 'googleGview' && fileUrl ? (
                <iframe
                  title="Document preview"
                  className="w-full h-full min-h-[300px] border-0"
                  src={`https://docs.google.com/gview?embedded=true&url=${encodeURIComponent(fileUrl)}`}
                />
              ) : previewType === 'videoPreView' && fileUrl ? (
                <video src={fileUrl} controls className="w-full max-h-full" />
              ) : previewType === 'audioPreView' && fileUrl ? (
                <audio src={fileUrl} controls className="w-full" />
              ) : (
                <div className={`text-xs ${theme.label}`}>Preview Not Available</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default FileControl;

