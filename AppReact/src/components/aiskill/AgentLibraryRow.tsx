import React, { useState } from 'react';
import {
    agentSkillSetSvc,
    AppAgentLibraryToolDto,
    AppAgentMcpServerDto,
    AppAgentToolLibraryDto,
} from '../../webapi/agentSkillSetSvc';
import { Theme } from '../../redux/features/ui/theme/types';

interface Props {
    theme: Theme;
    library: AppAgentToolLibraryDto;
    subscribed: boolean;
    onToggle: (checked: boolean) => void;
    filesUi?: boolean;
    /** Tools this agent does not use, keyed "libraryKey\u0001toolName" */
    excluded: Set<string>;
    onToggleTool: (toolName: string, use: boolean) => void;
}

interface McpState {
    server: AppAgentMcpServerDto;
    loading: boolean;
    tools: string[] | null;
    error: string | null;
}

// One library row in the agent's Tools tab: checkbox to subscribe + [+]/[-] to see the tools
// the agent would get from this library, with a checkbox per tool to exclude it for this agent.
const AgentLibraryRow: React.FC<Props> = ({ theme, library, subscribed, onToggle, filesUi, excluded, onToggleTool }) => {
    const [open, setOpen] = useState(false);
    const [loading, setLoading] = useState(false);
    const [loaded, setLoaded] = useState(false);
    const [tools, setTools] = useState<AppAgentLibraryToolDto[]>([]);
    const [servers, setServers] = useState<McpState[]>([]);
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        setLoading(true); setError(null);
        try {
            const [toolRes, mcpRes] = await Promise.all([
                agentSkillSetSvc.GetLibraryTools(library.LibraryKey),
                agentSkillSetSvc.GetAllMcpServers(),
            ]);
            setTools(toolRes.Object ?? []);
            setServers((mcpRes.Object ?? [])
                .filter(s => s.SkillKey === library.LibraryKey)
                .map(server => ({ server, loading: false, tools: null, error: null })));
            setLoaded(true);
        } catch (e: unknown) { setError(e instanceof Error ? e.message : String(e)); }
        finally { setLoading(false); }
    };

    const toggleOpen = () => {
        const next = !open;
        setOpen(next);
        if (next && !loaded) load();
    };

    const patchServer = (id: number, patch: Partial<McpState>) =>
        setServers(prev => prev.map(s => (s.server.McpServerId === id ? { ...s, ...patch } : s)));

    const discover = async (server: AppAgentMcpServerDto) => {
        patchServer(server.McpServerId, { loading: true, error: null });
        try {
            const res = await agentSkillSetSvc.TestMcpServer(server);
            const r = res.Object;
            if (r?.Success) patchServer(server.McpServerId, { loading: false, tools: r.ToolNames });
            else patchServer(server.McpServerId, { loading: false, error: r?.Message ?? 'No response.' });
        } catch (e: unknown) {
            patchServer(server.McpServerId, { loading: false, error: e instanceof Error ? e.message : String(e) });
        }
    };

    // Mirrors the engine: an item is used when the agent subscribes to the library, the item itself is
    // active and the agent has not excluded it. (The library's own Active flag is not enforced at run time.)
    const isExcluded = (toolName: string) => excluded.has(`${library.LibraryKey}\u0001${toolName}`);
    const usedInfo = (toolName: string, toolActive: boolean): { used: boolean; tip: string } => {
        if (!subscribed) return { used: false, tip: 'Not used — library not subscribed' };
        if (!toolActive) return { used: false, tip: 'Not used — inactive in the library' };
        if (isExcluded(toolName)) return { used: false, tip: 'Not used — excluded for this agent (tick to use)' };
        return { used: true, tip: 'Used by this agent (untick to exclude)' };
    };
    const toolCheckbox = (toolName: string, toolActive: boolean) => (
        <input
            type="checkbox"
            className="mt-0.5"
            title={usedInfo(toolName, toolActive).tip}
            checked={usedInfo(toolName, toolActive).used}
            disabled={!subscribed || !toolActive}
            onChange={e => onToggleTool(toolName, e.target.checked)}
        />
    );

    const usedToolCount = tools.filter(t => usedInfo(t.ToolName, t.IsActive).used).length;
    const activeServerCount = servers.filter(s => s.server.IsActive).length;

    return (
        <div>
            <div className={`flex items-center gap-1 px-2 py-0.5 text-xs hover:opacity-80 ${theme.label}`}>
                <button
                    type="button"
                    className="w-4 shrink-0 opacity-70 hover:opacity-100"
                    title={open ? 'Hide tools' : 'Show tools in this library'}
                    onClick={toggleOpen}
                >
                    <i className={`fa-solid ${open ? 'fa-square-minus' : 'fa-square-plus'}`} />
                </button>
                <label className="flex items-center gap-2 flex-auto cursor-pointer">
                    <input type="checkbox" checked={subscribed} onChange={e => onToggle(e.target.checked)} />
                    <span className="font-mono font-semibold">{library.LibraryKey}</span>
                    <span className="opacity-70">{library.LibraryName && `— ${library.LibraryName}`}</span>
                    {filesUi && <span className="opacity-50">+ Files UI</span>}
                    {library.ToolCount > 0 && <span className="ml-auto opacity-50">({library.ToolCount})</span>}
                </label>
            </div>

            {open && (
                <div className={`ml-7 mb-1 pl-2 border-l border-gray-200 text-xs ${theme.label}`}>
                    {loading && <div className="py-0.5 opacity-60"><i className="fa-solid fa-spinner fa-spin mr-1" />Loading…</div>}
                    {error && <div className="py-0.5 text-red-600">{error}</div>}
                    {loaded && tools.length === 0 && servers.length === 0 && (
                        <div className="py-0.5 opacity-60">This library has no tools or MCP servers.</div>
                    )}

                    {tools.length > 0 && (
                        <div className="py-0.5">
                            <div className="opacity-60 mb-0.5">
                                Tools — {subscribed ? `${usedToolCount} of ${tools.length} used by this agent` : 'none used (not subscribed)'}
                            </div>
                            {tools.map(t => {
                                const info = usedInfo(t.ToolName, t.IsActive);
                                return (
                                    <div key={t.Id} className="flex items-start gap-1.5 py-px" title={info.tip}>
                                        {toolCheckbox(t.ToolName, t.IsActive)}
                                        <span className={`font-mono ${info.used ? '' : 'opacity-50'}`}>{t.ToolName}</span>
                                        {t.Description && <span className="opacity-50 truncate">— {t.Description}</span>}
                                    </div>
                                );
                            })}
                        </div>
                    )}

                    {servers.length > 0 && (
                        <div className="py-0.5">
                            <div className="opacity-60 mb-0.5">
                                MCP servers — {subscribed ? `${activeServerCount} of ${servers.length} connected for this agent` : 'none used (not subscribed)'}
                            </div>
                            {servers.map(s => {
                                const serverUsed = subscribed && s.server.IsActive;
                                return (
                                    <div key={s.server.McpServerId} className="py-px">
                                        <div className="flex items-center gap-1.5" title={serverUsed ? 'Connected for this agent' : 'Not used — server inactive or library not subscribed'}>
                                            <i className={`fa-solid fa-plug ${serverUsed ? 'text-green-600' : 'opacity-40'}`} />
                                            <span className="font-mono">{s.server.ServerName}</span>
                                            <span className="opacity-50 truncate">{s.server.ServerUrl}</span>
                                            {s.server.ServerType === 'streamable-http' && (
                                                <button type="button" className="ml-auto underline opacity-70 hover:opacity-100 shrink-0" disabled={s.loading} onClick={() => discover(s.server)}>
                                                    {s.loading ? 'Loading…' : s.tools ? 'Reload tools' : 'Show tools'}
                                                </button>
                                            )}
                                        </div>
                                        {s.error && <div className="ml-5 text-red-600">{s.error}</div>}
                                        {s.tools && (
                                            <div className="ml-5">
                                                <div className="opacity-60">
                                                    {s.tools.filter(n => usedInfo(n, s.server.IsActive).used).length} of {s.tools.length} tools used
                                                </div>
                                                {s.tools.map(name => {
                                                    const info = usedInfo(name, s.server.IsActive);
                                                    return (
                                                        <div key={name} className="flex items-center gap-1.5 py-px" title={info.tip}>
                                                            {toolCheckbox(name, s.server.IsActive)}
                                                            <span className={`font-mono ${info.used ? '' : 'opacity-50'}`}>{name}</span>
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                        )}
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};

export default AgentLibraryRow;
