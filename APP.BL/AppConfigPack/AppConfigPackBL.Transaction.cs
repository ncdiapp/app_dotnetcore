using System;
using System.Collections.Generic;
using System.Data;
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
                            SiblingTableNames = MergeSiblingTableNames(tx.UnitStructure),
                            ChildTables = (tx.UnitStructure.ChildUnits ?? new List<AppConfigPackChildUnitDto>())
                                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.TableName))
                                .Select(c => new HierarchyChildTableDto
                                {
                                    TableName = c.TableName,
                                    GrandChildTableNames = MergeGrandChildTableNames(c)
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
                ApplyUnitOverlays(transactionId, tx);
                WireLogicalParentKeys(transactionId, tx, pack.Tables);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
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
                foreach (var field in tx.Fields
                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.ColumnName))
                    .OrderByDescending(f => f.IsPrimaryKey == true)
                    .ThenBy(f => f.IsLinkToParentPrimaryKey == true))
                {
                    int? entityId = ResolveEntityIdByCode(conn, field.EntityCode);
                    int? matrixFieldId = null;
                    if (!string.IsNullOrWhiteSpace(field.MatrixSourceTable)
                        && !string.IsNullOrWhiteSpace(field.MatrixSourceColumn))
                    {
                        matrixFieldId = GetTransactionFieldId(
                            conn, transactionId, field.MatrixSourceTable, field.MatrixSourceColumn);
                    }

                    int? dependsOnFieldId = null;
                    if (!string.IsNullOrWhiteSpace(field.DependsOnTable)
                        && !string.IsNullOrWhiteSpace(field.DependsOnColumn))
                    {
                        dependsOnFieldId = GetTransactionFieldId(
                            conn, transactionId, field.DependsOnTable, field.DependsOnColumn);
                        if (!dependsOnFieldId.HasValue)
                        {
                            throw new InvalidOperationException(
                                $"Depends-on field '{field.DependsOnTable}.{field.DependsOnColumn}' was not found for '{field.TableName}.{field.ColumnName}'.");
                        }
                    }

                    int? parentPkFieldId = null;
                    if (field.IsLinkToParentPrimaryKey == true && !string.IsNullOrWhiteSpace(field.TableName))
                    {
                        parentPkFieldId = GetParentPrimaryKeyFieldId(conn, transactionId, field.TableName);
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE f SET
    ControlType = COALESCE(@ControlType, f.ControlType),
    EntityId = COALESCE(@EntityId, f.EntityId),
    IsVisible = CASE WHEN @IsVisible IS NULL THEN f.IsVisible ELSE @IsVisible END,
    IsReadonly = CASE WHEN @IsReadOnly IS NULL THEN f.IsReadonly ELSE @IsReadOnly END,
    IsPrimaryKey = CASE WHEN @IsPrimaryKey IS NULL THEN f.IsPrimaryKey ELSE @IsPrimaryKey END,
    IsLinkToParentPrimaryKey = CASE WHEN @IsLinkToParent IS NULL THEN f.IsLinkToParentPrimaryKey ELSE @IsLinkToParent END,
    LinkToParentPrimaryKeyFieldID = COALESCE(@ParentPkFieldId, f.LinkToParentPrimaryKeyFieldID),
    IsPivotRow = CASE WHEN @IsPivotRow IS NULL THEN f.IsPivotRow ELSE @IsPivotRow END,
    IsPivotColumn = CASE WHEN @IsPivotColumn IS NULL THEN f.IsPivotColumn ELSE @IsPivotColumn END,
    IsPivotValue = CASE WHEN @IsPivotValue IS NULL THEN f.IsPivotValue ELSE @IsPivotValue END,
    MatrixForeignKeyFieldId = COALESCE(@MatrixFieldId, f.MatrixForeignKeyFieldId),
    DisplayName = COALESCE(@DisplayName, f.DisplayName),
    DDLParentLevelID = COALESCE(@DependsOnFieldId, f.DDLParentLevelID),
    CascadingRelationTable = COALESCE(@CascadingTable, f.CascadingRelationTable),
    CascadingRelationTableSchemaOwner = COALESCE(@CascadingSchema, f.CascadingRelationTableSchemaOwner),
    CascadingRelationTableParentKeyField = COALESCE(@CascadingParent, f.CascadingRelationTableParentKeyField),
    CascadingRelationTableChildKeyField = COALESCE(@CascadingChild, f.CascadingRelationTableChildKeyField),
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND f.DataBaseFieldName = @ColumnName
  AND (@TableName IS NULL OR u.DataBaseTableName = @TableName)";
                        cmd.Parameters.AddWithValue("@ControlType", (object)field.ControlType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EntityId", (object)entityId ?? DBNull.Value);
                        AddNullableBit(cmd, "@IsVisible", field.IsVisible);
                        AddNullableBit(cmd, "@IsReadOnly", field.IsReadOnly);
                        AddNullableBit(cmd, "@IsPrimaryKey", field.IsPrimaryKey);
                        AddNullableBit(cmd, "@IsLinkToParent", field.IsLinkToParentPrimaryKey);
                        cmd.Parameters.AddWithValue("@ParentPkFieldId", (object)parentPkFieldId ?? DBNull.Value);
                        AddNullableBit(cmd, "@IsPivotRow", field.IsPivotRow);
                        AddNullableBit(cmd, "@IsPivotColumn", field.IsPivotColumn);
                        AddNullableBit(cmd, "@IsPivotValue", field.IsPivotValue);
                        cmd.Parameters.AddWithValue("@MatrixFieldId", (object)matrixFieldId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DisplayName", string.IsNullOrWhiteSpace(field.DisplayName) ? (object)DBNull.Value : field.DisplayName.Trim());
                        cmd.Parameters.AddWithValue("@DependsOnFieldId", (object)dependsOnFieldId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CascadingTable", string.IsNullOrWhiteSpace(field.CascadingRelationTable) ? (object)DBNull.Value : field.CascadingRelationTable.Trim());
                        cmd.Parameters.AddWithValue("@CascadingSchema", string.IsNullOrWhiteSpace(field.CascadingRelationSchemaOwner) ? (object)DBNull.Value : field.CascadingRelationSchemaOwner.Trim());
                        cmd.Parameters.AddWithValue("@CascadingParent", string.IsNullOrWhiteSpace(field.CascadingParentKey) ? (object)DBNull.Value : field.CascadingParentKey.Trim());
                        cmd.Parameters.AddWithValue("@CascadingChild", string.IsNullOrWhiteSpace(field.CascadingChildKey) ? (object)DBNull.Value : field.CascadingChildKey.Trim());
                        cmd.Parameters.AddWithValue("@TxId", transactionId);
                        cmd.Parameters.AddWithValue("@ColumnName", field.ColumnName.Trim());
                        cmd.Parameters.AddWithValue("@TableName", string.IsNullOrWhiteSpace(field.TableName) ? (object)DBNull.Value : field.TableName.Trim());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static List<string> MergeSiblingTableNames(AppConfigPackUnitStructureDto structure)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                if (seen.Add(name.Trim()))
                    names.Add(name.Trim());
            }

            foreach (var name in structure?.SiblingTableNames ?? new List<string>())
                Add(name);
            foreach (var sibling in structure?.SiblingUnits ?? new List<AppConfigPackSiblingUnitDto>())
                Add(sibling?.TableName);
            return names;
        }

        private static List<string> MergeGrandChildTableNames(AppConfigPackChildUnitDto child)
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    return;
                if (seen.Add(name.Trim()))
                    names.Add(name.Trim());
            }

            foreach (var name in child?.GrandChildTableNames ?? new List<string>())
                Add(name);
            foreach (var grand in child?.GrandChildUnits ?? new List<AppConfigPackChildUnitDto>())
                Add(grand?.TableName);
            return names;
        }

        private static void ApplyUnitOverlays(int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx?.UnitStructure == null)
                return;

            using (var conn = OpenTenantConnection())
            {
                if (!string.IsNullOrWhiteSpace(tx.UnitStructure.RootDisplayName)
                    && !string.IsNullOrWhiteSpace(tx.UnitStructure.RootTableName))
                {
                    UpdateUnitDisplayName(conn, transactionId, tx.UnitStructure.RootTableName, tx.UnitStructure.RootDisplayName);
                }

                foreach (var sibling in tx.UnitStructure.SiblingUnits ?? Enumerable.Empty<AppConfigPackSiblingUnitDto>())
                {
                    if (sibling == null || string.IsNullOrWhiteSpace(sibling.TableName) || string.IsNullOrWhiteSpace(sibling.DisplayName))
                        continue;
                    UpdateUnitDisplayName(conn, transactionId, sibling.TableName, sibling.DisplayName);
                }

                foreach (var child in tx.UnitStructure.ChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                {
                    ApplyOneChildUnitOverlay(conn, transactionId, child);
                    foreach (var grand in child.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                        ApplyOneChildUnitOverlay(conn, transactionId, grand);
                }
            }
        }

        private static void ApplyOneChildUnitOverlay(SqlConnection conn, int transactionId, AppConfigPackChildUnitDto child)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.TableName))
                return;

            int? unitId = GetTransactionUnitId(conn, transactionId, child.TableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{child.TableName}' was not found.");

            if (!string.IsNullOrWhiteSpace(child.DisplayName))
                UpdateUnitDisplayNameById(conn, unitId.Value, child.DisplayName.Trim());

            if (child.GridDisplayType.HasValue || child.IsReadOnly.HasValue || child.IsSynchToDatabaseTable.HasValue)
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit SET
    EmGridViewDisplayType = COALESCE(@DisplayType, EmGridViewDisplayType),
    IsReadOnly = CASE WHEN @IsReadOnly IS NULL THEN IsReadOnly ELSE @IsReadOnly END,
    IsSynchToDatabaseTable = CASE WHEN @IsSynch IS NULL THEN IsSynchToDatabaseTable ELSE @IsSynch END,
    AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                    cmd.Parameters.AddWithValue("@DisplayType", (object)child.GridDisplayType ?? DBNull.Value);
                    AddNullableBit(cmd, "@IsReadOnly", child.IsReadOnly);
                    AddNullableBit(cmd, "@IsSynch", child.IsSynchToDatabaseTable);
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            if (string.IsNullOrWhiteSpace(child.AvailableSourceTableName))
                return;

            int? sourceUnitId = GetTransactionUnitId(conn, transactionId, child.AvailableSourceTableName);
            if (!sourceUnitId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Available source unit '{child.AvailableSourceTableName}' was not found for '{child.TableName}'.");
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET AvailableSourceUnitID = @SourceUnitId, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@SourceUnitId", sourceUnitId.Value);
                cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                cmd.ExecuteNonQuery();
            }

            using (var srcFlag = conn.CreateCommand())
            {
                srcFlag.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET IsUsedForLoadingAvailableSource = 1, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @SourceUnitId";
                srcFlag.Parameters.AddWithValue("@SourceUnitId", sourceUnitId.Value);
                srcFlag.ExecuteNonQuery();
            }

            string selectedColumn = child.AvailableSelectSelectedColumn;
            if (string.IsNullOrWhiteSpace(selectedColumn))
            {
                throw new InvalidOperationException(
                    $"availableSelectSelectedColumn is required when availableSourceTableName is set on '{child.TableName}'.");
            }

            string sourceColumn = string.IsNullOrWhiteSpace(child.AvailableSelectSourceColumn)
                ? selectedColumn
                : child.AvailableSelectSourceColumn;
            int? selectedFieldId = GetTransactionFieldId(conn, transactionId, child.TableName, selectedColumn);
            int? sourceFieldId = GetTransactionFieldId(conn, transactionId, child.AvailableSourceTableName, sourceColumn);
            if (!selectedFieldId.HasValue || !sourceFieldId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Available Select mapping {child.TableName}.{selectedColumn} → {child.AvailableSourceTableName}.{sourceColumn} was not found.");
            }

            using (var mapCmd = conn.CreateCommand())
            {
                mapCmd.CommandText = @"
UPDATE dbo.AppTransactionField
SET MappingToAvailableSourceUnitTransactionFieldID = @SourceFieldId, AppModifiedDate = GETDATE()
WHERE TransactionFieldID = @SelectedFieldId";
                mapCmd.Parameters.AddWithValue("@SourceFieldId", sourceFieldId.Value);
                mapCmd.Parameters.AddWithValue("@SelectedFieldId", selectedFieldId.Value);
                mapCmd.ExecuteNonQuery();
            }
        }

        internal static void ApplyTransactionChildLinkTargets(
            AppConfigPackDto pack,
            Dictionary<string, int> txIdsByIntegration)
        {
            foreach (var tx in pack?.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId) || tx.UnitStructure?.ChildUnits == null)
                    continue;
                if (!txIdsByIntegration.TryGetValue(tx.IntegrationId.Trim(), out int transactionId))
                    continue;

                using (var conn = OpenTenantConnection())
                {
                    foreach (var child in tx.UnitStructure.ChildUnits)
                    {
                        ApplyChildLinkTargets(conn, transactionId, child, txIdsByIntegration);
                        foreach (var grand in child?.GrandChildUnits ?? Enumerable.Empty<AppConfigPackChildUnitDto>())
                            ApplyChildLinkTargets(conn, transactionId, grand, txIdsByIntegration);
                    }
                }
            }
        }

        private static void ApplyChildLinkTargets(
            SqlConnection conn,
            int transactionId,
            AppConfigPackChildUnitDto child,
            Dictionary<string, int> txIdsByIntegration)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.TableName) || child.LinkTargets == null)
                return;

            int? unitId = GetTransactionUnitId(conn, transactionId, child.TableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{child.TableName}' was not found for link targets.");

            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM dbo.AppFormLinkTarget WHERE TransactionUnitID = @UnitId";
                del.Parameters.AddWithValue("@UnitId", unitId.Value);
                del.ExecuteNonQuery();
            }

            int sort = 10;
            foreach (var link in child.LinkTargets)
            {
                if (link == null || string.IsNullOrWhiteSpace(link.TransactionIntegrationId))
                    continue;

                int? targetTxId;
                if (!txIdsByIntegration.TryGetValue(link.TransactionIntegrationId.Trim(), out int mappedId))
                    targetTxId = GetTransactionIdByIntegrationId(conn, link.TransactionIntegrationId);
                else
                    targetTxId = mappedId;

                if (!targetTxId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Link target transaction '{link.TransactionIntegrationId}' was not found for unit '{child.TableName}'.");
                }

                string sourceColumn = string.IsNullOrWhiteSpace(link.SourceColumn) ? null : link.SourceColumn.Trim();
                if (string.IsNullOrWhiteSpace(sourceColumn))
                {
                    throw new InvalidOperationException(
                        $"Link target '{link.Name ?? link.ActionType}' on '{child.TableName}' is missing sourceColumn.");
                }

                string targetColumn = string.IsNullOrWhiteSpace(link.TargetColumn) ? sourceColumn : link.TargetColumn.Trim();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppFormLinkTarget (
    TransactionUnitID,
    NavigationActionName,
    ActionType,
    LinkTargetTransactionID,
    LinkTargetUsageType,
    SourceColumnType,
    SourceColumn1,
    TargetColumn1,
    Sort,
    IsPopup,
    PopupWidth,
    PopupHeight)
VALUES (
    @UnitId,
    @Name,
    @ActionType,
    @LinkTargetTransactionId,
    @UsageType,
    @SourceColumnType,
    @SourceColumn,
    @TargetColumn,
    @Sort,
    @IsPopup,
    @PopupWidth,
    @PopupHeight)";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(link.Name) ? (link.ActionType ?? "Edit") : link.Name.Trim());
                    cmd.Parameters.AddWithValue("@ActionType", ResolveLinkTargetActionType(link.ActionType));
                    cmd.Parameters.AddWithValue("@LinkTargetTransactionId", targetTxId.Value);
                    cmd.Parameters.AddWithValue("@UsageType", (int)EmAppLinkTargetUsageType.TransactionUnitLinkToForm);
                    cmd.Parameters.AddWithValue("@SourceColumnType", (int)EmAppLinkTargetSourceColumnType.TransactionField);
                    cmd.Parameters.AddWithValue("@SourceColumn", sourceColumn);
                    cmd.Parameters.AddWithValue("@TargetColumn", targetColumn);
                    cmd.Parameters.AddWithValue("@Sort", link.Sort ?? sort);
                    cmd.Parameters.AddWithValue("@IsPopup", link.IsPopup ?? true);
                    cmd.Parameters.AddWithValue("@PopupWidth", link.PopupWidth ?? 1200);
                    cmd.Parameters.AddWithValue("@PopupHeight", link.PopupHeight ?? 700);
                    cmd.ExecuteNonQuery();
                }

                sort += 10;
            }
        }

        private static void UpdateUnitDisplayName(SqlConnection conn, int transactionId, string tableName, string displayName)
        {
            int? unitId = GetTransactionUnitId(conn, transactionId, tableName);
            if (!unitId.HasValue)
                throw new InvalidOperationException($"Transaction unit '{tableName}' was not found.");
            UpdateUnitDisplayNameById(conn, unitId.Value, displayName);
        }

        private static void UpdateUnitDisplayNameById(SqlConnection conn, int unitId, string displayName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransactionUnit
SET UnitDisplayName = @DisplayName, AppModifiedDate = GETDATE()
WHERE TransactionUnitID = @UnitId";
                cmd.Parameters.AddWithValue("@DisplayName", TruncateName(displayName, 200, displayName));
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.ExecuteNonQuery();
            }
        }

        private static int? GetTransactionUnitId(SqlConnection conn, int transactionId, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionUnitID
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId AND DataBaseTableName = @TableName
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetParentPrimaryKeyFieldId(SqlConnection conn, int transactionId, string childTableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 pk.TransactionFieldID
FROM dbo.AppTransactionUnit child
INNER JOIN dbo.AppTransactionUnit parent
    ON parent.TransactionUnitID = COALESCE(child.ParentTransactionUnitID,
        (SELECT TOP 1 r.TransactionUnitID
         FROM dbo.AppTransactionUnit r
         WHERE r.TransactionID = child.TransactionID
           AND r.ParentTransactionUnitID IS NULL
           AND ISNULL(r.IsMasterSiblingUnit, 0) = 0
         ORDER BY r.TransactionUnitID))
INNER JOIN dbo.AppTransactionField pk
    ON pk.TransactionUnitID = parent.TransactionUnitID
   AND pk.IsPrimaryKey = 1
WHERE child.TransactionID = @TxId
  AND child.DataBaseTableName = @TableName
ORDER BY pk.TransactionFieldID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", childTableName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
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

        private static void AddNullableBit(SqlCommand cmd, string name, bool? value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Bit);
            p.IsNullable = true;
            p.Value = value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private sealed class PackUnitRow
        {
            public int UnitId { get; set; }
            public string TableName { get; set; }
            public int? ParentUnitId { get; set; }
            public bool IsSibling { get; set; }
        }

        /// <summary>
        /// After field/unit overlay: mark VIEW PKs, then wire child/sibling Link-To-Parent even when there is no DB FK.
        /// Same column name as parent PK wins; StyleSpecId ↔ ReferenceId is the product/spec alias.
        /// </summary>
        private static void WireLogicalParentKeys(
            int transactionId,
            AppConfigPackTransactionDto tx,
            List<AppConfigPackTableDto> tables)
        {
            using (var conn = OpenTenantConnection())
            {
                var units = LoadPackUnits(conn, transactionId);
                var root = units.FirstOrDefault(u => !u.ParentUnitId.HasValue && !u.IsSibling);
                if (root == null)
                    return;

                foreach (var field in tx.Fields ?? Enumerable.Empty<AppConfigPackFieldDto>())
                {
                    if (field?.IsPrimaryKey != true
                        || string.IsNullOrWhiteSpace(field.TableName)
                        || string.IsNullOrWhiteSpace(field.ColumnName))
                        continue;
                    SetFieldPrimaryKey(conn, transactionId, field.TableName.Trim(), field.ColumnName.Trim());
                }

                foreach (var unit in units)
                {
                    if (UnitHasFlag(conn, unit.UnitId, "IsPrimaryKey"))
                        continue;
                    int parentUnitId = ResolveParentUnitId(unit, root);
                    string parentPkCol = GetUnitPrimaryKeyColumn(conn, parentUnitId);
                    MarkHeuristicPrimaryKey(conn, unit.UnitId, parentPkCol);
                }

                foreach (var field in tx.Fields ?? Enumerable.Empty<AppConfigPackFieldDto>())
                {
                    if (field?.IsLinkToParentPrimaryKey != true
                        || string.IsNullOrWhiteSpace(field.TableName)
                        || string.IsNullOrWhiteSpace(field.ColumnName))
                        continue;

                    var unit = units.FirstOrDefault(u =>
                        string.Equals(u.TableName, field.TableName, StringComparison.OrdinalIgnoreCase));
                    if (unit == null)
                        continue;
                    int parentUnitId = ResolveParentUnitId(unit, root);
                    int? parentPkId = GetUnitPrimaryKeyFieldId(conn, parentUnitId);
                    if (!parentPkId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Parent primary key was not found for '{field.TableName}.{field.ColumnName}' (isLinkToParentPrimaryKey).");
                    }
                    SetFieldParentLink(conn, transactionId, field.TableName.Trim(), field.ColumnName.Trim(), parentPkId.Value);
                }

                var relationshipHints = BuildRelationshipHints(tables);
                foreach (var unit in units)
                {
                    if (unit.UnitId == root.UnitId)
                        continue;
                    if (UnitHasFlag(conn, unit.UnitId, "IsLinkToParentPrimaryKey"))
                        continue;

                    int parentUnitId = ResolveParentUnitId(unit, root);
                    int? parentPkId = GetUnitPrimaryKeyFieldId(conn, parentUnitId);
                    string parentPkCol = GetUnitPrimaryKeyColumn(conn, parentUnitId);
                    if (!parentPkId.HasValue || string.IsNullOrWhiteSpace(parentPkCol) || string.IsNullOrWhiteSpace(unit.TableName))
                        continue;

                    string childCol = FindLogicalChildLinkColumn(conn, unit, parentPkCol, parentUnitId, units, relationshipHints);
                    if (string.IsNullOrWhiteSpace(childCol))
                        continue;
                    SetFieldParentLink(conn, transactionId, unit.TableName, childCol, parentPkId.Value);
                }
            }
        }

        private static List<PackUnitRow> LoadPackUnits(SqlConnection conn, int transactionId)
        {
            var units = new List<PackUnitRow>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TransactionUnitID, DataBaseTableName, ParentTransactionUnitID, ISNULL(IsMasterSiblingUnit, 0)
FROM dbo.AppTransactionUnit
WHERE TransactionID = @TxId
ORDER BY TransactionUnitID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        units.Add(new PackUnitRow
                        {
                            UnitId = reader.GetInt32(0),
                            TableName = reader.IsDBNull(1) ? null : reader.GetString(1),
                            ParentUnitId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            IsSibling = Convert.ToBoolean(reader.GetValue(3))
                        });
                    }
                }
            }
            return units;
        }

        private static int ResolveParentUnitId(PackUnitRow unit, PackUnitRow root)
        {
            if (unit.IsSibling || !unit.ParentUnitId.HasValue)
                return root.UnitId;
            return unit.ParentUnitId.Value;
        }

        private static Dictionary<string, List<AppConfigPackRelationshipDto>> BuildRelationshipHints(
            List<AppConfigPackTableDto> tables)
        {
            var map = new Dictionary<string, List<AppConfigPackRelationshipDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables ?? Enumerable.Empty<AppConfigPackTableDto>())
            {
                if (table == null || string.IsNullOrWhiteSpace(table.Name) || table.Relationships == null)
                    continue;
                map[table.Name.Trim()] = table.Relationships
                    .Where(r => r != null && !string.IsNullOrWhiteSpace(r.ForeignKeyColumn))
                    .ToList();
            }
            return map;
        }

        private static bool UnitHasFlag(SqlConnection conn, int unitId, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"SELECT TOP 1 1 FROM dbo.AppTransactionField WHERE TransactionUnitID = @UnitId AND {columnName} = 1";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val != null && val != DBNull.Value;
            }
        }

        private static int? GetUnitPrimaryKeyFieldId(SqlConnection conn, int unitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 TransactionFieldID
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId AND IsPrimaryKey = 1
ORDER BY TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetUnitPrimaryKeyColumn(SqlConnection conn, int unitId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId AND IsPrimaryKey = 1
ORDER BY TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? null : Convert.ToString(val);
            }
        }

        private static void SetFieldPrimaryKey(SqlConnection conn, int transactionId, string tableName, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsPrimaryKey = 1,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                cmd.ExecuteNonQuery();
            }
        }

        private static void MarkHeuristicPrimaryKey(SqlConnection conn, int unitId, string parentPkColumn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsPrimaryKey = 1,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
WHERE f.TransactionFieldID = (
    SELECT TOP 1 TransactionFieldID
    FROM dbo.AppTransactionField
    WHERE TransactionUnitID = @UnitId
      AND RIGHT(DataBaseFieldName, 2) = 'Id'
      AND (@ParentPkCol IS NULL OR DataBaseFieldName <> @ParentPkCol)
    ORDER BY SortOrder, TransactionFieldID)";
                cmd.Parameters.AddWithValue("@UnitId", unitId);
                cmd.Parameters.AddWithValue("@ParentPkCol", (object)parentPkColumn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SetFieldParentLink(
            SqlConnection conn,
            int transactionId,
            string tableName,
            string columnName,
            int parentPkFieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE f SET
    IsLinkToParentPrimaryKey = 1,
    LinkToParentPrimaryKeyFieldID = @ParentPkFieldId,
    IsReadonly = 1,
    IsVisible = 0,
    AppModifiedDate = GETDATE()
FROM dbo.AppTransactionField f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
  AND u.DataBaseTableName = @TableName
  AND f.DataBaseFieldName = @ColumnName";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                cmd.Parameters.AddWithValue("@TableName", tableName);
                cmd.Parameters.AddWithValue("@ColumnName", columnName);
                cmd.Parameters.AddWithValue("@ParentPkFieldId", parentPkFieldId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string FindLogicalChildLinkColumn(
            SqlConnection conn,
            PackUnitRow unit,
            string parentPkColumn,
            int parentUnitId,
            List<PackUnitRow> units,
            Dictionary<string, List<AppConfigPackRelationshipDto>> relationshipHints)
        {
            var childColumns = new List<string>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DataBaseFieldName
FROM dbo.AppTransactionField
WHERE TransactionUnitID = @UnitId
ORDER BY SortOrder, TransactionFieldID";
                cmd.Parameters.AddWithValue("@UnitId", unit.UnitId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            childColumns.Add(reader.GetString(0));
                    }
                }
            }

            string sameName = childColumns.FirstOrDefault(c =>
                c.Equals(parentPkColumn, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(sameName))
                return sameName;

            var parent = units.FirstOrDefault(u => u.UnitId == parentUnitId);
            if (parent != null
                && relationshipHints.TryGetValue(unit.TableName ?? string.Empty, out var rels))
            {
                var rel = rels.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.TargetTable)
                    && r.TargetTable.Equals(parent.TableName, StringComparison.OrdinalIgnoreCase)
                    && childColumns.Any(c => c.Equals(r.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)));
                if (rel != null)
                    return childColumns.First(c => c.Equals(rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase));
            }

            string alias = AppTransactionBL.ResolveLogicalParentLinkAlias(parentPkColumn);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                string aliasMatch = childColumns.FirstOrDefault(c =>
                    c.Equals(alias, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(aliasMatch))
                    return aliasMatch;
            }

            return null;
        }
    }
}
