import { createContext } from 'react';

/** Host element in Transaction Form Group. The form dock portals here so it lines up with the record navigation bar. */
export const FormGroupDockSlotContext = createContext<HTMLDivElement | null>(null);

/** Form body (not the conversation dock). Result overlay portals here so it never covers chat. */
export const FormAgentResultSlotContext = createContext<HTMLDivElement | null>(null);

/** True when Form Group actually shows First/Prev (itemCount > 1). Slot existing is not enough. */
export function formGroupDockShowsNav(el: HTMLDivElement | null): boolean {
  return el?.getAttribute('data-form-group-nav') === '1';
}
