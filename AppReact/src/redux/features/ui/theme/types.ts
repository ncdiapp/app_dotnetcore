export interface ThemeClasses {
  default: string;
  sideBar: string;
  sideBar_menu: string;
  sideBar_menu_active: string;
  mainHeader: string;
  mainContentSection: string;
  // Aliases/additional combos for convenience (optional usage)
  mainContent?: string;
  tab: string;
  tab_active: string;
  modalHeader: string;
  button_default: string;
  button_secondary: string;
  menu_divider?: string;
  menu_default: string;
  menu_secondary: string;
  contextMenu: string;    
  title: string;
  label: string;
  inputBox: string;
}

export interface BaseTheme {
  scrollbar_style: string;

  bg_default: string;
  text_default: string;
  border_default: string;

  bg_default_hover: string;
  text_default_hover: string;
  border_default_hover: string;

  bg_default_active: string;
  text_default_active: string;
  border_default_active: string;

  bg_header: string;
  text_header: string;
  border_header: string;




  
  bg_mainContent?: string;
  bg_mainContentSection?: string;
  text_mainContentSection: string;
  border_mainContentSection: string;

  text_title: string;
  text_title_heavy?: string;

  bg_sidebar: string;
  text_sidebar: string;
  border_sidebar: string;

  bg_sidebar_menu_hover: string;
  text_sidebar_menu_hover: string;
  bg_sidebar_menu_active: string;
  text_sidebar_menu_active: string;

  bg_input_box: string;
  bg_input_readonly?: string;
  text_input_box: string;
  border_input_box: string;
  // Additional input tokens
 
  text_button_inputBox?: string;

  logo_filter: string;

  bg_tab: string;
  text_tab: string;
  border_tab: string;
  bg_tab_hover: string;
  text_tab_hover: string;
  border_tab_hover: string;

  bg_tab_active: string;
  text_tab_active: string;
  border_tab_active: string;

  bg_modalBackdrop: string;
  bg_modalHeader: string;
  text_modalHeader: string;
  border_modalHeader: string;

  bg_button_default: string;
  bg_button_default_hover: string;
  text_button_default: string;
  text_button_default_hover: string;
  border_button_default: string;
  border_button_default_hover: string;

  bg_button_secondary: string;
  bg_button_secondary_hover: string;
  text_button_secondary: string;
  text_button_secondary_hover: string;
  border_button_secondary: string;
  border_button_secondary_hover: string;

  bg_menu_divider: string;

  bg_menu_default: string;
  bg_menu_default_hover: string;
  text_menu_default: string;
  text_menu_default_hover: string;
  border_menu_default: string;
  border_menu_default_hover: string;

  bg_menu_secondary: string;
  bg_menu_secondary_hover: string;
  text_menu_secondary: string;
  text_menu_secondary_hover: string;
  border_menu_secondary: string;
  border_menu_secondary_hover: string;

  bg_contextMenu: string;
  text_contextMenu: string;
  bg_contextMenu_hover: string;
  text_contextMenu_hover: string;

  border_desktop_widget: string;

  bg_label: string;
  text_label: string;
  border_label: string;
  textAlign_label?: string;

  wijmo_grid_outer_border_color: string;
  wijmo_grid_row_border_color: string;
  wijmo_grid_column_border_color: string;
  wijmo_grid_header_background_color: string;
  wijmo_grid_header_border_color?: string;
  wijmo_grid_row_background_color: string;
  wijmo_grid_row_alt_background_color: string;
  wijmo_grid_selected_row_background_color: string;
  wijmo_grid_selected_row_text_color?: string;
  wijmo_grid_selected_cell_background_color: string;
  wijmo_grid_selected_cell_text_color?: string;
  wijmo_grid_header_text_color: string;
  wijmo_grid_default_text_color: string;
  wijmo_grid_default_background_color?: string;
  wijmo_grid_default_font_size?: string;
  wijmo_grid_footer_background_color?: string;
  wijmo_grid_footer_text_color?: string;
  wijmo_grid_container_border_color?: string;
  wijmo_grid_grouppanel_background_color?: string;
  wijmo_grid_grouppanel_text_color?: string;
  wijmo_grid_treeView_selectedRow_background_color?: string;
  wijmo_grid_treeView_selectedRow_text_color?: string;
  
}

// Runtime theme surface available to components:
// - Flattened class combos (e.g., contextMenu)
// - Raw parameters nested under `param` for direct access
export type Theme = ThemeClasses & { param: BaseTheme };

export interface ThemeOption {
  id: string;
  name: string;
  theme: BaseTheme;
}

export interface ThemeState {
  currentTheme: Theme;
  availableThemes: ThemeOption[];
  currentThemeId: string;
}
