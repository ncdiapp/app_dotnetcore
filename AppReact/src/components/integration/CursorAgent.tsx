import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import {
  CursorAgentDoneEvent,
  CursorAgentGateEvent,
  CursorAgentMessage,
  CursorAgentSessionSummary,
  CursorAgentSkillMenuItem,
  CursorAgentStepEvent,
  CursorAgentWorkspaceFile,
  archiveCursorSessions,
  cursorAgentService,
  cursorChatTitle,
  deleteCursorSessions,
  getCursorSession,
  getRecentCursorSessions,
  listCursorSkillMenu,
  listCursorWorkspaceFiles,
  readCursorWorkspaceFile,
  renameCursorSession,
} from '../../webapi/cursoragentsvc';
import { endpoints } from '../../webapi/endpoints';
import { isAdminUserFromContext } from '../../helper/adminPermissionHelper';
import Confirm from '../common/Confirm';
import appHelper from '../../helper/appHelper';
import { useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';
import CursorAgentChatManagement, { RenameChatDialog } from './CursorAgentChatManagement';
import { appConfigPackSvc } from '../../webapi/appConfigPackSvc';
import { refreshUserTreeMenu } from '../../helper/userMenuHelper';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: CursorAgentStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
  timestamp?: string;
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
  if (!cleaned && srcs.length === 0) return null;
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

function historyText(m: any): string {
  return String(m?.content ?? m?.Content ?? m?.text ?? m?.Text ?? '').trim();
}

function historyRole(m: any): 'user' | 'assistant' {
  const role = String(m?.role ?? m?.Role ?? 'assistant').toLowerCase();
  return role === 'user' ? 'user' : 'assistant';
}

function historyTimestamp(m: any): string | undefined {
  const raw = m?.Timestamp ?? m?.timestamp ?? m?.CreatedAt ?? m?.createdAt;
  if (!raw) return undefined;
  const d = new Date(raw);
  return isNaN(d.getTime()) ? undefined : d.toISOString();
}

function formatMessageTime(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  return d.toLocaleString(undefined, { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
}

function workspaceFilePath(f: CursorAgentWorkspaceFile): string {
  return String(f?.RelativePath ?? (f as any)?.relativePath ?? '');
}

function isAppConfigPackPath(path: string): boolean {
  return /\.appConfigPack\.json$/i.test(path || '');
}

function mapHistoryMessages(hist: any[]): ChatMessage[] {
  return (hist || []).map((m: any) => ({
    role: historyRole(m),
    content: historyText(m),
    steps: [],
    streamingContent: '',
    isStreaming: false,
    timestamp: historyTimestamp(m),
  }));
}

const formatElapsed = (seconds: number) => {
  const m = Math.floor(Math.max(0, seconds) / 60);
  const s = Math.max(0, seconds) % 60;
  return `${m}:${s.toString().padStart(2, '0')}`;
};

const WorkingStatus: React.FC<{ label: string }> = ({ label }) => {
  const { theme } = useTheme();
  const [elapsed, setElapsed] = useState(0);
  useEffect(() => {
    const started = Date.now();
    setElapsed(0);
    const id = window.setInterval(() => setElapsed(Math.floor((Date.now() - started) / 1000)), 250);
    return () => window.clearInterval(id);
  }, []);
  return (
    <div className={`flex items-center text-xs mb-2 ${theme.label}`}>
      <i className="fa-solid fa-circle-notch animate-spin mr-2" />
      <span>{label} {formatElapsed(elapsed)}</span>
    </div>
  );
};

const UserMessageBlock: React.FC<{
  msg: ChatMessage;
  disabled: boolean;
  isEditing: boolean;
  editText: string;
  onEditText: (v: string) => void;
  onStartEdit: () => void;
  onCancelEdit: () => void;
  onSaveEdit: () => void;
}> = ({ msg, disabled, isEditing, editText, onEditText, onStartEdit, onCancelEdit, onSaveEdit }) => {
  const { theme, t } = useTheme();
  const [copied, setCopied] = useState(false);
  const timeLabel = formatMessageTime(msg.timestamp);

  const handleCopy = () => {
    const text = msg.content || msg.streamingContent || '';
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => {
      setCopied(true);
      window.setTimeout(() => setCopied(false), 1500);
    }).catch(() => {});
  };

  return (
    <div>
      <div className="flex items-center justify-end mb-1 space-x-2">
        {timeLabel ? <span className={`text-[10px] ${theme.label}`}>{timeLabel}</span> : null}
        <button
          type="button"
          title="Edit"
          disabled={disabled}
          className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
          onClick={onStartEdit}
        >
          <i className="fa-solid fa-pencil" />
        </button>
        <button
          type="button"
          title={copied ? 'Copied' : 'Copy'}
          className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
          onClick={handleCopy}
        >
          <i className={`fa-solid ${copied ? 'fa-check' : 'fa-copy'}`} />
        </button>
      </div>
      {isEditing ? (
        <div className={`w-full rounded-xl px-5 py-4 border ${t('bg_default')} ${t('text_title')} ${theme.inputBox}`}>
          <textarea
            value={editText}
            onChange={e => onEditText(e.target.value)}
            rows={4}
            autoFocus
            autoComplete="off"
            className={`w-full text-sm leading-relaxed resize-none border-0 focus:outline-none bg-transparent ${t('text_title')}`}
          />
          <div className="flex justify-end space-x-2 mt-2">
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onSaveEdit}>Save</button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onCancelEdit}>Cancel</button>
          </div>
        </div>
      ) : (
        <div className={`w-full rounded-xl px-5 py-4 text-sm leading-relaxed whitespace-pre-wrap ${t('bg_default')} ${t('text_title')}`}>
          {msg.content || msg.streamingContent || ''}
        </div>
      )}
    </div>
  );
};

