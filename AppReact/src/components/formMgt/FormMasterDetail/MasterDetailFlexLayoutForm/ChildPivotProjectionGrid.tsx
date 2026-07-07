import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FlexGrid, FlexGridColumn, FlexGridCellTemplate } from '@mescius/wijmo.react.grid';
import { CollectionView } from '@mescius/wijmo';
import { DataMap } from '@mescius/wijmo.grid';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { useEnumValues } from '../../../../hooks/useEnumDictionary';
import { fileThumbnailUrl } from '../../../../webapi/fileEndpoints';
import { ChildPivotProjectionModel } from './childPivotProjectionHelper';

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
 */
const ChildPivotProjectionGrid: React.FC<ChildPivotProjectionGridProps> = ({
  model,
  isReadOnly = false,
  gridRef,
  resolveDataMap,
  resolveWidth,
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
  const wideRows = useMemo(() => model?.WideRows ?? [], [model]);

  const [collectionView] = useState<CollectionView<any>>(() => new CollectionView<any>([]));

  useEffect(() => {
    (collectionView as any).sourceCollection = wideRows;
    collectionView.sortDescriptions.clear();
    collectionView.refresh();
  }, [wideRows, collectionView]);

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

  // Resolve display text on each render so headers update when formStructure entity data loads.
  const groupHeaderLabel = useCallback(
    (group: { Header: string; ColValue?: any }): string => {
      if (model?.ColumnSourceFieldId != null && group.ColValue != null) {
        const sourceDataMap = resolveDataMap?.(model.ColumnSourceFieldId) ?? null;
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

  return (
    <div className="w-full h-full min-h-0 overflow-hidden flex flex-col">
      {nothingVisible && (
        <div className={`shrink-0 px-3 py-1 text-xs font-semibold text-amber-600`}>
          All Pivot Value fields are hidden (IsVisible = false). Mark at least one as visible.
        </div>
      )}
      <div className="h-full w-full">
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
        {/* Host (child) descriptor columns */}
        {hostColumns.map((hc) => (
          <FlexGridColumn
            key={`host_${hc.Binding}`}
            name={hc.FieldId != null ? String(hc.FieldId) : ''}
            binding={hc.Binding}
            header={hc.Header}
            width={resolveWidth?.(hc.FieldId) ?? 150}
            isReadOnly={isReadOnly || hc.IsReadOnly}
            isRequired={false}
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
            const isImageColumn = isImageControlType(leaf.ControlType);
            const colWidth = resolveWidth?.(leaf.FieldId) ?? (isImageColumn ? 130 : 110);
            const colReadOnly = isReadOnly || isImageColumn;

            if (isImageColumn) {
              const binding = leaf.Binding;
              const dbFieldName = leaf.DataBaseFieldName ?? '';
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
                </FlexGridColumn>
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
                dataType={dataTypeFor(leaf.ControlType)}
                format={formatFor(leaf.ControlType)}
                dataMap={resolveDataMap ? resolveDataMap(leaf.FieldId) ?? undefined : undefined}
              />
            );
          });
        })}

        {/* Spacer */}
        <FlexGridColumn header="" binding="" width="*" isReadOnly={true} isRequired={false} />
      </FlexGrid>
      </div>
    </div>
  );
};

// Memoized so an async fold (which re-renders the parent DataGridLayout) does not re-render this grid
// and disrupt an in-progress cell edit. All props from the parent are stabilized (useCallback / stable
// model identity), so the grid only re-renders on a real structural rebuild (new model).
export default React.memo(ChildPivotProjectionGrid);
