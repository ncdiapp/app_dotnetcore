import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

// Cache for table list results
interface TableListCacheEntry {
    data: any;
    timestamp: number;
}

class TableListCache {
    private cache: Map<string, TableListCacheEntry> = new Map();

    private getCacheKey(dataSourceRegisterId: number | null, saasFilterOption: number | null, filterByApplicationId: number | null): string {
        return `${dataSourceRegisterId ?? 'null'}_${saasFilterOption ?? 'null'}_${filterByApplicationId ?? 'null'}`;
    }

    get(
      dataSourceRegisterId: number | null,
      saasFilterOption: number | null,
      filterByApplicationId: number | null,
      maxAgeMs?: number
    ): any | null {
        const key = this.getCacheKey(dataSourceRegisterId, saasFilterOption, filterByApplicationId);
        const entry = this.cache.get(key);
        
        if (!entry) {
            return null;
        }
        if (maxAgeMs != null && maxAgeMs > 0) {
            const ageMs = Date.now() - entry.timestamp;
            if (ageMs > maxAgeMs) {
                this.cache.delete(key);
                return null;
            }
        }

        return entry.data;
    }

    set(dataSourceRegisterId: number | null, saasFilterOption: number | null, filterByApplicationId: number | null, data: any): void {
        const key = this.getCacheKey(dataSourceRegisterId, saasFilterOption, filterByApplicationId);
        this.cache.set(key, {
            data,
            timestamp: Date.now()
        });
    }

    buildKey(dataSourceRegisterId: number | null, saasFilterOption: number | null, filterByApplicationId: number | null): string {
      return this.getCacheKey(dataSourceRegisterId, saasFilterOption, filterByApplicationId);
    }

    clear(): void {
        this.cache.clear();
    }
}

const tableListCache = new TableListCache();
const TABLE_LIST_CACHE_TTL_MS = 10 * 60 * 1000; // 10 minutes

class SchemaMetadataService {
  private tableListPendingRequests: Map<string, Promise<any>> = new Map();
 
