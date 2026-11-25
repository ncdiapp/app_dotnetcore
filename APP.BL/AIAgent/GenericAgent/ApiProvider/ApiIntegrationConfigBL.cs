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
            SetIf(patch, "actionDescription", "ActionDescription", v => target.ActionDescription = v);
            SetIf(patch, "ActionDescription", "ActionDescription", v => target.ActionDescription = v);
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
                if (cfgObj["baseUrl"] != null || cfgObj["BaseUrl"] != null)
                    cfg.BaseUrl = (cfgObj["baseUrl"] ?? cfgObj["BaseUrl"])?.ToString();
                if (cfgObj["url"] != null || cfgObj["Url"] != null)
                    cfg.Url = (cfgObj["url"] ?? cfgObj["Url"])?.ToString();
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
            }

            target.IsModified = true;
        }

        public static void ApplyProviderPatch(AppIntergrationSettingExDto target, JObject patch)
        {
            if (target == null || patch == null) return;
            SetIf(patch, "name", "Name", v => target.Name = v);
            SetIf(patch, "Name", "Name", v => target.Name = v);
            SetIf(patch, "internalCode", "InternalCode", v => target.InternalCode = v);
            SetIf(patch, "InternalCode", "InternalCode", v => target.InternalCode = v);
            SetIf(patch, "description", "Description", v => target.Description = v);
            SetIf(patch, "restApiUrl", "RestApiurl", v => target.RestApiurl = v);
            SetIf(patch, "RestApiurl", "RestApiurl", v => target.RestApiurl = v);

            // Write-only credential blob — only set when caller supplies non-empty (never clear via "***")
            var cred = patch["apicredentialConfig"] ?? patch["ApicredentialConfig"] ?? patch["credentialConfig"];
            if (cred != null && cred.Type != JTokenType.Null)
            {
                var s = cred.Type == JTokenType.String ? cred.ToString() : cred.ToString(Formatting.None);
                if (!string.IsNullOrWhiteSpace(s) && s != "***")
                    target.ApicredentialConfig = s;
            }

            if (TryGetInt(patch, out var dsReg, "dataSourceRegisterId", "DataSourceRegisterId"))
                target.DataSourceRegisterId = dsReg;

            var env = patch["environmentVariables"] as JObject ?? patch["DictEnvironmentVariable"] as JObject;
            if (env != null)
            {
                target.OtherSettingsDto ??= new AppIntergrationOtherSettingsDto();
                target.OtherSettingsDto.DictEnvironmentVariable ??= new Dictionary<string, string>();
                MergeStringDict(target.OtherSettingsDto.DictEnvironmentVariable, env);
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

        private static bool TryGetInt(JObject patch, out int value, params string[] keys)
        {
            value = 0;
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
