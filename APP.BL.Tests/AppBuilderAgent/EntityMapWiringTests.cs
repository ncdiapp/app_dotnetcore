using App.BL.AppBuilderAgent.Plugins;

namespace APP.BL.Tests.AppBuilderAgent;

public class EntityMapWiringTests
{
    private static List<EntityMapWiring.FieldRef> SampleFields() =>
    [
        new() { FieldId = 3122, ColumnName = "Status", TableName = "Whse_Warehouse", UnitName = "Whse Warehouse" },
        new() { FieldId = 3132, ColumnName = "Status", TableName = "Whse_StockItem", UnitName = "Whse Stock Item" },
        new() { FieldId = 3127, ColumnName = "Category", TableName = "Whse_StockItem", UnitName = "Whse Stock Item" },
        new() { FieldId = 3136, ColumnName = "MovementType", TableName = "Whse_StockMovement", UnitName = "Whse Stock Movement" }
    ];

    [Fact]
    public void ResolveTargets_BareColumn_MatchesAllUnits()
    {
        var hits = EntityMapWiring.ResolveTargets("Status", SampleFields());
        Assert.Equal(2, hits.Count);
        Assert.Contains(hits, f => f.FieldId == 3122);
        Assert.Contains(hits, f => f.FieldId == 3132);
    }

    [Fact]
    public void ResolveTargets_TableDotColumn_MatchesOne()
    {
        var hits = EntityMapWiring.ResolveTargets("Whse_Warehouse.Status", SampleFields());
        Assert.Single(hits);
        Assert.Equal(3122, hits[0].FieldId);
    }

    [Fact]
    public void ResolveTargets_UnitDotColumn_MatchesOne()
    {
        var hits = EntityMapWiring.ResolveTargets("Whse Stock Item.Status", SampleFields());
        Assert.Single(hits);
        Assert.Equal(3132, hits[0].FieldId);
    }

    [Fact]
    public void ResolveTargets_FieldIdKey_MatchesExact()
    {
        var hits = EntityMapWiring.ResolveTargets("field:3127", SampleFields());
        Assert.Single(hits);
        Assert.Equal("Category", hits[0].ColumnName);
    }

    [Fact]
    public void ResolveTargets_Unknown_ReturnsEmpty()
    {
        Assert.Empty(EntityMapWiring.ResolveTargets("NoSuchColumn", SampleFields()));
        Assert.Empty(EntityMapWiring.ResolveTargets("field:99999", SampleFields()));
        Assert.Empty(EntityMapWiring.ResolveTargets("OtherTable.Status", SampleFields()));
    }

    [Fact]
    public void ColumnNameMatches_AllowsIdSuffixAlias()
    {
        Assert.True(EntityMapWiring.ColumnNameMatches("MovementTypeId", "MovementType"));
        Assert.True(EntityMapWiring.ColumnNameMatches("Status", "StatusId"));
        Assert.True(EntityMapWiring.ColumnNameMatches("StatusId", "StatusId"));
        Assert.False(EntityMapWiring.ColumnNameMatches("Category", "Status"));
    }

    [Fact]
    public void UnitOrTableMatches_IgnoresSpacesAndUnderscores()
    {
        Assert.True(EntityMapWiring.UnitOrTableMatches("Wh Inv Stock Movement", "WhInv_StockMovement"));
        Assert.True(EntityMapWiring.UnitOrTableMatches("WhInv_StockMovement", "WhInv_StockMovement"));
        Assert.False(EntityMapWiring.UnitOrTableMatches("Wh Inv Warehouse", "WhInv_StockMovement"));
    }

    [Fact]
    public void ResolveTargets_BareName_MatchesIdSuffixColumn()
    {
        var fields = new List<EntityMapWiring.FieldRef>
        {
            new() { FieldId = 1, ColumnName = "MovementTypeId", TableName = "Ord_Order", UnitName = "Ord Order" }
        };
        var hits = EntityMapWiring.ResolveTargets("MovementType", fields);
        Assert.Single(hits);
        Assert.Equal(1, hits[0].FieldId);
    }

    [Fact]
    public void ResolveTargets_TableDotMiss_DoesNotFallThroughToBareColumn()
    {
        // "WrongTable.Status" must not wire Ord_Order.Status (full key is not a bare column name).
        var hits = EntityMapWiring.ResolveTargets("WrongTable.Status",
        [
            new() { FieldId = 1, ColumnName = "Status", TableName = "Ord_Order", UnitName = "Ord Order" }
        ]);
        Assert.Empty(hits);
    }

    [Fact]
    public void ResolveTargets_ExactColumn_StillPreferredOverIdAliasWhenBothExist()
    {
        var fields = new List<EntityMapWiring.FieldRef>
        {
            new() { FieldId = 1, ColumnName = "Status", TableName = "Ord_Order", UnitName = "Ord Order" },
            new() { FieldId = 2, ColumnName = "StatusId", TableName = "Ord_Order", UnitName = "Ord Order" }
        };
        var hits = EntityMapWiring.ResolveTargets("Status", fields);
        Assert.Equal(2, hits.Count); // exact Status + StatusId alias — agent should prefer Table.Column
        Assert.Contains(hits, f => f.FieldId == 1);
        Assert.Contains(hits, f => f.FieldId == 2);
    }
}
