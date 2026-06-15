/**
 * Data Transfer Setting Editor modal (create or edit).
 * Supports all transfer types; basic fields (Description, From/To Data Model, API) with save.
 * Full mapping UIs per type match Angular separate pages (Message, Form, FormAndApi) - this provides the common edit surface.
 */

import React, { useState, useEffect, useCallback } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';

const EmAppTransactionDataTransferType = {
  FromDataModelToMessageTemplate: 1,
  FromDataModelToDataModel: 2,
  FromApiToDataModel: 3,
  FromDataModelToApi: 4,
};

export interface TransactionDataTransferSettingEditorProps {
  isOpen: boolean;
  mode: 'create' | 'edit';
  transactionId: number;
  transactionName?: string | null;
  settingId?: number;
  transferTypeId?: number;
  param2?: { emAppTransactionDataTransferType: number; srcTransactionId: number | null; destinationTransactionId: number | null };
  onClose: () => void;
  onSaved: () => void;
}

const TransactionDataTransferSettingEditor: React.FC<TransactionDataTransferSettingEditorProps> = ({
  isOpen,
  mode,
  transactionId,
  transactionName,
  settingId,
  transferTypeId,
  param2,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [transactionList, setTransactionList] = useState<{ Id: number; Display: string }[]>([]);
  const [form, setForm] = useState<{
    Id?: number;
    Description: string;
    TransferTypeId: number;
    TransactionId: number | null;
    DestinationTransactionId: number | null;
    InternalCode: string;
  }>({
    Description: '',
    TransferTypeId: EmAppTransactionDataTransferType.FromDataModelToDataModel,
    TransactionId: null,
    DestinationTransactionId: null,
    InternalCode: '',
  });

  const loadData = useCallback(async () => {
    if (!isOpen) return;
    dispatch(setIsBusy());
    setLoading(true);
    try {
      const [massEntityData, settingData] = await Promise.all([
        adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView|AppTransaction'),
        mode === 'edit' && settingId ? appTransactionService.retrieveOneAppTransactionDataTransferSettingExDto(settingId) : Promise.resolve(null),
      ]);

      const transactions = massEntityData?.['AppTransaction'];
      if (transactions) {
        const arr = Array.isArray(transactions) ? transactions : [];
        setTransactionList(arr.map((t: any) => ({ Id: t.Id, Display: t.Display ?? t.TransactionName ?? String(t.Id) })));
      } else {
        setTransactionList([]);
      }

      if (mode === 'edit' && settingData) {
        setForm({
          Id: settingData.Id,
          Description: settingData.Description ?? '',
          TransferTypeId: settingData.TransferTypeId ?? EmAppTransactionDataTransferType.FromDataModelToDataModel,
          TransactionId: settingData.TransactionId ?? null,
          DestinationTransactionId: settingData.DestinationTransactionId ?? null,
          InternalCode: settingData.InternalCode ?? '',
        });
      } else if (mode === 'create' && (transferTypeId != null || param2)) {
        const typeId = transferTypeId ?? param2?.emAppTransactionDataTransferType ?? EmAppTransactionDataTransferType.FromDataModelToDataModel;
        const srcId = param2?.srcTransactionId ?? (typeId === EmAppTransactionDataTransferType.FromApiToDataModel ? null : transactionId);
        const destId = param2?.destinationTransactionId ?? (typeId === EmAppTransactionDataTransferType.FromApiToDataModel ? transactionId : null);
        setForm({
          Description: '',
          TransferTypeId: typeId,
          TransactionId: srcId ?? null,
          DestinationTransactionId: destId ?? null,
          InternalCode: '',
        });
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [isOpen, mode, settingId, transferTypeId, param2, transactionId, dispatch, showError]);

  useEffect(() => {
    if (isOpen) loadData();
  }, [isOpen, loadData]);

  const handleSave = useCallback(async () => {
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveOneAppTransactionDataTransferSettingExDto(form);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.Object != null || result?.IsSuccessful) {
        showInfo('Data transfer setting saved.');
        onSaved();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
      setSaving(false);
    }
  }, [form, dispatch, showError, showValidationMessages, showInfo, onSaved]);

  const updateForm = (updates: Partial<typeof form>) => {
    setForm((prev) => ({ ...prev, ...updates }));
  };

  if (!isOpen) return null;

  const title = mode === 'edit' ? (transactionName ? `${transactionName}: Edit Data Transfer Setting` : 'Edit Data Transfer Setting') : (transactionName ? `${transactionName}: New Data Transfer Setting` : 'New Data Transfer Setting');

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black/50" onClick={(e) => e.stopPropagation()}>
      <div
        className={`max-w-lg w-full max-h-[90vh] overflow-auto rounded-t-md rounded-b-md border shadow-lg ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-1.5 border-b ${theme.modalHeader}`}>
          <h3 className={`text-sm font-semibold ${theme.title}`}>{title}</h3>
          <button type="button" onClick={onClose} className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs flex items-center justify-center`} aria-label="Close">
            <i className="fa fa-times" />
          </button>
        </div>
        <div className="p-3 space-y-2">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : (
            <>
              <div>
                <label className={`block text-xs mb-0.5 ${theme.label}`}>Description</label>
                <input
                  type="text"
                  className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                  value={form.Description}
                  onChange={(e) => updateForm({ Description: e.target.value })}
                  placeholder="Description"
                />
              </div>
              {mode === 'create' && (
                <div className={`text-xs ${theme.label}`}>Type: {form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ? 'From Data Model To Message Template' : form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel ? 'From Data Model To Data Model' : form.TransferTypeId === EmAppTransactionDataTransferType.FromApiToDataModel ? 'From Api To Data Model' : 'From Data Model To Api'}</div>
              )}
              {(form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel ||
                form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ||
                form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToApi) && (
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>From Data Model</label>
                  <select
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                    value={form.TransactionId ?? ''}
                    onChange={(e) => updateForm({ TransactionId: e.target.value ? Number(e.target.value) : null })}
                  >
                    <option value="">—</option>
                    {transactionList.map((item) => (
                      <option key={item.Id} value={item.Id}>{item.Display}</option>
                    ))}
                  </select>
                </div>
              )}
              {(form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel ||
                form.TransferTypeId === EmAppTransactionDataTransferType.FromApiToDataModel) && (
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>To Data Model</label>
                  <select
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                    value={form.DestinationTransactionId ?? ''}
                    onChange={(e) => updateForm({ DestinationTransactionId: e.target.value ? Number(e.target.value) : null })}
                  >
                    <option value="">—</option>
                    {transactionList.map((item) => (
                      <option key={item.Id} value={item.Id}>{item.Display}</option>
                    ))}
                  </select>
                </div>
              )}
              <div>
                <label className={`block text-xs mb-0.5 ${theme.label}`}>API (Internal Code)</label>
                <input
                  type="text"
                  className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                  value={form.InternalCode}
                  onChange={(e) => updateForm({ InternalCode: e.target.value })}
                  placeholder="Internal code / API"
                />
              </div>
              <p className={`text-xs ${theme.label}`}>Advanced mapping for this transfer type can be configured in the full application editor.</p>
            </>
          )}
        </div>
        <div className={`px-3 py-1.5 border-t flex justify-end space-x-2 ${theme.mainContentSection}`}>
          <button type="button" onClick={onClose} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Cancel"><i className="fa fa-times" aria-hidden="true" /> Cancel</button>
          <button type="button" onClick={handleSave} disabled={loading || saving} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed`} title="Save">
            <i className="fa fa-save" aria-hidden="true" /> {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default TransactionDataTransferSettingEditor;
