import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

/** EmAppSecuritySysObjType (match Angular) */
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

const OBJ_TYPE_DISPLAY: Record<number, string> = {
  [EmAppSecuritySysObjType.Transaction]: 'Transaction',
  [EmAppSecuritySysObjType.TransactionField]: 'TransactionField',
  [EmAppSecuritySysObjType.TransactionUnit]: 'TransactionUnit',
  [EmAppSecuritySysObjType.Search]: 'Search',
  [EmAppSecuritySysObjType.SearchView]: 'SearchView',
  [EmAppSecuritySysObjType.Report]: 'Report',
  [EmAppSecuritySysObjType.Dashboard]: 'Desktop',
  [EmAppSecuritySysObjType.TransactionAction]: 'TransactionAction',
  [EmAppSecuritySysObjType.TransactionUnitAction]: 'TransactionUnitAction',
  [EmAppSecuritySysObjType.TransactionCommand]: 'TransactionCommand',
};

export type SystemObjectSecurityEditorParam2 = {
  securityObjDisplay?: string;
  organizationId?: number | null;
  partnerType?: number | null;
  /** When Company User tab: filter available Role & User by this company. */
  companyId?: string | number | null;
  actionCode?: number | null;
  isIgnoreCurrentUserFilterSetup?: boolean;
};

type Props = {
  open: boolean;
  onClose: () => void;
  securityObjId: string | number | null;
  securityObjType: number | null;
  param2: SystemObjectSecurityEditorParam2 | null;
  onSaved?: () => void;
};

type GroupUserItem = {
  type: 'Group' | 'User';
  GroupId?: number;
  UserId?: number;
  Display?: string;
  OrganizationId?: number;
  IsInVisible?: boolean;
  IsUnSaveAble?: boolean;
  IsNeedSpecailEditPrivilege?: boolean;
};

/** Normalize backend DTO to GroupUserItem; support alternate property names (e.g. camelCase, SecurityGroupId, or Id+Type). */
function ensureGroupUserType(item: any): GroupUserItem {
  const groupId = item?.GroupId ?? item?.groupId ?? item?.SecurityGroupId ?? item?.SecurityGroupID;
  const userId = item?.UserId ?? item?.userId ?? item?.SecurityUserId ?? item?.SecurityUserID;
  const id = item?.Id ?? item?.id;
  const typeFromBackend = (item?.Type ?? item?.type ?? '').toString().toLowerCase();
  const hasGroupName = (item?.GroupName ?? item?.groupName ?? '') !== '';
  const hasUserName = (item?.UserName ?? item?.userName ?? '') !== '';
  const o: GroupUserItem = {
    ...item,
    GroupId: groupId ?? (typeFromBackend === 'group' || hasGroupName ? id : undefined),
    UserId: userId ?? (typeFromBackend === 'user' || hasUserName ? id : undefined),
    Display: item?.Display ?? item?.display ?? item?.GroupName ?? item?.groupName ?? item?.UserName ?? item?.userName ?? '',
    IsInVisible: item?.IsInVisible ?? item?.isInVisible ?? false,
    IsUnSaveAble: item?.IsUnSaveAble ?? item?.isUnSaveAble ?? false,
    IsNeedSpecailEditPrivilege: item?.IsNeedSpecailEditPrivilege ?? item?.isNeedSpecailEditPrivilege ?? false,
  };
  if (o.GroupId != null) o.type = 'Group';
  else if (o.UserId != null) o.type = 'User';
  return o;
}

function findInList(list: GroupUserItem[], item: GroupUserItem): GroupUserItem | undefined {
  if (!list || !item) return undefined;
  if (item.type === 'Group' && item.GroupId != null)
    return list.find((x) => x.type === 'Group' && x.GroupId === item.GroupId);
  if (item.type === 'User' && item.UserId != null)
    return list.find((x) => x.type === 'User' && x.UserId === item.UserId);
  return undefined;
}

/** Partner-type role name substrings to exclude when current tab is Company User (Angular shows only company/employee roles). */
const PARTNER_TYPE_ROLE_NAME_SUBSTRINGS = ['Customer', 'Supplier', 'Client Agent', 'Supplier Agent'];

function isPartnerTypeRole(displayName: string): boolean {
  const name = (displayName ?? '').toLowerCase();
  return PARTNER_TYPE_ROLE_NAME_SUBSTRINGS.some((s) => name.includes(s.toLowerCase()));
}

