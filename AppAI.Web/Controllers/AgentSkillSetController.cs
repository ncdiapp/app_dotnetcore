using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
using LibToolBL    = App.BL.TenantBusiness.AppAgentLibraryToolBL;
using ToolDto      = App.BL.TenantBusiness.AppAgentToolRegisterDto;
using LibToolDto   = App.BL.TenantBusiness.AppAgentLibraryToolDto;
using McpDto       = App.BL.TenantBusiness.AppAgentMcpServerDto;
using CatalogBL    = App.BL.TenantBusiness.AppAgentToolCatalogBL;
using CatalogDto   = App.BL.TenantBusiness.AppAgentToolCatalogDto;
using ToolCatalogRetrieval = App.BL.TenantBusiness.ToolCatalogRetrieval;
using ExclusionBL  = App.BL.TenantBusiness.AppAgentToolExclusionBL;
using ExclusionDto = App.BL.TenantBusiness.AppAgentToolExclusionDto;
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
        if (!AppAgentSkillSetBL.TryDeleteSkillSet(GetDsId(), skillKey, out var error))
        {
            result.Object = false;
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "DeleteSkillSet_Blocked", ValidationItemType.Error,
                error ?? "Delete failed."));
            return result;
        }
        result.Object = true;
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<AppAgentChildMappingDto>> GetAllChildMappings()
    {
        var result = new OperationCallResult<List<AppAgentChildMappingDto>>();
        result.Object = AppAgentChildMappingBL.GetAll(GetDsId());
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<AppAgentChildMappingDto>> GetChildAgents(string parentSkillKey)
    {
        var result = new OperationCallResult<List<AppAgentChildMappingDto>>();
        result.Object = AppAgentChildMappingBL.GetChildren(GetDsId(), parentSkillKey ?? "");
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<string>> GetChildUsedBy(string childSkillKey)
    {
        var result = new OperationCallResult<List<string>>();
        result.Object = AppAgentChildMappingBL.GetUsedByParents(GetDsId(), childSkillKey ?? "");
        return result;
    }

    [HttpPut]
    public OperationCallResult<bool> SetChildAgents(string parentSkillKey, [FromBody] List<AppAgentChildMappingDto> children)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(parentSkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "ParentSkillKey_Required", ValidationItemType.Error, "ParentSkillKey is required."));
            return result;
        }
        if (!AppAgentChildMappingBL.SetChildren(GetDsId(), parentSkillKey, children, out var error))
        {
            result.Object = false;
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "SetChildAgents_Failed", ValidationItemType.Error, error ?? "SetChildAgents failed."));
            return result;
        }
        result.Object = true;
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> AddChildAgents(string parentSkillKey, [FromBody] List<string> childSkillKeys)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(parentSkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "ParentSkillKey_Required", ValidationItemType.Error, "ParentSkillKey is required."));
            return result;
        }
        if (!AppAgentChildMappingBL.AddChildren(GetDsId(), parentSkillKey, childSkillKeys, out var error))
        {
            result.Object = false;
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "AddChildAgents_Failed", ValidationItemType.Error, error ?? "AddChildAgents failed."));
            return result;
        }
        result.Object = true;
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> RemoveChildAgent(string parentSkillKey, string childSkillKey)
    {
        var result = new OperationCallResult<bool>();
        if (!AppAgentChildMappingBL.RemoveChild(GetDsId(), parentSkillKey, childSkillKey, out var error))
        {
            result.Object = false;
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "RemoveChildAgent_Failed", ValidationItemType.Error, error ?? "RemoveChildAgent failed."));
            return result;
        }
        result.Object = true;
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
        // Header values never leave the server; the UI gets a mask and sends it back unchanged to keep the stored value.
        result.Object = McpBL.GetAll()
            .Select(s => s with { Headers = App.BL.TenantBusiness.McpHeaderSecrets.MaskForClient(s.Headers) })
            .ToList();
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
        if (!McpBL.LibraryExists(dto.SkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController),
                "LibraryKey_Invalid", ValidationItemType.Error, "MCP servers must belong to an existing Tool Library."));
            return result;
        }
        var policyError = McpSecurityPolicy.Validate(dto);
        if (policyError != null)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "McpServer_NotAllowed", ValidationItemType.Error, policyError));
            return result;
        }
        result.Object = McpBL.Upsert(dto) >= 0;
        return result;
    }

    [HttpPost]
    public async Task<OperationCallResult<McpTestResult>> TestMcpServer([FromBody] McpDto dto, CancellationToken cancellationToken)
    {
        var result = new OperationCallResult<McpTestResult>();
        result.Object = await McpConnectionHelper.TestConnectionAsync(McpBL.ResolveMaskedHeaders(dto), cancellationToken);
        return result;
    }

    // Reads the tools the MCP server reports now and stores them in the tool catalog (used by AI agent design).
    [HttpPost]
    public async Task<OperationCallResult<McpTestResult>> SyncMcpTools([FromBody] McpDto dto, CancellationToken cancellationToken)
    {
        var result = new OperationCallResult<McpTestResult>();
        if (dto == null || dto.McpServerId <= 0)
        {
            result.Object = new McpTestResult(false, "Save the server first, then sync its tools.", 0, new List<string>());
            return result;
        }
        var (sync, tools) = await McpConnectionHelper.ListCatalogToolsAsync(McpBL.ResolveMaskedHeaders(dto), cancellationToken);
        if (sync.Success)
        {
            try { CatalogBL.ReplaceMcpServerTools(GetDsId(), dto.McpServerId, tools); }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(SyncMcpTools));
                sync = new McpTestResult(false, "Could not save to the tool catalog (is migration V039 applied?): " + ex.Message, 0, new List<string>());
            }
        }
        result.Object = sync;
        return result;
    }

    [HttpGet]
    public OperationCallResult<List<CatalogDto>> GetToolCatalog(string libraryKeys)
    {
        var result = new OperationCallResult<List<CatalogDto>>();
        var keys = (libraryKeys ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        result.Object = keys.Length == 0 ? CatalogBL.GetAll(GetDsId()) : CatalogBL.GetByLibraries(GetDsId(), keys);
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
    // Tool Library — Library Tools (AppAgentLibraryTool)
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<LibToolDto>> GetLibraryTools(string libraryKey)
    {
        var result = new OperationCallResult<List<LibToolDto>>();
        result.Object = LibToolBL.GetByLibraryKey(GetDsId(), libraryKey ?? "");
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> UpsertLibraryTool([FromBody] LibToolDto dto)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(dto?.ToolName))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "ToolName_Required", ValidationItemType.Error, "ToolName is required."));
            return result;
        }
        result.Object = LibToolBL.Upsert(GetDsId(), dto) > 0;
        return result;
    }

    [HttpDelete]
    public OperationCallResult<bool> DeleteLibraryTool(int id)
    {
        var result = new OperationCallResult<bool>();
        result.Object = LibToolBL.Delete(GetDsId(), id);
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
    // Per-agent tool exclusions (tools an agent does NOT use from its subscribed libraries)
    // ─────────────────────────────────────────────────────────────────────

    [HttpGet]
    public OperationCallResult<List<ExclusionDto>> GetToolExclusions(string skillKey)
    {
        var result = new OperationCallResult<List<ExclusionDto>>();
        result.Object = ExclusionBL.GetBySkillKey(skillKey ?? "", GetDsId());
        return result;
    }

    [HttpPost]
    public OperationCallResult<bool> SetToolExclusions([FromBody] SetToolExclusionsRequest req)
    {
        var result = new OperationCallResult<bool>();
        if (string.IsNullOrWhiteSpace(req?.SkillKey))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "SkillKey_Required", ValidationItemType.Error, "SkillKey is required."));
            return result;
        }
        result.Object = ExclusionBL.ReplaceForSkill(req.SkillKey, req.Exclusions ?? new List<ExclusionDto>(), GetDsId());
        return result;
    }

    public class SetToolExclusionsRequest
    {
        public string SkillKey { get; set; } = "";
        public List<ExclusionDto> Exclusions { get; set; } = new();
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
        // ask_user is auto-injected for Interactive agents — never recommend as a private BuiltIn register.
        var autoInjectedBuiltIns = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ask_user" };
        var builtIns  = LibBL.GetAvailableBuiltInTools(dsId)
            .Where(t => !autoInjectedBuiltIns.Contains(t.ToolName ?? ""))
            .ToList();

        var libCatalog  = string.Join("\n", libraries.Select(l =>
            $"- {l.LibraryKey} ({l.ToolCategory}/{l.DomainKey}): {l.LibraryName} — {l.Description}"));
        var toolCatalog = string.Join("\n", builtIns.Select(t =>
            $"- {t.ToolName}: {t.ToolDescription}"));

        // Tool-level catalog (library tools + synced MCP tools). Shortlisted so the prompt stays small;
        // swap ToolCatalogRetrieval.Current for an embedding retriever to add RAG later.
        var warnings = new List<string>();
        var fullCatalog = new List<CatalogDto>();
        try
        {
            CatalogBL.SyncLibraryTools(dsId);
            // ask_user is injected at runtime — never offer it as a pickable tool.
            fullCatalog = CatalogBL.GetAll(dsId).Where(t => !autoInjectedBuiltIns.Contains(t.ToolName ?? "")).ToList();
            foreach (var s in CatalogBL.GetUnsyncedMcpServers(dsId))
                warnings.Add($"MCP server {s} has not been synced, so its tools cannot be recommended. Open Tool Libraries › MCP Servers and click Sync tools.");
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Tool catalog unavailable for AI agent design");
            warnings.Add("Tool catalog is unavailable (migration V039 may not be applied) — only libraries were recommended.");
        }

        var shortlist = ToolCatalogRetrieval.Current.Retrieve(fullCatalog, req.Description, 300);
        string ToolLine(CatalogDto t)
        {
            var desc = t.Description ?? "";
            if (desc.Length > 140) desc = desc.Substring(0, 140) + "…";
            var args = string.IsNullOrEmpty(t.InputSummary) ? "" : $" (args: {t.InputSummary})";
            return $"- {t.ToolName} [{t.Risk}]{args}: {desc}";
        }
        var libToolCatalog = string.Join("\n", shortlist.GroupBy(t => t.LibraryKey)
            .Select(g => $"## {g.Key}\n" + string.Join("\n", g.Select(ToolLine))));

        var metaPrompt = $@"You are an expert AI system prompt engineer for AppAI, an enterprise no-code platform.
Given a domain expert's description of an agent, output a JSON object with exactly four fields.

=== Available Tool Libraries (subscribe via LibraryKey) ===
{(string.IsNullOrEmpty(libCatalog) ? "(none configured)" : libCatalog)}

=== Tools inside libraries (grouped by LibraryKey; [read]/[write]/[delete] = what the tool does) ===
{(string.IsNullOrEmpty(libToolCatalog) ? "(none synced)" : libToolCatalog)}

=== Available Built-in Tools (register by ToolName as private agent tools; only for tools not found in a library above) ===
{(string.IsNullOrEmpty(toolCatalog) ? "(none configured)" : toolCatalog)}

IMPORTANT:
- Do NOT recommend ask_user. Interactive agents get ask_user automatically at runtime; it must not be registered as a private tool.
- Each RecommendedLibraryKeys / RecommendedBuiltInToolNames entry must appear at most once (no duplicates).
- Only pick tools the agent clearly needs beyond ask_user.

Output ONLY valid JSON — no markdown fences, no extra text:
{{
  ""SystemPrompt"": ""..."",
  ""RecommendedLibraryKeys"": [""lib-key-1""],
  ""RecommendedBuiltInToolNames"": [""ToolName1""],
  ""RecommendedTools"": [{{""LibraryKey"": ""lib-key-1"", ""ToolName"": ""ToolName"", ""Reason"": ""why this step needs it""}}]
}}

SystemPrompt must use exactly four ## H2 sections:
## Role — 2-3 sentences: agent name, domain, primary job
## Workflow — 4-6 numbered steps the agent follows. Name the exact ToolName the agent calls in each step.
## Rules — constraints and guardrails as bullet list
## Output Format — how the agent structures its responses

=== ask_user contract (CRITICAL — include in SystemPrompt when the user description collects input) ===
ask_user is runtime-injected. SystemPrompt MUST tell the agent exactly how to call it — vague phrases like ""structured text fields"" or ""standard options"" are FORBIDDEN because the UI will not render form controls.

1) Fill-a-form (user says form / fill / fields / First Name + Last Name + a choice, etc.):
   - Prefer ONE ask_user call with mode=text AND non-empty fieldsJson covering every form field.
   - fieldsJson is a JSON array of {{name,label,required?,type?,options?}}.
   - Text boxes: type=text.
   - Drop-down inside the form (ddl / dropdown / combobox): type=select + non-empty options [{{""id"":""A"",""display"":""A""}},...].
   - Radio buttons inside the form (user says radio / radio button): type=radio + non-empty options. NEVER use type=select for radio — select always renders a DDL.
   - Example (name + preferred fruit RADIO on one form):
     mode=text
     fieldsJson=[
       {{""name"":""firstName"",""label"":""First Name"",""required"":true,""type"":""text""}},
       {{""name"":""lastName"",""label"":""Last Name"",""required"":true,""type"":""text""}},
       {{""name"":""preferredFruit"",""label"":""Select your preferred fruit"",""required"":true,""type"":""radio"",""options"":[{{""id"":""A"",""display"":""A""}},{{""id"":""B"",""display"":""B""}},{{""id"":""C"",""display"":""C""}}]}}
     ]
   - NEVER ask for multiple fields only in conversational Prompt text without fieldsJson.
   - Valid fieldsJson type values ONLY: text | select | radio. Do not invent other types.

