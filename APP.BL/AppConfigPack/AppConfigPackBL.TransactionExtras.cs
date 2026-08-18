using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;

namespace APP.BL.AppConfigPack
{
    public static partial class AppConfigPackBL
    {
        private static readonly Regex RuntimeFormulaFieldRegex = new Regex(
            @"transactionfieldid_(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static void OverlayTransactionHeaderFlags(SqlConnection conn, int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx == null)
                return;
            if (!tx.IsShowSaveButton.HasValue
                && !tx.IsShowPrintButton.HasValue
                && !tx.IsShowCalculateButton.HasValue
                && !tx.IsReadOnly.HasValue)
            {
                return;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.AppTransaction
SET IsShowSaveButton = CASE WHEN @ShowSave IS NULL THEN IsShowSaveButton ELSE @ShowSave END,
    IsShowPrintButton = CASE WHEN @ShowPrint IS NULL THEN IsShowPrintButton ELSE @ShowPrint END,
    IsShowCalculateButton = CASE WHEN @ShowCalc IS NULL THEN IsShowCalculateButton ELSE @ShowCalc END,
    IsReadOnly = CASE WHEN @IsReadOnly IS NULL THEN IsReadOnly ELSE @IsReadOnly END,
    AppModifiedDate = GETDATE()
WHERE TransactionID = @Id";
                AddNullableBit(cmd, "@ShowSave", tx.IsShowSaveButton);
                AddNullableBit(cmd, "@ShowPrint", tx.IsShowPrintButton);
                AddNullableBit(cmd, "@ShowCalc", tx.IsShowCalculateButton);
                AddNullableBit(cmd, "@IsReadOnly", tx.IsReadOnly);
                cmd.Parameters.AddWithValue("@Id", transactionId);
                cmd.ExecuteNonQuery();
            }
        }

