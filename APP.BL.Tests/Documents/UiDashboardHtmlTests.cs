using System.Collections.Generic;
using System.Linq;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json.Linq;

namespace APP.BL.Tests.Documents;

public class UiDashboardHtmlTests
{
    private static JArray SampleRows() => JArray.Parse("""
        [
          { "Region": "East", "Amount": 100, "OrderId": "1" },
          { "Region": "West", "Amount": 250, "OrderId": "2" },
          { "Region": "East", "Amount": 50, "OrderId": "3" }
        ]
        """);

    [Fact]
    public void TryParse_and_validate_chart_spec()
    {
        Assert.True(UiRenderSpec.TryParse("""
            {"title":"Sales","chart":{"type":"bar","xField":"Region","yField":"Amount"}}
            """, out var spec, out var err));
        Assert.Null(err);
        Assert.Null(spec.ValidateForLayout("chart"));
        Assert.Equal("Region", spec.Chart.XField);
    }

    [Fact]
    public void Build_kpi_sums_field()
    {
        var spec = new UiRenderSpec
        {
            Title = "KPIs",
            Kpis = { new UiKpiSpec { Label = "Total", Field = "Amount", Op = "sum" } }
        };
        var html = UiDashboardHtml.Build("kpi", SampleRows(), spec, "KPIs", false, false, "r1", null);
        Assert.Contains("kpi-card", html);
        Assert.Contains("400", html); // 100+250+50
        Assert.DoesNotContain("data-rasterize", html);
    }

    [Fact]
    public void Build_chart_forExport_marks_rasterize()
    {
        var spec = new UiRenderSpec
        {
            Chart = new UiChartSpec { Type = "bar", XField = "Region", YField = "Amount", Title = "By region" }
        };
        var html = UiDashboardHtml.Build("chart", SampleRows(), spec, "Chart", false, forExport: true, "r1", null);
        Assert.Contains("data-rasterize", html);
        Assert.Contains("<svg", html);
        Assert.Contains("By region", html);
    }

    [Fact]
    public void FitTableFontPx_shrinks_for_wide_grids()
    {
        Assert.Equal(10, UiDashboardHtml.FitTableFontPx(4));
        Assert.Equal(8, UiDashboardHtml.FitTableFontPx(8));
        Assert.Equal(6, UiDashboardHtml.FitTableFontPx(16));
        Assert.Equal(5, UiDashboardHtml.FitTableFontPx(20));
        Assert.Equal(4, UiDashboardHtml.FitTableFontPx(30));
    }

    [Fact]
    public void Build_html_keeps_normal_grid_font_data_cols_only()
    {
        var cols = string.Join(",", Enumerable.Range(1, 18).Select(i => $"\"C{i}\":\"v{i}\""));
        var rows = JArray.Parse("[{" + cols + "}]");
        var html = UiDashboardHtml.Build("kpi", rows, new UiRenderSpec(), "Wide", false, forExport: true, null, null);
        Assert.Contains("data-cols=\"18\"", html);
        Assert.DoesNotContain("font-size:5px", html);
        Assert.DoesNotContain("data-font-px", html);
    }

    [Fact]
    public void ForDashboardPrint_injects_page_fit_grid_font()
    {
        var cols = string.Join(",", Enumerable.Range(1, 18).Select(i => $"\"C{i}\":\"v{i}\""));
        var rows = JArray.Parse("[{" + cols + "}]");
        var html = UiDashboardHtml.Build("kpi", rows, new UiRenderSpec(), "Wide", false, forExport: true, null, null);
        var print = HtmlForm.ForDashboardPrint(html);
        Assert.Contains("data-cols=\"18\"", print);
        Assert.Contains("font-size:5px!important", print);
        Assert.Contains("table-layout:fixed!important", print);
    }

    [Fact]
    public void BuildGridPageFitCss_by_column_count()
    {
        var css = UiDashboardHtml.BuildGridPageFitCss("<section class=\"data-table\" data-cols=\"18\">");
        Assert.Contains("[data-cols=\"18\"]", css);
        Assert.Contains("font-size:5px", css);
    }

    [Fact]
    public void ExtractChartPngs_reads_data_uri()
    {
        var html = """
            <section class="chart-panel"><h2 class="chart-title">Sales</h2>
            <div class="chart-surface"><img src="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="/></div>
            </section>
            """;
        var list = DashboardOffice.ExtractChartPngs(html);
        Assert.Single(list);
        Assert.Equal("Sales", list[0].Title);
        Assert.True(list[0].Png.Length > 10);
    }

    [Fact]
    public void ChartSvg_palette_and_types()
    {
        var pts = new List<(string, double)> { ("A", 10), ("B", 20), ("C", 5) };
        var bar = ChartSvg.Render("bar", pts, 400, 200);
        Assert.Contains("#2563eb", bar);
        Assert.Contains("#dc2626", bar);
        Assert.Contains("<path", ChartSvg.Render("pie", pts, 400, 280));
        Assert.Contains("<path", ChartSvg.Render("donut", pts, 400, 280));
        Assert.Contains("<circle", ChartSvg.Render("scatter", pts, 400, 200));
        Assert.Contains("text-anchor=\"end\"", ChartSvg.Render("hbar", pts, 400, 200));
        Assert.Equal("hbar", ChartSvg.NormalizeType("bar_h"));
    }

