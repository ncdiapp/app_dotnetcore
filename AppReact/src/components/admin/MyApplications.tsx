import React, { useEffect, useState } from 'react';
import { useTheme } from '../../redux/hooks/useTheme';
import { useErrorMessage } from '../../redux/hooks/useErrorMessage';
import { setIsBusy, setIsNotBusy } from '../../redux/features/ui/feedback/busyLoaderSlice';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { adminSvc } from '../../webapi/adminsvc';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useAlertConfirm } from '../common/AlertConfirmProvider';
import { refreshUserTreeMenu } from '../../helper/userMenuHelper';

interface ApplicationTile {
  Id?: string | number;
  Name: string;
  Description?: string;
  ImageUrl?: string;
  RouteCode?: string;
  GlobalGuid?: string;
  IsSpecial?: boolean; // For special tiles like Tutorial, Add Applications, etc.
  IsImportedFromOtherDB?: boolean; // For imported applications
}

const MyApplications: React.FC = () => {
  const { theme } = useTheme();
  const { showError } = useErrorMessage();
  const dispatch = useDispatch();
  const { addTabAndNavigate, addTabFromListMenu } = useTabNavigation();
  const { showConfirm } = useAlertConfirm();
  const userMenu = useSelector((state: RootState) => state.userSession.userMenu);
  
  const [selectedApplications, setSelectedApplications] = useState<ApplicationTile[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Special application tiles (built-in/system applications)
  const specialApplications: ApplicationTile[] = [
    {
      Id: 'tutorial',
      Name: 'Tutorial',
      Description: 'Application Tutorial',
      ImageUrl: 'fa-book', // Font Awesome icon class
      RouteCode: '/tutorial',
      IsSpecial: true
    },
    {
      Id: 'add-applications',
      Name: 'Add Applications',
      Description: 'Install new applications',
      ImageUrl: 'fa-plus-circle', // Font Awesome icon class
      RouteCode: '/add-applications',
      IsSpecial: true
    },
    {
      Id: 'settings',
      Name: 'Settings',
      Description: 'Company and System Settings',
      ImageUrl: 'fa-cogs', // Font Awesome icon class
      RouteCode: '/system-setting',
      IsSpecial: true
    },
    {
      Id: 'api-management',
      Name: 'API Management',
      Description: 'API Management',
      ImageUrl: 'fa-plug', // Font Awesome icon class
      RouteCode: '/third-party-api-provider-management',
      IsSpecial: true
    },
    {
      Id: 'database-management',
      Name: 'Database Management',
      Description: 'Database Management',
      ImageUrl: 'fa-database', // Font Awesome icon class
      RouteCode: '/database-design-management',
      IsSpecial: true
    },
    {
      Id: 'project-workflow',
      Name: 'Project And Workflow',
      Description: 'Project And Workflow',
      ImageUrl: 'fa-project-diagram', // Font Awesome icon class
      RouteCode: '/project-management',
      IsSpecial: true
    },
    {
      Id: 'file-management',
      Name: 'File Management',
      Description: 'Store and share files online with security',
      ImageUrl: 'fa-folder-open', // Font Awesome icon class
      RouteCode: '/file-management', // opens with defaultCategoryId: 3 (My Company) in handleApplicationClick
      IsSpecial: true
    }    
  ];

  const loadApplications = async () => {
    try {
      setIsLoading(true);
      dispatch(setIsBusy());
      
      const packages = await adminSvc.retrieveSelectedApplicationPackages(true);
      
      // Transform the packages to match our ApplicationTile interface
      // For dynamic applications (user-created), use a default Font Awesome icon if no ImageUrl is provided
      const transformedPackages: ApplicationTile[] = packages.map((pkg: any) => ({
        Id: pkg.Id,
        Name: pkg.Name,
        Description: pkg.Description || '',
        // Use Font Awesome icon for user-created applications (fa-cube represents an application/package)
        ImageUrl: pkg.ImageUrl && !pkg.ImageUrl.startsWith('fa-') ? pkg.ImageUrl : 'fa-cube',
        RouteCode: pkg.RouteCode,
        GlobalGuid: pkg.GlobalGuid,
        IsSpecial: false,
        IsImportedFromOtherDB: pkg.IsImportedFromOtherDB || false
      }));
      
      setSelectedApplications(transformedPackages);
    } catch (error: any) {
      showError(error.message || 'Failed to load applications');
    } finally {
      setIsLoading(false);
      dispatch(setIsNotBusy());
    }
  };

  useEffect(() => {
    loadApplications();
  }, []);

  const handleApplicationClick = (application: ApplicationTile, e?: React.MouseEvent) => {
    // "Add Applications" card: single click creates from scratch (no hover menu)
    if (application.Id === 'add-applications') {
      if (e) {
        void handleCreateFromScratch(e);
      }
      return;
    }

    // For other special applications, navigate directly
    if (application.IsSpecial) {
      if (application.RouteCode) {
        const paramObj =
          application.Id === 'file-management' ? { defaultCategoryId: 3 } : {};
        addTabAndNavigate(application.RouteCode, application.Name, paramObj, true);
      }
      return;
    }

    // Only for user-created dynamic applications, use Open event by default
    // This opens the first child menu or shows confirmation dialog for new/empty apps
    if (application.Id && !application.IsSpecial) {
      // Create a synthetic event if not provided
      const syntheticEvent = e || { stopPropagation: () => {} } as React.MouseEvent;
      handleOpenApplication(application, syntheticEvent);
    }
  };

  // Helper function to sort menu items by Sort property (matching Sidebar logic)
  const sortMenuItems = (menuItems: any[]): any[] => {
    if (!menuItems || menuItems.length === 0) return menuItems;
    
    // Sort by Sort property (ascending), items without Sort go to the end
    const sorted = [...menuItems].sort((a, b) => {
      const sortA = a.Sort !== undefined && a.Sort !== null ? Number(a.Sort) : 999999;
      const sortB = b.Sort !== undefined && b.Sort !== null ? Number(b.Sort) : 999999;
      return sortA - sortB;
    });
    
    // Recursively sort children
    return sorted.map(item => {
      if (item.AppListMenu_List && item.AppListMenu_List.length > 0) {
        return {
          ...item,
          AppListMenu_List: sortMenuItems(item.AppListMenu_List)
        };
      }
      return item;
    });
  };

  // Helper function to find menu by Id in the menu tree
  const findMenuById = (menuList: any[], menuId: string | number): any => {
    if (!menuList || menuList.length === 0) return null;
    
    for (const menu of menuList) {
      if (menu.Id === menuId || menu.Id === parseInt(menuId.toString())) {
        return menu;
      }
      if (menu.AppListMenu_List && menu.AppListMenu_List.length > 0) {
        const found = findMenuById(menu.AppListMenu_List, menuId);
        if (found) return found;
      }
    }
    return null;
  };

  // Handle opening application (matching AngularJS openApplicationPackage exactly)
  const handleOpenApplication = async (application: ApplicationTile, e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (!application.Id || !userMenu) return;

    // Find the package root menu in userMenu by Id (matching AngularJS logic)
    const packageRootMenu = findMenuById(userMenu, application.Id);
    
    if (!packageRootMenu) {
      // Fallback: if menu not found, show confirmation dialog (matching AngularJS logic for new/empty apps)
      const confirmed = await showConfirm(
        "This Application is new or empty. \nDo you want to start to build it?",
        {
          title: 'Message',
          confirmLabel: 'Yes',
          cancelLabel: 'No'
        }
      );
      
      if (confirmed) {
        // Open application editor (matching AngularJS openMyApplicationEditor)
        const paramObj = { id: application.Id };
        let routeBasePath = '/my-application-editor';
        
        // Route based on GlobalGuid (matching AngularJS logic)
        if (application.GlobalGuid) {
          const ESiteConfigurationRootMenuGuid = '706026CA-27FE-4A7D-B0BF-03DC2966DB74';
          const AppWebSiteConfigurationRootMenuGuid = '9F92CEB7-8EA9-4E2D-AD4A-00293DD06F4B';
          const globalGuidLower = application.GlobalGuid.toLowerCase();
          if (globalGuidLower === ESiteConfigurationRootMenuGuid.toLowerCase()) {
            routeBasePath = '/esite-management';
          } else if (globalGuidLower === AppWebSiteConfigurationRootMenuGuid.toLowerCase()) {
            routeBasePath = '/app-website-management';
          }
        }
        
        addTabAndNavigate(routeBasePath, `Config: ${application.Name}`, paramObj, true);
      }
      return;
    }

    // Check if packageRootMenu has AppListMenu_List (matching AngularJS logic exactly)
    if (packageRootMenu.AppListMenu_List && packageRootMenu.AppListMenu_List.length > 0) {
      // Sort the menu items to ensure we get the correct first item (matching Sidebar sorting)
      const sortedChildren = sortMenuItems(packageRootMenu.AppListMenu_List);
      
      // Follow AngularJS logic exactly:
      // 1. Get first child menu item (after sorting)
      let menuItem = sortedChildren[0];
      
      // 2. If first child has children, get its first child (also sorted)
      if (menuItem.AppListMenu_List && menuItem.AppListMenu_List.length > 0) {
        const sortedGrandChildren = sortMenuItems(menuItem.AppListMenu_List);
        menuItem = sortedGrandChildren[0];
        
        // 3. If that child also has children, get its first child (max 3 levels, also sorted)
        if (menuItem.AppListMenu_List && menuItem.AppListMenu_List.length > 0) {
          const sortedGreatGrandChildren = sortMenuItems(menuItem.AppListMenu_List);
          menuItem = sortedGreatGrandChildren[0];
        }
      }
      
      // Open the final menu item (matching AngularJS leftMenuClick behavior)
      if (menuItem) {
        addTabFromListMenu(menuItem);
      }
    } else {
      // If no children, show confirmation dialog (matching AngularJS logic)
      // "This Application is new or empty. Do you want to start to build it?"
      const confirmed = await showConfirm(
        "This Application is new or empty. \nDo you want to start to build it?",
        {
          title: 'Message',
          confirmLabel: 'Yes',
          cancelLabel: 'No'
        }
      );
      
      if (confirmed) {
        // Open application editor (matching AngularJS openMyApplicationEditor)
        const paramObj = { id: application.Id };
        let routeBasePath = '/my-application-editor';
        
        // Route based on GlobalGuid (matching AngularJS logic)
        if (application.GlobalGuid) {
          const ESiteConfigurationRootMenuGuid = '706026CA-27FE-4A7D-B0BF-03DC2966DB74';
          const AppWebSiteConfigurationRootMenuGuid = '9F92CEB7-8EA9-4E2D-AD4A-00293DD06F4B';
          const globalGuidLower = application.GlobalGuid.toLowerCase();
          if (globalGuidLower === ESiteConfigurationRootMenuGuid.toLowerCase()) {
            routeBasePath = '/esite-management';
          } else if (globalGuidLower === AppWebSiteConfigurationRootMenuGuid.toLowerCase()) {
            routeBasePath = '/app-website-management';
          }
        }
        
        addTabAndNavigate(routeBasePath, `Config: ${application.Name}`, paramObj, true);
      }
    }
  };

  // Handle creating application from scratch (matching AngularJS executeSaveNewApplication)
  const handleCreateFromScratch = async (e: React.MouseEvent) => {
    e.stopPropagation();
    
    try {
      dispatch(setIsBusy());
      
      // Create new application data (matching AngularJS logic)
      const newApplication = {
        Name: 'New Application',
        Description: 'New Application',
        isCreateAppFromExcel: false,
        isImportEntityFirst: true
      };
      
      // Call API to create new application
      const result = await adminSvc.createMyNewApplicationPackage(newApplication);
      
      if (result.IsSuccessful && result.Object) {
        // Refresh applications list
        await loadApplications();
        await refreshUserTreeMenu();
        
        // Auto-open new application configuration editor (matching AngularJS autoOpenNewAppConfigurationEditor)
        const applicationId = result.Object;
        const paramObj = { id: applicationId };
        addTabAndNavigate('/my-application-editor', `Config: New Application`, paramObj, true);
      } else {
        // Show validation errors
        const errorMessages = result.ValidationResult || [];
        if (errorMessages.length > 0) {
          showError(errorMessages.join(', '));
        } else {
          showError('Failed to create new application');
        }
      }
    } catch (error: any) {
      showError(error.message || 'Failed to create new application');
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Handle removing/uninstalling application (matching AngularJS deleteOneInstalledApplication)
  const handleRemoveApplication = async (application: ApplicationTile, e: React.MouseEvent) => {
    e.stopPropagation();
    
    if (!application.Id) return;

    const isImported = application.IsImportedFromOtherDB;
    const actionText = isImported ? 'Uninstall' : 'Remove';
    const confirmMessage = isImported 
      ? `Please Confirm To Uninstall The Application: ${application.Name}.`
      : `Please Confirm To Remove This Application`;

    const confirmed = await showConfirm(confirmMessage, {
      title: 'Confirm',
      confirmLabel: 'OK',
      cancelLabel: 'Cancel'
    });

    if (!confirmed) return;

    try {
      dispatch(setIsBusy());
      
      if (isImported) {
        // For imported applications, use UnistallApplicationFromCurrentUserDB
        // Note: This API might not exist in React app yet, using deleteOneApplicationPackage as fallback
        const result = await adminSvc.deleteOneApplicationPackage(application.Id.toString());
        if (result.IsSuccessful) {
          await loadApplications();
          await refreshUserTreeMenu();
        } else {
          showError(result.ValidationResult?.join(', ') || 'Failed to uninstall application');
        }
      } else {
        // For regular applications, use DeleteOneApplicationPackage
        const result = await adminSvc.deleteOneApplicationPackage(application.Id.toString());
        if (result.IsSuccessful) {
          await loadApplications();
          await refreshUserTreeMenu();
        } else {
          showError(result.ValidationResult?.join(', ') || 'Failed to remove application');
        }
      }
    } catch (error: any) {
      showError(error.message || `Failed to ${actionText.toLowerCase()} application`);
    } finally {
      dispatch(setIsNotBusy());
    }
  };

  // Check if imageUrl is a Font Awesome icon class (starts with 'fa-')
  const isFontAwesomeIcon = (imageUrl?: string): boolean => {
    return imageUrl ? imageUrl.startsWith('fa-') : false;
  };

  // Filter out duplicates: remove static special applications that match dynamic application names
  // Keep dynamic items (from API) and remove static hard-coded ones that have the same name
  const selectedAppNames = new Set(selectedApplications.map(app => app.Name.toLowerCase()));
  const filteredSpecialApplications = specialApplications.filter(
    app => !selectedAppNames.has(app.Name.toLowerCase())
  );

  // Combine: static special applications first, then dynamic applications at the end
  const allApplications: ApplicationTile[] = [
    ...filteredSpecialApplications,
    ...selectedApplications
  ];

  return (
    <>
      <div className={`w-full h-full flex flex-col rounded-t-md rounded-b-md overflow-hidden`}>
      {/* Header - matching UserManagement style */}
      <div className={`flex items-center justify-between px-3 py-2 mb-1 ${theme.mainContentSection}`}>
        <div className={`text-md font-semibold ${theme.title}`}>My Applications</div>
        <div className="flex items-center space-x-2">
          <button
            onClick={loadApplications}
            disabled={isLoading}
            className="w-8 h-6 bg-blue-400 text-white rounded-[4px] text-xs hover:bg-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            title="Refresh"
          >
            <i className="fa fa-refresh"></i>
          </button>
        </div>
      </div>

      {/* Content Area - matching UserManagement body style */}
      <div className={`flex-auto overflow-hidden ${theme.mainContentSection}`}>
        <div className="h-full w-full overflow-auto px-5 py-5">
          <div className="flex flex-wrap gap-2 justify-start items-start">
          {allApplications.map((application) => (
            <div
              key={application.Id}
              className="w-[320px] h-[240px] p-4 cursor-pointer application-card-group group relative flex-auto overflow-visible"
            >
              <div 
                className={`w-full h-full rounded-lg ${theme.mainContentSection} border p-4 flex flex-col items-center justify-center transition-all duration-200 shadow-sm hover:shadow-md relative`}
                style={{ overflow: 'visible' }}
                onClick={(e) => {
                  // Only handle click if not clicking on context menu
                  if (!(e.target as HTMLElement).closest('.application-context-menu')) {
                    handleApplicationClick(application, e);
                  }
                }}
              >
                {/* Icon */}
                <div className="w-16 h-16 mb-3 flex items-center justify-center">
                  {isFontAwesomeIcon(application.ImageUrl) ? (
                    <i className={`fa-solid ${application.ImageUrl} text-5xl`} style={{ color: '#3987D9' }}></i>
                  ) : (
                    // Fallback to default Font Awesome icon if ImageUrl is not a FA icon
                    <i className="fa-solid fa-cube text-5xl" style={{ color: '#3987D9' }}></i>
                  )}
                </div>

                {/* Name */}
                <div className={`text-center font-semibold text-sm mb-1 ${theme.title} truncate w-full`}>
                  {application.Name}
                </div>

                {/* Description */}
                {application.Description && (
                  <div className={`text-center text-xs text-gray-600 truncate w-full`}>
                    {application.Description}
                  </div>
                )}

                {/* Context Menu (on hover) — not shown for Add Applications (click creates from scratch) */}
                {application.Id !== 'add-applications' && (
                <div 
                  className={`absolute left-0 right-0 bg-white rounded-b-lg flex -bottom-[45px] h-[45px] p-[13px] group-hover:bottom-0 shadow-[rgba(0,0,0,0.2)] invisible opacity-0 group-hover:visible group-hover:opacity-100 hover:visible hover:opacity-100 transition-all duration-500`}
                >
                    {application.IsSpecial ? (
                      // Special application context menus
                      application.Id === 'tutorial' ? (
                        <div
                          className="flex-auto w-full cursor-pointer text-center text-black text-sm font-semibold px-2 hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleApplicationClick(application);
                          }}
                        >
                          Open Tutorial
                        </div>
                      ) : application.Id === 'settings' ? (
                        <div
                          className="flex-auto w-full cursor-pointer text-center text-black text-sm font-semibold px-2 hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleApplicationClick(application);
                          }}
                        >
                          Open Setup
                        </div>
                      ) : application.Id === 'api-management' ? (
                        <div
                          className="flex-auto w-full cursor-pointer text-center text-black text-sm font-semibold px-2 border-l border-gray-200 hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleApplicationClick(application);
                          }}
                        >
                          Open Builder
                        </div>
                      ) : null
                    ) : (
                      // Dynamic application context menu (user-created)
                      <>
                        <div
                          className="flex-auto w-full cursor-pointer text-right text-black text-sm font-semibold px-2 hover:underline"
                          onClick={(e) => handleOpenApplication(application, e)}
                        >
                          Open
                        </div>
                        <div
                          className="flex-auto w-full cursor-pointer text-center text-black text-sm font-semibold px-2 border-l border-gray-200 hover:underline"
                          onClick={(e) => {
                            e.stopPropagation();
                            // Open configuration
                            const paramObj = { id: application.Id };
                            let routeBasePath = '/my-application-editor';
                            if (application.GlobalGuid) {
                              const ESiteConfigurationRootMenuGuid = '706026CA-27FE-4A7D-B0BF-03DC2966DB74';
                              const AppWebSiteConfigurationRootMenuGuid = '9F92CEB7-8EA9-4E2D-AD4A-00293DD06F4B';
                              const globalGuidLower = application.GlobalGuid.toLowerCase();
                              if (globalGuidLower === ESiteConfigurationRootMenuGuid.toLowerCase()) {
                                routeBasePath = '/esite-management';
                              } else if (globalGuidLower === AppWebSiteConfigurationRootMenuGuid.toLowerCase()) {
                                routeBasePath = '/app-website-management';
                              }
                            }
                            addTabAndNavigate(routeBasePath, `Config: ${application.Name}`, paramObj, true);
                          }}
                        >
                          Configuration
                        </div>
                        <div
                          className="flex-auto w-full cursor-pointer text-left text-black text-sm font-semibold px-2 border-l border-gray-200 hover:underline"
                          onClick={(e) => handleRemoveApplication(application, e)}
                        >
                          {application.IsImportedFromOtherDB ? 'Uninstall' : 'Remove'}
                        </div>
                      </>
                    )}
                </div>
                )}
              </div>
            </div>
          ))}

            {/* Placeholder tiles for spacing (matching AngularJS layout) */}
            {Array.from({ length: 10 }).map((_, index) => (
              <div key={`placeholder-${index}`} className="w-[320px] h-[240px] p-4 flex-auto" />
            ))}
          </div>
        </div>
      </div>
    </div>
    </>
  );
};

export default MyApplications;
