import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { RootState } from '../../redux/store';
import { togglePopup, MessageType } from '../../redux/features/ui/feedback/errorMessageSlice';

const ErrorMessageButton: React.FC = () => {
  const dispatch = useDispatch();
  const { messages } = useSelector((state: RootState) => state.errorMessage);

  // Don't render if there are no messages
  if (messages.length === 0) {
    return null;
  }

  const errorCount = messages.length;
  const hasErrors = messages.some(msg => msg.type === MessageType.Error);
  const hasWarnings = messages.some(msg => msg.type === MessageType.Warning);

  // Determine button color based on message types - using lighter blue theme
  let buttonColor = 'bg-blue-400';
  if (hasErrors) {
    buttonColor = 'bg-blue-500';
  } else if (hasWarnings) {
    buttonColor = 'bg-blue-400';
  } else {
    buttonColor = 'bg-blue-300';
  }

  return (
    <div className="fixed bottom-4 right-4 z-[10000]">
      <button
        onClick={() => dispatch(togglePopup())}
        className={`${buttonColor} hover:${buttonColor.replace('400', '600').replace('500', '600').replace('300', '500')} text-white rounded-full w-8 h-8 flex items-center justify-center shadow-md transition-all duration-200 transform hover:scale-110`}
        title={`${errorCount} message${errorCount > 1 ? 's' : ''}`}
      >
        <div className="relative">       
          <div className="bg-white text-gray-800 rounded-full w-4 h-4 flex items-center justify-center text-[10px] font-bold">
            {errorCount > 99 ? '99+' : errorCount}
          </div>
        </div>
      </button>
    </div>
  );
};

export default ErrorMessageButton; 