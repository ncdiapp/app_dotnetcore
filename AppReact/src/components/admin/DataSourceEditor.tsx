import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

export interface DataSourceDef {
  name: string;
  type: 'sp' | 'api';
  value: string;
  sampleJson?: string;
}

interface Props {
  sources: DataSourceDef[];
  onChange: (sources: DataSourceDef[]) => void;
  compact?: boolean;
}

const SP_HINT  = 'sp_StyleSummaryReport';
const API_HINT = '/webapi/MyModule/GetData';

export function serializeSources(sources: DataSourceDef[]): string {
  const active = sources.filter(s => s.value.trim());
  if (active.length === 0) return '';
  return JSON.stringify(active);
}

export function parseSources(dataSpName: string | null | undefined): DataSourceDef[] {
  if (!dataSpName) return [{ name: 'header', type: 'sp', value: '' }];
  const trimmed = dataSpName.trim();
  if (trimmed.startsWith('[')) {
    try { return JSON.parse(trimmed) as DataSourceDef[]; } catch { /* fall through */ }
  }
  return [{ name: 'header', type: 'sp', value: dataSpName }];
}

/** Parse sources from ExtraParamConfig JSON object (new multi-source format). */
export function parseSourcesFromConfig(extraParamConfig: string | null | undefined, dataSpName: string | null | undefined): DataSourceDef[] {
  if (extraParamConfig) {
    const trimmed = extraParamConfig.trim();
    if (trimmed.startsWith('{')) {
      try {
        const cfg = JSON.parse(trimmed) as { dataSources?: DataSourceDef[] };
        if (cfg.dataSources && cfg.dataSources.length > 0) return cfg.dataSources;
      } catch { /* fall through */ }
    }
  }
  // Legacy: DataSpName
  return parseSources(dataSpName);
}

/** Serialise sources into ExtraParamConfig, preserving existing extraParams entries. */
export function serializeConfig(sources: DataSourceDef[], existingConfig: string | null | undefined): string {
  let existing: any = {};
  if (existingConfig) {
    const trimmed = existingConfig.trim();
    if (trimmed.startsWith('{')) {
      try { existing = JSON.parse(trimmed); } catch { /* ignore */ }
    }
    // If it was an old plain-array extraParams, migrate it
    if (trimmed.startsWith('[')) {
      try { existing = { extraParams: JSON.parse(trimmed) }; } catch { /* ignore */ }
    }
  }
  existing.dataSources = sources.filter(s => s.value.trim());
  return JSON.stringify(existing);
}

// ─────────────────────────────────────────────────────────────────────────────

