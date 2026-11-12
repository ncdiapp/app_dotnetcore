using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using App.BL.AIAgent.GenericAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using APP.Framework.Plugin;

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

            // Nested master-detail children live under unit.Children — flatten for lookup.
            var units = EntityMapWiring.FlattenUnits(
                transExDto.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());

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

            // Narrow to the specified unit when unitName is given (table/display; ignore spaces/_/-)
            var candidateUnits = string.IsNullOrWhiteSpace(unitName)
                ? units
                : units.Where(u =>
                    EntityMapWiring.UnitOrTableMatches(u.UnitDisplayName, unitName) ||
                    EntityMapWiring.UnitOrTableMatches(u.DataBaseTableName, unitName)).ToList();

            // If unitName was given but matched nothing, fall back to all units (still prefer fieldId).
            if (!string.IsNullOrWhiteSpace(unitName) && candidateUnits.Count == 0)
                candidateUnits = units;

            var candidateFields = candidateUnits
                .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .ToList();

            var matches = candidateFields
                .Where(f =>
                    string.Equals(f.DisplayName, fieldName, StringComparison.OrdinalIgnoreCase) ||
                    EntityMapWiring.ColumnNameMatches(f.DataBaseFieldName, fieldName))
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

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("update_transaction_field",
            "Modify properties of an existing field on a transaction. " +
            "Use this after create_application or when the user asks to rename a label, " +
            "change a field to a dropdown, set a default value, or link a field to an entity. " +
            "Pass only the properties you want to change in changesJson — omit unchanged ones. " +
            "Supported keys: displayName (string), controlType (int or name), entityId (int), defaultValue (string). " +
            "controlType EmAppControlType (NO value 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label, " +
            "13=CheckBox, 20=Numeric, 26=RichText, 27=DateTimeDetail, 34=Time, 37=ImageBinary, 38=AutoComplete, " +
            "48=SearchAbleDDL, 50=ExternalImageUrl, 53=MultiSelectDDL. Pass 4 or \"Memo\" for multiline text — never invent ids. " +
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
                // SaveAppTransactionFieldExDto refreshes cache via field.TransactionId — ensure it is set.
                if (!field.TransactionId.HasValue || field.TransactionId.Value <= 0)
                    field.TransactionId = transactionId;

                if (changes["displayName"] != null)
                    field.DisplayName = changes["displayName"].Value<string>();

                if (changes["controlType"] != null)
                {
                    if (!TryResolveControlType(changes["controlType"], out var resolvedControlType, out var ctError))
                        return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ctError });
                    field.ControlType = resolvedControlType;
                }

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

                // Same as Form Configuration / Transaction Editor save: refresh hierarchy cache
                // with the known transactionId so runtime TransactionForm sees new DisplayName etc.
                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);

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

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("set_field_entity",
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
                if (!field.TransactionId.HasValue || field.TransactionId.Value <= 0)
                    field.TransactionId = transactionId;
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

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);

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

        [AgentTool("get_unit_formulas",
            "List Calculation Validation Flow formulas for a unit (Assignment/Warning/Error/Aggregate/Subscribe). " +
            "defaultValue is NOT a formula.")]
        public string GetUnitFormulas(
            [AgentParam("Transaction id.", isRequired: true)] int transactionId,
            [AgentParam("Unit id (preferred).")] int? unitId = null,
            [AgentParam("Unit display/table name fallback.")] string unitName = null)
            => FormConfigFormulaLayoutService.GetUnitFormulas(transactionId, unitId, unitName);

        [AgentTool("update_unit_formula",
            "Add/update/delete unit formulas. operationType: 1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe. " +
            "Display-name expressions are resolved to [DbName_Id]; ambiguous → needsDisambiguation. " +
            "Preview first, ask_user, then confirmed=true. Optional dryRun.")]
        public string UpdateUnitFormula(
            [AgentParam("Transaction id.", isRequired: true)] int transactionId,
            [AgentParam("add | update | delete", isRequired: true)] string action = null,
            [AgentParam("Unit id.")] int? unitId = null,
            [AgentParam("Unit name.")] string unitName = null,
            [AgentParam("Formula id for update/delete.")] int? formulaId = null,
            [AgentParam("1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe")] int? operationType = null,
            [AgentParam("Expression (display names OK).")] string expression = null,
            [AgentParam("Assign-to field id.")] int? assignToFieldId = null,
            [AgentParam("Assign-to field name.")] string assignToFieldName = null,
            [AgentParam("Aggregate source / Subscribe parent field id.")] int? sourceFieldId = null,
            [AgentParam("Source field name.")] string sourceFieldName = null,
            [AgentParam("1=SUM 2=AVG 3=MIN 4=MAX 5=COUNT")] int? aggregationType = null,
            [AgentParam("Warning/Error message.")] string warningMessage = null,
            [AgentParam("Highlight field id.")] int? warningHighlightFieldId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("Preview only.")] bool? dryRun = null)
            => FormConfigFormulaLayoutService.UpdateUnitFormula(
                transactionId, action, unitId, unitName, formulaId, operationType, expression,
                assignToFieldId, assignToFieldName, sourceFieldId, sourceFieldName,
                aggregationType, warningMessage, warningHighlightFieldId, confirmed, dryRun);

        [AgentTool("get_form_layout",
            "Inspect Flex form layout tree for a transaction (FormId).")]
        public string GetFormLayout(
            [AgentParam("Transaction id.", isRequired: true)] int transactionId)
            => FormConfigFormulaLayoutService.GetFormLayout(transactionId);

        [AgentTool("design_form_layout",
            "Generate (N-col default / full rebuild) or patch Flex layout. " +
            "Overwrite requires confirmed=true; optional backup/dryRun. " +
            "patch ops: setHeight, moveField, setContainer, setColumns.")]
        public string DesignFormLayout(
            [AgentParam("Transaction id.", isRequired: true)] int transactionId,
            [AgentParam("generate | patch", isRequired: true)] string mode = null,
            [AgentParam("Column count for generate.")] int? nbColumns = null,
            [AgentParam("JSON array of patch ops.")] string patchesJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("Preview only.")] bool? dryRun = null,
            [AgentParam("Include layout backup summary.")] bool? backup = null)
            => FormConfigFormulaLayoutService.DesignFormLayout(
                transactionId, mode, nbColumns, patchesJson, confirmed, dryRun, backup);

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("delete_transaction",
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

        /// <summary>
        /// EmAppControlType has gaps (e.g. no 3 between TextBox=2 and Memo=4). Agents often invent 3 for Memo.
        /// Accept int or name; reject undefined values with an explicit allow-list message.
        /// </summary>
        private static readonly Dictionary<string, int> ControlTypeByName =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["DDL"] = (int)EmAppControlType.DDL,
                ["Dropdown"] = (int)EmAppControlType.DDL,
                ["DropdownList"] = (int)EmAppControlType.DDL,
                ["TextBox"] = (int)EmAppControlType.TextBox,
                ["Text"] = (int)EmAppControlType.TextBox,
                ["Memo"] = (int)EmAppControlType.Memo,
                ["TextArea"] = (int)EmAppControlType.Memo,
                ["Multiline"] = (int)EmAppControlType.Memo,
                ["Image"] = (int)EmAppControlType.Image,
                ["Date"] = (int)EmAppControlType.Date,
                ["File"] = (int)EmAppControlType.File,
                ["Label"] = (int)EmAppControlType.Label,
                ["CheckBox"] = (int)EmAppControlType.CheckBox,
                ["Checkbox"] = (int)EmAppControlType.CheckBox,
                ["Numeric"] = (int)EmAppControlType.Numeric,
                ["Number"] = (int)EmAppControlType.Numeric,
                ["RichText"] = (int)EmAppControlType.RichText,
                ["DateTimeDetail"] = (int)EmAppControlType.DateTimeDetail,
                ["DateTime"] = (int)EmAppControlType.DateTimeDetail,
                ["Time"] = (int)EmAppControlType.Time,
                ["ImageBinary"] = (int)EmAppControlType.ImageBinary,
                ["AutoComplete"] = (int)EmAppControlType.AutoComplete,
                ["SearchAbleDDL"] = (int)EmAppControlType.SearchAbleDDL,
                ["SearchableDDL"] = (int)EmAppControlType.SearchAbleDDL,
                ["ExternalImageUrl"] = (int)EmAppControlType.ExternalImageUrl,
                ["MultiSelectDDL"] = (int)EmAppControlType.MultiSelectDDL,
            };

        private const string ControlTypeAllowList =
            "1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label, 13=CheckBox, 20=Numeric, " +
            "26=RichText, 27=DateTimeDetail, 34=Time, 37=ImageBinary, 38=AutoComplete, 48=SearchAbleDDL, " +
            "50=ExternalImageUrl, 53=MultiSelectDDL (there is no controlType=3; Memo is 4)";

        private static bool TryResolveControlType(JToken token, out int controlType, out string error)
        {
            controlType = 0;
            error = null;
            if (token == null || token.Type == JTokenType.Null)
            {
                error = "controlType is required. " + ControlTypeAllowList;
                return false;
            }

            if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            {
                controlType = token.Value<int>();
            }
            else
            {
                var raw = (token.Type == JTokenType.String ? token.Value<string>() : token.ToString())?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    error = "controlType is empty. " + ControlTypeAllowList;
                    return false;
                }

                if (int.TryParse(raw, out controlType))
                {
                    // parsed as int
                }
                else if (!ControlTypeByName.TryGetValue(raw, out controlType))
                {
                    error = $"Unknown controlType '{raw}'. " + ControlTypeAllowList;
                    return false;
                }
            }

            if (!Enum.IsDefined(typeof(EmAppControlType), controlType)
                || controlType == (int)EmAppControlType.InvalidControlType)
            {
                error = $"Invalid controlType {controlType}. " + ControlTypeAllowList;
                return false;
            }

            return true;
        }
    }
}
