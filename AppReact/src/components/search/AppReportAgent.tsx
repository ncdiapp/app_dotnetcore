import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import {
  AgentReportMessage,
  AgentReportResultEvent,
  AgentReportStepEvent,
  AppReportAgentService,
} from '../../webapi/appreportagentsvc';
import { adminSvc } from '../../webapi/adminsvc';
import { dashboardService } from '../../webapi/dashboardsvc';
import { SearchView } from './SearchView';
import appHelper from '../../helper/appHelper';
import { useVoiceInput } from '../../hooks/useVoiceInput';

// ─────────────────────────────────────────────────────────────────────────────
// Hook: usePanelResize
// ─────────────────────────────────────────────────────────────────────────────

function usePanelResize(initialPx: number, min: number, max: number) {
  const [size, setSize] = useState(initialPx);
  const dragging = useRef(false);
  const startX   = useRef(0);
  const startSize = useRef(0);

  const onMouseDown = useCallback((e: React.MouseEvent) => {
    dragging.current  = true;
    startX.current    = e.clientX;
    startSize.current = size;
    e.preventDefault();

    const onMove = (ev: MouseEvent) => {
      if (!dragging.current) return;
      const delta = ev.clientX - startX.current;
      setSize(Math.min(max, Math.max(min, startSize.current + delta)));
    };
    const onUp = () => {
      dragging.current = false;
      window.removeEventListener('mousemove', onMove);
      window.removeEventListener('mouseup', onUp);
    };
    window.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
  }, [size, min, max]);

  return { size, onMouseDown };
}

// ─────────────────────────────────────────────────────────────────────────────
// Local types
// ─────────────────────────────────────────────────────────────────────────────

interface ReportChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: AgentReportStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
}

interface ChatHistoryItem {
  sessionId: string;
  title: string;
  timestamp: string;
  dataSourceId?: number;
  dataSourceName?: string;
  messages: ReportChatMessage[];
  conversationHistory: AgentReportMessage[];
  lastReportResult?: AgentReportResultEvent | null;
}

const HISTORY_KEY = 'appreport_chat_history';
const MAX_HISTORY = 20;

