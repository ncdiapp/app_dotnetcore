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
}

export const plmMigrationSvc = new PlmMigrationService();
