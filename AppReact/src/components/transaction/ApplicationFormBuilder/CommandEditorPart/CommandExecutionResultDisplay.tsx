import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export function CommandExecutionResultDisplay(props: {
  action: any;
  validationResultPrefEnum: any;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { action, validationResultPrefEnum, onMarkChange } = props;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Execute Result Console Display Type</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={String(Number(action.ActionAttribute?.EmAppValidationResultPreference ?? 1))}
          onChange={(e) => {
            action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
            action.ActionAttribute.EmAppValidationResultPreference = Number(e.target.value);
            onMarkChange();
          }}
          title="Execute result preference"
          required
        >
          <option value={String(Number(validationResultPrefEnum?.ShowResultDetails ?? 1))}>Show Result Details</option>
          <option value={String(Number(validationResultPrefEnum?.ShowResultStatusOnly ?? 2))}>Show Result Status Only</option>
        </select>
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-3">
        <label className={`text-xs ${theme.label} pt-1`}>Customized Execution Result Message</label>
        <div className="flex flex-col gap-2 min-w-0">
          <label className="flex items-center gap-2 text-xs">
            <input
              type="checkbox"
              checked={!!action.ActionAttribute?.IsEnableCommandExecuteResultMessage}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                const on = e.target.checked;
                action.ActionAttribute.IsEnableCommandExecuteResultMessage = on;
                if (!on) action.ActionAttribute.CommandSuccessMessage = '';
                onMarkChange();
              }}
            />
            <span className={theme.label}>Enable</span>
          </label>

          {!!action.ActionAttribute?.IsEnableCommandExecuteResultMessage ? (
            <textarea
              value={action.ActionAttribute?.CommandSuccessMessage ?? ''}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                action.ActionAttribute.CommandSuccessMessage = e.target.value;
                onMarkChange();
              }}
              rows={3}
              className={`w-full px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none`}
            />
          ) : null}
        </div>
      </div>
    </>
  );
}

