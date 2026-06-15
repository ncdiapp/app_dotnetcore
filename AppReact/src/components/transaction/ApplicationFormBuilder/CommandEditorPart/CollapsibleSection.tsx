import React from 'react';
import { useTheme } from '../../../../redux/hooks/useTheme';

export function CollapsibleSection(props: {
  sectionId: string;
  title: string;
  collapsed: boolean;
  onToggle: (sectionId: string) => void;
  children: React.ReactNode;
}) {
  const { theme } = useTheme();
  const { sectionId, title, collapsed, onToggle, children } = props;

  return (
    <div className={`border rounded-[6px] overflow-hidden ${theme.mainContentSection}`}>
      <button
        type="button"
        className={`w-full flex items-center justify-between gap-2 px-3 py-2 text-left ${theme.mainContentSection}`}
        onClick={() => onToggle(sectionId)}
      >
        <div className={`text-xs font-semibold ${theme.title}`}>{title}</div>
        <i
          className={`fa-solid fa-chevron-down text-[11px] ${theme.label} transition-transform ${collapsed ? '-rotate-90' : 'rotate-0'}`}
          aria-hidden
        />
      </button>
      {collapsed ? null : <div className={`border-t px-3 py-3 ${theme.mainContentSection}`}>{children}</div>}
    </div>
  );
}

