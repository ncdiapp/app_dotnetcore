import { getHeaders } from '../helper/apiServiceHelper';
import { endpoints } from './endpoints';

export interface CursorCloudAgentMessage {
  Role?: string;
  Content?: string;
  Timestamp?: string;
  StartedAt?: string;
  DurationSeconds?: number;
  WrittenPackPaths?: string[];
  role?: string;
  content?: string;
  timestamp?: string;
  startedAt?: string;
  durationSeconds?: number;
  writtenPackPaths?: string[];
}

export interface CursorCloudAgentStepEvent {
  Type: string;
  ToolName?: string;
  Description: string;
  Details?: string;
  IsSuccess: boolean;
  Timestamp: string;
}

export interface CursorCloudAgentGateEvent {
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

export interface CursorCloudAgentFileEvent {
  Action: string;
  RelativePath: string;
}

export interface CursorCloudAgentNavigateEvent {
  RouteCode?: string;
  Label?: string;
  Link?: string;
  ParamObj?: Record<string, unknown>;
  routeCode?: string;
  label?: string;
  link?: string;
  paramObj?: Record<string, unknown>;
}

export interface CursorCloudAgentTablePreviewItem {
  TableName?: string;
  DataSourceId?: number | null;
  SchemaOwner?: string | null;
  tableName?: string;
  dataSourceId?: number | null;
  schemaOwner?: string | null;
}

export interface CursorCloudAgentTablePreviewEvent {
  Tables?: CursorCloudAgentTablePreviewItem[];
  tables?: CursorCloudAgentTablePreviewItem[];
}

export interface CursorCloudAgentDoneEvent {
  FinalResponse: string;
  UpdatedHistory: CursorCloudAgentMessage[];
  WorkspaceFiles: string[];
  OpenUiOffers?: any[];
  openUiOffers?: any[];
  IsIncomplete?: boolean;
  isIncomplete?: boolean;
}

export interface CursorCloudAgentFileContent {
  Content?: string;
  content?: string;
  Truncated?: boolean;
  truncated?: boolean;
  RelativePath?: string;
  relativePath?: string;
}

export interface CursorCloudAgentEventHandlers {
  onStep: (step: CursorCloudAgentStepEvent) => void;
  onToken: (text: string) => void;
  onFile?: (file: CursorCloudAgentFileEvent) => void;
  onGate: (gate: CursorCloudAgentGateEvent) => void;
  onNavigate?: (nav: CursorCloudAgentNavigateEvent) => void;
  onTablePreview?: (preview: CursorCloudAgentTablePreviewEvent) => void;
  onDone: (result: CursorCloudAgentDoneEvent) => void;
  onError: (message: string) => void;
}

export interface CursorCloudAgentSessionSummary {
  SessionGuid: string;
  CreatedAt: string;
  UpdatedAt: string;
  UserRequest: string;
  DisplayTitle?: string;
  Status: string;
  CloudAgentId?: string;
  SaasApplicationId?: number;
  DataSourceRegisterId?: number;
  SkillKey?: string;
  WorkspaceRelativePath?: string;
  FinalResponse?: string;
  IsArchived?: boolean;
  SortOrder?: number;
}

export function cursorCloudAgentChatTitle(item?: { DisplayTitle?: string; UserRequest?: string } | null): string {
  const text = (item?.DisplayTitle || item?.UserRequest || '').trim();
  return text || 'Untitled chat';
}

export interface CursorCloudAgentWorkspaceFile {
  RelativePath: string;
  SizeBytes: number;
  UpdatedAt: string;
  IsDirectory: boolean;
  PublicUrl?: string;
}

class CursorCloudAgentService {
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  currentSessionId: string | null = null;

