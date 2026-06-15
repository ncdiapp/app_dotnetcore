import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appMessageService } from '../../webapi/appmessagesvc';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import MessageNotificationSettingEditor from './MessageNotificationSettingEditor';

function parseParam(param: string | undefined): {
  transactionId: string;
  projectId: string;
} {
  if (!param) return { transactionId: '', projectId: '' };
  try {
    const o = JSON.parse(decodeURIComponent(param));
    return {
      transactionId: o.transactionId != null ? String(o.transactionId) : '',
      projectId: o.projectId != null ? String(o.projectId) : '',
    };
  } catch {
    return { transactionId: '', projectId: '' };
  }
}

const MessageNotificationSettingManagement: React.FC = () => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const { showValidationMessages } = useErrorMessage();
  const ctx = parseParam(param);

  const usageEnum = useEnumValues('EmAppNotificationMessageUsageType');
  const usageItems = React.useMemo(() => {
    if (!usageEnum) return [];
    return Object.keys(usageEnum).map((k) => ({ Id: (usageEnum as any)[k], Display: k }));
  }, [usageEnum]);
  const usageMap = React.useMemo(() => new DataMap(usageItems, 'Id', 'Display'), [usageItems]);

  const [settings, setSettings] = useState<any[]>([]);
  const [settingsCv, setSettingsCv] = useState(() => new CollectionView<any>([]));
  const [currentSetting, setCurrentSetting] = useState<any | null>(null);
  const flexRef = useRef<any>(null);

  const loadList = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const list = await appMessageService.retrieveAllAppmessageNotificationSettingEntityDto(
        ctx.transactionId,
        ctx.projectId,
        ''
      );
      const arr = Array.isArray(list) ? list : [];
      setSettings(arr);
      setSettingsCv(new CollectionView(arr));
      if (arr.length > 0) {
        setCurrentSetting(arr[0]);
      } else {
        setCurrentSetting(null);
      }
    } catch (e: any) {
      showValidationMessages({ Items: [{ ItemType: 1, LocalizedMessage: e?.message || 'Load failed' }] }, true);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [ctx.projectId, ctx.transactionId, dispatch, showValidationMessages]);

  useEffect(() => {
    loadList();
  }, [loadList]);

  const openCreate = () => {
    const name = window.prompt('Setting name', 'New Notification Setting');
    if (!name) return;
    const dto: any = {
      SettingName: name,
      EmScanPeriod: 1,
      AlertSpanTime: 30,
      MessageUsageType: 2,
      TranscationId: ctx.transactionId ? Number(ctx.transactionId) : null,
    };
    dispatch(setIsBusy());
    appMessageService
      .saveOneAppmessageNotificationSettingEntityDto(dto)
      .then((res) => {
        if (res?.IsSuccessful && res?.Object) {
          loadList();
          setCurrentSetting(res.Object);
        }
        if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
      })
      .finally(() => dispatch(setIsNotBusy()));
  };

  const deleteSelected = () => {
    const flex = flexRef.current?.control ?? flexRef.current;
    const row = flex?.selection?.row;
    const item = flex?.rows?.[row]?.dataItem;
    if (!item?.Id) return;
    if (!window.confirm(`Please confirm to delete the setting: ${item.SettingName}`)) return;
    dispatch(setIsBusy());
    appMessageService
      .deleteOneAppmessageNotificationSettingEntityDto(String(item.Id))
      .then((res) => {
        if (res?.IsSuccessful && res?.Object) {
          setCurrentSetting(null);
          loadList();
        }
        if (res?.ValidationResult) showValidationMessages(res.ValidationResult, true);
      })
      .finally(() => dispatch(setIsNotBusy()));
  };

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.default}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Message Notification Settings</div>
        <div className="flex gap-2">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => loadList()}>
            Refresh
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={openCreate}>
            New
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={deleteSelected}>
            Delete
          </button>
        </div>
      </div>

      <div className="w-full min-h-0 flex-auto flex gap-1 p-1 overflow-hidden">
        <div className={`w-[320px] shrink-0 h-full min-h-0 border ${theme.inputBox}`}>
          <FlexGrid
            ref={flexRef}
            className="w-full h-full"
            itemsSource={settingsCv}
            selectionMode="Row"
            headersVisibility="Column"
            onSelectionChanged={(s: any) => {
              const flex = s?.control ?? s;
              const row = flex?.selection?.row;
              const item = flex?.rows?.[row]?.dataItem;
              if (item) setCurrentSetting(item);
            }}
          >
            <FlexGridFilter />
            <FlexGridColumn binding="SettingName" header="Name" width="*" minWidth={120} isReadOnly />
            <FlexGridColumn binding="MessageUsageType" header="Usage" width={100} dataMap={usageMap} isReadOnly />
          </FlexGrid>
        </div>
        <div className={`min-h-0 w-1 flex-auto min-w-0 overflow-hidden border ${theme.inputBox}`}>
          <MessageNotificationSettingEditor settingId={currentSetting?.Id ?? null} hideTopMenu={false} />
        </div>
      </div>
    </div>
  );
};

export default MessageNotificationSettingManagement;
