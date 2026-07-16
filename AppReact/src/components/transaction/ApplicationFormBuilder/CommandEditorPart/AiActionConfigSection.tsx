import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { aiSkillSvc, type AppAISkillDto } from '../../../../webapi/aiSkillSvc';

interface InputBinding { fieldName: string; inputType: 'text' | 'image'; }
interface OutputBinding { outputKey: string; targetType: 'text_field' | 'child_grid'; targetName: string; }

const flattenFields = (units: any[]): Array<{ groupLabel: string; fieldName: string; displayName: string }> => {
  const out: Array<{ groupLabel: string; fieldName: string; displayName: string }> = [];
  for (const u of units ?? []) {
    const grp = u.UnitDisplayName ?? u.UnitName ?? String(u.Id);
    for (const f of u.AppTransactionFieldList ?? []) {
      const fieldName = f.DataBaseFieldName ?? f.dataBaseFieldName ?? '';
      if (fieldName) out.push({ groupLabel: grp, fieldName, displayName: f.DisplayName ?? f.displayName ?? fieldName });
    }
    out.push(...flattenFields(u.AppTransactionUnitList ?? u.Children ?? []));
  }
  return out;
};

const collectUnits = (units: any[]): Array<{ id: string; name: string }> =>
  (units ?? []).map(u => ({ id: String(u.Id), name: u.UnitDisplayName ?? u.UnitName ?? String(u.Id) }));

const readCfg = (action: any): { skillName: string; inputBindings: InputBinding[]; outputBindings: OutputBinding[] } => {
  const raw = action.ActionAttribute;
  if (!raw || typeof raw !== 'object') return { skillName: '', inputBindings: [], outputBindings: [] };
  return {
    skillName: raw.skillName ?? '',
    inputBindings: (raw.inputBindings ?? []) as InputBinding[],
    outputBindings: (raw.outputBindings ?? []) as OutputBinding[],
  };
};

