using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using App.BL;
using APP.BL.AppMgr;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.Framework.Collections;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using AppWeb.Models;
using AppWebPluin.Models;
using ExchangeBL;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AppAI.Web.Controllers;

// no need to control security !

[ApiController]
[Route("webapi/[controller]/[action]")]
public class ExternalUserRegistrationController : ControllerBase
{
    [HttpGet]
    public UserContext ESiteLogin()
    {
        string authrizationHeader = HttpContext.Request.Headers["Authorization"];

        // it is basical 64 bit encodein

        //MvcApplication.Logger.Warn("ESiteLogin From IP" +  GetClientIp(Request));

        AppSecurityUserDto userDto = LoginModel.GetUserInfoFromRequestHeader(authrizationHeader);

        // Timezone Offset "UTC +8:45"

        UserContext aUserContext = HomeController.AuthenticateUserLogin(userDto);

        if (!aUserContext.IsLoginFailed)
        {
            string msTimetoken = GetMsTimeTokenFromRequestHeader();
            string msTimeZoneShortName = GetMsTimeZoneShortDisplayNameFromRequestHeader(msTimetoken);
            string timeZoneOffset = HttpContext.Request.Headers["TimeZoneOffset"];

            AppSaasUserSessionMgtBL.UpdateUserTimeZoneWithSession(aUserContext, msTimetoken, msTimeZoneShortName, timeZoneOffset);
        }

        return aUserContext;
    }

    private string GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    [HttpGet]
    public UserContext LinkExistMasterDBUserToESite(string token)
    {
        return null;
    }

    [HttpPost]
    public OperationCallResult<UserContext> ESiteUserRegistration(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        OperationCallResult<UserContext> toReturn = new OperationCallResult<UserContext>();

        string orgPassword = aAppSecurityUserExDto.Password;

        string msTimetoken = GetMsTimeTokenFromRequestHeader();

        if (!string.IsNullOrWhiteSpace(msTimetoken))
        {
            aAppSecurityUserExDto.TimeZoneInfoToken = msTimetoken;
        }

        // need to addmessge template
        SetupRegistgrationMessageTemplate(aAppSecurityUserExDto);

        OperationCallResult<AppSecurityUserExDto> regResult = AppEsiteBL.EStoreUserRegistration(aAppSecurityUserExDto);

        if (regResult.IsSuccessfulWithResult)
        {
            if (aAppSecurityUserExDto.IsNeedActivePartnerUserByEmail)
            {
                toReturn.ValidationResult.AddItem(null, "loginError", ValidationItemType.Message, "Please check your email to activate your account.");
                UserContext aUserContext = new UserContext();
                aUserContext.IsLoginFailed = true;
                aUserContext.IsWaitingForEmailActivation = true;
                aUserContext.LoginFailedErroMessage = "Please check your email to activate your account.";
                toReturn.Object = aUserContext;
            }
            else
            {
                AppSecurityUserDto userDto = new AppSecurityUserDto();
                userDto.LoginName = aAppSecurityUserExDto.LoginName;
                userDto.Password = orgPassword;
                UserContext aUserContext = HomeController.AuthenticateUserLogin(userDto);

                if (aUserContext.IsLoginFailed)
                {
                    toReturn.ValidationResult.AddItem(null, "loginError", ValidationItemType.Error, aUserContext.LoginFailedErroMessage);
                }
                else
                {
                    toReturn.Object = aUserContext;
                }
            }
        }
        else
        {
            toReturn.ValidationResult = regResult.ValidationResult;
        }

        return toReturn;
    }

