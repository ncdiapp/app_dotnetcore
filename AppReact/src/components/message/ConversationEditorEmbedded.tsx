import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useEnumEntry } from '../../hooks/useEnumDictionary';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { appMessageService } from '../../webapi/appmessagesvc';
import { adminSvc } from '../../webapi/adminsvc';
import { uploadFileToDataImage } from '../../webapi/dataImageUploadSvc';
import type { RootState } from '../../redux/store';
import EmailAddressSelectorModal from './EmailAddressSelectorModal';

type ConversationEditorEmbeddedProps = {
  scopeType: number | string;
  transactionId: string;
  transactionRootValueId: string;
  defaultMessageSubject?: string;
  className?: string;
};

function asString(v: unknown): string {
  if (v === null || v === undefined) return '';
  return String(v);
}

/** Angular: flat list OR subgroup list depending on GroupByType */
function getRawMessageList(messageGroupDto: any): any[] {
  if (!messageGroupDto) return [];
  const candidate =
    messageGroupDto.MessageDtoList ??
    messageGroupDto.messageDtoList ??
    messageGroupDto.messageList ??
    messageGroupDto.MessageList;
  if (Array.isArray(candidate)) return candidate;
  return [];
}

function buildEmptyNewMessageDto(
  scopeType: number | string,
  transactionId: string,
  transactionRootValueId: string,
  emPostTypeConversaction: number | undefined,
  subGroupId?: string | number | null
): any {
  const st = typeof scopeType === 'string' ? parseInt(scopeType, 10) || scopeType : scopeType;
  const dto: any = {
    Subject: '',
    Message: '',
    MessgaeScopeType: st,
    TransactionId: transactionId,
    TransactionRootValueId: transactionRootValueId,
    DictAttachmentFileIdAndDisplay: {} as Record<string, string>,
    IsAttachFormPrintDoc: false
  };
  if (emPostTypeConversaction !== undefined) {
    dto.MessagePostType = emPostTypeConversaction;
  }
  if (subGroupId != null && subGroupId !== '') {
    dto.SubGroupId = subGroupId;
  }
  return dto;
}

