import React from 'react';
import { useAlert } from '../../hooks/useAlert';
import Alert from './Alert';
import Confirm from './Confirm';

interface AlertConfirmProviderProps {
  children: React.ReactNode;
}

export const AlertConfirmContext = React.createContext<ReturnType<typeof useAlert> | null>(null);

export const AlertConfirmProvider: React.FC<AlertConfirmProviderProps> = ({ children }) => {
  const alertHook = useAlert();

  return (
    <AlertConfirmContext.Provider value={alertHook}>
      {children}
      <Alert
        isOpen={alertHook.alertState.isOpen}
        title={alertHook.alertState.title}
        message={alertHook.alertState.message}
        okLabel={alertHook.alertState.okLabel}
        onClose={alertHook.closeAlert}
      />
      <Confirm
        isOpen={alertHook.confirmState.isOpen}
        title={alertHook.confirmState.title}
        message={alertHook.confirmState.message}
        confirmLabel={alertHook.confirmState.confirmLabel}
        cancelLabel={alertHook.confirmState.cancelLabel}
        confirmButtonStyle={alertHook.confirmState.confirmButtonStyle}
        onConfirm={alertHook.confirmState.onConfirm || (() => {})}
        onCancel={alertHook.confirmState.onCancel || (() => {})}
      />
    </AlertConfirmContext.Provider>
  );
};

export const useAlertConfirm = () => {
  const context = React.useContext(AlertConfirmContext);
  if (!context) {
    throw new Error('useAlertConfirm must be used within AlertConfirmProvider');
  }
  return {
    showAlert: context.showAlert,
    showConfirm: context.showConfirm,
  };
};

