import React from 'react';
import FormMasterDetail from './FormMasterDetail';
import { useTheme } from '../../redux/hooks/useTheme';

/**
 * Standalone form route.
 * MUST be mounted outside LandingPage to avoid tab cache.
 * Provides its own viewport-height shell (LandingPage normally supplies h-screen),
 * otherwise FormMasterDetail's h-full / flex-auto chain collapses to a blank body
 * while the fixed toolbar still shows.
 */
const FormMasterDetailStandalone: React.FC = () => {
  const { theme, t } = useTheme();

  return (
    <div className={`${theme.default} ${t('scrollbar_style')} w-full h-screen flex flex-col overflow-hidden`}>
      <div className="w-full h-1 flex-auto min-h-0 flex flex-col overflow-hidden">
        <FormMasterDetail />
      </div>
    </div>
  );
};

export default FormMasterDetailStandalone;