2) Standalone choice menus (confirm / Retry|Cancel / not part of a multi-field form):
   - Call ask_user with mode=single_choice (or multi_choice), non-empty optionsJson, AND explicit ui.
   - optionsJson MUST be [{{""id"":""A"",""display"":""A""}},...] — NEVER list choices only in Prompt text.
   - ui: ddl/dropdown/combobox → ui=dropdown; radio → ui=radio; buttons/menu → ui=button_group.

3) Workflow steps that collect input MUST paste the concrete mode / fieldsJson (type=radio or type=select + options) or optionsJson+ui shape — not prose summaries.
4) Rules MUST restate: MUST use ask_user; MUST supply fieldsJson for forms; radio choices MUST use type=radio (not select); DDL MUST use type=select; NEVER put numbered choices only in FinalResponse text.
5) Do not put ask_user in RecommendedBuiltInToolNames.

RecommendedTools: plan the Workflow first, then list the tools its steps need, copying LibraryKey and ToolName exactly from the tools catalog. Choose the fewest tools that do the job. Never invent names. Never include ask_user. No duplicates.
Before any [write] or [delete] tool, the Workflow must include a confirmation step (call ask_user, which is always available at runtime, or propose_plan if it appears in the catalog), and Rules must say so.
RecommendedLibraryKeys: libraries of the recommended tools, plus up to 3 other libraries from the Tool Libraries catalog that genuinely match. Never invent keys. No duplicates.
RecommendedBuiltInToolNames: pick 0-5 tool names from the Built-in Tools catalog only if no library tool covers the need. Never invent names. Never include ask_user. No duplicates.
If nothing matches, return empty arrays.";

        var llmReq = new LLMRequestDto
        {
            Provider     = LLMProviderHelper.GetConfiguredProvider(),
            ApiKey       = LLMProviderHelper.GetConfiguredApiKey(),
            Model        = AIConfigSettingBL.GetModel(),
            SystemPrompt = metaPrompt,
            Prompt       = req.Description,
            MaxTokens    = 4096,
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
            var design = System.Text.Json.JsonSerializer.Deserialize<LlmAgentDesign>(
                raw, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            // Keep only tools that really exist in the catalog (the model sometimes invents names).
            var byKey = fullCatalog.ToDictionary(t => t.LibraryKey + "\u0001" + t.ToolName, t => t, StringComparer.OrdinalIgnoreCase);
            var picked = new List<RecommendedToolDto>();
            int dropped = 0;
            foreach (var r in design.RecommendedTools ?? new List<LlmToolPick>())
            {
                if (r?.LibraryKey == null || r.ToolName == null || !byKey.TryGetValue(r.LibraryKey + "\u0001" + r.ToolName, out var hit))
                { dropped++; continue; }
                if (picked.Any(p => p.LibraryKey == hit.LibraryKey && p.ToolName == hit.ToolName)) continue;
                picked.Add(new RecommendedToolDto(hit.LibraryKey, hit.ToolName, r.Reason ?? "", hit.Source, hit.Risk, hit.Description));
            }
            if (dropped > 0)
                warnings.Add($"{dropped} recommended tool(s) were not found in the catalog and were ignored.");

            var libKeys = (design.RecommendedLibraryKeys ?? new List<string>())
                .Concat(picked.Select(p => p.LibraryKey))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            result.Object = SanitizeGenerateAgentResult(
                new GenerateAgentResult(
                    design.SystemPrompt ?? "", libKeys, design.RecommendedBuiltInToolNames ?? new List<string>(), picked, warnings),
                autoInjectedBuiltIns);
        }
        catch (Exception ex)
        {
            NLog.LogManager.GetCurrentClassLogger().Warn(ex, "AI agent design response was not valid JSON");
            result.Object = new GenerateAgentResult(llmRes.Content ?? "", new List<string>(), new List<string>(), new List<RecommendedToolDto>(), warnings);
        }
        return result;
    }

    private static GenerateAgentResult SanitizeGenerateAgentResult(
        GenerateAgentResult parsed, HashSet<string> autoInjectedBuiltIns)
    {
        if (parsed == null)
            return new GenerateAgentResult("", new List<string>(), new List<string>(), new List<RecommendedToolDto>(), new List<string>());

        static List<string> Dedup(IEnumerable<string> items, HashSet<string> exclude = null)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            foreach (var raw in items ?? Array.Empty<string>())
            {
                var s = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (exclude != null && exclude.Contains(s)) continue;
                if (!seen.Add(s)) continue;
                list.Add(s);
            }
            return list;
        }

        return new GenerateAgentResult(
            parsed.SystemPrompt ?? "",
            Dedup(parsed.RecommendedLibraryKeys),
            Dedup(parsed.RecommendedBuiltInToolNames, autoInjectedBuiltIns),
            parsed.RecommendedTools ?? new List<RecommendedToolDto>(),
            parsed.Warnings ?? new List<string>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // AI-Assisted System Prompt Editing
    // ─────────────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<OperationCallResult<string>> EditSystemPrompt(
        [FromBody] EditSystemPromptRequest req)
    {
        var result = new OperationCallResult<string>();
        if (string.IsNullOrWhiteSpace(req?.CurrentPrompt) || string.IsNullOrWhiteSpace(req?.Instruction))
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "EditPrompt_Required", ValidationItemType.Error,
                "CurrentPrompt and Instruction are both required."));
            return result;
        }

        var metaPrompt = @"You are an expert AI system prompt editor for an enterprise no-code platform.
