import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export interface OperationCallResult<T> {
  Object: T;
  ObjectList?: T[];
  ValidationResult: {
    Items: Array<{
      Type: string;
      PropertyName: string;
      Message: string;
    }>;
    IsValid: boolean;
  };
  IsSuccessful: boolean;
  HasResult: boolean;
}

export interface PlmImportSessionDto {
  SessionId?: number | null;
  SessionGuid?: string | null;
  CompanyId?: number | null;
  SaasApplicationId?: number | null;
  SaasApplicationName?: string | null;
  CreatedByUserId?: number | null;
  CreatedAt?: string | null;
  UpdatedAt?: string | null;
  SessionStatus?: string | null;
  CurrentStepCode?: string | null;
  PlmConnectionString?: string | null;
  HasPlmConnection?: boolean;
  StepStateJson?: string | null;
  DataSourceDiscoveryJson?: string | null;
}

export interface PlmConnectionTestRequestDto {
  ConnectionString: string;
  TargetCompanyId?: number | null;
}

export interface PlmConnectionTestResultDto {
  IsSuccess: boolean;
  ServerVersion?: string | null;
  DatabaseName?: string | null;
  ErrorMessage?: string | null;
}

export interface PlmDiscoverDataSourcesRequestDto {
  PlmConnectionString: string;
  SaasApplicationId?: number | null;
  TargetCompanyId?: number | null;
  SessionId?: number | null;
}

export interface PlmDataSourceDiscoveryItemDto {
  DataSourceFrom: number;
  DataSourceFromName?: string | null;
  DataSourceName?: string | null;
  ConnectionString?: string | null;
  HasConnectionString: boolean;
  ConnectionTestSuccess: boolean;
  ConnectionTestMessage?: string | null;
  RegisteredDataSourceId?: number | null;
  RegisteredDataSourceName?: string | null;
  IsReusedRegister?: boolean;
}

export interface PlmDiscoverDataSourcesResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  DataSources?: PlmDataSourceDiscoveryItemDto[];
}

export interface PlmImportJobDto {
  JobId: number;
  SessionId: number;
  JobType?: string | null;
  Status?: string | null;
  ProgressPercent: number;
  ProgressMessage?: string | null;
  ResultJson?: string | null;
  ErrorMessage?: string | null;
  CreatedAt?: string | null;
  UpdatedAt?: string | null;
  StartedAt?: string | null;
  CompletedAt?: string | null;
}

export interface PlmTableExportEntityRefDto {
  EntityId: number;
  EntityCode?: string | null;
}

export interface PlmTableExportIssueDto {
  EntityId: number;
  EntityCode?: string | null;
  SchemaOwner?: string | null;
  TableName?: string | null;
  IssueType?: string | null;
  Message?: string | null;
}

export interface PlmTableExportPlanItemDto {
  SchemaOwner?: string | null;
  TableName?: string | null;
  TargetTableName?: string | null;
  PlmEntityCount?: number;
  SourceTableExists?: boolean;
  Entities?: PlmTableExportEntityRefDto[];
}

export interface PlmTableExportPlanDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  MissingSourceTableCount?: number;
  Issues?: PlmTableExportIssueDto[];
  Tables?: PlmTableExportPlanItemDto[];
}

export interface PlmTableExportResultItemDto {
  SchemaOwner?: string | null;
  TableName?: string | null;
  IsSuccess?: boolean;
  RowsCopied?: number;
  ErrorMessage?: string | null;
}

export interface PlmSketchImportPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  SourceSketchCount: number;
  SourceWithBinaryCount: number;
  ImageCount: number;
  FileCount: number;
  ExistingAppFileCount: number;
  ReadyToImportCount: number;
  MissingBinaryCount: number;
  Warnings?: string[];
}

export interface PlmSketchImportResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  SourceSketchCount: number;
  InsertedCount: number;
  SkippedExistingCount: number;
  SkippedMissingBinaryCount: number;
  ImageInsertedCount: number;
  FileInsertedCount: number;
  FailedCount: number;
  Errors?: string[];
}

export interface PlmFolderImportScopePreviewDto {
  PlmFolderType: number;
  PlmFolderTypeName?: string | null;
  AppTransactionId?: number | null;
  AppAnchorFolderId?: number | null;
  TotalPlmFolders: number;
  ExistingMappedFolders: number;
  ToCreateCount: number;
  MissingParentCount: number;
}

export interface PlmFolderImportPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  Scopes?: PlmFolderImportScopePreviewDto[];
  ColorDetailSourceCount: number;
  ColorDetailReadyToImport: number;
  ColorDetailExistingCount: number;
  Warnings?: string[];
}

export interface PlmFolderImportResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  FoldersCreated: number;
  FoldersSkippedExisting: number;
  MappingsWritten: number;
  ColorDetailsInserted: number;
  ColorDetailsSkipped: number;
  Errors?: string[];
}

