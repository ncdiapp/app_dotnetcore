import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { DataRenderKpiItem } from './types';

interface Props {
    title?: string;
    items: DataRenderKpiItem[];
}

/** Horizontal / wrapping KPI metric cards for kpi_dashboard. */
export const DataRenderKpiRow: React.FC<Props> = ({ title, items }) => {
    const { theme, t } = useTheme(); 

    return (
        <div className="w-full">
            {title && (
                <div className={`text-sm font-medium mb-2 ${theme.label}`}>{title}</div>
            )}
            <div className="flex flex-wrap gap-2">
                {items.map((it, i) => (
                    <div
                        key={`${it.label}-${i}`}
                        className={`min-w-[9rem] w-1 flex-auto max-w-xs rounded-[4px] border p-3 ${t('border_mainContentSection')} ${theme.mainContentSection}`}
                    >
                        <div className={`text-[10px] uppercase tracking-wide opacity-70 ${theme.label}`}>
                            {it.label || '—'}
                        </div>
                        <div className={`text-lg font-semibold mt-1 break-words ${theme.label}`}>
                            {it.value || '—'}
                        </div>
                        {it.hint && (
                            <div className={`text-[11px] mt-1 opacity-70 ${theme.label}`}>{it.hint}</div>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
};
