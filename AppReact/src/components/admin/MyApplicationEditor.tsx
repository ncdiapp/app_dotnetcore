import React, { useEffect, useState, useMemo, useRef } from 'react';
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
import ImportAppConfig from './MyApplicationEditor/ImportAppConfig';
import DatabaseDesignManagement from '../dbmgt/DatabaseDesignManagement';
import WorkflowAutomation from './MyApplicationEditor/WorkflowAutomation';
import MyApplicationEditorAgentDock, {
  EditorAgentFocus,
} from './MyApplicationEditor/MyApplicationEditorAgentDock';

// Section IDs matching EmAppApplicationBuilderSection enum
const ApplicationBuilderSection = {
  ApplicationSetting: 1,
  Transaction: 2, // Data Model Design
  Form: 3, // UX Design
  TransactionGroup: 17, // Data Model Template
  ListOfValue: 15, // Entity List Of Value
  DataPresentation: 12, // Report & View
  ImportConfig: 18, // Import App Config From JSON
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
  ApplicationBuilderSection.ImportConfig,
  ApplicationBuilderSection.Database,
  ApplicationBuilderSection.Workflow,
  ApplicationBuilderSection.MenuSetting
];

function parseEditorRouteParam(param: string | undefined): Record<string, any> {
  if (!param) return {};
  try {
    return JSON.parse(decodeURIComponent(param));
  } catch {
    return { id: param };
  }
}

function getInitialSelectedSectionFromRoute(paramObj: Record<string, any>): number | null {
  let initial = paramObj.initialSelectedSection;
  if (initial == null && paramObj.param2 != null) {
    try {
      const param2Obj =
        typeof paramObj.param2 === 'string' ? JSON.parse(paramObj.param2) : paramObj.param2;
      initial = param2Obj?.initialSelectedSection;
    } catch {
      initial = null;
    }
  }
  if (initial == null) return null;
  const sectionId = Number(initial);
  return VALID_SECTION_IDS.includes(sectionId) ? sectionId : null;
}

/** Same-app tab cache > route initialSelectedSection (new-app create) > Data Model Design. */
function getInitialCurrentSection(routeParam?: string): number {
  const paramObj = parseEditorRouteParam(routeParam);
  const routeAppId = paramObj.id != null ? String(paramObj.id) : null;
  const fromRoute = getInitialSelectedSectionFromRoute(paramObj);

  const activeTab = getCurrentActiveTab();
  const tabKey = activeTab?.tabKey ?? null;
  if (tabKey) {
    const cached = getDataModelFromCache(tabKey);
    const cachedAppId = cached?.menuId != null ? String(cached.menuId) : null;
    const sameApp = routeAppId != null && cachedAppId != null && routeAppId === cachedAppId;
    if (
      sameApp &&
      cached?.currentSection != null &&
      VALID_SECTION_IDS.includes(Number(cached.currentSection))
    ) {
      return Number(cached.currentSection);
    }
  }
  if (fromRoute != null) {
    return fromRoute;
  }
  return ApplicationBuilderSection.Transaction;
}

