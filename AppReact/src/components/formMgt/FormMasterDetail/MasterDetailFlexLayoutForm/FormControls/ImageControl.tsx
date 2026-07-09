import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../../../../redux/hooks/useTheme';
import { fileImageUrl, downloadFileById } from '../../../../../webapi/fileEndpoints';
import { getOneToOneFieldValue, buildFormDataWithOneToOneValue } from './formDataBindingHelper';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../../../../hooks/useClampedContextMenuPosition';
import FileUploader from '../../../../common/FileUploader';
import { FolderNavigation } from '../../../../folderNavigation';
import type { RootState } from '../../../../../redux/store';
import { appFileService } from '../../../../../webapi/appfilesvc';

interface ImageControlProps {
  layoutItemExDto: any;
  fieldDto: any;
  controllerModel: any;
  dataModel: any;
  onDataModelChange: (dataModel: any) => void;
  transactionExDto?: any;
}

const IMAGE_CONTEXT_MENU_ESTIMATED_WIDTH = 190;
const IMAGE_CONTEXT_MENU_ESTIMATED_HEIGHT = 180;

const ImageControl: React.FC<ImageControlProps> = ({
  layoutItemExDto,
  fieldDto,
  controllerModel,
  dataModel,
  onDataModelChange
}) => {
  const { theme, t } = useTheme();
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const rootRef = useRef<HTMLDivElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const [isUploadOpen, setIsUploadOpen] = useState(false);
  const [isLibraryOpen, setIsLibraryOpen] = useState(false);
  const [isPreviewOpen, setIsPreviewOpen] = useState(false);
  const [menuState, setMenuState] = useState<{ open: boolean; x: number; y: number }>({ open: false, x: 0, y: 0 });
  const [selectedFileName, setSelectedFileName] = useState<string>('');

  const fieldName = fieldDto.DataBaseFieldName;
  const fileId = getOneToOneFieldValue(dataModel.currentFormData, fieldDto, fieldName, undefined, layoutItemExDto);
  // getOneToOneFieldValue can return true (boolean); narrow to an id usable by the file endpoints.
  const fileIdValue: string | number | null =
    typeof fileId === 'number' || typeof fileId === 'string' ? fileId : null;
  const [pendingLibraryFileId, setPendingLibraryFileId] = useState<number | null>(() => {
    if (typeof fileId === 'number') return fileId;
    if (typeof fileId === 'string' && fileId) {
      const parsed = Number(fileId);
      return Number.isFinite(parsed) ? parsed : null;
    }
    return null;
  });
  
  // Check if field is read-only
  const isReadOnly = fieldDto.IsFormLayoutReadOnly === true || 
                    dataModel.currentFormData?.IsLockTransaction === true;
  
  // Get required mark (UI only)
  const isRequired = fieldDto.IsAllowEmpty === false;
  const requiredMark = isRequired ? <span className="text-red-500">*</span> : null;
  
  // Get tooltip
  const tooltip = fieldDto.ToolTip || fieldDto.LabelDisplayBinding || '';
  
  // Get label
  const label = fieldDto.DisplayName || fieldDto.LabelDisplayBinding || fieldName;

  // UI required validation message (set by FormMainMenus on Save)
  const rootUnitId = dataModel.currentFormData?.RootUnitId ?? null;
  const errorKey =
    rootUnitId != null && fieldDto?.TransactionUnitId != null && String(fieldDto.TransactionUnitId) !== String(rootUnitId)
      ? `${String(fieldDto.TransactionUnitId)}.${String(fieldName)}`
      : String(fieldName);
  const errorText = dataModel?.uiValidationErrors?.[errorKey] as string | undefined;

  const imageUrl = fileIdValue != null ? fileImageUrl(fileIdValue) : null;

  // Parse style string to object
  const styleObject: React.CSSProperties = {};
  if (layoutItemExDto.StyleLayoutInfo) {
    const styles = layoutItemExDto.StyleLayoutInfo.split(';').filter((s: string) => s.trim());
    styles.forEach((style: string) => {
      const [key, value] = style.split(':').map((s: string) => s.trim());
      if (key && value) {
        const camelKey = key.replace(/-([a-z])/g, (g: string) => g[1].toUpperCase());
        (styleObject as any)[camelKey] = value;
      }
    });
  }

  const isHideLabel = controllerModel?.isFilePropertyEdit === true;

  const fileStorageRootFolderId = dataModel?.currentFormStructure?.FileStorageRootFolderId ?? dataModel?.FileStorageRootFolderId;
  const sysFileTransactionId = userContext?.DictAppSetup?.SystemDefinedFileTransactionId ?? null;
  const defaultCategoryId_company = 3; // Angular openFileSelectorPopup param1: 3

  const closeAllOverlays = useCallback(() => {
    setMenuState((p) => ({ ...p, open: false }));
  }, []);

  useEffect(() => {
    const handleDocMouseDown = (e: MouseEvent) => {
      if (!menuState.open) return;
      const target = e.target as Node | null;
      if (!target) return;
      if (rootRef.current && rootRef.current.contains(target)) return;
      setMenuState((p) => ({ ...p, open: false }));
    };
    document.addEventListener('mousedown', handleDocMouseDown);
    return () => document.removeEventListener('mousedown', handleDocMouseDown);
  }, [menuState.open]);

  const assignFileIdToModel = useCallback(
    (newFileId: number | null, fileName?: string) => {
      const updatedFormData: any = buildFormDataWithOneToOneValue(
        dataModel.currentFormData,
        fieldDto,
        fieldName,
        newFileId,
        layoutItemExDto
      );

      if (newFileId && fileName) {
        updatedFormData.DictDocumentIdFileCode = {
          ...(updatedFormData.DictDocumentIdFileCode ?? dataModel.currentFormData?.DictDocumentIdFileCode ?? {}),
          [String(newFileId)]: fileName,
        };
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

  useRefineContextMenuField(menuState.open, menuRef, setMenuState);

  const openMenuAtEvent = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    const { x, y } = clampContextMenuPosition(
      e.clientX,
      e.clientY,
      IMAGE_CONTEXT_MENU_ESTIMATED_WIDTH,
      IMAGE_CONTEXT_MENU_ESTIMATED_HEIGHT
    );
    setMenuState({ open: true, x, y });
  }, []);

  const handleUploadUploaded = useCallback(
    async (result: any) => {
      const newId = result?.FileId != null ? Number(result.FileId) : null;
      if (!newId) return;
      assignFileIdToModel(newId);
      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(newId));
        const fileCode = dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? '';
        if (fileCode) {
          setSelectedFileName(String(fileCode));
          assignFileIdToModel(newId, String(fileCode));
        }
      } catch {
        // ignore - file name is optional for image display
      }
    },
    [assignFileIdToModel]
  );

  const handleLibraryFileSelected = useCallback(
    async (selectedId: string | number) => {
      const newId = selectedId != null ? Number(selectedId) : null;
      if (!newId) return;
      assignFileIdToModel(newId);
      setIsLibraryOpen(false);
      try {
        const dto = await appFileService.retrieveOneOrgAppFileExDto(String(newId));
        const fileCode = dto?.FileCode ?? dto?.Display ?? dto?.FileName ?? '';
        if (fileCode) {
          setSelectedFileName(String(fileCode));
          assignFileIdToModel(newId, String(fileCode));
        }
      } catch {
        // ignore
      }
    },
    [assignFileIdToModel]
  );

  const handleConfirmLibrarySelection = useCallback(async () => {
    if (!pendingLibraryFileId) return;
    await handleLibraryFileSelected(pendingLibraryFileId);
  }, [pendingLibraryFileId, handleLibraryFileSelected]);

  const handleDownload = useCallback(() => {
    if (fileIdValue == null) return;
    void downloadFileById(fileIdValue, selectedFileName || undefined);
    closeAllOverlays();
  }, [fileIdValue, selectedFileName, closeAllOverlays]);

  const handlePreview = useCallback(() => {
    if (!fileId) return;
    setIsPreviewOpen(true);
    closeAllOverlays();
  }, [fileId, closeAllOverlays]);

  const handleClear = useCallback(() => {
    assignFileIdToModel(null);
    setSelectedFileName('');
    closeAllOverlays();
  }, [assignFileIdToModel, closeAllOverlays]);

  return (
    <div 
      className="w-full h-full min-h-0 flex flex-col gap-1"
      style={styleObject}
      title={tooltip}
      ref={rootRef}
    >
      {!isHideLabel && (
      <div className="flex items-center justify-between">
        <label className={`text-xs font-semibold ${theme.title}`}>
          {label} {requiredMark}
        </label>
        {!isReadOnly && (
          <div className="flex gap-1">
            <button
              type="button"
              className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
              title="Upload Image"
              onClick={() => {
                closeAllOverlays();
                setIsUploadOpen(true);
              }}
            >
              <i className="fa-solid fa-upload" aria-hidden="true" />
            </button>
            <button
              type="button"
              className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs relative`}
              title="Select Image"
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
              title="Options"
              onClick={openMenuAtEvent}
            >
              <i className="fa-solid fa-bars" aria-hidden="true" />
            </button>
          </div>
        )}
      </div>
      )}
      <div
        className={`w-full h-1 flex-auto min-h-0 overflow-hidden border rounded p-2 flex items-center justify-center ${theme.mainContentSection} ${t('border_mainContentSection')} ${errorText ? 'border-red-500' : ''}`}
        style={{ minHeight: '150px' }}
      >
        {imageUrl ? (
          <img
            key={String(fileIdValue)}
            src={imageUrl}
            alt={label}
            className="max-w-full max-h-full w-auto h-auto object-contain cursor-pointer"
            onClick={() => {
              if (fileId) setIsPreviewOpen(true);
            }}
            onError={(e) => {
              // Hide broken image
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
        ) : (
          <div className={`flex items-center justify-center h-full min-h-[8rem] text-sm ${t('text_default')}`}>
            No image
          </div>
        )}
      </div>
      {errorText && <div className="text-xs text-red-600 mt-0.5">{errorText}</div>}

      {/* Context menu (Angular: imageContextMenu_) */}
      {menuState.open && (
        <div
          ref={menuRef}
          className={`${theme.mainContentSection} border rounded shadow-lg z-[10005] fixed`}
          style={{ left: menuState.x, top: menuState.y, minWidth: IMAGE_CONTEXT_MENU_ESTIMATED_WIDTH }}
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
          {fileId ? <div className="border-t opacity-40" /> : null}
          {fileId ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.label}`}
              onClick={handleDownload}
            >
              Download
            </button>
          ) : null}
          {fileId ? (
            <button
              type="button"
              className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.label}`}
              onClick={handlePreview}
            >
              Preview
            </button>
          ) : null}
          {fileId ? <div className="border-t opacity-40" /> : null}
          {fileId ? (
            <button
              type="button"
              className="w-full text-left px-3 py-2 text-xs text-red-600 dark:text-red-400 hover:opacity-90"
              onClick={handleClear}
            >
              Clear
            </button>
          ) : null}
        </div>
      )}

      {/* Upload popup (Angular: formImageUploaderPopup callingFrom=Image) */}
      {!isReadOnly && (
        <FileUploader
          isOpen={isUploadOpen}
          onClose={() => setIsUploadOpen(false)}
          mode="appFile"
          appFileCallingFrom="Image"
          targetFolderId={fileStorageRootFolderId != null ? Number(fileStorageRootFolderId) : undefined}
          accept="image/jpeg,image/png,image/gif,image/bmp"
          queueLimit={1}
          title="Upload Image"
          onUploaded={handleUploadUploaded}
        />
      )}

      {/* Library selector popup (Angular: fileSelectorPopup + embedded folder navigation) */}
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
                  onClick={handleConfirmLibrarySelection}
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
                    if (id == null) {
                      setPendingLibraryFileId(null);
                    } else {
                      setPendingLibraryFileId(Number(id));
                    }
                  }}
                />
              ) : (
                <div className="p-3">
                  <div className={`text-xs ${theme.label}`}>
                    File selector is not configured. `SystemDefinedFileTransactionId` is missing.
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Preview popup (Angular: previewFileInPopupControl.openFilePreviewPopup) */}
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
                {fileId && (
                  <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleDownload}>
                    <i className="fa-solid fa-download mr-1" aria-hidden="true" />
                    Download
                  </button>
                )}
                <button type="button" className={`p-1 ${theme.button_default} rounded-[4px] text-xs`} onClick={() => setIsPreviewOpen(false)} title="Close">
                  <i className="fa-solid fa-xmark" aria-hidden="true" />
                </button>
              </div>
            </div>
            <div className="w-full h-1 flex-auto overflow-hidden flex items-center justify-center p-3">
              {imageUrl ? (
                <img
                  src={imageUrl}
                  alt={selectedFileName || label}
                  className="max-w-full max-h-full object-contain"
                  onError={(e) => {
                    (e.target as HTMLImageElement).style.display = 'none';
                  }}
                />
              ) : (
                <div className={`text-xs ${theme.label}`}>Preview not available.</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ImageControl;

