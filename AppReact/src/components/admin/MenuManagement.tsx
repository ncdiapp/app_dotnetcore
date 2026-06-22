import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView, PropertyGroupDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useDispatch, useSelector } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { adminSvc } from '../../webapi/adminsvc';
import { refreshUserTreeMenu } from '../../helper/userMenuHelper';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import type { RootState } from '../../redux/store';
import { clampContextMenuPosition, useRefineContextMenuField } from '../../hooks/useClampedContextMenuPosition';

const CONTEXT_MENU_ESTIMATED_WIDTH = 170;
const CONTEXT_MENU_ESTIMATED_HEIGHT = 140;

type MenuDto = any;

const EmAppListMenuLinkType = {
  ApplicationPage: 1,
  WebPopup: 2,
  CallBuiltInFunction: 3,
  /** Application package / app root menu link (align with backend enum value 10) */
  Application: 10
} as const;

interface RowContextMenuState {
  visible: boolean;
  x: number;
  y: number;
  item: MenuDto | null;
}

const collectMenusDepthFirst = (items: MenuDto[] | null | undefined, out: MenuDto[] = []): MenuDto[] => {
  if (!Array.isArray(items)) return out;
  items.forEach((m) => {
    out.push(m);
    if (Array.isArray(m.AppListMenu_List) && m.AppListMenu_List.length > 0) {
      collectMenusDepthFirst(m.AppListMenu_List, out);
    }
  });
  return out;
};

const sortChildMenusBySort = (item: MenuDto): void => {
  const children = item?.AppListMenu_List;
  if (Array.isArray(children) && children.length > 0) {
    children.sort((a: MenuDto, b: MenuDto) => Number(a?.Sort ?? 0) - Number(b?.Sort ?? 0));
    children.forEach(sortChildMenusBySort);
  }
};

const sortAllTreeBySort = (list: MenuDto[]): MenuDto[] => {
  const clone = Array.isArray(list) ? list : [];
  clone.sort((a: MenuDto, b: MenuDto) => Number(a?.Sort ?? 0) - Number(b?.Sort ?? 0));
  clone.forEach(sortChildMenusBySort);
  return clone;
};

