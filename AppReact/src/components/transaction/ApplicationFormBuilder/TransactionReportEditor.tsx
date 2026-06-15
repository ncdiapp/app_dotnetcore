/**
 * Data Model Report Editor
 * Ported from AngularJS: TransactionReportEditor.cshtml + transactionReportEditorCtrl.js
 * Reference: C:\DevApp\App\PlmApplication\Server\Views\Transaction\TransactionReportEditor.cshtml
 *            C:\DevApp\App\PlmApplication\Scripts1x\mgtCtrl\Transaction\transactionReportEditorCtrl.js
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';

export interface TransactionReportEditorProps {
  transactionId: number | null;
  transactionName?: string | null;
  onRefresh?: () => void;
}

const TransactionReportEditor: React.FC<TransactionReportEditorProps> = ({
  transactionId,
  transactionName: transactionNameProp,
  onRefresh,
}) => {
  const { theme } = useTheme();
  const { showError, showValidationMessages, showInfo } = useErrorMessage();
  const dispatch = useDispatch();

  const [loading, setLoading] = useState(true);
  const [transactionName, setTransactionName] = useState(transactionNameProp || '');
  const [availableReports, setAvailableReports] = useState<any[]>([]);
  const [selectedReports, setSelectedReports] = useState<any[]>([]);
  const [deletedItemIds, setDeletedItemIds] = useState<number[]>([]);
  const [allReportItems, setAllReportItems] = useState<any[]>([]);
  const [reportDataMap, setReportDataMap] = useState<DataMap | null>(null);
  const dictReportIdAndDto = useRef<Record<number, any>>({});

  const flexAvailableRef = useRef<any>(null);
  const flexSelectedRef = useRef<any>(null);

  const availableItemsCV = React.useMemo(() => new CollectionView(availableReports), [availableReports]);
  const selectedItemsCV = React.useMemo(() => new CollectionView(selectedReports), [selectedReports]);

  const loadData = useCallback(async () => {
    if (!transactionId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    setDeletedItemIds([]);
    try {
      const [allReportItemsRes, transactionReportsRes] = await Promise.all([
        adminSvc.retrieveAllAppReportEntityDto(),
        appTransactionService.retrieveAllAppTranscationReportListByTransactionId(transactionId),
      ]);

      const allReports = Array.isArray(allReportItemsRes) ? allReportItemsRes : allReportItemsRes?.Items ?? [];
      const transactionReports = Array.isArray(transactionReportsRes) ? transactionReportsRes : transactionReportsRes?.Items ?? [];

      const dict: Record<number, any> = {};
      allReports.forEach((r: any) => {
        r.Display = r.ReportName ?? r.Display;
        dict[r.Id] = r;
      });
      dictReportIdAndDto.current = dict;
      setAllReportItems(allReports);
      setReportDataMap(new DataMap(allReports, 'Id', 'Display'));

      const available = allReports.filter((r: any) => !transactionReports.some((s: any) => s.ReportId === r.Id));
      transactionReports.forEach((sel: any) => {
        const report = dict[sel.ReportId];
        if (report) sel.ReportName = report.ReportName;
      });
      setAvailableReports(available);
      setSelectedReports(transactionReports);
    } catch (e: any) {
      showError(e?.message || 'Failed to load reports');
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [transactionId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [transactionId]);

  const handleRefresh = useCallback(() => {
    loadData();
    onRefresh?.();
  }, [loadData, onRefresh]);

  const handleSelectItems = useCallback(() => {
    const flex = flexAvailableRef.current;
    if (!flex?.selectedRows?.length) return;
    const toAdd: any[] = [];
    flex.selectedRows.forEach((row: any) => {
      const dataItem = row.dataItem;
      if (dataItem) {
        toAdd.push({
          ReportId: dataItem.Id,
          ReportName: dataItem.ReportName,
          ReportDisplayName: dataItem.ReportName ?? dataItem.Display,
          TranscationId: transactionId,
        });
      }
    });
    if (toAdd.length === 0) return;
    const newAvailable = availableReports.filter((r) => !toAdd.some((a) => a.ReportId === r.Id));
    setAvailableReports(newAvailable);
    setSelectedReports((prev) => [...prev, ...toAdd]);
  }, [availableReports, transactionId]);

  const handleRemoveItems = useCallback(() => {
    const flex = flexSelectedRef.current;
    if (!flex?.selectedRows?.length) return;
    const toRemove: any[] = [];
    flex.selectedRows.forEach((row: any) => toRemove.push(row.dataItem));
    const toAddToAvailable: any[] = [];
    const idsToDelete: number[] = [];
    toRemove.forEach((dataItem: any) => {
      if (dataItem?.ReportId && dictReportIdAndDto.current[dataItem.ReportId]) {
        const reportDto = dictReportIdAndDto.current[dataItem.ReportId];
        toAddToAvailable.push({ ...reportDto });
        if (dataItem.Id) idsToDelete.push(dataItem.Id);
      }
    });
    if (toAddToAvailable.length > 0 || idsToDelete.length > 0) {
      setAvailableReports((prev) => [...prev, ...toAddToAvailable]);
      setSelectedReports((prev) => prev.filter((x) => !toRemove.includes(x)));
      setDeletedItemIds((prev) => [...prev, ...idsToDelete]);
    }
  }, []);

  const handleSave = useCallback(async () => {
    if (!transactionId) return;
    const payload = {
      TransactionId: transactionId,
      TransactionReportSet: selectedReports,
      DeletedItemIds: deletedItemIds,
    };
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAllAppTranscationReportExDto(payload);
      if (result?.ValidationResult) {
        showValidationMessages(result.ValidationResult, true);
      }
      if (result?.IsSuccessful && result?.ObjectList) {
        showInfo('Reports saved.');
        loadData();
        onRefresh?.();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [transactionId, selectedReports, deletedItemIds, dispatch, showError, showValidationMessages, showInfo, loadData, onRefresh]);

  if (!transactionId) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Data Model Report Editor</div>
        </div>
        <div className={`w-full flex-auto overflow-auto ${theme.mainContentSection}`}>
          <div className="h-full w-full overflow-auto px-5 py-5">
            <p className={`text-xs ${theme.label}`}>Select a data model first to configure Reports.</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-1.5 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Data Model Report Editor</div>
        <div className="flex items-center space-x-2">
          <button type="button" onClick={handleRefresh} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Refresh">
            <i className="fa fa-refresh" aria-hidden="true" /> Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default} flex items-center gap-1`} title="Save">
            <i className="fa fa-save" aria-hidden="true" /> Save
          </button>
        </div>
      </div>

      <div className={`px-3 py-0.5 text-xs ${theme.label}`}>
        Data Model: {transactionName || '—'}
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden flex gap-2 p-2 ${theme.mainContentSection}`}>
        <div className="flex flex-col flex-1 min-w-0" style={{ maxWidth: 400 }}>
          <div className={`flex items-center px-3 py-1.5 rounded-t border-b ${theme.mainContentSection}`}>
            <span className={`text-sm font-semibold ${theme.title}`}>Available Reports</span>
          </div>
          <div className={`flex-1 overflow-hidden border rounded-b ${theme.mainContentSection}`}>
            {loading ? (
              <div className="p-4 flex items-center justify-center">
                <p className={`text-xs ${theme.label}`}>Loading...</p>
              </div>
            ) : (
              <FlexGrid
                ref={flexAvailableRef}
                itemsSource={availableItemsCV}
                isReadOnly={true}
                selectionMode="ListBox"
                headersVisibility="Column"
              >
                <FlexGridColumn binding="Id" header="Report ID" width={80} visible={false} format="d" />
                <FlexGridColumn binding="ReportName" header="Report Name" width="*" />
                <FlexGridColumn binding="ReportFileName" header="File Name" width="*" />
              </FlexGrid>
            )}
          </div>
        </div>

        <div className="flex flex-col justify-center gap-2 w-12 flex-none">
          <button
            type="button"
            onClick={handleSelectItems}
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs flex items-center justify-center`}
            title="Add selected"
          >
            <i className="fa fa-chevron-right" aria-hidden="true" />
          </button>
          <button
            type="button"
            onClick={handleRemoveItems}
            className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs flex items-center justify-center`}
            title="Remove selected"
          >
            <i className="fa fa-chevron-left" aria-hidden="true" />
          </button>
        </div>

        <div className="flex flex-col flex-1 min-w-0" style={{ maxWidth: 400 }}>
          <div className={`flex items-center px-3 py-1.5 rounded-t border-b ${theme.mainContentSection}`}>
            <span className={`text-sm font-semibold ${theme.title}`}>Selected Reports</span>
          </div>
          <div className={`flex-1 overflow-hidden border rounded-b ${theme.mainContentSection}`}>
            {loading ? (
              <div className="p-4 flex items-center justify-center">
                <p className={`text-xs ${theme.label}`}>Loading...</p>
              </div>
            ) : (
              <FlexGrid
                ref={flexSelectedRef}
                itemsSource={selectedItemsCV}
                isReadOnly={false}
                selectionMode="ListBox"
                headersVisibility="Column"
              >
                <FlexGridColumn binding="ReportId" header="Report Name" width="*" isReadOnly={true} dataMap={reportDataMap} />
                <FlexGridColumn binding="ReportDisplayName" header="Display Name" width="*" />
              </FlexGrid>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionReportEditor;
