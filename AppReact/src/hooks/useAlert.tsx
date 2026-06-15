import { useState, useCallback } from 'react';

export interface AlertOptions {
  title?: string;
  okLabel?: string;
}

export interface ConfirmOptions {
  title?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmButtonStyle?: string;
}

interface AlertState {
  isOpen: boolean;
  message: string;
  title?: string;
  okLabel?: string;
  onClose?: () => void;
}

interface ConfirmState {
  isOpen: boolean;
  message: string;
  title?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  confirmButtonStyle?: string;
  onConfirm?: () => void;
  onCancel?: () => void;
}

export const useAlert = () => {
  const [alertState, setAlertState] = useState<AlertState>({
    isOpen: false,
    message: '',
  });

  const [confirmState, setConfirmState] = useState<ConfirmState>({
    isOpen: false,
    message: '',
  });

  const showAlert = useCallback((message: string, options?: AlertOptions) => {
    return new Promise<void>((resolve) => {
      setAlertState({
        isOpen: true,
        message,
        title: options?.title,
        okLabel: options?.okLabel,
        onClose: () => {
          setAlertState({ isOpen: false, message: '' });
          resolve();
        },
      });
    });
  }, []);

  const showConfirm = useCallback((message: string, options?: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      setConfirmState({
        isOpen: true,
        message,
        title: options?.title,
        confirmLabel: options?.confirmLabel,
        cancelLabel: options?.cancelLabel,
        confirmButtonStyle: options?.confirmButtonStyle,
        onConfirm: () => {
          setConfirmState({ isOpen: false, message: '' });
          resolve(true);
        },
        onCancel: () => {
          setConfirmState({ isOpen: false, message: '' });
          resolve(false);
        },
      });
    });
  }, []);

  const closeAlert = useCallback(() => {
    if (alertState.onClose) {
      alertState.onClose();
    } else {
      setAlertState({ isOpen: false, message: '' });
    }
  }, [alertState]);

  const closeConfirm = useCallback(() => {
    if (confirmState.onCancel) {
      confirmState.onCancel();
    } else {
      setConfirmState({ isOpen: false, message: '' });
    }
  }, [confirmState]);

  return {
    alertState,
    confirmState,
    showAlert,
    showConfirm,
    closeAlert,
    closeConfirm,
  };
};

