import React, { useEffect, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { genericAgentSvc } from '../../webapi/genericAgentSvc';

interface ChatMessage {
    role: 'user' | 'assistant';
    content: string;
    isStreaming?: boolean;
}

interface StepIndicator {
    type: string;
    toolName?: string;
    description: string;
    isSuccess: boolean;
}

interface PlanEvent {
    PlanSummary: string;
}

interface Props {
    skillKey: string;
}

const GenericAgentChat: React.FC<Props> = ({ skillKey }) => {
    const { theme } = useTheme();
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [steps, setSteps] = useState<StepIndicator[]>([]);
    const [input, setInput] = useState('');
    const [isRunning, setIsRunning] = useState(false);
    const [pendingPlan, setPendingPlan] = useState<PlanEvent | null>(null);
    const [sessionId, setSessionId] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const bottomRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, steps]);

    useEffect(() => {
        return () => { genericAgentSvc.disconnect(); };
    }, []);

    const handleSend = async () => {
        const msg = input.trim();
        if (!msg || isRunning) return;
        setInput('');
        setError(null);
        setSteps([]);
        setIsRunning(true);
        setMessages(prev => [...prev, { role: 'user', content: msg }]);

        try {
            // Build history from prior turns so the LLM has full context.
            const history = messages
                .filter(m => !m.isStreaming)
                .map(m => ({ role: m.role === 'user' ? 'user' : 'assistant', content: m.content }));

            const sid = await genericAgentSvc.RunAgent({ SkillKey: skillKey, UserMessage: msg, SessionId: sessionId ?? undefined, Messages: history }, {
                onToken: (token) => {
                    setMessages(prev => {
                        const last = prev[prev.length - 1];
                        if (last?.role === 'assistant' && last.isStreaming) {
                            return [...prev.slice(0, -1), { ...last, content: last.content + token }];
                        }
                        return [...prev, { role: 'assistant', content: token, isStreaming: true }];
                    });
                },
                onStep: (step) => {
                    setSteps(prev => [...prev, {
                        type: step.Type, toolName: step.ToolName, description: step.Description, isSuccess: step.IsSuccess,
                    }]);
                },
                onPlan:  (plan) => setPendingPlan(plan),
                onDone:  (done) => {
                    setMessages(prev => {
                        const last = prev[prev.length - 1];
                        if (last?.role === 'assistant' && last.isStreaming) {
                            return [...prev.slice(0, -1), { ...last, content: done.FinalResponse || last.content, isStreaming: false }];
                        }
                        if (done.FinalResponse) return [...prev, { role: 'assistant', content: done.FinalResponse }];
                        return prev;
                    });
                    setIsRunning(false);
                },
                onError: (m) => { setError(m); setIsRunning(false); },
            });
            setSessionId(sid);
        } catch (e: unknown) {
            setError(e instanceof Error ? e.message : String(e));
            setIsRunning(false);
        }
    };

    const handleConfirmPlan = async (confirmed: boolean) => {
        setPendingPlan(null);
        if (sessionId) await genericAgentSvc.ConfirmPlan(sessionId, confirmed);
    };

    const btn = `px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`;

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            {/* Messages */}
            <div className="w-full h-1 flex-auto overflow-auto p-3 flex flex-col gap-2">
                {messages.map((m, i) => (
                    <div key={i} className={`flex ${m.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                        <div className={`max-w-2xl px-3 py-2 rounded-lg text-xs whitespace-pre-wrap ${m.role === 'user' ? `${theme.button_default} ml-8` : `${theme.mainContentSection} mr-8`}`}>
                            {m.content}
                            {m.isStreaming && <span className="animate-pulse ml-1">|</span>}
                        </div>
                    </div>
                ))}

                {/* Step indicators */}
                {isRunning && steps.length > 0 && (
                    <div className={`mx-2 p-2 rounded text-xs ${theme.mainContentSection} flex flex-col gap-1`}>
                        {steps.slice(-3).map((s, i) => (
                            <div key={i} className={`flex items-center gap-2 ${theme.label}`}>
                                <i className={`fa-solid ${s.isSuccess ? 'fa-circle-check text-green-500' : 'fa-circle-xmark text-red-500'}`} />
                                {s.toolName && <span className="font-mono font-semibold">{s.toolName}</span>}
                                <span>{s.description}</span>
                            </div>
                        ))}
                        {!steps.some(s => s.description) && (
                            <div className={`flex items-center gap-2 ${theme.label}`}>
                                <i className="fa-solid fa-spinner fa-spin" />
                                <span>Running...</span>
                            </div>
                        )}
                    </div>
                )}

                {isRunning && steps.length === 0 && (
                    <div className={`flex items-center gap-2 mx-2 text-xs ${theme.label}`}>
                        <i className="fa-solid fa-spinner fa-spin" />
                        <span>Thinking...</span>
                    </div>
                )}

                {/* Plan confirmation */}
                {pendingPlan && (
                    <div className={`mx-2 p-3 rounded border ${theme.mainContentSection} flex flex-col gap-2`}>
                        <div className={`text-xs font-semibold ${theme.title}`}>Plan Review</div>
                        <div className={`text-xs ${theme.label} whitespace-pre-wrap`}>{pendingPlan.PlanSummary}</div>
                        <div className="flex gap-2">
                            <button className={btn} onClick={() => handleConfirmPlan(true)}><i className="fa-solid fa-check mr-1" />Approve</button>
                            <button className={btn} onClick={() => handleConfirmPlan(false)}><i className="fa-solid fa-xmark mr-1" />Reject</button>
                        </div>
                    </div>
                )}

                {error && (
                    <div className="mx-2 px-3 py-2 text-xs text-red-600 bg-red-50 border border-red-200 rounded">
                        {error}<button className="ml-2 font-bold" onClick={() => setError(null)}>x</button>
                    </div>
                )}
                <div ref={bottomRef} />
            </div>

            {/* Input bar */}
            <div className={`flex items-end gap-2 px-3 py-2 border-t border-gray-200 ${theme.mainContentSection}`}>
                <textarea
                    className={`flex-auto px-2 py-1 text-xs border rounded-[4px] ${theme.inputBox} focus:outline-none resize-none`}
                    rows={2}
                    value={input}
                    placeholder={`Message ${skillKey || 'agent'}...`}
                    onChange={e => setInput(e.target.value)}
                    onKeyDown={e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); handleSend(); } }}
                    disabled={isRunning}
                />
                <button className={btn} onClick={handleSend} disabled={isRunning || !input.trim()}>
                    {isRunning ? <i className="fa-solid fa-spinner fa-spin" /> : <i className="fa-solid fa-paper-plane" />}
                </button>
                {messages.length > 0 && (
                    <button className={btn} onClick={() => { setMessages([]); setSteps([]); setSessionId(null); setError(null); }}>
                        <i className="fa-solid fa-rotate-left" />
                    </button>
                )}
            </div>
        </div>
    );
};

export default GenericAgentChat;
