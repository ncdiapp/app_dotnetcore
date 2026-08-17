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

export interface AppConfigPackPreviewItemDto {
  ObjectType?: string | null;
  Name?: string | null;
  IntegrationId?: string | null;
  Action?: string | null;
  ExistingId?: number | null;
  Detail?: string | null;
}

export interface AppConfigPackValidationDto {
  IsValid: boolean;
  Errors?: string[];
  Warnings?: string[];
}

export interface AppConfigPackPreviewDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  Items?: AppConfigPackPreviewItemDto[];
}

export interface AppConfigPackExecuteResultDto {
  IsSuccess: boolean;
  ErrorMessage?: string | null;
  Messages?: string[];
  TablesCreated?: number;
  ColumnsAdded?: number;
  ViewsApplied?: number;
  TransactionsInserted?: number;
  TransactionsUpdated?: number;
  SearchesInserted?: number;
  SearchesUpdated?: number;
  TransactionGroupId?: number | null;
}

export interface AppConfigPackExportResultDto {
  Pack?: any;
  JsonText?: string | null;
}

const baseUrl = `${endpoints.BASE_URL}/webapi/AppConfigPack`;

class AppConfigPackService {
  async Load(packJson: string): Promise<OperationCallResult<any>> {
    const response = await fetch(`${baseUrl}/Load`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ PackJson: packJson || '' })
    });
    if (!response.ok) throw new Error('Failed to load App Config Pack JSON.');
    return response.json();
  }

  async Validate(pack: any): Promise<OperationCallResult<AppConfigPackValidationDto>> {
    const response = await fetch(`${baseUrl}/Validate`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(pack)
    });
    if (!response.ok) throw new Error('Failed to validate App Config Pack.');
    return response.json();
  }

  async Preview(pack: any, saasApplicationId: number | null): Promise<OperationCallResult<AppConfigPackPreviewDto>> {
    const response = await fetch(`${baseUrl}/Preview`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        Pack: pack,
        SaasApplicationId: saasApplicationId
      })
    });
    if (!response.ok) throw new Error('Failed to preview App Config Pack.');
    return response.json();
  }

  async Execute(pack: any, saasApplicationId: number | null): Promise<OperationCallResult<AppConfigPackExecuteResultDto>> {
    const response = await fetch(`${baseUrl}/Execute`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        Pack: pack,
        SaasApplicationId: saasApplicationId
      })
    });
    if (!response.ok) throw new Error('Failed to execute App Config Pack.');
    return response.json();
  }

  async Export(
    saasApplicationId: number,
    transactionIds: number[],
    searchIds: number[],
    exportAll: boolean
  ): Promise<OperationCallResult<AppConfigPackExportResultDto>> {
    // Subset export must never send an empty id list: older backends treat [] as "export all of that kind".
    const none = [-1];
    const txIds = transactionIds || [];
    const srIds = searchIds || [];
    const response = await fetch(`${baseUrl}/Export`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({
        SaasApplicationId: saasApplicationId,
        ExportAll: !!exportAll,
        TransactionIds: exportAll ? [] : (txIds.length > 0 ? txIds : none),
        SearchIds: exportAll ? [] : (srIds.length > 0 ? srIds : none)
      })
    });
    if (!response.ok) throw new Error('Failed to export App Config Pack.');
    return response.json();
  }
}

export const appConfigPackSvc = new AppConfigPackService();
