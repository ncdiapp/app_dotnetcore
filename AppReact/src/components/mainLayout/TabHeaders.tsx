import React, { useState, useRef, useEffect } from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { useNavigate, useLocation } from 'react-router-dom';
import { RootState, store } from '../../redux/store';
import { activateTab, closeTab, Tab } from '../../redux/features/ui/navigation/tabnavSlice';
import { collapseSidebar } from '../../redux/features/ui/navigation/sidebarSlice';
//import { cacheCurrentTabData } from '../../redux/hooks/useTabNavigation';
import { useTheme } from '../../redux/hooks/useTheme';
import { resolveTabNavigationPath, tabRoutePathsMatch } from '../../helper/navigationHelper';

const TabHeaders: React.FC = () => {
  //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
  const { tabs, previousActiveTabKey } = useSelector((state: RootState) => state.tabnav);
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const { t, theme } = useTheme();
  const [hoveredTabKey, setHoveredTabKey] = useState<string | null>(null);
  const [openDropdownTabKey, setOpenDropdownTabKey] = useState<string | null>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const tabBarRef = useRef<HTMLDivElement>(null);

  const navigateToTabKey = (tabKey: string) => {
    const tab = store.getState().tabnav.tabs.find((t) => t.tabKey === tabKey);
    if (!tab) return;
    const path = resolveTabNavigationPath(tab);
    if (!path) return;
    navigate(path);
  };

  const handleTabClick = (tab: Tab) => {
    dispatch(activateTab(tab.tabKey));
    dispatch(collapseSidebar());
    navigateToTabKey(tab.tabKey);
  };

  const handleCloseTab = (e: React.MouseEvent, tab: Tab) => {
    e.stopPropagation();
    
    // Prevent closing the last tab if it's the Home tab
    if (tabs.length === 1 && (tab.path === '/home' || tab.path === '/company-security')) {
      return;
    }
    
    if (tab.isClosable !== false) {
      const wasActiveTab = tab.isActive;
      const isViewingClosedTab = tabRoutePathsMatch(tab.path, location.pathname);

      dispatch(closeTab(tab.tabKey));

      if (wasActiveTab || isViewingClosedTab) {
        setTimeout(() => {
          const nextActiveKey = store.getState().tabnav.activeTabKey;
          if (nextActiveKey) {
            navigateToTabKey(nextActiveKey);
          }
        }, 0);
      }
    }
    setOpenDropdownTabKey(null);
  };

  const handleCloseOtherTabs = (e: React.MouseEvent, currentTab: Tab) => {
    e.stopPropagation();
    
    // Close all tabs except the current one
    tabs.forEach(tab => {
      if (tab.tabKey !== currentTab.tabKey && tab.isClosable !== false) {
        dispatch(closeTab(tab.tabKey));
      }
    });
    
    // Activate the current tab if it's not already active
    if (!currentTab.isActive) {
      dispatch(activateTab(currentTab.tabKey));
      navigateToTabKey(currentTab.tabKey);
    }
    
    setOpenDropdownTabKey(null);
  };

  const handleDropdownToggle = (e: React.MouseEvent, tabKey: string) => {
    e.stopPropagation();
    setOpenDropdownTabKey(openDropdownTabKey === tabKey ? null : tabKey);
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpenDropdownTabKey(null);
      }
    };

    if (openDropdownTabKey) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }
  }, [openDropdownTabKey]);

  return (
    <div className="w-full flex-none">
      <div className={`border-b ${t('border_default')} w-full`}>
        <div 
          ref={tabBarRef}
          className="w-full flex flex-wrap relative"
          aria-label="Tabs"
        >
          {tabs.map((tab) => (
            <div
              key={tab.tabKey}
              className="relative"
              onMouseEnter={() => setHoveredTabKey(tab.tabKey)}
              onMouseLeave={() => setHoveredTabKey(null)}
            >
              <div
                className={`
                  min-w-[180px] px-6 py-1 text-xs border-r relative
                  focus:outline-none transition-colors duration-200 flex items-center cursor-pointer
                  ${tab.isActive 
                    ? `border-b-2 ${theme.tab_active}` 
                    : `${theme.tab}`}
                `}
                style={tab.isActive ? { color: theme.param.text_tab_active } : undefined}
                onClick={() => handleTabClick(tab)}
                role="button"
                tabIndex={0}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    handleTabClick(tab);
                  }
                }}
                aria-current={tab.isActive ? "page" : undefined}
              >
                <div className="flex-auto w-1 h-5 flex items-center">
                  <span className="w-ful truncate"> {tab.label}</span>
                 </div>
                {/* Only show dropdown/close buttons if there are multiple tabs, or if this is not the Home tab */}
                {(tabs.length > 1 || tab.tabKey !== 'home-tab') && (
                  <div 
                    className={`absolute h-full px-2 right-0 top-1/2 -translate-y-1/2 flex items-center gap-0.5 ${t('bg_default_hover')} ${hoveredTabKey === tab.tabKey ? 'opacity-100' : 'opacity-0'}`}
                    onClick={(e) => e.stopPropagation()}
                  >
                    {/* Dropdown button - 2 times smaller */}
                    <button
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDropdownToggle(e, tab.tabKey);
                      }}
                      className={`w-3 h-4 flex items-center justify-center ${t('bg_default_hover')} rounded text-[8px] ${theme.contextMenu} leading-none`}
                      title="Tab options"
                    >
                      {openDropdownTabKey === tab.tabKey ? '▲' : '▼'}
                    </button>
                    {/* Close button */}
                    {tab.isClosable !== false && (
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          handleCloseTab(e, tab);
                        }}
                        className={`w-3 h-4 rounded-full flex items-center justify-center ${t('bg_default_hover')} text-xs hover:bg-red-200`}
                        title="Close tab"
                      >
                        ×
                      </button>
                    )}
                  </div>
                )}
              </div>
              
              {/* Dropdown menu */}
              {openDropdownTabKey === tab.tabKey && (
                <div
                  ref={dropdownRef}
                  className={`absolute top-full left-0 mt-1 border rounded shadow-lg z-50 min-w-[150px] ${theme.mainContentSection} ${t('border_mainContentSection')}`}
                >
                  {/* Hide "Close tab" for Home tab */}
                  {tab.tabKey !== 'home-tab' && (
                    <button
                      type="button"
                      onClick={(e) => handleCloseTab(e, tab)}
                      className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 ${theme.contextMenu}`}
                    >
                      <span>Close tab</span>
                    </button>
                  )}
                  {tabs.length > 1 && (
                    <button
                      type="button"
                      onClick={(e) => handleCloseOtherTabs(e, tab)}
                      className={`w-full text-left px-4 py-2 text-sm flex items-center gap-2 ${theme.contextMenu} ${tab.tabKey !== 'home-tab' ? `border-t ${t('border_mainContentSection')}` : ''}`}
                    >
                      <span>Close other tabs</span>
                    </button>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default TabHeaders; 