export interface PlmFolderPlacementPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  ProductReadyCount: number;
  ProductMissingFolderMapCount: number;
  ColorDetailReadyCount: number;
  ColorDetailMissingFolderMapCount: number;
  ImageReadyCount: number;
  ImageMissingFolderMapCount: number;
  Warnings?: string[];
}

export interface PlmFolderPlacementResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  ProductsUpdated: number;
  ColorDetailsUpdated: number;
  AppFilesUpdated: number;
  SkippedNoMapping: number;
  Errors?: string[];
}

export interface PlmSystemDefineEntityPreviewItemDto {
  PlmEntityId: number;
  PlmEntityCode?: string | null;
  TargetEntityCode?: string | null;
  Description?: string | null;
  TableName?: string | null;
  SchemaOwner?: string | null;
  PlmDataSourceFrom?: number | null;
  AppDataSourceFrom?: number | null;
  TargetDatabaseName?: string | null;
  IdentityField?: string | null;
  DisplayFiled1?: string | null;
  DisplayFiled2?: string | null;
  DisplayFiled3?: string | null;
  ImportStatus?: string | null;
  ImportAction?: string | null;
  SkipReason?: string | null;
}

export interface PlmSystemDefineEntityBlockerDto {
  PlmEntityId: number;
  TargetEntityCode?: string | null;
  TableName?: string | null;
  TargetDatabaseName?: string | null;
  Issue?: string | null;
}

export interface PlmSystemDefineEntityPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  ReadyCount?: number;
  SkippedCount?: number;
  BlockerCount?: number;
  Entities?: PlmSystemDefineEntityPreviewItemDto[];
  Blockers?: PlmSystemDefineEntityBlockerDto[];
}

export interface PlmUserDefineEntityPreviewItemDto {
  PlmEntityId: number;
  PlmEntityCode?: string | null;
  TargetEntityCode?: string | null;
  Description?: string | null;
  TableName?: string | null;
  AppTargetType?: string | null;
  ColumnCount?: number;
  PlmRowCount?: number;
  ImportOrder?: number;
  ImportStatus?: string | null;
  ImportAction?: string | null;
  SkipReason?: string | null;
}

export interface PlmUserDefineEntityPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  ReadyCount?: number;
  SkippedCount?: number;
  BlockerCount?: number;
  Entities?: PlmUserDefineEntityPreviewItemDto[];
  Blockers?: Array<{
    PlmEntityId: number;
    TargetEntityCode?: string | null;
    TableName?: string | null;
    Issue?: string | null;
  }>;
}

export interface PlmImportLogDto {
  LogId: number;
  SessionId: number;
  JobId?: number | null;
  StepCode?: string | null;
  Action?: string | null;
  Status?: string | null;
  TargetKey?: string | null;
  PlmIntegrationKey?: string | null;
  RowsAffected?: number | null;
  DurationMs?: number | null;
  Message?: string | null;
  CreatedAt?: string | null;
}

export interface PlmUserDefineEntityImportResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  InsertedCount: number;
  UpdatedCount: number;
  SkippedCount: number;
  RowsImported: number;
}

export interface PlmTemplatePreviewItemDto {
  PlmTemplateId: number;
  PlmTemplateName?: string | null;
  PlmTabId: number;
  PlmTabName?: string | null;
  TabType?: string | null;
  ImportStatus?: string | null;
  ImportAction?: string | null;
  SiblingTableName?: string | null;
  ChildTableNames?: string | null;
  SiblingFieldCount: number;
  GridFieldCount: number;
  WarningCount: number;
  SkipReason?: string | null;
}

export interface PlmTemplatePreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  TemplateCount: number;
  ReadyCount: number;
  SkippedCount: number;
  BlockerCount: number;
  WarningCount: number;
  Tabs?: PlmTemplatePreviewItemDto[];
}

export interface PlmTemplateMappingGridRowDto {
  PlmTemplateId: number;
  PlmTemplateName?: string | null;
  PlmTabId: number;
  PlmTabName?: string | null;
  TabType?: string | null;
  ImportStatus?: string | null;
  ImportAction?: string | null;
  TransactionGroupName?: string | null;
  TransactionName?: string | null;
  IntegrationId?: string | null;
  SiblingTableName?: string | null;
  ChildTableNames?: string | null;
  SiblingFieldCount: number;
  GridFieldCount: number;
  WarningCount: number;
  SkipReason?: string | null;
  ShowTabWarning: boolean;
  SimilarTabGroupId?: string | null;
  SimilarTabJaccard?: number | null;
}

export interface PlmTemplateBlockAnalysisDto {
  BlockId: number;
  BlockName?: string | null;
  ReferencedTabCount: number;
  ReferencedTabLabels?: string[];
}

