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
        <div className={`border rounded-[8px] px-3 py-2.5 ${theme.mainContentSection}`}>
            <div className="flex items-start justify-between gap-3 mb-2 flex-wrap">
                <div className="min-w-0">
                    {title && <div className={`text-sm font-semibold ${theme.title}`}>{title}</div>}
                </div>
                {status && (
                    <span className={`text-xs px-2 py-0.5 rounded border ${theme.button_default}`}>
                        {status}
                    </span>
                )}
            </div>
            {fields.length === 0 ? (
                <div className={`text-sm ${theme.label}`}>No fields.</div>
            ) : (
                <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1.5">
                    {fields.map((f, i) => (
                        <div key={`${f.label}-${i}`} className="min-w-0 flex items-baseline gap-3">
                            <dt className={`w-28 shrink-0 text-xs ${theme.label}`}>{f.label}</dt>
                            <dd className={`text-sm break-words ${theme.title}`}>{f.value || '—'}</dd>
                        </div>
                    ))}
                </dl>
            )}
        </div>
    );
};
