import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';

export interface CursorAgentMessage {
  Role?: string;
  Content?: string;
  Timestamp?: string;
  WrittenPackPaths?: string[];
  role?: string;
  content?: string;
  timestamp?: string;
  writtenPackPaths?: string[];
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

export interface CursorAgentNavigateEvent {
  RouteCode?: string;
  Label?: string;
  Link?: string;
  ParamObj?: Record<string, unknown>;
  routeCode?: string;
  label?: string;
  link?: string;
  paramObj?: Record<string, unknown>;
}

export interface CursorAgentTablePreviewItem {
  TableName?: string;
  DataSourceId?: number | null;
  SchemaOwner?: string | null;
  tableName?: string;
  dataSourceId?: number | null;
  schemaOwner?: string | null;
}

export interface CursorAgentTablePreviewEvent {
  Tables?: CursorAgentTablePreviewItem[];
  tables?: CursorAgentTablePreviewItem[];
}

export interface CursorAgentDoneEvent {
  FinalResponse: string;
  UpdatedHistory: CursorAgentMessage[];
  WorkspaceFiles: string[];
  OpenUiOffers?: any[];
  openUiOffers?: any[];
}

export interface CursorAgentEventHandlers {
  onStep: (step: CursorAgentStepEvent) => void;
  onToken: (text: string) => void;
  onFile?: (file: CursorAgentFileEvent) => void;
  onGate: (gate: CursorAgentGateEvent) => void;
  onNavigate?: (nav: CursorAgentNavigateEvent) => void;
  onTablePreview?: (preview: CursorAgentTablePreviewEvent) => void;
  onDone: (result: CursorAgentDoneEvent) => void;
  onError: (message: string) => void;
}

export interface CursorAgentSessionSummary {
  SessionGuid: string;
  CreatedAt: string;
  UpdatedAt: string;
  UserRequest: string;
  DisplayTitle?: string;
  Status: string;
  CursorAgentId?: string;
  SaasApplicationId?: number;
  DataSourceRegisterId?: number;
  SkillKey?: string;
  WorkspaceRelativePath?: string;
  FinalResponse?: string;
  IsArchived?: boolean;
  SortOrder?: number;
}

export function cursorChatTitle(item?: { DisplayTitle?: string; UserRequest?: string } | null): string {
  const text = (item?.DisplayTitle || item?.UserRequest || '').trim();
  return text || 'Untitled chat';
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
    handlers: CursorAgentEventHandlers,
    skillKey?: string
  ): Promise<string> {
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorAgent/StartSession`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ userMessage, saasApplicationId, dataSourceRegisterId, conversationHistory, skillKey }),
    });
    if (!response.ok) throw new Error(`Failed to start App Data Integration Agent: ${response.status}`);
    const result = await response.json();
    const err = result?.ValidationResult?.Items?.find((i: any) => i.Type === 'Error' || i.ItemType === 1);
    if (err?.Message) throw new Error(err.Message);
    const sessionId: string = result?.Object?.SessionId;
    if (!sessionId) throw new Error('No session ID returned from server');
    this.currentSessionId = sessionId;
    this.startPolling(sessionId, handlers);
    return sessionId;
  }

  async followUp(
    userMessage: string,
    handlers: CursorAgentEventHandlers,
    skillKey?: string,
    saasApplicationId?: number,
    dataSourceRegisterId?: number
  ): Promise<void> {
    if (!this.currentSessionId) throw new Error('No active session');
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorAgent/FollowUp`;
    const response = await fetch(url, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId: this.currentSessionId, userMessage, skillKey, saasApplicationId, dataSourceRegisterId }),
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
    this.stopPolling();
    let consecutiveFailures = 0;
    const MAX_FAILURES = 10;
    let inFlight = false;
    const tick = async () => {
      if (inFlight) return;
      inFlight = true;
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
        for (const evt of (data?.Events ?? data?.events ?? [])) {
          const eventType = evt.EventType ?? evt.eventType;
          const token = evt.Token ?? evt.token;
          const step = evt.Step ?? evt.step;
          const file = evt.File ?? evt.file;
          const gate = evt.Gate ?? evt.gate;
          const navigate = evt.Navigate ?? evt.navigate;
          const tablePreview = evt.TablePreview ?? evt.tablePreview;
          const done = evt.Done ?? evt.done;
          const error = evt.Error ?? evt.error;
          // Process open offers before done so they attach to the assistant turn.
          if (eventType === 'step' && step) handlers.onStep(step);
          if (eventType === 'token' && token) handlers.onToken(token);
          if (eventType === 'file' && file) handlers.onFile?.(file);
          if (eventType === 'gate' && gate) handlers.onGate(gate);
          if (eventType === 'navigate' && navigate) handlers.onNavigate?.(navigate);
          if ((eventType === 'table_preview' || eventType === 'tablePreview') && tablePreview) {
            handlers.onTablePreview?.(tablePreview);
          }
          if (eventType === 'error') {
            this.stopPolling();
            handlers.onError(error ?? 'Unknown error');
            return;
          }
          if (eventType === 'done') {
            // Keep scanning the same batch for any late navigate/table_preview after done.
            continue;
          }
        }
        const doneEvt = (data?.Events ?? data?.events ?? []).find((e: any) => {
          const t = e.EventType ?? e.eventType;
          return t === 'done';
        });
        if (doneEvt) {
          this.stopPolling();
          const done = doneEvt.Done ?? doneEvt.done;
          const final = done?.FinalResponse ?? done?.finalResponse ?? '';
          handlers.onDone(done ?? { FinalResponse: final, UpdatedHistory: [], WorkspaceFiles: [] });
        }
      } catch {
        consecutiveFailures++;
        if (consecutiveFailures >= MAX_FAILURES) {
          this.stopPolling();
          handlers.onError('Lost connection to server after multiple retries.');
        }
      } finally {
        inFlight = false;
      }
    };
    this.pollTimer = setInterval(tick, 500);
    void tick();
  }

  private stopPolling(): void {
    if (this.pollTimer !== null) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }
}

export const cursorAgentService = new CursorAgentService();

export interface CursorAgentSkillMenuItem {
  Key: string;
  Label: string;
  Group: string;
  GroupLabel: string;
}

export interface CursorAgentSkillMenu {
  DefaultKey: string;
  Items: CursorAgentSkillMenuItem[];
}

export async function listCursorSkillMenu(): Promise<CursorAgentSkillMenu> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ListSkillMenu`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return { DefaultKey: 'app-config-builder', Items: [] };
  const data = await resp.json();
  return data?.Object ?? { DefaultKey: 'app-config-builder', Items: [] };
}

export async function getRecentCursorSessions(limit = 30): Promise<CursorAgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/RecentSessions?limit=${limit || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function listAllCursorSessions(): Promise<CursorAgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ListAllSessions`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function renameCursorSession(sessionId: string, title: string): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/RenameSession`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionId, title }),
  });
  if (!response.ok) throw new Error('Failed to rename chat');
}

export async function archiveCursorSessions(sessionIds: string[], archived: boolean): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ArchiveSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds, archived }),
  });
  if (!response.ok) throw new Error('Failed to archive chats');
}

export async function deleteCursorSessions(sessionIds: string[]): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/DeleteSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds }),
  });
  if (!response.ok) throw new Error('Failed to delete chats');
}

export async function reorderCursorSessions(sessionIds: string[]): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorAgent/ReorderSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds }),
  });
  if (!response.ok) throw new Error('Failed to reorder chats');
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
