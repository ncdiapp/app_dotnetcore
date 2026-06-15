import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { endpoints } from '../../webapi/endpoints';
import type { RootState } from '../../redux/store';

type MessageFilePreviewModalProps = {
  fileId: number | string;
  fileName?: string;
  onClose: () => void;
};

const MessageFilePreviewModal: React.FC<MessageFilePreviewModalProps> = ({ fileId, fileName, onClose }) => {
  const { theme } = useTheme();
  const sessionId = useSelector((s: RootState) => s.userSession.userContext?.SessionId ?? '');
  const previewUrl = useMemo(() => {
    const sid = sessionId ? `&CurrentUserSessionId=${encodeURIComponent(String(sessionId))}` : '';
    return endpoints.buildEndpointUrl(`/GetLatestFile.aspx?FileId=${fileId}${sid}`);
  }, [fileId, sessionId]);
  const downloadUrl = useMemo(() => {
    const sid = sessionId ? `&CurrentUserSessionId=${encodeURIComponent(String(sessionId))}` : '';
    return endpoints.buildEndpointUrl(`/GetLatestFile.aspx?FileId=${fileId}${sid}&IsDownload=true`);
  }, [fileId, sessionId]);

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]"
      onClick={onClose}
      role="presentation"
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '800px', height: '600px', maxWidth: '95vw', maxHeight: '90vh' }}
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-label="File preview"
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold truncate ${theme.title}`}>{fileName || `File ${fileId}`}</div>
          <div className="flex items-center gap-2 shrink-0">
            <a
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              href={downloadUrl}
              target="_blank"
              rel="noopener noreferrer"
              onClick={(e) => e.stopPropagation()}
            >
              <i className="fa-solid fa-download mr-1" aria-hidden="true" />
              Download
            </a>
            <button type="button" className={`p-1 ${theme.button_default} rounded-[4px] text-xs`} onClick={onClose} title="Close">
              <i className="fa-solid fa-xmark" aria-hidden="true" />
            </button>
          </div>
        </div>
        <div className="w-full h-1 flex-auto overflow-hidden flex items-center justify-center p-3">
          <img
            src={previewUrl}
            alt={fileName || ''}
            className="max-w-full max-h-full object-contain"
            onError={(e) => {
              (e.target as HTMLImageElement).style.display = 'none';
            }}
          />
        </div>
      </div>
    </div>
  );
};

export default MessageFilePreviewModal;
