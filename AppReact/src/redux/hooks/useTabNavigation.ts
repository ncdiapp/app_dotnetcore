import { useDispatch, useSelector } from 'react-redux';
import { useNavigate, useLocation } from 'react-router-dom';
import React, { useEffect, useRef, createContext, useContext, useMemo, type ReactNode } from 'react';
import type { RootState } from '../store';
import { store } from '../store';
import { addTab, activateTab, setDataModelToCache, updateTabPath } from '../features/ui/navigation/tabnavSlice';
import {
  buildRoutePathFromParamObj,
  getReactPathForRouteCode,
  getAgentChatSkillKey,
  isAgentChatPath,
  isTransactionFormGroupPath,
  isTransactionFolderNavigationPath,
  isTransactionFormInstancePath,
  resolveTabNavigationPath,
  tabRoutePathsMatch,
} from '../../helper/navigationHelper';

const FILE_MANAGEMENT_DEFAULT_CATEGORY = 3; // My Company

// Tab leave flush: pages register savers keyed by active tabKey (multiple savers per tab OK).
const _dictTabKeyAndSaveToCacheFuncs: Record<string, Map<string, () => void>> = {};

export const registerTabDataSaver = (tabKey: string, saver: () => void, saverId = 'default') => {
  if (!tabKey) return;
  if (!_dictTabKeyAndSaveToCacheFuncs[tabKey]) {
    _dictTabKeyAndSaveToCacheFuncs[tabKey] = new Map();
  }
  _dictTabKeyAndSaveToCacheFuncs[tabKey].set(saverId, saver);
};

export const unregisterTabDataSaver = (tabKey: string, saverId = 'default') => {
  if (!tabKey) return;
  const map = _dictTabKeyAndSaveToCacheFuncs[tabKey];
  if (!map) return;
  map.delete(saverId);
  if (map.size === 0) delete _dictTabKeyAndSaveToCacheFuncs[tabKey];
};

/** Flush the currently active tab's registered savers into tabnav cache (call before switching tabs). */
export const cacheCurrentTabData = () => {
  const currentState = store.getState();
  const activeTab = currentState.tabnav.tabs.find((tab: { isActive?: boolean; tabKey?: string }) => tab.isActive);
  if (!activeTab?.tabKey) return;
  const savers = _dictTabKeyAndSaveToCacheFuncs[activeTab.tabKey];
  if (!savers || savers.size === 0) return;
  savers.forEach((saver) => {
    try {
      saver();
    } catch (e) {
      console.warn('cacheCurrentTabData failed', e);
    }
  });
};

export type TabNavigationApi = {
  addTabAndNavigate: (
    routeBasePath: string,
    label: string,
    paramObj?: any,
    isClosable?: boolean,
  ) => void;
  addTabFromListMenu: (menuDto: any) => void;
};

const noopTabNavigation: TabNavigationApi = {
  addTabAndNavigate: () => {},
  addTabFromListMenu: () => {},
};

const TabNavigationContext = createContext<TabNavigationApi | null>(null);

/**
 * Must be rendered inside `<BrowserRouter>` (and Redux `Provider`).
 * Puts tab navigation on React context so deep children — including
 * `createPortal(..., document.body)` — do not call `useNavigate()` in subtrees
 * where router context can be missing.
 */
