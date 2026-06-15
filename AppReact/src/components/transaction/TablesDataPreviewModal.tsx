/**
 * TablesDataPreviewModal – multiple tables data preview in a popup (Angular MultipleMetaDataPreview).
 * Opens as a modal with tabs (one per table); each tab shows that table's data grid.
 */

import React, { useState, useEffect, useRef } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';

export type TablePreviewItem = {
  tableName: string;
  dataSourceId: number | null;
  schemaOwner?: string | null;
};

interface TablesDataPreviewModalProps {
  isOpen: boolean;
  onClose: () => void;
  /** List of tables to preview (name + datasource id). Order = tab order. */
  tables: TablePreviewItem[];
}

const DEFAULT_RECORD_LIMIT = 100;

const TablesDataPreviewModal: React.FC<TablesDataPreviewModalProps> = ({
  isOpen,
  onClose,
  tables,
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [tableData, setTableData] = useState<any[]>([]);
  const [columns, setColumns] = useState<string[]>([]);
  const [rowCount, setRowCount] = useState(0);
  const [recordLimit, setRecordLimit] = useState(DEFAULT_RECORD_LIMIT);
  const [tableDataCV, setTableDataCV] = useState<CollectionView | null>(null);
  const flexGridRef = useRef<any>(null);

  const currentTable = tables[selectedIndex] ?? null;

  useEffect(() => {
    if (!isOpen || tables.length === 0) return;
    setSelectedIndex(0);
    setTableDataCV(null);
    setTableData([]);
    setColumns([]);
    setRowCount(0);
  }, [isOpen, tables.length]);

  useEffect(() => {
    if (!isOpen || !currentTable?.tableName || currentTable.dataSourceId == null) {
      setTableDataCV(null);
      setTableData([]);
      setColumns([]);
      setRowCount(0);
      return;
    }
    let cancelled = false;

    const load = async () => {
      try {
        dispatch(setIsBusy());
        const data = await schemaMetadataService.getTableData(
          currentTable.tableName,
          currentTable.dataSourceId,
          currentTable.schemaOwner ?? null,
          recordLimit
        );
        if (cancelled) return;
        if (data) {
          const dataRowList = data.DataRowList || [];
          const columnList = data.Columns || [];
          let processedData = dataRowList;
          if (processedData.length === 0 && columnList.length > 0) {
            const blankRow: any = {};
            columnList.forEach((col: string) => {
              blankRow[col] = '';
            });
            processedData = [blankRow];
          } else if (processedData.length > 0) {
            processedData = processedData.map((row: any) => {
              const processedRow: any = { ...row };
              columnList.forEach((col: string) => {
                if (processedRow[col] === null) processedRow[col] = '';
              });
              return processedRow;
            });
          }
          setTableData(processedData);
          setColumns(columnList);
          setRowCount(dataRowList.length);
          setTableDataCV(new CollectionView(processedData));
        }
      } catch (error: any) {
        if (!cancelled) showError(error?.message || 'Failed to load table data');
      } finally {
        if (!cancelled) dispatch(setIsNotBusy());
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, [isOpen, selectedIndex, currentTable?.tableName, currentTable?.dataSourceId, currentTable?.schemaOwner, recordLimit, dispatch, showError]);

  const handleRefresh = () => {
    if (!currentTable) return;
    setTableDataCV(null);
    setTableData([]);
    setColumns([]);
    setRowCount(0);
    const load = async () => {
      try {
        dispatch(setIsBusy());
        const data = await schemaMetadataService.getTableData(
          currentTable.tableName,
          currentTable.dataSourceId,
          currentTable.schemaOwner ?? null,
          recordLimit
        );
        if (data) {
          const dataRowList = data.DataRowList || [];
          const columnList = data.Columns || [];
          let processedData = dataRowList;
          if (processedData.length === 0 && columnList.length > 0) {
            const blankRow: any = {};
            columnList.forEach((col: string) => {
              blankRow[col] = '';
            });
            processedData = [blankRow];
          } else if (processedData.length > 0) {
            processedData = processedData.map((row: any) => {
              const processedRow: any = { ...row };
              columnList.forEach((col: string) => {
                if (processedRow[col] === null) processedRow[col] = '';
              });
              return processedRow;
            });
          }
          setTableData(processedData);
          setColumns(columnList);
          setRowCount(dataRowList.length);
          setTableDataCV(new CollectionView(processedData));
        }
      } catch (error: any) {
        showError(error?.message || 'Failed to load table data');
      } finally {
        dispatch(setIsNotBusy());
      }
    };
    load();
  };

  const handleTabSelect = (index: number) => {
    setSelectedIndex(index);
    setTableDataCV(null);
    setTableData([]);
    setColumns([]);
    setRowCount(0);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div
        className={`${theme.mainContentSection} shadow-xl border flex flex-col overflow-hidden rounded-lg`}
        style={{ width: '90vw', height: '90vh', maxWidth: '1400px', maxHeight: '800px' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-2 border-b flex-shrink-0 ${theme.mainContentSection} rounded-t-lg`}>
          <h3 className={`text-lg font-semibold ${theme.title}`}>
            Tables Data Preview
            {currentTable && (
              <span className="font-normal text-gray-500 ml-2">
                {currentTable.tableName} ({rowCount} rows)
              </span>
            )}
          </h3>
          <div className="flex items-center gap-2">
            {currentTable && (
              <>
                <span className="text-xs">Records Limit:</span>
                <input
                  type="number"
                  min={1}
                  value={recordLimit}
                  onChange={(e) => setRecordLimit(parseInt(e.target.value, 10) || DEFAULT_RECORD_LIMIT)}
                  className={`w-20 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
                />
                <button
                  type="button"
                  onClick={handleRefresh}
                  className={`px-2 py-1 text-xs rounded ${theme.button_default}`}
                  title="Refresh"
                >
                  <i className="fa-solid fa-rotate mr-1" aria-hidden /> Refresh
                </button>
              </>
            )}
            <button
              type="button"
              onClick={onClose}
              className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
              title="Close"
            >
              &times;
            </button>
          </div>
        </div>

        {/* Tabs */}
        <div className={`flex flex-wrap gap-1 px-2 py-1 border-b flex-shrink-0 ${theme.mainContentSection}`}>
          {tables.map((t, i) => (
            <button
              key={t.tableName}
              type="button"
              onClick={() => handleTabSelect(i)}
              className={`px-3 py-1.5 text-xs rounded-t min-w-[120px] text-center ${
                selectedIndex === i
                  ? theme.button_default + ' ring-1 ring-inset'
                  : 'bg-transparent hover:bg-gray-200'
              }`}
            >
              {t.tableName}
            </button>
          ))}
        </div>

        {/* Body - Grid for selected table */}
        <div className="h-1 flex-auto overflow-hidden p-2 min-h-0">
          {tableDataCV ? (
            <FlexGrid
              ref={flexGridRef}
              itemsSource={tableDataCV}
              isReadOnly={true}
              style={{ height: '100%', width: '100%' }}
            >
              <FlexGridFilter />
              {columns.map((col) => (
                <FlexGridColumn key={col} header={col} binding={col} width={150} />
              ))}
            </FlexGrid>
          ) : currentTable ? (
            <div className="flex items-center justify-center h-full text-gray-500 text-sm">
              Loading…
            </div>
          ) : tables.length === 0 ? (
            <div className="flex items-center justify-center h-full text-gray-500 text-sm">
              No tables to preview.
            </div>
          ) : null}
        </div>
      </div>
    </div>
  );
};

export default TablesDataPreviewModal;
