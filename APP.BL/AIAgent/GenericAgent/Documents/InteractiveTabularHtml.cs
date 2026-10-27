using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Legacy iframe Wijmo host helpers (export / none-interactive twins only).
    /// Interactive Result grids use React + app @mescius — do not vendor UMD under public/agent-ui/wijmo.
    /// </summary>
    public static class InteractiveTabularHtml
    {
        public static string GridHostMarkup(string hostId = "gridHost")
        {
            var id = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(hostId) ? "gridHost" : hostId);
            // overflow:visible so FlexGridFilter popup is not clipped; grid scrolls inside #host via height.
            return "<div class=\"agent-wijmo-grid-wrap\" style=\"max-height:" + UiSectionChrome.GridMaxHeightCss +
                   ";overflow:visible;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:8px;min-height:200px;box-sizing:border-box;max-width:100%;position:relative;z-index:1;\">" +
                   "<div id=\"" + id + "\" style=\"width:100%;height:min(70vh,720px);max-width:100%;\"></div></div>";
        }

        /// <summary>Full Result document: one Wijmo grid (+ optional actions via WrapInteractive later).</summary>
        public static string BuildTableDocument(
            string title,
            JArray rows,
            string renderId,
            string actionsJson,
            string wijmoLicenseKey = null)
        {
            rows ??= new JArray();
            var pageTitle = string.IsNullOrWhiteSpace(title) ? "Table" : title.Trim();
            var sb = new StringBuilder(32_000);
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
            sb.Append("<title>").Append(WebUtility.HtmlEncode(pageTitle)).Append("</title>");
            AppendWijmoAssets(sb, wijmoLicenseKey);
            sb.Append("<style>body{margin:0;padding:12px;font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a;max-width:100%;box-sizing:border-box;overflow-x:hidden}");
            sb.Append("h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}");
            sb.Append(UiSectionChrome.SectionChromeCss());
            sb.Append(FilterPopupCss);
            sb.Append("</style></head><body>");
            sb.Append("<h1 data-role=\"title\">").Append(WebUtility.HtmlEncode(pageTitle)).Append("</h1>");
            sb.Append(GridHostMarkup());
            AppendAgentUiBoot(sb, rows, renderId, actionsJson, wijmoLicenseKey, "gridHost");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        /// <summary>Inject Wijmo assets + FlexGrid boot into an existing HTML document that contains #gridHost.</summary>
        public static string InjectWijmoGridBoot(
            string htmlDocument,
            JArray rows,
            string renderId,
            string actionsJson,
            string wijmoLicenseKey = null,
            string hostId = "gridHost")
        {
            if (string.IsNullOrWhiteSpace(htmlDocument)) return htmlDocument;
            var headBits = new StringBuilder();
            AppendWijmoAssets(headBits, wijmoLicenseKey);
            headBits.Append("<style>").Append(UiSectionChrome.SectionChromeCss()).Append(FilterPopupCss).Append("</style>");
            var boot = new StringBuilder();
            AppendAgentUiBoot(boot, rows, renderId, actionsJson, wijmoLicenseKey, hostId);

            var html = htmlDocument;
            if (Regex.IsMatch(html, "</head>", RegexOptions.IgnoreCase))
                html = Regex.Replace(html, "</head>", headBits + "</head>", RegexOptions.IgnoreCase);
            else
                html = headBits + html;

            if (Regex.IsMatch(html, "</body>", RegexOptions.IgnoreCase))
                html = Regex.Replace(html, "</body>", boot + "</body>", RegexOptions.IgnoreCase);
            else
                html += boot;
            return html;
        }

        private static void AppendWijmoAssets(StringBuilder sb, string wijmoLicenseKey)
        {
            var license = string.IsNullOrWhiteSpace(wijmoLicenseKey)
                ? InteractiveDashboardHtml.DefaultWijmoLicenseKey
                : wijmoLicenseKey.Trim();
            // Legacy HTML-only path (not Interactive Result). Prefer React @mescius for Result grids.
            var cdn = "https://cdn.mescius.com/wijmo/" + InteractiveDashboardHtml.WijmoCdnVersion;
            sb.Append("<link rel=\"stylesheet\" href=\"").Append(cdn).Append("/styles/wijmo.min.css\"/>");
            sb.Append("<script src=\"").Append(cdn).Append("/controls/wijmo.min.js\"></script>");
            sb.Append("<script>(function(){try{var k=");
            sb.Append(JsonConvert.SerializeObject(license));
            sb.Append(";window.__WIJMO_LICENSE_KEY__=k;");
            sb.Append("var w=window.wijmo;if(!w)return;");
            sb.Append("if(typeof w.setLicenseKey==='function')w.setLicenseKey(k);");
            sb.Append("else if(w.Control&&typeof w.Control.setLicenseKey==='function')w.Control.setLicenseKey(k);");
            sb.Append("}catch(e){}})();</script>");
            sb.Append("<script src=\"").Append(cdn).Append("/controls/wijmo.input.min.js\"></script>");
            sb.Append("<script src=\"").Append(cdn).Append("/controls/wijmo.grid.min.js\"></script>");
            sb.Append("<script src=\"").Append(cdn).Append("/controls/wijmo.grid.filter.min.js\"></script>");
        }

        private static void AppendAgentUiBoot(
            StringBuilder sb,
            JArray rows,
            string renderId,
            string actionsJson,
            string wijmoLicenseKey,
            string hostId)
        {
            var license = string.IsNullOrWhiteSpace(wijmoLicenseKey)
                ? InteractiveDashboardHtml.DefaultWijmoLicenseKey
                : wijmoLicenseKey.Trim();
            var actions = string.IsNullOrWhiteSpace(actionsJson) ? "[]" : actionsJson.Trim();
            if (!actions.StartsWith("[")) actions = "[]";

            sb.Append("<script>window.__AGENT_UI__=");
            sb.Append(JsonConvert.SerializeObject(new
            {
                renderId = renderId ?? "",
                data = rows ?? new JArray(),
                actions = JToken.Parse(actions),
                license,
                gridHostId = string.IsNullOrWhiteSpace(hostId) ? "gridHost" : hostId
            }));
            sb.Append(";</script><script>");
            sb.Append(GridBootJs);
            sb.Append("</script>");
        }

        private const string FilterPopupCss =
            "details.agent-ui-section,.agent-ui-section-body,.agent-wijmo-grid-wrap{overflow:visible!important;}" +
            ".wj-dropdown,.wj-popup,.wj-control.wj-content.wj-dropdown," +
            ".wj-columnfiltereditor,.wj-listbox{z-index:10050!important;position:fixed!important;}" +
            ".wj-flexgrid{max-width:100%;}";

        private const string GridBootJs = @"
(function () {
  var cfg = window.__AGENT_UI__ || {};
  var rows = Array.isArray(cfg.data) ? cfg.data : [];
  var hostId = cfg.gridHostId || 'gridHost';
  if (typeof wijmo === 'undefined') {
    var err = document.createElement('div');
    err.style.cssText = 'color:#b91c1c;padding:12px;font:13px Arial';
    err.textContent = 'Wijmo failed to load. Check network / license.';
    document.body.insertBefore(err, document.body.firstChild);
    return;
  }
  try {
    var key = cfg.license || window.__WIJMO_LICENSE_KEY__ || '';
    if (key) {
      if (typeof wijmo.setLicenseKey === 'function') wijmo.setLicenseKey(key);
      else if (wijmo.Control && typeof wijmo.Control.setLicenseKey === 'function') wijmo.Control.setLicenseKey(key);
    }
  } catch (e) {}
  // Keep filter popups on document.body so <details> does not clip them.
  try {
    if (!window.__agentWijmoPopupWatch) {
      window.__agentWijmoPopupWatch = true;
      var mo = new MutationObserver(function () {
        var nodes = document.querySelectorAll('.wj-popup,.wj-dropdown,.wj-columnfiltereditor');
        for (var i = 0; i < nodes.length; i++) {
          var el = nodes[i];
          if (el && el.parentElement !== document.body) document.body.appendChild(el);
        }
      });
      mo.observe(document.documentElement, { childList: true, subtree: true });
    }
  } catch (e0) {}
  var host = document.getElementById(hostId);
  if (!host) return;
  var cv = new wijmo.collections.CollectionView(rows);
  cv.trackChanges = false;
  var grid = new wijmo.grid.FlexGrid(host, {
    itemsSource: cv,
    isReadOnly: true,
    selectionMode: 'Row',
    headersVisibility: 'Column',
    alternatingRowStep: 1,
    autoGenerateColumns: true
  });
  try { grid.autoSizeColumns(); } catch (e3) {}
  setTimeout(function () {
    try {
      if (!wijmo.grid || !wijmo.grid.filter) return;
      var filter = new wijmo.grid.filter.FlexGridFilter(grid);
      var bindings = [];
      for (var i = 0; i < grid.columns.length; i++) {
        var col = grid.columns[i];
        if (col && col.binding) bindings.push(col.binding);
      }
      if (bindings.length) filter.filterColumns = bindings;
    } catch (e2) {}
  }, 0);
  host.addEventListener('dblclick', function () {
    try {
      var item = grid.collectionView && grid.collectionView.currentItem;
      if (!item) return;
      parent.postMessage({ type: 'agent_ui_action', renderId: cfg.renderId || '', actionId: 'select_row', selection: item }, '*');
    } catch (e4) {}
  });
})();
";
    }
}
