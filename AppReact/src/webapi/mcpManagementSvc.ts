import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export type McpExposableApi = {
  AppSource: string;
  OperationId: string;
  HttpMethod?: string;
  ApiPath?: string;
  Summary?: string;
  Tag?: string;
  InSpec: boolean;
  ForwardsCallerToken: boolean;
  ExposedApiId: number;
  IsEnabled: boolean;
  GroupIds: number[];
};

export type McpSecurityGroup = { GroupId: number; GroupName: string };

export type McpSaveExposedApiRequest = {
  AppSource: string;
  OperationId: string;
  IsEnabled: boolean;
  GroupIds: number[];
};

const base = `${endpoints.BASE_URL}/webapi/McpManagement`;

async function failure(response: Response, what: string): Promise<Error> {
  if (response.status === 403) return new Error('Only a company administrator can manage MCP API access.');
  return new Error(`Failed to ${what} (${response.status})`);
}

// Controls which APIs external MCP users (Claude Desktop, ChatGPT Desktop, ...) may use, per security group.
class McpManagementService {
  async getExposableApis(): Promise<McpExposableApi[]> {
    const response = await fetch(`${base}/GetExposableApis`, { headers: getHeaders() });
    if (!response.ok) throw await failure(response, 'load MCP APIs');
    return response.json();
  }

  async getSecurityGroups(): Promise<McpSecurityGroup[]> {
    const response = await fetch(`${base}/GetSecurityGroups`, { headers: getHeaders() });
    if (!response.ok) throw await failure(response, 'load security groups');
    return response.json();
  }

  async saveExposedApi(data: McpSaveExposedApiRequest): Promise<{ ExposedApiId: number }> {
    const response = await fetch(`${base}/SaveExposedApi`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data),
    });
    if (!response.ok) throw await failure(response, 'save MCP API access');
    return response.json();
  }

  async deleteExposedApi(exposedApiId: number): Promise<void> {
    const response = await fetch(`${base}/DeleteExposedApi?exposedApiId=${exposedApiId}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw await failure(response, 'remove MCP API access');
  }

  async refreshApiCatalog(): Promise<void> {
    const response = await fetch(`${base}/RefreshApiCatalog`, { method: 'POST', headers: getHeaders() });
    if (!response.ok) throw await failure(response, 'refresh the API catalog');
  }
}

export const mcpManagementSvc = new McpManagementService();