    private void SetupRegistgrationMessageTemplate(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        bool isWebAppCall = true;

        string clientAgentCallingFrom = HttpContext.Request.Headers["ClientAgentCallingFrom"];

        // specific device call
        if (!string.IsNullOrWhiteSpace(clientAgentCallingFrom))
        {
            int? callFronInt = ControlTypeValueConverter.ConvertValueToInt(clientAgentCallingFrom);
            if (callFronInt.Value == (int)EmClientAgentCallingFrom.MobileApp)
            {
                isWebAppCall = false;
            }
        }

        // EsiteId

        int? ESiteId = ControlTypeValueConverter.ConvertValueToInt(HttpContext.Request.Headers["ESiteId"]);
        var esiteEntity = AppEsiteConfigBL.RetrieveOneSimpleAppEsiteEntity(ESiteId);

        // Domain Type

        int userType = aAppSecurityUserExDto.DomainId;

        if (userType == (int)EmAppUserType.Customer)
        {
            aAppSecurityUserExDto.MessageTempalteId = ControlTypeValueConverter.ConvertValueToInt(esiteEntity.VisaPaymentApiParam1);
        }
        else if (userType == (int)EmAppUserType.Supplier)
        {
            aAppSecurityUserExDto.MessageTempalteId = ControlTypeValueConverter.ConvertValueToInt(esiteEntity.VisaPaymentApiParam2);
        }
        else if (userType == (int)EmAppUserType.ClientAgent)
        {
            // To Do Agent
        }
        else if (userType == (int)EmAppUserType.SupplierAgent)
        {
            // To Do Agent
        }

        if (isWebAppCall) // Website Redirect Url After User Activation
        {
            //https://www.fitconciergeapp.com/?ExternalCallingFrom=StaticSite&RouteCode=SignIn#

            //aAppSecurityUserExDto.PostEmailActivationRedirectUrl = esiteEntity.VisaPaymentApiParam3;
        }
        else // Mobile Redirect Url After User Activation
        {
            //https://www.fitconciergeapp.com/mgt/MobileDeeplink.html?DeepLinkType=login&

            //aAppSecurityUserExDto.PostEmailActivationRedirectUrl = esiteEntity.VisaPaymentApiParam4;
        }
    }

    [HttpPost]
    public OperationCallResult<bool> ESiteUserQuickRegistration(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        OperationCallResult<bool> toReturn = new OperationCallResult<bool>();

        aAppSecurityUserExDto.LoginName = aAppSecurityUserExDto.Email;
        aAppSecurityUserExDto.Password = "nopassword";
        aAppSecurityUserExDto.ConfirmPassword = "nopassword";
        aAppSecurityUserExDto.IsQuickRegistration = true;
        aAppSecurityUserExDto.IsNeedActivePartnerUserByEmail = false;

        string msTimetoken = GetMsTimeTokenFromRequestHeader();

        if (!string.IsNullOrWhiteSpace(msTimetoken))
        {
            aAppSecurityUserExDto.TimeZoneInfoToken = msTimetoken;
        }

        OperationCallResult<AppSecurityUserExDto> regResult = AppEsiteBL.EStoreUserRegistration(aAppSecurityUserExDto);

        if (regResult.IsSuccessfulWithResult)
        {
            if (aAppSecurityUserExDto.IsNeedActivePartnerUserByEmail)
            {
                toReturn.ValidationResult.AddItem(null, "loginError", ValidationItemType.Message, "Please check your mail to activate your account.");
            }
            else
            {
                toReturn.ValidationResult.Items.Add(new ValidationItem(typeof(AppSecurityUserEntity), "app_SecurityUserEntity_Reg_Ok", ValidationItemType.Message, "Sign Up Successful"));
                toReturn.Object = true;
            }
        }
        else
        {
            toReturn.ValidationResult = regResult.ValidationResult;
        }

        return toReturn;
    }

    [HttpPost]
    public OperationCallResult<UserContext> ESitePartnerUserThirdPartAccountLoginPostProcess(ExternalSignInTokenDto inputTokenDto)
    {
        OperationCallResult<UserContext> toReturn = new OperationCallResult<UserContext>();

        string userEmail = string.Empty;

        userEmail = GetUserEmailFromExternalSignInToken(inputTokenDto);

        string msTimetoken = GetMsTimeTokenFromRequestHeader();
        string msTimeZoneShortName = GetMsTimeZoneShortDisplayNameFromRequestHeader(msTimetoken);
        string timeZoneOffset = HttpContext.Request.Headers["TimeZoneOffset"];

        if (!string.IsNullOrWhiteSpace(msTimetoken))
        {
            inputTokenDto.TimeZoneInfoToken = msTimetoken;
        }

        OperationCallResult<AppSecurityUserExDto> regResult = AppEsiteBL.ESitePartnerUserThirdPartAccountLoginPostProcess(userEmail, inputTokenDto.NewUserPartnerType, inputTokenDto.RegisterFromEsiteId, inputTokenDto.PostEmailActivationRedirectUrl, inputTokenDto.TimeZoneInfoToken);

        if (regResult.IsSuccessfulWithResult)
        {
            AppSecurityUserDto fromDbUserDto = regResult.Object;
            fromDbUserDto.ExternalAcessToken = inputTokenDto.ExternalAcessToken;
            fromDbUserDto.EmExternalSigninType = inputTokenDto.EmExternalSigninType;

            UserContext aUserContext = AppSecurityAuthenticationBL.CreateUserContextAndSessionFromExistUserDto(fromDbUserDto);

            // Call extenal API

            if (aUserContext.IsLoginFailed)
            {
                toReturn.ValidationResult.AddItem(null, "loginError", ValidationItemType.Error, aUserContext.LoginFailedErroMessage);
            }
            else
            {
                AppSaasUserSessionMgtBL.UpdateUserTimeZoneWithSession(aUserContext, msTimetoken, msTimeZoneShortName, timeZoneOffset);

                aUserContext.IsFirstTimeLogin = fromDbUserDto.IsNewCompanyUser;

                // Note: Cookie setting moved to ASP.NET Core middleware; kept here for compatibility
                Response.Cookies.Append(ServerContext.CurrentUserSessionIdToken, aUserContext.SessionId.ToString());

                toReturn.Object = aUserContext;
            }
        }
        else
        {
            toReturn.ValidationResult = regResult.ValidationResult;
        }

        return toReturn;
    }

