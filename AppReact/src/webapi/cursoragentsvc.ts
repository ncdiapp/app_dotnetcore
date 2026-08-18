import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';

export interface CursorAgentMessage {
  Role?: string;
  Content?: string;
  role?: string;
  content?: string;
}

export interface CursorAgentStepEvent {
  Type: string;
  ToolName?: string;
  Description: string;
  Details?: string;
  IsSuccess: boolean;
  Timestamp: string;
}

export interface CursorAgentGateEvent {
  GateId: string;
  Kind: 'import_pack' | 'exec_sql' | string;
  Title: string;
  Summary: string;
  RelativePath?: string;
  Sql?: string;
  DataSourceRegisterId?: number;
  Preview?: any;
  Timestamp: string;
}

export interface CursorAgentFileEvent {
  Action: string;
  RelativePath: string;
}

export interface CursorAgentDoneEvent {
  FinalResponse: string;
  UpdatedHistory: CursorAgentMessage[];
  WorkspaceFiles: string[];
}

export interface CursorAgentEventHandlers {
  onStep: (step: CursorAgentStepEvent) => void;
  onToken: (text: string) => void;
  onFile?: (file: CursorAgentFileEvent) => void;
  onGate: (gate: CursorAgentGateEvent) => void;
  onDone: (result: CursorAgentDoneEvent) => void;
  onError: (message: string) => void;
}

export interface CursorAgentSessionSummary {
  SessionGuid: string;
  CreatedAt: string;
  UpdatedAt: string;
  UserRequest: string;
  Status: string;
  CursorAgentId?: string;
  SaasApplicationId?: number;
  DataSourceRegisterId?: number;
  WorkspaceRelativePath?: string;
  FinalResponse?: string;
}

export interface CursorAgentWorkspaceFile {
  RelativePath: string;
  SizeBytes: number;
  UpdatedAt: string;
  IsDirectory: boolean;
  PublicUrl?: string;
}

class CursorAgentService {
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  currentSessionId: string | null = null;

  async startSession(
    userMessage: string,
    saasApplicationId: number,
    dataSourceRegisterId: number | undefined,
    conversationHistory: CursorAgentMessage[],
    handlers: CursorAgentEventHandlers
  ): Promise<string> {
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorAgent/StartSession`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ userMessage, saasApplicationId, dataSourceRegisterId, conversationHistory }),
    });
    if (!response.ok) throw new Error(`Failed to start Cursor Agent: ${response.status}`);
    const result = await response.json();
    const err = result?.ValidationResult?.Items?.find((i: any) => i.Type === 'Error' || i.ItemType === 1);
    if (err?.Message) throw new Error(err.Message);
    const sessionId: string = result?.Object?.SessionId;
    if (!sessionId) throw new Error('No session ID returned from server');
    this.currentSessionId = sessionId;
    this.startPolling(sessionId, handlers);
    return sessionId;
  }

  async followUp(userMessage: string, handlers: CursorAgentEventHandlers): Promise<void> {
    if (!this.currentSessionId) throw new Error('No active session');
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorAgent/FollowUp`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId: this.currentSessionId, userMessage }),
    });
    if (!response.ok) throw new Error(`Follow-up failed: ${response.status}`);
    const result = await response.json();
    const err = result?.ValidationResult?.Items?.find((i: any) => i.Message);
    if (result?.ValidationResult?.IsValid === false && err?.Message) throw new Error(err.Message);
    this.startPolling(this.currentSessionId, handlers);
  }

  async resume(sessionId: string, userMessage: string, handlers: CursorAgentEventHandlers): Promise<void> {
    this.stopPolling();
    this.currentSessionId = sessionId;
    const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ResumeSession`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId, userMessage }),
    });
    if (!response.ok) throw new Error(`Resume failed: ${response.status}`);
    this.startPolling(sessionId, handlers);
  }

  async confirmGate(gateId: string, confirmed: boolean, feedback?: string): Promise<void> {
    if (!this.currentSessionId) return;
    await fetch(`${endpoints.BASE_URL}/webapi/CursorAgent/ConfirmGate`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        sessionId: this.currentSessionId,
        gateId,
        confirmed,
        feedback: feedback || null,
      }),
    }).catch(() => {});
  }

  async cancel(): Promise<void> {
    if (!this.currentSessionId) return;
    this.stopPolling();
    await fetch(`${endpoints.BASE_URL}/webapi/CursorAgent/Cancel`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId: this.currentSessionId }),
    }).catch(() => {});
  }

  disconnect(): void {
    this.stopPolling();
  }

  private startPolling(sessionId: string, handlers: CursorAgentEventHandlers): void {
    let consecutiveFailures = 0;
    const MAX_FAILURES = 10;
    this.pollTimer = setInterval(async () => {
      try {
        const url = `${endpoints.BASE_URL}/webapi/CursorAgent/PollEvents?sessionId=${sessionId || ''}`;
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
          handlers.onError('Session not found on server. The server may have restarted — try Resume from history.');
          return;
        }
        for (const evt of (data?.Events ?? [])) {
          if (evt.EventType === 'step' && evt.Step) handlers.onStep(evt.Step);
          if (evt.EventType === 'token' && evt.Token) handlers.onToken(evt.Token);
          if (evt.EventType === 'file' && evt.File) handlers.onFile?.(evt.File);
          if (evt.EventType === 'gate' && evt.Gate) handlers.onGate(evt.Gate);
          if (evt.EventType === 'done') {
            this.stopPolling();
            handlers.onDone(evt.Done ?? { FinalResponse: '', UpdatedHistory: [], WorkspaceFiles: [] });
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

export const cursorAgentService = new CursorAgentService();

export async function getRecentCursorSessions(limit = 30): Promise<CursorAgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/RecentSessions?limit=${limit || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function getCursorSession(sessionId: string): Promise<any> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/GetSession?sessionId=${sessionId || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return null;
  const data = await resp.json();
  return data?.Object ?? null;
}

export async function listCursorWorkspaceFiles(sessionId: string): Promise<CursorAgentWorkspaceFile[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ListWorkspaceFiles?sessionId=${sessionId || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function readCursorWorkspaceFile(sessionId: string, relativePath: string): Promise<string> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ReadWorkspaceFile?sessionId=${sessionId || ''}&relativePath=${encodeURIComponent(relativePath || '')}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) throw new Error('Failed to read workspace file');
  const data = await resp.json();
  return data?.Object?.Content ?? '';
}
