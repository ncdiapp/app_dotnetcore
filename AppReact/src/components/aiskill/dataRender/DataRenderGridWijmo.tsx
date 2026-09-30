import React, { useEffect, useMemo, useState } from 'react';
import { CollectionView } from '@mescius/wijmo';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import '@mescius/wijmo.styles/wijmo.css';
import { useTheme } from '../../../redux/hooks/useTheme';

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

function wijmoFormat(dataType?: string): string | undefined {
    if (dataType === 'currency') return 'c2';
    if (dataType === 'number') return 'n0';
    if (dataType === 'date') return 'd';
    return undefined;
}

/** Default Wijmo FlexGrid implementation for data_render. */
export const DataRenderGridWijmo: React.FC<Props> = ({ colDefs, rowData, title, meta, onRowSelected }) => {
    const { theme } = useTheme();
    const [cv] = useState(() => new CollectionView<any>([]));

    useEffect(() => {
        cv.sourceCollection = Array.isArray(rowData) ? rowData : [];
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

    const total = meta?.total ?? rowData.length;
    const height = Math.min(360, 48 + Math.max(rowData.length, 1) * 28);

    const onSelectionChanged = (s: any) => {
        const flex = s?.control ?? s;
        const row = flex?.selection?.row;
        if (row == null || row < 0) return;
        const item = flex.rows?.[row]?.dataItem;
        if (item && onRowSelected) onRowSelected(item as Record<string, unknown>);
    };

    return (
        <div className="flex flex-col gap-1">
            {(title || total != null) && (
                <div className={`flex items-center justify-between text-xs ${theme.label}`}>
                    <span className="font-medium">{title}</span>
                    <span>{total} row{total === 1 ? '' : 's'}</span>
                </div>
            )}
            <div className="w-full" style={{ height: Math.max(160, height), minHeight: 160 }}>
                <FlexGrid
                    className="w-full h-full"
                    style={{ width: '100%', height: '100%' }}
                    itemsSource={cv}
                    isReadOnly
                    headersVisibility="Column"
                    selectionMode="Row"
                    selectionChanged={onSelectionChanged}
                >
                    {visibleCols.map(col => (
                        <FlexGridColumn
                            key={col.field}
                            header={col.headerName || col.field}
                            binding={col.field}
                            width={col.width ?? 120}
                            format={wijmoFormat(col.dataType)}
                        />
                    ))}
                    <FlexGridColumn header="" binding="" width="*" />
                    <FlexGridFilter filterColumns={filterColumns} />
                </FlexGrid>
            </div>
        </div>
    );
};
