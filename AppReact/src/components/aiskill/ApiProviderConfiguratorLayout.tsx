/**
 * Wraps an API MANAGEMENT page with an optional right Configurator dock
 * when tenant AI is configured. Children keep their existing layout.
 */
import React, { useState } from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { useTheme } from '../../redux/hooks/useTheme';
import ApiProviderConfiguratorDock, {
  ApiProviderHostContext,
} from './ApiProviderConfiguratorDock';

export type ApiProviderConfiguratorLayoutProps = {
  scope: string;
  banner?: string;
  hostContext: ApiProviderHostContext;
  onReload?: () => void | Promise<void>;
  isDirty?: boolean;
  onSaveDirty?: () => Promise<boolean>;
  /** Default dock open when AI configured (default true). */
  defaultOpen?: boolean;
  children: React.ReactNode;
};

const ApiProviderConfiguratorLayout: React.FC<ApiProviderConfiguratorLayoutProps> = ({
  scope,
  banner,
  hostContext,
  onReload,
  isDirty,
  onSaveDirty,
  defaultOpen = true,
  children,
}) => {
  const { theme } = useTheme();
  const isAiConfigured = useSelector(
    (s: RootState) =>
      !!(s.userSession?.userContext?.IsAiConfigured ?? (s.userSession?.userContext as any)?.isAiConfigured),
  );
  const [showAgentDock, setShowAgentDock] = useState(defaultOpen);

  if (!isAiConfigured) {
    return <>{children}</>;
  }

  return (
    <div className="w-full h-full flex overflow-hidden">
      <div className="w-1 h-full flex-auto min-w-0 overflow-hidden">{children}</div>
      {showAgentDock ? (
        <div className="w-[360px] flex-none h-full overflow-hidden border-l">
          <ApiProviderConfiguratorDock
            scope={scope}
            banner={banner}
            hostContext={hostContext}
            onReload={onReload}
            isDirty={isDirty}
            onSaveDirty={onSaveDirty}
            onHidePanel={() => setShowAgentDock(false)}
          />
        </div>
      ) : (
        <button
          type="button"
          className={`flex-none w-9 h-full text-xs border-0 outline-none ${theme.mainContentSection} hover:opacity-80`}
          title="Show Configurator"
          aria-label="Show Configurator"
          onClick={() => setShowAgentDock(true)}
        >
          <i className="fa-solid fa-robot" aria-hidden />
        </button>
      )}
    </div>
  );
};

export default ApiProviderConfiguratorLayout;
