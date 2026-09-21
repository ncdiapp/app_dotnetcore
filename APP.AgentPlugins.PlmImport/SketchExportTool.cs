using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.AgentPlugins.PlmImport;

/// <summary>
/// Optional Agent-facing ExternalDll: export tblSketch binaries to a staging folder.
/// Prefer Host job path (BuiltIn execute → bridge → SketchImportExporter) for large imports.
///
/// ToolConfig:
///   { "AssemblyName": "APP.AgentPlugins.PlmImport.dll", "TypeName": "APP.AgentPlugins.PlmImport.SketchExportTool" }
/// Args: plmConnectionString, stagingDirectory, skipFileIdsCsv (optional)
/// </summary>
public sealed class SketchExportTool : IAgentTool
{
    public Task<string> ExecuteAsync(
        IReadOnlyDictionary<string, string> args,
        AgentToolContext context,
        CancellationToken cancellationToken)
    {
        args.TryGetValue("plmConnectionString", out var plm);
        args.TryGetValue("stagingDirectory", out var staging);
        args.TryGetValue("skipFileIdsCsv", out var skipCsv);

        int[] skip = ParseIds(skipCsv);
        try
        {
            var manifest = SketchImportExporter.Export(
                plm,
                staging,
                skip,
                (pct, msg) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                });
            return Task.FromResult(JsonConvert.SerializeObject(manifest));
        }
        catch (System.Exception ex)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new APP.Components.EntityDto.PlmSketchStagingManifestDto
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            }));
        }
    }

    private static int[] ParseIds(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return System.Array.Empty<int>();
        var list = new List<int>();
        foreach (var part in csv.Split(','))
        {
            if (int.TryParse(part.Trim(), out int id))
                list.Add(id);
        }
        return list.ToArray();
    }
}
