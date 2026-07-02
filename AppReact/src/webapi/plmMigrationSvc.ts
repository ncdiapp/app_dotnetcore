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

export const plmMigrationSvc = new PlmMigrationService();
