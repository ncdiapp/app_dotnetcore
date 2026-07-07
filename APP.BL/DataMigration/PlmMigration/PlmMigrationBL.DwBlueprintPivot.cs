using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.BL.DataMigration.PlmMigration
{
    public static partial class PlmMigrationBL
    {
        private static void ApplyBomColorwayPivotBindingsSql(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            int plmTabId,
            IReadOnlyList<PlmDwBlueprintBomColorwayPivotBindingDto> bindings)
        {
            if (bindings == null || bindings.Count == 0)
                return;

            foreach (var binding in bindings.Where(b => b != null && b.PlmTabId == plmTabId))
            {
                if (string.IsNullOrWhiteSpace(binding.HostAppTableName)
                    || string.IsNullOrWhiteSpace(binding.GrandchildAppTableName)
                    || string.IsNullOrWhiteSpace(binding.SourceAppTableName))
                    continue;

                int? hostUnitId = GetTransactionUnitIdByTableName(conn, tran, transactionId, binding.HostAppTableName);
                if (!hostUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: host unit not found for table {binding.HostAppTableName} on transaction {transactionId}.");

                int? sourceUnitId = GetTransactionUnitIdByTableName(conn, tran, transactionId, binding.SourceAppTableName);
                if (!sourceUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: source unit not found for table {binding.SourceAppTableName} on transaction {transactionId}.");

                string pivotKeyColumn = string.IsNullOrWhiteSpace(binding.SourcePivotKeyColumn)
                    ? "Color"
                    : binding.SourcePivotKeyColumn.Trim();

                int? sourcePivotFieldId = GetTransactionFieldId(conn, tran, sourceUnitId.Value, pivotKeyColumn);
                if (!sourcePivotFieldId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: source field {binding.SourceAppTableName}.{pivotKeyColumn} not found.");

                int? grandchildUnitId = GetChildTransactionUnitIdByTableName(
                    conn, tran, transactionId, hostUnitId.Value, binding.GrandchildAppTableName);
                if (!grandchildUnitId.HasValue)
                    throw new InvalidOperationException(
                        $"BOM colorway pivot: grandchild unit not found for table {binding.GrandchildAppTableName} under host unit {hostUnitId}.");

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET EmGridViewDisplayType = @DisplayType
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@DisplayType", (int)EmAppTransactionGridDisplayType.ChildUnitPivotColumns);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.ExecuteNonQuery();
                }

                string colorwayField = binding.GrandchildColumns?.ColorwayKey ?? "Colorway";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsPivotColumn = 1,
    MatrixForeignKeyFieldId = @SourceFieldId,
    IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@SourceFieldId", sourcePivotFieldId.Value);
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", colorwayField);
                    cmd.ExecuteNonQuery();
                }

                if (binding.GrandchildColumns?.ValueFields != null)
                {
                    foreach (var vf in binding.GrandchildColumns.ValueFields.Where(v => v != null && !string.IsNullOrWhiteSpace(v.Column)))
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tran;
                            cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsPivotValue = @IsPivotValue,
    IsVisible = 1
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                            cmd.Parameters.AddWithValue("@IsPivotValue", vf.IsPivotValue);
                            cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                            cmd.Parameters.AddWithValue("@FieldName", vf.Column.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                string parentLink = binding.GrandchildColumns?.ParentLink ?? "ParentRowId";
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET IsVisible = 0
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@UnitId", grandchildUnitId.Value);
                    cmd.Parameters.AddWithValue("@FieldName", parentLink);
                    cmd.ExecuteNonQuery();
                }

                DeleteHostStagingPivotFields(conn, tran, hostUnitId.Value, binding);
            }
        }

        private static void DeleteHostStagingPivotFields(
            SqlConnection conn,
            SqlTransaction tran,
            int hostUnitId,
            PlmDwBlueprintBomColorwayPivotBindingDto binding)
        {
            var fieldNames = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@UnitId", hostUnitId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            fieldNames.Add(reader.GetString(0));
                    }
                }
            }

            foreach (string fieldName in fieldNames)
            {
                if (!IsBomColorwayStagingHostColumn(fieldName, binding))
                    continue;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tran;
                    cmd.CommandText = @"
DELETE FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                    cmd.Parameters.AddWithValue("@UnitId", hostUnitId);
                    cmd.Parameters.AddWithValue("@FieldName", fieldName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool IsBomColorwayStagingHostColumn(
            string fieldName,
            PlmDwBlueprintBomColorwayPivotBindingDto binding)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                return false;

            if (Regex.IsMatch(fieldName, @"^Colorway_\d+$", RegexOptions.IgnoreCase))
                return true;
            if (Regex.IsMatch(fieldName, @"^Image\d+$", RegexOptions.IgnoreCase))
                return true;

            if (binding.StagingHostColumnPatterns != null)
            {
                foreach (string pattern in binding.StagingHostColumnPatterns.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    string regex = "^" + Regex.Escape(pattern).Replace("%", ".*") + "$";
                    if (Regex.IsMatch(fieldName, regex, RegexOptions.IgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private static int? GetChildTransactionUnitIdByTableName(
            SqlConnection conn,
            SqlTransaction tran,
            int transactionId,
            int parentUnitId,
            string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TransactionId
  AND ParentTransactionUnitID = @ParentUnitId
  AND DataBaseTableName = @TableName";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@ParentUnitId", parentUnitId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetTransactionFieldId(
            SqlConnection conn,
            SqlTransaction tran,
            int unitId,
            string fieldName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = @"
SELECT TOP 1 TransactionFieldID
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
  AND DataBaseFieldName = @FieldName";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.Parameters.AddWithValue("@FieldName", fieldName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void AttachBomColorwayHierarchyChildren(
            List<HierarchyChildTableDto> rootChildTables,
            IReadOnlyList<PlmDwBlueprintBomColorwayPivotBindingDto> tabBindings)
        {
            if (tabBindings == null || tabBindings.Count == 0)
                return;

            foreach (var binding in tabBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.HostAppTableName)
                    || string.IsNullOrWhiteSpace(binding.GrandchildAppTableName)
                    || string.IsNullOrWhiteSpace(binding.SourceAppTableName))
                    continue;

                if (!rootChildTables.Any(c => string.Equals(c.TableName, binding.SourceAppTableName, StringComparison.OrdinalIgnoreCase)))
                {
                    rootChildTables.Add(new HierarchyChildTableDto { TableName = binding.SourceAppTableName });
                }

                var hostChild = rootChildTables.FirstOrDefault(c =>
                    string.Equals(c.TableName, binding.HostAppTableName, StringComparison.OrdinalIgnoreCase));
                if (hostChild == null)
                {
                    hostChild = new HierarchyChildTableDto
                    {
                        TableName = binding.HostAppTableName,
                        GrandChildTableNames = new List<string>()
                    };
                    rootChildTables.Add(hostChild);
                }

                if (hostChild.GrandChildTableNames == null)
                    hostChild.GrandChildTableNames = new List<string>();

                if (!hostChild.GrandChildTableNames.Any(g =>
                    string.Equals(g, binding.GrandchildAppTableName, StringComparison.OrdinalIgnoreCase)))
                {
                    hostChild.GrandChildTableNames.Add(binding.GrandchildAppTableName);
                }
            }
        }
    }
}
