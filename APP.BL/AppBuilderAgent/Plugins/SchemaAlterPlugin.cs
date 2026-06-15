using System;
using System.Linq;
using System.Threading.Tasks;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// ALTER TABLE support — keeps the physical database schema and the AppAI
    /// platform data model in sync after initial creation.
    ///
    /// alter_table runs the DDL and, when a transactionId is supplied, also adds
    /// the corresponding AppTransactionField to the correct unit so the platform
    /// immediately reflects the new column.
    /// </summary>
    public class SchemaAlterPlugin
    {
        private readonly int?   _dataSourceId;
        private readonly string _schemaOwner;

        public SchemaAlterPlugin(int? dataSourceId = null, string schemaOwner = "dbo")
        {
            _dataSourceId = dataSourceId;
            _schemaOwner  = schemaOwner ?? "dbo";
        }

        [AgentFunction("alter_table",
            "Add, rename, or drop a column on an existing database table AND keep the AppAI data model in sync. " +
            "alterSql must be a single ALTER TABLE statement, e.g. " +
            "\"ALTER TABLE [dbo].[Order] ADD Notes NVARCHAR(500) NULL\". " +
            "If transactionId is supplied the tool also adds a matching AppTransactionField to the unit " +
            "so the form immediately shows the new field — pass newFieldJson to describe it. " +
            "Returns {IsSuccess, TableName, AlterSql, FieldAdded} on success.")]
        public string AlterTable(
            [AgentParam("The database table name to alter, e.g. 'Order' or 'dbo.Order'.", isRequired: true)]
            string tableName,
            [AgentParam("A single ALTER TABLE SQL statement. Must begin with ALTER TABLE. " +
                        "Use IF NOT EXISTS guards where possible.", isRequired: true)]
            string alterSql,
            [AgentParam("Transaction ID whose data model should be updated to match the schema change. " +
                        "Omit if you only want the DDL without platform sync.")]
            int? transactionId = null,
            [AgentParam("JSON describing the new platform field when a column is being ADDED. " +
                        "Keys: displayName (string), controlType (int: 2=Text,20=Numeric,7=Date,13=CheckBox,1=DDL,34=Time), " +
                        "entityId (int, for DDL fields), isNullable (bool), defaultValue (string). " +
                        "Omit when dropping or renaming (no new field is needed).")]
            string newFieldJson = null)
        {
            try
            {
                // ── Validate: must be an ALTER TABLE statement ──────────────────
                var trimmed = alterSql?.Trim() ?? "";
                if (!trimmed.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase))
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error     = "alterSql must begin with ALTER TABLE. Only ALTER TABLE statements are permitted."
                    });

                // ── Execute DDL ─────────────────────────────────────────────────
                var execResult = AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                {
                    SQL                  = trimmed,
                    DataSourceRegisterId = _dataSourceId,
                    RequireConfirmation  = false,
                    IsConfirmed          = true
                });

                if (!execResult.IsSuccess)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = execResult.Error });

                // Refresh cache so the new column appears in schema reads
                if (_dataSourceId.HasValue)
                {
                    try { AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(_dataSourceId); }
                    catch { /* non-fatal */ }
                }

                // ── Platform data model sync (optional) ─────────────────────────
                object fieldAdded = null;
                if (transactionId.HasValue && !string.IsNullOrWhiteSpace(newFieldJson))
                {
                    try
                    {
                        fieldAdded = AddPlatformField(transactionId.Value, tableName, newFieldJson);
                    }
                    catch (Exception ex)
                    {
                        // DDL succeeded; field sync failed — report but don't fail the whole operation
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess    = true,
                            TableName    = tableName,
                            AlterSql     = trimmed,
                            FieldAdded   = (object)null,
                            SyncWarning  = "DDL succeeded but platform field sync failed: " + ex.Message
                        }, Formatting.Indented);
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess  = true,
                    TableName  = tableName,
                    AlterSql   = trimmed,
                    FieldAdded = fieldAdded
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: add a new AppTransactionField to the unit whose table matches tableName
        // ─────────────────────────────────────────────────────────────────────

        private object AddPlatformField(int transactionId, string tableName, string newFieldJson)
        {
            JObject def;
            try { def = JObject.Parse(newFieldJson); }
            catch { throw new ArgumentException("newFieldJson is not valid JSON."); }

            // Fetch a fresh hierarchy
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (transExDto == null)
                throw new InvalidOperationException($"Transaction {transactionId} not found.");

            // Find the unit whose table matches
            var unit = (transExDto.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>())
                .FirstOrDefault(u =>
                    string.Equals(u.DataBaseTableName, tableName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.DataBaseTableName, tableName.Split('.').Last(), StringComparison.OrdinalIgnoreCase));

            if (unit == null)
                throw new InvalidOperationException(
                    $"No unit with table '{tableName}' found on transaction {transactionId}. " +
                    "Provide the exact table name as it appears in the transaction.");

            // Extract column name from ALTER TABLE … ADD <col> <type>
            // We store it as DataBaseFieldName
            var columnName = (string)def["columnName"]
                ?? throw new ArgumentException("newFieldJson must include 'columnName' (the DB column name).");

            var newField = AppTransactionBL.CreateNewAppTransactionFieldExDto();
            newField.TransactionUnitId  = Convert.ToInt32(unit.Id);
            newField.DisplayName        = (string)def["displayName"] ?? columnName;
            newField.DataBaseFieldName  = columnName;
            newField.ControlType        = def["controlType"] != null ? (int)def["controlType"] : 2; // default TextBox
            newField.IsAllowEmpty       = def["isNullable"]  != null ? (bool?)def["isNullable"] : true;
            newField.DefaultValue       = (string)def["defaultValue"];
            newField.IsModified         = true;

            var eid = def["entityId"] != null ? (int?)def["entityId"] : null;
            if (eid.HasValue)
            {
                newField.EntityId    = eid;
                newField.ControlType = 1; // DDL
            }

            var saveResult = AppTransactionBL.CreateNewAppTransactionFieldExDto(newField);
            bool ok = saveResult?.ValidationResult?.HasErrors == false;

            if (!ok)
            {
                var msgs = saveResult?.ValidationResult?.Items?.Select(i => i.Message).ToList();
                throw new InvalidOperationException(
                    msgs != null && msgs.Count > 0 ? string.Join("; ", msgs) : "Save failed");
            }

            return new
            {
                FieldId       = Convert.ToInt32(newField.Id),
                DisplayName   = newField.DisplayName,
                ColumnName    = newField.DataBaseFieldName,
                ControlType   = newField.ControlType,
                UnitId        = Convert.ToInt32(unit.Id),
                UnitTableName = unit.DataBaseTableName
            };
        }
    }
}
