import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export function NoteSection(props: { action: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { action, onMarkChange } = props;

  if (!action) return null;

  return (
    <textarea
        value={action.Description ?? ''}
        onChange={(e) => {
          action.Description = e.target.value;
          onMarkChange();
        }}
        className={`w-full h-[100px] px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none`}
    />
  );
}

