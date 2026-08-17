using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static Dictionary<string, int> UpsertTransactions(
            AppConfigPackDto pack,
            int tenantDataSourceId,
            int? saasApplicationId,
            AppConfigPackExecuteResultDto executeResult)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var tx in pack.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId) || tx.UnitStructure == null)
                    continue;

                string integrationId = tx.IntegrationId.Trim();
                int transactionId;
                bool inserted;

                using (var conn = OpenTenantConnection())
                {
                    int? existing = GetTransactionIdByIntegrationId(conn, integrationId);
                    if (existing.HasValue)
                    {
                        transactionId = existing.Value;
                        inserted = false;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET TransactionName = @Name,
    Description = @Description,
    SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID),
    AppModifiedDate = GETDATE()
WHERE TransactionID = @Id";
                            cmd.Parameters.AddWithValue("@Name", TruncateName(tx.Name ?? integrationId, 200, integrationId));
                            cmd.Parameters.AddWithValue("@Description", (object)(tx.Description ?? tx.Name) ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Id", transactionId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var setup = new HierarchyTableSetupDto
                        {
                            MasterTableName = tx.UnitStructure.RootTableName,
                            SiblingTableNames = (tx.UnitStructure.SiblingTableNames ?? new List<string>())
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .ToList(),
                            ChildTables = (tx.UnitStructure.ChildUnits ?? new List<AppConfigPackChildUnitDto>())
                                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.TableName))
                                .Select(c => new HierarchyChildTableDto
                                {
                                    TableName = c.TableName,
                                    GrandChildTableNames = (c.GrandChildTableNames ?? new List<string>())
                                        .Where(g => !string.IsNullOrWhiteSpace(g))
                                        .ToList()
                                })
                                .ToList(),
                            DataSourceRegisterId = tenantDataSourceId,
                            SchemaOwner = "dbo",
                            TransactionName = tx.Name,
                            SaasApplicationId = saasApplicationId
                        };

                        var saveResult = AppTransactionBL.CreateHierarchyTransactionFromTables(
                            setup, isIgnoreValidation: true, skipPostSaveCacheSync: false);
                        if (!saveResult.IsSuccessfulWithResult || saveResult.Object == null)
                        {
                            string msg = saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message
                                ?? $"Failed to create transaction '{integrationId}'.";
                            throw new InvalidOperationException(msg);
                        }

                        transactionId = Convert.ToInt32(saveResult.Object.Id);
                        SetIntegrationId(conn, "AppTransaction", "TransactionID", transactionId, integrationId);
                        inserted = true;
                    }
                }

                OverlayTransactionFields(transactionId, tx);
                ApplyChildGridDisplayTypes(transactionId, tx);
                EnsureDefaultForm(transactionId);

                map[integrationId] = transactionId;
                if (inserted)
                {
                    executeResult.TransactionsInserted++;
                    executeResult.Messages.Add($"Inserted transaction {transactionId} ({integrationId}).");
                }
                else
                {
                    executeResult.TransactionsUpdated++;
                    executeResult.Messages.Add($"Updated transaction {transactionId} ({integrationId}).");
                }
            }

            return map;
        }

        private static void OverlayTransactionFields(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx.Fields == null || tx.Fields.Count == 0)
                return;

            using (var conn = OpenTenantConnection())
            {
                foreach (var field in tx.Fields.Where(f => f != null && !string.IsNullOrWhiteSpace(f.ColumnName)))
                {
                    int? entityId = ResolveEntityIdByCode(conn, field.EntityCode);
                    int? matrixFieldId = null;
                    if (!string.IsNullOrWhiteSpace(field.MatrixSourceTable)
                        && !string.IsNullOrWhiteSpace(field.MatrixSourceColumn))
                    {
                        matrixFieldId = GetTransactionFieldId(
                            conn, transactionId, field.MatrixSourceTable, field.MatrixSourceColumn);
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE f SET
    ControlType = COALESCE(@ControlType, f.ControlType),
    EntityId = COALESCE(@EntityId, f.EntityId),
    IsVisible = COALESCE(@IsVisible, f.IsVisible),
    IsReadonly = COALESCE(@IsReadOnly, f.IsReadonly),
    IsPivotColumn = COALESCE(@IsPivotColumn, f.IsPivotColumn),
    IsPivotValue = COALESCE(@IsPivotValue, f.IsPivotValue),
    MatrixForeignKeyFieldId = COALESCE(@MatrixFieldId, f.MatrixForeignKeyFieldId),
    DisplayName = COALESCE(@DisplayName, f.DisplayName),
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND f.DataBaseFieldName = @ColumnName
  AND (@TableName IS NULL OR u.DataBaseTableName = @TableName)";
                        cmd.Parameters.AddWithValue("@ControlType", (object)field.ControlType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsVisible", field.IsVisible.HasValue ? (object)field.IsVisible.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsReadOnly", field.IsReadOnly.HasValue ? (object)field.IsReadOnly.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsPivotColumn", field.IsPivotColumn.HasValue ? (object)field.IsPivotColumn.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsPivotValue", field.IsPivotValue.HasValue ? (object)field.IsPivotValue.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MatrixFieldId", (object)matrixFieldId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DisplayName", string.IsNullOrWhiteSpace(field.DisplayName) ? (object)DBNull.Value : field.DisplayName.Trim());
                        cmd.Parameters.AddWithValue("@TxId", transactionId);
                        cmd.Parameters.AddWithValue("@ColumnName", field.ColumnName.Trim());
                        cmd.Parameters.AddWithValue("@TableName", string.IsNullOrWhiteSpace(field.TableName) ? (object)DBNull.Value : field.TableName.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ApplyChildGridDisplayTypes(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx.UnitStructure?.ChildUnits == null)
                return;

            using (var conn = OpenTenantConnection())
            {
                foreach (var child in tx.UnitStructure.ChildUnits.Where(c => c != null && c.GridDisplayType.HasValue && !string.IsNullOrWhiteSpace(c.TableName)))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET EmGridViewDisplayType = @DisplayType, AppModifiedDate = GETDATE()
WHERE TransactionID = @TxId AND DataBaseTableName = @TableName";
                        cmd.Parameters.AddWithValue("@DisplayType", child.GridDisplayType.Value);
                        cmd.Parameters.AddWithValue("@TxId", transactionId);
                        cmd.Parameters.AddWithValue("@TableName", child.TableName.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static int? GetTransactionFieldId(SqlConnection conn, int transactionId, string tableName, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 f.TransactionFieldID
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static void EnsureDefaultForm(int transactionId)
        {
            var formResult = AppDatabaseViewBL.EnsureTransactionDefaultFlexFormLayout(
                transactionId, migrationFastPath: true, numberOfLayoutColumns: 4);
            if (formResult.ValidationResult != null && formResult.ValidationResult.HasErrors)
            {
                throw new InvalidOperationException(
                    formResult.ValidationResult.Items?.FirstOrDefault()?.Message
                    ?? $"Failed to create default form for transaction {transactionId}.");
            }
        }

        private static int? UpsertTransactionGroup(
            AppConfigPackDto pack,
            Dictionary<string, int> txIdsByIntegration,
            int? saasApplicationId)
        {
            var group = pack.TransactionGroup;
            if (group == null || string.IsNullOrWhiteSpace(group.Name))
                return null;

            var memberIds = new List<int>();
            var memberKeys = (group.MemberTransactionIntegrationIds != null && group.MemberTransactionIntegrationIds.Count > 0)
                ? group.MemberTransactionIntegrationIds
                : txIdsByIntegration.Keys.ToList();

            foreach (var key in memberKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                if (txIdsByIntegration.TryGetValue(key.Trim(), out int txId))
                    memberIds.Add(txId);
            }

            if (memberIds.Count == 0)
                return null;

            int? primaryTxId = null;
            if (!string.IsNullOrWhiteSpace(group.PrimaryTransactionIntegrationId)
                && txIdsByIntegration.TryGetValue(group.PrimaryTransactionIntegrationId.Trim(), out int primary))
            {
                primaryTxId = primary;
            }

            using (var conn = OpenTenantConnection())
            {
                int? groupId = GetTransactionGroupIdByName(conn, group.Name);
                if (!groupId.HasValue)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroup (GroupName, Description, SaasApplicationID)
VALUES (@Name, @Description, @SaasApplicationId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        cmd.Parameters.AddWithValue("@Name", TruncateName(group.Name, 100, group.Name));
                        cmd.Parameters.AddWithValue("@Description", (object)group.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                        groupId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                else
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE dbo.AppTransactionGroup
SET Description = @Description, SaasApplicationID = COALESCE(@SaasApplicationId, SaasApplicationID)
WHERE TransactionGroupID = @GroupId";
                        cmd.Parameters.AddWithValue("@Description", (object)group.Name ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SaasApplicationId", (object)saasApplicationId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                using (var del = conn.CreateCommand())
                {
                    del.CommandText = "DELETE FROM dbo.AppTransactionGroupItem WHERE TransactionGroupID = @GroupId";
                    del.Parameters.AddWithValue("@GroupId", groupId.Value);
                    del.ExecuteNonQuery();
                }

                int order = 0;
                foreach (int txId in memberIds.Distinct())
                {
                    order++;
                    int itemId = EnsureAppTransactionItemId(conn, txId);
                    bool isHeader = primaryTxId.HasValue && primaryTxId.Value == txId;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO dbo.AppTransactionGroupItem (
    TransactionGroupID, TransactionItemID, TransactionLayoutOrder, TransID,
    IsGroupSharedHeader, IsCrossGroupSharedHeader)
VALUES (
    @GroupId, @TransactionItemId, @Order, @TransactionId,
    @IsHeader, @IsHeader)";
                        cmd.Parameters.AddWithValue("@GroupId", groupId.Value);
                        cmd.Parameters.AddWithValue("@TransactionItemId", itemId);
                        cmd.Parameters.AddWithValue("@Order", order);
                        cmd.Parameters.AddWithValue("@TransactionId", txId);
                        cmd.Parameters.AddWithValue("@IsHeader", isHeader);
                        cmd.ExecuteNonQuery();
                    }
                }

                return groupId;
            }
        }

        private static int EnsureAppTransactionItemId(SqlConnection conn, int transactionId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 AppTransactionItemID
FROM dbo.AppTransactionItem
WHERE TransactionID = @TransactionId
ORDER BY AppTransactionItemID";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                var existing = cmd.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                    return Convert.ToInt32(existing);
            }

            string itemName = $"Transaction {transactionId}";
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TransactionName FROM dbo.AppTransaction WHERE TransactionID = @TransactionId";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                var name = cmd.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(name))
                    itemName = name;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
INSERT INTO dbo.AppTransactionItem (TransactionID, TransactionItemName, Description)
VALUES (@TransactionId, @Name, @Description);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                cmd.Parameters.AddWithValue("@TransactionId", transactionId);
                cmd.Parameters.AddWithValue("@Name", itemName);
                cmd.Parameters.AddWithValue("@Description", itemName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void AttachApplicationAssets(
            int? saasApplicationId,
            List<int> transactionIds,
            AppConfigPackExecuteResultDto executeResult)
        {
            if (!saasApplicationId.HasValue || saasApplicationId.Value <= 0 || transactionIds == null || transactionIds.Count == 0)
                return;

            var importDto = new SaasApplicationSectionItemImportDto
            {
                ApplicationId = saasApplicationId.Value,
                SelectedItemIdList = transactionIds.Distinct().ToList()
            };
            var save = AppTransactionBL.ImportSaasApplicationTransactions(importDto);
            if (save.ValidationResult != null && save.ValidationResult.HasErrors)
            {
                executeResult.Messages.Add(
                    save.ValidationResult.Items?.FirstOrDefault()?.Message ?? "Failed to attach transactions to the application.");
            }
            else
            {
                executeResult.Messages.Add($"Attached {transactionIds.Distinct().Count()} transaction(s) to application {saasApplicationId.Value}.");
            }
        }
    }
}
