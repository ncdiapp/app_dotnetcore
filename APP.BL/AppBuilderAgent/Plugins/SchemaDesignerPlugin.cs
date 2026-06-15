using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// Schema design tools: propose the schema to the user for review/edit BEFORE any DDL,
    /// then execute the approved schema to create tables and transactions.
    ///
    /// Workflow:
    ///   1. propose_schema(requirements, appName) — LLM extracts schema, shows to user, blocks until approved.
    ///   2. execute_approved_schema(schemaJson, saasApplicationId) — runs DDL + creates transactions.
    ///
    /// This replaces the "black-box" create_application flow with a transparent two-step design.
    /// </summary>
    public class SchemaDesignerPlugin
    {
        private readonly int?    _dataSourceId;
        private readonly string  _schemaOwner;
        private readonly Func<AgentSchemaEvent, Task<AgentSchemaResponse>> _onSchemaReady;

        /// <summary>
        /// Stores the user-approved schema JSON after propose_schema completes.
        /// execute_approved_schema reads this instead of trusting the LLM to
        /// re-pass a large JSON string (which causes double-escaping corruption).
        /// </summary>
        private string _approvedSchemaJson;

        public SchemaDesignerPlugin(
            Func<AgentSchemaEvent, Task<AgentSchemaResponse>> onSchemaReady,
            int?   dataSourceId = null,
            string schemaOwner  = "dbo")
        {
            _onSchemaReady = onSchemaReady;
            _dataSourceId  = dataSourceId;
            _schemaOwner   = schemaOwner ?? "dbo";
        }

        // ─────────────────────────────────────────────────────────────────────
        // propose_schema — extract schema + present to user for review/edit
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("propose_schema",
            "Extract a database schema from natural-language requirements and present it to the user " +
            "for review and optional inline editing BEFORE any DDL is executed. " +
            "The user will see each table with its columns, data types, FK relationships, and a CREATE TABLE preview. " +
            "They can rename columns, change types, remove unwanted columns, or reject with feedback. " +
            "Returns {Confirmed:true, TableCount, Tables:[...names...]} when the user approves — the schema is stored internally. " +
            "Returns {Confirmed:false, Feedback:'...'} when the user rejects — adjust requirements and call again. " +
            "IMPORTANT: Call this EXACTLY ONCE per new application. " +
            "When it returns {Confirmed:true}, immediately call execute_approved_schema with NO schemaJson argument — the schema is already stored. " +
            "execute_approved_schema does NOT need a separate propose_plan call; propose_schema already served as the approval gate.")]
        public async Task<string> ProposeSchema(
            [AgentParam("Detailed natural-language description of the entities, fields, and relationships to build.", isRequired: true)]
            string requirements,
            [AgentParam("Application name used to prefix all table names, e.g. 'SalesOrder'. Max 10 chars, letters/numbers only.", isRequired: true)]
            string appName)
        {
            try
            {
                // ── IDEMPOTENCY GUARD ──────────────────────────────────────────
                // If the schema was already approved in this session, do NOT show the
                // user the approval dialog again.  The LLM sometimes calls propose_schema
                // a second time after execute_approved_schema fails; re-showing the dialog
                // would be annoying and could corrupt _approvedSchemaJson.
                // Return the prior approval immediately so the LLM can retry execute_approved_schema.
                if (!string.IsNullOrWhiteSpace(_approvedSchemaJson))
                {
                    SchemaExtractionResultDto prior = null;
                    try { prior = JsonConvert.DeserializeObject<SchemaExtractionResultDto>(_approvedSchemaJson); } catch { }
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed  = true,
                        TableCount = prior?.Tables?.Count ?? 0,
                        Tables     = prior?.Tables?.Select(t => t.Name).ToList() ?? new System.Collections.Generic.List<string>(),
                        NextStep   = "Schema was already approved earlier in this session — schema is stored internally. Call execute_approved_schema immediately. Do NOT call propose_schema again.",
                        Note       = "Returning previously approved schema (propose_schema was already confirmed)."
                    });
                }

                // ── Step 1: Extract schema via LLM ─────────────────────────────
                var extraction = await AppDbGenieBL.ExtractSchemaFromTextAsync(
                    requirements,
                    LLMProviderHelper.GetConfiguredProvider(),
                    LLMProviderHelper.GetConfiguredApiKey())
                    .ConfigureAwait(false);

                if (!extraction.IsSuccess || extraction.Tables == null || extraction.Tables.Count == 0)
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed = false,
                        Feedback  = extraction.Error ?? "Could not extract a schema from the requirements. Please provide more detail."
                    });

                // ── Step 1b: Apply app-name prefix (same logic as CreateHierarchyTransactionFromRequirementsAsync) ──
                var appPrefix = Regex.Replace(appName ?? "App", @"[^A-Za-z0-9]", "");
                if (appPrefix.Length > 10) appPrefix = appPrefix.Substring(0, 10);
                if (string.IsNullOrEmpty(appPrefix)) appPrefix = "App";
                appPrefix += "_";

                var nameMap = extraction.Tables.ToDictionary(
                    t => t.Name,
                    t => appPrefix + t.Name,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var table in extraction.Tables)
                {
                    table.Name = nameMap.ContainsKey(table.Name) ? nameMap[table.Name] : table.Name;
                    foreach (var rel in table.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                    {
                        if (!string.IsNullOrWhiteSpace(rel.TargetTable) && nameMap.ContainsKey(rel.TargetTable))
                            rel.TargetTable = nameMap[rel.TargetTable];
                    }
                }

                // ── Step 1c: Mark lookup tables (association) vs composition tables ──
                // FK direction is authoritative.  Two guards detect when the LLM inverted a
                // ONE_TO_MANY (i.e. wrote Orders→Customers instead of Orders←Customers):
                //
                //   Guard 1 — Explicit MANY_TO_ONE: if the source table ALSO has a MANY_TO_ONE
                //             to the same target, the ONE_TO_MANY is a duplicate in the wrong
                //             direction.  Source holds the FK → it is the "many" side → skip.
                //
                //   Guard 2 — FK column in source non-PK: if ForeignKeyColumn is named and lives
                //             in the SOURCE table as a non-PK column, the FK belongs to the source
                //             (MANY_TO_ONE), not the child.  Skip.
                //             Exception: if ForeignKeyColumn IS the source's PK, it is the column
                //             that the child table references (e.g. Orders.OrderId ← OrderItems.OrderId),
                //             which is a valid ONE_TO_MANY — do NOT skip.
                var columnsByTable = extraction.Tables.ToDictionary(
                    t => t.Name,
                    t => new HashSet<string>(
                        (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>()).Select(c => c.Name),
                        StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

                var pksByTable = extraction.Tables.ToDictionary(
                    t => t.Name,
                    t => new HashSet<string>(
                        (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                            .Where(c => c.IsPrimaryKey)
                            .Select(c => c.Name),
                        StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

                var compositionChildSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in extraction.Tables)
                {
                    columnsByTable.TryGetValue(t.Name, out var srcCols);
                    pksByTable.TryGetValue(t.Name, out var srcPks);

                    // Guard 1: collect MANY_TO_ONE targets — if source has MANY_TO_ONE → X,
                    // then any ONE_TO_MANY → X from the same source is the inverted direction.
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

                        // Guard 1: source also has MANY_TO_ONE to this target → inverted
                        if (manyToOneTargets.Contains(rel.TargetTable))
                            continue;

                        // Guard 2: FK column is in source table as a NON-PK column
                        //   → source owns the FK → this is MANY_TO_ONE, not ONE_TO_MANY
                        // (if FK column is the source PK it is the column the child references — valid)
                        if (!string.IsNullOrWhiteSpace(rel.ForeignKeyColumn) &&
                            srcCols != null && srcCols.Contains(rel.ForeignKeyColumn) &&
                            (srcPks == null || !srcPks.Contains(rel.ForeignKeyColumn)))
                            continue;   // inverted ONE_TO_MANY → treat as MANY_TO_ONE (lookup ref)

                        compositionChildSet.Add(rel.TargetTable);
                    }
                }

                foreach (var t in extraction.Tables)
                    t.IsLookup = !compositionChildSet.Contains(t.Name);

                // ── Step 2: Generate CREATE TABLE script preview ───────────────
                var script = AppDbGenieBL.GenerateCreateTableScripts(extraction.Tables, _schemaOwner);
                extraction.GeneratedScript = script;

                // ── Step 3: Build AgentSchemaEvent for frontend display ────────
                var schemaEvent = BuildSchemaEvent(extraction, script);

                // ── Step 4: Present to user and wait for response ─────────────
                if (_onSchemaReady == null)
                {
                    // Auto-approve when no callback wired (unit tests / legacy)
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed  = true,
                        SchemaJson = JsonConvert.SerializeObject(extraction),
                        Note       = "Auto-approved (no schema confirmation callback wired)."
                    });
                }

                AgentSchemaResponse response;
                try
                {
                    response = await _onSchemaReady(schemaEvent).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed = false,
                        Feedback  = "Schema confirmation callback error: " + ex.Message
                    });
                }

                if (!response.Confirmed)
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed = false,
                        Feedback  = response.Feedback ?? "User rejected the schema. Adjust the requirements and call propose_schema again."
                    });

                // Use the user's edited schema if provided, otherwise keep the original.
                // Store in instance field — do NOT return the raw JSON to the LLM to avoid
                // double-escaping corruption when the LLM re-passes large JSON strings.
                _approvedSchemaJson = !string.IsNullOrWhiteSpace(response.SchemaJson)
                    ? response.SchemaJson
                    : JsonConvert.SerializeObject(extraction);

                return JsonConvert.SerializeObject(new
                {
                    Confirmed  = true,
                    TableCount = extraction.Tables.Count,
                    Tables     = extraction.Tables.Select(t => t.Name).ToList(),
                    NextStep   = "Call execute_approved_schema immediately. Do NOT pass schemaJson — the schema is stored internally."
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Confirmed = false, Feedback = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // execute_approved_schema — run DDL + create transactions
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("execute_approved_schema",
            "Execute the schema approved by propose_schema: create the physical database tables and AppTransaction hierarchy. " +
            "Call this ONLY after propose_schema returns {Confirmed:true}. Do NOT pass schemaJson — the schema is stored internally. " +
            "On partial failure (tables created but transactions failed), " +
            "all newly created tables are automatically dropped and the error is reported. " +
            "Returns {IsSuccess, TransactionId, TransactionName, TablesCreated[], LookupTables[], RolledBack[]}. " +
            "IMPORTANT: LookupTables[] contains standalone reference tables excluded from the master-detail hierarchy. " +
            "Their tables are ALREADY IN THE DATABASE — do not call propose_schema or execute_approved_schema again. " +
            "For each table in LookupTables call ONLY: " +
            "1) create_entity_from_table(tableName, saasApplicationId) " +
            "2) create_transaction_from_table(tableName, saasApplicationId) " +
            "3) create_list_edit_form(transactionId) " +
            "4) create_search_view(transactionId) — to give it a navigation screen. " +
            "propose_schema is called EXACTLY ONCE per application — never call it again after this point.")]
        public async Task<string> ExecuteApprovedSchema(
            [AgentParam("The SaasApplicationId returned by create_app_package. Required to link transactions to the correct app.")]
            int? saasApplicationId = null,
            [AgentParam("Display name for the root transaction, e.g. 'Sales Order Management'.")]
            string transactionName = null)
        {
            try
            {
                // Use the schema stored by propose_schema — avoids LLM JSON-escaping corruption
                var schemaJson = _approvedSchemaJson;
                if (string.IsNullOrWhiteSpace(schemaJson))
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "No approved schema found. Call propose_schema first." });

                SchemaExtractionResultDto schema;
                try
                {
                    schema = JsonConvert.DeserializeObject<SchemaExtractionResultDto>(schemaJson);
                }
                catch (Exception ex)
                {
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "Invalid SchemaJson: " + ex.Message });
                }

                if (schema?.Tables == null || schema.Tables.Count == 0)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "SchemaJson contains no tables." });

                var request = new DbGenieCreateTransactionRequestDto
                {
                    DataSourceRegisterId = _dataSourceId,
                    SchemaOwner          = _schemaOwner,
                    TransactionName      = transactionName,
                    SaasApplicationId    = saasApplicationId
                };

                var tableNames = schema.Tables.Select(t => t.Name).ToList();

                var result = await AppDbGenieBL.CreateHierarchyFromApprovedSchemaAsync(schema, request)
                    .ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    // ── Auto-rollback: drop any tables that were physically created ──
                    var rolledBack = new List<string>();
                    var rollbackFailed = new List<string>();

                    foreach (var tableName in tableNames)
                    {
                        try
                        {
                            AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                            {
                                SQL = $"IF OBJECT_ID('[{_schemaOwner}].[{tableName}]', 'U') IS NOT NULL DROP TABLE [{_schemaOwner}].[{tableName}]",
                                DataSourceRegisterId = _dataSourceId,
                                RequireConfirmation  = false,
                                IsConfirmed          = true
                            });
                            rolledBack.Add(tableName);
                        }
                        catch
                        {
                            rollbackFailed.Add(tableName);
                        }
                    }

                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess      = false,
                        Error          = result.Error,
                        RolledBack     = rolledBack,
                        RollbackFailed = rollbackFailed.Count > 0 ? rollbackFailed : null,
                        Note           = rolledBack.Count > 0
                            ? $"Partial failure: {rolledBack.Count} table(s) were dropped during rollback."
                            : "No tables were created before the failure.",
                        RecoveryInstruction = "CRITICAL: Do NOT call propose_schema again — the schema is stored internally. Fix the error above and retry by calling execute_approved_schema again (no schemaJson parameter)."
                    }, Formatting.Indented);
                }

                var lookupTables = result.LookupTables ?? new System.Collections.Generic.List<string>();

                // Build FK → lookup-table mappings so the agent knows which fields to wire as dropdowns.
                // For each MANY_TO_ONE relationship pointing at a lookup table, record:
                //   LookupTable (the entity source table), FKTable (the table that holds the FK column),
                //   FKField (the column to set as DDL), MasterTransactionId (the transaction to call set_field_entity on).
                var lookupSet   = new System.Collections.Generic.HashSet<string>(lookupTables, StringComparer.OrdinalIgnoreCase);
                var fkMappings  = new System.Collections.Generic.List<object>();
                foreach (var t in schema.Tables)
                {
                    foreach (var rel in t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                    {
                        if (!string.Equals(rel.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.IsNullOrWhiteSpace(rel.TargetTable) || !lookupSet.Contains(rel.TargetTable)) continue;
                        if (string.IsNullOrWhiteSpace(rel.ForeignKeyColumn)) continue;

                        fkMappings.Add(new
                        {
                            LookupTable         = rel.TargetTable,
                            FKTable             = t.Name,
                            FKField             = rel.ForeignKeyColumn,
                            MasterTransactionId = result.TransactionId   // FK lives in the master/detail hierarchy
                        });
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess       = true,
                    TransactionId   = result.TransactionId,
                    TransactionName = result.TransactionName,
                    TablesCreated   = tableNames,
                    LookupTables    = lookupTables,
                    FKMappings      = fkMappings.Count > 0 ? fkMappings : null,
                    NextStep        = lookupTables.Count > 0
                        ? $"Lookup tables already exist in DB: {string.Join(", ", lookupTables)}. " +
                          "DO NOT call propose_schema or execute_approved_schema again — tables are already created. " +
                          "For each lookup table:\n" +
                          "  1) create_entity_from_table → note the returned entityId\n" +
                          "  2) For each matching entry in FKMappings, call set_field_entity(transactionId=MasterTransactionId, fieldName=FKField, unitName=FKTable, entityId=<returned entityId>)\n" +
                          "  3) create_transaction_from_table\n" +
                          "  4) create_list_edit_form  ← this also adds the nav menu entry automatically, do NOT call create_search_view"
                        : null,
                    Script          = result.CreatedTableScripts?.Length > 500
                        ? result.CreatedTableScripts.Substring(0, 500) + "..."
                        : result.CreatedTableScripts
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = false,
                    Error = ex.Message,
                    RecoveryInstruction = "CRITICAL: Do NOT call propose_schema again — the schema is stored internally. Call execute_approved_schema again to retry."
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: build AgentSchemaEvent from extraction result
        // ─────────────────────────────────────────────────────────────────────

        private static AgentSchemaEvent BuildSchemaEvent(SchemaExtractionResultDto extraction, string script)
        {
            // t.IsLookup is already computed by ProposeSchema (Step 1c) with FK direction validation.
            // Parent map: for each composition child, find its parent (same two-guard logic as Step 1c).
            var columnsByTable = extraction.Tables.ToDictionary(
                t => t.Name,
                t => new HashSet<string>(
                    (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>()).Select(c => c.Name),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            var pksByTable = extraction.Tables.ToDictionary(
                t => t.Name,
                t => new HashSet<string>(
                    (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                        .Where(c => c.IsPrimaryKey).Select(c => c.Name),
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

            var parentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in extraction.Tables)
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
                    // Guard 1: source has MANY_TO_ONE to same target → inverted
                    if (manyToOneTargets.Contains(rel.TargetTable))
                        continue;
                    // Guard 2: FK column in source non-PK → inverted
                    if (!string.IsNullOrWhiteSpace(rel.ForeignKeyColumn) &&
                        srcCols != null && srcCols.Contains(rel.ForeignKeyColumn) &&
                        (srcPks == null || !srcPks.Contains(rel.ForeignKeyColumn)))
                        continue;
                    parentMap[rel.TargetTable] = t.Name;
                }
            }

            var tables = extraction.Tables.Select(t =>
            {
                var isLookup = t.IsLookup;

                // For this table identify MANY_TO_ONE relations (FK columns pointing outward)
                var lookupFkMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rel in t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                    if (string.Equals(rel.Type, "MANY_TO_ONE", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(rel.ForeignKeyColumn))
                        lookupFkMap[rel.ForeignKeyColumn] = rel.TargetTable;

                // ONE_TO_MANY from this table → composition child FK columns in the CHILD table (not this one)
                var compositionFkTargets = new HashSet<string>(
                    (t.Relationships ?? Enumerable.Empty<DbGenieRelationshipMetadataDto>())
                        .Where(r => string.Equals(r.Type, "ONE_TO_MANY", StringComparison.OrdinalIgnoreCase))
                        .Select(r => r.TargetTable),
                    StringComparer.OrdinalIgnoreCase);

                var columns = (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>()).Select(c =>
                {
                    lookupFkMap.TryGetValue(c.Name, out var fkTarget);
                    return new AgentSchemaColumn
                    {
                        Name             = c.Name,
                        DataType         = c.DataType,
                        IsPrimaryKey     = c.IsPrimaryKey,
                        IsNullable       = c.IsNullable,
                        IsAutoIncrement  = c.IsAutoIncrement,
                        Length           = c.Length,
                        DefaultValue     = c.DefaultValue,
                        Description      = c.Description,
                        FKTargetTable    = fkTarget,
                        RelationshipType = fkTarget != null ? "MANY_TO_ONE (DDL dropdown)" : null
                    };
                }).ToList();

                parentMap.TryGetValue(t.Name, out var parent);

                return new AgentSchemaTable
                {
                    Name        = t.Name,
                    Description = t.Description,
                    IsLookup    = isLookup,
                    ParentTable = parent,
                    Columns     = columns
                };
            }).ToList();

            return new AgentSchemaEvent
            {
                Summary      = $"Schema extracted: {extraction.Tables.Count} table(s). Review and approve or edit before building.",
                SchemaJson   = JsonConvert.SerializeObject(extraction),
                Tables       = tables,
                CreateScript = script
            };
        }
    }
}
