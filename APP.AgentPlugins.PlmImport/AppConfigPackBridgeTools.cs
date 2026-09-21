using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// ExternalDll: convert DW blueprint JSON → AppConfigPack JSON (for review / Import Config).
/// Apply path: compose pack here → host/tenant calls AppConfigPackBL.Execute (or public steps).
/// </summary>
public sealed class BuildDwAppConfigPackTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        var mode = PlmBlToolArgs.GetString(args, "mode") ?? "Update";
        var saasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId");
        var blueprint = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json);
        var pack = DwBlueprintAppConfigPackBuilder.Build(blueprint, new DwBlueprintAppConfigPackBuilder.Options
        {
            SaasApplicationId = saasApplicationId,
            Mode = mode,
            IncludeSearchView = PlmBlToolArgs.ParseBool(args, "includeSearchView", true),
            IncludeNavigation = PlmBlToolArgs.ParseBool(args, "includeNavigation", true),
            IncludeTransactionGroup = PlmBlToolArgs.ParseBool(args, "includeTransactionGroup", true)
        });
        return Task.FromResult(PlmBlToolArgs.Serialize(pack));
    }
}

/// <summary>ExternalDll: convert Search Import blueprint JSON → AppConfigPack JSON.</summary>
public sealed class BuildSearchAppConfigPackTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        var saasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId");
        var blueprint = string.IsNullOrWhiteSpace(json)
            ? null
            : JsonConvert.DeserializeObject<PlmSearchImportBlueprintDto>(json);
        var pack = SearchImportAppConfigPackBuilder.Build(blueprint, saasApplicationId);
        return Task.FromResult(PlmBlToolArgs.Serialize(pack));
    }
}
