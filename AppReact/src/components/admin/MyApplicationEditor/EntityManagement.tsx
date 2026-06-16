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
import { useAlertConfirm } from '../../common/AlertConfirmProvider';
import { adminSvc } from '../../../webapi/adminsvc';
import { appTransactionService } from '../../../webapi/apptransactionsvc';

const EmAppEntityType = { SystemDefineTable: 1, Enum: 2, DataSet: 3, SimpleValueList: 4 };

const ENTITY_TYPE_LIST = [
  { Id: EmAppEntityType.SystemDefineTable, Display: 'System Define Table' },
  { Id: EmAppEntityType.Enum, Display: 'Enum' },
  { Id: EmAppEntityType.DataSet, Display: 'Data Set' },
  { Id: EmAppEntityType.SimpleValueList, Display: 'Simple Value List' }
];

interface EntityManagementProps {
  menuId: string | null;
}

interface EntityItem {
  Id: number;
  EntityCode?: string;
  Description?: string;
  EntityType?: number;
  DataSourceFrom?: number | null;
  TableName?: string;
  SchemaOwner?: string;
  IdentityField?: string;
  DisplayFiled1?: string;
  DisplayFiled2?: string;
  DisplayFiled3?: string;
  IsSystemDefine?: boolean;
  SaasApplicationId?: number | string | null;
}