export const TabNavigationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const tabs = useSelector((state: RootState) => state.tabnav.tabs);
  const activeTabKey = useSelector((state: RootState) => state.tabnav.activeTabKey);
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const systemDefinedFileTransactionId = userContext?.DictAppSetup?.SystemDefinedFileTransactionId;

  // Keep router URL in sync when active tab changes (tab bar click, close tab, addTab).
  useEffect(() => {
    if (!userContext || userContext.IsLoginFailed) return;
    if (!activeTabKey) return;
    const activeTab = tabs.find((t) => t.tabKey === activeTabKey);
    if (!activeTab) return;
    const targetPath = resolveTabNavigationPath(activeTab);
    if (!targetPath) return;
    const currentFull = `${location.pathname}${location.search || ''}`;
    if (currentFull === targetPath || location.pathname === targetPath) return;
    if (tabRoutePathsMatch(targetPath, currentFull)) return;
    navigate(targetPath);
  }, [activeTabKey, tabs, navigate, location.pathname, location.search, userContext]);

  const value = useMemo((): TabNavigationApi => {
  const activateTabFlushing = (tabKey: string) => {
    cacheCurrentTabData();
    dispatch(activateTab(tabKey));
  };

  const buildParamObjFromListMenu = (menuDto: any) => {
    let paramObj: any = {};
    const routeCode = (menuDto.RouteCode || '').toString().replace(/^\//, '');

    if (routeCode.toLowerCase() === 'root-menu-management') {
        return {};
    }

    if (menuDto.RouteCode === 'MasterDataManagement') {
        paramObj.searchId = menuDto.Link || null;
    }
    else if (menuDto.RouteCode === 'user-editor') {
        paramObj.userId = menuDto.Link || null;
    }
    else if (routeCode === 'file-management') {
        paramObj.defaultCategoryId = menuDto.Link != null ? Number(menuDto.Link) : FILE_MANAGEMENT_DEFAULT_CATEGORY;
    }
    else {
        paramObj.id = menuDto.Link || null;
    }

    return paramObj;
  }

  // Helper function to extract base route path (without parameters / query)
  const extractBaseRoutePath = (fullPath: string): string => {
    if (!fullPath) return '';
    const noQuery = fullPath.split('?')[0];
    
    // Split by '/' and filter empty parts
    const parts = noQuery.split('/').filter(p => p);
    if (parts.length === 0) return '/';
    
    // Check if the last part looks like encoded JSON (starts with %)
    const lastPart = parts[parts.length - 1];
    if (lastPart && lastPart.startsWith('%')) {
      // Remove the last part (encoded parameter)
      return '/' + parts.slice(0, -1).join('/');
    }
    
    return '/' + parts.join('/');
  };

  // Helper function to extract parameters from a route path
  const extractParamsFromPath = (fullPath: string): any => {
    try {
      const parts = fullPath.split('/').filter(p => p);
      if (parts.length === 0) return {};
      
      const lastPart = parts[parts.length - 1];
      if (lastPart && lastPart.startsWith('%')) {
        const decoded = decodeURIComponent(lastPart);
        return JSON.parse(decoded);
      }
    } catch (error) {
      // If parsing fails, return empty object
    }
    return {};
  };

  // Helper function to check if two routes point to the same component
  // Based on routes.tsx: /home and /my-applications both render MyApplications
  const isSameComponent = (path1: string, path2: string): boolean => {
    const base1 = extractBaseRoutePath(path1);
    const base2 = extractBaseRoutePath(path2);
    
    // Check if both paths point to the same component
    // /home and /my-applications both render MyApplications (based on routes.tsx)
    if ((base1 === '/home' && base2 === '/my-applications') || 
        (base1 === '/my-applications' && base2 === '/home')) {
      return true;
    }
    
    // Otherwise, check if base paths match exactly
    return base1 === base2;
  };

  const addTabAndNavigate = (
    routeBasePath: string,
    label: string,   
    paramObj?: any,
    isClosable: boolean = true   
  ) => {
    const reactPath = getReactPathForRouteCode(routeBasePath);
    const normalizedReactPath = reactPath.startsWith('/') ? reactPath : `/${reactPath}`;
    paramObj = paramObj || {};
    const originalParamObj = { ...paramObj };
    const hasMeaningfulParams = Object.keys(originalParamObj).some(
      (key) => originalParamObj[key] != null && originalParamObj[key] !== '',
    );
    paramObj.isNavigatedFromTab = true;
    let routePath = hasMeaningfulParams
      ? buildRoutePathFromParamObj(normalizedReactPath, paramObj)
      : normalizedReactPath;

    const targetBasePath = extractBaseRoutePath(routePath);

    // TransactionFormGroup / folder-nav / agent-chat: each instance gets its own tab
    // (agent-chat is one tab per skillKey, not one tab for all agents).
    const allowBaseReuse =
      !isTransactionFormGroupPath(routePath) &&
      !isTransactionFolderNavigationPath(routePath) &&
      !isAgentChatPath(routePath);

    if (isAgentChatPath(routePath)) {
      const skill = getAgentChatSkillKey(routePath);
      const existingAgent = skill
        ? tabs.find((tab) => isAgentChatPath(tab.path) && getAgentChatSkillKey(tab.path) === skill)
        : undefined;
      if (existingAgent) {
        activateTabFlushing(existingAgent.tabKey);
        navigate(existingAgent.path);
        return;
      }
    }

    // Reuse tab with same base route (e.g. /company-security vs /company-security/%7B...%7D)
    const existingByBase = allowBaseReuse
      ? tabs.find((tab) => extractBaseRoutePath(tab.path) === targetBasePath)
      : undefined;
    if (existingByBase) {
      const isFormRoute = isTransactionFormInstancePath(targetBasePath);
      const existingParams = isFormRoute ? extractParamsFromPath(existingByBase.path) : null;
      const sameFormInstance =
        !isFormRoute ||
        (String(existingParams?.id ?? '') === String(originalParamObj?.id ?? '') &&
          String(existingParams?.param1 ?? '') === String(originalParamObj?.param1 ?? '') &&
          String(originalParamObj?.id ?? '') !== '');
      if (sameFormInstance) {
        activateTabFlushing(existingByBase.tabKey);
        if (existingByBase.path !== routePath) {
          dispatch(updateTabPath({ tabKey: existingByBase.tabKey, path: routePath }));
        }
        navigate(routePath);
        return;
      }
      // Different FormMasterDetail transaction/record — open a new tab below.
    }

    // First, check if a tab with this exact path already exists
    const existingTab = tabs.find(tab => tab.path === routePath);
    if (existingTab) {
      // Tab already exists, just activate it
      activateTabFlushing(existingTab.tabKey);
      navigate(existingTab.path);
      return;
    }
    
    // Do not reuse Home tab for my-applications - always open a separate "My Applications" tab
    const isMyApplications = reactPath === '/my-applications' || reactPath === 'my-applications';
    const homeTab = !isMyApplications ? tabs.find(tab => tab.tabKey === 'home-tab') : null;
    if (homeTab) {
      // Default landing tab (e.g. SysAdmin Company and Users on home-tab)
      if (extractBaseRoutePath(homeTab.path) === targetBasePath) {
        activateTabFlushing(homeTab.tabKey);
        navigate(homeTab.path);
        return;
      }

      // Check if they point to the same component
      if (isSameComponent(routePath, homeTab.path)) {
        // Extract and compare parameters (ignoring isNavigatedFromTab)
        const targetParams = originalParamObj || {};
        const homeParams = extractParamsFromPath(homeTab.path);
        
        // Compare parameters (ignoring isNavigatedFromTab)
        const targetParamsFiltered = { ...targetParams };
        delete targetParamsFiltered.isNavigatedFromTab;
        const homeParamsFiltered = { ...homeParams };
        delete homeParamsFiltered.isNavigatedFromTab;
        
        // Sort keys for comparison
        const targetKeys = Object.keys(targetParamsFiltered).sort();
        const homeKeys = Object.keys(homeParamsFiltered).sort();
        
        if (targetKeys.length === 0 && homeKeys.length === 0) {
          // Both have no parameters, match - activate Home tab but navigate to requested path so URL/content match
          activateTabFlushing(homeTab.tabKey);
          navigate(routePath);
          return;
        }
        
        if (targetKeys.length === homeKeys.length) {
          const paramsMatch = targetKeys.every(key => 
            targetParamsFiltered[key] === homeParamsFiltered[key]
          );
          
          if (paramsMatch) {
            // If target route matches Home tab (same component and same params), activate and show requested path
            activateTabFlushing(homeTab.tabKey);
            navigate(routePath);
            return;
          }
        }
      }
    }
    
    // Add tab to Redux store (flush current tab first so Search/Agent caches survive)
    cacheCurrentTabData();
    dispatch(addTab({
      tabPath: routePath,
      label,
      isClosable      
    }));  
 
    // Always auto-collapse sidebar when opening a new page (following AngularJS behavior)
    //dispatch(collapseSidebar());

    navigate(routePath);
  };

  const addTabFromListMenu = (menuDto: any) => {
    let label = menuDto.Name || '';

    const routeCode = (menuDto.RouteCode || '').toString().trim();
    const isFolderNavigation =
      routeCode === 'FolderNavigation' || routeCode === 'main.FolderNavigation';

    if (isFolderNavigation) {
      const path = '/file-management';
      const link = menuDto.Link != null ? String(menuDto.Link).trim() : '';
      if (!link) {
        addTabAndNavigate(path, label, { defaultCategoryId: FILE_MANAGEMENT_DEFAULT_CATEGORY });
        return;
      }
      let id: string | number | null = null;
      let param1: string | number | null = null;
      if (link.indexOf('_') > 0) {
        const parts = link.split('_');
        id = parts[0] != null && parts[0] !== '' ? parts[0] : null;
        param1 = parts[1] != null && parts[1] !== '' ? parts[1] : null;
      } else {
        id = link;
      }
      const idNum = id != null && id !== '' ? Number(id) : null;
      const isSystemFileManagement =
        systemDefinedFileTransactionId != null &&
        idNum === Number(systemDefinedFileTransactionId);

      if (isSystemFileManagement) {
        addTabAndNavigate('/file-management', label, {
          defaultCategoryId: param1 != null ? Number(param1) : FILE_MANAGEMENT_DEFAULT_CATEGORY,
        });
        return;
      }

      addTabAndNavigate('transaction-folder-navigation', label, {
        transactionId: idNum ?? id,
      });
      return;
    }

    let paramObj = buildParamObjFromListMenu(menuDto);
    addTabAndNavigate(menuDto.RouteCode, label, paramObj);
  };

  return { addTabAndNavigate, addTabFromListMenu };
  }, [dispatch, navigate, tabs, systemDefinedFileTransactionId]);

  return React.createElement(TabNavigationContext.Provider, { value }, children);
};

