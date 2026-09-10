import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface GenericAgentRunDto {
    SkillKey: string;
    UserMessage: string;
    SessionId?: string;
    /** Stable AppGenericAgentSession.SessionKey. Omit for fixed test key SkillKey:UserId. */
    ChatSessionKey?: string;
    Messages?: Array<{ role: string; content: unknown }>;
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

export interface GenericAgentFile {
    RelativePath: string;
    SizeBytes: number;
    UpdatedAt: string;
    IsDirectory: boolean;
}

export interface GenericAgentEventHandlers {
    onToken: (text: string) => void;
    onStep: (step: { Type: string; ToolName?: string; Description: string; IsSuccess: boolean }) => void;
    onPlan: (plan: { PlanSummary: string }) => void;
    onDone: (done: { FinalResponse: string }) => void;
    onError: (message: string) => void;
}

const BASE = `${endpoints.BASE_URL}/webapi/GenericAgent`;

class GenericAgentService {
    private pollTimer: ReturnType<typeof setInterval> | null = null;
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

    /** Fixed test session: SessionKey = SkillKey:UserId */
    async LoadSession(skillKey: string): Promise<Array<{ role: string; content: string }> | null> {
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
        Messages: Array<{ role: string; content: string }>;
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

    async ClearSession(skillKey: string): Promise<void> {
        await fetch(`${BASE}/ClearSession?skillKey=${encodeURIComponent(skillKey)}`, {
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

    async ListAgentFiles(skillKey: string, sessionKey: string, path?: string): Promise<GenericAgentFile[]> {
        const q = new URLSearchParams({
            skillKey: skillKey || '',
            sessionKey: sessionKey || '',
            path: path || '',
        });
        const res = await fetch(`${BASE}/ListAgentFiles?${q}`, { headers: getHeaders() });
        if (!res.ok) return [];
        const data = await res.json();
        return data?.Object ?? [];
    }

    async ReadAgentFile(skillKey: string, sessionKey: string, path: string): Promise<{ Content: string; Truncated?: boolean } | null> {
        const q = new URLSearchParams({ skillKey: skillKey || '', sessionKey: sessionKey || '', path: path || '' });
        const res = await fetch(`${BASE}/ReadAgentFile?${q}`, { headers: getHeaders() });
        if (!res.ok) return null;
        const data = await res.json();
        return data?.Object ?? null;
    }

    async WriteAgentFile(skillKey: string, sessionKey: string, relativePath: string, content: string): Promise<void> {
        await fetch(`${BASE}/WriteAgentFile?skillKey=${encodeURIComponent(skillKey || '')}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey, RelativePath: relativePath, Content: content }),
        });
    }

    async MkdirAgentFile(skillKey: string, sessionKey: string, relativePath: string): Promise<void> {
        await fetch(`${BASE}/MkdirAgentFile?skillKey=${encodeURIComponent(skillKey || '')}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey, RelativePath: relativePath }),
        });
    }

    async RenameAgentFile(skillKey: string, sessionKey: string, relativePath: string, newPath: string): Promise<void> {
        await fetch(`${BASE}/RenameAgentFile?skillKey=${encodeURIComponent(skillKey || '')}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey, RelativePath: relativePath, NewPath: newPath }),
        });
    }

    async DeleteAgentFile(skillKey: string, sessionKey: string, relativePath: string): Promise<void> {
        await fetch(`${BASE}/DeleteAgentFile?skillKey=${encodeURIComponent(skillKey || '')}`, {
            method: 'POST',
            headers: getHeaders(),
            body: JSON.stringify({ SessionKey: sessionKey, RelativePath: relativePath }),
        });
    }

    async UploadAgentFile(skillKey: string, sessionKey: string, path: string, file: File): Promise<void> {
        const q = new URLSearchParams({ skillKey: skillKey || '', sessionKey: sessionKey || '', path: path || '' });
        const form = new FormData();
        form.append('file', file);
        const headers = { ...getHeaders() } as Record<string, string>;
        delete headers['Content-Type'];
        await fetch(`${BASE}/UploadAgentFile?${q}`, { method: 'POST', headers, body: form });
    }

    downloadAgentFileUrl(skillKey: string, sessionKey: string, path: string): string {
        const q = new URLSearchParams({ skillKey: skillKey || '', sessionKey: sessionKey || '', path: path || '' });
        return `${BASE}/DownloadAgentFile?${q}`;
    }

    async DownloadAgentFile(skillKey: string, sessionKey: string, relativePath: string): Promise<void> {
        const res = await fetch(this.downloadAgentFileUrl(skillKey, sessionKey, relativePath), {
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

    async GetActiveAgents(): Promise<Array<{ SkillKey: string; AgentName: string; AgentUi: number }>> {
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
    }

    private startPolling(sessionId: string, handlers: GenericAgentEventHandlers): void {
        let consecutiveFailures = 0;
        const MAX_FAILURES = 10;

        this.pollTimer = setInterval(async () => {
            try {
                const data: {
                    SessionExists?: boolean;
                    Events?: {
                        EventType: string;
                        Token?: string;
                        Step?: unknown;
                        Plan?: unknown;
                        Done?: { FinalResponse: string };
                        Error?: string;
                    }[];
                } = (await this.PollEvents(sessionId)) as never;
                if (data?.SessionExists === false) {
                    this.stopPolling();
                    handlers.onError('Session not found. Server may have restarted.');
                    return;
                }
                consecutiveFailures = 0;
                for (const evt of data?.Events ?? []) {
                    if (evt.EventType === 'token' && evt.Token) handlers.onToken(evt.Token);
                    if (evt.EventType === 'step' && evt.Step) handlers.onStep(evt.Step as never);
                    if (evt.EventType === 'plan' && evt.Plan) handlers.onPlan(evt.Plan as never);
                    if (evt.EventType === 'done') {
                        this.stopPolling();
                        handlers.onDone(evt.Done ?? { FinalResponse: '' });
                        return;
                    }
                    if (evt.EventType === 'error') {
                        this.stopPolling();
                        handlers.onError(evt.Error ?? 'Unknown error');
                        return;
                    }
                }
            } catch {
                consecutiveFailures++;
                if (consecutiveFailures >= MAX_FAILURES) {
                    this.stopPolling();
                    handlers.onError('Lost connection to server after multiple retries.');
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
