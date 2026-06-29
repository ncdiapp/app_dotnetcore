import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { ChildPivotProjectionModel } from './childPivotProjectionHelper';

interface ChildPivotProjectionGridProps {
  /** Server-built model (columns + wide rows) from BuildChildPivotProjection. */
  model: ChildPivotProjectionModel | null;
  isReadOnly?: boolean;
  /** Resolve a DDL/lookup DataMap for a field id. */
  resolveDataMap?: (fieldId: any) => DataMap | null;
  /** Resolve the configured column width (DisplayWidth) for a field id. */
  resolveWidth?: (fieldId: any) => number | undefined;
  /** Called with the edited wide rows so the host can fold them back (server) and persist. */
  onWideRowsChange?: (wideRows: any[]) => void;
}

/**
 * Pure renderer for the Child Unit Pivot Columns projection. All transform logic is server-side;
 * this component only renders the server model and emits edited wide rows back to the host.
 */
const ChildPivotProjectionGrid: React.FC<ChildPivotProjectionGridProps> = ({
  model,
  isReadOnly = false,
  resolveDataMap,
  resolveWidth,
  onWideRowsChange,
}) => {
  const { theme } = useTheme();
  const emAppControlType = useEnumValues('EmAppControlType');
  const flexGridRef = useRef<any>(null);

  const hostColumns = useMemo(() => model?.HostColumns ?? [], [model]);
  const columnGroups = useMemo(() => model?.ColumnGroups ?? [], [model]);
  const wideRows = useMemo(() => model?.WideRows ?? [], [model]);

  const [collectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));

  useEffect(() => {
    (collectionView as any).sourceCollection = wideRows;
    collectionView.sortDescriptions.clear();
    collectionView.refresh();
  }, [wideRows, collectionView]);

  const handleCellEditEnded = useCallback(() => {
    if (isReadOnly || !onWideRowsChange) return;
    const source = (collectionView as any).sourceCollection ?? wideRows;
    onWideRowsChange(source);
  }, [isReadOnly, onWideRowsChange, collectionView, wideRows]);

  const dataTypeFor = useCallback(
    (controlType?: number | null): string | undefined => {
      const ctl = controlType != null ? Number(controlType) : NaN;
      if (ctl === Number(emAppControlType?.Numeric)) return 'Number';
      if (ctl === Number(emAppControlType?.CheckBox)) return 'Boolean';
      if (ctl === Number(emAppControlType?.Date) || ctl === Number(emAppControlType?.DateTimeDetail))
        return 'Date';
      return undefined;
    },
    [emAppControlType],
  );

  const formatFor = useCallback(
    (controlType?: number | null): string | undefined => {
      const ctl = controlType != null ? Number(controlType) : NaN;
      if (ctl === Number(emAppControlType?.Date)) return 'd';
      if (ctl === Number(emAppControlType?.DateTimeDetail)) return 'g';
      return undefined;
    },
    [emAppControlType],
  );

  const visibleValueColumnCount = useMemo(
    () => columnGroups.reduce((n, g) => n + (g.Columns ?? []).filter((c) => c.Visible !== false).length, 0),
    [columnGroups],
  );

  // The column-group header should show the source field's DISPLAY text (e.g. color name),
  // not the raw stored id. Reuse the same DDL/lookup map the cells use, keyed by the source field id.
  const sourceDataMap = useMemo(
    () => (model?.ColumnSourceFieldId != null ? resolveDataMap?.(model.ColumnSourceFieldId) ?? null : null),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [model?.ColumnSourceFieldId, model],
  );

  const groupHeaderLabel = useCallback(
    (group: { Header: string; ColValue?: any }): string => {
      if (sourceDataMap && group.ColValue != null) {
        for (const key of [group.ColValue, String(group.ColValue), Number(group.ColValue)]) {
          try {
            const text = sourceDataMap.getDisplayValue(key);
            if (text != null && String(text).length > 0) return String(text);
          } catch {
            /* fall through to raw header */
          }
        }
      }
      return group.Header;
    },
    [sourceDataMap],
  );

  if (!model || model.IsConfigured === false) {
    return (
      <div className={`flex flex-col gap-1 p-3 text-xs ${theme.label}`}>
        <div className="font-semibold text-amber-600">
          Child pivot projection is not configured for this unit. Set a grandchild unit&apos;s Grid
          Display Type to &quot;ChildUnitPivotColumns&quot;, mark one Pivot Column field (with a Matrix
          ForeignKey Field to the source grid) and at least one Pivot Value field.
        </div>
      </div>
    );
  }

  const noSourceRows = columnGroups.length === 0;
  const nothingVisible = !noSourceRows && visibleValueColumnCount === 0;

  if (noSourceRows || nothingVisible) {
    return (
      <div className={`flex flex-col gap-1 p-3 text-xs ${theme.label}`}>
        {noSourceRows && (
          <div>
            No source rows to build columns. Add rows to the source grid
            {model.ColumnSourceUnitId != null ? ` (unit ${model.ColumnSourceUnitId})` : ''} first.
          </div>
        )}
        {nothingVisible && (
          <div className="font-semibold text-amber-600">
            All Pivot Value fields are hidden (IsVisible = false). Mark at least one as visible.
          </div>
        )}
        <div className="mt-1 opacity-80">
          <div>Child rows: {model.ChildRowCount ?? 0}</div>
          <div>Source rows: {model.SourceRowCount ?? 0}</div>
          <div>Column key field: {model.ColumnKeyFieldName ?? '(none)'}</div>
          <div>Source field: {model.ColumnSourceFieldName ?? '(none)'}</div>
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
        {/* Host (child) descriptor columns */}
        {hostColumns.map((hc) => (
          <FlexGridColumn
            key={`host_${hc.Binding}`}
            binding={hc.Binding}
            header={hc.Header}
            width={resolveWidth?.(hc.FieldId) ?? 150}
            isReadOnly={isReadOnly || hc.IsReadOnly}
            dataType={dataTypeFor(hc.ControlType)}
            format={formatFor(hc.ControlType)}
            dataMap={resolveDataMap ? resolveDataMap(hc.FieldId) ?? undefined : undefined}
          />
        ))}

        {/* Dynamic value columns — one per (source value × visible value field) */}
        {columnGroups.flatMap((group) => {
          const visibleLeaves = (group.Columns ?? []).filter((c) => c.Visible !== false);
          const groupLabel = groupHeaderLabel(group);
          return visibleLeaves.map((leaf) => {
            const header =
              visibleLeaves.length > 1 ? `${groupLabel} · ${leaf.Header}` : groupLabel;
            return (
              <FlexGridColumn
                key={`val_${leaf.Binding}`}
                binding={leaf.Binding}
                header={header}
                width={resolveWidth?.(leaf.FieldId) ?? 110}
                isReadOnly={isReadOnly}
                dataType={dataTypeFor(leaf.ControlType)}
                format={formatFor(leaf.ControlType)}
                dataMap={resolveDataMap ? resolveDataMap(leaf.FieldId) ?? undefined : undefined}
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

export default ChildPivotProjectionGrid;
