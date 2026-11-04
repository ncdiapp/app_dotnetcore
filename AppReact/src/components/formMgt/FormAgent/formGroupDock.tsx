import { createContext } from 'react';

/** Host element in Transaction Form Group. The form dock portals here so it lines up with the record navigation bar. */
export const FormGroupDockSlotContext = createContext<HTMLDivElement | null>(null);
