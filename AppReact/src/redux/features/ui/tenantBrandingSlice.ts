import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface TenantBrandingState {
  isFound: boolean;
  companyName: string | null;
  domainToken: string | null;
  customDomain: string | null;
}

const initialState: TenantBrandingState = {
  isFound: false,
  companyName: null,
  domainToken: null,
  customDomain: null,
};

const tenantBrandingSlice = createSlice({
  name: 'tenantBranding',
  initialState,
  reducers: {
    setTenantBranding: (state, action: PayloadAction<TenantBrandingState>) => {
      state.isFound = action.payload.isFound;
      state.companyName = action.payload.companyName ?? null;
      state.domainToken = action.payload.domainToken ?? null;
      state.customDomain = action.payload.customDomain ?? null;
    },
  },
});

export const { setTenantBranding } = tenantBrandingSlice.actions;
export default tenantBrandingSlice.reducer;
export type { TenantBrandingState };