export function AiActionConfigSection(props: {
  action: any;
  hierarchy: any;
  onMarkChange: () => void;
}) {
  const { action, hierarchy, onMarkChange } = props;
  const { theme } = useTheme();
  const [skills, setSkills] = useState<AppAISkillDto[]>([]);
  const loadedRef = useRef(false);

  useEffect(() => {
    if (loadedRef.current) return;
    loadedRef.current = true;
    aiSkillSvc.GetDefaultDataSourceId()
      .then(res => {
        const dsId = res?.Object;
        if (dsId == null) return;
        return aiSkillSvc.GetAll(dsId).then(r => setSkills(r?.Object ?? []));
      })
      .catch(() => { /* ignore */ });
  }, []);

  const cfg = readCfg(action);
  const allFields = flattenFields(hierarchy?.AppTransactionUnitList ?? []);
  const allUnits = collectUnits(hierarchy?.AppTransactionUnitList ?? []);

  const patch = (next: Partial<typeof cfg>) => {
    action.ActionAttribute = { ...(action.ActionAttribute ?? {}), ...next };
    onMarkChange();
  };

  const addInput = () => patch({ inputBindings: [...cfg.inputBindings, { fieldName: allFields[0]?.fieldName ?? '', inputType: 'text' }] });
  const removeInput = (i: number) => patch({ inputBindings: cfg.inputBindings.filter((_, idx) => idx !== i) });
  const updateInput = (i: number, p: Partial<InputBinding>) =>
    patch({ inputBindings: cfg.inputBindings.map((b, idx) => idx === i ? { ...b, ...p } : b) });

  const addOutput = () => patch({ outputBindings: [...cfg.outputBindings, { outputKey: '', targetType: 'child_grid', targetName: allUnits[0]?.id ?? '' }] });
  const removeOutput = (i: number) => patch({ outputBindings: cfg.outputBindings.filter((_, idx) => idx !== i) });
  const updateOutput = (i: number, p: Partial<OutputBinding>) =>
    patch({ outputBindings: cfg.outputBindings.map((b, idx) => idx === i ? { ...b, ...p } : b) });

  const jsonPreview = JSON.stringify({ skillName: cfg.skillName, inputBindings: cfg.inputBindings, outputBindings: cfg.outputBindings }, null, 2);

  const rowCls = 'flex items-center gap-2 mb-1 flex-wrap';
  const selectCls = `h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
  const btnCls = `px-2 py-0.5 text-xs rounded-[4px] ${theme.button_default}`;

  return (
    <div className="flex flex-col gap-4 pb-2">

      {/* Skill picker */}
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2">
        <label className={`text-xs ${theme.label}`}>AI Skill</label>
        <select
          value={cfg.skillName}
          onChange={e => patch({ skillName: e.target.value })}
          className={`w-72 ${selectCls}`}
        >
          <option value="">— Select Skill —</option>
          {skills.map(s => <option key={s.SkillId} value={s.Name}>{s.Name}</option>)}
        </select>
      </div>

      {/* Input Bindings */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <span className={`text-xs font-semibold ${theme.label}`}>Input Bindings</span>
          <button type="button" onClick={addInput} className={btnCls}>
            <i className="fa-solid fa-plus mr-1" />Add
          </button>
        </div>
        {cfg.inputBindings.length === 0 && (
          <p className={`text-xs italic ${theme.label}`}>No input bindings — click Add to configure.</p>
        )}
        {cfg.inputBindings.map((b, i) => (
          <div key={i} className={rowCls}>
            <select
              value={b.fieldName}
              onChange={e => updateInput(i, { fieldName: e.target.value })}
              className={`${selectCls} w-60`}
            >
              {allFields.map(f => (
                <option key={f.fieldName} value={f.fieldName}>{f.groupLabel} › {f.displayName}</option>
              ))}
            </select>
            <select
              value={b.inputType}
              onChange={e => updateInput(i, { inputType: e.target.value as 'text' | 'image' })}
              className={`${selectCls} w-24`}
            >
              <option value="text">Text</option>
              <option value="image">Image</option>
            </select>
            <button type="button" onClick={() => removeInput(i)} className={btnCls} title="Remove">
              <i className="fa-solid fa-xmark" />
            </button>
          </div>
        ))}
      </div>

      {/* Output Bindings */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <span className={`text-xs font-semibold ${theme.label}`}>Output Bindings</span>
          <button type="button" onClick={addOutput} className={btnCls}>
            <i className="fa-solid fa-plus mr-1" />Add
          </button>
        </div>
        {cfg.outputBindings.length === 0 && (
          <p className={`text-xs italic ${theme.label}`}>No output bindings — click Add to configure.</p>
        )}
        {cfg.outputBindings.map((ob, i) => (
          <div key={i} className={rowCls}>
            <input
              type="text"
              placeholder="AI key (e.g. rows)"
              value={ob.outputKey}
              onChange={e => updateOutput(i, { outputKey: e.target.value })}
              className={`${selectCls} w-36`}
            />
            <select
              value={ob.targetType}
              onChange={e => {
                const t = e.target.value as 'text_field' | 'child_grid';
                updateOutput(i, { targetType: t, targetName: t === 'child_grid' ? (allUnits[0]?.id ?? '') : (allFields[0]?.fieldName ?? '') });
              }}
              className={`${selectCls} w-28`}
            >
              <option value="child_grid">Child Grid</option>
              <option value="text_field">Text Field</option>
            </select>
            {ob.targetType === 'child_grid' ? (
              <select
                value={ob.targetName}
                onChange={e => updateOutput(i, { targetName: e.target.value })}
                className={`${selectCls} w-52`}
              >
                {allUnits.map(u => <option key={u.id} value={u.id}>{u.name}</option>)}
              </select>
            ) : (
              <select
                value={ob.targetName}
                onChange={e => updateOutput(i, { targetName: e.target.value })}
                className={`${selectCls} w-52`}
              >
                {allFields.map(f => <option key={f.fieldName} value={f.fieldName}>{f.displayName}</option>)}
              </select>
            )}
            <button type="button" onClick={() => removeOutput(i)} className={btnCls} title="Remove">
              <i className="fa-solid fa-xmark" />
            </button>
          </div>
        ))}
      </div>

      {/* JSON Preview */}
      <div>
        <span className={`text-xs font-semibold ${theme.label}`}>ActionAttribute Preview</span>
        <pre className={`mt-1 p-2 text-xs rounded border ${theme.inputBox} overflow-auto max-h-40 whitespace-pre-wrap`}>{jsonPreview}</pre>
      </div>

    </div>
  );
}
