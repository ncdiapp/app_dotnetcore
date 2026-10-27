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
            string actionsJson = null,
            string aiPolishedRenderSpec = null,
            bool selectAllColumns = false,
            string promptHash = null)
            => RenderCore(context, layout, dataset_name, title, content, specJson, limit, cardsPerRow, cardRows,
                interactive: false, actionsJson: null, mappedFrom: null, aiPolishedRenderSpec, selectAllColumns, promptHash);

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
            string actionsJson = null,
            string aiPolishedRenderSpec = null,
            bool selectAllColumns = false,
            string promptHash = null)
            => RenderCore(context, layout, dataset_name, title, content, specJson, limit, cardsPerRow, cardRows,
                interactive: true, actionsJson, mappedFrom: null, aiPolishedRenderSpec, selectAllColumns, promptHash);

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

            return RenderCore(context, layoutNorm, dataset_name, title, body, specJson, limit, cardsPerRow, cardRows,
                interactive, actionsJson, mappedFrom, aiPolishedRenderSpec: null, selectAllColumns: false, promptHash: null);
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
            string mappedFrom,
            string aiPolishedRenderSpec,
            bool selectAllColumns,
            string promptHash)
        {
            try
            {
                if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                    return Err("Chat session is required.");

                layout = UiTemplateMerge.NormalizeLayout(layout);
                var renderId = Guid.NewGuid().ToString("N");
                var resolvedDisplayLimit = ResolveDisplayLimit(context, datasetName, limit);
                FormDocument formDoc = null;
                CardSheet cardSheet = null;
                JArray dashRows = null;
                UiRenderSpec dashSpec = null;
                AiPolishedRenderSpec polish = null;
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
                    var views = ParsePageViews(specJson);
                    dashRows = LoadRows(context, datasetName, content, resolvedDisplayLimit, out resolvedDataset, out truncated, out rowCount);
                    if (views.Contains("cards"))
                    {
                        polish = ResolvePolish(context, resolvedDataset, "cards", title, dashRows, aiPolishedRenderSpec, selectAllColumns, promptHash);
                        // 0,0 → CardSheet.SuggestPrintGrid (not hard-coded 3×3 when count is small).
                        cardSheet = CardSheet.FromRows(
                            title ?? resolvedDataset ?? "Cards", dashRows, cardsPerRow ?? 0, cardRows ?? 0, polish);
                        if (!string.IsNullOrWhiteSpace(polish?.Title)) cardSheet.Title = polish.Title;
                        else if (!string.IsNullOrWhiteSpace(title)) cardSheet.Title = title.Trim();
                    }
                    if (views.Contains("table"))
                    {
                        formDoc = LoadFormOrTable(context, "table", datasetName, content, title, resolvedDisplayLimit, out _, out _, out _);
                        if (!string.IsNullOrWhiteSpace(title) && formDoc != null) formDoc.Title = title.Trim();
                    }
                }
                else if (layout == "cards")
                {
                    // Interactive cards: React + TemplateContent LAYOUT (cardsHtml); ignore small limit=.
                    var effLimit = Documents.AgentDisplayRowLimitBL.Coalesce(resolvedDisplayLimit, limit);
                    if (interactive)
                    {
                        dashRows = LoadRows(context, datasetName, content, effLimit, out resolvedDataset, out truncated, out rowCount);
                        polish = ResolvePolish(context, resolvedDataset, layout, title, dashRows, aiPolishedRenderSpec, selectAllColumns, promptHash);
                        // 0,0 → SuggestPrintGrid when tool/polish omitted page grid (avoid 1×N print strips).
                        cardSheet = CardSheet.FromRows(
                            title ?? resolvedDataset ?? "Cards", dashRows, cardsPerRow ?? 0, cardRows ?? 0, polish);
                    }
                    else
                    {
                        cardSheet = LoadCards(context, datasetName, content, title, cardsPerRow ?? 0, cardRows ?? 0, effLimit,
                            out resolvedDataset, out truncated, out dashRows, aiPolishedRenderSpec, selectAllColumns, promptHash, out polish);
                        rowCount = cardSheet.CardCount;
                    }
                    if (!string.IsNullOrWhiteSpace(polish?.Title)) cardSheet.Title = polish.Title;
                    else if (!string.IsNullOrWhiteSpace(title)) cardSheet.Title = title.Trim();
                }
                else
                {
                    var effLimit = interactive && layout == "table" ? resolvedDisplayLimit : limit;
                    if (interactive && layout == "table")
                    {
                        dashRows = LoadRows(context, datasetName, content, effLimit, out resolvedDataset, out truncated, out rowCount);
                        formDoc = LoadFormOrTable(context, layout, datasetName, content, title, effLimit, out _, out _, out _);
                    }
                    else
                    {
                        formDoc = LoadFormOrTable(context, layout, datasetName, content, title, effLimit, out resolvedDataset, out truncated, out rowCount);
                    }
                    if (!AgentDocumentPlugin.IsGenericExportTitle(title) && formDoc != null)
                        formDoc.Title = title.Trim();
                }

                string html;
                string htmlPath = null;
                string htmlUrl = null;
                var mode = interactive ? "Interactive" : "NoneInteractive";

                string dataJson = null;
                string emitSpecJson = null;

                // Interactive table/cards/page/kpi/dashboard → React hosts + TemplateContent control LAYOUTs.
                // None-interactive + chart-only → TemplateContent iframe. Export twin = none-interactive merge.
                var sectionTitles = ParseSectionTitles(specJson);
                if (polish == null && (layout == "table" || layout == "cards" || layout == "kpi"
                    || layout == "dashboard" || layout == "chart" || UiTemplateMerge.IsPageLayout(layout)))
                {
                    var polishLayout = layout == "page" ? "table" : layout;
                    polish = ResolvePolish(context, resolvedDataset, polishLayout, title,
                        dashRows, aiPolishedRenderSpec, selectAllColumns, promptHash);
                }
                // Prefer tool / polish / dashboard / dataset — skip placeholders like "Result".
                var pageTitle = AgentDocumentPlugin.FirstMeaningfulTitle(
                    title,
                    polish?.Title,
                    dashSpec?.Title,
                    formDoc?.Title,
                    cardSheet?.Title,
                    resolvedDataset) ?? "Result";
                if (!AgentDocumentPlugin.IsGenericExportTitle(pageTitle))
                {
                    if (formDoc != null && AgentDocumentPlugin.IsGenericExportTitle(formDoc.Title))
                        formDoc.Title = pageTitle;
                    if (cardSheet != null && AgentDocumentPlugin.IsGenericExportTitle(cardSheet.Title))
                        cardSheet.Title = pageTitle;
                    if (dashSpec != null && AgentDocumentPlugin.IsGenericExportTitle(dashSpec.Title))
                        dashSpec.Title = pageTitle;
                }
                if (polish != null && formDoc != null
                    && (layout == "table" || UiTemplateMerge.IsPageLayout(layout))
                    && polish.Bindings?.Columns != null && polish.Bindings.Columns.Count > 0
                    && !polish.SelectAllColumns)
                {
                    // Keep Image/Sketch columns even when AI column list omitted them (Result/print parity).
                    var cols = polish.Bindings.ColumnFieldNames();
                    foreach (var sec in formDoc.Sections ?? new List<FormSection>())
                    {
                        if (sec?.Tables == null) continue;
                        foreach (var t in sec.Tables)
                        {
                            if (t?.Columns == null) continue;
                            foreach (var c in t.Columns)
                            {
                                if (!CardSheet.IsImageFieldName(c)) continue;
                                if (!cols.Exists(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase)))
                                    cols.Insert(0, c);
                            }
                        }
                    }
                    formDoc = formDoc.ProjectColumns(cols);
                    formDoc.ApplyColumnPolish(polish.Bindings.Columns);
                }
                else if (polish?.Bindings?.Columns != null && formDoc != null
                         && (layout == "table" || UiTemplateMerge.IsPageLayout(layout)))
                {
                    formDoc.ApplyColumnPolish(polish.Bindings.Columns);
                }

                // Interactive Result: keep <img src="url"> — browser lazy-loads (no serial download → data-URI).
                // None-interactive iframe / twin: embed when needed. Print/Export regenerate via ExportResult.
                if (!interactive)
                {
                    if (cardSheet != null)
                        await CardImageLoader.AttachAsync(cardSheet, context.CompanyId, default).ConfigureAwait(false);
                    if (formDoc != null)
                        await CardImageLoader.AttachAsync(formDoc, context.CompanyId, default).ConfigureAwait(false);
                }

                var cardsTplInteractive = polish?.TemplateBody(interactive: true, "cards");
                var cardsTplNone = polish?.TemplateBody(interactive: false, "cards");
                var filterOptions = dashRows != null && polish != null
                    ? AgentAiPolishCacheBL.BuildFilterOptions(dashRows, polish)
                    : new JObject();
                var polishedClient = polish?.ToClientJson();

                if (interactive
                    && dashRows != null
                    && (layout == "kpi" || layout == "dashboard"
                        || layout == "table" || layout == "cards"
                        || UiTemplateMerge.IsPageLayout(layout)))
                {
                    dataJson = dashRows.ToString(Formatting.None);
                    string cardsHtml = null;
                    if (cardSheet != null && (layout == "cards" || UiTemplateMerge.IsPageLayout(layout)))
                        cardsHtml = UiTemplateMerge.RenderCardsLayoutFragment(cardSheet, cardsTplInteractive);

                    if (layout == "kpi" || layout == "dashboard")
                    {
                        var resolvedCharts = UiDashboardHtml.ResolveCharts(dashSpec, dashRows, allowAuto: true);
                        if (resolvedCharts.Count > 0 && dashSpec != null)
                        {
                            dashSpec.Charts = resolvedCharts;
                            if (dashSpec.Chart == null)
                                dashSpec.Chart = resolvedCharts[0];
                        }
                        var kpiHtml = dashSpec?.Kpis != null && dashSpec.Kpis.Count > 0
                            ? UiTemplateMerge.RenderKpiLayoutFragment(pageTitle, dashRows, dashSpec.Kpis)
                            : null;
                        var chartHostHtml = UiTemplateMerge.RenderControlHostFragment("chart", pageTitle);
                        var gridHostHtml = UiTemplateMerge.RenderControlHostFragment("table", pageTitle);
                        emitSpecJson = JsonConvert.SerializeObject(new
                        {
                            title = dashSpec?.Title ?? pageTitle,
                            dataset_name = resolvedDataset,
                            kpis = dashSpec?.Kpis,
                            chart = dashSpec?.Chart,
                            charts = dashSpec?.Charts,
                            blocks = dashSpec?.Blocks,
                            availableChartTypes = UiRenderSpec.AvailableChartTypes,
                            kpiHtml,
                            chartHostHtml,
                            gridHostHtml,
                            polishedSpec = polishedClient,
                            filterOptions
                        }, Formatting.None);
                    }
                    else if (UiTemplateMerge.IsPageLayout(layout))
                    {
                        // Prefer resolved CardSheet grid (includes SuggestPrintGrid); else tool/polish; else 3×3.
                        var pageCpr = cardSheet != null && cardSheet.CardsPerRow > 0
                            ? cardSheet.CardsPerRow
                            : (cardsPerRow ?? polish?.LayoutHints?.CardsPerRow ?? 3);
                        var pageCr = cardSheet != null && cardSheet.CardRows > 0
                            ? cardSheet.CardRows
                            : (cardRows ?? polish?.LayoutHints?.CardRows ?? CardSheet.DefaultCardRows);
                        emitSpecJson = JsonConvert.SerializeObject(new
                        {
                            title = pageTitle,
                            dataset_name = resolvedDataset,
                            views = ParsePageViews(specJson),
                            cardsPerRow = pageCpr,
                            cardRows = pageCr,
                            pageGrid = pageCpr + "x" + pageCr,
                            sectionTitles,
                            cardsHtml,
                            polishedSpec = polishedClient,
                            filterOptions
                        }, Formatting.None);
                    }
                    else if (layout == "cards")
                    {
                        var cpr = cardSheet != null && cardSheet.CardsPerRow > 0
                            ? cardSheet.CardsPerRow
                            : (cardsPerRow ?? polish?.LayoutHints?.CardsPerRow ?? 3);
                        var cr = cardSheet != null && cardSheet.CardRows > 0
                            ? cardSheet.CardRows
                            : (cardRows ?? polish?.LayoutHints?.CardRows ?? CardSheet.DefaultCardRows);
                        emitSpecJson = JsonConvert.SerializeObject(new
                        {
                            title = pageTitle,
                            dataset_name = resolvedDataset,
                            cardsPerRow = cpr,
                            cardRows = cr,
                            pageGrid = cpr + "x" + cr,
                            sectionTitles,
                            cardsHtml,
                            polishedSpec = polishedClient,
                            filterOptions
                        }, Formatting.None);
                    }
                    else
                    {
                        emitSpecJson = JsonConvert.SerializeObject(new
                        {
                            title = pageTitle,
                            dataset_name = resolvedDataset,
                            sectionTitles,
                            columns = polish?.Bindings?.Columns,
                            columnsJson = polish?.Bindings != null
                                ? JToken.Parse(polish.Bindings.ToColumnsJson())
                                : null,
                            polishedSpec = polishedClient,
                            filterOptions
                        }, Formatting.None);
                    }

                    // Export / Files twin = none-interactive merge. Dashboard twin marks charts with
                    // data-rasterize; ExportPdf/Docx/Pptx runs ChartRasterize.EmbedChartPngsAsync.
                    var twinForExport = layout == "kpi" || layout == "dashboard" || layout == "chart";
                    var twinHtml = UiTemplateMerge.MergeForRender(
                        layout, formDoc, cardSheet, dashRows, dashSpec,
                        interactive: false, forExport: twinForExport, renderId, actionsJson, sectionTitles, cardsTplNone);
                    if (!string.IsNullOrWhiteSpace(twinHtml)
                        && (layout == "cards" || UiTemplateMerge.IsPageLayout(layout)))
                        twinHtml = AgentUiCssInline.InlineAgentUiLinks(twinHtml);
                    htmlPath = "render/" + renderId + ".html";
                    GenericAgentFileBL.WriteText(context.ChatSessionKey, htmlPath, twinHtml, context.CompanyId);
                    html = null;
                    htmlUrl = null;
                }
                else
                {
                    if (layout == "chart" && dashSpec != null && dashRows != null)
                    {
                        var resolvedCharts = UiDashboardHtml.ResolveCharts(dashSpec, dashRows, allowAuto: true);
                        if (resolvedCharts.Count > 0)
                        {
                            dashSpec.Charts = resolvedCharts;
                            if (dashSpec.Chart == null)
                                dashSpec.Chart = resolvedCharts[0];
                        }
                    }

                    html = UiTemplateMerge.MergeForRender(
                        layout, formDoc, cardSheet, dashRows, dashSpec, interactive, forExport: false, renderId, actionsJson, sectionTitles, cardsTplNone);
                    // B5: none-interactive iframe — inline agent-ui CSS so /agent-ui/*.css always applies (PathBase/offline).
                    if (!interactive && !string.IsNullOrWhiteSpace(html)
                        && (layout == "cards" || UiTemplateMerge.IsPageLayout(layout)))
                        html = AgentUiCssInline.InlineAgentUiLinks(html);
                    var bytes = Encoding.UTF8.GetByteCount(html ?? "");
                    if (bytes > UiTemplateMerge.InlineHtmlMaxBytes)
                    {
                        htmlPath = "render/" + renderId + ".html";
                        GenericAgentFileBL.WriteText(context.ChatSessionKey, htmlPath, html, context.CompanyId);
                        html = null;
                    }
                }

                var columnsJsonEmit = polish?.Bindings != null && polish.Bindings.Columns.Count > 0
                    ? polish.Bindings.ToColumnsJson()
                    : null;

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
                    ColumnsJson = columnsJsonEmit,
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

                var onUi = AgentHitlBridge.Resolve(context?.ChatSessionKey)?.OnUiRender;
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
                    var onData = AgentHitlBridge.Resolve(context?.ChatSessionKey)?.OnDataRender;
                    if (onData != null)
                        await onData(legacy).ConfigureAwait(false);
                }

                string note;
                if (interactive && UiTemplateMerge.IsPageLayout(layout))
                    note = "layout=page: Interactive Result is React (Wijmo grid + TemplateContent cards LAYOUT). No view toggle — do not claim one in FinalResponse. htmlPath is export twin only.";
                else if (interactive && (layout == "kpi" || layout == "dashboard"))
                    note = "layout=" + layout + ": Interactive Result is React stack + TemplateContent control LAYOUTs (kpi/chart/table keys). htmlPath is export twin only.";
                else if (interactive && dataJson != null)
                    note = "Interactive Result uses DataJson+SpecJson (React Wijmo / TemplateContent control LAYOUTs). htmlPath is an export twin only.";
                else
                    note = "Result HTML is on the SSE ui_render event (or session file when htmlPath is set).";

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
                    ui_note = note,
                    note
                });
            }
            catch (Exception ex)
            {
                return Err(ex.Message);
            }
        }

        private static int? ResolveDisplayLimit(AgentToolContext context, string datasetName, int? limitParam)
        {
            int? fromDataset = null;
            if (context != null && !string.IsNullOrWhiteSpace(context.ChatSessionKey) && !string.IsNullOrWhiteSpace(datasetName))
                fromDataset = AgentDataAnalyzeCacheBL.GetDisplayRowLimit(context.ChatSessionKey, datasetName.Trim());
            int? fromTurn = null;
            if (context != null && Documents.AgentDisplayRowLimitBL.TryGetTurnRowIntent(context.ChatSessionKey, out var turn))
                fromTurn = turn;
            return Documents.AgentDisplayRowLimitBL.Coalesce(limitParam, fromDataset, fromTurn);
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
                limit = Documents.AgentDisplayRowLimitBL.Coalesce(
                    limit,
                    AgentDataAnalyzeCacheBL.GetDisplayRowLimit(context.ChatSessionKey, resolvedDataset));
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
                limit = Documents.AgentDisplayRowLimitBL.Coalesce(
                    limit,
                    AgentDataAnalyzeCacheBL.GetDisplayRowLimit(context.ChatSessionKey, resolvedDataset));
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
            out bool truncated,
            out JArray rowsOut,
            string aiPolishedRenderSpec,
            bool selectAllColumns,
            string promptHash,
            out AiPolishedRenderSpec polish)
        {
            resolvedDataset = null;
            truncated = false;
            polish = null;
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

            rowsOut = rows;
            polish = ResolvePolish(context, resolvedDataset, "cards", title, rows, aiPolishedRenderSpec, selectAllColumns, promptHash);
            return CardSheet.FromRows(title ?? resolvedDataset ?? "Cards", rows, cardsPerRow, cardRows, polish);
        }

        private static AiPolishedRenderSpec ResolvePolish(
            AgentToolContext context,
            string datasetName,
            string layout,
            string title,
            JArray rows,
            string aiPolishedRenderSpec,
            bool selectAllColumns,
            string promptHash)
        {
            if (!string.IsNullOrWhiteSpace(aiPolishedRenderSpec)
                && AiPolishedRenderSpec.TryParse(aiPolishedRenderSpec, out var fromArg, out _))
            {
                if (selectAllColumns) fromArg.SelectAllColumns = true;
                AgentAiPolishCacheBL.Put(context.ChatSessionKey, datasetName ?? "_", layout, fromArg, promptHash);
                return fromArg;
            }

            if (AgentAiPolishCacheBL.TryGet(context.ChatSessionKey, datasetName ?? "_", layout, out var cached, promptHash))
            {
                if (selectAllColumns && !cached.SelectAllColumns)
                {
                    cached.SelectAllColumns = true;
                    if (rows != null && rows.Count > 0 && rows[0] is JObject o0)
                        cached.Bindings.Columns = o0.Properties()
                            .Select(p => new AiPolishColumnBinding { Field = p.Name })
                            .ToList();
                }
                return cached;
            }

            // Heuristic polish (structure only) so GENERATE does not dump all fields by default.
            var cols = new List<string>();
            if (rows != null)
            {
                foreach (var token in rows.OfType<JObject>())
                {
                    foreach (var p in token.Properties())
                    {
                        if (!cols.Any(c => string.Equals(c, p.Name, StringComparison.OrdinalIgnoreCase)))
                            cols.Add(p.Name);
                    }
                    if (cols.Count > 0) break;
                }
            }
            var sample = AgentDataAnalyzeCacheBL.TakeFirst(rows, AgentAiPolishCacheBL.SampleMaxRows);
            var built = AiRenderPolishHeuristics.Build(layout, title, cols, sample, selectAllColumns);
            AgentAiPolishCacheBL.Put(context.ChatSessionKey, datasetName ?? "_", layout, built, promptHash);
            return built;
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

        /// <summary>
        /// LLM section titles for stacked collapse chrome (layout=page).
        /// Keys: table|grid, cards|card — values are short human labels from the user question.
        /// </summary>
        private static Dictionary<string, string> ParseSectionTitles(string specJson)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (string.IsNullOrWhiteSpace(specJson)) return map;
                var o = JObject.Parse(specJson);
                var titles = o["sectionTitles"] as JObject ?? o["SectionTitles"] as JObject;
                if (titles == null) return map;
                foreach (var p in titles.Properties())
                {
                    var key = (p.Name ?? "").Trim();
                    var val = p.Value?.Type == JTokenType.String ? p.Value.ToString() : p.Value?.ToString();
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) continue;
                    map[key] = val.Trim();
                }
            }
            catch { /* ignore */ }
            return map;
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
                        cells.Add(CellFromToken(obj[col], col));
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

        private static FormCell CellFromToken(JToken v, string columnName)
        {
            var cell = CellFromToken(v);
            if (cell != null && !cell.IsImage && CardSheet.IsRenderableImage(cell.Text, columnName, cell.IsImage))
                cell.IsImage = true;
            return cell;
        }

        private static string Err(string message) =>
            JsonConvert.SerializeObject(new { ok = false, error = message });
    }
}
