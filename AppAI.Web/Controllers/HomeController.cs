using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Globalization;
using APP.Framework.Validation;
using App.BL;
using AppWeb.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[ApiController]
[Route("webapi/[controller]/[action]")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public UserContext Login()
    {
        var authrizationHeader = HttpContext.Request.Headers["Authorization"];

        // it is basical 64 bit encodein

        AppSecurityUserDto userDto = LoginModel.GetUserInfoFromRequestHeader(authrizationHeader);

        UserContext aUserContext = AuthenticateUserLogin(userDto);


        return aUserContext;
    }

    [HttpPost]
    public UserContext MgtLogin()
    {
        var authrizationHeader = HttpContext.Request.Headers["Authorization"];

        // it is basical 64 bit encodein

        AppSecurityUserDto userDto = LoginModel.GetUserInfoFromRequestHeader(authrizationHeader);


        UserContext aUserContext = new UserContext();
        aUserContext.LoginFailedErroMessage = "Cannot find your  Account";
        aUserContext.IsLoginFailed = true;

        try
        {
            // need to ask XH
            aUserContext = AppSecurityAuthenticationBL.Authenticate(userDto.LoginName, userDto.Password);

            if (aUserContext.IsLoginFailed)
            {
                return aUserContext;
            }
            else
            {
                Response.Cookies.Append(ServerContext.CurrentUserSessionIdToken, aUserContext.SessionId.ToString());
                return aUserContext;
            }

        }
        catch (Exception ex)
        {
            aUserContext.LoginFailedErroMessage = ex.ToString().Replace("\r\n", " | ").Replace("\n", " | ");
            return aUserContext;
        }


        return aUserContext;
    }



    //   userDto.LoginName = userName;
    // userDto.Password = password;

    [HttpPost]
    public UserContext LoginWithDto(AppSecurityUserDto userDto)
    {
        UserContext aUserContext = AuthenticateUserLogin(userDto);

        return aUserContext;
    }


    [HttpGet]
    public UserContext Logout(string sessionId) //for administrato
    {
        UserContext aUserContext = new UserContext();
        AppSecurityUserSessionBL.DeleteAppSecurityUserSession(sessionId);
        aUserContext.IsLoginFailed = true;
        aUserContext.IsFirstTimeLogin = false;

        return aUserContext;

    }


    [HttpGet]
    public double CheckCurrenSessionIsExsit(string sessionId) //for administrato
    {
        UserContext aUserContext = new UserContext();
        return AppSecurityUserSessionBL.CheckCurrenSessionIsExsit(sessionId);
    }

    /// <summary>
    /// Returns the login page background image URL list for the current company (no auth required).
    /// Used by the login page to display company-configured background, same as Angular MgtLogin.
    /// </summary>
    [HttpGet]
    public List<string> GetLoginPageBackgroundImageUrlList()
    {
        try
        {
            if (ServerContext.Instance?.CompanySettings?.CompanyId != null)
            {
                return AppCompanyBL.GetCurrentCompanyBackgroundImageUrlList();
            }
        }
        catch { }
        return new List<string>();
    }

    [HttpGet]
    public UserContext GetUserContextBySessionId(string sessionId) //for Test
    {
        if (!string.IsNullOrEmpty(sessionId) && sessionId.Contains(","))
        {
            int index = sessionId.IndexOf(",");
            sessionId = sessionId.Substring(0, index);
        }

        UserContext aUserContext = AppSecurityUserBL.GetUserContextBySessionId(sessionId);

        aUserContext.UserId = null;
        // aUserContext.DomainId = null;
        // dont/ get

        if (aUserContext != null)
        {
            Dictionary<string, Dictionary<string, int>> dictEnumApp = GetJsClientEnum();

            aUserContext.DictEnumApp = dictEnumApp;
            return aUserContext;

        }
        else
        {
            return null;
        }

    }

    [HttpGet]
    public static SearchDto RetrieveOnePublicSearchDto(string searchId)
    {
        SearchDto toReturnSearchDto = null;

        var appSearchEntity = AppSearchConfigBL.RetrieveOneAppSearchEntity(searchId);

        toReturnSearchDto = AppSearchConfigBL.ConvertSearchEntitySearchDto(appSearchEntity);


        if (toReturnSearchDto != null)
        {
            if (toReturnSearchDto.WhereUsedSearchId.HasValue)
            {
                toReturnSearchDto.EmbeddedChildSearchDto = AppSearchBL.RetrieveOneSearchDto(toReturnSearchDto.WhereUsedSearchId.Value, false, false);
            }

            AppCascadingSearchBL.SetupIntialCscadingSearchCretiaDataSource(toReturnSearchDto, false);
        }


        return toReturnSearchDto;

    }

    private static void SetupOneEnumType(Type type, Dictionary<string, Dictionary<string, int>> dictEnumApp)
    {
        if (type.IsEnum)
        {
            Dictionary<string, int> dictEnum = new Dictionary<string, int>();

            foreach (var emAppType in Enum.GetValues(type))
            {
                dictEnum.Add(emAppType.ToString(), (int)emAppType);
            }

            dictEnumApp.Add(type.ToString().Replace("APP.Components.Dto.", ""), dictEnum);

        }
    }

    [HttpGet]
    public UserContext RetrievePassword() //for administrato
    {
        string retrievePasswordHeader = HttpContext.Request.Headers["RetrievePassword"];

        // it is basical 64 bit encodein
        if (retrievePasswordHeader != null)
        {
            //Extract credentials
            string encodedUsernamePassword = retrievePasswordHeader.Trim();

            Encoding encoding = Encoding.GetEncoding("UTF-8");
            string usernameEmail = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

            int seperatorIndex = usernameEmail.IndexOf(':');

            //string userName, string password
            string userName = usernameEmail.Substring(0, seperatorIndex);
            string email = usernameEmail.Substring(seperatorIndex + 1);

            return AppSecurityUserBL.SendUserNameAndPassword(userName, email);
        }
        else
        {
            return new UserContext();
        }
    }


    internal static UserContext AuthenticateUserLogin(AppSecurityUserDto userDto)
    {
        UserContext aUserContext = new UserContext();
        aUserContext.LoginFailedErroMessage = "Cannot find your  Account";
        aUserContext.IsLoginFailed = true;

        try
        {
            // need to ask XH
            aUserContext = AppSecurityAuthenticationBL.AuthenticateEStore(userDto.LoginName, userDto.Password);

            if (aUserContext.IsLoginFailed)
            {
                return aUserContext;
            }
            else
            {
                // Note: Cookie setting handled via response in ASP.NET Core
                // Response.Cookies.Append(HttpAuthenticationHelp.CurrentUserSessionId, aUserContext.SessionId.ToString());
                return aUserContext;
            }

        }
        catch (Exception ex)
        {
            return aUserContext;
        }
    }


    internal static Dictionary<string, Dictionary<string, int>> GetJsClientEnum()
    {
        var dictEnumApp = new Dictionary<string, Dictionary<string, int>>();


        Type type = typeof(EmAppWorkflowConditionType);
        SetupOneEnumType(type, dictEnumApp);



        type = typeof(EmBLFiledMappingSystemTokenField);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkFlowActionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionCommandType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkflowTaskField);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEntityLookupInfoCode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEntitySearchInfoCode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmTransactionOrganizedType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetActionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSearchViewLinkTargetActionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionUnitLinkTargetActionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEntityType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFormCreationFrom);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFormLayoutType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppAggregationFunctionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFormularType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFormularFunctionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLeadFunctionType);
        SetupOneEnumType(type, dictEnumApp);


        type = typeof(EmAppCriteriaOperatorType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppControlType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDataType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppImportLanguageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppUserType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetSourceType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmSystemSettings);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmTenantSettings);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppViewType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDateTimeProperties);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDashboardWidgetItemType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppListMenuLinkType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkedSearchAction);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkedSearchUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCascadingSourceType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppExternalSourceFrom);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMenuRegisterType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDataServiceType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDataSetUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSecuritySysObjType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSearchUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApplicationSettingValueType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCriteriaType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppAdminTheme);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppClientTheme);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTaskDurationUnit);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectDirection);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLanguageKeyType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApplicationSettingCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmOrgClassificationLevel);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppChartViewType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectWorkflowType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkflowTaskStageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkflowTaskStageStatus);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkflowDiagramShapeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarRecurringType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMonth);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDayOfWeek);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarWorkState);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppGroupUsage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEntityDatasourceType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMessgaeScopeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMessgaePostType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppViewColumnType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDateTimeTokenType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDocumentType);
        SetupOneEnumType(type, dictEnumApp);


        type = typeof(EmAppAuthenticationMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSysTransactionActionCode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSysTransactionUnitActionCode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDataServerType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFileFolderCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransBusinessType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransNavigationType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFolderSearchOption);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFileNotificationType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppBuseinssScope);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppGanttDisplayUnit);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectPrivacy);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectPostPrivacy);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectDisplayLayoutType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectCostType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskPriority);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskStatus);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskChangeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectStage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskStage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectTaskProgress);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTaskSystemDefinedCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmApprTaskViewOption);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTaskOwnerDeliverPhase);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppReportEngineType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppPivotAggregationType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppExportExcelType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWijmoPivotAggregationType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppNotificationMessageUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMessageScanPeriod);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMessagePlaceHolderToken);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionCommandSystemParameterToken);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppGrandChildEditMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDatePickerSearchViewField);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDeviceMenuShowMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarDateDefineType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarDateRangeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarViewEventType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarViewEventCompletStage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMassUpdateViewType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSearchCriteriaTokenType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCanlendarMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSearchFieldSubControlType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCriteriaSubType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetSourceColumnType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmInternalCodeRegistration);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmInternalCodeRegistrationForESiteOrderList);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmInternalCodeRegistrationForGoogleMapView);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppUserContactType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppProjectPrivilege);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetSystemDefinedPage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTaskDueDateType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppUserCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppFormLayoutItemType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionUnitLevel);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApplicationAssetsType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApplicationBuilderSection);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSaasTableFilterOption);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSearchViewGridOutputMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionDataTransferType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionGridDisplayType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppBarCodeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMobileTopMenuUploadButotnUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppBuiltInUserGroup);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSaasUserAvailableCompanyType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetPageLayoutMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppListSubMenuDisplayType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWarningHighlightPriority);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEStoreTheme);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEsiteLayoutType);
        SetupOneEnumType(type, dictEnumApp);


        type = typeof(EmAppShoppingBagFieldType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmEstoreLayoutView);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppESiteApplicationType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWebsitePageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppExternalLoginType);
        SetupOneEnumType(type, dictEnumApp);


        type = typeof(EmAppWebPageContainerControlDisplayType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWebPageUiControlDisplayType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWebPageSearchViewDisplayType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarRepeatSimpleSetting);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarRepeatTimeUnit);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarRepeatEndType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCalendarRepeatSettingApplyToRange);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppMenuItemCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEsitePageCategory);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppYesNo);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWebsiteComponentType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWebsiteComponentSubPartType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransFieldExpressionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransUnitExpressionType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppEsiteThirdPartControl);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionCrudType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppJsonNodeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSecurityGroupInernalCode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDesktopLayoutType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionTemplateItemType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWijmoOperator);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppImportStatus);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppSchemaDataSetMappingNodeProcessMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppIntergrationSettingParameterUsageType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppModuleConfigTable);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApiPayloadDataType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDbToDbImportSourceType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppBuiltInQueryType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppBatchCommandSourceFrom);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppValidationResultPreference);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCommandLoggingPreference);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppTransactionScopeUsage);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppLinkTargetApplyToRowRangeType);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppDataFieldStoreMode);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppWorkflowProgressStatus);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppCommandProgressStatus);
        SetupOneEnumType(type, dictEnumApp);

        type = typeof(EmAppApiSystemEnvironmentVariable);
        SetupOneEnumType(type, dictEnumApp);


        return dictEnumApp;
    }
}
