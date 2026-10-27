import React from 'react';
import AgentChatManagement from './AgentChatManagement';
import { UNIVERSAL_AGENT_KEY, UNIVERSAL_AGENT_ROUTE } from './universalAgent';

const UniversalAgentPage: React.FC = () => (
    <div className="w-full h-full flex flex-col overflow-hidden">
        <AgentChatManagement
            key={UNIVERSAL_AGENT_KEY}
            skillKey={UNIVERSAL_AGENT_KEY}
            routeBasePath={UNIVERSAL_AGENT_ROUTE}
            includeSkillQuery={false}
        />
    </div>
);

export default UniversalAgentPage;

