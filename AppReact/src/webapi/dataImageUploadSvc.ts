/**
 * Service for uploading files via the backend DataImage.aspx endpoint.
 * Same endpoint as Angular (DataImage.aspx / DataImage.aspx.cs).
 *
 * Two modes:
 * 1. Basic (AppFile): upload to file management, returns FileId.
 * 2. Folder: upload to a designated web folder (e.g. Company Logo, Company Background), returns ResultMessage (e.g. fileName) for building image URL.
 */

import { endpoints } from './endpoints';
import { store } from '../redux/store';
import type { RootState } from '../redux/store';

/** CallingFrom values supported by DataImage.aspx.cs (EmCallingFrom) */
export type DataImageCallingFrom =
  | 'File'
  | 'Image'
  | 'UploadCompanyLogoImage'
  | 'UploadCompanyBackgrundImage'
  | 'UploadFileToWebSiteFolder'
  | 'ImportExcelToDatabase'
  | 'APIOperationTesting'
  | 'UploadFileByTransactionCommand';

export interface DataImageUploadParams {
  /** Required. Determines backend handler and response shape. */
  callingFrom: DataImageCallingFrom;
  /** For AppFile mode: folder id in file management. Omit for default (e.g. PublicFileFolderId). */
  targetFolderId?: number;
  /** For update-existing-file scenario. */
  updateFileId?: number;
  transactionId?: number;
  /** Additional query params if needed (e.g. ImportSettingDataSetId, ApiOperationId). */
  extraParams?: Record<string, string | number | boolean | undefined>;
}

export interface DataImageUploadResult {
  /** Present when callingFrom is File/Image (AppFile) and upload succeeds. */
  FileId?: number;
  /** Message or fileName returned by folder-upload handlers (e.g. Company Logo). */
  ResultMessage?: string;
  ExcelUploadTableName?: string;
  FileOrgPath?: string;
  FormData?: unknown;
  ValidationResult?: unknown;
}

const FILE_UPLOAD_PATH = '/api/files/upload';

function getSessionId(): string | null {
  const state = store.getState() as RootState;
  return state?.userSession?.userContext?.SessionId ?? null;
}

/**
 * Build the full URL for /api/files/upload with query parameters.
 * Session is sent via header only (not query string).
 */
export function buildDataImageUploadUrl(params: DataImageUploadParams): string {
  const search = new URLSearchParams();
  search.set('CallingFrom', params.callingFrom);
  if (params.targetFolderId != null) {
    search.set('TargetFolderId', String(params.targetFolderId));
  }
  if (params.updateFileId != null) {
    search.set('UpdateFileId', String(params.updateFileId));
  }
  if (params.transactionId != null) {
    search.set('TransactionId', String(params.transactionId));
  }
  if (params.extraParams) {
    Object.entries(params.extraParams).forEach(([k, v]) => {
      if (v !== undefined && v !== '') {
        search.set(k, String(v));
      }
    });
  }
  const baseUrl =
    typeof window !== 'undefined' && window.location?.origin
      ? `${window.location.origin}/appai${FILE_UPLOAD_PATH}`
      : endpoints.buildEndpointUrl(FILE_UPLOAD_PATH);
  return `${baseUrl}?${search.toString()}`;
}

/**
 * Upload a single file to DataImage.aspx.
 * React app uses the same backend as Angular; session is sent via query string.
 */
export async function uploadFileToDataImage(
  file: File,
  params: DataImageUploadParams,
  onProgress?: (percent: number) => void
): Promise<DataImageUploadResult> {
  const url = buildDataImageUploadUrl(params);
  const formData = new FormData();
  formData.append('file', file);

  const xhr = new XMLHttpRequest();
  const result = await new Promise<DataImageUploadResult>((resolve, reject) => {
    xhr.upload.addEventListener('progress', (e) => {
      if (e.lengthComputable && onProgress) {
        onProgress(Math.round((e.loaded / e.total) * 100));
      }
    });
    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          const json = JSON.parse(xhr.responseText || '{}') as DataImageUploadResult;
          resolve(json);
        } catch {
          resolve({ ResultMessage: xhr.responseText || 'OK' });
        }
      } else {
        reject(new Error(xhr.responseText || `Upload failed: ${xhr.status}`));
      }
    });
    xhr.addEventListener('error', () => reject(new Error('Network error')));
    xhr.addEventListener('abort', () => reject(new Error('Upload aborted')));
    xhr.open('POST', url);
    const sessionId = getSessionId();
    if (sessionId) {
      xhr.setRequestHeader('CurrentUserSessionId', sessionId);
    }
    xhr.send(formData);
  });
  return result;
}

/**
 * Upload a single file using fetch (no progress). Use uploadFileToDataImage with onProgress when progress is needed.
 */
export async function uploadFileToDataImageFetch(
  file: File,
  params: DataImageUploadParams
): Promise<DataImageUploadResult> {
  const url = buildDataImageUploadUrl(params);
  const formData = new FormData();
  formData.append('file', file);

  const headers: HeadersInit = {};
  const sessionId = getSessionId();
  if (sessionId) {
    headers['CurrentUserSessionId'] = sessionId;
  }

  const response = await fetch(url, {
    method: 'POST',
    body: formData,
    headers,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Upload failed: ${response.status}`);
  }
  const json = (await response.json()) as DataImageUploadResult;
  return json;
}
