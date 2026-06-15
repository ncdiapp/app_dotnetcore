import React from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';

export const BusyLoader: React.FC = () => {
  const isBusy = useSelector((state: RootState) => state.busyLoader.isBusy);

  if (!isBusy) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-10 z-[9999] flex items-center justify-center pointer-events-auto">
      <div className="relative">
        <div className="w-16 h-16 border-t-4 border-blue-500 border-solid rounded-full animate-spin"></div>
      </div>
    </div>
  );
}; 