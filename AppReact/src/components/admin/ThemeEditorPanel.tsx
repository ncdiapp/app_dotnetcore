import React, { useCallback, useEffect, useMemo, useState } from 'react';
import type { ThemeOption, BaseTheme } from '../../redux/features/ui/theme/types';
import { useTheme } from '../../redux/hooks/useTheme';
import { saveOneUserDefinedTheme } from '../../helper/themeHelper';
import { builtInThemes } from '../../redux/features/ui/theme/themeSlice';
import { useDispatch } from 'react-redux';
import { AppDispatch } from '../../redux/store';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

interface ThemeEditorPanelProps {
  theme: ThemeOption;
  onClose: () => void;
  onThemeSaved?: (updatedThemeId: string) => Promise<void> | void;
}

type FieldType = 'color' | 'text';

type FieldMeta = {
  key: keyof BaseTheme;
  label: string;
  type?: FieldType;
  help?: string;
};

type FieldGroup = {
  id: string;
  title: string;
  description?: string;
  fields: FieldMeta[];
};

const FIELD_GROUPS: FieldGroup[] = [
  {
    id: 'core',
    title: 'Core Colors & Scrollbar',
    fields: [
      { key: 'scrollbar_style', label: 'Scrollbar Style', type: 'text' },
      { key: 'bg_default', label: 'Background Default', type: 'color' },
      { key: 'text_default', label: 'Text Default', type: 'color' },
      { key: 'border_default', label: 'Border Default', type: 'color' },
      { key: 'bg_default_hover', label: 'Background Hover', type: 'color' },
      { key: 'text_default_hover', label: 'Text Hover', type: 'color' },
      { key: 'border_default_hover', label: 'Border Hover', type: 'color' },
      { key: 'bg_default_active', label: 'Background Active', type: 'color' },
      { key: 'text_default_active', label: 'Text Active', type: 'color' },
      { key: 'border_default_active', label: 'Border Active', type: 'color' },
      { key: 'logo_filter', label: 'Logo Filter', type: 'text', help: 'Tailwind filter utility classes (e.g. brightness-95 contrast-125).' },
    ],
  },
  {
    id: 'layout',
    title: 'Layout & Typography',
    fields: [
      { key: 'bg_header', label: 'Header Background', type: 'color' },
      { key: 'text_header', label: 'Header Text', type: 'color' },
      { key: 'border_header', label: 'Header Border', type: 'color' },
      { key: 'bg_mainContent', label: 'Main Content Background', type: 'color' },
      { key: 'bg_mainContentSection', label: 'Section Background', type: 'color' },
      { key: 'text_mainContentSection', label: 'Section Text', type: 'color' },
      { key: 'border_mainContentSection', label: 'Section Border', type: 'color' },
      { key: 'text_title', label: 'Title Text', type: 'color' },
    ],
  },
  {
    id: 'sidebar',
    title: 'Sidebar & Navigation',
    fields: [
      { key: 'bg_sidebar', label: 'Sidebar Background', type: 'color' },
      { key: 'text_sidebar', label: 'Sidebar Text', type: 'color' },
      { key: 'border_sidebar', label: 'Sidebar Border', type: 'color' },
      { key: 'bg_sidebar_menu_hover', label: 'Sidebar Item Hover Background', type: 'color' },
      { key: 'text_sidebar_menu_hover', label: 'Sidebar Item Hover Text', type: 'color' },
      { key: 'bg_sidebar_menu_active', label: 'Sidebar Item Active Background', type: 'color' },
      { key: 'text_sidebar_menu_active', label: 'Sidebar Item Active Text', type: 'color' },
    ],
  },
  {
    id: 'inputs',
    title: 'Inputs & Forms',
    fields: [
      { key: 'bg_input_box', label: 'Input Background', type: 'color' },
      { key: 'bg_input_readonly', label: 'Input Readonly Background', type: 'color' },
      { key: 'text_input_box', label: 'Input Text', type: 'color' },
      { key: 'border_input_box', label: 'Input Border', type: 'color' },
      { key: 'text_button_inputBox', label: 'Input Button Icon', type: 'color' },
    ],
  },
  {
    id: 'tabsModal',
    title: 'Tabs & Modal',
    fields: [
      { key: 'bg_tab', label: 'Tab Background', type: 'color' },
      { key: 'text_tab', label: 'Tab Text', type: 'color' },
      { key: 'border_tab', label: 'Tab Border', type: 'color' },
      { key: 'bg_tab_hover', label: 'Tab Hover Background', type: 'color' },
      { key: 'text_tab_hover', label: 'Tab Hover Text', type: 'color' },
      { key: 'border_tab_hover', label: 'Tab Hover Border', type: 'color' },
      { key: 'bg_tab_active', label: 'Tab Active Background', type: 'color' },
      { key: 'text_tab_active', label: 'Tab Active Text', type: 'color' },
      { key: 'border_tab_active', label: 'Tab Active Border', type: 'color' },
      { key: 'bg_modalBackdrop', label: 'Modal Backdrop', type: 'color' },
      { key: 'bg_modalHeader', label: 'Modal Header Background', type: 'color' },
      { key: 'text_modalHeader', label: 'Modal Header Text', type: 'color' },
      { key: 'border_modalHeader', label: 'Modal Header Border', type: 'color' },
    ],
  },
  {
    id: 'buttons',
    title: 'Buttons',
    fields: [
      { key: 'bg_button_default', label: 'Primary Background', type: 'color' },
      { key: 'text_button_default', label: 'Primary Text', type: 'color' },
      { key: 'border_button_default', label: 'Primary Border', type: 'color' },
      { key: 'bg_button_default_hover', label: 'Primary Hover Background', type: 'color' },
      { key: 'text_button_default_hover', label: 'Primary Hover Text', type: 'color' },
      { key: 'border_button_default_hover', label: 'Primary Hover Border', type: 'color' },
      { key: 'bg_button_secondary', label: 'Secondary Background', type: 'color' },
      { key: 'text_button_secondary', label: 'Secondary Text', type: 'color' },
      { key: 'border_button_secondary', label: 'Secondary Border', type: 'color' },
      { key: 'bg_button_secondary_hover', label: 'Secondary Hover Background', type: 'color' },
      { key: 'text_button_secondary_hover', label: 'Secondary Hover Text', type: 'color' },
      { key: 'border_button_secondary_hover', label: 'Secondary Hover Border', type: 'color' },
    ],
  },
  {
    id: 'menus',
    title: 'Menus & Context Menus',
    fields: [
      { key: 'bg_menu_divider', label: 'Menu Divider', type: 'color' },
      { key: 'bg_menu_default', label: 'Menu Background', type: 'color' },
      { key: 'text_menu_default', label: 'Menu Text', type: 'color' },
      { key: 'border_menu_default', label: 'Menu Border', type: 'color' },
      { key: 'bg_menu_default_hover', label: 'Menu Hover Background', type: 'color' },
      { key: 'text_menu_default_hover', label: 'Menu Hover Text', type: 'color' },
      { key: 'border_menu_default_hover', label: 'Menu Hover Border', type: 'color' },
      { key: 'bg_menu_secondary', label: 'Secondary Menu Background', type: 'color' },
      { key: 'text_menu_secondary', label: 'Secondary Menu Text', type: 'color' },
      { key: 'border_menu_secondary', label: 'Secondary Menu Border', type: 'color' },
      { key: 'bg_menu_secondary_hover', label: 'Secondary Menu Hover Background', type: 'color' },
      { key: 'text_menu_secondary_hover', label: 'Secondary Menu Hover Text', type: 'color' },
      { key: 'border_menu_secondary_hover', label: 'Secondary Menu Hover Border', type: 'color' },
      { key: 'bg_contextMenu', label: 'Context Menu Background', type: 'color' },
      { key: 'text_contextMenu', label: 'Context Menu Text', type: 'color' },
      { key: 'bg_contextMenu_hover', label: 'Context Menu Hover Background', type: 'color' },
      { key: 'text_contextMenu_hover', label: 'Context Menu Hover Text', type: 'color' },
    ],
  },
  {
    id: 'labels',
    title: 'Labels & Misc',
    fields: [
      { key: 'text_title_heavy', label: 'Title Text (Heavy)', type: 'color' },
      { key: 'bg_label', label: 'Label Background', type: 'color' },
      { key: 'text_label', label: 'Label Text', type: 'color' },
      { key: 'border_label', label: 'Label Border', type: 'color' },
    ],
  },
  {
    id: 'wijmo',
    title: 'Wijmo Grid Overrides',
    description: 'Tailor grid colors for headers, rows, selection, and borders.',
    fields: [
      { key: 'wijmo_grid_outer_border_color', label: 'Grid Outer Border' },
      { key: 'wijmo_grid_row_border_color', label: 'Grid Row Border' },
      { key: 'wijmo_grid_column_border_color', label: 'Grid Column Border' },
      { key: 'wijmo_grid_header_background_color', label: 'Header Background' },
      { key: 'wijmo_grid_header_border_color', label: 'Header Border' },
      { key: 'wijmo_grid_header_text_color', label: 'Header Text Color' },
      { key: 'wijmo_grid_row_background_color', label: 'Row Background' },
      { key: 'wijmo_grid_row_alt_background_color', label: 'Row Alt Background' },
      { key: 'wijmo_grid_selected_row_background_color', label: 'Selected Row Background' },
      { key: 'wijmo_grid_selected_row_text_color', label: 'Selected Row Text' },
      { key: 'wijmo_grid_selected_cell_background_color', label: 'Selected Cell Background' },
      { key: 'wijmo_grid_selected_cell_text_color', label: 'Selected Cell Text' },
      { key: 'wijmo_grid_default_text_color', label: 'Default Text' },
      { key: 'wijmo_grid_default_background_color', label: 'Default Background' },
      { key: 'wijmo_grid_default_font_size', label: 'Default Font Size', type: 'text' },
      { key: 'wijmo_grid_footer_background_color', label: 'Footer Background' },
      { key: 'wijmo_grid_footer_text_color', label: 'Footer Text' },
      { key: 'wijmo_grid_container_border_color', label: 'Container Border' },
      { key: 'wijmo_grid_grouppanel_background_color', label: 'Group Panel Background' },
      { key: 'wijmo_grid_grouppanel_text_color', label: 'Group Panel Text' },
      { key: 'wijmo_grid_treeView_selectedRow_background_color', label: 'Tree Row Background' },
      { key: 'wijmo_grid_treeView_selectedRow_text_color', label: 'Tree Row Text' },
    ],
  },
];

