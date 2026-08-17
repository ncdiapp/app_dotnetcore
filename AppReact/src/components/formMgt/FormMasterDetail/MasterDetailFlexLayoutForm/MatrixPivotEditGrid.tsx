import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import {
  buildPivotModel,
  wideToFlatRows,
  PivotDtoLike,
  PivotFieldDef,
} from './pivotGridHelper';
import { wijmoColumnTypeAndFormat, coerceNumericWideRowsInPlace } from './childPivotProjectionHelper';

interface MatrixPivotEditGridProps {
  /** AppPivotDto from transactionStructureDto.DictUnitIdPivotGrid[unitId]. */
  pivotDto: PivotDtoLike;
  /** Flat AppChildDataDto rows from DictOneToManyFields[unitId]. */
  rows: any[];
  isReadOnly?: boolean;
  /** Resolve a DDL/lookup DataMap for a field id (DataGridLayout standalone data source). */
  resolveDataMap?: (fieldId: any) => DataMap | null;
  /** Resolve the configured column width (DisplayWidth) for a field id. */
  resolveWidth?: (fieldId: any) => number | undefined;
  /** Resolve Nbdecimal for a field id when the slim pivot DTO omitted it. */
  resolveNbdecimal?: (fieldId: any) => number | undefined;
  /** Called with the rebuilt flat rows after an edit so the parent can persist them. */
  onRowsChange?: (flatRows: any[]) => void;
  /**
   * Primary-key field DataBaseFieldNames for this unit. They are excluded from pivot ROW
   * grouping (a unique PK would otherwise split every source row into its own row), but
   * still round-trip as hidden value leaves so the PK is preserved on save.
   */
  primaryKeyFieldNames?: string[];
}

/**
 * Generic pivot-edit grid driven entirely by `AppPivotDto` (row/column/value fields).
 *
 * Cross-tabs flat one-to-many rows into a wide editable grid: distinct column-field
 * value combinations become dynamic columns, value fields become editable cells.
 * On edit it rebuilds the flat rows (incl. hidden PK / link fields) and hands them up.
 *
 * This is independent of the POM-specific `PivotEditGridPanel`.
 */
