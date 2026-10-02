import React, { useEffect, useMemo, useRef, useState } from 'react';
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

const startOfToday = () => {
    const d = new Date();
    d.setHours(0, 0, 0, 0);
    return d.getTime();
};

const formatWhen = (raw?: string) => {
    if (!raw) return '';
    const d = new Date(raw);
    if (Number.isNaN(d.getTime())) return '';
    return d.toLocaleString();
};

type ChatGroup = { key: string; label: string; items: GenericAgentChatSummary[] };

const groupChats = (chats: GenericAgentChatSummary[]): ChatGroup[] => {
    const today = startOfToday();
    const todayItems: GenericAgentChatSummary[] = [];
    const earlierItems: GenericAgentChatSummary[] = [];
    for (const c of chats) {
        const t = c.UpdatedAt ? new Date(c.UpdatedAt).getTime() : 0;
        if (t >= today) todayItems.push(c);
        else earlierItems.push(c);
    }
    const groups: ChatGroup[] = [];
    if (todayItems.length) groups.push({ key: 'today', label: 'Today', items: todayItems });
    if (earlierItems.length) groups.push({ key: 'earlier', label: 'Earlier', items: earlierItems });
    return groups;
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
    const borderCls = t('border_mainContentSection');
    const btn = `w-full px-3 py-2 text-sm rounded-[4px] ${theme.button_default}`;
    const iconBtn = `w-7 h-6 shrink-0 rounded-[4px] text-xs ${theme.button_default}`;
    const [query, setQuery] = useState('');
    const [menuKey, setMenuKey] = useState<string | null>(null);
    const [editingKey, setEditingKey] = useState<string | null>(null);
    const [draftTitle, setDraftTitle] = useState('');
    const editRef = useRef<HTMLInputElement | null>(null);
    const editingKeyRef = useRef<string | null>(null);
    const menuRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        if (editingKey) editRef.current?.focus();
    }, [editingKey]);

    useEffect(() => {
        if (!menuKey) return;
        const onDoc = (e: MouseEvent) => {
            if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                setMenuKey(null);
            }
        };
        document.addEventListener('mousedown', onDoc);
        return () => document.removeEventListener('mousedown', onDoc);
    }, [menuKey]);

    const filtered = useMemo(() => {
        const q = query.trim().toLowerCase();
        if (!q) return chats;
        return chats.filter(c => {
            const title = genericAgentChatTitle(c).toLowerCase();
            return title.includes(q) || (c.SessionKey || '').toLowerCase().includes(q);
        });
    }, [chats, query]);

    const groups = useMemo(() => groupChats(filtered), [filtered]);

    const beginRename = (chat: GenericAgentChatSummary) => {
        setMenuKey(null);
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
        <div className={`w-72 shrink-0 h-full flex flex-col overflow-hidden border-r ${borderCls} ${theme.mainContentSection}`}>
            <div className="px-3 pt-3 pb-2 shrink-0 flex flex-col gap-2">
                <button
                    type="button"
                    className={btn}
                    disabled={!!creating}
                    onClick={onNewChat}
                    title="New Chat"
                >
                    <i className="fa-solid fa-plus mr-1.5" />
                    New Chat
                </button>
                <input
                    type="search"
                    value={query}
                    onChange={e => setQuery(e.target.value)}
                    placeholder="Search chats…"
                    className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                />
            </div>

            <div className="h-1 flex-auto overflow-auto px-2 pb-3">
                {filtered.length === 0 && (
                    <div className={`px-2 py-3 text-xs ${theme.label}`}>
                        {chats.length === 0
                            ? 'No chats yet. Click New Chat to start.'
                            : 'No chats match your search.'}
                    </div>
                )}
                {groups.map(g => (
                    <div key={g.key} className="mb-3">
                        <div className={`px-2 py-1 text-[10px] uppercase tracking-wide opacity-60 ${theme.label}`}>
                            {g.label}
                        </div>
                        {g.items.map(chat => {
                            const selected = chat.SessionKey === selectedSessionKey;
                            const title = genericAgentChatTitle(chat);
                            const editing = editingKey === chat.SessionKey;
                            const menuOpen = menuKey === chat.SessionKey;
                            return (
                                <div
                                    key={chat.SessionKey}
                                    className={`group relative w-full mb-0.5 rounded-[4px] flex items-stretch border-l-2 ${
                                        selected
                                            ? `${t('bg_default')} ${theme.sideBar_menu_active} ${borderCls}`
                                            : `border-transparent ${theme.sideBar_menu}`
                                    }`}
                                >
                                    <div className="w-1 flex-auto min-w-0 flex items-start gap-1 px-2 py-2">
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
                                                    <div className={`text-sm truncate ${theme.title}${selected ? ' font-semibold' : ''}`}>
                                                        {title}
                                                    </div>
                                                    <div className={`text-[10px] truncate mt-0.5 ${theme.label}`}>
                                                        {chat.IsFixedTestSession ? 'Test session · ' : ''}
                                                        {formatWhen(chat.UpdatedAt)}
                                                    </div>
                                                </button>
                                            )}
                                        </div>
                                        {!editing && (
                                            <div className="relative shrink-0" ref={menuOpen ? menuRef : undefined}>
                                                <button
                                                    type="button"
                                                    className={`${iconBtn} opacity-0 group-hover:opacity-100 focus:opacity-100 ${menuOpen ? 'opacity-100' : ''}`}
                                                    title="Chat actions"
                                                    onClick={e => {
                                                        e.stopPropagation();
                                                        setMenuKey(menuOpen ? null : chat.SessionKey);
                                                    }}
                                                >
                                                    <i className="fa-solid fa-ellipsis" />
                                                </button>
                                                {menuOpen && (
                                                    <div
                                                        className={`absolute right-0 top-7 z-20 min-w-[8rem] py-1 rounded-[4px] border shadow-sm ${borderCls} ${theme.mainContentSection}`}
                                                    >
                                                        <button
                                                            type="button"
                                                            className={`w-full text-left px-3 py-1.5 text-xs ${theme.label} hover:opacity-100`}
                                                            onClick={e => {
                                                                e.stopPropagation();
                                                                beginRename(chat);
                                                            }}
                                                        >
                                                            <i className="fa-solid fa-pencil mr-2 opacity-70" />Rename
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className={`w-full text-left px-3 py-1.5 text-xs ${theme.label} hover:opacity-100`}
                                                            onClick={e => {
                                                                e.stopPropagation();
                                                                setMenuKey(null);
                                                                onDelete(chat.SessionKey);
                                                            }}
                                                        >
                                                            <i className="fa-solid fa-trash mr-2 opacity-70" />Delete
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        )}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                ))}
            </div>
        </div>
    );
};

export default AgentChatList;
