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
            public bool IsVisible { get; set; } = false;
        }

        /// <summary>
        /// Key: "{tabId}|{subItemId}", value: Visible from pdmTabBlockSubItemExtraInfo (null when row missing).
        /// </summary>
        private static Dictionary<string, int?> LoadPlmSubItemExtraInfoVisibleByKey(
            SqlConnection plmConn,
            IEnumerable<int> tabIds)
        {
            var map = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            var ids = tabIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
                return map;

            var inList = string.Join(",", ids);
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = $@"
SELECT ei.TabID, ei.SubItemID, ei.Visible
FROM dbo.pdmTabBlockSubItemExtraInfo ei
WHERE ei.TabID IN ({inList})";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int tabId = reader.GetInt32(0);
                        int subItemId = reader.GetInt32(1);
                        int? visible = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                        map[$"{tabId}|{subItemId}"] = visible;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Layer 2 (Tab Design layout): set of "{tabId}|{subItemId}" actually placed on the tab layout
        /// (pdmTabLayout -> pdmTabLayoutItem -> pdmTabLayoutSubitem). A sub-item not placed in the design
        /// is never shown even when pdmTabBlockSubItemExtraInfo.Visible = 1.
        /// </summary>
        private static HashSet<string> LoadPlmTabLayoutSubItemSet(
            SqlConnection plmConn,
            IEnumerable<int> tabIds)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = tabIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
                return set;

            var inList = string.Join(",", ids);
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = $@"
SELECT DISTINCT l.TabID, ls.SubItemID
FROM dbo.pdmTabLayout l
INNER JOIN dbo.pdmTabLayoutItem li ON li.LayoutID = l.LayoutID
INNER JOIN dbo.pdmTabLayoutSubitem ls ON ls.LayoutItemID = li.LayoutItemID
WHERE l.TabID IN ({inList})";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.IsDBNull(0) || reader.IsDBNull(1))
                            continue;
                        set.Add($"{reader.GetInt32(0)}|{reader.GetInt32(1)}");
                    }
                }
            }

            return set;
        }

        /// <summary>
        /// Grid column visibility is controlled at tab level by pdmTabGridMetaColumn.Visible (TabID + GridColumnID),
        /// NOT by pdmTabBlockSubItemExtraInfo. Key: "{tabId}|{gridColumnId}", value: Visible (null when row missing).
        /// </summary>
        private static Dictionary<string, int?> LoadPlmTabGridColumnVisibleByKey(
            SqlConnection plmConn,
            IEnumerable<int> tabIds)
        {
            var map = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            var ids = tabIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
                return map;

            var inList = string.Join(",", ids);
            using (var cmd = plmConn.CreateCommand())
            {
                cmd.CommandText = $@"
SELECT tgc.TabID, tgc.GridColumnID, tgc.Visible
FROM dbo.pdmTabGridMetaColumn tgc
WHERE tgc.TabID IN ({inList})";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int tabId = reader.GetInt32(0);
                        int gridColumnId = reader.GetInt32(1);
                        int? visible = reader.IsDBNull(2) ? (int?)null : Convert.ToInt32(reader.GetValue(2));
                        map[$"{tabId}|{gridColumnId}"] = visible;
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// Tab field is visible only when it (1) exists on the tab block, (2) pdmTabBlockSubItemExtraInfo.Visible = 1,
        /// and (3) is placed on the Tab Design layout (pdmTabLayoutSubitem).
        /// </summary>
        private static bool IsPlmTabBlockSubItemVisible(
            int tabId,
            int subItemId,
            HashSet<int> tabBlockSubItemIds,
            Dictionary<string, int?> extraInfoVisibleByKey,
            HashSet<string> tabLayoutSubItemSet)
        {
            if (!tabBlockSubItemIds.Contains(subItemId))
                return false;
            if (!extraInfoVisibleByKey.TryGetValue($"{tabId}|{subItemId}", out int? visible) || visible != 1)
                return false;
            if (!tabLayoutSubItemSet.Contains($"{tabId}|{subItemId}"))
                return false;
            return true;
        }

        /// <summary>
        /// Grid column is visible only when pdmTabGridMetaColumn.Visible = 1 for that TabID + GridColumnID.
        /// </summary>
        private static bool IsPlmGridColumnVisible(
            int tabId,
            int gridColumnId,
            Dictionary<string, int?> tabGridColumnVisibleByKey)
        {
            if (!tabGridColumnVisibleByKey.TryGetValue($"{tabId}|{gridColumnId}", out int? visible))
                return false;
            return visible == 1;
        }

        private static HashSet<string> GetVisibleColumnNames(IEnumerable<PlmTemplateSubItemRow> subItems)
        {
            return new HashSet<string>(
                (subItems ?? Enumerable.Empty<PlmTemplateSubItemRow>())
                    .Where(s => s.IsVisible && !string.IsNullOrWhiteSpace(s.ColumnName))
                    .Select(s => s.ColumnName),
                StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<string> GetVisibleGridColumnNames(IEnumerable<PlmTemplateGridColumnRow> columns)
        {
            return new HashSet<string>(
                (columns ?? Enumerable.Empty<PlmTemplateGridColumnRow>())
                    .Where(c => c.IsVisible && !string.IsNullOrWhiteSpace(c.ColumnSqlName))
                    .Select(c => c.ColumnSqlName),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// After hierarchy transaction creation, apply PLM sub-item / grid-column control types and entity bindings.
        /// CreateHierarchyTransactionFromTables defaults every field to TextBox with no EntityId.
        /// Schema-only TechPack units (TchpPomSpecLine / TchpFitRound / …) have no FieldMapping rows —
        /// do not hide their columns when metadata is missing (that left grids with zero visible columns).
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

            var schemaOnlyTables = GetSchemaOnlyUnitTableNames(plan, tab);
            var fields = new List<(int FieldId, string DbName, string TableName)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT f.TransactionFieldID, f.DataBaseFieldName, u.DataBaseTableName
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
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.IsDBNull(2) ? null : reader.GetString(2)));
                    }
                }
            }

            foreach (var (fieldId, dbName, tableName) in fields)
            {
                if (string.IsNullOrWhiteSpace(dbName))
                    continue;

                bool schemaOnlyUnit = !string.IsNullOrWhiteSpace(tableName)
                    && schemaOnlyTables.Contains(tableName);

                if (!metadataByColumn.TryGetValue(dbName, out PlmFieldMetadata meta))
                {
                    // Plm-mapped units: unmapped leftover columns stay hidden.
                    // Schema-only TechPack units: keep CreateHierarchy / Subset visibility.
                    if (schemaOnlyUnit)
                        continue;

                    using (var hideCmd = conn.CreateCommand())
                    {
                        hideCmd.Transaction = tran;
                        hideCmd.CommandText = @"
UPDATE dbo.AppTransactionField SET IsVisible = 0
WHERE TransactionFieldID = @FieldId";
                        hideCmd.Parameters.AddWithValue("@FieldId", fieldId);
                        hideCmd.ExecuteNonQuery();
                    }
                    continue;
                }

                // Do not overlay Plm column-name metadata onto schema-only TechPack tables
                // (global map is keyed by column name only — "Sort" etc. would steal visibility).
                if (schemaOnlyUnit)
                    continue;

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
    SortOrder = COALESCE(@SortOrder, SortOrder),
    IsVisible = @IsVisible
WHERE TransactionFieldID = @FieldId";
                    cmd.Parameters.AddWithValue("@ControlType", meta.ControlType);
                    cmd.Parameters.AddWithValue("@EntityId", (object)appEntityId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Nbdecimal", meta.Nbdecimal ?? 0);
                    cmd.Parameters.AddWithValue("@DisplayName", (object)meta.DisplayName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SortOrder", meta.SortOrder.HasValue ? (object)(meta.SortOrder.Value * 10) : DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsVisible", meta.IsVisible);
                    cmd.Parameters.AddWithValue("@FieldId", fieldId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Units created from blueprint sibling/child defs with no FieldMapping column plan
        /// (TechPack Tchp* tables filled by step 3b). Visibility comes from schema defaults.
        /// </summary>
        private static HashSet<string> GetSchemaOnlyUnitTableNames(
            TemplateTabExecutionPlan plan,
            PlmTemplateTabRow tab)
        {
            var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void AddMapped(Dictionary<string, List<PlmTemplateSubItemRow>> byTable)
            {
                if (byTable == null)
                    return;
                foreach (var pair in byTable)
                {
                    if (pair.Value != null && pair.Value.Count > 0)
                        mapped.Add(pair.Key);
                }
            }

            if (plan != null)
            {
                AddMapped(plan.SiblingColumnsByTable);
                AddMapped(plan.ChildColumnsByTable);
                AddMapped(plan.GrandchildColumnsByTable);
            }
            if (tab?.GridColumns != null)
            {
                foreach (var g in tab.GridColumns)
                {
                    if (!string.IsNullOrWhiteSpace(g.TableName))
                        mapped.Add(g.TableName);
                }
            }

            var schemaOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Consider(string tableName)
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return;
                string name = tableName.Trim();
                if (!mapped.Contains(name))
                    schemaOnly.Add(name);
            }

            if (plan?.SiblingUnitDefs != null)
            {
                foreach (var s in plan.SiblingUnitDefs)
                {
                    if (!string.IsNullOrWhiteSpace(s?.AppTableName))
                        Consider(s.AppTableName);
                }
            }
            if (plan?.ChildUnitDefs != null)
            {
                foreach (var c in plan.ChildUnitDefs)
                {
                    if (string.IsNullOrWhiteSpace(c?.AppTableName))
                        continue;
                    Consider(c.AppTableName);
                    if (c.GrandChildAppTableNames == null)
                        continue;
                    foreach (var g in c.GrandChildAppTableNames)
                        Consider(g);
                }
            }

            return schemaOnly;
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
                    SortOrder = subItem.SortOrder,
                    IsVisible = subItem.IsVisible
                };
            }

            foreach (var subItem in plan.RootSubItems)
                AddSubItem(subItem);

            foreach (var pair in plan.SiblingColumnsByTable)
            {
                foreach (var subItem in pair.Value)
                    AddSubItem(subItem);
            }

            foreach (var pair in plan.ChildColumnsByTable)
            {
                foreach (var subItem in pair.Value)
                    AddSubItem(subItem);
            }

            foreach (var pair in plan.GrandchildColumnsByTable)
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
                    SortOrder = col.ColumnOrder,
                    IsVisible = col.IsVisible
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
