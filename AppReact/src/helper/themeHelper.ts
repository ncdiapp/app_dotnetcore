import type { BaseTheme, Theme, ThemeClasses, ThemeOption } from '../redux/features/ui/theme/types';
import { adminSvc } from '../webapi/adminsvc';

// Function to format theme name from filename (reserved)
const _formatThemeName = (filename: string): string => {
  // Remove .json extension
  const nameWithoutExt = filename.replace('.json', '');
  // Replace hyphens with spaces and capitalize each word
  return nameWithoutExt
    .split('-')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
};

// BaseTheme keys for normalization from server DTO DictThemeDetails
const baseThemeKeys: (keyof BaseTheme)[] = [
  'scrollbar_style',
  'bg_default','text_default','border_default',
  'bg_default_hover','text_default_hover','border_default_hover',
  'bg_default_active','text_default_active','border_default_active',
  'bg_header','text_header','border_header',
  'bg_mainContent','bg_mainContentSection','text_mainContentSection','border_mainContentSection',
  'text_title',
  'bg_sidebar','text_sidebar','border_sidebar',
  'bg_sidebar_menu_hover','text_sidebar_menu_hover','bg_sidebar_menu_active','text_sidebar_menu_active',
  'bg_input_box','bg_input_readonly','text_input_box','border_input_box','text_button_inputBox',
  'logo_filter',
  'bg_tab','text_tab','border_tab','bg_tab_hover','text_tab_hover','border_tab_hover',
  'bg_tab_active','text_tab_active','border_tab_active',
  'bg_modalBackdrop','bg_modalHeader','text_modalHeader','border_modalHeader',
  'bg_button_default','bg_button_default_hover','text_button_default','text_button_default_hover','border_button_default','border_button_default_hover',
  'bg_button_secondary','bg_button_secondary_hover','text_button_secondary','text_button_secondary_hover','border_button_secondary','border_button_secondary_hover',
  'bg_menu_divider',
  'bg_menu_default','bg_menu_default_hover','text_menu_default','text_menu_default_hover','border_menu_default','border_menu_default_hover',
  'bg_menu_secondary','bg_menu_secondary_hover','text_menu_secondary','text_menu_secondary_hover','border_menu_secondary','border_menu_secondary_hover',
  'bg_contextMenu','text_contextMenu','bg_contextMenu_hover','text_contextMenu_hover',
  'border_desktop_widget',
  'bg_label','text_label','border_label',
  'wijmo_grid_outer_border_color','wijmo_grid_row_border_color','wijmo_grid_column_border_color','wijmo_grid_header_background_color','wijmo_grid_header_border_color','wijmo_grid_row_background_color','wijmo_grid_row_alt_background_color','wijmo_grid_selected_row_background_color','wijmo_grid_selected_row_text_color','wijmo_grid_selected_cell_background_color','wijmo_grid_selected_cell_text_color','wijmo_grid_header_text_color','wijmo_grid_default_text_color','wijmo_grid_default_background_color','wijmo_grid_default_font_size','wijmo_grid_footer_background_color','wijmo_grid_footer_text_color','wijmo_grid_container_border_color','wijmo_grid_grouppanel_background_color','wijmo_grid_grouppanel_text_color','wijmo_grid_treeView_selectedRow_background_color','wijmo_grid_treeView_selectedRow_text_color'
];

const dictToBaseTheme = (dict: Record<string, string> | null | undefined): BaseTheme => {
  const src = dict || {};
  const result: any = {};
  baseThemeKeys.forEach(k => {
    result[k] = (src as any)[k] ?? '';
  });
  return result as BaseTheme;
};

const dtoToThemeOption = (dto: any): ThemeOption => ({
  id: String(dto?.Id ?? ''),
  name: String(dto?.ThemeName ?? ''),
  theme: dictToBaseTheme(dto?.DictThemeDetails as Record<string, string> | undefined)
});

const _baseThemeToDict = (theme: BaseTheme): Record<string, string> => {
  const dict: any = {};
  baseThemeKeys.forEach(k => { dict[k] = (theme as any)[k] ?? ''; });
  return dict as Record<string, string>;
};

// Synchronous accessor used at boot: read cached user themes from sessionStorage
export const loadUserDefinedThemes = (): ThemeOption[] => {
  try {
    const cached = typeof window !== 'undefined' ? sessionStorage.getItem('app-user-themes') : null;
    if (!cached) return [];
    const arr = JSON.parse(cached) as any[];
    return arr.map(dtoToThemeOption);
  } catch {
    return [];
  }
};

// Async fetch from server and refresh cache
export const refreshUserDefinedThemesCache = async (): Promise<ThemeOption[]> => {
  const list = await adminSvc.retrieveAvailableUserDefineThemeDtoList();
  if (typeof window !== 'undefined') {
    try { sessionStorage.setItem('app-user-themes', JSON.stringify(list)); } catch {}
  }
  return (Array.isArray(list) ? list : []).map(dtoToThemeOption);
};

// CRUD helpers
export const retrieveOneUserDefinedTheme = async (themeId: number | null): Promise<ThemeOption | null> => {
  const dto = await adminSvc.retrieveOneAppUserDefineThemeDto(themeId);
  if (!dto) return null;
  return dtoToThemeOption(dto);
};

export const saveOneUserDefinedTheme = async (themeDto: any): Promise<any> => {  
  return adminSvc.saveOneAppUserDefineThemeDto(themeDto);
};

