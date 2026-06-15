import React, { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { login, setUserMenu, setUserContext } from '../../redux/features/admin/userSessionSlice';
import { adminSvc } from '../../webapi/adminsvc';
import { toAbsoluteResourceUrl } from '../../webapi/endpoints';
import { RootState, AppDispatch } from '../../redux/store';
import { refreshUserDefinedThemesCache } from '../../helper/themeHelper';
import { setAvailableThemes, builtInThemes } from '../../redux/features/ui/theme/themeSlice';
import { resetTabs } from '../../redux/features/ui/navigation/tabnavSlice';
import { getDefaultAuthenticatedPath, getDefaultAuthenticatedTab } from '../../helper/adminPermissionHelper';
import { store } from '../../redux/store';
import type { TenantBrandingState } from '../../redux/features/ui/tenantBrandingSlice';
import CompanyPicker from './CompanyPicker';

const DEFAULT_LOGIN_BACKGROUND = '/img/LoginImage.png';

const Login: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [backgroundImageUrl, setBackgroundImageUrl] = useState(DEFAULT_LOGIN_BACKGROUND);
  const [pendingSessionId, setPendingSessionId] = useState<string | null>(null);
  const [availableCompanies, setAvailableCompanies] = useState<any[]>([]);
  const [loginError, setLoginError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const { error, loading, userContext } = useSelector((state: RootState) => state.userSession);
  const tenantBranding = useSelector((state: RootState) => state.tenantBranding as TenantBrandingState);

  useEffect(() => {
    adminSvc.getLoginPageBackgroundImageUrlList().then((list) => {
      if (list?.length > 0) {
        const index = Math.floor(Math.random() * list.length);
        setBackgroundImageUrl(toAbsoluteResourceUrl(list[index]));
      }
    }).catch(() => {});
  }, []);

  const getUserMenu = async () => {
    try {        
      const userMenu = await adminSvc.retrieveUserTreeMenu();
      dispatch(setUserMenu(userMenu));      
    } catch (error) {
      console.error('Failed to load user menu:', error);
    }
  }

  const completeLogin = async () => {
    await getUserMenu();
    try {
      const userThemes = await refreshUserDefinedThemesCache();
      dispatch(setAvailableThemes([...builtInThemes, ...userThemes]));
    } catch (error) {
      console.error('Failed to load user themes:', error);
    }
    const ctx = store.getState().userSession.userContext;
    const landingTab = getDefaultAuthenticatedTab(ctx);
    dispatch(resetTabs(landingTab));
    navigate(getDefaultAuthenticatedPath(ctx), { replace: true });
  };

  const handleLogin = async () => {
    if (isSubmitting || loading) return;
    setIsSubmitting(true);
    setLoginError(null);
    try {
      localStorage.removeItem('tabsState');
    } catch {}
    dispatch(resetTabs());

    // Pre-check: verify credentials and detect multi-company before touching Redux
    let rawResponse: any;
    try {
      rawResponse = await adminSvc.mgtLogin(username, password);
    } catch {
      setLoginError('Login failed. Please check your credentials.');
      localStorage.removeItem('sessionId');
      setIsSubmitting(false);
      return;
    }

    if (rawResponse?.IsLoginFailed) {
      setLoginError(rawResponse.LoginFailedErroMessage || 'Login failed.');
      localStorage.removeItem('sessionId');
      setIsSubmitting(false);
      return;
    }

    if (rawResponse?.RequiresCompanySelection && rawResponse?.AvailableCompnay?.length > 1) {
      setPendingSessionId(String(rawResponse.SessionId));
      setAvailableCompanies(rawResponse.AvailableCompnay);
      setIsSubmitting(false);
      return;
    }

    // Single company — complete login using the already-verified response (no second mgtLogin call)
    dispatch(login({
      userName: username,
      password,
      preloadedResponse: rawResponse,
      callBack: completeLogin
    }));
    setIsSubmitting(false);
  };

  const handleCompanySelected = async () => {
    if (!pendingSessionId) return;
    try {
      localStorage.setItem('sessionId', pendingSessionId);
      const ctx = await adminSvc.getUserContextBySessionId(pendingSessionId);
      if (ctx && !ctx.IsLoginFailed) {
        dispatch(setUserContext(ctx));
        setPendingSessionId(null);
        setAvailableCompanies([]);
        await completeLogin();
      } else {
        setLoginError('Session activation failed. Please log in again.');
        setPendingSessionId(null);
        setAvailableCompanies([]);
      }
    } catch {
      setLoginError('Failed to activate session. Please log in again.');
      setPendingSessionId(null);
      setAvailableCompanies([]);
    }
  };

  return (
    <>
    {pendingSessionId && availableCompanies.length > 1 && (
      <CompanyPicker
        companies={availableCompanies}
        sessionId={pendingSessionId}
        onSelected={handleCompanySelected}
      />
    )}
    <div
      className="min-h-screen flex items-center justify-center"
      style={{
        backgroundImage: `url('${backgroundImageUrl}')`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        backgroundRepeat: 'no-repeat'
      }}
    >
      <div className="bg-white/60 backdrop-blur-sm rounded-lg p-8 max-w-sm w-full mx-4 shadow-xl">
        <div className="mb-6">
          <h2 className="text-2xl font-bold text-gray-900 text-center">
            {tenantBranding.isFound && tenantBranding.companyName ? tenantBranding.companyName : 'Welcome'}
          </h2>
          <p className="text-gray-600 text-center mt-2">Please sign in to continue</p>
        </div>
        <div className="space-y-4">
          <div>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="mt-1 px-2 py-2 block w-full rounded-[4px] border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
              placeholder="Username"
              autoComplete="username"
              disabled={loading || isSubmitting}
            />
          </div>
          <div className="relative">
            <input
              type={showPassword ? "text" : "password"}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-1 px-2 py-2 block w-full rounded-[4px] border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm pr-10"
              placeholder="Password"
              autoComplete="password"
              disabled={loading || isSubmitting}
            />
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="absolute inset-y-0 right-0 flex items-center pr-3 mt-1 text-gray-600 hover:text-gray-800"
              disabled={loading || isSubmitting}
            >
              {showPassword ? (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-5 h-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
                </svg>
              ) : (
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-5 h-5">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
              )}
            </button>
          </div>
          {(loading || isSubmitting) && (
            <div className="text-blue-600 text-sm text-center">
              Signing in...
            </div>
          )}
          {(error || loginError) && (
            <div className="text-red-500 text-sm text-center">
              {loginError || error}
            </div>
          )}
          {!loginError && !error && userContext?.IsLoginFailed && (
            <div className="text-red-500 text-sm text-center">
              {userContext.LoginFailedErroMessage}
            </div>
          )}
          <button
            onClick={handleLogin}
            className="w-full py-2 px-4 border border-transparent rounded-[4px] shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            disabled={loading || isSubmitting}
          >
            {(loading || isSubmitting) ? 'Signing in...' : 'Sign In'}
          </button>
        </div>
      </div>
    </div>
    </>
  );
};

export default Login; 