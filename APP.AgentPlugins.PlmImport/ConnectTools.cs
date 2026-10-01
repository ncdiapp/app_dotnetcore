using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Legacy ExternalDll entry points. Prefer platform BuiltIn tools:
/// list_tenant_data_sources / list_tenant_saas_applications / test_data_source_connection
/// (TenantCatalogPlugin). Kept for in-process callers of PlmImportEngine.
/// </summary>
public sealed class TestPlmConnectionTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var registerId = PlmBlToolArgs.ParseInt(args, "dataSourceRegisterId") ?? 0;
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        return Task.FromResult(
            App.BL.TenantBusiness.TenantCatalogBL.TestDataSourceConnectionJson(registerId, targetCompanyId));
    }
}

/// <summary>Legacy — prefer platform-database.list_tenant_data_sources.</summary>
public sealed class ListTenantDataSourcesTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        return Task.FromResult(
            App.BL.TenantBusiness.TenantCatalogBL.ListTenantDataSourcesJson(targetCompanyId));
    }
}

/// <summary>Legacy — prefer platform-application.list_tenant_saas_applications.</summary>
public sealed class ListTenantSaasApplicationsTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        return Task.FromResult(
            App.BL.TenantBusiness.TenantCatalogBL.ListTenantSaasApplicationsJson(targetCompanyId));
    }
}

/// <summary>
/// Apply TechPack Tchp* DDL from embedded Sql/TechPack scripts (full NewSchema; optional InspectionAddon).
/// </summary>
public sealed class EnsureTechPackSchemaTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmEnsureTechPackSchemaRequestDto
        {
            IncludeInspectionAddon = PlmBlToolArgs.ParseBool(args, "includeInspectionAddon", defaultValue: false),
            TargetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId")
        };
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.EnsureTechPackSchema(request)));
    }
}

public sealed class GetPlmImportSessionTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        var chatSessionKey = PlmBlToolArgs.GetString(args, "chatSessionKey")
            ?? context?.ChatSessionKey;
        return Task.FromResult(PlmBlToolArgs.Serialize(
            PlmImportEngine.GetImportSessionForChat(chatSessionKey, targetCompanyId)));
    }
}

public sealed class SavePlmImportSessionTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionJson = PlmBlToolArgs.GetString(args, "sessionJson");
        var dto = string.IsNullOrWhiteSpace(sessionJson)
            ? null
            : JsonConvert.DeserializeObject<PlmImportSessionDto>(sessionJson);

        // Allow flat args as an alternative to sessionJson for Connect.
        if (dto == null)
            dto = new PlmImportSessionDto();

        var saas = PlmBlToolArgs.ParseInt(args, "saasApplicationId");
        if (saas.HasValue) dto.SaasApplicationId = saas;

        var plmReg = PlmBlToolArgs.ParseInt(args, "plmDataSourceRegisterId");
        if (plmReg.HasValue) dto.PlmDataSourceRegisterId = plmReg;

        var dwReg = PlmBlToolArgs.ParseInt(args, "plmDwDataSourceRegisterId");
        if (dwReg.HasValue) dto.PlmDwDataSourceRegisterId = dwReg;

        var erpReg = PlmBlToolArgs.ParseInt(args, "erpDataSourceRegisterId");
        if (erpReg.HasValue) dto.ErpDataSourceRegisterId = erpReg;

        var exDbReg = PlmBlToolArgs.ParseInt(args, "plmExDbDataSourceRegisterId");
        if (exDbReg.HasValue) dto.PlmExDbDataSourceRegisterId = exDbReg;

        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        if (sessionId.HasValue) dto.SessionId = sessionId;

        var companyId = PlmBlToolArgs.ParseInt(args, "companyId")
            ?? PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        if (companyId.HasValue) dto.CompanyId = companyId;

        // Strip any connection string the model may have put in sessionJson.
        dto.PlmConnectionString = null;

        if (string.IsNullOrWhiteSpace(dto.ChatSessionKey))
            dto.ChatSessionKey = PlmBlToolArgs.GetString(args, "chatSessionKey")
                ?? context?.ChatSessionKey;

        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.SaveImportSession(dto)));
    }
}

/// <summary>
/// Persist wizard onto AppAgentSharedContext (ScopeId = current ChatSessionKey).
/// Survives restart; does not require AppPlmImportSession.
/// </summary>
public sealed class UpdatePlmWizardProgressTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var wizardJson = PlmBlToolArgs.GetString(args, "wizardJson");
        var currentStepCode = PlmBlToolArgs.GetString(args, "currentStepCode");
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        var chatSessionKey = PlmBlToolArgs.GetString(args, "chatSessionKey")
            ?? context?.ChatSessionKey;
        return Task.FromResult(PlmBlToolArgs.Serialize(
            PlmImportEngine.UpdateWizardProgress(
                sessionId, wizardJson, currentStepCode, targetCompanyId, chatSessionKey, context?.WorkflowId)));
    }
}

/// <summary>
/// Load wizard for this Chat from AppAgentSharedContext (ScopeId = ChatSessionKey).
/// </summary>
public sealed class GetPlmWizardProgressTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var sessionId = PlmBlToolArgs.ParseInt(args, "sessionId");
        var targetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId");
        var chatSessionKey = PlmBlToolArgs.GetString(args, "chatSessionKey")
            ?? context?.ChatSessionKey;
        return Task.FromResult(PlmBlToolArgs.Serialize(
            PlmImportEngine.GetWizardProgress(sessionId, targetCompanyId, chatSessionKey)));
    }
}
