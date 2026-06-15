import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { AgentMessage, AgentStepEvent } from '../../webapi/appbuilderagentsvc';
import { QueryAgentDoneEvent, queryAgentService } from '../../webapi/queryAgentSvc';

// ─────────────────────────────────────────────────────────────────────────────
// Persistence keys
// ─────────────────────────────────────────────────────────────────────────────

const WINDOW_KEY  = 'queryAgentPanel_window';   // { x, y, width, height }
const SESSION_KEY = 'queryAgentPanel_session';   // { messages, convHistory }

interface WindowGeom { x: number; y: number; width: number; height: number; }

const DEFAULT_W = 820;
const DEFAULT_H = 660;
const MIN_W = 420;
const MIN_H = 320;

function defaultGeom(): WindowGeom {
  return {
    x: Math.max(0, (window.innerWidth  - DEFAULT_W) / 2),
    y: Math.max(0, (window.innerHeight - DEFAULT_H) / 2),
    width:  DEFAULT_W,
    height: DEFAULT_H,
  };
}

function loadGeom(): WindowGeom {
  try {
    const raw = localStorage.getItem(WINDOW_KEY);
    if (!raw) return defaultGeom();
    const g = JSON.parse(raw) as WindowGeom;
    // Clamp to current viewport in case screen changed
    g.width  = Math.max(MIN_W, Math.min(g.width,  window.innerWidth  * 0.96));
    g.height = Math.max(MIN_H, Math.min(g.height, window.innerHeight * 0.95));
    g.x = Math.max(0, Math.min(g.x, window.innerWidth  - g.width));
    g.y = Math.max(0, Math.min(g.y, window.innerHeight - g.height));
    return g;
  } catch { return defaultGeom(); }
}

function saveGeom(g: WindowGeom) {
  try { localStorage.setItem(WINDOW_KEY, JSON.stringify(g)); } catch {}
}

// ─────────────────────────────────────────────────────────────────────────────
// Chat message type
// ─────────────────────────────────────────────────────────────────────────────

interface ChatMessage {
  role:             'user' | 'assistant';
  content:          string;
  steps:            AgentStepEvent[];
  streamingContent: string;
  isStreaming:      boolean;
  generatedQuery?:  string | null;
}

// Serialisable subset (strip transient streaming state before saving)
interface SavedMessage {
  role:           'user' | 'assistant';
  content:        string;
  generatedQuery: string | null;
}

interface SessionState {
  messages:    SavedMessage[];
  convHistory: AgentMessage[];
}

function loadSession(): SessionState {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return { messages: [], convHistory: [] };
    return JSON.parse(raw) as SessionState;
  } catch { return { messages: [], convHistory: [] }; }
}

function saveSession(messages: ChatMessage[], convHistory: AgentMessage[]) {
  try {
    const saved: SessionState = {
      messages: messages
        .filter(m => !m.isStreaming)
        .map(m => ({ role: m.role, content: m.content || m.streamingContent, generatedQuery: m.generatedQuery ?? null })),
      convHistory,
    };
    localStorage.setItem(SESSION_KEY, JSON.stringify(saved));
  } catch {}
}

function restoreMessages(saved: SavedMessage[]): ChatMessage[] {
  return saved.map(m => ({
    role:             m.role,
    content:          m.content,
    steps:            [],
    streamingContent: '',
    isStreaming:      false,
    generatedQuery:   m.generatedQuery,
  }));
}

// ─────────────────────────────────────────────────────────────────────────────
// StepRow
// ─────────────────────────────────────────────────────────────────────────────

const ToolIcon: Record<string, string> = {
  thinking:    'fa-solid fa-brain',
  tool_call:   'fa-solid fa-gear',
  tool_result: 'fa-solid fa-circle-check',
  error:       'fa-solid fa-circle-exclamation',
};

