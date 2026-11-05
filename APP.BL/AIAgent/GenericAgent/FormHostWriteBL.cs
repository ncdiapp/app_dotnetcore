using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.Dto;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Maps agent field/grid patches onto AppTransactionExDto columns.
    /// Does not save and does not run formula/cascading. The host applies the result to the open draft.
    /// </summary>
    public static class FormHostWriteBL
    {
        static readonly string[] StemSuffixes = { "name", "code", "id", "no", "number", "desc", "description", "text", "value", "title" };

        public sealed class Result
        {
            public bool Ok;
            public string Error;
            public JArray Forms = new JArray();
            public List<string> WriteKeys = new List<string>();
        }

        public static Result NormalizeGrid(string hostContextJson, string unitId, string op, string fieldsJson)
        {
            var result = new Result();
            var nodes = FormNodes(hostContextJson);
            if (nodes.Count == 0)
            {
                result.Error = "Open form context has no transactionId.";
                return result;
            }

            var operation = string.IsNullOrWhiteSpace(op) ? "add" : op.Trim().ToLowerInvariant();
            var patches = operation == "delete" ? new List<Dictionary<string, object>>() : ParsePatches(fieldsJson);
            if (operation != "delete" && patches.Count == 0)
            {
                result.Error = "fieldsJson must be a JSON array of row objects. Call update_grid once with every new row.";
                return result;
            }

            var lookups = new Dictionary<int, List<LookupItemDto>>();
            foreach (var node in nodes)
            {
                if (!int.TryParse(TokenString(node["transactionId"]), out var transactionId)) continue;
                var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                if (tx == null) continue;
                var isList = string.Equals(TokenString(node["mode"]), "list", StringComparison.OrdinalIgnoreCase);
                var unit = isList ? RootUnit(tx) : ResolveGridUnit(tx, unitId);
                if (unit?.AppTransactionFieldList == null) continue;
                var fields = unit.AppTransactionFieldList.Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName)).ToList();
                result.WriteKeys = fields.Select(f => f.DataBaseFieldName).Distinct().ToList();
                if (operation == "delete")
                {
                    result.Forms.Add(new JObject
                    {
                        ["transactionId"] = transactionId,
                        ["unitId"] = unit.Id == null ? JValue.CreateNull() : JToken.FromObject(unit.Id),
                        ["list"] = isList,
                        ["rows"] = new JArray()
                    });
                    continue;
                }

                var rows = new JArray();
                var unresolved = new List<string>();
                foreach (var patch in patches)
                {
                    var mapped = MapPatch(patch, fields, lookups, unresolved);
                    if (unresolved.Count > 0)
                    {
                        result.Error = string.Join(" ", unresolved);
                        return result;
                    }
                    if (mapped.Count == 0) continue;
                    rows.Add(ToRow(mapped));
                }
                if (rows.Count == 0) continue;
                result.Forms.Add(new JObject
                {
                    ["transactionId"] = transactionId,
                    ["unitId"] = unit.Id == null ? JValue.CreateNull() : JToken.FromObject(unit.Id),
                    ["list"] = isList,
                    ["rows"] = rows
                });
            }

            if (result.Forms.Count == 0)
            {
                var keys = result.WriteKeys.Count > 0 ? string.Join(", ", result.WriteKeys) : "grids[].columns[].dbName";
                result.Error = "Could not map fieldsJson onto a grid. Use these write keys: " + keys + ".";
                return result;
            }
            result.Ok = true;
            return result;
        }

        public static Result NormalizeFields(string hostContextJson, string fieldsJson)
        {
            var result = new Result();
            var nodes = FormNodes(hostContextJson);
            if (nodes.Count == 0)
            {
                result.Error = "Open form context has no transactionId.";
                return result;
            }
            var patch = ParsePatches(fieldsJson).FirstOrDefault() ?? new Dictionary<string, object>();
            if (patch.Count == 0)
            {
                result.Error = "fieldsJson must be a JSON object of field values.";
                return result;
            }

            var lookups = new Dictionary<int, List<LookupItemDto>>();
            foreach (var node in nodes)
            {
                if (!int.TryParse(TokenString(node["transactionId"]), out var transactionId)) continue;
                var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(transactionId);
                if (tx == null) continue;
                var fields = OneToOneFields(tx);
                result.WriteKeys = fields.Select(f => f.DataBaseFieldName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
                var writes = new JArray();
                foreach (var pair in patch)
                {
                    var field = MatchField(fields, pair.Key);
                    if (field == null || string.IsNullOrWhiteSpace(field.DataBaseFieldName)) continue;
                    var value = Coerce(field, pair.Value, lookups);
                    if (value == null && pair.Value != null && !string.IsNullOrWhiteSpace(pair.Value.ToString())) continue;
                    writes.Add(new JObject
                    {
                        ["unitId"] = field.TransactionUnitId,
                        ["dbName"] = field.DataBaseFieldName,
                        ["value"] = value == null ? JValue.CreateNull() : JToken.FromObject(value)
                    });
                }
                if (writes.Count == 0) continue;
                result.Forms.Add(new JObject
                {
                    ["transactionId"] = transactionId,
                    ["fieldWrites"] = writes
                });
            }

            if (result.Forms.Count == 0)
            {
                var keys = result.WriteKeys.Count > 0 ? string.Join(", ", result.WriteKeys) : "fields[].dbName";
                result.Error = "Could not map fieldsJson onto this form. Use these write keys: " + keys + ".";
                return result;
            }
            result.Ok = true;
            return result;
        }

        static List<JObject> FormNodes(string hostContextJson)
        {
            var list = new List<JObject>();
            if (string.IsNullOrWhiteSpace(hostContextJson)) return list;
            JToken root;
            try { root = JToken.Parse(hostContextJson); }
            catch { return list; }
            if (root is not JObject obj) return list;
            if (obj["forms"] is JArray forms && forms.Count > 0)
            {
                foreach (var form in forms.OfType<JObject>()) list.Add(form);
                return list;
            }
            list.Add(obj);
            return list;
        }

        static AppTransactionUnitExDto RootUnit(AppTransactionExDto tx)
        {
            if (tx?.RootMasterUnit != null) return tx.RootMasterUnit;
            return tx?.AppTransactionUnitList?.FirstOrDefault();
        }

        static List<AppTransactionUnitExDto> GridUnits(AppTransactionExDto tx)
        {
            var rootId = RootUnit(tx)?.Id;
            var units = tx?.DictAllTransactionUnitIdExDto?.Values;
            if (units == null) return new List<AppTransactionUnitExDto>();
            return units.Where(u => u?.Id != null
                && (rootId == null || u.Id != rootId)
                && u.IsMasterSiblingUnit != true
                && u.AppTransactionFieldList != null
                && u.AppTransactionFieldList.Count > 0).ToList();
        }

        static AppTransactionUnitExDto ResolveGridUnit(AppTransactionExDto tx, string rawUnitId)
        {
            var grids = GridUnits(tx);
            if (grids.Count == 0) return null;
            var n = Norm(rawUnitId);
            if (!string.IsNullOrEmpty(n))
            {
                var byId = grids.FirstOrDefault(g => string.Equals(g.Id?.ToString(), rawUnitId?.Trim(), StringComparison.OrdinalIgnoreCase) || Norm(g.Id) == n);
                if (byId != null) return byId;
                var byName = grids.FirstOrDefault(g =>
                {
                    var name = Norm(g.UnitDisplayName);
                    var table = Norm(g.DataBaseTableName);
                    return name == n || table == n || (!string.IsNullOrEmpty(name) && (name.Contains(n) || n.Contains(name)));
                });
                if (byName != null) return byName;
            }
            return grids.Count == 1 ? grids[0] : null;
        }

        static AppTransactionUnitExDto ResolveAnyUnit(AppTransactionExDto tx, string rawUnitId)
        {
            if (tx == null || string.IsNullOrWhiteSpace(rawUnitId)) return null;
            var n = Norm(rawUnitId);
            var root = RootUnit(tx);
            if (root != null && (string.Equals(root.Id?.ToString(), rawUnitId.Trim(), StringComparison.OrdinalIgnoreCase)
                || Norm(root.Id) == n
                || Norm(root.UnitDisplayName) == n
                || n == "header"
                || n == "root"))
                return root;

            var units = tx.DictAllTransactionUnitIdExDto?.Values;
            if (units == null) return root;
            var byId = units.FirstOrDefault(u => string.Equals(u?.Id?.ToString(), rawUnitId.Trim(), StringComparison.OrdinalIgnoreCase) || Norm(u?.Id) == n);
            if (byId != null) return byId;
            return units.FirstOrDefault(u =>
            {
                var name = Norm(u?.UnitDisplayName);
                var table = Norm(u?.DataBaseTableName);
                return name == n || table == n || (!string.IsNullOrEmpty(name) && (name.Contains(n) || n.Contains(name)));
            });
        }

        static bool IsOneToOneUnit(AppTransactionExDto tx, AppTransactionUnitExDto unit)
        {
            if (unit?.Id == null) return false;
            var rootId = RootUnit(tx)?.Id;
            if (rootId != null && unit.Id.ToString() == rootId.ToString()) return true;
            return unit.IsMasterSiblingUnit == true;
        }

        static List<AppTransactionFieldExDto> OneToOneFields(AppTransactionExDto tx)
        {
            var root = RootUnit(tx);
            var list = new List<AppTransactionFieldExDto>();
            if (root?.AppTransactionFieldList != null) list.AddRange(root.AppTransactionFieldList);
            var units = tx?.DictAllTransactionUnitIdExDto?.Values;
            if (units == null) return list.Where(f => !string.IsNullOrWhiteSpace(f?.DataBaseFieldName)).ToList();
            foreach (var unit in units)
            {
                if (unit?.IsMasterSiblingUnit != true || unit.AppTransactionFieldList == null) continue;
                list.AddRange(unit.AppTransactionFieldList);
            }
            return list.Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName)).ToList();
        }

        static List<Dictionary<string, object>> ParsePatches(string fieldsJson)
        {
            var token = Unwrap(fieldsJson);
            if (token == null || token.Type == JTokenType.Null) return new List<Dictionary<string, object>>();
            if (token is JArray array)
            {
                if (array.Count > 0 && array.All(item => item.Type != JTokenType.Object && item.Type != JTokenType.Null))
                    return array.Select(item => new Dictionary<string, object> { ["__value"] = JsonValue(item) }).ToList();
                return array.OfType<JObject>().Select(ObjectToDict).Where(d => d.Count > 0).ToList();
            }
            if (token is JObject obj && obj["rows"] is JArray rows)
                return rows.OfType<JObject>().Select(ObjectToDict).Where(d => d.Count > 0).ToList();
            if (token is JObject one)
            {
                var dict = ObjectToDict(one);
                return dict.Count > 0 ? new List<Dictionary<string, object>> { dict } : new List<Dictionary<string, object>>();
            }
            return new List<Dictionary<string, object>>();
        }

        static JToken Unwrap(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            JToken token;
            try { token = JToken.Parse(raw); }
            catch { return null; }
            for (var i = 0; i < 4 && token.Type == JTokenType.String; i++)
            {
                var text = token.ToString().Trim();
                if (text.Length == 0) break;
                try { token = JToken.Parse(text); }
                catch { break; }
            }
            return token;
        }

        static Dictionary<string, object> ObjectToDict(JObject obj)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in obj.Properties())
            {
                if (prop.Value == null || prop.Value.Type == JTokenType.Null) continue;
                dict[prop.Name] = JsonValue(prop.Value);
            }
            return dict;
        }

        static object JsonValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.Integer) return token.Value<long>();
            if (token.Type == JTokenType.Float) return token.Value<decimal>();
            if (token.Type == JTokenType.Boolean) return token.Value<bool>();
            return token.ToString();
        }

        static Dictionary<string, object> MapPatch(
            Dictionary<string, object> patch,
            List<AppTransactionFieldExDto> fields,
            Dictionary<int, List<LookupItemDto>> lookups,
            List<string> unresolved = null)
        {
            var mapped = new Dictionary<string, object>();
            object leftover = null;
            foreach (var pair in patch)
            {
                if (pair.Key == "__value")
                {
                    leftover = pair.Value;
                    continue;
                }
                var field = MatchField(fields, pair.Key);
                if (field == null)
                {
                    leftover = pair.Value;
                    continue;
                }
                var value = Coerce(field, pair.Value, lookups);
                if (value == null && pair.Value != null && !string.IsNullOrWhiteSpace(Convert.ToString(pair.Value, CultureInfo.InvariantCulture)))
                {
                    if (IsLookupField(field))
                        unresolved?.Add(LookupError(field, pair.Value));
                    continue;
                }
                mapped[field.DataBaseFieldName] = value;
            }
            if (mapped.Count == 0 && leftover != null)
            {
                var lookup = fields.FirstOrDefault(IsLookupField) ?? fields.FirstOrDefault();
                if (lookup != null)
                {
                    var value = Coerce(lookup, leftover, lookups);
                    if (value != null) mapped[lookup.DataBaseFieldName] = value;
                    else if (IsLookupField(lookup)) unresolved?.Add(LookupError(lookup, leftover));
                }
            }
            foreach (var pair in mapped.ToList())
            {
                var written = fields.FirstOrDefault(f => Norm(f.DataBaseFieldName) == Norm(pair.Key));
                if (written == null || IsLookupField(written)) continue;
                var lookup = RelatedLookup(fields, written);
                if (lookup == null || mapped.ContainsKey(lookup.DataBaseFieldName)) continue;
                var coerced = Coerce(lookup, pair.Value, lookups);
                if (coerced != null) mapped[lookup.DataBaseFieldName] = coerced;
            }
            return mapped;
        }

        static string LookupError(AppTransactionFieldExDto field, object value) =>
            (field.DisplayName ?? field.DataBaseFieldName)
            + " (" + field.DataBaseFieldName + ") is a dropdown. Write the lookup Id, not '"
            + Convert.ToString(value, CultureInfo.InvariantCulture) + "'.";

        static AppTransactionFieldExDto MatchField(List<AppTransactionFieldExDto> fields, string key)
        {
            if (fields == null || string.IsNullOrWhiteSpace(key)) return null;
            var n = Norm(key);
            var ck = Compact(key);
            var sk = Stem(key);
            List<AppTransactionFieldExDto> cands = fields.Where(f => Norm(f.Id) == n).ToList();
            if (cands.Count == 0) cands = fields.Where(f => Norm(f.DataBaseFieldName) == n).ToList();
            if (cands.Count == 0) cands = fields.Where(f => Norm(f.DisplayName) == n).ToList();
            if (cands.Count == 0) cands = fields.Where(f => Compact(f.DataBaseFieldName) == ck || Compact(f.DisplayName) == ck).ToList();
            if (cands.Count == 0)
            {
                cands = fields.Where(f =>
                {
                    var db = Compact(f.DataBaseFieldName);
                    var label = Compact(f.DisplayName);
                    return Stem(db) == sk || Stem(label) == sk || db == sk || label == sk;
                }).ToList();
            }
            if (cands.Count == 0 && sk.Length >= 3)
            {
                cands = fields.Where(f =>
                {
                    var db = Compact(f.DataBaseFieldName);
                    var label = Compact(f.DisplayName);
                    var stem = Stem(f.DataBaseFieldName);
                    var lstem = Stem(f.DisplayName);
                    return db.Contains(sk) || label.Contains(sk) || ck.Contains(db)
                        || (stem.Length >= 3 && (sk.Contains(stem) || ck.Contains(stem)))
                        || (lstem.Length >= 3 && (sk.Contains(lstem) || ck.Contains(lstem)));
                }).ToList();
            }
            return Pick(cands, ck, sk);
        }

        static AppTransactionFieldExDto RelatedLookup(List<AppTransactionFieldExDto> fields, AppTransactionFieldExDto from)
        {
            if (from == null || fields == null) return null;
            var stem = Stem(from.DataBaseFieldName);
            if (stem.Length < 2) stem = Stem(from.DisplayName);
            var hits = fields.Where(f =>
                f != from
                && IsLookupField(f)
                && (Stem(f.DataBaseFieldName) == stem || Stem(f.DisplayName) == stem
                    || Compact(f.DataBaseFieldName).StartsWith(stem, StringComparison.Ordinal)
                    || stem.StartsWith(Stem(f.DataBaseFieldName), StringComparison.Ordinal))).ToList();
            if (hits.Count == 1) return hits[0];
            return hits.OrderBy(f => (f.DataBaseFieldName ?? "").Length).FirstOrDefault();
        }

        static AppTransactionFieldExDto Pick(List<AppTransactionFieldExDto> cands, string ck, string sk)
        {
            if (cands == null || cands.Count == 0) return null;
            if (cands.Count == 1) return cands[0];
            var lookup = cands.Where(IsLookupField).ToList();
            if (lookup.Count == 1) return lookup[0];
            if (lookup.Count > 1)
                return lookup.OrderBy(f => (f.DataBaseFieldName ?? "").Length).First();
            var exact = cands.Where(f => Compact(f.DataBaseFieldName) == ck || Compact(f.DisplayName) == ck).ToList();
            if (exact.Count == 1) return exact[0];
            var stemExact = cands.Where(f => Compact(f.DataBaseFieldName) == sk || Compact(f.DisplayName) == sk).ToList();
            if (stemExact.Count > 0) return stemExact.OrderBy(f => (f.DataBaseFieldName ?? "").Length).First();
            return null;
        }

        static bool IsLookupField(AppTransactionFieldExDto field)
        {
            if (field == null) return false;
            if (field.EntityId.HasValue && field.EntityId.Value > 0) return true;
            var control = field.ControlType;
            return control == (int)EmAppControlType.DDL
                || control == (int)EmAppControlType.SearchAbleDDL
                || control == (int)EmAppControlType.AutoComplete
                || control == (int)EmAppControlType.RadioButtons
                || control == (int)EmAppControlType.MultiSelectDDL;
        }

        static object Coerce(AppTransactionFieldExDto field, object value, Dictionary<int, List<LookupItemDto>> lookups)
        {
            if (value == null) return null;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
            if (text.Length == 0) return null;
            var control = field.ControlType;
            if (control == (int)EmAppControlType.Date || control == (int)EmAppControlType.DateTimeDetail)
            {
                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                    return dt;
                if (DateTime.TryParse(text, out dt)) return dt;
                return text;
            }
            if (IsLookupField(field))
            {
                if (!field.EntityId.HasValue) return null;
                var hit = ResolveLookup(LookupItems(field.EntityId.Value, lookups), text);
                if (hit != null) return hit;
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lookupId))
                    return lookupId;
                return null;
            }
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
                return whole;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) && text.IndexOf('.') >= 0)
                return number;
            return text;
        }

        static object ResolveLookup(List<LookupItemDto> items, string text)
        {
            if (items == null || items.Count == 0 || string.IsNullOrWhiteSpace(text)) return null;
            var n = text.Trim();
            var ck = Compact(n);
            object One(IEnumerable<LookupItemDto> hits)
            {
                var list = hits.Where(i => i?.Id != null).ToList();
                if (list.Count == 1) return list[0].Id;
                var exact = list.Where(i => string.Equals((i.Display ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)).ToList();
                return exact.Count == 1 ? exact[0].Id : null;
            }
            var byId = One(items.Where(i => string.Equals(Convert.ToString(i?.Id, CultureInfo.InvariantCulture), n, StringComparison.OrdinalIgnoreCase)));
            if (byId != null) return byId;
            var byDisplay = One(items.Where(i => string.Equals((i?.Display ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)
                || Compact(i?.Display) == ck));
            if (byDisplay != null) return byDisplay;
            var byFirst = One(items.Where(i => string.Equals((i?.FirstField ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)));
            if (byFirst != null) return byFirst;
            var byLast = One(items.Where(i => string.Equals((i?.LastField ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)));
            if (byLast != null) return byLast;
            return One(items.Where(i =>
            {
                var parts = (i?.Display ?? "").Split('|');
                return parts.Any(p => string.Equals(p.Trim(), n, StringComparison.OrdinalIgnoreCase) || Compact(p) == ck);
            }));
        }

        static List<LookupItemDto> LookupItems(int entityId, Dictionary<int, List<LookupItemDto>> lookups)
        {
            if (lookups.TryGetValue(entityId, out var cached)) return cached;
            List<LookupItemDto> items;
            try { items = AppEntityInfoBL.GetLookupItemList(entityId, "") ?? new List<LookupItemDto>(); }
            catch { items = new List<LookupItemDto>(); }
            lookups[entityId] = items;
            return items;
        }

        static JObject ToRow(Dictionary<string, object> mapped)
        {
            var row = new JObject();
            foreach (var pair in mapped)
                row[pair.Key] = pair.Value == null ? JValue.CreateNull() : JToken.FromObject(pair.Value);
            return row;
        }

        static string TokenString(JToken token) => token == null || token.Type == JTokenType.Null ? "" : token.ToString().Trim();

        static string Norm(object value) => (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Trim().ToLowerInvariant();

        static string Compact(object value) => Regex.Replace(Norm(value), "[^a-z0-9]", "");

        static string Stem(object value)
        {
            var c = Compact(value);
            foreach (var suf in StemSuffixes)
            {
                if (c.Length > suf.Length && c.EndsWith(suf, StringComparison.Ordinal))
                    return c.Substring(0, c.Length - suf.Length);
            }
            return c;
        }

        public static FormAgentApplyToolResultDto ApplyPostedForm(FormAgentApplyToolRequestDto request)
        {
            var result = new FormAgentApplyToolResultDto
            {
                ToolResult = new JObject { ["ok"] = false, ["saved"] = false }
            };
            if (request?.FormData == null)
            {
                result.ToolResult["error"] = "FormData is required.";
                return result;
            }
            var tool = (request.ToolName ?? "").Trim().ToLowerInvariant();
            var mode = (request.Mode ?? "master").Trim().ToLowerInvariant();
            var args = request.Args ?? new JObject();
            try
            {
                if (mode == "list")
                {
                    var list = request.FormData.ToObject<AppListDataDto>();
                    if (list == null)
                    {
                        result.ToolResult["error"] = "FormData is not list data.";
                        result.FormData = request.FormData;
                        return result;
                    }
                    DataModelDateTimeConverterBL.ConvertListEditPostedUtcToClientForCalculation(list);
                    result.ToolResult = ApplyList(tool, list, args);
                    if (result.ToolResult.Value<bool>("ok") && tool != "get_form_state")
                        list.IsDirty = true;
                    result.FormData = JToken.FromObject(list);
                    return result;
                }
                var master = request.FormData.ToObject<AppMasterDetailDto>();
                if (master == null)
                {
                    result.ToolResult["error"] = "FormData is not master-detail data.";
                    result.FormData = request.FormData;
                    return result;
                }
                DataModelDateTimeConverterBL.ConvertMasterDetailPostedUtcToClientForCalculation(master);
                result.ToolResult = ApplyMaster(tool, master, args, master);
                if (result.ToolResult.Value<bool>("ok") && tool != "get_form_state")
                    master.IsDirty = true;
                result.FormData = JToken.FromObject(master);
                return result;
            }
            catch (Exception ex)
            {
                result.ToolResult["error"] = ex.Message;
                result.FormData = request.FormData;
                return result;
            }
        }

        static JObject ApplyMaster(string tool, AppMasterDetailDto form, JObject args, AppMasterDetailDto posted)
        {
            var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(form?.TransactionId ?? 0);
            if (tx == null) return Fail("Transaction was not found.");
            var lookups = new Dictionary<int, List<LookupItemDto>>();
            SeedLookups(posted ?? form, tx, lookups);
            if (tool == "get_form_state") return ReadMaster(form, tx, args, lookups);
            if (tool == "set_fields") return WriteMasterFields(form, tx, args, lookups);
            if (tool == "update_grid") return WriteMasterGrid(form, tx, args, lookups);
            return Fail(tool + " is not applied on FormData.");
        }

        static void SeedLookups(AppMasterDetailDto form, AppTransactionExDto tx, Dictionary<int, List<LookupItemDto>> lookups)
        {
            var src = form?.DictCascadingFiledDataSource;
            if (src == null || src.Count == 0) return;
            var byId = new Dictionary<string, AppTransactionFieldExDto>(StringComparer.OrdinalIgnoreCase);
            void AddUnit(AppTransactionUnitExDto unit)
            {
                if (unit?.AppTransactionFieldList == null) return;
                foreach (var field in unit.AppTransactionFieldList)
                {
                    if (field?.Id == null) continue;
                    byId[field.Id.ToString()] = field;
                }
            }
            AddUnit(RootUnit(tx));
            if (tx?.DictAllTransactionUnitIdExDto != null)
            {
                foreach (var unit in tx.DictAllTransactionUnitIdExDto.Values)
                    AddUnit(unit);
            }
            foreach (var pair in src)
            {
                if (pair.Value == null || pair.Value.Count == 0) continue;
                if (!byId.TryGetValue(pair.Key, out var field) || !field.EntityId.HasValue) continue;
                lookups[field.EntityId.Value] = pair.Value;
            }
        }

        static JObject ApplyList(string tool, AppListDataDto form, JObject args)
        {
            var tx = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(form?.TransactionId ?? 0);
            if (tx == null) return Fail("Transaction was not found.");
            var fields = RootUnit(tx)?.AppTransactionFieldList?.Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName)).ToList()
                ?? new List<AppTransactionFieldExDto>();
            var lookups = new Dictionary<int, List<LookupItemDto>>();
            if (form.ListData == null) form.ListData = new List<AppChildDataDto>();
            if (tool == "get_form_state")
            {
                var slice = RequestedFields(args, fields);
                if (slice.Error != null) return Fail(slice.Error);
                var take = ArgInt(args, "take", 20);
                var match = ArgObject(args, "match");
                var mappedMatch = match.Count == 0 ? null : MapPatch(match, fields, lookups);
                var rows = FilterRows(form.ListData, mappedMatch, ArgString(args, "rowKey"), take);
                return new JObject
                {
                    ["ok"] = true,
                    ["saved"] = false,
                    ["rowCount"] = form.ListData.Count,
                    ["matched"] = rows.Count,
                    ["rows"] = RowsJson(form.ListData, rows, slice.Fields)
                };
            }
            if (tool == "set_fields" || (tool == "update_grid" && ArgString(args, "op") != "add" && ArgString(args, "op") != "delete"))
            {
                var patch = ParsePatches(ArgString(args, "fieldsJson")).FirstOrDefault() ?? new Dictionary<string, object>();
                var unresolved = new List<string>();
                var mapped = MapPatch(patch, fields, lookups, unresolved);
                if (unresolved.Count > 0) return Fail(string.Join(" ", unresolved));
                if (mapped.Count == 0) return Fail("Could not map fieldsJson onto this list.");
                var match = ArgObject(args, "match");
                var mappedMatch = match.Count == 0 ? null : MapPatch(match, fields, lookups);
                var rowKey = ArgString(args, "rowKey");
                var hits = FilterRows(form.ListData, mappedMatch, rowKey, int.MaxValue);
                if (hits.Count == 0 && mappedMatch == null && string.IsNullOrEmpty(rowKey) && form.ListData.Count > 0)
                    hits = new List<int> { 0 };
                foreach (var i in hits) ApplyDict(form.ListData[i], mapped);
                return new JObject { ["ok"] = hits.Count > 0, ["saved"] = false, ["matched"] = hits.Count, ["updated"] = hits.Count, ["applied"] = JObject.FromObject(mapped) };
            }
            if (tool == "update_grid")
            {
                var op = ArgString(args, "op").ToLowerInvariant();
                if (op == "add")
                {
                    var patches = ParsePatches(ArgString(args, "fieldsJson"));
                    var added = 0;
                    var unresolved = new List<string>();
                    foreach (var patch in patches)
                    {
                        var mapped = MapPatch(patch, fields, lookups, unresolved);
                        if (unresolved.Count > 0) return Fail(string.Join(" ", unresolved));
                        if (mapped.Count == 0) continue;
                        form.ListData.Add(NewChild(mapped));
                        added++;
                    }
                    return new JObject { ["ok"] = added > 0, ["saved"] = false, ["added"] = added };
                }
                if (op == "delete")
                {
                    var match = ArgObject(args, "match");
                    var mappedMatch = match.Count == 0 ? null : MapPatch(match, fields, lookups);
                    var hits = FilterRows(form.ListData, mappedMatch, ArgString(args, "rowKey"), int.MaxValue);
                    foreach (var i in hits.OrderByDescending(x => x)) form.ListData.RemoveAt(i);
                    return new JObject { ["ok"] = hits.Count > 0, ["saved"] = false, ["deleted"] = hits.Count };
                }
            }
            return Fail("Could not apply the list tool.");
        }

        static JObject WriteMasterFields(AppMasterDetailDto form, AppTransactionExDto tx, JObject args, Dictionary<int, List<LookupItemDto>> lookups)
        {
            var patch = ParsePatches(ArgString(args, "fieldsJson")).FirstOrDefault() ?? new Dictionary<string, object>();
            var fields = OneToOneFields(tx);
            var unresolved = new List<string>();
            var mapped = MapPatch(patch, fields, lookups, unresolved);
            if (unresolved.Count > 0) return Fail(string.Join(" ", unresolved));
            if (mapped.Count == 0) return Fail("Could not map fieldsJson onto this form.");
            var applied = new JObject();
            foreach (var pair in mapped)
            {
                var field = MatchField(fields, pair.Key);
                if (field == null) continue;
                SetOneToOne(form, tx, field, pair.Value);
                applied[field.DataBaseFieldName] = pair.Value == null ? JValue.CreateNull() : JToken.FromObject(pair.Value);
            }
            return new JObject { ["ok"] = true, ["saved"] = false, ["applied"] = applied };
        }

        static JObject WriteMasterGrid(AppMasterDetailDto form, AppTransactionExDto tx, JObject args, Dictionary<int, List<LookupItemDto>> lookups)
        {
            var unit = ResolveGridUnit(tx, ArgString(args, "unitId"));
            if (unit == null) return Fail("Grid unit was not found. Use unitId or the unit name from HostContextJson.");
            var fields = unit.AppTransactionFieldList.Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName)).ToList();
            var buckets = ResolveGridBuckets(form, tx, unit, ArgString(args, "parentRowKey"));
            if (buckets.Count == 0) return Fail("Grid rows were not found on this form.");
            var op = (ArgString(args, "op") ?? "update").ToLowerInvariant();
            var match = ArgObject(args, "match");
            var mappedMatch = match.Count == 0 ? null : MapPatch(match, fields, lookups);
            var rowKey = ArgString(args, "rowKey");

            if (op == "add")
            {
                var patches = ParsePatches(ArgString(args, "fieldsJson"));
                var added = 0;
                var target = buckets[0];
                var unresolved = new List<string>();
                foreach (var patch in patches)
                {
                    var rowMapped = MapPatch(patch, fields, lookups, unresolved);
                    if (unresolved.Count > 0) return Fail(string.Join(" ", unresolved));
                    if (rowMapped.Count == 0) continue;
                    target.Add(NewChild(rowMapped));
                    added++;
                }
                return new JObject { ["ok"] = added > 0, ["saved"] = false, ["unitId"] = unit.Id == null ? JValue.CreateNull() : JToken.FromObject(unit.Id), ["added"] = added };
            }

            var patchOne = ParsePatches(ArgString(args, "fieldsJson")).FirstOrDefault() ?? new Dictionary<string, object>();
            var updateErrors = new List<string>();
            var mapped = op == "delete" ? new Dictionary<string, object>() : MapPatch(patchOne, fields, lookups, updateErrors);
            if (updateErrors.Count > 0) return Fail(string.Join(" ", updateErrors));
            if (op != "delete" && mapped.Count == 0) return Fail("Could not map fieldsJson onto grid columns.");
            var updated = 0;
            var deleted = 0;
            foreach (var bucket in buckets)
            {
                var hits = FilterRows(bucket, mappedMatch, rowKey, int.MaxValue);
                if (op == "delete")
                {
                    foreach (var i in hits.OrderByDescending(x => x)) { bucket.RemoveAt(i); deleted++; }
                    continue;
                }
                foreach (var i in hits) { ApplyDict(bucket[i], mapped); updated++; }
            }
            return new JObject
            {
                ["ok"] = (op == "delete" ? deleted : updated) > 0,
                ["saved"] = false,
                ["unitId"] = unit.Id == null ? JValue.CreateNull() : JToken.FromObject(unit.Id),
                ["matched"] = updated + deleted,
                ["updated"] = updated,
                ["deleted"] = deleted,
                ["applied"] = mapped.Count == 0 ? new JObject() : JObject.FromObject(mapped)
            };
        }

        static JObject ReadMaster(AppMasterDetailDto form, AppTransactionExDto tx, JObject args, Dictionary<int, List<LookupItemDto>> lookups)
        {
            var unitId = ArgString(args, "unitId");
            if (string.IsNullOrWhiteSpace(unitId))
            {
                var rootFields = RootUnit(tx)?.AppTransactionFieldList;
                var slice = RequestedFields(args, rootFields);
                if (slice.Error != null) return Fail(slice.Error);
                return new JObject
                {
                    ["ok"] = true,
                    ["saved"] = false,
                    ["values"] = CatalogValues(form.DictOneToOneFields, slice.Fields)
                };
            }
            var oneUnit = ResolveAnyUnit(tx, unitId);
            if (oneUnit != null && IsOneToOneUnit(tx, oneUnit))
            {
                var slice = RequestedFields(args, oneUnit.AppTransactionFieldList);
                if (slice.Error != null) return Fail(slice.Error);
                return new JObject
                {
                    ["ok"] = true,
                    ["saved"] = false,
                    ["unitId"] = oneUnit.Id == null ? JValue.CreateNull() : JToken.FromObject(oneUnit.Id),
                    ["values"] = CatalogValues(DictForOneToOne(form, tx, oneUnit), slice.Fields)
                };
            }
            var unit = ResolveGridUnit(tx, unitId) ?? (oneUnit != null && !IsOneToOneUnit(tx, oneUnit) ? oneUnit : null);
            if (unit == null) return Fail("Unit was not found. Use unitId or the unit name from HostContextJson.");
            var fields = unit.AppTransactionFieldList.Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName)).ToList();
            var gridSlice = RequestedFields(args, fields);
            if (gridSlice.Error != null) return Fail(gridSlice.Error);
            var buckets = ResolveGridBuckets(form, tx, unit, ArgString(args, "parentRowKey"));
            var all = buckets.SelectMany(b => b).ToList();
            var match = ArgObject(args, "match");
            var mappedMatch = match.Count == 0 ? null : MapPatch(match, fields, lookups);
            var take = ArgInt(args, "take", 20);
            var hits = FilterRows(all, mappedMatch, ArgString(args, "rowKey"), take);
            return new JObject
            {
                ["ok"] = true,
                ["saved"] = false,
                ["unitId"] = unit.Id == null ? JValue.CreateNull() : JToken.FromObject(unit.Id),
                ["rowCount"] = all.Count,
                ["matched"] = hits.Count,
                ["truncated"] = all.Count > hits.Count,
                ["rows"] = RowsJson(all, hits, gridSlice.Fields)
            };
        }

        static Dictionary<string, object> DictForOneToOne(AppMasterDetailDto form, AppTransactionExDto tx, AppTransactionUnitExDto unit)
        {
            var rootId = RootUnit(tx)?.Id;
            if (unit?.Id != null && rootId != null && unit.Id.ToString() != rootId.ToString())
            {
                var key = unit.Id.ToString();
                if (form.DictSiblingOneToOneFields != null
                    && form.DictSiblingOneToOneFields.TryGetValue(key, out var sibling)
                    && sibling != null)
                    return sibling;
            }
            return form.DictOneToOneFields ?? new Dictionary<string, object>();
        }

        static void SetOneToOne(AppMasterDetailDto form, AppTransactionExDto tx, AppTransactionFieldExDto field, object value)
        {
            var rootId = RootUnit(tx)?.Id;
            var uid = field.TransactionUnitId;
            if (uid != null && rootId != null && uid.ToString() != rootId.ToString())
            {
                if (form.DictSiblingOneToOneFields == null)
                    form.DictSiblingOneToOneFields = new Dictionary<string, Dictionary<string, object>>();
                var key = uid.ToString();
                if (!form.DictSiblingOneToOneFields.TryGetValue(key, out var dict) || dict == null)
                {
                    dict = new Dictionary<string, object>();
                    form.DictSiblingOneToOneFields[key] = dict;
                }
                dict[field.DataBaseFieldName] = value;
                return;
            }
            if (form.DictOneToOneFields == null) form.DictOneToOneFields = new Dictionary<string, object>();
            form.DictOneToOneFields[field.DataBaseFieldName] = value;
        }

        static List<List<AppChildDataDto>> ResolveGridBuckets(AppMasterDetailDto form, AppTransactionExDto tx, AppTransactionUnitExDto unit, string parentRowKey)
        {
            var uid = unit.Id?.ToString();
            var parentId = unit.ParentTransactionUnitId;
            var rootId = RootUnit(tx)?.Id;
            if (parentId == null || parentId.ToString() == rootId?.ToString())
            {
                EnsureMany(form, uid);
                return new List<List<AppChildDataDto>> { form.DictOneToManyFields[uid] };
            }
            var parentUnit = tx.DictAllTransactionUnitIdExDto != null && parentId != null && tx.DictAllTransactionUnitIdExDto.TryGetValue(parentId.Value.ToString(), out var pu) ? pu : null;
            var parentIsGrid = parentUnit != null && parentUnit.IsMasterSiblingUnit != true && parentUnit.Id?.ToString() != rootId?.ToString();
            if (!parentIsGrid)
            {
                EnsureMany(form, uid);
                return new List<List<AppChildDataDto>> { form.DictOneToManyFields[uid] };
            }
            EnsureMany(form, parentId.ToString());
            var parents = form.DictOneToManyFields[parentId.ToString()];
            var buckets = new List<List<AppChildDataDto>>();
            for (var i = 0; i < parents.Count; i++)
            {
                if (!string.IsNullOrEmpty(parentRowKey) && RowKey(parents[i], i) != parentRowKey) continue;
                if (parents[i].DictOneToManyFields == null)
                    parents[i].DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
                if (!parents[i].DictOneToManyFields.TryGetValue(uid, out var child) || child == null)
                {
                    child = new List<AppChildDataDto>();
                    parents[i].DictOneToManyFields[uid] = child;
                }
                buckets.Add(child);
            }
            return buckets;
        }

        static void EnsureMany(AppMasterDetailDto form, string uid)
        {
            if (form.DictOneToManyFields == null)
                form.DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>();
            if (!form.DictOneToManyFields.TryGetValue(uid, out var list) || list == null)
            {
                list = new List<AppChildDataDto>();
                form.DictOneToManyFields[uid] = list;
            }
        }

        static List<int> FilterRows(List<AppChildDataDto> rows, Dictionary<string, object> match, string rowKey, int take)
        {
            var hits = new List<int>();
            if (rows == null) return hits;
            for (var i = 0; i < rows.Count && hits.Count < take; i++)
            {
                if (!string.IsNullOrEmpty(rowKey) && RowKey(rows[i], i) != rowKey) continue;
                if (match != null && match.Count > 0 && !RowMatches(rows[i], match)) continue;
                hits.Add(i);
            }
            return hits;
        }

        static bool RowMatches(AppChildDataDto row, Dictionary<string, object> match)
        {
            var dict = row?.DictOneToOneFields;
            if (dict == null) return false;
            foreach (var pair in match)
            {
                object actual = null;
                foreach (var key in dict.Keys)
                {
                    if (Norm(key) == Norm(pair.Key)) { actual = dict[key]; break; }
                }
                if (Norm(actual) != Norm(pair.Value) && Compact(actual) != Compact(pair.Value))
                    return false;
            }
            return true;
        }

        static void ApplyDict(AppChildDataDto row, Dictionary<string, object> mapped)
        {
            if (row.DictOneToOneFields == null) row.DictOneToOneFields = new Dictionary<string, object>();
            foreach (var pair in mapped) row.DictOneToOneFields[pair.Key] = pair.Value;
            row.IsDirty = true;
        }

        static AppChildDataDto NewChild(Dictionary<string, object> mapped)
        {
            return new AppChildDataDto
            {
                IsNew = true,
                IsDirty = true,
                DictOneToOneFields = new Dictionary<string, object>(mapped),
                DictOneToManyFields = new Dictionary<string, List<AppChildDataDto>>()
            };
        }

        static string RowKey(AppChildDataDto row, int index)
        {
            if (!string.IsNullOrWhiteSpace(row?.PKValueCombinString)) return row.PKValueCombinString;
            return index.ToString(CultureInfo.InvariantCulture);
        }

        sealed class FieldSlice
        {
            public List<AppTransactionFieldExDto> Fields = new List<AppTransactionFieldExDto>();
            public string Error;
        }

        static FieldSlice RequestedFields(JObject args, IEnumerable<AppTransactionFieldExDto> fields)
        {
            var all = (fields ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName))
                .ToList();
            var names = ArgNameList(args, "fields");
            if (names.Count == 0)
                return new FieldSlice { Error = "Pass fields as a JSON array of the db names or labels you need from HostContextJson. Do not request the whole unit." };
            var picked = new List<AppTransactionFieldExDto>();
            foreach (var name in names)
            {
                var field = MatchField(all, name);
                if (field == null) continue;
                if (picked.Any(f => string.Equals(f.DataBaseFieldName, field.DataBaseFieldName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                picked.Add(field);
            }
            if (picked.Count == 0)
                return new FieldSlice { Error = "Could not map fields onto this unit. Use db names from HostContextJson." };
            return new FieldSlice { Fields = picked };
        }

        static List<string> ArgNameList(JObject args, string name)
        {
            var list = new List<string>();
            if (args == null) return list;
            var token = args[name] ?? args[char.ToUpperInvariant(name[0]) + name.Substring(1)];
            if (token == null || token.Type == JTokenType.Null) return list;
            if (token.Type == JTokenType.String)
            {
                var text = token.ToString().Trim();
                if (text.Length == 0) return list;
                try { token = JToken.Parse(text); }
                catch
                {
                    list.AddRange(text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0));
                    return list;
                }
            }
            if (token is JArray array)
            {
                foreach (var item in array)
                {
                    if (item == null || item.Type == JTokenType.Null) continue;
                    var text = item.Type == JTokenType.String ? item.ToString().Trim() : item.ToString(Newtonsoft.Json.Formatting.None);
                    if (text.Length > 0) list.Add(text);
                }
                return list;
            }
            if (token is JObject obj)
            {
                list.AddRange(obj.Properties().Select(p => p.Name).Where(n => !string.IsNullOrWhiteSpace(n)));
            }
            return list;
        }

        static List<string> CatalogDbNames(IEnumerable<AppTransactionFieldExDto> fields)
        {
            return (fields ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .Where(f => f != null && !string.IsNullOrWhiteSpace(f.DataBaseFieldName))
                .Select(f => f.DataBaseFieldName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static JObject CatalogValues(Dictionary<string, object> dict, IEnumerable<AppTransactionFieldExDto> fields)
        {
            var values = new JObject();
            var src = dict ?? new Dictionary<string, object>();
            foreach (var db in CatalogDbNames(fields))
            {
                string matchKey = null;
                foreach (var key in src.Keys)
                {
                    if (string.Equals(key, db, StringComparison.OrdinalIgnoreCase))
                    {
                        matchKey = key;
                        break;
                    }
                }
                if (matchKey == null) continue;
                var val = src[matchKey];
                values[db] = val == null ? JValue.CreateNull() : JToken.FromObject(val);
            }
            return values;
        }

        static JArray RowsJson(List<AppChildDataDto> rows, List<int> hits, IEnumerable<AppTransactionFieldExDto> fields)
        {
            var arr = new JArray();
            foreach (var i in hits)
            {
                var row = rows[i];
                var obj = new JObject { ["rowKey"] = RowKey(row, i) };
                var values = CatalogValues(row?.DictOneToOneFields, fields);
                foreach (var prop in values.Properties()) obj[prop.Name] = prop.Value;
                arr.Add(obj);
            }
            return arr;
        }

        static JObject Fail(string error) => new JObject { ["ok"] = false, ["saved"] = false, ["error"] = error };

        static string ArgString(JObject args, string name)
        {
            if (args == null) return "";
            var token = args[name] ?? args[char.ToUpperInvariant(name[0]) + name.Substring(1)];
            if (token == null || token.Type == JTokenType.Null) return "";
            if (token.Type == JTokenType.Object || token.Type == JTokenType.Array) return token.ToString(Newtonsoft.Json.Formatting.None);
            return token.ToString().Trim();
        }

        static int ArgInt(JObject args, string name, int fallback)
        {
            var text = ArgString(args, name);
            return int.TryParse(text, out var n) && n > 0 ? n : fallback;
        }

        static Dictionary<string, object> ArgObject(JObject args, string name)
        {
            if (args == null) return new Dictionary<string, object>();
            var token = args[name] ?? args[char.ToUpperInvariant(name[0]) + name.Substring(1)];
            if (token == null || token.Type == JTokenType.Null) return new Dictionary<string, object>();
            if (token.Type == JTokenType.String)
            {
                try { token = JToken.Parse(token.ToString()); }
                catch { return new Dictionary<string, object>(); }
            }
            return token is JObject obj ? ObjectToDict(obj) : new Dictionary<string, object>();
        }
    }
}
