import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';

// ─────────────────────────────────────────────────────────────────────────────
// DTOs (matching C# AppReportAgentDto.cs)
// ─────────────────────────────────────────────────────────────────────────────

export interface AgentReportMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AgentReportStepEvent {
  Type: 'thinking' | 'tool_call' | 'tool_result' | 'error';
  ToolName?: string;
  Description: string;
  Details?: string;
  IsSuccess: boolean;
  Timestamp: string;
}

export interface AgentReportResultEvent {
  SearchDefinitionId: number;
  SearchName: string;
  /** "grid" | "pivot" | "gantt" */
  ViewType: string;
  SearchDto: any;
  ViewDto: any;
  SearchResultRows: any[];
  RowCount: number;
  Timestamp: string;
}

export interface AgentReportDoneEvent {
  FinalResponse: string;
  ReportResult: AgentReportResultEvent | null;
  UpdatedHistory: AgentReportMessage[];
  Timestamp: string;
}

export interface AgentReportEventDto {
  EventType: 'step' | 'token' | 'done' | 'error';
  Step?: AgentReportStepEvent;
  Token?: string;
  Done?: AgentReportDoneEvent;
  Error?: string;
}

export interface AgentReportPollResponse {
  Events: AgentReportEventDto[];
  SessionExists: boolean;
}

export interface AgentReportStartResult {
  IsStarted: boolean;
  SessionId: string;
  Error?: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Event handler callbacks
// ─────────────────────────────────────────────────────────────────────────────

export interface AgentReportEventHandlers {
  onStep?:  (event: AgentReportStepEvent)   => void;
  onToken?: (text: string)                  => void;
  onDone?:  (event: AgentReportDoneEvent)   => void;
  onError?: (message: string)               => void;
}

// ─────────────────────────────────────────────────────────────────────────────
// Service class
// ─────────────────────────────────────────────────────────────────────────────

class AppReportAgentServiceClass {
  private pollingInterval: ReturnType<typeof setInterval> | null = null;
  private currentSessionId: string | null = null;

  /**
   * Start an agent session, then begin polling for events.
   */
  async startSession(
    userMessage: string,
    dataSourceRegisterId: number | null,
    conversationHistory: AgentReportMessage[],
    handlers: AgentReportEventHandlers
  ): Promise<string | null> {
    try {
      const body = {
        UserMessage:          userMessage,
        DataSourceRegisterId: dataSourceRegisterId ?? null,
        ConversationHistory:  conversationHistory,
      };

      const response = await fetch(`${endpoints.BASE_URL}/webapi/AppReportAgent/RunAgent`, {
        method:  'POST',
        headers: { ...getHeaders(), 'Content-Type': 'application/json' },
        body:    JSON.stringify(body),
      });

      if (!response.ok) throw new Error(`HTTP ${response.status}`);

      const result = await response.json();
      const data: AgentReportStartResult = result?.Object ?? result;

      if (!data?.IsStarted || !data?.SessionId) {
        handlers.onError?.(`Failed to start session: ${data?.Error ?? 'unknown error'}`);
        return null;
      }

      this.currentSessionId = data.SessionId;
      this.startPolling(data.SessionId, handlers);
      return data.SessionId;
    } catch (err: any) {
      handlers.onError?.(`Network error: ${err?.message ?? String(err)}`);
      return null;
    }
  }

  /**
   * Poll for events every 500 ms.
   */
  startPolling(sessionId: string, handlers: AgentReportEventHandlers): void {
    this.stopPolling();

    this.pollingInterval = setInterval(async () => {
      try {
        const response = await fetch(
          `${endpoints.BASE_URL}/webapi/AppReportAgent/PollEvents?sessionId=${sessionId}`,
          { headers: getHeaders() }
        );

        if (!response.ok) return;

        const data: AgentReportPollResponse = await response.json();

        if (!data?.SessionExists && !data?.Events?.length) return;

        for (const event of data.Events ?? []) {
          switch (event.EventType) {
            case 'step':
              if (event.Step) handlers.onStep?.(event.Step);
              break;
            case 'token':
              if (event.Token) handlers.onToken?.(event.Token);
              break;
            case 'done':
              if (event.Done) handlers.onDone?.(event.Done);
              this.stopPolling();
              break;
            case 'error':
              handlers.onError?.(event.Error ?? 'Unknown error');
              this.stopPolling();
              break;
          }
        }
      } catch {
        // Silently swallow transient network errors during polling
      }
    }, 500);
  }

  stopPolling(): void {
    if (this.pollingInterval !== null) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }

  disconnect(): void {
    this.stopPolling();
    this.currentSessionId = null;
  }
}

export const AppReportAgentService = new AppReportAgentServiceClass();
