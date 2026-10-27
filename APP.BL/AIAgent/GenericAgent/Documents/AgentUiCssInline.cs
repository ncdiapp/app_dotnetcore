using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Inlines /agent-ui/*.css into HTML so Playwright PDF (and offline hosts) do not need the SPA origin.
    /// </summary>
    public static class AgentUiCssInline
    {
        private static readonly Regex LinkHref = new Regex(
            @"<link\b[^>]*href\s*=\s*[""'](?<href>/agent-ui/[^""']+\.css)[""'][^>]*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Extra print helpers appended when preparing export HTML (does not replace TemplateContent layout).</summary>
        public const string ExportPrintExtrasCss =
            @"@media print {
  .agent-cards-root { background: #fff; }
  .agent-card { break-inside: avoid; page-break-inside: avoid; }
  .agent-cards-grid { gap: 0.5rem; }
}
.agent-card-image { max-width: 100%; max-height: 160px; object-fit: contain; display: block; margin: 0.25rem 0; }
";

        public static string PrepareExportHtml(string html, bool addPrintExtras = true)
        {
            if (string.IsNullOrWhiteSpace(html)) return html ?? "";
            var page = InlineAgentUiLinks(html);
            if (addPrintExtras)
                page = InjectStyle(page, ExportPrintExtrasCss);
            return page;
        }

        public static string InlineAgentUiLinks(string html)
        {
            if (string.IsNullOrEmpty(html)) return html ?? "";
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return LinkHref.Replace(html, m =>
            {
                var href = m.Groups["href"].Value.Trim();
                if (!seen.Add(href)) return "";
                var css = TryReadAgentUiFile(href);
                if (string.IsNullOrEmpty(css))
                    return m.Value;
                return "<style data-agent-ui=\"" + System.Net.WebUtility.HtmlEncode(href) + "\">\n"
                       + css + "\n</style>";
            });
        }

        public static string TryReadAgentUiFile(string href)
        {
            if (string.IsNullOrWhiteSpace(href)) return null;
            var name = href.Replace('\\', '/').Trim();
            var idx = name.IndexOf("/agent-ui/", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var relative = name.Substring(idx + "/agent-ui/".Length).TrimStart('/');
            if (relative.IndexOf("..", StringComparison.Ordinal) >= 0) return null;

            foreach (var root in CandidateRoots())
            {
                var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    if (File.Exists(path))
                        return File.ReadAllText(path, Encoding.UTF8);
                }
                catch { /* try next */ }
            }
            return null;
        }

        private static IEnumerable<string> CandidateRoots()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string dir)
            {
                if (string.IsNullOrWhiteSpace(dir)) return;
                try
                {
                    var full = Path.GetFullPath(dir);
                    if (Directory.Exists(full)) seen.Add(full);
                }
                catch { /* ignore */ }
            }

            Add(Path.Combine(AppContext.BaseDirectory, "wwwroot", "agent-ui"));
            Add(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "agent-ui"));
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                try
                {
                    var dir = new DirectoryInfo(start);
                    for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                    {
                        Add(Path.Combine(dir.FullName, "AppReact", "public", "agent-ui"));
                        Add(Path.Combine(dir.FullName, "public", "agent-ui"));
                        Add(Path.Combine(dir.FullName, "AppAI.Web", "wwwroot", "agent-ui"));
                    }
                }
                catch { /* ignore */ }
            }
            return seen;
        }

        private static string InjectStyle(string html, string css)
        {
            if (string.IsNullOrWhiteSpace(css)) return html;
            var tag = "<style data-agent-ui-print=\"1\">\n" + css + "\n</style>";
            var head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            if (head >= 0)
                return html.Insert(head + 6, tag);
            var headAny = Regex.Match(html, @"<head\b[^>]*>", RegexOptions.IgnoreCase);
            if (headAny.Success)
                return html.Insert(headAny.Index + headAny.Length, tag);
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>" + tag + "</head><body>" + html + "</body></html>";
        }
    }
}
