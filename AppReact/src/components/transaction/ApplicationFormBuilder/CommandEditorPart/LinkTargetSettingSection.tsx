import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

const EmAppTransactionCommandTypeOpenFormCreationWindow = 51;
const EmAppTransactionCommandTypeOpenFormEditWindow = 55;

export function LinkTargetSettingSection(props: { action: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { action, onMarkChange } = props;

  if (!action) return null;
  const type = Number(action.ActionType);
  if (type !== EmAppTransactionCommandTypeOpenFormCreationWindow && type !== EmAppTransactionCommandTypeOpenFormEditWindow) return null;

  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Link Target Setting Id</label>
        <div className="w-72">
          <input
            type="number"
            value={action.ActionAttribute?.LinkTargetId ?? ''}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute.LinkTargetId = e.target.value === '' ? null : Number(e.target.value);
              onMarkChange();
            }}
            className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          />
        </div>
    </div>
  );
}