export const deleteOneUserDefinedTheme = async (themeId: number | null): Promise<any> => {
  return adminSvc.deleteOneAppUserDefineTheme(themeId);
};

// Function to get theme by ID
export const getThemeById = (themes: ThemeOption[], id: string): ThemeOption | undefined => {
  return themes.find(theme => theme.id === id);
};

// Function to convert Tailwind color format to hex format
const convertTailwindToHex = (tailwindColor: string): string => {
  if (!tailwindColor) return '#000000';
  
  // If it's already in hex format with brackets like "[#000000]", extract the hex
  if (tailwindColor.startsWith('[') && tailwindColor.endsWith(']')) {
    const hexMatch = tailwindColor.match(/#[0-9a-fA-F]{6}/);
    if (hexMatch) return hexMatch[0];
  }
  
  // If it's already a hex color, return as is
  if (tailwindColor.startsWith('#')) {
    return tailwindColor;
  }
  
  // Convert common Tailwind colors to hex
  const tailwindToHex: { [key: string]: string } = {
    'gray-50': '#f9fafb',
    'gray-100': '#f3f4f6',
    'gray-200': '#e5e7eb',
    'gray-300': '#d1d5db',
    'gray-400': '#9ca3af',
    'gray-500': '#6b7280',
    'gray-600': '#4b5563',
    'gray-700': '#374151',
    'gray-800': '#1f2937',
    'gray-900': '#111827',
    'slate-50': '#f8fafc',
    'slate-100': '#f1f5f9',
    'slate-200': '#e2e8f0',
    'slate-300': '#cbd5e1',
    'slate-400': '#94a3b8',
    'slate-500': '#64748b',
    'slate-600': '#475569',
    'slate-700': '#334155',
    'slate-800': '#1e293b',
    'slate-900': '#0f172a',
    'zinc-50': '#fafafa',
    'zinc-100': '#f4f4f5',
    'zinc-200': '#e4e4e7',
    'zinc-300': '#d4d4d8',
    'zinc-400': '#a1a1aa',
    'zinc-500': '#71717a',
    'zinc-600': '#52525b',
    'zinc-700': '#3f3f46',
    'zinc-800': '#27272a',
    'zinc-900': '#18181b',
    'neutral-50': '#fafafa',
    'neutral-100': '#f5f5f5',
    'neutral-200': '#e5e5e5',
    'neutral-300': '#d4d4d4',
    'neutral-400': '#a3a3a3',
    'neutral-500': '#737373',
    'neutral-600': '#525252',
    'neutral-700': '#404040',
    'neutral-800': '#262626',
    'neutral-900': '#171717',
    'stone-50': '#fafaf9',
    'stone-100': '#f5f5f4',
    'stone-200': '#e7e5e4',
    'stone-300': '#d6d3d1',
    'stone-400': '#a8a29e',
    'stone-500': '#78716c',
    'stone-600': '#57534e',
    'stone-700': '#44403c',
    'stone-800': '#292524',
    'stone-900': '#1c1917',
    'red-50': '#fef2f2',
    'red-100': '#fee2e2',
    'red-200': '#fecaca',
    'red-300': '#fca5a5',
    'red-400': '#f87171',
    'red-500': '#ef4444',
    'red-600': '#dc2626',
    'red-700': '#b91c1c',
    'red-800': '#991b1b',
    'red-900': '#7f1d1d',
    'orange-50': '#fff7ed',
    'orange-100': '#ffedd5',
    'orange-200': '#fed7aa',
    'orange-300': '#fdba74',
    'orange-400': '#fb923c',
    'orange-500': '#f97316',
    'orange-600': '#ea580c',
    'orange-700': '#c2410c',
    'orange-800': '#9a3412',
    'orange-900': '#7c2d12',
    'amber-50': '#fffbeb',
    'amber-100': '#fef3c7',
    'amber-200': '#fde68a',
    'amber-300': '#fcd34d',
    'amber-400': '#fbbf24',
    'amber-500': '#f59e0b',
    'amber-600': '#d97706',
    'amber-700': '#b45309',
    'amber-800': '#92400e',
    'amber-900': '#78350f',
    'yellow-50': '#fefce8',
    'yellow-100': '#fef9c3',
    'yellow-200': '#fef08a',
    'yellow-300': '#fde047',
    'yellow-400': '#facc15',
    'yellow-500': '#eab308',
    'yellow-600': '#ca8a04',
    'yellow-700': '#a16207',
    'yellow-800': '#854d0e',
    'yellow-900': '#713f12',
    'lime-50': '#f7fee7',
    'lime-100': '#ecfccb',
    'lime-200': '#d9f99d',
    'lime-300': '#bef264',
    'lime-400': '#a3e635',
    'lime-500': '#84cc16',
    'lime-600': '#65a30d',
    'lime-700': '#4d7c0f',
    'lime-800': '#3f6212',
    'lime-900': '#365314',
    'green-50': '#f0fdf4',
    'green-100': '#dcfce7',
    'green-200': '#bbf7d0',
    'green-300': '#86efac',
    'green-400': '#4ade80',
    'green-500': '#22c55e',
    'green-600': '#16a34a',
    'green-700': '#15803d',
    'green-800': '#166534',
    'green-900': '#14532d',
    'emerald-50': '#ecfdf5',
    'emerald-100': '#d1fae5',
    'emerald-200': '#a7f3d0',
    'emerald-300': '#6ee7b7',
    'emerald-400': '#34d399',
    'emerald-500': '#10b981',
    'emerald-600': '#059669',
    'emerald-700': '#047857',
    'emerald-800': '#065f46',
    'emerald-900': '#064e3b',
    'teal-50': '#f0fdfa',
    'teal-100': '#ccfbf1',
    'teal-200': '#99f6e4',
    'teal-300': '#5eead4',
    'teal-400': '#2dd4bf',
    'teal-500': '#14b8a6',
    'teal-600': '#0d9488',
    'teal-700': '#0f766e',
    'teal-800': '#115e59',
    'teal-900': '#134e4a',
    'cyan-50': '#ecfeff',
    'cyan-100': '#cffafe',
    'cyan-200': '#a5f3fc',
    'cyan-300': '#67e8f9',
    'cyan-400': '#22d3ee',
    'cyan-500': '#06b6d4',
    'cyan-600': '#0891b2',
    'cyan-700': '#0e7490',
    'cyan-800': '#155e75',
    'cyan-900': '#164e63',
    'sky-50': '#f0f9ff',
    'sky-100': '#e0f2fe',
    'sky-200': '#bae6fd',
    'sky-300': '#7dd3fc',
    'sky-400': '#38bdf8',
    'sky-500': '#0ea5e9',
    'sky-600': '#0284c7',
    'sky-700': '#0369a1',
    'sky-800': '#075985',
    'sky-900': '#0c4a6e',
    'blue-50': '#eff6ff',
    'blue-100': '#dbeafe',
    'blue-200': '#bfdbfe',
    'blue-300': '#93c5fd',
    'blue-400': '#60a5fa',
    'blue-500': '#3b82f6',
    'blue-600': '#2563eb',
    'blue-700': '#1d4ed8',
    'blue-800': '#1e40af',
    'blue-900': '#1e3a8a',
    'indigo-50': '#eef2ff',
    'indigo-100': '#e0e7ff',
    'indigo-200': '#c7d2fe',
    'indigo-300': '#a5b4fc',
    'indigo-400': '#818cf8',
    'indigo-500': '#6366f1',
    'indigo-600': '#4f46e5',
    'indigo-700': '#4338ca',
    'indigo-800': '#3730a3',
    'indigo-900': '#312e81',
    'violet-50': '#f5f3ff',
    'violet-100': '#ede9fe',
    'violet-200': '#ddd6fe',
    'violet-300': '#c4b5fd',
    'violet-400': '#a78bfa',
    'violet-500': '#8b5cf6',
    'violet-600': '#7c3aed',
    'violet-700': '#6d28d9',
    'violet-800': '#5b21b6',
    'violet-900': '#4c1d95',
    'purple-50': '#faf5ff',
    'purple-100': '#f3e8ff',
    'purple-200': '#e9d5ff',
    'purple-300': '#d8b4fe',
    'purple-400': '#c084fc',
    'purple-500': '#a855f7',
    'purple-600': '#9333ea',
    'purple-700': '#7c3aed',
    'purple-800': '#6b21a8',
    'purple-900': '#581c87',
    'fuchsia-50': '#fdf4ff',
    'fuchsia-100': '#fae8ff',
    'fuchsia-200': '#f5d0fe',
    'fuchsia-300': '#f0abfc',
    'fuchsia-400': '#e879f9',
    'fuchsia-500': '#d946ef',
    'fuchsia-600': '#c026d3',
    'fuchsia-700': '#a21caf',
    'fuchsia-800': '#86198f',
    'fuchsia-900': '#701a75',
    'pink-50': '#fdf2f8',
    'pink-100': '#fce7f3',
    'pink-200': '#fbcfe8',
    'pink-300': '#f9a8d4',
    'pink-400': '#f472b6',
    'pink-500': '#ec4899',
    'pink-600': '#db2777',
    'pink-700': '#be185d',
    'pink-800': '#9d174d',
    'pink-900': '#831843',
    'rose-50': '#fff1f2',
    'rose-100': '#ffe4e6',
    'rose-200': '#fecdd3',
    'rose-300': '#fda4af',
    'rose-400': '#fb7185',
    'rose-500': '#f43f5e',
    'rose-600': '#e11d48',
    'rose-700': '#be123c',
    'rose-800': '#9f1239',
    'rose-900': '#881337',
    'white': '#ffffff',
    'black': '#000000',
    'transparent': 'transparent'
  };
  
  return tailwindToHex[tailwindColor] || '#000000';
};

// Normalize a token to a Tailwind class color segment.
// Accepts Tailwind token (e.g., "slate-500") or hex (e.g., "#0162e8" or "[#0162e8]").
// Returns a segment suitable for class concatenation: "slate-500" or "[#0162e8]".
export const toClassColor = (token: string | undefined | null): string => {
  if (!token) return '';
  const t = String(token).trim();
  if (!t) return '';
  if (t.startsWith('[') && t.endsWith(']')) return t; // already arbitrary value
  if (t.startsWith('#')) return `[${t}]`; // wrap hex for Tailwind arbitrary value
  return t; // tailwind color token (e.g., slate-500)
};

// Convenient helpers to build Tailwind classes from theme params
export const tw = {
  text: (token: string) => `text-${toClassColor(token)}`,
  bg: (token: string) => `bg-${toClassColor(token)}`,
  border: (token: string) => `border-${toClassColor(token)}`,
  ring: (token: string) => `ring-${toClassColor(token)}`,
  hover: {
    text: (token: string) => `hover:text-${toClassColor(token)}`,
    bg: (token: string) => `hover:bg-${toClassColor(token)}`,
    border: (token: string) => `hover:border-${toClassColor(token)}`,
  },
};

// Back-compat: generic helper
export const getTwClassName = (token: string, prefix: 'text' | 'bg' | 'border' | 'ring' = 'text'): string => {
  return `${prefix}-${toClassColor(token)}`;
};

// Build a Tailwind class list from prefix/value pairs
type TwClassConfig = { prefix: string; value?: string };
export const getTwClasses = (configs: TwClassConfig[]): string =>
  configs
    .map(({ prefix, value }) => (value ? `${prefix}-${value}` : ''))
    .filter(Boolean)
    .join(' ');

// Map BaseTheme params to composed class combos
export const getThemeClasses = (theme: BaseTheme): ThemeClasses => ({
  default: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_default) },
    { prefix: 'text', value: toClassColor(theme.text_default) },
    { prefix: 'border', value: toClassColor(theme.border_default) }
  ]),
  // Optional alias that maps to section tokens when present
  mainContent: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_mainContent || theme.bg_mainContentSection) },
    { prefix: 'text', value: toClassColor(theme.text_mainContentSection) },
    { prefix: 'border', value: toClassColor(theme.border_mainContentSection) }
  ]),
  sideBar: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_sidebar) },
    { prefix: 'text', value: toClassColor(theme.text_sidebar) },
    { prefix: 'border', value: toClassColor(theme.border_sidebar) }
  ]),
  sideBar_menu: getTwClasses([
    { prefix: 'text', value: toClassColor(theme.text_sidebar) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_sidebar_menu_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_sidebar_menu_hover) }
  ]),
  sideBar_menu_active: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_sidebar_menu_active) },
    { prefix: 'text', value: toClassColor(theme.text_sidebar_menu_active) }
  ]),
  mainHeader: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_header) },
    { prefix: 'text', value: toClassColor(theme.text_header) },
    { prefix: 'border', value: toClassColor(theme.border_header) }
  ]),
  mainContentSection: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_mainContentSection) },
    { prefix: 'text', value: toClassColor(theme.text_mainContentSection) },
    { prefix: 'border', value: toClassColor(theme.border_mainContentSection) }
  ]),
  tab: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_tab) },
    { prefix: 'text', value: toClassColor(theme.text_tab) },
    { prefix: 'border', value: toClassColor(theme.border_tab) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_tab_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_tab_hover) },
    { prefix: 'hover:border', value: toClassColor(theme.border_tab_hover) }
  ]),
  
  tab_active: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_tab_active) },
    { prefix: 'text', value: toClassColor(theme.text_tab_active) },
    { prefix: 'border', value: toClassColor(theme.border_tab_active) }
  ]),
  modalHeader: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_modalHeader) },
    { prefix: 'text', value: toClassColor(theme.text_modalHeader) },
    { prefix: 'border', value: toClassColor(theme.border_modalHeader) }
  ]),
  button_default: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_button_default) },
    { prefix: 'text', value: toClassColor(theme.text_button_default) },
    { prefix: 'border', value: toClassColor(theme.border_button_default) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_button_default_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_button_default_hover) },
    { prefix: 'hover:border', value: toClassColor(theme.border_button_default_hover) }
  ]),
  button_secondary: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_button_secondary) },
    { prefix: 'text', value: toClassColor(theme.text_button_secondary) },
    { prefix: 'border', value: toClassColor(theme.border_button_secondary) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_button_secondary_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_button_secondary_hover) },
    { prefix: 'hover:border', value: toClassColor(theme.border_button_secondary_hover) }
  ]),
 
  menu_default: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_menu_default) },
    { prefix: 'text', value: toClassColor(theme.text_menu_default) },
    { prefix: 'border', value: toClassColor(theme.border_menu_default) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_menu_default_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_menu_default_hover) },
    { prefix: 'hover:border', value: toClassColor(theme.border_menu_default_hover) }
  ]),
  menu_secondary: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_menu_secondary) },
    { prefix: 'text', value: toClassColor(theme.text_menu_secondary) },
    { prefix: 'border', value: toClassColor(theme.border_menu_secondary) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_menu_secondary_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_menu_secondary_hover) },
    { prefix: 'hover:border', value: toClassColor(theme.border_menu_secondary_hover) }
  ]),
  menu_divider: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_menu_divider) }
  ]),
  contextMenu: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_contextMenu) },
    { prefix: 'text', value: toClassColor(theme.text_contextMenu) },
    { prefix: 'hover:bg', value: toClassColor(theme.bg_contextMenu_hover) },
    { prefix: 'hover:text', value: toClassColor(theme.text_contextMenu_hover) }
  ]),  
  title: getTwClasses([
    { prefix: 'text', value: toClassColor(theme.text_title) }   
  ]),
  label: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_label) },
    { prefix: 'text', value: toClassColor(theme.text_label) },
    { prefix: 'border', value: toClassColor(theme.border_label) }
  ]),
  inputBox: getTwClasses([
    { prefix: 'bg', value: toClassColor(theme.bg_input_box) },
    { prefix: 'text', value: toClassColor(theme.text_input_box) },
    { prefix: 'border', value: toClassColor(theme.border_input_box) },
    { prefix: 'focus:ring', value: toClassColor(theme.border_default_active) }
  ])
});