    [HttpGet]
    public UserContext GetExternalUserContext()
    {
        // string AnymousTokenSesseion = "6601508d-e7e0-4ed6-892b-879c834676af";

        string eSiteId = HttpContext.Request.Headers["ESiteId"];

        string anoumousyUserSessionToken = AppEsiteConfigBL.GetSiteIdAnoymousToken(eSiteId);

        UserContext aUserContext = AppSecurityUserBL.GetUserContextBySessionId(anoumousyUserSessionToken);

        if (aUserContext != null)
        {
            Dictionary<string, Dictionary<string, int>> dictEnumApp = HomeController.GetJsClientEnum();

            aUserContext.DictEnumApp = dictEnumApp;
            aUserContext.UserId = null;
            //aUserContext.SessionId = null;
            return aUserContext;
        }
        else
        {
            return null;
        }
    }

    [HttpGet]
    public ObservableSet<AppListMenuExDto> RetrieveNoneMgtUserTreeMenu(int? siteId, int? siteMenuCategory) //for current user
    {
        ObservableSet<AppListMenuExDto> userMenuList = new ObservableSet<AppListMenuExDto>();

        if (siteId.HasValue && siteMenuCategory.HasValue &&
            (siteMenuCategory.Value == (int)EmAppMenuItemCategory.ClientPage
            || siteMenuCategory.Value == (int)EmAppMenuItemCategory.PublicPage
            || siteMenuCategory.Value == (int)EmAppMenuItemCategory.SupplierPage
            ))
        {
            userMenuList = AppTreeListMenuBL.RetrieveNoneMgtUserTreeMenu(siteId, siteMenuCategory);
        }

        return userMenuList;
    }

    [HttpPost]
    public OperationCallResult<object> SendPublicMessage(AppMessageDto aAppMessageDto)
    {
        return AppMessageBL.SaveOneAppMessageDto(aAppMessageDto);
    }

    [HttpPost]
    public OperationCallResult<object> SendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail(ExternalSignInTokenDto inputTokenDto)
    {
        string loginNameOrEmail = inputTokenDto.UserEmail;
        string postEmailActivationRedirectUrl = inputTokenDto.PostEmailActivationRedirectUrl;
        string logoImgUrl = inputTokenDto.LogoImgUrl;
        string messageTempalte = inputTokenDto.MessageTempalte;
        int? callinfFrom = inputTokenDto.ClientAgentCallingFrom;

        return AppSaasAccountUserBL.SendEsiteUserPasswordRetrieveEmailByLoginNameOrEmail(loginNameOrEmail, postEmailActivationRedirectUrl, logoImgUrl, messageTempalte, callinfFrom);
    }

    [HttpGet]
    public AppTransactionStructureDto GetFormStructure(int? transactionId, int? transGroupId)
    {
        return AppTransactionController.GetFormStructureMethod(transactionId);
    }

    [HttpGet]
    public AppMasterDetailDto GetNewFormData(int? transactionId, bool isConfigTestRun = false)
    {
        if (transactionId.HasValue)
        {
            AppMasterDetailDto aAppformDataDto = AppMasterDetailFormDataLoadBL.GetNewFormData(transactionId.Value, isConfigTestRun); // should be client time
            // To Do, Need to verify if need time convert
            return aAppformDataDto;
        }

        return null;
    }

    [HttpPost]
    public AppMasterDetailDto GetFormData(GetFormDataDto data)
    {
        return AppTransactionController.GetFormDataMethod(data.transactionId, data.rootPrimaryKeyValue, data.autoExecuteCommandId, data.selectDataRow);
    }

