import React, { useEffect, useState, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch } from 'react-redux';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabDataAutoCache, useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { getDataModelFromCache, getCurrentActiveTab } from '../../redux/features/ui/navigation/tabnavSlice';

// Import section components
import ApplicationProperties from './MyApplicationEditor/ApplicationProperties';
import DataModelDesign from './MyApplicationEditor/DataModelDesign';
import DataModelTemplate from './MyApplicationEditor/DataModelTemplate';
import FormManagement from './MyApplicationEditor/FormManagement';
import EntityManagement from './MyApplicationEditor/EntityManagement';
import SearchManagement from './MyApplicationEditor/SearchManagement';
import DatabaseDesignManagement from '../dbmgt/DatabaseDesignManagement';
import WorkflowAutomation from './MyApplicationEditor/WorkflowAutomation';

// Section IDs matching EmAppApplicationBuilderSection enum
const ApplicationBuilderSection = {
  ApplicationSetting: 1,
  Transaction: 2, // Data Model Design
  Form: 3, // UX Design
  TransactionGroup: 17, // Data Model Template
  ListOfValue: 15, // Entity List Of Value
  DataPresentation: 12, // Report & View
  Database: 9, // Database Management
  Workflow: 3, // Workflow Automation
  MenuSetting: 11
};

const VALID_SECTION_IDS = [
  ApplicationBuilderSection.ApplicationSetting,
  ApplicationBuilderSection.Transaction,
  ApplicationBuilderSection.TransactionGroup,
  ApplicationBuilderSection.Form,
  ApplicationBuilderSection.ListOfValue,
  ApplicationBuilderSection.DataPresentation,
  ApplicationBuilderSection.Database,
  ApplicationBuilderSection.Workflow,
  ApplicationBuilderSection.MenuSetting
];

function getInitialCurrentSection(): number {
  const activeTab = getCurrentActiveTab();
  const tabKey = activeTab?.tabKey ?? null;
  if (tabKey) {
    const cached = getDataModelFromCache(tabKey);
    if (cached?.currentSection != null && VALID_SECTION_IDS.includes(Number(cached.currentSection))) {
      return Number(cached.currentSection);
    }
  }
  return ApplicationBuilderSection.Transaction;
}

