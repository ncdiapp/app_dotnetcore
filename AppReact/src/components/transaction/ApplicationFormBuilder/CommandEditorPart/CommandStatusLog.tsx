import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export function CommandStatusLog(props: {
  action: any;
  commandLoggingPrefEnum: any;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { action, commandLoggingPrefEnum, onMarkChange } = props;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Log Command Status</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={String(Number((action.ActionAttribute as any)?.EmAppCommandLoggingPreference ?? 3))}
          onChange={(e) => {
            action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
            const v = Number(e.target.value);
            (action.ActionAttribute as any).EmAppCommandLoggingPreference = v;
            if (v === Number(commandLoggingPrefEnum?.DoNotLog ?? 3)) {
              action.ActionAttribute.IsLogCommandStartEnd = false;
              action.ActionAttribute.IsLogErrorDetails = false;
            } else if (v === Number(commandLoggingPrefEnum?.LogResultStatusOnly ?? 2)) {
              action.ActionAttribute.IsLogCommandStartEnd = true;
              action.ActionAttribute.IsLogErrorDetails = false;
            } else {
              action.ActionAttribute.IsLogCommandStartEnd = true;
              action.ActionAttribute.IsLogErrorDetails = true;
            }
            onMarkChange();
          }}
          title="Console log status preference"
        >
          <option value={String(Number(commandLoggingPrefEnum?.LogResultDetails ?? 1))}>Log Result Details</option>
          <option value={String(Number(commandLoggingPrefEnum?.LogResultStatusOnly ?? 2))}>Log Result Status Only</option>
          <option value={String(Number(commandLoggingPrefEnum?.DoNotLog ?? 3))}>Do Not Log</option>
        </select>
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Log Command Start &amp; End</label>
        <label className="flex items-center gap-2 text-xs">
          <input
            type="checkbox"
            disabled={
              Number((action.ActionAttribute as any)?.EmAppCommandLoggingPreference ?? 3) === Number(commandLoggingPrefEnum?.DoNotLog ?? 3)
            }
            checked={!!action.ActionAttribute?.IsLogCommandStartEnd}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute.IsLogCommandStartEnd = e.target.checked;
              onMarkChange();
            }}
          />
          <span className={theme.label}>Enable</span>
        </label>
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Log Command Info &amp; Error Details</label>
        <label className="flex items-center gap-2 text-xs">
          <input
            type="checkbox"
            disabled={
              Number((action.ActionAttribute as any)?.EmAppCommandLoggingPreference ?? 3) === Number(commandLoggingPrefEnum?.DoNotLog ?? 3)
            }
            checked={!!action.ActionAttribute?.IsLogErrorDetails}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute.IsLogErrorDetails = e.target.checked;
              onMarkChange();
            }}
          />
          <span className={theme.label}>Enable</span>
        </label>
      </div>
    </>
  );
}

