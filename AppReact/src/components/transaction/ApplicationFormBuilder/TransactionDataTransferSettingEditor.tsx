/**
 * Data Transfer Setting Full Editor (create / edit) — ~90% modal.
 * Types 1–3: header + field mapping grid. Type 4: header + embedded API mapping editor.
 */

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { DataTransferFromDataModelToApiEmbeddedEditor } from './CommandEditorPart/callApiOperation/DataTransferFromDataModelToApiEmbeddedEditor';
import {
  EmAppTransactionDataTransferType,
  buildAutoSourceFieldMappings,
  buildFieldLookupsFromTransaction,
  filterMappingsForSave,
  mappingsFromSetting,
  prepareFullReplaceMappingSavePayload,
  transferTypeDisplay,
  type DataTransferMappingRow,
  type TransactionFieldLookup,
} from './dataTransferSettingEditorHelpers';

export interface TransactionDataTransferSettingEditorProps {
  isOpen: boolean;
  mode: 'create' | 'edit';
  transactionId: number;
  transactionName?: string | null;
  settingId?: number;
  transferTypeId?: number;
  param2?: {
    emAppTransactionDataTransferType: number;
    srcTransactionId: number | null;
    destinationTransactionId: number | null;
  };
  onClose: () => void;
  onSaved: () => void;
}

