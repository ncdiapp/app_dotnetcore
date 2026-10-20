using System;
using System.Text.RegularExpressions;
using App.BL.TenantBusiness;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Loads TemplateContent from render_none_interactive / render_interactive and merges
    /// FormDocument / CardSheet to HTML. {{code_default}} (or empty) uses HtmlForm writers
    /// so Result and export_html share the same body.
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
        {
            layout = NormalizeLayout(layout);
            string html;
            if (IsDashboardLayout(layout))
            {
                var body = ResolveLayoutBody(interactive ? InteractiveTool : NoneInteractiveTool, layout);
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
            else if (layout == "cards" && cardSheet != null)
            {
                var body = ResolveLayoutBody(interactive ? InteractiveTool : NoneInteractiveTool, "cards");
                html = IsCodeDefault(body) ? HtmlForm.WriteCards(cardSheet, false) : ApplyTitlePlaceholder(body, cardSheet.Title);
            }
            else if (formDoc != null)
            {
                var key = layout == "form" ? "form" : "table";
                var body = ResolveLayoutBody(interactive ? InteractiveTool : NoneInteractiveTool, key);
                html = IsCodeDefault(body) ? HtmlForm.Write(formDoc) : ApplyTitlePlaceholder(body, formDoc.Title);
            }
            else
            {
                html = "<!DOCTYPE html><html><body><p>No data</p></body></html>";
            }

            // Dashboard interactive already embeds actions + postMessage in UiDashboardHtml
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
        {
            layout = NormalizeLayout(layout);
            try
            {
                var tool = AppAgentLibraryToolBL.GetByLibraryAndToolName(LibraryKey, toolName);
                if (tool == null || string.IsNullOrWhiteSpace(tool.TemplateContent))
                    return CodeDefaultMarker;
                var map = JObject.Parse(tool.TemplateContent);
                var token = map[layout] ?? map["table"] ?? map["form"];
                var text = token?.ToString();
                return string.IsNullOrWhiteSpace(text) ? CodeDefaultMarker : text;
            }
            catch
            {
                return CodeDefaultMarker;
            }
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
            if (n == "form" || n == "table" || n == "cards" || n == "kpi" || n == "chart" || n == "dashboard")
                return n;
            return "table";
        }

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
