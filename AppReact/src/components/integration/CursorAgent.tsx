import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import {
  CursorAgentDoneEvent,
  CursorAgentGateEvent,
  CursorAgentMessage,
  CursorAgentSessionSummary,
  CursorAgentStepEvent,
  CursorAgentWorkspaceFile,
  cursorAgentService,
  getCursorSession,
  getRecentCursorSessions,
  listCursorWorkspaceFiles,
  readCursorWorkspaceFile,
} from '../../webapi/cursoragentsvc';
import { endpoints } from '../../webapi/endpoints';
import { isAdminUserFromContext } from '../../helper/adminPermissionHelper';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: CursorAgentStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
}

const isImagePath = (path: string) => /\.(png|jpe?g|gif|webp|svg)$/i.test(path || '');

const toAppFileUrl = (url?: string) => {
  if (!url) return '';
  if (url.startsWith('http://') || url.startsWith('https://')) return url;
  return `${endpoints.BASE_URL}${url.startsWith('/') ? url : `/${url}`}`;
};

const AssistantBody: React.FC<{ text: string }> = ({ text }) => {
  const srcs = Array.from((text || '').matchAll(/src=["']([^"']+)["']/gi)).map(m => m[1]);
  const cleaned = (text || '').replace(/<img\b[^>]*>/gi, '').trim();
  return (
    <>
      {cleaned}
      {srcs.map(src => (
        <img
          key={src}
          src={src.includes('/FileRepository/') || src.startsWith('/FileRepository')
            ? toAppFileUrl(src.startsWith('/FileRepository') ? src : src.substring(src.indexOf('/FileRepository')))
            : src}
          alt=""
          className="max-w-full mt-2 rounded-[4px]"
        />
      ))}
    </>
  );
};