    [HttpGet]
    public AppListDataDto GetListEditFormData(int? transactionId)
    {
        if (transactionId.HasValue)
        {
            AppListDataDto aAppformDataDto = AppListEditFormDataLoadBL.GetListEditFormData(transactionId.Value);

            DataModelDateTimeConverterBL.ConvertListEditFromUtcToClient(aAppformDataDto);

            return aAppformDataDto;
        }

        return null;
    }

    [HttpPost]
    public OperationCallResult<AppMasterDetailDto> SaveTransactionData(AppMasterDetailDto appformDataDto)
    {
        if (appformDataDto != null)
        {
            DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(appformDataDto);

            var calculationResult = AppTransactionFormulaBL.ValidateAndCalculateMasterDetailTransactionData(appformDataDto);

            if (!calculationResult.IsSuccessfulWithResult)
            {
                return calculationResult;
            }
            else
            {
                if (appformDataDto != calculationResult.Object)
                {
                    appformDataDto = calculationResult.Object;
                }

                appformDataDto = DataModelDateTimeConverterBL.ConvertMasterDetailFromClientToUtc(appformDataDto);

                var saveResult = AppMasterDetailFormDataSaveBL.SaveTransactionData(appformDataDto);

                if (saveResult.Object != null)
                {
                    DataModelDateTimeConverterBL.ConvertMasterDetailFromUtcToClient(saveResult.Object);
                }

                if (calculationResult.ValidationResult.Items.FirstOrDefault(o => o.ItemType == ValidationItemType.Warning) != null)
                {
                    saveResult.ValidationResult.Merge(calculationResult.ValidationResult);

                    if (saveResult.Object != null && calculationResult.Object != null)
                    {
                        saveResult.Object.DictTransFieldIdAndWarningHighlightStyleId = calculationResult.Object.DictTransFieldIdAndWarningHighlightStyleId;
                    }
                }

                return saveResult;
            }
        }

        return null;
    }

    [HttpGet]
    public SearchDto RetrieveOneSearch(int searchId, bool? isSavedSearch)
    {
        return AppSearchController.RetrieveOneSearchMethod(searchId, isSavedSearch);
    }

    [HttpPost]
    public SearchResultDto RetrieveSearchResult(SearchDto searchDto)
    {
        return AppSearchController.RetrieveSearchResultMethod(searchDto);
    }

    [HttpPost]
    public IEnumerable<ReferenceViewDefinitionDto> RetrieveUserViewsBySearchDefinition(SearchDefinitionDto searchDefinition)
    {
        return AppSearchViewConfigBL.RetrieveUserViewsBySearchDefinition(searchDefinition);
    }

    [HttpPost]
    public dynamic RegisterUserDevice(dynamic sessionIsDeviceId)
    {
        if (sessionIsDeviceId != null)
        {
            if (sessionIsDeviceId.DeviceId != null && sessionIsDeviceId.CurrentUserSessionId != null)
            {
                try
                {
                    AppSaasUserSessionMgtBL.RegisterUserDevicdeIdWithSession(sessionIsDeviceId.CurrentUserSessionId.Value, sessionIsDeviceId.DeviceId.Value);

                    return new { IsSuccessful = true, Error = "" }; ;
                }
                catch (Exception ex)
                {
                    return new { IsSuccessful = false, Error = "Cannot find the match user" }; ;
                }
            }
        }

        return new { IsSuccessful = false, Error = "Cannot find the match user" }; ;
    }

