import React, { useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

export type SchemaConfirmColumn = {
  Name: string;
  DataType: string;
  IsPrimaryKey: boolean;
  IsNullable: boolean;
  IsAutoIncrement?: boolean;
  Length?: number;
  FKTargetTable?: string;
};

export type SchemaConfirmTable = {
  Name: string;
  Description?: string;
  IsLookup: boolean;
  ParentTable?: string;
  Columns: SchemaConfirmColumn[];
};

export type SchemaConfirmEvent = {
  Summary: string;
  SchemaJson: string;
  Tables: SchemaConfirmTable[];
  CreateScript?: string;
};

/** Inline schema review before DDL — shared by Managed Agent chat (app-builder). */
export const SchemaConfirmCard: React.FC<{
  schema: SchemaConfirmEvent;
  onApprove: (schemaJson?: string) => void;
  onReject: (feedback: string) => void;
}> = ({ schema, onApprove, onReject }) => {
  const { theme } = useTheme();
  const [deletedTables, setDeletedTables] = useState<Set<string>>(new Set());
  const [expandedTables, setExpandedTables] = useState<Set<string>>(
    () => new Set((schema.Tables ?? []).map(t => t.Name))
  );
  const [rejecting, setRejecting] = useState(false);
  const [feedback, setFeedback] = useState('');
  const [showScript, setShowScript] = useState(false);

  const tables = schema.Tables ?? [];
  const activeTables = tables.filter(t => !deletedTables.has(t.Name));

  const handleApprove = () => {
    if (deletedTables.size === 0) {
      onApprove(schema.SchemaJson);
      return;
    }
    try {
      const parsed = JSON.parse(schema.SchemaJson);
      const filteredTables = (parsed.Tables ?? []).filter((t: { Name?: string }) => !deletedTables.has(t.Name ?? ''));
      filteredTables.forEach((t: { Relationships?: Array<{ TargetTable?: string }> }) => {
        t.Relationships = (t.Relationships ?? []).filter(
          (r) => !deletedTables.has(r.TargetTable ?? '')
        );
      });
      onApprove(JSON.stringify({ ...parsed, Tables: filteredTables }));
    } catch {
      onApprove(schema.SchemaJson);
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
    <div className={`mx-2 rounded border-2 border-blue-400 overflow-hidden ${theme.mainContentSection}`}>
      <div className="flex items-center gap-2 px-3 py-2 bg-blue-50 border-b border-blue-300">
        <i className="fa-solid fa-table-columns text-blue-500" />
        <span className="text-xs font-semibold text-blue-700">
          Schema Review — Approve Before Building
        </span>
        <span className="text-[10px] text-blue-500 ml-auto">
          {tables.length} table{tables.length !== 1 ? 's' : ''}
        </span>
      </div>

      <div className="px-3 pt-2.5 pb-2 space-y-2">
        <p className={`text-xs leading-relaxed ${theme.label}`}>{schema.Summary}</p>

        {tables.map(table => {
          const isDeleted = deletedTables.has(table.Name);
          const isExpanded = expandedTables.has(table.Name) && !isDeleted;
          const borderCls = isDeleted
            ? 'border-gray-200 opacity-40'
            : table.IsLookup
              ? 'border-purple-200'
              : 'border-blue-200';
          const headerBg = isDeleted
            ? 'bg-gray-50'
            : table.IsLookup
              ? 'bg-purple-50'
              : 'bg-blue-50';

          return (
            <div key={table.Name} className={`rounded border ${borderCls}`}>
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
                  <span className="px-1 py-0.5 rounded text-[9px] bg-purple-100 text-purple-600">Lookup</span>
                )}
                {table.ParentTable && !isDeleted && (
                  <span className={`text-[10px] ${theme.label} opacity-60`}>
                    <i className="fa-solid fa-arrow-left text-[8px] mr-0.5" />{table.ParentTable}
                  </span>
                )}
                <span className={`ml-auto text-[10px] ${theme.label} opacity-50 mr-1`}>
                  {(table.Columns ?? []).length} col{(table.Columns ?? []).length !== 1 ? 's' : ''}
                </span>
                <button
                  type="button"
                  onClick={e => { e.stopPropagation(); toggleTable(table.Name); }}
                  title={isDeleted ? 'Restore table' : 'Remove table'}
                  className={`w-5 h-5 flex items-center justify-center rounded ${
                    isDeleted ? 'text-green-500' : 'text-red-400'
                  }`}
                >
                  <i className={`fa-solid ${isDeleted ? 'fa-rotate-left' : 'fa-trash'} text-[10px]`} />
                </button>
              </div>

              {isExpanded && (
                <table className="w-full text-[10px]">
                  <thead>
                    <tr className={`border-b border-gray-100 ${theme.label} opacity-60`}>
                      <th className="text-left px-2 py-1 font-medium">Column</th>
                      <th className="text-left px-2 py-1 font-medium">Type</th>
                      <th className="text-center px-1 py-1 font-medium w-8">PK</th>
                      <th className="text-center px-1 py-1 font-medium w-8">Null</th>
                      <th className="text-left px-2 py-1 font-medium">FK →</th>
                    </tr>
                  </thead>
                  <tbody>
                    {(table.Columns ?? []).map((col, ci) => (
                      <tr key={ci} className="border-b last:border-0 border-gray-100">
                        <td className={`px-2 py-1 font-mono ${col.IsPrimaryKey ? 'text-amber-600' : theme.title}`}>
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

        {schema.CreateScript && (
          <div>
            <button
              type="button"
              onClick={() => setShowScript(s => !s)}
              className={`flex items-center gap-1.5 text-[10px] ${theme.label} opacity-60`}
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

        {rejecting && (
          <textarea
            value={feedback}
            onChange={e => setFeedback(e.target.value)}
            placeholder="What should change? e.g. 'Add a Price column to Products'"
            rows={2}
            autoFocus
            className={`w-full px-2 py-1.5 text-xs border rounded-[4px] resize-none ${theme.inputBox}`}
          />
        )}
      </div>

      <div className="flex items-center gap-2 px-3 py-2 bg-blue-50 border-t border-blue-300">
        {!rejecting ? (
          <>
            <button
              type="button"
              onClick={handleApprove}
              disabled={activeTables.length === 0}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-green-600 text-white font-semibold disabled:opacity-50"
            >
              <i className="fa-solid fa-circle-check" />
              Approve — Build it{deletedTables.size > 0 ? ` (${activeTables.length} tables)` : ''}
            </button>
            <button
              type="button"
              onClick={() => setRejecting(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-500 text-white"
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
              type="button"
              onClick={() => onReject(feedback || 'User rejected the schema.')}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs rounded-[4px] bg-red-600 text-white font-semibold"
            >
              <i className="fa-solid fa-paper-plane" />
              Send feedback
            </button>
            <button
              type="button"
              onClick={() => { setRejecting(false); setFeedback(''); }}
              className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
            >
              Cancel
            </button>
          </>
        )}
      </div>
    </div>
  );
};
