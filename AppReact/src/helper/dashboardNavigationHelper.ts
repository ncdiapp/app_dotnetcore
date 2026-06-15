import { dashboardService } from '../webapi/dashboardsvc';

/**
 * Determines the default dashboard path after login based on AngularJS logic:
 * 1. If URL has a path → open that path
 * 2. If userContext.DefaultDesktopId exists → open that default Dashboard
 * 3. If user Dashboard list is not empty → open the first Dashboard
 * 4. Otherwise → open empty Dashboard page
 */
export const getDefaultDashboardPath = async (
  userContext: any,
  currentPath: string
): Promise<string> => {
  // If URL has a path (and it's not just '/'), use that path
  if (currentPath && currentPath !== '/' && currentPath !== '/login') {
    return currentPath;
  }

  // Check if user has a default desktop ID
  const defaultDesktopId = userContext?.DefaultDesktopId;
  if (defaultDesktopId) {
    return `/dashboard/${encodeURIComponent(JSON.stringify({ id: defaultDesktopId }))}`;
  }

  // If no default desktop, try to get user's dashboard list
  try {
    const dashboardList = await dashboardService.retrieveCurrentUserDashboardList();
    if (dashboardList && Array.isArray(dashboardList) && dashboardList.length > 0) {
      const firstDashboard = dashboardList[0];
      if (firstDashboard?.Id) {
        return `/dashboard/${encodeURIComponent(JSON.stringify({ id: firstDashboard.Id }))}`;
      }
    }
  } catch (error) {
    console.error('Failed to retrieve user dashboard list:', error);
  }

  // Fallback: open empty Dashboard page
  return '/dashboard';
};
