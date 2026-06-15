import { useCallback, useMemo, useRef, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { addErrorMessage, clearAllMessages, removeMessage, togglePopup, setPopupOpen, MessageType } from '../features/ui/feedback/errorMessageSlice';

interface ValidationItem {
  ItemType: number;
  LocalizedMessage: string;
  Details?: string;
}

interface ValidationResult {
  Items?: ValidationItem[];
}

interface ErrorMessages {
  error: string[];
  warning: string[];
  message: string[];
}

export const useErrorMessage = () => {
  const dispatch = useDispatch();
  const autoCloseTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  // Clear timeout on unmount
  useEffect(() => {
    return () => {
      if (autoCloseTimeoutRef.current) {
        clearTimeout(autoCloseTimeoutRef.current);
      }
    };
  }, []);

  const getErrorMessage = useCallback((validationResult: ValidationResult | null): ErrorMessages | null => {
    if (validationResult) {
      const items = validationResult.Items;
      if (items && items.length > 0) {
        const errorMessages: ErrorMessages = { error: [], warning: [], message: [] };

        items.forEach((item) => {
          let message = item.LocalizedMessage;
          
          if (item.Details) {
            message += '\n' + item.Details;
          }

          if (item.ItemType === 1) {
            errorMessages.error.push(message);
          } else if (item.ItemType === 2) {
            errorMessages.warning.push(message);
          } else if (item.ItemType === 3) {
            errorMessages.message.push(message);
          }
        });

        return errorMessages;
      }
    }
    return null;
  }, []);

  const showValidationMessages = useCallback((validationResult: ValidationResult | null, autoOpenPopup: boolean = true) => {
    const errorMessages = getErrorMessage(validationResult);
    
    if (errorMessages) {
      // Add error messages
      errorMessages.error.forEach(message => {
        dispatch(addErrorMessage({ message, type: MessageType.Error }));
      });

      // Add warning messages
      errorMessages.warning.forEach(message => {
        dispatch(addErrorMessage({ message, type: MessageType.Warning }));
      });

      // Add info messages
      errorMessages.message.forEach(message => {
        dispatch(addErrorMessage({ message, type: MessageType.Message }));
      });

      // Auto open popup if requested and there are messages
      if (autoOpenPopup && (errorMessages.error.length > 0 || errorMessages.warning.length > 0 || errorMessages.message.length > 0)) {
        dispatch(setPopupOpen(true));
      }
    }
  }, [dispatch, getErrorMessage]);

  const showError = useCallback((message: string) => {
    dispatch(addErrorMessage({ message, type: MessageType.Error }));
    dispatch(setPopupOpen(true));
  }, [dispatch]);

  const showWarning = useCallback((message: string) => {
    dispatch(addErrorMessage({ message, type: MessageType.Warning }));
    dispatch(setPopupOpen(true));
  }, [dispatch]);

  const showInfo = useCallback((message: string, autoClose: boolean = false) => {
    dispatch(addErrorMessage({ message, type: MessageType.Message }));
    dispatch(setPopupOpen(true));
    
    // Clear any existing timeout
    if (autoCloseTimeoutRef.current) {
      clearTimeout(autoCloseTimeoutRef.current);
      autoCloseTimeoutRef.current = null;
    }
    
    // Set auto-close timeout if requested
    if (autoClose) {
      autoCloseTimeoutRef.current = setTimeout(() => {
        dispatch(setPopupOpen(false));
        autoCloseTimeoutRef.current = null;
      }, 2000);
    }
  }, [dispatch]);

  const logMessage = useCallback((message: string) => {
    dispatch(addErrorMessage({ message, type: MessageType.Message }));
    // Don't open popup - just add to message list
  }, [dispatch]);

  const showMessage = useCallback((message: string, type: MessageType) => {
    dispatch(addErrorMessage({ message, type }));
    dispatch(setPopupOpen(true));
  }, [dispatch]);

  const clearAll = useCallback(() => {
    dispatch(clearAllMessages());
  }, [dispatch]);

  const remove = useCallback((messageId: string) => {
    dispatch(removeMessage(messageId));
  }, [dispatch]);

  const toggle = useCallback(() => {
    dispatch(togglePopup());
  }, [dispatch]);

  const open = useCallback(() => {
    dispatch(setPopupOpen(true));
  }, [dispatch]);

  const close = useCallback(() => {
    dispatch(setPopupOpen(false));
  }, [dispatch]);

  return useMemo(() => ({
    getErrorMessage,
    showValidationMessages,
    showError,
    showWarning,
    showInfo,
    showMessage,
    logMessage,
    clearAll,
    remove,
    toggle,
    open,
    close,
  }), [
    clearAll,
    close,
    getErrorMessage,
    logMessage,
    open,
    remove,
    showError,
    showInfo,
    showMessage,
    showValidationMessages,
    showWarning,
    toggle,
  ]);
}; 