import React, { useCallback, useEffect, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { agentMemorySvc, AgentMemoryEpisode, AgentMemoryFact } from '../../webapi/agentMemorySvc';

interface Props {
    skillKey: string;
    /** Current chat (AppGenericAgentSession.SessionKey); enables the "don't remember this chat" switch. */
    sessionKey?: string | null;
}

const fmtDate = (s: string) => (s ? s.substring(0, 10) : '');

const AgentMemoryPanel: React.FC<Props> = ({ skillKey, sessionKey }) => {
    const { theme } = useTheme();
    const [facts, setFacts] = useState<AgentMemoryFact[]>([]);
    const [episodes, setEpisodes] = useState<AgentMemoryEpisode[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [editId, setEditId] = useState<number | null>(null);
    const [editValue, setEditValue] = useState('');
    const [optOut, setOptOut] = useState(false);

    const btn = `px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`;
    const iconBtn = `w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`;

    const refresh = useCallback(async () => {
        if (!skillKey) return;
        setLoading(true);
        setError(null);
        try {
            const [f, e] = await Promise.all([agentMemorySvc.ListFacts(skillKey), agentMemorySvc.ListEpisodes(skillKey)]);
            setFacts(f);
            setEpisodes(e);
        } catch (err: unknown) {
            setError(err instanceof Error ? err.message : String(err));
        } finally {
            setLoading(false);
        }
    }, [skillKey]);

    useEffect(() => {
        void refresh();
    }, [refresh]);

    const run = async (action: () => Promise<boolean>) => {
        setError(null);
        try {
            await action();
            await refresh();
        } catch (err: unknown) {
            setError(err instanceof Error ? err.message : String(err));
        }
    };

    const saveEdit = async (f: AgentMemoryFact) => {
        const value = editValue.trim();
        if (!value) return;
        await run(() => agentMemorySvc.UpdateFact(f.FactId, value));
        setEditId(null);
    };

    const toggleOptOut = async (checked: boolean) => {
        if (!sessionKey) return;
        setOptOut(checked);
        try {
            await agentMemorySvc.SetChatMemoryOptOut(skillKey, sessionKey, checked);
        } catch (err: unknown) {
            setOptOut(!checked);
            setError(err instanceof Error ? err.message : String(err));
        }
    };

    const forgetAll = async () => {
        if (!window.confirm('Delete everything the assistant remembers about you (all saved facts and past conversation summaries)?')) return;
        await run(() => agentMemorySvc.ForgetEverything());
    };

    const pending = facts.filter(f => f.Status === 'pending');
    const confirmed = facts.filter(f => f.Status !== 'pending');

    const factRow = (f: AgentMemoryFact) => (
        <div key={f.FactId} className={`flex items-start gap-2 px-2 py-1.5 border-b text-xs ${theme.label}`}>
            <div className="w-1 flex-auto min-w-0">
                <div className="font-semibold break-words">{f.FactKey}</div>
                {editId === f.FactId ? (
                    <input
                        className={`w-full h-7 px-2 mt-1 text-xs border ${theme.inputBox} focus:outline-none`}
                        autoComplete="off"
                        maxLength={500}
                        value={editValue}
                        onChange={e => setEditValue(e.target.value)}
                        onKeyDown={e => { if (e.key === 'Enter') void saveEdit(f); if (e.key === 'Escape') setEditId(null); }}
                    />
                ) : (
                    <div className="opacity-80 break-words">{f.FactValue}</div>
                )}
                <div className="opacity-50">
                    {f.Category} · {f.Source} · {fmtDate(f.UpdatedAt)}{f.SkillKey ? '' : ' · all agents'}
                </div>
            </div>
            {editId === f.FactId ? (
                <>
                    <button className={iconBtn} title="Save" onClick={() => void saveEdit(f)}><i className="fa-solid fa-floppy-disk" /></button>
                    <button className={iconBtn} title="Cancel" onClick={() => setEditId(null)}><i className="fa-solid fa-xmark" /></button>
                </>
            ) : (
                <>
                    {f.Status === 'pending' && (
                        <button className={iconBtn} title="Confirm: remember this" onClick={() => void run(() => agentMemorySvc.ConfirmFact(f.FactId))}>
                            <i className="fa-solid fa-check" />
                        </button>
                    )}
                    <button className={iconBtn} title="Edit" onClick={() => { setEditId(f.FactId); setEditValue(f.FactValue); }}>
                        <i className="fa-solid fa-pencil" />
                    </button>
                    <button className={iconBtn} title="Delete" onClick={() => void run(() => agentMemorySvc.DeleteFact(f.FactId))}>
                        <i className="fa-solid fa-trash" />
                    </button>
                </>
            )}
        </div>
    );

    return (
        <div className="w-full h-full flex flex-col min-h-0">
            <div className="flex items-center gap-2 px-2 py-1.5">
                <span className={`text-xs font-semibold ${theme.title}`}>What the assistant remembers</span>
                <button className={`${iconBtn} ml-auto`} title="Refresh" onClick={() => void refresh()}>
                    <i className={`fa-solid fa-rotate${loading ? ' fa-spin' : ''}`} />
                </button>
            </div>
            {sessionKey && (
                <label className={`flex items-center gap-2 px-2 pb-1.5 text-xs ${theme.label}`}>
                    <input type="checkbox" checked={optOut} onChange={e => void toggleOptOut(e.target.checked)} />
                    Don&apos;t remember this chat
                </label>
            )}
            {error && <div className={`px-2 pb-1 text-xs ${theme.label}`}>{error}</div>}

            <div className="w-full h-1 flex-auto overflow-y-auto">
                {pending.length > 0 && (
                    <>
                        <div className={`px-2 py-1 text-xs font-semibold ${theme.label}`}>Suggested — confirm to remember</div>
                        {pending.map(factRow)}
                    </>
                )}
                <div className={`px-2 py-1 text-xs font-semibold ${theme.label}`}>Saved facts</div>
                {confirmed.length === 0 && <div className={`px-2 py-1 text-xs opacity-60 ${theme.label}`}>Nothing saved yet.</div>}
                {confirmed.map(factRow)}

                <div className={`px-2 pt-3 pb-1 text-xs font-semibold ${theme.label}`}>Past conversations</div>
                {episodes.length === 0 && <div className={`px-2 py-1 text-xs opacity-60 ${theme.label}`}>No conversations remembered yet.</div>}
                {episodes.map(e => (
                    <div key={e.EpisodeId} className={`flex items-start gap-2 px-2 py-1.5 border-b text-xs ${theme.label}`}>
                        <div className="w-1 flex-auto min-w-0">
                            <div className="opacity-50">{fmtDate(e.CreatedAt)}{e.Topics ? ` · ${e.Topics}` : ''}</div>
                            <div className="break-words">{e.Summary}</div>
                        </div>
                        <button className={iconBtn} title="Delete" onClick={() => void run(() => agentMemorySvc.DeleteEpisode(e.EpisodeId))}>
                            <i className="fa-solid fa-trash" />
                        </button>
                    </div>
                ))}
            </div>

            <div className="px-2 py-1.5">
                <button className={btn} onClick={() => void forgetAll()}>
                    <i className="fa-solid fa-eraser mr-1" />Forget everything
                </button>
            </div>
        </div>
    );
};

export default AgentMemoryPanel;
