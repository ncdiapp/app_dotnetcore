import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentSvc, type DataRenderEvent } from '../../webapi/genericAgentSvc';

interface Props {
    event: DataRenderEvent;
    skillKey: string;
    sessionKey: string | null | undefined;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

/**
 * Result panel host: iframe srcDoc for C#-merged HTML (none-interactive / interactive).
 */
const UiRenderFrame: React.FC<Props> = ({ event, skillKey, sessionKey, disabled, onAction }) => {
    const { theme } = useTheme();
    const iframeRef = useRef<HTMLIFrameElement>(null);
    const [html, setHtml] = useState<string | null>(event.Html ?? null);
    const [error, setError] = useState<string | null>(null);
    const interactive = (event.Mode || '').toLowerCase() === 'interactive';

    useEffect(() => {
        let cancelled = false;
        setError(null);
        if (event.Html) {
            setHtml(event.Html);
            return;
        }
        if (event.HtmlPath && sessionKey) {
            setHtml(null);
            void (async () => {
                try {
                    const res = await genericAgentSvc.ReadAgentFile(skillKey, sessionKey, event.HtmlPath!);
                    if (cancelled) return;
                    if (res?.Content) setHtml(res.Content);
                    else setError('Could not load Result HTML file.');
                } catch {
                    if (!cancelled) setError('Could not load Result HTML file.');
                }
            })();
            return () => { cancelled = true; };
        }
        setHtml(null);
        setError('Re-run to view in Result (no HTML on this message).');
        return () => { cancelled = true; };
    }, [event.Html, event.HtmlPath, event.RenderId, skillKey, sessionKey]);

    useEffect(() => {
        const onMessage = (ev: MessageEvent) => {
            if (disabled || !onAction) return;
            const data = ev.data;
            if (!data || typeof data !== 'object') return;
            if (data.type !== 'agent_ui_action') return;
            if (data.renderId && event.RenderId && data.renderId !== event.RenderId) return;
            const iframe = iframeRef.current;
            if (iframe && ev.source && ev.source !== iframe.contentWindow) return;
            const actionId = String(data.actionId || '');
            if (!actionId) return;
            const selectionJson = data.selection != null ? JSON.stringify(data.selection) : undefined;
            onAction(actionId, selectionJson);
        };
        window.addEventListener('message', onMessage);
        return () => window.removeEventListener('message', onMessage);
    }, [disabled, onAction, event.RenderId]);

    const sandbox = interactive
        ? 'allow-scripts allow-same-origin'
        : 'allow-same-origin';

    if (error) {
        return (
            <div className={`text-xs p-3 rounded border ${theme.label}`}>
                {error}
            </div>
        );
    }

    if (!html) {
        return (
            <div className={`text-xs p-3 ${theme.label}`}>
                <i className="fa-solid fa-spinner fa-spin mr-2" />
                Loading Result…
            </div>
        );
    }

    return (
        <iframe
            ref={iframeRef}
            title={event.Title || 'Result'}
            className="w-full h-full min-h-[240px] border-0 bg-white"
            sandbox={sandbox}
            srcDoc={html}
        />
    );
};

export default UiRenderFrame;

export function hasUiHtml(event: DataRenderEvent | null | undefined): boolean {
    if (!event) return false;
    return !!(event.Html || event.HtmlPath);
}
