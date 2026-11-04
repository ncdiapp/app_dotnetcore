import { createContext, type MutableRefObject } from 'react';
import type { FormAgentHostSlots } from './formAgentHost';

export type FormGroupAgentHostEntry = {
  key: string;
  role: 'header' | 'main';
  name?: string;
  slots: FormAgentHostSlots;
};

/** Host element in Transaction Form Group. The form dock portals here so it lines up with the record navigation bar. */
export const FormGroupDockSlotContext = createContext<HTMLDivElement | null>(null);

/** HEADER + MAIN Master Detail hosts in a Transaction Form Group. The dock writes fields on every registered form. */
export const FormGroupAgentHostsContext = createContext<MutableRefObject<Map<string, FormGroupAgentHostEntry>> | null>(null);

/** Form body (not the conversation dock). Result overlay portals here so it never covers chat. */
export const FormAgentResultSlotContext = createContext<HTMLDivElement | null>(null);

/** True when Form Group actually shows First/Prev (itemCount > 1). Slot existing is not enough. */
export function formGroupDockShowsNav(el: HTMLDivElement | null): boolean {
  return el?.getAttribute('data-form-group-nav') === '1';
}
