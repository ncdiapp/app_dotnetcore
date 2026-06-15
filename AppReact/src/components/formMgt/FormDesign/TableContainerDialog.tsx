import React, { useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

interface TableContainerDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (rows: number, columns: number) => void;
}

const TableContainerDialog: React.FC<TableContainerDialogProps> = ({
  isOpen,
  onClose,
  onConfirm
}) => {
  const { theme } = useTheme();
  const [rows, setRows] = useState<number>(1);
  const [columns, setColumns] = useState<number>(2);

  if (!isOpen) return null;

  const handleBackdropClick = (e: React.MouseEvent) => {
    e.stopPropagation();
  };

  const handleOk = () => {
    if (rows > 0 && columns > 0) {
      onConfirm(rows, columns);
      onClose();
    }
  };

  const handleIncrementRows = () => {
    setRows(prev => prev + 1);
  };

  const handleDecrementRows = () => {
    if (rows > 1) {
      setRows(prev => prev - 1);
    }
  };

  const handleIncrementColumns = () => {
    setColumns(prev => prev + 1);
  };

  const handleDecrementColumns = () => {
    if (columns > 1) {
      setColumns(prev => prev - 1);
    }
  };

  return (
    <div
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10000]"
      onClick={handleBackdropClick}
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '240px' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection}`}>
          <h3 className={`text-sm font-semibold ${theme.title}`}>
            Set Rows And Columns
          </h3>
          <button
            onClick={onClose}
            className={`p-1 ${theme.button_default} rounded transition-all duration-200 hover:bg-opacity-80 active:scale-95`}
            title="Close"
          >
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <div className="px-4 py-4 space-y-4">
          {/* Columns */}
          <div className="flex items-center justify-between">
            <label className={`text-xs ${theme.label} w-20`}>
              Columns
            </label>
            <div className="flex items-center gap-2">
              <button
                onClick={handleDecrementColumns}
                disabled={columns <= 1}
                className={`w-[24px] h-[24px] flex items-center justify-center ${theme.button_default} border rounded transition-all duration-200 hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed`}
                title="Decrease columns"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
                </svg>
              </button>
              <input
                type="number"
                min="1"
                max="24"
                value={columns}
                onChange={(e) => {
                  const value = parseInt(e.target.value) || 1;
                  if (value >= 1 && value <= 24) {
                    setColumns(value);
                  }
                }}
                className={`w-16 h-[24px] px-2 py-1 text-center text-xs border rounded ${theme.inputBox} focus:outline-none focus:ring-1 focus:ring-blue-500`}
              />
              <button
                onClick={handleIncrementColumns}
                disabled={columns >= 24}
                className={`w-[24px] h-[24px] flex items-center justify-center ${theme.button_default} border rounded transition-all duration-200 hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed`}
                title="Increase columns"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
              </button>
            </div>
          </div>

          {/* Rows */}
          <div className="flex items-center justify-between">
            <label className={`text-xs ${theme.label} w-20`}>
              Rows
            </label>
            <div className="flex items-center gap-2">
              <button
                onClick={handleDecrementRows}
                disabled={rows <= 1}
                className={`w-[24px] h-[24px] flex items-center justify-center ${theme.button_default} border rounded transition-all duration-200 hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed`}
                title="Decrease rows"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 12H4" />
                </svg>
              </button>
              <input
                type="number"
                min="1"
                max="100"
                value={rows}
                onChange={(e) => {
                  const value = parseInt(e.target.value) || 1;
                  if (value >= 1 && value <= 100) {
                    setRows(value);
                  }
                }}
                className={`w-16 h-[24px] px-2 py-1 text-center text-xs border rounded ${theme.inputBox} focus:outline-none focus:ring-1 focus:ring-blue-500`}
              />
              <button
                onClick={handleIncrementRows}
                disabled={rows >= 100}
                className={`w-[24px] h-[24px] flex items-center justify-center ${theme.button_default} border rounded transition-all duration-200 hover:shadow-sm active:scale-95 disabled:opacity-50 disabled:cursor-not-allowed`}
                title="Increase rows"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                </svg>
              </button>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className={`flex justify-end gap-2 px-4 py-3 border-t ${theme.mainContentSection}`}>
          <button
            onClick={onClose}
            className={`px-4 py-1.5 text-xs ${theme.button_default} border rounded hover:shadow-sm transition-all duration-200 active:scale-95`}
          >
            Cancel
          </button>
          <button
            onClick={handleOk}
            className="px-4 py-1.5 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 hover:shadow-sm transition-all duration-200 active:scale-95"
          >
            OK
          </button>
        </div>
      </div>
    </div>
  );
};

export default TableContainerDialog;
