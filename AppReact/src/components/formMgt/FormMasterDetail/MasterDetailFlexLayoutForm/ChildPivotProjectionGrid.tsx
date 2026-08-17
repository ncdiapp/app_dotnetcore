import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridColumnGroup, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { fileThumbnailUrl } from '../../../../webapi/fileEndpoints';
import {
  ChildPivotProjectionModel,
  ProjColumnGroup,
  ProjLeafColumn,
  countVisibleValueColumns,
  enumerateLeafColumnGroups,
  wijmoColumnTypeAndFormat,
  coerceNumericWideRowsInPlace,
} from './childPivotProjectionHelper';

const EMPTY_WIDE_ROWS: any[] = [];

export type ProjectionImageCellContext = {
  rowIndex: number;
  binding: string;
  dbFieldName: string;
  fileId: number | null;
  clientX: number;
  clientY: number;
};

interface ChildPivotProjectionGridProps {
  /** Server-built model (columns + wide rows) from BuildChildPivotProjection. */
  model: ChildPivotProjectionModel | null;
  isReadOnly?: boolean;
  /** Optional ref to the underlying FlexGrid (host uses this to read edited wide rows). */
  gridRef?: React.Ref<any>;
  /** Resolve a DDL/lookup DataMap for a field id. */
  resolveDataMap?: (fieldId: any) => DataMap | null;
  /** Resolve the configured column width (DisplayWidth) for a field id. */
  resolveWidth?: (fieldId: any) => number | undefined;
  /** Resolve Nbdecimal for a field id (grandchild / host AppTransactionField). */
  resolveNbdecimal?: (fieldId: any) => number | undefined;
  /** Open image cell actions menu (upload / library / preview). */
  onImageCellMenu?: (ctx: ProjectionImageCellContext) => void;
  /** Open full-size image preview. */
  onImagePreview?: (fileId: number) => void;
  /** Angular childCellEditBeginning parity — swap column dataMap to row-level cascading lookup. */
  onCellEditBeginning?: (grid: any, e: any) => void;
  /** Restore standalone dataMap after edit (Angular cellEditEnding). */
  onCellEditEnding?: (grid: any, e: any) => void;
  /** Called after edit; host folds wide rows and may refresh cascading data sources. */
  onCellEditEnded?: (grid: any, e: any) => void;
  /** @deprecated use onCellEditEnded */
  onWideRowsChange?: (wideRows: any[]) => void;
}

/**
 * Pure renderer for the Child Unit Pivot Columns projection. All transform logic is server-side;
 * this component only renders the server model and emits edited wide rows back to the host.
 *
 * Two paths (engineering clarity):
 * 1. Flat FlexGridColumn when IsNeedPivotColumnGroup is false
 * 2. FlexGridColumnGroup hierarchy when IsNeedPivotColumnGroup is true (IsPivotRow parents)
 */
