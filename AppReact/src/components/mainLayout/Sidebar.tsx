import React, { useState, useMemo } from 'react';
import { useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import { useTheme } from '../../redux/hooks/useTheme';
import { isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';

// GlobalGuid constants for routing (matching AngularJS implementation)
const ESiteConfigurationRootMenuGuid = '706026CA-27FE-4A7D-B0BF-03DC2966DB74';
const AppWebSiteConfigurationRootMenuGuid = '9F92CEB7-8EA9-4E2D-AD4A-00293DD06F4B';

const Sidebar: React.FC = () => {
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { t, theme } = useTheme();
  const userMenu = useSelector((state: RootState) => state.userSession.userMenu);
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  const isSysAdmin = isMasterSysAdminFromContext(userContext);
  const { addTabFromListMenu, addTabAndNavigate } = useTabNavigation();
  
  // State to track which parent menus are expanded
  const [expandedMenus, setExpandedMenus] = useState<Set<string>>(new Set());

  // State to track the currently selected (active) leaf menu item
  const [selectedMenuId, setSelectedMenuId] = useState<string | null>(null);
  
  // Helper function to sort menu items by Sort property (following AngularJS logic)
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
  
  // Prevent sidebar clicks from collapsing the sidebar
  const handleSidebarClick = (e: React.MouseEvent) => {
    e.stopPropagation();
  };

  // Hardcoded menu for master-DB SysAdmin only (DomainId=1).
  // SysAdmin's sole responsibility: tenant infrastructure management.
  // No tenant-level features (AI Agent, App Config, Workflow, API Mgmt, etc.)
  const sysAdminMenusRaw = [
    {
      Id: 'sysadmin-tenant',
      Sort: 1,
      Name: 'Tenant Administration',
      AppListMenu_List: [
        { Id: 'sysadmin-tenant-provisioning', Name: 'Tenant Provisioning', RouteCode: '/tenant-provisioning' },
        { Id: 'sysadmin-company-security', Name: 'Company and Users', RouteCode: '/company-security' },
        { Id: 'sysadmin-database-registration', Name: 'Database Registration', RouteCode: 'database-registration' },
      ]
    },
    {
      Id: 'sysadmin-system',
      Sort: 2,
      Name: 'System',
      AppListMenu_List: [
        { Id: 'sysadmin-system-setting', Name: 'System Setting', RouteCode: 'system-setting' },
      ]
    },
  ];

  // Static menu data for hard-coded menus (will be sorted)
  const staticMenusRaw = [
    {
      Id:'system-settings',
      Sort: 1000000,
      Name: 'System Settings',
      AppListMenu_List: [        
        {
          Id:'company-security',
          Name: 'Company and Users',
          RouteCode: '/company-security'
        },
        {
          Id:'database-registration',
          Name: 'Database Registration',
          RouteCode: 'database-registration'
        },
        {
          Id:'db-driver-management',
          Name: 'DB Drivers',
          RouteCode: 'db-driver-management'
        },
        {
          Id:'system-setting',
          Name: 'System Setting',
          RouteCode: 'system-setting'
        },
        {
          Id:'root-menu-management',
          Name: 'Menu Management',
          RouteCode: '/root-menu-management'
        }
      ]
    },
    {
      Id:'application-configuration',
      Sort: 2,
      Name: 'Application Configuration',
      AppListMenu_List: [     
        // Add root-level application menu items as dynamic sub-menus
        // These will be populated from rootLevelMenuItems (computed separately)
        // Format: "Config: {ApplicationName}"
        {
          Id:'dashboard-management',
          Name: 'Dashboard Management',
          RouteCode: '/dashboard-management'
        },
        {
          Id:'nextjs-app-design',
          Name: 'NextJs App Design',
          RouteCode: '/nextjs-app-design'
        }
      ]
    },
    {
      Id: 'sidebar-database-management',
      Sort: 2.5,
      Name: 'Database Management',
      RouteCode: '/database-design-management',
      AppListMenu_List: []
    },
    {
      Id: 'sidebar-ai-agent',
      Sort: 0,
      Name: 'AI Agent',
      AppListMenu_List: [
        { Id: 'ai-agent-dba', Name: 'DBA Agent', RouteCode: '/DbaGenie' },
        { Id: 'ai-agent-app-builder', Name: 'App Builder Agent', RouteCode: '/app-builder-agent' },
        { Id: 'ai-agent-app-report', Name: 'App Report Agent', RouteCode: '/app-report-agent' },

        { Id: 'ai-agent-skill-mgt', Name: 'AI Skill Management', RouteCode: '/ai-skill-management' },
      ]
    },
    {
      Id: 'sidebar-workflow-automation',
      Sort: 2.6,
      Name: 'Workflow Automation',
      RouteCode: '/workflow-automation-management',
      AppListMenu_List: []
    },
    {
      Id:'api-management',
      Sort: 3,
      Name: 'API Management',
      AppListMenu_List: [
        {
          Id:'third-party-api-provider-management',
          Name: '3rd Party API Provider',
          RouteCode: '/third-party-api-provider-management'
        },
        {
          Id:'app-api-provider',
          Name: 'App API Provider',
          RouteCode: '/app-api-provider'
        }
      ]
    },
    {
      Id:'test-examples',
      Sort: 1000001,
      Name: 'Test',
      AppListMenu_List: [
        {
          Id:'main-theme',
          Name: 'Default Theme Template',
          RouteCode: 'maintheme'
        },
        {
          Id:'wijmo-theme',
          Name: 'Wijmo Theme Template',
          RouteCode: 'wijmotheme'
        },
        {
          Id:'test-search',
          Link: 8595,
          Name: 'Test Search',
          RouteCode: 'MasterDataManagement'
        },
        {
          Id:'test-ddl',
          Name: 'Test ComboBox (DDL)',
          RouteCode: 'test-ddl'
        },
        {
          Id:'test-dom-drag-and-drop',
          Name: 'Test DOM Drag and Drop',
          RouteCode: 'test-dom-drag-and-drop'
        }
      ]
    },
  ];
  
  // Extract root-level menu items only (like threeLevelMenuList in AngularJS)
  // These are the applications that appear as "Config: {ApplicationName}" sub-menus
  const rootLevelMenuItems = useMemo(() => {
    if (!userMenu || !Array.isArray(userMenu)) return [];
    // Return only root-level items (top-level items in the hierarchy)
    // These represent application packages
    return userMenu.filter((item: any) => item && item.Id && item.Name);
  }, [userMenu]);

  // Build Application Configuration menu with dynamic sub-menus
  const applicationConfigurationMenu = useMemo(() => {
    const baseMenu = staticMenusRaw.find((m: any) => m.Id === 'application-configuration');
    if (!baseMenu) return null;

    // Add root-level application items as dynamic sub-menus
    const dynamicSubMenus = rootLevelMenuItems.map((item: any) => ({
      Id: `config-${item.Id}`,
      Name: `Config: ${item.Name}`,
      RouteCode: '/my-application-editor', // Default, will be overridden by handler
      OriginalMenuId: item.Id, // Store original ID for handler
      GlobalGuid: item.GlobalGuid // Store GlobalGuid for routing
    }));

    return {
      ...baseMenu,
      Sort: baseMenu.Sort || 2, // Preserve Sort property
      AppListMenu_List: [
        ...dynamicSubMenus,
        ...(baseMenu.AppListMenu_List || [])
      ]
    };
  }, [rootLevelMenuItems]);

  // Sort static menus and user menu by Sort property (following AngularJS logic)
  const staticMenus = useMemo(() => {
    const menus = staticMenusRaw.filter((m: any) => m.Id !== 'application-configuration');
    if (applicationConfigurationMenu) {
      return sortMenuItems([...menus, applicationConfigurationMenu]);
    }
    return sortMenuItems(menus);
  }, [applicationConfigurationMenu]);

  const sortedUserMenu = useMemo(() => {
    if (!userMenu) return null;
    return sortMenuItems(userMenu);
  }, [userMenu]);

  // Combined menu list — SysAdmin gets a dedicated hardcoded set; tenant users get static + user menus.
  const combinedMenus = useMemo(() => {
    if (isSysAdmin) {
      return sortMenuItems(sysAdminMenusRaw.map((m: any) => ({ ...m, _fromUserMenu: false })));
    }
    const tagged = staticMenus.map((m: any) => ({ ...m, _fromUserMenu: false }));
    const userTagged = sortedUserMenu ? sortedUserMenu.map((m: any) => {
      const nameLower = (m.Name ?? '').toLowerCase();
      const displayName = nameLower === 'project and workflow' ? 'Project and Task' : m.Name;
      return { ...m, Name: displayName, _fromUserMenu: true };
    }) : [];
    return sortMenuItems([...tagged, ...userTagged]);
  }, [isSysAdmin, staticMenus, sortedUserMenu]);

  // Helper function to find all parent IDs for a menu
  const findParentIds = (menuId: string): string[] => {
    const parentIds: any[] = [];
    
    // Check in static menus (using sorted version)
    for (const menu of staticMenus) {
      if (menu.Id === menuId) {
        return parentIds; // Top level menu, no parents
      }
      if (menu.AppListMenu_List) {
        for (const child of menu.AppListMenu_List) {
          if (child.Id === menuId) {
            parentIds.push(menu.Id);
            return parentIds;
          }
          if ((child as any).AppListMenu_List) {
            for (const grandChild of (child as any).AppListMenu_List) {
              if (grandChild.Id === menuId) {
                parentIds.push(menu.Id);
                parentIds.push(child.Id);
                return parentIds;
              }
            }
          }
        }
      }
    }
    
    // Check in user menu (using sorted version)
    if (sortedUserMenu) {
      for (const menu of sortedUserMenu) {
        if (menu.Id === menuId) {
          return parentIds; // Top level menu, no parents
        }
        if (menu.AppListMenu_List) {
          for (const child of menu.AppListMenu_List) {
            if (child.Id === menuId) {
              parentIds.push(menu.Id);
              return parentIds;
            }
          }
        }
      }
    }
    
    return parentIds;
  };

  // Handler for opening application editor (matching AngularJS openMyApplicationEditor)
  const openMyApplicationEditor = (menuId: string, clickEvent?: React.MouseEvent) => {
    if (clickEvent) {
      clickEvent.stopPropagation();
    }

    if (!menuId || !userMenu) return;

    // Find the menu item from userMenu using the Id
    const packageRootMenu = userMenu.find((item: any) => item.Id === menuId || item.Id === parseInt(menuId));
    
    if (!packageRootMenu) return;

    const displayName = `Config: ${packageRootMenu.Name}`;
    let routeBasePath = '/my-application-editor';
    const paramObj = { id: menuId };

    // Route based on GlobalGuid (matching AngularJS logic)
    if (packageRootMenu.GlobalGuid) {
      const globalGuidLower = packageRootMenu.GlobalGuid.toLowerCase();
      if (globalGuidLower === ESiteConfigurationRootMenuGuid.toLowerCase()) {
        routeBasePath = '/esite-management';
      } else if (globalGuidLower === AppWebSiteConfigurationRootMenuGuid.toLowerCase()) {
        routeBasePath = '/app-website-management';
      }
    }

    // Open in a new tab
    addTabAndNavigate(routeBasePath, displayName, paramObj, true);
  };

  const handleMenuClick = (menuObj: any, clickEvent?: React.MouseEvent) => {
    setSelectedMenuId(menuObj.Id?.toString() ?? null);

    // Check if this is an application configuration menu item
    if (menuObj.Id && menuObj.Id.toString().startsWith('config-')) {
      // Use OriginalMenuId if available, otherwise extract from Id
      const originalMenuId = menuObj.OriginalMenuId || menuObj.Id.replace('config-', '');
      openMyApplicationEditor(originalMenuId, clickEvent);
      return;
    }

    addTabFromListMenu(menuObj);
    // Note: Auto-collapse is handled in useTabNavigation hook
  };

  // Toggle menu expansion - keep parent/grandparent expanded when clicking child
  const handleMenuToggle = (menuId: string) => {
    setExpandedMenus(prev => {
      const newSet = new Set<string>();
      
      if (!prev.has(menuId)) {
        // If menu is not expanded, expand it and keep its parents expanded
        // Find and add all parent menus
        const parentIds = findParentIds(menuId);
        parentIds.forEach(id => newSet.add(id));
        newSet.add(menuId);
      }
      // If menu is already expanded, just collapse it (newSet remains empty)
      
      return newSet;
    });
  };

  // Check if a menu has children (you may need to adjust this based on your data structure)
  const hasChildren = (menu: any) => {
    return menu.AppListMenu_List && menu.AppListMenu_List.length > 0;
  };

  // Check if a menu is expanded
  const isExpanded = (menuId: string) => {
    return expandedMenus.has(menuId);
  };

  // Helper function to check if a menu is from user menu (dynamic) vs static menu
  const _isUserMenu = (menuId: string): boolean => {
    if (!sortedUserMenu) return false;
    return sortedUserMenu.some((item: any) => {
      if (item.Id === menuId) return true;
      // Check recursively in children
      const checkChildren = (children: any[]): boolean => {
        return children.some((child: any) => {
          if (child.Id === menuId) return true;
          if (child.AppListMenu_List && child.AppListMenu_List.length > 0) {
            return checkChildren(child.AppListMenu_List);
          }
          return false;
        });
      };
      if (item.AppListMenu_List && item.AppListMenu_List.length > 0) {
        return checkChildren(item.AppListMenu_List);
      }
      return false;
    });
  };

  // Render menu item with children
  const renderMenuItem = (menu: any, isFromUserMenu: boolean = false) => {
    const hasChildMenus = hasChildren(menu);
    const expanded = isExpanded(menu.Id);

    // Special handling for Application Configuration menu - navigate to My Applications
    const isApplicationConfiguration = (m: any) =>
      m?.Id === 'application-configuration' ||
      (m?.Name && String(m.Name).toLowerCase().includes('application configuration'));

    const handleParentMenuClick = (e: React.MouseEvent) => {
      e.preventDefault();
      // If clicking on the arrow, just toggle expansion (don't navigate)
      if ((e.target as HTMLElement).closest('svg')) {
        if (hasChildMenus) {
          handleMenuToggle(menu.Id);
        }
        return;
      }

      if (isApplicationConfiguration(menu)) {
        addTabAndNavigate('/my-applications', 'My Applications', {}, true);
        if (hasChildMenus && !expanded) {
          handleMenuToggle(menu.Id);
        }
        return;
      }

      if (hasChildMenus) {
        handleMenuToggle(menu.Id);
      } else {
        setSelectedMenuId(menu.Id?.toString() ?? null);
        handleMenuClick(menu);
      }
    };

    return (
      <div key={menu.Id} className="mb-6">
        <div 
          onClick={handleParentMenuClick}
          className={`cursor-pointer flex items-center text-[12px] font-bold uppercase tracking-wider mb-4 px-3 rounded-[4px] group ${!hasChildMenus && selectedMenuId === menu.Id?.toString() ? theme.sideBar_menu_active : theme.sideBar_menu}`}
        >
          {menu.Name}
          {hasChildMenus && (
            <svg 
              className={`w-4 h-4 ml-auto transform transition-transform duration-200 ${expanded ? 'rotate-180' : ''}`} 
              fill="none"
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M19 9l-7 7-7-7" />
            </svg>
          )}
        </div>
        
        {hasChildMenus && expanded && (
          <ul className="pl-4 space-y-1">
            {menu.AppListMenu_List.map((child: any) => (
              <li key={child.Id}>
                <div
                  onClick={(e) => {
                    e.preventDefault();
                    if (isApplicationConfiguration(child)) {
                      addTabAndNavigate('/my-applications', 'My Applications', {}, true);
                      if (hasChildren(child) && !isExpanded(child.Id)) {
                        handleMenuToggle(child.Id);
                      }
                      return;
                    }
                    if (hasChildren(child)) {
                      handleMenuToggle(child.Id);
                    } else {
                      handleMenuClick(child, e);
                    }
                  }}
                  className={`cursor-pointer flex items-center px-1 py-2 text-[13px] rounded-[4px] group ${selectedMenuId === child.Id?.toString() ? theme.sideBar_menu_active : theme.sideBar_menu}`}
                >
                  <span className="w-8 h-8 mr-2 flex items-center justify-center">
                    {child.Id && child.Id.toString().startsWith('config-') ? (
                      // Config items: pencil icon
                      <i className="fa-solid fa-pencil text-xs"></i>
                    ) : isFromUserMenu ? (
                      // User application dynamic sub-menus: hollow document icon
                      <i className="fa-regular fa-file-lines text-xs"></i>
                    ) : (
                      // Static management items: gear icon
                      <i className="fa-solid fa-gear text-xs"></i>
                    )}
                  </span>
                  {child.Name}
                  {hasChildren(child) && (
                    <svg 
                      className={`w-4 h-4 ml-auto transform transition-transform duration-200 ${isExpanded(child.Id) ? 'rotate-180' : ''}`} 
                      fill="none"
                      stroke="currentColor" 
                      viewBox="0 0 24 24"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5" d="M19 9l-7 7-7-7" />
                    </svg>
                  )}
                </div>
                  {hasChildren(child) && isExpanded(child.Id) && (
                  <ul className="pl-8 space-y-1">
                    {child.AppListMenu_List.map((grandChild: any) => (
                      <li key={grandChild.Id}>
                        <div
                          onClick={() => handleMenuClick(grandChild)}
                          className={`cursor-pointer flex items-center px-1 py-2 text-[13px] rounded-[4px] group ${selectedMenuId === grandChild.Id?.toString() ? theme.sideBar_menu_active : theme.sideBar_menu}`}
                        >
                          <span className="w-6 h-6 mr-2 flex items-center justify-center">
                            {isFromUserMenu ? (
                              // User application dynamic sub-menus: hollow document icon
                              <i className="fa-regular fa-file-lines text-xs"></i>
                            ) : (
                              <svg className="w-4 h-4" viewBox="0 0 24 24" fill="none">
                                <path
                                  d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"
                                  stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                              </svg>
                            )}
                          </span>
                          {grandChild.Name}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  };

  // useEffect(() => {
  //   console.log('Sidebar Effect Run', "Sidebar");
  // }, []);

  return (
    <div 
      data-sidebar-container
      className={`w-[300px] flex-none h-full overflow-auto shadow-sm hidden lg:block border-r ${theme.sideBar}`}
      onClick={handleSidebarClick}
    >
      <div className="p-4">
        {combinedMenus.map((menu) => renderMenuItem(menu, menu._fromUserMenu === true))}
      </div>
    </div>
  );
};

export default Sidebar; 