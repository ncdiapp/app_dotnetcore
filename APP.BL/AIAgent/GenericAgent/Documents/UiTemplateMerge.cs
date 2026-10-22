using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using App.BL.TenantBusiness;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Loads TemplateContent from render_none_interactive / render_interactive and merges
    /// data into HTML (MVC View pattern). Real HTML is materialized into DB on first use.
    /// None-interactive / chart Result → iframe.
    /// Interactive page/dashboard → React stacks controls; each control LAYOUT from TemplateContent
    /// (cardsHtml / kpiHtml / chartHostHtml / gridHostHtml).
    /// </summary>
    public static class UiTemplateMerge
    {
        public const string LibraryKey = "platform-ui-render";
        public const string NoneInteractiveTool = "render_none_interactive";
        public const string InteractiveTool = "render_interactive";
        public const string CodeDefaultMarker = "{{code_default}}";
        public const int InlineHtmlMaxBytes = 512 * 1024;

        public static string MergeForm(FormDocument doc, bool forPrint = false)
        {
            var body = ResolveLayoutBody(NoneInteractiveTool, InferFormLayout(doc));
            // Same Scriban path as Result (MergeForRender) — never title-only replace on TemplateContent.
            var html = RenderTableOrFormView("form", body, doc, forExport: true);
            return forPrint ? HtmlForm.ForPrint(html) : html;
        }

        public static string MergeTable(FormDocument doc, bool forPrint = false)
        {
            var body = ResolveLayoutBody(NoneInteractiveTool, "table");
            var html = RenderTableOrFormView("table", body, doc, forExport: true);
            return forPrint ? HtmlForm.ForPrint(html) : html;
        }

        public static string MergeCards(CardSheet sheet, bool forPrint = false, string templateBodyOverride = null)
        {
            // Print/PDF/DOCX/PPT: fitted R×C pages (cardRows × cardsPerRow). Screen Result uses TemplateContent flow.
            if (forPrint)
            {
                var fitted = HtmlForm.WriteCards(sheet, forPrint: true);
                return AgentUiCssInline.PrepareExportHtml(fitted, addPrintExtras: true);
            }

            var body = !string.IsNullOrWhiteSpace(templateBodyOverride)
                ? templateBodyOverride
                : ResolveLayoutBody(NoneInteractiveTool, "cards");
            string html;
            if (IsCodeDefault(body))
                html = HtmlForm.WriteCards(sheet, forPrint: false);
            else
            {
                try { html = RenderCardsView(body, sheet); }
                catch { html = HtmlForm.WriteCards(sheet, forPrint: false); }
            }
            return html;
        }

        /// <summary>Inline vendored agent-ui CSS for Playwright / offline export of any merged HTML.</summary>
        public static string PrepareExportHtml(string html)
            => AgentUiCssInline.PrepareExportHtml(html, addPrintExtras: true);

        /// <summary>None-interactive or interactive page for Result iframe.</summary>
        public static string MergeForRender(
            string layout,
            FormDocument formDoc,
            CardSheet cardSheet,
            bool interactive,
            string renderId,
            string actionsJson)
        {
            return MergeForRender(layout, formDoc, cardSheet, null, null, interactive, false, renderId, actionsJson);
        }

        /// <summary>Result iframe or export merge. Dashboard layouts use <see cref="UiDashboardHtml"/>.</summary>
        public static string MergeForRender(
            string layout,
            FormDocument formDoc,
            CardSheet cardSheet,
            JArray dashRows,
            UiRenderSpec dashSpec,
            bool interactive,
            bool forExport,
            string renderId,
            string actionsJson)
            => MergeForRender(layout, formDoc, cardSheet, dashRows, dashSpec, interactive, forExport, renderId, actionsJson, null);

        /// <param name="sectionTitles">LLM titles for stacked controls (keys: table/grid, cards/card). Platform chrome only.</param>
        /// <param name="cardsTemplateOverride">Session AI-polished cards TemplateContent body (not library write-back).</param>
        public static string MergeForRender(
            string layout,
            FormDocument formDoc,
            CardSheet cardSheet,
            JArray dashRows,
            UiRenderSpec dashSpec,
            bool interactive,
            bool forExport,
            string renderId,
            string actionsJson,
            IDictionary<string, string> sectionTitles,
            string cardsTemplateOverride = null)
        {
            layout = NormalizeLayout(layout);
            var toolName = interactive ? InteractiveTool : NoneInteractiveTool;
            string html;
            if (IsDashboardLayout(layout))
            {
                var body = ResolveLayoutBody(toolName, layout);
                // Interactive kpi/dashboard Result uses React; none-interactive + export twin use UiDashboardHtml.
                // Stubs / title-only TemplateContent must not skip Build (breaks charts + data-rasterize).
                if (IsCodeDefault(body) || IsEmptyDashboardShell(body)
                    || (forExport && body.IndexOf("chart-surface", StringComparison.OrdinalIgnoreCase) < 0
                        && body.IndexOf("data-rasterize", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    html = UiDashboardHtml.Build(
                        layout,
                        dashRows,
                        dashSpec,
                        dashSpec?.Title ?? formDoc?.Title,
                        interactive,
                        forExport,
                        renderId,
                        actionsJson);
                }
                else
                {
                    html = ApplyTitlePlaceholder(body, dashSpec?.Title ?? formDoc?.Title);
                }
            }
            else if (IsPageLayout(layout))
            {
                var body = ResolveLayoutBody(toolName, "page", allowTableFormFallback: false);
                var pageTitle = formDoc?.Title ?? cardSheet?.Title ?? dashSpec?.Title;
                // Empty page chrome (or code_default) → compose table+cards from TemplateContent keys.
                if (IsCodeDefault(body) || IsEmptyPageShell(body))
                {
                    if (interactive && !forExport)
                        html = ComposeInteractivePageHtml(
                            pageTitle, dashRows, formDoc, cardSheet, sectionTitles, renderId, actionsJson);
                    else
                        html = ComposePageHtml(formDoc, cardSheet, pageTitle, sectionTitles);
                }
                else
                {
                    html = ApplyTitlePlaceholder(body, pageTitle);
                    if (interactive && !forExport && dashRows != null && HasGridHost(html))
                        html = InteractiveTabularHtml.InjectWijmoGridBoot(html, dashRows, renderId, actionsJson);
                }
            }
            else if (layout == "cards" && cardSheet != null)
            {
                var body = !string.IsNullOrWhiteSpace(cardsTemplateOverride)
                    ? cardsTemplateOverride
                    : ResolveLayoutBody(toolName, "cards");
                html = RenderCardsView(body, cardSheet);
            }
            else if (formDoc != null || (layout == "table" && dashRows != null))
            {
                var key = layout == "form" ? "form" : "table";
                var body = ResolveLayoutBody(toolName, key);
                var tableTitle = formDoc?.Title ?? dashSpec?.Title ?? "Table";
                // Interactive table: TemplateContent LAYOUT (+ code_default → Wijmo FlexGrid host).
                if (interactive && !forExport && key == "table" && dashRows != null)
                    html = RenderInteractiveTableView(body, tableTitle, dashRows, renderId, actionsJson);
                else if (formDoc != null)
                    html = RenderTableOrFormView(key, body, formDoc, forExport);
                else if (dashRows != null)
                    html = RenderTableFromRows(body, tableTitle, dashRows, forExport);
                else
                    html = "<!DOCTYPE html><html><body><p>No data</p></body></html>";
            }
            else
            {
                html = "<!DOCTYPE html><html><body><p>No data</p></body></html>";
            }

            if (IsDashboardLayout(layout))
            {
                if (!interactive)
                    html = StripScripts(html);
                return html;
            }

            if (interactive)
                html = WrapInteractive(html, renderId, actionsJson);
            else
                html = StripScripts(html);

            return html;
        }

        private static string WrapDocumentBody(string html, System.Func<string, string> wrapInner)
        {
            if (string.IsNullOrEmpty(html) || wrapInner == null) return html;
            var m = Regex.Match(html, @"<body[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase);
            if (!m.Success) return wrapInner(html);
            var replaced = wrapInner(m.Groups[1].Value);
            return html.Substring(0, m.Groups[1].Index) + replaced + html.Substring(m.Groups[1].Index + m.Groups[1].Length);
        }

        private static string RenderCardsView(string body, CardSheet cardSheet)
        {
            if (IsCodeDefault(body))
                body = UiDefaultViewTemplates.CardsNoneInteractive;
            try
            {
                return UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromCards(cardSheet));
            }
            catch
            {
                return HtmlForm.WriteCards(cardSheet, false);
            }
        }

        /// <summary>
        /// Interactive cards control LAYOUT: TemplateContent Scriban HTML (+ head styles) for React host.
        /// Uses render_interactive TemplateContent key <c>cards</c>.
        /// </summary>
        public static string RenderCardsLayoutFragment(CardSheet cardSheet, string templateBodyOverride = null)
        {
            if (cardSheet == null) return null;
            var body = !string.IsNullOrWhiteSpace(templateBodyOverride)
                ? templateBodyOverride
                : ResolveLayoutBody(InteractiveTool, "cards");
            var html = RenderCardsView(body, cardSheet);
            return ExtractBodyInnerWithStyles(html);
        }

        /// <summary>
        /// Interactive KPI control LAYOUT from TemplateContent key <c>kpi</c> (Scriban items).
        /// </summary>
        public static string RenderKpiLayoutFragment(string title, JArray rows, IList<UiKpiSpec> kpis)
        {
            if (kpis == null || kpis.Count == 0) return null;
            var body = ResolveLayoutBody(InteractiveTool, "kpi", allowTableFormFallback: false);
            if (IsCodeDefault(body) || string.IsNullOrWhiteSpace(body)
                || body.IndexOf("data-role=\"dashboard\"", StringComparison.OrdinalIgnoreCase) >= 0)
                body = UiDefaultViewTemplates.KpiInteractive;
            try
            {
                var html = UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromKpis(title, rows, kpis));
                return ExtractBodyInnerWithStyles(html);
            }
            catch
            {
                return ExtractBodyInnerWithStyles(
                    UiTemplateScriban.Render(UiDefaultViewTemplates.KpiInteractive,
                        UiTemplateScriban.ModelFromKpis(title, rows, kpis)));
            }
        }

        /// <summary>
        /// Interactive control host shell (chart #chartHost / table #gridHost) for React Wijmo mount.
        /// </summary>
        public static string RenderControlHostFragment(string layoutKey, string title = null)
        {
            var key = (layoutKey ?? "").Trim().ToLowerInvariant();
            if (key != "chart" && key != "table") return null;
            var body = ResolveLayoutBody(InteractiveTool, key, allowTableFormFallback: false);
            if (IsCodeDefault(body) || string.IsNullOrWhiteSpace(body))
                body = key == "chart" ? UiDefaultViewTemplates.ChartInteractive : UiDefaultViewTemplates.TableInteractive;
            if (key == "chart"
                && body.IndexOf("chartHost", StringComparison.OrdinalIgnoreCase) < 0)
                body = UiDefaultViewTemplates.ChartInteractive;
            if (key == "table"
                && body.IndexOf("gridHost", StringComparison.OrdinalIgnoreCase) < 0)
                body = UiDefaultViewTemplates.TableInteractive;
            try
            {
                var html = UiTemplateScriban.Render(body, new { title = System.Net.WebUtility.HtmlEncode(title ?? "") });
                return ExtractBodyInnerWithStyles(html);
            }
            catch
            {
                return ExtractBodyInnerWithStyles(body);
            }
        }

        /// <summary>
        /// Interactive table from TemplateContent: code_default → Wijmo host;
        /// custom HTML with #gridHost → Scriban + Wijmo boot; else Scriban only.
        /// </summary>
        private static string RenderInteractiveTableView(
            string body,
            string title,
            JArray dashRows,
            string renderId,
            string actionsJson)
        {
            if (IsCodeDefault(body))
                return InteractiveTabularHtml.BuildTableDocument(title, dashRows, renderId, actionsJson);

            string html;
            try { html = UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromRows(title, dashRows)); }
            catch { return InteractiveTabularHtml.BuildTableDocument(title, dashRows, renderId, actionsJson); }

            if (html != null && Regex.IsMatch(html, @"id\s*=\s*[""']gridHost[""']", RegexOptions.IgnoreCase))
                return InteractiveTabularHtml.InjectWijmoGridBoot(html, dashRows, renderId, actionsJson);
            return html;
        }

        private static string RenderTableOrFormView(string key, string body, FormDocument formDoc, bool forExport)
        {
            if (key == "form")
            {
                if (IsCodeDefault(body))
                    return HtmlForm.Write(formDoc);
                try { return UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromForm(formDoc)); }
                catch { return HtmlForm.Write(formDoc); }
            }

            if (IsCodeDefault(body))
                body = UiDefaultViewTemplates.TableNoneInteractive;
            string html;
            try { html = UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromForm(formDoc)); }
            catch { html = HtmlForm.Write(formDoc); }
            if (!forExport)
                html = WrapDocumentBody(html, inner => UiSectionChrome.WrapGridScroll(inner));
            return html;
        }

        private static string RenderTableFromRows(string body, string title, JArray dashRows, bool forExport)
        {
            if (IsCodeDefault(body))
                body = UiDefaultViewTemplates.TableNoneInteractive;
            string html;
            try { html = UiTemplateScriban.Render(body, UiTemplateScriban.ModelFromRows(title, dashRows)); }
            catch
            {
                html = "<!DOCTYPE html><html><body><p>" + System.Net.WebUtility.HtmlEncode(title) + "</p></body></html>";
            }
            if (!forExport)
                html = WrapDocumentBody(html, inner => UiSectionChrome.WrapGridScroll(inner));
            return html;
        }

        public static bool IsDashboardLayout(string layout)
        {
            var n = NormalizeLayout(layout);
            return n == "kpi" || n == "chart" || n == "dashboard";
        }

        public static string MergeDashboard(
            string layout,
            JArray rows,
            UiRenderSpec spec,
            string title,
            bool forExport,
            string renderId = null)
        {
            layout = NormalizeLayout(layout);
            if (!IsDashboardLayout(layout))
                layout = "dashboard";
            if (!string.IsNullOrWhiteSpace(title) && spec != null && string.IsNullOrWhiteSpace(spec.Title))
                spec.Title = title;
            var body = ResolveLayoutBody(NoneInteractiveTool, layout);
            // Materialized kpi/chart/dashboard stubs are empty shells (title only). Always Build so
            // forExport marks .chart-surface[data-rasterize] for ChartRasterize → PNG in PDF/DOCX/PPTX.
            if (IsCodeDefault(body) || IsEmptyDashboardShell(body))
                return UiDashboardHtml.Build(layout, rows, spec, title, interactive: false, forExport, renderId, actionsJson: null);
            // Custom TemplateContent without chart markup still needs UiDashboardHtml for export charts.
            if (forExport && body.IndexOf("data-rasterize", StringComparison.OrdinalIgnoreCase) < 0
                && body.IndexOf("chart-surface", StringComparison.OrdinalIgnoreCase) < 0)
                return UiDashboardHtml.Build(layout, rows, spec, title, interactive: false, forExport, renderId, actionsJson: null);
            return ApplyTitlePlaceholder(body, title ?? spec?.Title);
        }

        public static string ResolveLayoutBody(string toolName, string layout)
            => ResolveLayoutBody(toolName, layout, allowTableFormFallback: true);

        /// <param name="allowTableFormFallback">
        /// When false (e.g. layout=page), missing key → code default compose — do not reuse table HTML as the page template.
        /// </param>
        public static string ResolveLayoutBody(string toolName, string layout, bool allowTableFormFallback)
        {
            layout = NormalizeLayout(layout);
            UiTemplateContentMaterialize.EnsureOnce();
            try
            {
                var tool = AppAgentLibraryToolBL.GetByLibraryAndToolName(LibraryKey, toolName);
                if (tool == null || string.IsNullOrWhiteSpace(tool.TemplateContent))
                    return CodeDefaultMarker;
                var map = JObject.Parse(tool.TemplateContent);
                var token = map[layout];
                if (token == null && allowTableFormFallback)
                    token = map["table"] ?? map["form"];
                var text = token?.ToString();
                return string.IsNullOrWhiteSpace(text) ? CodeDefaultMarker : text;
            }
            catch
            {
                return CodeDefaultMarker;
            }
        }

        /// <summary>
        /// Code-default page (none-interactive): stack table + cards with platform section chrome.
        /// </summary>
        public static string ComposePageHtml(
            FormDocument formDoc,
            CardSheet cardSheet,
            string title,
            IDictionary<string, string> sectionTitles = null)
        {
            var pageTitle = title ?? formDoc?.Title ?? cardSheet?.Title ?? "Result";
            string tableInner = null;
            string cardsInner = null;
            if (formDoc != null)
            {
                var saved = formDoc.Title;
                formDoc.Title = null;
                try { tableInner = ExtractBodyInnerWithStyles(HtmlForm.Write(formDoc)); }
                finally { formDoc.Title = saved; }
            }
            if (cardSheet != null)
            {
                var saved = cardSheet.Title;
                cardSheet.Title = null;
                try { cardsInner = ExtractBodyInnerWithStyles(HtmlForm.WriteCards(cardSheet, false)); }
                finally { cardSheet.Title = saved; }
            }

            var sections = new List<UiStackedSection>();
            if (!string.IsNullOrWhiteSpace(tableInner))
            {
                sections.Add(new UiStackedSection
                {
                    Title = ResolveSectionTitle(sectionTitles, "table", pageTitle, "Table view"),
                    BodyHtml = tableInner,
                    ApplyGridMaxHeight = true
                });
            }
            if (!string.IsNullOrWhiteSpace(cardsInner))
            {
                sections.Add(new UiStackedSection
                {
                    Title = ResolveSectionTitle(sectionTitles, "cards", pageTitle, "Card view"),
                    BodyHtml = cardsInner,
                    ApplyGridMaxHeight = false
                });
            }
            return UiSectionChrome.ComposeStacked(pageTitle, sections);
        }

        /// <summary>
        /// Print/Export for layout=page: Grid (data table) then fitted Card pages.
        /// One page title; section labels only — no repeated headings inside each block.
        /// Grid uses <see cref="UiDashboardHtml.BuildGridPageFitCss"/> (same font scale as dashboard export).
        /// Do not inject <see cref="HtmlForm.ForPrint"/> here: its global tr{page-break-inside:avoid}
        /// wraps the entire nested grid in one unbreakable row and leaves a blank first page.
        /// </summary>
        public static string ComposePageExportHtml(
            FormDocument formDoc,
            CardSheet cardSheet,
            string title)
        {
            var pageTitle = string.IsNullOrWhiteSpace(title)
                ? (formDoc?.Title ?? cardSheet?.Title ?? "Result")
                : title.Trim();
            var colCount = formDoc?.FirstGridColumnCount() ?? 0;
            var fitStub = colCount > 0
                ? "<section class=\"data-table\" data-cols=\""
                    + colCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "\"></section>"
                : "";
            var sb = new System.Text.StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
            sb.Append("<style>");
            sb.Append("@page{size:A4 portrait;margin:12mm}");
            // Contain wide grids so they don't inflate body width; cards then fill the same content box.
            sb.Append("html,body{margin:0;padding:0;width:100%;max-width:100%;overflow-x:hidden;");
            sb.Append("font-family:Arial,Helvetica,sans-serif;color:#111827;background:#ffffff;box-sizing:border-box}");
            sb.Append("*,*:before,*:after{box-sizing:border-box}");
            sb.Append("h1.page-export-title{font-size:16px;line-height:20px;font-weight:700;text-align:center;");
            sb.Append("margin:0 0 10px 0;page-break-after:avoid}");
            sb.Append("h2.page-export-section{font-size:13px;line-height:16px;font-weight:600;margin:0 0 8px 0;");
            sb.Append("page-break-after:avoid}");
            sb.Append(".page-export-break{page-break-before:always}");
            sb.Append("#export-grid{margin:0 0 8px 0;width:100%;max-width:100%}");
            // Outer form wrapper must break across pages (one tr holds the whole grid).
            sb.Append("#export-grid table[data-doc=\"form\"]>tr{page-break-inside:auto!important;break-inside:auto!important}");
            sb.Append("#export-grid table[data-role=\"grid\"]>tr{page-break-inside:avoid;break-inside:avoid}");
            sb.Append("#export-grid img{max-width:100%;max-height:48px;width:auto;height:auto;object-fit:contain}");
            if (!string.IsNullOrEmpty(fitStub))
                sb.Append(UiDashboardHtml.BuildGridPageFitCss(fitStub));
            // Fitted cards use fixed 180mm; inside page export that leaves a right gutter — stretch to 100%.
            sb.Append("#export-cards{width:100%;max-width:100%}");
            sb.Append("#export-cards .card-page{width:100%!important;max-width:100%;height:auto!important;");
            sb.Append("min-height:240mm;margin:0 auto}");
            sb.Append("#export-cards .card-page>table{width:100%!important;max-width:100%;height:auto!important}");
            // Last fitted page must not force a trailing blank sheet.
            sb.Append("#export-cards .card-page:last-child{page-break-after:auto!important;break-after:auto!important}");
            sb.Append("</style></head><body>");
            sb.Append("<h1 class=\"page-export-title\">");
            sb.Append(System.Net.WebUtility.HtmlEncode(pageTitle));
            sb.Append("</h1>");

            if (formDoc != null)
            {
                sb.Append("<h2 class=\"page-export-section\">Grid view</h2>");
                sb.Append("<div id=\"export-grid\">");
                sb.Append("<section class=\"data-table\" data-role=\"grid\" data-cols=\"");
                sb.Append(colCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append("\">");
                var savedTitle = formDoc.Title;
                formDoc.Title = null;
                try
                {
                    sb.Append(ExtractBodyInnerWithStyles(HtmlForm.Write(formDoc)));
                }
                finally { formDoc.Title = savedTitle; }
                sb.Append("</section></div>");
            }

            if (cardSheet != null && cardSheet.Cards != null && cardSheet.Cards.Count > 0)
            {
                if (formDoc != null)
                    sb.Append("<div class=\"page-export-break\"></div>");
                sb.Append("<h2 class=\"page-export-section\">Card view</h2>");
                sb.Append("<div id=\"export-cards\">");
                var savedCardTitle = cardSheet.Title;
                // Fitted pages already show a title bar; keep empty to avoid triple titles.
                cardSheet.Title = "";
                try
                {
                    var fitted = HtmlForm.WriteCards(cardSheet, forPrint: true);
                    sb.Append(ExtractBodyInnerWithStyles(fitted));
                }
                finally { cardSheet.Title = savedCardTitle; }
                sb.Append("</div>");
            }

            sb.Append("</body></html>");
            return PrepareExportHtml(sb.ToString());
        }

        /// <summary>
        /// Interactive page: TemplateContent layout (table=Wijmo host, cards=Scriban) + platform collapse chrome.
        /// </summary>
        public static string ComposeInteractivePageHtml(
            string title,
            JArray dashRows,
            FormDocument formDoc,
            CardSheet cardSheet,
            IDictionary<string, string> sectionTitles,
            string renderId,
            string actionsJson)
        {
            var pageTitle = title ?? formDoc?.Title ?? cardSheet?.Title ?? "Result";
            var sections = new List<UiStackedSection>();
            var injectWijmo = false;

            // Table section only when caller loaded formDoc (respects specJson.views).
            if (formDoc != null)
            {
                var tableBody = ResolveLayoutBody(InteractiveTool, "table");
                string gridInner;
                // code_default OR custom LAYOUT with #gridHost → Wijmo boot (not title-only placeholder).
                if (dashRows != null && (IsCodeDefault(tableBody) || HasGridHost(tableBody)))
                {
                    if (IsCodeDefault(tableBody))
                        gridInner = InteractiveTabularHtml.GridHostMarkup();
                    else
                    {
                        try
                        {
                            var html = UiTemplateScriban.Render(
                                tableBody, UiTemplateScriban.ModelFromRows(pageTitle, dashRows));
                            gridInner = ExtractBodyInnerWithStyles(html);
                        }
                        catch
                        {
                            gridInner = InteractiveTabularHtml.GridHostMarkup();
                        }
                    }
                    injectWijmo = true;
                }
                else if (IsCodeDefault(tableBody))
                {
                    var saved = formDoc.Title;
                    formDoc.Title = null;
                    try { gridInner = ExtractBodyInnerWithStyles(HtmlForm.Write(formDoc)); }
                    finally { formDoc.Title = saved; }
                }
                else
                {
                    try
                    {
                        var html = UiTemplateScriban.Render(
                            tableBody, UiTemplateScriban.ModelFromForm(formDoc));
                        gridInner = ExtractBodyInnerWithStyles(html);
                    }
                    catch
                    {
                        var saved = formDoc.Title;
                        formDoc.Title = null;
                        try { gridInner = ExtractBodyInnerWithStyles(HtmlForm.Write(formDoc)); }
                        finally { formDoc.Title = saved; }
                    }
                }

                sections.Add(new UiStackedSection
                {
                    Title = ResolveSectionTitle(sectionTitles, "table", pageTitle, "Table view"),
                    BodyHtml = gridInner,
                    ApplyGridMaxHeight = false // Wijmo host has its own max-height
                });
            }

            if (cardSheet != null)
            {
                var cardsBody = ResolveLayoutBody(InteractiveTool, "cards");
                var saved = cardSheet.Title;
                cardSheet.Title = null;
                string cardsInner;
                try
                {
                    if (IsCodeDefault(cardsBody))
                        cardsInner = ExtractBodyInnerWithStyles(HtmlForm.WriteCards(cardSheet, false));
                    else
                    {
                        // Scriban + keep &lt;head&gt; CSS (ExtractBodyInner alone dropped card layout styles).
                        var html = UiTemplateScriban.Render(
                            cardsBody, UiTemplateScriban.ModelFromCards(cardSheet));
                        cardsInner = ExtractBodyInnerWithStyles(html);
                    }
                }
                catch
                {
                    cardsInner = ExtractBodyInnerWithStyles(HtmlForm.WriteCards(cardSheet, false));
                }
                finally { cardSheet.Title = saved; }

                sections.Add(new UiStackedSection
                {
                    Title = ResolveSectionTitle(sectionTitles, "cards", pageTitle, "Card view"),
                    BodyHtml = cardsInner,
                    ApplyGridMaxHeight = false
                });
            }

            var doc = UiSectionChrome.ComposeStacked(pageTitle, sections);
            if (injectWijmo && dashRows != null)
                doc = InteractiveTabularHtml.InjectWijmoGridBoot(doc, dashRows, renderId, actionsJson);
            return doc;
        }

        private static bool HasGridHost(string html) =>
            !string.IsNullOrEmpty(html)
            && Regex.IsMatch(html, @"id\s*=\s*[""']gridHost[""']", RegexOptions.IgnoreCase);

        /// <summary>Materialized empty page stub with no grid/cards content — must Compose*.</summary>
        private static bool IsEmptyPageShell(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return true;
            var hasContent = HasGridHost(body)
                || Regex.IsMatch(body, @"data-role\s*=\s*[""']grid[""']", RegexOptions.IgnoreCase)
                || Regex.IsMatch(body, @"data-role\s*=\s*[""']cards[""']", RegexOptions.IgnoreCase)
                || Regex.IsMatch(body, @"\{\{\s*for\s+", RegexOptions.IgnoreCase);
            if (hasContent) return false;
            return Regex.IsMatch(body, @"data-role\s*=\s*[""']page[""']", RegexOptions.IgnoreCase)
                   || body.IndexOf("platform stacks table + cards", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Materialized kpi/chart/dashboard stub — no KPI/chart data binds; use UiDashboardHtml.</summary>
        private static bool IsEmptyDashboardShell(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return true;
            if (body.IndexOf("chart-surface", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("data-rasterize", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("kpi-card", StringComparison.OrdinalIgnoreCase) >= 0
                || body.IndexOf("agent-kpi-", StringComparison.OrdinalIgnoreCase) >= 0
                || Regex.IsMatch(body, @"\{\{\s*for\s+", RegexOptions.IgnoreCase))
                return false;
            return Regex.IsMatch(body, @"data-role\s*=\s*[""']dashboard[""']", RegexOptions.IgnoreCase)
                   || Regex.IsMatch(body, @"data-role\s*=\s*[""']chart[""']", RegexOptions.IgnoreCase)
                   || body.IndexOf("export uses UiDashboardHtml", StringComparison.OrdinalIgnoreCase) >= 0
                   || body.IndexOf("none-interactive / export bind", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ResolveSectionTitle(
            IDictionary<string, string> sectionTitles,
            string viewKey,
            string pageTitle,
            string fallbackLabel)
        {
            if (sectionTitles != null)
            {
                foreach (var key in SectionTitleKeys(viewKey))
                {
                    if (sectionTitles.TryGetValue(key, out var t) && !string.IsNullOrWhiteSpace(t))
                        return t.Trim();
                }
            }
            if (!string.IsNullOrWhiteSpace(pageTitle))
                return fallbackLabel + " — " + pageTitle.Trim();
            return fallbackLabel;
        }

        private static IEnumerable<string> SectionTitleKeys(string viewKey)
        {
            var k = (viewKey ?? "").Trim().ToLowerInvariant();
            if (k is "table" or "grid")
            {
                yield return "table";
                yield return "grid";
                yield return "Table";
                yield return "Grid";
            }
            else if (k is "cards" or "card")
            {
                yield return "cards";
                yield return "card";
                yield return "Cards";
                yield return "Card";
            }
            else
                yield return k;
        }

        private static string ExtractBodyInner(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var m = Regex.Match(html, @"<body[^>]*>([\s\S]*?)</body>", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : html;
        }

        /// <summary>
        /// Body inner HTML plus &lt;style&gt; / agent-cards.css link from the document head.
        /// Page Compose discards &lt;head&gt;; without this, TemplateContent card CSS is lost.
        /// </summary>
        private static string ExtractBodyInnerWithStyles(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var body = ExtractBodyInner(html);
            var prefix = new System.Text.StringBuilder();
            if (Regex.IsMatch(html, @"agent-cards\.css", RegexOptions.IgnoreCase)
                && body.IndexOf("agent-cards.css", StringComparison.OrdinalIgnoreCase) < 0)
                prefix.Append("<link rel=\"stylesheet\" href=\"/agent-ui/agent-cards.css\"/>");
            if (Regex.IsMatch(html, @"agent-kpi\.css", RegexOptions.IgnoreCase)
                && body.IndexOf("agent-kpi.css", StringComparison.OrdinalIgnoreCase) < 0)
                prefix.Append("<link rel=\"stylesheet\" href=\"/agent-ui/agent-kpi.css\"/>");
            var styles = ExtractStyleBlocks(html);
            if (!string.IsNullOrWhiteSpace(styles))
                prefix.Append("<style>\n").Append(styles).Append("\n</style>\n");
            return prefix.Length == 0 ? body : prefix + body;
        }

        private static string ExtractStyleBlocks(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (Match m in Regex.Matches(html, @"<style\b[^>]*>([\s\S]*?)</style>", RegexOptions.IgnoreCase))
            {
                if (m.Groups.Count > 1 && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
                    sb.Append(m.Groups[1].Value.Trim()).Append('\n');
            }
            return sb.ToString().Trim();
        }

        public static string GetPrintCss()
        {
            try
            {
                var tool = AppAgentLibraryToolBL.GetByLibraryAndToolName(LibraryKey, NoneInteractiveTool);
                if (tool == null || string.IsNullOrWhiteSpace(tool.TemplateMetaJson))
                    return null;
                var meta = JObject.Parse(tool.TemplateMetaJson);
                return meta["printCss"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static string NormalizeLayout(string layout)
        {
            var n = (layout ?? "").Trim().ToLowerInvariant();
            if (n is "page" or "multi" or "gallery" or "split")
                return "page";
            if (n == "form" || n == "table" || n == "cards" || n == "kpi" || n == "chart" || n == "dashboard")
                return n;
            return "table";
        }

        /// <summary>One Result page stacking table + cards (show-only; no KPI gate).</summary>
        public static bool IsPageLayout(string layout) => NormalizeLayout(layout) == "page";

        public static string MapLegacyUi(string ui, out string mappedFrom)
        {
            mappedFrom = null;
            var u = (ui ?? "").Trim().ToLowerInvariant();
            if (u == "grid") return "table";
            if (u == "card") return "cards";
            if (u == "chart")
            {
                mappedFrom = u;
                return "chart";
            }
            if (u == "kpi")
            {
                mappedFrom = u;
                return "kpi";
            }
            if (u == "kpi_dashboard")
            {
                mappedFrom = u;
                return "dashboard";
            }
            if (u == "form" || u == "table" || u == "cards" || u == "dashboard") return u;
            return "table";
        }

        private static string InferFormLayout(FormDocument doc)
        {
            if (doc == null) return "table";
            if (doc.Fields.Count > 0 || !string.IsNullOrWhiteSpace(doc.Notes))
                return "form";
            return "table";
        }

        private static bool IsCodeDefault(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return true;
            var t = body.Trim();
            return t.Equals(CodeDefaultMarker, StringComparison.OrdinalIgnoreCase)
                   || t == "{}"
                   || t == "\"\"";
        }

        private static string ApplyTitlePlaceholder(string template, string title)
        {
            var t = template ?? "";
            t = Regex.Replace(t, @"\{\{\s*title\s*\}\}", System.Net.WebUtility.HtmlEncode(title ?? ""), RegexOptions.IgnoreCase);
            return t;
        }

        private static string StripScripts(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;
            return Regex.Replace(html, @"<script\b[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        }

        private static string WrapInteractive(string html, string renderId, string actionsJson)
        {
            var rid = System.Net.WebUtility.HtmlEncode(renderId ?? "");
            var buttons = "";
            try
            {
                if (!string.IsNullOrWhiteSpace(actionsJson))
                {
                    var arr = JToken.Parse(actionsJson) as JArray;
                    if (arr != null)
                    {
                        foreach (var a in arr)
                        {
                            var id = a["Id"]?.ToString() ?? a["id"]?.ToString() ?? "";
                            var label = a["Label"]?.ToString() ?? a["label"]?.ToString() ?? id;
                            if (string.IsNullOrWhiteSpace(id)) continue;
                            buttons += "<button type=\"button\" data-action=\"" +
                                       System.Net.WebUtility.HtmlEncode(id) +
                                       "\" style=\"margin:4px;padding:6px 10px;font:12px Arial;\">" +
                                       System.Net.WebUtility.HtmlEncode(label) +
                                       "</button>";
                        }
                    }
                }
            }
            catch { /* ignore bad actionsJson */ }

            var bar = string.IsNullOrEmpty(buttons)
                ? ""
                : "<div id=\"agent-ui-actions\" style=\"padding:8px;border-bottom:1px solid #ddd;font-family:Arial,sans-serif;\">" + buttons + "</div>";

            var script = @"<script>
(function(){
  var renderId=" + JsonConvert.SerializeObject(renderId ?? "") + @";
  function post(actionId, selection){
    try{
      parent.postMessage({type:'agent_ui_action',renderId:renderId,actionId:actionId,selection:selection||{}}, '*');
    }catch(e){}
  }
  document.addEventListener('click', function(ev){
    var t=ev.target;
    if(!t || !t.getAttribute) return;
    var id=t.getAttribute('data-action');
    if(id){ post(id, {}); ev.preventDefault(); }
  });
  document.addEventListener('click', function(ev){
    var tr=ev.target && ev.target.closest ? ev.target.closest('tr[data-row]') : null;
    if(!tr) return;
    var raw=tr.getAttribute('data-row');
    try{ post('select_row', raw ? JSON.parse(raw) : {}); }catch(e){ post('select_row', {}); }
  });
})();
</script>";

            if (html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                html = Regex.Replace(html, @"<body([^>]*)>", "<body$1>" + bar, RegexOptions.IgnoreCase);
                return Regex.Replace(html, @"</body>", script + "</body>", RegexOptions.IgnoreCase);
            }
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>" + bar + html + script + "</body></html>";
        }
    }
}