// Build runtime theme: class combos + raw param
export const buildRuntimeTheme = (params: BaseTheme): Theme => ({
  ...getThemeClasses(params),
  param: params,
});

// Function to apply theme colors to Wijmo components and other theme-related CSS overrides
export const applyThirdPartCssOverrides = (theme: any) => {
  // Remove any existing theme styles
  const existingStyle = document.getElementById('theme-css-override');
  if (existingStyle) {
    existingStyle.remove();
  }
 
  // Capture theme values immediately to avoid Immer proxy revocation in async callbacks
  const capturedTheme = {
    // General
    defaultTextColor: theme.text_default,
    bg_mainContentSection: theme.bg_mainContentSection,
    text_label: theme.text_label,
    text_title: theme.text_title,
    // Input box
    bg_input_box: theme.bg_input_box,
    bg_input_readonly: theme.bg_input_readonly,
    border_input_box: theme.border_input_box,
    text_input_box: theme.text_input_box,
    text_button_inputBox: theme.text_button_inputBox,
    // Wijmo primary
    textColor: theme.wijmo_grid_default_text_color,
    headerColor: theme.wijmo_grid_header_text_color,
    bgColor: theme.wijmo_grid_row_background_color,
    headerBgColor: theme.wijmo_grid_header_background_color,
    altRowBgColor: theme.wijmo_grid_row_alt_background_color,
    borderColor: theme.wijmo_grid_row_border_color,
    columnBorderColor: theme.wijmo_grid_column_border_color,
    hoverBgColor: theme.wijmo_grid_selected_cell_background_color,
    activeBgColor: theme.wijmo_grid_selected_cell_background_color,
    activeColor: theme.wijmo_grid_header_text_color,
    // Container/outer
    outerBorderColor: theme.wijmo_grid_outer_border_color,
    containerBorderColor: theme.wijmo_grid_container_border_color,
    // Footer/Header extra
    headerBorderColor: theme.wijmo_grid_header_border_color,
    footerBgColor: theme.wijmo_grid_footer_background_color,
    footerTextColor: theme.wijmo_grid_footer_text_color,
    // Defaults
    defaultBgColor: theme.wijmo_grid_default_background_color,
    defaultFontSize: theme.wijmo_grid_default_font_size,
    // Group panel
    groupPanelBg: theme.wijmo_grid_grouppanel_background_color,
    groupPanelText: theme.wijmo_grid_grouppanel_text_color,
    // Selection text
    selectedRowTextColor: theme.wijmo_grid_selected_row_text_color,
    selectedCellTextColor: theme.wijmo_grid_selected_cell_text_color,
    // Tree view selection
    tvSelectedRowBg: theme.wijmo_grid_treeView_selectedRow_background_color,
    tvSelectedRowText: theme.wijmo_grid_treeView_selectedRow_text_color,
  };

  // Function to create and apply the theme styles
  const createThemeStyles = () => {
    const style = document.createElement('style');
    style.id = 'theme-css-override';

    const isDarkColor = (hex: string): boolean => {
      const h = (hex || '').trim();
      const m = h.match(/^#([0-9a-fA-F]{6})$/);
      if (!m) return false;
      const n = parseInt(m[1], 16);
      const r = (n >> 16) & 0xff;
      const g = (n >> 8) & 0xff;
      const b = n & 0xff;
      // perceived luminance
      const lum = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
      return lum < 0.5;
    };

    // Convert Tailwind colors to hex format
    const defaultTextColorHex = convertTailwindToHex(capturedTheme.defaultTextColor);
    const input_box_bg_colorHex = convertTailwindToHex(capturedTheme.bg_input_box);
    const inputBorderColorHex = convertTailwindToHex(capturedTheme.border_input_box);
    const dateIndicatorFilter = isDarkColor(input_box_bg_colorHex) ? 'invert(1)' : 'none';

    // Create CSS rules with captured theme colors
    style.textContent = `
      /* Autofill Override - Override browser autofill styling */
      input:-internal-autofill-selected,
      input:-webkit-autofill,
      input:-webkit-autofill:hover,
      input:-webkit-autofill:focus,
      input:-webkit-autofill:active,
      input:-internal-autofill-selected  {     
        color: ${defaultTextColorHex} !important;        
        -webkit-text-fill-color: ${defaultTextColorHex} !important;
        -webkit-box-shadow: 0 0 0 1000px ${input_box_bg_colorHex} inset !important;
        border: solid 1px ${inputBorderColorHex} !important;
        transition: background-color 5000s ease-in-out 0s;
      }

      /* Date/Time picker icon (Chrome/Edge) */
      input[type='date']::-webkit-calendar-picker-indicator,
      input[type='datetime-local']::-webkit-calendar-picker-indicator,
      input[type='time']::-webkit-calendar-picker-indicator {
        filter: ${dateIndicatorFilter};
        opacity: 0.8;
      }

      /* Grid and Content Styling */
      .wj-flexgrid {
        border: none !important;
        ${capturedTheme.defaultBgColor ? `background-color: ${capturedTheme.defaultBgColor} !important;` : `background-color: ${capturedTheme.headerBgColor} !important;`}
        color: ${capturedTheme.textColor} !important;
        border-radius: 0 !important;
        ${capturedTheme.defaultFontSize ? `font-size: ${capturedTheme.defaultFontSize};` : ''}
      }
      
      .wj-content:not(.customEdit), 
      div[wj-part='cells'],
      div[wj-part='root'] {
        ${capturedTheme.defaultBgColor ? `background: ${capturedTheme.defaultBgColor} !important;` : `background: ${capturedTheme.headerBgColor} !important;`}
      }

      .wj-flexgrid .wj-grid-editor {
        width: 100% !important;
      }

      /* Header Styling */
      .wj-header, .wj-cell.wj-header, .wj-cell.wj-header,
      .wj-colheaders .wj-header {
        background: ${capturedTheme.headerBgColor} !important;
        color: ${capturedTheme.headerColor} !important;
        font-weight: 400;
        ${capturedTheme.headerBorderColor ? `border-color: ${capturedTheme.headerBorderColor} !important;` : ''}
      }

      div[wj-part='rhcells'] .wj-header {
        background-color: ${capturedTheme.headerBgColor} !important;
      }

      /* Cell Styling */
      .wj-cell {
        border-color: ${capturedTheme.borderColor} !important;
        ${capturedTheme.defaultBgColor ? `background-color: ${capturedTheme.defaultBgColor} !important;` : `background-color: ${capturedTheme.bgColor} !important;`}
      }   

      .wj-cell.wj-header .wj-elem-filter {
        order: 3;
      }

      .wj-flexgrid .wj-cell {
        ${(capturedTheme.columnBorderColor || capturedTheme.borderColor) ? `border-right: 1px solid ${(capturedTheme.columnBorderColor || capturedTheme.borderColor)} !important;` : ''}
        ${capturedTheme.borderColor ? `border-bottom: 1px solid ${capturedTheme.borderColor} !important;` : ''}
        border-top-style: none;  
        padding: 4px 10px;
      }

      .wj-pivotgrid.wj-flexgrid .wj-cell {
        ${(capturedTheme.columnBorderColor || capturedTheme.borderColor) ? `border-right: 1px solid ${(capturedTheme.columnBorderColor || capturedTheme.borderColor)} !important;` : ''}
      }

      .wj-flexgrid .wj-colheaders .wj-cell {
        ${(capturedTheme.columnBorderColor || capturedTheme.borderColor) ? `border-right: 1px solid ${(capturedTheme.columnBorderColor || capturedTheme.borderColor)} !important;` : ''}
      }

      .wj-flexgrid .wj-flexgrid .wj-cell.wj-cell {
        border-top-style: none;
        padding: 4px;
      }

      /* Hide right border for rightmost column cells (not inside pivot — container uses .wj-pivotgrid) */
      .wj-flexgrid:not(.wj-pivotgrid) .wj-row .wj-cell:last-child,
      .wj-flexgrid:not(.wj-pivotgrid) .wj-colheaders .wj-cell:last-child,
      .wj-flexgrid:not(.wj-pivotgrid) .wj-colfooters .wj-cell:last-child {
        border-right: none !important;
      }

      /* Nested FlexGrid inside pivot: parent is .wj-pivotgrid, inner grid lacks .wj-pivotgrid — restore right border on last column */
      .wj-pivotgrid .wj-flexgrid .wj-row .wj-cell:last-child,
      .wj-pivotgrid .wj-flexgrid .wj-colheaders .wj-cell:last-child,
      .wj-pivotgrid .wj-flexgrid .wj-colfooters .wj-cell:last-child {
        ${(capturedTheme.columnBorderColor || capturedTheme.borderColor) ? `border-right: 1px solid ${(capturedTheme.columnBorderColor || capturedTheme.borderColor)} !important;` : 'border-right: revert !important;'}
      }

       /* Alternate Row Styling */
      .wj-alt {
        background-color: ${capturedTheme.altRowBgColor} !important;
      }

      .wj-alt:not(.wj-state-selected):not(.wj-state-multi-selected) {
        background: ${capturedTheme.activeBgColor} !important;
      }

      /* Wijmo GroupPanel control (drag columns to group) — flat chips, app-aligned (not grid group rows) */
      .wj-grouppanel {
        ${(
          capturedTheme.groupPanelBg ||
          capturedTheme.bg_mainContentSection ||
          capturedTheme.bg_input_box
        )
          ? `background: ${capturedTheme.groupPanelBg || capturedTheme.bg_mainContentSection || capturedTheme.bg_input_box} !important;`
          : ''}
        ${(
          capturedTheme.groupPanelText ||
          capturedTheme.headerColor ||
          capturedTheme.textColor
        )
          ? `color: ${capturedTheme.groupPanelText || capturedTheme.headerColor || capturedTheme.textColor} !important;`
          : ''}
        padding: 4px 8px !important;
        min-height: 0 !important;
        box-shadow: none !important;
        border: none !important;
        ${capturedTheme.defaultFontSize ? `font-size: ${capturedTheme.defaultFontSize} !important;` : 'font-size: 11px !important;'}
      }
      .wj-grouppanel .wj-groupplaceholder {
        font-size: 10px !important;
        opacity: 0.88 !important;
        ${capturedTheme.text_label || capturedTheme.headerColor ? `color: ${capturedTheme.text_label || capturedTheme.headerColor} !important;` : ''}
      }
      .wj-grouppanel .wj-groupmarker {
        padding: 2px 8px !important;
        margin-right: 6px !important;
        border-radius: 4px !important;
        box-shadow: none !important;
        ${capturedTheme.border_input_box ? `border: 1px solid ${capturedTheme.border_input_box} !important;` : ''}
        ${capturedTheme.bg_input_box ? `background: ${capturedTheme.bg_input_box} !important;` : ''}
        ${capturedTheme.headerColor || capturedTheme.text_title ? `color: ${capturedTheme.headerColor || capturedTheme.text_title} !important;` : ''}
        ${capturedTheme.defaultFontSize ? `font-size: ${capturedTheme.defaultFontSize} !important;` : 'font-size: 11px !important;'}
        font-weight: 500 !important;
      }
      .wj-grouppanel .wj-groupmarker:hover {
        ${capturedTheme.bg_input_readonly || capturedTheme.bg_input_box ? `background: ${capturedTheme.bg_input_readonly || capturedTheme.bg_input_box} !important;` : ''}
      }
      .wj-grouppanel .wj-groupmarker .wj-remove {
        padding: 2px 0 2px 6px !important;
        opacity: 0.7 !important;
      }
      .wj-grouppanel .wj-groupmarker .wj-glyph-drag {
        margin: 0 6px 0 0 !important;
        opacity: 0.5 !important;
      }
      .wj-grouppanel .wj-groupmarker span {
        opacity: 1 !important;
      }

      /* Group Styling */
      .wj-flexgrid .wj-group {
        text-align: left;
      }
      /* Group panel styling */
      .wj-group:not(.wj-state-selected):not(.wj-state-multi-selected) {
        ${capturedTheme.groupPanelBg ? `background-color: ${capturedTheme.groupPanelBg} !important;` : ''}
        ${capturedTheme.groupPanelText ? `color: ${capturedTheme.groupPanelText} !important;` : ''}
      }
    
      .wj-cell.wj-group  {
        color: unset;
      }
      // .wj-flexgrid .wj-cell.wj-header::before {
      //   content: "";
      //   width: 1px;
      //   height: 12px;
      //   position: absolute;
      //   top: calc(50% - 6px);
      //   right: 0;
      //   ${capturedTheme.borderColor ? `background: ${capturedTheme.borderColor} !important;` : ''}
      // }

      .wj-flexgrid .wj-colfooters .wj-cell.wj-header,
      div[wj-part='blcells'] .wj-header {
        ${capturedTheme.footerBgColor ? `background: ${capturedTheme.footerBgColor} !important;` : ''}
        ${capturedTheme.footerTextColor ? `color: ${capturedTheme.footerTextColor} !important;` : ''}
      }

      div[wj-part='rhcells'] .wj-header {
        ${capturedTheme.defaultBgColor ? `background-color: ${capturedTheme.defaultBgColor} !important;` : ''}
      }

      .wj-flexgrid :not(.wj-state-selected):not(.wj-state-multi-selected).wj-group {
        text-align: left;
      }

      /* Tree View Styling */
      .app-wj-treeview .wj-flexgrid .wj-group,
      .app-wj-treeview .wj-group,
      .app-wj-treeview.wj-flexgrid :not(.wj-state-selected):not(.wj-state-multi-selected).wj-group {
        background-color: transparent;
      }

      .app-wj-treeview .wj-content {
        border: none;
      }

      .app-wj-treeview .wj-cell.wj-cell {
        border-bottom-style: none;
        border-top-style: none;
      }

      /* Selection States */
      .wj-state-selected {
        background: ${capturedTheme.activeBgColor} !important;
        ${capturedTheme.selectedRowTextColor ? `color: ${capturedTheme.selectedRowTextColor} !important;` : `color: ${capturedTheme.activeColor} !important;`}
      }

      .wj-state-multi-selected {
        background-color: ${capturedTheme.activeBgColor} !important;
        ${capturedTheme.selectedRowTextColor ? `color: ${capturedTheme.selectedRowTextColor} !important;` : `color: ${capturedTheme.activeColor} !important;`}
      }

      .wj-state-selected .hoverText, 
      .wj-state-multi-selected .hoverText {
        color: ${capturedTheme.activeColor} !important;
      }

     

      /* Input Controls */
      .wj-input-group {
        ${capturedTheme.borderColor ? `border-color: ${capturedTheme.borderColor} !important;` : ''}
        color: ${capturedTheme.textColor} !important;
        border-radius: 0px;
        font-size: 12px;
      }

      .wj-input-group-btn > .wj-btn {
        border: 1px;
      }

      .wj-content:not(.customEdit) .wj-input-group-btn > .wj-btn:hover, 
      .wj-content:not(.customEdit) .wj-btn-group > .wj-btn:hover,
      .wj-content:not(.customEdit) .wj-input-group-btn > .wj-btn:focus, 
      .wj-content:not(.customEdit) .wj-btn-group > .wj-btn:focus {
        background: ${capturedTheme.hoverBgColor} !important;
      }

      .wj-input-group-btn:last-child > .wj-btn {
        color: ${capturedTheme.textColor}80 !important;
      }

      .wj-input-group:hover .wj-input-group-btn:last-child > .wj-btn {
        color: ${capturedTheme.textColor} !important;
      }

      /* Dropdown Panels */
      .wj-dropdown-panel {
        background-color: ${capturedTheme.headerBgColor} !important;
        color: ${capturedTheme.textColor} !important;
      }

      .wj-dropdown .wj-form-control {
        padding-left: 4px;
        padding-right: 4px;
        font-size: 12px;
        color: ${capturedTheme.text_input_box} !important;
      }

      /* Listbox Styling */
      .wj-listbox {
        max-height: 300px !important;
      }   

      .wj-listbox .wj-listbox {
        overflow: visible;
      }

      .wj-listbox-item label {
        width: auto;
      }

      .wj-listbox-item:not(.wj-state-selected):hover, 
      .wj-alt:not(.wj-state-selected):not(.wj-state-multi-selected) {
        background: ${capturedTheme.altRowBgColor} !important;
      }

      /* Content and Controls */
      .wj-content, .wj-btn-group, .wj-btn-group-vertical, .wj-tooltip, .customEdit {
        border-radius: 0px;
        ${capturedTheme.borderColor ? `border-color: ${capturedTheme.border_input_box} !important;` : ''}
        font-size: 12px;
      }

      .wj-content:not(.customEdit):not(.wj-dropdown-panel):not(.wj-listbox):not(.wj-flexgrid):not(.wj-calendar-outer) {
        border: 1px solid ${capturedTheme.border_input_box} !important;
      }

      .wj-btn-default {
        ${capturedTheme.borderColor ? `border: 1px solid ${capturedTheme.borderColor};` : ''}
      }

      .wj-control .wj-input-group .wj-input-group-btn:last-child:not(:first-child)>.wj-btn, .wj-viewer .wj-control .wj-input-group .wj-input-group-btn:last-child:not(:first-child)>.wj-applybutton{
        ${capturedTheme.border_input_box ? `border-left: 1px solid ${capturedTheme.border_input_box};` : ''}
      }

      .wj-control .wj-input-group .wj-input-group-btn:first-child:not(:last-child)>.wj-btn, .wj-viewer .wj-control .wj-input-group .wj-input-group-btn:first-child:not(:last-child)>.wj-applybutton
      {
        color: ${capturedTheme.textColor}80 !important;
        ${capturedTheme.border_input_box ? `border-right: 1px solid ${capturedTheme.border_input_box} !important;` : ''};
      }

      /* Tooltip */
      .wj-tooltip {
        background: ${capturedTheme.headerBgColor} !important;
        color: ${capturedTheme.textColor} !important;
        border: 1px solid ${capturedTheme.borderColor} !important;
      }

      /* Gauge */
      .wj-gauge .wj-pointer path {  
        fill: ${capturedTheme.activeBgColor} !important;
      }

      /* Dropdown Elements */
      .wj-cell .wj-elem-dropdown {
        opacity: 1;    
        font-size: 11.5px;
        color: ${capturedTheme.textColor}80 !important;
      }

      .wj-row:hover .wj-cell .wj-elem-dropdown {
        color: ${capturedTheme.textColor} !important;
      }

      /* Tree View Custom Styling */
      .custom-tree-line.wj-treeview .wj-nodelist > .wj-nodelist > .wj-node,
      .custom-tree-line.wj-treeview .wj-nodelist > .wj-nodelist > .wj-nodelist,
      .custom-tree-line.wj-treeview .wj-nodelist > .wj-nodelist > .wj-nodelist > .wj-node,
      .custom-tree-line.wj-treeview .wj-nodelist > .wj-nodelist > .wj-nodelist > .wj-nodelist {
        font-size: 100%;
        border-left: 1px solid ${capturedTheme.activeColor}50;
      }

      .custom-tree-line.wj-treeview .wj-nodelist .wj-node:before {      
        font-family: 'Glyphicons Halflings';
        font-size: 9px;
        color: ${capturedTheme.activeColor} !important;
        width: 12px;
        height: 12px;
        padding: 0px 0 0 2px;
        top: 0px;
        left: -2px;
        border: solid 1px;
        opacity: .3;
        transition: all .3s cubic-bezier(.4,0,.2,1);
      }

      .custom-tree-line.wj-treeview .wj-nodelist .wj-node.wj-state-collapsed:before,
      .custom-tree-line.wj-treeview .wj-nodelist .wj-node.wj-state-collapsing:before {      
        padding: 0px 0 0 2px;
        width: 12px;
        height: 12px;
        transform: rotate(0deg);
      }

      /* Utility Classes */
      .wj-col-picker-dropdown {
        z-index: 10;
      }

      .wj-control[disabled] {
        ${capturedTheme.bgColor ? `background-color: ${capturedTheme.bgColor} !important;` : ''}
        opacity: .9;
      }

      .wj-cell.wj-alt {
        background: #ffffff;
      }
      

      /* Input group fine-tuning */
      .wj-input-group {
        border-radius: 4px !important;
      }
      .wj-input-group-btn .wj-glyph-down::before {
        ${capturedTheme.text_button_inputBox ? `color: ${capturedTheme.text_button_inputBox} !important;` : ''}
      }
    `;

    // Replace any prior override (e.g. rapid theme switches) and apply immediately
    const priorStyle = document.getElementById('theme-css-override');
    if (priorStyle) {
      priorStyle.remove();
    }
    document.head.appendChild(style);
  };

  // Check if Wijmo styles are already loaded
  const wijmoStylesLoaded = document.querySelector('style[data-wijmo]') ||
    document.querySelector('link[href*="wijmo"]') ||
    document.querySelector('.wj-flexgrid');

  if (wijmoStylesLoaded) {
    // Wijmo styles are already loaded, apply our overrides immediately
    createThemeStyles();
  } else {
    // Wait for Wijmo styles to load, then apply our overrides
    const checkWijmoStyles = () => {
      document.querySelector('style[data-wijmo]') ||
        document.querySelector('link[href*="wijmo"]') ||
        document.querySelector('.wj-flexgrid');
      createThemeStyles();
      // if (wijmoLoaded) {
      //   createThemeStyles();
      // } else {
      //   // Check again after a short delay
      //   setTimeout(checkWijmoStyles, 100);
      // }
    };
    
    // Start checking for Wijmo styles
    setTimeout(checkWijmoStyles, 100);
  }
};

// Function to remove theme CSS overrides
export const removeThemeCssOverrides = () => {
  const existingStyle = document.getElementById('theme-css-override');
  if (existingStyle) {
    existingStyle.remove();
  }
}; 