import React, { useState, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { useNavigate } from 'react-router-dom';

import { logout } from '../../redux/features/admin/userSessionSlice';
import { setThemeById } from '../../redux/features/ui/theme/themeSlice';
import { toggleSidebar } from '../../redux/features/ui/navigation/sidebarSlice';
import { activateTab, clearTabsForLogout } from '../../redux/features/ui/navigation/tabnavSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { useTheme } from '../../redux/hooks/useTheme';
import { useTabNavigation } from '../../redux/hooks/useTabNavigation';
import ThemeManagementPanel from '../admin/ThemeManagementPanel';
import { appMessageService } from '../../webapi/appmessagesvc';
import { dashboardService } from '../../webapi/dashboardsvc';
import { APP_AI_REFRESH_UNREAD_MESSAGES } from '../message/messageUnreadRefresh';

const Header: React.FC = () => {
    const dispatch = useDispatch();
    const navigate = useNavigate();
    const { t, theme } = useTheme();
    const { addTabAndNavigate } = useTabNavigation();
    const { tabs } = useSelector((state: RootState) => state.tabnav);

    const [globalSearch, setGlobalSearch] = useState('');

    const handleGlobalSearch = () => {
        const q = globalSearch.trim();
        if (!q) return;
        setGlobalSearch('');
        addTabAndNavigate('/app-report-agent', 'Report AI', { searchQuery: q });
    };
   
    //const currentTheme = useSelector((state: RootState) => state.theme.currentTheme);
    const currentThemeId = useSelector((state: RootState) => state.theme.currentThemeId);
    const availableThemes = useSelector((state: RootState) => state.theme.availableThemes);
    const { userContext } = useSelector((state: RootState) => state.userSession);
    const [isThemeDropdownOpen, setIsThemeDropdownOpen] = useState(false);
    const [isThemeManagerOpen, setIsThemeManagerOpen] = useState(false);
    const [isDashboardDropdownOpen, setIsDashboardDropdownOpen] = useState(false);
    const [isUserDropdownOpen, setIsUserDropdownOpen] = useState(false);
    const [unreadMessageCount, setUnreadMessageCount] = useState(0);
    const [dashboardList, setDashboardList] = useState<any[]>([]);
    const [defaultDashboard, setDefaultDashboard] = useState<any>(null);
    const [homeDashboardId, setHomeDashboardId] = useState<string | null>(null);
    const dropdownRef = useRef<HTMLDivElement>(null);
    const dashboardDropdownRef = useRef<HTMLDivElement>(null);
    const userDropdownRef = useRef<HTMLDivElement>(null);

    // Debug: Log available themes
    // useEffect(() => {
    //     console.log('Available themes:', availableThemes);
    //     console.log('Current theme ID:', currentThemeId);
    // }, [availableThemes, currentThemeId]);

    // Load unread message count on component mount and when user context changes
    useEffect(() => {
        // Only load if user is authenticated
        if (userContext && !userContext.IsLoginFailed && userContext.SessionId) {
            loadUnreadMessageCount();
            // Refresh unread count every 60 seconds
            const interval = setInterval(loadUnreadMessageCount, 60000);
            return () => clearInterval(interval);
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [userContext]);

    // Load dashboard list on component mount and when user context changes
    useEffect(() => {
        if (userContext && !userContext.IsLoginFailed) {
            loadDashboardList();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [userContext]);

    useEffect(() => {
        const handleClickOutside = (event: MouseEvent) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
                setIsThemeDropdownOpen(false);
            }
            if (dashboardDropdownRef.current && !dashboardDropdownRef.current.contains(event.target as Node)) {
                setIsDashboardDropdownOpen(false);
            }
            if (userDropdownRef.current && !userDropdownRef.current.contains(event.target as Node)) {
                setIsUserDropdownOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const loadUnreadMessageCount = async () => {
        // Only attempt to load if user is authenticated
        if (!userContext || userContext.IsLoginFailed || !userContext.SessionId) {
            return;
        }
        
        try {
            const unreadMessages = await appMessageService.retrieveCurrentUserUnReadMessages();
            if (unreadMessages && Array.isArray(unreadMessages)) {
                setUnreadMessageCount(unreadMessages.length);
            } else {
                setUnreadMessageCount(0);
            }
        } catch (error) {
            // Silently handle errors - network issues shouldn't break the UI
            // The service method already handles network errors gracefully
            console.error('Failed to load unread message count:', error);
            setUnreadMessageCount(0);
        }
    };

    useEffect(() => {
        const onRefreshUnread = () => {
            loadUnreadMessageCount();
        };
        window.addEventListener(APP_AI_REFRESH_UNREAD_MESSAGES, onRefreshUnread);
        return () => window.removeEventListener(APP_AI_REFRESH_UNREAD_MESSAGES, onRefreshUnread);
    }, [userContext]);

    const loadDashboardList = async () => {
        try {
            const dashboardList = await dashboardService.retrieveCurrentUserDashboardList();
            if (dashboardList && Array.isArray(dashboardList)) {
                setDashboardList(dashboardList);
                
                // Find default dashboard and determine Home's dashboard ID
                const defaultDesktopId = userContext?.DefaultDesktopId;
                if (defaultDesktopId) {
                    const defaultDash = dashboardList.find((d: any) => d.Id === defaultDesktopId);
                    if (defaultDash) {
                        setDefaultDashboard(defaultDash);
                        setHomeDashboardId(defaultDesktopId);
                    }
                } else if (dashboardList.length > 0) {
                    setDefaultDashboard(dashboardList[0]);
                    setHomeDashboardId(dashboardList[0].Id);
                } else {
                    setHomeDashboardId(null);
                }
            }
        } catch (error) {
            console.error('Failed to load dashboard list:', error);
        }
    };

    const handleOpenDashboard = (desktopId: string, desktopName: string) => {
        setIsDashboardDropdownOpen(false);
        
        // Check if Home tab exists and is showing the same dashboard
        const homeTab = tabs.find(tab => tab.tabKey === 'home-tab');
        if (homeTab && homeTab.path === '/home') {
            // If the selected dashboard is the same as Home's dashboard, activate Home tab
            if (homeDashboardId === desktopId) {
                dispatch(activateTab('home-tab'));
                navigate('/home');
                return;
            }
        }
        
        // Otherwise, open dashboard in a new tab
        const paramObj = { id: desktopId };
        addTabAndNavigate('/dashboard', desktopName, paramObj, true);
    };

    const _handleOpenMyDefaultDashboard = () => {
        setIsDashboardDropdownOpen(false);
        if (defaultDashboard) {
            handleOpenDashboard(defaultDashboard.Id, defaultDashboard.DesktopName || 'My Default Dashboard');
        } else {
            // Navigate to home which will show default dashboard
            addTabAndNavigate('/home', 'Home', {}, true);
        }
    };

    const handleThemeChange = (themeId: string) => {
        dispatch(setThemeById(themeId));
        setIsThemeDropdownOpen(false);
    };

    const openThemeManagement = () => {
        setIsThemeDropdownOpen(false);
        setIsThemeManagerOpen(true);
    };

    const closeThemeManagement = () => {
        setIsThemeManagerOpen(false);
    };

    const _handleOpenMessages = () => {
        navigate('/message-management');
    };

    const handleOpenMyProfile = () => {
        setIsUserDropdownOpen(false);
        addTabAndNavigate('/myaccount', 'My Profile', {}, true);
    };

    const handleLogout = () => {
        setIsUserDropdownOpen(false);
        const sessionId = localStorage.getItem('sessionId');
        try {
            localStorage.removeItem('tabsState');
        } catch {}
        dispatch(clearTabsForLogout());
        dispatch(logout());
        navigate('/login', { replace: true });
        if (sessionId) {
            adminSvc.logout(sessionId).catch(() => {});
        }
    };

    return (
        <div className={`h-16 flex-none flex items-center justify-between px-4 lg:px-6 border-b ${theme.mainHeader} w-full`}>
            <div className="flex items-center">
                {/* Hamburger menu button - always visible on the left */}
                <button 
                    onClick={() => dispatch(toggleSidebar())}
                    className="w-8 h-8 flex items-center justify-center hover:bg-gray-200 dark:hover:bg-gray-700 rounded transition-colors mr-2 lg:mr-4"
                    title="Toggle Sidebar"
                >
                    <svg className="w-6 h-6 text-gray-700 dark:text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                    </svg>
                </button>
            </div>
            <div className="flex items-center flex-auto lg:flex-none">
                <div className="relative w-full lg:w-[400px] max-w-[400px] px-2">
                    <div className="relative w-full">
                        <input
                            type="text"
                            placeholder="Search for anything..."
                            value={globalSearch}
                            onChange={e => setGlobalSearch(e.target.value)}
                            onKeyDown={e => { if (e.key === 'Enter') handleGlobalSearch(); }}
                            className={`w-full pl-5 pr-10 py-1.5 ${theme.inputBox} border border rounded-full focus:outline-none`}
                        />
                        <div
                            onClick={handleGlobalSearch}
                            className={`absolute right-3 top-2.5 cursor-pointer ${theme.button_default}`}
                        >
                            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2"
                                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                            </svg>
                        </div>
                    </div>
                </div>
            </div>
            <div className="flex items-center space-x-4">
                {/* App Builder Agent shortcut */}
                <button
                    onClick={() => addTabAndNavigate('/app-builder-agent', 'App Builder Agent', {})}
                    className={`w-8 h-8 flex items-center justify-center ${theme.button_default}`}
                    title="App Builder Agent"
                >
                    <i className="fa-solid fa-wand-magic-sparkles w-5 h-5 flex items-center justify-center"></i>
                </button>
                {/* Dashboard Dropdown Menu - Use monitor/desktop icon button */}
                <div className="relative" ref={dashboardDropdownRef}>
                    <button
                        onClick={() => setIsDashboardDropdownOpen(!isDashboardDropdownOpen)}
                        className={`w-8 h-8 flex items-center justify-center ${theme.button_default}`}
                        title="Dashboard"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.5"
                                d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                        </svg>
                    </button>
                    {isDashboardDropdownOpen && (
                        <div className={`absolute right-0 mt-2 w-56 rounded-[4px] shadow-lg ${theme.mainContentSection} ring-1 ring-black ring-opacity-5 z-50 max-h-96 overflow-y-auto`}>
                            <div className="py-1" role="menu" aria-orientation="vertical">
                                {dashboardList.map((dashboard: any) => (
                                    <button
                                        key={dashboard.Id}
                                        onClick={() => handleOpenDashboard(dashboard.Id, dashboard.DesktopName)}
                                        className={`flex w-full items-center px-4 py-2 text-sm ${theme.button_default} hover:bg-gray-100 dark:hover:bg-gray-700`}
                                        role="menuitem"
                                    >
                                        <span>{dashboard.DesktopName}</span>
                                    </button>
                                ))}
                                {dashboardList.length === 0 && (
                                    <div className="px-4 py-2 text-sm text-gray-500">
                                        No dashboards available
                                    </div>
                                )}
                            </div>
                        </div>
                    )}
                </div>
                <button 
                    onClick={() => addTabAndNavigate('/message-management', 'Message Management', {})}
                    className={`relative w-8 h-8 flex items-center justify-center ${theme.button_default}`}
                    title="Message Management"
                >
                    <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none">
                        <path
                            d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
                            stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                    {unreadMessageCount > 0 && (
                        <span className="absolute top-0 right-0 bg-red-600 text-white text-[10px] font-bold rounded-full min-w-4 h-4 flex items-center justify-center px-0.5">
                            {unreadMessageCount > 9 ? '9+' : unreadMessageCount}
                        </span>
                    )}
                </button>
                <div className="relative" ref={dropdownRef}>
                    <button
                        onClick={() => setIsThemeDropdownOpen(!isThemeDropdownOpen)}
                        className={`w-8 h-8 flex items-center justify-center ${theme.button_default}`}
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} 
                                d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
                        </svg>
                    </button>
                    {isThemeDropdownOpen && (
                        <div className={`absolute right-0 mt-2 w-48 rounded-[4px] shadow-lg ${theme.mainContentSection} ring-1 ring-black ring-opacity-5 z-50`}>
                            <div className="py-1" role="menu" aria-orientation="vertical">
                                {availableThemes.map((themeOption) => (
                                    <button
                                        key={themeOption.id}
                                        onClick={() => handleThemeChange(themeOption.id)}
                                        className={`flex w-full items-center justify-between px-4 py-2 text-sm ${theme.button_default} ${currentThemeId === themeOption.id ? `!${t('text_default_active')}` : ''}`}
                                        role="menuitem"
                                    >
                                        <span>{themeOption.name}</span>
                                        {currentThemeId === themeOption.id && (
                                            <i className="fa fa-check" aria-hidden="true"></i>
                                        )}
                                    </button>
                                ))}
                                <div className={`my-1 border-t ${t('border_mainContentSection')}`} />
                                <button
                                    onClick={openThemeManagement}
                                    className={`flex w-full items-center justify-between px-4 py-2 text-sm ${theme.button_default}`}
                                    role="menuitem"
                                >
                                    Manage Themes <i className="fa fa-gear ml-2" aria-hidden="true"></i>
                                </button>
                            </div>
                        </div>
                    )}
                </div>
                {/* User Dropdown Menu (My Profile / My Calendar / Logout) */}
                <div className="relative" ref={userDropdownRef}>
                    <button
                        onClick={() => setIsUserDropdownOpen(!isUserDropdownOpen)}
                        className={`w-8 h-8 flex items-center justify-center rounded-full ${theme.button_default}`}
                        title={userContext?.DisplayName || 'User'}
                    >
                        <img
                            className="w-8 h-8 rounded-full"
                            src={`https://ui-avatars.com/api/?name=${encodeURIComponent(userContext?.DisplayName || 'User')}&background=0D8ABC&color=fff`}
                            alt="User"
                        />
                    </button>
                    {isUserDropdownOpen && (
                        <div
                            className={`absolute right-0 mt-2 w-52 rounded-[4px] shadow-lg ${theme.mainContentSection} ring-1 ring-black ring-opacity-5 z-50`}
                            role="menu"
                            aria-orientation="vertical"
                        >
                            <div className="py-1">
                                <button
                                    onClick={handleOpenMyProfile}
                                    className={`flex w-full items-center px-4 py-2 text-sm ${theme.button_default}`}
                                    role="menuitem"
                                >
                                    <i className="fa-solid fa-user w-5 mr-2" aria-hidden="true"></i>
                                    <span>My Profile</span>
                                </button>
                                <div className={`my-1 border-t ${t('border_mainContentSection')}`} />
                                <button
                                    onClick={handleLogout}
                                    className={`flex w-full items-center px-4 py-2 text-sm ${theme.button_default}`}
                                    role="menuitem"
                                >
                                    <i className="fa-solid fa-right-from-bracket w-5 mr-2" aria-hidden="true"></i>
                                    <span>Logout</span>
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>
            {isThemeManagerOpen && (
                <ThemeManagementPanel
                    onClose={closeThemeManagement}
                />
            )}
        </div>

    );
};

export default Header; 