import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

interface Props {
  skillKey: string;
  title: string;
}

/** Placeholder shell for Agent UI types not implemented yet. */
const AgentUiStubChat: React.FC<Props> = ({ skillKey, title }) => {
  const { theme } = useTheme();
  return (
    <div className={`w-full h-full flex flex-col items-center justify-center gap-2 px-4 ${theme.mainContentSection}`}>
      <div className={`text-sm font-semibold ${theme.title}`}>{title}</div>
      <div className={`text-xs ${theme.label}`}>Skill: {skillKey || '(none)'}</div>
      <div className={`text-xs ${theme.label}`}>This Agent UI is not implemented yet.</div>
    </div>
  );
};

export default AgentUiStubChat;
