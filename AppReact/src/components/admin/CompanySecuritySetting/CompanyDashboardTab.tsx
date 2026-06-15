import React from 'react';
import { useTheme } from '../../../redux/hooks/useTheme';
import DashboardEditor from '../../dashboard/DashboardEditor';

/** Sub-tab labels — align with Angular CompanyManagement (Employee, Client, Supplier, Client Agent, Supplier Agent). */
const DASHBOARD_SUB_TABS = ['Employee', 'Client', 'Supplier', 'Client Agent', 'Supplier Agent'] as const;
/** DesktopId by tab: backend uses EmAppUserType as desktop PK (Employee=2, Customer=3, Supplier=4, ClientAgent=5, SupplierAgent=9). */
const DESKTOP_IDS_BY_TAB = [2, 3, 4, 5, 9];

type Props = {
  companyId: string | number | null;
  currentDashboardSubTab: number;
  onDashboardSubTabChange: (index: number) => void;
  isModified: boolean;
};

/**
 * Dashboard tab for Company Security Setting.
 * Each User Type tab = one desktopId (EmAppUserType: 2,3,4,5,9); tab content embeds Dashboard Editor by that desktopId.
 */
const CompanyDashboardTab: React.FC<Props> = ({
  companyId: _companyId,
  currentDashboardSubTab,
  onDashboardSubTabChange,
  isModified,
}) => {
  const { theme } = useTheme();
  const desktopId = DESKTOP_IDS_BY_TAB[currentDashboardSubTab] ?? 2;

  return (
    <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden ${theme.mainContentSection}`}>
      <div className={`flex flex-wrap gap-0 border-b border-gray-300 dark:border-gray-600 ${theme.mainContentSection}`}>
        {DASHBOARD_SUB_TABS.map((label, idx) => (
          <button
            key={idx}
            type="button"
            className={`px-3 py-2 text-xs border-b-2 whitespace-nowrap ${currentDashboardSubTab === idx ? theme.tab_active : theme.tab}`}
            onClick={() => onDashboardSubTabChange(idx)}
            disabled={isModified}
          >
            {label}
          </button>
        ))}
      </div>
      <div className={`w-full flex-1 min-h-0 flex flex-col overflow-hidden ${theme.mainContentSection}`}>
        <DashboardEditor embeddedDesktopId={desktopId} />
      </div>
    </div>
  );
};

export default CompanyDashboardTab;
