using System;
using System.Collections.Generic;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL
{
    /// <summary>
    /// Wraps Search and Data Model GET JSON when the API is used by MCP.
    /// Payload generation calls the same GET with forPayload=1 and keeps the plain JSON.
    /// </summary>
    public static class McpApiResponseBL
    {
        public const string ForPayloadQueryKey = "forPayload";

        public static bool IsForPayload(IList<KeyValuePair<string, string>> parameters)
        {
            if (parameters == null)
                return false;
            foreach (var pair in parameters)
            {
                if (!string.Equals(pair.Key, ForPayloadQueryKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = pair.Value?.Trim();
                return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public static string ApplyEnvelope(AppIntergrationSettingParameterExDto setting, bool forPayload, string jsonData, JArray fields)
        {
            if (setting?.IsUsedByMcp != true || forPayload || string.IsNullOrWhiteSpace(jsonData))
                return jsonData;
            return Envelope(jsonData, fields);
        }

        public static string Envelope(string jsonData, JArray fields)
        {
            JToken data;
            try
            {
                data = JToken.Parse(jsonData);
            }
            catch
            {
                data = new JValue(jsonData);
            }

            var root = new JObject
            {
                ["fields"] = fields ?? new JArray(),
                ["data"] = data
            };
            return root.ToString(Formatting.None);
        }

        public static JObject EnvelopeSchema(JObject dataSchema)
        {
            return new JObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false,
                ["properties"] = new JObject
                {
                    ["fields"] = new JObject
                    {
                        ["type"] = "array",
                        ["items"] = new JObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JObject
                            {
                                ["name"] = new JObject { ["type"] = "string" },
                                ["controlType"] = new JObject { ["type"] = "string" },
                                ["entityId"] = new JObject { ["type"] = "integer", ["nullable"] = true }
                            },
                            ["additionalProperties"] = false
                        }
                    },
                    ["data"] = dataSchema ?? TransactionDataSchema(null)
                }
            };
        }

        /// <summary>
        /// MCP Data Model GET. fields and data use the same transaction field names as the live response.
        /// </summary>
        public static JObject TransactionDataSchema(AppTransactionExDto transaction)
        {
            JObject example;
            return TransactionDataSchema(transaction, out example);
        }

        public static JObject TransactionDataSchema(AppTransactionExDto transaction, out JObject example)
        {
            var properties = new JObject();
            example = new JObject();
            foreach (JObject field in TransactionFields(transaction))
            {
                var name = field["name"]?.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                SetDataProperty(properties, example, name, DataPropertySchema(field));
            }

            return new JObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["additionalProperties"] = false
            };
        }

        public static JObject TransactionEnvelopeSchema(AppTransactionExDto transaction)
        {
            var fields = TransactionFields(transaction);
            JObject dataExample;
            var dataSchema = TransactionDataSchema(transaction, out dataExample);
            var schema = EnvelopeSchema(dataSchema);
            var fieldsSchema = schema["properties"]?["fields"] as JObject;
            if (fieldsSchema != null)
                fieldsSchema["example"] = fields;
            schema["example"] = new JObject
            {
                ["fields"] = fields,
                ["data"] = dataExample
            };
            return schema;
        }

        private static JObject DataPropertySchema(JObject field)
        {
            var hint = field["controlType"]?.ToString();
            var schema = new JObject { ["nullable"] = true };
            if (string.Equals(hint, "CheckBox", StringComparison.OrdinalIgnoreCase))
                schema["type"] = "boolean";
            else if (string.Equals(hint, "Numeric", StringComparison.OrdinalIgnoreCase))
                schema["type"] = "number";
            else
                schema["type"] = "string";
            if (!string.IsNullOrWhiteSpace(hint))
                schema["x-ui-hint"] = hint;
            var entityId = field["entityId"];
            if (entityId != null && entityId.Type == JTokenType.Integer && entityId.Value<int>() > 0)
                schema["x-entityid"] = entityId.Value<int>();
            return schema;
        }

        private static void SetDataProperty(JObject properties, JObject example, string path, JObject schema)
        {
            var parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;
            var props = properties;
            var sample = example;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var child = props[parts[i]] as JObject;
                if (child == null || child["properties"] == null)
                {
                    child = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject(),
                        ["additionalProperties"] = false
                    };
                    props[parts[i]] = child;
                }
                props = (JObject)child["properties"];
                var sampleChild = sample[parts[i]] as JObject;
                if (sampleChild == null)
                {
                    sampleChild = new JObject();
                    sample[parts[i]] = sampleChild;
                }
                sample = sampleChild;
            }
            props[parts[parts.Length - 1]] = schema;
            sample[parts[parts.Length - 1]] = JValue.CreateNull();
        }

        public static string ControlTypeName(int controlType)
        {
            var name = Enum.GetName(typeof(EmAppControlType), controlType);
            return string.IsNullOrWhiteSpace(name) ? controlType.ToString() : name;
        }

        public static JArray SearchFields(ReferenceViewDto view)
        {
            var fields = new JArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddColumns(fields, seen, view?.Columns, null);
            if (view?.ChildReferenceViewDtoList == null)
                return fields;
            foreach (var child in view.ChildReferenceViewDtoList)
            {
                if (child == null)
                    continue;
                var prefix = (child.Display ?? "").Replace(" ", "") + "_" + child.Id;
                AddColumns(fields, seen, child.Columns, prefix);
            }
            return fields;
        }

        public static JArray TransactionFields(AppTransactionExDto transaction)
        {
            var fields = new JArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var root = transaction?.RootMasterUnit;
            if (root != null)
                AddObjectUnitFields(fields, seen, root, "");

            if (transaction?.AppTransactionUnitList != null)
            {
                foreach (var unit in transaction.AppTransactionUnitList)
                {
                    if (unit == null || ReferenceEquals(unit, root) || unit.IsMasterSiblingUnit != true)
                        continue;
                    AddObjectUnitFields(fields, seen, unit, unit.JsonPath);
                }
            }

            if (root?.Children != null)
            {
                foreach (var child in root.Children)
                    AddChildUnit(fields, seen, child, "");
            }
            return fields;
        }

        public static JObject PropertySchema(int? controlType, int? entityId, bool includeFieldHints)
        {
            var schema = new JObject { ["type"] = "string" };
            if (!includeFieldHints || !controlType.HasValue)
                return schema;
            schema["x-ui-hint"] = ControlTypeName(controlType.Value);
            if (entityId.HasValue && entityId.Value > 0)
                schema["x-entityid"] = entityId.Value;
            return schema;
        }

        private static void AddColumns(JArray fields, HashSet<string> seen, IList<ReferenceViewColumnDto> columns, string prefix)
        {
            if (columns == null)
                return;
            foreach (var column in columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.SysTableFiledPath))
                    continue;
                var name = string.IsNullOrWhiteSpace(prefix) ? column.SysTableFiledPath : prefix + "." + column.SysTableFiledPath;
                AddField(fields, seen, name, column.ControlType, column.EntityId);
            }
        }

        private static void AddChildUnit(JArray fields, HashSet<string> seen, AppTransactionUnitExDto unit, string parentPrefix)
        {
            if (unit == null)
                return;
            var path = Combine(parentPrefix, unit.JsonPath);
            if (unit.IsExclusiveForOwner == true)
            {
                var field = FirstField(unit);
                if (field != null)
                    AddField(fields, seen, path, field.ControlType, field.EntityId);
                return;
            }

            AddObjectUnitFields(fields, seen, unit, path);
            if (unit.Children == null)
                return;
            foreach (var child in unit.Children)
                AddChildUnit(fields, seen, child, path);
        }

        private static void AddObjectUnitFields(JArray fields, HashSet<string> seen, AppTransactionUnitExDto unit, string prefix)
        {
            if (unit?.AppTransactionFieldList == null)
                return;
            foreach (var field in unit.AppTransactionFieldList)
            {
                if (field == null || string.IsNullOrWhiteSpace(field.JsonPath))
                    continue;
                AddField(fields, seen, Combine(prefix, field.JsonPath), field.ControlType, field.EntityId);
            }
        }

        private static AppTransactionFieldExDto FirstField(AppTransactionUnitExDto unit)
        {
            if (unit?.AppTransactionFieldList == null)
                return null;
            foreach (var field in unit.AppTransactionFieldList)
            {
                if (field != null)
                    return field;
            }
            return null;
        }

        private static void AddField(JArray fields, HashSet<string> seen, string name, int controlType, int? entityId)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                return;
            fields.Add(new JObject
            {
                ["name"] = name,
                ["controlType"] = ControlTypeName(controlType),
                ["entityId"] = entityId.HasValue && entityId.Value > 0 ? new JValue(entityId.Value) : JValue.CreateNull()
            });
        }

        private static string Combine(string prefix, string name)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return name ?? "";
            if (string.IsNullOrWhiteSpace(name))
                return prefix;
            return prefix + "." + name;
        }
    }
}
