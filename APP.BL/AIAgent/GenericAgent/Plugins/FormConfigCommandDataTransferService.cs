using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Operation Task (Commands), Data Transfer, and Linked Search mutate helpers for Form Config.
    /// </summary>
    public static class FormConfigCommandDataTransferService
    {
        /// <summary>
        /// UI-offered EmAppTransactionCommandType values (Command Management taxonomy).
        /// Rejects confusions like Link Target actionType 1=Edit.
        /// </summary>
        static readonly HashSet<int> AllowedCommandActionTypes = new HashSet<int>
        {
            38, 40, 42, 43, 44, 45, 46, 47, 48, 49, 50, 53, 54, 57, 59,
            60, 61, 62, 63, 66, 67, 68, 71, 72, 73, 74, 76, 77, 81, 82, 200, 205
        };

        public static string GetCommands(int transactionId)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var cmds = (tx.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                    .Select(SummarizeCommand)
                    .ToList();
                return Ok(new { transactionId, commands = cmds });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetCommandDetail(int transactionId, int commandId)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var cmd = (tx.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                    .FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId);
                if (cmd == null) return Fail($"Command {commandId} not found on transaction {transactionId}.");
                return Ok(new { transactionId, command = DetailCommand(cmd) });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string UpsertCommand(
            int transactionId,
            int? commandId,
            string name,
            int? actionType,
            string notificationMessage,
            string optionsJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                tx.CommandActionList ??= new List<AppProjectWorkFlowActionExDto>();

                AppProjectWorkFlowActionExDto cmd;
                if (commandId.HasValue && commandId.Value > 0)
                {
                    cmd = tx.CommandActionList.FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId.Value);
                    if (cmd == null) return Fail($"Command {commandId} not found.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(name)) return Fail("name is required for create.");
                    if (!actionType.HasValue) return Fail("actionType is required for create (EmAppTransactionCommandType). Examples: Refresh=50, Save=49, ExecuteSQLStatement=42. Do NOT use Link Target codes (1=Edit).");
                    if (!AllowedCommandActionTypes.Contains(actionType.Value))
                        return Fail($"actionType {actionType.Value} is not a valid EmAppTransactionCommandType for Operation Task. Examples: Refresh=50, Save=49, ExecuteSQLStatement=42, Composition=200. Do NOT confuse with Link Target actionType (1=Edit, 2=Create).");
                    var maxOrder = tx.CommandActionList
                        .Select(c => c.ActionFlowOrder ?? 0)
                        .DefaultIfEmpty(0)
                        .Max();
                    cmd = new AppProjectWorkFlowActionExDto
                    {
                        CommandTransactionId = transactionId,
                        ActionFlowOrder = maxOrder + 1,
                        ActionAttribute = new AppActionAttributeDto
                        {
                            ChildActionList = new List<ChildTransactionCommandDto>(),
                            LinkToUI = true,
                            IsShowOnTopMenu = true,
                            IsAutoExecuteOnFormOpen = false
                        }
                    };
                    tx.CommandActionList.Add(cmd);
                }

                if (name != null) cmd.Name = name;
                if (actionType.HasValue)
                {
                    if (!AllowedCommandActionTypes.Contains(actionType.Value))
                        return Fail($"actionType {actionType.Value} is not a valid EmAppTransactionCommandType for Operation Task. Examples: Refresh=50, Save=49, ExecuteSQLStatement=42, Composition=200. Do NOT confuse with Link Target actionType (1=Edit, 2=Create).");
                    cmd.ActionType = actionType;
                }
                if (notificationMessage != null) cmd.NotificationMessage = notificationMessage;
                if (cmd.ActionAttribute == null)
                    cmd.ActionAttribute = new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };

                if (!string.IsNullOrWhiteSpace(optionsJson))
                {
                    try
                    {
                        var jo = JObject.Parse(optionsJson);
                        ApplyCommandOptions(cmd, jo);
                        if (cmd.ActionType.HasValue && !AllowedCommandActionTypes.Contains(cmd.ActionType.Value))
                            return Fail($"actionType {cmd.ActionType.Value} from optionsJson is not a valid EmAppTransactionCommandType. Examples: Refresh=50, ExecuteSQLStatement=42.");
                    }
                    catch (Exception ex) { return Fail("optionsJson invalid: " + ex.Message); }
                }

                var preview = new { action = cmd.IsNew ? "create" : "update", command = DetailCommand(cmd) };
                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then call again with confirmed=true.",
                        proposed = preview
                    });
                }

                var result = AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save command failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                var fresh = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                var saved = (fresh?.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                    .FirstOrDefault(c =>
                        (!cmd.IsNew && c.Id != null && Convert.ToInt32(c.Id) == commandId) ||
                        (cmd.IsNew && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

                return Ok(new
                {
                    saved = true,
                    commandId = saved?.Id ?? commandId,
                    command = saved != null ? DetailCommand(saved) : null,
                    changed = new[] { new { type = "command", id = saved?.Id ?? commandId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string DeleteCommand(int transactionId, int commandId, bool? confirmed, bool? dryRun)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                tx.CommandActionList ??= new List<AppProjectWorkFlowActionExDto>();
                var cmd = tx.CommandActionList.FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId);
                if (cmd == null) return Fail($"Command {commandId} not found.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new { action = "delete", command = SummarizeCommand(cmd) }
                    });
                }

                // Save path deletes any DB command missing from the submitted list.
                tx.CommandActionList = tx.CommandActionList
                    .Where(c => c.Id == null || Convert.ToInt32(c.Id) != commandId)
                    .ToList();

                var result = AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete command failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    deleted = true,
                    commandId,
                    changed = new[] { new { type = "command", id = commandId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetDataTransfers(int transactionId)
        {
            try
            {
                var list = AppTransactionDataTransferSettingBL
                    .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                    .Select(d => new
                    {
                        dataTransferId = d.Id,
                        description = d.Description,
                        internalCode = d.InternalCode,
                        transferTypeId = d.TransferTypeId,
                        transactionId = d.TransactionId,
                        destinationTransactionId = d.DestinationTransactionId,
                        mappingCount = d.AppTransactionSaveAsMappingList?.Count ?? 0
                    }).ToList();
                return Ok(new { transactionId, dataTransfers = list });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetDataTransferDetail(int transactionId, int dataTransferId)
        {
            try
            {
                var dto = AppTransactionDataTransferSettingBL
                    .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                    .FirstOrDefault(d => d.Id != null && Convert.ToInt32(d.Id) == dataTransferId);
                if (dto == null) return Fail($"DataTransfer {dataTransferId} not found on transaction {transactionId}.");
                return Ok(new
                {
                    transactionId,
                    dataTransfer = new
                    {
                        dataTransferId = dto.Id,
                        description = dto.Description,
                        internalCode = dto.InternalCode,
                        transferTypeId = dto.TransferTypeId,
                        destinationTransactionId = dto.DestinationTransactionId,
                        mappings = (dto.AppTransactionSaveAsMappingList ?? new ObservableSet<AppTransactionSaveAsMappingExDto>())
                            .Select(m => new
                            {
                                mappingId = m.Id,
                                sourceFieldId = m.SourceFiledId,
                                targetFieldId = m.TargetFiledId,
                                name = m.Name
                            }).ToList()
                    }
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string UpsertDataTransfer(
            int transactionId,
            int? dataTransferId,
            string description,
            string internalCode,
            int? transferTypeId,
            int? destinationTransactionId,
            string mappingsJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                AppTransactionDataTransferSettingExDto dto;
                if (dataTransferId.HasValue && dataTransferId.Value > 0)
                {
                    dto = AppTransactionDataTransferSettingBL
                        .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                        .FirstOrDefault(d => d.Id != null && Convert.ToInt32(d.Id) == dataTransferId.Value);
                    if (dto == null) return Fail($"DataTransfer {dataTransferId} not found.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(description)) return Fail("description is required for create.");
                    dto = new AppTransactionDataTransferSettingExDto
                    {
                        TransactionId = transactionId,
                        AppTransactionSaveAsMappingList = new ObservableSet<AppTransactionSaveAsMappingExDto>()
                    };
                }

                if (description != null) dto.Description = description;
                if (internalCode != null) dto.InternalCode = internalCode;
                if (transferTypeId.HasValue) dto.TransferTypeId = transferTypeId;
                if (destinationTransactionId.HasValue) dto.DestinationTransactionId = destinationTransactionId;

                if (!string.IsNullOrWhiteSpace(mappingsJson))
                {
                    var maps = JArray.Parse(mappingsJson);
                    dto.AppTransactionSaveAsMappingList ??= new ObservableSet<AppTransactionSaveAsMappingExDto>();
                    foreach (var m in maps)
                    {
                        var mapId = m.Value<int?>("mappingId") ?? m.Value<int?>("id");
                        AppTransactionSaveAsMappingExDto mapDto = null;
                        if (mapId.HasValue)
                            mapDto = dto.AppTransactionSaveAsMappingList.FirstOrDefault(x => x.Id != null && Convert.ToInt32(x.Id) == mapId.Value);
                        if (mapDto == null)
                        {
                            mapDto = new AppTransactionSaveAsMappingExDto();
                            dto.AppTransactionSaveAsMappingList.Add(mapDto);
                        }
                        var src = m.Value<int?>("sourceFieldId") ?? m.Value<int?>("sourceFiledId");
                        var dest = m.Value<int?>("targetFieldId") ?? m.Value<int?>("targetFiledId");
                        if (src.HasValue) mapDto.SourceFiledId = src;
                        if (dest.HasValue) mapDto.TargetFiledId = dest;
                        if (m["name"] != null) mapDto.Name = m.Value<string>("name");
                    }
                }

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = dto.IsNew ? "create" : "update",
                            description = dto.Description,
                            destinationTransactionId = dto.DestinationTransactionId,
                            mappingCount = dto.AppTransactionSaveAsMappingList?.Count ?? 0
                        }
                    });
                }

                var result = AppTransactionDataTransferSettingBL.SaveOneAppTransactionDataTransferSettingExDto(dto);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save DataTransfer failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    saved = true,
                    dataTransferId = result?.Object?.Id ?? dto.Id,
                    changed = new[] { new { type = "dataTransfer", id = result?.Object?.Id ?? dto.Id } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string DeleteDataTransfer(int transactionId, int dataTransferId, bool? confirmed, bool? dryRun)
        {
            try
            {
                var dto = AppTransactionDataTransferSettingBL
                    .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                    .FirstOrDefault(d => d.Id != null && Convert.ToInt32(d.Id) == dataTransferId);
                if (dto == null) return Fail($"DataTransfer {dataTransferId} not found.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new { action = "delete", dataTransferId, description = dto.Description }
                    });
                }

                var result = AppTransactionDataTransferSettingBL.DeleteOneAppTransactionDataTransferSettingExDto(dataTransferId);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete DataTransfer failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    deleted = true,
                    dataTransferId,
                    changed = new[] { new { type = "dataTransfer", id = dataTransferId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string UpsertLinkedSearch(
            int transactionId,
            int unitId,
            int? linkedSearchId,
            string name,
            int? searchId,
            string changesJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                if (!UnitBelongsToTxn(transactionId, unitId))
                    return Fail($"unitId {unitId} is not on transaction {transactionId}.");

                AppTransactionUnitLinkedSearchExDto dto;
                if (linkedSearchId.HasValue && linkedSearchId.Value > 0)
                {
                    dto = AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchList(unitId)
                        .FirstOrDefault(x => x.Id != null && Convert.ToInt32(x.Id) == linkedSearchId.Value);
                    if (dto == null) return Fail($"LinkedSearch {linkedSearchId} not found on unit {unitId}.");
                }
                else
                {
                    if (!searchId.HasValue) return Fail("searchId is required for create.");
                    dto = new AppTransactionUnitLinkedSearchExDto
                    {
                        TransactionUnitId = unitId,
                        SearchId = searchId,
                        Name = name,
                        AppTransactionUnitSearchFieldMappingList = new ObservableSet<AppTransactionUnitSearchFieldMappingExDto>(),
                        AppTransactionUnitSearchViewFieldMappingList = new ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>()
                    };
                }

                if (name != null) dto.Name = name;
                if (searchId.HasValue) dto.SearchId = searchId;
                if (!string.IsNullOrWhiteSpace(changesJson))
                {
                    try
                    {
                        JsonConvert.PopulateObject(changesJson, dto);
                    }
                    catch (Exception ex) { return Fail("changesJson invalid: " + ex.Message); }
                }

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = dto.IsNew ? "create" : "update",
                            unitId,
                            name = dto.Name,
                            searchId = dto.SearchId
                        }
                    });
                }

                var result = AppTransactionUnitLinkedSearchBL.SaveAppTransactionUnitLinkedSearchExDto(dto);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save LinkedSearch failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    saved = true,
                    linkedSearchId = result?.Object?.Id ?? dto.Id,
                    changed = new[] { new { type = "linkedSearch", id = result?.Object?.Id ?? dto.Id } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string DeleteLinkedSearch(int transactionId, int linkedSearchId, bool? confirmed, bool? dryRun)
        {
            try
            {
                // Verify ownership via hierarchy
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);
                var units = EntityMapWiring.FlattenUnits(tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                var found = units.SelectMany(u => u.AppTransactionUnitLinkedSearchList
                        ?? Enumerable.Empty<AppTransactionUnitLinkedSearchExDto>())
                    .FirstOrDefault(ls => ls.Id != null && Convert.ToInt32(ls.Id) == linkedSearchId);
                if (found == null) return Fail($"LinkedSearch {linkedSearchId} not on transaction {transactionId}.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new { action = "delete", linkedSearchId, name = found.Name, searchId = found.SearchId }
                    });
                }

                var result = AppTransactionUnitLinkedSearchBL.DeleteAppTransactionUnitLinkedSearch(linkedSearchId);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete LinkedSearch failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    deleted = true,
                    linkedSearchId,
                    changed = new[] { new { type = "linkedSearch", id = linkedSearchId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static void ApplyCommandOptions(AppProjectWorkFlowActionExDto cmd, JObject jo)
        {
            if (jo["notificationMessage"] != null) cmd.NotificationMessage = jo.Value<string>("notificationMessage");
            if (jo["name"] != null) cmd.Name = jo.Value<string>("name");
            if (jo["actionType"] != null) cmd.ActionType = jo.Value<int?>("actionType");
            if (jo["sql"] != null || jo["sqlStatement"] != null)
                cmd.NotificationMessage = jo.Value<string>("sql") ?? jo.Value<string>("sqlStatement") ?? cmd.NotificationMessage;
            // Attribute flags commonly edited in Operation Task
            cmd.ActionAttribute ??= new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
            var attr = cmd.ActionAttribute;
            if (jo["linkToUI"] != null || jo["isPublic"] != null || jo["isPublicTask"] != null)
                attr.LinkToUI = jo.Value<bool?>("linkToUI") ?? jo.Value<bool?>("isPublic") ?? jo.Value<bool?>("isPublicTask");
            if (jo["isShowOnTopMenu"] != null) attr.IsShowOnTopMenu = jo.Value<bool?>("isShowOnTopMenu");
            if (jo["isAutoExecuteOnFormOpen"] != null) attr.IsAutoExecuteOnFormOpen = jo.Value<bool?>("isAutoExecuteOnFormOpen");
            if (jo["actionFlowOrder"] != null || jo["flowOrder"] != null)
                cmd.ActionFlowOrder = jo.Value<int?>("actionFlowOrder") ?? jo.Value<int?>("flowOrder");
            if (jo["linkTargetId"] != null) attr.LinkTargetId = jo.Value<int?>("linkTargetId");
            if (jo["linkedSearchId"] != null) attr.LinkedSearchId = jo.Value<int?>("linkedSearchId");
            if (jo["paramId1"] != null) attr.ParamId1 = jo.Value<int?>("paramId1"); // often DataTransfer / related id
            // Preserve unknown keys for advanced types via raw merge into ActionAttribute JSON if present
            if (jo["actionAttribute"] is JObject attrObj)
            {
                var existing = attr == null ? new JObject() : JObject.FromObject(attr);
                existing.Merge(attrObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                cmd.ActionAttribute = existing.ToObject<AppActionAttributeDto>();
            }
        }

        static object SummarizeCommand(AppProjectWorkFlowActionExDto c) => new
        {
            commandId = c.Id,
            name = c.Name ?? c.DisplayName,
            actionType = c.ActionType
        };

        static object DetailCommand(AppProjectWorkFlowActionExDto c) => new
        {
            commandId = c.Id,
            name = c.Name,
            displayName = c.DisplayName,
            actionType = c.ActionType,
            notificationMessage = c.NotificationMessage,
            commandTransactionId = c.CommandTransactionId,
            actionAttribute = c.ActionAttribute
        };

        static AppTransactionExDto LoadTx(int transactionId, out string error)
        {
            error = null;
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx == null) error = $"Transaction {transactionId} not found.";
            return tx;
        }

        static bool UnitBelongsToTxn(int transactionId, int unitId)
        {
            var tx = LoadTx(transactionId, out _);
            if (tx == null) return false;
            var units = EntityMapWiring.FlattenUnits(tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            return units.Any(u => Convert.ToInt32(u.Id) == unitId);
        }

        static string Ok(object payload)
        {
            var jo = JObject.FromObject(payload);
            jo["ok"] = true;
            jo["IsSuccess"] = true;
            return jo.ToString(Formatting.Indented);
        }

        static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });
    }
}
