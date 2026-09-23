import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import type { GenericAgentChatSummary } from '../../webapi/genericAgentSvc';

interface Props {
    chats: GenericAgentChatSummary[];
    selectedSessionKey: string | null;
    onSelect: (sessionKey: string) => void;
    onDelete: (sessionKey: string) => void;
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
    onNewChat,
    creating,
}) => {
    const { theme, t } = useTheme();
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

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
                    return (
                        <div
                            key={chat.SessionKey}
                            className={`w-full mb-1 px-2 py-2 rounded-[4px] flex items-start gap-1 ${
                                selected
                                    ? `${t('bg_default')} ${theme.sideBar_menu_active}`
                                    : theme.sideBar_menu
                            }`}
                        >
                            <button
                                type="button"
                                className="w-1 flex-auto min-w-0 text-left"
                                onClick={() => onSelect(chat.SessionKey)}
                            >
                                <div className={`text-xs truncate ${theme.title}${selected ? ' font-semibold' : ''}`}>
                                    {chat.Title?.trim() || 'New Chat'}
                                </div>
                                <div className={`text-[10px] truncate ${theme.label}`}>
                                    {chat.IsFixedTestSession ? 'Test session · ' : ''}
                                    {formatWhen(chat.UpdatedAt)}
                                </div>
                            </button>
                            <button
                                type="button"
                                className={`w-7 h-6 shrink-0 rounded-[4px] text-xs ${theme.button_default}`}
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
