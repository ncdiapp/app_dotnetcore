import React, { useCallback, useMemo, useRef } from 'react';
import { AgGridReact } from 'ag-grid-react';
import { AllCommunityModule, ModuleRegistry, type ColDef, type RowClickedEvent } from 'ag-grid-community';
import 'ag-grid-community/styles/ag-grid.css';
import 'ag-grid-community/styles/ag-theme-alpine.css';
import { useTheme } from '../../../redux/hooks/useTheme';

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
}

const currencyFormatter = (p: { value?: unknown }) =>
    p.value != null
        ? `$${Number(p.value).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
        : '';
const numberFormatter = (p: { value?: unknown }) =>
    p.value != null ? Number(p.value).toLocaleString() : '';
const dateFormatter = (p: { value?: unknown }) =>
    p.value
        ? new Date(String(p.value)).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
        : '';

export const DataRenderGrid: React.FC<Props> = ({ colDefs, rowData, title, meta, onRowSelected }) => {
    const { theme } = useTheme();
    const gridRef = useRef<AgGridReact>(null);

    const enrichedColDefs = useMemo<ColDef[]>(() => colDefs.map(col => {
        const enriched: ColDef = {
            field: col.field,
            headerName: col.headerName,
            width: col.width ?? 120,
            hide: col.hide,
        };
        if (col.dataType === 'currency') enriched.valueFormatter = currencyFormatter;
        else if (col.dataType === 'number') enriched.valueFormatter = numberFormatter;
        else if (col.dataType === 'date') enriched.valueFormatter = dateFormatter;
        return enriched;
    }), [colDefs]);

    const onRowClicked = useCallback((event: RowClickedEvent) => {
        if (event.data) onRowSelected?.(event.data as Record<string, unknown>);
    }, [onRowSelected]);

    const total = meta?.total ?? rowData.length;

    return (
        <div className="flex flex-col gap-1">
            {(title || total != null) && (
                <div className={`flex items-center justify-between text-xs ${theme.label}`}>
                    <span className="font-medium">{title}</span>
                    <span>{total} row{total === 1 ? '' : 's'}</span>
                </div>
            )}
            <div className="ag-theme-alpine w-full" style={{ height: Math.min(360, 48 + rowData.length * 36), minHeight: 160 }}>
                <AgGridReact
                    ref={gridRef}
                    rowData={rowData}
                    columnDefs={enrichedColDefs}
                    defaultColDef={DEFAULT_COL_DEF}
                    onRowClicked={onRowClicked}
                    animateRows={false}
                    rowSelection="single"
                    suppressCellFocus
                />
            </div>
        </div>
    );
};
