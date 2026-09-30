import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface Props {
    title?: string;
    status?: string;
    fields: Array<{ label: string; value: string }>;
}

export const DataRenderCard: React.FC<Props> = ({ title, status, fields }) => {
    const { theme } = useTheme();

    return (
        <div className={`border rounded-[4px] p-3 max-w-2xl ${theme.mainContentSection}`}>
            <div className="flex items-start justify-between gap-2 mb-2 flex-wrap">
                <div>
                    {title && <div className={`text-sm font-medium ${theme.label}`}>{title}</div>}
                </div>
                {status && (
                    <span className={`text-xs px-2 py-0.5 rounded border ${theme.button_default}`}>
                        {status}
                    </span>
                )}
            </div>
            {fields.length === 0 ? (
                <div className={`text-xs ${theme.label}`}>No fields.</div>
            ) : (
                <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-4 gap-y-1.5">
                    {fields.map((f, i) => (
                        <div key={`${f.label}-${i}`} className="min-w-0">
                            <dt className={`text-[10px] uppercase tracking-wide opacity-70 ${theme.label}`}>{f.label}</dt>
                            <dd className={`text-xs break-words ${theme.label}`}>{f.value || '—'}</dd>
                        </div>
                    ))}
                </dl>
            )}
        </div>
    );
};
