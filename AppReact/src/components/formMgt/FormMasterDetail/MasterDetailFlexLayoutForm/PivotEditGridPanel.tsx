import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { useTheme } from '../../../../redux/hooks/useTheme';

// ── Types ────────────────────────────────────────────────────────────────────

interface PivotFieldDto {
  Id?: any;
  DataBaseFieldName: string;
  DisplayName?: string;
  ControlType?: number;
  IsVisible?: boolean;
}

interface AppPivotDto {
  PivotRowFields?: PivotFieldDto[];    // fixed left descriptor columns
  PivotColumnFields?: PivotFieldDto[]; // dimension field (distinct values → dynamic columns)
  PivotValueFields?: PivotFieldDto[];  // value field (cell content)
  IsPivotEdit?: boolean;
}

interface PivotEditGridPanelProps {
  /** AppPivotDto from transactionStructureDto.DictUnitIdPivotGrid[unitId] */
  pivotDto: AppPivotDto;
  /** Normalized rows from DictOneToManyFields[unitId].
   *  Each row has: row-field values, the column-dimension value (e.g. SizeRunSizeId),
   *  a display label for that column (SizeLabel), and the cell value (GradingDelta).
   */
  rows: any[];
  isReadOnly?: boolean;
  /** SizeRunSizeId of the base size — highlighted amber and locked (delta must stay 0). */
  baseSizeId?: any;
  /** Name of the field used as the column-dimension key (default: first PivotColumnField.DataBaseFieldName). */
  columnDimFieldName?: string;
  /** Name of the field holding the human-readable column label (default: "SizeLabel"). */
  columnLabelFieldName?: string;
  /** Name of the value/cell field (default: first PivotValueField.DataBaseFieldName). */
  valueFieldName?: string;
  /** Name of the row group key — identifies which normalized rows belong to the same pivot row (default: first PK-like field). */
  rowGroupKeyFieldName?: string;
  /** Called when the user edits a value cell. Return the updated rows to re-render. */
  onCellEdit?: (rowGroupKey: any, sizeId: any, newValue: decimal) => Promise<any[] | void>;
}

type decimal = number;

// ── Pivot transform ──────────────────────────────────────────────────────────

interface PivotColumn {
  sizeId: any;
  label: string;
  binding: string; // key in the wide-format row object, e.g. "_v_101"
}

interface PivotRow {
  _groupKey: any;
  _originalRows: any[];
  [key: string]: any;
}

function buildPivotData(
  rows: any[],
  pivotDto: AppPivotDto,
  columnDimField: string,
  columnLabelField: string,
  valueField: string,
  rowGroupKeyField: string,
): { columns: PivotColumn[]; pivotRows: PivotRow[] } {
  // 1. Collect distinct column values in encounter order
  const colOrder: any[] = [];
  const colLabels = new Map<any, string>();
  for (const row of rows) {
    const id = row[columnDimField];
    if (id != null && !colLabels.has(id)) {
      colOrder.push(id);
      colLabels.set(id, String(row[columnLabelField] ?? id));
    }
  }
  const columns: PivotColumn[] = colOrder.map((id) => ({
    sizeId: id,
    label: colLabels.get(id) ?? String(id),
    binding: `_v_${String(id)}`,
  }));

  // 2. Group rows by rowGroupKey → one wide pivot row per group
  const groupMap = new Map<any, PivotRow>();
  for (const row of rows) {
    const key = row[rowGroupKeyField];
    if (!groupMap.has(key)) {
      const pivotRow: PivotRow = { _groupKey: key, _originalRows: [] };
      // Copy all row-descriptor fields
      for (const rf of pivotDto.PivotRowFields ?? []) {
        pivotRow[rf.DataBaseFieldName] = row[rf.DataBaseFieldName];
      }
      groupMap.set(key, pivotRow);
    }
    const pivotRow = groupMap.get(key)!;
    pivotRow._originalRows.push(row);
    const sizeId = row[columnDimField];
    if (sizeId != null) {
      pivotRow[`_v_${String(sizeId)}`] = row[valueField] ?? null;
    }
  }

  return { columns, pivotRows: Array.from(groupMap.values()) };
}

// ── Component ────────────────────────────────────────────────────────────────

