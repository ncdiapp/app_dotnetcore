using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using App.BL.AppMgr.AiSkill;
using App.BL.AIAgent.GenericAgent;
using APP.Components.EntityDto;
using ToolBL  = App.BL.TenantBusiness.AppAgentToolRegisterBL;
using McpBL   = App.BL.TenantBusiness.AppAgentMcpServerBL;
using ToolDto = App.BL.TenantBusiness.AppAgentToolRegisterDto;
using McpDto  = App.BL.TenantBusiness.AppAgentMcpServerDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using AppAI.Web.Controllers.Base;

namespace AppAI.Web.Controllers;

[Route("webapi/[controller]/[action]")]
public class AgentSkillSetController : SecureBaseController
{
    private static int GetDsId() => AppAISkillBL.GetDefaultDataSourceId() ?? 0;

    [HttpGet]
    public OperationCallResult<int?> GetDefaultDataSourceId()
    {
        var result = new OperationCallResult<int?>();
        result.Object = AppAISkillBL.GetDefaultDataSourceId();
        return result;
    }

    [HttpGet]
    public OperationCallResult<string> GetDebugInfo()
    {
        var result = new OperationCallResult<string>();
        try
        {
            var dsId = AppAISkillBL.GetDefaultDataSourceId();
            var (connStr, rowCount) = AppAgentSkillSetBL.GetDebugInfo(dsId ?? 0);
            result.Object = $"DsId={dsId} | Rows={rowCount} | Conn={connStr}";
        }
        catch (Exception ex) { result.Object = "ERR: " + ex.Message; }
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<AppAgentSkillSetDto>> GetAllSkillSets()
    {
        var result = new OperationCallResult<List<AppAgentSkillSetDto>>();
        result.Object = AppAgentSkillSetBL.GetAllSkillSets(GetDsId());
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertSkillSet([FromBody] AppAgentSkillSetDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.SkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "SkillKey_Required", ValidationItemType.Error, "SkillKey is required."));
            return result;
        }
        result.Object = AppAgentSkillSetBL.UpsertSkillSet(GetDsId(), dto);
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteSkillSet(string skillKey)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(skillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "SkillKey_Required", ValidationItemType.Error, "SkillKey is required."));
            return result;
        }
        result.Object = AppAgentSkillSetBL.DeleteSkillSet(GetDsId(), skillKey);
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<ToolDto>> GetToolsBySkillKey(string skillKey)
    {
        var result = new OperationCallResult<List<ToolDto>>();
        result.Object = ToolBL.GetBySkillKey(skillKey ?? "");
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertTool([FromBody] ToolDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.ToolName))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "ToolName_Required", ValidationItemType.Error, "ToolName is required."));
            return result;
        }
        result.Object = ToolBL.Upsert(dto) >= 0;
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteTool(int id)
    {
        var result = new OperationCallResult<bool>();
        result.Object = ToolBL.Delete(id);
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<McpDto>> GetAllMcpServers()
    {
        var result = new OperationCallResult<List<McpDto>>();
        result.Object = McpBL.GetAll();
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertMcpServer([FromBody] McpDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.ServerName))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "ServerName_Required", ValidationItemType.Error, "ServerName is required."));
            return result;
        }
        result.Object = McpBL.Upsert(dto) >= 0;
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteMcpServer(int mcpServerId)
    {
        var result = new OperationCallResult<bool>();
        result.Object = McpBL.Delete(mcpServerId);
        return result;
    }
}
