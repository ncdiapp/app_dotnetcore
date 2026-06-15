import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class NavigationService {
  
  async getUserMenu(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/GetUserMenu`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get user menu');
    return response.json();
  }

  async saveMenuItem(menuItem: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/SaveMenuItem`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(menuItem)
    });
    if (!response.ok) throw new Error('Failed to save menu item');
    return response.json();
  }

  async deleteMenuItem(menuItemId: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/DeleteMenuItem?menuItemId=${menuItemId}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete menu item');
  }

  async reorderMenuItems(menuItems: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/ReorderMenuItems`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(menuItems)
    });
    if (!response.ok) throw new Error('Failed to reorder menu items');
  }

  async getNavigationState(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/GetNavigationState`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get navigation state');
    return response.json();
  }

  async saveNavigationState(state: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/SaveNavigationState`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(state)
    });
    if (!response.ok) throw new Error('Failed to save navigation state');
  }

  async getUserFavorites(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/GetUserFavorites`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get user favorites');
    return response.json();
  }

  async toggleFavorite(menuItemId: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Navigation/ToggleFavorite?menuItemId=${menuItemId}`, {
      method: 'POST',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to toggle favorite');
  }
}

export const navigationService = new NavigationService(); 