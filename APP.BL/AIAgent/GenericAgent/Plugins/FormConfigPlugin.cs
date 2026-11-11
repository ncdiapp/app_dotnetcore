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
            "Inspect one open form's transaction configuration (units, fields, commands, linked searches). " +
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
            "List fields for one unit of the open transaction. Prefer unitId; unitName is a fallback. " +
            "Do NOT guess unitId when looking up a field by name — use find_fields instead.")]
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
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                AppTransactionUnitExDto unit = null;
                if (unitId.HasValue)
                    unit = units.FirstOrDefault(u => Convert.ToInt32(u.Id) == unitId.Value);
                else if (!string.IsNullOrWhiteSpace(unitName))
                    unit = units.FirstOrDefault(u =>
                        EntityMapWiring.UnitOrTableMatches(u.UnitDisplayName, unitName) ||
                        EntityMapWiring.UnitOrTableMatches(u.DataBaseTableName, unitName));
                if (unit == null)
                    return Fail("Unit not found. Pass unitId from find_fields or get_current_transaction_config.");

                var fields = (unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    .Select(SummarizeField)
                    .ToList();
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    transactionId = txnId,
                    unitId = unit.Id,
                    unitName = unit.UnitDisplayName,
                    table = unit.DataBaseTableName,
                    fields
                }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("find_fields",
            "Search the open form(s) for fields by display name or DB column name across ALL units. " +
            "Use this when the user asks which table/column a field is on — do NOT guess unitId. " +
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

                    var units = EntityMapWiring.FlattenUnits(
                        tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                    var unitGroups = new List<object>();

                    foreach (var u in units)
                    {
                        var hits = (u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                            .Where(f => FieldNameMatches(f, needle))
                            .Select(f => new
                            {
                                fieldId = f.Id,
                                name = f.DisplayName,
                                dbName = f.DataBaseFieldName,
                                controlType = f.ControlType,
                                entityId = f.EntityId,
                                isVisible = f.IsVisible
                            })
                            .ToList();
                        if (hits.Count == 0) continue;
                        matchCount += hits.Count;
                        unitGroups.Add(new
                        {
                            unitId = u.Id,
                            unitName = u.UnitDisplayName,
                            table = u.DataBaseTableName,
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
            "List commands configured on the open transaction.")]
        public string GetCommands(
            AgentToolContext context,
            [AgentParam("Required when HostContext lists multiple transactions.")] int? transactionId = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            try
            {
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(txnId);
                if (tx == null) return Fail($"Transaction {txnId} not found.");
                var cmds = (tx.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name ?? c.DisplayName
                    }).ToList();
                return JsonConvert.SerializeObject(new { ok = true, transactionId = txnId, commands = cmds }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        [AgentTool("get_linked_searches",
            "List Linked Search configurations on units of the open transaction.")]
        public string GetLinkedSearches(
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
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                var list = new List<object>();
                foreach (var u in units)
                {
                    foreach (var ls in u.AppTransactionUnitLinkedSearchList
                             ?? Enumerable.Empty<AppTransactionUnitLinkedSearchExDto>())
                    {
                        list.Add(new
                        {
                            unitId = u.Id,
                            unitName = u.UnitDisplayName,
                            id = ls.Id,
                            name = ls.Name,
                            searchId = ls.SearchId
                        });
                    }
                }
                return JsonConvert.SerializeObject(new { ok = true, transactionId = txnId, linkedSearches = list }, Formatting.Indented);
            }
            catch (Exception ex) { return Fail(ex.Message); }
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
            var units = EntityMapWiring.FlattenUnits(
                tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            return new
            {
                ok = true,
                surface = "form",
                mode = "configurator",
                transactionId = tx.Id,
                transactionName = tx.TransactionName,
                saasApplicationId = tx.SaasApplicationId,
                units = units.Select(u => new
                {
                    unitId = u.Id,
                    name = u.UnitDisplayName,
                    table = u.DataBaseTableName,
                    fieldCount = u.AppTransactionFieldList?.Count ?? 0,
                    fields = (u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                        .Select(SummarizeField).ToList()
                }).ToList()
            };
        }

        static object SummarizeField(AppTransactionFieldExDto f) => new
        {
            fieldId = f.Id,
            name = f.DisplayName,
            dbName = f.DataBaseFieldName,
            controlType = f.ControlType,
            entityId = f.EntityId,
            isVisible = f.IsVisible,
            isReadonly = f.IsReadonly,
            isAllowEmpty = f.IsAllowEmpty,
            defaultValue = f.DefaultValue
        };

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
