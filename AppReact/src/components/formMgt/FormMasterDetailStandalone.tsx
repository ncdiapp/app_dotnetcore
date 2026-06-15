import React from 'react';
import FormMasterDetail from './FormMasterDetail';

/**
 * Standalone form route.
 * MUST be mounted outside LandingPage to avoid tab cache.
 * Reuses FormMasterDetail with normal URL param parsing.
 */
const FormMasterDetailStandalone: React.FC = () => {
  return <FormMasterDetail />;
};

export default FormMasterDetailStandalone;

