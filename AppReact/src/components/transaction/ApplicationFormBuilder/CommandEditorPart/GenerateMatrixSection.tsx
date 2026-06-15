import React, { useMemo } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export const EmAppTransactionCommandTypeGenerateMatrix = 46;

export function GenerateMatrixSection(props: { action: any; hierarchy: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { action, hierarchy, onMarkChange } = props;

  const matrixUnits = useMemo(() => {
    // Angular: matrixUnitList = [{Id:null, UnitDisplayName:'All'}] + child units where IsMatrixUnit === true
    const rootUnits = hierarchy?.AppTransactionUnitList || hierarchy?.AppTransactionData?.AppTransactionUnitList || [];
    const result: any[] = [];
    (Array.isArray(rootUnits) ? rootUnits : []).forEach((u: any) => {
      (u?.Children || []).forEach((cu: any) => {
        if (cu?.IsMatrixUnit) result.push(cu);
      });
    });
    return result;
  }, [hierarchy]);

  if (!action) return null;
  if (Number(action.ActionType) !== EmAppTransactionCommandTypeGenerateMatrix) return null;

  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
      <label className={`text-xs ${theme.label}`}>Maxtri Unit</label>
      <select
        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        value={action?.ActionAttribute?.ParamId1 == null ? '' : String(action.ActionAttribute.ParamId1)}
        onChange={(e) => {
          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
          action.ActionAttribute.ParamId1 = e.target.value ? Number(e.target.value) : null;
          onMarkChange();
        }}
      >
        <option value="">All</option>
        {matrixUnits.map((u: any) => (
          <option key={String(u.Id)} value={String(u.Id)}>
            {u.UnitDisplayName ?? u.DataBaseTableName ?? String(u.Id)}
          </option>
        ))}
      </select>
    </div>
  );
}

