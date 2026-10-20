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
                AppendResolvedCharts(sb, rows, spec, forExport, ref chartIndex, allowAuto: true);
                AppendMiniTable(sb, rows); // always show grid for template path
            }
            else if (layout == "chart")
            {
                AppendResolvedCharts(sb, rows, spec, forExport, ref chartIndex, allowAuto: false);
                AppendMiniTable(sb, rows);
            }
            else // dashboard
            {
                var paintedChart = false;
                var paintedTable = false;
                if (spec.Blocks != null && spec.Blocks.Count > 0)
                {
                    foreach (var block in spec.Blocks)
                    {
                        if (block.Type == "kpi")
                            AppendKpis(sb, rows, block.Kpis != null && block.Kpis.Count > 0 ? block.Kpis : spec.Kpis);
                        else if (block.Type == "chart" && block.Chart != null)
                        {
                            if (TryAppendChart(sb, rows, block.Chart, forExport, ref chartIndex))
                                paintedChart = true;
                        }
                        else if (block.Type == "table")
                        {
                            AppendMiniTable(sb, rows, block.Limit, block.Title);
                            paintedTable = true;
                        }
                    }
                    // Empty chart blocks (title "by", No data) must not block auto charts
                    if (!paintedChart)
                        paintedChart = AppendResolvedCharts(sb, rows, spec, forExport, ref chartIndex, allowAuto: true);
                }
                else
                {
                    if (spec.Kpis != null && spec.Kpis.Count > 0)
                        AppendKpis(sb, rows, spec.Kpis);
                    paintedChart = AppendResolvedCharts(sb, rows, spec, forExport, ref chartIndex, allowAuto: true);
                }
                if (!paintedTable)
                    AppendMiniTable(sb, rows);
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

        private static bool AppendResolvedCharts(
            StringBuilder sb, JArray rows, UiRenderSpec spec, bool forExport, ref int chartIndex, bool allowAuto)
        {
            var any = false;
            foreach (var chart in ResolveCharts(spec, rows, allowAuto))
            {
                if (TryAppendChart(sb, rows, chart, forExport, ref chartIndex))
                    any = true;
            }
            return any;
        }

        /// <summary>Returns false when chart has no usable series (skips empty "by" / No data widgets).</summary>
        public static bool TryAppendChart(StringBuilder sb, JArray rows, UiChartSpec chart, bool forExport, ref int chartIndex)
        {
            if (chart == null || sb == null) return false;
            var xField = ResolveColumn(rows, chart.XField);
            if (string.IsNullOrWhiteSpace(xField)) return false;
            var yField = string.IsNullOrWhiteSpace(chart.YField) || IsCountY(chart.YField)
                ? "count"
                : (ResolveColumn(rows, chart.YField) ?? chart.YField);
            var points = ExtractSeries(rows, xField, yField);
            if (points == null || points.Count == 0) return false;

            chartIndex++;
            var id = "chart-" + chartIndex;
            var title = chart.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title) || title.Equals("by", StringComparison.OrdinalIgnoreCase)
                || title.Equals("by ", StringComparison.OrdinalIgnoreCase))
            {
                title = IsCountY(yField)
                    ? (xField + " (count)")
                    : (yField + " by " + xField);
            }
            var chartType = ChartSvg.NormalizeType(chart.Type);

            sb.Append("<section class=\"chart-panel\" data-role=\"chart\" data-chart-id=\"")
                .Append(id).Append("\">");
            sb.Append("<h2 class=\"chart-title\">").Append(Enc(title)).Append("</h2>");
            var svg = ChartSvg.Render(chartType, points, 640, 280);
            if (forExport)
            {
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
            return true;
        }

        private static bool IsCountY(string yField) =>
            string.IsNullOrWhiteSpace(yField)
            || yField.Equals("count", StringComparison.OrdinalIgnoreCase)
            || yField.Equals("_count", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Template/program grid: show all loaded rows by default.
        /// Only truncate when <paramref name="limit"/> is explicitly set (e.g. block.Limit).
        /// Hard cap matches render MaxRows (50000).
        /// HTML keeps normal font (horizontal scroll OK). Page-fit fonts applied later for PDF/DOCX/PPTX via <see cref="BuildGridPageFitCss"/>.
        /// </summary>
        private static void AppendMiniTable(StringBuilder sb, JArray rows, int? limit = null, string title = null)
        {
            if (rows == null || rows.Count == 0) return;
            var cols = new List<string>();
            if (rows[0] is JObject first)
                foreach (var p in first.Properties()) cols.Add(p.Name);
            if (cols.Count == 0) return;
            sb.Append("<section class=\"data-table\" data-role=\"grid\" data-cols=\"")
                .Append(cols.Count.ToString(CultureInfo.InvariantCulture))
                .Append("\">");
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

        /// <summary>Font size so N columns fit ~A4 landscape / slide content width (~920–1000 CSS px).</summary>
        public static int FitTableFontPx(int columnCount)
        {
            if (columnCount <= 0) return 10;
            if (columnCount <= 5) return 10;
            if (columnCount <= 8) return 8;
            if (columnCount <= 12) return 7;
            if (columnCount <= 16) return 6;
            if (columnCount <= 22) return 5;
            return 4;
        }

        /// <summary>
        /// CSS that shrinks grid fonts by data-cols so all columns fit page/slide width.
        /// Used for PDF/DOCX/PPTX only — not for HTML export.
        /// </summary>
        public static string BuildGridPageFitCss(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            var seen = new HashSet<int>();
            foreach (System.Text.RegularExpressions.Match m in
                     System.Text.RegularExpressions.Regex.Matches(html, @"data-cols=""(\d+)"""))
            {
                if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
                    seen.Add(n);
            }
            if (seen.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var cols in seen.OrderBy(x => x))
            {
                var font = FitTableFontPx(cols);
                var pad = font <= 6 ? "1px 2px" : "2px 3px";
                var sel = ".data-table[data-cols=\"" + cols.ToString(CultureInfo.InvariantCulture) + "\"]";
                sb.Append(sel).Append(" table{width:100%!important;table-layout:fixed!important;border-collapse:collapse;font-size:")
                    .Append(font.ToString(CultureInfo.InvariantCulture)).Append("px!important}");
                sb.Append(sel).Append(" th,").Append(sel).Append(" td{padding:")
                    .Append(pad)
                    .Append("!important;word-break:break-word;overflow-wrap:anywhere;white-space:normal;vertical-align:top}");
            }
            return sb.ToString();
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

            xField = ResolveColumn(rows, xField) ?? xField;
            if (!useCount)
                yField = ResolveColumn(rows, yField) ?? yField;

            foreach (var row in rows.OfType<JObject>())
            {
                var xTok = GetProp(row, xField);
                if (xTok == null || xTok.Type == JTokenType.Null) continue;
                var x = xTok.ToString()?.Trim() ?? "";
                if (x.Length == 0) continue;

                counts[x] = counts.TryGetValue(x, out var c) ? c + 1 : 1;
                if (useCount) continue;

                var yTok = GetProp(row, yField);
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
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(UiChartSpec src)
            {
                if (src == null || list.Count >= UiRenderSpec.MaxCharts) return;
                var resolved = ResolveColumn(rows, src.XField);
                if (string.IsNullOrEmpty(resolved) || !seen.Add(resolved)) return;
                // LLM should set type; when omitted, infer from category shape
                var type = string.IsNullOrWhiteSpace(src.Type)
                    ? SuggestChartType(rows, resolved)
                    : ChartSvg.NormalizeType(src.Type);
                list.Add(new UiChartSpec
                {
                    Type = type,
                    XField = resolved,
                    YField = string.IsNullOrWhiteSpace(src.YField) ? "count" : src.YField,
                    Title = string.IsNullOrWhiteSpace(src.Title) ? resolved + " (count)" : src.Title
                });
            }

            if (spec?.Charts != null)
            {
                foreach (var c in spec.Charts)
                    TryAdd(c);
            }
            if (list.Count == 0 && spec?.Chart != null)
                TryAdd(spec.Chart);
            if (list.Count == 0 && spec?.Blocks != null)
            {
                foreach (var b in spec.Blocks)
                {
                    if (b?.Type == "chart" && b.Chart != null)
                        TryAdd(b.Chart);
                }
            }
            if (list.Count > 0)
                return list;

            if (!allowAuto || rows == null || rows.Count == 0 || spec?.Kpis == null)
                return list;

            foreach (var k in spec.Kpis)
            {
                if (list.Count >= 3) break;
                var field = ResolveColumn(rows, k?.Field);
                if (string.IsNullOrEmpty(field) || !seen.Add(field)) continue;
                var op = (k.Op ?? "").Trim().ToLowerInvariant();
                if (op is "count" or "rows") continue;
                var distinct = DistinctNonEmpty(rows, field);
                if (distinct < 2 || distinct > 40) continue;
                if (rows.Count >= 20 && distinct >= rows.Count * 0.8) continue;
                list.Add(new UiChartSpec
                {
                    Type = SuggestChartType(rows, field),
                    XField = field,
                    YField = "count",
                    Title = (string.IsNullOrWhiteSpace(k.Label) ? field : k.Label) + " (count)"
                });
            }
            return list;
        }

        /// <summary>
        /// Pick a default chart type from category count / label length / field name.
        /// LLM should set type explicitly when semantics are clear; this is fallback.
        /// </summary>
        public static string SuggestChartType(JArray rows, string xField)
        {
            xField = ResolveColumn(rows, xField) ?? xField;
            if (string.IsNullOrWhiteSpace(xField) || rows == null || rows.Count == 0)
                return "bar";

            var name = xField.ToLowerInvariant();
            if (name.Contains("date") || name.Contains("time") || name.Contains("period")
                || name.Contains("month") || name.Contains("year") || name.Contains("week")
                || name.Contains("day") || name.EndsWith("_dt") || name.Contains("season"))
                return "line";

            var labels = new List<string>();
            foreach (var row in rows.OfType<JObject>())
            {
                var t = GetProp(row, xField);
                if (t == null || t.Type == JTokenType.Null) continue;
                var s = t.ToString()?.Trim() ?? "";
                if (s.Length == 0) continue;
                if (!labels.Contains(s, StringComparer.OrdinalIgnoreCase))
                    labels.Add(s);
                if (labels.Count > 40) break;
            }
            var distinct = labels.Count;
            var avgLen = labels.Count == 0 ? 0 : labels.Average(l => l.Length);

            if (distinct <= 5 && avgLen <= 24)
                return distinct <= 4 ? "donut" : "pie";
            if (distinct >= 8 || avgLen >= 14)
                return "hbar";
            return "bar";
        }

        /// <summary>Case-insensitive column match against the first data row.</summary>
        public static string ResolveColumn(JArray rows, string field)
        {
            if (string.IsNullOrWhiteSpace(field) || rows == null) return null;
            field = field.Trim();
            var sample = rows.OfType<JObject>().FirstOrDefault();
            if (sample == null) return field;
            if (sample.Property(field) != null) return field;
            foreach (var p in sample.Properties())
            {
                if (string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase))
                    return p.Name;
            }
            // Soft match: strip spaces/underscores (Size Range ↔ Size_Range ↔ SizeRange)
            static string Norm(string s) =>
                new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            var want = Norm(field);
            if (want.Length == 0) return null;
            foreach (var p in sample.Properties())
            {
                if (Norm(p.Name) == want)
                    return p.Name;
            }
            return null;
        }

        private static JToken GetProp(JObject row, string field)
        {
            if (row == null || string.IsNullOrEmpty(field)) return null;
            var p = row.Property(field) ?? row.Properties()
                .FirstOrDefault(x => string.Equals(x.Name, field, StringComparison.OrdinalIgnoreCase));
            return p?.Value;
        }

        private static int DistinctNonEmpty(JArray rows, string field)
        {
            field = ResolveColumn(rows, field) ?? field;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.OfType<JObject>())
            {
                var t = GetProp(row, field);
                if (t == null || t.Type == JTokenType.Null) continue;
                var s = t.ToString()?.Trim() ?? "";
                if (s.Length > 0) set.Add(s);
            }
            return set.Count;
        }

        private static int NonEmptyCount(JArray rows, string field)
        {
            field = ResolveColumn(rows, field) ?? field;
            var n = 0;
            foreach (var row in rows.OfType<JObject>())
            {
                var t = GetProp(row, field);
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
        public static readonly string[] Palette =
        {
            "#2563eb", "#dc2626", "#059669", "#d97706", "#7c3aed",
            "#0891b2", "#db2777", "#65a30d", "#ea580c", "#4f46e5",
            "#0d9488", "#c026d3"
        };

        public static string NormalizeType(string type)
        {
            var t = (type ?? "bar").Trim().ToLowerInvariant();
            return t switch
            {
                "column" or "vbar" or "bar_v" => "bar",
                "hbar" or "bar_h" or "horizontal" or "horizontalbar" => "hbar",
                "point" or "dots" => "scatter",
                "doughnut" => "donut",
                "chart" or "kpi" or "" => "bar",
                _ => t
            };
        }

        public static string Render(string type, List<(string Label, double Value)> points, int width, int height)
        {
            type = NormalizeType(type);
            return type switch
            {
                "hbar" => HorizontalBar(points, width, height),
                "line" => Line(points, width, height),
                "scatter" => Scatter(points, width, height),
                "pie" => Pie(points, width, Math.Max(height, 320), donut: false),
                "donut" => Pie(points, width, Math.Max(height, 320), donut: true),
                _ => Bar(points, width, height)
            };
        }

        public static string ColorAt(int i) => Palette[i % Palette.Length];

        public static string Bar(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0) return Empty(width, height);
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 48;
            var padB = 36;
            var padT = 16;
            var padR = 16;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var barW = Math.Max(4, (plotW / (double)points.Count) * 0.7);
            var gap = (plotW / (double)points.Count) - barW;
            var sb = StartSvg(width, height);
            for (var i = 0; i < points.Count; i++)
            {
                var (label, value) = points[i];
                var h = plotH * (value / max);
                var x = padL + i * (barW + gap) + gap / 2;
                var y = padT + plotH - h;
                sb.Append("<rect class=\"bar\" x=\"").Append(F(x)).Append("\" y=\"").Append(F(y))
                    .Append("\" width=\"").Append(F(barW)).Append("\" height=\"").Append(F(h))
                    .Append("\" fill=\"").Append(ColorAt(i)).Append("\"><title>")
                    .Append(Tip(label, value)).Append("</title></rect>");
                sb.Append("<text x=\"").Append(F(x + barW / 2)).Append("\" y=\"").Append(height - 8)
                    .Append("\" text-anchor=\"middle\" font-size=\"8\" fill=\"#6b7280\">")
                    .Append(WebUtility.HtmlEncode(Truncate(label, 8))).Append("</text>");
            }
            return EndSvg(sb);
        }

        public static string HorizontalBar(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0) return Empty(width, height);
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 88;
            var padR = 24;
            var padT = 12;
            var padB = 12;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var rowH = Math.Max(10, plotH / (double)points.Count);
            var barH = rowH * 0.65;
            var sb = StartSvg(width, height);
            for (var i = 0; i < points.Count; i++)
            {
                var (label, value) = points[i];
                var w = plotW * (value / max);
                var y = padT + i * rowH + (rowH - barH) / 2;
                sb.Append("<text x=\"").Append(padL - 6).Append("\" y=\"").Append(F(y + barH * 0.75))
                    .Append("\" text-anchor=\"end\" font-size=\"8\" fill=\"#6b7280\">")
                    .Append(WebUtility.HtmlEncode(Truncate(label, 12))).Append("</text>");
                sb.Append("<rect x=\"").Append(padL).Append("\" y=\"").Append(F(y))
                    .Append("\" width=\"").Append(F(w)).Append("\" height=\"").Append(F(barH))
                    .Append("\" fill=\"").Append(ColorAt(i)).Append("\"><title>")
                    .Append(Tip(label, value)).Append("</title></rect>");
            }
            return EndSvg(sb);
        }

        public static string Line(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0) return Empty(width, height);
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 48;
            var padB = 36;
            var padT = 16;
            var padR = 16;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var sb = StartSvg(width, height);
            sb.Append("<polyline fill=\"none\" stroke=\"").Append(ColorAt(0))
                .Append("\" stroke-width=\"2.5\" points=\"");
            for (var i = 0; i < points.Count; i++)
            {
                var x = padL + (points.Count == 1 ? plotW / 2 : i * (plotW / (double)(points.Count - 1)));
                var y = padT + plotH - plotH * (points[i].Value / max);
                if (i > 0) sb.Append(' ');
                sb.Append(F(x)).Append(',').Append(F(y));
            }
            sb.Append("\"/>");
            for (var i = 0; i < points.Count; i++)
            {
                var x = padL + (points.Count == 1 ? plotW / 2 : i * (plotW / (double)(points.Count - 1)));
                var y = padT + plotH - plotH * (points[i].Value / max);
                sb.Append("<circle cx=\"").Append(F(x)).Append("\" cy=\"").Append(F(y))
                    .Append("\" r=\"4\" fill=\"").Append(ColorAt(i)).Append("\"><title>")
                    .Append(Tip(points[i].Label, points[i].Value)).Append("</title></circle>");
                sb.Append("<text x=\"").Append(F(x)).Append("\" y=\"").Append(height - 8)
                    .Append("\" text-anchor=\"middle\" font-size=\"8\" fill=\"#6b7280\">")
                    .Append(WebUtility.HtmlEncode(Truncate(points[i].Label, 8))).Append("</text>");
            }
            return EndSvg(sb);
        }

        public static string Scatter(List<(string Label, double Value)> points, int width, int height)
        {
            if (points == null || points.Count == 0) return Empty(width, height);
            var max = Math.Max(points.Max(p => p.Value), 1);
            var padL = 48;
            var padB = 36;
            var padT = 16;
            var padR = 16;
            var plotW = width - padL - padR;
            var plotH = height - padT - padB;
            var sb = StartSvg(width, height);
            for (var i = 0; i < points.Count; i++)
            {
                var x = padL + (points.Count == 1 ? plotW / 2 : i * (plotW / (double)(points.Count - 1)));
                var y = padT + plotH - plotH * (points[i].Value / max);
                sb.Append("<circle cx=\"").Append(F(x)).Append("\" cy=\"").Append(F(y))
                    .Append("\" r=\"6\" fill=\"").Append(ColorAt(i)).Append("\" opacity=\"0.9\"><title>")
                    .Append(Tip(points[i].Label, points[i].Value)).Append("</title></circle>");
                sb.Append("<text x=\"").Append(F(x)).Append("\" y=\"").Append(height - 8)
                    .Append("\" text-anchor=\"middle\" font-size=\"8\" fill=\"#6b7280\">")
                    .Append(WebUtility.HtmlEncode(Truncate(points[i].Label, 8))).Append("</text>");
            }
            return EndSvg(sb);
        }

        public static string Pie(List<(string Label, double Value)> points, int width, int height, bool donut)
        {
            if (points == null || points.Count == 0) return Empty(width, height);
            var total = points.Sum(p => Math.Max(p.Value, 0));
            if (total <= 0) return Empty(width, height);
            var cx = Math.Min(width, height) * 0.38;
            var cy = height / 2.0;
            var r = Math.Min(cx, cy) - 12;
            var inner = donut ? r * 0.55 : 0;
            var sb = StartSvg(width, height);
            double angle = -Math.PI / 2;
            for (var i = 0; i < points.Count; i++)
            {
                var slice = Math.Max(points[i].Value, 0) / total * Math.PI * 2;
                if (slice <= 0) continue;
                var a0 = angle;
                var a1 = angle + slice;
                angle = a1;
                var x0 = cx + r * Math.Cos(a0);
                var y0 = cy + r * Math.Sin(a0);
                var x1 = cx + r * Math.Cos(a1);
                var y1 = cy + r * Math.Sin(a1);
                var large = slice > Math.PI ? 1 : 0;
                if (donut)
                {
                    var ix0 = cx + inner * Math.Cos(a1);
                    var iy0 = cy + inner * Math.Sin(a1);
                    var ix1 = cx + inner * Math.Cos(a0);
                    var iy1 = cy + inner * Math.Sin(a0);
                    sb.Append("<path d=\"M ").Append(F(x0)).Append(' ').Append(F(y0))
                        .Append(" A ").Append(F(r)).Append(' ').Append(F(r)).Append(" 0 ").Append(large).Append(" 1 ")
                        .Append(F(x1)).Append(' ').Append(F(y1))
                        .Append(" L ").Append(F(ix0)).Append(' ').Append(F(iy0))
                        .Append(" A ").Append(F(inner)).Append(' ').Append(F(inner)).Append(" 0 ").Append(large).Append(" 0 ")
                        .Append(F(ix1)).Append(' ').Append(F(iy1))
                        .Append(" Z\" fill=\"").Append(ColorAt(i)).Append("\"><title>")
                        .Append(Tip(points[i].Label, points[i].Value)).Append("</title></path>");
                }
                else
                {
                    sb.Append("<path d=\"M ").Append(F(cx)).Append(' ').Append(F(cy))
                        .Append(" L ").Append(F(x0)).Append(' ').Append(F(y0))
                        .Append(" A ").Append(F(r)).Append(' ').Append(F(r)).Append(" 0 ").Append(large).Append(" 1 ")
                        .Append(F(x1)).Append(' ').Append(F(y1))
                        .Append(" Z\" fill=\"").Append(ColorAt(i)).Append("\"><title>")
                        .Append(Tip(points[i].Label, points[i].Value)).Append("</title></path>");
                }
            }
            var legendX = cx + r + 24;
            var legendY = 24.0;
            for (var i = 0; i < points.Count && i < 12; i++)
            {
                sb.Append("<rect x=\"").Append(F(legendX)).Append("\" y=\"").Append(F(legendY + i * 18))
                    .Append("\" width=\"10\" height=\"10\" fill=\"").Append(ColorAt(i)).Append("\"/>");
                sb.Append("<text x=\"").Append(F(legendX + 16)).Append("\" y=\"").Append(F(legendY + i * 18 + 9))
                    .Append("\" font-size=\"8\" fill=\"#6b7280\">")
                    .Append(WebUtility.HtmlEncode(Truncate(points[i].Label, 18))).Append("</text>");
            }
            return EndSvg(sb);
        }

        private static StringBuilder StartSvg(int width, int height)
        {
            var sb = new StringBuilder();
            sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(width)
                .Append("\" height=\"").Append(height).Append("\" viewBox=\"0 0 ")
                .Append(width).Append(' ').Append(height).Append("\">");
            sb.Append("<rect width=\"100%\" height=\"100%\" fill=\"#fff\"/>");
            return sb;
        }

        private static string EndSvg(StringBuilder sb)
        {
            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string Empty(int width, int height) =>
            "<svg width=\"" + width + "\" height=\"" + height + "\" xmlns=\"http://www.w3.org/2000/svg\"><text x=\"20\" y=\"40\" fill=\"#666\">No data</text></svg>";

        private static string Tip(string label, double value) =>
            WebUtility.HtmlEncode(label) + ": " + value.ToString("0.##", CultureInfo.InvariantCulture);

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
