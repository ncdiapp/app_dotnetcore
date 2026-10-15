import React, { useImperativeHandle, useRef } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import {
  ApiConfigFormModel,
  KvRow,
  SESSION_HEADER_KEY,
} from '../../helper/apiConfigForm';

export type ApiConfigFormHandle = {
  insertIntoApiUrl: (token: string) => void;
};

type Props = {
  model: ApiConfigFormModel;
  onChange: (model: ApiConfigFormModel) => void;
  onApiUrlBlur?: () => void;
};

function updateRow(rows: KvRow[], index: number, patch: Partial<KvRow>): KvRow[] {
  return rows.map((row, i) => (i === index ? { ...row, ...patch } : row));
}

const KvTable: React.FC<{
  rows: KvRow[];
  onChange: (rows: KvRow[]) => void;
  lockedFirstKey?: string;
}> = ({ rows, onChange, lockedFirstKey }) => {
  const { theme } = useTheme();
  const inputClass = `h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`;
  return (
    <div className="flex flex-col gap-1">
      {rows.map((row, index) => {
        const locked = lockedFirstKey != null && row.key === lockedFirstKey;
        return (
          <div key={`${locked ? 'locked' : 'row'}-${index}`} className="flex items-center gap-1">
            <input
              type="text"
              value={row.key}
              readOnly={locked}
              onChange={(e) => onChange(updateRow(rows, index, { key: e.target.value }))}
              className={`${inputClass} w-48 flex-shrink-0 ${locked ? 'opacity-80' : ''}`}
            />
            <input
              type="text"
              value={row.value}
              onChange={(e) => onChange(updateRow(rows, index, { value: e.target.value }))}
              className={`${inputClass} w-1 flex-auto min-w-0`}
            />
            {locked ? (
              <span className="w-6 flex-shrink-0" />
            ) : (
              <button
                type="button"
                title="Remove"
                onClick={() => onChange(rows.filter((_, i) => i !== index))}
                className={`h-7 w-6 flex-shrink-0 rounded-[4px] text-xs ${theme.button_default}`}
              >
                <i className="fa-solid fa-xmark" aria-hidden />
              </button>
            )}
          </div>
        );
      })}
      <button
        type="button"
        onClick={() => onChange([...rows, { key: '', value: '' }])}
        className={`self-start h-7 px-2 text-xs rounded-[4px] ${theme.button_default}`}
      >
        Add
      </button>
    </div>
  );
};

