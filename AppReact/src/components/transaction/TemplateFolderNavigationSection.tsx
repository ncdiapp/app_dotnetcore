import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useDispatch } from 'react-redux';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { appFolderNavigationService } from '../../webapi/appfoldernavigationsvc';
import { searchSvc } from '../../webapi/searchSvc';

function isFolderIdPath(path: string): boolean {
  const lower = path.toLowerCase();
  return lower === 'folderid' || lower.endsWith('.folderid') || lower.endsWith('_folderid');
}

interface TemplateFolderNavigationSectionProps {
  templateSearchId: number | null;
  templateSearchName?: string;
  searchViewId?: number | null;
  dataSetId?: number | null;
  applicationId?: number | string | null;
  mainItemTransactionIds?: number[];
  onSaved?: () => void;
}

const TemplateFolderNavigationSection: React.FC<TemplateFolderNavigationSectionProps> = ({
  templateSearchId,
  templateSearchName,
  searchViewId,
  dataSetId,
  applicationId,
  mainItemTransactionIds = [],
  onSaved,
}) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();
  const { showValidationMessages, showWarning } = useErrorMessage();

  const [config, setConfig] = useState<any>(null);
  const [hostTransactionId, setHostTransactionId] = useState<number | null>(null);
  const [rootFolderId, setRootFolderId] = useState<number | null>(null);
  const [folderIdFieldPath, setFolderIdFieldPath] = useState('');
  const [folderIdOptions, setFolderIdOptions] = useState<{ path: string; display: string }[]>([]);
  const [isEnableFolderSecurity, setIsEnableFolderSecurity] = useState(false);
  const [allTransactions, setAllTransactions] = useState<any[]>([]);
  const [rootFolderList, setRootFolderList] = useState<any[]>([]);

  const hostOptions = useMemo(() => {
    const mainSet = new Set(mainItemTransactionIds.filter(Boolean));
    const fromMain = allTransactions.filter((t) => mainSet.has(t.Id));
    const others = allTransactions.filter((t) => !mainSet.has(t.Id) && t.TransactionOrganizedType === 1);
    return [...fromMain, ...others];
  }, [allTransactions, mainItemTransactionIds]);

  const loadData = useCallback(async () => {
    if (!templateSearchId) return;
    dispatch(setIsBusy());
    try {
      const [cfg, txList, roots] = await Promise.all([
        appTransactionService.retrieveTemplateFolderNavigationConfig(templateSearchId),
        appTransactionService.retrieveAllAppTransactions(false, '1', false),
        appTransactionService.retrieveAllRootFolderDtoList(1),
      ]);
      setConfig(cfg);
      setHostTransactionId(cfg?.HostTransactionId ?? null);
      setRootFolderId(cfg?.RootFolderId ?? null);
      setIsEnableFolderSecurity(Boolean(cfg?.IsEnableFolderSecurity));
      setAllTransactions(Array.isArray(txList) ? txList : []);
      setRootFolderList(roots);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [templateSearchId, dispatch]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const loadFolderIdFieldOptions = useCallback(async () => {
    if (!dataSetId && !searchViewId) {
      setFolderIdOptions([]);
      setFolderIdFieldPath('');
      return;
    }
    try {
      let viewFieldsList: any[] = [];
      if (searchViewId) {
        const view = await searchSvc.retrieveOneAppSearchViewExDto(String(searchViewId));
        viewFieldsList = Array.isArray(view?.AppSearchViewFieldList) ? view.AppSearchViewFieldList : [];
      }

      let queryColumns: any[] = [];
      if (dataSetId) {
        const cols = await searchSvc.retrieveQueryColumnList(String(dataSetId));
        queryColumns = Array.isArray(cols) ? cols : [];
      }

      if (hostTransactionId) {
        try {
          const tx = await appTransactionService.getOneAppTransactionData(String(hostTransactionId));
          for (const unit of tx?.AppTransactionUnitList ?? []) {
            for (const field of unit?.AppTransactionFieldList ?? []) {
              const path = String(field.DataBaseFieldName ?? field.DisplayName ?? '').trim();
              if (!path) continue;
              queryColumns.push({
                Id: path,
                Display: field.DisplayName ?? field.DataBaseFieldName ?? path,
              });
            }
          }
        } catch {
          // Host transaction fields are optional fallback for folder id selection.
        }
      }

      const optionMap = new Map<string, string>();
      for (const col of queryColumns) {
        const path = String(col.Id ?? col.ColumnId ?? '').trim();
        if (!path) continue;
        const label = col.Display ?? col.Name ?? col.ColumnName ?? path;
        optionMap.set(path, `${path} (${label})`);
      }
      for (const f of viewFieldsList) {
        const path = String(f.SysTableFiledPath ?? '').trim();
        if (path && !optionMap.has(path)) {
          optionMap.set(path, `${path} (${f.DisplayText ?? f.Id ?? ''})`);
        }
      }

      const options = Array.from(optionMap.entries())
        .map(([path, display]) => ({ path, display }))
        .sort((a, b) => {
          const aFolder = isFolderIdPath(a.path);
          const bFolder = isFolderIdPath(b.path);
          if (aFolder !== bFolder) return aFolder ? -1 : 1;
          return a.path.localeCompare(b.path);
        });
      setFolderIdOptions(options);

      const configuredField = viewFieldsList.find((f: any) => f.IsFileFoderId);
      if (configuredField?.SysTableFiledPath) {
        setFolderIdFieldPath(configuredField.SysTableFiledPath);
        return;
      }

      const folderColumn = queryColumns.find((col) => isFolderIdPath(String(col.Id ?? col.ColumnId ?? '')));
      if (folderColumn) {
        setFolderIdFieldPath(String(folderColumn.Id ?? folderColumn.ColumnId ?? ''));
        return;
      }

      setFolderIdFieldPath('');
    } catch {
      setFolderIdOptions([]);
      setFolderIdFieldPath('');
    }
  }, [dataSetId, searchViewId, hostTransactionId]);

  useEffect(() => {
    loadFolderIdFieldOptions();
  }, [loadFolderIdFieldOptions]);

  const persistFolderIdField = async (): Promise<boolean> => {
    if (!folderIdFieldPath) {
      showWarning('Please select a Folder Id field.');
      return false;
    }
    if (!searchViewId) return false;

    const view = await searchSvc.retrieveOneAppSearchViewExDto(String(searchViewId));
    const list = Array.isArray(view?.AppSearchViewFieldList) ? [...view.AppSearchViewFieldList] : [];
    const pathLower = folderIdFieldPath.toLowerCase();
    let targetField = list.find((f: any) => String(f.SysTableFiledPath ?? '').toLowerCase() === pathLower);

    if (!targetField) {
      const maxSort = list.reduce((max, f) => Math.max(max, f.Sort ?? 0), 0);
      targetField = {
        Sort: maxSort + 10,
        DisplayText: folderIdFieldPath,
        SysTableFiledPath: folderIdFieldPath,
        ControlType: 2,
        IsVisible: false,
        IsFileFoderId: true,
      };
      list.push(targetField);
    }

    let changed = !targetField.Id;
    const updatedList = list.map((f: any) => {
      const fieldPath = String(f.SysTableFiledPath ?? '').toLowerCase();
      const isTarget = fieldPath === pathLower;
      const wasFolderId = Boolean(f.IsFileFoderId);
      if (isTarget) {
        if (!wasFolderId || f.IsVisible !== false) {
          changed = true;
          return { ...f, IsFileFoderId: true, IsVisible: false, IsModified: Boolean(f.Id) };
        }
        return f;
      }
      if (wasFolderId) {
        changed = true;
        return { ...f, IsFileFoderId: false, IsModified: true };
      }
      return f;
    });

    if (!changed) return true;

    const data = await searchSvc.saveAppSearchViewExDto({
      ...view,
      AppSearchViewFieldList: updatedList,
      IsModified: true,
    });
    showValidationMessages(data?.ValidationResult ?? null, true);
    const ok = data?.IsSuccessful !== false && data?.ValidationResult?.IsValid !== false;
    if (ok) await loadFolderIdFieldOptions();
    return ok;
  };

  const editSearchView = () => {
    if (!searchViewId || !dataSetId) {
      showWarning('Template default Search View and dataset are required.');
      return;
    }
    const param2 = JSON.stringify({ callBackTabKey: null, saasApplicationId: applicationId || null });
    addTabAndNavigate(
      'search-view-editor',
      'View Editor',
      { id: String(searchViewId), param1: String(dataSetId), param2 },
      true
    );
  };

  const createRootFolder = async () => {
    const host = hostOptions.find((t) => t.Id === hostTransactionId);
    const name = templateSearchName || host?.TransactionName || 'Template Root';
    dispatch(setIsBusy());
    try {
      const data = await appFolderNavigationService.saveAppSeFolder({
        Name: name,
        FolderType: host?.EmAppTransBusinessType ?? 1,
      });
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful && data?.Object?.Id) {
        const roots = await appTransactionService.retrieveAllRootFolderDtoList(1);
        setRootFolderList(roots);
        setRootFolderId(data.Object.Id);
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleSave = async () => {
    if (!templateSearchId) {
      showWarning('Save the template before configuring folder navigation.');
      return;
    }
    if (!hostTransactionId) {
      showWarning('Please select a Host Transaction.');
      return;
    }
    if (!rootFolderId) {
      showWarning('Please select or create a Root Folder.');
      return;
    }
    if (!searchViewId) {
      showWarning('Template default Search View is required.');
      return;
    }
    if (!folderIdFieldPath) {
      showWarning('Please select a Folder Id field.');
      return;
    }
    dispatch(setIsBusy());
    try {
      if (!(await persistFolderIdField())) return;

      const data = await appTransactionService.saveTemplateFolderNavigationConfig({
        TemplateSearchId: templateSearchId,
        HostTransactionId: hostTransactionId,
        RootFolderId: rootFolderId,
        SearchViewId: searchViewId,
        IsEnableFolderSecurity: isEnableFolderSecurity,
      });
      showValidationMessages(data?.ValidationResult ?? null, true);
      if (data?.IsSuccessful) {
        await loadData();
        onSaved?.();
      }
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  return (
    <div className={`border rounded-[4px] p-3 ${theme.mainContentSection}`}>
      <div className={`text-sm font-semibold mb-2 ${theme.title}`}>Folder Navigation</div>
      {config?.IsConfigured && (
        <p className={`text-xs mb-2 ${theme.label}`}>
          Configured on host transaction {config.HostTransactionId}
          {config.TemplateSearchName ? ` — menu: ${config.TemplateSearchName}` : ''}
        </p>
      )}
      <div className="flex flex-col gap-2">
        <div className="flex items-center">
          <label className={`w-32 text-xs ${theme.label} mr-2 flex-none`}>Host Transaction</label>
          <select
            className={`w-64 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            value={hostTransactionId ?? ''}
            onChange={(e) => {
              const nextId = e.target.value ? Number(e.target.value) : null;
              setHostTransactionId(nextId);
            }}
          >
            <option value="">-- Select --</option>
            {hostOptions.map((t) => (
              <option key={t.Id} value={t.Id}>
                {t.TransactionName || t.Name} ({t.Id})
              </option>
            ))}
          </select>
        </div>
        <div className="flex items-center">
          <label className={`w-32 text-xs ${theme.label} mr-2 flex-none`}>Root Folder</label>
          <select
            className={`w-64 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            value={rootFolderId ?? ''}
            onChange={(e) => setRootFolderId(e.target.value ? Number(e.target.value) : null)}
          >
            <option value="">-- Select --</option>
            {rootFolderList.map((f) => (
              <option key={f.Id} value={f.Id}>
                {f.Name} ({f.Id})
              </option>
            ))}
          </select>
          <button type="button" className={`ml-2 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`} onClick={createRootFolder}>
            New Root
          </button>
        </div>
        <div className="flex items-center">
          <label className={`w-32 text-xs ${theme.label} mr-2 flex-none`}>Folder Id Field</label>
          <select
            className={`w-64 h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            value={folderIdFieldPath}
            onChange={(e) => setFolderIdFieldPath(e.target.value)}
            disabled={!dataSetId}
          >
            <option value="">-- Select --</option>
            {folderIdOptions.map((opt) => (
              <option key={opt.path} value={opt.path}>
                {opt.display}
              </option>
            ))}
          </select>
          <button
            type="button"
            className={`ml-2 px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
            onClick={editSearchView}
            disabled={!searchViewId}
          >
            Edit View
          </button>
        </div>
        {!dataSetId && (
          <p className={`text-xs ${theme.label}`}>Select a dataset on the template before configuring folder navigation.</p>
        )}
        {!searchViewId && dataSetId && (
          <p className={`text-xs ${theme.label}`}>Set a default Search View in Template View Fields before saving folder navigation.</p>
        )}
        {dataSetId && folderIdOptions.length === 0 && (
          <p className={`text-xs ${theme.label}`}>No dataset columns found for Folder Id field.</p>
        )}
        <div className="flex items-center">
          <label className={`w-32 text-xs ${theme.label} mr-2 flex-none`} />
          <label className={`flex items-center gap-2 text-xs ${theme.label}`}>
            <input type="checkbox" checked={isEnableFolderSecurity} onChange={(e) => setIsEnableFolderSecurity(e.target.checked)} />
            Enable Folder Security
          </label>
        </div>
        <div className="flex gap-2 mt-1">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={handleSave}>
            Save Folder Navigation
          </button>
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={loadData}>
            Refresh
          </button>
        </div>
      </div>
    </div>
  );
};

export default TemplateFolderNavigationSection;
