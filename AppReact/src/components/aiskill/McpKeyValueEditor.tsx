import React, { useState } from 'react';
import { Theme } from '../../redux/features/ui/theme/types';

interface Props {
    theme: Theme;
    label: string;
    value: string;
    keyPlaceholder: string;
    valuePlaceholder: string;
    addLabel: string;
    onChange: (json: string) => void;
    /** Values are secrets: shown as dots. A saved value comes back from the server masked and stays unchanged if left as is. */
    maskValues?: boolean;
}

interface Row { k: string; v: string; }

const parse = (json: string): Row[] => {
    if (!json) return [];
    try {
        const obj = JSON.parse(json) as Record<string, string>;
        return Object.entries(obj).map(([k, v]) => ({ k, v: String(v ?? '') }));
    } catch {
        return [];
    }
};

const serialize = (rows: Row[]): string => {
    const obj: Record<string, string> = {};
    rows.forEach(r => { if (r.k.trim()) obj[r.k.trim()] = r.v; });
    return Object.keys(obj).length ? JSON.stringify(obj) : '';
};

// Edits a JSON object string as key/value rows. Remount (change `key`) to reload from `value`.
const McpKeyValueEditor: React.FC<Props> = ({ theme, label, value, keyPlaceholder, valuePlaceholder, addLabel, onChange, maskValues }) => {
    const [rows, setRows] = useState<Row[]>(() => parse(value));

    const commit = (next: Row[]) => { setRows(next); onChange(serialize(next)); };
    const setRow = (i: number, patch: Partial<Row>) => commit(rows.map((r, idx) => (idx === i ? { ...r, ...patch } : r)));

    const inp = `flex-auto w-32 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`;
    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

    return (
        <div className="flex items-start py-1">
            <label className={`w-32 text-xs ${theme.label} mr-2 pt-1.5`}>{label}</label>
            <div className="w-1 flex-auto flex flex-col gap-1">
                {rows.map((r, i) => (
                    <div key={i} className="flex items-center gap-1">
                        <input className={inp} value={r.k} placeholder={keyPlaceholder} autoComplete="off" onChange={e => setRow(i, { k: e.target.value })} />
                        <input className={inp} type={maskValues ? 'password' : 'text'} value={r.v} placeholder={valuePlaceholder}
                            autoComplete={maskValues ? 'new-password' : 'off'} onChange={e => setRow(i, { v: e.target.value })} />
                        <button className={btn} title="Remove" onClick={() => commit(rows.filter((_, idx) => idx !== i))}>
                            <i className="fa-solid fa-trash" />
                        </button>
                    </div>
                ))}
                <div>
                    <button className={btn} onClick={() => setRows([...rows, { k: '', v: '' }])}>
                        <i className="fa-solid fa-plus mr-1" />{addLabel}
                    </button>
                </div>
            </div>
        </div>
    );
};

export default McpKeyValueEditor;
