import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { AgentCheckpoint, AgentCreatedTransaction, AgentMessage, AgentPlanEvent, AgentSchemaEvent, AgentSessionSummary, AgentStepEvent, appBuilderAgentService, deleteAgentSession, getAgentSession, getRecentAgentSessions } from '../../webapi/appbuilderagentsvc';
import { adminSvc } from '../../webapi/adminsvc';
import appHelper from '../../helper/appHelper';
import { useVoiceInput } from '../../hooks/useVoiceInput';

// ─────────────────────────────────────────────────────────────────────────────
// Local types
// ─────────────────────────────────────────────────────────────────────────────

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  steps: AgentStepEvent[];
  streamingContent: string;
  isStreaming: boolean;
  createdTransactions: AgentCreatedTransaction[];
  checkpoint?: AgentCheckpoint;
}

// ── Active session persistence (survives tab switches) ────────────────────────
const ACTIVE_SESSION_KEY = 'appbuilder_active_session';

interface ActiveSession {
  sessionId: string;
  messages: ChatMessage[];
  conversationHistory: AgentMessage[];
  activeCheckpoint: AgentCheckpoint | null;
  dataSourceId?: number;
}

function loadActiveSession(): ActiveSession | null {
  try {
    const raw = localStorage.getItem(ACTIVE_SESSION_KEY);
    if (!raw) return null;
    const s: ActiveSession = JSON.parse(raw);
    // Any message that was still streaming when the tab closed is no longer streaming
    s.messages = s.messages.map(m => m.isStreaming ? { ...m, isStreaming: false } : m);
    return s;
  } catch {
    return null;
  }
}

function saveActiveSession(s: ActiveSession) {
  try {
    // Only persist completed messages to keep storage small
    const toSave: ActiveSession = {
      ...s,
      messages: s.messages.filter(m => !m.isStreaming),
    };
    localStorage.setItem(ACTIVE_SESSION_KEY, JSON.stringify(toSave));
  } catch {}
}

function clearActiveSession() {
  try { localStorage.removeItem(ACTIVE_SESSION_KEY); } catch {}
}

function formatRelativeTime(iso: string): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(iso).toLocaleDateString();
}

// ─────────────────────────────────────────────────────────────────────────────
// Icons (Font Awesome 6 via fa-solid)
// ─────────────────────────────────────────────────────────────────────────────

const ToolIcon: Record<string, string> = {
  thinking:    'fa-solid fa-brain',
  tool_call:   'fa-solid fa-gear',
  tool_result: 'fa-solid fa-circle-check',
  error:       'fa-solid fa-circle-exclamation',
};

// ─────────────────────────────────────────────────────────────────────────────
// Sub-components
// ─────────────────────────────────────────────────────────────────────────────

const StepRow: React.FC<{ step: AgentStepEvent }> = ({ step }) => {
  const { theme } = useTheme();
  const [expanded, setExpanded] = useState(false);
  const icon = ToolIcon[step.Type] ?? 'fa-solid fa-circle-dot';
  const isThinking = step.Type === 'thinking';
  const isToolCall = step.Type === 'tool_call';
  const isToolResult = step.Type === 'tool_result';
  const iconColor = isThinking
    ? 'text-blue-400 animate-pulse'
    : step.IsSuccess ? 'text-green-500' : 'text-red-500';

  return (
    <div className="flex items-start gap-2 py-1 text-xs">
      <i className={`${icon} ${iconColor} mt-0.5 w-3.5 shrink-0`} />
      <div className="min-w-0 w-1 flex-auto">
        {/* Badge + description in one flex row — badge is nowrap so it never splits */}
        <div
          className={`flex items-baseline gap-1.5 flex-wrap cursor-default ${step.Details ? 'cursor-pointer' : ''}`}
          onClick={() => step.Details && setExpanded(e => !e)}
        >
          {isThinking && (
            <span className="whitespace-nowrap font-semibold text-blue-400">Thinking</span>
          )}
          {isToolCall && (
            <span className="whitespace-nowrap font-semibold text-yellow-500 font-mono">
              {step.ToolName}
            </span>
          )}
          {isToolResult && (
            <span className={`whitespace-nowrap font-semibold ${step.IsSuccess ? 'text-green-500' : 'text-red-500'}`}>
              {step.IsSuccess ? 'Done' : 'Failed'}
            </span>
          )}
          {step.Description && (
            <span className={`${theme.label} leading-tight`}>{step.Description}</span>
          )}
          {step.Details && (
            <i className={`fa-solid ${expanded ? 'fa-chevron-up' : 'fa-chevron-down'} text-[9px] opacity-50 ml-auto`} />
          )}
        </div>
        {expanded && step.Details && (
          <pre className={`mt-1.5 p-2 rounded text-[10px] overflow-auto max-h-48 ${theme.inputBox} whitespace-pre-wrap`}>
            {step.Details}
          </pre>
        )}
      </div>
    </div>
  );
};

const TransactionLink: React.FC<{ tx: AgentCreatedTransaction; onOpen: (tx: AgentCreatedTransaction) => void }> = ({ tx, onOpen }) => {
  const { theme } = useTheme();
  return (
    <button
      onClick={() => tx.TransactionId > 0 && onOpen(tx)}
      disabled={tx.TransactionId <= 0}
      className={`inline-flex items-center gap-1.5 px-2 py-1 text-xs rounded-[4px] mr-1.5 mb-1 ${theme.button_default}`}
    >
      <i className="fa-solid fa-table-list" />
      {tx.Name || tx.TableName}
      {tx.TransactionId > 0 && <i className="fa-solid fa-arrow-up-right-from-square text-[9px]" />}
    </button>
  );
};

