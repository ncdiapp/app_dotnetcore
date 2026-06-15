import React, { createContext, useContext, type ReactNode } from 'react';

/** Runtime "field setting" actions from Form Master Detail (Angular: openTransactionFieldEditor / openUnitContextMenu / …). */
export type FormMasterDetailRuntimeConfigApi = {
  openTransactionFieldEditor: (
    e: React.MouseEvent,
    transFieldId: number,
    transFieldDisplayName: string,
    layoutItemId: string | number,
    isGrid?: boolean
  ) => void;
  openTransactionUnitEditor: (e: React.MouseEvent, transUnitId: number) => void;
  openUnitContextMenu: (
    e: React.MouseEvent,
    unitId: number,
    unitDisplayName: string,
    layoutItemId: string | number
  ) => void;
  openRootUnitsContextMenu: (e: React.MouseEvent) => void;
};

const defaultApi: FormMasterDetailRuntimeConfigApi = {
  openTransactionFieldEditor: () => {},
  openTransactionUnitEditor: () => {},
  openUnitContextMenu: () => {},
  openRootUnitsContextMenu: () => {},
};

/** Use until FormMainMenus registers real handlers (no-op). */
export const emptyFormMasterDetailRuntimeConfigApi = defaultApi;

const FormMasterDetailRuntimeConfigContext = createContext<FormMasterDetailRuntimeConfigApi>(defaultApi);

export const FormMasterDetailRuntimeConfigProvider = ({
  value,
  children,
}: {
  value: FormMasterDetailRuntimeConfigApi;
  children: ReactNode;
}) => (
  <FormMasterDetailRuntimeConfigContext.Provider value={value}>{children}</FormMasterDetailRuntimeConfigContext.Provider>
);

export function useFormMasterDetailRuntimeConfig(): FormMasterDetailRuntimeConfigApi {
  return useContext(FormMasterDetailRuntimeConfigContext);
}
