using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

public sealed class TestPlmConnectionTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmConnectionTestRequestDto
        {
            DataSourceRegisterId = PlmBlToolArgs.ParseInt(args, "dataSourceRegisterId"),
            TargetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId")
        };
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.TestPlmConnection(request)));
    }
}

/// <summary>List tenant AppDataSourceRegister rows for ask_user (ids + names only; no connection strings).</summary>
public sealed class ListTenantDataSourcesTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmListTenantDataSourcesRequestDto
        {
            TargetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId")
        };
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.ListTenantDataSources(request)));
    }
}

/// <summary>List tenant SaaS Application packages for ask_user (SaasApplicationId + name only).</summary>
public sealed class ListTenantSaasApplicationsTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmListTenantSaasApplicationsRequestDto
        {
            TargetCompanyId = PlmBlToolArgs.ParseInt(args, "targetCompanyId")
        };
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.ListTenantSaasApplications(request)));
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
        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.GetActiveImportSession(targetCompanyId)));
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

        return Task.FromResult(PlmBlToolArgs.Serialize(PlmImportEngine.SaveImportSession(dto)));
    }
}

/// <summary>
/// Persist Agent Wizard checklist onto AppPlmImportSession.StepStateJson (agentWizardJson).
/// Call after every successful step so progress survives app restart.
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
        return Task.FromResult(PlmBlToolArgs.Serialize(
            PlmImportEngine.UpdateWizardProgress(sessionId, wizardJson, currentStepCode, targetCompanyId)));
    }
}

/// <summary>
/// Load Agent Wizard from AppPlmImportSession for resume after app restart.
/// Prefer this over WorkflowId-scoped shared context alone.
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
        return Task.FromResult(PlmBlToolArgs.Serialize(
            PlmImportEngine.GetWizardProgress(sessionId, targetCompanyId)));
    }
}
