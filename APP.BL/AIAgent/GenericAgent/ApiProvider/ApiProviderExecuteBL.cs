using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.BL;
using App.BL.AIAgent.GenericAgent.Plugins;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using ExchangeBL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.ApiProvider
{
    /// <summary>
    /// Execute published API Management operations via DataIntegration path.
    /// Write/mutating ops require user confirmation (ask_user or confirmed=true).
    /// </summary>
    public static class ApiProviderExecuteBL
    {
        private const int MaxResponseChars = 24000;
        private const int MaxPayloadPreviewChars = 1500;

        public static async Task<string> ExecuteAsync(
            string actionCode,
            string bodyJson,
            bool confirmed,
            AgentToolContext context)
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

                var code = actionCode.Trim();
                var dto = DataExchangeSettingBL.GetSetting(code);
                if (dto == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"API operation '{code}' not found."
                    }, Formatting.Indented);
                }

                var mapping = dto.MappingInternalCode;
                if (!string.IsNullOrWhiteSpace(mapping)
                    && !string.Equals(mapping, EmAppIntergrationSettingParameterUsageType.ApiOperation.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        error = $"'{code}' is not an ApiOperation."
                    }, Formatting.Indented);
                }

                var payload = NormalizeBody(bodyJson);
                var isWrite = IsWriteOperation(dto);

                if (isWrite && !confirmed)
                {
                    var gate = await ConfirmWriteAsync(code, dto, payload, context).ConfigureAwait(false);
                    if (gate != null) return gate;
                }

                var httpMethod = (dto.HttpMethd ?? dto.APIConfigParameters?.Method.ToString() ?? "Post").Trim();
                string responseText;

                if (httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
                    || httpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    var query = BodyToQueryParams(payload);
                    var result = await DataExchangeWithoutJsonSchemaBL.GetAsync(code, query).ConfigureAwait(false);
                    responseText = NormalizeResponseObject(result);
                }
                else
                {
                    responseText = DataExchangeWithoutJsonSchemaBL.ExecuteApiOperationSaveCommand(
                        code, payload, null);
                }

                var truncated = responseText ?? "";
                var wasTruncated = truncated.Length > MaxResponseChars;
                if (wasTruncated)
                    truncated = truncated.Substring(0, MaxResponseChars) + "…";

                object responseJson = null;
                try { responseJson = string.IsNullOrWhiteSpace(truncated) ? null : JToken.Parse(truncated); }
                catch { /* keep as string */ }

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    actionCode = code,
                    httpMethod,
                    isWrite,
                    confirmed = isWrite ? true : (bool?)null,
                    truncated = wasTruncated,
                    response = responseJson ?? truncated,
                    next = "Present the response to the user. Use data_render if tabular."
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = ex.Message
                }, Formatting.Indented);
            }
        }

        /// <summary>
        /// Write = transaction APIs, non-reader stored procedures, or non-GET third-party/App HTTP.
        /// App SP APIs use POST even for Get* — reader names are treated as read.
        /// </summary>
        public static bool IsWriteOperation(AppIntergrationSettingParameterExDto dto)
        {
            if (dto == null) return true;
            if (dto.TranscationId.HasValue) return true;

            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto))
            {
                var spName = dto.APIConfigParameters?.SpName;
                if (string.IsNullOrWhiteSpace(spName))
                    spName = dto.JsonQuery;
                if (AppStoredProcedureApiBL.IsReaderProcedureName(spName))
                    return false;
                return true;
            }

            if (dto.IsSimpleQuery == true) return false;

            var method = (dto.HttpMethd ?? dto.APIConfigParameters?.Method.ToString() ?? "Post").Trim();
            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static async Task<string> ConfirmWriteAsync(
            string actionCode,
            AppIntergrationSettingParameterExDto dto,
            string payload,
            AgentToolContext context)
        {
            var preview = payload ?? "{}";
            if (preview.Length > MaxPayloadPreviewChars)
                preview = preview.Substring(0, MaxPayloadPreviewChars) + "…";

            if (context != null && context.IsDeterministic)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    needsConfirmation = true,
                    actionCode,
                    description = dto.ActionDescription,
                    payloadPreview = preview,
                    error = "Write API requires user confirmation. In Deterministic mode pass confirmed=true only after the caller has approved. In Interactive mode omit confirmed and the tool will ask_user."
                }, Formatting.Indented);
            }

            var ask = new AgentAskUserPlugin();
            var prompt =
                $"[api-provider] Confirm write API execute\n" +
                $"API: {actionCode}\n" +
                $"Description: {dto.ActionDescription ?? "(none)"}\n" +
                $"Payload:\n{preview}\n" +
                "Approve execute?";

            var askJson = await ask.AskUser(
                prompt,
                context,
                mode: "single_choice",
                optionsJson: "[{\"id\":\"yes\",\"display\":\"Yes, execute\"},{\"id\":\"no\",\"display\":\"Cancel\"}]",
                ui: "button_group",
                layout: "horizontal").ConfigureAwait(false);

            try
            {
                var jo = JObject.Parse(askJson);
                if (jo["cancelled"]?.Value<bool>() == true || jo["ok"]?.Value<bool>() != true)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        cancelled = true,
                        error = "User cancelled API execute."
                    }, Formatting.Indented);
                }

                var selected = jo["selectedIds"] as JArray;
                var ids = selected?.Select(x => x?.ToString() ?? "").ToList() ?? new List<string>();
                if (!ids.Any(id => string.Equals(id, "yes", StringComparison.OrdinalIgnoreCase)))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        ok = false,
                        cancelled = true,
                        error = "User did not approve API execute."
                    }, Formatting.Indented);
                }
            }
            catch
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    needsConfirmation = true,
                    error = "Could not parse confirmation response. Retry with confirmed=true after user approval.",
                    askResult = askJson
                }, Formatting.Indented);
            }

            return null; // approved
        }

        private static string NormalizeBody(string bodyJson)
        {
            if (string.IsNullOrWhiteSpace(bodyJson)) return "{}";
            var trimmed = bodyJson.Trim();
            try
            {
                var token = JToken.Parse(trimmed);
                if (token is JObject) return token.ToString(Formatting.None);
                // Allow bare args object wrapped later — arrays not valid as full body for most APIs
                return new JObject { ["value"] = token }.ToString(Formatting.None);
            }
            catch
            {
                // Treat as raw string payload
                return JsonConvert.SerializeObject(trimmed);
            }
        }

        private static List<KeyValuePair<string, string>> BodyToQueryParams(string payload)
        {
            var list = new List<KeyValuePair<string, string>>();
            try
            {
                var jo = JObject.Parse(payload ?? "{}");
                var args = jo["args"] as JObject ?? jo;
                foreach (var p in args.Properties())
                    list.Add(new KeyValuePair<string, string>(p.Name, p.Value?.ToString() ?? ""));
            }
            catch { /* empty */ }
            return list;
        }

        private static string NormalizeResponseObject(object result)
        {
            if (result == null) return "";
            if (result is string s) return s;
            if (result is MemoryStream ms)
            {
                ms.Position = 0;
                using var reader = new StreamReader(ms, leaveOpen: true);
                return reader.ReadToEnd();
            }
            if (result is Stream stream)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            return result.ToString() ?? "";
        }
    }
}
