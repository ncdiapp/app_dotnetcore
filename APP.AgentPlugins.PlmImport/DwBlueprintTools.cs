using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>ExternalDll: load DW blueprint from JSON string.</summary>
public sealed class LoadDwBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmDwBlueprintLoadRequestDto
        {
            BlueprintJson = PlmBlToolArgs.GetString(args, "blueprintJson"),
            TablePrefix = PlmBlToolArgs.GetString(args, "tablePrefix")
        };
        var result = PlmImportEngine.LoadDwImportBlueprint(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll: load DW blueprint from tenant ImportBlueprint table.</summary>
public sealed class LoadDwBlueprintFromTableTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var tablePrefix = PlmBlToolArgs.GetString(args, "tablePrefix") ?? "Tchp";
        var blueprintKey = PlmBlToolArgs.GetString(args, "blueprintKey") ?? "default";
        var result = PlmImportEngine.LoadDwImportBlueprintFromTenantTable(tablePrefix, blueprintKey);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll: preview DW blueprint apply plan.</summary>
public sealed class PreviewDwBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var blueprint = DeserializeBlueprint(args);
        var result = PlmImportEngine.PreviewDwBlueprintConfig(blueprint);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }

    internal static PlmDwImportBlueprintDto? DeserializeBlueprint(IReadOnlyDictionary<string, string> args)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json);
    }
}

/// <summary>ExternalDll: execute DW blueprint (Insert|Update|Repair).</summary>
public sealed class ExecuteDwBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmDwBlueprintExecuteRequestDto
        {
            Blueprint = PreviewDwBlueprintTool.DeserializeBlueprint(args),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId"),
            Mode = PlmBlToolArgs.GetString(args, "mode") ?? "Insert",
            IncludeSearchView = PlmBlToolArgs.ParseBool(args, "includeSearchView", true),
            IncludeNavigation = PlmBlToolArgs.ParseBool(args, "includeNavigation", true),
            IncludeTransactionGroup = PlmBlToolArgs.ParseBool(args, "includeTransactionGroup", true)
        };
        var result = PlmImportEngine.ExecuteDwBlueprintConfig(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
