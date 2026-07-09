import React, { Suspense, useEffect, useMemo, useRef, useState } from 'react';

const MonacoEditor = React.lazy(() => import('@monaco-editor/react'));

type JsonCodeEditorProps = {
  value: string;
  onChange: (next: string) => void;
  language?: string;
  placeholder?: string;
  className?: string;
  readOnly?: boolean;
  /** Debounce parent onChange for typing responsiveness */
  debounceMs?: number;
  onMount?: (editor: any, monaco: any) => void;
  /** Monaco editor theme — 'vs' (light) or 'vs-dark'. Defaults to 'vs'. */
  monacoTheme?: string;
  /** Enable trigger-character suggestions (e.g. {{ autocomplete). Default false. */
  enableTriggerSuggestions?: boolean;
};

export const JsonCodeEditor: React.FC<JsonCodeEditorProps> = ({
  value,
  onChange,
  language = 'json',
  placeholder,
  className,
  readOnly = false,
  debounceMs = 150,
  onMount,
  monacoTheme = 'vs',
  enableTriggerSuggestions = false,
}) => {
  const [localValue, setLocalValue] = useState<string>(value ?? '');
  const latestOnChangeRef = useRef(onChange);
  const debounceTimerRef = useRef<number | null>(null);
  const localValueRef = useRef<string>(value ?? '');

  useEffect(() => {
    latestOnChangeRef.current = onChange;
  }, [onChange]);

  useEffect(() => {
    localValueRef.current = localValue;
  }, [localValue]);

  // Sync from parent only when it actually changes (e.g., load/reset/regenerate).
  useEffect(() => {
    const next = value ?? '';
    setLocalValue((prev) => (prev === next ? prev : next));
  }, [value]);

  const flush = useMemo(() => {
    return (next: string) => {
      if (debounceTimerRef.current) {
        window.clearTimeout(debounceTimerRef.current);
        debounceTimerRef.current = null;
      }
      latestOnChangeRef.current(next);
    };
  }, []);

  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) {
        window.clearTimeout(debounceTimerRef.current);
        debounceTimerRef.current = null;
      }
      // Ensure we don't lose the last buffered edits on unmount (e.g., switching rows/commands).
      // Safe to call even if parent already has this value.
      try {
        latestOnChangeRef.current(localValueRef.current ?? '');
      } catch {
        // ignore
      }
    };
  }, []);

  const scheduleChange = (next: string) => {
    setLocalValue(next);
    if (debounceMs <= 0) {
      flush(next);
      return;
    }
    if (debounceTimerRef.current) window.clearTimeout(debounceTimerRef.current);
    debounceTimerRef.current = window.setTimeout(() => flush(next), debounceMs) as unknown as number;
  };

  return (
    <div className={className ?? 'w-full h-full'}>
      <Suspense fallback={<div className="p-2 text-xs text-gray-500">Loading editor...</div>}>
        <MonacoEditor
          language={language}
          value={localValue || placeholder || ''}
          theme={monacoTheme}
          onMount={(editor, monaco) => {
            editor.onDidBlurEditorText(() => {
              flush(editor.getValue());
            });
            onMount?.(editor, monaco);
          }}
          onChange={(v) => {
            if (readOnly) return;
            scheduleChange(v ?? '');
          }}
          options={{
            readOnly,
            minimap: { enabled: false },
            wordWrap: 'on',
            scrollBeyondLastLine: false,
            renderWhitespace: 'none',
            lineNumbers: 'on',
            folding: true,
            contextmenu: true,
            automaticLayout: true,
            quickSuggestions: enableTriggerSuggestions ? { other: true, comments: false, strings: true } : false,
            suggestOnTriggerCharacters: enableTriggerSuggestions,
            selectionHighlight: false,
            occurrencesHighlight: 'off',
            unicodeHighlight: { ambiguousCharacters: false },
            fontSize: 12,
          }}
        />
      </Suspense>
    </div>
  );
};