const TransactionDataTransferSettingEditor: React.FC<TransactionDataTransferSettingEditorProps> = ({
  isOpen,
  mode,
  transactionId,
  transactionName,
  settingId: settingIdProp,
  transferTypeId,
  param2,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo, showWarning } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [transactionList, setTransactionList] = useState<{ Id: number; Display: string }[]>([]);
  const [settingData, setSettingData] = useState<any>(null);
  const [effectiveSettingId, setEffectiveSettingId] = useState<number | null>(settingIdProp ?? null);

  const [form, setForm] = useState<{
    Description: string;
    TransferTypeId: number;
    TransactionId: number | null;
    DestinationTransactionId: number | null;
    InternalCode: string;
  }>({
    Description: '',
    TransferTypeId: EmAppTransactionDataTransferType.FromDataModelToDataModel as number,
    TransactionId: null,
    DestinationTransactionId: null,
    InternalCode: '',
  });

  const [mappings, setMappings] = useState<DataTransferMappingRow[]>([]);
  const [srcFields, setSrcFields] = useState<TransactionFieldLookup[]>([]);
  const [destFields, setDestFields] = useState<TransactionFieldLookup[]>([]);

  const mappingGridRef = useRef<any>(null);
  const type4SaveRef = useRef<((callback?: () => void) => Promise<void>) | null>(null);
  const [type4Dirty, setType4Dirty] = useState(false);

  const mappingCV = useMemo(() => new CollectionView<DataTransferMappingRow>([]), []);

  useEffect(() => {
    mappingCV.sourceCollection = mappings;
    mappingCV.refresh();
  }, [mappingCV, mappings]);

  // Keep Root → Sibling → Child order from buildFieldLookupsFromTransaction (do not A–Z by field name).
  // Leading blank row so DataMap DDL can clear (with isRequired={false}).
  const srcFieldDataMap = useMemo(() => {
    if (!srcFields.length) return null;
    return new DataMap([{ Id: null, Display2: '' }, ...srcFields] as any[], 'Id', 'Display2');
  }, [srcFields]);

  const destFieldDataMap = useMemo(() => {
    if (!destFields.length) return null;
    return new DataMap([{ Id: null, Display2: '' }, ...destFields] as any[], 'Id', 'Display2');
  }, [destFields]);

  const loadTransactionFields = useCallback(async (txId: number | null | undefined) => {
    if (!txId) return [] as TransactionFieldLookup[];
    try {
      const data = await appTransactionService.getOneAppTransactionData(String(txId));
      return buildFieldLookupsFromTransaction(data);
    } catch {
      return [];
    }
  }, []);

  const loadData = useCallback(
    async (forceSettingId?: number | null) => {
      if (!isOpen) return;
      dispatch(setIsBusy());
      setLoading(true);
      try {
        const sid = forceSettingId !== undefined ? forceSettingId : settingIdProp ?? effectiveSettingId;
        const [massEntityData, settingRaw] = await Promise.all([
          adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView|AppTransaction'),
          sid != null
            ? appTransactionService.retrieveOneAppTransactionDataTransferSettingExDto(Number(sid))
            : Promise.resolve(null),
        ]);

        const transactions = massEntityData?.['AppTransaction'];
        if (transactions) {
          const arr = Array.isArray(transactions) ? transactions : [];
          setTransactionList(arr.map((t: any) => ({ Id: t.Id, Display: t.Display ?? t.TransactionName ?? String(t.Id) })));
        } else {
          setTransactionList([]);
        }

        let nextForm: {
          Description: string;
          TransferTypeId: number;
          TransactionId: number | null;
          DestinationTransactionId: number | null;
          InternalCode: string;
        } = {
          Description: '',
          TransferTypeId: EmAppTransactionDataTransferType.FromDataModelToDataModel as number,
          TransactionId: null,
          DestinationTransactionId: null,
          InternalCode: '',
        };
        let nextSetting: any = null;
        let nextMappings: DataTransferMappingRow[] = [];

        if (settingRaw) {
          nextSetting = settingRaw;
          setEffectiveSettingId(settingRaw.Id ?? sid);
          nextForm = {
            Description: settingRaw.Description ?? '',
            TransferTypeId: settingRaw.TransferTypeId ?? EmAppTransactionDataTransferType.FromDataModelToDataModel,
            TransactionId: settingRaw.TransactionId ?? null,
            DestinationTransactionId: settingRaw.DestinationTransactionId ?? null,
            InternalCode: settingRaw.InternalCode ?? '',
          };
          nextMappings = mappingsFromSetting(settingRaw);
        } else if (mode === 'create' && (transferTypeId != null || param2)) {
          const typeId =
            transferTypeId ?? param2?.emAppTransactionDataTransferType ?? EmAppTransactionDataTransferType.FromDataModelToDataModel;
          const srcId =
            param2?.srcTransactionId ?? (typeId === EmAppTransactionDataTransferType.FromApiToDataModel ? null : transactionId);
          const destId =
            param2?.destinationTransactionId ??
            (typeId === EmAppTransactionDataTransferType.FromApiToDataModel ? transactionId : null);
          nextForm = {
            Description: '',
            TransferTypeId: typeId,
            TransactionId: srcId ?? null,
            DestinationTransactionId: destId ?? null,
            InternalCode: '',
          };
          nextSetting = {
            TransferTypeId: typeId,
            TransactionId: srcId ?? null,
            DestinationTransactionId: destId ?? null,
            Description: '',
            InternalCode: '',
            AppTransactionSaveAsMappingList: [],
          };
          nextMappings = [];
          setEffectiveSettingId(null);
        }

        setForm(nextForm);
        setSettingData(nextSetting);

        const [src, dest] = await Promise.all([
          loadTransactionFields(nextForm.TransactionId),
          loadTransactionFields(nextForm.DestinationTransactionId),
        ]);
        setSrcFields(src);
        setDestFields(dest);

        // Angular: SRC Data Model → one row per field (all units); merge saved targets/tokens.
        const srcIsDataModel =
          nextForm.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ||
          nextForm.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel;
        if (srcIsDataModel && src.length > 0) {
          setMappings(buildAutoSourceFieldMappings(src, nextMappings, nextForm.TransactionId));
        } else {
          setMappings(nextMappings);
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to load');
      } finally {
        dispatch(setIsNotBusy());
        setLoading(false);
      }
    },
    [
      isOpen,
      mode,
      settingIdProp,
      effectiveSettingId,
      transferTypeId,
      param2,
      transactionId,
      dispatch,
      showError,
      loadTransactionFields,
    ],
  );

  useEffect(() => {
    if (isOpen) {
      setEffectiveSettingId(settingIdProp ?? null);
      void loadData(settingIdProp ?? null);
    }
    // intentionally only re-open / external settingId
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, settingIdProp]);

  const updateForm = (updates: Partial<typeof form>) => {
    setForm((prev) => ({ ...prev, ...updates }));
    setSettingData((prev: any) => (prev ? { ...prev, ...updates, IsModified: true } : prev));
  };

  const handleFromTxChange = async (txId: number | null) => {
    updateForm({ TransactionId: txId });
    const src = await loadTransactionFields(txId);
    setSrcFields(src);
    const srcIsDataModel =
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ||
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel;
    if (srcIsDataModel) {
      const prev = syncMappingsFromGrid();
      setMappings(buildAutoSourceFieldMappings(src, prev, txId));
    }
  };

  const handleToTxChange = async (txId: number | null) => {
    updateForm({ DestinationTransactionId: txId });
    setDestFields(await loadTransactionFields(txId));
  };

  const syncMappingsFromGrid = useCallback(() => {
    const flex = mappingGridRef.current?.control ?? mappingGridRef.current;
    flex?.finishEditing?.();
    const items = (mappingCV.sourceCollection as DataTransferMappingRow[]) || mappings;
    setMappings([...items]);
    return [...items];
  }, [mappingCV, mappings]);

  const addMapping = () => {
    // Type 1/2: source rows are auto-generated; Add only for Api→DM (JSON path rows).
    if (
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ||
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel
    ) {
      showWarning('Source fields are auto-filled from the From Data Model. Select a Target Field on each row.');
      return;
    }
    const row: DataTransferMappingRow = {
      SourceFiledId: null,
      TargetFiledId: null,
      Name: '',
      JsonPropertyPathName: '',
      IsBlankTargetField: false,
      TransactionId: form.TransactionId,
    };
    const next = [...syncMappingsFromGrid(), row];
    setMappings(next);
  };

  const removeMapping = () => {
    const flex = mappingGridRef.current?.control ?? mappingGridRef.current;
    const rowIndex = flex?.selection?.row;
    if (typeof rowIndex !== 'number' || rowIndex < 0) {
      showWarning('Please select a mapping row to remove');
      return;
    }
    const current = syncMappingsFromGrid();
    if (rowIndex >= current.length) return;
    // Type 1/2: clear Target/Name instead of dropping the auto source row
    if (
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate ||
      form.TransferTypeId === EmAppTransactionDataTransferType.FromDataModelToDataModel
    ) {
      const next = current.map((m, i) =>
        i === rowIndex ? { ...m, TargetFiledId: null, Name: '', Id: null } : m,
      );
      setMappings(next);
      return;
    }
    setMappings(current.filter((_, i) => i !== rowIndex));
  };

  const handleSave = useCallback(async () => {
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const typeId = form.TransferTypeId;

      // Type 4 edit: embedded editor owns mapping save (must have setting Id).
      if (typeId === EmAppTransactionDataTransferType.FromDataModelToApi && effectiveSettingId != null && type4SaveRef.current) {
        if (settingData) {
          settingData.Description = form.Description;
          settingData.TransactionId = form.TransactionId;
          settingData.DestinationTransactionId = form.DestinationTransactionId;
          settingData.InternalCode = form.InternalCode;
          settingData.IsModified = true;
        }
        await type4SaveRef.current(() => {
          showInfo('Data transfer setting saved.');
          onSaved();
        });
        return;
      }

      const currentMappings =
        typeId === EmAppTransactionDataTransferType.FromDataModelToApi
          ? []
          : filterMappingsForSave(typeId, syncMappingsFromGrid());

      const payload = prepareFullReplaceMappingSavePayload(
        settingData || {},
        form,
        currentMappings,
      );

      const result = await appTransactionService.saveOneAppTransactionDataTransferSettingExDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      const savedObj = result?.Object;
      if (savedObj != null || result?.IsSuccessful) {
        showInfo('Data transfer setting saved.');
        const newId = savedObj?.Id;
        // Type 4 create: stay open and load Full Editor after Id exists
        if (
          typeId === EmAppTransactionDataTransferType.FromDataModelToApi &&
          newId != null &&
          effectiveSettingId == null
        ) {
          setEffectiveSettingId(Number(newId));
          setType4Dirty(false);
          await loadData(Number(newId));
        } else {
          onSaved();
        }
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
      setSaving(false);
    }
  }, [
    dispatch,
    effectiveSettingId,
    form,
    loadData,
    onSaved,
    settingData,
    showError,
    showInfo,
    showValidationMessages,
    syncMappingsFromGrid,
  ]);

  if (!isOpen) return null;

  const title =
    mode === 'edit'
      ? transactionName
        ? `${transactionName}: Edit Data Transfer Setting`
        : 'Edit Data Transfer Setting'
      : transactionName
        ? `${transactionName}: New Data Transfer Setting`
        : 'New Data Transfer Setting';

  const typeId = form.TransferTypeId;
  const isType1 = typeId === EmAppTransactionDataTransferType.FromDataModelToMessageTemplate;
  const isType2 = typeId === EmAppTransactionDataTransferType.FromDataModelToDataModel;
  const isType3 = typeId === EmAppTransactionDataTransferType.FromApiToDataModel;
  const isType4 = typeId === EmAppTransactionDataTransferType.FromDataModelToApi;
  const showFromDm = isType1 || isType2 || isType4;
  const showToDm = isType2 || isType3;
  const showMappingGrid = isType1 || isType2 || isType3;
  const showType4Editor = isType4 && effectiveSettingId != null;

  const mappingSectionTitle = isType1
    ? 'Field → Message Token Mapping'
    : isType3
      ? 'API JSON Path → Field Mapping'
      : 'Source Field → Target Field Mapping';

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black/50" onClick={(e) => e.stopPropagation()}>
      <div
        className={`flex flex-col h-[90vh] max-h-[90vh] w-[95vw] max-w-[1400px] rounded-t-md rounded-b-md overflow-hidden border ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-1.5 border-b shrink-0 ${theme.modalHeader}`}>
          <h3 className={`text-sm font-semibold ${theme.title}`}>{title}</h3>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={onClose}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
              title="Cancel"
            >
              <i className="fa-solid fa-xmark" aria-hidden /> Cancel
            </button>
            <button
              type="button"
              onClick={() => void handleSave()}
              disabled={loading || saving}
              className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed`}
              title="Save"
            >
              <i className="fa-solid fa-floppy-disk" aria-hidden /> {saving ? 'Saving...' : 'Save'}
            </button>
          </div>
        </div>

        <div className="h-1 flex-auto min-h-0 overflow-auto p-3 flex flex-col gap-3">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : (
            <>
              <div className={`text-xs font-semibold ${theme.title}`}>
                Type: {transferTypeDisplay(typeId)}
                {type4Dirty ? <span className="ml-2 opacity-70 font-normal">(unsaved mapping changes)</span> : null}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-2 shrink-0">
                <div className="md:col-span-2">
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>Description</label>
                  <input
                    type="text"
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                    value={form.Description}
                    onChange={(e) => updateForm({ Description: e.target.value })}
                    placeholder="Description"
                  />
                </div>

                {showFromDm && (
                  <div>
                    <label className={`block text-xs mb-0.5 ${theme.label}`}>From Data Model</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                      value={form.TransactionId ?? ''}
                      onChange={(e) => void handleFromTxChange(e.target.value ? Number(e.target.value) : null)}
                    >
                      <option value="">—</option>
                      {transactionList.map((item) => (
                        <option key={item.Id} value={item.Id}>
                          {item.Display}
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                {showToDm && (
                  <div>
                    <label className={`block text-xs mb-0.5 ${theme.label}`}>To Data Model</label>
                    <select
                      className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                      value={form.DestinationTransactionId ?? ''}
                      onChange={(e) => void handleToTxChange(e.target.value ? Number(e.target.value) : null)}
                    >
                      <option value="">—</option>
                      {transactionList.map((item) => (
                        <option key={item.Id} value={item.Id}>
                          {item.Display}
                        </option>
                      ))}
                    </select>
                  </div>
                )}

                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>
                    {isType4 || isType3 ? 'API Operation Id (Internal Code)' : 'API (Internal Code)'}
                  </label>
                  <input
                    type="text"
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                    value={form.InternalCode}
                    onChange={(e) => updateForm({ InternalCode: e.target.value })}
                    placeholder={isType4 || isType3 ? 'API operation Id' : 'Internal code / API'}
                  />
                </div>
              </div>

              {showMappingGrid && (
                <div className="flex flex-col min-h-0 h-1 flex-auto border rounded overflow-hidden">
                  <div className={`flex items-center justify-between px-3 py-1.5 border-b shrink-0 ${theme.mainContentSection}`}>
                    <span className={`text-sm font-semibold ${theme.title}`}>{mappingSectionTitle}</span>
                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={addMapping}
                        className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                      >
                        <i className="fa-solid fa-plus" aria-hidden /> Add
                      </button>
                      <button
                        type="button"
                        onClick={removeMapping}
                        className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`}
                      >
                        <i className="fa-solid fa-minus" aria-hidden /> Remove
                      </button>
                    </div>
                  </div>
                  <div className="h-1 flex-auto min-h-[280px] overflow-hidden">
                    <FlexGrid
                      ref={mappingGridRef}
                      className="h-full w-full"
                      itemsSource={mappingCV}
                      isReadOnly={false}
                      selectionMode="Row"
                      headersVisibility="Column"
                    >
                      {isType3 ? (
                        <FlexGridColumn binding="JsonPropertyPathName" header="API JSON Path" width={280} isRequired={false} />
                      ) : (
                        <FlexGridColumn
                          binding="SourceFiledId"
                          header="Source Field"
                          width={320}
                          dataMap={srcFieldDataMap}
                          isReadOnly={isType1 || isType2}
                          isRequired={false}
                        />
                      )}
                      {isType1 ? (
                        <FlexGridColumn binding="Name" header="Message Token Name" width={220} isRequired={false} />
                      ) : (
                        <FlexGridColumn
                          binding="TargetFiledId"
                          header="Target Field"
                          width={320}
                          dataMap={destFieldDataMap}
                          isRequired={false}
                        />
                      )}
                      {isType2 ? <FlexGridColumn binding="Name" header="Note" width={160} isRequired={false} /> : null}
                      <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
                    </FlexGrid>
                  </div>
                </div>
              )}

              {isType4 && effectiveSettingId == null && (
                <p className={`text-xs ${theme.label}`}>
                  Save once to create this setting, then the full API mapping editor (params / payload / response) will appear here.
                </p>
              )}

              {showType4Editor && form.TransactionId != null && (
                <div className="flex flex-col min-h-0 h-1 flex-auto border rounded p-2 overflow-auto">
                  <div className={`text-sm font-semibold mb-2 shrink-0 ${theme.title}`}>API Call Mapping</div>
                  <DataTransferFromDataModelToApiEmbeddedEditor
                    key={effectiveSettingId}
                    settingId={Number(effectiveSettingId)}
                    srcTransactionId={Number(form.TransactionId)}
                    onMarkChange={() => setType4Dirty(true)}
                    onRegisterSave={(fn) => {
                      type4SaveRef.current = fn;
                    }}
                  />
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default TransactionDataTransferSettingEditor;
