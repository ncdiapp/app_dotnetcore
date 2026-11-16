/**
 * My Application Editor multi-agent chat (design §11).
 * App / list → app-builder (Assistant)
 * TX / Template open → app-configurator (Configurator)
 * Search open → app-search-view-config (Configurator)
 * Entity open → app-builder
 */
import React, { useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import AgentUiChatHost from '../../aiskill/AgentUiChatHost';
import { genericAgentSvc } from '../../../webapi/genericAgentSvc';

export type EditorAgentFocus =
  | { kind: 'app' }
  | { kind: 'transaction'; transactionId: number; transactionName?: string }
  | { kind: 'template'; transactionId: number; templateName?: string }
  | { kind: 'search'; searchId: number; searchViewId?: number | null; dataSetId?: number | null; searchName?: string }
  | { kind: 'entity'; entityId?: number; entityName?: string };

const SKILL = {
  builder: 'app-builder',
  configurator: 'app-configurator',
  searchViewConfig: 'app-search-view-config',
} as const;

export interface MyApplicationEditorAgentDockProps {
  saasApplicationId: number;
  applicationName?: string | null;
  sectionId: number;
  sectionName?: string;
  focus?: EditorAgentFocus | null;
  onHidePanel?: () => void;
}

function resolveRoute(
  focus: EditorAgentFocus | null | undefined
): {
  skillKey: string;
  label: 'Assistant' | 'Configurator';
  banner: string;
} {
  if (focus?.kind === 'transaction') {
    return {
      skillKey: SKILL.configurator,
      label: 'Configurator',
      banner: `Configurator · TX: ${focus.transactionName || focus.transactionId}`,
    };
  }
  if (focus?.kind === 'template') {
    return {
      skillKey: SKILL.configurator,
      label: 'Configurator',
      banner: `Configurator · Template: ${focus.templateName || focus.transactionId}`,
    };
  }
  if (focus?.kind === 'search') {
    return {
      skillKey: SKILL.searchViewConfig,
      label: 'Configurator',
      banner: `Configurator · Search: ${focus.searchName || focus.searchId}`,
    };
  }
  // Entity / app / list — title is just Assistant (section lives in HostContext only)
  return {
    skillKey: SKILL.builder,
    label: 'Assistant',
    banner: 'Assistant',
  };
}

const MyApplicationEditorAgentDock: React.FC<MyApplicationEditorAgentDockProps> = ({
  saasApplicationId,
  applicationName,
  sectionId,
  sectionName,
  focus = null,
  onHidePanel,
}) => {
  const { theme } = useTheme();
  const route = useMemo(() => resolveRoute(focus), [focus]);
  const [chatKey, setChatKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // App-depth Assistant shares ONE session per saasApplicationId across all left-nav sections
  // (HostContext carries current section). Drill-in Configurator sessions are per TX/Search.
  useEffect(() => {
    let cancelled = false;
    setChatKey(null);
    setError(null);

    (async () => {
      try {
        let key = '';
        if (route.skillKey === SKILL.configurator && (focus?.kind === 'transaction' || focus?.kind === 'template')) {
          key = await genericAgentSvc.EnsureFormConfigChat(focus.transactionId, `editor:app:${saasApplicationId}`);
        } else if (route.skillKey === SKILL.searchViewConfig && focus?.kind === 'search') {
          key = await genericAgentSvc.EnsureSearchViewConfigChat(focus.searchId, `editor:app:${saasApplicationId}`);
        } else {
          key = await genericAgentSvc.EnsureAppBuilderEditorChat(
            saasApplicationId,
            `editor:app:${saasApplicationId}`
          );
        }
        if (!cancelled) setChatKey(key);
      } catch (e: any) {
        if (!cancelled) setError(e?.message || 'Could not open editor agent chat.');
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [saasApplicationId, route.skillKey, focus]);

  const hostContextJson = useMemo(() => {
    const base: Record<string, unknown> = {
      surface: 'myApplicationEditor',
      saasApplicationId,
      applicationName: applicationName || undefined,
      section: sectionName || String(sectionId),
      sectionId,
    };
    if (focus?.kind === 'transaction' || focus?.kind === 'template') {
      return JSON.stringify({
        ...base,
        mode: 'configurator',
        editorKind: focus.kind,
        transactionId: focus.transactionId,
      });
    }
    if (focus?.kind === 'search') {
      return JSON.stringify({
        ...base,
        mode: 'search-view-config',
        searchId: focus.searchId,
        searchViewId: focus.searchViewId ?? undefined,
        dataSetId: focus.dataSetId ?? undefined,
      });
    }
    if (focus?.kind === 'entity') {
      return JSON.stringify({
        ...base,
        mode: 'appBuilder',
        selection: { entityId: focus.entityId, entityName: focus.entityName },
      });
    }
    return JSON.stringify({ ...base, mode: 'appBuilder', selection: null });
  }, [saasApplicationId, applicationName, sectionId, sectionName, focus]);

  return (
    <div className={`h-full w-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex-none px-2 py-1.5 text-xs border-b ${theme.mainContentSection} ${theme.label}`}>
        {route.label === 'Assistant' ? (
          <span className={`font-medium ${theme.title}`}>Assistant</span>
        ) : (
          <>
            <span className={`font-medium ${theme.title}`}>{route.label}</span>
            <span className="mx-1">·</span>
            <span>{route.banner.replace(/^(Assistant|Configurator) · /, '')}</span>
          </>
        )}
      </div>
      <div className="h-1 flex-auto min-h-0 overflow-hidden">
        {error && <div className={`p-3 text-xs ${theme.label}`}>{error}</div>}
        {!error && !chatKey && <div className={`p-3 text-xs ${theme.label}`}>Opening agent…</div>}
        {!error && chatKey && (
          <AgentUiChatHost
            skillKey={route.skillKey}
            chatSessionKey={chatKey}
            showChatListButton={false}
            squareChrome
            onHidePanel={onHidePanel}
            getHostContext={() => hostContextJson}
          />
        )}
      </div>
    </div>
  );
};

export default MyApplicationEditorAgentDock;
