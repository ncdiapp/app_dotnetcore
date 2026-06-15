import React, { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import TransactionFolderNavigation from './TransactionFolderNavigation';

const TransactionFolderNavigationPage: React.FC = () => {
  const { param } = useParams<{ param?: string }>();
  const { theme } = useTheme();

  const transactionId = useMemo(() => {
    if (!param) return null;
    try {
      const decoded = decodeURIComponent(param);
      const obj = JSON.parse(decoded);
      return obj.transactionId ?? obj.id ?? null;
    } catch {
      return param;
    }
  }, [param]);

  if (!transactionId) {
    return <div className={`p-4 text-xs ${theme.label}`}>Transaction ID is required for folder navigation.</div>;
  }

  return (
    <div className="w-full h-full flex flex-col overflow-hidden">
      <TransactionFolderNavigation transactionId={transactionId} />
    </div>
  );
};

export default TransactionFolderNavigationPage;
