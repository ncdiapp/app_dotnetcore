import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, DataType, SortDescription } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useEnumValues } from '../../hooks/useEnumDictionary';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { appTransactionService } from '../../webapi/apptransactionsvc';
import { searchSvc } from '../../webapi/searchSvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';

type DbToDbImportSettingRow = any;

type ImportCreationModel = {
  SourceDataSourceFrom: number | null;
  SourceDataSourceType: number | null;
  SourceTableUiId: string | null;
  SourceDataSetId: number | null;
  TargetDataSourceFrom: number | null;
  IsSpilitToMultipleTables: boolean;
};

export type DbToDbImportManagementProps = {
  /** Angular: param2.isUsedAsSelector — hosted in command editor selector popup. */
  isUsedAsSelector?: boolean;
  onConfirmSelection?: (item: DbToDbImportSettingRow) => void;
  onRequestClose?: () => void;
  /** When embedded, open import editor in parent popup instead of app tab. */
  onOpenImportEditor?: (importSettingId: number) => void;
};

const DbToDbImportManagement: React.FC<DbToDbImportManagementProps> = (props) => {
  const { isUsedAsSelector = false, onConfirmSelection, onRequestClose, onOpenImportEditor } = props;
  const { theme } = useTheme();
  const { showError, showWarning } = useErrorMessage();
  const dispatch = useDispatch();
  const { addTabAndNavigate } = useTabNavigation();

  const importStatusEnum = useEnumValues('EmAppImportStatus');
  const sourceTypeEnum = useEnumValues('EmAppDbToDbImportSourceType');

  const flexRef = useRef<wjGrid.FlexGrid | null>(null);
  const [cv] = useState<CollectionView>(() => new CollectionView<DbToDbImportSettingRow>([]));

  const [dataSources, setDataSources] = useState<any[]>([]);
  const [allDataSetList, setAllDataSetList] = useState<any[]>([]);
  const [dictTableUiIdAndDto, setDictTableUiIdAndDto] = useState<Record<string, any>>({});

  const [isLoading, setIsLoading] = useState(false);
  const [filterByCurrentApp, setFilterByCurrentApp] = useState(false);

  // We are hosted inside DatabaseDesignManagement; applicationId can be passed in future via tab param.
  const applicationId: number | null = null;

  const [contextMenuOpen, setContextMenuOpen] = useState(false);
  const [contextMenuPos, setContextMenuPos] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [selectedRowData, setSelectedRowData] = useState<DbToDbImportSettingRow | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
  const [createModel, setCreateModel] = useState<ImportCreationModel | null>(null);
  const [sourceTablesCV, setSourceTablesCV] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const [sourceDataSetsCV, setSourceDataSetsCV] = useState<CollectionView<any>>(() => new CollectionView<any>([]));

  const dataSourceMap = useMemo(() => (dataSources.length ? new DataMap(dataSources, 'Id', 'DataSourceName') : null), [dataSources]);
  const dataSetMap = useMemo(() => (allDataSetList.length ? new DataMap(allDataSetList, 'Id', 'Name') : null), [allDataSetList]);
  const importStatusList = useMemo(() => {
    const dict = importStatusEnum;
    if (!dict) return [];
    return Object.entries(dict).map(([key, id]) => ({ Id: id, Display: key }));
  }, [importStatusEnum]);

  const sourceTypeList = useMemo(() => {
    const dict = sourceTypeEnum;
    if (!dict) return [];
    return Object.entries(dict).map(([key, id]) => ({ Id: id, Display: key }));
  }, [sourceTypeEnum]);

  const importStatusMap = useMemo(() => new DataMap(importStatusList, 'Id', 'Display'), [importStatusList]);
  const sourceTypeMap = useMemo(() => new DataMap(sourceTypeList, 'Id', 'Display'), [sourceTypeList]);

  const closeContextMenu = useCallback(() => {
    setContextMenuOpen(false);
    setSelectedRowData(null);
  }, []);

  const openContextMenu = useCallback((e: React.MouseEvent, item: DbToDbImportSettingRow) => {
    e.preventDefault();
    e.stopPropagation();
    setSelectedRowData(item);
    setContextMenuPos({ x: e.clientX, y: e.clientY });
    setContextMenuOpen(true);
  }, []);

  useEffect(() => {
    if (!contextMenuOpen) return;
    const handler = () => closeContextMenu();
    document.addEventListener('click', handler);
    return () => document.removeEventListener('click', handler);
  }, [contextMenuOpen, closeContextMenu]);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    dispatch(setIsBusy());
    try {
      const [list, dsList, allDataSets] = await Promise.all([
        filterByCurrentApp && applicationId
          ? appTransactionService.retrieveSaasApplicationDbToDbTableImportSettingList(String(applicationId))
          : appTransactionService.retrieveAllDbToDbTableImportSettingDto(),
        adminSvc.getDataSourceRegisterList(false),
        searchSvc.retrieveAllAppDataSetEntityDto(),
      ]);

      const safeList = (Array.isArray(list) ? list : []).filter((d: any) => !d?.BaseDataSetId);
      cv.sourceCollection = safeList;
      cv.sortDescriptions.clear();
      cv.sortDescriptions.push(new SortDescription('AppModifiedDate', false));
      cv.sortDescriptions.push(new SortDescription('Id', false));
      cv.refresh();

      setDataSources(Array.isArray(dsList) ? dsList : []);
      setAllDataSetList(Array.isArray(allDataSets) ? allDataSets : []);
    } catch (e: any) {
      showError(e?.message || 'Failed to load DB-to-DB import settings');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [dispatch, filterByCurrentApp, applicationId, cv, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const openImportEditor = useCallback(
    (dto: DbToDbImportSettingRow | null | undefined) => {
      if (!dto?.Id) return;
      if (onOpenImportEditor) {
        onOpenImportEditor(Number(dto.Id));
        return;
      }
      const label = dto.Name || `Import Setting (${dto.Id})`;
      addTabAndNavigate('db-to-db-import-editor', label, { id: dto.Id }, true);
    },
    [addTabAndNavigate, onOpenImportEditor],
  );

  const handleRowDoubleClick = useCallback(
    (flex: wjGrid.FlexGrid) => {
      const rowIndex = flex.selection?.row;
      if (rowIndex == null || rowIndex < 0) return;
      const dto = flex.rows[rowIndex]?.dataItem as DbToDbImportSettingRow | undefined;
      openImportEditor(dto);
    },
    [openImportEditor],
  );

  const confirmSelectionAndClose = useCallback(() => {
    const flex = flexRef.current;
    if (!flex || !onConfirmSelection) return;
    const rowIndex = flex.selection?.row;
    if (rowIndex == null || rowIndex < 0) return;
    const dataItem = flex.rows[rowIndex]?.dataItem as DbToDbImportSettingRow | undefined;
    if (!dataItem?.Id) return;
    onConfirmSelection(dataItem);
    onRequestClose?.();
  }, [onConfirmSelection, onRequestClose]);

  const deleteById = useCallback(
    async (id: number) => {
      try {
        dispatch(setIsBusy());
        const result = await searchSvc.deleteOneAppDataSetEntityDto(String(id));
        if (result?.ValidationResult?.IsValid ?? result?.IsSuccessful ?? true) {
          await loadData();
        } else {
          const msg =
            result?.ValidationResult?.ErrorMessage ||
            result?.ValidationResult?.Message ||
            'Failed to delete import setting';
          showWarning(msg);
        }
      } catch (e: any) {
        showError(e?.message || 'Failed to delete import setting');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [dispatch, loadData, showError, showWarning],
  );

  const contextMenuOpenSetting = useCallback(() => {
    if (!selectedRowData?.Id) {
      closeContextMenu();
      return;
    }
    const dto = selectedRowData;
    closeContextMenu();
    openImportEditor(dto);
  }, [selectedRowData, closeContextMenu, openImportEditor]);

  const contextMenuDelete = useCallback(() => {
    if (!selectedRowData?.Id) {
      closeContextMenu();
      return;
    }
    const ok = window.confirm(`Please confirm to delete:\n${selectedRowData.Name ?? 'Import Setting'}`);
    if (ok) deleteById(Number(selectedRowData.Id));
    closeContextMenu();
  }, [selectedRowData, deleteById, closeContextMenu]);

  const contextMenuOpenImportedFormData = useCallback(() => {
    const searchId = selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.DefaultSearchId;
    closeContextMenu();
    if (searchId) addTabAndNavigate('masterdatamanagement', 'Search', { searchId, isSavedSearch: false }, true);
  }, [selectedRowData, closeContextMenu, addTabAndNavigate]);

  const contextMenuOpenTransactionEditor = useCallback(() => {
    const transactionId = selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.NeedToUpdateTransactionId;
    closeContextMenu();
    if (transactionId) addTabAndNavigate('application-form-builder', 'Application Builder', { transactionId }, true);
  }, [selectedRowData, closeContextMenu, addTabAndNavigate]);

  const contextMenuOpenDataUpdateApi = useCallback(() => {
    const apiId = selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.DefaultUpdateApiId;
    closeContextMenu();
    if (apiId) addTabAndNavigate('excel-import-data-update-api-editor', `API: ${apiId}`, { apiId }, true);
  }, [selectedRowData, closeContextMenu, addTabAndNavigate]);

  const handleRefresh = useCallback(() => loadData(), [loadData]);

  const openCreatePopup = useCallback(() => {
    setCreateModel({
      SourceDataSourceFrom: null,
      SourceDataSourceType: null,
      SourceTableUiId: null,
      SourceDataSetId: null,
      TargetDataSourceFrom: null,
      IsSpilitToMultipleTables: false,
    });
    setSourceTablesCV(new CollectionView<any>([]));
    setSourceDataSetsCV(new CollectionView<any>([]));
    setDictTableUiIdAndDto({});
    setCreateOpen(true);
  }, []);

  const closeCreatePopup = useCallback(() => {
    setCreateOpen(false);
    setCreateModel(null);
  }, []);

  const sourceDbChanged = useCallback(
    async (newSourceDbId: number | null) => {
      if (!createModel) return;
      if (!newSourceDbId) {
        setSourceTablesCV(new CollectionView<any>([]));
        setSourceDataSetsCV(new CollectionView<any>([]));
        setDictTableUiIdAndDto({});
        setCreateModel((m) => (m ? { ...m, SourceTableUiId: null, SourceDataSetId: null } : m));
        return;
      }
      try {
        dispatch(setIsBusy());
        // Cache-first list is fine for selector.
        const dbSchemaData = await schemaMetadataService.getDataSourceTableAndViewListFromCache(newSourceDbId, null, null);
        const list = Array.isArray(dbSchemaData) ? dbSchemaData : [];
        const dict: Record<string, any> = {};
        const withUi = list.map((t: any) => {
          const UiId = `${Date.now()}_${Math.random().toString(16).slice(2)}`;
          const dto = { ...t, UiId };
          dict[UiId] = dto;
          return dto;
        });
        setDictTableUiIdAndDto(dict);
        setSourceTablesCV(new CollectionView<any>(withUi));

        const dsList = allDataSetList.filter((d: any) => Number(d?.DataSourceFrom) === Number(newSourceDbId) && d?.QueryText);
        setSourceDataSetsCV(new CollectionView<any>(dsList));

        setCreateModel((m) => (m ? { ...m, SourceTableUiId: null, SourceDataSetId: null } : m));
      } catch (e: any) {
        showError(e?.message || 'Failed to load source tables/datasets');
      } finally {
        dispatch(setIsNotBusy());
      }
    },
    [createModel, dispatch, allDataSetList, showError],
  );

  const isReadyToCreate = useMemo(() => {
    const m = createModel;
    if (!m?.SourceDataSourceFrom || !m?.TargetDataSourceFrom || !m?.SourceDataSourceType) return false;
    if (Number(m.SourceDataSourceType) === 1) return !!m.SourceTableUiId;
    if (Number(m.SourceDataSourceType) === 2) return !!m.SourceDataSetId;
    return false;
  }, [createModel]);

  const createImportSetting = useCallback(async () => {
    if (!createModel || !isReadyToCreate) return;
    try {
      dispatch(setIsBusy());

      const dataSetDto: any = {
        DataSourceFrom: createModel.TargetDataSourceFrom,
        SaasApplicationId: applicationId,
        OtherSettingsDto: {
          TableImportSettingDto: {
            SourceDataSourceFrom: createModel.SourceDataSourceFrom,
            SourceDataSourceType: createModel.SourceDataSourceType,
            SourceDataSetId: createModel.SourceDataSetId,
            SourceTableName: null,
            Tables: [],
            IsSpilitToMultipleTables: !!createModel.IsSpilitToMultipleTables,
          },
        },
      };

      if (createModel.SourceTableUiId && dictTableUiIdAndDto[createModel.SourceTableUiId]) {
        dataSetDto.OtherSettingsDto.TableImportSettingDto.SourceTableName = dictTableUiIdAndDto[createModel.SourceTableUiId].Name;
      }

      const result = await schemaMetadataService.createDbToDbTableImportSetting(dataSetDto);
      if (result?.IsSuccessful && result.Object?.Id) {
        const created = result.Object;
        closeCreatePopup();
        await loadData();
        openImportEditor(created);
      } else {
        const msg =
          result?.ValidationResult?.ErrorMessage ||
          result?.ValidationResult?.Message ||
          'Failed to create import setting';
        showWarning(msg);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to create import setting');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [
    createModel,
    isReadyToCreate,
    dispatch,
    applicationId,
    dictTableUiIdAndDto,
    closeCreatePopup,
    loadData,
    openImportEditor,
    showError,
    showWarning,
  ]);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>
          {isUsedAsSelector
            ? 'Database To Database Import Setting Selector:'
            : 'Database To Database Import Setting Management'}
        </div>
        <div className="flex items-center gap-2">
          {isUsedAsSelector ? (
            <button
              type="button"
              onClick={confirmSelectionAndClose}
              disabled={isLoading}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
              title="Confirm Selection & Close"
            >
              <i className="fa-solid fa-check mr-1" aria-hidden /> Confirm Selection &amp; Close
            </button>
          ) : null}
          <button
            type="button"
            onClick={handleRefresh}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button
            type="button"
            onClick={openCreatePopup}
            disabled={isLoading}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
            title="New Import"
          >
            <i className="fa-solid fa-plus" aria-hidden /> New Import
          </button>
          {applicationId && (
            <button
              type="button"
              onClick={() => {
                setFilterByCurrentApp((v) => !v);
              }}
              disabled={isLoading}
              className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
              title="Filter"
            >
              <i className="fa-solid fa-filter" aria-hidden /> {filterByCurrentApp ? 'By Current App' : 'Show All'}
            </button>
          )}
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          ref={flexRef}
          itemsSource={cv}
          autoGenerateColumns={false}
          selectionMode="Row"
          isReadOnly
          allowDelete={false}
          initialized={(flex: wjGrid.FlexGrid) => {
            flexRef.current = flex;
            flex.hostElement.addEventListener('dblclick', () => handleRowDoubleClick(flex));
          }}
          className="w-full h-full !border-0"
        >
          <FlexGridColumn isReadOnly width={80} header="Actions">
            <FlexGridCellTemplate
              cellType="Cell"
              template={(ctx: any) => (
                <div className="flex justify-center w-full">
                  <button
                    type="button"
                    className={theme.menu_default}
                    style={{ width: '30px' }}
                    title="More Options"
                    onClick={(e) => openContextMenu(e, ctx.item)}
                  >
                    <i className="fa-solid fa-pencil" aria-hidden style={{ fontSize: '12px' }} />
                    <i
                      className="fa-solid fa-bars"
                      aria-hidden
                      style={{ position: 'relative', left: '-1px', top: '2px', fontSize: '9px' }}
                    />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>

          <FlexGridColumn binding="Id" header="Id" width={70} dataType={DataType.Number} />
          <FlexGridColumn binding="Name" header="Name" width={260} />
          <FlexGridColumn binding="Description" header="Description" width={220} />
          <FlexGridColumn
            binding="OtherSettingsDto.TableImportSettingDto.Status"
            header="Import Status"
            width={140}
            dataMap={importStatusMap}
          />
          <FlexGridColumn
            binding="OtherSettingsDto.TableImportSettingDto.SourceDataSourceFrom"
            header="Source Data Source"
            width={200}
            dataMap={dataSourceMap ?? undefined}
          />
          <FlexGridColumn
            binding="OtherSettingsDto.TableImportSettingDto.SourceDataSourceType"
            header="Source Type"
            width={140}
            dataMap={sourceTypeMap}
          />
          <FlexGridColumn binding="OtherSettingsDto.TableImportSettingDto.SourceTableName" header="Source Table" width={200} />
          <FlexGridColumn
            binding="OtherSettingsDto.TableImportSettingDto.SourceDataSetId"
            header="Source DataSet"
            width={200}
            dataMap={dataSetMap ?? undefined}
          />
          <FlexGridColumn binding="DataSourceFrom" header="Destination Database" width={200} dataMap={dataSourceMap ?? undefined} />
          <FlexGridColumn binding="AppModifiedDate" header="Modified Date" width={150} dataType={DataType.Date} />
          <FlexGridColumn binding="AppCreatedDate" header="Created Date" width={150} dataType={DataType.Date} />
          <FlexGridColumn binding="" header="" width="*" isReadOnly />
        </FlexGrid>
      </div>

      {contextMenuOpen && (
        <div
          className={`fixed z-50 ${theme.mainContentSection} border rounded-[4px] shadow-lg py-1 min-w-[240px]`}
          style={{ left: contextMenuPos.x, top: contextMenuPos.y }}
          onClick={(e) => e.stopPropagation()}
        >
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuOpenSetting}
          >
            <i className="fa-solid fa-gear mr-2 flex-shrink-0" aria-hidden /> Open Import Setting
          </button>
          {!!selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.DefaultSearchId && (
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={contextMenuOpenImportedFormData}
            >
              <i className="fa-solid fa-list mr-2 flex-shrink-0" aria-hidden /> View Imported Form Data
            </button>
          )}
          {!!selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.NeedToUpdateTransactionId && (
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={contextMenuOpenTransactionEditor}
            >
              <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Edit Imported Data Model
            </button>
          )}
          {!!selectedRowData?.OtherSettingsDto?.TableImportSettingDto?.DefaultUpdateApiId && (
            <button
              type="button"
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
              onClick={contextMenuOpenDataUpdateApi}
            >
              <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden /> Open Data Update API
            </button>
          )}
          <div className="my-1 border-t opacity-60" />
          <button
            type="button"
            className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center`}
            onClick={contextMenuDelete}
          >
            <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden /> Delete Import Setting
          </button>
        </div>
      )}

      {createOpen && createModel && (
        <div className="fixed inset-0 z-40 flex items-center justify-center bg-black/20" onClick={closeCreatePopup}>
          <div
            className={`w-[520px] max-w-[95vw] rounded-[6px] overflow-hidden ${theme.mainContentSection}`}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between px-3 py-2">
              <div className={`text-sm font-semibold ${theme.title}`}>New Import Setting</div>
              <button type="button" className={`px-2 py-1 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeCreatePopup}>
                ×
              </button>
            </div>
            <div className="px-4 pb-4 pt-2 flex gap-4">
              <div className="w-1 flex-auto">
                <div className="text-xs mb-1">Source Database</div>
                <select
                  className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                  value={createModel.SourceDataSourceFrom ?? ''}
                  onChange={(e) => {
                    const v = e.target.value ? Number(e.target.value) : null;
                    setCreateModel((m) => (m ? { ...m, SourceDataSourceFrom: v } : m));
                    sourceDbChanged(v);
                  }}
                >
                  <option value="">(select)</option>
                  {dataSources.map((d) => (
                    <option key={d.Id} value={d.Id}>
                      {d.DataSourceName}
                    </option>
                  ))}
                </select>

                {!!createModel.SourceDataSourceFrom && (
                  <>
                    <div className="text-xs mt-3 mb-1">Datasource Type</div>
                    <select
                      className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                      value={createModel.SourceDataSourceType ?? ''}
                      onChange={(e) => {
                        const v = e.target.value ? Number(e.target.value) : null;
                        setCreateModel((m) =>
                          m
                            ? { ...m, SourceDataSourceType: v, SourceTableUiId: null, SourceDataSetId: null }
                            : m,
                        );
                      }}
                    >
                      <option value="">(select)</option>
                      {sourceTypeList.map((x: any) => (
                        <option key={x.Id} value={x.Id}>
                          {x.Display}
                        </option>
                      ))}
                    </select>
                  </>
                )}

                <div className="mt-3">
                  {Number(createModel.SourceDataSourceType) === 1 && (
                    <>
                      <div className="text-xs mb-1">Source Table</div>
                      <select
                        className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                        value={createModel.SourceTableUiId ?? ''}
                        onChange={(e) => setCreateModel((m) => (m ? { ...m, SourceTableUiId: e.target.value || null } : m))}
                      >
                        <option value="">(select)</option>
                        {(sourceTablesCV?.items ?? []).map((t: any) => (
                          <option key={t.UiId} value={t.UiId}>
                            {t.Name}
                          </option>
                        ))}
                      </select>
                    </>
                  )}
                  {Number(createModel.SourceDataSourceType) === 2 && (
                    <>
                      <div className="text-xs mb-1">Source Dataset</div>
                      <select
                        className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                        value={createModel.SourceDataSetId ?? ''}
                        onChange={(e) => setCreateModel((m) => (m ? { ...m, SourceDataSetId: e.target.value ? Number(e.target.value) : null } : m))}
                      >
                        <option value="">(select)</option>
                        {(sourceDataSetsCV?.items ?? []).map((d: any) => (
                          <option key={d.Id} value={d.Id}>
                            {d.Name}
                          </option>
                        ))}
                      </select>
                    </>
                  )}
                </div>
              </div>

              <div className="w-1 flex-auto">
                <div className="text-xs mb-1">Destination Database</div>
                <select
                  className={`w-full h-8 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
                  value={createModel.TargetDataSourceFrom ?? ''}
                  onChange={(e) =>
                    setCreateModel((m) => (m ? { ...m, TargetDataSourceFrom: e.target.value ? Number(e.target.value) : null } : m))
                  }
                >
                  <option value="">(select)</option>
                  {dataSources.map((d) => (
                    <option key={d.Id} value={d.Id}>
                      {d.DataSourceName}
                    </option>
                  ))}
                </select>

                {Number(createModel.SourceDataSourceType) === 2 && (
                  <div className="mt-3 flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={!!createModel.IsSpilitToMultipleTables}
                      onChange={(e) => setCreateModel((m) => (m ? { ...m, IsSpilitToMultipleTables: e.target.checked } : m))}
                    />
                    <div className="text-xs">Import To Multi-Tables</div>
                  </div>
                )}
              </div>
            </div>
            <div className="px-4 pb-4 flex justify-end gap-2">
              <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={closeCreatePopup}>
                Cancel
              </button>
              <button
                type="button"
                disabled={!isReadyToCreate}
                className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default} disabled:opacity-60`}
                onClick={createImportSetting}
              >
                Create
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DbToDbImportManagement;
