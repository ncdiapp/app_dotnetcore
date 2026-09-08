using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using App.BL.AppMgr.AiSkill;
using App.BL.AIAgent.GenericAgent;
using APP.Components.EntityDto;
using HistoryDto   = App.BL.TenantBusiness.AppAgentSkillSetHistoryDto;
using HistBL       = App.BL.TenantBusiness.AppAgentSkillSetHistoryBL;
using ToolBL       = App.BL.TenantBusiness.AppAgentToolRegisterBL;
using McpBL        = App.BL.TenantBusiness.AppAgentMcpServerBL;
using LibBL        = App.BL.TenantBusiness.AppAgentToolLibraryBL;
using ToolDto      = App.BL.TenantBusiness.AppAgentToolRegisterDto;
using McpDto       = App.BL.TenantBusiness.AppAgentMcpServerDto;
using DomainDto    = App.BL.TenantBusiness.AppAgentToolDomainDto;
using LibraryDto   = App.BL.TenantBusiness.AppAgentToolLibraryDto;
using SubDto       = App.BL.TenantBusiness.AppAgentLibrarySubscriptionDto;
using ToolPreview  = App.BL.TenantBusiness.LibraryToolPreviewDto;
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

    // ─────────────────────────────────────────────────────────────────────
    // Tool Library — Domains
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<DomainDto>> GetAllDomains()
    {
        var result = new OperationCallResult<List<DomainDto>>();
        result.Object = LibBL.GetAllDomains(GetDsId());
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertDomain([FromBody] DomainDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.DomainKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "DomainKey_Required", ValidationItemType.Error, "DomainKey is required."));
            return result;
        }
        result.Object = LibBL.UpsertDomain(GetDsId(), dto);
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteDomain(string domainKey)
    {
        var result = new OperationCallResult<bool>();
        result.Object = LibBL.DeleteDomain(GetDsId(), domainKey ?? "");
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Tool Library — Libraries
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<LibraryDto>> GetAllLibraries()
    {
        var result = new OperationCallResult<List<LibraryDto>>();
        result.Object = LibBL.GetAllLibraries(GetDsId());
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<LibraryDto>> GetLibrariesByDomain(string domainKey)
    {
        var result = new OperationCallResult<List<LibraryDto>>();
        result.Object = LibBL.GetLibrariesByDomain(GetDsId(), domainKey ?? "");
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<LibraryDto>> SearchLibraries(string query)
    {
        var result = new OperationCallResult<List<LibraryDto>>();
        result.Object = LibBL.SearchLibraries(GetDsId(), query ?? "");
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<ToolPreview>> GetLibraryToolPreview(string libraryKey)
    {
        var result = new OperationCallResult<List<ToolPreview>>();
        result.Object = LibBL.GetLibraryToolPreview(GetDsId(), libraryKey ?? "");
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertLibrary([FromBody] LibraryDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.LibraryKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "LibraryKey_Required", ValidationItemType.Error, "LibraryKey is required."));
            return result;
        }
        result.Object = LibBL.UpsertLibrary(GetDsId(), dto);
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteLibrary(string libraryKey)
    {
        var result = new OperationCallResult<bool>();
        result.Object = LibBL.DeleteLibrary(GetDsId(), libraryKey ?? "");
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Tool Library — Subscriptions
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<SubDto>> GetSubscriptions(string skillKey)
    {
        var result = new OperationCallResult<List<SubDto>>();
        result.Object = LibBL.GetSubscriptions(GetDsId(), skillKey ?? "");
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> SetSubscriptions([FromBody] SetSubscriptionsRequest req)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(req?.SkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "SkillKey_Required", ValidationItemType.Error, "SkillKey is required."));
            return result;
        }
        result.Object = LibBL.SetSubscriptions(GetDsId(), req.SkillKey, req.LibraryKeys ?? new List<string>());
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // BuiltIn tool browser
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<ToolPreview>> GetAvailableBuiltInTools()
    {
        var result = new OperationCallResult<List<ToolPreview>>();
        result.Object = LibBL.GetAvailableBuiltInTools(GetDsId());
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Agent Templates
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<AppAgentSkillSetDto>> GetTemplates()
    {
        var result = new OperationCallResult<List<AppAgentSkillSetDto>>();
        result.Object = AppAgentSkillSetBL.GetTemplates(GetDsId());
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Prompt Version History
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<HistoryDto>> GetPromptHistory(string skillKey)
    {
        var result = new OperationCallResult<List<HistoryDto>>();
        result.Object = HistBL.GetRecent(GetDsId(), skillKey ?? "");
        return result;
    }
}

public sealed class SetSubscriptionsRequest
{
    public string SkillKey { get; set; }
    public List<string> LibraryKeys { get; set; }
}
