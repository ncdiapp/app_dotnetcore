import React from 'react';
import { useParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';

type PlaceholderKind = 'search-editor' | 'eshop-category-search-editor';

const titles: Record<PlaceholderKind, string> = {
  'search-editor': 'Search Editor',
  'eshop-category-search-editor': 'Eshop Category Search Editor'
};

/**
 * Placeholder for AngularJS Search Editor / Eshop Category Search Editor until migrated.
 * Used by routes search-editor and eshop-category-search-editor.
 */
const SearchEditorPlaceholder: React.FC<{ kind: PlaceholderKind }> = ({ kind }) => {
  const { theme } = useTheme();
  const { param } = useParams<{ param: string }>();
  let paramObj: any = {};
  if (param) {
    try {
      paramObj = JSON.parse(decodeURIComponent(param));
    } catch {
      paramObj = { id: param };
    }
  }
  const title = titles[kind];

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden p-4 ${theme.mainContentSection}`}>
      <div className={`text-md font-semibold mb-2 ${theme.title}`}>{title}</div>
      <div className={`text-sm ${theme.label}`}>
        This section is under development. Full implementation to be added based on AngularJS Search Editor.
      </div>
      {(paramObj?.id || paramObj?.param1) && (
        <div className={`mt-2 text-xs ${theme.label}`}>
          Param: id={paramObj.id ?? '—'}, param1={paramObj.param1 ?? '—'}
        </div>
      )}
    </div>
  );
};

export default SearchEditorPlaceholder;
