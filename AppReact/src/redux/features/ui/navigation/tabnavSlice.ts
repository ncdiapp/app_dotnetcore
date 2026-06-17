import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { store } from '../../../store';
import { canUpdateTabPath, isTransactionFormGroupPath } from '../../../../helper/navigationHelper';

function repairAllTabPaths(tabs: Tab[]) {
  tabs.forEach((tab) => {
    // Never snapshot a corrupted TransactionFormGroup path as initialPath.
    if (!tab.initialPath && !isTransactionFormGroupPath(tab.path)) {
      tab.initialPath = tab.path;
    }
    if (tab.tabKey === 'home-tab') {
      if (isTransactionFormGroupPath(tab.path)) {
        tab.path = '/home';
      }
      tab.initialPath = '/home';
      return;
    }
    if (
      isTransactionFormGroupPath(tab.path) &&
      tab.initialPath &&
      !isTransactionFormGroupPath(tab.initialPath)
    ) {
      tab.path = tab.initialPath;
    }
  });
}

// Helper function to generate GUID
const generateGuid = (): string => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : ((r & 0x3) | 0x8);
    return v.toString(16);
  });
};

export interface Tab {
  label: string;
  path: string;
  /** Original path at tab creation; used to repair accidental cross-route overwrites. */
  initialPath?: string;
  isActive: boolean;
  isClosable?: boolean; 
  tabKey: string; // GUID for caching
}

interface TabState {
  tabs: Tab[];
  activeTabKey: string | null;
  previousActiveTabKey: string | null;
  dictDataModelKeyAndCachedData: Record<string, any>;
}

// Helper function to save tabs to localStorage (reserved for persistence)
const _saveTabsToStorage = (state: TabState) => {
  try {
    // Only save tabs and activeTabKey, not cached data
    const stateToSave = {
      tabs: state.tabs,
      activeTabKey: state.activeTabKey,
    };
    localStorage.setItem('tabsState', JSON.stringify(stateToSave));
  } catch (error) {
    console.error('Failed to save tabs to localStorage:', error);
  }
};

const initialState: TabState = {
  tabs: [
    {
      label: 'Home',
      path: '/home',
      initialPath: '/home',
      isActive: true,
      isClosable: false,
      tabKey: 'home-tab'
    }
  ],
  activeTabKey: 'home-tab',
  previousActiveTabKey: null,
  dictDataModelKeyAndCachedData: {},
};

