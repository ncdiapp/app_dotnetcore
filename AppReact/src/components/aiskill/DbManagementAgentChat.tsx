import React from 'react';
import AgentUiStubChat from './AgentUiStubChat';

interface Props {
  skillKey: string;
}

const DbManagementAgentChat: React.FC<Props> = ({ skillKey }) => (
  <AgentUiStubChat skillKey={skillKey} title="DB Management Agent Chat" />
);

export default DbManagementAgentChat;
