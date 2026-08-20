import React, { useState, useEffect, useMemo } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
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
import PlmDataImportManagement from './PlmDataImportManagement';
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
  PlmDataImportManagement: 'PlmDataImportManagement',
};

type SqlWorkbenchSeed = {
  queryText?: string;
  dataSourceRegisterId?: number | null;
  autoExecute?: boolean;
};

function parseRouteParamObj(param?: string): Record<string, unknown> {
  if (!param) return {};
  try {
    return JSON.parse(decodeURIComponent(param)) as Record<string, unknown>;
  } catch {
    try {
      return JSON.parse(param) as Record<string, unknown>;
    } catch {
      return {};
    }
  }
}

function sqlWorkbenchSeedFromParam(paramObj: Record<string, unknown>): SqlWorkbenchSeed {
  const queryRaw = paramObj.queryText ?? paramObj.sql ?? paramObj.query;
  const queryText = typeof queryRaw === 'string' ? queryRaw.trim() : '';
  const dsRaw =
    paramObj.dataSourceRegisterId ?? paramObj.dataSourceId ?? paramObj.initDataSourceRegId;
  let dataSourceRegisterId: number | null = null;
  if (typeof dsRaw === 'number' && Number.isFinite(dsRaw)) dataSourceRegisterId = dsRaw;
  else if (dsRaw != null && dsRaw !== '') {
    const n = Number(dsRaw);
    if (Number.isFinite(n)) dataSourceRegisterId = n;
  }
  const autoExecute =
    paramObj.autoExecute === false || paramObj.autoExecute === 'false'
      ? false
      : !!queryText;
  return {
    queryText: queryText || undefined,
    dataSourceRegisterId,
    autoExecute,
  };
}

// Section configuration
interface SectionConfig {
  code: string;
  label: string;
  icon: string;
  component: React.FC<any>;
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
  {
    code: EmSectionCode.PlmDataImportManagement,
    label: 'PLM Data Import',
    icon: 'fa-solid fa-file-import',
    component: PlmDataImportManagement,
  },
];

// Helper: get initial section from tab cache (when re-selecting tab) or URL or default
function getInitialSectionCode(searchParams: URLSearchParams, forceSqlWorkbench: boolean): string {
  if (forceSqlWorkbench) return EmSectionCode.DatabaseManagement;
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
  const { param } = useParams<{ param?: string }>();
  const [searchParams, setSearchParams] = useSearchParams();

  const routeParamObj = useMemo(() => parseRouteParamObj(param), [param]);
  const sqlSeed = useMemo(() => sqlWorkbenchSeedFromParam(routeParamObj), [routeParamObj]);
  const forceSqlWorkbench = !!sqlSeed.queryText;

  const [currentSectionCode, setCurrentSectionCode] = useState<string>(() =>
    getInitialSectionCode(searchParams, forceSqlWorkbench)
  );

  useEffect(() => {
    if (forceSqlWorkbench) {
      setCurrentSectionCode(EmSectionCode.DatabaseManagement);
    }
  }, [forceSqlWorkbench, sqlSeed.queryText, sqlSeed.dataSourceRegisterId]);

  const dataModel = useMemo(() => ({ currentSectionCode }), [currentSectionCode]);
  useTabDataAutoCache(dataModel);

  useEffect(() => {
    const currentParam1 = searchParams.get('param1');
    if (currentParam1 !== currentSectionCode) {
      const newParams = new URLSearchParams(searchParams);
      newParams.set('param1', currentSectionCode);
      setSearchParams(newParams, { replace: true });
    }
  }, [currentSectionCode, searchParams, setSearchParams]);

  const handleSelectSection = (sectionCode: string) => {
    setCurrentSectionCode(sectionCode);
  };

  const getSectionClass = (sectionCode: string): string => {
    return currentSectionCode === sectionCode ? `${theme.tab_active}` : `${theme.tab}`;
  };

  const sqlWorkbenchKey = [
    sqlSeed.queryText || '',
    sqlSeed.dataSourceRegisterId ?? '',
    sqlSeed.autoExecute ? '1' : '0',
  ].join('|');

  const renderSectionPanels = (): React.ReactNode => (
    <>
      {SECTIONS.filter(s => !s.hidden).map((section) => {
        const SectionComponent = section.component;
        const isActive = currentSectionCode === section.code;
        const isSqlWorkbench = section.code === EmSectionCode.DatabaseManagement;
        return (
          <div
            key={isSqlWorkbench ? `sql-${sqlWorkbenchKey}` : section.code}
            className={`h-full w-full overflow-hidden ${isActive ? '' : 'hidden'}`}
            aria-hidden={!isActive}
          >
            {isSqlWorkbench ? (
              <DatabaseManagement
                initialQueryText={sqlSeed.queryText}
                initialDataSourceId={sqlSeed.dataSourceRegisterId}
                autoExecute={sqlSeed.autoExecute}
              />
            ) : (
              <SectionComponent />
            )}
          </div>
        );
      })}
    </>
  );

  return (
    <div className="w-full h-full rounded-t-md rounded-b-md overflow-hidden">
      <div className="w-full h-full overflow-hidden flex gap-1">
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

        <div className={`w-1 flex-auto h-full overflow-hidden rounded-r-md ${theme.mainContentSection}`}>
          {renderSectionPanels()}
        </div>
      </div>
    </div>
  );
};

export default DatabaseDesignManagement;