const MyApplicationEditor: React.FC = () => {
  const { param } = useParams<{ param: string }>();
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const dispatch = useDispatch();
  useTabNavigation();
  
  // Init menuId from route synchronously so Entity/Search remount with the correct app filter
  // (tab switch remounts via Outlet key; cached currentSection can restore ListOfValue/DataPresentation
  // before a post-mount effect would set menuId).
  const [menuId, setMenuId] = useState<string | null>(() => {
    const paramObj = parseEditorRouteParam(param);
    return paramObj.id != null ? String(paramObj.id) : null;
  });
  const [applicationData, setApplicationData] = useState<any>(null);
  const [currentSection, setCurrentSection] = useState<number>(() => getInitialCurrentSection(param));
  const [agentFocus, setAgentFocus] = useState<EditorAgentFocus | null>(null);
  const [showAgentDock, setShowAgentDock] = useState(true);
  /** Agent Chat dock width (px); drag divider between main area and dock. */
  const AGENT_DOCK_DEFAULT = 360;
  const AGENT_DOCK_MIN = 280;
  const AGENT_DOCK_MAX = 720;
  const [agentDockWidthPx, setAgentDockWidthPx] = useState(AGENT_DOCK_DEFAULT);
  const contentDockSplitRef = useRef<HTMLDivElement | null>(null);
  const agentDockResizeDragRef = useRef(false);
  const lastSectionParamRef = React.useRef<string | undefined>(param);

  useEffect(() => {
    const onMove = (e: MouseEvent) => {
      if (!agentDockResizeDragRef.current || !contentDockSplitRef.current) return;
      const rect = contentDockSplitRef.current.getBoundingClientRect();
      // Dragging the left edge of the dock: width = containerRight - mouseX
      const next = rect.right - e.clientX;
      const maxDock = Math.max(AGENT_DOCK_MIN, rect.width - 280);
      setAgentDockWidthPx(Math.min(Math.max(next, AGENT_DOCK_MIN), Math.min(AGENT_DOCK_MAX, maxDock)));
    };
    const onUp = () => {
      if (!agentDockResizeDragRef.current) return;
      agentDockResizeDragRef.current = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    return () => {
      document.removeEventListener('mousemove', onMove);
      document.removeEventListener('mouseup', onUp);
    };
  }, []);

  // Extract menu ID from route parameter; reset section when editor opens a different app (tab reuse).
  useEffect(() => {
    const paramObj = parseEditorRouteParam(param);
    const idValue = paramObj.id != null ? String(paramObj.id) : null;
    setMenuId(idValue);

    if (param !== lastSectionParamRef.current) {
      lastSectionParamRef.current = param;
      const fromRoute = getInitialSelectedSectionFromRoute(paramObj);
      setCurrentSection(fromRoute ?? ApplicationBuilderSection.Transaction);
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
    { id: ApplicationBuilderSection.DataPresentation, name: 'Report & View', icon: 'fa-chart-bar' },
    { id: ApplicationBuilderSection.ImportConfig, name: 'Import Config', icon: 'fa-file-import' }
  ];

  const getSectionName = (sectionId: number): string => {
    return sections.find(s => s.id === sectionId)?.name || 'Section';
  };

  const handleSectionClick = (sectionId: number, _sectionName: string) => {
    setCurrentSection(sectionId);
    // Leaving a drilled-in object returns chat to app-builder Assistant.
    // Drill-in: section children will call setAgentFocus({ kind:'transaction'|… }) next.
    setAgentFocus(null);
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

      case ApplicationBuilderSection.ImportConfig:
        return <ImportAppConfig menuId={menuId} />;
      
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

        {/* Content Area + Agent Chat dock */}
        <div
          ref={contentDockSplitRef}
          className={`w-1 flex-auto h-full overflow-hidden rounded-r-md flex gap-1`}
        >
          <div className="w-1 flex-auto h-full min-w-0 overflow-hidden">
            {renderSectionContent()}
          </div>
          {showAgentDock && menuId && (
            <>
              <div
                className={`w-[6px] shrink-0 cursor-col-resize border-l border-r self-stretch min-h-0 ${theme.inputBox} ${theme.mainContentSection}`}
                role="separator"
                aria-orientation="vertical"
                aria-label="Resize Assistant"
                title="Drag to resize"
                onMouseDown={(e) => {
                  e.preventDefault();
                  agentDockResizeDragRef.current = true;
                  document.body.style.cursor = 'col-resize';
                  document.body.style.userSelect = 'none';
                }}
              />
              <div
                className="flex-none h-full overflow-hidden border-l"
                style={{ width: agentDockWidthPx, minWidth: AGENT_DOCK_MIN }}
              >
                <MyApplicationEditorAgentDock
                  saasApplicationId={Number(menuId)}
                  applicationName={applicationData?.Name || applicationData?.DisplayName}
                  sectionId={currentSection}
                  sectionName={getSectionName(currentSection)}
                  focus={agentFocus}
                  onHidePanel={() => setShowAgentDock(false)}
                />
              </div>
            </>
          )}
          {!showAgentDock && (
            <button
              type="button"
              className={`flex-none w-8 h-full text-xs ${theme.button_default}`}
              title="Show Assistant"
              onClick={() => setShowAgentDock(true)}
            >
              <i className="fa-solid fa-robot" aria-hidden />
            </button>
          )}
        </div>
      </div>
  
  );
};

export default MyApplicationEditor;
