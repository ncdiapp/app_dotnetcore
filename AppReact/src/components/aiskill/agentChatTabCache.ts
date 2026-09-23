import { store } from '../../redux/store';
import {
  getCurrentActiveTab,
  getDataModelFromCache,
  setDataModelToCache,
} from '../../redux/features/ui/navigation/tabnavSlice';
import type { GenericAgentChatUiSnapshot } from '../../webapi/genericAgentSvc';

export const AGENT_CHAT_PAGE_TYPE = 'generic-agent-chat' as const;

export type AgentChatTabCache = GenericAgentChatUiSnapshot & {
  pageType: typeof AGENT_CHAT_PAGE_TYPE;
  version: 1;
  tabKey: string;
};

export function getActiveTabKey(): string | null {
  return getCurrentActiveTab()?.tabKey ?? null;
}

export function agentChatCacheKey(
  tabKey: string,
  chatSessionKey?: string | null,
): string {
  return `${tabKey}|agent-chat|${chatSessionKey ?? ''}`;
}

export function saveAgentChatToTabCache(
  tabKey: string | null | undefined,
  snapshot: GenericAgentChatUiSnapshot,
): void {
  if (!tabKey) return;
  const payload: AgentChatTabCache = {
    ...snapshot,
    pageType: AGENT_CHAT_PAGE_TYPE,
    version: 1,
    tabKey,
  };
  store.dispatch(setDataModelToCache({
    dataModelKey: agentChatCacheKey(tabKey, snapshot.chatSessionKey),
    dataModel: payload,
  }));
}

export function loadAgentChatFromTabCache(
  tabKey: string | null | undefined,
  skillKey: string,
  chatSessionKey?: string | null,
): GenericAgentChatUiSnapshot | null {
  if (!tabKey) return null;
  const raw = getDataModelFromCache(agentChatCacheKey(tabKey, chatSessionKey)) as AgentChatTabCache | null;
  if (!raw || raw.pageType !== AGENT_CHAT_PAGE_TYPE) return null;
  if (raw.skillKey && skillKey && raw.skillKey !== skillKey) return null;
  const cachedChat = raw.chatSessionKey ?? null;
  const wantChat = chatSessionKey ?? null;
  if (cachedChat !== wantChat) return null;
  const hasUi =
    (raw.messages?.length ?? 0) > 0
    || !!raw.pendingAskUser
    || !!raw.pendingPlan
    || raw.isRunning;
  if (!hasUi) return null;
  return raw;
}

export function clearAgentChatTabCache(
  tabKey: string | null | undefined,
  chatSessionKey?: string | null,
): void {
  if (!tabKey) return;
  const key = agentChatCacheKey(tabKey, chatSessionKey);
  const raw = getDataModelFromCache(key) as AgentChatTabCache | null;
  if (raw?.pageType === AGENT_CHAT_PAGE_TYPE) {
    store.dispatch(setDataModelToCache({ dataModelKey: key, dataModel: null }));
  }
}
