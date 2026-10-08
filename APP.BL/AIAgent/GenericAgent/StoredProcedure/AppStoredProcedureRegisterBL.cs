using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using App.BL;
using App.BL.DbGenie;
using App.BL.GenericAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using DatabaseSchemaMrg;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.StoredProcedure
{
    /// <summary>
    /// Tenant table AppStoredProcedureRegister — AI-trained SP catalog for agent discovery.
    /// Table lives on the default tenant DataSource; rows point at DataSourceRegisterId of the SP host.
    /// </summary>
    public static class AppStoredProcedureRegisterBL
    {
        private const string CreateTableSql = @"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'AppStoredProcedureRegister'
)
BEGIN
    CREATE TABLE dbo.AppStoredProcedureRegister (
        Id                      INT IDENTITY(1,1) NOT NULL,
        DataSourceRegisterId    INT            NOT NULL,
        SchemaName              NVARCHAR(128)  NOT NULL CONSTRAINT DF_AppSpReg_Schema DEFAULT (N'dbo'),
        SpName                  NVARCHAR(256)  NOT NULL,
        FullName                NVARCHAR(400)  NOT NULL,
        Description             NVARCHAR(1000) NULL,
        InputJson               NVARCHAR(MAX)  NULL,
        OutputColumnsJson       NVARCHAR(MAX)  NULL,
        SampleJson              NVARCHAR(MAX)  NULL,
        UsageText               NVARCHAR(1000) NULL,
        IsPublishedToAgent      BIT            NOT NULL CONSTRAINT DF_AppSpReg_Published DEFAULT (1),
        DefinitionHash          NVARCHAR(64)   NULL,
        AiAnalyzedAt            DATETIME2      NULL,
        AiModel                 NVARCHAR(100)  NULL,
        AppCreatedDate          DATETIME2      NOT NULL CONSTRAINT DF_AppSpReg_Created DEFAULT (SYSUTCDATETIME()),
        AppModifiedDate         DATETIME2      NOT NULL CONSTRAINT DF_AppSpReg_Modified DEFAULT (SYSUTCDATETIME()),
        AppCreatedById          INT            NULL,
        AppModifiedById         INT            NULL,
        CONSTRAINT PK_AppStoredProcedureRegister PRIMARY KEY (Id),
        CONSTRAINT UQ_AppStoredProcedureRegister_DsSchemaName UNIQUE (DataSourceRegisterId, SchemaName, SpName)
    );
    CREATE INDEX IX_AppStoredProcedureRegister_DsPublished
        ON dbo.AppStoredProcedureRegister (DataSourceRegisterId, IsPublishedToAgent)
        INCLUDE (SpName, SchemaName, FullName);
END";

        public class RegisterRowDto
        {
            public int Id { get; set; }
            public int DataSourceRegisterId { get; set; }
            public string DataSourceName { get; set; }
            public string SchemaName { get; set; }
            public string SpName { get; set; }
            public string FullName { get; set; }
            public string Description { get; set; }
            public string InputJson { get; set; }
            public string OutputColumnsJson { get; set; }
            public string SampleJson { get; set; }
            public string UsageText { get; set; }
            public bool IsPublishedToAgent { get; set; }
            public string DefinitionHash { get; set; }
            public DateTime? AiAnalyzedAt { get; set; }
            public string AiModel { get; set; }
            public DateTime? AppModifiedDate { get; set; }
            /// <summary>Computed: SP not found on live DataSource catalog.</summary>
            public bool IsMissing { get; set; }
            /// <summary>Computed: App API Provider has a Stored Procedure API for this SP.</summary>
            public bool HasAppApi { get; set; }
        }

        public class TrainItem
        {
            public string Schema { get; set; }
            public string SpName { get; set; }
            public List<AppStoredProcedureApiBL.SpApiParameterItem> Parameters { get; set; }
        }

        public class TrainRequest
        {
            public int DataSourceId { get; set; }
            public List<TrainItem> Items { get; set; }
            /// <summary>When true (default), new/updated rows are published to agent tools.</summary>
            public bool PublishToAgent { get; set; } = true;
        }

        public class SaveRequest
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public string UsageText { get; set; }
            public bool? IsPublishedToAgent { get; set; }
            public string InputJson { get; set; }
            public string OutputColumnsJson { get; set; }
        }

        public class BatchDeleteRequest
        {
            public List<int> Ids { get; set; }
        }

        public class BatchSetPublishRequest
        {
            public List<int> Ids { get; set; }
            public bool IsPublishedToAgent { get; set; }
        }

        public class PublishItem
        {
            public int Id { get; set; }
            public bool IsPublishedToAgent { get; set; }
        }

        public class BatchSavePublishRequest
        {
            public List<PublishItem> Items { get; set; }
        }

        public static void EnsureTable()
        {
            var fixture = GetTenantFixture();
            if (fixture == null) return;
            try
            {
                fixture.ExecuteNonQueryResult(CreateTableSql, new List<DbParameter>());
            }
            catch
            {
                // ignore — list/save will surface errors
            }
        }

        public static OperationCallResult<object> ListForManagement(int? dataSourceRegisterId = null)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            try
            {
                EnsureTable();
                var rows = LoadAllRows(dataSourceRegisterId);
                AttachComputedFlags(rows);
                result.Object = new
                {
                    items = rows
                        .OrderBy(r => r.DataSourceName ?? "", StringComparer.OrdinalIgnoreCase)
                        .ThenBy(r => r.SpName ?? "", StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_List_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<object> SaveOne(SaveRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request == null || request.Id <= 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Save_Invalid", ValidationItemType.Error, "Id is required."));
                return result;
            }

            try
            {
                EnsureTable();
                var fixture = GetTenantFixture()
                    ?? throw new InvalidOperationException("No default DataSource for tenant table.");

                var sql = @"
UPDATE dbo.AppStoredProcedureRegister SET
    Description = @Description,
    UsageText = @UsageText,
    InputJson = COALESCE(@InputJson, InputJson),
    OutputColumnsJson = COALESCE(@OutputColumnsJson, OutputColumnsJson),
    IsPublishedToAgent = COALESCE(@IsPublishedToAgent, IsPublishedToAgent),
    AppModifiedDate = SYSUTCDATETIME()
WHERE Id = @Id";

                var pId = fixture.CreateParameter("@Id"); pId.Value = request.Id;
                var pDesc = fixture.CreateParameter("@Description");
                pDesc.Value = (object)Truncate(request.Description, 1000) ?? DBNull.Value;
                var pUsage = fixture.CreateParameter("@UsageText");
                pUsage.Value = (object)Truncate(request.UsageText, 1000) ?? DBNull.Value;
                var pIn = fixture.CreateParameter("@InputJson");
                pIn.Value = string.IsNullOrWhiteSpace(request.InputJson) ? (object)DBNull.Value : request.InputJson;
                var pOut = fixture.CreateParameter("@OutputColumnsJson");
                pOut.Value = string.IsNullOrWhiteSpace(request.OutputColumnsJson) ? (object)DBNull.Value : request.OutputColumnsJson;
                var pPub = fixture.CreateParameter("@IsPublishedToAgent");
                pPub.Value = request.IsPublishedToAgent.HasValue
                    ? (object)(request.IsPublishedToAgent.Value ? 1 : 0)
                    : DBNull.Value;

                fixture.ExecuteNonQueryResult(sql, new List<DbParameter> { pId, pDesc, pUsage, pIn, pOut, pPub });
                result.Object = new { id = request.Id };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Save_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<object> BatchSetPublish(BatchSetPublishRequest request)
        {
            var items = (request?.Ids ?? new List<int>())
                .Distinct()
                .Select(id => new PublishItem { Id = id, IsPublishedToAgent = request.IsPublishedToAgent })
                .ToList();
            return BatchSavePublish(new BatchSavePublishRequest { Items = items });
        }

        /// <summary>Persist per-row IsPublishedToAgent values (from editable Published column).</summary>
        public static OperationCallResult<object> BatchSavePublish(BatchSavePublishRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request?.Items == null || request.Items.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Publish_Invalid", ValidationItemType.Error, "Items are required."));
                return result;
            }

            try
            {
                EnsureTable();
                var fixture = GetTenantFixture()
                    ?? throw new InvalidOperationException("No default DataSource for tenant table.");
                var updated = 0;
                foreach (var item in request.Items.Where(i => i != null && i.Id > 0))
                {
                    var pId = fixture.CreateParameter("@Id"); pId.Value = item.Id;
                    var pPub = fixture.CreateParameter("@IsPublishedToAgent");
                    pPub.Value = item.IsPublishedToAgent ? 1 : 0;
                    fixture.ExecuteNonQueryResult(@"
UPDATE dbo.AppStoredProcedureRegister SET
    IsPublishedToAgent = @IsPublishedToAgent,
    AppModifiedDate = SYSUTCDATETIME()
WHERE Id = @Id", new List<DbParameter> { pId, pPub });
                    updated++;
                }
                result.Object = new { updatedCount = updated };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Publish_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        public static OperationCallResult<object> BatchDelete(BatchDeleteRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request?.Ids == null || request.Ids.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Delete_Invalid", ValidationItemType.Error, "Ids are required."));
                return result;
            }

            try
            {
                EnsureTable();
                var fixture = GetTenantFixture()
                    ?? throw new InvalidOperationException("No default DataSource for tenant table.");
                var deleted = 0;
                foreach (var id in request.Ids.Distinct())
                {
                    var p = fixture.CreateParameter("@Id"); p.Value = id;
                    fixture.ExecuteNonQueryResult(
                        "DELETE FROM dbo.AppStoredProcedureRegister WHERE Id = @Id",
                        new List<DbParameter> { p });
                    deleted++;
                }
                result.Object = new { deletedCount = deleted };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Delete_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        /// <summary>Batch AI train / upsert register rows. Requires AI configured.</summary>
        public static OperationCallResult<object> BatchTrain(TrainRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request == null || request.DataSourceId <= 0 || request.Items == null || request.Items.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Train_Invalid", ValidationItemType.Error, "DataSourceId and Items are required."));
                return result;
            }

            if (!AIConfigSettingBL.IsConfigured())
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Train_NoAi", ValidationItemType.Error, "AI is not configured."));
                return result;
            }

            try
            {
                EnsureTable();
                var hostFixture = AppCacheManagerBL.GetOneDatabaseFixture(request.DataSourceId);
                var engine = hostFixture.SqlServerType ?? EmSqlType.SqlServer;
                var trained = 0;
                var warnings = new List<string>();

                foreach (var item in request.Items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.SpName)) continue;
                    StoredProcedureCatalogBL.ParseName(item.SpName, item.Schema, out var sch, out var name, request.DataSourceId);
                    var fullName = StoredProcedureCatalogBL.FormatFull(sch, name);

                    try
                    {
                        var definition = StoredProcedureCatalogBL.TryLoadDefinition(hostFixture, engine, sch, name);
                        var dbComment = StoredProcedureCatalogBL.LoadDescription(hostFixture, engine, sch, name);
                        var parameters = (item.Parameters != null && item.Parameters.Count > 0)
                            ? item.Parameters
                            : StoredProcedureCatalogBL.LoadParameters(hostFixture, engine, sch, name)
                                .Select(p => new AppStoredProcedureApiBL.SpApiParameterItem
                                {
                                    Name = p.Name,
                                    Type = p.Type,
                                    Direction = p.Direction,
                                    MaxLength = p.MaxLength,
                                    Ordinal = p.Ordinal,
                                    HasDefault = p.HasDefault,
                                    DefaultValue = AppStoredProcedureApiBL.PlaceholderDefault(p.Type, p.HasDefault),
                                }).ToList();

                        string sampleJson = null;
                        IList<string> returnColumns = null;
                        if (AppStoredProcedureApiBL.IsLikelyReadOnlyProcedure(definition)
                            || AppStoredProcedureApiBL.IsReaderProcedureName(name))
                        {
                            try
                            {
                                sampleJson = Truncate(CaptureSample(request.DataSourceId, sch, name, parameters), 100_000);
                                returnColumns = TryExtractColumns(sampleJson);
                            }
                            catch (Exception ex)
                            {
                                warnings.Add($"{fullName}: sample — {ex.Message}");
                            }
                        }

                        LlmAnalysis analysis = null;
                        try
                        {
                            analysis = AnalyzeWithLlm(fullName, dbComment, definition, parameters, returnColumns);
                        }
                        catch (Exception ex)
                        {
                            // Do not skip the row — LLM often returns truncated/non-JSON for large Get* procs.
                            warnings.Add($"{fullName}: AI analysis failed — {ex.Message}. Saved with fallback metadata.");
                        }

                        var inputJson = analysis?.InputJson
                            ?? JsonConvert.SerializeObject(parameters.Select(p => new
                            {
                                name = p.Name,
                                type = p.Type,
                                direction = p.Direction,
                                hasDefault = p.HasDefault,
                                defaultValue = p.DefaultValue,
                            }));
                        var outputJson = analysis?.OutputColumnsJson
                            ?? (returnColumns != null
                                ? JsonConvert.SerializeObject(returnColumns)
                                : null);
                        var description = !string.IsNullOrWhiteSpace(analysis?.Description)
                            ? analysis.Description
                            : AppStoredProcedureApiBL.BuildFallbackApiDescription(fullName, dbComment, parameters);
                        var usage = !string.IsNullOrWhiteSpace(analysis?.Usage)
                            ? analysis.Usage
                            : AppStoredProcedureApiBL.BuildSpUsage(fullName, dbComment, parameters);

                        UpsertRow(new RegisterRowDto
                        {
                            DataSourceRegisterId = request.DataSourceId,
                            SchemaName = sch ?? "dbo",
                            SpName = name,
                            FullName = fullName,
                            Description = Truncate(description, 1000),
                            InputJson = inputJson,
                            OutputColumnsJson = outputJson,
                            SampleJson = sampleJson,
                            UsageText = Truncate(usage, 1000),
                            IsPublishedToAgent = request.PublishToAgent,
                            DefinitionHash = HashText(definition),
                            AiAnalyzedAt = analysis != null ? DateTime.UtcNow : (DateTime?)null,
                            AiModel = analysis != null ? AIConfigSettingBL.GetModel() : null,
                        });
                        trained++;
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"{fullName}: {ex.Message}");
                    }
                }

                foreach (var w in warnings)
                {
                    validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                        "SpReg_Train_Warning", ValidationItemType.Warning, w));
                }

                result.Object = new { trainedCount = trained, warnings };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpReg_Train_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        // ── Agent tools (published register only) ────────────────────────────

        public static string ListForAgentJson(int? dataSourceId, int skip = 0, int take = 50)
        {
            try
            {
                EnsureTable();
                take = Math.Clamp(take <= 0 ? 50 : take, 1, 200);
                skip = Math.Max(0, skip);
                var rows = LoadAllRows(dataSourceId)
                    .Where(r => r.IsPublishedToAgent)
                    .OrderBy(r => r.SpName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var page = rows.Skip(skip).Take(take).Select(r => ToAgentListItem(r)).ToList();
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    source = "AppStoredProcedureRegister",
                    dataSourceId,
                    total = rows.Count,
                    skip,
                    take,
                    procedures = page,
                    next = "Use stored_procedure_search for keywords, then stored_procedure_detail, then stored_procedure_execute."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message, dataSourceId }, Formatting.Indented);
            }
        }

        public static string SearchForAgentJson(int? dataSourceId, string query, int take = 30)
        {
            try
            {
                EnsureTable();
                if (string.IsNullOrWhiteSpace(query))
                    return JsonConvert.SerializeObject(new { ok = false, error = "query is required." }, Formatting.Indented);

                take = Math.Clamp(take <= 0 ? 30 : take, 1, 100);
                var needle = query.Trim();
                var tokens = Regex.Split(needle, @"[^0-9a-zA-Z_]+")
                    .Where(t => t.Length >= 2).Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToList();

                var scored = new List<(RegisterRowDto Row, int Score)>();
                foreach (var r in LoadAllRows(dataSourceId).Where(x => x.IsPublishedToAgent))
                {
                    var blob = $"{r.FullName} {r.SpName} {r.SchemaName} {r.Description} {r.InputJson} {r.OutputColumnsJson} {r.UsageText}";
                    var score = 0;
                    if (Contains(r.SpName, needle) || Contains(r.FullName, needle)) score += 80;
                    if (Contains(r.Description, needle)) score += 40;
                    foreach (var t in tokens)
                    {
                        if (Contains(r.SpName, t)) score += 30;
                        if (Contains(r.Description, t)) score += 20;
                        if (Contains(r.InputJson, t)) score += 15;
                        if (Contains(r.OutputColumnsJson, t)) score += 12;
                        if (Contains(blob, t)) score += 5;
                    }
                    if (score > 0) scored.Add((r, score));
                }

                var hits = scored.OrderByDescending(x => x.Score)
                    .Take(take)
                    .Select(x => ToAgentListItem(x.Row, x.Score))
                    .ToList();

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    source = "AppStoredProcedureRegister",
                    dataSourceId,
                    query = needle,
                    count = hits.Count,
                    procedures = hits,
                    next = "Call stored_procedure_detail with procedureName (and dataSourceId) before execute."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message, dataSourceId }, Formatting.Indented);
            }
        }

        public static string DetailForAgentJson(int? dataSourceId, string procedureName, string schema = null)
        {
            try
            {
                EnsureTable();
                if (string.IsNullOrWhiteSpace(procedureName))
                    return JsonConvert.SerializeObject(new { ok = false, error = "procedureName is required." }, Formatting.Indented);

                ParseLooseName(procedureName, schema, out var sch, out var name);
                var rows = LoadAllRows(dataSourceId)
                    .Where(r => r.IsPublishedToAgent
                        && string.Equals(r.SpName, name, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrWhiteSpace(sch)
                            || string.Equals(r.SchemaName, sch, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (rows.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"No published AI register entry for '{procedureName}'. Train it in Stored Procedure AI Register and set IsPublishedToAgent."
                    }, Formatting.Indented);
                }

                var row = rows.Count == 1
                    ? rows[0]
                    : rows.FirstOrDefault(r => dataSourceId == null || r.DataSourceRegisterId == dataSourceId)
                      ?? rows[0];

                object input = null;
                object output = null;
                try { if (!string.IsNullOrWhiteSpace(row.InputJson)) input = JToken.Parse(row.InputJson); } catch { input = row.InputJson; }
                try { if (!string.IsNullOrWhiteSpace(row.OutputColumnsJson)) output = JToken.Parse(row.OutputColumnsJson); } catch { output = row.OutputColumnsJson; }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    source = "AppStoredProcedureRegister",
                    dataSourceId = row.DataSourceRegisterId,
                    schema = row.SchemaName,
                    name = row.SpName,
                    fullName = row.FullName,
                    description = row.Description,
                    usage = row.UsageText,
                    inputParameters = input,
                    outputColumns = output,
                    sampleJson = Truncate(row.SampleJson, 8000),
                    aiAnalyzedAt = row.AiAnalyzedAt,
                    next = "Call stored_procedure_execute with procedureName, argsJson, and dataSourceId=" + row.DataSourceRegisterId
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message }, Formatting.Indented);
            }
        }

        // ── Internals ────────────────────────────────────────────────────────

        private static DatabaseFixture GetTenantFixture()
        {
            var dsId = AppDataSourceRegisterBL.GetDefaultDataSourceRegId();
            if (dsId == null || dsId.Value <= 0) return null;
            return AppCacheManagerBL.GetOneDatabaseFixture(dsId.Value);
        }

        private static List<RegisterRowDto> LoadAllRows(int? dataSourceRegisterId)
        {
            var fixture = GetTenantFixture()
                ?? throw new InvalidOperationException("No default DataSource for tenant table.");
            EnsureTable();

            var sql = @"
SELECT Id, DataSourceRegisterId, SchemaName, SpName, FullName, Description,
       InputJson, OutputColumnsJson, SampleJson, UsageText, IsPublishedToAgent,
       DefinitionHash, AiAnalyzedAt, AiModel, AppModifiedDate
FROM dbo.AppStoredProcedureRegister";
            var pars = new List<DbParameter>();
            if (dataSourceRegisterId is > 0)
            {
                sql += " WHERE DataSourceRegisterId = @DsId";
                var p = fixture.CreateParameter("@DsId");
                p.Value = dataSourceRegisterId.Value;
                pars.Add(p);
            }

            var dt = fixture.RetriveDataTable(sql, pars);
            var dsNames = LoadDataSourceNames();
            var list = new List<RegisterRowDto>();
            foreach (DataRow row in dt.Rows)
            {
                var dsId = Convert.ToInt32(row["DataSourceRegisterId"]);
                dsNames.TryGetValue(dsId, out var dsName);
                list.Add(new RegisterRowDto
                {
                    Id = Convert.ToInt32(row["Id"]),
                    DataSourceRegisterId = dsId,
                    DataSourceName = dsName ?? $"DS {dsId}",
                    SchemaName = row["SchemaName"]?.ToString(),
                    SpName = row["SpName"]?.ToString(),
                    FullName = row["FullName"]?.ToString(),
                    Description = row["Description"]?.ToString(),
                    InputJson = row["InputJson"]?.ToString(),
                    OutputColumnsJson = row["OutputColumnsJson"]?.ToString(),
                    SampleJson = row["SampleJson"]?.ToString(),
                    UsageText = row["UsageText"]?.ToString(),
                    IsPublishedToAgent = row["IsPublishedToAgent"] != DBNull.Value && Convert.ToBoolean(row["IsPublishedToAgent"]),
                    DefinitionHash = row["DefinitionHash"]?.ToString(),
                    AiAnalyzedAt = row["AiAnalyzedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["AiAnalyzedAt"]),
                    AiModel = row["AiModel"]?.ToString(),
                    AppModifiedDate = row["AppModifiedDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(row["AppModifiedDate"]),
                });
            }
            return list;
        }

        private static Dictionary<int, string> LoadDataSourceNames()
        {
            var map = new Dictionary<int, string>();
            try
            {
                var list = AppDataSourceRegisterBL.RetrieveAllAppDataSourceRegisterExDto();
                if (list == null) return map;
                foreach (var ds in list)
                {
                    if (ds?.Id == null) continue;
                    var id = Convert.ToInt32(ds.Id);
                    if (id == AppDataSourceRegisterBL.MasterDataSourceRegisterId) continue;
                    map[id] = ds.DataSourceName ?? $"DS {id}";
                }
            }
            catch { /* ignore */ }
            return map;
        }

        private static void AttachComputedFlags(List<RegisterRowDto> rows)
        {
            if (rows == null || rows.Count == 0) return;

            var appApiKeys = LoadAppSpApiKeys();
            var liveByDs = new Dictionary<int, HashSet<string>>();

            foreach (var group in rows.GroupBy(r => r.DataSourceRegisterId))
            {
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var raw = StoredProcedureCatalogBL.ListJson(group.Key, null, 0, 500);
                    var jo = JObject.Parse(raw);
                    var arr = jo["procedures"] as JArray;
                    if (arr != null)
                    {
                        foreach (var p in arr)
                        {
                            var full = p["FullName"]?.ToString() ?? p["fullName"]?.ToString();
                            var sch = p["Schema"]?.ToString() ?? p["schema"]?.ToString() ?? "dbo";
                            var name = p["Name"]?.ToString() ?? p["name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(full)) set.Add(full);
                            if (!string.IsNullOrWhiteSpace(name))
                                set.Add(StoredProcedureCatalogBL.FormatFull(sch, name));
                        }
                    }
                }
                catch { /* live unavailable → don't mark missing */ set = null; }

                liveByDs[group.Key] = set;
            }

            foreach (var r in rows)
            {
                r.HasAppApi = RegisterRowUsedByAppApi(appApiKeys, r);

                if (liveByDs.TryGetValue(r.DataSourceRegisterId, out var live) && live != null)
                {
                    r.IsMissing = !(live.Contains(r.FullName)
                        || live.Contains($"{r.SchemaName}.{r.SpName}")
                        || live.Contains(r.SpName));
                }
            }
        }

        private static bool RegisterRowUsedByAppApi(HashSet<string> keys, RegisterRowDto row)
        {
            if (keys == null || keys.Count == 0 || row == null) return false;
            var ds = row.DataSourceRegisterId;
            var full = (row.FullName ?? "").Trim();
            var schema = string.IsNullOrWhiteSpace(row.SchemaName) ? "dbo" : row.SchemaName.Trim();
            var name = (row.SpName ?? "").Trim();
            var schemaName = schema + "." + name;
            return keys.Contains(ds + "|" + full)
                || keys.Contains(ds + "|" + schemaName)
                || keys.Contains(ds + "|" + name)
                || keys.Contains("0|" + full)
                || keys.Contains("0|" + schemaName)
                || keys.Contains("0|" + name);
        }

        private static void AddProcKeys(HashSet<string> set, int dsId, string procRef)
        {
            if (set == null || string.IsNullOrWhiteSpace(procRef)) return;
            var raw = procRef.Trim().Replace("[", "").Replace("]", "");
            var space = raw.IndexOf(' ');
            if (space > 0) raw = raw.Substring(0, space);
            raw = raw.Trim();
            if (raw.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase)) return;
            set.Add(dsId + "|" + raw);
            var dot = raw.LastIndexOf('.');
            if (dot > 0 && dot < raw.Length - 1)
            {
                var schema = raw.Substring(0, dot);
                var name = raw.Substring(dot + 1);
                set.Add(dsId + "|" + schema + "." + name);
                set.Add(dsId + "|" + name);
            }
            else
            {
                set.Add(dsId + "|dbo." + raw);
                set.Add(dsId + "|" + raw);
            }
        }

        /// <summary>
        /// App API Provider operations created from stored procedures store the procedure in JsonQuery.
        /// The integration list API omits JsonQuery, so this reads those rows directly.
        /// </summary>
        private static HashSet<string> LoadAppSpApiKeys()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
                {
                    var list = new EntityCollection<AppIntergrationSettingParameterEntity>();
                    var exclude = new ExcludeFieldsList();
                    exclude.Add(AppIntergrationSettingParameterFields.ApiconfigParameters);
                    exclude.Add(AppIntergrationSettingParameterFields.JsonSampleData);
                    exclude.Add(AppIntergrationSettingParameterFields.JsonSchema);
                    exclude.Add(AppIntergrationSettingParameterFields.SchemaDataSetMapping);
                    exclude.Add(AppIntergrationSettingParameterFields.SchemaFromDataSetMapping);
                    exclude.Add(AppIntergrationSettingParameterFields.PostProcessScript);

                    var filter = new RelationPredicateBucket(
                        AppIntergrationSettingParameterFields.IntergrationSettingId == AppIntergrationSettingBL.AppBuiltInProviderId);
                    adapter.FetchEntityCollection(list, exclude, filter);

                    foreach (var op in list)
                    {
                        if (op == null) continue;
                        var usage = op.MappingInternalCode;
                        if (!string.IsNullOrWhiteSpace(usage)
                            && !string.Equals(usage,
                                EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(),
                                StringComparison.OrdinalIgnoreCase))
                            continue;

                        var action = op.ActionCode ?? "";
                        if (!action.StartsWith("AppSp_", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var jq = op.JsonQuery?.Trim();
                        if (string.IsNullOrWhiteSpace(jq)) continue;
                        AddProcKeys(set, op.DataSourceId ?? 0, jq);
                    }
                }
            }
            catch { /* ignore */ }
            return set;
        }

        private static void UpsertRow(RegisterRowDto row)
        {
            var fixture = GetTenantFixture()
                ?? throw new InvalidOperationException("No default DataSource for tenant table.");

            var pars = new List<DbParameter>
            {
                P(fixture, "@DataSourceRegisterId", row.DataSourceRegisterId),
                P(fixture, "@SchemaName", row.SchemaName ?? "dbo"),
                P(fixture, "@SpName", row.SpName),
                P(fixture, "@FullName", row.FullName),
                P(fixture, "@Description", (object)row.Description ?? DBNull.Value),
                P(fixture, "@InputJson", (object)row.InputJson ?? DBNull.Value),
                P(fixture, "@OutputColumnsJson", (object)row.OutputColumnsJson ?? DBNull.Value),
                P(fixture, "@SampleJson", (object)row.SampleJson ?? DBNull.Value),
                P(fixture, "@UsageText", (object)row.UsageText ?? DBNull.Value),
                P(fixture, "@IsPublishedToAgent", row.IsPublishedToAgent ? 1 : 0),
                P(fixture, "@DefinitionHash", (object)row.DefinitionHash ?? DBNull.Value),
                P(fixture, "@AiAnalyzedAt", row.AiAnalyzedAt.HasValue ? (object)row.AiAnalyzedAt.Value : DBNull.Value),
                P(fixture, "@AiModel", (object)row.AiModel ?? DBNull.Value),
            };

            // Prefer UPDATE/INSERT over MERGE — more reliable across fixture wrappers.
            const string updateSql = @"
UPDATE dbo.AppStoredProcedureRegister SET
    FullName = @FullName,
    Description = @Description,
    InputJson = @InputJson,
    OutputColumnsJson = @OutputColumnsJson,
    SampleJson = @SampleJson,
    UsageText = @UsageText,
    IsPublishedToAgent = @IsPublishedToAgent,
    DefinitionHash = @DefinitionHash,
    AiAnalyzedAt = @AiAnalyzedAt,
    AiModel = @AiModel,
    AppModifiedDate = SYSUTCDATETIME()
WHERE DataSourceRegisterId = @DataSourceRegisterId AND SchemaName = @SchemaName AND SpName = @SpName";

            const string existsSql = @"
SELECT COUNT(1) AS Cnt FROM dbo.AppStoredProcedureRegister
WHERE DataSourceRegisterId = @DataSourceRegisterId AND SchemaName = @SchemaName AND SpName = @SpName";

            var existsPars = new List<DbParameter>
            {
                P(fixture, "@DataSourceRegisterId", row.DataSourceRegisterId),
                P(fixture, "@SchemaName", row.SchemaName ?? "dbo"),
                P(fixture, "@SpName", row.SpName),
            };
            var dt = fixture.RetriveDataTable(existsSql, existsPars);
            var exists = dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["Cnt"]) > 0;

            if (exists)
            {
                fixture.ExecuteNonQueryResult(updateSql, pars);
                return;
            }

            const string insertSql = @"
INSERT INTO dbo.AppStoredProcedureRegister
    (DataSourceRegisterId, SchemaName, SpName, FullName, Description, InputJson, OutputColumnsJson,
     SampleJson, UsageText, IsPublishedToAgent, DefinitionHash, AiAnalyzedAt, AiModel)
VALUES
    (@DataSourceRegisterId, @SchemaName, @SpName, @FullName, @Description, @InputJson, @OutputColumnsJson,
     @SampleJson, @UsageText, @IsPublishedToAgent, @DefinitionHash, @AiAnalyzedAt, @AiModel)";
            fixture.ExecuteNonQueryResult(insertSql, pars);
        }

        private static DbParameter P(DatabaseFixture fixture, string name, object value)
        {
            var p = fixture.CreateParameter(name);
            p.Value = value ?? DBNull.Value;
            return p;
        }

        private sealed class LlmAnalysis
        {
            public string Description { get; set; }
            public string InputJson { get; set; }
            public string OutputColumnsJson { get; set; }
            public string Usage { get; set; }
        }

        private static LlmAnalysis AnalyzeWithLlm(
            string fullName,
            string dbComment,
            string definition,
            List<AppStoredProcedureApiBL.SpApiParameterItem> parameters,
            IList<string> returnColumns)
        {
            // Input/output structure come from catalog + sample — do NOT ask the LLM for JSON
            // (large Get* procs routinely truncate mid-string and break JObject.Parse).
            var inputJson = JsonConvert.SerializeObject(
                (parameters ?? new List<AppStoredProcedureApiBL.SpApiParameterItem>())
                    .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                    .OrderBy(p => p.Ordinal)
                    .Select(p => new
                    {
                        name = p.Name,
                        type = p.Type,
                        direction = p.Direction,
                        hasDefault = p.HasDefault,
                        defaultValue = p.DefaultValue,
                    }));
            var outputJson = returnColumns != null && returnColumns.Count > 0
                ? JsonConvert.SerializeObject(returnColumns)
                : null;

            // Plain-text description only (same style as SP API description generator).
            var description = AppStoredProcedureApiBL.GenerateApiDescriptionWithLlm(
                fullName, dbComment, definition, parameters, returnColumns);
            if (string.IsNullOrWhiteSpace(description))
                description = AppStoredProcedureApiBL.BuildFallbackApiDescription(fullName, dbComment, parameters);

            return new LlmAnalysis
            {
                Description = Truncate(description, 1000),
                Usage = Truncate(AppStoredProcedureApiBL.BuildSpUsage(fullName, dbComment, parameters), 1000),
                InputJson = inputJson,
                OutputColumnsJson = outputJson,
            };
        }

        private static string CaptureSample(
            int dataSourceId,
            string schema,
            string spName,
            List<AppStoredProcedureApiBL.SpApiParameterItem> parameters)
        {
            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parameters ?? new List<AppStoredProcedureApiBL.SpApiParameterItem>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
                var d = (p.Direction ?? "IN").ToUpperInvariant();
                if ((d == "OUT" || d == "OUTPUT") && p.DefaultValue == null) continue;
                if (p.HasDefault && p.DefaultValue == null) continue;
                var bare = p.Name.TrimStart('@', ':');
                args[bare] = ParseToken(p.DefaultValue);
            }
            return StoredProcedureExecuteBL.ExecuteJson(
                dataSourceId, spName, schema, JsonConvert.SerializeObject(args));
        }

        private static IList<string> TryExtractColumns(string sampleJson)
        {
            if (string.IsNullOrWhiteSpace(sampleJson)) return null;
            try
            {
                var jo = JObject.Parse(sampleJson);
                if (jo["ok"]?.Value<bool>() != true && jo["IsSuccess"]?.Value<bool>() != true)
                    return null;
                var arr = jo["columnNames"] as JArray ?? jo["ColumnNames"] as JArray;
                if (arr == null || arr.Count == 0) return null;
                return arr.Select(x => x?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            catch { return null; }
        }

        private static object ParseToken(string value)
        {
            if (value == null) return null;
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (long.TryParse(value, out var l)) return l;
            if (decimal.TryParse(value, out var d)) return d;
            return value;
        }

        private static object ToAgentListItem(RegisterRowDto r, int? score = null)
        {
            return new
            {
                Schema = r.SchemaName,
                Name = r.SpName,
                FullName = r.FullName,
                Description = r.Description,
                DataSourceId = r.DataSourceRegisterId,
                DataSourceName = r.DataSourceName,
                InputHint = Truncate(r.InputJson, 200),
                OutputHint = Truncate(r.OutputColumnsJson, 200),
                score,
            };
        }

        private static void ParseLooseName(string procedureName, string schema, out string schemaOut, out string nameOut)
        {
            nameOut = (procedureName ?? "").Trim().Trim('[', ']', '`', '"');
            schemaOut = string.IsNullOrWhiteSpace(schema) ? null : schema.Trim().Trim('[', ']', '`', '"');
            if (schemaOut == null && nameOut.Contains("."))
            {
                var parts = nameOut.Split(new[] { '.' }, 2);
                schemaOut = parts[0].Trim().Trim('[', ']', '`', '"');
                nameOut = parts[1].Trim().Trim('[', ']', '`', '"');
            }
        }

        private static bool Contains(string hay, string needle) =>
            !string.IsNullOrEmpty(hay) && !string.IsNullOrEmpty(needle)
            && hay.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
            return text.Substring(0, max) + "…";
        }

        private static string HashText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 32);
        }
    }
}
