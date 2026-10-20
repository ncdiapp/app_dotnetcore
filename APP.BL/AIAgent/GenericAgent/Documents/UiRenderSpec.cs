using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Small chart/KPI/dashboard spec from the model or data_analyze (not full HTML).
    /// </summary>
    public sealed class UiRenderSpec
    {
        public string Title { get; set; }
        public List<UiKpiSpec> Kpis { get; set; } = new List<UiKpiSpec>();
        public UiChartSpec Chart { get; set; }
        public List<UiDashBlockSpec> Blocks { get; set; } = new List<UiDashBlockSpec>();

        public const int MaxKpis = 12;
        public const int MaxCharts = 9;
        public const int MaxTables = 3;

        public static bool TryParse(string specJson, out UiRenderSpec spec, out string error)
        {
            spec = null;
            error = null;
            if (string.IsNullOrWhiteSpace(specJson))
            {
                error = "specJson is required for layout kpi, chart, or dashboard.";
                return false;
            }
            try
            {
                var obj = JObject.Parse(specJson);
                spec = new UiRenderSpec
                {
                    Title = obj["title"]?.ToString() ?? obj["Title"]?.ToString()
                };
                if (obj["kpis"] is JArray kpis || obj["Kpis"] is JArray)
                {
                    var arr = (obj["kpis"] ?? obj["Kpis"]) as JArray;
                    foreach (var k in arr.OfType<JObject>())
                    {
                        spec.Kpis.Add(new UiKpiSpec
                        {
                            Label = k["label"]?.ToString() ?? k["Label"]?.ToString() ?? "",
                            Field = k["field"]?.ToString() ?? k["Field"]?.ToString() ?? "",
                            Op = (k["op"]?.ToString() ?? k["Op"]?.ToString() ?? "sum").ToLowerInvariant()
                        });
                    }
                }
                var chartTok = obj["chart"] ?? obj["Chart"];
                if (chartTok is JObject co)
                    spec.Chart = ParseChart(co);
                if (obj["blocks"] is JArray blocks || obj["Blocks"] is JArray)
                {
                    var arr = (obj["blocks"] ?? obj["Blocks"]) as JArray;
                    foreach (var b in arr.OfType<JObject>())
                        spec.Blocks.Add(ParseBlock(b));
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "specJson is not valid JSON: " + ex.Message;
                return false;
            }
        }

        public string ValidateForLayout(string layout)
        {
            layout = (layout ?? "").ToLowerInvariant();
            if (layout == "kpi")
            {
                if (Kpis == null || Kpis.Count == 0)
                    return "layout=kpi requires specJson.kpis with at least one {label,field,op}.";
                if (Kpis.Count > MaxKpis)
                    return "layout=kpi allows at most " + MaxKpis + " KPIs.";
                foreach (var k in Kpis)
                    if (string.IsNullOrWhiteSpace(k.Field))
                        return "Each KPI needs a field.";
                return null;
            }
            if (layout == "chart")
            {
                if (Chart == null || string.IsNullOrWhiteSpace(Chart.XField) || string.IsNullOrWhiteSpace(Chart.YField))
                    return "layout=chart requires specJson.chart with xField and yField.";
                return null;
            }
            if (layout == "dashboard")
            {
                if (Blocks == null || Blocks.Count == 0)
                {
                    // Allow shorthand: kpis + chart without blocks
                    if ((Kpis != null && Kpis.Count > 0) || Chart != null)
                        return null;
                    return "layout=dashboard requires specJson.blocks, or kpis and/or chart.";
                }
                var charts = Blocks.Count(b => b.Type == "chart");
                var tables = Blocks.Count(b => b.Type == "table");
                var kpis = Blocks.Where(b => b.Type == "kpi").SelectMany(b => b.Kpis ?? new List<UiKpiSpec>()).Count();
                if (Kpis != null) kpis += Kpis.Count;
                if (charts > MaxCharts) return "dashboard allows at most " + MaxCharts + " chart blocks.";
                if (tables > MaxTables) return "dashboard allows at most " + MaxTables + " table blocks.";
                if (kpis > MaxKpis) return "dashboard allows at most " + MaxKpis + " KPI items.";
                return null;
            }
            return null;
        }

        private static UiChartSpec ParseChart(JObject co) => new UiChartSpec
        {
            Type = (co["type"]?.ToString() ?? co["chartType"]?.ToString() ?? co["Type"]?.ToString() ?? "bar").ToLowerInvariant(),
            XField = co["xField"]?.ToString() ?? co["XField"]?.ToString() ?? "",
            YField = co["yField"]?.ToString() ?? co["YField"]?.ToString() ?? "",
            Title = co["title"]?.ToString() ?? co["Title"]?.ToString()
        };

        private static UiDashBlockSpec ParseBlock(JObject b)
        {
            var type = (b["type"]?.ToString() ?? b["Type"]?.ToString() ?? "").ToLowerInvariant();
            var block = new UiDashBlockSpec
            {
                Type = type,
                Limit = b["limit"]?.Value<int?>() ?? b["Limit"]?.Value<int?>(),
                Title = b["title"]?.ToString() ?? b["Title"]?.ToString()
            };
            if (type == "chart")
                block.Chart = ParseChart(b);
            if (type == "kpi" && b["kpis"] is JArray ka)
            {
                block.Kpis = new List<UiKpiSpec>();
                foreach (var k in ka.OfType<JObject>())
                {
                    block.Kpis.Add(new UiKpiSpec
                    {
                        Label = k["label"]?.ToString() ?? k["Label"]?.ToString() ?? "",
                        Field = k["field"]?.ToString() ?? k["Field"]?.ToString() ?? "",
                        Op = (k["op"]?.ToString() ?? k["Op"]?.ToString() ?? "sum").ToLowerInvariant()
                    });
                }
            }
            return block;
        }
    }

    public sealed class UiKpiSpec
    {
        public string Label { get; set; }
        public string Field { get; set; }
        /// <summary>sum | avg | count | min | max — applied over the shaped dataset rows.</summary>
        public string Op { get; set; } = "sum";
    }

    public sealed class UiChartSpec
    {
        public string Type { get; set; } = "bar";
        public string XField { get; set; }
        public string YField { get; set; }
        public string Title { get; set; }
    }

    public sealed class UiDashBlockSpec
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int? Limit { get; set; }
        public UiChartSpec Chart { get; set; }
        public List<UiKpiSpec> Kpis { get; set; } = new List<UiKpiSpec>();
    }
}
