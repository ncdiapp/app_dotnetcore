import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { ComboBox } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';

type ApplicationSettingItem = any;
type SettingSubGroup = { subCategory: string; items: ApplicationSettingItem[] };
type SettingGroup = { category: string; subGroups: SettingSubGroup[] };

/** Split SetupCode and wrap filter matches for highlight. */
const renderHighlightedSetupCode = (setupCode: string, filter: string) => {
  const text = setupCode || '';
  const q = filter.trim();
  if (!q || !text) return text;

  const lower = text.toLowerCase();
  const needle = q.toLowerCase();
  const parts: React.ReactNode[] = [];
  let start = 0;
  let idx = lower.indexOf(needle, start);
  let key = 0;
  while (idx !== -1) {
    if (idx > start) parts.push(text.slice(start, idx));
    parts.push(
      <mark
        key={`m-${key++}`}
        className="bg-amber-200 text-inherit rounded-sm px-0.5"
      >
        {text.slice(idx, idx + needle.length)}
      </mark>,
    );
    start = idx + needle.length;
    idx = lower.indexOf(needle, start);
  }
  if (start < text.length) parts.push(text.slice(start));
  return parts;
};

const ApplicationSettingValueType = Object.freeze({
  Unknown: 0,
  Integer: 1,
  List: 2,
  Boolean: 3,
  Text: 4,
  Password: 5,
  ProductFolder: 101,
  EntityFolder: 102,
  ImageFolder: 103,
  FileFolder: 104,
  ProjectFolder: 105,
});

const resolveUsageType = (item: ApplicationSettingItem) => {
  if (!item) {
    return ApplicationSettingValueType.Text;
  }
  const { UsageType } = item;
  const numericUsage = Number(UsageType);
  return Number.isFinite(numericUsage) ? numericUsage : ApplicationSettingValueType.Text;
};

/** ObservableSet from API may be a plain array or { InternalItems: [...] }. */
const unwrapAppSetupList = (data: unknown): ApplicationSettingItem[] => {
  if (Array.isArray(data)) {
    return data;
  }
  if (data && typeof data === 'object' && Array.isArray((data as any).InternalItems)) {
    return (data as any).InternalItems;
  }
  return [];
};

const buildAppSetupSavePayload = (items: ApplicationSettingItem[]) => {
  const modifiedItems = items
    .filter((item) => Boolean(item?.IsModified))
    .map((item) => {
      const usageType = resolveUsageType(item);
      let valueForSave: any = item.SetupValue;
      if (usageType === ApplicationSettingValueType.Boolean) {
        if (typeof item.SetupValue === 'string') {
          valueForSave = item.SetupValue === 'True' ? 'True' : 'False';
        } else {
          valueForSave = item.SetupValue ? 'True' : 'False';
        }
      }
      return {
        Id: item.Id,
        SetupCode: item.SetupCode,
        SetupValue: valueForSave == null || valueForSave === '' ? '' : String(valueForSave),
        Description: item.Description,
        EntityId: item.EntityId ?? null,
        UsageType: item.UsageType ?? null,
        IsModified: true,
      };
    });

  return {
    DeletedItemIds: [] as unknown[],
    InternalItems: modifiedItems,
  };
};

const extractErrorMessages = (validationResult: any): string[] => {
  if (!validationResult) {
    return [];
  }
  if (Array.isArray(validationResult)) {
    return validationResult
      .map((item) => item?.ErrorMessage || item?.Message || '')
      .filter(Boolean);
  }
  if (Array.isArray(validationResult?.Errors)) {
    return validationResult.Errors.map((err: any) => err?.ErrorMessage || err?.Message || '').filter(Boolean);
  }
  if (typeof validationResult === 'string') {
    return [validationResult];
  }
  return [];
};