export const ApiConfigParametersForm = React.forwardRef<ApiConfigFormHandle, Props>(function ApiConfigParametersForm(
  { model, onChange, onApiUrlBlur },
  ref,
) {
  const { theme } = useTheme();
  const apiUrlRef = useRef<HTMLInputElement>(null);
  const labelClass = `w-40 flex-shrink-0 text-xs ${theme.label}`;
  const inputClass = `w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`;

  useImperativeHandle(ref, () => ({
    insertIntoApiUrl: (token: string) => {
      const el = apiUrlRef.current;
      const current = model.apiUrl ?? '';
      const at = el?.selectionStart ?? current.length;
      const next = current.slice(0, at) + token + current.slice(at);
      onChange({ ...model, apiUrl: next });
      const pos = at + token.length;
      setTimeout(() => {
        el?.focus();
        el?.setSelectionRange(pos, pos);
      }, 0);
    },
  }));

  return (
    <div className="flex flex-col gap-3 h-full min-h-0 overflow-auto">
      <div className="flex items-center gap-2">
        <label className={labelClass}>API URL</label>
        <input
          ref={apiUrlRef}
          type="text"
          value={model.apiUrl}
          onChange={(e) => onChange({ ...model, apiUrl: e.target.value })}
          onBlur={onApiUrlBlur}
          className={inputClass}
          spellCheck={false}
        />
      </div>

      <div className="flex items-start gap-2">
        <label className={`${labelClass} pt-1`}>Headers</label>
        <div className="w-1 flex-auto min-w-0">
          <KvTable
            rows={model.headers}
            lockedFirstKey={SESSION_HEADER_KEY}
            onChange={(headers) => onChange({ ...model, headers })}
          />
        </div>
      </div>

      <div className="flex items-start gap-2">
        <label className={`${labelClass} pt-1`}>Query Params</label>
        <div className="w-1 flex-auto min-w-0">
          <KvTable rows={model.queryParams} onChange={(queryParams) => onChange({ ...model, queryParams })} />
        </div>
      </div>

      <div className="flex items-start gap-2">
        <label className={`${labelClass} pt-1`}>Path Params</label>
        <div className="w-1 flex-auto min-w-0">
          <KvTable rows={model.pathParams} onChange={(pathParams) => onChange({ ...model, pathParams })} />
        </div>
      </div>

      <div className="flex items-center gap-2">
        <label className={labelClass}>Post Process Method</label>
        <input
          type="text"
          value={model.postProcessMethodName}
          onChange={(e) => onChange({ ...model, postProcessMethodName: e.target.value })}
          className={inputClass}
        />
      </div>

      <div className="flex items-start gap-2">
        <label className={`${labelClass} pt-1`}>Response → Environment</label>
        <div className="w-1 flex-auto min-w-0">
          <KvTable rows={model.responseToEnv} onChange={(responseToEnv) => onChange({ ...model, responseToEnv })} />
        </div>
      </div>

      <div className="flex items-start gap-2">
        <label className={`${labelClass} pt-1`}>Response Header → Cookie</label>
        <div className="w-1 flex-auto min-w-0 flex flex-col gap-1">
          {model.cookieHeaderNames.map((name, index) => (
            <div key={index} className="flex items-center gap-1">
              <input
                type="text"
                value={name}
                onChange={(e) => {
                  const cookieHeaderNames = model.cookieHeaderNames.slice();
                  cookieHeaderNames[index] = e.target.value;
                  onChange({ ...model, cookieHeaderNames });
                }}
                className={inputClass}
              />
              <button
                type="button"
                title="Remove"
                onClick={() => onChange({ ...model, cookieHeaderNames: model.cookieHeaderNames.filter((_, i) => i !== index) })}
                className={`h-7 w-6 flex-shrink-0 rounded-[4px] text-xs ${theme.button_default}`}
              >
                <i className="fa-solid fa-xmark" aria-hidden />
              </button>
            </div>
          ))}
          <button
            type="button"
            onClick={() => onChange({ ...model, cookieHeaderNames: [...model.cookieHeaderNames, ''] })}
            className={`self-start h-7 px-2 text-xs rounded-[4px] ${theme.button_default}`}
          >
            Add
          </button>
        </div>
      </div>
    </div>
  );
});

export type ApiConfigEditorField = {
  Name?: string;
  DefaultValue?: string | null;
};

export type ApiConfigEditorDto = {
  CurrentUserSessionId?: string | null;
  Criteria?: ApiConfigEditorField[];
};

/** Search API config: session header plus one read-only criterion name and its default value. */
export const SearchApiConfigForm: React.FC<{
  editor: ApiConfigEditorDto;
  onChange: (editor: ApiConfigEditorDto) => void;
}> = ({ editor, onChange }) => {
  const { theme } = useTheme();
  const labelClass = `w-48 flex-shrink-0 text-xs truncate ${theme.label}`;
  const inputClass = `w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`;
  const criteria = editor.Criteria ?? [];
  return (
    <div className="flex flex-col gap-2 h-full min-h-0 overflow-auto">
      <div className="flex items-center gap-2">
        <label className={labelClass}>CurrentUserSessionId</label>
        <input
          type="text"
          value={editor.CurrentUserSessionId ?? ''}
          onChange={(e) => onChange({ ...editor, CurrentUserSessionId: e.target.value })}
          className={inputClass}
          spellCheck={false}
        />
      </div>
      {criteria.map((row, index) => (
        <div key={`${row.Name ?? 'field'}-${index}`} className="flex items-center gap-2">
          <label className={labelClass} title={row.Name ?? ''}>{row.Name}</label>
          <input
            type="text"
            value={row.DefaultValue ?? ''}
            onChange={(e) => {
              const next = criteria.slice();
              next[index] = { ...row, DefaultValue: e.target.value };
              onChange({ ...editor, Criteria: next });
            }}
            className={inputClass}
            spellCheck={false}
          />
        </div>
      ))}
    </div>
  );
};