    [HttpPost]
    public dynamic PasswordRecovery(dynamic dynUserPasswordChange)
    {
        string parameter = dynUserPasswordChange.ServiceName;
        string password = dynUserPasswordChange.Password;
        string confirmPassword = dynUserPasswordChange.ConfirmPassword;

        dynamic retrunObject = new ExpandoObject();
        retrunObject.IsSuccessful = true;
        retrunObject.ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            retrunObject.IsSuccessful = false;
            retrunObject.ErrorMessage = "Pasword cannot be empty";
        }
        else if (password != confirmPassword)
        {
            retrunObject.IsSuccessful = false;
            retrunObject.ErrorMessage = "Pasword does not match confirm password";
        }
        else
        {
            //0:action, 1:userguid,2:dateTicker, 3: redreictUrl
            string[] paramArray = AppSaasAccountUserBL.DecryptParamString(parameter);

            string userGuidStr = paramArray[1].Trim();
            string sentDateTicksStr = paramArray[2].Trim();

            Guid userGuid = new Guid(userGuidStr);
            long? sentDateTicks = ControlTypeValueConverter.ConvertValueToLong(sentDateTicksStr);

            long diffTicks = DateTime.UtcNow.Ticks - sentDateTicks.Value;
            long ticksOneHour = 60 * 60 * (long)10000000;
            bool isNotExpired = diffTicks >= 0 && diffTicks <= ticksOneHour * 24;

            if (isNotExpired)
            {
                HttpBasicAuthenticator.RegisterSysTemAgentWebUserIdentity();

                AppSecurityUserExDto appSecurityUserExDto = AppSaasAccountUserBL.RetrieveOneAppSecurityUserExDtoByGuid(userGuid);
                appSecurityUserExDto.Password = password;
                appSecurityUserExDto.ConfirmPassword = confirmPassword;
                var result = AppSecurityUserBL.UpdateMasterDBUserLoginInfo(appSecurityUserExDto);
                if (!result.ValidationResult.HasErrors)
                {
                    retrunObject.IsSuccessful = true;
                    retrunObject.ErrorMessage = "";
                }
                else
                {
                    retrunObject.IsSuccessful = false; ;
                    retrunObject.ErrorMessage = "Password recovery get failed";
                }
            }
            else
            {
                retrunObject.IsSuccessful = false;
                retrunObject.ErrorMessage = "Password recovery get expired";
            }
        }

