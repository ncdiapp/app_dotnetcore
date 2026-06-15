import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export const EmAppTransactionCommandTypeCommnadFormulaCalculation = 57;

// Angular: transactionCommandSingleEditorPopupCtrl.js
const ASSIGNMENT_OPERATOR_TOKENS = [
  '=',
  '+',
  '-',
  '*',
  '/',
  '(',
  ')',
  '==',
  '!=',
  '>',
  '<',
  '>=',
  '<=',
  '&&',
  '||',
  '!',
  ':',
  'true',
  'false',
];

const ASSIGNMENT_FUNCTION_TOKENS = [
  'string.IsNullOrEmpty()',
  'IsNumericHasValue()',
  'IsDDLHasValue()',
  'IsDateHasValue()',
  'ConvertValueToDate()',
  'ConvertValueToBoolean()',
  'ConvertValueToInt()',
  'ConvertValueToDecimal()',
];

const JSON_FUNCTION_TOKENS = [
  'GetJsonNodeValueByPath(JsonString, JsonPath)',
  'FindOneItemFromJsonArray(JsonArrayString, PropName, PropValue)',
  'GetOneItemFromJsonArrayByIndex(JsonArrayString, Index)',
];

// Angular: FormulaBuiltInTokenList
const FORMULA_BUILT_IN_TOKENS = ['[CurrentUserId]', '[CurrentPartnerId]', '[CurrentUserIPAddress]'];

function insertTextAtTextareaCursor(textarea: HTMLTextAreaElement, text: string) {
  const start = textarea.selectionStart ?? textarea.value.length;
  const end = textarea.selectionEnd ?? textarea.value.length;
  const v = textarea.value ?? '';
  const next = v.slice(0, start) + text + v.slice(end);
  textarea.value = next;
  const caret = start + text.length;
  textarea.setSelectionRange(caret, caret);
}

