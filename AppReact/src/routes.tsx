import React, { useEffect, useState } from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import { useSelector, useDispatch } from 'react-redux';
import { RootState, AppDispatch } from './redux/store';
import { restoreSession, setUserMenu } from './redux/features/admin/userSessionSlice';
import { setTenantBranding } from './redux/features/ui/tenantBrandingSlice';
import { adminSvc } from './webapi/adminsvc';
import { tenantSvc } from './webapi/tenantSvc';
import Login from './components/admin/Login';
import LandingPage from './components/mainLayout/LandingPage';
import { AUTHENTICATED_ROUTES } from './routes.shared';
import FormMasterDetailPrint from './components/formMgt/FormMasterDetailPrint';
import FormMasterDetailStandalone from './components/formMgt/FormMasterDetailStandalone';
import SearchEditorStandalone from './components/search/SearchEditorStandalone';
import { getDefaultAuthenticatedPath, isMasterSysAdminFromContext } from './helper/adminPermissionHelper';

const SysAdminGuard: React.FC<{ children: React.ReactElement }> = ({ children }) => {
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  if (!isMasterSysAdminFromContext(userContext)) {
    return <Navigate to={getDefaultAuthenticatedPath(userContext)} replace />;
  }
  return children;
};

const DefaultAuthenticatedRedirect: React.FC = () => {
  const userContext = useSelector((state: RootState) => state.userSession.userContext);
  return <Navigate to={getDefaultAuthenticatedPath(userContext)} replace />;
};

// Guard against repeated restore attempts causing request storms.
// (If AppRoutes remounts or state flaps, we still only try once per app lifetime.)
let restoreSessionAttempted = false;
let tenantInfoLoaded = false;

const AppRoutes: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const location = useLocation();
  const { userContext, loading } = useSelector((state: RootState) => state.userSession);
  const [sessionRestored, setSessionRestored] = useState(false);
  const isAuthenticated = Boolean(userContext) && !userContext?.IsLoginFailed;

  // Load tenant branding once on app startup (before session restore)
  useEffect(() => {
    if (tenantInfoLoaded) return;
    tenantInfoLoaded = true;
    tenantSvc.GetTenantInfo().then((info) => {
      dispatch(setTenantBranding({
        isFound: info.isFound,
        companyName: info.companyName ?? null,
        domainToken: info.domainToken ?? null,
        customDomain: info.customDomain ?? null,
      }));
    }).catch(() => {});
  }, [dispatch]);

  // Restore session on component mount (page load/refresh)
  useEffect(() => {
    // Only try to restore if we don't already have a user context
    if (!userContext && !sessionRestored) {
      // Check if there's a sessionId in localStorage first
      const hasSessionId = localStorage.getItem('sessionId');
      if (hasSessionId) {
        if (restoreSessionAttempted) {
          // We already attempted restore; don't retry endlessly.
          setSessionRestored(true);
          return;
        }
        restoreSessionAttempted = true;

        dispatch(restoreSession())
          .then((result) => {
            // If session was successfully restored, load user menu
            if (result.type === 'userSession/restoreSession/fulfilled' && result.payload) {
              adminSvc
                .retrieveUserTreeMenu()
                .then((userMenu: any) => {
                  dispatch(setUserMenu(userMenu));
                })
                .catch((error: any) => {
                  console.error('Failed to load user menu after session restore:', error);
                });
            }
          })
          .finally(() => {
            // Only mark as restored after the promise settles completely
            setSessionRestored(true);
          });
      } else {
        // No sessionId, mark as restored immediately
        setSessionRestored(true);
      }
    } else if (userContext) {
      // If we already have user context, mark as restored
      setSessionRestored(true);
    }
  }, [dispatch, userContext, sessionRestored]);

  // Block rendering until any in-progress session restoration completes.
  // This applies to ALL pages, including /login, so the login form is only shown
  // after we know there is no valid existing session — eliminating the race where
  // a background restore could authenticate the user after a failed login attempt.
  const hasSessionId = localStorage.getItem('sessionId');
  const isRestoringSession = hasSessionId && (!sessionRestored || (loading && !userContext));

  if (isRestoringSession) {
    return (
      <div style={{ 
        display: 'flex', 
        justifyContent: 'center', 
        alignItems: 'center', 
        height: '100vh',
        fontSize: '16px'
      }}>
        Loading...
      </div>
    );
  }

  // After session restoration is complete, render routes
  // The BrowserRouter will preserve the current URL, so routes should match correctly

  return (
    <Routes>
      {!isAuthenticated ? (
        <>
          <Route path="/login" element={<Login />} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </>
      ) : (
        <>
          {/* Standalone print route: MUST NOT mount LandingPage (tab cache). */}
          <Route path="/FormMasterDetailPrint/:param" element={<FormMasterDetailPrint />} />
          <Route path="/FormMasterDetailPrint" element={<FormMasterDetailPrint />} />
          {/* Standalone form route: MUST NOT mount LandingPage (tab cache). */}
          <Route path="/FormMasterDetailStandalone/:param" element={<FormMasterDetailStandalone />} />
          <Route path="/FormMasterDetailStandalone" element={<FormMasterDetailStandalone />} />
          {/* Standalone search editor route: MUST NOT mount LandingPage (tab cache). */}
          <Route path="/SearchEditorStandalone/:param" element={<SearchEditorStandalone />} />
          <Route path="/SearchEditorStandalone" element={<SearchEditorStandalone />} />

          <Route element={<LandingPage />}>
            {AUTHENTICATED_ROUTES.map((r) => (
              <Route
                key={r.path}
                path={r.path}
                element={
                  r.sysAdminOnly
                    ? <SysAdminGuard>{r.element}</SysAdminGuard>
                    : r.element
                }
              />
            ))}

            <Route path="*" element={<DefaultAuthenticatedRedirect />} />
          </Route>
        </>
      )}
    </Routes>
  );
};

export default AppRoutes; 