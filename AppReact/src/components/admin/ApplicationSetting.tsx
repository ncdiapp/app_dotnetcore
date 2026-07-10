import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useDispatch } from 'react-redux';
import { ComboBox } from '@mescius/wijmo.react.input';
import * as wjInput from '@mescius/wijmo.input';
import '@mescius/wijmo.styles/wijmo.css';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useEnumValues } from '../../hooks/useEnumDictionary';

type ApplicationSettingItem = any;
type SettingGroup = { category: string | number; items: ApplicationSettingItem[] };

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
  const categoryEnumMap = useEnumValues('EmAppApplicationSettingCategory');
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
      const map = new Map<string | number, ApplicationSettingItem[]>();

      const resolveCategoryValue = (item: ApplicationSettingItem): string | number => {
        const rawCandidate =
          item?.ApplicationSettingCategory ??
          item?.Category ??
          item?.Description ??
          'General';

        if (typeof rawCandidate === 'number' && Number.isFinite(rawCandidate)) {
          return rawCandidate;
        }

        if (typeof rawCandidate === 'string') {
          const trimmed = rawCandidate.trim();
          if (trimmed === '') {
            return 'General';
          }

          if (categoryEnumMap && categoryEnumMap[trimmed] !== undefined) {
            return categoryEnumMap[trimmed];
          }

          const numericCandidate = Number(trimmed);
          if (Number.isFinite(numericCandidate)) {
            return numericCandidate;
          }

          return trimmed;
        }

        return 'General';
      };

      list.forEach((item) => {
        const categoryValue = resolveCategoryValue(item);
        if (!map.has(categoryValue)) {
          map.set(categoryValue, []);
        }
        map.get(categoryValue)!.push(item);
      });

      const toNumeric = (value: string | number): number | null => {
        if (typeof value === 'number' && Number.isFinite(value)) {
          return value;
        }
        const numericValue = Number(value);
        return Number.isFinite(numericValue) ? numericValue : null;
      };

      return Array.from(map.entries())
        .map(([category, items]) => ({
          category,
          items: [...items].sort((a, b) => (a.SetupCode || '').localeCompare(b.SetupCode || '')),
        }))
        .sort((a, b) => {
          const aNum = toNumeric(a.category);
          const bNum = toNumeric(b.category);

          if (aNum !== null && bNum !== null) {
            return aNum - bNum;
          }
          if (aNum !== null) {
            return -1;
          }
          if (bNum !== null) {
            return 1;
          }
          return String(a.category ?? '').localeCompare(String(b.category ?? ''));
        });
    },
    [categoryEnumMap],
  );

  const groupedSettings = useMemo(() => prepareGroups(settings), [prepareGroups, settings]);

  const filteredGroupedSettings = useMemo(
    () =>
      groupedSettings.filter(
        ({ items }) => !(items.length === 1 && items[0]?.SetupCode === 'AppVersion'),
      ),
    [groupedSettings],
  );

  const resolveCategoryLabel = useCallback(
    (categoryValue: string | number | null | undefined) => {
      const fallbackLabel = categoryValue ?? 'General';
      if (!categoryEnumMap) {
        return fallbackLabel as string;
      }

      const humanizeKey = (key: string) =>
        key.replace(/_/g, ' ').replace(/([a-z0-9])([A-Z])/g, '$1 $2').trim();

      const numericValue =
        typeof categoryValue === 'number' ? categoryValue : Number(categoryValue);

      if (Number.isFinite(numericValue)) {
        const matchedEntry = Object.entries(categoryEnumMap).find(
          ([, value]) => value === numericValue,
        );
        if (matchedEntry) {
          return humanizeKey(matchedEntry[0]);
        }
      }

      if (typeof categoryValue === 'string' && categoryEnumMap[categoryValue] !== undefined) {
        return humanizeKey(categoryValue);
      }

      if (typeof categoryValue === 'string') {
        const caseInsensitiveKey = Object.keys(categoryEnumMap).find(
          (enumKey) => enumKey.toLowerCase() === categoryValue.toLowerCase(),
        );
        if (caseInsensitiveKey) {
          return humanizeKey(caseInsensitiveKey);
        }
      }

      return fallbackLabel as string;
    },
    [categoryEnumMap],
  );

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
        <div className="text-sm font-semibold tracking-wide">
          System Setting
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 flex items-center justify-center"
            onClick={handleRefresh}
            title="Refresh"
          >
            <i className="fa fa-refresh" />
          </button>
          <button
            type="button"
            disabled={isSaving}
            onClick={handleSave}
            className="w-8 h-6 text-center bg-orange-400 text-white rounded-[4px] text-xs hover:bg-orange-500 disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center"
            title="Save"
          >
            {isSaving ? (
              <i className="fa fa-spinner fa-spin" />
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
              <i className="fa fa-database" />
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
        {filteredGroupedSettings.map(({ category, items }) => {
          const categoryDisplayName = resolveCategoryLabel(category);

          return (
            <section key={category} className={`${theme.mainContentSection} rounded-lg border px-4 py-3`}>
              <header className={`mb-3 text-sm font-semibold tracking-wide ${theme.label}`}>
                {categoryDisplayName}
              </header>
              <div className="flex flex-col gap-3">
                {items.map((item) => (
                  <div key={item.SetupCode} className="flex items-center gap-3 text-sm">
                    <label className={`w-[400px] pl-5 truncate select-none text-xs tracking-wide ${theme.label}`}>
                      {item.SetupCode}
                    </label>
                    <div className="w-[400px]">{renderField(item)}</div>

                    {Boolean(item.IsReadOnly) && (
                      <span
                        className="text-xs tracking-wide text-slate-400"
                        title="Read Only"
                      >
                        <i className="fa fa-lock"></i>
                      </span>
                    )}
                  </div>
                ))}
              </div>
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