const PivotEditGridPanel: React.FC<PivotEditGridPanelProps> = ({
  pivotDto,
  rows,
  isReadOnly = false,
  baseSizeId,
  columnDimFieldName,
  columnLabelFieldName = 'SizeLabel',
  valueFieldName,
  rowGroupKeyFieldName,
  onCellEdit,
}) => {
  const { theme } = useTheme();
  const flexGridRef = useRef<any>(null);

  // Resolve field names from pivotDto if not explicitly passed
  const colDimField = columnDimFieldName ?? pivotDto.PivotColumnFields?.[0]?.DataBaseFieldName ?? 'SizeRunSizeId';
  const valField = valueFieldName ?? pivotDto.PivotValueFields?.[0]?.DataBaseFieldName ?? 'GradingDelta';
  // Row group key: first PivotValueField that's a PK or link-to-parent (platform convention: PKs are in PivotValueFields)
  const rowGroupField =
    rowGroupKeyFieldName ??
    pivotDto.PivotValueFields?.find((f) => f.DataBaseFieldName.toLowerCase().includes('id'))?.DataBaseFieldName ??
    'PomSpecLineId';

  const rowFields = useMemo(
    () => (pivotDto.PivotRowFields ?? []).filter((f) => f.IsVisible !== false),
    [pivotDto.PivotRowFields],
  );

  // Build pivot data from normalized rows
  const { columns, pivotRows } = useMemo(
    () => buildPivotData(rows, pivotDto, colDimField, columnLabelFieldName, valField, rowGroupField),
    [rows, pivotDto, colDimField, columnLabelFieldName, valField, rowGroupField],
  );

  const [collectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));

  // Sync CollectionView when pivot rows change
  useEffect(() => {
    (collectionView as any).sourceCollection = pivotRows;
    collectionView.sortDescriptions.clear();
    collectionView.refresh();
  }, [pivotRows, collectionView]);

  // Cell edit: map wide-format cell back to (groupKey, sizeId, newValue)
  const handleCellEditEnded = useCallback(
    async (s: any, e: any) => {
      if (isReadOnly || !onCellEdit) return;
      const flex = s?.control ?? s;
      const rowIndex = e?.row ?? flex?.selection?.row;
      if (typeof rowIndex !== 'number' || rowIndex < 0) return;

      const pivotRow: PivotRow = flex.rows[rowIndex]?.dataItem;
      if (!pivotRow) return;

      const colIndex: number = e?.col ?? flex?.selection?.col;
      if (typeof colIndex !== 'number') return;

      const colBinding: string = flex.columns[colIndex]?.binding ?? '';
      // Only act on dynamic value cells (binding starts with "_v_")
      if (!colBinding.startsWith('_v_')) return;

      const sizeId = colBinding.slice(3); // strip "_v_"
      const newValue = pivotRow[colBinding] ?? 0;

      const updatedRows = await onCellEdit(pivotRow._groupKey, sizeId, newValue);
      if (Array.isArray(updatedRows)) {
        const { pivotRows: newPivotRows } = buildPivotData(
          updatedRows, pivotDto, colDimField, columnLabelFieldName, valField, rowGroupField,
        );
        (collectionView as any).sourceCollection = newPivotRows;
        collectionView.sortDescriptions.clear();
        collectionView.refresh();
      }
    },
    [isReadOnly, onCellEdit, pivotDto, colDimField, columnLabelFieldName, valField, rowGroupField, collectionView],
  );

  // Base size column styling
  const baseSizeBinding = baseSizeId != null ? `_v_${String(baseSizeId)}` : null;

  if (!pivotDto || rows.length === 0) {
    return (
      <div className={`flex items-center justify-center h-16 text-xs ${theme.label}`}>
        No grading data
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
        {/* Fixed row descriptor columns */}
        {rowFields.map((rf) => (
          <FlexGridColumn
            key={String(rf.Id ?? rf.DataBaseFieldName)}
            binding={rf.DataBaseFieldName}
            header={rf.DisplayName ?? rf.DataBaseFieldName}
            width={rf.DataBaseFieldName === 'BaseValue' || rf.DataBaseFieldName === 'Tolerance' ? 70 : 110}
            isReadOnly={true}
          />
        ))}

        {/* Dynamic size columns — one per distinct SizeRunSizeId */}
        {columns.map((col) => {
          const isBase = col.binding === baseSizeBinding;
          return (
            <FlexGridColumn
              key={col.binding}
              binding={col.binding}
              header={col.label}
              width={60}
              dataType="Number"
              format="n3"
              isReadOnly={isReadOnly || isBase}
            >
              {isBase && (
                <FlexGridCellTemplate
                  cellType="Cell"
                  template={(cell: any) => {
                    const val = cell.item?.[col.binding] ?? 0;
                    return (
                      <div
                        className="flex items-center justify-end w-full h-full px-1 text-xs font-medium text-gray-800"
                        style={{ background: '#fbbf24' /* amber-400 */ }}
                      >
                        {Number(val).toFixed(3)}
                      </div>
                    );
                  }}
                />
              )}
            </FlexGridColumn>
          );
        })}

        {/* Spacer */}
        <FlexGridColumn header="" binding="" width="*" isReadOnly={true} />
      </FlexGrid>
    </div>
  );
};

export default PivotEditGridPanel;
