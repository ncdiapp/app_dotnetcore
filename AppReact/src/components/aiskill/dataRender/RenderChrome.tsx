import React, { useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import type { DataRenderEvent } from './types';

interface ChipProps {
    event: DataRenderEvent;
    open: boolean;
    onOpen: () => void;
}

const iconFor = (ui: string): string => {
    if (ui === 'chart') return 'fa-solid fa-chart-column';
    if (ui === 'card' || ui === 'cards') return 'fa-solid fa-table-cells-large';
    if (ui === 'kpi_dashboard' || ui === 'kpi' || ui === 'dashboard') return 'fa-solid fa-gauge';
    if (ui === 'page') return 'fa-solid fa-table-columns';
    return 'fa-solid fa-table';
};

export const RenderChip: React.FC<ChipProps> = ({ event, open, onOpen }) => {
    const { theme } = useTheme();
    const uiRaw = (event.Ui || event.Layout || 'grid').toLowerCase();
    const ui = uiRaw === 'cards' ? 'card' : uiRaw;
    const title = event.Title || (ui === 'chart' ? 'Chart' : ui === 'card' ? 'Cards' : ui === 'page' ? 'Page' : 'Table');
    const fact = event.RowCount && event.RowCount > 0 ? `${event.RowCount} records` : 'Open the full view';
    return (
        <button
            type="button"
            onClick={onOpen}
            className={`my-1 w-full max-w-sm flex items-center gap-3 px-3 py-2.5 rounded-[8px] border text-left ${theme.mainContentSection} ${open ? 'ring-2 ring-offset-1' : ''}`}
        >
            <i className={`${iconFor(ui)} text-sm`} aria-hidden />
            <span className="min-w-0 w-1 flex-auto">
                <span className={`block text-sm font-medium truncate ${theme.title}`}>{title}</span>
                <span className={`block text-xs ${theme.label}`}>{open ? 'Showing in panel' : fact}</span>
            </span>
            <i className="fa-solid fa-chevron-right text-xs" aria-hidden />
        </button>
    );
};

export const RenderBuilding: React.FC = () => {
    const { theme } = useTheme();
    return (
        <div className={`my-1 max-w-sm rounded-[8px] border px-3 py-3 animate-pulse ${theme.mainContentSection}`}>
            <span className={`text-xs ${theme.label}`}>Building…</span>
        </div>
    );
};

interface ToolsProps {
    steps: Array<{ toolName: string; label?: string; isSuccess?: boolean; args?: string; result?: string }>;
    running: boolean;
}

export const TurnToolHeader: React.FC<ToolsProps> = ({ steps, running }) => {
    const { theme, t } = useTheme();
    const [open, setOpen] = useState(running);
    const [touched, setTouched] = useState(false);
    React.useEffect(() => {
        if (running || touched) return;
        const t = window.setTimeout(() => setOpen(false), 800);
        return () => window.clearTimeout(t);
    }, [running, touched]);
    if (!steps.length && !running) return null;
    const label = running ? 'Working…' : `${steps.length} tool${steps.length === 1 ? '' : 's'} used`;
    return (
        <div className={`rounded-[8px] border text-xs ${theme.mainContentSection}`}>
            <button
                type="button"
                className="w-full flex items-center gap-2 px-3 py-2 text-left"
                onClick={() => { setTouched(true); setOpen(v => !v); }}
            >
                {running
                    ? <i className={`fa-solid fa-spinner fa-spin text-xs ${t('text_label')}`} aria-hidden />
                    : <span className={`inline-block w-1.5 h-1.5 rounded-full bg-current opacity-40 ${t('text_label')}`} aria-hidden />}
                <span className={`font-normal ${t('text_label')}`}>{label}</span>
                <i className={`ml-auto text-xs fa-solid ${open ? 'fa-chevron-down' : 'fa-chevron-right'} ${t('text_label')}`} aria-hidden />
            </button>
            {open && steps.length > 0 && (
                <div className="px-3 pb-2 flex flex-col gap-1">
                    {steps.map((step, i) => (
                        <div key={`${step.toolName}-${i}`} className="flex items-center gap-2">
                            <span className="font-mono truncate">{step.label || step.toolName}</span>
                            <i className={`ml-auto fa-solid ${step.isSuccess === false ? 'fa-circle-xmark' : 'fa-circle-check'}`} aria-hidden />
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};
