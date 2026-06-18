using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using APP.Components.Dto;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private sealed class PlmFieldMetadata
        {
            public int ControlType { get; set; }
            public int? PlmEntityId { get; set; }
            public int? Nbdecimal { get; set; }
            public string DisplayName { get; set; }
            public int? SortOrder { get; set; }
        }

        /// <summary>
        /// After hierarchy transaction creation, apply PLM sub-item / grid-column control types and entity bindings.
        /// CreateHierarchyTransactionFromTables defaults every field to TextBox with no EntityId.
        /// </summary>
        private static void ApplyTransactionFieldPlmMetadataSql(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            TemplateTabExecutionPlan plan,
            PlmTemplateTabRow tab)
        {
            var metadataByColumn = BuildPlmFieldMetadataByColumnName(plan, tab);
            if (metadataByColumn.Count == 0)
                return;

            var fields = new List<(int FieldId, string DbName)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT f.TransactionFieldID, f.DataBaseFieldName
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TransactionId
  AND ISNULL(f.IsLinkToParentPrimaryKey, 0) = 0
  AND ISNULL(f.IsPrimaryKey, 0) = 0";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        fields.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1)));
                    }
                }
            }

            foreach (var (fieldId, dbName) in fields)
            {
                if (string.IsNullOrWhiteSpace(dbName)
                    || !metadataByColumn.TryGetValue(dbName, out PlmFieldMetadata meta))
                {
                    continue;
                }

                int? appEntityId = null;
                if (meta.PlmEntityId.HasValue && meta.ControlType == (int)EmAppControlType.DDL)
                    appEntityId = GetAppEntityInfoIdByPlmEntityId(conn, tran, meta.PlmEntityId.Value);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField SET
    ControlType = @ControlType,
    EntityId = @EntityId,
    Nbdecimal = @Nbdecimal,
    DisplayName = @DisplayName,
    SortOrder = COALESCE(@SortOrder, SortOrder)
WHERE TransactionFieldID = @FieldId";
                    cmd.Parameters.AddWithValue("@ControlType", meta.ControlType);
                    cmd.Parameters.AddWithValue("@EntityId", (object)appEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nbdecimal", meta.Nbdecimal ?? 0);
                    cmd.Parameters.AddWithValue("@DisplayName", (object)meta.DisplayName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SortOrder", meta.SortOrder.HasValue ? (object)(meta.SortOrder.Value * 10) : DBNull.Value);
                    cmd.Parameters.AddWithValue("@FieldId", fieldId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Dictionary<string, PlmFieldMetadata> BuildPlmFieldMetadataByColumnName(
            TemplateTabExecutionPlan plan,
            PlmTemplateTabRow tab)
        {
            var map = new Dictionary<string, PlmFieldMetadata>(StringComparer.OrdinalIgnoreCase);

            void AddSubItem(PlmTemplateSubItemRow subItem)
            {
                if (string.IsNullOrWhiteSpace(subItem?.ColumnName))
                    return;

                map[subItem.ColumnName] = new PlmFieldMetadata
                {
                    ControlType = MapPlmControlTypeToApp(subItem.ControlType),
                    PlmEntityId = subItem.EntityId,
                    Nbdecimal = subItem.Nbdecimal,
                    DisplayName = subItem.SubItemName,
                    SortOrder = subItem.SortOrder
                };
            }

            foreach (var subItem in plan.RootSubItems)
                AddSubItem(subItem);

            foreach (var pair in plan.SiblingColumnsByTable)
            {
                foreach (var subItem in pair.Value)
                    AddSubItem(subItem);
            }

            foreach (var col in tab.GridColumns)
            {
                if (string.IsNullOrWhiteSpace(col.ColumnSqlName))
                    continue;

                map[col.ColumnSqlName] = new PlmFieldMetadata
                {
                    ControlType = MapPlmControlTypeToApp(col.ColumnTypeId),
                    PlmEntityId = col.EntityId,
                    Nbdecimal = col.Nbdecimal,
                    DisplayName = col.ColumnName,
                    SortOrder = col.ColumnOrder
                };
            }

            return map;
        }

        /// <summary>PLM EmControlType / EmGridColumnType values align with EmAppControlType for standard controls.</summary>
        private static int MapPlmControlTypeToApp(int plmControlType)
        {
            switch (plmControlType)
            {
                case 1: return (int)EmAppControlType.DDL;
                case 2: return (int)EmAppControlType.TextBox;
                case 4: return (int)EmAppControlType.Memo;
                case 5: return (int)EmAppControlType.Image;
                case 6: return (int)EmAppControlType.Grid;
                case 7: return (int)EmAppControlType.Date;
                case 9: return (int)EmAppControlType.File;
                case 10: return (int)EmAppControlType.Label;
                case 13: return (int)EmAppControlType.CheckBox;
                case 17: return (int)EmAppControlType.Empty;
                case 20: return (int)EmAppControlType.Numeric;
                case 23: return (int)EmAppControlType.AutoGeneration;
                case 24: return (int)EmAppControlType.RGBColorDisplay;
                default: return (int)EmAppControlType.TextBox;
            }
        }

        private static int? GetAppEntityInfoIdByPlmEntityId(SqlConnection conn, SqlTransaction tran, int plmEntityId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 EntityInfoID
FROM dbo.AppEntityInfo
WHERE IntegrationId = @IntegrationId";
                cmd.Parameters.AddWithValue("@IntegrationId", plmEntityId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }
    }
}
