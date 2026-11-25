using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Validation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.ApiProvider
{
    /// <summary>
    /// Helpers for API Management config agent tools (summaries, redaction, merge, HITL gate).
    /// Persistence stays in AppIntergrationSettingBL / AppStoredProcedureApiBL / AppOpenApiDocumentBL.
    /// </summary>
    public static class ApiIntegrationConfigBL
    {
        private static readonly Regex SecretKeyRx = new Regex(
            @"(password|secret|token|apikey|api_key|authorization|clientsecret|client_secret|accesskey|access_key|bearer)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Ok(object payload)
        {
            var jo = payload == null ? new JObject() : JObject.FromObject(payload);
            jo["ok"] = true;
            jo["IsSuccess"] = true;
            return jo.ToString(Formatting.Indented);
        }

        public static string Fail(string error) =>
            JsonConvert.SerializeObject(new { IsSuccess = false, ok = false, Error = error, error });

        public static string Preview(object proposed, string message = null) =>
            Ok(new
            {
                preview = true,
                message = message ?? "Preview only. ask_user then confirmed=true.",
                proposed
            });

        public static bool NeedsConfirm(bool? confirmed, bool? dryRun) =>
            dryRun == true || confirmed != true;

        public static string ValidationMessages(ValidationResult vr)
        {
            if (vr?.Items == null || vr.Items.Count == 0) return "Save failed.";
            var msgs = vr.Items
                .Where(i => i.ItemType == ValidationItemType.Error)
                .Select(i =>
                {
                    var loc = i.LocalizedMessage;
                    if (!string.IsNullOrWhiteSpace(loc) && loc.IndexOf("{0}", StringComparison.Ordinal) < 0)
                        return string.IsNullOrWhiteSpace(i.PropertyName) ? loc : $"{i.PropertyName}: {loc}";
                    if (!string.IsNullOrWhiteSpace(i.PropertyName))
                        return $"{i.PropertyName} is empty";
                    return !string.IsNullOrWhiteSpace(i.Message) ? i.Message : i.Key;
                })
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .ToList();
            return msgs.Count > 0 ? string.Join("; ", msgs) : "Save failed.";
        }

        public static object SummarizeProvider(AppIntergrationSettingExDto p, bool includeApis)
        {
            if (p == null) return null;
            var id = Convert.ToInt32(p.Id);
            var apis = (p.AppIntergrationSettingParameterList ?? Enumerable.Empty<AppIntergrationSettingParameterExDto>())
                .Where(a => a != null)
                .Select(SummarizeApiBrief)
                .ToList();

            return new
            {
                providerId = id,
                name = p.Name,
                internalCode = p.InternalCode,
                description = p.Description,
                restApiUrl = p.RestApiurl,
                dataSourceRegisterId = p.DataSourceRegisterId,
                integrationType = p.IntergrationType,
                providerKind = id == AppIntergrationSettingBL.AppBuiltInProviderId ? "App" : "ThirdParty",
                hasCredentialConfig = !string.IsNullOrWhiteSpace(p.ApicredentialConfig),
                credentialConfigRedacted = RedactJsonString(p.ApicredentialConfig),
                environmentKeys = p.OtherSettingsDto?.DictEnvironmentVariable?.Keys?.ToList(),
                apiCount = apis.Count,
                apis = includeApis ? apis : null
            };
        }

        public static object SummarizeApiBrief(AppIntergrationSettingParameterExDto a)
        {
            if (a == null) return null;
            return new
            {
                settingParameterId = Convert.ToInt32(a.Id),
                actionCode = a.ActionCode,
                description = Truncate(a.ActionDescription, 240),
                httpMethod = a.HttpMethd,
                apiType = a.APIType ?? ResolveApiType(a),
                providerId = a.IntergrationSettingId,
                isSimpleQuery = a.IsSimpleQuery,
                transactionId = a.TranscationId,
                searchOrViewFieldId = a.TranscationFieId,
                dataSourceId = a.DataSourceId
            };
        }

        public static object SummarizeApiDetail(AppIntergrationSettingParameterExDto a)
        {
            if (a == null) return null;
            var cfg = a.APIConfigParameters;
            return new
            {
                settingParameterId = Convert.ToInt32(a.Id),
                actionCode = a.ActionCode,
                description = a.ActionDescription,
                httpMethod = a.HttpMethd,
                apiType = a.APIType ?? ResolveApiType(a),
                providerId = a.IntergrationSettingId,
                mappingInternalCode = a.MappingInternalCode,
                isSimpleQuery = a.IsSimpleQuery,
                transactionId = a.TranscationId,
                searchOrViewFieldId = a.TranscationFieId,
                dataSourceId = a.DataSourceId,
                jsonQuery = Truncate(a.JsonQuery, 4000),
                whereClauseFormat = a.WhereClauseFormat,
                jsonSampleData = Truncate(a.JsonSampleData, 4000),
                jsonSchema = Truncate(a.JsonSchema, 4000),
                config = SanitizeApiConfig(cfg),
                publicUrl = string.IsNullOrWhiteSpace(a.ActionCode)
                    ? null
                    : "/webapi/DataIntegration/" + a.ActionCode.Trim()
            };
        }

        public static object SanitizeApiConfig(APIConfigParameterDTO cfg)
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
                excelDataImportDataSetId = cfg.ExcelDataImportDataSetId,
                authenticationType = cfg.AuthenticationType.ToString(),
                hasHeaders = cfg.Headers != null && cfg.Headers.Count > 0,
                headerKeys = cfg.Headers?.Keys?.Where(k => !IsSecretKey(k)).ToList(),
                hasAuthParameters = cfg.AuthenticationParameters != null && cfg.AuthenticationParameters.Count > 0
            };
        }

        public static string RedactJsonString(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var token = JToken.Parse(json);
                RedactToken(token);
                return token.ToString(Formatting.None);
            }
            catch
            {
                return "***";
            }
        }

        public static void RedactToken(JToken token)
        {
            if (token is JObject jo)
            {
                foreach (var p in jo.Properties().ToList())
                {
                    if (IsSecretKey(p.Name))
                        p.Value = "***";
                    else
                        RedactToken(p.Value);
                }
            }
            else if (token is JArray ja)
            {
                foreach (var c in ja)
                    RedactToken(c);
            }
        }

        public static bool IsSecretKey(string name) =>
            !string.IsNullOrWhiteSpace(name) && SecretKeyRx.IsMatch(name);

        public static string ResolveApiType(AppIntergrationSettingParameterExDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto?.APIType)) return dto.APIType;
            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto)) return "Stored Procedure API";
            if (dto?.IsSimpleQuery == true) return "SQL JSON Query API";
            if (dto?.TranscationId.HasValue == true) return "APP Data Model API";
            if (dto?.TranscationFieId.HasValue == true) return "APP Data Presentation API";
            if (dto?.APIConfigParameters?.ExcelDataImportDataSetId.HasValue == true)
                return "Excel Import Update Data API";
            if ((dto?.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
                return "Standard API";
            return "Standard API";
        }

        public static AppIntergrationSettingParameterExDto ResolveApi(
            int? settingParameterId,
            string actionCode,
            out string error)
        {
            error = null;
            if (settingParameterId.HasValue && settingParameterId.Value > 0)
            {
                var dto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(
                    settingParameterId.Value, true);
                if (dto == null || Convert.ToInt32(dto.Id) <= 0)
                {
                    error = $"API settingParameterId={settingParameterId} not found.";
                    return null;
                }
                return dto;
            }

            if (!string.IsNullOrWhiteSpace(actionCode))
            {
                var dto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDtoByActionCode(
                    actionCode.Trim());
                if (dto == null)
                {
                    error = $"API actionCode '{actionCode}' not found.";
                    return null;
                }
                return dto;
            }

            error = "Provide settingParameterId or actionCode.";
            return null;
        }

        /// <summary>
        /// Merge flat JSON onto an existing or new parameter DTO. Secrets in AuthenticationParameters/Headers are write-only merges.
        /// </summary>
        public static void ApplyApiPatch(AppIntergrationSettingParameterExDto target, JObject patch)
        {
            if (target == null || patch == null) return;

            SetIf(patch, "actionCode", "ActionCode", v => target.ActionCode = v);
            SetIf(patch, "ActionCode", "ActionCode", v => target.ActionCode = v);
            // UI column ActionDescription — agents often send "description" (not actionDescription)
            SetIf(patch, "actionDescription", "ActionDescription", v => target.ActionDescription = v);
            SetIf(patch, "ActionDescription", "ActionDescription", v => target.ActionDescription = v);
            SetIf(patch, "description", "ActionDescription", v => target.ActionDescription = v);
            SetIf(patch, "Description", "ActionDescription", v => target.ActionDescription = v);
            SetIf(patch, "httpMethod", "HttpMethd", v => target.HttpMethd = v);
            SetIf(patch, "HttpMethd", "HttpMethd", v => target.HttpMethd = v);
            SetIf(patch, "jsonQuery", "JsonQuery", v => target.JsonQuery = v);
            SetIf(patch, "JsonQuery", "JsonQuery", v => target.JsonQuery = v);
            SetIf(patch, "whereClauseFormat", "WhereClauseFormat", v => target.WhereClauseFormat = v);
            SetIf(patch, "jsonSampleData", "JsonSampleData", v => target.JsonSampleData = v);
            SetIf(patch, "jsonSchema", "JsonSchema", v => target.JsonSchema = v);
            SetIf(patch, "mappingInternalCode", "MappingInternalCode", v => target.MappingInternalCode = v);

            if (TryGetBool(patch, out var isSimple, "isSimpleQuery", "IsSimpleQuery"))
                target.IsSimpleQuery = isSimple;
            if (TryGetInt(patch, out var dsId, "dataSourceId", "DataSourceId"))
                target.DataSourceId = dsId;
            if (TryGetInt(patch, out var txId, "transactionId", "TranscationId"))
                target.TranscationId = txId;
            if (TryGetInt(patch, out var viewFieldId, "transcationFieId", "TranscationFieId", "searchOrViewFieldId"))
                target.TranscationFieId = viewFieldId;
            if (TryGetInt(patch, out var providerId, "providerId", "intergrationSettingId", "IntergrationSettingId"))
                target.IntergrationSettingId = providerId;

            var cfgToken = patch["apiConfig"] ?? patch["APIConfigParameters"] ?? patch["config"];
            if (cfgToken is JObject cfgObj)
            {
                var cfg = target.APIConfigParameters ?? new APIConfigParameterDTO();

                // Single absolute apiUrl (UI form field) → BaseUrl + Url, same as apiConfigForm.splitApiUrl
                var apiUrl = (cfgObj["apiUrl"] ?? cfgObj["ApiUrl"] ?? patch["apiUrl"])?.ToString();
                if (!string.IsNullOrWhiteSpace(apiUrl) && apiUrl.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    SplitAbsoluteApiUrl(apiUrl.Trim(), cfg);
                else
                {
                    if (cfgObj["baseUrl"] != null || cfgObj["BaseUrl"] != null)
                        cfg.BaseUrl = (cfgObj["baseUrl"] ?? cfgObj["BaseUrl"])?.ToString();
                    if (cfgObj["url"] != null || cfgObj["Url"] != null)
                        cfg.Url = (cfgObj["url"] ?? cfgObj["Url"])?.ToString();
                }

                if (cfgObj["method"] != null || cfgObj["Method"] != null)
                {
                    var m = (cfgObj["method"] ?? cfgObj["Method"])?.ToString();
                    if (Enum.TryParse(m, true, out EnumHttpMethod method))
                        cfg.Method = method;
                }
                if (TryGetInt(cfgObj, out var excelDs, "excelDataImportDataSetId", "ExcelDataImportDataSetId"))
                    cfg.ExcelDataImportDataSetId = excelDs;
                if (cfgObj["isStoredProcedureApi"] != null)
                    cfg.IsStoredProcedureApi = cfgObj["isStoredProcedureApi"].Value<bool>();
                if (cfgObj["spName"] != null) cfg.SpName = cfgObj["spName"].ToString();
                if (cfgObj["spSchema"] != null) cfg.SpSchema = cfgObj["spSchema"].ToString();

                MergeStringDict(cfg.Headers ??= new Dictionary<string, string>(), cfgObj["headers"] as JObject);
                MergeStringDict(cfg.QueryParams ??= new Dictionary<string, string>(), cfgObj["queryParams"] as JObject);
                MergeStringDict(cfg.PathParams ??= new Dictionary<string, string>(), cfgObj["pathParams"] as JObject);
                MergeStringDict(
                    cfg.AuthenticationParameters ??= new Dictionary<string, string>(),
                    cfgObj["authenticationParameters"] as JObject);

                target.APIConfigParameters = cfg;
                if (cfgObj["method"] != null || cfgObj["Method"] != null)
                    target.HttpMethd = cfg.Method.ToString();
            }
            else if (patch["apiUrl"] != null && patch["apiUrl"].Type != JTokenType.Null)
            {
                var apiUrl = patch["apiUrl"].ToString();
                if (!string.IsNullOrWhiteSpace(apiUrl))
                {
                    var cfg = target.APIConfigParameters ?? new APIConfigParameterDTO();
                    SplitAbsoluteApiUrl(apiUrl.Trim(), cfg);
                    target.APIConfigParameters = cfg;
                }
            }

            target.IsModified = true;
        }

        /// <summary>
        /// After patch: inherit provider RestApiurl as BaseUrl, sync HttpMethd↔Method,
        /// flush typed config into ApiconfigParameters JSON (UI/save contract).
        /// </summary>
        public static void FinalizeHttpApiConfig(AppIntergrationSettingParameterExDto target)
        {
            if (target == null) return;
            var cfg = target.APIConfigParameters;
            if (cfg == null) return;

            if (string.IsNullOrWhiteSpace(cfg.BaseUrl)
                && target.IntergrationSettingId.HasValue
                && target.IntergrationSettingId.Value > 0
                && target.IntergrationSettingId.Value != AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                try
                {
                    var provider = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingExDto(
                        target.IntergrationSettingId.Value);
                    if (!string.IsNullOrWhiteSpace(provider?.RestApiurl))
                        cfg.BaseUrl = provider.RestApiurl.Trim().TrimEnd('/');
                }
                catch { /* ignore inherit failures */ }
            }

            // Absolute Url with empty BaseUrl → split
            if (string.IsNullOrWhiteSpace(cfg.BaseUrl)
                && !string.IsNullOrWhiteSpace(cfg.Url)
                && cfg.Url.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                SplitAbsoluteApiUrl(cfg.Url.Trim(), cfg);
            }

            cfg.Headers ??= new Dictionary<string, string>();
            if (!cfg.Headers.ContainsKey("CurrentUserSessionId"))
                cfg.Headers["CurrentUserSessionId"] = null;

            if (!string.IsNullOrWhiteSpace(target.HttpMethd)
                && Enum.TryParse(target.HttpMethd, true, out EnumHttpMethod fromCol))
                cfg.Method = fromCol;
            else if (cfg.Method != default)
                target.HttpMethd = cfg.Method.ToString();

            target.APIConfigParameters = cfg;
            // Mirror UI: persist JSON string; converter also serializes when object non-null.
            try
            {
                target.ApiconfigParameters = JsonConvert.SerializeObject(cfg, Formatting.Indented);
            }
            catch { /* leave existing */ }
        }

        /// <summary>
        /// 3rd-party HTTP ops must have absolute call URL + HttpMethd (same bar as UI save).
        /// </summary>
        public static string ValidateThirdPartyHttpConfig(AppIntergrationSettingParameterExDto target)
        {
            if (target == null) return "API dto is null.";
            var pid = target.IntergrationSettingId ?? 0;
            if (pid <= 0 || pid == AppIntergrationSettingBL.AppBuiltInProviderId)
                return null;

            var cfg = target.APIConfigParameters;
            if (cfg == null)
                return "apiConfig is required for 3rd-party APIs. Include apiUrl (https://…) or baseUrl+url, method, and queryParams/headers as needed.";

            var joined = JoinApiUrl(cfg.BaseUrl, cfg.Url);
            if (string.IsNullOrWhiteSpace(joined)
                || !joined.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return "3rd-party API needs an absolute http(s) URL. Set apiConfig.apiUrl (full URL) or apiConfig.baseUrl + apiConfig.url. "
                    + "Example: {\"apiUrl\":\"https://api.weatherapi.com/v1/forecast.json\",\"method\":\"Get\",\"queryParams\":{\"q\":\"{{QueryParams.q}}\",\"key\":\"{{Env.ApiKey}}\"}}";
            }

            if (string.IsNullOrWhiteSpace(target.HttpMethd))
                return "httpMethod is required (Get/Post/Put/Delete). Do not leave blank — weather/read APIs are usually Get.";

            return null;
        }

        public static string JoinApiUrl(string baseUrl, string url)
        {
            var b = (baseUrl ?? "").Trim().TrimEnd('/');
            var u = (url ?? "").Trim().TrimStart('/');
            if (string.IsNullOrEmpty(b)) return u;
            if (string.IsNullOrEmpty(u)) return b;
            return b + "/" + u;
        }

        public static void SplitAbsoluteApiUrl(string apiUrl, APIConfigParameterDTO cfg)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(apiUrl)) return;
            var text = apiUrl.Trim();
            // https://host[:port]/rest
            var m = System.Text.RegularExpressions.Regex.Match(
                text, @"^(https?://[^/\s?#]+)/?(.*)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                cfg.Url = text;
                return;
            }
            cfg.BaseUrl = m.Groups[1].Value;
            var rest = m.Groups[2].Value ?? "";
            var q = rest.IndexOf('?');
            if (q >= 0)
            {
                cfg.Url = rest.Substring(0, q).TrimStart('/');
                var query = rest.Substring(q + 1);
                cfg.QueryParams ??= new Dictionary<string, string>();
                foreach (var part in query.Split('&'))
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    var eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = Uri.UnescapeDataString(part.Substring(0, eq));
                    var val = Uri.UnescapeDataString(part.Substring(eq + 1));
                    if (!string.IsNullOrEmpty(key))
                        cfg.QueryParams[key] = val;
                }
            }
            else
            {
                cfg.Url = rest.TrimStart('/');
            }
        }

        /// <summary>
        /// Find 3rd-party providers by InternalCode (preferred) or Name.
        /// Excludes App built-in id=1. Used for idempotent provider upsert.
        /// </summary>
        public static List<AppIntergrationSettingExDto> FindThirdPartyProviders(
            string internalCode,
            string name)
        {
            var all = AppIntergrationSettingBL.RetrieveAllAppIntergrationSettingDto(isIncludeAppBuiltInApi: false)
                ?? new List<AppIntergrationSettingExDto>();
            IEnumerable<AppIntergrationSettingExDto> q = all.Where(p =>
                p != null && Convert.ToInt32(p.Id) != AppIntergrationSettingBL.AppBuiltInProviderId);

            if (!string.IsNullOrWhiteSpace(internalCode))
            {
                var code = internalCode.Trim();
                var byCode = q.Where(p =>
                    string.Equals(p.InternalCode?.Trim(), code, StringComparison.OrdinalIgnoreCase)).ToList();
                if (byCode.Count > 0) return byCode;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var n = name.Trim();
                return q.Where(p =>
                    string.Equals(p.Name?.Trim(), n, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return new List<AppIntergrationSettingExDto>();
        }

        /// <summary>
        /// True when dtoJson looks like an external/3rd-party HTTP op (must not default to provider id=1).
        /// </summary>
        public static bool LooksLikeThirdPartyHttpOp(JObject patch)
        {
            if (patch == null) return false;
            var cfg = patch["apiConfig"] as JObject
                ?? patch["APIConfigParameters"] as JObject
                ?? patch["config"] as JObject;
            if (cfg == null) return false;
            var url = (cfg["url"] ?? cfg["Url"])?.ToString();
            var baseUrl = (cfg["baseUrl"] ?? cfg["BaseUrl"])?.ToString();
            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(baseUrl))
                return false;
            // App-typed fields → treat as App API even if apiConfig present
            if (TryGetInt(patch, out _, "transactionId", "TranscationId")) return false;
            if (TryGetInt(patch, out _, "transcationFieId", "TranscationFieId", "searchOrViewFieldId")) return false;
            if (TryGetBool(patch, out var simple, "isSimpleQuery", "IsSimpleQuery") && simple) return false;
            if (cfg["isStoredProcedureApi"]?.Value<bool>() == true) return false;
            if (TryGetInt(cfg, out _, "excelDataImportDataSetId", "ExcelDataImportDataSetId")) return false;
            return true;
        }

        /// <summary>
        /// True when patch clearly targets App Provider create types (safe to bind provider id=1).
        /// </summary>
        public static bool LooksLikeAppProviderOp(JObject patch)
        {
            if (patch == null) return false;
            if (TryGetInt(patch, out _, "transactionId", "TranscationId")) return true;
            if (TryGetInt(patch, out _, "transcationFieId", "TranscationFieId", "searchOrViewFieldId")) return true;
            if (TryGetBool(patch, out var simple, "isSimpleQuery", "IsSimpleQuery") && simple) return true;
            var cfg = patch["apiConfig"] as JObject
                ?? patch["APIConfigParameters"] as JObject
                ?? patch["config"] as JObject;
            if (cfg?["isStoredProcedureApi"]?.Value<bool>() == true) return true;
            if (cfg != null && TryGetInt(cfg, out _, "excelDataImportDataSetId", "ExcelDataImportDataSetId"))
                return true;
            return false;
        }

        public static void ApplyProviderPatch(AppIntergrationSettingExDto target, JObject patch)
        {
            if (target == null || patch == null) return;

            // Column APICredentialConfig persists OtherSettingsDto JSON (converter OnCopyDtoToEntityDone).
            // Always initialize so create never serializes null → JSON "null" in DB.
            target.OtherSettingsDto ??= new AppIntergrationOtherSettingsDto();

            SetIf(patch, "name", "Name", v => target.Name = v);
            SetIf(patch, "Name", "Name", v => target.Name = v);
            SetIf(patch, "internalCode", "InternalCode", v => target.InternalCode = v);
            SetIf(patch, "InternalCode", "InternalCode", v => target.InternalCode = v);
            SetIf(patch, "description", "Description", v => target.Description = v);
            SetIf(patch, "restApiUrl", "RestApiurl", v => target.RestApiurl = v);
            SetIf(patch, "RestApiurl", "RestApiurl", v => target.RestApiurl = v);

            if (TryGetInt(patch, out var dsReg, "dataSourceRegisterId", "DataSourceRegisterId"))
                target.DataSourceRegisterId = dsReg;

            var env = patch["environmentVariables"] as JObject ?? patch["DictEnvironmentVariable"] as JObject;
            if (env != null)
            {
                target.OtherSettingsDto.DictEnvironmentVariable ??= new Dictionary<string, string>();
                MergeStringDict(target.OtherSettingsDto.DictEnvironmentVariable, env);
            }

            // Nested otherSettings / apicredentialConfig object → merge into OtherSettingsDto
            var other = patch["otherSettings"] as JObject
                ?? patch["OtherSettingsDto"] as JObject
                ?? patch["apicredentialConfig"] as JObject
                ?? patch["ApicredentialConfig"] as JObject;
            if (other != null)
            {
                if (other["databaseTablePrefix"] != null || other["DatabaseTablePrefix"] != null)
                    target.OtherSettingsDto.DatabaseTablePrefix =
                        (other["databaseTablePrefix"] ?? other["DatabaseTablePrefix"])?.ToString();
                var envNested = other["dictEnvironmentVariable"] as JObject
                    ?? other["DictEnvironmentVariable"] as JObject
                    ?? other["environmentVariables"] as JObject;
                if (envNested != null)
                {
                    target.OtherSettingsDto.DictEnvironmentVariable ??= new Dictionary<string, string>();
                    MergeStringDict(target.OtherSettingsDto.DictEnvironmentVariable, envNested);
                }
                var cookies = other["dictCookieSetting"] as JObject ?? other["DictCookieSetting"] as JObject;
                if (cookies != null)
                {
                    target.OtherSettingsDto.DictCookieSetting ??= new Dictionary<string, string>();
                    MergeStringDict(target.OtherSettingsDto.DictCookieSetting, cookies);
                }
            }

            target.IsModified = true;
        }

        public static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max) return text;
            return text.Substring(0, max) + "…";
        }

        private static void SetIf(JObject patch, string key1, string key2, Action<string> set)
        {
            var t = patch[key1] ?? (key1 != key2 ? patch[key2] : null);
            if (t == null || t.Type == JTokenType.Null) return;
            set(t.ToString());
        }

        public static bool TryGetInt(JObject patch, out int value, params string[] keys)
        {
            value = 0;
            if (patch == null || keys == null) return false;
            foreach (var k in keys)
            {
                var t = patch[k];
                if (t == null || t.Type == JTokenType.Null) continue;
                if (t.Type == JTokenType.Integer)
                {
                    value = t.Value<int>();
                    return true;
                }
                if (int.TryParse(t.ToString(), out value))
                    return true;
            }
            return false;
        }

        private static bool TryGetBool(JObject patch, out bool value, params string[] keys)
        {
            value = false;
            foreach (var k in keys)
            {
                var t = patch[k];
                if (t == null || t.Type == JTokenType.Null) continue;
                if (t.Type == JTokenType.Boolean)
                {
                    value = t.Value<bool>();
                    return true;
                }
                if (bool.TryParse(t.ToString(), out value))
                    return true;
            }
            return false;
        }

        private static void MergeStringDict(Dictionary<string, string> target, JObject source)
        {
            if (target == null || source == null) return;
            foreach (var p in source.Properties())
            {
                if (p.Value == null || p.Value.Type == JTokenType.Null) continue;
                var v = p.Value.ToString();
                if (v == "***") continue; // do not overwrite secret with redacted placeholder
                target[p.Name] = v;
            }
        }
    }
}
