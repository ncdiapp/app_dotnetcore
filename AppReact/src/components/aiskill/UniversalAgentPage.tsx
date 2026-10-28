import React from 'react';
import AgentChatManagement from './AgentChatManagement';
import { GENERAL_AGENT_KEY, GENERAL_AGENT_ROUTE } from './generalAgent';

const UniversalAgentPage: React.FC = () => (
    <div className="w-full h-full flex flex-col overflow-hidden">
        <AgentChatManagement
            key={GENERAL_AGENT_KEY}
            skillKey={GENERAL_AGENT_KEY}
            routeBasePath={GENERAL_AGENT_ROUTE}
            includeSkillQuery={false}
        />
    </div>
);

export default UniversalAgentPage;

