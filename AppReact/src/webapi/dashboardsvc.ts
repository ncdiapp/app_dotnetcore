import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
export class DashboardService {
  
  async retrieveAllAppDesktopDto(isIncludeUserDefaultDesktop: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveAllAppDesktopDto?isIncludeUserDefaultDesktop=${isIncludeUserDefaultDesktop}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app desktop DTOs');
    return response.json();
  }

  async retrieveCurrentUserDashboardList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveCurrnetUserDashboardList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user dashboard list');
    return response.json();
  }

  async retrieveDesktopDtoListByUserId(userId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveDesktopDtoListByUserId?userId=${userId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve desktop DTOs by user ID');
    return response.json();
  }

  async retrieveDesktopDtoListByRoleId(roleId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveDesktopDtoListByRoleId?roleId=${roleId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve desktop DTOs by role ID');
    return response.json();
  }

  async getOrganizationDesktopListByOrganizationId(organizationId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/GetOrganizationDesktopListByOrganizationId?organizationId=${organizationId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve organization desktop list');
    return response.json();
  }

  async getOneAppDesktop(desktopId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveOneAppDesktopExDto?desktopId=${desktopId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app desktop');
    return response.json();
  }

  async saveOneDashboard(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/SaveDesktop`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save dashboard');
    return response.json();
  }

  async saveAsAppDesktopExDto(desktopId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/SaveAsAppDesktopExDto?desktopId=${desktopId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to save as app desktop');
    return response.json();
  }

  async deleteOneAppDesktop(desktopId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/DeleteOneAppDesktop?desktopId=${desktopId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app desktop');
    return response.json();
  }

  async createUserDefaultDesktop(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/CreateUserDefaultDesktop`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to create user default desktop');
    return response.json();
  }

  async retrieveFolderHierarchyDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/RetrieveFolderHairarchyDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve folder hierarchy');
    return response.json();
  }

  async setUserDashboardAsDefault(desktopId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/DashBoard/SetUserDashboardAsDefault?desktopId=${desktopId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to set user dashboard as default');
    return response.json();
  }
}

export const dashboardService = new DashboardService(); 