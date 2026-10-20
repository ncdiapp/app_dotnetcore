using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.AIAgent.GenericAgent.Plugins;

namespace APP.BL.Tests.Documents;

public class DataAnalyzeGateTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hi")]
    public void Rejects_missing_user_ask(string ask)
    {
        Assert.NotNull(AgentDataAnalyzePlugin.ValidateUserAsk(ask));
    }

    [Theory]
    [InlineData("export the card view to pdf with 2x2 layout")]
    [InlineData("render top 20 results, 3 cards per row")]
    [InlineData("execute style search and show cards")]
    [InlineData("top 20 styles as a grid")]
    public void Rejects_show_or_export_only(string ask)
    {
        var err = AgentDataAnalyzePlugin.ValidateUserAsk(ask);
        Assert.NotNull(err);
        Assert.Contains("render", err, System.StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("analyze margin by region")]
    [InlineData("sum Amount group by Season")]
    [InlineData("KPI dashboard of total sales")]
    [InlineData("chart average price by month")]
    [InlineData("how many styles per brand")]
    public void Allows_explicit_analysis_ask(string ask)
    {
        Assert.Null(AgentDataAnalyzePlugin.ValidateUserAsk(ask));
    }

    [Fact]
    public void Analysis_path_confirm_store()
    {
        Assert.True(AgentUiPathConfirmBL.LooksLikeKpiDashboardChoice(new[] { "kpi_dashboard" }, null));
        Assert.True(AgentUiPathConfirmBL.LooksLikeFetchDisplayChoice(new[] { "fetch_display" }, null));
        Assert.False(AgentUiPathConfirmBL.LooksLikeKpiDashboardChoice(new[] { "fetch_display" }, null));

        var key = "test-session-" + System.Guid.NewGuid().ToString("N");
        Assert.False(AgentUiPathConfirmBL.IsKpiDashboardConfirmed(key));
        AgentUiPathConfirmBL.MarkKpiDashboard(key);
        Assert.True(AgentUiPathConfirmBL.IsKpiDashboardConfirmed(key));
        AgentUiPathConfirmBL.MarkFetchDisplay(key);
        Assert.True(AgentUiPathConfirmBL.IsFetchDisplayOnly(key));
        Assert.False(AgentUiPathConfirmBL.IsKpiDashboardConfirmed(key));
        AgentUiPathConfirmBL.Clear(key);
    }
}