        return retrunObject;
    }

    // commandId|transRId|transFieldId1_Value1|transFieldId2_Value2|transFieldId3_Value3
    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> ExternalLinkServiceCall(string parameterStr)
    {
        string[] paramArray = AppSaasAccountUserBL.DecryptParamString(parameterStr);

        if (paramArray != null && paramArray.Length >= 2)
        {
            int? commandId = ControlTypeValueConverter.ConvertValueToInt(paramArray[0]);

            string transactionRId = paramArray[1];

            List<string> transFieldIdValueParameterStrList = paramArray.ToList().GetRange(2, paramArray.Length - 2);

            if (commandId.HasValue && !string.IsNullOrWhiteSpace(transactionRId))
            {
                return AppTransactionCommandBL.ExecuteCommandFromTransFieldIdValueParameters(commandId.Value, transactionRId, transFieldIdValueParameterStrList);
            }
        }

        return null;
    }

    [HttpGet]
    public string EncryptString(string paramString, string token = "")
    {
        string toReturn = EnDeCrypt.Encrypt(paramString, token);
        return toReturn;
    }

    [HttpGet]
    public string DecryptString(string paramString, string token = "")
    {
        string toReturn = EnDeCrypt.Decrypt(paramString, token);
        return toReturn;
    }

    [HttpPost]
    public OperationCallResult<AppSecurityUserExDto> RegisterNewSaasAccountWithUserCompanyDB(AppSecurityUserExDto aAppSecurityUserExDto)
    {
        OperationCallResult<AppSecurityUserExDto> toReturn = new OperationCallResult<AppSecurityUserExDto>();

        string msTimetoken = GetMsTimeTokenFromRequestHeader();

        if (!string.IsNullOrWhiteSpace(msTimetoken))
        {
            aAppSecurityUserExDto.TimeZoneInfoToken = msTimetoken;
        }

        OperationCallResult<AppSecurityUserExDto> regResult = AppSaasAccountUserBL.RegisterNewSaasAccountWithUserCompanyDB(aAppSecurityUserExDto);

        toReturn.ValidationResult = regResult.ValidationResult;

        return toReturn;
    }

    [HttpGet]
    public OperationCallResult<AppSecurityUserExDto> ConfirmNewSaasAccountEmailAndCreateUserCompanyDB(string userGuid)
    {
        Guid guid = new Guid(userGuid);
        OperationCallResult<AppSecurityUserExDto> regResult = AppSaasAccountUserBL.ConfirmNewSaasAccountEmailAndCreateUserCompanyDB(guid);

        return regResult;
    }

    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExcuteTransactionCommonad(AppMasterDetailDto aformData)
    {
        if (aformData != null)
        {
            if (aformData != null && aformData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
                OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(aformData);

                return actionResult;
            }
        }
        return null;
    }

    [HttpPost]
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteWorkflowRootCommonad(AppMasterDetailDto aformData)
    {
        if (aformData != null)
        {
            if (aformData != null && aformData.TransactionCommandId.HasValue)
            {
                // Input: Client DateTime
                // Output: Client DateTime

                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(aformData);
                OperationCallResult<TransactionCommandActionResultDto> actionResult = AppTransactionCommandBL.ExecuteWorkflowRootCommand(aformData);

                return actionResult;
            }
        }
        return null;
    }

    [HttpGet]
    public OperationCallResult<TransactionCommandActionResultDto> ExecuteOneTransactionCommonadById(int? commandId, int? transactionId, string rootPrimaryKeyValue, int? chlldUnitId = null, string childRowPkCombString = null)
    {
        if (commandId.HasValue && transactionId.HasValue)
        {
            return AppTransactionCommandBL.ExecuteOneRootCommonadById(commandId.Value, transactionId.Value, rootPrimaryKeyValue, chlldUnitId, childRowPkCombString);
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

    [HttpPost]
    public async Task<AppStripeCheckOutDto> CreateStripeCheckoutSession(AppStripeCheckOutDto checkOutDto)
    {
        if (checkOutDto != null)
        {
            try
            {
                return await AppStripePaymentBL.CreateStripeCheckoutSession(checkOutDto);
            }
            catch (Exception ex)
            {
                checkOutDto.ErrorMessage = ex.Message;
                return checkOutDto;
            }
        }

        return null;
    }

    [HttpGet]
    public string RetrieveStripeCheckoutSession(string sessionId)
    {
        return AppStripePaymentBL.RetrieveStripeCheckoutSession(sessionId);
    }

    [HttpPost]
    public AppMasterDetailDto GetRootUnitFieldTriggerCascadingDataSource(object rootAppformDataDto)
    {
        var aformData = JsonConvert.DeserializeObject<AppMasterDetailDto>(rootAppformDataDto.ToString());

        if (aformData != null)
        {
            return AppCascadingBL.GetRootUnitFieldTriggerCascadingDataSource(aformData);
        }

        return null;
    }

    private static string GetUserEmailFromExternalSignInToken(ExternalSignInTokenDto inputTokenDto)
    {
        string userEmail = string.Empty;

        if (inputTokenDto.EmExternalSigninType.HasValue)
        {
            EmAppExternalLoginType signInType = (EmAppExternalLoginType)inputTokenDto.EmExternalSigninType.Value;
            string externalAceeeToken = inputTokenDto.ExternalAcessToken;

            userEmail = ExternalAccessTokeParseBL.GetEmailFromExternalToken(signInType, externalAceeeToken);
        }

        return userEmail;
    }

    //TODO need to add External Serch, exteranl Form?/

    private string GetMsTimeTokenFromRequestHeader()
    {
        string timeZoneName = HttpContext.Request.Headers["TimeZoneName"];
        // or
        string abbreviation = HttpContext.Request.Headers["TimeZoneAbbreviation"];
        string timeZoneOffset = HttpContext.Request.Headers["TimeZoneOffset"];

        string msTimetoken = "";

        if (!string.IsNullOrWhiteSpace(timeZoneName))
        {
            if (AppTimeZoneBL.DictTimeZoneFullNameKey.ContainsKey(timeZoneName))
            {
                msTimetoken = AppTimeZoneBL.DictTimeZoneFullNameKey[timeZoneName];
            }
        }

        if (string.IsNullOrWhiteSpace(msTimetoken))
        {
            msTimetoken = AppTimeZoneBL.GetMsTimezoneTokenFromAbbreviationAndTimezoneOffset(abbreviation, timeZoneOffset);
        }

        return msTimetoken;
    }

    private string GetMsTimeZoneShortDisplayNameFromRequestHeader(string msTimetoken = "")
    {
        string timeZoneName = HttpContext.Request.Headers["TimeZoneName"];
        string abbreviation = HttpContext.Request.Headers["TimeZoneAbbreviation"];

        if (string.IsNullOrWhiteSpace(msTimetoken))
        {
            msTimetoken = GetMsTimeTokenFromRequestHeader();
        }

        if (string.IsNullOrWhiteSpace(msTimetoken))
        {
            if (!string.IsNullOrWhiteSpace(abbreviation))
            {
                return " Unknown Zone (" + abbreviation + ")";
            }
            else if (!string.IsNullOrWhiteSpace(timeZoneName))
            {
                return " Unknown Zone (" + timeZoneName + ")";
            }
            else
            {
                return "Unknown Timezone";
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(abbreviation))
            {
                return abbreviation;
            }
            else if (!string.IsNullOrWhiteSpace(timeZoneName))
            {
                return timeZoneName;
            }
            else
            {
                return "";
            }
        }
    }
}
