import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import TransactionDataLoadManagement from './TransactionDataLoadManagement';

interface TransactionUnitDataLoadDialogProps {
  isOpen: boolean;
  transactionId: number | null;
  transactionUnitId: number | null;
  transactionName?: string | null;
  onClose: () => void;
}

const TransactionUnitDataLoadDialog: React.FC<TransactionUnitDataLoadDialogProps> = ({
  isOpen,
  transactionId,
  transactionUnitId,
  transactionName,
  onClose,
}) => {
  const { theme } = useTheme();

  if (!isOpen || !transactionId || !transactionUnitId) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[10000] flex items-center justify-center bg-black bg-opacity-50">
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '90vw', height: '90vh', maxWidth: 1400, maxHeight: 900 }}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between px-4 py-2 border-b ${theme.mainContentSection}`}>
          <div className={`text-sm font-semibold ${theme.title}`}>
            Data Load Settings: {transactionName || 'Unit'}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-2xl hover:text-gray-600 px-2 w-9 h-9 flex items-center justify-center"
            title="Close"
          >
            &times;
          </button>
        </div>

        <div className="flex-1 min-h-0 overflow-hidden">
          <TransactionDataLoadManagement
            transactionId={transactionId}
            transactionUnitId={transactionUnitId}
            transactionName={transactionName}
          />
        </div>
      </div>
    </div>
  );
};

export default TransactionUnitDataLoadDialog;

