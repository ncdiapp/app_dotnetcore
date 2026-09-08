import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentSvc } from '../../webapi/genericAgentSvc';

/**
 * Configuration and Integration Agent UI shell.
 * Layout mirrors DataIntegrationAgent's center chat pane (no left chat list).
 * Runtime uses GenericAgent + AppAgentSkillSet (prompt / tools / MCP / limits from Agent config),
 * not the legacy Cursor-hardcoded DataIntegrationAgent pipeline.
 */

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  isStreaming?: boolean;
  steps?: StepIndicator[];
}

interface StepIndicator {
  type: string;
  toolName?: string;
  description: string;
  isSuccess: boolean;
}

interface PlanEvent {
  PlanSummary: string;
}

interface Props {
  skillKey: string;
}

const WORKSPACE_DEFAULT_PX = 280;
const WORKSPACE_MIN_PX = 200;

const formatElapsed = (seconds: number) => {
  const m = Math.floor(Math.max(0, seconds) / 60);
  const s = Math.max(0, seconds) % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
};

const WorkingStatus: React.FC<{ label: string; onStop?: () => void }> = ({ label, onStop }) => {
  const { theme } = useTheme();
  const [elapsed, setElapsed] = useState(0);
  useEffect(() => {
    const started = Date.now();
    setElapsed(0);
    const id = window.setInterval(() => setElapsed(Math.floor((Date.now() - started) / 1000)), 250);
    return () => window.clearInterval(id);
  }, []);
  return (
    <div className={`flex items-center flex-wrap gap-2 text-xs mb-2 ${theme.label}`}>
      <i className="fa-solid fa-circle-notch animate-spin" />
      <span>{label} {formatElapsed(elapsed)}</span>
      {onStop && (
        <button
          type="button"
          onClick={onStop}
          className={`px-2 h-6 text-xs rounded-[4px] shrink-0 ${theme.button_default}`}
          title="Stop"
        >
          <i className="fa-solid fa-stop mr-1" />Stop
        </button>
      )}
    </div>
  );
};

