/**
 * Excel table import setting editor – AppDataSetExDto (UsageType ExcelTableImportSetting).
 * Angular parity: top form (Import name, description, original file, Create Import API),
 * toolbar (Refresh, Save Setting, Simulate Import Data, Release Import), two-pane grids
 * (available source columns ↔ import-to table columns; left excludes names already on the right),
 * source/target checkbox multi-select & select-all,
 * drag selected (or single) source columns to the right to append rows, preview staging/target data.
 */

import React, { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import * as wjGrid from '@mescius/wijmo.grid';
import { CollectionView, DataType } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { searchSvc } from '../../webapi/searchSvc';
import { adminSvc } from '../../webapi/adminsvc';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import TableDataPreview from '../transaction/TableDataPreview';

/** EmAppDataSetUsageType.ExcelTableImportSetting */
const USAGE_EXCEL_TABLE_IMPORT = 4;
const EM_IMPORT_STATUS_DRAFT = 1;
const EM_IMPORT_STATUS_RELEASED = 2;

/** Resizable split: source columns (left) vs import table (right). */
const SPLIT_LEFT_DEFAULT_PX = 400;
const SPLIT_LEFT_MIN_PX = 200;
const SPLIT_RIGHT_MIN_PX = 280;

const IMPORT_LOGICAL_TYPES = [
  { id: 'String', name: 'String' },
  { id: 'Integer', name: 'Integer' },
  { id: 'Decimal', name: 'Decimal' },
  { id: 'DateTime', name: 'DateTime' },
  { id: 'Boolean', name: 'Boolean' },
];

const TAG_TO_DB: Record<string, string> = {
  String: 'nvarchar',
  Integer: 'int',
  Decimal: 'decimal',
  DateTime: 'datetime2',
  Boolean: 'bit',
};

function getLevelOneTable(tip: any): { table: any; index: number } | null {
  if (!tip?.Tables || !Array.isArray(tip.Tables) || tip.Tables.length === 0) return null;
  const idx = tip.Tables.findIndex((t: any) => String(t?.Tag) === '1' || t?.Tag === 1);
  if (idx >= 0) return { table: tip.Tables[idx], index: idx };
  return { table: tip.Tables[0], index: 0 };
}

function syncColumnMappingForTable(tip: any, tableName: string, columns: any[] | null | undefined): void {
  if (!tableName || !tip) return;
  const inner: Record<string, string> = {};
  for (const col of columns || []) {
    if (!col?.IsPrimaryKey && !col?.IsForeignKey && col?.Name) {
      inner[col.Name] = col.Name;
    }
  }
  if (!tip.DictTableNameColumnNameAndSourceColumnNameMapping) {
    tip.DictTableNameColumnNameAndSourceColumnNameMapping = {};
  }
  tip.DictTableNameColumnNameAndSourceColumnNameMapping[tableName] = inner;
}

function applyDbTypeForTag(col: any): void {
  const tag = col?.Tag != null ? String(col.Tag) : 'String';
  col.DbDataType = TAG_TO_DB[tag] ?? col.DbDataType ?? 'nvarchar';
}

function columnFromSourceTemplate(src: any, targetTableName: string): any {
  const tag = src?.Tag != null ? String(src.Tag) : src?.DataType != null ? String(src.DataType) : 'String';
  const normalizedTag = IMPORT_LOGICAL_TYPES.some((t) => t.id === tag) ? tag : 'String';
  const col: any = {
    Name: src?.Name,
    TableName: targetTableName,
    Tag: normalizedTag,
    DbDataType: TAG_TO_DB[normalizedTag] ?? 'nvarchar',
    Nullable: src?.Nullable !== false,
    Length: src?.Length,
    Precision: src?.Precision,
    Scale: src?.Scale,
    IsPrimaryKey: false,
    IsForeignKey: false,
    IsLogicKey: false,
    IsAutoNumber: !!src?.IsAutoNumber,
    SchemaOwner: src?.SchemaOwner ?? '',
    NetName: src?.NetName,
    DefaultValue: src?.DefaultValue,
  };
  return col;
}

function ensureSystemPrimaryKeyColumn(tableDto: any): void {
  if (!tableDto) return;
  if (!Array.isArray(tableDto.Columns)) tableDto.Columns = [];
  const hasPk = tableDto.Columns.some((c: any) => c?.IsPrimaryKey);
  if (hasPk) return;

  const baseName = String(tableDto.Name || 'ImportTable').replace(/[^0-9a-zA-Z_]/g, '');
  const pkName = `${baseName || 'ImportTable'}Id`;

  tableDto.Columns.unshift({
    Name: pkName,
    Tag: 'Integer',
    DbDataType: 'int',
    Nullable: false,
    Length: null,
    Precision: null,
    Scale: null,
    TableName: tableDto.Name || '',
    IsPrimaryKey: true,
    IsForeignKey: false,
    IsLogicKey: false,
    IsAutoNumber: true,
    IsIdentity: true,
    SchemaOwner: tableDto.SchemaOwner ?? null,
  });
}

function ensureImportSettingPrimaryKeys(settingDto: any): void {
  if (!settingDto?.Tables || !Array.isArray(settingDto.Tables)) return;
  settingDto.Tables.forEach((t: any) => ensureSystemPrimaryKeyColumn(t));
}

function ensureDatabaseDiagramInfo(dataSetDto: any): void {
  if (!dataSetDto) return;
  if (!dataSetDto.OtherSettingsDto) dataSetDto.OtherSettingsDto = {};
  if (!dataSetDto.OtherSettingsDto.DatabaseDiagramInfo || typeof dataSetDto.OtherSettingsDto.DatabaseDiagramInfo !== 'object') {
    dataSetDto.OtherSettingsDto.DatabaseDiagramInfo = {};
  }
  const ddi = dataSetDto.OtherSettingsDto.DatabaseDiagramInfo;
  if (ddi.QueryString == null) ddi.QueryString = '';
  if (!ddi.DictTables) ddi.DictTables = {};
  if (!ddi.DictAllColumns) ddi.DictAllColumns = {};
  if (!Array.isArray(ddi.SelectedColumnsList)) ddi.SelectedColumnsList = [];
  if (!Array.isArray(ddi.Joins)) ddi.Joins = [];
  if (!Array.isArray(ddi.WhereConditionFilterColumns)) ddi.WhereConditionFilterColumns = [];
  if (ddi.DataSourceRegisterId == null) ddi.DataSourceRegisterId = dataSetDto.DataSourceFrom ?? null;
  if (ddi.IsErDiagram == null) ddi.IsErDiagram = true;
}

function ensureImportPayloadShape(dataSetDto: any): void {
  if (!dataSetDto) return;
  if (!dataSetDto.OtherSettingsDto) dataSetDto.OtherSettingsDto = {};
  ensureDatabaseDiagramInfo(dataSetDto);
  ensureImportSettingPrimaryKeys(dataSetDto?.OtherSettingsDto?.TableImportSettingDto);
}

function getValidationMessages(result: any, errorsOnly = false): string[] {
  const items = Array.isArray(result?.ValidationResult?.Items) ? result.ValidationResult.Items : [];
  const msgs = items
    .filter((i: any) => (errorsOnly ? i?.ItemType === 1 || !!i?.ErrorMessage : true))
    .map((i: any) => i?.ErrorMessage || i?.LocalizedMessage || i?.Message)
    .filter((m: any) => !!m);
  return msgs;
}

function hasValidationErrors(result: any): boolean {
  return getValidationMessages(result, true).length > 0 || !!result?.ValidationResult?.ErrorMessage;
}

function extractProcessError(result: any): string {
  const errorMsgs = getValidationMessages(result, true);
  if (errorMsgs.length) return errorMsgs.join('\n');
  const allMsgs = getValidationMessages(result, false);
  if (allMsgs.length) return allMsgs.join('\n');
  return result?.ValidationResult?.LocalizedResult || result?.ValidationResult?.ErrorMessage || 'Operation failed.';
}

export interface ExcelTableImportEditorProps {
  ignoreRouteParam?: boolean;
  dataSetId?: number | null;
  onClose?: () => void;
}

const ExcelTableImportEditor: React.FC<ExcelTableImportEditorProps> = (props) => {
  const { ignoreRouteParam = false, dataSetId: dataSetIdProp = null } = props;
  const { theme } = useTheme();
  const { showError, showInfo, showWarning } = useErrorMessage();
  const { showConfirm } = useAlertConfirm();
  const dispatch = useDispatch();
  const { param } = useParams<{ param?: string }>();
  const [, forceRender] = useReducer((x) => x + 1, 0);

  const sourceSelectAllRef = useRef<HTMLInputElement>(null);
  const targetSelectAllRef = useRef<HTMLInputElement>(null);

  /** UI-only multi-select for source file columns (Angular parity). */
  const [sourceSelected, setSourceSelected] = useState<Set<string>>(() => new Set());
  /** UI-only multi-select for import-to table rows (Angular parity). */
  const [targetSelected, setTargetSelected] = useState<Set<string>>(() => new Set());

  const routeDataSetId: number | null = useMemo(() => {
    if (ignoreRouteParam || !param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      const idValue = obj.id ?? obj.Id ?? null;
      if (idValue == null) return null;
      const n = Number(idValue);
      return Number.isNaN(n) ? null : n;
    } catch {
      const n = Number(param);
      return Number.isNaN(n) ? null : n;
    }
  }, [param, ignoreRouteParam]);

  const dataSetId = dataSetIdProp ?? routeDataSetId;

  const [dto, setDto] = useState<any | null>(null);
  const [dataSources, setDataSources] = useState<any[]>([]);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [previewSourceOpen, setPreviewSourceOpen] = useState(false);
  const [previewTargetOpen, setPreviewTargetOpen] = useState(false);
  const [leftPanelWidthPx, setLeftPanelWidthPx] = useState(SPLIT_LEFT_DEFAULT_PX);
  const splitContainerRef = useRef<HTMLDivElement | null>(null);
  const splitDragRef = useRef(false);

  const typeDataMap = useMemo(() => new DataMap(IMPORT_LOGICAL_TYPES, 'id', 'name'), []);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!splitDragRef.current || !splitContainerRef.current) return;
      const rect = splitContainerRef.current.getBoundingClientRect();
      const w = e.clientX - rect.left;
      const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, rect.width - SPLIT_RIGHT_MIN_PX);
      const clamped = Math.min(Math.max(w, SPLIT_LEFT_MIN_PX), maxLeft);
      setLeftPanelWidthPx(clamped);
    };
    const onUp = () => {
      if (!splitDragRef.current) return;
      splitDragRef.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, []);

  const onSplitDividerMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    splitDragRef.current = true;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }, []);

  const loadData = useCallback(async () => {
    if (dataSetId == null) {
      setLoadError('Missing import setting id. Open this page from Excel File Import list.');
      setDto(null);
      return;
    }
    setLoadError(null);
    dispatch(setIsBusy());
    try {
      const [data, dsList] = await Promise.all([
        searchSvc.retrieveOneAppDataSetExDto(String(dataSetId), false),
        adminSvc.getDataSourceRegisterList(false),
      ]);
      setDataSources(Array.isArray(dsList) ? dsList : []);
      if (data && typeof data === 'object') {
        const usage = Number(data.UsageTypeId);
        if (usage !== USAGE_EXCEL_TABLE_IMPORT) {
          setLoadError(`This dataset is not an Excel table import setting (UsageTypeId=${data.UsageTypeId ?? 'n/a'}).`);
          setDto(null);
          return;
        }
        const cloned = JSON.parse(JSON.stringify(data));
        ensureImportPayloadShape(cloned);
        setDto(cloned);
      } else {
        setLoadError('Import setting not found.');
        setDto(null);
      }
    } catch (e: any) {
      showError(e?.message || 'Failed to load import setting');
      setDto(null);
    } finally {
      dispatch(setIsNotBusy());
    }
  }, [dataSetId, dispatch, showError]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const tip = dto?.OtherSettingsDto?.TableImportSettingDto;
  const levelOne = tip ? getLevelOneTable(tip) : null;
  const levelOneTable = levelOne?.table;
  const statusNum = tip?.Status != null ? Number(tip.Status) : null;
  const isDraft = statusNum === EM_IMPORT_STATUS_DRAFT;
  const isReleased = statusNum === EM_IMPORT_STATUS_RELEASED;

  const targetCV = useMemo(
    () =>
      new CollectionView<any>(
        Array.isArray(levelOneTable?.Columns) ? levelOneTable.Columns.filter((c: any) => !c?.IsPrimaryKey) : [],
      ),
    [dto, levelOneTable?.Columns],
  );

  const targetColumnNames = useMemo(
    () =>
      (Array.isArray(levelOneTable?.Columns)
        ? levelOneTable.Columns.filter((c: any) => !c?.IsPrimaryKey).map((c: any) => c.Name).filter(Boolean)
        : []) as string[],
    [levelOneTable?.Columns],
  );

  const targetColumnNameSet = useMemo(() => new Set(targetColumnNames), [targetColumnNames]);

  /** Left grid: source columns not yet on the import table (Angular-style available list). */
  const availableSourceColumns = useMemo(() => {
    const src = tip?.SourceColumns;
    if (!Array.isArray(src)) return [];
    return src.filter((c: any) => c?.Name && !targetColumnNameSet.has(String(c.Name)));
  }, [tip?.SourceColumns, targetColumnNameSet]);

  const sourceCV = useMemo(
    () => new CollectionView<any>(availableSourceColumns),
    [availableSourceColumns],
  );

  const sourceColumnNames = useMemo(
    () => availableSourceColumns.map((c: any) => c.Name).filter(Boolean) as string[],
    [availableSourceColumns],
  );

  useEffect(() => {
    const valid = new Set(sourceColumnNames);
    setSourceSelected((prev) => {
      const next = new Set<string>();
      let changed = false;
      prev.forEach((n) => {
        if (valid.has(n)) next.add(n);
        else changed = true;
      });
      if (!changed && next.size === prev.size) return prev;
      return next;
    });
  }, [sourceColumnNames]);

  useEffect(() => {
    const valid = new Set(targetColumnNames);
    setTargetSelected((prev) => {
      const next = new Set<string>();
      let changed = false;
      prev.forEach((n) => {
        if (valid.has(n)) next.add(n);
        else changed = true;
      });
      if (!changed && next.size === prev.size) return prev;
      return next;
    });
  }, [targetColumnNames]);

  const sourceSelectAllState = useMemo(() => {
    const total = sourceColumnNames.length;
    const count = sourceColumnNames.filter((n) => sourceSelected.has(n)).length;
    return { total, count, all: total > 0 && count === total, some: count > 0 && count < total };
  }, [sourceColumnNames, sourceSelected]);

  const targetSelectAllState = useMemo(() => {
    const total = targetColumnNames.length;
    const count = targetColumnNames.filter((n) => targetSelected.has(n)).length;
    return { total, count, all: total > 0 && count === total, some: count > 0 && count < total };
  }, [targetColumnNames, targetSelected]);

  useEffect(() => {
    const el = sourceSelectAllRef.current;
    if (el) el.indeterminate = sourceSelectAllState.some;
  }, [sourceSelectAllState.some]);

  useEffect(() => {
    const el = targetSelectAllRef.current;
    if (el) el.indeterminate = targetSelectAllState.some;
  }, [targetSelectAllState.some]);

  const stagingTableName = tip?.OrgTempTableName || tip?.TempTableName || '';
  const dataSourceId = dto?.DataSourceFrom != null ? Number(dto.DataSourceFrom) : null;
  const schemaOwnerPreview = levelOneTable?.SchemaOwner != null ? String(levelOneTable.SchemaOwner) : null;

  const patchTip = useCallback((recipe: (tipDraft: any, draft: any) => void) => {
    setDto((prev: any) => {
      if (!prev) return prev;
      const draft = JSON.parse(JSON.stringify(prev));
      const t = draft.OtherSettingsDto?.TableImportSettingDto;
      if (!t) return prev;
      recipe(t, draft);
      ensureImportPayloadShape(draft);
      return draft;
    });
  }, []);

  const handleFieldChange = (field: string, value: any) => {
    setDto((prev: any) => (prev ? { ...prev, [field]: value } : prev));
  };

  const handleTableNameChange = (name: string) => {
    patchTip((t) => {
      const lo = getLevelOneTable(t);
      if (lo?.table) {
        const oldName = lo.table.Name;
        lo.table.Name = name;
        if (
          oldName &&
          t.DictTableNameColumnNameAndSourceColumnNameMapping &&
          t.DictTableNameColumnNameAndSourceColumnNameMapping[oldName]
        ) {
          const m = t.DictTableNameColumnNameAndSourceColumnNameMapping[oldName];
          delete t.DictTableNameColumnNameAndSourceColumnNameMapping[oldName];
          t.DictTableNameColumnNameAndSourceColumnNameMapping[name] = m;
        }
        syncColumnMappingForTable(t, name, lo.table.Columns);
      }
    });
  };

  const handleTransformConditionChange = (value: string) => {
    patchTip((t) => {
      const lo = getLevelOneTable(t);
      if (lo?.table) lo.table.TransformCondition = value;
    });
  };

  const handleSave = async () => {
    if (!dto) return;
    if (!dto.Name?.trim()) {
      showWarning('Name is required.');
      return;
    }
    if (!dto.DataSourceFrom) {
      showWarning('Select a database (data source).');
      return;
    }
    dispatch(setIsBusy());
    try {
      const payload = JSON.parse(JSON.stringify(dto));
      ensureImportPayloadShape(payload);
      const result = await searchSvc.saveOneAppDataSetEntityDto(payload);
      if (result?.IsSuccessful && result?.Object) {
        showInfo('Import setting saved.');
        const obj = JSON.parse(JSON.stringify(result.Object));
        ensureImportPayloadShape(obj);
        setDto(obj);
      } else {
        showError(extractProcessError(result));
      }
    } catch (e: any) {
      showError(e?.message || 'Save failed.');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const saveThenGetDto = async (): Promise<any | null> => {
    if (!dto) return null;
    const payload = JSON.parse(JSON.stringify(dto));
    ensureImportPayloadShape(payload);
    const result = await searchSvc.saveOneAppDataSetEntityDto(payload);
    if (result?.IsSuccessful && result?.Object) {
      const obj = JSON.parse(JSON.stringify(result.Object));
      ensureImportPayloadShape(obj);
      setDto(obj);
      return obj;
    }
    showError(extractProcessError(result));
    return null;
  };

  const handleSimulateImport = async () => {
    if (!dto?.Id) return;
    dispatch(setIsBusy());
    try {
      const fresh = await saveThenGetDto();
      if (!fresh) return;
      if (fresh.OtherSettingsDto?.TableImportSettingDto) {
        fresh.OtherSettingsDto.TableImportSettingDto.Status = EM_IMPORT_STATUS_DRAFT;
      }
      ensureImportPayloadShape(fresh);
      const processResult = await schemaMetadataService.createTableImportSettingAndProcessImport(fresh);
      if (processResult?.IsSuccessful && processResult?.Object && !hasValidationErrors(processResult)) {
        const obj = JSON.parse(JSON.stringify(processResult.Object));
        ensureImportPayloadShape(obj);
        setDto(obj);
        const okMsg = getValidationMessages(processResult, false)[0] || 'Simulate import completed.';
        showInfo(okMsg);
        await loadData();
      } else {
        showError(extractProcessError(processResult));
      }
    } catch (e: any) {
      showError(e?.message || 'Simulate import failed.');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleReleaseImport = async () => {
    if (!dto?.Id) return;
    const ok = await showConfirm(
      'Release will run the full import and finalize this setting (Angular parity). Continue?',
      { title: 'Release Import', confirmLabel: 'Release', cancelLabel: 'Cancel' },
    );
    if (!ok) return;
    dispatch(setIsBusy());
    try {
      const fresh = await saveThenGetDto();
      if (!fresh) return;
      if (fresh.OtherSettingsDto?.TableImportSettingDto) {
        fresh.OtherSettingsDto.TableImportSettingDto.Status = EM_IMPORT_STATUS_RELEASED;
      }
      ensureImportPayloadShape(fresh);
      const processResult = await schemaMetadataService.createTableImportSettingAndProcessImport(fresh);
      if (processResult?.IsSuccessful && processResult?.Object && !hasValidationErrors(processResult)) {
        const obj = JSON.parse(JSON.stringify(processResult.Object));
        ensureImportPayloadShape(obj);
        setDto(obj);
        showInfo('Import released.');
        await loadData();
      } else {
        showError(extractProcessError(processResult));
      }
    } catch (e: any) {
      showError(e?.message || 'Release import failed.');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const toggleSourceRowSelected = useCallback(
    (name: string, checked: boolean) => {
      if (isReleased || !name) return;
      setSourceSelected((prev) => {
        const next = new Set(prev);
        if (checked) next.add(name);
        else next.delete(name);
        return next;
      });
    },
    [isReleased],
  );

  const toggleSourceSelectAll = useCallback(
    (checked: boolean) => {
      if (isReleased) return;
      setSourceSelected(() => (checked ? new Set(sourceColumnNames) : new Set()));
    },
    [isReleased, sourceColumnNames],
  );

  const toggleTargetRowSelected = useCallback(
    (name: string, checked: boolean) => {
      if (isReleased || !name) return;
      setTargetSelected((prev) => {
        const next = new Set(prev);
        if (checked) next.add(name);
        else next.delete(name);
        return next;
      });
    },
    [isReleased],
  );

  const toggleTargetSelectAll = useCallback(
    (checked: boolean) => {
      if (isReleased) return;
      setTargetSelected(() => (checked ? new Set(targetColumnNames) : new Set()));
    },
    [isReleased, targetColumnNames],
  );

  const addSourceColumnsToTarget = useCallback(
    (srcList: any[]) => {
      const list = (srcList || []).filter((s) => s?.Name);
      if (!list.length) return;
      const duplicates: string[] = [];
      let added = 0;

      setDto((prev: any) => {
        if (!prev) return prev;
        const draft = JSON.parse(JSON.stringify(prev));
        const t = draft.OtherSettingsDto?.TableImportSettingDto;
        if (!t) return prev;

        let lo = getLevelOneTable(t);
        if (!lo?.table) {
          if (!t.Tables) t.Tables = [];
          const nt = {
            Name: 'ImportTable',
            Tag: 1,
            SchemaOwner: '',
            IsNewTable: true,
            Columns: [] as any[],
            TransformCondition: '',
          };
          t.Tables.push(nt);
          lo = { table: nt, index: t.Tables.length - 1 };
        }

        const cols = lo.table.Columns || (lo.table.Columns = []);
        const existing = new Set(cols.map((c: any) => c.Name));
        const hasAnyLogic = cols.some((c: any) => c.IsLogicKey);
        let assignLogic = !hasAnyLogic;

        for (const src of list) {
          if (existing.has(src.Name)) {
            duplicates.push(src.Name);
            continue;
          }
          const newCol = columnFromSourceTemplate(src, lo.table.Name);
          if (assignLogic) {
            newCol.IsLogicKey = true;
            assignLogic = false;
          }
          cols.push(newCol);
          applyDbTypeForTag(newCol);
          existing.add(src.Name);
          added++;
        }

        ensureImportSettingPrimaryKeys(t);
        if (added === 0) return prev;
        syncColumnMappingForTable(t, lo.table.Name, cols);
        return draft;
      });

      if (duplicates.length) {
        showWarning(`Already on import table: ${duplicates.join(', ')}`);
      }
    },
    [showWarning],
  );

  const addSourceColumnToTarget = useCallback(
    (src: any) => addSourceColumnsToTarget([src]),
    [addSourceColumnsToTarget],
  );

  const removeSelectedTargetRows = useCallback(() => {
    if (!tip) return;
    const names = Array.from(targetSelected);
    if (names.length === 0) {
      showWarning('Select one or more rows in Import To Table to remove (checkboxes).');
      return;
    }
    patchTip((t) => {
      const lo = getLevelOneTable(t);
      if (!lo?.table?.Columns) return;
      const removeSet = new Set(names);
      lo.table.Columns = lo.table.Columns.filter((c: any) => !removeSet.has(c.Name));
      syncColumnMappingForTable(t, lo.table.Name, lo.table.Columns);
    });
    setTargetSelected(new Set());
    forceRender();
  }, [patchTip, showWarning, tip, targetSelected]);

  const dragNamesForSourceItem = useCallback(
    (item: any): string[] => {
      const name = item?.Name;
      if (!name) return [];
      if (sourceSelected.has(name)) {
        return sourceColumnNames.filter((n) => sourceSelected.has(n));
      }
      return [name];
    },
    [sourceColumnNames, sourceSelected],
  );

  const onTargetCellEditEnded = useCallback(
    (grid: wjGrid.FlexGrid, e: wjGrid.CellRangeEventArgs) => {
      const col = grid.columns[e.col];
      const binding = col?.binding as string;
      const rowItem = grid.rows[e.row]?.dataItem;
      if (!rowItem) return;
      if (binding === 'Tag') {
        applyDbTypeForTag(rowItem);
      }
      const lo = tip ? getLevelOneTable(tip) : null;
      if (lo?.table?.Name) {
        syncColumnMappingForTable(tip, lo.table.Name, lo.table.Columns);
      }
      forceRender();
    },
    [tip],
  );

  const onSourceDragEnd = useCallback(() => {
    document.body.style.cursor = '';
  }, []);

  const onSourceDragStart = (e: React.DragEvent, src: any) => {
    if (isReleased) {
      e.preventDefault();
      return;
    }
    const names = dragNamesForSourceItem(src);
    if (!names.length) {
      e.preventDefault();
      return;
    }
    e.dataTransfer.setData('application/json', JSON.stringify({ names }));
    e.dataTransfer.effectAllowed = 'copy';
    document.body.style.cursor = 'grabbing';
  };

  const onTargetDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = 'copy';
  };

  const onTargetDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (isReleased) return;
    let names: string[] = [];
    try {
      const raw = e.dataTransfer.getData('application/json');
      if (raw) {
        const o = JSON.parse(raw);
        if (Array.isArray(o.names)) names = o.names.filter(Boolean);
        else if (o.name) names = [o.name];
      }
    } catch {
      /* ignore */
    }
    if (!names.length || !tip?.SourceColumns) return;
    const byName = new Map((tip.SourceColumns as any[]).map((c: any) => [c.Name, c]));
    const srcs = names.map((n) => byName.get(n)).filter(Boolean) as any[];
    if (srcs.length) addSourceColumnsToTarget(srcs);
  };

  if (loadError) {
    return (
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
        <div className={`px-3 py-2 ${theme.mainContentSection}`}>
          <div className={`text-md font-semibold ${theme.title}`}>Excel File Import</div>
        </div>
        <div className={`w-full h-1 flex-auto flex items-center justify-center p-6 ${theme.mainContentSection}`}>
          <p className={`text-sm ${theme.label}`}>{loadError}</p>
        </div>
      </div>
    );
  }

  if (!dto) {
    return (
      <div className={`w-full h-full flex items-center justify-center ${theme.mainContentSection}`}>
        <span className={`text-sm ${theme.label}`}>Loading…</span>
      </div>
    );
  }

  const importFile = tip?.ImportFileName ?? '';

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      <div className={`flex items-center justify-between px-3 py-2 mb-1 shrink-0 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title} truncate min-w-0 pr-2`}>
          Import Setting: {dto.Name || 'Excel File Import'}
        </div>
        <div className="flex items-center gap-2 shrink-0 flex-wrap justify-end">
          <button
            type="button"
            onClick={() => loadData()}
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            title="Reload from server"
          >
            <i className="fa-solid fa-rotate mr-1" aria-hidden />
            Refresh
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={isReleased}
            className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
          >
            <i className="fa-solid fa-floppy-disk mr-1" aria-hidden />
            Save Setting
          </button>
          <button
            type="button"
            onClick={handleSimulateImport}
            disabled={!dto.Id || !isDraft}
            className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
            title="Runs import while status is Draft (simulated tables)"
          >
            <i className="fa-solid fa-play mr-1" aria-hidden />
            Simulate Import Data
          </button>
          <button
            type="button"
            onClick={handleReleaseImport}
            disabled={!dto.Id || !isDraft}
            className={`px-3 py-1.5 text-sm rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
            title="Finalize and run full import pipeline"
          >
            <i className="fa-solid fa-check mr-1" aria-hidden />
            Release Import
          </button>
        </div>
      </div>

      <div
        className={`w-full h-1 flex-auto flex flex-col min-h-0 overflow-hidden px-4 pb-4 ${theme.mainContentSection}`}
      >
        <div className="shrink-0 pt-4 pb-3">
          <div className="flex flex-wrap gap-x-8 gap-y-3">
            <div className="flex items-center py-1 min-w-0 flex-auto w-1 basis-[280px]">
              <label className={`w-32 shrink-0 text-xs ${theme.label} mr-2`}>Import Name</label>
              <input
                type="text"
                value={dto.Name ?? ''}
                onChange={(e) => handleFieldChange('Name', e.target.value)}
                disabled={isReleased}
                autoComplete="off"
                className={`min-w-0 flex-auto w-1 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none disabled:opacity-60`}
              />
            </div>
            <div className="flex items-center py-1 min-w-0 flex-auto w-1 basis-[280px]">
              <label className={`w-32 shrink-0 text-xs ${theme.label} mr-2`}>Description</label>
              <input
                type="text"
                value={dto.Description ?? ''}
                onChange={(e) => handleFieldChange('Description', e.target.value)}
                disabled={isReleased}
                autoComplete="off"
                className={`min-w-0 flex-auto w-1 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none disabled:opacity-60`}
              />
            </div>
            <div className="flex items-center py-1 min-w-0 flex-auto w-1 basis-[280px]">
              <label className={`w-32 shrink-0 text-xs ${theme.label} mr-2`}>Original File</label>
              <input
                type="text"
                readOnly
                value={importFile}
                className={`min-w-0 flex-auto w-1 h-7 px-2 text-xs border opacity-80 ${theme.inputBox}`}
              />
            </div>
            <div className="flex items-center py-1 gap-2">
              <input
                id="excel-import-create-api"
                type="checkbox"
                checked={!!tip?.IsNeedToCreateImportApi}
                disabled={isReleased}
                onChange={(e) =>
                  patchTip((t) => {
                    t.IsNeedToCreateImportApi = e.target.checked;
                  })
                }
                className="rounded border-gray-400"
              />
              <label htmlFor="excel-import-create-api" className={`text-xs ${theme.label}`}>
                Create Import API
              </label>
            </div>
            <div className="flex items-center py-1 min-w-0 flex-auto w-1 basis-[240px]">
              <label className={`w-32 shrink-0 text-xs ${theme.label} mr-2`}>Database</label>
              <select
                value={dto.DataSourceFrom ?? ''}
                onChange={(e) => handleFieldChange('DataSourceFrom', e.target.value ? Number(e.target.value) : null)}
                disabled={isReleased}
                className={`min-w-0 flex-auto w-1 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none disabled:opacity-60`}
              >
                <option value="">Select…</option>
                {dataSources.map((ds: any) => (
                  <option key={ds.Id} value={ds.Id}>
                    {ds.DataSourceName}
                  </option>
                ))}
              </select>
            </div>
            {isReleased ? (
              <div className={`text-xs ${theme.label}`}>
                <span className="font-semibold">Status: </span>Released
              </div>
            ) : isDraft ? (
              <div className={`text-xs ${theme.label}`}>
                <span className="font-semibold">Status: </span>Draft
              </div>
            ) : null}
          </div>
        </div>

        <div
          ref={splitContainerRef}
          className="flex h-1 min-h-0 w-full flex-auto flex-row"
          onDragOver={onTargetDragOver}
          onDrop={onTargetDrop}
        >
          <div
            className={`shrink-0 flex flex-col min-h-0 h-full border border-r-0 rounded-l-[4px] overflow-hidden ${theme.mainContentSection}`}
            style={{ width: leftPanelWidthPx, minWidth: SPLIT_LEFT_MIN_PX }}
          >
              <div className={`flex items-center justify-between px-2 py-1.5 border-b shrink-0 ${theme.mainContentSection}`}>
                <span className={`text-xs font-semibold ${theme.title}`}>
                  Available File Columns (Drag To Table)
                </span>
                <button
                  type="button"
                  onClick={() => setPreviewSourceOpen(true)}
                  disabled={!stagingTableName || !dataSourceId}
                  className={`text-xs px-2 py-1 rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                >
                  Preview Source Data
                </button>
              </div>
              <FlexGrid
                itemsSource={sourceCV}
                selectionMode="None"
                headersVisibility="Column"
                allowSorting={false}
                isReadOnly
                className="w-full h-1 flex-auto min-h-0"
                beginningEdit={(_s, e) => {
                  e.cancel = true;
                }}
              >
                <FlexGridColumn width={44} header="">
                  <FlexGridCellTemplate
                    cellType="ColumnHeader"
                    template={() => (
                      <div className="flex h-full w-full items-center justify-center">
                        <input
                          ref={sourceSelectAllRef}
                          type="checkbox"
                          checked={sourceSelectAllState.all}
                          onChange={(e) => toggleSourceSelectAll(e.target.checked)}
                          disabled={isReleased}
                          title="Select all source columns"
                          className="rounded border-gray-400"
                          onClick={(e) => e.stopPropagation()}
                        />
                      </div>
                    )}
                  />
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(ctx: any) => {
                      const name = ctx.item?.Name as string | undefined;
                      return (
                        <div className="flex h-full w-full items-center justify-center">
                          <input
                            type="checkbox"
                            checked={!!name && sourceSelected.has(name)}
                            onChange={(e) => name && toggleSourceRowSelected(name, e.target.checked)}
                            disabled={isReleased || !name}
                            className="rounded border-gray-400"
                            onClick={(e) => e.stopPropagation()}
                          />
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="Name" header="Column Name" width="*" minWidth={120}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(ctx: any) => {
                      const name = ctx.item?.Name as string | undefined;
                      const sel = !!name && sourceSelected.has(name);
                      const canDrag = !isReleased && !!name;
                      return (
                        <div
                          className={`flex min-w-0 items-center gap-1 py-0.5 pr-1 ${
                            sel ? `rounded-sm ${theme.tab_active}` : ''
                          } ${canDrag ? 'cursor-grab active:cursor-grabbing' : ''}`}
                          draggable={canDrag}
                          onDragStart={(e) => onSourceDragStart(e, ctx.item)}
                          onDragEnd={onSourceDragEnd}
                          title={name ? 'Drag to Import To Table (all checked columns move together)' : undefined}
                        >
                          {sel ? (
                            <i
                              className="fa-solid fa-grip-vertical shrink-0 cursor-grab text-[10px] opacity-80 active:cursor-grabbing"
                              aria-hidden
                            />
                          ) : null}
                          <span className="truncate text-xs">{name ?? ''}</span>
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="Tag" header="Data Type" width={100}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(ctx: any) => (
                      <span className="text-xs">{ctx.item?.Tag ?? ctx.item?.DataType ?? ''}</span>
                    )}
                  />
                </FlexGridColumn>
                <FlexGridColumn width={72} header="">
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(ctx: any) => (
                      <button
                        type="button"
                        onClick={() => addSourceColumnToTarget(ctx.item)}
                        disabled={isReleased}
                        className={`text-xs px-2 py-0.5 rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                        title="Add this column to import table"
                      >
                        Add
                      </button>
                    )}
                  />
                </FlexGridColumn>
              </FlexGrid>
            </div>

            <div
              role="separator"
              aria-orientation="vertical"
              aria-label="Resize import panels"
              title="Drag to resize panels"
              tabIndex={0}
              onMouseDown={onSplitDividerMouseDown}
              onKeyDown={(e) => {
                const el = splitContainerRef.current;
                if (!el) return;
                const maxLeft = Math.max(SPLIT_LEFT_MIN_PX, el.getBoundingClientRect().width - SPLIT_RIGHT_MIN_PX);
                if (e.key === 'ArrowLeft') {
                  e.preventDefault();
                  setLeftPanelWidthPx((w) => Math.max(SPLIT_LEFT_MIN_PX, w - 16));
                } else if (e.key === 'ArrowRight') {
                  e.preventDefault();
                  setLeftPanelWidthPx((w) => Math.min(maxLeft, w + 16));
                }
              }}
              className={`shrink-0 w-1.5 cursor-col-resize border-x self-stretch min-h-0 ${theme.inputBox} hover:opacity-90 focus:outline-none focus:ring-1 focus:ring-inset`}
            />

            <div
              className={`min-w-0 w-1 flex-auto flex flex-col min-h-0 h-full border border-l-0 rounded-r-[4px] overflow-hidden ${theme.mainContentSection}`}
            >
              <div className={`flex items-center justify-between px-2 py-1.5 border-b shrink-0 ${theme.mainContentSection}`}>
                <span className={`text-xs font-semibold ${theme.title}`}>Import To Table</span>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setPreviewTargetOpen(true)}
                    disabled={!levelOneTable?.Name || !dataSourceId}
                    className={`text-xs px-2 py-1 rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                  >
                    Preview Target Table Data
                  </button>
                  <button
                    type="button"
                    onClick={removeSelectedTargetRows}
                    disabled={isReleased}
                    className={`text-xs px-2 py-1 rounded-[4px] disabled:opacity-50 ${theme.button_default}`}
                  >
                    Remove Row
                  </button>
                </div>
              </div>
              <div className={`px-2 py-2 border-b shrink-0 space-y-2 ${theme.mainContentSection}`}>
                <div className="flex items-center py-1">
                  <label className={`w-28 shrink-0 text-xs ${theme.label} mr-2`}>Table Name</label>
                  <input
                    type="text"
                    value={levelOneTable?.Name ?? ''}
                    onChange={(e) => handleTableNameChange(e.target.value)}
                    disabled={isReleased || !levelOneTable}
                    autoComplete="off"
                    className={`min-w-0 flex-auto w-1 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none disabled:opacity-60`}
                  />
                </div>
                <div className="flex items-start py-1">
                  <label className={`w-28 shrink-0 text-xs ${theme.label} mr-2 pt-1`}>Transform Condition</label>
                  <textarea
                    value={levelOneTable?.TransformCondition ?? ''}
                    onChange={(e) => handleTransformConditionChange(e.target.value)}
                    disabled={isReleased || !levelOneTable}
                    rows={2}
                    className={`min-w-0 flex-auto w-1 px-2 py-1 text-xs border ${theme.inputBox} focus:outline-none disabled:opacity-60`}
                  />
                </div>
              </div>
              <FlexGrid
                itemsSource={targetCV}
                selectionMode="None"
                allowDragging={false}
                allowSorting={false}
                isReadOnly={isReleased}
                className="w-full h-1 flex-auto min-h-0"
                beginningEdit={(s: wjGrid.FlexGrid, e: wjGrid.CellRangeEventArgs) => {
                  if (e.col === 0) e.cancel = true;
                }}
                cellEditEnded={(s: wjGrid.FlexGrid, e: wjGrid.CellRangeEventArgs) => onTargetCellEditEnded(s, e)}
              >
                <FlexGridColumn width={44} header="">
                  <FlexGridCellTemplate
                    cellType="ColumnHeader"
                    template={() => (
                      <div className="flex h-full w-full items-center justify-center">
                        <input
                          ref={targetSelectAllRef}
                          type="checkbox"
                          checked={targetSelectAllState.all}
                          onChange={(e) => toggleTargetSelectAll(e.target.checked)}
                          disabled={isReleased}
                          title="Select all import columns"
                          className="rounded border-gray-400"
                          onClick={(e) => e.stopPropagation()}
                        />
                      </div>
                    )}
                  />
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={(ctx: any) => {
                      const name = ctx.item?.Name as string | undefined;
                      return (
                        <div className="flex h-full w-full items-center justify-center">
                          <input
                            type="checkbox"
                            checked={!!name && targetSelected.has(name)}
                            onChange={(e) => name && toggleTargetRowSelected(name, e.target.checked)}
                            disabled={isReleased || !name}
                            className="rounded border-gray-400"
                            onClick={(e) => e.stopPropagation()}
                          />
                        </div>
                      );
                    }}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="Name" header="Column Name" width={120} />
                <FlexGridColumn binding="IsLogicKey" header="Logic Key" width={72} dataType={DataType.Boolean} />
                <FlexGridColumn binding="Tag" header="Data Type" width={110} dataMap={typeDataMap} />
                <FlexGridColumn binding="Length" header="Max Length" width={80} dataType={DataType.Number} />
                <FlexGridColumn binding="Precision" header="Precision" width={80} dataType={DataType.Number} />
                <FlexGridColumn binding="Scale" header="Scale" width={72} dataType={DataType.Number} />
                <FlexGridColumn header="Mapping" width={56}>
                  <FlexGridCellTemplate
                    cellType="Cell"
                    template={() => (
                      <button
                        type="button"
                        className={`w-7 h-6 text-xs rounded-[4px] ${theme.button_default}`}
                        title="Entity mapping — use legacy designer for advanced FK mapping"
                        onClick={(ev) => {
                          ev.stopPropagation();
                          showWarning('Advanced entity column mapping is not in React yet; use the legacy designer if required.');
                        }}
                      >
                        +
                      </button>
                    )}
                  />
                </FlexGridColumn>
                <FlexGridColumn binding="EntityColumnName" header="Mapping Entity Column" width="*" minWidth={100} />
                <FlexGridColumn binding="" header="" width={24} />
              </FlexGrid>
            </div>
        </div>
      </div>

      {previewSourceOpen && stagingTableName && dataSourceId != null && (
        <TableDataPreview
          isOpen={previewSourceOpen}
          onClose={() => setPreviewSourceOpen(false)}
          tableName={stagingTableName}
          dataSourceRegisterId={dataSourceId}
          schemaOwner={schemaOwnerPreview}
          recordLimit={200}
        />
      )}
      {previewTargetOpen && levelOneTable?.Name && dataSourceId != null && (
        <TableDataPreview
          isOpen={previewTargetOpen}
          onClose={() => setPreviewTargetOpen(false)}
          tableName={levelOneTable.Name}
          dataSourceRegisterId={dataSourceId}
          schemaOwner={schemaOwnerPreview}
          recordLimit={200}
        />
      )}
    </div>
  );
};

export default ExcelTableImportEditor;
