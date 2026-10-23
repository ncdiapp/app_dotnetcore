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
    /// Prepare polish inputs (templates + sample rows + fill-rates + schema) and cache AiPolishedRenderSpec.
    /// Iron rule: never merge Result data into HTML here; never return a painted page.
    /// </summary>
    public class AgentUiPolishPlugin
    {
        /// <summary>
        /// Returns library TemplateContent for interactive + none-interactive, field schema,
        /// sample rows (≤ SampleMaxRows), and per-column fill rates. Model polishes structure only.
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
                var colNames = JArray.Parse(columnsJson ?? "[]")
                    .Select(t => t?.Type == JTokenType.String
                        ? t.ToString()
                        : (t as JObject)?["name"]?.ToString() ?? (t as JObject)?["Name"]?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (colNames.Count == 0 && sample?.Count > 0 && sample[0] is JObject first)
                    colNames = first.Properties().Select(p => p.Name).ToList();
                var sampleObjs = sample?.OfType<JObject>().ToList() ?? new System.Collections.Generic.List<JObject>();
                var fillRates = AiRenderPolishHeuristics.ComputeFillRates(colNames, sampleObjs);
                var columnFillRates = new JObject();
                foreach (var kv in fillRates)
                    columnFillRates[kv.Key] = Math.Round(kv.Value, 3);

                UiTemplateContentMaterialize.EnsureOnce();
                var interactiveMap = LoadTemplateMap(UiTemplateMerge.InteractiveTool);
                var noneMap = LoadTemplateMap(UiTemplateMerge.NoneInteractiveTool);

                // Only the layout key bodies — keep payload small
                var interactiveBody = SliceLayout(interactiveMap, layout);
                var noneBody = SliceLayout(noneMap, layout);

                var contract = new JObject
                {
                    ["rule"] = "Return AiPolishedRenderSpec: polished TemplateContent fragments + bindings (field names/roles/showLabel/label/columns/filters). Do NOT merge sample or full rows into HTML. Do NOT emit a Result page.",
                    ["renderTools"] = new JArray("render_interactive", "render_none_interactive"),
                    ["cacheTool"] = "cache_ui_render_polish",
                    ["sampleMaxRows"] = AgentAiPolishCacheBL.SampleMaxRows,
                    ["maxDisplayFields"] = AiPolishedRenderSpec.MaxDisplayFields,
                    ["maxColumnsDefault"] = AiPolishedRenderSpec.MaxColumnsDefault,
                    ["minFillRate"] = AiRenderPolishHeuristics.MinFillRate,
                    ["selectAllColumns"] = "Set true only when the user asked for all columns.",
                    ["fieldSelection"] = "Prefer name/theme fit to the dataset subject. Reject columns with low non-empty rate in sample even if the name fits. Max "
                        + AiPolishedRenderSpec.MaxDisplayFields + " fields + optional primaryImageField.",
                    ["showLabelRule"] = "Set showLabel=true when multiple similar values on the same card would be ambiguous (several dates, several positions like 左上/右上, etc.). A single clear field of that kind → showLabel=false. Obscure business columns → showLabel=true with a short label. Clear ID/Code/Name/Status → showLabel=false. Labels must be concise: Created Date→Created, Revised Date→Revised (drop redundant Date/Time words).",
                    ["compactRule"] = "Set compact=true when the displayed value (plus short label if any) is short enough to share a meta row with siblings (e.g. S/S 2025, Men's, Ref #523, Created Jan 15, 2025). Long descriptions stay compact=false (full line).",
                    ["columnHeaderRule"] = "For layout=table (and page grid): bindings.columns MUST be objects {field,header,format}. header is a short human label you author (required for table), in the user's language when appropriate. Never leave long underscore field names as headers.",
                    ["columnFormatRule"] = "YOU classify each column's format from the field name PLUS sample cell values (not English keyword matching alone — works for any language). Use exactly: delta (signed variance / day lag — Result shows red↓/green↑), percent, date, image (URLs or file ids), number, currency. Plain text/code/name → omit format. Look at sample values: ISO dates → date; http(s) image URLs → image; signed small integers next to Target/Actual pairs → delta; 0–100 with % → percent.",
                    ["fieldBindingShape"] = new JObject
                    {
                        ["name"] = "exact column name",
                        ["role"] = "idLink|title|statusPill|colorSwatch|meta (optional)",
                        ["showLabel"] = "boolean",
                        ["label"] = "optional short label when showLabel=true (Created not Created Date)",
                        ["compact"] = "boolean — short chips share a row"
                    },
                    ["columnBindingShape"] = new JObject
                    {
                        ["field"] = "exact column name from sample",
                        ["header"] = "short display header (LLM required for table)",
                        ["format"] = "optional — YOU decide from field+samples: delta|percent|date|image|number|currency"
                    }
                };

                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    dataset_name = AgentDataAnalyzeCacheBL.SanitizeName(dataset_name),
                    layout,
                    rowCount,
                    sampleRowCount = sample?.Count ?? 0,
                    columnsJson = JToken.Parse(columnsJson ?? "[]"),
                    columnFillRates,
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
