import React, { useMemo, useState, useCallback, useRef, useEffect } from "react";
import { useDispatch } from "react-redux";
import { GridViewLayout, type MassUpdateGridApi } from "./searchViewLayout/GridViewLayout";
import {
  AdvancedMassUpdatePopup,
  type AdvancedMassUpdateApplyParams,
} from "./searchViewLayout/AdvancedMassUpdatePopup";
import FlexGridAddOn from "../common/FlexGridAddOn";
import { CardViewLayout } from "./searchViewLayout/CardViewLayout";
import { CalendarViewLayout } from "./searchViewLayout/CalendarViewLayout";
import { GanttViewLayout } from "./searchViewLayout/GanttViewLayout";
import { SchedulerViewLayout } from "./searchViewLayout/SchedulerViewLayout";
import { PivotViewLayout } from "./searchViewLayout/PivotViewLayout";
import { ChartViewLayout } from "./searchViewLayout/ChartViewLayout";
import { ClusterAnalysisViewLayout } from "./searchViewLayout/ClusterAnalysisViewLayout";
import { FlatDataSetTreeViewLayout } from "./searchViewLayout/FlatDataSetTreeViewLayout";
import { GoogleMapViewLayout } from "./searchViewLayout/GoogleMapViewLayout";
import FormListEdit from "../formMgt/FormListEdit";
import { useEnumValues } from "../../hooks/useEnumDictionary";
import { useTheme } from "../../redux/hooks/useTheme";
import { useTabNavigation } from "../../redux/hooks/useTabNavigation";
import { useErrorMessage } from "../../redux/hooks/useErrorMessage";
import { getCurrentActiveTab, preserveTabInitialPath } from "../../redux/features/ui/navigation/tabnavSlice";
import {
  buildFormGroupOpenPayload,
  cacheFormGroupSession,
  hasDataModelTemplateFormGroup,
} from "../../utils/transactionFormGroupHelper";
import { isTransactionFormGroupPath } from "../../helper/navigationHelper";
import { searchSvc } from "../../webapi/searchSvc";
import type { AppDispatch } from "../../redux/store";

function getRowColumnValue(row: any, columnId: number | string): any {
  const dict = row?.DictViewColumnIDKeyValue;
  if (!dict || typeof dict !== 'object') return undefined;
  if (Object.prototype.hasOwnProperty.call(dict, columnId)) return dict[columnId as any];
  const asStr = String(columnId);
  if (Object.prototype.hasOwnProperty.call(dict, asStr)) return dict[asStr];
  const asNum = Number(columnId);
  if (Number.isFinite(asNum) && Object.prototype.hasOwnProperty.call(dict, asNum)) return dict[asNum];
  return undefined;
}

function setRowColumnValue(row: any, columnId: number, value: any): void {
  if (!row.DictViewColumnIDKeyValue || typeof row.DictViewColumnIDKeyValue !== 'object') {
    row.DictViewColumnIDKeyValue = {};
  }
  const dict = row.DictViewColumnIDKeyValue;
  const asStr = String(columnId);
  if (Object.prototype.hasOwnProperty.call(dict, asStr)) {
    dict[asStr] = value;
  } else if (Object.prototype.hasOwnProperty.call(dict, columnId)) {
    dict[columnId] = value;
  } else {
    // Prefer string keys (JSON / Immer round-trip); keep numeric if already used elsewhere.
    dict[asStr] = value;
  }
}

function advancedValuesEqual(a: any, b: any): boolean {
  if (a == null && (b == null || b === '')) return true;
  if (b == null && (a == null || a === '')) return true;
  if (typeof a === 'boolean' || typeof b === 'boolean') {
    const toBool = (v: any) => v === true || v === 'true' || v === 1 || v === '1';
    return toBool(a) === toBool(b);
  }
  if (typeof a === 'number' || typeof b === 'number') {
    const na = Number(a);
    const nb = Number(b);
    if (Number.isFinite(na) && Number.isFinite(nb)) return na === nb;
  }
  return String(a ?? '') === String(b ?? '');
}

