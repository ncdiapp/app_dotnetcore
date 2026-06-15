/**
 * Data Load Editor (create/edit single data load)
 * Ported from AngularJS: TransactionDataLoadEditor.cshtml + transactionDataLoadEditorCtrl.js
 */

import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { searchSvc } from '../../../webapi/searchSvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';

export interface TransactionDataLoadEditorProps {
  isOpen: boolean;
  mode: 'create' | 'edit';
  transactionId: number;
  transactionUnitId: number;
  dataLoadId?: number;
  onClose: () => void;
  onSaved: () => void;
}

const AVAILABLE_WHERE_TOKENS = [
  { TokenName: '=' }, { TokenName: '<>' }, { TokenName: '>' }, { TokenName: '<' },
  { TokenName: '>=' }, { TokenName: '<=' }, { TokenName: ' AND ' }, { TokenName: ' OR ' },
];

const TransactionDataLoadEditor: React.FC<TransactionDataLoadEditorProps> = ({
  isOpen,
  mode,
  transactionId,
  transactionUnitId,
  dataLoadId,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [currentDataLoad, setCurrentDataLoad] = useState<any>(null);
  const [conditionMappingList, setConditionMappingList] = useState<any[]>([]);
  const [fieldValueMappingList, setFieldValueMappingList] = useState<any[]>([]);
  const [allDataSet, setAllDataSet] = useState<any[]>([]);
  const [dataSetColumns, setDataSetColumns] = useState<any[]>([]);
  const [transactionFieldLookup, setTransactionFieldLookup] = useState<any[]>([]);
  const [dictDeletedIds, setDictDeletedIds] = useState<Record<string, any[]>>({});

  const conditionCV = useMemo(() => new CollectionView(conditionMappingList), [conditionMappingList]);
  const fieldValueCV = useMemo(() => new CollectionView(fieldValueMappingList), [fieldValueMappingList]);
  const allDataSetCV = useMemo(() => {
    const cv = new CollectionView(allDataSet);
    cv.sortDescriptions.push(new SortDescription('Name', true));
    return cv;
  }, [allDataSet]);
  const dataSetColumnsCV = useMemo(() => new CollectionView(dataSetColumns), [dataSetColumns]);
  const transactionFieldCV = useMemo(() => new CollectionView(transactionFieldLookup), [transactionFieldLookup]);
  const dataSetColumnsDataMap = useMemo(() => {
    const list = dataSetColumns.map((c: any) => ({ ...c, ExpressionDisplay: '[DBColumn_' + (c.Id ?? c) + ']' }));
    return list.length ? new DataMap(list, 'Id', 'ExpressionDisplay') : null;
  }, [dataSetColumns]);
  const transactionFieldDataMap = useMemo(() => {
    return transactionFieldLookup.length ? new DataMap(transactionFieldLookup, 'Id', 'Display') : null;
  }, [transactionFieldLookup]);

  const loadData = useCallback(async () => {
    if (!transactionId || !transactionUnitId) return;
    setLoading(true);
    try {
      const [txnData, dataSetList] = await Promise.all([
        appTransactionService.getOneHierarchyTransaction(String(transactionId), false, '', '', '', false, ''),
        searchSvc.retrieveAllAppDataSetEntityDto(),
      ]);

      setAllDataSet(Array.isArray(dataSetList) ? dataSetList : []);

      const dictUnitIdAndFieldLookUp: Record<number, any[]> = {};
      const rootLevel: any[] = [];
      (txnData?.AppTransactionUnitList || []).forEach((unit: any) => {
        dictUnitIdAndFieldLookUp[unit.Id] = rootLevel;
        (unit.AppTransactionFieldList || []).forEach((field: any) => {
          rootLevel.push({
            Id: field.Id,
            Display: unit.IsMasterSiblingUnit ? (unit.DataBaseTableName + '.' + field.DataBaseFieldName) : field.DataBaseFieldName,
            ExpressionDisplay: '[Field_' + (unit.IsMasterSiblingUnit ? unit.DataBaseTableName + '.' + field.DataBaseFieldName : field.DataBaseFieldName) + ']',
          });
        });
        (unit.Children || []).forEach((child: any) => {
          dictUnitIdAndFieldLookUp[child.Id] = [];
          (child.AppTransactionFieldList || []).forEach((field: any) => {
            dictUnitIdAndFieldLookUp[child.Id].push({
              Id: field.Id,
              Display: field.DataBaseFieldName,
              ExpressionDisplay: '[Field_' + field.DataBaseFieldName + ']',
            });
          });
          (child.Children || []).forEach((grand: any) => {
            dictUnitIdAndFieldLookUp[grand.Id] = [];
            (grand.AppTransactionFieldList || []).forEach((field: any) => {
              dictUnitIdAndFieldLookUp[grand.Id].push({
                Id: field.Id,
                Display: field.DataBaseFieldName,
                ExpressionDisplay: '[Field_' + field.DataBaseFieldName + ']',
              });
            });
          });
        });
      });
      setTransactionFieldLookup(dictUnitIdAndFieldLookUp[transactionUnitId] || []);

      if (mode === 'edit' && dataLoadId) {
        const dataLoadData = await appTransactionService.retrieveOneAppTransactionDataLoadExDto(dataLoadId);
        setCurrentDataLoad(dataLoadData);
        const cond: any[] = [];
        const fld: any[] = [];
        (dataLoadData?.AppTranscationDataLoadFieldMappingList || []).forEach((m: any) => {
          if (m.IsConditionMapping) cond.push(m);
          else fld.push(m);
        });
        setConditionMappingList(cond);
        setFieldValueMappingList(fld);
        setDictDeletedIds(dataLoadData?.DictDeletedItemsIds || {});
        if (dataLoadData?.DataSetId) {
          const cols = await searchSvc.retrieveQueryColumnList(String(dataLoadData.DataSetId));
          const list = Array.isArray(cols) ? cols : [];
          setDataSetColumns(list.map((c: any) => ({ ...c, ExpressionDisplay: '[DBColumn_' + (c.Id ?? c) + ']' })));
        } else {
          setDataSetColumns([]);
        }
      } else {
        setCurrentDataLoad({
          TransactionId: transactionId,
          TransactionUnitId: transactionUnitId,
          DataSetId: null,
          LoadName: '',
          Description: '',
          LoadOrder: null,
          IsAutoExcutedWhenOpenEditForm: false,
          IsAutoExecuteBeforeIntialCscading: false,
          AppTranscationDataLoadFieldMappingList: [],
          DictDeletedItemsIds: {},
        });
        setConditionMappingList([]);
        setFieldValueMappingList([]);
        setDictDeletedIds({});
        setDataSetColumns([]);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load data load');
    } finally {
      setLoading(false);
    }
  }, [isOpen, mode, transactionId, transactionUnitId, dataLoadId]);

  useEffect(() => {
    if (isOpen) loadData();
  }, [isOpen, loadData]);

  const handleDataSetChange = useCallback(async (sender: any) => {
    const dataSetId = sender?.selectedValue ?? null;
    setCurrentDataLoad((p: any) => (p ? { ...p, DataSetId: dataSetId } : p));
    if (!dataSetId) {
      setDataSetColumns([]);
      return;
    }
    try {
      const cols = await searchSvc.retrieveQueryColumnList(String(dataSetId));
      const list = Array.isArray(cols) ? cols : [];
      setDataSetColumns(list.map((c: any) => ({ ...c, ExpressionDisplay: '[DBColumn_' + (c.Id ?? c) + ']' })));
    } catch (e: any) {
      showError(e?.message || 'Failed to load dataset columns');
    }
  }, [showError]);

  const addConditionMapping = useCallback(() => {
    setConditionMappingList((prev) => [...prev, { IsConditionMapping: 1, DbcolumnName: null, TransactionFieldId: null, WhereClause: '' }]);
  }, []);

  const conditionGridRef = useRef<any>(null);
  const removeConditionMapping = useCallback(() => {
    const flex = conditionGridRef.current;
    const row = flex?.selection?.row;
    if (row == null || row < 0) return;
    const item = conditionMappingList[row];
    if (!item) return;
    const deleted = dictDeletedIds['AppTranscationDataLoadFieldMappingList'] || [];
    if (item.Id) setDictDeletedIds((p) => ({ ...p, AppTranscationDataLoadFieldMappingList: [...deleted, item.Id] }));
    setConditionMappingList((prev) => prev.filter((_, i) => i !== row));
  }, [conditionMappingList, dictDeletedIds]);

  const addFieldValueMapping = useCallback(() => {
    setFieldValueMappingList((prev) => [...prev, { IsConditionMapping: 0, TransactionFieldId: null, DbcolumnName: null }]);
  }, []);

  const fieldValueGridRef = useRef<any>(null);
  const removeFieldValueMapping = useCallback(() => {
    const flex = fieldValueGridRef.current;
    const row = flex?.selection?.row;
    if (row == null || row < 0) return;
    const item = fieldValueMappingList[row];
    if (!item) return;
    const deleted = dictDeletedIds['AppTranscationDataLoadFieldMappingList'] || [];
    if (item.Id) setDictDeletedIds((p) => ({ ...p, AppTranscationDataLoadFieldMappingList: [...deleted, item.Id] }));
    setFieldValueMappingList((prev) => prev.filter((_, i) => i !== row));
  }, [fieldValueMappingList, dictDeletedIds]);

  const handleSave = useCallback(async () => {
    const payload = {
      ...currentDataLoad,
      AppTranscationDataLoadFieldMappingList: [...conditionMappingList, ...fieldValueMappingList],
      DictDeletedItemsIds: dictDeletedIds,
      IsModified: true,
    };
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAppTransactionDataLoadExDto(payload);
      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful && result?.Object) {
        showInfo('Data load saved.');
        onSaved();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentDataLoad, conditionMappingList, fieldValueMappingList, dictDeletedIds, dispatch, showError, showValidationMessages, showInfo, onSaved]);

  const updateDataLoad = useCallback((updates: Partial<any>) => {
    setCurrentDataLoad((p: any) => (p ? { ...p, ...updates } : p));
  }, []);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black/50" onClick={(e) => e.stopPropagation()}>
      <div
        className={`flex flex-col max-h-[90vh] w-[95vw] max-w-[1200px] rounded-t-md rounded-b-md overflow-hidden border ${theme.mainContentSection}`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-1.5 border-b ${theme.modalHeader}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>Data Load Editor</div>
          <div className="flex items-center space-x-2">
            <button type="button" onClick={loadData} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Refresh"><i className="fa fa-refresh" aria-hidden="true" /> Refresh</button>
            <button type="button" onClick={handleSave} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Save"><i className="fa fa-save" aria-hidden="true" /> Save</button>
            <button type="button" onClick={onClose} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Close"><i className="fa fa-times" aria-hidden="true" /> Close</button>
          </div>
        </div>
        <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5 space-y-2">
          {loading ? (
            <p className={`text-xs ${theme.label}`}>Loading...</p>
          ) : (
            <>
              <div className="grid grid-cols-2 gap-4 mb-4">
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>Load Name</label>
                  <input
                    type="text"
                    value={currentDataLoad?.LoadName ?? ''}
                    onChange={(e) => updateDataLoad({ LoadName: e.target.value })}
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                  />
                </div>
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>Description</label>
                  <input
                    type="text"
                    value={currentDataLoad?.Description ?? ''}
                    onChange={(e) => updateDataLoad({ Description: e.target.value })}
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                  />
                </div>
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>Dataset</label>
                  <ComboBox
                    itemsSource={allDataSetCV}
                    displayMemberPath="Name"
                    selectedValuePath="Id"
                    selectedValue={currentDataLoad?.DataSetId ?? null}
                    selectedIndexChanged={(s: any) => handleDataSetChange(s)}
                  />
                </div>
                <div>
                  <label className={`block text-xs mb-0.5 ${theme.label}`}>Load Order</label>
                  <input
                    type="number"
                    value={currentDataLoad?.LoadOrder ?? ''}
                    onChange={(e) => updateDataLoad({ LoadOrder: e.target.value === '' ? null : parseInt(e.target.value, 10) })}
                    className={`w-full h-7 px-2 text-xs border rounded ${theme.inputBox} focus:outline-none`}
                  />
                </div>
                <div className="col-span-2 flex gap-4 items-center">
                  <label className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={!!currentDataLoad?.IsAutoExcutedWhenOpenEditForm}
                      onChange={(e) => updateDataLoad({ IsAutoExcutedWhenOpenEditForm: e.target.checked })}
                    />
                    <span className={`text-xs ${theme.label}`}>Auto Executed On Open Form</span>
                  </label>
                  {currentDataLoad?.IsAutoExcutedWhenOpenEditForm && (
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={!!currentDataLoad?.IsAutoExecuteBeforeIntialCscading}
                        onChange={(e) => updateDataLoad({ IsAutoExecuteBeforeIntialCscading: e.target.checked })}
                      />
                      <span className={`text-xs ${theme.label}`}>Executed Before Initial Cascading</span>
                    </label>
                  )}
                </div>
              </div>

              <div>
                <div className={`flex items-center justify-between px-3 py-1.5 mb-1 border-b ${theme.mainContentSection}`}>
                  <span className={`text-sm font-semibold ${theme.title}`}>Condition Mapping</span>
                  <div className="flex items-center space-x-2">
                    <button type="button" onClick={addConditionMapping} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Add"><i className="fa fa-plus" aria-hidden="true" /> Add</button>
                    <button type="button" onClick={removeConditionMapping} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Remove"><i className="fa fa-minus" aria-hidden="true" /> Remove</button>
                  </div>
                </div>
                <div className="h-48 overflow-auto border rounded mt-2">
                  <FlexGrid
                    ref={conditionGridRef}
                    itemsSource={conditionCV}
                    isReadOnly={false}
                    selectionMode="Row"
                    headersVisibility="Column"
                  >
                    <FlexGridColumn binding="DbcolumnName" header="DB Column" width={200} dataMap={dataSetColumnsDataMap} />
                    <FlexGridColumn binding="TransactionFieldId" header="Data Model Field" width={200} dataMap={transactionFieldDataMap} />
                    <FlexGridColumn binding="WhereClause" header="WhereClause" width={400} />
                    <FlexGridColumn width="*" isReadOnly={true} />
                  </FlexGrid>
                </div>
              </div>

              <div>
                <div className={`flex items-center justify-between px-3 py-1.5 mb-1 border-b ${theme.mainContentSection}`}>
                  <span className={`text-sm font-semibold ${theme.title}`}>Update Field Mapping</span>
                  <div className="flex items-center space-x-2">
                    <button type="button" onClick={addFieldValueMapping} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Add"><i className="fa fa-plus" aria-hidden="true" /> Add</button>
                    <button type="button" onClick={removeFieldValueMapping} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Remove"><i className="fa fa-minus" aria-hidden="true" /> Remove</button>
                  </div>
                </div>
                <div className="h-48 overflow-auto border rounded mt-2">
                  <FlexGrid
                    ref={fieldValueGridRef}
                    itemsSource={fieldValueCV}
                    isReadOnly={false}
                    selectionMode="Row"
                    headersVisibility="Column"
                  >
                    <FlexGridColumn binding="TransactionFieldId" header="Data Model Field" width={200} dataMap={transactionFieldDataMap} />
                    <FlexGridColumn binding="DbcolumnName" header="DB Column" width={200} dataMap={dataSetColumnsDataMap} />
                    <FlexGridColumn width="*" isReadOnly={true} />
                  </FlexGrid>
                </div>
              </div>
            </>
          )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionDataLoadEditor;
