import React, { Suspense } from 'react';

import PageNotFoundErrorPage from './components/public/PageNotFoundErrorPage';
import WijmoTheme from './components/themeTemplate/wijmoTheme';
import UserManagement from './components/admin/UserManagement';
import UserLoginInfoEditor from './components/admin/UserLoginInfoEditor';
import AppSearch from './components/search/AppSearch';
import SearchEditor from './components/search/SearchEditor';
import EshopCategorySearchEditor from './components/search/EshopCategorySearchEditor';
import SearchApiSetting from './components/search/SearchApiSetting';
import SearchViewNavigationMenuEditor from './components/search/SearchViewNavigationMenuEditor';
import UnderDevelopmentPlaceholder from './components/admin/UnderDevelopmentPlaceholder';
import SearchViewEditor from './components/search/SearchViewEditor';
import TransactionGroupEditor from './components/transaction/TransactionGroupEditor';
import TransactionFormGroup from './components/transaction/TransactionFormGroup';
import SimpleValueListEntityEdit from './components/admin/EntityListOfValue/SimpleValueListEntityEdit';
import AppEntityInfoEdit from './components/admin/EntityListOfValue/AppEntityInfoEdit';
import EntityDataPreview from './components/admin/EntityListOfValue/EntityDataPreview';
import ApplicationSetting from './components/admin/ApplicationSetting';
import ApplicationSetting2 from './components/admin/ApplicationSetting2';
import TestDDL from './components/admin/TestDDL';
import DataSourceRegister from './components/admin/DataSourceRegister';
import MetaDataManagement from './components/transaction/metaDataManagement';
import FormMasterDetail from './components/formMgt/FormMasterDetail';
import FormListEdit from './components/formMgt/FormListEdit';
import FormMasterDetailPrint from './components/formMgt/FormMasterDetailPrint';
import FileManagement from './components/fileMgt/FileManagement';
import TransactionFolderNavigationPage from './components/transaction/TransactionFolderNavigationPage';
import TransactionNavigationEditor from './components/transaction/TransactionNavigationEditor';
import DatabaseDesignManagement from './components/dbmgt/DatabaseDesignManagement';
import RestApiImportEditor from './components/dbmgt/RestApiImportEditor';
import ExcelTableImportEditor from './components/dbmgt/ExcelTableImportEditor';
import DbToDbImportEditor from './components/dbmgt/DbToDbImportEditor';
import DataSetEditor from './components/dbmgt/DataSetEditor';
import ErDiagramEditor from './components/dbmgt/ErDiagramEditor';
import ApplicationFormBuilderPage from './components/transaction/ApplicationFormBuilderPage';
import MyApplicationEditor from './components/admin/MyApplicationEditor';
import MyApplications from './components/admin/MyApplications';
import MenuManagement from './components/admin/MenuManagement';
import ProjectMgt from './components/project/ProjectMgt';
import WorkflowAutomationManagement from './components/workflow/WorkflowAutomationManagement';
import WorkflowAutomationEditor from './components/workflow/WorkflowAutomationEditor';
import WorkflowExecutionMonitor from './components/workflow/WorkflowExecutionMonitor';
import TestDomDragAndDrop from './components/test/dragAndDrop/TestDomDragAndDrop';
import Dashboard from './components/dashboard/Dashboard';
import DashboardManagement from './components/dashboard/DashboardManagement';
import DashboardEditor from './components/dashboard/DashboardEditor';
import { CompanySecuritySetting } from './components/admin/CompanySecuritySetting';
import {
  ThirdPartyApiProviderManagement,
  AppApiManagement,
  ThirdPartyApiProviderEditor,
  ThirdPartyApiEditor,
  AppJsonQueryApiEditor,
  AppDataModelApiEditor,
  AppDataPresentationApiEditor,
  ExcelImportDataUpdateApiEditor,
} from './components/integration';
import { DbaGenie } from './components/dbgenie';
import AISkillManagement from './components/aiskill/AISkillManagement';
import AppBuilderAgent from './components/integration/AppBuilderAgent';
import AppReportAgent from './components/search/AppReportAgent';
import MessageManagement from './components/message/MessageManagement';
import MessageEditor from './components/message/MessageEditor';
import MessageTemplateManagement from './components/message/MessageTemplateManagement';
import MessageTemplateLayoutEditor from './components/message/MessageTemplateLayoutEditor';
import FormMessageManagement from './components/message/FormMessageManagement';
import MessageNotificationSettingManagement from './components/message/MessageNotificationSettingManagement';
import TenantProvisioning from './components/admin/TenantProvisioning';
import DbDriverManagement from './components/admin/DbDriverManagement';

