/**
 * Operation Type: Call API Operation (EmAppTransactionCommandType.CallApiOperation = 59).
 * Angular: TransactionCommandSingleEditorPopup.cshtml + embedded TransactionFormDataTransferSettingEditor.
 */

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';
import { integrationService } from '../../../../webapi/integrationsvc';
import { PopupStackLayer, usePopupZIndex } from '../../../formMgt/popupStack';
import { DataTransferFromDataModelToApiEmbeddedEditor } from './callApiOperation/DataTransferFromDataModelToApiEmbeddedEditor';

export const EmAppTransactionCommandTypeCallApiOperation = 59;

export function CallApiOperationSection(props: {
  transactionId: number | null;
  hierarchy: any;
  action: any;
  onMarkChange: () => void;
}) {
  const { transactionId, hierarchy, action, onMarkChange } = props;
  const { theme } = useTheme();
  const { showError, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();

  const [integrationSettingList, setIntegrationSettingList] = useState<any[]>([]);
  const [integrationsLoaded, setIntegrationsLoaded] = useState(false);
  const [showPicker, setShowPicker] = useState(false);
  const [pickerPos, setPickerPos] = useState<{ top: number; left: number } | null>(null);
  const [hoveredIntegrationId, setHoveredIntegrationId] = useState<number | null>(null);
  const pickerTriggerRef = useRef<HTMLAnchorElement>(null);
  const embeddedSaveRef = useRef<((callback?: () => void) => Promise<void>) | null>(null);
  const pickerZIndex = usePopupZIndex();

  const dataTransferSettingId = action?.DataTransferSettingId ? Number(action.DataTransferSettingId) : null;

  const ensureActionAttribute = useCallback(() => {
    if (!action.ActionAttribute) {
      action.ActionAttribute = { ChildActionList: [] };
    }
    return action.ActionAttribute;
  }, [action]);

  const loadIntegrationSettings = useCallback(async () => {
    if (integrationsLoaded) return;
    try {
      const list = await integrationService.retrieveAllAppIntegrationSettingDto(false);
      setIntegrationSettingList(Array.isArray(list) ? list : []);
      setIntegrationsLoaded(true);
    } catch (e: any) {
      showError(e?.message || 'Failed to load API operations');
    }
  }, [integrationsLoaded, showError]);

  useEffect(() => {
    if (Number(action?.ActionType) === EmAppTransactionCommandTypeCallApiOperation) {
      loadIntegrationSettings();
    }
  }, [action?.ActionType, loadIntegrationSettings]);

  useEffect(() => {
    if (!action) return;
    action.autoSaveFunc = (callback?: () => void) => {
      const fn = embeddedSaveRef.current;
      if (fn) {
        void fn(callback);
      } else if (callback) {
        callback();
      }
    };
    return () => {
      action.autoSaveFunc = null;
    };
  }, [action, dataTransferSettingId]);

  const handleSelectApiOperationAndGenerate = useCallback(
    async (apiOperationId: number) => {
      if (!action || !apiOperationId || !transactionId) return;
      setShowPicker(false);
      setPickerPos(null);
      dispatch(setIsBusy());
      try {
        const commandDto = {
          ...action,
          CommandTransactionId: action.CommandTransactionId || hierarchy?.Id || transactionId,
          ApiOperationId: apiOperationId,
        };
        const result = await appTransactionService.generateCommandCallApiOperationSetting(commandDto);
        if (result?.ValidationResult) {
          showValidationMessages(result.ValidationResult);
        }
        if (result?.IsSuccessful && result?.Object) {
          action.DataTransferSettingId = result.Object.DataTransferSettingId ?? null;
          onMarkChange();
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to generate API operation setting');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [action, dispatch, hierarchy?.Id, onMarkChange, showError, showValidationMessages, transactionId],
  );

  if (!action || Number(action.ActionType) !== EmAppTransactionCommandTypeCallApiOperation) {
    return null;
  }

  const attr = ensureActionAttribute();

  return (
    <>
      <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
        <label className={`text-xs ${theme.label}`}>API Call Log Setting</label>
        <div className="flex flex-wrap gap-5 text-xs">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={!!attr.IsLogApiRequest}
              onChange={(e) => {
                attr.IsLogApiRequest = e.target.checked;
                onMarkChange();
              }}
            />
            <span className={theme.label}>Log Request</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={!!attr.IsLogApiPayload}
              onChange={(e) => {
                attr.IsLogApiPayload = e.target.checked;
                onMarkChange();
              }}
            />
            <span className={theme.label}>Log Payload</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={!!attr.IsLogApiResponse}
              onChange={(e) => {
                attr.IsLogApiResponse = e.target.checked;
                onMarkChange();
              }}
            />
            <span className={theme.label}>Log Response</span>
          </label>
        </div>
      </div>

      {!dataTransferSettingId ? (
        <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
          <label className={`text-xs ${theme.label}`}>API Operation</label>
          <div className="text-xs">
            <a
              ref={pickerTriggerRef}
              href="#"
              className="underline inline-flex items-center gap-1"
              onMouseEnter={() => loadIntegrationSettings()}
              onClick={(e) => {
                e.preventDefault();
                loadIntegrationSettings();
                if (!showPicker) {
                  const rect = pickerTriggerRef.current?.getBoundingClientRect();
                  if (rect) setPickerPos({ top: rect.bottom + 4, left: rect.left });
                } else {
                  setPickerPos(null);
                }
                setShowPicker((v) => !v);
              }}
            >
              Click To Select <i className="fa-solid fa-caret-down" aria-hidden />
            </a>
            {showPicker &&
              pickerPos &&
              createPortal(
                <PopupStackLayer zIndex={pickerZIndex}>
                  <div
                    className={`fixed flex flex-row items-start ${theme.mainContentSection}`}
                    style={{ zIndex: pickerZIndex, top: pickerPos.top, left: pickerPos.left }}
                    onMouseLeave={() => setHoveredIntegrationId(null)}
                  >
                  <div className={`min-w-[200px] max-h-[500px] overflow-y-auto border rounded-l shadow-lg py-1 ${theme.mainContentSection}`}>
                    <div className="px-3 py-2 text-xs font-semibold border-b">Select An API Operation:</div>
                    {integrationSettingList.map((s: any) => (
                      <div
                        key={s.Id}
                        className={`px-3 py-2 text-xs flex items-center justify-between cursor-default ${hoveredIntegrationId === s.Id ? (theme.tab_active ?? 'bg-gray-100') : ''}`}
                        onMouseEnter={() => setHoveredIntegrationId(s.Id)}
                      >
                        <span>{s.Name}</span>
                        <i className="fa-solid fa-angle-right" />
                      </div>
                    ))}
                  </div>
                  {hoveredIntegrationId != null && (() => {
                    const setting = integrationSettingList.find((s: any) => s.Id === hoveredIntegrationId);
                    const ops =
                      setting?.AppIntergrationSettingParameterList || setting?.AppIntegrationSettingParameterList || [];
                    return (
                      <div className={`min-w-[220px] max-h-[500px] overflow-y-auto border border-l-0 rounded-r shadow-lg py-1 ${theme.mainContentSection}`}>
                        {ops.map((p: any) => (
                          <button
                            key={p.Id}
                            type="button"
                            className={`w-full px-3 py-2 text-left text-xs ${theme.contextMenu}`}
                            onClick={() => handleSelectApiOperationAndGenerate(p.Id)}
                          >
                            {p.ActionCode}
                          </button>
                        ))}
                      </div>
                    );
                  })()}
                  </div>
                </PopupStackLayer>,
                document.body,
              )}
          </div>
        </div>
      ) : (
        <div className="w-full min-h-[360px] border rounded-[4px] p-2 mt-1">
          {transactionId != null ? (
            <DataTransferFromDataModelToApiEmbeddedEditor
              key={dataTransferSettingId}
              settingId={dataTransferSettingId}
              srcTransactionId={transactionId}
              onMarkChange={onMarkChange}
              onRegisterSave={(fn) => {
                embeddedSaveRef.current = fn;
              }}
            />
          ) : (
            <p className={`text-xs ${theme.label}`}>Save the data model before configuring API mappings.</p>
          )}
        </div>
      )}
    </>
  );
}
