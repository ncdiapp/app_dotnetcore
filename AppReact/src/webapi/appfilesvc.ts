import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
export class AppFileService {
  
  async deleteAppFileDtoByIds(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/DeleteAppFileDtoByIds`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to delete app files');
    return response.json();
  }

  async getFileUpdateHistoryDtoListByFileId(fileId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/GetFileUpdateHistoryDtoListByFileId?fileId=${fileId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get file update history');
    return response.json();
  }

  async rollBackFileVersion(fileId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/RollBackFileVersion?fileId=${fileId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to rollback file version');
    return response.json();
  }

  async retrieveCurrentUserFileFolderHierarchyDto(folderCategory: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/RetrieveCurrentUserFileFolderHairarchyDto?folderCategory=${folderCategory}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve folder hierarchy');
    return response.json();
  }

  async getExcelFileColumnNameList(excelFilePath: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/GetExcelFileColumnNameList?excelFilePath=${excelFilePath}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get Excel column names');
    return response.json();
  }

  async retrieveOneOrgAppFileExDto(fileId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppFile/RetrieveOneOrgAppFileExDto?fileId=${fileId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app file');
    return response.json();
  }
}

export const appFileService = new AppFileService(); 