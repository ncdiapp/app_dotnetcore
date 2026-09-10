import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import {
    genericAgentSvc,
    GenericAgentChatSummary,
    GenericAgentFile,
} from '../../webapi/genericAgentSvc';

interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    isStreaming?: boolean;
}

interface ToolStep {
    toolName: string;
    args?: string;
    result?: string;
    isSuccess: boolean;
    durationMs?: number;
}

interface TurnActivity {
    turnIndex: number;
    steps: ToolStep[];
    isComplete: boolean;
}

interface PlanEvent {
    PlanSummary: string;
}

interface Props {
    skillKey: string;
    /** Agent Management test RUN — fixed SessionKey = SkillKey:UserId */
    testMode?: boolean;
}

const snippet = (s?: string | null, max = 300) =>
    !s ? '' : s.length > max ? s.slice(0, max) + '…' : s;

const fileName = (p: string) => {
    const i = p.lastIndexOf('/');
    return i >= 0 ? p.slice(i + 1) : p;
};

const parentPath = (p: string) => {
    const i = p.lastIndexOf('/');
    return i >= 0 ? p.slice(0, i) : '';
};

const GenericAgentChat: React.FC<Props> = ({ skillKey, testMode }) => {
    const { theme } = useTheme();
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [turnActivities, setTurnActivities] = useState<TurnActivity[]>([]);
    const [currentTurnIndex, setCurrentTurnIndex] = useState(0);
    const [input, setInput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    const [pendingPlan, setPendingPlan] = useState<PlanEvent | null>(null);
    const [sessionId, setSessionId] = useState<string | null>(null);
    const [chatSessionKey, setChatSessionKey] = useState<string | null>(null);
    const [chatList, setChatList] = useState<GenericAgentChatSummary[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [sidebarOpen, setSidebarOpen] = useState(true);
    const [rightTab, setRightTab] = useState<'tools' | 'files'>('tools');
    const [expandedTools, setExpandedTools] = useState<Set<string>>(new Set());
    const [wsPath, setWsPath] = useState('');
    const [wsFiles, setWsFiles] = useState<GenericAgentFile[]>([]);
    const [wsPreview, setWsPreview] = useState<{ path: string; content: string } | null>(null);
    const bottomRef = useRef<HTMLDivElement | null>(null);
    const pendingCallRef = useRef<Map<string, { args?: string; startedAt: number }>>(new Map());
    const fileInputRef = useRef<HTMLInputElement | null>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, turnActivities]);

    useEffect(() => {
        let cancelled = false;
        const init = async () => {
            setMessages([]);
            setTurnActivities([]);
            setCurrentTurnIndex(0);
            setSessionId(null);
            setError(null);
            setWsPath('');
            setWsPreview(null);

            if (testMode) {
                const fixedKey = await genericAgentSvc.GetFixedSessionKey(skillKey);
                if (cancelled) return;
                setChatSessionKey(fixedKey);
                const prior = await genericAgentSvc.LoadSession(skillKey);
                if (cancelled) return;
                if (prior && prior.length > 0) {
                    setMessages(prior.map(m => ({ role: m.role as 'user' | 'assistant', content: m.content })));
                    setCurrentTurnIndex(prior.filter(m => m.role === 'user').length);
                }
                return;
            }

            const list = await genericAgentSvc.ListChats(skillKey);
            if (cancelled) return;
            const chats = (list || []).filter(c => !c.IsFixedTestSession);
            setChatList(chats);
            if (chats.length > 0) {
                await openChat(chats[0].SessionKey, cancelled);
            } else {
                const created = await genericAgentSvc.CreateChat(skillKey);
                if (cancelled) return;
                if (created?.SessionKey) {
                    setChatSessionKey(created.SessionKey);
                    setChatList([created]);
                }
            }
        };
        init();
        return () => {
            cancelled = true;
            genericAgentSvc.disconnect();
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [skillKey, testMode]);

    const openChat = async (sessionKey: string, cancelled = false) => {
        const detail = await genericAgentSvc.LoadChat(skillKey, sessionKey);
        if (cancelled) return;
        setChatSessionKey(sessionKey);
        setSessionId(null);
        setTurnActivities([]);
        setWsPath('');
        setWsPreview(null);
        if (detail?.Messages?.length) {
            setMessages(detail.Messages.map(m => ({ role: m.role as 'user' | 'assistant', content: String(m.content ?? '') })));
            setCurrentTurnIndex(detail.Messages.filter(m => m.role === 'user').length);
        } else {
            setMessages([]);
            setCurrentTurnIndex(0);
        }
    };

    const refreshChatList = async () => {
        if (testMode) return;
        const list = await genericAgentSvc.ListChats(skillKey);
        setChatList((list || []).filter(c => !c.IsFixedTestSession));
    };

    const handleNewChat = async () => {
        if (testMode || isRunning) return;
        const created = await genericAgentSvc.CreateChat(skillKey);
        if (!created?.SessionKey) {
            setError('Failed to create chat');
            return;
        }
        setMessages([]);
        setTurnActivities([]);
        setCurrentTurnIndex(0);
        setSessionId(null);
        setChatSessionKey(created.SessionKey);
        setWsPath('');
        setWsPreview(null);
        setRightTab('tools');
        await refreshChatList();
    };

    const refreshFiles = async (folder = wsPath) => {
        if (!chatSessionKey) {
            setWsFiles([]);
            return;
        }
        const files = await genericAgentSvc.ListAgentFiles(skillKey, chatSessionKey, folder);
        setWsFiles(files);
    };

    useEffect(() => {
        if (rightTab === 'files') refreshFiles(wsPath);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [rightTab, chatSessionKey, skillKey, wsPath]);

    const findLastIdx = <T,>(arr: T[], pred: (item: T) => boolean): number => {
        for (let i = arr.length - 1; i >= 0; i--) if (pred(arr[i])) return i;
        return -1;
    };

    const addStep = (turnIdx: number, patch: Partial<ToolStep> & { toolName: string }) => {
        setTurnActivities(prev => {
            const existing = prev.find(t => t.turnIndex === turnIdx);
            if (existing) {
                const steps = [...existing.steps];
                const idx = findLastIdx(steps, s => s.toolName === patch.toolName && s.result === undefined);
                if (idx >= 0 && (patch.result !== undefined || patch.durationMs !== undefined)) {
                    steps[idx] = { ...steps[idx], ...patch };
                } else {
                    steps.push({ isSuccess: true, ...patch });
                }
                return prev.map(t => (t.turnIndex === turnIdx ? { ...t, steps } : t));
            }
            return [...prev, { turnIndex: turnIdx, steps: [{ isSuccess: true, ...patch }], isComplete: false }];
        });
    };

    const handleSend = async () => {
        const msg = input.trim();
        if (!msg || isRunning) return;
        if (!chatSessionKey) {
            setError(testMode ? 'Session key not ready.' : 'No chat session. Click New Chat first.');
            return;
        }
        setInput('');
        setError(null);
        setIsRunning(true);
        const turnIdx = currentTurnIndex;
        pendingCallRef.current.clear();
        setMessages(prev => [...prev, { role: 'user', content: msg }]);

        try {
            const history = messages
                .filter(m => !m.isStreaming)
                .map(m => ({ role: m.role === 'user' ? 'user' : 'assistant', content: m.content }));

            const sid = await genericAgentSvc.RunAgent(
                {
                    SkillKey: skillKey,
                    UserMessage: msg,
                    SessionId: sessionId ?? undefined,
                    ChatSessionKey: chatSessionKey,
                    Messages: history,
                },
                {
                    onToken: token => {
                        setMessages(prev => {
                            const last = prev[prev.length - 1];
                            if (last?.role === 'assistant' && last.isStreaming) {
                                return [...prev.slice(0, -1), { ...last, content: last.content + token }];
                            }
                            return [...prev, { role: 'assistant', content: token, isStreaming: true }];
                        });
                    },
                    onStep: step => {
                        const s = step as {
                            Type: string;
                            ToolName?: string;
                            Description: string;
                            IsSuccess: boolean;
                            Details?: string;
                        };
                        if (!s.ToolName) return;
                        if (s.Type === 'tool_call') {
                            pendingCallRef.current.set(s.ToolName, { args: s.Details, startedAt: Date.now() });
                            addStep(turnIdx, { toolName: s.ToolName, args: s.Details, isSuccess: true });
                        } else if (s.Type === 'tool_result') {
                            const pending = pendingCallRef.current.get(s.ToolName);
                            const durationMs = pending ? Date.now() - pending.startedAt : undefined;
                            pendingCallRef.current.delete(s.ToolName);
                            addStep(turnIdx, {
                                toolName: s.ToolName,
                                result: s.Details,
                                isSuccess: s.IsSuccess,
                                durationMs,
                            });
                            if (s.ToolName.startsWith('file_')) {
                                setRightTab('files');
                                refreshFiles(wsPath);
                            }
                        }
                    },
                    onPlan: plan => setPendingPlan(plan),
                    onDone: done => {
                        setMessages(prev => {
                            const last = prev[prev.length - 1];
                            if (last?.role === 'assistant' && last.isStreaming) {
                                return [
                                    ...prev.slice(0, -1),
                                    { ...last, content: done.FinalResponse || last.content, isStreaming: false },
                                ];
                            }
                            if (done.FinalResponse) return [...prev, { role: 'assistant', content: done.FinalResponse }];
                            return prev;
                        });
                        setTurnActivities(prev =>
                            prev.map(t => (t.turnIndex === turnIdx ? { ...t, isComplete: true } : t))
                        );
                        setCurrentTurnIndex(i => i + 1);
                        setIsRunning(false);
                        if (!testMode) refreshChatList();
                    },
                    onError: m => {
                        setError(m);
                        setIsRunning(false);
                    },
                }
            );
            setSessionId(sid);
            if (genericAgentSvc.currentChatSessionKey) {
                setChatSessionKey(genericAgentSvc.currentChatSessionKey);
            }
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
            setIsRunning(false);
        }
    };

    const handleConfirmPlan = async (confirmed: boolean) => {
        setPendingPlan(null);
        if (sessionId) await genericAgentSvc.ConfirmPlan(sessionId, confirmed);
    };

    const toggleExpand = (key: string) =>
        setExpandedTools(prev => {
            const n = new Set(prev);
            n.has(key) ? n.delete(key) : n.add(key);
            return n;
        });

    const handleClear = async () => {
        setMessages([]);
        setTurnActivities([]);
        setCurrentTurnIndex(0);
        setSessionId(null);
        setError(null);
        if (testMode) {
            await genericAgentSvc.ClearSession(skillKey);
        } else if (chatSessionKey) {
            await genericAgentSvc.DeleteChat(skillKey, chatSessionKey);
            await handleNewChat();
        }
    };

    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const allActivities = turnActivities;
    const hasAnyTools = allActivities.some(t => t.steps.length > 0);

    return (
        <div className="w-full h-full flex flex-row overflow-hidden">
            <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
                {!testMode && (
                    <div className={`flex items-center gap-2 px-3 py-2 border-b border-gray-200 ${theme.mainContentSection}`}>
                        <button className={btn} onClick={handleNewChat} disabled={isRunning} title="New Chat">
                            <i className="fa-solid fa-plus mr-1" />
                            New Chat
                        </button>
                        <select
                            className={`flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                            value={chatSessionKey || ''}
                            disabled={isRunning || chatList.length === 0}
                            onChange={e => {
                                const v = e.target.value;
                                if (v) openChat(v);
                            }}
                        >
                            {chatList.length === 0 && <option value="">No chats</option>}
                            {chatList.map(c => (
                                <option key={c.SessionKey} value={c.SessionKey}>
                                    {c.Title?.trim() || c.SessionKey.slice(0, 8) + '…'}
                                </option>
                            ))}
                        </select>
                    </div>
                )}

                <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-2">
                    {messages.map((m, i) => (
                        <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                            <div
                                className={`max-w-2xl px-3 py-2 rounded-lg text-xs whitespace-pre-wrap ${
                                    m.role === 'user' ? `${theme.button_default} ml-8` : `${theme.mainContentSection} mr-8`
                                }`}
                            >
                                {m.content}
                                {m.isStreaming && <span className="animate-pulse ml-1">|</span>}
                            </div>
                        </div>
                    ))}

                    {isRunning && (
                        <div className={`flex items-center gap-2 mx-2 text-xs ${theme.label}`}>
                            <i className="fa-solid fa-spinner fa-spin" />
                            <span>
                                {turnActivities.find(t => t.turnIndex === currentTurnIndex)?.steps.length
                                    ? `Running tools…`
                                    : 'Thinking…'}
                            </span>
                        </div>
                    )}

                    {pendingPlan && (
                        <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                            <div className={`text-xs font-semibold ${theme.title}`}>Plan Review</div>
                            <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{pendingPlan.PlanSummary}</div>
                            <div className="flex gap-2">
                                <button className={btn} onClick={() => handleConfirmPlan(true)}>
                                    <i className="fa-solid fa-check mr-1" />
                                    Approve
                                </button>
                                <button className={btn} onClick={() => handleConfirmPlan(false)}>
                                    <i className="fa-solid fa-xmark mr-1" />
                                    Reject
                                </button>
                            </div>
                        </div>
                    )}

                    {error && (
                        <div className="mx-2 px-3 py-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded">
                            {error}
                            <button className="ml-2 font-bold" onClick={() => setError(null)}>
                                x
                            </button>
                        </div>
                    )}
                    <div ref={bottomRef} />
                </div>

                <div className={`flex items-end gap-2 px-3 py-2 border-t border-gray-200 ${theme.mainContentSection}`}>
                    <textarea
                        className={`w-1 flex-auto px-2 py-1 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none resize-none`}
                        rows={2}
                        value={input}
                        placeholder={`Message ${skillKey || 'agent'}…`}
                        onChange={e => setInput(e.target.value)}
                        onKeyDown={e => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                                e.preventDefault();
                                handleSend();
                            }
                        }}
                        disabled={isRunning}
                    />
                    <button className={btn} onClick={handleSend} disabled={isRunning || !input.trim()}>
                        {isRunning ? <i className="fa-solid fa-spinner fa-spin" /> : <i className="fa-solid fa-paper-plane" />}
                    </button>
                    {messages.length > 0 && (
                        <button className={btn} onClick={handleClear} title={testMode ? 'Clear conversation' : 'Delete chat'}>
                            <i className="fa-solid fa-rotate-left" />
                        </button>
                    )}
                    <button className={`${btn} ml-auto`} onClick={() => setSidebarOpen(o => !o)} title="Toggle panel">
                        <i className="fa-solid fa-timeline" />
                    </button>
                </div>
            </div>

            {sidebarOpen && (
                <div className={`w-80 flex flex-col overflow-hidden border-l border-gray-200 ${theme.mainContentSection} shrink-0`}>
                    <div className="flex items-center border-b border-gray-200">
                        <button
                            className={`px-3 py-2 text-xs font-semibold ${rightTab === 'tools' ? theme.title : 'opacity-50'}`}
                            onClick={() => setRightTab('tools')}
                        >
                            <i className="fa-solid fa-timeline mr-1 text-blue-500" />
                            Tools
                        </button>
                        <button
                            className={`px-3 py-2 text-xs font-semibold ${rightTab === 'files' ? theme.title : 'opacity-50'}`}
                            onClick={() => setRightTab('files')}
                        >
                            <i className="fa-solid fa-folder mr-1" />
                            Files
                        </button>
                        <button className="ml-auto px-3 text-xs opacity-50 hover:opacity-100" onClick={() => setSidebarOpen(false)}>
                            <i className="fa-solid fa-xmark" />
                        </button>
                    </div>

                    {rightTab === 'tools' && (
                        <div className="w-full h-1 flex-auto overflow-y-auto flex flex-col gap-0 p-2">
                            {!hasAnyTools && !isRunning && messages.length > 0 && (
                                <div className={`p-3 rounded text-xs ${theme.label} flex flex-col gap-1`}>
                                    <div className="flex items-center gap-2 opacity-60">
                                        <i className="fa-solid fa-circle-info" />
                                        <span className="font-semibold">No tools called</span>
                                    </div>
                                    <p className="opacity-50 leading-relaxed">
                                        The agent replied without invoking tools. Check tool descriptions if one should have fired.
                                    </p>
                                </div>
                            )}

                            {allActivities.map(turn => (
                                <div key={turn.turnIndex} className="mb-3">
                                    {allActivities.length > 1 && (
                                        <div className={`text-xs opacity-40 px-1 mb-1 ${theme.label}`}>Turn {turn.turnIndex + 1}</div>
                                    )}
                                    {turn.steps.length === 0 && turn.isComplete && (
                                        <div className={`px-2 py-1.5 text-xs rounded flex items-center gap-2 opacity-50 ${theme.label}`}>
                                            <i className="fa-solid fa-circle-minus" />
                                            No tools called
                                        </div>
                                    )}
                                    {(() => {
                                        const paired: Array<{ call?: ToolStep; result?: ToolStep; key: string }> = [];
                                        turn.steps.forEach((s, idx) => {
                                            if (s.result !== undefined) {
                                                const callIdx = findLastIdx(paired, p => p.call?.toolName === s.toolName && !p.result);
                                                if (callIdx >= 0) paired[callIdx] = { ...paired[callIdx], result: s };
                                                else paired.push({ result: s, key: `${s.toolName}-${idx}` });
                                            } else {
                                                paired.push({ call: s, key: `${s.toolName}-${idx}` });
                                            }
                                        });
                                        return paired.map((pair, pi) => {
                                            const toolName = pair.call?.toolName ?? pair.result?.toolName ?? '';
                                            const isSuccess = pair.result ? pair.result.isSuccess : true;
                                            const hasResult = pair.result !== undefined;
                                            const expandKey = `${turn.turnIndex}-${pi}`;
                                            const isExpanded = expandedTools.has(expandKey);
                                            const hasDetails = !!(pair.call?.args || pair.result?.result);
                                            return (
                                                <div
                                                    key={pair.key}
                                                    className={`mb-1 rounded border ${isSuccess ? 'border-green-200' : 'border-red-200'} overflow-hidden`}
                                                >
                                                    <button
                                                        className={`w-full flex items-center gap-2 px-2 py-1.5 text-xs text-left ${
                                                            isSuccess ? 'bg-green-50 hover:bg-green-100' : 'bg-red-50 hover:bg-red-100'
                                                        }`}
                                                        onClick={() => hasDetails && toggleExpand(expandKey)}
                                                    >
                                                        <i
                                                            className={`fa-solid ${
                                                                !hasResult
                                                                    ? 'fa-spinner fa-spin text-blue-400'
                                                                    : isSuccess
                                                                      ? 'fa-circle-check text-green-500'
                                                                      : 'fa-circle-xmark text-red-500'
                                                            }`}
                                                        />
                                                        <span className="font-mono font-semibold w-1 flex-auto">{toolName}</span>
                                                        {pair.result?.durationMs != null && (
                                                            <span className="opacity-40">{pair.result.durationMs}ms</span>
                                                        )}
                                                        {hasDetails && (
                                                            <i className={`fa-solid fa-chevron-${isExpanded ? 'up' : 'down'} opacity-40`} />
                                                        )}
                                                    </button>
                                                    {isExpanded && (
                                                        <div className="px-2 py-1.5 flex flex-col gap-1.5">
                                                            {pair.call?.args && (
                                                                <div>
                                                                    <div className="text-xs opacity-40 mb-0.5">Args</div>
                                                                    <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${theme.label}`}>
                                                                        {snippet(pair.call.args, 400)}
                                                                    </pre>
                                                                </div>
                                                            )}
                                                            {pair.result?.result && (
                                                                <div>
                                                                    <div className="text-xs opacity-40 mb-0.5">
                                                                        {isSuccess ? 'Result' : 'Error'}
                                                                    </div>
                                                                    <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${theme.label}`}>
                                                                        {snippet(pair.result.result, 400)}
                                                                    </pre>
                                                                </div>
                                                            )}
                                                        </div>
                                                    )}
                                                </div>
                                            );
                                        });
                                    })()}
                                </div>
                            ))}
                        </div>
                    )}

                    {rightTab === 'files' && (
                        <div className="w-full h-1 flex-auto overflow-hidden flex flex-col">
                            <div className="flex items-center gap-1 px-2 py-1.5 border-b border-gray-200 flex-wrap">
                                <button
                                    className={btn}
                                    disabled={!chatSessionKey}
                                    onClick={() => setWsPath(p => parentPath(p))}
                                    title="Up"
                                >
                                    <i className="fa-solid fa-arrow-up" />
                                </button>
                                <button className={btn} disabled={!chatSessionKey} onClick={() => refreshFiles(wsPath)} title="Refresh">
                                    <i className="fa-solid fa-rotate" />
                                </button>
                                <button
                                    className={btn}
                                    disabled={!chatSessionKey}
                                    onClick={async () => {
                                        const name = window.prompt('Folder name');
                                        if (!name?.trim() || !chatSessionKey) return;
                                        const rel = wsPath ? `${wsPath}/${name.trim()}` : name.trim();
                                        await genericAgentSvc.MkdirAgentFile(skillKey, chatSessionKey, rel);
                                        refreshFiles(wsPath);
                                    }}
                                    title="New folder"
                                >
                                    <i className="fa-solid fa-folder-plus" />
                                </button>
                                <button className={btn} disabled={!chatSessionKey} onClick={() => fileInputRef.current?.click()} title="Upload">
                                    <i className="fa-solid fa-upload" />
                                </button>
                                <input
                                    ref={fileInputRef}
                                    type="file"
                                    className="hidden"
                                    onChange={async e => {
                                        const f = e.target.files?.[0];
                                        e.target.value = '';
                                        if (!f || !chatSessionKey) return;
                                        await genericAgentSvc.UploadAgentFile(skillKey, chatSessionKey, wsPath, f);
                                        refreshFiles(wsPath);
                                    }}
                                />
                                <span className={`text-xs opacity-50 truncate w-1 flex-auto ${theme.label}`}>
                                    {wsPath || '/'}
                                </span>
                            </div>

                            {!chatSessionKey && (
                                <div className={`p-3 text-xs opacity-50 ${theme.label}`}>
                                    No chat session selected.
                                </div>
                            )}

                            <div className="w-full h-1 flex-auto overflow-y-auto p-1">
                                {wsFiles.map(f => (
                                    <div
                                        key={f.RelativePath}
                                        className="flex items-center gap-1 px-2 py-1 text-xs rounded hover:bg-black/5"
                                    >
                                        <button
                                            className={`text-left w-1 flex-auto truncate ${theme.label}`}
                                            onClick={async () => {
                                                if (f.IsDirectory) {
                                                    setWsPath(f.RelativePath);
                                                    setWsPreview(null);
                                                    return;
                                                }
                                                if (!chatSessionKey) return;
                                                const content = await genericAgentSvc.ReadAgentFile(
                                                    skillKey,
                                                    chatSessionKey,
                                                    f.RelativePath
                                                );
                                                if (content) setWsPreview({ path: f.RelativePath, content: content.Content });
                                            }}
                                        >
                                            <i
                                                className={`fa-solid ${f.IsDirectory ? 'fa-folder text-amber-500' : 'fa-file'} mr-1`}
                                            />
                                            {fileName(f.RelativePath)}
                                        </button>
                                        {!f.IsDirectory && chatSessionKey && (
                                            <button
                                                className="opacity-50 hover:opacity-100 px-1"
                                                title="Download"
                                                onClick={() =>
                                                    genericAgentSvc.DownloadAgentFile(skillKey, chatSessionKey, f.RelativePath)
                                                }
                                            >
                                                <i className="fa-solid fa-download" />
                                            </button>
                                        )}
                                        <button
                                            className="opacity-50 hover:opacity-100 px-1"
                                            title="Rename"
                                            disabled={!chatSessionKey}
                                            onClick={async () => {
                                                if (!chatSessionKey) return;
                                                const name = window.prompt('New name', fileName(f.RelativePath));
                                                if (!name?.trim()) return;
                                                const np = wsPath ? `${wsPath}/${name.trim()}` : name.trim();
                                                await genericAgentSvc.RenameAgentFile(skillKey, chatSessionKey, f.RelativePath, np);
                                                refreshFiles(wsPath);
                                            }}
                                        >
                                            <i className="fa-solid fa-pencil" />
                                        </button>
                                        <button
                                            className="opacity-50 hover:opacity-100 px-1"
                                            title="Delete"
                                            disabled={!chatSessionKey}
                                            onClick={async () => {
                                                if (!chatSessionKey || !window.confirm(`Delete ${fileName(f.RelativePath)}?`)) return;
                                                await genericAgentSvc.DeleteAgentFile(skillKey, chatSessionKey, f.RelativePath);
                                                if (wsPreview?.path === f.RelativePath) setWsPreview(null);
                                                refreshFiles(wsPath);
                                            }}
                                        >
                                            <i className="fa-solid fa-trash" />
                                        </button>
                                    </div>
                                ))}
                            </div>

                            {wsPreview && (
                                <div className="border-t border-gray-200 max-h-40 overflow-auto p-2">
                                    <div className={`text-xs font-semibold mb-1 ${theme.title}`}>{wsPreview.path}</div>
                                    <pre className={`text-xs whitespace-pre-wrap break-all ${theme.label}`}>
                                        {snippet(wsPreview.content, 2000)}
                                    </pre>
                                </div>
                            )}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default GenericAgentChat;
