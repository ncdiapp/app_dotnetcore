import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class WebSiteMgtService {
  

  async retrieveOneAppEsiteCatalogueExDto(eStoreId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveOneAppEsiteCatalogueExDto?eStoreId=${eStoreId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve site catalogue');
    return response.json();
  }

  async saveAppEsiteCatalogueExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SaveAppEsiteCatalogueExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to save site catalogue');
    return response.json();
  }

  async deleteOneAppEsiteCatalogue(eStoreId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/DeleteOneAppEsiteCatalogue?eStoreId=${eStoreId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to delete site catalogue');
    return response.json();
  }

  async retrieveOneAppEsitePagesExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveOneAppEsitePagesExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to retrieve site pages');
    return response.json();
  }

  async saveAppEsitePagesExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SaveAppEsitePagesExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to save site pages');
    return response.json();
  }

  async saveAppEsitePageExDtoList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SaveAppEsitePageExDtoList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to save site page list');
    return response.json();
  }

  async moveOneEsiteFileToNewLocation(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/MoveOneEsiteFileToNewLocation`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to move file');
    return response.json();
  }

  async deleteOneAppEsitePages(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/DeleteOneAppEsitePages`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to delete site pages');
    return response.json();
  }

  async retrieveApplicationWebsiteDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveApplicationWebsiteDtoList`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve website list');
    return response.json();
  }

  async retrieveNextJsApplicationList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveNextJsApplicationList`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve NextJS apps');
    return response.json();
  }

  async retrieveWebsiteTemplateDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveWebsiteTemplateDtoList`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve website templates');
    return response.json();
  }

  async importWebSiteTemplateToApplicationSite(templateSiteId: string, appSiteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/ImportWebSiteTemplateToApplicationSite?tempalteSiteId=${templateSiteId}&appSiteId=${appSiteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to import website template');
    return response.json();
  }

  async synchronizeWebSiteRoutstatejs(appSiteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SynchronizeWebSiteRoutstatejs?appSiteId=${appSiteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to synchronize route state');
    return response.json();
  }

  async retrieveOneEsiteExDto(esiteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveOneEsiteExDto?esiteId=${esiteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve site');
    return response.json();
  }

  async saveOneAppEsiteExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SaveOneAppEsiteExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to save site');
    return response.json();
  }

  async deleteOneAppEsite(esiteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/DeleteOneAppEsite?esiteId=${esiteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to delete site');
    return response.json();
  }

  async saveAsAppWebsite(esiteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/SaveAsAppWebsite?esiteId=${esiteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to save as website');
    return response.json();
  }

  async retrieveLocalFolderHairarchyDtoByFolderPath(rootFolderPath: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveLocalFolderHairarchyDtoByFolderPath?rootFolderPath=${rootFolderPath}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve folder hierarchy');
    return response.json();
  }

  async retrieveLocalFileInfoDtosByFolderPath(folderPath: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveLocalFileInfoDtosByFolderPath?folderPath=${folderPath}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve file info');
    return response.json();
  }

  async retrieveAppEsiteLocalFolderHairarchyDto(eSiteId: string, subFolderPath: string = '', subsiteType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveAppEsiteLocalFolderHairarchyDto?eSiteId=${eSiteId}&subFolderPath=${subFolderPath}&subsiteType=${subsiteType}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve site folder hierarchy');
    return response.json();
  }

  async retrieveLocalFileInfoDtosByFolderDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveLocalFileInfoDtosByFolderDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw new Error('Failed to retrieve folder file info');
    return response.json();
  }

  async retrieveAppEsiteComponentList(siteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveAppEsiteComponentList?siteId=${siteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve site components');
    return response.json();
  }

  async retrieveAppEsiteComponetFolders(siteId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppWebSiteMgt/RetrieveAppEsiteComponetFolders?siteId=${siteId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to retrieve component folders');
    return response.json();
  }
}

export const websiteMgtService = new WebSiteMgtService(); 