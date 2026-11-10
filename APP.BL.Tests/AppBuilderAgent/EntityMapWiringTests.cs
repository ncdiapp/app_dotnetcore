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
}
