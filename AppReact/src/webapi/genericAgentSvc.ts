import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface GenericAgentRunDto {
    SkillKey: string;
    UserMessage: string;
    SessionId?: string;
    /** Stable AppGenericAgentSession.SessionKey. Omit for fixed test key SkillKey:UserId. */
    ChatSessionKey?: string;
    Messages?: Array<{
        role: string;
        content: unknown;
        toolSteps?: Array<{ toolName: string; label?: string; args?: string; result?: string; isSuccess?: boolean; durationMs?: number }>;
    }>;
}

export interface GenericAgentStartResult {
    SessionId: string;
    ChatSessionKey?: string;
    IsStarted: boolean;
}

export interface GenericAgentChatSummary {
    SessionKey: string;
    SkillKey: string;
    Title?: string | null;
    UpdatedAt: string;
    IsFixedTestSession: boolean;
}

export function genericAgentChatTitle(item?: { Title?: string | null } | null): string {
    const text = (item?.Title || '').trim();
    return text || 'New Chat';
}

export interface GenericAgentFile {
    RelativePath: string;
    SizeBytes: number;
    UpdatedAt: string;
    IsDirectory: boolean;
}

export interface AskUserField {
    Name: string;
    Label?: string;
    Required?: boolean;
    /** text (default) | select — select uses Options as dropdown */
    Type?: string;
    Options?: LookupItemDto[];
}

/** Matches APP.Components.Dto.LookupItemDto (Id + Display). */
export interface LookupItemDto {
    Id: string | number;
    Display?: string;
}

export interface AskUserEvent {
    Prompt: string;
    Mode: string;
    /** radio (default) | button_group — button_group one-click select+submit */
    Ui?: string;
    /** vertical (default) | horizontal — button_group layout */
    Layout?: string;
    Fields?: AskUserField[];
    Options?: LookupItemDto[];
    ContextKey?: string;
}

export interface ConfirmAskUserDto {
    SessionId: string;
    Cancelled: boolean;
    Answers?: Record<string, string>;
    SelectedIds?: string[];
    FreeText?: string;
    SkillKey?: string;
    ChatSessionKey?: string;
}

/** In-memory UI state so Agent Chat survives App-tab remount without restarting. */
export interface GenericAgentChatUiSnapshot {
    skillKey: string;
    chatSessionKey?: string | null;
    messages: Array<{
        role: 'user' | 'assistant';
        content: string;
        isStreaming?: boolean;
        toolSteps?: Array<{
            toolName: string;
            label?: string;
            args?: string;
            result?: string;
            isSuccess: boolean;
            durationMs?: number;
        }>;
    }>;
    turnActivities: Array<{
        turnIndex: number;
        steps: Array<{
            toolName: string;
            label?: string;
            args?: string;
            result?: string;
            isSuccess: boolean;
            durationMs?: number;
        }>;
        isComplete: boolean;
    }>;
    currentTurnIndex: number;
    sessionId: string | null;
    pendingAskUser: AskUserEvent | null;
    pendingPlan: { PlanSummary: string } | null;
    askAnswers: Record<string, string>;
    askSelectedIds: string[];
    askFreeText: string;
    isRunning: boolean;
    error: string | null;
    skillExecutionMode: string;
}

export interface GenericAgentEventHandlers {
    onToken: (text: string) => void;
    onStep: (step: { Type: string; ToolName?: string; Description: string; IsSuccess: boolean }) => void;
    onPlan: (plan: { PlanSummary: string }) => void;
    onAskUser?: (ask: AskUserEvent) => void;
    onDone: (done: { FinalResponse: string }) => void;
    onError: (message: string) => void;
}

const BASE = `${endpoints.BASE_URL}/webapi/GenericAgent`;

class GenericAgentService {
    private pollTimer: ReturnType<typeof setInterval> | null = null;
    private activeHandlers: GenericAgentEventHandlers | null = null;
    currentSessionId: string | null = null;
    currentChatSessionKey: string | null = null;

