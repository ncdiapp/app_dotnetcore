import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { endpoints } from '../../webapi/endpoints';
import { genericAgentSvc, type DataRenderEvent } from '../../webapi/genericAgentSvc';

interface Props {
    event: DataRenderEvent;
    skillKey: string;
    sessionKey: string | null | undefined;
    disabled?: boolean;
    onAction?: (actionId: string, selectionJson?: string) => void;
}

function resolveHtmlUrl(url: string | null | undefined): string | null {
    if (!url || !url.trim()) return null;
    const u = url.trim();
    if (/^https?:\/\//i.test(u)) return u;
    const base = (endpoints.BASE_URL || '').replace(/\/$/, '');
    if (u.startsWith('/')) return `${base}${u}`;
    return `${base}/${u}`;
}

/**
 * None-interactive / chart Result: TemplateContent HTML in iframe.
 * Interactive table/cards/page/kpi/dashboard use React hosts (not this frame).
 */
const UiRenderFrame: React.FC<Props> = ({ event, skillKey, sessionKey, disabled, onAction }) => {
    const { theme } = useTheme();
    const iframeRef = useRef<HTMLIFrameElement>(null);
    const [frameSrc, setFrameSrc] = useState<string | null>(null);
    const [srcDoc, setSrcDoc] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        let blobUrl: string | null = null;
        setError(null);
        setFrameSrc(null);
        setSrcDoc(null);

        if (event.HtmlPath && sessionKey) {
            void (async () => {
                try {
                    const res = await genericAgentSvc.ReadAgentFile(skillKey, sessionKey, event.HtmlPath!);
                    if (cancelled) return;
                    if (res?.Content) {
                        blobUrl = URL.createObjectURL(new Blob([res.Content], { type: 'text/html;charset=utf-8' }));
                        setFrameSrc(blobUrl);
                    } else setError('Could not load Result HTML file.');
                } catch {
                    if (!cancelled) setError('Could not load Result HTML file.');
                }
            })();
            return () => {
                cancelled = true;
                if (blobUrl) URL.revokeObjectURL(blobUrl);
            };
        }

        if (event.Html) {
            setSrcDoc(event.Html);
            return () => { cancelled = true; };
        }

        const htmlUrl = resolveHtmlUrl(event.HtmlUrl);
        if (htmlUrl) {
            setFrameSrc(htmlUrl);
            return () => { cancelled = true; };
        }

        setError('Re-run to view in Result (no HTML on this message).');
        return () => { cancelled = true; };
    }, [event.Html, event.HtmlPath, event.HtmlUrl, skillKey, sessionKey]);

    useEffect(() => {
        return () => {
            if (frameSrc && frameSrc.startsWith('blob:')) URL.revokeObjectURL(frameSrc);
        };
    }, [frameSrc]);

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

    if (error) {
        return (
            <div className={`text-xs p-3 rounded border ${theme.label}`}>
                {error}
            </div>
        );
    }

    if (!frameSrc && !srcDoc) {
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
            className="w-full h-full min-h-[240px] min-w-0 max-w-full border-0 bg-white"
            style={{ width: '100%', maxWidth: '100%' }}
            sandbox="allow-scripts allow-same-origin allow-popups"
            {...(srcDoc ? { srcDoc } : { src: frameSrc || undefined })}
        />
    );
};

export default UiRenderFrame;

export function hasUiHtml(event: DataRenderEvent | null | undefined): boolean {
    if (!event) return false;
    return !!(event.Html || event.HtmlPath || event.HtmlUrl);
}
