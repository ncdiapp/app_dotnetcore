using System;
using System.Linq;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Prepare polish inputs (templates + ≤10 sample rows + schema) and cache AiPolishedRenderSpec.
    /// Iron rule: never merge Result data into HTML here; never return a painted page.
    /// </summary>
    public class AgentUiPolishPlugin
    {
        /// <summary>
        /// Returns library TemplateContent for interactive + none-interactive, field schema,
        /// and at most 10 sample rows. Model must polish structure only, then call cache_ui_render_polish
        /// and/or pass aiPolishedRenderSpec into render_*.
        /// </summary>
        public Task<string> PrepareUiRenderPolish(
            AgentToolContext context,
            string dataset_name = null,
            string layout = null,
            string user_prompt = null)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return Task.FromResult(Err("Chat session is required."));

                layout = UiTemplateMerge.NormalizeLayout(layout);
                if (string.IsNullOrWhiteSpace(layout)) layout = "cards";

                if (string.IsNullOrWhiteSpace(dataset_name))
                    dataset_name = AgentDataAnalyzeCacheBL.Latest(context.ChatSessionKey)?.Name;
                if (string.IsNullOrWhiteSpace(dataset_name))
                    return Task.FromResult(Err("dataset_name is required (or cache a dataset first)."));

                if (!AgentDataAnalyzeCacheBL.TryExportDataJson(
                        context.ChatSessionKey, dataset_name.Trim(), out var dataJson, out var columnsJson, out var rowCount))
                    return Task.FromResult(Err("Dataset not found in session cache: " + dataset_name));

                var rows = JArray.Parse(dataJson);
                var sample = AgentDataAnalyzeCacheBL.TakeFirst(rows, AgentAiPolishCacheBL.SampleMaxRows);

                UiTemplateContentMaterialize.EnsureOnce();
                var interactiveMap = LoadTemplateMap(UiTemplateMerge.InteractiveTool);
                var noneMap = LoadTemplateMap(UiTemplateMerge.NoneInteractiveTool);

                // Only the layout key bodies — keep payload small
                var interactiveBody = SliceLayout(interactiveMap, layout);
                var noneBody = SliceLayout(noneMap, layout);

                var contract = new JObject
                {
                    ["rule"] = "Return AiPolishedRenderSpec: polished TemplateContent fragments + bindings (field names/roles/columns/filters). Do NOT merge sample or full rows into HTML. Do NOT emit a Result page.",
                    ["renderTools"] = new JArray("render_interactive", "render_none_interactive"),
                    ["cacheTool"] = "cache_ui_render_polish",
                    ["sampleMaxRows"] = AgentAiPolishCacheBL.SampleMaxRows,
                    ["maxDisplayFields"] = AiPolishedRenderSpec.MaxDisplayFields,
                    ["maxColumnsDefault"] = AiPolishedRenderSpec.MaxColumnsDefault,
                    ["selectAllColumns"] = "Set true only when the user asked for all columns."
                };

                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataset_name = AgentDataAnalyzeCacheBL.SanitizeName(dataset_name),
                    layout,
                    rowCount,
                    sampleRowCount = sample?.Count ?? 0,
                    columnsJson = JToken.Parse(columnsJson ?? "[]"),
                    sampleRows = sample,
                    templateContentInteractive = interactiveBody,
                    templateContentNoneInteractive = noneBody,
                    user_prompt = string.IsNullOrWhiteSpace(user_prompt) ? null : user_prompt.Trim(),
                    promptHash = AgentAiPolishCacheBL.HashPrompt(user_prompt),
                    contract,
                    note = "Iron rule: polish TemplateContent + field structure only. GENERATE tools bind full N rows. Never paint pages from data in the model."
                }, Formatting.None));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Err(ex.Message));
            }
        }

        /// <summary>Validate and store session polish. Does not write library TemplateContent.</summary>
        public Task<string> CacheUiRenderPolish(
            AgentToolContext context,
            string aiPolishedRenderSpec = null,
            string dataset_name = null,
            string layout = null,
            string promptHash = null)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return Task.FromResult(Err("Chat session is required."));
                if (!AiPolishedRenderSpec.TryParse(aiPolishedRenderSpec, out var spec, out var err))
                    return Task.FromResult(Err(err));

                layout = UiTemplateMerge.NormalizeLayout(
                    !string.IsNullOrWhiteSpace(layout) ? layout : spec.Layout);
                if (string.IsNullOrWhiteSpace(layout)) layout = "cards";
                spec.Layout = layout;

                if (string.IsNullOrWhiteSpace(dataset_name))
                    dataset_name = AgentDataAnalyzeCacheBL.Latest(context.ChatSessionKey)?.Name;
                if (string.IsNullOrWhiteSpace(dataset_name))
                    return Task.FromResult(Err("dataset_name is required."));

                AgentAiPolishCacheBL.Put(
                    context.ChatSessionKey, dataset_name.Trim(), layout, spec, promptHash);

                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataset_name = AgentDataAnalyzeCacheBL.SanitizeName(dataset_name),
                    layout,
                    promptHash = string.IsNullOrWhiteSpace(promptHash) ? "default" : promptHash,
                    bindings = spec.ToClientJson()["bindings"],
                    layoutHints = spec.ToClientJson()["layoutHints"],
                    hasInteractiveTemplate = spec.TemplateContentInteractive != null,
                    hasNoneInteractiveTemplate = spec.TemplateContentNoneInteractive != null,
                    note = "Session polish cached. Call render_interactive / render_none_interactive with dataset_name — GENERATE binds full rows. Not written to library."
                }, Formatting.None));
            }
            catch (Exception ex)
            {
                return Task.FromResult(Err(ex.Message));
            }
        }

        private static JObject LoadTemplateMap(string toolName)
        {
            try
            {
                var tool = App.BL.TenantBusiness.AppAgentLibraryToolBL
                    .GetByLibraryAndToolName(UiTemplateMerge.LibraryKey, toolName);
                if (tool == null || string.IsNullOrWhiteSpace(tool.TemplateContent))
                    return new JObject();
                return JObject.Parse(tool.TemplateContent);
            }
            catch
            {
                return new JObject();
            }
        }

        private static JObject SliceLayout(JObject map, string layout)
        {
            var body = map?[layout]?.ToString();
            if (string.IsNullOrWhiteSpace(body))
                body = UiTemplateMerge.CodeDefaultMarker;
            // Cap template text size for LLM context
            if (body.Length > 12000)
                body = body.Substring(0, 12000) + "\n<!-- truncated for polish context -->";
            return new JObject { [layout] = body };
        }

        private static string Err(string message)
            => JsonConvert.SerializeObject(new { ok = false, error = message });
    }
}