function loadChatHistory(): ChatHistoryItem[] {
  try {
    const raw = localStorage.getItem(HISTORY_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch { return []; }
}

function saveChatHistory(list: ChatHistoryItem[]) {
  try {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(list.slice(0, MAX_HISTORY)));
  } catch {}
}

function formatRelativeTime(ts: string): string {
  const diff = Date.now() - new Date(ts).getTime();
  if (diff < 60_000)  return 'just now';
  if (diff < 3600_000) return `${Math.floor(diff / 60_000)}m ago`;
  if (diff < 86400_000) return `${Math.floor(diff / 3600_000)}h ago`;
  return `${Math.floor(diff / 86400_000)}d ago`;
}

// ─────────────────────────────────────────────────────────────────────────────
// Sub-component: StepRow
// ─────────────────────────────────────────────────────────────────────────────

const StepRow: React.FC<{ step: AgentReportStepEvent }> = ({ step }) => {
  const { theme } = useTheme();
  const [expanded, setExpanded] = useState(false);

  const iconClass =
    step.Type === 'thinking'    ? 'fa-solid fa-brain text-purple-400' :
    step.Type === 'tool_call'   ? 'fa-solid fa-wrench text-blue-400' :
    step.Type === 'tool_result' ? (step.IsSuccess ? 'fa-solid fa-circle-check text-green-400' : 'fa-solid fa-circle-xmark text-red-400') :
                                   'fa-solid fa-triangle-exclamation text-yellow-400';

  return (
    <div className={`flex flex-col gap-0.5 py-0.5 px-2 rounded ${step.IsSuccess ? '' : 'bg-red-50 dark:bg-red-900/20'}`}>
      <button
        className={`flex items-center gap-1.5 text-left w-full`}
        onClick={() => step.Details && setExpanded(e => !e)}
      >
        <i className={`${iconClass} text-[10px] shrink-0`} />
        <span className={`text-[10px] ${theme.label} truncate`}>{step.Description}</span>
        {step.Details && (
          <i className={`fa-solid fa-chevron-${expanded ? 'up' : 'down'} text-[8px] ${theme.label} opacity-50 ml-auto shrink-0`} />
        )}
      </button>
      {expanded && step.Details && (
        <pre className={`text-[10px] ${theme.label} whitespace-pre-wrap break-all px-4 opacity-70`}>
          {step.Details}
        </pre>
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Sub-component: UserBubble
// ─────────────────────────────────────────────────────────────────────────────

const UserBubble: React.FC<{
  msg:     ReportChatMessage;
  onRetry: (content: string) => void;
  onEdit:  (content: string) => void;
}> = ({ msg, onRetry, onEdit }) => {
  const { theme } = useTheme();
  return (
    <div className="flex flex-col items-end gap-1 max-w-[75%]">
      <div className="rounded-lg px-3 py-2 text-xs bg-blue-500 text-white">{msg.content}</div>
      <div className={`flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity ${theme.label}`}>
        <button onClick={() => onRetry(msg.content)} title="Retry"
          className="w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700">
          <i className="fa-solid fa-rotate-right text-[10px]" />
        </button>
        <button onClick={() => onEdit(msg.content)} title="Edit"
          className="w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700">
          <i className="fa-solid fa-pencil text-[10px]" />
        </button>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Sub-component: AssistantBubble
// ─────────────────────────────────────────────────────────────────────────────

const AssistantBubble: React.FC<{ msg: ReportChatMessage }> = ({ msg }) => {
  const { theme } = useTheme();
  const displayText = msg.isStreaming ? msg.streamingContent : msg.content;

  return (
    <div className="flex flex-col gap-1 max-w-[90%]">
      {/* Steps */}
      {msg.steps.length > 0 && (
        <div className={`rounded-lg px-1 py-1 border ${theme.inputBox} text-[10px] space-y-0.5`}>
          {msg.steps.map((step, i) => <StepRow key={i} step={step} />)}
          {msg.isStreaming && (
            <div className="flex items-center gap-1.5 px-2 py-0.5">
              <span className="inline-block w-1.5 h-1.5 rounded-full bg-blue-400 animate-pulse" />
              <span className={`text-[10px] ${theme.label}`}>Thinking…</span>
            </div>
          )}
        </div>
      )}
      {/* Final text */}
      {displayText && (
        <div className={`rounded-lg px-3 py-2 text-xs leading-relaxed ${theme.mainContentSection} border ${theme.inputBox}`}>
          {displayText}
        </div>
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Sub-component: AddToDashboardModal
// ─────────────────────────────────────────────────────────────────────────────

const AddToDashboardModal: React.FC<{
  reportResult: AgentReportResultEvent;
  onClose: () => void;
  onSuccess: (dashboardName: string) => void;
}> = ({ reportResult, onClose, onSuccess }) => {
  const { theme } = useTheme();
  const [dashboards, setDashboards] = useState<any[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        const list = await dashboardService.retrieveCurrentUserDashboardList();
        const items = Array.isArray(list) ? list : list?.Object ?? [];
        setDashboards(items);
        if (items.length > 0) setSelectedId(String(items[0].Id ?? items[0].id ?? ''));
      } catch {
        setError('Failed to load dashboards.');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const handleSave = async () => {
    if (!selectedId) return;
    setSaving(true);
    setError(null);
    try {
      // Load full dashboard structure
      const desktop = await dashboardService.getOneAppDesktop(selectedId);
      const dto = desktop?.Object ?? desktop;

      // Build a new flex widget for the search
      const newWidget = {
        Id:            `widget_${Date.now()}`,
        WidgetItemType: 3,
        DirectiveName:  'appsearch',
        SearchId:       String(reportResult.SearchDefinitionId),
        DisplayName:    reportResult.SearchName,
        IsSavedSearchParameter: false,
        ColSpan:        12,
        RowHeight:      400,
      };

      // Parse OtherSettings and inject the widget into the flex layout
      let otherSettings: any = {};
      try {
        otherSettings = dto.OtherSettings ? JSON.parse(dto.OtherSettings) : {};
      } catch {}

      if (!otherSettings.FlexLayoutItems) otherSettings.FlexLayoutItems = [];

      // Find the first ResponsiveContainer, or create one
      let container = otherSettings.FlexLayoutItems.find((i: any) => i.WidgetItemType === 100);
      if (!container) {
        container = { Id: `rc_${Date.now()}`, WidgetItemType: 100, Children: [] };
        otherSettings.FlexLayoutItems.push(container);
      }
      if (!container.Children) container.Children = [];

      // Add a new RowContainer with the search widget
      container.Children.push({
        Id:            `row_${Date.now()}`,
        WidgetItemType: 101,
        Children:      [newWidget],
      });

      dto.OtherSettings = JSON.stringify(otherSettings);

      await dashboardService.saveOneDashboard(dto);

      const selectedDashboard = dashboards.find(d => String(d.Id ?? d.id ?? '') === selectedId);
      onSuccess(selectedDashboard?.DesktopName ?? selectedDashboard?.Name ?? 'Dashboard');
    } catch (err: any) {
      setError(err?.message ?? 'Failed to save dashboard.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className={`w-[400px] rounded-lg shadow-xl overflow-hidden ${theme.mainContentSection} border ${theme.inputBox}`}>
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.inputBox}`}>
          <div className={`flex items-center gap-2 text-sm font-semibold ${theme.title}`}>
            <i className="fa-solid fa-gauge text-blue-500" />
            Add to Dashboard
          </div>
          <button onClick={onClose} className={`w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700 ${theme.label}`}>
            <i className="fa-solid fa-xmark text-xs" />
          </button>
        </div>

        {/* Body */}
        <div className="px-4 py-4 space-y-3">
          <div className={`text-xs ${theme.label}`}>
            Adding <span className={`font-semibold ${theme.title}`}>{reportResult.SearchName}</span> as a widget.
          </div>

          {loading ? (
            <div className={`text-xs ${theme.label} text-center py-4`}>
              <i className="fa-solid fa-spinner fa-spin mr-2" />Loading dashboards…
            </div>
          ) : dashboards.length === 0 ? (
            <div className={`text-xs ${theme.label}`}>No dashboards found. Create a dashboard first.</div>
          ) : (
            <div>
              <label className={`text-xs ${theme.label} block mb-1`}>Select Dashboard</label>
              <select
                value={selectedId}
                onChange={e => setSelectedId(e.target.value)}
                className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
              >
                {dashboards.map((d: any) => (
                  <option key={d.Id ?? d.id} value={String(d.Id ?? d.id ?? '')}>
                    {d.DesktopName ?? d.Name ?? `Dashboard ${d.Id}`}
                  </option>
                ))}
              </select>
            </div>
          )}

          {error && <div className="text-xs text-red-500">{error}</div>}
        </div>

        {/* Footer */}
        <div className={`flex items-center justify-end gap-2 px-4 py-3 border-t ${theme.inputBox}`}>
          <button onClick={onClose} className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}>
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={saving || loading || !selectedId}
            className="px-3 py-1.5 text-xs rounded-[4px] bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5"
          >
            {saving ? <i className="fa-solid fa-spinner fa-spin text-[10px]" /> : <i className="fa-solid fa-plus text-[10px]" />}
            {saving ? 'Saving…' : 'Add to Dashboard'}
          </button>
        </div>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Main component
// ─────────────────────────────────────────────────────────────────────────────

const AppReportAgent: React.FC = () => {
  const { theme } = useTheme();
  const { param } = useParams<{ param: string }>();

  // ── Panel sizes ─────────────────────────────────────────────────────────────
  const history = usePanelResize(200, 120, 400);
  const chat    = usePanelResize(340, 200, 600);

  // ── Chat state ──────────────────────────────────────────────────────────────
  const [messages, setMessages] = useState<ReportChatMessage[]>([]);
  const [input, setInput] = useState('');

  // ── Voice input ─────────────────────────────────────────────────────────────
  const voice = useVoiceInput((transcript) => {
    setInput(prev => prev ? `${prev} ${transcript}` : transcript);
    textareaRef.current?.focus();
  });
  const [isRunning, setIsRunning] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [conversationHistory, setConversationHistory] = useState<AgentReportMessage[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [chatHistory, setChatHistory] = useState<ChatHistoryItem[]>(() => loadChatHistory());

  // ── Data source (for build fallback) ───────────────────────────────────────
  const [dataSourceId, setDataSourceId] = useState<number | null>(null);
  const [dataSources, setDataSources] = useState<any[]>([]);

  // ── Report result (drives the right panel) ─────────────────────────────────
  const [reportResult, setReportResult] = useState<AgentReportResultEvent | null>(null);
  const [currentViewType, setCurrentViewType] = useState<string>('grid');

  // ── Add-to-Dashboard modal ─────────────────────────────────────────────────
  const [showDashboardModal, setShowDashboardModal] = useState(false);
  const [successToast, setSuccessToast] = useState<string | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef    = useRef<HTMLTextAreaElement>(null);

  // ── Load data sources ───────────────────────────────────────────────────────
  useEffect(() => {
    adminSvc.getDataSourceRegisterList(false).then((list: any[]) => {
      const mapped = (list ?? []).map((d: any) => ({ id: d.id ?? d.Id, name: d.name ?? d.Name ?? d.RegisterName }));
      setDataSources(mapped);
      if (mapped.length > 0 && !dataSourceId)
        setDataSourceId(mapped[0].id ?? null);
    }).catch(() => {});
  }, []);

  // ── Auto-send query from global search bar ──────────────────────────────────
  useEffect(() => {
    if (!param) return;
    try {
      const decoded = JSON.parse(decodeURIComponent(param));
      const q: string = decoded?.searchQuery;
      if (q && q.trim()) sendMessage(q.trim());
    } catch { /* invalid param — ignore */ }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [param]);

  // ── Auto-scroll on new messages ─────────────────────────────────────────────
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // ── Save history when messages change ──────────────────────────────────────
  useEffect(() => {
    if (!sessionId || messages.length === 0) return;
    const existing = chatHistory.find(h => h.sessionId === sessionId);
    const title = messages.find(m => m.role === 'user')?.content?.slice(0, 60) ?? 'New report';
    const ds = dataSources.find(d => (d.Id ?? d.id) === dataSourceId);
    const item: ChatHistoryItem = {
      sessionId,
      title,
      timestamp: existing?.timestamp ?? new Date().toISOString(),
      dataSourceId:   dataSourceId ?? undefined,
      dataSourceName: ds?.Name ?? ds?.name ?? undefined,
      messages:       messages.filter(m => !m.isStreaming),
      conversationHistory,
      lastReportResult: reportResult,
    };
    setChatHistory(prev => {
      const filtered = prev.filter(h => h.sessionId !== sessionId);
      const updated  = [item, ...filtered];
      saveChatHistory(updated);
      return updated;
    });
  }, [messages, conversationHistory, reportResult]);

  // ── Helper: update the last assistant message ─────────────────────────────
  const updateLastAssistant = useCallback((updater: (m: ReportChatMessage) => ReportChatMessage) => {
    setMessages(prev => {
      const idx = [...prev].reverse().findIndex(m => m.role === 'assistant');
      if (idx === -1) return prev;
      const realIdx = prev.length - 1 - idx;
      const updated = [...prev];
      updated[realIdx] = updater(updated[realIdx]);
      return updated;
    });
  }, []);

  // ── Core send message ──────────────────────────────────────────────────────
  const sendMessage = useCallback(async (text: string) => {
    if (isRunning || !text.trim()) return;
    setIsRunning(true);
    setError(null);
    AppReportAgentService.stopPolling();

    const userMsg: ReportChatMessage = {
      role: 'user', content: text, steps: [], streamingContent: '', isStreaming: false,
    };
    const assistantMsg: ReportChatMessage = {
      role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true,
    };
    setMessages(prev => [...prev, userMsg, assistantMsg]);

    try {
      const sid = await AppReportAgentService.startSession(
        text,
        dataSourceId,
        conversationHistory,
        {
          onStep: (step) => {
            appHelper.debugLog('[ReportAgentStep]', step);
            updateLastAssistant(msg => ({ ...msg, steps: [...msg.steps, step] }));
          },
          onToken: (token) => {
            updateLastAssistant(msg => ({ ...msg, streamingContent: (msg.streamingContent || '') + token }));
          },
          onDone: (result) => {
            appHelper.debugLog('[ReportAgentDone]', result);
            updateLastAssistant(msg => ({
              ...msg,
              content:         result.FinalResponse || msg.streamingContent || '',
              streamingContent: '',
              isStreaming:      false,
            }));
            setConversationHistory(result.UpdatedHistory || []);
            if (result.ReportResult) {
              setReportResult(result.ReportResult);
              setCurrentViewType(result.ReportResult.ViewType ?? 'grid');
            }
            setIsRunning(false);
          },
          onError: (message) => {
            updateLastAssistant(msg => ({
              ...msg, content: `Error: ${message}`, isStreaming: false,
            }));
            setError(message);
            setIsRunning(false);
          },
        }
      );
      if (sid) setSessionId(sid);
    } catch (err: any) {
      const errMsg = err?.message ?? 'Unknown error';
      setError(errMsg);
      updateLastAssistant(msg => ({ ...msg, content: `Failed to start: ${errMsg}`, isStreaming: false }));
      setIsRunning(false);
    }
  }, [isRunning, dataSourceId, conversationHistory, updateLastAssistant]);

  const handleSend = useCallback(async () => {
    const text = input.trim();
    if (!text) return;
    setInput('');
    await sendMessage(text);
  }, [input, sendMessage]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }, [handleSend]);

  const handleNewChat = useCallback(() => {
    AppReportAgentService.disconnect();
    setMessages([]);
    setInput('');
    setSessionId(null);
    setConversationHistory([]);
    setReportResult(null);
    setError(null);
    setIsRunning(false);
  }, []);

  const handleLoadSession = useCallback((item: ChatHistoryItem) => {
    AppReportAgentService.disconnect();
    setMessages(item.messages);
    setSessionId(item.sessionId);
    setConversationHistory(item.conversationHistory);
    setReportResult(item.lastReportResult ?? null);
    setCurrentViewType(item.lastReportResult?.ViewType ?? 'grid');
    setError(null);
    setIsRunning(false);
    if (item.dataSourceId !== undefined) setDataSourceId(item.dataSourceId);
  }, []);

  const handleDeleteSession = useCallback((e: React.MouseEvent, sid: string) => {
    e.stopPropagation();
    setChatHistory(prev => {
      const updated = prev.filter(h => h.sessionId !== sid);
      saveChatHistory(updated);
      return updated;
    });
  }, []);

  // ── View type switch — sends a follow-up to the agent ─────────────────────
  const handleSwitchView = useCallback((vt: string) => {
    if (!reportResult) return;
    if (vt === currentViewType) return;
    setCurrentViewType(vt);
    sendMessage(`Show the same results as a ${vt} view.`);
  }, [reportResult, currentViewType, sendMessage]);

  // ── Add to dashboard ───────────────────────────────────────────────────────
  const handleAddToDashboardSuccess = useCallback((dashName: string) => {
    setShowDashboardModal(false);
    setSuccessToast(`Added to "${dashName}"`);
    setTimeout(() => setSuccessToast(null), 3000);
  }, []);

  // ── Build view DTO adjusted for currentViewType ───────────────────────────
  const viewDtoForCurrentType = reportResult?.ViewDto
    ? { ...reportResult.ViewDto, ViewType: currentViewType === 'pivot' ? 4 : currentViewType === 'gantt' ? 3 : 1 }
    : null;

  // ─────────────────────────────────────────────────────────────────────────
  // Render
  // ─────────────────────────────────────────────────────────────────────────

  return (
    <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">
      {/* Success toast */}
      {successToast && (
        <div className="fixed top-4 right-4 z-50 px-4 py-2 rounded-lg bg-green-600 text-white text-xs shadow-lg flex items-center gap-2">
          <i className="fa-solid fa-circle-check" />
          {successToast}
        </div>
      )}

      {/* Add-to-Dashboard modal */}
      {showDashboardModal && reportResult && (
        <AddToDashboardModal
          reportResult={reportResult}
          onClose={() => setShowDashboardModal(false)}
          onSuccess={handleAddToDashboardSuccess}
        />
      )}

      <div className="w-full h-full overflow-hidden flex">

        {/* ── Chat History Panel ──────────────────────────────────────────── */}
        <div
          style={{ width: history.size, minWidth: 120, maxWidth: 400 }}
          className={`flex-none flex flex-col ${theme.mainContentSection} overflow-hidden rounded-l-md`}
        >
          {/* Header */}
          <div className={`flex items-center justify-between px-3 py-2.5 border-b border-gray-200 dark:border-gray-700`}>
            <div className="flex items-center gap-1.5">
              <i className="fa-solid fa-chart-bar text-blue-500 text-sm" />
              <span className={`text-xs font-semibold ${theme.title}`}>History</span>
            </div>
            <button
              onClick={handleNewChat}
              disabled={isRunning}
              className={`flex items-center gap-1 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title="New report query"
            >
              <i className="fa-solid fa-plus text-[10px]" />
              New
            </button>
          </div>

          {/* Session list */}
          <div className="h-1 flex-auto overflow-y-auto py-1">
            {chatHistory.length === 0 ? (
              <div className={`text-center py-8 px-3 text-xs ${theme.label}`}>
                <i className="fa-solid fa-magnifying-glass-chart text-2xl mb-2 opacity-30 block" />
                No queries yet
              </div>
            ) : (
              chatHistory.map(item => {
                const isActive = item.sessionId === sessionId;
                return (
                  <div
                    key={item.sessionId}
                    onClick={() => handleLoadSession(item)}
                    className={`
                      group relative cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-md transition-colors
                      ${isActive ? theme.tab_active : 'hover:bg-gray-100 dark:hover:bg-gray-700/50'}
                    `}
                  >
                    <div className={`text-xs font-medium truncate pr-5 ${theme.title}`}>
                      {item.title}
                    </div>
                    <div className={`text-[10px] mt-0.5 ${theme.label}`}>
                      {formatRelativeTime(item.timestamp)}
                    </div>
                    <button
                      onClick={(e) => handleDeleteSession(e, item.sessionId)}
                      className={`
                        absolute right-2 top-1/2 -translate-y-1/2
                        opacity-0 group-hover:opacity-100 transition-opacity
                        w-5 h-5 flex items-center justify-center rounded
                        hover:text-red-500 ${theme.label}
                      `}
                      title="Delete"
                    >
                      <i className="fa-solid fa-trash text-[9px]" />
                    </button>
                  </div>
                );
              })
            )}
          </div>

          {/* Data source picker */}
          {dataSources.length > 0 && (
            <div className={`px-3 py-2 border-t border-gray-200 dark:border-gray-700`}>
              <div className={`text-[10px] ${theme.label} mb-1`}>
                <i className="fa-solid fa-database mr-1" />Database
              </div>
              <select
                value={dataSourceId ?? ''}
                onChange={e => setDataSourceId(e.target.value ? Number(e.target.value) : null)}
                className={`w-full h-6 px-1 text-[10px] border rounded-[4px] ${theme.inputBox}`}
                disabled={isRunning}
              >
                <option value="">— None (report only) —</option>
                {dataSources.map((ds: any) => (
                  <option key={ds.Id ?? ds.id} value={ds.Id ?? ds.id}>
                    {ds.Name ?? ds.name}
                  </option>
                ))}
              </select>
            </div>
          )}
        </div>

        {/* ── Divider 1 ──────────────────────────────────────────────────── */}
        <div
          onMouseDown={history.onMouseDown}
          className="w-1 flex-none h-full cursor-col-resize bg-gray-200 dark:bg-gray-700 hover:bg-blue-400 dark:hover:bg-blue-500 transition-colors mx-0.5"
          title="Drag to resize"
        />

        {/* ── Chat Panel ─────────────────────────────────────────────────── */}
        <div
          style={{ width: chat.size, minWidth: 200, maxWidth: 600 }}
          className={`flex-none h-full overflow-hidden flex flex-col ${theme.mainContentSection}`}
        >

          {/* Header */}
          <div className={`flex items-center gap-2 px-3 py-2 shrink-0 border-b border-gray-200 dark:border-gray-700`}>
            <i className="fa-solid fa-robot text-blue-500" />
            <span className={`text-sm font-semibold ${theme.title}`}>Report AI</span>
            <span className={`text-xs ${theme.label} opacity-60`}>
              — ask about your data
            </span>
          </div>

          {/* Messages */}
          <div className="h-1 flex-auto overflow-y-auto px-3 py-3 space-y-3">
            {messages.length === 0 ? (
              <div className={`text-center py-10 ${theme.label} text-xs space-y-2`}>
                <i className="fa-solid fa-chart-bar text-3xl opacity-20 block" />
                <p>Ask a question about your data.</p>
                <div className="space-y-1 mt-3">
                  {[
                    'Show me open work orders from last 30 days',
                    'List all customers by region',
                    'Show inventory below minimum stock level',
                  ].map((ex, i) => (
                    <button
                      key={i}
                      onClick={() => { setInput(ex); textareaRef.current?.focus(); }}
                      className={`block w-full text-left px-3 py-2 rounded-lg text-xs border ${theme.inputBox} hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors`}
                    >
                      {ex}
                    </button>
                  ))}
                </div>
              </div>
            ) : (
              messages.map((msg, i) => (
                <div key={i} className={`flex group ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                  {msg.role === 'user' ? (
                    <UserBubble
                      msg={msg}
                      onRetry={sendMessage}
                      onEdit={setInput}
                    />
                  ) : (
                    <AssistantBubble msg={msg} />
                  )}
                </div>
              ))
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Error banner */}
          {error && (
            <div className="mx-3 mb-1 px-3 py-2 text-xs text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-200 dark:border-red-800 flex items-start gap-2">
              <i className="fa-solid fa-triangle-exclamation shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          {/* Input bar */}
          <div className={`px-3 pb-3 pt-2 shrink-0 border-t border-gray-200 dark:border-gray-700`}>
            <div className={`flex gap-2 rounded-lg border ${theme.inputBox} px-2 py-1.5 items-end`}>
              <textarea
                ref={textareaRef}
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Ask about your data…"
                rows={1}
                disabled={isRunning}
                className={`w-1 flex-auto resize-none bg-transparent text-xs outline-none ${theme.label} placeholder-gray-400 dark:placeholder-gray-600 disabled:opacity-60`}
                style={{ maxHeight: '96px', overflowY: 'auto' }}
              />
              {voice.supported && (
                <button
                  onClick={voice.toggle}
                  disabled={isRunning}
                  className={`w-7 h-7 flex items-center justify-center rounded-[4px] shrink-0 transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
                    voice.isListening
                      ? 'bg-red-500 hover:bg-red-600 text-white animate-pulse'
                      : `${theme.button_default}`
                  }`}
                  title={voice.isListening ? 'Stop listening' : 'Voice input'}
                >
                  <i className={`fa-solid ${voice.isListening ? 'fa-stop' : 'fa-microphone'} text-xs`} />
                </button>
              )}
              <button
                onClick={handleSend}
                disabled={isRunning || !input.trim()}
                className="w-7 h-7 flex items-center justify-center rounded-[4px] bg-blue-500 hover:bg-blue-600 text-white disabled:opacity-40 disabled:cursor-not-allowed shrink-0"
                title="Send (Enter)"
              >
                {isRunning
                  ? <i className="fa-solid fa-spinner fa-spin text-xs" />
                  : <i className="fa-solid fa-paper-plane text-xs" />
                }
              </button>
            </div>
            <div className={`text-[10px] mt-1 ${theme.label} opacity-50`}>
              Enter to send · Shift+Enter for newline
            </div>
          </div>
        </div>

        {/* ── Divider 2 ──────────────────────────────────────────────────── */}
        <div
          onMouseDown={chat.onMouseDown}
          className="w-1 flex-none h-full cursor-col-resize bg-gray-200 dark:bg-gray-700 hover:bg-blue-400 dark:hover:bg-blue-500 transition-colors mx-0.5"
          title="Drag to resize"
        />

        {/* ── Report Panel ───────────────────────────────────────────────── */}
        <div className={`w-1 flex-auto h-full overflow-hidden flex flex-col rounded-r-md ${theme.mainContentSection}`}>

          {/* Report panel header */}
          <div className={`flex items-center justify-between px-3 py-2 shrink-0 border-b border-gray-200 dark:border-gray-700`}>
            <div className="flex items-center gap-2">
              <i className="fa-solid fa-table text-blue-500 text-sm" />
              <span className={`text-sm font-semibold ${theme.title}`}>
                {reportResult ? reportResult.SearchName : 'Report View'}
              </span>
              {reportResult && (
                <span className={`text-xs ${theme.label} opacity-60`}>
                  {reportResult.RowCount} row{reportResult.RowCount !== 1 ? 's' : ''}
                </span>
              )}
            </div>

            <div className="flex items-center gap-2">
              {/* View type toggle */}
              {reportResult && (
                <div className={`flex items-center rounded-[4px] border ${theme.inputBox} overflow-hidden`}>
                  {(['grid', 'pivot', 'gantt'] as const).map(vt => (
                    <button
                      key={vt}
                      onClick={() => handleSwitchView(vt)}
                      className={`px-2 py-1 text-xs transition-colors ${
                        currentViewType === vt
                          ? 'bg-blue-500 text-white'
                          : `${theme.label} hover:bg-gray-100 dark:hover:bg-gray-700`
                      }`}
                      title={`${vt.charAt(0).toUpperCase() + vt.slice(1)} view`}
                    >
                      <i className={`fa-solid ${
                        vt === 'grid'  ? 'fa-table' :
                        vt === 'pivot' ? 'fa-chart-bar' :
                                         'fa-diagram-project'
                      } text-[10px]`} />
                    </button>
                  ))}
                </div>
              )}

              {/* Add to Dashboard */}
              {reportResult && (
                <button
                  onClick={() => setShowDashboardModal(true)}
                  className={`flex items-center gap-1.5 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                  title="Add to Dashboard"
                >
                  <i className="fa-solid fa-gauge text-[10px]" />
                  Add to Dashboard
                </button>
              )}
            </div>
          </div>

          {/* Report content */}
          <div className="w-full h-1 flex-auto overflow-hidden">
            {reportResult && viewDtoForCurrentType ? (
              <SearchView
                viewDto={viewDtoForCurrentType}
                viewDataList={reportResult.SearchResultRows ?? []}
              />
            ) : (
              <div className={`w-full h-full flex flex-col items-center justify-center gap-3 ${theme.label}`}>
                <i className="fa-solid fa-chart-bar text-5xl opacity-10" />
                <div className="text-sm opacity-40">
                  {isRunning ? 'Running report…' : 'Ask a question to see results here'}
                </div>
                {isRunning && (
                  <i className="fa-solid fa-spinner fa-spin text-blue-400 text-xl" />
                )}
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  );
};

export default AppReportAgent;
