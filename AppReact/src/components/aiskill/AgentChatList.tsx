import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentChatTitle, type GenericAgentChatSummary } from '../../webapi/genericAgentSvc';

interface Props {
    chats: GenericAgentChatSummary[];
    selectedSessionKey: string | null;
    onSelect: (sessionKey: string) => void;
    onDelete: (sessionKey: string) => void;
    onRename: (sessionKey: string, title: string) => void;
    onNewChat: () => void;
    creating?: boolean;
}

const formatWhen = (raw?: string) => {
    if (!raw) return '';
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return '';
    return d.toLocaleString();
};

const AgentChatList: React.FC<Props> = ({
    chats,
    selectedSessionKey,
    onSelect,
    onDelete,
    onRename,
    onNewChat,
    creating,
}) => {
    const { theme, t } = useTheme();
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const iconBtn = `w-7 h-6 shrink-0 rounded-[4px] text-xs ${theme.button_default}`;
    const [editingKey, setEditingKey] = useState<string | null>(null);
    const [draftTitle, setDraftTitle] = useState('');
    const editRef = useRef<HTMLInputElement | null>(null);
    const editingKeyRef = useRef<string | null>(null);

    useEffect(() => {
        if (editingKey) editRef.current?.focus();
    }, [editingKey]);

    const beginRename = (chat: GenericAgentChatSummary) => {
        editingKeyRef.current = chat.SessionKey;
        setEditingKey(chat.SessionKey);
        setDraftTitle(chat.Title?.trim() || '');
    };

    const cancelRename = () => {
        editingKeyRef.current = null;
        setEditingKey(null);
        setDraftTitle('');
    };

    const commitRename = (sessionKey: string) => {
        if (editingKeyRef.current !== sessionKey) return;
        editingKeyRef.current = null;
        const next = draftTitle.trim();
        setEditingKey(null);
        setDraftTitle('');
        if (!next) return;
        const current = chats.find(c => c.SessionKey === sessionKey);
        if (current && genericAgentChatTitle(current) === next) return;
        onRename(sessionKey, next);
    };

    return (
        <div className={`w-64 shrink-0 h-full flex flex-col overflow-hidden border-r ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between px-3 py-2 shrink-0">
                <span className={`text-xs font-semibold ${theme.title}`}>Chats</span>
                <button
                    type="button"
                    className={btn}
                    disabled={!!creating}
                    onClick={onNewChat}
                    title="New Chat"
                >
                    <i className="fa-solid fa-plus mr-1" />
                    New Chat
                </button>
            </div>
            <div className="h-1 flex-auto overflow-auto px-2 pb-2">
                {chats.length === 0 && (
                    <div className={`px-2 py-3 text-xs ${theme.label}`}>
                        No chats yet. Click New Chat to start.
                    </div>
                )}
                {chats.map(chat => {
                    const selected = chat.SessionKey === selectedSessionKey;
                    const title = genericAgentChatTitle(chat);
                    const editing = editingKey === chat.SessionKey;
                    return (
                        <div
                            key={chat.SessionKey}
                            className={`group w-full mb-1 px-2 py-2 rounded-[4px] flex items-start gap-1 ${
                                selected
                                    ? `${t('bg_default')} ${theme.sideBar_menu_active}`
                                    : theme.sideBar_menu
                            }`}
                        >
                            <div className="w-1 flex-auto min-w-0">
                                {editing ? (
                                    <input
                                        ref={editRef}
                                        type="text"
                                        value={draftTitle}
                                        maxLength={200}
                                        autoComplete="off"
                                        className={`w-full h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                        onClick={e => e.stopPropagation()}
                                        onChange={e => setDraftTitle(e.target.value)}
                                        onBlur={() => commitRename(chat.SessionKey)}
                                        onKeyDown={e => {
                                            if (e.key === 'Enter') {
                                                e.preventDefault();
                                                commitRename(chat.SessionKey);
                                            } else if (e.key === 'Escape') {
                                                e.preventDefault();
                                                cancelRename();
                                            }
                                        }}
                                    />
                                ) : (
                                    <button
                                        type="button"
                                        className="w-full min-w-0 text-left"
                                        onClick={() => onSelect(chat.SessionKey)}
                                        onDoubleClick={e => {
                                            e.preventDefault();
                                            e.stopPropagation();
                                            beginRename(chat);
                                        }}
                                        title={`${title} — double-click to rename`}
                                    >
                                        <div className={`text-xs truncate ${theme.title}${selected ? ' font-semibold' : ''}`}>
                                            {title}
                                        </div>
                                        <div className={`text-[10px] truncate ${theme.label}`}>
                                            {chat.IsFixedTestSession ? 'Test session · ' : ''}
                                            {formatWhen(chat.UpdatedAt)}
                                        </div>
                                    </button>
                                )}
                            </div>
                            {!editing && (
                                <button
                                    type="button"
                                    className={`${iconBtn} opacity-0 group-hover:opacity-100 focus:opacity-100`}
                                    title="Rename chat"
                                    onClick={e => {
                                        e.stopPropagation();
                                        beginRename(chat);
                                    }}
                                >
                                    <i className="fa-solid fa-pencil" />
                                </button>
                            )}
                            <button
                                type="button"
                                className={iconBtn}
                                title="Delete chat"
                                onClick={e => {
                                    e.stopPropagation();
                                    onDelete(chat.SessionKey);
                                }}
                            >
                                <i className="fa-solid fa-trash" />
                            </button>
                        </div>
                    );
                })}
            </div>
        </div>
    );
};

export default AgentChatList;
