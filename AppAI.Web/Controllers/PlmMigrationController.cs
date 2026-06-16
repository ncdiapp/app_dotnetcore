using System;
using System.Collections.Generic;
using APP.BL.DataMigration.PlmMigration;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]")]
public class PlmMigrationController : SecureBaseController
{
    [HttpPost("TestPlmConnection")]
    public OperationCallResult<PlmConnectionTestResultDto> TestPlmConnection([FromBody] PlmConnectionTestRequestDto request)
    {
        try
        {
            return PlmMigrationBL.TestPlmConnection(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmConnectionTestResultDto>("Plm_TestConnection_Error", ex);
        }
    }

    [HttpPost("DiscoverPlmDataSources")]
    public OperationCallResult<PlmDiscoverDataSourcesResultDto> DiscoverPlmDataSources([FromBody] PlmDiscoverDataSourcesRequestDto request)
    {
        try
        {
            return PlmMigrationBL.DiscoverPlmDataSources(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDiscoverDataSourcesResultDto>("Plm_Discover_Error", ex);
        }
    }

    [HttpGet("ImportSession/active")]
    public OperationCallResult<PlmImportSessionDto> GetActiveImportSession(int? targetCompanyId)
    {
        try
        {
            return PlmMigrationBL.GetActiveImportSession(targetCompanyId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportSessionDto>("Plm_Session_GetActive_Error", ex);
        }
    }

    [HttpPost("ImportSession")]
    public OperationCallResult<PlmImportSessionDto> SaveImportSession([FromBody] PlmImportSessionDto dto)
    {
        try
        {
            return PlmMigrationBL.SaveImportSession(dto);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportSessionDto>("Plm_Session_Save_Error", ex);
        }
    }

    [HttpPost("ImportSession/discard")]
    public OperationCallResult<bool> DiscardImportSession(int? sessionId, int? targetCompanyId)
    {
        try
        {
            return PlmMigrationBL.DiscardImportSession(sessionId, targetCompanyId);
        }
        catch (Exception ex)
        {
            return ErrorResult<bool>("Plm_Session_Discard_Error", ex);
        }
    }

    [HttpPost("PreviewUserDefineEntityImport")]
    public OperationCallResult<object> PreviewUserDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewUserDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<object>("Plm_UserDefine_Preview_Error", ex);
        }
    }

    [HttpPost("PreviewSystemDefineEntityImport")]
    public OperationCallResult<object> PreviewSystemDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewSystemDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<object>("Plm_SystemDefine_Preview_Error", ex);
        }
    }

    [HttpPost("ExecuteUserDefineEntityImport")]
    public OperationCallResult<PlmImportJobDto> ExecuteUserDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecuteUserDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_UserDefine_Execute_Error", ex);
        }
    }

    [HttpPost("ExecuteSystemDefineEntityImport")]
    public OperationCallResult<PlmImportJobDto> ExecuteSystemDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecuteSystemDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_SystemDefine_Execute_Error", ex);
        }
    }

    [HttpGet("ImportJob/{jobId:int}")]
    public OperationCallResult<PlmImportJobDto> GetImportJob(int jobId)
    {
        try
        {
            return PlmMigrationBL.GetImportJob(jobId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_Job_Get_Error", ex);
        }
    }

    [HttpPost("ImportJob/{jobId:int}/cancel")]
    public OperationCallResult<bool> CancelImportJob(int jobId)
    {
        try
        {
            return PlmMigrationBL.CancelImportJob(jobId);
        }
        catch (Exception ex)
        {
            return ErrorResult<bool>("Plm_Job_Cancel_Error", ex);
        }
    }

    [HttpGet("ImportLog")]
    public OperationCallResult<List<PlmImportLogDto>> GetImportLog(int? sessionId, int? targetCompanyId)
    {
        try
        {
            return PlmMigrationBL.GetImportLog(sessionId, targetCompanyId);
        }
        catch (Exception ex)
        {
            return ErrorResult<List<PlmImportLogDto>>("Plm_Log_Get_Error", ex);
        }
    }

    [HttpPost("PreviewTemplateMapping")]
    public OperationCallResult<object> PreviewTemplateMapping(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewTemplateMapping(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<object>("Plm_Template_Preview_Error", ex);
        }
    }

    [HttpPost("ExecuteTemplateImport")]
    public OperationCallResult<PlmImportJobDto> ExecuteTemplateImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecuteTemplateImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_Template_Execute_Error", ex);
        }
    }

    private static OperationCallResult<T> ErrorResult<T>(string code, Exception ex)
    {
        var result = new OperationCallResult<T>();
        result.ValidationResult.Items.Add(new ValidationItem(
            typeof(PlmMigrationController), code, ValidationItemType.Error, ex.Message));
        return result;
    }
}
