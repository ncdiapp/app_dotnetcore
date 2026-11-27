using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Form Config tools mirroring Unit Editor: Add Temp Field, Add Table Existing Field,
    /// Remove Field, and Alter Table (ADD COLUMN) for the missing-column → existing-field chain.
    /// </summary>
    public static class FormConfigUnitFieldService
    {
        public static string CreateTempField(
            int transactionId,
            string displayName,
            int? unitId,
            string unitName,
            int? controlType,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(displayName))
                    return Fail("displayName is required.");

                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var unit = ResolveUnit(tx, unitId, unitName, out err);
                if (unit == null) return Fail(err);

                var existing = FindFieldByDisplayName(unit, displayName);
                if (existing != null)
                {
                    return Ok(new
                    {
                        ok = true,
                        alreadyExisted = true,
                        transactionId,
                        unitId = Convert.ToInt32(unit.Id),
                        fieldId = Convert.ToInt32(existing.Id),
                        displayName = existing.DisplayName,
                        isTempVariable = true,
                        formulaToken = TempFormulaToken(existing),
                        nextStep = "Field already exists. Call request_form_reload if the host UI is stale."
                    });
                }

                var proposed = new
                {
                    action = "createTempField",
                    transactionId,
                    unitId = Convert.ToInt32(unit.Id),
                    unitName = unit.UnitDisplayName,
                    table = unit.DataBaseTableName,
                    displayName = displayName.Trim(),
                    controlType = controlType ?? (int)EmAppControlType.TextBox,
                    isTempVariable = true
                };

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed
                    });
                }

                var field = AppTransactionBL.CreateNewAppTransactionFieldExDto();
                field.TransactionId = transactionId;
                field.TransactionUnitId = Convert.ToInt32(unit.Id);
                field.DisplayName = displayName.Trim();
                field.DataBaseFieldName = "";
                field.ControlType = controlType ?? (int)EmAppControlType.TextBox;
                field.DataType = (int)EmAppDataType.String;
                field.DisplayWidth = "100";
                field.Nbdecimal = 0;
                field.IsVisible = true;
                field.IsReadonly = false;
                field.IsGroupBy = false;
                field.IsGridUseAvailableEntitySource = false;
                field.IsNeedLog = false;
                field.IsLogicalDisplay = false;
                field.IsChangeTrigerNotification = false;
                field.IsAllowEmpty = true;
                field.IsConvertToUpperCase = false;
                field.IsTempVariable = true;
                field.IsStoreToExtendTable = false;
                field.IsPrimaryKey = false;
                field.IsLinkToParentPrimaryKey = false;
                field.SortOrder = NextSortOrder(unit);
                field.RowIdentityGuid = Guid.NewGuid();
                field.IsModified = true;

                var save = AppTransactionBL.CreateNewAppTransactionFieldExDto(field);
                if (save?.ValidationResult?.HasErrors == true)
                    return Fail(JoinErrors(save.ValidationResult));

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    ok = true,
                    created = true,
                    transactionId,
                    unitId = Convert.ToInt32(unit.Id),
                    fieldId = Convert.ToInt32(field.Id),
                    displayName = field.DisplayName,
                    dataBaseFieldName = field.DataBaseFieldName,
                    isTempVariable = true,
                    formulaToken = TempFormulaToken(field),
                    nextStep = "Call request_form_reload. For API staging: map response paths to these temp fields, then update_unit_formula into Notes."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string AddExistingTableFields(
            int transactionId,
            int? unitId,
            string unitName,
            string columnNamesJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var unit = ResolveUnit(tx, unitId, unitName, out err);
                if (unit == null) return Fail(err);
                if (string.IsNullOrWhiteSpace(unit.DataBaseTableName))
                    return Fail("Unit has no DataBaseTableName — cannot add existing table fields.");

                var names = ParseNameList(columnNamesJson);
                if (names.Count == 0)
                    return Fail("columnNamesJson must be a JSON array of column names, e.g. [\"LocationName\",\"SunriseTime\"].");

                var dsId = tx.DataSourceFrom;
                var schemaOwner = unit.SchemaOwner;
                var dbTable = AppMetaDataBL.GetOneDatabaseTableSchema(unit.DataBaseTableName, dsId, schemaOwner);
                if (dbTable == null)
                    return Fail($"Could not load schema for table '{unit.DataBaseTableName}'.");
                if (dsId.HasValue)
                    AppMetaDataBL.ApplyColumnNetTypeTags(dbTable, dsId.Value);

                var existingDb = new HashSet<string>(
                    (unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                        .Where(f => !string.IsNullOrWhiteSpace(f.DataBaseFieldName) && f.IsTempVariable != true)
                        .Select(f => f.DataBaseFieldName.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                var toAdd = new List<DatabaseColumn>();
                var skipped = new List<object>();
                var missing = new List<string>();
                foreach (var name in names)
                {
                    var col = dbTable.Columns?.FirstOrDefault(c =>
                        string.Equals(c?.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (col == null)
                    {
                        missing.Add(name);
                        continue;
                    }
                    if (AppTransactionBL.ShouldExcludeDatabaseColumnFromTransactionField(col))
                    {
                        skipped.Add(new { column = col.Name, reason = "excluded system/audit column" });
                        continue;
                    }
                    if (existingDb.Contains(col.Name))
                    {
                        skipped.Add(new { column = col.Name, reason = "already mapped on unit" });
                        continue;
                    }
                    toAdd.Add(col);
                }

                if (missing.Count > 0)
                {
                    return Fail(
                        "Column(s) not on table '" + unit.DataBaseTableName + "': " + string.Join(", ", missing) +
                        ". If the table is missing columns, call alter_table (ADD COLUMN) first, then add_existing_table_fields.");
                }

                var proposed = new
                {
                    action = "addExistingTableFields",
                    transactionId,
                    unitId = Convert.ToInt32(unit.Id),
                    table = unit.DataBaseTableName,
                    columnsToAdd = toAdd.Select(c => c.Name).ToList(),
                    skipped
                };

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed
                    });
                }

                if (toAdd.Count == 0)
                {
                    return Ok(new
                    {
                        ok = true,
                        added = Array.Empty<object>(),
                        skipped,
                        note = "Nothing to add.",
                        nextStep = "Call request_form_reload if needed."
                    });
                }

                var added = new List<object>();
                var sort = NextSortOrder(unit);
                foreach (var col in toAdd)
                {
                    var field = BuildFieldFromColumn(col, transactionId, Convert.ToInt32(unit.Id), sort);
                    sort += 10;
                    var save = AppTransactionBL.CreateNewAppTransactionFieldExDto(field);
                    if (save?.ValidationResult?.HasErrors == true)
                        return Fail("Failed adding column '" + col.Name + "': " + JoinErrors(save.ValidationResult));
                    added.Add(new
                    {
                        fieldId = Convert.ToInt32(field.Id),
                        displayName = field.DisplayName,
                        dbName = field.DataBaseFieldName,
                        controlType = field.ControlType,
                        formulaToken = $"[{field.DataBaseFieldName}_{field.Id}]"
                    });
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    ok = true,
                    added,
                    skipped,
                    nextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string RemoveTransactionField(
            int transactionId,
            int? fieldId,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                if (!fieldId.HasValue || fieldId.Value <= 0)
                    return Fail("fieldId is required.");

                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                var field = units
                    .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .FirstOrDefault(f => Convert.ToInt32(f.Id) == fieldId.Value);
                if (field == null)
                    return Fail($"fieldId {fieldId.Value} not found on transaction {transactionId}.");

                var proposed = new
                {
                    action = "removeTransactionField",
                    transactionId,
                    fieldId = fieldId.Value,
                    displayName = field.DisplayName,
                    dbName = field.DataBaseFieldName,
                    isTempVariable = field.IsTempVariable == true,
                    note = "Deletes the AppTransactionField definition only — does not DROP the database column."
                };

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed
                    });
                }

                DeleteFieldAndRefs(fieldId.Value);
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    ok = true,
                    deleted = true,
                    fieldId = fieldId.Value,
                    nextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        /// <summary>
        /// Form Config alter_table: DDL only (prefer ADD COLUMN). Do not pass newFieldJson —
        /// follow with add_existing_table_fields so control types match the UI converter path.
        /// </summary>
        public static string AlterTable(
            int transactionId,
            string tableName,
            string alterSql,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName))
                    return Fail("tableName is required.");
                if (string.IsNullOrWhiteSpace(alterSql))
                    return Fail("alterSql is required.");

                var trimmed = alterSql.Trim();
                if (!trimmed.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase))
                    return Fail("alterSql must begin with ALTER TABLE. Only ALTER TABLE is permitted.");

                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);

                // Prefer unit schema owner when table matches a unit.
                string schemaOwner = "dbo";
                var unit = EntityMapWiring.FlattenUnits(
                        tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>())
                    .FirstOrDefault(u =>
                        string.Equals(u.DataBaseTableName, tableName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(u.DataBaseTableName, tableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(unit?.SchemaOwner))
                    schemaOwner = unit.SchemaOwner;

                var proposed = new
                {
                    action = "alterTable",
                    transactionId,
                    tableName,
                    alterSql = trimmed,
                    dataSourceId = tx.DataSourceFrom,
                    schemaOwner,
                    note = "DDL only. After ADD COLUMN succeeds, call add_existing_table_fields with the new column name(s)."
                };

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed
                    });
                }

                var plugin = new SchemaAlterPlugin(tx.DataSourceFrom, schemaOwner);
                // DDL only — omit newFieldJson so fields are added via add_existing_table_fields.
                var raw = plugin.AlterTable(tableName, trimmed, transactionId: null, newFieldJson: null);
                JObject result;
                try { result = JObject.Parse(raw); }
                catch { return Fail("alter_table returned invalid JSON: " + raw); }

                if (result["IsSuccess"]?.Value<bool>() != true)
                    return Fail(result["Error"]?.ToString() ?? "alter_table failed.");

                if (tx.DataSourceFrom.HasValue)
                {
                    try { AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(tx.DataSourceFrom); }
                    catch { /* non-fatal */ }
                }

                return Ok(new
                {
                    ok = true,
                    altered = true,
                    tableName,
                    alterSql = trimmed,
                    nextStep = "Call add_existing_table_fields(unitId, columnNamesJson) for the new column(s), then request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static AppTransactionExDto LoadTx(int transactionId, out string error)
        {
            error = null;
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx == null)
            {
                error = $"Transaction {transactionId} not found.";
                return null;
            }
            return tx;
        }

        static AppTransactionUnitExDto ResolveUnit(
            AppTransactionExDto tx, int? unitId, string unitName, out string error)
        {
            error = null;
            var units = EntityMapWiring.FlattenUnits(
                tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            AppTransactionUnitExDto unit = null;
            if (unitId.HasValue && unitId.Value > 0)
                unit = units.FirstOrDefault(u => Convert.ToInt32(u.Id) == unitId.Value);
            else if (!string.IsNullOrWhiteSpace(unitName))
            {
                unit = units.FirstOrDefault(u =>
                    EntityMapWiring.UnitOrTableMatches(u.UnitDisplayName, unitName) ||
                    EntityMapWiring.UnitOrTableMatches(u.DataBaseTableName, unitName));
            }
            else
            {
                // Default: root master (no parent).
                unit = units.FirstOrDefault(u =>
                    u.ParentTransactionUnitId == null || u.ParentTransactionUnitId == 0);
            }

            if (unit == null)
            {
                error = "Unit not found. Pass unitId from get_current_transaction_config / find_fields (or omit for root unit).";
                return null;
            }
            return unit;
        }

        static AppTransactionFieldExDto FindFieldByDisplayName(AppTransactionUnitExDto unit, string displayName)
        {
            return (unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .FirstOrDefault(f =>
                    string.Equals(f.DisplayName, displayName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        static int NextSortOrder(AppTransactionUnitExDto unit)
        {
            var max = (unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .Select(f => f.SortOrder ?? 0)
                .DefaultIfEmpty(0)
                .Max();
            return ((int)Math.Ceiling(max / 10.0) * 10) + 10;
        }

        static List<string> ParseNameList(string columnNamesJson)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(columnNamesJson)) return list;
            try
            {
                var arr = JArray.Parse(columnNamesJson);
                foreach (var t in arr)
                {
                    var s = (t?.Type == JTokenType.String ? t.Value<string>() : t?.ToString())?.Trim();
                    if (!string.IsNullOrWhiteSpace(s) && !list.Contains(s, StringComparer.OrdinalIgnoreCase))
                        list.Add(s);
                }
            }
            catch
            {
                // also allow comma-separated
                foreach (var part in columnNamesJson.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var s = part.Trim().Trim('"', '\'');
                    if (!string.IsNullOrWhiteSpace(s) && !list.Contains(s, StringComparer.OrdinalIgnoreCase))
                        list.Add(s);
                }
            }
            return list;
        }

        static AppTransactionFieldExDto BuildFieldFromColumn(
            DatabaseColumn col, int transactionId, int unitId, int sortOrder)
        {
            var field = AppTransactionBL.CreateNewAppTransactionFieldExDto();
            field.TransactionId = transactionId;
            field.TransactionUnitId = unitId;
            field.DataBaseFieldName = col.Name;
            field.DisplayName = AppTransactionBL.ConvertDbNameToDisplayName(col.Name);
            field.SortOrder = sortOrder;
            field.RowIdentityGuid = Guid.NewGuid();
            field.DisplayWidth = "100";
            field.Nbdecimal = col.Scale ?? 0;
            field.IsPrimaryKey = col.IsPrimaryKey;
            field.IsVisible = !col.IsPrimaryKey;
            field.IsReadonly = col.IsPrimaryKey;
            field.IsAllowEmpty = true;
            field.IsTempVariable = false;
            field.IsStoreToExtendTable = false;
            field.IsLinkToParentPrimaryKey = false;
            field.DataRetrieveType = (int)EmAppCascadingSourceType.RelationalTable;
            field.ControlType = (int)EmAppControlType.TextBox;

            var dataType = AppTransactionBL.TryParseEmAppDataTypeFromColumnTag(col.Tag);
            if (dataType.HasValue)
            {
                field.DataType = dataType.Value;
                try
                {
                    field.ControlType = (int)ControlTypeValueConverter.ConvertDataTypeToDefaultControlType(field.DataType);
                }
                catch { /* keep TextBox */ }
            }

            if (col.Length.HasValue && col.Length.Value > 0)
            {
                field.MaxCharLegnth = col.Length;
                field.NeedValidator = true;
            }
            if ((col.IsPrimaryKey || !col.Nullable) && !col.IsAutoNumber)
            {
                field.IsAllowEmpty = false;
                field.NeedValidator = true;
            }
            field.IsModified = true;
            return field;
        }

        static void DeleteFieldAndRefs(int fieldId)
        {
            using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                adapter.StartTransaction(IsolationLevel.ReadCommitted, "DeleteTransactionField");
                try
                {
                    adapter.DeleteEntitiesDirectly(
                        typeof(AppTransactionUnitExtendFieldValueEntity),
                        new RelationPredicateBucket(AppTransactionUnitExtendFieldValueFields.UnitExtendFiledId == fieldId));
                    adapter.DeleteEntitiesDirectly(
                        typeof(AppFormLayoutItemEntity),
                        new RelationPredicateBucket(AppFormLayoutItemFields.TransactionFieldId == fieldId));
                    adapter.DeleteEntitiesDirectly(
                        typeof(AppTransactionSaveAsMappingEntity),
                        new RelationPredicateBucket(AppTransactionSaveAsMappingFields.SourceFiledId == fieldId));
                    adapter.DeleteEntitiesDirectly(
                        typeof(AppTransactionSaveAsMappingEntity),
                        new RelationPredicateBucket(AppTransactionSaveAsMappingFields.TargetFiledId == fieldId));
                    adapter.DeleteEntitiesDirectly(
                        typeof(AppTransactionFieldEntity),
                        new RelationPredicateBucket(AppTransactionFieldFields.TransactionFieldId == fieldId));
                    adapter.Commit();
                }
                catch
                {
                    adapter.Rollback();
                    throw;
                }
            }
        }

        static string TempFormulaToken(AppTransactionFieldExDto f)
        {
            var db = !string.IsNullOrWhiteSpace(f.DataBaseFieldName)
                ? f.DataBaseFieldName
                : (f.DisplayName ?? $"Field_{f.Id}");
            return $"[{db}]";
        }

        static string JoinErrors(ValidationResult vr) =>
            string.Join("; ", (vr?.Items ?? Enumerable.Empty<ValidationItem>()).Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m)));

        static string Ok(object payload) => JsonConvert.SerializeObject(payload, Formatting.Indented);

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });
    }
}
