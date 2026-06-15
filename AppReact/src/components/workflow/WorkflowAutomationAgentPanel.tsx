import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import {
  AgentMessage,
  AgentStepEvent,
} from '../../webapi/appbuilderagentsvc';
import {
  WfAgentDoneEvent,
  WfAgentPlanEvent,
  workflowAutomationAgentService,
} from '../../webapi/workflowautomationagentsvc';
import appHelper from '../../helper/appHelper';

// ─────────────────────────────────────────────────────────────────────────────
// Local types
// ─────────────────────────────────────────────────────────────────────────────

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: AgentStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
  pendingPlan?: WfAgentPlanEvent;  // set when a plan gate is waiting for approval
}

// ─────────────────────────────────────────────────────────────────────────────
// StepRow — expandable tool-call / result row
// ─────────────────────────────────────────────────────────────────────────────

const ToolIcon: Record<string, string> = {
  thinking:    'fa-solid fa-brain',
  tool_call:   'fa-solid fa-gear',
  tool_result: 'fa-solid fa-circle-check',
  error:       'fa-solid fa-circle-exclamation',
};

const StepRow: React.FC<{ step: AgentStepEvent }> = ({ step }) => {
  const { theme, t } = useTheme();
  const [expanded, setExpanded] = useState(false);
  const icon = ToolIcon[step.Type] ?? 'fa-solid fa-circle-dot';
  const isThinking = step.Type === 'thinking';
  const isToolCall = step.Type === 'tool_call';
  const isToolResult = step.Type === 'tool_result';
  const iconColor = isThinking
    ? 'text-blue-400 animate-pulse'
    : step.IsSuccess ? 'text-green-500' : 'text-red-500';

  return (
    <div className="flex items-start gap-2 py-1 text-xs">
      <i className={`${icon} ${iconColor} mt-0.5 w-3.5 shrink-0`} />
      <div className="min-w-0 w-1 flex-auto">
        <div
          className={`flex items-baseline gap-1.5 flex-wrap ${step.Details ? 'cursor-pointer' : 'cursor-default'}`}
          onClick={() => step.Details && setExpanded(e => !e)}
        >
          {isThinking && <span className="whitespace-nowrap font-semibold text-blue-400">Thinking</span>}
          {isToolCall && <span className="whitespace-nowrap font-semibold text-yellow-500 font-mono">{step.ToolName}</span>}
          {isToolResult && (
            <span className={`whitespace-nowrap font-semibold ${step.IsSuccess ? 'text-green-500' : 'text-red-500'}`}>
              {step.IsSuccess ? 'Done' : 'Failed'}
            </span>
          )}
          {step.Description && <span className={`${theme.label} leading-tight`}>{step.Description}</span>}
          {step.Details && (
            <i className={`fa-solid ${expanded ? 'fa-chevron-up' : 'fa-chevron-down'} text-[9px] opacity-50 ml-auto`} />
          )}
        </div>
        {expanded && step.Details && (
          <pre className={`mt-1.5 p-2 rounded text-[10px] overflow-auto max-h-48 ${theme.inputBox} whitespace-pre-wrap`}>
            {step.Details}
          </pre>
        )}
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// WfPlanApprovalCard — inline confirmation before workflow writes
// ─────────────────────────────────────────────────────────────────────────────

const WfPlanApprovalCard: React.FC<{
  plan:      WfAgentPlanEvent;
  onApprove: () => void;
  onReject:  (feedback: string) => void;
}> = ({ plan, onApprove, onReject }) => {
  const { theme, t } = useTheme();
  const [feedback, setFeedback] = useState('');
  const [showFeedback, setShowFeedback] = useState(false);

  return (
    <div className="mt-1 rounded-lg border-2 border-amber-400 dark:border-amber-500 overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-900/30 border-b border-amber-300 dark:border-amber-600">
        <i className="fa-solid fa-list-check text-amber-500" />
        <span className="text-xs font-semibold text-amber-700 dark:text-amber-300">
          Review Workflow Changes — Approval Required
        </span>
      </div>

      {/* Body */}
      <div className={`px-3 py-2.5 space-y-2 ${theme.mainContentSection}`}>
        <p className={`text-xs leading-relaxed ${theme.label}`}>{plan.PlanSummary}</p>

        {plan.TasksToCreate?.length > 0 && (
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-plus mr-1 text-green-500" />
              Create ({plan.TasksToCreate.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.TasksToCreate.map((t, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300">
                  {t}
                </span>
              ))}
            </div>
          </div>
        )}

        {plan.TasksToModify?.length > 0 && (
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-pencil mr-1 text-blue-400" />
              Modify ({plan.TasksToModify.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.TasksToModify.map((t, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300">
                  {t}
                </span>
              ))}
            </div>
          </div>
        )}

        {plan.TasksToDelete?.length > 0 && (
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-trash mr-1 text-red-400" />
              Delete ({plan.TasksToDelete.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.TasksToDelete.map((t, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300">
                  {t}
                </span>
              ))}
            </div>
          </div>
        )}

        {showFeedback && (
          <textarea
            className={`w-full h-16 px-2 py-1 text-xs border rounded resize-none ${theme.inputBox}`}
            placeholder="Tell the agent what to change…"
            value={feedback}
            onChange={e => setFeedback(e.target.value)}
            autoFocus
          />
        )}
      </div>

      {/* Buttons */}
      <div className="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-900/20 border-t border-amber-300 dark:border-amber-700 flex-wrap">
        {!showFeedback ? (
          <>
            <button
              onClick={onApprove}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-green-600 hover:bg-green-700 text-white font-semibold"
            >
              <i className="fa-solid fa-circle-check" />
              Approve
            </button>
            <button
              onClick={() => setShowFeedback(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-500 hover:bg-red-600 text-white"
            >
              <i className="fa-solid fa-circle-xmark" />
              Reject
            </button>
            <span className={`text-[10px] ${theme.label} opacity-50 ml-auto`}>
              Agent is waiting…
            </span>
          </>
        ) : (
          <>
            <button
              onClick={() => { onReject(feedback); setShowFeedback(false); setFeedback(''); }}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-500 hover:bg-red-600 text-white"
            >
              <i className="fa-solid fa-paper-plane" />
              Send feedback
            </button>
            <button
              onClick={() => { setShowFeedback(false); setFeedback(''); }}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            >
              Cancel
            </button>
          </>
        )}
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// MessageBubble — renders one chat message with its steps
// ─────────────────────────────────────────────────────────────────────────────

const MessageBubble: React.FC<{
  msg:       ChatMessage;
  onApprove: () => void;
  onReject:  (feedback: string) => void;
}> = ({ msg, onApprove, onReject }) => {
  const { theme, t } = useTheme();

  if (msg.role === 'user') {
    return (
      <div className="flex justify-end mb-3">
        <div className={`max-w-[85%] px-3 py-2 rounded-lg text-xs ${theme.mainContentSection} border ${t('border_mainContentSection')} text-right`}>
          {msg.content}
        </div>
      </div>
    );
  }

  const displayContent = msg.isStreaming
    ? msg.streamingContent
    : msg.content;

  return (
    <div className="mb-3">
      {/* Steps */}
      {msg.steps.length > 0 && (
        <div className={`mb-1.5 px-2 py-1.5 rounded-lg ${theme.mainContentSection} border ${t('border_mainContentSection')}`}>
          {msg.steps.map((step, i) => <StepRow key={i} step={step} />)}
        </div>
      )}

      {/* Text content */}
      {displayContent && (
        <div className={`px-3 py-2 rounded-lg text-xs leading-relaxed ${theme.mainContentSection} border ${t('border_mainContentSection')} whitespace-pre-wrap`}>
          {displayContent}
          {msg.isStreaming && <span className="animate-pulse">▌</span>}
        </div>
      )}

      {/* Plan approval card (blocking gate) */}
      {msg.pendingPlan && (
        <WfPlanApprovalCard
          plan={msg.pendingPlan}
          onApprove={onApprove}
          onReject={onReject}
        />
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// WorkflowAutomationAgentPanel — collapsible right-side chat panel
// ─────────────────────────────────────────────────────────────────────────────

interface Props {
  transactionId:     number;
  onWorkflowChanged: () => void;
  onClose:           () => void;
  hideHeader?:       boolean;
}

export const WorkflowAutomationAgentPanel: React.FC<Props> = ({
  transactionId,
  onWorkflowChanged,
  onClose,
  hideHeader = false,
}) => {
  const { theme, t } = useTheme();

  const [messages,    setMessages]    = useState<ChatMessage[]>([]);
  const [input,       setInput]       = useState('');
  const [isRunning,   setIsRunning]   = useState(false);
  const [convHistory, setConvHistory] = useState<AgentMessage[]>([]);

  const scrollRef = useRef<HTMLDivElement>(null);
  const inputRef  = useRef<HTMLTextAreaElement>(null);

  const scrollToBottom = useCallback(() => {
    requestAnimationFrame(() => {
      if (scrollRef.current)
        scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    });
  }, []);

  useEffect(() => { scrollToBottom(); }, [messages, scrollToBottom]);

  // ── Message helpers ─────────────────────────────────────────────────────────

  const addUserMessage = useCallback((text: string) => {
    setMessages(prev => [...prev, {
      role: 'user', content: text,
      steps: [], streamingContent: '', isStreaming: false,
    }]);
  }, []);

  const addAssistantMessage = useCallback((): number => {
    setMessages(prev => {
      const newMsg: ChatMessage = {
        role: 'assistant', content: '',
        steps: [], streamingContent: '', isStreaming: true,
      };
      return [...prev, newMsg];
    });
    // return the new index
    return -1; // will use lastAssistantIdx ref
  }, []);

  const lastAssistantIdxRef = useRef<number>(-1);

  const getOrCreateAssistantMsg = useCallback(() => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (last?.role === 'assistant') {
        lastAssistantIdxRef.current = prev.length - 1;
        return prev;
      }
      const newMsg: ChatMessage = {
        role: 'assistant', content: '',
        steps: [], streamingContent: '', isStreaming: true,
      };
      lastAssistantIdxRef.current = prev.length;
      return [...prev, newMsg];
    });
  }, []);

  const appendToken = useCallback((token: string) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') return prev;
      const updated = { ...last, streamingContent: last.streamingContent + token };
      return [...prev.slice(0, -1), updated];
    });
  }, []);

  const addStep = useCallback((step: AgentStepEvent) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') {
        const newMsg: ChatMessage = {
          role: 'assistant', content: '',
          steps: [step], streamingContent: '', isStreaming: true,
        };
        return [...prev, newMsg];
      }
      return [...prev.slice(0, -1), { ...last, steps: [...last.steps, step] }];
    });
  }, []);

  const showPlan = useCallback((plan: WfAgentPlanEvent) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') {
        return [...prev, {
          role: 'assistant', content: '',
          steps: [], streamingContent: '', isStreaming: false,
          pendingPlan: plan,
        }];
      }
      return [...prev.slice(0, -1), { ...last, isStreaming: false, pendingPlan: plan }];
    });
  }, []);

  const finishAssistantMsg = useCallback((finalContent: string) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') return prev;
      return [...prev.slice(0, -1), {
        ...last,
        content: finalContent || last.streamingContent,
        streamingContent: '',
        isStreaming: false,
        pendingPlan: undefined,
      }];
    });
  }, []);

  // ── Plan gate handlers ──────────────────────────────────────────────────────

  const handleApprove = useCallback(async () => {
    // Clear the pendingPlan from the last assistant message
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last) return prev;
      return [...prev.slice(0, -1), { ...last, pendingPlan: undefined }];
    });
    await workflowAutomationAgentService.confirmPlan(true);
  }, []);

  const handleReject = useCallback(async (feedback: string) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last) return prev;
      return [...prev.slice(0, -1), { ...last, pendingPlan: undefined }];
    });
    await workflowAutomationAgentService.confirmPlan(false, feedback);
  }, []);

  // ── Send message ────────────────────────────────────────────────────────────

  const sendMessage = useCallback(async () => {
    const text = input.trim();
    if (!text || isRunning || !transactionId) return;

    setInput('');
    setIsRunning(true);
    addUserMessage(text);

    try {
      await workflowAutomationAgentService.startSession(
        { userMessage: text, transactionId, conversationHistory: convHistory },
        {
          onStep:  step  => { getOrCreateAssistantMsg(); addStep(step); },
          onToken: token => { getOrCreateAssistantMsg(); appendToken(token); },
          onPlan:  plan  => { getOrCreateAssistantMsg(); showPlan(plan); },
          onDone:  (done: WfAgentDoneEvent) => {
            finishAssistantMsg(done.FinalResponse);
            setConvHistory(done.UpdatedHistory ?? []);
            setIsRunning(false);
            if ((done.CreatedOrModifiedTasks?.length ?? 0) > 0) {
              onWorkflowChanged();
            }
          },
          onError: (msg: string) => {
            finishAssistantMsg('');
            setMessages(prev => [...prev, {
              role: 'assistant',
              content: 'Error: ' + msg,
              steps: [], streamingContent: '', isStreaming: false,
            }]);
            setIsRunning(false);
          },
        }
      );
    } catch (e: any) {
      finishAssistantMsg('');
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: 'Failed to start agent: ' + (e?.message ?? String(e)),
        steps: [], streamingContent: '', isStreaming: false,
      }]);
      setIsRunning(false);
    }
  }, [
    input, isRunning, transactionId, convHistory,
    addUserMessage, getOrCreateAssistantMsg, addStep, appendToken,
    showPlan, finishAssistantMsg, onWorkflowChanged,
  ]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  }, [sendMessage]);

  // ── Cleanup on unmount ──────────────────────────────────────────────────────

  useEffect(() => {
    return () => { workflowAutomationAgentService.disconnect(); };
  }, []);

  // ─────────────────────────────────────────────────────────────────────────────
  // Render
  // ─────────────────────────────────────────────────────────────────────────────

  const hasPendingPlan = messages.some(m => m.pendingPlan != null);

  return (
    <div className={`w-full h-full flex flex-col ${theme.mainContentSection}`}>
      {/* Header — hidden when used inside FloatingPanel (which has its own header) */}
      {!hideHeader && (
        <div className={`flex items-center justify-between px-3 py-2 border-b ${t('border_mainContentSection')} shrink-0`}>
          <div className="flex items-center gap-2">
            <i className="fa-solid fa-robot text-blue-500" />
            <span className={`text-sm font-semibold ${theme.title}`}>Workflow AI</span>
          </div>
          <div className="flex items-center gap-1.5">
            {isRunning && (
              <span className={`text-[10px] ${theme.label} opacity-60 animate-pulse`}>
                <i className="fa-solid fa-spinner fa-spin mr-1" />
                Running…
              </span>
            )}
            <button
              onClick={onClose}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title="Close AI panel"
            >
              <i className="fa-solid fa-xmark" />
            </button>
          </div>
        </div>
      )}

      {/* Messages area */}
      <div
        ref={scrollRef}
        className="h-1 flex-auto w-full overflow-y-auto px-3 py-3 space-y-1"
      >
        {messages.length === 0 && (
          <div className={`text-center text-xs ${theme.label} opacity-50 mt-8 px-4`}>
            <i className="fa-solid fa-robot text-2xl mb-3 block opacity-30" />
            <p>Ask me to build, modify, or explain this workflow.</p>
            <p className="mt-1 opacity-70">e.g. "Add a SQL task to truncate the staging table"</p>
          </div>
        )}
        {messages.map((msg, i) => (
          <MessageBubble
            key={i}
            msg={msg}
            onApprove={handleApprove}
            onReject={handleReject}
          />
        ))}
      </div>

      {/* Input area */}
      <div className={`shrink-0 border-t ${t('border_mainContentSection')} px-3 py-2`}>
        {hideHeader && isRunning && (
          <div className={`text-[10px] ${theme.label} opacity-60 animate-pulse mb-1`}>
            <i className="fa-solid fa-spinner fa-spin mr-1" />Running…
          </div>
        )}
        <div className="flex gap-2 items-end">
          <textarea
            ref={inputRef}
            className={`w-1 flex-auto min-h-[56px] max-h-32 px-2 py-1.5 text-xs border rounded resize-none ${theme.inputBox}`}
            placeholder={isRunning ? 'Agent is running…' : 'Message Workflow AI… (Enter to send, Shift+Enter for newline)'}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={isRunning || hasPendingPlan}
            rows={2}
          />
          <button
            onClick={sendMessage}
            disabled={isRunning || !input.trim() || hasPendingPlan}
            className="shrink-0 px-3 py-1.5 text-xs rounded-[4px] bg-blue-600 hover:bg-blue-700 text-white disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <i className="fa-solid fa-paper-plane" />
          </button>
        </div>
        {hasPendingPlan && (
          <p className={`text-[10px] text-amber-600 dark:text-amber-400 mt-1`}>
            <i className="fa-solid fa-circle-exclamation mr-1" />
            Review and approve the plan above before continuing.
          </p>
        )}
      </div>
    </div>
  );
};

export default WorkflowAutomationAgentPanel;
