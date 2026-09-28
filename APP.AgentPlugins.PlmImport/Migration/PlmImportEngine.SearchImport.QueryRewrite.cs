using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using APP.Components.EntityDto;

namespace APP.AgentPlugins.PlmImport
{
    public static partial class PlmImportEngine
    {
        private static readonly Regex SearchSqlBracketedIdent = new Regex(
            @"\[(?<qual>[^\]]+)\]\.\[(?<col>[^\]]+)\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SearchSqlFromJoinAlias = new Regex(
            @"(?:FROM|JOIN)\s+(?:\[?dbo\]?\.)?\[(?<table>[^\]]+)\](?:\s+(?:AS\s+)?\[(?<alias>[^\]]+)\])?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SearchSqlFirstFrom = new Regex(
            @"\bFROM\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] SearchSqlIdentityColumns =
        {
            "ReferenceBasicInfoID",
            "ReferenceBasicInfoId",
            "ProductReferenceID",
            "ProductReferenceId"
        };

        private static void RewriteSearchImportQuery(
            PlmSearchImportBlueprintDto blueprint,
            SqlConnection conn,
            PlmSearchImportValidationDto validation)
        {
            if (blueprint?.DataSet == null || string.IsNullOrWhiteSpace(blueprint.DataSet.QueryText))
                return;

            var hints = BuildSearchColumnHints(blueprint);

            blueprint.DataSet.QueryText = RewriteAndValidateSearchSql(
                conn,
                blueprint.DataSet.QueryText,
                hints,
                validation);

            foreach (var join in blueprint.DataSet.Joins ?? Enumerable.Empty<PlmSearchImportJoinDto>())
            {
                if (join == null)
                    continue;
                string leftTable = FirstNonEmpty(join.LeftTable, blueprint.DataSet.RootTableName, blueprint.DataSet.PrimaryTableName);
                string rightTable = join.AppTableName;
                join.LeftColumn = RewriteStandaloneColumnName(conn, join.LeftColumn, leftTable, hints, validation);
                join.RightColumn = RewriteStandaloneColumnName(conn, join.RightColumn, rightTable, hints, validation);
            }
        }

        private static void RewriteSiblingDataSetPatchQuery(
            PlmSearchSiblingViewBlueprintDto blueprint,
            SqlConnection conn,
            PlmSearchImportValidationDto validation)
        {
            var patch = blueprint?.DataSetPatch;
            if (patch == null)
                return;

            var hints = BuildSiblingColumnHints(blueprint);

            if (!string.IsNullOrWhiteSpace(patch.ResultingQueryText))
            {
                patch.ResultingQueryText = RewriteAndValidateSearchSql(
                    conn, patch.ResultingQueryText, hints, validation);
            }

            foreach (var join in patch.AddLeftJoins ?? Enumerable.Empty<PlmSearchSiblingViewAddJoinDto>())
            {
                if (join == null)
                    continue;
                join.LeftColumn = RewriteStandaloneColumnName(
                    conn, join.LeftColumn, join.LeftTable, hints, validation);
                join.RightColumn = RewriteStandaloneColumnName(
                    conn, join.RightColumn, join.AppTableName, hints, validation);
            }

            foreach (var col in patch.AddColumns ?? Enumerable.Empty<PlmSearchSiblingViewAddColumnDto>())
            {
                if (col == null)
                    continue;
                col.SysTableFiledPath = RewriteStandaloneColumnName(
                    conn, col.SysTableFiledPath, col.AppTableName, hints, validation);
            }
        }

        private static string RewriteAndValidateSearchSql(
            SqlConnection conn,
            string sql,
            SearchColumnHints hints,
            PlmSearchImportValidationDto validation)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            string rewritten = sql;
            for (int pass = 0; pass < 3; pass++)
            {
                var ctx = LoadSearchQueryRewriteContext(conn, rewritten, hints);
                string next = RewriteQualifiedColumnIdents(rewritten, ctx, validation);
                string schemaError = TryValidateSearchSqlSchema(conn, next);
                if (schemaError == null)
                    return next;

                if (string.Equals(next, rewritten, StringComparison.Ordinal))
                {
                    validation.Errors.Add(
                        "Search DataSet query failed schema validation after column remap: " + schemaError);
                    return next;
                }

                rewritten = next;
            }

            string lastError = TryValidateSearchSqlSchema(conn, rewritten);
            if (lastError != null)
                validation.Errors.Add("Search DataSet query failed schema validation: " + lastError);
            return rewritten;
        }