export function CommandFormulaSection(props: {
  action: any;
  rootLevelAllFieldLookUpList: any[];
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { action, rootLevelAllFieldLookUpList, onMarkChange } = props;

  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const keypadRef = useRef<HTMLDivElement | null>(null);
  const [showKeypad, setShowKeypad] = useState(false);

  const actionType = Number(action?.ActionType);
  const show = !!action && actionType === EmAppTransactionCommandTypeCommnadFormulaCalculation;

  const fieldTokens = useMemo(() => {
    const list = Array.isArray(rootLevelAllFieldLookUpList) ? rootLevelAllFieldLookUpList : [];
    return list
      .map((f: any) => ({
        id: f?.Id ?? f?.id ?? f?.Value ?? Math.random(),
        short: f?.ShortDisplay ?? f?.Display ?? String(f?.Id ?? ''),
        token: f?.FormulaDisplayName ?? f?.Display ?? f?.ShortDisplay ?? String(f?.Id ?? ''),
        tooltip: f?.FormulaDisplayName ?? f?.Display ?? f?.ShortDisplay ?? '',
      }))
      .slice(0, 400);
  }, [rootLevelAllFieldLookUpList]);

  const setFormulaValue = useCallback(
    (next: string) => {
      if (!action) return;
      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
      action.ActionAttribute.ExecutionFormulaUI = next;
      onMarkChange();
    },
    [action, onMarkChange],
  );

  const handleInsertToken = useCallback(
    (token: string, opts?: { padSpaces?: boolean }) => {
      const padSpaces = opts?.padSpaces ?? true;
      const t = padSpaces ? ` ${token} ` : token;
      const ta = textareaRef.current;
      if (!ta) {
        setFormulaValue((action?.ActionAttribute?.ExecutionFormulaUI ?? '') + t);
        return;
      }
      try {
        // Keep textarea DOM value in sync for correct cursor behavior
        ta.focus();
        insertTextAtTextareaCursor(ta, t);
        setFormulaValue(ta.value);
      } catch {
        setFormulaValue((action?.ActionAttribute?.ExecutionFormulaUI ?? '') + t);
      }
    },
    [action?.ActionAttribute?.ExecutionFormulaUI, setFormulaValue],
  );

  const handleInsertField = useCallback(
    (token: string) => {
      const ta = textareaRef.current;
      if (!ta) {
        setFormulaValue((action?.ActionAttribute?.ExecutionFormulaUI ?? '') + token);
        return;
      }
      try {
        ta.focus();
        insertTextAtTextareaCursor(ta, token);
        setFormulaValue(ta.value);
      } catch {
        setFormulaValue((action?.ActionAttribute?.ExecutionFormulaUI ?? '') + token);
      }
    },
    [action?.ActionAttribute?.ExecutionFormulaUI, setFormulaValue],
  );

  useEffect(() => {
    if (!showKeypad) return;
    const onDocMouseDown = (e: MouseEvent) => {
      const t = e.target as Node;
      const ta = textareaRef.current;
      const kp = keypadRef.current;
      if (kp && kp.contains(t)) return;
      if (ta && ta.contains(t)) return;
      setShowKeypad(false);
    };
    document.addEventListener('mousedown', onDocMouseDown);
    return () => document.removeEventListener('mousedown', onDocMouseDown);
  }, [showKeypad]);

  if (!show) return null;

  return (
    <div className="grid grid-cols-[14rem_1fr] items-start gap-2 py-1">
      <div>
        <label className={`text-xs ${theme.label}`}>Execute Formula</label>
        <div className={`text-[11px] ${theme.label} opacity-80`}>(Please seperate multiple formulas with &quot;;&quot;)</div>
      </div>
      <div className="flex flex-col gap-2">
        <textarea
          ref={textareaRef}
          rows={8}
          className={`w-full font-mono text-xs px-2 py-1 border ${theme.inputBox} focus:outline-none`}
          value={action?.ActionAttribute?.ExecutionFormulaUI ?? ''}
          placeholder={action?.ActionAttribute?.ExecutionFormulaUI ? '' : 'Example: [Field] = 1'}
          onClick={() => setShowKeypad(true)}
          onFocus={() => setShowKeypad(true)}
          onChange={(e) => setFormulaValue(e.target.value)}
        />

        {showKeypad ? (
          <div ref={keypadRef} className={`w-full border rounded ${theme.mainContentSection}`}>
            <div className="flex items-center justify-between px-2 py-1 border-b">
              <div className={`text-xs font-semibold ${theme.title}`}>Form Fields / Operators and Functions</div>
              <button
                type="button"
                className={`w-7 h-7 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
                onClick={() => setShowKeypad(false)}
                title="Close"
                aria-label="Close"
              >
                <i className="fa-solid fa-xmark" aria-hidden />
              </button>
            </div>

            <div className="w-full flex gap-2 p-2 overflow-hidden">
              <div className="w-1 flex-auto min-w-0 overflow-auto" style={{ maxHeight: 220 }}>
                <div className={`text-[11px] font-semibold mb-2 ${theme.title}`}>Form Fields</div>
                <div className="grid grid-cols-3 gap-1">
                  {fieldTokens.map((f) => (
                    <button
                      key={String(f.id)}
                      type="button"
                      className={`text-left px-2 py-1 text-[11px] rounded border ${theme.inputBox}`}
                      title={f.tooltip || f.token}
                      onClick={() => handleInsertField(f.token)}
                    >
                      {f.short}
                    </button>
                  ))}
                </div>
              </div>

              <div className="w-[320px] shrink-0 overflow-auto border-l pl-2" style={{ maxHeight: 220 }}>
                <div className={`text-[11px] font-semibold mb-2 ${theme.title}`}>Operators and Functions</div>

                <div className="grid grid-cols-4 gap-1 mb-2">
                  {ASSIGNMENT_OPERATOR_TOKENS.map((t) => (
                    <button
                      key={t}
                      type="button"
                      className={`px-2 py-1 text-[11px] rounded border ${theme.inputBox}`}
                      title={t}
                      onClick={() => handleInsertToken(t)}
                    >
                      {t}
                    </button>
                  ))}
                </div>

                <div className={`text-[11px] font-semibold mb-1 ${theme.title}`}>Functions</div>
                <div className="flex flex-col gap-1 mb-2">
                  {ASSIGNMENT_FUNCTION_TOKENS.map((t) => (
                    <button
                      key={t}
                      type="button"
                      className={`text-left px-2 py-1 text-[11px] rounded border ${theme.inputBox}`}
                      title={t}
                      onClick={() => handleInsertToken(t, { padSpaces: true })}
                    >
                      {t}
                    </button>
                  ))}
                </div>

                <div className={`text-[11px] font-semibold mb-1 ${theme.title}`}>JSON Functions</div>
                <div className="flex flex-col gap-1">
                  {JSON_FUNCTION_TOKENS.map((t) => (
                    <button
                      key={t}
                      type="button"
                      className={`text-left px-2 py-1 text-[11px] rounded border ${theme.inputBox}`}
                      title={t}
                      onClick={() => handleInsertToken(t, { padSpaces: true })}
                    >
                      {t}
                    </button>
                  ))}
                </div>

                <div className="mt-3">
                  <div className={`text-[11px] font-semibold mb-1 ${theme.title}`}>Built-In Token</div>
                  <div className="grid grid-cols-2 gap-1">
                    {FORMULA_BUILT_IN_TOKENS.map((t) => (
                      <button
                        key={t}
                        type="button"
                        className={`text-left px-2 py-1 text-[11px] rounded border ${theme.inputBox}`}
                        title={t}
                        onClick={() => handleInsertToken(t)}
                      >
                        {t}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </div>
        ) : null}
      </div>
    </div>
  );
}