const ChildPivotProjectionGrid: React.FC<ChildPivotProjectionGridProps> = ({
  model,
  isReadOnly = false,
  gridRef,
  resolveDataMap,
  resolveWidth,
  resolveNbdecimal,
  onImageCellMenu,
  onImagePreview,
  onCellEditBeginning,
  onCellEditEnding,
  onCellEditEnded,
  onWideRowsChange,
}) => {
  const { theme } = useTheme();
  const emAppControlType = useEnumValues('EmAppControlType');
  const flexGridRef = useRef<any>(null);

  const setGridRef = useCallback(
    (instance: any) => {
      flexGridRef.current = instance;
      if (typeof gridRef === 'function') gridRef(instance);
      else if (gridRef && typeof gridRef === 'object') (gridRef as React.MutableRefObject<any>).current = instance;
    },
    [gridRef],
  );

  const parseFileId = useCallback((raw: any): number | null => {
    if (raw == null || raw === '') return null;
    const n = Number(raw);
    return Number.isFinite(n) && n > 0 ? n : null;
  }, []);

  const isImageControlType = useCallback(
    (controlType?: number | null): boolean => {
      const ctl = controlType != null ? Number(controlType) : NaN;
      return (
        ctl === Number(emAppControlType?.Image) ||
        ctl === Number(emAppControlType?.ExternalImageUrl) ||
        ctl === Number(emAppControlType?.ImageBinary)
      );
    },
    [emAppControlType?.ExternalImageUrl, emAppControlType?.Image, emAppControlType?.ImageBinary],
  );

  const hostColumns = useMemo(() => model?.HostColumns ?? [], [model]);
  const columnGroups = useMemo(() => model?.ColumnGroups ?? [], [model]);
  const wideRows = model?.WideRows ?? EMPTY_WIDE_ROWS;
  const useColumnGroups = Boolean(model?.IsNeedPivotColumnGroup);

  const numericBindings = useMemo(() => {
    const names: string[] = [];
    const numericType = Number(emAppControlType?.Numeric);
    const isNumeric = (ctl: unknown) =>
      Number.isFinite(numericType) && ctl != null && Number(ctl) === numericType;
    for (const hc of hostColumns) {
      if (isNumeric(hc.ControlType)) names.push(hc.Binding);
    }
    for (const group of enumerateLeafColumnGroups(columnGroups)) {
      for (const leaf of group.Columns ?? []) {
        if (isNumeric(leaf.ControlType)) names.push(leaf.Binding);
      }
    }
    return names;
  }, [hostColumns, columnGroups, emAppControlType?.Numeric]);

  const numericBindingKey = numericBindings.slice().sort().join('|');

  const [collectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));
  const boundWideRowsRef = useRef<any[] | null>(null);
  const boundNumericKeyRef = useRef('');

  useEffect(() => {
    const alreadyBound = boundWideRowsRef.current === wideRows;
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
    boundWideRowsRef.current = wideRows;
    boundNumericKeyRef.current = numericBindingKey;
    collectionView.sortDescriptions.clear();
    collectionView.refresh();
  }, [wideRows, numericBindingKey, numericBindings, collectionView]);

  const handleCellEditEnded = useCallback(
    (s: any, e: any) => {
      if (isReadOnly) return;
      if (onCellEditEnded) {
        onCellEditEnded(s, e);
        return;
      }
      if (!onWideRowsChange) return;
      const source = (collectionView as any).sourceCollection ?? wideRows;
      onWideRowsChange(source);
    },
    [isReadOnly, onCellEditEnded, onWideRowsChange, collectionView, wideRows],
  );

  const handleCellEditBeginning = useCallback(
    (s: any, e: any) => {
      if (isReadOnly) return;
      onCellEditBeginning?.(s, e);
    },
    [isReadOnly, onCellEditBeginning],
  );

  const handleCellEditEnding = useCallback(
    (s: any, e: any) => {
      onCellEditEnding?.(s, e);
    },
    [onCellEditEnding],
  );

  const columnTypeAndFormat = useCallback(
    (controlType?: number | null, nbdecimal?: unknown) =>
      wijmoColumnTypeAndFormat(controlType, nbdecimal, emAppControlType),
    [emAppControlType],
  );

  const visibleValueColumnCount = useMemo(
    () => countVisibleValueColumns(columnGroups),
    [columnGroups],
  );

  // Resolve display text on each render so headers update when formStructure entity data loads.
  // Parent Column Groups use FieldId + ColValue (grandchild field value); size leaves use ColumnSourceFieldId.
  const groupHeaderLabel = useCallback(
    (group: { Header: string; ColValue?: any; FieldId?: number | null }): string => {
      const fieldIdForMap =
        group.FieldId != null ? group.FieldId : model?.ColumnSourceFieldId ?? null;
      if (fieldIdForMap != null && group.ColValue != null) {
        const sourceDataMap = resolveDataMap?.(fieldIdForMap) ?? null;
        if (sourceDataMap) {
          for (const key of [group.ColValue, String(group.ColValue), Number(group.ColValue)]) {
            try {
              const text = sourceDataMap.getDisplayValue(key);
              if (text != null && String(text).length > 0) return String(text);
            } catch {
              /* fall through */
            }
          }
        }
      }
      return group.Header;
    },
    [model?.ColumnSourceFieldId, resolveDataMap],
  );

  const renderImageLeaf = useCallback(
    (
      leaf: ProjLeafColumn,
      header: string,
      colWidth: number,
      colReadOnly: boolean,
      asColumnGroup: boolean,
    ) => {
      const binding = leaf.Binding;
      const dbFieldName = leaf.DataBaseFieldName ?? '';
      const cellTemplate = (
        <FlexGridCellTemplate
          cellType="Cell"
          template={(cell: any) => {
            const item = cell?.item as Record<string, any> | undefined;
            const raw = item?.[binding];
            const fileId = parseFileId(raw);
            const rowIndex =
              typeof item?.__rowIndex === 'number' && item.__rowIndex >= 0
                ? item.__rowIndex
                : Number(cell?.row?.index ?? -1);
            const thumbUrl = fileId ? fileThumbnailUrl(fileId) : null;
            return (
              <div className="flex items-center justify-between w-full h-full gap-1">
                <div className="flex items-center gap-2 min-w-0 flex-auto">
                  {thumbUrl ? (
                    <img
                      src={thumbUrl}
                      alt=""
                      className="max-h-[30px] max-w-[30px] object-contain cursor-pointer flex-shrink-0"
                      onClick={(e) => {
                        e.stopPropagation();
                        if (isReadOnly || !fileId) return;
                        onImagePreview?.(fileId);
                      }}
                    />
                  ) : (
                    <div className="w-[30px] h-[30px]" />
                  )}
                </div>
                {!isReadOnly && onImageCellMenu && (
                  <button
                    type="button"
                    className={`${theme.button_default} w-7 h-6 rounded-[4px] text-xs flex items-center justify-center flex-shrink-0`}
                    title="Actions"
                    onMouseDown={(e) => e.stopPropagation()}
                    onClick={(e) => {
                      e.stopPropagation();
                      const rect = (e.currentTarget as HTMLButtonElement).getBoundingClientRect();
                      onImageCellMenu({
                        rowIndex,
                        binding,
                        dbFieldName,
                        fileId,
                        clientX: rect.right,
                        clientY: rect.top,
                      });
                    }}
                  >
                    <i className="fa-solid fa-bars" aria-hidden="true" />
                  </button>
                )}
              </div>
            );
          }}
        />
      );

      if (asColumnGroup) {
        return (
          <FlexGridColumnGroup
            key={`val_${leaf.Binding}`}
            name={leaf.FieldId != null ? String(leaf.FieldId) : ''}
            binding={binding}
            header={header}
            width={colWidth}
            isReadOnly={colReadOnly}
            isRequired={false}
          >
            {cellTemplate}
          </FlexGridColumnGroup>
        );
      }

      return (
        <FlexGridColumn
          key={`val_${leaf.Binding}`}
          name={leaf.FieldId != null ? String(leaf.FieldId) : ''}
          binding={binding}
          header={header}
          width={colWidth}
          isReadOnly={colReadOnly}
          isRequired={false}
        >
          {cellTemplate}
        </FlexGridColumn>
      );
    },
    [isReadOnly, onImageCellMenu, onImagePreview, parseFileId, theme.button_default],
  );

  const renderValueLeaf = useCallback(
    (leaf: ProjLeafColumn, header: string, asColumnGroup: boolean) => {
      const isImageColumn = isImageControlType(leaf.ControlType);
      const colWidth = resolveWidth?.(leaf.FieldId) ?? (isImageColumn ? 130 : 110);
      const colReadOnly = isReadOnly || isImageColumn;
      const nbdecimal = leaf.Nbdecimal ?? (leaf as any).nbdecimal ?? resolveNbdecimal?.(leaf.FieldId);
      const { dataType, format } = columnTypeAndFormat(leaf.ControlType, nbdecimal);

      if (isImageColumn) {
        return renderImageLeaf(leaf, header, colWidth, colReadOnly, asColumnGroup);
      }

      if (asColumnGroup) {
        return (
          <FlexGridColumnGroup
            key={`val_${leaf.Binding}`}
            name={leaf.FieldId != null ? String(leaf.FieldId) : ''}
            binding={leaf.Binding}
            header={header}
            width={colWidth}
            isReadOnly={colReadOnly}
            isRequired={false}
            dataType={dataType}
            format={format}
            dataMap={resolveDataMap ? resolveDataMap(leaf.FieldId) ?? undefined : undefined}
          />
        );
      }

      return (
        <FlexGridColumn
          key={`val_${leaf.Binding}`}
          name={leaf.FieldId != null ? String(leaf.FieldId) : ''}
          binding={leaf.Binding}
          header={header}
          width={colWidth}
          isReadOnly={colReadOnly}
          isRequired={false}
          dataType={dataType}
          format={format}
          dataMap={resolveDataMap ? resolveDataMap(leaf.FieldId) ?? undefined : undefined}
        />
      );
    },
    [columnTypeAndFormat, isImageControlType, isReadOnly, renderImageLeaf, resolveDataMap, resolveNbdecimal, resolveWidth],
  );

  /** Flat path: one FlexGridColumn per leaf; header = size or size · value. */
  const renderFlatValueColumns = useCallback(() => {
    const leafGroups = enumerateLeafColumnGroups(columnGroups);
    return leafGroups.flatMap((group) => {
      const visibleLeaves = (group.Columns ?? []).filter((c) => c.Visible !== false);
      const groupLabel = groupHeaderLabel(group);
      return visibleLeaves.map((leaf) => {
        const header = visibleLeaves.length > 1 ? `${groupLabel} · ${leaf.Header}` : groupLabel;
        return renderValueLeaf(leaf, header, false);
      });
    });
  }, [columnGroups, groupHeaderLabel, renderValueLeaf]);

  /**
   * ColumnGroup path: recursive FlexGridColumnGroup.
   * IsPivotRow parents nest by grandchild field VALUES; comboId groups have Columns (leaves).
   */
  const renderColumnGroupTree = useCallback(
    (groups: ProjColumnGroup[]): React.ReactNode[] => {
      return groups.flatMap((group) => {
        const children = group.ChildGroups ?? [];
        if (children.length > 0) {
          const parentLabel = groupHeaderLabel(group);
          return [
            <FlexGridColumnGroup
              key={`grp_${group.FieldId ?? 'f'}_${group.ComboId ?? group.Header}`}
              header={parentLabel}
              align="center"
            >
              {renderColumnGroupTree(children)}
            </FlexGridColumnGroup>,
          ];
        }

        // Data-bearing comboId group → leaves under an optional size-level group header
        const visibleLeaves = (group.Columns ?? []).filter((c) => c.Visible !== false);
        if (visibleLeaves.length === 0) return [];

        const groupLabel = groupHeaderLabel(group);

        // Single visible value: leaf ColumnGroup with size as header (no extra nesting).
        if (visibleLeaves.length === 1) {
          return [renderValueLeaf(visibleLeaves[0], groupLabel, true)];
        }

        // Multiple values: nest under a size ColumnGroup.
        return [
          <FlexGridColumnGroup
            key={`combo_${group.ComboId ?? group.Header}`}
            header={groupLabel}
            align="center"
          >
            {visibleLeaves.map((leaf) => renderValueLeaf(leaf, leaf.Header, true))}
          </FlexGridColumnGroup>,
        ];
      });
    },
    [groupHeaderLabel, renderValueLeaf],
  );

  if (!model) {
    return null;
  }

  if (model.IsConfigured === false) {
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

  const nothingVisible = columnGroups.length > 0 && visibleValueColumnCount === 0;
  const leafGroupsExist = enumerateLeafColumnGroups(columnGroups).length > 0;

  return (
    <div className="w-full h-full min-h-0 overflow-hidden flex flex-col">
      {nothingVisible && (
        <div className={`shrink-0 px-3 py-1 text-xs font-semibold text-amber-600`}>
          All Pivot Value fields are hidden (IsVisible = false). Mark at least one as visible.
        </div>
      )}
      <div className="h-full w-full">
        {useColumnGroups ? (
          <FlexGrid
            ref={setGridRef}
            itemsSource={collectionView}
            isReadOnly={isReadOnly}
            allowSorting={false}
            headersVisibility="All"
            selectionMode="Cell"
            className="w-full h-full"
            style={{ height: '100%', width: '100%', border: 'none' }}
            beginningEdit={handleCellEditBeginning}
            cellEditEnding={handleCellEditEnding}
            cellEditEnded={handleCellEditEnded}
          >
            {/* Host leaves must also be ColumnGroup when any parent group exists */}
            {hostColumns.map((hc) => {
              const { dataType, format } = columnTypeAndFormat(
                hc.ControlType,
                hc.Nbdecimal ?? resolveNbdecimal?.(hc.FieldId),
              );
              return (
              <FlexGridColumnGroup
                key={`host_${hc.Binding}`}
                name={hc.FieldId != null ? String(hc.FieldId) : ''}
                binding={hc.Binding}
                header={hc.Header}
                width={resolveWidth?.(hc.FieldId) ?? 150}
                isReadOnly={isReadOnly || hc.IsReadOnly}
                isRequired={false}
                dataType={dataType}
                format={format}
                dataMap={resolveDataMap ? resolveDataMap(hc.FieldId) ?? undefined : undefined}
              />
              );
            })}

            {leafGroupsExist && renderColumnGroupTree(columnGroups)}

            <FlexGridColumnGroup header="" binding="" width="*" isReadOnly={true} isRequired={false} />
          </FlexGrid>
        ) : (
          <FlexGrid
            ref={setGridRef}
            itemsSource={collectionView}
            isReadOnly={isReadOnly}
            allowSorting={false}
            headersVisibility="All"
            selectionMode="Cell"
            className="w-full h-full"
            style={{ height: '100%', width: '100%', border: 'none' }}
            beginningEdit={handleCellEditBeginning}
            cellEditEnding={handleCellEditEnding}
            cellEditEnded={handleCellEditEnded}
          >
            {hostColumns.map((hc) => {
              const { dataType, format } = columnTypeAndFormat(
                hc.ControlType,
                hc.Nbdecimal ?? resolveNbdecimal?.(hc.FieldId),
              );
              return (
              <FlexGridColumn
                key={`host_${hc.Binding}`}
                name={hc.FieldId != null ? String(hc.FieldId) : ''}
                binding={hc.Binding}
                header={hc.Header}
                width={resolveWidth?.(hc.FieldId) ?? 150}
                isReadOnly={isReadOnly || hc.IsReadOnly}
                isRequired={false}
                dataType={dataType}
                format={format}
                dataMap={resolveDataMap ? resolveDataMap(hc.FieldId) ?? undefined : undefined}
              />
              );
            })}

            {renderFlatValueColumns()}

            <FlexGridColumn header="" binding="" width="*" isReadOnly={true} isRequired={false} />
          </FlexGrid>
        )}
      </div>
    </div>
  );
};

// Memoized so an async fold (which re-renders the parent DataGridLayout) does not re-render this grid
// and disrupt an in-progress cell edit. All props from the parent are stabilized (useCallback / stable
// model identity), so the grid only re-renders on a real structural rebuild (new model).
export default React.memo(ChildPivotProjectionGrid);
