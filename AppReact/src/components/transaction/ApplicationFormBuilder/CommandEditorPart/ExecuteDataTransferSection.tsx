import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';

export const EmAppTransactionCommandTypeExecuteFormDataTransfer = 43;

type DataTransferSettingItem = { Id: number; Description: string; DestinationTransactionId?: number | null };

export function ExecuteDataTransferSection(props: {
  action: any;
  transactionId: number | null;
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const { action, transactionId, onMarkChange } = props;

  const [dataTransferSettingList, setDataTransferSettingList] = useState<DataTransferSettingItem[]>([]);
  const [targetCommandList, setTargetCommandList] = useState<any[]>([]);

  const actionType = Number(action?.ActionType);
  const show = actionType === EmAppTransactionCommandTypeExecuteFormDataTransfer;

  const dataTransferSettingId = action?.DataTransferSettingId != null ? Number(action.DataTransferSettingId) : null;

  const selectedSetting = useMemo(
    () => (dataTransferSettingId != null ? dataTransferSettingList.find((s) => Number(s.Id) === Number(dataTransferSettingId)) ?? null : null),
    [dataTransferSettingId, dataTransferSettingList],
  );

  const loadDataTransferSettings = useCallback(async () => {
    if (!transactionId) return;
    try {
      const list = await appTransactionService.retrieveAllAppTransactionDataTransferSettingExDto(transactionId);
      const arr = Array.isArray(list) ? list : list?.ObjectList ?? [];
      setDataTransferSettingList(arr);
    } catch (e: any) {
      showError(e?.message || 'Failed to load data transfer settings');
    }
  }, [showError, transactionId]);

  const loadTargetCommands = useCallback(
    async (destinationTransactionId: number) => {
      try {
        const targetTransactionData = await appTransactionService.getOneHierarchyTransaction(
          String(destinationTransactionId),
          false,
          '',
          '',
          '',
          false,
          '',
        );
        const list = targetTransactionData?.CommandActionList || [];
        setTargetCommandList(Array.isArray(list) ? list : []);
      } catch (e: any) {
        setTargetCommandList([]);
        showError(e?.message || 'Failed to load target data model tasks');
      }
    },
    [showError],
  );

  useEffect(() => {
    if (!show) return;
    void loadDataTransferSettings();
  }, [loadDataTransferSettings, show]);

  useEffect(() => {
    if (!show) return;
    const destId = selectedSetting?.DestinationTransactionId;
    if (destId) {
      void loadTargetCommands(Number(destId));
    } else {
      setTargetCommandList([]);
    }
  }, [loadTargetCommands, selectedSetting?.DestinationTransactionId, show]);

  if (!action || !show) return null;

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Data Transfer Setting</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={dataTransferSettingId == null ? '' : String(dataTransferSettingId)}
          onChange={(e) => {
            const v = e.target.value ? Number(e.target.value) : null;
            action.DataTransferSettingId = v;
            onMarkChange();
          }}
        >
          <option value="">(None)</option>
          {dataTransferSettingList.map((s) => (
            <option key={s.Id} value={String(s.Id)}>
              {s.Description}
            </option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Execute Task On Target Data Model</label>
        <select
          className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
          value={String(action?.ActionAttribute?.TargetTransactionCommandId ?? '')}
          onChange={(e) => {
            action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
            action.ActionAttribute.TargetTransactionCommandId = e.target.value ? Number(e.target.value) : null;
            onMarkChange();
          }}
        >
          <option value="">(None)</option>
          {targetCommandList.map((c: any) => (
            <option key={c.Id} value={String(c.Id)}>
              {c.Name}
            </option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>Need To Open Form</label>
        <label className="flex items-center gap-2 text-xs">
          <input
            type="checkbox"
            checked={!!action?.ActionAttribute?.IsNeedToOpenNewForm}
            onChange={(e) => {
              action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
              action.ActionAttribute.IsNeedToOpenNewForm = e.target.checked;
              onMarkChange();
            }}
          />
          <span className={theme.label}>Enable</span>
        </label>
      </div>
    </>
  );
}

