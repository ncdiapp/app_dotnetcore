import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import PrivilegeSecurityObjSelectorPopup from './PrivilegeSecurityObjSelectorPopup';
import SystemObjectSecurityEditorPopup, { SystemObjectSecurityEditorParam2 } from './SystemObjectSecurityEditorPopup';

const MASS_ENTITY_CODES = 'AppTransaction|AppSearch|AppSearchView|AppDesktop|AppReport';

/** EmAppSecuritySysObjType (Angular) */
const EmAppSecuritySysObjType = {
  Transaction: 1,
  TransactionField: 2,
  TransactionUnit: 3,
  Search: 4,
  SearchView: 5,
  Report: 6,
  Dashboard: 8,
  TransactionAction: 9,
  TransactionUnitAction: 10,
  Menu: 11,
  TransactionCommand: 12,
} as const;

/** First-layer tabs: User types — Company User + Customer, Supplier, Client Agent, Supplier Agent */
const USER_TYPE_TABS = [
  { label: 'Company User', partnerType: null as { Id: number; Display: string } | null },
  { label: 'Customer', partnerType: { Id: 3, Display: 'Customer' } },
  { label: 'Supplier', partnerType: { Id: 4, Display: 'Supplier' } },
  { label: 'Client Agent', partnerType: { Id: 5, Display: 'Client Agent' } },
  { label: 'Supplier Agent', partnerType: { Id: 9, Display: 'Supplier Agent' } },
];

/** Tabs: Data Model, Search, Dashboard, Report — same order as Angular uib-tabset; icons for second layer */
const PRIVILEGE_TABS = [
  { index: 0, heading: 'Data Model', objType: EmAppSecuritySysObjType.Transaction, icon: 'fa-table-cells' },
  { index: 1, heading: 'Search', objType: EmAppSecuritySysObjType.Search, icon: 'fa-search' },
  { index: 2, heading: 'Dashboard', objType: EmAppSecuritySysObjType.Dashboard, icon: 'fa-chart-pie' },
  { index: 3, heading: 'Report', objType: EmAppSecuritySysObjType.Report, icon: 'fa-file-lines' },
] as const;

/** Transaction detail sub-tabs (Angular order: Unit Access, Field Access, Command Access, Data Model Action, Unit Action) */
const TRANSACTION_DETAIL_TABS = [
  { key: 'TransactionUnitAccess', label: 'Unit Access' },
  { key: 'TransactionFieldAccess', label: 'Field Access' },
  { key: 'TransactionCommandAccess', label: 'Command Access' },
  { key: 'TransactionAction', label: 'Data Model Action' },
  { key: 'TransactionUnitAction', label: 'Unit Action' },
] as const;

type SecurityTypeObj = {
  objType: number;
  DisplayName: string;
  IdProperty: string;
  AllItemList: any[];
};

type Props = { companyId: string | number | null; isEmbedded?: boolean };

/** Normalize privilege API response to array (Report/Search/Dashboard may return wrapped list). */
function privilegeListFromResponse(res: any): any[] {
  if (Array.isArray(res)) return res;
  if (res == null || typeof res !== 'object') return [];
  if (Array.isArray(res.Data)) return res.Data;
  if (Array.isArray(res.Items)) return res.Items;
  if (Array.isArray(res.ObjectList)) return res.ObjectList;
  if (Array.isArray(res.$values)) return res.$values;
  if (Array.isArray(res.Result)) return res.Result;
  const keys = Object.keys(res);
  if (keys.length > 0 && keys.every((k) => /^\d+$/.test(k)))
    return keys.sort((a, b) => Number(a) - Number(b)).map((k) => res[k]);
  return [];
}

