import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { RootState } from '../../redux/store';
import { clearAllMessages, removeMessage, setPopupOpen, MessageType } from '../../redux/features/ui/feedback/errorMessageSlice';
import { useTheme } from '../../redux/hooks/useTheme';

const ErrorMessagePopup: React.FC = () => {
  const dispatch = useDispatch();
  const { messages, isPopupOpen } = useSelector((state: RootState) => state.errorMessage);
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { t, theme } = useTheme();

  if (!isPopupOpen || messages.length === 0) {
    return null;
  }

  const formatTime = (timestamp: number) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString();
  };

  const getMessageIcon = (type?: MessageType) => {
    switch (type) {
      case MessageType.Error:
        return (
          <svg className="w-3.5 h-3.5 text-red-500" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
          </svg>
        );
      case MessageType.Warning:
        return (
          <svg className="w-3.5 h-3.5 text-yellow-500" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
        );
      default:
        return (
          <svg className="w-3.5 h-3.5 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
          </svg>
        );
    }
  };

  const getMessageBgColor = (type?: MessageType) => {
    switch (type) {
      case MessageType.Error:
        return `${t('bg_mainContentSection')} border-red-300`;
      case MessageType.Warning:
        return `${t('bg_mainContentSection')} border-yellow-300`;
      default:
        return `${t('bg_mainContentSection')} border-blue-300`;
    }
  };

  return (
    <div className="fixed bottom-4 right-4 z-[10000]">
      {/* Popup */}
      <div className={`w-[576px] max-h-[60vh] ${theme.mainContentSection} rounded-lg shadow-xl border flex flex-col`}>
        {/* Header */}
        <div className={`flex items-center justify-between px-3 py-2 border-b ${theme.mainContentSection}`}>
          <h3 className={`text-md font-semibold ${theme.title}`}>
            Messages ({messages.length})
          </h3>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => dispatch(clearAllMessages())}
              className={`px-3 py-1.5 text-xs font-medium ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95`}
            >
              Clear All
            </button>
            <button
              onClick={() => dispatch(setPopupOpen(false))}
              className={`p-1.5 ${theme.button_default} rounded-[4px] transition-all duration-200 hover:shadow-sm active:scale-95`}
            >
              <svg className={`w-4 h-4 transition-colors`} fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
              </svg>
            </button>
          </div>
        </div>

        {/* Messages List */}
        <div className="overflow-y-auto px-3 py-3 space-y-2">
          {[...messages].sort((a, b) => b.timestamp - a.timestamp).map((message) => (
            <div
              key={message.id}
              className={`p-2 rounded border ${getMessageBgColor(message.type)}`}
            >
              <div className="flex items-start space-x-2">
                <div className="flex-shrink-0 mt-[1px] opacity-70">
                  {getMessageIcon(message.type)}
                </div>
                <div className="w-1 flex-auto min-w-0">
                  <p className={`text-xs break-words`}>
                    {message.message}
                  </p>
                  <p className={`text-xs mt-1`}>
                    {formatTime(message.timestamp)}
                  </p>
                </div>
                <button
                  onClick={() => dispatch(removeMessage(message.id))}
                  className={`flex-shrink-0 p-1.5 rounded-[4px] transition-all duration-200 border border-transparent ${theme.button_default} hover:shadow-sm active:scale-95`}
                >
                  <svg className={`w-3 h-3 transition-colors`} fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
                  </svg>
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className={`px-3 py-2 border-t ${theme.mainContentSection}`}>
          <button
            onClick={() => dispatch(setPopupOpen(false))}
            className={`w-full px-3 py-2 ${theme.button_default} rounded-[4px] transition-all duration-200 border hover:shadow-sm active:scale-95 text-xs font-medium`}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

export default ErrorMessagePopup; 