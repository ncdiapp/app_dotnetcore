import React, { useEffect, useMemo, useState } from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';
import { appTransactionService } from '../../../../webapi/apptransactionsvc';

const EmAppTransactionCommandTypeOpenLinkedSearch = 53;

export function OpenLinkedSearchSection(props: { hierarchy: any; action: any; onMarkChange: () => void }) {
  const { theme } = useTheme();
  const { hierarchy, action, onMarkChange } = props;

  const [rootUnitLinkedSearches, setRootUnitLinkedSearches] = useState<any[]>([]);

  const rootUnitId = useMemo(() => hierarchy?.AppTransactionUnitList?.[0]?.Id ?? null, [hierarchy?.AppTransactionUnitList]);

  // Angular: rootLevelUnitLinkedSearchDtoList is prepared when editor opens (not only on focus).
  useEffect(() => {
    if (!action || Number(action.ActionType) !== EmAppTransactionCommandTypeOpenLinkedSearch) return;
    if (rootUnitId == null) {
      setRootUnitLinkedSearches([]);
      return;
    }
    let cancelled = false;
    (async () => {
      try {
        const raw = await appTransactionService.retrieveOneAppTransactionUnitLinkedSearchList(String(rootUnitId));
        if (!cancelled) setRootUnitLinkedSearches(Array.isArray(raw) ? raw : []);
      } catch {
        if (!cancelled) setRootUnitLinkedSearches([]);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [action?.Id, action?.ActionType, rootUnitId]);

  if (!action || Number(action.ActionType) !== EmAppTransactionCommandTypeOpenLinkedSearch) return null;

  const selectedLinkedSearchId = action.ActionAttribute?.LinkedSearchId;
  const selectedValue =
    selectedLinkedSearchId != null && selectedLinkedSearchId !== '' ? String(selectedLinkedSearchId) : '';
  const hasSelectedInList =
    !!selectedValue && rootUnitLinkedSearches.some((ls: any) => String(ls?.Id ?? '') === selectedValue);

  return (
    <div className="grid grid-cols-[14rem_1fr] items-center gap-2 py-1">
      <label className={`text-xs ${theme.label}`}>Navigate To Search</label>
      <select
        className={`w-72 h-7 px-2 text-xs border ${theme.inputBox} focus:outline-none`}
        value={selectedValue}
        onChange={(e) => {
          action.ActionAttribute = action.ActionAttribute || { ChildActionList: [] };
          const v = e.target.value;
          action.ActionAttribute.LinkedSearchId = v ? Number(v) : null;
          onMarkChange();
        }}
      >
        <option value="">(Select)</option>
        {selectedValue && !hasSelectedInList ? (
          <option value={selectedValue}>{`Search ${selectedValue}`}</option>
        ) : null}
        {rootUnitLinkedSearches.map((ls: any) => (
          <option key={String(ls?.Id)} value={String(ls?.Id ?? '')}>
            {ls?.Name ?? ls?.DisplayName ?? `Search ${ls?.Id ?? ''}`}
          </option>
        ))}
      </select>
    </div>
  );
}