The user will provide their current system prompt and an editing instruction.
Apply the instruction to produce an improved version of the system prompt.
Return ONLY the edited system prompt — no preamble, no explanation, no markdown fences.
Preserve the existing structure and formatting style unless the instruction explicitly asks to change it.";

        var userMessage = $@"=== Current System Prompt ===
{req.CurrentPrompt}

=== Editing Instruction ===
{req.Instruction}";

        var llmReq = new LLMRequestDto
        {
            Provider     = LLMProviderHelper.GetConfiguredProvider(),
            ApiKey       = LLMProviderHelper.GetConfiguredApiKey(),
            Model        = AIConfigSettingBL.GetModel(),
            SystemPrompt = metaPrompt,
            Prompt       = userMessage,
            MaxTokens    = 4096,
        };

        var llmRes = await LLMProviderHelper.CallLLMAsync(llmReq);
        if (!llmRes.IsSuccess)
        {
            result.ValidationResult.Items.Add(new ValidationItem(
                typeof(AgentSkillSetController), "LLM_Error", ValidationItemType.Error,
                llmRes.Error ?? "LLM call failed"));
            return result;
        }

        var edited = llmRes.Content?.Trim() ?? "";
        if (edited.StartsWith("```"))
            edited = Regex.Replace(edited, @"^```[a-z]*\r?\n?|```$", "", RegexOptions.Multiline).Trim();
        result.Object = edited;
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
    List<string> RecommendedBuiltInToolNames,
    List<RecommendedToolDto> RecommendedTools,
    List<string> Warnings);

public sealed record RecommendedToolDto(
    string LibraryKey, string ToolName, string Reason, string Source, string Risk, string Description);

// Shape the model is asked to return (validated and converted to GenerateAgentResult).
public sealed class LlmAgentDesign
{
    public string SystemPrompt { get; set; }
    public List<string> RecommendedLibraryKeys { get; set; }
    public List<string> RecommendedBuiltInToolNames { get; set; }
    public List<LlmToolPick> RecommendedTools { get; set; }
}

public sealed class LlmToolPick
{
    public string LibraryKey { get; set; }
    public string ToolName { get; set; }
    public string Reason { get; set; }
}

public sealed class EditSystemPromptRequest
{
    public string CurrentPrompt { get; set; }
    public string Instruction   { get; set; }
}