const ConfigurationAndIntegrationAgentChat: React.FC<Props> = ({ skillKey }) => {
  const { theme, t } = useTheme();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const [workingLabel, setWorkingLabel] = useState('Thinking');
  const [pendingPlan, setPendingPlan] = useState<PlanEvent | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [workspaceWidth] = useState(WORKSPACE_DEFAULT_PX);
  const bottomRef = useRef<HTMLDivElement | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, pendingPlan, isRunning]);

  useEffect(() => {
    return () => { genericAgentSvc.disconnect(); };
  }, []);

  // Reset session when switching agent skill
  useEffect(() => {
    genericAgentSvc.disconnect();
    setMessages([]);
    setSessionId(null);
    setPendingPlan(null);
    setError(null);
    setIsRunning(false);
    setInput('');
  }, [skillKey]);

  const handleStop = () => {
    genericAgentSvc.disconnect();
    setIsRunning(false);
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (last?.role === 'assistant' && last.isStreaming) {
        return [...prev.slice(0, -1), { ...last, isStreaming: false }];
      }
      return prev;
    });
  };

  const handleNewChat = () => {
    if (isRunning) handleStop();
    genericAgentSvc.disconnect();
    setMessages([]);
    setSessionId(null);
    setPendingPlan(null);
    setError(null);
    setInput('');
  };

  const handleSend = async () => {
    const msg = input.trim();
    if (!msg || isRunning || !skillKey) return;
    setInput('');
    setError(null);
    setPendingPlan(null);
    setIsRunning(true);
    setWorkingLabel('Thinking');
    setMessages(prev => [...prev, { role: 'user', content: msg }]);

    try {
      const history = messages
        .filter(m => !m.isStreaming)
        .map(m => ({ role: m.role === 'user' ? 'user' : 'assistant', content: m.content }));

      const sid = await genericAgentSvc.RunAgent(
        { SkillKey: skillKey, UserMessage: msg, SessionId: sessionId ?? undefined, Messages: history },
        {
          onToken: (token) => {
            setWorkingLabel('Writing');
            setMessages(prev => {
              const last = prev[prev.length - 1];
              if (last?.role === 'assistant' && last.isStreaming) {
                return [...prev.slice(0, -1), { ...last, content: last.content + token }];
              }
              return [...prev, { role: 'assistant', content: token, isStreaming: true, steps: [] }];
            });
          },
          onStep: (step) => {
            setWorkingLabel(step.ToolName ? `Tool: ${step.ToolName}` : 'Working');
            setMessages(prev => {
              const last = prev[prev.length - 1];
              const stepItem: StepIndicator = {
                type: step.Type,
                toolName: step.ToolName,
                description: step.Description,
                isSuccess: step.IsSuccess,
              };
              if (last?.role === 'assistant' && last.isStreaming) {
                return [...prev.slice(0, -1), { ...last, steps: [...(last.steps ?? []), stepItem] }];
              }
              return [...prev, { role: 'assistant', content: '', isStreaming: true, steps: [stepItem] }];
            });
          },
          onPlan: (plan) => {
            setWorkingLabel('Waiting for confirmation');
            setPendingPlan(plan);
          },
          onDone: (done) => {
            setMessages(prev => {
              const last = prev[prev.length - 1];
              if (last?.role === 'assistant' && last.isStreaming) {
                return [
                  ...prev.slice(0, -1),
                  { ...last, content: done.FinalResponse || last.content, isStreaming: false },
                ];
              }
              if (done.FinalResponse) {
                return [...prev, { role: 'assistant', content: done.FinalResponse }];
              }
              return prev;
            });
            setIsRunning(false);
            setWorkingLabel('Thinking');
          },
          onError: (m) => {
            setError(m);
            setIsRunning(false);
            setWorkingLabel('Thinking');
          },
        }
      );
      setSessionId(sid);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : String(e));
      setIsRunning(false);
    }
  };

  const handleConfirmPlan = async (confirmed: boolean) => {
    setPendingPlan(null);
    if (sessionId) await genericAgentSvc.ConfirmPlan(sessionId, confirmed);
  };

  const chatTitle = (() => {
    const firstUser = messages.find(m => m.role === 'user')?.content?.trim();
    if (!firstUser) return 'New Chat';
    return firstUser.length > 55 ? `${firstUser.slice(0, 55)}…` : firstUser;
  })();

  const borderCls = t('border_mainContentSection');

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      {error && (
        <div className={`px-3 py-1 text-xs shrink-0 ${theme.mainContentSection} ${theme.label}`}>
          {error}
          <button type="button" className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
        </div>
      )}

      <div className="w-full h-1 flex-auto overflow-hidden flex gap-1 min-h-0">
        {/* Center chat (DIA-like; no left chat list) */}
        <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
          <div className={`flex items-center justify-between px-3 h-10 shrink-0 mb-1 ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold truncate ${theme.title}`} title={chatTitle}>
              {chatTitle}
            </div>
            <div className="flex items-center gap-2 shrink-0 ml-2">
              <span className={`text-[10px] ${theme.label} truncate max-w-[140px]`} title={skillKey}>
                {skillKey}
              </span>
              {isRunning && (
                <button
                  type="button"
                  onClick={handleStop}
                  className={`px-2 h-6 text-xs rounded-[4px] ${theme.button_default}`}
                  title="Stop"
                >
                  <i className="fa-solid fa-stop mr-1" />Stop
                </button>
              )}
              <button
                type="button"
                onClick={handleNewChat}
                disabled={isRunning}
                className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                title="New chat"
              >
                <i className="fa-solid fa-plus" />
              </button>
              <button
                type="button"
                onClick={() => setWorkspaceOpen(v => !v)}
                className={`px-2 h-6 text-xs rounded-[4px] ${theme.button_secondary}`}
                title="Workspace (paths from Agent config — coming soon)"
              >
                <i className="fa-solid fa-folder-open mr-1" />Workspace
              </button>
            </div>
          </div>

          <div className={`h-1 flex-auto overflow-auto ${theme.mainContentSection}`}>
            {messages.length === 0 ? (
              <div className="h-full w-full flex items-center justify-center px-5">
                <div className={`max-w-xl text-center text-sm ${theme.label}`}>
                  Configuration &amp; Integration preview. Prompt, tools, MCP and limits come from this Agent&apos;s settings.
                  Describe what you need below.
                </div>
              </div>
            ) : (
              <div className="w-full max-w-[720px] mx-auto px-5 py-6">
                {messages.map((msg, i) => (
                  <div key={i} className={msg.role === 'user' ? 'mb-4' : 'mb-8'}>
                    {msg.role === 'user' ? (
                      <div className="flex justify-end">
                        <div className={`max-w-[85%] px-3 py-2 rounded-[4px] text-sm whitespace-pre-wrap ${theme.button_default}`}>
                          {msg.content}
                        </div>
                      </div>
                    ) : (
                      <div className={`w-full text-sm leading-relaxed whitespace-pre-wrap ${t('text_title')}`}>
                        {msg.isStreaming && (
                          <WorkingStatus
                            label={pendingPlan ? 'Waiting for confirmation' : workingLabel}
                            onStop={handleStop}
                          />
                        )}
                        {(msg.steps?.length ?? 0) > 0 && (
                          <div className={`mb-3 text-xs ${theme.label}`}>
                            {(msg.steps ?? []).slice(-8).map((s, idx) => (
                              <div key={idx} className="break-words whitespace-pre-wrap mb-1">
                                <i className={`fa-solid ${s.isSuccess ? 'fa-gear' : 'fa-triangle-exclamation'} mr-1`} />
                                {s.toolName && <span className="font-mono font-semibold mr-1">{s.toolName}</span>}
                                {s.description}
                              </div>
                            ))}
                          </div>
                        )}
                        {msg.content}
                        {msg.isStreaming && <span className="animate-pulse ml-1">|</span>}
                      </div>
                    )}
                  </div>
                ))}

                {pendingPlan && (
                  <div className={`border rounded-[4px] px-3 py-2 text-xs mb-5 ${theme.inputBox}`}>
                    <div className={`font-semibold mb-1 ${theme.title}`}>Plan Review</div>
                    <div className={`mb-2 whitespace-pre-wrap ${theme.label}`}>{pendingPlan.PlanSummary}</div>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                        onClick={() => handleConfirmPlan(true)}
                      >
                        Confirm
                      </button>
                      <button
                        type="button"
                        className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                        onClick={() => handleConfirmPlan(false)}
                      >
                        Reject
                      </button>
                    </div>
                  </div>
                )}

                <div ref={bottomRef} />
              </div>
            )}
          </div>

          <div className={`shrink-0 px-5 py-3 mt-1 ${theme.mainContentSection}`}>
            <div className="w-full max-w-3xl mx-auto">
              <div className={`flex items-center gap-2 mb-2 text-xs ${theme.label}`}>
                <span className={`font-semibold ${theme.title}`}>Agent</span>
                <span className="font-mono">{skillKey || '(none)'}</span>
                <span className="opacity-70">· tools / MCP / limits from Agent Setting</span>
              </div>
              <div className={`flex items-end border rounded-[4px] px-2 py-2 ${theme.inputBox}`}>
                <textarea
                  ref={textareaRef}
                  value={input}
                  onChange={e => setInput(e.target.value)}
                  onKeyDown={e => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      void handleSend();
                    }
                  }}
                  disabled={isRunning || !skillKey}
                  placeholder="What do you need help with?"
                  rows={3}
                  className="w-1 flex-auto px-2 py-1 text-xs resize-none border-0 focus:outline-none bg-transparent"
                />
                {isRunning ? (
                  <button
                    type="button"
                    onClick={handleStop}
                    className={`px-3 h-7 ml-2 shrink-0 text-xs rounded-[4px] ${theme.button_default}`}
                    title="Stop"
                  >
                    <i className="fa-solid fa-stop mr-1" />Stop
                  </button>
                ) : (
                  <button
                    type="button"
                    onClick={() => void handleSend()}
                    disabled={!input.trim() || !skillKey}
                    className={`w-8 h-6 ml-2 shrink-0 ${theme.button_default} rounded-[4px] text-xs`}
                    title="Send"
                  >
                    <i className="fa-solid fa-paper-plane" />
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>

        {/* Workspace stub — resource/output paths will come from Agent config */}
        {workspaceOpen && (
          <div
            className={`shrink-0 flex flex-col overflow-hidden min-h-0 border-l ${borderCls}`}
            style={{ width: Math.max(workspaceWidth, WORKSPACE_MIN_PX) }}
          >
            <div className={`flex items-center justify-between px-3 h-10 shrink-0 mb-1 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Workspace</span>
              <button
                type="button"
                onClick={() => setWorkspaceOpen(false)}
                className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                title="Close workspace"
              >
                <i className="fa-solid fa-xmark" />
              </button>
            </div>
            <div className={`h-1 flex-auto overflow-auto px-4 py-6 ${theme.mainContentSection}`}>
              <div className="flex flex-col items-center justify-center text-center min-h-[140px]">
                <div className={`w-12 h-12 rounded-full flex items-center justify-center mb-3 ${theme.inputBox}`}>
                  <i className={`fa-solid fa-folder-open text-lg opacity-50 ${theme.label}`} />
                </div>
                <div className={`text-sm font-medium ${theme.title}`}>No files yet</div>
                <div className={`text-xs mt-1 max-w-[220px] leading-relaxed ${theme.label}`}>
                  Resource / input / output paths will be driven by this Agent&apos;s configuration (not hardcoded Cursor paths).
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ConfigurationAndIntegrationAgentChat;
