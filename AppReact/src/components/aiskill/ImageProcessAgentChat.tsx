import React from 'react';
import AgentUiStubChat from './AgentUiStubChat';

interface Props {
  skillKey: string;
}

const ImageProcessAgentChat: React.FC<Props> = ({ skillKey }) => (
  <AgentUiStubChat skillKey={skillKey} title="Image and File Process Agent Chat" />
);

export default ImageProcessAgentChat;
