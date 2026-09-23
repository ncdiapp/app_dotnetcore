using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using App.BL.AIAgent.GenericAgent;
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

/// <summary>ExternalDll: preview DW blueprint from an AgentOutput file path (does not load JSON into the LLM).</summary>
public sealed class PreviewDwBlueprintFromFileTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = PlmBlToolArgs.GetString(args, "relativePath");
            var json = AgentOutputPathReader.ReadText(context, path);
            var blueprint = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json);
            var result = PlmImportEngine.PreviewDwBlueprintConfig(blueprint);
            var preview = result?.Object;
            return Task.FromResult(PlmBlToolArgs.Serialize(new
            {
                Ok = preview?.IsSuccess == true,
                Path = path,
                Error = preview?.ErrorMessage
                    ?? result?.ValidationResult?.Items?.FirstOrDefault()?.Message,
                ItemCount = preview?.Items?.Count ?? 0,
                Items = (preview?.Items ?? new List<PlmDwBlueprintPreviewItemDto>())
                    .Take(40)
                    .Select(i => new { i.ObjectType, i.Name, i.IntegrationId, i.Action, i.ExistingId })
                    .ToList()
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(PlmBlToolArgs.Serialize(new
            {
                Ok = false,
                Path = PlmBlToolArgs.GetString(args, "relativePath"),
                Error = ex.Message
            }));
        }
    }
}

/// <summary>ExternalDll: execute DW blueprint from an AgentOutput file path (does not load JSON into the LLM).</summary>
public sealed class ExecuteDwBlueprintFromFileTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var path = PlmBlToolArgs.GetString(args, "relativePath");
            var json = AgentOutputPathReader.ReadText(context, path);
            var request = new PlmDwBlueprintExecuteRequestDto
            {
                Blueprint = JsonConvert.DeserializeObject<PlmDwImportBlueprintDto>(json),
                SaasApplicationId = PlmBlToolArgs.ParseInt(args, "saasApplicationId"),
                Mode = PlmBlToolArgs.GetString(args, "mode") ?? "Insert",
                IncludeSearchView = PlmBlToolArgs.ParseBool(args, "includeSearchView", true),
                IncludeNavigation = PlmBlToolArgs.ParseBool(args, "includeNavigation", true),
                IncludeTransactionGroup = PlmBlToolArgs.ParseBool(args, "includeTransactionGroup", true)
            };
            var result = PlmImportEngine.ExecuteDwBlueprintConfig(request);
            var exec = result?.Object;
            return Task.FromResult(PlmBlToolArgs.Serialize(new
            {
                Ok = exec?.IsSuccess == true,
                Path = path,
                Mode = request.Mode,
                Error = exec?.ErrorMessage
                    ?? result?.ValidationResult?.Items?.FirstOrDefault()?.Message,
                TransactionsInserted = exec?.TransactionsInserted ?? 0,
                TransactionsUpdated = exec?.TransactionsUpdated ?? 0,
                TransactionGroupId = exec?.TransactionGroupId,
                SearchId = exec?.SearchId,
                TransactionIds = exec?.TransactionIds,
                Messages = exec?.Messages
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(PlmBlToolArgs.Serialize(new
            {
                Ok = false,
                Path = PlmBlToolArgs.GetString(args, "relativePath"),
                Error = ex.Message
            }));
        }
    }
}
