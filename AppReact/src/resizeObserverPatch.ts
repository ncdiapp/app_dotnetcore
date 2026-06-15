/**
 * Mitigate Chromium "ResizeObserver loop completed with undelivered notifications".
 * - Defer observer callbacks to the next animation frame (reduces same-frame layout loops).
 * - Swallow the benign error so CRA dev overlay does not treat it as a runtime crash.
 */

const RESIZE_OBSERVER_LOOP_ERROR = 'ResizeObserver loop completed with undelivered notifications';

export function isResizeObserverLoopError(message: unknown): boolean {
  if (typeof message === 'string') {
    return message.includes(RESIZE_OBSERVER_LOOP_ERROR);
  }
  if (message && typeof message === 'object' && 'message' in message) {
    return isResizeObserverLoopError((message as { message?: unknown }).message);
  }
  return false;
}

function installResizeObserverDeferPatch(): void {
  if (typeof window === 'undefined' || typeof window.ResizeObserver === 'undefined') {
    return;
  }

  const OriginalResizeObserver = window.ResizeObserver;

  window.ResizeObserver = class ResizeObserverDeferred extends OriginalResizeObserver {
    constructor(callback: ResizeObserverCallback) {
      super((entries, observer) => {
        window.requestAnimationFrame(() => {
          callback(entries, observer);
        });
      });
    }
  };
}

function installResizeObserverErrorSuppression(): void {
  if (typeof window === 'undefined') {
    return;
  }

  // Capture phase runs before CRA react-error-overlay listeners.
  window.addEventListener(
    'error',
    (event) => {
      if (!isResizeObserverLoopError(event.message) && !isResizeObserverLoopError(event.error?.message)) {
        return;
      }
      event.stopImmediatePropagation();
      event.preventDefault();
    },
    true,
  );

  const originalOnError = window.onerror;
  window.onerror = (message, source, lineno, colno, error) => {
    if (isResizeObserverLoopError(message) || isResizeObserverLoopError(error?.message)) {
      return true;
    }
    if (originalOnError) {
      return originalOnError(message, source, lineno, colno, error);
    }
    return false;
  };

  window.addEventListener('unhandledrejection', (event) => {
    if (!isResizeObserverLoopError(event.reason?.message)) {
      return;
    }
    event.preventDefault();
  });
}

installResizeObserverDeferPatch();
installResizeObserverErrorSuppression();