    [Fact]
    public void SuggestChartType_by_cardinality_and_labels()
    {
        var few = JArray.Parse("""[{"C":"A"},{"C":"B"},{"C":"A"},{"C":"C"}]""");
        Assert.Equal("donut", UiDashboardHtml.SuggestChartType(few, "C"));

        var longLabels = new JArray(
            Enumerable.Range(0, 10).Select(i => new JObject
            {
                ["Size"] = "VeryLongSizeRangeLabel_" + i + "_XXXXXXXX"
            }));
        Assert.Equal("hbar", UiDashboardHtml.SuggestChartType(longLabels, "Size"));

        var seasonal = JArray.Parse("""[{"Season":"SS24"},{"Season":"FW24"},{"Season":"SS25"}]""");
        Assert.Equal("line", UiDashboardHtml.SuggestChartType(seasonal, "Season"));
    }

    [Fact]
    public void InteractiveDashboardHtml_embeds_wijmo_cdn_and_data()
    {
        var rows = SampleRows();
        var spec = new UiRenderSpec
        {
            Title = "Sales",
            Kpis = { new UiKpiSpec { Label = "Total", Field = "", Op = "count" } },
            Chart = new UiChartSpec { Type = "bar", XField = "Region", YField = "count" }
        };
        var html = InteractiveDashboardHtml.Build("Sales", "dashboard", rows, spec, "[]", "rid1");
        Assert.Contains("cdn.mescius.com/wijmo/", html);
        Assert.Contains("wijmo.chart.min.js", html);
        Assert.Contains("window.__AGENT_UI__", html);
        Assert.Contains("Region", html);
        Assert.Contains("gridHost", html);
        Assert.Contains("GrapeCity,", html); // Wijmo license embedded (same as AppReact license.js)
        Assert.Contains("wijmo.min.js", html);
        // License script must appear before control modules (grid/chart)
        var coreAt = html.IndexOf("wijmo.min.js", StringComparison.Ordinal);
        var licenseAt = html.IndexOf("setLicenseKey", StringComparison.Ordinal);
        var gridAt = html.IndexOf("wijmo.grid.min.js", StringComparison.Ordinal);
        Assert.True(coreAt >= 0 && licenseAt > coreAt && gridAt > licenseAt);
        Assert.Contains("itemFormatter", html); // per-bar colors
    }

    [Fact]
    public void Build_dashboard_blocks()
    {
        var spec = new UiRenderSpec
        {
            Title = "Overview",
            Blocks =
            {
                new UiDashBlockSpec
                {
                    Type = "kpi",
                    Kpis = { new UiKpiSpec { Label = "Orders", Field = "OrderId", Op = "count" } }
                },
                new UiDashBlockSpec
                {
                    Type = "chart",
                    Chart = new UiChartSpec { Type = "line", XField = "Region", YField = "Amount" }
                },
                new UiDashBlockSpec { Type = "table", Limit = 2, Title = "Top" }
            }
        };
        var html = UiDashboardHtml.Build("dashboard", SampleRows(), spec, null, false, false, null, null);
        Assert.Contains("kpi-row", html);
        Assert.Contains("chart-panel", html);
        Assert.Contains("data-table", html);
        Assert.Contains(">Top<", html);
        Assert.Contains("polyline", html); // line chart
    }

    [Fact]
    public void Build_dashboard_grid_shows_all_rows_by_default()
    {
        var rows = new JArray(Enumerable.Range(1, 25).Select(i =>
            new JObject { ["Id"] = i, ["Name"] = "r" + i }));
        var spec = new UiRenderSpec
        {
            Kpis = { new UiKpiSpec { Label = "Total", Field = "", Op = "count" } }
        };
        var html = UiDashboardHtml.Build("dashboard", rows, spec, "All", false, false, null, null);
        Assert.Equal(25, System.Text.RegularExpressions.Regex.Matches(html, "<tr>").Count - 1); // minus header
        Assert.Contains(">r25<", html);
        Assert.Contains(">r1<", html);
    }

