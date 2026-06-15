import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import MetaDataViewDesign from '../../../transaction/metaDataViewDesign';
import { JsonCodeEditor } from '../../../common/JsonCodeEditor';
import {
  insertTextIntoMonacoEditor,
  resolveTransactionFieldToken,
  SQL_EXPRESSION_BUILTIN_TOKENS,
} from './sqlQueryBuilderTokens';

/** Selection if non-empty; otherwise full editor text; otherwise fallback draft. */
function getSqlSeedForQueryDesign(editor: any, fallbackDraft: string): string {
  try {
    if (editor?.getModel && editor?.getSelection) {
      const sel = editor.getSelection();
      const model = editor.getModel();
      if (sel && model) {
        const isEmpty =
          typeof sel.isEmpty === 'function'
            ? sel.isEmpty()
            : sel.startLineNumber === sel.endLineNumber && sel.startColumn === sel.endColumn;
        if (!isEmpty) {
          const selectedText = model.getValueInRange(sel);
          if (selectedText != null && String(selectedText).trim()) return String(selectedText);
        }
      }
      const full = editor.getValue?.();
      if (full != null && String(full).trim()) return String(full);
    }
  } catch {
    // ignore
  }
  return fallbackDraft ?? '';
}

export function SqlStatementSection(props: {
  hierarchy: any;
  action: any;
  rootLevelTransFieldLookUpList: any[];
  childGridFieldGroups: Array<{ unitName: string; fields: any[] }>;
  globalTransFieldLookUpList?: any[];
  rootFieldSectionTitle?: string;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const {
    hierarchy,
    action,
    rootLevelTransFieldLookUpList,
    childGridFieldGroups,
    globalTransFieldLookUpList = [],
    rootFieldSectionTitle = 'Form Root Level Fields',
    onMarkChange,
  } = props;

  const sqlEditorRef = useRef<any>(null);
  const [isSqlTokenBuilderOpen, setIsSqlTokenBuilderOpen] = useState(false);
  const [isSqlQueryDesignOpen, setIsSqlQueryDesignOpen] = useState(false);
  const [sqlQueryBuilderSeed, setSqlQueryBuilderSeed] = useState<string>('');
  const [sqlTokenBuilderDraft, setSqlTokenBuilderDraft] = useState<string>('');

  const isSqlCommand = !!action && Number(action.ActionType) === 42;

  const sortedGlobalFields = useMemo(() => {
    const list = Array.isArray(globalTransFieldLookUpList) ? [...globalTransFieldLookUpList] : [];
    return list.sort((a, b) => String(a?.ShortDisplay ?? '').localeCompare(String(b?.ShortDisplay ?? '')));
  }, [globalTransFieldLookUpList]);

  const sortedRootFields = useMemo(() => {
    const list = Array.isArray(rootLevelTransFieldLookUpList) ? [...rootLevelTransFieldLookUpList] : [];
    return list.sort((a, b) => String(a?.ShortDisplay ?? '').localeCompare(String(b?.ShortDisplay ?? '')));
  }, [rootLevelTransFieldLookUpList]);

  const exampleFieldToken = useMemo(() => {
    const first = sortedRootFields[0];
    return first ? resolveTransactionFieldToken(first) : '[TF_FieldId]';
  }, [sortedRootFields]);

  const insertSqlToken = useCallback((token: string) => {
    const editor = sqlEditorRef.current;
    if (insertTextIntoMonacoEditor(editor, token)) {
      const next = editor.getValue?.() ?? '';
      setSqlTokenBuilderDraft(next);
      return;
    }
    setSqlTokenBuilderDraft((prev) => (prev ?? '') + token);
  }, []);

  // Angular / server: SQL text is stored on AppProjectWorkFlowAction.NotificationMessage (DB column NotificationMessage).
  // AssignSqlResultToFiledId is on ActionAttribute.
  useEffect(() => {
    if (!action || !isSqlCommand) return;
    const msg = action.NotificationMessage;
    const wrongAttrSql = action.ActionAttribute?.SqlStatement;
    if ((!msg || !String(msg).trim()) && wrongAttrSql && String(wrongAttrSql).trim()) {
      action.NotificationMessage = String(wrongAttrSql);
      onMarkChange();
    }
  }, [action, isSqlCommand, onMarkChange]);

  if (!isSqlCommand) return null;

  const sqlStatement = action.NotificationMessage ?? '';

  return (
    <>
      <div className="flex flex-col gap-3">
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2">
          <label className={`text-xs ${theme.label}`}>Assign Query Scalar Result To Root Field</label>
          <select
            className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
            value={action.ActionAttribute?.AssignSqlResultToFiledId ?? ''}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              const v = e.target.value;
              action.ActionAttribute.AssignSqlResultToFiledId = v ? Number(v) : null;
              onMarkChange();
            }}
          >
            <option value="">(None)</option>
            {sortedRootFields.map((f: any) => (
              <option key={String(f.Id)} value={f.Id}>
                {f.ShortDisplay ?? f.Display ?? f.Id}
              </option>
            ))}
          </select>
        </div>

        <div>
          <div className="flex items-center justify-between gap-2 mb-1">
            <label className={`block text-xs ${theme.label}`}>SQL Statement</label>
            <button
              type="button"
              className={`px-2.5 py-1 text-xs rounded-[4px] ${theme.button_default}`}
              onClick={() => {
                const editor = sqlEditorRef.current;
                let seed = sqlStatement ?? '';
                try {
                  const sel = editor?.getSelection?.();
                  const model = editor?.getModel?.();
                  if (sel && model) {
                    const selectedText = model.getValueInRange(sel);
                    if (selectedText && String(selectedText).trim()) seed = String(selectedText);
                  }
                } catch {
                  // ignore
                }
                setSqlQueryBuilderSeed(seed ?? '');
                setSqlTokenBuilderDraft(sqlStatement ?? '');
                setIsSqlTokenBuilderOpen(true);
              }}
              title="Query Builder with Built-in Tokens"
            >
              <i className="fa-solid fa-pen-to-square mr-1" aria-hidden />
              Query Builder with Built-in Tokens
            </button>
          </div>
          <div className={`w-full h-[260px] border rounded overflow-hidden ${theme.inputBox}`}>
            <JsonCodeEditor
              key={String(action?.Id ?? 'sql')}
              value={sqlStatement}
              language="sql"
              debounceMs={150}
              className="w-full h-full"
              onMount={(editor) => {
                sqlEditorRef.current = editor;
              }}
              onChange={(next) => {
                action.NotificationMessage = next;
                onMarkChange();
              }}
            />
          </div>
        </div>
      </div>

      {isSqlTokenBuilderOpen && (
        <EmbeddedLinkedPopupFrame
          title="Query Builder with Built-in Tokens"
          fullscreenPosition="afterTrailing"
          toolbarTrailing={
            <div className="flex items-center gap-2 w-full">
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => {
                  const seed = getSqlSeedForQueryDesign(sqlEditorRef.current, sqlTokenBuilderDraft ?? '');
                  setSqlQueryBuilderSeed(seed);
                  setIsSqlQueryDesignOpen(true);
                }}
                title="Query Design Tool"
              >
                <i className="fa-solid fa-database mr-1" aria-hidden />
                Query Design Tool
              </button>
              <div className="w-1 flex-auto" />
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => {
                  action.NotificationMessage = sqlTokenBuilderDraft ?? '';
                  onMarkChange();
                  setIsSqlTokenBuilderOpen(false);
                }}
                title="Apply and close"
              >
                <i className="fa-solid fa-floppy-disk mr-1" aria-hidden />
                Apply &amp; Close
              </button>
            </div>
          }
          toolbarTrailingEnd={
            <button
              type="button"
              className={`w-8 h-8 flex items-center justify-center rounded-[4px] ${theme.button_default}`}
              onClick={() => setIsSqlTokenBuilderOpen(false)}
              aria-label="Close"
              title="Close"
            >
              <i className="fa-solid fa-xmark" aria-hidden />
            </button>
          }
        >
          <div className="w-full h-full min-h-0 flex overflow-hidden">
            <div className="w-1 flex-auto min-w-0 p-2 overflow-hidden">
              <div className={`w-full h-full border rounded overflow-hidden ${theme.inputBox}`}>
                <JsonCodeEditor
                  value={sqlTokenBuilderDraft}
                  language="sql"
                  debounceMs={150}
                  className="w-full h-full"
                  onMount={(editor) => {
                    sqlEditorRef.current = editor;
                  }}
                  onChange={(next) => {
                    setSqlTokenBuilderDraft(next ?? '');
                  }}
                />
              </div>
            </div>
            <div
              className={`w-[380px] shrink-0 border-l p-2 overflow-y-auto ${theme.mainContentSection}`}
              style={{ maxHeight: '100%' }}
            >
              <fieldset className={`border rounded px-2 pt-3 pb-2 relative mb-4 ${theme.inputBox}`}>
                <legend className={`px-1 text-[11px] ${theme.label}`}>Example: Use Token In Query</legend>
                <div className={`text-[11px] ${theme.label} space-y-3`}>
                  <div>
                    <div className="font-semibold">Example 1:</div>
                    <div className="text-blue-700 dark:text-blue-300 py-1">
                      <div>UPDATE TableProduct SET ProductCode = &apos;abc&apos;</div>
                      <div>
                        WHERE ProductId = {exampleFieldToken} AND CreatedByUserId = [CurrentUserId]
                      </div>
                    </div>
                  </div>
                  <div>
                    <div className="font-semibold">Example 2:</div>
                    <div className="text-blue-700 dark:text-blue-300 py-1">
                      <div>Declare @ProductId int = {exampleFieldToken};</div>
                      <div className="pt-2">UPDATE TableProduct SET ProductCode = &apos;abc&apos;</div>
                      <div>WHERE ProductId = @ProductId;</div>
                    </div>
                  </div>
                </div>
              </fieldset>

              {sortedGlobalFields.length > 0 ? (
                <fieldset className={`border rounded px-2 pt-3 pb-2 relative mb-4 ${theme.inputBox}`}>
                  <legend className={`px-1 text-[11px] ${theme.label}`}>Global Fields</legend>
                  <div className="flex flex-wrap gap-1">
                    {sortedGlobalFields.map((f: any) => {
                      const token = resolveTransactionFieldToken(f);
                      return (
                        <button
                          key={`g-${String(f.Id)}`}
                          type="button"
                          title={token}
                          className={`px-2 py-1 text-[11px] rounded border text-center ${theme.button_default}`}
                          style={{ minWidth: '120px', flex: '1 1 auto' }}
                          onClick={() => insertSqlToken(token)}
                        >
                          {f.ShortDisplay ?? f.Display}
                        </button>
                      );
                    })}
                  </div>
                </fieldset>
              ) : null}

              <fieldset className={`border rounded px-2 pt-3 pb-2 relative mb-4 ${theme.inputBox}`}>
                <legend className={`px-1 text-[11px] ${theme.label}`}>{rootFieldSectionTitle}</legend>
                <div className="flex flex-wrap gap-1">
                  {sortedRootFields.map((f: any) => {
                    const token = resolveTransactionFieldToken(f);
                    return (
                      <button
                        key={String(f.Id)}
                        type="button"
                        title={token}
                        className={`px-2 py-1 text-[11px] rounded border text-center ${theme.button_default}`}
                        style={{ minWidth: '120px', flex: '1 1 auto' }}
                        onClick={() => insertSqlToken(token)}
                      >
                        {f.ShortDisplay ?? f.Display}
                      </button>
                    );
                  })}
                </div>
              </fieldset>

              {childGridFieldGroups.map((g) => (
                <fieldset key={g.unitName} className={`border rounded px-2 pt-3 pb-2 relative mb-4 ${theme.inputBox}`}>
                  <legend className={`px-1 text-[11px] ${theme.label}`}>Grid {g.unitName} Columns</legend>
                  <div className="flex flex-wrap gap-1">
                    {g.fields.map((f: any) => {
                      const token = resolveTransactionFieldToken(f);
                      return (
                        <button
                          key={String(f.Id)}
                          type="button"
                          title={token}
                          className={`px-2 py-1 text-[11px] rounded border text-center ${theme.button_default}`}
                          style={{ minWidth: '120px', flex: '1 1 auto' }}
                          onClick={() => insertSqlToken(token)}
                        >
                          {f.ShortDisplay}
                        </button>
                      );
                    })}
                  </div>
                </fieldset>
              ))}

              <fieldset className={`border rounded px-2 pt-3 pb-2 relative ${theme.inputBox}`}>
                <legend className={`px-1 text-[11px] ${theme.label}`}>Built-in Token</legend>
                <div className="flex flex-wrap gap-1">
                  {SQL_EXPRESSION_BUILTIN_TOKENS.map((t) => (
                    <button
                      key={t}
                      type="button"
                      title={t}
                      className={`px-2 py-1 text-[11px] rounded border text-center ${theme.button_default}`}
                      style={{ minWidth: '120px', flex: '1 1 auto' }}
                      onClick={() => insertSqlToken(t)}
                    >
                      {t}
                    </button>
                  ))}
                </div>
              </fieldset>
            </div>
          </div>
        </EmbeddedLinkedPopupFrame>
      )}

      {isSqlQueryDesignOpen && (
        <EmbeddedLinkedPopupFrame
          title="Command Query Builder"
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setIsSqlQueryDesignOpen(false)}>
              Close
            </button>
          }
        >
          <div className="w-full h-full min-h-0 flex flex-col overflow-hidden">
            <MetaDataViewDesign
              key={sqlQueryBuilderSeed}
              isOpen={true}
              onClose={() => setIsSqlQueryDesignOpen(false)}
              isEmbeddedByOtherPage={true}
              dataSourceRegisterId={
                hierarchy?.DataSourceFrom != null
                  ? Number(hierarchy.DataSourceFrom)
                  : hierarchy?.DataSourceRegisterId != null
                    ? Number(hierarchy.DataSourceRegisterId)
                    : null
              }
              initialQueryText={sqlQueryBuilderSeed}
              onQueryBuilt={(queryText: string) => {
                const editor = sqlEditorRef.current;
                try {
                  if (editor?.executeEdits && editor?.getSelection) {
                    const range = editor.getSelection();
                    const id = { major: 1, minor: 1 };
                    const op = { identifier: id, range, text: queryText, forceMoveMarkers: true };
                    editor.executeEdits('query-builder', [op]);
                    const next = editor.getValue?.() ?? '';
                    setSqlTokenBuilderDraft(next);
                    setIsSqlQueryDesignOpen(false);
                    return;
                  }
                } catch {
                  // ignore
                }
                setSqlTokenBuilderDraft(queryText ?? '');
                setIsSqlQueryDesignOpen(false);
              }}
            />
          </div>
        </EmbeddedLinkedPopupFrame>
      )}
    </>
  );
}
