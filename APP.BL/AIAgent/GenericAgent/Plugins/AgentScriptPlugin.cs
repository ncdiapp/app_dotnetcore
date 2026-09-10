using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn: run a user-provided script under AgentOutput/{ChatSessionKey}/ (sandboxed).
    /// ToolConfig may include AllowedPathPrefixes, AllowedExtensions, MaxTimeoutSeconds,
    /// InjectDataSourcesFromConfig (see GenericAgentProcessBL.ParsePolicy).
    /// </summary>
    public class AgentScriptPlugin
    {
        public async Task<string> Run(
            AgentToolContext context,
            CancellationToken ct,
            string relativePath,
            string toolConfig = null)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return JsonConvert.SerializeObject(new { Error = "ChatSessionKey is required for agent script tools." });
                if (context.CompanyId <= 0)
                    return JsonConvert.SerializeObject(new { Error = "CompanyId is required for agent script tools." });
                if (string.IsNullOrWhiteSpace(relativePath))
                    return JsonConvert.SerializeObject(new { Error = "relativePath is required (e.g. source/_gen_plmdw_import_sql.ps1)." });

                var result = await GenericAgentProcessBL.RunScriptAsync(
                    context.ChatSessionKey,
                    context.CompanyId,
                    relativePath.Trim(),
                    toolConfig,
                    ct).ConfigureAwait(false);

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Ok = false, Error = ex.Message });
            }
        }

        /// <summary>
        /// Optional helper: check output file sizes. minSizeByFileNameJson e.g.
        /// {"4_PlmDw_ImportBlueprint.json":500000,"1_PlmDw_Tables.sql":400000}
        /// </summary>
        public Task<string> ValidateOutputs(
            AgentToolContext context,
            CancellationToken ct,
            string relativeDir = "output",
            string minSizeByFileNameJson = null)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "ChatSessionKey is required." }));

                var mins = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(minSizeByFileNameJson))
                {
                    try
                    {
                        mins = JsonConvert.DeserializeObject<Dictionary<string, long>>(minSizeByFileNameJson)
                               ?? mins;
                    }
                    catch { /* ignore bad json */ }
                }

                var dir = string.IsNullOrWhiteSpace(relativeDir) ? "output" : relativeDir.Trim().TrimStart('/');
                var files = GenericAgentFileBL.List(context.ChatSessionKey, dir, context.CompanyId);
                var flat = new List<object>();
                var errors = new List<string>();

                void CheckFile(APP.Components.EntityDto.GenericAgentFileDto f)
                {
                    if (f.IsDirectory) return;
                    var name = System.IO.Path.GetFileName(f.RelativePath);
                    flat.Add(new { f.RelativePath, f.SizeBytes });
                    if (mins.TryGetValue(name, out var min) && f.SizeBytes < min)
                        errors.Add(f.RelativePath + " is too small (" + f.SizeBytes + " < " + min + ")");
                }

                foreach (var f in files)
                {
                    if (f.IsDirectory)
                    {
                        foreach (var n in GenericAgentFileBL.List(context.ChatSessionKey, f.RelativePath, context.CompanyId))
                            CheckFile(n);
                    }
                    else CheckFile(f);
                }

                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    Ok = errors.Count == 0,
                    RelativeDir = dir,
                    Files = flat,
                    Errors = errors
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Ok = false, Error = ex.Message }));
            }
        }
    }
}
