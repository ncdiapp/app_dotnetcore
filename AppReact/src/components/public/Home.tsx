import React, { useEffect, useRef, useState } from 'react';
import { Navigate } from 'react-router-dom';
import { shallowEqual, useSelector } from 'react-redux';
import { RootState } from '../../redux/store';
import { isMasterSysAdminFromContext } from '../../helper/adminPermissionHelper';
import { dashboardService } from '../../webapi/dashboardsvc';
import Dashboard from '../dashboard/Dashboard';
import { BusyLoader } from '../common/BusyLoader';

/**
 * Home component that determines which Dashboard to show based on:
 * 1. If userContext.DefaultDesktopId exists → show that default Dashboard
 * 2. If user Dashboard list is not empty → show the first Dashboard
 * 3. Otherwise → show empty Dashboard page
 * 
 * Note: This component directly renders Dashboard without navigation.
 * Login flow already handles navigation to the correct path.
 */
const Home: React.FC = () => {
  // IMPORTANT: Select only the fields we care about.
  // If we select the whole userContext object, any unrelated change in that object
  // will re-trigger this effect and unmount/remount Dashboard (causing repeated loads).
  const { hasUserContext, isLoginFailed, defaultDesktopId, userContext } = useSelector(
    (state: RootState) => ({
      hasUserContext: Boolean(state.userSession.userContext),
      isLoginFailed: Boolean(state.userSession.userContext?.IsLoginFailed),
      defaultDesktopId: state.userSession.userContext?.DefaultDesktopId ?? null,
      userContext: state.userSession.userContext,
    }),
    shallowEqual
  );

  const [dashboardId, setDashboardId] = useState<string | null | undefined>(undefined);
  const [loading, setLoading] = useState(true);
  const didTryListFallbackRef = useRef(false);

  useEffect(() => {
    const determineDefaultDashboard = async () => {
      if (!hasUserContext || isLoginFailed) {
        setDashboardId(null);
        setLoading(false);
        return;
      }

      // 1. Check if user has a default desktop ID
      if (defaultDesktopId) {
        setDashboardId(String(defaultDesktopId));
        setLoading(false);
        return;
      }

      // 2. If no default desktop, try to get user's dashboard list
      // Only try this fallback once per mount/session to avoid repeated remount loops.
      if (didTryListFallbackRef.current) {
        setDashboardId(null);
        setLoading(false);
        return;
      }
      didTryListFallbackRef.current = true;

      setLoading(true);
      try {
        const dashboardList = await dashboardService.retrieveCurrentUserDashboardList();
        if (dashboardList && Array.isArray(dashboardList) && dashboardList.length > 0) {
          const firstDashboard = dashboardList[0];
          if (firstDashboard?.Id) {
            setDashboardId(String(firstDashboard.Id));
            setLoading(false);
            return;
          }
        }
      } catch (error) {
        console.error('Failed to retrieve user dashboard list:', error);
      }

      // 3. Fallback: show empty Dashboard page
      setDashboardId(null);
      setLoading(false);
    };

    determineDefaultDashboard();
  }, [hasUserContext, isLoginFailed, defaultDesktopId]);

  if (isMasterSysAdminFromContext(userContext)) {
    return <Navigate to="/company-security" replace />;
  }

  if (loading) {
    return (
      <div style={{ padding: '20px', textAlign: 'center' }}>
        <BusyLoader />
      </div>
    );
  }

  // Render Dashboard with the determined dashboardId (no navigation, just render)
  return <Dashboard dashboardId={dashboardId} />;
};

export default Home;
