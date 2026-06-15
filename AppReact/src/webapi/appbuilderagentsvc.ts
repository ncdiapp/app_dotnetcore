import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints'; // BASE_URL = '/appai'

// ─────────────────────────────────────────────────────────────────────────────
// DTOs (matching C# AppBuilderAgentDto.cs)
// ─────────────────────────────────────────────────────────────────────────────

export interface AgentMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface AgentStepEvent {
  Type: 'thinking' | 'tool_call' | 'tool_result' | 'error' | 'plan';
  ToolName?: string;
  Description: string;
  Details?: string;
  IsSuccess: boolean;
  Timestamp: string;
}

export interface AgentPlanEvent {
  PlanSummary: string;
  TablesToCreate: string[];
  ScreensToCreate: string[];
  /** Non-empty when the agent is asking the user to confirm DROP TABLE (confirm_drop_tables tool). */
  TablesToDrop: string[];
  Timestamp: string;
}

export interface AgentCreatedTransaction {
  TransactionId: number;
  Name: string;
  TableName?: string;
}

export interface AgentCheckpoint {
  SaasApplicationId?: number;
  ApplicationName?: string;
  TablesCreated: string[];
  TransactionsCreated: AgentCreatedTransaction[];
  EntitiesCreated: string[];
  /** Present only when schema was approved but execute_approved_schema hasn't run yet. */
  ApprovedSchemaJson?: string;
  /** True when the agent run completed normally (all steps done). False = run was cut short or errored. */
  IsComplete?: boolean;
  Timestamp: string;
}

export interface AgentDoneEvent {
  FinalResponse: string;
  CreatedTransactions: AgentCreatedTransaction[];
  UpdatedHistory: AgentMessage[];
  Checkpoint?: AgentCheckpoint;
}

export interface AppBuilderAgentRequest {
  userMessage: string;
  dataSourceRegisterId?: number;
  schemaOwner?: string;
  conversationHistory?: AgentMessage[];
}

// ─────────────────────────────────────────────────────────────────────────────
// Callbacks the React component provides
// ─────────────────────────────────────────────────────────────────────────────

export interface AgentSchemaColumn {
  Name: string;
  DataType: string;
  IsPrimaryKey: boolean;
  IsNullable: boolean;
  IsAutoIncrement: boolean;
  Length?: number;
  DefaultValue?: string;
  Description?: string;
  FKTargetTable?: string;
  RelationshipType?: string;
}

export interface AgentSchemaTable {
  Name: string;
  Description?: string;
  IsLookup: boolean;
  ParentTable?: string;
  Columns: AgentSchemaColumn[];
}

export interface AgentSchemaEvent {
  Summary: string;
  SchemaJson: string;
  Tables: AgentSchemaTable[];
  CreateScript?: string;
  Timestamp: string;
}

export interface AgentEventHandlers {
  onStep:   (step: AgentStepEvent) => void;
  onToken:  (text: string)          => void;
  onDone:   (result: AgentDoneEvent) => void;
  onError:  (message: string)       => void;
  /** Called when the agent needs the user to approve a build plan before executing DDL. */
  onPlan:   (plan: AgentPlanEvent)  => void;
  /** Called when the agent presents a schema for review/editing before DDL. */
  onSchema: (schema: AgentSchemaEvent) => void;
}

// ─────────────────────────────────────────────────────────────────────────────
// Service — polling-based streaming (no SignalR)
//
// Flow:
//   1. POST /RunAgent  → server starts agent, returns { Object: { SessionId } }
//   2. Poll GET /PollEvents?sessionId=... every 500 ms
//   3. Dispatch each event to handlers; stop polling on "done" / "error"
// ─────────────────────────────────────────────────────────────────────────────

class AppBuilderAgentService {
  private pollTimer:       ReturnType<typeof setInterval> | null = null;
  private currentSessionId: string | null = null;

