import { combineReducers } from '@reduxjs/toolkit';
import counterReducer from './features/example/counterSlice';
import userSessionReducer from './features/admin/userSessionSlice';
import busyLoaderReducer from './features/ui/feedback/busyLoaderSlice';
import themeReducer from './features/ui/theme/themeSlice';
import tabnavReducer from './features/ui/navigation/tabnavSlice';
import errorMessageReducer from './features/ui/feedback/errorMessageSlice';
import sidebarReducer from './features/ui/navigation/sidebarSlice';
import tenantBrandingReducer from './features/ui/tenantBrandingSlice';

export const rootReducer = combineReducers({
  counter: counterReducer,
  busyLoader: busyLoaderReducer,
  theme: themeReducer,
  userSession: userSessionReducer,
  tabnav: tabnavReducer,
  errorMessage: errorMessageReducer,
  sidebar: sidebarReducer,
  tenantBranding: tenantBrandingReducer,
}); 