using System.Collections.Generic;
using System.Linq;
using APP.Components.Dto;
using APP.Components.EntityDto;
using App.BL;
using APP.Framework.Collections;
using APP.Framework.Communication;
using Microsoft.AspNetCore.Mvc;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class DashBoardController : SecureBaseController
{
    [HttpGet]
    public IEnumerable<AppDesktopDto> RetrieveAllAppDesktopDto(bool? isIncludeUserDefaultDesktop)
    {
        isIncludeUserDefaultDesktop = isIncludeUserDefaultDesktop.HasValue ? isIncludeUserDefaultDesktop.Value : false;
        return AppDesktopBL.RetrieveAllAppDesktopDto(isIncludeUserDefaultDesktop.Value);
    }

    [HttpGet]
    public IEnumerable<AppDesktopDto> RetrieveSaasDesktopDtoList(int? applicationId)
    {
        return AppDesktopBL.RetrieveSaasDesktopDtoList(applicationId);
    }

    [HttpGet]
    public IEnumerable<AppDesktopDto> RetrieveCurrnetUserDashboardList()
    {
        return AppSecurityManagementBL.RetrieveCurrnetUserDesktopDto();
    }

    [HttpGet]
    public List<AppDesktopDto> RetrieveDesktopDtoListByUserId(int? userId)
    {
        if (userId.HasValue)
        {
            return AppSecurityManagementBL.RetrieveDesktopDtoListByUserId(userId.Value);
        }

        return new List<AppDesktopDto>();
    }

    [HttpGet]
    public List<AppDesktopDto> RetrieveDesktopDtoListByRoleId(int? roleId)
    {
        if (roleId.HasValue)
        {
            return AppSecurityManagementBL.RetrieveDesktopDtoListByRoleId(roleId.Value);
        }

        return new List<AppDesktopDto>();
    }

    [HttpGet]
    public List<AppDesktopDto> GetOrganizationDesktopListByOrganizationId(int? organizationId)
    {
        if (organizationId.HasValue)
        {
            return AppSecurityManagementBL.GetOrganizationDesktopListByOrganizationId(organizationId.Value);
        }

        return new List<AppDesktopDto>();
    }

    [HttpGet]
    public AppDesktopDto RetrieveOneAppDesktopExDto(int desktopId)
    {
        AppDesktopExDto toReturn = AppDesktopBL.RetrieveOneAppDesktopExDto(desktopId);
        return toReturn;
    }

    [HttpPost]
    public OperationCallResult<AppDesktopExDto> SaveDesktop(AppDesktopExDto aAppDesktopExDto)
    {
        if (aAppDesktopExDto != null)
        {
            if (aAppDesktopExDto.DictDeletedItemsIds != null)
            {
                aAppDesktopExDto.AppDesktopItemList.DeletedItemIds = aAppDesktopExDto.DictDeletedItemsIds.FirstOrDefault().Value;
            }
            return AppDesktopBL.SaveAppDesktopExDto(aAppDesktopExDto);
        }

        return null;
    }

    [HttpGet]
    public OperationCallResult<AppDesktopExDto> SaveAsAppDesktopExDto(int? desktopId)
    {
        if (desktopId.HasValue)
        {
            var toReturn = AppDesktopBL.SaveAsAppDesktopExDto(desktopId.Value);
            return toReturn;
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<object> DeleteOneAppDesktop(int desktopId)
    {
        var toReturn = AppDesktopBL.DeleteOneAppDesktop(desktopId);
        return toReturn;
    }

    [HttpGet]
    public OperationCallResult<AppDesktopExDto> CreateUserDefaultDesktop()
    {
        var toReturn = AppDesktopBL.CreateUserDefaultDesktop();
        return toReturn;
    }

    [HttpGet]
    public OperationCallResult<object> SetUserDashboardAsDefault(int? desktopId)
    {
        if (desktopId.HasValue)
        {
            var toReturn = AppDesktopBL.SetUserDashboardAsDefault(desktopId.Value);
            return toReturn;
        }

        return null;
    }
}
