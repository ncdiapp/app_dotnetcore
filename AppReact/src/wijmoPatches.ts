/**
 * Guard Wijmo grid against null refs when queued microtasks run after dispose/re-render.
 * - editRange: DirectiveCellFactoryBase._autoSizeIfRequired runs in queueMicrotask; guard when this.grid or editRange is invalid.
 * - finishEditing: FlexGrid.refresh() calls finishEditing(); guard when internal editor ref is null.
 */

import * as wjcInteropGrid from '@mescius/wijmo.interop.grid';
import * as wjcGrid from '@mescius/wijmo.grid';

// 1) Guard _autoSizeIfRequired (editRange error)
const interopProto = (wjcInteropGrid as { DirectiveCellFactoryBase?: { prototype: unknown } })
  .DirectiveCellFactoryBase?.prototype as { _autoSizeIfRequired?: () => void } | undefined;

if (interopProto && typeof interopProto._autoSizeIfRequired === 'function') {
  const orig = interopProto._autoSizeIfRequired;
  interopProto._autoSizeIfRequired = function (this: { grid: unknown }) {
    try {
      if (!this.grid) return;
      return orig.call(this);
    } catch {
      // grid disposed or editRange getter threw
    }
  };
}

// 2) Guard FlexGrid.finishEditing (finishEditing error when refresh() runs after dispose)
const flexGridProto = (wjcGrid as { FlexGrid?: { prototype: unknown } }).FlexGrid?.prototype as {
  finishEditing?: (cancel?: boolean) => boolean;
  refresh?: (fullUpdate?: boolean) => void;
} | undefined;

if (flexGridProto && typeof flexGridProto.finishEditing === 'function') {
  const origFinish = flexGridProto.finishEditing;
  flexGridProto.finishEditing = function (this: unknown, cancel?: boolean) {
    try {
      return origFinish.call(this, cancel);
    } catch {
      return false;
    }
  };
}

// 3) Guard FlexGrid.refresh (scrollPosition setter can read host.scrollLeft after dispose)
if (flexGridProto && typeof flexGridProto.refresh === 'function') {
  const origRefresh = flexGridProto.refresh;
  flexGridProto.refresh = function (this: unknown, fullUpdate?: boolean) {
    try {
      return origRefresh.call(this, fullUpdate);
    } catch (e: any) {
      const msg = String(e?.message ?? '');
      if (msg.includes('scrollLeft') || msg.includes('scrollPosition')) {
        return;
      }
      throw e;
    }
  };
}
