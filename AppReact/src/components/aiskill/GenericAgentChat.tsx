import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { AskUserEvent, genericAgentSvc } from '../../webapi/genericAgentSvc';
import { agentSkillSetSvc } from '../../webapi/agentSkillSetSvc';
import GenericAgentFilesPanel from './GenericAgentFilesPanel';

const SESSION_START = '[session_start]';

interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    isStreaming?: boolean;
    /** Persisted with session; restored into Tool Activity on reload. */
    toolSteps?: ToolStep[];
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
}

const snippet = (s?: string | null, max = 300) =>
    !s ? '' : s.length > max ? s.slice(0, max) + '…' : s;

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

const toolTooltip = (toolName: string, args?: string | null, skillNames?: Record<string, string>, label?: string) => {
    let key = extractCallAgentKeyFromLabel(label);
    if (!key && /^call_agent$/i.test(toolName))
        key = parseCallAgentTarget(args, skillNames);
    if (!key) return toolName;
    const display = skillNames?.[key];
    if (display && display !== key) return `SkillKey: ${key}\nDisplayName: ${display}`;
    return `SkillKey: ${key}`;
};

const GenericAgentChat: React.FC<Props> = ({ skillKey, testMode }) => {
    const { theme } = useTheme();
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [turnActivities, setTurnActivities] = useState<TurnActivity[]>([]);
    const [currentTurnIndex, setCurrentTurnIndex] = useState(0);
    const [input, setInput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    const [pendingPlan, setPendingPlan] = useState<PlanEvent | null>(null);
    const [pendingAskUser, setPendingAskUser] = useState<AskUserEvent | null>(null);
    const [askAnswers, setAskAnswers] = useState<Record<string, string>>({});
    const [askSelectedIds, setAskSelectedIds] = useState<string[]>([]);
    const [askFreeText, setAskFreeText] = useState('');
    const [sessionId, setSessionId] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [sidebarOpen, setSidebarOpen] = useState(true);
    const [rightTab, setRightTab] = useState<'tools' | 'files'>('tools');
    const [fileSessionKey, setFileSessionKey] = useState<string | null>(null);
    const [expandedTools, setExpandedTools] = useState<Set<string>>(new Set());
    const [skillDisplayNames, setSkillDisplayNames] = useState<Record<string, string>>({});
    const [skillExecutionMode, setSkillExecutionMode] = useState<string>('Interactive');
    const bottomRef = useRef<HTMLDivElement | null>(null);
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

    messagesRef.current = messages;
    sessionIdRef.current = sessionId;
    currentTurnIndexRef.current = currentTurnIndex;
    isRunningRef.current = isRunning;
    pendingAskUserRef.current = pendingAskUser;
    skillExecutionModeRef.current = skillExecutionMode;

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, turnActivities, pendingAskUser]);

    useEffect(() => {
        let cancelled = false;
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

        genericAgentSvc.GetFixedSessionKey(skillKey).then(key => {
            if (!cancelled) setFileSessionKey(key);
        });

        const fireSessionStartIfAllowed = (mode: string, allowFirstTurn: boolean) => {
            if (cancelled || sessionStartFiredRef.current) return;
            sessionStartFiredRef.current = true;
            // Interactive + AllowAgentFirstTurn only (UI: "Agent speaks first")
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
                const res = await agentSkillSetSvc.GetAllSkillSets();
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
                setSkillExecutionMode(mode);
                skillExecutionModeRef.current = mode;
            } catch { /* optional */ }

            if (testMode) {
                fireSessionStartIfAllowed(mode, allowFirstTurn);
                return;
            }

            try {
                const prior = await genericAgentSvc.LoadSession(skillKey);
                if (cancelled) return;
                const meaningful = (prior ?? []).filter(
                    m => !(m.role === 'user' && (typeof m.content === 'string' ? m.content : String(m.content ?? '')) === SESSION_START)
                );
                if (meaningful.length > 0) {
                    sessionStartFiredRef.current = true;
                    const restoredMsgs: ChatMessage[] = [];
                    const restored: TurnActivity[] = [];
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
                        if (m.role === 'user') {
                            userTurn++;
                            restoredMsgs.push({ role: 'user', content });
                        } else if (m.role === 'assistant') {
                            restoredMsgs.push({ role: 'assistant', content, toolSteps: steps });
                            if (steps && steps.length > 0) {
                                restored.push({
                                    turnIndex: Math.max(0, userTurn),
                                    isComplete: true,
                                    steps,
                                });
                            }
                        }
                    }
                    setMessages(restoredMsgs);
                    setTurnActivities(restored);
                    setCurrentTurnIndex(meaningful.filter(m => m.role === 'user').length);
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
            genericAgentSvc.disconnect();
        };
    }, [skillKey, testMode]);

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

    const runAgentTurn = async (opts: {
        userMessage: string;
        hideUserBubble?: boolean;
        historyBase?: ChatMessage[];
    }) => {
        const msg = opts.userMessage.trim();
        if (!msg || isRunningRef.current) return;
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
                    ...(m.role === 'assistant' && m.toolSteps && m.toolSteps.length > 0
                        ? { toolSteps: m.toolSteps }
                        : {}),
                }));

            const sid = await genericAgentSvc.RunAgent(
                { SkillKey: skillKey, UserMessage: msg, SessionId: sessionIdRef.current ?? undefined, Messages: history },
                {
                    onToken: (token) => {
                        setMessages(prev => {
                            const last = prev[prev.length - 1];
                            if (last?.role === 'assistant' && last.isStreaming) {
                                return [...prev.slice(0, -1), { ...last, content: last.content + token }];
                            }
                            return [...prev, { role: 'assistant', content: token, isStreaming: true }];
                        });
                    },
                    onStep: (step) => {
                        const s = step as { Type: string; ToolName?: string; Description: string; IsSuccess: boolean; Details?: string };
                        if (!s.ToolName) return;
                        if (s.Type === 'tool_call') {
                            pendingCallRef.current.set(s.ToolName, { args: s.Details, startedAt: Date.now() });
                            addStep(turnIdx, { toolName: s.ToolName, label: s.Description, args: s.Details, isSuccess: true });
                        } else if (s.Type === 'tool_result') {
                            const pending = pendingCallRef.current.get(s.ToolName);
                            const durationMs = pending ? Date.now() - pending.startedAt : undefined;
                            pendingCallRef.current.delete(s.ToolName);
                            const resultLabel = extractCallAgentKeyFromLabel(s.Description)
                                ? s.Description.split('—')[0].trim()
                                : undefined;
                            addStep(turnIdx, {
                                toolName: s.ToolName,
                                result: s.Details,
                                isSuccess: s.IsSuccess,
                                durationMs,
                                ...(resultLabel ? { label: resultLabel } : {}),
                            });
                        }
                    },
                    onPlan: (plan) => setPendingPlan(plan),
                    onAskUser: (ask) => {
                        setPendingAskUser(ask);
                        setAskAnswers({});
                        setAskSelectedIds([]);
                        setAskFreeText('');
                        setIsRunning(true);
                        isRunningRef.current = true;
                    },
                    onDone: (done) => {
                        setTurnActivities(prev => {
                            const turn = prev.find(t => t.turnIndex === turnIdx);
                            const stepsForMsg = turn?.steps ? [...turn.steps] : [];
                            setMessages(msgs => {
                                const last = msgs[msgs.length - 1];
                                if (last?.role === 'assistant' && last.isStreaming) {
                                    return [...msgs.slice(0, -1), {
                                        ...last,
                                        content: done.FinalResponse || last.content,
                                        isStreaming: false,
                                        toolSteps: stepsForMsg.length > 0 ? stepsForMsg : undefined,
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
                            return prev.map(t => t.turnIndex === turnIdx ? { ...t, isComplete: true } : t);
                        });
                        setCurrentTurnIndex(i => {
                            const next = i + 1;
                            currentTurnIndexRef.current = next;
                            return next;
                        });
                        setPendingAskUser(null);
                        setIsRunning(false);
                        isRunningRef.current = false;
                    },
                    onError: (m) => {
                        setError(m);
                        setPendingAskUser(null);
                        setIsRunning(false);
                        isRunningRef.current = false;
                    },
                },
            );
            setSessionId(sid);
            sessionIdRef.current = sid;
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

    const handleConfirmPlan = async (confirmed: boolean) => {
        setPendingPlan(null);
        if (sessionId) await genericAgentSvc.ConfirmPlan(sessionId, confirmed);
    };

    const handleConfirmAskUser = async (cancelled: boolean) => {
        const ask = pendingAskUserRef.current;
        const sid = sessionIdRef.current;
        const answers = { ...askAnswers };
        const selectedIds = [...askSelectedIds];
        const freeText = askFreeText;
        const mode = (ask?.Mode || 'text').toLowerCase();
        setPendingAskUser(null);
        setAskAnswers({});
        setAskSelectedIds([]);
        setAskFreeText('');
        // Agent remains blocked/running until onDone after ConfirmAskUser unblocks the tool.
        setIsRunning(true);
        isRunningRef.current = true;
        if (!sid) return;
        await genericAgentSvc.ConfirmAskUser(sid, {
            SessionId: sid,
            Cancelled: cancelled,
            ...(cancelled
                ? {}
                : mode === 'single_choice' || mode === 'multi_choice'
                    ? { SelectedIds: selectedIds }
                    : (ask?.Fields && ask.Fields.length > 0)
                        ? { Answers: answers }
                        : { FreeText: freeText }),
        });
    };

    const handleClear = async () => {
        genericAgentSvc.disconnect();
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
        if (!testMode) await genericAgentSvc.ClearSession(skillKey);
        sessionStartFiredRef.current = true;
        if (/^Interactive$/i.test(skillExecutionModeRef.current || 'Interactive')) {
            await runAgentTurn({ userMessage: SESSION_START, hideUserBubble: true, historyBase: [] });
        }
    };

    const toggleExpand = (key: string) =>
        setExpandedTools(prev => { const n = new Set(prev); n.has(key) ? n.delete(key) : n.add(key); return n; });

    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;
    const allActivities = turnActivities;
    const hasAnyTools = allActivities.some(t => t.steps.length > 0);
    const askMode = (pendingAskUser?.Mode || 'text').toLowerCase();
    const blocked = isRunning || !!pendingAskUser;

    return (
        <div className="w-full h-full flex flex-row overflow-hidden">
            {/* ── Left: chat ── */}
            <div className="w-1 flex-auto flex flex-col overflow-hidden min-w-0">
                <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-2">
                    {messages.map((m, i) => (
                        <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                            <div className={`max-w-2xl px-3 py-2 rounded-lg text-xs whitespace-pre-wrap ${m.role === 'user' ? `${theme.button_default} ml-8` : `${theme.mainContentSection} mr-8`}`}>
                                {m.content}
                                {m.isStreaming && <span className="animate-pulse ml-1">|</span>}
                            </div>
                        </div>
                    ))}

                    {isRunning && !pendingAskUser && (
                        <div className={`flex items-center gap-2 mx-2 text-xs ${theme.label}`}>
                            <i className="fa-solid fa-spinner fa-spin" />
                            <span>{turnActivities.find(t => t.turnIndex === currentTurnIndex)?.steps.length
                                ? `Running tools…`
                                : 'Thinking…'}</span>
                        </div>
                    )}

                    {pendingPlan && (
                        <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                            <div className={`text-xs font-semibold ${theme.title}`}>Plan Review</div>
                            <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{pendingPlan.PlanSummary}</div>
                            <div className="flex gap-2">
                                <button className={btn} onClick={() => handleConfirmPlan(true)}><i className="fa-solid fa-check mr-1" />Approve</button>
                                <button className={btn} onClick={() => handleConfirmPlan(false)}><i className="fa-solid fa-xmark mr-1" />Reject</button>
                            </div>
                        </div>
                    )}

                    {pendingAskUser && (
                        <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                            <div className={`text-xs font-semibold ${theme.title}`}>Question</div>
                            <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{pendingAskUser.Prompt}</div>

                            {askMode === 'single_choice' && (pendingAskUser.Options?.length ?? 0) > 0 && (
                                <div className="flex flex-col gap-1.5">
                                    {pendingAskUser.Options!.map(opt => (
                                        <label key={opt.Id} className={`flex items-center gap-2 text-xs cursor-pointer ${theme.label}`}>
                                            <input
                                                type="radio"
                                                name="ask-user-single"
                                                checked={askSelectedIds[0] === opt.Id}
                                                onChange={() => setAskSelectedIds([opt.Id])}
                                            />
                                            <span>{opt.Label || opt.Id}</span>
                                        </label>
                                    ))}
                                </div>
                            )}

                            {askMode === 'multi_choice' && (pendingAskUser.Options?.length ?? 0) > 0 && (
                                <div className="flex flex-col gap-1.5">
                                    {pendingAskUser.Options!.map(opt => {
                                        const checked = askSelectedIds.includes(opt.Id);
                                        return (
                                            <label key={opt.Id} className={`flex items-center gap-2 text-xs cursor-pointer ${theme.label}`}>
                                                <input
                                                    type="checkbox"
                                                    checked={checked}
                                                    onChange={() => setAskSelectedIds(prev =>
                                                        checked ? prev.filter(id => id !== opt.Id) : [...prev, opt.Id]
                                                    )}
                                                />
                                                <span>{opt.Label || opt.Id}</span>
                                            </label>
                                        );
                                    })}
                                </div>
                            )}

                            {(askMode === 'text' || (askMode !== 'single_choice' && askMode !== 'multi_choice')) && (
                                (pendingAskUser.Fields && pendingAskUser.Fields.length > 0) ? (
                                    <div className="flex flex-col gap-2">
                                        {pendingAskUser.Fields.map(field => (
                                            <div key={field.Name} className="flex items-center gap-2">
                                                <label className={`w-32 text-xs shrink-0 ${theme.label}`}>
                                                    {field.Label || field.Name}
                                                    {field.Required ? ' *' : ''}
                                                </label>
                                                <input
                                                    className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                                                    value={askAnswers[field.Name] || ''}
                                                    onChange={e => setAskAnswers(prev => ({ ...prev, [field.Name]: e.target.value }))}
                                                />
                                            </div>
                                        ))}
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

                            <div className="flex gap-2">
                                <button className={btn} onClick={() => handleConfirmAskUser(false)}>
                                    <i className="fa-solid fa-check mr-1" />Submit
                                </button>
                                <button className={btn} onClick={() => handleConfirmAskUser(true)}>
                                    <i className="fa-solid fa-xmark mr-1" />Cancel
                                </button>
                            </div>
                        </div>
                    )}

                    {error && (
                        <div className="mx-2 px-3 py-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded">
                            {error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
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
                        onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); } }}
                        disabled={blocked}
                    />
                    <button className={btn} onClick={handleSend} disabled={blocked || !input.trim()}>
                        {isRunning ? <i className="fa-solid fa-spinner fa-spin" /> : <i className="fa-solid fa-paper-plane" />}
                    </button>
                    {(messages.length > 0 || pendingAskUser) && (
                        <button className={btn} onClick={() => { void handleClear(); }} title="Clear conversation">
                            <i className="fa-solid fa-rotate-left" />
                        </button>
                    )}
                    <button className={`${btn} ml-auto`} onClick={() => setSidebarOpen(o => !o)} title="Toggle sidebar">
                        <i className={`fa-solid fa-timeline`} />
                    </button>
                </div>
            </div>

            {/* ── Right: Tool Activity | Files ── */}
            {sidebarOpen && (
                <div className={`w-80 flex flex-col overflow-hidden border-l border-gray-200 ${theme.mainContentSection} shrink-0`}>
                    <div className={`flex items-center gap-1 px-2 py-1.5 border-b border-gray-200`}>
                        <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}${rightTab === 'tools' ? ' font-semibold' : ' opacity-70'}`}
                            onClick={() => setRightTab('tools')}
                        >
                            Tool Activity
                        </button>
                        <button
                            type="button"
                            className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}${rightTab === 'files' ? ' font-semibold' : ' opacity-70'}`}
                            onClick={() => setRightTab('files')}
                        >
                            Files
                        </button>
                        <button className="text-xs opacity-50 hover:opacity-100 ml-auto" onClick={() => setSidebarOpen(false)}>
                            <i className="fa-solid fa-xmark" />
                        </button>
                    </div>

                    {rightTab === 'files' ? (
                        <div className="w-full h-1 flex-auto overflow-hidden min-h-0">
                            <GenericAgentFilesPanel skillKey={skillKey} sessionKey={fileSessionKey} />
                        </div>
                    ) : (
                        <div className="w-full h-1 flex-auto overflow-y-auto flex flex-col gap-0 p-2">
                            {!hasAnyTools && !isRunning && messages.length > 0 && (
                                <div className={`p-3 rounded text-xs ${theme.label} flex flex-col gap-1`}>
                                    <div className="flex items-center gap-2 opacity-60">
                                        <i className="fa-solid fa-circle-info" />
                                        <span className="font-semibold">No tool activity</span>
                                    </div>
                                    <p className="opacity-50 leading-relaxed">
                                        No tool steps are stored for this chat view. New runs save tool activity with the session; chats saved before that change will not show past tools after reload.
                                    </p>
                                </div>
                            )}

                            {allActivities.map((turn) => (
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
                                                <div key={pair.key} className={`mb-1 rounded border ${isSuccess ? 'border-green-200' : 'border-red-200'} overflow-hidden`}>
                                                    <button
                                                        type="button"
                                                        title={tip}
                                                        className={`w-full flex items-center gap-2 px-2 py-1.5 text-xs text-left ${isSuccess ? 'bg-green-50 hover:bg-green-100' : 'bg-red-50 hover:bg-red-100'}`}
                                                        onClick={() => hasDetails && toggleExpand(expandKey)}
                                                    >
                                                        <i className={`fa-solid ${!hasResult ? 'fa-spinner fa-spin text-blue-400' : isSuccess ? 'fa-circle-check text-green-500' : 'fa-circle-xmark text-red-500'}`} />
                                                        <span className="font-mono font-semibold flex-auto truncate">{title}</span>
                                                        {pair.result?.durationMs != null && (
                                                            <span className="opacity-40 shrink-0">{pair.result.durationMs}ms</span>
                                                        )}
                                                        {hasDetails && (
                                                            <i className={`fa-solid fa-chevron-${isExpanded ? 'up' : 'down'} opacity-40 shrink-0`} />
                                                        )}
                                                    </button>
                                                    {isExpanded && (
                                                        <div className="px-2 py-1.5 flex flex-col gap-1.5">
                                                            {argsText && (
                                                                <div>
                                                                    <div className="text-xs opacity-40 mb-0.5">Args</div>
                                                                    <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${theme.label}`}>{formatToolPayload(argsText)}</pre>
                                                                </div>
                                                            )}
                                                            {resultText && (
                                                                <div>
                                                                    <div className="text-xs opacity-40 mb-0.5">{isSuccess ? 'Result' : 'Error'}</div>
                                                                    <pre className={`text-xs whitespace-pre-wrap break-all opacity-70 ${theme.label}`}>{formatToolPayload(resultText)}</pre>
                                                                </div>
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
