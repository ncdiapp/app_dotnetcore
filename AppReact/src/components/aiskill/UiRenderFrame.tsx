import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { endpoints } from '../../webapi/endpoints';
import { genericAgentSvc, type DataRenderEvent } from '../../webapi/genericAgentSvc';
import { WIJMO_LICENSE_KEY } from '../../wijmoLicenseKey';

/** Ensure iframe HTML applies the same Wijmo key as the main SPA, immediately after core script. */
function ensureWijmoLicenseInHtml(raw: string): string {
    if (!raw || !WIJMO_LICENSE_KEY) return raw;
    const keyLit = JSON.stringify(WIJMO_LICENSE_KEY);
    const inject =
        `<script>(function(){try{var k=${keyLit};` +
        `window.__WIJMO_LICENSE_KEY__=k;` +
        `function apply(){if(window.wijmo&&wijmo.setLicenseKey)wijmo.setLicenseKey(k);}` +
        `apply();` +
        `})();</script>`;
    // Prefer right after core wijmo.min.js (before grid/chart scripts)
    const coreRe = /(<script[^>]+wijmo\.min\.js[^>]*><\/script>)/i;
    if (coreRe.test(raw)) {
        return raw.replace(coreRe, `$1${inject}`);
    }
    if (/<\/head>/i.test(raw)) {
        return raw.replace(/<\/head>/i, `${inject}</head>`);
    }
    return inject + raw;
}

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

/** iframe src= navigation cannot send CurrentUserSessionId header → 401 → SPA Home. Only use src when token is present. */
function htmlUrlHasToken(url: string | null | undefined): boolean {
    if (!url) return false;
    return /[?&]token=/.test(url);
}

/**
 * Result panel host:
 * - Interactive KPI/dashboard: fetch session HTML with auth headers, srcDoc (Wijmo CDN scripts OK)
 * - Optional HtmlUrl+token: iframe src (print-like)
 * - None-interactive: srcDoc for C#-merged SVG HTML
 */
const UiRenderFrame: React.FC<Props> = ({ event, skillKey, sessionKey, disabled, onAction }) => {
    const { theme } = useTheme();
    const iframeRef = useRef<HTMLIFrameElement>(null);
    const [html, setHtml] = useState<string | null>(event.Html ?? null);
    const [error, setError] = useState<string | null>(null);
    const interactive = (event.Mode || '').toLowerCase() === 'interactive';
    const htmlUrl = resolveHtmlUrl(event.HtmlUrl);
    const useIframeSrc = htmlUrlHasToken(htmlUrl);

    useEffect(() => {
        let cancelled = false;
        setError(null);

        // Tokenized URL → real navigation (cookies/token query only)
        if (useIframeSrc && htmlUrl) {
            setHtml(null);
            return;
        }

        // Prefer authenticated file read (interactive pages write HtmlPath; headers work here)
        if (event.HtmlPath && sessionKey) {
            setHtml(null);
            void (async () => {
                try {
                    const res = await genericAgentSvc.ReadAgentFile(skillKey, sessionKey, event.HtmlPath!);
                    if (cancelled) return;
                    if (res?.Content) setHtml(ensureWijmoLicenseInHtml(res.Content));
                    else setError('Could not load Result HTML file.');
                } catch {
                    if (!cancelled) setError('Could not load Result HTML file.');
                }
            })();
            return () => { cancelled = true; };
        }

        if (event.Html) {
            setHtml(ensureWijmoLicenseInHtml(event.Html));
            return;
        }

        setHtml(null);
        setError('Re-run to view in Result (no HTML on this message).');
        return () => { cancelled = true; };
    }, [event.Html, event.HtmlPath, event.HtmlUrl, event.RenderId, skillKey, sessionKey, useIframeSrc, htmlUrl]);

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

    // Interactive / CDN pages need scripts; same-origin for postMessage
    const sandbox = interactive || !!event.HtmlPath || useIframeSrc
        ? 'allow-scripts allow-same-origin allow-popups'
        : 'allow-same-origin';

    if (error) {
        return (
            <div className={`text-xs p-3 rounded border ${theme.label}`}>
                {error}
            </div>
        );
    }

    if (useIframeSrc && htmlUrl) {
        return (
            <iframe
                ref={iframeRef}
                title={event.Title || 'Result'}
                className="w-full h-full min-h-[240px] border-0 bg-white"
                sandbox={sandbox}
                src={htmlUrl}
            />
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
    return !!(event.Html || event.HtmlPath || event.HtmlUrl);
}
