using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Builds MCP-like KPI / chart / dashboard HTML for the Result iframe (inline CSS, SVG charts).
    /// forExport: chart panels marked for PNG replacement.
    /// </summary>
    public static class UiDashboardHtml
    {
        public static string Build(
            string layout,
            JArray rows,
            UiRenderSpec spec,
            string title,
            bool interactive,
            bool forExport,
            string renderId,
            string actionsJson)
        {
            layout = (layout ?? "").ToLowerInvariant();
            spec = spec ?? new UiRenderSpec();
            rows = rows ?? new JArray();
            var pageTitle = !string.IsNullOrWhiteSpace(title) ? title : (spec.Title ?? layout);
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"/>");
            sb.Append("<title>").Append(Enc(pageTitle)).Append("</title>");
            sb.Append("<style>").Append(AgentUiCss.Content).Append("</style>");
            sb.Append("</head><body class=\"agent-ui\">");
            sb.Append("<header class=\"agent-ui-header\"><h1>").Append(Enc(pageTitle)).Append("</h1></header>");

            var chartIndex = 0;
            if (layout == "kpi")
            {
                AppendKpis(sb, rows, spec.Kpis);
                foreach (var chart in ResolveCharts(spec, rows, allowAuto: true))
                    AppendChart(sb, rows, chart, forExport, ref chartIndex);
            }
            else if (layout == "chart")
            {
                foreach (var chart in ResolveCharts(spec, rows, allowAuto: false))
                    AppendChart(sb, rows, chart, forExport, ref chartIndex);
                AppendMiniTable(sb, rows);
            }
            else // dashboard
            {
                if (spec.Blocks != null && spec.Blocks.Count > 0)
                {
                    var hadChart = false;
                    foreach (var block in spec.Blocks)
                    {
                        if (block.Type == "kpi")
                            AppendKpis(sb, rows, block.Kpis != null && block.Kpis.Count > 0 ? block.Kpis : spec.Kpis);
                        else if (block.Type == "chart" && block.Chart != null)
                        {
                            AppendChart(sb, rows, block.Chart, forExport, ref chartIndex);
                            hadChart = true;
                        }
                        else if (block.Type == "table")
                            AppendMiniTable(sb, rows, block.Limit, block.Title);
                    }
                    if (!hadChart)
                    {
                        foreach (var chart in ResolveCharts(spec, rows, allowAuto: true))
                            AppendChart(sb, rows, chart, forExport, ref chartIndex);
                    }
                }
                else
                {
                    if (spec.Kpis != null && spec.Kpis.Count > 0)
                        AppendKpis(sb, rows, spec.Kpis);
                    foreach (var chart in ResolveCharts(spec, rows, allowAuto: true))
                        AppendChart(sb, rows, chart, forExport, ref chartIndex);
                    AppendMiniTable(sb, rows);
                }
            }

            if (interactive && !string.IsNullOrWhiteSpace(actionsJson))
                AppendActions(sb, actionsJson, renderId);

            sb.Append("</body></html>");
            return sb.ToString();
        }

        private static void AppendKpis(StringBuilder sb, JArray rows, List<UiKpiSpec> kpis)
        {
            if (kpis == null || kpis.Count == 0) return;
            sb.Append("<section class=\"kpi-row\" data-role=\"kpi\">");
            foreach (var k in kpis.Take(UiRenderSpec.MaxKpis))
            {
                var value = ComputeKpi(rows, k.Field, k.Op);
                sb.Append("<div class=\"kpi-card\"><div class=\"kpi-label\">")
                    .Append(Enc(string.IsNullOrWhiteSpace(k.Label) ? k.Field : k.Label))
                    .Append("</div><div class=\"kpi-value\">")
                    .Append(Enc(value))
                    .Append("</div></div>");
            }
            sb.Append("</section>");
        }

        private static void AppendChart(StringBuilder sb, JArray rows, UiChartSpec chart, bool forExport, ref int chartIndex)
        {
            if (chart == null) return;
            chartIndex++;
            var id = "chart-" + chartIndex;
            var title = string.IsNullOrWhiteSpace(chart.Title) ? (chart.YField + " by " + chart.XField) : chart.Title;
            var points = ExtractSeries(rows, chart.XField, chart.YField);
            sb.Append("<section class=\"chart-panel\" data-role=\"chart\" data-chart-id=\"")
                .Append(id).Append("\">");
            sb.Append("<h2 class=\"chart-title\">").Append(Enc(title)).Append("</h2>");
            var svg = chart.Type == "line" ? ChartSvg.Line(points, 640, 280) : ChartSvg.Bar(points, 640, 280);
            if (forExport)
            {
                // Rasterizer replaces .chart-surface inner SVG with <img data-URI>
                sb.Append("<div class=\"chart-surface\" data-rasterize=\"1\">");
                sb.Append(svg);
                sb.Append("</div>");
            }
            else
            {
                sb.Append("<div class=\"chart-surface\">");
                sb.Append(svg);
                sb.Append("</div>");
            }
            sb.Append("</section>");
        }

        /// <summary>
        /// Template/program grid: show all loaded rows by default.
        /// Only truncate when <paramref name="limit"/> is explicitly set (e.g. block.Limit).
        /// Hard cap matches render MaxRows (50000).
        /// </summary>
        private static void AppendMiniTable(StringBuilder sb, JArray rows, int? limit = null, string title = null)
        {
            if (rows == null || rows.Count == 0) return;
            var cols = new List<string>();
            if (rows[0] is JObject first)
                foreach (var p in first.Properties()) cols.Add(p.Name);
            if (cols.Count == 0) return;
            sb.Append("<section class=\"data-table\" data-role=\"grid\">");
            if (!string.IsNullOrWhiteSpace(title))
                sb.Append("<h2>").Append(Enc(title)).Append("</h2>");
            sb.Append("<table><thead><tr>");
            foreach (var c in cols)
                sb.Append("<th>").Append(Enc(c)).Append("</th>");
            sb.Append("</tr></thead><tbody>");
            var n = rows.Count;
            if (limit.HasValue && limit.Value > 0)
                n = Math.Min(limit.Value, n);
            n = Math.Min(n, UiRenderSpec.MaxTableRows);
            for (var i = 0; i < n; i++)
            {
                if (rows[i] is not JObject row) continue;
                sb.Append("<tr>");
                foreach (var c in cols)
                {
                    var v = row[c];
                    sb.Append("<td>");
                    AppendTableCell(sb, v);
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></section>");
        }

        private static void AppendActions(StringBuilder sb, string actionsJson, string renderId)
        {
            try
            {
                var arr = JToken.Parse(actionsJson) as JArray;
                if (arr == null || arr.Count == 0) return;
                sb.Append("<div class=\"agent-ui-actions\" id=\"agent-ui-actions\">");
                foreach (var a in arr)
                {
                    var id = a["Id"]?.ToString() ?? a["id"]?.ToString() ?? "";
                    var label = a["Label"]?.ToString() ?? a["label"]?.ToString() ?? id;
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    sb.Append("<button type=\"button\" data-action=\"").Append(Enc(id))
                        .Append("\">").Append(Enc(label)).Append("</button>");
                }
                sb.Append("</div>");
                sb.Append("<script>(function(){var renderId=")
                    .Append(Newtonsoft.Json.JsonConvert.SerializeObject(renderId ?? ""))
                    .Append(";document.addEventListener('click',function(ev){var t=ev.target;if(!t||!t.getAttribute)return;var id=t.getAttribute('data-action');if(!id)return;try{parent.postMessage({type:'agent_ui_action',renderId:renderId,actionId:id,selection:{}},'*');}catch(e){}});})();</script>");
            }
            catch { /* ignore */ }
        }

        public static string ComputeKpi(JArray rows, string field, string op)
        {
            op = (op ?? "sum").Trim().ToLowerInvariant();
            if (rows == null) return "—";
            // count = row count (field optional)
            if (op is "count" or "rows")
                return rows.Count.ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(field)) return "—";

            if (op is "distinct" or "nunique" or "unique")
                return DistinctNonEmpty(rows, field).ToString(CultureInfo.InvariantCulture);

            if (op is "nonempty" or "filled")
                return NonEmptyCount(rows, field).ToString(CultureInfo.InvariantCulture);

            var nums = new List<double>();
            foreach (var row in rows.OfType<JObject>())
            {
                var t = row[field];
                if (t == null || t.Type == JTokenType.Null) continue;
                if (TryParseNumber(t.ToString(), out var d))
                    nums.Add(d);
            }
            // Categorical fields with sum/avg/min/max → distinct count (agent often passes dimension names as KPIs)
            if (nums.Count == 0)
                return DistinctNonEmpty(rows, field).ToString(CultureInfo.InvariantCulture);

            double v = op switch
            {
                "avg" or "average" or "mean" => nums.Average(),
                "min" => nums.Min(),
                "max" => nums.Max(),
                _ => nums.Sum()
            };
            if (Math.Abs(v) >= 1000)
                return v.ToString("N0", CultureInfo.InvariantCulture);
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Group by xField. Sum y when numeric; otherwise count rows per category
        /// (yField empty / "count" / non-numeric). Caps at 40 groups by value desc.
        /// </summary>
        public static List<(string Label, double Value)> ExtractSeries(JArray rows, string xField, string yField)
        {
            var list = new List<(string, double)>();
            if (rows == null || string.IsNullOrWhiteSpace(xField)) return list;

            var useCount = string.IsNullOrWhiteSpace(yField)
                           || yField.Equals("count", StringComparison.OrdinalIgnoreCase)
                           || yField.Equals("_count", StringComparison.OrdinalIgnoreCase);

            var sums = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var anyNumeric = false;

            foreach (var row in rows.OfType<JObject>())
            {
                var xTok = row[xField];
                if (xTok == null || xTok.Type == JTokenType.Null) continue;
                var x = xTok.ToString()?.Trim() ?? "";
                if (x.Length == 0) continue;

                counts[x] = counts.TryGetValue(x, out var c) ? c + 1 : 1;
                if (useCount) continue;

                var yTok = row[yField];
                if (yTok == null || yTok.Type == JTokenType.Null) continue;
                if (TryParseNumber(yTok.ToString(), out var y))
                {
                    anyNumeric = true;
                    sums[x] = sums.TryGetValue(x, out var s) ? s + y : y;
                }
            }

            IEnumerable<KeyValuePair<string, double>> ranked;
            if (useCount || !anyNumeric)
                ranked = counts.Select(kv => new KeyValuePair<string, double>(kv.Key, kv.Value));
            else
                ranked = sums;

            foreach (var kv in ranked.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Take(40))
                list.Add((kv.Key, kv.Value));
            return list;
        }

        /// <summary>
        /// Explicit chart(s) from spec, or auto count-by charts from KPI dimension fields
        /// when the model omitted chart (common after kpi_dashboard on raw detail rows).
        /// </summary>
        public static List<UiChartSpec> ResolveCharts(UiRenderSpec spec, JArray rows, bool allowAuto)
        {
            var list = new List<UiChartSpec>();
            if (spec?.Chart != null
                && !string.IsNullOrWhiteSpace(spec.Chart.XField))
            {
                list.Add(spec.Chart);
                return list;
            }

            if (!allowAuto || rows == null || rows.Count == 0 || spec?.Kpis == null)
                return list;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in spec.Kpis)
            {
                if (list.Count >= 3) break;
                var field = k?.Field?.Trim();
                if (string.IsNullOrEmpty(field) || !seen.Add(field)) continue;
                var op = (k.Op ?? "").Trim().ToLowerInvariant();
                // Skip pure row-count KPIs with no dimension
                if (op is "count" or "rows") continue;
                var distinct = DistinctNonEmpty(rows, field);
                if (distinct < 2 || distinct > 40) continue;
                // Skip high-cardinality ids (almost unique per row)
                if (distinct > rows.Count * 0.6) continue;
                list.Add(new UiChartSpec
                {
                    Type = "bar",
                    XField = field,
                    YField = "count",
                    Title = (string.IsNullOrWhiteSpace(k.Label) ? field : k.Label) + " (count)"
                });
            }
            return list;
        }

        private static int DistinctNonEmpty(JArray rows, string field)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.OfType<JObject>())
            {
                var t = row[field];
                if (t == null || t.Type == JTokenType.Null) continue;
                var s = t.ToString()?.Trim() ?? "";
                if (s.Length > 0) set.Add(s);
            }
            return set.Count;
        }

        private static int NonEmptyCount(JArray rows, string field)
        {
            var n = 0;
            foreach (var row in rows.OfType<JObject>())
            {
                var t = row[field];
                if (t == null || t.Type == JTokenType.Null) continue;
                if (!string.IsNullOrWhiteSpace(t.ToString())) n++;
            }
            return n;
        }

        private static bool TryParseNumber(string text, out double d)
        {
            d = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            // Avoid treating codes like "MC-PD-50" or pure ids with letters as numbers
            if (text.Any(c => char.IsLetter(c))) return false;
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d);
        }

        private static void AppendTableCell(StringBuilder sb, JToken v)
        {
            if (v == null || v.Type == JTokenType.Null)
                return;
            string text;
            var image = false;
            if (v is JObject o)
            {
                text = o["url"]?.ToString() ?? o["Url"]?.ToString() ?? o["fileId"]?.ToString() ?? o.ToString(Newtonsoft.Json.Formatting.None);
                image = o["url"] != null || o["Url"] != null || CardSheet.IsImageValue(text);
            }
            else
            {
                text = v.Type == JTokenType.String ? v.ToString() : v.ToString();
                image = CardSheet.IsImageValue(text);
            }
            if (image && !string.IsNullOrWhiteSpace(text))
            {
                sb.Append("<img src=\"").Append(Enc(text.Trim()))
                    .Append("\" alt=\"\" style=\"border:0;max-width:120px;max-height:80px;object-fit:contain;display:block\"/>");
                return;
            }
            sb.Append(Enc(text ?? ""));
        }

        private static string Enc(string s) => WebUtility.HtmlEncode(s ?? "");
    }

    public static class ChartSvg
    {
        public static string Bar(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0)
                return "<svg width=\"" + width + "\" height=\"" + height + "\" xmlns=\"http://www.w3.org/2000/svg\"><text x=\"20\" y=\"40\" fill=\"#666\">No data</text></svg>";
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 48;
            var padB = 36;
            var padT = 16;
            var padR = 16;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var barW = Math.Max(4, (plotW / (double)points.Count) * 0.7);
            var gap = (plotW / (double)points.Count) - barW;
            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
                .Append("\" height=\"").Append(height).Append("\" viewBox=\"0 0 ")
                .Append(width).Append(' ').Append(height).Append("\">");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");
            for (var i = 0; i < points.Count; i++)
            {
                var (label, value) = points[i];
                var h = plotH * (value / max);
                var x = padL + i * (barW + gap) + gap / 2;
                var y = padT + plotH - h;
                sb.Append("<rect class=\"bar\" x=\"").Append(F(x)).Append("\" y=\"").Append(F(y))
                    .Append("\" width=\"").Append(F(barW)).Append("\" height=\"").Append(F(h))
                    .Append("\" fill=\"#3d4f5f\"><title>")
                    .Append(WebUtility.HtmlEncode(label)).Append(": ")
                    .Append(value.ToString("0.##", CultureInfo.InvariantCulture))
                    .Append("</title></rect>");
                var lx = x + barW / 2;
                sb.Append("<text x=\"").Append(F(lx)).Append("\" y=\"").Append(height - 8)
                    .Append("\" text-anchor=\"middle\" font-size=\"10\" fill=\"#555\">")
                    .Append(WebUtility.HtmlEncode(Truncate(label, 8))).Append("</text>");
            }
            sb.Append("</svg>");
            return sb.ToString();
        }

        public static string Line(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0)
                return Bar(points, width, height);
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 48;
            var padB = 36;
            var padT = 16;
            var padR = 16;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
                .Append("\" height=\"").Append(height).Append("\" viewBox=\"0 0 ")
                .Append(width).Append(' ').Append(height).Append("\">");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");
            sb.Append("<polyline fill=\"none\" stroke=\"#3d4f5f\" stroke-width=\"2\" points=\"");
            for (var i = 0; i < points.Count; i++)
            {
                var x = padL + (points.Count == 1 ? plotW / 2 : i * (plotW / (double)(points.Count - 1)));
                var y = padT + plotH - plotH * (points[i].Value / max);
                if (i > 0) sb.Append(' ');
                sb.Append(F(x)).Append(',').Append(F(y));
            }
            sb.Append("\"/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= n) return s ?? "";
            return s.Substring(0, n - 1) + "…";
        }
    }

    /// <summary>Inline styles for agent Result iframe (no CDN).</summary>
    public static class AgentUiCss
    {
        public const string Content = @"
.agent-ui{margin:0;padding:16px;font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a;font-size:14px}
.agent-ui-header h1{margin:0 0 16px;font-size:18px;font-weight:700;color:#1a1a1a}
.kpi-row{display:flex;flex-wrap:wrap;gap:12px;margin-bottom:16px}
.kpi-card{flex:1 1 140px;min-width:120px;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:14px 16px;box-shadow:0 1px 2px rgba(0,0,0,.04)}
.kpi-label{font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:#6b7280;font-weight:600}
.kpi-value{font-size:22px;font-weight:700;margin-top:6px;color:#111827}
.chart-panel{background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:12px 16px;margin-bottom:16px}
.chart-title{margin:0 0 8px;font-size:14px;font-weight:600}
.chart-surface{width:100%;overflow:auto}
.chart-surface svg{max-width:100%;height:auto;display:block}
.data-table{background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:12px;overflow:auto}
.data-table table{width:100%;border-collapse:collapse;font-size:12px}
.data-table th{background:#3d4f5f;color:#fff;text-align:left;padding:6px 8px;font-weight:600}
.data-table td{border-bottom:1px solid #eee;padding:6px 8px;color:#1a1a1a}
.agent-ui-actions{margin-top:12px;display:flex;flex-wrap:wrap;gap:8px}
.agent-ui-actions button{padding:6px 12px;font-size:12px;border:1px solid #c5cad1;border-radius:4px;background:#fff;cursor:pointer}
.agent-ui-actions button:hover{background:#f0f2f4}
";
    }
}
