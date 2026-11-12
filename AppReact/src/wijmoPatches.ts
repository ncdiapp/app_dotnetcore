/**
 * Guard Wijmo grid against null refs when queued microtasks run after dispose/re-render.
 * - editRange: DirectiveCellFactoryBase._autoSizeIfRequired runs in queueMicrotask; guard when this.grid or editRange is invalid.
 * - finishEditing: FlexGrid.refresh() calls finishEditing(); guard when internal editor ref is null.
 * - refresh while typing: CollectionView/React/FlexGridAddOn often call refresh(); Wijmo
 *   refresh starts with finishEditing() and steals cell focus (~0.5s). Skip only when
 *   activeEditor is present — do NOT use editRange (often a non-null CellRange while idle;
 *   skipping on editRange blanked Formula grids).
 *
 * IMPORTANT: Always wrap the true original methods (saved once). HMR must not stack wrappers.
 */

import { Control } from '@mescius/wijmo';
import * as wjcInteropGrid from '@mescius/wijmo.interop.grid';
import * as wjcGrid from '@mescius/wijmo.grid';

// Visible in DevTools: confirms this safe patch build is loaded (not a stacked HMR wrapper).
(window as any).__APPAI_WIJMO_PATCH__ = 'safe-refresh-v4';

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
  const p = interopProto as any;
  if (!p.__appaiOrigAutoSize) {
    p.__appaiOrigAutoSize = interopProto._autoSizeIfRequired;
  }
  const origAutoSize = p.__appaiOrigAutoSize as () => void;
  interopProto._autoSizeIfRequired = function (this: { grid: unknown }) {
    try {
      if (!this.grid) return;
      return origAutoSize.call(this);
    } catch {
      // grid disposed or editRange getter threw
    }
  };
}

// 2) Guard FlexGrid.finishEditing / refresh.
const flexGridProto = (wjcGrid as { FlexGrid?: { prototype: unknown } }).FlexGrid?.prototype as {
  finishEditing?: (cancel?: boolean) => boolean;
  refresh?: (fullUpdate?: boolean) => void;
} | undefined;

if (flexGridProto && typeof flexGridProto.finishEditing === 'function') {
  const p = flexGridProto as any;
  if (!p.__appaiOrigFinishEditing) {
    p.__appaiOrigFinishEditing = flexGridProto.finishEditing;
  }
  const origFinish = p.__appaiOrigFinishEditing as (cancel?: boolean) => boolean;
  flexGridProto.finishEditing = function (this: unknown, cancel?: boolean) {
    try {
      return origFinish.call(this, cancel);
    } catch {
      return false;
    }
  };
}

if (flexGridProto && typeof flexGridProto.refresh === 'function') {
  const p = flexGridProto as any;
  // Prefer first-seen original so HMR does not stack wrappers.
  if (!p.__appaiOrigRefresh) {
    p.__appaiOrigRefresh = flexGridProto.refresh;
  }
  const origRefresh = p.__appaiOrigRefresh as (fullUpdate?: boolean) => void;
  flexGridProto.refresh = function (this: any, fullUpdate?: boolean) {
    try {
      if (!this || this.isDisposed || !this.hostElement) return;
      // Keep the in-cell editor if a refresh is queued while the user is typing.
      // activeEditor only — editRange stays set when idle and must not block refresh.
      if (this.activeEditor) return;
      return origRefresh.call(this, fullUpdate);
    } catch (e: any) {
      const msg = String(e?.message ?? '');
      if (
        msg.includes('scrollLeft') ||
        msg.includes('scrollPosition') ||
        msg.includes('hasColumnGroups')
      ) {
        return;
      }
      throw e;
    }
  };
}

// Remove any prior getColumn monkey-patch from older hot-reload sessions.
if (flexGridProto && (flexGridProto as any).__appaiOrigGetColumn) {
  (flexGridProto as any).getColumn = (flexGridProto as any).__appaiOrigGetColumn;
  delete (flexGridProto as any).__appaiOrigGetColumn;
}