const CompanyPrivilegeManagement: React.FC<Props> = ({ companyId }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [userTypeTabIndex, setUserTypeTabIndex] = useState(0);
  const currentPartnerType = USER_TYPE_TABS[userTypeTabIndex]?.partnerType ?? null;
  const [activeTabIndex, setActiveTabIndex] = useState(0);
  const [transactionDetailTab, setTransactionDetailTab] = useState<string>('TransactionUnitAccess');
  const [lookupData, setLookupData] = useState<Record<string, any[]>>({});
  const [availableItems, setAvailableItems] = useState<any[]>([]);
  const [selectedItems, setSelectedItems] = useState<any[]>([]);
  const [availableItemsCV] = useState(() => new CollectionView<any>([]));
  const [selectedItemsCV] = useState(() => new CollectionView<any>([]));
  const [currentTransaction, setCurrentTransaction] = useState<any>(null);
  const [transactionActionCV] = useState(() => new CollectionView<any>([]));
  const [transactionUnitActionCV] = useState(() => new CollectionView<any>([]));
  const [transactionUnitCV] = useState(() => new CollectionView<any>([]));
  const [transactionFieldCV] = useState(() => new CollectionView<any>([]));
  const [transactionCommandCV] = useState(() => new CollectionView<any>([]));
  const [addPopupOpen, setAddPopupOpen] = useState(false);
  const [securityEditorOpen, setSecurityEditorOpen] = useState(false);
  const [securityEditorParams, setSecurityEditorParams] = useState<{
    securityObjId: string | number | null;
    securityObjType: number | null;
    param2: SystemObjectSecurityEditorParam2 | null;
  }>({ securityObjId: null, securityObjType: null, param2: null });
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const selectedGridRef = useRef<any>(null);
  const availablePopupGridRef = useRef<any>(null);

  const currentObjType = PRIVILEGE_TABS[activeTabIndex]?.objType ?? EmAppSecuritySysObjType.Transaction;
  const securityTypeObj = useMemo((): SecurityTypeObj | null => {
    if (currentObjType === EmAppSecuritySysObjType.Transaction) {
      return { objType: currentObjType, DisplayName: 'Transaction', IdProperty: 'TransactionId', AllItemList: lookupData['AppTransaction'] ?? [] };
    }
    if (currentObjType === EmAppSecuritySysObjType.Search) {
      return { objType: currentObjType, DisplayName: 'Search', IdProperty: 'SearchId', AllItemList: lookupData['AppSearch'] ?? [] };
    }
    if (currentObjType === EmAppSecuritySysObjType.Dashboard) {
      return { objType: currentObjType, DisplayName: 'Desktop', IdProperty: 'DesktopId', AllItemList: lookupData['AppDesktop'] ?? [] };
    }
    if (currentObjType === EmAppSecuritySysObjType.Report) {
      return { objType: currentObjType, DisplayName: 'Report', IdProperty: 'ReportId', AllItemList: lookupData['AppReport'] ?? [] };
    }
    return null;
  }, [currentObjType, lookupData]);

  const loadLookup = useCallback(async () => {
    try {
      const data = await adminSvc.getMassEntitiesLookupItem(MASS_ENTITY_CODES);
      const out: Record<string, any[]> = {};
      for (const code of ['AppTransaction', 'AppSearch', 'AppSearchView', 'AppDesktop', 'AppReport']) {
        out[code] = Array.isArray(data?.[code]) ? data[code] : [];
      }
      setLookupData(out);
      return out;
    } catch {
      setLookupData({});
      return {};
    }
  }, []);

  const loadTabByType = useCallback(
    async (objType: number) => {
      if (!securityTypeObj) return;
      setErrorMessages([]);
      dispatch(setIsBusy());
      try {
        let list: any[] = [];
        if (currentPartnerType) {
          const res = await adminSvc.retrieveUserTypePrivilegeDtoByType(String(currentPartnerType.Id), String(objType));
          list = privilegeListFromResponse(res);
        } else {
          const res = await adminSvc.retrieveOrganizationPrivilegeDtoByType('null', String(objType));
          list = privilegeListFromResponse(res);
        }
        const idProp = securityTypeObj.IdProperty;
        const available = [...(securityTypeObj.AllItemList ?? [])];
        const selected = list.map((item: any) => {
          const id = item[idProp] ?? item.Id;
          const fromAll = available.find((a: any) => (a.Id ?? a[idProp]) === id);
          const resourceString = item.ResourceString ?? item.GroupUserString ?? item.Resource ?? '';
          return { ...item, [idProp]: id, Display: fromAll?.Display ?? item.Display ?? String(id), ResourceString: resourceString };
        });
        selected.forEach((sel: any) => {
          const idx = available.findIndex((a: any) => (a.Id ?? a[securityTypeObj.IdProperty]) === (sel[securityTypeObj.IdProperty] ?? sel.Id));
          if (idx >= 0) available.splice(idx, 1);
        });
        setAvailableItems(available);
        setSelectedItems(selected);
        availableItemsCV.sourceCollection = available;
        availableItemsCV.refresh();
        selectedItemsCV.sourceCollection = selected;
        selectedItemsCV.refresh();
        if (objType === EmAppSecuritySysObjType.Transaction && selected.length > 0 && selected[0].TransactionId) {
          const transactionId = selected[0].TransactionId;
          const organizationId = '';
          const userType = currentPartnerType ? String(currentPartnerType.Id) : '';
          const data = await adminSvc.getOneAppTransactionAvailablePrivileges(String(transactionId), organizationId, userType);
          setCurrentTransaction(data?.Transaction ?? null);
          const ensureBool = (arr: any[], ...keys: string[]) => {
            (arr || []).forEach((o: any) => keys.forEach((k) => { if (o[k] === undefined) o[k] = false; }));
          };
          const actionList = data?.TransactionActionPrivileges ?? [];
          const unitActionList = data?.TransactionUnitActionPrivileges ?? [];
          const unitList = data?.TransactionUnitPrivileges ?? [];
          const fieldList = data?.TransactionFieldPrivileges ?? [];
          const commandList = data?.TransactionCommandPrivileges ?? [];
          ensureBool(actionList, 'IsRestrict');
          ensureBool(unitActionList, 'IsRestrict');
          ensureBool(unitList, 'IsUnSaveAble', 'IsInVisible');
          ensureBool(fieldList, 'IsUnSaveAble', 'IsInVisible');
          ensureBool(commandList, 'IsRestrict');
          transactionActionCV.sourceCollection = actionList;
          transactionActionCV.refresh();
          transactionUnitActionCV.sourceCollection = unitActionList;
          transactionUnitActionCV.groupDescriptions.clear();
          transactionUnitActionCV.groupDescriptions.push(new PropertyGroupDescription('UnitUiName'));
          transactionUnitActionCV.refresh();
          transactionUnitCV.sourceCollection = unitList;
          transactionUnitCV.refresh();
          transactionFieldCV.sourceCollection = fieldList;
          transactionFieldCV.groupDescriptions.clear();
          transactionFieldCV.groupDescriptions.push(new PropertyGroupDescription('UnitUiName'));
          transactionFieldCV.refresh();
          transactionCommandCV.sourceCollection = commandList;
          transactionCommandCV.refresh();
        } else {
          setCurrentTransaction(null);
        }
      } catch (e: any) {
        setErrorMessages([e?.message ?? 'Failed to load privileges']);
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [currentPartnerType, securityTypeObj, dispatch, availableItemsCV, selectedItemsCV, transactionActionCV, transactionUnitActionCV, transactionUnitCV, transactionFieldCV, transactionCommandCV]
  );

  useEffect(() => {
    if (!companyId) return;
    loadLookup();
  }, [companyId, loadLookup]);

  useEffect(() => {
    if (!securityTypeObj) return;
    loadTabByType(currentObjType);
  }, [currentObjType, currentPartnerType, securityTypeObj]);

  const handleUserTypeTab = useCallback((index: number) => {
    setUserTypeTabIndex(index);
  }, []);

  const handleAddFromPopup = useCallback(() => {
    const grid = availablePopupGridRef.current?.control ?? availablePopupGridRef.current;
    if (!grid?.selectedRows?.length || !securityTypeObj) return;
    const newItemList = grid.selectedRows.map((row: any) => {
      const dataItem = row.dataItem;
      const idProp = securityTypeObj.IdProperty;
      return { [idProp]: dataItem.Id ?? dataItem[idProp], Display: dataItem.Display };
    });
    if (newItemList.length === 0) return;
    setAddPopupOpen(false);
    setErrorMessages([]);
    dispatch(setIsBusy());
    const payload = {
      AppSecuritySysObjType: securityTypeObj.objType,
      AppSecuritySysObjGroupUserSet: newItemList,
      ...(currentPartnerType ? { UserType: currentPartnerType.Id } : {}),
    };
    const savePromise = currentPartnerType
      ? adminSvc.saveNewUserTypePrivilegeByType(payload)
      : adminSvc.saveNewOrganizationPrivilegeByType(payload);
    savePromise
      .then((data: any) => {
        if (data?.IsSuccessful) loadTabByType(currentObjType);
        else if (data?.ValidationResult?.Items?.length) setErrorMessages((data.ValidationResult.Items as any[]).map((i: any) => i.Message ?? i.ErrorMessage).filter(Boolean) as string[]);
      })
      .catch((e: any) => setErrorMessages([e?.message ?? 'Save failed']))
      .finally(() => dispatch(setIsNotBusy()));
  }, [securityTypeObj, currentPartnerType, currentObjType, loadTabByType, dispatch]);

  const handleRemove = useCallback(() => {
    const grid = selectedGridRef.current?.control ?? selectedGridRef.current;
    if (!grid?.selectedRows?.length || !securityTypeObj) return;
    const deleteItemList = grid.selectedRows.map((row: any) => row.dataItem);
    if (deleteItemList.length === 0) return;
    setErrorMessages([]);
    dispatch(setIsBusy());
    const payload = {
      AppSecuritySysObjType: securityTypeObj.objType,
      AppSecuritySysObjGroupUserSet: deleteItemList,
      ...(currentPartnerType ? { UserType: currentPartnerType.Id } : {}),
    };
    const deletePromise = currentPartnerType
      ? adminSvc.deleteUserTypePrivilegeByType(payload)
      : adminSvc.deleteOrganizationPrivilegeByType(payload);
    deletePromise
      .then((data: any) => {
        if (data?.IsSuccessful) {
          setCurrentTransaction(null);
          loadTabByType(currentObjType);
        } else if (data?.ValidationResult?.Items?.length) {
          setErrorMessages((data.ValidationResult.Items as any[]).map((i: any) => i.Message ?? i.ErrorMessage).filter(Boolean) as string[]);
        }
      })
      .catch((e: any) => setErrorMessages([e?.message ?? 'Delete failed']))
      .finally(() => dispatch(setIsNotBusy()));
  }, [securityTypeObj, currentPartnerType, currentObjType, loadTabByType, dispatch]);

  const openSecurityEditor = useCallback(
    (item: any) => {
      if (!item) return;
      const partnerTypeId = currentPartnerType?.Id ?? null;
      const param2Base: SystemObjectSecurityEditorParam2 = {
        securityObjDisplay: item.Display ?? item.UnitUiName ?? item.CommandUiName ?? '',
        partnerType: partnerTypeId,
        companyId: companyId ?? null,
        actionCode: null,
        isIgnoreCurrentUserFilterSetup: false,
      };

      // Right panel: Transaction detail grids (Restricted Role & User) — check first so we don't treat unit/field/action row as left-grid Transaction
      const isRightPanelDetailRow =
        currentTransaction &&
        currentObjType === EmAppSecuritySysObjType.Transaction &&
        (item.TransactionUnitId != null ||
          item.TransactionFieldId != null ||
          item.CommandId != null ||
          item.UserActionTransactionCode != null ||
          item.UserActionTransactionUnitId != null);

      if (isRightPanelDetailRow) {
        if (transactionDetailTab === 'TransactionAction') {
          const securityObjId = currentTransaction.Id;
          const securityObjDisplay = `${currentTransaction.Id}: ${currentTransaction.Display ?? ''}: ${item.Display ?? ''}`;
          setSecurityEditorParams({
            securityObjId,
            securityObjType: EmAppSecuritySysObjType.TransactionAction,
            param2: {
              ...param2Base,
              securityObjDisplay,
              actionCode: item.UserActionTransactionCode ?? null,
              isIgnoreCurrentUserFilterSetup: undefined,
            },
          });
          setSecurityEditorOpen(true);
          return;
        }
        if (transactionDetailTab === 'TransactionUnitAction') {
          setSecurityEditorParams({
            securityObjId: item.UserActionTransactionUnitId,
            securityObjType: EmAppSecuritySysObjType.TransactionUnitAction,
            param2: {
              ...param2Base,
              securityObjDisplay: item.Display ?? '',
              actionCode: item.UserActionTransactionUnitCode ?? null,
              isIgnoreCurrentUserFilterSetup: undefined,
            },
          });
          setSecurityEditorOpen(true);
          return;
        }
        if (transactionDetailTab === 'TransactionUnitAccess') {
          const unitId = item.TransactionUnitId ?? '';
          const unitName = (item.UnitUiName ?? item.Display ?? '').trim();
          const securityObjDisplay = unitName && String(unitId) && unitName.startsWith(unitId + ':')
            ? unitName
            : `${unitId}: ${unitName || 'Unit'}`.trim();
          setSecurityEditorParams({
            securityObjId: item.TransactionUnitId,
            securityObjType: EmAppSecuritySysObjType.TransactionUnit,
            param2: {
              ...param2Base,
              securityObjDisplay,
              isIgnoreCurrentUserFilterSetup: undefined,
            },
          });
          setSecurityEditorOpen(true);
          return;
        }
        if (transactionDetailTab === 'TransactionFieldAccess') {
          setSecurityEditorParams({
            securityObjId: item.TransactionFieldId,
            securityObjType: EmAppSecuritySysObjType.TransactionField,
            param2: {
              ...param2Base,
              securityObjDisplay: item.Display ?? '',
              isIgnoreCurrentUserFilterSetup: undefined,
            },
          });
          setSecurityEditorOpen(true);
          return;
        }
        if (transactionDetailTab === 'TransactionCommandAccess') {
          setSecurityEditorParams({
            securityObjId: item.CommandId,
            securityObjType: EmAppSecuritySysObjType.TransactionCommand,
            param2: {
              ...param2Base,
              securityObjDisplay: item.CommandUiName ?? '',
              isIgnoreCurrentUserFilterSetup: undefined,
            },
          });
          setSecurityEditorOpen(true);
          return;
        }
        return;
      }

      // Left grid: Accessible (Transaction, Search, Dashboard, Report)
      if (securityTypeObj && currentObjType === securityTypeObj.objType) {
        const idProp = securityTypeObj.IdProperty;
        const securityObjId = idProp ? item[idProp] : item.TransactionId ?? item.SearchId ?? item.DesktopId ?? item.ReportId;
        if (securityObjId == null) return;
        setSecurityEditorParams({
          securityObjId,
          securityObjType: currentObjType,
          param2: { ...param2Base, securityObjDisplay: item.Display ?? '', isIgnoreCurrentUserFilterSetup: false },
        });
        setSecurityEditorOpen(true);
      }
    },
    [
      companyId,
      currentPartnerType,
      securityTypeObj,
      currentObjType,
      currentTransaction,
      transactionDetailTab,
    ]
  );

  const loadTransactionDetail = useCallback(
    async (transactionId: number | string) => {
      if (!transactionId) return;
      const organizationId = '';
      const userType = currentPartnerType ? String(currentPartnerType.Id) : '';
      try {
        const data = await adminSvc.getOneAppTransactionAvailablePrivileges(String(transactionId), organizationId, userType);
        setCurrentTransaction(data?.Transaction ?? null);
        const ensureBool = (arr: any[], ...keys: string[]) => {
          (arr || []).forEach((o: any) => keys.forEach((k) => { if (o[k] === undefined) o[k] = false; }));
        };
        const actionList = data?.TransactionActionPrivileges ?? [];
        const unitActionList = data?.TransactionUnitActionPrivileges ?? [];
        const unitList = data?.TransactionUnitPrivileges ?? [];
        const fieldList = data?.TransactionFieldPrivileges ?? [];
        const commandList = data?.TransactionCommandPrivileges ?? [];
        ensureBool(actionList, 'IsRestrict');
        ensureBool(unitActionList, 'IsRestrict');
        ensureBool(unitList, 'IsUnSaveAble', 'IsInVisible');
        ensureBool(fieldList, 'IsUnSaveAble', 'IsInVisible');
        ensureBool(commandList, 'IsRestrict');
        transactionActionCV.sourceCollection = actionList;
        transactionActionCV.refresh();
        transactionUnitActionCV.sourceCollection = unitActionList;
        transactionUnitActionCV.groupDescriptions.clear();
        transactionUnitActionCV.groupDescriptions.push(new PropertyGroupDescription('UnitUiName'));
        transactionUnitActionCV.refresh();
        transactionUnitCV.sourceCollection = unitList;
        transactionUnitCV.refresh();
        transactionFieldCV.sourceCollection = fieldList;
        transactionFieldCV.groupDescriptions.clear();
        transactionFieldCV.groupDescriptions.push(new PropertyGroupDescription('UnitUiName'));
        transactionFieldCV.refresh();
        transactionCommandCV.sourceCollection = commandList;
        transactionCommandCV.refresh();
      } catch {
        setCurrentTransaction(null);
      }
    },
    [currentPartnerType, transactionActionCV, transactionUnitActionCV, transactionUnitCV, transactionFieldCV, transactionCommandCV]
  );

  const handleSelectedGridSelectionChange = useCallback(
    (s: any) => {
      if (currentObjType !== EmAppSecuritySysObjType.Transaction) return;
      const flex = s?.control ?? s;
      const rowIndex = flex?.selection?.row ?? -1;
      if (rowIndex < 0 || !flex?.rows?.length) return;
      const item = flex.rows[rowIndex]?.dataItem;
      if (item?.TransactionId) loadTransactionDetail(item.TransactionId);
    },
    [currentObjType, loadTransactionDetail]
  );

  if (!companyId) {
    return (
      <div className={`p-4 ${theme.mainContentSection}`}>
        <p className={`text-xs ${theme.label}`}>Select a company.</p>
      </div>
    );
  }

  const hasData = lookupData.AppTransaction?.length || lookupData.AppSearch?.length || lookupData.AppDesktop?.length || lookupData.AppReport?.length;
  const emptyState = !securityTypeObj?.AllItemList?.length && activeTabIndex >= 0;

  const currentUserTypeLabel = USER_TYPE_TABS[userTypeTabIndex]?.label ?? 'Company User';

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`w-full h-1 flex-auto flex flex-col min-w-0 overflow-hidden ${theme.mainContentSection}`}>
        {/* Layer 1: User type — tabs only; selected tab shows upward triangle below (absolute so text height unchanged) */}
        <div className={`flex flex-wrap items-center gap-2 px-3 py-2 pb-3 border-b ${theme.mainContentSection}`}>
          {USER_TYPE_TABS.map((tab, idx) => (
            <div key={idx} className={`relative flex flex-col items-center ${userTypeTabIndex === idx ? theme.tab_active : ''}`}>
              <button
                type="button"
                className={`rounded-full px-3.5 py-1.5 text-xs font-medium whitespace-nowrap transition-colors ${
                  userTypeTabIndex === idx ? theme.tab_active : theme.tab
                }`}
                onClick={() => handleUserTypeTab(idx)}
              >
                {tab.label}
              </button>
              {userTypeTabIndex === idx && (
                <span className="absolute left-1/2 -translate-x-1/2 top-full mt-0.5 text-[10px] leading-none text-current" aria-hidden>▲</span>
              )}
            </div>
          ))}
        </div>

        {/* Layer 2: Category — tabs only with icon, theme colors (hover/active) */}
        <div className={`flex flex-wrap items-center gap-0 border-b ${theme.mainContentSection}`}>
          <div className={`flex rounded-lg overflow-hidden border ${theme.mainContentSection}`}>
            {PRIVILEGE_TABS.map((tab) => (
              <button
                key={tab.index}
                type="button"
                className={`flex items-center justify-center gap-1.5 w-[150px] min-w-[150px] px-3 py-1.5 text-xs whitespace-nowrap transition-colors border-r last:border-r-0 ${
                  activeTabIndex === tab.index ? theme.tab_active : theme.tab
                }`}
                onClick={() => setActiveTabIndex(tab.index)}
              >
                <i className={`fa-solid ${tab.icon} text-[11px] shrink-0`} aria-hidden />
                <span>{tab.heading}</span>
              </button>
            ))}
          </div>
        </div>

        {/* Title and content */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>
            Privileges : {currentUserTypeLabel}
          </div>
        </div>

          <div className={`w-full h-1 flex-auto flex flex-col overflow-hidden border ${theme.mainContentSection}`}>
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-sm ${theme.title}`}>
                {securityTypeObj?.DisplayName ?? 'Privileges'} Privileges
              </span>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => loadTabByType(currentObjType)} title="Refresh">
                <i className="fa-solid fa-rotate text-sm" aria-hidden />
              </button>
            </div>

            {errorMessages.length > 0 && (
              <div className="px-3 py-2 text-xs text-red-600 dark:text-red-400">
                {errorMessages.map((m, i) => (
                  <div key={i}>{m}</div>
                ))}
              </div>
            )}

            {emptyState && !hasData && (
              <div className={`flex items-center justify-center w-full h-1 flex-auto px-5 py-5 ${theme.mainContentSection}`}>
                <p className={`text-xs ${theme.label}`}>No privileges defined for this company.</p>
              </div>
            )}

            {(hasData || selectedItems.length > 0 || securityTypeObj?.AllItemList?.length) && (
              <div className="w-full h-1 flex-auto flex min-h-0 p-2">
                <div className="w-full h-full flex min-w-0">
                  <div className="w-1 flex-auto min-w-0 flex flex-col pr-2" style={{ minWidth: 280 }}>
                    <div className={`flex items-center justify-between mb-1`}>
                      <span className={`text-xs ${theme.label}`}>Accessible {securityTypeObj?.DisplayName ?? ''}</span>
                      <div className="flex items-center gap-2">
                        <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setAddPopupOpen(true)} title="Add" disabled={!securityTypeObj}>
                          <i className="fa-solid fa-plus text-sm mr-1" aria-hidden /> Add
                        </button>
                        <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleRemove} title="Remove" disabled={!securityTypeObj}>
                          <i className="fa-solid fa-minus text-sm mr-1" aria-hidden /> Remove
                        </button>
                      </div>
                    </div>
                    <div className="w-full h-1 flex-auto min-h-0">
                      <FlexGrid
                        ref={selectedGridRef}
                        itemsSource={selectedItemsCV}
                        selectionMode="ListBox"
                        headersVisibility="Column"
                        isReadOnly={true}
                        selectionChanged={handleSelectedGridSelectionChange}
                        className="h-full"
                      >
                        <FlexGridColumn header="Name" binding="Display" width={300} />
                        <FlexGridColumn header="Accessible Role & User" width="*">
                          <FlexGridCellTemplate
                            cellType="Cell"
                            template={(cell: any) => {
                              const item = cell.item;
                              return (
                                <div className="flex items-center gap-1">
                                  <button
                                    type="button"
                                    className={`w-7 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                                    title="Role & User"
                                    onClick={() => openSecurityEditor(item)}
                                  >
                                    <i className="fa-solid fa-lock-open" aria-hidden />
                                  </button>
                                  <span className={`text-xs ${theme.label} truncate`}>{item?.ResourceString ?? ''}</span>
                                </div>
                              );
                            }}
                          />
                        </FlexGridColumn>
                        <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                      </FlexGrid>
                    </div>
                  </div>

                  {currentObjType === EmAppSecuritySysObjType.Transaction && currentTransaction && (
                    <div className="w-1 flex-auto min-w-0 flex flex-col pl-2 border-l" style={{ minWidth: 280 }}>
                      <div className={`text-xs ${theme.label} mb-1 truncate`}>Data Model Restriction: {currentTransaction?.Display ?? ''}</div>
                      <div className="flex gap-0 border-b mb-1">
                        {TRANSACTION_DETAIL_TABS.map((t) => (
                          <button
                            key={t.key}
                            type="button"
                            className={`px-2 py-1 text-xs whitespace-nowrap ${transactionDetailTab === t.key ? theme.tab_active : theme.tab}`}
                            onClick={() => setTransactionDetailTab(t.key)}
                          >
                            {t.label}
                          </button>
                        ))}
                      </div>
                      <div className="w-full h-1 flex-auto min-h-0">
                        {transactionDetailTab === 'TransactionAction' && (
                          <FlexGrid itemsSource={transactionActionCV} headersVisibility="Column" selectionMode="Row" isReadOnly={true} className="h-full">
                            <FlexGridColumn header="Action Code" binding="Display" width={300} />
                            <FlexGridColumn header="Restricted Role & User" width="*">
                              <FlexGridCellTemplate
                                cellType="Cell"
                                template={(cell: any) => {
                                  const item = cell.item;
                                  return (
                                    <div className="flex items-center gap-1">
                                      <button type="button" className={`w-7 h-6 ${theme.button_default} rounded text-xs`} title="Role & User" onClick={() => openSecurityEditor(item)}>
                                        <i className="fa-solid fa-lock" aria-hidden />
                                      </button>
                                      <span className={`text-xs ${theme.label} truncate`}>{item?.ResourceString ?? ''}</span>
                                    </div>
                                  );
                                }}
                              />
                            </FlexGridColumn>
                            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                          </FlexGrid>
                        )}
                        {transactionDetailTab === 'TransactionUnitAction' && (
                          <FlexGrid itemsSource={transactionUnitActionCV} headersVisibility="Column" selectionMode="Row" isReadOnly={true} className="h-full">
                            <FlexGridColumn header="Action Code" binding="Display" width={300} />
                            <FlexGridColumn header="Restricted Role & User" width="*">
                              <FlexGridCellTemplate
                                cellType="Cell"
                                template={(cell: any) => (
                                  <div className="flex items-center gap-1">
                                    <button type="button" className={`w-7 h-6 ${theme.button_default} rounded text-xs`} title="Role & User" onClick={() => openSecurityEditor(cell.item)}>
                                      <i className="fa-solid fa-lock" aria-hidden />
                                    </button>
                                    <span className={`text-xs ${theme.label} truncate`}>{cell.item?.ResourceString ?? ''}</span>
                                  </div>
                                )}
                              />
                            </FlexGridColumn>
                            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                          </FlexGrid>
                        )}
                        {transactionDetailTab === 'TransactionUnitAccess' && (
                          <FlexGrid itemsSource={transactionUnitCV} headersVisibility="Column" selectionMode="Row" isReadOnly={true} className="h-full">
                            <FlexGridColumn header="Data Model Unit" binding="UnitUiName" width={300} />
                            <FlexGridColumn header="Restricted Role & User" width="*">
                              <FlexGridCellTemplate
                                cellType="Cell"
                                template={(cell: any) => (
                                  <div className="flex items-center gap-1">
                                    <button type="button" className={`w-7 h-6 ${theme.button_default} rounded text-xs`} title="Role & User" onClick={() => openSecurityEditor(cell.item)}>
                                      <i className="fa-solid fa-lock" aria-hidden />
                                    </button>
                                    <span className={`text-xs ${theme.label} truncate`}>{cell.item?.ResourceString ?? ''}</span>
                                  </div>
                                )}
                              />
                            </FlexGridColumn>
                            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                          </FlexGrid>
                        )}
                        {transactionDetailTab === 'TransactionFieldAccess' && (
                          <FlexGrid itemsSource={transactionFieldCV} headersVisibility="Column" selectionMode="Row" isReadOnly={true} className="h-full">
                            <FlexGridColumn header="Data Model Field" binding="Display" width={300} />
                            <FlexGridColumn header="Restricted Role & User" width="*">
                              <FlexGridCellTemplate
                                cellType="Cell"
                                template={(cell: any) => (
                                  <div className="flex items-center gap-1">
                                    <button type="button" className={`w-7 h-6 ${theme.button_default} rounded text-xs`} title="Role & User" onClick={() => openSecurityEditor(cell.item)}>
                                      <i className="fa-solid fa-lock" aria-hidden />
                                    </button>
                                    <span className={`text-xs ${theme.label} truncate`}>{cell.item?.ResourceString ?? ''}</span>
                                  </div>
                                )}
                              />
                            </FlexGridColumn>
                            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                          </FlexGrid>
                        )}
                        {transactionDetailTab === 'TransactionCommandAccess' && (
                          <FlexGrid itemsSource={transactionCommandCV} headersVisibility="Column" selectionMode="Row" isReadOnly={true} className="h-full">
                            <FlexGridColumn header="Command" binding="CommandUiName" width={300} />
                            <FlexGridColumn header="Restricted Role & User" width="*">
                              <FlexGridCellTemplate
                                cellType="Cell"
                                template={(cell: any) => (
                                  <div className="flex items-center gap-1">
                                    <button type="button" className={`w-7 h-6 ${theme.button_default} rounded text-xs`} title="Role & User" onClick={() => openSecurityEditor(cell.item)}>
                                      <i className="fa-solid fa-lock" aria-hidden />
                                    </button>
                                    <span className={`text-xs ${theme.label} truncate`}>{cell.item?.ResourceString ?? ''}</span>
                                  </div>
                                )}
                              />
                            </FlexGridColumn>
                            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
                          </FlexGrid>
                        )}
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        </div>

      <PrivilegeSecurityObjSelectorPopup
        open={addPopupOpen}
        title={securityTypeObj?.DisplayName ?? ''}
        availableItemsCV={availableItemsCV}
        gridRef={availablePopupGridRef}
        onClose={() => setAddPopupOpen(false)}
        onAdd={handleAddFromPopup}
      />

      <SystemObjectSecurityEditorPopup
        open={securityEditorOpen}
        onClose={() => {
          setSecurityEditorOpen(false);
          setSecurityEditorParams({ securityObjId: null, securityObjType: null, param2: null });
        }}
        securityObjId={securityEditorParams.securityObjId}
        securityObjType={securityEditorParams.securityObjType}
        param2={securityEditorParams.param2}
        onSaved={() => {
          loadTabByType(currentObjType);
          if (currentTransaction?.TransactionId) loadTransactionDetail(currentTransaction.TransactionId);
        }}
      />
    </div>
  );
};

export default CompanyPrivilegeManagement;
