import { endpoints } from './endpoints';
import { getHeaders, getHeadersWithAuth } from '../helper/apiServiceHelper';
class AdminService {  

  async mgtLogin(userName: string, password: string): Promise<any> {
    const authValue = 'Basic ' + btoa(`${userName}:${password}`);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/MgtLogin`, {
      method: 'POST',
      headers: getHeadersWithAuth(authValue)
    });
    if (!response.ok) throw new Error('Authentication failed');
    return response.json();
  }

  async selectCompany(sessionId: string, companyId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/SelectCompany`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify({ sessionId, companyId })
    });
    if (!response.ok) throw new Error('Company selection failed');
    return response.json();
  }

  async authentication(userName: string, password: string): Promise<any> {
    const authValue = 'Basic ' + btoa(`${userName}:${password}`);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/Login`, {
      method: 'POST',
      headers: getHeadersWithAuth(authValue)
    });
    if (!response.ok) throw new Error('Authentication failed');
    return response.json();
  }

  async retrievePassword(userName: string, emailAddress: string): Promise<any> {
    const retrievePasswordValue = btoa(`${userName}:${emailAddress}`);
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/RetrievePassword`, {
      headers: { 'RetrievePassword': retrievePasswordValue }
    });
    if (!response.ok) throw new Error('Password retrieval failed');
    return response.json();
  }

  async checkCurrentSessionExists(sessionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/CheckCurrenSessionIsExsit?sessionId=${sessionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Session check failed');
    return response.json();
  }

  /** Login page background image URL list for current company (no auth). Same as Angular MgtLogin. */
  async getLoginPageBackgroundImageUrlList(): Promise<string[]> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/GetLoginPageBackgroundImageUrlList`);
    if (!response.ok) return [];
    const list = await response.json();
    return Array.isArray(list) ? list : [];
  }

  async touchServer(sessionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/TouchServer?sessionId=${sessionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Server touch failed');
    return response.json();
  }

  async retrieveCurrentUserUnReadMessages(sessionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/AppMessage/RetrieveCurrentUserUnReadMessagesByUserId?sessionId=${sessionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve unread messages');
    return response.json();
  }

  async getUserContextBySessionId(sessionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/GetUserContextBySessionId?sessionId=${sessionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error(`Invalid Session: ${sessionId}`);
    return response.json();
  }

  async retrieveAllMenu(isWebPageMenu: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllMenu?isWebPageMenu=${isWebPageMenu}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve menu');
    return response.json();
  }

  async getMenu(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetMenu`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get menu');
    return response.json();
  }

  async retrieveUserTreeMenu(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveUserTreeMenu`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user tree menu');
    return response.json();
  }

  async saveAllAppListMenuEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppListMenuEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save menu entity');
    return response.json();
  }

  async retrieveOneAppListMenuExDto(menuId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppListMenuExDto?menuId=${menuId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve menu');
    return response.json();
  }

  async saveOneAppListMenuExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppListMenuExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save menu');
    return response.json();
  }

  async retrieveEmployeeDomainMenuTree(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveEmployeeDomainMenuTree`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve domain menu tree');
    return response.json();
  }

  async retrieveListMenuHairarchyDto(isWebPageMenu: boolean, rootMenuId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveListMenuHairarchyDto?isWebPageMenu=${isWebPageMenu}&rootMenuId=${rootMenuId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve menu hierarchy');
    return response.json();
  }

  async saveOneAppListMenuTreeNode(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppListMenuTreeNode`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save menu tree node');
    return response.json();
  }

  async saveAllTreeListMenuDto(listMenusObjDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllTreeListMenuDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(listMenusObjDto)
    });
    if (!response.ok) throw new Error('Failed to save tree menu');
    return response.json();
  }

  async addSearchToMainMenu(searchOrSavedSearchId: string, menuName: string, isSavedSearch: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/AddSearchToMainMenu?searchOrSavedSearchId=${searchOrSavedSearchId}&menuName=${menuName}&isSavedSearch=${isSavedSearch}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to add search to menu');
    return response.json();
  }

  async addListTransactionToMainMenu(transactionId: string, menuName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/AddListTransactionToMainMenu?transactionId=${transactionId}&menuName=${menuName}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to add transaction to menu');
    return response.json();
  }

  async deleteOneAppListMenuTreeNode(menuId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppListMenuTreeNode?menuId=${menuId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete menu node');
    return response.json();
  }

  async retrieveDomainOrUserMenu(id: string, anEmMenuRegisterType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveDomainOrUserMenu?Id=${id}&anEmMenuRegisterType=${anEmMenuRegisterType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve domain/user menu');
    return response.json();
  }

  async saveDomainOrUserMenu(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveDomainOrUserMenu`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save domain/user menu');
    return response.json();
  }

  async getUserData(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllPdmSecurityWebUserDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get user data');
    return response.json();
  }

  async getOneUser(userId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOnePdmSecurityWebUserExDto?userId=${userId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get user');
    return response.json();
  }

  async getOrganizationUserDtoList(companyId: any, isIncludeBusinessPartnerUsers: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetOrganizationUserDtoList?comapnyId=${companyId}&isIncludeBusinessParternerUsers=${isIncludeBusinessPartnerUsers}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get organization user list');
    return response.json();
  }

  async getCurrentTenantUserDtoList(isIncludeBusinessPartnerUsers: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetCurrentTenantUserDtoList?isIncludeBusinessParternerUsers=${isIncludeBusinessPartnerUsers}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get current tenant user list');
    return response.json();
  }

  async getOrganizationGroupList(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetOrganizationGroupList?comapnyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get organization group list');
    return response.json();
  }

  // User Editor related methods
  async RetrieveEmployeeUserExternalMappingAccountLookupItemList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveEmployeeUserExternalMappingAccountLookupItemList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve external employee account mapping');
    return response.json();
  }

  async getMassEntitiesLookupItem(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveMassAppEntitiesLookupItem?entityCodes=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get mass entities lookup items');
    return response.json();
  }

  async retrieveTimeZones(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveTimeZones`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve time zones');
    return response.json();
  }

  async getCultroInfos(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetCultroInfos`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get culture infos');
    return response.json();
  }

  async RetrieveMasterDBUserLoginInfo(userId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveMasterDBUserLoginInfo?userId=${userId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user login info');
    return response.json();
  }

  async UpdateSaasUserLoginInfo(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/UpdateSaasUserLoginInfo`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update user login info');
    return response.json();
  }

  async InviteSaasComapnyNewUser(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/InviteSaasComapnyNewUser`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to invite new user');
    return response.json();
  }

  // Additional methods from AngularJS adminSvc.js
  async getLookupItemListByEntityInfoId(entityInfoId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetLookupItemListByEntityInfoId?entityInfoId=${entityInfoId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get lookup item list');
    return response.json();
  }

  async addOneLookupItemList(entityInfoID: string, displayField1: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/AddOneLookupItemList?entityInfoID=${entityInfoID}&displayField1=${displayField1}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to add lookup item list');
    return response.json();
  }

  async retrieveAllAppEntityInfoDto(isGetUserDefinedOnly: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppEntityInfoDto?isGetUserDefinedOnly=${isGetUserDefinedOnly}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app entity info');
    return response.json();
  }

  async retrieveOneAppEntityInfoExDto(id: string, includeLookUpItems: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppEntityInfoExDto?id=${id}&includeLookUpItems=${includeLookUpItems}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app entity info');
    return response.json();
  }

  async deleteOneAppEntityInfo(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppEntityInfo?id=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app entity info');
    return response.json();
  }

  async saveOneAppEntityInfoDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppEntityInfoDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app entity info');
    return response.json();
  }

  async retrieveAllAppSecurityGroupDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppSecurityGroupDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security groups');
    return response.json();
  }

  async retrieveAppSecurityGroupDtoByGroupUsage(groupUsage: string, isFilterBuiltInRoles: boolean, userType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAppSecurityGroupDtoByGroupUsage?groupUsage=${groupUsage}&isFilterBuiltInRoles=${isFilterBuiltInRoles}&userType=${userType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security groups by usage');
    return response.json();
  }

  async retrieveTransactionAvailableOrganizationRoleAndUser(transactionId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveTransactionAvailableOrganizationRoleAndUser?transactionId=${transactionId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve transaction available organization role and user');
    return response.json();
  }

  async retrieveOneAppSecurityGroupExDto(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppSecurityGroupExDto?groupId=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security group');
    return response.json();
  }

  async deleteAppSecurityGroup(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteAppSecurityGroup?securityGroupId=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app security group');
    return response.json();
  }

  async saveAppSecurityGroupExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppSecurityGroupExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app security group');
    return response.json();
  }

  async retrieveAllAppSecurityUserDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppSecurityUserDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security users');
    return response.json();
  }

  async retrieveAppSecurityUserDtoByCompanyId(companyId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAppSecurityUserDtoByCompanyId?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve company users');
    return response.json();
  }

  async getSimpleUserListByCompanyId(companyId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetSimpleUserListByCompanyId?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve company users');
    return response.json();
  }

  async retrieveAllSimpleUserDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllSimpleUserDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve simple users');
    return response.json();
  }

  async retrieveCurrentUserAvailableEmailToUsers(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserAvailableEmailToUsers`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user available email to users');
    return response.json();
  }

  async retrieveAllSystemBuiltinUserDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllSystemBuiltinUserDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve system builtin users');
    return response.json();
  }

  async getPartnerTypeUserDtoList(partnerType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetPartnerTypeUserDtoList?partnerType=${partnerType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get partner type user list');
    return response.json();
  }

  async getPartnerTypeGroupList(partnerType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetPartnerTypeGroupList?partnerType=${partnerType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get partner type group list');
    return response.json();
  }

  async getCompanyUserDtoList(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetCompanyUserDtoList?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get company user list');
    return response.json();
  }

  async retrieveOneAppSecurityUserExDto(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppSecurityUserExDto?userId=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security user');
    return response.json();
  }

  async retrieveCurrentAppSecurityUserExDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentAppSecurityUserExDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current app security user');
    return response.json();
  }

  async retrieveUserExDtoByDomainBusinessUser(domainId: string, businessUserId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveUserExDtoByDomainBusinessUser?domainId=${domainId}&businessUserId=${businessUserId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user by domain business user');
    return response.json();
  }

  async deleteAppSecurityUser(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteAppSecurityUser?userId=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app security user');
    return response.json();
  }

  async saveAppSecurityUserExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppSecurityUserExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app security user');
    return response.json();
  }

  async updateMasterDBUserLoginInfo(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/UpdateMasterDBUserLoginInfo`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update master DB user login info');
    return response.json();
  }

  async updateMyProfile(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/UpdateMyProfile`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to update my profile');
    return response.json();
  }

  async retrieveAllAppSecurityRegDomainEntityDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppSecurityRegDomainEntityDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security reg domain entities');
    return response.json();
  }

  async retrieveOneAppSecurityRegDomainExDto(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppSecurityRegDomainExDto?Id=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app security reg domain');
    return response.json();
  }

  async saveAllAppSecurityRegDomainEntityDto(securityRegDomainSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppSecurityRegDomainEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityRegDomainSetDto)
    });
    if (!response.ok) throw new Error('Failed to save app security reg domain entities');
    return response.json();
  }

  async deleteOneAppSecurityRegDomainEntityDto(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppSecurityRegDomainEntityDto?Id=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app security reg domain entity');
    return response.json();
  }

  async retrieveAllRootCompanyDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllRootCompanyDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve root company list');
    return response.json();
  }

  async retrieveAllSaasCompanyDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllSaasCompanyDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve SaaS company list');
    return response.json();
  }

  async retrieveOneAppCompanyExDto(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppCompanyExDto?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app company');
    return response.json();
  }

  /** Tenant admin: company for current session (no companyId on client). */
  async retrieveCurrentUserCompanyExDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserCompanyExDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user company');
    return response.json();
  }

  async saveOneAppCompanyExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppCompanyExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app company');
    return response.json();
  }

  async deleteOneAppCompany(companyId: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppCompany?companyId=${companyId}`, {
      method: 'POST',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete company');
    return response.json();
  }

  /** partnerType: EmAppUserType (3=Customer, 4=Supplier, 5=ClientAgent, 9=SupplierAgent) */
  async retrieveCompanyPartnerDtoList(companyId: string, partnerType: number): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCompanyPartnerDtoList?companyId=${companyId}&partnerType=${partnerType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve company partner list');
    return response.json();
  }

  async retrieveOneAppBusinessPartnerExDto(partnerID: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppBusinessPartnerExDto?partnerID=${partnerID}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app business partner');
    return response.json();
  }

  async retrieveCurrentUserAppBusinessPartnerExDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserAppBusinessPartnerExDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user app business partner');
    return response.json();
  }

  async saveOneAppBusinessPartnerExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppBusinessPartnerExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app business partner');
    return response.json();
  }

  async retrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(parternerId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList?parternerId=${parternerId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current company business partner invite user list');
    return response.json();
  }

  async retrieveCompanyOrgLevelDtoList(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCompanyOrgLevelDtoList?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve company org level list');
    return response.json();
  }

  /** Query param serialization to match Angular: null/undefined kept in query string. */
  static qsVal(v: string | null | undefined, nullAs: 'null' | 'undefined' = 'null'): string {
    if (v === undefined) return 'undefined';
    if (v === null || v === '') return nullAs;
    return encodeURIComponent(String(v));
  }
  static qsBool(v: boolean | null | undefined): string {
    if (v === undefined) return 'undefined';
    if (v === null) return 'null';
    return v ? 'true' : 'false';
  }

  async retrieveOrganizationDetailGroupUserPrivilegeDtoByType(
    objectID: string,
    objType: string,
    organizationId: string | null | undefined,
    actionCode: string | null | undefined,
    isIgnoreCurrentUserFilterSetup: boolean | null | undefined,
    partnerType: string | null | undefined
  ): Promise<any> {
    const o = AdminService.qsVal(organizationId, 'null');
    const a = AdminService.qsVal(actionCode, 'null');
    const i = AdminService.qsBool(isIgnoreCurrentUserFilterSetup);
    const p = AdminService.qsVal(partnerType, 'undefined');
    const url = `${endpoints.BASE_URL}/webapi/Administration/RetrieveOrganizationDetailGroupUserPrivilegeDtoByType?objectID=${objectID}&objType=${objType}&organizationId=${o}&actionCode=${a}&isIgnoreCurrentUserFilterSetup=${i}&partnerType=${p}`;
    const response = await fetch(url, { headers: getHeaders() });
    if (!response.ok) throw new Error('Failed to retrieve organization detail group user privilege');
    return response.json();
  }

  async saveOrganizationDetailLevelUserRowbyType(securityObjSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SavOrganizationDetailLevelUserRowbyType`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityObjSetDto)
    });
    if (!response.ok) throw new Error('Failed to save organization detail level user row');
    return response.json();
  }

  async retrieveOrganizationPrivilegeDtoByType(organizationId: string, objType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOrganizationPrivilegeDtoByType?organizationId=${organizationId}&objType=${objType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve organization privilege');
    return response.json();
  }

  async saveNewOrganizationPrivilegeByType(securityObjSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveNewOrganizationPrivilegeByType`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityObjSetDto)
    });
    if (!response.ok) throw new Error('Failed to save new organization privilege');
    return response.json();
  }

  async deleteOrganizationPrivilegeByType(securityObjSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOrganizationPrivilegeByType`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityObjSetDto)
    });
    if (!response.ok) throw new Error('Failed to delete organization privilege');
    return response.json();
  }

  async retrieveUserTypePrivilegeDtoByType(userType: string, objType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveUserTypePrivilegeDtoByType?userType=${userType}&objType=${objType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user type privilege');
    return response.json();
  }

  async saveNewUserTypePrivilegeByType(securityObjSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveNewUserTypePrivilegeByType`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityObjSetDto)
    });
    if (!response.ok) throw new Error('Failed to save new user type privilege');
    return response.json();
  }

  async deleteUserTypePrivilegeByType(securityObjSetDto: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteUserTypePrivilegeByType`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(securityObjSetDto)
    });
    if (!response.ok) throw new Error('Failed to delete user type privilege');
    return response.json();
  }

  async getOneAppTransactionAvailablePrivileges(transactionId: string, organizationId: string, userType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetOneAppTransactionAvailablePrivileges?transactionId=${transactionId}&organizationId=${organizationId}&userType=${userType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get app transaction available privileges');
    return response.json();
  }

  async retrieveAllAppSetupDtoList(isMasterDb: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppSetupDtoList?isMasterDb=${isMasterDb}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app setup list');
    return response.json();
  }

  async saveAllAppSetupEntityDto(aSet: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppSetupEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aSet)
    });
    if (!response.ok) {
      const detail = await response.text().catch(() => '');
      throw new Error(detail || `Failed to save app setup entities (${response.status})`);
    }
    return response.json();
  }

  async retrieveAllAppRouteStateEntityDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppRouteStateEntityDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app route state entities');
    return response.json();
  }

  async checkServerSetting(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/CheckServerSetting`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to check server setting');
    return response.json();
  }

  async enableOrDisableCache(isEnableCache: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/EnableOrDisableCache?isEnableCache=${isEnableCache}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to enable or disable cache');
    return response.json();
  }

  async retrieveAllWebPageTemplateFileNameList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllWebPageTemplateFileNameList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve web page template file name list');
    return response.json();
  }

  async getApplicationIconList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetApplicationIconList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get application icon list');
    return response.json();
  }

  async retrieveAllAppCalendarDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppCalendarDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app calendar list');
    return response.json();
  }

  async retrieveAllCompanyCalendarDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllCompanyCalendarDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve company calendar list');
    return response.json();
  }

  async retrieveOneAppCalendarExDto(calendarId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppCalendarExDto?calendarId=${calendarId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app calendar');
    return response.json();
  }

  async getUserCalendarId(userId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetUserCalendarId?userId=${userId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get user calendar ID');
    return response.json();
  }

  async deleteAppCalendar(calendarId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteAppCalendar?calendarId=${calendarId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app calendar');
    return response.json();
  }

  async saveAppCalendar(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppCalendar`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app calendar');
    return response.json();
  }

  async retrieveCalenarView(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCalenarView`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve calendar view');
    return response.json();
  }

  async logout(data: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Home/Logout?sessionId=${data}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to logout');
    return response.json();
  }

  async getCompanyOrgHairarchy(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetCompanyOrgHairarchy?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get company org hierarchy');
    return response.json();
  }

  async getCompanyOrganizationDtoFlatList(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetCompanyOrganizationDtoFlatList?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get company organization flat list');
    return response.json();
  }

  async saveAppComOrganizationSet(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppComOrganizationSet`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app com organization set');
    return response.json();
  }

  async transferOrganizationUsers(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/TransferOrganizationUsers`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to transfer organization users');
    return response.json();
  }

  async saveAppComOrganization(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppComOrganization`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app com organization');
    return response.json();
  }

  async deleteAppComOrganization(organizationId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteAppComOrganization?organizationId=${organizationId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app com organization');
    return response.json();
  }

  async retrieveAllAppDataSourceRegisterExDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppDataSourceRegisterExDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app data source register');
    return response.json();
  }

  async saveAllAppDataSourceRegisterExDto(aSet: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppDataSourceRegisterExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(aSet)
    });
    if (!response.ok) throw new Error('Failed to save app data source register');
    return response.json();
  }

  async getDataSourceRegisterList(withAppMasterdb: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GetDataSourceRegisterList?withAppMasterdb=${withAppMasterdb}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to get data source register list');
    return response.json();
  }

  async retrieveCurrentUserCompany(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserCompany`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user company');
    return response.json();
  }

  async retrieveAllAppReportEntityDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllAppReportEntityDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app report entities');
    return response.json();
  }

  async retrieveCurrnetUserReportDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrnetUserReportDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user report');
    return response.json();
  }

  async retrieveOneAppReportExDto(reportId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppReportExDto?reportId=${reportId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app report');
    return response.json();
  }

  async saveAllAppReportEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppReportEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app report entities');
    return response.json();
  }

  async saveOneAppReportEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppReportEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app report entity');
    return response.json();
  }

  async deleteOneAppReportEntityDto(reportId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppReportEntityDto?reportId=${reportId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete app report entity');
    return response.json();
  }

  async retrieveOneUserAllContactEntityDto(userId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneUserAllContactEntityDto?userId=${userId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user contact entities');
    return response.json();
  }

  async saveAllAppSecurityUserContactEntityDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAllAppSecurityUserContactEntityDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app security user contact entities');
    return response.json();
  }

  async retrieveAvailableApplicationPackages(excludeChildMenu: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAvailableApplicationPackages?excludeChildMenu=${excludeChildMenu}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve available application packages');
    return response.json();
  }

  async retrieveSelectedApplicationPackages(excludeChildMenu: boolean): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveSelectedApplicationPackages?excludeChildMenu=${excludeChildMenu}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve selected application packages');
    return response.json();
  }

  async importApplicationFromHostDBToCurrentUserDB(packageMenuId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/ImportApplicationFromHostDBToCurrentUserDB?packageMenuId=${packageMenuId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to import application from host DB');
    return response.json();
  }

  async deleteOneApplicationPackage(packageMenuId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneApplicationPackage?packageMenuId=${packageMenuId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete application package');
    return response.json();
  }

  async createMyNewApplicationPackage(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/CreateMyNewApplicationPackage`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to create new application package');
    return response.json();
  }

  async retrieveAppApplicationAssetsItemDtoListByType(applicationId: string, emAppApplicationAssetsType: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAppApplicationAssetsItemDtoListByType?applicationId=${applicationId}&emAppApplicationAssetsType=${emAppApplicationAssetsType}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app application assets item list');
    return response.json();
  }

  async saveAppApplicationAssetsItemDtoList(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveAppApplicationAssetsItemDtoList`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save app application assets item list');
    return response.json();
  }

  async retrieveAppApplicationDataManipulations(applicationId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAppApplicationDataManipulations?applicationId=${applicationId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve app application data manipulations');
    return response.json();
  }

  async saveOneSaasApplicationSetting(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneSaasApplicationSetting`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save SaaS application setting');
    return response.json();
  }

  async inviteBusinessPaternerUser(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/InviteBusinessPaternerUser`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to invite business partner user');
    return response.json();
  }

  async sendUserAccountUnlockEmail(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SendUserAccountUnlockEmail`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to send user account unlock email');
    return response.json();
  }

  async retrieveCurrentUserAvailableCompaniesFromMasterDB(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserAvailableCompaniesFromMasterDB`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user available companies from master DB');
    return response.json();
  }

  async retrieveCurrentUserCalendarSearch(searchId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveCurrentUserCalendarSearch?searchId=${searchId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve current user calendar search');
    return response.json();
  }

  async retrieveUserCalendarSearchResult(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveUserCalendarSearchResult`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to retrieve user calendar search result');
    return response.json();
  }

  async generatePayPalSellerOnboardingUrlByTrackingId(trackingId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/GeneratePayPalSellerOnboardingUrlByTrackingId?trackingId=${trackingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to generate PayPal seller onboarding URL');
    return response.json();
  }

  async trackSellerOnboardingStatusByTrackingId(trackingId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/TrackSellerOnboardingStatusByTrackingId?trackingId=${trackingId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to track seller onboarding status');
    return response.json();
  }

  async deleteCompanyLogoImage(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteCompanyLogoImage`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete company logo image');
    return response.json();
  }

  async deleteOneCompanyBackgroundImage(fileName: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneCompanyBackgroundImage?fileName=${fileName}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete company background image');
    return response.json();
  }

  async uninstallSaasAccount(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/UninstallSaasAccount?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to uninstall SaaS account');
    return response.json();
  }

  async convertOneSaasAccountToStandaloneApplication(companyId: string): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/ConvertOneSaasAccountToStandaloneApplication?companyId=${companyId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to convert SaaS account to standalone application');
    return response.json();
  }

  async addOnePageToUserBookmark(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/AddOnePageToUserBookmark`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to add page to user bookmark');
    return response.json();
  }

  async retrieveAllIntegrationTokenDto(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAllIntegrationTokenDto`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve integration token list');
    return response.json();
  }

  async saveOneIntegrationTokenExDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneIntegrationTokenExDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save integration token');
    return response.json();
  }

  
  // Theme management (user-defined themes)
  async retrieveAvailableUserDefineThemeDtoList(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveAvailableUserDefineThemeDtoList`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve available user-defined themes');
    return response.json();
  }

  async retrieveOneAppUserDefineThemeDto(themeId: number | null): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/RetrieveOneAppUserDefineThemeDto?themeId=${themeId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to retrieve user-defined theme');
    return response.json();
  }

  async saveOneAppUserDefineThemeDto(data: any): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/SaveOneAppUserDefineThemeDto`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Failed to save user-defined theme');
    return response.json();
  }

  async deleteOneAppUserDefineTheme(themeId: number | null): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/Administration/DeleteOneAppUserDefineTheme?themeId=${themeId}`, {
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Failed to delete user-defined theme');
    return response.json();
  }

  async provisionTenant(data: {
    CompanyName: string;
    DomainToken: string;
    AdminEmail: string;
    AdminLoginName: string;
    AdminPassword: string;
    TemplateId?: string;
  }): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/TenantProvisioning/Provision`, {
      method: 'POST',
      headers: getHeaders(),
      body: JSON.stringify(data)
    });
    if (!response.ok) throw new Error('Tenant provisioning failed');
    return response.json();
  }

  async runMigrationsOnAllTenants(): Promise<any> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/TenantProvisioning/RunMigrations`, {
      method: 'POST',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Migration run failed');
    return response.json();
  }

  async repairTenantAdminUsers(): Promise<{ RowsFixed: number }> {
    const response = await fetch(`${endpoints.BASE_URL}/webapi/TenantProvisioning/RepairAdminUsers`, {
      method: 'POST',
      headers: getHeaders()
    });
    if (!response.ok) throw new Error('Repair failed');
    return response.json();
  }
}

export const adminSvc = new AdminService(); 