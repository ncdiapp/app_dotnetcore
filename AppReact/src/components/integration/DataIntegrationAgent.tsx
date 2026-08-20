import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import {
  AppDataIntegrationAgentDoneEvent,
  AppDataIntegrationAgentFileEvent,
  AppDataIntegrationAgentGateEvent,
  AppDataIntegrationAgentMessage,
  AppDataIntegrationAgentNavigateEvent,
  AppDataIntegrationAgentSessionSummary,
  AppDataIntegrationAgentSkillMenuItem,
  AppDataIntegrationAgentStepEvent,
  AppDataIntegrationAgentTablePreviewEvent,
  AppDataIntegrationAgentWorkspaceFile,
  archiveAppDataIntegrationAgentSessions,
  appDataIntegrationAgentService,
  appDataIntegrationAgentChatTitle,
  deleteAppDataIntegrationAgentSessions,
  getAppDataIntegrationAgentSession,
  getRecentAppDataIntegrationAgentSessions,
  listAppDataIntegrationAgentSkillMenu,
  listAppDataIntegrationAgentWorkspaceFiles,
  readAppDataIntegrationAgentWorkspaceFile,
  renameAppDataIntegrationAgentSession,
} from '../../webapi/appDataIntegrationAgentSvc';
import { endpoints } from '../../webapi/endpoints';
import { isAdminUserFromContext } from '../../helper/adminPermissionHelper';
import {
  buildParamObjFromRouteCodeAndLink,
  getReactPathForRouteCode,
} from '../../helper/navigationHelper';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import Confirm from '../common/Confirm';
import appHelper from '../../helper/appHelper';
import { useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';
import TablesDataPreviewModal, { type TablePreviewItem } from '../transaction/TablesDataPreviewModal';
import DataIntegrationAgentChatManagement, { RenameChatDialog } from './DataIntegrationAgentChatManagement';
import DataIntegrationAgentStartBuildDialog from './DataIntegrationAgentStartBuildDialog';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: AppDataIntegrationAgentStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
  timestamp?: string;
  /** Pack paths written during this assistant turn (for Start Build UI). */
  writtenPackPaths?: string[];
  /** Open-page / table-preview offers (user must click Open — never auto-open). */
  openUiOffers?: OpenUiOffer[];
}

type OpenUiOffer =
  | {
      id: string;
      kind: 'table_preview';
      label: string;
      tables: TablePreviewItem[];
    }
  | {
      id: string;
      kind: 'navigate';
      label: string;
      routeCode: string;
      link?: string | null;
      paramObj: Record<string, unknown>;
    };

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

function workspaceFilePath(f: AppDataIntegrationAgentWorkspaceFile): string {
  return String(f?.RelativePath ?? (f as any)?.relativePath ?? '');
}

function isAppConfigPackPath(path: string): boolean {
  return /\.appConfigPack\.json$/i.test(path || '');
}

function normalizeWorkspacePath(path: string): string {
  return String(path || '').trim().replace(/\\/g, '/');
}

function extractRelativePathFromJsonish(text: string): string | null {
  if (!text) return null;
  const m = text.match(/"relativePath"\s*:\s*"([^"]+)"/i)
    || text.match(/'relativePath'\s*:\s*'([^']+)'/i);
  return m ? normalizeWorkspacePath(m[1]) : null;
}

function extractAppConfigPackPathsFromText(text: string): string[] {
  if (!text) return [];
  const found: string[] = [];
  const re = /[A-Za-z0-9_./-]+\.appConfigPack\.json/gi;
  let m: RegExpExecArray | null;
  while ((m = re.exec(text)) != null) {
    const p = normalizeWorkspacePath(m[0]);
    if (isAppConfigPackPath(p)) found.push(p);
  }
  return found;
}

/**
 * Packs actually touched this turn via tools — NOT free-text mentions.
 * write = generated; validate/preview = ready for Start Build.
 */
function extractAppConfigPackPathsFromSteps(steps: AppDataIntegrationAgentStepEvent[]): string[] {
  const found: string[] = [];
  for (const s of steps || []) {
    const tool = String(s.ToolName ?? (s as any).toolName ?? '').toLowerCase();
    const desc = String(s.Description ?? (s as any).description ?? '').toLowerCase();
    const details = String(s.Details ?? (s as any).details ?? '');
    const isWrite = tool.includes('write_workspace_file') || desc.includes('write_workspace_file');
    const isValidate = tool.includes('validate_config_pack') || desc.includes('validate_config_pack');
    const isPreview = tool.includes('preview_config_pack') || desc.includes('preview_config_pack');
    const isPropose = tool.includes('propose_import_pack') || desc.includes('propose_import_pack');
    if (!isWrite && !isValidate && !isPreview && !isPropose) continue;
    if (s.IsSuccess === false) continue;

    const fromArg = extractRelativePathFromJsonish(details)
      || extractRelativePathFromJsonish(String(s.Description ?? (s as any).description ?? ''));
    if (fromArg && isAppConfigPackPath(fromArg)) found.push(fromArg);
    // Only parse pack paths out of tool args/details for these tools — never from thinking text.
    found.push(...extractAppConfigPackPathsFromText(details));
  }
  return found;
}

