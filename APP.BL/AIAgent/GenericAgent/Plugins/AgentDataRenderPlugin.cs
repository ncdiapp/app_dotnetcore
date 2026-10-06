using System;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// data_render — non-blocking UI panel in Agent Chat (grid | card | chart | kpi_dashboard).
    /// Emits <see cref="AgentDataRenderEvent"/> via <see cref="AgentHitlBridge"/> / OnDataRender,
    /// then returns immediately. Rich payload rides on SSE so MaxToolResultChars cannot truncate it.
    /// Prefer <c>dataset_name</c> (session cache from a prior tool) so the grid gets ALL columns,
    /// including nulls — do not pass an LLM-curated column subset via dataJson.
    /// </summary>
    public class AgentDataRenderPlugin
    {
        private const int MaxRows = 500;
        private const int MaxKpiItems = 12;
        private const int MaxChartBlocks = 9;
        private const int MaxGridBlocks = 3;

        public async System.Threading.Tasks.Task<string> Render(
            string ui,
            AgentToolContext context,
            string dataJson = null,
            string dataset_name = null,
            string title = null,
            string columnsJson = null,
            string chartConfigJson = null,
            string actionsJson = null,
            string metaJson = null,
            string blocksJson = null)
        {
            var uiNorm = (ui ?? "").Trim().ToLowerInvariant();
            if (uiNorm != "grid" && uiNorm != "card" && uiNorm != "chart" && uiNorm != "kpi_dashboard")
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "ui must be grid, card, chart, or kpi_dashboard."
                });
            }

            string cappedDataJson = null;
            string normalizedBlocksJson = null;
            int rowCount = 0;
            bool truncated = false;
            string resolvedFromDataset = null;

            // Full tabular payload from session cache (includes every column / null cell).
            if (!string.IsNullOrWhiteSpace(dataset_name)
                && context != null
                && !string.IsNullOrWhiteSpace(context.ChatSessionKey)
                && (uiNorm == "grid" || uiNorm == "card" || uiNorm == "chart"))
            {
                if (AgentDataAnalyzeCacheBL.TryExportDataJson(
                        context.ChatSessionKey, dataset_name.Trim(),
                        out var exportedData, out var exportedCols, out var exportedRows))
                {
                    dataJson = exportedData;
                    // Always use full column list from cache — ignore LLM subset columnsJson.
                    columnsJson = exportedCols;
                    resolvedFromDataset = AgentDataAnalyzeCacheBL.SanitizeName(dataset_name);
                    rowCount = exportedRows;
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"dataset_name '{dataset_name}' not found in this chat session. " +
                                "Run a query/SP tool first (look for dataset_cached / dataset_name on the tool result)."
                    });
                }
            }

            if (uiNorm == "kpi_dashboard")
            {
                var dash = NormalizeDashboardBlocks(blocksJson, dataJson);
                if (!dash.Ok)
                {
                    return JsonConvert.SerializeObject(new { ok = false, error = dash.Error });
                }
                normalizedBlocksJson = dash.BlocksJson;
                rowCount = dash.RowCount;
                truncated = dash.Truncated;
                cappedDataJson = dash.SummaryDataJson;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dataJson))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "dataJson is required (or pass dataset_name from a prior tool that set dataset_cached)."
                    });
                }

                cappedDataJson = dataJson.Trim();
                try
                {
                    var token = JToken.Parse(cappedDataJson);
                    if (token is JArray arr)
                    {
                        rowCount = arr.Count;
                        if (arr.Count > MaxRows)
                        {
                            truncated = true;
                            var slice = new JArray();
                            for (int i = 0; i < MaxRows; i++)
                                slice.Add(arr[i]);
                            cappedDataJson = slice.ToString(Formatting.None);
                            rowCount = MaxRows;
                        }
                    }
                    else if (token is JObject)
                    {
                        rowCount = 1;
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            ok = false,
                            error = "dataJson must be a JSON array or object."
                        });
                    }
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "dataJson is not valid JSON: " + ex.Message
                    });
                }
            }

            columnsJson = NormalizeOptionalJson(columnsJson);
            chartConfigJson = NormalizeOptionalJson(chartConfigJson);
            actionsJson = NormalizeOptionalJson(actionsJson);
            metaJson = NormalizeOptionalJson(metaJson);

            var renderId = Guid.NewGuid().ToString("N");
            var evt = new AgentDataRenderEvent
            {
                RenderId = renderId,
                Ui = uiNorm,
                Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                DataJson = cappedDataJson,
                ColumnsJson = columnsJson,
                ChartConfigJson = chartConfigJson,
                ActionsJson = actionsJson,
                MetaJson = metaJson,
                BlocksJson = normalizedBlocksJson,
                RowCount = rowCount,
                Truncated = truncated,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var onRender = AgentHitlBridge.Current?.OnDataRender;
            if (onRender != null)
            {
                try
                {
                    await onRender(evt).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "data_render callback error: " + ex.Message
                    });
                }
            }

            return JsonConvert.SerializeObject(new
            {
                ok = true,
                renderId,
                ui = uiNorm,
                rowCount,
                truncated,
                dataset_name = resolvedFromDataset,
                columnCount = CountColumns(columnsJson),
                blockCount = uiNorm == "kpi_dashboard" ? CountBlocks(normalizedBlocksJson) : (int?)null,
                pushed = onRender != null
            });
        }

        private static int? CountColumns(string columnsJson)
        {
            if (string.IsNullOrWhiteSpace(columnsJson)) return null;
            try
            {
                if (JToken.Parse(columnsJson) is JArray arr) return arr.Count;
            }
            catch { /* ignore */ }
            return null;
        }
        private sealed class DashboardNormalizeResult
        {
            public bool Ok;
            public string Error;
            public string BlocksJson;
            public string SummaryDataJson;
            public int RowCount;
            public bool Truncated;
        }

        private static DashboardNormalizeResult NormalizeDashboardBlocks(string blocksJson, string dataJsonFallback)
        {
            var result = new DashboardNormalizeResult();
            string raw = !string.IsNullOrWhiteSpace(blocksJson) ? blocksJson.Trim() : null;
            if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(dataJsonFallback))
            {
                // Allow passing blocks as dataJson when blocksJson omitted.
                try
                {
                    var t = JToken.Parse(dataJsonFallback.Trim());
                    if (t is JArray) raw = dataJsonFallback.Trim();
                    else if (t is JObject obj && obj["blocks"] is JArray)
                        raw = obj["blocks"].ToString(Formatting.None);
                }
                catch { /* fall through */ }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                result.Error = "kpi_dashboard requires blocksJson (JSON array of blocks).";
                return result;
            }

            JArray blocks;
            try
            {
                var token = JToken.Parse(raw);
                blocks = token as JArray;
                if (blocks == null)
                {
                    result.Error = "blocksJson must be a JSON array.";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Error = "blocksJson is not valid JSON: " + ex.Message;
                return result;
            }

            if (blocks.Count == 0)
            {
                result.Error = "blocksJson must contain at least one block.";
                return result;
            }

            int chartCount = 0;
            int gridCount = 0;
            int totalRows = 0;
            bool truncated = false;
            var outBlocks = new JArray();

            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is not JObject block)
                {
                    result.Error = "blocksJson[" + i + "] must be an object.";
                    return result;
                }

                var type = (block.Value<string>("type") ?? block.Value<string>("Type") ?? "").Trim().ToLowerInvariant();
                if (type != "markdown" && type != "kpi" && type != "chart" && type != "grid" && type != "card")
                {
                    result.Error = "blocksJson[" + i + "].type must be markdown|kpi|chart|grid|card.";
                    return result;
                }

                var copy = (JObject)block.DeepClone();
                copy["type"] = type;
                if (string.IsNullOrWhiteSpace(copy.Value<string>("id")))
                    copy["id"] = "b" + (i + 1);

                if (type == "kpi")
                {
                    var items = copy["items"] as JArray ?? copy["Items"] as JArray;
                    if (items == null || items.Count == 0)
                    {
                        result.Error = "kpi block requires non-empty items[].";
                        return result;
                    }
                    if (items.Count > MaxKpiItems)
                    {
                        truncated = true;
                        var slice = new JArray();
                        for (int k = 0; k < MaxKpiItems; k++)
                            slice.Add(items[k]);
                        copy["items"] = slice;
                    }
                }
                else if (type == "chart")
                {
                    chartCount++;
                    if (chartCount > MaxChartBlocks)
                    {
                        result.Error = "kpi_dashboard allows at most " + MaxChartBlocks + " chart blocks.";
                        return result;
                    }
                    CapBlockData(copy, ref totalRows, ref truncated);
                }
                else if (type == "grid")
                {
                    gridCount++;
                    if (gridCount > MaxGridBlocks)
                    {
                        result.Error = "kpi_dashboard allows at most " + MaxGridBlocks + " grid blocks.";
                        return result;
                    }
                    CapBlockData(copy, ref totalRows, ref truncated);
                }
                else if (type == "card")
                {
                    totalRows += 1;
                }
                else if (type == "markdown")
                {
                    var content = copy.Value<string>("content") ?? copy.Value<string>("Content") ?? "";
                    if (content.Length > 12000)
                    {
                        truncated = true;
                        copy["content"] = content.Substring(0, 12000) + "\n…";
                    }
                }

                // Soft-normalize nested actions
                if (copy["actions"] != null)
                    copy["actions"] = NormalizeActionsToken(copy["actions"]);
                if (copy["Actions"] != null)
                    copy["actions"] = NormalizeActionsToken(copy["Actions"]);

                outBlocks.Add(copy);
            }

            result.Ok = true;
            result.BlocksJson = outBlocks.ToString(Formatting.None);
            result.RowCount = totalRows;
            result.Truncated = truncated;
            result.SummaryDataJson = new JObject
            {
                ["blockCount"] = outBlocks.Count,
                ["chartCount"] = chartCount,
                ["gridCount"] = gridCount
            }.ToString(Formatting.None);
            return result;
        }

        private static void CapBlockData(JObject block, ref int totalRows, ref bool truncated)
        {
            JToken data = block["data"] ?? block["Data"];
            if (data == null && block["dataJson"] != null)
            {
                try { data = JToken.Parse(block.Value<string>("dataJson") ?? "[]"); }
                catch { data = null; }
            }
            if (data is JArray arr)
            {
                totalRows += Math.Min(arr.Count, MaxRows);
                if (arr.Count > MaxRows)
                {
                    truncated = true;
                    var slice = new JArray();
                    for (int i = 0; i < MaxRows; i++)
                        slice.Add(arr[i]);
                    block["data"] = slice;
                }
                else
                {
                    block["data"] = arr;
                }
            }
            else if (data != null)
            {
                block["data"] = data;
                totalRows += 1;
            }
        }

        private static JToken NormalizeActionsToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token is JArray) return token;
            if (token.Type == JTokenType.String)
            {
                try { return JToken.Parse(token.Value<string>() ?? "[]"); }
                catch { return token; }
            }
            return token;
        }

        private static int CountBlocks(string blocksJson)
        {
            if (string.IsNullOrWhiteSpace(blocksJson)) return 0;
            try
            {
                var arr = JArray.Parse(blocksJson);
                return arr.Count;
            }
            catch { return 0; }
        }

        private static string NormalizeOptionalJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var t = raw.Trim();
            try
            {
                JToken.Parse(t);
                return t;
            }
            catch
            {
                return t;
            }
        }
    }
}
