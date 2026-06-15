import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export function IntegrationConfigSection(props: { action: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { action, onMarkChange } = props;

  if (!action) return null;
  const actionType = Number(action.ActionType);
  const show = [83, 84, 85, 86].includes(actionType);
  if (!show) return null;

  return (
    <textarea
        value={action.ActionAttribute?.IntegrationConfigJson ?? ''}
        onChange={(e) => {
          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
          action.ActionAttribute.IntegrationConfigJson = e.target.value;
          onMarkChange();
        }}
        rows={8}
        className={`w-full font-mono text-xs px-2 py-1 border ${theme.inputBox} focus:outline-none`}
    />
  );
}

