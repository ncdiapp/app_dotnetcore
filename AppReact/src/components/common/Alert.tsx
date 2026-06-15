import React from 'react';
import { useTheme } from '../../redux/hooks/useTheme';

interface AlertProps {
  isOpen: boolean;
  title?: string;
  message: string;
  onClose: () => void;
  okLabel?: string;
}

const Alert: React.FC<AlertProps> = ({
  isOpen,
  title = 'Message',
  message,
  onClose,
  okLabel = 'OK'
}) => {
  const { theme } = useTheme();

  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-[10002]"
      onClick={(e) => e.stopPropagation()}
    >
      <div
        className={`${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col overflow-hidden`}
        style={{ width: '400px', maxWidth: '90vw' }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className={`flex items-center justify-between px-4 py-3 border-b ${theme.mainContentSection} rounded-t-lg`}>
          <h3 className={`text-base font-semibold ${theme.title}`}>
            {title}
          </h3>
          <button
            onClick={onClose}
            className={`p-1 ${theme.button_default} rounded-[4px] transition-all duration-200 hover:shadow-sm active:scale-95`}
          >
            <svg className={`w-4 h-4`} fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
            </svg>
          </button>
        </div>

        {/* Body */}
        <div className="px-4 py-4">
          <p className={`text-sm ${theme.label} whitespace-pre-wrap break-words`}>
            {message}
          </p>
        </div>

        {/* Footer */}
        <div className={`px-4 py-3 border-t ${theme.mainContentSection} flex justify-end`}>
          <button
            onClick={onClose}
            className={`px-4 py-2 ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95 text-sm font-medium`}
          >
            {okLabel}
          </button>
        </div>
      </div>
    </div>
  );
};

export default Alert;

