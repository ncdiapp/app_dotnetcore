import React, { useState, useEffect, useRef } from 'react';
import { FlexGrid, FlexGridColumn } from '@mescius/wijmo.react.grid';
import { FlexGridFilter } from '@mescius/wijmo.react.grid.filter';
import { CollectionView } from '@mescius/wijmo';
import { schemaMetadataService } from '../../webapi/schemaMetaDataSvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';

interface TableDataPreviewProps {
  isOpen: boolean;
  onClose: () => void;
  tableName: string;
  dataSourceRegisterId: number | null;
  schemaOwner?: string | null;
  recordLimit?: number;
}

const TableDataPreview: React.FC<TableDataPreviewProps> = ({
  isOpen,
  onClose,
  tableName,
  dataSourceRegisterId,
  schemaOwner = null,
  recordLimit = 100
}) => {
  const dispatch = useDispatch();
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const [tableData, setTableData] = useState<any[]>([]);
  const [columns, setColumns] = useState<string[]>([]);
  const [rowCount, setRowCount] = useState<number>(0);
  const [currentRecordLimit, setCurrentRecordLimit] = useState<number>(recordLimit);
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false);
  const flexGridRef = useRef<any>(null);
  const tableDataCVRef = useRef<CollectionView | null>(null);

  useEffect(() => {
    if (isOpen && tableName && dataSourceRegisterId) {
      loadTableData();
    }
  }, [isOpen, tableName, dataSourceRegisterId, currentRecordLimit]);

  const loadTableData = async () => {
    if (!tableName || !dataSourceRegisterId) return;

    try {
      dispatch(setIsBusy());
      const data = await schemaMetadataService.getTableData(
        tableName,
        dataSourceRegisterId,
        schemaOwner,
        currentRecordLimit
      );

      if (data) {
        const dataRowList = data.DataRowList || [];
        const columnList = data.Columns || [];

        // Handle empty data - create a blank row with all columns
        let processedData = dataRowList;
        if (processedData.length === 0 && columnList.length > 0) {
          const blankRow: any = {};
          columnList.forEach((col: string) => {
            blankRow[col] = '';
          });
          processedData = [blankRow];
        } else if (processedData.length > 0) {
          // Replace null values with empty strings for display
          processedData = processedData.map((row: any) => {
            const processedRow: any = { ...row };
            columnList.forEach((col: string) => {
              if (processedRow[col] === null) {
                processedRow[col] = '';
              }
            });
            return processedRow;
          });
        }

        setTableData(processedData);
        setColumns(columnList);
        setRowCount(dataRowList.length);

        // Create CollectionView
        const cv = new CollectionView(processedData);
        tableDataCVRef.current = cv;

        // Format date columns
        setTimeout(() => {
          if (flexGridRef.current && flexGridRef.current.columns) {
            flexGridRef.current.columns.forEach((col: any) => {
              if (col.dataType === 4) { // Date type
                col.format = 'G';
              }
            });
          }
        }, 0);
      }
    } catch (error: any) {
      showError(error.message || 'Failed to load table data');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  const handleRefresh = () => {
    loadTableData();
  };

  const toggleFullscreen = () => {
    setIsFullscreen(!isFullscreen);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div
        className={`${theme.mainContentSection} shadow-xl border flex flex-col overflow-hidden ${isFullscreen ? 'rounded-none' : 'rounded-lg'}`}
        style={isFullscreen 
          ? { width: '100vw', height: '100vh', position: 'fixed', top: 0, left: 0, zIndex: 9999 }
          : { width: '90vw', height: '90vh', maxWidth: '1400px', maxHeight: '800px' }
        }
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection} ${isFullscreen ? 'rounded-none' : 'rounded-t-lg'}`}>
          <h3 className={`text-lg font-semibold ${theme.title}`}>
            Preview: {tableName} ({rowCount} Rows)
          </h3>
          <div className="flex items-center gap-2">
            <div className="flex items-center gap-2">
              <span className="text-xs">Records Limit:</span>
              <input
                type="number"
                min="1"
                value={currentRecordLimit}
                onChange={(e) => setCurrentRecordLimit(parseInt(e.target.value) || 100)}
                className={`w-20 px-2 py-1 text-xs border rounded ${theme.inputBox}`}
              />
            </div>
            <button
              onClick={handleRefresh}
              className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500"
              title="Reload"
            >
              <i className="fa-solid fa-rotate"></i>
            </button>
            <div className="ml-4 flex items-center gap-1">
              <button
                onClick={toggleFullscreen}
                className="text-xs hover:text-gray-600 px-0.5 w-5 h-5 flex items-center justify-center"
                title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}
              >
                <i className={`fa-solid ${isFullscreen ? 'fa-compress' : 'fa-expand'}`}></i>
              </button>
              <button
                onClick={onClose}
                className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
                title="Close"
              >
                &times;
              </button>
            </div>
          </div>
        </div>

        {/* Body - FlexGrid */}
        <div className="h-1 flex-auto overflow-hidden p-2">
          {tableDataCVRef.current && (
            <FlexGrid
              ref={flexGridRef}
              itemsSource={tableDataCVRef.current}
              isReadOnly={true}
              style={{ height: '100%', width: '100%' }}
            >
              <FlexGridFilter />
              {columns.map((columnName) => (
                <FlexGridColumn
                  key={columnName}
                  header={columnName}
                  binding={columnName}
                  width={150}
                />
              ))}
            </FlexGrid>
          )}
        </div>
      </div>
    </div>
  );
};

export default TableDataPreview;