const MyApplicationEditor: React.FC = () => {
  const { param } = useParams<{ param: string }>();
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const dispatch = useDispatch();
  useTabNavigation();
  
  const [menuId, setMenuId] = useState<string | null>(null);
  const [applicationData, setApplicationData] = useState<any>(null);
  const [currentSection, setCurrentSection] = useState<number>(getInitialCurrentSection);

  // Extract menu ID from route parameter
  useEffect(() => {
    let paramObj: any = {};
    
    if (param) {
      try {
        const decodedParam = decodeURIComponent(param);
        paramObj = JSON.parse(decodedParam);
      } catch (error) {
        paramObj = { id: param };
      }
    }
    
    const idValue = paramObj.id || null;
    if (idValue) {
      setMenuId(idValue.toString());
    }
  }, [param]);

  // Load application data
  useEffect(() => {
    if (!menuId) return;

    const loadApplicationData = async () => {
      try {
        dispatch(setIsBusy());
        
        const menuData = await adminSvc.retrieveOneAppListMenuExDto(menuId);
        
        if (menuData) {
          setApplicationData(menuData);
          // If application name is "New Application", auto-select Application Properties only when no tab cache (first open)
          if (menuData.Name === 'New Application') {
            const tabKey = getCurrentActiveTab()?.tabKey ?? null;
            const cached = tabKey ? getDataModelFromCache(tabKey) : null;
            if (cached?.currentSection == null) {
              setCurrentSection(ApplicationBuilderSection.ApplicationSetting);
            }
          }
        } else {
          showError('Application not found');
        }
      } catch (error: any) {
        showError(error.message || 'Failed to load application data');
      } finally {
        dispatch(setIsNotBusy());
      }
    };

    loadApplicationData();
  }, [menuId, dispatch, showError]);


  // Data model for tab caching
  const dataModel = useMemo(() => ({
    menuId,
    applicationData,
    currentSection
  }), [menuId, applicationData, currentSection]);

  useTabDataAutoCache(dataModel);

  // Section definitions with icons
  const sections = [
    { id: ApplicationBuilderSection.ApplicationSetting, name: 'Application Properties', icon: 'fa-gear' },
    { id: ApplicationBuilderSection.Transaction, name: 'Data Model Design', icon: 'fa-cubes' },
    { id: ApplicationBuilderSection.TransactionGroup, name: 'Data Model Template', icon: 'fa-table' },
    //{ id: ApplicationBuilderSection.Form, name: 'UX Design', icon: 'fa-palette' },
    { id: ApplicationBuilderSection.ListOfValue, name: 'Entity List Of Value', icon: 'fa-list' },
    { id: ApplicationBuilderSection.DataPresentation, name: 'Report & View', icon: 'fa-chart-bar' }
  ];

  const getSectionName = (sectionId: number): string => {
    return sections.find(s => s.id === sectionId)?.name || 'Section';
  };

  const handleSectionClick = (sectionId: number, _sectionName: string) => {
    setCurrentSection(sectionId);
  };

  // Render section content
  const renderSectionContent = () => {
    switch (currentSection) {
      case ApplicationBuilderSection.ApplicationSetting:
        return <ApplicationProperties menuId={menuId} applicationData={applicationData} />;
      
      case ApplicationBuilderSection.Transaction:
        return <DataModelDesign menuId={menuId} />;
      
      case ApplicationBuilderSection.TransactionGroup:
        return <DataModelTemplate menuId={menuId} />;
      
      case ApplicationBuilderSection.Form:
        return <FormManagement menuId={menuId} />;
      
      case ApplicationBuilderSection.ListOfValue:
        return <EntityManagement menuId={menuId} />;
      
      case ApplicationBuilderSection.DataPresentation:
        return <SearchManagement menuId={menuId} />;
      
      case ApplicationBuilderSection.Database:
        return <DatabaseDesignManagement />;
      
      case ApplicationBuilderSection.Workflow:
        return <WorkflowAutomation menuId={menuId} />;
      
      default:
        return (
          <div className="p-4">
            <h3 className={`text-lg font-semibold mb-4 ${theme.title}`}>
              {getSectionName(currentSection)}
            </h3>
            <div className="mt-4 p-4 bg-gray-50 rounded">
              <p className="text-sm text-gray-600">
                This section is under development. Full implementation to be added based on AngularJS MyApplicationEditor.
              </p>
            </div>
          </div>
        );
    }
  };

  const getSectionClass = (sectionId: number): string => {
    return currentSection &&currentSection === sectionId 
      ? `${theme.tab_active}` 
      : `${theme.tab}`;
  };

  return (
  
      <div className="w-full h-full overflow-hidden flex gap-1">
        {/* Left Sidebar Navigation (90px wide) */}
        <div className={`w-[90px] flex-none ${theme.mainContentSection} overflow-y-auto rounded-l-md`}>
          <div className="py-5">
            {sections.map((section) => (
              <div
                key={section.name}
                onClick={() => handleSectionClick(section.id, section.name)}
                className={`
                  cursor-pointer flex flex-col items-center py-3 px-2 mb-1 transition-colors
                  ${getSectionClass(section.id)}
                `}
                title={section.name}
              >
                {section.id === ApplicationBuilderSection.DataPresentation ? (
                  <span className="relative inline-block w-7 h-7 text-2xl mb-1" style={{ fontSize: '0.85em' }}>
                    <i className="fa-solid fa-chart-bar absolute bottom-0.5 left-0.5 opacity-90" aria-hidden />
                    <i className="fa-solid fa-search absolute top-0.5 right-0.5 opacity-90" aria-hidden />
                  </span>
                ) : (
                  <i className={`fa-solid ${section.icon} text-2xl mb-1`} aria-hidden />
                )}
                <div className="text-[10px] text-center leading-tight" style={{ wordBreak: 'break-word' }}>
                  {section.name}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Content Area */}
        <div className={`w-1 flex-auto h-full overflow-hidden rounded-r-md`}>
          {renderSectionContent()}
        </div>
      </div>
  
  );
};

export default MyApplicationEditor;
