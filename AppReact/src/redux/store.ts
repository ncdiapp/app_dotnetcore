import { configureStore, Middleware } from '@reduxjs/toolkit';
import { rootReducer } from './rootReducer';

// Track if we're currently restoring tabs to prevent immediate overwrite
let isRestoringTabs = false;
let restoreTabsTimeout: NodeJS.Timeout | null = null;

// Track if tabs have been restored at least once (to prevent saving before initial restore)
let hasRestoredTabsOnce = false;

// Middleware to persist tabs to localStorage
const tabsPersistenceMiddleware: Middleware = (store) => (next) => (action: any) => {
  // Check if this is a restoreTabs action
  if (action.type === 'tabnav/restoreTabs') {
    isRestoringTabs = true;
    hasRestoredTabsOnce = true; // Mark that we've restored tabs at least once
    // Clear any existing timeout
    if (restoreTabsTimeout) {
      clearTimeout(restoreTabsTimeout);
    }
    const result = next(action);
    
    // Reset flag after a delay to allow restore and navigation to complete
    // Use a longer delay to ensure all navigation and component mounting completes
    restoreTabsTimeout = setTimeout(() => {
      isRestoringTabs = false;
      restoreTabsTimeout = null;
    }, 3000); // Increase to 3 seconds to ensure all navigation completes
    return result;
  }
  
  const result = next(action);
  
  // Save tabs to localStorage after any tabnav action (except restoreTabs and cache-only actions)
  // Also skip saving during the restore window and before initial restore
  // Exclude actions that only modify cache data, not tab structure
  const cacheOnlyActions = [
    'tabnav/setDataModelToCache',
    'tabnav/setCurrentTabDataToCache'
  ];
  
  // Only save if:
  // 1. It's a tabnav action
  // 2. We're not currently restoring tabs
  // 3. We've restored tabs at least once (to prevent saving before initial restore)
  // 4. It's not a cache-only action
  if (action.type?.startsWith('tabnav/') && !isRestoringTabs && hasRestoredTabsOnce && !cacheOnlyActions.includes(action.type)) {
    const state = store.getState();
    const tabnavState = state.tabnav;
    
    try {
      // Save tabs and activeTabKey for multi-tab state
      const stateToSave = {
        tabs: tabnavState.tabs,
        activeTabKey: tabnavState.activeTabKey,
      };
      localStorage.setItem('tabsState', JSON.stringify(stateToSave));
    } catch (error) {
      console.error('Failed to save tabs to localStorage:', error);
    }
  }
  
  return result;
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // Ignore these action types
        ignoredActions: ['tabnav/setDataModelToCache'],
        //// Ignore these field paths in all actions
        //ignoredActionPaths: ['payload.dataModel.userDataCV', 'payload.dataModel.languageDataMap'],
        // Ignore these paths in the state
        ignoredPaths: ['tabnav.dictDataModelKeyAndCachedData'],
      },
    }).concat(tabsPersistenceMiddleware),
  devTools: process.env.NODE_ENV !== 'production',
});

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
