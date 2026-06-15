/**
 * Unit Link Target Editor – Angular-style layout: left grid (Menus) to select, right panel (Menu Properties) to edit.
 * Ported from: TransactionUnitNavigateToFormEditor.cshtml + _LinkToFormMenuProperties.cshtml
 */
import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';

const LINK_TARGET_ACTIONS = [
  { Id: 2, Display: 'Create Blank Form' },
  { Id: 13, Display: 'Create New Form Using Current Data' },
  { Id: 1, Display: 'Edit Form' },
  { Id: 5, Display: 'Preview Form' },
  { Id: 3, Display: 'Delete Form' },
];

export interface TransactionUnitLinkTargetEditorProps {
  isOpen: boolean;
  transactionUnitId: number | null;
  unitDisplayName?: string | null;
  linkTargetUsageType: number;
  title: string;
  onClose: () => void;
  onSaved?: () => void;
}

const TransactionUnitLinkTargetEditor: React.FC<TransactionUnitLinkTargetEditorProps> = ({
  isOpen,
  transactionUnitId,
  unitDisplayName,
  linkTargetUsageType,
  title,
  onClose,
  onSaved,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();
  const [loading, setLoading] = useState(true);
  const [unitData, setUnitData] = useState<any>(null);
  const [linkTargetList, setLinkTargetList] = useState<any[]>([]);
  const [deletedIds, setDeletedIds] = useState<number[]>([]);
  const [transactionList, setTransactionList] = useState<any[]>([]);
  const [routeStateList, setRouteStateList] = useState<any[]>([]);
  const [currentEditMenu, setCurrentEditMenu] = useState<any | null>(null);
  const [targetFieldList, setTargetFieldList] = useState<any[]>([]);
  const gridRef = useRef<any>(null);
  const hasInitialSelection = useRef(false);

  const isAnyPage = Number(linkTargetUsageType) === 5;

  const transactionDataMap = useMemo(() => {
    if (!transactionList.length) return null;
    return new DataMap(transactionList, 'Id', 'TransactionName');
  }, [transactionList]);

  const routeStateDataMap = useMemo(() => {
    if (!routeStateList.length) return null;
    // store route code in LinkTargetUrlOrRouteCode, display StateCode (Angular shows "UserEditor" etc)
    return new DataMap(routeStateList, 'StateCode', 'StateCode');
  }, [routeStateList]);

  const linkTargetCV = useMemo(() => {
    const cv = new CollectionView(linkTargetList);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    return cv;
  }, [linkTargetList]);

  const unitFields = unitData?.AppTransactionFieldList || [];

  const loadData = useCallback(async () => {
    if (!isOpen || !transactionUnitId) {
      setLoading(false);
      return;
    }
    hasInitialSelection.current = false;
    dispatch(setIsBusy());
    setLoading(true);
    try {
      const [unitExDto, linkTargetsRaw, txnList] = await Promise.all([
        appTransactionService.retrieveOneAppTransactionUnitExDto(String(transactionUnitId)),
        appTransactionService.retrieveOneTransactionUnitLinkTargetList(String(transactionUnitId)),
        appTransactionService.retrieveAllAppTransactions(null, 1, false),
      ]);
      setUnitData(unitExDto || null);
      const allTargets = Array.isArray(linkTargetsRaw) ? linkTargetsRaw : [];
      const filtered = allTargets.filter((t: any) => Number(t.LinkTargetUsageType) === linkTargetUsageType);
      setLinkTargetList(filtered);
      setDeletedIds([]);
      setTransactionList(Array.isArray(txnList) ? txnList : []);
      setCurrentEditMenu(filtered.length > 0 ? filtered[0] : null);
    } catch (e: any) {
      showError(e?.message || 'Failed to load link targets');
      setLinkTargetList([]);
      setUnitData(null);
      setTransactionList([]);
      setRouteStateList([]);
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [isOpen, transactionUnitId, linkTargetUsageType, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Load built-in pages (route states) when editing Navigate To Any Page
  useEffect(() => {
    if (!isOpen || !isAnyPage) return;
    adminSvc
      .retrieveAllAppRouteStateEntityDto()
      .then((list: any) => setRouteStateList(Array.isArray(list) ? list : []))
      .catch(() => setRouteStateList([]));
  }, [isOpen, isAnyPage]);

  // After initial load, select first row in grid once so it matches currentEditMenu
  useEffect(() => {
    if (loading || linkTargetList.length === 0 || hasInitialSelection.current) return;
    const t = setTimeout(() => {
      const grid = gridRef.current?.control;
      if (grid?.rows?.length) {
        grid.select(0, 0);
        hasInitialSelection.current = true;
      }
    }, 50);
    return () => clearTimeout(t);
  }, [loading, linkTargetList.length]);

  // Load target transaction root unit fields when target data model changes
  useEffect(() => {
    if (isAnyPage) return;
    const tid = currentEditMenu?.LinkTargetTransactionId;
    if (!tid) {
      setTargetFieldList([]);
      return;
    }
    appTransactionService
      .getOneHierarchyTransaction(String(tid), false, '', '', '', false, '')
      .then((h: any) => {
        const units = h?.AppTransactionUnitList;
        const root = units && units.length ? units[0] : null;
        const fields = root?.AppTransactionFieldList || [];
        setTargetFieldList(Array.isArray(fields) ? fields : []);
      })
      .catch(() => setTargetFieldList([]));
  }, [currentEditMenu?.LinkTargetTransactionId]);

  const handleSelectionChanged = useCallback(() => {
    const grid = gridRef.current?.control;
    if (!grid) return;
    const row = grid.selection?.row;
    if (row == null || row < 0 || !grid.rows?.length) {
      setCurrentEditMenu(null);
      return;
    }
    const item = grid.rows[row]?.dataItem;
    setCurrentEditMenu(item || null);
  }, []);

  const handleAdd = useCallback(() => {
    if (!transactionUnitId) return;
    const newItem: any = {
      TransactionUnitId: transactionUnitId,
      LinkTargetUsageType: linkTargetUsageType,
      NavigationActionName: '',
      ActionType: 1,
      LinkTargetTransactionId: null,
      LinkTargetUrlOrRouteCode: null,
      SourceColumn1: null,
      TargetColumn1: null,
      SourceColumn2: null,
      TargetColumn2: null,
      SourceColumn3: null,
      TargetColumn3: null,
      RowDisplayDbField: null,
      SourceConditionColumn: null,
      Sort: linkTargetList.length + 1,
      IsPopup: false,
      PopupWidth: 800,
      PopupHeight: 600,
      IconName: '',
    };
    const next = [...linkTargetList, newItem];
    setLinkTargetList(next);
    setCurrentEditMenu(newItem);
    setTimeout(() => {
      const grid = gridRef.current?.control;
      if (grid && grid.rows?.length) grid.select(grid.rows.length - 1, 0);
    }, 0);
  }, [transactionUnitId, linkTargetUsageType, linkTargetList.length]);

  const handleDelete = useCallback(() => {
    if (!currentEditMenu) {
      showInfo('Select a menu to delete');
      return;
    }
    if (currentEditMenu.Id) setDeletedIds((prev) => [...prev, currentEditMenu.Id]);
    setLinkTargetList((prev) => prev.filter((r) => r !== currentEditMenu));
    setCurrentEditMenu(null);
  }, [currentEditMenu, showInfo]);

  const handleSave = useCallback(async () => {
    if (!transactionUnitId) return;
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveOneTransactionUnitLinkTargetList({
        TransactionUnitId: transactionUnitId,
        DeletedItemIds: deletedIds,
        AppFormLinkTargetDtoSet: linkTargetList,
      });
      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful && result?.ObjectList) {
        showInfo('Link targets saved.');
        const filtered = (result.ObjectList as any[]).filter(
          (t: any) => Number(t.LinkTargetUsageType) === linkTargetUsageType
        );
        setLinkTargetList(filtered);
        setDeletedIds([]);
        onSaved?.();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    transactionUnitId,
    deletedIds,
    linkTargetList,
    linkTargetUsageType,
    dispatch,
    showError,
    showInfo,
    showValidationMessages,
    onSaved,
  ]);

  const updateCurrent = useCallback((key: string, value: any) => {
    if (!currentEditMenu) return;
    currentEditMenu[key] = value;
    setLinkTargetList((prev) => [...prev]);
  }, [currentEditMenu]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black bg-opacity-50">
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '95vw', height: '90vh', maxWidth: 1200, maxHeight: 720 }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Top: Unit title + Refresh / Save / Close (no Add/Delete here) */}
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>
            Unit: <span title={unitData?.DataBaseTableName}>{unitDisplayName || 'Unit'}</span>
            <span className="ml-2">– {title}</span>
          </div>
          <div className="flex items-center gap-2">
            <button type="button" onClick={loadData} className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}>
              <i className="fa fa-refresh mr-1" /> Refresh
            </button>
            <button type="button" onClick={handleSave} className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}>
              <i className="fa fa-save mr-1" /> Save
            </button>
            <button type="button" onClick={onClose} className="px-3 py-1 rounded-[4px] text-xs border">
              Close
            </button>
          </div>
        </div>

        <div className="flex-1 min-h-0 flex overflow-hidden">
          {loading ? (
            <div className={`p-4 ${theme.label}`}>Loading...</div>
          ) : (
            <>
              {/* Left: Menus grid (click to select) */}
              <div className="basis-3/5 flex flex-col border-r overflow-hidden">
                <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                  <div className={`text-xs font-semibold ${theme.title}`}>
                    <i className="fa-solid fa-file-lines mr-1 opacity-60" /> Menus
                  </div>
                  <div className="flex items-center gap-1">
                    <button type="button" onClick={handleAdd} className={`px-2 py-1 ${theme.button_default} rounded text-xs`}>
                      <i className="fa fa-plus mr-1" /> Add
                    </button>
                    <button type="button" onClick={handleDelete} className={`px-2 py-1 ${theme.button_default} rounded text-xs`}>
                      <i className="fa fa-trash mr-1" /> Delete
                    </button>
                  </div>
                </div>
                <div className="flex-1 min-h-0">
                  <FlexGrid
                    ref={gridRef}
                    itemsSource={linkTargetCV}
                    isReadOnly={true}
                    selectionMode="Row"
                    selectionChanged={handleSelectionChanged}
                    style={{ height: '100%', border: 'none' }}
                  >
                    <FlexGridFilter />
                    <FlexGridColumn binding="Sort" header="Sort" width={80} />
                    <FlexGridColumn binding="NavigationActionName" header="Menu Name" width={180} />
                    {isAnyPage ? (
                      <FlexGridColumn
                        binding="LinkTargetUrlOrRouteCode"
                        header="Navigate To Page"
                        width={220}
                        dataMap={routeStateDataMap}
                      />
                    ) : (
                      <FlexGridColumn
                        binding="LinkTargetTransactionId"
                        header="Navigate To Data Model"
                        width={220}
                        dataMap={transactionDataMap}
                      />
                    )}
                    {!isAnyPage && (
                      <FlexGridColumn
                        binding="ActionType"
                        header="Action"
                        width={150}
                        dataMap={new DataMap(LINK_TARGET_ACTIONS, 'Id', 'Display')}
                      />
                    )}
                    <FlexGridColumn header="" width="*" />
                  </FlexGrid>
                </div>
              </div>

              {/* Right: Menu Properties form */}
              <div className="basis-2/5 flex flex-col overflow-hidden">
                <div className={`px-4 py-2 border-b ${theme.mainContentSection}`}>
                  <div className={`text-xs font-semibold ${theme.title}`}>
                    <i className="fa fa-gear mr-1 opacity-60" /> Menu Properties
                  </div>
                </div>
                <div className="flex-1 overflow-y-auto p-4">
                  {!currentEditMenu ? (
                    <div className={`text-sm ${theme.label}`}>Select a menu from the left to edit.</div>
                  ) : (
                    <div className="space-y-3 max-w-xl">
                      <div className="flex items-center gap-2">
                        <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Sort Order</label>
                        <input
                          type="number"
                          min={1}
                          value={currentEditMenu.Sort ?? 1}
                          onChange={(e) => updateCurrent('Sort', parseInt(e.target.value, 10) || 1)}
                          className={`w-24 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                        />
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Menu Name</label>
                        <input
                          type="text"
                          value={currentEditMenu.NavigationActionName ?? ''}
                          onChange={(e) => updateCurrent('NavigationActionName', e.target.value)}
                          className={`flex-1 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                        />
                      </div>
                      {!isAnyPage && (
                        <div className="flex items-center gap-2">
                          <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Action Type</label>
                          <select
                            value={currentEditMenu.ActionType ?? ''}
                            onChange={(e) => updateCurrent('ActionType', e.target.value ? Number(e.target.value) : null)}
                            className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                          >
                            {LINK_TARGET_ACTIONS.map((a) => (
                              <option key={a.Id} value={a.Id}>{a.Display}</option>
                            ))}
                          </select>
                        </div>
                      )}
                      <div className="flex items-center gap-2">
                        <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>{isAnyPage ? 'Navigate To Page' : 'Navigate To Data Model'}</label>
                        {isAnyPage ? (
                          <select
                            value={currentEditMenu.LinkTargetUrlOrRouteCode ?? ''}
                            onChange={(e) => updateCurrent('LinkTargetUrlOrRouteCode', e.target.value || null)}
                            className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                          >
                            <option value="">-- Select --</option>
                            {routeStateList.map((r: any) => (
                              <option key={r.StateCode} value={r.StateCode}>{r.StateCode}</option>
                            ))}
                          </select>
                        ) : (
                          <select
                            value={currentEditMenu.LinkTargetTransactionId ?? ''}
                            onChange={(e) => updateCurrent('LinkTargetTransactionId', e.target.value ? Number(e.target.value) : null)}
                            className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                          >
                            <option value="">-- Select --</option>
                            {transactionList.map((t: any) => (
                              <option key={t.Id} value={t.Id}>{t.TransactionName}</option>
                            ))}
                          </select>
                        )}
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Form Title Field (Optional)</label>
                        <select
                          value={currentEditMenu.RowDisplayDbField ?? ''}
                          onChange={(e) => updateCurrent('RowDisplayDbField', e.target.value || null)}
                          className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        >
                          <option value="">-- Select --</option>
                          {unitFields.map((f: any) => (
                            <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                          ))}
                        </select>
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Condition Field (Optional)</label>
                        <select
                          value={currentEditMenu.SourceConditionColumn ?? ''}
                          onChange={(e) => updateCurrent('SourceConditionColumn', e.target.value || null)}
                          className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        >
                          <option value="">-- Select --</option>
                          {unitFields.map((f: any) => (
                            <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                          ))}
                        </select>
                      </div>
                      <label className="flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={Boolean(currentEditMenu.IsPopup)}
                          onChange={(e) => updateCurrent('IsPopup', e.target.checked)}
                        />
                        <span className={`text-xs ${theme.label}`}>Open as Popup</span>
                      </label>
                      {!isAnyPage && currentEditMenu.IsPopup && (
                        <>
                          <div className="flex items-center gap-3">
                            <label className={`w-32 text-xs ${theme.label}`}>Popup Width (px)</label>
                            <input
                              type="number"
                              value={currentEditMenu.PopupWidth ?? 800}
                              onChange={(e) => updateCurrent('PopupWidth', parseInt(e.target.value, 10) || 800)}
                              className={`w-24 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                            />
                          </div>
                          <div className="flex items-center gap-3">
                            <label className={`w-32 text-xs ${theme.label}`}>Popup Height (px)</label>
                            <input
                              type="number"
                              value={currentEditMenu.PopupHeight ?? 600}
                              onChange={(e) => updateCurrent('PopupHeight', parseInt(e.target.value, 10) || 600)}
                              className={`w-24 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                            />
                          </div>
                        </>
                      )}
                      {isAnyPage ? (
                        <>
                          <div className={`text-xs font-semibold ${theme.title} pt-2`}>Parameter Mapping:</div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Parameter-Id</label>
                            <select
                              value={currentEditMenu.SourceColumn1 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn1', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Parameter-1</label>
                            <select
                              value={currentEditMenu.SourceColumn2 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn2', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Parameter-2</label>
                            <select
                              value={currentEditMenu.SourceColumn3 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn3', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                        </>
                      ) : (
                        <>
                          <div className={`text-xs font-semibold ${theme.title} pt-2`}>Field Mapping:</div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Source Field 1</label>
                            <select
                              value={currentEditMenu.SourceColumn1 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn1', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Target Field 1</label>
                            <select
                              value={currentEditMenu.TargetColumn1 ?? ''}
                              onChange={(e) => updateCurrent('TargetColumn1', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {targetFieldList.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Source Field 2</label>
                            <select
                              value={currentEditMenu.SourceColumn2 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn2', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Target Field 2</label>
                            <select
                              value={currentEditMenu.TargetColumn2 ?? ''}
                              onChange={(e) => updateCurrent('TargetColumn2', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {targetFieldList.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Source Field 3</label>
                            <select
                              value={currentEditMenu.SourceColumn3 ?? ''}
                              onChange={(e) => updateCurrent('SourceColumn3', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {unitFields.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                          <div className="flex items-center gap-2">
                            <label className={`w-40 text-xs ${theme.label} whitespace-nowrap`}>Target Field 3</label>
                            <select
                              value={currentEditMenu.TargetColumn3 ?? ''}
                              onChange={(e) => updateCurrent('TargetColumn3', e.target.value || null)}
                              className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                            >
                              <option value="">-- Select --</option>
                              {targetFieldList.map((f: any) => (
                                <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>
                              ))}
                            </select>
                          </div>
                        </>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default TransactionUnitLinkTargetEditor;
