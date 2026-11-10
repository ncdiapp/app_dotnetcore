using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Pure helpers for propose_schema: idempotent app-name prefixing,
    /// composition vs lookup classification, and DDL preflight checks.
    /// </summary>
    internal static class SchemaProposeHelpers
    {
        private static readonly Regex LeadingShortPrefix = new Regex(
            @"^[A-Za-z0-9]{2,10}_(?=.+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Builds the table-name prefix from an application name (alphanumeric, max 10 + "_").
        /// </summary>
        public static string NormalizeAppPrefix(string appName)
        {
            var appPrefix = Regex.Replace(appName ?? "App", @"[^A-Za-z0-9]", "");
            if (appPrefix.Length > 10) appPrefix = appPrefix.Substring(0, 10);
            if (string.IsNullOrEmpty(appPrefix)) appPrefix = "App";
            return appPrefix + "_";
        }

        /// <summary>
        /// Strips a leading short Xxx_ segment when it is not already the target app prefix.
        /// Domain-agnostic: any 2–10 alphanumeric token before the first underscore.
        /// </summary>
        public static string StripForeignShortPrefix(string tableName, string appPrefix)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrEmpty(appPrefix))
                return tableName;

            if (tableName.StartsWith(appPrefix, StringComparison.OrdinalIgnoreCase))
                return tableName;

            var m = LeadingShortPrefix.Match(tableName);
            if (!m.Success) return tableName;

            return tableName.Substring(m.Length);
        }

        /// <summary>
        /// Prefixes a table name unless it already starts with the app prefix (case-insensitive).
        /// Strips a mismatched short Xxx_ first to avoid AppPrefix_Xxx_Name doubles.
        /// </summary>
        public static string PrefixedTableName(string tableName, string appPrefix)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrEmpty(appPrefix))
                return tableName;

            if (tableName.StartsWith(appPrefix, StringComparison.OrdinalIgnoreCase))
                return tableName;

            var stripped = StripForeignShortPrefix(tableName, appPrefix);
            if (stripped.StartsWith(appPrefix, StringComparison.OrdinalIgnoreCase))
                return stripped;

            return appPrefix + stripped;
        }

        /// <summary>
        /// Renames tables (and relationship TargetTable) with an idempotent app-name prefix.
        /// </summary>
        public static void ApplyAppNamePrefix(IList<DbGenieTableMetadataDto> tables, string appName)
        {
            if (tables == null || tables.Count == 0) return;

            var appPrefix = NormalizeAppPrefix(appName);
            var nameMap = tables.ToDictionary(
                t => t.Name,
                t => PrefixedTableName(t.Name, appPrefix),
                StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                if (nameMap.TryGetValue(table.Name, out var prefixed))
                    table.Name = prefixed;

                foreach (var rel in table.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                {
                    if (!string.IsNullOrWhiteSpace(rel.TargetTable) &&
                        nameMap.TryGetValue(rel.TargetTable, out var targetPrefixed))
                        rel.TargetTable = targetPrefixed;
                }
            }
        }

        /// <summary>
        /// Marks association/lookup tables vs composition hierarchy participants.
        /// A table is composition (IsLookup=false) if it appears as a parent OR child
        /// on a validated ONE_TO_MANY edge. Standalone reference tables remain IsLookup=true.
        /// </summary>
        public static void MarkLookupVsComposition(IList<DbGenieTableMetadataDto> tables)
        {
            if (tables == null || tables.Count == 0) return;

            var columnsByTable = tables.ToDictionary(
                t => t.Name,
                t => new HashSet<string>(
                    (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>()).Select(c => c.Name),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            var pksByTable = tables.ToDictionary(
                t => t.Name,
                t => new HashSet<string>(
                    (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                        .Where(c => c.IsPrimaryKey)
                        .Select(c => c.Name),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            var compositionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in tables)
            {
                columnsByTable.TryGetValue(t.Name, out var srcCols);
                pksByTable.TryGetValue(t.Name, out var srcPks);

                var manyToOneTargets = new HashSet<string>(
                    (t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                        .Where(r => string.Equals(r.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase)
                                    && !string.IsNullOrWhiteSpace(r.TargetTable))
                        .Select(r => r.TargetTable),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var rel in t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                {
                    if (!string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(rel.TargetTable))
                        continue;

                    if (manyToOneTargets.Contains(rel.TargetTable))
                        continue;

                    if (!string.IsNullOrWhiteSpace(rel.ForeignKeyColumn) &&
                        srcCols != null && srcCols.Contains(rel.ForeignKeyColumn) &&
                        (srcPks == null || !srcPks.Contains(rel.ForeignKeyColumn)))
                        continue;

                    compositionSet.Add(t.Name);
                    compositionSet.Add(rel.TargetTable);
                }
            }

            foreach (var t in tables)
                t.IsLookup = !compositionSet.Contains(t.Name);
        }

        /// <summary>
        /// Preflight: FK columns and referenced PK columns must exist before DDL.
        /// Returns empty list when valid.
        /// </summary>
        public static List<string> ValidateSchemaForDdl(IList<DbGenieTableMetadataDto> tables)
        {
            var errors = new List<string>();
            if (tables == null || tables.Count == 0)
            {
                errors.Add("Schema contains no tables.");
                return errors;
            }

            var byName = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var table in tables)
            {
                foreach (var rel in table.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                {
                    if (string.IsNullOrWhiteSpace(rel.ForeignKeyColumn) || string.IsNullOrWhiteSpace(rel.TargetTable))
                        continue;

                    if (!byName.TryGetValue(rel.TargetTable, out var target))
                    {
                        errors.Add($"Table '{table.Name}': relationship target '{rel.TargetTable}' is not in the schema.");
                        continue;
                    }

                    DbGenieTableMetadataDto fkOwner;
                    DbGenieTableMetadataDto parent;
                    if (string.Equals(rel.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase))
                    {
                        fkOwner = target;
                        parent = table;
                    }
                    else
                    {
                        fkOwner = table;
                        parent = target;
                    }

                    var fkCols = fkOwner.Columns ?? new List<DbGenieColumnMetadataDto>();
                    if (!fkCols.Any(c => string.Equals(c.Name, rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add(
                            $"FK column '{rel.ForeignKeyColumn}' not found on table '{fkOwner.Name}' " +
                            $"(relationship {rel.Type ?? "?"} from '{table.Name}' → '{rel.TargetTable}').");
                    }

                    string refColumn = rel.ReferencedColumn;
                    if (string.IsNullOrWhiteSpace(refColumn))
                    {
                        var pk = parent.Columns?.FirstOrDefault(c => c.IsPrimaryKey);
                        refColumn = pk?.Name;
                    }

                    if (string.IsNullOrWhiteSpace(refColumn))
                    {
                        errors.Add($"Table '{parent.Name}' has no primary key and no ReferencedColumn for FK from '{fkOwner.Name}'.");
                    }
                    else
                    {
                        var parentCols = parent.Columns ?? new List<DbGenieColumnMetadataDto>();
                        if (!parentCols.Any(c => string.Equals(c.Name, refColumn, StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add(
                                $"Referenced column '{refColumn}' not found on parent table '{parent.Name}' " +
                                $"(FK '{rel.ForeignKeyColumn}' on '{fkOwner.Name}').");
                        }
                    }
                }
            }

            return errors;
        }
    }
}
