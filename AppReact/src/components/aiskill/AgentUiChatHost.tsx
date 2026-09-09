import React from 'react';
import GenericAgentChat from './GenericAgentChat';
import ConfigurationAndIntegrationAgentChat from './ConfigurationAndIntegrationAgentChat';
import DbManagementAgentChat from './DbManagementAgentChat';
import ImageProcessAgentChat from './ImageProcessAgentChat';
import { EmAppAgentUi, resolveAgentUi } from './agentUiTypes';

interface Props {
  skillKey: string;
  agentUi?: number | null;
  testMode?: boolean;
}

/** Pick Agent chat shell by AgentUi (Run preview / later published instance). */
const AgentUiChatHost: React.FC<Props> = ({ skillKey, agentUi, testMode }) => {
  const ui = resolveAgentUi(agentUi);
  switch (ui) {
    case EmAppAgentUi.ConfigurationAndIntegration:
      return <ConfigurationAndIntegrationAgentChat skillKey={skillKey} />;
    case EmAppAgentUi.DbManagement:
      return <DbManagementAgentChat skillKey={skillKey} />;
    case EmAppAgentUi.ImageAndFileProcess:
      return <ImageProcessAgentChat skillKey={skillKey} />;
    case EmAppAgentUi.GenericChat:
    default:
      return <GenericAgentChat skillKey={skillKey} testMode={testMode} />;
  }
};

export default AgentUiChatHost;
