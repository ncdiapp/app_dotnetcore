import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';

const LINK_TARGET_ACTIONS = [
  { Id: 2, Display: 'Create Blank Form' },
  { Id: 13, Display: 'Create New Form Using Current Data' },
  { Id: 1, Display: 'Edit Form' },
  { Id: 5, Display: 'Preview Form' },
  { Id: 3, Display: 'Delete Form' },
  { Id: 17, Display: 'Execute Data Model Command' }
];

const MENU_LOCATION_LIST = [
  { Id: 1, Display: 'On Result Row' },
  { Id: 2, Display: 'On Top Menu' }
];

/** Align with APP.Components.Dto AppEnums.EmAppLinkTargetActionType */
const EmAppLinkTargetActionType = {
  Edit: 1,
  Create: 2,
  Delete: 3,
  Preview: 5,
  CreateFromExistingItem: 13,
  CopyEvent: 14,
  CutEvent: 15,
  PasteEvent: 16,
  ExecuteTransactionCommand: 17
} as const;

/** Align with EmAppViewType (calendar / Gantt / scheduler) */
const EmAppViewType = {
  CalendarView: 6,
  GanttView: 15,
  SchedulerView: 16
} as const;

/**
 * Navigate-to-Data-Model (SearchViewLinkToForm): visibility mirrors
 * PlmApplication Scripts1x Helper linkTargetConfigHelper.js (isShowLinkTarget_*).
 */
function getFormNavigateToDataModelVisibility(opts: {
  actionType: number | null | undefined;
  viewType: number | null | undefined;
  linkTargetTransactionId: number | null | undefined;
  dataTransferSettingId: number | null | undefined;
  isApiIntegrationTransaction: boolean;
}) {
  const a = Number(opts.actionType);
  const vt = Number(opts.viewType);
  const isDayPilot =
    vt === EmAppViewType.CalendarView ||
    vt === EmAppViewType.GanttView ||
    vt === EmAppViewType.SchedulerView;
  const hasTxn = opts.linkTargetTransactionId != null;
  const noDataTransfer = !opts.dataTransferSettingId;

  const mapGroup =
    a === EmAppLinkTargetActionType.CreateFromExistingItem ||
    a === EmAppLinkTargetActionType.Edit ||
    a === EmAppLinkTargetActionType.Delete ||
    a === EmAppLinkTargetActionType.Preview ||
    a === EmAppLinkTargetActionType.CopyEvent ||
    a === EmAppLinkTargetActionType.CutEvent ||
    a === EmAppLinkTargetActionType.PasteEvent ||
    a === EmAppLinkTargetActionType.ExecuteTransactionCommand;

  const mapGroupWithApi = mapGroup && opts.isApiIntegrationTransaction;

  const calendarCreatePaste =
    isDayPilot &&
    (a === EmAppLinkTargetActionType.Create || a === EmAppLinkTargetActionType.PasteEvent);

  const showSource1 = mapGroup || calendarCreatePaste;

  const showSource2 =
    (a === EmAppLinkTargetActionType.CreateFromExistingItem && noDataTransfer) ||
    mapGroupWithApi ||
    calendarCreatePaste;

  const showSource3 =
    (a === EmAppLinkTargetActionType.CreateFromExistingItem && noDataTransfer) ||
    a === EmAppLinkTargetActionType.PasteEvent ||
    mapGroupWithApi ||
    calendarCreatePaste;

  const showTarget1 =
    a === EmAppLinkTargetActionType.CreateFromExistingItem ||
    a === EmAppLinkTargetActionType.CopyEvent ||
    a === EmAppLinkTargetActionType.CutEvent ||
    a === EmAppLinkTargetActionType.PasteEvent ||
    a === EmAppLinkTargetActionType.Edit ||
    a === EmAppLinkTargetActionType.Delete ||
    a === EmAppLinkTargetActionType.Preview ||
    a === EmAppLinkTargetActionType.ExecuteTransactionCommand ||
    calendarCreatePaste;

  const showTarget2 =
    (a === EmAppLinkTargetActionType.CreateFromExistingItem && noDataTransfer) ||
    mapGroupWithApi ||
    calendarCreatePaste;

  const showTarget3 =
    (a === EmAppLinkTargetActionType.CreateFromExistingItem && noDataTransfer) ||
    mapGroupWithApi ||
    calendarCreatePaste;

  const showFormTitleField =
    a !== EmAppLinkTargetActionType.Delete &&
    a !== EmAppLinkTargetActionType.CopyEvent &&
    a !== EmAppLinkTargetActionType.CutEvent &&
    a !== EmAppLinkTargetActionType.ExecuteTransactionCommand;

  const showOpenAsPopup =
    a !== EmAppLinkTargetActionType.Delete &&
    a !== EmAppLinkTargetActionType.CopyEvent &&
    a !== EmAppLinkTargetActionType.CutEvent &&
    a !== EmAppLinkTargetActionType.ExecuteTransactionCommand;

  const showDataTransfer =
    (a === EmAppLinkTargetActionType.CreateFromExistingItem || a === EmAppLinkTargetActionType.PasteEvent) && !!hasTxn;

  const showTargetTransaction =
    a === EmAppLinkTargetActionType.Create ||
    a === EmAppLinkTargetActionType.CreateFromExistingItem ||
    a === EmAppLinkTargetActionType.Edit ||
    a === EmAppLinkTargetActionType.Delete ||
    a === EmAppLinkTargetActionType.Preview ||
    a === EmAppLinkTargetActionType.PasteEvent;

  return {
    showSource1,
    showSource2,
    showSource3,
    showTarget1,
    showTarget2,
    showTarget3,
    showFormTitleField,
    showOpenAsPopup,
    showDataTransfer,
    showFieldMappingHeader: showSource1,
    showTargetTransaction
  };
}

