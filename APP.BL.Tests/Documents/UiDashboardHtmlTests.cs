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
