import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface GenericAgentRunDto {
    SkillKey:    string;
    UserMessage: string;
    SessionId?:  string;
    Messages?:   Array<{ role: string; content: unknown }>;
}

export interface GenericAgentStartResult {
    SessionId: string;
    IsStarted: boolean;
}

export interface GenericAgentEventHandlers {
    onToken:  (text: string) => void;
    onStep:   (step: { Type: string; ToolName?: string; Description: string; IsSuccess: boolean }) => void;
    onPlan:   (plan: { PlanSummary: string }) => void;
    onDone:   (done: { FinalResponse: string }) => void;
    onError:  (message: string) => void;
}

const BASE = `${endpoints.BASE_URL}/webapi/GenericAgent`;

class GenericAgentService {
    private pollTimer: ReturnType<typeof setInterval> | null = null;
    currentSessionId: string | null = null;

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

    disconnect(): void {
        this.stopPolling();
    }

    private startPolling(sessionId: string, handlers: GenericAgentEventHandlers): void {
        let consecutiveFailures = 0;
        const MAX_FAILURES = 10;

        this.pollTimer = setInterval(async () => {
            try {
                const data: { SessionExists?: boolean; Events?: { EventType: string; Token?: string; Step?: unknown; Plan?: unknown; Done?: { FinalResponse: string }; Error?: string }[] } = await this.PollEvents(sessionId) as never;
                if (data?.SessionExists === false) {
                    this.stopPolling();
                    handlers.onError('Session not found. Server may have restarted.');
                    return;
                }
                consecutiveFailures = 0;
                for (const evt of (data?.Events ?? [])) {
                    if (evt.EventType === 'token' && evt.Token) handlers.onToken(evt.Token);
                    if (evt.EventType === 'step'  && evt.Step)  handlers.onStep(evt.Step as never);
                    if (evt.EventType === 'plan'  && evt.Plan)  handlers.onPlan(evt.Plan as never);
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