const EntityManagement: React.FC<EntityManagementProps> = ({ menuId }) => {
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showWarning, showValidationMessages } = useErrorMessage();
  const { addTabAndNavigate } = useTabNavigation();
  const { showConfirm } = useAlertConfirm();

  const applicationId = menuId ?? null;
  const [entities, setEntities] = useState<EntityItem[]>([]);
  const [entitiesCV, setEntitiesCV] = useState<CollectionView>(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.push(new SortDescription('EntityCode', true));
    return cv;
  });
  const [entityTypeDataMap] = useState(() => new DataMap(ENTITY_TYPE_LIST, 'Id', 'Display'));
  const [dataSourceRegisterList, setDataSourceRegisterList] = useState<any[]>([]);
  const [datasourceRegisterDataMap, setDatasourceRegisterDataMap] = useState<DataMap | null>(null);
  const [isFilterByApp, setIsFilterByApp] = useState(Boolean(applicationId));
  const [contextMenu, setContextMenu] = useState<{ visible: boolean; x: number; y: number; item: EntityItem | null }>({
    visible: false,
    x: 0,
    y: 0,
    item: null
  });
  const [createFromDbDropdown, setCreateFromDbDropdown] = useState(false);
  const createFromDbRef = useRef<HTMLDivElement>(null);

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const [entityList, dsList] = await Promise.all([
        adminSvc.retrieveAllAppEntityInfoDto(true),
        adminSvc.getDataSourceRegisterList(false)
      ]);
      // API returns array of entity DTOs; fallback if wrapped (e.g. ObjectList) or non-array
      const rawList: EntityItem[] = Array.isArray(entityList)
        ? entityList
        : (entityList?.ObjectList && Array.isArray(entityList.ObjectList)
            ? entityList.ObjectList
            : entityList?.Object && Array.isArray(entityList.Object)
              ? entityList.Object
              : []);
      const useAppFilter = applicationId && isFilterByApp;
      const list = useAppFilter
        ? rawList.filter((e: EntityItem) => String(e.SaasApplicationId) === String(applicationId))
        : rawList;

      setEntities(list);
      const cv = new CollectionView<any>(list);
      cv.sortDescriptions.push(new SortDescription('EntityCode', true));
      setEntitiesCV(cv);

      const dsArr = Array.isArray(dsList)
        ? dsList
        : (dsList?.ObjectList && Array.isArray(dsList.ObjectList)
            ? dsList.ObjectList
            : dsList?.Object && Array.isArray(dsList.Object)
              ? dsList.Object
              : []);
      setDataSourceRegisterList(dsArr);
      setDatasourceRegisterDataMap(dsArr.length ? new DataMap(dsArr, 'Id', 'DataSourceName') : null);
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load entities');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [applicationId, isFilterByApp, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (contextMenu.visible) setContextMenu((c) => ({ ...c, visible: false, item: null }));
      if (createFromDbDropdown && createFromDbRef.current && !createFromDbRef.current.contains(e.target as Node)) {
        setCreateFromDbDropdown(false);
      }
    };
    document.addEventListener('click', handleClickOutside);
    return () => document.removeEventListener('click', handleClickOutside);
  }, [contextMenu.visible, createFromDbDropdown]);

  const isSystemBuildIn = (item: EntityItem | null) => item?.IsSystemDefine === true;
  const isAllowEditData = (item: EntityItem | null) => {
    if (!item || item.IsSystemDefine) return false;
    return item.EntityType === EmAppEntityType.SystemDefineTable && !!item.TableName;
  };

  const handleEditStructure = (item: EntityItem | null) => {
    setContextMenu((c) => ({ ...c, visible: false, item: null }));
    if (!item?.Id) return;
    const label = item.EntityCode || `Entity ${item.Id}`;
    const param2 = applicationId ? JSON.stringify({ applicationId }) : undefined;
    const params: any = { id: item.Id, param1: item.DataSourceFrom ?? '', param2 };
    if (item.EntityType === EmAppEntityType.SimpleValueList) {
      addTabAndNavigate('simple-value-list-entity-edit', label, params, true);
    } else {
      addTabAndNavigate('entity-info-edit', label, params, true);
    }
  };

  const handleEditData = async (item: EntityItem | null) => {
    setContextMenu((c) => ({ ...c, visible: false, item: null }));
    if (!item?.Id || !item.TableName) return;
    dispatch(setIsBusy());
    try {
      const lookup = await appTransactionService.retrieveListEditTransactionsBySchemaOwnerTableName(
        item.TableName,
        item.SchemaOwner || '',
        item.DataSourceFrom ?? null
      );
      const entries = lookup ? Object.entries(lookup) : [];
      let transactionId: number | null = null;
      let transactionName = '';

      if (entries.length > 0) {
        const [idStr, name] = entries[0];
        transactionId = parseInt(idStr, 10);
        transactionName = typeof name === 'string' ? name : String(name);
      } else {
        const result = await appTransactionService.createDefaultListTransactionFromTableName(
          item.TableName,
          item.DataSourceFrom ?? null,
          item.SchemaOwner || '',
          item.SaasApplicationId ?? null
        );
        if (result?.IsSuccessful && result?.Object) {
          transactionId = result.Object.Id;
          transactionName = result.Object.TransactionName || result.Object.Name || '';
        }
        if (result?.ValidationResult && !result.ValidationResult.IsValid) {
          showValidationMessages(result.ValidationResult, true);
          return;
        }
      }
      if (transactionId != null) {
        addTabAndNavigate('form-list-edit', transactionName || `List ${transactionId}`, { id: transactionId }, true);
      } else {
        showWarning('No list-edit transaction available for this entity.');
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to open list data');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleDelete = async (item: EntityItem | null) => {
    setContextMenu((c) => ({ ...c, visible: false, item: null }));
    if (!item?.Id) return;
    if (isSystemBuildIn(item)) {
      showWarning('System defined entity does not allow delete.');
      return;
    }
    const ok = await showConfirm('Confirm To Delete', { title: 'Confirm' });
    if (!ok) return;
    dispatch(setIsBusy());
    try {
      const data = await adminSvc.deleteOneAppEntityInfo(String(item.Id));
      if (data?.IsValid) {
        await loadData();
      } else if (data?.ValidationResult) {
        showValidationMessages(data.ValidationResult, true);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Delete failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleCreateEntity = (dataSourceRegisterId: number, entityType: number) => {
    setCreateFromDbDropdown(false);
    const label = 'New Entity';
    const param2 = applicationId ? JSON.stringify({ applicationId }) : undefined;
    const params: any = { param1: dataSourceRegisterId, param2 };
    if (entityType === EmAppEntityType.SimpleValueList) {
      addTabAndNavigate('simple-value-list-entity-edit', label, params, true);
    } else {
      addTabAndNavigate('entity-info-edit', label, params, true);
    }
  };

  const dataSourceDisplay = (ds: any) => (ds?.Id === 2147483647 ? 'Master DB' : (ds?.DataSourceName || ds?.Id));

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>List of Value Management:</div>
        <div className="flex items-center gap-2 flex-wrap">
          {applicationId && (
            <button
              type="button"
              onClick={() => setIsFilterByApp((prev) => !prev)}
              className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
              title={isFilterByApp ? 'Click to show all list of values' : 'Click to filter by current application'}
            >
              {isFilterByApp ? 'Show All' : 'Showing All'}
              <i className={`fa-solid fa-toggle-${isFilterByApp ? 'off' : 'on'} ml-1`} aria-hidden />
            </button>
          )}
          <button
            type="button"
            onClick={loadData}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-blue-400 hover:bg-blue-500"
            title="Refresh"
          >
            <i className="fa-solid fa-rotate" aria-hidden />
            Refresh
          </button>
          <button
            type="button"
            onClick={() => {
              const firstDs = dataSourceRegisterList[0];
              if (firstDs) handleCreateEntity(firstDs.Id, EmAppEntityType.SimpleValueList);
              else showWarning('No data source available.');
            }}
            className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
          >
            <i className="fa-solid fa-plus" aria-hidden />
            Create Simple List
          </button>
          {dataSourceRegisterList.length <= 1 ? (
            <button
              type="button"
              onClick={() => {
                const firstDs = dataSourceRegisterList[0];
                if (firstDs) handleCreateEntity(firstDs.Id, EmAppEntityType.SystemDefineTable);
                else showWarning('No data source available.');
              }}
              className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
            >
              <i className="fa-solid fa-plus" aria-hidden />
              Create From Database
            </button>
          ) : (
            <div className="relative" ref={createFromDbRef}>
              <button
                type="button"
                onClick={() => setCreateFromDbDropdown(!createFromDbDropdown)}
                className="h-6 px-2 inline-flex items-center justify-center gap-1 rounded-[4px] text-xs text-white transition disabled:cursor-not-allowed disabled:opacity-60 bg-green-500 hover:bg-green-600"
              >
                <i className="fa-solid fa-plus" aria-hidden />
                Create From Database <i className="fa-solid fa-caret-down text-xs" aria-hidden />
              </button>
              {createFromDbDropdown && (
                <div
                  className={`absolute right-0 mt-1 min-w-[200px] rounded shadow-lg z-50 border py-1 ${theme.mainContentSection}`}
                  onClick={(e) => e.stopPropagation()}
                >
                  {dataSourceRegisterList.map((ds: any) => (
                    <button
                      key={ds.Id}
                      type="button"
                      onClick={() => handleCreateEntity(ds.Id, EmAppEntityType.SystemDefineTable)}
                      className={`w-full px-3 py-2 text-left text-sm ${theme.contextMenu}`}
                    >
                      On {dataSourceDisplay(ds)} ({ds.Id})
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      <div className={`w-full h-[200px] flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <FlexGrid
          itemsSource={entitiesCV}
          selectionMode="Row"
          isReadOnly
          allowDelete={false}
          showGroups={false}
          className="w-full h-full"
        >
          <FlexGridFilter />
          <FlexGridColumn width={60} header="Actions" isReadOnly>
            <FlexGridCellTemplate
              cellType="Cell"
              template={(cell: any) => (
                <div className="flex items-center justify-center w-full">
                  <button
                    type="button"
                    className={`${theme.menu_default} w-8 h-6 flex items-center justify-center`}
                    title="More Options"
                    onClick={(e) => {
                      e.stopPropagation();
                      const rect = e.currentTarget.getBoundingClientRect();
                      setContextMenu({ visible: true, x: rect.right, y: rect.top, item: cell.item });
                    }}
                  >
                    <i className="fa-solid fa-pencil text-xs" aria-hidden />
                    <i className="fa-solid fa-bars text-[9px] relative -left-1 top-0.5" aria-hidden />
                  </button>
                </div>
              )}
            />
          </FlexGridColumn>
          <FlexGridColumn binding="EntityCode" header="Entity Code" width={300} />
          <FlexGridColumn binding="Description" header="Description" width={150} />
          <FlexGridColumn binding="EntityType" header="Entity Type" width={150} dataMap={entityTypeDataMap} />
          {datasourceRegisterDataMap && (
            <FlexGridColumn binding="DataSourceFrom" header="Data Source From" width={200} dataMap={datasourceRegisterDataMap} />
          )}
          <FlexGridColumn binding="TableName" header="Table Name" width={250} />
          <FlexGridColumn binding="IdentityField" header="Identity Field" width={200} />
          <FlexGridColumn binding="DisplayFiled1" header="Display Filed 1" width={150} />
          <FlexGridColumn binding="DisplayFiled2" header="Display Filed 2" width={150} />
          <FlexGridColumn binding="DisplayFiled3" header="Display Filed 3" width={150} />
        </FlexGrid>
      </div>

      {contextMenu.visible && (
        <div
          className={`fixed z-50 border rounded-[4px] shadow-lg py-1 min-w-max ${theme.mainContentSection}`}
          style={{ left: contextMenu.x, top: contextMenu.y }}
          onClick={(e) => e.stopPropagation()}
        >
          {!isSystemBuildIn(contextMenu.item) && (
            <button
              type="button"
              onClick={() => handleEditStructure(contextMenu.item)}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
            >
              <i className="fa-solid fa-pen-to-square mr-2 flex-shrink-0" aria-hidden />
              Edit
            </button>
          )}
          {isAllowEditData(contextMenu.item) && (
            <button
              type="button"
              onClick={() => handleEditData(contextMenu.item)}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
            >
              <i className="fa-solid fa-database mr-2 flex-shrink-0" aria-hidden />
              View Data
            </button>
          )}
          {!isSystemBuildIn(contextMenu.item) && (
            <button
              type="button"
              onClick={() => handleDelete(contextMenu.item)}
              className={`w-full text-left px-4 py-2 text-xs ${theme.contextMenu} flex items-center gap-2 whitespace-nowrap`}
            >
              <i className="fa-solid fa-trash mr-2 flex-shrink-0" aria-hidden />
              Delete
            </button>
          )}
        </div>
      )}
    </div>
  );
};

export default EntityManagement;
