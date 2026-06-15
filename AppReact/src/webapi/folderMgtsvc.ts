import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class FolderManagementService {
  

  async getFolderStructure(rootFolderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/GetFolderStructure?rootFolderId=${rootFolderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get folder structure');
    return response.json();
  }

  async createFolder(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/CreateFolder`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create folder');
    return response.json();
  }

  async updateFolder(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/UpdateFolder`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update folder');
    return response.json();
  }

  async deleteFolder(folderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/DeleteFolder?folderId=${folderId}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete folder');
    return response.json();
  }

  async moveFolder(sourceFolderId: string, targetFolderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/MoveFolder`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sourceFolderId, targetFolderId })
    });
    if (!response.ok) throw new Error('Failed to move folder');
    return response.json();
  }

  async getFolderPermissions(folderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/GetFolderPermissions?folderId=${folderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get folder permissions');
    return response.json();
  }

  async updateFolderPermissions(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/FolderManagement/UpdateFolderPermissions`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update folder permissions');
    return response.json();
  }
}

export const folderManagementService = new FolderManagementService(); 