const CursorAgent: React.FC = () => {
  const { theme } = useTheme();
  const userContext = useSelector((s: RootState) => s.userSession.userContext);
  const isAdmin = isAdminUserFromContext(userContext);

  const [applications, setApplications] = useState<{ id: number; name: string }[]>([]);
  const [dataSources, setDataSources] = useState<{ id: number; name: string }[]>([]);
  const [saasApplicationId, setSaasApplicationId] = useState<number | undefined>();
  const [dataSourceId, setDataSourceId] = useState<number | undefined>();

  const [sessionId, setSessionId] = useState<string | null>(null);
  const [hasAgent, setHasAgent] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pendingGate, setPendingGate] = useState<CursorAgentGateEvent | null>(null);
  const [gateFeedback, setGateFeedback] = useState('');
  const [chatHistory, setChatHistory] = useState<CursorAgentSessionSummary[]>([]);
  const [files, setFiles] = useState<CursorAgentWorkspaceFile[]>([]);
  const [previewPath, setPreviewPath] = useState<string | null>(null);
  const [previewContent, setPreviewContent] = useState('');

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const refreshHistory = useCallback(() => {
    getRecentCursorSessions(30).then(setChatHistory).catch(() => {});
  }, []);

  const refreshFiles = useCallback((sid: string | null) => {
    if (!sid) { setFiles([]); return; }
    listCursorWorkspaceFiles(sid).then(setFiles).catch(() => setFiles([]));
  }, []);

  useEffect(() => {
    adminSvc.retrieveSelectedApplicationPackages(true).then((list: any) => {
      const arr = Array.isArray(list) ? list : (list?.Object ?? list?.ObjectList ?? []);
      const mapped = (arr || []).map((a: any) => ({
        id: Number(a.Id ?? a.id),
        name: String(a.Name ?? a.name ?? a.Id),
      })).filter((a: { id: number }) => a.id > 0);
      setApplications(mapped);
      if (mapped.length) setSaasApplicationId(mapped[0].id);
    }).catch(() => {});
    adminSvc.getDataSourceRegisterList(true).then((list: any[]) => {
      if (list?.length) {
        const mapped = list.map((d: any) => ({ id: d.Id, name: d.Name || d.DataSourceName }));
        setDataSources(mapped);
        setDataSourceId(mapped[0].id);
      }
    }).catch(() => {});
    refreshHistory();
  }, [refreshHistory]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, pendingGate]);

  useEffect(() => () => { cursorAgentService.disconnect(); }, []);

  const updateLastAssistant = useCallback((updater: (msg: ChatMessage) => ChatMessage) => {
    setMessages(prev => {
      const copy = [...prev];
      const last = copy[copy.length - 1];
      if (last?.role === 'assistant') copy[copy.length - 1] = updater(last);
      return copy;
    });
  }, []);

  const makeHandlers = useCallback(() => ({
    onStep: (step: CursorAgentStepEvent) => {
      updateLastAssistant(msg => ({ ...msg, steps: [...msg.steps, step] }));
    },
    onToken: (token: string) => {
      updateLastAssistant(msg => ({ ...msg, streamingContent: (msg.streamingContent || '') + token }));
    },
    onFile: () => refreshFiles(cursorAgentService.currentSessionId),
    onGate: (gate: CursorAgentGateEvent) => setPendingGate(gate),
    onDone: (result: CursorAgentDoneEvent) => {
      try {
        setPendingGate(null);
        updateLastAssistant(msg => ({
          ...msg,
          content: result.FinalResponse || msg.streamingContent || '',
          streamingContent: '',
          isStreaming: false,
        }));
        refreshHistory();
        refreshFiles(cursorAgentService.currentSessionId);
      } finally {
        setIsRunning(false);
      }
    },
    onError: (message: string) => {
      try {
        setPendingGate(null);
        updateLastAssistant(msg => ({ ...msg, content: `Error: ${message}`, isStreaming: false }));
        setError(message);
      } finally {
        setIsRunning(false);
      }
    },
  }), [refreshFiles, refreshHistory, updateLastAssistant]);

  const sendFirstOrFollowUp = useCallback(async (text: string) => {
    if (!text || isRunning) return;
    if (!saasApplicationId) {
      setError('Select an Application first.');
      return;
    }
    setError(null);
    setIsRunning(true);
    setPendingGate(null);
    const userMsg: ChatMessage = { role: 'user', content: text, steps: [], streamingContent: '', isStreaming: false };
    const assistantMsg: ChatMessage = { role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true };
    setMessages(prev => [...prev, userMsg, assistantMsg]);
    try {
      if (!hasAgent || !sessionId) {
        const sid = await cursorAgentService.startSession(
          text, saasApplicationId, dataSourceId, [], makeHandlers());
        setSessionId(sid);
        setHasAgent(true);
        refreshFiles(sid);
      } else {
        await cursorAgentService.followUp(text, makeHandlers());
      }
    } catch (err: any) {
      const errMsg = err?.message ?? 'Unknown error';
      setError(errMsg);
      updateLastAssistant(msg => ({ ...msg, content: `Failed: ${errMsg}`, isStreaming: false }));
      setIsRunning(false);
    }
  }, [dataSourceId, hasAgent, isRunning, makeHandlers, refreshFiles, saasApplicationId, sessionId, updateLastAssistant]);

  const handleSend = useCallback(async () => {
    const text = input.trim();
    if (!text) return;
    setInput('');
    await sendFirstOrFollowUp(text);
  }, [input, sendFirstOrFollowUp]);

  const handleNewChat = useCallback(() => {
    cursorAgentService.disconnect();
    cursorAgentService.currentSessionId = null;
    setSessionId(null);
    setHasAgent(false);
    setMessages([]);
    setPendingGate(null);
    setError(null);
    setFiles([]);
    setPreviewPath(null);
  }, []);

  const handleLoadSession = useCallback(async (summary: CursorAgentSessionSummary) => {
    handleNewChat();
    const session = await getCursorSession(summary.SessionGuid);
    if (!session) return;
    setSessionId(summary.SessionGuid);
    cursorAgentService.currentSessionId = summary.SessionGuid;
    setHasAgent(!!(session.CursorAgentId || summary.CursorAgentId));
    if (session.SaasApplicationId) setSaasApplicationId(session.SaasApplicationId);
    if (session.DataSourceRegisterId) setDataSourceId(session.DataSourceRegisterId);
    const hist = session.ConversationHistory ?? [];
    setMessages(hist.map((m: CursorAgentMessage) => ({
      role: ((m.role ?? m.Role ?? 'assistant') as 'user' | 'assistant'),
      content: m.content ?? m.Content ?? '',
      steps: [],
      streamingContent: '',
      isStreaming: false,
    })));
    refreshFiles(summary.SessionGuid);
  }, [handleNewChat, refreshFiles]);

  const handleResume = useCallback(async () => {
    if (!sessionId || isRunning) return;
    setIsRunning(true);
    const assistantMsg: ChatMessage = { role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true };
    setMessages(prev => [...prev, assistantMsg]);
    try {
      await cursorAgentService.resume(sessionId, 'Continue from where we left off.', makeHandlers());
    } catch (err: any) {
      setError(err?.message ?? 'Resume failed');
      setIsRunning(false);
    }
  }, [isRunning, makeHandlers, sessionId]);

  const handleConfirmGate = useCallback((confirmed: boolean) => {
    if (!pendingGate) return;
    cursorAgentService.confirmGate(pendingGate.GateId, confirmed, confirmed ? undefined : gateFeedback);
    setPendingGate(null);
    setGateFeedback('');
  }, [gateFeedback, pendingGate]);

  const openPreview = useCallback(async (relativePath: string) => {
    if (!sessionId) return;
    try {
      const content = await readCursorWorkspaceFile(sessionId, relativePath);
      setPreviewPath(relativePath);
      setPreviewContent(content);
    } catch (err: any) {
      setError(err?.message ?? 'Failed to read file');
    }
  }, [sessionId]);

  if (!isAdmin) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <div className={`text-sm ${theme.label}`}>Administrator access is required for Cursor Agent.</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">
      <div className="w-full h-full overflow-hidden flex">
        <div className={`w-56 flex-none flex flex-col ${theme.mainContentSection} overflow-hidden rounded-l-md`}>
          <div className="flex items-center justify-between px-3 py-2.5 border-b border-gray-200 dark:border-gray-700">
            <span className={`text-xs font-semibold ${theme.title}`}>Chats</span>
            <button onClick={handleNewChat} disabled={isRunning} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}>
              <i className="fa-solid fa-plus text-[10px] mr-1" />New
            </button>
          </div>
          <div className="h-1 flex-auto overflow-y-auto py-1">
            {chatHistory.length === 0 ? (
              <div className={`text-center py-8 px-3 text-xs ${theme.label}`}>No chats yet</div>
            ) : chatHistory.map(item => (
              <div
                key={item.SessionGuid}
                onClick={() => handleLoadSession(item)}
                className="cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700/50"
              >
                <div className={`text-xs font-medium truncate ${theme.title}`}>{(item.UserRequest ?? '').slice(0, 55)}</div>
                <div className={`text-[10px] ${theme.label}`}>{item.Status}</div>
              </div>
            ))}
          </div>
        </div>

        <div className={`w-1 flex-auto flex flex-col overflow-hidden ${theme.mainContentSection} rounded-r-md`}>
          <div className="flex items-center justify-between px-3 py-2 mb-1 gap-2">
            <div className={`text-md font-semibold ${theme.title}`}>Cursor Agent</div>
            <div className="flex items-center gap-2">
              <select
                className={`h-7 px-2 text-xs border ${theme.inputBox}`}
                value={saasApplicationId ?? ''}
                onChange={e => setSaasApplicationId(Number(e.target.value) || undefined)}
                disabled={isRunning}
              >
                <option value="">Select Application</option>
                {applications.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
              <select
                className={`h-7 px-2 text-xs border ${theme.inputBox}`}
                value={dataSourceId ?? ''}
                onChange={e => setDataSourceId(Number(e.target.value) || undefined)}
                disabled={isRunning}
              >
                <option value="">DataSource (optional)</option>
                {dataSources.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
              </select>
              {hasAgent && (
                <button onClick={handleResume} disabled={isRunning} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                  Resume
                </button>
              )}
              {isRunning && (
                <button onClick={() => cursorAgentService.cancel()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                  Cancel
                </button>
              )}
            </div>
          </div>

          {error && <div className={`px-3 py-1 text-xs ${theme.label}`}>{error}</div>}

          <div className="w-full h-1 flex-auto overflow-hidden flex">
            <div className="w-1 flex-auto overflow-auto px-5 py-5 space-y-3">
              {messages.length === 0 && (
                <div className={`text-xs ${theme.label}`}>
                  Ask Cursor to inspect schema, write an AppConfigPack JSON, or run gated SQL. Source code is read-only.
                </div>
              )}
              {messages.map((msg, i) => (
                <div key={i} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                  <div className={`max-w-[85%] text-xs whitespace-pre-wrap px-3 py-2 rounded-[4px] border ${theme.inputBox}`}>
                    {msg.role === 'assistant' && msg.steps.length > 0 && (
                      <div className={`mb-2 ${theme.label}`}>
                        {msg.steps.slice(-8).map((s, idx) => (
                          <div key={idx} className="truncate"><i className="fa-solid fa-gear mr-1" />{s.Description}</div>
                        ))}
                      </div>
                    )}
                    {msg.role === 'assistant'
                      ? <AssistantBody text={msg.content || msg.streamingContent || (msg.isStreaming ? '…' : '')} />
                      : (msg.content || msg.streamingContent || (msg.isStreaming ? '…' : ''))}
                  </div>
                </div>
              ))}
              {pendingGate && (
                <div className={`border rounded-[4px] px-3 py-2 text-xs ${theme.inputBox}`}>
                  <div className={`font-semibold mb-1 ${theme.title}`}>{pendingGate.Title}</div>
                  <div className={`mb-2 ${theme.label}`}>{pendingGate.Summary}</div>
                  {pendingGate.Sql && <pre className="mb-2 overflow-auto max-h-40">{pendingGate.Sql}</pre>}
                  {pendingGate.RelativePath && <div className="mb-2">Pack: {pendingGate.RelativePath}</div>}
                  <input
                    className={`w-full h-7 px-2 text-xs border mb-2 ${theme.inputBox}`}
                    placeholder="Feedback if you reject"
                    value={gateFeedback}
                    onChange={e => setGateFeedback(e.target.value)}
                    autoComplete="off"
                  />
                  <div className="flex gap-2">
                    <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleConfirmGate(true)}>Confirm</button>
                    <button className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleConfirmGate(false)}>Reject</button>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            <div className="w-64 flex-none border-l border-gray-200 dark:border-gray-700 flex flex-col overflow-hidden">
              <div className={`px-3 py-2 text-xs font-semibold ${theme.title}`}>Workspace</div>
              <div className="h-1 flex-auto overflow-auto px-2">
                {files.filter(f => !f.IsDirectory).map(f => (
                  <button
                    key={f.RelativePath}
                    className={`block w-full text-left text-xs px-2 py-1 rounded-[4px] truncate ${theme.button_default}`}
                    onClick={() => openPreview(f.RelativePath)}
                    title={f.RelativePath}
                  >
                    {f.RelativePath}
                  </button>
                ))}
              </div>
              {previewPath && (
                <div className="h-40 overflow-auto border-t border-gray-200 dark:border-gray-700 px-2 py-1">
                  <div className={`text-[10px] ${theme.label}`}>{previewPath}</div>
                  {isImagePath(previewPath) && files.find(f => f.RelativePath === previewPath)?.PublicUrl ? (
                    <img
                      src={toAppFileUrl(files.find(f => f.RelativePath === previewPath)?.PublicUrl)}
                      alt={previewPath}
                      className="max-w-full mt-1"
                    />
                  ) : (
                    <pre className="text-[10px] whitespace-pre-wrap">{previewContent}</pre>
                  )}
                </div>
              )}
            </div>
          </div>

          <div className="shrink-0 px-3 py-2 border-t border-gray-200 dark:border-gray-700">
            <div className="flex items-end gap-2">
              <textarea
                ref={textareaRef}
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={e => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSend();
                  }
                }}
                disabled={isRunning}
                placeholder="Describe the config, SQL, or question… (Enter to send)"
                rows={2}
                className={`w-1 flex-auto px-3 py-2 text-xs border rounded-[4px] resize-none ${theme.inputBox}`}
              />
              <button
                onClick={handleSend}
                disabled={isRunning || !input.trim()}
                className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_default}`}
              >
                {isRunning ? <><i className="fa-solid fa-circle-notch animate-spin mr-1" />Running…</> : <>Send</>}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CursorAgent;
