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
        var warehouse = Table("Warehouse", pk: "WarehouseId",
            cols: ["WarehouseId", "WarehouseCode", "WarehouseName"]);
        var category = Table("Category", pk: "CategoryId",
            cols: ["CategoryId", "CategoryName"]);
        var movement = Table("StockMovement", pk: "MovementId",
            cols: ["MovementId", "WarehouseId", "MovementType", "Status", "MovementDate"],
            rels:
            [
                Rel("ONE_TO_MANY", "StockMovementItem", "MovementId"),
                Rel("MANY_TO_ONE", "Warehouse", "WarehouseId")
            ]);
        var item = Table("StockMovementItem", pk: "ItemDetailId",
            cols: ["ItemDetailId", "MovementId", "CategoryId", "SKUCode", "Quantity"],
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
        var warehouse = Table("Warehouse", pk: "WarehouseId",
            cols: ["WarehouseId", "WarehouseCode", "WarehouseName"]);
        var movement = Table("StockMovement", pk: "MovementId",
            cols: ["MovementId", "WarehouseId", "MovementDate", "ReferenceNo"],
            rels:
            [
                Rel("ONE_TO_MANY", "Warehouse", "WarehouseId"),
                Rel("MANY_TO_ONE", "Warehouse", "WarehouseId"),
                Rel("ONE_TO_MANY", "StockMovementItem", "MovementId")
            ]);
        var item = Table("StockMovementItem", pk: "ItemDetailId",
            cols: ["ItemDetailId", "MovementId", "Quantity"],
            rels: [Rel("MANY_TO_ONE", "StockMovement", "MovementId")]);

        var tables = new List<DbGenieTableMetadataDto> { warehouse, movement, item };
        SchemaProposeHelpers.MarkLookupVsComposition(tables);

        Assert.True(warehouse.IsLookup);
        Assert.False(movement.IsLookup);
        Assert.False(item.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_DemotesReferenceParentOfDocument()
    {
        // LLM wrongly emits Customer ONE_TO_MANY → Order; Order is the real document master.
        var customer = Table("Customer", pk: "CustomerId",
            cols: ["CustomerId", "CustomerCode", "CustomerName"]);
        var order = Table("Order", pk: "OrderId",
            cols: ["OrderId", "CustomerId", "OrderDate", "Status"],
            rels:
            [
                Rel("MANY_TO_ONE", "Customer", "CustomerId"),
                Rel("ONE_TO_MANY", "OrderItem", "OrderId")
            ]);
        var item = Table("OrderItem", pk: "OrderItemId",
            cols: ["OrderItemId", "OrderId", "Quantity"],
            rels: [Rel("MANY_TO_ONE", "Order", "OrderId")]);
        customer.Relationships =
        [
            Rel("ONE_TO_MANY", "Order", "CustomerId")
        ];

        var tables = new List<DbGenieTableMetadataDto> { customer, order, item };
        SchemaProposeHelpers.MarkLookupVsComposition(tables);

        Assert.True(customer.IsLookup);
        Assert.False(order.IsLookup);
        Assert.False(item.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_HonorsExplicitIsLookupHint()
    {
        var supplier = Table("Supplier", pk: "SupplierId",
            cols: ["SupplierId", "SupplierName"]);
        supplier.IsLookup = true;
        var order = Table("Order", pk: "OrderId",
            cols: ["OrderId", "SupplierId", "OrderDate"],
            rels:
            [
                Rel("MANY_TO_ONE", "Supplier", "SupplierId"),
                Rel("ONE_TO_MANY", "OrderItem", "OrderId")
            ]);
        var item = Table("OrderItem", pk: "OrderItemId",
            cols: ["OrderItemId", "OrderId", "Quantity"],
            rels: [Rel("MANY_TO_ONE", "Order", "OrderId")]);

        SchemaProposeHelpers.MarkLookupVsComposition([supplier, order, item]);

        Assert.True(supplier.IsLookup);
        Assert.False(order.IsLookup);
        Assert.False(item.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_KeepsTrueDocumentRoot()
    {
        var order = Table("Order", pk: "OrderId",
            cols: ["OrderId", "OrderDate", "OrderNumber", "Status"],
            rels: [Rel("ONE_TO_MANY", "OrderItem", "OrderId")]);
        var item = Table("OrderItem", pk: "OrderItemId",
            cols: ["OrderItemId", "OrderId", "Quantity"],
            rels: [Rel("MANY_TO_ONE", "Order", "OrderId")]);

        SchemaProposeHelpers.MarkLookupVsComposition([order, item]);

        Assert.False(order.IsLookup);
        Assert.False(item.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_KeepsReferenceMasterWithOnlyOwnedChildren()
    {
        // Customer + Address only (no Order document) must stay composition — not demoted to lookup.
        var customer = Table("Customer", pk: "CustomerId",
            cols: ["CustomerId", "CustomerCode", "CustomerName"],
            rels: [Rel("ONE_TO_MANY", "CustomerAddress", "CustomerId")]);
        var address = Table("CustomerAddress", pk: "AddressId",
            cols: ["AddressId", "CustomerId", "AddressLine", "City"],
            rels: [Rel("MANY_TO_ONE", "Customer", "CustomerId")]);

        SchemaProposeHelpers.MarkLookupVsComposition([customer, address]);

        Assert.False(customer.IsLookup);
        Assert.False(address.IsLookup);
    }

    [Fact]
    public void MarkLookupVsComposition_InfersCompositionFromChildManyToOneOnly()
    {
        // LLM omitted parent ONE_TO_MANY — only child MANY_TO_ONE to document parent.
        var movement = Table("StockMovement", pk: "Id",
            cols: ["Id", "MovementDate", "MovementNumber", "Status", "WarehouseId"],
            rels: [Rel("MANY_TO_ONE", "Warehouse", "WarehouseId")]);
        var item = Table("StockMovementItem", pk: "Id",
            cols: ["Id", "StockMovementId", "ProductId", "Quantity", "UnitPrice"],
            rels:
            [
                Rel("MANY_TO_ONE", "StockMovement", "StockMovementId"),
                Rel("MANY_TO_ONE", "Product", "ProductId")
            ]);
        var warehouse = Table("Warehouse", pk: "Id",
            cols: ["Id", "WarehouseCode", "WarehouseName"]);
        var product = Table("Product", pk: "Id",
            cols: ["Id", "SKU", "ProductName"]);

        var tables = new List<DbGenieTableMetadataDto> { warehouse, product, movement, item };
        SchemaProposeHelpers.MarkLookupVsComposition(tables);

        Assert.True(warehouse.IsLookup);
        Assert.True(product.IsLookup);
        Assert.False(movement.IsLookup);
        Assert.False(item.IsLookup);

        var children = SchemaProposeHelpers.GetDirectCompositionChildren("StockMovement", tables);
        Assert.Contains("StockMovementItem", children);
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