  async startSession(
    userMessage: string,
    dataSourceRegisterId: number | undefined,
    conversationHistory: AgentMessage[],
    handlers: AgentEventHandlers
  ): Promise<void> {
    this.stopPolling();

    const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/RunAgent`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ userMessage, dataSourceRegisterId, conversationHistory }),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to start agent: ${response.status} ${text}`);
    }

    const result = await response.json();
    const sessionId: string = result?.Object?.SessionId;
    if (!sessionId) throw new Error('No session ID returned from server');

    this.currentSessionId = sessionId;
    this.startPolling(sessionId, handlers);
  }

  private startPolling(sessionId: string, handlers: AgentEventHandlers): void {
    let consecutiveFailures = 0;
    const MAX_FAILURES = 10;

    this.pollTimer = setInterval(async () => {
      try {
        const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/PollEvents?sessionId=${sessionId}`;
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

        // Session not found on server — it was never created or the server restarted
        if (data?.SessionExists === false) {
          this.stopPolling();
          handlers.onError('Session not found on server. The server may have restarted.');
          return;
        }

        for (const evt of (data?.Events ?? [])) {
          if (evt.EventType === 'step'  && evt.Step)  handlers.onStep(evt.Step);
          if (evt.EventType === 'token' && evt.Token) handlers.onToken(evt.Token);
          if (evt.EventType === 'plan'   && evt.Plan)   handlers.onPlan(evt.Plan);
          if (evt.EventType === 'schema' && evt.Schema) handlers.onSchema(evt.Schema);
          if (evt.EventType === 'done') {
            this.stopPolling();
            handlers.onDone(evt.Done ?? { FinalResponse: '', CreatedTransactions: [], UpdatedHistory: [] });
            return;
          }
          if (evt.EventType === 'error') {
            this.stopPolling();
            handlers.onError(evt.Error ?? 'Unknown error');
            return;
          }
        }
      } catch {
        // network blip — keep polling
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

  /**
   * Resolve the pending propose_plan gate.
   * Call this after the user clicks Approve or Reject in the UI.
   */
  async confirmPlan(confirmed: boolean): Promise<void> {
    if (!this.currentSessionId) return;
    const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/ConfirmPlan`;
    await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId: this.currentSessionId, confirmed }),
    }).catch(() => {});
  }

  /**
   * Resolve the pending propose_schema gate.
   * Pass confirmed=true with optional edited schemaJson, or confirmed=false with feedback.
   */
  async confirmSchema(confirmed: boolean, schemaJson?: string, feedback?: string): Promise<void> {
    if (!this.currentSessionId) return;
    const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/ConfirmSchema`;
    await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        sessionId: this.currentSessionId,
        confirmed,
        schemaJson: schemaJson ?? null,
        feedback:   feedback  ?? null,
      }),
    }).catch(() => {});
  }

  async disconnect(): Promise<void> {
    this.stopPolling();
  }
}

export const appBuilderAgentService = new AppBuilderAgentService();

// ─────────────────────────────────────────────────────────────────────────────
// Session history (GET /RecentSessions)
// ─────────────────────────────────────────────────────────────────────────────

export interface AgentSessionSummary {
  SessionGuid: string;
  CreatedAt: string;
  UpdatedAt: string;
  UserRequest: string;
  Status: 'InProgress' | 'Completed' | 'Failed';
  CurrentStep?: string;
  FinalResponse?: string;
  Checkpoint?: AgentCheckpoint;
}

export async function getRecentAgentSessions(limit = 20): Promise<AgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/RecentSessions?limit=${limit}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export interface AgentSessionFull extends AgentSessionSummary {
  ConversationHistory?: AgentMessage[];
}

export async function getAgentSession(sessionGuid: string): Promise<AgentSessionFull | null> {
  const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/GetSession?sessionGuid=${encodeURIComponent(sessionGuid)}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return null;
  const data = await resp.json();
  return data?.Object ?? null;
}

export async function deleteAgentSession(sessionGuid: string): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/AppBuilderAgent/DeleteSession?sessionGuid=${encodeURIComponent(sessionGuid)}`;
  await fetch(url, { method: 'DELETE', headers: getHeaders() }).catch(() => {});
}
