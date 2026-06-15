import React from 'react';
import { useParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';

interface UnderDevelopmentPlaceholderProps {
  title: string;
}

/**
 * Generic placeholder for AngularJS-migrated routes not yet fully implemented.
 * Shows title and optional param (from :param in path).
 */
const UnderDevelopmentPlaceholder: React.FC<UnderDevelopmentPlaceholderProps> = ({ title }) => {
  const { theme } = useTheme();
  const { param } = useParams<{ param?: string }>();
  let paramObj: any = {};
  if (param) {
    try {
      paramObj = JSON.parse(decodeURIComponent(param));
    } catch {
      paramObj = { id: param };
    }
  }

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden p-4 ${theme.mainContentSection}`}>
      <div className={`text-md font-semibold mb-2 ${theme.title}`}>{title}</div>
      <div className={`text-sm ${theme.label}`}>
        This section is under development. Full implementation to be added based on AngularJS.
      </div>
      {(paramObj?.id != null || paramObj?.param1 != null) && (
        <div className={`mt-2 text-xs ${theme.label}`}>
          Params: id={String(paramObj.id ?? '—')}, param1={String(paramObj.param1 ?? '—')}
        </div>
      )}
    </div>
  );
};

export default UnderDevelopmentPlaceholder;