const tabnavSlice = createSlice({
  name: 'tabnav',
  initialState,
  reducers: {
    addTab: (state, action: PayloadAction<{
      tabPath: string;
      label: string; 
      isClosable?: boolean;
    }>) => {
      const { tabPath, label, isClosable = true } = action.payload;
    
      // Check if a tab with this exact path already exists
      const existingTab = state.tabs.find(tab => tab.path === tabPath);
      if (existingTab) {
        if (state.activeTabKey && state.activeTabKey !== existingTab.tabKey) {
          state.previousActiveTabKey = state.activeTabKey;
        }
        // Tab already exists, just activate it without modifying the tabs array
        state.tabs.forEach(tab => {
          tab.isActive = tab.tabKey === existingTab.tabKey;
        });
        state.activeTabKey = existingTab.tabKey;
        repairAllTabPaths(state.tabs);
        return; // Early return to prevent any further modifications
      }
      
      // No existing tab found, add a new one
      if (state.activeTabKey) {
        state.previousActiveTabKey = state.activeTabKey;
      }
      state.tabs.forEach(tab => {
        tab.isActive = false;
      });
   
      // Generate GUID for new tab
      const tabKey = generateGuid();

      // Add new tab
      const newTab: Tab = {
        label,
        path: tabPath,
        initialPath: tabPath,
        isActive: true,
        isClosable,         
        tabKey
      };

      state.tabs.push(newTab);
      state.activeTabKey = tabKey;
      repairAllTabPaths(state.tabs);
    },

    activateTab: (state, action: PayloadAction<string>) => {
      const tabKey = action.payload;
      if (state.activeTabKey && state.activeTabKey !== tabKey) {
        state.previousActiveTabKey = state.activeTabKey;
      }

      // Deactivate all tabs
      state.tabs.forEach(tab => {
        tab.isActive = tab.tabKey === tabKey;
      });

      state.activeTabKey = tabKey;
      repairAllTabPaths(state.tabs);
    },

    closeTab: (state, action: PayloadAction<string>) => {
      const tabKey = action.payload;
      const tabIndex = state.tabs.findIndex(tab => tab.tabKey === tabKey);

      if (tabIndex >= 0) {
        const isActiveTab = state.tabs[tabIndex].isActive;
        const isClosable = state.tabs[tabIndex].isClosable !== false; // Default to true

        // Prevent closing the last tab if it's the Home tab
        if (state.tabs.length === 1 && (state.tabs[0].path === '/home' || state.tabs[0].path === '/company-security')) {
          return;
        }

        if (isClosable) {
          // Remove cached data for this tab
          const tabKey = state.tabs[tabIndex].tabKey;
          if (tabKey && state.dictDataModelKeyAndCachedData[tabKey]) {
            delete state.dictDataModelKeyAndCachedData[tabKey];
          }

          state.tabs.splice(tabIndex, 1);

          repairAllTabPaths(state.tabs);

          // If we closed the active tab, activate the previously-active tab when possible,
          // otherwise fall back to the existing behavior (activate the last tab).
          if (isActiveTab && state.tabs.length > 0) {
            let nextActiveTabKey: string | null = null;

            if (state.previousActiveTabKey) {
              const prevStillExists = state.tabs.some(t => t.tabKey === state.previousActiveTabKey);
              if (prevStillExists) {
                nextActiveTabKey = state.previousActiveTabKey;
              }
            }

            if (!nextActiveTabKey) {
              nextActiveTabKey = state.tabs[state.tabs.length - 1]?.tabKey || null;
            }

            state.tabs.forEach(t => {
              t.isActive = t.tabKey === nextActiveTabKey;
            });
            state.activeTabKey = nextActiveTabKey;
          }
        }
      }
    },

    updateCurrentTabLabel: (state, action: PayloadAction<string>) => {
      let label = action.payload;
      if (label) {
        const activeTab = state.tabs.find(tab => tab.isActive);
        if (activeTab) {
          activeTab.label = label;
        }
      }
    },

    /** Replace active tab path (e.g. after new FormMasterDetail save assigns param1 / root PK). */
    updateActiveTabPath: (state, action: PayloadAction<string>) => {
      const newPath = action.payload;
      if (!newPath) return;
      const activeTab = state.tabs.find(tab => tab.isActive);
      if (activeTab && canUpdateTabPath(activeTab.path, newPath)) {
        activeTab.path = newPath;
      }
    },

    /** Replace a specific tab path (avoids corrupting another tab when isActive is out of sync). */
    updateTabPath: (state, action: PayloadAction<{ tabKey: string; path: string }>) => {
      const { tabKey, path } = action.payload;
      if (!tabKey || !path) return;
      const tab = state.tabs.find((t) => t.tabKey === tabKey);
      if (tab && canUpdateTabPath(tab.path, path)) {
        tab.path = path;
      }
    },

    /** Snapshot a tab's route before opening TransactionFormGroup from Search (prevents path corruption). */
    preserveTabInitialPath: (state, action: PayloadAction<{ tabKey: string; path: string }>) => {
      const { tabKey, path } = action.payload;
      if (!tabKey || !path || isTransactionFormGroupPath(path)) return;
      const tab = state.tabs.find((t) => t.tabKey === tabKey);
      if (!tab) return;
      if (!tab.initialPath || isTransactionFormGroupPath(tab.initialPath)) {
        tab.initialPath = path;
      }
      if (isTransactionFormGroupPath(tab.path)) {
        tab.path = path;
      }
    },

    setCurrentTabDataToCache: (state, action: PayloadAction<{ dataModel: any }>) => {
      const { dataModel } = action.payload;
      const activeTab = state.tabs.find(tab => tab.isActive);

      if (activeTab && activeTab.tabKey) {
        state.dictDataModelKeyAndCachedData[activeTab.tabKey] = dataModel;
      }
    },

    setDataModelToCache: (state, action: PayloadAction<{ dataModelKey: string, dataModel: any }>) => {
      const { dataModelKey, dataModel } = action.payload;

      if (dataModelKey) {
        state.dictDataModelKeyAndCachedData[dataModelKey] = dataModel;
      }
    },

    restoreTabs: (state, action: PayloadAction<{ tabs: Tab[], activeTabKey: string | null }>) => {
      const { tabs, activeTabKey } = action.payload;
      
      if (tabs && Array.isArray(tabs) && tabs.length > 0) {
        // Create a new array to ensure immutability
        const restoredTabs = tabs.map(tab => ({ ...tab }));
        
        // Determine which tab should be active
        const targetActiveTabKey = activeTabKey || tabs.find(t => t.isActive)?.tabKey || tabs[0]?.tabKey || 'home-tab';
        
        // Set isActive for all tabs based on targetActiveTabKey
        restoredTabs.forEach(tab => {
          tab.isActive = tab.tabKey === targetActiveTabKey;
        });
        repairAllTabPaths(restoredTabs);

        // IMPORTANT: Replace the entire tabs array, don't just modify it
        // This ensures we restore ALL tabs, not just add to existing ones
        state.tabs = restoredTabs;
        state.activeTabKey = targetActiveTabKey;
        state.previousActiveTabKey = null;
      }
    },
    resetTabs: (state, action: PayloadAction<{ path?: string; label?: string } | undefined>) => {
      const path = action.payload?.path ?? '/home';
      const label = action.payload?.label ?? 'Home';
      state.tabs = [
        {
          label,
          path,
          initialPath: path,
          isActive: true,
          isClosable: false,
          tabKey: 'home-tab',
        },
      ];
      state.activeTabKey = 'home-tab';
      state.previousActiveTabKey = null;
      state.dictDataModelKeyAndCachedData = {};
    },
    clearTabsForLogout: (state) => {
      state.tabs = [];
      state.activeTabKey = null;
      state.previousActiveTabKey = null;
      state.dictDataModelKeyAndCachedData = {};
    },
  }
});

export const { addTab, activateTab, closeTab, updateCurrentTabLabel, updateActiveTabPath, updateTabPath, preserveTabInitialPath, setCurrentTabDataToCache, setDataModelToCache, restoreTabs, resetTabs, clearTabsForLogout } = tabnavSlice.actions;

// export const getDataModelFromCache = (dataModelKey: string) => {
//   const currentState = store.getState();
//   const activeTab = currentState.tabnav.tabs.find(tab => tab.isActive);

//   if (activeTab && activeTab.tabKey) {
//     return currentState.tabnav.dictDataModelKeyAndCachedData[activeTab.tabKey] || null;
//   }

//   return null;
// };

export const getCurrentActiveTab = () => {
  const currentState = store.getState();
  const activeTab = currentState.tabnav.tabs.find((tab: Tab) => tab.isActive) || null;
  return activeTab;
};

export const getDataModelFromCache = (dataModelKey: string) => {
  if (dataModelKey) {
    const currentState = store.getState();
    return currentState.tabnav.dictDataModelKeyAndCachedData[dataModelKey] || null;
  }

  return null;
};

export default tabnavSlice.reducer; 