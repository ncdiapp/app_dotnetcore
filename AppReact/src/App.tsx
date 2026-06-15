import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { store } from './redux/store';
import { BusyLoader } from './components/common/BusyLoader';
import ErrorMessageButton from './components/common/ErrorMessageButton';
import ErrorMessagePopup from './components/common/ErrorMessagePopup';
import { AlertConfirmProvider } from './components/common/AlertConfirmProvider';
import AppRoutes from './routes';
import { TabNavigationProvider } from './redux/hooks/useTabNavigation';
// import './styles/wijmo-overrides.css';

const App: React.FC = () => {
  return (
    <Provider store={store}>
      <BrowserRouter>
        <TabNavigationProvider>
          <AlertConfirmProvider>
            <AppRoutes />
            <BusyLoader />
            <ErrorMessageButton />
            <ErrorMessagePopup />
          </AlertConfirmProvider>
        </TabNavigationProvider>
      </BrowserRouter>
    </Provider>
  );
};

export default App;