function uniquePackPaths(paths: string[]): string[] {
  const map = new Map<string, string>();
  for (const raw of paths) {
    const p = normalizeWorkspacePath(raw);
    if (!isAppConfigPackPath(p)) continue;
    map.set(p.toLowerCase(), p);
  }
  return Array.from(map.values());
}

/** Start Build only when this turn wrote/validated/previewed a pack (not when text merely mentions a path). */
function packPathsForStartBuild(
  msg: ChatMessage,
  workspacePackPathSet: Set<string>
): string[] {
  const fromTools = uniquePackPaths([
    ...(msg.writtenPackPaths ?? []),
    ...extractAppConfigPackPathsFromSteps(msg.steps),
  ]);
  const candidates = fromTools.length > 0
    ? fromTools
    : inferLegacyReadyPackPaths(msg.content || '', workspacePackPathSet);

  return candidates.filter(p => {
    const key = p.toLowerCase();
    if (workspacePackPathSet.has(key)) return true;
    return (msg.writtenPackPaths ?? []).some(w => normalizeWorkspacePath(w).toLowerCase() === key);
  });
}

/**
 * Old chats have no WrittenPackPaths. Infer only when the reply looks like a finished pack
 * (not "I'll update after you confirm").
 */
function inferLegacyReadyPackPaths(content: string, workspacePackPathSet: Set<string>): string[] {
  const paths = uniquePackPaths(extractAppConfigPackPathsFromText(content));
  if (paths.length === 0) return [];
  if (looksLikePackPlanningOnly(content)) return [];
  if (!looksLikePackReadyForBuild(content)) return [];
  return paths.filter(p => workspacePackPathSet.has(p.toLowerCase()));
}

function looksLikePackPlanningOnly(content: string): boolean {
  return /确认后我会|确认后我再|确认后再|after\s+confirmation|after\s+you\s+confirm|i('?ll| will)\s+update|i('?ll| will)\s+write|before\s+i\s+write|请先确认|先确认以下|是否加|是否创建|or just (a )?read-only/i
    .test(content || '');
}

function looksLikePackReadyForBuild(content: string): boolean {
  return /pack\s+is\s+ready|click\s+start\s+build|点\s*start\s*build|validate\s*:\s*ok|preview\s*:\s*insert|可以导入|已就绪|config\s+file\s+completed|start\s+build\s+in\s+this\s+chat|validate\s*[&＆]\s*preview|wrote\s+packs\//i
    .test(content || '');
}

function mapOpenUiOffersFromHistory(raw: any): OpenUiOffer[] {
  const list = raw?.OpenUiOffers ?? raw?.openUiOffers;
  if (!Array.isArray(list) || list.length === 0) return [];
  const offers: OpenUiOffer[] = [];
  list.forEach((o: any, idx: number) => {
    const kind = String(o?.Kind ?? o?.kind ?? '').toLowerCase();
    const label = String(o?.Label ?? o?.label ?? '');
    if (kind === 'table_preview' || kind === 'tablepreview') {
      const tablesRaw = o?.Tables ?? o?.tables ?? [];
      const tables: TablePreviewItem[] = (Array.isArray(tablesRaw) ? tablesRaw : [])
        .map((t: any) => ({
          tableName: String(t?.TableName ?? t?.tableName ?? '').trim(),
          dataSourceId: (t?.DataSourceId ?? t?.dataSourceId ?? null) as number | null,
          schemaOwner: (t?.SchemaOwner ?? t?.schemaOwner ?? 'dbo') as string | null,
        }))
        .filter((t: TablePreviewItem) => !!t.tableName);
      if (tables.length === 0) return;
      offers.push({
        id: `hist-preview-${idx}-${tables.map(t => t.tableName).join(',')}`,
        kind: 'table_preview',
        label: label || tables.map(t => t.tableName).join(', '),
        tables,
      });
      return;
    }
    if (kind === 'navigate' || kind === 'page') {
      const routeCode = String(o?.RouteCode ?? o?.routeCode ?? '').trim();
      if (!routeCode) return;
      const link = o?.Link ?? o?.link;
      const rawParams = (o?.ParamObj ?? o?.paramObj) as Record<string, unknown> | undefined;
      const paramObj =
        rawParams && typeof rawParams === 'object'
          ? rawParams
          : buildParamObjFromRouteCodeAndLink(routeCode, link != null ? String(link) : '');
      offers.push({
        id: `hist-nav-${idx}-${routeCode}`,
        kind: 'navigate',
        label: label || routeCode,
        routeCode,
        link: link != null ? String(link) : null,
        paramObj,
      });
    }
  });
  return offers;
}

