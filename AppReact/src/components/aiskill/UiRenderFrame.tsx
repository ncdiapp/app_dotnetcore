import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { endpoints } from '../../webapi/endpoints';
import { genericAgentSvc, type DataRenderEvent } from '../../webapi/genericAgentSvc';
import { WIJMO_LICENSE_KEY } from '../../wijmoLicenseKey';

/**
 * Ensure iframe HTML applies the same Wijmo key as the main SPA, immediately after core script
 * and again before FlexGrid boot.
 */
function ensureWijmoLicenseInHtml(raw: string): string {
    if (!raw || !WIJMO_LICENSE_KEY) return raw;
    if (!/wijmo/i.test(raw)) return raw;
    const keyLit = JSON.stringify(WIJMO_LICENSE_KEY);
    const applyFn =
        `function __agentApplyWijmoLicense(k){var w=window.wijmo;if(!w||!k)return;` +
        `if(typeof w.setLicenseKey==='function')w.setLicenseKey(k);` +
        `else if(w.Control&&typeof w.Control.setLicenseKey==='function')w.Control.setLicenseKey(k);}`;
    const inject =
        `<script>(function(){try{var k=${keyLit};window.__WIJMO_LICENSE_KEY__=k;${applyFn};__agentApplyWijmoLicense(k);}` +
        `catch(e){}})();</script>`;

    let html = raw;
    html = html.replace(/window\.__WIJMO_LICENSE_KEY__\s*=\s*(?:[^;]|\\;)+;/g, `window.__WIJMO_LICENSE_KEY__=${keyLit};`);

    const coreRe = /(<script[^>]+wijmo\.min\.js[^>]*><\/script>)/i;
    if (coreRe.test(html)) {
        html = html.replace(coreRe, `$1${inject}`);
    } else if (/<\/head>/i.test(html)) {
        html = html.replace(/<\/head>/i, `${inject}</head>`);
    } else {
        html = inject + html;
    }

    const reapply =
        `<script>(function(){try{var k=window.__WIJMO_LICENSE_KEY__||${keyLit};` +
        `${applyFn};__agentApplyWijmoLicense(k);}catch(e){}})();</script>`;
    if (/window\.__AGENT_UI__\s*=/.test(html)) {
        html = html.replace(/<script>\s*window\.__AGENT_UI__\s*=/, `${reapply}<script>window.__AGENT_UI__=`);
    }
    return html;
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

function htmlUrlHasToken(url: string | null | undefined): boolean {
    if (!url) return false;
    return /[?&]token=/.test(url);
}

/**
 * Result panel host:
 * - Interactive Wijmo HTML → mint token + iframe src on API host (license + filter popups).
 * - No sandbox on Wijmo pages (sandbox/srcdoc caused evaluation watermark and broken popups).
 */
const UiRenderFrame: React.FC<Props> = ({ event, skillKey, sessionKey, disabled, onAction }) => {
    const { theme } = useTheme();
    const iframeRef = useRef<HTMLIFrameElement>(null);
    const [frameSrc, setFrameSrc] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const interactive = (event.Mode || '').toLowerCase() === 'interactive';
    const htmlUrl = resolveHtmlUrl(event.HtmlUrl);
    const useTokenUrl = htmlUrlHasToken(htmlUrl);

    useEffect(() => {
        let cancelled = false;
        setError(null);
        setFrameSrc(null);

        if (useTokenUrl && htmlUrl) {
            setFrameSrc(htmlUrl);
            return;
        }

        const mountHtml = async (raw: string) => {
            const html = ensureWijmoLicenseInHtml(raw);
            if (/wijmo/i.test(html)) {
                try {
                    const issued = await genericAgentSvc.IssueAgentUiHtmlToken(html, event.RenderId, sessionKey);
                    if (cancelled) return;
                    if (issued?.HtmlUrl) {
                        setFrameSrc(issued.HtmlUrl);
                        return;
                    }
                } catch {
                    /* fall through to blob */
                }
            }
            if (cancelled) return;
            const blob = new Blob([html], { type: 'text/html;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            setFrameSrc(url);
        };

        if (event.HtmlPath && sessionKey) {
            void (async () => {
                try {
                    const res = await genericAgentSvc.ReadAgentFile(skillKey, sessionKey, event.HtmlPath!);
                    if (cancelled) return;
                    if (res?.Content) await mountHtml(res.Content);
                    else setError('Could not load Result HTML file.');
                } catch {
                    if (!cancelled) setError('Could not load Result HTML file.');
                }
            })();
            return () => { cancelled = true; };
        }

        if (event.Html) {
            void mountHtml(event.Html);
            return () => { cancelled = true; };
        }

        setError('Re-run to view in Result (no HTML on this message).');
        return () => { cancelled = true; };
    }, [event.Html, event.HtmlPath, event.HtmlUrl, event.RenderId, skillKey, sessionKey, useTokenUrl, htmlUrl]);

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

    if (!frameSrc) {
        return (
            <div className={`text-xs p-3 ${theme.label}`}>
                <i className="fa-solid fa-spinner fa-spin mr-2" />
                Loading Result…
            </div>
        );
    }

    // Wijmo interactive: no sandbox — license hostname + filter Popup need a normal browsing context.
    const isWijmo = /AgentUiInteractiveHtml|blob:/i.test(frameSrc) && interactive;
    return (
        <iframe
            ref={iframeRef}
            title={event.Title || 'Result'}
            className="w-full h-full min-h-[240px] min-w-0 max-w-full border-0 bg-white"
            style={{ width: '100%', maxWidth: '100%' }}
            {...(isWijmo ? {} : { sandbox: 'allow-scripts allow-same-origin allow-popups' })}
            src={frameSrc}
        />
    );
};

export default UiRenderFrame;

export function hasUiHtml(event: DataRenderEvent | null | undefined): boolean {
    if (!event) return false;
    return !!(event.Html || event.HtmlPath || event.HtmlUrl);
}