const CheckpointBadge: React.FC<{
  cp: AgentCheckpoint;
  onResume: (cp: AgentCheckpoint) => void;
}> = ({ cp, onResume }) => {
  const { theme } = useTheme();
  const rows: string[] = [];
  if (cp.ApplicationName)        rows.push(`App: "${cp.ApplicationName}" (ID ${cp.SaasApplicationId})`);
  if (cp.TablesCreated?.length)  rows.push(`Tables: ${cp.TablesCreated.join(', ')}`);
  if (cp.TransactionsCreated?.length) rows.push(`Transactions: ${cp.TransactionsCreated.map(t => `${t.Name} [${t.TransactionId}]`).join(', ')}`);
  if (cp.EntitiesCreated?.length) rows.push(`Entities: ${cp.EntitiesCreated.join(', ')}`);

  return (
    <div className={`mt-2 pt-2 border-t border-dashed`}>
      <div className="flex items-center gap-2 mb-1.5">
        <i className="fa-solid fa-flag-checkered text-green-500 text-xs" />
        <span className={`text-[10px] font-semibold text-green-600 dark:text-green-400`}>Checkpoint saved</span>
        <span className={`text-[10px] ${theme.label} opacity-50`}>
          {new Date(cp.Timestamp).toLocaleTimeString()}
        </span>
      </div>
      <div className={`text-[10px] ${theme.label} space-y-0.5 mb-2`}>
        {rows.map((r, i) => (
          <div key={i} className="flex items-start gap-1">
            <i className="fa-solid fa-check text-green-500 mt-0.5 shrink-0" />
            <span>{r}</span>
          </div>
        ))}
      </div>
      {!cp.IsComplete && (
        <button
          onClick={() => onResume(cp)}
          className={`inline-flex items-center gap-1.5 px-2.5 py-1 text-xs rounded-[4px] bg-green-600 hover:bg-green-700 text-white`}
        >
          <i className="fa-solid fa-rotate-right text-[10px]" />
          Resume from checkpoint
        </button>
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Plan approval card — shown inline while the agent awaits user confirmation
// ─────────────────────────────────────────────────────────────────────────────

const PlanApprovalCard: React.FC<{
  plan:      AgentPlanEvent;
  onApprove: () => void;
  onReject:  () => void;
}> = ({ plan, onApprove, onReject }) => {
  const { theme } = useTheme();
  const isDropConfirm = (plan.TablesToDrop?.length ?? 0) > 0;

  if (isDropConfirm) {
    return (
      <div className="mt-1 rounded-lg border-2 border-red-500 dark:border-red-600 overflow-hidden">
        {/* Header */}
        <div className="flex items-center gap-2 px-3 py-2 bg-red-50 dark:bg-red-900/30 border-b border-red-300 dark:border-red-700">
          <i className="fa-solid fa-triangle-exclamation text-red-500" />
          <span className="text-xs font-semibold text-red-700 dark:text-red-300">
            Drop Database Tables — Irreversible Action
          </span>
        </div>

        {/* Body */}
        <div className={`px-3 py-2.5 space-y-2 ${theme.mainContentSection}`}>
          <p className={`text-xs leading-relaxed ${theme.label}`}>{plan.PlanSummary}</p>
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-trash mr-1 text-red-400" />
              Tables to DROP ({plan.TablesToDrop.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.TablesToDrop.map((t, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-300 font-mono">
                  {t}
                </span>
              ))}
            </div>
          </div>
          <p className="text-[10px] text-red-600 dark:text-red-400 font-semibold">
            ⚠ All data in these tables will be permanently deleted and cannot be recovered.
          </p>
        </div>

        {/* Buttons */}
        <div className="flex items-center gap-2 px-3 py-2 bg-red-50 dark:bg-red-900/20 border-t border-red-300 dark:border-red-700">
          <button
            onClick={onApprove}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-600 hover:bg-red-700 text-white font-semibold"
          >
            <i className="fa-solid fa-trash" />
            Yes — Drop the tables
          </button>
          <button
            onClick={onReject}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-gray-500 hover:bg-gray-600 text-white"
          >
            <i className="fa-solid fa-circle-xmark" />
            No — Keep the tables
          </button>
          <span className={`text-[10px] ${theme.label} opacity-50 ml-auto`}>
            Agent is waiting for your decision…
          </span>
        </div>
      </div>
    );
  }

  return (
    <div className={`mt-1 rounded-lg border-2 border-amber-400 dark:border-amber-500 overflow-hidden`}>
      {/* Header */}
      <div className="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-900/30 border-b border-amber-300 dark:border-amber-600">
        <i className="fa-solid fa-clipboard-list text-amber-500" />
        <span className="text-xs font-semibold text-amber-700 dark:text-amber-300">
          Review Build Plan — Approval Required
        </span>
      </div>

      {/* Plan summary */}
      <div className={`px-3 py-2.5 space-y-2 ${theme.mainContentSection}`}>
        <p className={`text-xs leading-relaxed ${theme.label}`}>{plan.PlanSummary}</p>

        {/* Tables */}
        {plan.TablesToCreate?.length > 0 && (
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-table mr-1 text-blue-400" />
              Tables to create ({plan.TablesToCreate.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.TablesToCreate.map((t, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 font-mono">
                  {t}
                </span>
              ))}
            </div>
          </div>
        )}

        {/* Screens */}
        {plan.ScreensToCreate?.length > 0 && (
          <div>
            <div className={`text-[10px] font-semibold uppercase tracking-wide mb-1 ${theme.title}`}>
              <i className="fa-solid fa-window-restore mr-1 text-purple-400" />
              Screens to generate ({plan.ScreensToCreate.length})
            </div>
            <div className="flex flex-wrap gap-1">
              {plan.ScreensToCreate.map((s, i) => (
                <span key={i} className="px-1.5 py-0.5 rounded text-[10px] bg-purple-100 dark:bg-purple-900/40 text-purple-700 dark:text-purple-300">
                  {s}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Action buttons */}
      <div className="flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-900/20 border-t border-amber-300 dark:border-amber-700">
        <button
          onClick={onApprove}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-green-600 hover:bg-green-700 text-white font-semibold"
        >
          <i className="fa-solid fa-circle-check" />
          Approve — Build it
        </button>
        <button
          onClick={onReject}
          className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-500 hover:bg-red-600 text-white"
        >
          <i className="fa-solid fa-circle-xmark" />
          Reject — Change the plan
        </button>
        <span className={`text-[10px] ${theme.label} opacity-50 ml-auto`}>
          Agent is waiting for your decision…
        </span>
      </div>
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Schema confirm card — inline schema review before DDL
// ─────────────────────────────────────────────────────────────────────────────

const SchemaConfirmCard: React.FC<{
  schema:    AgentSchemaEvent;
  onApprove: (schemaJson?: string) => void;
  onReject:  (feedback: string) => void;
}> = ({ schema, onApprove, onReject }) => {
  const { theme } = useTheme();
  const [deletedTables,  setDeletedTables]  = useState<Set<string>>(new Set());
  const [expandedTables, setExpandedTables] = useState<Set<string>>(
    () => new Set(schema.Tables.map(t => t.Name))
  );
  const [rejecting,  setRejecting]  = useState(false);
  const [feedback,   setFeedback]   = useState('');
  const [showScript, setShowScript] = useState(false);

  const activeTables = schema.Tables.filter(t => !deletedTables.has(t.Name));

  const handleApprove = () => {
    if (deletedTables.size === 0) {
      onApprove(schema.SchemaJson);
    } else {
      try {
        const parsed = JSON.parse(schema.SchemaJson);
        const filteredTables = (parsed.Tables ?? []).filter((t: any) => !deletedTables.has(t.Name));
        filteredTables.forEach((t: any) => {
          t.Relationships = (t.Relationships ?? []).filter(
            (r: any) => !deletedTables.has(r.TargetTable ?? '')
          );
        });
        onApprove(JSON.stringify({ ...parsed, Tables: filteredTables }));
      } catch {
        onApprove(schema.SchemaJson);
      }
    }
  };

  const toggleTable = (name: string) =>
    setDeletedTables(prev => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name); else next.add(name);
      return next;
    });

  const toggleExpand = (name: string) =>
    setExpandedTables(prev => {
      const next = new Set(prev);
      if (next.has(name)) next.delete(name); else next.add(name);
      return next;
    });

  return (
    <div className="mt-1 rounded-lg border-2 border-blue-400 dark:border-blue-500 overflow-hidden">
      {/* Header */}
      <div className="flex items-center gap-2 px-3 py-2 bg-blue-50 dark:bg-blue-900/30 border-b border-blue-300 dark:border-blue-600">
        <i className="fa-solid fa-table-columns text-blue-500" />
        <span className="text-xs font-semibold text-blue-700 dark:text-blue-300">
          Schema Review — Approve Before Building
        </span>
        <span className="text-[10px] text-blue-500 dark:text-blue-400 ml-auto">
          {schema.Tables.length} table{schema.Tables.length !== 1 ? 's' : ''}
        </span>
      </div>

      {/* Body */}
      <div className={`px-3 pt-2.5 pb-2 space-y-2 ${theme.mainContentSection}`}>
        <p className={`text-xs leading-relaxed ${theme.label}`}>{schema.Summary}</p>

        {/* Table list */}
        {schema.Tables.map(table => {
          const isDeleted  = deletedTables.has(table.Name);
          const isExpanded = expandedTables.has(table.Name) && !isDeleted;
          const borderCls  = isDeleted
            ? 'border-gray-200 dark:border-gray-700 opacity-40'
            : table.IsLookup
              ? 'border-purple-200 dark:border-purple-700'
              : 'border-blue-200 dark:border-blue-700';
          const headerBg   = isDeleted
            ? 'bg-gray-50 dark:bg-gray-800/20'
            : table.IsLookup
              ? 'bg-purple-50 dark:bg-purple-900/20'
              : 'bg-blue-50 dark:bg-blue-900/20';

          return (
            <div key={table.Name} className={`rounded border ${borderCls}`}>
              {/* Table header */}
              <div
                className={`flex items-center gap-1.5 px-2 py-1.5 cursor-pointer select-none ${headerBg}`}
                onClick={() => !isDeleted && toggleExpand(table.Name)}
              >
                <i className={`fa-solid ${isExpanded ? 'fa-chevron-down' : 'fa-chevron-right'} text-[9px] ${theme.label} shrink-0`} />
                <i className={`fa-solid fa-table text-[10px] shrink-0 ${table.IsLookup ? 'text-purple-500' : 'text-blue-500'}`} />
                <span className={`text-xs font-semibold font-mono ${isDeleted ? 'line-through text-gray-400' : theme.title}`}>
                  {table.Name}
                </span>
                {table.IsLookup && !isDeleted && (
                  <span className="px-1 py-0.5 rounded text-[9px] bg-purple-100 dark:bg-purple-900/40 text-purple-600 dark:text-purple-300">
                    Lookup
                  </span>
                )}
                {table.ParentTable && !isDeleted && (
                  <span className={`text-[10px] ${theme.label} opacity-60`}>
                    <i className="fa-solid fa-arrow-left text-[8px] mr-0.5" />{table.ParentTable}
                  </span>
                )}
                <span className={`ml-auto text-[10px] ${theme.label} opacity-50 mr-1`}>
                  {table.Columns.length} col{table.Columns.length !== 1 ? 's' : ''}
                </span>
                <button
                  onClick={e => { e.stopPropagation(); toggleTable(table.Name); }}
                  title={isDeleted ? 'Restore table' : 'Remove table'}
                  className={`w-5 h-5 flex items-center justify-center rounded transition-colors ${
                    isDeleted ? 'text-green-500 hover:text-green-700' : 'text-red-400 hover:text-red-600'
                  }`}
                >
                  <i className={`fa-solid ${isDeleted ? 'fa-rotate-left' : 'fa-trash'} text-[10px]`} />
                </button>
              </div>

              {/* Columns table */}
              {isExpanded && (
                <table className="w-full text-[10px]">
                  <thead>
                    <tr className={`border-b border-gray-100 dark:border-gray-700 ${theme.label} opacity-60`}>
                      <th className="text-left px-2 py-1 font-medium">Column</th>
                      <th className="text-left px-2 py-1 font-medium">Type</th>
                      <th className="text-center px-1 py-1 font-medium w-8">PK</th>
                      <th className="text-center px-1 py-1 font-medium w-8">Null</th>
                      <th className="text-left px-2 py-1 font-medium">FK →</th>
                    </tr>
                  </thead>
                  <tbody>
                    {table.Columns.map((col, ci) => (
                      <tr key={ci} className="border-b last:border-0 border-gray-100 dark:border-gray-700">
                        <td className={`px-2 py-1 font-mono ${col.IsPrimaryKey ? 'text-amber-600 dark:text-amber-400' : theme.title}`}>
                          {col.IsPrimaryKey && <i className="fa-solid fa-key text-amber-400 mr-1 text-[8px]" />}
                          {col.IsAutoIncrement && <i className="fa-solid fa-bolt text-blue-400 mr-1 text-[8px]" title="Auto-increment" />}
                          {col.Name}
                        </td>
                        <td className={`px-2 py-1 font-mono ${theme.label} opacity-80`}>
                          {col.DataType}{col.Length ? `(${col.Length})` : ''}
                        </td>
                        <td className="px-1 py-1 text-center">
                          {col.IsPrimaryKey && <i className="fa-solid fa-check text-amber-500 text-[9px]" />}
                        </td>
                        <td className="px-1 py-1 text-center">
                          {col.IsNullable && <i className="fa-solid fa-check text-gray-400 text-[9px]" />}
                        </td>
                        <td className={`px-2 py-1 ${theme.label} opacity-70 font-mono`}>
                          {col.FKTargetTable && (
                            <span className="flex items-center gap-0.5">
                              <i className="fa-solid fa-arrow-right text-[8px] text-purple-400" />
                              {col.FKTargetTable}
                            </span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          );
        })}

        {/* CREATE TABLE script (collapsible) */}
        {schema.CreateScript && (
          <div>
            <button
              onClick={() => setShowScript(s => !s)}
              className={`flex items-center gap-1.5 text-[10px] ${theme.label} opacity-60 hover:opacity-100`}
            >
              <i className={`fa-solid ${showScript ? 'fa-chevron-up' : 'fa-chevron-down'} text-[9px]`} />
              {showScript ? 'Hide' : 'Show'} CREATE TABLE script
            </button>
            {showScript && (
              <pre className={`mt-1 p-2 rounded text-[10px] overflow-auto max-h-40 ${theme.inputBox} whitespace-pre-wrap font-mono`}>
                {schema.CreateScript}
              </pre>
            )}
          </div>
        )}

        {/* Rejection feedback */}
        {rejecting && (
          <textarea
            value={feedback}
            onChange={e => setFeedback(e.target.value)}
            placeholder="What should change? e.g. 'Add a Price column to Products', 'Remove the Audit table'"
            rows={2}
            autoFocus
            className={`w-full px-2 py-1.5 text-xs border rounded-[4px] resize-none ${theme.inputBox}`}
          />
        )}
      </div>

      {/* Action buttons */}
      <div className={`flex items-center gap-2 px-3 py-2 bg-blue-50 dark:bg-blue-900/20 border-t border-blue-300 dark:border-blue-700`}>
        {!rejecting ? (
          <>
            <button
              onClick={handleApprove}
              disabled={activeTables.length === 0}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-green-600 hover:bg-green-700 text-white font-semibold disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <i className="fa-solid fa-circle-check" />
              Approve — Build it{deletedTables.size > 0 ? ` (${activeTables.length} tables)` : ''}
            </button>
            <button
              onClick={() => setRejecting(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-500 hover:bg-red-600 text-white"
            >
              <i className="fa-solid fa-circle-xmark" />
              Reject — Change schema
            </button>
            <span className={`text-[10px] ${theme.label} opacity-50 ml-auto`}>
              Agent is waiting for your review…
            </span>
          </>
        ) : (
          <>
            <button
              onClick={() => onReject(feedback || 'User rejected the schema.')}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-600 hover:bg-red-700 text-white font-semibold"
            >
              <i className="fa-solid fa-paper-plane" />
              Send feedback
            </button>
            <button
              onClick={() => { setRejecting(false); setFeedback(''); }}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            >
              Cancel
            </button>
          </>
        )}
      </div>
    </div>
  );
};

const UserBubble: React.FC<{
  msg:     ChatMessage;
  onRetry: (content: string) => void;
  onEdit:  (content: string) => void;
}> = ({ msg, onRetry, onEdit }) => {
  const { theme } = useTheme();
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard.writeText(msg.content).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    }).catch(() => {});
  };

  return (
    <div className="flex flex-col items-end gap-1 max-w-[75%]">
      <div className="rounded-lg px-3 py-2 text-xs bg-blue-500 text-white">
        {msg.content}
      </div>
      {/* Action buttons — visible on parent group-hover */}
      <div className={`flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity ${theme.label}`}>
        <button
          onClick={() => onRetry(msg.content)}
          title="Retry"
          className="w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700"
        >
          <i className="fa-solid fa-rotate-right text-[10px]" />
        </button>
        <button
          onClick={() => onEdit(msg.content)}
          title="Edit"
          className="w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700"
        >
          <i className="fa-solid fa-pencil text-[10px]" />
        </button>
        <button
          onClick={handleCopy}
          title={copied ? 'Copied!' : 'Copy'}
          className="w-6 h-6 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-gray-700"
        >
          <i className={`fa-solid ${copied ? 'fa-check text-green-500' : 'fa-copy'} text-[10px]`} />
        </button>
      </div>
    </div>
  );
};

const AssistantBubble: React.FC<{
  msg:              ChatMessage;
  onResume?:        (cp: AgentCheckpoint) => void;
  pendingPlan?:     AgentPlanEvent   | null;
  onApprovePlan?:   () => void;
  onRejectPlan?:    () => void;
  pendingSchema?:   AgentSchemaEvent | null;
  onApproveSchema?: (schemaJson?: string) => void;
  onRejectSchema?:  (feedback: string)    => void;
  onOpenTransaction?: (tx: AgentCreatedTransaction) => void;
}> = ({ msg, onResume, pendingPlan, onApprovePlan, onRejectPlan,
        pendingSchema, onApproveSchema, onRejectSchema, onOpenTransaction }) => {
  const { theme } = useTheme();
  const text = msg.isStreaming ? (msg.streamingContent || '') : msg.content;
  const hasSteps = msg.steps.length > 0;
  const showPlanCard   = msg.isStreaming && !!pendingPlan   && !!onApprovePlan  && !!onRejectPlan;
  const showSchemaCard = msg.isStreaming && !!pendingSchema && !!onApproveSchema && !!onRejectSchema;
  const agentWaiting   = showPlanCard || showSchemaCard;

  return (
    <div className={`rounded-lg p-3 ${theme.mainContentSection} border w-full`}>
      {/* Steps timeline */}
      {hasSteps && (
        <div className={`mb-2 pb-2 border-b border-dashed space-y-0.5`}>
          {msg.steps.map((s, i) => <StepRow key={i} step={s} />)}
          {msg.isStreaming && !agentWaiting && (
            <div className="flex items-center gap-2 mt-1.5 text-xs text-blue-400">
              <i className="fa-solid fa-circle-notch animate-spin" />
              <span>Agent is working…</span>
            </div>
          )}
        </div>
      )}

      {/* Final text */}
      {text && (
        <div className={`text-xs leading-relaxed whitespace-pre-wrap ${theme.label}`}>
          {text}
        </div>
      )}

      {/* Plan approval card */}
      {showPlanCard && (
        <PlanApprovalCard
          plan={pendingPlan!}
          onApprove={onApprovePlan!}
          onReject={onRejectPlan!}
        />
      )}

      {/* Schema review card */}
      {showSchemaCard && (
        <SchemaConfirmCard
          schema={pendingSchema!}
          onApprove={onApproveSchema!}
          onReject={onRejectSchema!}
        />
      )}

      {/* Created transactions */}
      {msg.createdTransactions.length > 0 && (
        <div className="mt-2 pt-2 border-t border-dashed">
          <div className={`text-[10px] font-semibold mb-1.5 ${theme.title}`}>
            <i className="fa-solid fa-layer-group mr-1" /> Created Screens
          </div>
          {msg.createdTransactions.map((tx, i) => <TransactionLink key={i} tx={tx} onOpen={onOpenTransaction ?? (() => {})} />)}
        </div>
      )}

      {/* Checkpoint badge */}
      {msg.checkpoint && onResume && (
        <CheckpointBadge cp={msg.checkpoint} onResume={onResume} />
      )}
    </div>
  );
};

// ─────────────────────────────────────────────────────────────────────────────
// Main component
// ─────────────────────────────────────────────────────────────────────────────

const AppBuilderAgent: React.FC = () => {
  const { theme } = useTheme();
  const { addTabAndNavigate } = useTabNavigation();

  const handleOpenTransaction = useCallback((tx: AgentCreatedTransaction) => {
    addTabAndNavigate('FormMasterDetail', tx.Name || tx.TableName || `Screen ${tx.TransactionId}`, { id: tx.TransactionId });
  }, [addTabAndNavigate]);

  // ── State ─────────────────────────────────────────────────────────────────
  const _savedSession = loadActiveSession();
  const [sessionId, setSessionId]                 = useState<string>(() => _savedSession?.sessionId ?? appHelper.guid());
  const [messages, setMessages]                   = useState<ChatMessage[]>(() => _savedSession?.messages ?? []);
  const [input, setInput]                         = useState('');
  const [isRunning, setIsRunning]                 = useState(false);
  const [dataSourceId, setDataSourceId]           = useState<number | undefined>(_savedSession?.dataSourceId);
  const [dataSources, setDataSources]             = useState<{ id: number; name: string }[]>([]);
  const [conversationHistory, setConversationHistory] = useState<AgentMessage[]>(() => _savedSession?.conversationHistory ?? []);
  const [error, setError]                         = useState<string | null>(null);
  const [activeCheckpoint, setActiveCheckpoint]   = useState<AgentCheckpoint | null>(_savedSession?.activeCheckpoint ?? null);
  const [pendingPlan,   setPendingPlan]   = useState<AgentPlanEvent   | null>(null);
  const [pendingSchema, setPendingSchema] = useState<AgentSchemaEvent | null>(null);

  // Chat history (loaded from DB)
  const [chatHistory, setChatHistory]             = useState<AgentSessionSummary[]>([]);
  const [activeDbGuid, setActiveDbGuid]           = useState<string | null>(null);

  const messagesEndRef  = useRef<HTMLDivElement>(null);
  const textareaRef     = useRef<HTMLTextAreaElement>(null);

  // ── Resizable sidebar ───────────────────────────────────────────────────────
  const [sidebarWidth, setSidebarWidth]   = useState(220);
  const isDraggingRef                     = useRef(false);
  const dragStartXRef                     = useRef(0);
  const dragStartWidthRef                 = useRef(0);

  const handleDragStart = useCallback((e: React.MouseEvent) => {
    isDraggingRef.current    = true;
    dragStartXRef.current    = e.clientX;
    dragStartWidthRef.current = sidebarWidth;
    e.preventDefault();
  }, [sidebarWidth]);

  useEffect(() => {
    const onMouseMove = (e: MouseEvent) => {
      if (!isDraggingRef.current) return;
      const delta    = e.clientX - dragStartXRef.current;
      const newWidth = Math.max(150, Math.min(420, dragStartWidthRef.current + delta));
      setSidebarWidth(newWidth);
    };
    const onMouseUp = () => { isDraggingRef.current = false; };
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup',   onMouseUp);
    return () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup',   onMouseUp);
    };
  }, []);

  // ── Voice input ─────────────────────────────────────────────────────────────
  const voice = useVoiceInput((transcript) => {
    setInput(prev => prev ? `${prev} ${transcript}` : transcript);
    textareaRef.current?.focus();
  });

  // ── Load data sources + chat history from DB ───────────────────────────────
  const refreshChatHistory = useCallback(() => {
    getRecentAgentSessions(30).then(setChatHistory).catch(() => {});
  }, []);

  useEffect(() => {
    adminSvc.getDataSourceRegisterList(true).then((list: any[]) => {
      if (list?.length) {
        setDataSources(list.map((d: any) => ({ id: d.Id, name: d.Name })));
        setDataSourceId(list[0].Id);
      }
    }).catch(() => {});
    refreshChatHistory();
  }, []);

  // ── Auto-scroll ────────────────────────────────────────────────────────────
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // ── Disconnect on unmount ──────────────────────────────────────────────────
  useEffect(() => {
    return () => {
      appBuilderAgentService.disconnect().catch(() => {});
    };
  }, []);

  // ── Persist active session to localStorage so tab switches don't lose state ─
  useEffect(() => {
    if (!messages.length && !conversationHistory.length) return;
    saveActiveSession({ sessionId, messages, conversationHistory, activeCheckpoint, dataSourceId });
  }, [sessionId, messages, conversationHistory, activeCheckpoint, dataSourceId]);


  // ── Helpers to mutate the last assistant message ───────────────────────────
  const updateLastAssistant = useCallback((updater: (msg: ChatMessage) => ChatMessage) => {
    setMessages(prev => {
      const copy = [...prev];
      const last = copy[copy.length - 1];
      if (last?.role === 'assistant') copy[copy.length - 1] = updater(last);
      return copy;
    });
  }, []);

  // ── Core send logic (shared by handleSend and handleRetry) ───────────────
  const sendMessage = useCallback(async (text: string) => {
    if (!text || isRunning) return;

    setError(null);
    setIsRunning(true);
    setPendingPlan(null);
    setPendingSchema(null);

    const userMsg: ChatMessage = {
      role: 'user', content: text, steps: [],
      streamingContent: '', isStreaming: false, createdTransactions: [],
    };
    const assistantMsg: ChatMessage = {
      role: 'assistant', content: '', steps: [],
      streamingContent: '', isStreaming: true, createdTransactions: [],
    };

    setMessages(prev => [...prev, userMsg, assistantMsg]);

    try {
      await appBuilderAgentService.startSession(
        text,
        dataSourceId,
        conversationHistory,
        {
          onStep: (step) => {
            appHelper.debugLog('[AgentStep]', step);
            updateLastAssistant(msg => ({ ...msg, steps: [...msg.steps, step] }));
          },
          onToken: (token) => {
            updateLastAssistant(msg => ({ ...msg, streamingContent: (msg.streamingContent || '') + token }));
          },
          onDone: (result) => {
            appHelper.debugLog('[AgentDone]', result);
            try {
              const cp = result.Checkpoint ?? null;
              setPendingPlan(null);
              setPendingSchema(null);
              updateLastAssistant(msg => ({
                ...msg,
                content: result.FinalResponse || msg.streamingContent || '',
                streamingContent: '',
                isStreaming: false,
                createdTransactions: result.CreatedTransactions || [],
                checkpoint: cp ?? undefined,
              }));
              setActiveCheckpoint(cp);
              setConversationHistory(result.UpdatedHistory || []);
              // Refresh sidebar so the new session appears from DB
              setTimeout(refreshChatHistory, 500);
            } finally {
              setIsRunning(false);
            }
          },
          onError: (message) => {
            try {
              setPendingPlan(null);
              setPendingSchema(null);
              updateLastAssistant(msg => ({
                ...msg,
                content: `Error: ${message}`,
                isStreaming: false,
              }));
            } finally {
              setIsRunning(false);
            }
          },
          onPlan: (plan) => {
            appHelper.debugLog('[AgentPlan]', plan);
            setPendingPlan(plan);
          },
          onSchema: (schema) => {
            appHelper.debugLog('[AgentSchema]', schema);
            setPendingSchema(schema);
          },
        }
      );
    } catch (err: any) {
      const errMsg = err?.message ?? 'Unknown error';
      setError(errMsg);
      updateLastAssistant(msg => ({ ...msg, content: `Failed to start: ${errMsg}`, isStreaming: false }));
      setIsRunning(false);
    }
  }, [isRunning, dataSourceId, conversationHistory, updateLastAssistant]);

  // ── Send message ───────────────────────────────────────────────────────────
  const handleSend = useCallback(async () => {
    const text = input.trim();
    if (!text) return;
    setInput('');
    await sendMessage(text);
  }, [input, sendMessage]);

  // ── Retry: re-send an existing user message ────────────────────────────────
  const handleRetry = useCallback((content: string) => {
    sendMessage(content);
  }, [sendMessage]);

  // ── Edit: load a past message into the input box ───────────────────────────
  const handleEdit = useCallback((content: string) => {
    setInput(content);
    textareaRef.current?.focus();
  }, []);

  // ── Plan approval / rejection ──────────────────────────────────────────────
  const handleApprovePlan = useCallback(() => {
    setPendingPlan(null);
    appBuilderAgentService.confirmPlan(true);
  }, []);

  const handleRejectPlan = useCallback(() => {
    setPendingPlan(null);
    appBuilderAgentService.confirmPlan(false);
  }, []);

  // ── Schema approval / rejection ────────────────────────────────────────────
  const handleApproveSchema = useCallback((schemaJson?: string) => {
    setPendingSchema(null);
    appBuilderAgentService.confirmSchema(true, schemaJson);
  }, []);

  const handleRejectSchema = useCallback((feedback: string) => {
    setPendingSchema(null);
    appBuilderAgentService.confirmSchema(false, undefined, feedback);
  }, []);

  // ── Resume from checkpoint ─────────────────────────────────────────────────
  const handleResumeFromCheckpoint = useCallback((cp: AgentCheckpoint) => {
    const lines: string[] = ['Resume building the application from my last checkpoint.', '', 'Already completed:'];
    if (cp.ApplicationName)
      lines.push(`- App Package: "${cp.ApplicationName}" (SaasApplicationId = ${cp.SaasApplicationId})`);
    if (cp.TablesCreated?.length)
      lines.push(`- Tables in database: ${cp.TablesCreated.join(', ')}`);
    if (cp.TransactionsCreated?.length)
      lines.push(`- Transactions created: ${cp.TransactionsCreated.map(t => `${t.Name} (ID=${t.TransactionId})`).join(', ')}`);
    if (cp.EntitiesCreated?.length)
      lines.push(`- Entity data sources: ${cp.EntitiesCreated.join(', ')}`);

    // If schema was approved but tables haven't been created yet, pass the schema JSON
    // so the agent calls execute_approved_schema directly — NOT propose_schema again.
    if (cp.ApprovedSchemaJson && !cp.TablesCreated?.length) {
      lines.push('', '⚠ Schema was already approved in the previous run. DO NOT call propose_schema or propose_plan again.');
      lines.push(`Call execute_approved_schema immediately with:`);
      lines.push(`- saasApplicationId: ${cp.SaasApplicationId ?? 'null'}`);
      lines.push(`- schemaJson: ${cp.ApprovedSchemaJson}`);
    } else {
      lines.push('', 'Please continue with any remaining steps. If transactions or search views are missing, use the recovery tools to complete the setup.');
    }

    sendMessage(lines.join('\n'));
  }, [sendMessage]);

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // ── New chat ───────────────────────────────────────────────────────────────
  const handleNewChat = useCallback(() => {
    clearActiveSession();
    setSessionId(appHelper.guid());
    setMessages([]);
    setConversationHistory([]);
    setActiveCheckpoint(null);
    setActiveDbGuid(null);
    setError(null);
  }, []);

  // ── Load session from history (fetches full conversation from DB) ──────────
  const handleLoadSession = useCallback(async (summary: AgentSessionSummary) => {
    clearActiveSession();
    setActiveDbGuid(summary.SessionGuid);
    setMessages([]);
    setConversationHistory([]);
    setActiveCheckpoint(null);
    setError(null);

    const session = await getAgentSession(summary.SessionGuid);
    if (!session) return;

    // Reconstruct ChatMessage[] from stored conversation history.
    // Backend serialises as PascalCase (Role/Content); guard both.
    const msgs: ChatMessage[] = (session.ConversationHistory ?? []).map((m: any) => ({
      role: (m.role ?? m.Role ?? 'assistant') as 'user' | 'assistant',
      content: m.content ?? m.Content ?? '',
      steps: [],
      streamingContent: '',
      isStreaming: false,
      createdTransactions: [],
    }));

    // Attach checkpoint to the last assistant message
    if (session.Checkpoint) {
      const lastAss = [...msgs].reverse().find(m => m.role === 'assistant');
      if (lastAss) lastAss.checkpoint = session.Checkpoint;
    }

    setMessages(msgs);
    // Normalise to camelCase so sendMessage can forward these back to the API correctly
    setConversationHistory((session.ConversationHistory ?? []).map((m: any) => ({
      role: (m.role ?? m.Role ?? 'assistant') as 'user' | 'assistant',
      content: m.content ?? m.Content ?? '',
    })));
    const lastCp = [...msgs].reverse().find(m => m.checkpoint)?.checkpoint ?? null;
    setActiveCheckpoint(lastCp);
  }, []);

  // ── Delete session from history ────────────────────────────────────────────
  const handleDeleteSession = useCallback((e: React.MouseEvent, guid: string) => {
    e.stopPropagation();
    setChatHistory(prev => prev.filter(h => h.SessionGuid !== guid));
    if (activeDbGuid === guid) {
      setActiveDbGuid(null);
      setMessages([]);
      setConversationHistory([]);
      setActiveCheckpoint(null);
    }
    deleteAgentSession(guid);
  }, [activeDbGuid]);

  // ── Example prompts ────────────────────────────────────────────────────────
  const EXAMPLES = [
    'Build me a customer order management system with customers, orders, and order items.',
    'Create an inventory management app with products, categories, suppliers, and stock levels.',
    'Build a simple CRM with contacts, companies, and activity log.',
    'Create a helpdesk ticket system with tickets, categories, and comments.',
  ];

  // ─────────────────────────────────────────────────────────────────────────
  // Render
  // ─────────────────────────────────────────────────────────────────────────

  return (
    <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">
      <div className="w-full h-full overflow-hidden flex">

        {/* ── Chat History Panel ── */}
        <div style={{ width: sidebarWidth }} className={`flex-none flex flex-col ${theme.mainContentSection} overflow-hidden rounded-l-md`}>
          {/* Header */}
          <div className="flex items-center justify-between px-3 py-2.5 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-1.5">
              <i className="fa-solid fa-robot text-blue-500 text-sm" />
              <span className={`text-xs font-semibold ${theme.title}`}>Chats</span>
            </div>
            <button
              onClick={handleNewChat}
              disabled={isRunning}
              className={`flex items-center gap-1 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              title="New chat"
            >
              <i className="fa-solid fa-plus text-[10px]" />
              New
            </button>
          </div>

          {/* Session list */}
          <div className="h-1 flex-auto overflow-y-auto py-1">
            {chatHistory.length === 0 ? (
              <div className={`text-center py-8 px-3 text-xs ${theme.label}`}>
                <i className="fa-solid fa-comments text-2xl mb-2 opacity-30 block" />
                No chats yet
              </div>
            ) : (
              chatHistory.map(item => {
                const isActive = item.SessionGuid === activeDbGuid;
                const title = (item.UserRequest ?? '').slice(0, 55);
                return (
                  <div
                    key={item.SessionGuid}
                    onClick={() => handleLoadSession(item)}
                    title={`${title} · ${formatRelativeTime(item.UpdatedAt)}`}
                    className={`
                      group relative cursor-pointer px-3 py-2 mx-1 mb-0.5 rounded-md transition-colors
                      ${isActive
                        ? `${theme.tab_active}`
                        : `hover:bg-gray-100 dark:hover:bg-gray-700/50`
                      }
                    `}
                  >
                    <div className={`text-xs font-medium truncate pr-5 ${theme.title}`}>
                      {title}
                    </div>
                    <div className={`text-[10px] mt-0.5 ${theme.label} flex items-center gap-1`}>
                      <span>{formatRelativeTime(item.UpdatedAt)}</span>
                    </div>
                    {/* Delete button — shown on hover */}
                    <button
                      onClick={(e) => handleDeleteSession(e, item.SessionGuid)}
                      className={`
                        absolute right-2 top-1/2 -translate-y-1/2
                        opacity-0 group-hover:opacity-100 transition-opacity
                        w-5 h-5 flex items-center justify-center rounded
                        hover:text-red-500 ${theme.label}
                      `}
                      title="Delete chat"
                    >
                      <i className="fa-solid fa-trash text-[9px]" />
                    </button>
                  </div>
                );
              })
            )}
          </div>
        </div>

        {/* ── Drag Handle ── */}
        <div
          onMouseDown={handleDragStart}
          className="w-1 flex-none bg-gray-200 dark:bg-gray-700 hover:bg-blue-400 dark:hover:bg-blue-500 cursor-col-resize transition-colors"
          title="Drag to resize"
        />

        {/* ── Main Chat Area ── */}
        <div className={`w-1 flex-auto h-full overflow-hidden flex flex-col rounded-r-md ${theme.mainContentSection}`}>

          {/* Header */}
          <div className={`flex items-center justify-between px-3 py-2 shrink-0 border-b border-gray-200 dark:border-gray-700`}>
            <div className="flex items-center gap-2">
              <div className={`text-md font-semibold ${theme.title}`}>AppBuilder AI Agent</div>
              <span className={`text-xs ${theme.label} opacity-70`}>
                — describe an app in plain language and I'll build it for you
              </span>
            </div>
            <div className="flex items-center gap-2">
              {/* Data source selector */}
              {dataSources.length > 0 && (
                <select
                  value={dataSourceId ?? ''}
                  onChange={e => setDataSourceId(Number(e.target.value))}
                  className={`h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                  disabled={isRunning}
                >
                  {dataSources.map(ds => (
                    <option key={ds.id} value={ds.id}>{ds.name}</option>
                  ))}
                </select>
              )}
            </div>
          </div>

          {/* Error banner */}
          {error && (
            <div className="px-3 py-2 bg-red-50 border-b border-red-200 text-xs text-red-600 shrink-0">
              <i className="fa-solid fa-triangle-exclamation mr-1" />{error}
            </div>
          )}

          {/* Messages area */}
          <div className={`h-1 flex-auto overflow-y-auto px-4 py-3 space-y-4`}>

            {/* Empty state */}
            {messages.length === 0 && (
              <div className="flex flex-col items-center justify-center h-full gap-6">
                <div className="text-center">
                  <i className="fa-solid fa-robot text-blue-300 text-5xl mb-3" />
                  <div className={`text-lg font-semibold ${theme.title}`}>How can I help you build today?</div>
                  <div className={`text-sm mt-1 ${theme.label} opacity-70`}>
                    Describe the application you need and I'll configure the database, data models, and screens automatically.
                  </div>
                </div>
                <div className="grid grid-cols-1 gap-2 w-full max-w-2xl">
                  {EXAMPLES.map((ex, i) => (
                    <button
                      key={i}
                      onClick={() => { setInput(ex); textareaRef.current?.focus(); }}
                      className={`text-left text-xs px-3 py-2 rounded-[4px] border ${theme.button_default} ${theme.inputBox}`}
                    >
                      <i className="fa-solid fa-lightbulb mr-1.5 text-yellow-400" />
                      {ex}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Chat messages */}
            {messages.map((msg, i) => (
              <div key={i} className={`group flex ${msg.role === 'user' ? 'justify-end' : 'justify-start w-full'}`}>
                {msg.role === 'user' ? (
                  <UserBubble msg={msg} onRetry={handleRetry} onEdit={handleEdit} />
                ) : (
                  <AssistantBubble
                    msg={msg}
                    onResume={handleResumeFromCheckpoint}
                    pendingPlan={msg.isStreaming ? pendingPlan : null}
                    onApprovePlan={handleApprovePlan}
                    onRejectPlan={handleRejectPlan}
                    pendingSchema={msg.isStreaming ? pendingSchema : null}
                    onApproveSchema={handleApproveSchema}
                    onRejectSchema={handleRejectSchema}
                    onOpenTransaction={handleOpenTransaction}
                  />
                )}
              </div>
            ))}

            <div ref={messagesEndRef} />
          </div>

          {/* Checkpoint resume banner — only shown when there is incomplete work to resume */}
          {activeCheckpoint && !isRunning && !activeCheckpoint.IsComplete && (
            <div className="shrink-0 px-3 py-1.5 border-t border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/20 flex items-center gap-2">
              <i className="fa-solid fa-flag-checkered text-green-600 dark:text-green-400 text-xs" />
              <span className="text-xs text-green-700 dark:text-green-400 w-1 flex-auto">
                <strong>{activeCheckpoint.ApplicationName ?? 'Checkpoint'}</strong> saved — continue after fixing any issues
              </span>
              <button
                onClick={() => handleResumeFromCheckpoint(activeCheckpoint)}
                className="px-2.5 py-1 text-xs rounded-[4px] bg-green-600 hover:bg-green-700 text-white shrink-0"
              >
                <i className="fa-solid fa-rotate-right mr-1 text-[10px]" />Resume
              </button>
              <button
                onClick={() => setActiveCheckpoint(null)}
                className="w-5 h-5 flex items-center justify-center text-green-600 hover:text-green-800 dark:text-green-400 shrink-0"
                title="Dismiss"
              >
                <i className="fa-solid fa-xmark text-[10px]" />
              </button>
            </div>
          )}

          {/* Input area */}
          <div className={`shrink-0 px-3 py-2 border-t border-gray-200 dark:border-gray-700`}>
            <div className="flex items-end gap-2">
              <textarea
                ref={textareaRef}
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                disabled={isRunning}
                placeholder="Describe the application you want to build… (Enter to send, Shift+Enter for new line)"
                rows={2}
                className={`w-1 flex-auto px-3 py-2 text-xs border rounded-[4px] resize-none ${theme.inputBox}`}
              />
              {voice.supported && (
                <button
                  onClick={voice.toggle}
                  disabled={isRunning}
                  className={`w-8 h-8 flex items-center justify-center rounded-[4px] shrink-0 transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
                    voice.isListening
                      ? 'bg-red-500 hover:bg-red-600 text-white animate-pulse'
                      : theme.button_default
                  }`}
                  title={voice.isListening ? 'Stop listening' : 'Voice input'}
                >
                  <i className={`fa-solid ${voice.isListening ? 'fa-stop' : 'fa-microphone'} text-xs`} />
                </button>
              )}
              <button
                onClick={handleSend}
                disabled={isRunning || !input.trim()}
                className={`px-3 py-1.5 text-sm rounded-[4px] shrink-0 ${theme.button_default} ${isRunning ? 'opacity-50 cursor-not-allowed' : ''}`}
              >
                {isRunning
                  ? <><i className="fa-solid fa-circle-notch animate-spin mr-1" />Building…</>
                  : <><i className="fa-solid fa-paper-plane mr-1" />Send</>}
              </button>
            </div>
            <div className={`text-[10px] mt-1 ${theme.label} opacity-50`}>
              The agent will explore your platform, design the schema, create tables, and set up screens automatically.
            </div>
          </div>

        </div>
      </div>
    </div>
  );
};

export default AppBuilderAgent;
