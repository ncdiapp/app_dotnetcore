/** Extract route base path (strip encoded JSON param segment). */
export function extractRouteBasePath(path: string): string {
  if (!path) return '/';
  const parts = path.split('/').filter((p) => p);
  if (parts.length === 0) return '/';
  const lastPart = parts[parts.length - 1];
  if (lastPart?.startsWith('%')) {
    return `/${parts.slice(0, -1).join('/')}`;
  }
  try {
    JSON.parse(lastPart);
    return `/${parts.slice(0, -1).join('/')}`;
  } catch {
    return `/${parts.join('/')}`;
  }
}

/** Path updates are only allowed within the same route family (prevents Search/Home path corruption). */
export function canUpdateTabPath(currentPath: string, newPath: string): boolean {
  if (!newPath) return false;
  if (!currentPath) return true;
  if (currentPath === newPath) return true;
  return extractRouteBasePath(currentPath) === extractRouteBasePath(newPath);
}

/** Compare tab stored path with current browser route (handles encoded JSON route params). */
export function tabRoutePathsMatch(tabPath: string, urlPath: string): boolean {
  if (!tabPath || !urlPath) return false;
  if (tabPath === urlPath) return true;

  try {
    if (decodeURIComponent(tabPath) === decodeURIComponent(urlPath)) return true;
  } catch {
    // ignore decode errors
  }

  const tabBase = extractRouteBasePath(tabPath);
  const urlBase = extractRouteBasePath(urlPath);
  if (tabBase !== urlBase || tabBase === '/') return false;

  const extractParams = (path: string): Record<string, unknown> | null => {
    try {
      const parts = path.split('/').filter((p) => p);
      const lastPart = parts[parts.length - 1];
      if (!lastPart) return null;
      const json = lastPart.startsWith('%') ? decodeURIComponent(lastPart) : lastPart;
      return JSON.parse(json);
    } catch {
      return null;
    }
  };

  const tabParams = extractParams(tabPath);
  const urlParams = extractParams(urlPath);
  if (tabParams && urlParams) {
    const tabFiltered = { ...tabParams };
    const urlFiltered = { ...urlParams };
    delete tabFiltered.isNavigatedFromTab;
    delete urlFiltered.isNavigatedFromTab;
    const tabKeys = Object.keys(tabFiltered).sort();
    const urlKeys = Object.keys(urlFiltered).sort();
    if (tabKeys.length === urlKeys.length) {
      return tabKeys.every((key) => tabFiltered[key] === urlFiltered[key]);
    }
    return false;
  }

  return false;
}

type TabPathInfo = { tabKey: string; path: string; initialPath?: string };

function repairTabStoredPath(tab: TabPathInfo): string {
  if (tab.tabKey === 'home-tab') {
    const initial = tab.initialPath || tab.path;
    if (isTransactionFormGroupPath(tab.path) && !isTransactionFormGroupPath(initial)) {
      return initial;
    }
    return tab.path || initial || '/home';
  }
  const initial = tab.initialPath || tab.path;
  if (isTransactionFormGroupPath(tab.path) && !isTransactionFormGroupPath(initial)) {
    return initial;
  }
  return tab.path;
}

/** Resolve navigation path for a tab (repairs corrupted Search/Home paths). */
export function resolveTabNavigationPath(tab: TabPathInfo): string {
  return repairTabStoredPath(tab);
}

const TRANSACTION_FORM_GROUP_BASE = '/TransactionFormGroup';
const TRANSACTION_FOLDER_NAVIGATION_BASE = '/transaction-folder-navigation';

export function isTransactionFormGroupPath(path: string): boolean {
  if (!path) return false;
  const normalized = path.startsWith('/') ? path : `/${path}`;
  return (
    normalized === TRANSACTION_FORM_GROUP_BASE ||
    normalized.startsWith(`${TRANSACTION_FORM_GROUP_BASE}/`)
  );
}

/** Each host transaction (Style vs Fabric, etc.) needs its own tab — do not reuse by base path only. */
export function isTransactionFolderNavigationPath(path: string): boolean {
  if (!path) return false;
  const normalized = path.startsWith('/') ? path : `/${path}`;
  return (
    normalized === TRANSACTION_FOLDER_NAVIGATION_BASE ||
    normalized.startsWith(`${TRANSACTION_FOLDER_NAVIGATION_BASE}/`)
  );
}

export const buildRoutePathFromParamObj = (baseRoutePath: string, paramObj: any) => {

    // Convert to JSON string and encode for URL
    const jsonParam = encodeURIComponent(JSON.stringify(paramObj));
    
    // Ensure path starts with / for absolute navigation
    if (!baseRoutePath.startsWith("/")) {
        baseRoutePath = "/" + baseRoutePath;
    }
    
    // Ensure path ends with / before adding param
    if (!baseRoutePath.endsWith("/")) {
        baseRoutePath += "/";
    }

    return `${baseRoutePath}${jsonParam}`;
}

/**
 * Map API RouteCode (e.g. from AppListMenu) to React route path.
 * Ensures menu items and links that open FormListEdit navigate to form-list-edit.
 */
export function getReactPathForRouteCode(routeCode: string): string {
  const code = (routeCode || '').trim();
  if (code === 'FormListEdit') return 'form-list-edit';
  if (code === 'WorkflowExecutionMonitor' || code === 'main.WorkflowExecutionMonitor') {
    return 'workflow-execution-monitor';
  }
  if (code === 'WorkflowAutomationEditor' || code === 'main.WorkflowAutomationEditor') {
    return 'workflow-automation-editor';
  }
  return code;
}

/** Build param object for internal route from routeCode + link (matches useTabNavigation / list menu). */
export function buildParamObjFromRouteCodeAndLink(routeCode: string, linkParam: string): Record<string, any> {
  const routeLower = (routeCode || '').toLowerCase();

  if (routeLower === 'masterdatamanagement') {
    return { searchId: linkParam || null };
  }
  if (routeLower === 'user-editor') {
    return { userId: linkParam || null };
  }
  if (routeLower === 'file-management') {
    const categoryId = linkParam != null && linkParam !== '' ? Number(linkParam) : 3;
    return { defaultCategoryId: categoryId };
  }

  let paramId: string | null = null;
  let param1: string | null = null;
  let param2: string | null = null;

  if (linkParam && linkParam.indexOf('_') > 0) {
    const parts = linkParam.split('_');
    paramId = parts[0] || null;
    param1 = parts[1] || null;
    param2 = parts[2] || null;
  } else {
    paramId = linkParam || null;
  }

  const paramObj: Record<string, any> = { id: paramId };
  if (param1) paramObj.param1 = param1;
  if (param2) paramObj.param2 = param2;
  return paramObj;
}

