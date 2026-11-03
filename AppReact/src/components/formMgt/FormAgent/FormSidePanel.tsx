import React, { useContext, useEffect, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import ConversationEditorEmbedded from '../../message/ConversationEditorEmbedded';
import AgentUiChatHost from '../../aiskill/AgentUiChatHost';
import { genericAgentSvc } from '../../../webapi/genericAgentSvc';
import { FORM_AGENT_SKILL_KEY, FormAgentHostContext } from './formAgentHost';

type TabId = 'messages' | 'ai';

interface Props {
  showMessages: boolean;
  showAgent: boolean;
  transactionId: string;
  messageRootId?: string;
  messageScopeType?: number | null;
  chatScope: string;
  initialTab?: TabId;
}

const FormSidePanel: React.FC<Props> = ({
  showMessages,
  showAgent,
  transactionId,
  messageRootId,
  messageScopeType,
  chatScope,
  initialTab,
}) => {
  const { theme } = useTheme();
  const hostRef = useContext(FormAgentHostContext);
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
  const showMessagesPane = showMessages && (!both || tab === 'messages');
  const showAgentPane = showAgent && (!both || tab === 'ai');

  return (
    <div className="flex h-full min-h-0 w-full flex-col overflow-hidden">
      {both && (
        <div className={`flex shrink-0 gap-1 border-b px-2 py-1 ${theme.mainContentSection}`}>
          <button type="button" className={`px-2 py-1 text-xs rounded-[4px] ${tab === 'messages' ? theme.button_default : theme.label}`} onClick={() => setTab('messages')}>Messages</button>
          <button type="button" className={`px-2 py-1 text-xs rounded-[4px] ${tab === 'ai' ? theme.button_default : theme.label}`} onClick={() => setTab('ai')}>AI</button>
        </div>
      )}
      {showMessagesPane && messageScopeType != null && (
        <div className="h-1 min-h-0 w-full flex-auto overflow-hidden">
          <ConversationEditorEmbedded
            scopeType={messageScopeType}
            transactionId={transactionId}
            transactionRootValueId={messageRootId || ''}
            defaultMessageSubject="New Form Message"
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
              getHostContext={() => {
                const ctx = hostRef?.current.getContext?.();
                return ctx == null ? undefined : JSON.stringify(ctx);
              }}
              onHostTool={async (toolName, args) => {
                const slots = hostRef?.current;
                if (!slots) return { ok: false, error: 'This tool only runs inside an open transaction form.' };
                if (toolName === 'get_form_state') return { ok: true, form: slots.getContext?.() ?? null };
                if (toolName === 'describe_action') return slots.describeAction?.(args) ?? { ok: false, error: 'describe_action is not available.' };
                if (toolName === 'set_fields') return slots.setFields?.(args) ?? { ok: false, error: 'set_fields is not available.' };
                if (toolName === 'update_grid') return slots.updateGrid?.(args) ?? { ok: false, error: 'update_grid is not available.' };
                if (toolName === 'open_linked_search') return slots.openLinkedSearch?.(args) ?? { ok: false, error: 'open_linked_search is not available on this form.' };
                if (toolName === 'open_link_target') return slots.openLinkTarget?.(args) ?? { ok: false, error: 'open_link_target is not available on this form.' };
                if (toolName === 'run_command') return slots.runCommand?.(args) ?? { ok: false, error: 'run_command is not available on this form.' };
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
