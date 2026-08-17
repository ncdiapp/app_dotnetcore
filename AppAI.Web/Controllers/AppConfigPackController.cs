using System;
using APP.BL.AppConfigPack;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]")]
public class AppConfigPackController : SecureBaseController
{
    [HttpPost("Load")]
    public OperationCallResult<AppConfigPackDto> Load([FromBody] AppConfigPackLoadRequestDto request)
    {
        try
        {
            return AppConfigPackBL.Load(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<AppConfigPackDto>("AppConfigPack_Load_Error", ex);
        }
    }

    [HttpPost("Validate")]
    public OperationCallResult<AppConfigPackValidationDto> Validate([FromBody] AppConfigPackDto pack)
    {
        try
        {
            return AppConfigPackBL.Validate(pack);
        }
        catch (Exception ex)
        {
            return ErrorResult<AppConfigPackValidationDto>("AppConfigPack_Validate_Error", ex);
        }
    }

    [HttpPost("Preview")]
    public OperationCallResult<AppConfigPackPreviewDto> Preview([FromBody] AppConfigPackExecuteRequestDto request)
    {
        try
        {
            return AppConfigPackBL.Preview(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<AppConfigPackPreviewDto>("AppConfigPack_Preview_Error", ex);
        }
    }

    [HttpPost("Execute")]
    public OperationCallResult<AppConfigPackExecuteResultDto> Execute([FromBody] AppConfigPackExecuteRequestDto request)
    {
        try
        {
            return AppConfigPackBL.Execute(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<AppConfigPackExecuteResultDto>("AppConfigPack_Execute_Error", ex);
        }
    }

    [HttpPost("Export")]
    public OperationCallResult<AppConfigPackExportResultDto> Export([FromBody] AppConfigPackExportRequestDto request)
    {
        try
        {
            return AppConfigPackBL.Export(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<AppConfigPackExportResultDto>("AppConfigPack_Export_Error", ex);
        }
    }

    private static OperationCallResult<T> ErrorResult<T>(string code, Exception ex)
    {
        var result = new OperationCallResult<T>();
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(AppConfigPackController), code, ValidationItemType.Error, ex.Message));
        return result;
    }
}
