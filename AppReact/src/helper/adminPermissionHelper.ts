/**
 * Admin detection helper shared across the app.
 * Angular uses AppSecurityUserBL.IsAdminUser() which effectively maps to:
 * - IsAdminUser / IsAdmin (when present)
 * - OR IsInSysAdminDomain
 * - OR DomainId/domainId fallback: SysAdmin=1, SaasCompanyAdmin=6
 */
/** DictAppSetup values are strings from AppTenantSetting (e.g. "true", "1"). */
export const readDictAppSetupBool = (
  dictAppSetup: Record<string, unknown> | null | undefined,
  key: string,
  defaultValue = false
): boolean => {
  if (!dictAppSetup) return defaultValue;
  const raw =
    dictAppSetup[key] ??
    (dictAppSetup as Record<string, unknown>)[key.toLowerCase()] ??
    (dictAppSetup as Record<string, unknown>)[key.toUpperCase()];
  if (raw == null || raw === '') return defaultValue;
  if (raw === true || raw === 1 || raw === '1') return true;
  if (raw === false || raw === 0 || raw === '0') return false;
  const s = String(raw).trim().toLowerCase();
  if (s === 'true' || s === 'yes') return true;
  if (s === 'false' || s === 'no') return false;
  return defaultValue;
};

/**
 * Tenant System Setting EnableConfigurationMode (Angular AppSetupBL.GetBoolValue).
 * Empty/absent/false => false; only explicit true/"true"/"1" enables Configuration UI.
 * Callers must still require admin (IsAdminUser) separately, matching Angular views.
 */
export const isEnableConfigurationModeForUser = (userContext: any): boolean => {
  const dict = userContext?.DictAppSetup ?? userContext?.dictAppSetup;
  return readDictAppSetupBool(dict, 'EnableConfigurationMode', false);
};

export const isAdminUserFromContext = (userContext: any): boolean => {
  if (!userContext) return false;

  const v = userContext?.IsAdminUser ?? userContext?.isAdminUser ?? userContext?.IsAdmin ?? userContext?.isAdmin;
  if (v === true || v === 1 || v === '1') return true;

  if (userContext?.IsInSysAdminDomain === true || userContext?.isInSysAdminDomain === true) return true;

  const domainId = userContext?.DomainId ?? userContext?.domainId;
  const d = typeof domainId === 'number' ? domainId : parseInt(String(domainId), 10);
  if (!Number.isNaN(d) && (d === 1 || d === 6)) return true;

  return false;
};

/**
 * True only for the master-DB SysAdmin (EmAppUserType.SysAdmin = 1).
 * AppSecurityUser.DomainId stores the EmAppUserType value directly, so DomainId=1 means SysAdmin.
 * This matches the backend check in AppSecurityUserBL.IsAdminUser() and ClassifyCurrentLoginUserType().
 * SaasCompanyAdmin (DomainId=6) returns false here.
 */
export const isMasterSysAdminFromContext = (userContext: any): boolean => {
  if (!userContext) return false;
  const domainId = userContext?.DomainId ?? userContext?.domainId;
  const d = typeof domainId === 'number' ? domainId : parseInt(String(domainId), 10);
  return !Number.isNaN(d) && d === 1;
};

/** Post-login / unknown-route landing path. SysAdmin has no tenant Desktop on Home. */
export const getDefaultAuthenticatedPath = (userContext: any): string =>
  isMasterSysAdminFromContext(userContext) ? '/company-security' : '/home';

export const getDefaultAuthenticatedTab = (userContext: any): { path: string; label: string } =>
  isMasterSysAdminFromContext(userContext)
    ? { path: '/company-security', label: 'Company and Users' }
    : { path: '/home', label: 'Home' };

