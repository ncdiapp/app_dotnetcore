import { createSlice } from '@reduxjs/toolkit';

interface BusyLoaderState {
  isBusy: boolean;
}

const initialState: BusyLoaderState = {
  isBusy: false
};

export const busyLoaderSlice = createSlice({
  name: 'busyLoader',
  initialState,
  reducers: {
    setIsBusy: (state) => {
      state.isBusy = true;
    },
    setIsNotBusy: (state) => {
      state.isBusy = false;
    },    
  }
});

export const { setIsBusy, setIsNotBusy } = busyLoaderSlice.actions;
export default busyLoaderSlice.reducer; 