const ApplicationSetting: React.FC = () => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const errorMessage = useErrorMessage();

  const [serverSettings, setServerSettings] = useState<any>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [settings, setSettings] = useState<ApplicationSettingItem[]>([]);
  const [isCacheMenuOpen, setIsCacheMenuOpen] = useState(false);
  const [collapsedCategories, setCollapsedCategories] = useState<Set<string>>(() => new Set());
  const [setupCodeFilter, setSetupCodeFilter] = useState('');
  const cacheMenuRef = useRef<HTMLDivElement | null>(null);
  const installedDbDriverRows = serverSettings?.InstalledDbDriver?.DataRowList ?? [];

  const installedDbDriverColumns = useMemo(() => {
    if (!installedDbDriverRows.length) {
      return [];
    }

    const columnList = serverSettings?.InstalledDbDriver?.ColumnList;
    if (Array.isArray(columnList) && columnList.length > 0) {
      return columnList
        .map((column: any) => column?.ColumnName || column?.Name || column)
        .filter(Boolean);
    }

    const keys = new Set<string>();
    installedDbDriverRows.forEach((row: Record<string, any>) => {
      Object.keys(row || {}).forEach((key) => keys.add(key));
    });
    return Array.from(keys);
  }, [installedDbDriverRows, serverSettings]);

  const normalizeSettingValue = (item: ApplicationSettingItem) => {
    if (!item) {
      return '';
    }
    const usageType = resolveUsageType(item);

    if (usageType === ApplicationSettingValueType.List) {
      return parseInt(item.SetupValue) || null;
    }
    
    return item.SetupValue || null;
  };

  const prepareGroups = useCallback(
    (list: ApplicationSettingItem[]): SettingGroup[] => {
      const categoryMap = new Map<string, Map<string, ApplicationSettingItem[]>>();

      const resolveCategoryValue = (item: ApplicationSettingItem): string => {
        const raw =
          item?.Category ??
          item?.ApplicationSettingCategory ??
          item?.Description ??
          'General';
        if (typeof raw === 'number' && Number.isFinite(raw)) return String(raw);
        if (typeof raw === 'string') {
          const trimmed = raw.trim();
          return trimmed === '' ? 'General' : trimmed;
        }
        return 'General';
      };

      const resolveSubCategoryValue = (item: ApplicationSettingItem): string => {
        const raw = item?.SubCategory;
        if (typeof raw === 'string' && raw.trim() !== '') return raw.trim();
        return '';
      };

      list.forEach((item) => {
        const categoryValue = resolveCategoryValue(item);
        const subCategoryValue = resolveSubCategoryValue(item);
        if (!categoryMap.has(categoryValue)) categoryMap.set(categoryValue, new Map());
        const subMap = categoryMap.get(categoryValue)!;
        if (!subMap.has(subCategoryValue)) subMap.set(subCategoryValue, []);
        subMap.get(subCategoryValue)!.push(item);
      });

      const sortLabel = (a: string, b: string) => a.localeCompare(b);

      return Array.from(categoryMap.entries())
        .map(([category, subMap]) => ({
          category,
          subGroups: Array.from(subMap.entries())
            .map(([subCategory, items]) => ({
              subCategory,
              items: [...items].sort((a, b) => (a.SetupCode || '').localeCompare(b.SetupCode || '')),
            }))
            .sort((a, b) => {
              if (!a.subCategory) return -1;
              if (!b.subCategory) return 1;
              return sortLabel(a.subCategory, b.subCategory);
            }),
        }))
        .sort((a, b) => sortLabel(a.category, b.category));
    },
    [],
  );

  const groupedSettings = useMemo(() => prepareGroups(settings), [prepareGroups, settings]);

  const filteredGroupedSettings = useMemo(() => {
    const withoutAppVersion = groupedSettings.filter(({ subGroups }) => {
      const items = subGroups.flatMap((g) => g.items);
      return !(items.length === 1 && items[0]?.SetupCode === 'AppVersion');
    });
    const q = setupCodeFilter.trim().toLowerCase();
    if (!q) return withoutAppVersion;
    return withoutAppVersion
      .map((g) => ({
        ...g,
        subGroups: g.subGroups
          .map((sg) => ({
            ...sg,
            items: sg.items.filter((item) =>
              String(item.SetupCode || '').toLowerCase().includes(q),
            ),
          }))
          .filter((sg) => sg.items.length > 0),
      }))
      .filter((g) => g.subGroups.length > 0);
  }, [groupedSettings, setupCodeFilter]);

  const resolveCategoryLabel = useCallback((categoryValue: string | null | undefined) => {
    const label = (categoryValue ?? 'General').trim();
    return label === '' ? 'General' : label;
  }, []);

  const allCategoriesCollapsed =
    filteredGroupedSettings.length > 0 &&
    filteredGroupedSettings.every((g) => collapsedCategories.has(g.category));

  const toggleExpandCollapseAll = useCallback(() => {
    if (allCategoriesCollapsed) setCollapsedCategories(new Set());
    else setCollapsedCategories(new Set(filteredGroupedSettings.map((g) => g.category)));
  }, [allCategoriesCollapsed, filteredGroupedSettings]);

  const prepareAppSetupDtoList = (items: ApplicationSettingItem[]) => {

    const emptyValueDtoList = (items || []).map((item) => ({
      ...item,
      SetupValue: '',
    }));
    setSettings(emptyValueDtoList);

    // Important: reset selection to '' first, then restore actual values in the next tick.
    // This mirrors the Wijmo ComboBox rule (Prompt/ConverterAnularJsPagePrompt.txt) so the component
    // doesn’t auto-select the first item when the item source refreshes.
    setTimeout(() => {
      const normalizedList = (items || []).map((item) => ({
        ...item,
        SetupValue: normalizeSettingValue(item),
      }));
      setSettings(normalizedList);
    }, 0);

  }



  const loadDataFromServer = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [appSetupDtoList, serverSettingDto] = await Promise.all([
        adminSvc.retrieveAllAppSetupDtoList(false),
        adminSvc.checkServerSetting(),
      ]);
      prepareAppSetupDtoList(unwrapAppSetupList(appSetupDtoList));
      setServerSettings(serverSettingDto);
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());

    }
  }, [dispatch]);

  useEffect(() => {
    loadDataFromServer();
  }, [loadDataFromServer]);

  const updateSetting = useCallback((setupCode: string, changes: Partial<ApplicationSettingItem>) => {
    if (!changes || Object.keys(changes).length === 0) {
      return;
    }

    setSettings((prev) => {
      let didUpdate = false;
      const next = prev.map((item) => {
        if (item.SetupCode !== setupCode) {
          return item;
        }

        const hasDifference = Object.entries(changes).some(([key, value]) => {
          return (item as any)[key] !== value;
        });

        if (!hasDifference) {
          return item;
        }

        didUpdate = true;
        return {
          ...item,
          ...changes,
          IsModified: true,
        };
      });

      return didUpdate ? next : prev;
    });
  }, []);

  const handleBooleanChange = useCallback(
    (setupCode: string, checked: boolean) => {
      updateSetting(setupCode, { SetupValue: checked? 'True' : 'False', IsModified: true });
    },
    [updateSetting]
  );

  const handleTextChange = useCallback(
    (setupCode: string, value: string) => {
      updateSetting(setupCode, { SetupValue: value });
    },
    [updateSetting]
  );

  const _handleNumberChange = useCallback(
    (setupCode: string, value: number | null) => {
      updateSetting(setupCode, { SetupValue: value });
    },
    [updateSetting]
  );

  const handleListChange = useCallback(
    (setupCode: string, value: any) => {
      updateSetting(setupCode, { SetupValue: value || null });
    },
    [updateSetting]
  );

  const handleRefresh = async () => {
    await loadDataFromServer();
  };

  const handleSave = async () => {
    const payload = buildAppSetupSavePayload(settings);
    if (!payload.InternalItems.length) {
      errorMessage.showWarning('No changes to save.');
      return;
    }

    setIsSaving(true);
    dispatch(setIsBusy());
    try {
      const response = await adminSvc.saveAllAppSetupEntityDto(payload);
      const validationMessages = extractErrorMessages(response?.ValidationResult);

      if (response?.IsSuccessful) {
        await loadDataFromServer();
        if (validationMessages.length) {
          validationMessages.forEach((msg) => errorMessage.showWarning(msg));
        } else {
          errorMessage.showInfo('Application settings saved. Please re-login to apply changes.');
        }
      } else if (validationMessages.length) {
        validationMessages.forEach((msg) => errorMessage.showError(msg));
      } else {
        errorMessage.showError('Failed to save application settings.');
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      setIsSaving(false);
      dispatch(setIsNotBusy());
    }
  };

  const handleToggleCache = async (enable: boolean) => {
    dispatch(setIsBusy());
    try {
      const result = await adminSvc.enableOrDisableCache(enable);
      if (result) {
        // eslint-disable-next-line no-alert
        errorMessage.showInfo('Please re-login to take effect the changes.');
      } else {
        errorMessage.showError('Failed to change system cache setting.');
      }
    } catch (error) {
      errorMessage.showError(error instanceof Error ? error.message : String(error));
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const toggleCacheMenu = () => {
    setIsCacheMenuOpen((prev) => !prev);
  };

  const handleCacheSelection = (enable: boolean) => {
    setIsCacheMenuOpen(false);
    handleToggleCache(enable);
  };

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (cacheMenuRef.current && !cacheMenuRef.current.contains(event.target as Node)) {
        setIsCacheMenuOpen(false);
      }
    };

    if (isCacheMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    } else {
      document.removeEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isCacheMenuOpen]);

  const renderField = (item: ApplicationSettingItem) => {
    const usageType = resolveUsageType(item);
    const isReadOnly = Boolean(item?.IsReadOnly);
    const inputBaseClass = `${theme.inputBox} border w-full px-2 py-1 text-xs focus:outline-none`;

    switch (usageType) {
      case ApplicationSettingValueType.Boolean:
        return (
          <>
          <input
            type="checkbox"
            className="h-4 w-4 border"
            checked={item.SetupValue === 'True'}
            disabled={isReadOnly}
            onChange={(event) => handleBooleanChange(item.SetupCode, Boolean(event.target.checked))}
          />
          </>
        );

      case ApplicationSettingValueType.List:



        if (item?.EntityDataSource?.length > 0) {
          return (
            <>
              {/* <span>{item.SetupValue}</span> */}
              <ComboBox
                itemsSource={item?.EntityDataSource}
                displayMemberPath="Display"
                selectedValuePath="Id"
                selectedValue={item.SetupValue}
                isEditable={false}
                isRequired={false}
                disabled={isReadOnly}
                className={`${theme.inputBox} border w-full`}

              selectedIndexChanged={(sender: wjInput.ComboBox) => {
                handleListChange(item.SetupCode, sender.selectedValue);
              }}
              />
            </>


          );
        }
        else {
          return (
            <ComboBox
              className={`${theme.inputBox} border w-full`}
            />
          );
        }



      case ApplicationSettingValueType.Password:
        return (
          <input
            type="text"
            className={inputBaseClass}
            value={item.SetupValue ?? ''}
            disabled={isReadOnly}
            onChange={(event) => handleTextChange(item.SetupCode, event.target.value ?? '')}
          />
        );
      default:
        return (
          <input
            type="text"
            className={inputBaseClass}
            value={item.SetupValue ?? ''}
            disabled={isReadOnly}
            onChange={(event) => handleTextChange(item.SetupCode, event.target.value ?? '')}
          />
        );
    }
  };

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 mb-1 py-2 ${theme.mainContentSection}`}>
        <div className="flex items-center gap-3">
          <div className="text-sm font-semibold tracking-wide">
            System Setting
          </div>
          <button
            type="button"
            className={`inline-flex items-center gap-1.5 px-2.5 h-7 text-xs rounded-[4px] border ${theme.button_default}`}
            onClick={toggleExpandCollapseAll}
            title={allCategoriesCollapsed ? 'Expand All Categories' : 'Collapse All Categories'}
          >
            <i className={`fa-solid ${allCategoriesCollapsed ? 'fa-angles-down' : 'fa-angles-up'} text-[11px]`} />
            <span>{allCategoriesCollapsed ? 'Expand all' : 'Collapse all'}</span>
          </button>
          <div className="relative ml-3">
            <i className="fa-solid fa-filter pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-[10px] text-slate-400 opacity-70" />
            <input
              type="text"
              className={`w-56 h-7 pl-7 pr-7 text-xs border rounded-[4px] ${theme.inputBox}`}
              placeholder="Filter SetupCode..."
              value={setupCodeFilter}
              onChange={(e) => setSetupCodeFilter(e.target.value)}
              title="Filter by SetupCode"
            />
            {setupCodeFilter.trim() !== '' && (
              <button
                type="button"
                className="absolute right-1 top-1/2 -translate-y-1/2 w-5 h-5 text-[10px] text-slate-400 hover:text-slate-600 flex items-center justify-center"
                onClick={() => setSetupCodeFilter('')}
                title="Clear filter"
              >
                <i className="fa-solid fa-xmark" />
              </button>
            )}
          </div>
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center justify-center"
            onClick={handleRefresh}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" />
          </button>
          <button
            type="button"
            disabled={isSaving}
            onClick={handleSave}
            className="w-8 h-6 text-center bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center"
            title="Save"
          >
            {isSaving ? (
              <i className="fa-solid fa-spinner fa-spin" />
            ) : (
              <svg className="w-[14px] h-[14px] fill-white" viewBox="0 0 448 512">
                <path d="M433.941 129.941l-83.882-83.882A48 48 0 0 0 316.118 32H48C21.49 32 0 53.49 0 80v352c0 26.51 21.49 48 48 48h352c26.51 0 48-21.49 48-48V163.882a48 48 0 0 0-14.059-33.941zM272 80v80H144V80h128zm122 352H54a6 6 0 0 1-6-6V86a6 6 0 0 1 6-6h42v104c0 13.255 10.745 24 24 24h176c13.255 0 24-10.745 24-24V83.882l78.243 78.243a6 6 0 0 1 1.757 4.243V426a6 6 0 0 1-6 6zM224 232c-48.523 0-88 39.477-88 88s39.477 88 88 88 88-39.477 88-88-39.477-88-88-88zm0 128c-22.056 0-40-17.944-40-40s17.944-40 40-40 40 17.944 40 40-17.944 40-40 40z" />
              </svg>
            )}
          </button>
          <div className="relative" ref={cacheMenuRef}>
            <button
              type="button"
              className="w-8 h-6 bg-slate-500 text-white rounded-[4px] text-xs hover:bg-slate-600 flex items-center justify-center"
              onClick={toggleCacheMenu}
              title="Cache Settings"
            >
              <i className="fa-solid fa-database" />
            </button>
            {isCacheMenuOpen && (
              <div className="absolute right-0 mt-1 w-48 divide-y divide-slate-100 rounded-md border bg-white text-xs shadow-lg z-10">
                <button
                  type="button"
                  className="flex w-full items-center px-3 py-2 text-left hover:bg-slate-100"
                  onClick={() => handleCacheSelection(true)}
                >
                  Enable System Cache
                </button>
                <button
                  type="button"
                  className="flex w-full items-center px-3 py-2 text-left hover:bg-slate-100"
                  onClick={() => handleCacheSelection(false)}
                >
                  Disable System Cache
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className={`h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="flex flex-col gap-4 h-full overflow-y-auto px-4 py-3">
        {filteredGroupedSettings.map(({ category, subGroups }) => {
          const categoryDisplayName = resolveCategoryLabel(category);
          const isCollapsed = collapsedCategories.has(category);

          return (
            <section key={category} className={`${theme.mainContentSection} rounded-lg border px-4 py-3`}>
              <header
                className={`flex items-center gap-2 text-sm font-semibold tracking-wide cursor-pointer select-none ${theme.label} ${isCollapsed ? '' : 'mb-3'}`}
                onClick={() => {
                  setCollapsedCategories((prev) => {
                    const next = new Set(prev);
                    if (next.has(category)) next.delete(category);
                    else next.add(category);
                    return next;
                  });
                }}
                title={isCollapsed ? 'Expand' : 'Collapse'}
              >
                <i className={`fa-solid ${isCollapsed ? 'fa-chevron-right' : 'fa-chevron-down'} text-[10px] w-3`} />
                <span>{categoryDisplayName}</span>
              </header>
              {!isCollapsed && (
              <div className="flex flex-col gap-3 pl-4">
                {subGroups.map(({ subCategory, items }) => (
                  <div key={`${category}::${subCategory || '_'}`} className="flex flex-col gap-2">
                    {subCategory ? (
                      <div className={`text-xs font-semibold tracking-wide pt-1 pl-2 ${theme.label}`}>
                        {subCategory}
                      </div>
                    ) : null}
                    <div className={`flex flex-col gap-2 ${subCategory ? 'pl-4' : 'pl-2'}`}>
                      {items.map((item) => (
                        <div key={item.SetupCode} className="flex items-center gap-3 text-sm">
                          <label className={`w-[380px] truncate text-xs tracking-wide ${theme.label}`}>
                            {renderHighlightedSetupCode(item.SetupCode || '', setupCodeFilter)}
                          </label>
                          <div className="w-[400px]">{renderField(item)}</div>

                          {Boolean(item.IsReadOnly) && (
                            <span
                              className="text-xs tracking-wide text-slate-400"
                              title="Read Only"
                            >
                              <i className="fa-solid fa-lock"></i>
                            </span>
                          )}
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
              )}
            </section>
          );
        })}
        {installedDbDriverRows.length > 0 && installedDbDriverColumns.length > 0 && (
          <section className={`${theme.mainContentSection} rounded-lg border px-4 py-3`}>
            <header className={`mb-3 text-sm font-semibold tracking-wide ${theme.label}`}>
              Installed Db Driver
            </header>
            <div className="overflow-auto rounded border bg-white">
              <table className="min-w-full border-collapse text-xs">
                <thead className="bg-slate-50">
                  <tr>
                    {installedDbDriverColumns.map((column) => (
                      <th
                        key={`installed-driver-header-${column}`}
                        className="border-b border-slate-200 px-3 py-2 text-left font-semibold text-slate-600"
                      >
                        {column}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {installedDbDriverRows.map((row: any, rowIndex: number) => (
                    <tr
                      key={`installed-driver-row-${rowIndex}`}
                      className={rowIndex % 2 === 0 ? 'bg-white' : 'bg-slate-50/60'}
                    >
                      {installedDbDriverColumns.map((column) => (
                        <td
                          key={`installed-driver-cell-${rowIndex}-${column}`}
                          className="border-b border-slate-100 px-3 py-2 font-normal text-slate-700"
                        >
                          {row?.[column] ?? ''}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}
        </div>
      </div>
    </div>
  );
};

export default ApplicationSetting;