        private static string RewriteStandaloneColumnName(
            SqlConnection conn,
            string column,
            string table,
            SearchColumnHints hints,
            PlmSearchImportValidationDto validation)
        {
            if (string.IsNullOrWhiteSpace(column) || string.IsNullOrWhiteSpace(table))
                return column;

            var ctx = LoadSearchQueryRewriteContext(conn, "SELECT 1 FROM [dbo].[" + table.Trim().Trim('[', ']') + "]", hints);
            string resolved = ctx.Resolve(table, column, hints.DisplayTextFor(column), hints.SubItemIdFor(column));
            if (!string.IsNullOrWhiteSpace(resolved)
                && !resolved.Equals(column.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                validation.Warnings.Add($"Remapped {table}.{column} -> {resolved}.");
                return resolved;
            }

            return column;
        }

        private static string RewriteQualifiedColumnIdents(
            string sql,
            SearchQueryRewriteContext ctx,
            PlmSearchImportValidationDto validation)
        {
            var fromMatch = SearchSqlFirstFrom.Match(sql);
            int fromIndex = fromMatch.Success ? fromMatch.Index : sql.Length;
            var matches = SearchSqlBracketedIdent.Matches(sql);
            if (matches.Count == 0)
                return sql;

            string result = sql;
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var m = matches[i];
                string qual = m.Groups["qual"].Value;
                string col = m.Groups["col"].Value;
                if (IsSqlQualifierNotTable(qual))
                    continue;

                string resolved = ctx.Resolve(qual, col, ctx.Hints.DisplayTextFor(col), ctx.Hints.SubItemIdFor(col));
                if (string.IsNullOrWhiteSpace(resolved) || resolved.Equals(col, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool inSelectList = m.Index < fromIndex;
                string replacement = "[" + qual + "].[" + resolved + "]";
                string after = m.Index + m.Length < result.Length
                    ? result.Substring(m.Index + m.Length)
                    : string.Empty;
                bool alreadyAliased = Regex.IsMatch(after, @"^\s+AS\s+", RegexOptions.IgnoreCase);
                if (inSelectList && !alreadyAliased)
                    replacement += " AS [" + col + "]";

                validation.Warnings.Add($"Query remap [{qual}].[{col}] -> [{resolved}].");
                result = result.Substring(0, m.Index) + replacement + result.Substring(m.Index + m.Length);
            }

            return result;
        }

        private static string TryValidateSearchSqlSchema(SqlConnection conn, string sql)
        {
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 30;
                    using (cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static SearchQueryRewriteContext LoadSearchQueryRewriteContext(
            SqlConnection conn,
            string sql,
            SearchColumnHints hints)
        {
            var ctx = new SearchQueryRewriteContext { Hints = hints ?? new SearchColumnHints() };
            foreach (Match m in SearchSqlFromJoinAlias.Matches(sql ?? string.Empty))
            {
                string table = (m.Groups["table"].Value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(table))
                    continue;
                ctx.AddTable(table);
                if (m.Groups["alias"].Success && !string.IsNullOrWhiteSpace(m.Groups["alias"].Value))
                    ctx.AliasToTable[m.Groups["alias"].Value] = table;
            }

            foreach (var table in ctx.Tables.ToList())
                ctx.ColumnsByTable[table] = LoadTableColumnSet(conn, table);

            ctx.MappingRows = LoadSearchFieldMappingRows(conn, ctx.Tables);
            ctx.TxFields = LoadTransactionDisplayFields(conn, ctx.Tables);
            return ctx;
        }

        private static HashSet<string> LoadTableColumnSet(SqlConnection conn, string tableName)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(tableName))
                return set;
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT c.name
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name = @Table";
                cmd.Parameters.AddWithValue("@Table", tableName);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            set.Add(reader.GetString(0));
                    }
                }
            }

