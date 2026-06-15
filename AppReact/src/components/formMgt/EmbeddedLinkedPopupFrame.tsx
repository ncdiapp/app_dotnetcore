import React, { useEffect, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { PopupStackLayer, usePopupZIndex } from './popupStack';

export type EmbeddedLinkedPopupFrameProps = {
  /** Minimum z-index; nested popups auto-stack above parent via popup stack. */
  zIndex?: number;
  title: string;
  /** Optional; e.g. Confirm &amp; Close for embedded add/edit forms only (omit for preview/search). */
  toolbarLeading?: ReactNode;
  /** Toolbar actions before the fullscreen control (when fullscreenPosition is afterTrailing). */
  toolbarTrailing?: ReactNode;
  /** Toolbar actions after the fullscreen control (e.g. Close). Only used with fullscreenPosition afterTrailing. */
  toolbarTrailingEnd?: ReactNode;
  children: ReactNode;
  maximizable?: boolean;
  /** Where the fullscreen icon should render in the toolbar. */
  fullscreenPosition?: 'between' | 'afterTrailing';
  /** Bump when opening a new popup so maximized state resets. */
  frameInstanceKey?: string | number;
  /** Non-maximized width (default `80vw`). Example: `1000px`. */
  defaultWidth?: string;
  /** Non-maximized height (default `80vh`). */
  defaultHeight?: string;
};

/**
 * Linked search / embedded route popups: 80vw×80vh by default, dim backdrop (does not close on backdrop click),
 * optional maximize to fill the viewport.
 */
export function EmbeddedLinkedPopupFrame({
  zIndex,
  title,
  toolbarLeading,
  toolbarTrailing,
  toolbarTrailingEnd,
  children,
  maximizable = true,
  fullscreenPosition = 'between',
  frameInstanceKey = 0,
  defaultWidth,
  defaultHeight,
}: EmbeddedLinkedPopupFrameProps) {
  const { theme } = useTheme();
  const [maximized, setMaximized] = useState(false);
  const resolvedZIndex = usePopupZIndex(zIndex);

  useEffect(() => {
    setMaximized(false);
  }, [frameInstanceKey, resolvedZIndex]);

  if (typeof document === 'undefined') return null;

  const useCustomSize = !!(defaultWidth || defaultHeight);
  const panelSizeStyle: React.CSSProperties | undefined =
    !maximized && useCustomSize
      ? {
          width: defaultWidth ?? '80vw',
          height: defaultHeight ?? '80vh',
          maxWidth: '95vw',
          maxHeight: '90vh',
        }
      : undefined;

  return createPortal(
    <div
      className="fixed inset-0 flex items-center justify-center"
      style={{ zIndex: resolvedZIndex }}
      role="dialog"
      aria-modal="true"
      aria-labelledby="embedded-linked-popup-title"
    >
      {/* Backdrop — visual only; do not close when clicked */}
      <div className="absolute inset-0 bg-black/40" aria-hidden />

      <div
        className={`z-10 flex flex-col overflow-hidden border shadow-xl ${theme.mainContentSection} ${
          maximized
            ? 'absolute inset-0 h-full w-full rounded-none'
            : useCustomSize
              ? 'relative rounded-md'
              : 'relative h-[80vh] w-[80vw] max-h-[90vh] max-w-[95vw] rounded-md'
        }`}
        style={panelSizeStyle}
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className={`flex flex-none items-center justify-between gap-2 border-b px-3 py-2 ${theme.mainContentSection}`}>
          <div
            id="embedded-linked-popup-title"
            className={`min-w-0 truncate text-sm font-semibold ${theme.title}`}
            title={title}
          >
            {title}
          </div>
          <div className="flex shrink-0 items-center gap-2">
            {toolbarLeading}
            {maximizable && fullscreenPosition === 'between' && (
              <button
                type="button"
                className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                onClick={() => setMaximized((m) => !m)}
                title={maximized ? 'Exit full screen' : 'Full screen'}
                aria-label={maximized ? 'Exit full screen' : 'Full screen'}
              >
                <i className={`fa-solid ${maximized ? 'fa-compress' : 'fa-expand'}`} aria-hidden />
              </button>
            )}
            {toolbarTrailing}
            {maximizable && fullscreenPosition === 'afterTrailing' && (
              <button
                type="button"
                className={`rounded-[4px] p-1.5 ${theme.button_default}`}
                onClick={() => setMaximized((m) => !m)}
                title={maximized ? 'Exit full screen' : 'Full screen'}
                aria-label={maximized ? 'Exit full screen' : 'Full screen'}
              >
                <i className={`fa-solid ${maximized ? 'fa-compress' : 'fa-expand'}`} aria-hidden />
              </button>
            )}
            {toolbarTrailingEnd}
          </div>
        </div>
        <div className="h-1 w-full min-h-0 flex-auto overflow-hidden">
          <PopupStackLayer zIndex={resolvedZIndex}>{children}</PopupStackLayer>
        </div>
      </div>
    </div>,
    document.body
  );
}
