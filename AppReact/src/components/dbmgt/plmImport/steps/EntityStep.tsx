import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import type { PlmImportWizardState } from '../types';

export type EntityStepProps = {
  state: PlmImportWizardState;
};

const EntityStep: React.FC<EntityStepProps> = ({ state }) => {
  const { theme } = useTheme();

  return (
    <div className="flex flex-col gap-4 p-4 h-full overflow-auto">
      <div>
        <h2 className={`text-sm font-semibold ${theme.label}`}>Step 2 — Entity Import</h2>
        <p className={`text-xs mt-1 ${theme.menu_secondary}`}>
          System Define entities are imported first, then User Define. Preview and execute will be available in Phase 3–4.
        </p>
      </div>

      <div className={`border rounded p-3 text-xs ${theme.inputBox}`}>
        <div className="font-semibold mb-2">System Define</div>
        <p className={theme.menu_secondary}>Export PLM tables (DSF=1), preview mapping, then run async import job.</p>
        <p className={`mt-2 ${theme.menu_secondary}`}>Status: {state.systemDefineComplete ? 'Complete' : 'Not started'}</p>
      </div>

      <div className={`border rounded p-3 text-xs ${theme.inputBox}`}>
        <div className="font-semibold mb-2">User Define</div>
        <p className={theme.menu_secondary}>
          Preview entity metadata and row data. Execute performs TRUNCATE + full reload per entity.
        </p>
        <p className={`mt-2 ${theme.menu_secondary}`}>
          {state.systemDefineComplete ? 'Unlocked' : 'Locked until System Define completes or is skipped'}
        </p>
      </div>
    </div>
  );
};

export default EntityStep;
