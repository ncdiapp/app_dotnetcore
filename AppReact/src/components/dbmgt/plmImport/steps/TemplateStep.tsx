import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

const TemplateStep: React.FC = () => {
  const { theme } = useTheme();

  return (
    <div className="flex flex-col gap-4 p-4 h-full overflow-auto">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 3 — Template Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Map 1 PLM Template to 1 APP Transaction Group. Preview and execute coming in Phase 5.
        </p>
      </div>
      <div className={`border rounded p-3 text-xs ${theme.inputBox}`}>
        <p className={theme.menu_secondary}>
          Template → Transaction Group · Tab → Transaction · Block → Unit · Columns → Fields with IntegrationId.
        </p>
      </div>
    </div>
  );
};

export default TemplateStep;
