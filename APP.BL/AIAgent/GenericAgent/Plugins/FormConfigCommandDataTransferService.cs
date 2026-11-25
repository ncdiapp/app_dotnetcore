using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using APP.Framework.Validation;
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

                var isCreate = !(commandId.HasValue && commandId.Value > 0);
                AppProjectWorkFlowActionExDto cmd;
                if (!isCreate)
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
                    // Same shape as UI "Add Operation" → CreateOneTransactionCommand (do not invent a parallel save path).
                    cmd = new AppProjectWorkFlowActionExDto
                    {
                        CommandTransactionId = transactionId,
                        ActionFlowOrder = maxOrder + 1,
                        ActionAttribute = new AppActionAttributeDto
                        {
                            ChildActionList = new List<ChildTransactionCommandDto>(),
                            LinkToUI = true,
                            IsShowOnTopMenu = true,
                            IsAutoExecuteOnFormOpen = false,
                            IsLogCommandStartEnd = false,
                            IsLogErrorDetails = true
                        }
                    };
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
                        ApplyCommandOptions(cmd, jo, tx.CommandActionList);
                        if (cmd.ActionType.HasValue && !AllowedCommandActionTypes.Contains(cmd.ActionType.Value))
                            return Fail($"actionType {cmd.ActionType.Value} from optionsJson is not a valid EmAppTransactionCommandType. Examples: Refresh=50, ExecuteSQLStatement=42.");
                    }
                    catch (Exception ex) { return Fail("optionsJson invalid: " + ex.Message); }
                }

                NormalizeSendMessageNotificationBodyNewlines(cmd);

                var formulaRescueNotes = NormalizeCommandFormulaStorage(cmd);

                var preview = new { action = isCreate ? "create" : "update", command = DetailCommand(cmd) };
                var warnings = BuildCommandSqlTokenWarnings(cmd.ActionType, cmd.NotificationMessage);
                warnings.AddRange(BuildCompositionChildWarnings(cmd));
                warnings.AddRange(formulaRescueNotes);
                warnings.AddRange(BuildCommandFormulaWarnings(cmd));
                warnings.AddRange(BuildPrintFromMessageTemplateWarnings(cmd));
                warnings.AddRange(BuildSendMessageDestinationWarnings(cmd));
                warnings.AddRange(BuildExecuteDataTransferWarnings(cmd));
                var navHard = BuildNavigateSearchHardWarnings(tx, cmd);
                var navSoft = BuildNavigateSearchSoftWarnings(cmd);
                warnings.AddRange(navHard);
                warnings.AddRange(navSoft);
                if (dryRun == true || confirmed != true)
                {
                    var next = (string)null;
                    if (navHard.Count > 0 || warnings.Count > 0)
                    {
                        if (cmd.ActionType == 57)
                            next = "Fix Command Formula: put expression in optionsJson.executionFormulaUI (ActionAttribute), use formulaToken + [Now]/[Today] — not notificationMessage / Format(Now()).";
                        else if (cmd.ActionType == 53 && navHard.Count > 0)
                            next = "Fix Navigate To Search: list_available_searches → upsert_linked_search on root unit → set optionsJson.linkedSearchId to returned linkedSearchId (not AppSearch.SearchId / invent 1).";
                        else if (cmd.ActionType == 53 && navSoft.Count > 0)
                            next = "Search filter←form field mapping uses upsert_linked_search filterMappingsJson (UI Search Mapping), not ParamId1.";
                        else if (cmd.ActionType == 74)
                            next = "Fix Print From Message Template: upsert_message_template → optionsJson.messageTemplateId (not notificationMessage body).";
                        else
                            next = "Fix SQL tokens/WHERE using find_fields / get_current_transaction_config.rootPrimaryKeys[].sqlToken before confirmed=true.";
                    }
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then call again with confirmed=true.",
                        proposed = preview,
                        warnings = warnings.Count > 0 ? warnings : null,
                        NextStep = next
                    });
                }

                // Hard-block orphan LinkedSearchId on Navigate To Search (53) — UI would show fake "Search {id}".
                if (navHard.Count > 0 && cmd.ActionType == (int)EmAppTransactionCommandType.OpenLinkedSearch)
                    return Fail(string.Join(" ", navHard));

                // Create path does not run SaveOneTransactionCommandActionList — format formula here.
                // Update path formats again inside SaveOneTransactionCommandActionList (safe / idempotent for UI tokens).
                if (cmd.ActionType == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                    PrepareCommandFormulaForPersist(tx, cmd);

                var expectedDataTransferSettingId = cmd.DataTransferSettingId;

                AppProjectWorkFlowActionExDto saved;
                if (isCreate)
                {
                    // UI Add Operation uses CreateOneTransactionCommand — keep Agent on that path only.
                    // Do NOT push a new row through SaveOneTransactionCommandActionList (different insert path).
                    var createResult = AppTransactionCommandBL.CreateOneTransactionCommand(cmd);
                    if (createResult?.ValidationResult?.HasErrors == true)
                    {
                        var msgs = createResult.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                        return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Create command failed.");
                    }
                    saved = createResult?.Object;
                    if (saved?.Id == null)
                        return Fail("Create command failed: no id returned.");
                }
                else
                {
                    // UI Save uses full-list SaveOneTransactionCommandActionList — same for Agent updates.
                    var result = AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
                    if (result?.ValidationResult?.HasErrors == true)
                    {
                        var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                        return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save command failed.");
                    }
                    AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                    var fresh = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                    saved = (fresh?.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                        .FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId.Value);
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);

                // Type 43: Agent previously reported saved while UI Data Transfer Setting stayed (None).
                if (cmd.ActionType == (int)EmAppTransactionCommandType.ExecuteFormDataTransfer
                    && expectedDataTransferSettingId != null
                    && Convert.ToInt32(expectedDataTransferSettingId) > 0)
                {
                    var persisted = saved?.DataTransferSettingId;
                    if (persisted == null || Convert.ToInt32(persisted) != Convert.ToInt32(expectedDataTransferSettingId))
                    {
                        return Fail(
                            "Command save did not persist DataTransferSettingId " +
                            $"(expected {expectedDataTransferSettingId}, got {persisted?.ToString() ?? "null"}). " +
                            "UI Data Transfer Setting would stay (None). Retry upsert_command or upsert_data_transfer(bindToCommandId=…).");
                    }
                }

                return Ok(new
                {
                    saved = true,
                    commandId = saved?.Id ?? commandId,
                    command = saved != null ? DetailCommand(saved) : null,
                    changed = new[] { new { type = "command", id = saved?.Id ?? commandId } },
                    NextStep = "Call request_form_reload. For actionType 43 confirm get_command_detail.dataTransferSettingId before claiming success."
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
                var dto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferId);
                if (dto == null || dto.Id == null)
                    return Fail($"DataTransfer {dataTransferId} not found.");
                if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId
                    && (!dto.DestinationTransactionId.HasValue || Convert.ToInt32(dto.DestinationTransactionId) != transactionId))
                {
                    // Setting must belong to open TX as source or destination.
                    var onTx = AppTransactionDataTransferSettingBL
                        .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                        .Any(d => d.Id != null && Convert.ToInt32(d.Id) == dataTransferId);
                    if (!onTx) return Fail($"DataTransfer {dataTransferId} not found on transaction {transactionId}.");
                }

                return Ok(new
                {
                    transactionId,
                    dataTransfer = SummarizeDataTransferDetail(dto),
                    mappingHints = BuildDataTransferMappingHints(dto),
                    sourceFields = BuildTransactionFieldCatalog(dto.TransactionId),
                    destinationFields = BuildTransactionFieldCatalog(dto.DestinationTransactionId),
                    fieldCatalogNote =
                        "For DM→DM mappings use sourceFields[].fieldId and destinationFields[].fieldId only. " +
                        "Never invent ids or reuse source ids as targetFieldId."
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
            bool? dryRun,
            int? bindToCommandId = null,
            bool? isNeedToOpenNewForm = null)
        {
            try
            {
                AppTransactionDataTransferSettingExDto dto;
                var isCreate = !(dataTransferId.HasValue && dataTransferId.Value > 0);
                if (!isCreate)
                {
                    dto = AppTransactionDataTransferSettingBL.RetrieveOneAppTransactionDataTransferSettingExDto(dataTransferId.Value);
                    if (dto == null || dto.Id == null) return Fail($"DataTransfer {dataTransferId} not found.");
                    var onTx = dto.TransactionId != null && Convert.ToInt32(dto.TransactionId) == transactionId;
                    if (!onTx)
                    {
                        onTx = AppTransactionDataTransferSettingBL
                            .RetrieveAllAppTransactionDataTransferSettingExDto(transactionId, null)
                            .Any(d => d.Id != null && Convert.ToInt32(d.Id) == dataTransferId.Value);
                    }
                    if (!onTx) return Fail($"DataTransfer {dataTransferId} not found on transaction {transactionId}.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(description)) return Fail("description is required for create.");
                    dto = new AppTransactionDataTransferSettingExDto
                    {
                        TransactionId = transactionId,
                        TransferTypeId = (int)EmAppTransactionDataTransferType.FromDataModelToDataModel,
                        AppTransactionSaveAsMappingList = new ObservableSet<AppTransactionSaveAsMappingExDto>()
                    };
                }

                if (description != null) dto.Description = description;
                if (internalCode != null) dto.InternalCode = internalCode;
                if (transferTypeId.HasValue)
                {
                    if (!IsValidTransferType(transferTypeId.Value))
                    {
                        return Fail(
                            "transferTypeId must be EmAppTransactionDataTransferType: " +
                            "1=FromDataModelToMessageTemplate, 2=FromDataModelToDataModel, " +
                            "3=FromApiToDataModel, 4=FromDataModelToApi.");
                    }
                    dto.TransferTypeId = transferTypeId;
                }
                else if (isCreate && !dto.TransferTypeId.HasValue)
                {
                    dto.TransferTypeId = (int)EmAppTransactionDataTransferType.FromDataModelToDataModel;
                }

                if (destinationTransactionId.HasValue) dto.DestinationTransactionId = destinationTransactionId;

                // Ensure source TransactionId stays on open form TX for types that use "from data model".
                var tType = dto.TransferTypeId ?? (int)EmAppTransactionDataTransferType.FromDataModelToDataModel;
                if (!dto.TransactionId.HasValue
                    || tType == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
                    || tType == (int)EmAppTransactionDataTransferType.FromDataModelToMessageTemplate
                    || tType == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
                {
                    dto.TransactionId = transactionId;
                }

                if ((tType == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
                     || tType == (int)EmAppTransactionDataTransferType.FromApiToDataModel)
                    && (!dto.DestinationTransactionId.HasValue || Convert.ToInt32(dto.DestinationTransactionId) <= 0))
                {
                    return Fail(
                        "destinationTransactionId is required for transferType " + tType +
                        " (To Data Model). Pass the Campaign/target transaction id — do not invent field ids.");
                }

                string mappingApplyNote = null;
                if (mappingsJson != null)
                {
                    JArray maps;
                    try { maps = JArray.Parse(string.IsNullOrWhiteSpace(mappingsJson) ? "[]" : mappingsJson); }
                    catch (Exception ex) { return Fail("mappingsJson invalid: " + ex.Message); }

                    dto.AppTransactionSaveAsMappingList ??= new ObservableSet<AppTransactionSaveAsMappingExDto>();
                    // Full replace (including []): Clear() marks prior rows deleted for ProcessDirtyDto.
                    dto.AppTransactionSaveAsMappingList.Clear();
                    foreach (var m in maps)
                    {
                        if (m == null || m.Type == JTokenType.Null) continue;
                        if (m.Value<bool?>("delete") == true) continue;

                        var mapDto = new AppTransactionSaveAsMappingExDto
                        {
                            TransactionId = dto.TransactionId ?? transactionId,
                            SourceFiledId = m.Value<int?>("sourceFieldId") ?? m.Value<int?>("sourceFiledId"),
                            TargetFiledId = m.Value<int?>("targetFieldId") ?? m.Value<int?>("targetFiledId"),
                            MappingUnitId = m.Value<int?>("mappingUnitId"),
                            Name = m.Value<string>("name") ?? "",
                            JsonPropertyPathName = m.Value<string>("jsonPropertyPathName")
                                ?? m.Value<string>("jsonPath")
                                ?? "",
                            IsBlankTargetField = m.Value<bool?>("isBlankTargetField")
                                ?? m.Value<bool?>("isResponseMapping")
                                ?? false
                        };
                        // Runtime transfer keys by MappingUnitId (= source field's unit). Infer when Agent omits it.
                        if (!mapDto.MappingUnitId.HasValue && mapDto.SourceFiledId.HasValue)
                        {
                            try
                            {
                                var srcTxId = dto.TransactionId ?? transactionId;
                                AppCacheManagerBL.RefreshOnetHierarchyTranscation(srcTxId);
                                var srcTx = AppTransactionBL.GetHierarchyTranscationFromDatabase(srcTxId);
                                if (srcTx?.DictAllTransactionField != null
                                    && srcTx.DictAllTransactionField.TryGetValue(mapDto.SourceFiledId.Value, out var sf)
                                    && sf?.TransactionUnitId != null)
                                    mapDto.MappingUnitId = sf.TransactionUnitId;
                            }
                            catch { /* leave null; runtime EnsureMappingUnitIdsFromSourceFields also fills */ }
                        }
                        dto.AppTransactionSaveAsMappingList.Add(mapDto);
                    }
                    mappingApplyNote = maps.Count == 0
                        ? "Cleared all mappings."
                        : $"Replaced mappings with {maps.Count} row(s).";
                }

                dto.IsModified = true;

                var mappingErrors = ValidateDataTransferFieldMappings(dto);
                if (mappingErrors.Count > 0)
                {
                    return Fail(
                        string.Join(" ", mappingErrors) +
                        " Never invent targetFieldId or copy sourceFieldId. " +
                        "Resolve destination fields via get_data_transfer_detail.destinationFields " +
                        "or find_fields(name, transactionId=destinationTransactionId).");
                }

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = isCreate || dto.IsNew ? "create" : "update",
                            description = dto.Description,
                            transferTypeId = dto.TransferTypeId,
                            transferType = TransferTypeName(dto.TransferTypeId),
                            internalCode = dto.InternalCode,
                            destinationTransactionId = dto.DestinationTransactionId,
                            mappingCount = dto.AppTransactionSaveAsMappingList?.Count ?? 0,
                            mappingApplyNote,
                            bindToCommandId,
                            isNeedToOpenNewForm,
                            mappings = (dto.AppTransactionSaveAsMappingList ?? new ObservableSet<AppTransactionSaveAsMappingExDto>())
                                .Select(SummarizeMapping).ToList()
                        },
                        mappingHints = BuildDataTransferMappingHints(dto),
                        destinationFields = BuildTransactionFieldCatalog(dto.DestinationTransactionId),
                        sourceFields = BuildTransactionFieldCatalog(dto.TransactionId ?? transactionId),
                        NextStep = bindToCommandId.HasValue && bindToCommandId.Value > 0
                            ? "After confirm, this will also bind dataTransferSettingId onto command " + bindToCommandId.Value + "."
                            : "After confirm, pass bindToCommandId=<Execute Data Transfer command id> (or upsert_command optionsJson.dataTransferSettingId) — creating the setting alone leaves UI Data Transfer Setting as (None)."
                    });
                }

                var result = AppTransactionDataTransferSettingBL.SaveOneAppTransactionDataTransferSettingExDto(dto);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save DataTransfer failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                var savedId = result?.Object?.Id ?? dto.Id;
                var savedIdInt = Convert.ToInt32(savedId);

                object commandBind = null;
                string nextStep =
                    "Creating/updating a Data Transfer setting does NOT set Operation→Data Transfer Setting (UI stays None). " +
                    "REQUIRED next: upsert_command(commandId=<Execute Data Transfer cmd>, actionType=43, " +
                    "optionsJson:{dataTransferSettingId:" + savedIdInt + ", isNeedToOpenNewForm:true}, confirmed=true) " +
                    "OR re-call upsert_data_transfer with bindToCommandId=<cmdId>. " +
                    "Then get_command_detail and confirm dataTransferSettingId=" + savedIdInt + " before claiming success. " +
                    "Finally request_form_reload.";

                if (bindToCommandId.HasValue && bindToCommandId.Value > 0)
                {
                    var bindResult = BindDataTransferSettingToCommand(
                        transactionId,
                        bindToCommandId.Value,
                        savedIdInt,
                        isNeedToOpenNewForm ?? true);
                    if (!bindResult.Ok)
                        return Fail(
                            "DataTransfer " + savedIdInt + " was saved, but binding to command " +
                            bindToCommandId.Value + " failed: " + bindResult.Error);
                    commandBind = bindResult.Detail;
                    nextStep =
                        "Bound dataTransferSettingId=" + savedIdInt + " on command " + bindToCommandId.Value +
                        ". Call request_form_reload. Do not claim success unless get_command_detail shows that id.";
                }

                return Ok(new
                {
                    saved = true,
                    dataTransferId = savedId,
                    mappingApplyNote,
                    commandBind,
                    changed = new[] { new { type = "dataTransfer", id = savedId } },
                    NextStep = nextStep
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        /// <summary>
        /// Sets command.DataTransferSettingId (UI Data Transfer Setting column) and optional Need To Open Form.
        /// Verifies the column actually persisted — Agent previously claimed success while UI stayed (None).
        /// </summary>
        internal static (bool Ok, string Error, object Detail) BindDataTransferSettingToCommand(
            int transactionId,
            int commandId,
            int dataTransferSettingId,
            bool isNeedToOpenNewForm)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return (false, err ?? "Transaction not found.", null);
                tx.CommandActionList ??= new List<AppProjectWorkFlowActionExDto>();
                var cmd = tx.CommandActionList.FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId);
                if (cmd == null) return (false, $"Command {commandId} not found on transaction {transactionId}.", null);

                cmd.DataTransferSettingId = dataTransferSettingId;
                cmd.ActionAttribute ??= new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
                cmd.ActionAttribute.IsNeedToOpenNewForm = isNeedToOpenNewForm;
                if (cmd.ActionType == null || Convert.ToInt32(cmd.ActionType) != (int)EmAppTransactionCommandType.ExecuteFormDataTransfer)
                    cmd.ActionType = (int)EmAppTransactionCommandType.ExecuteFormDataTransfer;

                var saveResult = AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
                if (saveResult?.ValidationResult?.HasErrors == true)
                {
                    var msgs = saveResult.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return (false, msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save command failed.", null);
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                var fresh = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                var saved = (fresh?.CommandActionList ?? new List<AppProjectWorkFlowActionExDto>())
                    .FirstOrDefault(c => c.Id != null && Convert.ToInt32(c.Id) == commandId);
                if (saved == null)
                    return (false, $"Command {commandId} missing after save.", null);
                if (saved.DataTransferSettingId == null
                    || Convert.ToInt32(saved.DataTransferSettingId) != dataTransferSettingId)
                {
                    return (false,
                        "Save returned OK but command.DataTransferSettingId did not persist " +
                        $"(expected {dataTransferSettingId}, got {saved.DataTransferSettingId?.ToString() ?? "null"}). " +
                        "UI Data Transfer Setting would stay (None).",
                        null);
                }

                return (true, null, new
                {
                    commandId,
                    dataTransferSettingId = saved.DataTransferSettingId,
                    isNeedToOpenNewForm = saved.ActionAttribute?.IsNeedToOpenNewForm
                });
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        static bool IsValidTransferType(int typeId) =>
            typeId == (int)EmAppTransactionDataTransferType.FromDataModelToMessageTemplate
            || typeId == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
            || typeId == (int)EmAppTransactionDataTransferType.FromApiToDataModel
            || typeId == (int)EmAppTransactionDataTransferType.FromDataModelToApi;

        static string TransferTypeName(int? typeId)
        {
            if (!typeId.HasValue) return null;
            return typeId.Value switch
            {
                (int)EmAppTransactionDataTransferType.FromDataModelToMessageTemplate => "FromDataModelToMessageTemplate",
                (int)EmAppTransactionDataTransferType.FromDataModelToDataModel => "FromDataModelToDataModel",
                (int)EmAppTransactionDataTransferType.FromApiToDataModel => "FromApiToDataModel",
                (int)EmAppTransactionDataTransferType.FromDataModelToApi => "FromDataModelToApi",
                _ => "Unknown"
            };
        }

        static object SummarizeMapping(AppTransactionSaveAsMappingExDto m) => new
        {
            mappingId = m.Id,
            sourceFieldId = m.SourceFiledId,
            targetFieldId = m.TargetFiledId,
            mappingUnitId = m.MappingUnitId,
            name = m.Name,
            jsonPropertyPathName = m.JsonPropertyPathName,
            isBlankTargetField = m.IsBlankTargetField,
            isResponseMapping = m.IsBlankTargetField == true
        };

        static object SummarizeDataTransferDetail(AppTransactionDataTransferSettingExDto dto) => new
        {
            dataTransferId = dto.Id,
            description = dto.Description,
            internalCode = dto.InternalCode,
            transferTypeId = dto.TransferTypeId,
            transferType = TransferTypeName(dto.TransferTypeId),
            transactionId = dto.TransactionId,
            destinationTransactionId = dto.DestinationTransactionId,
            targetApiOperationId = dto.TargetApiOperationId,
            targetApiInputParameters = dto.TargetApiInputParameterList,
            mappings = (dto.AppTransactionSaveAsMappingList ?? new ObservableSet<AppTransactionSaveAsMappingExDto>())
                .Select(SummarizeMapping).ToList(),
            requestMappings = (dto.DataTransferMappingList ?? new List<AppTransactionSaveAsMappingExDto>())
                .Select(SummarizeMapping).ToList(),
            responseMappings = (dto.ApiResponseMappingList ?? new List<AppTransactionSaveAsMappingExDto>())
                .Select(SummarizeMapping).ToList()
        };

        static object BuildDataTransferMappingHints(AppTransactionDataTransferSettingExDto dto)
        {
            var typeId = dto?.TransferTypeId;
            return new
            {
                transferTypeId = typeId,
                transferType = TransferTypeName(typeId),
                mappingsJsonIsFullReplace =
                    "Pass mappingsJson as a JSON array to fully replace mappings; pass [] to clear. Omit mappingsJson to leave mappings unchanged.",
                rowShape = new
                {
                    sourceFieldId = "Source transaction field id (DDL Id). Required for most FromDataModel* rows.",
                    targetFieldId = "Destination transaction field id (DM↔DM / API response→field). MUST belong to destinationTransactionId — never invent or copy sourceFieldId.",
                    name = "API input param name (path/query/env) OR message-template token; also used for mapToInputParam.",
                    jsonPropertyPathName = "Absolute JSON path for API payload/response leaf (e.g. order.customerId).",
                    isBlankTargetField = "true = API response mapping (IsBlankTargetField); false = request/payload/DM mapping.",
                    mappingUnitId = "Optional unit id when mapping is unit-scoped.",
                    delete = "Ignored under full-replace (omit row instead)."
                },
                byType = new
                {
                    FromDataModelToDataModel = "Rows: sourceFieldId (from TX) + targetFieldId (destination TX). transferTypeId=2, destinationTransactionId required. Load destinationFields from get_data_transfer_detail / upsert preview before mapping.",
                    FromDataModelToMessageTemplate = "Rows: sourceFieldId + name (template token). transferTypeId=1. internalCode may identify template.",
                    FromApiToDataModel = "Rows: jsonPropertyPathName (API) + targetFieldId (DM). transferTypeId=3. internalCode = API operation id. destinationTransactionId = target DM.",
                    FromDataModelToApi = "Rows mix: (1) name=API input param + sourceFieldId; (2) jsonPropertyPathName + sourceFieldId for payload; (3) isBlankTargetField=true + jsonPropertyPathName + targetFieldId for response. transferTypeId=4. internalCode = API operation id. Call get_data_transfer_detail after create to see targetApiInputParameters / structure."
                },
                targetApiInputParameters = dto?.TargetApiInputParameterList,
                hasPayloadStructure = dto?.TargetApiPayloadDataStructure != null,
                hasResponseStructure = dto?.TargetApiResponseDataStructure != null
            };
        }

        /// <summary>
        /// Compact field list for agent mapping (prevents inventing targetFieldId from wrong transactions).
        /// </summary>
        static List<object> BuildTransactionFieldCatalog(int? transactionId)
        {
            var list = new List<object>();
            if (!transactionId.HasValue || transactionId.Value <= 0) return list;
            try
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId.Value);
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId.Value);
                if (tx == null) return list;
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                foreach (var u in units)
                {
                    foreach (var f in u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                    {
                        if (f?.Id == null) continue;
                        list.Add(new
                        {
                            fieldId = Convert.ToInt32(f.Id),
                            name = f.DisplayName ?? f.DataBaseFieldName,
                            dbName = f.DataBaseFieldName,
                            unitName = u.UnitDisplayName
                        });
                    }
                }
            }
            catch { /* return empty catalog */ }
            return list;
        }

        static HashSet<int> BuildTransactionFieldIdSetCore(int? transactionId)
        {
            var set = new HashSet<int>();
            if (!transactionId.HasValue || transactionId.Value <= 0) return set;
            try
            {
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId.Value);
                if (tx?.DictAllTransactionField == null) return set;
                foreach (var id in tx.DictAllTransactionField.Keys)
                    set.Add(id);
            }
            catch { /* empty */ }
            return set;
        }

        /// <summary>
        /// Agents invent targetFieldId (copy source id or wrong TX ids) — reject before save/preview.
        /// </summary>
        static List<string> ValidateDataTransferFieldMappings(AppTransactionDataTransferSettingExDto dto)
        {
            var errors = new List<string>();
            if (dto == null) return errors;
            var maps = dto.AppTransactionSaveAsMappingList;
            if (maps == null || maps.Count == 0) return errors;

            var tType = dto.TransferTypeId ?? 0;
            var srcIds = BuildTransactionFieldIdSetCore(dto.TransactionId);
            var destIds = BuildTransactionFieldIdSetCore(dto.DestinationTransactionId);

            var badSrc = new List<int>();
            var badDest = new List<int>();
            var copied = new List<string>();
            var i = 0;
            foreach (var m in maps)
            {
                i++;
                if (m == null) continue;

                if ((tType == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
                     || tType == (int)EmAppTransactionDataTransferType.FromDataModelToMessageTemplate
                     || tType == (int)EmAppTransactionDataTransferType.FromDataModelToApi)
                    && m.SourceFiledId.HasValue && m.SourceFiledId.Value > 0
                    && srcIds.Count > 0 && !srcIds.Contains(m.SourceFiledId.Value))
                    badSrc.Add(m.SourceFiledId.Value);

                var isResp = m.IsBlankTargetField == true;

                if (tType == (int)EmAppTransactionDataTransferType.FromDataModelToDataModel
                    && m.TargetFiledId.HasValue && m.TargetFiledId.Value > 0)
                {
                    if (destIds.Count > 0 && !destIds.Contains(m.TargetFiledId.Value))
                        badDest.Add(m.TargetFiledId.Value);
                    if (m.SourceFiledId.HasValue && m.SourceFiledId.Value == m.TargetFiledId.Value
                        && dto.TransactionId.HasValue && dto.DestinationTransactionId.HasValue
                        && Convert.ToInt32(dto.TransactionId) != Convert.ToInt32(dto.DestinationTransactionId))
                        copied.Add($"row{i}:sourceFieldId=targetFieldId={m.SourceFiledId.Value}");
                }
                else if (tType == (int)EmAppTransactionDataTransferType.FromApiToDataModel
                         && m.TargetFiledId.HasValue && m.TargetFiledId.Value > 0
                         && destIds.Count > 0 && !destIds.Contains(m.TargetFiledId.Value))
                {
                    badDest.Add(m.TargetFiledId.Value);
                }
                else if (tType == (int)EmAppTransactionDataTransferType.FromDataModelToApi
                         && isResp && m.TargetFiledId.HasValue && m.TargetFiledId.Value > 0
                         && srcIds.Count > 0 && !srcIds.Contains(m.TargetFiledId.Value))
                {
                    badDest.Add(m.TargetFiledId.Value);
                }
            }

            if (badSrc.Count > 0)
                errors.Add(
                    "Invalid sourceFieldId(s) not on source transaction " + dto.TransactionId + ": " +
                    string.Join(",", badSrc.Distinct()) + ".");
            if (badDest.Count > 0)
                errors.Add(
                    "Invalid targetFieldId(s) not on destination transaction " + dto.DestinationTransactionId + ": " +
                    string.Join(",", badDest.Distinct()) +
                    ". UI shows raw numbers when id is missing from To Data Model.");
            if (copied.Count > 0)
                errors.Add(
                    "DM→DM cannot use the same id for source and target across different transactions (" +
                    string.Join("; ", copied) + ").");

            return errors;
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

        public static string ListAvailableSearches(int transactionId, string nameContains, int? take)
        {
            try
            {
                var tx = LoadTx(transactionId, out var err);
                if (tx == null) return Fail(err);

                var appId = tx.SaasApplicationId;
                List<AppSearchDto> list = appId.HasValue
                    ? AppSearchConfigBL.RetrieveSaasApplicationSearchList(appId.Value)
                    : AppSearchConfigBL.RetrieveAllAppSearchDto();

                IEnumerable<AppSearchDto> q = list ?? new List<AppSearchDto>();
                if (!string.IsNullOrWhiteSpace(nameContains))
                {
                    var needle = nameContains.Trim();
                    q = q.Where(s =>
                        (!string.IsNullOrEmpty(s.Name) && s.Name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (s.Id != null && s.Id.ToString().IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                var limit = take.HasValue && take.Value > 0 ? Math.Min(take.Value, 100) : 40;
                var items = q
                    .OrderBy(s => s.Name ?? "")
                    .ThenBy(s => Convert.ToInt32(s.Id ?? 0))
                    .Take(limit)
                    .Select(s => new
                    {
                        searchId = s.Id,
                        name = s.Name,
                        description = s.Description,
                        saasApplicationId = s.SaasApplicationId,
                        dataSetId = s.DataSetId,
                        searchViewId = s.SearchViewId
                    })
                    .ToList();

                return Ok(new
                {
                    transactionId,
                    saasApplicationId = appId,
                    count = items.Count,
                    truncated = (list?.Count ?? 0) > items.Count,
                    searches = items,
                    note = "Use searchId (AppSearch.SearchId — not dataSetId) with upsert_linked_search(unitId, searchId, name). " +
                           "upsert_linked_search auto-fills SearchViewId from the search default view. " +
                           "After save, use the returned linkedSearchId on upsert_command actionType 53 — " +
                           "do not put searchId into linkedSearchId."
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
            int? action,
            string changesJson,
            string filterMappingsJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                if (!UnitBelongsToTxn(transactionId, unitId))
                    return Fail($"unitId {unitId} is not on transaction {transactionId}.");

                var existingOnUnit = AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchList(unitId)
                    ?? new ObservableSet<AppTransactionUnitLinkedSearchExDto>();

                AppTransactionUnitLinkedSearchExDto dto;
                int? preservedLinkedSearchId = null;
                if (linkedSearchId.HasValue && linkedSearchId.Value > 0)
                {
                    dto = existingOnUnit.FirstOrDefault(x => x.Id != null && Convert.ToInt32(x.Id) == linkedSearchId.Value);
                    if (dto == null)
                        return Fail(
                            $"LinkedSearch {linkedSearchId} not found on unit {unitId}. " +
                            "Call get_linked_searches and use an existing linkedSearchId — do not invent ids.");
                    preservedLinkedSearchId = linkedSearchId.Value;
                }
                else
                {
                    if (!searchId.HasValue)
                        return Fail("searchId is required for create. Call list_available_searches and pass a real AppSearch.SearchId (not dataSetId).");

                    // Reuse existing binding for the same AppSearch on this unit (avoid duplicate "Opportunity Search" + orphan Search N).
                    var reuse = existingOnUnit.FirstOrDefault(x =>
                        x.SearchId == searchId
                        || (!string.IsNullOrWhiteSpace(name)
                            && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
                    if (reuse != null)
                    {
                        dto = reuse;
                        preservedLinkedSearchId = Convert.ToInt32(reuse.Id);
                    }
                    else
                    {
                        // Match UI create defaults: Action=3 (Open Search), UsageType=102, Sort next.
                        dto = new AppTransactionUnitLinkedSearchExDto
                        {
                            TransactionUnitId = unitId,
                            SearchId = searchId,
                            Name = name,
                            Action = (int)EmAppLinkedSearchAction.ViewSearchResult,
                            UsageType = 102,
                            Sort = NextLinkedSearchSort(unitId),
                            AppTransactionUnitSearchFieldMappingList = new ObservableSet<AppTransactionUnitSearchFieldMappingExDto>(),
                            AppTransactionUnitSearchViewFieldMappingList = new ObservableSet<AppTransactionUnitSearchViewFieldMappingExDto>()
                        };
                    }
                }

                if (name != null) dto.Name = name;
                if (searchId.HasValue) dto.SearchId = searchId;
                if (action.HasValue) dto.Action = action;

                // Agents often nest filterMappings inside changesJson instead of filterMappingsJson.
                string effectiveFilterMappingsJson = filterMappingsJson;
                if (!string.IsNullOrWhiteSpace(changesJson))
                {
                    try
                    {
                        var jo = JObject.Parse(changesJson);
                        if (string.IsNullOrWhiteSpace(effectiveFilterMappingsJson))
                        {
                            var nested = jo["filterMappingsJson"] ?? jo["filterMappings"]
                                ?? jo["appTransactionUnitSearchFieldMappingList"]
                                ?? jo["AppTransactionUnitSearchFieldMappingList"];
                            if (nested != null)
                                effectiveFilterMappingsJson = nested.Type == JTokenType.String
                                    ? nested.Value<string>()
                                    : nested.ToString(Formatting.None);
                        }

                        // Strip mapping collections / ids so PopulateObject cannot wipe Id → IsNew=true (duplicate insert).
                        jo.Remove("filterMappingsJson");
                        jo.Remove("filterMappings");
                        jo.Remove("appTransactionUnitSearchFieldMappingList");
                        jo.Remove("AppTransactionUnitSearchFieldMappingList");
                        jo.Remove("id");
                        jo.Remove("Id");
                        jo.Remove("ID");

                        var safeJson = jo.ToString(Formatting.None);
                        if (jo.Properties().Any())
                            JsonConvert.PopulateObject(safeJson, dto);
                    }
                    catch (Exception ex) { return Fail("changesJson invalid: " + ex.Message); }
                }

                // Keep identity + unit ownership even if changesJson tried to overwrite them.
                dto.TransactionUnitId = unitId;
                if (preservedLinkedSearchId.HasValue)
                    dto.Id = preservedLinkedSearchId.Value;

                if (!dto.SearchId.HasValue || dto.SearchId.Value <= 0)
                    return Fail("SearchId is required. Call list_available_searches for a real AppSearch.SearchId (not dataSetId).");

                // Required by ValidateDto — empty Message historically hid this from agents.
                if (!dto.Action.HasValue)
                    dto.Action = (int)EmAppLinkedSearchAction.ViewSearchResult;
                if (!dto.UsageType.HasValue)
                    dto.UsageType = 102;
                if (!dto.Sort.HasValue || dto.Sort.Value <= 0)
                    dto.Sort = dto.IsNew ? NextLinkedSearchSort(unitId) : 1;

                AppSearchExDto searchExists;
                try
                {
                    searchExists = AppSearchConfigBL.RetrieveOneAppSearchExDto(dto.SearchId.Value);
                }
                catch (Exception ex)
                {
                    return Fail(
                        $"searchId {dto.SearchId} lookup failed: {ex.Message}. " +
                        "Use searchId from list_available_searches — never dataSetId / transactionId / unitId.");
                }

                // FetchEntity on a missing PK still returns a hollow entity (IsNew=true, no Name/SearchViewId).
                if (searchExists == null || searchExists.Id == null || searchExists.IsNew
                    || (!searchExists.SearchViewId.HasValue && string.IsNullOrWhiteSpace(searchExists.Name)))
                {
                    return Fail(
                        $"searchId {dto.SearchId} is not a valid AppSearch. " +
                        "Call list_available_searches and use returned searchId — " +
                        "do not pass dataSetId, invent 1, or reuse transactionId/unitId.");
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                    dto.Name = searchExists.Name ?? $"Search {dto.SearchId}";

                // DTO SearchViewId is non-nullable int (defaults 0); DB column is NOT NULL + FK to AppSearchView.
                // Agent defaults to the AppSearch's SearchViewId (UI requires an explicit view pick).
                if (dto.SearchViewId <= 0)
                {
                    if (!searchExists.SearchViewId.HasValue || searchExists.SearchViewId.Value <= 0)
                    {
                        return Fail(
                            $"AppSearch {dto.SearchId} ({searchExists.Name}) has no SearchViewId; " +
                            "cannot create unit Linked Search until the search has a default view.");
                    }
                    dto.SearchViewId = searchExists.SearchViewId.Value;
                }

                List<object> appliedFilterMappings = null;
                if (!string.IsNullOrWhiteSpace(effectiveFilterMappingsJson))
                {
                    if (!TryApplyFilterMappings(dto, unitId, searchExists, effectiveFilterMappingsJson, out appliedFilterMappings, out var mapErr))
                        return Fail(mapErr);
                    // Force ProcessDirty path on update (scalar-only save branch is a no-op in BL).
                    dto.IsModified = true;
                    if (preservedLinkedSearchId.HasValue)
                        dto.Id = preservedLinkedSearchId.Value;
                }

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            op = dto.IsNew ? "create" : "update",
                            linkedSearchId = dto.Id,
                            unitId,
                            name = dto.Name,
                            searchId = dto.SearchId,
                            searchName = searchExists.Name,
                            searchViewId = dto.SearchViewId,
                            action = dto.Action,
                            actionName = "ViewSearchResult/Open Search (3)",
                            usageType = dto.UsageType,
                            filterMappings = appliedFilterMappings
                                ?? SummarizeFilterMappings(dto.AppTransactionUnitSearchFieldMappingList)
                        },
                        note = "If op=update, keep this linkedSearchId on upsert_command (53). " +
                               "Filter→form mappings belong here (filterMappingsJson), NOT command ParamId1. Do not create duplicates."
                    });
                }

                APP.Framework.Communication.OperationCallResult<AppTransactionUnitLinkedSearchExDto> result;
                try
                {
                    result = AppTransactionUnitLinkedSearchBL.SaveAppTransactionUnitLinkedSearchExDto(dto);
                }
                catch (Exception saveEx)
                {
                    // SaveEntity may have committed before SynchronizeDatabaseTableAndUpdateCahce threw OutOfSync.
                    var recovered = TryFindLinkedSearchOnUnit(unitId, dto.SearchId, dto.Name);
                    if (recovered != null)
                    {
                        try { AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId); } catch { /* best effort */ }
                        var recoveredId = recovered.Id;
                        return Ok(new
                        {
                            saved = true,
                            recoveredAfterSyncError = true,
                            linkedSearchId = recoveredId,
                            searchId = dto.SearchId,
                            searchViewId = dto.SearchViewId,
                            name = dto.Name,
                            action = dto.Action,
                            unitId,
                            syncWarning = ShortExceptionMessage(saveEx),
                            changed = new[] { new { type = "linkedSearch", id = recoveredId } },
                            NextStep = "For Navigate To Search (53): upsert_command with optionsJson.linkedSearchId=" + recoveredId + ", then request_form_reload."
                        });
                    }
                    return Fail(ShortExceptionMessage(saveEx));
                }

                if (result?.ValidationResult?.HasErrors == true)
                {
                    var detail = FormatValidationErrors(result.ValidationResult);
                    return Fail(
                        detail + " Required: Name, SearchId (from list_available_searches), Action (default 3), SearchViewId (auto from AppSearch). " +
                        "Do not pass dataSetId as searchId.");
                }

                var savedId = result?.Object?.Id ?? dto.Id;
                var savedMappings = SummarizeFilterMappings(
                    result?.Object?.AppTransactionUnitSearchFieldMappingList
                    ?? dto.AppTransactionUnitSearchFieldMappingList);
                try { AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId); }
                catch (Exception refreshEx)
                {
                    // Row is saved; cache refresh failure must not hide success from the agent.
                    return Ok(new
                    {
                        saved = true,
                        linkedSearchId = savedId,
                        searchId = dto.SearchId,
                        searchViewId = dto.SearchViewId,
                        name = dto.Name,
                        action = dto.Action,
                        unitId,
                        filterMappings = savedMappings,
                        cacheRefreshWarning = ShortExceptionMessage(refreshEx),
                        changed = new[] { new { type = "linkedSearch", id = savedId } },
                        NextStep = "For Navigate To Search (53): upsert_command with optionsJson.linkedSearchId=" + savedId + ", then request_form_reload."
                    });
                }

                return Ok(new
                {
                    saved = true,
                    linkedSearchId = savedId,
                    searchId = dto.SearchId,
                    searchViewId = dto.SearchViewId,
                    name = dto.Name,
                    action = dto.Action,
                    unitId,
                    filterMappings = savedMappings,
                    changed = new[] { new { type = "linkedSearch", id = savedId } },
                    NextStep = "For Navigate To Search (53): upsert_command with optionsJson.linkedSearchId=" + savedId + ", then request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ShortExceptionMessage(ex)); }
        }

        /// <summary>
        /// UI "Search Mapping: Set Search Filter Values From Form" → AppTransactionUnitSearchFieldMapping.
        /// JSON: [{"searchFieldId":35,"transactionFieldId":3500}] or
        /// [{"searchFilter":"Customer","formField":"CustomerId"}] (names / db names / display text).
        /// </summary>
        static bool TryApplyFilterMappings(
            AppTransactionUnitLinkedSearchExDto dto,
            int unitId,
            AppSearchExDto search,
            string filterMappingsJson,
            out List<object> applied,
            out string error)
        {
            applied = null;
            error = null;
            JToken root;
            try { root = JToken.Parse(filterMappingsJson); }
            catch (Exception ex)
            {
                error = "filterMappingsJson invalid JSON: " + ex.Message;
                return false;
            }

            if (root is not JArray arr)
            {
                error = "filterMappingsJson must be a JSON array of {searchFieldId, transactionFieldId} " +
                        "or {searchFilter, formField}. Pass [] to clear all Search Mapping rows (undo) — " +
                        "do NOT delete_linked_search.";
                return false;
            }

            dto.AppTransactionUnitSearchFieldMappingList ??=
                new ObservableSet<AppTransactionUnitSearchFieldMappingExDto>();
            var list = dto.AppTransactionUnitSearchFieldMappingList;

            // Empty array = clear all filter mappings on this Linked Search (keep the binding + command link).
            if (arr.Count == 0)
            {
                list.Clear();
                applied = new List<object>();
                return true;
            }

            var searchFields = search.AppSearchFieldList ?? new ObservableSet<AppSearchFieldExDto>();
            var unitFields = ResolveUnitFields(unitId);
            applied = new List<object>();

            foreach (var token in arr)
            {
                if (token is not JObject jo)
                {
                    error = "Each filterMappingsJson item must be an object.";
                    return false;
                }

                int searchFieldId = jo.Value<int?>("searchFieldId")
                    ?? jo.Value<int?>("SearchFieldId")
                    ?? 0;
                int transactionFieldId = jo.Value<int?>("transactionFieldId")
                    ?? jo.Value<int?>("TransactionFieldId")
                    ?? jo.Value<int?>("formFieldId")
                    ?? 0;

                var searchFilterName = jo.Value<string>("searchFilter")
                    ?? jo.Value<string>("searchField")
                    ?? jo.Value<string>("filter")
                    ?? jo.Value<string>("criteria");
                var formFieldName = jo.Value<string>("formField")
                    ?? jo.Value<string>("transactionField")
                    ?? jo.Value<string>("field");

                if (searchFieldId <= 0)
                {
                    var sf = ResolveSearchFilterField(searchFields, searchFilterName);
                    if (sf?.Id == null)
                    {
                        var available = string.Join(", ", searchFields
                            .Where(f => f?.Id != null)
                            .Select(f => $"{f.Id}:{f.DisplayText ?? f.SysTableFiledPath}"));
                        error = $"Could not resolve search filter '{searchFilterName}'. " +
                                $"Available filters on search {search.Id}: {available}";
                        return false;
                    }
                    searchFieldId = Convert.ToInt32(sf.Id);
                }
                else
                {
                    var sf = searchFields.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == searchFieldId);
                    if (sf == null)
                    {
                        error = $"searchFieldId {searchFieldId} is not a criteria field on AppSearch {search.Id}. " +
                                "Use get_linked_searches (availableSearchFilters) or DisplayText/SysTableFiledPath names.";
                        return false;
                    }
                }

                if (transactionFieldId <= 0)
                {
                    var tf = ResolveUnitFormField(unitFields, formFieldName);
                    if (tf?.Id == null)
                    {
                        error = $"Could not resolve form field '{formFieldName}' on unit {unitId}. " +
                                "Use find_fields / get_unit_fields (CustomerId → fieldId).";
                        return false;
                    }
                    transactionFieldId = Convert.ToInt32(tf.Id);
                }
                else
                {
                    var tf = unitFields.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == transactionFieldId);
                    if (tf == null)
                    {
                        error = $"transactionFieldId {transactionFieldId} is not a field on unit {unitId}.";
                        return false;
                    }
                }

                var existing = list.FirstOrDefault(m => m.SearchFieldId == searchFieldId);
                if (existing != null)
                {
                    existing.TransactionFieldId = transactionFieldId;
                    existing.IsModified = true;
                }
                else
                {
                    list.Add(new AppTransactionUnitSearchFieldMappingExDto
                    {
                        TransactionUnitLinkedSearchId = dto.Id != null ? Convert.ToInt32(dto.Id) : 0,
                        SearchFieldId = searchFieldId,
                        TransactionFieldId = transactionFieldId
                    });
                }

                var sfLabel = searchFields.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == searchFieldId);
                var tfLabel = unitFields.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == transactionFieldId);
                applied.Add(new
                {
                    searchFieldId,
                    searchFilter = sfLabel?.DisplayText ?? sfLabel?.SysTableFiledPath,
                    transactionFieldId,
                    formField = tfLabel?.DisplayName ?? tfLabel?.DataBaseFieldName
                });
            }

            return true;
        }

        static List<object> SummarizeFilterMappings(
            ObservableSet<AppTransactionUnitSearchFieldMappingExDto> list)
        {
            if (list == null || list.Count == 0) return new List<object>();
            return list.Select(m => (object)new
            {
                searchFieldId = m.SearchFieldId,
                transactionFieldId = m.TransactionFieldId,
                id = m.Id
            }).ToList();
        }

        static AppSearchFieldExDto ResolveSearchFilterField(
            ObservableSet<AppSearchFieldExDto> fields, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || fields == null) return null;
            var needle = name.Trim();
            return fields.FirstOrDefault(f =>
                       string.Equals(f.DisplayText, needle, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.SysTableFiledPath, needle, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.SysTableFiledFullPath, needle, StringComparison.OrdinalIgnoreCase))
                   ?? fields.FirstOrDefault(f =>
                       (!string.IsNullOrEmpty(f.DisplayText) && f.DisplayText.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                       || (!string.IsNullOrEmpty(f.SysTableFiledPath) && f.SysTableFiledPath.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        static List<AppTransactionFieldExDto> ResolveUnitFields(int unitId)
        {
            try
            {
                var unit = AppTransactionBL.RetrieveOneAppTransactionUnitExDto(unitId);
                return (unit?.AppTransactionFieldList ?? new ObservableSet<AppTransactionFieldExDto>())
                    .Where(f => f != null)
                    .ToList();
            }
            catch
            {
                return new List<AppTransactionFieldExDto>();
            }
        }

        static AppTransactionFieldExDto ResolveUnitFormField(List<AppTransactionFieldExDto> fields, string name)
        {
            if (string.IsNullOrWhiteSpace(name) || fields == null) return null;
            var needle = name.Trim();
            // Strip optional [TF_id_Name] wrappers agents sometimes paste.
            if (needle.StartsWith("[TF_", StringComparison.OrdinalIgnoreCase) && needle.EndsWith("]"))
            {
                var inner = needle.Substring(4, needle.Length - 5);
                var parts = inner.Split(new[] { '_' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[0], out var fid))
                    return fields.FirstOrDefault(f => f.Id != null && Convert.ToInt32(f.Id) == fid);
                if (parts.Length == 2) needle = parts[1];
            }

            return fields.FirstOrDefault(f =>
                       string.Equals(f.DataBaseFieldName, needle, StringComparison.OrdinalIgnoreCase)
                       || string.Equals(f.DisplayName, needle, StringComparison.OrdinalIgnoreCase))
                   ?? fields.FirstOrDefault(f =>
                       (!string.IsNullOrEmpty(f.DisplayName) && f.DisplayName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                       || (!string.IsNullOrEmpty(f.DataBaseFieldName) && f.DataBaseFieldName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        /// <summary>UI create uses Sort=1; agent assigns max(existing)+1 so menus stay ordered.</summary>
        static int NextLinkedSearchSort(int unitId)
        {
            try
            {
                var list = AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchList(unitId);
                if (list == null || list.Count == 0) return 1;
                var max = list.Where(x => x.Sort.HasValue).Select(x => x.Sort.Value).DefaultIfEmpty(0).Max();
                return max + 1;
            }
            catch
            {
                return 1;
            }
        }

        static AppTransactionUnitLinkedSearchExDto TryFindLinkedSearchOnUnit(int unitId, int? searchId, string name)
        {
            try
            {
                var list = AppTransactionUnitLinkedSearchBL.RetrieveOneAppTransactionUnitLinkedSearchList(unitId);
                if (list == null) return null;
                IEnumerable<AppTransactionUnitLinkedSearchExDto> q = list;
                if (searchId.HasValue)
                    q = q.Where(x => x.SearchId == searchId);
                if (!string.IsNullOrWhiteSpace(name))
                    q = q.Where(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                return q
                    .OrderByDescending(x => Convert.ToInt32(x.Id ?? 0))
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        static string ShortExceptionMessage(Exception ex)
        {
            if (ex == null) return "Unknown error.";
            var text = ex.ToString();
            // Prefer the actionable SQL / ORM one-liner when present.
            var fk = System.Text.RegularExpressions.Regex.Match(
                text,
                @"FOREIGN KEY constraint [""']?(?<n>[^""'\s]+)[""']?.*?table [""']?dbo\.(?<t>\w+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            if (fk.Success)
                return $"FK conflict ({fk.Groups["n"].Value} → {fk.Groups["t"].Value}). " +
                       "For Linked Search create, SearchViewId must be a real AppSearchView id (auto-filled from AppSearch.SearchViewId); " +
                       "searchId must be AppSearch.SearchId, not dataSetId.";
            if (text.IndexOf("out of sync", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Entity out of sync during save/cache refresh. Retry upsert_linked_search; if it persists, call get_linked_searches — the row may already exist.";
            var msg = ex.Message ?? "";
            return msg.Length > 400 ? msg.Substring(0, 400) + "…" : msg;
        }

        static string FormatValidationErrors(ValidationResult vr)
        {
            if (vr?.Items == null || vr.Items.Count == 0)
                return "Save LinkedSearch failed (no validation items).";
            var parts = vr.Items
                .Where(i => i != null && i.ItemType == ValidationItemType.Error)
                .Select(i =>
                {
                    var raw = !string.IsNullOrWhiteSpace(i.Message) ? i.Message : null;
                    var msg = raw != null ? ShortExceptionMessage(new Exception(raw)) : null;
                    // If ShortExceptionMessage only echoed a long blob, keep a tighter slice.
                    if (msg != null && msg.Length > 280 && raw.IndexOf("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) < 0
                        && raw.IndexOf("out of sync", StringComparison.OrdinalIgnoreCase) < 0)
                        msg = msg.Substring(0, 280) + "…";
                    var key = !string.IsNullOrWhiteSpace(i.Key) ? i.Key : null;
                    var prop = !string.IsNullOrWhiteSpace(i.PropertyName) ? i.PropertyName : null;
                    if (msg != null && key != null) return $"{prop ?? "field"}: {msg} ({key})";
                    if (msg != null) return $"{prop ?? "field"}: {msg}";
                    if (key != null) return $"{prop ?? "field"}: {key}";
                    return prop != null ? $"{prop} is invalid" : "validation error";
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();
            return parts.Count > 0 ? string.Join("; ", parts) : "Save LinkedSearch failed (empty error messages).";
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

        static void ApplyCommandOptions(
            AppProjectWorkFlowActionExDto cmd,
            JObject jo,
            IList<AppProjectWorkFlowActionExDto> allCommands = null)
        {
            if (jo["notificationMessage"] != null) cmd.NotificationMessage = jo.Value<string>("notificationMessage");
            // UI: Notification Subject (Send Message 60–62). Column on command row — NOT ActionAttribute.
            if (TryGetPropertyIgnoreCase(jo, out var subjProp, "notificationSubject", "subject")
                && subjProp?.Value != null)
            {
                cmd.NotificationSubject = subjProp.Value.Type == JTokenType.Null
                    ? null
                    : subjProp.Value.ToString();
            }
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
            if (jo["childCommandsSwitchConditionFieldId"] != null)
                attr.ChildCommandsSwitchConditionFieldId = jo.Value<int?>("childCommandsSwitchConditionFieldId");

            // Execute Data Transfer (43): UI binds command.DataTransferSettingId (column), NOT ParamId1 / ActionAttribute.
            // Agents often pass optionsJson.dataTransferSettingId — must map explicitly (same bug class as SMS phone field).
            var dtSettingId = FirstNullableInt(jo,
                "dataTransferSettingId", "DataTransferSettingId",
                "dataTransferId", "DataTransferId");
            if (dtSettingId.HasValue)
                cmd.DataTransferSettingId = dtSettingId.Value > 0 ? dtSettingId : null;

            // UI: Need To Open Form (Execute Data Transfer / related) → ActionAttribute.IsNeedToOpenNewForm
            var openNewForm = FirstNullableBool(jo,
                "isNeedToOpenNewForm", "IsNeedToOpenNewForm",
                "needToOpenForm", "NeedToOpenForm",
                "openTargetForm", "OpenTargetForm");
            if (openNewForm.HasValue)
                attr.IsNeedToOpenNewForm = openNewForm.Value;

            // Print From Message Template (74) / Send Message types: top-level MessageTemplateId column
            // (NOT ActionAttribute). UI: PrintFromMessageTemplateSection / NotificationExpressionSection.
            var msgTplId = jo.Value<int?>("messageTemplateId")
                ?? jo.Value<int?>("MessageTemplateId")
                ?? (jo["actionAttribute"] as JObject)?.Value<int?>("messageTemplateId")
                ?? (jo["actionAttribute"] as JObject)?.Value<int?>("MessageTemplateId");
            if (msgTplId.HasValue)
                cmd.MessageTemplateId = msgTplId.Value > 0 ? msgTplId : null;

            // Preserve unknown keys for advanced types via raw merge into ActionAttribute JSON if present
            if (jo["actionAttribute"] is JObject attrObj)
            {
                var existing = attr == null ? new JObject() : JObject.FromObject(attr);
                existing.Merge(attrObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                cmd.ActionAttribute = existing.ToObject<AppActionAttributeDto>()
                    ?? new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
                attr = cmd.ActionAttribute;
            }

            // Command Formula (57): UI expression → ActionAttribute.ExecutionFormulaUI (NOT notificationMessage).
            // Apply AFTER actionAttribute merge so top-level aliases win.
            var formulaUi = FirstNonEmptyString(jo,
                "executionFormulaUI", "ExecutionFormulaUI",
                "executionFormula", "ExecutionFormula",
                "formulaExpression", "FormulaExpression");
            if (formulaUi != null)
                attr.ExecutionFormulaUI = formulaUi;

            // Send SMS / Message destination fields (UI: Notification Message → Phone Number / Email / Partner Field).
            // Agents often pass these at optionsJson top-level (not under actionAttribute); must map explicitly.
            // DB/DTO typo is "FiledId" — also accept corrected "FieldId" spellings.
            var smsPhoneFieldId = FirstNullableInt(jo,
                "smsMessageToPhoneNumberFiledId", "SmsMessageToPhoneNumberFiledId",
                "smsMessageToPhoneNumberFieldId", "SmsMessageToPhoneNumberFieldId",
                "phoneNumberFieldId", "PhoneNumberFieldId");
            if (smsPhoneFieldId.HasValue)
                attr.SmsMessageToPhoneNumberFiledId = smsPhoneFieldId.Value > 0 ? smsPhoneFieldId : null;

            var emailFieldId = FirstNullableInt(jo,
                "notificationDestinationEmailAddressTransactionFiledId",
                "NotificationDestinationEmailAddressTransactionFiledId",
                "notificationDestinationEmailAddressTransactionFieldId",
                "emailAddressFieldId", "EmailAddressFieldId");
            if (emailFieldId.HasValue)
                attr.NotificationDestinationEmailAddressTransactionFiledId = emailFieldId.Value > 0 ? emailFieldId : null;

            var partnerOrUserFieldId = FirstNullableInt(jo,
                "notificationDestinationPartnerIdTransactionFiledId",
                "NotificationDestinationPartnerIdTransactionFiledId",
                "notificationDestinationPartnerIdTransactionFieldId",
                "partnerIdFieldId", "PartnerIdFieldId",
                "userIdFieldId", "UserIdFieldId",
                "messageToUserIdFieldId", "MessageToUserIdFieldId");
            if (partnerOrUserFieldId.HasValue)
                attr.NotificationDestinationPartnerIdTransactionFiledId = partnerOrUserFieldId.Value > 0 ? partnerOrUserFieldId : null;

            var toEmail = FirstNonEmptyString(jo, "toEmailAddress", "ToEmailAddress");
            if (toEmail != null)
                attr.ToEmailAddress = toEmail;

            // UI: Use Rich Text Message Template / Attach Form Files (Send Message 60–62).
            var useRich = FirstNullableBool(jo,
                "isUseRichTextMessageTemplate", "IsUseRichTextMessageTemplate",
                "useRichTextMessageTemplate", "useRichText");
            if (useRich.HasValue)
                attr.IsUseRichTextMessageTemplate = useRich.Value;

            var attachFiles = FirstNullableBool(jo,
                "isAttachAllFormFilesToMessage", "IsAttachAllFormFilesToMessage",
                "attachAllFormFilesToMessage", "attachFormFiles");
            if (attachFiles.HasValue)
                attr.IsAttachAllFormFilesToMessage = attachFiles.Value;

            // Explicit Composition / Message-link children (same shape as UI Child Operations grid).
            // Prefer top-level childActionList; also accept actionAttribute.childActionList via merge above.
            // Message Link Operations UI/runtime: Partner (62) only — not Email (61).
            var childTok = jo["childActionList"] ?? jo["ChildActionList"];
            if (childTok is JArray childArr)
                attr.ChildActionList = ParseChildActionList(childArr, allCommands);
            else if (attr.ChildActionList != null && attr.ChildActionList.Count > 0)
                EnrichChildActionDisplays(attr.ChildActionList, allCommands);
        }

        static string FirstNonEmptyString(JObject jo, params string[] keys)
        {
            if (jo == null || keys == null) return null;
            foreach (var key in keys)
            {
                if (!TryGetTokenIgnoreCase(jo, key, out var tok)) continue;
                var s = tok.Type == JTokenType.String ? tok.Value<string>() : tok.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            return null;
        }

        static int? FirstNullableInt(JObject jo, params string[] keys)
        {
            if (jo == null || keys == null) return null;
            foreach (var key in keys)
            {
                if (!TryGetTokenIgnoreCase(jo, key, out var tok)) continue;
                if (tok.Type == JTokenType.Integer) return tok.Value<int>();
                if (int.TryParse(tok.ToString(), out var v)) return v;
            }
            return null;
        }

        static bool? FirstNullableBool(JObject jo, params string[] keys)
        {
            if (jo == null || keys == null) return null;
            foreach (var key in keys)
            {
                if (!TryGetTokenIgnoreCase(jo, key, out var tok)) continue;
                if (tok.Type == JTokenType.Boolean) return tok.Value<bool>();
                if (bool.TryParse(tok.ToString(), out var v)) return v;
                if (tok.Type == JTokenType.Integer) return tok.Value<int>() != 0;
            }
            return null;
        }

        static bool TryGetTokenIgnoreCase(JObject jo, string key, out JToken tok)
        {
            tok = null;
            if (jo == null || string.IsNullOrEmpty(key)) return false;
            if (jo.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out tok)
                && tok != null && tok.Type != JTokenType.Null)
                return true;
            tok = null;
            return false;
        }

        static bool TryGetPropertyIgnoreCase(JObject jo, out JProperty prop, params string[] keys)
        {
            prop = null;
            if (jo == null || keys == null) return false;
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                prop = jo.Property(key, StringComparison.OrdinalIgnoreCase);
                if (prop != null) return true;
            }
            prop = null;
            return false;
        }

        /// <summary>
        /// If agent put the formula in notificationMessage (SQL/message column), move it to ExecutionFormulaUI.
        /// </summary>
        static List<string> NormalizeCommandFormulaStorage(AppProjectWorkFlowActionExDto cmd)
        {
            var notes = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                return notes;

            cmd.ActionAttribute ??= new AppActionAttributeDto { ChildActionList = new List<ChildTransactionCommandDto>() };
            var attr = cmd.ActionAttribute;
            var ui = attr.ExecutionFormulaUI;
            var msg = cmd.NotificationMessage;

            if (string.IsNullOrWhiteSpace(ui) && LooksLikeCommandFormulaExpression(msg))
            {
                attr.ExecutionFormulaUI = msg.Trim();
                cmd.NotificationMessage = null;
                notes.Add(
                    "Moved formula from notificationMessage → ActionAttribute.ExecutionFormulaUI. " +
                    "For actionType 57, always pass optionsJson.executionFormulaUI (or actionAttribute.executionFormulaUI); " +
                    "notificationMessage is for SQL (42) / message bodies only.");
            }

            return notes;
        }

        static void PrepareCommandFormulaForPersist(AppTransactionExDto tx, AppProjectWorkFlowActionExDto cmd)
        {
            if (tx == null || cmd?.ActionAttribute == null) return;
            var fields = tx.DictAllTransactionField?.Values.ToList() ?? new List<AppTransactionFieldExDto>();
            if (fields.Count == 0) return;
            AppTransactionFormulaSetupBL.InitialTransactionFieldFormularDisplayName(tx, fields);
            AppTransactionCommandBL.InFormatExecutionFormula(fields, cmd);
        }

        static bool LooksLikeCommandFormulaExpression(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim();
            var upper = t.ToUpperInvariant();
            if (upper.StartsWith("SELECT", StringComparison.Ordinal)
                || upper.StartsWith("UPDATE", StringComparison.Ordinal)
                || upper.StartsWith("DELETE", StringComparison.Ordinal)
                || upper.StartsWith("INSERT", StringComparison.Ordinal)
                || upper.StartsWith("EXEC", StringComparison.Ordinal))
                return false;
            return t.IndexOf('=') >= 0;
        }

        static List<ChildTransactionCommandDto> ParseChildActionList(
            JArray childArr,
            IList<AppProjectWorkFlowActionExDto> allCommands)
        {
            var list = new List<ChildTransactionCommandDto>();
            var sort = 0;
            foreach (var item in childArr)
            {
                if (item is not JObject co) continue;
                sort++;
                var child = new ChildTransactionCommandDto
                {
                    Sort = co.Value<int?>("sort") ?? co.Value<int?>("Sort") ?? sort,
                    CommandId = co.Value<int?>("commandId") ?? co.Value<int?>("CommandId"),
                    CommandDisplay = co.Value<string>("commandDisplay") ?? co.Value<string>("CommandDisplay"),
                    ExternalTransactionId = co.Value<int?>("externalTransactionId") ?? co.Value<int?>("ExternalTransactionId"),
                    IsSkip = co.Value<bool?>("isSkip") ?? co.Value<bool?>("IsSkip") ?? false,
                    IsGoToNextCommandWithError = co.Value<bool?>("isGoToNextCommandWithError")
                        ?? co.Value<bool?>("IsGoToNextCommandWithError"),
                    PredictValue = co.Value<int?>("predictValue") ?? co.Value<int?>("PredictValue"),
                    ChangeTriggerRootLevelFieldId = co.Value<int?>("changeTriggerRootLevelFieldId")
                        ?? co.Value<int?>("ChangeTriggerRootLevelFieldId"),
                    ChangeTriggerChildGridUnitId = co.Value<int?>("changeTriggerChildGridUnitId")
                        ?? co.Value<int?>("ChangeTriggerChildGridUnitId"),
                    IsBatchCommand = co.Value<bool?>("isBatchCommand") ?? co.Value<bool?>("IsBatchCommand") ?? false
                };
                list.Add(child);
            }
            EnrichChildActionDisplays(list, allCommands);
            return list;
        }

        static void EnrichChildActionDisplays(
            IList<ChildTransactionCommandDto> children,
            IList<AppProjectWorkFlowActionExDto> allCommands)
        {
            if (children == null || allCommands == null) return;
            foreach (var child in children)
            {
                if (!child.CommandId.HasValue) continue;
                if (!string.IsNullOrWhiteSpace(child.CommandDisplay) && !child.ExternalTransactionId.HasValue)
                    continue;
                if (child.ExternalTransactionId.HasValue) continue; // external display must be supplied by caller
                var match = allCommands.FirstOrDefault(c =>
                    c.Id != null && Convert.ToInt32(c.Id) == child.CommandId.Value);
                if (match == null) continue;
                if (string.IsNullOrWhiteSpace(child.CommandDisplay))
                    child.CommandDisplay = match.Name ?? match.DisplayName ?? $"Command {child.CommandId}";
                if (match.ActionAttribute != null)
                    child.IsBatchCommand = match.ActionAttribute.IsBatchCommand;
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
            notificationSubject = c.NotificationSubject,
            notificationMessage = c.NotificationMessage,
            messageTemplateId = c.MessageTemplateId,
            dataTransferSettingId = c.DataTransferSettingId,
            commandTransactionId = c.CommandTransactionId,
            actionAttribute = c.ActionAttribute
        };

        /// <summary>
        /// Print From Message Template (74) binds via top-level MessageTemplateId (AppMessage template),
        /// not NotificationMessage body and not ActionAttribute.
        /// </summary>
        static List<string> BuildPrintFromMessageTemplateWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.PrintFromMessageTemplate
                && cmd?.ActionType != 74)
                return warnings;

            if (cmd.MessageTemplateId == null || Convert.ToInt32(cmd.MessageTemplateId) <= 0)
            {
                warnings.Add(
                    "Print From Message Template (74) requires optionsJson.messageTemplateId = an existing " +
                    "transaction message template Id (AppMessage). UI Message Template field stays (None) until set. " +
                    "Do NOT put template body in notificationMessage. " +
                    "Flow: upsert_message_template (or get_message_templates) → upsert_command(optionsJson:{messageTemplateId}).");
            }

            if (!string.IsNullOrWhiteSpace(cmd.NotificationMessage)
                && (cmd.MessageTemplateId == null || Convert.ToInt32(cmd.MessageTemplateId) <= 0)
                && cmd.NotificationMessage.IndexOf("TEMPLATE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                warnings.Add(
                    "notificationMessage looks like template text but MessageTemplateId is empty — " +
                    "the Operation editor will still show Message Template = (None).");
            }

            return warnings;
        }

        /// <summary>
        /// Send Message / SMS (60–63): destination fields + subject/body/rich-text/content checks.
        /// </summary>
        static List<string> BuildSendMessageDestinationWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType == null) return warnings;
            var t = cmd.ActionType.Value;
            var attr = cmd.ActionAttribute;
            var isSms = t == (int)EmAppTransactionCommandType.SendSmsToTransFieldPhoneNumber;
            var isEmail = t == (int)EmAppTransactionCommandType.SendMessageToTransFieldEmailAddress;
            var isPartner = t == (int)EmAppTransactionCommandType.SendMessageToTransFieldPartnerId;
            var isUserId = t == (int)EmAppTransactionCommandType.SendMessageToTransFieldUserId;
            if (!isSms && !isEmail && !isPartner && !isUserId) return warnings;

            if (isSms)
            {
                if (attr?.SmsMessageToPhoneNumberFiledId == null || attr.SmsMessageToPhoneNumberFiledId <= 0)
                {
                    warnings.Add(
                        "Send SMS (63) requires optionsJson.smsMessageToPhoneNumberFiledId = form fieldId " +
                        "(UI: Phone Number Field). Also accept smsMessageToPhoneNumberFieldId / phoneNumberFieldId. " +
                        "Stored on ActionAttribute.SmsMessageToPhoneNumberFiledId (DTO spelling FiledId).");
                }
                if (string.IsNullOrWhiteSpace(cmd.NotificationMessage))
                {
                    warnings.Add(
                        "Send SMS (63) requires notificationMessage (SMS body). Runtime will not send an empty body.");
                }
            }
            else if (isEmail)
            {
                if (attr?.NotificationDestinationEmailAddressTransactionFiledId == null
                    || attr.NotificationDestinationEmailAddressTransactionFiledId <= 0)
                {
                    warnings.Add(
                        "Send Email (61) requires optionsJson.emailAddressFieldId " +
                        "(or notificationDestinationEmailAddressTransactionFiledId) = form fieldId. UI: Email Address Field.");
                }
                if (attr?.ChildActionList != null && attr.ChildActionList.Count > 0)
                {
                    warnings.Add(
                        "Message Link Operations (childActionList) apply only to Partner Send Message (62). " +
                        "UI hides Message Link for Email (61); runtime does not append links on 61. Clear childActionList.");
                }
            }
            else if (isPartner)
            {
                var hasPartnerField = attr?.NotificationDestinationPartnerIdTransactionFiledId != null
                    && attr.NotificationDestinationPartnerIdTransactionFiledId > 0;
                var hasToEmail = !string.IsNullOrWhiteSpace(attr?.ToEmailAddress);
                if (!hasPartnerField && !hasToEmail)
                {
                    warnings.Add(
                        "Send Message Partner (62) needs optionsJson.partnerIdFieldId " +
                        "(notificationDestinationPartnerIdTransactionFiledId) OR toEmailAddress. " +
                        "UI: Message To Partner Field / Additional To Email Address.");
                }
            }
            else if (isUserId)
            {
                if (attr?.NotificationDestinationPartnerIdTransactionFiledId == null
                    || attr.NotificationDestinationPartnerIdTransactionFiledId <= 0)
                {
                    warnings.Add(
                        "Send Message UserId (60) requires optionsJson.userIdFieldId " +
                        "(or notificationDestinationPartnerIdTransactionFiledId) = form fieldId. UI: Message To UserId Field.");
                }
            }

            // Shared content for email-style types 60–62 (not SMS).
            if (isEmail || isPartner || isUserId)
            {
                var useRich = attr != null && attr.IsUseRichTextMessageTemplate;
                var hasTpl = cmd.MessageTemplateId != null && Convert.ToInt32(cmd.MessageTemplateId) > 0;
                var hasSubject = !string.IsNullOrWhiteSpace(cmd.NotificationSubject);
                var hasBody = !string.IsNullOrWhiteSpace(cmd.NotificationMessage);

                if (hasTpl && !useRich)
                {
                    warnings.Add(
                        "messageTemplateId is set but isUseRichTextMessageTemplate is false — runtime clears MessageTemplateId. " +
                        "Set optionsJson.isUseRichTextMessageTemplate:true (UI: Use Rich Text Message Template).");
                }
                if (useRich && !hasTpl)
                {
                    warnings.Add(
                        "isUseRichTextMessageTemplate is true but messageTemplateId is empty. " +
                        "Flow: upsert_message_template → optionsJson.messageTemplateId + isUseRichTextMessageTemplate:true.");
                }
                if (!useRich && !hasTpl && (!hasSubject || !hasBody))
                {
                    warnings.Add(
                        "Plain Send Message (60/61/62) needs optionsJson.notificationSubject AND notificationMessage " +
                        "(or rich-text path: isUseRichTextMessageTemplate + messageTemplateId).");
                }
                if (hasBody && cmd.NotificationMessage.IndexOf("\\n", StringComparison.Ordinal) >= 0)
                {
                    warnings.Add(
                        "notificationMessage contains literal \\n text. Emails are HTML — use real line breaks in the JSON string " +
                        "(or <br />). upsert_command normalizes literal \\n → newline on save; prefer writing real newlines.");
                }
            }

            return warnings;
        }

        /// <summary>
        /// Execute Data Transfer (43): UI Data Transfer Setting = command.DataTransferSettingId column.
        /// </summary>
        static List<string> BuildExecuteDataTransferWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.ExecuteFormDataTransfer
                && cmd?.ActionType != 43)
                return warnings;

            if (cmd.DataTransferSettingId == null || Convert.ToInt32(cmd.DataTransferSettingId) <= 0)
            {
                warnings.Add(
                    "Execute Data Transfer (43) requires optionsJson.dataTransferSettingId = an existing " +
                    "Data Transfer setting Id (UI: Data Transfer Setting). " +
                    "Flow: upsert_data_transfer → upsert_command(optionsJson:{dataTransferSettingId, isNeedToOpenNewForm?}). " +
                    "Do NOT use ParamId1 for this — UI binds the DataTransferSettingId column.");
            }
            return warnings;
        }

        /// <summary>
        /// Send Message/SMS bodies: agents often store the characters '\'+'n' instead of a real newline.
        /// Normalize on upsert so the Operation editor and runtime see actual line breaks.
        /// </summary>
        static void NormalizeSendMessageNotificationBodyNewlines(AppProjectWorkFlowActionExDto cmd)
        {
            if (cmd?.ActionType == null || string.IsNullOrEmpty(cmd.NotificationMessage)) return;
            var t = cmd.ActionType.Value;
            if (t != (int)EmAppTransactionCommandType.SendMessageToTransFieldUserId
                && t != (int)EmAppTransactionCommandType.SendMessageToTransFieldEmailAddress
                && t != (int)EmAppTransactionCommandType.SendMessageToTransFieldPartnerId
                && t != (int)EmAppTransactionCommandType.SendSmsToTransFieldPhoneNumber)
                return;
            cmd.NotificationMessage = AppMessageBL.NormalizeAgentLiteralNewlines(cmd.NotificationMessage);
        }

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

        /// <summary>
        /// Composition (200) must reference child operations via ActionAttribute.ChildActionList.
        /// </summary>
        static List<string> BuildCompositionChildWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != 200) return warnings;

            var children = cmd.ActionAttribute?.ChildActionList;
            if (children == null || children.Count == 0)
            {
                warnings.Add(
                    "Composition Operation (200) has an empty ChildActionList — the Child Operations grid will be empty. " +
                    "Create Private child operations first (upsert_command linkToUI:false), then update this composition with " +
                    "optionsJson.childActionList:[{commandId,sort},…] (same as UI Add Existing). " +
                    "Do not put SQL in the composition itself; put SQL on a child with actionType 42.");
            }
            else
            {
                var missingId = children.Where(c => !c.CommandId.HasValue && !c.ExternalTransactionId.HasValue).ToList();
                if (missingId.Count > 0)
                {
                    warnings.Add(
                        $"{missingId.Count} child row(s) lack commandId. Each ChildActionList item needs commandId of an existing operation.");
                }
            }

            if (!string.IsNullOrWhiteSpace(cmd.NotificationMessage)
                && (cmd.NotificationMessage.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0
                    || cmd.NotificationMessage.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase) >= 0
                    || cmd.NotificationMessage.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                warnings.Add(
                    "Composition notificationMessage looks like SQL. actionType 200 does not execute that SQL as the parent body — " +
                    "move SQL to a Private child (42) and reference it from childActionList.");
            }

            return warnings;
        }

        /// <summary>
        /// Soft checks for Execute SQL Statement (42) — mirrors Query Builder token rules.
        /// </summary>
        static List<string> BuildCommandSqlTokenWarnings(int? actionType, string sql)
        {
            var warnings = new List<string>();
            if (actionType != 42 || string.IsNullOrWhiteSpace(sql))
                return warnings;

            var trimmed = sql.TrimStart();
            var upper = trimmed.ToUpperInvariant();
            var isUpdateOrDelete =
                upper.StartsWith("UPDATE", StringComparison.Ordinal) ||
                upper.StartsWith("DELETE", StringComparison.Ordinal);

            if (sql.IndexOf("[rootid]", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                warnings.Add(
                    "[rootid] is not a valid form token. Use live sqlToken from find_fields / get_unit_fields / " +
                    "get_current_transaction_config.rootPrimaryKeys (e.g. [TF_3499_OpportunityId]).");
            }

            if (isUpdateOrDelete && upper.IndexOf("WHERE", StringComparison.Ordinal) < 0)
            {
                warnings.Add(
                    "UPDATE/DELETE without WHERE is unsafe. Scope to the current form row with " +
                    "rootPrimaryKeys[].sqlToken (root master PK) or the correct grid field sqlToken.");
            }

            if (isUpdateOrDelete && sql.IndexOf("[TF_", StringComparison.OrdinalIgnoreCase) < 0)
            {
                warnings.Add(
                    "UPDATE/DELETE should use [TF_{fieldId}_{dbColumn}] tokens from Query Builder field buttons " +
                    "(exposed as sqlToken on field tools). Do not invent placeholders.");
            }

            return warnings;
        }

        /// <summary>
        /// Navigate To Search (53): LinkedSearchId must be an existing unit binding id (not AppSearch.SearchId).
        /// Hard-blocks confirmed save.
        /// </summary>
        static List<string> BuildNavigateSearchHardWarnings(AppTransactionExDto tx, AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.OpenLinkedSearch)
                return warnings;

            var linkedId = cmd.ActionAttribute?.LinkedSearchId;
            var units = EntityMapWiring.FlattenUnits(
                tx?.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
            var bindings = units
                .SelectMany(u => u.AppTransactionUnitLinkedSearchList
                    ?? Enumerable.Empty<AppTransactionUnitLinkedSearchExDto>())
                .Where(ls => ls?.Id != null)
                .ToList();

            if (!linkedId.HasValue || linkedId.Value <= 0)
            {
                warnings.Add(
                    bindings.Count == 0
                        ? "Navigate To Search (53) needs a unit Linked Search first: " +
                          "list_available_searches → upsert_linked_search(rootUnitId, searchId) → " +
                          "then optionsJson.linkedSearchId = returned linkedSearchId."
                        : "Navigate To Search (53) requires optionsJson.linkedSearchId from get_linked_searches " +
                          $"(available: {string.Join(", ", bindings.Select(b => $"{b.Id}:{b.Name}"))}).");
                return warnings;
            }

            var match = bindings.FirstOrDefault(b => Convert.ToInt32(b.Id) == linkedId.Value);
            if (match == null)
            {
                warnings.Add(
                    $"linkedSearchId {linkedId} is not a unit Linked Search binding on this transaction " +
                    "(UI would show a fake 'Search {id}'). " +
                    (bindings.Count == 0
                        ? "get_linked_searches is empty — create bindings via list_available_searches + upsert_linked_search first."
                        : $"Valid linkedSearchId values: {string.Join(", ", bindings.Select(b => b.Id))}. " +
                          "Do not use AppSearch.SearchId or invent 1."));
            }

            return warnings;
        }

        /// <summary>
        /// Soft tips for Navigate To Search — do not block save.
        /// </summary>
        static List<string> BuildNavigateSearchSoftWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.OpenLinkedSearch)
                return warnings;

            // Agents often put form field ids on ParamId1 — that is NOT UI Search Mapping.
            if (cmd.ActionAttribute?.ParamId1 != null)
            {
                warnings.Add(
                    "ParamId1 is not Linked Search filter mapping. " +
                    "To pass form field values into search criteria, call upsert_linked_search with " +
                    "filterMappingsJson=[{\"searchFilter\":\"Customer\",\"formField\":\"CustomerId\"}] " +
                    "on the Linked Search binding (UI: Search Mapping). Clear ParamId1 unless you intentionally need it.");
            }

            return warnings;
        }

        public static string GetLinkedSearches(int transactionId)
        {
            try
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                if (tx == null) return Fail($"Transaction {transactionId} not found.");
                var units = EntityMapWiring.FlattenUnits(
                    tx.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>());
                var list = new List<object>();
                foreach (var u in units)
                {
                    foreach (var ls in u.AppTransactionUnitLinkedSearchList
                             ?? Enumerable.Empty<AppTransactionUnitLinkedSearchExDto>())
                    {
                        object availableFilters = Array.Empty<object>();
                        if (ls.SearchId.HasValue)
                        {
                            try
                            {
                                var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(ls.SearchId.Value);
                                availableFilters = (search?.AppSearchFieldList ?? new ObservableSet<AppSearchFieldExDto>())
                                    .Where(f => f?.Id != null)
                                    .Select(f => (object)new
                                    {
                                        searchFieldId = f.Id,
                                        displayText = f.DisplayText,
                                        path = f.SysTableFiledPath
                                    })
                                    .ToList();
                            }
                            catch { /* best effort */ }
                        }

                        var mappings = (ls.AppTransactionUnitSearchFieldMappingList
                                        ?? new ObservableSet<AppTransactionUnitSearchFieldMappingExDto>())
                            .Select(m => (object)new
                            {
                                searchFieldId = m.SearchFieldId,
                                transactionFieldId = m.TransactionFieldId,
                                id = m.Id
                            })
                            .ToList();

                        list.Add(new
                        {
                            linkedSearchId = ls.Id,
                            id = ls.Id,
                            unitId = u.Id,
                            unitName = u.UnitDisplayName,
                            name = ls.Name,
                            searchId = ls.SearchId,
                            searchViewId = ls.SearchViewId,
                            sort = ls.Sort,
                            filterMappings = mappings,
                            availableSearchFilters = availableFilters
                        });
                    }
                }
                return Ok(new
                {
                    transactionId,
                    linkedSearches = list,
                    note = list.Count == 0
                        ? "No unit Linked Searches yet. Call list_available_searches → upsert_linked_search. Do NOT invent linkedSearchId."
                        : "Use linkedSearchId on upsert_command (53). " +
                          "Search Mapping (filter←form field) via upsert_linked_search filterMappingsJson — not ParamId1."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        /// <summary>
        /// Soft checks for Execute Command Formula (57) — storage + formulaToken / [Now] rules.
        /// </summary>
        static List<string> BuildCommandFormulaWarnings(AppProjectWorkFlowActionExDto cmd)
        {
            var warnings = new List<string>();
            if (cmd?.ActionType != (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                return warnings;

            var expr = cmd.ActionAttribute?.ExecutionFormulaUI;
            if (string.IsNullOrWhiteSpace(expr))
                expr = cmd.ActionAttribute?.ExecutionFormula;

            if (!string.IsNullOrWhiteSpace(cmd.NotificationMessage)
                && !string.IsNullOrWhiteSpace(expr)
                && string.Equals(cmd.NotificationMessage.Trim(), expr.Trim(), StringComparison.Ordinal))
            {
                warnings.Add(
                    "Formula is duplicated in notificationMessage. For actionType 57 keep it only in ActionAttribute.ExecutionFormulaUI.");
            }
            else if (!string.IsNullOrWhiteSpace(cmd.NotificationMessage)
                     && LooksLikeCommandFormulaExpression(cmd.NotificationMessage)
                     && string.IsNullOrWhiteSpace(expr))
            {
                warnings.Add(
                    "Formula appears in notificationMessage but ActionAttribute.ExecutionFormulaUI is empty. " +
                    "Use optionsJson.executionFormulaUI with formulaToken (not notificationMessage).");
            }

            if (string.IsNullOrWhiteSpace(expr))
            {
                warnings.Add(
                    "Command Formula (57) requires a non-empty ExecutionFormulaUI. " +
                    "Example: [Notes_3499] = [Now]. Resolve left side via find_fields → formulaToken.");
                return warnings;
            }

            if (expr.IndexOf("Format(", StringComparison.OrdinalIgnoreCase) >= 0
                || expr.IndexOf("Now()", StringComparison.OrdinalIgnoreCase) >= 0
                || expr.IndexOf("Today()", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                warnings.Add(
                    "VBA/Excel-style Format(Now(), …) / Now() is not valid Command Formula syntax. " +
                    "Use date tokens [Now] or [Today], e.g. [Notes_3499] = [Now].");
            }

            // Bare left-hand field name (Notes = …) instead of [Notes_id]
            var eq = expr.IndexOf('=');
            if (eq > 0)
            {
                var left = expr.Substring(0, eq).Trim();
                if (left.Length > 0 && left[0] != '[' && left.IndexOf('[') < 0)
                {
                    warnings.Add(
                        $"Left side '{left}' is not a formulaToken. Call find_fields / get_unit_fields and use formulaToken " +
                        "(root: [DbColumn_fieldId], e.g. [Notes_3499]). Do not use bare display/DB names or sqlToken [TF_…].");
                }
            }

            if (expr.IndexOf("[TF_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                warnings.Add(
                    "[TF_…] is sqlToken/messageToken for SQL/Message — not Command Formula. Use formulaToken [DbColumn_fieldId].");
            }

            return warnings;
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
