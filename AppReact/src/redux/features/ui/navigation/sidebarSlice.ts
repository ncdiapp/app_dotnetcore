import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface SidebarState {
  isCollapsed: boolean;
  autoCollapseOnPageOpen: boolean; // Setting to auto-collapse when opening/clicking a page
}

const initialState: SidebarState = {
  isCollapsed: false,
  autoCollapseOnPageOpen: true, // Default to true to match AngularJS behavior (can be overridden by app setup)
};

const sidebarSlice = createSlice({
  name: 'sidebar',
  initialState,
  reducers: {
    toggleSidebar: (state) => {
      state.isCollapsed = !state.isCollapsed;
    },
    setSidebarCollapsed: (state, action: PayloadAction<boolean>) => {
      state.isCollapsed = action.payload;
    },
    setAutoCollapseOnPageOpen: (state, action: PayloadAction<boolean>) => {
      state.autoCollapseOnPageOpen = action.payload;
    },
    collapseSidebar: (state) => {
      state.isCollapsed = true;
    },
    expandSidebar: (state) => {
      state.isCollapsed = false;
    },
  },
});

export const { 
  toggleSidebar, 
  setSidebarCollapsed, 
  setAutoCollapseOnPageOpen,
  collapseSidebar,
  expandSidebar 
} = sidebarSlice.actions;

export default sidebarSlice.reducer;