const ConversationEditorEmbedded: React.FC<ConversationEditorEmbeddedProps> = ({
  scopeType,
  transactionId,
  transactionRootValueId,
  defaultMessageSubject,
  className
}) => {
  const { theme, t } = useTheme();
  const { addTabAndNavigate } = useTabNavigation();
  const emPostTypeConversaction = useEnumEntry('EmAppMessgaePostType', 'Conversaction');

  const publicFileFolderId = useSelector((state: RootState) => {
    const raw = state.userSession?.userContext?.DictAppSetup?.PublicFileFolderId;
    return raw != null && raw !== '' ? Number(raw) : undefined;
  });

  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [messageGroupDto, setMessageGroupDto] = useState<any | null>(null);
  const [allUsers, setAllUsers] = useState<any[] | null>(null);

  const [currentMessageDto, setCurrentMessageDto] = useState<any>(() =>
    buildEmptyNewMessageDto(scopeType, transactionId, transactionRootValueId, emPostTypeConversaction)
  );

  const [isEmailSelectorOpen, setIsEmailSelectorOpen] = useState(false);
  /** Group-by: bottom main composer ("New Post For All") starts collapsed; flat list expands via effect */
  const [isMainComposerExpanded, setIsMainComposerExpanded] = useState(false);
  const attachInputRef = useRef<HTMLInputElement>(null);
  const subGroupAttachRefs = useRef<Record<string, HTMLInputElement | null>>({});

  /** Group-by: collapsed state per SubGroupId (Angular: isCollapsed) */
  const [subGroupCollapsed, setSubGroupCollapsed] = useState<Record<string, boolean>>({});
  /** Group-by: draft newMessageDto per subgroup (key = SubGroupId) */
  const [subGroupDrafts, setSubGroupDrafts] = useState<Record<string, any>>({});

  const currentUserId: string | null = useMemo(() => {
    const v = messageGroupDto?.CurrentUserId ?? messageGroupDto?.currentUserId ?? null;
    return v ? String(v) : null;
  }, [messageGroupDto]);

  const followupDtoList: any[] = useMemo(() => {
    const list = messageGroupDto?.FollowupDtoList ?? messageGroupDto?.followupDtoList ?? [];
    return Array.isArray(list) ? list : [];
  }, [messageGroupDto]);

  const followupDisplay2 = useMemo(() => {
    return followupDtoList
      .map((f: any) => f?.UserName ?? f?.userName ?? '')
      .filter(Boolean)
      .join('; ');
  }, [followupDtoList]);

  const isCurrentUserFollowup = useMemo(() => {
    if (!currentUserId) return false;
    return followupDtoList.some((f: any) => String(f?.UserId ?? f?.userId ?? '') === currentUserId);
  }, [currentUserId, followupDtoList]);

  const groupByType = messageGroupDto?.GroupByType ?? messageGroupDto?.groupByType;
  const rawList = useMemo(() => getRawMessageList(messageGroupDto), [messageGroupDto]);
  const isGroupBy = Boolean(groupByType);

  const flatMessages = useMemo(() => {
    if (isGroupBy) return [];
    return rawList;
  }, [isGroupBy, rawList]);

  const subGroups = useMemo(() => {
    if (!isGroupBy) return [];
    return rawList;
  }, [isGroupBy, rawList]);

  const isFollowUpUserFromTransaction = Boolean(
    messageGroupDto?.IsFollowUpUserFromTransaction ?? messageGroupDto?.isFollowUpUserFromTransaction
  );

  useEffect(() => {
    setIsMainComposerExpanded(!isGroupBy);
  }, [isGroupBy]);

  const refresh = useCallback(async () => {
    if (!transactionId || !transactionRootValueId) return;
    setIsBusy(true);
    setError(null);
    try {
      const [mg, users] = await Promise.all([
        appMessageService.retrieveScopeMessageGroupDto(
          asString(scopeType),
          asString(transactionId),
          asString(transactionRootValueId),
          '',
          ''
        ),
        adminSvc.retrieveCurrentUserAvailableEmailToUsers()
      ]);
      setMessageGroupDto(mg ?? null);
      setAllUsers(Array.isArray(users) ? users : []);

      const g = mg ?? null;
      const gList = getRawMessageList(g);
      const gb = Boolean(g?.GroupByType ?? g?.groupByType);
      if (gb && gList.length) {
        const nextDrafts: Record<string, any> = {};
        const nextCollapsed: Record<string, boolean> = {};
        gList.forEach((sg: any) => {
          const sid = String(sg?.SubGroupId ?? sg?.subGroupId ?? '');
          if (!sid) return;
          nextDrafts[sid] =
            sg.newMessageDto ??
            buildEmptyNewMessageDto(scopeType, transactionId, transactionRootValueId, emPostTypeConversaction, sid);
          nextCollapsed[sid] = Boolean(sg.isCollapsed ?? sg.IsCollapsed);
        });
        setSubGroupDrafts(nextDrafts);
        setSubGroupCollapsed(nextCollapsed);
      } else {
        setSubGroupDrafts({});
        setSubGroupCollapsed({});
      }

      setCurrentMessageDto((prev: any) => ({
        ...buildEmptyNewMessageDto(scopeType, transactionId, transactionRootValueId, emPostTypeConversaction),
        IsAttachFormPrintDoc: prev?.IsAttachFormPrintDoc ?? false
      }));
    } catch (e: any) {
      setError(e?.message ?? 'Failed to load conversation');
    } finally {
      setIsBusy(false);
    }
  }, [scopeType, transactionId, transactionRootValueId, emPostTypeConversaction]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const prepareFollowupUserIdList = useCallback((): string[] => {
    return followupDtoList.map((f: any) => String(f?.UserId ?? f?.userId ?? '')).filter(Boolean);
  }, [followupDtoList]);

  const initialFollowerUserIds = useMemo(
    () => prepareFollowupUserIdList(),
    [prepareFollowupUserIdList]
  );

  const applyFollowersFromModal = useCallback(
    async (followupUserIdList: string[]) => {
      const updateDto = {
        FollowupUserIdList: followupUserIdList,
        ScopeType: typeof scopeType === 'string' ? parseInt(scopeType, 10) || scopeType : scopeType,
        TransactionId: transactionId,
        TransactionRId: transactionRootValueId,
        TaskId: '',
        ProjectOrWorkflowId: '',
        ProjectTeamId: ''
      };
      setIsBusy(true);
      setError(null);
      try {
        await appMessageService.updateUserMessgeFollowupByScope(updateDto);
        await refresh();
      } catch (e: any) {
        setError(e?.message ?? 'Failed to update followers');
        throw e;
      } finally {
        setIsBusy(false);
      }
    },
    [scopeType, transactionId, transactionRootValueId, refresh]
  );

  const saveOne = useCallback(
    async (newMessageDto: any) => {
      const dto = { ...newMessageDto };
      dto.Subject = dto.Subject || defaultMessageSubject || '';
      dto.MessgaeScopeType =
        typeof scopeType === 'string' ? parseInt(scopeType, 10) || scopeType : scopeType;
      dto.TransactionId = transactionId;
      dto.TransactionRootValueId = transactionRootValueId;
      if (emPostTypeConversaction !== undefined) {
        dto.MessagePostType = emPostTypeConversaction;
      }
      dto.ToList = '';
      if (!dto.SubGroupId) {
        dto.ToUserIdList = prepareFollowupUserIdList();
      } else {
        dto.ToUserIdList = [];
      }

      let attachmentToken = '';
      if (dto.DictAttachmentFileIdAndDisplay) {
        Object.keys(dto.DictAttachmentFileIdAndDisplay).forEach((fileId) => {
          attachmentToken = attachmentToken ? `${attachmentToken}|${fileId}` : String(fileId);
        });
      }
      dto.AttachmentFileToken = attachmentToken;
      if (attachmentToken.length > 0 && !dto.Message?.trim()) {
        dto.Message = 'New file added.';
      }

      if (!dto.Message?.trim()) return;

      setIsBusy(true);
      setError(null);
      try {
        await appMessageService.saveOneAppMessageDto(dto);
        await refresh();
      } catch (e: any) {
        setError(e?.message ?? 'Failed to send message');
      } finally {
        setIsBusy(false);
      }
    },
    [
      defaultMessageSubject,
      scopeType,
      transactionId,
      transactionRootValueId,
      emPostTypeConversaction,
      prepareFollowupUserIdList,
      refresh
    ]
  );

  const handleSendMain = useCallback(() => {
    saveOne({
      ...currentMessageDto,
      Subject: currentMessageDto.Subject || defaultMessageSubject || ''
    });
  }, [currentMessageDto, defaultMessageSubject, saveOne]);

  const handleSendSubGroup = useCallback(
    (subGroupId: string) => {
      const draft = subGroupDrafts[subGroupId];
      if (!draft) return;
      saveOne({ ...draft });
    },
    [subGroupDrafts, saveOne]
  );

  const setFollow = useCallback(
    async (isFollow: boolean) => {
      if (!currentUserId) return;
      const existing = followupDtoList.map((f: any) => String(f?.UserId ?? f?.userId ?? '')).filter(Boolean);
      const next = new Set(existing);
      if (isFollow) next.add(currentUserId);
      else next.delete(currentUserId);

      const updateDto = {
        FollowupUserIdList: Array.from(next),
        ScopeType: typeof scopeType === 'string' ? parseInt(scopeType, 10) || scopeType : scopeType,
        TransactionId: transactionId,
        TransactionRId: transactionRootValueId,
        TaskId: '',
        ProjectOrWorkflowId: '',
        ProjectTeamId: ''
      };

      setIsBusy(true);
      setError(null);
      try {
        await appMessageService.updateUserMessgeFollowupByScope(updateDto);
        await refresh();
      } catch (e: any) {
        setError(e?.message ?? 'Failed to update followers');
      } finally {
        setIsBusy(false);
      }
    },
    [currentUserId, followupDtoList, scopeType, transactionId, transactionRootValueId, refresh]
  );

  const openMessageEditor = useCallback(() => {
    addTabAndNavigate(
      'message-editor',
      'Message Editor',
      {
        MessgaeScopeType: typeof scopeType === 'string' ? parseInt(scopeType, 10) || scopeType : scopeType,
        TransactionId: transactionId,
        TransactionRootValueId: transactionRootValueId,
        newMessageBody: currentMessageDto?.Message ?? '',
        ToUserIdList: prepareFollowupUserIdList()
      },
      true
    );
  }, [addTabAndNavigate, scopeType, transactionId, transactionRootValueId, currentMessageDto?.Message, prepareFollowupUserIdList]);

  const handleAttachFiles = useCallback(
    async (files: FileList | null, targetDto: any, updateDto: (patch: Partial<any>) => void) => {
      if (!files?.length) return;
      setIsBusy(true);
      setError(null);
      try {
        const dict = { ...(targetDto.DictAttachmentFileIdAndDisplay || {}) };
        for (let i = 0; i < files.length; i++) {
          const file = files[i];
          const res = await uploadFileToDataImage(file, {
            callingFrom: 'File',
            targetFolderId: publicFileFolderId
          });
          const fid = res.FileId;
          if (fid != null) {
            dict[String(fid)] = file.name;
          }
        }
        updateDto({ DictAttachmentFileIdAndDisplay: dict });
      } catch (e: any) {
        setError(e?.message ?? 'Upload failed');
      } finally {
        setIsBusy(false);
      }
    },
    [publicFileFolderId]
  );

  const dictAttachment = messageGroupDto?.DictAttachmentFileIdAndDisplay ?? messageGroupDto?.dictAttachmentFileIdAndDisplay ?? {};

  const expandOrCollapseAll = useCallback(
    (collapseAll: boolean) => {
      setSubGroupCollapsed((prev) => {
        const next = { ...prev };
        subGroups.forEach((sg: any) => {
          const sid = String(sg?.SubGroupId ?? sg?.subGroupId ?? '');
          if (sid) next[sid] = collapseAll;
        });
        return next;
      });
    },
    [subGroups]
  );

  const renderMessageBubble = (messageDto: any, idx: number) => {
    const fromId = messageDto?.AppCreatedById ?? messageDto?.FromUserId ?? messageDto?.fromUserId;
    const isMine = currentUserId && fromId && String(fromId) === String(currentUserId);
    const userName =
      messageDto?.CreateByUserName ?? messageDto?.createByUserName ?? messageDto?.FromUserName ?? '';
    const messageText = messageDto?.Message ?? messageDto?.message ?? '';
    const created =
      messageDto?.CreateDateString ?? messageDto?.createDateString ?? messageDto?.CreatedTimeDisplay ?? '';
    const sort = messageDto?.Sort ?? messageDto?.sort;

    return (
      <div
        key={messageDto?.Id ?? messageDto?.id ?? `m-${idx}`}
        className={`flex w-full ${isMine ? 'justify-end' : 'justify-start'}`}
      >
        <div className={`max-w-[85%] min-w-0 sm:min-w-[200px] ${isMine ? 'text-right' : 'text-left'}`}>
          {/* Flex row: avoid absolute #sort overlapping the timestamp (was squeezing "PM" etc.) */}
          <div
            className={`mb-0.5 flex items-baseline gap-2 text-[11px] ${theme.label} opacity-80 ${
              isMine ? 'flex-row-reverse' : 'flex-row'
            }`}
          >
            <div className={`min-w-0 flex-1 ${isMine ? 'text-right' : 'text-left'}`}>
              <span className="capitalize">{userName}</span>
              {created ? (
                <span className="ml-1 inline-block break-words align-baseline">{created}</span>
              ) : null}
            </div>
            {sort != null ? (
              <span className="shrink-0 text-[10px] tabular-nums opacity-90" title={`#${sort}`}>
                #{sort}
              </span>
            ) : null}
          </div>
            <div
              className="text-xs rounded-[6px] px-3 py-2 text-left"
              style={{
                background: 'linear-gradient(to bottom right, rgb(23,135,217), rgb(82,173,207))',
                color: 'white'
              }}
            >
            {messageText.includes('<') ? (
              <div
                className="whitespace-pre-wrap break-words [&_a]:underline"
                dangerouslySetInnerHTML={{ __html: messageText }}
              />
            ) : (
              <div className="whitespace-pre-wrap break-words">{messageText}</div>
            )}
          </div>
          {messageDto?.AttachmentFieldIds?.length ? (
            <div className="mt-1 flex flex-wrap gap-1 text-[11px] px-2">
              {messageDto.AttachmentFieldIds.map((fileId: any) => (
                <span key={String(fileId)} className="underline cursor-pointer opacity-80">
                  {(dictAttachment as any)[fileId] ?? fileId}
                </span>
              ))}
            </div>
          ) : null}
        </div>
      </div>
    );
  };

  const composerToolbar = (opts: {
    variant: 'main' | 'sub';
    subGroupId?: string;
    draft: any;
    setDraft: (d: any) => void;
    onSend: () => void;
    /** Main composer only: collapsed to one row (group-by default) */
    mainComposerCollapsed?: boolean;
    onExpandMainComposer?: () => void;
    /** Main: show "New Post For All" heading (group-by + expanded only) */
    showMainFollowersTitleRow?: boolean;
    showMainComposerCollapseButton?: boolean;
    onCollapseMainComposer?: () => void;
  }) => {
    const {
      variant,
      subGroupId,
      draft,
      setDraft,
      onSend,
      mainComposerCollapsed,
      onExpandMainComposer,
      showMainFollowersTitleRow,
      showMainComposerCollapseButton,
      onCollapseMainComposer
    } = opts;
    const fileInputId = variant === 'main' ? 'conv-attach-main' : `conv-attach-${subGroupId}`;
    const isMain = variant === 'main';

    const attachInputBlock =
      variant === 'main' ? (
        <input
          ref={attachInputRef}
          type="file"
          multiple
          className="hidden"
          id={fileInputId}
          onChange={(e) => {
            handleAttachFiles(e.target.files, draft, (patch) => setDraft({ ...draft, ...patch }));
            e.target.value = '';
          }}
        />
      ) : (
        <input
          ref={(el) => {
            if (subGroupId) subGroupAttachRefs.current[subGroupId] = el;
          }}
          type="file"
          multiple
          className="hidden"
          id={fileInputId}
          onChange={(e) => {
            handleAttachFiles(e.target.files, draft, (patch) => setDraft({ ...draft, ...patch }));
            e.target.value = '';
          }}
        />
      );

    const attachmentsStrip =
      draft?.DictAttachmentFileIdAndDisplay && Object.keys(draft.DictAttachmentFileIdAndDisplay).length > 0 ? (
        <div className={`text-[11px] ${isMain ? 'px-3 pb-2' : 'px-2 pb-2'}`}>
          <span className={theme.label}>Files: </span>
          {Object.entries(draft.DictAttachmentFileIdAndDisplay).map(([fid, name]) => (
            <span key={fid} className="inline-block mr-2">
              <button
                type="button"
                className="text-red-600 mr-0.5"
                onClick={() => {
                  const next = { ...draft.DictAttachmentFileIdAndDisplay };
                  delete next[fid];
                  setDraft({ ...draft, DictAttachmentFileIdAndDisplay: next });
                }}
              >
                ×
              </button>
              <span className="underline">{String(name)}</span>
            </span>
          ))}
        </div>
      ) : null;

    if (isMain) {
      if (mainComposerCollapsed && onExpandMainComposer) {
        return (
          <div
            className={`shrink-0 border-t ${t('border_mainContentSection')} ${theme.mainContentSection} shadow-[0_-6px_16px_rgba(0,0,0,0.06)]`}
          >
            {/* Row 1: primary action — expand composer only */}
            <button
              type="button"
              className={`flex w-full items-center gap-2 px-3 py-2.5 text-left transition-colors ${theme.menu_default} hover:opacity-95`}
              onClick={onExpandMainComposer}
              title="New post for all — visible here for anyone who can open the form. Followers only receive email alerts."
            >
              <i className="fa-solid fa-paper-plane shrink-0 opacity-80" aria-hidden />
              <span className={`min-w-0 flex-1 truncate text-xs font-medium ${theme.title}`}>
                New Post For All
              </span>
              <i className="fa-solid fa-chevron-up shrink-0 text-[10px] opacity-60" aria-hidden />
            </button>
            {/* Row 2: optional email recipients + watch — same handlers as expanded footer */}
            <div
              className={`flex items-center gap-2 border-t px-3 py-2 ${t('border_mainContentSection')} ${theme.menu_secondary}`}
            >
              <button
                type="button"
                className={`flex min-w-0 flex-1 items-center gap-1.5 rounded-lg px-2.5 py-1.5 text-left text-xs ${theme.button_default} ${theme.label}`}
                title={
                  followupDisplay2
                    ? `Email to: ${followupDisplay2}. Optional — does not limit who can read messages on this form.`
                    : 'Choose who gets email when messages are posted (optional). Anyone who can open this form can still read here without following.'
                }
                onClick={() => setIsEmailSelectorOpen(true)}
                disabled={isBusy}
              >
                <i className="fa-solid fa-user-plus shrink-0 opacity-90" aria-hidden />
                <span className="truncate">{followupDisplay2 || 'Email to…'}</span>
              </button>
              {!isCurrentUserFollowup ? (
                <button
                  type="button"
                  className={`shrink-0 whitespace-nowrap rounded-lg px-2.5 py-1.5 text-xs ${theme.button_default}`}
                  onClick={() => setFollow(true)}
                  disabled={isBusy}
                  title="Watch"
                >
                  <i className="fa-regular fa-bell mr-1" aria-hidden />
                  Watch
                </button>
              ) : (
                <button
                  type="button"
                  className={`shrink-0 whitespace-nowrap rounded-lg px-2.5 py-1.5 text-xs ${theme.button_default}`}
                  onClick={() => setFollow(false)}
                  disabled={isBusy}
                  title="Watching"
                >
                  <i className="fa-solid fa-bell mr-1" aria-hidden />
                  Watching
                </button>
              )}
            </div>
          </div>
        );
      }

      return (
        <div
          className={`shrink-0 border-t ${t('border_mainContentSection')} ${theme.mainContentSection} shadow-[0_-6px_16px_rgba(0,0,0,0.06)]`}
        >
          {showMainFollowersTitleRow ? (
            <>
              <div className="flex items-center justify-between gap-2 px-3 pt-2.5 pb-1">
                <span className={`text-xs font-semibold leading-snug ${theme.title}`}>
                  <i className="fa-solid fa-paper-plane mr-1.5 opacity-70" aria-hidden />
                  New Post For All
                </span>
                {showMainComposerCollapseButton && onCollapseMainComposer ? (
                  <button
                    type="button"
                    className={`shrink-0 px-2 py-1 text-[10px] rounded-lg ${theme.button_default}`}
                    onClick={onCollapseMainComposer}
                    title="Collapse"
                    aria-label="Collapse New Post For All composer"
                  >
                    <i className="fa-solid fa-chevron-down" aria-hidden />
                  </button>
                ) : null}
              </div>
              <p className={`px-3 pb-2 text-[10px] leading-snug ${theme.label} opacity-90`}>
                Shown on this form for everyone who can access it. Followers only add email notifications; you do not
                need to follow to read messages here.
              </p>
            </>
          ) : null}

          <div
            className={`flex flex-wrap items-center gap-2 px-3 pb-2 ${!showMainFollowersTitleRow ? 'pt-2.5' : ''}`}
          >
            <button
              type="button"
              className={`px-2.5 py-1.5 text-xs rounded-lg ${theme.button_default}`}
              onClick={openMessageEditor}
              title="Message Editor"
            >
              <i className="fa-solid fa-pencil mr-1" aria-hidden />
              Message Editor
            </button>
            <label className="flex items-center gap-2 text-xs cursor-pointer rounded-lg px-2 py-1 border border-transparent hover:border-current/20">
              <input
                type="checkbox"
                checked={Boolean(draft?.IsAttachFormPrintDoc)}
                onChange={(e) => setDraft({ ...draft, IsAttachFormPrintDoc: e.target.checked })}
                disabled={isBusy}
              />
              <span className={theme.label}>Attachment Print Doc</span>
            </label>
            <div className="min-w-0 flex-1" />
          </div>

          <div className="px-3 pb-2">
            <div className={`rounded-xl border ${t('border_mainContentSection')} ${theme.menu_default} p-2`}>
              <div className="flex gap-2 items-end">
                <textarea
                  value={draft?.Message ?? ''}
                  onChange={(e) => setDraft({ ...draft, Message: e.target.value })}
                  className={`box-border min-w-0 flex-1 min-h-[72px] max-h-[140px] resize-y px-3 py-2.5 text-sm leading-snug border-0 rounded-lg ${theme.inputBox} focus:outline-none focus:ring-2 focus:ring-offset-0 focus:ring-current/20`}
                  disabled={isBusy}
                  rows={3}
                />
                <button
                  type="button"
                  className={`shrink-0 self-end mb-0.5 px-4 py-2 text-sm font-medium rounded-lg ${theme.button_default}`}
                  onClick={onSend}
                  disabled={isBusy || !(draft?.Message || '').trim()}
                  title="Send"
                >
                  <i className="fa-solid fa-paper-plane mr-1.5" aria-hidden />
                  Send
                </button>
              </div>
            </div>
          </div>

          <div className="relative flex flex-wrap items-center gap-1.5 px-3 pb-3 min-h-[36px]">
            {attachInputBlock}
            <button
              type="button"
              className={`px-2.5 py-1.5 text-xs rounded-lg ${theme.button_default}`}
              title="Attach files"
              onClick={() => attachInputRef.current?.click()}
              disabled={isBusy}
            >
              <i className="fa-solid fa-paperclip" aria-hidden />
            </button>
            <button
              type="button"
              className={`px-2.5 py-1.5 text-xs rounded-lg ${theme.button_default}`}
              title="Choose who gets email when messages are posted (optional)"
              onClick={() => setIsEmailSelectorOpen(true)}
              disabled={isBusy}
            >
              <i className="fa-solid fa-user-plus" aria-hidden />
            </button>
            <div
              className={`min-w-0 max-w-[55%] flex-1 truncate text-xs px-1 py-0.5 cursor-pointer rounded ${theme.label}`}
              title={
                followupDisplay2
                  ? `Email to: ${followupDisplay2}`
                  : 'Optional email recipients — does not affect who can read messages on this form'
              }
              onClick={() => setIsEmailSelectorOpen(true)}
            >
              {followupDisplay2 || 'Email to…'}
            </div>
            {!isCurrentUserFollowup ? (
              <button
                type="button"
                className={`ml-auto px-2.5 py-1.5 text-xs rounded-lg ${theme.button_default}`}
                onClick={() => setFollow(true)}
                disabled={isBusy}
                title="Watch"
              >
                <i className="fa-regular fa-bell mr-1" aria-hidden />
                Watch
              </button>
            ) : (
              <button
                type="button"
                className={`ml-auto px-2.5 py-1.5 text-xs rounded-lg ${theme.button_default}`}
                onClick={() => setFollow(false)}
                disabled={isBusy}
                title="Watching"
              >
                <i className="fa-solid fa-bell mr-1" aria-hidden />
                Watching
              </button>
            )}
          </div>

          {attachmentsStrip}
        </div>
      );
    }

    return (
      <div
        className={`shrink-0 mt-2 flex rounded-xl border overflow-hidden ${t('border_mainContentSection')} ${theme.menu_secondary}`}
      >
        <div className={`w-[3px] shrink-0 ${t('bg_menu_default')}`} aria-hidden />
        <div className="min-w-0 flex-1 flex flex-col">
        <div className={`px-2.5 pt-2 pb-1 flex items-center justify-between gap-2 border-b ${t('border_mainContentSection')}`}>
          <span className={`text-[11px] font-medium ${theme.label}`}>
            <i className="fa-solid fa-reply mr-1.5 opacity-80" aria-hidden />
            Reply in this group
          </span>
        </div>

        <div className="p-2">
          <div className="flex gap-2 items-end">
            <textarea
              value={draft?.Message ?? ''}
              onChange={(e) => setDraft({ ...draft, Message: e.target.value })}
              className={`box-border min-w-0 flex-1 min-h-[52px] max-h-[120px] resize-y px-2.5 py-2 text-xs leading-snug border rounded-lg ${theme.inputBox} focus:outline-none focus:ring-2 focus:ring-offset-0 focus:ring-current/15`}
              disabled={isBusy}
              rows={2}
            />
            <button
              type="button"
              className={`shrink-0 self-end mb-0.5 px-3 py-1.5 text-xs font-medium rounded-lg ${theme.button_default}`}
              onClick={onSend}
              disabled={isBusy || !(draft?.Message || '').trim()}
              title="Send"
            >
              <i className="fa-solid fa-paper-plane" aria-hidden />
            </button>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-1 px-2 pb-2 min-h-[32px]">
          {attachInputBlock}
          <button
            type="button"
            className={`px-2 py-1 text-xs rounded-lg ${theme.button_default}`}
            title="Attach files"
            onClick={() => subGroupId && subGroupAttachRefs.current[subGroupId]?.click()}
            disabled={isBusy}
          >
            <i className="fa-solid fa-paperclip" aria-hidden />
          </button>
          <div className="min-w-0 flex-1" />
        </div>

        {attachmentsStrip}
        </div>
      </div>
    );
  };

  return (
    <div className={`flex h-full min-h-0 w-full flex-col ${className ?? ''}`}>
      <div
        className={`flex h-full min-h-0 w-full flex-col overflow-hidden rounded border ${theme.mainContentSection} ${t('border_mainContentSection')}`}
      >
        <div className={`shrink-0 px-3 py-2 border-b ${t('border_mainContentSection')}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Conversation</div>
        </div>

        {error ? <div className="shrink-0 px-3 py-2 text-xs text-red-600">{error}</div> : null}

        {/* Message list: in a column flex, do NOT use w-1 (Tailwind = 4px wide); use w-full + flex-1 + min-h-0 */}
            <div
              className={`min-h-0 min-w-0 w-full flex-1 overflow-y-auto overflow-x-hidden ${isGroupBy ? `p-3 space-y-3 ${theme.menu_secondary ?? ''} opacity-95` : 'p-2'}`}
          >
          {isBusy && rawList.length === 0 ? <div className="text-xs opacity-70 p-2">Loading…</div> : null}

          {isGroupBy ? (
            <>
              {subGroups.length > 1 ? (
                <div className="flex flex-wrap items-center gap-2">
                  <span className={`text-[10px] uppercase tracking-wide ${theme.label} mr-1`}>Threads</span>
                  <button
                    type="button"
                    className={`px-3 py-1 text-xs rounded-full border ${t('border_mainContentSection')} ${theme.button_default}`}
                    onClick={() => expandOrCollapseAll(false)}
                  >
                    <i className="fa-solid fa-angles-down mr-1" aria-hidden />
                    Expand all
                  </button>
                  <button
                    type="button"
                    className={`px-3 py-1 text-xs rounded-full border ${t('border_mainContentSection')} ${theme.button_default}`}
                    onClick={() => expandOrCollapseAll(true)}
                  >
                    <i className="fa-solid fa-angles-up mr-1" aria-hidden />
                    Collapse all
                  </button>
                </div>
              ) : null}

              {subGroups.length === 0 ? (
                <div
                  className={`p-4 text-xs text-center rounded-xl border border-dashed ${t('border_mainContentSection')} ${theme.label}`}
                >
                  No group messages yet.
                </div>
              ) : (
                subGroups.map((sg: any, gi: number) => {
                  const sid = String(sg?.SubGroupId ?? sg?.subGroupId ?? gi);
                  const name = sg?.SubGroupName ?? sg?.subGroupName ?? `Group ${sid}`;
                  const collapsed = subGroupCollapsed[sid] ?? false;
                  const messagesInGroup = sg?.ConversationMessageList ?? sg?.conversationMessageList ?? [];
                  const draft = subGroupDrafts[sid] ?? buildEmptyNewMessageDto(
                    scopeType,
                    transactionId,
                    transactionRootValueId,
                    emPostTypeConversaction,
                    sid
                  );
                  const initial = (name || '?').trim().charAt(0).toUpperCase();

                  return (
                    <div
                      key={sid}
                      className={`rounded-xl border ${t('border_mainContentSection')} ${theme.mainContentSection} shadow-sm overflow-hidden`}
                    >
                      <button
                        type="button"
                        className={`flex w-full items-center gap-3 px-3 py-2.5 text-left text-sm font-medium transition-colors ${theme.menu_default} hover:opacity-95`}
                        onClick={() =>
                          setSubGroupCollapsed((prev) => ({ ...prev, [sid]: !collapsed }))
                        }
                      >
                        <span
                          className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold ${t('bg_menu_default')} ${t('text_menu_default')}`}
                          aria-hidden
                        >
                          {initial}
                        </span>
                        <span className={`min-w-0 flex-1 capitalize truncate ${theme.title}`}>{name}</span>
                        <i
                          className={`fa-solid shrink-0 opacity-60 ${collapsed ? 'fa-chevron-right' : 'fa-chevron-down'}`}
                          aria-hidden
                        />
                      </button>
                      {!collapsed ? (
                        <div className={`space-y-2 px-3 pb-3 pt-1 border-t ${t('border_mainContentSection')}`}>
                          {Array.isArray(messagesInGroup) &&
                            messagesInGroup.map((m: any, mi: number) => renderMessageBubble(m, mi))}
                          {composerToolbar({
                            variant: 'sub',
                            subGroupId: sid,
                            draft,
                            setDraft: (d) => setSubGroupDrafts((prev) => ({ ...prev, [sid]: d })),
                            onSend: () => handleSendSubGroup(sid)
                          })}
                        </div>
                      ) : null}
                    </div>
                  );
                })
              )}
            </>
          ) : (
            <div className="space-y-2 min-h-[80px]">
              {Object.keys(dictAttachment).length > 0 ? (
                <div className={`text-xs p-2 rounded border ${t('border_mainContentSection')}`}>
                  <div className="font-medium mb-1">File List:</div>
                  <div className="flex flex-wrap gap-2">
                    {Object.entries(dictAttachment).map(([fid, fname]) => (
                      <span key={fid} className="underline cursor-pointer">
                        {String(fname)}
                      </span>
                    ))}
                  </div>
                </div>
              ) : null}
              {flatMessages.map((m: any, idx: number) => renderMessageBubble(m, idx))}
              {!isBusy && flatMessages.length === 0 ? (
                <div className="text-xs opacity-70 p-2">No messages</div>
              ) : null}
            </div>
          )}
        </div>

        {/* Main composer at bottom (hidden when group-by + follow-up user from transaction only) */}
        {!(isGroupBy && isFollowUpUserFromTransaction) ? (
          composerToolbar({
            variant: 'main',
            draft: currentMessageDto,
            setDraft: setCurrentMessageDto,
            onSend: handleSendMain,
            mainComposerCollapsed: Boolean(isGroupBy && !isMainComposerExpanded),
            onExpandMainComposer: () => setIsMainComposerExpanded(true),
            showMainFollowersTitleRow: Boolean(isGroupBy && isMainComposerExpanded),
            showMainComposerCollapseButton: Boolean(isGroupBy && isMainComposerExpanded),
            onCollapseMainComposer: () => setIsMainComposerExpanded(false)
          })
        ) : null}
      </div>

      <EmailAddressSelectorModal
        open={isEmailSelectorOpen}
        onClose={() => setIsEmailSelectorOpen(false)}
        rawUsers={allUsers}
        initialFollowerUserIds={initialFollowerUserIds}
        onApply={applyFollowersFromModal}
        isBusy={isBusy}
      />
    </div>
  );
};

export default ConversationEditorEmbedded;
