import React, { useState, useMemo, useCallback, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { DbGenieChatMessageDto, SchemaExtractionResultDto, DbGenieTableMetadataDto } from '../../webapi/dbgeniesvc';

// Section components
import DbaGenieDashboard from './DbaGenieDashboard';
import DbaGenieChat from './DbaGenieChat';
import DbaGenieSchemaView from './DbaGenieSchemaView';

// Section codes
const EmSectionCode = {
    Dashboard: 'Dashboard',
    Chat: 'Chat',
    SchemaView: 'SchemaView',
};

// Section configuration
interface SectionConfig {
    code: string;
    label: string;
    icon: string;
}

const SECTIONS: SectionConfig[] = [
    { code: EmSectionCode.Dashboard, label: 'Dashboard', icon: 'fa-solid fa-gauge-high' },
    { code: EmSectionCode.Chat,      label: 'AI Chat',   icon: 'fa-solid fa-comments' },
    { code: EmSectionCode.SchemaView, label: 'Schema',   icon: 'fa-solid fa-diagram-project' },
];

// ── Chat History (localStorage) ──────────────────────────────────────────────

export interface ChatHistoryItem {
    sessionId: string;
    title: string;
    timestamp: string;
    dataSourceName?: string;
    dataSourceRegisterId?: number;
    dataSourceType?: number;
    conversationHistory: DbGenieChatMessageDto[];
}

const HISTORY_KEY = 'dbgenie_chat_history';
const MAX_HISTORY = 30;

function loadChatHistory(): ChatHistoryItem[] {
    try {
        const raw = localStorage.getItem(HISTORY_KEY);
        return raw ? JSON.parse(raw) : [];
    } catch {
        return [];
    }
}

function saveChatHistory(list: ChatHistoryItem[]) {
    try {
        localStorage.setItem(HISTORY_KEY, JSON.stringify(list.slice(0, MAX_HISTORY)));
    } catch {}
}

function upsertSession(list: ChatHistoryItem[], session: DbaGenieSessionState): ChatHistoryItem[] {
    if (!session.conversationHistory.length) return list;
    const firstUserMsg = session.conversationHistory.find(m => m.Role === 'user');
    const title = firstUserMsg ? firstUserMsg.Content.slice(0, 55) : 'New Chat';
    const item: ChatHistoryItem = {
        sessionId: session.sessionId,
        title,
        timestamp: new Date().toISOString(),
        dataSourceName: session.dataSourceName,
        dataSourceRegisterId: session.dataSourceRegisterId,
        dataSourceType: session.dataSourceType,
        conversationHistory: session.conversationHistory,
    };
    const filtered = list.filter(h => h.sessionId !== session.sessionId);
    return [item, ...filtered];
}

function formatRelativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days < 7) return `${days}d ago`;
    return new Date(iso).toLocaleDateString();
}

// ── Session State ─────────────────────────────────────────────────────────────

// Helper: get initial section from tab cache or URL or default
function getInitialSectionCode(searchParams: URLSearchParams): string {
    const tabKey = getCurrentActiveTab()?.tabKey ?? null;
    if (tabKey) {
        const cached = getDataModelFromCache(tabKey);
        if (cached?.currentSectionCode && SECTIONS.some(s => s.code === cached.currentSectionCode)) {
            return cached.currentSectionCode;
        }
    }
    return searchParams.get('param1') || EmSectionCode.Dashboard;
}

// DBA-Genie session state
export interface DbaGenieSessionState {
    sessionId: string;
    dataSourceRegisterId?: number;
    dataSourceType?: number;
    dataSourceName?: string;
    conversationHistory: DbGenieChatMessageDto[];
    extractedSchema?: SchemaExtractionResultDto;
    schemaContext?: DbGenieTableMetadataDto[];
}

// ── Component ─────────────────────────────────────────────────────────────────