export interface PlmTemplateSimilarTabGroupDto {
  GroupId?: string | null;
  PlmTemplateId: number;
  SuggestedSharedTableName?: string | null;
  JaccardScore: number;
  TabIds?: number[];
  TabLabels?: string[];
}

export interface PlmTemplateMappingGridDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  TemplateCount: number;
  ReadyCount: number;
  SkippedCount: number;
  BlockerCount: number;
  WarningCount: number;
  Rows?: PlmTemplateMappingGridRowDto[];
  Blocks?: PlmTemplateBlockAnalysisDto[];
  SimilarTabGroups?: PlmTemplateSimilarTabGroupDto[];
  Blockers?: Array<{ PlmTabId?: number | null; PlmTabName?: string | null; Issue?: string | null }>;
  Warnings?: Array<{ Issue?: string | null }>;
  SavedSetting?: PlmTemplateImportSettingDto | null;
}

export interface PlmTemplateImportSettingRowDto {
  PlmTemplateId: number;
  PlmTabId: number;
  TransactionGroupName?: string | null;
  TransactionName?: string | null;
  IntegrationId?: string | null;
  SiblingTableName?: string | null;
  ImportStatus?: string | null;
}

export interface PlmTemplateTabSharedTableGroupDto {
  GroupId?: string | null;
  SharedTableName?: string | null;
  TabIds?: number[];
}

export interface PlmTemplateBlockStorageOverrideDto {
  BlockId: number;
  StorageTarget?: string | null;
  SharedTableName?: string | null;
}

export interface PlmTemplateImportSettingDto {
  Rows?: PlmTemplateImportSettingRowDto[];
  TabSharedTableGroups?: PlmTemplateTabSharedTableGroupDto[];
  BlockStorageOverrides?: PlmTemplateBlockStorageOverrideDto[];
}

export interface PlmTemplateMappingValidationDto {
  IsValid: boolean;
  Errors?: string[];
  Warnings?: string[];
}

class PlmMigrationService {
  private baseUrl = `${endpoints.BASE_URL}/webapi/PlmMigration`;

  async testPlmConnection(request: PlmConnectionTestRequestDto): Promise<OperationCallResult<PlmConnectionTestResultDto>> {
    const response = await fetch(`${this.baseUrl}/TestPlmConnection`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to test PLM connection');
    return response.json();
  }

  async discoverPlmDataSources(request: PlmDiscoverDataSourcesRequestDto): Promise<OperationCallResult<PlmDiscoverDataSourcesResultDto>> {
    const response = await fetch(`${this.baseUrl}/DiscoverPlmDataSources`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to discover PLM data sources');
    return response.json();
  }

  async getActiveImportSession(targetCompanyId?: number | null): Promise<OperationCallResult<PlmImportSessionDto>> {
    const qs = targetCompanyId != null ? `?targetCompanyId=${targetCompanyId}` : '';
    const response = await fetch(`${this.baseUrl}/ImportSession/active${qs}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to get active import session');
    return response.json();
  }

  async saveImportSession(dto: PlmImportSessionDto): Promise<OperationCallResult<PlmImportSessionDto>> {
    const response = await fetch(`${this.baseUrl}/ImportSession`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(dto),
    });
    if (!response.ok) throw new Error('Failed to save import session');
    return response.json();
  }

  async discardImportSession(sessionId?: number | null, targetCompanyId?: number | null): Promise<OperationCallResult<boolean>> {
    const params = new URLSearchParams();
    if (sessionId != null) params.set('sessionId', String(sessionId));
    if (targetCompanyId != null) params.set('targetCompanyId', String(targetCompanyId));
    const qs = params.toString() ? `?${params.toString()}` : '';
    const response = await fetch(`${this.baseUrl}/ImportSession/discard${qs}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to discard import session');
    return response.json();
  }

  async getImportJob(jobId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ImportJob/${jobId}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to get import job');
    return response.json();
  }

  async cancelImportJob(jobId: number): Promise<OperationCallResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/ImportJob/${jobId}/cancel`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to cancel import job');
    return response.json();
  }

  async getImportLog(sessionId?: number | null, targetCompanyId?: number | null): Promise<OperationCallResult<PlmImportLogDto[]>> {
    const params = new URLSearchParams();
    if (sessionId != null) params.set('sessionId', String(sessionId));
    if (targetCompanyId != null) params.set('targetCompanyId', String(targetCompanyId));
    const qs = params.toString() ? `?${params.toString()}` : '';
    const response = await fetch(`${this.baseUrl}/ImportLog${qs}`, {
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to get import log');
    return response.json();
  }

  async previewPlmTableExportPlan(sessionId: number): Promise<OperationCallResult<PlmTableExportPlanDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmTableExportPlan?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM table export plan');
    return response.json();
  }

  async executePlmTableExport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmTableExport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start PLM table export');
    return response.json();
  }

  async previewPlmSketchImport(sessionId: number): Promise<OperationCallResult<PlmSketchImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmSketchImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM sketch import');
    return response.json();
  }

  async executePlmSketchImport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmSketchImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start PLM sketch import');
    return response.json();
  }

  async previewPlmFolderImport(sessionId: number): Promise<OperationCallResult<PlmFolderImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmFolderImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM folder import');
    return response.json();
  }

  async executePlmFolderImport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmFolderImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start PLM folder import');
    return response.json();
  }

  async previewPlmFolderPlacement(sessionId: number): Promise<OperationCallResult<PlmFolderPlacementPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmFolderPlacement?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM folder placement');
    return response.json();
  }

  async executePlmFolderPlacement(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmFolderPlacement?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start PLM folder placement');
    return response.json();
  }

  async previewPlmColorImport(sessionId: number): Promise<OperationCallResult<PlmColorImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmColorImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM color import');
    return response.json();
  }

  async executePlmColorImport(request: PlmColorImportExecuteRequestDto): Promise<OperationCallResult<PlmColorImportExecuteResultDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmColorImport`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to execute PLM color import');
    return response.json();
  }

  async previewPlmPomImport(sessionId: number): Promise<OperationCallResult<PlmPomImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewPlmPomImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview PLM POM import');
    return response.json();
  }

  async executePlmPomImport(request: PlmPomImportExecuteRequestDto): Promise<OperationCallResult<PlmPomImportExecuteResultDto>> {
    const response = await fetch(`${this.baseUrl}/ExecutePlmPomImport`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to execute PLM POM import');
    return response.json();
  }

  async previewSystemDefineEntityImport(sessionId: number): Promise<OperationCallResult<PlmSystemDefineEntityPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewSystemDefineEntityImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview System Define entity import');
    return response.json();
  }

  async executeSystemDefineEntityImport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteSystemDefineEntityImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start System Define entity import');
    return response.json();
  }

