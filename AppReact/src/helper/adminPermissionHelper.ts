/**
 * Admin detection helper shared across the app.
 * Angular uses AppSecurityUserBL.IsAdminUser() which effectively maps to:
 * - IsAdminUser / IsAdmin (when present)
 * - OR IsInSysAdminDomain
 * - OR DomainId/domainId fallback: SysAdmin=1, SaasCompanyAdmin=6
 */
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

