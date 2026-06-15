import React, { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { searchSvc } from '../../webapi/searchSvc';
import { addTab } from '../../redux/features/ui/navigation/tabnavSlice';
import { useNavigate } from 'react-router-dom';

interface SearchApiSettingItem {
  ConsumeOrProvideType?: string;
  CRUDType?: string;
  OperationId?: number | null;
  ActionCode?: string;
  BaseUrl?: string;
  Url?: string;
  HttpMethd?: string;
}

const SearchApiSetting: React.FC = () => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();
  const { showError, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const navigate = useNavigate();

  const [searchId, setSearchId] = useState<number | null>(null);
  const [searchName, setSearchName] = useState<string>('');
  const [defaultDataSourceId, setDefaultDataSourceId] = useState<number | null>(null);
  const [activeTabIndex, setActiveTabIndex] = useState<number>(0); // 0 = Consume, 1 = Provide
  const [consumeCV, setConsumeCV] = useState<CollectionView | null>(null);
  const [provideCV, setProvideCV] = useState<CollectionView | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!param) return;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const idVal = obj.id ?? obj.Id ?? null;
      setSearchId(idVal != null ? Number(idVal) : null);
      setSearchName(obj.param1 ?? obj.searchName ?? '');

      if (obj.param2) {
        try {
          const p2 = typeof obj.param2 === 'string' ? JSON.parse(obj.param2) : obj.param2;
          if (p2?.apiConsumeOrProvideType === 'Provide') {
            setActiveTabIndex(1);
          }
        } catch {
          // ignore param2 parse errors
        }
      }
    } catch {
      // if parsing fails, leave defaults
    }
  }, [param]);

  const loadData = useCallback(async () => {
    if (!searchId) {
      showWarning('No search selected for API settings.');
      return;
    }
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [dataSourceList, apiSettings] = await Promise.all([
        adminSvc.retrieveAllAppDataSourceRegisterExDto(),
        searchSvc.retrieveSearchApiSettings(String(searchId)),
      ]);

      // Default datasource (company master DB) – same rule as Angular
      let defaultDs: number | null = null;
      if (Array.isArray(dataSourceList)) {
        dataSourceList.forEach((ds: any) => {
          if (ds.Id !== 2147483647 && ds.IsCompanyMasterDb && defaultDs == null) {
            defaultDs = ds.Id;
          }
        });
      }
      setDefaultDataSourceId(defaultDs);

      const list: SearchApiSettingItem[] = Array.isArray(apiSettings) ? apiSettings : [];
      const consumeList = list.filter((o) => o.ConsumeOrProvideType === 'Consume');
      const provideList = list.filter((o) => o.ConsumeOrProvideType === 'Provide');

      const consumeCv = new CollectionView(consumeList);
      consumeCv.sortDescriptions.push(new SortDescription('ActionCode', true));
      setConsumeCV(consumeCv);

      const provideCv = new CollectionView(provideList);
      provideCv.sortDescriptions.push(new SortDescription('ActionCode', true));
      setProvideCV(provideCv);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load Search API settings');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [searchId, dispatch, showError, showWarning]);

  useEffect(() => {
    if (searchId != null) {
      loadData();
    }
  }, [searchId, loadData]);

  const handleCreateProvideApi = useCallback(() => {
    if (!searchId) {
      showWarning('Save the search first.');
      return;
    }
    const param2 = JSON.stringify({
      dataSourceId: defaultDataSourceId,
      searchId,
    });
    const path = `/app-data-presentation-api-editor?param2=${encodeURIComponent(param2)}`;
    dispatch(addTab({ tabPath: path, label: 'New Search API', isClosable: true }));
    navigate(path);
  }, [defaultDataSourceId, searchId, dispatch, navigate, showWarning]);

  const handleEditApi = useCallback(
    (item: SearchApiSettingItem) => {
      if (!item?.OperationId) return;
      const label = item.ActionCode ? `API: ${item.ActionCode}` : `API (${item.OperationId})`;
      const path = `/app-data-presentation-api-editor/${item.OperationId}`;
      dispatch(addTab({ tabPath: path, label, isClosable: true }));
      navigate(path);
    },
    [dispatch, navigate],
  );

  if (isLoading && !consumeCV && !provideCV) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
        <div className="p-3 text-xs">Loading...</div>
      </div>
    );
  }

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex-1 flex flex-col min-h-0 ${theme.mainContentSection}`}>
        {activeTabIndex === 0 && (
          <div className="flex-1 flex flex-col">
            <div className="flex items-center justify-between px-3 py-2 border-b">
              <div className={`text-sm font-semibold ${theme.title}`}>
                Consume APIs (Call 3rd Part APIs){searchName ? `: ${searchName}` : ''}
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={loadData}
                  className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-rotate" aria-hidden /> Refresh
                </button>
              </div>
            </div>
            <div className="flex-1 min-h-0 p-2">
              {consumeCV ? (
                <FlexGrid
                  itemsSource={consumeCV}
                  isReadOnly={true}
                  selectionMode="Row"
                  className="w-full h-full"
                >
                  <FlexGridFilter />
                  <FlexGridColumn width={120} header="">
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        const item = cell.item as SearchApiSettingItem;
                        if (!item) return null;
                        return (
                          <div className="flex justify-center">
                            <button
                              type="button"
                              onClick={() => handleEditApi(item)}
                              className={`px-2 py-1 text-[11px] rounded ${theme.button_default}`}
                              title="Edit API"
                            >
                              <i className="fa-solid fa-pen" aria-hidden /> Edit
                            </button>
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                  <FlexGridColumn binding="OperationId" header="Operation Id" width={100} format="d" />
                  <FlexGridColumn binding="ActionCode" header="Operation Code" width={220} />
                  <FlexGridColumn binding="HttpMethd" header="Http Method" width={100} />
                  <FlexGridColumn binding="" header="" width="*" isReadOnly allowSorting={false} />
                </FlexGrid>
              ) : (
                <div className={`text-xs px-3 py-4 ${theme.label}`}>No Consume APIs.</div>
              )}
            </div>
          </div>
        )}

        {activeTabIndex === 1 && (
          <div className="flex-1 flex flex-col">
            <div className="flex items-center justify-between px-3 py-2 border-b">
              <div className={`text-sm font-semibold ${theme.title}`}>
                Provide APIs (Provide APIs To 3rd Part){searchName ? `: ${searchName}` : ''}
              </div>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={loadData}
                  className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-rotate" aria-hidden /> Refresh
                </button>
                <button
                  type="button"
                  onClick={handleCreateProvideApi}
                  className={`px-3 py-1.5 text-xs rounded-[4px] ${theme.button_default}`}
                >
                  <i className="fa-solid fa-pen" aria-hidden /> Create
                </button>
              </div>
            </div>
            <div className="flex-1 min-h-0 p-2">
              {provideCV ? (
                <FlexGrid
                  itemsSource={provideCV}
                  isReadOnly={true}
                  selectionMode="Row"
                  className="w-full h-full"
                >
                  <FlexGridFilter />
                  <FlexGridColumn width={120} header="">
                    <FlexGridCellTemplate
                      cellType="Cell"
                      template={(cell: any) => {
                        const item = cell.item as SearchApiSettingItem;
                        if (!item) return null;
                        return (
                          <div className="flex justify-center">
                            <button
                              type="button"
                              onClick={() => handleEditApi(item)}
                              className={`px-2 py-1 text-[11px] rounded ${theme.button_default}`}
                              title="Edit API"
                            >
                              <i className="fa-solid fa-pen" aria-hidden /> Edit
                            </button>
                          </div>
                        );
                      }}
                    />
                  </FlexGridColumn>
                  <FlexGridColumn binding="OperationId" header="Operation Id" width={100} format="d" />
                  <FlexGridColumn binding="ActionCode" header="Operation Code" width={220} />
                  <FlexGridColumn binding="HttpMethd" header="Http Method" width={100} />
                  <FlexGridColumn binding="" header="" width="*" isReadOnly allowSorting={false} />
                </FlexGrid>
              ) : (
                <div className={`text-xs px-3 py-4 ${theme.label}`}>No Provide APIs.</div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default SearchApiSetting;
