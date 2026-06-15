using System;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Iterative-refinement tools: modify existing transactions/fields or delete transactions.
    /// These are the tools that make the platform "truly no-code" by allowing the LLM to
    /// respond to user requests like "make the Status field a dropdown" or "delete the Draft app".
    /// </summary>
    public class TransactionModifierPlugin
    {
        private readonly int? _dataSourceId;

        public TransactionModifierPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shared helper: locate a field by Id (preferred) or by unitName+fieldName
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves a field from a fresh hierarchy fetch.
        ///
        /// Lookup priority:
        ///   1. fieldId != null  → direct match by Id (unique — always use this when known).
        ///   2. fieldId == null  → match by fieldName (DisplayName or DB column),
        ///      optionally scoped to a specific unit via unitName to avoid ambiguity.
        ///
        /// fieldName alone is NOT unique across a hierarchy — the same column name
        /// (e.g. "StatusId") can appear in the master unit AND child units.
        /// Always prefer fieldId; provide unitName when only fieldName is known.
        /// </summary>
        private AppTransactionFieldExDto FindField(
            int     transactionId,
            int?    fieldId,
            string  fieldName,
            string  unitName,
            out string error)
        {
            error = null;

            // Force a fresh DB fetch — entities saved by create_application are OutOfSync in memory
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (transExDto == null)
            {
                error = $"Transaction {transactionId} not found";
                return null;
            }

            var units = (transExDto.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>()).ToList();

            // Build a unitId → display label map for error messages
            var unitLabel = units.ToDictionary(
                u => u.Id,
                u => !string.IsNullOrWhiteSpace(u.UnitDisplayName) ? u.UnitDisplayName : u.DataBaseTableName ?? u.Id.ToString());

            // ── 1. Lookup by fieldId (unique — no ambiguity) ──────────────────
            if (fieldId.HasValue)
            {
                var byId = units
                    .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .FirstOrDefault(f => Convert.ToInt32(f.Id) == fieldId.Value);

                if (byId == null)
                    error = $"Field Id={fieldId} not found on transaction {transactionId}.";

                return byId;
            }

            // ── 2. Lookup by name, optionally scoped to a unit ────────────────
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                error = "Either fieldId or fieldName must be provided.";
                return null;
            }

            // Narrow to the specified unit when unitName is given
            var candidateUnits = string.IsNullOrWhiteSpace(unitName)
                ? units
                : units.Where(u =>
                    string.Equals(u.UnitDisplayName,  unitName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.DataBaseTableName, unitName, StringComparison.OrdinalIgnoreCase));

            var candidateFields = candidateUnits
                .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .ToList();

            var matches = candidateFields
                .Where(f =>
                    string.Equals(f.DisplayName,       fieldName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.DataBaseFieldName, fieldName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 1)
                return matches[0];

            if (matches.Count > 1)
            {
                var detail = string.Join(", ", matches.Select(f =>
                    $"Id={f.Id} on unit '{(unitLabel.TryGetValue(f.TransactionUnitId, out var ul) ? ul : "?")}'"));
                error = $"Field '{fieldName}' is ambiguous on transaction {transactionId} ({matches.Count} matches: {detail}). " +
                        "Pass fieldId (preferred) or add unitName to disambiguate.";
                return null;
            }

            // Not found — help the agent pick the right Id next time
            var allFields = units
                .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .ToList();

            var hint = string.Join(", ", allFields.Select(f =>
            {
                var uLabel = unitLabel.TryGetValue(f.TransactionUnitId, out var l) ? l : "?";
                return $"{f.DisplayName} (Id={f.Id}, unit='{uLabel}')";
            }));

            error = string.IsNullOrWhiteSpace(unitName)
                ? $"Field '{fieldName}' not found on transaction {transactionId}. Available: {hint}"
                : $"Field '{fieldName}' not found in unit '{unitName}' on transaction {transactionId}. Available: {hint}";

            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        // update_transaction_field
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("update_transaction_field",
            "Modify properties of an existing field on a transaction. " +
            "Use this after create_application or when the user asks to rename a label, " +
            "change a field to a dropdown, set a default value, or link a field to an entity. " +
            "Pass only the properties you want to change in changesJson — omit unchanged ones. " +
            "Supported keys: displayName (string), controlType (int), entityId (int), defaultValue (string). " +
            "controlType values: 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time. " +
            "IMPORTANT: fieldId is unique and always preferred. fieldName alone is NOT unique across a hierarchy — " +
            "the same column name can exist in the master and child units. " +
            "Use get_transaction_details to find the exact fieldId before calling this tool. " +
            "Returns the updated field state on success.")]
        public string UpdateTransactionField(
            [AgentParam("ID of the transaction that owns the field.", isRequired: true)]
            int transactionId,
            [AgentParam("Unique numeric Id of the field to modify (preferred). Obtain from get_transaction_details. " +
                        "Use this instead of fieldName whenever possible — it is always unambiguous.")]
            int? fieldId,
            [AgentParam("Field name to match by DisplayName or DB column name (case-insensitive). " +
                        "Only used when fieldId is not known. Must be combined with unitName if the name is not unique.")]
            string fieldName = null,
            [AgentParam("Unit name or table name to scope the fieldName search. " +
                        "Required when fieldName appears in more than one unit of the transaction.")]
            string unitName = null,
            [AgentParam("JSON object with the properties to change, e.g. {\"displayName\":\"Order Status\",\"controlType\":1,\"entityId\":42}.", isRequired: true)]
            string changesJson = null)
        {
            try
            {
                var fieldSummary = FindField(transactionId, fieldId, fieldName, unitName, out var findError);
                if (fieldSummary == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = findError });

                // Load the full field DTO (fresh from DB — never touch a cached OutOfSync instance)
                var resolvedFieldId = Convert.ToInt32(fieldSummary.Id);
                var field   = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(resolvedFieldId);
                if (field == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = $"Could not load field Id={resolvedFieldId}" });

                // Parse and apply changes
                JObject changes;
                try { changes = JObject.Parse(changesJson); }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = "changesJson is not valid JSON: " + ex.Message
                    });
                }

                field.IsModified = true;

                if (changes["displayName"] != null)
                    field.DisplayName = changes["displayName"].Value<string>();

                if (changes["controlType"] != null)
                    field.ControlType = changes["controlType"].Value<int>();

                if (changes["entityId"] != null)
                {
                    var eid = changes["entityId"].Value<int?>();
                    field.EntityId = eid;
                }

                if (changes["defaultValue"] != null)
                    field.DefaultValue = changes["defaultValue"].Value<string>();

                var result = AppTransactionBL.SaveAppTransactionFieldExDto(field);

                bool success = result?.ValidationResult?.HasErrors == false;
                if (!success)
                {
                    var msgs = result?.ValidationResult?.Items?
                        .Select(i => i.Message).ToList();
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = msgs != null && msgs.Count > 0 ? string.Join("; ", msgs) : "Save failed"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess      = true,
                    TransactionId  = transactionId,
                    FieldId        = resolvedFieldId,
                    DisplayName    = field.DisplayName,
                    ControlType    = field.ControlType,
                    EntityId       = field.EntityId,
                    DefaultValue   = field.DefaultValue
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // set_field_entity — explicit entity-field linkage (avoids burying it in requirements text)
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("set_field_entity",
            "Link a transaction field to an Entity Data Source so it renders as a dropdown (DDL). " +
            "Use this AFTER create_application or create_transaction_from_table to wire up FK columns " +
            "that should show as dropdowns. " +
            "Automatically sets ControlType = 1 (DDL). " +
            "Pass entityId = null to remove an existing entity link and revert to a plain text/numeric field. " +
            "IMPORTANT: fieldId is unique and always preferred. Use get_transaction_details to find fieldId first.")]
        public string SetFieldEntity(
            [AgentParam("ID of the transaction that owns the field.", isRequired: true)]
            int transactionId,
            [AgentParam("Unique numeric Id of the field to link (preferred). Obtain from get_transaction_details.")]
            int? fieldId = null,
            [AgentParam("Field name to match by DisplayName or DB column name. Only used when fieldId is not known.")]
            string fieldName = null,
            [AgentParam("Unit name or table name to scope the fieldName search when the name is not unique across units.")]
            string unitName = null,
            [AgentParam("ID of the Entity Data Source to link (from list_entity_data_sources). Pass null to remove the link.")]
            int? entityId = null)
        {
            try
            {
                var fieldSummary = FindField(transactionId, fieldId, fieldName, unitName, out var findError);
                if (fieldSummary == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = findError });

                var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(fieldSummary.Id);
                if (field == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = $"Could not load field Id={fieldSummary.Id}" });

                field.IsModified  = true;
                field.EntityId    = entityId;
                // When linking → DDL control; when removing → revert to TextBox (2)
                field.ControlType = entityId.HasValue ? 1 : 2;

                var result  = AppTransactionBL.SaveAppTransactionFieldExDto(field);
                bool success = result?.ValidationResult?.HasErrors == false;

                if (!success)
                {
                    var msgs = result?.ValidationResult?.Items?.Select(i => i.Message).ToList();
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = msgs != null && msgs.Count > 0 ? string.Join("; ", msgs) : "Save failed"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess     = true,
                    TransactionId = transactionId,
                    FieldId       = Convert.ToInt32(fieldSummary.Id),
                    DisplayName   = field.DisplayName,
                    EntityId      = field.EntityId,
                    ControlType   = field.ControlType
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentFunction("delete_transaction",
            "Permanently delete a transaction and all its units, fields, forms, and search views. " +
            "Use this when the user asks to remove a screen/module from the application. " +
            "WARNING: This is irreversible. Always confirm with propose_plan before calling this. " +
            "Does NOT drop the underlying database table — only removes the AppAI configuration.")]
        public string DeleteTransaction(
            [AgentParam("ID of the transaction to delete.", isRequired: true)]
            int transactionId)
        {
            try
            {
                // Force a fresh DB fetch — avoids OutOfSync entities left by create_application
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                if (transExDto == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = $"Transaction {transactionId} not found" });

                var name = transExDto.TransactionName ?? transactionId.ToString();

                var result = AppTransactionBL.DeleteOneAppTransaction(transactionId);

                bool success = result?.ValidationResult?.HasErrors == false;
                if (!success)
                {
                    var msgs = result?.ValidationResult?.Items?
                        .Select(i => i.Message).ToList();
                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess = false,
                        Error = msgs != null && msgs.Count > 0 ? string.Join("; ", msgs) : "Delete failed"
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess     = true,
                    TransactionId = transactionId,
                    DeletedName   = name,
                    Note          = "Transaction deleted. The underlying database table was NOT dropped."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}