/** EmAppCriteriaOperatorType matching AdvancedMassUpdatePopup operators. */
function rowMatchesCondition(
  row: any,
  columnId: number,
  operator: number,
  conditionValue: any,
): boolean {
  const current = getRowColumnValue(row, columnId);
  const isEmpty = current == null || current === '';

  switch (operator) {
    case 1: // Null
      return current == null;
    case 2: // Not Null
      return current != null;
    case 8: // NullOrEmpty — not in popup list but keep safe
      return isEmpty;
    case 9: // NotNullOrEmpty
      return !isEmpty;
    case 0: // Equals
      return advancedValuesEqual(current, conditionValue);
    case 13: // Not Equal
      return !advancedValuesEqual(current, conditionValue);
    case 3: // Greater Than
      return Number(current) > Number(conditionValue);
    case 4: // Greater Than Or Equals
      return Number(current) >= Number(conditionValue);
    case 5: // Less Than
      return Number(current) < Number(conditionValue);
    case 6: // Less Than Or Equals
      return Number(current) <= Number(conditionValue);
    case 7: // Like / Contains
      return String(current ?? '').toLowerCase().includes(String(conditionValue ?? '').toLowerCase());
    case 10: // Start With
      return String(current ?? '').toLowerCase().startsWith(String(conditionValue ?? '').toLowerCase());
    case 11: // End With
      return String(current ?? '').toLowerCase().endsWith(String(conditionValue ?? '').toLowerCase());
    default:
      return true;
  }
}
interface SearchViewProps {
  viewDto: any;
  viewDataList: any[];
  dataModel?: any;
  onExecuteSearch?: () => Promise<void>;
  /** Calendar navigator: merge criteria DCU values then re-run search (Angular calendarHelper). */
  onPatchDictDcuValueAndSearch?: (patch: Record<string, any>) => Promise<void>;
  onSelectionChanged?: (selectedItems: any[]) => void;
}

