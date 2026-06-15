import React, { useMemo } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

export type MatrixFkTableOption = { UiId: string; Name: string; isSelected: boolean };

type Props = {
  isOpen: boolean;
  options: MatrixFkTableOption[];
  onToggle: (uiId: string, selected: boolean) => void;
  onApply: () => void;
  onClose: () => void;
};

const MatrixKeySelectorPopup: React.FC<Props> = ({ isOpen, options, onToggle, onApply, onClose }) => {
  const { theme } = useTheme();
  const selectedCount = useMemo(() => options.filter((o) => o.isSelected).length, [options]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[1200] flex items-center justify-center bg-black/40">
      <div className={`w-[400px] max-h-[80vh] flex flex-col rounded-md shadow-lg ${theme.mainContentSection}`}>
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <div className={`text-sm font-semibold ${theme.title}`}>Foreign Matrix Keys</div>
          <button type="button" className={`px-2 py-1 text-xs ${theme.button_default}`} onClick={onClose}>
            <i className="fa-solid fa-xmark" aria-hidden />
          </button>
        </div>
        <div className="px-3 py-2 overflow-auto flex-auto max-h-[50vh]">
          {options.length === 0 ? (
            <div className={`text-xs ${theme.label}`}>No level-2 parent tables available.</div>
          ) : (
            <ul className="space-y-1">
              {options.map((o) => (
                <li key={o.UiId} className="flex items-center gap-2 text-xs">
                  <input
                    type="checkbox"
                    checked={o.isSelected}
                    onChange={(e) => onToggle(o.UiId, e.target.checked)}
                  />
                  <span>{o.Name}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
        <div className="flex justify-end gap-2 px-3 py-2 border-t">
          <button type="button" className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`} onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className={`px-3 py-1.5 text-sm rounded-[4px] ${theme.button_default}`}
            onClick={onApply}
            disabled={!selectedCount}
          >
            Apply
          </button>
        </div>
      </div>
    </div>
  );
};

export default MatrixKeySelectorPopup;
