/**
 * Unit Linked Search Management (Unit Navigate To Search)
 * Ported from Angular: TransactionUnitLinkedSearchManagement.cshtml + transactionUnitLinkedSearchManagementCtrl.js
 */
import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { searchSvc } from '../../../webapi/searchSvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useAlertConfirm } from '../../common/AlertConfirmProvider';

export interface TransactionUnitLinkedSearchManagementDialogProps {
  isOpen: boolean;
  transactionUnitId: number | null;
  unitDisplayName?: string | null;
  onClose: () => void;
}

const TransactionUnitLinkedSearchManagementDialog: React.FC<TransactionUnitLinkedSearchManagementDialogProps> = ({
  isOpen,
  transactionUnitId,
  unitDisplayName,
  onClose,
}) => {
  const { theme } = useTheme();
  const { showError, showInfo } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();
  const [loading, setLoading] = useState(true);
  const [list, setList] = useState<any[]>([]);
  const [searchDataMap, setSearchDataMap] = useState<DataMap | null>(null);
  const [searchSavedDataMap, setSearchSavedDataMap] = useState<DataMap | null>(null);
  const [searchViewDataMap, setSearchViewDataMap] = useState<DataMap | null>(null);
  const [actionDataMap, setActionDataMap] = useState<DataMap | null>(null);
  const [searchLookupList, setSearchLookupList] = useState<any[]>([]);
  const [searchViewLookupList, setSearchViewLookupList] = useState<any[]>([]);
  const [selectedSearchViewLookupList, setSelectedSearchViewLookupList] = useState<any[]>([]);
  const [actionLookupList, setActionLookupList] = useState<any[]>([]);
  const [unitFieldLookupList, setUnitFieldLookupList] = useState<any[]>([]);
  const [unitInfo, setUnitInfo] = useState<{ isRootUnit: boolean; isGridUnit: boolean }>({ isRootUnit: false, isGridUnit: false });
  const [searchViewFieldLookupList, setSearchViewFieldLookupList] = useState<any[]>([]);
  const [searchViewFieldDataMap, setSearchViewFieldDataMap] = useState<DataMap | null>(null);
  const gridRef = useRef<any>(null);
  const mappingGridRef = useRef<any>(null);
  const mapping2GridRef = useRef<any>(null);

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [selectedDto, setSelectedDto] = useState<any | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  const [allSearchFieldList, setAllSearchFieldList] = useState<any[]>([]);
  const [unitFieldDataMap, setUnitFieldDataMap] = useState<DataMap | null>(null);
  const [searchFilterFieldDataMap, setSearchFilterFieldDataMap] = useState<DataMap | null>(null);

  const toKey = useCallback((value: any): string | null => {
    if (value == null) return null;
    const key = String(value).trim();
    return key.length > 0 ? key : null;
  }, []);

  const normalizeLookupRows = useCallback(
    (rows: any[]): { Id: number; Display: string }[] =>
      (Array.isArray(rows) ? rows : [])
        .map((item: any) => {
          const id = item?.Id ?? item?.SearchId ?? null;
          const display =
            item?.Display ??
            item?.Name ??
            item?.Description ??
            item?.TransactionName ??
            (id != null ? String(id) : '');
          return id != null ? { Id: Number(id), Display: String(display) } : null;
        })
        .filter((x): x is { Id: number; Display: string } => x != null),
    []
  );

  const getMassLookupRows = useCallback(
    (massEntity: any, key: string): { Id: number; Display: string }[] => {
      if (!massEntity) return [];
      if (Array.isArray(massEntity)) return normalizeLookupRows(massEntity);
      const raw = massEntity[key] ?? massEntity[key.toLowerCase()] ?? null;
      return normalizeLookupRows(Array.isArray(raw) ? raw : []);
    },
    [normalizeLookupRows]
  );

  const dedupeLookupRows = useCallback((rows: { Id: number; Display: string }[]) => {
    const seen = new Set<number>();
    return rows.filter((row) => {
      if (seen.has(row.Id)) return false;
      seen.add(row.Id);
      return true;
    });
  }, []);

  const selectedSearchKey = toKey(selectedDto?.SearchId);
  const getSearchKeyFromView = useCallback((view: any): string | null => {
    if (!view) return null;
    const candidates = [
      view.SearchId,
      view.AppSearchId,
      view.SearchDefinitionId,
      view.SearchDefId,
      view.BlQueryId,
      view.BlqueryId,
      view.ReferenceSearchId,
      view.ForeignSearchId,
      view.RelatedSearchId,
    ];
    for (const c of candidates) {
      const key = toKey(c);
      if (key != null) return key;
    }
    return null;
  }, [toKey]);

  const filteredSearchViewLookupList = useMemo(() => {
    if (selectedSearchKey == null) return [];
    if (selectedSearchViewLookupList.length > 0) return selectedSearchViewLookupList;
    return searchViewLookupList.filter((v: any) => getSearchKeyFromView(v) === selectedSearchKey);
  }, [searchViewLookupList, selectedSearchKey, getSearchKeyFromView, selectedSearchViewLookupList]);

  useEffect(() => {
    let cancelled = false;
    const normalizeViewRows = (rows: any[]) =>
      (Array.isArray(rows) ? rows : [])
        .map((v: any) => ({
          ...v,
          Id: v?.Id ?? v?.SearchViewId ?? null,
          Display: v?.Display ?? v?.Name ?? v?.DisplayText ?? v?.ViewName ?? String(v?.Id ?? v?.SearchViewId ?? ''),
        }))
        .filter((v: any) => v?.Id != null);

    const loadViewsBySearch = async () => {
      if (!selectedSearchKey) {
        setSelectedSearchViewLookupList([]);
        return;
      }
      // Clear immediately so we never validate SearchViewId against the *previous* row's view list,
      // and so the "invalid id" effect does not run while this list is mid-load (see effect below).
      setSelectedSearchViewLookupList([]);
      try {
        // RetrieveOneSearch returns SearchDto — it does NOT populate Views on the server.
        // Match runtime AppSearch: load user-available views by search definition (BlqueryId / dataset tree).
        const searchDto = await searchSvc.retrieveOneSearch(selectedSearchKey, false);
        if (cancelled) return;
        if (searchDto?.Id === -1) {
          setSelectedSearchViewLookupList([]);
          return;
        }

        const searchDefinitionDto = {
          IsStaticBuiltInSearch: searchDto.IsStaticBuiltInSearch,
          Id: searchDto.Id,
          BlqueryId: searchDto.BlqueryId,
          Display: searchDto.Display,
          IsSavedSearch: searchDto.IsSavedSearch,
          SearchType: searchDto.SearchType,
        };

        let normalized: any[] = [];
        try {
          const viewsData = await searchSvc.retrieveUserViewsBySearchDefinition(searchDefinitionDto);
          if (cancelled) return;
          normalized = normalizeViewRows(Array.isArray(viewsData) ? viewsData : []);
        } catch {
          normalized = [];
        }

        // Fallback: search config carries explicit view list
        if (normalized.length === 0) {
          try {
            const ex = await searchSvc.retrieveOneAppSearchExDto(selectedSearchKey);
            if (cancelled) return;
            const rawList = ex?.AppSearchViewList ?? ex?.appSearchViewList ?? ex?.AppSearchView__List ?? ex?.appSearchView__List ?? [];
            const arr = Array.isArray(rawList) ? rawList : [];
            normalized = normalizeViewRows(arr);
          } catch {
            /* ignore */
          }
        }

        // Last resort: mass lookup filtered by dataset id on the view row (when present)
        if (normalized.length === 0 && searchDto.BlqueryId != null) {
          const bq = toKey(searchDto.BlqueryId);
          normalized = normalizeViewRows(
            searchViewLookupList.filter((v: any) => toKey(v?.BlqueryId ?? v?.DataSetId ?? v?.BlQueryId ?? v?.blqueryId) === bq)
          );
        }

        if (!cancelled) setSelectedSearchViewLookupList(normalized);
      } catch {
        if (!cancelled) setSelectedSearchViewLookupList([]);
      }
    };
    void loadViewsBySearch();
    return () => {
      cancelled = true;
    };
  }, [selectedSearchKey, searchViewLookupList, toKey]);

  // Drop SearchViewId only when we *know* it's not in the loaded list. While the list is still empty
  // (async load in progress) or we only have stale data, do nothing — otherwise first row select clears
  // the saved default view before retrieveUserViewsBySearchDefinition returns.
  useEffect(() => {
    if (!selectedDto) return;
    if (selectedDto.SearchViewId == null || selectedDto.SearchViewId === '') return;
    if (!Array.isArray(filteredSearchViewLookupList) || filteredSearchViewLookupList.length === 0) {
      return;
    }
    const validIds = new Set(filteredSearchViewLookupList.map((v: any) => toKey(v?.Id)));
    if (!validIds.has(toKey(selectedDto.SearchViewId))) {
      setSelectedDto((prev: any) => (prev ? { ...prev, SearchViewId: null } : prev));
    }
  }, [filteredSearchViewLookupList, selectedDto, toKey]);

  const cv = useMemo(() => {
    const c = new CollectionView(list);
    c.sortDescriptions.push(new SortDescription('Sort', true));
    return c;
  }, [list]);

  const loadData = useCallback(async () => {
    if (!isOpen || !transactionUnitId) {
      setLoading(false);
      return;
    }
    dispatch(setIsBusy());
    setLoading(true);
    try {
      const [data, allSearchRows, massEntity, unitDto, searchFields] = await Promise.all([
        appTransactionService.retrieveOneAppTransactionUnitLinkedSearchList(String(transactionUnitId)),
        searchSvc.retrieveAllAppSearchDto(null),
        adminSvc.getMassEntitiesLookupItem('AppSearchSaved|AppSearchView'),
        appTransactionService.retrieveOneAppTransactionUnitExDto(String(transactionUnitId)),
        searchSvc.retrieveAllAppSearchFieldDtoList(),
      ]);
      setList(Array.isArray(data) ? data : []);

      let appSearch = dedupeLookupRows(normalizeLookupRows(Array.isArray(allSearchRows) ? allSearchRows : []));
      if (appSearch.length === 0) {
        try {
          const searchMass = await adminSvc.getMassEntitiesLookupItem('AppSearch');
          appSearch = dedupeLookupRows(getMassLookupRows(searchMass, 'AppSearch'));
        } catch {
          /* ignore */
        }
      }
      const appSearchSaved = getMassLookupRows(massEntity, 'AppSearchSaved');
      const appSearchView = getMassLookupRows(massEntity, 'AppSearchView');
      setSearchLookupList(appSearch);
      setSearchViewLookupList(appSearchView);
      setSearchDataMap(appSearch.length ? new DataMap(appSearch, 'Id', 'Display') : null);
      setSearchSavedDataMap(appSearchSaved.length ? new DataMap(appSearchSaved, 'Id', 'Display') : null);
      setSearchViewDataMap(appSearchView.length ? new DataMap(appSearchView, 'Id', 'Display') : null);
      const fields = unitDto?.AppTransactionFieldList ?? [];
      const unitFieldOptions = (Array.isArray(fields) ? fields : [])
        .map((f: any) => ({
          Id: f?.Id ?? null,
          Display: f?.DisplayName ?? f?.FieldName ?? f?.DataBaseFieldName ?? String(f?.Id ?? ''),
        }))
        .filter((x: any) => x.Id != null);
      setUnitFieldLookupList(unitFieldOptions);
      setUnitFieldDataMap(unitFieldOptions.length ? new DataMap(unitFieldOptions, 'Id', 'Display') : null);

      const isRootUnit = unitDto?.ParentTransactionUnitId == null && !unitDto?.IsMasterSiblingUnit;
      const isGridUnit = unitDto?.ParentTransactionUnitId != null && Number(unitDto?.EmGridViewDisplayType ?? 0) === 1;
      setUnitInfo({ isRootUnit: Boolean(isRootUnit), isGridUnit: Boolean(isGridUnit) });

      const sf = Array.isArray(searchFields) ? searchFields : [];
      setAllSearchFieldList(sf);

      // Action list differs between Root unit and Grid unit (Angular behavior)
      // - Root master: "Update Form Field By Search"
      // - Grid unit: "Update Single Row Data By Search"
      const actionListAll = [
        { Id: 3, Display: 'Open Search' }, // EmAppLinkedSearchAction.ViewSearchResult
        { Id: 1, Display: 'Add Grid Row(s) By Search' }, // EmAppLinkedSearchAction.AddFormGridRow
        { Id: 2, Display: isGridUnit ? 'Update Single Row Data By Search' : 'Update Form Field By Search' }, // EmAppLinkedSearchAction.UpdateFormData
      ];
      // Root master unit supports ViewSearchResult + UpdateFormData (Angular); no AddFormGridRow
      const actionList = isRootUnit ? actionListAll.filter((x) => x.Id === 3 || x.Id === 2) : actionListAll;
      setActionLookupList(actionList);
      setActionDataMap(new DataMap(actionList, 'Id', 'Display'));
    } catch (e: any) {
      showError(e?.message || 'Failed to load linked searches');
      setList([]);
    } finally {
      dispatch(setIsNotBusy());
      setLoading(false);
    }
  }, [isOpen, transactionUnitId, dispatch, showError, dedupeLookupRows, normalizeLookupRows, getMassLookupRows]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const loadDetail = useCallback(
    async (id: number | null) => {
      if (!isOpen || !transactionUnitId) return;
      if (!id) {
        setSelectedDto(null);
        setSearchFilterFieldDataMap(null);
        return;
      }
      setDetailLoading(true);
      dispatch(setIsBusy());
      try {
        const dto = await appTransactionService.retrieveOneAppTransactionUnitLinkedSearchExDto(String(id));
        setSelectedDto({
          ...dto,
          TransactionUnitId: transactionUnitId,
          AppTransactionUnitSearchFieldMappingList: dto?.AppTransactionUnitSearchFieldMappingList ?? [],
          AppTransactionUnitSearchViewFieldMappingList: dto?.AppTransactionUnitSearchViewFieldMappingList ?? [],
        });

        const searchId = dto?.SearchId ?? null;
        const filtered = (Array.isArray(allSearchFieldList) ? allSearchFieldList : [])
          .filter((f: any) => (searchId != null ? Number(f?.SearchId) === Number(searchId) : true))
          .map((f: any) => ({
            Id: f?.Id ?? null,
            Display:
              f?.DisplayText ??
              f?.DisplayName ??
              f?.FieldName ??
              f?.DataBaseFieldName ??
              f?.SysTableFiledPath ??
              String(f?.Id ?? ''),
          }))
          .filter((x: any) => x.Id != null);
        setSearchFilterFieldDataMap(filtered.length ? new DataMap(filtered, 'Id', 'Display') : null);
      } catch (e: any) {
        showError(e?.message || 'Failed to load menu');
        setSelectedDto(null);
      } finally {
        dispatch(setIsNotBusy());
        setDetailLoading(false);
      }
    },
    [isOpen, transactionUnitId, dispatch, showError, allSearchFieldList]
  );

  const handleCreate = useCallback(() => {
    if (!transactionUnitId) return;
    setSelectedId(null);
    setSelectedDto({
      TransactionUnitId: transactionUnitId,
      Name: '',
      Sort: 1,
      Action: 3,
      SearchId: null,
      SearchSaveId: null,
      SearchViewId: null,
      ConditionTransFieldId: null,
      IsPopup: false,
      PopupWidth: null,
      PopupHeight: null,
      IconName: '',
      UsageType: 102,
      AppTransactionUnitSearchFieldMappingList: [],
      AppTransactionUnitSearchViewFieldMappingList: [],
    });
    setSearchFilterFieldDataMap(null);
  }, [transactionUnitId]);

  const handleSelect = useCallback(
    (item: any) => {
      const id = item?.Id != null ? Number(item.Id) : null;
      setSelectedId(id);
      void loadDetail(id);
    },
    [loadDetail]
  );

  const handleDelete = useCallback(async () => {
    const id = selectedDto?.Id ?? selectedId;
    if (!id) {
      showInfo('Select a row to delete');
      return;
    }
    const ok = await showConfirm('Confirm delete this linked search?', { title: 'Delete' });
    if (!ok) return;
    dispatch(setIsBusy());
    try {
      await appTransactionService.deleteAppTransactionUnitLinkedSearch(String(id));
      showInfo('Deleted.');
      setSelectedId(null);
      setSelectedDto(null);
      loadData();
    } catch (e: any) {
      showError(e?.message || 'Failed to delete');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [showConfirm, showInfo, showError, loadData, dispatch]);

  const update = useCallback((key: string, value: any) => {
    setSelectedDto((prev: any) => ({ ...(prev || {}), [key]: value }));
  }, []);

  const handleSave = useCallback(async () => {
    if (!selectedDto) {
      showInfo('Select or create a menu first.');
      return;
    }
    setSaving(true);
    dispatch(setIsBusy());
    try {
      const result = await appTransactionService.saveAppTransactionUnitLinkedSearchExDto(selectedDto);
      if (result?.ValidationResult) {
        // keep same behavior as other editors: show messages but don't block UI here
        // showValidationMessages is not used in this dialog intentionally
      }
      if (result?.IsSuccessful) {
        showInfo('Saved.');
        await loadData();
        const newId = result?.Object?.Id ?? selectedDto?.Id ?? null;
        if (newId) {
          setSelectedId(Number(newId));
          await loadDetail(Number(newId));
        }
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to save');
    } finally {
      dispatch(setIsNotBusy());
      setSaving(false);
    }
  }, [selectedDto, dispatch, showInfo, showError, loadData, loadDetail]);

  const handleAddMapping = useCallback(() => {
    if (!selectedDto) return;
    const searchFieldId = (searchFilterFieldDataMap as any)?.collectionView?.items?.[0]?.Id ?? null;
    const transFieldId = unitFieldLookupList?.[0]?.Id ?? null;
    setSelectedDto((prev: any) => ({
      ...(prev || {}),
      AppTransactionUnitSearchFieldMappingList: [
        ...(prev?.AppTransactionUnitSearchFieldMappingList || []),
        {
          TransactionUnitLinkedSearchId: prev?.Id ?? 0,
          SearchFieldId: searchFieldId,
          TransactionFieldId: transFieldId,
        },
      ],
    }));
  }, [selectedDto, searchFilterFieldDataMap, unitFieldDataMap]);

  const handleRemoveMapping = useCallback(() => {
    const grid = mappingGridRef.current?.control;
    const item = grid?.selectedRows?.[0]?.dataItem;
    if (!item) return;
    setSelectedDto((prev: any) => ({
      ...(prev || {}),
      AppTransactionUnitSearchFieldMappingList: (prev?.AppTransactionUnitSearchFieldMappingList || []).filter((m: any) => m !== item),
    }));
  }, []);

  const handleAddResultMapping = useCallback(() => {
    if (!selectedDto) return;
    const viewFieldId = (searchViewFieldDataMap as any)?.collectionView?.items?.[0]?.Id ?? null;
    const transFieldId = unitFieldLookupList?.[0]?.Id ?? null;
    setSelectedDto((prev: any) => ({
      ...(prev || {}),
      AppTransactionUnitSearchViewFieldMappingList: [
        ...(prev?.AppTransactionUnitSearchViewFieldMappingList || []),
        {
          TransactionUnitLinkedSearchId: prev?.Id ?? 0,
          SearchViewFieldId: viewFieldId,
          TransactionFieldId: transFieldId,
          IsUnique: false,
          ExternalAppFieldMappingCode: '',
        },
      ],
    }));
  }, [selectedDto, searchViewFieldDataMap, unitFieldLookupList]);

  const handleRemoveResultMapping = useCallback(() => {
    const grid = mapping2GridRef.current?.control;
    const item = grid?.selectedRows?.[0]?.dataItem;
    if (!item) return;
    setSelectedDto((prev: any) => ({
      ...(prev || {}),
      AppTransactionUnitSearchViewFieldMappingList: (prev?.AppTransactionUnitSearchViewFieldMappingList || []).filter((m: any) => m !== item),
    }));
  }, []);

  // refresh SearchField datamap when SearchId changes
  useEffect(() => {
    if (!selectedDto) return;
    const searchId = selectedDto?.SearchId ?? null;
    const filtered = (Array.isArray(allSearchFieldList) ? allSearchFieldList : [])
      .filter((f: any) => (searchId != null ? Number(f?.SearchId) === Number(searchId) : true))
      .map((f: any) => ({
        Id: f?.Id ?? null,
        Display:
          f?.DisplayText ??
          f?.DisplayName ??
          f?.FieldName ??
          f?.DataBaseFieldName ??
          f?.SysTableFiledPath ??
          String(f?.Id ?? ''),
      }))
      .filter((x: any) => x.Id != null);
    setSearchFilterFieldDataMap(filtered.length ? new DataMap(filtered, 'Id', 'Display') : null);
  }, [selectedDto?.SearchId, allSearchFieldList]);

  // refresh SearchViewField datamap when SearchViewId changes (for action 1/2 mapping)
  useEffect(() => {
    if (!selectedDto?.SearchViewId) {
      setSearchViewFieldLookupList([]);
      setSearchViewFieldDataMap(null);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const viewDto = await searchSvc.retrieveOneAppSearchViewExDto(String(selectedDto.SearchViewId));
        const list = Array.isArray(viewDto?.AppSearchViewFieldList) ? viewDto.AppSearchViewFieldList : [];
        const options = list
          .map((f: any) => ({
            Id: f?.Id ?? null,
            Display: f?.DisplayText ?? f?.SysTableFiledPath ?? String(f?.Id ?? ''),
          }))
          .filter((x: any) => x.Id != null);
        if (cancelled) return;
        setSearchViewFieldLookupList(options);
        setSearchViewFieldDataMap(options.length ? new DataMap(options, 'Id', 'Display') : null);
      } catch (_) {
        if (cancelled) return;
        setSearchViewFieldLookupList([]);
        setSearchViewFieldDataMap(null);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [selectedDto?.SearchViewId]);

  if (!isOpen) return null;

  return (
    <>
      <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black bg-opacity-50">
        <div
          className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
          style={{ width: '95vw', height: '88vh', maxWidth: 1200 }}
          onClick={(e) => e.stopPropagation()}
        >
          <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
            <div className={`text-sm font-semibold ${theme.title}`}>
              Unit Navigate To Search: {unitDisplayName || 'Unit'}
            </div>
            <div className="flex items-center gap-2">
              <button type="button" onClick={loadData} className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs`}>
                Refresh
              </button>
              <button type="button" onClick={handleSave} disabled={saving || detailLoading} className={`px-3 py-1 ${theme.button_default} rounded-[4px] text-xs disabled:opacity-50`}>
                Save
              </button>
              <button type="button" onClick={onClose} className="px-3 py-1 rounded-[4px] text-xs border">
                Close
              </button>
            </div>
          </div>
          <div className="flex-1 min-h-0 overflow-hidden p-2">
            {loading ? (
              <div className={`p-4 ${theme.label}`}>Loading...</div>
            ) : (
              <div className="w-full h-full flex gap-2 overflow-hidden">
                {/* Left: Menus */}
                <div className={`basis-[55%] flex flex-col border rounded overflow-hidden ${theme.mainContentSection}`}>
                  <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                    <div className={`text-xs font-semibold ${theme.title}`}>
                      <i className="fa-solid fa-bars mr-1 opacity-70" /> Menus
                    </div>
                    <div className="flex items-center gap-1">
                      <button type="button" onClick={handleCreate} className={`px-2 py-1 ${theme.button_default} rounded text-xs`}>
                        <i className="fa-solid fa-plus mr-1" /> Add
                      </button>
                      <button type="button" onClick={handleDelete} className={`px-2 py-1 ${theme.button_default} rounded text-xs`}>
                        <i className="fa-solid fa-trash mr-1" /> Delete
                      </button>
                    </div>
                  </div>
                  <div className="flex-1 min-h-0">
                    <FlexGrid
                      ref={gridRef}
                      className="w-full h-full"
                      itemsSource={cv}
                      selectionMode="Row"
                      isReadOnly={true}
                      style={{ height: '100%', border: 'none' }}
                      selectionChanged={(s: any) => {
                        const flex = s?.control ?? s;
                        const rowIndex = flex?.selection?.row ?? -1;
                        const item = rowIndex >= 0 ? flex?.rows?.[rowIndex]?.dataItem : null;
                        if (item) handleSelect(item);
                      }}
                    >
                      <FlexGridFilter />
                      <FlexGridColumn binding="Sort" header="Sort" width={70} dataType="Number" />
                      <FlexGridColumn binding="Name" header="Menu Name" width={200} />
                      <FlexGridColumn binding="SearchId" header="Navigate To Search" width={220} dataMap={searchDataMap} />
                      <FlexGridColumn binding="Action" header="Action" width={160} dataMap={actionDataMap} />
                      <FlexGridColumn header="" width="*" />
                    </FlexGrid>
                  </div>
                </div>

                {/* Right: Menu Properties */}
                <div className={`basis-[45%] flex flex-col border rounded overflow-hidden ${theme.mainContentSection}`}>
                  <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
                    <div className={`text-xs font-semibold ${theme.title}`}>
                      <i className="fa-solid fa-sliders mr-1 opacity-70" /> Menu Properties
                    </div>
                  </div>

                  <div className="flex-1 min-h-0 overflow-auto p-3">
                    {!selectedDto ? (
                      <div className={`text-xs ${theme.label}`}>Select a menu on the left, or click Add.</div>
                    ) : detailLoading ? (
                      <div className={`text-xs ${theme.label}`}>Loading...</div>
                    ) : (
                      <div className="space-y-2">
                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Sort Order</div>
                          <input
                            type="number"
                            value={selectedDto.Sort ?? 0}
                            onChange={(e) => update('Sort', parseInt(e.target.value, 10) || 0)}
                            className={`w-20 h-7 px-2 text-xs border ${theme.inputBox}`}
                          />
                          <div className="w-1 flex-auto" />
                          <div className={`w-12 text-xs ${theme.label}`}>Icon</div>
                          <input
                            type="text"
                            value={selectedDto.IconName ?? ''}
                            onChange={(e) => update('IconName', e.target.value)}
                            className={`w-32 h-7 px-2 text-xs border ${theme.inputBox}`}
                          />
                          <button type="button" className={`h-7 w-7 border ${theme.button_default}`} title="Clear Icon" onClick={() => update('IconName', '')}>
                            <i className="fa-solid fa-xmark" />
                          </button>
                        </div>

                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Name</div>
                          <input
                            type="text"
                            value={selectedDto.Name ?? ''}
                            onChange={(e) => update('Name', e.target.value)}
                            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                          />
                        </div>

                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Action</div>
                          <select
                            value={selectedDto.Action ?? 3}
                            onChange={(e) => update('Action', e.target.value ? Number(e.target.value) : null)}
                            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                          >
                            {actionLookupList.map((o: any) => (
                              <option key={o.Id} value={o.Id}>
                                {o.Display}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Link To Search</div>
                          <select
                            value={selectedDto.SearchId ?? ''}
                            onChange={(e) => {
                              const nextSearchId = e.target.value ? Number(e.target.value) : null;
                              // Keep Angular behavior: Search View should follow selected Search.
                              setSelectedDto((prev: any) =>
                                prev
                                  ? {
                                      ...prev,
                                      SearchId: nextSearchId,
                                      SearchViewId: null,
                                    }
                                  : prev
                              );
                            }}
                            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                          >
                            <option value=""></option>
                            {searchLookupList.map((o: any) => (
                              <option key={o.Id} value={o.Id}>
                                {o.Display}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Default Search View</div>
                          <select
                            value={selectedDto.SearchViewId ?? ''}
                            onChange={(e) => update('SearchViewId', e.target.value ? Number(e.target.value) : null)}
                            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                          >
                            <option value=""></option>
                            {filteredSearchViewLookupList.map((o: any) => (
                              <option key={o.Id} value={o.Id}>
                                {o.Display}
                              </option>
                            ))}
                          </select>
                        </div>

                        <div className="flex items-center gap-2">
                          <div className={`w-28 text-xs ${theme.label}`}>Condition Field (Optional)</div>
                          <select
                            value={selectedDto.ConditionTransFieldId ?? ''}
                            onChange={(e) => update('ConditionTransFieldId', e.target.value ? Number(e.target.value) : null)}
                            className={`flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
                          >
                            <option value=""></option>
                            {unitFieldLookupList.map((o: any) => (
                              <option key={o.Id} value={o.Id}>
                                {o.Display}
                              </option>
                            ))}
                          </select>
                        </div>

                        <label className="flex items-center gap-2 pt-1">
                          <input
                            type="checkbox"
                            checked={Boolean(selectedDto.IsPopup)}
                            onChange={(e) => update('IsPopup', e.target.checked)}
                            className="w-4 h-4"
                          />
                          <span className={`text-xs ${theme.label}`}>Open as Popup</span>
                        </label>

                        {/* Search Mapping (always shown, matches Angular) */}
                        <div className="pt-2">
                          <div className={`text-xs font-semibold ${theme.title} mb-1`}>
                            Search Mapping: Set Search Filter Values From Form
                          </div>
                          <div className="flex items-center justify-end gap-2 mb-1">
                            <button type="button" onClick={handleAddMapping} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                              Add
                            </button>
                            <button type="button" onClick={handleRemoveMapping} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                              Remove
                            </button>
                          </div>
                          <div className="h-[180px] border rounded overflow-hidden">
                            <FlexGrid
                              ref={mappingGridRef}
                              className="w-full h-full"
                              itemsSource={new CollectionView(selectedDto.AppTransactionUnitSearchFieldMappingList || [])}
                              selectionMode="Row"
                              allowAddNew={false}
                              allowDelete={false}
                              style={{ border: 'none' }}
                            >
                              <FlexGridColumn binding="SearchFieldId" header="Search Filter" width={220} dataMap={searchFilterFieldDataMap || undefined} />
                              <FlexGridColumn binding="TransactionFieldId" header="Form Field" width={220} dataMap={unitFieldDataMap || undefined} />
                              <FlexGridColumn header="" width="*" />
                            </FlexGrid>
                          </div>
                        </div>

                        {/* Result Mapping (shown for Update/Add actions, matches Angular) */}
                        {Number(selectedDto.Action ?? 3) !== 3 && (
                          <div className="pt-4">
                            <div className={`text-xs font-semibold ${theme.title} mb-1`}>
                              Result Mapping: Send Result Values Back To Form
                            </div>
                            <div className="flex items-center justify-end gap-2 mb-1">
                              <button type="button" onClick={handleAddResultMapping} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                                Add
                              </button>
                              <button type="button" onClick={handleRemoveResultMapping} className={`px-2 py-1 text-xs rounded ${theme.button_default}`}>
                                Remove
                              </button>
                            </div>
                            <div className="h-[180px] border rounded overflow-hidden">
                              <FlexGrid
                                ref={mapping2GridRef}
                                className="w-full h-full"
                                itemsSource={new CollectionView(selectedDto.AppTransactionUnitSearchViewFieldMappingList || [])}
                                selectionMode="Row"
                                allowAddNew={false}
                                allowDelete={false}
                                style={{ border: 'none' }}
                              >
                                <FlexGridColumn binding="SearchViewFieldId" header="Search Result View Field" width={220} dataMap={searchViewFieldDataMap || undefined} />
                                <FlexGridColumn binding="TransactionFieldId" header="Form Field" width={220} dataMap={unitFieldDataMap || undefined} />
                                <FlexGridColumn binding="IsUnique" header="Unique" width={80} dataType="Boolean" />
                                <FlexGridColumn header="" width="*" />
                              </FlexGrid>
                            </div>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </>
  );
};

export default TransactionUnitLinkedSearchManagementDialog;
