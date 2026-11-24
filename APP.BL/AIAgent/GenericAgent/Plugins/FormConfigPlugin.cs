using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Durable Form/Search configuration tools for app-configurator.
    /// HostContextJson supplies transactionId; client-supplied ids are ignored or rejected on mismatch.
    /// </summary>
    public class FormConfigPlugin
    {
        readonly TransactionModifierPlugin _modifier = new TransactionModifierPlugin();

        [AgentTool("get_current_transaction_config",
            "Inspect one open form's transaction configuration (units, fields with sqlToken/messageToken/formulaToken, " +
            "rootPrimaryKeys, commands, linked searches). " +
            "When HostContext.multiTransaction=true, pass transactionId from HostContext.forms / find_fields.")]
        public string GetCurrentTransactionConfig(
            AgentToolContext context,
            [AgentParam("Required when HostContext lists multiple transactions.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            try
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(txnId);
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(txnId);
                if (tx == null) return Fail($"Transaction {txnId} not found.");
                return JsonConvert.SerializeObject(BuildConfigSnapshot(tx), Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("get_unit_fields",
            "List fields for one unit of the open transaction (same catalog as Query Builder / Message / Formula token buttons). " +
            "Each field includes sqlToken/messageToken [TF_{fieldId}_{dbName}], formulaToken, isPrimaryKey, unitKind. " +
            "Prefer unitId; unitName is a fallback. Do NOT guess unitId — use find_fields.")]
        public string GetUnitFields(
            AgentToolContext context,
            [AgentParam("Unit id from HostContext / get_current_transaction_config / find_fields.")] int? unitId = null,
            [AgentParam("Unit display or table name when unitId is unknown.")] string unitName = null,
            [AgentParam("Required when HostContext is multi-transaction and unitId alone is ambiguous.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            try
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(txnId);
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(txnId);
                if (tx == null) return Fail($"Transaction {txnId} not found.");
                var unitMeta = BuildUnitTokenMeta(tx);
                AppTransactionUnitExDto unit = null;
                FieldUnitTokenMeta meta = null;
                if (unitId.HasValue)
                {
                    meta = unitMeta.FirstOrDefault(m => m.UnitId == unitId.Value);
                    unit = meta?.Unit;
                }
                else if (!string.IsNullOrWhiteSpace(unitName))
                {
                    meta = unitMeta.FirstOrDefault(m =>
                        EntityMapWiring.UnitOrTableMatches(m.Unit.UnitDisplayName, unitName) ||
                        EntityMapWiring.UnitOrTableMatches(m.Unit.DataBaseTableName, unitName));
                    unit = meta?.Unit;
                }
                if (unit == null || meta == null)
                    return Fail("Unit not found. Pass unitId from find_fields or get_current_transaction_config.");

                var fields = (unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .Select(f => SummarizeField(f, meta))
                    .ToList();
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    transactionId = txnId,
                    unitId = unit.Id,
                    unitName = unit.UnitDisplayName,
                    table = unit.DataBaseTableName,
                    unitKind = meta.UnitKind,
                    fields,
                    note = "sqlToken/messageToken match Query Builder & Message template buttons; formulaToken matches Command Formula keypad. Never invent [rootid]."
                }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("find_fields",
            "Search the open form(s) for fields by display name or DB column name across ALL units. " +
            "Returns sqlToken/messageToken/formulaToken/isPrimaryKey (same as Query Builder token buttons). " +
            "Use this when the user asks which table/column a field is on — do NOT guess unitId. " +
            "For 'current record' / root PK SQL WHERE clauses, prefer isPrimaryKey=true on unitKind=rootMaster (or rootPrimaryKeys from get_current_transaction_config). " +
            "Template multi-transaction hosts: results grouped by transaction then unit. " +
            "Single transaction: grouped by unit only. If multiple matches, list them all or ask_user.")]
        public string FindFields(
            AgentToolContext context,
            [AgentParam("Display name or DB column name to search (e.g. Country Of Origin, Country_Of_Origin).", isRequired: true)]
            string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Fail("name is required.");
            if (!TryResolveHostTransactions(context, out var hosts, out var err))
                return Fail(err);

            try
            {
                var needle = name.Trim();
                var multi = hosts.Count > 1;
                var matchCount = 0;
                var txnGroups = new List<object>();

                foreach (var host in hosts)
                {
                    AppCacheManagerBL.RefreshOnetHierarchyTranscation(host.TransactionId);
                    var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(host.TransactionId);
                    if (tx == null) continue;

                    var unitMeta = BuildUnitTokenMeta(tx);
                    var unitGroups = new List<object>();

                    foreach (var meta in unitMeta)
                    {
                        var u = meta.Unit;
                        var hits = (u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                            .Where(f => FieldNameMatches(f, needle))
                            .Select(f => SummarizeField(f, meta))
                            .ToList();
                        if (hits.Count == 0) continue;
                        matchCount += hits.Count;
                        unitGroups.Add(new
                        {
                            unitId = u.Id,
                            unitName = u.UnitDisplayName,
                            table = u.DataBaseTableName,
                            unitKind = meta.UnitKind,
                            fields = hits
                        });
                    }

                    if (unitGroups.Count == 0) continue;

                    if (multi)
                    {
                        txnGroups.Add(new
                        {
                            transactionId = host.TransactionId,
                            transactionName = host.TransactionName
                                ?? tx.TransactionName
                                ?? host.TransactionId.ToString(),
                            formRole = host.FormRole,
                            units = unitGroups
                        });
                    }
                    else
                    {
                        // Single transaction: group by unit only
                        return JsonConvert.SerializeObject(new
                        {
                            ok = true,
                            multiTransaction = false,
                            transactionId = host.TransactionId,
                            transactionName = host.TransactionName ?? tx.TransactionName,
                            matchCount,
                            query = needle,
                            units = unitGroups,
                            note = matchCount > 1
                                ? "Multiple matches — use fieldId + table/unit from the list; ask_user if ambiguous."
                                : null
                        }, Formatting.Indented);
                    }
                }

                if (multi)
                {
                    string multiNote = null;
                    if (matchCount == 0)
                        multiNote = "No fields matched across open transactions. Try another name.";
                    else if (matchCount > 1)
                        multiNote = "Multiple matches across transactions/units — pick fieldId (and transactionId for writes); ask_user if ambiguous.";
                    return JsonConvert.SerializeObject(new
                    {
                        ok = true,
                        multiTransaction = true,
                        matchCount,
                        query = needle,
                        transactions = txnGroups,
                        note = multiNote
                    }, Formatting.Indented);
                }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    multiTransaction = false,
                    matchCount = 0,
                    query = needle,
                    units = Array.Empty<object>(),
                    note = "No fields matched. Try another name or get_current_transaction_config."
                }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("update_field_setting",
            "Update durable field settings on the open transaction. Pass only changed keys in changesJson: " +
            "displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty. " +
            "controlType EmAppControlType (no 3): 1=DDL, 2=TextBox, 4=Memo, 5=Image, 7=Date, 9=File, 10=Label, " +
            "13=CheckBox, 20=Numeric, 26=RichText, 34=Time, 37=ImageBinary, 38=AutoComplete, 48=SearchAbleDDL, " +
            "50=ExternalImageUrl, 53=MultiSelectDDL. Memo=4 (not 3). Name strings like \"Memo\" also accepted. " +
            "Prefer fieldId. Call ask_user before writing. After success, call request_form_reload.")]
        public string UpdateFieldSetting(
            AgentToolContext context,
            [AgentParam("Field id (preferred).", isRequired: true)] int? fieldId = null,
            [AgentParam("JSON of properties to change.", isRequired: true)] string changesJson = null,
            [AgentParam("Optional when fieldId uniquely identifies the field; required only if ambiguous across open forms.")] int? transactionId = null,
            [AgentParam("Fallback when fieldId unknown.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err, fieldId))
                return Fail(err);
            if (string.IsNullOrWhiteSpace(changesJson))
                return Fail("changesJson is required.");

            // Map isVisible onto update path via dedicated load/save when present.
            JObject changes;
            try { changes = JObject.Parse(changesJson); }
            catch (Exception ex) { return Fail("changesJson is not valid JSON: " + ex.Message); }

            var hasVisibilityKeys = changes["isVisible"] != null || changes["isReadonly"] != null || changes["isAllowEmpty"] != null;
            var hasCoreKeys = changes["displayName"] != null || changes["controlType"] != null
                || changes["entityId"] != null || changes["defaultValue"] != null;

            string coreResult = null;
            if (hasCoreKeys)
            {
                var core = new JObject();
                if (changes["displayName"] != null) core["displayName"] = changes["displayName"];
                if (changes["controlType"] != null) core["controlType"] = changes["controlType"];
                if (changes["entityId"] != null) core["entityId"] = changes["entityId"];
                if (changes["defaultValue"] != null) core["defaultValue"] = changes["defaultValue"];
                coreResult = _modifier.UpdateTransactionField(txnId, fieldId, fieldName, unitName, core.ToString(Formatting.None));
                if (IsFail(coreResult)) return coreResult;
            }

            if (hasVisibilityKeys)
            {
                var vis = ApplyVisibilityAndFlags(txnId, fieldId, fieldName, unitName, changes);
                if (IsFail(vis)) return vis;
                // ApplyVisibilityAndFlags already refreshes; keep MergeOk for combined payload.
                return MergeOk(coreResult, vis, txnId);
            }

            // UpdateTransactionField refreshes; belt-and-suspenders with known host txnId.
            if (!IsFail(coreResult))
                AppCacheManagerBL.RefreshOneHierarchyTransaction(txnId);

            return string.IsNullOrWhiteSpace(coreResult)
                ? Fail("No supported keys in changesJson.")
                : coreResult;
        }

        [AgentTool("set_field_entity",
            "Link a field on the open transaction to an Entity data source (DDL). Prefer fieldId. ask_user first; then request_form_reload.")]
        public string SetFieldEntity(
            AgentToolContext context,
            [AgentParam("Field id (preferred).", isRequired: true)] int? fieldId = null,
            [AgentParam("Entity id; null removes the link.")] int? entityId = null,
            [AgentParam("Optional when fieldId uniquely identifies the field; required only if ambiguous across open forms.")] int? transactionId = null,
            [AgentParam("Fallback field name.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err, fieldId))
                return Fail(err);
            return _modifier.SetFieldEntity(txnId, fieldId, fieldName, unitName, entityId);
        }

        [AgentTool("set_field_visibility",
            "Soft-hide or show a field on the form (IsVisible). Prefer this over deleting the field. ask_user first; then request_form_reload.")]
        public string SetFieldVisibility(
            AgentToolContext context,
            [AgentParam("Field id (preferred).", isRequired: true)] int? fieldId = null,
            [AgentParam("true = show, false = hide.", isRequired: true)] bool? isVisible = null,
            [AgentParam("Optional when fieldId uniquely identifies the field; required only if ambiguous across open forms.")] int? transactionId = null,
            [AgentParam("Fallback field name.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err, fieldId))
                return Fail(err);
            if (!isVisible.HasValue) return Fail("isVisible is required.");
            var changes = new JObject { ["isVisible"] = isVisible.Value };
            return ApplyVisibilityAndFlags(txnId, fieldId, fieldName, unitName, changes);
        }

        [AgentTool("get_commands",
            "List Operation Task commands on the open transaction (id, name, actionType).")]
        public string GetCommands(
            AgentToolContext context,
            [AgentParam("Required when HostContext lists multiple transactions.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.GetCommands(txnId);
        }

        [AgentTool("get_command_detail",
            "Full detail for one Operation Task command (options / ActionAttribute / SQL text fields).")]
        public string GetCommandDetail(
            AgentToolContext context,
            [AgentParam("Command id.", isRequired: true)] int? commandId = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!commandId.HasValue) return Fail("commandId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.GetCommandDetail(txnId, commandId.Value);
        }

        [AgentTool("upsert_command",
            "Create or update a Data Model Operation. Preview → ask_user → confirmed=true → request_form_reload. " +
            "actionType MUST be EmAppTransactionCommandType (NOT Link Target 1=Edit/2=Create). " +
            "Common: 50=Refresh, 49=Save, 42=Execute SQL, 43=Data Transfer, 59=Call API, " +
            "200=Composition, 54=Close Window, 60/61/62=Send Message, 57=Command Formula, 205=Validate Required. " +
            "For actionType 42: put SQL in notificationMessage; resolve live tokens via find_fields/get_unit_fields/get_current_transaction_config " +
            "(sqlToken like [TF_3499_OpportunityId]). UPDATE/DELETE must WHERE the current row — usually rootPrimaryKeys[].sqlToken. " +
            "Never invent [rootid] or omit WHERE. Message templates use messageToken. " +
            "For actionType 57 (Command Formula): put expression in optionsJson.executionFormulaUI (ActionAttribute) — NOT notificationMessage. " +
            "Resolve left side via find_fields → formulaToken ([DbColumn_fieldId]); datetime = [Now] or [Today]. " +
            "Example: [Notes_3499] = [Now]. Never Format(Now(),…), bare Notes=, or update_unit_formula (that is Calculation Flow). " +
            "For actionType 53 (Navigate To Search): optionsJson.linkedSearchId MUST be an existing unit Linked Search binding id " +
            "from get_linked_searches / upsert_linked_search return — never invent 1 or AppSearch.SearchId. " +
            "If get_linked_searches is empty: list_available_searches → upsert_linked_search first. " +
            "For actionType 200: create Private children first, then optionsJson.childActionList:[{commandId,sort},…]. " +
            "Empty ChildActionList = empty Child Operations grid; do not put SQL on the composition parent. " +
            "optionsJson: linkToUI, isShowOnTopMenu, isAutoExecuteOnFormOpen, actionFlowOrder, childActionList, " +
            "executionFormulaUI (57), linkedSearchId (53), actionAttribute{…}. Does NOT execute the command.")]
        public string UpsertCommand(
            AgentToolContext context,
            [AgentParam("Existing command id for update; omit to create.")] int? commandId = null,
            [AgentParam("Command display name (required on create).")] string name = null,
            [AgentParam("EmAppTransactionCommandType int (required on create). Examples: Refresh=50, Save=49, ExecuteSQLStatement=42, CommandFormula=57. NEVER use 1 (that is Link Target Edit, not a command type).")] int? actionType = null,
            [AgentParam("SQL body for type 42, or message/notification body for messaging types. NOT for type 57 — use optionsJson.executionFormulaUI. Use [TF_{fieldId}_{dbName}] from sqlToken/messageToken — never [rootid].")] string notificationMessage = null,
            [AgentParam("JSON options: linkToUI, childActionList, executionFormulaUI (required for 57), actionAttribute{…}.")] string optionsJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.UpsertCommand(
                txnId, commandId, name, actionType, notificationMessage, optionsJson, confirmed, dryRun);
        }

        [AgentTool("delete_command",
            "Delete an Operation Task command. Preview → ask_user → confirmed=true. Then request_form_reload.")]
        public string DeleteCommand(
            AgentToolContext context,
            [AgentParam("Command id.", isRequired: true)] int? commandId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!commandId.HasValue) return Fail("commandId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.DeleteCommand(txnId, commandId.Value, confirmed, dryRun);
        }

        [AgentTool("get_data_transfers",
            "List Internal Data Transfer settings on the open transaction.")]
        public string GetDataTransfers(
            AgentToolContext context,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.GetDataTransfers(txnId);
        }

        [AgentTool("get_data_transfer_detail",
            "Detail + field mappings for one Data Transfer setting.")]
        public string GetDataTransferDetail(
            AgentToolContext context,
            [AgentParam("DataTransfer setting id.", isRequired: true)] int? dataTransferId = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!dataTransferId.HasValue) return Fail("dataTransferId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.GetDataTransferDetail(txnId, dataTransferId.Value);
        }

        [AgentTool("upsert_data_transfer",
            "Create/update Internal Data Transfer header and optional mappingsJson " +
            "([{sourceFieldId,targetFieldId,name},…]). Preview → ask_user → confirmed=true → request_form_reload.")]
        public string UpsertDataTransfer(
            AgentToolContext context,
            [AgentParam("Existing id for update; omit to create.")] int? dataTransferId = null,
            [AgentParam("Description / name (required on create).")] string description = null,
            [AgentParam("Optional internal code.")] string internalCode = null,
            [AgentParam("Transfer type id.")] int? transferTypeId = null,
            [AgentParam("Destination transaction id.")] int? destinationTransactionId = null,
            [AgentParam("JSON array of field mappings.")] string mappingsJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.UpsertDataTransfer(
                txnId, dataTransferId, description, internalCode, transferTypeId,
                destinationTransactionId, mappingsJson, confirmed, dryRun);
        }

        [AgentTool("delete_data_transfer",
            "Delete a Data Transfer setting. Preview → ask_user → confirmed=true → request_form_reload.")]
        public string DeleteDataTransfer(
            AgentToolContext context,
            [AgentParam("DataTransfer setting id.", isRequired: true)] int? dataTransferId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!dataTransferId.HasValue) return Fail("dataTransferId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.DeleteDataTransfer(txnId, dataTransferId.Value, confirmed, dryRun);
        }

        [AgentTool("get_linked_searches",
            "List unit Linked Search bindings (AppTransactionUnitLinkedSearch). " +
            "Each item.linkedSearchId is for upsert_command actionType 53 — NOT AppSearch.SearchId. " +
            "Also returns filterMappings (Search Filter→Form Field) and availableSearchFilters for mapping. " +
            "To map form CustomerId → search Customer filter: upsert_linked_search(linkedSearchId, filterMappingsJson) — NOT command ParamId1.")]
        public string GetLinkedSearches(
            AgentToolContext context,
            [AgentParam("Required when HostContext lists multiple transactions.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.GetLinkedSearches(txnId);
        }

        [AgentTool("list_available_searches",
            "List real AppSearch definitions for this transaction's SaasApplication (searchId, name, searchViewId, dataSetId). " +
            "Use BEFORE upsert_linked_search — pass searchId only (never dataSetId / transactionId / unitId / invent 1). " +
            "Optional nameContains filter. Returns up to take rows (default 40).")]
        public string ListAvailableSearches(
            AgentToolContext context,
            [AgentParam("Optional case-insensitive name filter.")] string nameContains = null,
            [AgentParam("Max rows (default 40, max 100).")] int? take = null,
            [AgentParam("Required when HostContext lists multiple transactions.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.ListAvailableSearches(txnId, nameContains, take);
        }

        [AgentTool("upsert_linked_search",
            "Create/update unit Linked Search binding (AppTransactionUnitLinkedSearch). " +
            "UPDATE existing: get_linked_searches → pass linkedSearchId (required) + filterMappingsJson. " +
            "Never create a second binding for the same searchId when one already exists on the unit. " +
            "Create only when get_linked_searches has no row for that search: list_available_searches → upsert_linked_search(unitId, searchId, name). " +
            "Search Mapping (UI Search Filter←Form Field): filterMappingsJson=" +
            "[{\"searchFilter\":\"Customer\",\"formField\":\"CustomerId\"}]. " +
            "To undo/clear mappings only: filterMappingsJson=[] on the same linkedSearchId — NEVER delete_linked_search + recreate. " +
            "NEVER command ParamId1 / invent linkedSearchId. " +
            "Defaults: Action=3, UsageType=102, SearchViewId from AppSearch, Sort=max+1. " +
            "Preview → ask_user → confirmed=true → request_form_reload.")]
        public string UpsertLinkedSearch(
            AgentToolContext context,
            [AgentParam("Unit id (prefer root master unitId from get_current_transaction_config).", isRequired: true)] int? unitId = null,
            [AgentParam("Existing linked-search binding id for update.")] int? linkedSearchId = null,
            [AgentParam("Display name for the binding.")] string name = null,
            [AgentParam("AppSearch.SearchId from list_available_searches (required on create).")] int? searchId = null,
            [AgentParam("EmAppLinkedSearchAction: 1=AddGridRows, 2=UpdateFormData, 3=Open Search (default).")] int? action = null,
            [AgentParam("Optional extra DTO fields as JSON.")] string changesJson = null,
            [AgentParam("Search Mapping JSON array: {searchFieldId,transactionFieldId} or {searchFilter,formField}. Pass [] to clear all mappings without deleting the Linked Search.")] string filterMappingsJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!unitId.HasValue) return Fail("unitId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.UpsertLinkedSearch(
                txnId, unitId.Value, linkedSearchId, name, searchId, action, changesJson, filterMappingsJson, confirmed, dryRun);
        }

        [AgentTool("delete_linked_search",
            "Delete a unit Linked Search binding. Preview → ask_user → confirmed=true → request_form_reload.")]
        public string DeleteLinkedSearch(
            AgentToolContext context,
            [AgentParam("Linked search id.", isRequired: true)] int? linkedSearchId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!linkedSearchId.HasValue) return Fail("linkedSearchId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigCommandDataTransferService.DeleteLinkedSearch(txnId, linkedSearchId.Value, confirmed, dryRun);
        }

        [AgentTool("get_link_targets",
            "List AppFormLinkTarget menus on a Transaction Unit (usageType 101=LinkToForm, 5=System page). " +
            "Prefer unitId. Distinct from Linked Search.")]
        public string GetLinkTargets(
            AgentToolContext context,
            [AgentParam("Unit id (preferred).")] int? unitId = null,
            [AgentParam("Optional usageType filter (101 or 5).")] int? usageType = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            if (!unitId.HasValue)
                return Fail("unitId is required. Pass from get_current_transaction_config.");
            if (!UnitBelongsToOpenTxn(txnId, unitId.Value))
                return Fail($"unitId {unitId} is not on transaction {txnId}.");
            return AppFormLinkTargetConfigService.List(
                (int)EmAppLinkTargetSourceType.TransactionUnit, unitId.Value, usageType);
        }

        [AgentTool("get_link_target_detail",
            "Full AppFormLinkTarget row (mappings, destination TX/Search/route, DT id, popup…).")]
        public string GetLinkTargetDetail(
            AgentToolContext context,
            [AgentParam("LinkTarget id.", isRequired: true)] int? linkTargetId = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!linkTargetId.HasValue) return Fail("linkTargetId is required.");
            if (!TryResolveTransactionId(context, transactionId, out _, out var err))
                return Fail(err);
            return AppFormLinkTargetConfigService.Detail(linkTargetId.Value);
        }

        [AgentTool("upsert_link_target",
            "Create/update Transaction Unit Link Target menu. usageType default 101 (LinkToForm) needs linkTargetTransactionId; " +
            "5 needs linkTargetUrlOrRouteCode. actionType: 1=Edit 2=Create 3=Delete 5=Preview 13=CreateFromExisting. " +
            "Preview → ask_user → confirmed=true → request_form_reload.")]
        public string UpsertLinkTarget(
            AgentToolContext context,
            [AgentParam("Unit id.", isRequired: true)] int? unitId = null,
            [AgentParam("Existing linkTargetId for update.")] int? linkTargetId = null,
            [AgentParam("101=LinkToForm (default), 5=System page/route.")] int? usageType = null,
            [AgentParam("1=Edit 2=Create 3=Delete 5=Preview 13=CreateFromExisting.")] int? actionType = null,
            [AgentParam("Menu caption.")] string navigationActionName = null,
            [AgentParam("Destination transaction id (LinkToForm).")] int? linkTargetTransactionId = null,
            [AgentParam("Destination transaction group id.")] int? linkTargetTransactionGroupId = null,
            [AgentParam("Destination search id (rare on TX unit).")] int? linkTargetSearchId = null,
            [AgentParam("Route/URL for system page.")] string linkTargetUrlOrRouteCode = null,
            [AgentParam("Optional Data Transfer setting id.")] int? dataTransferSettingId = null,
            [AgentParam("Sort order.")] int? sort = null,
            [AgentParam("Open as popup.")] bool? isPopup = null,
            [AgentParam("Popup width.")] int? popupWidth = null,
            [AgentParam("Popup height.")] int? popupHeight = null,
            [AgentParam("Icon class name.")] string iconName = null,
            [AgentParam("Menu group name.")] string groupName = null,
            [AgentParam("Source column type enum.")] int? sourceColumnType = null,
            [AgentParam("Source column 1 (TX field map).")] string sourceColumn1 = null,
            [AgentParam("Source column 2.")] string sourceColumn2 = null,
            [AgentParam("Source column 3.")] string sourceColumn3 = null,
            [AgentParam("Target column 1.")] string targetColumn1 = null,
            [AgentParam("Target column 2.")] string targetColumn2 = null,
            [AgentParam("Target column 3.")] string targetColumn3 = null,
            [AgentParam("Layout display mode.")] int? layoutDisplayMode = null,
            [AgentParam("OtherSettings JSON.")] string otherSettingsJson = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!unitId.HasValue) return Fail("unitId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            if (!UnitBelongsToOpenTxn(txnId, unitId.Value))
                return Fail($"unitId {unitId} is not on transaction {txnId}.");
            return AppFormLinkTargetConfigService.Upsert(
                (int)EmAppLinkTargetSourceType.TransactionUnit, unitId.Value, linkTargetId,
                usageType, actionType, navigationActionName,
                linkTargetTransactionId, linkTargetTransactionGroupId, linkTargetSearchId,
                linkTargetUrlOrRouteCode, dataTransferSettingId, sort, isPopup, popupWidth, popupHeight,
                iconName, groupName, sourceColumnType,
                sourceColumn1, sourceColumn2, sourceColumn3,
                targetColumn1, targetColumn2, targetColumn3,
                null, null, null, null, null, null,
                layoutDisplayMode, otherSettingsJson, confirmed, dryRun);
        }

        [AgentTool("delete_link_target",
            "Delete a Transaction Unit Link Target. Preview → ask_user → confirmed=true → request_form_reload.")]
        public string DeleteLinkTarget(
            AgentToolContext context,
            [AgentParam("Unit id that owns the menu.", isRequired: true)] int? unitId = null,
            [AgentParam("LinkTarget id.", isRequired: true)] int? linkTargetId = null,
            [AgentParam("Must be true to persist.")] bool? confirmed = null,
            [AgentParam("If true, never persist.")] bool? dryRun = null,
            [AgentParam("Required when multi-transaction HostContext.")] int? transactionId = null)
        {
            if (!unitId.HasValue) return Fail("unitId is required.");
            if (!linkTargetId.HasValue) return Fail("linkTargetId is required.");
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            if (!UnitBelongsToOpenTxn(txnId, unitId.Value))
                return Fail($"unitId {unitId} is not on transaction {txnId}.");
            return AppFormLinkTargetConfigService.Delete(
                (int)EmAppLinkTargetSourceType.TransactionUnit, unitId.Value, linkTargetId.Value, confirmed, dryRun);
        }

        static bool UnitBelongsToOpenTxn(int transactionId, int unitId)
        {
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx == null) return false;
            var units = EntityMapWiring.FlattenUnits(
                tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            return units.Any(u => Convert.ToInt32(u.Id) == unitId);
        }

        [AgentTool("get_unit_formulas",
            "List Calculation Validation Flow formulas for a unit: Assignment, Warning, Error, Aggregate, Subscribe. " +
            "Prefer unitId. defaultValue is NOT a formula — use this tool / update_unit_formula instead.")]
        public string GetUnitFormulas(
            AgentToolContext context,
            [AgentParam("Unit id (preferred).")] int? unitId = null,
            [AgentParam("Unit display/table name fallback.")] string unitName = null,
            [AgentParam("Required when HostContext is multi-transaction.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigFormulaLayoutService.GetUnitFormulas(txnId, unitId, unitName);
        }

        [AgentTool("update_unit_formula",
            "Add/update/delete a unit formula (Assignment=1, Warning=2, Error=3, Aggregate=4, Subscribe=5). " +
            "User may name fields by display name — tool resolves to [DbName_Id] tokens. " +
            "If ambiguous, returns needsDisambiguation (ask_user which field). " +
            "Always preview first (confirmed omitted/false), ask_user Yes/No, then call again with confirmed=true. " +
            "Optional dryRun=true. After success call request_form_reload. " +
            "Do NOT use update_field_setting.defaultValue for Calculation Flow formulas.")]
        public string UpdateUnitFormula(
            AgentToolContext context,
            [AgentParam("add | update | delete", isRequired: true)] string action = null,
            [AgentParam("Unit id (preferred).")] int? unitId = null,
            [AgentParam("Unit name fallback.")] string unitName = null,
            [AgentParam("Existing formula id (update/delete Assignment/Warning/Error).")] int? formulaId = null,
            [AgentParam("1=Assignment 2=Warning 3=Error 4=Aggregate 5=Subscribe")] int? operationType = null,
            [AgentParam("Human or token expression, e.g. Description = Product Code + \" - \" + Nomenclature")] string expression = null,
            [AgentParam("Assign-to field id (preferred).")] int? assignToFieldId = null,
            [AgentParam("Assign-to display/DB name when id unknown.")] string assignToFieldName = null,
            [AgentParam("Aggregate source child field id / Subscribe parent field id.")] int? sourceFieldId = null,
            [AgentParam("Source field name fallback.")] string sourceFieldName = null,
            [AgentParam("Aggregate: 1=SUM 2=AVG 3=MIN 4=MAX 5=COUNT")] int? aggregationType = null,
            [AgentParam("Warning/Error message text.")] string warningMessage = null,
            [AgentParam("Optional highlight field id for Warning/Error.")] int? warningHighlightFieldId = null,
            [AgentParam("Must be true to persist after ask_user.")] bool? confirmed = null,
            [AgentParam("Preview only; do not write.")] bool? dryRun = null,
            [AgentParam("Required when HostContext is multi-transaction.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err, assignToFieldId))
                return Fail(err);
            return FormConfigFormulaLayoutService.UpdateUnitFormula(
                txnId, action, unitId, unitName, formulaId, operationType, expression,
                assignToFieldId, assignToFieldName, sourceFieldId, sourceFieldName,
                aggregationType, warningMessage, warningHighlightFieldId, confirmed, dryRun);
        }

        [AgentTool("get_form_layout",
            "Inspect the Flex form layout tree for the open transaction (containers, fields, heights, collapsible, tabs).")]
        public string GetFormLayout(
            AgentToolContext context,
            [AgentParam("Required when HostContext is multi-transaction.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigFormulaLayoutService.GetFormLayout(txnId);
        }

        [AgentTool("design_form_layout",
            "Generate or patch Flex form layout. mode=generate: N-column or default layout (REPLACES existing — ask_user + confirmed=true; optional backup/dryRun). " +
            "mode=patch: patchesJson ops setHeight, moveField, setContainer, setColumns(alone→generate). " +
            "After success call request_form_reload.")]
        public string DesignFormLayout(
            AgentToolContext context,
            [AgentParam("generate | patch", isRequired: true)] string mode = null,
            [AgentParam("Column count for generate / setColumns.")] int? nbColumns = null,
            [AgentParam("JSON array of patch ops.")] string patchesJson = null,
            [AgentParam("Must be true to persist after ask_user.")] bool? confirmed = null,
            [AgentParam("Preview only; do not write.")] bool? dryRun = null,
            [AgentParam("Include current layout summary before overwrite.")] bool? backup = null,
            [AgentParam("Required when HostContext is multi-transaction.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return FormConfigFormulaLayoutService.DesignFormLayout(
                txnId, mode, nbColumns, patchesJson, confirmed, dryRun, backup);
        }

        [AgentTool("request_form_reload",
            "Call after successful configuration writes (cache is already refreshed by the save path). " +
            "Signals the form host to prompt Reload Current Form after this agent turn finishes — " +
            "does not block on the user dialog.")]
        public Task<string> RequestFormReload(AgentToolContext context) =>
            AskHost(context, "request_form_reload", new { });

        static EntityDataSourcePlugin EntitiesFor(AgentToolContext context) =>
            new EntityDataSourcePlugin(context?.DataSourceId > 0 ? context.DataSourceId : (int?)null);

        [AgentTool("list_entity_data_sources",
            "List Entity Data Sources (SimpleList and DatabaseTable dropdowns). " +
            "Call before create_entity_* to avoid duplicates, or to find an entityId for set_field_entity.")]
        public string ListEntityDataSources(AgentToolContext context) =>
            EntitiesFor(context).ListEntityDataSources();

        [AgentTool("create_entity_simple_list",
            "Create a SimpleList Entity Data Source (fixed dropdown items, e.g. UD Country: USA,Canada). " +
            "saasApplicationId defaults from HostContext. After create, ask_user then set_field_entity(fieldId, entityId) " +
            "and request_form_reload.")]
        public string CreateEntitySimpleList(
            AgentToolContext context,
            [AgentParam("Entity name/code, e.g. UD Country.", isRequired: true)] string entityCode = null,
            [AgentParam("Comma-separated item labels in order, e.g. USA,Canada.", isRequired: true)] string items = null,
            [AgentParam("Optional; defaults to HostContext.saasApplicationId.")] int? saasApplicationId = null,
            [AgentParam("Optional description.")] string description = null)
        {
            if (string.IsNullOrWhiteSpace(entityCode)) return Fail("entityCode is required.");
            if (string.IsNullOrWhiteSpace(items)) return Fail("items is required.");
            if (!TryResolveSaasApplicationId(context, saasApplicationId, out var appId, out var err))
                return Fail(err);
            return EntitiesFor(context).CreateEntitySimpleList(entityCode.Trim(), items, appId, description);
        }

        [AgentTool("create_entity_from_table",
            "Create a DatabaseTable Entity Data Source (dropdown from an existing DB table). " +
            "Table must already exist. saasApplicationId defaults from HostContext. " +
            "Then ask_user → set_field_entity(fieldId, entityId) → request_form_reload.")]
        public string CreateEntityFromTable(
            AgentToolContext context,
            [AgentParam("Entity name/code.", isRequired: true)] string entityCode = null,
            [AgentParam("Database table name without schema.", isRequired: true)] string tableName = null,
            [AgentParam("Schema owner, e.g. dbo.", isRequired: true)] string schemaOwner = null,
            [AgentParam("PK / value column name.", isRequired: true)] string identityField = null,
            [AgentParam("Primary display column.", isRequired: true)] string displayField1 = null,
            [AgentParam("Optional; defaults to HostContext.saasApplicationId.")] int? saasApplicationId = null,
            [AgentParam("Optional secondary display column.")] string displayField2 = null,
            [AgentParam("Optional tertiary display column.")] string displayField3 = null,
            [AgentParam("Optional description.")] string description = null)
        {
            if (string.IsNullOrWhiteSpace(entityCode)) return Fail("entityCode is required.");
            if (string.IsNullOrWhiteSpace(tableName)) return Fail("tableName is required.");
            if (string.IsNullOrWhiteSpace(schemaOwner)) return Fail("schemaOwner is required.");
            if (string.IsNullOrWhiteSpace(identityField)) return Fail("identityField is required.");
            if (string.IsNullOrWhiteSpace(displayField1)) return Fail("displayField1 is required.");
            if (!TryResolveSaasApplicationId(context, saasApplicationId, out var appId, out var err))
                return Fail(err);
            return EntitiesFor(context).CreateEntityFromTable(
                entityCode.Trim(), tableName.Trim(), schemaOwner.Trim(),
                identityField.Trim(), displayField1.Trim(), appId,
                displayField2, displayField3, description);
        }

        static string ApplyVisibilityAndFlags(
            int txnId, int? fieldId, string fieldName, string unitName, JObject changes)
        {
            try
            {
                // Reuse modifier find via a no-op update path: load field through public BL after resolving id from hierarchy.
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(txnId);
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(txnId);
                if (tx == null) return Fail($"Transaction {txnId} not found.");
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                AppTransactionFieldExDto summary = null;
                if (fieldId.HasValue)
                {
                    summary = units.SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                        .FirstOrDefault(f => Convert.ToInt32(f.Id) == fieldId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    var candidateUnits = string.IsNullOrWhiteSpace(unitName)
                        ? units
                        : units.Where(u =>
                            EntityMapWiring.UnitOrTableMatches(u.UnitDisplayName, unitName) ||
                            EntityMapWiring.UnitOrTableMatches(u.DataBaseTableName, unitName)).ToList();
                    if (!string.IsNullOrWhiteSpace(unitName) && candidateUnits.Count == 0)
                        candidateUnits = units;
                    var matches = candidateUnits
                        .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                        .Where(f =>
                            string.Equals(f.DisplayName, fieldName, StringComparison.OrdinalIgnoreCase) ||
                            EntityMapWiring.ColumnNameMatches(f.DataBaseFieldName, fieldName))
                        .ToList();
                    if (matches.Count == 1) summary = matches[0];
                    else if (matches.Count > 1)
                        return Fail($"Field '{fieldName}' is ambiguous. Pass fieldId.");
                }
                if (summary == null) return Fail("Field not found. Pass fieldId from get_current_transaction_config.");

                var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(Convert.ToInt32(summary.Id));
                if (field == null) return Fail($"Could not load field Id={summary.Id}");
                field.IsModified = true;
                // Parity with Form Configuration / Transaction Editor save: cache refresh keys off TransactionId.
                if (!field.TransactionId.HasValue || field.TransactionId.Value <= 0)
                    field.TransactionId = txnId;
                if (changes["isVisible"] != null)
                    field.IsVisible = changes["isVisible"].Value<bool>();
                if (changes["isReadonly"] != null)
                    field.IsReadonly = changes["isReadonly"].Value<bool>();
                if (changes["isAllowEmpty"] != null)
                    field.IsAllowEmpty = changes["isAllowEmpty"].Value<bool>();

                var result = AppTransactionBL.SaveAppTransactionFieldExDto(field);
                var success = result?.ValidationResult?.HasErrors == false;
                if (!success)
                {
                    var msgs = result?.ValidationResult?.Items?.Select(i => i.Message).ToList();
                    return Fail(msgs != null && msgs.Count > 0 ? string.Join("; ", msgs) : "Save failed");
                }
                // Explicit refresh with known txnId (same as editor save path).
                AppCacheManagerBL.RefreshOneHierarchyTransaction(txnId);
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = true,
                    ok = true,
                    TransactionId = txnId,
                    FieldId = Convert.ToInt32(summary.Id),
                    DisplayName = field.DisplayName,
                    IsVisible = field.IsVisible,
                    IsReadonly = field.IsReadonly,
                    IsAllowEmpty = field.IsAllowEmpty,
                    changed = new[] { new { type = "field", id = Convert.ToInt32(summary.Id) } },
                    NextStep = "Call request_form_reload so the host can reload form metadata."
                }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static object BuildConfigSnapshot(AppTransactionExDto tx)
        {
            var unitMeta = BuildUnitTokenMeta(tx);
            var rootPrimaryKeys = unitMeta
                .Where(m => m.UnitKind == "rootMaster")
                .SelectMany(m => (m.Unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .Where(f => f.IsPrimaryKey)
                    .Select(f => SummarizeField(f, m)))
                .ToList();

            return new
            {
                ok = true,
                surface = "form",
                mode = "configurator",
                transactionId = tx.Id,
                transactionName = tx.TransactionName,
                saasApplicationId = tx.SaasApplicationId,
                rootPrimaryKeys,
                tokenGuide = new
                {
                    sqlAndMessage = "[TF_{fieldId}_{dbName}] — Query Builder Form Root / Grid buttons and Message template field buttons (sqlToken / messageToken).",
                    formula = "[DbColumn_fieldId] on rootMaster; [Table.DbColumn_fieldId] on masterSibling/childGrid (formulaToken). Store in optionsJson.executionFormulaUI for actionType 57.",
                    builtinsSql = new[] { "[CurrentUserId]", "[CurrentPartnerId]", "[CurrentUserName]", "GETDATE()", "GETUTCDATE()" },
                    builtinsMessage = new[] { "[Today]", "[CurrentDatetime]", "[CurrentUserName]", "[CurrentUserId]", "[CurrentPartnerId]", "[CurrentPkValue]", "[ApplicationURL]" },
                    builtinsFormula = new[] { "[Now]", "[Today]", "[CurrentUserId]", "[CurrentPartnerId]", "[CurrentUserIPAddress]" },
                    neverInvent = new[] { "[rootid]", "[TF_ColumnOnly]", "bare column without TF_ prefix for runtime form values", "Format(Now(),…)", "Notes = … (use formulaToken)" }
                },
                units = unitMeta.Select(m => new
                {
                    unitId = m.Unit.Id,
                    name = m.Unit.UnitDisplayName,
                    table = m.Unit.DataBaseTableName,
                    unitKind = m.UnitKind,
                    parentTable = m.ParentTable,
                    fieldCount = m.Unit.AppTransactionFieldList?.Count ?? 0,
                    fields = (m.Unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                        .Select(f => SummarizeField(f, m)).ToList()
                }).ToList()
            };
        }

        sealed class FieldUnitTokenMeta
        {
            public int UnitId;
            public AppTransactionUnitExDto Unit;
            public string UnitKind; // rootMaster | masterSibling | childGrid
            public string ParentTable;
        }

        /// <summary>
        /// Mirrors Command Query Builder / Message / Formula unit layering
        /// (Form Root Level Fields vs Grid {unit} Columns vs sibling units).
        /// </summary>
        static List<FieldUnitTokenMeta> BuildUnitTokenMeta(AppTransactionExDto tx)
        {
            var list = new List<FieldUnitTokenMeta>();
            foreach (var root in tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>())
            {
                if (root?.Id == null) continue;
                var kind = root.IsMasterSiblingUnit == true ? "masterSibling" : "rootMaster";
                list.Add(new FieldUnitTokenMeta
                {
                    UnitId = Convert.ToInt32(root.Id),
                    Unit = root,
                    UnitKind = kind,
                    ParentTable = null
                });
                foreach (var child in root.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                {
                    if (child?.Id == null) continue;
                    list.Add(new FieldUnitTokenMeta
                    {
                        UnitId = Convert.ToInt32(child.Id),
                        Unit = child,
                        UnitKind = "childGrid",
                        ParentTable = root.DataBaseTableName
                    });
                }
            }
            return list;
        }

        static string ResolveDbColumn(AppTransactionFieldExDto f) =>
            !string.IsNullOrWhiteSpace(f?.DataBaseFieldName)
                ? f.DataBaseFieldName
                : (!string.IsNullOrWhiteSpace(f?.DisplayName) ? f.DisplayName : $"Field_{f?.Id}");

        /// <summary>Same string Query Builder inserts for SQL / Message template field buttons.</summary>
        static string BuildSqlOrMessageToken(AppTransactionFieldExDto f)
        {
            if (f?.Id == null) return null;
            return $"[TF_{f.Id}_{ResolveDbColumn(f)}]";
        }

        /// <summary>Same string Command Formula keypad inserts (Angular FormulaDisplayName).</summary>
        static string BuildFormulaToken(AppTransactionFieldExDto f, FieldUnitTokenMeta meta)
        {
            if (f?.Id == null) return null;
            var db = ResolveDbColumn(f);
            var isTemp = f.IsTempVariable == true;
            var unitTable = meta?.Unit?.DataBaseTableName ?? meta?.Unit?.UnitDisplayName ?? "";
            if (meta != null && (meta.UnitKind == "childGrid" || meta.UnitKind == "masterSibling"))
            {
                var prefix = string.IsNullOrWhiteSpace(unitTable) ? "" : unitTable + ".";
                return isTemp ? $"[{prefix}{db}]" : $"[{prefix}{db}_{f.Id}]";
            }
            return isTemp ? $"[{db}]" : $"[{db}_{f.Id}]";
        }

        static string BuildShortLabel(AppTransactionFieldExDto f, FieldUnitTokenMeta meta)
        {
            var db = ResolveDbColumn(f);
            if (meta == null) return db;
            if (meta.UnitKind == "childGrid")
                return $"Grid:{(meta.ParentTable ?? meta.Unit?.DataBaseTableName ?? "")}.{db}";
            if (meta.UnitKind == "masterSibling")
                return $"{meta.Unit?.DataBaseTableName ?? meta.Unit?.UnitDisplayName}.{db}";
            return db;
        }

        static object SummarizeField(AppTransactionFieldExDto f, FieldUnitTokenMeta meta)
        {
            var sqlToken = BuildSqlOrMessageToken(f);
            return new
            {
                fieldId = f.Id,
                name = f.DisplayName,
                dbName = f.DataBaseFieldName,
                shortLabel = BuildShortLabel(f, meta),
                controlType = f.ControlType,
                entityId = f.EntityId,
                isVisible = f.IsVisible,
                isReadonly = f.IsReadonly,
                isAllowEmpty = f.IsAllowEmpty,
                isPrimaryKey = f.IsPrimaryKey,
                isTempVariable = f.IsTempVariable == true,
                unitId = meta?.UnitId,
                unitKind = meta?.UnitKind,
                // Query Builder / Message template field button value
                sqlToken,
                messageToken = sqlToken,
                // Command Formula keypad value (different syntax)
                formulaToken = BuildFormulaToken(f, meta),
                defaultValue = f.DefaultValue
            };
        }

        sealed class HostTxnInfo
        {
            public int TransactionId;
            public string TransactionName;
            public string FormRole;
        }

        static bool FieldNameMatches(AppTransactionFieldExDto f, string needle)
        {
            if (f == null || string.IsNullOrWhiteSpace(needle)) return false;
            if (EntityMapWiring.UnitOrTableMatches(f.DisplayName, needle)) return true;
            if (EntityMapWiring.UnitOrTableMatches(f.DataBaseFieldName, needle)) return true;
            return false;
        }

        static bool TryResolveSaasApplicationId(
            AgentToolContext context, int? clientAppId, out int saasApplicationId, out string error)
        {
            saasApplicationId = 0;
            error = null;
            if (clientAppId.HasValue && clientAppId.Value > 0)
            {
                saasApplicationId = clientAppId.Value;
                return true;
            }

            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            if (!string.IsNullOrWhiteSpace(hostJson))
            {
                try
                {
                    var root = JToken.Parse(hostJson);
                    var fromHost = (root["saasApplicationId"] ?? root["SaasApplicationId"])?.Value<int?>();
                    if (fromHost.HasValue && fromHost.Value > 0)
                    {
                        saasApplicationId = fromHost.Value;
                        return true;
                    }
                }
                catch { /* fall through to transaction lookup */ }
            }

            if (TryResolveHostTransactions(context, out var hosts, out _) && hosts.Count > 0)
            {
                foreach (var host in hosts)
                {
                    try
                    {
                        var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(host.TransactionId)
                            ?? AppTransactionBL.GetHierarchyTranscationFromDatabase(host.TransactionId);
                        if (tx?.SaasApplicationId != null && tx.SaasApplicationId.Value > 0)
                        {
                            saasApplicationId = tx.SaasApplicationId.Value;
                            return true;
                        }
                    }
                    catch { /* try next */ }
                }
            }

            error = "saasApplicationId missing. Pass it or ensure HostContext.saasApplicationId / open transaction has SaasApplicationId.";
            return false;
        }

        static bool TryResolveHostTransactions(
            AgentToolContext context, out List<HostTxnInfo> hosts, out string error)
        {
            hosts = new List<HostTxnInfo>();
            error = null;
            var hostJson = FormHostContextStore.Get(context?.ChatSessionKey);
            if (string.IsNullOrWhiteSpace(hostJson))
            {
                error = "No HostContext. This tool runs inside Form/Search Configurator only.";
                return false;
            }
            try
            {
                var root = JToken.Parse(hostJson);
                if (root.Type != JTokenType.Object)
                {
                    error = "Invalid HostContextJson.";
                    return false;
                }

                if (root["forms"] is JArray forms && forms.Count > 0)
                {
                    foreach (var f in forms)
                    {
                        var id = (f["transactionId"] ?? f["TransactionId"])?.Value<int?>();
                        if (!id.HasValue || id.Value <= 0) continue;
                        if (hosts.Any(h => h.TransactionId == id.Value)) continue;
                        hosts.Add(new HostTxnInfo
                        {
                            TransactionId = id.Value,
                            TransactionName = (f["transactionName"] ?? f["TransactionName"] ?? f["formName"])?.ToString(),
                            FormRole = (f["formRole"] ?? f["FormRole"])?.ToString()
                        });
                    }
                }

                if (hosts.Count == 0)
                {
                    var hostId = (root["transactionId"] ?? root["TransactionId"])?.Value<int?>();
                    if (hostId.HasValue && hostId.Value > 0)
                    {
                        hosts.Add(new HostTxnInfo
                        {
                            TransactionId = hostId.Value,
                            TransactionName = (root["transactionName"] ?? root["TransactionName"])?.ToString(),
                            FormRole = (root["formRole"] ?? root["FormRole"])?.ToString()
                        });
                    }
                }

                if (hosts.Count == 0)
                {
                    error = "HostContext is missing transactionId / forms[].transactionId.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "Invalid HostContextJson: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Resolves one transaction for read/write tools.
        /// Multi-transaction HostContext: prefer client transactionId; else locate fieldId on open forms.
        /// Single: HostContext transactionId wins; client id must match if provided.
        /// </summary>
        static bool TryResolveTransactionId(
            AgentToolContext context, int? clientTxnId, out int txnId, out string error,
            int? fieldId = null)
        {
            txnId = 0;
            error = null;
            if (!TryResolveHostTransactions(context, out var hosts, out error))
                return false;

            if (hosts.Count > 1)
            {
                if (clientTxnId.HasValue && clientTxnId.Value > 0)
                {
                    if (!hosts.Any(h => h.TransactionId == clientTxnId.Value))
                    {
                        error = $"transactionId {clientTxnId.Value} is not in the open form group. Use HostContext.forms / find_fields.";
                        return false;
                    }
                    txnId = clientTxnId.Value;
                    return true;
                }

                if (fieldId.HasValue && fieldId.Value > 0)
                {
                    var owners = new List<HostTxnInfo>();
                    foreach (var host in hosts)
                    {
                        if (HostOwnsField(host.TransactionId, fieldId.Value))
                            owners.Add(host);
                    }
                    if (owners.Count == 1)
                    {
                        txnId = owners[0].TransactionId;
                        return true;
                    }
                    if (owners.Count > 1)
                    {
                        error = $"fieldId {fieldId.Value} exists on multiple open transactions (" +
                                string.Join(", ", owners.Select(o => $"{o.TransactionName ?? "?"}={o.TransactionId}")) +
                                "). Pass transactionId from find_fields.";
                        return false;
                    }
                    error = $"fieldId {fieldId.Value} was not found on any open HostContext transaction. Call find_fields first.";
                    return false;
                }

                error = "HostContext has multiple transactions. Pass fieldId (preferred) or transactionId from find_fields — do not ask the user to leave multi-form mode.";
                return false;
            }

            var hostId = hosts[0].TransactionId;
            if (clientTxnId.HasValue && clientTxnId.Value > 0 && clientTxnId.Value != hostId)
            {
                error = $"transactionId mismatch: host={hostId}, args={clientTxnId.Value}. Use the open form only.";
                return false;
            }
            txnId = hostId;
            return true;
        }

        static bool HostOwnsField(int transactionId, int fieldId)
        {
            try
            {
                var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId)
                    ?? AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                if (tx == null) return false;
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                return units
                    .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .Any(f => Convert.ToInt32(f.Id) == fieldId);
            }
            catch
            {
                return false;
            }
        }

        static async Task<string> AskHost(AgentToolContext context, string toolName, object args)
        {
            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null)
                return Fail("This tool only runs inside an open form Configurator.");

            var evt = new FormHostToolEvent
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                ArgsJson = JsonConvert.SerializeObject(args)
            };
            try
            {
                var result = await callbacks.OnHostTool(evt).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result)
                    ? Fail("Empty host result.")
                    : result;
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });

        static bool IsFail(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return true;
            try
            {
                var jo = JObject.Parse(json);
                if (jo["IsSuccess"]?.Value<bool>() == false) return true;
                if (jo["ok"]?.Value<bool>() == false) return true;
                return false;
            }
            catch { return true; }
        }

        static string MergeOk(string coreResult, string visResult, int txnId)
        {
            if (!string.IsNullOrWhiteSpace(coreResult) && !IsFail(coreResult))
            {
                try
                {
                    var core = JObject.Parse(coreResult);
                    var vis = JObject.Parse(visResult);
                    core["IsVisible"] = vis["IsVisible"];
                    core["IsReadonly"] = vis["IsReadonly"];
                    core["IsAllowEmpty"] = vis["IsAllowEmpty"];
                    core["NextStep"] = "Call request_form_reload so the host can reload form metadata.";
                    core["ok"] = true;
                    return core.ToString(Formatting.Indented);
                }
                catch { /* fall through */ }
            }
            return visResult;
        }
    }
}