  async getDatabaseSchemas(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/GetDatabaseSchemas`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get database schemas');
    return response.json();
  }

  async getSchemaByName(schemaName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/GetSchemaByName?schemaName=${schemaName}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get schema');
    return response.json();
  }

  async getTableMetadata(schemaName: string, tableName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/GetTableMetadata?schemaName=${schemaName}&tableName=${tableName}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get table metadata');
    return response.json();
  }

  async createTable(schemaName: string, tableMetadata: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/CreateTable?schemaName=${schemaName}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(tableMetadata)
    });
    if (!response.ok) throw new Error('Failed to create table');
    // Invalidate cached table list so the newly created table appears immediately.
    tableListCache.clear();
  }

  async updateTable(schemaName: string, tableName: string, tableMetadata: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/UpdateTable?schemaName=${schemaName}&tableName=${tableName}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(tableMetadata)
    });
    if (!response.ok) throw new Error('Failed to update table');
  }

  async deleteTable(schemaName: string, tableName: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/DeleteTable?schemaName=${schemaName}&tableName=${tableName}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete table');
  }

  async addField(schemaName: string, tableName: string, field: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/AddField?schemaName=${schemaName}&tableName=${tableName}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(field)
    });
    if (!response.ok) throw new Error('Failed to add field');
  }

  async updateField(schemaName: string, tableName: string, fieldName: string, field: any): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/UpdateField?schemaName=${schemaName}&tableName=${tableName}&fieldName=${fieldName}`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify(field)
    });
    if (!response.ok) throw new Error('Failed to update field');
  }

  async deleteField(schemaName: string, tableName: string, fieldName: string): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/DeleteField?schemaName=${schemaName}&tableName=${tableName}&fieldName=${fieldName}`, {
      method: 'DELETE',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete field');
  }

  async validateSchema(schema: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetadata/ValidateSchema`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(schema)
    });
    if (!response.ok) throw new Error('Failed to validate schema');
    return response.json();
  }

  // Methods for MetaDataManagement
  // Always fetches from server and refreshes cache with newest data
  async getDataSourceTableAndViewList(
    dataSourceRegisterId: number | null,
    saasFilterOption: number | null,
    filterByApplicationId: number | null,
    options?: { bypassHttpCache?: boolean }
  ): Promise<any> {
    const bypass = options?.bypassHttpCache === true;
    const cacheKey = tableListCache.buildKey(dataSourceRegisterId, saasFilterOption, filterByApplicationId);

    if (!bypass) {
      const cachedResult = tableListCache.get(
        dataSourceRegisterId,
        saasFilterOption,
        filterByApplicationId,
        TABLE_LIST_CACHE_TTL_MS
      );
      if (cachedResult !== null) {
        return cachedResult;
      }
      const pending = this.tableListPendingRequests.get(cacheKey);
      if (pending) {
        return pending;
      }
    }

    const params = new URLSearchParams();
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    params.append('saasFilterOption', saasFilterOption !== null ? saasFilterOption.toString() : '');
    params.append('filterByApplicationId', filterByApplicationId !== null ? filterByApplicationId.toString() : '');
    if (bypass) {
      params.append('_', Date.now().toString());
    }
    const requestPromise = (async () => {
      const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetDataSourceTableAndViewList?${params.toString()}`, {
        headers: getHeaders(),
        cache: bypass ? 'no-store' : 'default'
      });
      if (!response.ok) throw new Error('Failed to get data source table and view list');
      const result = await response.json();
      // Refresh cache with newest data from server
      tableListCache.set(dataSourceRegisterId, saasFilterOption, filterByApplicationId, result);
      return result;
    })();

    if (!bypass) {
      this.tableListPendingRequests.set(cacheKey, requestPromise);
    }
    try {
      return await requestPromise;
    } finally {
      if (!bypass) this.tableListPendingRequests.delete(cacheKey);
    }
  }

  // Gets data from cache first, if no cache exists, calls getDataSourceTableAndViewList() to fetch and cache
  async getDataSourceTableAndViewListFromCache(dataSourceRegisterId: number | null, saasFilterOption: number | null, filterByApplicationId: number | null): Promise<any> {
    // Check cache first
    const cachedResult = tableListCache.get(
      dataSourceRegisterId,
      saasFilterOption,
      filterByApplicationId,
      TABLE_LIST_CACHE_TTL_MS
    );
    if (cachedResult !== null) {
      return cachedResult;
    }

    // If no cache, fetch from server (which will also cache the result)
    return this.getDataSourceTableAndViewList(dataSourceRegisterId, saasFilterOption, filterByApplicationId);
  }

  // Method to clear table list cache (useful for manual refresh)
  clearTableListCache(): void {
    tableListCache.clear();
  }

  async getOneDatabaseTableSchema(tableName: string, dataSourceRegisterId: number | null, schemaOwner: string | null): Promise<any> {
    const params = new URLSearchParams();
    params.append('tableName', tableName);
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    params.append('schemaOwner', schemaOwner || '');
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetOneDatabaseTableSchema?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get database table schema');
    return response.json();
  }

  async executeQueryResult(keyValue: { Key: number; Value: string }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/ExcuteQueryResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(keyValue)
    });
    if (!response.ok) throw new Error('Failed to execute query');
    return response.json();
  }

  async getDatabaseTableBuiltInQuery(tableName: string, dataSourceRegisterId: number | null, schemaOwner: string | null, emBuiltInQueryType: number): Promise<string> {
    const params = new URLSearchParams();
    params.append('tableName', tableName);
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    params.append('schemaOwner', schemaOwner || '');
    params.append('emBuiltInQueryType', emBuiltInQueryType.toString());
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetDatabaseTableBuiltInQuery?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get built-in query');
    return response.text();
  }

  async dropDatabaseTable(databaseTable: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/DropDatabaseTable`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseTable)
    });
    if (!response.ok) throw new Error('Failed to drop database table');
    return response.json();
  }

  async renameTable(orgTableName: string, newTableName: string, dataSourceRegisterId: number | null, schemaOwner: string | null): Promise<any> {
    const params = new URLSearchParams();
    params.append('orgTableName', orgTableName);
    params.append('newTableName', newTableName);
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    params.append('schemaOwner', schemaOwner || '');
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/RenameTableName?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to rename table');
    return response.json();
  }

  async getTableData(tableName: string, dataSourceRegisterId: number | null, schemaOwner: string | null, recordLimit: number | null = null): Promise<any> {
    const params = new URLSearchParams();
    params.append('tableName', tableName);
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    params.append('schemaOwner', schemaOwner || '');
    params.append('recordLimit', recordLimit !== null ? recordLimit.toString() : '');
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetTableData?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get table data');
    return response.json();
  }

  async getDataBaseSchemaOwnerList(dataSourceRegisterId: number | null): Promise<string[]> {
    const params = new URLSearchParams();
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetDataBaseSchemaOwnerList?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get database schema owner list');
    return response.json();
  }

  async createNewTable(databaseTable: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/CreateNewTable`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseTable)
    });
    if (!response.ok) throw new Error('Failed to create new table');
    // Invalidate cached table list so the newly created table appears immediately.
    tableListCache.clear();
    return response.json();
  }

  async saveModifiedTableSchema(schemaMetaDataDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/SaveModifiedTableSchema`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(schemaMetaDataDto)
    });
    if (!response.ok) throw new Error('Failed to save modified table schema');
    return response.json();
  }

  async saveDatabaseViewFromDesignQuery(databaseViewDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/SaveDatabaseViewFromDesignQuery`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseViewDto)
    });
    if (!response.ok) throw new Error('Failed to save database view');
    return response.json();
  }

  async getViewQueryText(viewName: string): Promise<string> {
    const params = new URLSearchParams();
    params.append('viewName', viewName);
    
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetViewQueryText?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get view query text');
    return response.text();
  }

  async updateDatabaseViewDtoFromQuery(databaseViewDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/UpdateDatabaseViewDtoFromQuery`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseViewDto)
    });
    if (!response.ok) throw new Error('Failed to update database view DTO from query');
    return response.json();
  }

  async addTablesToDatabaseView(tableAddRemoveDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/AddTablesToDatabaseView`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(tableAddRemoveDto)
    });
    if (!response.ok) throw new Error('Failed to add tables to database view');
    return response.json();
  }

  async removeTablesFromDatabaseView(tableAddRemoveDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/RemoveTablesFromDatabaseView`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(tableAddRemoveDto)
    });
    if (!response.ok) throw new Error('Failed to remove tables from database view');
    return response.json();
  }

  async addOneJoinConditionLineToDatabaseView(joinUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/AddOneJoinConditionLineToDatabaseView`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(joinUpdateDto)
    });
    if (!response.ok) throw new Error('Failed to add join condition line');
    return response.json();
  }

  /** Add FK constraint to DB and rebuild ER diagram Joins. Server-side save; bind to response, no client Save/Refresh. */
  async addOneFkLineToErDiagram(joinUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/AddOneFkLineToErDiagram`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(joinUpdateDto)
    });
    if (!response.ok) throw new Error('Failed to add FK line to ER diagram');
    return response.json();
  }

  /** Remove FK constraint from DB and rebuild ER diagram Joins. Server-side save; bind to response, no client Save/Refresh. */
  async removeFkLinesFromErDiagram(joinUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/RemoveFkLinesFromErDiagram`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(joinUpdateDto)
    });
    if (!response.ok) throw new Error('Failed to remove FK lines from ER diagram');
    return response.json();
  }

  async updateDatabaseViewSelectedColumns(databaseViewDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/UpdateDatabaseViewSelectedColumns`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseViewDto)
    });
    if (!response.ok) throw new Error('Failed to update database view selected columns');
    return response.json();
  }

  async removeJoinConditionLinesFromDatabaseView(joinUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/RemoveJoinConditionLinesFromDatabaseView`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(joinUpdateDto)
    });
    if (!response.ok) throw new Error('Failed to remove join condition lines');
    return response.json();
  }

  async updateDatabaseViewJoinMethod(joinUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/UpdateDatabaseViewJoinMethod`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(joinUpdateDto)
    });
    if (!response.ok) throw new Error('Failed to update database view join method');
    return response.json();
  }

  async quickGenerateTransactionDefaultSearchNavigation(transactionId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/QuickGenerateTransactionDefaultSeachNavigation?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to generate transaction default search navigation');
    return response.json();
  }

  /** Angular schemaMetaDataSvc.SaveDataSetAndCreateFolderViewNavigation */
  async saveDataSetAndCreateFolderViewNavigation(databaseViewUpdateDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/SaveDataSetAndCreateFolderViewNavigation`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(databaseViewUpdateDto ?? null),
    });
    if (!response.ok) throw new Error('Failed to save folder view navigation');
    return response.json();
  }

  async getSysStoredProcedureNameList(searchText: string, dataSourceRegisterId: number | null): Promise<string[]> {
    const params = new URLSearchParams();
    params.append('searchText', searchText || '');
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');

    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetSysStoredProcedureNameList?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get stored procedure name list');
    return response.json();
  }

  async getStoredProcedureParameterList(storedProcedureName: string, schemaOwner: string | null, dataSourceRegisterId: number | null): Promise<any[]> {
    const params = new URLSearchParams();
    params.append('storedProcedureName', storedProcedureName);
    params.append('schemaOwner', schemaOwner || '');
    params.append('dataSourceRegisterId', dataSourceRegisterId !== null ? dataSourceRegisterId.toString() : '');

    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/GetStoredProcedureParamterList?${params.toString()}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get stored procedure parameter list');
    return response.json();
  }

  /** After Excel upload into temp table for an existing released import setting. Matches SchemaMetaDataController.UpdateImportedTableDataFromTempTable. */
  async updateImportedTableDataFromTempTable(
    importSettingDataSetId: number,
    uploadedFileTempTableName: string,
    fileName: string
  ): Promise<any> {
    const params = new URLSearchParams();
    params.set('importSettingDataSetId', String(importSettingDataSetId));
    params.set('uploadedFileTempTableName', uploadedFileTempTableName);
    params.set('fileName', fileName || '');
    const response = await fetch(
      `${endpoints.BASE_URL}/webapi/SchemaMetaData/UpdateImportedTableDataFromTempTable?${params.toString()}`,
      { headers: getHeaders() }
    );
    if (!response.ok) throw new Error('Failed to update imported table from Excel');
    return response.json();
  }

  /**
   * Run table import pipeline (simulate while draft, full release when Status = Released).
   * SchemaMetaDataController.CreateTableImportSettingAndProcessImport → ProcessMetaDataTablesImport.
   */
  async createTableImportSettingAndProcessImport(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/CreateTableImportSettingAndProcessImport`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto),
    });
    if (!response.ok) throw new Error('Failed to run table import process');
    return response.json();
  }

  /** Save draft for table-import settings (Excel/DB-to-DB/etc). Matches SchemaMetaDataController.SaveDraftTableImportSetting. */
  async saveDraftTableImportSetting(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/SaveDraftTableImportSetting`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto),
    });
    if (!response.ok) throw new Error('Failed to save draft table import setting');
    return response.json();
  }

  /** DB-to-DB: regenerate SourceColumns based on configured source table/dataset. */
  async resetDbToDbImportSourceColumns(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/ResetDbToDbImportSourceColumns`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto),
    });
    if (!response.ok) throw new Error('Failed to refresh DB-to-DB source columns');
    return response.json();
  }

  /** DB-to-DB: create a new import setting skeleton. Matches SchemaMetaDataController.CreateDbToDbTableImportSetting. */
  async createDbToDbTableImportSetting(aAppDataSetExDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/SchemaMetaData/CreateDbToDbTableImportSetting`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aAppDataSetExDto),
    });
    if (!response.ok) throw new Error('Failed to create DB-to-DB import setting');
    return response.json();
  }
}

export const schemaMetadataService = new SchemaMetadataService(); 