/** Known wrapper keys for list responses (Restricted Role & User API may use any of these). */
const LIST_RESPONSE_KEYS = [
  'ObjectList',
  'Data',
  'Items',
  '$values',
  'Result',
  'AppSecuritySysObjGroupUserSet',
  'GroupUserSet',
  'List',
  'GroupUserList',
  'SelectedRoleAndUser',
  'DetailGroupUserPrivilegeList',
];

/** Backend may return array or { key: array }; normalize to array for Restricted Role & User. */
function toArray(res: any): any[] {
  if (Array.isArray(res)) return res;
  if (res == null) return [];
  if (typeof res !== 'object') return [];
  for (const key of LIST_RESPONSE_KEYS) {
    const val = res[key];
    if (Array.isArray(val)) return val;
  }
  const keys = Object.keys(res);
  if (keys.length > 0 && keys.every((k) => /^\d+$/.test(k)))
    return keys.sort((a, b) => Number(a) - Number(b)).map((k) => res[k]);
  for (const key of keys) {
    const val = res[key];
    if (Array.isArray(val)) return val;
  }
  return [];
}

const SystemObjectSecurityEditorPopup: React.FC<Props> = ({
  open,
  onClose,
  securityObjId,
  securityObjType,
  param2,
  onSaved,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const [availableItems, setAvailableItems] = useState<GroupUserItem[]>([]);
  const [selectedItems, setSelectedItems] = useState<GroupUserItem[]>([]);
  const [availableItemsCV] = useState(() => new CollectionView<GroupUserItem>([]));
  const [selectedItemsCV] = useState(() => new CollectionView<GroupUserItem>([]));
  const [securityObjSetDto, setSecurityObjSetDto] = useState<Record<string, any>>({});
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const availableGridRef = useRef<any>(null);
  const selectedGridRef = useRef<any>(null);

  const partnerType = param2?.partnerType ?? null;
  const companyId = param2?.companyId != null ? String(param2.companyId) : '';
  const isForOnePartnerType = partnerType != null;
  const securityObjDisplay = param2?.securityObjDisplay ?? '';
  const actionCode = param2?.actionCode ?? null;
  const isIgnoreCurrentUserFilterSetup = param2?.isIgnoreCurrentUserFilterSetup ?? false;
  const isIgnoreCurrentUserFilterSetupParam = param2?.isIgnoreCurrentUserFilterSetup;

  const securityTypeDisplay = securityObjType != null ? OBJ_TYPE_DISPLAY[Number(securityObjType)] ?? String(securityObjType) : '';
  const objTypeNum = securityObjType != null ? Number(securityObjType) : null;
  const isShowAdvancedCheckboxColumns = useMemo(
    () =>
      objTypeNum === EmAppSecuritySysObjType.TransactionUnit ||
      objTypeNum === EmAppSecuritySysObjType.TransactionField ||
      objTypeNum === EmAppSecuritySysObjType.TransactionAction ||
      objTypeNum === EmAppSecuritySysObjType.TransactionUnitAction ||
      objTypeNum === EmAppSecuritySysObjType.TransactionCommand,
    [objTypeNum]
  );
  const isShowSpecailEditCheckboxColumns = objTypeNum === EmAppSecuritySysObjType.TransactionField;
  // Restricted types (Unit/Field/Action/Command) always show "Restricted Role & User"; else Accessible or "Need Not Filter" text
  const description = isShowAdvancedCheckboxColumns
    ? 'Restricted Role & User'
    : isIgnoreCurrentUserFilterSetup
      ? "Roles and Users Need Not To 'Filter By Current User'"
      : 'Accessible Role & User';

  const loadData = useCallback(async () => {
    if (!open || securityObjId == null || securityObjType == null) return;
    const objId = String(securityObjId);
    const objType = String(securityObjType);
    const actCode = actionCode != null ? String(actionCode) : '';
    const partType = partnerType != null ? String(partnerType) : '';

    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      if (isForOnePartnerType) {
        const [allGroups, allUsers, selectedData] = await Promise.all([
          adminSvc.getPartnerTypeGroupList(partType),
          adminSvc.getPartnerTypeUserDtoList(partType),
          adminSvc.retrieveOrganizationDetailGroupUserPrivilegeDtoByType(
            objId,
            objType,
            param2?.organizationId != null ? String(param2.organizationId) : null,
            actionCode != null ? actCode : null,
            isIgnoreCurrentUserFilterSetupParam,
            partType
          ),
        ]);
        const allGroupsAndUsers: GroupUserItem[] = [];
        (allGroups || []).forEach((g: any) => {
          allGroupsAndUsers.push({
            type: 'Group',
            GroupId: g.Id,
            Display: g.GroupName,
            OrganizationId: g.OrganizationId,
          });
        });
        (allUsers || []).forEach((u: any) => {
          allGroupsAndUsers.push({
            type: 'User',
            UserId: u.Id,
            Display: u.UserName,
            OrganizationId: u.OrganizationId,
          });
        });
        const selected = toArray(selectedData).map(ensureGroupUserType);
        selected.forEach((s: GroupUserItem) => {
          const found = findInList(allGroupsAndUsers, s);
          if (found) {
            s.Display = found.Display;
            s.OrganizationId = found.OrganizationId;
            const idx = allGroupsAndUsers.indexOf(found);
            if (idx >= 0) allGroupsAndUsers.splice(idx, 1);
          }
        });
        setAvailableItems(allGroupsAndUsers);
        setSelectedItems(selected);
      } else {
        // Non-partner-type (Company User): use same APIs as Angular so data matches.
        // Use isIgnoreCurrentUserFilterSetup so backend returns Restricted Role & User when true, Accessible when false
        const selectedRoleAndUserRaw = await adminSvc.retrieveOrganizationDetailGroupUserPrivilegeDtoByType(
          objId,
          objType,
          param2?.organizationId != null ? String(param2.organizationId) : null,
          actionCode != null ? actCode : null,
          isIgnoreCurrentUserFilterSetupParam,
          partnerType ?? undefined
        );
        const selectedRoleAndUserData = toArray(selectedRoleAndUserRaw);

        let availalbeRoleAndUserData: any | null = null;
        try {
          availalbeRoleAndUserData = await appTransactionService.retrieveSecurityObjectAvailableOrganizationAllRoleAndUser(
            securityObjId,
            securityObjType,
            actionCode != null ? actionCode : undefined
          );
        } catch {
          availalbeRoleAndUserData = null;
        }

        const allGroupsAndUsers: GroupUserItem[] = [];
        const roleList = availalbeRoleAndUserData?.RoleList;
        const userList = availalbeRoleAndUserData?.UserList;
        if (Array.isArray(roleList) || Array.isArray(userList)) {
          // API returns available roles/users for current User Type; use as-is, no client-side filter
          (roleList ?? []).forEach((g: any) => {
            allGroupsAndUsers.push({
              type: 'Group',
              GroupId: g.Id,
              Display: g.GroupName,
              OrganizationId: g.OrganizationId,
            });
          });
          (userList ?? []).forEach((u: any) => {
            allGroupsAndUsers.push({
              type: 'User',
              UserId: u.Id,
              Display: u.UserName,
              OrganizationId: u.OrganizationId,
            });
          });
        } else {
          // Fallback when API 404/unavailable: filter by current User Type tab
          if (companyId) {
            // Company User tab: Role filtered by name (exclude partner-type); User filtered by getCompanyUserDtoList
            const [orgGroups, companyUsers] = await Promise.all([
              adminSvc.getOrganizationGroupList(companyId),
              adminSvc.getCompanyUserDtoList(companyId),
            ]);
            const orgGroupsList = toArray(orgGroups) as any[];
            orgGroupsList.forEach((g: any) => {
              const displayName = g.GroupName ?? g.Display ?? '';
              if (isPartnerTypeRole(displayName)) return; // Company User: only company/employee roles (match Angular)
              allGroupsAndUsers.push({
                type: 'Group',
                GroupId: g.Id,
                Display: displayName,
                OrganizationId: g.OrganizationId,
              });
            });
            (toArray(companyUsers)).forEach((u: any) => {
              allGroupsAndUsers.push({
                type: 'User',
                UserId: u.Id,
                Display: u.UserName ?? u.Display ?? '',
                OrganizationId: u.OrganizationId,
              });
            });
          } else {
            const massEntityData = await adminSvc.getMassEntitiesLookupItem('AppSecurityGroup|AppSecurityUser');
            (massEntityData?.['AppSecurityGroup'] ?? []).forEach((g: any) => {
              allGroupsAndUsers.push({
                type: 'Group',
                GroupId: g.Id,
                Display: g.GroupName ?? g.Display ?? '',
                OrganizationId: g.OrganizationId,
              });
            });
            (massEntityData?.['AppSecurityUser'] ?? []).forEach((u: any) => {
              allGroupsAndUsers.push({
                type: 'User',
                UserId: u.Id,
                Display: u.UserName ?? u.Display ?? '',
                OrganizationId: u.OrganizationId,
              });
            });
          }
        }
        const selected = selectedRoleAndUserData.map(ensureGroupUserType);
        selected.forEach((s: GroupUserItem) => {
          const found = findInList(allGroupsAndUsers, s);
          if (found) {
            s.Display = found.Display;
            s.OrganizationId = found.OrganizationId;
            const idx = allGroupsAndUsers.indexOf(found);
            if (idx >= 0) allGroupsAndUsers.splice(idx, 1);
          }
        });
        setAvailableItems(allGroupsAndUsers);
        setSelectedItems(selected);
      }

      setSecurityObjSetDto({
        AppSecuritySysObjId: securityObjId,
        AppSecuritySysObjType: securityObjType,
        OrganizationId: param2?.organizationId ?? null,
        PartnerType: partnerType,
        ActionCode: actionCode,
        securityObjDisplay,
        securityTypeDisplay,
        description,
        IsIgnoreCurrentUserFilterSetup: isIgnoreCurrentUserFilterSetup,
        isShowAdvancedCheckboxColumns,
        isShowSpecailEditCheckboxColumns,
        AppSecuritySysObjGroupUserSet: [],
        IsModified: false,
      });
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Failed to load data']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    open,
    securityObjId,
    securityObjType,
    partnerType,
    companyId,
    actionCode,
    isIgnoreCurrentUserFilterSetup,
    isForOnePartnerType,
    param2?.organizationId,
    securityObjDisplay,
    dispatch,
  ]);

  useEffect(() => {
    if (open && securityObjId != null && securityObjType != null) loadData();
  }, [open, securityObjId, securityObjType, loadData]);

  useEffect(() => {
    availableItemsCV.groupDescriptions.clear();
    availableItemsCV.groupDescriptions.push(new PropertyGroupDescription('type'));
    availableItemsCV.sourceCollection = availableItems;
    availableItemsCV.refresh();
  }, [availableItems, availableItemsCV]);
  useEffect(() => {
    selectedItemsCV.groupDescriptions.clear();
    selectedItemsCV.groupDescriptions.push(new PropertyGroupDescription('type'));
    selectedItemsCV.sourceCollection = selectedItems;
    selectedItemsCV.refresh();
  }, [selectedItems, selectedItemsCV]);

  const markChange = useCallback(() => {
    setSecurityObjSetDto((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const getSelectedDataItems = useCallback((flex: any): GroupUserItem[] => {
    const items: GroupUserItem[] = [];
    if (flex?.selectedRows?.length > 0) {
      for (let i = 0; i < flex.selectedRows.length; i++) {
        const row = flex.selectedRows[i];
        const dataItem = row?.dataItem as GroupUserItem;
        if (dataItem && (dataItem.GroupId != null || dataItem.UserId != null)) items.push(dataItem);
      }
    }
    if (items.length === 0 && flex?.rows?.length > 0) {
      for (let i = 0; i < flex.rows.length; i++) {
        const row = flex.rows[i];
        if (row?.isSelected) {
          const dataItem = row.dataItem as GroupUserItem;
          if (dataItem && (dataItem.GroupId != null || dataItem.UserId != null)) items.push(dataItem);
        }
      }
    }
    return items;
  }, []);

  const handleAdd = useCallback(() => {
    const flex = availableGridRef.current?.control ?? availableGridRef.current;
    const toAdd = getSelectedDataItems(flex);
    if (toAdd.length === 0) return;
    const newItems: GroupUserItem[] = toAdd.map((dataItem) => ({
      type: dataItem.type,
      GroupId: dataItem.GroupId,
      UserId: dataItem.UserId,
      Display: dataItem.Display,
      OrganizationId: dataItem.OrganizationId,
      IsInVisible: isShowAdvancedCheckboxColumns ? true : false,
      IsUnSaveAble: false,
      IsNeedSpecailEditPrivilege: false,
    }));
    const keysToRemove = new Set(toAdd.map((x) => (x.GroupId != null ? `g-${x.GroupId}` : `u-${x.UserId}`)));
    setSelectedItems((prev) => [...prev, ...newItems]);
    setAvailableItems((prev) => prev.filter((x) => !keysToRemove.has(x.GroupId != null ? `g-${x.GroupId}` : `u-${x.UserId}`)));
    markChange();
  }, [isShowAdvancedCheckboxColumns, getSelectedDataItems, markChange]);

  const handleRemove = useCallback(() => {
    const flex = selectedGridRef.current?.control ?? selectedGridRef.current;
    const toRemove = getSelectedDataItems(flex);
    if (toRemove.length === 0) return;
    const keysToRemove = new Set(toRemove.map((x) => (x.GroupId != null ? `g-${x.GroupId}` : `u-${x.UserId}`)));
    const addBack: GroupUserItem[] = toRemove.map((dataItem) => ({
      type: dataItem.type,
      GroupId: dataItem.GroupId,
      UserId: dataItem.UserId,
      Display: dataItem.Display,
      OrganizationId: dataItem.OrganizationId,
    }));
    setAvailableItems((prev) => [...prev, ...addBack]);
    setSelectedItems((prev) => prev.filter((x) => !keysToRemove.has(x.GroupId != null ? `g-${x.GroupId}` : `u-${x.UserId}`)));
    markChange();
  }, [getSelectedDataItems, markChange]);

  type VisibilityRadioValue = 'Invisible' | 'Readonly' | 'Specail Edit';
  const handleVisibilityRadioChange = useCallback(
    (item: GroupUserItem, value: VisibilityRadioValue) => {
      const groupId = item.GroupId;
      const userId = item.UserId;
      setSelectedItems((prev) => {
        const idx = prev.findIndex((x) => (groupId != null ? x.GroupId === groupId : x.UserId === userId));
        if (idx < 0) return prev;
        const it = prev[idx];
        const nextItem: GroupUserItem =
          value === 'Invisible'
            ? { ...it, IsInVisible: true, IsUnSaveAble: false, IsNeedSpecailEditPrivilege: false }
            : value === 'Readonly'
              ? { ...it, IsInVisible: false, IsUnSaveAble: true, IsNeedSpecailEditPrivilege: false }
              : { ...it, IsInVisible: false, IsUnSaveAble: false, IsNeedSpecailEditPrivilege: true };
        const next = prev.slice();
        next[idx] = nextItem;
        return next;
      });
      markChange();
    },
    [markChange]
  );

  const handleSave = useCallback(async () => {
    const dto = {
      ...securityObjSetDto,
      AppSecuritySysObjGroupUserSet: selectedItems,
    };
    dispatch(setIsBusy());
    setErrorMessages([]);
    try {
      const data = await adminSvc.saveOrganizationDetailLevelUserRowbyType(dto);
      const validationList = Array.isArray(data?.ValidationResult)
        ? data.ValidationResult
        : (data?.ValidationResult?.Items ?? data?.ValidationResult?.Errors ?? []);
      const messages = (Array.isArray(validationList) ? validationList : []).map((r: any) => r?.ErrorMessage ?? r?.Message ?? '').filter(Boolean);
      if (data?.IsSuccessful) {
        onSaved?.();
        setSecurityObjSetDto((prev) => (prev ? { ...prev, IsModified: false } : prev));
        await loadData();
      } else {
        setErrorMessages(messages.length ? messages : [data?.Message ?? 'Save failed']);
      }
    } catch (e: any) {
      setErrorMessages([e?.message ?? 'Save failed']);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [securityObjSetDto, selectedItems, dispatch, onSaved, loadData]);

  const handleSelectedCellEditEnded = useCallback(() => {
    selectedItemsCV.refresh();
    markChange();
  }, [selectedItemsCV, markChange]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" onClick={onClose}>
      <div
        className={`${theme.mainContentSection} rounded shadow-xl flex flex-col overflow-hidden`}
        style={{ width: 1080, maxWidth: '95vw', height: 560 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <span className={`text-sm font-semibold ${theme.title}`}>
            {securityTypeDisplay}: {description}
          </span>
          <div className="flex items-center gap-2">
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={loadData} title="Refresh">
              <i className="fa-solid fa-rotate text-sm" aria-hidden />
            </button>
            <button
              type="button"
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
              onClick={handleSave}
              disabled={!securityObjSetDto?.IsModified}
              title="Save"
            >
              <i className="fa-solid fa-save text-sm" aria-hidden />
            </button>
            <button type="button" className="text-lg leading-none w-8 h-8" onClick={onClose} aria-label="Close">
              &times;
            </button>
          </div>
        </div>

        <div className="px-3 py-2 flex items-center gap-4 flex-wrap">
          <div className="flex items-center gap-2">
            <span className={`text-xs ${theme.label}`}>{securityTypeDisplay}</span>
            <input
              type="text"
              value={securityObjDisplay}
              readOnly
              className={`flex-auto w-48 h-7 px-2 text-xs border ${theme.inputBox} bg-white`}
            />
          </div>
        </div>

        {errorMessages.length > 0 && (
          <div className="px-3 py-1 text-xs text-red-600 dark:text-red-400">
            {errorMessages.map((m, i) => (
              <div key={i}>{m}</div>
            ))}
          </div>
        )}

        <div className="flex-1 flex min-h-0 px-3 pb-3 gap-2">
          <div className="flex flex-col flex-1 min-w-0">
            <div className={`text-xs ${theme.label} mb-1`}>All Role And User (Ctrl+Click to multi-select)</div>
            <div className="flex-1 min-h-0">
              <FlexGrid
                ref={availableGridRef}
                itemsSource={availableItemsCV}
                selectionMode="MultiRange"
                headersVisibility="Column"
                isReadOnly={true}
                className="h-full"
              >
                <FlexGridColumn header="Type" binding="type" width={80} />
                <FlexGridColumn header="Name" binding="Display" width="*" />
              </FlexGrid>
            </div>
          </div>
          <div className="flex flex-col justify-center gap-1">
            <button type="button" className={`p-2 ${theme.button_default} rounded`} onClick={handleAdd} title="Add selected">
              <i className="fa-solid fa-chevron-right" aria-hidden />
            </button>
            <button type="button" className={`p-2 ${theme.button_default} rounded`} onClick={handleRemove} title="Remove selected">
              <i className="fa-solid fa-chevron-left" aria-hidden />
            </button>
          </div>
          <div className="flex flex-col flex-1 min-w-0">
            <div className={`text-xs ${theme.label} mb-1`}>{description}</div>
            <div className="flex-1 min-h-0">
              <FlexGrid
                ref={selectedGridRef}
                itemsSource={selectedItemsCV}
                selectionMode="MultiRange"
                headersVisibility="Column"
                isReadOnly={false}
                cellEditEnded={handleSelectedCellEditEnded}
                className="h-full"
              >
                <FlexGridColumn header="Type" binding="type" width={80} isReadOnly={true} visible={false} />
                <FlexGridColumn header="Name" binding="Display" width="*" isReadOnly={true} />
                {isShowAdvancedCheckboxColumns && (
                  <FlexGridColumn
                    header={isShowSpecailEditCheckboxColumns ? 'Invisible / Readonly / Specail Edit' : 'Invisible / Readonly'}
                    width={isShowSpecailEditCheckboxColumns ? 320 : 220}
                    isReadOnly={true}
                  >
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        const item = cell.item as GroupUserItem;
                        if (!item) return null;
                        const val: VisibilityRadioValue | null = item.IsNeedSpecailEditPrivilege
                          ? 'Specail Edit'
                          : item.IsInVisible
                            ? 'Invisible'
                            : item.IsUnSaveAble
                              ? 'Readonly'
                              : null;
                        const name = `visibility-${item.GroupId ?? item.UserId ?? cell.row}`;
                        // Layout: keep wrapper and radio classes as user-tuned standard; do not revert.
                        return (
                          <div
                            className="flex items-center justify-center gap-5 px-2 py-0 shrink-0"
                            onClick={(e) => e.stopPropagation()}
                          >
                            <label className="flex items-center gap-3 cursor-pointer">
                              <input
                                type="radio"
                                name={name}
                                checked={val === 'Invisible'}
                                onChange={() => handleVisibilityRadioChange(item, 'Invisible')}
                                className="w-2.5 h-2.5 shrink-0 mr-1"
                              />
                              <span className="text-xs leading-none">Invisible</span>
                            </label>
                            <label className="flex items-center gap-3 cursor-pointer">
                              <input
                                type="radio"
                                name={name}
                                checked={val === 'Readonly'}
                                onChange={() => handleVisibilityRadioChange(item, 'Readonly')}
                                className="w-2.5 h-2.5 shrink-0 mr-1"
                              />
                              <span className="text-xs leading-none">Readonly</span>
                            </label>
                            {isShowSpecailEditCheckboxColumns && (
                              <label className="flex items-center gap-3 cursor-pointer">
                                <input
                                  type="radio"
                                  name={name}
                                  checked={val === 'Specail Edit'}
                                  onChange={() => handleVisibilityRadioChange(item, 'Specail Edit')}
                                  className="w-2.5 h-2.5 shrink-0 mr-1"
                                />
                                <span className="text-xs leading-none">Specail Edit</span>
                              </label>
                            )}
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                )}
              </FlexGrid>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SystemObjectSecurityEditorPopup;
