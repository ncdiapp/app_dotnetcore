import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../../redux/hooks/useErrorMessage';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { searchSvc } from '../../../../webapi/searchSvc';
import DataSetEditor from '../../../dbmgt/DataSetEditor';
import { EmbeddedLinkedPopupFrame } from '../../../formMgt/EmbeddedLinkedPopupFrame';
import { PopupModalOverlay } from '../../../formMgt/PopupModalOverlay';
import SearchEditor from '../../../search/SearchEditor';

export function BatchCommandSettingSection(props: {
  hierarchy: any;
  action: any;
  rootLevelAllFieldLookUpList: any[];
  onMarkChange: () => void;
}) {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const batchSourceFromEnum = useEnumValues('EmAppBatchCommandSourceFrom');

  const { hierarchy, action, rootLevelAllFieldLookUpList, onMarkChange } = props;

  // Batch command pickers (Angular parity: popup selector + on-the-fly editor)
  const [isDataSetPickerOpen, setIsDataSetPickerOpen] = useState(false);
  const [isSearchPickerOpen, setIsSearchPickerOpen] = useState(false);
  const [dataSetPickerFilter, setDataSetPickerFilter] = useState('');
  const [searchPickerFilter, setSearchPickerFilter] = useState('');
  const [dataSetPickerPreviewId, setDataSetPickerPreviewId] = useState<number | null>(null);
  const [allDataSetList, setAllDataSetList] = useState<any[]>([]);
  const [allSearchList, setAllSearchList] = useState<any[]>([]);
  const [batchSearchDisplayCache, setBatchSearchDisplayCache] = useState<Record<string, string>>({});
  const [dataSetColumnOptions, setDataSetColumnOptions] = useState<Array<{ Id: string; Display: string }>>([]);
  const [searchViewFieldOptions, setSearchViewFieldOptions] = useState<Array<{ Id: number; Display: string }>>([]);
  const [popupDatasetEditorId, setPopupDatasetEditorId] = useState<number | null>(null);
  const [popupSearchEditorId, setPopupSearchEditorId] = useState<number | null>(null);

  // Batch command: Search filter value mapping popup (Angular: BatchSearchCriterialMappingPopup + DictBatchSearchCrietraIdAndTransFieldId)
  const [isBatchSearchCriteriaMappingOpen, setIsBatchSearchCriteriaMappingOpen] = useState(false);
  const [batchSearchCriteriaMappingCV] = useState(() => new CollectionView<any>([]));
  const [batchSearchCriteriaDataMap, setBatchSearchCriteriaDataMap] = useState<DataMap | null>(null);
  const [batchSearchTransFieldDataMap, setBatchSearchTransFieldDataMap] = useState<DataMap | null>(null);

  const normalizeDisplay = useCallback((obj: any): string => {
    if (!obj) return '';
    return (
      obj?.Display ??
      obj?.Name ??
      obj?.DisplayText ??
      obj?.DisplayName ??
      obj?.SearchName ??
      obj?.DataSetName ??
      String(obj?.Id ?? '')
    );
  }, []);

  const ensureDataSetListLoaded = useCallback(async () => {
    if (allDataSetList.length > 0) return;
    const raw = await searchSvc.retrieveAllAppDataSetEntityDto();
    setAllDataSetList(Array.isArray(raw) ? raw : []);
  }, [allDataSetList.length]);

  const ensureSearchListLoaded = useCallback(async () => {
    if (allSearchList.length > 0) return;
    const raw = await searchSvc.retrieveAllAppSearchDto(null);
    setAllSearchList(Array.isArray(raw) ? raw : []);
  }, [allSearchList.length]);

  const loadDataSetColumnsForBatch = useCallback(async (dataSetId: number | null) => {
    if (!dataSetId) {
      setDataSetColumnOptions([]);
      return;
    }
    try {
      const cols = await searchSvc.retrieveQueryColumnList(String(dataSetId));
      const list = Array.isArray(cols) ? cols : [];
      const options = list
        .map((c: any) => {
          const id = c?.Id ?? c?.Name ?? c?.ColumnName ?? null;
          const name = c?.Name ?? c?.ColumnName ?? c?.DbcolumnName ?? c?.DisplayText ?? String(id ?? '');
          if (id == null) return null;
          return { Id: String(id), Display: String(name) };
        })
        .filter((x: any): x is { Id: string; Display: string } => x != null);
      setDataSetColumnOptions(options);
    } catch {
      setDataSetColumnOptions([]);
    }
  }, []);

  const loadSearchViewFieldsForBatch = useCallback(async (searchId: number | null) => {
    if (!searchId) {
      setSearchViewFieldOptions([]);
      return;
    }
    try {
      const resolveSearchViewId = (searchEx: any): number | null => {
        const candidates = [
          searchEx?.DefaultSearchViewId,
          searchEx?.DefaultViewId,
          searchEx?.SearchViewId,
          searchEx?.AppSearchViewId,
        ].filter((x) => x != null);
        if (candidates.length) return Number(candidates[0]);
        const list = searchEx?.AppSearchViewList ?? searchEx?.AppSearchViewDtoList ?? [];
        const first = Array.isArray(list) ? list[0] : null;
        const id = first?.Id ?? first?.SearchViewId ?? first?.AppSearchViewId ?? null;
        return id != null ? Number(id) : null;
      };

      const searchEx = await searchSvc.retrieveOneAppSearchExDto(String(searchId));
      const viewId = resolveSearchViewId(searchEx);
      if (!viewId) {
        setSearchViewFieldOptions([]);
        return;
      }
      const viewDto = await searchSvc.retrieveOneAppSearchViewExDto(String(viewId));
      const list = Array.isArray(viewDto?.AppSearchViewFieldList) ? viewDto.AppSearchViewFieldList : [];
      const options = list
        .map((f: any) => ({
          Id: Number(f?.Id ?? f?.SearchViewFieldId ?? f?.FieldId ?? 0),
          Display: String(f?.DisplayText ?? f?.SysTableFiledPath ?? f?.DisplayName ?? f?.FieldName ?? f?.Id ?? ''),
        }))
        .filter((x: any) => x?.Id);
      setSearchViewFieldOptions(options);
    } catch {
      setSearchViewFieldOptions([]);
    }
  }, []);

  const dataSetPickerList = useMemo(() => {
    const arr = allDataSetList || [];
    const filter = String(dataSetPickerFilter ?? '').trim().toLowerCase();
    if (!filter) return arr;
    return arr.filter((x: any) => normalizeDisplay(x).toLowerCase().includes(filter) || String(x?.Id ?? '').toLowerCase().includes(filter));
  }, [allDataSetList, dataSetPickerFilter, normalizeDisplay]);

  const searchPickerList = useMemo(() => {
    const arr = allSearchList || [];
    const filter = String(searchPickerFilter ?? '').trim().toLowerCase();
    if (!filter) return arr;
    return arr.filter((x: any) => normalizeDisplay(x).toLowerCase().includes(filter) || String(x?.Id ?? '').toLowerCase().includes(filter));
  }, [allSearchList, searchPickerFilter, normalizeDisplay]);

  // Ensure BatchCommandSearch shows "Name (Id)" even before opening picker.
  useEffect(() => {
    const isBatch = !!action?.ActionAttribute?.IsBatchCommand;
    const isSearchType =
      Number(action?.ActionAttribute?.BatchCommandSourceFromType ?? 1) === Number(batchSourceFromEnum?.Search ?? 2);
    const id = action?.ActionAttribute?.BatchCommandSearchId ?? null;
    if (!isBatch || !isSearchType || !id) return;
    const sid = String(id);
    const fromList = allSearchList.find((x: any) => String(x?.Id ?? '') === sid);
    if (fromList) return;
    if (batchSearchDisplayCache[sid]) return;
    (async () => {
      try {
        await ensureSearchListLoaded();
      } catch {
        // ignore
      }
    })();
  }, [
    action?.ActionAttribute?.IsBatchCommand,
    action?.ActionAttribute?.BatchCommandSourceFromType,
    action?.ActionAttribute?.BatchCommandSearchId,
    batchSourceFromEnum?.Search,
    allSearchList,
    batchSearchDisplayCache,
    ensureSearchListLoaded,
    action,
  ]);

  const openBatchSearchCriteriaMappingPopup = useCallback(
    async (searchId: number) => {
      if (!action) return;
      try {
        const searchData = await searchSvc.retrieveOneSearch(String(searchId), false);
        const criterias = Array.isArray(searchData?.Criterias) ? searchData.Criterias : [];
        const dictOrgMapping = (action?.ActionAttribute as any)?.DictBatchSearchCrietraIdAndTransFieldId || {};
        const mappingList = criterias.map((c: any) => ({
          criteiaId: c?.SearcDCUID ?? null,
          transFieldId: dictOrgMapping?.[c?.SearcDCUID] ?? null,
        }));
        const transFieldLookupList = (rootLevelAllFieldLookUpList || []).map((f: any) => ({
          Id: f?.Id,
          ShortDisplay: f?.ShortDisplay ?? f?.Display ?? String(f?.Id ?? ''),
        }));
        setBatchSearchCriteriaDataMap(new DataMap(criterias, 'SearcDCUID', 'Display'));
        setBatchSearchTransFieldDataMap(new DataMap(transFieldLookupList, 'Id', 'ShortDisplay'));
        batchSearchCriteriaMappingCV.sourceCollection = mappingList;
        batchSearchCriteriaMappingCV.refresh();
        setIsBatchSearchCriteriaMappingOpen(true);
      } catch (e: any) {
        showError(e?.message || 'Failed to load search criterias');
      }
    },
    [action, batchSearchCriteriaMappingCV, rootLevelAllFieldLookUpList, showError]
  );

  const applyBatchSearchCriteriaMapping = useCallback(() => {
    if (!action) return;
    const list = (batchSearchCriteriaMappingCV.sourceCollection as any[]) || [];
    const dict: Record<string, number> = {};
    list.forEach((m: any) => {
      if (m?.criteiaId && m?.transFieldId) {
        dict[String(m.criteiaId)] = Number(m.transFieldId);
      }
    });
    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
    (action.ActionAttribute as any).DictBatchSearchCrietraIdAndTransFieldId = dict;
    onMarkChange();
    setIsBatchSearchCriteriaMappingOpen(false);
  }, [action, batchSearchCriteriaMappingCV, onMarkChange]);

  if (!action) return null;

  return (
    <>
      <div className="flex flex-col gap-2 min-w-0">
          <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
            <label className={`text-xs ${theme.label}`}>Is Batch Command</label>
            <label className="flex items-center gap-2 text-xs">
              <input
                type="checkbox"
                checked={!!action.ActionAttribute?.IsBatchCommand}
                onChange={(e) => {
                  action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                  action.ActionAttribute.IsBatchCommand = e.target.checked;
                  if (e.target.checked && !action.ActionAttribute.BatchCommandSourceFromType) {
                    action.ActionAttribute.BatchCommandSourceFromType = 1;
                  }
                  onMarkChange();
                }}
              />
              <span className={theme.label}>Enable</span>
            </label>
          </div>

          {action.ActionAttribute?.IsBatchCommand ? (
            <div className="flex flex-col gap-2">
              <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                <label className={`text-xs ${theme.label}`}>Batch Command Source Type</label>
                <select
                  className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                  value={Number(action.ActionAttribute?.BatchCommandSourceFromType ?? 1)}
                  onChange={(e) => {
                    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                    action.ActionAttribute.BatchCommandSourceFromType = Number(e.target.value);
                    action.ActionAttribute.BatchCommandDataSetId = null;
                    action.ActionAttribute.BatchCommandSearchId = null;
                    action.ActionAttribute.BatchCommandSourceViewFieldId = null;
                    action.ActionAttribute.BatchCommandSourceDataSetFieldName = null;
                    action.ActionAttribute.ForeachLoopSourceUnitId = null;
                    onMarkChange();
                  }}
                  title="Batch source"
                >
                  <option value={Number(batchSourceFromEnum?.DataSet ?? 1)}>DataSet</option>
                  <option value={Number(batchSourceFromEnum?.Search ?? 2)}>Search</option>
                  <option value={Number(batchSourceFromEnum?.ChildUnit ?? 3)}>ChildUnit</option>
                </select>
              </div>

              {Number(action.ActionAttribute?.BatchCommandSourceFromType ?? 1) === Number(batchSourceFromEnum?.DataSet ?? 1) ? (
                <>
                  <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                    <label className={`text-xs ${theme.label}`}>Batch Command DataSet</label>
                    <div className="flex items-center gap-2 min-w-0">
                      <button
                        type="button"
                        className={`text-xs underline ${theme.label}`}
                        title="Select DataSet"
                        onClick={async () => {
                          try {
                            await ensureDataSetListLoaded();
                          } catch (e: any) {
                            showError(e?.message || 'Failed to load dataset list');
                          }
                          setIsDataSetPickerOpen(true);
                        }}
                      >
                        {(() => {
                          const id = action.ActionAttribute?.BatchCommandDataSetId ?? null;
                          if (!id) return '(Select)';
                          const ds = allDataSetList.find((x: any) => Number(x?.Id) === Number(id));
                          return ds ? `${normalizeDisplay(ds)} (${ds?.Id})` : String(id);
                        })()}
                      </button>
                      {action.ActionAttribute?.BatchCommandDataSetId ? (
                        <button
                          type="button"
                          className={`w-7 h-7 rounded ${theme.button_default} text-xs`}
                          title="Edit DataSet"
                          onClick={() => {
                            const id = action.ActionAttribute?.BatchCommandDataSetId ?? null;
                            if (!id) return;
                            setPopupDatasetEditorId(Number(id));
                          }}
                        >
                          <i className="fa-solid fa-pen-to-square" aria-hidden />
                        </button>
                      ) : null}
                    </div>
                  </div>

                  {!!action.ActionAttribute?.BatchCommandDataSetId ? (
                    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                      <label className={`text-xs ${theme.label}`}>Batch Command TransactionRId DataSet Field</label>
                      <select
                        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                        value={action.ActionAttribute?.BatchCommandSourceDataSetFieldName ?? ''}
                        onFocus={async () => {
                          const id = action.ActionAttribute?.BatchCommandDataSetId ?? null;
                          await loadDataSetColumnsForBatch(id ? Number(id) : null);
                        }}
                        onChange={(e) => {
                          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                          action.ActionAttribute.BatchCommandSourceDataSetFieldName = e.target.value || null;
                          onMarkChange();
                        }}
                      >
                        <option value="">(Select)</option>
                        {dataSetColumnOptions.map((c) => (
                          <option key={c.Id} value={c.Display}>
                            {c.Display}
                          </option>
                        ))}
                      </select>
                    </div>
                  ) : null}
                </>
              ) : null}

              {Number(action.ActionAttribute?.BatchCommandSourceFromType ?? 1) === Number(batchSourceFromEnum?.Search ?? 2) ? (
                <>
                  <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                    <label className={`text-xs ${theme.label}`}>Batch Command Search</label>
                    <div className="w-72">
                      <div className="flex items-stretch w-full">
                        <div
                          className={`w-1 flex-auto min-w-0 h-7 px-2 text-[11px] border border-r-0 rounded-l-[4px] ${theme.inputBox} flex items-center overflow-hidden`}
                          title="Click to edit search"
                        >
                          <button
                            type="button"
                            className={`w-full truncate text-left underline text-[11px] leading-none ${theme.label}`}
                            onClick={() => {
                              const id = action.ActionAttribute?.BatchCommandSearchId ?? null;
                              if (!id) return;
                              setPopupSearchEditorId(Number(id));
                            }}
                          >
                            {(() => {
                              const id = action.ActionAttribute?.BatchCommandSearchId ?? null;
                              if (!id) return '(Select)';
                              const sid = String(id);
                              const fromList = allSearchList.find((x: any) => String(x?.Id ?? '') === sid);
                              if (fromList) return `${normalizeDisplay(fromList)} (${sid})`;
                              return batchSearchDisplayCache[sid] ?? sid;
                            })()}
                          </button>
                        </div>

                        <button
                          type="button"
                          className={`w-7 h-7 border rounded-none ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                          title="Select Search"
                          onClick={async () => {
                            try {
                              await ensureSearchListLoaded();
                            } catch (e: any) {
                              showError(e?.message || 'Failed to load search list');
                            }
                            setIsSearchPickerOpen(true);
                          }}
                        >
                          <i className="fa-solid fa-chevron-down text-[10px]" aria-hidden />
                        </button>

                        {!!action.ActionAttribute?.BatchCommandSearchId ? (
                          <button
                            type="button"
                            className={`w-7 h-7 border border-l-0 rounded-r-[4px] ${theme.inputBox} text-[11px] leading-none flex items-center justify-center`}
                            title="Search Filter Value Mapping"
                            onClick={() => {
                              const id = action.ActionAttribute?.BatchCommandSearchId ?? null;
                              if (!id) return;
                              openBatchSearchCriteriaMappingPopup(Number(id));
                            }}
                          >
                            <i className="fa-solid fa-filter text-[10px]" aria-hidden />
                          </button>
                        ) : (
                          <div className={`w-7 h-7 border border-l-0 rounded-r-[4px] ${theme.mainContentSection}`} />
                        )}
                      </div>
                    </div>
                  </div>

                  {!!action.ActionAttribute?.BatchCommandSearchId ? (
                    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                      <label className={`text-xs ${theme.label}`}>Batch Command TransactionRId View Field</label>
                      <select
                        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                        value={action.ActionAttribute?.BatchCommandSourceViewFieldId ?? ''}
                        onFocus={async () => {
                          const id = action.ActionAttribute?.BatchCommandSearchId ?? null;
                          await loadSearchViewFieldsForBatch(id ? Number(id) : null);
                        }}
                        onChange={(e) => {
                          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                          const v = e.target.value;
                          action.ActionAttribute.BatchCommandSourceViewFieldId = v ? Number(v) : null;
                          onMarkChange();
                        }}
                      >
                        <option value="">(Select)</option>
                        {searchViewFieldOptions.map((f) => (
                          <option key={String(f.Id)} value={String(f.Id)}>
                            {f.Display}
                          </option>
                        ))}
                      </select>
                    </div>
                  ) : null}
                </>
              ) : null}

              {Number(action.ActionAttribute?.BatchCommandSourceFromType ?? 1) === Number(batchSourceFromEnum?.ChildUnit ?? 3) ? (
                <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
                  <label className={`text-xs ${theme.label}`}>Batch Command Source Unit</label>
                  <select
                    className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
                    value={action.ActionAttribute?.ForeachLoopSourceUnitId ?? ''}
                    onChange={(e) => {
                      action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                      const v = e.target.value;
                      action.ActionAttribute.ForeachLoopSourceUnitId = v ? Number(v) : null;
                      onMarkChange();
                    }}
                  >
                    <option value="">(Select)</option>
                    {(hierarchy?.AppTransactionUnitList ?? []).map((u: any) => (
                      <option key={String(u?.Id)} value={u?.Id}>
                        {u?.UnitDisplayName ?? u?.DisplayName ?? u?.UnitName ?? u?.Id}
                      </option>
                    ))}
                  </select>
                </div>
              ) : null}
            </div>
          ) : null}
      </div>

      {/* --- Batch pickers/popups --- */}
      {isDataSetPickerOpen && (
        <PopupModalOverlay className="p-4" onBackdropClick={() => setIsDataSetPickerOpen(false)}>
          <div
            className={`flex max-h-[75vh] w-full max-w-lg flex-col overflow-hidden rounded-md border shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Select dataset</span>
              <button
                type="button"
                onClick={() => setIsDataSetPickerOpen(false)}
                className={`px-2 py-1 text-lg leading-none rounded-[4px] ${theme.button_default}`}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="p-2 flex items-center gap-2">
              <input
                type="text"
                autoComplete="off"
                value={dataSetPickerFilter}
                onChange={(e) => setDataSetPickerFilter(e.target.value)}
                placeholder="Filter..."
                className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
              />
            </div>
            <div className="min-h-0 flex-auto overflow-auto">
              {dataSetPickerList.map((ds: any) => (
                <button
                  key={String(ds?.Id ?? '')}
                  type="button"
                  className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.mainContentSection}`}
                  onClick={() => {
                    const id = ds?.Id ? Number(ds.Id) : null;
                    if (!id) return;
                    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                    action.ActionAttribute.BatchCommandDataSetId = id;
                    action.ActionAttribute.BatchCommandSourceDataSetFieldName = null;
                    setIsDataSetPickerOpen(false);
                    onMarkChange();
                  }}
                  onMouseEnter={() => setDataSetPickerPreviewId(ds?.Id ? Number(ds.Id) : null)}
                >
                  {normalizeDisplay(ds)} ({String(ds?.Id ?? '')})
                </button>
              ))}
            </div>
            {!!dataSetPickerPreviewId && (
              <div className="border-t p-2 text-xs">
                <span className={`${theme.label}`}>Preview: {dataSetPickerPreviewId}</span>
              </div>
            )}
          </div>
        </PopupModalOverlay>
      )}

      {isSearchPickerOpen && (
        <PopupModalOverlay onBackdropClick={() => setIsSearchPickerOpen(false)}>
          <div
            className={`flex max-h-[75vh] w-full max-w-lg flex-col overflow-hidden rounded-md border shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Select search</span>
              <button
                type="button"
                onClick={() => setIsSearchPickerOpen(false)}
                className={`px-2 py-1 text-lg leading-none rounded-[4px] ${theme.button_default}`}
                aria-label="Close"
              >
                &times;
              </button>
            </div>
            <div className="p-2 flex items-center gap-2">
              <input
                type="text"
                autoComplete="off"
                value={searchPickerFilter}
                onChange={(e) => setSearchPickerFilter(e.target.value)}
                placeholder="Filter..."
                className={`w-1 flex-auto h-7 px-2 text-xs border ${theme.inputBox}`}
              />
            </div>
            <div className="min-h-0 flex-auto overflow-auto">
              {searchPickerList.map((s: any) => (
                <button
                  key={String(s?.Id ?? '')}
                  type="button"
                  className={`w-full text-left px-3 py-2 text-xs hover:opacity-90 ${theme.mainContentSection}`}
                  onClick={() => {
                    const id = s?.Id ? Number(s.Id) : null;
                    if (!id) return;
                    action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
                    action.ActionAttribute.BatchCommandSearchId = id;
                    action.ActionAttribute.BatchCommandSourceViewFieldId = null;
                    setBatchSearchDisplayCache((prev) => ({ ...prev, [String(id)]: `${normalizeDisplay(s)} (${id})` }));
                    setIsSearchPickerOpen(false);
                    onMarkChange();
                  }}
                >
                  {normalizeDisplay(s)} ({String(s?.Id ?? '')})
                </button>
              ))}
            </div>
          </div>
        </PopupModalOverlay>
      )}

      {popupDatasetEditorId != null && (
        <EmbeddedLinkedPopupFrame
          title={`Dataset: ${popupDatasetEditorId}`}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setPopupDatasetEditorId(null)}>
              Close
            </button>
          }
        >
          <DataSetEditor ignoreRouteParam dataSetId={popupDatasetEditorId} onClose={() => setPopupDatasetEditorId(null)} />
        </EmbeddedLinkedPopupFrame>
      )}

      {popupSearchEditorId != null && (
        <EmbeddedLinkedPopupFrame
          title={`Search: ${popupSearchEditorId}`}
          toolbarTrailing={
            <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={() => setPopupSearchEditorId(null)}>
              Close
            </button>
          }
        >
          <SearchEditor ignoreRouteParam searchId={popupSearchEditorId} onClose={() => setPopupSearchEditorId(null)} />
        </EmbeddedLinkedPopupFrame>
      )}

      {isBatchSearchCriteriaMappingOpen && (
        <PopupModalOverlay className="p-4">
          <div
            className={`flex h-[400px] w-[600px] max-w-[95vw] flex-col overflow-hidden rounded-md border shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex shrink-0 items-center justify-between border-b px-3 py-2 ${theme.mainContentSection}`}>
              <div className={`text-sm font-semibold ${theme.title}`}>Search Filter Value Mapping</div>
              <button
                type="button"
                onClick={() => setIsBatchSearchCriteriaMappingOpen(false)}
                className={`px-2 py-1 text-xs rounded-[4px] ${theme.button_default}`}
                aria-label="Close"
              >
                <i className="fa-solid fa-xmark" aria-hidden />
              </button>
            </div>

            <div className="min-h-0 flex-auto p-3 overflow-hidden">
              <div className="h-full w-full overflow-hidden">
                <FlexGrid
                  itemsSource={batchSearchCriteriaMappingCV}
                  allowAddNew={false}
                  allowDelete={false}
                  autoGenerateColumns={false}
                  headersVisibility="Column"
                  selectionMode="Row"
                  className="w-full h-full"
                >
                  <FlexGridFilter />
                  <FlexGridColumn binding="criteiaId" header="Assign To Search Filter" width={280} isReadOnly={true} dataMap={batchSearchCriteriaDataMap ?? undefined} />
                  <FlexGridColumn binding="transFieldId" header="Assign From Transaction Field" width={290} dataMap={batchSearchTransFieldDataMap ?? undefined} />
                </FlexGrid>
              </div>
            </div>

            <div className={`shrink-0 border-t px-3 py-2 flex items-center gap-2 ${theme.mainContentSection}`}>
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={applyBatchSearchCriteriaMapping}>
                <i className="fa-solid fa-floppy-disk mr-1" aria-hidden />
                Apply
              </button>
              <button
                type="button"
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
                onClick={() => setIsBatchSearchCriteriaMappingOpen(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </PopupModalOverlay>
      )}
    </>
  );
}

