import React, { useCallback, useContext, useEffect, useRef, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import ConversationEditorEmbedded from '../../message/ConversationEditorEmbedded';
import AgentUiChatHost from '../../aiskill/AgentUiChatHost';
import { genericAgentSvc } from '../../../webapi/genericAgentSvc';
import { FORM_AGENT_SKILL_KEY, FormAgentHostContext, type FormAgentHostSlots } from './formAgentHost';
import { FORM_CONFIG_SKILL_KEY, buildFormConfigContext, buildFormConfigMultiContext } from './formConfigHost';
import { FormGroupAgentHostsContext, FormGroupDockSlotContext, formGroupDockShowsNav } from './formGroupDock';

type TabId = 'messages' | 'ai' | 'configurator';

interface Props {
  showMessages: boolean;
  showAgent: boolean;
  showConfigurator?: boolean;
  configTransaction?: any;
  configFormKind?: 'master' | 'list';
  onConfigReload?: () => void;
  transactionId: string;
  messageRootId?: string;
  messageScopeType?: number | null;
  chatScope: string;
  initialTab?: TabId;
  onHidePanel?: () => void;
  dockPlacement?: 'right' | 'bottom';
}

const FormSidePanel: React.FC<Props> = ({
  showMessages,
  showAgent,
  showConfigurator = false,
  configTransaction,
  configFormKind = 'master',
  onConfigReload,
  transactionId,
  messageRootId,
  messageScopeType,
  chatScope,
  initialTab,
  onHidePanel,
  dockPlacement = 'right',
}) => {
  const { theme, t } = useTheme();
  const { showConfirm } = useAlertConfirm();
  const pendingReloadPromptRef = useRef(false);
  const onConfigReloadRef = useRef(onConfigReload);
  onConfigReloadRef.current = onConfigReload;
  const hostRef = useContext(FormAgentHostContext);
  const formGroupAgentHostsRef = useContext(FormGroupAgentHostsContext);
  const formGroupDockEl = useContext(FormGroupDockSlotContext);
  const listFormHosts = (): Array<{ key: string; role: string; name?: string; slots: FormAgentHostSlots }> => {
    const map = formGroupAgentHostsRef?.current;
    if (map && map.size > 0) return Array.from(map.values());
    if (hostRef?.current) return [{ key: 'local', role: 'main', slots: hostRef.current }];
    return [];
  };
  const hostHeaderCls = formGroupDockShowsNav(formGroupDockEl)
    ? `flex shrink-0 items-center gap-1 px-2 py-1 text-xs border-b ${theme.mainHeader}`
    : `flex items-center gap-1 px-2 h-10 flex-shrink-0 box-border border-b ${t('border_mainContentSection')} ${theme.mainContentSection}`;

  const defaultTab: TabId = initialTab
    || (showAgent ? 'ai' : showConfigurator ? 'configurator' : 'messages');
  const [tab, setTab] = useState<TabId>(defaultTab);
  const [chatKey, setChatKey] = useState<string | null>(null);
  const [configChatKey, setConfigChatKey] = useState<string | null>(null);
  const storageKey = `formSideTab:${transactionId || 'unknown'}`;

  useEffect(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved === 'messages' && showMessages) setTab('messages');
      else if (saved === 'ai' && showAgent) setTab('ai');
      else if (saved === 'configurator' && showConfigurator) setTab('configurator');
    } catch {
      /* ignore */
    }
  }, [storageKey, showMessages, showAgent, showConfigurator]);

  useEffect(() => {
    try { localStorage.setItem(storageKey, tab); } catch { /* ignore */ }
  }, [storageKey, tab]);

  useEffect(() => {
    if (!showAgent || !transactionId || !chatScope) return;
    let cancelled = false;
    genericAgentSvc.EnsureFormHostChat(Number(transactionId), chatScope)
      .then((key) => { if (!cancelled) setChatKey(key); })
      .catch(() => { if (!cancelled) setChatKey(null); });
    return () => { cancelled = true; };
  }, [showAgent, transactionId, chatScope]);

  useEffect(() => {
    if (!showConfigurator || !transactionId || !chatScope) return;
    let cancelled = false;
    genericAgentSvc.EnsureFormConfigChat(Number(transactionId), `config:${chatScope}`)
      .then((key) => { if (!cancelled) setConfigChatKey(key); })
      .catch(() => { if (!cancelled) setConfigChatKey(null); });
    return () => { cancelled = true; };
  }, [showConfigurator, transactionId, chatScope]);

  const tabCount = [showMessages, showAgent, showConfigurator].filter(Boolean).length;
  const both = tabCount > 1;
  const isBottom = dockPlacement === 'bottom';
  const showMessagesPane = showMessages && (!both || tab === 'messages');
  const showAgentPane = showAgent && (!both || tab === 'ai');
  const showConfigPane = showConfigurator && (!both || tab === 'configurator');
  const showHostHeader = both || (isBottom && !!onHidePanel) || (showConfigurator && !showMessages && !showAgent);

  const hideBtn = onHidePanel ? (
    <button
      type="button"
      className={`ml-auto w-6 h-6 shrink-0 rounded-[4px] text-xs ${theme.button_default}`}
      title="Collapse panel"
      aria-label="Collapse panel"
      onClick={onHidePanel}
    >
      <i className={`fa-solid ${isBottom ? 'fa-chevron-down' : 'fa-angles-right'}`} aria-hidden />
    </button>
  ) : null;

  const tabBtn = (id: TabId, label: string) => (
    <button
      type="button"
      className={`px-2 py-1 text-xs rounded-[4px] ${tab === id ? `${theme.tab_active} font-medium` : theme.tab}`}
      onClick={() => setTab(id)}
    >
      {label}
    </button>
  );

  const handleFormAgentHostTool = async (toolName: string, args: any) => {
    const hosts = listFormHosts();
    if (hosts.length === 0) return { ok: false, error: 'This tool only runs inside an open transaction form.' };
    const gridNotOnForm = (err: unknown) => /grid unit was not found/i.test(String(err || ''));
    const hostOwnsGrid = (slots: FormAgentHostSlots) => {
      const ctx: any = slots.getContext?.();
      const units = Array.isArray(ctx?.units) ? ctx.units : [];
      const raw = args?.unitId ?? args?.UnitId;
      if (raw == null || String(raw).trim() === '') {
        return units.some((u: any) => Number(u?.level) === 2 || Number(u?.level) === 3);
      }
      const key = String(raw).trim().toLowerCase();
      return units.some((u: any) => {
        if (Number(u?.level) !== 2 && Number(u?.level) !== 3) return false;
        if (String(u?.unitId ?? '') === String(raw)) return true;
        const path = String(u?.path || '').toLowerCase();
        return path === key || path.endsWith(key) || path.split('/').pop()?.trim() === key;
      });
    };
    const callHosts = async (
      targets: typeof hosts,
      fn: (slots: FormAgentHostSlots) => Promise<unknown> | unknown,
    ) => {
      const results: any[] = [];
      for (const h of targets) {
        const raw = { formRole: h.role, formName: h.name, ...(await Promise.resolve(fn(h.slots)) as any) };
        if (toolName === 'update_grid' && raw?.ok === false && gridNotOnForm(raw.error)) continue;
        results.push(raw);
      }
      const ok = results.some((r) => r?.ok === true);
      if (results.length === 0) {
        return { ok: false, error: 'Grid unit was not found. Use unitId or the unit name from HostContextJson.' };
      }
      return results.length === 1 ? results[0] : { ok, results };
    };
    if (toolName === 'get_form_state' || toolName === 'set_fields' || toolName === 'update_grid') {
      const targets = toolName === 'update_grid' && hosts.length > 1
        ? (() => {
            const owned = hosts.filter((h) => hostOwnsGrid(h.slots));
            return owned.length ? owned : hosts;
          })()
        : hosts;
      return callHosts(targets, (s) => s.applyTool?.(toolName, args)
        ?? (toolName === 'set_fields' ? s.setFields?.(args) : toolName === 'update_grid' ? s.updateGrid?.(args) : { ok: false, error: `${toolName} is not available.` })
        ?? { ok: false, error: `${toolName} is not available.` });
    }
    if (toolName === 'describe_action') {
      for (const h of hosts) {
        const r: any = h.slots.describeAction?.(args);
        if (r?.ok) return r;
      }
      return { ok: false, error: 'Action not found on this form group.' };
    }
    const primary = hosts.find((h) => h.role === 'main')?.slots ?? hosts[0].slots;
    if (toolName === 'open_linked_search') return primary.openLinkedSearch?.(args) ?? { ok: false, error: 'open_linked_search is not available on this form.' };
    if (toolName === 'open_link_target') return primary.openLinkTarget?.(args) ?? { ok: false, error: 'open_link_target is not available on this form.' };
    if (toolName === 'run_command') return primary.runCommand?.(args) ?? { ok: false, error: 'run_command is not available on this form.' };
    return { ok: false, error: `${toolName} is not a form host tool.` };
  };

  const handleConfigHostTool = async (toolName: string, _args: any) => {
    if (toolName === 'request_form_reload') {
      if (!onConfigReloadRef.current) {
        return { ok: false, error: 'Reload is not wired on this form host.' };
      }
      // Do not block the agent turn on the dialog. Cache was already refreshed on save.
      // After the run completes (onRunComplete), show confirm → handleReloadCurrentForm.
      pendingReloadPromptRef.current = true;
      return { ok: true, deferredPrompt: true };
    }
    return { ok: false, error: `${toolName} is not a configurator host tool.` };
  };

  const handleConfigRunComplete = useCallback(() => {
    if (!pendingReloadPromptRef.current) return;
    pendingReloadPromptRef.current = false;
    const reload = onConfigReloadRef.current;
    if (!reload) return;
    void (async () => {
      const ok = await showConfirm(
        'Configuration has been changed. Reload the current form to apply it?',
        {
          title: 'Reload Form',
          confirmLabel: 'Reload',
          cancelLabel: 'Later',
        },
      );
      if (ok) {
        try { reload(); } catch { /* ignore */ }
      }
    })();
  }, [showConfirm]);

  return (
    <div className="flex h-full min-h-0 w-full flex-col overflow-hidden">
      {showHostHeader && (
        <div className={hostHeaderCls}>
          {both ? (
            <>
              {showMessages && tabBtn('messages', 'Conversation')}
              {showAgent && tabBtn('ai', 'Assistant')}
              {showConfigurator && tabBtn('configurator', 'Configurator')}
            </>
          ) : (
            <span className={`text-xs ${theme.label}`}>
              {showConfigurator ? 'Configurator' : showAgent ? 'Assistant' : 'Conversation'}
            </span>
          )}
          {hideBtn}
        </div>
      )}
      {showMessagesPane && messageScopeType != null && (
        <div className="h-1 min-h-0 w-full flex-auto overflow-hidden">
          <ConversationEditorEmbedded
            scopeType={messageScopeType}
            transactionId={transactionId}
            transactionRootValueId={messageRootId || ''}
            defaultMessageSubject="New Form Message"
            onHidePanel={!both && !isBottom ? onHidePanel : undefined}
            hostHeaderClassName={!both && !isBottom && onHidePanel ? hostHeaderCls : undefined}
            hideTitleHeader={both || isBottom}
          />
        </div>
      )}
      {showAgentPane && (
        <div className="h-1 min-h-0 w-full flex-auto overflow-hidden">
          {chatKey ? (
            <AgentUiChatHost
              skillKey={FORM_AGENT_SKILL_KEY}
              chatSessionKey={chatKey}
              showChatListButton={false}
              squareChrome
              onHidePanel={both || isBottom ? undefined : onHidePanel}
              getHostContext={() => {
                const hosts = listFormHosts();
                if (hosts.length === 0) return undefined;
                if (hosts.length === 1) {
                  const ctx = hosts[0].slots.getContext?.();
                  return ctx == null ? undefined : JSON.stringify(ctx);
                }
                return JSON.stringify({
                  mode: 'transactionFormGroup',
                  forms: hosts.map((h) => ({
                    formRole: h.role,
                    formName: h.name,
                    ...(typeof h.slots.getContext?.() === 'object' && h.slots.getContext?.() != null
                      ? (h.slots.getContext() as object)
                      : {}),
                  })),
                });
              }}
              onHostTool={handleFormAgentHostTool}
            />
          ) : (
            <div className={`p-3 text-xs ${theme.label}`}>Opening form agent…</div>
          )}
        </div>
      )}
      {showConfigPane && (
        <div className="h-1 min-h-0 w-full flex-auto overflow-hidden">
          {configChatKey ? (
            <AgentUiChatHost
              skillKey={FORM_CONFIG_SKILL_KEY}
              chatSessionKey={configChatKey}
              showChatListButton={false}
              squareChrome
              onHidePanel={both || isBottom ? undefined : onHidePanel}
              getHostContext={() => {
                const hosts = listFormHosts();
                if (hosts.length > 1) {
                  const forms = hosts.map((h) => {
                    const c: any = h.slots.getContext?.() ?? {};
                    return {
                      formRole: h.role ?? c.formRole,
                      formName: h.name ?? c.form,
                      transactionId: c.transactionId,
                      transactionName: c.form ?? h.name,
                    };
                  }).filter((f) => f.transactionId != null && String(f.transactionId).trim() !== '');
                  if (forms.length > 1) {
                    return JSON.stringify(buildFormConfigMultiContext(forms, {
                      saasApplicationId:
                        configTransaction?.SaasApplicationId
                        ?? configTransaction?.saasApplicationId
                        ?? null,
                    }));
                  }
                }
                const ctx = buildFormConfigContext({
                  mode: configFormKind,
                  transaction: configTransaction,
                });
                return JSON.stringify(ctx);
              }}
              onHostTool={handleConfigHostTool}
              onRunComplete={handleConfigRunComplete}
            />
          ) : (
            <div className={`p-3 text-xs ${theme.label}`}>Opening configurator…</div>
          )}
        </div>
      )}
    </div>
  );
};

export default FormSidePanel;
