using System;
using System.Collections.Generic;
using System.Linq;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Configure dynamic Matrix child units under a Root (classic Cartesian Generate Matrix).
    ///
    /// Runtime (Generate Matrix / actionType 46):
    ///   For each Matrix KEY field with MatrixForeignKeyFieldId set, read that SOURCE GRID
    ///   field's VALUES (e.g. 2 selected ColorIds), then Cartesian-product into Matrix rows.
    ///
    /// Typical 80% design (shared master DDL Entity):
    ///   Color master (1000 rows) + Size master (1000) → two Entity DDLs.
    ///   Source Color grid columns example:
    ///     ProductColorId (PK, auto-increment — meaningless, NOT Matrix KEY),
    ///     ProductId (FK Link-to-parent),
    ///     ColorId (DDL → Color Entity) ← THIS is the Matrix KEY / Generate source field.
    ///   Matrix KEY columns share the same EntityId as Source ColorId/SizeId for display.
    ///   MatrixForeignKeyFieldId → Source.ColorId (DDL KEY), never Source PK / Link FK.
    ///   Rare (~20%): Entity is built from the Source grid table itself.
    ///
    /// Terminology: do NOT call Root's children "siblings"; Sibling Unit = root-level peers of Root only.
    /// </summary>
    public static class MatrixUnitConfigHelper
    {
        /// <summary>
        /// Fuzzy table/unit name match: exact, ends-with, or suffix after last underscore
        /// (SalesTier ↔ CrmQuota_SalesTier).
        /// </summary>
        public static bool TableNameMatches(string actual, string hint)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(hint))
                return false;
            if (string.Equals(actual, hint, StringComparison.OrdinalIgnoreCase))
                return true;

            var a = actual.Trim();
            var h = hint.Trim();
            if (a.EndsWith(h, StringComparison.OrdinalIgnoreCase))
                return true;
            if (h.EndsWith(a, StringComparison.OrdinalIgnoreCase))
                return true;

            var aTail = a.Contains("_") ? a.Substring(a.LastIndexOf('_') + 1) : a;
            var hTail = h.Contains("_") ? h.Substring(h.LastIndexOf('_') + 1) : h;
            return string.Equals(aTail, hTail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(aTail, h, StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, hTail, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolve matrixKeyTables hints to real table names present in the schema list.
        /// </summary>
        public static void NormalizeMatrixKeyTableNames(IList<DbGenieTableMetadataDto> tables)
        {
            if (tables == null || tables.Count == 0) return;
            var allNames = tables.Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name))
                .Select(t => t.Name).ToList();

            foreach (var t in tables.Where(x => x != null && x.IsMatrix))
            {
                var raw = t.MatrixKeyTableNames ?? new List<string>();
                var resolved = new List<string>();
                foreach (var hint in raw.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    var match = allNames.FirstOrDefault(n => TableNameMatches(n, hint));
                    resolved.Add(match ?? hint.Trim());
                }
                t.MatrixKeyTableNames = resolved
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                t.IsLookup = false;
            }
        }

        /// <summary>True when <paramref name="tableName"/> is a matrix table or listed in any matrixKeyTables (fuzzy).</summary>
        public static bool IsMatrixOrDimensionTable(string tableName, IList<DbGenieTableMetadataDto> tables)
        {
            if (string.IsNullOrWhiteSpace(tableName) || tables == null) return false;
            foreach (var t in tables.Where(x => x != null))
            {
                if (t.IsMatrix && TableNameMatches(t.Name, tableName))
                    return true;
                if (t.IsMatrix)
                {
                    foreach (var k in t.MatrixKeyTableNames ?? Enumerable.Empty<string>())
                    {
                        if (TableNameMatches(tableName, k) || TableNameMatches(k, tableName))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Before DDL / review: ensure Matrix + Source dimension tables have the columns
        /// Generate Matrix needs (80% pattern). Mutates <paramref name="tables"/> in place.
        ///
        /// Source dim (e.g. CrmMx_SalesRegion):
        ///   SalesRegionId = identity PK (never the logic KEY),
        ///   PlanId = Link to Root,
        ///   RegionId = DDL Matrix KEY (shared master Entity later).
        /// Matrix:
        ///   own identity PK + Root link + same logic KEY columns as each Source.
        /// </summary>
        public static List<string> EnsureMatrixReadyColumns(IList<DbGenieTableMetadataDto> tables)
        {
            var notes = new List<string>();
            if (tables == null || tables.Count == 0) return notes;

            NormalizeMatrixKeyTableNames(tables);
            var matrixTables = tables.Where(t => t != null && t.IsMatrix).ToList();
            if (matrixTables.Count == 0) return notes;

            var root = InferRootTable(tables);
            var rootPk = FindPrimaryKeyColumn(root) ?? EnsureIdentityPk(root, PreferIdentityName(root?.Name), notes);

            foreach (var mt in matrixTables)
            {
                mt.IsLookup = false;
                EnsureIdentityPk(mt, PreferIdentityName(mt.Name), notes);
                if (root != null && rootPk != null)
                    EnsureLinkToParent(mt, root, rootPk, notes);

                var logicKeys = new List<string>();
                foreach (var hint in mt.MatrixKeyTableNames ?? Enumerable.Empty<string>())
                {
                    var dim = tables.FirstOrDefault(t => t != null && TableNameMatches(t.Name, hint));
                    if (dim == null)
                    {
                        notes.Add($"matrixKeyTables hint '{hint}' not found in schema tables.");
                        continue;
                    }

                    dim.IsLookup = false;
                    var dimIdentity = PreferIdentityName(dim.Name);
                    var linkName = rootPk?.Name ?? (ShortTableToken(root?.Name) + "Id");

                    // If current PK is a logic-looking *Id (RegionId) that is NOT {DimShort}Id, demote it to KEY.
                    var currentPk = FindPrimaryKeyColumn(dim);
                    string logicKeyName = null;
                    if (currentPk != null
                        && !string.Equals(currentPk.Name, dimIdentity, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(currentPk.Name, linkName, StringComparison.OrdinalIgnoreCase)
                        && currentPk.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        logicKeyName = currentPk.Name;
                        currentPk.IsPrimaryKey = false;
                        currentPk.IsAutoIncrement = false;
                        currentPk.IsNullable = false;
                        notes.Add($"{dim.Name}: demoted '{logicKeyName}' from PK → DDL Matrix KEY; added identity PK '{dimIdentity}'.");
                    }

                    EnsureIdentityPk(dim, dimIdentity, notes);
                    if (root != null && rootPk != null)
                        EnsureLinkToParent(dim, root, rootPk, notes);

                    if (string.IsNullOrWhiteSpace(logicKeyName))
                    {
                        // Prefer existing non-PK / non-link *Id (RegionId).
                        logicKeyName = (dim.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                            .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name))
                            .Where(c => !c.IsPrimaryKey)
                            .Where(c => !string.Equals(c.Name, linkName, StringComparison.OrdinalIgnoreCase))
                            .Where(c => !string.Equals(c.Name, dimIdentity, StringComparison.OrdinalIgnoreCase))
                            .Where(c => c.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                            .Select(c => c.Name)
                            .FirstOrDefault();
                    }

                    if (string.IsNullOrWhiteSpace(logicKeyName))
                    {
                        // Infer from matrix columns already named *Id, else Color-style from dim short.
                        var dimShort = ShortTableToken(dim.Name);
                        logicKeyName = (mt.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                            .Select(c => c?.Name)
                            .FirstOrDefault(n =>
                                !string.IsNullOrWhiteSpace(n)
                                && n.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(n, PreferIdentityName(mt.Name), StringComparison.OrdinalIgnoreCase)
                                && !string.Equals(n, linkName, StringComparison.OrdinalIgnoreCase)
                                && n.IndexOf(dimShort, StringComparison.OrdinalIgnoreCase) < 0
                                && !string.Equals(n, dimIdentity, StringComparison.OrdinalIgnoreCase));

                        // Common: Region from SalesRegion → RegionId (strip leading Sales/Customer/…)
                        if (string.IsNullOrWhiteSpace(logicKeyName))
                            logicKeyName = InferLogicKeyName(dim.Name);

                        EnsureIntColumn(dim, logicKeyName, nullable: false, notes,
                            $"{dim.Name}: added DDL Matrix KEY column '{logicKeyName}'.");
                    }
                    else
                    {
                        var logicCol = dim.Columns.First(c =>
                            string.Equals(c.Name, logicKeyName, StringComparison.OrdinalIgnoreCase));
                        logicCol.IsPrimaryKey = false;
                        logicCol.IsAutoIncrement = false;
                    }

                    logicKeys.Add(logicKeyName);
                    EnsureIntColumn(mt, logicKeyName, nullable: false, notes,
                        $"{mt.Name}: added Matrix KEY column '{logicKeyName}' (mirrors Source).");

                    EnsureCompositionRelationships(root, dim, rootPk?.Name ?? linkName, notes);
                    EnsureCompositionRelationships(root, mt, rootPk?.Name ?? linkName, notes);
                }

                if (logicKeys.Count >= 2)
                {
                    notes.Add(
                        $"{mt.Name}: Matrix KEY columns [{string.Join(", ", logicKeys)}] ready; " +
                        "Source grids have separate identity PK + parent Link + DDL KEY.");
                }
            }

            return notes;
        }

        static DbGenieTableMetadataDto InferRootTable(IList<DbGenieTableMetadataDto> tables)
        {
            // Prefer non-lookup, non-matrix, not listed as matrixKeyTables.
            var candidates = tables.Where(t => t != null && !t.IsLookup && !t.IsMatrix
                && !IsMatrixOrDimensionTable(t.Name, tables)).ToList();
            if (candidates.Count == 1) return candidates[0];

            // Root = table that is MANY_TO_ONE target of matrix/dims, or has ONE_TO_MANY to them.
            foreach (var t in tables.Where(x => x != null && x.IsMatrix))
            {
                foreach (var rel in t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                {
                    if (!string.Equals(rel.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase)) continue;
                    var parent = tables.FirstOrDefault(p => p != null && TableNameMatches(p.Name, rel.TargetTable));
                    if (parent != null && !parent.IsMatrix) return parent;
                }
            }

            return candidates.FirstOrDefault() ?? tables.FirstOrDefault(t => t != null && !t.IsMatrix);
        }

        static string PreferIdentityName(string tableName)
        {
            var shortName = ShortTableToken(tableName);
            return string.IsNullOrEmpty(shortName) ? "Id" : shortName + "Id";
        }

        /// <summary>SalesRegion → RegionId; CustomerSegment → SegmentId; Region → RegionId.</summary>
        static string InferLogicKeyName(string dimTableName)
        {
            var shortName = ShortTableToken(dimTableName) ?? "Key";
            var prefixes = new[] { "Sales", "Customer", "Product", "Campaign", "Market", "Target", "App" };
            foreach (var p in prefixes)
            {
                if (shortName.StartsWith(p, StringComparison.OrdinalIgnoreCase) && shortName.Length > p.Length)
                {
                    var rest = shortName.Substring(p.Length);
                    if (rest.Length >= 3)
                        return rest + "Id";
                }
            }
            // Avoid identity collision: if identity is SalesRegionId, logic key can't be that — use KeyId fallback rarely
            return shortName + "KeyId";
        }

        static DbGenieColumnMetadataDto FindPrimaryKeyColumn(DbGenieTableMetadataDto table) =>
            (table?.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                .FirstOrDefault(c => c != null && c.IsPrimaryKey);

        static DbGenieColumnMetadataDto EnsureIdentityPk(
            DbGenieTableMetadataDto table,
            string identityName,
            List<string> notes)
        {
            if (table == null) return null;
            table.Columns ??= new List<DbGenieColumnMetadataDto>();

            foreach (var c in table.Columns.Where(x => x != null && x.IsPrimaryKey
                         && !string.Equals(x.Name, identityName, StringComparison.OrdinalIgnoreCase)))
            {
                c.IsPrimaryKey = false;
                c.IsAutoIncrement = false;
            }

            var existing = table.Columns.FirstOrDefault(c =>
                c != null && string.Equals(c.Name, identityName, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new DbGenieColumnMetadataDto
                {
                    Name = identityName,
                    DataType = "INT",
                    IsPrimaryKey = true,
                    IsAutoIncrement = true,
                    IsNullable = false,
                    Description = "Identity PK (not Matrix KEY)"
                };
                table.Columns.Insert(0, existing);
                notes.Add($"{table.Name}: added identity PK '{identityName}'.");
            }
            else
            {
                existing.IsPrimaryKey = true;
                existing.IsAutoIncrement = true;
                existing.IsNullable = false;
                if (string.IsNullOrWhiteSpace(existing.DataType))
                    existing.DataType = "INT";
            }

            return existing;
        }

        static void EnsureLinkToParent(
            DbGenieTableMetadataDto child,
            DbGenieTableMetadataDto root,
            DbGenieColumnMetadataDto rootPk,
            List<string> notes)
        {
            if (child == null || root == null || rootPk == null) return;
            var linkName = rootPk.Name;
            EnsureIntColumn(child, linkName, nullable: false, notes,
                $"{child.Name}: added parent Link FK '{linkName}' → {root.Name}.{rootPk.Name}.");
        }

        static void EnsureIntColumn(
            DbGenieTableMetadataDto table,
            string columnName,
            bool nullable,
            List<string> notes,
            string addNote)
        {
            if (table == null || string.IsNullOrWhiteSpace(columnName)) return;
            table.Columns ??= new List<DbGenieColumnMetadataDto>();
            var existing = table.Columns.FirstOrDefault(c =>
                c != null && string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (string.IsNullOrWhiteSpace(existing.DataType))
                    existing.DataType = "INT";
                return;
            }

            table.Columns.Add(new DbGenieColumnMetadataDto
            {
                Name = columnName,
                DataType = "INT",
                IsPrimaryKey = false,
                IsAutoIncrement = false,
                IsNullable = nullable,
                Description = "Matrix KEY / Link"
            });
            if (!string.IsNullOrWhiteSpace(addNote))
                notes.Add(addNote);
        }

        static void EnsureCompositionRelationships(
            DbGenieTableMetadataDto parent,
            DbGenieTableMetadataDto child,
            string fkColumn,
            List<string> notes)
        {
            if (parent == null || child == null || string.IsNullOrWhiteSpace(fkColumn)) return;
            parent.Relationships ??= new List<DbGenieRelationshipMetadataDto>();
            child.Relationships ??= new List<DbGenieRelationshipMetadataDto>();

            if (!parent.Relationships.Any(r =>
                    string.Equals(r?.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase)
                    && TableNameMatches(r.TargetTable, child.Name)))
            {
                parent.Relationships.Add(new DbGenieRelationshipMetadataDto
                {
                    Type = "ONE_TO_MANY",
                    TargetTable = child.Name,
                    ForeignKeyColumn = fkColumn,
                    ReferencedColumn = FindPrimaryKeyColumn(parent)?.Name
                });
                notes.Add($"{parent.Name}: ONE_TO_MANY → {child.Name} ({fkColumn}).");
            }

            if (!child.Relationships.Any(r =>
                    string.Equals(r?.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase)
                    && TableNameMatches(r.TargetTable, parent.Name)))
            {
                child.Relationships.Add(new DbGenieRelationshipMetadataDto
                {
                    Type = "MANY_TO_ONE",
                    TargetTable = parent.Name,
                    ForeignKeyColumn = fkColumn,
                    ReferencedColumn = FindPrimaryKeyColumn(parent)?.Name
                });
            }
        }

        public static object ApplyMatrixFlagsFromSchema(int transactionId, IList<DbGenieTableMetadataDto> tables)
        {
            if (transactionId <= 0 || tables == null || tables.Count == 0)
                return new { applied = false, reason = "no tables" };

            NormalizeMatrixKeyTableNames(tables);
            var matrixTables = tables.Where(t => t != null && t.IsMatrix).ToList();
            if (matrixTables.Count == 0)
                return new { applied = false, reason = "no isMatrix tables" };

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx?.RootMasterUnit?.Children == null)
                return new { applied = false, reason = "transaction has no Root children" };

            var results = new List<object>();
            var anyConfigured = false;
            var errors = new List<string>();
            foreach (var mt in matrixTables)
            {
                var keyTables = mt.MatrixKeyTableNames ?? new List<string>();
                var r = ConfigureMatrixUnitCore(tx, mt.Name, null, keyTables, createGenerateCommand: false);
                results.Add(r);
                if (IsOk(r))
                    anyConfigured = true;
                else
                    errors.Add(GetError(r) ?? "matrix configure failed");
            }

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            object commandResult = null;
            if (anyConfigured && tx != null)
                commandResult = EnsureGenerateMatrixCommand(tx, matrixUnitId: null);

            return new
            {
                applied = anyConfigured && errors.Count == 0,
                matrixResults = results,
                errors = errors.Count > 0 ? errors : null,
                generateMatrixCommand = commandResult,
                hint =
                    "Matrix KEY fields: on Matrix unit, each key column's MatrixForeignKeyFieldId must point to the " +
                    "dimension Root-child field whose VALUES (e.g. RED/BLUE and SMALL/LARGE) are Cartesian-expanded by Generate Matrix (46)."
            };
        }

        public static object ConfigureMatrixUnit(
            int transactionId,
            int? matrixUnitId,
            string matrixUnitNameOrTable,
            string matrixKeyUnitIdsCsv,
            string matrixKeyTableNamesCsv,
            bool? createGenerateMatrixCommand,
            bool? confirmed,
            bool? dryRun)
        {
            if (transactionId <= 0)
                return Fail("transactionId is required.");

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx?.RootMasterUnit == null)
                return Fail($"Transaction {transactionId} not found or has no Root unit.");

            var keyTableNames = SplitCsv(matrixKeyTableNamesCsv);
            if (!string.IsNullOrWhiteSpace(matrixKeyUnitIdsCsv))
            {
                foreach (var idStr in SplitCsv(matrixKeyUnitIdsCsv))
                {
                    if (!int.TryParse(idStr, out var uid)) continue;
                    var u = (tx.RootMasterUnit.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                        .FirstOrDefault(c => c?.Id != null && Convert.ToInt32(c.Id) == uid);
                    if (u != null && !string.IsNullOrWhiteSpace(u.DataBaseTableName))
                        keyTableNames.Add(u.DataBaseTableName);
                }
            }

            if (keyTableNames.Count < 2)
                return Fail(
                    "Provide at least two matrix key units/tables (the dimension Root child grids). " +
                    "Example: matrixKeyTableNamesCsv='ColorGrid,SizeGrid' then Generate Matrix builds Color×Size rows.");

            var preview = BuildPreview(tx, matrixUnitId, matrixUnitNameOrTable, keyTableNames);
            if (preview.error != null)
                return Fail(preview.error);

            if (dryRun == true || confirmed != true)
            {
                return new
                {
                    ok = true,
                    IsSuccess = true,
                    preview = true,
                    message = "Preview only. ask_user then confirmed=true.",
                    proposed = preview.proposed,
                    matrixKeyFieldExplain =
                        "Each Matrix KEY column gets MatrixForeignKeyFieldId = dimension Root-child KEY field Id. " +
                        "Generate Matrix reads those KEY VALUES and Cartesian-products them into Matrix rows."
                };
            }

            var applied = ConfigureMatrixUnitCore(tx, preview.matrixTableName, preview.matrixUnitId, keyTableNames, createGenerateCommand: false);
            if (!IsOk(applied))
                return applied;

            object cmd = null;
            if (createGenerateMatrixCommand != false)
            {
                AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
                tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
                cmd = EnsureGenerateMatrixCommand(tx, preview.matrixUnitId);
            }

            AppCacheManagerBL.RefreshOneHierarchyTransaction(transactionId);
            return new
            {
                ok = true,
                IsSuccess = true,
                saved = true,
                matrixUnitId = preview.matrixUnitId,
                matrixConfigured = applied,
                generateMatrixCommand = cmd,
                NextStep = "Open form → add dimension KEY rows → run Generate Matrix (46) to expand combination rows."
            };
        }

        static object ConfigureMatrixUnitCore(
            AppTransactionExDto tx,
            string matrixTableName,
            int? matrixUnitId,
            IList<string> keyTableNames,
            bool createGenerateCommand)
        {
            var root = tx?.RootMasterUnit;
            if (root?.Children == null)
                return Fail("Root has no child units.");

            var matrixUnit = FindRootChild(root, matrixUnitId, matrixTableName);
            if (matrixUnit == null)
            {
                return Fail(
                    $"Matrix unit '{matrixTableName ?? matrixUnitId?.ToString()}' is not a direct child of Root. " +
                    "Dynamic matrix units must be Root child units (not grandchildren). " +
                    "Available Root children: " + DescribeChildren(root));
            }

            matrixUnit.IsModified = true;
            matrixUnit.IsMatrixUnit = true;

            // Clear bad prior wiring on Matrix PK/Id if present (legacy auto-config bug).
            foreach (var f in matrixUnit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
            {
                if (f?.MatrixForeignKeyFieldId == null) continue;
                if (f.IsPrimaryKey == true || f.IsLinkToParentPrimaryKey == true
                    || string.Equals(f.DataBaseFieldName, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    f.IsModified = true;
                    f.MatrixForeignKeyFieldId = null;
                }
            }

            var wired = new List<object>();
            var hardErrors = new List<string>();
            var softWarnings = new List<string>();
            var usedMatrixFieldIds = new HashSet<int>();
            foreach (var keyTable in keyTableNames.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var dimUnit = FindRootChild(root, null, keyTable);
                if (dimUnit == null)
                {
                    hardErrors.Add(
                        $"Dimension unit/table '{keyTable}' not found under Root children. " +
                        $"Available: {DescribeChildren(root)}. " +
                        "Dimension grids that feed the matrix MUST be Root children (isLookup=false), not List Edit lookups.");
                    continue;
                }

                var pair = ResolveMatrixKeyPair(matrixUnit, dimUnit, usedMatrixFieldIds);
                if (pair.matrixFkField == null || pair.dimKeyField?.Id == null)
                {
                    hardErrors.Add(pair.error
                        ?? $"Could not wire Matrix KEY for dimension '{keyTable}'.");
                    continue;
                }

                // UI requires DDL-like ControlType to edit Matrix FK; match editor + Import convention.
                pair.matrixFkField.IsModified = true;
                pair.matrixFkField.MatrixForeignKeyFieldId = Convert.ToInt32(pair.dimKeyField.Id);
                pair.matrixFkField.ControlType = (int)EmAppControlType.DDL;

                var entityResolve = ResolveSharedKeyEntityId(pair.dimKeyField, pair.matrixFkField, dimUnit);
                string entityNote = entityResolve.warning;
                if (entityResolve.entityId.HasValue && entityResolve.entityId.Value > 0)
                {
                    pair.matrixFkField.EntityId = entityResolve.entityId;
                    // Keep Source KEY on the same DDL Entity (80% shared master Color/Size list).
                    if (pair.dimKeyField.EntityId == null || Convert.ToInt32(pair.dimKeyField.EntityId) <= 0)
                    {
                        pair.dimKeyField.IsModified = true;
                        pair.dimKeyField.EntityId = entityResolve.entityId;
                        if (pair.dimKeyField.ControlType != (int)EmAppControlType.DDL
                            && pair.dimKeyField.ControlType != (int)EmAppControlType.AutoComplete
                            && pair.dimKeyField.ControlType != (int)EmAppControlType.SearchAbleDDL)
                            pair.dimKeyField.ControlType = (int)EmAppControlType.DDL;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(entityNote))
                {
                    softWarnings.Add(entityNote);
                }

                if (pair.matrixFkField.Id != null)
                    usedMatrixFieldIds.Add(Convert.ToInt32(pair.matrixFkField.Id));
                wired.Add(new
                {
                    matrixKeyFieldId = pair.matrixFkField.Id,
                    matrixKeyField = pair.matrixFkField.DataBaseFieldName,
                    matrixForeignKeyFieldId = pair.dimKeyField.Id,
                    sourceKeyField = pair.dimKeyField.DataBaseFieldName,
                    sourceIsPrimaryKey = pair.dimKeyField.IsPrimaryKey == true,
                    sourceUnitId = dimUnit.Id,
                    sourceUnit = dimUnit.DataBaseTableName,
                    controlType = pair.matrixFkField.ControlType,
                    entityId = pair.matrixFkField.EntityId,
                    entitySource = entityResolve.source,
                    entityWarning = entityNote,
                    meaning =
                        $"Matrix KEY MatrixForeignKeyFieldId → Source KEY DDL [{dimUnit.DataBaseTableName}].[{pair.dimKeyField.DataBaseFieldName}]. " +
                        "Generate Matrix takes those SOURCE ROW VALUES; display labels come from the shared Entity DDL " +
                        "(usually master Color/Size list — not the Source grid table)."
                });
            }

            if (wired.Count < 2)
            {
                return Fail(
                    "Need MatrixForeignKeyFieldId wired for at least 2 dimension Root children. " +
                    string.Join(" ", hardErrors));
            }

            tx.IsModified = true;
            var save = AppTransactionBL.SaveAppTransactionExDto(tx);
            if (save?.ValidationResult?.HasErrors == true)
            {
                var msgs = save.ValidationResult.Items?.Select(i => i.Message).Where(m => !string.IsNullOrWhiteSpace(m));
                return Fail(msgs != null && msgs.Any() ? string.Join("; ", msgs) : "Save matrix unit failed.");
            }

            object cmd = null;
            if (createGenerateCommand)
                cmd = EnsureGenerateMatrixCommand(tx, Convert.ToInt32(matrixUnit.Id));

            return new
            {
                ok = true,
                IsSuccess = true,
                matrixUnitId = matrixUnit.Id,
                matrixTable = matrixUnit.DataBaseTableName,
                isMatrixUnit = true,
                wired,
                warnings = softWarnings.Count > 0 ? softWarnings : null,
                generateMatrixCommand = cmd
            };
        }

        /// <summary>
        /// Shared DDL Entity for Source KEY + Matrix KEY (80%): master Color/Size list, NOT Source grid table.
        /// Priority: Source KEY EntityId → Matrix KEY EntityId → entity whose TableName matches ColorId→Color
        /// → rare: entity for Source grid table.
        /// </summary>
        static (int? entityId, string source, string warning) ResolveSharedKeyEntityId(
            AppTransactionFieldExDto dimKeyField,
            AppTransactionFieldExDto matrixKeyField,
            AppTransactionUnitExDto dimUnit)
        {
            if (dimKeyField?.EntityId != null && Convert.ToInt32(dimKeyField.EntityId) > 0)
                return (Convert.ToInt32(dimKeyField.EntityId), "sourceKeyField", null);

            if (matrixKeyField?.EntityId != null && Convert.ToInt32(matrixKeyField.EntityId) > 0)
                return (Convert.ToInt32(matrixKeyField.EntityId), "matrixKeyField", null);

            try
            {
                var entities = AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                    .Where(e => e != null && e.EntityType == 1 && e.Id != null)
                    .ToList();

                // ColorId / SizeId → master Color / Size entity (usual case).
                var token = LogicKeyToken(dimKeyField?.DataBaseFieldName)
                    ?? LogicKeyToken(matrixKeyField?.DataBaseFieldName)
                    ?? ShortTableToken(dimUnit?.DataBaseTableName);
                if (!string.IsNullOrEmpty(token))
                {
                    var byLogic = entities.FirstOrDefault(e =>
                        (!string.IsNullOrWhiteSpace(e.TableName) && TableNameMatches(e.TableName, token))
                        || (!string.IsNullOrWhiteSpace(e.EntityCode) && TableNameMatches(e.EntityCode, token)));
                    if (byLogic?.Id != null)
                        return (Convert.ToInt32(byLogic.Id), "masterEntityByLogicKey:" + token, null);
                }

                // Rare (~20%): Entity built from the Source grid table itself.
                if (!string.IsNullOrWhiteSpace(dimUnit?.DataBaseTableName))
                {
                    var bySourceTable = entities.FirstOrDefault(e =>
                        !string.IsNullOrWhiteSpace(e.TableName)
                        && TableNameMatches(e.TableName, dimUnit.DataBaseTableName));
                    if (bySourceTable?.Id != null)
                        return (Convert.ToInt32(bySourceTable.Id), "sourceGridTableEntity", null);
                }
            }
            catch
            {
                // non-fatal
            }

            return (null, null,
                $"No shared DDL Entity for KEY '{dimKeyField?.DataBaseFieldName}'. " +
                "Usual fix: create_entity_from_table on the MASTER list (Color/Size with ~1000 rows), " +
                "set EntityId on the Source KEY DDL column, then re-run configure_matrix_unit so Matrix shares it. " +
                "Do NOT build Entity from the small Source grid table unless that is intentional (~20%).");
        }

        /// <summary>ColorId → Color; Size_Name stays Size_Name.</summary>
        static string LogicKeyToken(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) return null;
            var n = fieldName.Trim();
            if (n.Length > 2 && n.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                return n.Substring(0, n.Length - 2);
            return n;
        }

        /// <summary>
        /// Pick Matrix KEY column + Source KEY DDL column (Import style: same logic-key name).
        /// MatrixForeignKeyFieldId → Source KEY field whose ROW VALUES Generate Matrix expands.
        /// Usual: Matrix.ColorId → Source.ColorId (DDL to master Color Entity), NOT Source.Id PK.
        /// </summary>
        static (AppTransactionFieldExDto matrixFkField, AppTransactionFieldExDto dimKeyField, string error) ResolveMatrixKeyPair(
            AppTransactionUnitExDto matrixUnit,
            AppTransactionUnitExDto dimUnit,
            HashSet<int> alreadyUsedMatrixFieldIds = null)
        {
            var matrixFields = (matrixUnit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>()).ToList();
            var dimFields = (dimUnit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>()).ToList();
            var shortName = ShortTableToken(dimUnit.DataBaseTableName);
            alreadyUsedMatrixFieldIds ??= new HashSet<int>();

            bool IsUsableMatrixKeyColumn(AppTransactionFieldExDto f)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.DataBaseFieldName)) return false;
                if (f.IsPrimaryKey == true) return false;
                if (f.IsLinkToParentPrimaryKey == true) return false;
                if (string.Equals(f.DataBaseFieldName, "Id", StringComparison.OrdinalIgnoreCase)) return false;
                if (f.Id != null && alreadyUsedMatrixFieldIds.Contains(Convert.ToInt32(f.Id))) return false;
                return true;
            }

            // Source PK name (ProductColorId) — auto-increment identity, NEVER Matrix KEY in the 80% design.
            var dimPk = dimFields.FirstOrDefault(f => f.IsPrimaryKey == true)
                ?? dimFields.FirstOrDefault(f => string.Equals(f.DataBaseFieldName, "Id", StringComparison.OrdinalIgnoreCase));
            var dimPkName = dimPk?.DataBaseFieldName;

            bool IsOwnIdentityName(string fieldName)
            {
                if (string.IsNullOrWhiteSpace(fieldName)) return false;
                if (string.Equals(fieldName, "Id", StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrWhiteSpace(dimPkName)
                    && string.Equals(fieldName, dimPkName, StringComparison.OrdinalIgnoreCase))
                    return true;
                // ProductColor → ProductColorId often IS the identity even if IsPrimaryKey flag missing
                if (!string.IsNullOrEmpty(shortName)
                    && string.Equals(fieldName, shortName + "Id", StringComparison.OrdinalIgnoreCase)
                    && dimFields.Any(f => f.IsPrimaryKey == true
                        && string.Equals(f.DataBaseFieldName, fieldName, StringComparison.OrdinalIgnoreCase)))
                    return true;
                if (!string.IsNullOrWhiteSpace(dimUnit.DataBaseTableName)
                    && string.Equals(fieldName, dimUnit.DataBaseTableName + "Id", StringComparison.OrdinalIgnoreCase)
                    && dimFields.Any(f => f.IsPrimaryKey == true
                        && string.Equals(f.DataBaseFieldName, fieldName, StringComparison.OrdinalIgnoreCase)))
                    return true;
                return false;
            }

            bool IsUsableSourceKeyColumn(AppTransactionFieldExDto f)
            {
                if (f == null || string.IsNullOrWhiteSpace(f.DataBaseFieldName)) return false;
                if (f.IsLinkToParentPrimaryKey == true) return false; // ProductId → parent
                if (f.IsPrimaryKey == true) return false;             // ProductColorId PK
                if (IsOwnIdentityName(f.DataBaseFieldName)) return false;
                return true;
            }

            bool IsDdlLike(AppTransactionFieldExDto f) =>
                f != null
                && (f.EntityId != null && Convert.ToInt32(f.EntityId) > 0
                    || f.ControlType == (int)EmAppControlType.DDL
                    || f.ControlType == (int)EmAppControlType.AutoComplete
                    || f.ControlType == (int)EmAppControlType.SearchAbleDDL);

            // 1) Import / usual: same logic-key name (Matrix.ColorId ↔ Source.ColorId DDL).
            foreach (var mf in matrixFields.Where(IsUsableMatrixKeyColumn))
            {
                var dimSame = dimFields.FirstOrDefault(d =>
                    IsUsableSourceKeyColumn(d)
                    && string.Equals(d.DataBaseFieldName, mf.DataBaseFieldName, StringComparison.OrdinalIgnoreCase));
                if (dimSame?.Id != null)
                    return (mf, dimSame, null);
            }

            // 2) Source KEY = DDL Entity column (ColorId), not table identity. Prefer fields with EntityId.
            var sourceDdlKeys = dimFields
                .Where(IsUsableSourceKeyColumn)
                .Where(IsDdlLike)
                .OrderByDescending(d => d.EntityId != null && Convert.ToInt32(d.EntityId) > 0)
                .ThenBy(d => d.DataBaseFieldName)
                .ToList();

            foreach (var dimKey in sourceDdlKeys)
            {
                var matrixMatch = matrixFields.FirstOrDefault(m =>
                    IsUsableMatrixKeyColumn(m)
                    && string.Equals(m.DataBaseFieldName, dimKey.DataBaseFieldName, StringComparison.OrdinalIgnoreCase));
                if (matrixMatch != null)
                    return (matrixMatch, dimKey, null);
            }

            // 3) Soft: Matrix column name token (Color from ColorId) matches Source DDL KEY token
            foreach (var mf in matrixFields.Where(IsUsableMatrixKeyColumn))
            {
                var token = LogicKeyToken(mf.DataBaseFieldName);
                if (string.IsNullOrEmpty(token)) continue;
                var dimKey = sourceDdlKeys.FirstOrDefault(d =>
                    string.Equals(LogicKeyToken(d.DataBaseFieldName), token, StringComparison.OrdinalIgnoreCase)
                    || (d.DataBaseFieldName ?? "").IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? dimFields.FirstOrDefault(d =>
                        IsUsableSourceKeyColumn(d)
                        && (string.Equals(d.DataBaseFieldName, token + "Id", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(LogicKeyToken(d.DataBaseFieldName), token, StringComparison.OrdinalIgnoreCase)));
                if (dimKey?.Id != null)
                    return (mf, dimKey, null);
            }

            // 4) Last: any non-PK/non-Link *Id on Source matching Matrix *Id (still never Source PK)
            foreach (var mf in matrixFields.Where(IsUsableMatrixKeyColumn))
            {
                var dimKey = dimFields.FirstOrDefault(d =>
                    IsUsableSourceKeyColumn(d)
                    && string.Equals(d.DataBaseFieldName, mf.DataBaseFieldName, StringComparison.OrdinalIgnoreCase));
                if (dimKey?.Id != null)
                    return (mf, dimKey, null);
            }

            return (null, null,
                $"Matrix unit has no KEY column pair for dimension '{dimUnit.DataBaseTableName}'. " +
                "Source grid needs a DDL KEY column separate from its auto-increment PK and parent FK " +
                $"(example: ProductColorId=PK, ProductId=Link, ColorId=DDL Matrix KEY). " +
                $"Wire Matrix.ColorId → Source.ColorId — never → Source PK '{dimPkName ?? shortName + "Id"}'.");
        }

        /// <summary>
        /// Read-only snapshot of Matrix unit wiring for agent verification.
        /// </summary>
        public static object InspectMatrixUnit(int transactionId, int? matrixUnitId, string matrixUnitNameOrTable)
        {
            if (transactionId <= 0)
                return Fail("transactionId is required.");

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var tx = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (tx?.RootMasterUnit == null)
                return Fail($"Transaction {transactionId} not found or has no Root unit.");

            var root = tx.RootMasterUnit;
            var matrixUnit = FindRootChild(root, matrixUnitId, matrixUnitNameOrTable);
            if (matrixUnit == null)
            {
                // Fallback: first Root child marked IsMatrixUnit
                matrixUnit = (root.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                    .FirstOrDefault(c => c?.IsMatrixUnit == true);
            }
            if (matrixUnit == null)
            {
                return Fail(
                    "No matrix unit found. Pass matrixUnitId/name, or ensure a Root child has IsMatrixUnit=true. " +
                    "Available: " + DescribeChildren(root));
            }

            var fieldById = new Dictionary<int, (AppTransactionUnitExDto unit, AppTransactionFieldExDto field)>();
            void IndexUnit(AppTransactionUnitExDto u)
            {
                if (u?.AppTransactionFieldList == null) return;
                foreach (var f in u.AppTransactionFieldList)
                {
                    if (f?.Id == null) continue;
                    fieldById[Convert.ToInt32(f.Id)] = (u, f);
                }
            }
            IndexUnit(root);
            foreach (var c in root.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
            {
                IndexUnit(c);
                foreach (var gc in c.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                    IndexUnit(gc);
            }

            var issues = new List<string>();
            if (matrixUnit.IsMatrixUnit != true)
                issues.Add("IsMatrixUnit is false — call configure_matrix_unit.");

            var keyFields = (matrixUnit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .Where(f => f?.MatrixForeignKeyFieldId != null)
                .Select(f =>
                {
                    int srcId = Convert.ToInt32(f.MatrixForeignKeyFieldId);
                    fieldById.TryGetValue(srcId, out var src);
                    var ct = f.ControlType;
                    bool ddlOk = ct == (int)EmAppControlType.DDL
                        || ct == (int)EmAppControlType.AutoComplete
                        || ct == (int)EmAppControlType.SearchAbleDDL;
                    if (!ddlOk)
                        issues.Add($"Matrix KEY '{f.DataBaseFieldName}' ControlType={ct} is not DDL-like — editor will hide Pivot Matrix FK.");
                    if (f.EntityId == null || Convert.ToInt32(f.EntityId) <= 0)
                        issues.Add(
                            $"Matrix KEY '{f.DataBaseFieldName}' has no EntityId — wire shared master DDL Entity " +
                            $"(Color/Size list) on Source KEY '{src.field?.DataBaseFieldName}', then re-run configure_matrix_unit. " +
                            "Do not build Entity from the small Source grid table unless intentional.");
                    if (src.field == null)
                        issues.Add($"Matrix KEY '{f.DataBaseFieldName}' MatrixForeignKeyFieldId={srcId} not found.");
                    else
                    {
                        if (src.field.IsPrimaryKey == true
                            || string.Equals(src.field.DataBaseFieldName, "Id", StringComparison.OrdinalIgnoreCase))
                            issues.Add(
                                $"Matrix KEY '{f.DataBaseFieldName}' points at Source PK '{src.unit?.DataBaseTableName}.{src.field.DataBaseFieldName}'. " +
                                "Usual design: point at Source KEY DDL column (e.g. ColorId) that shares the master Entity.");
                        if (src.field.EntityId != null && Convert.ToInt32(src.field.EntityId) > 0
                            && f.EntityId != null && Convert.ToInt32(f.EntityId) > 0
                            && Convert.ToInt32(src.field.EntityId) != Convert.ToInt32(f.EntityId))
                            issues.Add(
                                $"Matrix KEY '{f.DataBaseFieldName}' EntityId={f.EntityId} ≠ Source KEY EntityId={src.field.EntityId} — should share the same DDL Entity.");
                        else if (src.field.EntityId == null || Convert.ToInt32(src.field.EntityId) <= 0)
                            issues.Add(
                                $"Source KEY '{src.unit?.DataBaseTableName}.{src.field.DataBaseFieldName}' has no EntityId — " +
                                "set shared master DDL Entity on Source KEY (80% case).");
                    }

                    return new
                    {
                        matrixFieldId = f.Id,
                        matrixField = f.DataBaseFieldName,
                        controlType = f.ControlType,
                        entityId = f.EntityId,
                        matrixForeignKeyFieldId = srcId,
                        sourceUnitId = src.unit?.Id,
                        sourceUnit = src.unit?.DataBaseTableName,
                        sourceField = src.field?.DataBaseFieldName,
                        sourceEntityId = src.field?.EntityId,
                        sourceIsPrimaryKey = src.field?.IsPrimaryKey == true,
                        sharedEntity = f.EntityId != null && src.field?.EntityId != null
                            && Convert.ToInt32(f.EntityId) == Convert.ToInt32(src.field.EntityId)
                    };
                })
                .ToList();

            if (keyFields.Count < 2)
                issues.Add($"Need ≥2 Matrix KEY fields (MatrixForeignKeyFieldId set); found {keyFields.Count}.");

            var genCmd = (tx.CommandActionList ?? Enumerable.Empty<AppProjectWorkFlowActionExDto>())
                .FirstOrDefault(c => c?.ActionType == (int)EmAppTransactionCommandType.GenerateMatrix);
            if (genCmd == null)
                issues.Add("No Generate Matrix command (ActionType 46) — configure_matrix_unit with createGenerateMatrixCommand=true.");

            return new
            {
                ok = true,
                IsSuccess = true,
                transactionId,
                matrixUnitId = matrixUnit.Id,
                matrixTable = matrixUnit.DataBaseTableName,
                isMatrixUnit = matrixUnit.IsMatrixUnit == true,
                matrixKeyFields = keyFields,
                generateMatrixCommand = genCmd == null
                    ? null
                    : new
                    {
                        commandId = genCmd.Id,
                        actionType = genCmd.ActionType,
                        paramId1 = genCmd.ActionAttribute?.ParamId1
                    },
                ready = issues.Count == 0,
                issues
            };
        }

        static AppTransactionUnitExDto FindRootChild(AppTransactionUnitExDto root, int? unitId, string nameOrTable)
        {
            var children = root.Children ?? Enumerable.Empty<AppTransactionUnitExDto>();
            if (unitId.HasValue)
            {
                var byId = children.FirstOrDefault(c => c?.Id != null && Convert.ToInt32(c.Id) == unitId.Value);
                if (byId != null) return byId;
            }
            if (string.IsNullOrWhiteSpace(nameOrTable)) return null;
            return children.FirstOrDefault(c =>
                TableNameMatches(c?.DataBaseTableName, nameOrTable)
                || TableNameMatches(c?.UnitDisplayName, nameOrTable));
        }

        static string DescribeChildren(AppTransactionUnitExDto root) =>
            string.Join(", ",
                (root.Children ?? Enumerable.Empty<AppTransactionUnitExDto>())
                    .Select(c => $"{c?.DataBaseTableName}(Id={c?.Id})"));

        static string ShortTableToken(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName)) return "";
            var t = tableName.Trim();
            return t.Contains("_") ? t.Substring(t.LastIndexOf('_') + 1) : t;
        }

        static (string error, int? matrixUnitId, string matrixTableName, object proposed) BuildPreview(
            AppTransactionExDto tx,
            int? matrixUnitId,
            string matrixUnitNameOrTable,
            IList<string> keyTableNames)
        {
            var root = tx.RootMasterUnit;
            var matrixUnit = FindRootChild(root, matrixUnitId, matrixUnitNameOrTable);
            if (matrixUnit == null)
                return (
                    $"Matrix unit not found as Root child: {matrixUnitNameOrTable ?? matrixUnitId?.ToString()}. Available: {DescribeChildren(root)}",
                    null, null, null);

            var dims = keyTableNames.Select(k =>
            {
                var u = FindRootChild(root, null, k);
                var pair = u != null ? ResolveMatrixKeyPair(matrixUnit, u) : (null, null, "not found");
                return new
                {
                    keyHint = k,
                    found = u != null,
                    unitId = u?.Id,
                    table = u?.DataBaseTableName,
                    proposedMatrixKeyField = pair.matrixFkField?.DataBaseFieldName,
                    proposedSourceKeyField = pair.dimKeyField?.DataBaseFieldName,
                    wireError = pair.error
                };
            }).ToList();

            return (null, matrixUnit.Id != null ? Convert.ToInt32(matrixUnit.Id) : (int?)null, matrixUnit.DataBaseTableName,
                new
                {
                    action = "configure_matrix_unit",
                    matrixUnitId = matrixUnit.Id,
                    matrixTable = matrixUnit.DataBaseTableName,
                    isMatrixUnit = true,
                    dimensionKeys = dims,
                    example =
                        "Source Color KEY DDL picks 2 of 1000 Colors; Source Size picks 2 of 1000 Sizes → " +
                        "Generate Matrix creates 4 Matrix rows. Display from shared Color/Size Entity; IDs from Source rows.",
                    note = "Matrix + Source grids = Root children; Matrix KEY EntityId == Source KEY EntityId (usual)."
                });
        }

        static AppTransactionFieldExDto PreferKeyField(AppTransactionUnitExDto unit)
        {
            var fields = unit.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>();
            return fields.FirstOrDefault(f => f.IsPrimaryKey == true)
                ?? fields.FirstOrDefault(f => string.Equals(f?.DataBaseFieldName, "Id", StringComparison.OrdinalIgnoreCase))
                ?? fields.FirstOrDefault();
        }

        static object EnsureGenerateMatrixCommand(AppTransactionExDto tx, int? matrixUnitId)
        {
            if (tx == null) return null;
            if (tx.CommandActionList == null)
                tx.CommandActionList = new List<AppProjectWorkFlowActionExDto>();

            var existing = tx.CommandActionList.FirstOrDefault(c =>
                c?.ActionType == (int)EmAppTransactionCommandType.GenerateMatrix);
            if (existing != null)
            {
                if (matrixUnitId.HasValue)
                {
                    existing.ActionAttribute ??= new AppActionAttributeDto();
                    existing.ActionAttribute.ParamId1 = matrixUnitId;
                    existing.IsModified = true;
                    tx.IsModified = true;
                    AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
                }
                return new { existed = true, commandId = existing.Id, paramId1 = existing.ActionAttribute?.ParamId1 };
            }

            var maxOrder = tx.CommandActionList
                .Select(c => c.ActionFlowOrder ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            var cmd = new AppProjectWorkFlowActionExDto
            {
                CommandTransactionId = Convert.ToInt32(tx.Id),
                Name = "Generate Matrix",
                ActionType = (int)EmAppTransactionCommandType.GenerateMatrix,
                ActionFlowOrder = maxOrder + 1,
                ActionGuid = Guid.NewGuid(),
                ActionAttribute = new AppActionAttributeDto
                {
                    LinkToUI = true,
                    IsShowOnTopMenu = true,
                    ParamId1 = matrixUnitId,
                    ChildActionList = new List<ChildTransactionCommandDto>()
                }
            };
            tx.CommandActionList.Add(cmd);
            tx.IsModified = true;
            var save = AppTransactionCommandBL.SaveOneTransactionCommandActionList(tx);
            if (save?.ValidationResult?.HasErrors == true)
            {
                var msgs = save.ValidationResult.Items?.Select(i => i.Message);
                return new { created = false, error = msgs != null ? string.Join("; ", msgs) : "Create Generate Matrix command failed." };
            }
            return new { created = true, commandId = cmd.Id, actionType = 46, paramId1 = matrixUnitId };
        }

        public static List<string> ValidateMatrixHierarchyPlacement(
            HierarchyTableSetupDto hierarchy,
            IList<DbGenieTableMetadataDto> tables)
        {
            var errors = new List<string>();
            if (hierarchy?.ChildTables == null || tables == null) return errors;

            NormalizeMatrixKeyTableNames(tables);

            var matrixNames = new HashSet<string>(
                tables.Where(t => t != null && t.IsMatrix).Select(t => t.Name),
                StringComparer.OrdinalIgnoreCase);

            if (matrixNames.Count == 0) return errors;

            var rootChildren = new HashSet<string>(
                hierarchy.ChildTables.Select(c => c.TableName),
                StringComparer.OrdinalIgnoreCase);

            foreach (var name in matrixNames)
            {
                if (!rootChildren.Contains(name))
                {
                    errors.Add(
                        $"Matrix table '{name}' must be a Root child unit (IsMatrix=true). " +
                        "It was not placed under Root — check FK to Root and isLookup=false.");
                }
            }

            // Dimension tables = Root children; Source KEY = DDL column (ColorId), NOT Source PK (ProductColorId).
            foreach (var mt in tables.Where(t => t != null && t.IsMatrix))
            {
                var matrixColNames = new HashSet<string>(
                    (mt.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                        .Select(c => c?.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var k in mt.MatrixKeyTableNames ?? Enumerable.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    if (!rootChildren.Any(c => TableNameMatches(c, k)))
                    {
                        errors.Add(
                            $"Matrix dimension table '{k}' (matrixKeyTables of '{mt.Name}') must be a Root child unit — " +
                            "not a lookup/List Edit. Without it, Generate Matrix cannot Cartesian-expand KEY values.");
                    }

                    var dimTable = tables.FirstOrDefault(t => t != null && TableNameMatches(t.Name, k));
                    if (dimTable == null) continue;

                    var dimCols = (dimTable.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>()).ToList();
                    var dimPkNames = new HashSet<string>(
                        dimCols.Where(c => c != null && c.IsPrimaryKey && !string.IsNullOrWhiteSpace(c.Name))
                            .Select(c => c.Name),
                        StringComparer.OrdinalIgnoreCase);
                    if (dimPkNames.Count == 0)
                        dimPkNames.Add("Id");

                    // Candidate Source KEY: *Id that are NOT auto-increment PK and NOT parent Link FK (ProductId).
                    var rootTable = hierarchy.MasterTableName;
                    var rootShort = ShortTableToken(rootTable);
                    var sourceKeyCols = dimCols
                        .Where(c => c != null && !string.IsNullOrWhiteSpace(c.Name))
                        .Where(c => !dimPkNames.Contains(c.Name))
                        .Where(c => !string.Equals(c.Name, "Id", StringComparison.OrdinalIgnoreCase))
                        .Where(c => c.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                        // Exclude parent Link FK: ProductId when Root is Product
                        .Where(c =>
                            string.IsNullOrEmpty(rootShort)
                            || (!string.Equals(c.Name, rootShort + "Id", StringComparison.OrdinalIgnoreCase)
                                && (string.IsNullOrEmpty(rootTable)
                                    || !string.Equals(c.Name, rootTable + "Id", StringComparison.OrdinalIgnoreCase))))
                        .Select(c => c.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (sourceKeyCols.Count == 0)
                    {
                        errors.Add(
                            $"Source grid table '{dimTable.Name}' must include a DDL Matrix KEY column " +
                            $"(e.g. ColorId) separate from its auto-increment PK " +
                            $"({string.Join("/", dimPkNames)}) and parent Link FK. " +
                            "Example: ProductColorId=PK, ProductId=FK, ColorId=DDL KEY.");
                        continue;
                    }

                    if (matrixColNames.Count == 0) continue;
                    foreach (var keyCol in sourceKeyCols)
                    {
                        if (!matrixColNames.Contains(keyCol))
                        {
                            errors.Add(
                                $"Matrix table '{mt.Name}' must include KEY column '{keyCol}' " +
                                $"(same name as Source '{dimTable.Name}.{keyCol}' DDL KEY; shared master Entity). " +
                                "Do not use Source PK as Matrix KEY.");
                        }
                    }
                }
            }

            foreach (var child in hierarchy.ChildTables)
            {
                foreach (var gc in child.GrandChildTableNames ?? Enumerable.Empty<string>())
                {
                    if (matrixNames.Contains(gc))
                    {
                        errors.Add(
                            $"Matrix table '{gc}' must NOT be a grandchild under '{child.TableName}'. " +
                            "Place it as a Root child unit alongside the dimension child grids.");
                    }
                }
            }

            return errors;
        }

        static List<string> SplitCsv(string csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? new List<string>()
                : csv.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

        static bool IsOk(object r)
        {
            if (r == null) return false;
            var t = r.GetType();
            var p = t.GetProperty("ok") ?? t.GetProperty("IsSuccess");
            if (p == null) return false;
            var v = p.GetValue(r);
            return v is bool b && b;
        }

        static string GetError(object r)
        {
            if (r == null) return null;
            var t = r.GetType();
            var p = t.GetProperty("Error") ?? t.GetProperty("error");
            return p?.GetValue(r)?.ToString();
        }

        static object Fail(string error) =>
            new { ok = false, IsSuccess = false, Error = error, error };
    }
}