const DataSourceEditor: React.FC<Props> = ({ sources, onChange, compact = false }) => {
  const { theme, t } = useTheme();

  const add = () => {
    onChange([...sources, { name: `src${sources.length}`, type: 'sp', value: '' }]);
  };

  const remove = (idx: number) => {
    if (sources.length <= 1) return;
    onChange(sources.filter((_, i) => i !== idx));
  };

  const update = (idx: number, field: keyof DataSourceDef, val: string) => {
    onChange(sources.map((s, i) => i === idx ? { ...s, [field]: val } : s));
  };

  if (compact) {
    // Compact mode: used inside the Designer toolbar (horizontal)
    return (
      <div className="flex flex-col gap-1">
        {sources.map((src, idx) => (
          <div key={idx} className="flex flex-col gap-0.5">
            <div className="flex items-center gap-1">
              {/* Editable name — same for every source */}
              <input
                className={`h-7 w-20 px-1.5 text-xs border rounded-[4px] font-mono ${theme.inputBox}`}
                value={src.name}
                onChange={e => update(idx, 'name', e.target.value.replace(/\s/g, '_').toLowerCase())}
                placeholder="name"
                title={src.type === 'sp'
                  ? `SP tokens — scalar: {{${src.name}.Field}}  list RS1: {{#each ${src.name}_rs1}}  RS2: {{#each ${src.name}_rs2}}`
                  : `API tokens — primitive: {{${src.name}.Field}}  array "Lines": {{#each ${src.name}_Lines}}`}
              />

              {/* Type toggle */}
              <button
                className={`h-7 px-2 text-xs rounded-[4px] border shrink-0 ${
                  src.type === 'sp' ? 'text-blue-500 border-blue-300' : 'text-purple-500 border-purple-300'
                } ${theme.mainContentSection}`}
                onClick={() => update(idx, 'type', src.type === 'sp' ? 'api' : 'sp')}
                title="Toggle SP / API"
              >
                {src.type === 'sp' ? <><i className="fa-solid fa-database mr-1" />SP</> : <><i className="fa-solid fa-network-wired mr-1" />API</>}
              </button>

              {/* SP name / API path */}
              <input
                className={`h-7 w-48 px-2 text-xs border rounded-[4px] font-mono flex-shrink ${theme.inputBox}`}
                value={src.value}
                onChange={e => update(idx, 'value', e.target.value)}
                placeholder={src.type === 'sp' ? SP_HINT : API_HINT}
              />

              {/* Remove — not for the first source */}
              {idx > 0 && (
                <button
                  className="h-7 px-1.5 text-xs text-red-400 hover:text-red-600"
                  onClick={() => remove(idx)}
                  title="Remove this source"
                >
                  <i className="fa-solid fa-xmark" />
                </button>
              )}
            </div>

            {/* Sample JSON — only for API sources */}
            {src.type === 'api' && (
              <textarea
                className={`w-full h-16 px-2 py-1 text-xs border rounded-[4px] font-mono resize-none ${theme.inputBox}`}
                value={src.sampleJson || ''}
                onChange={e => update(idx, 'sampleJson', e.target.value)}
                placeholder={`Paste sample API response:\n{"Brand":"CGS","Lines":[{"Sku":"A","Qty":1}]}`}
              />
            )}
          </div>
        ))}
        <button onClick={add} className={`h-6 px-2 text-xs rounded-[4px] self-start ${theme.button_default}`}>
          <i className="fa-solid fa-plus mr-1" />Add Source
        </button>
      </div>
    );
  }

  // Full mode: used in the Create / Edit dialog (vertical)
  return (
    <div className="space-y-2">
      {sources.map((src, idx) => (
        <div key={idx} className={`rounded-[4px] border p-3 ${t('border_mainContentSection')} ${theme.mainContentSection}`}>
          <div className="flex items-center gap-2 mb-2">
            <span className={`text-xs shrink-0 ${theme.label}`}>Source Name:</span>
            <input
              className={`h-6 w-24 px-2 text-xs border rounded-[4px] font-mono ${theme.inputBox}`}
              value={src.name}
              onChange={e => update(idx, 'name', e.target.value.replace(/\s/g, '_').toLowerCase())}
              placeholder="header"
              title="Used as the token prefix in your report template"
            />
            {idx > 0 && (
              <button className="ml-auto text-xs text-red-400 hover:text-red-600" onClick={() => remove(idx)}>
                <i className="fa-solid fa-trash" />
              </button>
            )}
          </div>
          {/* Token hint — SP uses RS0/RS1/RS2 numbering; API uses JSON property paths */}
          <div className={`text-[10px] mb-2 ${theme.label} opacity-60`}>
            {src.type === 'sp' ? (
              <>
                <i className="fa-solid fa-database mr-1 text-blue-400" />
                <code>{`{{${src.name || `src${idx}`}.Field}}`}</code> (RS0 scalar) &nbsp;·&nbsp;
                <code>{`{{#each ${src.name || `src${idx}`}_rs1}}`}</code> (RS1 list) &nbsp;·&nbsp;
                <code>{`{{#each ${src.name || `src${idx}`}_rs2}}`}</code> (RS2 list) …
              </>
            ) : (
              <>
                <i className="fa-solid fa-network-wired mr-1 text-purple-400" />
                <code>{`{{${src.name || `src${idx}`}.Field}}`}</code> (JSON primitive) &nbsp;·&nbsp;
                array property <code>"Lines"</code> → <code>{`{{#each ${src.name || `src${idx}`}_Lines}}`}</code>
              </>
            )}
          </div>
          <div className="flex items-center gap-3">
            <div className="flex gap-2 shrink-0">
              <label className={`flex items-center gap-1 text-xs cursor-pointer ${theme.label}`}>
                <input type="radio" checked={src.type === 'sp'} onChange={() => update(idx, 'type', 'sp')} />
                <i className="fa-solid fa-database text-blue-500 mr-0.5" />SP
              </label>
              <label className={`flex items-center gap-1 text-xs cursor-pointer ${theme.label}`}>
                <input type="radio" checked={src.type === 'api'} onChange={() => update(idx, 'type', 'api')} />
                <i className="fa-solid fa-network-wired text-purple-500 mr-0.5" />API
              </label>
            </div>
            <input
              className={`h-7 flex-auto px-2 text-xs border rounded-[4px] font-mono ${theme.inputBox}`}
              value={src.value}
              onChange={e => update(idx, 'value', e.target.value)}
              placeholder={src.type === 'sp' ? SP_HINT : API_HINT}
            />
          </div>
          {src.type === 'sp' && (
            <p className={`text-[10px] mt-1 ${theme.label} opacity-60`}>
              SP must accept <code>@MainReferenceId INT, @MasterReferenceId INT = NULL, @ExtraParams NVARCHAR(MAX) = NULL</code>
            </p>
          )}
          {src.type === 'api' && (
            <div className="mt-2">
              <label className={`text-xs font-medium block mb-1 ${theme.label}`}>
                <i className="fa-solid fa-brackets-curly mr-1 text-purple-400" />
                Sample JSON <span className="opacity-60 font-normal">(paste one record — used for token discovery)</span>
              </label>
              <textarea
                className={`w-full h-28 px-2 py-1.5 text-xs border rounded-[4px] font-mono resize-y ${theme.inputBox}`}
                value={src.sampleJson || ''}
                onChange={e => update(idx, 'sampleJson', e.target.value)}
                placeholder={`{\n  "ProductCode": "P001",\n  "Description": "Shirt",\n  "Lines": [{"Sku":"A","Qty":1}]\n}`}
              />
              <p className={`text-[10px] mt-0.5 ${theme.label} opacity-60`}>
                Root array → <code>{'{{#each ' + src.name + '}}'}</code> &nbsp;·&nbsp;
                Nested array <code>"Lines"</code> → <code>{'{{#each ' + src.name + '_Lines}}'}</code>
              </p>
            </div>
          )}
        </div>
      ))}
      <button onClick={add} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
        <i className="fa-solid fa-plus mr-1" />Add Another Source
      </button>
    </div>
  );
};

export default DataSourceEditor;
