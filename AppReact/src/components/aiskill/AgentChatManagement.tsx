import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch } from 'react-redux';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { getCurrentActiveTab, updateTabPath } from '../../redux/features/ui/navigation/tabnavSlice';
import { genericAgentSvc, type GenericAgentChatSummary } from '../../webapi/genericAgentSvc';
import AgentChatList from './AgentChatList';
import AgentUiChatHost from './AgentUiChatHost';

interface Props {
    skillKey: string;
}

const AgentChatManagement: React.FC<Props> = ({ skillKey }) => {
    const { theme } = useTheme();
    const dispatch = useDispatch();
    const [searchParams, setSearchParams] = useSearchParams();
    const chatFromUrl = searchParams.get('chat');
    const [chats, setChats] = useState<GenericAgentChatSummary[]>([]);
    const [selectedKey, setSelectedKey] = useState<string | null>(chatFromUrl);
    const [creating, setCreating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const applyChatQuery = useCallback((sessionKey: string | null) => {
        const next = new URLSearchParams(searchParams);
        if (sessionKey) next.set('chat', sessionKey);
        else next.delete('chat');
        if (!next.get('skillKey')) next.set('skillKey', skillKey);
        next.delete('agentUi');
        setSearchParams(next, { replace: true });
        const tab = getCurrentActiveTab();
        if (tab?.tabKey) {
            const qs = next.toString();
            dispatch(updateTabPath({ tabKey: tab.tabKey, path: qs ? `/agent-chat?${qs}` : '/agent-chat' }));
        }
    }, [dispatch, searchParams, setSearchParams, skillKey]);

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

    const selected = useMemo(
        () => chats.find(c => c.SessionKey === selectedKey) ?? null,
        [chats, selectedKey],
    );

    return (
        <div className="w-full h-full flex flex-row overflow-hidden">
            <AgentChatList
                chats={chats}
                selectedSessionKey={selectedKey}
                onSelect={handleSelect}
                onDelete={handleDelete}
                onNewChat={() => { void handleNewChat(); }}
                creating={creating}
            />
            <div className="w-1 flex-auto h-full min-w-0 overflow-hidden flex flex-col">
                {error && (
                    <div className={`px-3 py-1 text-xs ${theme.label}`}>{error}</div>
                )}
                {selectedKey ? (
                    <AgentUiChatHost
                        key={`${skillKey}:${selectedKey}`}
                        skillKey={skillKey}
                        chatSessionKey={selectedKey}
                        onConversationChanged={() => { void refreshList(selectedKey); }}
                    />
                ) : (
                    <div className={`w-full h-full flex items-center justify-center text-sm ${theme.label}`}>
                        {selected ? selected.Title : 'Select a chat or click New Chat.'}
                    </div>
                )}
            </div>
        </div>
    );
};

export default AgentChatManagement;
