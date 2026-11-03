import React from 'react';
import { useSearchParams } from 'react-router-dom';
import AgentChatManagement from './AgentChatManagement';
import { FORM_AGENT_SKILL_KEY } from '../formMgt/FormAgent/formAgentHost';

/** Sidebar entry: Agent Chat Management (list + current chat). */
const AgentChatPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const skillKey = searchParams.get('skillKey') ?? '';

    if (skillKey.trim().toLowerCase() === FORM_AGENT_SKILL_KEY) {
        return (
            <div className="w-full h-full flex items-center justify-center text-sm opacity-70 px-6 text-center">
                App Form Agent runs inside a transaction form. Open a Master Detail or List Edit form that has AI Agent enabled.
            </div>
        );
    }

    if (!skillKey) {
        return (
            <div className="w-full h-full flex items-center justify-center text-sm opacity-50">
                Agent not found.
            </div>
        );
    }

    return (
        <div className="w-full h-full flex flex-col overflow-hidden">
            <AgentChatManagement key={skillKey} skillKey={skillKey} />
        </div>
    );
};

export default AgentChatPage;
