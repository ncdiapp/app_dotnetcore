import React, { useState, useEffect, useRef } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import { useErrorMessage } from '../../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { schemaMetadataService } from '../../../webapi/schemaMetaDataSvc';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView, SortDescription } from '@mescius/wijmo';
import '@mescius/wijmo.styles/wijmo.css';

interface TableColumnSelectorDialogProps {
    isOpen: boolean;
    tableName: string;
    schemaOwner: string | null;
    dataSourceRegisterId: number | null;
    lockedColumnNames: string[]; // Columns that are already added and should be locked
    onClose: () => void;
    onSelect: (selectedColumns: any[]) => void;
}

const TableColumnSelectorDialog: React.FC<TableColumnSelectorDialogProps> = ({
    isOpen,
    tableName,
    schemaOwner,
    dataSourceRegisterId,
    lockedColumnNames,
    onSelect,
    onClose
}) => {
    const { theme } = useTheme();
    const { showError } = useErrorMessage();
    const dispatch = useDispatch();
    const [columns, setColumns] = useState<any[]>([]);
    const [columnCollectionView, setColumnCollectionView] = useState<CollectionView | null>(null);
    const [loading, setLoading] = useState(false);
    const [selectionChanged, setSelectionChanged] = useState(0); // Force re-render on selection change
    const flexGridRef = useRef<any>(null);

    // Load table columns
    useEffect(() => {
        const loadColumns = async () => {
            if (!isOpen || !tableName || dataSourceRegisterId === null) return;

            setLoading(true);
            dispatch(setIsBusy());
            try {
                const tableData = await schemaMetadataService.getOneDatabaseTableSchema(
                    tableName,
                    dataSourceRegisterId,
                    schemaOwner
                );

                if (tableData && tableData.Columns) {
                    // Filter out columns that are already added (don't show them)
                    const availableColumns = tableData.Columns.filter((col: any) => 
                        !lockedColumnNames.includes(col.Name)
                    );
                    setColumns(availableColumns);
                } else {
                    setColumns([]);
                }
            } catch (error: any) {
                showError(error.message || 'Failed to load table columns');
                setColumns([]);
            } finally {
                setLoading(false);
                dispatch(setIsNotBusy());
            }
        };

        loadColumns();
    }, [isOpen, tableName, schemaOwner, dataSourceRegisterId, lockedColumnNames, dispatch, showError]);

    // Initialize column collection view
    useEffect(() => {
        if (columns.length > 0) {
            const cv = new CollectionView(columns);
            cv.sortDescriptions.push(new SortDescription('Name', true));
            setColumnCollectionView(cv);
        } else {
            setColumnCollectionView(null);
        }
    }, [columns]);

    const handleSelectionChanged = () => {
        // Force re-render to update check icons
        setSelectionChanged(prev => prev + 1);
    };

    const handleOk = () => {
        if (!flexGridRef.current) return;

        const flex = flexGridRef.current.control;
        if (!flex) return;

        // Get selected rows (matching AngularJS logic)
        const selected: any[] = [];
        for (let i = 0; i < flex.rows.length; i++) {
            const row = flex.rows[i];
            if (row.isSelected && row.dataItem) {
                selected.push(row.dataItem);
            }
        }

        if (selected.length === 0) {
            showError('Please select at least one column');
            return;
        }

        // Return selected column data
        const columnData = selected;

        onSelect(columnData);
    };

    const handleBackdropClick = (e: React.MouseEvent) => {
        e.stopPropagation();
    };

    if (!isOpen) return null;

    return (
        <div
            className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10000]"
            onClick={handleBackdropClick}
        >
            <div
                className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
                style={{ width: '400px', height: '600px' }}
                onClick={(e) => e.stopPropagation()}
            >
                {/* Header */}
                <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
                    <h3 className={`text-base font-semibold ${theme.title}`}>
                        Database Table Column Selector
                    </h3>
                    <button
                        onClick={onClose}
                        className={`p-1 ${theme.button_default} rounded-[4px] transition-all duration-200 hover:shadow-sm active:scale-95`}
                    >
                        <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                            <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                        </svg>
                    </button>
                </div>

                {/* Columns Grid */}
                <div className="flex-1 overflow-hidden" style={{ height: '500px' }}>
                    {loading ? (
                        <div className={`text-center py-8 ${theme.label}`}>
                            <i className="fa fa-spinner fa-spin mr-2"></i>
                            Loading columns...
                        </div>
                    ) : columnCollectionView ? (
                        <FlexGrid
                            ref={flexGridRef}
                            itemsSource={columnCollectionView}
                            isReadOnly={true}
                            selectionMode="ListBox"
                            style={{ height: '100%', border: 'none' }}
                            selectionChanged={handleSelectionChanged}
                            formatItem={(s: any, e: any) => {
                                // Format row header to show check icon for selected rows
                                if (e.panel.cellType === 0) { // RowHeader
                                    if (e.row.isSelected) {
                                        // Show check icon for selected rows
                                        e.cell.innerHTML = '<div style="padding:0px 5px;"><i class="fa fa-check" style="color:#808080;"></i></div>';
                                    } else {
                                        e.cell.innerHTML = '<div style="padding:0px 5px;"></div>';
                                    }
                                }
                            }}
                        >
                            <FlexGridFilter />
                            <FlexGridColumn binding="Name" header="Column Name" width="*" />
                        </FlexGrid>
                    ) : (
                        <div className={`text-center py-8 ${theme.label}`}>
                            No columns available
                        </div>
                    )}
                </div>

                {/* Footer */}
                <div className={`px-4 py-3 border-t ${theme.mainContentSection} flex justify-end gap-2`}>
                    <button
                        onClick={onClose}
                        className={`px-4 py-2 ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95 text-sm font-medium`}
                        style={{ width: '80px' }}
                    >
                        Close
                    </button>
                    <button
                        onClick={handleOk}
                        className={`px-4 py-2 ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95 text-sm font-medium`}
                        style={{ width: '80px' }}
                    >
                        Ok
                    </button>
                </div>
            </div>
        </div>
    );
};

export default TableColumnSelectorDialog;
