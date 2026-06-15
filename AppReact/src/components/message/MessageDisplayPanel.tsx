import React, { useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import MessageFilePreviewModal from './MessageFilePreviewModal';
import { MESSAGE_TEMPLATE_SCOPE_TYPE } from './messageScopeConstants';

export type MessageDisplayMode = 'management' | 'standalone';

type MessageDisplayPanelProps = {
  mode: MessageDisplayMode;
  currentMessage: any;
  currentBoxType?: string;
  /** System notification — Angular: MessagePostType == 1 */
  isSystemNotification?: boolean;
  onClose?: () => void;
  onDeleteCurrent?: () => void;
  onOpenReply?: () => void;
  onOpenReplyAll?: () => void;
  onOpenForward?: () => void;
  onOpenDraft?: () => void;
  /** Message template: open code/layout editors */
  onOpenTemplateCodeEditor?: () => void;
  onOpenTemplateLayoutEditor?: () => void;
};

function attachCount(msg: any): number {
  const d = msg?.DictAttachmentFileIdAndDisplay ?? msg?.dictAttachmentFileIdAndDisplay;
  if (!d || typeof d !== 'object') return 0;
  return Object.keys(d).length;
}

const MessageDisplayPanel: React.FC<MessageDisplayPanelProps> = ({
  mode,
  currentMessage,
  currentBoxType,
  isSystemNotification,
  onClose,
  onDeleteCurrent,
  onOpenReply,
  onOpenReplyAll,
  onOpenForward,
  onOpenDraft,
  onOpenTemplateCodeEditor,
  onOpenTemplateLayoutEditor,
}) => {
  const { theme } = useTheme();
  const { addTabAndNavigate } = useTabNavigation();
  const [preview, setPreview] = useState<{ fileId: number; fileName: string } | null>(null);

  const m = currentMessage;
  if (!m) return null;

  const scopeType = m.MessgaeScopeType ?? m.messgaeScopeType;
  const isTemplate = Number(scopeType) === MESSAGE_TEMPLATE_SCOPE_TYPE;
  const isDraft = !!(m.IsDraft ?? m.isDraft);
  const postType = m.MessagePostType ?? m.messagePostType;

  const openForm = () => {
    const tid = m.TransactionId ?? m.transactionId;
    const rid = m.TransactionRootValueId ?? m.transactionRootValueId;
    if (tid == null || rid == null) return;
    addTabAndNavigate('FormMasterDetail', `Message Form: ${m.Subject || ''}`, { id: tid, param1: rid });
  };

  const openProject = () => {
    const pid = m.LinkToProjectId ?? m.linkToProjectId;
    if (pid == null) return;
    addTabAndNavigate('project-management', `Message Form: ${m.Subject || ''}`, { id: pid });
  };

  return (
    <section
      className={`w-full h-full flex flex-col ${theme.default}`}
      aria-label="Message reading pane"
    >
      <div className={`flex items-center justify-between px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
        <h2 className={`text-sm font-semibold m-0 ${theme.title}`}>
          {isSystemNotification || postType === 1 ? 'System Notification' : 'Message detail'}
        </h2>
        <div className="flex flex-wrap items-center gap-1">
          {onDeleteCurrent &&
            (mode === 'standalone' ||
              (mode === 'management' && (currentBoxType === 'Inbox' || currentBoxType === 'Sent'))) && (
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onDeleteCurrent}>
                <i className="fa-solid fa-trash mr-1" aria-hidden="true" />
                Delete
              </button>
            )}
          {!isTemplate && !isDraft && (
            <>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenReply}>
                <i className="fa-solid fa-reply mr-1" aria-hidden="true" />
                Reply
              </button>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenReplyAll}>
                <i className="fa-solid fa-reply-all mr-1" aria-hidden="true" />
                Reply All
              </button>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenForward}>
                <i className="fa-solid fa-share mr-1" aria-hidden="true" />
                Forward
              </button>
            </>
          )}
          {isTemplate && onOpenTemplateCodeEditor && (
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenTemplateCodeEditor}>
              <i className="fa-solid fa-pen-to-square mr-1" aria-hidden="true" />
              Edit
            </button>
          )}
          {isTemplate && onOpenTemplateLayoutEditor && (
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenTemplateLayoutEditor}>
              Design Layout
            </button>
          )}
          {isDraft && onOpenDraft && (
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onOpenDraft}>
              Open Draft
            </button>
          )}
          {!isTemplate && onClose && (
            <button type="button" className={`mr-4 px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
              <i className="fa-solid fa-circle-xmark mr-1" aria-hidden="true" />
              Close
            </button>
          )}
        </div>
      </div>

      <div className={`w-full min-h-0 flex-auto overflow-auto ${theme.mainContentSection} border ${theme.inputBox} m-1 p-2`}>
        {isTemplate ? (
          <div className="text-xs mb-1">
            <span className={theme.label}>Name: </span>
            <span>{m.Bcclist ?? m.bcclist ?? ''}</span>
          </div>
        ) : null}
        <div className="mb-1">
          <span className={`text-xs ${theme.label}`}>Subject: </span>
          <h3 className={`inline text-sm font-semibold m-0 ${theme.title}`}>
            {m.Subject ?? m.subject ?? ''}
          </h3>
        </div>
        {!isTemplate ? (
          <>
            <div className="text-xs mb-1">
              <span className={theme.label}>Date: </span>
              <span>{m.CreateDateString ?? m.createDateString ?? ''}</span>
            </div>
            <div className="text-xs mb-1">
              <span className={theme.label}>From: </span>
              <span>
                {m.CreateByUserName ?? m.createByUserName ?? ''} &lt;{m.FromEmail ?? m.fromEmail ?? ''}&gt;
              </span>
            </div>
            <div className="text-xs mb-1">
              <span className={theme.label}>To: </span>
              <span>{m.ToList ?? m.toList ?? ''}</span>
            </div>
            <div className="text-xs mb-1">
              <span className={theme.label}>Cc: </span>
              <span>{m.Cclist ?? m.cclist ?? ''}</span>
            </div>
            <div className="text-xs mb-1">
              <span className={theme.label}>Bcc: </span>
              <span>{m.Bcclist ?? m.bcclist ?? ''}</span>
            </div>
          </>
        ) : null}

        {attachCount(m) > 0 ? (
          <div className="text-xs my-2">
            <div className={`${theme.label} mb-1`}>Attachment</div>
            <div className="flex flex-wrap gap-2">
              {Object.entries(m.DictAttachmentFileIdAndDisplay ?? m.dictAttachmentFileIdAndDisplay ?? {}).map(([fid, name]) => (
                <button
                  key={fid}
                  type="button"
                  className={`underline ${theme.button_default} text-xs px-1`}
                  onClick={() => setPreview({ fileId: Number(fid), fileName: String(name) })}
                >
                  {String(name)}
                </button>
              ))}
            </div>
          </div>
        ) : null}

        <div className="flex gap-3 text-xs py-1">
          {(m.TransactionId ?? m.transactionId) != null && (m.TransactionRootValueId ?? m.transactionRootValueId) != null && (
            <button type="button" className={`underline ${theme.button_default}`} onClick={openForm}>
              Open Details
            </button>
          )}
          {(m.LinkToProjectId ?? m.linkToProjectId) != null && (
            <button type="button" className={`underline ${theme.button_default}`} onClick={openProject}>
              Open Project
            </button>
          )}
        </div>

        <div
          className={`prose prose-sm max-w-none text-sm mt-2 ${theme.default}`}
          dangerouslySetInnerHTML={{ __html: m.Message ?? m.message ?? '' }}
        />
      </div>

      {preview ? (
        <MessageFilePreviewModal fileId={preview.fileId} fileName={preview.fileName} onClose={() => setPreview(null)} />
      ) : null}
    </section>
  );
};

export default MessageDisplayPanel;