type ParamObj = { id?: string; param1?: string; param2?: string };

const SearchViewNavigationMenuEditor: React.FC = () => {
  const { param } = useParams<{ param: string }>();
  const { theme } = useTheme();
  const { showError, showInfo, showValidationMessages } = useErrorMessage();
  const dispatch = useDispatch();

  const [paramObj, setParamObj] = useState<ParamObj>({});
  const [loading, setLoading] = useState(false);
  const [searchView, setSearchView] = useState<any>(null);
  const [linkTargetList, setLinkTargetList] = useState<any[]>([]);
  const [deletedIds, setDeletedIds] = useState<number[]>([]);
  const [currentEditMenu, setCurrentEditMenu] = useState<any | null>(null);
  const [transactionList, setTransactionList] = useState<any[]>([]);
  const [searchList, setSearchList] = useState<any[]>([]);
  const [routeStateList, setRouteStateList] = useState<any[]>([]);
  const [targetFieldList, setTargetFieldList] = useState<any[]>([]);
  const [hierarchyTransactionData, setHierarchyTransactionData] = useState<any>(null);
  const [, setEditorRefreshKey] = useState(0);
  const gridRef = useRef<any>(null);

  const linkTargetUsageType = Number(paramObj.param1 || 1);
  const isToForm = linkTargetUsageType === 1;
  const isToSearch = linkTargetUsageType === 3;
  const isToBuiltIn = linkTargetUsageType === 5;

  const titleSuffix = isToForm
    ? 'Menus Navigate To Data Model'
    : isToSearch
      ? 'Menus Navigate To Search'
      : 'Menus Navigate To Built-In Page';

  const searchViewFieldList = useMemo(() => {
    const list = Array.isArray(searchView?.AppSearchViewFieldList) ? searchView.AppSearchViewFieldList : [];
    return list.map((f: any) => ({
      ...f,
      Display: `${f?.SysTableFiledPath ?? ''} (${f?.DisplayText ?? ''})`
    }));
  }, [searchView?.AppSearchViewFieldList]);

  const [linkTargetCV] = useState<CollectionView<any>>(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    return cv;
  });

  useEffect(() => {
    linkTargetCV.sourceCollection = linkTargetList ?? [];
    linkTargetCV.refresh();
  }, [linkTargetList, linkTargetCV]);

  const transactionDataMap = useMemo(() => (
    transactionList.length ? new DataMap(transactionList, 'Id', 'TransactionName') : null
  ), [transactionList]);

  const searchDataMap = useMemo(() => (
    searchList.length ? new DataMap(searchList, 'Id', 'Display') : null
  ), [searchList]);

  const routeStateDataMap = useMemo(() => (
    routeStateList.length ? new DataMap(routeStateList, 'StateCode', 'StateCode') : null
  ), [routeStateList]);

  const menuNameField = isToForm ? 'NavigationActionName' : 'DisplayText';

  const isApiIntegrationTransaction = useMemo(() => {
    const list = hierarchyTransactionData?.ApiInputParameterList;
    return Array.isArray(list) && list.length > 0;
  }, [hierarchyTransactionData]);

  const formLinkVis = useMemo(() => {
    if (!isToForm || !currentEditMenu) return null;
    return getFormNavigateToDataModelVisibility({
      actionType: currentEditMenu.ActionType,
      viewType: searchView?.ViewType,
      linkTargetTransactionId: currentEditMenu.LinkTargetTransactionId,
      dataTransferSettingId: currentEditMenu.DataTransferSettingId,
      isApiIntegrationTransaction
    });
  }, [
    isToForm,
    currentEditMenu?.ActionType,
    currentEditMenu?.LinkTargetTransactionId,
    currentEditMenu?.DataTransferSettingId,
    searchView?.ViewType,
    isApiIntegrationTransaction
  ]);

  const parseParam = useCallback(() => {
    if (!param) {
      setParamObj({});
      return;
    }
    try {
      setParamObj(JSON.parse(decodeURIComponent(param)));
    } catch {
      setParamObj({ id: param });
    }
  }, [param]);

  useEffect(() => {
    parseParam();
  }, [parseParam]);

  const loadData = useCallback(async () => {
    const searchViewId = paramObj.id ? String(paramObj.id) : '';
    if (!searchViewId) return;
    setLoading(true);
    dispatch(setIsBusy());
    try {
      const view = await searchSvc.retrieveOneAppSearchViewExDto(searchViewId);
      setSearchView(view || null);
      setDeletedIds([]);

      if (isToForm) {
        const [allTx, linkTargetsRaw] = await Promise.all([
          appTransactionService.retrieveAllAppTransactions(null, 1, true),
          searchSvc.retrieveOneSearchViewLinkTargetList(searchViewId, 1)
        ]);
        setTransactionList(Array.isArray(allTx) ? allTx : []);
        const list = Array.isArray(linkTargetsRaw) ? linkTargetsRaw : [];
        setLinkTargetList(list);
        setCurrentEditMenu(list[0] ?? null);
      } else {
        const [linkedRaw, searchLookup, routeStateLookup] = await Promise.all([
          searchSvc.retrieveOneAppViewLinkedSeaechOrUrlExDto(searchViewId),
          isToSearch ? adminSvc.getMassEntitiesLookupItem('AppSearch') : Promise.resolve(null),
          isToBuiltIn ? adminSvc.retrieveAllAppRouteStateEntityDto() : Promise.resolve(null)
        ]);

        const linkedList = Array.isArray(linkedRaw) ? linkedRaw : [];
        const filtered = linkedList.filter((m: any) => (
          isToSearch ? !!m?.LinkTargetSearchId : !!m?.LinkTargetUrlOrRouteCode
        ));
        setLinkTargetList(filtered);
        setCurrentEditMenu(filtered[0] ?? null);

        if (isToSearch) {
          const list = Array.isArray(searchLookup) ? searchLookup : (searchLookup?.AppSearch ?? []);
          setSearchList(Array.isArray(list) ? list : []);
        } else {
          setSearchList([]);
        }
        if (isToBuiltIn) {
          setRouteStateList(Array.isArray(routeStateLookup) ? routeStateLookup : []);
        } else {
          setRouteStateList([]);
        }
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load navigation menus');
      setCurrentEditMenu(null);
      setLinkTargetList([]);
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [paramObj.id, dispatch, showError, isToBuiltIn, isToForm, isToSearch]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    if (!isToForm) {
      setTargetFieldList([]);
      setHierarchyTransactionData(null);
      return;
    }
    const tid = currentEditMenu?.LinkTargetTransactionId;
    if (!tid) {
      setTargetFieldList([]);
      setHierarchyTransactionData(null);
      return;
    }
    appTransactionService
      .getOneHierarchyTransaction(String(tid), false, '', '', '', false, '')
      .then((h: any) => {
        setHierarchyTransactionData(h || null);
        const root = h?.AppTransactionUnitList?.[0];
        const fields = root?.AppTransactionFieldList ?? [];
        setTargetFieldList(Array.isArray(fields) ? fields : []);
      })
      .catch(() => {
        setHierarchyTransactionData(null);
        setTargetFieldList([]);
      });
  }, [currentEditMenu?.LinkTargetTransactionId, isToForm]);

  const updateCurrent = useCallback((key: string, value: any) => {
    if (!currentEditMenu) return;
    currentEditMenu[key] = value;
    linkTargetCV.refresh();
    setEditorRefreshKey((v) => v + 1);
  }, [currentEditMenu, linkTargetCV]);

  const handleSelectionChanged = useCallback(() => {
    const grid = gridRef.current?.control ?? gridRef.current;
    const rowIndex = grid?.selection?.row ?? -1;
    if (rowIndex < 0 || !grid?.rows?.length) {
      setCurrentEditMenu(null);
      return;
    }
    setCurrentEditMenu(grid.rows[rowIndex]?.dataItem ?? null);
  }, []);

  const handleAdd = useCallback(() => {
    if (!searchView?.Id) return;
    const maxSort = Math.max(0, ...linkTargetList.map((x: any) => Number(x?.Sort || 0)));
    const newItem: any = {
      SearchViewId: searchView.Id,
      LinkTargetUsageType: linkTargetUsageType,
      ActionType: isToForm ? 1 : null,
      NavigationActionName: 'New Menu',
      DisplayText: 'New Menu',
      Sort: maxSort + 1,
      LinkTargetTransactionId: null,
      LinkTargetSearchId: null,
      LinkTargetUrlOrRouteCode: null,
      LayoutDisplayMode: 1,
      RowDisplayViewColumnId: null,
      SourceConditionViewColumnId: null,
      SourceViewColumnId1: null,
      SourceViewColumnId2: null,
      SourceViewColumnId3: null,
      TargetColumn1: '',
      TargetColumn2: '',
      TargetColumn3: '',
      TargetSearchFieldId1: null,
      TargetSearchFieldId2: null,
      TargetSearchFieldId3: null,
      IsPopup: isToSearch,
      PopupWidth: 1200,
      PopupHeight: 700,
      IconName: ''
    };
    const next = [...linkTargetList, newItem];
    setLinkTargetList(next);
    setCurrentEditMenu(newItem);
    setTimeout(() => {
      const grid = gridRef.current?.control ?? gridRef.current;
      if (grid?.rows?.length) grid.select(grid.rows.length - 1, 0);
    }, 0);
  }, [searchView?.Id, linkTargetList, linkTargetUsageType, isToForm, isToSearch]);

  const handleDelete = useCallback(() => {
    if (!currentEditMenu) {
      showInfo('Select a menu to delete');
      return;
    }
    if (currentEditMenu.Id) setDeletedIds((prev) => [...prev, currentEditMenu.Id]);
    setLinkTargetList((prev) => prev.filter((x) => x !== currentEditMenu));
    setCurrentEditMenu(null);
  }, [currentEditMenu, showInfo]);

  const handleSave = useCallback(async () => {
    if (!searchView?.Id) return;
    dispatch(setIsBusy());
    try {
      const payload = isToForm
        ? {
            SearchViewId: searchView.Id,
            DeletedItemIds: deletedIds,
            AppFormLinkTargetDtoSet: linkTargetList
          }
        : {
            SearchViewId: searchView.Id,
            DeletedItemIds: deletedIds,
            AppViewLinkedSeaechOrUrlDtoSet: linkTargetList
          };

      const result = isToForm
        ? await searchSvc.saveOneSearchViewLinkTargetList(payload)
        : await searchSvc.saveAllAppViewLinkedSeaechOrUrlEntityDto(payload);

      if (result?.ValidationResult) showValidationMessages(result.ValidationResult, true);
      if (result?.IsSuccessful) {
        showInfo('Saved');
        await loadData();
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [searchView?.Id, dispatch, deletedIds, linkTargetList, isToForm, showInfo, showError, showValidationMessages, loadData]);

  return (
    <div className="w-full h-full flex flex-col overflow-hidden rounded-t-md rounded-b-md">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-sm font-semibold ${theme.title}`}>
          Search View: <span className="ml-1">{searchView?.Name || ''}</span>
          <span className="ml-2">- {titleSuffix}</span>
        </div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={loadData} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="w-full h-full flex overflow-hidden">
          <div className="basis-[52%] h-full flex flex-col border-r overflow-hidden">
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-xs font-semibold ${theme.title}`}>
                <i className="fa-solid fa-file-lines mr-1 opacity-60" aria-hidden /> Menus
              </div>
              <div className="flex items-center gap-1">
                <button type="button" onClick={handleAdd} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                  <i className="fa-solid fa-plus mr-1" aria-hidden /> Add
                </button>
                <button type="button" onClick={handleDelete} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                  <i className="fa-solid fa-trash mr-1" aria-hidden /> Delete
                </button>
              </div>
            </div>
            <div className="h-1 flex-auto overflow-hidden">
              <FlexGrid
                ref={gridRef}
                itemsSource={linkTargetCV}
                isReadOnly={true}
                selectionMode="Row"
                selectionChanged={handleSelectionChanged}
                style={{ width: '100%', height: '100%', border: 'none' }}
              >
                <FlexGridFilter />
                <FlexGridColumn binding="Sort" header="Sort" width={70} />
                <FlexGridColumn binding={menuNameField} header="Menu Name" width={190} />
                {isToForm && (
                  <FlexGridColumn binding="LinkTargetTransactionId" header="Navigate To Data Model" width={240} dataMap={transactionDataMap ?? undefined} />
                )}
                {isToSearch && (
                  <FlexGridColumn binding="LinkTargetSearchId" header="Navigate To Search" width={240} dataMap={searchDataMap ?? undefined} />
                )}
                {isToBuiltIn && (
                  <FlexGridColumn binding="LinkTargetUrlOrRouteCode" header="System Page" width={240} dataMap={routeStateDataMap ?? undefined} />
                )}
                {isToForm && (
                  <FlexGridColumn binding="ActionType" header="Action" width={220} dataMap={new DataMap(LINK_TARGET_ACTIONS, 'Id', 'Display')} />
                )}
                <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly />
              </FlexGrid>
            </div>
          </div>

          <div className="basis-[48%] h-full flex flex-col overflow-hidden">
            <div className={`px-4 py-2 border-b ${theme.mainContentSection}`}>
              <div className={`text-xs font-semibold ${theme.title}`}>
                <i className="fa-solid fa-gear mr-1 opacity-60" aria-hidden /> Menu Properties
              </div>
            </div>
            <div className="h-1 flex-auto overflow-auto p-4">
              {!currentEditMenu ? (
                <div className={`text-sm ${theme.label}`}>{loading ? 'Loading...' : 'Select a menu from the left to edit.'}</div>
              ) : (
                <div className="space-y-3">
                  <div className="flex items-center gap-2">
                    <label className={`w-44 text-xs ${theme.label}`}>Sort Order</label>
                    <input
                      type="number"
                      min={1}
                      value={currentEditMenu.Sort ?? 1}
                      onChange={(e) => updateCurrent('Sort', parseInt(e.target.value, 10) || 1)}
                      className={`w-24 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                    />
                  </div>
                  <div className="flex items-center gap-2">
                    <label className={`w-44 text-xs ${theme.label}`}>Icon</label>
                    <input
                      type="text"
                      value={currentEditMenu.IconName ?? ''}
                      onChange={(e) => updateCurrent('IconName', e.target.value)}
                      className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      placeholder="fa icon class/code"
                    />
                  </div>
                  <div className="flex items-center gap-2">
                    <label className={`w-44 text-xs ${theme.label}`}>Menu Name</label>
                    <input
                      type="text"
                      value={currentEditMenu[menuNameField] ?? ''}
                      onChange={(e) => updateCurrent(menuNameField, e.target.value)}
                      className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                    />
                  </div>

                  {isToForm && (
                    <>
                      <div className="flex items-center gap-2">
                        <label className={`w-44 text-xs ${theme.label}`}>Action Type</label>
                        <select
                          value={currentEditMenu.ActionType ?? ''}
                          onChange={(e) => updateCurrent('ActionType', e.target.value ? Number(e.target.value) : null)}
                          className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        >
                          {LINK_TARGET_ACTIONS.map((a) => <option key={a.Id} value={a.Id}>{a.Display}</option>)}
                        </select>
                      </div>
                      {formLinkVis?.showTargetTransaction && (
                        <div className="flex items-center gap-2">
                          <label className={`w-44 text-xs ${theme.label}`}>Data Model</label>
                          <select
                            value={currentEditMenu.LinkTargetTransactionId ?? ''}
                            onChange={(e) => updateCurrent('LinkTargetTransactionId', e.target.value ? Number(e.target.value) : null)}
                            className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                          >
                            <option value="">-- Select --</option>
                            {transactionList.map((t: any) => <option key={t.Id} value={t.Id}>{t.TransactionName}</option>)}
                          </select>
                        </div>
                      )}
                    </>
                  )}

                  {isToSearch && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Navigate To Search</label>
                      <select
                        value={currentEditMenu.LinkTargetSearchId ?? ''}
                        onChange={(e) => updateCurrent('LinkTargetSearchId', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {searchList.map((s: any) => <option key={s.Id} value={s.Id}>{s.Display ?? s.Name ?? s.Id}</option>)}
                      </select>
                    </div>
                  )}

                  {isToBuiltIn && (
                    <>
                      <div className="flex items-center gap-2">
                        <label className={`w-44 text-xs ${theme.label}`}>Menu Location</label>
                        <select
                          value={currentEditMenu.LayoutDisplayMode ?? 1}
                          onChange={(e) => updateCurrent('LayoutDisplayMode', e.target.value ? Number(e.target.value) : 1)}
                          className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        >
                          {MENU_LOCATION_LIST.map((m) => <option key={m.Id} value={m.Id}>{m.Display}</option>)}
                        </select>
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`w-44 text-xs ${theme.label}`}>Navigate To Page</label>
                        <select
                          value={currentEditMenu.LinkTargetUrlOrRouteCode ?? ''}
                          onChange={(e) => updateCurrent('LinkTargetUrlOrRouteCode', e.target.value || null)}
                          className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        >
                          <option value="">-- Select --</option>
                          {routeStateList.map((r: any) => <option key={r.StateCode} value={r.StateCode}>{r.StateCode}</option>)}
                        </select>
                      </div>
                    </>
                  )}

                  {(isToSearch || isToBuiltIn || (isToForm && !!formLinkVis?.showFormTitleField)) && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Title Label Field (Optional)</label>
                      <select
                        value={currentEditMenu.RowDisplayViewColumnId ?? ''}
                        onChange={(e) => updateCurrent('RowDisplayViewColumnId', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  <div className="flex items-center gap-2">
                    <label className={`w-44 text-xs ${theme.label}`}>Condition Field (Optional)</label>
                    <select
                      value={currentEditMenu.SourceConditionViewColumnId ?? ''}
                      onChange={(e) => updateCurrent('SourceConditionViewColumnId', e.target.value ? Number(e.target.value) : null)}
                      className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                    >
                      <option value="">-- Select --</option>
                      {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                    </select>
                  </div>

                  {(isToSearch || isToBuiltIn || (isToForm && !!formLinkVis?.showOpenAsPopup)) && (
                    <label className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={!!currentEditMenu.IsPopup}
                        onChange={(e) => updateCurrent('IsPopup', e.target.checked)}
                      />
                      <span className={`text-xs ${theme.label}`}>Open as Popup</span>
                    </label>
                  )}
                  {(isToSearch || isToBuiltIn || (isToForm && !!formLinkVis?.showOpenAsPopup)) && !!currentEditMenu.IsPopup && (
                    <>
                      <div className="flex items-center gap-2">
                        <label className={`w-44 text-xs ${theme.label}`}>Popup Width (px)</label>
                        <input
                          type="number"
                          value={currentEditMenu.PopupWidth ?? 1200}
                          onChange={(e) => updateCurrent('PopupWidth', parseInt(e.target.value, 10) || 1200)}
                          className={`w-24 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        />
                      </div>
                      <div className="flex items-center gap-2">
                        <label className={`w-44 text-xs ${theme.label}`}>Popup Height (px)</label>
                        <input
                          type="number"
                          value={currentEditMenu.PopupHeight ?? 700}
                          onChange={(e) => updateCurrent('PopupHeight', parseInt(e.target.value, 10) || 700)}
                          className={`w-24 h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        />
                      </div>
                    </>
                  )}

                  {(isToSearch || (isToForm && !!formLinkVis?.showFieldMappingHeader)) && (
                    <div className={`text-xs font-semibold ${theme.title} pt-2`}>
                      Field Mapping:
                    </div>
                  )}
                  {isToBuiltIn && (
                    <div className={`text-xs font-semibold ${theme.title} pt-2`}>
                      Built-in Page Parameter Mapping:
                    </div>
                  )}

                  {(!isToForm || !!formLinkVis?.showSource1) && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>{isToSearch ? 'View Field 1' : isToBuiltIn ? 'Parameter-Id Mapping' : 'Source View Field 1'}</label>
                      <select
                        value={currentEditMenu.SourceViewColumnId1 ?? ''}
                        onChange={(e) => updateCurrent('SourceViewColumnId1', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        disabled={isToBuiltIn && !!currentEditMenu.TargetSearchFieldId1}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToForm && !!formLinkVis?.showTarget1 && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Target Transaction Field 1</label>
                      <select
                        value={currentEditMenu.TargetColumn1 ?? ''}
                        onChange={(e) => updateCurrent('TargetColumn1', e.target.value || '')}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {targetFieldList.map((f: any) => <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>)}
                      </select>
                    </div>
                  )}
                  {isToSearch && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Search Field 1</label>
                      <select
                        value={currentEditMenu.TargetSearchFieldId1 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId1', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToBuiltIn && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Parameter-Id Fixed Value</label>
                      <input
                        type="text"
                        value={currentEditMenu.TargetSearchFieldId1 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId1', e.target.value || null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      />
                    </div>
                  )}

                  {(!isToForm || !!formLinkVis?.showSource2) && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>{isToSearch ? 'View Field 2' : isToBuiltIn ? 'Parameter-1 Mapping' : 'Source View Field 2'}</label>
                      <select
                        value={currentEditMenu.SourceViewColumnId2 ?? ''}
                        onChange={(e) => updateCurrent('SourceViewColumnId2', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        disabled={isToBuiltIn && !!currentEditMenu.TargetSearchFieldId2}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToForm && !!formLinkVis?.showTarget2 && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Target Transaction Field 2</label>
                      <select
                        value={currentEditMenu.TargetColumn2 ?? ''}
                        onChange={(e) => updateCurrent('TargetColumn2', e.target.value || '')}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {targetFieldList.map((f: any) => <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>)}
                      </select>
                    </div>
                  )}
                  {isToSearch && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Search Field 2</label>
                      <select
                        value={currentEditMenu.TargetSearchFieldId2 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId2', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToBuiltIn && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Parameter-1 Fixed Value</label>
                      <input
                        type="text"
                        value={currentEditMenu.TargetSearchFieldId2 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId2', e.target.value || null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      />
                    </div>
                  )}

                  {(!isToForm || !!formLinkVis?.showSource3) && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>{isToSearch ? 'View Field 3' : isToBuiltIn ? 'Parameter-2 Mapping' : 'Source View Field 3'}</label>
                      <select
                        value={currentEditMenu.SourceViewColumnId3 ?? ''}
                        onChange={(e) => updateCurrent('SourceViewColumnId3', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                        disabled={isToBuiltIn && !!currentEditMenu.TargetSearchFieldId3}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToForm && !!formLinkVis?.showTarget3 && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Target Transaction Field 3</label>
                      <select
                        value={currentEditMenu.TargetColumn3 ?? ''}
                        onChange={(e) => updateCurrent('TargetColumn3', e.target.value || '')}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {targetFieldList.map((f: any) => <option key={f.DataBaseFieldName} value={f.DataBaseFieldName}>{f.DataBaseFieldName}</option>)}
                      </select>
                    </div>
                  )}
                  {isToSearch && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Search Field 3</label>
                      <select
                        value={currentEditMenu.TargetSearchFieldId3 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId3', e.target.value ? Number(e.target.value) : null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      >
                        <option value="">-- Select --</option>
                        {searchViewFieldList.map((f: any) => <option key={f.Id} value={f.Id}>{f.Display}</option>)}
                      </select>
                    </div>
                  )}
                  {isToBuiltIn && (
                    <div className="flex items-center gap-2">
                      <label className={`w-44 text-xs ${theme.label}`}>Parameter-2 Fixed Value</label>
                      <input
                        type="text"
                        value={currentEditMenu.TargetSearchFieldId3 ?? ''}
                        onChange={(e) => updateCurrent('TargetSearchFieldId3', e.target.value || null)}
                        className={`w-1 flex-auto max-w-xs h-7 px-2 text-xs border rounded ${theme.inputBox}`}
                      />
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SearchViewNavigationMenuEditor;
