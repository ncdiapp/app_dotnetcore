using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>ExternalDll: load Search Import blueprint from JSON.</summary>
public sealed class LoadSearchBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmSearchImportLoadRequestDto
        {
            BlueprintJson = PlmBlToolArgs.GetString(args, "blueprintJson")
        };
        var result = PlmImportEngine.LoadSearchImportBlueprint(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll: preview Search Import blueprint.</summary>
public sealed class PreviewSearchBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var blueprint = DeserializeSearchBlueprint(args);
        var result = PlmImportEngine.PreviewSearchBlueprintConfig(blueprint);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }

    internal static PlmSearchImportBlueprintDto? DeserializeSearchBlueprint(IReadOnlyDictionary<string, string> args)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonConvert.DeserializeObject<PlmSearchImportBlueprintDto>(json);
    }
}

/// <summary>ExternalDll: execute Search Import blueprint.</summary>
public sealed class ExecuteSearchBlueprintTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmSearchImportExecuteRequestDto
        {
            Blueprint = PreviewSearchBlueprintTool.DeserializeSearchBlueprint(args),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId")
        };
        var result = PlmImportEngine.ExecuteSearchBlueprintConfig(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll: preview Search Sibling View blueprint.</summary>
public sealed class PreviewSearchSiblingViewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var blueprint = DeserializeSibling(args);
        var result = PlmImportEngine.PreviewSearchSiblingViewConfig(blueprint);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }

    internal static PlmSearchSiblingViewBlueprintDto? DeserializeSibling(IReadOnlyDictionary<string, string> args)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonConvert.DeserializeObject<PlmSearchSiblingViewBlueprintDto>(json);
    }
}

/// <summary>ExternalDll: execute Search Sibling View blueprint.</summary>
public sealed class ExecuteSearchSiblingViewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmSearchSiblingViewExecuteRequestDto
        {
            Blueprint = PreviewSearchSiblingViewTool.DeserializeSibling(args),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId")
        };
        var result = PlmImportEngine.ExecuteSearchSiblingViewConfig(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}

/// <summary>ExternalDll: preview Search MassUpdate View blueprint.</summary>
public sealed class PreviewSearchMassUpdateViewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var blueprint = DeserializeMassUpdate(args);
        var result = PlmImportEngine.PreviewSearchMassUpdateViewConfig(blueprint);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }

    internal static PlmSearchMassUpdateViewBlueprintDto? DeserializeMassUpdate(IReadOnlyDictionary<string, string> args)
    {
        var json = PlmBlToolArgs.GetString(args, "blueprintJson");
        if (string.IsNullOrWhiteSpace(json))
            return null;
        return JsonConvert.DeserializeObject<PlmSearchMassUpdateViewBlueprintDto>(json);
    }
}

/// <summary>ExternalDll: execute Search MassUpdate View blueprint.</summary>
public sealed class ExecuteSearchMassUpdateViewTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        var request = new PlmSearchMassUpdateViewExecuteRequestDto
        {
            Blueprint = PreviewSearchMassUpdateViewTool.DeserializeMassUpdate(args),
            SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId")
        };
        var result = PlmImportEngine.ExecuteSearchMassUpdateViewConfig(request);
        return Task.FromResult(PlmBlToolArgs.Serialize(result));
    }
}
