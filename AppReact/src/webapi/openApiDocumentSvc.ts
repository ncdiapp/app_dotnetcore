import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export type OpenApiDocumentMember = {
  ActionCode?: string;
  HttpMethod?: string;
  Source?: string;
  Description?: string;
};

export type OpenApiDocument = {
  Id: number;
  Name?: string;
  Code?: string;
  Version?: string;
  Description?: string;
  Status?: string;
  ApiCount?: number;
  LastGenerated?: string | null;
  AppCreatedDate?: string | null;
  AppModifiedDate?: string | null;
  AppCreatedByID?: number | null;
  AppModifiedByID?: number | null;
  OpenApiJson?: string;
  Members?: OpenApiDocumentMember[];
};

export type OpenApiSelectableApi = {
  Id: number;
  ActionCode?: string;
  Description?: string;
  ApiType?: string;
  HttpMethod?: string;
  DataSourceId?: number | null;
  DataSourceName?: string;
  TranscationId?: number | null;
  DataModelName?: string;
  Application?: string;
  ProviderName?: string;
  ProviderKind?: string;
};

async function readError(response: Response): Promise<string> {
  const text = await response.text();
  if (!text) return response.statusText || 'Request failed';
  try {
    const body = JSON.parse(text);
    return body?.message || body?.Message || text;
  } catch {
    return text;
  }
}

class OpenApiDocumentService {
  async list(): Promise<OpenApiDocument[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/List`, { headers: getHeaders() });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async get(id: number): Promise<OpenApiDocument> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/Get?id=${id}`, { headers: getHeaders() });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async listSelectableApis(): Promise<OpenApiSelectableApi[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/ListSelectableApis`, { headers: getHeaders() });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async save(body: {
    Id: number;
    Name: string;
    Code: string;
    Version: string;
    Description: string;
    ActionCodes: string[];
  }): Promise<OpenApiDocument> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/Save`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(body),
    });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async remove(id: number): Promise<void> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/Delete?id=${id}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error(await readError(response));
  }

  async setPublished(id: number, published: boolean): Promise<OpenApiDocument> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/SetPublished?id=${id}&published=${published}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async regenerate(id: number): Promise<{ Document: OpenApiDocument; Skipped: { ActionCode?: string; Reason?: string }[] }> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/Regenerate?id=${id}`, {
      method: 'POST',
      headers: getHeaders(),
    });
    if (!response.ok) throw new Error(await readError(response));
    return response.json();
  }

  async download(id: number): Promise<Blob> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/OpenApiDocument/Download?id=${id}`, { headers: getHeaders() });
    if (!response.ok) throw new Error(await readError(response));
    return response.blob();
  }
}

export const openApiDocumentService = new OpenApiDocumentService();
