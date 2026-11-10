using App.BL.AppBuilderAgent.Plugins;

namespace APP.BL.Tests.AppBuilderAgent;

public class ExecuteSqlMultiStatementTests
{
    [Theory]
    [InlineData("SELECT 1", false)]
    [InlineData("SELECT 1;", false)]
    [InlineData("SELECT WarehouseId FROM Whse_Warehouse; SELECT CategoryId FROM Whse_Category;", true)]
    [InlineData("SELECT ';' AS x", false)]
    [InlineData("SELECT 1 -- ; SELECT 2", false)]
    public void TryGetMultiStatementError_DetectsBatches(string sql, bool expectError)
    {
        var hit = DataQueryPlugin.TryGetMultiStatementError(sql, out var error);
        Assert.Equal(expectError, hit);
        if (expectError)
            Assert.Contains("one SQL statement", error, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Null(error);
    }
}