  async startSession(
    userMessage: string,
    saasApplicationId: number,
    dataSourceRegisterId: number | undefined,
    conversationHistory: CursorCloudAgentMessage[],
    handlers: CursorCloudAgentEventHandlers,
    skillKey?: string
  ): Promise<string> {
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/StartSession`;
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
    handlers: CursorCloudAgentEventHandlers,
    skillKey?: string,
    saasApplicationId?: number,
    dataSourceRegisterId?: number
  ): Promise<void> {
    if (!this.currentSessionId) throw new Error('No active session');
    this.stopPolling();
    const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/FollowUp`;
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

  async resume(sessionId: string, userMessage: string, handlers: CursorCloudAgentEventHandlers): Promise<void> {
    this.stopPolling();
    this.currentSessionId = sessionId;
    const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ResumeSession`;
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
    await fetch(`${endpoints.BASE_URL}/webapi/CursorCloudAgent/ConfirmGate`, {
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
    await fetch(`${endpoints.BASE_URL}/webapi/CursorCloudAgent/Cancel`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId: this.currentSessionId }),
    }).catch(() => {});
  }

  disconnect(): void {
    this.stopPolling();
  }

  private startPolling(sessionId: string, handlers: CursorCloudAgentEventHandlers): void {
    this.stopPolling();
    let consecutiveFailures = 0;
    const MAX_FAILURES = 10;
    let inFlight = false;
    const tick = async () => {
      if (inFlight) return;
      inFlight = true;
      try {
        const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/PollEvents?sessionId=${sessionId || ''}`;
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

export const cursorCloudAgentService = new CursorCloudAgentService();

export interface CursorCloudAgentSkillMenuItem {
  Key: string;
  Label: string;
  Group: string;
  GroupLabel: string;
}

export interface CursorCloudAgentSkillMenu {
  DefaultKey: string;
  Items: CursorCloudAgentSkillMenuItem[];
}

export async function listCursorCloudAgentSkillMenu(): Promise<CursorCloudAgentSkillMenu> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ListSkillMenu`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return { DefaultKey: 'app-config-builder', Items: [] };
  const data = await resp.json();
  return data?.Object ?? { DefaultKey: 'app-config-builder', Items: [] };
}

export async function getRecentCursorCloudAgentSessions(limit = 30): Promise<CursorCloudAgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/RecentSessions?limit=${limit || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function listAllCursorCloudAgentSessions(): Promise<CursorCloudAgentSessionSummary[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ListAllSessions`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function renameCursorCloudAgentSession(sessionId: string, title: string): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/RenameSession`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionId, title }),
  });
  if (!response.ok) throw new Error('Failed to rename chat');
}

export async function archiveCursorCloudAgentSessions(sessionIds: string[], archived: boolean): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ArchiveSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds, archived }),
  });
  if (!response.ok) throw new Error('Failed to archive chats');
}

export async function deleteCursorCloudAgentSessions(sessionIds: string[]): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/DeleteSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds }),
  });
  if (!response.ok) throw new Error('Failed to delete chats');
}

export async function reorderCursorCloudAgentSessions(sessionIds: string[]): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ReorderSessions`;
  const response = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionIds }),
  });
  if (!response.ok) throw new Error('Failed to reorder chats');
}

export async function getCursorCloudAgentSession(sessionId: string): Promise<any> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/GetSession?sessionId=${sessionId || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return null;
  const data = await resp.json();
  return data?.Object ?? null;
}

export async function listCursorCloudAgentWorkspaceFiles(sessionId: string): Promise<CursorCloudAgentWorkspaceFile[]> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ListWorkspaceFiles?sessionId=${sessionId || ''}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) return [];
  const data = await resp.json();
  return data?.Object ?? [];
}

export async function readCursorCloudAgentWorkspaceFile(
  sessionId: string,
  relativePath: string
): Promise<{ content: string; truncated: boolean }> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/ReadWorkspaceFile?sessionId=${sessionId || ''}&relativePath=${encodeURIComponent(relativePath || '')}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) throw new Error('Failed to read workspace file');
  const data = await resp.json();
  const obj = (data?.Object ?? {}) as CursorCloudAgentFileContent;
  return {
    content: obj.Content ?? obj.content ?? '',
    truncated: !!(obj.Truncated ?? obj.truncated),
  };
}

function parseFileNameFromContentDisposition(header: string | null): string | null {
  if (!header) return null;
  const utf8 = /filename\*=(?:UTF-8'')?([^;]+)/i.exec(header);
  if (utf8?.[1]) {
    try { return decodeURIComponent(utf8[1].replace(/"/g, '').trim()); } catch { /* ignore */ }
  }
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain?.[1]?.trim() ?? null;
}

export async function downloadCursorCloudAgentWorkspaceFile(
  sessionId: string,
  relativePath: string
): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/DownloadWorkspaceFile?sessionId=${sessionId || ''}&relativePath=${encodeURIComponent(relativePath || '')}`;
  const resp = await fetch(url, { headers: getHeaders() });
  if (!resp.ok) {
    const errText = await resp.text().catch(() => '');
    throw new Error(errText || `Download failed (${resp.status})`);
  }
  const blob = await resp.blob();
  const fallbackName = (relativePath || '').split(/[/\\]/).pop() || 'workspace-file';
  const name =
    parseFileNameFromContentDisposition(resp.headers.get('Content-Disposition')) ||
    fallbackName;
  const objectUrl = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = objectUrl;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
}

export async function deleteCursorCloudAgentWorkspaceFile(
  sessionId: string,
  relativePath: string
): Promise<void> {
  const url = `${endpoints.BASE_URL}/webapi/CursorCloudAgent/DeleteWorkspaceFile`;
  const resp = await fetch(url, {
    method: 'POST',
    headers: getHeaders(),
    body: JSON.stringify({ sessionId, relativePath }),
  });
  if (!resp.ok) throw new Error('Failed to delete workspace file');
  const data = await resp.json();
  const err = data?.ValidationResult?.Items?.find((i: any) => i.Type === 'Error' || i.ItemType === 1);
  if (err?.Message) throw new Error(err.Message);
}
