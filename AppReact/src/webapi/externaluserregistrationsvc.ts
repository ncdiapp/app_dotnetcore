import { endpoints } from './endpoints';
import { getHeaders, getHeadersWithAuth } from '../helper/apiServiceHelper';
class ExternalUserRegistrationService {
  private isByPassCookieCheck = true;
 

  async eSiteLogin(userName: string, password: string): Promise<any> {
    const authValue = 'Basic ' + btoa(`${userName}:${password}`);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/ESiteLogin`, {
      headers: getHeadersWithAuth(authValue)
    });
    if (!response.ok) throw new Error('ESite login failed');
    return response.json();
  }

  async linkExistMasterDBUserToESite(token: string, userName: string, password: string): Promise<any> {
    const authValue = 'Basic ' + btoa(`${userName}:${password}`);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/LinkExistMasterDBUserToESite?token=${token}`, {
      headers: getHeadersWithAuth(authValue)
    });
    if (!response.ok) throw new Error('Failed to link master DB user');
    return response.json();
  }

  async eSiteUserRegistration(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/ESiteUserRegistration`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('User registration failed');
    return response.json();
  }

  async eSiteUserQuickRegistration(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/ESiteUserQuickRegistration`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Quick registration failed');
    return response.json();
  }

  async eSitePartnerUserThirdPartAccountLoginPostProcess(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/ESitePartnerUserThirdPartAccountLoginPostProcess`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Third party login post-process failed');
    return response.json();
  }

  async getExternalUserContext(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/GetExternalUserContext`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get external user context');
    return response.json();
  }

  async retrieveNoneMgtUserTreeMenu(siteId: string, siteMenuCategory: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/RetrieveNoneMgtUserTreeMenu?siteId=${siteId}&siteMenuCategory=${siteMenuCategory}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve menu tree');
    return response.json();
  }

  async sendPublicMessage(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/SendPublicMessage`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to send public message');
    return response.json();
  }

  async sendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/SendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to send password retrieval email');
    return response.json();
  }

  async getFormStructure(transactionId: string, transGroupId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/GetFormStructure?transactionId=${transactionId}&transGroupId=${transGroupId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get form structure');
    return response.json();
  }

  async getNewFormData(transactionId: string, isConfigTestRun: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/GetNewFormData?transactionId=${transactionId}&isConfigTestRun=${isConfigTestRun}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get new form data');
    return response.json();
  }

  async getFormData(transactionId: string, rootPrimaryKeyValue: string, transGroupId: string, autoExecuteCommandId: string, selectDataRow: any): Promise<any> {
    const data = { transactionId, rootPrimaryKeyValue, transGroupId, autoExecuteCommandId, selectDataRow };
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/GetFormData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to get form data');
    return response.json();
  }

  async saveTransactionData(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/SaveTransactionData`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save transaction data');
    return response.json();
  }

  async retrieveOneSearch(searchId: string, isSavedSearch: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/RetrieveOneSearch?searchId=${searchId}&isSavedSearch=${isSavedSearch}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve search');
    return response.json();
  }

  async retrieveSearchResult(searchDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/retrieveSearchResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDto)
    });
    if (!response.ok) throw new Error('Failed to retrieve search result');
    return response.json();
  }

  async retrieveUserViewsBySearchDefinition(searchDefinition: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/RetrieveUserViewsBySearchDefinition`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(searchDefinition)
    });
    if (!response.ok) throw new Error('Failed to retrieve user views');
    return response.json();
  }

  async prepareZoomMeetingRuntimeParameters(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/PrepareZoomMeetingRuntimeParameters`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to prepare Zoom meeting parameters');
    return response.json();
  }

  async externalLinkServiceCall(parameterStr: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/ExternalLinkServiceCall?parameterStr=${parameterStr}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('External link service call failed');
    return response.json();
  }

  async encryptString(paramString: string, token: string): Promise<string> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/EncryptString?paramString=${paramString}&token=${token}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('String encryption failed');
    return response.text();
  }

  async decryptString(paramString: string, token: string): Promise<string> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/DecryptString?paramString=${paramString}&token=${token}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('String decryption failed');
    return response.text();
  }

  async registerNewSaasAccountWithUserCompanyDB(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/ExternalUserRegistration/RegisterNewSaasAccountWithUserCompanyDB`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to register new SaaS account');
    return response.json();
  }
}

export const externalUserRegistrationService = new ExternalUserRegistrationService(); 