using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Platform collapse/expand chrome around stacked Result controls.
    /// Not stored in TemplateContent — templates only supply each control's body.
    /// </summary>
    public static class UiSectionChrome
    {
        /// <summary>Grid pane scroll box (Result only).</summary>
        public const string GridMaxHeightCss = "min(80vh, 800px)";

        public static string WrapGridScroll(string innerHtml)
        {
            return "<div data-role=\"grid-scroll\" style=\"max-height:" + GridMaxHeightCss +
                   ";overflow:auto;border:1px solid #e6e8eb;border-radius:6px;background:#fff;\">" +
                   (innerHtml ?? "") + "</div>";
        }

        /// <summary>
        /// When <paramref name="sections"/> has 2+ items, wrap each in a collapse/expand details block.
        /// Single section: optional grid scroll only (no collapse chrome).
        /// </summary>
        public static string ComposeStacked(string pageTitle, IReadOnlyList<UiStackedSection> sections)
        {
            if (sections == null || sections.Count == 0)
                return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body><p>No data</p></body></html>";

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>");
            sb.Append(WebUtility.HtmlEncode(pageTitle ?? "Result"));
            sb.Append("</title><style>");
            sb.Append("body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f4f6f8;color:#111827;}");
            sb.Append("details.agent-ui-section{margin:0 0 12px 0;background:#fff;border:1px solid #e6e8eb;border-radius:8px;overflow:visible;}");
            sb.Append("details.agent-ui-section>summary{cursor:pointer;list-style:none;padding:10px 14px;font-size:14px;font-weight:600;user-select:none;}");
            sb.Append("details.agent-ui-section>summary::-webkit-details-marker{display:none;}");
            sb.Append("details.agent-ui-section>summary::before{content:'\\25BC';display:inline-block;margin-right:8px;font-size:10px;color:#6b7280;}");
            sb.Append("details.agent-ui-section:not([open])>summary::before{content:'\\25B6';}");
            sb.Append(".agent-ui-section-body{padding:0 12px 12px 12px;overflow:visible;}");
            sb.Append(".agent-ui-page-title{margin:0 0 12px 0;font-size:18px;font-weight:bold;text-align:center;}");
            sb.Append("</style></head><body>");
            if (!string.IsNullOrWhiteSpace(pageTitle))
            {
                sb.Append("<h1 class=\"agent-ui-page-title\" data-role=\"title\">");
                sb.Append(WebUtility.HtmlEncode(pageTitle));
                sb.Append("</h1>");
            }

            sb.Append(RenderSections(sections, pageTitle));
            sb.Append("</body></html>");
            return sb.ToString();
        }

        /// <summary>Section markup only (for embedding inside an existing document body).</summary>
        public static string RenderSections(IReadOnlyList<UiStackedSection> sections, string pageTitle = null)
        {
            if (sections == null || sections.Count == 0) return "";
            var sb = new StringBuilder();
            var useCollapse = sections.Count >= 2;
            foreach (var sec in sections)
            {
                if (sec == null) continue;
                var body = sec.BodyHtml ?? "";
                if (sec.ApplyGridMaxHeight)
                    body = WrapGridScroll(body);

                if (useCollapse)
                {
                    sb.Append("<details class=\"agent-ui-section\" data-role=\"section\" open>");
                    sb.Append("<summary>").Append(WebUtility.HtmlEncode(sec.Title ?? "Section")).Append("</summary>");
                    sb.Append("<div class=\"agent-ui-section-body\">").Append(body).Append("</div>");
                    sb.Append("</details>");
                }
                else
                {
                    sb.Append("<div data-role=\"section\" style=\"margin-bottom:12px;\">");
                    if (!string.IsNullOrWhiteSpace(sec.Title) &&
                        !string.Equals(sec.Title, pageTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("<div style=\"font-size:14px;font-weight:600;margin:0 0 8px 0;\">");
                        sb.Append(WebUtility.HtmlEncode(sec.Title));
                        sb.Append("</div>");
                    }
                    sb.Append(body).Append("</div>");
                }
            }
            return sb.ToString();
        }

        public static string SectionChromeCss() =>
            "details.agent-ui-section{margin:0 0 12px 0;background:#fff;border:1px solid #e6e8eb;border-radius:8px;overflow:visible;}" +
            "details.agent-ui-section>summary{cursor:pointer;list-style:none;padding:10px 14px;font-size:14px;font-weight:600;user-select:none;}" +
            "details.agent-ui-section>summary::-webkit-details-marker{display:none;}" +
            "details.agent-ui-section>summary::before{content:'\\25BC';display:inline-block;margin-right:8px;font-size:10px;color:#6b7280;}" +
            "details.agent-ui-section:not([open])>summary::before{content:'\\25B6';}" +
            ".agent-ui-section-body{padding:0 12px 12px 12px;overflow:visible;}";
    }

    public sealed class UiStackedSection
    {
        public string Title { get; set; }
        public string BodyHtml { get; set; }
        public bool ApplyGridMaxHeight { get; set; }
    }
}
