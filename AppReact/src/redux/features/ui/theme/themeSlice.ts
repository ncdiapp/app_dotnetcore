import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import lightTheme from './defaultTheme/light.json';
import darkTheme from './defaultTheme/dark.json';
import { loadUserDefinedThemes, applyThirdPartCssOverrides, buildRuntimeTheme } from '../../../../helper/themeHelper';
import type { BaseTheme, ThemeOption, ThemeState } from './types';

// Types moved to ./types


// Built-in themes
export const builtInThemes: ThemeOption[] = [
  {
    id: 'light',
    name: 'Light',
    theme: lightTheme
  },
  {
    id: 'dark',
    name: 'Dark',
    theme: darkTheme
  }
];

// Combine built-in themes with user-defined themes
const getAllAvailableThemes = (): ThemeOption[] => {
  const userThemes = loadUserDefinedThemes();
  return [...builtInThemes, ...userThemes];
};



const initialState: ThemeState = {
  currentTheme: buildRuntimeTheme(lightTheme),
  availableThemes: getAllAvailableThemes(),
  currentThemeId: 'light',
};

// Apply initial Wijmo theme
if (typeof window !== 'undefined') {
  applyThirdPartCssOverrides(initialState.currentTheme.param);
}

const themeSlice = createSlice({
  name: 'theme',
  initialState,
  reducers: {
    setThemeById: (state, action: PayloadAction<string>) => {
      const themeFound = state.availableThemes.find(t => t.id === action.payload);
      if (themeFound) {
        state.currentTheme = buildRuntimeTheme(themeFound.theme);
        state.currentThemeId = themeFound.id;
        
        if (typeof window !== 'undefined') {
          applyThirdPartCssOverrides(themeFound.theme);
        }
      }
    },
    setTheme: (state, action: PayloadAction<BaseTheme>) => {
      state.currentTheme = buildRuntimeTheme(action.payload);
      if (typeof window !== 'undefined') {
        applyThirdPartCssOverrides(action.payload);
      }
    },
    setAvailableThemes: (state, action: PayloadAction<ThemeOption[]>) => {
      state.availableThemes = action.payload;
      const currentExists = action.payload.find(theme => theme.id === state.currentThemeId);
      if (!currentExists) {
        const fallback = action.payload.find(theme => theme.id === 'light') ?? action.payload[0];
        if (fallback) {
          state.currentThemeId = fallback.id;
          state.currentTheme = buildRuntimeTheme(fallback.theme);
          if (typeof window !== 'undefined') {
            applyThirdPartCssOverrides(fallback.theme);
          }
        }
      }
    },
  },
});

export const { setThemeById, setTheme, setAvailableThemes } = themeSlice.actions;
export default themeSlice.reducer;
export type { BaseTheme, Theme, ThemeOption, ThemeClasses } from './types';