import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class LanguageService {
  
  async getCurrentUserLanguage(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/GetCurrentUserLanguage`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get current user language');
    return response.json();
  }

  async setCurrentUserLanguage(languageCode: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/SetCurrentUserLanguage`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ languageCode })
    });
    if (!response.ok) throw new Error('Failed to set current user language');
  }

  async getAvailableLanguages(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/GetAvailableLanguages`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get available languages');
    return response.json();
  }

  async getLanguageResources(culture: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/GetLanguageResources?culture=${culture}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get language resources');
    return response.json();
  }

  async updateLanguageResource(resource: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/UpdateLanguageResource`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(resource)
    });
    if (!response.ok) throw new Error('Failed to update language resource');
  }

  async importLanguageResources(culture: string, resources: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/ImportLanguageResources`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ culture, resources })
    });
    if (!response.ok) throw new Error('Failed to import language resources');
  }

  async exportLanguageResources(culture: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Language/ExportLanguageResources?culture=${culture}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to export language resources');
    return response.json();
  }
}

export const languageService = new LanguageService(); 