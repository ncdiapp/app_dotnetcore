using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Print-like interactive KPI/dashboard page: loads Wijmo from CDN, embeds rows+spec.
    /// Served via token URL and embedded in Result iframe (src, not srcDoc).
    /// </summary>
    public static class InteractiveDashboardHtml
    {
        public const string WijmoCdnVersion = "5.20252.42";

        /// <summary>
        /// Same key as AppReact/src/license.js (setLicenseKey). Required to remove Wijmo watermark in the Result iframe page.
        /// </summary>
        public const string DefaultWijmoLicenseKey =
            "GrapeCity,427351286422477#B0JoIIklkIs4nIzYXMyAjMiojIyVmdiwSZzxWYmpjIyNHZisnOiwmbBJye0ICRiwiI34TQvAVW7ZWbqNjeWZzRh9Ucl5UTuZVaCR7ZpB5UH9EVC3kaqJWZ0pnasJ7Q9I4bB3GR0F6aIt6NZ96MFN4aotEZrUXe4kHUlllerlGb9dDSPhEcFFmclJXd8syNEtENzMTOBNVYpFje6lDUlBlbkdmcNNGOrAHR9pXNSl6NpVkbCNWUxlkZ7o7QT3icHNGavFXdapWQDZ5KsN5N7dDcyRUW85ESFBlb4JUZq3GWlBldXBzU0hjW9wkb8cncopnSOBjNkJFdyNmSqB7YVlnNzpnb9BDSMllTSFDaNFjca54dtBXatlEdS5mVNV6dxIEN7RUNxYGSvU7KIdndPpENvlXW6hzbCh7RsZ7YLJzboNnU7ZERTRkVBNlN7RWSGhDVrdmZqZ4cq94aFpkbWZDMGBHdVZ5YwUTTIdlN0RnVvMDdFJzbQt6bolDM5NkdrsUO536LrNWSotmI0IyUiwiIDRTR4MjM9QjI0ICSiwSMyIjM9UTMwEjM0IicfJye35XX3JSSwIjUiojIDJCLi86bpNnblRHeFBCI4VWZoNFelxmRg2Wbql6ViojIOJyes4nI5kkTRJiOiMkIsIibvl6cuVGd8VEIgIXZ7VWaWRncvBXZSBybtpWaXJiOi8kI1xSfis4N8gkI0IyQiwiIu3Waz9WZ4hXRgAydvJVa4xWdNBybtpWaXJiOi8kI1xSfiQjR6QkI0IyQiwiIu3Waz9WZ4hXRgACUBx4TgAybtpWaXJiOi8kI1xSfiMzQwIkI0IyQiwiIlJ7bDBybtpWaXJiOi8kI1xSfiUFO7EkI0IyQiwiIu3Waz9WZ4hXRgACdyFGaDxWYpNmbh9WaGBybtpWaXJiOi8kI1tlOiQmcQJCLiETMwAzNwACOwcDMwIDMyIiOiQncDJCLi46bj9idlRWe4l6YlBXYydmLqwibj9SbvNmL9RXajVGchJ7ZuoCLt36YukHdpNWZwFmcn9iKsI7au26YukHdpNWZwFmcn9iKsAnau26YukHdpNWZwFmcn9iKiojIz5GRiwiI9RXaDVGchJ7RiojIh94QiwiI7cDNyIDN6gjMxUzMdI6N";

        public static string Build(
            string title,
            string layout,
            JArray rows,
            UiRenderSpec spec,
            string actionsJson,
            string renderId,
            string wijmoLicenseKey = null)
        {
            rows ??= new JArray();
            spec ??= new UiRenderSpec();
            var pageTitle = !string.IsNullOrWhiteSpace(title) ? title : (spec.Title ?? layout ?? "Dashboard");
            var dataJson = rows.ToString(Formatting.None);
            var specJson = JsonConvert.SerializeObject(new
            {
                title = spec.Title,
                kpis = spec.Kpis,
                chart = spec.Chart,
                blocks = spec.Blocks
            }, Formatting.None);
            var actions = string.IsNullOrWhiteSpace(actionsJson) ? "[]" : actionsJson.Trim();
            if (!actions.StartsWith("[")) actions = "[]";
            var license = string.IsNullOrWhiteSpace(wijmoLicenseKey) ? DefaultWijmoLicenseKey : wijmoLicenseKey.Trim();

            var sb = new StringBuilder(64_000);
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
            sb.Append("<title>").Append(Enc(pageTitle)).Append("</title>");
            var cdn = "https://cdn.mescius.com/wijmo/" + WijmoCdnVersion;
            sb.Append("<link rel=\"stylesheet\" href=\"").Append(cdn).Append("/styles/wijmo.min.css\"/>");
            sb.Append("<style>");
            sb.Append(PageCss);
            sb.Append("</style></head><body>");
            sb.Append("<header class=\"hdr\"><h1>").Append(Enc(pageTitle)).Append("</h1>");
            sb.Append("<div class=\"toolbar\" id=\"toolbar\"></div></header>");
            sb.Append("<section class=\"kpi-row\" id=\"kpiRow\"></section>");
            sb.Append("<section class=\"charts\" id=\"charts\"></section>");
            sb.Append("<section class=\"grid-wrap\"><div id=\"gridHost\"></div></section>");
            sb.Append("<div class=\"actions\" id=\"actions\"></div>");

            // License MUST be applied immediately after core wijmo.min.js and BEFORE other control scripts,
            // otherwise CDN modules start in evaluation mode (watermark) and stay that way.
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
            sb.Append("<script src=\"").Append(cdn).Append("/controls/wijmo.chart.min.js\"></script>");

            sb.Append("<script>");
            sb.Append("window.__AGENT_UI__=");
            sb.Append(JsonConvert.SerializeObject(new
            {
                renderId = renderId ?? "",
                title = pageTitle,
                layout = layout ?? "dashboard",
                data = JToken.Parse(dataJson),
                spec = JToken.Parse(specJson),
                actions = JToken.Parse(actions),
                license
            }));
            sb.Append(";</script>");
            sb.Append("<script>");
            sb.Append(PageJs);
            sb.Append("</script></body></html>");
            return sb.ToString();
        }

        private static string Enc(string s) => WebUtility.HtmlEncode(s ?? "");

        private const string PageCss = @"
body{margin:0;padding:12px 16px;font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
.hdr{display:flex;flex-wrap:wrap;align-items:center;gap:12px;margin-bottom:12px}
.hdr h1{margin:0;font-size:18px;font-weight:700}
.toolbar{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-left:auto}
.toolbar label{font-size:12px;color:#555}
.toolbar select, .toolbar button{font-size:12px;padding:4px 8px;border:1px solid #c5cad1;border-radius:4px;background:#fff}
.kpi-row{display:flex;flex-wrap:wrap;gap:12px;margin-bottom:16px}
.kpi-card{flex:1 1 140px;min-width:120px;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:14px 16px}
.kpi-label{font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:#6b7280;font-weight:600}
.kpi-value{font-size:22px;font-weight:700;margin-top:6px}
.charts{display:flex;flex-direction:column;gap:12px;margin-bottom:16px}
.chart-panel{background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:12px 16px}
.chart-panel h2{margin:0 0 8px;font-size:14px;font-weight:600}
.chart-host{width:100%;height:280px}
.grid-wrap{background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:8px;min-height:240px}
#gridHost{width:100%;height:420px}
.actions{margin-top:12px;display:flex;flex-wrap:wrap;gap:8px}
.actions button{padding:6px 12px;font-size:12px;border:1px solid #c5cad1;border-radius:4px;background:#fff;cursor:pointer}
.err{color:#b91c1c;font-size:13px;padding:12px}
";

        /// <summary>Vanilla JS bootstraps Wijmo FlexGrid + FlexChart from embedded __AGENT_UI__.</summary>
        private const string PageJs = @"
(function () {
  var cfg = window.__AGENT_UI__ || {};
  var rows = Array.isArray(cfg.data) ? cfg.data : [];
  var spec = cfg.spec || {};
  var kpis = Array.isArray(spec.kpis) ? spec.kpis : [];
  var chartTypes = ['bar','hbar','line','scatter','pie','donut','area'];
  var palette = ['#2563eb','#dc2626','#059669','#d97706','#7c3aed','#0891b2','#db2777','#65a30d'];

  function el(tag, cls, html) {
    var n = document.createElement(tag);
    if (cls) n.className = cls;
    if (html != null) n.innerHTML = html;
    return n;
  }

  if (typeof wijmo === 'undefined') {
    document.body.insertBefore(el('div','err','Wijmo CDN failed to load. Check network / license.'), document.body.firstChild);
    return;
  }
  // Re-apply (core already licensed before control scripts); keep same key as AppReact license.js
  try {
    var key = cfg.license || window.__WIJMO_LICENSE_KEY__ || '';
    if (key && wijmo.setLicenseKey) wijmo.setLicenseKey(key);
  } catch (e) {}

  var cv = new wijmo.collections.CollectionView(rows);
  cv.trackChanges = false;

  function distinct(field) {
    var s = {};
    cv.items.forEach(function (r) {
      var v = r && r[field];
      if (v != null && String(v).trim() !== '') s[String(v)] = 1;
    });
    return Object.keys(s).length;
  }
  function nonempty(field) {
    var n = 0;
    cv.items.forEach(function (r) {
      var v = r && r[field];
      if (v != null && String(v).trim() !== '') n++;
    });
    return n;
  }
  function kpiValue(k) {
    var op = String(k.op || k.Op || 'sum').toLowerCase();
    var field = k.field || k.Field || '';
    if (op === 'count' || op === 'rows') return String(cv.items.length);
    if (!field) return '—';
    if (op === 'distinct' || op === 'nunique' || op === 'unique') return String(distinct(field));
    if (op === 'nonempty' || op === 'filled') return String(nonempty(field));
    var nums = [];
    cv.items.forEach(function (r) {
      var t = r[field];
      if (t == null) return;
      var n = Number(t);
      if (!isNaN(n) && String(t).search(/[A-Za-z]/) < 0) nums.push(n);
    });
    if (!nums.length) return String(distinct(field));
    var v = 0;
    if (op === 'avg' || op === 'average' || op === 'mean') v = nums.reduce(function(a,b){return a+b;},0) / nums.length;
    else if (op === 'min') v = Math.min.apply(null, nums);
    else if (op === 'max') v = Math.max.apply(null, nums);
    else v = nums.reduce(function(a,b){return a+b;},0);
    return Math.abs(v) >= 1000 ? v.toLocaleString(undefined,{maximumFractionDigits:0}) : String(Math.round(v*100)/100);
  }

  function renderKpis() {
    var host = document.getElementById('kpiRow');
    host.innerHTML = '';
    (kpis || []).slice(0, 12).forEach(function (k) {
      var card = el('div', 'kpi-card');
      card.appendChild(el('div', 'kpi-label', (k.label || k.Label || k.field || k.Field || '')));
      card.appendChild(el('div', 'kpi-value', kpiValue(k)));
      host.appendChild(card);
    });
  }

  function seriesFor(xField) {
    var map = {};
    cv.items.forEach(function (r) {
      var x = r && r[xField];
      if (x == null || String(x).trim() === '') return;
      var key = String(x);
      map[key] = (map[key] || 0) + 1;
    });
    return Object.keys(map).map(function (k) { return { x: k, y: map[k] }; })
      .sort(function (a, b) { return b.y - a.y; }).slice(0, 40);
  }

  function chartFields() {
    var fields = [];
    var seen = {};
    (kpis || []).forEach(function (k) {
      var f = k.field || k.Field;
      var op = String(k.op || k.Op || '').toLowerCase();
      if (!f || seen[f] || op === 'count' || op === 'rows') return;
      var d = distinct(f);
      if (d >= 2 && d <= 40) { seen[f] = 1; fields.push({ field: f, label: k.label || k.Label || f }); }
    });
    if (spec.chart && (spec.chart.xField || spec.chart.XField)) {
      var xf = spec.chart.xField || spec.chart.XField;
      if (!seen[xf]) fields.unshift({ field: xf, label: spec.chart.title || spec.chart.Title || xf });
    }
    return fields.slice(0, 3);
  }

  var chartState = { type: (spec.chart && (spec.chart.type || spec.chart.Type)) || 'bar', dimFilter: '' };

  function mapWijmoType(t) {
    t = String(t || 'bar').toLowerCase();
    if (t === 'hbar' || t === 'bar_h') return wijmo.chart.ChartType.Bar;
    if (t === 'line') return wijmo.chart.ChartType.Line;
    if (t === 'scatter' || t === 'point') return wijmo.chart.ChartType.Scatter;
    if (t === 'area') return wijmo.chart.ChartType.Area;
    return wijmo.chart.ChartType.Column;
  }

  function renderCharts() {
    var host = document.getElementById('charts');
    host.innerHTML = '';
    var fields = chartFields();
    if (!fields.length) return;
    fields.forEach(function (cf, idx) {
      var panel = el('div', 'chart-panel');
      panel.appendChild(el('h2', null, (cf.label || cf.field) + ' (count)'));
      var typeBar = el('div', 'toolbar');
      chartTypes.forEach(function (t) {
        var b = el('button', null, t);
        if (t === chartState.type) b.style.fontWeight = '700';
        b.onclick = function () { chartState.type = t; renderCharts(); renderKpis(); };
        typeBar.appendChild(b);
      });
      panel.appendChild(typeBar);
      var chartDiv = el('div', 'chart-host');
      chartDiv.id = 'chartHost' + idx;
      panel.appendChild(chartDiv);
      host.appendChild(panel);

      var data = seriesFor(cf.field);
      if (chartState.dimFilter) {
        data = data.filter(function (d) { return d.x === chartState.dimFilter; });
      }
      var isPie = chartState.type === 'pie' || chartState.type === 'donut';
      if (isPie) {
        var pie = new wijmo.chart.FlexPie('#' + chartDiv.id);
        pie.bindingName = 'x';
        pie.binding = 'y';
        pie.itemsSource = data;
        pie.palette = palette;
        if (chartState.type === 'donut') pie.innerRadius = 0.5;
      } else {
        var chart = new wijmo.chart.FlexChart('#' + chartDiv.id);
        chart.bindingX = 'x';
        chart.chartType = mapWijmoType(chartState.type);
        chart.itemsSource = data;
        chart.palette = palette;
        // Single series uses one color by default — color each point/bar
        chart.itemFormatter = function (engine, ht, defaultRender) {
          if (ht && ht.pointIndex >= 0 && ht.chartElement === wijmo.chart.ChartElement.SeriesSymbol) {
            var c = palette[ht.pointIndex % palette.length];
            engine.fill = c;
            engine.stroke = c;
          }
          defaultRender();
        };
        var series = new wijmo.chart.Series();
        series.binding = 'y';
        series.name = 'count';
        chart.series.push(series);
      }
    });
  }

  function renderGrid() {
    var host = document.getElementById('gridHost');
    host.innerHTML = '';
    var grid = new wijmo.grid.FlexGrid(host, {
      itemsSource: cv,
      autoGenerateColumns: true,
      isReadOnly: true,
      selectionMode: 'Row'
    });
    new wijmo.grid.filter.FlexGridFilter(grid);
    grid.headersVisibility = wijmo.grid.HeadersVisibility.Column;
  }

  function renderToolbar() {
    var tb = document.getElementById('toolbar');
    tb.innerHTML = '';
    var fields = chartFields();
    if (!fields.length) return;
    var f0 = fields[0].field;
    var lab = el('label', null, 'Filter ' + f0 + ':');
    var sel = document.createElement('select');
    var optAll = document.createElement('option');
    optAll.value = '';
    optAll.text = '(all)';
    sel.appendChild(optAll);
    var vals = {};
    rows.forEach(function (r) {
      var v = r && r[f0];
      if (v != null && String(v).trim() !== '') vals[String(v)] = 1;
    });
    Object.keys(vals).sort().forEach(function (v) {
      var o = document.createElement('option');
      o.value = v; o.text = v;
      sel.appendChild(o);
    });
    sel.onchange = function () {
      chartState.dimFilter = sel.value || '';
      if (!sel.value) {
        cv.filter = null;
      } else {
        var field = f0;
        var want = sel.value;
        cv.filter = function (item) { return item && String(item[field]) === want; };
      }
      cv.refresh();
      renderKpis();
      renderCharts();
    };
    tb.appendChild(lab);
    tb.appendChild(sel);
  }

  function renderActions() {
    var host = document.getElementById('actions');
    host.innerHTML = '';
    var acts = Array.isArray(cfg.actions) ? cfg.actions : [];
    acts.forEach(function (a) {
      var id = a.id || a.Id;
      var label = a.label || a.Label || id;
      if (!id) return;
      var b = el('button', null, label);
      b.onclick = function () {
        try {
          parent.postMessage({ type: 'agent_ui_action', renderId: cfg.renderId || '', actionId: id, selection: {} }, '*');
        } catch (e) {}
      };
      host.appendChild(b);
    });
  }

  renderToolbar();
  renderKpis();
  renderCharts();
  renderGrid();
  renderActions();
  cv.collectionChanged.addHandler(function () { renderKpis(); });
})();
";
    }
}
