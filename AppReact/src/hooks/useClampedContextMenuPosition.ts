import { useLayoutEffect, type Dispatch, type RefObject, type SetStateAction } from 'react';
import appHelper from '../helper/appHelper';

export const CONTEXT_MENU_VIEWPORT_MARGIN = 8;

export type MenuPosition = { x: number; y: number };

export function clampContextMenuPosition(
  x: number,
  y: number,
  menuWidth: number,
  menuHeight: number,
  margin: number = CONTEXT_MENU_VIEWPORT_MARGIN
): MenuPosition {
  return appHelper.clampMenuPositionToViewport({
    x,
    y,
    menuWidth,
    menuHeight,
    margin,
  });
}

/**
 * After a fixed-position context menu renders, refine its position using measured size.
 */
export function useRefineContextMenuPosition(
  isOpen: boolean,
  menuRef: RefObject<HTMLDivElement | null>,
  setPosition: Dispatch<SetStateAction<MenuPosition>>,
  margin: number = CONTEXT_MENU_VIEWPORT_MARGIN
): void {
  useLayoutEffect(() => {
    if (!isOpen || !menuRef.current) return;

    const menuEl = menuRef.current;
    setPosition((prev) => {
      const next = appHelper.clampMenuPositionToViewport({
        x: prev.x,
        y: prev.y,
        menuWidth: menuEl.offsetWidth,
        menuHeight: menuEl.offsetHeight,
        margin,
      });
      if (next.x === prev.x && next.y === prev.y) return prev;
      return next;
    });
  }, [isOpen, margin, menuRef, setPosition]);
}

/** For menu state objects that include x/y plus other fields (item, visible, etc.). */
export function useRefineContextMenuField<T extends MenuPosition>(
  isOpen: boolean,
  menuRef: RefObject<HTMLDivElement | null>,
  setState: Dispatch<SetStateAction<T>>,
  margin: number = CONTEXT_MENU_VIEWPORT_MARGIN
): void {
  useLayoutEffect(() => {
    if (!isOpen || !menuRef.current) return;

    const menuEl = menuRef.current;
    setState((prev) => {
      const next = appHelper.clampMenuPositionToViewport({
        x: prev.x,
        y: prev.y,
        menuWidth: menuEl.offsetWidth,
        menuHeight: menuEl.offsetHeight,
        margin,
      });
      if (next.x === prev.x && next.y === prev.y) return prev;
      return { ...prev, x: next.x, y: next.y };
    });
  }, [isOpen, margin, menuRef, setState]);
}
