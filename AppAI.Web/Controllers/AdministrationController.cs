using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using AppAI.Web.Controllers.Base;
using AppAI.Web.Models;
using Microsoft.AspNetCore.Mvc;
using static App.BL.AppThemeBL;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AdministrationController : SecureBaseController
{
    // ============================================================
    // From AdministrationController.cs (partial)
    // ============================================================

    [HttpGet]
    public OperationCallResult<object> AddSearchToMainMenu(int? searchOrSavedSearchId, string menuName, bool isSavedSearch)
    {
        if (searchOrSavedSearchId.HasValue)
        {
            return AppTreeListMenuBL.AddSearchToMainMenu(searchOrSavedSearchId, menuName, isSavedSearch);
        }
        else
        {
            return null;
        }
    }


    [HttpGet]
    public OperationCallResult<object> AddListTransactionToMainMenu(int? transactionId, string menuName)
    {
        if (transactionId.HasValue)
        {
            return AppTreeListMenuBL.AddListTransactionToMainMenu(transactionId.Value, menuName);
        }
        else
        {
            return null;
        }
    }



    [HttpGet]
    public OperationCallResult<object> DeleteOneAppListMenuTreeNode(int? menuId)
    {
        if (menuId.HasValue)
        {
            return AppTreeListMenuBL.DeleteOneAppListMenuTreeNode(menuId.Value);
        }
        else
        {
            return null;
        }
    }



    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveDomainOrUserMenu(int? Id, EmAppMenuRegisterType anEmMenuRegisterType)
    {
        if (Id.HasValue)
        {
            return AppListMenuUserAndDomainBL.RetrieveDomainOrUserTreeMenu(new List<int>() { Id.Value }, anEmMenuRegisterType);
        }

        return new ObservableSet<AppListMenuExDto>();
    }

    [HttpPost]
    public OperationCallResult<AppListMenuExDto> SaveDomainOrUserMenu(DomainOrUserListMenuSetDto aDomainOrUserListMenuSetDto)
    {
        if (aDomainOrUserListMenuSetDto.DomainOrUserId.HasValue && aDomainOrUserListMenuSetDto.MenuRegisterType != 0 && aDomainOrUserListMenuSetDto.NeedToSaveMenuIds != null)
        {
            return AppListMenuUserAndDomainBL.SaveDomainOrUserMenu(aDomainOrUserListMenuSetDto.DomainOrUserId.Value, aDomainOrUserListMenuSetDto.MenuRegisterType, aDomainOrUserListMenuSetDto.NeedToSaveMenuIds);
        }

        return null;
    }



    [HttpGet]
    public List<AppEntityInfoDto> RetrieveAllAppEntityInfoDto(bool? isGetUserDefinedOnly)
    {
        if (isGetUserDefinedOnly.HasValue && isGetUserDefinedOnly.Value)
        {
            return AppEntityInfoBL.RetrieveAllAppEntityInfoDto().Where(o => !o.IsSystemDefine.HasValue || !o.IsSystemDefine.Value).OrderBy(o => o.EntityCode).ToList();
        }
        else
        {
            return AppEntityInfoBL.RetrieveAllAppEntityInfoDto().OrderBy(o => o.EntityCode).ToList();
        }
    }

    [HttpGet]
    public AppEntityInfoExDto RetrieveOneAppEntityInfoExDto(int? id, bool? includeLookUpItems)
    {
        if (id.HasValue)
        {
            return AppEntityInfoBL.RetrieveOneAppEntityInfoExDto(id.Value, includeLookUpItems);
        }
        return null;
    }

    [HttpGet]
    public ValidationResult DeleteOneAppEntityInfo(int id)
    {
        return AppEntityInfoBL.DeleteOneAppEntityInfo(id);
    }


    [HttpPost]
    public OperationCallResult<AppEntityInfoExDto> SaveOneAppEntityInfoDto(AppEntityInfoExDto appEntityInfoDto)
    {
        return AppEntityInfoBL.SaveOneAppEntityInfoDto(appEntityInfoDto);
    }

    [HttpGet]
    public List<LookupItemDto> GetLookupItemListByEntityInfoId(int? entityInfoId)
    {
        if (entityInfoId.HasValue)
        {
            List<LookupItemDto> toReturn = AppEntityInfoBL.GetLookupItemList(entityInfoId.Value, string.Empty);
            return toReturn;
        }
        return null;
    }


    [HttpGet]
    public IDictionary<string, IEnumerable<LookupItemDto>> RetrieveMassAppEntitiesLookupItem(string entityCodes)
    {
        List<EmAppEntityLookupInfoCode> EmEntityCodeList = new List<EmAppEntityLookupInfoCode>();

        string[] entityCodesArray = entityCodes.Split('|');

        IDictionary<string, IEnumerable<LookupItemDto>> dictEntityCodeLookupItem = AppEntityInfoBL.RetrieveMassAppEntitiesLookupItemByEntityCodes(entityCodesArray);

        return dictEntityCodeLookupItem;
    }

    [HttpGet]
    public List<LookupItemDto> AddOneLookupItemList(int entityInfoID, string displayField1)
    {
        List<LookupItemDto> toReturn = AppEntityInfoBL.AddOneLookupItemList(entityInfoID, displayField1);

        return toReturn;
    }


    [HttpGet]
    public OperationCallResult<object> DeleteAppSecurityGroup(int? securityGroupId)
    {
        return AppSecurityGroupBL.DeleteAppSecurityGroup(securityGroupId);
    }

    [HttpPost]
    public OperationCallResult<AppSecurityGroupExDto> SaveAppSecurityGroupExDto(AppSecurityGroupExDto aAppSecurityGroupExDto)
    {
        if (aAppSecurityGroupExDto.DictDeletedItemsIds != null)
        {
            if (aAppSecurityGroupExDto.DictDeletedItemsIds["AppSecurityGroupMemberList"] != null)
            {
                aAppSecurityGroupExDto.AppSecurityGroupMemberList.DeletedItemIds = aAppSecurityGroupExDto.DictDeletedItemsIds["AppSecurityGroupMemberList"];
            }
        }

        return AppSecurityGroupBL.SaveAppSecurityGroupExDto(aAppSecurityGroupExDto);
    }


    [HttpGet]
    public AppSecurityUserExDto RetrieveUserExDtoByDomainBusinessUser(int? domainId, int? businessUserId)
    {
        if (domainId.HasValue && businessUserId.HasValue)
        {
            return AppSecurityUserBL.RetrieveUserExDtoByDomainBusinessUser(domainId, businessUserId);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<object> DeleteAppSecurityUser(int? userId)
    {
        return AppSecurityUserBL.DeleteAppSecurityUser(userId);
    }


    [HttpGet]
    public ObservableSet<AppSecurityRegDomainExDto> RetrieveAllAppSecurityRegDomainEntityDto()
    {
        return AppSecurityRegDomainBL.RetrieveAllAppSecurityRegDomainEntityDto();
    }

    [HttpGet]
    public AppSecurityRegDomainExDto RetrieveOneAppSecurityRegDomainExDto(int? Id)
    {
        if (Id.HasValue)
        {
            return AppSecurityRegDomainBL.RetrieveOneAppSecurityRegDomainExDto(Id);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppSecurityRegDomainExDto> SaveAllAppSecurityRegDomainEntityDto(SecurityRegDomainSetDto securityRegDomainSetDto)
    {
        if (securityRegDomainSetDto != null)
        {
            securityRegDomainSetDto.SecurityRegDomainSet.DeletedItemIds = securityRegDomainSetDto.DeletedItemIds;

            return AppSecurityRegDomainBL.SaveAllAppSecurityRegDomainEntityDto(securityRegDomainSetDto.SecurityRegDomainSet);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<object> DeleteOneAppSecurityRegDomainEntityDto(int? Id)
    {
        return AppSecurityRegDomainBL.DeleteOneAppSecurityRegDomainEntityDto(Id);
    }


    [HttpGet]
    public List<LookupItemDto> RetrieveTimeZones()
    {
        return AppSecurityUserBL.RetrieveTimeZones().ToList();
    }

    [HttpGet]
    public List<LookupItemDto> GetCultroInfos()
    {
        return AppSecurityUserBL.GetCultroInfos();
    }

    [HttpGet]
    public ObservableSet<AppRouteStateExDto> RetrieveAllAppRouteStateEntityDto()
    {
        return AppRouteStateBL.RetrieveAllAppRouteStateEntityDto();
    }


    [HttpGet]
    public ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveOrganizationDetailGroupUserPrivilegeDtoByType(int? objectID, int? objType, int? organizationId, int? actionCode, bool? isIgnoreCurrentUserFilterSetup, int? partnerType)
    {
        if (objectID.HasValue && objType.HasValue)
        {
            isIgnoreCurrentUserFilterSetup = isIgnoreCurrentUserFilterSetup.HasValue ? isIgnoreCurrentUserFilterSetup.Value : false;
            return AppSecuritySysObjGroupUserBL.RetrieveOrganizationDetailGroupUserPrivilegeDtoByType(objectID, objType.Value, actionCode, isIgnoreCurrentUserFilterSetup.Value, partnerType);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppSecuritySysObjGroupUserExDto> SavOrganizationDetailLevelUserRowbyType(SecurityObjSetDto securityObjSetDto)
    {
        if (securityObjSetDto != null)
        {
            return AppSecuritySysObjGroupUserBL.SavOrganizationDetailLevelUserRowbyType(securityObjSetDto);
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveOrganizationPrivilegeDtoByType(int? organizationId, int objType)
    {
        return AppSecuritySysObjGroupUserBL.RetrieveOrganizationPrivilegeDtoByType(objType);
    }


    [HttpPost]
    public OperationCallResult<AppSecuritySysObjGroupUserExDto> SaveNewOrganizationPrivilegeByType(SecurityObjSetDto securityObjSetDto)
    {
        if (securityObjSetDto != null)
        {
            return AppSecuritySysObjGroupUserBL.SaveNewOrganizationPrivilegeByType(securityObjSetDto.AppSecuritySysObjGroupUserSet, securityObjSetDto.AppSecuritySysObjType);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppSecuritySysObjGroupUserExDto> DeleteOrganizationPrivilegeByType(SecurityObjSetDto securityObjSetDto)
    {
        if (securityObjSetDto != null)
        {
            return AppSecuritySysObjGroupUserBL.DeleteOrganizationPrivilegeByType(securityObjSetDto.AppSecuritySysObjGroupUserSet, securityObjSetDto.AppSecuritySysObjType);
        }

        return null;
    }


    //---------

    [HttpGet]
    public ObservableSet<AppSecuritySysObjGroupUserExDto> RetrieveUserTypePrivilegeDtoByType(int userType, int objType)
    {
        return AppSecuritySysObjGroupUserBL.RetrieveUserTypePrivilegeDtoByType(userType, objType);
    }

    [HttpPost]
    public OperationCallResult<AppSecuritySysObjGroupUserExDto> SaveNewUserTypePrivilegeByType(SecurityObjSetDto securityObjSetDto)
    {
        if (securityObjSetDto != null && securityObjSetDto.UserType.HasValue)
        {
            return AppSecuritySysObjGroupUserBL.SaveNewUserTypePrivilegeByType(securityObjSetDto.AppSecuritySysObjGroupUserSet, securityObjSetDto.UserType.Value, securityObjSetDto.AppSecuritySysObjType);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppSecuritySysObjGroupUserExDto> DeleteUserTypePrivilegeByType(SecurityObjSetDto securityObjSetDto)
    {
        if (securityObjSetDto != null && securityObjSetDto.UserType.HasValue)
        {
            return AppSecuritySysObjGroupUserBL.DeleteUserTypePrivilegeByType(securityObjSetDto.AppSecuritySysObjGroupUserSet, securityObjSetDto.UserType.Value, securityObjSetDto.AppSecuritySysObjType);
        }

        return null;
    }


    [HttpGet]
    public TransactionPrivilegeDto GetOneAppTransactionAvailablePrivileges(int? transactionId, int? organizationId, int? userType)
    {
        return AppSecuritySysObjGroupUserBL.GetOneAppTransactionAvailablePrivileges(transactionId, userType);
    }


    [HttpGet]
    public ObservableSet<AppSetupExDto> RetrieveAllAppSetupDtoList(bool isMasterDb)
    {
        var identity = (APP.Components.Dto.AppClientIdentity?)APP.Framework.ServerContext.Instance.CurrnetClientIdentity;
        bool isSysAdmin = identity?.CurrentLoginUserType == (int)EmAppUserType.SysAdmin;
        return isSysAdmin
            ? AppSystemSettingBL.RetrieveAllAsDto()
            : AppTenantSettingBL.RetrieveAllAsDto();
    }

    [HttpPost]
    public OperationCallResult<AppSetupExDto> SaveAllAppSetupEntityDto([FromBody] AppSetupSaveRequest request)
    {
        if (request?.InternalItems == null) return null;

        // ObservableSet implements IEnumerable — Newtonsoft expects a JSON array for that type,
        // so bind via AppSetupSaveRequest ({ InternalItems, DeletedItemIds }) instead.
        var aSet = new ObservableSet<AppSetupExDto>();
        foreach (var item in request.InternalItems)
        {
            if (item != null)
                aSet.Add(item);
        }
        aSet.DeletedItemIds = request.DeletedItemIds ?? new List<object>();

        var identity = (APP.Components.Dto.AppClientIdentity?)APP.Framework.ServerContext.Instance.CurrnetClientIdentity;
        bool isSysAdmin = identity?.CurrentLoginUserType == (int)EmAppUserType.SysAdmin;

        return isSysAdmin
            ? AppSystemSettingBL.SaveAll(aSet)
            : AppTenantSettingBL.SaveAll(aSet);
    }


    [HttpGet]
    public IEnumerable<string> RetrieveAllWebPageTemplateFileNameList()
    {
        return AppTenantSettingBL.RetrieveAllWebPageTemplateFileNameList();
    }


    [HttpGet]
    public List<string> GetApplicationIconList()
    {
        string apPath = AppContext.BaseDirectory;
        string folderpath = apPath + "Resources\\Images\\SystemIcons\\64\\";
        return Directory.EnumerateFiles(folderpath, "*.png").Select(o => Path.GetFileName(o)).ToList();
    }


    [HttpGet]
    public ObservableSet<AppCalendarDto> RetrieveAllAppCalendarDto()
    {
        return AppCalendarBL.RetrieveAllAppCalendarDto();
    }


    [HttpGet]
    public ObservableSet<AppCalendarDto> RetrieveAllCompanyCalendarDto()
    {
        return AppCalendarBL.RetrieveAllAppCalendarDto();
    }


    [HttpGet]
    public AppCalendarExDto RetrieveOneAppCalendarExDto(int? calendarId)
    {
        if (calendarId.HasValue)
        {
            return AppCalendarBL.RetrieveOneAppCalendarExDto(calendarId.Value);
        }
        return null;
    }

    [HttpGet]
    public int? GetUserCalendarId(int? userId)
    {
        if (userId.HasValue)
        {
            return AppCalendarBL.GetUserCalendarId(userId.Value);
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<AppCalendarExDto> SaveAppCalendar(AppCalendarExDto aAppCalendarExDto)
    {
        if (aAppCalendarExDto != null)
        {
            if (aAppCalendarExDto.DictDeletedItemsIds == null)
            {
                aAppCalendarExDto.DictDeletedItemsIds = new Dictionary<string, List<object>>();
            }

            if (aAppCalendarExDto.DictDeletedItemsIds.ContainsKey("AppCalendarRecurringDayList"))
            {
                aAppCalendarExDto.AppCalendarRecurringDayList.DeletedItemIds = aAppCalendarExDto.DictDeletedItemsIds["AppCalendarRecurringDayList"];
            }

            if (aAppCalendarExDto.DictDeletedItemsIds.ContainsKey("AppCalendarSpecificDayList"))
            {
                aAppCalendarExDto.AppCalendarSpecificDayList.DeletedItemIds = aAppCalendarExDto.DictDeletedItemsIds["AppCalendarSpecificDayList"];
            }

            return AppCalendarBL.SaveAppCalendar(aAppCalendarExDto);
        }
        return null;
    }


    [HttpGet]
    public OperationCallResult<object> DeleteAppCalendar(int? calendarId)
    {
        if (calendarId.HasValue)
        {
            return AppCalendarBL.DeleteAppCalendar(calendarId.Value);
        }
        return null;
    }

    [HttpPost]
    public AppCalendarViewExDto RetrieveCalenarView(AppCalendarViewExDto calendarView)
    {
        return AppCalendarBL.RetrieveCalenarView(calendarView);
    }


    [HttpGet]
    public List<AppComOrganizationDto> GetCompanyOrgHairarchy(int companyId)
    {
        List<AppComOrganizationDto> toReturn = AppComOrgBL.GetCompanyOrgHairarchy(companyId);

        return toReturn;
    }

    [HttpGet]
    public List<AppComOrganizationDto> GetCompanyOrganizationDtoFlatList(int companyId)
    {
        List<AppComOrganizationDto> toReturn = AppComOrgBL.GetCompanyOrganizationDtoFlatList(companyId);

        return toReturn;
    }


    [HttpPost]
    public OperationCallResult<bool> TransferOrganizationUsers(UserTransferSetDto userTransferSetDto)
    {
        if (userTransferSetDto != null
            && userTransferSetDto.TargetOrganizationId.HasValue
            && userTransferSetDto.TargetDomainId.HasValue
            && userTransferSetDto.NeedToTransferUserIdList != null
            && userTransferSetDto.NeedToTransferUserIdList.Count > 0)
        {
            return AppComOrgBL.TransferOrganizationUsers(userTransferSetDto.TargetOrganizationId.Value, userTransferSetDto.TargetDomainId.Value, userTransferSetDto.NeedToTransferUserIdList);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<bool> SaveAppComOrganizationSet(ComOrganizationSetDto comOrganizationSetDto)
    {
        if (comOrganizationSetDto != null && comOrganizationSetDto.ComOrganizationExDtoSet != null)
        {
            comOrganizationSetDto.ComOrganizationExDtoSet.DeletedItemIds = comOrganizationSetDto.DeletedItemIds;
            return AppComOrgBL.SaveAppComOrganizationSet(comOrganizationSetDto.ComOrganizationExDtoSet);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppComOrganizationExDto> SaveAppComOrganization(AppComOrganizationExDto aAppComOrganizationExDto)
    {
        aAppComOrganizationExDto.IsModified = true;
        if (aAppComOrganizationExDto.Children != null)
        {
            foreach (var child in aAppComOrganizationExDto.Children)
            {
                child.IsModified = true;
            }
        }
        return AppComOrgBL.SaveAppComOrganization(aAppComOrganizationExDto);
    }



    [HttpGet]
    public OperationCallResult<object> DeleteAppComOrganization(int? organizationId)
    {
        if (organizationId.HasValue)
        {
            return AppComOrgBL.DeleteAppComOrganization(organizationId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppSecurityUserDto> GetOrganizationUserDtoList(int? comapnyId, bool isIncludeBusinessParternerUsers)
    {
        if (comapnyId.HasValue)
        {
            return AppComOrgBL.GetOrganizationUserDtoList(isIncludeBusinessParternerUsers)
                .Where(o => o.DomainId != (int)EmAppUserType.Customer
                && o.DomainId != (int)EmAppUserType.Supplier
                && o.DomainId != (int)EmAppUserType.ClientAgent
                && o.DomainId != (int)EmAppUserType.SupplierAgent)
                .ToList();
        }
        return null;
    }

    [HttpGet]
    public List<AppSecurityUserDto> GetCurrentTenantUserDtoList(bool isIncludeBusinessParternerUsers)
    {
        return AppComOrgBL.GetCurrentTenantUserDtoList(isIncludeBusinessParternerUsers)
            .Where(o => o.DomainId != (int)EmAppUserType.Customer
            && o.DomainId != (int)EmAppUserType.Supplier
            && o.DomainId != (int)EmAppUserType.ClientAgent
            && o.DomainId != (int)EmAppUserType.SupplierAgent)
            .ToList();
    }


    [HttpGet]
    public List<AppSecurityGroupDto> GetOrganizationGroupList(int? comapnyId)
    {
        if (comapnyId.HasValue)
        {
            return AppComOrgBL.GetOrganizationGroupList();
        }

        return null;
    }

    [HttpGet]
    public List<AppSecurityUserDto> GetPartnerTypeUserDtoList(int? partnerType)
    {
        if (partnerType.HasValue)
        {
            return AppComOrgBL.GetPartnerTypeUserDtoList(partnerType.Value);
        }
        return null;
    }


    [HttpGet]
    public List<AppSecurityGroupDto> GetPartnerTypeGroupList(int? partnerType)
    {
        if (partnerType.HasValue)
        {
            return AppComOrgBL.GetPartnerTypeGroupList(partnerType.Value);
        }

        return null;
    }


    [HttpGet]
    public List<AppSecurityUserDto> GetCompanyUserDtoList(int? companyId)
    {
        if (companyId.HasValue)
        {
            return AppSecurityUserBL.GetCompanyUserDtoList(companyId.Value);
        }
        return null;
    }


    [HttpGet]
    public List<AppListMenuExDto> RetrieveEmployeeDomainMenuTree()
    {
        return AppListMenuUserAndDomainBL.RetrieveEmployeeDomainMenuTree();
    }



    [HttpGet]
    public RoleAndUserListDto RetrieveTransactionAvailableOrganizationRoleAndUser(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            return AppSecuritySysObjGroupUserBL.RetrieveTransactionAvailableOrganizationRoleAndUser(transactionId.Value);
        }

        return null;
    }


    [HttpGet]
    public List<AppDataSourceRegisterExDto> RetrieveAllAppDataSourceRegisterExDto()
    {
        return AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterExDto().ToList();
    }


    [HttpPost]
    public OperationCallResult<AppDataSourceRegisterExDto> SaveAllAppDataSourceRegisterExDto(DataSourceRegisterSetDto aSet)
    {
        if (aSet != null && aSet.DataSourceRegisterSet != null)
        {
            aSet.DataSourceRegisterSet.DeletedItemIds = aSet.DeletedItemIds;
            return AppDataSourceRegisterBL.SaveAllAppDataSourceRegisterExDto(aSet.DataSourceRegisterSet);
        }

        return null;
    }


    [HttpGet]
    public List<AppDataSourceRegisterExDto> GetDataSourceRegisterList()
    {
        return AppDataSourceRegisterBL.GetDataSourceRegisterList().ToList();
    }


    [HttpGet]
    public List<AppCompanyDto> RetrieveAllRootCompanyDtoList()
    {
        return AppCompanyBL.RetrieveAllRootCompanyDtoList();
    }

    [HttpGet]
    public List<AppCompanyDto> RetrieveAllSaasCompanyDtoList()
    {
        return AppCompanyBL.RetrieveAllSaasCompanyDtoList();
    }

    [HttpGet]
    public AppCompanyExDto RetrieveCurrentUserCompanyExDto()
    {
        AppCompanyExDto dto = AppCompanyBL.RetrieveCurrentUserCompanyExDto();
        if (dto?.Id == null)
        {
            return null;
        }

        RequireCompanyAccess((int)dto.Id);
        return dto;
    }

    [HttpGet]
    public AppCompanyExDto RetrieveOneAppCompanyExDto(int? companyId)
    {
        if (!companyId.HasValue) return null;

        RequireCompanyAccess(companyId.Value);
        return AppCompanyBL.RetrieveOneAppCompanyExDto(companyId.Value);
    }

    [HttpPost]
    public OperationCallResult<AppCompanyExDto> SaveOneAppCompanyExDto(AppCompanyExDto aAppCompanyExDto)
    {
        if (aAppCompanyExDto == null) return null;

        if (aAppCompanyExDto.Id != null)
            RequireCompanyAccess((int)aAppCompanyExDto.Id);
        return AppCompanyBL.SaveOneAppCompanyExDto(aAppCompanyExDto);
    }

    [HttpPost]
    public OperationCallResult<bool> DeleteOneAppCompany(int? companyId)
    {
        if (!companyId.HasValue) return null;
        RequireCompanyAccess(companyId.Value);
        return AppCompanyBL.DeleteOneAppCompany(companyId.Value);
    }

    [HttpGet]
    public object RetrieveAppSecurityUserDtoByCompanyId(int? companyId)
    {
        if (!companyId.HasValue) return null;
        RequireCompanyAccess(companyId.Value);
        return AppSecurityUserBL.RetrieveAppSecurityUserDtoByCompanyId(companyId.Value);
    }

    public List<AppSecurityUserDto> GetSimpleUserListByCompanyId(int? companyId)
    {
        if (!companyId.HasValue) return new List<AppSecurityUserDto>();
        RequireCompanyAccess(companyId.Value);
        return AppSecurityUserBL.RetrieveSimpleUserDtoListByCompanyId(companyId.Value);
    }

    // SysAdmin may access any company; SaasCompanyAdmin only their own.
    private void RequireCompanyAccess(int companyId)
    {
        var identity = ServerContext.Instance.CurrnetClientIdentity;
        if (identity == null)
            throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Unauthorized", (int)HttpStatusCode.Unauthorized);

        if (identity.CurrentLoginUserType == (int)EmAppUserType.SysAdmin)
            return;

        if (identity.CurrentLoginUserType == (int)EmAppUserType.SaasCompanyAdmin
            && identity.CurrentWorkingCompanyId != null && (int)identity.CurrentWorkingCompanyId == companyId)
            return;

        throw new Microsoft.AspNetCore.Http.BadHttpRequestException("Forbidden", (int)HttpStatusCode.Forbidden);
    }


    [HttpGet]
    public List<AppBusinessPartnerDto> RetrieveCompanyPartnerDtoList(int? companyId, int? partnerType)
    {
        if (companyId.HasValue)
        {
            return AppBusinessPartnerBL.RetrieveCompanyPartnerDtoList(companyId.Value, partnerType);
        }

        return null;
    }

    [HttpGet]
    public AppBusinessPartnerExDto RetrieveOneAppBusinessPartnerExDto(int? partnerID)
    {
        if (partnerID.HasValue)
        {
            return AppBusinessPartnerBL.RetrieveOneAppBusinessPartnerExDto(partnerID.Value);
        }

        return null;
    }

    [HttpGet]
    public AppBusinessPartnerExDto RetrieveCurrentUserAppBusinessPartnerExDto()
    {
        return AppBusinessPartnerBL.RetrieveCurrentUserAppBusinessPartnerExDto();
    }

    [HttpPost]
    public OperationCallResult<AppBusinessPartnerExDto> SaveOneAppBusinessPartnerExDto(AppBusinessPartnerExDto aAppBusinessPartnerExDto)
    {
        if (aAppBusinessPartnerExDto != null)
        {
            return AppBusinessPartnerBL.SaveOneAppBusinessPartnerExDto(aAppBusinessPartnerExDto);
        }

        return null;
    }

    [HttpGet]
    public List<AppSecurityUserDto> RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(int? parternerId)
    {
        if (parternerId.HasValue)
        {
            return AppBusinessPartnerBL.RetrieveCurrentCompanyOneBusinessPartnerAllInviteUserDtoList(parternerId.Value);
        }

        return null;
    }


    [HttpGet]
    public List<AppComOrgLevelDto> RetrieveCompanyOrgLevelDtoList(int? companyId)
    {
        if (companyId.HasValue)
        {
            return AppComOrgBL.RetrieveCompanyOrgLevelDtoList(companyId.Value);
        }

        return null;
    }


    [HttpGet]
    public List<LookupItemDto> RetrieveCurrentUserCompany()
    {
        return AppSecurityManagementBL.RetrieveCurrentUserCompany();
    }


    [HttpGet]
    public ObservableSet<AppReportExDto> RetrieveAllAppReportEntityDto()
    {
        return AppReportBL.RetrieveAllAppReportEntityDto();
    }

    [HttpGet]
    public List<AppReportExDto> RetrieveCurrnetUserReportDto()
    {
        return AppSecurityManagementBL.RetrieveCurrnetUserReportDto();
    }


    [HttpGet]
    public AppReportExDto RetrieveOneAppReportExDto(int? reportId)
    {
        if (reportId.HasValue)
        {
            return AppReportBL.RetrieveOneAppReportExDto(reportId.Value);
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<AppReportExDto> SaveAllAppReportEntityDto(AppReportSetDto appReportSetDto)
    {
        if (appReportSetDto != null && appReportSetDto.AppReportExDtoSet != null)
        {
            appReportSetDto.AppReportExDtoSet.DeletedItemIds = new List<object>();

            if (appReportSetDto.DeletedItemIds != null)
            {
                appReportSetDto.AppReportExDtoSet.DeletedItemIds = appReportSetDto.DeletedItemIds;
            }

            return AppReportBL.SaveAllAppReportEntityDto(appReportSetDto.AppReportExDtoSet);
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppReportExDto> SaveOneAppReportEntityDto(AppReportExDto aAppReportExDto)
    {
        if (aAppReportExDto != null)
        {
            return AppReportBL.SaveOneAppReportEntityDto(aAppReportExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppReportEntityDto(int? reportId)
    {
        if (reportId.HasValue)
        {
            return AppReportBL.DeleteOneAppReportEntityDto(reportId.Value);
        }
        return null;
    }


    [HttpGet]
    public List<AppSecurityUserContactExDto> RetrieveOneUserAllContactEntityDto(int? userId)
    {
        if (userId.HasValue)
        {
            return AppSecurityUserContactBL.RetrieveOneUserAllContactEntityDto(userId.Value);
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserContactExDto> SaveAllAppSecurityUserContactEntityDto(UserContactSetDto userContactSetDto)
    {
        if (userContactSetDto != null && userContactSetDto.UserContactList != null && userContactSetDto.UserId.HasValue)
        {
            return AppSecurityUserContactBL.SaveAllAppSecurityUserContactEntityDto(userContactSetDto.UserContactList, userContactSetDto.UserId.Value);
        }

        return null;
    }


    [HttpGet]
    public List<LookupItemDto> RetrieveCurrentUserAvailableCompaniesFromMasterDB()
    {
        return AppSecurityManagementBL.RetrieveUserAvailableCompaniesFromMasterDB(AppSecurityUserBL.CurrentUserId);
    }


    [HttpGet]
    public List<LookupItemDto> RetrieveEmployeeUserExternalMappingAccountLookupItemList()
    {
        return AppSecurityManagementBL.RetrieveEmployeeUserExternalMappingAccountLookupItemList();
    }


    [HttpGet]
    public SearchDto RetrieveCurrentUserCalendarSearch(int? searchId)
    {
        return AppSearchBL.RetrieveCurrentUserCalendarSearch(searchId);
    }


    [HttpPost]
    public SearchResultDto RetrieveUserCalendarSearchResult(SearchDto searchDto)
    {
        AppSearchBL.ConvertSearchCriteriaDateFromUTCToClient(searchDto);
        SearchResultDto searchResult = AppSearchBL.RetrieveUserCalendarSearchResult(searchDto);
        return searchResult;
    }


    [HttpGet]
    public OperationCallResult<bool> DeleteCompanyLogoImage()
    {
        return AppCompanyBL.DeleteCompanyLogoImage();
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneCompanyBackgroundImage(string fileName)
    {
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return AppCompanyBL.DeleteOneCompanyBackgroundImage(fileName);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<object> UninstallSaasAccount(int? companyId)
    {
        if (companyId.HasValue)
        {
            return AppSaasAccountUserBL.UninstallSaasAccount(companyId.Value);
        }

        return null;
    }


    [HttpGet]
    public OperationCallResult<object> ConvertOneSaasAccountToStandaloneApplication(int? companyId)
    {
        if (companyId.HasValue)
        {
            return AppSaasAccountUserBL.ConvertOneSaasAccountToStandaloneApplication(companyId.Value);
        }

        return null;
    }

    [HttpGet]
    public List<AppSecurityUserDto> RetrieveAllIntegrationTokenDto()
    {
        return AppSecurityUserBL.RetrieveAllIntegrationTokenDto();
    }

    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> SaveOneIntegrationTokenExDto(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        return AppSecurityUserBL.SaveOneIntegrationTokenExDto(aAppSecurityUserExDto);
    }


    [HttpGet]
    public List<AppUserThemeDto> RetrieveAvailableUserDefineThemeDtoList()
    {
        return AppThemeBL.RetrieveAvailableUserDefineThemeDtoList();
    }

    [HttpGet]
    public AppUserThemeDto RetrieveOneAppUserDefineThemeDto(int? themeId)
    {
        return AppThemeBL.RetrieveOneAppUserDefineThemeDto(themeId);
    }

    [HttpPost]
    public OperationCallResult<AppUserThemeDto> SaveOneAppUserDefineThemeDto(AppUserThemeDto themeDto)
    {
        return AppThemeBL.SaveOneAppUserDefineThemeDto(themeDto);
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneAppUserDefineTheme(int? themeId)
    {
        return AppThemeBL.DeleteOneAppUserDefineTheme(themeId);
    }

    // ============================================================
    // From AdministrationControllerSecurity.cs (partial)
    // ============================================================

    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveAllMenu(bool? isWebPageMenu) //for administrato
    {
        isWebPageMenu = isWebPageMenu.HasValue && isWebPageMenu.Value;
        return AppListMenuBL.RetrieveAllAppListMenuEntityDto(isWebPageMenu.Value);
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> GetAllMenu(bool isWebPageMenu) //for Test
    {
        return AppListMenuBL.RetrieveAllAppListMenuEntityDto(isWebPageMenu);
    }


    [HttpGet]
    public ObservableSet<AppListMenuExDto> GetMenu() //for current user
    {
        ObservableSet<AppListMenuExDto> userMenuList = AppSecurityManagementBL.RetrieveUserMenu();

        return userMenuList;
    }


    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveUserTreeMenu() //for current user
    {
        ObservableSet<AppListMenuExDto> userMenuList = AppSecurityManagementBL.RetrieveUserTreeMenu();

        return userMenuList;
    }


    [HttpPost]
    public ObservableSet<AppListMenuExDto> SaveAllAppListMenuEntityDto(ListMenusObjDto listMenusObjDto)
    {
        if (listMenusObjDto != null)
        {
            listMenusObjDto.ListMenuSet.DeletedItemIds = listMenusObjDto.DeletedItemIds;


            foreach (var item in listMenusObjDto.ListMenuSet)
            {
                if (item.DeletedItemsIds != null)
                {
                    item.AppListMenu_List.DeletedItemIds = item.DeletedItemsIds;
                }
            }

            OperationCallResult<AppListMenuExDto> aOperationCallResult = AppListMenuBL.SaveAllAppListMenuEntityDto(listMenusObjDto.ListMenuSet, listMenusObjDto.IsWebPageMenu);

            if (aOperationCallResult != null && aOperationCallResult.ObjectList != null)
            {
                return aOperationCallResult.ObjectList as ObservableSet<AppListMenuExDto>;
            }
        }

        return null;
    }


    [HttpGet]
    public AppListMenuExDto RetrieveOneAppListMenuExDto(int? menuId)
    {
        if (menuId.HasValue)
        {
            return AppListMenuBL.RetrieveOneAppListMenuExDto(menuId.Value);
        }
        else
        {
            return null;
        }
    }


    [HttpPost]
    public OperationCallResult<AppListMenuExDto> SaveOneAppListMenuExDto(AppListMenuExDto appListMenuExDto)
    {
        return AppListMenuBL.SaveOneAppListMenuExDto(appListMenuExDto);
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveListMenuHairarchyDto(bool? isWebPageMenu, int? rootMenuId)
    {
        isWebPageMenu = isWebPageMenu.HasValue && isWebPageMenu.Value;
        return AppTreeListMenuBL.RetrieveListMenuHairarchyDto(isWebPageMenu.Value, rootMenuId);
    }


    [HttpPost]
    public OperationCallResult<AppListMenuExDto> SaveOneAppListMenuTreeNode(AppListMenuExDto appListMenuExDto)
    {
        return AppTreeListMenuBL.SaveOneAppListMenuTreeNode(appListMenuExDto);
    }

    [HttpPost]
    public OperationCallResult<AppListMenuExDto> SaveAllTreeListMenuDto(ListMenusObjDto listMenusObjDto)
    {
        if (listMenusObjDto != null)
        {
            OperationCallResult<AppListMenuExDto> aOperationCallResult = AppTreeListMenuBL.SaveAllTreeListMenuDto(listMenusObjDto.ListMenuSet, listMenusObjDto.IsWebPageMenu);
            return aOperationCallResult;
        }

        return null;
    }


    [HttpGet]
    public ObservableSet<AppSecurityGroupDto> RetrieveAllAppSecurityGroupDto()
    {
        return AppSecurityGroupBL.RetrieveAllAppSecurityGroupDto();
    }

    [HttpGet]
    public List<AppSecurityGroupDto> RetrieveAppSecurityGroupDtoByGroupUsage(int? groupUsage, bool? isFilterBuiltInRoles, int? userType)
    {
        if (groupUsage.HasValue)
        {
            return AppSecurityGroupBL.RetrieveAppSecurityGroupDtoByUsageType(groupUsage.Value, isFilterBuiltInRoles, userType);
        }
        return null;
    }


    [HttpGet]
    public AppSecurityGroupExDto RetrieveOneAppSecurityGroupExDto(int? groupId)
    {
        return AppSecurityGroupBL.RetrieveOneAppSecurityGroupExDto(groupId);
    }


    [HttpGet]
    public ServerSettingDto CheckServerSetting()
    {
        ServerSettingDto toReturn = AppSystemSettingBL.CheckServerSetting();
        return toReturn;
    }

    [HttpGet]
    public bool EnableOrDisableCache(bool? isEnableCache)
    {
        if (!isEnableCache.HasValue)
        {
            isEnableCache = false;
        }
        return AppSystemSettingBL.EnableOrDisableCache(isEnableCache.Value);
    }


    #region ---- user security managment

    [HttpGet]
    public ObservableSet<AppSecurityUserDto> RetrieveAllAppSecurityUserDto()
    {
        var listUser = AppSecurityUserBL.RetrieveAllAppSecurityUserDto(true);

        return listUser;
    }

    [HttpGet]
    public List<AppSecurityUserSimpleDto> RetrieveAllSimpleUserDto()
    {
        var toReturn = AppSecurityUserBL.RetrieveAllSimpleUserDto();

        return toReturn;
    }

    [HttpGet]
    public List<AppSecurityUserSimpleDto> RetrieveCurrentUserAvailableEmailToUsers()
    {
        var toReturn = AppSecurityUserBL.RetrieveCurrentUserAvailableEmailToUsers();

        return toReturn;
    }


    [HttpGet]
    public ObservableSet<AppSecurityUserDto> RetrieveAllSystemBuiltinUserDto()
    {
        var listUser = AppSecurityUserBL.RetrieveAllSystemBuiltinUserDto();

        return listUser;
    }


    [HttpGet]
    public AppSecurityUserExDto RetrieveOneAppSecurityUserExDto(int? userId)
    {
        AppSecurityUserExDto aAppSecurityUserExDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(userId);

        return aAppSecurityUserExDto;
    }

    [HttpGet]
    public AppSecurityUserExDto RetrieveCurrentAppSecurityUserExDto()
    {
        AppSecurityUserExDto aAppSecurityUserExDto = AppSecurityUserBL.RetrieveOneAppSecurityUserExDto(AppSecurityUserBL.CurrentUserId);
        return aAppSecurityUserExDto;
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> SaveAppSecurityUserExDto(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        if (!(aAppSecurityUserExDto.IsDeleted as bool?).HasValue)
        {
            aAppSecurityUserExDto.IsDeleted = true;
        }

        if (aAppSecurityUserExDto.DictDeletedItemsIds != null)
        {
            if (aAppSecurityUserExDto.DictDeletedItemsIds.ContainsKey("AppSecurityGroupMemberList")
                && aAppSecurityUserExDto.DictDeletedItemsIds["AppSecurityGroupMemberList"] != null)
            {
                aAppSecurityUserExDto.AppSecurityGroupMemberList.DeletedItemIds = aAppSecurityUserExDto.DictDeletedItemsIds["AppSecurityGroupMemberList"];
            }
        }

        string newPassword = "";
        if (aAppSecurityUserExDto.IsNew)
        {
            newPassword = aAppSecurityUserExDto.Password;
            aAppSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(aAppSecurityUserExDto.Password);
            aAppSecurityUserExDto.ConfirmPassword = aAppSecurityUserExDto.Password;
        }
        else // update one user
        {
        }


        return AppSecurityUserBL.SaveAppSecurityUserExDto(aAppSecurityUserExDto, newPassword);
    }


    [HttpGet]
    public AppSecurityUserExDto RetrieveMasterDBUserLoginInfo(int? userId)
    {
        if (userId.HasValue)
        {
            return AppSecurityUserBL.RetrieveMasterDBUserLoginInfo(userId);
        }
        return null;
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> UpdateMasterDBUserLoginInfo(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        return AppSecurityUserBL.UpdateMasterDBUserLoginInfo(aAppSecurityUserExDto);
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> UpdateSaasUserLoginInfo(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        return AppSecurityUserBL.UpdateSaasUserLoginInfo(aAppSecurityUserExDto);
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> InviteSaasComapnyNewUser(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        aAppSecurityUserExDto.IsInvitingCompanyUser = true;
        aAppSecurityUserExDto.MyOwnCompnanyId = ControlTypeValueConverter.ConvertValueToInt(ServerContext.Instance.CurrentCompanyId);

        return AppSaasAccountUserBL.QuickCreateSaasUser(aAppSecurityUserExDto);
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> UpdateMyProfile(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        AppSecurityUserEntity aAppSecurityUserEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(aAppSecurityUserExDto.Id);

        // Cannot update  following property
        aAppSecurityUserExDto.DomainId = aAppSecurityUserEntity.DomainId;
        aAppSecurityUserExDto.IsBuiltIntUser = aAppSecurityUserEntity.IsBuiltIntUser;

        aAppSecurityUserExDto.IsActive = aAppSecurityUserEntity.IsActive;
        aAppSecurityUserExDto.IsDeleted = aAppSecurityUserEntity.IsDeleted;
        aAppSecurityUserExDto.RefreshToken = aAppSecurityUserEntity.RefreshToken;

        if (aAppSecurityUserExDto.Password != "password")
        {
            aAppSecurityUserExDto.Password = AppSecurityPasswordHashBL.HashPassword(aAppSecurityUserExDto.Password);
            aAppSecurityUserExDto.ConfirmPassword = aAppSecurityUserExDto.Password;
        }
        else
        {
            aAppSecurityUserExDto.Password = aAppSecurityUserExDto.ConfirmPassword = aAppSecurityUserEntity.Password;
        }


        return AppSecurityUserBL.SaveAppSecurityUserExDto(aAppSecurityUserExDto);
    }


    [HttpGet]
    public List<AppMenuSimpleDto> RetrieveAvailableApplicationPackages(bool? excludeChildMenu)
    {
        List<AppListMenuExDto> menuList = AppSaasUserApplicationPackageBL.RetrieveAvailableApplicationPackages(excludeChildMenu);

        List<AppMenuSimpleDto> toReturn = new List<AppMenuSimpleDto>();

        foreach (var menuExDto in menuList)
        {
            var simpleDto = AppMenuSimpleDto.ConvertAppListMenuExDtoToAppMenuSimpleDto(menuExDto);
            toReturn.Add(simpleDto);
        }

        return toReturn;
    }

    [HttpGet]
    public List<AppMenuSimpleDto> RetrieveSelectedApplicationPackages(bool? excludeChildMenu)
    {
        List<AppMenuSimpleDto> toReturn = AppSaasUserApplicationPackageBL.GetSaasApplicationList(excludeChildMenu);

        return toReturn;
    }

    [HttpGet]
    public OperationCallResult<bool> ImportApplicationFromHostDBToCurrentUserDB(int? packageMenuId)
    {
        if (packageMenuId.HasValue)
        {
            return AppApplicationImportBL.ImportApplicationFromHostDBToCurrentUserDB(packageMenuId.Value);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<bool> DeleteOneApplicationPackage(int? packageMenuId)
    {
        return AppSaasUserApplicationPackageBL.DeleteOneApplicationPackage(packageMenuId);
    }


    [HttpPost]
    public OperationCallResult<int?> CreateMyNewApplicationPackage(AppListMenuExDto menuDto)
    {
        return AppSaasUserApplicationPackageBL.CreateMyNewApplicationPackage(menuDto);
    }


    [HttpGet]
    public List<AppApplicationAssetsItemExDto> RetrieveAppApplicationAssetsItemDtoListByType(int? applicationId, int? emAppApplicationAssetsType)
    {
        if (applicationId.HasValue)
        {
            return AppSaasUserApplicationPackageBL.RetrieveAppApplicationAssetsItemDtoListByType(applicationId.Value, emAppApplicationAssetsType);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppApplicationAssetsItemExDto> SaveAppApplicationAssetsItemDtoList(AppApplicationAssetsItemSetDto setDto)
    {
        if (setDto != null && setDto.AppApplicationAssetsItemExDtoSet != null && setDto.ApplicationId.HasValue && setDto.EmAppApplicationAssetsType.HasValue)
        {
            return AppSaasUserApplicationPackageBL.SaveAppApplicationAssetsItemDtoList(
                setDto.AppApplicationAssetsItemExDtoSet, setDto.ApplicationId.Value, setDto.EmAppApplicationAssetsType.Value);
        }

        return null;
    }


    [HttpGet]
    public AppApplicationDataManipulationDto RetrieveAppApplicationDataManipulations(int? applicationId)
    {
        if (applicationId.HasValue)
        {
            return AppSaasUserApplicationPackageBL.RetrieveAppApplicationDataManipulations(applicationId.Value);
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<AppListMenuExDto> SaveOneSaasApplicationSetting(AppListMenuExDto appListMenuExDto)
    {
        if (appListMenuExDto != null)
        {
            OperationCallResult<AppListMenuExDto> toReturn = AppSaasUserApplicationPackageBL.SaveOneSaasApplicationSetting(appListMenuExDto);

            return toReturn;
        }

        return null;
    }


    [HttpGet]
    public bool TouchServer() //for administrato
    {
        return AppSecurityUserSessionBL.TouchServer(null);
    }


    [HttpPost]
    public OperationCallResult<AppSecurityUserInvitationExDto> InviteBusinessPaternerUser(AppSecurityUserInvitationExDto userInvitationExDto)
    {
        if (userInvitationExDto != null)
        {
            OperationCallResult<AppSecurityUserInvitationExDto> toReturn = AppSaasAccountUserBL.CreateSaasBusinessPaternerUserInvitation(userInvitationExDto);
            return toReturn;
        }

        return null;
    }


    [HttpPost]
    public OperationCallResult<object> SendUserAccountUnlockEmail(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        if (aAppSecurityUserExDto != null)
        {
            if (aAppSecurityUserExDto.RedirectToEsiteId.HasValue)
            {
                AppEsiteExDto esiteExDto = AppEsiteConfigBL.RetrieveOneAppEsiteExDto(aAppSecurityUserExDto.RedirectToEsiteId.Value);
                if (esiteExDto != null)
                {
                    esiteExDto.SitePublishedLoginUrl = esiteExDto.Description;
                    if (!string.IsNullOrWhiteSpace(esiteExDto.SitePublishedLoginUrl))
                    {
                        aAppSecurityUserExDto.PostEmailActivationRedirectUrl = esiteExDto.SitePublishedLoginUrl;
                    }
                }
            }

            return AppSaasAccountUserBL.SendUserAccountUnlockEmail(aAppSecurityUserExDto);
        }

        return null;
    }


    [HttpGet]
    public int? GetOrCreateUserFileFolderId(int? userId)
    {
        return AppSecurityUserBL.GetOrCreateUserFileFolderId(userId);
    }

    [HttpGet]
    public AppSecurityUserExDto GetUserInfoWithAutoGenerateFileFolderId(int? userId)
    {
        return AppSecurityUserBL.GetUserInfoWithAutoGenerateFileFolderId(userId);
    }


    [HttpPost]
    public OperationCallResult<bool> AddOnePageToUserBookmark(AppListMenuDto menuDto)
    {
        if (menuDto != null)
        {
            OperationCallResult<bool> aOperationCallResult = AppDesktopBL.AddOnePageToUserBookmark(menuDto);
            return aOperationCallResult;
        }

        return null;
    }

    #endregion
}
