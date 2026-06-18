using System.Collections.Generic;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Microsoft.AspNetCore.Mvc;
using static App.BL.AppThemeBL;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class DynamicLayoutController : SecureBaseController
{
    [HttpGet]
    public AppTransactionExDto TransactionForm(int transactionId, int? transGroupId, string rootPkId, string isPrint, int? opennedFormAutoExecuteCommandId = null, string isPreview = "")
    {
        AppTransactionExDto appTransactionExDto = null;

        // -1: Preload MVC Views
        if (transactionId == -1)
        {
            int? fileTransactionId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemDefinedFileTransactionId);

            if (fileTransactionId.HasValue)
            {
                transactionId = fileTransactionId.Value;

                appTransactionExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                appTransactionExDto.IsLoadingPrintForm = false;
                appTransactionExDto.IsAllowAccess = true;
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(isPrint) && isPrint.ToLower() == "true")
            {
                bool isConfigTestRun = !string.IsNullOrWhiteSpace(isPreview) && isPreview.ToLower() == "true";

                appTransactionExDto = AppMasterDetailFormPrintBL.PrepareFormMasterDetailPrintData(transactionId, rootPkId, false, opennedFormAutoExecuteCommandId, isConfigTestRun);

                appTransactionExDto.IsLoadingPrintForm = true;
            }
            else
            {
                appTransactionExDto = AppTransactionBL.GetCurrentUserOneHierarchyTransactionWithSecurityAndLangLable(transactionId, rootPkId);
                appTransactionExDto.IsLoadingPrintForm = false;
            }
        }


        appTransactionExDto.TransactionHeader = new List<AppTransactionExDto>();
        appTransactionExDto.TransactionCrossHeader = new List<AppTransactionExDto>();


        AppFormExDto appFormExDto = appTransactionExDto.ForeignAppFormExDto;
        if (appFormExDto == null && appTransactionExDto.FormId.HasValue)
        {
            appFormExDto = appTransactionExDto.ForeignAppFormExDto =
                AppFormBL.RetrieveTransactionAppFormExDto(appTransactionExDto);
        }


        return appTransactionExDto;
    }
}
