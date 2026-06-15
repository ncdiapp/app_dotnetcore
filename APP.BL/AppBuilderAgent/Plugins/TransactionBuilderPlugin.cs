using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using App.BL.DbGenie;
using APP.Components.EntityDto;
using DatabaseSchemaMrg;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    public class TransactionBuilderPlugin
    {
        private readonly int?   _dataSourceId;
        private readonly string _schemaOwner;

        public TransactionBuilderPlugin(int? dataSourceId = null, string schemaOwner = "dbo")
        {
            _dataSourceId = dataSourceId;
            _schemaOwner  = schemaOwner ?? "dbo";
        }

        [AgentFunction("create_application",
            "Step 2+4 — Build the database schema and transaction hierarchy from natural language requirements. " +
            "Runs the full pipeline: AI extracts schema, creates physical tables, " +
            "then creates the AppTransaction hierarchy with forms. " +
            "PREFER propose_schema + execute_approved_schema for new builds — they let the user review the schema first. " +
            "Use create_application only when skipping schema review is intentional. " +
            "Call AFTER create_app_package and AFTER any create_entity_* calls. " +
            "Pass entityMapJson to wire FK columns to entity dropdowns at creation time (eliminates the post-hoc set_field_entity pass). " +
            "On partial failure (tables created but transactions failed) all newly created tables are automatically rolled back. " +
            "Returns created table names, TransactionId(s), and success/error status.")]
        public async Task<string> CreateApplication(
            [AgentParam("Detailed natural-language description of what to build, including entity names, fields, and relationships.", isRequired: true)]
            string requirements,
            [AgentParam("The SaasApplicationId returned by create_app_package. Required to link the transaction to the correct application.")]
            int? saasApplicationId = null,
            [AgentParam("Optional descriptive name for the root transaction, e.g. 'Sales Order Management'")]
            string appName = null,
            [AgentParam("JSON object mapping FK column names to EntityDataSource IDs — wires dropdowns at creation time. " +
                        "Format: {\"ProductId\":42,\"CategoryId\":15,\"StatusId\":8}. " +
                        "Obtain entity IDs from list_entity_data_sources. " +
                        "This eliminates the need for separate set_field_entity calls after build.")]
            string entityMapJson = null)
        {
            try
            {
                var request = new DbGenieCreateTransactionRequestDto
                {
                    RequirementsText     = requirements,
                    DataSourceRegisterId = _dataSourceId,
                    SchemaOwner          = _schemaOwner,
                    TransactionName      = appName,
                    SaasApplicationId    = saasApplicationId
                };

                var result = await AppDbGenieBL.CreateHierarchyTransactionFromRequirementsAsync(request)
                    .ConfigureAwait(false);

                // ── Auto-rollback on partial failure ─────────────────────────────
                // If extraction succeeded (tables list is known) but a later step failed,
                // the physical tables may already exist — drop them so the state is clean.
                if (!result.IsSuccess && result.SchemaExtraction?.Tables?.Count > 0)
                {
                    var rolledBack     = new List<string>();
                    var rollbackFailed = new List<string>();

                    foreach (var table in result.SchemaExtraction.Tables)
                    {
                        try
                        {
                            AppDbGenieBL.ExecuteSQL(new ExecuteSQLRequestDto
                            {
                                SQL = $"IF OBJECT_ID('[{_schemaOwner}].[{table.Name}]', 'U') IS NOT NULL DROP TABLE [{_schemaOwner}].[{table.Name}]",
                                DataSourceRegisterId = _dataSourceId,
                                RequireConfirmation  = false,
                                IsConfirmed          = true
                            });
                            rolledBack.Add(table.Name);
                        }
                        catch { rollbackFailed.Add(table.Name); }
                    }

                    return JsonConvert.SerializeObject(new
                    {
                        IsSuccess      = false,
                        Error          = result.Error,
                        RolledBack     = rolledBack,
                        RollbackFailed = rollbackFailed.Count > 0 ? rollbackFailed : null,
                        Note           = rolledBack.Count > 0
                            ? $"Partial failure auto-rolled back {rolledBack.Count} table(s). Retry from the beginning."
                            : "No tables were created before the failure — safe to retry."
                    }, Formatting.Indented);
                }

                // ── Entity map wiring (eliminates post-hoc set_field_entity calls) ──
                var entityWireResults = new List<object>();
                if (result.IsSuccess && !string.IsNullOrWhiteSpace(entityMapJson) && result.TransactionId > 0)
                {
                    try
                    {
                        var entityMap = Newtonsoft.Json.Linq.JObject.Parse(entityMapJson);
                        entityWireResults = WireEntityMap(result.TransactionId, entityMap);
                    }
                    catch (Exception ex)
                    {
                        entityWireResults.Add(new { Warning = "entityMapJson parse failed: " + ex.Message });
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    result.IsSuccess,
                    result.Error,
                    result.TransactionId,
                    result.TransactionName,
                    TablesCreated = result.SchemaExtraction?.Tables?
                        .Select(t => new { t.Name, t.Description }).ToList(),
                    EntityWiring = entityWireResults.Count > 0 ? entityWireResults : null,
                    Script = result.CreatedTableScripts?.Length > 500
                        ? result.CreatedTableScripts.Substring(0, 500) + "..."
                        : result.CreatedTableScripts
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message, StackTrace = ex.ToString() });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: wire FK columns → entity IDs across all units of a transaction
        // ─────────────────────────────────────────────────────────────────────

        private List<object> WireEntityMap(int transactionId, Newtonsoft.Json.Linq.JObject entityMap)
        {
            var results = new List<object>();

            AppCacheManagerBL.RefreshOnetHierarchyTranscation(transactionId);
            var transExDto = AppTransactionBL.GetHierarchyTranscationFromDatabase(transactionId);
            if (transExDto == null) return results;

            var allFields = (transExDto.AppTransactionUnitList ?? Enumerable.Empty<AppTransactionUnitExDto>())
                .SelectMany(u => u.AppTransactionFieldList ?? Enumerable.Empty<AppTransactionFieldExDto>())
                .ToList();

            foreach (var prop in entityMap.Properties())
            {
                var columnName = prop.Name;
                var entityId   = (int?)prop.Value;
                if (!entityId.HasValue) continue;

                var matches = allFields.Where(f =>
                    string.Equals(f.DataBaseFieldName, columnName, StringComparison.OrdinalIgnoreCase)).ToList();

                foreach (var fieldSummary in matches)
                {
                    try
                    {
                        var field = AppTransactionBL.RetrieveOneAppTransactionFieldExDto(Convert.ToInt32(fieldSummary.Id));
                        if (field == null) continue;
                        field.IsModified  = true;
                        field.EntityId    = entityId;
                        field.ControlType = 1; // DDL
                        var saveResult = AppTransactionBL.SaveAppTransactionFieldExDto(field);
                        bool ok = saveResult?.ValidationResult?.HasErrors == false;
                        results.Add(new { Column = columnName, EntityId = entityId, FieldId = Convert.ToInt32(field.Id), Wired = ok });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new { Column = columnName, EntityId = entityId, Error = ex.Message });
                    }
                }
            }

            return results;
        }

        [AgentFunction("create_search_view",
            "Generate a default search/list navigation view for an existing transaction. " +
            "Call this after create_application for each main transaction.")]
        public string CreateSearchView(
            [AgentParam("The transaction ID to create the search view for", isRequired: true)]
            int transactionId)
        {
            try
            {
                var result = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(transactionId);
                if (result == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "Transaction not found" });

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess = result.IsSuccessful,
                    Error     = result.ValidationResult?.Items?.FirstOrDefault()?.Message
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentFunction("create_transaction_from_table",
            "Create a single AppTransaction (data model + form) from a PRE-EXISTING database table. " +
            "WARNING: ONLY use this for tables that existed BEFORE your current session (discovered via explore_platform). " +
            "NEVER call this after create_application — create_application already creates all transactions. " +
            "The table must already exist in the database.")]
        public string CreateTransactionFromTable(
            [AgentParam("Name of the existing database table", isRequired: true)] string tableName,
            [AgentParam("The SaasApplicationId returned by create_app_package, to link this transaction to the correct app.")] int? saasApplicationId = null,
            [AgentParam("Schema owner, e.g. 'dbo'. Leave empty to use the data source default.")] string schemaOwner = null)
        {
            try
            {
                var owner = schemaOwner ?? _schemaOwner;

                // Refresh the in-memory schema cache so newly created tables are visible
                if (_dataSourceId.HasValue)
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(_dataSourceId);

                var result = AppTransactionBL.CreateDefaultListTransactionFromTableName(
                    tableName, _dataSourceId, owner, saasApplicationId);

                if (result == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "Null result from BL" });

                if (result.Object == null)
                {
                    var messages = result.ValidationResult?.Items?
                        .Select(i => i.Message).ToList();
                    var errorDetail = (messages != null && messages.Count > 0)
                        ? string.Join("; ", messages)
                        : "Transaction was not created — table may not exist or already has a transaction";
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = errorDetail });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess       = true,
                    TransactionId   = result.Object.Id,
                    TransactionName = result.Object.TransactionName,
                    TableName       = tableName
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        [AgentFunction("create_hierarchy_from_tables",
            "RECOVERY TOOL — Create an AppTransaction hierarchy from tables that already exist in the database. " +
            "Use this when create_application failed AFTER the tables were physically created " +
            "(i.e. you can see the tables in explore_platform or check_table_exists confirms they exist). " +
            "Provide the master (root) table name, child table names as a comma-separated list, " +
            "and optionally grandChildMapJson to specify grandchild tables per child. " +
            "FK relationships are auto-detected from the database schema.")]
        public string CreateHierarchyFromTables(
            [AgentParam("Name of the root/master table (the top-level parent)", isRequired: true)]
            string masterTableName,
            [AgentParam("Comma-separated names of child tables that belong under the master, e.g. 'OrderLine,OrderPayment'")]
            string childTableNames = null,
            [AgentParam("The SaasApplicationId to link this transaction to the correct application")]
            int? saasApplicationId = null,
            [AgentParam("Display name for the transaction, e.g. 'Sales Order Management'. Defaults to master table name.")]
            string transactionName = null,
            [AgentParam("Schema owner, e.g. 'dbo'")]
            string schemaOwner = null,
            [AgentParam("JSON object mapping each child table name to a list of its grandchild table names. " +
                        "Format: {ChildTable:[GrandChild1,GrandChild2]}. " +
                        "Only needed when 3-level hierarchy (grandchild tables) exists.")]
            string grandChildMapJson = null)
        {
            try
            {
                var owner = schemaOwner ?? _schemaOwner;

                // Parse grandchild map once so we can apply it below
                Dictionary<string, List<string>> grandChildMap = null;
                if (!string.IsNullOrWhiteSpace(grandChildMapJson))
                {
                    try
                    {
                        grandChildMap = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(grandChildMapJson);
                    }
                    catch
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error     = "grandChildMapJson is not valid JSON. Expected format: {\"ChildTable\":[\"GrandChild1\",\"GrandChild2\"]}"
                        });
                    }
                }

                var childList = new List<APP.Components.EntityDto.HierarchyChildTableDto>();
                if (!string.IsNullOrWhiteSpace(childTableNames))
                {
                    foreach (var name in childTableNames.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = name.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;

                        List<string> grandChildren = null;
                        grandChildMap?.TryGetValue(trimmed, out grandChildren);

                        childList.Add(new APP.Components.EntityDto.HierarchyChildTableDto
                        {
                            TableName            = trimmed,
                            GrandChildTableNames = grandChildren
                        });
                    }
                }

                // ── Rule 1: exclude child/grandchild tables that are already registered as
                //    DatabaseTable Entity Data Sources — those are reference/lookup tables
                //    that must be used as DDL fields, not as structural child units.
                var entityLinkedTables = new HashSet<string>(
                    AppEntityInfoBL.RetrieveAllAppEntityInfoDto()
                        .Where(e => e.EntityType == 1 && !string.IsNullOrEmpty(e.TableName))
                        .Select(e => e.TableName),
                    StringComparer.OrdinalIgnoreCase);

                var excludedAsEntities = childList
                    .Where(c => entityLinkedTables.Contains(c.TableName))
                    .Select(c => c.TableName)
                    .ToList();

                if (excludedAsEntities.Count > 0)
                {
                    // Remove entity-linked tables from child list
                    childList = childList.Where(c => !entityLinkedTables.Contains(c.TableName)).ToList();

                    // Also remove from grandchild lists within remaining children
                    foreach (var child in childList)
                    {
                        if (child.GrandChildTableNames != null)
                            child.GrandChildTableNames = child.GrandChildTableNames
                                .Where(g => !entityLinkedTables.Contains(g))
                                .ToList();
                    }
                }

                // ── Rule 2: every remaining child/grandchild must have a DB foreign key to its parent.
                //    If any are missing, block creation so the agent knows what to fix.
                if (_dataSourceId.HasValue && childList.Count > 0)
                {
                    var fixture  = AppCacheManagerBL.GetOneDatabaseFixture(_dataSourceId.Value);
                    var fkErrors = new List<string>();

                    foreach (var child in childList)
                    {
                        if (!FkExists(fixture, child.TableName, masterTableName))
                            fkErrors.Add($"Child table '{child.TableName}' has no foreign key to parent table '{masterTableName}'.");

                        if (child.GrandChildTableNames != null)
                        {
                            foreach (var grandChild in child.GrandChildTableNames)
                            {
                                if (!FkExists(fixture, grandChild, child.TableName))
                                    fkErrors.Add($"Grandchild table '{grandChild}' has no foreign key to parent table '{child.TableName}'.");
                            }
                        }
                    }

                    if (fkErrors.Count > 0)
                        return JsonConvert.SerializeObject(new
                        {
                            IsSuccess = false,
                            Error = "Cannot create hierarchy — child unit(s) have no link key to their parent unit. " +
                                    string.Join(" ", fkErrors) +
                                    " Add FK columns referencing the parent table's primary key, then retry.",
                            ExcludedEntityTables = excludedAsEntities
                        });
                }

                var setupDto = new APP.Components.EntityDto.HierarchyTableSetupDto
                {
                    MasterTableName      = masterTableName,
                    ChildTables          = childList,
                    DataSourceRegisterId = _dataSourceId,
                    SchemaOwner          = owner,
                    TransactionName      = transactionName,
                    SaasApplicationId    = saasApplicationId
                };

                // Refresh the in-memory schema cache so newly created tables are visible
                if (_dataSourceId.HasValue)
                    AppCacheManagerBL.RefreshOneCustomerDbRegAndFixtureCache(_dataSourceId);

                var result = AppTransactionBL.CreateHierarchyTransactionFromTables(setupDto);

                if (result == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "Null result from BL" });

                if (result.Object == null)
                {
                    var messages = result.ValidationResult?.Items?
                        .Select(i => i.Message).ToList();
                    var errorDetail = messages != null && messages.Count > 0
                        ? string.Join("; ", messages)
                        : "Transaction was not created";
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = errorDetail });
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess            = true,
                    TransactionId        = result.Object.Id,
                    TransactionName      = result.Object.TransactionName,
                    MasterTable          = masterTableName,
                    ChildTables          = childList.Select(c => new
                    {
                        c.TableName,
                        GrandChildren = c.GrandChildTableNames ?? new List<string>()
                    }).ToList(),
                    ExcludedEntityTables = excludedAsEntities,
                    Note = excludedAsEntities.Count > 0
                        ? $"Tables [{string.Join(", ", excludedAsEntities)}] were excluded from child units because they are registered Entity Data Sources. " +
                          "Wire their FK columns in the master unit as DDL fields using set_field_entity."
                        : null
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // FK check helper — queries sys.foreign_keys to verify a FK exists from
        // childTable → parentTable.  Fails open so a query failure won't block.
        // ─────────────────────────────────────────────────────────────────────

        private static bool FkExists(DatabaseFixture fixture, string childTable, string parentTable)
        {
            try
            {
                const string sql = @"
                    SELECT COUNT(1) AS FkCount
                    FROM sys.foreign_keys fk
                    JOIN sys.tables child_t  ON fk.parent_object_id     = child_t.object_id
                    JOIN sys.tables parent_t ON fk.referenced_object_id = parent_t.object_id
                    WHERE child_t.name  = @ChildTable
                      AND parent_t.name = @ParentTable";

                var pChild  = fixture.CreateParameter("@ChildTable");  pChild.Value  = childTable;
                var pParent = fixture.CreateParameter("@ParentTable"); pParent.Value = parentTable;

                var dt = fixture.RetriveDataTable(sql, new List<DbParameter> { pChild, pParent });
                return dt != null && dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["FkCount"]) > 0;
            }
            catch
            {
                return true; // If we cannot query, allow through rather than false-blocking
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // create_list_edit_form — generate a MasterDetail edit form for a List transaction
        // ─────────────────────────────────────────────────────────────────────

        [AgentFunction("create_list_edit_form",
            "Create a MasterDetail edit form linked to an existing List-type transaction. " +
            "A List transaction (Data Model Type = 3. List) shows data in a grid. " +
            "Calling this tool generates the corresponding edit/create/delete form, " +
            "wires up the Create, Edit (Open), and Delete link-target actions automatically, " +
            "and adds the list transaction to the application's left navigation menu. " +
            "Call AFTER create_transaction_from_table or create_hierarchy_from_tables " +
            "when the resulting transaction is a List type. " +
            "Returns the new MasterDetail TransactionId and FormId on success. " +
            "NOTE: The nav menu entry is created automatically — do NOT call create_search_view separately for this transaction.")]
        public string CreateListEditForm(
            [AgentParam("TransactionId of the existing List-type transaction to add an edit form to.", isRequired: true)]
            int listTransactionId)
        {
            try
            {
                var result = AppTransactionBL.CreateDefaultMasterDetailTransaction(listTransactionId, false);

                if (result == null)
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = "Null result from BL" });

                if (result.Object == null)
                {
                    var messages = result.ValidationResult?.Items?
                        .Select(i => i.Message).ToList();
                    var errorDetail = messages != null && messages.Count > 0
                        ? string.Join("; ", messages)
                        : "Edit form was not created";
                    return JsonConvert.SerializeObject(new { IsSuccess = false, Error = errorDetail });
                }

                // Auto-add the list transaction to the app navigation menu so it appears
                // in the left sidebar below any previously created nav entries (e.g. the
                // master-detail transaction whose search view is created first).
                // QuickGenerateTransactionDefaultSeachNavigation is idempotent — it skips
                // creation if a nav entry for this search already exists.
                string navNote = null;
                try
                {
                    var navResult = AppDatabaseViewBL.QuickGenerateTransactionDefaultSeachNavigation(listTransactionId);
                    navNote = navResult?.IsSuccessful == true ? "Nav menu entry created." : "Nav menu entry skipped (may already exist).";
                }
                catch
                {
                    navNote = "Nav menu entry could not be created — call create_search_view manually if needed.";
                }

                return JsonConvert.SerializeObject(new
                {
                    IsSuccess            = true,
                    EditTransactionId    = result.Object.Id,
                    EditTransactionName  = result.Object.TransactionName,
                    FormId               = result.Object.FormId,
                    ListTransactionId    = listTransactionId,
                    NavMenuNote          = navNote
                }, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { IsSuccess = false, Error = ex.Message });
            }
        }
    }
}
