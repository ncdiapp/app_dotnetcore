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
            "Inspect the open form's transaction configuration (units, fields, commands, linked searches). Uses HostContext transactionId.")]
        public string GetCurrentTransactionConfig(AgentToolContext context)
        {
            if (!TryResolveTransactionId(context, null, out var txnId, out var err))
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
            "List fields for one unit of the open transaction. Prefer unitId; unitName is a fallback.")]
        public string GetUnitFields(
            AgentToolContext context,
            [AgentParam("Unit id from HostContext / get_current_transaction_config.")] int? unitId = null,
            [AgentParam("Unit display or table name when unitId is unknown.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, null, out var txnId, out var err))
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
                    return Fail("Unit not found. Pass unitId from get_current_transaction_config.");

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

        [AgentTool("update_field_setting",
            "Update durable field settings on the open transaction. Pass only changed keys in changesJson: " +
            "displayName, controlType, entityId, defaultValue, isVisible, isReadonly, isAllowEmpty. " +
            "Prefer fieldId. Call ask_user before writing. After success, call request_form_reload.")]
        public string UpdateFieldSetting(
            AgentToolContext context,
            [AgentParam("Field id (preferred).", isRequired: true)] int? fieldId = null,
            [AgentParam("JSON of properties to change.", isRequired: true)] string changesJson = null,
            [AgentParam("Optional; ignored when HostContext has transactionId.")] int? transactionId = null,
            [AgentParam("Fallback when fieldId unknown.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
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
                return MergeOk(coreResult, vis, txnId);
            }

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
            [AgentParam("Optional; ignored when HostContext has transactionId.")] int? transactionId = null,
            [AgentParam("Fallback field name.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            return _modifier.SetFieldEntity(txnId, fieldId, fieldName, unitName, entityId);
        }

        [AgentTool("set_field_visibility",
            "Soft-hide or show a field on the form (IsVisible). Prefer this over deleting the field. ask_user first; then request_form_reload.")]
        public string SetFieldVisibility(
            AgentToolContext context,
            [AgentParam("Field id (preferred).", isRequired: true)] int? fieldId = null,
            [AgentParam("true = show, false = hide.", isRequired: true)] bool? isVisible = null,
            [AgentParam("Optional; ignored when HostContext has transactionId.")] int? transactionId = null,
            [AgentParam("Fallback field name.")] string fieldName = null,
            [AgentParam("Unit scope for fieldName.")] string unitName = null)
        {
            if (!TryResolveTransactionId(context, transactionId, out var txnId, out var err))
                return Fail(err);
            if (!isVisible.HasValue) return Fail("isVisible is required.");
            var changes = new JObject { ["isVisible"] = isVisible.Value };
            return ApplyVisibilityAndFlags(txnId, fieldId, fieldName, unitName, changes);
        }

        [AgentTool("get_commands",
            "List commands configured on the open transaction.")]
        public string GetCommands(AgentToolContext context)
        {
            if (!TryResolveTransactionId(context, null, out var txnId, out var err))
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
        public string GetLinkedSearches(AgentToolContext context)
        {
            if (!TryResolveTransactionId(context, null, out var txnId, out var err))
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
            "Ask the form host to prompt the user to reload metadata after configuration changes. Call after successful writes.")]
        public Task<string> RequestFormReload(AgentToolContext context) =>
            AskHost(context, "request_form_reload", new { });

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

        static bool TryResolveTransactionId(
            AgentToolContext context, int? clientTxnId, out int txnId, out string error)
        {
            txnId = 0;
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
                var hostId = root.Type == JTokenType.Object
                    ? (root["transactionId"] ?? root["TransactionId"])?.Value<int?>()
                    : null;
                if (root.Type == JTokenType.Object && root["forms"] is JArray forms && forms.Count > 0)
                {
                    hostId ??= (forms[0]["transactionId"] ?? forms[0]["TransactionId"])?.Value<int?>();
                }
                if (!hostId.HasValue || hostId.Value <= 0)
                {
                    error = "HostContext is missing transactionId.";
                    return false;
                }
                if (clientTxnId.HasValue && clientTxnId.Value > 0 && clientTxnId.Value != hostId.Value)
                {
                    error = $"transactionId mismatch: host={hostId.Value}, args={clientTxnId.Value}. Use the open form only.";
                    return false;
                }
                txnId = hostId.Value;
                return true;
            }
            catch (Exception ex)
            {
                error = "Invalid HostContextJson: " + ex.Message;
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
