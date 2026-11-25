/**
 * Right-side Configurator chat for API MANAGEMENT hosts → app-api-configurator.
 * HostContext carries optional providerId / settingParameterId / actionCode / openApiDocId.
 */
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { genericAgentSvc } from '../../webapi/genericAgentSvc';
import AgentUiChatHost from './AgentUiChatHost';

export const API_PROVIDER_CONFIG_SKILL_KEY = 'app-api-configurator';

export type ApiProviderHostContext = {
  surface?: string;
  mode?: string;
  providerId?: number | null;
  providerName?: string | null;
  providerKind?: 'App' | 'ThirdParty' | string | null;
  settingParameterId?: number | null;
  actionCode?: string | null;
  openApiDocId?: number | null;
  openApiDocName?: string | null;
  saasApplicationId?: number | null;
};

export type ApiProviderConfiguratorDockProps = {
  /** Stable chat scope, e.g. provider:1, api:4401, openapi:3, list:app-api */
  scope: string;
  banner?: string;
  hostContext: ApiProviderHostContext;
  onReload?: () => void | Promise<void>;
  onHidePanel?: () => void;
  isDirty?: boolean;
  onSaveDirty?: () => Promise<boolean>;
};

const ApiProviderConfiguratorDock: React.FC<ApiProviderConfiguratorDockProps> = (props) => {
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

  const banner =
    props.banner ||
    props.hostContext.actionCode ||
    props.hostContext.providerName ||
    props.hostContext.openApiDocName ||
    props.scope;

  useEffect(() => {
    let cancelled = false;
    setChatKey(null);
    setError(null);
    (async () => {
      try {
        const key = await genericAgentSvc.EnsureApiProviderConfigChat(props.scope);
        if (!cancelled) setChatKey(key);
      } catch (e: any) {
        if (!cancelled) setError(e?.message || 'Could not open API Provider Configurator chat.');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [props.scope]);

  const hostContextJson = useMemo(() => {
    const h = props.hostContext || {};
    return JSON.stringify({
      surface: h.surface || 'api-management',
      mode: h.mode || 'api-configurator',
      providerId: h.providerId ?? undefined,
      providerName: h.providerName || undefined,
      providerKind: h.providerKind || undefined,
      settingParameterId: h.settingParameterId ?? undefined,
      actionCode: h.actionCode || undefined,
      openApiDocId: h.openApiDocId ?? undefined,
      openApiDocName: h.openApiDocName || undefined,
      saasApplicationId: h.saasApplicationId ?? undefined,
    });
  }, [props.hostContext]);

  const handleHostTool = useCallback(async (toolName: string, _args: any) => {
    if (toolName === 'request_api_mgmt_reload') {
      if (!onReloadRef.current) {
        return { ok: false, error: 'Reload is not wired on this host.' };
      }
      pendingReloadRef.current = true;
      return { ok: true, deferredPrompt: true };
    }
    return { ok: false, error: `${toolName} is not an API Management host tool.` };
  }, []);

  const handleRunComplete = useCallback(() => {
    if (!pendingReloadRef.current) return;
    pendingReloadRef.current = false;
    const reload = onReloadRef.current;
    if (!reload) return;
    void (async () => {
      if (isDirty) {
        const discardAndReload = await showConfirm(
          'Configurator updated the database, but this editor still has unsaved local changes. Discard local edits and reload from the database (recommended). Saving local first can overwrite agent changes.',
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
        'Configuration has been changed. Reload this API Management page to apply it?',
        {
          title: 'Reload API Management',
          confirmLabel: 'Reload',
          cancelLabel: 'Later',
        },
      );
      if (ok) await reload();
    })();
  }, [showConfirm, showAlert, isDirty]);

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
            skillKey={API_PROVIDER_CONFIG_SKILL_KEY}
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

export default ApiProviderConfiguratorDock;
