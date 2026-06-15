import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';
import { AgentMessage, AgentStepEvent } from './appbuilderagentsvc';

// ─────────────────────────────────────────────────────────────────────────────
// DTOs (matching C# WorkflowAutomationAgentDto.cs)
// ─────────────────────────────────────────────────────────────────────────────

export interface WfAgentTaskResult {
  TaskId: number;
  Name: string;
  ActionType: number;
  SortOrder: number;
  /** Future canvas support: optional X position */
  PositionX?: number;
  /** Future canvas support: optional Y position */
  PositionY?: number;
}

export interface WfAgentDoneEvent {
  FinalResponse: string;
  UpdatedHistory: AgentMessage[];
  CreatedOrModifiedTasks: WfAgentTaskResult[];
}

export interface WfAgentPlanEvent {
  PlanSummary: string;
  TasksToCreate: string[];
  TasksToModify: string[];
  TasksToDelete: string[];
  Timestamp: string;
}

export interface WfAgentRequest {
  userMessage: string;
  transactionId: number;
  conversationHistory?: AgentMessage[];
}

export interface WfAgentEventHandlers {
  onStep:  (step: AgentStepEvent)    => void;
  onToken: (text: string)             => void;
  onDone:  (result: WfAgentDoneEvent) => void;
  onError: (message: string)          => void;
  /** Called when the agent needs the user to approve proposed workflow changes. */
  onPlan:  (plan: WfAgentPlanEvent)  => void;
}

// ─────────────────────────────────────────────────────────────────────────────
// Service — polling-based streaming (same pattern as AppBuilderAgentService)
//
// Flow:
//   1. POST /RunAgent  → server starts agent, returns { Object: { SessionId } }
//   2. Poll GET /PollEvents?sessionId=... every 500 ms
//   3. Dispatch events to handlers; stop polling on "done" / "error"
// ─────────────────────────────────────────────────────────────────────────────

class WorkflowAutomationAgentService {
  private pollTimer:        ReturnType<typeof setInterval> | null = null;
  private currentSessionId: string | null = null;

  async startSession(
    request: WfAgentRequest,
    handlers: WfAgentEventHandlers
  ): Promise<void> {
    this.stopPolling();

    const url = `${endpoints.BASE_URL}/webapi/WorkflowAutomationAgent/RunAgent`;
    const response = await fetch(url, {
      method:  'POST',
      headers: getHeaders(),
      body:    JSON.stringify({
        userMessage:         request.userMessage,
        transactionId:       request.transactionId,
        conversationHistory: request.conversationHistory ?? [],
      }),
    });

    if (!response.ok) {
      const text = await response.text();
      throw new Error(`Failed to start workflow agent: ${response.status} ${text}`);
    }

    const result     = await response.json();
    const sessionId: string = result?.Object?.SessionId;
    if (!sessionId) throw new Error('No session ID returned from server');

    this.currentSessionId = sessionId;
    this.startPolling(sessionId, handlers);
  }

  private startPolling(sessionId: string, handlers: WfAgentEventHandlers): void {
    let consecutiveFailures = 0;
    const MAX_FAILURES = 10;

    this.pollTimer = setInterval(async () => {
      try {
        const url  = `${endpoints.BASE_URL}/webapi/WorkflowAutomationAgent/PollEvents?sessionId=${sessionId}`;
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
          if (evt.EventType === 'plan'  && evt.Plan)  handlers.onPlan(evt.Plan);
          if (evt.EventType === 'done') {
            this.stopPolling();
            handlers.onDone(evt.Done ?? { FinalResponse: '', UpdatedHistory: [], CreatedOrModifiedTasks: [] });
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

  /**
   * Resolve the pending propose_workflow_changes gate.
   * Call after the user clicks Approve or Reject in WfPlanApprovalCard.
   */
  async confirmPlan(isApproved: boolean, feedback?: string): Promise<void> {
    if (!this.currentSessionId) return;
    const url = `${endpoints.BASE_URL}/webapi/WorkflowAutomationAgent/ConfirmPlan`;
    await fetch(url, {
      method:  'POST',
      headers: getHeaders(),
      body:    JSON.stringify({
        sessionId:  this.currentSessionId,
        isApproved,
        feedback:   feedback ?? null,
      }),
    }).catch(() => {});
  }

  async disconnect(): Promise<void> {
    this.stopPolling();
  }

  get sessionId(): string | null {
    return this.currentSessionId;
  }
}

export const workflowAutomationAgentService = new WorkflowAutomationAgentService();
