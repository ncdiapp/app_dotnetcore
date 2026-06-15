/**
 * FileUploader component – uploads via backend DataImage.aspx (same as Angular).
 *
 * Two modes:
 * 1. appFile: upload to AppFile (file management), returns FileId.
 * 2. folder: upload to a designated web folder (e.g. Company Logo / Login Background), returns ResultMessage (fileName) so the app can display image by URL.
 */

import React, { useCallback, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import {
  uploadFileToDataImage,
  type DataImageCallingFrom,
  type DataImageUploadParams,
  type DataImageUploadResult,
} from '../../webapi/dataImageUploadSvc';

export type FileUploaderMode = 'appFile' | 'folder';

export type FolderCallingFrom = 'UploadCompanyLogoImage' | 'UploadCompanyBackgrundImage';

export interface FileUploaderProps {
  isOpen: boolean;
  onClose: () => void;
  /** 'appFile' = save to file management (FileId); 'folder' = save to web path (ResultMessage/fileName). */
  mode: FileUploaderMode;
  /** For mode='folder': which backend handler (Company Logo vs Login Page Background). */
  folderCallingFrom?: FolderCallingFrom;
  /** For mode='appFile': which backend handler (File vs Image vs UploadFileByTransactionCommand). */
  appFileCallingFrom?: DataImageCallingFrom;
  /** For mode='appFile': target folder id in file management. Omit for default. */
  targetFolderId?: number;
  /** Called after a successful upload with backend result (FileId or ResultMessage). */
  onUploaded: (result: DataImageUploadResult) => void;
  /** HTML accept attribute (e.g. "image/*" for images only). */
  accept?: string;
  /** Max files to select (default 1 for folder, 10 for appFile). */
  queueLimit?: number;
  title?: string;
}

const DEFAULT_ACCEPT_IMAGE = 'image/jpeg,image/png,image/gif,image/bmp';

const FileUploader: React.FC<FileUploaderProps> = ({
  isOpen,
  onClose,
  mode,
  folderCallingFrom = 'UploadCompanyLogoImage',
  appFileCallingFrom = 'File',
  targetFolderId,
  onUploaded,
  accept,
  queueLimit,
  title = 'Upload File',
}) => {
  const { theme } = useTheme();
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const effectiveAccept = accept ?? (mode === 'folder' ? DEFAULT_ACCEPT_IMAGE : undefined);
  const effectiveQueueLimit = queueLimit ?? (mode === 'folder' ? 1 : 10);

  const getUploadParams = useCallback((): DataImageUploadParams => {
    if (mode === 'folder') {
      return { callingFrom: folderCallingFrom as DataImageCallingFrom };
    }
    return {
      callingFrom: appFileCallingFrom,
      targetFolderId,
    };
  }, [mode, folderCallingFrom, appFileCallingFrom, targetFolderId]);

  const handleSelectFiles = useCallback(
    (files: FileList | null) => {
      if (!files?.length) return;
      const list = Array.from(files).slice(0, effectiveQueueLimit);
      setSelectedFiles((prev) => {
        const combined = [...prev, ...list];
        return combined.slice(0, effectiveQueueLimit);
      });
      setError(null);
    },
    [effectiveQueueLimit]
  );

  const handleInputChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      handleSelectFiles(e.target.files);
      e.target.value = '';
    },
    [handleSelectFiles]
  );

  const removeFile = useCallback((index: number) => {
    setSelectedFiles((prev) => prev.filter((_, i) => i !== index));
  }, []);

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      handleSelectFiles(e.dataTransfer.files);
    },
    [handleSelectFiles]
  );

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleUpload = useCallback(async () => {
    if (selectedFiles.length === 0) {
      setError('Select at least one file.');
      return;
    }
    setError(null);
    setUploading(true);
    setProgress(0);
    const params = getUploadParams();
    try {
      for (let i = 0; i < selectedFiles.length; i++) {
        setProgress(0);
        const result = await uploadFileToDataImage(selectedFiles[i], params, (p) => setProgress(p));
        onUploaded(result);
        if (i < selectedFiles.length - 1) {
          setProgress(100);
        }
      }
      setSelectedFiles([]);
      onClose();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setUploading(false);
      setProgress(0);
    }
  }, [selectedFiles, getUploadParams, onUploaded, onClose]);

  const handleClose = useCallback(() => {
    if (!uploading) {
      setSelectedFiles([]);
      setError(null);
      onClose();
    }
  }, [uploading, onClose]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]"
      onClick={(e) => e.stopPropagation()}
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '403px', maxWidth: '95vw' }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection} rounded-t-lg`}>
          <h3 className={`text-sm font-semibold ${theme.title}`}>{title}</h3>
          <button
            type="button"
            onClick={handleClose}
            disabled={uploading}
            className={`p-1 ${theme.button_default} rounded-[4px] text-xs`}
          >
            <i className="fa fa-times" aria-hidden="true" />
          </button>
        </div>

        <div className="p-3 flex flex-col gap-3">
          <input
            ref={fileInputRef}
            type="file"
            className="hidden"
            accept={effectiveAccept}
            multiple={effectiveQueueLimit > 1}
            onChange={handleInputChange}
          />
          <div
            role="button"
            tabIndex={0}
            onDrop={handleDrop}
            onDragOver={handleDragOver}
            onClick={() => fileInputRef.current?.click()}
            className={`border-2 border-dashed rounded-lg p-4 text-center cursor-pointer ${theme.mainContentSection} border-gray-300 dark:border-gray-600 hover:border-blue-400 focus:outline-none`}
          >
            <i className="fa fa-upload text-2xl mb-2 opacity-70" aria-hidden="true" />
            <p className={`text-xs ${theme.label}`}>
              Drop files here or click to select (max {effectiveQueueLimit})
            </p>
          </div>

          {selectedFiles.length > 0 && (
            <div className={`flex flex-col gap-1 max-h-32 overflow-auto ${theme.mainContentSection}`}>
              <div className={`text-xs font-medium ${theme.title}`}>Selected</div>
              {selectedFiles.map((f, i) => (
                <div key={`${f.name}-${i}`} className="flex items-center justify-between text-xs">
                  <span className={`truncate flex-auto min-w-0 ${theme.label}`}>{f.name}</span>
                  <button
                    type="button"
                    onClick={() => removeFile(i)}
                    disabled={uploading}
                    className="ml-2 text-red-500 hover:underline shrink-0"
                  >
                    Remove
                  </button>
                </div>
              ))}
            </div>
          )}

          {uploading && (
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                className="h-2 rounded-full bg-blue-500 transition-all duration-300"
                style={{ width: `${progress}%` }}
              />
            </div>
          )}

          {error && (
            <div className="text-red-600 dark:text-red-400 text-xs">{error}</div>
          )}
        </div>

        <div className={`px-3 py-2 border-t ${theme.mainContentSection} flex justify-end gap-2`}>
          <button
            type="button"
            onClick={handleUpload}
            disabled={uploading || selectedFiles.length === 0}
            className="px-3 py-1.5 text-sm rounded-[4px] bg-blue-500 hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed text-white"
          >
            {uploading ? 'Uploading…' : 'Upload'}
          </button>
          <button
            type="button"
            onClick={handleClose}
            disabled={uploading}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default FileUploader;
