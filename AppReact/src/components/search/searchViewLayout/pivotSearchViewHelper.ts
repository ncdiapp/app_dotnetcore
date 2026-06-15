import { CollectionView } from '@mescius/wijmo';
import { PivotEngine, ShowTotals } from '@mescius/wijmo.olap';
import { buildEndpointUrl } from '../../../webapi/endpoints';

/** Matches Angular `EmAppControlType` values used in search pivot (searchViewHelper.js). */
const EmAppControlType = {
  DDL: 1,
  TextBox: 2,
  Memo: 4,
  Image: 5,
  CheckBox: 13,
  Numeric: 20,
  Date: 7,
  DateTimeDetail: 27,
} as const;

export type PivotAggregateLookupItem = { Id: number; Display: string };

function setupDictLookup(
  dictEntityLookupItemDto: Record<string, any[]> | undefined | null
): Record<string, Record<string, string>> {
  const map: Record<string, Record<string, string>> = {};
  if (!dictEntityLookupItemDto) return map;
  Object.keys(dictEntityLookupItemDto).forEach((key) => {
    const valueList = dictEntityLookupItemDto[key];
    const dictLookupIdDisplay: Record<string, string> = {};
    (valueList || []).forEach((item: any) => {
      if (item && item.Id != null) {
        dictLookupIdDisplay[String(item.Id)] = item.Display ?? '';
      }
    });
    map[String(key)] = dictLookupIdDisplay;
  });
  return map;
}

function findFirstOrDefaultById<T extends { Id?: any; Display?: string }>(
  arr: T[] | undefined | null,
  id: string
): T | null {
  if (!arr?.length) return null;
  for (let i = 0; i < arr.length; i++) {
    if (String(arr[i]?.Id) === id) return arr[i];
  }
  return null;
}

/**
 * Port of `preparePivotData` (searchViewHelper.js): raw rows → CollectionView for PivotEngine.
 * First row is a header name array; following rows are flat objects keyed by `DictViewColumnIDKeyValue.{columnId}`.
 */
export function preparePivotSearchData(
  data: any[] | undefined | null,
  currentViewDto: any
): CollectionView<any> | undefined {
  const columns = currentViewDto?.Columns;
  if (!Array.isArray(columns) || columns.length === 0) {
    return undefined;
  }
  if (!data?.length) {
    return new CollectionView<any>([]);
  }

  const pivotData: any[] = [];
  const firstColumnHeaders: string[] = [];
  const dictIdColumn: Record<string, any> = {};
  columns.forEach((column: any) => {
    if (column?.Id != null) dictIdColumn[String(column.Id)] = column;
  });

  const firstRow = data[0]?.DictViewColumnIDKeyValue;
  if (!firstRow || typeof firstRow !== 'object') {
    return new CollectionView<any>([]);
  }

  const allColumnIds = Object.keys(firstRow);
  allColumnIds.forEach((columnId) => {
    const column = dictIdColumn[columnId];
    if (column) {
      firstColumnHeaders.push(column.Name);
    }
  });
  pivotData.push(firstColumnHeaders);

  const dictEntityIdLookup = setupDictLookup(currentViewDto.DictEntityLookupItemDto);

  data.forEach((row: any) => {
    const rawData: Record<string, any> = {};
    const dictVals = row?.DictViewColumnIDKeyValue;
    if (!dictVals || typeof dictVals !== 'object') return;

    Object.keys(dictVals).forEach((id) => {
      const value = dictVals[id];
      const column = dictIdColumn[id];
      if (!column) return;

      const binding = `DictViewColumnIDKeyValue.${column.Id}`;
      const ct = column.ControlType;

      if (ct === EmAppControlType.Image) {
        const imageUrl = buildEndpointUrl(`/GetThumbnailImage.aspx?FileId=${value ?? ''}`);
        const noImageUrl = buildEndpointUrl('/Images/noImage.jpeg');
        const imgValue = value
          ? `<img id="pivot-view-image" name="${String(value)}" src="${imageUrl}"/>`
          : `<img id="pivot-view-image" name="" src="${noImageUrl}"/>`;
        rawData[binding] = imgValue;
      } else if (ct === EmAppControlType.DDL) {
        let display = '';
        if (value && column.EntityId != null) {
          const lookItemObject = dictEntityIdLookup[String(column.EntityId)];
          if (lookItemObject) {
            display = lookItemObject[String(value)] ?? '';
          }
        }
        rawData[binding] = display;
      } else if (ct === EmAppControlType.Date) {
        let display = '';
        if (value) {
          display = new Date(value).toLocaleDateString();
        }
        rawData[binding] = display;
      } else if (ct === EmAppControlType.Numeric) {
        rawData[binding] = value ? value : 0;
      } else {
        rawData[binding] = value != null && value !== '' ? value : '';
      }
    });
    pivotData.push(rawData);
  });

  return new CollectionView<any>(pivotData);
}