/** Tab open + navigate; safe anywhere under {@link TabNavigationProvider} (including portals). */
export const useTabNavigation = (): TabNavigationApi => {
  return useContext(TabNavigationContext) ?? noopTabNavigation;
};


// Helper function to create a serializable version of dataModel (reserved)
const _createSerializableDataModel = (dataModel: any): any => {
  if (!dataModel) return null;
  
  const serializableDataModel: any = {};
  
  Object.keys(dataModel).forEach(key => {
    const value = dataModel[key];
    
    // Skip non-serializable objects
    if (value && typeof value === 'object') {
      // Check if it's a Wijmo CollectionView
      if (value.constructor && value.constructor.name === 'CollectionView') {
        // Extract the source data from CollectionView
        serializableDataModel[key] = value.sourceCollection || [];
      }
      // Check if it's a Wijmo DataMap
      else if (value.constructor && value.constructor.name === 'DataMap') {
        // Extract the source data from DataMap
        serializableDataModel[key] = value.sourceCollection || [];
      }
      // For other objects, try to serialize them
      else if (Array.isArray(value)) {
        serializableDataModel[key] = value;
      }
      else {
        try {
          // Try to serialize the object
          JSON.parse(JSON.stringify(value));
          serializableDataModel[key] = value;
        } catch {
          // If it can't be serialized, skip it
          console.warn(`Skipping non-serializable property: ${key}`);
        }
      }
    } else {
      // Primitive values are always serializable
      serializableDataModel[key] = value;
    }
  });
  
  return serializableDataModel;
};