const isFileManagementApp = (name: string) => /file\s*management/i.test(name || '');

const pickDefaultApplicationId = (apps: { id: number; name: string }[]) => {
  const other = apps.find(a => !isFileManagementApp(a.name));
  return (other ?? apps[0])?.id;
};

const SUBMENU_CLOSE_DELAY_MS = 350;
const CHAT_LIST_DEFAULT_PX = 208;
const CHAT_LIST_MIN_PX = 160;
const CHAT_LIST_MAX_PX = 420;
const WORKSPACE_DEFAULT_PX = 256;
const WORKSPACE_MIN_PX = 180;
const WORKSPACE_MAX_PX = 560;
const CENTER_MIN_PX = 280;

interface PanelResizeHandleProps {
  label: string;
  edge: 'right' | 'left';
  onMouseDown: (e: React.MouseEvent) => void;
}

const PanelResizeHandle: React.FC<PanelResizeHandleProps> = ({ label, edge, onMouseDown }) => (
  <div
    role="separator"
    aria-orientation="vertical"
    aria-label={label}
    title="Drag to resize"
    onMouseDown={onMouseDown}
    className={`absolute top-0 bottom-0 z-10 w-2 cursor-col-resize select-none ${
      edge === 'right' ? 'right-0 -mr-1' : 'left-0 -ml-1'
    }`}
  />
);

const itemKey = (i: CursorAgentSkillMenuItem) => i.Key;
const itemLabel = (i: CursorAgentSkillMenuItem) => i.Label;
const itemGroup = (i: CursorAgentSkillMenuItem) => i.Group;

const ToolbarField: React.FC<{ label: string; children: React.ReactNode }> = ({ label, children }) => {
  const { theme } = useTheme();
  return (
    <div className="flex items-center shrink-0">
      <label className={`text-xs ${theme.label} mr-2 shrink-0 whitespace-nowrap`}>{label}</label>
      {children}
    </div>
  );
};

