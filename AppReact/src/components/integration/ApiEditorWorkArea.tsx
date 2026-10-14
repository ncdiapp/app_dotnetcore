import React, { useEffect, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { JsonCodeEditor } from '../common/JsonCodeEditor';

type OuterTab = 'config' | 'payload';
type InnerTab = 'form' | 'json';

type Props = {
  apiUrl: string;
  showPayload: boolean;
  form: React.ReactNode;
  formExtra?: React.ReactNode;
  jsonText: string;
  jsonError?: string;
  onJsonChange: (text: string) => void;
  onJsonBlur?: () => void;
  payload?: React.ReactNode;
  result: React.ReactNode;
  onOuterTabChange?: (next: OuterTab, previous: OuterTab) => void;
};

function tabClass(active: boolean, themeTab: string): string {
  return `px-3 py-1.5 text-xs rounded-t-[4px] -mb-px ${active ? 'border-b-2 border-blue-500 font-medium' : ''} ${themeTab}`;
}

/** Left API Config (Form | JSON) and optional Payload, right Result. */
export const ApiEditorWorkArea: React.FC<Props> = ({
  apiUrl,
  showPayload,
  form,
  formExtra,
  jsonText,
  jsonError,
  onJsonChange,
  onJsonBlur,
  payload,
  result,
  onOuterTabChange,
}) => {
  const { theme } = useTheme();
  const [outer, setOuter] = useState<OuterTab>('config');
  const [inner, setInner] = useState<InnerTab>('form');

  useEffect(() => {
    if (!showPayload && outer === 'payload') setOuter('config');
  }, [showPayload, outer]);

  const selectOuter = (next: OuterTab) => {
    if (next === outer) return;
    if (inner === 'json' && outer === 'config') onJsonBlur?.();
    onOuterTabChange?.(next, outer);
    setOuter(next);
  };

  const selectInner = (next: InnerTab) => {
    if (next === inner) return;
    if (inner === 'json') onJsonBlur?.();
    setInner(next);
  };

  const showConfig = !showPayload || outer === 'config';

  return (
    <div className="h-1 flex-auto flex flex-col min-h-0">
      <div className="flex items-center gap-2 mb-3 flex-shrink-0 min-w-0">
        <span className={`flex-shrink-0 text-xs font-semibold ${theme.label}`}>API Url</span>
        <span className="w-1 flex-auto min-w-0 text-xs underline select-all truncate" title={apiUrl}>{apiUrl || '—'}</span>
      </div>

      <div className="h-1 flex-auto flex gap-4 min-h-0">
        <div className={`w-1 flex-auto flex flex-col border rounded min-h-0 min-w-0 ${theme.mainContentSection}`}>
          {showPayload ? (
            <div className="flex gap-0 border-b flex-shrink-0">
              <button type="button" className={tabClass(outer === 'config', theme.tab)} onClick={() => selectOuter('config')}>API Config</button>
              <button type="button" className={tabClass(outer === 'payload', theme.tab)} onClick={() => selectOuter('payload')}>Payload</button>
            </div>
          ) : (
            <div className="px-2 py-1.5 border-b flex-shrink-0">
              <span className="text-xs font-semibold">API Config</span>
            </div>
          )}

          {showConfig && (
            <>
              <div className="flex items-center justify-between border-b flex-shrink-0">
                <div className="flex gap-0">
                  <button type="button" className={tabClass(inner === 'form', theme.tab)} onClick={() => selectInner('form')}>Form</button>
                  <button type="button" className={tabClass(inner === 'json', theme.tab)} onClick={() => selectInner('json')}>JSON</button>
                </div>
                {inner === 'form' && formExtra ? <div className="flex items-center gap-1 pr-2">{formExtra}</div> : null}
              </div>
              {inner === 'form' ? (
                <div className="h-1 flex-auto flex flex-col p-2 min-h-0 overflow-auto">{form}</div>
              ) : (
                <div className="h-1 flex-auto flex flex-col p-2 min-h-0">
                  {jsonError ? <div className="mb-1 text-xs text-red-600 flex-shrink-0">{jsonError}</div> : null}
                  <div className="w-full h-1 flex-auto min-h-0 border rounded overflow-hidden bg-white">
                    <JsonCodeEditor
                      value={jsonText}
                      onChange={onJsonChange}
                      className="w-full h-full"
                    />
                  </div>
                </div>
              )}
            </>
          )}

          {showPayload && outer === 'payload' && (
            <div className="h-1 flex-auto flex flex-col min-h-0 overflow-auto">{payload}</div>
          )}
        </div>

        <div className={`w-1 flex-auto flex flex-col border rounded min-h-0 min-w-0 ${theme.mainContentSection}`}>
          <div className="px-2 py-1.5 border-b flex-shrink-0">
            <span className="text-xs font-semibold">Result</span>
          </div>
          <div className="h-1 flex-auto flex flex-col p-2 min-h-0">
            {result}
          </div>
        </div>
      </div>
    </div>
  );
};
