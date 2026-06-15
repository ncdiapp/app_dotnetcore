import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../../webapi/adminsvc';
import { useTheme } from '../../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';

/** EmAppMenuRegisterType — align with Angular / C# enum */
const EmAppMenuRegisterType = {
  RegionDomain: 1,
  User: 2,
  Organization: 3,
  Role: 4,
} as const;

/** Collect leaf menu IDs from saved tree into dict */
function findLastLevelSelectedMenuIds(domainOrUserMenuData: any[], dict: Record<number, boolean>): void {
  if (!domainOrUserMenuData) return;
  for (const aMenu of domainOrUserMenuData) {
    if (aMenu.AppListMenu_List?.length) {
      findLastLevelSelectedMenuIds(aMenu.AppListMenu_List, dict);
    } else if (aMenu.Id) {
      dict[aMenu.Id] = true;
    }
  }
}

/** Set IsSelectedForDomainOrUser on tree from dict (parent = true if all children selected) */
function checkSelectedMenus(availableMenuList: any[], _parentMenu: any, dict: Record<number, boolean>): number {
  let count = 0;
  for (const aMenu of availableMenuList) {
    if (aMenu.AppListMenu_List?.length) {
      aMenu.IsSelectedForDomainOrUser = false;
      const childCount = checkSelectedMenus(aMenu.AppListMenu_List, aMenu, dict);
      if (childCount === aMenu.AppListMenu_List.length) aMenu.IsSelectedForDomainOrUser = true;
    } else {
      aMenu.IsSelectedForDomainOrUser = !!dict[aMenu.Id];
    }
    if (aMenu.IsSelectedForDomainOrUser) count++;
  }
  return count;
}

/** Deep clone tree and apply selection from dict */
function initialMenuData(allMenudata: any[], domainOrUserMenuData: any[]): any[] {
  const dict: Record<number, boolean> = {};
  findLastLevelSelectedMenuIds(domainOrUserMenuData, dict);
  const clone = (arr: any[]): any[] =>
    (arr || []).map((item) => ({
      ...item,
      AppListMenu_List: item.AppListMenu_List?.length ? clone(item.AppListMenu_List) : undefined,
    }));
  const allClone = clone(allMenudata);
  checkSelectedMenus(allClone, null, dict);
  return allClone;
}

/** Apply selected state to all nodes in tree */
function applySelectedStateOnChildMenus(childMenus: any[], isSelected: boolean, dict: Record<number, boolean>): void {
  if (!childMenus) return;
  for (const menuItem of childMenus) {
    menuItem.IsSelectedForDomainOrUser = isSelected;
    if (menuItem.Id) dict[menuItem.Id] = isSelected;
    if (menuItem.AppListMenu_List?.length) {
      applySelectedStateOnChildMenus(menuItem.AppListMenu_List, isSelected, dict);
    }
  }
}

/** Collect selected leaf menu IDs from tree */
function collectSelectedLeafIds(items: any[], out: number[]): void {
  if (!items) return;
  for (const item of items) {
    if (item.AppListMenu_List?.length) {
      collectSelectedLeafIds(item.AppListMenu_List, out);
    } else if (item.IsSelectedForDomainOrUser && item.Id) {
      out.push(item.Id);
    }
  }
}

type Props = { isEmbedded?: boolean };

