/**
 * Right-side Configurator chat for Search / Form design hosts.
 * - Search → app-search-view-config
 * - Form / TX → app-configurator
 *
 * A′ collaboration (TX / Search editors): if the design surface has unsaved memory changes,
 * block chat until Save or Discard+Reload so Agent DB writes cannot fight local draft.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { genericAgentSvc } from '../../webapi/genericAgentSvc';
import { FORM_CONFIG_SKILL_KEY, buildFormConfigContext } from '../formMgt/FormAgent/formConfigHost';
import AgentUiChatHost from './AgentUiChatHost';

export const SEARCH_VIEW_CONFIG_SKILL_KEY = 'app-search-view-config';

type DirtyGateProps = {
  isDirty?: boolean;
  onSaveDirty?: () => Promise<boolean>;
};

export type ConfiguratorAgentDockProps =
  | ({
      kind: 'search';
      searchId: number;
      searchName?: string | null;
      searchViewId?: number | null;
      dataSetId?: number | null;
      saasApplicationId?: number | null;
      scope: string;
      onReload?: () => void | Promise<void>;
      onHidePanel?: () => void;
    } & DirtyGateProps)
  | ({
      kind: 'form';
      transactionId: number;
      transactionName?: string | null;
      transaction?: any;
      scope: string;
      onReload?: () => void | Promise<void>;
      onHidePanel?: () => void;
    } & DirtyGateProps);

const ConfiguratorAgentDock: React.FC<ConfiguratorAgentDockProps> = (props) => {
  const { theme } = useTheme();
  const { showConfirm, showAlert } = useAlertConfirm();
  const [chatKey, setChatKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [dirtyBusy, setDirtyBusy] = useState(false);
  const pendingReloadRef = useRef(false);
  const onReloadRef = useRef(props.onReload);
  onReloadRef.current = props.onReload;
  const isDirty = !!props.isDirty;
  const onSaveDirtyRef = useRef(props.onSaveDirty);
  onSaveDirtyRef.current = props.onSaveDirty;

  const skillKey = props.kind === 'search' ? SEARCH_VIEW_CONFIG_SKILL_KEY : FORM_CONFIG_SKILL_KEY;
  const banner =
    props.kind === 'search'
      ? `Search: ${props.searchName || props.searchId}`
      : `TX: ${props.transactionName || props.transactionId}`;

  useEffect(() => {
    let cancelled = false;
    setChatKey(null);
    setError(null);
    (async () => {
      try {
        const key =
          props.kind === 'search'
            ? await genericAgentSvc.EnsureSearchViewConfigChat(props.searchId, props.scope)
            : await genericAgentSvc.EnsureFormConfigChat(props.transactionId, props.scope);
        if (!cancelled) setChatKey(key);
      } catch (e: any) {
        if (!cancelled) setError(e?.message || 'Could not open Configurator chat.');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [
    props.kind,
    props.scope,
    props.kind === 'search' ? props.searchId : props.transactionId,
  ]);

  const hostContextJson = useMemo(() => {
    if (props.kind === 'search') {
      return JSON.stringify({
        surface: 'search',
        mode: 'search-view-config',
        searchId: props.searchId,
        searchName: props.searchName || undefined,
        searchViewId: props.searchViewId ?? undefined,
        dataSetId: props.dataSetId ?? undefined,
        saasApplicationId: props.saasApplicationId ?? undefined,
      });
    }
    if (props.transaction) {
      return JSON.stringify(
        buildFormConfigContext({
          mode: 'master',
          transaction: props.transaction,
        }),
      );
    }
    return JSON.stringify({
      surface: 'form',
      mode: 'configurator',
      transactionId: props.transactionId,
      transactionName: props.transactionName || undefined,
    });
  }, [props]);

  const handleHostTool = useCallback(async (toolName: string, _args: any) => {
    const reloadTools =
      props.kind === 'search'
        ? ['request_search_reload']
        : ['request_form_reload'];
    if (reloadTools.includes(toolName)) {
      if (!onReloadRef.current) {
        return { ok: false, error: 'Reload is not wired on this host.' };
      }
      pendingReloadRef.current = true;
      return { ok: true, deferredPrompt: true };
    }
    return { ok: false, error: `${toolName} is not a configurator host tool.` };
  }, [props.kind]);

  const handleRunComplete = useCallback(() => {
    if (!pendingReloadRef.current) return;
    pendingReloadRef.current = false;
    const reload = onReloadRef.current;
    if (!reload) return;
    void (async () => {
      // Dirty editor after agent DB write: never "Save then Reload" — that pushes stale local
      // values (e.g. Data Transfer Setting = None) and wipes the agent's persisted bind.
      if (isDirty) {
        const discardAndReload = await showConfirm(
          'Configurator updated the database, but this editor still has unsaved local changes. Discard local edits and reload from the database (recommended). Saving local first can overwrite agent changes (e.g. clear Data Transfer Setting).',
          {
            title: 'Reload from database',
            confirmLabel: 'Discard & Reload',
            cancelLabel: 'Keep local (Later)',
          },
        );
        if (discardAndReload) await reload();
        return;
      }

      const ok = await showConfirm(
        props.kind === 'search'
          ? 'Configuration has been changed. Reload the current Search to apply it?'
          : 'Configuration has been changed. Reload the editor from the database to apply it?',
        {
          title: props.kind === 'search' ? 'Reload Search' : 'Reload Editor',
          confirmLabel: 'Reload',
          cancelLabel: 'Later',
        },
      );
      if (ok) await reload();
    })();
  }, [props.kind, showConfirm, showAlert, isDirty]);

  const handleSaveDirty = async () => {
    if (!onSaveDirtyRef.current) return;
    setDirtyBusy(true);
    try {
      const ok = await onSaveDirtyRef.current();
      if (!ok) showAlert?.('Save failed. Fix errors in the editor, then try again.');
    } finally {
      setDirtyBusy(false);
    }
  };

  const handleDiscardDirty = async () => {
    if (!onReloadRef.current) return;
    const ok = await showConfirm(
      'Discard unsaved local changes and reload from the database?',
      {
        title: 'Discard local changes',
        confirmLabel: 'Discard & Reload',
        cancelLabel: 'Cancel',
      },
    );
    if (!ok) return;
    setDirtyBusy(true);
    try {
      await onReloadRef.current();
    } finally {
      setDirtyBusy(false);
    }
  };

  const chatBlocked = isDirty;

  return (
    <div className={`h-full w-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex-none px-2 py-1.5 text-xs border-b ${theme.mainContentSection} ${theme.label}`}>
        <span className={`font-medium ${theme.title}`}>Configurator</span>
        <span className="mx-1">·</span>
        <span>{banner}</span>
      </div>
      {chatBlocked && (
        <div className={`flex-none px-2 py-2 text-xs border-b ${theme.mainContentSection} ${theme.label}`}>
          <div className="mb-2">
            Editor has unsaved changes. Save or discard them before using Configurator (Agent writes to the database).
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={dirtyBusy || !props.onSaveDirty}
              className={`px-2 py-1 rounded-[4px] text-xs ${theme.button_default} disabled:opacity-50`}
              onClick={() => void handleSaveDirty()}
            >
              Save
            </button>
            <button
              type="button"
              disabled={dirtyBusy || !props.onReload}
              className={`px-2 py-1 rounded-[4px] text-xs ${theme.button_default} disabled:opacity-50`}
              onClick={() => void handleDiscardDirty()}
            >
              Discard &amp; Reload
            </button>
          </div>
        </div>
      )}
      <div className={`h-1 flex-auto min-h-0 overflow-hidden relative ${chatBlocked ? 'opacity-40 pointer-events-none' : ''}`}>
        {error && <div className={`p-3 text-xs ${theme.label}`}>{error}</div>}
        {!error && !chatKey && <div className={`p-3 text-xs ${theme.label}`}>Opening Configurator…</div>}
        {!error && chatKey && (
          <AgentUiChatHost
            skillKey={skillKey}
            chatSessionKey={chatKey}
            showChatListButton={false}
            squareChrome
            onHidePanel={props.onHidePanel}
            getHostContext={() => hostContextJson}
            onHostTool={handleHostTool}
            onRunComplete={handleRunComplete}
          />
        )}
      </div>
    </div>
  );
};

export default ConfiguratorAgentDock;
