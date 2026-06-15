import React, { useMemo } from 'react';
import { useTheme } from '../../../../../redux/hooks/useTheme';

function isLikelyFtpPath(path: string) {
  const p = (path || '').trim().toLowerCase();
  return p.startsWith('ftp://') || p.startsWith('ftps://');
}

export function FilePathAndFtpSection(props: {
  label: string;
  action: any;
  filePathProp: 'FilePath';
  onMarkChange: () => void;
  helpLines?: string[];
}) {
  const { theme } = useTheme();
  const { label, action, filePathProp, onMarkChange, helpLines } = props;

  const filePathValue = String(action?.ActionAttribute?.[filePathProp] ?? '');
  const showFtpLogin = useMemo(() => isLikelyFtpPath(filePathValue), [filePathValue]);

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>{label}</label>
        <div className="flex flex-col gap-1">
          <input
            type="text"
            className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={filePathValue}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute[filePathProp] = e.target.value;
              onMarkChange();
            }}
          />
          {helpLines?.length ? (
            <div className={`text-[11px] ${theme.label} opacity-80 leading-4`}>
              {helpLines.map((l) => (
                <div key={l}>{l}</div>
              ))}
            </div>
          ) : null}
        </div>
      </div>

      {showFtpLogin ? (
        <>
          <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Ftp User Name</label>
            <input
              type="text"
              className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              value={action?.ActionAttribute?.FtpUserName ?? ''}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                action.ActionAttribute.FtpUserName = e.target.value;
                onMarkChange();
              }}
            />
          </div>
          <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Ftp Password</label>
            <input
              type="text"
              className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
              value={action?.ActionAttribute?.FtpPassword ?? ''}
              onChange={(e) => {
                action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                action.ActionAttribute.FtpPassword = e.target.value;
                onMarkChange();
              }}
            />
          </div>
        </>
      ) : null}
    </>
  );
}

