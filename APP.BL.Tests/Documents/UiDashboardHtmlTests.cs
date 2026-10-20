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
        Assert.Equal("133.33", UiDashboardHtml.ComputeKpi(rows, "Amount", "avg"));
    }

    [Fact]
    public void ExtractSeries_caps_at_40()
    {
        var rows = new JArray(Enumerable.Range(0, 50).Select(i =>
            new JObject { ["X"] = "r" + i, ["Y"] = i }));
        var series = UiDashboardHtml.ExtractSeries(rows, "X", "Y");
        Assert.Equal(40, series.Count);
    }
}
