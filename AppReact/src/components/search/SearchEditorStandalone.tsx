import React from 'react';
import SearchEditor from './SearchEditor';

/**
 * Standalone search editor route.
 * MUST be mounted outside LandingPage to avoid tab cache overriding URL.
 * Reuses SearchEditor with normal URL param parsing.
 */
const SearchEditorStandalone: React.FC = () => {
  return <SearchEditor />;
};

export default SearchEditorStandalone;

