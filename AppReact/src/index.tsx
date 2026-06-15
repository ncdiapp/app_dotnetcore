import './resizeObserverPatch';
import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import './wijmoPatches';
import App from './App';
import { Provider } from 'react-redux';
import { store } from './redux/store';
import './license';
import './styles/dayPilotCustomeized.css';
import { isResizeObserverLoopError } from './resizeObserverPatch';

// Suppress React 18 warnings from third-party libraries (like Wijmo)
const originalConsoleError = console.error;
console.error = (...args) => {
  if (
    typeof args[0] === 'string' &&
    (args[0].includes('Warning: unmountComponentAtNode is deprecated') ||
      args[0].includes('Warning: ReactDOM.render is no longer supported') ||
      isResizeObserverLoopError(args[0]))
  ) {
    return;
  }
  originalConsoleError.apply(console, args);
};

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);
root.render(

  <Provider store={store}>
    <App />
  </Provider>

);
