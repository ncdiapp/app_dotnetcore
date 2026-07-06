import type { MutableRefObject } from 'react';
import { CellType } from '@mescius/wijmo.grid';

export interface FolderTreeItem {
  Id: number;
  Name?: string;
  Children?: FolderTreeItem[];
  CountContent?: number;
  countContent?: number;
  CountContentSubTotal?: number;
  countContentSubTotal?: number;
  IsFolderReadonly?: boolean;
}

export function getFolderCountContent(item: FolderTreeItem | Record<string, unknown> | null | undefined): number {
  if (!item) return 0;
  const record = item as Record<string, unknown>;
  const raw =
    record.CountContent ??
    record.countContent ??
    record.CountContentSubTotal ??
    record.countContentSubTotal;
  if (raw == null) return 0;
  const value = Number(raw);
  return Number.isFinite(value) ? value : 0;
}

export function buildFolderCountMap(
  folders: Array<FolderTreeItem | Record<string, unknown>> | null | undefined,
  map: Record<number, number> = {},
): Record<number, number> {
  for (const folder of folders || []) {
    const record = folder as Record<string, unknown>;
    const folderId = Number(record.Id ?? record.id);
    if (Number.isFinite(folderId)) {
      map[folderId] = getFolderCountContent(folder);
    }
    const children = record.Children ?? record.children;
    if (Array.isArray(children) && children.length > 0) {
      buildFolderCountMap(children, map);
    }
  }
  return map;
}

export function folderExistsInTree(folders: FolderTreeItem[], id: number): boolean {
  for (const folder of folders || []) {
    if (folder.Id === id) return true;
    if (folder.Children?.length && folderExistsInTree(folder.Children, id)) return true;
  }
  return false;
}

export function findFolderRowIndex(flex: any, folderId: number): number {
  if (!flex?.rows) return -1;
  for (let i = 0; i < flex.rows.length; i++) {
    const item = flex.rows[i]?.dataItem as FolderTreeItem | undefined;
    if (item?.Id === folderId) return i;
  }
  return -1;
}

export function initializeFolderTreeGrid(flex: any, collapseToLevel = 1) {
  if (flex?.collapseGroupsToLevel) {
    flex.collapseGroupsToLevel(collapseToLevel);
  }
}

export function isFolderTreeExpandToggleTarget(target: EventTarget | null): boolean {
  const el = target instanceof HTMLElement ? target : null;
  if (!el) return false;
  return Boolean(
    el.closest('.wj-elem-collapse') ||
      el.classList.contains('wj-glyph') ||
      el.classList.contains('wj-glyph-right') ||
      el.classList.contains('wj-glyph-down') ||
      el.classList.contains('wj-glyph-none'),
  );
}

export function restoreFolderTreeSelection(
  flex: any,
  folderId: number | null | undefined,
  suppressSelectionRef: MutableRefObject<boolean>,
) {
  if (!flex || folderId == null) return;
  const rowIndex = findFolderRowIndex(flex, folderId);
  if (rowIndex < 0 || flex.selection?.row === rowIndex) return;
  suppressSelectionRef.current = true;
  flex.select(rowIndex, 0);
  suppressSelectionRef.current = false;
}

export function bindFolderTreeExpandOnlyBehavior(
  flex: any,
  getCurrentFolderId: () => number | null | undefined,
  suppressSelectionRef: MutableRefObject<boolean>,
  expandToggleClickRef: MutableRefObject<boolean>,
) {
  const host = flex?.hostElement as HTMLElement | undefined;
  if (!host || host.dataset.folderExpandOnlyBound) return;
  host.dataset.folderExpandOnlyBound = '1';

  host.addEventListener(
    'mousedown',
    (e: MouseEvent) => {
      expandToggleClickRef.current = isFolderTreeExpandToggleTarget(e.target);
    },
    true,
  );

  host.addEventListener(
    'click',
    (e: MouseEvent) => {
      if (!isFolderTreeExpandToggleTarget(e.target) && !expandToggleClickRef.current) return;
      expandToggleClickRef.current = false;
      window.setTimeout(() => {
        restoreFolderTreeSelection(flex, getCurrentFolderId(), suppressSelectionRef);
        flex?.invalidate?.();
      }, 0);
    },
    true,
  );
}

export function createFolderTreeItemFormatter(
  getCurrentFolderId: () => number | null | undefined,
  getCountMap?: () => Record<number, number>,
) {
  return (panel: any, r: number, c: number, cell: HTMLElement) => {
    if (panel.cellType !== CellType.Cell || c !== 0) return;

    const flex = panel.grid;
    const rowData = flex?.rows?.[r]?.dataItem as FolderTreeItem | undefined;
    if (!rowData) return;

    const countMap = getCountMap?.();
    const countContent = countMap?.[rowData.Id] ?? getFolderCountContent(rowData);
    const folderId = rowData.Id;
    const isSelected = getCurrentFolderId() === folderId;
    const folderIconClass = isSelected ? 'fa-folder-open' : 'fa-folder';

    const spanIndex = cell.innerHTML.lastIndexOf('</span>');
    let el: string;
    if (spanIndex > 0) {
      const strCollapseButton = cell.innerHTML.substring(0, spanIndex + 7);
      const strName = cell.innerHTML.substring(spanIndex + 7);
      el =
        strCollapseButton +
        ` <i class="fa-solid ${folderIconClass} text-yellow-500" style="padding-left:4px;"></i>` +
        ' ' +
        strName +
        ` (${countContent})`;
    } else {
      el =
        '<div style="width:14px;height:1px;display:inline-block"></div>' +
        ` <i class="fa-solid ${folderIconClass} text-yellow-500" style="padding-left:4px;"></i>` +
        ' ' +
        cell.innerHTML +
        ` (${countContent})`;
    }

    if (rowData.IsFolderReadonly) {
      el = `<span style="color:red;">${el}</span>`;
    }

    cell.innerHTML = el;
  };
}

export function shouldIgnoreFolderTreeSelectionChange(
  expandToggleClickRef: MutableRefObject<boolean>,
  flex: any,
  getCurrentFolderId: () => number | null | undefined,
  suppressSelectionRef: MutableRefObject<boolean>,
): boolean {
  if (!expandToggleClickRef.current) return false;
  expandToggleClickRef.current = false;
  restoreFolderTreeSelection(flex, getCurrentFolderId(), suppressSelectionRef);
  flex?.invalidate?.();
  return true;
}
