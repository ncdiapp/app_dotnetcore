using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace App.BL
{
    /// <summary>
    /// Builds one OpenAPI 3.0.0 document from selected API operations.
    /// servers[0].url is left empty; the reader fills it from the current request.
    /// </summary>
    public static class AppOpenApiDocumentGenerator
    {
        public class Skip
        {
            public string ActionCode { get; set; }
            public string Reason { get; set; }
        }

        public class Result
        {
            public string Json { get; set; }
            public int ApiCount { get; set; }
            public List<Skip> Skipped { get; set; } = new List<Skip>();
        }

        public static Result Generate(string title, string version, string description, IList<AppIntergrationSettingParameterExDto> members)
        {
            var skipped = new List<Skip>();
            var paths = new JObject();
            var schemas = new JObject();
            var seenPathMethod = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenOperationId = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dto in members ?? new List<AppIntergrationSettingParameterExDto>())
            {
                var code = dto?.ActionCode?.Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    skipped.Add(new Skip { ActionCode = "", Reason = "ActionCode is empty." });
                    continue;
                }

                if (IsExcelImport(dto))
                {
                    skipped.Add(new Skip { ActionCode = code, Reason = "Excel Import is not included." });
                    continue;
                }

                try
                {
                    var method = NormalizeMethod(dto);
                    var path = "/DataIntegration/" + code;
                    var key = path + " " + method;
                    if (!seenPathMethod.Add(key))
                    {
                        skipped.Add(new Skip { ActionCode = code, Reason = "The same path and method is already in this document." });
                        continue;
                    }

                    var operationId = code;
                    if (!seenOperationId.Add(operationId))
                    {
                        operationId = method + "_" + code;
                        seenOperationId.Add(operationId);
                    }

                    JObject requestSchema;
                    JObject responseSchema;
                    JArray parameters;
                    BuildSchemas(dto, method, out parameters, out requestSchema, out responseSchema);

                    var requestName = code + "_Request";
                    var responseName = code + "_Response";
                    if (requestSchema != null)
                        schemas[requestName] = requestSchema;
                    schemas[responseName] = responseSchema ?? GenericObject();

                    var operation = new JObject
                    {
                        ["operationId"] = operationId,
                        ["summary"] = code,
                        ["description"] = dto.ActionDescription ?? "",
                        ["tags"] = Tags(dto),
                        ["responses"] = new JObject
                        {
                            ["200"] = new JObject
                            {
                                ["description"] = "OK",
                                ["content"] = new JObject
                                {
                                    ["application/json"] = new JObject
                                    {
                                        ["schema"] = new JObject { ["$ref"] = "#/components/schemas/" + responseName }
                                    }
                                }
                            },
                            ["400"] = new JObject { ["description"] = "Bad Request" },
                            ["500"] = new JObject { ["description"] = "Internal Server Error" }
                        },
                        ["security"] = new JArray
                        {
                            new JObject { ["CurrentUserSessionId"] = new JArray() }
                        }
                    };

                    if (parameters != null && parameters.Count > 0)
                        operation["parameters"] = parameters;

                    if (requestSchema != null && (method == "post" || method == "put"))
                    {
                        operation["requestBody"] = new JObject
                        {
                            ["required"] = true,
                            ["content"] = new JObject
                            {
                                ["application/json"] = new JObject
                                {
                                    ["schema"] = new JObject { ["$ref"] = "#/components/schemas/" + requestName }
                                }
                            }
                        };
                    }

                    if (paths[path] == null)
                        paths[path] = new JObject();
                    ((JObject)paths[path])[method] = operation;
                }
                catch (Exception ex)
                {
                    skipped.Add(new Skip { ActionCode = code, Reason = ex.Message });
                }
            }

            var doc = new JObject
            {
                ["openapi"] = "3.0.0",
                ["info"] = new JObject
                {
                    ["title"] = string.IsNullOrWhiteSpace(title) ? "OpenAPI Document" : title,
                    ["version"] = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version,
                    ["description"] = description ?? ""
                },
                ["servers"] = new JArray
                {
                    new JObject { ["url"] = "" }
                },
                ["paths"] = paths,
                ["components"] = new JObject
                {
                    ["schemas"] = schemas,
                    ["securitySchemes"] = new JObject
                    {
                        ["CurrentUserSessionId"] = new JObject
                        {
                            ["type"] = "apiKey",
                            ["in"] = "header",
                            ["name"] = "CurrentUserSessionId",
                            ["description"] = "Login session. Reading this document does not require this header."
                        }
                    }
                }
            };

            return new Result
            {
                Json = doc.ToString(Newtonsoft.Json.Formatting.Indented),
                ApiCount = paths.Properties().Count(),
                Skipped = skipped
            };
        }

        public static string ApplyServerUrl(string json, string requestRoot)
        {
            if (string.IsNullOrWhiteSpace(json))
                return json;
            var root = (requestRoot ?? "").TrimEnd('/');
            var doc = JObject.Parse(json);
            doc["servers"] = new JArray
            {
                new JObject { ["url"] = root + "/webapi" }
            };
            return doc.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        private static void BuildSchemas(AppIntergrationSettingParameterExDto dto, string method, out JArray parameters, out JObject requestSchema, out JObject responseSchema)
        {
            parameters = new JArray();
            requestSchema = null;
            responseSchema = GenericObject();

            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto))
            {
                requestSchema = StoredProcedureRequest(dto);
                responseSchema = SchemaFromSample(dto.JsonSampleData) ?? GenericObject();
                if (method == "get")
                    parameters = QueryParamsFromObject(requestSchema["properties"] as JObject);
                return;
            }

            if (dto.IsSimpleQuery == true)
            {
                parameters = SimpleQueryParameters(dto);
                if (method == "post" || method == "put")
                {
                    requestSchema = SchemaFromSample(dto.JsonSampleData) ?? GenericObject();
                    responseSchema = new JObject { ["type"] = "string" };
                }
                else
                {
                    responseSchema = GenericObject();
                }
                return;
            }

            if (dto.TranscationFieId.HasValue && (dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                var query = dto.APIConfigParameters?.QueryParams;
                if (query != null)
                {
                    foreach (var key in query.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                            parameters.Add(QueryParameter(key));
                    }
                }
                responseSchema = SearchResponseSchema(dto.TranscationFieId.Value);
                return;
            }

            if (dto.TranscationId.HasValue && (dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                var query = dto.APIConfigParameters?.QueryParams;
                if (query != null && query.Count > 0)
                {
                    foreach (var key in query.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(key))
                            parameters.Add(QueryParameter(key));
                    }
                }
                else if (method == "get")
                {
                    parameters.Add(QueryParameter("id"));
                }

                if (method == "post" || method == "put")
                    requestSchema = SchemaFromSample(dto.JsonSampleData) ?? GenericObject();
                responseSchema = GenericObject();
                return;
            }

            var thirdQuery = dto.APIConfigParameters?.QueryParams;
            if ((method == "get" || method == "delete") && thirdQuery != null)
            {
                foreach (var key in thirdQuery.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        parameters.Add(QueryParameter(key));
                }
            }

            if (method == "post" || method == "put")
            {
                requestSchema = SchemaFromSample(dto.JsonSampleData) ?? GenericObject();
                responseSchema = SchemaFromSample(dto.PostResponseDto?.ResponseJsonData) ?? GenericObject();
            }
            else
            {
                responseSchema = SchemaFromSample(dto.JsonSampleData) ?? GenericObject();
            }
        }

        private static JObject SearchResponseSchema(int searchId)
        {
            var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            var view = search?.DefaultSearchViewExDto;
            var properties = new JObject();
            var tree = false;
            if (view?.AppSearchViewFieldList != null)
            {
                foreach (var viewField in view.AppSearchViewFieldList)
                {
                    if (viewField == null)
                        continue;
                    if (viewField.IsTreeNodeId == true)
                        tree = true;
                    var field = viewField.ForeignAppSearchFieldExDto ?? viewField.ForeignAppSearchField_ExDto;
                    var path = field?.SysTableFiledPath;
                    if (string.IsNullOrWhiteSpace(path) || properties[path] != null)
                        continue;
                    properties[path] = new JObject { ["type"] = "string" };
                }
            }

            var item = new JObject
            {
                ["type"] = "object",
                ["properties"] = properties
            };
            if (!properties.Properties().Any())
                item["additionalProperties"] = true;
            if (tree)
            {
                properties["Children"] = new JObject
                {
                    ["type"] = "array",
                    ["items"] = new JObject { ["type"] = "object", ["additionalProperties"] = true }
                };
            }

            return new JObject { ["type"] = "array", ["items"] = item };
        }

        private static JObject StoredProcedureRequest(AppIntergrationSettingParameterExDto dto)
        {
            var args = new JObject();
            var list = dto.APIConfigParameters?.SpParameters;
            if (list != null)
            {
                foreach (var parameter in list.Where(IsInputParameter).OrderBy(p => p.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(parameter.Name) || args[parameter.Name] != null)
                        continue;
                    args[parameter.Name] = new JObject { ["type"] = "string" };
                }
            }

            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["args"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = args
                    }
                },
                ["required"] = new JArray("args")
            };
        }

        private static bool IsInputParameter(StoredProcedureApiParameterDTO parameter)
        {
            if (parameter == null)
                return false;
            var direction = (parameter.Direction ?? "").Trim();
            if (direction.Equals("OUT", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("OUTPUT", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }

        private static JArray SimpleQueryParameters(AppIntergrationSettingParameterExDto dto)
        {
            var sql = dto.JsonQuery ?? "";
            var names = new List<string>();
            if (dto.SimpleQueryParameterNameList != null)
            {
                foreach (var raw in dto.SimpleQueryParameterNameList)
                {
                    var name = (raw ?? "").Trim().TrimStart('@');
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    if (sql.IndexOf("@" + name, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (!names.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        names.Add(name);
                }
            }

            if (names.Count == 0)
            {
                foreach (Match match in Regex.Matches(sql, @"@([A-Za-z_][A-Za-z0-9_]*)"))
                {
                    var name = match.Groups[1].Value;
                    if (name.Equals("json", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!names.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        names.Add(name);
                }
            }

            var parameters = new JArray();
            foreach (var name in names)
                parameters.Add(QueryParameter(name));
            return parameters;
        }

        private static JArray QueryParamsFromObject(JObject properties)
        {
            var parameters = new JArray();
            if (properties == null)
                return parameters;
            foreach (var property in properties.Properties())
                parameters.Add(QueryParameter(property.Name));
            return parameters;
        }

        private static JObject QueryParameter(string name)
        {
            return new JObject
            {
                ["name"] = name,
                ["in"] = "query",
                ["required"] = false,
                ["schema"] = new JObject { ["type"] = "string" }
            };
        }

        private static JArray Tags(AppIntergrationSettingParameterExDto dto)
        {
            var tags = new JArray();
            var apiType = Classify(dto);
            if (!string.IsNullOrWhiteSpace(apiType))
                tags.Add(apiType);
            if ((dto.IntergrationSettingId ?? 0) != AppIntergrationSettingBL.AppBuiltInProviderId
                && !string.IsNullOrWhiteSpace(dto.ProviderName))
                tags.Add(dto.ProviderName);
            if (tags.Count == 0)
                tags.Add("API");
            return tags;
        }

        public static string Classify(AppIntergrationSettingParameterExDto dto)
        {
            if (dto == null)
                return "";
            if (dto.IsSimpleQuery == true)
                return "SQL JSON Query API";
            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto))
                return AppStoredProcedureApiBL.ApiTypeDisplayName;
            if (dto.TranscationId.HasValue)
                return "APP Data Model API";
            if (dto.TranscationFieId.HasValue)
                return "APP Data Presentation API";
            if (IsExcelImport(dto))
                return "Excel Import Update Data API";
            if ((dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
                return "Standard API";
            return "3rd Party API";
        }

        public static bool IsExcelImport(AppIntergrationSettingParameterExDto dto)
        {
            return dto?.APIConfigParameters != null && dto.APIConfigParameters.ExcelDataImportDataSetId.HasValue;
        }

        private static string NormalizeMethod(AppIntergrationSettingParameterExDto dto)
        {
            var raw = dto.HttpMethd;
            if (string.IsNullOrWhiteSpace(raw) && dto.APIConfigParameters != null)
                raw = dto.APIConfigParameters.Method.ToString();
            raw = (raw ?? "get").Trim().ToLowerInvariant();
            if (raw == "post" || raw == "put" || raw == "delete" || raw == "get")
                return raw;
            return "get";
        }

        private static JObject GenericObject()
        {
            return new JObject
            {
                ["type"] = "object",
                ["additionalProperties"] = true
            };
        }

        private static JObject SchemaFromSample(string sample)
        {
            if (string.IsNullOrWhiteSpace(sample))
                return null;
            try
            {
                var token = JToken.Parse(sample);
                return SchemaFromToken(token, 0);
            }
            catch
            {
                return GenericObject();
            }
        }

        private static JObject SchemaFromToken(JToken token, int depth)
        {
            if (token == null || token.Type == JTokenType.Null || depth > 6)
                return GenericObject();

            if (token.Type == JTokenType.Object)
            {
                var properties = new JObject();
                foreach (var property in ((JObject)token).Properties())
                    properties[property.Name] = SchemaFromToken(property.Value, depth + 1);
                return new JObject
                {
                    ["type"] = "object",
                    ["properties"] = properties
                };
            }

            if (token.Type == JTokenType.Array)
            {
                var first = ((JArray)token).FirstOrDefault();
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = first == null ? GenericObject() : SchemaFromToken(first, depth + 1)
                };
            }

            if (token.Type == JTokenType.Integer)
                return new JObject { ["type"] = "integer" };
            if (token.Type == JTokenType.Float)
                return new JObject { ["type"] = "number" };
            if (token.Type == JTokenType.Boolean)
                return new JObject { ["type"] = "boolean" };
            return new JObject { ["type"] = "string" };
        }
    }
}
