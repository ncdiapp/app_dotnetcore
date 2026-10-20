import React, { useEffect, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

function splitSegments(text: string): string[] {
    const raw = (text || '').trim();
    if (!raw) return [];
    // One causal beat per blank-line block (backend emits intent + outcome as separate thinking events)
    const parts = raw
        .split(/\n{2,}/)
        .map(s => s.trim())
        .filter(Boolean);
    return parts.length > 0 ? parts : [raw];
}

function truncate(s: string, n: number): string {
    if (!s || s.length <= n) return s || '';
    return s.slice(0, n - 1) + '…';
}

interface Props {
    text: string;
    isLive?: boolean;
}

/**
 * Reasoning loop panel — agent's reasoning narrative (how it picks tools, recovers, converges).
 * Not a dump of tool args/results (those stay in Tool Activity).
 */
export const ReasoningLoopPanel: React.FC<Props> = ({ text, isLive }) => {
    const { theme, t } = useTheme();
    const [open, setOpen] = useState(!!isLive);
    const segments = splitSegments(text);

    useEffect(() => {
        if (isLive) setOpen(true);
    }, [isLive]);

    if (!segments.length && !isLive) return null;

    const preview = truncate(segments[segments.length - 1] ?? (isLive ? 'Reasoning…' : ''), 100);

    return (
        <div className={`rounded-[4px] border text-xs ${theme.mainContentSection}`}>
            <button
                type="button"
                className="w-full flex items-start gap-2 px-2.5 py-1.5 text-left"
                onClick={() => setOpen(v => !v)}
            >
                {isLive
                    ? <i className={`fa-solid fa-spinner fa-spin mt-0.5 shrink-0 ${t('text_label')}`} aria-hidden />
                    : <i className={`fa-solid fa-brain mt-0.5 shrink-0 opacity-60 ${t('text_label')}`} aria-hidden />}
                <span className="w-1 flex-auto min-w-0">
                    <span className={`font-medium ${t('text_default')}`}>Reasoning loop</span>
                    <span className={`ml-2 ${theme.label}`}>
                        {segments.length > 0 ? `${segments.length} step${segments.length === 1 ? '' : 's'}` : ''}
                        {isLive ? (segments.length ? ' · live' : '…') : ''}
                    </span>
                    {!open && preview && (
                        <span className={`block mt-0.5 opacity-70 truncate ${theme.label}`}>{preview}</span>
                    )}
                </span>
                <i className={`fa-solid ${open ? 'fa-chevron-down' : 'fa-chevron-right'} mt-0.5 opacity-40 shrink-0`} aria-hidden />
            </button>
            {open && (
                <div className="px-2.5 pb-2 max-h-56 overflow-y-auto flex flex-col gap-1.5">
                    {segments.length === 0 && isLive && (
                        <div className={`opacity-60 ${theme.label}`}>Analyzing…</div>
                    )}
                    {segments.map((seg, idx) => (
                        <div
                            key={idx}
                            className={`px-2 py-1.5 rounded-[4px] whitespace-pre-wrap break-words leading-relaxed ${theme.label} ${t('border_mainContentSection')} border`}
                        >
                            <span className="opacity-40 mr-1.5 text-[10px]">#{idx + 1}</span>
                            {seg}
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
};

/** @deprecated use ReasoningLoopPanel */
export const ThinkingPanel = ReasoningLoopPanel;