            return set;
        }

        private static List<SearchFieldMappingRow> LoadSearchFieldMappingRows(
            SqlConnection conn,
            ICollection<string> tables)
        {
            var rows = new List<SearchFieldMappingRow>();
            if (tables == null || tables.Count == 0 || !OBJECT_ID_Exists(conn, "Plm_FieldMapping"))
                return rows;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT AppTableName, AppColumnName, DwTableName, DwColumnName, PlmSubItemId, FieldKind
FROM dbo.Plm_FieldMapping
WHERE AppTableName IS NOT NULL AND AppColumnName IS NOT NULL";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appTable = reader.IsDBNull(0) ? null : reader.GetString(0);
                        string appCol = reader.IsDBNull(1) ? null : reader.GetString(1);
                        string dwTable = reader.IsDBNull(2) ? null : reader.GetString(2);
                        string dwCol = reader.IsDBNull(3) ? null : reader.GetString(3);
                        if (string.IsNullOrWhiteSpace(appTable) || string.IsNullOrWhiteSpace(appCol))
                            continue;
                        if (!tables.Contains(appTable) && (string.IsNullOrWhiteSpace(dwTable) || !tables.Contains(dwTable)))
                            continue;
                        rows.Add(new SearchFieldMappingRow
                        {
                            AppTableName = appTable,
                            AppColumnName = appCol,
                            DwTableName = dwTable,
                            DwColumnName = dwCol,
                            PlmSubItemId = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4)),
                            FieldKind = reader.IsDBNull(5) ? null : reader.GetString(5)
                        });
                    }
                }
            }

            return rows;
        }

        private static List<SearchTxDisplayField> LoadTransactionDisplayFields(
            SqlConnection conn,
            ICollection<string> tables)
        {
            var rows = new List<SearchTxDisplayField>();
            if (tables == null || tables.Count == 0 || !OBJECT_ID_Exists(conn, "AppTransactionField"))
                return rows;

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT u.DataBaseTableName, f.DisplayName, f.DataBaseFieldName, f.SortOrder, ISNULL(f.IsVisible, 1)
FROM dbo.AppTransactionUnit u
INNER JOIN dbo.AppTransactionField f ON f.TransactionUnitID = u.TransactionUnitID
WHERE u.DataBaseTableName IS NOT NULL AND f.DataBaseFieldName IS NOT NULL";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string table = reader.IsDBNull(0) ? null : reader.GetString(0);
                        if (string.IsNullOrWhiteSpace(table) || !tables.Contains(table))
                            continue;
                        rows.Add(new SearchTxDisplayField
                        {
                            TableName = table,
                            DisplayName = reader.IsDBNull(1) ? null : reader.GetString(1),
                            DatabaseFieldName = reader.IsDBNull(2) ? null : reader.GetString(2),
                            SortOrder = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                            IsVisible = !reader.IsDBNull(4) && Convert.ToInt32(reader.GetValue(4)) != 0
                        });
                    }
                }
            }

            return rows;
        }

        private static SearchColumnHints BuildSearchColumnHints(PlmSearchImportBlueprintDto blueprint)
        {
            var hints = new SearchColumnHints();
            foreach (var field in blueprint?.SearchView?.Fields ?? Enumerable.Empty<PlmSearchImportSearchViewFieldDto>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;
                hints.Add(field.SysTableFiledPath, field.DisplayText, null);
            }

            foreach (var field in blueprint?.CriteriaFields ?? Enumerable.Empty<PlmSearchImportCriteriaFieldDto>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;
                hints.Add(field.SysTableFiledPath, field.DisplayText, null);
            }

            foreach (var fr in blueprint?.FieldResolution ?? Enumerable.Empty<PlmSearchImportFieldResolutionDto>())
            {
                if (fr == null)
                    continue;
                string path = FirstNonEmpty(fr.Resolved?.SysTableFiledPath, fr.Resolved?.AppColumnName);
                hints.Add(path, fr.PlmSource?.DisplayLabel, fr.PlmSource?.PlmSubItemId);
            }

            return hints;
        }

        private static SearchColumnHints BuildSiblingColumnHints(PlmSearchSiblingViewBlueprintDto blueprint)
        {
            var hints = new SearchColumnHints();
            foreach (var field in blueprint?.SearchView?.Fields ?? Enumerable.Empty<PlmSearchImportSearchViewFieldDto>())
            {
                if (field == null || string.IsNullOrWhiteSpace(field.SysTableFiledPath))
                    continue;
                hints.Add(field.SysTableFiledPath, field.DisplayText, null);
            }

            foreach (var col in blueprint?.DataSetPatch?.AddColumns ?? Enumerable.Empty<PlmSearchSiblingViewAddColumnDto>())
            {
                if (col == null || string.IsNullOrWhiteSpace(col.SysTableFiledPath))
                    continue;
                hints.Add(col.SysTableFiledPath, col.SysTableFiledPath, null);
            }

            return hints;
        }

        private static bool IsSqlQualifierNotTable(string qual)
        {
            return string.Equals(qual, "dbo", StringComparison.OrdinalIgnoreCase)
                || string.Equals(qual, "sys", StringComparison.OrdinalIgnoreCase)
                || string.Equals(qual, "INFORMATION_SCHEMA", StringComparison.OrdinalIgnoreCase);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }

        private static string NormalizeSearchIdent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return Regex.Replace(value.Trim().ToLowerInvariant(), @"[_\s\-/]+", string.Empty);
        }

        private sealed class SearchColumnHints
        {
            private readonly Dictionary<string, string> _displayByColumn =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, int> _subItemByColumn =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public void Add(string column, string display, int? subItemId)
            {
                if (string.IsNullOrWhiteSpace(column))
                    return;
                string key = column.Trim();
                if (!string.IsNullOrWhiteSpace(display) && !_displayByColumn.ContainsKey(key))
                    _displayByColumn[key] = display.Trim();
                if (subItemId.HasValue)
                    _subItemByColumn[key] = subItemId.Value;
            }

            public string DisplayTextFor(string column)
            {
                if (string.IsNullOrWhiteSpace(column))
                    return null;
                return _displayByColumn.TryGetValue(column.Trim(), out var display) ? display : null;
            }

            public int? SubItemIdFor(string column)
            {
                if (string.IsNullOrWhiteSpace(column))
                    return null;
                return _subItemByColumn.TryGetValue(column.Trim(), out var id) ? id : (int?)null;
            }
        }

        private sealed class SearchFieldMappingRow
        {
            public string AppTableName { get; set; }
            public string AppColumnName { get; set; }
            public string DwTableName { get; set; }
            public string DwColumnName { get; set; }
            public int? PlmSubItemId { get; set; }
            public string FieldKind { get; set; }
        }

        private sealed class SearchTxDisplayField
        {
            public string TableName { get; set; }
            public string DisplayName { get; set; }
            public string DatabaseFieldName { get; set; }
            public int SortOrder { get; set; }
            public bool IsVisible { get; set; }
        }

        private sealed class SearchQueryRewriteContext
        {
            public SearchColumnHints Hints { get; set; }
            public HashSet<string> Tables { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> AliasToTable { get; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, HashSet<string>> ColumnsByTable { get; } =
                new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            public List<SearchFieldMappingRow> MappingRows { get; set; } = new List<SearchFieldMappingRow>();
            public List<SearchTxDisplayField> TxFields { get; set; } = new List<SearchTxDisplayField>();

            public void AddTable(string table)
            {
                if (!string.IsNullOrWhiteSpace(table))
                    Tables.Add(table.Trim());
            }

            public string Resolve(string qualifier, string column, string displayText, int? subItemId)
            {
                if (string.IsNullOrWhiteSpace(column))
                    return column;
                string table = ResolveTable(qualifier);
                if (string.IsNullOrWhiteSpace(table))
                    return column;

                ColumnsByTable.TryGetValue(table, out var cols);
                cols ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (cols.Contains(column))
                    return column;

                foreach (var identity in SearchSqlIdentityColumns)
                {
                    if (column.Equals(identity, StringComparison.OrdinalIgnoreCase) && cols.Contains("ReferenceId"))
                        return "ReferenceId";
                }

                var tableMaps = MappingRows.Where(r => TableMatches(r, table)).ToList();
                if (subItemId.HasValue)
                {
                    var bySub = tableMaps.FirstOrDefault(r =>
                        r.PlmSubItemId == subItemId.Value
                        && !string.IsNullOrWhiteSpace(r.AppColumnName)
                        && cols.Contains(PhysicalColumnFor(r, table)));
                    if (bySub != null)
                        return PhysicalColumnFor(bySub, table);
                }

                var byApp = tableMaps.FirstOrDefault(r =>
                    r.AppColumnName.Equals(column, StringComparison.OrdinalIgnoreCase) && cols.Contains(PhysicalColumnFor(r, table)));
                if (byApp != null)
                    return PhysicalColumnFor(byApp, table);

                var byDw = tableMaps.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.DwColumnName)
                    && r.DwColumnName.Equals(column, StringComparison.OrdinalIgnoreCase)
                    && cols.Contains(PhysicalColumnFor(r, table)));
                if (byDw != null)
                    return PhysicalColumnFor(byDw, table);

                if (!string.IsNullOrWhiteSpace(displayText) && cols.Contains(displayText.Trim()))
                    return cols.First(c => c.Equals(displayText.Trim(), StringComparison.OrdinalIgnoreCase));

                string tx = ResolveByTransactionDisplay(table, column, displayText, cols);
                if (!string.IsNullOrWhiteSpace(tx))
                    return tx;

                string byNorm = UniqueColumnByNorm(cols, column) ?? UniqueColumnByNorm(cols, displayText);
                return byNorm ?? column;
            }

            private string ResolveTable(string qualifier)
            {
                if (string.IsNullOrWhiteSpace(qualifier))
                    return null;
                if (AliasToTable.TryGetValue(qualifier, out var table))
                    return table;
                if (Tables.Contains(qualifier))
                    return qualifier;
                return qualifier;
            }

            private bool TableMatches(SearchFieldMappingRow row, string table)
            {
                return table.Equals(row.AppTableName, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(row.DwTableName)
                        && table.Equals(row.DwTableName, StringComparison.OrdinalIgnoreCase));
            }

            private static string PhysicalColumnFor(SearchFieldMappingRow row, string table)
            {
                if (!string.IsNullOrWhiteSpace(row.DwTableName)
                    && table.Equals(row.DwTableName, StringComparison.OrdinalIgnoreCase)
                    && !table.Equals(row.AppTableName, StringComparison.OrdinalIgnoreCase))
                {
                    return row.DwColumnName ?? row.AppColumnName;
                }

                return row.AppColumnName;
            }

            private string ResolveByTransactionDisplay(
                string table,
                string column,
                string displayText,
                HashSet<string> cols)
            {
                var candidates = TxFields
                    .Where(f =>
                        table.Equals(f.TableName, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(f.DatabaseFieldName)
                        && cols.Contains(f.DatabaseFieldName)
                        && DisplayMatches(f.DisplayName, column, displayText))
                    .OrderByDescending(f => f.IsVisible)
                    .ThenBy(f => f.SortOrder)
                    .ToList();
                return candidates.Count == 0 ? null : candidates[0].DatabaseFieldName;
            }

            private static bool DisplayMatches(string displayName, string column, string displayText)
            {
                string left = NormalizeSearchIdent(displayName);
                if (string.IsNullOrEmpty(left))
                    return false;
                string colNorm = NormalizeSearchIdent(column);
                string dispNorm = NormalizeSearchIdent(displayText);
                return (!string.IsNullOrEmpty(colNorm) && left == colNorm)
                    || (!string.IsNullOrEmpty(dispNorm) && left == dispNorm);
            }

            private static string UniqueColumnByNorm(HashSet<string> cols, string name)
            {
                string norm = NormalizeSearchIdent(name);
                if (string.IsNullOrEmpty(norm))
                    return null;
                var hits = cols.Where(c => NormalizeSearchIdent(c) == norm).ToList();
                return hits.Count == 1 ? hits[0] : null;
            }
        }
    }
}
