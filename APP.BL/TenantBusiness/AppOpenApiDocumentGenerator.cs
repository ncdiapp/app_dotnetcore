using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.Dto;
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
            var tagOrder = new List<string>();
            var seenTags = new HashSet<string>(StringComparer.Ordinal);
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
                    schemas[responseName] = responseSchema ?? ClosedObject();

                    var tag = TagName(dto);
                    if (seenTags.Add(tag))
                        tagOrder.Add(tag);

                    var operation = new JObject
                    {
                        ["operationId"] = operationId,
                        ["summary"] = code,
                        ["description"] = dto.ActionDescription ?? "",
                        ["tags"] = new JArray(tag),
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
                        var media = new JObject
                        {
                            ["schema"] = new JObject { ["$ref"] = "#/components/schemas/" + requestName }
                        };
                        if (!AppStoredProcedureApiBL.IsStoredProcedureApi(dto))
                        {
                            var example = ParseJsonOrNull(dto.JsonSampleData);
                            if (example != null)
                                media["example"] = example;
                        }
                        operation["requestBody"] = new JObject
                        {
                            ["required"] = true,
                            ["content"] = new JObject
                            {
                                ["application/json"] = media
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
                    ["title"] = string.IsNullOrWhiteSpace(title) ? "OpenAPI Swagger Document" : title,
                    ["version"] = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version,
                    ["description"] = description ?? ""
                },
                ["tags"] = TagDefinitions(tagOrder),
                ["x-tagGroups"] = TagGroups(tagOrder),
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
            responseSchema = ClosedObject();

            if (AppStoredProcedureApiBL.IsStoredProcedureApi(dto))
            {
                requestSchema = StoredProcedureRequest(dto);
                responseSchema = SchemaFromSample(dto.JsonSampleData) ?? StoredProcedureResponseSchema(dto);
                if (method == "get")
                    parameters = QueryParamsFromObject(requestSchema["properties"] as JObject);
                return;
            }

            if (dto.IsSimpleQuery == true)
            {
                parameters = SimpleQueryParameters(dto);
                if (method == "post" || method == "put")
                {
                    requestSchema = SchemaFromSample(dto.JsonSampleData) ?? ClosedObject();
                    responseSchema = new JObject { ["type"] = "string" };
                }
                else
                {
                    var sample = ParseJsonOrNull(dto.JsonSampleData);
                    responseSchema = SchemaFromSample(dto.JsonSampleData) ?? SimpleQueryResponseSchema(dto.JsonQuery);
                    if (sample != null)
                        responseSchema["example"] = sample;
                }
                return;
            }

            if (dto.TranscationFieId.HasValue && (dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                var query = dto.APIConfigParameters?.QueryParams;
                var fieldDefaults = SearchFieldDefaults(dto.TranscationFieId.Value);
                if (query != null)
                {
                    foreach (var key in query.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            continue;
                        var value = query[key];
                        if (string.IsNullOrWhiteSpace(value))
                            fieldDefaults.TryGetValue(key, out value);
                        parameters.Add(QueryParameter(key, value));
                    }
                }
                ReferenceViewDto view = null;
                try
                {
                    view = AppSearchBL.RetrieveOneSearchDto(dto.TranscationFieId.Value, false, false)?.DefaultView;
                }
                catch
                {
                }
                var mcpGet = dto.IsUsedByMcp == true && method == "get";
                responseSchema = SearchResponseSchema(view, mcpGet);
                if (mcpGet)
                {
                    var fields = McpApiResponseBL.SearchFields(view);
                    responseSchema = McpApiResponseBL.EnvelopeSchema(responseSchema);
                    var fieldsSchema = responseSchema["properties"]?["fields"] as JObject;
                    if (fieldsSchema != null)
                        fieldsSchema["example"] = fields;
                    var sample = ParseJsonOrNull(dto.JsonSampleData);
                    if (sample is JObject envelope && envelope["fields"] != null && envelope["data"] != null)
                        responseSchema["example"] = envelope;
                    else if (sample is JArray rows)
                        responseSchema["example"] = new JObject { ["fields"] = fields, ["data"] = rows };
                    else
                        responseSchema["example"] = new JObject { ["fields"] = fields, ["data"] = new JArray() };
                }
                else
                {
                    var sample = ParseJsonOrNull(dto.JsonSampleData);
                    if (sample != null)
                        responseSchema["example"] = sample;
                }
                return;
            }

            if (dto.TranscationId.HasValue && (dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                var query = dto.APIConfigParameters?.QueryParams;
                if (method == "get" || method == "delete")
                {
                    if (query != null && query.Count > 0)
                    {
                        foreach (var key in query.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                        {
                            if (!string.IsNullOrWhiteSpace(key))
                                parameters.Add(QueryParameter(key, query[key]));
                        }
                    }
                    else if (method == "get")
                    {
                        parameters.Add(QueryParameter("id"));
                    }
                }

                AppTransactionExDto transaction = null;
                try
                {
                    transaction = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(dto.TranscationId.Value);
                }
                catch
                {
                }
                var dataSchema = McpApiResponseBL.TransactionDataSchema(transaction);
                if (method == "post" || method == "put")
                    requestSchema = SchemaFromSample(dto.JsonSampleData) ?? dataSchema;
                responseSchema = dataSchema;
                if (method == "post" || method == "put")
                {
                    var savedResponse = SchemaFromSample(dto.PostResponseDto?.ResponseJsonData);
                    if (savedResponse != null)
                        responseSchema = savedResponse;
                }
                if (dto.IsUsedByMcp == true && method == "get")
                {
                    responseSchema = McpApiResponseBL.TransactionEnvelopeSchema(transaction);
                    var sample = ParseJsonOrNull(dto.JsonSampleData) as JObject;
                    if (sample != null && sample["fields"] != null && sample["data"] != null)
                        responseSchema["example"] = sample;
                }
                return;
            }

            var thirdQuery = dto.APIConfigParameters?.QueryParams;
            if ((method == "get" || method == "delete") && thirdQuery != null)
            {
                foreach (var key in thirdQuery.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(key))
                        parameters.Add(QueryParameter(key, thirdQuery[key]));
                }
            }

            if (method == "post" || method == "put")
            {
                requestSchema = SchemaFromSample(dto.JsonSampleData) ?? ClosedObject();
                responseSchema = SchemaFromSample(dto.PostResponseDto?.ResponseJsonData) ?? ClosedObject();
            }
            else
            {
                responseSchema = SchemaFromSample(dto.JsonSampleData) ?? ClosedObject();
            }
        }

        private static JObject SearchResponseSchema(ReferenceViewDto view, bool includeFieldHints = false)
        {
            return new JObject
            {
                ["type"] = "array",
                ["items"] = SearchRowSchema(view, includeFieldHints, true)
            };
        }

        private static JObject SearchRowSchema(ReferenceViewDto view, bool includeFieldHints, bool includeChildren)
        {
            var properties = new JObject();
            if (view?.Columns != null)
            {
                foreach (var column in view.Columns)
                {
                    var path = column?.SysTableFiledPath;
                    if (string.IsNullOrWhiteSpace(path) || properties[path] != null)
                        continue;
                    properties[path] = McpApiResponseBL.PropertySchema(column.ControlType, column.EntityId, includeFieldHints);
                }
            }

            if (view?.ChildReferenceViewDtoList != null)
            {
                foreach (var child in view.ChildReferenceViewDtoList)
                {
                    if (child == null)
                        continue;
                    var name = (child.Display ?? "").Replace(" ", "") + "_" + child.Id;
                    if (string.IsNullOrWhiteSpace(name) || properties[name] != null)
                        continue;
                    properties[name] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = SearchRowSchema(child, includeFieldHints, true)
                    };
                }
            }

            if (view != null && view.ViewType == (int)EmAppViewType.FlatDataSetTreeView)
            {
                if (properties["TreeNodeId"] == null)
                    properties["TreeNodeId"] = new JObject { ["type"] = "string" };
                if (properties["TreeNodeDisplay"] == null)
                    properties["TreeNodeDisplay"] = new JObject { ["type"] = "string" };
                if (includeChildren && properties["Children"] == null)
                {
                    properties["Children"] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = SearchRowSchema(view, includeFieldHints, false)
                    };
                }
            }

            return ClosedObject(properties);
        }

        private static JObject StoredProcedureResponseSchema(AppIntergrationSettingParameterExDto dto)
        {
            var row = ClosedObject();
            var rows = new JObject { ["type"] = "array", ["items"] = row };
            var outputs = new JObject();
            var list = dto.APIConfigParameters?.SpParameters;
            if (list != null)
            {
                foreach (var parameter in list.Where(p => !IsInputParameter(p)))
                {
                    var name = (parameter.Name ?? "").Trim().TrimStart('@', ':');
                    if (string.IsNullOrWhiteSpace(name) || outputs[name] != null)
                        continue;
                    outputs[name] = SpArgumentSchema(parameter);
                }
            }

            if (dto.IsUsedByMcp == true)
            {
                return ClosedObject(new JObject
                {
                    ["ok"] = new JObject { ["type"] = "boolean" },
                    ["IsSuccess"] = new JObject { ["type"] = "boolean" },
                    ["dataSourceId"] = new JObject { ["type"] = "integer", ["nullable"] = true },
                    ["procedure"] = new JObject { ["type"] = "string" },
                    ["columnNames"] = new JObject { ["type"] = "array", ["items"] = new JObject { ["type"] = "string" } },
                    ["rowCount"] = new JObject { ["type"] = "integer" },
                    ["truncated"] = new JObject { ["type"] = "boolean" },
                    ["rows"] = rows,
                    ["outputParameters"] = outputs.Count > 0 ? ClosedObject(outputs) : ClosedObject()
                });
            }

            if (outputs.Count > 0)
            {
                return ClosedObject(new JObject
                {
                    ["rows"] = rows,
                    ["outputParameters"] = ClosedObject(outputs)
                });
            }

            return rows;
        }

        private static JObject SimpleQueryResponseSchema(string sql)
        {
            var properties = new JObject();
            foreach (var name in ForJsonColumnNames(sql))
            {
                if (properties[name] == null)
                    properties[name] = new JObject { ["type"] = "string", ["nullable"] = true };
            }

            var row = ClosedObject(properties);
            if (string.IsNullOrWhiteSpace(sql))
                return row;
            var hasForJson = sql.IndexOf("FOR JSON", StringComparison.OrdinalIgnoreCase) >= 0;
            var withoutWrapper = sql.IndexOf("WITHOUT_ARRAY_WRAPPER", StringComparison.OrdinalIgnoreCase) >= 0;
            if (hasForJson && !withoutWrapper)
                return new JObject { ["type"] = "array", ["items"] = row };
            return row;
        }

        private static List<string> ForJsonColumnNames(string sql)
        {
            var names = new List<string>();
            if (string.IsNullOrWhiteSpace(sql))
                return names;
            var forJson = sql.IndexOf("FOR JSON", StringComparison.OrdinalIgnoreCase);
            if (forJson < 0)
                return names;
            var selectAt = -1;
            var scan = 0;
            while (scan < forJson)
            {
                var found = IndexOfWord(sql, "SELECT", scan);
                if (found < 0 || found >= forJson)
                    break;
                selectAt = found;
                scan = found + 6;
            }
            if (selectAt < 0)
                return names;

            var listStart = selectAt + 6;
            while (listStart < sql.Length && char.IsWhiteSpace(sql[listStart]))
                listStart++;
            if (sql.IndexOf("DISTINCT", listStart, StringComparison.OrdinalIgnoreCase) == listStart)
                listStart += 8;
            while (listStart < sql.Length && char.IsWhiteSpace(sql[listStart]))
                listStart++;
            if (sql.IndexOf("TOP", listStart, StringComparison.OrdinalIgnoreCase) == listStart)
            {
                listStart += 3;
                while (listStart < sql.Length && (char.IsWhiteSpace(sql[listStart]) || char.IsDigit(sql[listStart]) || sql[listStart] == '(' || sql[listStart] == ')'))
                    listStart++;
            }

            var fromAt = IndexOfWord(sql, "FROM", listStart);
            var listEnd = fromAt > listStart && fromAt < forJson ? fromAt : forJson;
            foreach (var piece in SplitTopLevel(sql.Substring(listStart, Math.Max(0, listEnd - listStart)), ','))
            {
                var name = ColumnAlias(piece);
                if (string.IsNullOrWhiteSpace(name) || name == "*" || name.Equals("JSON", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!names.Any(n => n.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    names.Add(name);
            }
            return names;
        }

        private static string ColumnAlias(string expression)
        {
            var text = (expression ?? "").Trim();
            if (text.Length == 0)
                return null;
            var aliasAt = -1;
            var depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '(') depth++;
                else if (ch == ')' && depth > 0) depth--;
                else if (depth == 0 && IsWordAt(text, i, "AS"))
                    aliasAt = i + 2;
            }
            if (aliasAt >= 0)
                text = text.Substring(aliasAt).Trim();
            else
            {
                var parts = SplitTopLevel(text, ' ');
                text = parts.Count == 0 ? text : parts[parts.Count - 1];
            }

            text = text.Trim().Trim('[', ']', '"', '`');
            if (text.Length == 0 || text.IndexOfAny(new[] { ' ', '(', ')', '.', ',' }) >= 0)
                return null;
            return text;
        }

        private static List<string> SplitTopLevel(string text, char separator)
        {
            var parts = new List<string>();
            if (text == null)
                return parts;
            var depth = 0;
            var start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '(') depth++;
                else if (ch == ')' && depth > 0) depth--;
                else if (ch == separator && depth == 0)
                {
                    parts.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }
            parts.Add(text.Substring(start));
            return parts;
        }

        private static int IndexOfWord(string text, string word, int start)
        {
            if (text == null || word == null)
                return -1;
            for (int i = Math.Max(0, start); i <= text.Length - word.Length; i++)
            {
                if (IsWordAt(text, i, word))
                    return i;
            }
            return -1;
        }

        private static bool IsWordAt(string text, int index, string word)
        {
            if (index < 0 || index + word.Length > text.Length)
                return false;
            if (string.Compare(text, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            if (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_'))
                return false;
            var after = index + word.Length;
            if (after < text.Length && (char.IsLetterOrDigit(text[after]) || text[after] == '_'))
                return false;
            return true;
        }

        private static JObject StoredProcedureRequest(AppIntergrationSettingParameterExDto dto)
        {
            var args = new JObject();
            var list = dto.APIConfigParameters?.SpParameters;
            if (list != null)
            {
                foreach (var parameter in list.Where(IsInputParameter).OrderBy(p => p.Ordinal))
                {
                    var name = (parameter.Name ?? "").Trim().TrimStart('@', ':');
                    if (string.IsNullOrWhiteSpace(name) || args[name] != null)
                        continue;
                    args[name] = SpArgumentSchema(parameter);
                }
            }

            return ClosedObject(args);
        }

        private static JObject SpArgumentSchema(StoredProcedureApiParameterDTO parameter)
        {
            var schema = new JObject { ["type"] = OpenApiTypeForSql(parameter?.Type) };
            if (IsNullDefault(parameter?.DefaultValue))
            {
                schema["nullable"] = true;
                schema["example"] = JValue.CreateNull();
                return schema;
            }

            var example = ExampleFromDefault(parameter.DefaultValue, (string)schema["type"]);
            if (example != null)
                schema["example"] = example;
            return schema;
        }

        private static string OpenApiTypeForSql(string sqlType)
        {
            var name = SqlTypeName(sqlType);
            if (name == "bit" || name == "bool" || name == "boolean")
                return "boolean";
            if (name == "tinyint" || name == "smallint" || name == "int" || name == "bigint" || name == "integer")
                return "integer";
            if (name == "decimal" || name == "numeric" || name == "float" || name == "real"
                || name == "money" || name == "smallmoney" || name == "number" || name == "double")
                return "number";
            return "string";
        }

        private static string SqlTypeName(string sqlType)
        {
            var text = (sqlType ?? "").Trim().ToLowerInvariant();
            var paren = text.IndexOf('(');
            if (paren >= 0)
                text = text.Substring(0, paren);
            return text.Trim();
        }

        private static bool IsNullDefault(string value)
        {
            return value == null
                || value.Trim().Length == 0
                || value.Trim().Equals("null", StringComparison.OrdinalIgnoreCase);
        }

        private static JToken ExampleFromDefault(string value, string openApiType)
        {
            var text = (value ?? "").Trim();
            if (openApiType == "boolean")
            {
                if (text.Equals("true", StringComparison.OrdinalIgnoreCase)) return new JValue(true);
                if (text.Equals("false", StringComparison.OrdinalIgnoreCase)) return new JValue(false);
                return new JValue(text);
            }
            if (openApiType == "integer" && long.TryParse(text, out var whole))
                return new JValue(whole);
            if (openApiType == "number" && decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var number))
                return new JValue(number);
            return new JValue(text);
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

            var queryValues = dto.APIConfigParameters?.QueryParams;
            var parameters = new JArray();
            foreach (var name in names)
                parameters.Add(QueryParameter(name, QueryValue(queryValues, name)));
            return parameters;
        }

        private static string QueryValue(Dictionary<string, string> query, string name)
        {
            if (query == null || string.IsNullOrWhiteSpace(name))
                return null;
            string value;
            if (query.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value))
                return value;
            if (query.TryGetValue("@" + name, out value) && !string.IsNullOrWhiteSpace(value))
                return value;
            return null;
        }

        private static Dictionary<string, string> SearchFieldDefaults(int searchId)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var search = AppSearchConfigBL.RetrieveOneAppSearchExDto(searchId);
            if (search?.AppSearchFieldList == null)
                return map;
            foreach (var field in search.AppSearchFieldList)
            {
                var path = field?.SysTableFiledPath;
                if (string.IsNullOrWhiteSpace(path) || map.ContainsKey(path))
                    continue;
                if (!string.IsNullOrWhiteSpace(field.DefaultValue))
                    map[path] = field.DefaultValue;
            }
            return map;
        }

        private static JArray QueryParamsFromObject(JObject properties)
        {
            var parameters = new JArray();
            if (properties == null)
                return parameters;
            foreach (var property in properties.Properties())
            {
                var propertySchema = property.Value as JObject;
                var parameter = QueryParameter(property.Name);
                if (propertySchema != null)
                {
                    parameter["schema"] = (JObject)propertySchema.DeepClone();
                    if (propertySchema["example"] != null)
                        parameter["example"] = propertySchema["example"].DeepClone();
                }
                parameters.Add(parameter);
            }
            return parameters;
        }

        private static JObject QueryParameter(string name, string defaultValue = null)
        {
            var schema = new JObject { ["type"] = "string" };
            var parameter = new JObject
            {
                ["name"] = name,
                ["in"] = "query",
                ["required"] = false,
                ["schema"] = schema
            };
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                schema["default"] = defaultValue;
                schema["example"] = defaultValue;
                parameter["example"] = defaultValue;
            }
            return parameter;
        }

        private static JToken ParseJsonOrNull(string sample)
        {
            if (string.IsNullOrWhiteSpace(sample))
                return null;
            try
            {
                return JToken.Parse(sample);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Splits an App API tag into provider and type. Swagger UI has one tag level, so the page groups on this mark.
        /// </summary>
        public const string TagLevelSeparator = " » ";

        /// <summary>
        /// One tag per operation. A second tag makes Swagger list the same API twice.
        /// Third-party tag is the provider name. App API tag is provider, then API type.
        /// </summary>
        private static string TagName(AppIntergrationSettingParameterExDto dto)
        {
            if ((dto.IntergrationSettingId ?? 0) == AppIntergrationSettingBL.AppBuiltInProviderId)
            {
                var typeName = Classify(dto);
                if (string.IsNullOrWhiteSpace(typeName))
                    typeName = "Standard API";
                return AppIntergrationSettingBL.AppBuiltInProviderName + TagLevelSeparator + typeName;
            }

            var provider = dto.ProviderName?.Trim();
            if (string.IsNullOrWhiteSpace(provider))
                provider = "3rd Party API";
            return provider;
        }

        private static JArray TagDefinitions(List<string> tagOrder)
        {
            var tags = new JArray();
            foreach (var name in OrderTags(tagOrder))
            {
                var tag = new JObject { ["name"] = name };
                var separator = name.IndexOf(TagLevelSeparator, StringComparison.Ordinal);
                if (separator >= 0)
                {
                    tag["x-provider"] = name.Substring(0, separator);
                    tag["x-api-type"] = name.Substring(separator + TagLevelSeparator.Length);
                }
                tags.Add(tag);
            }
            return tags;
        }

        private static JArray TagGroups(List<string> tagOrder)
        {
            var ordered = OrderTags(tagOrder);
            var groups = new JArray();
            var appTags = new JArray();
            var appPrefix = AppIntergrationSettingBL.AppBuiltInProviderName + TagLevelSeparator;
            foreach (var name in ordered)
            {
                if (name.StartsWith(appPrefix, StringComparison.Ordinal))
                    appTags.Add(name);
            }
            if (appTags.Count > 0)
            {
                groups.Add(new JObject
                {
                    ["name"] = AppIntergrationSettingBL.AppBuiltInProviderName,
                    ["tags"] = appTags
                });
            }
            foreach (var name in ordered)
            {
                if (name.StartsWith(appPrefix, StringComparison.Ordinal))
                    continue;
                groups.Add(new JObject
                {
                    ["name"] = name,
                    ["tags"] = new JArray(name)
                });
            }
            return groups;
        }

        private static List<string> OrderTags(List<string> tagOrder)
        {
            var appPrefix = AppIntergrationSettingBL.AppBuiltInProviderName + TagLevelSeparator;
            var appTags = tagOrder
                .Where(name => name.StartsWith(appPrefix, StringComparison.Ordinal))
                .OrderBy(name => AppTypeRank(name.Substring(appPrefix.Length)))
                .ThenBy(name => name, StringComparer.Ordinal)
                .ToList();
            var otherTags = tagOrder
                .Where(name => !name.StartsWith(appPrefix, StringComparison.Ordinal))
                .ToList();
            appTags.AddRange(otherTags);
            return appTags;
        }

        private static int AppTypeRank(string typeName)
        {
            switch (typeName)
            {
                case "Stored Procedure API": return 0;
                case "APP Data Presentation API": return 1;
                case "APP Data Model API": return 2;
                case "SQL JSON Query API": return 3;
                case "Standard API": return 4;
                default: return 10;
            }
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

        private static JObject ClosedObject(JObject properties = null)
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = properties ?? new JObject(),
                ["additionalProperties"] = false
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
                return ClosedObject();
            }
        }

        private static JObject SchemaFromToken(JToken token, int depth)
        {
            if (token == null || token.Type == JTokenType.Null)
                return new JObject { ["nullable"] = true };
            if (depth > 6)
                return ClosedObject();

            if (token.Type == JTokenType.Object)
            {
                var properties = new JObject();
                foreach (var property in ((JObject)token).Properties())
                    properties[property.Name] = SchemaFromToken(property.Value, depth + 1);
                return ClosedObject(properties);
            }

            if (token.Type == JTokenType.Array)
            {
                var first = ((JArray)token).FirstOrDefault();
                return new JObject
                {
                    ["type"] = "array",
                    ["items"] = first == null ? ClosedObject() : SchemaFromToken(first, depth + 1)
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
