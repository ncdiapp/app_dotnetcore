using System;
using System.Collections.Generic;
using System.Linq;
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
                    if (context.IsDeterministic != true
                        && !AgentUiPathConfirmBL.IsKpiDashboardConfirmed(context.ChatSessionKey))
                    {
                        return Err(
                            "layout=" + layout + " requires KPI/Dashboard confirmation first. " +
                            "Call ask_user with contextKey='" + AgentUiPathConfirmBL.ContextKey +
                            "', mode=single_choice, prompt='" + AgentUiPathConfirmBL.AskUserPromptHint() +
                            "', optionsJson=" + AgentUiPathConfirmBL.AskUserOptionsJson() + ". " +
                            "Only after the user picks kpi_dashboard, call this render again.");
                    }

                    if (!UiRenderSpec.TryParse(specJson, out dashSpec, out var specErr))
                        return Err(specErr);
                    var valErr = dashSpec.ValidateForLayout(layout);
                    if (!string.IsNullOrEmpty(valErr))
                        return Err(valErr);

                    // Dashboard KPI/chart/grid need the full cached dataset. Models often pass
                    // limit=20 from the old execute_sql preview habit — ignore that here.
                    dashRows = LoadRows(context, datasetName, content, limit: null, out resolvedDataset, out truncated, out rowCount);
                    if (!string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(dashSpec.Title))
                        dashSpec.Title = title.Trim();
                }
                else if (UiTemplateMerge.IsPageLayout(layout))
                {
                    // Show-only: grid + cards on ONE Result page. No KPI ask_user gate.
                    dashRows = LoadRows(context, datasetName, content, limit: null, out resolvedDataset, out truncated, out rowCount);
                    cardSheet = CardSheet.FromRows(
                        title ?? resolvedDataset ?? "Cards", dashRows, cardsPerRow ?? 3, cardRows ?? CardSheet.DefaultCardRows);
                    if (!string.IsNullOrWhiteSpace(title)) cardSheet.Title = title.Trim();
                    formDoc = LoadFormOrTable(context, "table", datasetName, content, title, null, out _, out _, out _);
                    if (!string.IsNullOrWhiteSpace(title) && formDoc != null) formDoc.Title = title.Trim();
                }
                else if (layout == "cards")
                {
                    // Interactive Result uses React cards + full cache; ignore small limit= habits.
                    var effLimit = interactive ? null : limit;
                    if (interactive)
                    {
                        dashRows = LoadRows(context, datasetName, content, null, out resolvedDataset, out truncated, out rowCount);
                        cardSheet = CardSheet.FromRows(
                            title ?? resolvedDataset ?? "Cards", dashRows, cardsPerRow ?? 1, cardRows ?? CardSheet.DefaultCardRows);
                    }
                    else
                    {
                        cardSheet = LoadCards(context, datasetName, content, title, cardsPerRow ?? 1, cardRows ?? CardSheet.DefaultCardRows, effLimit, out resolvedDataset, out truncated);
                        rowCount = cardSheet.CardCount;
                    }
                    if (!string.IsNullOrWhiteSpace(title)) cardSheet.Title = title.Trim();
                }
                else
                {
                    var effLimit = interactive && layout == "table" ? null : limit;
                    if (interactive && layout == "table")
                    {
                        dashRows = LoadRows(context, datasetName, content, null, out resolvedDataset, out truncated, out rowCount);
                        formDoc = LoadFormOrTable(context, layout, datasetName, content, title, null, out _, out _, out _);
                    }
                    else
                    {
                        formDoc = LoadFormOrTable(context, layout, datasetName, content, title, effLimit, out resolvedDataset, out truncated, out rowCount);
                    }
                    if (!string.IsNullOrWhiteSpace(title) && formDoc != null) formDoc.Title = title.Trim();
                }

                string html;
                string htmlPath = null;
                string htmlUrl = null;
                var mode = interactive ? "Interactive" : "NoneInteractive";
                var pageTitle = string.IsNullOrWhiteSpace(title)
                    ? (dashSpec?.Title ?? formDoc?.Title ?? cardSheet?.Title)
                    : title.Trim();

                string dataJson = null;
                string emitSpecJson = null;

                // Interactive dashboard still uses React host (DataJson + SpecJson); HTML twin for export.
                // Interactive table/cards/page → TemplateContent iframe (same merge as none-interactive, tool row = render_interactive).
                if (interactive && UiTemplateMerge.IsDashboardLayout(layout) && dashRows != null && dashSpec != null)
                {
                    dataJson = dashRows.ToString(Formatting.None);
                    // Ensure charts[] carries a type per series (LLM or SuggestChartType)
                    var resolvedCharts = UiDashboardHtml.ResolveCharts(dashSpec, dashRows, allowAuto: true);
                    if (resolvedCharts.Count > 0)
                    {
                        dashSpec.Charts = resolvedCharts;
                        if (dashSpec.Chart == null)
                            dashSpec.Chart = resolvedCharts[0];
                    }
                    emitSpecJson = JsonConvert.SerializeObject(new
                    {
                        title = dashSpec.Title,
                        kpis = dashSpec.Kpis,
                        chart = dashSpec.Chart,
                        charts = dashSpec.Charts,
                        blocks = dashSpec.Blocks,
                        availableChartTypes = UiRenderSpec.AvailableChartTypes
                    }, Formatting.None);
                    var svgHtml = UiTemplateMerge.MergeForRender(
                        layout, null, null, dashRows, dashSpec, interactive: false, forExport: false, renderId, actionsJson);
                    htmlPath = "render/" + renderId + ".html";
                    GenericAgentFileBL.WriteText(context.ChatSessionKey, htmlPath, svgHtml, context.CompanyId);
                    html = null;
                    htmlUrl = null;
                }
                else
                {
                    // table / cards / page / form / none-interactive dashboard bits: merge from TemplateContent.
                    html = UiTemplateMerge.MergeForRender(
                        layout, formDoc, cardSheet, dashRows, dashSpec, interactive, forExport: false, renderId, actionsJson);
                    var bytes = Encoding.UTF8.GetByteCount(html);
                    if (bytes > UiTemplateMerge.InlineHtmlMaxBytes)
                    {
                        htmlPath = "render/" + renderId + ".html";
                        GenericAgentFileBL.WriteText(context.ChatSessionKey, htmlPath, html, context.CompanyId);
                        html = null;
                    }
                }

                var evt = new AgentUiRenderEvent
                {
                    RenderId = renderId,
                    Mode = mode,
                    Layout = layout,
                    Title = pageTitle,
                    Html = html,
                    HtmlPath = htmlPath,
                    HtmlUrl = htmlUrl,
                    DataJson = dataJson,
                    SpecJson = emitSpecJson,
                    RowCount = rowCount,
                    Truncated = truncated,
                    ActionsJson = interactive ? actionsJson : null,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var legacyUi = layout switch
                {
                    "cards" => "card",
                    "form" => "form",
                    "page" => "page",
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
                    htmlUrl,
                    note = interactive && dataJson != null
                        ? "Interactive Result uses DataJson+SpecJson (React Wijmo, app license — no CDN watermark)."
                        : "Result HTML is on the SSE ui_render event (or session file when htmlPath is set)."
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

        /// <summary>specJson.views for layout=page; default table + cards on one Result.</summary>
        private static string[] ParsePageViews(string specJson)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(specJson))
                {
                    var o = JObject.Parse(specJson);
                    var arr = o["views"] as JArray ?? o["Views"] as JArray;
                    if (arr != null && arr.Count > 0)
                    {
                        var list = new List<string>();
                        foreach (var t in arr)
                        {
                            var v = (t?.ToString() ?? "").Trim().ToLowerInvariant();
                            if (v is "table" or "grid") list.Add("table");
                            else if (v is "cards" or "card") list.Add("cards");
                        }
                        if (list.Count > 0) return list.Distinct().ToArray();
                    }
                }
            }
            catch { /* default */ }
            return new[] { "table", "cards" };
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
                        cells.Add(CellFromToken(obj[col]));
                }
                data.Add(cells);
            }
            return data;
        }

        private static FormCell CellFromToken(JToken v)
        {
            if (v == null || v.Type == JTokenType.Null)
                return new FormCell();
            if (v is JObject o)
            {
                var url = o["url"]?.ToString() ?? o["Url"]?.ToString();
                if (!string.IsNullOrWhiteSpace(url))
                    return new FormCell { Text = url, IsImage = true };
                var fileId = o["fileId"]?.ToString() ?? o["FileId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(fileId))
                    return new FormCell { Text = fileId, IsImage = true };
            }
            if (v.Type == JTokenType.Integer || v.Type == JTokenType.Float)
            {
                return new FormCell
                {
                    Text = ((JValue)v).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    IsNumber = true
                };
            }
            var text = v.Type == JTokenType.String ? v.ToString() : v.ToString(Formatting.None);
            return new FormCell { Text = text, IsImage = CardSheet.IsImageValue(text) };
        }

        private static string Err(string message) =>
            JsonConvert.SerializeObject(new { ok = false, error = message });
    }
}
