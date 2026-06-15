import React, { useMemo, useState } from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';

type Props = {
  isOpen: boolean;
  entities: any[];
  onSelect: (entityId: string | number) => void;
  onClose: () => void;
};

const EntitySelectorPopup: React.FC<Props> = ({ isOpen, entities, onSelect, onClose }) => {
  const { theme } = useTheme();
  const [filter, setFilter] = useState('');

  const filtered = useMemo(() => {
    const q = filter.trim().toLowerCase();
    if (!q) return entities;
    return entities.filter(
      (e) =>
        String(e.EntityCode ?? e.Name ?? '').toLowerCase().includes(q) ||
        String(e.Id ?? '').includes(q),
    );
  }, [entities, filter]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[1200] flex items-center justify-center bg-black/40">
      <div className={`w-[480px] max-h-[80vh] flex flex-col rounded-md shadow-lg ${theme.mainContentSection}`}>
        <div className="flex items-center justify-between px-3 py-2 border-b">
          <div className={`text-sm font-semibold ${theme.title}`}>Select Entity</div>
          <button type="button" className={`px-2 py-1 text-xs ${theme.button_default}`} onClick={onClose}>
            <i className="fa-solid fa-xmark" aria-hidden />
          </button>
        </div>
        <div className="px-3 py-2">
          <input
            className={`w-full h-7 px-2 text-xs border rounded-[4px] ${theme.inputBox}`}
            placeholder="Filter entities…"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          />
        </div>
        <div className="px-3 pb-2 overflow-auto flex-auto max-h-[50vh]">
          <ul className="space-y-0.5">
            {filtered.map((e) => (
              <li key={String(e.Id)}>
                <button
                  type="button"
                  className={`w-full text-left px-2 py-1.5 text-xs rounded hover:opacity-90 ${theme.button_default}`}
                  onClick={() => onSelect(e.Id)}
                >
                  {e.EntityCode ?? e.Name ?? e.Id}
                </button>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};

export default EntitySelectorPopup;
