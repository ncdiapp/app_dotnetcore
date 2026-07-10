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
    public OperationCallResult<PlmUserDefineEntityPreviewDto> PreviewUserDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewUserDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmUserDefineEntityPreviewDto>("Plm_UserDefine_Preview_Error", ex);
        }
    }

    [HttpPost("PreviewSystemDefineEntityImport")]
    public OperationCallResult<PlmSystemDefineEntityPreviewDto> PreviewSystemDefineEntityImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewSystemDefineEntityImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSystemDefineEntityPreviewDto>("Plm_SystemDefine_Preview_Error", ex);
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

    [HttpPost("PreviewPlmTableExportPlan")]
    public OperationCallResult<PlmTableExportPlanDto> PreviewPlmTableExportPlan(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmTableExportPlan(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmTableExportPlanDto>("Plm_TableExport_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmTableExport")]
    public OperationCallResult<PlmImportJobDto> ExecutePlmTableExport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.StartPlmTableExportJob(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_TableExport_Execute_Error", ex);
        }
    }

    [HttpPost("PreviewPlmSketchImport")]
    public OperationCallResult<PlmSketchImportPreviewDto> PreviewPlmSketchImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmSketchImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSketchImportPreviewDto>("Plm_Sketch_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmSketchImport")]
    public OperationCallResult<PlmImportJobDto> ExecutePlmSketchImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecutePlmSketchImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_Sketch_Execute_Error", ex);
        }
    }

    [HttpPost("PreviewPlmFolderImport")]
    public OperationCallResult<PlmFolderImportPreviewDto> PreviewPlmFolderImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmFolderImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmFolderImportPreviewDto>("Plm_Folder_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmFolderImport")]
    public OperationCallResult<PlmImportJobDto> ExecutePlmFolderImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecutePlmFolderImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_Folder_Execute_Error", ex);
        }
    }

    [HttpPost("PreviewPlmFolderPlacement")]
    public OperationCallResult<PlmFolderPlacementPreviewDto> PreviewPlmFolderPlacement(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmFolderPlacement(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmFolderPlacementPreviewDto>("Plm_FolderPlacement_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmFolderPlacement")]
    public OperationCallResult<PlmImportJobDto> ExecutePlmFolderPlacement(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.ExecutePlmFolderPlacement(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmImportJobDto>("Plm_FolderPlacement_Execute_Error", ex);
        }
    }

    [HttpPost("PreviewTemplateMapping")]
    public OperationCallResult<PlmTemplatePreviewDto> PreviewTemplateMapping(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewTemplateMapping(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmTemplatePreviewDto>("Plm_Template_Preview_Error", ex);
        }
    }

    [HttpPost("GetTemplateTabMappingGrid")]
    public OperationCallResult<PlmTemplateMappingGridDto> GetTemplateTabMappingGrid(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.GetTemplateTabMappingGrid(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmTemplateMappingGridDto>("Plm_Template_Grid_Error", ex);
        }
    }

    [HttpPost("SaveTemplateMapping")]
    public OperationCallResult<PlmTemplateImportSettingDto> SaveTemplateMapping(int? sessionId, [FromBody] PlmTemplateImportSettingDto setting)
    {
        try
        {
            return PlmMigrationBL.SaveTemplateMapping(sessionId, setting);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmTemplateImportSettingDto>("Plm_Template_Save_Error", ex);
        }
    }

    [HttpPost("ValidateTemplateMapping")]
    public OperationCallResult<PlmTemplateMappingValidationDto> ValidateTemplateMapping(int? sessionId, [FromBody] PlmTemplateImportSettingDto setting)
    {
        try
        {
            return PlmMigrationBL.ValidateTemplateMapping(sessionId, setting);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmTemplateMappingValidationDto>("Plm_Template_Validate_Error", ex);
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

    [HttpPost("LoadDwImportBlueprint")]
    public OperationCallResult<PlmDwImportBlueprintDto> LoadDwImportBlueprint([FromBody] PlmDwBlueprintLoadRequestDto request)
    {
        try
        {
            return PlmMigrationBL.LoadDwImportBlueprint(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDwImportBlueprintDto>("Plm_DwBlueprint_Load_Error", ex);
        }
    }

    [HttpGet("LoadDwImportBlueprintFromTable")]
    public OperationCallResult<PlmDwImportBlueprintDto> LoadDwImportBlueprintFromTable(string tablePrefix, string blueprintKey = "default")
    {
        try
        {
            return PlmMigrationBL.LoadDwImportBlueprintFromTenantTable(tablePrefix, blueprintKey);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDwImportBlueprintDto>("Plm_DwBlueprint_LoadTable_Error", ex);
        }
    }

    [HttpPost("ValidateDwImportBlueprint")]
    public OperationCallResult<PlmDwBlueprintValidationDto> ValidateDwImportBlueprint([FromBody] PlmDwImportBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.ValidateDwImportBlueprint(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDwBlueprintValidationDto>("Plm_DwBlueprint_Validate_Error", ex);
        }
    }

    [HttpPost("PreviewDwBlueprintConfig")]
    public OperationCallResult<PlmDwBlueprintPreviewDto> PreviewDwBlueprintConfig([FromBody] PlmDwImportBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.PreviewDwBlueprintConfig(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDwBlueprintPreviewDto>("Plm_DwBlueprint_Preview_Error", ex);
        }
    }

    [HttpPost("ExecuteDwBlueprintConfig")]
    public OperationCallResult<PlmDwBlueprintExecuteResultDto> ExecuteDwBlueprintConfig([FromBody] PlmDwBlueprintExecuteRequestDto request)
    {
        try
        {
            return PlmMigrationBL.ExecuteDwBlueprintConfig(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmDwBlueprintExecuteResultDto>("Plm_DwBlueprint_Execute_Error", ex);
        }
    }

    [HttpPost("RefreshDwImportTenantCaches")]
    public OperationCallResult<bool> RefreshDwImportTenantCaches([FromBody] PlmDwRefreshCachesRequestDto request)
    {
        try
        {
            return PlmMigrationBL.RefreshDwImportTenantCaches(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<bool>("Plm_DwBlueprint_RefreshCaches_Error", ex);
        }
    }

    [HttpPost("PreviewPlmColorImport")]
    public OperationCallResult<PlmColorImportPreviewDto> PreviewPlmColorImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmColorImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmColorImportPreviewDto>("Plm_Color_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmColorImport")]
    public OperationCallResult<PlmColorImportExecuteResultDto> ExecutePlmColorImport([FromBody] PlmColorImportExecuteRequestDto request)
    {
        try
        {
            return PlmMigrationBL.ExecutePlmColorImport(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmColorImportExecuteResultDto>("Plm_Color_Execute_Error", ex);
        }
    }

    [HttpPost("PreviewPlmPomImport")]
    public OperationCallResult<PlmPomImportPreviewDto> PreviewPlmPomImport(int? sessionId)
    {
        try
        {
            return PlmMigrationBL.PreviewPlmPomImport(sessionId);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmPomImportPreviewDto>("Plm_Pom_Preview_Error", ex);
        }
    }

    [HttpPost("ExecutePlmPomImport")]
    public OperationCallResult<PlmPomImportExecuteResultDto> ExecutePlmPomImport([FromBody] PlmPomImportExecuteRequestDto request)
    {
        try
        {
            return PlmMigrationBL.ExecutePlmPomImport(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmPomImportExecuteResultDto>("Plm_Pom_Execute_Error", ex);
        }
    }

    [HttpPost("LoadSearchImportBlueprint")]
    public OperationCallResult<PlmSearchImportBlueprintDto> LoadSearchImportBlueprint([FromBody] PlmSearchImportLoadRequestDto request)
    {
        try
        {
            return PlmMigrationBL.LoadSearchImportBlueprint(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportBlueprintDto>("Plm_SearchImport_Load_Error", ex);
        }
    }

    [HttpPost("ValidateSearchImportBlueprint")]
    public OperationCallResult<PlmSearchImportValidationDto> ValidateSearchImportBlueprint([FromBody] PlmSearchImportBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.ValidateSearchImportBlueprint(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportValidationDto>("Plm_SearchImport_Validate_Error", ex);
        }
    }

    [HttpPost("PreviewSearchBlueprintConfig")]
    public OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchBlueprintConfig([FromBody] PlmSearchImportBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.PreviewSearchBlueprintConfig(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportPreviewDto>("Plm_SearchImport_Preview_Error", ex);
        }
    }

    [HttpPost("ExecuteSearchBlueprintConfig")]
    public OperationCallResult<PlmSearchImportExecuteResultDto> ExecuteSearchBlueprintConfig([FromBody] PlmSearchImportExecuteRequestDto request)
    {
        try
        {
            return PlmMigrationBL.ExecuteSearchBlueprintConfig(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportExecuteResultDto>("Plm_SearchImport_Execute_Error", ex);
        }
    }

    [HttpPost("LoadSearchSiblingViewBlueprint")]
    public OperationCallResult<PlmSearchSiblingViewBlueprintDto> LoadSearchSiblingViewBlueprint(
        [FromBody] PlmSearchSiblingViewLoadRequestDto request)
    {
        try
        {
            return PlmMigrationBL.LoadSearchSiblingViewBlueprint(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchSiblingViewBlueprintDto>("Plm_SearchSibling_Load_Error", ex);
        }
    }

    [HttpPost("ValidateSearchSiblingViewBlueprint")]
    public OperationCallResult<PlmSearchImportValidationDto> ValidateSearchSiblingViewBlueprint(
        [FromBody] PlmSearchSiblingViewBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.ValidateSearchSiblingViewBlueprint(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportValidationDto>("Plm_SearchSibling_Validate_Error", ex);
        }
    }

    [HttpPost("PreviewSearchSiblingViewConfig")]
    public OperationCallResult<PlmSearchImportPreviewDto> PreviewSearchSiblingViewConfig(
        [FromBody] PlmSearchSiblingViewBlueprintDto blueprint)
    {
        try
        {
            return PlmMigrationBL.PreviewSearchSiblingViewConfig(blueprint);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchImportPreviewDto>("Plm_SearchSibling_Preview_Error", ex);
        }
    }

    [HttpPost("ExecuteSearchSiblingViewConfig")]
    public OperationCallResult<PlmSearchSiblingViewExecuteResultDto> ExecuteSearchSiblingViewConfig(
        [FromBody] PlmSearchSiblingViewExecuteRequestDto request)
    {
        try
        {
            return PlmMigrationBL.ExecuteSearchSiblingViewConfig(request);
        }
        catch (Exception ex)
        {
            return ErrorResult<PlmSearchSiblingViewExecuteResultDto>("Plm_SearchSibling_Execute_Error", ex);
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
