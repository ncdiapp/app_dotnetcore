using App.BL;

namespace APP.BL.Tests.AppBuilderAgent;

public class ParentLinkFieldTests
{
    [Fact]
    public void GetParentLinkColumnCandidates_PrefersEntityId_OverBareId()
    {
        var cols = AppTransactionBL.GetParentLinkColumnCandidates("Whse_StockMovement", "Id").ToList();
        Assert.Equal("StockMovementId", cols[0]);
        Assert.Contains("Whse_StockMovementId", cols);
        Assert.Contains("Id", cols);
        Assert.True(cols.IndexOf("StockMovementId") < cols.IndexOf("Id"));
    }

    [Fact]
    public void GetParentLinkColumnCandidates_KeepsExplicitParentPkName()
    {
        var cols = AppTransactionBL.GetParentLinkColumnCandidates("Ord_Order", "OrderId").ToList();
        Assert.Contains("OrderId", cols);
        Assert.Contains("Ord_OrderId", cols);
    }

    [Fact]
    public void ResolveLogicalParentLinkAlias_StyleSpecRoundTrip()
    {
        Assert.Equal("StyleSpecId", AppTransactionBL.ResolveLogicalParentLinkAlias("ReferenceId"));
        Assert.Equal("ReferenceId", AppTransactionBL.ResolveLogicalParentLinkAlias("StyleSpecId"));
        Assert.Null(AppTransactionBL.ResolveLogicalParentLinkAlias("OrderId"));
    }
}
