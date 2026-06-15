/**
 * Mitigate Chromium "ResizeObserver loop completed with undelivered notifications".
 * - Defer observer callbacks (double rAF + debounce) to reduce same-frame layout loops.
 * - Swallow the benign error so CRA dev overlay does not treat it as a runtime crash.
 */

const RESIZE_OBSERVER_LOOP_PREFIX = 'ResizeObserver loop';

export function isResizeObserverLoopError(message: unknown): boolean {
  if (typeof message === 'string') {
    return message.includes(RESIZE_OBSERVER_LOOP_PREFIX);
  }
  if (message && typeof message === 'object' && 'message' in message) {
    return isResizeObserverLoopError((message as { message?: unknown }).message);
  }
  return false;
}

function dismissDevErrorOverlay(): void {
  const hook = (window as Window & { __REACT_ERROR_OVERLAY_GLOBAL_HOOK__?: { dismissOverlay?: () => void } })
    .__REACT_ERROR_OVERLAY_GLOBAL_HOOK__;
  hook?.dismissOverlay?.();

  document.getElementById('webpack-dev-server-client-overlay')?.remove();
  document.getElementById('webpack-dev-server-client-overlay-div')?.remove();
}

function installResizeObserverDeferPatch(): void {
  if (typeof window === 'undefined' || typeof window.ResizeObserver === 'undefined') {
    return;
  }

  const OriginalResizeObserver = window.ResizeObserver;
  if ((OriginalResizeObserver as { __appAiDeferred?: boolean }).__appAiDeferred) {
    return;
  }

  window.ResizeObserver = class ResizeObserverDeferred extends OriginalResizeObserver {
    constructor(callback: ResizeObserverCallback) {
      let rafId = 0;
      super((entries, observer) => {
        if (rafId) {
          cancelAnimationFrame(rafId);
        }
        rafId = requestAnimationFrame(() => {
          rafId = requestAnimationFrame(() => {
            rafId = 0;
            try {
              callback(entries, observer);
            } catch (error) {
              if (!isResizeObserverLoopError(error instanceof Error ? error.message : String(error))) {
                throw error;
              }
            }
          });
        });
      });
    }
  };

  (window.ResizeObserver as { __appAiDeferred?: boolean }).__appAiDeferred = true;
}

function suppressResizeObserverRuntimeError(): void {
  dismissDevErrorOverlay();
}

function installResizeObserverErrorSuppression(): void {
  if (typeof window === 'undefined') {
    return;
  }

  const handleResizeObserverError = (message: unknown, error?: unknown): boolean => {
    if (!isResizeObserverLoopError(message) && !isResizeObserverLoopError(error)) {
      return false;
    }
    suppressResizeObserverRuntimeError();
    return true;
  };

  // Capture phase runs before CRA react-error-overlay listeners.
  window.addEventListener(
    'error',
    (event) => {
      if (!handleResizeObserverError(event.message, event.error?.message)) {
        return;
      }
      event.stopImmediatePropagation();
      event.preventDefault();
    },
    true,
  );

  const originalOnError = window.onerror;
  window.onerror = (message, source, lineno, colno, error) => {
    if (handleResizeObserverError(message, error?.message)) {
      return true;
    }
    if (originalOnError) {
      return originalOnError(message, source, lineno, colno, error);
    }
    return false;
  };

  window.addEventListener('unhandledrejection', (event) => {
    if (!handleResizeObserverError(event.reason, event.reason?.message)) {
      return;
    }
    event.preventDefault();
    suppressResizeObserverRuntimeError();
  });
}

function installReactErrorOverlayHookPatch(): void {
  if (typeof window === 'undefined') {
    return;
  }

  const patchHook = (): boolean => {
    const hook = (window as Window & {
      __REACT_ERROR_OVERLAY_GLOBAL_HOOK__?: Record<string, unknown> & { __appAiResizeObserverPatched?: boolean };
    }).__REACT_ERROR_OVERLAY_GLOBAL_HOOK__;

    if (!hook || hook.__appAiResizeObserverPatched) {
      return !!hook?.__appAiResizeObserverPatched;
    }

    (['onError', 'onUnhandledError', 'onUnhandledRejection'] as const).forEach((key) => {
      const original = hook[key];
      if (typeof original !== 'function') {
        return;
      }
      hook[key] = (...args: unknown[]) => {
        const error = args[0];
        if (isResizeObserverLoopError(error) || isResizeObserverLoopError((error as Error | undefined)?.message)) {
          suppressResizeObserverRuntimeError();
          return;
        }
        return (original as (...innerArgs: unknown[]) => unknown).apply(hook, args);
      };
    });

    hook.__appAiResizeObserverPatched = true;
    return true;
  };

  if (patchHook()) {
    return;
  }

  const intervalId = window.setInterval(() => {
    if (patchHook()) {
      window.clearInterval(intervalId);
    }
  }, 50);

  window.setTimeout(() => window.clearInterval(intervalId), 15000);
}

installResizeObserverDeferPatch();
installResizeObserverErrorSuppression();
installReactErrorOverlayHookPatch();