const MenuManagement: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError, showWarning, showInfo, showValidationMessages } = useErrorMessage();

  const userMenu = useSelector((state: RootState) => state.userSession.userMenu);

  // Root-level menu items = applications (same concept as Angular's threeLevelMenuList)
  const applicationList = useMemo(() => {
    if (!userMenu || !Array.isArray(userMenu)) return [];
    return userMenu.filter((item: any) => item && item.Id != null && item.Name != null);
  }, [userMenu]);

  const emAppDeviceMenuShowMode = useEnumValues('EmAppDeviceMenuShowMode');
  const deviceMenuShowModeList = useMemo(
    () =>
      emAppDeviceMenuShowMode
        ? Object.entries(emAppDeviceMenuShowMode).map(([Display, Id]) => ({ Id, Display }))
        : [],
    [emAppDeviceMenuShowMode]
  );

  const [menuTree, setMenuTree] = useState<MenuDto[]>([]);
  const [menuCV, setMenuCV] = useState<CollectionView>(() => new CollectionView<any>([]));
  const [isShowAdvancedColumns, setIsShowAdvancedColumns] = useState(false);
  const [selectedMenuId, setSelectedMenuId] = useState<number | null>(null);
  const [showNavigationSelector, setShowNavigationSelector] = useState(false);
  const [navigationSelectorActiveTabIndex, setNavigationSelectorActiveTabIndex] = useState(0);
  const [editingMenuId, setEditingMenuId] = useState<number | null>(null);
  const [rowContextMenu, setRowContextMenu] = useState<RowContextMenuState>({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  const rowContextMenuRef = useRef<HTMLDivElement | null>(null);

  const [manipulationDto, setManipulationDto] = useState<any>(null);
  const [dictLogicIdAndNavigationItem, setDictLogicIdAndNavigationItem] = useState<Record<string, any>>({});
  const [searchDataMap, setSearchDataMap] = useState<DataMap | null>(null);
  const [searchViewDataMap, setSearchViewDataMap] = useState<DataMap | null>(null);
  const [transactionDataMap, setTransactionDataMap] = useState<DataMap | null>(null);
  const [availableNavigationMenuItemsDataMap, setAvailableNavigationMenuItemsDataMap] = useState<DataMap | null>(null);

  const listMenuGridRef = useRef<any>(null);
  const navSearchGridRef = useRef<any>(null);
  const navListEditGridRef = useRef<any>(null);
  const navFolderGridRef = useRef<any>(null);
  const navSearchNavGridRef = useRef<any>(null);
  const navFormGridRef = useRef<any>(null);

  const appListMenuLinkTypeList = useMemo(
    () => [
      { Id: EmAppListMenuLinkType.ApplicationPage, Display: 'Application Page' },
      { Id: EmAppListMenuLinkType.WebPopup, Display: 'Web Page Popup' },
      { Id: EmAppListMenuLinkType.Application, Display: 'Application' }
    ],
    []
  );

  const appListMenuLinkTypeDataMap = useMemo(() => new DataMap(appListMenuLinkTypeList, 'Id', 'Display'), [appListMenuLinkTypeList]);
  const deviceMenuShowModeDataMap = useMemo(() => new DataMap(deviceMenuShowModeList, 'Id', 'Display'), [deviceMenuShowModeList]);

  const getSelectedMenuFromGrid = useCallback(() => {
    const flex = listMenuGridRef.current?.control ?? listMenuGridRef.current;
    const rowIndex = flex?.selection?.row ?? -1;
    if (rowIndex < 0 || !flex?.rows?.[rowIndex]) return null;
    return flex.rows[rowIndex].dataItem as MenuDto;
  }, []);

  const findMenuById = useCallback((id: number | null): MenuDto | null => {
    if (!id) return null;
    const all = collectMenusDepthFirst(menuTree);
    return all.find((m) => Number(m.Id) === Number(id)) ?? null;
  }, [menuTree]);

  const convertMenuPropertiesToNavigationLogicalId = useCallback((aMenuDto: MenuDto, dict: Record<string, any>) => {
    if (!aMenuDto) return;
    if (aMenuDto.RouteCode && aMenuDto.Link && !aMenuDto.NavigationItemLogicId && dict) {
      if (aMenuDto.RouteCode === 'MasterDataManagement') {
        const logicIdSearchNavigation = `Search_${aMenuDto.Link}`;
        const logicIdDataPresentation = `DataPresentation_${aMenuDto.Link}`;
        if (dict[logicIdSearchNavigation]) {
          aMenuDto.NavigationItemLogicId = logicIdSearchNavigation;
        } else if (dict[logicIdDataPresentation]) {
          aMenuDto.NavigationItemLogicId = logicIdDataPresentation;
        }
      } else {
        let logicId: string | null = null;
        if (aMenuDto.RouteCode === 'FolderNavigation') logicId = `FolderTree_${aMenuDto.Link}`;
        else if (aMenuDto.RouteCode === 'FormListEdit') logicId = `ListEdit_${aMenuDto.Link}`;
        else if (aMenuDto.RouteCode === 'FormMasterDetail') logicId = `FormMasterDetail_${aMenuDto.Link}`;
        if (logicId && dict[logicId]) aMenuDto.NavigationItemLogicId = logicId;
      }
    }
    if (Array.isArray(aMenuDto.AppListMenu_List)) {
      aMenuDto.AppListMenu_List.forEach((sub: MenuDto) => convertMenuPropertiesToNavigationLogicalId(sub, dict));
    }
  }, []);

  const convertNavigationLogicalIdToMenuProperties = useCallback((aMenuDto: MenuDto, dict: Record<string, any>) => {
    if (!aMenuDto) return;
    if (aMenuDto.NavigationItemLogicId && dict) {
      const nav = dict[aMenuDto.NavigationItemLogicId];
      if (nav) {
        aMenuDto.RouteCode = nav.RouteCode;
        aMenuDto.Link = nav.Link;
        aMenuDto.LinkType = nav.LinkType;
        aMenuDto.IconName = aMenuDto.IconName || nav.IconName;
        if (!aMenuDto.EmDeviceMenuShowMode && emAppDeviceMenuShowMode?.ForBoth != null) {
          aMenuDto.EmDeviceMenuShowMode = emAppDeviceMenuShowMode.ForBoth;
        }
      }
    }
    if (Array.isArray(aMenuDto.AppListMenu_List)) {
      aMenuDto.AppListMenu_List.forEach((sub: MenuDto) => convertNavigationLogicalIdToMenuProperties(sub, dict));
    }
  }, [emAppDeviceMenuShowMode]);

  const buildNavigationItems = useCallback((mDto: any, sMap: DataMap, tMap: DataMap) => {
    const availableNavigationMenuItems: any[] = [];
    const dict: Record<string, any> = {};

    (mDto?.SearchNavigationList || []).forEach((aNavigation: any) => {
      const menuItem = {
        logicId: `Search_${aNavigation.QuickSearchId}`,
        Name: sMap.getDisplayValue(aNavigation.QuickSearchId),
        Description: `Search: ${sMap.getDisplayValue(aNavigation.QuickSearchId) || ''}`,
        LinkType: 1,
        RouteCode: 'MasterDataManagement',
        Link: aNavigation.QuickSearchId,
        IconName: 'Fine Print-64.png',
        EmDeviceMenuShowMode: 3
      };
      aNavigation.logicMenuItem = menuItem;
      availableNavigationMenuItems.push(menuItem);
      dict[menuItem.logicId] = menuItem;
    });

    (mDto?.FolderNavigationList || []).forEach((aNavigation: any) => {
      const txName = tMap.getDisplayValue(aNavigation.TransactionId);
      const menuItem = {
        logicId: `FolderTree_${aNavigation.TransactionId}`,
        Name: txName,
        Description: `Folder Tree: ${txName || ''}`,
        LinkType: 1,
        RouteCode: 'FolderNavigation',
        Link: aNavigation.TransactionId,
        IconName: 'Open Folder-64.png',
        EmDeviceMenuShowMode: 3
      };
      aNavigation.logicMenuItem = menuItem;
      availableNavigationMenuItems.push(menuItem);
      dict[menuItem.logicId] = menuItem;
    });

    (mDto?.ListEditTransactionList || []).forEach((aListTransaction: any) => {
      const menuItem = {
        logicId: `ListEdit_${aListTransaction.Id}`,
        Name: aListTransaction.TransactionName,
        Description: `List Edit: ${aListTransaction.TransactionName || ''}`,
        LinkType: 1,
        RouteCode: 'FormListEdit',
        Link: aListTransaction.Id,
        IconName: 'Todo List-64.png',
        EmDeviceMenuShowMode: 3
      };
      aListTransaction.logicMenuItem = menuItem;
      availableNavigationMenuItems.push(menuItem);
      dict[menuItem.logicId] = menuItem;
    });

    (mDto?.MasterDetailTransactionList || []).forEach((aTransaction: any) => {
      aTransaction.LinkParam1 = null;
      aTransaction.LinkParam2 = null;
      const menuItem = {
        logicId: `FormMasterDetail_${aTransaction.Id}`,
        Name: aTransaction.TransactionName,
        Description: `Form: ${aTransaction.TransactionName || ''}`,
        LinkType: 1,
        RouteCode: 'FormMasterDetail',
        Link: aTransaction.Id,
        LinkParam1: null,
        LinkParam2: null,
        IconName: 'Rules-64.png',
        EmDeviceMenuShowMode: 3
      };
      aTransaction.logicMenuItem = menuItem;
      availableNavigationMenuItems.push(menuItem);
      dict[menuItem.logicId] = menuItem;
    });

    (mDto?.ApplicationSearchDtoList || []).forEach((aSearchDto: any) => {
      const menuItem = {
        logicId: `DataPresentation_${aSearchDto.Id}`,
        Name: aSearchDto.Name,
        Description: `Report & View: ${aSearchDto.Name || ''}`,
        LinkType: 1,
        RouteCode: 'MasterDataManagement',
        Link: aSearchDto.Id,
        IconName: 'Pie Chart-64.png',
        EmDeviceMenuShowMode: 3
      };
      aSearchDto.logicMenuItem = menuItem;
      availableNavigationMenuItems.push(menuItem);
      dict[menuItem.logicId] = menuItem;
    });

    return { availableNavigationMenuItems, dict };
  }, []);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [menuData, massEntityData] = await Promise.all([
        adminSvc.retrieveListMenuHairarchyDto(false, ''),
        adminSvc.getMassEntitiesLookupItem('AppSearch|AppSearchView|AppTransaction')
      ]);

      const manipulations = await Promise.all(
        applicationList.map((app: any) =>
          adminSvc.retrieveAppApplicationDataManipulations(String(app.Id)).catch(() => null)
        )
      );

      const searches = massEntityData?.AppSearch || [];
      const searchViews = massEntityData?.AppSearchView || [];
      const transactions = massEntityData?.AppTransaction || [];
      const sMap = new DataMap(searches, 'Id', 'Display');
      const svMap = new DataMap(searchViews, 'Id', 'Display');
      const tMap = new DataMap(transactions, 'Id', 'Display');
      setSearchDataMap(sMap);
      setSearchViewDataMap(svMap);
      setTransactionDataMap(tMap);

      const mergedManipulationDto: any = {
        ApplicationId: null,
        PublishedFormList: [],
        ListEditTransactionList: [],
        MasterDetailTransactionList: [],
        FolderNavigationList: [],
        SearchNavigationList: [],
        AllSearchDtoList: [],
        ApplicationSearchDtoList: []
      };

      const mergeWithAppContext = (app: any, list: any[] | null | undefined) => {
        if (!Array.isArray(list)) return [];
        const out = list;
        out.forEach((it: any) => {
          if (it?.SaasApplicationId == null) it.SaasApplicationId = app.Id;
          it.ApplicationDisplay = app.Name;
        });
        return out;
      };

      applicationList.forEach((app: any, idx: number) => {
        const mDto = manipulations[idx];
        if (!mDto) return;

        mergedManipulationDto.PublishedFormList.push(...(mDto.PublishedFormList || []));
        mergedManipulationDto.ListEditTransactionList.push(
          ...mergeWithAppContext(app, mDto.ListEditTransactionList || [])
        );
        mergedManipulationDto.MasterDetailTransactionList.push(
          ...mergeWithAppContext(app, mDto.MasterDetailTransactionList || [])
        );
        mergedManipulationDto.FolderNavigationList.push(
          ...mergeWithAppContext(app, mDto.FolderNavigationList || [])
        );
        mergedManipulationDto.SearchNavigationList.push(
          ...mergeWithAppContext(app, mDto.SearchNavigationList || [])
        );
        mergedManipulationDto.AllSearchDtoList.push(...(mDto.AllSearchDtoList || []));
        mergedManipulationDto.ApplicationSearchDtoList.push(
          ...mergeWithAppContext(app, mDto.ApplicationSearchDtoList || [])
        );
      });

      const { availableNavigationMenuItems, dict } = buildNavigationItems(mergedManipulationDto, sMap, tMap);
      setManipulationDto(mergedManipulationDto);
      setDictLogicIdAndNavigationItem(dict);
      setAvailableNavigationMenuItemsDataMap(new DataMap(availableNavigationMenuItems, 'logicId', 'Description'));

      const listData: MenuDto[] = Array.isArray(menuData)
        ? menuData
        : Array.isArray(menuData?.ObjectList)
          ? menuData.ObjectList
          : Array.isArray(menuData?.Object)
            ? menuData.Object
            : [];

      listData.forEach((m: MenuDto) => convertMenuPropertiesToNavigationLogicalId(m, dict));
      const sorted = sortAllTreeBySort(listData);
      setMenuTree(sorted);
      const cv = new CollectionView(sorted);
      setMenuCV(cv);
      setSelectedMenuId(null);
    } catch (error: any) {
      showError(error?.message || 'Failed to load menus');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [applicationList, buildNavigationItems, convertMenuPropertiesToNavigationLogicalId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const refreshGrid = useCallback(() => {
    setMenuCV(new CollectionView<any>([...menuTree]));
  }, [menuTree]);

  const saveAllMenu = useCallback(async () => {
    const saveList = JSON.parse(JSON.stringify(menuTree || []));
    saveList.forEach((m: MenuDto) => convertNavigationLogicalIdToMenuProperties(m, dictLogicIdAndNavigationItem));
    dispatch(setIsBusy());
    try {
      const saveData = { ListMenuSet: saveList, IsWebPageMenu: false };
      const data = await adminSvc.saveAllTreeListMenuDto(saveData);
      if (data?.ValidationResult) showValidationMessages(data.ValidationResult, true);
      if (data?.IsSuccessful) {
        showInfo('Menu saved');
        await loadData();
        await refreshUserTreeMenu();
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to save menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [convertNavigationLogicalIdToMenuProperties, dictLogicIdAndNavigationItem, dispatch, loadData, menuTree, showError, showInfo]);

  const addMenu = useCallback(async (parentMenuArg?: MenuDto | null) => {
    const currentGroup = parentMenuArg ?? getSelectedMenuFromGrid();
    if (!currentGroup?.Id) {
      showWarning('Please select a parent menu');
      return;
    }
    const newMenu: MenuDto = {
      ParentId: currentGroup.Id,
      Name: 'New Menu',
      Description: '',
      RouteCode: '',
      IconName: '',
      IconName2: '',
      Sort: 1,
      EmDeviceMenuShowMode: emAppDeviceMenuShowMode?.ForBoth ?? 3,
      EmAppMenuItemCategory: 1,
      LinkType: EmAppListMenuLinkType.ApplicationPage,
      IsNew: true
    };
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveOneAppListMenuTreeNode(newMenu);
      if (data?.IsSuccessful && data?.Object) {
        await loadData();
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to add sub menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, emAppDeviceMenuShowMode?.ForBoth, getSelectedMenuFromGrid, loadData, showError, showWarning]);

  const deleteMenu = useCallback(async (menuItemArg?: MenuDto | null) => {
    const menuItem = menuItemArg ?? getSelectedMenuFromGrid();
    if (!menuItem?.Id) {
      showWarning('Please select a menu to delete');
      return;
    }
    if (!window.confirm('Please confirm to delete this menu.')) return;

    const saveList = JSON.parse(JSON.stringify(menuTree || []));
    saveList.forEach((m: MenuDto) => convertNavigationLogicalIdToMenuProperties(m, dictLogicIdAndNavigationItem));

    dispatch(setIsBusy());
    try {
      const saved = await adminSvc.saveAllTreeListMenuDto({ ListMenuSet: saveList, IsWebPageMenu: false });
      if (!saved?.IsSuccessful) {
        if (saved?.ValidationResult) {
          showValidationMessages(saved.ValidationResult, true);
        } else {
          showError('Save failed before delete');
        }
        return;
      }
      const data = await adminSvc.deleteOneAppListMenuTreeNode(String(menuItem.Id));
      if (data?.IsSuccessful) {
        await loadData();
      } else {
        if (data?.ValidationResult) {
          showValidationMessages(data.ValidationResult, true);
        } else {
          showError('Delete failed');
        }
      }
    } catch (error: any) {
      showError(error?.message || 'Failed to delete menu');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [convertNavigationLogicalIdToMenuProperties, dictLogicIdAndNavigationItem, dispatch, getSelectedMenuFromGrid, loadData, menuTree, showError, showValidationMessages, showWarning]);

  const openNavigationSelector = useCallback((item: MenuDto) => {
    if (!item) return;
    setEditingMenuId(Number(item.Id));
    setNavigationSelectorActiveTabIndex(0);
    setShowNavigationSelector(true);
  }, []);

  const clearNavigationItem = useCallback(() => {
    const menuItem = findMenuById(editingMenuId);
    if (!menuItem) return;
    menuItem.NavigationItemLogicId = '';
    menuItem.RouteCode = null;
    menuItem.Link = null;
    menuItem.LinkParam1 = null;
    menuItem.LinkParam2 = null;
    menuItem.IsModified = true;
    refreshGrid();
    setShowNavigationSelector(false);
  }, [editingMenuId, findMenuById, refreshGrid]);

  const getNavigationGrid = useCallback((tabIdx: number) => {
    if (tabIdx === 0) return navSearchGridRef.current?.control ?? navSearchGridRef.current;
    if (tabIdx === 1) return navListEditGridRef.current?.control ?? navListEditGridRef.current;
    if (tabIdx === 2) return navFolderGridRef.current?.control ?? navFolderGridRef.current;
    if (tabIdx === 3) return navSearchNavGridRef.current?.control ?? navSearchNavGridRef.current;
    if (tabIdx === 4) return navFormGridRef.current?.control ?? navFormGridRef.current;
    return null;
  }, []);

  const applySelectedNavigation = useCallback(() => {
    const menuItem = findMenuById(editingMenuId);
    if (!menuItem) return;
    const flex = getNavigationGrid(navigationSelectorActiveTabIndex);
    const row = flex?.selection?.row ?? -1;
    const dataItem = row >= 0 ? flex?.rows?.[row]?.dataItem : null;
    if (dataItem?.logicMenuItem?.logicId) {
      menuItem.NavigationItemLogicId = dataItem.logicMenuItem.logicId;
      menuItem.LinkParam1 = dataItem.LinkParam1 || null;
      menuItem.LinkParam2 = dataItem.LinkParam2 || null;
      menuItem.IsModified = true;
      refreshGrid();
    }
    setShowNavigationSelector(false);
  }, [editingMenuId, findMenuById, getNavigationGrid, navigationSelectorActiveTabIndex, refreshGrid]);

  const onMainGridSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    const rowIndex = flex?.selection?.row ?? -1;
    const item = rowIndex >= 0 ? flex?.rows?.[rowIndex]?.dataItem : null;
    setSelectedMenuId(item?.Id ?? null);
  }, []);

  const onCellEditEnded = useCallback((s: any, e: any) => {
    const flex = s?.control ?? s;
    const row = e?.row ?? -1;
    const rowData = row >= 0 ? flex?.rows?.[row]?.dataItem : null;
    if (rowData?.Id) rowData.IsModified = true;
  }, []);

  const itemFormatter = useCallback((panel: any, r: number) => {
    // Match Angular behavior: tree rows editable unless a column is explicitly read-only.
    if (panel?.rows?.[r]) {
      panel.rows[r].isReadOnly = false;
    }
  }, []);

  const collapseAll = useCallback(() => {
    const flex = listMenuGridRef.current?.control ?? listMenuGridRef.current;
    if (flex?.collapseGroupsToLevel) {
      flex.collapseGroupsToLevel(0);
    }
  }, []);

  const expandAll = useCallback(() => {
    const flex = listMenuGridRef.current?.control ?? listMenuGridRef.current;
    if (flex?.collapseGroupsToLevel) {
      flex.collapseGroupsToLevel(10);
    }
  }, []);

  const selectedMenu = findMenuById(selectedMenuId);

  const applicationSearchCV = useMemo(() => {
    if (!manipulationDto) return null;
    const list = manipulationDto.ApplicationSearchDtoList || [];
    const cv = new CollectionView(list);
    cv.groupDescriptions.push(new PropertyGroupDescription('ApplicationDisplay'));
    return cv;
  }, [manipulationDto]);

  const listEditTransactionCV = useMemo(() => {
    if (!manipulationDto) return null;
    const list = manipulationDto.ListEditTransactionList || [];
    const cv = new CollectionView(list);
    cv.groupDescriptions.push(new PropertyGroupDescription('ApplicationDisplay'));
    return cv;
  }, [manipulationDto]);

  const folderNavigationCV = useMemo(() => {
    if (!manipulationDto) return null;
    const list = manipulationDto.FolderNavigationList || [];
    const cv = new CollectionView(list);
    cv.groupDescriptions.push(new PropertyGroupDescription('ApplicationDisplay'));
    return cv;
  }, [manipulationDto]);

  const searchNavigationCV = useMemo(() => {
    if (!manipulationDto) return null;
    const list = manipulationDto.SearchNavigationList || [];
    const cv = new CollectionView(list);
    cv.groupDescriptions.push(new PropertyGroupDescription('ApplicationDisplay'));
    return cv;
  }, [manipulationDto]);

  const masterDetailTransactionCV = useMemo(() => {
    if (!manipulationDto) return null;
    const list = manipulationDto.MasterDetailTransactionList || [];
    const cv = new CollectionView(list);
    cv.groupDescriptions.push(new PropertyGroupDescription('ApplicationDisplay'));
    return cv;
  }, [manipulationDto]);

  useEffect(() => {
    if (!rowContextMenu.visible) return;
    const handleDocClick = () => {
      setRowContextMenu({ visible: false, x: 0, y: 0, item: null });
    };
    document.addEventListener('click', handleDocClick);
    return () => document.removeEventListener('click', handleDocClick);
  }, [rowContextMenu.visible]);

  useRefineContextMenuField(rowContextMenu.visible, rowContextMenuRef, setRowContextMenu);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Menu Setting</div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={loadData} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={saveAllMenu} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden /> Save
          </button>
          <button
            type="button"
            onClick={() => setIsShowAdvancedColumns((v) => !v)}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
          >
            <i className="fa-solid fa-gear mr-1" aria-hidden /> Show Advanced Columns
            {isShowAdvancedColumns ? <i className="fa-solid fa-check ml-1" aria-hidden /> : null}
          </button>
        </div>
      </div>

      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={listMenuGridRef}
          className="w-full h-full"
          itemsSource={menuCV}
          selectionMode="Row"
          autoGenerateColumns={false}
          allowSorting={false}
          childItemsPath="AppListMenu_List"
          allowAddNew={false}
          allowDelete={false}
          isReadOnly={false}
          headersVisibility="Column"
          treeIndent={20}
          itemFormatter={itemFormatter}
          selectionChanged={onMainGridSelectionChanged}
          cellEditEnded={onCellEditEnded}
        >
          <FlexGridColumn header="Name" binding="Name" width={300} allowSorting={false} isReadOnly={false}>
            <FlexGridCellTemplate
              cellType="ColumnHeader"
              template={() => (
                <div className="w-full h-full flex items-center gap-1 px-1">
                  <button type="button" onClick={expandAll} className={`px-2 h-5 text-xs rounded-[4px] ${theme.button_default}`} title="Expand All Menus">
                    <i className="fa-solid fa-square-plus mr-1" aria-hidden /> Expand All
                  </button>
                  <button type="button" onClick={collapseAll} className={`px-2 h-5 text-xs rounded-[4px] ${theme.button_default}`} title="Collapse All Menus">
                    <i className="fa-solid fa-square-minus mr-1" aria-hidden /> Collapse All
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>

          <FlexGridColumn width={60} header="" isReadOnly={true} allowSorting={false}>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => {
                const item = cell.item;
                if (!item) return null;
                return (
                  <div className="flex items-center justify-center w-full">
                    <button
                      type="button"
                      className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                      title="More Options"
                      onClick={(e) => {
                        e.stopPropagation();
                        const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                        const { x, y } = clampContextMenuPosition(
                          rect.right,
                          rect.top,
                          CONTEXT_MENU_ESTIMATED_WIDTH,
                          CONTEXT_MENU_ESTIMATED_HEIGHT
                        );
                        setRowContextMenu({ visible: true, x, y, item });
                      }}
                    >
                      <i className="fa-solid fa-pencil text-xs" aria-hidden />
                      <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                    </button>
                  </div>
                );
              }}
            />
          </FlexGridColumn>

          <FlexGridColumn header="" binding="Sort" width={70} format="d" visible={true} isReadOnly={false} />
          <FlexGridColumn header="Icon" binding="IconName2" allowSorting={false} width={100} isReadOnly={false} visible={false} />
          <FlexGridColumn header="Logo" binding="IconName" allowSorting={false} width={120} isReadOnly={false} visible={false} />

          <FlexGridColumn header="Link To" binding="NavigationItemLogicId" allowSorting={false} width={420} isReadOnly={true}>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => {
                const item = cell.item;
                if (!item || (Array.isArray(item.AppListMenu_List) && item.AppListMenu_List.length > 0)) return null;
                const desc = dictLogicIdAndNavigationItem?.[item.NavigationItemLogicId]?.Description || '';
                return (
                  <div className="w-full h-full px-1 flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => openNavigationSelector(item)}
                      className={`w-7 h-6 rounded-[4px] text-xs ${theme.button_default}`}
                      title="Edit menu link"
                    >
                      <i className="fa-solid fa-pen-to-square" aria-hidden />
                    </button>
                    <span className="text-xs truncate">{desc}</span>
                  </div>
                );
              }}
            />
          </FlexGridColumn>

          <FlexGridColumn
            header="Link Type"
            binding="LinkType"
            allowSorting={false}
            isRequired={false}
            dataMap={appListMenuLinkTypeDataMap}
            width={160}
            visible={isShowAdvancedColumns}
          />
          <FlexGridColumn header="Route Code" binding="RouteCode" allowSorting={false} width={220} visible={isShowAdvancedColumns} />
          <FlexGridColumn header="Link Parameter / URL" binding="Link" allowSorting={false} width={200} visible={isShowAdvancedColumns} />
          <FlexGridColumn
            header="Device Menu Show"
            binding="EmDeviceMenuShowMode"
            allowSorting={false}
            isRequired={false}
            dataMap={deviceMenuShowModeDataMap}
            width={170}
            visible={isShowAdvancedColumns}
          />
          <FlexGridColumn header="Id" binding="Id" width={90} visible={isShowAdvancedColumns} isReadOnly={true} format="d" />
          <FlexGridColumn header="ParentId" binding="ParentId" width={100} visible={isShowAdvancedColumns} format="d" />
          <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
        </FlexGrid>
      </div>

      {rowContextMenu.visible && rowContextMenu.item && (
        <div
          ref={rowContextMenuRef}
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-max`}
          style={{ left: rowContextMenu.x, top: rowContextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {(() => {
            const isRootLevelMenu =
              rowContextMenu.item?.ParentId === null ||
              rowContextMenu.item?.ParentId === undefined ||
              rowContextMenu.item?.ParentId === '';
            const hasChildMenus = Array.isArray(rowContextMenu.item?.AppListMenu_List) && rowContextMenu.item?.AppListMenu_List.length > 0;
            return (
              <>
                <button
                  type="button"
                  className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                  onClick={() => {
                    addMenu(rowContextMenu.item);
                    setRowContextMenu({ visible: false, x: 0, y: 0, item: null });
                  }}
                >
                  <i className="fa-solid fa-sitemap mr-2 flex-shrink-0" aria-hidden /> Add Sub Menu
                </button>
                {!isRootLevelMenu && !hasChildMenus && (
                  <button
                    type="button"
                    className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center whitespace-nowrap`}
                    onClick={() => {
                      deleteMenu(rowContextMenu.item);
                      setRowContextMenu({ visible: false, x: 0, y: 0, item: null });
                    }}
                  >
                    <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete
                  </button>
                )}
              </>
            );
          })()}
        </div>
      )}

      {showNavigationSelector && (
        <div className="fixed inset-0 z-[120] flex items-center justify-center bg-black/30" onClick={() => setShowNavigationSelector(false)}>
          <div className={`w-[900px] h-[780px] max-h-[95vh] max-w-[95vw] rounded-md flex flex-col ${theme.mainContentSection}`} onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between px-3 py-2 border-b">
              <div className={`text-sm font-semibold ${theme.title}`}>Navigation Item Selector</div>
              <button type="button" onClick={() => setShowNavigationSelector(false)} className={`w-8 h-8 rounded ${theme.button_default}`}>&times;</button>
            </div>
            <div className="px-3 pt-2">
              <div className="flex items-center gap-2">
                {['Report & View', 'List Editors', 'Form Folder Navigations', 'Form Search Navigations', 'Form Editor'].map((tab, idx) => (
                  <button
                    key={tab}
                    type="button"
                    onClick={() => setNavigationSelectorActiveTabIndex(idx)}
                    className={`px-2 py-1 text-xs rounded-[4px] ${navigationSelectorActiveTabIndex === idx ? theme.tab_active : theme.button_default}`}
                  >
                    {tab}
                  </button>
                ))}
              </div>
            </div>
            <div className="h-1 flex-auto p-3 overflow-hidden">
              <div className="w-full h-full overflow-hidden">
                {navigationSelectorActiveTabIndex === 0 && (
                  <FlexGrid
                    ref={navSearchGridRef}
                    className="w-full h-full"
                    itemsSource={applicationSearchCV ?? []}
                    selectionMode="Row"
                    isReadOnly={true}
                    allowSorting={false}
                    showGroups={true}
                    groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
                  >
                    <FlexGridColumn binding="Id" header="Id" width={80} format="d" />
                    <FlexGridColumn binding="Name" header="Name" width={280} />
                    <FlexGridColumn binding="Description" header="Description" width={280} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                )}
                {navigationSelectorActiveTabIndex === 1 && (
                  <FlexGrid
                    ref={navListEditGridRef}
                    className="w-full h-full"
                    itemsSource={listEditTransactionCV ?? []}
                    selectionMode="Row"
                    isReadOnly={true}
                    allowSorting={false}
                    showGroups={true}
                    groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
                  >
                    <FlexGridColumn binding="Id" header="Id" width={80} format="d" />
                    <FlexGridColumn binding="TransactionName" header="List Editor" width={320} />
                    <FlexGridColumn binding="Description" header="Description" width={320} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                )}
                {navigationSelectorActiveTabIndex === 2 && (
                  <FlexGrid
                    ref={navFolderGridRef}
                    className="w-full h-full"
                    itemsSource={folderNavigationCV ?? []}
                    selectionMode="Row"
                    isReadOnly={true}
                    allowSorting={false}
                    showGroups={true}
                    groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
                  >
                    <FlexGridColumn binding="TransactionId" header="Id" width={80} format="d" />
                    <FlexGridColumn binding="FolderViewId" header="Folder View" width={260} dataMap={searchViewDataMap || undefined} />
                    <FlexGridColumn binding="TransactionId" header="Form Data Model" width={280} dataMap={transactionDataMap || undefined} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                )}
                {navigationSelectorActiveTabIndex === 3 && (
                  <FlexGrid
                    ref={navSearchNavGridRef}
                    className="w-full h-full"
                    itemsSource={searchNavigationCV ?? []}
                    selectionMode="Row"
                    isReadOnly={true}
                    allowSorting={false}
                    showGroups={true}
                    groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
                  >
                    <FlexGridColumn binding="QuickSearchId" header="Search Id" width={90} format="d" />
                    <FlexGridColumn binding="QuickSearchId" header="Form Search" width={260} dataMap={searchDataMap || undefined} />
                    <FlexGridColumn binding="TransactionId" header="Form Data Model" width={280} dataMap={transactionDataMap || undefined} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                )}
                {navigationSelectorActiveTabIndex === 4 && (
                  <FlexGrid
                    ref={navFormGridRef}
                    className="w-full h-full"
                    itemsSource={masterDetailTransactionCV ?? []}
                    selectionMode="Row"
                    isReadOnly={true}
                    allowSorting={false}
                    showGroups={true}
                    groupHeaderFormat="<span style='font-weight:600;'>{value}</span>"
                  >
                    <FlexGridColumn binding="Id" header="Id" width={90} format="d" />
                    <FlexGridColumn binding="TransactionName" header="Form Data Model" width={300} />
                    <FlexGridColumn binding="Description" header="Description" width={300} />
                    <FlexGridColumn header="" binding="" width="*" />
                  </FlexGrid>
                )}
              </div>
            </div>
            <div className="flex items-center justify-end gap-2 px-3 py-2 border-t">
              <button type="button" onClick={applySelectedNavigation} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-check mr-1" aria-hidden /> Apply
              </button>
              <button type="button" onClick={clearNavigationItem} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-eraser mr-1" aria-hidden /> Clear
              </button>
              <button type="button" onClick={() => setShowNavigationSelector(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Keep these refs/data "used" for parity completeness and advanced mapping */}
      <div className="hidden">{availableNavigationMenuItemsDataMap ? selectedMenu?.Id : ''}</div>
    </div>
  );
};

export default MenuManagement;