const MatrixPivotEditGrid: React.FC<MatrixPivotEditGridProps> = ({
  pivotDto,
  rows,
  isReadOnly = false,
  resolveDataMap,
  resolveWidth,
  resolveNbdecimal,
  onRowsChange,
  primaryKeyFieldNames,
}) => {
  const { theme } = useTheme();
  const emAppControlType = useEnumValues('EmAppControlType');
  const flexGridRef = useRef<any>(null);

  // Exclude primary-key fields from row grouping (kept as hidden value leaves for round-trip).
  const effectivePivotDto = useMemo<PivotDtoLike>(() => {
    if (!primaryKeyFieldNames?.length || !pivotDto?.PivotRowFields?.length) return pivotDto;
    const pkSet = new Set(primaryKeyFieldNames);
    return {
      ...pivotDto,
      PivotRowFields: pivotDto.PivotRowFields.filter((f) => !pkSet.has(f.DataBaseFieldName)),
    };
  }, [pivotDto, primaryKeyFieldNames]);

  const getDisplay = useCallback(
    (field: PivotFieldDef, value: any): string => {
      if (value == null) return '';
      const dm = resolveDataMap ? resolveDataMap(field.Id) : null;
      if (dm) {
        try {
          const disp = dm.getDisplayValue(value);
          if (disp != null && disp !== '') return String(disp);
        } catch {
          /* fall through to raw value */
        }
      }
      return String(value);
    },
    [resolveDataMap],
  );

  const { rowColumns, columnGroups, valueFields, wideRows } = useMemo(
    () => buildPivotModel(effectivePivotDto, rows, getDisplay),
    [effectivePivotDto, rows, getDisplay],
  );

  // Field def lookups (by DataBaseFieldName) for control type / data map resolution.
  const valueFieldByDb = useMemo(() => {
    const m = new Map<string, PivotFieldDef>();
    for (const vf of valueFields) m.set(vf.DataBaseFieldName, vf);
    return m;
  }, [valueFields]);

  const numericBindings = useMemo(() => {
    const names: string[] = [];
    const numericType = Number(emAppControlType?.Numeric);
    const isNumeric = (ctl: unknown) =>
      Number.isFinite(numericType) && ctl != null && Number(ctl) === numericType;
    for (const rc of rowColumns) {
      if (isNumeric(rc.controlType)) names.push(rc.binding);
    }
    for (const g of columnGroups) {
      for (const leaf of g.columns) {
        const vf = valueFieldByDb.get(leaf.dataBaseFieldName);
        if (isNumeric(leaf.controlType ?? vf?.ControlType)) names.push(leaf.binding);
      }
    }
    return names;
  }, [rowColumns, columnGroups, valueFieldByDb, emAppControlType?.Numeric]);

  const numericBindingKey = numericBindings.slice().sort().join('|');

  const [collectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const boundRowsRef = useRef<any[] | null>(null);
  const boundNumericKeyRef = useRef('');

  useEffect(() => {
    const alreadyBound = boundRowsRef.current === rows;
    const sameNumericKey = boundNumericKeyRef.current === numericBindingKey;

    if (alreadyBound && sameNumericKey) return;

    if (alreadyBound) {
      const coerced = coerceNumericWideRowsInPlace((collectionView as any).sourceCollection, numericBindings);
      (collectionView as any).sourceCollection = coerced;
      boundNumericKeyRef.current = numericBindingKey;
      collectionView.refresh();
      return;
    }

    const coerced = coerceNumericWideRowsInPlace(wideRows, numericBindings);
    (collectionView as any).sourceCollection = coerced;
    boundRowsRef.current = rows;
    boundNumericKeyRef.current = numericBindingKey;
    collectionView.sortDescriptions.clear();
    collectionView.refresh();
  }, [rows, wideRows, numericBindingKey, numericBindings, collectionView]);

  const handleCellEditEnded = useCallback(() => {
    if (isReadOnly || !onRowsChange) return;
    const source = (collectionView as any).sourceCollection ?? wideRows;
    const flat = wideToFlatRows(rowColumns, columnGroups, source);
    onRowsChange(flat);
  }, [isReadOnly, onRowsChange, collectionView, wideRows, rowColumns, columnGroups]);

  const columnTypeAndFormat = useCallback(
    (controlType?: number, nbdecimal?: unknown) =>
      wijmoColumnTypeAndFormat(controlType, nbdecimal, emAppControlType),
    [emAppControlType],
  );

  const visibleValueColumnCount = useMemo(
    () => columnGroups.reduce((n, g) => n + g.columns.filter((c) => c.visible).length, 0),
    [columnGroups],
  );

  if (!pivotDto) {
    return (
      <div className={`flex items-center justify-center h-16 text-xs ${theme.label}`}>
        Pivot grid configuration not found.
      </div>
    );
  }

  // Diagnostic empty-state: distinguish "no rows" from "pivot not configured / nothing visible".
  const rowFieldDefs = effectivePivotDto.PivotRowFields ?? [];
  const columnFieldDefs = effectivePivotDto.PivotColumnFields ?? [];
  const valueFieldDefs = effectivePivotDto.PivotValueFields ?? [];
  const noRows = !wideRows.length;
  const noPivotConfig = columnFieldDefs.length === 0 || valueFieldDefs.length === 0;
  const nothingVisible = !noRows && !noPivotConfig && visibleValueColumnCount === 0;

  if (noRows || noPivotConfig || nothingVisible) {
    const names = (arr: PivotFieldDef[]) =>
      arr.length ? arr.map((f) => f.DisplayName ?? f.DataBaseFieldName).join(', ') : '(none)';
    return (
      <div className={`flex flex-col gap-1 p-3 text-xs ${theme.label}`}>
        {noRows && (
          <div>No data to pivot. Use &quot;Generate Matrix&quot; to create rows, or add data first.</div>
        )}
        {!noRows && noPivotConfig && (
          <div className="font-semibold text-amber-600">
            Pivot is not fully configured: this unit needs at least one Pivot Column field and one
            Pivot Value field.
          </div>
        )}
        {!noRows && !noPivotConfig && nothingVisible && (
          <div className="font-semibold text-amber-600">
            All Pivot Value fields are hidden (IsVisible = false). Mark at least one value field as
            visible.
          </div>
        )}
        <div className="mt-1 opacity-80">
          <div>Rows in unit: {rows?.length ?? 0}</div>
          <div>Pivot Row fields ({rowFieldDefs.length}): {names(rowFieldDefs)}</div>
          <div>Pivot Column fields ({columnFieldDefs.length}): {names(columnFieldDefs)}</div>
          <div>Pivot Value fields ({valueFieldDefs.length}): {names(valueFieldDefs)}</div>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full h-full overflow-hidden">
      <FlexGrid
        ref={flexGridRef}
        itemsSource={collectionView}
        isReadOnly={isReadOnly}
        allowSorting={false}
        headersVisibility="All"
        selectionMode="Cell"
        className="w-full h-full"
        style={{ height: '100%', width: '100%', border: 'none' }}
        cellEditEnded={handleCellEditEnded}
      >
        {/* Fixed left descriptor columns (read-only). Hidden row fields (IsVisible=false)
            still group and round-trip, but are not rendered as columns. */}
        {rowColumns.filter((rc) => rc.visible).map((rc) => {
          const { dataType, format } = columnTypeAndFormat(
            rc.controlType,
            rc.nbdecimal ?? resolveNbdecimal?.(rc.fieldId),
          );
          return (
          <FlexGridColumn
            key={`row_${rc.binding}`}
            binding={rc.binding}
            header={rc.header}
            width={resolveWidth?.(rc.fieldId) ?? 130}
            isReadOnly={true}
            dataType={dataType}
            format={format}
            dataMap={resolveDataMap ? resolveDataMap(rc.fieldId) ?? undefined : undefined}
          />
          );
        })}

        {/* Dynamic value columns — one per (column-key combination × visible value field) */}
        {columnGroups.flatMap((group) => {
          const visibleLeaves = group.columns.filter((c) => c.visible);
          return visibleLeaves.map((leaf) => {
            const vf = valueFieldByDb.get(leaf.dataBaseFieldName);
            const header =
              visibleLeaves.length > 1 ? `${group.header} · ${leaf.header}` : group.header;
            const { dataType, format } = columnTypeAndFormat(
              leaf.controlType ?? vf?.ControlType,
              leaf.nbdecimal ?? vf?.Nbdecimal ?? resolveNbdecimal?.(leaf.fieldId ?? vf?.Id),
            );
            return (
              <FlexGridColumn
                key={`val_${leaf.binding}`}
                binding={leaf.binding}
                header={header}
                width={resolveWidth?.(leaf.fieldId) ?? 110}
                isReadOnly={isReadOnly}
                dataType={dataType}
                format={format}
                dataMap={
                  vf && resolveDataMap ? resolveDataMap(vf.Id) ?? undefined : undefined
                }
              />
            );
          });
        })}

        {/* Spacer */}
        <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
      </FlexGrid>
    </div>
  );
};

export default MatrixPivotEditGrid;
