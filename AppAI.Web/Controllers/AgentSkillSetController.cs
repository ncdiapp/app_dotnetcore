using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using App.BL.AppMgr.AiSkill;
using App.BL.AIAgent.GenericAgent;
using App.BL.DbGenie;
using App.BL.GenericAgent;
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

    // ─────────────────────────────────────────────────────────────────────
    // AI-Assisted Agent Design Generation
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<OperationCallResult<GenerateAgentResult>> GenerateAgentDesign(
        [FromBody] GenerateAgentRequest req)
    {
        var result = new OperationCallResult<GenerateAgentResult>();
        if (string.IsNullOrWhiteSpace(req?.Description))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "Description_Required", ValidationItemType.Error,
                "Description is required."));
            return result;
        }

        var dsId = GetDsId();
        var libraries = LibBL.GetAllLibraries(dsId);
        var builtIns  = LibBL.GetAvailableBuiltInTools(dsId);

        var libCatalog  = string.Join("\n", libraries.Select(l =>
            $"- {l.LibraryKey} ({l.ToolCategory}/{l.DomainKey}): {l.LibraryName} — {l.Description}"));
        var toolCatalog = string.Join("\n", builtIns.Select(t =>
            $"- {t.ToolName}: {t.ToolDescription}"));

        var metaPrompt = $@"You are an expert AI system prompt engineer for AppAI, an enterprise no-code platform.
Given a domain expert's description of an agent, output a JSON object with exactly three fields.

=== Available Tool Libraries (subscribe via LibraryKey) ===
{(string.IsNullOrEmpty(libCatalog) ? "(none configured)" : libCatalog)}

=== Available Built-in Tools (register by ToolName) ===
{(string.IsNullOrEmpty(toolCatalog) ? "(none configured)" : toolCatalog)}

Output ONLY valid JSON — no markdown fences, no extra text:
{{
  ""SystemPrompt"": ""..."",
  ""RecommendedLibraryKeys"": [""lib-key-1""],
  ""RecommendedBuiltInToolNames"": [""ToolName1""]
}}

SystemPrompt must use exactly four ## H2 sections:
## Role — 2-3 sentences: agent name, domain, primary job
## Workflow — 4-6 numbered steps the agent follows
## Rules — constraints and guardrails as bullet list
## Output Format — how the agent structures its responses

RecommendedLibraryKeys: pick 0-3 keys from the Tool Libraries catalog that genuinely match. Never invent keys.
RecommendedBuiltInToolNames: pick 0-5 tool names from the Built-in Tools catalog the agent clearly needs. Never invent names.
If nothing matches, return empty arrays.";

        var llmReq = new LLMRequestDto
        {
            Provider     = LLMProviderHelper.GetConfiguredProvider(),
            ApiKey       = LLMProviderHelper.GetConfiguredApiKey(),
            Model        = AIConfigSettingBL.GetModel(),
            SystemPrompt = metaPrompt,
            Prompt       = req.Description,
            MaxTokens    = 2048,
        };

        var llmRes = await LLMProviderHelper.CallLLMAsync(llmReq);
        if (!llmRes.IsSuccess)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "LLM_Error", ValidationItemType.Error,
                llmRes.Error ?? "LLM call failed"));
            return result;
        }

        try
        {
            var raw = llmRes.Content?.Trim() ?? "";
            if (raw.StartsWith("```"))
                raw = Regex.Replace(raw, @"^```[a-z]*\r?\n?|```$", "", RegexOptions.Multiline).Trim();
            result.Object = System.Text.Json.JsonSerializer.Deserialize<GenerateAgentResult>(
                raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
        catch
        {
            result.Object = new GenerateAgentResult(llmRes.Content ?? "", new List<string>(), new List<string>());
        }
        return result;
    }
}

public sealed class SetSubscriptionsRequest
{
    public string SkillKey { get; set; }
    public List<string> LibraryKeys { get; set; }
}

public sealed class GenerateAgentRequest
{
    public string Description { get; set; }
}

public sealed record GenerateAgentResult(
    string       SystemPrompt,
    List<string> RecommendedLibraryKeys,
    List<string> RecommendedBuiltInToolNames);