/**
 * Port of `preparedataBindingFields` (searchViewHelper.js).
 */
export function preparePivotDataBindingFields(
  columns: any[] | undefined,
  currentViewDto: any,
  pivotAggregateFunctionList: PivotAggregateLookupItem[]
): any[] {
  const dataBindingFields: any[] = [];
  if (!Array.isArray(columns)) return dataBindingFields;

  columns.forEach((column: any) => {
    const bind: any = {
      binding: `DictViewColumnIDKeyValue.${column.Id}`,
      header: column.Name,
    };

    const ct = column.ControlType;
    if (ct === EmAppControlType.Image) {
      bind.isContentHtml = true;
    } else if (ct === EmAppControlType.Numeric) {
      bind.format = column.Nbdecimal ? `n${column.Nbdecimal}` : 'n0';
      bind.dataType = 'Number';
    } else if (ct === EmAppControlType.Date) {
      bind.format = 'd';
      bind.dataType = 'Date';
    } else if (ct === EmAppControlType.DateTimeDetail) {
      bind.format = 'G';
      bind.dataType = 'Date';
    } else if (ct === EmAppControlType.CheckBox) {
      bind.dataType = 'Boolean';
    }

    const pivotAggFields = currentViewDto?.PivotAggregationFields;
    if (Array.isArray(pivotAggFields)) {
      const aggField = findFirstOrDefaultById(pivotAggFields, String(column.Id));
      if (aggField) {
        const aggregationTypeId = aggField.AggregationFunctionType ?? currentViewDto.PivotDefaultAggregation;
        if (aggregationTypeId != null) {
          const lookupItem = findFirstOrDefaultById(pivotAggregateFunctionList, String(aggregationTypeId));
          if (lookupItem?.Display) {
            bind.aggregate = lookupItem.Display;
          }
        }
      }
    }

    dataBindingFields.push(bind);
  });

  return dataBindingFields;
}

function pivotFieldNames(list: any[] | undefined | null): string[] {
  if (!Array.isArray(list)) return [];
  return list.map((c: any) => c?.Name).filter((n: any) => n != null && String(n).length > 0);
}

/**
 * Builds a PivotEngine matching Angular `displayPivotViewWijmo` / `searchViewHelper.js`.
 */
export function buildSearchPivotEngine(
  data: any[] | undefined | null,
  currentViewDto: any,
  pivotAggregateFunctionList: PivotAggregateLookupItem[]
): PivotEngine | null {
  if (!currentViewDto) return null;

  const itemsSource = preparePivotSearchData(data, currentViewDto);
  if (!itemsSource) return null;

  const fields = preparePivotDataBindingFields(currentViewDto.Columns, currentViewDto, pivotAggregateFunctionList);
  const rowFields = pivotFieldNames(currentViewDto.PivotRows);
  const columnFields = pivotFieldNames(currentViewDto.PivotColumns);
  const valueFields = pivotFieldNames(currentViewDto.PivotAggregationFields);

  return new PivotEngine({
    autoGenerateFields: false,
    itemsSource,
    showColumnTotals: ShowTotals.None,
    showRowTotals: ShowTotals.None,
    fields,
    rowFields,
    columnFields,
    valueFields,
    filterFields: [],
  });
}
