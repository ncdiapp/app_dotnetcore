using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using APP.Framework.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Transaction Data Load CRUD helpers for Form Config Agent.
    /// </summary>
    public static class FormConfigDataLoadService
    {
        public static string GetDataLoads(int transactionId, int? unitId)
        {
            try
            {
                var list = AppTransactionTemplateDataLoadSetupBL.RetrievOneAppTransactionDataLoadDto(transactionId)
                    ?? new ObservableSet<AppTransactionDataLoadDto>();
                var rows = list
                    .Where(d => !unitId.HasValue
                        || (d.TransactionUnitId.HasValue && Convert.ToInt32(d.TransactionUnitId) == unitId.Value))
                    .OrderBy(d => d.LoadOrder ?? int.MaxValue)
                    .ThenBy(d => Convert.ToInt32(d.Id ?? 0))
                    .Select(d => new
                    {
                        dataLoadId = d.Id,
                        loadName = d.LoadName,
                        description = d.Description,
                        transactionUnitId = d.TransactionUnitId,
                        dataSetId = d.DataSetId,
                        loadOrder = d.LoadOrder,
                        isAutoExecutedWhenOpenEditForm = d.IsAutoExcutedWhenOpenEditForm,
                        isAutoExecuteBeforeInitialCascading = d.IsAutoExecuteBeforeIntialCscading
                    })
                    .ToList();
                return Ok(new { transactionId, unitId, dataLoads = rows });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetDataLoadDetail(int transactionId, int dataLoadId)
        {
            try
            {
                var dto = AppTransactionTemplateDataLoadSetupBL.RetrieveOneAppTransactionDataLoadExDto(dataLoadId);
                if (dto == null || dto.Id == null)
                    return Fail($"DataLoad {dataLoadId} not found.");
                if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId)
                    return Fail($"DataLoad {dataLoadId} does not belong to transaction {transactionId}.");

                object columns = null;
                if (dto.DataSetId.HasValue)
                {
                    try
                    {
                        columns = AppDataSetBL.RetrieveQueryColumnList(dto.DataSetId.Value)
                            .Select(c => new { columnId = c.Id, display = c.Display })
                            .ToList();
                    }
                    catch { columns = null; }
                }

                var mappings = dto.AppTranscationDataLoadFieldMappingList
                    ?? new ObservableSet<AppTranscationDataLoadFieldMappingExDto>();
                return Ok(new
                {
                    transactionId,
                    dataLoad = SummarizeDataLoad(dto),
                    conditionMappings = mappings.Where(m => m.IsConditionMapping == true).Select(SummarizeMapping).ToList(),
                    fieldMappings = mappings.Where(m => m.IsConditionMapping != true).Select(SummarizeMapping).ToList(),
                    dataSetColumns = columns,
                    mappingHints = new
                    {
                        conditionMappingsJson =
                            "[{transactionFieldId, dbColumnName, whereClause}] — IsConditionMapping=true. whereClause uses operators (=, AND, OR, …).",
                        fieldMappingsJson =
                            "[{transactionFieldId, dbColumnName}] — value load from dataset column into form field.",
                        replaceSemantics =
                            "Upsert: pass conditionMappingsJson and/or fieldMappingsJson as full replace for that side. " +
                            "Pass [] to clear that side. Omit a json param to leave that side unchanged. " +
                            "Pass both when creating a complete rule."
                    }
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string ListAvailableDatasets(int transactionId, string nameContains, int? take)
        {
            try
            {
                _ = transactionId; // scoped to open form host; datasets are tenant-wide
                var limit = take.HasValue && take.Value > 0 ? Math.Min(take.Value, 200) : 50;
                var q = (nameContains ?? "").Trim();
                var list = AppDataSetBL.RetrieveAllAppDataSetEntityDto()
                    .Where(d => string.IsNullOrEmpty(q)
                        || (d.Name != null && d.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        || (d.Description != null && d.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                    .OrderBy(d => d.Name)
                    .Take(limit)
                    .Select(d => new
                    {
                        dataSetId = d.Id,
                        name = d.Name,
                        description = d.Description,
                        dataSourceFrom = d.DataSourceFrom
                    })
                    .ToList();
                return Ok(new { transactionId, count = list.Count, datasets = list });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string GetDatasetColumns(int dataSetId)
        {
            try
            {
                var cols = AppDataSetBL.RetrieveQueryColumnList(dataSetId)
                    .Select(c => new { columnId = c.Id, display = c.Display, dbColumnName = c.Id })
                    .ToList();
                return Ok(new { dataSetId, columns = cols });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string UpsertDataLoad(
            int transactionId,
            int? dataLoadId,
            int? unitId,
            string loadName,
            string description,
            int? dataSetId,
            int? loadOrder,
            bool? isAutoExecutedWhenOpenEditForm,
            bool? isAutoExecuteBeforeInitialCascading,
            string conditionMappingsJson,
            string fieldMappingsJson,
            bool? confirmed,
            bool? dryRun)
        {
            try
            {
                var isCreate = !(dataLoadId.HasValue && dataLoadId.Value > 0);
                AppTransactionDataLoadExDto dto;
                if (!isCreate)
                {
                    dto = AppTransactionTemplateDataLoadSetupBL.RetrieveOneAppTransactionDataLoadExDto(dataLoadId.Value);
                    if (dto == null || dto.Id == null) return Fail($"DataLoad {dataLoadId} not found.");
                    if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId)
                        return Fail($"DataLoad {dataLoadId} does not belong to transaction {transactionId}.");
                }
                else
                {
                    if (!unitId.HasValue || unitId.Value <= 0)
                        return Fail("unitId is required for create (TransactionUnitId of the Data Load).");
                    if (string.IsNullOrWhiteSpace(loadName))
                        return Fail("loadName is required for create.");
                    if (!dataSetId.HasValue || dataSetId.Value <= 0)
                        return Fail("dataSetId is required for create. Use list_available_datasets / get_dataset_columns.");

                    // UI "Load Order" / Sort Order: when omitted on create, append after current max
                    // (same pattern as upsert_command ActionFlowOrder = max+1).
                    int resolvedLoadOrder;
                    if (loadOrder.HasValue)
                    {
                        resolvedLoadOrder = loadOrder.Value;
                    }
                    else
                    {
                        var existing = AppTransactionTemplateDataLoadSetupBL.RetrievOneAppTransactionDataLoadDto(transactionId)
                            ?? new ObservableSet<AppTransactionDataLoadDto>();
                        var maxOrder = existing
                            .Select(d => d.LoadOrder ?? 0)
                            .DefaultIfEmpty(0)
                            .Max();
                        resolvedLoadOrder = maxOrder + 1;
                    }

                    dto = new AppTransactionDataLoadExDto
                    {
                        TransactionId = transactionId,
                        TransactionUnitId = unitId.Value,
                        LoadName = loadName,
                        Description = description ?? "",
                        DataSetId = dataSetId.Value,
                        LoadOrder = resolvedLoadOrder,
                        IsAutoExcutedWhenOpenEditForm = isAutoExecutedWhenOpenEditForm ?? false,
                        IsAutoExecuteBeforeIntialCscading = isAutoExecuteBeforeInitialCascading ?? false,
                        AppTranscationDataLoadFieldMappingList = new ObservableSet<AppTranscationDataLoadFieldMappingExDto>()
                    };
                }

                if (unitId.HasValue) dto.TransactionUnitId = unitId;
                if (loadName != null) dto.LoadName = loadName;
                if (description != null) dto.Description = description;
                if (dataSetId.HasValue) dto.DataSetId = dataSetId;
                if (loadOrder.HasValue) dto.LoadOrder = loadOrder;
                if (isAutoExecutedWhenOpenEditForm.HasValue) dto.IsAutoExcutedWhenOpenEditForm = isAutoExecutedWhenOpenEditForm;
                if (isAutoExecuteBeforeInitialCascading.HasValue) dto.IsAutoExecuteBeforeIntialCscading = isAutoExecuteBeforeInitialCascading;
                dto.TransactionId = transactionId;

                dto.AppTranscationDataLoadFieldMappingList ??= new ObservableSet<AppTranscationDataLoadFieldMappingExDto>();

                string mappingNote = null;
                if (conditionMappingsJson != null || fieldMappingsJson != null)
                {
                    var existing = dto.AppTranscationDataLoadFieldMappingList.ToList();
                    var keepConditions = conditionMappingsJson == null
                        ? existing.Where(m => m.IsConditionMapping == true).ToList()
                        : null;
                    var keepFields = fieldMappingsJson == null
                        ? existing.Where(m => m.IsConditionMapping != true).ToList()
                        : null;

                    JArray condArr = null;
                    JArray fieldArr = null;
                    try
                    {
                        if (conditionMappingsJson != null)
                            condArr = JArray.Parse(string.IsNullOrWhiteSpace(conditionMappingsJson) ? "[]" : conditionMappingsJson);
                        if (fieldMappingsJson != null)
                            fieldArr = JArray.Parse(string.IsNullOrWhiteSpace(fieldMappingsJson) ? "[]" : fieldMappingsJson);
                    }
                    catch (Exception ex) { return Fail("mappings JSON invalid: " + ex.Message); }

                    // Full rebuild of mapping list; Clear marks prior rows deleted.
                    dto.AppTranscationDataLoadFieldMappingList.Clear();

                    if (keepConditions != null)
                    {
                        foreach (var m in keepConditions)
                            dto.AppTranscationDataLoadFieldMappingList.Add(CloneMappingAsNew(m, isCondition: true));
                    }
                    else if (condArr != null)
                    {
                        foreach (var m in condArr)
                        {
                            if (m == null || m.Type == JTokenType.Null) continue;
                            dto.AppTranscationDataLoadFieldMappingList.Add(ParseMappingToken(m, isCondition: true));
                        }
                    }

                    if (keepFields != null)
                    {
                        foreach (var m in keepFields)
                            dto.AppTranscationDataLoadFieldMappingList.Add(CloneMappingAsNew(m, isCondition: false));
                    }
                    else if (fieldArr != null)
                    {
                        foreach (var m in fieldArr)
                        {
                            if (m == null || m.Type == JTokenType.Null) continue;
                            dto.AppTranscationDataLoadFieldMappingList.Add(ParseMappingToken(m, isCondition: false));
                        }
                    }

                    mappingNote =
                        $"conditionMappings={(condArr == null ? "unchanged→rebuilt" : condArr.Count.ToString())}; " +
                        $"fieldMappings={(fieldArr == null ? "unchanged→rebuilt" : fieldArr.Count.ToString())}";
                }

                dto.IsModified = true;

                if (dryRun == true || confirmed != true)
                {
                    var maps = dto.AppTranscationDataLoadFieldMappingList
                        ?? new ObservableSet<AppTranscationDataLoadFieldMappingExDto>();
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview only. ask_user then confirmed=true.",
                        proposed = new
                        {
                            action = isCreate || dto.IsNew ? "create" : "update",
                            loadName = dto.LoadName,
                            transactionUnitId = dto.TransactionUnitId,
                            dataSetId = dto.DataSetId,
                            loadOrder = dto.LoadOrder,
                            isAutoExecutedWhenOpenEditForm = dto.IsAutoExcutedWhenOpenEditForm,
                            isAutoExecuteBeforeInitialCascading = dto.IsAutoExecuteBeforeIntialCscading,
                            conditionMappings = maps.Where(m => m.IsConditionMapping == true).Select(SummarizeMapping).ToList(),
                            fieldMappings = maps.Where(m => m.IsConditionMapping != true).Select(SummarizeMapping).ToList(),
                            mappingNote
                        }
                    });
                }

                var result = AppTransactionTemplateDataLoadSetupBL.SaveAppTransactionDataLoadExDto(dto);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save DataLoad failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                var savedId = result?.Object?.Id ?? dto.Id;
                return Ok(new
                {
                    saved = true,
                    dataLoadId = savedId,
                    mappingNote,
                    changed = new[] { new { type = "dataLoad", id = savedId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        public static string DeleteDataLoad(int transactionId, int dataLoadId, bool? confirmed, bool? dryRun)
        {
            try
            {
                var dto = AppTransactionTemplateDataLoadSetupBL.RetrieveOneAppTransactionDataLoadExDto(dataLoadId);
                if (dto == null || dto.Id == null) return Fail($"DataLoad {dataLoadId} not found.");
                if (dto.TransactionId.HasValue && Convert.ToInt32(dto.TransactionId) != transactionId)
                    return Fail($"DataLoad {dataLoadId} does not belong to transaction {transactionId}.");

                if (dryRun == true || confirmed != true)
                {
                    return Ok(new
                    {
                        preview = true,
                        message = "Preview delete. ask_user then confirmed=true.",
                        proposed = new { action = "delete", dataLoadId, loadName = dto.LoadName }
                    });
                }

                var result = AppTransactionTemplateDataLoadSetupBL.DeleteAppTransactionDataLoad(dataLoadId);
                if (result?.ValidationResult?.HasErrors == true)
                {
                    var msgs = result.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                    return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Delete DataLoad failed.");
                }

                AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
                return Ok(new
                {
                    deleted = true,
                    dataLoadId,
                    changed = new[] { new { type = "dataLoad", id = dataLoadId } },
                    NextStep = "Call request_form_reload."
                });
            }
            catch (Exception ex) { return Fail(ex.Message); }
        }

        static AppTranscationDataLoadFieldMappingExDto ParseMappingToken(JToken m, bool isCondition)
        {
            return new AppTranscationDataLoadFieldMappingExDto
            {
                IsConditionMapping = isCondition,
                TransactionFieldId = m.Value<int?>("transactionFieldId") ?? m.Value<int?>("fieldId"),
                DbcolumnName = m.Value<string>("dbColumnName")
                    ?? m.Value<string>("dbcolumnName")
                    ?? m.Value<string>("columnId")
                    ?? m.Value<string>("column"),
                WhereClause = isCondition
                    ? (m.Value<string>("whereClause") ?? m.Value<string>("where") ?? "")
                    : null
            };
        }

        static AppTranscationDataLoadFieldMappingExDto CloneMappingAsNew(AppTranscationDataLoadFieldMappingExDto src, bool isCondition)
        {
            return new AppTranscationDataLoadFieldMappingExDto
            {
                IsConditionMapping = isCondition,
                TransactionFieldId = src.TransactionFieldId,
                DbcolumnName = src.DbcolumnName,
                WhereClause = src.WhereClause
            };
        }

        static object SummarizeMapping(AppTranscationDataLoadFieldMappingExDto m) => new
        {
            mappingId = m.Id,
            transactionFieldId = m.TransactionFieldId,
            dbColumnName = m.DbcolumnName,
            whereClause = m.WhereClause,
            isConditionMapping = m.IsConditionMapping == true
        };

        static object SummarizeDataLoad(AppTransactionDataLoadExDto dto) => new
        {
            dataLoadId = dto.Id,
            loadName = dto.LoadName,
            description = dto.Description,
            transactionId = dto.TransactionId,
            transactionUnitId = dto.TransactionUnitId,
            dataSetId = dto.DataSetId,
            loadOrder = dto.LoadOrder,
            isAutoExecutedWhenOpenEditForm = dto.IsAutoExcutedWhenOpenEditForm,
            isAutoExecuteBeforeInitialCascading = dto.IsAutoExecuteBeforeIntialCscading
        };

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
