using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityConverter;
using APP.Components.EntityDto;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using APP.LBL.HelperClasses;
using ExchangeBL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace App.BL.AIAgent.GenericAgent.ApiProvider
{
    /// <summary>
    /// Search / detail for published API Management operations
    /// (App API Provider Id=1 + all 3rd-party providers).
    /// </summary>
    public static class ApiProviderCatalogBL
    {
        private const int MaxSampleSearchChars = 1200;
        private const int MaxSampleDetailChars = 8000;
        private const int MaxSchemaDetailChars = 4000;

        public static string SearchJson(string query, string providerName = null, int take = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "query is required."
                    }, Formatting.Indented);
                }

                take = Math.Clamp(take <= 0 ? 30 : take, 1, 100);
                var tokens = Tokenize(query);
                var ops = LoadApiOperations(providerName);
                var scored = new List<(ApiOpIndex Op, int Score)>();

                foreach (var op in ops)
                {
                    var score = Score(op, tokens, query);
                    if (score > 0)
                        scored.Add((op, score));
                }

                var hits = scored
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Op.ActionCode, StringComparer.OrdinalIgnoreCase)
                    .Take(take)
                    .Select(x => new
                    {
                        actionCode = x.Op.ActionCode,
                        description = Truncate(x.Op.ActionDescription, 500),
                        providerId = x.Op.ProviderId,
                        providerName = x.Op.ProviderName,
                        providerKind = x.Op.ProviderKind,
                        httpMethod = x.Op.HttpMethod,
                        apiType = x.Op.ApiType,
                        inputHint = Truncate(x.Op.InputHint, 240),
                        outputHint = Truncate(x.Op.OutputHint, 240),
                        score = x.Score,
                    })
                    .ToList();

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    query,
                    providerFilter = string.IsNullOrWhiteSpace(providerName) ? null : providerName.Trim(),
                    totalHits = scored.Count,
                    take,
                    apis = hits,
                    next = "Call api-provider-detail with actionCode, then api-provider-execute with bodyJson. For writes, user confirmation is required."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message }, Formatting.Indented);
            }
        }

        public static string DetailJson(string actionCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(actionCode))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = "actionCode is required."
                    }, Formatting.Indented);
                }

                var dto = DataExchangeSettingBL.GetSetting(actionCode.Trim());
                if (dto == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"API operation '{actionCode}' not found."
                    }, Formatting.Indented);
                }

                if (!IsApiOperation(dto.MappingInternalCode))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"'{actionCode}' is not an ApiOperation (MappingInternalCode={dto.MappingInternalCode})."
                    }, Formatting.Indented);
                }

                var providerId = dto.IntergrationSettingId ?? 0;
                var providerName = ResolveProviderName(providerId, dto.ProviderName);
                var apiType = ResolveApiType(dto);
                var httpMethod = ResolveHttpMethod(dto);
                var cfg = SanitizeConfig(dto.APIConfigParameters);
                var inputParams = BuildInputParams(dto);
                var sample = Truncate(dto.JsonSampleData, MaxSampleDetailChars);
                var schema = Truncate(dto.JsonSchema, MaxSchemaDetailChars);
                var isWrite = ApiProviderExecuteBL.IsWriteOperation(dto);

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    actionCode = dto.ActionCode,
                    description = dto.ActionDescription,
                    providerId,
                    providerName,
                    providerKind = providerId == AppIntergrationSettingBL.AppBuiltInProviderId ? "app" : "thirdParty",
                    httpMethod,
                    apiType,
                    apiUrl = $"/webapi/DataIntegration/{dto.ActionCode}",
                    dataSourceId = dto.DataSourceId,
                    transactionId = dto.TranscationId,
                    isSimpleQuery = dto.IsSimpleQuery,
                    jsonQuery = apiType == "StoredProcedure" || apiType == "SimpleQuery" ? dto.JsonQuery : null,
                    inputParameters = inputParams,
                    apiConfig = cfg,
                    sampleResponse = string.IsNullOrWhiteSpace(sample) ? null : sample,
                    jsonSchema = string.IsNullOrWhiteSpace(schema) ? null : schema,
                    requiresUserConfirm = isWrite,
                    executeHint = isWrite
                        ? "Write/mutating API: call ask_user (or pass confirmed=true after user approval) before api-provider-execute."
                        : "Read API: call api-provider-execute with bodyJson (JSON object / args).",
                    next = "Build bodyJson from inputParameters + user intent, then api-provider-execute."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message }, Formatting.Indented);
            }
        }

        private static List<ApiOpIndex> LoadApiOperations(string providerNameFilter)
        {
            var providerNames = new Dictionary<int, string>();
            var providers = AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(isIncludeAppBuiltInApi: true)
                            ?? new List<AppIntergrationSettingExDto>();
            foreach (var p in providers)
            {
                if (p?.Id == null) continue;
                providerNames[(int)p.Id] = p.Name ?? $"Provider {p.Id}";
            }
            if (!providerNames.ContainsKey(AppIntergrationSettingBL.AppBuiltInProviderId))
                providerNames[AppIntergrationSettingBL.AppBuiltInProviderId] = AppIntergrationSettingBL.AppBuiltInProviderName;

            string providerFilter = string.IsNullOrWhiteSpace(providerNameFilter) ? null : providerNameFilter.Trim();

            var result = new List<ApiOpIndex>();
            using (var adapter = AppTenantAdapterBL.GetTenantAdapter())
            {
                var list = new EntityCollection<AppIntergrationSettingParameterEntity>();
                adapter.FetchEntityCollection(list, null);

                foreach (var e in list)
                {
                    if (!IsApiOperation(e.MappingInternalCode)) continue;
                    if (string.IsNullOrWhiteSpace(e.ActionCode)) continue;

                    var providerId = e.IntergrationSettingId ?? 0;
                    if (providerId <= 0) continue;
                    providerNames.TryGetValue(providerId, out var pname);
                    pname ??= providerId == AppIntergrationSettingBL.AppBuiltInProviderId
                        ? AppIntergrationSettingBL.AppBuiltInProviderName
                        : $"Provider {providerId}";

                    if (providerFilter != null
                        && pname.IndexOf(providerFilter, StringComparison.OrdinalIgnoreCase) < 0
                        && !string.Equals(providerId.ToString(), providerFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var dto = AppIntergrationSettingParameterConverter.ConvertEntityToExDto(e);
                    var apiType = ResolveApiType(dto);
                    var inputHint = BuildInputHint(dto);
                    var outputHint = BuildOutputHint(dto.JsonSampleData, dto.JsonSchema);
                    var searchBlob = BuildSearchBlob(dto, pname, inputHint, outputHint);

                    result.Add(new ApiOpIndex
                    {
                        ActionCode = e.ActionCode,
                        ActionDescription = e.ActionDescription ?? "",
                        ProviderId = providerId,
                        ProviderName = pname,
                        ProviderKind = providerId == AppIntergrationSettingBL.AppBuiltInProviderId ? "app" : "thirdParty",
                        HttpMethod = ResolveHttpMethod(dto),
                        ApiType = apiType,
                        InputHint = inputHint,
                        OutputHint = outputHint,
                        SearchBlob = searchBlob,
                    });
                }
            }

            return result;
        }

        private static bool IsApiOperation(string mappingInternalCode)
        {
            if (string.IsNullOrWhiteSpace(mappingInternalCode)) return true;
            return string.Equals(
                mappingInternalCode,
                EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveProviderName(int providerId, string fromDto)
        {
            if (!string.IsNullOrWhiteSpace(fromDto)) return fromDto.Trim();
            if (providerId == AppIntergrationSettingBL.AppBuiltInProviderId)
                return AppIntergrationSettingBL.AppBuiltInProviderName;
            try
            {
                var p = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(providerId);
                if (!string.IsNullOrWhiteSpace(p?.Name)) return p.Name;
            }
            catch { /* ignore */ }
            return $"Provider {providerId}";
        }

        private static string ResolveApiType(AppIntergrationSettingParameterExDto dto)
        {
            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto)) return "StoredProcedure";
            if (dto?.IsSimpleQuery == true) return "SimpleQuery";
            if (dto?.TranscationId.HasValue == true) return "Transaction";
            if ((dto?.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
                return "AppApi";
            return "ThirdParty";
        }

        private static string ResolveHttpMethod(AppIntergrationSettingParameterExDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto?.HttpMethd)) return dto.HttpMethd.Trim();
            if (dto?.APIConfigParameters != null)
                return dto.APIConfigParameters.Method.ToString();
            return "Post";
        }

        private static string BuildInputHint(AppIntergrationSettingParameterExDto dto)
        {
            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto)
                && dto.APIConfigParameters?.SpParameters != null
                && dto.APIConfigParameters.SpParameters.Count > 0)
            {
                return string.Join(", ",
                    dto.APIConfigParameters.SpParameters
                        .Where(p => p != null && !string.IsNullOrWhiteSpace(p.Name))
                        .OrderBy(p => p.Ordinal)
                        .Select(p => $"{p.Name}({p.Type ?? "?"})"));
            }

            var cfg = dto?.APIConfigParameters;
            if (cfg != null)
            {
                var parts = new List<string>();
                if (cfg.QueryParams != null && cfg.QueryParams.Count > 0)
                    parts.Add("query:" + string.Join(",", cfg.QueryParams.Keys));
                if (cfg.PathParams != null && cfg.PathParams.Count > 0)
                    parts.Add("path:" + string.Join(",", cfg.PathParams.Keys));
                if (parts.Count > 0) return string.Join("; ", parts);
            }

            if (!string.IsNullOrWhiteSpace(dto?.WhereClauseFormat))
                return Truncate(dto.WhereClauseFormat, 200);

            return ExtractJsonKeys(dto?.JsonSampleData, 12);
        }

        private static string BuildOutputHint(string sample, string schema)
        {
            var fromSample = ExtractJsonKeys(sample, 16);
            if (!string.IsNullOrWhiteSpace(fromSample)) return fromSample;
            if (!string.IsNullOrWhiteSpace(schema))
                return Truncate(schema.Replace("\r", " ").Replace("\n", " "), 200);
            return "";
        }

        private static string BuildSearchBlob(
            AppIntergrationSettingParameterExDto dto,
            string providerName,
            string inputHint,
            string outputHint)
        {
            var sb = new StringBuilder();
            sb.Append(dto.ActionCode).Append(' ');
            sb.Append(dto.ActionDescription).Append(' ');
            sb.Append(providerName).Append(' ');
            sb.Append(inputHint).Append(' ');
            sb.Append(outputHint).Append(' ');
            sb.Append(dto.JsonQuery).Append(' ');
            if (dto.APIConfigParameters != null)
            {
                sb.Append(dto.APIConfigParameters.SpName).Append(' ');
                sb.Append(dto.APIConfigParameters.Url).Append(' ');
                if (dto.APIConfigParameters.SpParameters != null)
                {
                    foreach (var p in dto.APIConfigParameters.SpParameters)
                        if (p?.Name != null) sb.Append(p.Name).Append(' ');
                }
            }
            if (!string.IsNullOrWhiteSpace(dto.JsonSampleData))
                sb.Append(Truncate(dto.JsonSampleData, MaxSampleSearchChars));
            return sb.ToString();
        }

        private static int Score(ApiOpIndex op, List<string> tokens, string rawQuery)
        {
            if (tokens.Count == 0) return 0;
            var code = op.ActionCode ?? "";
            var desc = op.ActionDescription ?? "";
            var blob = op.SearchBlob ?? "";
            var score = 0;

            if (code.IndexOf(rawQuery.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                score += 80;
            if (string.Equals(code, rawQuery.Trim(), StringComparison.OrdinalIgnoreCase))
                score += 120;

            foreach (var t in tokens)
            {
                if (code.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 40;
                if (desc.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 25;
                if ((op.ProviderName ?? "").IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 20;
                if ((op.InputHint ?? "").IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 15;
                if ((op.OutputHint ?? "").IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
                if (blob.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) score += 5;
            }

            return score;
        }

        private static List<object> BuildInputParams(AppIntergrationSettingParameterExDto dto)
        {
            var list = new List<object>();
            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto)
                && dto.APIConfigParameters?.SpParameters != null)
            {
                foreach (var p in dto.APIConfigParameters.SpParameters
                             .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                             .OrderBy(x => x.Ordinal))
                {
                    list.Add(new
                    {
                        name = p.Name,
                        type = p.Type,
                        direction = p.Direction,
                        hasDefault = p.HasDefault,
                        defaultValue = p.DefaultValue,
                    });
                }
                return list;
            }

            var cfg = dto?.APIConfigParameters;
            if (cfg?.QueryParams != null)
            {
                foreach (var kv in cfg.QueryParams)
                    list.Add(new { name = kv.Key, type = "query", direction = "IN", defaultValue = kv.Value });
            }
            if (cfg?.PathParams != null)
            {
                foreach (var kv in cfg.PathParams)
                    list.Add(new { name = kv.Key, type = "path", direction = "IN", defaultValue = kv.Value });
            }

            if (list.Count == 0 && !string.IsNullOrWhiteSpace(dto?.WhereClauseFormat))
                list.Add(new { name = "(whereClauseFormat)", type = "hint", direction = "IN", defaultValue = dto.WhereClauseFormat });

            return list;
        }

        private static object SanitizeConfig(APIConfigParameterDTO cfg)
        {
            if (cfg == null) return null;
            return new
            {
                method = cfg.Method.ToString(),
                baseUrl = cfg.BaseUrl,
                url = cfg.Url,
                isStoredProcedureApi = cfg.IsStoredProcedureApi,
                spName = cfg.SpName,
                spSchema = cfg.SpSchema,
                spEngine = cfg.SpEngine,
                spParameters = cfg.SpParameters,
                queryParams = cfg.QueryParams,
                pathParams = cfg.PathParams,
                // Credentials / auth headers intentionally omitted
                authenticationType = cfg.AuthenticationType.ToString(),
                hasHeaders = cfg.Headers != null && cfg.Headers.Count > 0,
            };
        }

        private static string ExtractJsonKeys(string json, int maxKeys)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";
            try
            {
                var token = JToken.Parse(json);
                var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                CollectKeys(token, keys, 0);
                return string.Join(", ", keys.Take(maxKeys));
            }
            catch
            {
                return "";
            }
        }

        private static void CollectKeys(JToken token, HashSet<string> keys, int depth)
        {
            if (token == null || depth > 4 || keys.Count >= 40) return;
            if (token is JObject jo)
            {
                foreach (var p in jo.Properties())
                {
                    keys.Add(p.Name);
                    if (p.Name.Equals("columnNames", StringComparison.OrdinalIgnoreCase)
                        && p.Value is JArray arr)
                    {
                        foreach (var c in arr.Take(20))
                            if (c.Type == JTokenType.String) keys.Add(c.ToString());
                    }
                    else
                        CollectKeys(p.Value, keys, depth + 1);
                    if (keys.Count >= 40) return;
                }
            }
            else if (token is JArray ja && ja.Count > 0)
            {
                CollectKeys(ja[0], keys, depth + 1);
            }
        }

        private static List<string> Tokenize(string query)
        {
            return Regex.Split(query ?? "", @"[^0-9a-zA-Z_]+")
                .Where(t => t.Length >= 2)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? "";
            return text.Substring(0, max) + "…";
        }

        private sealed class ApiOpIndex
        {
            public string ActionCode { get; set; }
            public string ActionDescription { get; set; }
            public int ProviderId { get; set; }
            public string ProviderName { get; set; }
            public string ProviderKind { get; set; }
            public string HttpMethod { get; set; }
            public string ApiType { get; set; }
            public string InputHint { get; set; }
            public string OutputHint { get; set; }
            public string SearchBlob { get; set; }
        }
    }
}
