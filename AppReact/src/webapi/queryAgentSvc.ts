import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';
import { AgentMessage, AgentStepEvent } from './appbuilderagentsvc';

// ─────────────────────────────────────────────────────────────────────────────
// DTOs (matching C# QueryAgentDto.cs)
// ─────────────────────────────────────────────────────────────────────────────

export interface QueryAgentDoneEvent {
  FinalResponse:  string;
  GeneratedQuery: string | null;
  UpdatedHistory: AgentMessage[];
}

export interface QueryAgentRequest {
  userMessage:         string;
  dataSourceId:        number;
  selectedTables:      string[];
  conversationHistory?: AgentMessage[];
}

export interface QueryAgentEventHandlers {
  onStep:  (step: AgentStepEvent)        => void;
  onToken: (text: string)                 => void;
  onDone:  (result: QueryAgentDoneEvent) => void;
  onError: (message: string)             => void;
}

// ─────────────────────────────────────────────────────────────────────────────
// Service — polling-based streaming (same pattern as WorkflowAutomationAgentService)
//
// Flow:
//   1. POST /RunAgent  → server starts agent, returns { Object: { SessionId } }
//   2. Poll GET /PollEvents?sessionId=... every 500 ms
//   3. Dispatch events to handlers; stop polling on "done" / "error"
// ─────────────────────────────────────────────────────────────────────────────

class QueryAgentService {
  private pollTimer:        ReturnType<typeof setInterval> | null = null;
  private currentSessionId: string | null = null;

  async startSession(
    request:  QueryAgentRequest,
    handlers: QueryAgentEventHandlers
  ): Promise<void> {
    this.stopPolling();

    const url      = `${endpoints.BASE_URL}/webapi/QueryAgent/RunAgent`;
    const response = await fetch(url, {
      method:  'POST',
      headers: getHeaders(),
      body:    JSON.stringify({
        userMessage:         request.userMessage,
        dataSourceId:        request.dataSourceId,
        selectedTables:      request.selectedTables ?? [],
        conversationHistory: request.conversationHistory ?? [],
      }),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to start query agent: ${response.status} ${text}`);
    }

    const result    = await response.json();
    const sessionId = result?.Object?.SessionId as string;
    if (!sessionId) throw new Error('No session ID returned from server');

    this.currentSessionId = sessionId;
    this.startPolling(sessionId, handlers);
  }

  private startPolling(sessionId: string, handlers: QueryAgentEventHandlers): void {
    let consecutiveFailures = 0;
    const MAX_FAILURES      = 10;

    this.pollTimer = setInterval(async () => {
      try {
        const url  = `${endpoints.BASE_URL}/webapi/QueryAgent/PollEvents?sessionId=${sessionId}`;
        const resp = await fetch(url, { headers: getHeaders() });

        if (!resp.ok) {
          consecutiveFailures++;
          if (consecutiveFailures >= MAX_FAILURES) {
            this.stopPolling();
            handlers.onError(`Polling failed (${resp.status})`);
          }
          return;
        }

        consecutiveFailures = 0;
        const data = await resp.json();

        if (data?.SessionExists === false) {
          this.stopPolling();
          handlers.onError('Session not found on server. The server may have restarted.');
          return;
        }

        for (const evt of (data?.Events ?? [])) {
          if (evt.EventType === 'step'  && evt.Step)  handlers.onStep(evt.Step);
          if (evt.EventType === 'token' && evt.Token) handlers.onToken(evt.Token);
          if (evt.EventType === 'done') {
            this.stopPolling();
            handlers.onDone(evt.Done ?? { FinalResponse: '', GeneratedQuery: null, UpdatedHistory: [] });
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

  async disconnect(): Promise<void> {
    this.stopPolling();
  }

  get sessionId(): string | null {
    return this.currentSessionId;
  }
}

export const queryAgentService = new QueryAgentService();
