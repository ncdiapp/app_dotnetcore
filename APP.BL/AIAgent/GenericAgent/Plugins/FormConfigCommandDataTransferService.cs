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

                var formulaRescueNotes = NormalizeCommandFormulaStorage(cmd);

                var preview = new { action = isCreate ? "create" : "update", command = DetailCommand(cmd) };
                var warnings = BuildCommandSqlTokenWarnings(cmd.ActionType, cmd.NotificationMessage);
                warnings.AddRange(BuildCompositionChildWarnings(cmd));
                warnings.AddRange(formulaRescueNotes);
                warnings.AddRange(BuildCommandFormulaWarnings(cmd));
                warnings.AddRange(BuildNavigateSearchWarnings(tx, cmd));
                if (dryRun == true || confirmed != true)
                {
                    var next = (string)null;
                    if (warnings.Count > 0)
                    {
                        if (cmd.ActionType == 57)
                            next = "Fix Command Formula: put expression in optionsJson.executionFormulaUI (ActionAttribute), use formulaToken + [Now]/[Today] — not notificationMessage / Format(Now()).";
                        else if (cmd.ActionType == 53)
                            next = "Fix Navigate To Search: list_available_searches → upsert_linked_search on root unit → set optionsJson.linkedSearchId to returned linkedSearchId (not AppSearch.SearchId / invent 1).";
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
                var navBlock = BuildNavigateSearchWarnings(tx, cmd);
                if (navBlock.Count > 0 && cmd.ActionType == (int)EmAppTransactionCommandType.OpenLinkedSearch)
                    return Fail(string.Join(" ", navBlock));

                // Create path does not run SaveOneTransactionCommandActionList — format formula here.
                // Update path formats again inside SaveOneTransactionCommandActionList (safe / idempotent for UI tokens).
                if (cmd.ActionType == (int)EmAppTransactionCommandType.CommnadFormulaCalculation)
                    PrepareCommandFormulaForPersist(tx, cmd);

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
                    if (!searchId.HasValue)
                        return Fail("searchId is required for create. Call list_available_searches and pass a real AppSearch.SearchId (not dataSetId).");
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

                if (name != null) dto.Name = name;
                if (searchId.HasValue) dto.SearchId = searchId;
                if (action.HasValue) dto.Action = action;
                if (!string.IsNullOrWhiteSpace(changesJson))
                {
                    try
                    {
                        // Agent sometimes passes a JSON object string; also tolerate raw object via ToString.
                        JsonConvert.PopulateObject(changesJson, dto);
                    }
                    catch (Exception ex) { return Fail("changesJson invalid: " + ex.Message); }
                }

                // Keep unit ownership even if changesJson overwrote it.
                dto.TransactionUnitId = unitId;

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

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            op = dto.IsNew ? "create" : "update",
                            unitId,
                            name = dto.Name,
                            searchId = dto.SearchId,
                            searchName = searchExists.Name,
                            searchViewId = dto.SearchViewId,
                            action = dto.Action,
                            actionName = "ViewSearchResult/Open Search (3)",
                            usageType = dto.UsageType
                        },
                        note = "After confirmed save, pass returned linkedSearchId to upsert_command (actionType 53). Do not use searchId as linkedSearchId."
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
                    changed = new[] { new { type = "linkedSearch", id = savedId } },
                    NextStep = "For Navigate To Search (53): upsert_command with optionsJson.linkedSearchId=" + savedId + ", then request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ShortExceptionMessage(ex)); }
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

            // Explicit Composition / Message-link children (same shape as UI Child Operations grid).
            // Prefer top-level childActionList; also accept actionAttribute.childActionList via merge above.
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
                var tok = jo[key];
                if (tok == null || tok.Type == JTokenType.Null) continue;
                var s = tok.Type == JTokenType.String ? tok.Value<string>() : tok.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            return null;
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
        /// </summary>
        static List<string> BuildNavigateSearchWarnings(AppTransactionExDto tx, AppProjectWorkFlowActionExDto cmd)
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
