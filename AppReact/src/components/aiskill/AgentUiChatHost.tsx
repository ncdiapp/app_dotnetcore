import React from 'react';
import GenericAgentChat from './GenericAgentChat';

interface Props {
  skillKey: string;
  testMode?: boolean;
  /** AppGenericAgentSession.SessionKey. Sidebar Chat Management passes a GUID; Agent Management Run omits it. */
  chatSessionKey?: string | null;
  chatTitle?: string | null;
  showChatListButton?: boolean;
  onOpenChatList?: () => void;
  onConversationChanged?: () => void;
}

/** Always Generic Chat. Extra panels come from subscribed tool libraries (see agentUiModules). */
const AgentUiChatHost: React.FC<Props> = ({
  skillKey,
  testMode,
  chatSessionKey,
  chatTitle,
  showChatListButton,
  onOpenChatList,
  onConversationChanged,
}) => (
  <GenericAgentChat
    skillKey={skillKey}
    testMode={testMode}
    chatSessionKey={chatSessionKey}
    chatTitle={chatTitle}
    showChatListButton={showChatListButton}
    onOpenChatList={onOpenChatList}
    onConversationChanged={onConversationChanged}
  />
);

export default AgentUiChatHost;