// Lazy load pages (same as routes.tsx)
const Home = React.lazy(() => import('./components/public/Home'));
const MyAccount = React.lazy(() => import('./components/admin/MyAccount'));

export type SharedRouteDef = {
  path: string;
  element: React.ReactElement;
  /**
   * If false, this route will NOT be available in EmbeddedAppRoute.
   * (Prevents recursive Dashboard/Home rendering inside dashboard widgets.)
   */
  embeddable?: boolean;
  /** If true, only master-DB SysAdmin (DomainId=1) may access this route. */
  sysAdminOnly?: boolean;
};

const suspense = (node: React.ReactElement) => (
  <Suspense fallback={<div style={{ padding: '20px' }}>Loading...</div>}>{node}</Suspense>
);

/**
 * Shared, authenticated routes list used by BOTH:
 * - `src/routes.tsx` (main app router)
 * - `EmbeddedAppRoute` (dashboard widget internal rendering)
 *
 * NOTE: Some routes are marked `embeddable: false` to avoid recursion.
 */
export const AUTHENTICATED_ROUTES: SharedRouteDef[] = [
  // IMPORTANT: '/home/*' allows descendant <Routes> under the /home route.
  // We render embedded routes inside dashboard widgets while on /home.
  { path: '/home/*', element: suspense(<Home />), embeddable: false },
  { path: '/page-not-found', element: <PageNotFoundErrorPage /> },

  { path: 'myaccount/:param', element: suspense(<MyAccount />) },
  { path: 'my-calendar/:param', element: <UnderDevelopmentPlaceholder title="My Calendar" /> },
  { path: 'my-calendar', element: <UnderDevelopmentPlaceholder title="My Calendar" /> },
  { path: 'user-management/:param', element: <UserManagement /> },
  { path: 'user-editor/:param', element: <UserLoginInfoEditor /> },
  { path: 'system-setting/:param', element: <ApplicationSetting /> },
  { path: 'system-setting', element: <ApplicationSetting /> },
  { path: 'system-setting-2/:param', element: <ApplicationSetting2 /> },
  { path: 'system-setting-2', element: <ApplicationSetting2 /> },
  { path: 'database-registration/:param', element: <DataSourceRegister /> },
  { path: 'database-registration', element: <DataSourceRegister /> },

  { path: 'company-security/:param', element: <CompanySecuritySetting /> },
  { path: 'company-security', element: <CompanySecuritySetting /> },

  { path: 'tenant-provisioning/:param', element: <TenantProvisioning />, sysAdminOnly: true },
  { path: 'tenant-provisioning', element: <TenantProvisioning />, sysAdminOnly: true },

  { path: 'db-driver-management/:param', element: <DbDriverManagement />, sysAdminOnly: true },
  { path: 'db-driver-management', element: <DbDriverManagement />, sysAdminOnly: true },

  { path: 'maintheme/:param', element: suspense(<Home />), embeddable: false },
  { path: 'wijmotheme/:param', element: <WijmoTheme /> },

  { path: 'MasterDataManagement/:param', element: <AppSearch /> },
  { path: 'MasterDataManagement', element: <AppSearch /> },
  { path: 'masterdatamanagement/:param', element: <AppSearch /> },
  { path: 'masterdatamanagement', element: <AppSearch /> },

  { path: 'test-ddl/:param', element: <TestDDL /> },
  { path: 'test-ddl', element: <TestDDL /> },

  { path: 'metadata-management/:param', element: <MetaDataManagement /> },
  { path: 'metadata-management', element: <MetaDataManagement /> },

  // Database Design Management
  { path: 'DatabaseDesignManagement/:param', element: <DatabaseDesignManagement /> },
  { path: 'DatabaseDesignManagement', element: <DatabaseDesignManagement /> },
  { path: 'database-design-management/:param', element: <DatabaseDesignManagement /> },
  { path: 'database-design-management', element: <DatabaseDesignManagement /> },

  // DBA-Genie AI Agent
  { path: 'DbaGenie/:param', element: <DbaGenie /> },
  { path: 'DbaGenie', element: <DbaGenie /> },
  { path: 'dba-genie/:param', element: <DbaGenie /> },
  { path: 'dba-genie', element: <DbaGenie /> },

  // AI Skill Management
  { path: 'ai-skill-management/:param', element: <AISkillManagement /> },
  { path: 'ai-skill-management', element: <AISkillManagement /> },

  // AppBuilder AI Agent
  { path: 'app-builder-agent/:param', element: <AppBuilderAgent /> },
  { path: 'app-builder-agent', element: <AppBuilderAgent /> },

  // AppReport AI Agent
  { path: 'app-report-agent/:param', element: <AppReportAgent /> },
  { path: 'app-report-agent', element: <AppReportAgent /> },

  { path: 'FormMasterDetail/:param', element: <FormMasterDetail /> },
  { path: 'FormMasterDetail', element: <FormMasterDetail /> },

  { path: 'TransactionFormGroup/:param', element: <TransactionFormGroup /> },
  { path: 'TransactionFormGroup', element: <TransactionFormGroup /> },

  // React-only print page (replaces legacy OpenWindow/FormMasterDetailPrint)
  { path: 'FormMasterDetailPrint/:param', element: <FormMasterDetailPrint /> },
  { path: 'FormMasterDetailPrint', element: <FormMasterDetailPrint /> },

  { path: 'application-form-builder/:param', element: <ApplicationFormBuilderPage /> },
  { path: 'application-form-builder', element: <ApplicationFormBuilderPage /> },
  { path: 'my-application-editor/:param', element: <MyApplicationEditor /> },
  { path: 'my-application-editor', element: <MyApplicationEditor /> },
  { path: 'esite-management/:param', element: <MyApplicationEditor /> },
  { path: 'esite-management', element: <MyApplicationEditor /> },
  { path: 'app-website-management/:param', element: <MyApplicationEditor /> },
  { path: 'app-website-management', element: <MyApplicationEditor /> },

  { path: 'my-applications/:param', element: <MyApplications /> },
  { path: 'my-applications', element: <MyApplications /> },
  { path: 'root-menu-management/:param', element: <MenuManagement /> },
  { path: 'root-menu-management', element: <MenuManagement /> },

  { path: 'file-management/:param', element: <FileManagement /> },
  { path: 'file-management', element: <FileManagement /> },
  { path: 'transaction-folder-navigation/:param', element: <TransactionFolderNavigationPage /> },
  { path: 'transaction-folder-navigation', element: <TransactionFolderNavigationPage /> },
  { path: 'transaction-navigation-editor/:param', element: <TransactionNavigationEditor /> },
  { path: 'transaction-navigation-editor', element: <TransactionNavigationEditor /> },

  { path: 'project-management/:param', element: <ProjectMgt /> },
  { path: 'project-management', element: <ProjectMgt /> },

  { path: 'workflow-automation-management/:param', element: <WorkflowAutomationManagement /> },
  { path: 'workflow-automation-management', element: <WorkflowAutomationManagement /> },
  { path: 'workflow-automation-editor/:param', element: <WorkflowAutomationEditor /> },
  { path: 'workflow-automation-editor', element: <WorkflowAutomationEditor /> },
  { path: 'workflow-execution-monitor/:param', element: <WorkflowExecutionMonitor /> },
  { path: 'workflow-execution-monitor', element: <WorkflowExecutionMonitor /> },

  { path: 'test-dom-drag-and-drop/:param', element: <TestDomDragAndDrop /> },
  { path: 'test-dom-drag-and-drop', element: <TestDomDragAndDrop /> },

  // Dashboard routes (never embeddable inside dashboard widgets)
  { path: 'dashboard/:param', element: <Dashboard />, embeddable: false },
  { path: 'dashboard', element: <Dashboard />, embeddable: false },
  { path: 'dashboard-management/:param', element: <DashboardManagement />, embeddable: false },
  { path: 'dashboard-management', element: <DashboardManagement />, embeddable: false },
  { path: 'dashboard-edit/:param/*', element: <DashboardEditor />, embeddable: false },
  { path: 'dashboard-edit/*', element: <DashboardEditor />, embeddable: false },

  // API Management – Third Party API Provider & App API Provider
  { path: 'third-party-api-provider-management/:param', element: <ThirdPartyApiProviderManagement /> },
  { path: 'third-party-api-provider-management', element: <ThirdPartyApiProviderManagement /> },
  { path: 'app-api-provider/:param', element: <AppApiManagement /> },
  { path: 'app-api-provider', element: <AppApiManagement /> },
  { path: 'third-party-api-provider-editor/:param', element: <ThirdPartyApiProviderEditor /> },
  { path: 'third-party-api-provider-editor', element: <ThirdPartyApiProviderEditor /> },
  { path: 'api-builder-editor/:param', element: <AppJsonQueryApiEditor /> },
  { path: 'api-builder-editor', element: <AppJsonQueryApiEditor /> },
  { path: 'app-data-model-api-editor/:param', element: <AppDataModelApiEditor /> },
  { path: 'app-data-model-api-editor', element: <AppDataModelApiEditor /> },
  { path: 'app-data-presentation-api-editor/:param', element: <AppDataPresentationApiEditor /> },
  { path: 'app-data-presentation-api-editor', element: <AppDataPresentationApiEditor /> },
  { path: 'third-party-api-editor/:param', element: <ThirdPartyApiEditor /> },
  { path: 'third-party-api-editor', element: <ThirdPartyApiEditor /> },
  { path: 'excel-import-data-update-api-editor/:param', element: <ExcelImportDataUpdateApiEditor /> },
  { path: 'excel-import-data-update-api-editor', element: <ExcelImportDataUpdateApiEditor /> },
  { path: 'rest-api-import-editor/:param', element: <RestApiImportEditor /> },
  { path: 'rest-api-import-editor', element: <RestApiImportEditor /> },
  { path: 'excel-table-import-editor/:param', element: <ExcelTableImportEditor /> },
  { path: 'excel-table-import-editor', element: <ExcelTableImportEditor /> },
  { path: 'db-to-db-import-editor/:param', element: <DbToDbImportEditor /> },
  { path: 'db-to-db-import-editor', element: <DbToDbImportEditor /> },
  { path: 'dataset-editor/:param', element: <DataSetEditor /> },
  { path: 'dataset-editor', element: <DataSetEditor /> },
  { path: 'er-diagram-editor/:param', element: <ErDiagramEditor /> },
  { path: 'er-diagram-editor', element: <ErDiagramEditor /> },
  { path: 'search-editor/:param', element: <SearchEditor /> },
  { path: 'search-editor', element: <SearchEditor /> },
  { path: 'search-api-setting/:param', element: <SearchApiSetting /> },
  { path: 'search-api-setting', element: <SearchApiSetting /> },
  { path: 'transaction-group-editor/:param', element: <TransactionGroupEditor /> },
  { path: 'transaction-group-editor', element: <TransactionGroupEditor /> },
  { path: 'search-view-editor/:param', element: <SearchViewEditor /> },
  { path: 'search-view-editor', element: <SearchViewEditor /> },
  { path: 'search-view-navigate-to-form-editor/:param', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-navigate-to-form-editor', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-navigate-to-search-editor/:param', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-navigate-to-search-editor', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-navigate-to-built-in-page-editor/:param', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-navigate-to-built-in-page-editor', element: <SearchViewNavigationMenuEditor /> },
  { path: 'search-view-formula-editor/:param', element: <UnderDevelopmentPlaceholder title="Formula Editor" /> },
  { path: 'search-view-formula-editor', element: <UnderDevelopmentPlaceholder title="Formula Editor" /> },
  { path: 'form-design-grid/:param', element: <UnderDevelopmentPlaceholder title="Search View Layout Editor" /> },
  { path: 'form-design-grid', element: <UnderDevelopmentPlaceholder title="Search View Layout Editor" /> },
  { path: 'eshop-category-search-editor/:param', element: <EshopCategorySearchEditor /> },
  { path: 'eshop-category-search-editor', element: <EshopCategorySearchEditor /> },
  { path: 'entity-info-edit/:param', element: <AppEntityInfoEdit /> },
  { path: 'entity-info-edit', element: <AppEntityInfoEdit /> },
  { path: 'entity-data-preview/:param', element: <EntityDataPreview /> },
  { path: 'entity-data-preview', element: <EntityDataPreview /> },
  { path: 'simple-value-list-entity-edit/:param', element: <SimpleValueListEntityEdit /> },
  { path: 'simple-value-list-entity-edit', element: <SimpleValueListEntityEdit /> },
  { path: 'form-list-edit/:param', element: <FormListEdit /> },
  { path: 'form-list-edit', element: <FormListEdit /> },
  { path: 'message-management/:param', element: <MessageManagement /> },
  { path: 'message-management', element: <MessageManagement /> },
  { path: 'message-editor/:param', element: <MessageEditor /> },
  { path: 'message-editor', element: <MessageEditor /> },
  { path: 'message-template-management/:param', element: <MessageTemplateManagement /> },
  { path: 'message-template-management', element: <MessageTemplateManagement /> },
  { path: 'message-template-code-editor/:param', element: <MessageEditor /> },
  { path: 'message-template-code-editor', element: <MessageEditor /> },
  { path: 'message-template-layout-editor/:param', element: <MessageTemplateLayoutEditor /> },
  { path: 'message-template-layout-editor', element: <MessageTemplateLayoutEditor /> },
  { path: 'form-message-management/:param', element: <FormMessageManagement /> },
  { path: 'message-notification-setting-management/:param', element: <MessageNotificationSettingManagement /> },
];

export const EMBEDDABLE_ROUTES: SharedRouteDef[] = AUTHENTICATED_ROUTES.filter(
  (r) => r.embeddable !== false
);

