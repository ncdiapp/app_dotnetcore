import React, { useEffect, useRef, useState } from 'react';
import { Outlet, useLocation, useNavigate } from 'react-router-dom';
import { useDispatch, useSelector } from 'react-redux';
import { startTransition } from 'react';
import Header from './Header';
import Sidebar from  './Sidebar';
import TabHeaders from './TabHeaders';
import { RootState } from '../../redux/store';
import { store } from '../../redux/store';
import { useTheme } from '../../redux/hooks/useTheme';
import { setAutoCollapseOnPageOpen, collapseSidebar } from '../../redux/features/ui/navigation/sidebarSlice';
import { restoreTabs, Tab } from '../../redux/features/ui/navigation/tabnavSlice';
import { isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';

const LandingPage: React.FC = () => {
  const dispatch = useDispatch();
  const location = useLocation();
  const navigate = useNavigate();
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { t, theme } = useTheme();
  const isSidebarCollapsed = useSelector((state: RootState) => state.sidebar.isCollapsed);
  const { userContext } = useSelector((state: RootState) => state.userSession);
  const { tabs, activeTabKey } = useSelector((state: RootState) => state.tabnav);
  const mainContentRef = useRef<HTMLDivElement>(null);
  const [tabsRestored, setTabsRestored] = useState(false);

  // Always enable auto-collapse (following AngularJS behavior - always collapse on page open/click)
  useEffect(() => {
    dispatch(setAutoCollapseOnPageOpen(true));
  }, [dispatch]);

  // SysAdmin has no tenant Desktop; keep them off /home if tabs or bookmarks point there.
  useEffect(() => {
    if (!userContext || userContext.IsLoginFailed) return;
    if (!isMasterSysAdminFromContext(userContext)) return;
    const path = location.pathname;
    if (path === '/home' || path.startsWith('/home/')) {
      navigate('/company-security', { replace: true });
    }
  }, [userContext, location.pathname, navigate]);

  // Restore tabs from localStorage after session is restored
  useEffect(() => {
    // Only restore tabs once, and only if user is authenticated
    if (!tabsRestored && userContext && !userContext.IsLoginFailed) {
      try {
        const stored = localStorage.getItem('tabsState');
        if (stored) {
          const parsed = JSON.parse(stored);
          
          if (parsed.tabs && Array.isArray(parsed.tabs) && parsed.tabs.length > 0) {
            // Find tab matching current URL BEFORE restoring
            const currentPath = location.pathname;
            
            // Helper function to normalize path for comparison
            // This handles both encoded and decoded paths
            const normalizePath = (path: string): string => {
              if (!path) return '/';
              // Decode the path to handle URL encoding
              try {
                return decodeURIComponent(path);
              } catch {
                return path;
              }
            };

            // Helper function to extract base path (without encoded parameters)
            const extractBasePath = (path: string): string => {
              if (!path) return '/';
              const parts = path.split('/').filter(p => p);
              if (parts.length === 0) return '/';
              // Remove the last part if it's an encoded JSON parameter (starts with %)
              const lastPart = parts[parts.length - 1];
              if (lastPart && lastPart.startsWith('%')) {
                return '/' + parts.slice(0, -1).join('/');
              }
              // Also check if it's a JSON object (decoded)
              try {
                JSON.parse(lastPart);
                return '/' + parts.slice(0, -1).join('/');
              } catch {
                // Not JSON, return full path
                return '/' + parts.join('/');
              }
            };

            // Helper function to extract and compare parameters
            const extractParams = (path: string): any => {
              try {
                const parts = path.split('/').filter(p => p);
                if (parts.length === 0) return null;
                const lastPart = parts[parts.length - 1];
                if (lastPart && lastPart.startsWith('%')) {
                  const decoded = decodeURIComponent(lastPart);
                  return JSON.parse(decoded);
                }
                // Try parsing as JSON directly
                return JSON.parse(lastPart);
              } catch {
                return null;
              }
            };

            // Helper function to check if two paths match (considering base path and parameters)
            const pathsMatch = (tabPath: string, urlPath: string): boolean => {
              // Exact match (both encoded or both decoded)
              if (tabPath === urlPath) {
                return true;
              }
              
              // Normalize both paths and compare
              const normalizedTab = normalizePath(tabPath);
              const normalizedUrl = normalizePath(urlPath);
              if (normalizedTab === normalizedUrl) {
                return true;
              }
              
              // Extract base paths
              const tabBase = extractBasePath(tabPath);
              const urlBase = extractBasePath(urlPath);
              
              // If base paths match, compare parameters
              if (tabBase === urlBase && tabBase !== '/') {
                const tabParams = extractParams(tabPath);
                const urlParams = extractParams(urlPath);
                
                // If both have parameters, compare them (ignoring isNavigatedFromTab)
                if (tabParams && urlParams) {
                  const tabParamsFiltered = { ...tabParams };
                  const urlParamsFiltered = { ...urlParams };
                  delete tabParamsFiltered.isNavigatedFromTab;
                  delete urlParamsFiltered.isNavigatedFromTab;
                  
                  // Compare parameter keys and values
                  const tabKeys = Object.keys(tabParamsFiltered).sort();
                  const urlKeys = Object.keys(urlParamsFiltered).sort();
                  
                  if (tabKeys.length === urlKeys.length) {
                    return tabKeys.every(key => 
                      tabParamsFiltered[key] === urlParamsFiltered[key]
                    );
                  }
                }
                
                // If base paths match, they're the same route
                return true;
              }
              
              // Special case: /home and /my-applications both render MyApplications
              if ((tabBase === '/home' && urlBase === '/my-applications') ||
                  (tabBase === '/my-applications' && urlBase === '/home')) {
                return true;
              }
              
              return false;
            };

            // Find matching tab
            const matchingTab = parsed.tabs.find((tab: Tab) => pathsMatch(tab.path, currentPath));

            // Determine which tab should be active (matching tab or saved activeTabKey)
            let tabKeyToActivate = matchingTab ? matchingTab.tabKey : (parsed.activeTabKey || null);
            
            // If no matching tab and no saved activeTabKey, try to find by base path
            if (!tabKeyToActivate) {
              const currentBasePath = extractBasePath(currentPath);
              const basePathMatch = parsed.tabs.find((tab: Tab) => {
                const tabBasePath = extractBasePath(tab.path);
                return tabBasePath === currentBasePath && currentBasePath !== '/';
              });
              if (basePathMatch) {
                tabKeyToActivate = basePathMatch.tabKey;
              } else if (currentPath === '/home' || currentPath === '/') {
                const homeTab = parsed.tabs.find((tab: Tab) => tab.tabKey === 'home-tab');
                if (homeTab) {
                  tabKeyToActivate = homeTab.tabKey;
                }
              }
            }

            // Restore tabs with the correct active tab
            dispatch(restoreTabs({
              tabs: parsed.tabs,
              activeTabKey: tabKeyToActivate,
            }));
            
            // Verify restoration after a short delay
            setTimeout(() => {
              const currentState = store.getState();
              const restoredTabs = currentState.tabnav.tabs;
              
              // Check if tabs were overwritten
              if (restoredTabs.length !== parsed.tabs.length) {
                console.error('[LandingPage] WARNING: Tab count mismatch after restore! Expected:', parsed.tabs.length, 'Got:', restoredTabs.length);
              }
            }, 200);

            // Navigate to the active tab's path if it doesn't match current URL
            // Use a delay to ensure restore completes before navigation
            // IMPORTANT: Only navigate if the path is different, and don't navigate if we're already on the correct path
            if (tabKeyToActivate) {
              const activeTab = parsed.tabs.find((tab: Tab) => tab.tabKey === tabKeyToActivate);
              if (activeTab) {
                // Check if paths match (using the same matching logic)
                const pathsMatch = (tabPath: string, urlPath: string): boolean => {
                  if (tabPath === urlPath) return true;
                  try {
                    const normalizedTab = decodeURIComponent(tabPath);
                    const normalizedUrl = decodeURIComponent(urlPath);
                    if (normalizedTab === normalizedUrl) return true;
                  } catch {}
                  return false;
                };
                
                if (!pathsMatch(activeTab.path, currentPath)) {
                  setTimeout(() => {
                    startTransition(() => {
                      navigate(activeTab.path);
                    });
                  }, 100); // Increase delay to ensure restore completes
                }
              }
            }
          }
        } else {
          // IMPORTANT: Even if no tabs were found, we need to mark that restoration attempt has completed
          // This ensures that future tab actions will be saved to localStorage
          // Dispatch a restoreTabs action with current state to set hasRestoredTabsOnce flag in middleware
          const currentState = store.getState();
          dispatch(restoreTabs({
            tabs: currentState.tabnav.tabs,
            activeTabKey: currentState.tabnav.activeTabKey
          }));
        }
      } catch (error) {
        console.error('Failed to restore tabs from localStorage:', error);
        // Even on error, mark restoration as complete so future actions can be saved
        const currentState = store.getState();
        dispatch(restoreTabs({
          tabs: currentState.tabnav.tabs,
          activeTabKey: currentState.tabnav.activeTabKey
        }));
      } finally {
        setTabsRestored(true);
      }
    }
  }, [userContext, tabsRestored, location.pathname, dispatch, navigate, tabs.length]);

  // Handle main body click to collapse sidebar (following AngularJS pageClicked behavior)
  const handleMainBodyClick = (e: React.MouseEvent) => {
    // Only collapse if click is outside the sidebar
    const target = e.target as HTMLElement;
    const sidebarElement = document.querySelector('[data-sidebar-container]');
    
    if (sidebarElement && !sidebarElement.contains(target) && !isSidebarCollapsed) {
      dispatch(collapseSidebar());
    }
  };

  return (
    <div className={`${theme.default} ${t('scrollbar_style')} w-full h-screen flex flex-col overflow-hidden`}>
      <Header />
      <div className="w-full h-1 flex-auto flex">
        {!isSidebarCollapsed && <Sidebar />}
        <main 
          ref={mainContentRef}
          className="w-1 flex-auto flex flex-col"
          onClick={handleMainBodyClick}
        >
          <TabHeaders />
          <div className="flex h-1 min-h-0 w-full flex-auto flex-col p-4">
            <div className="flex h-full min-h-0 w-full flex-col overflow-auto">
              <Outlet key={`${activeTabKey ?? ''}-${location.pathname}`} />
            </div>
          </div>
        </main>
      </div>
    </div>
  );
};

export default LandingPage; 

