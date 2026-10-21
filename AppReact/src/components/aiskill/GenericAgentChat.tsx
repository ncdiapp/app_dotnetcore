import React, { useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import type { RootState } from '../../redux/store';
import { AskUserEvent, DataRenderEvent, genericAgentSvc, isInteractivePage, isInteractiveTabular, uiRenderToDataRender, type GenericAgentChatUiSnapshot, type LookupItemDto, type UiRenderEvent } from '../../webapi/genericAgentSvc';
import { agentSkillSetSvc } from '../../webapi/agentSkillSetSvc';
import { registerTabDataSaver, unregisterTabDataSaver } from '../../redux/hooks/useTabNavigation';
import {
    clearAgentChatTabCache,
    getActiveTabKey,
    loadAgentChatFromTabCache,
    saveAgentChatToTabCache,
} from './agentChatTabCache';
import GenericAgentFilesPanel from './GenericAgentFilesPanel';
import { chatModulesFromEnabledTools, chatModulesFromLibraries, type AgentChatUiModule } from './agentUiModules';
import { DataRenderPanel } from './dataRender';
import InteractiveDashboardPanel, { isInteractiveDashboard } from './InteractiveDashboardPanel';
import InteractivePagePanel from './InteractivePagePanel';
import UiRenderFrame, { hasUiHtml } from './UiRenderFrame';
import { placeRender, renderKey } from './dataRender/placeRender';
import { RenderBuilding, RenderChip } from './dataRender/RenderChrome';
import { ReasoningLoopPanel, type ThinkingStep } from './dataRender/ReasoningLoopPanel';
import { AgentMarkdown, FollowupChips, splitFollowups, type AgentFileLink } from './AgentMarkdown';

const SESSION_START = '[session_start]';
const RUN_IN_PROGRESS = '[run_in_progress]';

type StoredChatMessage = {
    role: string;
    content?: string;
    thinking?: string;
    Thinking?: string;
    thinkingSteps?: ThinkingStep[];
    ThinkingSteps?: ThinkingStep[];
    toolSteps?: Array<{ toolName: string; label?: string; args?: string; result?: string; isSuccess?: boolean; durationMs?: number }>;
    dataRenders?: DataRenderEvent[];
    DataRenders?: DataRenderEvent[];
    pendingAskUser?: AskUserEvent;
    PendingAskUser?: AskUserEvent;
    runSessionId?: string;
    RunSessionId?: string;
    askUserSummary?: boolean;
    AskUserSummary?: boolean;
    askUserAnswer?: boolean;
    AskUserAnswer?: boolean;
}; 

const parseStoredDataRenders = (m: StoredChatMessage): DataRenderEvent[] | undefined => {
    const raw = m.dataRenders ?? m.DataRenders;
    if (!Array.isArray(raw) || raw.length === 0) return undefined;
    return raw.map((dr: any) => ({
        RenderId: String(dr?.RenderId ?? dr?.renderId ?? ''),
        Ui: String(dr?.Ui ?? dr?.ui ?? dr?.Layout ?? dr?.layout ?? 'grid'),
        Title: dr?.Title ?? dr?.title ?? null,
        DataJson: dr?.DataJson ?? dr?.dataJson ?? null,
        ColumnsJson: dr?.ColumnsJson ?? dr?.columnsJson ?? null,
        ChartConfigJson: dr?.ChartConfigJson ?? dr?.chartConfigJson ?? null,
        ActionsJson: dr?.ActionsJson ?? dr?.actionsJson ?? null,
        MetaJson: dr?.MetaJson ?? dr?.metaJson ?? null,
        BlocksJson: dr?.BlocksJson ?? dr?.blocksJson ?? null,
        RowCount: dr?.RowCount ?? dr?.rowCount,
        Truncated: !!(dr?.Truncated ?? dr?.truncated),
        Timestamp: dr?.Timestamp ?? dr?.timestamp,
        Mode: dr?.Mode ?? dr?.mode ?? null,
        Layout: dr?.Layout ?? dr?.layout ?? null,
        Html: dr?.Html ?? dr?.html ?? null,
        HtmlPath: dr?.HtmlPath ?? dr?.htmlPath ?? null,
    })).filter(dr => dr.Ui || dr.Html || dr.HtmlPath);
};

const asLookupItems = (raw: unknown): LookupItemDto[] => {
    if (!Array.isArray(raw)) return [];
    return raw
        .map((x: any) => ({
            Id: String(x?.Id ?? x?.id ?? ''),
            Display: String(x?.Display ?? x?.display ?? x?.label ?? x?.Id ?? x?.id ?? ''),
        }))
        .filter(x => x.Id);
};

/** Phase A *Ok / proceed / importMode: LLM often omits options → UI would show a text box. */
const resolveAskUserField = (field: any): {
    name: string; label: string; required: boolean;
    isSelect: boolean; isRadio: boolean; options: LookupItemDto[];
} => {
    const name = String(field?.Name ?? field?.name ?? '');
    let options = asLookupItems(field?.Options ?? field?.options);
    let type = String(field?.Type ?? field?.type ?? 'text').toLowerCase();
    if (type === 'ddl' || type === 'dropdown' || type === 'combobox') type = 'select';
    if (type === 'radiobutton' || type === 'radio_button' || type === 'radios') type = 'radio';
    if (options.length === 0) {
        if (/ok$/i.test(name) || /_ok$/i.test(name)) {
            options = [{ Id: 'ok', Display: 'OK' }, { Id: 'revise', Display: 'Revise' }];
            type = 'select';
        } else if (/^(proceed|action)$/i.test(name)) {
            options = [
                { Id: 'approve', Display: 'Approve — proceed to Phase B' },
                { Id: 'revise', Display: 'Revise' },
                { Id: 'cancel', Display: 'Cancel' },
            ];
            type = 'select';
        } else if (/^(importMode|import_mode)$/i.test(name)) {
            options = [
                { Id: 'APPEND', Display: 'APPEND' },
                { Id: 'REPLACE', Display: 'REPLACE' },
            ];
            type = 'select';
        } else if (/^(recommendedOption|attachOption|muOption|massUpdateOption)$/i.test(name)) {
            options = [
                { Id: 'A', Display: 'A — Single table update' },
                { Id: 'B1', Display: 'B1 — Hierarchical, use existing ListEdit' },
                { Id: 'B2', Display: 'B2 — Hierarchical, create new ListEdit' },
                { Id: 'C', Display: 'C — Do not attach this Mass Update View' },
            ];
            type = 'select';
        }
    } else if (type !== 'select' && type !== 'radio') {
        // Options present but type omitted/unknown → dropdown (not radio).
        type = 'select';
    }
    return {
        name,
        label: String(field?.Label ?? field?.label ?? name),
        required: !!(field?.Required ?? field?.required),
        isSelect: type === 'select' && options.length > 0,
        isRadio: type === 'radio' && options.length > 0,
        options,
    };
};

const parseAskUser = (raw: unknown): AskUserEvent | null => {
    if (!raw || typeof raw !== 'object') return null;
    const o = raw as Record<string, unknown>;
    const prompt = (o.Prompt ?? o.prompt) as string | undefined;
    if (typeof prompt !== 'string' && !o.Mode && !o.mode) return null;
    return {
        Prompt: String(prompt ?? ''),
        Mode: String(o.Mode ?? o.mode ?? 'text'),
        Ui: o.Ui != null || o.ui != null ? String(o.Ui ?? o.ui) : undefined,
        Layout: o.Layout != null || o.layout != null ? String(o.Layout ?? o.layout) : undefined,
        Fields: (o.Fields ?? o.fields) as AskUserEvent['Fields'],
        Options: (o.Options ?? o.options) as AskUserEvent['Options'],
        ContextKey: (o.ContextKey ?? o.contextKey) as string | undefined,
    };
};

const findPendingAskUser = (messages: StoredChatMessage[]): AskUserEvent | null => {
    for (let i = messages.length - 1; i >= 0; i--) {
        const ask = parseAskUser(messages[i]?.pendingAskUser ?? messages[i]?.PendingAskUser);
        if (ask) return ask;
    }
    return null;
};

const findRunSessionId = (messages: StoredChatMessage[]): string | null => {
    for (let i = messages.length - 1; i >= 0; i--) {
        const m = messages[i];
        const sid = (m?.runSessionId ?? m?.RunSessionId ?? '').trim();
        if (sid) return sid;
        const content = typeof m?.content === 'string' ? m.content : '';
        if ((m?.role || '').toLowerCase() === 'system' && content === RUN_IN_PROGRESS) {
            const fromContent = (m as StoredChatMessage).runSessionId || (m as StoredChatMessage).RunSessionId;
            if (fromContent) return String(fromContent);
        }
    }
    return null;
};

interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    isStreaming?: boolean;
    /** Reasoning loop narrative (tool choice / recovery) — not raw tool dumps. */
    thinking?: string;
    /** Per-step description + expandable SQL/JSON detail. */
    thinkingSteps?: ThinkingStep[];
    /** Persisted with session; restored into Tool Activity on reload. */
    toolSteps?: ToolStep[];
    /** Non-blocking data_render panels attached to this assistant turn. */
    dataRenders?: DataRenderEvent[];
    /** Read-only ask_user question bubble (controls dismissed). */
    askUserSummary?: boolean;
    /** Read-only ask_user answer bubble on the user side. */
    askUserAnswer?: boolean;
}

interface ToolStep {
    toolName: string;
    /** Optional label from server (e.g. "call_agent → excel-read-agent"). */
    label?: string;
    args?: string;      // Details from tool_call
    result?: string;    // Details from tool_result
    isSuccess: boolean;
    durationMs?: number;
}

/** File names this turn actually wrote, so the reply can link them to download. */
const exportFileLinks = (steps?: ToolStep[]): AgentFileLink[] => {
    const byName = new Map<string, string>();
    for (const s of steps ?? []) {
        const raw = s.result || '';
        const re = /"relativePath"\s*:\s*"([^"\\]+)"/g;
        let m: RegExpExecArray | null;
        while ((m = re.exec(raw)) !== null) {
            const path = m[1].replace(/\\/g, '/');
            const name = path.split('/').filter(Boolean).pop();
            if (name) byName.set(name, path);
        }
    }
    return Array.from(byName.entries()).map(([name, path]) => ({ name, path }));
};

interface TurnActivity {
    turnIndex: number;  // which user message this belongs to
    steps: ToolStep[];
    isComplete: boolean;
}

interface PlanEvent {
    PlanSummary: string;
}

interface Props {
    skillKey: string;
    testMode?: boolean;
    /** AppGenericAgentSession.SessionKey. Sidebar Chat Management passes a GUID; Agent Management Run omits it. */
    chatSessionKey?: string | null;
    chatTitle?: string | null;
    showChatListButton?: boolean;
    onOpenChatList?: () => void;
    onConversationChanged?: () => void;
}

const snippet = (s?: string | null, max = 300) =>
    !s ? '' : s.length > max ? s.slice(0, max) + '…' : s;

