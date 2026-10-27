import React, { useCallback, useMemo, useRef } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { AllCommunityModule, ModuleRegistry, type ColDef, type RowClickedEvent } from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { useTheme } from '../../../redux/hooks/useTheme';
import { DataRenderImage } from './DataRenderImage';
import { formatDateValue } from './dateValue';
import { cellImageKind, unwrapQuoted } from './imageValue';

ModuleRegistry.registerModules([AllCommunityModule]);

const DEFAULT_COL_DEF: ColDef = {
    resizable: true,
    sortable: true,
    filter: true,
    filterParams: { buttons: ['reset'] },
    minWidth: 60,
};

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
    fillHeight?: boolean;
}

const currencyFormatter = (p: { value?: unknown }) =>
    p.value != null
        ? `$${Number(p.value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
        : '';
const numberFormatter = (p: { value?: unknown }) =>
    p.value != null ? Number(p.value).toLocaleString() : '';
const dateFormatter = (p: { value?: unknown }) => {
    if (p.value == null || String(p.value).trim() === '') return '';
    return formatDateValue(p.value) ?? unwrapQuoted(p.value);
};

/** AG Grid implementation — kept for future use; selected when DATA_RENDER_ENGINE === 'aggrid'. */
export const DataRenderGridAg: React.FC<Props> = ({ colDefs, rowData, title, meta, onRowSelected, fillHeight }) => {
    const { theme } = useTheme();
    const gridRef = useRef<AgGridReact>(null);

    const imageFields = useMemo(() => {
        const fields = new Set<string>();
        for (const col of colDefs) {
            if (col.dataType === 'image'
                || (rowData ?? []).some(row => cellImageKind(row?.[col.field], col.field) != null))
                fields.add(col.field);
        }
        return fields;
    }, [colDefs, rowData]);

    const enrichedColDefs = useMemo<ColDef[]>(() => colDefs.map(col => {
        const enriched: ColDef = {
            field: col.field,
            headerName: col.headerName,
            width: imageFields.has(col.field) ? Math.max(col.width ?? 90, 90) : (col.width ?? 120),
            hide: col.hide,
        };
        if (imageFields.has(col.field)) {
            enriched.cellRenderer = (p: { value?: unknown }) => {
                const kind = cellImageKind(p.value, col.field);
                if (kind == null)
                    return p.value == null ? '' : String(p.value);
                return <DataRenderImage url={unwrapQuoted(p.value)} alt={col.headerName} kind={kind} />;
            };
            return enriched;
        }
        if (col.dataType === 'currency') enriched.valueFormatter = currencyFormatter;
        else if (col.dataType === 'number') enriched.valueFormatter = numberFormatter;
        else if (col.dataType === 'date'
            || (rowData ?? []).some(row => formatDateValue(row?.[col.field]) != null))
            enriched.valueFormatter = dateFormatter;
        return enriched;
    }), [colDefs, imageFields, rowData]);

    const onRowClicked = useCallback((event: RowClickedEvent) => {
        if (event.data) onRowSelected?.(event.data as Record<string, unknown>);
    }, [onRowSelected]);

    const total = meta?.total ?? rowData.length;

    const cappedH = Math.min(420, 48 + rowData.length * (imageFields.size > 0 ? 68 : 36));

    return (
        <div className={`flex flex-col gap-1 min-h-0 ${fillHeight ? 'h-full' : ''}`}>
            {(title || total != null) && (
                <div className={`flex items-center justify-between text-xs shrink-0 ${theme.label}`}>
                    <span className="font-medium">{title}</span>
                    <span>{total} row{total === 1 ? '' : 's'}</span>
                </div>
            )}
            <div
                className={`ag-theme-alpine w-full min-h-0 ${fillHeight ? 'h-1 flex-auto' : ''}`}
                style={fillHeight ? undefined : { height: Math.max(160, cappedH), minHeight: 160 }}
            >
                <AgGridReact
                    ref={gridRef}
                    rowData={rowData}
                    columnDefs={enrichedColDefs}
                    defaultColDef={DEFAULT_COL_DEF}
                    onRowClicked={onRowClicked}
                    animateRows={false}
                    rowSelection="single"
                    suppressCellFocus
                    rowHeight={imageFields.size > 0 ? 68 : undefined}
                />
            </div>
        </div>
    );
};
