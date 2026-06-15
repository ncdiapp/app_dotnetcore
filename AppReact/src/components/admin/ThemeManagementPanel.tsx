import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import {
  refreshUserDefinedThemesCache,
  saveOneUserDefinedTheme,
  deleteOneUserDefinedTheme,
  retrieveOneUserDefinedTheme,
} from '../../helper/themeHelper';
import type { ThemeOption } from '../../redux/features/ui/theme/types';
import ThemeEditorPanel from './ThemeEditorPanel';
import { useTheme } from '../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { setAvailableThemes, setThemeById } from '../../redux/features/ui/theme/themeSlice';

interface ThemeManagementPanelProps {
  onClose: () => void;
}

const ThemeManagementPanel: React.FC<ThemeManagementPanelProps> = ({ onClose }) => {
  const dispatch = useDispatch();
  const availableThemes = useSelector((state: RootState) => state.theme.availableThemes);
  const currentThemeId = useSelector((state: RootState) => state.theme.currentThemeId);
  const { theme, t } = useTheme();
  const [, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [createError, setCreateError] = useState<string | null>(null);
  const builtInThemes = useMemo(
    () => availableThemes.filter((theme) => theme.id === 'light' || theme.id === 'dark'),
    [availableThemes]
  );
  const mergeWithBuiltIns = useCallback(
    (userThemes: ThemeOption[]) => {
      const builtInsMap = new Map(builtInThemes.map((theme) => [theme.id, theme]));
      const merged = [...builtInThemes];
      userThemes.forEach((theme) => {
        if (!builtInsMap.has(theme.id)) {
          merged.push(theme);
        }
      });
      return merged;
    },
    [builtInThemes]
  );
  const [themes, setThemes] = useState<ThemeOption[]>(() => mergeWithBuiltIns(availableThemes));
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [selectedBaseThemeId, setSelectedBaseThemeId] = useState<string>('light');
  const [newThemeName, setNewThemeName] = useState<string>('');
  const [editorTheme, setEditorTheme] = useState<ThemeOption | null>(null);
  const [isEditorOpen, setIsEditorOpen] = useState(false);
  const [isSavingTheme, setIsSavingTheme] = useState(false);

  const refreshThemes = useCallback(async (): Promise<ThemeOption[]> => {
    setIsLoading(true);
    let merged: ThemeOption[] = [];
    try {
      const result = await refreshUserDefinedThemesCache();
      merged = mergeWithBuiltIns(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      merged = mergeWithBuiltIns([]);
    } finally {
      setIsLoading(false);
    }
    setThemes(merged);
    dispatch(setAvailableThemes(merged));
    return merged;
  }, [dispatch, mergeWithBuiltIns]);

  useEffect(() => {
    refreshThemes();
  }, [refreshThemes]);

  const handleEditTheme = async (themeId: string) => {
    if (themeId === 'light' || themeId === 'dark') {
      setError('Built-in themes cannot be edited. Please create a new theme from a base theme.');
      return;
    }
    dispatch(setIsBusy());
    try {
      const dto = await retrieveOneUserDefinedTheme(Number(themeId));
      if (!dto) {
        setError('Unable to load theme.');
        return;
      }
      const themeOption: ThemeOption = {
        id: String(dto.id ?? themeId),
        name: dto.name,
        theme: dto.theme,
      };
      dispatch(setThemeById(themeOption.id));
      setEditorTheme(themeOption);
      setIsEditorOpen(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleDeleteTheme = async (themeId: string) => {
    if (themeId === 'light' || themeId === 'dark') {
      setError('Built-in themes cannot be deleted.');
      return;
    }
    dispatch(setIsBusy());
    try {
      await deleteOneUserDefinedTheme(Number(themeId));
      await refreshThemes();
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const openCreateModal = () => {
    setError(null);
    setCreateError(null);
    setNewThemeName('');
    setSelectedBaseThemeId('light');
    setShowCreateModal(true);
  };

  const closeCreateModal = () => {
    setShowCreateModal(false);
    setNewThemeName('');
    setSelectedBaseThemeId('light');
  };

  const handleCreateTheme = async () => {
    setError(null);
    setCreateError(null);
    const trimmedName = newThemeName.trim();
    if (!trimmedName) {
      setCreateError('Theme name is required.');
      return;
    }
    if (themes.some((theme) => theme.name.toLowerCase() === trimmedName.toLowerCase())) {
      setCreateError('A theme with that name already exists.');
      return;
    }
    const baseTheme =
      themes.find((theme) => theme.id === selectedBaseThemeId) ??
      builtInThemes.find((theme) => theme.id === selectedBaseThemeId);
    if (!baseTheme) {
      setError('Selected base theme not found.');
      return;
    }
    dispatch(setIsBusy());
    setIsSavingTheme(true);
    try {
      const baseThemeCopy = JSON.parse(JSON.stringify(baseTheme.theme));
      const payload = {
        ThemeName: trimmedName,
        Description: '',
        DictThemeDetails: baseThemeCopy,
        IsForAllUsers: false,
      };
      const saveResult = await saveOneUserDefinedTheme(payload);
      const merged = await refreshThemes();
      closeCreateModal();
      const createdThemeId = saveResult?.Object?.Id ?? null;
      const createdTheme =
        merged.find((theme) => (createdThemeId ? theme.id === String(createdThemeId) : false)) ??
        merged[merged.length - 1] ??
        null;
      if (!createdTheme) {
        setError('Theme saved, but could not find it in the refreshed list.');
        return;
      }
      setEditorTheme(createdTheme);
      setIsEditorOpen(true);
    } catch (err) {
      setCreateError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsSavingTheme(false);
      dispatch(setIsNotBusy());
    }
  };

  const handleCloseEditor = () => {
    setIsEditorOpen(false);
    setEditorTheme(null);
  };

  const handleThemeSaved = useCallback(
    async (updatedThemeId: string) => {
      const merged = await refreshThemes();
      const refreshed = merged.find((themeOption) => themeOption.id === updatedThemeId);
      if (refreshed) {
        setEditorTheme(refreshed);
        dispatch(setThemeById(updatedThemeId));
      }
    },
    [dispatch, refreshThemes]
  );

  const isBuiltInTheme = (themeId: string) => themeId === 'light' || themeId === 'dark';

  return (
    <div className={`fixed inset-y-0 right-0 z-40 w-full max-w-md shadow-xl ${theme.mainContentSection}`}>
      <div className="relative flex h-full flex-col">
        <div className={`relative flex items-center justify-between border-b px-4 py-3 pr-16 ${t('border_default')}`}>
          <h2 className="text-lg font-semibold">Theme Management</h2>
          <div className="flex items-center gap-2">
            <button
              onClick={async () => {
                dispatch(setIsBusy());
                try {
                  await refreshThemes();
                } finally {
                  dispatch(setIsNotBusy());
                }
              }}
              className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center justify-center"
              title="Refresh"
            >
              <i className="fa fa-refresh" aria-hidden="true"></i>
            </button>
            <button
              onClick={openCreateModal}
              className="w-8 h-6 bg-green-500 text-white rounded-[4px] text-xs hover:bg-green-600 flex items-center justify-center"
              title="Create Theme"
            >
              <i className="fa fa-plus" aria-hidden="true"></i>
            </button>
          </div>
          <button
            onClick={onClose}
            className={`absolute right-3 top-3 flex h-7 w-7 items-center justify-center rounded-full text-sm ${theme.button_default}`}
            title="Close"
          >
            <i className="fa fa-times" aria-hidden="true"></i>
          </button>
        </div>
        <div className="h-1 flex-auto overflow-y-auto p-4 text-sm">
          {error && <p className="text-xs text-red-500">{error}</p>}

          <div className="space-y-3">
            {themes.map((themeOption) => (
              <div
                key={themeOption.id}
                className={`rounded border p-3 ${t('border_default')} ${theme.mainContentSection}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <div className="flex h-6 w-6 items-center justify-center rounded-full bg-blue-500/10 text-blue-600">
                      <i className="fa fa-palette text-xs" aria-hidden="true" />
                    </div>
                    <div className="font-medium">{themeOption.name}</div>
                  </div>
                  <div className="flex items-center gap-2">
                    {isBuiltInTheme(themeOption.id) ? (
                      <button
                        onClick={() => dispatch(setThemeById(themeOption.id))}
                        className={`flex h-5 w-5 items-center justify-center rounded-full text-[9px] shadow-sm ${
                          currentThemeId === themeOption.id
                            ? 'bg-emerald-500 text-white hover:bg-emerald-600'
                            : 'bg-gray-500/30 text-white/80 hover:bg-slate-500'
                        }`}
                        aria-label="Use theme"
                        title="Activate theme"
                      >
                        <i className="fa fa-check" aria-hidden="true" />
                      </button>
                    ) : (
                      <>
                        <button
                          onClick={() => handleEditTheme(themeOption.id)}
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-blue-500 text-white text-[9px] shadow-sm hover:bg-blue-600"
                          aria-label="Edit theme"
                          title="Edit theme"
                        >
                          <i className="fa fa-pencil" aria-hidden="true" />
                        </button>
                        <button
                          onClick={() => handleDeleteTheme(themeOption.id)}
                          className="flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-white text-[9px] shadow-sm hover:bg-red-600"
                          aria-label="Delete theme"
                          title="Delete theme"
                        >
                          <i className="fa fa-trash" aria-hidden="true" />
                        </button>
                        <button
                          onClick={() => dispatch(setThemeById(themeOption.id))}
                          className={`flex h-5 w-5 items-center justify-center rounded-full text-[9px] shadow-sm ${
                            currentThemeId === themeOption.id
                              ? 'bg-emerald-500 text-white hover:bg-emerald-600'
                              : 'bg-gray-500/30 text-white/80 hover:bg-slate-500'
                          }`}
                          aria-label="Use theme"
                          title="Activate theme"
                        >
                          <i className="fa fa-check" aria-hidden="true" />
                        </button>
                      </>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {showCreateModal && (
          <div className="absolute inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
            <div className={`w-full max-w-sm rounded-lg p-5 shadow-xl ${theme.mainContentSection}`}>
              <h3 className="text-base font-semibold">Create Theme</h3>
              <div className="mt-4 space-y-3">
                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold uppercase opacity-80">
                    New Theme Name
                  </label>
                  <input
                    type="text"
                    value={newThemeName}
                    onChange={(event) => setNewThemeName(event.target.value)}
                    placeholder="e.g. My Custom Theme"
                    className={`rounded px-3 py-2 text-sm border focus:outline-none ${theme.inputBox}`}
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <label className="text-xs font-semibold uppercase opacity-80">
                    Base Theme
                  </label>
                  <div className="relative">
                    <select
                      value={selectedBaseThemeId}
                      onChange={(event) => setSelectedBaseThemeId(event.target.value)}
                      className={`w-full appearance-none rounded border pl-3 pr-8 py-2 text-sm focus:outline-none ${theme.inputBox}`}
                    >
                      {builtInThemes.map((builtIn) => (
                        <option key={builtIn.id} value={builtIn.id}>
                          {builtIn.name}
                        </option>
                      ))}
                    </select>
                    <span className="pointer-events-none absolute inset-y-0 right-2 flex items-center text-xs opacity-70">
                      <i className="fa fa-chevron-down" aria-hidden="true" />
                    </span>
                  </div>
                </div>

                {createError && (
                  <p className="rounded border border-red-400 bg-red-50 px-3 py-2 text-xs text-red-600 dark:border-red-500/50 dark:bg-red-500/10 dark:text-red-200">
                    {createError}
                  </p>
                )}
              </div>
              <div className="mt-6 flex justify-end gap-2">
                <button
                  onClick={closeCreateModal}
                  className={`rounded px-3 py-2 text-xs ${theme.button_secondary}`}
                  disabled={isSavingTheme}
                >
                  Cancel
                </button>
                <button
                  onClick={handleCreateTheme}
                  className={`inline-flex items-center justify-center rounded px-3 py-2 text-xs font-medium ${theme.button_default}`}
                  disabled={isSavingTheme}
                >
                  Create
                </button>
              </div>
            </div>
          </div>
        )}

        {isEditorOpen && editorTheme && (
          <ThemeEditorPanel
            theme={editorTheme}
            onClose={handleCloseEditor}
            onThemeSaved={handleThemeSaved}
          />
        )}
      </div>
    </div>
  );
};

export default ThemeManagementPanel;