const DbaGenie: React.FC = () => {
    const { theme } = useTheme();
    const [searchParams, setSearchParams] = useSearchParams();

    // Current section
    const [currentSectionCode, setCurrentSectionCode] = useState<string>(() =>
        getInitialSectionCode(searchParams)
    );

    // Session state
    const [sessionState, setSessionState] = useState<DbaGenieSessionState>({
        sessionId: crypto.randomUUID(),
        conversationHistory: [],
    });

    // Chat history list
    const [chatHistory, setChatHistory] = useState<ChatHistoryItem[]>(() => loadChatHistory());

    // Update session state helper
    const updateSessionState = useCallback((updates: Partial<DbaGenieSessionState>) => {
        setSessionState(prev => ({ ...prev, ...updates }));
    }, []);

    // Auto-save session to history whenever conversation changes
    useEffect(() => {
        if (!sessionState.conversationHistory.length) return;
        setChatHistory(prev => {
            const updated = upsertSession(prev, sessionState);
            saveChatHistory(updated);
            return updated;
        });
    }, [sessionState.conversationHistory, sessionState.sessionId]);

    // Data model for tab cache
    const dataModel = useMemo(() => ({
        currentSectionCode,
        sessionState,
    }), [currentSectionCode, sessionState]);
    useTabDataAutoCache(dataModel);

    // Handle section selection
    const handleSelectSection = (sectionCode: string) => {
        setCurrentSectionCode(sectionCode);
        const newParams = new URLSearchParams(searchParams);
        newParams.set('param1', sectionCode);
        setSearchParams(newParams, { replace: true });
    };

    // Section button class
    const getSectionClass = (sectionCode: string): string => {
        return currentSectionCode === sectionCode ? `${theme.tab_active}` : `${theme.tab}`;
    };

    // Navigate to chat section
    const navigateToChat = useCallback(() => {
        handleSelectSection(EmSectionCode.Chat);
    }, [searchParams]);

    // Navigate to schema view
    const navigateToSchemaView = useCallback(() => {
        handleSelectSection(EmSectionCode.SchemaView);
    }, [searchParams]);

    // Handle schema extraction result
    const handleSchemaExtracted = useCallback((result: SchemaExtractionResultDto) => {
        updateSessionState({ extractedSchema: result });
        navigateToSchemaView();
    }, [updateSessionState, navigateToSchemaView]);

    // New chat — create a fresh session (current one is already saved via useEffect)
    const handleNewChat = useCallback(() => {
        setSessionState({
            sessionId: crypto.randomUUID(),
            dataSourceRegisterId: sessionState.dataSourceRegisterId,
            dataSourceType: sessionState.dataSourceType,
            dataSourceName: sessionState.dataSourceName,
            conversationHistory: [],
        });
        handleSelectSection(EmSectionCode.Chat);
    }, [sessionState.dataSourceRegisterId, sessionState.dataSourceType, sessionState.dataSourceName]);

    // Load a session from history
    const handleLoadSession = useCallback((item: ChatHistoryItem) => {
        setSessionState({
            sessionId: item.sessionId,
            dataSourceRegisterId: item.dataSourceRegisterId,
            dataSourceType: item.dataSourceType,
            dataSourceName: item.dataSourceName,
            conversationHistory: item.conversationHistory,
        });
        handleSelectSection(EmSectionCode.Chat);
    }, []);

    // Delete a session from history
    const handleDeleteSession = useCallback((e: React.MouseEvent, sessionId: string) => {
        e.stopPropagation();
        setChatHistory(prev => {
            const updated = prev.filter(h => h.sessionId !== sessionId);
            saveChatHistory(updated);
            return updated;
        });
    }, []);

    // Get the current section component
    const getCurrentSectionComponent = (): React.ReactNode => {
        switch (currentSectionCode) {
            case EmSectionCode.Dashboard:
                return (
                    <DbaGenieDashboard
                        sessionState={sessionState}
                        onSessionStateChange={updateSessionState}
                        onSchemaExtracted={handleSchemaExtracted}
                        onNavigateToChat={navigateToChat}
                    />
                );
            case EmSectionCode.Chat:
                return (
                    <DbaGenieChat
                        sessionState={sessionState}
                        onSessionStateChange={updateSessionState}
                        onNavigateToSchemaView={navigateToSchemaView}
                        onNewChat={handleNewChat}
                    />
                );
            case EmSectionCode.SchemaView:
                return (
                    <DbaGenieSchemaView
                        sessionState={sessionState}
                        onSessionStateChange={updateSessionState}
                    />
                );
            default:
                return (
                    <div className="flex items-center justify-center h-full text-gray-500">
                        <div className="text-center">
                            <i className="fa-solid fa-question-circle text-4xl mb-2"></i>
                            <div>Unknown section: {currentSectionCode}</div>
                        </div>
                    </div>
                );
        }
    };

    const isChat = currentSectionCode === EmSectionCode.Chat;

    return (
        <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">
            <div className="w-full h-full overflow-hidden flex gap-1">

                {/* Left Icon Navigation */}
                <div className={`w-[72px] flex-none ${theme.mainContentSection} overflow-y-auto rounded-l-md`}>
                    <div className="py-4">
                        {/* DBA-Genie Title */}
                        <div className="flex flex-col items-center mb-4 px-1">
                            <i className="fa-solid fa-robot text-xl text-blue-500 mb-1"></i>
                            <div className="text-[10px] font-semibold text-center leading-tight">DBA<br/>Genie</div>
                        </div>

                        {/* Section buttons */}
                        {SECTIONS.map((section) => (
                            <div
                                key={section.code}
                                onClick={() => handleSelectSection(section.code)}
                                className={`
                                    cursor-pointer flex flex-col items-center py-2.5 px-1 mb-1 transition-colors rounded-sm mx-1
                                    ${getSectionClass(section.code)}
                                `}
                                title={section.label}
                            >
                                <i className={`${section.icon} text-lg mb-1`}></i>
                                <div className="text-[9px] text-center leading-snug" style={{ wordBreak: 'break-word' }}>
                                    {section.label}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Chat History Panel — visible only in Chat section */}
                {isChat && (
                    <div className={`w-[220px] flex-none flex flex-col ${theme.mainContentSection} overflow-hidden`}>
                        {/* Header */}
                        <div className={`flex items-center justify-between px-3 py-2.5 border-b border-gray-200 dark:border-gray-700`}>
                            <span className={`text-xs font-semibold ${theme.title}`}>Chats</span>
                            <button
                                onClick={handleNewChat}
                                className={`flex items-center gap-1 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                                title="New chat"
                            >
                                <i className="fa-solid fa-plus text-[10px]"></i>
                                New
                            </button>
                        </div>

                        {/* Session list */}
                        <div className="flex-auto overflow-y-auto py-1">
                            {chatHistory.length === 0 ? (
                                <div className={`text-center py-8 px-3 text-xs ${theme.label}`}>
                                    <i className="fa-solid fa-comments text-2xl mb-2 opacity-30 block"></i>
                                    No chats yet
                                </div>
                            ) : (
                                chatHistory.map(item => {
                                    const isActive = item.sessionId === sessionState.sessionId;
                                    return (
                                        <div
                                            key={item.sessionId}
                                            onClick={() => handleLoadSession(item)}
                                            className={`
                                                group relative cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-md transition-colors
                                                ${isActive
                                                    ? `${theme.tab_active}`
                                                    : `hover:bg-gray-100 dark:hover:bg-gray-700/50`
                                                }
                                            `}
                                        >
                                            <div className={`text-xs font-medium truncate pr-5 ${theme.title}`}>
                                                {item.title}
                                            </div>
                                            <div className={`text-[10px] mt-0.5 ${theme.label} flex items-center gap-1`}>
                                                {item.dataSourceName && (
                                                    <>
                                                        <i className="fa-solid fa-database text-[9px]"></i>
                                                        <span className="truncate max-w-[80px]">{item.dataSourceName}</span>
                                                        <span className="opacity-40">·</span>
                                                    </>
                                                )}
                                                <span>{formatRelativeTime(item.timestamp)}</span>
                                            </div>
                                            {/* Delete button — shown on hover */}
                                            <button
                                                onClick={(e) => handleDeleteSession(e, item.sessionId)}
                                                className={`
                                                    absolute right-2 top-1/2 -translate-y-1/2
                                                    opacity-0 group-hover:opacity-100 transition-opacity
                                                    w-5 h-5 flex items-center justify-center rounded
                                                    hover:text-red-500 ${theme.label}
                                                `}
                                                title="Delete chat"
                                            >
                                                <i className="fa-solid fa-trash text-[9px]"></i>
                                            </button>
                                        </div>
                                    );
                                })
                            )}
                        </div>
                    </div>
                )}

                {/* Content Area */}
                <div className={`w-1 flex-auto h-full overflow-hidden ${isChat ? '' : 'rounded-r-md'} ${theme.mainContentSection}`}>
                    {getCurrentSectionComponent()}
                </div>
            </div>
        </div>
    );
};

export default DbaGenie;
