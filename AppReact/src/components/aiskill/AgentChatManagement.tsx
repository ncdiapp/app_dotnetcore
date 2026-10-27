import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { getCurrentActiveTab, updateTabPath } from '../../redux/features/ui/navigation/tabnavSlice';
import { genericAgentSvc, genericAgentChatTitle, type GenericAgentChatSummary } from '../../webapi/genericAgentSvc';
import AgentChatList from './AgentChatList';
import AgentUiChatHost from './AgentUiChatHost';

interface Props {
    skillKey: string;
    routeBasePath?: string;
    includeSkillQuery?: boolean;
}

const AgentChatManagement: React.FC<Props> = ({ skillKey, routeBasePath = '/agent-chat', includeSkillQuery = true }) => {
    const { theme, t } = useTheme();
    const dispatch = useDispatch();
    const [searchParams, setSearchParams] = useSearchParams();
    const chatFromUrl = searchParams.get('chat');
    const handoffRun = searchParams.get('handoffRun') === '1';
    const [chats, setChats] = useState<GenericAgentChatSummary[]>([]);
    const [selectedKey, setSelectedKey] = useState<string | null>(chatFromUrl);
    const [creating, setCreating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const applyChatQuery = useCallback((sessionKey: string | null) => {
        const next = new URLSearchParams(searchParams);
        if (sessionKey) next.set('chat', sessionKey);
        else next.delete('chat');
        if (includeSkillQuery) {
            if (!next.get('skillKey')) next.set('skillKey', skillKey);
        } else {
            next.delete('skillKey');
        }
        next.delete('agentUi');
        setSearchParams(next, { replace: true });
        const tab = getCurrentActiveTab();
        if (tab?.tabKey) {
            const qs = next.toString();
            dispatch(updateTabPath({ tabKey: tab.tabKey, path: qs ? `${routeBasePath}?${qs}` : routeBasePath }));
        }
    }, [dispatch, includeSkillQuery, routeBasePath, searchParams, setSearchParams, skillKey]);

    const refreshList = useCallback(async (preferKey?: string | null) => {
        const list = await genericAgentSvc.ListChats(skillKey);
        setChats(list);
        const want = preferKey ?? chatFromUrl;
        const exists = want && list.some(c => c.SessionKey === want);
        if (exists) {
            setSelectedKey(want);
            return;
        }
        const latestOpen = list.find(c => !c.IsFixedTestSession) ?? list[0] ?? null;
        const nextKey = latestOpen?.SessionKey ?? null;
        setSelectedKey(nextKey);
        applyChatQuery(nextKey);
    }, [applyChatQuery, chatFromUrl, skillKey]);

    const clearHandoffRun = useCallback(() => {
        const next = new URLSearchParams(searchParams);
        if (next.get('handoffRun') !== '1') return;
        next.delete('handoffRun');
        setSearchParams(next, { replace: true });
        const tab = getCurrentActiveTab();
        if (tab?.tabKey) {
            const qs = next.toString();
            dispatch(updateTabPath({ tabKey: tab.tabKey, path: qs ? `${routeBasePath}?${qs}` : routeBasePath }));
        }
    }, [dispatch, routeBasePath, searchParams, setSearchParams]);

    useEffect(() => {
        void refreshList(chatFromUrl);
    }, [skillKey]); // eslint-disable-line react-hooks/exhaustive-deps -- boot / skill change only

    const handleSelect = (sessionKey: string) => {
        setSelectedKey(sessionKey);
        applyChatQuery(sessionKey);
    };

    const handleNewChat = async () => {
        setCreating(true);
        setError(null);
        try {
            const created = await genericAgentSvc.CreateChat(skillKey);
            if (!created?.SessionKey) {
                setError('Failed to create chat.');
                return;
            }
            await refreshList(created.SessionKey);
            setSelectedKey(created.SessionKey);
            applyChatQuery(created.SessionKey);
        } finally {
            setCreating(false);
        }
    };

    const handleDelete = async (sessionKey: string) => {
        await genericAgentSvc.DeleteChat(skillKey, sessionKey);
        const remaining = chats.filter(c => c.SessionKey !== sessionKey);
        const next = remaining.find(c => !c.IsFixedTestSession) ?? remaining[0] ?? null;
        setChats(remaining);
        setSelectedKey(next?.SessionKey ?? null);
        applyChatQuery(next?.SessionKey ?? null);
    };

    const handleRename = async (sessionKey: string, title: string) => {
        const next = title.trim();
        if (!next) return;
        setChats(prev => prev.map(c => c.SessionKey === sessionKey ? { ...c, Title: next } : c));
        try {
            const ok = await genericAgentSvc.RenameChat(skillKey, sessionKey, next);
            if (!ok) {
                setError('Failed to rename chat.');
                await refreshList(selectedKey);
                return;
            }
            setError(null);
        } catch {
            setError('Failed to rename chat.');
            await refreshList(selectedKey);
        }
    };

    const selected = useMemo(
        () => chats.find(c => c.SessionKey === selectedKey) ?? null,
        [chats, selectedKey],
    );
    const shellRef = useRef<HTMLDivElement | null>(null);
    const listDragRef = useRef(false);
    const [listWidth, setListWidth] = useState(288);
    const [narrow, setNarrow] = useState(false);
    const [listCollapsed, setListCollapsed] = useState(false);
    const [drawerOpen, setDrawerOpen] = useState(false);

    useEffect(() => {
        const el = shellRef.current;
        if (!el) return;
        const update = () => setNarrow(el.clientWidth < 900);
        update();
        const obs = new ResizeObserver(update);
        obs.observe(el);
        return () => obs.disconnect();
    }, []);

    const startListResize = (e: React.MouseEvent) => {
        e.preventDefault();
        listDragRef.current = true;
        const startX = e.clientX;
        const startW = listWidth;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        const onMove = (ev: MouseEvent) => {
            if (!listDragRef.current) return;
            const shellW = shellRef.current?.clientWidth ?? 1200;
            const max = Math.min(520, Math.max(220, shellW - 420));
            setListWidth(Math.min(max, Math.max(220, startW + (ev.clientX - startX))));
        };
        const onUp = () => {
            listDragRef.current = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
        };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    };

    const selectChat = (sessionKey: string) => {
        handleSelect(sessionKey);
        setDrawerOpen(false);
    };

    const list = (
        <AgentChatList
            chats={chats}
            selectedSessionKey={selectedKey}
            onSelect={selectChat}
            onDelete={handleDelete}
            onRename={(sessionKey, title) => { void handleRename(sessionKey, title); }}
            onNewChat={() => { void handleNewChat(); }}
            creating={creating}
            widthPx={listWidth}
            collapsed={!narrow && listCollapsed}
            onToggleCollapse={() => {
                if (narrow) setDrawerOpen(false);
                else setListCollapsed(v => !v);
            }}
        />
    );

    return (
        <div ref={shellRef} className={`relative w-full h-full flex flex-row overflow-hidden ${t('bg_default')}`}>
            {!narrow && list}
            {!narrow && !listCollapsed && (
                <div
                    role="separator"
                    aria-orientation="vertical"
                    title="Drag to resize chats"
                    className="w-2 shrink-0 cursor-col-resize"
                    onMouseDown={startListResize}
                />
            )}
            {narrow && drawerOpen && (
                <>
                    <button
                        type="button"
                        className={`absolute inset-0 z-10 opacity-60 ${theme.mainContentSection}`}
                        aria-label="Close chats"
                        onClick={() => setDrawerOpen(false)}
                    />
                    <div className="absolute left-0 top-0 z-20 h-full">
                        {list}
                    </div>
                </>
            )}
            <div className="w-1 flex-auto h-full min-w-0 overflow-hidden flex flex-col">
                {error && (
                    <div className={`px-3 py-1 text-xs ${theme.label}`}>{error}</div>
                )}
                {selectedKey ? (
                    <AgentUiChatHost
                        key={`${skillKey}:${selectedKey}`}
                        skillKey={skillKey}
                        chatSessionKey={selectedKey}
                        chatTitle={selected ? genericAgentChatTitle(selected) : 'New chat'}
                        showChatListButton={narrow || listCollapsed}
                        onOpenChatList={() => {
                            if (narrow) setDrawerOpen(true);
                            else setListCollapsed(false);
                        }}
                        onConversationChanged={() => { void refreshList(selectedKey); }}
                        autoRunPendingUser={handoffRun}
                        onAutoRunStarted={clearHandoffRun}
                    />
                ) : (
                    <div className={`w-full h-full flex items-center justify-center text-sm rounded-[8px] border shadow-sm ${t('border_mainContentSection')} ${theme.mainContentSection} ${theme.label}`}>
                        {selected ? genericAgentChatTitle(selected) : 'Select a chat or click New Chat.'}
                    </div>
                )}
            </div>
        </div>
    );
};

export default AgentChatManagement;
