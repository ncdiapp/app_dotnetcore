using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL.AIAgent.GenericAgent.StoredProcedure;
using App.BL.DbGenie;
using App.BL.GenericAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.EntityClasses;
using DatabaseSchemaMrg.DataSchema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL
{
    /// <summary>
    /// Batch create / delete App API Provider operations that wrap stored procedures.
    /// </summary>
    public static class AppStoredProcedureApiBL
    {
        public const string ApiTypeDisplayName = "Stored Procedure API";

        public class SpApiParameterItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Direction { get; set; }
            public int? MaxLength { get; set; }
            public int Ordinal { get; set; }
            public bool HasDefault { get; set; }
            public string DefaultValue { get; set; }
        }

        public class SpApiCreateItem
        {
            public string Schema { get; set; }
            public string SpName { get; set; }
            public string ActionCode { get; set; }
            /// <summary>Not written to ActionDescription (generator Usage only).</summary>
            public string Description { get; set; }
            /// <summary>Legacy per-item flag; prefer <see cref="SpApiCreateRequest.CaptureSampleOnGenerate"/>.</summary>
            public bool CaptureSample { get; set; }
            public List<SpApiParameterItem> Parameters { get; set; }
        }

        public class SpApiCreateRequest
        {
            public int DataSourceId { get; set; }
            /// <summary>When true and AI is configured, LLM writes ActionDescription (English, ≤500).</summary>
            public bool GenerateAiDescription { get; set; }
            /// <summary>
            /// When true (default), auto-execute read-like SPs and persist JsonSampleData.
            /// Non-read SPs are never executed.
            /// </summary>
            public bool CaptureSampleOnGenerate { get; set; } = true;
            public List<SpApiCreateItem> Items { get; set; }
        }

        public class SpApiGenerateDescriptionRequest
        {
            public int DataSourceId { get; set; }
            public string Schema { get; set; }
            public string SpName { get; set; }
            public List<SpApiParameterItem> Parameters { get; set; }
            public string ExistingDescription { get; set; }
        }

        public class SpApiBatchDeleteRequest
        {
            public List<int> Ids { get; set; }
        }

        public class SpCatalogListItemDto
        {
            public string Schema { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            /// <summary>DB MS_Description / comment only (for API Description when AI off).</summary>
            public string DbComment { get; set; }
            /// <summary>Generator grid Usage: DB comment + Execute/params (not API Description).</summary>
            public string Usage { get; set; }
            /// <summary>Backward-compatible alias of Usage.</summary>
            public string Description { get; set; }
            public string SuggestedActionCode { get; set; }
            public List<SpApiParameterItem> Parameters { get; set; }
        }

        public static bool IsStoredProcedureApi(AppIntergrationSettingParameterExDto dto)
        {
            return dto?.APIConfigParameters != null && dto.APIConfigParameters.IsStoredProcedureApi;
        }

        public static List<SpCatalogListItemDto> ListProceduresForApiBuilder(int dataSourceId, string schema = null, int take = 500)
        {
            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
            // Reuse catalog list via public ListJson parse, or call internal through Detail.
            var raw = StoredProcedureCatalogBL.ListJson(dataSourceId, schema, 0, Math.Clamp(take, 1, 500));
            var jo = JObject.Parse(raw);
            if (jo["ok"]?.Value<bool>() != true)
                throw new InvalidOperationException(jo["error"]?.ToString() ?? "Failed to list stored procedures.");

            var result = new List<SpCatalogListItemDto>();
            var procs = jo["procedures"] as JArray;
            if (procs == null) return result;

            foreach (var p in procs)
            {
                var sch = p["Schema"]?.ToString() ?? p["schema"]?.ToString();
                var name = p["Name"]?.ToString() ?? p["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var paramMeta = StoredProcedureCatalogBL.LoadParameters(fixture, engine, sch, name);
                var parameters = paramMeta.Select(ToApiParamWithDefault).ToList();
                var fullName = p["FullName"]?.ToString() ?? StoredProcedureCatalogBL.FormatFull(sch, name);
                var dbComment = p["Description"]?.ToString() ?? p["description"]?.ToString();
                var usage = BuildSpUsage(fullName, dbComment, parameters);

                result.Add(new SpCatalogListItemDto
                {
                    Schema = sch,
                    Name = name,
                    FullName = fullName,
                    DbComment = dbComment,
                    Usage = usage,
                    Description = usage,
                    SuggestedActionCode = BuildDefaultActionCode(name),
                    Parameters = parameters,
                });
            }

            return result;
        }

        /// <summary>
        /// Reload one SP's metadata + placeholder defaults (for editor Reset Parameters).
        /// </summary>
        public static SpCatalogListItemDto GetProcedureForApiBuilder(int dataSourceId, string schema, string spName)
        {
            if (dataSourceId <= 0)
                throw new ArgumentException("DataSourceId is required.", nameof(dataSourceId));
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("SpName is required.", nameof(spName));

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(dataSourceId);
            var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
            StoredProcedureCatalogBL.ParseName(spName, schema, out var sch, out var name, dataSourceId);

            var paramMeta = StoredProcedureCatalogBL.LoadParameters(fixture, engine, sch, name);
            var parameters = paramMeta.Select(ToApiParamWithDefault).ToList();
            var fullName = StoredProcedureCatalogBL.FormatFull(sch, name);
            var dbComment = StoredProcedureCatalogBL.LoadDescription(fixture, engine, sch, name);
            var usage = BuildSpUsage(fullName, dbComment, parameters);

            return new SpCatalogListItemDto
            {
                Schema = sch,
                Name = name,
                FullName = fullName,
                DbComment = dbComment,
                Usage = usage,
                Description = usage,
                SuggestedActionCode = BuildDefaultActionCode(name),
                Parameters = parameters,
            };
        }

        public static OperationCallResult<object> BatchCreate(SpApiCreateRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request == null || request.DataSourceId <= 0 || request.Items == null || request.Items.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchCreate_Invalid", ValidationItemType.Error, "DataSourceId and at least one item are required."));
                return result;
            }

            AppIntergrationSettingBL.EnsureAppBuiltInProviderExists();

            var fixture = AppCacheManagerBL.GetOneDatabaseFixture(request.DataSourceId);
            var engine = (fixture.SqlServerType ?? EmSqlType.SqlServer).ToString();

            var created = new List<object>();
            var warnings = new List<string>();
            var sampleCaptured = 0;
            var sampleSkipped = 0;

            foreach (var item in request.Items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.SpName))
                {
                    warnings.Add("Skipped item with empty SpName.");
                    continue;
                }

                var actionCode = string.IsNullOrWhiteSpace(item.ActionCode)
                    ? BuildDefaultActionCode(item.SpName)
                    : item.ActionCode.Trim();

                var parameters = (item.Parameters ?? new List<SpApiParameterItem>())
                    .Select(NormalizeParam)
                    .ToList();

                var apiConfig = new APIConfigParameterDTO
                {
                    IsStoredProcedureApi = true,
                    SpName = item.SpName.Trim(),
                    SpSchema = string.IsNullOrWhiteSpace(item.Schema) ? null : item.Schema.Trim(),
                    SpEngine = engine,
                    Method = EnumHttpMethod.Post,
                    SpParameters = parameters.Select(p => new StoredProcedureApiParameterDTO
                    {
                        Name = p.Name,
                        Type = p.Type,
                        Direction = p.Direction,
                        MaxLength = p.MaxLength,
                        Ordinal = p.Ordinal,
                        HasDefault = p.HasDefault,
                        DefaultValue = p.DefaultValue,
                    }).ToList(),
                };

                var fullName = StoredProcedureCatalogBL.FormatFull(item.Schema, item.SpName);
                string sampleJson = null;
                IList<string> returnColumns = null;
                var captured = false;

                var engineEnum = fixture.SqlServerType ?? EmSqlType.SqlServer;
                var definition = StoredProcedureCatalogBL.TryLoadDefinition(
                    fixture, engineEnum, item.Schema, item.SpName);
                var dbComment = StoredProcedureCatalogBL.LoadDescription(
                    fixture, engineEnum, item.Schema, item.SpName);
                // Auto-execute + persist JsonSampleData when enabled and SP looks read-only / Get*/List*…
                var readOnlyBody = IsLikelyReadOnlyProcedure(definition);
                var readerName = IsReaderProcedureName(item.SpName);
                var trySample = request.CaptureSampleOnGenerate && (readOnlyBody || readerName);
                if (trySample)
                {
                    try
                    {
                        sampleJson = CaptureSampleJson(request.DataSourceId, item.Schema, item.SpName, parameters);
                        returnColumns = TryExtractReturnColumns(sampleJson);
                        if (returnColumns == null && !IsSampleOk(sampleJson))
                            warnings.Add($"{actionCode}: sample capture returned an error payload.");
                        else if (IsSampleOk(sampleJson))
                            captured = true;
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"{actionCode}: sample capture failed — {ex.Message}");
                    }
                }
                else if (!request.CaptureSampleOnGenerate)
                {
                    // User opted out — do not count as "unsafe SP skipped".
                }
                else
                {
                    sampleSkipped++;
                }

                string actionDescription = null;
                if (request.GenerateAiDescription && AIConfigSettingBL.IsConfigured())
                {
                    try
                    {
                        actionDescription = GenerateApiDescriptionWithLlm(
                            fullName, dbComment, definition, parameters, returnColumns);
                        if (!IsUsableApiDescription(actionDescription))
                        {
                            actionDescription = BuildFallbackApiDescription(fullName, dbComment, parameters);
                            warnings.Add($"{actionCode}: AI description unusable — used fallback.");
                        }
                    }
                    catch (Exception ex)
                    {
                        actionDescription = BuildFallbackApiDescription(fullName, dbComment, parameters);
                        warnings.Add($"{actionCode}: AI description failed — {ex.Message}");
                    }
                }
                else
                {
                    // No AI: DB comment or SP/params fallback (never generator Usage / Execute…params).
                    actionDescription = BuildFallbackApiDescription(fullName, dbComment, parameters);
                }

                var dto = new AppIntergrationSettingParameterExDto
                {
                    IntergrationSettingId = AppIntergrationSettingBL.AppBuiltInProviderId,
                    IsSimpleQuery = false,
                    HttpMethd = "Post",
                    DataSourceId = request.DataSourceId,
                    ActionCode = actionCode,
                    ActionDescription = actionDescription,
                    JsonQuery = fullName,
                    MappingInternalCode = EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(),
                    APIConfigParameters = apiConfig,
                    ApiconfigParameters = JsonConvert.SerializeObject(apiConfig),
                    JsonSampleData = sampleJson,
                };

                var saveResult = AppIntergrationSettingBL.SaveAppIntergrationSettingParameterExDto(dto);
                validation.Merge(saveResult.ValidationResult);
                if (saveResult.IsSuccessfulWithResult)
                {
                    if (captured) sampleCaptured++;
                    created.Add(new
                    {
                        Id = saveResult.Object.Id,
                        ActionCode = saveResult.Object.ActionCode,
                        CapturedSample = captured && !string.IsNullOrWhiteSpace(saveResult.Object.JsonSampleData),
                        SampleSkipped = !trySample,
                    });
                }
                else
                {
                    warnings.Add($"{actionCode}: save failed.");
                }
            }

            if (sampleSkipped > 0)
            {
                warnings.Add(
                    $"Sample auto-capture skipped for {sampleSkipped} SP(s) (definition missing or contains INSERT/UPDATE/DELETE/EXEC/…).");
            }

            foreach (var w in warnings)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchCreate_Warning", ValidationItemType.Warning, w));
            }

            result.Object = new { createdCount = created.Count, created, warnings, sampleCaptured, sampleSkipped };
            return result;
        }

        public static OperationCallResult<object> BatchDelete(SpApiBatchDeleteRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request?.Ids == null || request.Ids.Count == 0)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_BatchDelete_Invalid", ValidationItemType.Error, "Ids are required."));
                return result;
            }

            var deleted = 0;
            foreach (var id in request.Ids.Distinct())
            {
                var one = AppIntergrationSettingBL.DeleteOneAppIntergrationSettingParameter(id);
                validation.Merge(one.ValidationResult);
                if (!one.ValidationResult.HasErrors)
                    deleted++;
            }

            result.Object = new { deletedCount = deleted };
            return result;
        }

        /// <summary>
        /// Merge request args over stored defaults for SP invoke.
        /// </summary>
        public static string BuildArgsJsonFromDefaultsAndBody(AppIntergrationSettingParameterExDto setting, string requestBody)
        {
            var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (setting?.APIConfigParameters?.SpParameters != null)
            {
                foreach (var p in setting.APIConfigParameters.SpParameters)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
                    var bare = p.Name.TrimStart('@', ':');
                    if (IsOutputOnly(p.Direction) && string.IsNullOrEmpty(p.DefaultValue))
                        continue;
                    if (p.HasDefault && (p.DefaultValue == null))
                        continue;
                    map[bare] = ParseDefaultToken(p.DefaultValue);
                }
            }

            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                try
                {
                    var jo = JObject.Parse(requestBody);
                    var args = jo["args"] as JObject ?? jo;
                    foreach (var prop in args.Properties())
                    {
                        map[prop.Name.TrimStart('@', ':')] = prop.Value?.Type == JTokenType.Null
                            ? null
                            : prop.Value?.ToObject<object>();
                    }
                }
                catch
                {
                    // keep defaults only
                }
            }

            return JsonConvert.SerializeObject(map);
        }

        public static string BuildDefaultActionCode(string spName)
        {
            var bare = (spName ?? "Proc").Trim();
            var idx = bare.LastIndexOf('.');
            if (idx >= 0 && idx < bare.Length - 1) bare = bare.Substring(idx + 1);
            bare = Regex.Replace(bare, @"[^0-9a-zA-Z]+", "_");
            if (string.IsNullOrWhiteSpace(bare)) bare = "Proc";
            return "AppSp_" + bare;
        }

        /// <summary>
        /// Generator Usage column: DB comment + Execute/params signature (not API ActionDescription).
        /// </summary>
        public static string BuildSpUsage(
            string fullName,
            string dbComment,
            IEnumerable<SpApiParameterItem> parameters)
        {
            var signature = SynthesizeSpSignature(fullName, parameters);
            if (!string.IsNullOrWhiteSpace(dbComment))
            {
                var comment = dbComment.Trim();
                if (!comment.EndsWith(".", StringComparison.Ordinal)) comment += ".";
                return $"{comment} {signature}";
            }
            return signature;
        }

        /// <summary>
        /// LLM English API description for agent discovery. Requires AIConfigSettingBL.IsConfigured().
        /// Result truncated to 500 chars. Returns empty string when the model output is unusable.
        /// </summary>
        public static string GenerateApiDescriptionWithLlm(
            string fullName,
            string dbComment,
            string definition,
            IEnumerable<SpApiParameterItem> parameters,
            IList<string> returnColumns = null)
        {
            if (!AIConfigSettingBL.IsConfigured())
                throw new InvalidOperationException("AI is not configured (missing API key for default provider).");

            var paramList = (parameters ?? Enumerable.Empty<SpApiParameterItem>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                .OrderBy(p => p.Ordinal)
                .ToList();
            var paramLines = paramList
                .Select(p => $"{p.Name} ({p.Type ?? "unknown"}, {p.Direction ?? "IN"})")
                .ToList();
            var cols = returnColumns != null
                ? string.Join(", ", returnColumns.Where(c => !string.IsNullOrWhiteSpace(c)))
                : "";

            // Keep definition short — long templates cause label-echo junk from small models.
            var defClip = definition ?? "";
            if (defClip.Length > 2500) defClip = defClip.Substring(0, 2500) + "\n...[truncated]";

            var userPrompt = new StringBuilder();
            userPrompt.AppendLine($"Stored procedure: {fullName}");
            if (!string.IsNullOrWhiteSpace(dbComment))
                userPrompt.AppendLine($"Database comment: {dbComment.Trim()}");
            userPrompt.AppendLine("Parameters:");
            userPrompt.AppendLine(paramLines.Count > 0 ? string.Join("\n", paramLines) : "(none)");
            if (!string.IsNullOrWhiteSpace(cols))
                userPrompt.AppendLine($"Known result columns: {cols}");
            userPrompt.AppendLine("Procedure body excerpt (for context only; do not copy headings or labels from it):");
            userPrompt.AppendLine(string.IsNullOrWhiteSpace(defClip) ? "(unavailable)" : defClip);
            userPrompt.AppendLine();
            userPrompt.AppendLine("Good example (GetTab with @tabid, @referenceIds, @clientTimeZone): Returns tab data for the given tab id and reference ids; accepts client time zone.");
            userPrompt.AppendLine("Bad example (over-claim): Retrieves tab details and layout information adjusted for the client time zone.");
            userPrompt.AppendLine("Bad example (junk): Business Purpose: ** Name");
            userPrompt.AppendLine("Write one plain-English sentence describing what this API does. No labels, no markdown.");

            const string systemPrompt =
                "You write short English API descriptions for a low-code platform so an AI agent can find the right API by natural language. " +
                "Be conservative and factual. Prefer the procedure name and parameter names; use the body only when it clearly confirms behavior. " +
                "Get/List/Select procedures usually only return data — say that. " +
                "Do NOT invent business meaning (layout, adjusted, transformed, enriched, calculated, workflow) unless the definition or DB comment explicitly says so. " +
                "Parameter names are inputs to pass, not proof of side effects. " +
                "Reply with ONLY the description sentence itself (1 sentence preferred, 2 max, under 500 characters). " +
                "Do not mention instructions, rules, markdown, labels, or formatting. " +
                "Do not start with Here is / Check against / Plain text.";

            var request = new LLMRequestDto
            {
                Provider = LLMProviderHelper.GetConfiguredProvider(),
                ApiKey = LLMProviderHelper.GetConfiguredApiKey(),
                Model = AIConfigSettingBL.GetModel(),
                SystemPrompt = systemPrompt,
                Prompt = userPrompt.ToString(),
                Temperature = 0.1,
                MaxTokens = 280,
            };

            var response = LLMProviderHelper.CallLLMAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
            if (response == null || !response.IsSuccess)
                throw new InvalidOperationException(response?.Error ?? "LLM call failed.");

            var text = SanitizeLlmApiDescription(response.Content);
            if (!IsUsableApiDescription(text))
                return string.Empty;
            return TruncateDescription(text, 500);
        }

        /// <summary>Editor / API: generate ActionDescription via LLM for one SP.</summary>
        public static OperationCallResult<object> GenerateApiDescription(SpApiGenerateDescriptionRequest request)
        {
            var result = new OperationCallResult<object>();
            var validation = new ValidationResult();
            result.ValidationResult = validation;

            if (request == null || request.DataSourceId <= 0 || string.IsNullOrWhiteSpace(request.SpName))
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_GenerateDesc_Invalid", ValidationItemType.Error, "DataSourceId and SpName are required."));
                return result;
            }

            if (!AIConfigSettingBL.IsConfigured())
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_GenerateDesc_NoAi", ValidationItemType.Error, "AI is not configured."));
                return result;
            }

            try
            {
                var fixture = AppCacheManagerBL.GetOneDatabaseFixture(request.DataSourceId);
                var engine = fixture.SqlServerType ?? EmSqlType.SqlServer;
                StoredProcedureCatalogBL.ParseName(request.SpName, request.Schema, out var sch, out var name, request.DataSourceId);
                var fullName = StoredProcedureCatalogBL.FormatFull(sch, name);
                var definition = StoredProcedureCatalogBL.TryLoadDefinition(fixture, engine, sch, name);
                var dbComment = StoredProcedureCatalogBL.LoadDescription(fixture, engine, sch, name);
                var parameters = (request.Parameters != null && request.Parameters.Count > 0)
                    ? request.Parameters.Select(NormalizeParam).ToList()
                    : StoredProcedureCatalogBL.LoadParameters(fixture, engine, sch, name).Select(ToApiParamWithDefault).ToList();

                var text = GenerateApiDescriptionWithLlm(fullName, dbComment, definition, parameters, null);
                if (!IsUsableApiDescription(text))
                    text = BuildFallbackApiDescription(fullName, dbComment, parameters);
                result.Object = new { description = text, fullName, dbComment };
            }
            catch (Exception ex)
            {
                validation.Items.Add(new ValidationItem(typeof(AppIntergrationSettingParameterEntity),
                    "SpApi_GenerateDesc_Failed", ValidationItemType.Error, ex.Message));
            }

            return result;
        }

        private static string SynthesizeSpSignature(string fullName, IEnumerable<SpApiParameterItem> parameters)
        {
            var proc = string.IsNullOrWhiteSpace(fullName) ? "stored procedure" : fullName.Trim();
            var list = (parameters ?? Enumerable.Empty<SpApiParameterItem>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                .OrderBy(p => p.Ordinal)
                .ToList();

            var inputs = list.Where(p => !IsOutputOnly(p.Direction)).ToList();
            var outputs = list.Where(p => IsOutputOnly(p.Direction)).ToList();

            var parts = new List<string>();
            if (inputs.Count == 0)
                parts.Add($"Execute {proc} (no input parameters)");
            else
                parts.Add($"Execute {proc} — params: {string.Join(", ", inputs.Select(FormatParamBrief))}");

            if (outputs.Count > 0)
                parts.Add($"OUTPUT: {string.Join(", ", outputs.Select(FormatParamBrief))}");

            var text = string.Join(". ", parts);
            if (!text.EndsWith(".", StringComparison.Ordinal)) text += ".";
            return text;
        }

        private static string FormatParamBrief(SpApiParameterItem p)
        {
            var name = p.Name.Trim();
            var type = string.IsNullOrWhiteSpace(p.Type) ? "unknown" : p.Type.Trim();
            return $"{name} ({type})";
        }

        private static string TruncateDescription(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text ?? string.Empty;
            if (maxLen <= 3) return text.Substring(0, maxLen);
            return text.Substring(0, maxLen - 3).TrimEnd() + "...";
        }

        /// <summary>Strip markdown / label junk from LLM API descriptions.</summary>
        private static string SanitizeLlmApiDescription(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var text = raw.Trim().Trim('"', '\'', '`');
            // Drop fenced blocks / leading bullets
            text = Regex.Replace(text, @"```[\s\S]*?```", " ");
            text = Regex.Replace(text, @"^\s*[-*•]\s+", "", RegexOptions.Multiline);
            // Remove markdown emphasis / headings
            text = Regex.Replace(text, @"[*_#`]+", " ");
            // Remove common template labels the model echoes from SP header comments
            text = Regex.Replace(text,
                @"\b(Business\s*Purpose|Purpose|Description|Name|Parameters?|Returns?|Usage|Summary)\s*:\s*",
                " ",
                RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+", " ").Trim();
            // Strip leading punctuation leftovers from broken markdown (e.g. "/Business Purpose:**")
            text = Regex.Replace(text, @"^[\s/\\|<>\-–—*]+", "").Trim();
            return text;
        }

        /// <summary>Reject empty, tiny, label-only, or prompt-echo LLM leftovers.</summary>
        private static bool IsUsableApiDescription(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim();
            if (t.Length < 24) return false;
            if (Regex.IsMatch(t, @"[*_#`]{2,}")) return false;
            // Mostly punctuation / leftovers
            var letters = t.Count(char.IsLetter);
            if (letters < 16) return false;
            // Still just a label fragment
            if (Regex.IsMatch(t, @"^(Business\s*Purpose|Purpose|Name|Description)\b", RegexOptions.IgnoreCase)
                && t.Length < 60)
                return false;
            // Model echoed instruction meta instead of a description
            if (Regex.IsMatch(t,
                    @"\b(hard\s*rules?|plain\s*text|check\s+against|no\s+markdown|section\s+labels?|output\s+only)\b",
                    RegexOptions.IgnoreCase))
                return false;
            if (t.IndexOf('?') >= 0 && t.Length < 80) return false;
            return true;
        }

        /// <summary>DB comment when useful; otherwise a short SP + params sentence (≤500).</summary>
        public static string BuildFallbackApiDescription(
            string fullName,
            string dbComment,
            IEnumerable<SpApiParameterItem> parameters)
        {
            var comment = SanitizeLlmApiDescription(dbComment ?? "");
            if (IsUsableApiDescription(comment))
                return TruncateDescription(comment, 500);

            var proc = string.IsNullOrWhiteSpace(fullName) ? "stored procedure" : fullName.Trim();
            var inputs = (parameters ?? Enumerable.Empty<SpApiParameterItem>())
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name) && !IsOutputOnly(p.Direction))
                .OrderBy(p => p.Ordinal)
                .Select(FormatParamBrief)
                .ToList();

            string text;
            if (inputs.Count == 0)
                text = $"Runs {proc} and returns its result set.";
            else
                text = $"Runs {proc}. Key inputs: {string.Join(", ", inputs)}.";
            return TruncateDescription(text, 500);
        }

        private static bool IsSampleOk(string sampleJson)
        {
            if (string.IsNullOrWhiteSpace(sampleJson)) return false;
            try
            {
                var jo = JObject.Parse(sampleJson);
                return jo["ok"]?.Value<bool>() == true || jo["IsSuccess"]?.Value<bool>() == true;
            }
            catch
            {
                return false;
            }
        }

        private static IList<string> TryExtractReturnColumns(string sampleJson)
        {
            if (!IsSampleOk(sampleJson)) return null;
            try
            {
                var jo = JObject.Parse(sampleJson);
                var arr = jo["columnNames"] as JArray ?? jo["ColumnNames"] as JArray;
                if (arr == null || arr.Count == 0) return null;
                return arr.Select(t => t?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return null;
            }
        }

        public static string PlaceholderDefault(string type, bool hasDefault)
        {
            // When SP declares a default, leave null so execute can omit and let the engine apply it.
            if (hasDefault) return null;

            var t = (type ?? "").ToLowerInvariant();
            if (t.Contains("int") || t.Contains("decimal") || t.Contains("numeric") || t.Contains("float")
                || t.Contains("real") || t.Contains("money") || t.Contains("number") || t.Contains("double"))
                return "0";
            if (t.Contains("bit") || t.Contains("bool"))
                return "false";
            if (t.Contains("date") || t.Contains("time"))
                return "";
            if (t.Contains("char") || t.Contains("text") || t.Contains("xml") || t.Contains("json") || t.Contains("clob") || t.Contains("uuid") || t.Contains("uniqueidentifier"))
                return "";
            return null;
        }

        private static SpApiParameterItem ToApiParamWithDefault(StoredProcedureParameterDto p)
        {
            var item = new SpApiParameterItem
            {
                Name = p.Name,
                Type = p.Type,
                Direction = p.Direction,
                MaxLength = p.MaxLength,
                Ordinal = p.Ordinal,
                HasDefault = p.HasDefault,
            };
            item.DefaultValue = PlaceholderDefault(p.Type, p.HasDefault);
            return item;
        }

        private static SpApiParameterItem NormalizeParam(SpApiParameterItem p)
        {
            if (p == null) return new SpApiParameterItem();
            if (p.DefaultValue == null && !p.HasDefault)
                p.DefaultValue = PlaceholderDefault(p.Type, false);
            return p;
        }

        /// <summary>
        /// True when body has no permanent-table mutations. Temp-table / table-variable ops are allowed.
        /// Missing definition → false (caller may still sample via <see cref="IsReaderProcedureName"/>).
        /// </summary>
        public static bool IsLikelyReadOnlyProcedure(string definition)
        {
            if (string.IsNullOrWhiteSpace(definition)) return false;

            var text = StripSqlNoiseForScan(definition);
            text = StripTempTableOps(text);
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Permanent-table data-changing / DDL / nested CALL — any hit → unsafe.
            if (MutatingSqlToken.IsMatch(text)) return false;

            // Dynamic SQL EXEC(...) / nested EXEC proc (not "EXECUTE AS caller/owner").
            // sp_executesql is allowed (common for parameterized SELECT).
            if (ExecDynamicOrNested.IsMatch(text)) return false;

            // SELECT … INTO permanent table (temp INTO already stripped).
            if (SelectIntoTable.IsMatch(text)) return false;

            return true;
        }

        /// <summary>Get/List/Select… names — try sample even if body heuristic is unsure.</summary>
        public static bool IsReaderProcedureName(string spName)
        {
            if (string.IsNullOrWhiteSpace(spName)) return false;
            var bare = spName.Trim();
            var dot = bare.LastIndexOf('.');
            if (dot >= 0 && dot < bare.Length - 1) bare = bare.Substring(dot + 1);
            bare = Regex.Replace(bare, @"^usp_?", "", RegexOptions.IgnoreCase);
            return Regex.IsMatch(
                bare,
                @"^(Get|List|Select|Fetch|Load|Read|Query|Retrieve|Find|Search|Lookup)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static readonly Regex MutatingSqlToken = new Regex(
            @"\b(INSERT|UPDATE|DELETE|MERGE|TRUNCATE|ALTER|DROP|CREATE|CALL|BULK|OPENROWSET|OPENDATASOURCE|WRITETEXT|UPDATETEXT)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // EXEC(...) / EXECUTE(...) / EXEC otherProc — exclude EXECUTE AS (impersonation).
        private static readonly Regex ExecDynamicOrNested = new Regex(
            @"\bEXEC(?:UTE)?\s*(?:\(|(?!AS\b)\S)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex SelectIntoTable = new Regex(
            @"\bINTO\s+(?!@)[\w#\[]",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static string StripSqlNoiseForScan(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return sql;

            // Block comments
            var s = Regex.Replace(sql, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            // Line comments
            s = Regex.Replace(s, @"--.*?$", " ", RegexOptions.Multiline);
            // Quoted strings (N'...', '...', "...")
            s = Regex.Replace(s, @"N?'([^']|'')*'", " ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"""([^""]|"""")*""", " ");

            // Outer CREATE PROCEDURE header (always present; not a body mutation).
            s = Regex.Replace(
                s,
                @"\bCREATE\s+(OR\s+REPLACE\s+)?(DEFINER\s*=\s*\S+\s+)?(PROC|PROCEDURE)\b[\s\S]*?(\bAS\b|\))",
                " ",
                RegexOptions.IgnoreCase);

            return s;
        }

        /// <summary>Remove #temp / @table-variable write patterns so Get* procs are not treated as mutating.</summary>
        private static string StripTempTableOps(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return sql;
            // #temp or [#temp]
            const string tempIdent = @"(?:#\w+|\[#\w+\])";
            var s = sql;
            s = Regex.Replace(s, $@"\b(?:CREATE|DROP)\s+TABLE\s+(?:IF\s+EXISTS\s+)?{tempIdent}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, $@"\bINSERT\s+(?:INTO\s+)?{tempIdent}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, $@"\bUPDATE\s+{tempIdent}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, $@"\bDELETE\s+(?:FROM\s+)?{tempIdent}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, $@"\bINTO\s+{tempIdent}", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            // Table variables
            s = Regex.Replace(s, @"\bINSERT\s+(?:INTO\s+)?@\w+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, @"\bUPDATE\s+@\w+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, @"\bDELETE\s+(?:FROM\s+)?@\w+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            s = Regex.Replace(s, @"\bINTO\s+@\w+", " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return s;
        }

        private static string CaptureSampleJson(int dataSourceId, string schema, string spName, List<SpApiParameterItem> parameters)
        {
            var args = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in parameters ?? new List<SpApiParameterItem>())
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Name)) continue;
                if (IsOutputOnly(p.Direction) && p.DefaultValue == null) continue;
                if (p.HasDefault && p.DefaultValue == null) continue;
                args[p.Name.TrimStart('@', ':')] = ParseDefaultToken(p.DefaultValue);
            }

            return StoredProcedureExecuteBL.ExecuteJson(
                dataSourceId,
                spName,
                schema,
                JsonConvert.SerializeObject(args));
        }

        private static bool IsOutputOnly(string direction)
        {
            var d = (direction ?? "IN").ToUpperInvariant();
            return d == "OUT" || d == "OUTPUT";
        }

        private static object ParseDefaultToken(string value)
        {
            if (value == null) return null;
            if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (long.TryParse(value, out var l)) return l;
            if (decimal.TryParse(value, out var d)) return d;
            return value;
        }
    }
}
