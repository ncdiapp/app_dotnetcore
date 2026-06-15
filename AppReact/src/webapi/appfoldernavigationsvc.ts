import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
class AppFolderNavigationService {  

  async getFormDefaultTransactionFolderNavigation(transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetFormDefaultTransctionFolderNivigation?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get default transaction folder navigation');
    return response.json();
  }

  async getCurrentUserFileFolderCategoryViewList(folderCategory: string, transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetCurrentUserFileFolderCategoryViewList?folderCategory=${folderCategory}&transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get folder category view list');
    return response.json();
  }

  async getTransactionFolderViewList(folderId: string, searchViewId: string, transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetTransctionFolderViewList?folderId=${folderId}&searchViewId=${searchViewId}&transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get transaction folder view list');
    return response.json();
  }

  async getCurrentUserFileFolderCategoryViewContent(folderCategory: string, searchViewId: string, transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetCurrentUserFileFolderCategoryViewContent?folderCategory=${folderCategory}&searchViewId=${searchViewId}&transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get folder category view content');
    return response.json();
  }

  async addFilesToMyFavorite(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/AddFilesToMyFavourite`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to add files to favorites');
    return response.json();
  }

  async addFolderIdsToMyFavorite(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/AddFolderIdsToMyFavourite`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to add folders to favorites');
    return response.json();
  }

  async removeFilesFromMyFavorite(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RemoveFilesFromMyFavourite`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to remove files from favorites');
    return response.json();
  }

  async removeFolderIdsFromMyFavorite(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RemoveFolderIdsFromMyFavourite`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to remove folders from favorites');
    return response.json();
  }

  async addFilesToShareOther(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/AddFilesToShareOther`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to share files');
    return response.json();
  }

  async sendFileNotificationFromFileSharingMessageTemplate(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/SendFileNotificationFromFileSharingMessageTemplate`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to send file sharing notification');
    return response.json();
  }

  async getCurrentUserAvailableShareFileToRolesAndUsers(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetCurrentUserAvailaleShareFileToRolesAndUsers`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get available share roles and users');
    return response.json();
  }

  async getCurrentUserFilesToShareOtherDtoList(fileId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetCurrentUserFilesToShareOtherDtoList?fileId=${fileId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get files to share');
    return response.json();
  }

  async removeFilesToShareOther(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RemoveFilesToShareOther`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to remove shared files');
    return response.json();
  }

  async getFileLogicCategoryFullTextSearchResult(folderCategory: string, searchViewId: string, transactionId: string, searchText: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetFileLogicCategoryFullTextSearchResult?folderCategory=${folderCategory}&searchViewId=${searchViewId}&transactionId=${transactionId}&searchText=${searchText}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get category search results');
    return response.json();
  }

  async getFileFolderFullTextSearchResult(searchViewId: string, folderId: string, emAppFolderSearchOption: string, transactionId: string, searchText: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/GetFileFolderFullTextSearchResult?searchViewId=${searchViewId}&folderId=${folderId}&emAppFolderSearchOption=${emAppFolderSearchOption}&transactionId=${transactionId}&searchText=${searchText}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get folder search results');
    return response.json();
  }

  async moveFileToRecycleBin(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/MoveFileToRecycleBin`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to move file to recycle bin');
    return response.json();
  }

  async restoreFileFromRecycleBin(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RestoreFileFromRecycleBin`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to restore file from recycle bin');
    return response.json();
  }

  async deleteFileFromRecycleBin(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/DeleteFileFromRecycleBin`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to delete file from recycle bin');
    return response.json();
  }

  async retrieveFolderHierarchyDto(transactionId: string, entryFolderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RetrieveFolderHairarchyDto?transactionId=${transactionId}&entryFolderId=${entryFolderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve folder hierarchy');
    return response.json();
  }

  async retrieveOneAppSeFolderExDto(folderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RetrieveOneAppSefolderExDto?folderId=${folderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve folder');
    return response.json();
  }

  async saveAppSeFolder(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/SaveAppSefolder`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save folder');
    return response.json();
  }

  async deleteAppSeFolder(folderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/DeleteAppSefolder?folderId=${folderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete folder');
    return response.json();
  }

  async pasteAppSeFolder(folderId: string, parentFolderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/PasteAppSefolder?folderId=${folderId}&parentFolderId=${parentFolderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to paste folder');
    return response.json();
  }

  async retrieveOneAppSeFolderResourceList(folderId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RetrieveOneAppSefolderResourceList?folderId=${folderId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve folder resource list');
    return response.json();
  }

  async saveAppSefolderResource(data: { FolderId: number; TransactionId: number; AppSefolderResourceExDtoSet: any[]; DeletedItemIds?: number[] }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/SaveAppSefolderResource`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save folder security');
    return response.json();
  }

  async applySecurityToSubFolders(data: { FolderId: number; TransactionId: number; AppSefolderResourceExDtoSet: any[] }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/ApplySecurityToSubFolders`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to apply security to subfolders');
    return response.json();
  }

  async removeSecurityFromSubFolders(folderId: number, transactionId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFolderNavigation/RemoveSecurityFromSubFolders?folderId=${folderId}&transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to remove security from subfolders');
    return response.json();
  }
}

export const appFolderNavigationService = new AppFolderNavigationService(); 