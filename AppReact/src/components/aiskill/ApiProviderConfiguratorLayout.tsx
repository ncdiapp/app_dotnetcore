/**
 * Wraps an API MANAGEMENT page with an optional right Configurator dock
 * when tenant AI is configured. Children keep their existing layout.
 * Dock width is drag-resizable (same pattern as Form Builder / My Application Editor).
 */
import React, { useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { useTheme } from '../../redux/hooks/useTheme';
import ApiProviderConfiguratorDock, {
  ApiProviderHostContext,
} from './ApiProviderConfiguratorDock';

const CONFIG_DOCK_DEFAULT = 360;
const CONFIG_DOCK_MIN = 280;
const CONFIG_DOCK_MAX = 720;

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
  const [configDockWidthPx, setConfigDockWidthPx] = useState(CONFIG_DOCK_DEFAULT);
  const contentDockSplitRef = useRef<HTMLDivElement | null>(null);
  const configDockResizeDragRef = useRef(false);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!configDockResizeDragRef.current || !contentDockSplitRef.current) return;
      const rect = contentDockSplitRef.current.getBoundingClientRect();
      // Dragging the left edge of the dock: width = containerRight - mouseX
      const next = rect.right - e.clientX;
      const maxDock = Math.max(CONFIG_DOCK_MIN, rect.width - 280);
      setConfigDockWidthPx(Math.min(Math.max(next, CONFIG_DOCK_MIN), Math.min(CONFIG_DOCK_MAX, maxDock)));
    };
    const onUp = () => {
      if (!configDockResizeDragRef.current) return;
      configDockResizeDragRef.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, []);

  if (!isAiConfigured) {
    return <>{children}</>;
  }

  return (
    <div ref={contentDockSplitRef} className="w-full h-full flex overflow-hidden">
      <div className="w-1 h-full flex-auto min-w-0 overflow-hidden">{children}</div>
      {showAgentDock ? (
        <>
          <div
            className={`w-[6px] shrink-0 cursor-col-resize border-l border-r self-stretch min-h-0 ${theme.inputBox} ${theme.mainContentSection}`}
            role="separator"
            aria-orientation="vertical"
            aria-label="Resize Configurator"
            title="Drag to resize"
            onMouseDown={(e) => {
              e.preventDefault();
              configDockResizeDragRef.current = true;
              document.body.style.cursor = 'col-resize';
              document.body.style.userSelect = 'none';
            }}
          />
          <div
            className="flex-none h-full overflow-hidden border-l"
            style={{ width: configDockWidthPx, minWidth: CONFIG_DOCK_MIN }}
          >
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
        </>
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