export const SearchView: React.FC<SearchViewProps> = ({ 
  viewDto, 
  viewDataList, 
  dataModel,
  onExecuteSearch,
  onPatchDictDcuValueAndSearch,
  onSelectionChanged
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch<AppDispatch>();
  const { addTabAndNavigate } = useTabNavigation();
  const { showError, showInfo } = useErrorMessage();
  const emAppViewType = useEnumValues('EmAppViewType');
  const [gridControl, setGridControl] = useState<any>(null);
  const massUpdateApiRef = useRef<MassUpdateGridApi | null>(null);
  const [deletedMassUpdateRows, setDeletedMassUpdateRows] = useState<any[]>([]);
  const [massUpdateBusy, setMassUpdateBusy] = useState(false);
  const [advancedMassUpdateOpen, setAdvancedMassUpdateOpen] = useState(false);

  const isMassUpdate = Boolean(viewDto?.IsMassUpdate);
  // Hierarchical ListEdit mass update uses embedded FormListEdit (not this SingleTable grid path).
  const isHierarchicalMassUpdate = Number(viewDto?.MassUpdateViewType) === 2;
  const showSingleTableMassUpdateToolbar = isMassUpdate && !isHierarchicalMassUpdate;

  const onMassUpdateApiReady = useCallback((api: MassUpdateGridApi | null) => {
    massUpdateApiRef.current = api;
  }, []);

  const massupdated_addRow = useCallback(() => {
    massUpdateApiRef.current?.addRow();
  }, []);

  const massupdated_removeRow = useCallback(() => {
    const removed = massUpdateApiRef.current?.removeSelectedRow();
    if (removed) {
      setDeletedMassUpdateRows((prev) => [...prev, removed]);
    }
  }, []);

  const massupdated_save = useCallback(async () => {
    const api = massUpdateApiRef.current;
    if (!api || !viewDto?.Id) return;
    const changed = api.getChangedItems();
    if ((!changed || changed.length === 0) && deletedMassUpdateRows.length === 0) {
      showInfo('No changes to save.', true);
      return;
    }
    setMassUpdateBusy(true);
    try {
      const result = await searchSvc.saveMassUpdateResult({
        SearchViewId: Number(viewDto.Id),
        ModifiedSearchResult: changed ?? [],
        DeletedSearchResult: deletedMassUpdateRows,
      });
      if (result?.IsSuccessful === false) {
        const msgs = result?.ValidationResult?.Items?.map((i: any) => i.Message).filter(Boolean) ?? [];
        showError(msgs.join('; ') || 'Mass update save failed.');
        return;
      }
      setDeletedMassUpdateRows([]);
      showInfo('Mass update saved.', true);
      await onExecuteSearch?.();
    } catch (err: any) {
      showError(err?.message || 'Mass update save failed.');
    } finally {
      setMassUpdateBusy(false);
    }
  }, [deletedMassUpdateRows, onExecuteSearch, showError, showInfo, viewDto?.Id]);

  const applyAdvancedMassUpdate = useCallback((params: AdvancedMassUpdateApplyParams) => {
    const api = massUpdateApiRef.current;
    if (!api) {
      showError('Mass update grid is not ready.');
      return;
    }

    let rows = api.getItems();
    if (!rows.length) {
      showInfo('No rows to update.', true);
      return;
    }

    if (params.isFilterBySelectedRows) {
      const selected = api.getSelectedItems();
      if (!selected.length) {
        showInfo('No selected rows. Select rows in the grid first, or turn off Filter By Selected Rows.', true);
        return;
      }
      const selectedSet = new Set(selected);
      rows = rows.filter((r) => selectedSet.has(r));
    }

    if (params.conditionColumnId != null && params.conditionOperator != null) {
      rows = rows.filter((r) =>
        rowMatchesCondition(r, params.conditionColumnId!, params.conditionOperator!, params.conditionValue),
      );
    }

    let updated = 0;
    for (const row of rows) {
      if (params.matchFindValue) {
        const current = getRowColumnValue(row, params.updateColumnId);
        if (!advancedValuesEqual(current, params.findValue)) continue;
      }
      setRowColumnValue(row, params.updateColumnId, params.replaceValue);
      api.markRowChanged(row, params.updateColumnId);
      updated += 1;
    }

    api.refresh();
    if (updated === 0) {
      showInfo('No rows matched the criteria.', true);
    } else {
      showInfo(`Updated ${updated} row(s). Click Save to persist.`, true);
    }
  }, [showError, showInfo]);

  useEffect(() => {
    setDeletedMassUpdateRows([]);
    setAdvancedMassUpdateOpen(false);
  }, [viewDto?.Id]);

  const gridViewType = emAppViewType?.GridView ?? 1;
  const cardViewType = emAppViewType?.CardView ?? 2;
  const calendarViewType = emAppViewType?.CalendarView ?? 6;
  const ganttViewType = emAppViewType?.GanttView ?? 15;
  const pivotViewType = emAppViewType?.PivotView ?? 5;
  const chartViewType = emAppViewType?.ChartView ?? 7;
  const schedulerViewType = emAppViewType?.SchedulerView ?? 16;
  const googleMapViewType = emAppViewType?.GoogleMapView ?? 17;
  const clusterAnalysisViewType = emAppViewType?.ClusterAnalysisView ?? 25;
  const flatDataSetTreeViewType = emAppViewType?.FlatDataSetTreeView ?? 8;
  const eshopCardViewType = emAppViewType?.EShopCardView ?? 9;
  const rowCount = Array.isArray(viewDataList) ? viewDataList.length : 0;
  const headerTitle = `${viewDto?.Display || dataModel?.currentSearchName || "View"}: (${rowCount})`;
  const EmAppLinkTargetActionType = { CreateBlankForm: 2 };
  const topMenuData = useMemo(() => {
    const formTargets = Array.isArray(viewDto?.AppFormLinkTargetList) ? viewDto.AppFormLinkTargetList : [];
    const linkedTargets = Array.isArray(viewDto?.AppViewLinkedSeaechOrUrlDtoList) ? viewDto.AppViewLinkedSeaechOrUrlDtoList : [];

    // Angular parity requested by user:
    // only "Nav To Data Model" with ActionType=Create Blank Form is shown on header menu.
    const toDataModel = formTargets.filter((m: any) => m?.ActionType === EmAppLinkTargetActionType.CreateBlankForm);
    const toSearch = linkedTargets.filter((m: any) => m?.LinkTargetSearchId && m?.LayoutDisplayMode === 2);
    const toBuiltIn = linkedTargets.filter((m: any) => m?.LinkTargetUrlOrRouteCode && m?.LayoutDisplayMode === 2);
    return { toDataModel, toSearch, toBuiltIn };
  }, [viewDto?.AppFormLinkTargetList, viewDto?.AppViewLinkedSeaechOrUrlDtoList]);

  const openHeaderNavigationMenu = (menu: any, usageType: number) => {
    if (!menu) return;
    const title = menu?.NavigationActionName || menu?.Name || 'Navigate';
    if (usageType === 1 && menu?.LinkTargetTransactionId) {
      if (hasDataModelTemplateFormGroup(viewDto)) {
        const emptyRow = { DictViewColumnIDKeyValue: {} as Record<string, unknown> };
        const { tabTitle, sessionKey, param2, sessionData } = buildFormGroupOpenPayload(
          menu,
          emptyRow,
          viewDto,
          viewDataList || [],
        );
        cacheFormGroupSession(dispatch, sessionKey, sessionData);
        const activeTab = getCurrentActiveTab();
        if (activeTab?.tabKey && activeTab.path && !isTransactionFormGroupPath(activeTab.path)) {
          dispatch(preserveTabInitialPath({ tabKey: activeTab.tabKey, path: activeTab.path }));
        }
        addTabAndNavigate('TransactionFormGroup', tabTitle, {
          id: sessionKey,
          preserveLinkTabTitle: true,
          param2,
        });
        return;
      }
      addTabAndNavigate('FormMasterDetail', title, { id: menu.LinkTargetTransactionId });
      return;
    }
    if (usageType === 3 && menu?.LinkTargetSearchId) {
      addTabAndNavigate('masterdatamanagement', title, { searchId: menu.LinkTargetSearchId, isSavedSearch: false });
      return;
    }
    if (usageType === 5 && menu?.LinkTargetUrlOrRouteCode) {
      let routeCode = String(menu.LinkTargetUrlOrRouteCode);
      if (routeCode.includes('main.')) routeCode = routeCode.replace('main.', '');
      addTabAndNavigate(routeCode, title, {});
    }
  };

  if (!viewDto) {
    return null;
  }

  // Render different view types based on ViewType.
  // Unmigrated types are routed to GridView for now so every type is usable.
  // Hierarchical ListEdit mass update: Angular SearchView/ListEditViewLayout embeds FormListEdit.
  const renderViewByType = () => {
    if (
      isHierarchicalMassUpdate &&
      viewDto?.MassUpdateTransactionId != null &&
      Number(viewDto.MassUpdateTransactionId) > 0
    ) {
      const listDto = dataModel?.searchResultDto?.MassUpdateAppListDataDto;
      const emptyMassUpdateListDto = {
        TransactionId: Number(viewDto.MassUpdateTransactionId),
        ListData: [] as any[],
        IsMassUpdate: true,
        MassUpdateViewId: viewDto.Id,
      };
      return (
        <div className="w-full h-full overflow-hidden">
          <FormListEdit
            embedded={{
              embeddedTransactionId: Number(viewDto.MassUpdateTransactionId),
              embeddedListEditDataDto: listDto ?? emptyMassUpdateListDto,
              hideHeader: true,
              onMassUpdateSaved: onExecuteSearch,
              embeddedParam2: { isEnableFormConfigButtons: false },
            }}
          />
        </div>
      );
    }

    switch (viewDto.ViewType) {
      case gridViewType:
        return (
          <GridViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onSelectionChanged={onSelectionChanged}
            onGridControlReady={setGridControl}
            onMassUpdateApiReady={onMassUpdateApiReady}
          />
        );
      case cardViewType:
        return (
          <CardViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onSelectionChanged={onSelectionChanged}
          />
        );
      case eshopCardViewType:
        return (
          <CardViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onSelectionChanged={onSelectionChanged}
          />
        );
      case flatDataSetTreeViewType:
        return (
          <FlatDataSetTreeViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onSelectionChanged={onSelectionChanged}
          />
        );
      case calendarViewType:
        return (
          <CalendarViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onPatchDictDcuValueAndSearch={onPatchDictDcuValueAndSearch}
          />
        );
      case ganttViewType:
        return (
          <GanttViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
          />
        );
      case pivotViewType:
        return (
          <PivotViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
          />
        );
      case chartViewType:
        return (
          <ChartViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onSelectionChanged={onSelectionChanged}
          />
        );
      case schedulerViewType:
        return (
          <SchedulerViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onPatchDictDcuValueAndSearch={onPatchDictDcuValueAndSearch}
          />
        );
      case googleMapViewType:
        return (
          <GoogleMapViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onSelectionChanged={onSelectionChanged}
          />
        );
      case clusterAnalysisViewType:
        return (
          <ClusterAnalysisViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onSelectionChanged={onSelectionChanged}
          />
        );
      default:
        return (
          <GridViewLayout
            viewDto={viewDto}
            viewDataList={viewDataList}
            dataModel={dataModel}
            onExecuteSearch={onExecuteSearch}
            onSelectionChanged={onSelectionChanged}
            onGridControlReady={setGridControl}
            onMassUpdateApiReady={onMassUpdateApiReady}
          />
        );
    }
  };

  return (
    <div className={`w-full h-full flex flex-col overflow-hidden ${theme.mainContentSection}`}>
      <div className={`px-3 py-1 text-xs border-b flex items-center justify-between gap-2 ${theme.label}`}>
        <div className="flex items-center gap-1 min-w-0">
          <span className="truncate">{headerTitle}</span>
          {gridControl && (
            <FlexGridAddOn
              grid={gridControl}
              buttonClassName={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
              title="Freeze / Show / Hide columns"
            />
          )}
        </div>
        <div className="flex items-center gap-1 shrink-0">
          {showSingleTableMassUpdateToolbar && (
            <>
              <button
                type="button"
                className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                onClick={() => void onExecuteSearch?.()}
                disabled={massUpdateBusy}
                title="Refresh"
              >
                <i className="fa-solid fa-rotate mr-1" aria-hidden="true" />
                Refresh
              </button>
              <button
                type="button"
                className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                onClick={() => void massupdated_save()}
                disabled={massUpdateBusy}
                title="Save"
              >
                <i className="fa-solid fa-floppy-disk mr-1" aria-hidden="true" />
                Save
              </button>
              {viewDto?.IsAllowAddRow && (
                <button
                  type="button"
                  className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                  onClick={massupdated_addRow}
                  disabled={massUpdateBusy}
                  title="Add Row"
                >
                  <i className="fa-solid fa-plus mr-1" aria-hidden="true" />
                  Add Row
                </button>
              )}
              {viewDto?.IsAllowDeleteRow && (
                <button
                  type="button"
                  className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                  onClick={massupdated_removeRow}
                  disabled={massUpdateBusy}
                  title="Delete Row"
                >
                  <i className="fa-solid fa-xmark mr-1" aria-hidden="true" />
                  Delete Row
                </button>
              )}
              {viewDto?.IsAllowAdvancedUpdate && (
                <button
                  type="button"
                  className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
                  onClick={() => setAdvancedMassUpdateOpen(true)}
                  disabled={massUpdateBusy}
                  title="Advanced Update"
                >
                  <i className="fa-solid fa-filter mr-1" aria-hidden="true" />
                  Advanced Update
                </button>
              )}
            </>
          )}
          {topMenuData.toDataModel.map((m: any, idx: number) => (
            <button
              key={`to-form-${m?.Id ?? idx}`}
              type="button"
              className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
              onClick={() => openHeaderNavigationMenu(m, 1)}
              title="To Data Model"
            >
              <i className="fa fa-plus mr-1" aria-hidden="true" />
              {m?.NavigationActionName || 'To Data Model'}
            </button>
          ))}
          {topMenuData.toSearch.map((m: any, idx: number) => (
            <button
              key={`to-search-${m?.Id ?? idx}`}
              type="button"
              className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
              onClick={() => openHeaderNavigationMenu(m, 3)}
              title="To Search"
            >
              {m?.NavigationActionName || 'To Search'}
            </button>
          ))}
          {topMenuData.toBuiltIn.map((m: any, idx: number) => (
            <button
              key={`to-builtin-${m?.Id ?? idx}`}
              type="button"
              className={`px-2 py-0.5 text-xs rounded ${theme.button_default}`}
              onClick={() => openHeaderNavigationMenu(m, 5)}
              title="To Built-In Page"
            >
              {m?.NavigationActionName || 'To Built-In Page'}
            </button>
          ))}
        </div>
      </div>
      <div className="w-full h-1 flex-auto overflow-hidden">
        {renderViewByType()}
      </div>
      {showSingleTableMassUpdateToolbar && viewDto?.IsAllowAdvancedUpdate && (
        <AdvancedMassUpdatePopup
          isOpen={advancedMassUpdateOpen}
          viewId={viewDto?.Id}
          columns={Array.isArray(viewDto?.Columns) ? viewDto.Columns : []}
          dictEntityLookupItemDto={viewDto?.DictEntityLookupItemDto}
          onClose={() => setAdvancedMassUpdateOpen(false)}
          onApply={applyAdvancedMassUpdate}
        />
      )}
    </div>
  );
}; 