const StepRow: React.FC<{ step: AgentStepEvent }> = ({ step }) => {
  const { theme } = useTheme();
  const [expanded, setExpanded] = useState(false);
  const icon         = ToolIcon[step.Type] ?? 'fa-solid fa-circle-dot';
  const isThinking   = step.Type === 'thinking';
  const isToolCall   = step.Type === 'tool_call';
  const isToolResult = step.Type === 'tool_result';
  const iconColor    = isThinking ? 'text-blue-400 animate-pulse'
                     : step.IsSuccess ? 'text-green-500' : 'text-red-500';

  return (
    <div className="flex items-start gap-2 py-1 text-xs">
      <i className={`${icon} ${iconColor} mt-0.5 w-3.5 shrink-0`} />
      <div className="min-w-0 w-1 flex-auto">
        <div
          className={`flex items-baseline gap-1.5 flex-wrap ${step.Details ? 'cursor-pointer' : ''}`}
          onClick={() => step.Details && setExpanded(e => !e)}
        >
          {isThinking   && <span className="whitespace-nowrap font-semibold text-blue-400">Thinking</span>}
          {isToolCall   && <span className="whitespace-nowrap font-semibold text-yellow-500 font-mono">{step.ToolName}</span>}
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
// GeneratedQueryCard
// ─────────────────────────────────────────────────────────────────────────────

const GeneratedQueryCard: React.FC<{
  sql:        string;
  onUseQuery: (sql: string) => void;
}> = ({ sql, onUseQuery }) => {
  const { theme } = useTheme();
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(sql).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  return (
    <div className="mt-2 rounded-lg border-2 border-blue-400 dark:border-blue-500 overflow-hidden">
      <div className="flex items-center justify-between gap-2 px-3 py-2 bg-blue-50 dark:bg-blue-900/30 border-b border-blue-300 dark:border-blue-600">
        <div className="flex items-center gap-2">
          <i className="fa-solid fa-code text-blue-500" />
          <span className="text-xs font-semibold text-blue-700 dark:text-blue-300">Generated SQL Query</span>
        </div>
        <div className="flex items-center gap-1.5">
          <button
            onClick={handleCopy}
            className="flex items-center gap-1 px-2 py-1 text-xs rounded-[4px] text-blue-600 dark:text-blue-300 hover:bg-blue-100 dark:hover:bg-blue-800/40"
          >
            <i className={`fa-solid ${copied ? 'fa-check' : 'fa-copy'}`} />
            {copied ? 'Copied' : 'Copy'}
          </button>
          <button
            onClick={() => onUseQuery(sql)}
            className="flex items-center gap-1.5 px-3 py-1 text-xs rounded-[4px] bg-blue-600 hover:bg-blue-700 text-white font-semibold"
          >
            <i className="fa-solid fa-arrow-right" />
            Use This Query
          </button>
        </div>
      </div>
      <pre className={`px-3 py-2.5 text-[11px] font-mono overflow-auto max-h-52 leading-relaxed whitespace-pre-wrap ${theme.mainContentSection}`}>
        {sql}
      </pre>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// MessageBubble
// ─────────────────────────────────────────────────────────────────────────────

const MessageBubble: React.FC<{
  msg:        ChatMessage;
  onUseQuery: (sql: string) => void;
}> = ({ msg, onUseQuery }) => {
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

  const displayContent = msg.isStreaming ? msg.streamingContent : msg.content;

  return (
    <div className="mb-3">
      {msg.steps.length > 0 && (
        <div className={`mb-1.5 px-2 py-1.5 rounded-lg ${theme.mainContentSection} border ${t('border_mainContentSection')}`}>
          {msg.steps.map((step, i) => <StepRow key={i} step={step} />)}
        </div>
      )}
      {displayContent && (
        <div className={`px-3 py-2 rounded-lg text-xs leading-relaxed ${theme.mainContentSection} border ${t('border_mainContentSection')} whitespace-pre-wrap`}>
          {displayContent}
          {msg.isStreaming && <span className="animate-pulse">▌</span>}
        </div>
      )}
      {msg.generatedQuery && !msg.isStreaming && (
        <GeneratedQueryCard sql={msg.generatedQuery} onUseQuery={onUseQuery} />
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Drag / resize interaction type
// ─────────────────────────────────────────────────────────────────────────────

type ResizeDir = 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'nw';

interface DragState {
  kind:      'drag' | 'resize';
  dir?:      ResizeDir;
  startMouseX: number;
  startMouseY: number;
  startX:    number;
  startY:    number;
  startW:    number;
  startH:    number;
}

// ─────────────────────────────────────────────────────────────────────────────
// ResizeHandle — thin strips + corner squares around the panel
// ─────────────────────────────────────────────────────────────────────────────

const HANDLE_SIZE = 6; // px

const resizeHandleStyle = (dir: ResizeDir): React.CSSProperties => {
  const base: React.CSSProperties = { position: 'absolute', zIndex: 10, userSelect: 'none' };
  const edge = 0;
  const sz   = HANDLE_SIZE;
  switch (dir) {
    case 'n':  return { ...base, top: edge, left: sz, right: sz, height: sz, cursor: 'n-resize' };
    case 's':  return { ...base, bottom: edge, left: sz, right: sz, height: sz, cursor: 's-resize' };
    case 'w':  return { ...base, left: edge, top: sz, bottom: sz, width: sz, cursor: 'w-resize' };
    case 'e':  return { ...base, right: edge, top: sz, bottom: sz, width: sz, cursor: 'e-resize' };
    case 'nw': return { ...base, top: edge, left: edge, width: sz * 2, height: sz * 2, cursor: 'nw-resize' };
    case 'ne': return { ...base, top: edge, right: edge, width: sz * 2, height: sz * 2, cursor: 'ne-resize' };
    case 'sw': return { ...base, bottom: edge, left: edge, width: sz * 2, height: sz * 2, cursor: 'sw-resize' };
    case 'se': return { ...base, bottom: edge, right: edge, width: sz * 2, height: sz * 2, cursor: 'se-resize' };
  }
};

// ─────────────────────────────────────────────────────────────────────────────
// QueryAgentPanel — draggable, resizable, persistent
// ─────────────────────────────────────────────────────────────────────────────

interface Props {
  dataSourceId:     number;
  selectedTables:   string[];
  onQueryGenerated: (sql: string) => void;
  onClose:          () => void;
}

export const QueryAgentPanel: React.FC<Props> = ({
  dataSourceId,
  selectedTables,
  onQueryGenerated,
  onClose,
}) => {
  const { theme, t } = useTheme();

  // ── Window geometry (persisted) ──────────────────────────────────────────
  const [geom, setGeom] = useState<WindowGeom>(() => loadGeom());
  const geomRef = useRef(geom);
  geomRef.current = geom;

  // ── Conversation state (persisted) ──────────────────────────────────────
  const savedSession = useRef(loadSession());
  const [messages,    setMessages]    = useState<ChatMessage[]>(() => restoreMessages(savedSession.current.messages));
  const [convHistory, setConvHistory] = useState<AgentMessage[]>(() => savedSession.current.convHistory);

  const [input,     setInput]     = useState('');
  const [isRunning, setIsRunning] = useState(false);

  const scrollRef = useRef<HTMLDivElement>(null);
  const inputRef  = useRef<HTMLTextAreaElement>(null);
  const dragRef   = useRef<DragState | null>(null);

  // ── Persist session on every message/history change ─────────────────────
  useEffect(() => {
    saveSession(messages, convHistory);
  }, [messages, convHistory]);

  // ── Auto-scroll ──────────────────────────────────────────────────────────
  const scrollToBottom = useCallback(() => {
    requestAnimationFrame(() => {
      if (scrollRef.current)
        scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    });
  }, []);
  useEffect(() => { scrollToBottom(); }, [messages, scrollToBottom]);
  useEffect(() => { inputRef.current?.focus(); }, []);

  // ── Drag & resize mouse handlers ─────────────────────────────────────────
  const startDrag = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    dragRef.current = {
      kind: 'drag',
      startMouseX: e.clientX,
      startMouseY: e.clientY,
      startX: geomRef.current.x,
      startY: geomRef.current.y,
      startW: geomRef.current.width,
      startH: geomRef.current.height,
    };
  }, []);

  const startResize = useCallback((e: React.MouseEvent, dir: ResizeDir) => {
    e.preventDefault();
    e.stopPropagation();
    dragRef.current = {
      kind: 'resize',
      dir,
      startMouseX: e.clientX,
      startMouseY: e.clientY,
      startX: geomRef.current.x,
      startY: geomRef.current.y,
      startW: geomRef.current.width,
      startH: geomRef.current.height,
    };
  }, []);

  useEffect(() => {
    const onMouseMove = (e: MouseEvent) => {
      const ds = dragRef.current;
      if (!ds) return;

      const dx = e.clientX - ds.startMouseX;
      const dy = e.clientY - ds.startMouseY;
      const vw = window.innerWidth;
      const vh = window.innerHeight;

      if (ds.kind === 'drag') {
        const newX = Math.max(0, Math.min(ds.startX + dx, vw  - ds.startW));
        const newY = Math.max(0, Math.min(ds.startY + dy, vh  - ds.startH));
        setGeom(g => ({ ...g, x: newX, y: newY }));
        return;
      }

      // resize
      const dir = ds.dir!;
      let { startX: nx, startY: ny, startW: nw, startH: nh } = ds;

      if (dir.includes('e')) { nw = Math.max(MIN_W, ds.startW + dx); nw = Math.min(nw, vw - ds.startX); }
      if (dir.includes('s')) { nh = Math.max(MIN_H, ds.startH + dy); nh = Math.min(nh, vh - ds.startY); }
      if (dir.includes('w')) {
        const rawW = ds.startW - dx;
        nw = Math.max(MIN_W, rawW);
        nx = ds.startX + (ds.startW - nw);
      }
      if (dir.includes('n')) {
        const rawH = ds.startH - dy;
        nh = Math.max(MIN_H, rawH);
        ny = ds.startY + (ds.startH - nh);
      }

      setGeom({ x: nx, y: ny, width: nw, height: nh });
    };

    const onMouseUp = () => {
      if (dragRef.current) {
        saveGeom(geomRef.current);
        dragRef.current = null;
      }
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup',   onMouseUp);
    return () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup',   onMouseUp);
    };
  }, []);

  // ── Message helpers ──────────────────────────────────────────────────────

  const addUserMessage = useCallback((text: string) => {
    setMessages(prev => [...prev, {
      role: 'user', content: text,
      steps: [], streamingContent: '', isStreaming: false,
    }]);
  }, []);

  const getOrCreateAssistantMsg = useCallback(() => {
    setMessages(prev => {
      if (prev[prev.length - 1]?.role === 'assistant') return prev;
      return [...prev, { role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true }];
    });
  }, []);

  const appendToken = useCallback((token: string) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') return prev;
      return [...prev.slice(0, -1), { ...last, streamingContent: last.streamingContent + token }];
    });
  }, []);

  const addStep = useCallback((step: AgentStepEvent) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant')
        return [...prev, { role: 'assistant', content: '', steps: [step], streamingContent: '', isStreaming: true }];
      return [...prev.slice(0, -1), { ...last, steps: [...last.steps, step] }];
    });
  }, []);

  const finishAssistantMsg = useCallback((finalContent: string, generatedQuery: string | null) => {
    setMessages(prev => {
      const last = prev[prev.length - 1];
      if (!last || last.role !== 'assistant') return prev;
      return [...prev.slice(0, -1), {
        ...last,
        content:          finalContent || last.streamingContent,
        streamingContent: '',
        isStreaming:      false,
        generatedQuery,
      }];
    });
  }, []);

  // ── Use query ────────────────────────────────────────────────────────────

  const handleUseQuery = useCallback((sql: string) => {
    onQueryGenerated(sql);
    onClose();
  }, [onQueryGenerated, onClose]);

  // ── Clear conversation ───────────────────────────────────────────────────

  const handleClearConversation = useCallback(() => {
    setMessages([]);
    setConvHistory([]);
    try { localStorage.removeItem(SESSION_KEY); } catch {}
  }, []);

  // ── Send message ─────────────────────────────────────────────────────────

  const sendMessage = useCallback(async () => {
    const text = input.trim();
    if (!text || isRunning) return;
    setInput('');
    setIsRunning(true);
    addUserMessage(text);

    try {
      await queryAgentService.startSession(
        { userMessage: text, dataSourceId, selectedTables, conversationHistory: convHistory },
        {
          onStep:  step  => { getOrCreateAssistantMsg(); addStep(step); },
          onToken: token => { getOrCreateAssistantMsg(); appendToken(token); },
          onDone:  (done: QueryAgentDoneEvent) => {
            finishAssistantMsg(done.FinalResponse, done.GeneratedQuery ?? null);
            setConvHistory(done.UpdatedHistory ?? []);
            setIsRunning(false);
          },
          onError: (msg: string) => {
            finishAssistantMsg('', null);
            setMessages(prev => [...prev, {
              role: 'assistant', content: 'Error: ' + msg,
              steps: [], streamingContent: '', isStreaming: false,
            }]);
            setIsRunning(false);
          },
        }
      );
    } catch (e: any) {
      finishAssistantMsg('', null);
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: 'Failed to start agent: ' + (e?.message ?? String(e)),
        steps: [], streamingContent: '', isStreaming: false,
      }]);
      setIsRunning(false);
    }
  }, [
    input, isRunning, dataSourceId, selectedTables, convHistory,
    addUserMessage, getOrCreateAssistantMsg, addStep, appendToken, finishAssistantMsg,
  ]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMessage(); }
  }, [sendMessage]);

  useEffect(() => () => { queryAgentService.disconnect(); }, []);

  // ── Escape to close ──────────────────────────────────────────────────────
  useEffect(() => {
    const h = (e: KeyboardEvent) => { if (e.key === 'Escape' && !isRunning) onClose(); };
    document.addEventListener('keydown', h);
    return () => document.removeEventListener('keydown', h);
  }, [isRunning, onClose]);

  // ─────────────────────────────────────────────────────────────────────────
  // Render
  // ─────────────────────────────────────────────────────────────────────────

  const DIRS: ResizeDir[] = ['n', 'ne', 'e', 'se', 's', 'sw', 'w', 'nw'];

  return createPortal(
    <div
      className={`fixed flex flex-col rounded-xl shadow-2xl border overflow-hidden select-none ${t('border_default')} ${theme.mainContentSection}`}
      style={{
        left:   geom.x,
        top:    geom.y,
        width:  geom.width,
        height: geom.height,
        zIndex: 9999,
      }}
    >
      {/* ── Resize handles ── */}
      {DIRS.map(dir => (
        <div key={dir} style={resizeHandleStyle(dir)} onMouseDown={e => startResize(e, dir)} />
      ))}

      {/* ── Header (drag target) ── */}
      <div
        className={`flex items-center justify-between px-4 py-2.5 border-b shrink-0 cursor-move select-none ${t('border_default')} ${theme.mainHeader}`}
        onMouseDown={startDrag}
      >
        <div className="flex items-center gap-2.5 pointer-events-none">
          <i className="fa-solid fa-database text-purple-500 text-sm" />
          <span className={`text-sm font-semibold ${theme.title}`}>DBA-Genie</span>
          <span className={`text-[10px] px-1.5 py-0.5 rounded-full bg-purple-100 text-purple-700 font-medium`}>Super DBA</span>
          {isRunning && (
            <span className={`text-[10px] ${theme.label} opacity-60 animate-pulse`}>
              <i className="fa-solid fa-spinner fa-spin mr-1" />
              Generating…
            </span>
          )}
        </div>
        <div className="flex items-center gap-1 pointer-events-auto" onMouseDown={e => e.stopPropagation()}>
          {messages.length > 0 && !isRunning && (
            <button
              onClick={handleClearConversation}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} opacity-60 hover:opacity-100`}
              title="Clear conversation"
            >
              <i className="fa-solid fa-trash-can" />
            </button>
          )}
          <button
            onClick={onClose}
            disabled={isRunning}
            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} disabled:opacity-40`}
            title="Close (Esc)"
          >
            <i className="fa-solid fa-xmark" />
          </button>
        </div>
      </div>

      {/* ── Selected tables chips ── */}
      {selectedTables.length > 0 && (
        <div className={`px-4 py-1.5 border-b shrink-0 ${t('border_default')}`}>
          <div className="flex items-center gap-1.5 flex-wrap">
            <span className={`text-[10px] font-semibold uppercase tracking-wide ${theme.label} shrink-0`}>
              Tables:
            </span>
            {selectedTables.map((tbl, i) => (
              <span
                key={i}
                className="px-2 py-0.5 rounded text-[10px] bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-300 font-mono"
              >
                {tbl}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* ── Messages ── */}
      <div
        ref={scrollRef}
        className="h-1 flex-auto w-full overflow-y-auto px-4 py-3 space-y-1"
        style={{ cursor: 'default' }}
      >
        {messages.length === 0 && (
          <div className={`text-center text-xs ${theme.label} opacity-50 mt-10 px-4`}>
            <i className="fa-solid fa-robot text-3xl mb-3 block opacity-25" />
            <p className="font-semibold mb-1">Ask me to generate a SQL query</p>
            {selectedTables.length > 0
              ? <p className="opacity-70">e.g. "Show all {selectedTables[0]} records with their details"</p>
              : <p className="opacity-70">e.g. "Select all tickets with High priority ordered by created date"</p>
            }
            <p className="mt-2 opacity-50">I'll inspect the table columns and write the query for you.</p>
          </div>
        )}
        {messages.map((msg, i) => (
          <MessageBubble key={i} msg={msg} onUseQuery={handleUseQuery} />
        ))}
      </div>

      {/* ── Input ── */}
      <div
        className={`shrink-0 border-t px-4 py-3 ${t('border_default')}`}
        style={{ cursor: 'default' }}
      >
        <div className="flex gap-2 items-end">
          <textarea
            ref={inputRef}
            className={`w-1 flex-auto min-h-[52px] max-h-32 px-2 py-1.5 text-xs border rounded resize-none select-text ${theme.inputBox}`}
            placeholder={isRunning ? 'Agent is working…' : 'Describe the query you need… (Enter to send, Shift+Enter for newline)'}
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            onMouseDown={e => e.stopPropagation()}
            disabled={isRunning}
            rows={2}
          />
          <button
            onClick={sendMessage}
            disabled={isRunning || !input.trim()}
            className="shrink-0 px-3 py-1.5 text-xs rounded-[4px] bg-purple-600 hover:bg-purple-700 text-white disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <i className="fa-solid fa-paper-plane" />
          </button>
        </div>
      </div>

      {/* SE corner resize grip (visual indicator) */}
      <div
        className="absolute bottom-0 right-0 w-4 h-4 flex items-end justify-end pb-0.5 pr-0.5 pointer-events-none"
        style={{ zIndex: 11 }}
      >
        <i className={`fa-solid fa-grip-lines-vertical text-[8px] rotate-45 opacity-30 ${theme.label}`} />
      </div>
    </div>,
    document.body
  );
};

export default QueryAgentPanel;