/** Strip leading LLM junk before a step title like [Linear]… (not before TODO [x] lines). */
const sanitizeAgentDisplayText = (raw?: string | null): string => {
    if (!raw) return '';
    let s = raw.trim();
    // ###[Title] at start → [Title]
    if (/^#{1,6}\s*\[/.test(s)) {
        s = s.replace(/^#{1,6}\s*/, '').trim();
    }
    // Inline junk before first [StepTitle] on the same line (e.g. "巧妙 eyes0123…###[Linear] …")
    const m = s.match(/^([^\n[\]]{1,80}?)((?:#{1,6}\s*)?\[[A-Za-z][^\]]{1,80}\])/);
    if (m && m[1] && !/^\s*$/.test(m[1])) {
        return (m[2].replace(/^#{1,6}\s*/, '') + s.slice(m[0].length)).trim();
    }
    return s;
};

/** Format ask_user answers so Q&A stays in the chat after the Question card closes. */
const formatAskUserAnswerSummary = (
    ask: AskUserEvent,
    answers: Record<string, string>,
    selectedIds: string[],
    freeText: string,
    cancelled: boolean,
): string => {
    if (cancelled) return '(cancelled)';
    const mode = (ask.Mode || 'text').toLowerCase();
    if (mode === 'single_choice' || mode === 'multi_choice') {
        const opts = ask.Options ?? [];
        const labels = selectedIds.map(id => {
            const opt = opts.find(o => String(o.Id ?? '') === id);
            return opt?.Display || id;
        });
        return labels.length > 0 ? labels.join(', ') : '(no selection)';
    }
    if (ask.Fields && ask.Fields.length > 0) {
        const lines = ask.Fields.map(field => {
            const resolved = resolveAskUserField(field);
            const raw = answers[resolved.name] ?? '';
            if (!raw) return `${resolved.label}: (empty)`;
            if (resolved.isSelect || resolved.isRadio) {
                const opt = resolved.options.find(o => String(o.Id ?? '') === raw);
                return `${resolved.label}: ${opt?.Display || raw}`;
            }
            return `${resolved.label}: ${raw}`;
        });
        return lines.join('\n');
    }
    return freeText.trim() || '(empty)';
};

/** Short title / first line for long ask_user prompts (expand UI later). */
const formatAskUserQuestionSummary = (prompt?: string | null, maxLen = 220): string => {
    if (!prompt || !prompt.trim()) return '(question)';
    const s = prompt.trim().replace(/\r\n/g, '\n');
    const titleMatch = s.match(/^(?:#{1,6}\s*)?(\[[^\]]{1,80}\])/m);
    let line = titleMatch?.[1]?.trim()
        ?? (s.includes('\n') ? s.slice(0, s.indexOf('\n')).trim() : s);
    if (line.length > maxLen) line = `${line.slice(0, maxLen).trimEnd()}…`;
    return line || '(question)';
};

/** Try parse a string that may be JSON (including truncated / double-escaped blobs). */
const tryParseJsonish = (s: string): unknown | undefined => {
    const t = s.trim();
    if (!t) return undefined;
    try { return JSON.parse(t); } catch { /* continue */ }
    // Truncated with … — try closing braces/brackets for partial display
    if (/[…\u2026]$/.test(t) || t.endsWith('...')) {
        const base = t.replace(/[…\u2026]+$/, '').replace(/\.\.\.$/, '');
        for (const suffix of ['}', '"]}', '"}]}', ']', '" ] }']) {
            try { return JSON.parse(base + suffix); } catch { /* try next */ }
        }
    }
    return undefined;
};

/** Recursively expand stringified JSON fields (e.g. valueJson / value). */
const deepExpandJson = (v: unknown, depth = 0): unknown => {
    if (depth > 6 || v == null) return v;
    if (typeof v === 'string') {
        const parsed = tryParseJsonish(v);
        if (parsed !== undefined && (typeof parsed === 'object'))
            return deepExpandJson(parsed, depth + 1);
        // Unescape common sequences for readability when still a plain string
        if (v.includes('\\n') || v.includes('\\t') || v.includes('\\"'))
            return v.replace(/\\n/g, '\n').replace(/\\t/g, '\t').replace(/\\"/g, '"');
        return v;
    }
    if (Array.isArray(v)) return v.map(x => deepExpandJson(x, depth + 1));
    if (typeof v === 'object') {
        const out: Record<string, unknown> = {};
        for (const [k, val] of Object.entries(v as Record<string, unknown>)) {
            // Prefer "value" over legacy double-encoded "valueJson"
            if ((k === 'valueJson' || k === 'value') && typeof val === 'string') {
                const parsed = tryParseJsonish(val);
                out[k === 'valueJson' ? 'value' : k] = parsed !== undefined
                    ? deepExpandJson(parsed, depth + 1)
                    : deepExpandJson(val, depth + 1);
                continue;
            }
            out[k] = deepExpandJson(val, depth + 1);
        }
        return out;
    }
    return v;
};

/** Pretty-print tool Args/Result for human reading. */
const formatToolPayload = (raw?: string | null, max = 4000): string => {
    if (raw == null || raw === '' || raw === 'null') return '';
    const parsed = tryParseJsonish(raw);
    if (parsed !== undefined) {
        const pretty = JSON.stringify(deepExpandJson(parsed), null, 2);
        return snippet(pretty, max);
    }
    // Not JSON — unescape common sequences for readability
    const unescaped = raw.replace(/\\n/g, '\n').replace(/\\t/g, '\t').replace(/\\"/g, '"');
    return snippet(unescaped, max);
};

/** Split backend ## SQL / ## Input / ## Output (or ## Error) sections for labeled Tool Activity. */
const parseIoSections = (raw?: string | null): Array<{ label: string; body: string }> => {
    if (raw == null || raw === '' || raw === 'null') return [];
    const text = raw.replace(/\r\n/g, '\n').trim();
    const re = /^##\s+(SQL|Input|Output|Error)\s*$/gim;
    const matches: Array<{ label: string; index: number; headerLen: number }> = [];
    let m: RegExpExecArray | null;
    while ((m = re.exec(text)) !== null) {
        matches.push({ label: (m[1] || '').trim(), index: m.index, headerLen: m[0].length });
    }
    if (matches.length === 0) return [];
    const sections: Array<{ label: string; body: string }> = [];
    for (let i = 0; i < matches.length; i++) {
        const cur = matches[i];
        const start = cur.index + cur.headerLen;
        const end = i + 1 < matches.length ? matches[i + 1].index : text.length;
        let body = text.slice(start, end).trim();
        // Strip fenced code wrappers from ToolIoFormatter
        const fence = body.match(/^```(?:sql|json)?\s*\n?([\s\S]*?)\n?```$/i);
        if (fence) body = fence[1].trim();
        if (cur.label && body) sections.push({ label: cur.label, body });
    }
    return sections;
};

const renderIoPayload = (
    raw: string | null | undefined,
    themeLabel: string,
    fallbackLabel: string,
): React.ReactNode => {
    const sections = parseIoSections(raw);
    if (sections.length > 0) {
        return (
            <>
                {sections.map((sec, i) => (
                    <div key={`${sec.label}-${i}`}>
                        <div className="text-xs opacity-40 mb-0.5">{sec.label}</div>
                        <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${themeLabel}`}>
                            {formatToolPayload(sec.body)}
                        </pre>
                    </div>
                ))}
            </>
        );
    }
    if (!raw) return null;
    return (
        <div>
            <div className="text-xs opacity-40 mb-0.5">{fallbackLabel}</div>
            <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${themeLabel}`}>
                {formatToolPayload(raw)}
            </pre>
        </div>
    );
};

/** Normalize tool result text — literal "null" from void tools is not useful. */
const displayResult = (result?: string | null) => {
    if (result == null || result === '' || result === 'null') return null;
    return result;
};

const findSkillKeyInValue = (v: unknown, depth = 0): string | undefined => {
    if (depth > 4 || v == null) return undefined;
    if (typeof v === 'string') {
        const t = v.trim();
        if (t && !t.includes(' ') && t.length < 100 && /^[a-zA-Z0-9][\w.-]*$/.test(t)) {
            // likely a skill key when found under a target* property — caller filters
            return t;
        }
        return undefined;
    }
    if (Array.isArray(v)) {
        for (const item of v) {
            const f = findSkillKeyInValue(item, depth + 1);
            if (f) return f;
        }
        return undefined;
    }
    if (typeof v === 'object') {
        const obj = v as Record<string, unknown>;
        for (const [k, val] of Object.entries(obj)) {
            if (/target.*skill|skill.*key|skillKey/i.test(k) && typeof val === 'string' && val.trim())
                return val.trim();
        }
        for (const val of Object.values(obj)) {
            const f = findSkillKeyInValue(val, depth + 1);
            if (f) return f;
        }
    }
    return undefined;
};

/** Extract call_agent targetSkillKey from serialized tool args. */
const parseCallAgentTarget = (args?: string | null, knownKeys?: Record<string, string>): string | undefined => {
    if (!args) return undefined;
    try {
        const o = JSON.parse(args);
        const fromProp = findSkillKeyInValue(o);
        if (fromProp) return fromProp;
    } catch { /* ignore */ }
    const m = /targetSkillKey["']?\s*[:=]\s*["']([^"']+)/i.exec(args);
    if (m?.[1]?.trim()) return m[1].trim();
    if (knownKeys) {
        for (const key of Object.keys(knownKeys)) {
            if (new RegExp(`["']${key.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}["']`).test(args))
                return key;
        }
    }
    return undefined;
};

/** "call_agent → excel-read-agent" or "call_agent → excel-read-agent — done (9s)" */
const extractCallAgentKeyFromLabel = (label?: string | null): string | undefined => {
    if (!label || !/call_agent\s*→/i.test(label)) return undefined;
    const after = label.split('→')[1];
    if (!after) return undefined;
    return after.split('—')[0].split('|')[0].trim() || undefined;
};

const toolTitle = (toolName: string, args?: string | null, skillNames?: Record<string, string>, label?: string) => {
    const fromLabel = extractCallAgentKeyFromLabel(label);
    if (fromLabel) {
        const display = skillNames?.[fromLabel];
        return display && display !== fromLabel ? `call_agent: ${display}` : `call_agent: ${fromLabel}`;
    }
    if (!/^call_agent$/i.test(toolName)) return toolName;
    const key = parseCallAgentTarget(args, skillNames);
    if (!key) return toolName;
    const display = skillNames?.[key];
    return display && display !== key ? `call_agent: ${display}` : `call_agent: ${key}`;
};

/** Runtime-injected platform tools (not from library subscription or private ToolRegister). */
const isSystemBuiltInTool = (toolName: string) => /^ask_user$/i.test(toolName || '');

const toolTooltip = (toolName: string, args?: string | null, skillNames?: Record<string, string>, label?: string) => {
    if (isSystemBuiltInTool(toolName)) {
        return 'ask_user — system built-in (auto-injected for Interactive agents). No library subscription or private tool registration needed.';
    }
    let key = extractCallAgentKeyFromLabel(label);
    if (!key && /^call_agent$/i.test(toolName))
        key = parseCallAgentTarget(args, skillNames);
    if (!key) return toolName;
    const display = skillNames?.[key];
    if (display && display !== key) return `SkillKey: ${key}\nDisplayName: ${display}`;
    return `SkillKey: ${key}`;
};

const GenericAgentChat: React.FC<Props> = ({
    skillKey,
    testMode,
    chatSessionKey,
    chatTitle,
    showChatListButton,
    onOpenChatList,
    onConversationChanged,
}) => {
    const { theme, t } = useTheme();
    const displayName = useSelector((s: RootState) => String(s.userSession?.userContext?.DisplayName || ''));
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [turnActivities, setTurnActivities] = useState<TurnActivity[]>([]);
    const [currentTurnIndex, setCurrentTurnIndex] = useState(0);
    const [input, setInput] = useState('');
    const [composerFocus, setComposerFocus] = useState(false);
    const [openRenderKey, setOpenRenderKey] = useState<string | null>(null);
    const [openRender, setOpenRender] = useState<DataRenderEvent | null>(null);
    const autoOpenedRender = useRef<string | null>(null);
    const liveResultOpen = useRef(false);
    const [isRunning, setIsRunning] = useState(false);
    const [pendingPlan, setPendingPlan] = useState<PlanEvent | null>(null);
    const [pendingAskUser, setPendingAskUser] = useState<AskUserEvent | null>(null);
    const [askAnswers, setAskAnswers] = useState<Record<string, string>>({});
    const [askSelectedIds, setAskSelectedIds] = useState<string[]>([]);
    const [askFreeText, setAskFreeText] = useState('');
    const [sessionId, setSessionId] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [sidebarOpen, setSidebarOpen] = useState(true);
    const [toolsWidth, setToolsWidth] = useState(288);
    const [renderWidth, setRenderWidth] = useState<number | null>(() => {
        const stored = Number(localStorage.getItem('agentRenderPanelPx'));
        return stored >= 280 ? stored : null;
    });
    const toolsDragRef = useRef(false);
    const composerRef = useRef<HTMLTextAreaElement | null>(null);
    const chatRootRef = useRef<HTMLDivElement | null>(null);
    const [showEarlierTurns, setShowEarlierTurns] = useState(false);
    const [rightTab, setRightTab] = useState<'result' | 'tools' | 'files'>('tools');
    const [uiModules, setUiModules] = useState<Set<AgentChatUiModule>>(() => new Set());
    const [fileSessionKey, setFileSessionKey] = useState<string | null>(null);
    const [expandedTools, setExpandedTools] = useState<Set<string>>(new Set());
    const [skillDisplayNames, setSkillDisplayNames] = useState<Record<string, string>>({});
    const [skillExecutionMode, setSkillExecutionMode] = useState<string>('Interactive');
    const scrollRef = useRef<HTMLDivElement | null>(null);
    // Track in-progress tool calls (call event received, result pending)
    const pendingCallRef = useRef<Map<string, { args?: string; startedAt: number }>>(new Map());
    const sessionStartFiredRef = useRef(false);
    const skillExecutionModeRef = useRef('Interactive');
    const runAgentTurnRef = useRef<(opts: {
        userMessage: string;
        hideUserBubble?: boolean;
        historyBase?: ChatMessage[];
    }) => Promise<void>>(async () => {});
    const messagesRef = useRef<ChatMessage[]>([]);
    const sessionIdRef = useRef<string | null>(null);
    const currentTurnIndexRef = useRef(0);
    const isRunningRef = useRef(false);
    const pendingAskUserRef = useRef<AskUserEvent | null>(null);
    const pendingPlanRef = useRef<PlanEvent | null>(null);
    const mountedRef = useRef(true);
    const turnActivitiesRef = useRef<TurnActivity[]>([]);
    const skillKeyRef = useRef(skillKey);
    const tabKeyRef = useRef<string | null>(null);
    const askAnswersRef = useRef<Record<string, string>>({});
    const askSelectedIdsRef = useRef<string[]>([]);
    const askFreeTextRef = useRef('');

    messagesRef.current = messages;
    sessionIdRef.current = sessionId;
    currentTurnIndexRef.current = currentTurnIndex;
    isRunningRef.current = isRunning;
    pendingAskUserRef.current = pendingAskUser;
    pendingPlanRef.current = pendingPlan;
    skillExecutionModeRef.current = skillExecutionMode;
    turnActivitiesRef.current = turnActivities;
    skillKeyRef.current = skillKey;
    askAnswersRef.current = askAnswers;
    askSelectedIdsRef.current = askSelectedIds;
    askFreeTextRef.current = askFreeText;
    const chatSessionKeyRef = useRef<string | null>(chatSessionKey ?? null);
    chatSessionKeyRef.current = chatSessionKey ?? null;

    const historyRenderKeys = useRef<Set<string>>(new Set());

    const collectPanelKeys = (list: ChatMessage[]) => {
        const keys: string[] = [];
        list.forEach(m => {
            if (m.role !== 'assistant' || !m.dataRenders?.length) return;
            m.dataRenders.forEach((dr, r) => {
                const placed = placeRender(dr);
                if (placed.panel) keys.push(renderKey(placed.panel, r));
            });
        });
        return keys;
    };

    useEffect(() => {
        setOpenRender(null);
        setOpenRenderKey(null);
        setRightTab('tools');
        autoOpenedRender.current = null;
        liveResultOpen.current = false;
        historyRenderKeys.current = new Set();
    }, [chatSessionKey, skillKey]);

    useEffect(() => {
        if (!liveResultOpen.current) {
            historyRenderKeys.current = new Set(collectPanelKeys(messages));
            return;
        }
        for (let i = messages.length - 1; i >= 0; i--) {
            const m = messages[i];
            if (m.role !== 'assistant' || !m.dataRenders?.length) continue;
            for (let r = 0; r < m.dataRenders.length; r++) {
                const placed = placeRender(m.dataRenders[r]);
                if (!placed.panel) continue;
                const key = renderKey(placed.panel, r);
                if (historyRenderKeys.current.has(key)) continue;
                if (autoOpenedRender.current === key) return;
                autoOpenedRender.current = key;
                setOpenRenderKey(key);
                setOpenRender(placed.panel);
                setRightTab('result');
                setSidebarOpen(true);
                return;
            }
        }
    }, [messages]);
    const onConversationChangedRef = useRef(onConversationChanged);
    onConversationChangedRef.current = onConversationChanged;

    useEffect(() => {
        const el = scrollRef.current;
        if (!el) return;
        el.scrollTop = el.scrollHeight;
    }, [messages, turnActivities, pendingAskUser]);

    const appendDataRender = (evt: DataRenderEvent) => {
        setMessages(prev => {
            const last = prev[prev.length - 1];
            if (last?.role === 'assistant') {
                const dataRenders = [...(last.dataRenders ?? []), evt];
                return [...prev.slice(0, -1), { ...last, dataRenders }];
            }
            return [...prev, { role: 'assistant', content: '', isStreaming: true, dataRenders: [evt] }];
        });
    };

    const handleDataRenderAction = (evt: DataRenderEvent, actionId: string, selectionJson?: string) => {
        if (isRunningRef.current || pendingAskUserRef.current) return;
        const useUiPrefix = hasUiHtml(evt) || !!(evt.Mode || evt.Layout);
        const lines = [
            useUiPrefix ? '[ui_render_action]' : '[data_render_action]',
            `actionId=${actionId}`,
            `renderId=${evt.RenderId || ''}`,
            `ui=${evt.Ui || evt.Layout || ''}`,
            `mode=${evt.Mode || ''}`,
        ];
        if (selectionJson) lines.push(`selectionJson=${selectionJson}`);
        void runAgentTurnRef.current({ userMessage: lines.join('\n') });
    };

    const appendUiRender = (evt: UiRenderEvent) => {
        appendDataRender(uiRenderToDataRender(evt));
    };

    useEffect(() => {
        if ((rightTab === 'files' && !uiModules.has('files')) || (rightTab === 'result' && !openRender)) setRightTab('tools');
    }, [rightTab, uiModules, openRender]);

    const buildSnapshot = (): GenericAgentChatUiSnapshot => ({
        skillKey: skillKeyRef.current,
        chatSessionKey: chatSessionKeyRef.current,
        messages: messagesRef.current.map(m => ({
            ...m,
            thinking: m.thinking || undefined,
            thinkingSteps: m.thinkingSteps?.length ? m.thinkingSteps.map(s => ({ ...s })) : undefined,
            toolSteps: m.toolSteps ? [...m.toolSteps] : undefined,
            dataRenders: m.dataRenders ? [...m.dataRenders] : undefined,
        })),
        turnActivities: turnActivitiesRef.current.map(t => ({
            ...t,
            steps: t.steps.map(s => ({ ...s })),
        })),
        currentTurnIndex: currentTurnIndexRef.current,
        sessionId: sessionIdRef.current,
        pendingAskUser: pendingAskUserRef.current,
        pendingPlan: pendingPlanRef.current,
        askAnswers: { ...askAnswersRef.current },
        askSelectedIds: [...askSelectedIdsRef.current],
        askFreeText: askFreeTextRef.current,
        isRunning: isRunningRef.current,
        error: null,
        skillExecutionMode: skillExecutionModeRef.current,
    });

    const persistSnapshotToTab = () => {
        const tabKey = tabKeyRef.current ?? getActiveTabKey();
        if (!tabKey) return;
        tabKeyRef.current = tabKey;
        saveAgentChatToTabCache(tabKey, buildSnapshot());
    };

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
                return prev.map(t => t.turnIndex === turnIdx ? { ...t, steps } : t);
            }
            return [...prev, { turnIndex: turnIdx, steps: [{ isSuccess: true, ...patch }], isComplete: false }];
        });
    };

    const buildLiveHandlers = (_turnIdx: number) => ({
        onToken: (token: string) => {
            if (!mountedRef.current) return;
                    setMessages(prev => {
                        const last = prev[prev.length - 1];
                        if (last?.role === 'assistant' && last.isStreaming) {
                            return [...prev.slice(0, -1), { ...last, content: last.content + token }];
                        }
                return [...prev, { role: 'assistant' as const, content: token, isStreaming: true }];
                    });
                },
        onStep: (step: { Type: string; ToolName?: string; Description: string; IsSuccess: boolean; Details?: string }) => {
            if (!mountedRef.current) return;
            const s = step;
            const idx = currentTurnIndexRef.current;
            const appendThinking = (line: string, details?: string) => {
                const bit = (line || '').trim();
                if (!bit && !(details || '').trim()) return;
                const stepItem: ThinkingStep = {
                    description: bit || '…',
                    ...(details?.trim() ? { details: details.trim() } : {}),
                };
                setMessages(prev => {
                    const last = prev[prev.length - 1];
                    if (last?.role === 'assistant' && (last.isStreaming || isRunningRef.current)) {
                        const prevThink = (last.thinking || '').trim();
                        const prevSteps = last.thinkingSteps ? [...last.thinkingSteps] : [];
                        // Only skip exact consecutive duplicates — same tool retries often share
                        // the same one-line description (e.g. "Querying… FROM table") but have
                        // different Details (fixed SQL / success after soft failure).
                        const prevLast = prevSteps[prevSteps.length - 1];
                        const sameAsLast = !!prevLast
                            && prevLast.description === stepItem.description
                            && (prevLast.details || '') === (stepItem.details || '');
                        if (sameAsLast) {
                            return last.isStreaming ? prev : [...prev.slice(0, -1), { ...last, isStreaming: true }];
                        }
                        const nextThink = prevThink ? (bit ? `${prevThink}\n\n${bit}` : prevThink) : bit;
                        const nextSteps = [...prevSteps, stepItem];
                        return [...prev.slice(0, -1), {
                            ...last,
                            thinking: nextThink || last.thinking,
                            thinkingSteps: nextSteps,
                            isStreaming: true,
                        }];
                    }
                    return [...prev, {
                        role: 'assistant' as const,
                        content: '',
                        thinking: bit || undefined,
                        thinkingSteps: [stepItem],
                        isStreaming: true,
                    }];
                });
            };
            if (s.Type === 'thinking') {
                appendThinking(s.Description || '', s.Details);
                return;
            }
            if (!s.ToolName) return;
            if (s.Type === 'tool_call') {
                // Pre-tool streamed prose: fold into Reasoning only when Think is already active this turn.
                setMessages(prev => {
                    const last = prev[prev.length - 1];
                    if (!(last?.role === 'assistant' && last.isStreaming)) return prev;
                    const draft = (last.content || '').trim();
                    if (!draft) return prev;
                    const prevThink = (last.thinking || '').trim();
                    const prevSteps = last.thinkingSteps;
                    if (!prevThink && !(prevSteps?.length)) {
                        return [...prev.slice(0, -1), { ...last, content: '' }];
                    }
                    const nextThink = prevThink.includes(draft) ? prevThink : `${prevThink}\n\n${draft}`;
                    const nextSteps = prevThink.includes(draft)
                        ? prevSteps
                        : [...(prevSteps || []), { description: draft }];
                    return [...prev.slice(0, -1), {
                        ...last,
                        thinking: nextThink,
                        thinkingSteps: nextSteps,
                        content: '',
                    }];
                });
                pendingCallRef.current.set(s.ToolName, { args: s.Details, startedAt: Date.now() });
                addStep(idx, { toolName: s.ToolName, label: s.Description, args: s.Details, isSuccess: true });
            } else if (s.Type === 'tool_result') {
                const pending = pendingCallRef.current.get(s.ToolName);
                const durationMs = pending ? Date.now() - pending.startedAt : undefined;
                pendingCallRef.current.delete(s.ToolName);
                const resultLabel = extractCallAgentKeyFromLabel(s.Description)
                    ? s.Description.split('—')[0].trim()
                    : undefined;
                addStep(idx, {
                    toolName: s.ToolName,
                    result: s.Details,
                    isSuccess: s.IsSuccess,
                    durationMs,
                    ...(resultLabel ? { label: resultLabel } : {}),
                });
            }
        },
        onPlan: (plan: PlanEvent) => {
            if (!mountedRef.current) return;
            setPendingPlan(plan);
        },
        onAskUser: (ask: AskUserEvent) => {
            if (!mountedRef.current) return;
            // Freeze any in-flight assistant bubble so post-answer Q&A / tokens don't merge into it.
            setMessages(prev => {
                const last = prev[prev.length - 1];
                if (last?.role === 'assistant' && last.isStreaming) {
                    return [...prev.slice(0, -1), { ...last, isStreaming: false }];
                }
                return prev;
            });
            setPendingAskUser(ask);
            setAskAnswers({});
            setAskSelectedIds([]);
            setAskFreeText('');
            setIsRunning(true);
            isRunningRef.current = true;
        },
        onDataRender: (evt: DataRenderEvent) => {
            if (!mountedRef.current || !evt) return;
            appendDataRender(evt);
        },
        onUiRender: (evt: UiRenderEvent) => {
            if (!mountedRef.current || !evt) return;
            appendUiRender(evt);
        },
        onDone: (done: { FinalResponse: string }) => {
            if (!mountedRef.current) return;
            const idx = currentTurnIndexRef.current;
            const stopped = /stopped by user/i.test(done?.FinalResponse || '');
            if (stopped) {
                setPendingAskUser(null);
                setPendingPlan(null);
            }
            setTurnActivities(prev => {
                const turn = prev.find(t => t.turnIndex === idx);
                const stepsForMsg = turn?.steps ? [...turn.steps] : [];
                setMessages(msgs => {
                    const last = msgs[msgs.length - 1];
                    if (last?.role === 'assistant' && last.isStreaming) {
                        return [...msgs.slice(0, -1), {
                            ...last,
                            content: done.FinalResponse || last.content,
                            isStreaming: false,
                            thinking: last.thinking || undefined,
                            toolSteps: stepsForMsg.length > 0 ? stepsForMsg : undefined,
                            dataRenders: last.dataRenders,
                        }];
                    }
                    if (done.FinalResponse) {
                        return [...msgs, {
                            role: 'assistant' as const,
                            content: done.FinalResponse,
                            toolSteps: stepsForMsg.length > 0 ? stepsForMsg : undefined,
                        }];
                    }
                    return msgs;
                });
                return prev.map(t => t.turnIndex === idx ? { ...t, isComplete: true } : t);
            });
            setCurrentTurnIndex(i => {
                const next = i + 1;
                currentTurnIndexRef.current = next;
                return next;
            });
            setPendingAskUser(null);
            setIsRunning(false);
            isRunningRef.current = false;
            onConversationChangedRef.current?.();
        },
        onError: (m: string) => {
            if (!mountedRef.current) return;
            setError(m);
            setPendingAskUser(null);
            setIsRunning(false);
            isRunningRef.current = false;
        },
        onSessionGone: () => {
            if (!mountedRef.current) return;
            setSessionId(null);
            sessionIdRef.current = null;
            // Keep pending question + history. The in-memory run died with the web process.
            if (!pendingAskUserRef.current) {
                setIsRunning(false);
                isRunningRef.current = false;
                setError(
                    'The agent run stopped when the server restarted. History is kept — send Continue to resume.',
                );
            }
        },
    });

    useEffect(() => {
        let cancelled = false;
        mountedRef.current = true;

        const applySnapshot = (): boolean => {
            if (testMode) return false;
            const tabKey = getActiveTabKey();
            tabKeyRef.current = tabKey;
            const snap = loadAgentChatFromTabCache(tabKey, skillKey, chatSessionKey ?? null);
            if (!snap) return false;
            const hasUi =
                (snap.messages?.length ?? 0) > 0
                || !!snap.pendingAskUser
                || !!snap.pendingPlan
                || snap.isRunning;
            if (!hasUi) return false;

            sessionStartFiredRef.current = true;
            setMessages(snap.messages as ChatMessage[]);
            setTurnActivities(snap.turnActivities as TurnActivity[]);
            setCurrentTurnIndex(snap.currentTurnIndex);
            currentTurnIndexRef.current = snap.currentTurnIndex;
            setSessionId(snap.sessionId);
            sessionIdRef.current = snap.sessionId;
            setPendingAskUser(snap.pendingAskUser);
            setPendingPlan(snap.pendingPlan);
            setAskAnswers(snap.askAnswers || {});
            setAskSelectedIds(snap.askSelectedIds || []);
            setAskFreeText(snap.askFreeText || '');
            setIsRunning(!!snap.isRunning || !!snap.pendingAskUser);
            isRunningRef.current = !!snap.isRunning || !!snap.pendingAskUser;
            setSkillExecutionMode(snap.skillExecutionMode || 'Interactive');
            skillExecutionModeRef.current = snap.skillExecutionMode || 'Interactive';
            if (snap.error) setError(snap.error);
            if (snap.sessionId && (genericAgentSvc.isPolling() || snap.isRunning || snap.pendingAskUser)) {
                genericAgentSvc.resumePolling(snap.sessionId, buildLiveHandlers(snap.currentTurnIndex), chatSessionKey ?? null);
            }
            return true;
        };

        const restored = applySnapshot();

        // Register tab flush saver so switching App tabs writes Redux tabKey cache.
        const tabKeyForSaver = tabKeyRef.current ?? getActiveTabKey();
        if (tabKeyForSaver && !testMode) {
            tabKeyRef.current = tabKeyForSaver;
            registerTabDataSaver(tabKeyForSaver, () => persistSnapshotToTab(), 'agent-chat');
        }

        if (!restored) {
            sessionStartFiredRef.current = false;
            setIsRunning(false);
            isRunningRef.current = false;
            setMessages([]);
            setTurnActivities([]);
            setCurrentTurnIndex(0);
            currentTurnIndexRef.current = 0;
            setSessionId(null);
            sessionIdRef.current = null;
            setPendingPlan(null);
            setPendingAskUser(null);
            setAskAnswers({});
            setAskSelectedIds([]);
            setAskFreeText('');
            setError(null);
            setSkillExecutionMode('Interactive');
            skillExecutionModeRef.current = 'Interactive';
        }

        if (chatSessionKey) {
            setFileSessionKey(chatSessionKey);
        } else {
            genericAgentSvc.GetFixedSessionKey(skillKey).then(key => {
                if (!cancelled) setFileSessionKey(key);
            });
        }

        const fireSessionStartIfAllowed = (mode: string, allowFirstTurn: boolean) => {
            if (cancelled || sessionStartFiredRef.current) return;
            sessionStartFiredRef.current = true;
            if (!/^Interactive$/i.test(mode || 'Interactive')) return;
            if (!allowFirstTurn) return;
            void runAgentTurnRef.current({
                userMessage: SESSION_START,
                hideUserBubble: true,
                historyBase: [],
            });
        };

        const boot = async () => {
            let mode = 'Interactive';
            let allowFirstTurn = false;
            try {
                const [res, subRes] = await Promise.all([
                    agentSkillSetSvc.GetAllSkillSets(),
                    agentSkillSetSvc.GetSubscriptions(skillKey).catch(() => null),
                ]);
                if (cancelled) return;
                const map: Record<string, string> = {};
                for (const s of res?.Object ?? []) {
                    if (s?.SkillKey) map[s.SkillKey] = s.DisplayName || s.SkillKey;
                    if (s?.SkillKey === skillKey) {
                        mode = s.ExecutionMode || 'Interactive';
                        allowFirstTurn = !!s.AllowAgentFirstTurn;
                    }
                }
                setSkillDisplayNames(map);
                const libraryKeys = (subRes?.Object ?? []).map(s => s.LibraryKey);
                const fileLibs = libraryKeys.filter(k => chatModulesFromLibraries([k]).has('files'));
                if (fileLibs.length === 0) {
                    setUiModules(new Set());
                } else {
                    try {
                        const [exclRes, ...toolRes] = await Promise.all([
                            agentSkillSetSvc.GetToolExclusions(skillKey),
                            ...fileLibs.map(k => agentSkillSetSvc.GetLibraryTools(k)),
                        ]);
                        const excluded = new Set(
                            (exclRes?.Object ?? []).map(e => `${e.LibraryKey}\u0001${e.ToolName}`),
                        );
                        const toolsByLibrary = fileLibs.map((k, i) => ({
                            libraryKey: k,
                            toolNames: (toolRes[i]?.Object ?? [])
                                .filter(t => t.IsActive !== false)
                                .map(t => t.ToolName),
                        }));
                        setUiModules(chatModulesFromEnabledTools(libraryKeys, toolsByLibrary, excluded));
                    } catch {
                        setUiModules(chatModulesFromLibraries(libraryKeys));
                    }
                }
                if (!restored) {
                    setSkillExecutionMode(mode);
                    skillExecutionModeRef.current = mode;
                }
            } catch { /* optional */ }

            if (cancelled) return;

            // Mid-run tab switch: keep live cache (tokens not yet in DB).
            // Completed / reopen: always LoadChat so ask_user Q&A + data_renders come from MessagesJson.
            const keepLiveCache = restored
                && (isRunningRef.current || !!pendingAskUserRef.current)
                && genericAgentSvc.isPolling();
            if (keepLiveCache) return;
            if (restored && !chatSessionKey) return;

            if (testMode && !chatSessionKey) {
                fireSessionStartIfAllowed(mode, allowFirstTurn);
                return;
            }

            try {
                const loaded = chatSessionKey
                    ? await genericAgentSvc.LoadChat(skillKey, chatSessionKey)
                    : null;
                const prior = chatSessionKey
                    ? loaded?.Messages ?? []
                    : await genericAgentSvc.LoadSession(skillKey);
                if (cancelled) return;
                const meaningful = (prior ?? []).filter(m => {
                    const role = (m.role || '').toLowerCase();
                    const content = typeof m.content === 'string' ? m.content : String(m.content ?? '');
                    if (role === 'system') return false;
                    if (role === 'user' && content === SESSION_START) return false;
                    return true;
                });
                const stored = (prior ?? []) as StoredChatMessage[];
                const pendingFromStore = findPendingAskUser(stored);
                const runSid = findRunSessionId(stored);
                if (meaningful.length > 0 || pendingFromStore || runSid) {
                    sessionStartFiredRef.current = true;
                    const restoredMsgs: ChatMessage[] = [];
                    const restoredActs: TurnActivity[] = [];
                    let userTurn = -1;
                    for (const m of meaningful) {
                        const content = typeof m.content === 'string' ? m.content : String(m.content ?? '');
                        const steps: ToolStep[] | undefined =
                            m.role === 'assistant' && Array.isArray(m.toolSteps) && m.toolSteps.length > 0
                                ? m.toolSteps
                                    .filter(s => s && s.toolName)
                                    .map(s => ({
                                        toolName: s.toolName,
                                        label: s.label,
                                        args: s.args,
                                        result: s.result,
                                        isSuccess: s.isSuccess !== false,
                                        durationMs: s.durationMs,
                                    }))
                                : undefined;
                        const storedMsg = m as StoredChatMessage;
                        const isAskSummary = !!(storedMsg.askUserSummary ?? storedMsg.AskUserSummary);
                        const isAskAnswer = !!(storedMsg.askUserAnswer ?? storedMsg.AskUserAnswer);
                        if (m.role === 'user') {
                            // ask_user answer bubbles are not agent turns — don't advance turn index.
                            if (!isAskAnswer) userTurn++;
                            restoredMsgs.push({
                                role: 'user',
                                content,
                                askUserAnswer: isAskAnswer || undefined,
                            });
                        } else if (m.role === 'assistant') {
                            const pendingOnMsg = parseAskUser(
                                storedMsg.pendingAskUser ?? storedMsg.PendingAskUser,
                            );
                            // Pending ask is shown as the live Question card — don't duplicate in history.
                            // Finalized askUserSummary keeps its bubble after pendingAskUser is cleared.
                            const skipPendingPlaceholder = !!pendingOnMsg && !isAskSummary;
                            if (!skipPendingPlaceholder) {
                                const dataRenders = parseStoredDataRenders(storedMsg);
                                const thinkingText = String(storedMsg.thinking ?? storedMsg.Thinking ?? '').trim();
                                const rawSteps = storedMsg.thinkingSteps ?? storedMsg.ThinkingSteps;
                                const thinkingSteps = Array.isArray(rawSteps)
                                    ? rawSteps
                                        .map((x: any) => ({
                                            description: String(x?.description ?? x?.Description ?? '').trim(),
                                            details: String(x?.details ?? x?.Details ?? '').trim() || undefined,
                                        }))
                                        .filter(x => x.description)
                                    : undefined;
                                restoredMsgs.push({
                                    role: 'assistant',
                                    content: isAskSummary
                                        ? formatAskUserQuestionSummary(content)
                                        : content,
                                    thinking: thinkingText || undefined,
                                    thinkingSteps: thinkingSteps?.length ? thinkingSteps : undefined,
                                    toolSteps: steps,
                                    dataRenders,
                                    askUserSummary: isAskSummary || undefined,
                                });
                            }
                            if (steps && steps.length > 0) {
                                restoredActs.push({
                                    turnIndex: Math.max(0, userTurn),
                                    isComplete: true,
                                    steps,
                                });
                            }
                        }
                    }
                    const userTurns = meaningful.filter(m => {
                        if (m.role !== 'user') return false;
                        const sm = m as StoredChatMessage;
                        return !(sm.askUserAnswer ?? sm.AskUserAnswer);
                    }).length;
                    setMessages(restoredMsgs);
                    setTurnActivities(restoredActs);
                    setCurrentTurnIndex(userTurns);
                    currentTurnIndexRef.current = userTurns;
                    if (pendingFromStore) {
                        setPendingAskUser(pendingFromStore);
                        setIsRunning(true);
                        isRunningRef.current = true;
                        if (runSid) {
                            setSessionId(runSid);
                            sessionIdRef.current = runSid;
                            genericAgentSvc.resumePolling(runSid, buildLiveHandlers(Math.max(0, userTurns)), chatSessionKey ?? null);
                        }
                    } else if (runSid) {
                        setSessionId(runSid);
                        sessionIdRef.current = runSid;
                        setIsRunning(true);
                        isRunningRef.current = true;
                        genericAgentSvc.resumePolling(runSid, buildLiveHandlers(0), chatSessionKey ?? null);
                    }
                } else {
                    fireSessionStartIfAllowed(mode, allowFirstTurn);
                }
            } catch {
                if (!cancelled) fireSessionStartIfAllowed(mode, allowFirstTurn);
            }
        };

        void boot();

        return () => {
            cancelled = true;
            mountedRef.current = false;
            const tabKey = tabKeyRef.current ?? getActiveTabKey();
            if (tabKey) {
                unregisterTabDataSaver(tabKey, 'agent-chat');
                saveAgentChatToTabCache(tabKey, buildSnapshot());
            }
            if (!isRunningRef.current && !pendingAskUserRef.current) {
                genericAgentSvc.disconnect();
            }
        };
    }, [skillKey, testMode, chatSessionKey]);

    const runAgentTurn = async (opts: {
        userMessage: string;
        hideUserBubble?: boolean;
        historyBase?: ChatMessage[];
    }) => {
        const msg = opts.userMessage.trim();
        if (!msg || isRunningRef.current) return;
        liveResultOpen.current = true;
        setError(null);
        setIsRunning(true);
        isRunningRef.current = true;
        const turnIdx = currentTurnIndexRef.current;
        pendingCallRef.current.clear();
        if (!opts.hideUserBubble) {
            setMessages(prev => [...prev, { role: 'user', content: msg }]);
        }

        try {
            const base = opts.historyBase ?? messagesRef.current;
            const history = base
                .filter(m => !m.isStreaming)
                .map(m => ({
                    role: m.role === 'user' ? 'user' : 'assistant',
                    content: m.content,
                    ...(m.role === 'assistant' && m.thinking ? { thinking: m.thinking } : {}),
                    ...(m.role === 'assistant' && m.thinkingSteps && m.thinkingSteps.length > 0
                        ? { thinkingSteps: m.thinkingSteps }
                        : {}),
                    ...(m.role === 'assistant' && m.toolSteps && m.toolSteps.length > 0
                        ? { toolSteps: m.toolSteps }
                        : {}),
                }));

            const sid = await genericAgentSvc.RunAgent(
                {
                    SkillKey: skillKey,
                    UserMessage: msg,
                    SessionId: sessionIdRef.current ?? undefined,
                    ChatSessionKey: chatSessionKeyRef.current || undefined,
                    Messages: history,
                },
                buildLiveHandlers(turnIdx),
            );
            setSessionId(sid);
            sessionIdRef.current = sid;
            if (msg !== SESSION_START) onConversationChangedRef.current?.();
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
            setPendingAskUser(null);
            setIsRunning(false);
            isRunningRef.current = false;
        }
    };
    runAgentTurnRef.current = runAgentTurn;

    const handleSend = async () => {
        const msg = input.trim();
        if (!msg || isRunning) return;
        setInput('');
        await runAgentTurn({ userMessage: msg });
    };

    const handleStop = async () => {
        const sid = sessionIdRef.current || genericAgentSvc.currentSessionId;
        if (!sid && !isRunning && !pendingAskUser) return;
        await genericAgentSvc.CancelRun(sid);
        // Optimistic UI clear; server also emits done "Stopped by user."
        setPendingAskUser(null);
        setPendingPlan(null);
        setIsRunning(false);
        isRunningRef.current = false;
        setMessages(prev => {
            const last = prev[prev.length - 1];
            if (last?.role === 'assistant' && last.isStreaming) {
                return [...prev.slice(0, -1), {
                    ...last,
                    content: last.content || 'Stopped by user.',
                    isStreaming: false,
                }];
            }
            return [...prev, { role: 'assistant' as const, content: 'Stopped by user.' }];
        });
    };

    const handleConfirmPlan = async (confirmed: boolean) => {
        setPendingPlan(null);
        if (sessionId) await genericAgentSvc.ConfirmPlan(sessionId, confirmed);
    };

    const handleConfirmAskUser = async (
        cancelled: boolean,
        overrideSelectedIds?: string[],
    ) => {
        const ask = pendingAskUserRef.current;
        const sid = sessionIdRef.current || genericAgentSvc.currentSessionId;
        const answers = { ...askAnswers };
        const selectedIds = overrideSelectedIds ?? [...askSelectedIds];
        const freeText = askFreeText;
        const mode = (ask?.Mode || 'text').toLowerCase();

        const answerText = ask
            ? formatAskUserAnswerSummary(ask, answers, selectedIds, freeText, cancelled)
            : cancelled
                ? '(cancelled)'
                : '';
        const questionText = formatAskUserQuestionSummary(ask?.Prompt);

        const appendQaAndClearCard = () => {
            if (ask) {
                setMessages(prev => {
                    // Avoid duplicate Q&A if Confirm already restored or double-click.
                    const last = prev[prev.length - 1];
                    const prevQ = prev[prev.length - 2];
                    if (
                        last?.role === 'user'
                        && last.askUserAnswer
                        && last.content === answerText
                        && prevQ?.role === 'assistant'
                        && prevQ.askUserSummary
                    ) {
                        return prev;
                    }
                    return [
                        ...prev,
                        { role: 'assistant', content: questionText, askUserSummary: true },
                        { role: 'user', content: answerText, askUserAnswer: true },
                    ];
                });
            }
            setPendingAskUser(null);
            setAskAnswers({});
            setAskSelectedIds([]);
            setAskFreeText('');
        };

        if (!sid) {
            appendQaAndClearCard();
            isRunningRef.current = false;
            setIsRunning(false);
            setError(null);
            const historyBase = [
                ...messagesRef.current,
                ...(ask
                    ? [
                        { role: 'assistant' as const, content: questionText, askUserSummary: true },
                        { role: 'user' as const, content: answerText, askUserAnswer: true },
                    ]
                    : []),
            ];
            await runAgentTurnRef.current({
                userMessage: answerText || 'Continue',
                hideUserBubble: true,
                historyBase,
            });
            return;
        }

        setIsRunning(true);
        isRunningRef.current = true;

        const ok = await genericAgentSvc.ConfirmAskUser(sid, {
            SessionId: sid,
            Cancelled: cancelled,
            SkillKey: skillKeyRef.current,
            ChatSessionKey: chatSessionKeyRef.current || genericAgentSvc.currentChatSessionKey || undefined,
            QuestionSummary: questionText,
            AnswerSummary: answerText,
            ...(cancelled
                ? {}
                : mode === 'single_choice' || mode === 'multi_choice'
                    ? { SelectedIds: selectedIds }
                    : (ask?.Fields && ask.Fields.length > 0)
                        ? { Answers: answers }
                        : { FreeText: freeText }),
        });

        if (!ok) {
            // Live run died (web restart / timeout). Keep Q&A and start a new run with the answer.
            appendQaAndClearCard();
            isRunningRef.current = false;
            setIsRunning(false);
            setError(null);
            const historyBase = [
                ...messagesRef.current,
                ...(ask
                    ? [
                        { role: 'assistant' as const, content: questionText, askUserSummary: true },
                        { role: 'user' as const, content: answerText, askUserAnswer: true },
                    ]
                    : []),
            ];
            await runAgentTurnRef.current({
                userMessage: answerText || 'Continue',
                hideUserBubble: true,
                historyBase,
            });
            return;
        }

        appendQaAndClearCard();
    };

    const handleClear = async () => {
        genericAgentSvc.disconnect();
        clearAgentChatTabCache(tabKeyRef.current ?? getActiveTabKey(), chatSessionKeyRef.current);
        setIsRunning(false);
        isRunningRef.current = false;
        setMessages([]);
        setTurnActivities([]);
        setCurrentTurnIndex(0);
        currentTurnIndexRef.current = 0;
        setSessionId(null);
        sessionIdRef.current = null;
        setError(null);
        setPendingPlan(null);
        setPendingAskUser(null);
        setAskAnswers({});
        setAskSelectedIds([]);
        setAskFreeText('');
        // Clear Result panel (ui_render / data_render) with the chat
        setOpenRender(null);
        setOpenRenderKey(null);
        liveResultOpen.current = false;
        setRightTab('tools');
        if (!testMode) await genericAgentSvc.ClearSession(skillKey, chatSessionKeyRef.current || undefined);
        sessionStartFiredRef.current = true;
        if (/^Interactive$/i.test(skillExecutionModeRef.current || 'Interactive')) {
            await runAgentTurn({ userMessage: SESSION_START, hideUserBubble: true, historyBase: [] });
        }
    };

    const toggleExpand = (key: string) =>
        setExpandedTools(prev => { const n = new Set(prev); n.has(key) ? n.delete(key) : n.add(key); return n; });

    const btn = `px-3 py-1.5 text-sm rounded-[4px] border ${theme.button_default}`;
    const choiceCardBase =
        `w-full text-left px-3 py-2.5 text-sm rounded-[4px] border transition-colors ${theme.button_default}`;
    const choiceCardPrimary =
        `w-full text-left px-3 py-2.5 text-sm rounded-[4px] border font-medium transition-colors ${theme.button_default}`;
    const regionBorder = t('border_mainContentSection');
    const allActivities = turnActivities;
    const hasAnyTools = allActivities.some(t => t.steps.length > 0);
    const askMode = (pendingAskUser?.Mode || 'text').toLowerCase();
    const askUi = (pendingAskUser?.Ui || 'radio').toLowerCase();
    const askLayout = (pendingAskUser?.Layout || 'vertical').toLowerCase();
    const isButtonGroup =
        askMode === 'single_choice'
        && askUi === 'button_group'
        && (pendingAskUser?.Options?.length ?? 0) > 0;
    const blocked = isRunning || !!pendingAskUser;
    const skillLabel = skillDisplayNames[skillKey] || skillKey || 'Agent';
    const hasConversation = messages.some(m => {
        if (m.role === 'user' && (m.content || '').trim() === SESSION_START) return false;
        if (m.askUserSummary || m.askUserAnswer || m.isStreaming) return true;
        if ((m.dataRenders?.length ?? 0) > 0) return true;
        return !!(m.content || '').trim();
    });
    const showWelcome = !hasConversation && !isRunning && !pendingAskUser && !pendingPlan;
    const hour = new Date().getHours();
    const greetingWord = hour < 12 ? 'Good morning' : hour < 17 ? 'Good afternoon' : 'Good evening';
    const firstName = (displayName || '').trim().split(/\s+/)[0];
    const starterPrompts = [
        'What can you help me with?',
        'Which tools can you use?',
        'Give me a short example of a request I can make.',
        'What should I tell you to get started?',
    ];

    const windowCard = `flex flex-col overflow-hidden min-w-0 min-h-0 rounded-[8px] shadow-sm ${theme.mainContentSection}`;

    useEffect(() => {
        const ta = composerRef.current;
        if (!ta) return;
        ta.style.height = 'auto';
        const next = Math.min(ta.scrollHeight, 200);
        ta.style.height = next + 'px';
        ta.style.overflowY = ta.scrollHeight > 200 ? 'auto' : 'hidden';
    }, [input]);

    const startToolsResize = (e: React.MouseEvent) => {
        e.preventDefault();
        toolsDragRef.current = true;
        const startX = e.clientX;
        const hostW = chatRootRef.current?.clientWidth ?? 900;
        const usingResult = openRender != null;
        const startW = usingResult
            ? (renderWidth ?? Math.max(280, Math.round((hostW - 8) / 2)))
            : toolsWidth;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        let nextW = startW;
        const onMove = (ev: MouseEvent) => {
            if (!toolsDragRef.current) return;
            const max = Math.max(320, hostW - 280);
            nextW = Math.min(max, Math.max(280, startW - (ev.clientX - startX)));
            if (usingResult) setRenderWidth(nextW);
            else setToolsWidth(nextW);
        };
        const onUp = () => {
            toolsDragRef.current = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            if (usingResult) localStorage.setItem('agentRenderPanelPx', String(nextW));
        };
        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    };

    return (
        <div ref={chatRootRef} className="w-full h-full flex flex-row overflow-hidden">
            {/* ── Left: chat ── */}
            <div className={`w-1 flex-auto border ${regionBorder} ${windowCard}`}>
                <div className={`flex items-center gap-1 px-2 py-1.5 border-b shrink-0 ${regionBorder}`}>
                    {showChatListButton && (
                        <button
                            type="button"
                            className={`w-6 h-6 rounded-[4px] text-xs shrink-0 ${theme.button_default}`}
                            title="Chats"
                            onClick={onOpenChatList}
                        >
                            <i className="fa-solid fa-bars" aria-hidden />
                        </button>
                    )}
                    <div className={`min-w-0 w-1 flex-auto truncate text-xs leading-none ${t('text_default')}`}>
                        <span className="font-medium">{chatTitle || 'New Conversation'}</span>
                        {skillLabel ? <span className={`ml-1.5 font-normal ${theme.label}`}>{skillLabel}</span> : null}
                    </div>
                    <div className="ml-auto flex items-center gap-1 shrink-0">
                        {(messages.length > 0 || pendingAskUser) && (
                            <button
                                type="button"
                                className={`w-6 h-6 rounded-[4px] text-xs ${theme.button_default}`}
                                onClick={() => { void handleClear(); }}
                                title="Clear conversation"
                            >
                                <i className="fa-solid fa-rotate-left" aria-hidden />
                            </button>
                        )}
                        <button
                            type="button"
                            className={`w-6 h-6 rounded-[4px] text-xs ${theme.button_default}${sidebarOpen ? ' font-medium' : ' opacity-70'}`}
                            onClick={() => setSidebarOpen(o => !o)}
                            title="Tool activity"
                        >
                            <i className="fa-solid fa-timeline" aria-hidden />
                        </button>
                    </div>
                </div>
                <div ref={scrollRef} className="w-full h-1 flex-auto overflow-auto px-6 py-4 flex flex-col">
                    {showWelcome && (
                        <div className="w-full max-w-3xl mx-auto h-1 flex-auto flex flex-col items-center justify-center gap-6 py-8">
                            <div className={`w-14 h-14 rounded-2xl flex items-center justify-center text-xl ${theme.button_default}`}>
                                <i className="fa-solid fa-robot" aria-hidden />
                            </div>
                            <div className="text-center">
                                <div className={`text-xl font-medium ${t('text_default')}`}>
                                    {greetingWord}{firstName ? `, ${firstName}` : ''}
                                </div>
                                <div className={`mt-2 text-sm ${theme.label}`}>
                                    {skillLabel} is ready. Pick a starting point or type below.
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-2 w-full">
                                {starterPrompts.map(prompt => (
                                    <button
                                        key={prompt}
                                        type="button"
                                        className={`text-left px-3 py-2.5 text-sm leading-relaxed rounded-[8px] border ${theme.mainContentSection} ${t('text_mainContentSection')} ${regionBorder}`}
                                        onClick={() => { void runAgentTurn({ userMessage: prompt }); }}
                                    >
                                        {prompt}
                                    </button>
                                ))}
                            </div>
                        </div>
                    )}
                    <div className="w-full flex flex-col gap-3">
                    {messages.map((m, i) => {
                        const split = m.role === 'assistant'
                            ? splitFollowups(sanitizeAgentDisplayText(m.content), m.isStreaming)
                            : { body: m.content, followups: [] as string[] };
                        const showBubble = !!(split.body || m.isStreaming || m.askUserSummary || m.askUserAnswer);
                        const hasRenders = m.role === 'assistant' && (m.dataRenders?.length ?? 0) > 0;
                        const showThinking = m.role === 'assistant' && !!(
                            m.thinking?.trim()
                            || (m.thinkingSteps?.length ?? 0) > 0
                            || (m.isStreaming && isRunning && !split.body)
                        );
                        const hasFollowups = m.role === 'assistant' && !m.isStreaming && split.followups.length > 0;
                        const largeOpen = hasRenders && m.dataRenders!.some(dr => placeRender(dr).panel);
                        if (!showBubble && !hasRenders && !hasFollowups && !showThinking) return null;
                        return (
                        <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                            <div className={m.role === 'user' ? 'max-w-xl' : 'w-full min-w-0 flex flex-col gap-2'}>
                            {showThinking && (
                                <ReasoningLoopPanel
                                    text={m.thinking || ''}
                                    steps={m.thinkingSteps}
                                    isLive={!!(m.isStreaming || (i === messages.length - 1 && isRunning && !pendingAskUser))}
                                />
                            )}
                            {showBubble && (
                                    <div className={
                                        m.role === 'user'
                                            ? `px-3 py-2 rounded-tl-[8px] rounded-tr-[8px] rounded-bl-[8px] rounded-br-none border text-sm leading-6 ${t('text_default')} ${regionBorder}`
                                            : `w-full text-sm leading-6 ${t('text_default')}`
                                    }>
                                        {(m.askUserSummary || m.askUserAnswer) && (
                                            <div className={`text-xs mb-1 ${theme.label}`}>
                                                {m.askUserSummary ? 'Question' : 'Answer'}
                                            </div>
                                        )}
                                        {m.role === 'assistant' && !m.askUserSummary ? (
                                            <AgentMarkdown
                                                content={split.body}
                                                fileLinks={exportFileLinks(m.toolSteps)}
                                                onFileDownload={(path) => {
                                                    if (fileSessionKey) void genericAgentSvc.DownloadAgentFile(skillKey, fileSessionKey, path);
                                                }}
                                            />
                                        ) : (
                                            <div className="whitespace-pre-wrap leading-relaxed">
                                                {m.role === 'assistant' ? split.body : m.content}
                                            </div>
                                        )}
                                        {m.isStreaming && !largeOpen && <span className="inline-block w-0.5 h-4 ml-0.5 align-middle animate-pulse">|</span>}
                                    </div>
                            )}
                            {hasRenders && (
                                <div className="w-full flex flex-col gap-2">
                                    {m.dataRenders!.map((dr, r) => {
                                        const placed = placeRender(dr);
                                        const key = renderKey(dr, r);
                                        return (
                                            <React.Fragment key={key}>
                                                {placed.inline && (
                                                    <DataRenderPanel
                                                        event={placed.inline}
                                                        disabled={blocked}
                                                        onAction={(actionId, selectionJson) => handleDataRenderAction(dr, actionId, selectionJson)}
                                                    />
                                                )}
                                                {placed.panel && (
                                                    <RenderChip
                                                        event={placed.panel}
                                                        open={openRenderKey === key}
                                                        onOpen={() => { setOpenRenderKey(key); setOpenRender(placed.panel!); setRightTab('result'); setSidebarOpen(true); }}
                                                    />
                                                )}
                                            </React.Fragment>
                                        );
                                    })}
                                </div>
                            )}
                            {m.role === 'assistant' && m.isStreaming && !hasRenders && (m.toolSteps?.some(s => /data_render|render_none_interactive|render_interactive/i.test(s.toolName)) ?? false) && (
                                <RenderBuilding />
                            )}
                            {hasFollowups && (
                                <FollowupChips
                                    items={split.followups}
                                    disabled={blocked}
                                    onSelect={(text) => { void runAgentTurn({ userMessage: text }); }}
                                />
                            )}
                            </div>
                        </div>
                        );
                    })}

                    {isRunning && !pendingAskUser && (
                        <div className={`flex items-center gap-2 text-xs ${theme.label}`}>
                            {!(messages[messages.length - 1]?.role === 'assistant' && messages[messages.length - 1]?.thinking) && (
                                <>
                                    <i className="fa-solid fa-spinner fa-spin" />
                                    <span>Reasoning…</span>
                                </>
                            )}
                            <button
                                type="button"
                                className={`${btn} ml-1`}
                                onClick={() => { void handleStop(); }}
                                title="Stop current agent run"
                            >
                                <i className="fa-solid fa-stop mr-1" />Stop
                            </button>
                        </div>
                    )}

                    {pendingPlan && (
                        <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                            <div className={`text-xs font-medium ${t('text_default')}`}>Plan Review</div>
                            <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{pendingPlan.PlanSummary}</div>
                            <div className="flex gap-2">
                                <button className={btn} onClick={() => handleConfirmPlan(true)}><i className="fa-solid fa-check mr-1" />Approve</button>
                                <button className={btn} onClick={() => handleConfirmPlan(false)}><i className="fa-solid fa-xmark mr-1" />Reject</button>
                            </div>
                        </div>
                    )}

                    {pendingAskUser && (
                        <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                            <div className={`text-xs font-medium ${t('text_default')}`}>Question</div>
                            <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{sanitizeAgentDisplayText(pendingAskUser.Prompt)}</div>

                            {isButtonGroup && (
                                <div
                                    className={
                                        askLayout === 'horizontal'
                                            ? 'flex flex-row flex-nowrap gap-2 w-full items-stretch'
                                            : 'flex flex-col gap-2 w-full max-w-xl items-stretch'
                                    }
                                >
                                    {pendingAskUser.Options!.map((opt, optIdx) => {
                                        const id = String(opt.Id ?? '');
                                        const isCancelish = /^(cancel|done|stop)$/i.test(id);
                                        const cardClass = askLayout === 'horizontal'
                                            ? `${btn} ${isCancelish ? 'opacity-80' : ''}`
                                            : (optIdx === 0 && !isCancelish ? choiceCardPrimary : choiceCardBase);
                                        return (
                                            <button
                                                key={id}
                                                type="button"
                                                className={cardClass}
                                                onClick={() => handleConfirmAskUser(false, [id])}
                                            >
                                                {opt.Display || id}
                                            </button>
                                        );
                                    })}
                                </div>
                            )}

                            {askMode === 'single_choice' && askUi === 'dropdown' && (pendingAskUser.Options?.length ?? 0) > 0 && (
                                <select
                                    className={`w-full max-w-xl h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                                    value={askSelectedIds[0] || ''}
                                    onChange={e => setAskSelectedIds(e.target.value ? [e.target.value] : [])}
                                >
                                    <option value="">— select —</option>
                                    {pendingAskUser.Options!.map(opt => {
                                        const id = String(opt.Id ?? '');
                                        return (
                                            <option key={id} value={id}>{opt.Display || id}</option>
                                        );
                                    })}
                                </select>
                            )}

                            {askMode === 'single_choice' && !isButtonGroup && askUi !== 'dropdown' && (pendingAskUser.Options?.length ?? 0) > 0 && (
                                <div className="flex flex-col gap-1.5">
                                    {pendingAskUser.Options!.map(opt => {
                                        const id = String(opt.Id ?? '');
                                        return (
                                        <label key={id} className={`flex items-center gap-2 text-xs cursor-pointer ${theme.label}`}>
                                            <input
                                                type="radio"
                                                name="ask-user-single"
                                                checked={askSelectedIds[0] === id}
                                                onChange={() => setAskSelectedIds([id])}
                                            />
                                            <span>{opt.Display || id}</span>
                                        </label>
                                        );
                                    })}
                                </div>
                            )}

                            {askMode === 'multi_choice' && (pendingAskUser.Options?.length ?? 0) > 0 && (
                                <div className="flex flex-col gap-1.5">
                                    {pendingAskUser.Options!.map(opt => {
                                        const id = String(opt.Id ?? '');
                                        const checked = askSelectedIds.includes(id);
                                        return (
                                            <label key={id} className={`flex items-center gap-2 text-xs cursor-pointer ${theme.label}`}>
                                                <input
                                                    type="checkbox"
                                                    checked={checked}
                                                    onChange={() => setAskSelectedIds(prev =>
                                                        checked ? prev.filter(x => x !== id) : [...prev, id]
                                                    )}
                                                />
                                                <span>{opt.Display || id}</span>
                                            </label>
                                        );
                                    })}
                                </div>
                            )}

                            {(askMode === 'text' || (askMode !== 'single_choice' && askMode !== 'multi_choice')) && (
                                (pendingAskUser.Fields && pendingAskUser.Fields.length > 0) ? (
                                    <div className="flex flex-col gap-2">
                                        {pendingAskUser.Fields.map(field => {
                                            const resolved = resolveAskUserField(field);
                                            return (
                                            <div key={resolved.name} className={`flex gap-2 ${resolved.isRadio ? 'items-start' : 'items-center'}`}>
                                                <label className={`w-40 text-xs shrink-0 ${theme.label}${resolved.isRadio ? ' pt-0.5' : ''}`}>
                                                    {resolved.label}
                                                    {resolved.required ? ' *' : ''}
                                                </label>
                                                {resolved.isRadio ? (
                                                    <div className="w-1 flex-auto flex flex-col gap-1">
                                                        {resolved.options.map(opt => {
                                                            const id = String(opt.Id ?? '');
                                                            return (
                                                                <label key={id} className={`flex items-center gap-2 text-xs cursor-pointer ${theme.label}`}>
                                                                    <input
                                                                        type="radio"
                                                                        name={`ask-field-${resolved.name}`}
                                                                        checked={(askAnswers[resolved.name] || '') === id}
                                                                        onChange={() => setAskAnswers(prev => ({ ...prev, [resolved.name]: id }))}
                                                                    />
                                                                    <span>{opt.Display || id}</span>
                                                                </label>
                                                            );
                                                        })}
                                                    </div>
                                                ) : resolved.isSelect ? (
                                                    <select
                                                        className={`w-1 flex-auto h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none`}
                                                        value={askAnswers[resolved.name] || ''}
                                                        onChange={e => setAskAnswers(prev => ({ ...prev, [resolved.name]: e.target.value }))}
                                                    >
                                                        <option value="">{resolved.required ? '— select —' : '— skip —'}</option>
                                                        {resolved.options.map(opt => {
                                                            const id = String(opt.Id ?? '');
                                                            return (
                                                            <option key={id} value={id}>{opt.Display || id}</option>
                                                            );
                                                        })}
                                                    </select>
                                                ) : (
                                                    <input
                                                        className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                                        value={askAnswers[resolved.name] || ''}
                                                        onChange={e => setAskAnswers(prev => ({ ...prev, [resolved.name]: e.target.value }))}
                                                    />
                                                )}
                                            </div>
                                            );
                                        })}
                                    </div>
                                ) : (
                                    <textarea
                                        className={`w-full px-2 py-1 text-xs border rounded-[4px] resize-none ${theme.inputBox} focus:outline-none`}
                                        rows={3}
                                        value={askFreeText}
                                        placeholder="Your answer…"
                                        onChange={e => setAskFreeText(e.target.value)}
                                    />
                                )
                            )}

                            {/* button_group is one-click; options already include Cancel — do not add a second Cancel row. */}
                            {!isButtonGroup && (
                                <div className="flex gap-2">
                                    <button className={btn} onClick={() => handleConfirmAskUser(false)}>
                                        <i className="fa-solid fa-check mr-1" />Submit
                                    </button>
                                    <button className={btn} onClick={() => handleConfirmAskUser(true)}>
                                        <i className="fa-solid fa-xmark mr-1" />Cancel
                                    </button>
                                </div>
                            )}
                        </div>
                    )}

                    {error && (
                        <div className="mx-2 px-3 py-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded">
                            {error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                        </div>
                    )}
                    </div>
                </div>

                <div className="px-6 pb-2 pt-1 shrink-0 w-full max-w-3xl mx-auto">
                    <div className={`relative min-h-[100px] px-3 pt-2 pb-2 rounded-[8px] border ${theme.inputBox}${composerFocus ? ' ring-1' : ''}`}>
                    <textarea
                        ref={composerRef}
                        className={`w-full px-0 py-1 text-sm leading-6 bg-transparent border-0 focus:outline-none resize-none ${isRunning || pendingAskUser ? 'pr-20' : 'pr-12'}`}
                        rows={2}
                        value={input}
                        placeholder={`Message ${skillLabel}…`}
                        onChange={e => setInput(e.target.value)}
                        onFocus={() => setComposerFocus(true)}
                        onBlur={() => setComposerFocus(false)}
                        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); } }}
                        disabled={blocked}
                    />
                    <div className="absolute bottom-2 right-2 flex items-center gap-1">
                    {(isRunning || pendingAskUser) && (
                        <button
                            type="button"
                            className={`w-8 h-8 rounded-[4px] text-sm ${theme.button_default}`}
                            onClick={() => { void handleStop(); }}
                            title="Stop current agent run"
                        >
                            <i className="fa-solid fa-stop" />
                        </button>
                    )}
                    <button
                        type="button"
                        className={`w-8 h-8 rounded-[4px] text-sm ${theme.button_default}`}
                        onClick={handleSend}
                        disabled={blocked || !input.trim()}
                        title="Send"
                    >
                        {isRunning ? <i className="fa-solid fa-spinner fa-spin" /> : <i className="fa-solid fa-paper-plane" />}
                    </button>
                    </div>
                    </div>
                    <p className={`hidden md:block text-center text-xs mt-2 ${theme.label}`}>The agent can be wrong about people, places, and facts.</p>
                </div>
            </div>

            {sidebarOpen && (
                <div
                    role="separator"
                    aria-orientation="vertical"
                    title="Drag to resize"
                    className="w-2 shrink-0 cursor-col-resize"
                    onMouseDown={startToolsResize}
                />
            )}
            {sidebarOpen && (
                <div style={{ width: openRender ? (renderWidth ?? Math.max(280, Math.round(((chatRootRef.current?.clientWidth ?? 900) - 8) / 2))) : toolsWidth }} className={`shrink-0 border flex flex-col min-w-0 min-h-0 ${regionBorder} ${windowCard}`}>
                    <div className={`flex items-center gap-1 px-2 py-1.5 border-b ${regionBorder}`}>
                        <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}${rightTab === 'tools' ? ' font-medium' : ' opacity-70'}`}
                            onClick={() => setRightTab('tools')}
                        >
                            Tool Activity
                        </button>
                        {uiModules.has('files') && (
                        <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}${rightTab === 'files' ? ' font-medium' : ' opacity-70'}`}
                            onClick={() => setRightTab('files')}
                        >
                            Files
                        </button>
                        )}
                        {openRender && (
                        <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}${rightTab === 'result' ? ' font-medium' : ' opacity-70'}`}
                            onClick={() => setRightTab('result')}
                        >
                            Result
                        </button>
                        )}
                        <button className="text-xs opacity-50 hover:opacity-100 ml-auto" onClick={() => setSidebarOpen(false)}>
                            <i className="fa-solid fa-xmark" />
                        </button>
                    </div>

                    {rightTab === 'result' && openRender ? (
                        <div className="w-full h-1 flex-auto overflow-hidden min-h-0 p-2">
                            {isInteractiveDashboard(openRender) ? (
                                <InteractiveDashboardPanel
                                    event={openRender}
                                    disabled={blocked}
                                    onAction={(actionId, selectionJson) => handleDataRenderAction(openRender, actionId, selectionJson)}
                                />
                            ) : isInteractivePage(openRender) ? (
                                <InteractivePagePanel
                                    event={openRender}
                                    disabled={blocked}
                                    onAction={(actionId, selectionJson) => handleDataRenderAction(openRender, actionId, selectionJson)}
                                />
                            ) : isInteractiveTabular(openRender) ? (
                                <div className="w-full h-full overflow-auto">
                                    <DataRenderPanel
                                        event={openRender}
                                        disabled={blocked}
                                        onAction={(actionId, selectionJson) => handleDataRenderAction(openRender, actionId, selectionJson)}
                                    />
                                </div>
                            ) : hasUiHtml(openRender) ? (
                                <UiRenderFrame
                                    event={openRender}
                                    skillKey={skillKey}
                                    sessionKey={fileSessionKey}
                                    disabled={blocked}
                                    onAction={(actionId, selectionJson) => handleDataRenderAction(openRender, actionId, selectionJson)}
                                />
                            ) : (
                                <div className="w-full h-full overflow-auto">
                                    <DataRenderPanel
                                        event={openRender}
                                        disabled={blocked}
                                        onAction={(actionId, selectionJson) => handleDataRenderAction(openRender, actionId, selectionJson)}
                                    />
                                </div>
                            )}
                        </div>
                    ) : rightTab === 'files' && uiModules.has('files') ? (
                        <div className="w-full h-1 flex-auto overflow-hidden min-h-0">
                            <GenericAgentFilesPanel skillKey={skillKey} sessionKey={fileSessionKey} />
                        </div>
                    ) : (
                        <div className="w-full h-1 flex-auto overflow-y-auto flex flex-col gap-0 p-2">
                            {!hasAnyTools && !isRunning && messages.length > 0 && (
                                <div className={`p-3 rounded text-xs ${theme.label} flex flex-col gap-1`}>
                                    <div className="flex items-center gap-2 opacity-60">
                                        <i className="fa-solid fa-circle-info" />
                                        <span className="font-medium">No tool activity</span>
                                    </div>
                                    <p className="opacity-50 leading-relaxed">
                                        No tool steps are stored for this chat view. New runs save tool activity with the session; chats saved before that change will not show past tools after reload.
                                    </p>
                                </div>
                            )}

                            {allActivities.length > 1 && (
                                <button
                                    type="button"
                                    className={`mb-2 px-1 text-xs text-left ${theme.label}`}
                                    onClick={() => setShowEarlierTurns(v => !v)}
                                >
                                    {showEarlierTurns ? 'Hide earlier turns' : `Earlier turns (${allActivities.length - 1})`}
                                </button>
                            )}
                            {(showEarlierTurns ? allActivities : allActivities.slice(-1)).map((turn) => (
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
                                            // Merged step (args + result on one object) — keep args visible for titles
                                            if (s.result !== undefined && s.args !== undefined) {
                                                paired.push({ call: s, result: s, key: `${s.toolName}-${idx}` });
                                                return;
                                            }
                                            if (s.result !== undefined) {
                                                const callIdx = findLastIdx(paired, p => p.call?.toolName === s.toolName && !p.result);
                                                if (callIdx >= 0) {
                                                    paired[callIdx] = { ...paired[callIdx], result: s };
                                                } else {
                                                    paired.push({ call: s, result: s, key: `${s.toolName}-${idx}` });
                                                }
                                            } else {
                                                paired.push({ call: s, key: `${s.toolName}-${idx}` });
                                            }
                                        });
                                        return paired.map((pair, pi) => {
                                            const toolName = pair.call?.toolName ?? pair.result?.toolName ?? '';
                                            const argsText = pair.call?.args ?? pair.result?.args;
                                            const labelText = pair.call?.label ?? pair.result?.label;
                                            const isSuccess = pair.result ? pair.result.isSuccess : true;
                                            const hasResult = pair.result !== undefined;
                                            const expandKey = `${turn.turnIndex}-${pi}`;
                                            const isExpanded = expandedTools.has(expandKey);
                                            const resultText = displayResult(pair.result?.result);
                                            const hasDetails = !!(argsText || resultText);
                                            const title = toolTitle(toolName, argsText, skillDisplayNames, labelText);
                                            const tip = toolTooltip(toolName, argsText, skillDisplayNames, labelText);
                                            return (
                                                <div key={pair.key} className={`mb-1 rounded-[4px] overflow-hidden border ${regionBorder}`}>
                                                    <button
                                                        type="button"
                                                        title={tip}
                                                        className="w-full flex items-center gap-2 px-2.5 py-2 text-xs text-left"
                                                        onClick={() => hasDetails && toggleExpand(expandKey)}
                                                    >
                                                        <i className={`fa-solid ${!hasResult ? `fa-spinner fa-spin ${t('text_default_active')}` : isSuccess ? `fa-circle-check ${t('text_default_active')}` : 'fa-circle-xmark text-red-600'}`} />
                                                        <span className={`font-mono font-medium truncate ${t('text_default')}`}>{title}</span>
                                                        {isSystemBuiltInTool(toolName) && (
                                                            <span
                                                                className={`text-[10px] px-1.5 py-0.5 rounded shrink-0 opacity-70 border ${regionBorder} ${theme.label}`}
                                                                title="System built-in — auto-injected; no library or private tool needed"
                                                            >
                                                                built-in
                                                            </span>
                                                        )}
                                                        <span className="flex-auto" />
                                                        {(pair.result?.durationMs ?? 0) > 0 && (
                                                            <span className={`shrink-0 ${theme.label}`}>{pair.result!.durationMs}ms</span>
                                                        )}
                                                        {hasDetails && (
                                                            <i className={`fa-solid fa-chevron-${isExpanded ? 'up' : 'down'} opacity-40 shrink-0`} />
                                                        )}
                                                    </button>
                                                    {isExpanded && (
                                                        <div className="px-2 py-1.5 flex flex-col gap-1.5">
                                                            {argsText && renderIoPayload(argsText, theme.label, 'Args')}
                                                            {resultText && renderIoPayload(
                                                                resultText,
                                                                theme.label,
                                                                isSuccess ? 'Result' : 'Error',
                                                            )}
                                                            {!resultText && hasResult && isSuccess && (
                                                                <div className={`text-xs opacity-50 ${theme.label}`}>Completed (see Args for written values).</div>
                                                            )}
                                                        </div>
                                                    )}
                                                </div>
                                            );
                                        });
                                    })()}
                                </div>
                            ))}

                            {isRunning && turnActivities.find(t => t.turnIndex === currentTurnIndex) === undefined && (
                                <div className={`px-2 py-1.5 text-xs opacity-50 ${theme.label} flex items-center gap-2`}>
                                    <i className="fa-solid fa-spinner fa-spin" />
                                    Waiting for tools…
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
