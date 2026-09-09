import React from 'react';
import AgentUiChatHost from './AgentUiChatHost';

/** Standalone full-page chat for a dynamic agent reached via the sidebar. */
const AgentChatPage: React.FC = () => {
    const params = new URLSearchParams(window.location.search);
    const skillKey = params.get('skillKey') ?? '';
    const agentUi  = parseInt(params.get('agentUi') ?? '1', 10);

    if (!skillKey) {
        return (
            <div className="w-full h-full flex items-center justify-center text-sm opacity-50">
                Agent not found.
            </div>
        );
    }

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            <AgentUiChatHost skillKey={skillKey} agentUi={agentUi} />
        </div>
    );
};

export default AgentChatPage;
