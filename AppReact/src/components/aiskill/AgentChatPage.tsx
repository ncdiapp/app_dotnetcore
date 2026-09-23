import React from 'react';
import { useSearchParams } from 'react-router-dom';
import AgentChatManagement from './AgentChatManagement';

/** Sidebar entry: Agent Chat Management (list + current chat). */
const AgentChatPage: React.FC = () => {
    const [searchParams] = useSearchParams();
    const skillKey = searchParams.get('skillKey') ?? '';

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
