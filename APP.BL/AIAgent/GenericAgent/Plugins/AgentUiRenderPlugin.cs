using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// render_none_interactive / render_interactive — C# merges templates into HTML for the Result iframe.
    /// Emits <see cref="AgentUiRenderEvent"/> via OnUiRender (SSE ui_render).
    /// </summary>
    public class AgentUiRenderPlugin
    {
        private const int MaxRows = 50000;

        public Task<string> RenderNoneInteractive(
            AgentToolContext context,
            string layout = null,
            string dataset_name = null,
            string title = null,
            string content = null,
            string specJson = null,
            int? limit = null,
            int? cardsPerRow = null,
            int? cardRows = null,
            string actionsJson = null)
            => RenderCore(context, layout, dataset_name, title, content, specJson, limit, cardsPerRow, cardRows, interactive: false, actionsJson: null, mappedFrom: null);

        public Task<string> RenderInteractive(
            AgentToolContext context,
            string layout = null,
            string dataset_name = null,
            string title = null,
            string content = null,
            string specJson = null,
            int? limit = null,
            int? cardsPerRow = null,
            int? cardRows = null,
            string actionsJson = null)
            => RenderCore(context, layout, dataset_name, title, content, specJson, limit, cardsPerRow, cardRows, interactive: true, actionsJson, mappedFrom: null);

        /// <summary>Deprecated data_render alias.</summary>
        public Task<string> RenderAlias(
            AgentToolContext context,
            string ui = null,
            string layout = null,
            string dataset_name = null,
            string title = null,
            string dataJson = null,
            string content = null,
            string specJson = null,
            string actionsJson = null,
            string blocksJson = null,
            int? limit = null,
            int? cardsPerRow = null,
            int? cardRows = null)
        {
            string mappedFrom = null;
            var layoutNorm = !string.IsNullOrWhiteSpace(layout)
                ? UiTemplateMerge.NormalizeLayout(layout)
                : UiTemplateMerge.MapLegacyUi(ui, out mappedFrom);

            var interactive = !string.IsNullOrWhiteSpace(actionsJson);
            var body = content;
            if (string.IsNullOrWhiteSpace(body) && !string.IsNullOrWhiteSpace(dataJson))
                body = dataJson;

            // Legacy blocksJson → specJson for kpi_dashboard
            if (string.IsNullOrWhiteSpace(specJson) && !string.IsNullOrWhiteSpace(blocksJson))
                specJson = WrapLegacyBlocks(blocksJson);

            return RenderCore(context, layoutNorm, dataset_name, title, body, specJson, limit, cardsPerRow, cardRows, interactive, actionsJson, mappedFrom);
        }

        private static string WrapLegacyBlocks(string blocksJson)
        {
            try
            {
                var token = JToken.Parse(blocksJson);
                if (token is JArray)
                    return new JObject { ["blocks"] = token }.ToString(Formatting.None);
                if (token is JObject obj && obj["blocks"] == null && obj["kpis"] == null && obj["chart"] == null)
                    return new JObject { ["blocks"] = new JArray(obj) }.ToString(Formatting.None);
                return blocksJson;
            }
            catch
            {
                return blocksJson;
            }
        }

        private static async Task<string> RenderCore(
            AgentToolContext context,
            string layout,
            string datasetName,
            string title,
            string content,
            string specJson,
            int? limit,
            int? cardsPerRow,
            int? cardRows,
            bool interactive,
            string actionsJson,
            string mappedFrom)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return Err("Chat session is required.");

                layout = UiTemplateMerge.NormalizeLayout(layout);
                var renderId = Guid.NewGuid().ToString("N");
                FormDocument formDoc = null;
                CardSheet cardSheet = null;
                JArray dashRows = null;
                UiRenderSpec dashSpec = null;
                int rowCount = 0;
                bool truncated = false;
                string resolvedDataset = null;

                if (UiTemplateMerge.IsDashboardLayout(layout))
                {
                    if (!UiRenderSpec.TryParse(specJson, out dashSpec, out var specErr))
                        return Err(specErr);
                    var valErr = dashSpec.ValidateForLayout(layout);
                    if (!string.IsNullOrEmpty(valErr))
                        return Err(valErr);

                    dashRows = LoadRows(context, datasetName, content, limit, out resolvedDataset, out truncated, out rowCount);
                    if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(dashSpec.Title))
                        dashSpec.Title = title.Trim();
                }
                else if (layout == "cards")
                {
                    cardSheet = LoadCards(context, datasetName, content, title, cardsPerRow ?? 1, cardRows ?? CardSheet.DefaultCardRows, limit, out resolvedDataset, out truncated);
                    rowCount = cardSheet.CardCount;
                    if (!string.IsNullOrWhiteSpace(title)) cardSheet.Title = title.Trim();
                }
                else
                {
                    formDoc = LoadFormOrTable(context, layout, datasetName, content, title, limit, out resolvedDataset, out truncated, out rowCount);
                    if (!string.IsNullOrWhiteSpace(title)) formDoc.Title = title.Trim();
                }

                var html = UiTemplateMerge.MergeForRender(
                    layout, formDoc, cardSheet, dashRows, dashSpec, interactive, forExport: false, renderId, actionsJson);
                string htmlPath = null;
                var bytes = Encoding.UTF8.GetByteCount(html);
                if (bytes > UiTemplateMerge.InlineHtmlMaxBytes)
                {
                    htmlPath = "render/" + renderId + ".html";
                    GenericAgentFileBL.WriteText(context.ChatSessionKey, htmlPath, html, context.CompanyId);
                    html = null;
                }

                var mode = interactive ? "Interactive" : "NoneInteractive";
                var evt = new AgentUiRenderEvent
                {
                    RenderId = renderId,
                    Mode = mode,
                    Layout = layout,
                    Title = string.IsNullOrWhiteSpace(title)
                        ? (dashSpec?.Title ?? formDoc?.Title ?? cardSheet?.Title)
                        : title.Trim(),
                    Html = html,
                    HtmlPath = htmlPath,
                    RowCount = rowCount,
                    Truncated = truncated,
                    ActionsJson = interactive ? actionsJson : null,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var legacyUi = layout switch
                {
                    "cards" => "card",
                    "form" => "form",
                    "kpi" => "kpi_dashboard",
                    "chart" => "chart",
                    "dashboard" => "kpi_dashboard",
                    _ => "grid"
                };
                var legacy = new AgentDataRenderEvent
                {
                    RenderId = renderId,
                    Ui = legacyUi,
                    Title = evt.Title,
                    RowCount = rowCount,
                    Truncated = truncated,
                    ActionsJson = evt.ActionsJson,
                    MetaJson = JsonConvert.SerializeObject(new { mode, layout, htmlPath }),
                    Timestamp = evt.Timestamp
                };

                var onUi = AgentHitlBridge.Current?.OnUiRender;
                if (onUi != null)
                    await onUi(evt).ConfigureAwait(false);
                else
                {
                    legacy.MetaJson = JsonConvert.SerializeObject(new
                    {
                        mode,
                        layout,
                        htmlPath,
                        html = html != null && html.Length < 200000 ? html : null
                    });
                    var onData = AgentHitlBridge.Current?.OnDataRender;
                    if (onData != null)
                        await onData(legacy).ConfigureAwait(false);
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    mode,
                    layout,
                    title = evt.Title,
                    rowCount,
                    truncated,
                    renderId,
                    dataset_name = resolvedDataset,
                    mappedFrom,
                    htmlPath,
                    note = "Result HTML is on the SSE ui_render event (or session file when htmlPath is set)."
                });
            }
            catch (Exception ex)
            {
                return Err(ex.Message);
            }
        }

        private static JArray LoadRows(
            AgentToolContext context,
            string datasetName,
            string content,
            int? limit,
            out string resolvedDataset,
            out bool truncated,
            out int rowCount)
        {
            resolvedDataset = null;
            truncated = false;
            rowCount = 0;
            JArray arr = null;

            if (!string.IsNullOrWhiteSpace(datasetName)
                && AgentDataAnalyzeCacheBL.TryExportDataJson(context.ChatSessionKey, datasetName.Trim(), out var dataJson, out _, out var rows))
            {
                resolvedDataset = AgentDataAnalyzeCacheBL.SanitizeName(datasetName);
                arr = JArray.Parse(dataJson);
                rowCount = rows;
            }
            else if (!string.IsNullOrWhiteSpace(content))
            {
                var token = JToken.Parse(content);
                if (token is JArray inline)
                {
                    if (AgentDataAnalyzeCacheBL.TryRestoreRows(context.ChatSessionKey, inline, false, out var restored, out var restoredName))
                    {
                        resolvedDataset = restoredName;
                        inline = restored;
                    }
                    arr = inline;
                }
                else if (token is JObject obj && obj["records"] is JArray rec)
                    arr = rec;
            }

            if (arr == null)
                throw new InvalidOperationException(
                    "Pass dataset_name from a prior query/analyze tool, or content with a JSON array of rows.");

            if (limit.HasValue && limit.Value > 0 && arr.Count > limit.Value)
            {
                arr = AgentDataAnalyzeCacheBL.TakeFirst(arr, limit.Value);
                rowCount = limit.Value;
            }
            else
            {
                rowCount = arr.Count;
            }

            if (arr.Count > MaxRows)
            {
                truncated = true;
                arr = AgentDataAnalyzeCacheBL.TakeFirst(arr, MaxRows);
                rowCount = MaxRows;
            }

            return arr;
        }

        private static FormDocument LoadFormOrTable(
            AgentToolContext context,
            string layout,
            string datasetName,
            string content,
            string title,
            int? limit,
            out string resolvedDataset,
            out bool truncated,
            out int rowCount)
        {
            resolvedDataset = null;
            truncated = false;
            rowCount = 0;

            if (!string.IsNullOrWhiteSpace(datasetName)
                && AgentDataAnalyzeCacheBL.TryExportDataJson(context.ChatSessionKey, datasetName.Trim(), out var dataJson, out var colsJson, out var rows))
            {
                resolvedDataset = AgentDataAnalyzeCacheBL.SanitizeName(datasetName);
                var arr = JArray.Parse(dataJson);
                if (limit.HasValue && limit.Value > 0 && arr.Count > limit.Value)
                {
                    arr = AgentDataAnalyzeCacheBL.TakeFirst(arr, limit.Value);
                    rowCount = limit.Value;
                }
                else
                {
                    rowCount = rows;
                    if (rows > MaxRows)
                    {
                        truncated = true;
                        arr = AgentDataAnalyzeCacheBL.TakeFirst(arr, MaxRows);
                        rowCount = MaxRows;
                    }
                }

                var columns = ParseColumns(colsJson, arr);
                var data = RowsToCells(arr, columns);
                var doc = FormDocument.FromTable(title ?? resolvedDataset ?? "Data", columns, data);
                return doc;
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    var token = JToken.Parse(content);
                    if (token is JArray inline)
                    {
                        if (AgentDataAnalyzeCacheBL.TryRestoreRows(context.ChatSessionKey, inline, false, out var restored, out var restoredName))
                        {
                            resolvedDataset = restoredName;
                            inline = restored;
                        }
                        if (limit.HasValue && limit.Value > 0 && inline.Count > limit.Value)
                            inline = AgentDataAnalyzeCacheBL.TakeFirst(inline, limit.Value);
                        rowCount = inline.Count;
                        if (inline.Count > MaxRows)
                        {
                            truncated = true;
                            inline = AgentDataAnalyzeCacheBL.TakeFirst(inline, MaxRows);
                            rowCount = MaxRows;
                        }
                        var columns = ColumnsFromArray(inline);
                        return FormDocument.FromTable(title ?? "Data", columns, RowsToCells(inline, columns));
                    }
                    if (token is JObject)
                    {
                        var doc = FormDocument.Parse(content);
                        rowCount = Math.Max(1, doc.DataRowCount());
                        return doc;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("content is not valid data JSON: " + ex.Message);
                }
            }

            throw new InvalidOperationException(
                "Pass dataset_name from a prior query tool, or content with a form/table JSON.");
        }

        private static CardSheet LoadCards(
            AgentToolContext context,
            string datasetName,
            string content,
            string title,
            int cardsPerRow,
            int cardRows,
            int? limit,
            out string resolvedDataset,
            out bool truncated)
        {
            resolvedDataset = null;
            truncated = false;
            JArray rows = null;

            if (!string.IsNullOrWhiteSpace(datasetName)
                && AgentDataAnalyzeCacheBL.TryExportDataJson(context.ChatSessionKey, datasetName.Trim(), out var dataJson, out _, out _))
            {
                resolvedDataset = AgentDataAnalyzeCacheBL.SanitizeName(datasetName);
                rows = JArray.Parse(dataJson);
            }
            else if (!string.IsNullOrWhiteSpace(content))
            {
                var token = JToken.Parse(content);
                if (token is JArray arr) rows = arr;
                else if (token is JObject obj && obj["records"] is JArray rec) rows = rec;
                else rows = CardSheet.ReadRecords(content);
            }

            if (rows == null)
                throw new InvalidOperationException("Pass dataset_name or card records content.");

            if (limit.HasValue && limit.Value > 0 && rows.Count > limit.Value)
                rows = AgentDataAnalyzeCacheBL.TakeFirst(rows, limit.Value);
            if (rows.Count > MaxRows)
            {
                truncated = true;
                rows = AgentDataAnalyzeCacheBL.TakeFirst(rows, MaxRows);
            }

            return CardSheet.FromRows(title ?? resolvedDataset ?? "Cards", rows, cardsPerRow, cardRows);
        }

        private static List<string> ParseColumns(string columnsJson, JArray rows)
        {
            if (!string.IsNullOrWhiteSpace(columnsJson))
            {
                try
                {
                    var token = JToken.Parse(columnsJson);
                    if (token is JArray arr)
                    {
                        var list = new List<string>();
                        foreach (var c in arr)
                        {
                            if (c is JObject o)
                                list.Add(o["field"]?.ToString() ?? o["Field"]?.ToString() ?? o.ToString());
                            else
                                list.Add(c.ToString());
                        }
                        if (list.Count > 0) return list;
                    }
                }
                catch { /* fall through */ }
            }
            return ColumnsFromArray(rows);
        }

        private static List<string> ColumnsFromArray(JArray rows)
        {
            var cols = new List<string>();
            if (rows == null || rows.Count == 0) return cols;
            if (rows[0] is JObject first)
            {
                foreach (var p in first.Properties())
                    cols.Add(p.Name);
            }
            return cols;
        }

        private static List<List<FormCell>> RowsToCells(JArray rows, List<string> columns)
        {
            var data = new List<List<FormCell>>();
            if (rows == null) return data;
            foreach (var row in rows)
            {
                var cells = new List<FormCell>();
                if (row is JObject obj)
                {
                    foreach (var col in columns)
                    {
                        var v = obj[col];
                        cells.Add(new FormCell { Text = v?.Type == JTokenType.Null ? "" : v?.ToString() ?? "" });
                    }
                }
                data.Add(cells);
            }
            return data;
        }

        private static string Err(string message) =>
            JsonConvert.SerializeObject(new { ok = false, error = message });
    }
}
