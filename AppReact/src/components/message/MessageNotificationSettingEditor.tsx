import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appMessageService } from '../../webapi/appmessagesvc';
import { useEnumEntry, useEnumValues } from '../../hooks/useEnumDictionary';

type MessageNotificationSettingEditorProps = {
  settingId: string | number | null;
  hideTopMenu?: boolean;
};

function enumToItems(ev: Record<string, number> | null): { Id: number; Display: string }[] {
  if (!ev) return [];
  return Object.keys(ev).map((k) => ({ Id: (ev as any)[k], Display: k }));
}

const MessageNotificationSettingEditor: React.FC<MessageNotificationSettingEditorProps> = ({
  settingId,
  hideTopMenu,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showValidationMessages } = useErrorMessage();

  const usageEnum = useEnumValues('EmAppNotificationMessageUsageType');
  const scanEnum = useEnumValues('EmAppMessageScanPeriod');

  const usageTargetDate = useEnumEntry('EmAppNotificationMessageUsageType', 'TargetDateQuery');
  const usageContentQuery = useEnumEntry('EmAppNotificationMessageUsageType', 'NotificationContentQuery');

  const usageItems = useMemo(() => enumToItems(usageEnum), [usageEnum]);
  const scanItems = useMemo(() => enumToItems(scanEnum), [scanEnum]);

  const [currentSetting, setCurrentSetting] = useState<any>(null);
  const [contentSettings, setContentSettings] = useState<any[]>([]);

  const load = useCallback(async () => {
    if (!settingId) return;
    dispatch(setIsBusy());
    try {
      const data = await appMessageService.retrieveOneAppmessageNotificationSettingExDto(String(settingId));
      const tid = data?.TranscationId ?? data?.TransactionId ?? '';
      const pid = data?.ProejctId ?? data?.ProjectId ?? '';
      const usageTypeForQuery =
        usageContentQuery != null ? String(usageContentQuery) : '3';
      const list = await appMessageService.retrieveAllAppmessageNotificationSettingEntityDto(
        String(tid),
        String(pid),
        usageTypeForQuery
      );
      setCurrentSetting(data);
      setContentSettings(Array.isArray(list) ? list : []);
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, settingId, showValidationMessages, usageContentQuery]);

  useEffect(() => {
    load();
  }, [load]);

  const save = async () => {
    if (!currentSetting) return;
    dispatch(setIsBusy());
    try {
      const res = await appMessageService.saveOneAppmessageNotificationSettingEntityDto(currentSetting);
      if (res?.IsSuccessful && res?.Object) {
        setCurrentSetting(res.Object);
        await load();
      }
      if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Save failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  if (!settingId || !currentSetting) {
    return (
      <div className={`p-3 text-xs ${theme.label}`}>{settingId ? 'Loading…' : 'Select a setting'}</div>
    );
  }

  const showTargetDateFields =
    usageTargetDate != null && Number(currentSetting.MessageUsageType) === usageTargetDate;

  return (
    <div className={`w-full h-full flex flex-col overflow-auto ${theme.default}`}>
      {!hideTopMenu ? (
        <div className={`flex items-center justify-between px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold truncate ${theme.title}`}>
            Setting Detail: {currentSetting.SettingName}
          </div>
          <div className="flex gap-2">
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => load()}>
              Refresh
            </button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => save()}>
              Save
            </button>
          </div>
        </div>
      ) : null}

      <div className={`w-full h-1 flex-auto overflow-auto p-2 space-y-2 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-2">
          <label className={`w-32 text-xs shrink-0 ${theme.label}`}>Setting Name</label>
          <input
            type="text"
            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
            value={currentSetting.SettingName ?? ''}
            onChange={(e) => setCurrentSetting({ ...currentSetting, SettingName: e.target.value, IsModified: true })}
          />
        </div>
        <div className="flex items-center gap-2">
          <label className={`w-32 text-xs shrink-0 ${theme.label}`}>Usage Type</label>
          <select
            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
            value={currentSetting.MessageUsageType ?? ''}
            onChange={(e) =>
              setCurrentSetting({ ...currentSetting, MessageUsageType: Number(e.target.value), IsModified: true })
            }
          >
            {usageItems.map((u) => (
              <option key={u.Id} value={u.Id}>
                {u.Display}
              </option>
            ))}
          </select>
        </div>
        {showTargetDateFields ? (
          <>
            <div className="flex items-center gap-2">
              <label className={`w-32 text-xs shrink-0 ${theme.label}`}>Scan Period</label>
              <select
                className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                value={currentSetting.EmScanPeriod ?? ''}
                onChange={(e) =>
                  setCurrentSetting({ ...currentSetting, EmScanPeriod: Number(e.target.value), IsModified: true })
                }
              >
                {scanItems.map((u) => (
                  <option key={u.Id} value={u.Id}>
                    {u.Display}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <label className={`w-32 text-xs shrink-0 ${theme.label}`}>Content Setting</label>
              <select
                className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                value={currentSetting.NotificationQueryContentSettingId ?? ''}
                onChange={(e) =>
                  setCurrentSetting({
                    ...currentSetting,
                    NotificationQueryContentSettingId: e.target.value ? Number(e.target.value) : null,
                    IsModified: true,
                  })
                }
              >
                <option value="">—</option>
                {contentSettings.map((c) => (
                  <option key={c.Id} value={c.Id}>
                    {c.SettingName}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-2">
              <label className={`w-32 text-xs shrink-0 ${theme.label}`}>Alert Span Time</label>
              <input
                type="number"
                className={`w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                value={currentSetting.AlertSpanTime ?? ''}
                onChange={(e) =>
                  setCurrentSetting({ ...currentSetting, AlertSpanTime: Number(e.target.value), IsModified: true })
                }
              />
            </div>
          </>
        ) : null}
        <div className="flex flex-col gap-1">
          <label className={`text-xs ${theme.label}`}>Notification Query</label>
          <textarea
            className={`w-full min-h-[120px] px-2 py-1 text-xs border font-mono ${theme.inputBox}`}
            value={currentSetting.NotificationQuery ?? ''}
            onChange={(e) =>
              setCurrentSetting({ ...currentSetting, NotificationQuery: e.target.value, IsModified: true })
            }
          />
        </div>
        {usageContentQuery != null && Number(currentSetting.MessageUsageType) === usageContentQuery ? (
          <div className="flex flex-col gap-1">
            <label className={`text-xs ${theme.label}`}>Message Template</label>
            <textarea
              className={`w-full min-h-[120px] px-2 py-1 text-xs border font-mono ${theme.inputBox}`}
              value={currentSetting.MessageTemplate ?? ''}
              onChange={(e) =>
                setCurrentSetting({ ...currentSetting, MessageTemplate: e.target.value, IsModified: true })
              }
            />
          </div>
        ) : null}
      </div>
    </div>
  );
};

export default MessageNotificationSettingEditor;
