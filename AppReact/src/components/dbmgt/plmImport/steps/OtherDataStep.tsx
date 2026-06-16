import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

const OtherDataStep: React.FC = () => {
  const { theme } = useTheme();

  return (
    <div className="flex flex-col gap-4 p-4 h-full overflow-auto">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 4 — Other Data</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          Placeholder for additional PLM data (Color, POM, etc.). Extensible step registry — Phase 6.
        </p>
      </div>
      <div className={`border rounded p-3 text-xs ${theme.inputBox}`}>
        <p className={theme.menu_secondary}>No import modules registered yet.</p>
      </div>
    </div>
  );
};

export default OtherDataStep;
