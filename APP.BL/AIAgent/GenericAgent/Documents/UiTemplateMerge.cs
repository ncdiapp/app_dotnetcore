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
    /// data into HTML (MVC View pattern). {{code_default}} / empty → <see cref="UiDefaultViewTemplates"/>
    /// via Scriban; custom DB HTML is Scriban-bound the same way.
    /// Interactive Result grids use app Wijmo (React), not CDN — this merge feeds iframe / export.
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
            string html;
            if (IsCodeDefault(body))
                html = HtmlForm.Write(doc);
            else
                html = ApplyTitlePlaceholder(body, doc?.Title);
            return forPrint ? HtmlForm.ForPrint(html) : html;
        }

        public static string MergeTable(FormDocument doc, bool forPrint = false)
        {
            var body = ResolveLayoutBody(NoneInteractiveTool, "table");
            string html;
            if (IsCodeDefault(body))
                html = HtmlForm.Write(doc);
            else
                html = ApplyTitlePlaceholder(body, doc?.Title);
            return forPrint ? HtmlForm.ForPrint(html) : html;
        }

        public static string MergeCards(CardSheet sheet, bool forPrint = false)
        {
            var body = ResolveLayoutBody(NoneInteractiveTool, "cards");
            string html;
            if (IsCodeDefault(body))
                html = HtmlForm.WriteCards(sheet, forPrint);
            else
                html = ApplyTitlePlaceholder(body, sheet?.Title);
            return forPrint && !IsCodeDefault(body) ? HtmlForm.ForPrint(html) : html;
        }

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
            IDictionary<string, string> sectionTitles)
        {
            layout = NormalizeLayout(layout);
            var toolName = interactive ? InteractiveTool : NoneInteractiveTool;
            string html;
            if (IsDashboardLayout(layout))
            {
                var body = ResolveLayoutBody(toolName, layout);
                if (IsCodeDefault(body))
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
                if (IsCodeDefault(body))
                {
                    if (interactive && !forExport)
                        html = ComposeInteractivePageHtml(
                            pageTitle, dashRows, formDoc, cardSheet, sectionTitles, renderId, actionsJson);
                    else
                        html = ComposePageHtml(formDoc, cardSheet, pageTitle, sectionTitles);
                }
                else
                    html = ApplyTitlePlaceholder(body, pageTitle);
            }
            else if (layout == "cards" && cardSheet != null)
            {
                var body = ResolveLayoutBody(toolName, "cards");
                html = RenderCardsView(body, cardSheet);
            }
            else if (formDoc != null || (layout == "table" && dashRows != null))
            {
                var key = layout == "form" ? "form" : "table";
                var body = ResolveLayoutBody(toolName, key);
                // Interactive Result grid uses app Wijmo (React + DataJson) — no CDN HTML here.
                // TemplateContent merge builds none-interactive / export HTML only.
                if (formDoc != null)
                    html = RenderTableOrFormView(key, body, formDoc, forExport);
                else if (dashRows != null)
                    html = RenderTableFromRows(body, formDoc?.Title ?? dashSpec?.Title ?? "Table", dashRows, forExport);
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
            if (IsCodeDefault(body))
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
                try { tableInner = ExtractBodyInner(HtmlForm.Write(formDoc)); }
                finally { formDoc.Title = saved; }
            }
            if (cardSheet != null)
            {
                var saved = cardSheet.Title;
                cardSheet.Title = null;
                try { cardsInner = ExtractBodyInner(HtmlForm.WriteCards(cardSheet, false)); }
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
        /// Interactive page: TemplateContent layout (table=Wijmo host, cards=template cards) + platform collapse chrome.
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
                if (IsCodeDefault(tableBody) && dashRows != null)
                {
                    gridInner = InteractiveTabularHtml.GridHostMarkup();
                    injectWijmo = true;
                }
                else
                {
                    var saved = formDoc.Title;
                    formDoc.Title = null;
                    try { gridInner = ExtractBodyInner(IsCodeDefault(tableBody) ? HtmlForm.Write(formDoc) : ApplyTitlePlaceholder(tableBody, saved)); }
                    finally { formDoc.Title = saved; }
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
                    cardsInner = ExtractBodyInner(
                        IsCodeDefault(cardsBody) ? HtmlForm.WriteCards(cardSheet, false) : ApplyTitlePlaceholder(cardsBody, saved));
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
