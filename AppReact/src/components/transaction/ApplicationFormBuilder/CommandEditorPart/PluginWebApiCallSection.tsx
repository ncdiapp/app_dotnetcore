import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export const EmAppTransactionCommandTypePluginWebApiCall = 40;

export function PluginWebApiCallSection(props: { action: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { action, onMarkChange } = props;

  if (!action) return null;
  if (Number(action.ActionType) !== EmAppTransactionCommandTypePluginWebApiCall) return null;

  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
      <label className={`text-xs ${theme.label}`}>API Method</label>
      <input
        type="text"
        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        value={action?.ActionAttribute?.WebApiMethodName ?? ''}
        onChange={(e) => {
          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
          action.ActionAttribute.WebApiMethodName = e.target.value;
          onMarkChange();
        }}
      />
    </div>
  );
}

