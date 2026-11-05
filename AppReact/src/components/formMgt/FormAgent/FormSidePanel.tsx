import React, { useContext, useEffect, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import ConversationEditorEmbedded from '../../message/ConversationEditorEmbedded';
import AgentUiChatHost from '../../aiskill/AgentUiChatHost';
import { genericAgentSvc } from '../../../webapi/genericAgentSvc';
import { FORM_AGENT_SKILL_KEY, FormAgentHostContext, type FormAgentHostSlots } from './formAgentHost';
import { FormGroupAgentHostsContext, FormGroupDockSlotContext, formGroupDockShowsNav } from './formGroupDock';

type TabId = 'messages' | 'ai';

interface Props {
  showMessages: boolean;
  showAgent: boolean;
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
  transactionId,
  messageRootId,
  messageScopeType,
  chatScope,
  initialTab,
  onHidePanel,
  dockPlacement = 'right',
}) => {
  const { theme, t } = useTheme();
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
  const [tab, setTab] = useState<TabId>(initialTab || (showMessages ? 'messages' : 'ai'));
  const [chatKey, setChatKey] = useState<string | null>(null);
  const storageKey = `formSideTab:${transactionId || 'unknown'}`;

  useEffect(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved === 'messages' && showMessages) setTab('messages');
      else if (saved === 'ai' && showAgent) setTab('ai');
    } catch {
      /* ignore */
    }
  }, [storageKey, showMessages, showAgent]);

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

  const both = showMessages && showAgent;
  const isBottom = dockPlacement === 'bottom';
  const showMessagesPane = showMessages && (!both || tab === 'messages');
  const showAgentPane = showAgent && (!both || tab === 'ai');
  const showHostHeader = both || (isBottom && !!onHidePanel);

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

  return (
    <div className="flex h-full min-h-0 w-full flex-col overflow-hidden">
      {showHostHeader && (
        <div className={hostHeaderCls}>
          {both ? (
            <>
              <button type="button" className={`px-2 py-1 text-xs rounded-[4px] ${tab === 'messages' ? `${theme.tab_active} font-medium` : theme.tab}`} onClick={() => setTab('messages')}>Conversation</button>
              <button type="button" className={`px-2 py-1 text-xs rounded-[4px] ${tab === 'ai' ? `${theme.tab_active} font-medium` : theme.tab}`} onClick={() => setTab('ai')}>Assistant</button>
            </>
          ) : (
            <span className={`text-xs ${theme.label}`}>{showMessages ? 'Conversation' : 'Assistant'}</span>
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
              onHostTool={async (toolName, args) => {
                const hosts = listFormHosts();
                if (hosts.length === 0) return { ok: false, error: 'This tool only runs inside an open transaction form.' };
                const callAll = async (fn: (slots: FormAgentHostSlots) => Promise<unknown> | unknown) => {
                  const results = [];
                  for (const h of hosts) {
                    results.push({ formRole: h.role, formName: h.name, ...(await Promise.resolve(fn(h.slots)) as any) });
                  }
                  return { ok: results.some((r: any) => r?.ok === true), results };
                };
                if (toolName === 'get_form_state' || toolName === 'set_fields' || toolName === 'update_grid') {
                  return callAll((s) => s.applyTool?.(toolName, args)
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
              }}
            />
          ) : (
            <div className={`p-3 text-xs ${theme.label}`}>Opening form agent…</div>
          )}
        </div>
      )}
    </div>
  );
};

export default FormSidePanel;