const BASE_THEME_KEYS: (keyof BaseTheme)[] = Array.from(
  new Set(FIELD_GROUPS.flatMap((group) => group.fields.map((field) => field.key)))
);

const buildBaseTheme = (source: Partial<BaseTheme>): BaseTheme => {
  const result: Partial<BaseTheme> = {};
  BASE_THEME_KEYS.forEach((key) => {
    (result as any)[key] = (source as any)?.[key] ?? '';
  });
  return result as BaseTheme;
};

const toThemeDetailsDict = (theme: BaseTheme): Record<string, string> => {
  const dict: Record<string, string> = {};
  BASE_THEME_KEYS.forEach((key) => {
    dict[key as string] = theme[key] ?? '';
  });
  return dict;
};

const hexColorRegex = /^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/;

const colorPreview = (value: string): string | null => {
  if (!value) return null;
  if (value.startsWith('[#') && value.endsWith(']')) {
    return value.slice(1, -1);
  }
  if (value.startsWith('[') && value.endsWith(']')) {
    return value.slice(1, -1);
  }
  if (value.startsWith('#')) return value;
  if (/^rgba?\(/i.test(value) || /^hsla?\(/i.test(value)) return value;
  return null;
};

const validateColorValue = (value: string): string | null => {
  if (!value) return null;
  const tailwindRegex = /^[a-z]+(-[0-9]{1,3})?(\/[0-9]{1,3})?$/i;
  const arbitraryRegex = /^\[[^\]]+\]$/;
  const standardFuncRegex = /^(rgba?|hsla?)\([^)]+\)$/i;
  const keywordRegex = /^(transparent|inherit|currentColor|initial)$/i;
  if (
    hexColorRegex.test(value) ||
    tailwindRegex.test(value) ||
    arbitraryRegex.test(value) ||
    standardFuncRegex.test(value) ||
    keywordRegex.test(value)
  ) {
    return null;
  }
  return 'Invalid color format. Use hex (#0162e8), Tailwind token (slate-500), or [custom] notation.';
};