const SkillPicker: React.FC<{
  items: CursorAgentSkillMenuItem[];
  value: string;
  disabled: boolean;
  lockSelection: boolean;
  onChange: (key: string) => void;
}> = ({ items, value, disabled, lockSelection, onChange }) => {
  const { theme } = useTheme();
  const triggerRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<{
    left: number;
    top?: number;
    bottom?: number;
    openUp: boolean;
    maxHeight: number;
  } | null>(null);
  const [hoveredGroup, setHoveredGroup] = useState<string | null>(null);
  const closeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const selected = items.find(i => itemKey(i) === value);
  const label = selected ? itemLabel(selected) : 'App Config Builder';

  const categories: { id: string; label: string; leafKey?: string; children: CursorAgentSkillMenuItem[] }[] = [
    { id: 'general', label: 'General', leafKey: 'general', children: [] },
    { id: 'app', label: 'App Config Builder', leafKey: 'app-config-builder', children: [] },
    { id: 'plm', label: 'PLM Integration', children: items.filter(i => itemGroup(i) === 'plm') },
    { id: 'other', label: 'Other skills', children: items.filter(i => itemGroup(i) === 'other' || itemGroup(i) === 'saved') },
  ];

  const close = useCallback(() => {
    setOpen(false);
    setPosition(null);
    setHoveredGroup(null);
  }, []);

  const pick = (key: string) => {
    if (lockSelection) return;
    onChange(key);
    close();
  };

  const toggleOpen = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (disabled) return;
    if (open) {
      close();
      return;
    }
    const rect = triggerRef.current?.getBoundingClientRect();
    const gap = 4;
    const left = rect?.left ?? 8;
    const topEdge = rect?.top ?? 40;
    const bottomEdge = rect?.bottom ?? 40;
    const spaceBelow = window.innerHeight - bottomEdge - gap;
    const spaceAbove = topEdge - gap;
    const estimatedHeight = 220;
    const openUp = spaceBelow < estimatedHeight && spaceAbove > spaceBelow;
    const maxHeight = Math.max(120, Math.min(384, openUp ? spaceAbove : spaceBelow));
    setPosition(openUp
      ? { left, bottom: window.innerHeight - topEdge + gap, openUp: true, maxHeight }
      : { left, top: bottomEdge + gap, openUp: false, maxHeight });
    setOpen(true);
  };

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      const t = e.target as Node;
      if (triggerRef.current?.contains(t)) return;
      const portal = document.getElementById('cursor-agent-skill-menu');
      if (portal?.contains(t)) return;
      close();
    };
    const timer = window.setTimeout(() => {
      document.addEventListener('mousedown', onDoc);
    }, 0);
    return () => {
      window.clearTimeout(timer);
      document.removeEventListener('mousedown', onDoc);
    };
  }, [open, close]);

  return (
    <div ref={triggerRef} className="relative shrink-0">
      <button
        type="button"
        disabled={disabled}
        className={`h-7 px-2 text-xs border rounded-[4px] w-52 flex items-center justify-between gap-2 focus:outline-none ${theme.inputBox}`}
        onMouseDown={toggleOpen}
        title={lockSelection ? `${label} (locked for this chat — click New to change)` : label}
      >
        <span className="truncate min-w-0">{label}</span>
        <i className={`fa-solid ${lockSelection ? 'fa-lock' : 'fa-chevron-down'} text-[10px] shrink-0`} />
      </button>
      {open && position && createPortal(
        <div
          id="cursor-agent-skill-menu"
          className={`fixed z-[9999] flex flex-row overflow-visible ${position.openUp ? 'items-end' : 'items-start'} ${theme.mainContentSection}`}
          style={position.openUp
            ? { bottom: position.bottom, left: position.left }
            : { top: position.top, left: position.left }}
          onMouseEnter={() => {
            if (closeTimerRef.current) {
              clearTimeout(closeTimerRef.current);
              closeTimerRef.current = null;
            }
          }}
          onMouseLeave={() => {
            closeTimerRef.current = setTimeout(() => {
              setHoveredGroup(null);
              closeTimerRef.current = null;
            }, SUBMENU_CLOSE_DELAY_MS);
          }}
        >
          <div
            className={`min-w-[200px] overflow-y-auto border rounded-l shadow-lg py-1 ${theme.mainContentSection}`}
            style={{ maxHeight: position.maxHeight }}
          >
            {lockSelection && (
              <div className={`px-3 py-2 text-xs ${theme.label}`}>Skill is locked for this chat</div>
            )}
            {categories.map(cat => {
              const hasChildren = cat.id === 'plm' || cat.id === 'other';
              const isHovered = hoveredGroup === cat.id;
              return (
                <button
                  type="button"
                  key={cat.id}
                  className={`w-full px-3 py-2 text-left text-sm flex items-center justify-between ${theme.contextMenu} ${isHovered ? (theme.tab_active ?? '') : ''}`}
                  onMouseEnter={() => {
                    if (closeTimerRef.current) {
                      clearTimeout(closeTimerRef.current);
                      closeTimerRef.current = null;
                    }
                    setHoveredGroup(hasChildren ? cat.id : null);
                  }}
                  onClick={() => {
                    if (cat.leafKey) pick(cat.leafKey);
                  }}
                >
                  <span className="truncate">{cat.label}</span>
                  {hasChildren && <i className="fa-solid fa-chevron-right text-xs ml-1 shrink-0" />}
                </button>
              );
            })}
          </div>
          {hoveredGroup === 'plm' && (
            <div
              className={`min-w-[240px] max-w-[320px] overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}
              style={{ maxHeight: position.maxHeight }}
            >
              {categories.find(c => c.id === 'plm')!.children.length === 0 ? (
                <div className={`px-3 py-2 text-sm ${theme.label}`}>No PLM skills</div>
              ) : categories.find(c => c.id === 'plm')!.children.map(item => (
                <button
                  key={itemKey(item)}
                  type="button"
                  onClick={() => pick(itemKey(item))}
                  className={`w-full px-3 py-2 text-left text-sm truncate ${theme.contextMenu} hover:opacity-90`}
                >
                  {itemLabel(item)}
                </button>
              ))}
            </div>
          )}
          {hoveredGroup === 'other' && (
            <div
              className={`min-w-[240px] max-w-[320px] overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}
              style={{ maxHeight: position.maxHeight }}
            >
              {categories.find(c => c.id === 'other')!.children.length === 0 ? (
                <div className={`px-3 py-2 text-sm ${theme.label}`}>No other skills</div>
              ) : categories.find(c => c.id === 'other')!.children.map(item => (
                <button
                  key={itemKey(item)}
                  type="button"
                  onClick={() => pick(itemKey(item))}
                  className={`w-full px-3 py-2 text-left text-sm truncate ${theme.contextMenu} hover:opacity-90`}
                >
                  {itemLabel(item)}
                </button>
              ))}
            </div>
          )}
        </div>,
        document.body
      )}
    </div>
  );
};

