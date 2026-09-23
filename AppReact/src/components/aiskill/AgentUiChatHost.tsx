import React from 'react';
import GenericAgentChat from './GenericAgentChat';

interface Props {
  skillKey: string;
  testMode?: boolean;
  /** AppGenericAgentSession.SessionKey. Sidebar Chat Management passes a GUID; Agent Management Run omits it. */
  chatSessionKey?: string | null;
  onConversationChanged?: () => void;
}

/** Always Generic Chat. Extra panels come from subscribed tool libraries (see agentUiModules). */
const AgentUiChatHost: React.FC<Props> = ({ skillKey, testMode, chatSessionKey, onConversationChanged }) => (
  <GenericAgentChat
    skillKey={skillKey}
    testMode={testMode}
    chatSessionKey={chatSessionKey}
    onConversationChanged={onConversationChanged}
  />
);

export default AgentUiChatHost;