  async previewUserDefineEntityImport(sessionId: number): Promise<OperationCallResult<PlmUserDefineEntityPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewUserDefineEntityImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview User Define entity import');
    return response.json();
  }

  async executeUserDefineEntityImport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteUserDefineEntityImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start User Define entity import');
    return response.json();
  }

  async previewTemplateMapping(sessionId: number): Promise<OperationCallResult<PlmTemplatePreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewTemplateMapping?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to preview template mapping');
    return response.json();
  }

  async getTemplateTabMappingGrid(sessionId: number): Promise<OperationCallResult<PlmTemplateMappingGridDto>> {
    const response = await fetch(`${this.baseUrl}/GetTemplateTabMappingGrid?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to analyze template mapping grid');
    return response.json();
  }

  async saveTemplateMapping(
    sessionId: number,
    setting: PlmTemplateImportSettingDto,
  ): Promise<OperationCallResult<PlmTemplateImportSettingDto>> {
    const response = await fetch(`${this.baseUrl}/SaveTemplateMapping?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(setting),
    });
    if (!response.ok) throw new Error('Failed to save template mapping');
    return response.json();
  }

  async validateTemplateMapping(
    sessionId: number,
    setting: PlmTemplateImportSettingDto,
  ): Promise<OperationCallResult<PlmTemplateMappingValidationDto>> {
    const response = await fetch(`${this.baseUrl}/ValidateTemplateMapping?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(setting),
    });
    if (!response.ok) throw new Error('Failed to validate template mapping');
    return response.json();
  }

  async executeTemplateImport(sessionId: number): Promise<OperationCallResult<PlmImportJobDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteTemplateImport?sessionId=${sessionId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to start template import');
    return response.json();
  }

  async loadDwImportBlueprint(blueprintJson: string, tablePrefix?: string | null): Promise<OperationCallResult<PlmDwImportBlueprintDto>> {
    const response = await fetch(`${this.baseUrl}/LoadDwImportBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ BlueprintJson: blueprintJson, TablePrefix: tablePrefix || '' }),
    });
    if (!response.ok) throw new Error('Failed to load DW import blueprint');
    return response.json();
  }

  async loadDwImportBlueprintFromTable(
    tablePrefix?: string | null,
    blueprintKey = 'default',
  ): Promise<OperationCallResult<PlmDwImportBlueprintDto>> {
    const params = new URLSearchParams();
    if (tablePrefix) params.set('tablePrefix', tablePrefix);
    params.set('blueprintKey', blueprintKey);
    const response = await fetch(`${this.baseUrl}/LoadDwImportBlueprintFromTable?${params.toString()}`, {
      method: 'GET',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error('Failed to load DW import blueprint from tenant table');
    return response.json();
  }

  async validateDwImportBlueprint(blueprint: PlmDwImportBlueprintDto): Promise<OperationCallResult<PlmDwBlueprintValidationDto>> {
    const response = await fetch(`${this.baseUrl}/ValidateDwImportBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to validate DW import blueprint');
    return response.json();
  }

  async previewDwBlueprintConfig(blueprint: PlmDwImportBlueprintDto): Promise<OperationCallResult<PlmDwBlueprintPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewDwBlueprintConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to preview DW blueprint config');
    return response.json();
  }

  async executeDwBlueprintConfig(request: PlmDwBlueprintExecuteRequestDto): Promise<OperationCallResult<PlmDwBlueprintExecuteResultDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteDwBlueprintConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to execute DW blueprint config');
    return response.json();
  }

  async refreshDwImportTenantCaches(request: PlmDwRefreshCachesRequestDto): Promise<OperationCallResult<boolean>> {
    const response = await fetch(`${this.baseUrl}/RefreshDwImportTenantCaches`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to refresh DW import tenant caches');
    return response.json();
  }

  async loadSearchImportBlueprint(blueprintJson: string): Promise<OperationCallResult<PlmSearchImportBlueprintDto>> {
    const response = await fetch(`${this.baseUrl}/LoadSearchImportBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ BlueprintJson: blueprintJson }),
    });
    if (!response.ok) throw new Error('Failed to load search import blueprint');
    return response.json();
  }

  async validateSearchImportBlueprint(
    blueprint: PlmSearchImportBlueprintDto,
  ): Promise<OperationCallResult<PlmSearchImportValidationDto>> {
    const response = await fetch(`${this.baseUrl}/ValidateSearchImportBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to validate search import blueprint');
    return response.json();
  }

  async previewSearchBlueprintConfig(
    blueprint: PlmSearchImportBlueprintDto,
  ): Promise<OperationCallResult<PlmSearchImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewSearchBlueprintConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to preview search blueprint config');
    return response.json();
  }

  async executeSearchBlueprintConfig(
    request: PlmSearchImportExecuteRequestDto,
  ): Promise<OperationCallResult<PlmSearchImportExecuteResultDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteSearchBlueprintConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to execute search blueprint config');
    return response.json();
  }

  async loadSearchSiblingViewBlueprint(
    blueprintJson: string,
  ): Promise<OperationCallResult<PlmSearchSiblingViewBlueprintDto>> {
    const response = await fetch(`${this.baseUrl}/LoadSearchSiblingViewBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ BlueprintJson: blueprintJson }),
    });
    if (!response.ok) throw new Error('Failed to load search sibling view blueprint');
    return response.json();
  }

  async validateSearchSiblingViewBlueprint(
    blueprint: PlmSearchSiblingViewBlueprintDto,
  ): Promise<OperationCallResult<PlmSearchImportValidationDto>> {
    const response = await fetch(`${this.baseUrl}/ValidateSearchSiblingViewBlueprint`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to validate search sibling view blueprint');
    return response.json();
  }

  async previewSearchSiblingViewConfig(
    blueprint: PlmSearchSiblingViewBlueprintDto,
  ): Promise<OperationCallResult<PlmSearchImportPreviewDto>> {
    const response = await fetch(`${this.baseUrl}/PreviewSearchSiblingViewConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(blueprint),
    });
    if (!response.ok) throw new Error('Failed to preview search sibling view config');
    return response.json();
  }

  async executeSearchSiblingViewConfig(
    request: PlmSearchSiblingViewExecuteRequestDto,
  ): Promise<OperationCallResult<PlmSearchSiblingViewExecuteResultDto>> {
    const response = await fetch(`${this.baseUrl}/ExecuteSearchSiblingViewConfig`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to execute search sibling view config');
    return response.json();
  }
}

export interface PlmDwImportBlueprintDto {
  SchemaVersion?: number;
  GeneratedAt?: string | null;
  Source?: PlmDwBlueprintSourceDto | null;
  TransactionGroup?: PlmDwBlueprintTransactionGroupDto | null;
  RootUnit?: PlmDwBlueprintRootUnitDto | null;
  TabSharedTableGroups?: PlmDwBlueprintTabSharedTableGroupDto[] | null;
  Transactions?: PlmDwBlueprintTransactionDto[] | null;
  GridBindings?: PlmDwBlueprintGridBindingDto[] | null;
  BlueprintFields?: PlmDwBlueprintFieldDto[] | null;
  SearchView?: PlmDwBlueprintSearchViewDto | null;
  Navigation?: PlmDwBlueprintNavigationDto | null;
  PlmTemplate?: PlmDwBlueprintPlmTemplateDto | null;
}

export interface PlmDwBlueprintPlmTemplateDto {
  TemplateId?: number;
  TemplateName?: string | null;
  TemplateHeaderTabIds?: number[] | null;
}

export interface PlmDwBlueprintSourceDto {
  DwDatabase?: string | null;
  ImportTabIds?: number[] | null;
  TablePrefix?: string | null;
  ConfigFile?: string | null;
  PlmTemplateId?: number | null;
  PlmDatabase?: string | null;
}

export interface PlmDwBlueprintTransactionGroupDto {
  Name?: string | null;
  IntegrationId?: string | null;
  SaasApplicationId?: number | null;
}

export interface PlmDwBlueprintRootUnitDto {
  AppTableName?: string | null;
  IntegrationId?: string | null;
  ReferenceScope?: PlmDwBlueprintReferenceScopeDto | null;
}

export interface PlmDwBlueprintReferenceScopeDto {
  DwTable?: string | null;
  DwColumn?: string | null;
  PlmTabId?: number;
  PlmSubItemId?: number;
}

export interface PlmDwBlueprintTabSharedTableGroupDto {
  GroupId?: string | null;
  SharedAppTableName?: string | null;
  PrimaryPlmTabId?: number;
  SecondaryPlmTabIds?: number[] | null;
  Rule?: string | null;
}

export interface PlmDwBlueprintTransactionDto {
  PlmTabId?: number;
  PlmTabName?: string | null;
  IntegrationId?: string | null;
  TransactionName?: string | null;
  ImportStatus?: string | null;
  PlmTabSort?: number | null;
  IsTemplateHeaderTab?: boolean | null;
  UnitStructure?: PlmDwBlueprintUnitStructureDto | null;
}

export interface PlmDwBlueprintUnitStructureDto {
  Mode?: string | null;
  RootTableName?: string | null;
  SiblingUnits?: PlmDwBlueprintSiblingUnitDto[] | null;
  ChildUnits?: PlmDwBlueprintChildUnitDto[] | null;
}

export interface PlmDwBlueprintSiblingUnitDto {
  AppTableName?: string | null;
  IsMasterSibling?: boolean;
  FieldPolicy?: string | null;
  ExcludeSubItemsFromDwTable?: string | null;
}

export interface PlmDwBlueprintChildUnitDto {
  AppTableName?: string | null;
  AttachToRoot?: boolean;
}

export interface PlmDwBlueprintGridBindingDto {
  PlmGridId?: number;
  AppTableName?: string | null;
  ParentPlmTabId?: number | null;
  AttachToRoot?: boolean;
  IntegrationId?: string | null;
  TransactionIntegrationId?: string | null;
}

export interface PlmDwBlueprintFieldDto {
  AppTableName?: string | null;
  AppColumnName?: string | null;
  PlmTabIds?: number[] | null;
  AppControlType?: number | null;
  PlmControlType?: number | null;
  PlmEntityId?: number | null;
  DisplayLabel?: string | null;
  DisplayOrder?: number | null;
  IncludeInSearch?: boolean;
}

export interface PlmDwBlueprintSearchViewDto {
  Search?: PlmDwBlueprintSearchDto | null;
  SearchView?: PlmDwBlueprintSearchViewSpecDto | null;
}

export interface PlmDwBlueprintSearchDto {
  Name?: string | null;
  IntegrationId?: string | null;
  UsageType?: string | null;
  RootTableName?: string | null;
}

export interface PlmDwBlueprintSearchViewSpecDto {
  IntegrationId?: string | null;
  Fields?: string | null;
}

export interface PlmDwBlueprintNavigationDto {
  FolderName?: string | null;
  ParentFolderIntegrationId?: string | null;
  MenuOrder?: number;
}

export interface PlmDwBlueprintValidationDto {
  IsValid?: boolean;
  Errors?: string[] | null;
  Warnings?: string[] | null;
}

export interface PlmDwBlueprintPreviewDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  Items?: PlmDwBlueprintPreviewItemDto[] | null;
}

export interface PlmDwBlueprintPreviewItemDto {
  ObjectType?: string | null;
  Name?: string | null;
  IntegrationId?: string | null;
  Action?: string | null;
  ExistingId?: number | null;
}

export interface PlmDwBlueprintExecuteRequestDto {
  Blueprint?: PlmDwImportBlueprintDto | null;
  SaasApplicationId?: number | null;
  Mode?: string | null;
  IncludeSearchView?: boolean;
  IncludeNavigation?: boolean;
  IncludeTransactionGroup?: boolean;
}

export interface PlmDwBlueprintExecuteResultDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  TransactionsInserted?: number;
  TransactionsUpdated?: number;
  TransactionGroupId?: number | null;
  SearchId?: number | null;
  TransactionIds?: number[] | null;
}

export interface PlmDwRefreshCachesRequestDto {
  TransactionIds?: number[] | null;
  TableNames?: string[] | null;
}

export interface PlmColorRootFolderPreviewDto {
  PlmFolderId?: number;
  PlmFolderName?: string | null;
  AppFolderId?: number | null;
  AppFolderName?: string | null;
}

export interface PlmColorImportPreviewDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  PlmColorRootFolderCount?: number;
  RootFolderStrategy?: string | null;
  ResolvedAppRootFolderId?: number | null;
  ResolvedAppRootFolderName?: string | null;
  PlmColorRootFolders?: PlmColorRootFolderPreviewDto[] | null;
  HasRgbColorTable?: boolean;
  RgbColorRowCount?: number;
  ColorGroupDetailRowCount?: number;
  ExistingTransactionId?: number | null;
  ExistingListSearchId?: number | null;
  ExistingFolderTemplateSearchId?: number | null;
  Warnings?: string[] | null;
  PlannedActions?: string[] | null;
}

export interface PlmColorImportExecuteRequestDto {
  SessionId?: number | null;
  SaasApplicationId?: number | null;
}

export interface PlmColorImportExecuteResultDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  RootFolderStrategy?: string | null;
  AppRootFolderId?: number | null;
  TransactionId?: number | null;
  FormId?: number | null;
  ListSearchId?: number | null;
  FolderTemplateSearchId?: number | null;
  FolderSearchViewId?: number | null;
  ListSearchViewId?: number | null;
  Messages?: string[] | null;
}

export interface PlmPomRootFolderPreviewDto {
  PlmFolderId?: number;
  PlmFolderName?: string | null;
  AppFolderId?: number | null;
  AppFolderName?: string | null;
}

export interface PlmPomImportPreviewDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  HasBodyPartTable?: boolean;
  BodyPartRowCount?: number;
  HasBodyTypeTable?: boolean;
  BodyTypeRowCount?: number;
  HasBodyTypeDetailTable?: boolean;
  BodyTypeDetailRowCount?: number;
  BodyTypeDetailSourceRowCount?: number;
  HasSpecBodyPartGradingTable?: boolean;
  SpecBodyPartGradingRowCount?: number;
  SpecBodyPartGradingSourceRowCount?: number;
  PlmPomRootFolderCount?: number;
  PomAppRootFolderId?: number | null;
  PomAppRootFolderName?: string | null;
  PlmPomRootFolders?: PlmPomRootFolderPreviewDto[] | null;
  PlmPomTemplateRootFolderCount?: number;
  PomTemplateAppRootFolderId?: number | null;
  PomTemplateAppRootFolderName?: string | null;
  PlmPomTemplateRootFolders?: PlmPomRootFolderPreviewDto[] | null;
  ExistingPomTransactionId?: number | null;
  ExistingPomListSearchId?: number | null;
  ExistingPomFolderSearchId?: number | null;
  ExistingPomTemplateTransactionId?: number | null;
  ExistingPomTemplateListSearchId?: number | null;
  ExistingPomTemplateFolderSearchId?: number | null;
  PomFolderIdReadyToRemap?: number;
  PomFolderIdUnmappedCount?: number;
  PomTemplateFolderIdReadyToRemap?: number;
  PomTemplateFolderIdUnmappedCount?: number;
  Warnings?: string[] | null;
  PlannedActions?: string[] | null;
}

export interface PlmPomImportExecuteRequestDto {
  SessionId?: number | null;
  SaasApplicationId?: number | null;
  ImportJunctionTables?: boolean;
  ImportFoldersIfMissing?: boolean;
}

export interface PlmPomImportExecuteResultDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  PomTransactionId?: number | null;
  PomFormId?: number | null;
  PomListSearchId?: number | null;
  PomFolderSearchId?: number | null;
  PomAppRootFolderId?: number | null;
  PomTemplateTransactionId?: number | null;
  PomTemplateFormId?: number | null;
  PomTemplateListSearchId?: number | null;
  PomTemplateFolderSearchId?: number | null;
  PomTemplateAppRootFolderId?: number | null;
  BodyTypeDetailRowsImported?: number;
  SpecBodyPartGradingRowsImported?: number;
  FoldersImported?: number;
  PomFolderIdsRemapped?: number;
  PomTemplateFolderIdsRemapped?: number;
  Messages?: string[] | null;
}

export interface PlmSearchImportBlueprintDto {
  SchemaVersion?: number;
  GeneratedAt?: string | null;
  Mode?: string | null;
  Source?: PlmSearchImportSourceDto | null;
  Search?: PlmSearchImportSearchDto | null;
  DataSet?: PlmSearchImportDataSetDto | null;
  JoinPlan?: PlmSearchImportJoinPlanDto | null;
  TransactionGroup?: PlmSearchImportTransactionGroupDto | null;
  CriteriaFields?: PlmSearchImportCriteriaFieldDto[] | null;
  SearchView?: PlmSearchImportSearchViewDto | null;
  LinkTargets?: PlmSearchImportLinkTargetDto[] | null;
  Menu?: PlmSearchImportMenuDto | null;
  Coverage?: PlmSearchImportCoverageDto | null;
  UnmappedPlmFields?: PlmSearchImportUnmappedFieldDto[] | null;
}

export interface PlmSearchImportSourceDto {
  PlmSearchTemplateId?: number | null;
  PlmSearchName?: string | null;
  PrimaryTableName?: string | null;
  SelectedJoinPlanId?: string | null;
}

export interface PlmSearchImportSearchDto {
  Name?: string | null;
  Description?: string | null;
  IntegrationId?: string | null;
  UsageType?: string | null;
  AutoExecute?: boolean;
  SaasApplicationId?: number | null;
}

export interface PlmSearchImportDataSetDto {
  Name?: string | null;
  PrimaryTableName?: string | null;
  QueryText?: string | null;
}

export interface PlmSearchImportJoinPlanDto {
  PlanId?: string | null;
  Label?: string | null;
}

export interface PlmSearchImportTransactionGroupDto {
  TransactionGroupId?: number | null;
  GroupName?: string | null;
  PrimaryTransactionIntegrationId?: string | null;
}

export interface PlmSearchImportCriteriaFieldDto {
  DisplayText?: string | null;
  SysTableFiledPath?: string | null;
}

export interface PlmSearchImportSearchViewDto {
  Name?: string | null;
  IntegrationId?: string | null;
  Fields?: PlmSearchImportSearchViewFieldDto[] | null;
}

export interface PlmSearchImportSearchViewFieldDto {
  DisplayText?: string | null;
  SysTableFiledPath?: string | null;
}

export interface PlmSearchImportLinkTargetDto {
  Name?: string | null;
  ActionType?: string | null;
  TransactionIntegrationId?: string | null;
}

export interface PlmSearchImportMenuDto {
  RegisterInMainMenu?: boolean;
  MenuTitle?: string | null;
}

export interface PlmSearchImportUnmappedFieldDto {
  DisplayLabel?: string | null;
  Reason?: string | null;
}

export interface PlmSearchImportCoverageDto {
  Criteria?: { Total?: number; Mapped?: number; Ignored?: number };
  View?: { Total?: number; Mapped?: number; Ignored?: number };
}

export interface PlmSearchImportValidationDto {
  IsValid?: boolean;
  Errors?: string[] | null;
  Warnings?: string[] | null;
}

export interface PlmSearchImportPreviewItemDto {
  ObjectType?: string | null;
  Name?: string | null;
  IntegrationId?: string | null;
  Action?: string | null;
  ExistingId?: number | null;
  Detail?: string | null;
}

export interface PlmSearchImportPreviewDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  Items?: PlmSearchImportPreviewItemDto[] | null;
}

export interface PlmSearchImportExecuteRequestDto {
  Blueprint?: PlmSearchImportBlueprintDto | null;
  SaasApplicationId?: number | null;
}

export interface PlmSearchImportExecuteResultDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  SearchId?: number | null;
  SearchViewId?: number | null;
  DataSetId?: number | null;
  Messages?: string[] | null;
}

export interface PlmSearchSiblingViewBlueprintDto {
  SchemaVersion?: number;
  Mode?: string | null;
  GeneratedAt?: string | null;
  Source?: {
    PlmSearchTemplateId?: number | null;
    PlmSearchName?: string | null;
    PlmReferenceViewId?: number | null;
    PlmReferenceViewName?: string | null;
    TablePrefix?: string | null;
    RowGrain?: string | null;
  } | null;
  Target?: {
    AppSearchIntegrationId?: string | null;
    AppSearchId?: number | null;
    AppDataSetId?: number | null;
  } | null;
  DataSetPatch?: {
    ResultingQueryText?: string | null;
    AddColumns?: Array<{
      SysTableFiledPath?: string | null;
      AppTableName?: string | null;
      Alias?: string | null;
    }> | null;
    AddLeftJoins?: Array<{
      Alias?: string | null;
      AppTableName?: string | null;
      JoinType?: string | null;
      Cardinality?: string | null;
      LeftTable?: string | null;
      LeftColumn?: string | null;
      RightColumn?: string | null;
    }> | null;
  } | null;
  SearchView?: PlmSearchImportSearchViewDto | null;
  LinkTargets?: {
    CopyFromDefaultSearchView?: boolean;
    Items?: PlmSearchImportLinkTargetDto[] | null;
  } | null;
  Coverage?: {
    Covered?: number;
    AddColumn?: number;
    AddOneToOneLeftJoin?: number;
    RequiresOneToN?: number;
    Unmapped?: number;
  } | null;
}

export interface PlmSearchSiblingViewExecuteRequestDto {
  Blueprint?: PlmSearchSiblingViewBlueprintDto | null;
  SaasApplicationId?: number | null;
}

export interface PlmSearchSiblingViewExecuteResultDto {
  IsSuccess?: boolean;
  ErrorMessage?: string | null;
  SearchId?: number | null;
  DataSetId?: number | null;
  SiblingSearchViewId?: number | null;
  DefaultSearchViewId?: number | null;
  Messages?: string[] | null;
}

export const plmMigrationSvc = new PlmMigrationService();
