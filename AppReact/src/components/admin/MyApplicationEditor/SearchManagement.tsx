import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useTabNavigation } from '../../../redux/hooks/useTabNavigation';
import { searchSvc } from '../../../webapi/searchSvc';
import { adminSvc } from '../../../webapi/adminsvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';
import { useEnumValues } from '../../../hooks/useEnumDictionary';
import appHelper from '../../../helper/appHelper';

const EmAppApplicationAssetsType = { Search: 4 };
const EmAppSearchUsageType = { EshopCategorySearch: 7 };

interface SearchManagementProps {
  menuId: string | null;
}

interface SearchItem {
  Id: number;
  Name?: string;
  Description?: string;
  Type?: number;
  DataSetId?: number | null;
  DataServiceTypeDisplay?: string;
  SaasApplicationId?: number | string | null;
  AppModifiedDate?: string;
}

const SearchManagement: React.FC<SearchManagementProps> = ({ menuId }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showWarning, showInfo } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const flexGridRef = useRef<any>(null);

  const applicationId = menuId ?? null;
  const isFilterByApplicationId = Boolean(applicationId);

  const [searchList, setSearchList] = useState<SearchItem[]>([]);
  const [allSearchCV, setAllSearchCV] = useState<CollectionView | null>(null);
  const [searchTypeDataMap, setSearchTypeDataMap] = useState<DataMap | null>(null);
  const [dataSetDataMap, setDataSetDataMap] = useState<DataMap | null>(null);
  const [isFilterByApp, setIsFilterByApp] = useState(isFilterByApplicationId);
  const [createDropdownVisible, setCreateDropdownVisible] = useState(false);
  const [filterDropdownVisible, setFilterDropdownVisible] = useState(false);
  const [importPopupOpen, setImportPopupOpen] = useState(false);
  const [availableSearchList, setAvailableSearchList] = useState<SearchItem[]>([]);
  const [importSelectedIds, setImportSelectedIds] = useState<Set<number>>(new Set());
  const [errorMessages, setErrorMessages] = useState<string[]>([]);
  const createDropdownRef = useRef<HTMLDivElement>(null);
  const filterDropdownRef = useRef<HTMLDivElement>(null);

  const emAppSearchUsageType = useEnumValues('EmAppSearchUsageType');

  const appSearchUsageTypeList = React.useMemo(() => {
    if (!emAppSearchUsageType) return [];
    return Object.entries(emAppSearchUsageType).map(([key, id]) => ({
      Id: id,
      Display: key.replace(/([A-Z])/g, ' $1').trim()
    }));
  }, [emAppSearchUsageType]);

  useEffect(() => {
    setSearchTypeDataMap(new DataMap(appSearchUsageTypeList, 'Id', 'Display'));
  }, [appSearchUsageTypeList]);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [allDataSets, allSearchRaw] = await Promise.all([
        searchSvc.retrieveAllAppDataSetEntityDto(),
        searchSvc.retrieveAllAppSearchDto()
      ]);
      const allSearchList: SearchItem[] = Array.isArray(allSearchRaw) ? allSearchRaw : [];
      allSearchList.forEach((s: SearchItem) => {
        (s as any).Display = s.Name;
      });

      setDataSetDataMap(new DataMap(Array.isArray(allDataSets) ? allDataSets : [], 'Id', 'Name'));

      let list: SearchItem[];
      const useAppFilter = isFilterByApplicationId && applicationId && isFilterByApp;
      if (useAppFilter) {
        const appList = await appTransactionService.retrieveSaasApplicationSearchList(applicationId);
        list = Array.isArray(appList) ? appList : [];
        setSearchList(list);
        setAvailableSearchList(allSearchList.filter((s: SearchItem) => String(s.SaasApplicationId) !== String(applicationId)));
      } else {
        list = allSearchList;
        setSearchList(list);
        setAvailableSearchList(applicationId ? allSearchList.filter((s: SearchItem) => String(s.SaasApplicationId) !== String(applicationId)) : []);
      }

      const cv = new CollectionView(list);
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      setAllSearchCV(cv);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load data');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [applicationId, isFilterByApplicationId, isFilterByApp, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleRefresh = () => loadData();

  const handleEditSearch = useCallback(
    (selectedRow?: SearchItem) => {
      const flex = flexGridRef.current?.control ?? flexGridRef.current;
      const rowIndex = flex?.selection?.row ?? -1;
      const item = selectedRow ?? (rowIndex >= 0 && flex?.rows?.[rowIndex] ? (flex.rows[rowIndex].dataItem as SearchItem) : null);
      if (!item) {
        showWarning('Please select a search to edit');
        return;
      }
      const type = item.Type;
      const name = item.Name || `Search ${item.Id}`;
      if (type === EmAppSearchUsageType.EshopCategorySearch) {
        addTabAndNavigate('eshop-category-search-editor', name, { id: item.Id }, true);
      } else {
        addTabAndNavigate('search-editor', name, { id: item.Id }, true);
      }
    },
    [addTabAndNavigate, showWarning]
  );

  const handleCreateSearch = useCallback(
    (isEshopCategorySearch: boolean) => {
      const heading = 'New Report & View';
      if (isEshopCategorySearch) {
        addTabAndNavigate('eshop-category-search-editor', heading, { param1: applicationId }, true);
      } else {
        addTabAndNavigate('search-editor', heading, { param1: applicationId }, true);
      }
      setCreateDropdownVisible(false);
    },
    [addTabAndNavigate, applicationId]
  );

  const handleDeleteSearch = useCallback(async () => {
    const flex = flexGridRef.current?.control ?? flexGridRef.current;
    const rowIndex = flex?.selection?.row ?? -1;
    const item = rowIndex >= 0 && flex?.rows?.[rowIndex] ? (flex.rows[rowIndex].dataItem as SearchItem) : null;
    if (!item?.Id) {
      showWarning('Please select a Report to delete');
      return;
    }
    if (!window.confirm('Confirm To Delete')) return;
    dispatch(setIsBusy());
    try {
      const data = await searchSvc.deleteAppSearch(String(item.Id));
      if (data?.ValidationResult?.IsValid) {
        showInfo('Report deleted');
        await loadData();
      } else {
        const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
        setErrorMessages(msgs.length ? msgs : ['Delete failed']);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Delete failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dispatch, loadData, showError, showInfo, showWarning]);

  const openImportPopup = useCallback(() => {
    setImportSelectedIds(new Set());
    setImportPopupOpen(true);
  }, []);

  const saveImport = useCallback(async () => {
    if (!applicationId) return;
    const updateList = Array.from(importSelectedIds).map((searchId) => ({
      SearchId: searchId,
      ApplicationId: applicationId
    }));
    const saveSetDto = {
      ApplicationId: applicationId,
      EmAppApplicationAssetsType: EmAppApplicationAssetsType.Search,
      AppApplicationAssetsItemExDtoSet: updateList
    };
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.saveAppApplicationAssetsItemDtoList(saveSetDto);
      if (data?.IsSuccessful && data?.ObjectList) {
        setImportPopupOpen(false);
        await loadData();
      }
      const msgs = data?.ValidationResult?.Items?.map((i: any) => i.ErrorMessage).filter(Boolean) || [];
      setErrorMessages(msgs.length ? msgs : []);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [applicationId, importSelectedIds, dispatch, loadData, showError]);

  const toggleImportSelection = (id: number) => {
    setImportSelectedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      if (createDropdownRef.current?.contains(target) || filterDropdownRef.current?.contains(target)) return;
      setCreateDropdownVisible(false);
      setFilterDropdownVisible(false);
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, []);

  const handleSelectionChanged = useCallback((s: any) => {
    const flex = s?.control ?? s;
    appHelper.debugLog('SearchManagement selectionChanged', { row: flex?.selection?.row });
  }, []);

  const toolbarButtonBaseClass =
    'h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60';
  const refreshButtonClass = `${toolbarButtonBaseClass} bg-blue-500 hover:bg-blue-600`;
  const createButtonClass = `${toolbarButtonBaseClass} bg-emerald-500 hover:bg-emerald-600`;
  const importButtonClass = `${toolbarButtonBaseClass} bg-cyan-500 hover:bg-cyan-600`;
  const editButtonClass = `${toolbarButtonBaseClass} bg-amber-500 hover:bg-amber-600`;
  const deleteButtonClass = `${toolbarButtonBaseClass} bg-rose-500 hover:bg-rose-600`;
  const filterButtonClass = `${toolbarButtonBaseClass} bg-teal-500 hover:bg-teal-600`;

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      {/* Header toolbar */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Report List:</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={handleRefresh}
            className={refreshButtonClass}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            Refresh
          </button>

          <div className="relative" ref={createDropdownRef}>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setCreateDropdownVisible(!createDropdownVisible);
                setFilterDropdownVisible(false);
              }}
              className={createButtonClass}
            >
              <i className="fa-solid fa-plus" aria-hidden />
              Create <i className="fa-solid fa-caret-down text-xs" aria-hidden />
            </button>
            {createDropdownVisible && (
              <div
                className={`absolute right-0 mt-1 min-w-[180px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}
                onClick={(e) => e.stopPropagation()}
              >
                <button
                  type="button"
                  onClick={() => handleCreateSearch(false)}
                  className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                >
                  Regular Report
                </button>
                <button
                  type="button"
                  onClick={() => handleCreateSearch(true)}
                  className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                >
                  Eshop Category Search
                </button>
              </div>
            )}
          </div>

          {isFilterByApplicationId && (
            <button
              type="button"
              onClick={openImportPopup}
              className={importButtonClass}
              title="Import searches from library"
            >
              <i className="fa-solid fa-search-plus" aria-hidden />
              Import
            </button>
          )}

          <button
            type="button"
            onClick={() => handleEditSearch()}
            className={editButtonClass}
          >
            <i className="fa-solid fa-pen-to-square" aria-hidden />
            Edit
          </button>
          <button
            type="button"
            onClick={handleDeleteSearch}
            className={deleteButtonClass}
          >
            <i className="fa-solid fa-trash" aria-hidden />
            Delete
          </button>
          {isFilterByApplicationId && (
            <div className="relative" ref={filterDropdownRef}>
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  setFilterDropdownVisible(!filterDropdownVisible);
                  setCreateDropdownVisible(false);
                }}
                className={filterButtonClass}
              >
                <i className="fa-solid fa-filter" aria-hidden />
                Filter <i className="fa-solid fa-caret-down text-xs" aria-hidden />
              </button>
              {filterDropdownVisible && (
                <div
                  className={`absolute right-0 mt-1 min-w-[160px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}
                  onClick={(e) => e.stopPropagation()}
                >
                  <button
                    type="button"
                    onClick={() => {
                      setIsFilterByApp(true);
                      setFilterDropdownVisible(false);
                      loadData();
                    }}
                    className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu} ${isFilterByApp ? theme.tab_active : ''}`}
                  >
                    By Current Application {isFilterByApp && <i className="fa-solid fa-check ml-1" aria-hidden />}
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setIsFilterByApp(false);
                      setFilterDropdownVisible(false);
                      loadData();
                    }}
                    className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu} ${!isFilterByApp ? theme.tab_active : ''}`}
                  >
                    Show All Searches {!isFilterByApp && <i className="fa-solid fa-check ml-1" aria-hidden />}
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Grid */}
      <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
        {allSearchCV && (
          <FlexGrid
            ref={flexGridRef}
            itemsSource={allSearchCV}
            selectionMode="Row"
            isReadOnly={true}
            allowDelete={false}
            selectionChanged={handleSelectionChanged}
            style={{ height: '100%' }}
          >
            <FlexGridFilter />
            <FlexGridColumn isReadOnly={true} width={60}>
              <FlexGridCellTemplate
                cellType="Cell"
                template={(cell: any) => {
                  const item = cell.item;
                  if (!item) return null;
                  return (
                    <div className="flex justify-center">
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleEditSearch(item);
                        }}
                        className={`w-8 h-6 ${theme.button_default} rounded-[4px] text-xs`}
                        title="Edit"
                      >
                        <i className="fa-solid fa-pen-to-square" aria-hidden />
                      </button>
                    </div>
                  );
                }}
              />
            </FlexGridColumn>
            <FlexGridColumn binding="Id" header="Id" width={60} format="d" />
            <FlexGridColumn binding="Name" header="Name" width={300} />
            <FlexGridColumn binding="Description" header="Description" width={200} />
            <FlexGridColumn
              binding="Type"
              header="Type"
              width={200}
              dataMap={searchTypeDataMap ?? undefined}
            />
            <FlexGridColumn
              binding="DataSetId"
              header="Data Service"
              width={300}
              dataMap={dataSetDataMap ?? undefined}
            />
            <FlexGridColumn binding="DataServiceTypeDisplay" header="Data Service Type" width={300} />
            <FlexGridColumn header="" binding="" width="*" allowSorting={false} isReadOnly={true} />
          </FlexGrid>
        )}
      </div>

      {/* Import popup */}
      {importPopupOpen && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/30" onClick={() => setImportPopupOpen(false)}>
          <div
            className={`max-h-[80vh] w-[500px] flex flex-col rounded shadow-lg ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
              <span className={`text-sm font-semibold ${theme.title}`}>Import Searches From Library</span>
              <button type="button" onClick={() => setImportPopupOpen(false)} className="text-lg leading-none px-2 py-1">&times;</button>
            </div>
            <div className="flex-1 overflow-auto p-3">
              {availableSearchList.length === 0 ? (
                <p className={`text-sm ${theme.label}`}>No searches available to import.</p>
              ) : (
                <ul className="space-y-1">
                  {availableSearchList.map((s) => (
                    <li key={s.Id} className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={importSelectedIds.has(s.Id)}
                        onChange={() => toggleImportSelection(s.Id)}
                        className="rounded"
                      />
                      <span className="text-sm">{s.Name ?? s.Id}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
            <div className="flex justify-end gap-2 p-3 border-t">
              <button type="button" onClick={() => setImportPopupOpen(false)} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Cancel
              </button>
              <button type="button" onClick={saveImport} disabled={availableSearchList.length === 0} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                Save
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Validation messages */}
      {errorMessages.length > 0 && (
        <div className={`px-3 py-2 text-sm border-t ${theme.mainContentSection}`}>
          {errorMessages.map((msg, i) => (
            <div key={i} className="text-red-600">
              {msg}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default SearchManagement;
