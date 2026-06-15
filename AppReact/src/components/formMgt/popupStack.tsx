import React, { createContext, useContext, type CSSProperties, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

/** Default floor for the first popup when no parent layer exists. */
export const POPUP_Z_BASE = 10000;
/** Each nested popup stacks this many levels above its parent. */
export const POPUP_Z_STEP = 10;

const PopupStackContext = createContext(POPUP_Z_BASE - POPUP_Z_STEP);

/**
 * Resolve z-index for a new popup: always strictly above the nearest parent popup.
 * Pass `explicitZIndex` only as a minimum floor (e.g. legacy constants); nesting still wins.
 */
export function usePopupZIndex(explicitZIndex?: number): number {
  const parentZ = useContext(PopupStackContext);
  const nestedZ = parentZ + POPUP_Z_STEP;
  if (explicitZIndex == null || !Number.isFinite(explicitZIndex)) {
    return nestedZ;
  }
  return Math.max(explicitZIndex, nestedZ);
}

/** Provides popup stack context to descendants (including portaled children). */
export function PopupStackLayer({ zIndex, children }: { zIndex: number; children: ReactNode }) {
  return <PopupStackContext.Provider value={zIndex}>{children}</PopupStackContext.Provider>;
}

export type PopupModalOverlayProps = {
  children: ReactNode;
  /** Optional minimum z-index; actual value is max(this, parent + step). */
  zIndex?: number;
  className?: string;
  backdropClassName?: string;
  onBackdropClick?: () => void;
  style?: CSSProperties;
};

/** Centered modal overlay (portaled). Uses popup stack for z-index. */
export function PopupModalOverlay({
  children,
  zIndex: explicitZIndex,
  className = '',
  backdropClassName = 'bg-black/40',
  onBackdropClick,
  style,
}: PopupModalOverlayProps) {
  const resolvedZ = usePopupZIndex(explicitZIndex);

  if (typeof document === 'undefined') return null;

  return createPortal(
    <PopupStackLayer zIndex={resolvedZ}>
      <div
        className={`fixed inset-0 flex items-center justify-center ${backdropClassName} ${className}`}
        style={{ zIndex: resolvedZ, ...style }}
        onClick={onBackdropClick}
        role="presentation"
      >
        {children}
      </div>
    </PopupStackLayer>,
    document.body,
  );
}