/** Transaction API config: session header and the id query value only. */
export const TransactionApiConfigForm: React.FC<{
  model: ApiConfigFormModel;
  onChange: (model: ApiConfigFormModel) => void;
}> = ({ model, onChange }) => {
  const { theme } = useTheme();
  const labelClass = `w-48 flex-shrink-0 text-xs ${theme.label}`;
  const inputClass = `w-1 flex-auto min-w-0 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`;
  const session = model.headers.find((row) => row.key === SESSION_HEADER_KEY)?.value ?? '';
  const id = model.queryParams.find((row) => row.key === 'id')?.value ?? '';
  return (
    <div className="flex flex-col gap-2 h-full min-h-0 overflow-auto">
      <div className="flex items-center gap-2">
        <label className={labelClass}>CurrentUserSessionId</label>
        <input
          type="text"
          autoComplete="off"
          spellCheck={false}
          value={session}
          onChange={(e) => {
            const value = e.target.value;
            const headers = model.headers.some((row) => row.key === SESSION_HEADER_KEY)
              ? model.headers.map((row) => (row.key === SESSION_HEADER_KEY ? { ...row, value } : row))
              : [{ key: SESSION_HEADER_KEY, value }, ...model.headers];
            onChange({ ...model, headers });
          }}
          className={inputClass}
        />
      </div>
      <div className="flex items-center gap-2">
        <label className={labelClass}>Id</label>
        <input
          type="text"
          autoComplete="off"
          spellCheck={false}
          value={id}
          onChange={(e) => {
            const value = e.target.value;
            const queryParams = model.queryParams.some((row) => row.key === 'id')
              ? model.queryParams.map((row) => (row.key === 'id' ? { ...row, value } : row))
              : [{ key: 'id', value }, ...model.queryParams];
            onChange({ ...model, queryParams });
          }}
          className={inputClass}
        />
      </div>
    </div>
  );
};

export const McpUsageFields: React.FC<{
  hint: string;
  notes: string;
  onHintChange: (value: string) => void;
  onNotesChange: (value: string) => void;
}> = ({ hint, notes, onHintChange, onNotesChange }) => {
  const { theme } = useTheme();
  const labelClass = `w-48 shrink-0 text-xs ${theme.label}`;
  const boxClass = `h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`;
  return (
    <div className="flex items-center gap-4 w-full min-w-0">
      <div className="flex items-center gap-2 shrink-0">
        <span className={labelClass}>MCP UI Hint</span>
        <select value={hint} onChange={(e) => onHintChange(e.target.value)} className={`w-52 ${boxClass}`}>
          <option value=""> </option>
          <option value="SelectorGrid">SelectorGrid</option>
          <option value="DateRangeFilter">DateRangeFilter</option>
          <option value="RecordCard">RecordCard</option>
          <option value="FlexGrid">FlexGrid</option>
          <option value="ActionMenu">ActionMenu</option>
          <option value="Form">Form</option>
          <option value="ChartView">ChartView</option>
          <option value="PivotTable">PivotTable</option>
          <option value="DataAnalysis">DataAnalysis</option>
        </select>
      </div>
      <div className="flex items-center gap-2 w-1 flex-auto min-w-0">
        <span className={labelClass}>AI Usage Notes For MCP</span>
        <input
          type="text"
          value={notes}
          onChange={(e) => onNotesChange(e.target.value)}
          className={`w-1 flex-auto min-w-0 ${boxClass}`}
        />
      </div>
    </div>
  );
};

export const IsUsedByMcpCheckbox: React.FC<{
  checked: boolean;
  onChange: (checked: boolean) => void;
}> = ({ checked, onChange }) => {
  const { theme } = useTheme();
  return (
    <label className={`inline-flex items-center gap-2 shrink-0 whitespace-nowrap text-xs ${theme.label}`}>
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
      />
      Is Used By MCP
    </label>
  );
};
