using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using Newtonsoft.Json;
using APP.Framework.Plugin;

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

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("propose_schema",
            "Extract a database schema from natural-language requirements and present it to the user " +
            "for review and optional inline editing BEFORE any DDL is executed. " +
            "The user will see each table with its columns, data types, FK relationships, and a CREATE TABLE preview. " +
            "They can rename columns, change types, remove unwanted columns, or reject with feedback. " +
            "Returns {Confirmed:true, TableCount, Tables:[...names...], UserFacingMessage:'...'} when the user approves — the schema is stored internally. " +
            "Returns {Confirmed:false, Feedback:'...'} when the user rejects — adjust requirements and call again. " +
            "IMPORTANT: Call this EXACTLY ONCE per new application. " +
            "When it returns {Confirmed:true}: (1) FIRST send the UserFacingMessage to the user verbatim as your reply so they know what happens next; (2) THEN call execute_approved_schema with NO schemaJson argument — the schema is already stored. " +
            "execute_approved_schema does NOT need a separate propose_plan call; propose_schema already served as the approval gate.")]
        public async Task<string> ProposeSchema(
            [AgentParam("Detailed natural-language description of the entities, fields, and relationships to build.", isRequired: true)]
            string requirements,
            [AgentParam("Application name used to prefix all table names, e.g. 'SalesOrder'. Max 10 chars, letters/numbers only. Pass unprefixed logical table names in requirements — the platform prefixes once.", isRequired: true)]
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
                        Confirmed         = true,
                        TableCount        = prior?.Tables?.Count ?? 0,
                        Tables            = prior?.Tables?.Select(t => t.Name).ToList() ?? new System.Collections.Generic.List<string>(),
                        UserFacingMessage = "Schema was already approved. Resuming application build now...",
                        NextStep          = "Schema was already approved earlier in this session — schema is stored internally. Call execute_approved_schema immediately. Do NOT call propose_schema again.",
                        Note              = "Returning previously approved schema (propose_schema was already confirmed)."
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

                // ── Step 1b: Idempotent app-name prefix (skip if name already prefixed) ──
                SchemaProposeHelpers.ApplyAppNamePrefix(extraction.Tables, appName);

                // ── Step 1c: Mark lookup vs composition (parents ∪ children on ONE_TO_MANY) ──
                SchemaProposeHelpers.MarkLookupVsComposition(extraction.Tables);

                // ── Step 2: Generate CREATE TABLE script preview ───────────────
                var script = AppDbGenieBL.GenerateCreateTableScripts(extraction.Tables, _schemaOwner);
                extraction.GeneratedScript = script;

                // ── Step 3: Build AgentSchemaEvent for frontend display ────────
                var schemaEvent = BuildSchemaEvent(extraction, script);

                // ── Step 4: Present to user and wait for response ─────────────
                if (_onSchemaReady == null)
                {
                    // Auto-approve when no callback wired (unit tests / Deterministic).
                    // MUST still store schema — execute_approved_schema reads _approvedSchemaJson only.
                    _approvedSchemaJson = JsonConvert.SerializeObject(extraction);
                    return JsonConvert.SerializeObject(new
                    {
                        Confirmed  = true,
                        TableCount = extraction.Tables.Count,
                        Tables     = extraction.Tables.Select(t => t.Name).ToList(),
                        Note       = "Auto-approved (no schema confirmation callback wired). Schema stored internally — call execute_approved_schema next without schemaJson.",
                        NextStep   = "Call execute_approved_schema immediately. Do NOT pass schemaJson — the schema is stored internally."
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

                var tableList = string.Join(", ", extraction.Tables.Select(t => t.Name));
                return JsonConvert.SerializeObject(new
                {
                    Confirmed          = true,
                    TableCount         = extraction.Tables.Count,
                    Tables             = extraction.Tables.Select(t => t.Name).ToList(),
                    UserFacingMessage  = $"Schema approved! I'll now build {extraction.Tables.Count} table(s): {tableList}.\n\nBuilding your application — this will take a moment...",
                    NextStep           = "Send the UserFacingMessage to the user verbatim first, then call execute_approved_schema immediately. Do NOT pass schemaJson — the schema is stored internally."
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

        // Legacy: Generic Agent reads tool description from AppAgentToolRegister DB; this attribute is used by AppBuilderAgentBL only.
        [AgentTool("execute_approved_schema",
            "Execute the schema approved by propose_schema: create the physical database tables and AppTransaction hierarchy. " +
            "Call this ONLY after propose_schema returns {Confirmed:true}. Do NOT pass schemaJson — the schema is stored internally. " +
            "Create SimpleList / entity data sources BEFORE this call, then pass entityMapJson to wire dropdowns in one step. " +
            "entityMapJson keys: ColumnName, TableName.ColumnName, UnitName.ColumnName, or field:{fieldId}. " +
            "Returns TransactionId, TablesCreated, LookupTables, FKMappings, Fields, TableColumns, EntityWiring. " +
            "Use returned Fields/TableColumns — do NOT call get_transaction_details or get_table_schema for greenfield mockup/wiring. " +
            "For each LookupTables entry: create_entity_from_table → set_field via FKMappings → create_transaction_from_table → create_list_edit_form (no create_search_view on lookups). " +
            "Then create_search_view on the MASTER TransactionId only.")]
        public async Task<string> ExecuteApprovedSchema(
            [AgentParam("The SaasApplicationId returned by create_app_package. Required to link transactions to the correct app.")]
            int? saasApplicationId = null,
            [AgentParam("Display name for the root transaction, e.g. 'Sales Order Management'.")]
            string transactionName = null,
            [AgentParam("JSON mapping fields to EntityDataSource IDs. Keys: ColumnName, TableName.ColumnName, UnitName.ColumnName, or field:{fieldId}. Example: {\"Ord_Order.Status\":4577,\"Category\":4578,\"ProductId\":4580}.")]
            string entityMapJson = null)
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
                    return FailExecuteAndClear("Invalid SchemaJson: " + ex.Message);
                }

                if (schema?.Tables == null || schema.Tables.Count == 0)
                    return FailExecuteAndClear("SchemaJson contains no tables.");

                var preflight = SchemaProposeHelpers.ValidateSchemaForDdl(schema.Tables);
                if (preflight.Count > 0)
                {
                    return FailExecuteAndClear(
                        "Schema preflight failed: " + string.Join(" ", preflight),
                        extra: new { PreflightErrors = preflight });
                }

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

                    // Clear stored schema so propose_schema can re-extract a fixed design.
                    _approvedSchemaJson = null;

                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess      = false,
                        Error          = result.Error,
                        RolledBack     = rolledBack,
                        RollbackFailed = rollbackFailed.Count > 0 ? rollbackFailed : null,
                        Note           = rolledBack.Count > 0
                            ? $"Partial failure: {rolledBack.Count} table(s) were dropped during rollback. Approved schema cleared."
                            : "No tables were created before the failure. Approved schema cleared.",
                        SchemaCleared  = true,
                        RecoveryInstruction = ExecuteFailureRecoveryInstruction
                    }, Formatting.Indented);
                }

                var lookupTables = result.LookupTables ?? new System.Collections.Generic.List<string>();

                // Build FK → lookup-table mappings so the agent knows which fields to wire as dropdowns.
                var lookupSet   = new HashSet<string>(lookupTables, StringComparer.OrdinalIgnoreCase);
                var fkMappings  = new List<object>();
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
                            MasterTransactionId = result.TransactionId
                        });
                    }
                }

                // Wire SimpleList / entity dropdowns when agent created entities before execute.
                var entityWireResults = EntityMapWiring.WireFromJson(
                    result.TransactionId, entityMapJson, out _);

                // Field + column snapshots so the agent skips get_transaction_details / get_table_schema.
                var fields = EntityMapWiring.LoadFields(result.TransactionId);
                var units = fields
                    .GroupBy(f => new { f.TableName, f.UnitName })
                    .Select(g => new
                    {
                        TableName = g.Key.TableName,
                        UnitName  = g.Key.UnitName,
                        FieldCount = g.Count()
                    })
                    .ToList();
                var fieldPayload = fields.Select(f => new
                {
                    f.FieldId,
                    f.ColumnName,
                    f.UnitName,
                    f.TableName,
                    f.EntityId,
                    f.ControlType
                }).ToList();
                var tableColumns = schema.Tables.Select(t => new
                {
                    TableName = t.Name,
                    Columns = (t.Columns ?? Enumerable.Empty<DbGenieColumnMetadataDto>())
                        .Select(c => new { c.Name, c.DataType, c.IsPrimaryKey, c.IsNullable })
                        .ToList()
                }).ToList();

                var unboundEnums = fields
                    .Where(f => !f.EntityId.HasValue || f.EntityId.Value <= 0)
                    .Where(f =>
                    {
                        var n = f.ColumnName ?? "";
                        return n.EndsWith("Status", StringComparison.OrdinalIgnoreCase)
                            || n.EndsWith("Type", StringComparison.OrdinalIgnoreCase)
                            || n.Equals("Category", StringComparison.OrdinalIgnoreCase)
                            || n.EndsWith("Category", StringComparison.OrdinalIgnoreCase);
                    })
                    .Select(f => $"field:{f.FieldId} ({f.TableName}.{f.ColumnName})")
                    .ToList();

                string nextStep;
                if (lookupTables.Count > 0)
                {
                    nextStep =
                        $"Lookup tables already exist in DB: {string.Join(", ", lookupTables)}. " +
                        "DO NOT call propose_schema or execute_approved_schema again. " +
                        "For each lookup table: create_entity_from_table → wire FKMappings with set_field_entity(fieldId=…) → " +
                        "create_transaction_from_table → create_list_edit_form (do NOT create_search_view on lookups). " +
                        $"Then create_search_view(transactionId={result.TransactionId}) for the MASTER transaction. " +
                        "Use returned Fields/TableColumns — do NOT call get_transaction_details or get_table_schema.";
                }
                else
                {
                    nextStep =
                        $"Tables and master-detail transaction created (TransactionId={result.TransactionId}). " +
                        "1) If EntityWiring left any enum/FK unbound, call set_field_entity with fieldId from Fields[] for EVERY unbound dropdown. " +
                        (unboundEnums.Count > 0
                            ? $"Unbound candidates: {string.Join(", ", unboundEnums)}. "
                            : "") +
                        $"2) create_search_view(transactionId={result.TransactionId}). " +
                        "3) insert_mockup_data using TableColumns (do NOT call get_table_schema). " +
                        "Do NOT call get_transaction_details, get_existing_transactions, or list_applications.";
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess       = true,
                    TransactionId   = result.TransactionId,
                    TransactionName = result.TransactionName,
                    TablesCreated   = tableNames,
                    LookupTables    = lookupTables,
                    FKMappings      = fkMappings.Count > 0 ? fkMappings : null,
                    Units           = units,
                    Fields          = fieldPayload,
                    TableColumns    = tableColumns,
                    EntityWiring    = entityWireResults.Count > 0 ? entityWireResults : null,
                    NextStep        = nextStep,
                    Script          = result.CreatedTableScripts?.Length > 500
                        ? result.CreatedTableScripts.Substring(0, 500) + "..."
                        : result.CreatedTableScripts
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return FailExecuteAndClear(ex.Message);
            }
        }

        private const string ExecuteFailureRecoveryInstruction =
            "Approved schema was CLEARED. Fix the Error (exact PK/FK column names on the correct tables), " +
            "then call propose_schema AGAIN with revised requirements (user will re-confirm), " +
            "then execute_approved_schema with the same entityMapJson. " +
            "Do NOT call create_application on the first failure — only after two failed propose/execute cycles or if the user explicitly skips schema review.";

        private string FailExecuteAndClear(string error, object extra = null)
        {
            _approvedSchemaJson = null;
            var payload = new Dictionary<string, object>
            {
                ["IsSuccess"] = false,
                ["Error"] = error,
                ["SchemaCleared"] = true,
                ["RecoveryInstruction"] = ExecuteFailureRecoveryInstruction
            };
            if (extra != null)
            {
                foreach (var p in extra.GetType().GetProperties())
                    payload[p.Name] = p.GetValue(extra);
            }
            return JsonConvert.SerializeObject(payload, Formatting.Indented);
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
