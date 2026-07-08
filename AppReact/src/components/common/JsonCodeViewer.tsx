import React, { Suspense, useMemo, useState } from 'react';

const MonacoEditor = React.lazy(() => import('@monaco-editor/react'));

type JsonCodeViewerProps = {
  text: string;
  placeholder?: string;
  className?: string;
  monacoTheme?: string;
};

export const JsonCodeViewer: React.FC<JsonCodeViewerProps> = ({
  text,
  placeholder,
  className,
  monacoTheme = 'vs',
}) => {
  const raw = text ?? '';

  const displayed = useMemo(() => {
    return raw || '';
  }, [raw]);

  return (
    <div className="w-full h-full min-h-0 flex flex-col">
      <div className="flex-1 min-h-0 border rounded overflow-hidden bg-white">
        <Suspense fallback={<div className="p-2 text-xs text-gray-500">Loading code viewer...</div>}>
          <MonacoEditor
            language="json"
            value={displayed || placeholder || ''}
            theme={monacoTheme}
            options={{
              readOnly: true,
              minimap: { enabled: false },
              wordWrap: 'on',
              scrollBeyondLastLine: false,
              renderWhitespace: 'none',
              lineNumbers: 'on',
              folding: true,
              contextmenu: true,
              automaticLayout: true,
              quickSuggestions: false,
              suggestOnTriggerCharacters: false,
              selectionHighlight: false,
              occurrencesHighlight: 'off',
              unicodeHighlight: { ambiguousCharacters: false },
            }}
          />
        </Suspense>
      </div>
    </div>
  );
};

