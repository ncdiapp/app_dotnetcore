import React, { useState, useEffect, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabDataAutoCache } from '../../redux/hooks/useTabNavigation';
import { getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';

// Section components
import DatabaseManagement from './DatabaseManagement';
import DatasetManagement from './DatasetManagement';
import ErDiagramManagement from './ErDiagramManagement';
import ExcelDataImportManagement from './ExcelDataImportManagement';
import JsonFileTableImportManagement from './JsonFileTableImportManagement';
import RestApiImportManagement from './RestApiImportManagement';
import DbToDbImportManagement from './DbToDbImportManagement';
import ExtractViewManagement from './ExtractViewManagement';

// Section code enum (matching AngularJS EmSectionCode)
const EmSectionCode = {
  DatasetManagement: 'DatasetManagement',
  ExtractViewManagement: 'ExtractViewManagement',
  DatabaseManagement: 'DatabaseManagement',
  ErDiagramManagement: 'ErDiagramManagement',
  ExcelDataImportManagement: 'ExcelDataImportManagement',
  JsonFileTableImportManagement: 'JsonFileTableImportManagement',
  RestApiImportManagement: 'RestApiImportManagement',
  DbToDbImportManagement: 'DbToDbImportManagement',
};

// Section configuration
interface SectionConfig {
  code: string;
  label: string;
  icon: string;
  component: React.FC;
  hidden?: boolean;
}

const SECTIONS: SectionConfig[] = [
  {
    code: EmSectionCode.DatabaseManagement,
    label: 'SQL Workbench',
    icon: 'fa-solid fa-database',
    component: DatabaseManagement,
  },
  {
    code: EmSectionCode.DatasetManagement,
    label: 'Dataset Management',
    icon: 'fa-solid fa-table-list',
    component: DatasetManagement,
  },
  {
    code: EmSectionCode.ExtractViewManagement,
    label: 'Extract Views',
    icon: 'fa-solid fa-table-columns',
    component: ExtractViewManagement,
    hidden: true,
  },
  {
    code: EmSectionCode.ErDiagramManagement,
    label: 'ER Diagrams',
    icon: 'fa-solid fa-diagram-project',
    component: ErDiagramManagement,
  },
  {
    code: EmSectionCode.ExcelDataImportManagement,
    label: 'Excel File Import',
    icon: 'fa-solid fa-file-excel',
    component: ExcelDataImportManagement,
  },
  {
    code: EmSectionCode.JsonFileTableImportManagement,
    label: 'JSON File Import',
    icon: 'fa-solid fa-file-code',
    component: JsonFileTableImportManagement,
  },
  {
    code: EmSectionCode.RestApiImportManagement,
    label: 'REST API Import',
    icon: 'fa-solid fa-cloud-arrow-down',
    component: RestApiImportManagement,
  },
  {
    code: EmSectionCode.DbToDbImportManagement,
    label: 'DB to DB Import',
    icon: 'fa-solid fa-arrows-left-right',
    component: DbToDbImportManagement,
  },
];

// Helper: get initial section from tab cache (when re-selecting tab) or URL or default
function getInitialSectionCode(searchParams: URLSearchParams): string {
  const tabKey = getCurrentActiveTab()?.tabKey ?? null;
  if (tabKey) {
    const cached = getDataModelFromCache(tabKey);
    if (cached?.currentSectionCode && SECTIONS.some(s => !s.hidden && s.code === cached.currentSectionCode)) {
      return cached.currentSectionCode;
    }
  }
  return searchParams.get('param1') || EmSectionCode.DatabaseManagement;
}

const DatabaseDesignManagement: React.FC = () => {
  const { theme } = useTheme();
  const [searchParams, setSearchParams] = useSearchParams();

  // Initial section: prefer tab cache (so re-selecting tab restores selection), then URL param1, then default
  const [currentSectionCode, setCurrentSectionCode] = useState<string>(() =>
    getInitialSectionCode(searchParams)
  );

  // Data model for tab cache (persist left-side selection when user switches section or leaves tab)
  const dataModel = useMemo(() => ({ currentSectionCode }), [currentSectionCode]);
  useTabDataAutoCache(dataModel);

  // Sync URL when section changes
  useEffect(() => {
    const currentParam1 = searchParams.get('param1');
    if (currentParam1 !== currentSectionCode) {
      const newParams = new URLSearchParams(searchParams);
      newParams.set('param1', currentSectionCode);
      setSearchParams(newParams, { replace: true });
    }
  }, [currentSectionCode, searchParams, setSearchParams]);

  // Handle section selection
  const handleSelectSection = (sectionCode: string) => {
    setCurrentSectionCode(sectionCode);
  };

  // Section button class (match MyApplicationEditor: theme.tab_active / theme.tab)
  const getSectionClass = (sectionCode: string): string => {
    return currentSectionCode === sectionCode ? `${theme.tab_active}` : `${theme.tab}`;
  };

  // Get the current section component
  const getCurrentSectionComponent = (): React.ReactNode => {
    const section = SECTIONS.find(s => s.code === currentSectionCode);
    if (section) {
      const SectionComponent = section.component;
      return <SectionComponent />;
    }
    return (
      <div className="flex items-center justify-center h-full text-gray-500">
        <div className="text-center">
          <i className="fa-solid fa-question-circle text-4xl mb-2"></i>
          <div>Unknown section: {currentSectionCode}</div>
        </div>
      </div>
    );
  };

  return (
    <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">    

      {/* Main Content Area */}
      <div className="w-full h-full overflow-hidden flex gap-1">
        {/* Left Sidebar Navigation (match MyApplicationEditor) */}
        <div className={`w-[90px] flex-none ${theme.mainContentSection} overflow-y-auto rounded-l-md`}>
          <div className="py-5">
            {SECTIONS.filter(s => !s.hidden).map((section) => (
              <div
                key={section.code}
                onClick={() => handleSelectSection(section.code)}
                className={`
                  cursor-pointer flex flex-col items-center py-3 px-2 mb-1.5 transition-colors
                  ${getSectionClass(section.code)}
                `}
                title={section.label}
              >
                <i className={`${section.icon} text-xl mb-1.5`}></i>
                <div className="text-[10px] text-center leading-snug" style={{ wordBreak: 'break-word' }}>
                  {section.label}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Content Area */}
        <div className={`w-1 flex-auto h-full overflow-hidden rounded-r-md ${theme.mainContentSection}`}>
          {getCurrentSectionComponent()}
        </div>
      </div>
    </div>
  );
};

export default DatabaseDesignManagement;
