import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
import { normalizeIntegrationSettingParameterForSave } from '../helper/integrationPayloadHelper';
class IntegrationService {
  

  async retrieveAllAppIntegrationSettingDto(isIncludeAppBuiltInApi: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveAllAppIntergrationSettingDto?isIncludeAppBuiltInApi=${isIncludeAppBuiltInApi}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve integration settings');
    return response.json();
  }

  async retrieveOneAppIntegrationSettingExDto(integrationSettingId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveOneAppIntergrationSettingExDto?IntergrationSettingId=${integrationSettingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve integration setting');
    return response.json();
  }

  async retrieveAllJsonFileTableImportSettingDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveAllJsonFileTableImportSettingDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve JSON file table import settings');
    return response.json();
  }

  async retrieveAllApiStagingTableImportSettingDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveAllApiStagingTableImportSettingDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve API staging table import settings');
    return response.json();
  }

  async deleteOneAppIntegrationSetting(integrationSettingId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/DeleteOneAppIntergrationSetting?IntergrationSettingId=${integrationSettingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete integration setting');
    return response.json();
  }

  async saveAppIntegrationSettingExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/SaveAppIntergrationSettingExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save integration setting');
    return response.json();
  }

  async retrieveOneApiAvailableFetchDataNodeStructure(settingParameterId: string, rootNodeFixedName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveOneApiAvailableFetchDataNodeStructure?settingParameterId=${settingParameterId}&rootNodeFixedName=${rootNodeFixedName}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve API data node structure');
    return response.json();
  }

  async retrieveOneAppIntegrationSettingParameterExDto(settingParameterId: string, isIncludeApiDataStructure: boolean = false): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/RetrieveOneAppIntergrationSettingParameterExDto?settingParameterId=${settingParameterId}&isInlucdeApiDataStructure=${isIncludeApiDataStructure}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve integration setting parameter');
    return response.json();
  }

  async getAppSearchDefaultProviderApi(searchId: string, isIncludeApiDataStructure: boolean = false, appBaseUrl: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GetAppSearchDefaultProviderApi?searchId=${searchId}&isIncludeApiDataStructure=${isIncludeApiDataStructure}&appBaseUrl=${appBaseUrl}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get search default provider API');
    return response.json();
  }

  async getAppTransactionDefaultProviderApi(transactionId: string, isIncludeApiDataStructure: boolean = false, appBaseUrl: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GetAppTransactionDefaultProviderApi?transactionId=${transactionId}&isIncludeApiDataStructure=${isIncludeApiDataStructure}&appBaseUrl=${appBaseUrl}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get transaction default provider API');
    return response.json();
  }

  async saveAppIntegrationSettingParameterExDto(data: any): Promise<any> {
    const payload = normalizeIntegrationSettingParameterForSave(data);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/SaveAppIntergrationSettingParameterExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(payload)
    });
    if (!response.ok) throw new Error('Failed to save integration setting parameter');
    return response.json();
  }

  async buildJsonImportTableDiagramFromSetting(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BuildJsonImportTableDiagramFromSetting`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to build JSON import table diagram');
    return response.json();
  }

  async deleteOneAppIntegrationSettingParameter(settingParameterId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/DeleteOneAppIntergrationSettingParameter?settingParameterId=${settingParameterId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete integration setting parameter');
    return response.json();
  }

  async createJsonFileDatabaseTableImportSettingByFileId(jsonFileId: string, dataSourceRegId: string, isImportToExistingTable: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/CreateJsonFileDatabaseTableImportSettingByFileId?jsonFileId=${jsonFileId}&dataSourceRegId=${dataSourceRegId}&isImportToExistingTable=${isImportToExistingTable}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to create JSON file database table import setting');
    return response.json();
  }

  async createJsonDatabaseTableImportSettingFromJsonText(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/CreateJsonDatabaseTableImportSettingFromJsonText`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create JSON database table import setting');
    return response.json();
  }

  async createStagingTableImportSettingFromApiOperation(apiOperationId: string, isImportToExistingTable: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/CreateStatingTableImportSettingFromApiOperation?apiOperationId=${apiOperationId}&isImportToExistingTable=${isImportToExistingTable}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to create staging table import setting');
    return response.json();
  }

  async generateSampleJsonDataFromApiConfig(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateSampleJsonDataFromApiConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate sample JSON data');
    return response.json();
  }

  async generateDefaultSchemaAndDataSetMappingFromSampleJson(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateDefaultSchemaAndDataSetMappingFromSampleJson`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate schema and data set mapping');
    return response.json();
  }

  async generateRuntimeSchemaFromDataSetMapping(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateRuntimeSchemaFromDataSetMapping`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate runtime schema');
    return response.json();
  }

  async createOrAlterDatabaseTablesFromRuntimeSchema(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/CreateOrAlterDatabaseTablesFromRuntimeSchema`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create/alter database tables');
    return response.json();
  }

  async generateScriptsFromRuntimeSchema(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateScriptsFromRuntimeSchema`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate scripts');
    return response.json();
  }

  async generateTableAndScriptsFromSchemaDataSetMappingDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateTableAndScriptsFromSchemaDataSetMappingDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to generate table and scripts');
    return response.json();
  }

  async executeOneOperationWithTestParameters(settingParameterId: string, isSimulate: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/ExecuteOneOperationWithTestParameters?settingParameterId=${settingParameterId}&isSimulate=${isSimulate}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to execute operation');
    return response.json();
  }

  async executeDataImportOnJsonFileTableImportSetting(importSettingId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/ExecuteDataImportOnJsonFileTableImportSetting?importSettingId=${importSettingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to execute data import');
    return response.json();
  }

  async updateStagingTableDataFromJsonUpload(importSettingId: string, jsonFileId: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Integration/UpdateStagingTableDataFromJsonUpload?importSettingId=${importSettingId}&jsonFileId=${jsonFileId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to update staging table from JSON upload');
    return response.json();
  }

  async updateJsonSchemaFromJsonUpload(importSettingId: string, jsonFileId: string): Promise<any> {
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Integration/UpdateJsonSchemaFromJsonUpload?importSettingId=${importSettingId}&jsonFileId=${jsonFileId}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to update JSON schema from upload');
    return response.json();
  }

  async dropAllStagingTablesByImportSettingId(settingParameterId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/DropAllStagingTablesByImportSettingId?settingParameterId=${settingParameterId || ''}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to drop staging tables');
    return response.json();
  }
}

export const integrationService = new IntegrationService(); 