export const useTabDataAutoCache = (
  dataModel: any,
  customDataModelKey?: string,
  enrichBeforeCache?: () => Record<string, any> | null | undefined,
) => {
  const { tabs } = useSelector((state: RootState) => state.tabnav);
  const dispatch = useDispatch();
  const activeTab = tabs.find((tab) => tab.isActive);
  const tabKey = activeTab?.tabKey ?? null;
  const saverId = customDataModelKey || 'page';

  // Use ref so tab-leave flush always writes the latest snapshot.
  const dataRef = useRef({ dataModel, customDataModelKey });
  dataRef.current = { dataModel, customDataModelKey };
  const enrichRef = useRef(enrichBeforeCache);
  enrichRef.current = enrichBeforeCache;

  useEffect(() => {
    if (!tabKey || dataModel == null) {
      return;
    }

    const saveFunction = () => {
      let currentDataModel = dataRef.current.dataModel;
      if (currentDataModel == null) return;
      if (typeof currentDataModel === 'object' && Object.keys(currentDataModel).length === 0) return;

      const enrich = enrichRef.current?.();
      if (enrich && typeof currentDataModel === 'object') {
        currentDataModel = {
          ...currentDataModel,
          ...enrich,
          dictDcuValue: {
            ...(currentDataModel.dictDcuValue ?? {}),
            ...(enrich.dictDcuValue ?? {}),
          },
        };
      }

      const dataModelKey = dataRef.current.customDataModelKey || tabKey;
      dispatch(setDataModelToCache({ dataModelKey, dataModel: currentDataModel }));
    };

    registerTabDataSaver(tabKey, saveFunction, saverId);
    saveFunction();

    return () => {
      try {
        saveFunction();
      } catch {
        /* ignore */
      }
      unregisterTabDataSaver(tabKey, saverId);
    };
  }, [dataModel, customDataModelKey, tabKey, saverId, dispatch]);
}; 

