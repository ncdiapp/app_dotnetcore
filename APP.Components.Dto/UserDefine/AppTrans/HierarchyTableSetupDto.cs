using System.Collections.Generic;
using System.Runtime.Serialization;

namespace APP.Components.EntityDto
{
    /// <summary>
    /// Input DTO for creating a hierarchy transaction from existing database table names.
    ///
    /// Structure mirrors the 3-level AppTransaction unit tree:
    ///   RootUnit  (1 master table)
    ///     └── ChildUnit[]  (multiple child tables)
    ///           └── GrandChildUnit[]  (multiple grandchild tables per child)
    ///
    /// FK relationships between tables are auto-detected from the database schema.
    /// </summary>
    [DataContract]
    public class HierarchyTableSetupDto
    {
        /// <summary>The root (master) table name, e.g. "Order".</summary>
        [DataMember]
        public string MasterTableName { get; set; }

        /// <summary>
        /// Master-sibling units (same ReferenceId as root). Saved to <c>AppTransactionUnitList</c> with
        /// <see cref="AppTransactionUnitExDto.IsMasterSiblingUnit"/> — not under <c>rootUnit.Children</c>.
        /// PLM tab tables use this; grid tables belong in <see cref="ChildTables"/>.
        /// </summary>
        [DataMember]
        public List<string> SiblingTableNames { get; set; }

        /// <summary>
        /// Child units directly under the root, each optionally with their own grandchild tables.
        /// Example: Order → [OrderLineItem[OrderLineItemDetail, OrderLineItemNote], OrderNote[]]
        /// </summary>
        [DataMember]
        public List<HierarchyChildTableDto> ChildTables { get; set; }

        [DataMember]
        public int? DataSourceRegisterId { get; set; }

        [DataMember]
        public string SchemaOwner { get; set; }

        /// <summary>Optional friendly name for the generated transaction. Defaults to the master table display name.</summary>
        [DataMember]
        public string TransactionName { get; set; }

        [DataMember]
        public int? SaasApplicationId { get; set; }
    }

    /// <summary>
    /// Represents one child unit in the hierarchy, with an optional list of its own grandchild tables.
    /// </summary>
    [DataContract]
    public class HierarchyChildTableDto
    {
        /// <summary>Child table name, e.g. "OrderLineItem".</summary>
        [DataMember]
        public string TableName { get; set; }

        /// <summary>
        /// Grandchild table names that belong to this child, e.g. ["OrderLineItemDetail", "OrderLineItemNote"].
        /// </summary>
        [DataMember]
        public List<string> GrandChildTableNames { get; set; }
    }
}