const DomainAndUserMenuManagement: React.FC<Props> = ({ isEmbedded: _isEmbedded }) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();

  const menuRegisterType = EmAppMenuRegisterType.Role;

  const [roleCV, setRoleCV] = useState<CollectionView | null>(null);

  const [selectedRoleId, setSelectedRoleId] = useState<number | null>(null);

  const [listMenuCV, setListMenuCV] = useState<CollectionView | null>(null);
  const [modified, setModified] = useState(false);
  const [, setRefreshTrigger] = useState(0);

  const allMenuDataRef = useRef<any[]>([]);
  const dictLastLevelSelectedMenuIdsRef = useRef<Record<number, boolean>>({});
  const flexMenuRef = useRef<any>(null);

  const loadRoles = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const roleData = await adminSvc.retrieveAppSecurityGroupDtoByGroupUsage('4', false, '');
      const roles = Array.isArray(roleData) ? roleData : [];
      setRoleCV(new CollectionView(roles));
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch]);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  const loadMenuForCurrentSelection = useCallback(async () => {
    const id = selectedRoleId;
    if (id == null) {
      setListMenuCV(null);
      return;
    }
    const typeStr = String(EmAppMenuRegisterType.Role);
    dispatch(setIsBusy());
    try {
      const [allMenu, domainOrUserMenu] = await Promise.all([
        adminSvc.retrieveListMenuHairarchyDto(false, ''),
        adminSvc.retrieveDomainOrUserMenu(String(id), typeStr),
      ]);
      const allList = Array.isArray(allMenu) ? allMenu : [];
      const savedList = Array.isArray(domainOrUserMenu) ? domainOrUserMenu : domainOrUserMenu?.AppListMenu_List ? [domainOrUserMenu] : [];
      const merged = initialMenuData(allList, savedList);
      allMenuDataRef.current = merged;
      dictLastLevelSelectedMenuIdsRef.current = {};
      const walk = (items: any[]) => {
        if (!items) return;
        for (const m of items) {
          if (!m.AppListMenu_List?.length && m.Id) dictLastLevelSelectedMenuIdsRef.current[m.Id] = !!m.IsSelectedForDomainOrUser;
          if (m.AppListMenu_List?.length) walk(m.AppListMenu_List);
        }
      };
      walk(merged);
      setListMenuCV(new CollectionView(merged));
      setModified(false);
    } catch {
      setListMenuCV(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selectedRoleId, dispatch]);

  useEffect(() => {
    if (selectedRoleId != null) loadMenuForCurrentSelection();
    else setListMenuCV(null);
  }, [selectedRoleId, loadMenuForCurrentSelection]);

  const refresh = useCallback(() => {
    loadRoles();
    if (selectedRoleId != null) loadMenuForCurrentSelection();
  }, [loadRoles, loadMenuForCurrentSelection, selectedRoleId]);

  const save = useCallback(async () => {
    const id = selectedRoleId;
    if (id == null || !allMenuDataRef.current.length) return;
    const needToSaveMenuIds: number[] = [];
    collectSelectedLeafIds(allMenuDataRef.current, needToSaveMenuIds);
    dispatch(setIsBusy());
    try {
      const result = await adminSvc.saveDomainOrUserMenu({
        DomainOrUserId: id,
        MenuRegisterType: menuRegisterType,
        NeedToSaveMenuIds: needToSaveMenuIds,
      });
      if (result?.IsSuccessful) {
        setModified(false);
        refresh();
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [selectedRoleId, dispatch, refresh]);

  const selectOrUnselectAllMenus = useCallback(
    (isSelectAll: boolean) => {
      const data = allMenuDataRef.current;
      if (!data.length) return;
      applySelectedStateOnChildMenus(data, isSelectAll, dictLastLevelSelectedMenuIdsRef.current);
      setModified(true);
      setListMenuCV(new CollectionView([...data]));
    },
    []
  );

  const cellEditEnded = useCallback((_s: any, e: any) => {
    const grid = flexMenuRef.current?.control ?? flexMenuRef.current;
    if (!grid?.rows?.[e?.row]) return;
    const rowData = grid.rows[e.row].dataItem;
    if (!rowData) return;
    dictLastLevelSelectedMenuIdsRef.current[rowData.Id] = !!rowData.IsSelectedForDomainOrUser;
    if (rowData.AppListMenu_List?.length) {
      applySelectedStateOnChildMenus(rowData.AppListMenu_List, !!rowData.IsSelectedForDomainOrUser, dictLastLevelSelectedMenuIdsRef.current);
    }
    setModified(true);
    grid.invalidate();
  }, []);

  const showGrid = selectedRoleId != null && listMenuCV;

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Application Menu Privileges</div>
      </div>

      <div className="flex items-center gap-2 px-3 py-2 mb-1">
        <label className={`w-28 text-xs ${theme.label}`}>Security Role</label>
        <select
          className={`h-7 text-xs border px-2 flex-auto max-w-[280px] ${theme.inputBox}`}
          value={selectedRoleId ?? ''}
          onChange={(e) => setSelectedRoleId(e.target.value ? Number(e.target.value) : null)}
          disabled={modified}
        >
          <option value="">Select</option>
          {(roleCV?.sourceCollection as any[])?.map((r: any) => (
            <option key={r.Id} value={r.Id}>{r.Display ?? r.GroupName ?? r.Id}</option>
          )) ?? []}
        </select>
      </div>

      {showGrid && (
        <>
          <div className={`flex items-center gap-2 px-3 py-2 mb-1 ${theme.mainContentSection}`}>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`} onClick={refresh} title="Refresh">
              <i className="fa-solid fa-rotate" aria-hidden /> Refresh
            </button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default} disabled:opacity-50`} onClick={save} disabled={!modified} title="Save">
              <i className="fa-solid fa-save" aria-hidden /> Save
            </button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`} onClick={() => selectOrUnselectAllMenus(true)}>
              <i className="fa-regular fa-check-square" aria-hidden /> Select All
            </button>
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] flex items-center gap-1.5 ${theme.button_default}`} onClick={() => selectOrUnselectAllMenus(false)}>
              <i className="fa-regular fa-square" aria-hidden /> Unselect All
            </button>
          </div>
          <div className={`h-1 flex-auto min-h-0 overflow-hidden ${theme.mainContentSection}`} style={{ width: 400, minHeight: 200 }}>
            <FlexGrid
              ref={flexMenuRef}
              itemsSource={listMenuCV!}
              headersVisibility="None"
              selectionMode="Row"
              autoGenerateColumns={false}
              childItemsPath="AppListMenu_List"
              allowAddNew={false}
              allowDelete={false}
              isReadOnly={false}
              cellEditEnded={cellEditEnded}
              style={{ height: '100%' }}
            >
              <FlexGridColumn header="Name" binding="Name" width="*" isReadOnly>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    return item ? <span className="text-xs">{item.Name}</span> : null;
                  }}
                />
              </FlexGridColumn>
              <FlexGridColumn binding="IsSelectedForDomainOrUser" header="" width={50} dataType="Boolean" isReadOnly>
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const item = cell.item;
                    if (!item) return null;
                    return (
                      <input
                        type="checkbox"
                        checked={!!item.IsSelectedForDomainOrUser}
                        onChange={(e) => {
                          item.IsSelectedForDomainOrUser = e.target.checked;
                          if (item.Id) dictLastLevelSelectedMenuIdsRef.current[item.Id] = e.target.checked;
                          if (item.AppListMenu_List?.length) {
                            applySelectedStateOnChildMenus(item.AppListMenu_List, e.target.checked, dictLastLevelSelectedMenuIdsRef.current);
                          }
                          setModified(true);
                          (listMenuCV as CollectionView).refresh();
                          setRefreshTrigger((t) => t + 1);
                        }}
                      />
                    );
                  }}
                />
              </FlexGridColumn>
            </FlexGrid>
          </div>
        </>
      )}
    </div>
  );
};

export default DomainAndUserMenuManagement;
