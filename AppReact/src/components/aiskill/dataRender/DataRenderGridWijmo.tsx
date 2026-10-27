import React, { useEffect, useMemo, useRef, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { FlexGrid, FlexGridCellTemplate, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import '@mescius/wijmo.styles/wijmo.css';
import FlexGridAddOn from '../../common/FlexGridAddOn';
import { useTheme } from '../../../redux/hooks/useTheme';
import './agent-result-grid.css';
import { FormattedResultCell, normalizeCellFormat } from './cellFormat';
import { DataRenderImage } from './DataRenderImage';
import { formatDateValue } from './dateValue';
import { cellImageKind, isImageColumnName, unwrapQuoted } from './imageValue';

const MIN_COL_WIDTH = 150;

type ColInput = {
    field: string;
    headerName: string;
    width?: number;
    dataType?: string;
    hide?: boolean;
};

interface Props {
    colDefs: ColInput[];
    rowData: Record<string, unknown>[];
    title?: string;
    meta?: { total?: number };
    onRowSelected?: (row: Record<string, unknown>) => void;
    /** Single-control Result: grid fills remaining chrome height (no ~420px cap). */
    fillHeight?: boolean;
}

function wijmoFormat(dataType?: string): string | undefined {
    if (dataType === 'currency') return 'c2';
    if (dataType === 'number') return 'n0';
    // Dates use a cell template (ISO strings → locale date); Wijmo 'd' needs Date objects.
    return undefined;
}

/** Default Wijmo FlexGrid implementation for data_render. */
const DataRenderGridWijmoInner: React.FC<Props> = ({ colDefs, rowData, title, meta, onRowSelected, fillHeight }) => {
    const { theme } = useTheme();
    const flexGridRef = useRef<any>(null);
    const hostRef = useRef<HTMLDivElement>(null);
    const [cv] = useState(() => new CollectionView<any>([]));
    const dataSigRef = useRef<string>('');

    // Only rebind when row content actually changes — parent chat re-renders must not refresh Wijmo.
    useEffect(() => {
        const next = Array.isArray(rowData) ? rowData : [];
        const sig = `${next.length}:${next[0] ? JSON.stringify(next[0]) : ''}:${next.length > 1 ? JSON.stringify(next[next.length - 1]) : ''}`;
        if (sig === dataSigRef.current && cv.sourceCollection === next) return;
        if (sig === dataSigRef.current && Array.isArray(cv.sourceCollection) && (cv.sourceCollection as any[]).length === next.length)
            return;
        dataSigRef.current = sig;
        cv.sourceCollection = next;
        cv.refresh();
    }, [cv, rowData]);

    const visibleCols = useMemo(
        () => (colDefs ?? []).filter(c => c.field && !c.hide),
        [colDefs],
    );
    const filterColumns = useMemo(
        () => visibleCols.map(c => c.field),
        [visibleCols],
    );
    // Prefer AI polish format=image; fall back for old chats / missing polish (Sketch URL → thumbnail).
    const imageFields = useMemo(() => {
        const fields = new Set<string>();
        for (const col of visibleCols) {
            if (normalizeCellFormat(col.dataType) === 'image') {
                fields.add(col.field);
                continue;
            }
            // Old Result / reopen: polish format often missing — detect like AG Grid + print (field name + URL).
            if (isImageColumnName(col.field)
                && (rowData ?? []).some(row => cellImageKind(row?.[col.field], col.field) != null)) {
                fields.add(col.field);
            }
        }
        return fields;
    }, [visibleCols, rowData]);

    const formatFields = useMemo(() => {
        const map = new Map<string, string>();
        for (const col of visibleCols) {
            const kind = normalizeCellFormat(col.dataType);
            if (kind === 'delta' || kind === 'percent' || kind === 'date')
                map.set(col.field, kind);
        }
        return map;
    }, [visibleCols]);

    const total = meta?.total ?? rowData.length;
    const rowHeight = imageFields.size > 0 ? 64 : 28;
    // Stacked page (grid+cards): size by rows, cap so Card view still has room below.
    const height = fillHeight ? undefined : Math.min(480, 48 + Math.max(rowData.length, 1) * rowHeight);

    useEffect(() => {
        const flex = flexGridRef.current?.control;
        if (flex?.rows) flex.rows.defaultSize = rowHeight;
    }, [rowHeight]);

    // Keep Wijmo sized when the Result pane fills remaining height.
    useEffect(() => {
        if (!fillHeight || !hostRef.current) return;
        const el = hostRef.current;
        const sync = () => {
            const flex = flexGridRef.current?.control;
            if (!flex) return;
            try { flex.invalidate(); } catch { /* ignore */ }
        };
        const ro = typeof ResizeObserver !== 'undefined' ? new ResizeObserver(sync) : null;
        ro?.observe(el);
        sync();
        return () => ro?.disconnect();
    }, [fillHeight]);

    const onSelectionChanged = (s: any) => {
        const flex = s?.control ?? s;
        const row = flex?.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (item && onRowSelected) onRowSelected(item as Record<string, unknown>);
    };

    return (
        <div className={`flex flex-col gap-1 min-h-0 ${fillHeight ? 'h-full' : ''}`}>
            <div className={`flex items-center justify-between gap-2 text-xs shrink-0 ${theme.label}`}>
                <div className="flex items-center gap-2 min-w-0">
                    {title && <span className="font-medium truncate">{title}</span>}
                    <FlexGridAddOn
                        gridRef={flexGridRef}
                        title="Freeze / Show / Hide columns"
                    />
                </div>
                <span className="shrink-0">{total} row{total === 1 ? '' : 's'}</span>
            </div>
            <div
                ref={hostRef}
                className={`agent-result-grid w-full min-h-0 ${fillHeight ? 'h-1 flex-auto' : ''}`}
                style={fillHeight ? undefined : { height: Math.max(160, height ?? 160), minHeight: 160 }}
            >
                <FlexGrid
                    ref={flexGridRef}
                    className="w-full h-full"
                    style={{ width: '100%', height: '100%' }}
                    itemsSource={cv}
                    isReadOnly
                    headersVisibility="Column"
                    selectionMode="Row"
                    selectionChanged={onSelectionChanged}
                >
                    {visibleCols.map(col => {
                        const fmt = formatFields.get(col.field);
                        const useImage = imageFields.has(col.field);
                        const useDate = !useImage && fmt === 'date';
                        const useDeltaOrPct = !useImage && (fmt === 'delta' || fmt === 'percent');
                        return (
                        <FlexGridColumn
                            key={col.field}
                            header={col.headerName || col.field}
                            binding={col.field}
                            width={useImage ? Math.max(col.width ?? 90, 90) : Math.max(col.width ?? MIN_COL_WIDTH, MIN_COL_WIDTH)}
                            minWidth={useImage ? 90 : MIN_COL_WIDTH}
                            format={wijmoFormat(col.dataType)}
                        >
                            {useImage && (
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => {
                                        const value = cell.item?.[col.field];
                                        const kind = cellImageKind(value, col.field, { trustFormat: true });
                                        if (kind == null)
                                            return <span>{value == null ? '' : String(value)}</span>;
                                        return (
                                            <DataRenderImage
                                                url={unwrapQuoted(value)}
                                                alt={col.headerName || col.field}
                                                kind={kind}
                                            />
                                        );
                                    }}
                                />
                            )}
                            {!useImage && useDeltaOrPct && (
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => (
                                        <FormattedResultCell
                                            value={cell.item?.[col.field]}
                                            format={fmt}
                                        />
                                    )}
                                />
                            )}
                            {!useImage && useDate && (
                                <FlexGridCellTemplate
                                    cellType="Cell"
                                    template={(cell: any) => {
                                        const value = cell.item?.[col.field];
                                        if (value == null || String(value).trim() === '') return <span />;
                                        const formatted = formatDateValue(value);
                                        return (
                                            <FormattedResultCell
                                                value={value}
                                                format="date"
                                                dateText={formatted ?? unwrapQuoted(value)}
                                            />
                                        );
                                    }}
                                />
                            )}
                        </FlexGridColumn>
                        );
                    })}
                    <FlexGridColumn header="" binding="" width="*" />
                    <FlexGridFilter filterColumns={filterColumns} />
                </FlexGrid>
            </div>
        </div>
    );
};

function sameColDefs(a: ColInput[] | undefined, b: ColInput[] | undefined): boolean {
    if (a === b) return true;
    if (!a || !b || a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
        const x = a[i], y = b[i];
        if (x.field !== y.field || x.headerName !== y.headerName || x.width !== y.width
            || x.dataType !== y.dataType || !!x.hide !== !!y.hide) return false;
    }
    return true;
}

/** Skip re-render when chat parent churns but grid data is unchanged. */
export const DataRenderGridWijmo = React.memo(DataRenderGridWijmoInner, (prev, next) => (
    prev.fillHeight === next.fillHeight
    && prev.title === next.title
    && prev.meta?.total === next.meta?.total
    && prev.rowData === next.rowData
    && sameColDefs(prev.colDefs, next.colDefs)
    && prev.onRowSelected === next.onRowSelected
));