function mapHistoryMessages(hist: any[]): ChatMessage[] {
  return (hist || []).map((m: any) => ({
    role: historyRole(m),
    content: historyText(m),
    steps: [],
    streamingContent: '',
    isStreaming: false,
    timestamp: historyTimestamp(m),
    writtenPackPaths: uniquePackPaths(
      (m?.WrittenPackPaths ?? m?.writtenPackPaths ?? []) as string[]
    ),
    openUiOffers: mapOpenUiOffersFromHistory(m),
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
const PREVIEW_DEFAULT_PX = 160;
const PREVIEW_MIN_PX = 80;
const PREVIEW_MAX_PX = 480;
const CENTER_MIN_PX = 280;

/** Remember last selected chat so reopening the page restores it (not New Chat). */
const LAST_SESSION_STORAGE_KEY = 'appai.appDataIntegrationAgent.lastSessionGuid';

function readLastSessionGuid(): string | null {
  try {
    const v = localStorage.getItem(LAST_SESSION_STORAGE_KEY);
    return v && v.trim() ? v.trim() : null;
  } catch {
    return null;
  }
}

function writeLastSessionGuid(sessionGuid: string | null) {
  try {
    if (!sessionGuid) localStorage.removeItem(LAST_SESSION_STORAGE_KEY);
    else localStorage.setItem(LAST_SESSION_STORAGE_KEY, sessionGuid);
  } catch { /* ignore */ }
}

interface PanelResizeHandleProps {
  label: string;
  edge: 'right' | 'left' | 'top';
  onMouseDown: (e: React.MouseEvent) => void;
}

const PanelResizeHandle: React.FC<PanelResizeHandleProps> = ({ label, edge, onMouseDown }) => {
  const isHorizontal = edge === 'top';
  return (
    <div
      role="separator"
      aria-orientation={isHorizontal ? 'horizontal' : 'vertical'}
      aria-label={label}
      title="Drag to resize"
      onMouseDown={onMouseDown}
      className={`absolute z-10 select-none ${
        isHorizontal
          ? 'left-0 right-0 top-0 -mt-1 h-2 cursor-row-resize'
          : `top-0 bottom-0 w-2 cursor-col-resize ${edge === 'right' ? 'right-0 -mr-1' : 'left-0 -ml-1'}`
      }`}
    />
  );
};

const itemKey = (i: AppDataIntegrationAgentSkillMenuItem) => i.Key;
const itemLabel = (i: AppDataIntegrationAgentSkillMenuItem) => i.Label;
const itemGroup = (i: AppDataIntegrationAgentSkillMenuItem) => i.Group;

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
  items: AppDataIntegrationAgentSkillMenuItem[];
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

  const categories: { id: string; label: string; leafKey?: string; children: AppDataIntegrationAgentSkillMenuItem[] }[] = [
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
      const portal = document.getElementById('data-integration-agent-skill-menu');
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
          id="data-integration-agent-skill-menu"
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

const DataIntegrationAgent: React.FC = () => {
  const { theme, t } = useTheme();
  const userContext = useSelector((s: RootState) => s.userSession.userContext);
  const isAdmin = isAdminUserFromContext(userContext);
  const { addTabAndNavigate } = useTabNavigation();

  const [applications, setApplications] = useState<{ id: number; name: string }[]>([]);
  const [dataSources, setDataSources] = useState<{ id: number; name: string }[]>([]);
  const [saasApplicationId, setSaasApplicationId] = useState<number | undefined>();
  const [dataSourceId, setDataSourceId] = useState<number | undefined>();
  const [skillKey, setSkillKey] = useState('app-config-builder');
  const [skillItems, setSkillItems] = useState<AppDataIntegrationAgentSkillMenuItem[]>([]);

  const [sessionId, setSessionId] = useState<string | null>(null);
  const [hasAgent, setHasAgent] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [isRunning, setIsRunning] = useState(false);
  const isRunningRef = useRef(false);
  const [error, setError] = useState<string | null>(null);
  const [pendingGate, setPendingGate] = useState<AppDataIntegrationAgentGateEvent | null>(null);
  const [gateFeedback, setGateFeedback] = useState('');
  const [tablePreviewOpen, setTablePreviewOpen] = useState(false);
  const [tablePreviewTables, setTablePreviewTables] = useState<TablePreviewItem[]>([]);
  const [chatHistory, setChatHistory] = useState<AppDataIntegrationAgentSessionSummary[]>([]);
  const [files, setFiles] = useState<AppDataIntegrationAgentWorkspaceFile[]>([]);
  const [previewPath, setPreviewPath] = useState<string | null>(null);
  const [previewContent, setPreviewContent] = useState('');
  const [previewHeight, setPreviewHeight] = useState(PREVIEW_DEFAULT_PX);
  const [previewCopyHint, setPreviewCopyHint] = useState<string | null>(null);
  const [workspaceOpen, setWorkspaceOpen] = useState(false);
  const [chatListWidth, setChatListWidth] = useState(CHAT_LIST_DEFAULT_PX);
  const [workspaceWidth, setWorkspaceWidth] = useState(WORKSPACE_DEFAULT_PX);
  const layoutRef = useRef<HTMLDivElement | null>(null);
  const workspacePanelRef = useRef<HTMLDivElement | null>(null);
  const panelDragRef = useRef<'chat' | 'workspace' | 'preview' | null>(null);
  const chatListWidthRef = useRef(CHAT_LIST_DEFAULT_PX);
  const workspaceWidthRef = useRef(WORKSPACE_DEFAULT_PX);
  const previewHeightRef = useRef(PREVIEW_DEFAULT_PX);
  const workspaceOpenRef = useRef(false);
  const [chatMenu, setChatMenu] = useState<{ visible: boolean; x: number; y: number; item: AppDataIntegrationAgentSessionSummary | null }>({
    visible: false, x: 0, y: 0, item: null,
  });
  const [renameItem, setRenameItem] = useState<AppDataIntegrationAgentSessionSummary | null>(null);
  const [deleteItem, setDeleteItem] = useState<AppDataIntegrationAgentSessionSummary | null>(null);
  const [manageOpen, setManageOpen] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editText, setEditText] = useState('');
  const [buildPackPath, setBuildPackPath] = useState<string | null>(null);
  const chatMenuRef = useRef<HTMLDivElement | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const didAutoRestoreRef = useRef(false);

  chatListWidthRef.current = chatListWidth;
  workspaceWidthRef.current = workspaceWidth;
  previewHeightRef.current = previewHeight;
  workspaceOpenRef.current = workspaceOpen;

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      const kind = panelDragRef.current;
      if (!kind) return;
      if (kind === 'preview') {
        const panelRect = workspacePanelRef.current?.getBoundingClientRect();
        if (!panelRect) return;
        const next = Math.min(
          PREVIEW_MAX_PX,
          Math.max(PREVIEW_MIN_PX, panelRect.bottom - e.clientY)
        );
        setPreviewHeight(next);
        return;
      }
      const rect = layoutRef.current?.getBoundingClientRect();
      if (!rect) return;
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

  const startResize = useCallback((kind: 'chat' | 'workspace' | 'preview') => (e: React.MouseEvent) => {
    e.preventDefault();
    panelDragRef.current = kind;
    document.body.style.cursor = kind === 'preview' ? 'row-resize' : 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const refreshHistory = useCallback(() => {
    return getRecentAppDataIntegrationAgentSessions(30).then(list => {
      setChatHistory(list);
      return list;
    }).catch(() => {
      setChatHistory([]);
      return [] as AppDataIntegrationAgentSessionSummary[];
    });
  }, []);

  const refreshFiles = useCallback((sid: string | null) => {
    if (!sid) { setFiles([]); return; }
    listAppDataIntegrationAgentWorkspaceFiles(sid).then(list => {
      setFiles(list);
      if (list.some(f => !f.IsDirectory)) setWorkspaceOpen(true);
    }).catch(() => setFiles([]));
  }, []);

  const resetChatUi = useCallback(() => {
    appDataIntegrationAgentService.disconnect();
    appDataIntegrationAgentService.currentSessionId = null;
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
    setBuildPackPath(null);
    setPreviewCopyHint(null);
  }, []);

  const applyLoadedSession = useCallback(async (summary: AppDataIntegrationAgentSessionSummary) => {
    resetChatUi();
    const session = await getAppDataIntegrationAgentSession(summary.SessionGuid);
    if (!session) return false;
    setSessionId(summary.SessionGuid);
    appDataIntegrationAgentService.currentSessionId = summary.SessionGuid;
    writeLastSessionGuid(summary.SessionGuid);
    setHasAgent(!!(session.CloudAgentId || summary.CloudAgentId));
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
    return true;
  }, [refreshFiles, resetChatUi]);

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
    listAppDataIntegrationAgentSkillMenu().then(menu => {
      setSkillItems(menu.Items ?? []);
      if (menu.DefaultKey) setSkillKey(prev => prev || menu.DefaultKey);
    }).catch(() => {});

    void (async () => {
      const list = await refreshHistory();
      if (didAutoRestoreRef.current) return;
      didAutoRestoreRef.current = true;
      if (!list.length) return;
      const lastId = readLastSessionGuid();
      const target =
        (lastId ? list.find(s => s.SessionGuid === lastId) : null)
        ?? list[0];
      if (target) await applyLoadedSession(target);
    })();
  }, [applyLoadedSession, refreshHistory]);

  useEffect(() => {
    if (sessionId) writeLastSessionGuid(sessionId);
  }, [sessionId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, pendingGate]);

  useEffect(() => () => { appDataIntegrationAgentService.disconnect(); }, []);

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
    onStep: (step: AppDataIntegrationAgentStepEvent) => {
      updateLastAssistant(msg => ({ ...msg, steps: [...msg.steps, step] }));
    },
    onToken: (token: string) => {
      updateLastAssistant(msg => ({ ...msg, streamingContent: (msg.streamingContent || '') + token }));
    },
    onFile: (file: AppDataIntegrationAgentFileEvent) => {
      refreshFiles(appDataIntegrationAgentService.currentSessionId);
      const path = normalizeWorkspacePath(file?.RelativePath ?? (file as any)?.relativePath ?? '');
      const action = String(file?.Action ?? (file as any)?.action ?? '').toLowerCase();
      if (!path || !isAppConfigPackPath(path)) return;
      if (action === 'delete') {
        updateLastAssistant(msg => ({
          ...msg,
          writtenPackPaths: (msg.writtenPackPaths ?? []).filter(
            p => normalizeWorkspacePath(p).toLowerCase() !== path.toLowerCase()
          ),
        }));
        return;
      }
      // write | artifact | empty — any non-delete pack event counts for Start Build
      updateLastAssistant(msg => {
        const existing = msg.writtenPackPaths ?? [];
        if (existing.some(p => normalizeWorkspacePath(p).toLowerCase() === path.toLowerCase())) return msg;
        return { ...msg, writtenPackPaths: [...existing, path] };
      });
    },
    onGate: (gate: AppDataIntegrationAgentGateEvent) => setPendingGate(gate),
    onNavigate: (nav: AppDataIntegrationAgentNavigateEvent) => {
      const routeCode = String(nav?.RouteCode ?? nav?.routeCode ?? '').trim();
      if (!routeCode) return;
      const label = String(nav?.Label ?? nav?.label ?? routeCode);
      const link = nav?.Link ?? nav?.link;
      const rawParams = (nav?.ParamObj ?? nav?.paramObj) as Record<string, unknown> | undefined;
      const paramObj =
        rawParams && typeof rawParams === 'object'
          ? rawParams
          : buildParamObjFromRouteCodeAndLink(routeCode, link != null ? String(link) : '');
      const offer: OpenUiOffer = {
        id: `nav-${Date.now()}-${routeCode}`,
        kind: 'navigate',
        label,
        routeCode,
        link: link != null ? String(link) : null,
        paramObj,
      };
      updateLastAssistant(msg => ({
        ...msg,
        openUiOffers: [...(msg.openUiOffers ?? []), offer],
      }));
    },
    onTablePreview: (preview: AppDataIntegrationAgentTablePreviewEvent) => {
      const raw = preview?.Tables ?? preview?.tables ?? [];
      const tables: TablePreviewItem[] = (Array.isArray(raw) ? raw : [])
        .map((t) => ({
          tableName: String(t?.TableName ?? t?.tableName ?? '').trim(),
          dataSourceId: (t?.DataSourceId ?? t?.dataSourceId ?? null) as number | null,
          schemaOwner: (t?.SchemaOwner ?? t?.schemaOwner ?? 'dbo') as string | null,
        }))
        .filter((t) => !!t.tableName);
      if (tables.length === 0) return;
      const names = tables.map(t => t.tableName).join(', ');
      const offer: OpenUiOffer = {
        id: `preview-${Date.now()}-${names}`,
        kind: 'table_preview',
        label: names || 'Table Preview',
        tables,
      };
      updateLastAssistant(msg => ({
        ...msg,
        openUiOffers: [...(msg.openUiOffers ?? []), offer],
      }));
    },
    onDone: (result: AppDataIntegrationAgentDoneEvent) => {
      try {
        setPendingGate(null);
        const final = String((result as any)?.FinalResponse ?? (result as any)?.finalResponse ?? '').trim();
        const hist = (result as any)?.UpdatedHistory ?? (result as any)?.updatedHistory ?? [];
        const fromHist = Array.isArray(hist) && hist.length
          ? historyText(hist[hist.length - 1])
          : '';
        updateLastAssistant(msg => {
          const content = final || msg.streamingContent || fromHist || msg.content || '';
          const fromHistMsg = Array.isArray(hist) && hist.length ? hist[hist.length - 1] : null;
          const histPacks = (fromHistMsg?.WrittenPackPaths ?? fromHistMsg?.writtenPackPaths ?? []) as string[];
          const touched = uniquePackPaths([
            ...(msg.writtenPackPaths ?? []),
            ...extractAppConfigPackPathsFromSteps(msg.steps),
            ...histPacks,
          ]);
          const fromDone = mapOpenUiOffersFromHistory({
            OpenUiOffers: (result as any)?.OpenUiOffers ?? (result as any)?.openUiOffers,
          });
          const fromHistOffers = mapOpenUiOffersFromHistory(fromHistMsg);
          const openUiOffers =
            (msg.openUiOffers?.length ? msg.openUiOffers : null)
            ?? (fromDone.length ? fromDone : null)
            ?? (fromHistOffers.length ? fromHistOffers : null)
            ?? [];
          return {
            ...msg,
            content: content || 'No reply received.',
            streamingContent: '',
            isStreaming: false,
            writtenPackPaths: touched,
            openUiOffers,
          };
        });
        refreshHistory();
        refreshFiles(appDataIntegrationAgentService.currentSessionId);
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
  }), [addTabAndNavigate, refreshFiles, refreshHistory, updateLastAssistant]);

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
        const sid = await appDataIntegrationAgentService.startSession(
          text, saasApplicationId, dataSourceId, [], makeHandlers(), skillKey);
        setSessionId(sid);
        setHasAgent(true);
        refreshFiles(sid);
        refreshHistory();
      } else {
        await appDataIntegrationAgentService.followUp(text, makeHandlers(), skillKey, saasApplicationId, dataSourceId);
      }
    } catch (err: any) {
      const errMsg = err?.message ?? 'Unknown error';
      setError(errMsg);
      updateLastAssistant(msg => ({ ...msg, content: `Failed: ${errMsg}`, isStreaming: false }));
      setIsRunning(false);
      isRunningRef.current = false;
    }
  }, [dataSourceId, hasAgent, makeHandlers, refreshFiles, refreshHistory, saasApplicationId, sessionId, skillKey, updateLastAssistant]);

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
    writeLastSessionGuid(null);
    resetChatUi();
    setSkillKey('app-config-builder');
    setSaasApplicationId(pickDefaultApplicationId(applications));
    setDataSourceId(dataSources[0]?.id);
  }, [applications, dataSources, resetChatUi]);

  const handleDeletedSessions = useCallback((ids?: string[]) => {
    refreshHistory();
    if (ids?.some(id => id === sessionId)) {
      handleNewChat();
    }
  }, [handleNewChat, refreshHistory, sessionId]);

  const handleLoadSession = useCallback(async (summary: AppDataIntegrationAgentSessionSummary) => {
    await applyLoadedSession(summary);
  }, [applyLoadedSession]);

  const handleResume = useCallback(async () => {
    if (!sessionId || isRunning) return;
    setIsRunning(true);
    const assistantMsg: ChatMessage = { role: 'assistant', content: '', steps: [], streamingContent: '', isStreaming: true };
    setMessages(prev => [...prev, assistantMsg]);
    try {
      await appDataIntegrationAgentService.resume(sessionId, 'Continue from where we left off.', makeHandlers());
    } catch (err: any) {
      setError(err?.message ?? 'Resume failed');
      setIsRunning(false);
    }
  }, [isRunning, makeHandlers, sessionId]);

  const handleConfirmGate = useCallback((confirmed: boolean) => {
    if (!pendingGate) return;
    appDataIntegrationAgentService.confirmGate(pendingGate.GateId, confirmed, confirmed ? undefined : gateFeedback);
    setPendingGate(null);
    setGateFeedback('');
  }, [gateFeedback, pendingGate]);

  const handleOpenUiOffer = useCallback((offer: OpenUiOffer) => {
    if (offer.kind === 'table_preview') {
      setTablePreviewTables(offer.tables);
      setTablePreviewOpen(true);
      return;
    }
    addTabAndNavigate(
      getReactPathForRouteCode(offer.routeCode),
      offer.label,
      offer.paramObj
    );
  }, [addTabAndNavigate]);

  const openPreview = useCallback(async (relativePath: string) => {
    if (!sessionId) return;
    try {
      const content = await readAppDataIntegrationAgentWorkspaceFile(sessionId, relativePath);
      setPreviewPath(relativePath);
      setPreviewContent(content);
      setPreviewCopyHint(null);
      setWorkspaceOpen(true);
    } catch (err: any) {
      setError(err?.message ?? 'Failed to read file');
    }
  }, [sessionId]);

  const copyPreview = useCallback(async () => {
    if (!previewPath) return;
    try {
      if (isImagePath(previewPath)) {
        const file = files.find(f => workspaceFilePath(f) === previewPath || f.RelativePath === previewPath);
        const url = toAppFileUrl(file?.PublicUrl);
        if (!url) throw new Error('Image URL not available');
        const resp = await fetch(url);
        const blob = await resp.blob();
        if (typeof ClipboardItem !== 'undefined' && navigator.clipboard?.write) {
          await navigator.clipboard.write([new ClipboardItem({ [blob.type || 'image/png']: blob })]);
        } else {
          await navigator.clipboard.writeText(url);
        }
      } else {
        await navigator.clipboard.writeText(previewContent || '');
      }
      setPreviewCopyHint('Copied');
      window.setTimeout(() => setPreviewCopyHint(null), 1500);
    } catch (err: any) {
      setPreviewCopyHint(err?.message || 'Copy failed');
      window.setTimeout(() => setPreviewCopyHint(null), 2000);
    }
  }, [files, previewContent, previewPath]);

  const chatTitle = (() => {
    const fromList = chatHistory.find(c => c.SessionGuid === sessionId);
    const fromMessages = messages.find(m => m.role === 'user')?.content;
    const text = (fromList ? appDataIntegrationAgentChatTitle(fromList) : fromMessages || '').trim();
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
  const workspacePackPathSet = new Set(
    workspaceFiles
      .map(f => normalizeWorkspacePath(workspaceFilePath(f)))
      .filter(isAppConfigPackPath)
      .map(p => p.toLowerCase())
  );
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
            <div className={`flex items-center justify-between px-3 h-10 shrink-0 mb-1 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold truncate ${theme.title}`}>Chats</span>
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
                    <div className="text-xs font-medium truncate">{appDataIntegrationAgentChatTitle(item).slice(0, 55)}</div>
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
            <div className={`flex items-center justify-between px-3 h-10 shrink-0 mb-1 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold truncate ${theme.title}`} title={chatTitle}>
                {chatTitle}
              </div>
              <div className="flex items-center space-x-2 shrink-0 ml-2">
                {canResume && (
                  <button
                    type="button"
                    onClick={handleResume}
                    disabled={isRunning}
                    className={`px-2 h-6 text-xs rounded-[4px] ${theme.button_default}`}
                    title="Continue this chat from where it stopped (failed, cancelled, or interrupted)."
                  >
                    Resume
                  </button>
                )}
                {isRunning && (
                  <button type="button" onClick={() => appDataIntegrationAgentService.cancel()} className={`px-2 h-6 text-xs rounded-[4px] ${theme.button_default}`}>
                    Cancel
                  </button>
                )}
                {workspaceFiles.length > 0 && (
                  <button
                    type="button"
                    onClick={() => setWorkspaceOpen(v => !v)}
                    className={`px-2 h-6 text-xs rounded-[4px] ${theme.button_secondary}`}
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
                          {(() => {
                            if (msg.isStreaming) return null;
                            const startBuildPacks = packPathsForStartBuild(msg, workspacePackPathSet);
                            if (startBuildPacks.length === 0) return null;
                            return (
                            <div className={`mt-4 px-3 py-3 rounded-[4px] border text-xs ${theme.inputBox}`}>
                              <div className={`font-semibold mb-1 ${theme.title}`}>Config file completed</div>
                              <div className={`mb-2 ${theme.label}`}>
                                Draft pack(s) are ready in the workspace. Open a file to review, then Start Build on that file to validate and import.
                              </div>
                              {startBuildPacks.map(path => (
                                  <div key={path} className="flex items-center gap-2 mb-1">
                                    <button
                                      type="button"
                                      className={`text-left w-1 flex-auto truncate underline ${theme.label}`}
                                      onClick={() => openPreview(path)}
                                      title={path}
                                    >
                                      {path}
                                    </button>
                                    <button
                                      type="button"
                                      disabled={isRunning || !saasApplicationId}
                                      className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_secondary} disabled:opacity-50 disabled:cursor-not-allowed`}
                                      onClick={() => setBuildPackPath(path)}
                                    >
                                      Start Build
                                    </button>
                                  </div>
                              ))}
                            </div>
                            );
                          })()}
                          {(() => {
                            if (msg.isStreaming) return null;
                            const offers = msg.openUiOffers ?? [];
                            if (offers.length === 0) return null;
                            return (
                              <div className="mt-4 space-y-2">
                                {offers.map(offer => (
                                  <div
                                    key={offer.id}
                                    className={`px-3 py-3 rounded-[4px] border text-xs ${theme.inputBox}`}
                                  >
                                    <div className={`font-semibold mb-1 ${theme.title}`}>
                                      {offer.kind === 'table_preview' ? 'Table Preview ready' : 'Page ready to open'}
                                    </div>
                                    <div className={`mb-2 ${theme.label}`}>
                                      {offer.kind === 'table_preview'
                                        ? `Preview: ${offer.label}`
                                        : offer.label}
                                    </div>
                                    <button
                                      type="button"
                                      className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_secondary}`}
                                      onClick={() => handleOpenUiOffer(offer)}
                                    >
                                      Open
                                    </button>
                                  </div>
                                ))}
                              </div>
                            );
                          })()}
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
            <div ref={workspacePanelRef} className="relative shrink-0 flex flex-col" style={{ width: workspaceWidth }}>
              <PanelResizeHandle edge="left" label="Resize workspace" onMouseDown={startResize('workspace')} />
              <div className={`flex items-center justify-between px-3 h-10 shrink-0 mb-1 ${theme.mainContentSection}`}>
                  <span className={`text-sm font-semibold truncate ${theme.title}`}>Workspace</span>
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
                  <div
                    className={`relative shrink-0 overflow-hidden mt-1 flex flex-col ${theme.mainContentSection}`}
                    style={{ height: previewHeight }}
                  >
                    <PanelResizeHandle edge="top" label="Resize preview" onMouseDown={startResize('preview')} />
                    <div className="flex items-center justify-between px-2 py-1 shrink-0">
                      <div className={`text-[10px] truncate w-1 flex-auto mr-2 ${theme.label}`} title={previewPath}>
                        {previewPath}
                      </div>
                      <div className="flex items-center shrink-0 space-x-1">
                        {previewCopyHint && (
                          <span className={`text-[10px] ${theme.label}`}>{previewCopyHint}</span>
                        )}
                        <button
                          type="button"
                          className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                          title="Copy preview"
                          onClick={() => void copyPreview()}
                        >
                          <i className="fa-solid fa-copy" />
                        </button>
                      </div>
                    </div>
                    <div className="h-1 flex-auto overflow-auto px-2 pb-1">
                      {isImagePath(previewPath) && files.find(f => f.RelativePath === previewPath || workspaceFilePath(f) === previewPath)?.PublicUrl ? (
                        <img
                          src={toAppFileUrl(files.find(f => f.RelativePath === previewPath || workspaceFilePath(f) === previewPath)?.PublicUrl)}
                          alt={previewPath}
                          className="max-w-full mt-1"
                        />
                      ) : (
                        <pre className="text-[10px] whitespace-pre-wrap">{previewContent}</pre>
                      )}
                    </div>
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
              await archiveAppDataIntegrationAgentSessions([guid], true);
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
        initialTitle={renameItem ? appDataIntegrationAgentChatTitle(renameItem) : ''}
        onCancel={() => setRenameItem(null)}
        onSave={async title => {
          if (!renameItem) return;
          await renameAppDataIntegrationAgentSession(renameItem.SessionGuid, title);
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
          await deleteAppDataIntegrationAgentSessions([guid]);
          handleDeletedSessions([guid]);
        }}
      />
      <DataIntegrationAgentStartBuildDialog
        isOpen={!!buildPackPath}
        sessionId={sessionId}
        packPath={buildPackPath}
        saasApplicationId={saasApplicationId}
        onClose={() => setBuildPackPath(null)}
      />
      <TablesDataPreviewModal
        isOpen={tablePreviewOpen}
        onClose={() => setTablePreviewOpen(false)}
        tables={tablePreviewTables}
      />
      <DataIntegrationAgentChatManagement
        isOpen={manageOpen}
        onClose={() => setManageOpen(false)}
        onChanged={handleDeletedSessions}
      />
    </div>
  );
};

export default DataIntegrationAgent;
