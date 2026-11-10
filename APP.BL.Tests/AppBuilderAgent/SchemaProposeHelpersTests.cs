using App.BL.AppBuilderAgent.Plugins;
using APP.Components.EntityDto;

namespace APP.BL.Tests.AppBuilderAgent;

public class SchemaProposeHelpersTests
{
    [Fact]
    public void PrefixedTableName_IsIdempotent_WhenAlreadyPrefixed()
    {
        var prefix = SchemaProposeHelpers.NormalizeAppPrefix("Warehouse Management");
        Assert.Equal("WarehouseM_", prefix);

        Assert.Equal("WarehouseM_Warehouse", SchemaProposeHelpers.PrefixedTableName("Warehouse", prefix));
        Assert.Equal("WarehouseM_Warehouse", SchemaProposeHelpers.PrefixedTableName("WarehouseM_Warehouse", prefix));
        Assert.Equal("Whse_Warehouse", SchemaProposeHelpers.PrefixedTableName("Whse_Warehouse", "Whse_"));
    }

    [Fact]
    public void ApplyAppNamePrefix_DoesNotDoublePrefix()
    {
        var tables = new List<DbGenieTableMetadataDto>
        {
            new() { Name = "Whse_Warehouse" },
            new() { Name = "Category" },
            new()
            {
                Name = "StockMovement",
                Relationships =
                [
                    new DbGenieRelationshipMetadataDto
                    {
                        Type = "MANY_TO_ONE",
                        TargetTable = "Whse_Warehouse",
                        ForeignKeyColumn = "WarehouseId"
                    }
                ]
            }
        };

        SchemaProposeHelpers.ApplyAppNamePrefix(tables, "Whse");

        Assert.Equal("Whse_Warehouse", tables[0].Name);
        Assert.Equal("Whse_Category", tables[1].Name);
        Assert.Equal("Whse_StockMovement", tables[2].Name);
        Assert.Equal("Whse_Warehouse", tables[2].Relationships[0].TargetTable);
    }

    [Fact]
    public void PrefixedTableName_StripsMismatchedShortPrefix()
    {
        var prefix = SchemaProposeHelpers.NormalizeAppPrefix("Warehouse Management");
        Assert.Equal("WarehouseM_Warehouse", SchemaProposeHelpers.PrefixedTableName("Whse_Warehouse", prefix));
        Assert.Equal("WarehouseM_StockItem", SchemaProposeHelpers.PrefixedTableName("Abc_StockItem", prefix));
    }

    [Fact]
    public void ValidateSchemaForDdl_DetectsMissingFkColumn()
    {
        var parent = Table("Order", pk: "OrderId");
        var child = Table("OrderItem", pk: "OrderItemId",
            cols: ["OrderItemId", "Qty"],
            rels: [Rel("MANY_TO_ONE", "Order", "OrderId")]); // OrderId missing on child

        var errors = SchemaProposeHelpers.ValidateSchemaForDdl([parent, child]);
        Assert.Contains(errors, e => e.Contains("OrderId") && e.Contains("OrderItem"));
    }

    [Fact]
    public void ValidateSchemaForDdl_PassesWhenFkAndPkExist()
    {
        var parent = Table("Order", pk: "OrderId");
        var child = Table("OrderItem", pk: "OrderItemId",
            cols: ["OrderItemId", "OrderId"],
            rels: [Rel("MANY_TO_ONE", "Order", "OrderId")]);

        Assert.Empty(SchemaProposeHelpers.ValidateSchemaForDdl([parent, child]));
    }

    [Fact]
    public void MarkLookupVsComposition_KeepsCompositionRootNonLookup()
    {
        // Warehouse/Category = lookup; StockMovement (root) + Item (child) = composition
        var warehouse = Table("Warehouse", pk: "WarehouseId");
        var category = Table("Category", pk: "CategoryId");
        var movement = Table("StockMovement", pk: "MovementId",
            cols: ["MovementId", "WarehouseId", "MovementType", "Status"],
            rels:
            [
                Rel("ONE_TO_MANY", "StockMovementItem", "MovementId"),
                Rel("MANY_TO_ONE", "Warehouse", "WarehouseId")
            ]);
        var item = Table("StockMovementItem", pk: "ItemDetailId",
            cols: ["ItemDetailId", "MovementId", "CategoryId", "SKUCode"],
            rels:
            [
                Rel("MANY_TO_ONE", "StockMovement", "MovementId"),
                Rel("MANY_TO_ONE", "Category", "CategoryId")
            ]);

        var tables = new List<DbGenieTableMetadataDto> { warehouse, category, movement, item };
        SchemaProposeHelpers.MarkLookupVsComposition(tables);

        Assert.True(warehouse.IsLookup);
        Assert.True(category.IsLookup);
        Assert.False(movement.IsLookup);
        Assert.False(item.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_SkipsInvertedOneToManyTowardLookup()
    {
        // StockMovement → Warehouse as ONE_TO_MANY with FK on source is inverted association
        var warehouse = Table("Warehouse", pk: "WarehouseId");
        var movement = Table("StockMovement", pk: "MovementId",
            cols: ["MovementId", "WarehouseId"],
            rels:
            [
                Rel("ONE_TO_MANY", "Warehouse", "WarehouseId"),
                Rel("MANY_TO_ONE", "Warehouse", "WarehouseId"),
                Rel("ONE_TO_MANY", "StockMovementItem", "MovementId")
            ]);
        var item = Table("StockMovementItem", pk: "ItemDetailId",
            cols: ["ItemDetailId", "MovementId"],
            rels: [Rel("MANY_TO_ONE", "StockMovement", "MovementId")]);

        var tables = new List<DbGenieTableMetadataDto> { warehouse, movement, item };
        SchemaProposeHelpers.MarkLookupVsComposition(tables);

        Assert.True(warehouse.IsLookup);
        Assert.False(movement.IsLookup);
        Assert.False(item.IsLookup);
    }

    private static DbGenieTableMetadataDto Table(
        string name,
        string pk,
        string[]? cols = null,
        DbGenieRelationshipMetadataDto[]? rels = null)
    {
        cols ??= [pk];
        return new DbGenieTableMetadataDto
        {
            Name = name,
            Columns = cols.Select(c => new DbGenieColumnMetadataDto
            {
                Name = c,
                IsPrimaryKey = string.Equals(c, pk, StringComparison.OrdinalIgnoreCase)
            }).ToList(),
            Relationships = rels?.ToList() ?? []
        };
    }

    private static DbGenieRelationshipMetadataDto Rel(string type, string target, string fk) =>
        new() { Type = type, TargetTable = target, ForeignKeyColumn = fk };
}