        internal static void ApplyTransactionRuntimeExtras(
            AppConfigPackDto pack,
            Dictionary<string, int> txIdsByIntegration,
            int tenantDataSourceId,
            int? saasApplicationId)
        {
            foreach (var tx in pack?.Transactions ?? Enumerable.Empty<AppConfigPackTransactionDto>())
            {
                if (tx == null || string.IsNullOrWhiteSpace(tx.IntegrationId))
                    continue;
                if (!txIdsByIntegration.TryGetValue(tx.IntegrationId.Trim(), out int transactionId))
                    continue;

                bool applied = tx.UnitFormulas != null
                    || tx.ConditionalActions != null
                    || tx.DataLoads != null
                    || tx.LinkedSearches != null;
                if (!applied)
                    continue;

                using (var conn = OpenTenantConnection())
                {
                    ApplyTransactionUnitFormulas(conn, transactionId, tx);
                    ApplyTransactionConditionalActions(conn, transactionId, tx);
                    ApplyTransactionDataLoads(conn, transactionId, tx, tenantDataSourceId, saasApplicationId);
                    ApplyTransactionLinkedSearches(conn, transactionId, tx, txIdsByIntegration);
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            }
        }

        internal static string PreviewExtrasDetail(AppConfigPackTransactionDto tx)
        {
            if (tx == null)
                return string.Empty;
            var parts = new List<string>();
            if (tx.DataLoads != null)
                parts.Add("dataLoads " + (tx.DataLoads.Count == 0 ? "clear" : "replace " + tx.DataLoads.Count));
            if (tx.UnitFormulas != null)
                parts.Add("formulas " + (tx.UnitFormulas.Count == 0 ? "clear" : "replace " + tx.UnitFormulas.Count));
            if (tx.ConditionalActions != null)
                parts.Add("conditional " + (tx.ConditionalActions.Count == 0 ? "clear" : "replace " + tx.ConditionalActions.Count));
            if (tx.LinkedSearches != null)
                parts.Add("linkedSearch " + (tx.LinkedSearches.Count == 0 ? "clear" : "replace " + tx.LinkedSearches.Count));
            return parts.Count == 0 ? string.Empty : "; " + string.Join("; ", parts);
        }

        private static List<AppConfigPackDataLoadDto> ExportTransactionDataLoads(SqlConnection conn, int transactionId)
        {
            var loads = new List<(int Id, int? DataSetId, int? UnitId, string Name, string Description, int? Order, bool? AutoOpen, bool? AutoCascade)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DataLoadID, DataSetID, TransactionUnitID, LoadName, Description, LoadOrder,
       IsAutoExcutedWhenOpenEditForm, IsAutoExecuteBeforeIntialCscading
FROM dbo.AppTransactionDataLoad
WHERE TransactionID = @TxId
ORDER BY ISNULL(LoadOrder, 9999), DataLoadID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        loads.Add((
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4),
                            reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            reader.IsDBNull(6) ? (bool?)null : reader.GetBoolean(6),
                            reader.IsDBNull(7) ? (bool?)null : reader.GetBoolean(7)));
                    }
                }
            }

            var list = new List<AppConfigPackDataLoadDto>();
            foreach (var row in loads)
            {
                var dto = new AppConfigPackDataLoadDto
                {
                    Name = row.Name,
                    Description = row.Description,
                    TableName = row.UnitId.HasValue ? GetUnitTableName(conn, row.UnitId.Value) : null,
                    LoadOrder = row.Order,
                    IsAutoExecutedWhenOpenEditForm = row.AutoOpen,
                    IsAutoExecuteBeforeInitialCascading = row.AutoCascade,
                    DataSet = row.DataSetId.HasValue ? ExportDataSetById(conn, row.DataSetId.Value) : null,
                    Mappings = ExportDataLoadMappings(conn, transactionId, row.Id)
                };
                list.Add(dto);
            }

            return list;
        }

        private static AppConfigPackDataSetDto ExportDataSetById(SqlConnection conn, int dataSetId)
        {
            string name = null;
            string query = null;
            string baseTable = null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Name, QueryText, BaseTableName FROM dbo.AppDataSet WHERE DataSetID = @Id";
                cmd.Parameters.AddWithValue("@Id", dataSetId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;
                    name = reader.IsDBNull(0) ? null : reader.GetString(0);
                    query = reader.IsDBNull(1) ? null : reader.GetString(1);
                    baseTable = reader.IsDBNull(2) ? null : reader.GetString(2);
                }
            }

            return new AppConfigPackDataSetDto
            {
                Name = name,
                QueryText = string.IsNullOrWhiteSpace(query) ? null : query,
                PrimaryTableName = baseTable
            };
        }

        private static List<AppConfigPackDataLoadMappingDto> ExportDataLoadMappings(SqlConnection conn, int transactionId, int dataLoadId)
        {
            var raw = new List<(int? FieldId, string Column, bool? IsCondition, string Where)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TransactionFieldID, DBColumnName, IsConditionMapping, WhereClause
FROM dbo.AppTranscationDataLoadFieldMapping
WHERE DataLoadID = @Id";
                cmd.Parameters.AddWithValue("@Id", dataLoadId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        raw.Add((
                            reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.IsDBNull(2) ? (bool?)null : reader.GetBoolean(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3)));
                    }
                }
            }

            var list = new List<AppConfigPackDataLoadMappingDto>();
            foreach (var row in raw)
            {
                var loc = row.FieldId.HasValue ? ResolveFieldTableColumn(conn, row.FieldId.Value) : (null, null);
                list.Add(new AppConfigPackDataLoadMappingDto
                {
                    TableName = loc.Item1,
                    ColumnName = loc.Item2,
                    DataSetColumn = row.Column,
                    IsConditionMapping = row.IsCondition,
                    WhereClause = RewriteRuntimeMixedTokensToPack(conn, transactionId, row.Where)
                });
            }
            return list.Count == 0 ? null : list;
        }

        private static List<AppConfigPackUnitFormulaDto> ExportTransactionUnitFormulas(SqlConnection conn, int transactionId)
        {
            var raw = new List<(string Table, string Name, string Expression, string Warning, int? Sort, int? FunctionType,
                int? OperationType, int? ApplyToScope, int? ConditionFieldId, bool? SwitchType, int? ChildUnitId,
                int? HighlightFieldId, int? StyleId, int? SearchViewId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT u.DataBaseTableName, f.FormulaName, f.FormulaExpression, f.WarningMessage, f.CaculationFlowSort,
       f.FunctionType, f.OperationType, f.ApplyToScope, f.ConditionFieldID, f.SwitchTrueFalseType,
       f.ChildTransactionUnitID, f.WarningHighlightTransFieldID, f.WarningHighlightStyleID, f.SearchViewId
FROM dbo.AppTransactionUnitFormula f
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = f.TransactionUnitID
WHERE u.TransactionID = @TxId
ORDER BY u.TransactionUnitID, ISNULL(f.CaculationFlowSort, 9999), f.TransactionUnitFormulaID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string expression = reader.IsDBNull(2) ? null : reader.GetString(2);
                        if (string.IsNullOrWhiteSpace(expression))
                            continue;
                        raw.Add((
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            expression,
                            reader.IsDBNull(3) ? null : reader.GetString(3),
                            reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                            reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            reader.IsDBNull(9) ? (bool?)null : reader.GetBoolean(9),
                            reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                            reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11),
                            reader.IsDBNull(12) ? (int?)null : reader.GetInt32(12),
                            reader.IsDBNull(13) ? (int?)null : reader.GetInt32(13)));
                    }
                }
            }

            var list = new List<AppConfigPackUnitFormulaDto>();
            foreach (var row in raw)
            {
                var condition = row.ConditionFieldId.HasValue && row.ConditionFieldId.Value > 0
                    ? ResolveFieldTableColumn(conn, row.ConditionFieldId.Value)
                    : (null, null);
                var highlight = row.HighlightFieldId.HasValue && row.HighlightFieldId.Value > 0
                    ? ResolveFieldTableColumn(conn, row.HighlightFieldId.Value)
                    : (null, null);
                list.Add(new AppConfigPackUnitFormulaDto
                {
                    TableName = row.Table,
                    FormulaName = row.Name,
                    FormulaExpression = RewriteRuntimeFormulaTokensToPack(conn, transactionId, row.Expression),
                    WarningMessage = row.Warning,
                    CalculationFlowSort = row.Sort,
                    FunctionType = row.FunctionType,
                    OperationType = row.OperationType,
                    ApplyToScope = row.ApplyToScope,
                    ConditionTableName = condition.Item1,
                    ConditionColumnName = condition.Item2,
                    SwitchTrueFalseType = row.SwitchType,
                    ChildTableName = row.ChildUnitId.HasValue ? GetUnitTableName(conn, row.ChildUnitId.Value) : null,
                    HighlightTableName = highlight.Item1,
                    HighlightColumnName = highlight.Item2,
                    WarningHighlightStyleId = row.StyleId,
                    SearchIntegrationId = row.SearchViewId.HasValue
                        ? GetSearchIntegrationIdBySearchViewId(conn, row.SearchViewId.Value)
                        : null
                });
            }
            return list;
        }

        private static List<AppConfigPackConditionalActionDto> ExportTransactionConditionalActions(SqlConnection conn, int transactionId)
        {
            var raw = new List<(string Name, int? ConditionUnitId, int? BoolFieldId, int? TriggerFieldId, string Formula,
                int? LockFieldId, int? LockFieldUnitId, bool? LockTx, int? LockUnitId, bool? SpecialLock, int? HideFieldId)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Name, ConditionUnitID, BooleanConditionFieldID, UITriggerTransactionFieldID, BooleanConditionFormula,
       LockingTransactionFieldID, LockingFieldUnitID, IsLockingTransaction, LockingTransactionUnitID,
       IsLockForSpecailEditPrivilege, NeedToHideTransactionFieldID
FROM dbo.AppConditionalAction
WHERE TransactionID = @TxId
ORDER BY ActionID";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        raw.Add((
                            reader.IsDBNull(0) ? null : reader.GetString(0),
                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4),
                            reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            reader.IsDBNull(7) ? (bool?)null : reader.GetBoolean(7),
                            reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            reader.IsDBNull(9) ? (bool?)null : reader.GetBoolean(9),
                            reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10)));
                    }
                }
            }

            var list = new List<AppConfigPackConditionalActionDto>();
            foreach (var row in raw)
            {
                var boolField = row.BoolFieldId.HasValue ? ResolveFieldTableColumn(conn, row.BoolFieldId.Value) : (null, null);
                var trigger = row.TriggerFieldId.HasValue ? ResolveFieldTableColumn(conn, row.TriggerFieldId.Value) : (null, null);
                var lockField = row.LockFieldId.HasValue ? ResolveFieldTableColumn(conn, row.LockFieldId.Value) : (null, null);
                var hide = row.HideFieldId.HasValue ? ResolveFieldTableColumn(conn, row.HideFieldId.Value) : (null, null);
                list.Add(new AppConfigPackConditionalActionDto
                {
                    Name = row.Name,
                    ConditionTableName = row.ConditionUnitId.HasValue ? GetUnitTableName(conn, row.ConditionUnitId.Value) : null,
                    BooleanConditionTableName = boolField.Item1,
                    BooleanConditionColumnName = boolField.Item2,
                    UiTriggerTableName = trigger.Item1,
                    UiTriggerColumnName = trigger.Item2,
                    BooleanConditionFormula = RewriteRuntimeFormulaTokensToPack(conn, transactionId, row.Formula),
                    LockingTableName = lockField.Item1,
                    LockingColumnName = lockField.Item2,
                    LockingFieldUnitTableName = row.LockFieldUnitId.HasValue ? GetUnitTableName(conn, row.LockFieldUnitId.Value) : null,
                    IsLockingTransaction = row.LockTx,
                    LockingTransactionUnitTableName = row.LockUnitId.HasValue ? GetUnitTableName(conn, row.LockUnitId.Value) : null,
                    IsLockForSpecialEditPrivilege = row.SpecialLock,
                    HideTableName = hide.Item1,
                    HideColumnName = hide.Item2
                });
            }
            return list;
        }

        private static List<AppConfigPackLinkedSearchDto> ExportTransactionLinkedSearches(SqlConnection conn, int transactionId)
        {
            var raw = new List<(int Id, int UnitId, int? SearchId, int SearchViewId, string Name, string Description, int? Action,
                int? UsageType, string GroupName, bool? Single, bool? Pre, bool? Post, string CallbackUri, int? TargetTxId,
                int? ConditionFieldId, int? CallbackCommandId, int? Sort, bool? Popup, int? Width, int? Height, string Icon, string Other)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT ls.TransactionUnitLinkedSearchId, ls.TransactionUnitId, ls.SearchId, ls.SearchViewId, ls.Name, ls.Description,
       ls.Action, ls.UsageType, ls.GroupName, ls.IsSingleSelectedRow, ls.IsNeedPreValidation, ls.IsNeedPostValidation,
       ls.CallbackRestResourceUri, ls.TargetTransactionID, ls.ConditionTransFieldID, ls.CallBackCommandID,
       ls.Sort, ls.IsPopup, ls.PopupWidth, ls.PopupHeight, ls.IconName, ls.OtherSettings
FROM dbo.AppTransactionUnitLinkedSearch ls
INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = ls.TransactionUnitId
WHERE u.TransactionID = @TxId
ORDER BY ISNULL(ls.Sort, 9999), ls.TransactionUnitLinkedSearchId";
                cmd.Parameters.AddWithValue("@TxId", transactionId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        raw.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            reader.GetInt32(3),
                            reader.IsDBNull(4) ? null : reader.GetString(4),
                            reader.IsDBNull(5) ? null : reader.GetString(5),
                            reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                            reader.IsDBNull(8) ? null : reader.GetString(8),
                            reader.IsDBNull(9) ? (bool?)null : reader.GetBoolean(9),
                            reader.IsDBNull(10) ? (bool?)null : reader.GetBoolean(10),
                            reader.IsDBNull(11) ? (bool?)null : reader.GetBoolean(11),
                            reader.IsDBNull(12) ? null : reader.GetString(12),
                            reader.IsDBNull(13) ? (int?)null : reader.GetInt32(13),
                            reader.IsDBNull(14) ? (int?)null : reader.GetInt32(14),
                            reader.IsDBNull(15) ? (int?)null : reader.GetInt32(15),
                            reader.IsDBNull(16) ? (int?)null : reader.GetInt32(16),
                            reader.IsDBNull(17) ? (bool?)null : reader.GetBoolean(17),
                            reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                            reader.IsDBNull(19) ? (int?)null : reader.GetInt32(19),
                            reader.IsDBNull(20) ? null : reader.GetString(20),
                            reader.IsDBNull(21) ? null : reader.GetString(21)));
                    }
                }
            }

            var list = new List<AppConfigPackLinkedSearchDto>();
            foreach (var row in raw)
            {
                int? searchId = row.SearchId;
                if (!searchId.HasValue)
                    searchId = GetSearchIdBySearchViewId(conn, row.SearchViewId);
                string searchIntegrationId = null;
                if (searchId.HasValue)
                    searchIntegrationId = GetOrCreateSearchIntegrationId(conn, searchId.Value, GetSearchName(conn, searchId.Value));

                var condition = row.ConditionFieldId.HasValue
                    ? ResolveFieldTableColumn(conn, row.ConditionFieldId.Value)
                    : (null, null);

                list.Add(new AppConfigPackLinkedSearchDto
                {
                    TableName = GetUnitTableName(conn, row.UnitId),
                    Name = row.Name,
                    Description = row.Description,
                    SearchIntegrationId = searchIntegrationId,
                    Action = row.Action,
                    UsageType = row.UsageType,
                    GroupName = row.GroupName,
                    IsSingleSelectedRow = row.Single,
                    IsNeedPreValidation = row.Pre,
                    IsNeedPostValidation = row.Post,
                    CallbackRestResourceUri = row.CallbackUri,
                    TargetTransactionIntegrationId = row.TargetTxId.HasValue
                        ? GetTransactionIntegrationId(conn, row.TargetTxId.Value)
                        : null,
                    ConditionTableName = condition.Item1,
                    ConditionColumnName = condition.Item2,
                    CallbackCommandName = row.CallbackCommandId.HasValue
                        ? GetCommandNameById(conn, row.CallbackCommandId.Value)
                        : null,
                    Sort = row.Sort,
                    IsPopup = row.Popup,
                    PopupWidth = row.Width,
                    PopupHeight = row.Height,
                    IconName = row.Icon,
                    OtherSettings = row.Other,
                    FieldMappings = ExportLinkedSearchFieldMappings(conn, row.Id),
                    ViewFieldMappings = ExportLinkedSearchViewMappings(conn, row.Id)
                });
            }
            return list;
        }

        private static List<AppConfigPackLinkedSearchFieldMappingDto> ExportLinkedSearchFieldMappings(SqlConnection conn, int linkedSearchId)
        {
            var raw = new List<(int FieldId, int SearchFieldId, int? TargetUnitId, string TargetColumn)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TransactionFieldId, SearchFieldId, TargetUnitID, TargetTransactionFieldDBName
FROM dbo.AppTransactionUnitSearchFieldMapping
WHERE TransactionUnitLinkedSearchId = @Id";
                cmd.Parameters.AddWithValue("@Id", linkedSearchId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        raw.Add((
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                            reader.IsDBNull(3) ? null : reader.GetString(3)));
                    }
                }
            }

            var list = new List<AppConfigPackLinkedSearchFieldMappingDto>();
            foreach (var row in raw)
            {
                var loc = ResolveFieldTableColumn(conn, row.FieldId);
                list.Add(new AppConfigPackLinkedSearchFieldMappingDto
                {
                    TableName = loc.Table,
                    ColumnName = loc.Column,
                    SearchFieldColumn = GetSearchFieldColumn(conn, row.SearchFieldId),
                    TargetTableName = row.TargetUnitId.HasValue ? GetUnitTableName(conn, row.TargetUnitId.Value) : null,
                    TargetColumnName = row.TargetColumn
                });
            }
            return list.Count == 0 ? null : list;
        }

        private static List<AppConfigPackLinkedSearchViewMappingDto> ExportLinkedSearchViewMappings(SqlConnection conn, int linkedSearchId)
        {
            var raw = new List<(int? FieldId, int? ViewFieldId, string ExternalCode, bool? Unique, int? TargetUnitId, string TargetColumn)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TransactionFieldId, SearchViewFieldId, ExternalAppFieldMappingCode, IsUnique, TargetUnitID, TargetTransactionFieldDBName
FROM dbo.AppTransactionUnitSearchViewFieldMapping
WHERE TransactionUnitLinkedSearchId = @Id";
                cmd.Parameters.AddWithValue("@Id", linkedSearchId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        raw.Add((
                            reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
                            reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            reader.IsDBNull(2) ? null : reader.GetString(2),
                            reader.IsDBNull(3) ? (bool?)null : reader.GetBoolean(3),
                            reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                            reader.IsDBNull(5) ? null : reader.GetString(5)));
                    }
                }
            }

            var list = new List<AppConfigPackLinkedSearchViewMappingDto>();
            foreach (var row in raw)
            {
                var loc = row.FieldId.HasValue ? ResolveFieldTableColumn(conn, row.FieldId.Value) : (null, null);
                list.Add(new AppConfigPackLinkedSearchViewMappingDto
                {
                    TableName = loc.Item1,
                    ColumnName = loc.Item2,
                    SearchViewFieldColumn = row.ViewFieldId.HasValue ? GetSearchViewFieldColumn(conn, row.ViewFieldId.Value) : null,
                    TargetTableName = row.TargetUnitId.HasValue ? GetUnitTableName(conn, row.TargetUnitId.Value) : null,
                    TargetColumnName = row.TargetColumn,
                    ExternalAppFieldMappingCode = row.ExternalCode,
                    IsUnique = row.Unique
                });
            }
            return list.Count == 0 ? null : list;
        }

        private static void ApplyTransactionUnitFormulas(SqlConnection conn, int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx.UnitFormulas == null)
                return;

            using (var del = conn.CreateCommand())
            {
                del.CommandText = @"
DELETE FROM dbo.AppTransactionUnitFormula
WHERE TransactionUnitID IN (SELECT TransactionUnitID FROM dbo.AppTransactionUnit WHERE TransactionID = @TxId)";
                del.Parameters.AddWithValue("@TxId", transactionId);
                del.ExecuteNonQuery();
            }

            int sort = 0;
            foreach (var formula in tx.UnitFormulas.Where(f => f != null && !string.IsNullOrWhiteSpace(f.FormulaExpression)))
            {
                if (string.IsNullOrWhiteSpace(formula.TableName))
                    throw new InvalidOperationException("unitFormulas[].tableName is required.");
                int? unitId = GetTransactionUnitId(conn, transactionId, formula.TableName);
                if (!unitId.HasValue)
                    throw new InvalidOperationException($"Unit formula table '{formula.TableName}' was not found.");

                sort++;
                int? conditionFieldId = ResolveOptionalFieldId(conn, transactionId, formula.ConditionTableName, formula.ConditionColumnName, "unit formula condition");
                int? highlightFieldId = ResolveOptionalFieldId(conn, transactionId, formula.HighlightTableName, formula.HighlightColumnName, "unit formula highlight");
                int? childUnitId = string.IsNullOrWhiteSpace(formula.ChildTableName)
                    ? null
                    : GetTransactionUnitId(conn, transactionId, formula.ChildTableName);
                int? searchViewId = null;
                if (!string.IsNullOrWhiteSpace(formula.SearchIntegrationId))
                {
                    int? searchId = GetSearchIdByIntegrationId(conn, formula.SearchIntegrationId);
                    if (!searchId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Unit formula search '{formula.SearchIntegrationId}' was not found.");
                    }
                    searchViewId = GetSearchViewIdBySearchId(conn, searchId.Value);
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppTransactionUnitFormula
    (TransactionUnitID, CaculationFlowSort, FormulaExpression, WarningMessage, FunctionType, OperationType,
     ConditionFieldID, SwitchTrueFalseType, ChildTransactionUnitID, WarningHighlightTransFieldID,
     WarningHighlightStyleID, FormulaName, ApplyToScope, SearchViewId, AppCreatedDate, AppModifiedDate)
VALUES
    (@UnitId, @Sort, @Expression, @Warning, @FunctionType, @OperationType,
     @ConditionFieldId, @Switch, @ChildUnitId, @HighlightFieldId,
     @StyleId, @Name, @Scope, @SearchViewId, GETDATE(), GETDATE())";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.Parameters.AddWithValue("@Sort", formula.CalculationFlowSort ?? sort);
                    cmd.Parameters.AddWithValue("@Expression", RewritePackFormulaTokensToRuntime(conn, transactionId, formula.FormulaExpression));
                    cmd.Parameters.AddWithValue("@Warning", (object)formula.WarningMessage ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FunctionType", (object)formula.FunctionType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@OperationType", (object)formula.OperationType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ConditionFieldId", (object)conditionFieldId ?? DBNull.Value);
                    AddNullableBit(cmd, "@Switch", formula.SwitchTrueFalseType);
                    cmd.Parameters.AddWithValue("@ChildUnitId", (object)childUnitId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HighlightFieldId", (object)highlightFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@StyleId", (object)formula.WarningHighlightStyleId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(formula.FormulaName) ? (object)DBNull.Value : TruncateName(formula.FormulaName, 500, formula.FormulaName));
                    cmd.Parameters.AddWithValue("@Scope", (object)formula.ApplyToScope ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SearchViewId", (object)searchViewId ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ApplyTransactionConditionalActions(SqlConnection conn, int transactionId, AppConfigPackTransactionDto tx)
        {
            if (tx.ConditionalActions == null)
                return;

            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM dbo.AppConditionalAction WHERE TransactionID = @TxId";
                del.Parameters.AddWithValue("@TxId", transactionId);
                del.ExecuteNonQuery();
            }

            foreach (var action in tx.ConditionalActions.Where(a => a != null))
            {
                int? conditionUnitId = string.IsNullOrWhiteSpace(action.ConditionTableName)
                    ? null
                    : GetTransactionUnitId(conn, transactionId, action.ConditionTableName);
                int? boolFieldId = ResolveOptionalFieldId(conn, transactionId, action.BooleanConditionTableName, action.BooleanConditionColumnName, "conditional boolean field");
                int? triggerFieldId = ResolveOptionalFieldId(conn, transactionId, action.UiTriggerTableName, action.UiTriggerColumnName, "conditional UI trigger");
                int? lockFieldId = ResolveOptionalFieldId(conn, transactionId, action.LockingTableName, action.LockingColumnName, "conditional locking field");
                int? hideFieldId = ResolveOptionalFieldId(conn, transactionId, action.HideTableName, action.HideColumnName, "conditional hide field");
                int? lockFieldUnitId = string.IsNullOrWhiteSpace(action.LockingFieldUnitTableName)
                    ? null
                    : GetTransactionUnitId(conn, transactionId, action.LockingFieldUnitTableName);
                int? lockUnitId = string.IsNullOrWhiteSpace(action.LockingTransactionUnitTableName)
                    ? null
                    : GetTransactionUnitId(conn, transactionId, action.LockingTransactionUnitTableName);

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppConditionalAction
    (Name, TransactionID, ConditionUnitID, BooleanConditionFieldID, UITriggerTransactionFieldID, BooleanConditionFormula,
     LockingTransactionFieldID, LockingFieldUnitID, IsLockingTransaction, LockingTransactionUnitID,
     IsLockForSpecailEditPrivilege, NeedToHideTransactionFieldID, AppCreatedDate, AppModifiedDate)
VALUES
    (@Name, @TxId, @ConditionUnitId, @BoolFieldId, @TriggerFieldId, @Formula,
     @LockFieldId, @LockFieldUnitId, @LockTx, @LockUnitId,
     @SpecialLock, @HideFieldId, GETDATE(), GETDATE())";
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(action.Name) ? (object)DBNull.Value : TruncateName(action.Name, 200, action.Name));
                    cmd.Parameters.AddWithValue("@TxId", transactionId);
                    cmd.Parameters.AddWithValue("@ConditionUnitId", (object)conditionUnitId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BoolFieldId", (object)boolFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TriggerFieldId", (object)triggerFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Formula",
                        string.IsNullOrWhiteSpace(action.BooleanConditionFormula)
                            ? (object)DBNull.Value
                            : RewritePackFormulaTokensToRuntime(conn, transactionId, action.BooleanConditionFormula));
                    cmd.Parameters.AddWithValue("@LockFieldId", (object)lockFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LockFieldUnitId", (object)lockFieldUnitId ?? DBNull.Value);
                    AddNullableBit(cmd, "@LockTx", action.IsLockingTransaction);
                    cmd.Parameters.AddWithValue("@LockUnitId", (object)lockUnitId ?? DBNull.Value);
                    AddNullableBit(cmd, "@SpecialLock", action.IsLockForSpecialEditPrivilege);
                    cmd.Parameters.AddWithValue("@HideFieldId", (object)hideFieldId ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void ApplyTransactionDataLoads(
            SqlConnection conn,
            int transactionId,
            AppConfigPackTransactionDto tx,
            int tenantDataSourceId,
            int? saasApplicationId)
        {
            if (tx.DataLoads == null)
                return;

            using (var unlink = conn.CreateCommand())
            {
                unlink.CommandText = @"
UPDATE dbo.AppProjectWorkFlowAction
SET DataLoadId = NULL
WHERE DataLoadId IN (SELECT DataLoadID FROM dbo.AppTransactionDataLoad WHERE TransactionID = @TxId)";
                unlink.Parameters.AddWithValue("@TxId", transactionId);
                unlink.ExecuteNonQuery();
            }
            using (var delMap = conn.CreateCommand())
            {
                delMap.CommandText = @"
DELETE FROM dbo.AppTranscationDataLoadFieldMapping
WHERE DataLoadID IN (SELECT DataLoadID FROM dbo.AppTransactionDataLoad WHERE TransactionID = @TxId)";
                delMap.Parameters.AddWithValue("@TxId", transactionId);
                delMap.ExecuteNonQuery();
            }
            using (var del = conn.CreateCommand())
            {
                del.CommandText = "DELETE FROM dbo.AppTransactionDataLoad WHERE TransactionID = @TxId";
                del.Parameters.AddWithValue("@TxId", transactionId);
                del.ExecuteNonQuery();
            }

            int order = 0;
            foreach (var load in tx.DataLoads.Where(l => l != null))
            {
                order++;
                string name = string.IsNullOrWhiteSpace(load.Name) ? "DataLoad" + order : load.Name.Trim();
                int? unitId = string.IsNullOrWhiteSpace(load.TableName)
                    ? null
                    : GetTransactionUnitId(conn, transactionId, load.TableName);
                if (!string.IsNullOrWhiteSpace(load.TableName) && !unitId.HasValue)
                    throw new InvalidOperationException($"Data load unit '{load.TableName}' was not found.");

                int? dataSetId = SaveDataLoadDataSet(load, tenantDataSourceId, saasApplicationId);
                int dataLoadId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppTransactionDataLoad
    (DataSetID, TransactionUnitID, TransactionID, LoadName, Description, LoadOrder,
     IsAutoExcutedWhenOpenEditForm, IsAutoExecuteBeforeIntialCscading, AppCreatedDate, AppModifiedDate)
VALUES
    (@DataSetId, @UnitId, @TxId, @Name, @Description, @Order,
     @AutoOpen, @AutoCascade, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@DataSetId", (object)dataSetId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UnitId", (object)unitId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@TxId", transactionId);
                    cmd.Parameters.AddWithValue("@Name", TruncateName(name, 50, name));
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(load.Description) ? (object)DBNull.Value : TruncateName(load.Description, 100, load.Description));
                    cmd.Parameters.AddWithValue("@Order", load.LoadOrder ?? order);
                    AddNullableBit(cmd, "@AutoOpen", load.IsAutoExecutedWhenOpenEditForm);
                    AddNullableBit(cmd, "@AutoCascade", load.IsAutoExecuteBeforeInitialCascading);
                    dataLoadId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var mapping in load.Mappings ?? Enumerable.Empty<AppConfigPackDataLoadMappingDto>())
                {
                    if (mapping == null)
                        continue;
                    int? fieldId = ResolveOptionalFieldId(conn, transactionId, mapping.TableName, mapping.ColumnName, "data load mapping");
                    using (var mapCmd = conn.CreateCommand())
                    {
                        mapCmd.CommandText = @"
INSERT INTO dbo.AppTranscationDataLoadFieldMapping
    (DataLoadID, TransactionFieldID, DBColumnName, IsConditionMapping, WhereClause, AppCreatedDate, AppModifiedDate)
VALUES
    (@DataLoadId, @FieldId, @Column, @IsCondition, @Where, GETDATE(), GETDATE())";
                        mapCmd.Parameters.AddWithValue("@DataLoadId", dataLoadId);
                        mapCmd.Parameters.AddWithValue("@FieldId", (object)fieldId ?? DBNull.Value);
                        mapCmd.Parameters.AddWithValue("@Column", string.IsNullOrWhiteSpace(mapping.DataSetColumn) ? (object)DBNull.Value : mapping.DataSetColumn.Trim());
                        AddNullableBit(mapCmd, "@IsCondition", mapping.IsConditionMapping);
                        mapCmd.Parameters.AddWithValue("@Where",
                            string.IsNullOrWhiteSpace(mapping.WhereClause)
                                ? (object)DBNull.Value
                                : RewritePackMixedTokensToRuntime(conn, transactionId, mapping.WhereClause));
                        mapCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static int? SaveDataLoadDataSet(AppConfigPackDataLoadDto load, int tenantDataSourceId, int? saasApplicationId)
        {
            string queryText = load.DataSet?.QueryText;
            if (string.IsNullOrWhiteSpace(queryText) && string.IsNullOrWhiteSpace(load.DataSet?.Name))
                return null;

            var dataSetDto = new AppDataSetExDto
            {
                Name = TruncateName(load.DataSet?.Name ?? load.Name ?? "DataLoad", 100, "DataLoad"),
                Description = load.Description ?? load.Name,
                QueryType = (int)EmAppDataServiceType.QueryText,
                QueryText = queryText,
                DataSourceFrom = tenantDataSourceId,
                SaasApplicationId = saasApplicationId,
                BaseTableName = load.DataSet?.PrimaryTableName
            };
            var saveResult = AppDataSetBL.SaveOneAppDataSetEntityDto(dataSetDto);
            if (!saveResult.IsSuccessfulWithResult)
            {
                throw new InvalidOperationException(
                    saveResult.ValidationResult?.Items?.FirstOrDefault()?.Message ?? "Failed to save data-load dataset.");
            }
            return Convert.ToInt32(saveResult.Object.Id);
        }

        private static void ApplyTransactionLinkedSearches(
            SqlConnection conn,
            int transactionId,
            AppConfigPackTransactionDto tx,
            Dictionary<string, int> txIdsByIntegration)
        {
            if (tx.LinkedSearches == null)
                return;

            using (var delMap = conn.CreateCommand())
            {
                delMap.CommandText = @"
DELETE FROM dbo.AppTransactionUnitSearchFieldMapping
WHERE TransactionUnitLinkedSearchId IN (
    SELECT ls.TransactionUnitLinkedSearchId
    FROM dbo.AppTransactionUnitLinkedSearch ls
    INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = ls.TransactionUnitId
    WHERE u.TransactionID = @TxId)";
                delMap.Parameters.AddWithValue("@TxId", transactionId);
                delMap.ExecuteNonQuery();
            }
            using (var delView = conn.CreateCommand())
            {
                delView.CommandText = @"
DELETE FROM dbo.AppTransactionUnitSearchViewFieldMapping
WHERE TransactionUnitLinkedSearchId IN (
    SELECT ls.TransactionUnitLinkedSearchId
    FROM dbo.AppTransactionUnitLinkedSearch ls
    INNER JOIN dbo.AppTransactionUnit u ON u.TransactionUnitID = ls.TransactionUnitId
    WHERE u.TransactionID = @TxId)";
                delView.Parameters.AddWithValue("@TxId", transactionId);
                delView.ExecuteNonQuery();
            }
            using (var del = conn.CreateCommand())
            {
                del.CommandText = @"
DELETE FROM dbo.AppTransactionUnitLinkedSearch
WHERE TransactionUnitId IN (SELECT TransactionUnitID FROM dbo.AppTransactionUnit WHERE TransactionID = @TxId)";
                del.Parameters.AddWithValue("@TxId", transactionId);
                del.ExecuteNonQuery();
            }

            foreach (var item in tx.LinkedSearches.Where(l => l != null))
            {
                if (string.IsNullOrWhiteSpace(item.TableName))
                    throw new InvalidOperationException("linkedSearches[].tableName is required.");
                int? unitId = GetTransactionUnitId(conn, transactionId, item.TableName);
                if (!unitId.HasValue)
                    throw new InvalidOperationException($"Linked search unit '{item.TableName}' was not found.");
                if (string.IsNullOrWhiteSpace(item.SearchIntegrationId))
                    throw new InvalidOperationException($"Linked search '{item.Name}' is missing searchIntegrationId.");

                int? searchId = GetSearchIdByIntegrationId(conn, item.SearchIntegrationId);
                if (!searchId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Linked search '{item.Name}' search '{item.SearchIntegrationId}' was not found.");
                }
                int? searchViewId = GetSearchViewIdBySearchId(conn, searchId.Value);
                if (!searchViewId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Linked search '{item.Name}' search '{item.SearchIntegrationId}' has no SearchView.");
                }

                int? targetTxId = null;
                if (!string.IsNullOrWhiteSpace(item.TargetTransactionIntegrationId))
                {
                    if (!txIdsByIntegration.TryGetValue(item.TargetTransactionIntegrationId.Trim(), out int mappedId))
                        targetTxId = GetTransactionIdByIntegrationId(conn, item.TargetTransactionIntegrationId);
                    else
                        targetTxId = mappedId;
                    if (!targetTxId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Linked search '{item.Name}' target transaction '{item.TargetTransactionIntegrationId}' was not found.");
                    }
                }

                int? conditionFieldId = ResolveOptionalFieldId(conn, transactionId, item.ConditionTableName, item.ConditionColumnName, "linked search condition");
                int? callbackCommandId = string.IsNullOrWhiteSpace(item.CallbackCommandName)
                    ? null
                    : GetCommandIdByName(conn, transactionId, item.CallbackCommandName);

                int linkedSearchId;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.AppTransactionUnitLinkedSearch
    (TransactionUnitId, SearchId, SearchViewId, Name, Description, Action, UsageType, GroupName,
     IsSingleSelectedRow, IsNeedPreValidation, IsNeedPostValidation, CallbackRestResourceUri,
     TargetTransactionID, ConditionTransFieldID, CallBackCommandID, Sort, IsPopup, PopupWidth, PopupHeight,
     IconName, OtherSettings, AppCreatedDate, AppModifiedDate)
VALUES
    (@UnitId, @SearchId, @SearchViewId, @Name, @Description, @Action, @UsageType, @GroupName,
     @Single, @Pre, @Post, @CallbackUri,
     @TargetTxId, @ConditionFieldId, @CallbackCommandId, @Sort, @Popup, @Width, @Height,
     @Icon, @Other, GETDATE(), GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@UnitId", unitId.Value);
                    cmd.Parameters.AddWithValue("@SearchId", searchId.Value);
                    cmd.Parameters.AddWithValue("@SearchViewId", searchViewId.Value);
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(item.Name) ? (object)DBNull.Value : TruncateName(item.Name, 100, item.Name));
                    cmd.Parameters.AddWithValue("@Description", string.IsNullOrWhiteSpace(item.Description) ? (object)DBNull.Value : TruncateName(item.Description, 500, item.Description));
                    cmd.Parameters.AddWithValue("@Action", (object)item.Action ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UsageType", (object)item.UsageType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GroupName", string.IsNullOrWhiteSpace(item.GroupName) ? (object)DBNull.Value : item.GroupName.Trim());
                    AddNullableBit(cmd, "@Single", item.IsSingleSelectedRow);
                    AddNullableBit(cmd, "@Pre", item.IsNeedPreValidation);
                    AddNullableBit(cmd, "@Post", item.IsNeedPostValidation);
                    cmd.Parameters.AddWithValue("@CallbackUri", string.IsNullOrWhiteSpace(item.CallbackRestResourceUri) ? (object)DBNull.Value : item.CallbackRestResourceUri.Trim());
                    cmd.Parameters.AddWithValue("@TargetTxId", (object)targetTxId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ConditionFieldId", (object)conditionFieldId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CallbackCommandId", (object)callbackCommandId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Sort", (object)item.Sort ?? DBNull.Value);
                    AddNullableBit(cmd, "@Popup", item.IsPopup);
                    cmd.Parameters.AddWithValue("@Width", (object)item.PopupWidth ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Height", (object)item.PopupHeight ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Icon", string.IsNullOrWhiteSpace(item.IconName) ? (object)DBNull.Value : item.IconName.Trim());
                    cmd.Parameters.AddWithValue("@Other", string.IsNullOrWhiteSpace(item.OtherSettings) ? (object)DBNull.Value : item.OtherSettings);
                    linkedSearchId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                foreach (var mapping in item.FieldMappings ?? Enumerable.Empty<AppConfigPackLinkedSearchFieldMappingDto>())
                {
                    if (mapping == null || string.IsNullOrWhiteSpace(mapping.ColumnName))
                        continue;
                    int? fieldId = GetTransactionFieldId(conn, transactionId, mapping.TableName ?? item.TableName, mapping.ColumnName);
                    if (!fieldId.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"Linked search field mapping '{mapping.TableName}.{mapping.ColumnName}' was not found.");
                    }
                    int? searchFieldId = string.IsNullOrWhiteSpace(mapping.SearchFieldColumn)
                        ? 0
                        : GetSearchFieldId(conn, searchId.Value, mapping.SearchFieldColumn);
                    int? targetUnitId = string.IsNullOrWhiteSpace(mapping.TargetTableName)
                        ? null
                        : GetTransactionUnitId(conn, transactionId, mapping.TargetTableName);
                    using (var mapCmd = conn.CreateCommand())
                    {
                        mapCmd.CommandText = @"
INSERT INTO dbo.AppTransactionUnitSearchFieldMapping
    (TransactionUnitLinkedSearchId, TransactionFieldId, SearchFieldId, TargetUnitID, TargetTransactionFieldDBName,
     AppCreatedDate, AppModifiedDate)
VALUES
    (@LinkedId, @FieldId, @SearchFieldId, @TargetUnitId, @TargetColumn, GETDATE(), GETDATE())";
                        mapCmd.Parameters.AddWithValue("@LinkedId", linkedSearchId);
                        mapCmd.Parameters.AddWithValue("@FieldId", fieldId.Value);
                        mapCmd.Parameters.AddWithValue("@SearchFieldId", searchFieldId ?? 0);
                        mapCmd.Parameters.AddWithValue("@TargetUnitId", (object)targetUnitId ?? DBNull.Value);
                        mapCmd.Parameters.AddWithValue("@TargetColumn", string.IsNullOrWhiteSpace(mapping.TargetColumnName) ? (object)DBNull.Value : mapping.TargetColumnName.Trim());
                        mapCmd.ExecuteNonQuery();
                    }
                }

                foreach (var mapping in item.ViewFieldMappings ?? Enumerable.Empty<AppConfigPackLinkedSearchViewMappingDto>())
                {
                    if (mapping == null)
                        continue;
                    int? fieldId = ResolveOptionalFieldId(conn, transactionId, mapping.TableName ?? item.TableName, mapping.ColumnName, "linked search view mapping");
                    int? viewFieldId = string.IsNullOrWhiteSpace(mapping.SearchViewFieldColumn)
                        ? null
                        : GetSearchViewFieldId(conn, searchViewId.Value, mapping.SearchViewFieldColumn);
                    int? targetUnitId = string.IsNullOrWhiteSpace(mapping.TargetTableName)
                        ? null
                        : GetTransactionUnitId(conn, transactionId, mapping.TargetTableName);
                    using (var mapCmd = conn.CreateCommand())
                    {
                        mapCmd.CommandText = @"
INSERT INTO dbo.AppTransactionUnitSearchViewFieldMapping
    (TransactionUnitLinkedSearchId, TransactionFieldId, SearchViewFieldId, ExternalAppFieldMappingCode, IsUnique,
     TargetUnitID, TargetTransactionFieldDBName, AppCreatedDate, AppModifiedDate)
VALUES
    (@LinkedId, @FieldId, @ViewFieldId, @External, @Unique, @TargetUnitId, @TargetColumn, GETDATE(), GETDATE())";
                        mapCmd.Parameters.AddWithValue("@LinkedId", linkedSearchId);
                        mapCmd.Parameters.AddWithValue("@FieldId", (object)fieldId ?? DBNull.Value);
                        mapCmd.Parameters.AddWithValue("@ViewFieldId", (object)viewFieldId ?? DBNull.Value);
                        mapCmd.Parameters.AddWithValue("@External", string.IsNullOrWhiteSpace(mapping.ExternalAppFieldMappingCode) ? (object)DBNull.Value : mapping.ExternalAppFieldMappingCode.Trim());
                        AddNullableBit(mapCmd, "@Unique", mapping.IsUnique);
                        mapCmd.Parameters.AddWithValue("@TargetUnitId", (object)targetUnitId ?? DBNull.Value);
                        mapCmd.Parameters.AddWithValue("@TargetColumn", string.IsNullOrWhiteSpace(mapping.TargetColumnName) ? (object)DBNull.Value : mapping.TargetColumnName.Trim());
                        mapCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private static string RewriteRuntimeFormulaTokensToPack(SqlConnection conn, int transactionId, string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return expression;
            var fields = transactionId > 0 ? LoadTransactionFieldLookup(conn, transactionId) : new Dictionary<int, (string Table, string Column)>();
            expression = RuntimeFormulaFieldRegex.Replace(expression, match =>
            {
                int fieldId = int.Parse(match.Groups[1].Value);
                if (!fields.TryGetValue(fieldId, out var loc) || string.IsNullOrWhiteSpace(loc.Table) || string.IsNullOrWhiteSpace(loc.Column))
                {
                    var resolved = ResolveFieldTableColumn(conn, fieldId);
                    if (string.IsNullOrWhiteSpace(resolved.Table) || string.IsNullOrWhiteSpace(resolved.Column))
                        return match.Value;
                    loc = resolved;
                }
                return $"[TF:{loc.Table}.{loc.Column}]";
            });
            if (transactionId > 0)
                expression = RewriteRuntimeSqlTokensToPack(conn, transactionId, expression);
            return expression;
        }

        private static string RewritePackFormulaTokensToRuntime(SqlConnection conn, int transactionId, string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return expression;
            expression = PackFieldTokenRegex.Replace(expression, match =>
            {
                ParseTableColumnSpec(match.Groups[1].Value, null, out string table, out string column);
                int? fieldId = string.IsNullOrWhiteSpace(table)
                    ? GetTransactionFieldIdByColumn(conn, transactionId, column)
                    : GetTransactionFieldId(conn, transactionId, table, column);
                if (!fieldId.HasValue)
                    throw new InvalidOperationException($"Formula token '{match.Value}' did not match a transaction field.");
                return "transactionfieldid_" + fieldId.Value;
            });
            return expression;
        }

        private static string RewriteRuntimeMixedTokensToPack(SqlConnection conn, int transactionId, string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            value = RewriteRuntimeFormulaTokensToPack(conn, transactionId, value);
            return RewriteRuntimeSqlTokensToPack(conn, transactionId, value);
        }

        private static string RewritePackMixedTokensToRuntime(SqlConnection conn, int transactionId, string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            value = RewritePackFormulaTokensToRuntime(conn, transactionId, value);
            return RewritePackSqlTokensToRuntime(conn, transactionId, value);
        }

        private static int? ResolveOptionalFieldId(
            SqlConnection conn,
            int transactionId,
            string tableName,
            string columnName,
            string context)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return null;
            int? fieldId = GetTransactionFieldId(conn, transactionId, tableName, columnName);
            if (!fieldId.HasValue)
            {
                throw new InvalidOperationException(
                    $"{context} '{tableName}.{columnName}' was not found.");
            }
            return fieldId;
        }

        private static int? GetSearchIdBySearchViewId(SqlConnection conn, int searchViewId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 SearchID FROM dbo.AppSearch WHERE SearchViewID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchViewId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static int? GetSearchViewIdBySearchId(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SearchViewID FROM dbo.AppSearch WHERE SearchID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchId);
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }

        private static string GetSearchIntegrationIdBySearchViewId(SqlConnection conn, int searchViewId)
        {
            int? searchId = GetSearchIdBySearchViewId(conn, searchViewId);
            if (!searchId.HasValue)
                return null;
            return GetOrCreateSearchIntegrationId(conn, searchId.Value, GetSearchName(conn, searchId.Value));
        }

        private static string GetSearchName(SqlConnection conn, int searchId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Name FROM dbo.AppSearch WHERE SearchID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchId);
                return cmd.ExecuteScalar() as string;
            }
        }

        private static string GetSearchFieldColumn(SqlConnection conn, int searchFieldId)
        {
            if (searchFieldId <= 0)
                return null;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SysTableFiledPath FROM dbo.AppSearchField WHERE SearchFieldID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchFieldId);
                var val = cmd.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }

        private static string GetSearchViewFieldColumn(SqlConnection conn, int searchViewFieldId)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SysTableFiledPath FROM dbo.AppSearchViewField WHERE SearchViewFieldID = @Id";
                cmd.Parameters.AddWithValue("@Id", searchViewFieldId);
                var val = cmd.ExecuteScalar() as string;
                return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
            }
        }

        private static int? GetSearchFieldId(SqlConnection conn, int searchId, string columnName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 SearchFieldID
FROM dbo.AppSearchField
WHERE SearchID = @SearchId AND SysTableFiledPath = @ColumnName";
                cmd.Parameters.AddWithValue("@SearchId", searchId);
                cmd.Parameters.AddWithValue("@ColumnName", columnName.Trim());
                var val = cmd.ExecuteScalar();
                return val == null || val == DBNull.Value ? (int?)null : Convert.ToInt32(val);
            }
        }
    }
}
