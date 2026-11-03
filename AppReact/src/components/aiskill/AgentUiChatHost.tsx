import React from 'react';
import SharedAgentChat from './SharedAgentChat';

interface Props {
  skillKey: string;
  testMode?: boolean;
  /** AppGenericAgentSession.SessionKey. Sidebar Chat Management passes a GUID; Agent Management Run omits it. */
  chatSessionKey?: string | null;
  chatTitle?: string | null;
  showChatListButton?: boolean;
  onOpenChatList?: () => void;
  onConversationChanged?: () => void;
  autoRunPendingUser?: boolean;
  onAutoRunStarted?: () => void;
  getHostContext?: () => string | undefined;
  onHostTool?: (toolName: string, args: any) => Promise<unknown>;
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
  autoRunPendingUser,
  onAutoRunStarted,
  getHostContext,
  onHostTool,
}) => (
  <SharedAgentChat
    skillKey={skillKey}
    testMode={testMode}
    chatSessionKey={chatSessionKey}
    chatTitle={chatTitle}
    showChatListButton={showChatListButton}
    onOpenChatList={onOpenChatList}
    onConversationChanged={onConversationChanged}
    autoRunPendingUser={autoRunPendingUser}
    onAutoRunStarted={onAutoRunStarted}
    getHostContext={getHostContext}
    onHostTool={onHostTool}
  />
);

export default AgentUiChatHost;