    async RunAgent(dto: GenericAgentRunDto, handlers: GenericAgentEventHandlers): Promise<string> {
        this.stopPolling();
        const res = await fetch(`${BASE}/RunAgent`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify(dto),
        });
        if (!res.ok) throw new Error(`RunAgent failed (${res.status})`);
        const data = await res.json();
        const err = data?.ValidationResult?.Items?.find((i: { Message: string }) => i.Message);
        if (data?.ValidationResult?.IsValid === false && err) throw new Error(err.Message);
        const sessionId: string = data?.Object?.SessionId;
        if (!sessionId) throw new Error('No session ID returned');
        this.currentSessionId = sessionId;
        this.currentChatSessionKey = data?.Object?.ChatSessionKey ?? dto.ChatSessionKey ?? null;
        this.startPolling(sessionId, handlers);
        return sessionId;
    }

    getStreamUrl(sessionId: string): string {
        return `${endpoints.BASE_URL}/webapi/GenericAgent/StreamEvents?sessionId=${encodeURIComponent(sessionId)}`;
    }

    async PollEvents(sessionId: string): Promise<unknown> {
        const res = await fetch(`${BASE}/PollEvents?sessionId=${encodeURIComponent(sessionId)}`, {
            headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`PollEvents failed (${res.status})`);
        return res.json();
    }

    async ConfirmPlan(sessionId: string, confirmed: boolean): Promise<void> {
        await fetch(`${BASE}/ConfirmPlan`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ sessionId, confirmed }),
        }).catch(() => {});
    }

    async ConfirmAskUser(sessionId: string, body: ConfirmAskUserDto): Promise<boolean> {
        try {
            const res = await fetch(`${BASE}/ConfirmAskUser`, {
                method: 'POST',
                headers: getHeaders(),
                body: JSON.stringify({ ...body, SessionId: body.SessionId || sessionId }),
            });
            if (!res.ok) return false;
            const data = await res.json();
            // Object=true when pending TCS was found and completed.
            return data?.Object === true;
        } catch {
            return false;
        }
    }

    async GetFixedSessionKey(skillKey: string): Promise<string | null> {
        try {
            const res = await fetch(`${BASE}/GetFixedSessionKey?skillKey=${encodeURIComponent(skillKey)}`, {
                headers: getHeaders(),
            });
            if (!res.ok) return null;
            const data = await res.json();
            return data?.Object ?? null;
        } catch {
            return null;
        }
    }

    /** Fixed test session: SessionKey = SkillKey:UserId. toolSteps optional on assistant messages. */
    async LoadSession(skillKey: string): Promise<Array<{
        role: string;
        content: string;
        toolSteps?: Array<{ toolName: string; label?: string; args?: string; result?: string; isSuccess?: boolean; durationMs?: number }>;
    }> | null> {
        try {
            const res = await fetch(`${BASE}/LoadSession?skillKey=${encodeURIComponent(skillKey)}`, {
                headers: getHeaders(),
            });
            if (!res.ok) return null;
            const data = await res.json();
            return data?.Object ?? null;
        } catch {
            return null;
        }
    }

    async LoadChat(skillKey: string, sessionKey: string): Promise<{
        SessionKey: string;
        Title?: string | null;
        Messages: Array<{
            role: string;
            content: string;
            toolSteps?: Array<{ toolName: string; label?: string; args?: string; result?: string; isSuccess?: boolean; durationMs?: number }>;
            pendingAskUser?: AskUserEvent;
            PendingAskUser?: AskUserEvent;
            runSessionId?: string;
            RunSessionId?: string;
        }>;
    } | null> {
        try {
            const res = await fetch(
                `${BASE}/LoadChat?skillKey=${encodeURIComponent(skillKey)}&sessionKey=${encodeURIComponent(sessionKey)}`,
                { headers: getHeaders() }
            );
            if (!res.ok) return null;
            const data = await res.json();
            return data?.Object ?? null;
        } catch {
            return null;
        }
    }

    async ListChats(skillKey: string): Promise<GenericAgentChatSummary[]> {
        try {
            const res = await fetch(`${BASE}/ListChats?skillKey=${encodeURIComponent(skillKey)}`, {
                headers: getHeaders(),
            });
            if (!res.ok) return [];
            const data = await res.json();
            return data?.Object ?? [];
        } catch {
            return [];
        }
    }

    async CreateChat(skillKey: string): Promise<GenericAgentChatSummary | null> {
        try {
            const res = await fetch(`${BASE}/CreateChat?skillKey=${encodeURIComponent(skillKey)}`, {
                method: 'POST',
                headers: getHeaders(),
            });
            if (!res.ok) return null;
            const data = await res.json();
            return data?.Object ?? null;
        } catch {
            return null;
        }
    }

    async ClearSession(skillKey: string, sessionKey?: string): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '' });
        if (sessionKey) q.set('sessionKey', sessionKey);
        await fetch(`${BASE}/ClearSession?${q.toString()}`, {
            method: 'POST',
            headers: getHeaders(),
        }).catch(() => {});
    }

    async DeleteChat(skillKey: string, sessionKey: string): Promise<void> {
        await fetch(
            `${BASE}/DeleteChat?skillKey=${encodeURIComponent(skillKey)}&sessionKey=${encodeURIComponent(sessionKey)}`,
            { method: 'POST', headers: getHeaders() }
        ).catch(() => {});
    }

    async RenameChat(skillKey: string, sessionKey: string, title: string): Promise<boolean> {
        const res = await fetch(`${BASE}/RenameChat`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({
                SkillKey: skillKey || '',
                SessionKey: sessionKey || '',
                Title: title || '',
            }),
        });
        if (!res.ok) throw new Error('Failed to rename chat');
        const data = await res.json();
        return !!data?.Object;
    }

    fileScopeQuery(skillKey: string, sessionKey: string | null | undefined, path?: string, fileScope?: string): URLSearchParams {
        const q = new URLSearchParams({
            skillKey: skillKey || '',
            sessionKey: sessionKey || '',
            path: path || '',
            scope: fileScope || '',
        });
        return q;
    }

    async ListAgentFiles(skillKey: string, sessionKey: string | null | undefined, path?: string, fileScope?: string): Promise<GenericAgentFile[]> {
        const q = this.fileScopeQuery(skillKey, sessionKey, path, fileScope);
        const res = await fetch(`${BASE}/ListAgentFiles?${q}`, { headers: getHeaders() });
        if (!res.ok) return [];
        const data = await res.json();
        return data?.Object ?? [];
    }

    async ReadAgentFile(skillKey: string, sessionKey: string | null | undefined, path: string, fileScope?: string): Promise<{ Content: string; Truncated?: boolean } | null> {
        const q = this.fileScopeQuery(skillKey, sessionKey, path, fileScope);
        const res = await fetch(`${BASE}/ReadAgentFile?${q}`, { headers: getHeaders() });
        if (!res.ok) return null;
        const data = await res.json();
        return data?.Object ?? null;
    }

    async WriteAgentFile(skillKey: string, sessionKey: string | null | undefined, relativePath: string, content: string, fileScope?: string): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '', scope: fileScope || '' });
        await fetch(`${BASE}/WriteAgentFile?${q}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey || '', RelativePath: relativePath, Content: content }),
        });
    }

    async MkdirAgentFile(skillKey: string, sessionKey: string | null | undefined, relativePath: string, fileScope?: string): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '', scope: fileScope || '' });
        await fetch(`${BASE}/MkdirAgentFile?${q}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey || '', RelativePath: relativePath }),
        });
    }

    async RenameAgentFile(skillKey: string, sessionKey: string | null | undefined, relativePath: string, newPath: string, fileScope?: string): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '', scope: fileScope || '' });
        await fetch(`${BASE}/RenameAgentFile?${q}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey || '', RelativePath: relativePath, NewPath: newPath }),
        });
    }

    async DeleteAgentFile(skillKey: string, sessionKey: string | null | undefined, relativePath: string, fileScope?: string): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '', scope: fileScope || '' });
        await fetch(`${BASE}/DeleteAgentFile?${q}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey || '', RelativePath: relativePath }),
        });
    }

    async UploadAgentFile(skillKey: string, sessionKey: string | null | undefined, path: string, file: File, fileScope?: string): Promise<void> {
        const q = this.fileScopeQuery(skillKey, sessionKey, path, fileScope);
        const form = new FormData();
        form.append('file', file);
        // Let the browser set multipart boundary — do not copy JSON Content-Type from getHeaders().
        const headers = new Headers(getHeaders());
        headers.delete('Content-Type');
        await fetch(`${BASE}/UploadAgentFile?${q}`, { method: 'POST', headers, body: form });
    }

    downloadAgentFileUrl(skillKey: string, sessionKey: string | null | undefined, path: string, fileScope?: string): string {
        const q = this.fileScopeQuery(skillKey, sessionKey, path, fileScope);
        return `${BASE}/DownloadAgentFile?${q}`;
    }

    async RestoreDefaultSourceFiles(skillKey: string, sessionKey: string): Promise<number> {
        const q = new URLSearchParams({ skillKey: skillKey || '', sessionKey: sessionKey || '' });
        const res = await fetch(`${BASE}/RestoreDefaultSourceFiles?${q}`, {
            method: 'POST',
            headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`Restore failed (${res.status})`);
        const data = await res.json();
        return data?.Object ?? 0;
    }

    async DownloadAgentFile(skillKey: string, sessionKey: string | null | undefined, relativePath: string, fileScope?: string): Promise<void> {
        const res = await fetch(this.downloadAgentFileUrl(skillKey, sessionKey, relativePath, fileScope), {
            headers: getHeaders(),
        });
        if (!res.ok) throw new Error(`Download failed (${res.status})`);
        const blob = await res.blob();
        const name = relativePath.split(/[/\\]/).pop() || 'agent-file';
        const objectUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = name;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    }

    async GetActiveAgents(): Promise<Array<{ SkillKey: string; AgentName: string }>> {
        try {
            const res = await fetch(`${BASE}/GetActiveAgents`, { headers: getHeaders() });
            if (!res.ok) return [];
            const data = await res.json();
            return data?.Object ?? [];
        } catch {
            return [];
        }
    }

    disconnect(): void {
        this.stopPolling();
        this.activeHandlers = null;
    }

    /** True while PollEvents loop is active for the current RunAgent session. */
    isPolling(): boolean {
        return this.pollTimer !== null;
    }

    /** Rebind UI handlers after remount without starting a new RunAgent. */
    reattachHandlers(handlers: GenericAgentEventHandlers): void {
        this.activeHandlers = handlers;
    }

    /** Resume PollEvents for a live HITL / in-progress run after switching chats. */
    resumePolling(sessionId: string, handlers: GenericAgentEventHandlers, chatSessionKey?: string | null): void {
        if (!sessionId) return;
        this.stopPolling();
        this.currentSessionId = sessionId;
        this.currentChatSessionKey = chatSessionKey ?? this.currentChatSessionKey;
        this.startPolling(sessionId, handlers);
    }

    private startPolling(sessionId: string, handlers: GenericAgentEventHandlers): void {
        let consecutiveFailures = 0;
        const MAX_FAILURES = 10;
        this.activeHandlers = handlers;

        this.pollTimer = setInterval(async () => {
            const h = this.activeHandlers;
            if (!h) return;
            try {
                const data: {
                    SessionExists?: boolean;
                    Events?: {
                        EventType: string;
                        Token?: string;
                        Step?: unknown;
                        Plan?: unknown;
                        AskUser?: AskUserEvent;
                        Done?: { FinalResponse: string };
                        Error?: string;
                    }[];
                } = (await this.PollEvents(sessionId)) as never;
                if (data?.SessionExists === false) {
                    this.stopPolling();
                    h.onError('Session not found. Server may have restarted.');
                    return;
                }
                consecutiveFailures = 0;
                for (const evt of data?.Events ?? []) {
                    if (evt.EventType === 'token' && evt.Token) h.onToken(evt.Token);
                    if (evt.EventType === 'step' && evt.Step) h.onStep(evt.Step as never);
                    if (evt.EventType === 'plan' && evt.Plan) h.onPlan(evt.Plan as never);
                    if (evt.EventType === 'ask_user' && evt.AskUser) h.onAskUser?.(evt.AskUser);
                    if (evt.EventType === 'done') {
                        this.stopPolling();
                        h.onDone(evt.Done ?? { FinalResponse: '' });
                        return;
                    }
                    if (evt.EventType === 'error') {
                        this.stopPolling();
                        h.onError(evt.Error ?? 'Unknown error');
                        return;
                    }
                }
            } catch {
                consecutiveFailures++;
                if (consecutiveFailures >= MAX_FAILURES) {
                    this.stopPolling();
                    h.onError('Lost connection to server after multiple retries.');
                }
            }
        }, 500);
    }

    private stopPolling(): void {
        if (this.pollTimer !== null) {
            clearInterval(this.pollTimer);
            this.pollTimer = null;
        }
    }
}

export const genericAgentSvc = new GenericAgentService();