const CursorAgent: React.FC = () => {
  const { theme, t } = useTheme();
  const userContext = useSelector((s: RootState) => s.userSession.userContext);
  const isAdmin = isAdminUserFromContext(userContext);

  const [applications, setApplications] = useState<{ id: number; name: string }[]>([]);
  const [dataSources, setDataSources] = useState<{ id: number; name: string }[]>([]);
  const [saasApplicationId, setSaasApplicationId] = useState<number | undefined>();
  const [dataSourceId, setDataSourceId] = useState<number | undefined>();
  const [skillKey, setSkillKey] = useState('app-config-builder');
  const [skillItems, setSkillItems] = useState<CursorAgentSkillMenuItem[]>([]);

  const [sessionId, setSessionId] = useState<string | null>(null);
  const [hasAgent, setHasAgent] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const isRunningRef = useRef(false);
  const [error, setError] = useState<string | null>(null);
  const [pendingGate, setPendingGate] = useState<CursorAgentGateEvent | null>(null);
  const [gateFeedback, setGateFeedback] = useState('');
  const [chatHistory, setChatHistory] = useState<CursorAgentSessionSummary[]>([]);
  const [files, setFiles] = useState<CursorAgentWorkspaceFile[]>([]);
  const [previewPath, setPreviewPath] = useState<string | null>(null);
  const [previewContent, setPreviewContent] = useState('');
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [chatListWidth, setChatListWidth] = useState(CHAT_LIST_DEFAULT_PX);
  const [workspaceWidth, setWorkspaceWidth] = useState(WORKSPACE_DEFAULT_PX);
  const layoutRef = useRef<HTMLDivElement | null>(null);
  const panelDragRef = useRef<'chat' | 'workspace' | null>(null);
  const chatListWidthRef = useRef(CHAT_LIST_DEFAULT_PX);
  const workspaceWidthRef = useRef(WORKSPACE_DEFAULT_PX);
  const workspaceOpenRef = useRef(false);
  const [chatMenu, setChatMenu] = useState<{ visible: boolean; x: number; y: number; item: CursorAgentSessionSummary | null }>({
    visible: false, x: 0, y: 0, item: null,
  });
  const [renameItem, setRenameItem] = useState<CursorAgentSessionSummary | null>(null);
  const [deleteItem, setDeleteItem] = useState<CursorAgentSessionSummary | null>(null);
  const [manageOpen, setManageOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editText, setEditText] = useState('');
  const [isBuildingPack, setIsBuildingPack] = useState(false);
  const [buildResult, setBuildResult] = useState<string | null>(null);
  const [confirmPackPath, setConfirmPackPath] = useState<string | null>(null);
  const chatMenuRef = useRef<HTMLDivElement | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  chatListWidthRef.current = chatListWidth;
  workspaceWidthRef.current = workspaceWidth;
  workspaceOpenRef.current = workspaceOpen;

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      const kind = panelDragRef.current;
      const rect = layoutRef.current?.getBoundingClientRect();
      if (!kind || !rect) return;
      const gap = 4;
      const wsOpen = workspaceOpenRef.current;
      const wsW = wsOpen ? workspaceWidthRef.current : 0;
      const extra = wsOpen ? gap : 0;
      if (kind === 'chat') {
        const max = Math.min(
          CHAT_LIST_MAX_PX,
          Math.max(CHAT_LIST_MIN_PX, rect.width - CENTER_MIN_PX - gap - wsW - extra)
        );
        const next = Math.min(max, Math.max(CHAT_LIST_MIN_PX, e.clientX - rect.left));
        setChatListWidth(next);
      } else {
        const max = Math.min(
          WORKSPACE_MAX_PX,
          Math.max(WORKSPACE_MIN_PX, rect.width - CENTER_MIN_PX - gap - chatListWidthRef.current - gap)
        );
        const next = Math.min(max, Math.max(WORKSPACE_MIN_PX, rect.right - e.clientX));
        setWorkspaceWidth(next);
      }
    };
    const onUp = () => {
      if (!panelDragRef.current) return;
      panelDragRef.current = null;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, []);

  const startResize = useCallback((kind: 'chat' | 'workspace') => (e: React.MouseEvent) => {
    e.preventDefault();
    panelDragRef.current = kind;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const refreshHistory = useCallback(() => {
    getRecentCursorSessions(30).then(setChatHistory).catch(() => {});
  }, []);

  const refreshFiles = useCallback((sid: string | null) => {
    if (!sid) { setFiles([]); return; }
    listCursorWorkspaceFiles(sid).then(list => {
      setFiles(list);
      if (list.some(f => !f.IsDirectory)) setWorkspaceOpen(true);
    }).catch(() => setFiles([]));
  }, []);

  useEffect(() => {
    adminSvc.retrieveSelectedApplicationPackages(true).then((list: any) => {
      const arr = Array.isArray(list) ? list : (list?.Object ?? list?.ObjectList ?? []);
      const mapped = (arr || []).map((a: any) => ({
        id: Number(a.Id ?? a.id),
        name: String(a.Name ?? a.name ?? a.Id),
      })).filter((a: { id: number }) => a.id > 0);
      setApplications(mapped);
      setSaasApplicationId(prev => prev ?? pickDefaultApplicationId(mapped));
    }).catch(() => {});
    adminSvc.getDataSourceRegisterList(true).then((list: any[]) => {
      if (list?.length) {
        const mapped = list.map((d: any) => ({ id: d.Id, name: d.Name || d.DataSourceName }));
        setDataSources(mapped);
        setDataSourceId(prev => prev ?? mapped[0]?.id);
      }
    }).catch(() => {});
    refreshHistory();
    listCursorSkillMenu().then(menu => {
      setSkillItems(menu.Items ?? []);
      if (menu.DefaultKey) setSkillKey(prev => prev || menu.DefaultKey);
    }).catch(() => {});
  }, [refreshHistory]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, pendingGate]);

  useEffect(() => () => { cursorAgentService.disconnect(); }, []);

  const closeChatMenu = useCallback(() => {
    setChatMenu({ visible: false, x: 0, y: 0, item: null });
  }, []);

  useEffect(() => {
    if (!chatMenu.visible) return;
    const onDoc = (e: MouseEvent) => {
      if (chatMenuRef.current?.contains(e.target as Node)) return;
      closeChatMenu();
    };
    const t = window.setTimeout(() => document.addEventListener('mousedown', onDoc), 0);
    return () => {
      window.clearTimeout(t);
      document.removeEventListener('mousedown', onDoc);
    };
  }, [chatMenu.visible, closeChatMenu]);

  useRefineContextMenuField(chatMenu.visible, chatMenuRef, setChatMenu);

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
        const final = String((result as any)?.FinalResponse ?? (result as any)?.finalResponse ?? '').trim();
        const hist = (result as any)?.UpdatedHistory ?? (result as any)?.updatedHistory ?? [];
        const fromHist = Array.isArray(hist) && hist.length
          ? historyText(hist[hist.length - 1])
          : '';
        updateLastAssistant(msg => {
          const content = final || msg.streamingContent || fromHist || msg.content || '';
          return {
            ...msg,
            content: content || 'No reply received.',
            streamingContent: '',
            isStreaming: false,
          };
        });
        refreshHistory();
        refreshFiles(cursorAgentService.currentSessionId);
      } finally {
        setIsRunning(false);
        isRunningRef.current = false;
      }
    },
    onError: (message: string) => {
      try {
        setPendingGate(null);
        updateLastAssistant(msg => ({ ...msg, content: `Error: ${message}`, isStreaming: false }));
        setError(message);
      } finally {
        setIsRunning(false);
        isRunningRef.current = false;
      }
    },
  }), [refreshFiles, refreshHistory, updateLastAssistant]);

  const sendFirstOrFollowUp = useCallback(async (text: string, truncateFrom?: number) => {
    if (!text || isRunningRef.current) return;
    if (!saasApplicationId) {
      setError('Select an Application first.');
      return;
    }
    isRunningRef.current = true;
    setError(null);
    setIsRunning(true);
    setPendingGate(null);
    const now = new Date().toISOString();
    const userMsg: ChatMessage = { role: 'user', content: text, steps: [], streamingContent: '', isStreaming: false, timestamp: now };
    const assistantMsg: ChatMessage = { role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true, timestamp: now };
    setMessages(prev => {
      const base = truncateFrom == null ? prev : prev.slice(0, truncateFrom);
      return [...base, userMsg, assistantMsg];
    });
    try {
      if (!hasAgent || !sessionId) {
        const sid = await cursorAgentService.startSession(
          text, saasApplicationId, dataSourceId, [], makeHandlers(), skillKey);
        setSessionId(sid);
        setHasAgent(true);
        refreshFiles(sid);
      } else {
        await cursorAgentService.followUp(text, makeHandlers(), skillKey, saasApplicationId, dataSourceId);
      }
    } catch (err: any) {
      const errMsg = err?.message ?? 'Unknown error';
      setError(errMsg);
      updateLastAssistant(msg => ({ ...msg, content: `Failed: ${errMsg}`, isStreaming: false }));
      setIsRunning(false);
      isRunningRef.current = false;
    }
  }, [dataSourceId, hasAgent, makeHandlers, refreshFiles, saasApplicationId, sessionId, skillKey, updateLastAssistant]);

  const handleSend = useCallback(async () => {
    const text = input.trim();
    if (!text) return;
    setInput('');
    setEditingIndex(null);
    await sendFirstOrFollowUp(text);
  }, [input, sendFirstOrFollowUp]);

  const handleSaveEdit = useCallback(async () => {
    const text = editText.trim();
    if (!text || editingIndex == null || isRunning) return;
    const from = editingIndex;
    setEditingIndex(null);
    setEditText('');
    await sendFirstOrFollowUp(text, from);
  }, [editText, editingIndex, isRunning, sendFirstOrFollowUp]);

  const handleNewChat = useCallback(() => {
    cursorAgentService.disconnect();
    cursorAgentService.currentSessionId = null;
    setSessionId(null);
    setHasAgent(false);
    setMessages([]);
    setPendingGate(null);
    setError(null);
    setIsRunning(false);
    isRunningRef.current = false;
    setFiles([]);
    setPreviewPath(null);
    setWorkspaceOpen(false);
    setEditingIndex(null);
    setEditText('');
    setSkillKey('app-config-builder');
    setSaasApplicationId(pickDefaultApplicationId(applications));
    setDataSourceId(dataSources[0]?.id);
    setIsBuildingPack(false);
    setBuildResult(null);
    setConfirmPackPath(null);
  }, [applications, dataSources]);

  const handleDeletedSessions = useCallback((ids?: string[]) => {
    refreshHistory();
    if (ids?.some(id => id === sessionId)) {
      handleNewChat();
    }
  }, [handleNewChat, refreshHistory, sessionId]);

  const handleLoadSession = useCallback(async (summary: CursorAgentSessionSummary) => {
    handleNewChat();
    const session = await getCursorSession(summary.SessionGuid);
    if (!session) return;
    setSessionId(summary.SessionGuid);
    cursorAgentService.currentSessionId = summary.SessionGuid;
    setHasAgent(!!(session.CursorAgentId || summary.CursorAgentId));
    const appId = Number(session.SaasApplicationId ?? session.saasApplicationId ?? 0);
    const dsId = Number(session.DataSourceRegisterId ?? session.dataSourceRegisterId ?? 0);
    const loadedSkill = session.SkillKey ?? session.skillKey;
    if (appId > 0) setSaasApplicationId(appId);
    if (dsId > 0) setDataSourceId(dsId);
    if (loadedSkill) setSkillKey(loadedSkill);
    const hist = session.ConversationHistory ?? session.conversationHistory ?? [];
    const mapped = mapHistoryMessages(hist);
    const final = String(session.FinalResponse ?? session.finalResponse ?? '').trim();
    const last = mapped[mapped.length - 1];
    if (last?.role === 'assistant' && !last.content && final) last.content = final;
    setMessages(mapped);
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
      setWorkspaceOpen(true);
    } catch (err: any) {
      setError(err?.message ?? 'Failed to read file');
    }
  }, [sessionId]);

  const runStartBuild = useCallback(async (relativePath: string) => {
    if (!sessionId || !saasApplicationId || isBuildingPack) return;
    setIsBuildingPack(true);
    setBuildResult(null);
    setError(null);
    try {
      const text = await readCursorWorkspaceFile(sessionId, relativePath);
      const loaded = await appConfigPackSvc.Load(text);
      const pack = loaded?.Object;
      if (!loaded?.IsSuccessful || !pack) {
        const msg = loaded?.ValidationResult?.Items?.map(i => i.Message).join('; ') || 'Could not load the config pack.';
        setBuildResult(msg);
        setError(msg);
        return;
      }
      const validated = await appConfigPackSvc.Validate(pack);
      const errors = validated?.Object?.Errors ?? [];
      if (validated?.Object && validated.Object.IsValid === false && errors.length) {
        const msg = errors.join('; ');
        setBuildResult(msg);
        setError(msg);
        return;
      }
      const result = await appConfigPackSvc.Execute(pack, saasApplicationId);
      const exec = result.Object;
      if (!result.IsSuccessful || !exec?.IsSuccess) {
        const msg = exec?.ErrorMessage
          || result.ValidationResult?.Items?.map(i => i.Message).join('; ')
          || 'Start Build failed.';
        setBuildResult(msg);
        setError(msg);
        return;
      }
      try { await refreshUserTreeMenu(); } catch { /* non-blocking */ }
      const summary = `Import completed. Tables created: ${exec.TablesCreated ?? 0}, columns added: ${exec.ColumnsAdded ?? 0}, TX inserted: ${exec.TransactionsInserted ?? 0}, TX updated: ${exec.TransactionsUpdated ?? 0}, searches inserted: ${exec.SearchesInserted ?? 0}.`;
      setBuildResult(summary);
    } catch (err: any) {
      const msg = err?.message ?? 'Start Build failed.';
      setBuildResult(msg);
      setError(msg);
    } finally {
      setIsBuildingPack(false);
    }
  }, [isBuildingPack, saasApplicationId, sessionId]);

  const chatTitle = (() => {
    const fromList = chatHistory.find(c => c.SessionGuid === sessionId);
    const fromMessages = messages.find(m => m.role === 'user')?.content;
    const text = (fromList ? cursorChatTitle(fromList) : fromMessages || '').trim();
    if (!text) return 'New Chat';
    return text;
  })();

  if (!isAdmin) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <div className={`text-sm ${theme.label}`}>Administrator access is required for App Data Integration Agent.</div>
      </div>
    );
  }

  const workspaceFiles = files.filter(f => !f.IsDirectory);
  const packFiles = workspaceFiles.filter(f => isAppConfigPackPath(workspaceFilePath(f)));
  const lastAssistantIndex = messages.reduce((acc, m, idx) => (m.role === 'assistant' ? idx : acc), -1);
  const contextLocked = isRunning;
  const currentStatus = chatHistory.find(c => c.SessionGuid === sessionId)?.Status ?? '';
  const canResume = hasAgent && !isRunning && !!sessionId
    && currentStatus.toLowerCase() !== 'completed';

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {error && (
        <div className={`px-3 py-1 text-xs mb-1 ${theme.mainContentSection} ${theme.label}`}>{error}</div>
      )}

      <div ref={layoutRef} className="w-full h-[200px] flex-auto overflow-hidden flex gap-1">
          <div className="relative shrink-0 flex flex-col" style={{ width: chatListWidth }}>
            <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
              <span className={`text-xs font-semibold ${theme.title}`}>Chats</span>
              <button
                type="button"
                onClick={handleNewChat}
                disabled={isRunning}
                className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                title="New chat"
              >
                <i className="fa-solid fa-plus" />
              </button>
            </div>
            <div className={`h-1 flex-auto overflow-y-auto py-1 ${theme.mainContentSection}`}>
              <div
                onClick={handleNewChat}
                className={`cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-[4px] ${
                  !sessionId ? theme.sideBar_menu_active : theme.sideBar_menu
                }`}
              >
                <div className="text-xs font-medium truncate">New Chat</div>
              </div>
              {chatHistory.map(item => (
                <div
                  key={item.SessionGuid}
                  onClick={() => handleLoadSession(item)}
                  className={`group cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-[4px] flex items-start ${
                    sessionId === item.SessionGuid ? theme.sideBar_menu_active : theme.sideBar_menu
                  }`}
                >
                  <div className="w-1 flex-auto min-w-0">
                    <div className="text-xs font-medium truncate">{cursorChatTitle(item).slice(0, 55)}</div>
                    <div className={`text-[10px] ${theme.label}`}>{item.Status}</div>
                  </div>
                  <button
                    type="button"
                    title="More Options"
                    className={`ml-1 shrink-0 w-6 h-6 rounded-[4px] text-xs ${theme.menu_default} ${
                      chatMenu.visible && chatMenu.item?.SessionGuid === item.SessionGuid
                        ? 'opacity-100'
                        : 'opacity-0 group-hover:opacity-100 focus:opacity-100'
                    }`}
                    onClick={e => {
                      e.preventDefault();
                      e.stopPropagation();
                      const rect = e.currentTarget.getBoundingClientRect();
                      setChatMenu({ visible: true, x: rect.right, y: rect.top, item });
                    }}
                  >
                    <i className="fa-solid fa-ellipsis-vertical" />
                  </button>
                </div>
              ))}
            </div>
            <PanelResizeHandle edge="right" label="Resize chat list" onMouseDown={startResize('chat')} />
          </div>

          <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
            <div className={`flex items-center justify-between px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
              <div className={`text-md font-semibold truncate ${theme.title}`} title={chatTitle}>
                {chatTitle}
              </div>
              <div className="flex items-center space-x-2 shrink-0 ml-2">
                {canResume && (
                  <button
                    type="button"
                    onClick={handleResume}
                    disabled={isRunning}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                    title="Continue this chat from where it stopped (failed, cancelled, or interrupted)."
                  >
                    Resume
                  </button>
                )}
                {isRunning && (
                  <button type="button" onClick={() => cursorAgentService.cancel()} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                    Cancel
                  </button>
                )}
                {workspaceFiles.length > 0 && (
                  <button
                    type="button"
                    onClick={() => setWorkspaceOpen(v => !v)}
                    className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
                    title="Workspace"
                  >
                    <i className="fa-solid fa-folder-open mr-1" />Workspace
                  </button>
                )}
              </div>
            </div>

            <div className={`h-1 flex-auto overflow-auto ${theme.mainContentSection}`}>
              {messages.length === 0 ? (
                <div className="h-full w-full flex items-center justify-center px-5">
                  <div className={`max-w-xl text-center text-sm ${theme.label}`}>
                    Ask AI to build your application. Choose a skill below, then send a message.
                  </div>
                </div>
              ) : (
                <div className="w-full max-w-[720px] mx-auto px-5 py-6">
                  {messages.map((msg, i) => (
                    <div key={i} className={msg.role === 'user' ? 'mb-4' : 'mb-8'}>
                      {msg.role === 'user' ? (
                        <UserMessageBlock
                          msg={msg}
                          disabled={isRunning}
                          isEditing={editingIndex === i}
                          editText={editText}
                          onEditText={setEditText}
                          onStartEdit={() => {
                            if (isRunning) return;
                            setEditingIndex(i);
                            setEditText(msg.content || msg.streamingContent || '');
                          }}
                          onCancelEdit={() => { setEditingIndex(null); setEditText(''); }}
                          onSaveEdit={handleSaveEdit}
                        />
                      ) : (
                        <div className={`w-full text-sm leading-relaxed whitespace-pre-wrap ${t('text_title')}`}>
                          {msg.isStreaming && (
                            <>
                              <WorkingStatus label={pendingGate ? 'Waiting for confirmation' : 'Thinking'} />
                              {msg.steps.length > 0 && (
                                <div className={`mb-3 text-xs ${theme.label}`}>
                                  {msg.steps.slice(-8).map((s, idx) => (
                                    <div key={idx} className="truncate">
                                      <i className="fa-solid fa-gear mr-1" />
                                      {s.Description || (s as any).description}
                                    </div>
                                  ))}
                                </div>
                              )}
                            </>
                          )}
                          <AssistantBody text={msg.content || msg.streamingContent || ''} />
                          {!msg.isStreaming && !(msg.content || msg.streamingContent) && (
                            <div className={theme.label}>No reply received.</div>
                          )}
                          {!msg.isStreaming && i === lastAssistantIndex && packFiles.length > 0 && (
                            <div className={`mt-4 px-3 py-3 rounded-[4px] border text-xs ${theme.inputBox}`}>
                              <div className={`font-semibold mb-1 ${theme.title}`}>Config file completed</div>
                              <div className={`mb-2 ${theme.label}`}>
                                A draft pack is ready in the workspace. Review it, then click Start Build to import into the selected Application.
                              </div>
                              {packFiles.map(f => {
                                const path = workspaceFilePath(f);
                                return (
                                  <div key={path} className="flex items-center mb-1">
                                    <button
                                      type="button"
                                      className={`text-left w-1 flex-auto truncate underline ${theme.label}`}
                                      onClick={() => openPreview(path)}
                                      title={path}
                                    >
                                      {path}
                                    </button>
                                  </div>
                                );
                              })}
                              {buildResult && (
                                <div className={`mt-2 ${theme.label}`}>{buildResult}</div>
                              )}
                              <div className="flex items-center mt-2">
                                <button
                                  type="button"
                                  disabled={isBuildingPack || isRunning || !saasApplicationId}
                                  className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary} disabled:opacity-50 disabled:cursor-not-allowed`}
                                  onClick={() => setConfirmPackPath(workspaceFilePath(packFiles[packFiles.length - 1]))}
                                >
                                  {isBuildingPack ? 'Building…' : 'Start Build'}
                                </button>
                              </div>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                  ))}
                  {pendingGate && (
                    <div className={`border rounded-[4px] px-3 py-2 text-xs mb-5 ${theme.inputBox}`}>
                      <div className={`font-semibold mb-1 ${theme.title}`}>{pendingGate.Title}</div>
                      <div className={`mb-2 ${theme.label}`}>{pendingGate.Summary}</div>
                      {pendingGate.Sql && <pre className="mb-2 overflow-auto max-h-40">{pendingGate.Sql}</pre>}
                      {pendingGate.RelativePath && <div className="mb-2">Pack: {pendingGate.RelativePath}</div>}
                      <input
                        className={`w-full h-7 px-2 text-xs border mb-2 focus:outline-none ${theme.inputBox}`}
                        placeholder="Feedback if you reject"
                        value={gateFeedback}
                        onChange={e => setGateFeedback(e.target.value)}
                        autoComplete="off"
                      />
                      <div className="flex items-center space-x-2">
                        <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleConfirmGate(true)}>Confirm</button>
                        <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => handleConfirmGate(false)}>Reject</button>
                      </div>
                    </div>
                  )}
                  <div ref={messagesEndRef} />
                </div>
              )}
            </div>

            <div className={`shrink-0 px-5 py-3 mt-1 ${theme.mainContentSection}`}>
              <div className="w-full max-w-3xl mx-auto">
                <div className="flex flex-wrap items-center mb-2">
                  <div className="mr-3 mb-1">
                    <ToolbarField label="Skill">
                      <SkillPicker
                        items={skillItems}
                        value={skillKey}
                        disabled={isRunning}
                        lockSelection={false}
                        onChange={setSkillKey}
                      />
                    </ToolbarField>
                  </div>
                  <div className="mr-3 mb-1">
                    <ToolbarField label="Application">
                      <select
                        className={`h-7 px-2 text-xs border w-40 focus:outline-none ${theme.inputBox}`}
                        value={saasApplicationId ?? ''}
                        onChange={e => setSaasApplicationId(Number(e.target.value) || undefined)}
                        disabled={contextLocked}
                      >
                        <option value="">Select Application</option>
                        {applications.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
                      </select>
                    </ToolbarField>
                  </div>
                  <div className="mb-1">
                    <ToolbarField label="DataSource">
                      <select
                        className={`h-7 px-2 text-xs border w-40 focus:outline-none ${theme.inputBox}`}
                        value={dataSourceId ?? ''}
                        onChange={e => setDataSourceId(Number(e.target.value) || undefined)}
                        disabled={contextLocked}
                      >
                        <option value="">Optional</option>
                        {dataSources.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                      </select>
                    </ToolbarField>
                  </div>
                </div>
                <div className={`flex items-end border rounded-[4px] px-2 py-2 ${theme.inputBox}`}>
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
                    placeholder="Message App Data Integration Agent…"
                    rows={3}
                    className="w-1 flex-auto px-2 py-1 text-xs resize-none border-0 focus:outline-none bg-transparent"
                  />
                  <button
                    type="button"
                    onClick={handleSend}
                    disabled={isRunning || !input.trim()}
                    className={`w-8 h-6 ml-2 shrink-0 ${theme.button_default} rounded-[4px] text-xs`}
                    title="Send"
                  >
                    {isRunning
                      ? <i className="fa-solid fa-circle-notch animate-spin" />
                      : <i className="fa-solid fa-paper-plane" />}
                  </button>
                </div>
              </div>
            </div>
          </div>

          {workspaceOpen && workspaceFiles.length > 0 && (
            <div className="relative shrink-0 flex flex-col" style={{ width: workspaceWidth }}>
              <PanelResizeHandle edge="left" label="Resize workspace" onMouseDown={startResize('workspace')} />
              <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
                  <span className={`text-xs font-semibold ${theme.title}`}>Workspace</span>
                  <button
                    type="button"
                    onClick={() => setWorkspaceOpen(false)}
                    className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                    title="Close workspace"
                  >
                    <i className="fa-solid fa-xmark" />
                  </button>
                </div>
                <div className={`h-1 flex-auto overflow-auto px-2 py-2 ${theme.mainContentSection}`}>
                  {workspaceFiles.map(f => (
                    <button
                      type="button"
                      key={workspaceFilePath(f)}
                      className={`block w-full text-left text-xs px-2 py-1 mb-0.5 rounded-[4px] truncate ${theme.button_default}`}
                      onClick={() => openPreview(workspaceFilePath(f))}
                      title={workspaceFilePath(f)}
                    >
                      {workspaceFilePath(f)}
                    </button>
                  ))}
                </div>
                {previewPath && (
                  <div className={`h-40 overflow-auto mt-1 px-2 py-1 ${theme.mainContentSection}`}>
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
          )}
      </div>

      {chatMenu.visible && chatMenu.item && createPortal(
        <div
          ref={chatMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: chatMenu.x, top: chatMenu.y, zIndex: appHelper.getGlobalOverlayZIndex() }}
          onClick={e => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => { setRenameItem(chatMenu.item); closeChatMenu(); }}
          >
            <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden />Rename
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={async () => {
              const guid = chatMenu.item?.SessionGuid;
              closeChatMenu();
              if (!guid) return;
              await archiveCursorSessions([guid], true);
              refreshHistory();
            }}
          >
            <i className="fa-solid fa-box-archive mr-2 flex-shrink-0" aria-hidden />Archive
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => { setDeleteItem(chatMenu.item); closeChatMenu(); }}
          >
            <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />Delete
          </button>
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
            onClick={() => { closeChatMenu(); setManageOpen(true); }}
          >
            <i className="fa-solid fa-list-check mr-2 flex-shrink-0" aria-hidden />Manage all chats
          </button>
        </div>,
        document.body
      )}

      <RenameChatDialog
        isOpen={!!renameItem}
        initialTitle={renameItem ? cursorChatTitle(renameItem) : ''}
        onCancel={() => setRenameItem(null)}
        onSave={async title => {
          if (!renameItem) return;
          await renameCursorSession(renameItem.SessionGuid, title);
          setRenameItem(null);
          refreshHistory();
        }}
      />
      <Confirm
        isOpen={!!deleteItem}
        title="Delete chat"
        message="Permanently delete this chat? This cannot be undone."
        confirmLabel="Delete"
        confirmButtonStyle={theme.button_default}
        onCancel={() => setDeleteItem(null)}
        onConfirm={async () => {
          const guid = deleteItem?.SessionGuid;
          setDeleteItem(null);
          if (!guid) return;
          await deleteCursorSessions([guid]);
          handleDeletedSessions([guid]);
        }}
      />
      <Confirm
        isOpen={!!confirmPackPath}
        title="Start Build"
        message={`Import ${confirmPackPath || 'this config pack'} into the selected Application? This creates or updates tables, transactions, and searches.`}
        confirmLabel="Start Build"
        confirmButtonStyle={theme.button_secondary}
        onCancel={() => setConfirmPackPath(null)}
        onConfirm={() => {
          const path = confirmPackPath;
          setConfirmPackPath(null);
          if (path) void runStartBuild(path);
        }}
      />
      <CursorAgentChatManagement
        isOpen={manageOpen}
        onClose={() => setManageOpen(false)}
        onChanged={handleDeletedSessions}
      />
    </div>
  );
};

export default CursorAgent;
