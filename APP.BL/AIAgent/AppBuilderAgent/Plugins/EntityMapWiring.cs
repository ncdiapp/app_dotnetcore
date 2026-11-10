using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.EntityDto;
using Newtonsoft.Json.Linq;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Wires Entity Data Source IDs onto transaction fields from an entityMapJson object.
    /// Keys (first match wins resolution style per key):
    ///   field:{fieldId}              — that field only
    ///   {TableName}.{ColumnName}     — one field (also matches UnitDisplayName.Column)
    ///   {ColumnName}                 — all fields with that column name
    /// </summary>
    internal static class EntityMapWiring
    {
        public sealed class FieldRef
        {
            public int     FieldId    { get; set; }
            public string  ColumnName { get; set; }
            public string  TableName  { get; set; }
            public string  UnitName   { get; set; }
            public int?    EntityId   { get; set; }
            public int?    ControlType { get; set; }
        }

        public static List<FieldRef> LoadFields(int transactionId)
        {
            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            return SnapshotFields(transExDto);
        }

        public static List<FieldRef> SnapshotFields(AppTransactionExDto transExDto)
        {
            var list = new List<FieldRef>();
            if (transExDto?.AppTransactionUnitList == null) return list;

            foreach (var u in transExDto.AppTransactionUnitList)
            {
                foreach (var f in u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                {
                    if (f?.Id == null) continue;
                    list.Add(new FieldRef
                    {
                        FieldId     = Convert.ToInt32(f.Id),
                        ColumnName  = f.DataBaseFieldName,
                        TableName   = u.DataBaseTableName,
                        UnitName    = u.UnitDisplayName,
                        EntityId    = f.EntityId,
                        ControlType = f.ControlType
                    });
                }
            }
            return list;
        }

        /// <summary>
        /// Pure key → field resolution (unit-testable without DB saves).
        /// </summary>
        public static List<FieldRef> ResolveTargets(string key, IReadOnlyList<FieldRef> fields)
        {
            var hits = new List<FieldRef>();
            if (string.IsNullOrWhiteSpace(key) || fields == null || fields.Count == 0)
                return hits;

            key = key.Trim();

            // field:{id}
            if (key.StartsWith("field:", StringComparison.OrdinalIgnoreCase))
            {
                var idPart = key.Substring("field:".Length).Trim();
                if (int.TryParse(idPart, out var fieldId))
                {
                    var one = fields.FirstOrDefault(f => f.FieldId == fieldId);
                    if (one != null) hits.Add(one);
                }
                return hits;
            }

            // Table.Column or Unit.Column
            var dot = key.IndexOf('.');
            if (dot > 0 && dot < key.Length - 1)
            {
                var left  = key.Substring(0, dot).Trim();
                var right = key.Substring(dot + 1).Trim();
                hits.AddRange(fields.Where(f =>
                    ColumnNameMatches(f.ColumnName, right) &&
                    (UnitOrTableMatches(f.TableName, left) || UnitOrTableMatches(f.UnitName, left))));
                if (hits.Count > 0) return hits;
            }

            // bare column name (also try Status ↔ StatusId)
            hits.AddRange(fields.Where(f => ColumnNameMatches(f.ColumnName, key)));
            return hits;
        }

        /// <summary>Match Status to StatusId / MovementType to MovementTypeId and exact names.</summary>
        internal static bool ColumnNameMatches(string columnName, string key)
        {
            if (string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(key))
                return false;
            if (string.Equals(columnName, key, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(columnName, key + "Id", StringComparison.OrdinalIgnoreCase))
                return true;
            if (key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(columnName, key.Substring(0, key.Length - 2), StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        internal static bool UnitOrTableMatches(string unitOrTable, string key)
        {
            if (string.IsNullOrWhiteSpace(unitOrTable) || string.IsNullOrWhiteSpace(key))
                return false;
            if (string.Equals(unitOrTable, key, StringComparison.OrdinalIgnoreCase))
                return true;
            static string Norm(string s) =>
                new string((s ?? "").Where(c => c != ' ' && c != '_' && c != '-').ToArray());
            return string.Equals(Norm(unitOrTable), Norm(key), StringComparison.OrdinalIgnoreCase);
        }

        public static List<object> Wire(int transactionId, JObject entityMap)
        {
            var results = new List<object>();
            if (entityMap == null || transactionId <= 0) return results;

            var fields = LoadFields(transactionId);
            if (fields.Count == 0) return results;

            foreach (var prop in entityMap.Properties())
            {
                var key = prop.Name;
                var entityId = (int?)prop.Value;
                if (!entityId.HasValue) continue;

                var targets = ResolveTargets(key, fields);
                if (targets.Count == 0)
                {
                    results.Add(new { Key = key, EntityId = entityId, Wired = false, Error = "No matching field" });
                    continue;
                }

                foreach (var target in targets)
                {
                    try
                    {
                        var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(target.FieldId);
                        if (field == null)
                        {
                            results.Add(new { Key = key, EntityId = entityId, FieldId = target.FieldId, Wired = false, Error = "Field not found" });
                            continue;
                        }
                        field.IsModified  = true;
                        field.EntityId    = entityId;
                        field.ControlType = 1; // DDL
                        var saveResult = AppTransactionBL.SaveAppTransactionFieldExDto(field);
                        bool ok = saveResult?.ValidationResult?.HasErrors == false;
                        target.EntityId = entityId;
                        target.ControlType = 1;
                        results.Add(new
                        {
                            Key = key,
                            Column = target.ColumnName,
                            Table = target.TableName,
                            EntityId = entityId,
                            FieldId = target.FieldId,
                            Wired = ok
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { Key = key, EntityId = entityId, FieldId = target.FieldId, Error = ex.Message });
                    }
                }
            }

            return results;
        }

        public static List<object> WireFromJson(int transactionId, string entityMapJson, out string parseError)
        {
            parseError = null;
            if (string.IsNullOrWhiteSpace(entityMapJson) || transactionId <= 0)
                return new List<object>();

            try
            {
                var entityMap = JObject.Parse(entityMapJson);
                return Wire(transactionId, entityMap);
            }
            catch (Exception ex)
            {
                parseError = ex.Message;
                return new List<object> { new { Warning = "entityMapJson parse failed: " + ex.Message } };
            }
        }
    }
}
