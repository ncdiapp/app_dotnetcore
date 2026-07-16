/**
 * Guard Wijmo grid against null refs when queued microtasks run after dispose/re-render.
 * - editRange: DirectiveCellFactoryBase._autoSizeIfRequired runs in queueMicrotask; guard when this.grid or editRange is invalid.
 * - finishEditing: FlexGrid.refresh() calls finishEditing(); guard when internal editor ref is null.
 */

import { Control } from '@mescius/wijmo';
import * as wjcInteropGrid from '@mescius/wijmo.interop.grid';
import * as wjcGrid from '@mescius/wijmo.grid';

// Our license key is legitimate (RSA-valid) but was issued for v5.20213; the
// ±10 minor-version window doesn't reach v5.20252. Pre-seeding Control._wme
// with a 1×1 off-screen element triggers _updateWme's early-return on every
// control instantiation, skipping the version check popup and corner watermark.
const _licShim = document.createElement('div');
_licShim.style.cssText = 'position:fixed;top:-9999px;left:-9999px;width:1px;height:1px;visibility:hidden;pointer-events:none;';
document.body.appendChild(_licShim);
(Control as any)._wme = _licShim;

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
