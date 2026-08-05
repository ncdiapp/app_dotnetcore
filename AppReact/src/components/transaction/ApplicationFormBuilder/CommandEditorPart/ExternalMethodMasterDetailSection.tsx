/**
 * Operation Type: External Method MasterDetail
 * (EmAppTransactionCommandType.ExternalMethodMasterDetail = 38).
 *
 * Runtime: AppTransactionCommandBL → ActionAttribute.ExternalMethodRegisterId
 * → AppExternalMethodRegisterBL.CallExternalMethodMasterDetail.
 */

import React, { useCallback, useEffect, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { adminSvc } from '../../../../webapi/adminsvc';

export const EmAppTransactionCommandTypeExternalMethodMasterDetail = 38;

type ExternalMethodOption = {
  Id?: number | null;
  MethodDisplayName?: string;
  MethodName?: string;
  AssemblyName?: string;
  TypeName?: string;
};

export function ExternalMethodMasterDetailSection(props: {
  action: any;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const { action, onMarkChange } = props;

  const [methods, setMethods] = useState<ExternalMethodOption[]>([]);
  const [loaded, setLoaded] = useState(false);

  const loadMethods = useCallback(async () => {
    if (loaded) return;
    try {
      const list = await adminSvc.retrieveAllAppExternalMethodRegisterExDto();
      setMethods(Array.isArray(list) ? list : []);
      setLoaded(true);
    } catch (e: any) {
      showError(e?.message || 'Failed to load external methods');
    }
  }, [loaded, showError]);

  useEffect(() => {
    if (Number(action?.ActionType) === EmAppTransactionCommandTypeExternalMethodMasterDetail) {
      void loadMethods();
    }
  }, [action?.ActionType, loadMethods]);

  if (!action) return null;
  if (Number(action.ActionType) !== EmAppTransactionCommandTypeExternalMethodMasterDetail) return null;

  const selectedId = action?.ActionAttribute?.ExternalMethodRegisterId;
  const selected =
    selectedId != null
      ? methods.find((m) => Number(m.Id) === Number(selectedId))
      : null;

  return (
    <div className="flex flex-col gap-1">
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>External Method</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={selectedId != null ? String(selectedId) : ''}
          onChange={(e) => {
            action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
            const v = e.target.value;
            action.ActionAttribute.ExternalMethodRegisterId = v ? Number(v) : null;
            onMarkChange();
          }}
        >
          <option value="">— Select method —</option>
          {methods.map((m) => (
            <option key={String(m.Id)} value={String(m.Id)}>
              {m.MethodDisplayName || m.MethodName || `Id ${m.Id}`}
              {m.MethodName ? ` (${m.MethodName})` : ''}
            </option>
          ))}
        </select>
      </div>
      {selected ? (
        <div className={`text-[11px] pl-[14rem] ${theme.label}`}>
          {selected.AssemblyName}.{selected.TypeName?.split('.').pop()}.{selected.MethodName}
        </div>
      ) : null}
    </div>
  );
}
