import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';
import { normalizeIntegrationSettingParameterForSave, prettyPrintJsonForDisplay } from '../helper/integrationPayloadHelper';
class IntegrationService {

  private withPrettyApiConfig(dto: any): any {
    if (dto == null || typeof dto !== 'object') return dto;
    if (typeof dto.ApiconfigParameters !== 'string' || !dto.ApiconfigParameters.trim()) return dto;
    return { ...dto, ApiconfigParameters: prettyPrintJsonForDisplay(dto.ApiconfigParameters) };
  }
  

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
    // 204 / empty body (legacy null) — do not call response.json()
    if (response.status === 204) return null;
    const text = await response.text();
    if (!text) return null;
    return JSON.parse(text);
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
    return this.withPrettyApiConfig(await response.json());
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
    const result = await response.json();
    if (result?.Object) result.Object = this.withPrettyApiConfig(result.Object);
    return result;
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

  async listStoredProceduresForApiBuilder(dataSourceId: number, schema?: string, take?: number): Promise<any[]> {
    const q = new URLSearchParams();
    q.set('dataSourceId', String(dataSourceId));
    if (schema) q.set('schema', schema);
    if (take != null) q.set('take', String(take));
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Integration/ListStoredProceduresForApiBuilder?${q.toString()}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to list stored procedures');
    const data = await response.json();
    return Array.isArray(data) ? data : [];
  }

  async getStoredProcedureForApiBuilder(
    dataSourceId: number,
    spName: string,
    schema?: string | null,
  ): Promise<any> {
    const q = new URLSearchParams();
    q.set('dataSourceId', String(dataSourceId));
    q.set('spName', spName || '');
    if (schema) q.set('schema', schema);
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Integration/GetStoredProcedureForApiBuilder?${q.toString()}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to reload stored procedure parameters');
    return response.json();
  }

  async batchCreateStoredProcedureApis(payload: {
    DataSourceId: number;
    GenerateAiDescription?: boolean;
    /** Default true: execute read-like SPs and save JsonSampleData. */
    CaptureSampleOnGenerate?: boolean;
    Items: Array<{
      Schema?: string;
      SpName: string;
      ActionCode?: string;
      Parameters?: Array<{
        Name?: string;
        Type?: string;
        Direction?: string;
        MaxLength?: number | null;
        Ordinal?: number;
        HasDefault?: boolean;
        DefaultValue?: string | null;
      }>;
    }>;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchCreateStoredProcedureApis`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error('Failed to batch create stored procedure APIs');
    return response.json();
  }

  async generateStoredProcedureApiDescription(payload: {
    DataSourceId: number;
    Schema?: string | null;
    SpName: string;
    Parameters?: Array<{
      Name?: string;
      Type?: string;
      Direction?: string;
      MaxLength?: number | null;
      Ordinal?: number;
      HasDefault?: boolean;
      DefaultValue?: string | null;
    }>;
    ExistingDescription?: string | null;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/GenerateStoredProcedureApiDescription`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error('Failed to generate stored procedure API description');
    return response.json();
  }

  async batchDeleteAppIntegrationSettingParameters(ids: number[]): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchDeleteAppIntergrationSettingParameters`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ Ids: ids }),
    });
    if (!response.ok) throw new Error('Failed to batch delete APIs');
    return response.json();
  }

  async ensureStoredProcedureRegisterTable(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/EnsureStoredProcedureRegisterTable`, {
      method: 'POST',
      headers: getHeaders(),
      body: '{}',
    });
    if (!response.ok) throw new Error('Failed to ensure SP register table');
    return response.json();
  }

  async listStoredProcedureRegister(dataSourceId?: number | null): Promise<any> {
    const q = new URLSearchParams();
    if (dataSourceId != null && dataSourceId > 0) q.set('dataSourceId', String(dataSourceId));
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/Integration/ListStoredProcedureRegister?${q.toString()}`,
      { headers: getHeaders() },
    );
    if (!response.ok) throw new Error('Failed to list SP register');
    return response.json();
  }

  async saveStoredProcedureRegister(payload: {
    Id: number;
    Description?: string | null;
    UsageText?: string | null;
    IsPublishedToAgent?: boolean | null;
    InputJson?: string | null;
    OutputColumnsJson?: string | null;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/SaveStoredProcedureRegister`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error('Failed to save SP register');
    return response.json();
  }

  async batchDeleteStoredProcedureRegister(ids: number[]): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchDeleteStoredProcedureRegister`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ Ids: ids }),
    });
    if (!response.ok) throw new Error('Failed to delete SP register rows');
    return response.json();
  }

  async batchSetPublishStoredProcedureRegister(ids: number[], isPublishedToAgent: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchSetPublishStoredProcedureRegister`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ Ids: ids, IsPublishedToAgent: isPublishedToAgent }),
    });
    if (!response.ok) throw new Error('Failed to update publish status');
    return response.json();
  }

  async batchSavePublishStoredProcedureRegister(
    items: Array<{ Id: number; IsPublishedToAgent: boolean }>,
  ): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchSavePublishStoredProcedureRegister`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ Items: items }),
    });
    if (!response.ok) throw new Error('Failed to save publish changes');
    return response.json();
  }

  async batchTrainStoredProcedureRegister(payload: {
    DataSourceId: number;
    PublishToAgent?: boolean;
    Items: Array<{
      Schema?: string;
      SpName: string;
      Parameters?: Array<{
        Name?: string;
        Type?: string;
        Direction?: string;
        MaxLength?: number | null;
        Ordinal?: number;
        HasDefault?: boolean;
        DefaultValue?: string | null;
      }>;
    }>;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Integration/BatchTrainStoredProcedureRegister`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(payload),
    });
    if (!response.ok) throw new Error('Failed to AI-train SP register');
    return response.json();
  }

  private apiServerRootPromise: Promise<string> | null = null;

  /** Scheme and host of the API server that received this call, with no path prefix. */
  loadApiServerRoot(): Promise<string> {
    if (!this.apiServerRootPromise) {
      this.apiServerRootPromise = fetch(`${endpoints.BASE_URL}/webapi/Integration/GetApiServerRoot`, {
        headers: getHeaders(),
        credentials: 'include',
      })
        .then(async (response) => {
          if (!response.ok) throw new Error(String(response.status));
          const data = await response.json();
          return String(data?.ServerRoot ?? data?.serverRoot ?? '').replace(/\/$/, '');
        })
        .catch((error) => {
          this.apiServerRootPromise = null;
          throw error;
        });
    }
    return this.apiServerRootPromise;
  }
}

export const integrationService = new IntegrationService(); 