    [Fact]
    public void Build_skips_empty_chart_blocks_and_still_draws_grid()
    {
        var rows = JArray.Parse("""
            [
              { "Classification": "MC", "Size": "S" },
              { "Classification": "MC", "Size": "M" },
              { "Classification": "Army", "Size": "S" }
            ]
            """);
        var spec = new UiRenderSpec
        {
            Title = "Style Search Analysis",
            Kpis =
            {
                new UiKpiSpec { Label = "Total Styles", Field = "", Op = "count" },
                new UiKpiSpec { Label = "UNIQUE CLASSIFICATIONS", Field = "Classification", Op = "distinct" }
            },
            Blocks =
            {
                new UiDashBlockSpec { Type = "kpi" },
                new UiDashBlockSpec { Type = "chart", Chart = new UiChartSpec { Type = "bar", Title = "by" } },
                new UiDashBlockSpec { Type = "chart", Chart = new UiChartSpec { Type = "chart" } }
            }
        };
        var html = UiDashboardHtml.Build("dashboard", rows, spec, null, false, false, null, null);
        Assert.DoesNotContain(">by<", html);
        Assert.DoesNotContain("No data", html);
        Assert.Contains("chart-panel", html);
        Assert.Contains("UNIQUE CLASSIFICATIONS (count)", html);
        Assert.Contains("data-table", html);
        Assert.Contains(">Army<", html);
    }

    [Fact]
    public void Build_kpi_layout_includes_grid()
    {
        var rows = SampleRows();
        var spec = new UiRenderSpec
        {
            Kpis = { new UiKpiSpec { Label = "Total", Field = "", Op = "count" } }
        };
        var html = UiDashboardHtml.Build("kpi", rows, spec, "KPIs", false, false, null, null);
        Assert.Contains("data-table", html);
        Assert.Contains(">East<", html);
    }

    [Fact]
    public void MapLegacyUi_chart_and_kpi_dashboard()
    {
        Assert.Equal("chart", UiTemplateMerge.MapLegacyUi("chart", out var from1));
        Assert.Equal("chart", from1);
        Assert.Equal("dashboard", UiTemplateMerge.MapLegacyUi("kpi_dashboard", out var from2));
        Assert.Equal("kpi_dashboard", from2);
        Assert.Equal("kpi", UiTemplateMerge.MapLegacyUi("kpi", out _));
        Assert.True(UiTemplateMerge.IsDashboardLayout("chart"));
        Assert.False(UiTemplateMerge.IsDashboardLayout("table"));
    }

    [Fact]
    public void ComputeKpi_avg_and_count()
    {
        var rows = SampleRows();
        Assert.Equal("3", UiDashboardHtml.ComputeKpi(rows, "OrderId", "count"));
        Assert.Equal("3", UiDashboardHtml.ComputeKpi(rows, null, "count"));
        Assert.Equal("133.33", UiDashboardHtml.ComputeKpi(rows, "Amount", "avg"));
        Assert.Equal("2", UiDashboardHtml.ComputeKpi(rows, "Region", "distinct"));
        // Categorical + default sum → distinct fallback
        Assert.Equal("2", UiDashboardHtml.ComputeKpi(rows, "Region", "sum"));
    }

    [Fact]
    public void ExtractSeries_groups_and_counts()
    {
        var rows = SampleRows();
        var bySum = UiDashboardHtml.ExtractSeries(rows, "Region", "Amount");
        Assert.Equal(2, bySum.Count);
        Assert.Equal(("East", 150), bySum.First(p => p.Label == "East"));
        Assert.Equal(("West", 250), bySum.First(p => p.Label == "West"));

        var byCount = UiDashboardHtml.ExtractSeries(rows, "Region", "count");
        Assert.Equal(2, byCount.First(p => p.Label == "East").Value);
        Assert.Equal(1, byCount.First(p => p.Label == "West").Value);
    }

    [Fact]
    public void ExtractSeries_caps_at_40()
    {
        var rows = new JArray(Enumerable.Range(0, 50).Select(i =>
            new JObject { ["X"] = "r" + i, ["Y"] = i }));
        var series = UiDashboardHtml.ExtractSeries(rows, "X", "Y");
        Assert.Equal(40, series.Count);
    }

    [Fact]
    public void Build_dashboard_auto_charts_from_categorical_kpis()
    {
        var styleRows = JArray.Parse("""
            [
              { "ReferenceId": 1, "Classification": "MC", "Size": "S" },
              { "ReferenceId": 2, "Classification": "MC", "Size": "M" },
              { "ReferenceId": 3, "Classification": "Army", "Size": "S" },
              { "ReferenceId": 4, "Classification": "MC", "Size": "L" }
            ]
            """);
        var spec = new UiRenderSpec
        {
            Title = "Style Search Analysis",
            Kpis =
            {
                new UiKpiSpec { Label = "Total", Field = "", Op = "count" },
                new UiKpiSpec { Label = "CLASSIFICATION", Field = "Classification", Op = "sum" },
                new UiKpiSpec { Label = "SIZE", Field = "Size", Op = "sum" }
            }
        };
        var html = UiDashboardHtml.Build("dashboard", styleRows, spec, null, false, false, null, null);
        Assert.Contains("kpi-value", html);
        Assert.Contains(">4<", html); // total count
        Assert.Contains(">2<", html); // distinct Classification / Size
        Assert.Contains("chart-panel", html);
        Assert.Contains("CLASSIFICATION (count)", html);
        Assert.Contains("<svg", html);
    }
}
