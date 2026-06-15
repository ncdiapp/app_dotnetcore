/**
 * Simple Value List Entity Edit – migrated from AngularJS simpleValueListEntityEditCtrl / SimpleValueListEntityEdit.cshtml.
 * Used by "Create Simple List" from Entity List Of Value. Edit entity type SimpleValueList with list values grid.
 */
import React, { useCallback, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { ComboBox } from '@mescius/wijmo.react.input';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';
import { useDispatch } from 'react-redux';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { adminSvc } from '../../../webapi/adminsvc';
import appHelper from '../../../helper/appHelper';

const EmAppEntityType = { SimpleValueList: 4 };

interface RouteParam {
  id?: string | number | null;
  param1?: string | number | null;
  param2?: string | null;
}

interface ListValueItem {
  Id?: number;
  Sort?: number;
  InternalKey?: number;
  Code?: string;
  Description?: string;
}

interface CurrentEntity {
  Id?: number;
  EntityCode?: string;
  Description?: string;
  EntityType?: number;
  DataSourceFrom?: number | null;
  SaasApplicationId?: number | string | null;
  AppEntitySimpleListValueList?: ListValueItem[];
  IsModified?: boolean;
}

export interface SimpleValueListEntityEditProps {
  /** Optional: render inside DIV popup without changing URL. */
  paramOverride?: string;
}

const SimpleValueListEntityEdit: React.FC<SimpleValueListEntityEditProps> = ({ paramOverride }) => {
  const { param: routeParam } = useParams<{ param?: string }>();
  const param = paramOverride ?? routeParam;
  const { theme } = useTheme();
  const dispatch = useDispatch();
  const { showError, showValidationMessages } = useErrorMessage();
  const flexListRef = useRef<any>(null);

  const [paramObj, setParamObj] = useState<RouteParam>({});
  const [dataSourceList, setDataSourceList] = useState<any[]>([]);
  const [currentEntity, setCurrentEntity] = useState<CurrentEntity>({
    EntityCode: '',
    Description: '',
    EntityType: EmAppEntityType.SimpleValueList,
    DataSourceFrom: null,
    SaasApplicationId: null,
    AppEntitySimpleListValueList: [],
    IsModified: false
  });
  const [listValueCV, setListValueCV] = useState<CollectionView>(() => {
    const cv = new CollectionView<any>([]);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    return cv;
  });
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    let p: RouteParam = {};
    if (param) {
      try {
        p = JSON.parse(decodeURIComponent(param));
      } catch {
        p = { id: param };
      }
    }
    setParamObj(p);
  }, [param]);

  const entityInfoId = paramObj.id != null ? String(paramObj.id) : null;
  const dataSourceRegisterId = paramObj.param1 != null ? Number(paramObj.param1) : null;
  let applicationId: string | number | null = null;
  if (paramObj.param2) {
    try {
      const p2 = typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
      applicationId = p2?.applicationId ?? null;
    } catch {
      /* ignore */
    }
  }

  const loadData = useCallback(async () => {
    dispatch(setIsBusy());
    setIsLoading(true);
    try {
      const dsList = await adminSvc.getDataSourceRegisterList(false);
      const dsArr = Array.isArray(dsList) ? dsList : [];
      setDataSourceList(dsArr);

      const dataSourceId = dataSourceRegisterId ?? (dsArr[0]?.Id ?? null);

      if (entityInfoId) {
        const entityData = await adminSvc.retrieveOneAppEntityInfoExDto(entityInfoId, true);
        if (entityData) {
          const list = entityData.AppEntitySimpleListValueList ?? [];
          setCurrentEntity({
            ...entityData,
            DataSourceFrom: entityData.DataSourceFrom ?? dataSourceId,
            SaasApplicationId: entityData.SaasApplicationId ?? applicationId,
            AppEntitySimpleListValueList: list
          });
          const cv = new CollectionView<any>(list);
          cv.sortDescriptions.push(new SortDescription('Sort', true));
          setListValueCV(cv);
        }
      } else {
        setCurrentEntity((prev) => ({
          ...prev,
          EntityType: EmAppEntityType.SimpleValueList,
          DataSourceFrom: dataSourceId,
          SaasApplicationId: applicationId,
          AppEntitySimpleListValueList: [],
          IsModified: false
        }));
        const cv = new CollectionView<any>([]);
        cv.sortDescriptions.push(new SortDescription('Sort', true));
        setListValueCV(cv);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  }, [entityInfoId, dataSourceRegisterId, applicationId, dispatch, showError]);

  useEffect(() => {
    if (paramObj.param1 !== undefined || paramObj.id !== undefined) {
      loadData();
    }
  }, [loadData, paramObj.param1, paramObj.id]);

  const markChange = useCallback(() => {
    setCurrentEntity((prev) => (prev ? { ...prev, IsModified: true } : prev));
  }, []);

  const addListValueRow = useCallback(() => {
    const list = currentEntity.AppEntitySimpleListValueList ?? [];
    const maxSort = appHelper.findMaxValueFromArray(list, 'Sort', 0) + 1;
    const maxKey = appHelper.findMaxValueFromArray(list, 'InternalKey', 0) + 1;
    const newItem: ListValueItem = { Sort: maxSort, InternalKey: maxKey, Code: '', Description: '' };
    const newList = [...list, newItem];
    setCurrentEntity((prev) => (prev ? { ...prev, AppEntitySimpleListValueList: newList, IsModified: true } : prev));
    const cv = new CollectionView<any>(newList);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    setListValueCV(cv);
  }, [currentEntity.AppEntitySimpleListValueList]);

  const removeListValueRow = useCallback(() => {
    const flex = flexListRef.current?.control ?? flexListRef.current;
    const rowIndex = flex?.selection?.row ?? -1;
    if (rowIndex < 0 || !flex?.rows?.[rowIndex]) return;
    const dataItem = flex.rows[rowIndex].dataItem;
    const list = currentEntity.AppEntitySimpleListValueList ?? [];
    const idx = list.indexOf(dataItem);
    if (idx < 0) return;
    const newList = list.slice(0, idx).concat(list.slice(idx + 1));
    setCurrentEntity((prev) => (prev ? { ...prev, AppEntitySimpleListValueList: newList, IsModified: true } : prev));
    const cv = new CollectionView<any>(newList);
    cv.sortDescriptions.push(new SortDescription('Sort', true));
    setListValueCV(cv);
  }, [currentEntity.AppEntitySimpleListValueList]);

  const handleSave = useCallback(async () => {
    dispatch(setIsBusy());
    try {
      const payload = {
        ...currentEntity,
        AppEntitySimpleListValueList: currentEntity.AppEntitySimpleListValueList ?? []
      };
      const data = await adminSvc.saveOneAppEntityInfoDto(payload);
      if (data?.IsSuccessful && data?.Object) {
        setCurrentEntity((prev) => (prev ? { ...data.Object, AppEntitySimpleListValueList: data.Object?.AppEntitySimpleListValueList ?? prev.AppEntitySimpleListValueList } : prev));
        const list = data.Object?.AppEntitySimpleListValueList ?? [];
        const cv = new CollectionView<any>(list);
        cv.sortDescriptions.push(new SortDescription('Sort', true));
        setListValueCV(cv);
        if (data.Object?.Id != null) {
          setParamObj((p) => ({ ...p, id: data.Object.Id }));
        }
      }
      if (data?.ValidationResult) {
        showValidationMessages(data.ValidationResult, true);
      }
    } catch (e) {
      showError(e instanceof Error ? e.message : 'Save failed');
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [currentEntity, dispatch, showError, showValidationMessages]);

  const handleCellEditEnded = useCallback(() => {
    markChange();
  }, [markChange]);

  return (
    <div className="w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden">
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>Entity Info Edit</div>
        <div className="flex items-center gap-2">
          <button type="button" onClick={loadData} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-rotate" aria-hidden /> Refresh
          </button>
          <button type="button" onClick={handleSave} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
            <i className="fa-solid fa-floppy-disk" aria-hidden /> Save
          </button>
        </div>
      </div>
      <div className={`flex flex-col gap-2 px-3 py-2 min-h-0 h-1 flex-auto overflow-auto ${theme.mainContentSection}`}>
        <div className="flex flex-wrap items-center gap-4 flex-shrink-0">
          <div className="flex items-center gap-2">
            <label className={`w-32 text-xs ${theme.label}`}>Entity Code</label>
            <input
              type="text"
              value={currentEntity.EntityCode ?? ''}
              onChange={(e) => {
                setCurrentEntity((p) => (p ? { ...p, EntityCode: e.target.value, IsModified: true } : p));
              }}
              className={`flex-auto w-48 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-32 text-xs ${theme.label}`}>Description</label>
            <input
              type="text"
              value={currentEntity.Description ?? ''}
              onChange={(e) => {
                setCurrentEntity((p) => (p ? { ...p, Description: e.target.value, IsModified: true } : p));
              }}
              className={`flex-auto w-48 h-7 px-2 text-xs border ${theme.inputBox}`}
            />
          </div>
          <div className="flex items-center gap-2">
            <label className={`w-32 text-xs ${theme.label}`}>Entity Type</label>
            <input type="text" readOnly value="Simple Value List" className={`flex-auto w-40 h-7 px-2 text-xs border ${theme.inputBox}`} />
          </div>
          {dataSourceList.length > 0 && (
            <div className="flex items-center gap-2">
              <label className={`w-32 text-xs ${theme.label}`}>Datasource From</label>
              <ComboBox
                itemsSource={new CollectionView(dataSourceList)}
                displayMemberPath="DataSourceName"
                selectedValuePath="Id"
                selectedValue={currentEntity.DataSourceFrom}
                isDisabled={true}
                className={`flex-auto w-40 ${theme.inputBox}`}
              />
            </div>
          )}
        </div>
        <div className="flex flex-col gap-1 min-h-0 h-1 flex-auto">
          <div className="flex items-center justify-between flex-shrink-0">
            <span className={`text-sm font-medium ${theme.title}`}>List Values</span>
            <div className="flex gap-2">
              <button type="button" onClick={addListValueRow} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-plus" aria-hidden /> Add
              </button>
              <button type="button" onClick={removeListValueRow} className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}>
                <i className="fa-solid fa-minus" aria-hidden /> Remove
              </button>
            </div>
          </div>
          <div className={`w-full h-1 flex-auto overflow-hidden ${theme.mainContentSection}`}>
            <FlexGrid
              className="h-full h-full"
              ref={flexListRef}
              itemsSource={listValueCV}
              selectionMode="Cell"
              isReadOnly={false}
              allowDelete={false}
              cellEditEnded={handleCellEditEnded}
            >
              <FlexGridColumn binding="Sort" header="Sort" width={80} />
              <FlexGridColumn binding="InternalKey" header="Key" width={80} />
              <FlexGridColumn binding="Code" header="Code" width={300} />
              <FlexGridColumn binding="Description" header="Description" width={300} />
              <FlexGridColumn header="" binding="" width="*" isReadOnly />
            </FlexGrid>
          </div>
        </div>
      </div>
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-black/10">
          <div className="busyLoader w-12 h-12" />
        </div>
      )}
    </div>
  );
};

export default SimpleValueListEntityEdit;
