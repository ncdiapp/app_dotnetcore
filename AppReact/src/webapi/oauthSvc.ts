import { endpoints } from './endpoints';
import { getHeaders } from '../helper/apiServiceHelper';

export type OAuthConsentInfo = {
  ClientName: string;
  RedirectHost: string;
  Scopes: string[];
  Resource: string;
  UserName: string;
  CompanyId: number;
};

export type OAuthConnectionRow = {
  AuthorizationId: string;
  ClientName: string;
  UserId?: number;
  UserName?: string;
  CreatedUtc?: string | null;
};

export type OAuthClientRow = {
  ClientId: string;
  Name: string;
  RedirectUris: string[];
  IsDynamic: boolean;
};

const consentBase = `${endpoints.BASE_URL}/webapi/OAuthConsent`;
const adminBase = `${endpoints.BASE_URL}/webapi/OAuthAdmin`;

// The server answers a problem with { error: "message" } (400) or 403; show that text, never a stack.
async function failure(response: Response, what: string): Promise<Error> {
  let detail = '';
  try {
    detail = (await response.json())?.error ?? '';
  } catch {
    detail = '';
  }
  if (detail) return new Error(detail);
  if (response.status === 403) return new Error('You do not have permission to do this.');
  if (response.status === 404) return new Error('Connecting AI applications is not enabled on this server.');
  return new Error(`Failed to ${what} (${response.status})`);
}

async function post<T>(url: string, what: string, body?: unknown): Promise<T> {
  const response = await fetch(url, { method: 'POST', headers: getHeaders(), body: body === undefined ? undefined : JSON.stringify(body) });
  if (!response.ok) throw await failure(response, what);
  return response.json();
}

async function get<T>(url: string, what: string): Promise<T> {
  const response = await fetch(url, { headers: getHeaders() });
  if (!response.ok) throw await failure(response, what);
  return response.json();
}

// Sign-in/consent for AI applications (Claude, ChatGPT, ...), a user's own connections, and their administration.
class OAuthService {
  /** What the consent page shows: which application asks, where it returns, what it gets. */
  describe(authorizeUrl: string): Promise<OAuthConsentInfo> {
    return post(`${consentBase}/Describe`, 'check the request', { AuthorizeUrl: authorizeUrl });
  }

  /** The user approves. Returns the URL the browser must open next (it carries the consent ticket). */
  async approve(authorizeUrl: string): Promise<string> {
    return (await post<{ redirectUrl: string }>(`${consentBase}/Approve`, 'approve the request', { AuthorizeUrl: authorizeUrl })).redirectUrl;
  }

  /** The user declines. Returns the URL that sends the application back with access_denied. */
  async deny(authorizeUrl: string): Promise<string> {
    return (await post<{ redirectUrl: string }>(`${consentBase}/Deny`, 'decline the request', { AuthorizeUrl: authorizeUrl })).redirectUrl;
  }

  myConnections(): Promise<OAuthConnectionRow[]> {
    return get(`${consentBase}/MyConnections`, 'load your connections');
  }

  async revokeMyConnection(authorizationId: string): Promise<boolean> {
    return (await post<{ revoked: boolean }>(`${consentBase}/RevokeMyConnection?id=${encodeURIComponent(authorizationId)}`, 'disconnect')).revoked;
  }

  companyConnections(): Promise<OAuthConnectionRow[]> {
    return get(`${adminBase}/Connections`, 'load connections');
  }

  async revokeCompanyConnection(authorizationId: string): Promise<boolean> {
    return (await post<{ revoked: boolean }>(`${adminBase}/RevokeConnection?id=${encodeURIComponent(authorizationId)}`, 'disconnect')).revoked;
  }

  clients(): Promise<OAuthClientRow[]> {
    return get(`${adminBase}/Clients`, 'load OAuth clients');
  }

  async createClient(name: string, redirectUris: string[]): Promise<string> {
    return (await post<{ clientId: string }>(`${adminBase}/CreateClient`, 'register the client', { Name: name, RedirectUris: redirectUris })).clientId;
  }

  async deleteClient(clientId: string): Promise<boolean> {
    return (await post<{ deleted: boolean }>(`${adminBase}/DeleteClient?clientId=${encodeURIComponent(clientId)}`, 'delete the client')).deleted;
  }
}

export const oauthSvc = new OAuthService();