const ThemeEditorPanel: React.FC<ThemeEditorPanelProps> = ({ theme: editingTheme, onClose, onThemeSaved }) => {
  const { theme, t } = useTheme();
  const dispatch = useDispatch<AppDispatch>();

  const [themeName, setThemeName] = useState<string>(editingTheme.name);
  const [formTheme, setFormTheme] = useState<BaseTheme>(() => buildBaseTheme(editingTheme.theme));
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null);

  useEffect(() => {
    setThemeName(editingTheme.name);
    setFormTheme(buildBaseTheme(editingTheme.theme));
    setFieldErrors({});
    setSaveError(null);
    setSaveSuccess(null);
  }, [editingTheme]);

  const hasErrors = useMemo(() => Object.keys(fieldErrors).length > 0, [fieldErrors]);

  const handleFieldChange = useCallback(
    (key: keyof BaseTheme, value: string, type: FieldType = 'color') => {
      setFormTheme((prev) => ({
        ...prev,
        [key]: value,
      }));
      if (type === 'color') {
        const keyName = key as unknown as string;
        const validationMessage = validateColorValue(value);
        setFieldErrors((prev) => {
          if (!validationMessage) {
            if (prev[keyName]) {
              const { [keyName]: _, ...rest } = prev;
              return rest;
            }
            return prev;
          }
          return {
            ...prev,
            [keyName]: validationMessage,
          };
        });
      } else {
        const keyName = key as unknown as string;
        setFieldErrors((prev) => {
          if (prev[keyName]) {
            const { [keyName]: _, ...rest } = prev;
            return rest;
          }
          return prev;
        });
      }
    },
    []
  );

  const handleResetToBase = useCallback(
    (baseId: string) => {
      const base = builtInThemes.find((themeOption) => themeOption.id === baseId);
      if (!base) return;
      setFormTheme(buildBaseTheme(base.theme));
      setFieldErrors({});
    },
    []
  );

  const handleRevertChanges = useCallback(() => {
    setFormTheme(buildBaseTheme(editingTheme.theme));
    setFieldErrors({});
  }, [editingTheme.theme]);

  const handleSave = useCallback(async () => {
    const trimmedName = themeName.trim();
    if (!trimmedName) {
      setSaveError('Theme name is required.');
      return;
    }
    if (hasErrors) {
      setSaveError('Please fix validation errors before saving.');
      return;
    }

    setSaveError(null);
    setSaveSuccess(null);
    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const themeIdNumber = Number(editingTheme.id);
      const payload = {
        Id: Number.isFinite(themeIdNumber) ? themeIdNumber : null,
        ThemeName: trimmedName,
        Description: '',
        DictThemeDetails: toThemeDetailsDict(formTheme),
        IsForAllUsers: false,
      };
      const saveResult = await saveOneUserDefinedTheme(payload);
      const updatedThemeId = saveResult?.Object?.Id ?? payload.Id ?? editingTheme.id;
      if (onThemeSaved) {
        await onThemeSaved(String(updatedThemeId));
      }
      setSaveSuccess('Theme saved successfully.');
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : 'Failed to save theme.');
    } finally {
      setIsSaving(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, editingTheme.id, formTheme, hasErrors, onThemeSaved, themeName]);

  const renderField = (field: FieldMeta) => {
    const value = formTheme[field.key] ?? '';
    const error = fieldErrors[field.key as string];
    const preview = field.type === 'text' ? null : colorPreview(value);

    return (
      <div key={field.key as string} className={`border-b last:border-b-0 ${t('border_default')}`}>
        <div className="flex items-center gap-2 px-2.5 py-1.5">
          <div className="w-36 shrink-0 text-[10px] font-semibold uppercase tracking-wide opacity-70">
            {field.label}
          </div>
          <div className="flex w-1 flex-auto items-center gap-1.5">
            {field.type === 'color' ? (
              <>
                <input
                  type="color"
                  value={hexColorRegex.test(value) ? value : '#000000'}
                  onChange={(event) => handleFieldChange(field.key, event.target.value, field.type)}
                  className="h-6 w-6 cursor-pointer rounded border border-white/10 p-0"
                  title="Pick a color (updates as hex)"
                />
                <input
                  type="text"
                  value={value}
                  onChange={(event) => handleFieldChange(field.key, event.target.value, field.type)}
                  className={`w-1 flex-auto rounded px-2 py-1 text-[11px] border focus:outline-none focus:ring ${
                    error ? 'ring-1 ring-red-500 border-red-500' : 'ring-blue-500/40'
                  } ${theme.inputBox}`}
                  placeholder="#0162e8, slate-500, [rgba(0,0,0,0.5)]"
                />
                {preview && (
                  <span
                    className="h-5 w-5 rounded border border-white/10 shadow-inner"
                    style={{ background: preview }}
                    title={preview}
                  />
                )}
              </>
            ) : (
              <input
                type="text"
                value={value}
                onChange={(event) => handleFieldChange(field.key, event.target.value, field.type)}
                className={`flex-1 rounded px-2 py-1 text-[11px] border focus:outline-none focus:ring ${
                  error ? 'ring-1 ring-red-500 border-red-500' : 'ring-blue-500/40'
                } ${theme.inputBox}`}
              />
            )}
          </div>
        </div>
        {(field.help || error) && (
          <div className="px-2.5 pb-1.5 text-[10px]">
            {field.help && <span className="opacity-70">{field.help}</span>}
            {error && <span className="text-red-500">{error}</span>}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className={`absolute inset-0 z-60 flex flex-col shadow-2xl ${theme.mainContentSection}`}>
      <div className={`relative flex items-center justify-between border-b px-4 py-3 pr-16 ${t('border_default')} ${theme.mainHeader}`}>
        <div>
          <h3 className="text-lg font-semibold">Theme Editor</h3>
          <p className="text-xs opacity-70">Editing theme: {themeName}</p>
        </div>
        <div className="flex items-center gap-2">
         
          <button
            onClick={handleRevertChanges}
            className="w-8 h-6 rounded-[4px] bg-orange-500 text-white text-xs hover:bg-orange-600 flex items-center justify-center"
            title="Refresh"
          >
            <i className="fa fa-refresh" aria-hidden="true" />
          </button>
          <button
            onClick={handleSave}
            disabled={hasErrors || !themeName.trim() || isSaving}
            className={`w-8 h-6 rounded-[4px] text-xs flex items-center justify-center ${
              hasErrors || !themeName.trim() || isSaving
                ? 'bg-green-500/40 text-white/60 cursor-not-allowed'
                : 'bg-green-500 text-white hover:bg-green-600'
            }`}
            title="Save theme"
          >
            <i className={`fa ${isSaving ? 'fa-spinner fa-spin' : 'fa-save'}`} aria-hidden="true" />
          </button>
          <button
            onClick={() => handleResetToBase('light')}
            className="w-8 h-6 rounded-[4px] bg-amber-500 text-white text-xs hover:bg-amber-600 flex items-center justify-center"
            title="Reset to light theme"
          >
            <i className="fa fa-sun" aria-hidden="true" />
          </button>
          <button
            onClick={() => handleResetToBase('dark')}
            className="w-8 h-6 rounded-[4px] bg-slate-600 text-white text-xs hover:bg-slate-700 flex items-center justify-center"
            title="Reset to dark theme"
          >
            <i className="fa fa-moon" aria-hidden="true" />
          </button>
        </div>
        <button
          onClick={onClose}
          className={`absolute right-3 top-3 flex h-7 w-7 items-center justify-center rounded-full text-sm ${theme.button_default}`}
          title="Close"
        >
          <i className="fa fa-times" aria-hidden="true" />
        </button>
      </div>

      <div className={`flex flex-col gap-1.5 border-b px-4 py-2 ${t('border_default')}`}>
        <div className="flex flex-col gap-1">
          <label className="text-[11px] font-semibold uppercase opacity-80">Theme Name</label>
          <input
            type="text"
            value={themeName}
            onChange={(event) => setThemeName(event.target.value)}
            className={`w-full rounded px-3 py-1 text-[13px] border focus:outline-none focus:ring ring-blue-500/40 ${theme.inputBox}`}
            placeholder="Theme name"
          />
        </div>
        {(saveError || saveSuccess) && (
          <div className="flex items-center gap-2 text-[11px]">
            {saveError && <span className="text-red-500">{saveError}</span>}
            {saveSuccess && <span className="text-green-500">{saveSuccess}</span>}
          </div>
        )}
      </div>

      <div className="h-1 flex-auto overflow-y-auto px-2.5 py-2.5 space-y-2.5">
        {FIELD_GROUPS.map((group) => (
          <details key={group.id} open>
            <summary className="cursor-pointer text-[12px] font-semibold uppercase opacity-80">
              {group.title}
            </summary>
            {group.description && (
              <p className="mb-2 text-[10px] opacity-70">{group.description}</p>
            )}
            <div
              className={`mt-2 overflow-hidden rounded border shadow-sm ${t('border_default')} ${theme.mainContentSection}`}
            >
              {group.fields.map((field) => renderField(field))}
            </div>
          </details>
        ))}
      </div>
    </div>
  );
};

export default ThemeEditorPanel;