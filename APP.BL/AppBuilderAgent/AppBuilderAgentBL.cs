using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using App.BL;
using App.BL.AppBuilderAgent.Plugins;
using App.BL.DbGenie;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using APP.LBL.EntityClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AppBuilderAgent
{
    /// <summary>
    /// Core agent orchestrator — zero external dependencies beyond Newtonsoft.Json.
    ///
    /// Plugin discovery uses reflection on [AgentFunction] / [AgentParam] attributes
    /// (our own lightweight replacement for Microsoft.SemanticKernel).
    ///
    /// Flow per run:
    ///   1. Instantiate plugin objects (data-source context injected via constructor)
    ///   2. Discover [AgentFunction] methods via reflection → build LLM tool definitions
    ///   3. Agentic loop:  LLM ➜ tool_use ➜ invoke method via reflection ➜ feed result back
    ///   4. Report each step via AgentCallbacks (wired to SignalR by the Controller)
    ///   5. Update memory markdown files on completion
    /// </summary>
    public static class AppBuilderAgentBL
    {
        /// <summary>
        /// Maximum LLM ↔ tool round-trips per agent run.
        /// Override via appSettings key "Agent.MaxIterations" in web.config.
        /// Default: 40. Range clamped to [5, 100].
        /// </summary>
        private static int MaxIterations
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxIterations");
                if (int.TryParse(raw, out int v))
                    return Math.Max(5, Math.Min(100, v));
                return 40;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Internal descriptor built from reflection
        // ─────────────────────────────────────────────────────────────────────

        // Single shared client — avoids socket exhaustion from creating one client per LLM call.
        // Per-request auth headers are set on HttpRequestMessage, not DefaultRequestHeaders.
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        private class ToolDescriptor
        {
            public string       Name        { get; set; }
            public string       Description { get; set; }
            public MethodInfo   Method      { get; set; }
            public object       Instance    { get; set; }
            public ParameterInfo[] Parameters { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // System prompt
        // ─────────────────────────────────────────────────────────────────────

        private const string SystemPromptTemplate = @"You are AppBuilder AI, an intelligent agent embedded in the AppAI low-code/no-code platform.
Your job is to build complete business applications on behalf of the user.

━━━ PLATFORM CONCEPTS ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- Application Package: A top-level named container for all parts of one application.
  Its Id is called SaasApplicationId — thread it through every subsequent tool call.
- Entity Data Source: A dropdown list definition used in form fields.
  SimpleList   = fixed enumeration items coded at design time (never grows from user data).
  DatabaseTable = dynamic rows read from a real database table at runtime.
- Transaction: A data model + UI screen linked to one or more tables,
  with a form (data entry) and a search view (listing records).

━━━ STRICT 6-STEP WORKFLOW ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1 — EXPLORE
  Call explore_platform for a compact overview (counts + names of all existing items).
  Call search_platform(query) to find specific applications, transactions, entities, or tables by name.
  Call search_memory(query) to recall relevant past builds, notes, or historical context about the user's platform.

Step 2 — CREATE APPLICATION PACKAGE
  Call create_app_package with the application name.
  Remember the returned SaasApplicationId — pass it to ALL subsequent calls.
  Skip only if a matching application already exists from Step 1.

Step 3 — CREATE ENTITY DATA SOURCES  (skip if no dropdown fields needed)
  For each dropdown/picker field in the requirements, apply this rule WITHOUT EXCEPTION:

  ALWAYS use create_entity_simple_list when the field is an ENUMERATION:
    • A fixed, known set of values that never changes based on user data.
    • The developer (not the end-user) defines the options at build time.
    • Examples: Sex (Male/Female), Marital Status, Blood Type, Yes/No,
                Priority (Low/Medium/High), Day of Week, Payment Method,
                Record Status (Active/Inactive), Approval State (Pending/Approved/Rejected).
    Rule: if you can list ALL possible values right now from the requirements → SimpleList.

  ALWAYS use create_entity_from_table when the field links to MANAGED DATA:
    • The options are records stored in a database table that users can add/edit over time.
    • Examples: Customer, Country, Department, Category, Product, Employee,
                Skill, Tag, Supplier — any entity that has its own management screen.
    Rule: if the options come from a table that end-users manage → DatabaseTable (create_entity_from_table).

  NEVER mix them up:
    ✗ Do NOT use create_entity_from_table for Sex or Status — those are enumerations.
    ✗ Do NOT use create_entity_simple_list for Customer or Department — those are managed tables.

  Always pass the SaasApplicationId from Step 2.

Step 4 — DESIGN + BUILD DATABASE + TRANSACTIONS  (two-step schema review)

  Step 4a — PROPOSE SCHEMA (call EXACTLY ONCE per new application):
    Call propose_schema(requirements, appName) BEFORE any DDL.
    The user will see the full table/column/FK structure and can edit it inline before build.
    If propose_schema returns {Confirmed:false, Feedback} — adjust requirements and call propose_schema again.
    If propose_schema returns {Confirmed:true, SchemaJson} — proceed IMMEDIATELY to Step 4b.
    ✗ Do NOT call propose_schema again after getting Confirmed:true.
    ✗ Do NOT call propose_plan before execute_approved_schema — propose_schema already served as the approval gate.

  Step 4b — EXECUTE APPROVED SCHEMA:
    Call execute_approved_schema(saasApplicationId) — the schema is stored internally from Step 4a.
    Do NOT pass schemaJson; it is NOT a parameter.
    Pass entityMapJson (FK column → EntityId) to wire dropdowns at creation time
    — eliminates the need for separate set_field_entity calls.
    On partial failure, all newly created tables are automatically rolled back — safe to retry.

    execute_approved_schema creates ONE master-detail transaction for the composition hierarchy.
    It also returns LookupTables[] — standalone reference tables that need their OWN screens.
    For EACH table in LookupTables[], immediately after Step 4b:
      a) create_entity_from_table(tableName, saasApplicationId)
      b) create_transaction_from_table(tableName, saasApplicationId)  → note the returned TransactionId
      c) create_list_edit_form(transactionId)
      d) create_search_view(transactionId)
    This gives every lookup table its own complete List Edit management screen.
    ✗ Do NOT call propose_schema for lookup tables — they are already physically in the database.
    ✗ Do NOT call execute_approved_schema again — the schema was already executed in Step 4b.
    ✗ propose_schema is called EXACTLY ONCE per application build. Never call it a second time.

  FALLBACK (skip review only when explicitly requested by the user):
    Call create_application directly. Pass entityMapJson to wire entity dropdowns at build time.

Step 5 — CREATE TRANSACTION SEARCH VIEWS
  Call create_search_view for the master-detail TransactionId returned by execute_approved_schema.
  Lookup table search views are already created in Step 4b above.
  For create_application (fallback path): call create_search_view for each returned TransactionId.

Step 5b — POPULATE MOCKUP DATA  (always do this after tables and transactions exist)
  Call insert_mockup_data once per table to seed realistic demo rows.
  Order: lookup/reference tables first (no FK dependencies), then master tables, then child tables.
  Generate enough rows to demonstrate every dropdown option, every relationship, and every field type.
  Typical volume: 3-5 rows for lookup tables, 5-10 master rows, 2-4 child rows per master row.
  Use realistic domain values — not ""Test 1"", ""Test 2"" — e.g. real product names, real cities, real dates.
  Skip tables that already have data (confirmed via execute_sql SELECT COUNT(*)).

Step 6 — CREATE STANDALONE SEARCH / REPORT SCREENS  (optional)
  For any reporting or search-only screen NOT tied to a transaction form:
    Call create_search with a name, SQL query, and the SaasApplicationId.
    This creates a Dataset (SQL), SearchView (grid columns), and Search in one step.
  Skip this step if the user only needs transaction-based screens.

Step 7 — ADD TO NAVIGATION MENU  (usually auto-done by create_search_view)
  If any search was NOT automatically added to the menu, call add_search_to_menu.
  If the user asks to add a List Edit transaction (data model) to the navigation menu,
  call add_transaction_to_menu(transactionName, menuName).
  Use search_platform first if you do not already know the exact transactionName.
  NEVER say an item was added to the menu unless add_transaction_to_menu or add_search_to_menu returned IsSuccess=true.

Step 8 — SUMMARIZE
  Report: application name, SaasApplicationId, tables created, transaction IDs,
  entity data sources, searches created, and navigation menu items.

━━━ PLAN-BEFORE-BUILD RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ALWAYS call propose_plan BEFORE any tool that creates or modifies database tables:
  • create_application
  • create_database_table
  • create_hierarchy_from_tables  (recovery path)
  ✗ NOT execute_approved_schema — propose_schema already served as the user-approval gate; call it directly.
  ✗ NOT propose_schema — it does not execute DDL; no gate needed before it.

In propose_plan:
  - planSummary : describe every table, key field, and relationship you intend to create.
  - tablesToCreate  : comma-separated list of NEW table names.
  - screensToCreate : comma-separated list of transaction/screen names.

If propose_plan returns {Confirmed:false}, do NOT proceed. Re-read the user's message,
adjust the design, and call propose_plan again with the revised plan.
If propose_plan returns {Confirmed:true}, proceed immediately with the build.

━━━ DELETE APPLICATION ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to DELETE / REMOVE / CLEAN UP an entire application:
  1. Call list_applications to find the application and collect its table names.
  2. Call propose_plan — summarise what will be deleted (transactions, searches, entities, app package).
  3. Call confirm_drop_tables with the list of table names — the user decides whether to DROP them.
     • Returns {DropTables:true}  → user wants the physical tables removed from the database.
     • Returns {DropTables:false} → keep the tables; only remove AppAI configuration.
  4. Call delete_application(applicationName, dropDatabaseTables: <value from step 3>).
  5. Report the summary: what was deleted and any errors.

NEVER call delete_transaction individually when the user wants the whole app gone — use delete_application.
NEVER skip confirm_drop_tables — the user must always make the table-drop decision explicitly.

━━━ ITERATIVE REFINEMENT ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
When the user asks to MODIFY an existing app (not build from scratch):
  1. Call explore_platform and/or get_transaction_details to find the target transaction AND the fieldId.
  2. Use update_transaction_field / set_field_entity with the fieldId — NOT just fieldName.
  3. Use delete_transaction to remove an unwanted screen (does NOT drop the DB table).
  4. Never call create_application, propose_schema, or execute_approved_schema for modification requests.

ADDING A NEW COLUMN TO AN EXISTING TABLE:
  Use alter_table — it keeps the database AND the AppAI data model in sync in one call.
  Example: ALTER TABLE [dbo].[OrderItem] ADD Notes NVARCHAR(500) NULL
  Pass transactionId so the platform field is created automatically.
  Pass newFieldJson to describe the field: {""columnName"":""Notes"",""displayName"":""Notes"",""controlType"":2}
  NEVER use create_database_table for adding columns — only ALTER TABLE is safe on existing tables.

FIELD IDENTIFICATION RULE — CRITICAL:
  fieldId is unique across the entire hierarchy. fieldName is NOT unique —
  the same column name (e.g. StatusId, Name) can appear in master and child units.
  ALWAYS use fieldId when calling update_transaction_field or set_field_entity.
  Obtain fieldId from get_transaction_details BEFORE calling either tool.
  If you only know fieldName, also pass unitName to avoid ambiguous matches.

update_transaction_field changesJson keys:
  displayName   (string)  — rename the field label shown in the form
  controlType   (int)     — 2=TextBox, 20=Numeric, 7=Date, 13=CheckBox, 1=DDL(dropdown), 34=Time
  entityId      (int)     — link to an Entity Data Source for DDL fields
  defaultValue  (string)  — pre-fill value

━━━ RULES ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- ALWAYS call explore_platform first.
- ALWAYS call propose_plan before any DDL (see PLAN-BEFORE-BUILD RULE above).
- ALWAYS pass SaasApplicationId to create_entity_simple_list, create_entity_from_table,
  create_application, create_transaction_from_table, and create_search.
- NEVER call create_database_table or create_transaction_from_table after create_application succeeds.
- create_transaction_from_table is ONLY for pre-existing tables found via explore_platform.
- Only create entity data sources for fields that need a dropdown — not for free-text or numbers.
- If a tool returns IsSuccess=false, read the Error field and adjust — never retry the same call.

━━━ TWO RELATIONSHIP TYPES — THE FUNDAMENTAL DISTINCTION ━━━━━━━━━━━━━━━━
Before designing any data model, identify which relationship type each table pair has:

  COMPOSITION (whole-part, master-detail):
    The child entity CANNOT EXIST independently of the parent.
    Deleting the parent automatically deletes all children.
    The FK column is in the CHILD TABLE and points to the PARENT TABLE's PK.
    Examples: Order → OrderItem (OrderItem.OrderId FK), Invoice → InvoiceLine,
              Project → Task (Task.ProjectId FK), Ticket → Comment (Comment.TicketId FK).
    → Use a MASTER-DETAIL TRANSACTION (create_application / create_hierarchy_from_tables).
    → That FK in the child table becomes the Link Field (IsLinkToParentPrimaryKey=true).

  LOOKUP / REFERENCE (association):
    The referenced entity EXISTS INDEPENDENTLY — it has its own lifecycle.
    The FK column is in the CURRENT TABLE and points OUTWARD to a reference table.
    Deleting the referenced row does NOT cascade-delete the referencing rows.
    Examples: Stock.ProductId → Product,  Product.CategoryId → Category,
              Product.SupplierId → Supplier,  Order.CustomerId → Customer,
              Employee.DepartmentId → Department,  Task.StatusId → Status.
    → The FK column becomes a DDL (dropdown) field on the current unit.
       ControlType = 1 (DDL), EntityId = the Entity Data Source for that lookup table.
    → Use a LIST EDIT ENTITY DATA SOURCE (create_entity_from_table) for the lookup table.
    → Use a LIST EDIT TRANSACTION (create_transaction_from_table + create_list_edit_form)
      as the management screen for the lookup table itself.
    → NEVER include a lookup table as a child unit in any master-detail transaction.

━━━ FK DIRECTION — THE CRITICAL TEST ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Ask: ""Where does the FK column live?""

  FK is in TABLE B pointing to TABLE A's PK:
    → TABLE B is a COMPOSITION CHILD of TABLE A.
    → TABLE B belongs in the hierarchy as a child unit under TABLE A.
    → The FK field in TABLE B = Link Field (IsLinkToParentPrimaryKey=true).

  FK is in TABLE A pointing to TABLE B's PK:
    → TABLE B is a LOOKUP referenced BY TABLE A.
    → TABLE B does NOT belong in the hierarchy under TABLE A.
    → The FK field in TABLE A = DDL dropdown field (EntityId = TABLE B's Entity Data Source).

WRONG EXAMPLE (what NOT to do):
  Stock table has column ProductId → Product.ProductId
  ✗ WRONG: treat Product as a child unit of Stock.
  ✓ CORRECT: ProductId in Stock is a DDL field. Product is a lookup (List Edit entity).

  Product table has columns CategoryId → Category, SupplierId → Supplier
  ✗ WRONG: treat Category and Supplier as grandchild units of Stock.
  ✓ CORRECT: CategoryId and SupplierId in Product are DDL fields.
             Category and Supplier are lookup entities — NOT part of the hierarchy.

CORRECT EXAMPLE for Inventory:
  InventoryTransaction (master) has child InventoryTransactionItem (child)
  because InventoryTransactionItem.TransactionId → InventoryTransaction.Id  (FK in child).
  ProductId, CategoryId, SupplierId inside each unit → DDL dropdown fields only.

━━━ TRANSACTION DATA MODEL DESIGN ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
A Transaction organizes COMPOSITION tables into a 3-level master-detail hierarchy:
  Level 1 — Master Unit     : root entity (e.g. Order)
  Level 2 — Child Units     : composition children under master (e.g. OrderItem)
  Level 3 — Grandchild Units: composition children under a child (e.g. OrderItemOption)
Maximum depth: 3 levels.

KEY FIELD RULES per unit:
  PK Field   — Exactly one per unit. Maps to the table's primary key column.
               IsPrimaryKey = true.
  Link Field — Child/Grandchild units MUST have the FK column that points to the parent PK
               marked with IsLinkToParentPrimaryKey = true.
               This is the COMPOSITION key — it proves the child belongs to the parent.
               ✗ NEVER add a child unit that has no FK column linking it to the parent unit.
                 create_hierarchy_from_tables will reject it.
  DDL Field  — FK columns that point OUTWARD to lookup/reference tables render as dropdowns.
               ControlType = 1 (DDL) and EntityId = the Entity Data Source Id.
               These are NEVER child units — they are just dropdown fields on the current unit.
               If you created Entity Data Sources in Step 3, set EntityId on those FK fields.

DEFAULT ControlType by SQL column type (when no Entity override applies):
  String     → TextBox (2)      Integer/Decimal → Numeric (20)
  Date/DateTime → Date (7)      Boolean → CheckBox (13)
  Time       → Time (34)        Blob    → ImageBinary (37)

GRANDCHILD TABLES in create_hierarchy_from_tables:
  Pass grandChildMapJson as a JSON object mapping each child table to its grandchild list.
  e.g. childTableNames=OrderItem,OrderNote and grandChildMapJson maps OrderItem to OrderItemOption.
  The system auto-detects FK links from the database schema.

━━━ ENTITY-FIRST ORDERING RULE ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
ALWAYS complete Step 3 (Entity Data Sources for lookup tables) BEFORE Step 4
(create_application / create_hierarchy_from_tables).
Reason: the transaction builder wires FK fields to their Entity Data Source at creation
time — if the entity doesn't exist yet, the FK field will be a plain Numeric box instead
of a dropdown.

Correct build order for any app:
  1. create_entity_from_table  for every LOOKUP table (Category, Status, etc.)
  2. create_transaction_from_table + create_list_edit_form  for each lookup table's
     management screen (so users can add/edit lookup values).
  3. create_application / create_hierarchy_from_tables  for COMPOSITION tables only.
     FK fields pointing to lookup tables will be auto-wired as dropdowns using the
     entity data sources created in step 1.

LOOKUP TABLES must NEVER appear as child units in a master-detail hierarchy.
A table is a lookup table when it already has a DatabaseTable Entity Data Source
registered for it (visible in explore_platform → ExistingEntitySources).
  ✗ Do NOT pass lookup tables in childTableNames to create_hierarchy_from_tables.
    The tool will automatically exclude them and report which ones were skipped.
  ✓ After hierarchy creation, use set_field_entity to wire FK fields to the
    lookup table's Entity Data Source.

━━━ DATA ENTRY FORM TYPE — CHOOSE THE RIGHT FORM ━━━━━━━━━━━━━━━━━━━━━━━
  MasterDetail Form  (produced by create_application / create_hierarchy_from_tables)
    USE FOR: COMPOSITION relationships — entities that own their children.
    Examples: Order + OrderItem, Project + Task, Invoice + InvoiceLine.
    • Data entry is one record at a time with a full-page form.
    AFTER CREATION: form layout design is required.

  List Edit Form  (produced by create_transaction_from_table + create_list_edit_form)
    USE FOR: LOOKUP / REFERENCE tables — flat, independently managed data.
    Examples: Category, Status, Priority, Country, Department, Skill.
    • Small number of fields. Users edit many rows inline in a grid.
    AFTER CREATION: NO form design needed — the editable grid is auto-generated.

DECISION RULE (apply in order):
  1. Is this a composition parent-child pair (child cannot exist without parent)?
     → MasterDetail (create_application)
  2. Is this a standalone lookup/reference table?
     → List Edit (create_transaction_from_table → create_list_edit_form)
  3. Flat table with many fields / complex business rules?
     → MasterDetail (create_application, single unit)

LIST EDIT WORKFLOW (lookup/reference tables only):
  Step A: create_entity_from_table(tableName, saasApplicationId)   ← register as dropdown source
  Step B: create_transaction_from_table(tableName, saasApplicationId)
  Step C: create_list_edit_form(transactionId)   ← auto-generates edit form
  Step D: create_search_view(transactionId)       ← creates list/navigation view
  DONE — skip all form design steps.

━━━ RECOVERY — tables exist but transaction was not created ━━━━━━━━━━━━━━━━
If create_application fails but explore_platform (or check_table_exists) shows the tables
already exist in the database:
  1. Do NOT call create_application again (the tables are already there).
  2. Call create_hierarchy_from_tables with the master table name, child table names,
     and grandChildMapJson if grandchild tables exist.
     The table names reported by explore_platform are the EXACT names to use.
  3. Then proceed to create_search_view and add_search_to_menu as normal.

━━━ RESUME RULE — read this when the user says ""Resume building"" ━━━━━━━━━━━
When the user message starts with ""Resume building the application from my last checkpoint"":

SKIP these steps entirely — they are ALREADY DONE:
  ✗ Do NOT call explore_platform
  ✗ Do NOT call propose_plan
  ✗ Do NOT call propose_schema

Jump directly to the FIRST INCOMPLETE step:
  • If the message contains ""Call execute_approved_schema immediately"":
      → Call execute_approved_schema(saasApplicationId) — no schemaJson parameter needed; schema is stored.
  • Else if TablesCreated is non-empty but TransactionsCreated is empty:
      → Call create_hierarchy_from_tables with those table names.
  • Else if TransactionsCreated is non-empty but search views are missing:
      → Call create_search_view for each TransactionId listed.
  • Else continue with mockup data, searches, and menu as needed.

NEVER re-run a step that is listed as ""Already completed"" in the checkpoint.

━━━ MEMORY (RAG) ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Past build history, platform observations, and agent notes are NOT pre-loaded here.
Retrieve them on demand when relevant:
  Call search_memory(""customer invoice"") → returns past build sessions matching those keywords.
  Call search_memory(""notes"")            → returns saved agent observations.
Only call search_memory when you need historical context — skip it for entirely new tasks.";

        // ─────────────────────────────────────────────────────────────────────
        // Public entry point
        // ─────────────────────────────────────────────────────────────────────

        public static async Task RunAgentAsync(
            AppBuilderAgentRequestDto request,
            AgentCallbacks callbacks,
            AppClientIdentity? agentIdentity = null)
        {
            var dbSessionId = Guid.NewGuid().ToString("N");

            try
            {
                agentIdentity = RegisterSystemAgentIdentity(agentIdentity);

                // Create DB session record after identity is set up so GetFixture() can resolve the connection
                int? createdById = null;
                try { createdById = Convert.ToInt32(agentIdentity?.UserId); } catch { }
                AppBuilderAgentSessionBL.SaveSession(dbSessionId, request?.UserMessage, createdById);

                // 1. System prompt — memory is now retrieved on demand via search_memory tool (RAG).
                var systemPrompt = SystemPromptTemplate;

                // 2. Discover tools via reflection (pass full callbacks so all blocking plugins can pause)
                var tools = DiscoverTools(request.DataSourceRegisterId, request.SchemaOwner, callbacks);

                // 3. Build LLM tool definitions
                var provider = LLMProviderHelper.GetConfiguredProvider();
                var toolDefs = BuildToolDefinitions(tools, provider);

                // 4. Seed conversation (format differs per provider)
                // Trim stored history to the last N turns to avoid exploding the context window.
                var trimmedHistory = TrimConversationHistory(request.ConversationHistory);

                var messages = new List<JObject>();
                if (provider == EmLLMProvider.Gemini)
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                        {
                            var r = h.Role == "assistant" ? "model" : "user";
                            messages.Add(JObject.FromObject(new { role = r, parts = new[] { new { text = h.Content } } }));
                        }
                    messages.Add(JObject.FromObject(new { role = "user", parts = new[] { new { text = request.UserMessage } } }));
                }
                else
                {
                    if (trimmedHistory?.Any() == true)
                        foreach (var h in trimmedHistory)
                            messages.Add(JObject.FromObject(new { role = h.Role, content = h.Content }));
                    messages.Add(JObject.FromObject(new { role = "user", content = request.UserMessage }));
                }

                // 5. Agentic loop
                string finalResponse = null;
                var createdItems = new List<(int Id, string Name)>();

                for (int iteration = 0; iteration < MaxIterations; iteration++)
                {
                    // Prune oldest turn groups if the context window is getting too large.
                    messages = PruneMessages(messages, systemPrompt, provider);

                    await SafeCallback(callbacks.OnStep, new AgentStepEvent
                    {
                        Type = "thinking",
                        Description = iteration == 0 ? "Analyzing your request…" : "Processing results…",
                        IsSuccess = true
                    });

                    var llmResponse = provider == EmLLMProvider.Anthropic
                        ? await CallAnthropicWithToolsAsync(messages, toolDefs, systemPrompt)
                        : provider == EmLLMProvider.Gemini
                            ? await CallGeminiWithToolsAsync(messages, toolDefs, systemPrompt)
                            : await CallOpenAIWithToolsAsync(messages, toolDefs, systemPrompt);

                    if (!llmResponse.IsSuccess)
                    {
                        await SafeCallback(callbacks.OnError, $"LLM error: {llmResponse.Error}");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(llmResponse.TextContent))
                        await SafeCallback(callbacks.OnToken, llmResponse.TextContent);

                    if (llmResponse.StopReason == "end_turn" || llmResponse.StopReason == "stop")
                    {
                        finalResponse = llmResponse.TextContent;
                        break;
                    }

                    if (llmResponse.ToolCalls?.Any() == true)
                    {
                        messages.Add((JObject)llmResponse.AssistantMessageRaw);

                        // Run independent tool calls concurrently; results collected in LLM call order.
                        var toolTasks = llmResponse.ToolCalls.Select(async toolCall =>
                        {
                            await SafeCallback(callbacks.OnStep, new AgentStepEvent
                            {
                                Type        = "tool_call",
                                ToolName    = toolCall.Name,
                                Description = FriendlyLabel(toolCall.Name),
                                Details     = Truncate(toolCall.InputJson, 400),
                                IsSuccess   = true
                            });

                            string toolResult;
                            bool   success = true;
                            try
                            {
                                toolResult = await InvokeToolAsync(tools, toolCall);
                            }
                            catch (Exception ex)
                            {
                                toolResult = JsonConvert.SerializeObject(new { Error = ex.Message });
                                success    = false;
                            }

                            await SafeCallback(callbacks.OnStep, new AgentStepEvent
                            {
                                Type        = "tool_result",
                                ToolName    = toolCall.Name,
                                Description = success ? FriendlyLabel(toolCall.Name) + " — done"
                                                      : toolCall.Name + " failed",
                                Details   = Truncate(toolResult, 600),
                                IsSuccess = success
                            });

                            return (toolCall, toolResult, success);
                        }).ToArray();

                        var outputs     = await Task.WhenAll(toolTasks);
                        var toolResults = new List<object>();

                        foreach (var (toolCall, toolResult, success) in outputs)
                        {
                            // ExtractCreatedItems runs single-threaded here (after WhenAll)
                            ExtractCreatedItems(toolCall.Name, toolResult, createdItems);

                            var cappedResult = CapToolResult(toolResult);

                            if (provider == EmLLMProvider.Anthropic)
                                toolResults.Add(new { type = "tool_result", tool_use_id = toolCall.Id, content = cappedResult });
                            else if (provider == EmLLMProvider.Gemini)
                                toolResults.Add(new { functionResponse = new { name = toolCall.Name, response = new { result = cappedResult } } });
                            else
                                toolResults.Add(new { role = "tool", tool_call_id = toolCall.Id, content = cappedResult });
                        }

                        if (provider == EmLLMProvider.Anthropic)
                            messages.Add(JObject.FromObject(new { role = "user", content = toolResults }));
                        else if (provider == EmLLMProvider.Gemini)
                            messages.Add(JObject.FromObject(new { role = "user", parts = toolResults }));
                        else
                            foreach (var tr in toolResults)
                                messages.Add(JObject.FromObject(tr));
                    }
                    else
                    {
                        finalResponse = llmResponse.TextContent;
                        break;
                    }
                }

                // 6. Save memory
                AppBuilderAgentMemoryBL.RecordBuildSession(
                    request.UserMessage,
                    finalResponse ?? "(no response)",
                    createdItems.Select(i => i.Name).ToList(),
                    createdItems);

                // 7. Build done payload and persist to DB
                var updatedHistory = BuildUpdatedHistory(request, finalResponse);
                var checkpoint     = BuildCheckpoint(createdItems);
                if (checkpoint != null)
                    checkpoint.IsComplete = !string.IsNullOrEmpty(finalResponse);

                AppBuilderAgentSessionBL.UpdateSession(
                    dbSessionId, "Completed", checkpoint, updatedHistory, finalResponse);

                await SafeCallback(callbacks.OnDone, new AgentDoneEvent
                {
                    FinalResponse = finalResponse,
                    CreatedTransactions = createdItems
                        .Where(i => i.Id > 0)
                        .Select(i => new AgentCreatedTransactionDto { TransactionId = i.Id, Name = i.Name })
                        .ToList(),
                    UpdatedHistory = updatedHistory,
                    Checkpoint     = checkpoint
                });
            }
            catch (Exception ex)
            {
                AppBuilderAgentSessionBL.UpdateSession(
                    dbSessionId, "Failed", null, null, ex.Message);
                await SafeCallback(callbacks.OnError, $"Agent error: {ex.Message}");
            }
        }

        private static AppClientIdentity? RegisterSystemAgentIdentity(AppClientIdentity? agentIdentity)
        {
            if (!agentIdentity.HasValue)
            {
                var systemAgentUserId = AppTenantSettingBL.GetIntValue(EmTenantSettings.SystemAgentUser);
                if (systemAgentUserId.HasValue)
                {
                    var userEntity = AppSecurityUserBL.RetrieveOneAppSecurityUserEntity(systemAgentUserId);
                    if (userEntity != null)
                        agentIdentity = new AppClientIdentity
                        {
                            UserId = systemAgentUserId,
                            SessionId = Guid.NewGuid().ToString(),
                            IsCallingFromBrowser = true,
                            LanguageId = userEntity.LanguageId,
                            CurrentWorkingCompanyId = userEntity.MyOwnCompnanyId,
                            TimeZoneKey = userEntity.TimeZoneInfoToken,
                        };
                }
            }
            if (agentIdentity.HasValue && ServerContext.Instance.WindowsIdentityProvider != null)
                ServerContext.Instance.WindowsIdentityProvider.RegisterIdentity(agentIdentity);
            return agentIdentity;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Reflection-based plugin discovery (replaces SK Kernel)
        // ─────────────────────────────────────────────────────────────────────

        private static List<ToolDescriptor> DiscoverTools(
            int? dataSourceId, string schemaOwner,
            AgentCallbacks callbacks = null)
        {
            var pluginInstances = new object[]
            {
                new PlanConfirmPlugin(callbacks?.OnPlanReady),                          // propose_plan, confirm_drop_tables
                new SchemaDesignerPlugin(callbacks?.OnSchemaReady, dataSourceId, schemaOwner), // propose_schema, execute_approved_schema
                new ApplicationManagerPlugin(dataSourceId),
                new PlatformExplorerPlugin(dataSourceId),                               // explore_platform, search_platform
                new EntityDataSourcePlugin(dataSourceId),
                new TransactionBuilderPlugin(dataSourceId, schemaOwner),
                new TransactionModifierPlugin(dataSourceId),                            // update_transaction_field, set_field_entity, delete_transaction
                new SchemaBuilderPlugin(dataSourceId, schemaOwner),
                new SchemaAlterPlugin(dataSourceId, schemaOwner),                       // alter_table
                new DataQueryPlugin(dataSourceId),
                new SearchBuilderPlugin(dataSourceId),
                new MemorySearchPlugin()                                                 // search_memory (RAG over build history / notes)
            };

            var tools = new List<ToolDescriptor>();

            foreach (var instance in pluginInstances)
            {
                var methods = instance.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<AgentFunctionAttribute>() != null);

                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<AgentFunctionAttribute>();
                    tools.Add(new ToolDescriptor
                    {
                        Name        = attr.Name,
                        Description = attr.Description,
                        Method      = method,
                        Instance    = instance,
                        Parameters  = method.GetParameters()
                    });
                }
            }

            return tools;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build provider-specific tool definitions from ToolDescriptor list
        // ─────────────────────────────────────────────────────────────────────

        private static List<object> BuildToolDefinitions(List<ToolDescriptor> tools, EmLLMProvider provider)
        {
            if (provider == EmLLMProvider.Gemini)
            {
                // Gemini wraps all tools in a single { functionDeclarations: [...] } object
                var decls = new List<object>();
                foreach (var tool in tools)
                {
                    var props = new JObject();
                    var req   = new JArray();
                    foreach (var param in tool.Parameters)
                    {
                        var pa = param.GetCustomAttribute<AgentParamAttribute>();
                        props[param.Name] = new JObject
                        {
                            ["type"]        = MapToJsonTypeGemini(param.ParameterType),
                            ["description"] = pa?.Description ?? param.Name
                        };
                        if (pa?.IsRequired == true) req.Add(param.Name);
                    }
                    decls.Add(new
                    {
                        name        = tool.Name,
                        description = tool.Description,
                        parameters  = new JObject
                        {
                            ["type"]       = "OBJECT",
                            ["properties"] = props,
                            ["required"]   = req
                        }
                    });
                }
                return new List<object> { new { functionDeclarations = decls } };
            }

            var result = new List<object>();
            foreach (var tool in tools)
            {
                var properties = new JObject();
                var required   = new JArray();

                foreach (var param in tool.Parameters)
                {
                    var paramAttr = param.GetCustomAttribute<AgentParamAttribute>();
                    properties[param.Name] = new JObject
                    {
                        ["type"]        = MapToJsonType(param.ParameterType),
                        ["description"] = paramAttr?.Description ?? param.Name
                    };
                    if (paramAttr?.IsRequired == true) required.Add(param.Name);
                }

                var schema = new JObject
                {
                    ["type"]       = "object",
                    ["properties"] = properties,
                    ["required"]   = required
                };

                if (provider == EmLLMProvider.Anthropic)
                    result.Add(new { name = tool.Name, description = tool.Description, input_schema = schema });
                else
                    result.Add(new { type = "function", function = new { name = tool.Name, description = tool.Description, parameters = schema } });
            }

            return result;
        }

        private static string MapToJsonType(Type t)
        {
            if (t == null) return "string";
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int)    || t == typeof(long))    return "integer";
            if (t == typeof(double) || t == typeof(float)
                                    || t == typeof(decimal)) return "number";
            if (t == typeof(bool))                           return "boolean";
            return "string";
        }

        private static string MapToJsonTypeGemini(Type t)
        {
            if (t == null) return "STRING";
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(int)    || t == typeof(long))    return "INTEGER";
            if (t == typeof(double) || t == typeof(float)
                                    || t == typeof(decimal)) return "NUMBER";
            if (t == typeof(bool))                           return "BOOLEAN";
            return "STRING";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Invoke a tool by name via reflection
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<string> InvokeToolAsync(
            List<ToolDescriptor> tools, AgentToolCallDto toolCall)
        {
            var tool = tools.FirstOrDefault(t =>
                string.Equals(t.Name, toolCall.Name, StringComparison.OrdinalIgnoreCase));

            if (tool == null)
                return JsonConvert.SerializeObject(new { Error = $"Tool '{toolCall.Name}' not found" });

            var inputObj = string.IsNullOrWhiteSpace(toolCall.InputJson)
                ? new JObject()
                : JObject.Parse(toolCall.InputJson);

            // Map JSON args → method parameters
            var args = new object[tool.Parameters.Length];
            for (int i = 0; i < tool.Parameters.Length; i++)
            {
                var param = tool.Parameters[i];
                if (inputObj.TryGetValue(param.Name, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    var targetType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                    try   { args[i] = token.ToObject(targetType); }
                    catch { args[i] = param.HasDefaultValue ? param.DefaultValue : null; }
                }
                else
                {
                    args[i] = param.HasDefaultValue ? param.DefaultValue : null;
                }
            }

            // Invoke — handle both sync and async methods
            var rawResult = tool.Method.Invoke(tool.Instance, args);

            if (rawResult is Task<string> taskStr)
                return await taskStr.ConfigureAwait(false);

            if (rawResult is Task task)
            {
                await task.ConfigureAwait(false);
                var prop = task.GetType().GetProperty("Result");
                return prop?.GetValue(task)?.ToString() ?? "null";
            }

            return rawResult?.ToString() ?? "null";
        }

        // ─────────────────────────────────────────────────────────────────────
        // Anthropic API: messages + tools
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<AgentLLMResponseDto> CallAnthropicWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.AnthropicDefaultModel;

            var body = new JObject
            {
                ["model"]      = model,
                ["max_tokens"] = 8192,
                ["system"]     = systemPrompt,
                ["messages"]   = JArray.FromObject(messages),
                ["tools"]      = JArray.FromObject(tools)
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            req.Headers.Add("x-api-key", apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            var raw  = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return new AgentLLMResponseDto { IsSuccess = false, Error = $"Anthropic: {raw}" };

            var json       = JObject.Parse(raw);
            var stopReason = json["stop_reason"]?.ToString() ?? "end_turn";
            var contentArr = json["content"] as JArray ?? new JArray();

            var textParts = new List<string>();
            var toolCalls = new List<AgentToolCallDto>();

            foreach (var item in contentArr)
            {
                var itype = item["type"]?.ToString();
                if (itype == "text")
                    textParts.Add(item["text"]?.ToString() ?? "");
                else if (itype == "tool_use")
                    toolCalls.Add(new AgentToolCallDto
                    {
                        Id        = item["id"]?.ToString(),
                        Name      = item["name"]?.ToString(),
                        InputJson = item["input"]?.ToString(Formatting.None) ?? "{}"
                    });
            }

            return new AgentLLMResponseDto
            {
                IsSuccess           = true,
                StopReason          = stopReason,
                TextContent         = string.Join("\n", textParts),
                ToolCalls           = toolCalls,
                AssistantMessageRaw = JObject.FromObject(new { role = "assistant", content = contentArr })
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // OpenAI API: chat/completions + tools
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<AgentLLMResponseDto> CallOpenAIWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.OpenAIDefaultModel;

            var allMsgs = new JArray(
                JObject.FromObject(new { role = "system", content = systemPrompt }));
            foreach (var m in messages) allMsgs.Add(m);

            var body = new JObject
            {
                ["model"]       = model,
                ["messages"]    = allMsgs,
                ["tools"]       = JArray.FromObject(tools),
                ["tool_choice"] = "auto",
                ["max_tokens"]  = 8192
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            var raw  = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return new AgentLLMResponseDto { IsSuccess = false, Error = $"OpenAI: {raw}" };

            var json         = JObject.Parse(raw);
            var choice       = json["choices"]?[0];
            var finishReason = choice?["finish_reason"]?.ToString() ?? "stop";
            var message      = choice?["message"] as JObject ?? new JObject();
            var textContent  = message["content"]?.ToString();
            var toolCallsJ   = message["tool_calls"] as JArray;

            var toolCalls = new List<AgentToolCallDto>();
            if (toolCallsJ != null)
                foreach (var tc in toolCallsJ)
                    toolCalls.Add(new AgentToolCallDto
                    {
                        Id        = tc["id"]?.ToString(),
                        Name      = tc["function"]?["name"]?.ToString(),
                        InputJson = tc["function"]?["arguments"]?.ToString() ?? "{}"
                    });

            message["role"] = "assistant";

            return new AgentLLMResponseDto
            {
                IsSuccess           = true,
                StopReason          = finishReason == "tool_calls" ? "tool_use" : "end_turn",
                TextContent         = textContent,
                ToolCalls           = toolCalls,
                AssistantMessageRaw = message
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Google Gemini API: generateContent + functionDeclarations
        // ─────────────────────────────────────────────────────────────────────

        private static async Task<AgentLLMResponseDto> CallGeminiWithToolsAsync(
            List<JObject> messages, List<object> tools, string systemPrompt)
        {
            var apiKey = LLMProviderHelper.GetConfiguredApiKey();
            var model  = LLMProviderHelper.GeminiDefaultModel;
            var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var body = new JObject
            {
                ["systemInstruction"] = JObject.FromObject(new { parts = new[] { new { text = systemPrompt } } }),
                ["contents"]          = JArray.FromObject(messages),
                ["tools"]             = JArray.FromObject(tools)
            };

            var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            var raw  = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
                return new AgentLLMResponseDto { IsSuccess = false, Error = $"Gemini: {raw}" };

            var json      = JObject.Parse(raw);
            var candidate = json["candidates"]?[0];
            var content   = candidate?["content"] as JObject ?? new JObject();
            var parts     = content["parts"] as JArray ?? new JArray();

            var textParts = new List<string>();
            var toolCalls = new List<AgentToolCallDto>();

            foreach (var part in parts)
            {
                var text = part["text"]?.ToString();
                if (!string.IsNullOrEmpty(text)) textParts.Add(text);

                var fc = part["functionCall"] as JObject;
                if (fc != null)
                    toolCalls.Add(new AgentToolCallDto
                    {
                        Id        = fc["name"]?.ToString(), // Gemini has no call ID; use name
                        Name      = fc["name"]?.ToString(),
                        InputJson = fc["args"]?.ToString(Formatting.None) ?? "{}"
                    });
            }

            // Gemini signals "done" by returning text with no function calls
            var stopReason = toolCalls.Count > 0 ? "tool_use" : "end_turn";

            return new AgentLLMResponseDto
            {
                IsSuccess           = true,
                StopReason          = stopReason,
                TextContent         = string.Join("\n", textParts),
                ToolCalls           = toolCalls,
                AssistantMessageRaw = content  // { role: "model", parts: [...] }
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static async Task SafeCallback<T>(Func<T, Task> cb, T arg)
        {
            if (cb == null) return;
            try { await cb(arg).ConfigureAwait(false); } catch { }
        }

        private static List<AppBuilderAgentMessageDto> BuildUpdatedHistory(
            AppBuilderAgentRequestDto request, string finalResponse)
        {
            var history = new List<AppBuilderAgentMessageDto>(
                request.ConversationHistory ?? new List<AppBuilderAgentMessageDto>());
            history.Add(new AppBuilderAgentMessageDto { Role = "user",      Content = request.UserMessage });
            if (!string.IsNullOrEmpty(finalResponse))
                history.Add(new AppBuilderAgentMessageDto { Role = "assistant", Content = finalResponse });
            return history;
        }

        private static void ExtractCreatedItems(
            string toolName, string toolResult, List<(int Id, string Name)> list)
        {
            try
            {
                var json = JObject.Parse(toolResult);
                if (json["IsSuccess"]?.Value<bool>() == false) return;

                switch (toolName)
                {
                    case "create_app_package":
                    {
                        var appId   = json["SaasApplicationId"]?.Value<int>() ?? 0;
                        var appName = json["ApplicationName"]?.ToString();
                        if (appId > 0 && !string.IsNullOrEmpty(appName))
                            list.Add((appId, $"[App] {appName}"));
                        break;
                    }
                    case "execute_approved_schema":
                    case "create_application":
                    case "create_transaction_from_table":
                    {
                        var tables = json["TablesCreated"] as JArray;
                        if (tables != null)
                            foreach (var t in tables)
                            {
                                // execute_approved_schema returns plain strings; others return objects with Name
                                var name = t.Type == JTokenType.String ? t.Value<string>() : t["Name"]?.ToString();
                                if (!string.IsNullOrEmpty(name)) list.Add((-1, $"[Table] {name}"));
                            }
                        var txId   = json["TransactionId"]?.Value<int>() ?? 0;
                        var txName = json["TransactionName"]?.ToString();
                        if (txId > 0 && !string.IsNullOrEmpty(txName)) list.Add((txId, txName));
                        break;
                    }
                    case "propose_schema":
                    {
                        // Store the approved schema so resume can call execute_approved_schema directly
                        if (json["Confirmed"]?.Value<bool>() == true)
                        {
                            var schemaJson = json["SchemaJson"]?.ToString();
                            if (!string.IsNullOrEmpty(schemaJson))
                                list.Add((-1, $"[Schema] {schemaJson}"));
                        }
                        break;
                    }
                    case "create_entity_simple_list":
                    case "create_entity_from_table":
                    {
                        var entityCode = json["EntityCode"]?.ToString();
                        if (!string.IsNullOrEmpty(entityCode))
                            list.Add((-1, $"[Entity] {entityCode}"));
                        break;
                    }
                    case "create_search":
                    {
                        var searchId   = json["SearchId"]?.Value<int>() ?? 0;
                        var searchName = json["SearchName"]?.ToString();
                        if (searchId > 0 && !string.IsNullOrEmpty(searchName))
                            list.Add((-1, $"[Search] {searchName}"));
                        break;
                    }
                    case "create_list_edit_form":
                    {
                        var editTxId   = json["EditTransactionId"]?.Value<int>() ?? 0;
                        var editTxName = json["EditTransactionName"]?.ToString();
                        if (editTxId > 0 && !string.IsNullOrEmpty(editTxName))
                            list.Add((editTxId, editTxName));
                        break;
                    }
                }
            }
            catch { }
        }

        private static string FriendlyLabel(string n)
        {
            switch (n)
            {
                case "create_app_package":             return "Creating application package…";
                case "explore_platform":               return "Exploring platform state…";
                case "search_platform":                return "Searching platform items…";
                case "search_memory":                  return "Searching build history and notes…";
                case "get_database_tables":            return "Listing database tables…";
                case "get_existing_transactions":      return "Listing existing transactions…";
                case "get_transaction_details":        return "Fetching transaction details…";
                case "list_entity_data_sources":       return "Listing entity data sources…";
                case "create_entity_simple_list":      return "Creating simple list entity data source…";
                case "create_entity_from_table":       return "Creating table-linked entity data source…";
                case "create_application":             return "Building application (schema + tables + forms)…";
                case "create_search_view":             return "Creating search / list view…";
                case "add_search_to_menu":             return "Adding search to navigation menu…";
                case "create_transaction_from_table":  return "Creating transaction from table…";
                case "create_hierarchy_from_tables":   return "Building hierarchy transaction from existing tables…";
                case "get_table_schema":               return "Inspecting table schema…";
                case "create_database_table":          return "Creating database table…";
                case "execute_sql":                    return "Running verification query…";
                case "insert_mockup_data":             return "Inserting mockup / demo data…";
                case "check_table_exists":             return "Checking table exists…";
                case "create_search":                  return "Creating dataset + search view + search…";
                case "list_searches":                  return "Listing existing searches…";
                case "create_list_edit_form":          return "Creating list-edit form for transaction…";
                case "propose_plan":                   return "Proposing build plan — awaiting your approval…";
                case "propose_schema":                 return "Extracting schema — awaiting your review…";
                case "execute_approved_schema":        return "Executing approved schema — creating tables and transactions…";
                case "alter_table":                    return "Altering table + syncing platform data model…";
                case "delete_application":             return "Deleting application and all related data…";
                case "confirm_drop_tables":            return "Asking whether to drop database tables…";
                case "update_transaction_field":       return "Updating transaction field…";
                case "set_field_entity":               return "Linking field to entity data source…";
                case "delete_transaction":             return "Deleting transaction…";
                case "list_applications":              return "Listing applications with full child tree…";
                default:                               return n;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Build checkpoint from collected createdItems
        // ─────────────────────────────────────────────────────────────────────

        private static AgentCheckpointDto BuildCheckpoint(List<(int Id, string Name)> createdItems)
        {
            var cp = new AgentCheckpointDto();
            foreach (var (id, name) in createdItems)
            {
                if (name.StartsWith("[App] "))
                {
                    cp.SaasApplicationId = id > 0 ? id : cp.SaasApplicationId;
                    cp.ApplicationName   = name.Substring(6);
                }
                else if (name.StartsWith("[Table] "))
                {
                    var tableName = name.Substring(8);
                    if (!cp.TablesCreated.Contains(tableName))
                        cp.TablesCreated.Add(tableName);
                }
                else if (name.StartsWith("[Entity] "))
                {
                    var entityName = name.Substring(9);
                    if (!cp.EntitiesCreated.Contains(entityName))
                        cp.EntitiesCreated.Add(entityName);
                }
                else if (name.StartsWith("[Search] "))
                {
                    // searches tracked in EntitiesCreated for simplicity (no dedicated list)
                    var searchLabel = name;
                    if (!cp.EntitiesCreated.Contains(searchLabel))
                        cp.EntitiesCreated.Add(searchLabel);
                }
                else if (id > 0)
                {
                    if (!cp.TransactionsCreated.Any(t => t.TransactionId == id))
                        cp.TransactionsCreated.Add(new AgentCreatedTransactionDto { TransactionId = id, Name = name });
                }
            }

            // If schema was approved but no tables were created yet, store the pending schema
            // so resume can call execute_approved_schema directly (avoids infinite re-propose loop)
            if (cp.TablesCreated.Count == 0)
            {
                // Take the last [Schema] entry (most recently approved)
                var schemaEntry = createdItems.LastOrDefault(i => i.Name != null && i.Name.StartsWith("[Schema] "));
                if (schemaEntry.Name != null)
                    cp.ApprovedSchemaJson = schemaEntry.Name.Substring("[Schema] ".Length);
            }

            // Only return a checkpoint if something meaningful was accomplished
            bool hasData = cp.SaasApplicationId.HasValue
                        || cp.TablesCreated.Count > 0
                        || cp.TransactionsCreated.Count > 0
                        || cp.EntitiesCreated.Count > 0
                        || cp.ApprovedSchemaJson != null;
            return hasData ? cp : null;
        }

        private static string Truncate(string s, int max) =>
            s == null ? null : s.Length > max ? s.Substring(0, max) + "…" : s;

        // ─────────────────────────────────────────────────────────────────────
        // Context management — prevents token count explosion
        // ─────────────────────────────────────────────────────────────────────

        /// Tools whose results are always re-runnable and typically high-volume.
        /// PruneMessages drops these groups first before touching any other content.
        private static readonly HashSet<string> EphemeralToolNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explore_platform",
            "search_platform",
            "get_database_tables",
            "get_existing_transactions",
            "get_transaction_details",
            "list_entity_data_sources",
            "list_applications",
            "list_searches",
            "check_table_exists",
            "execute_sql",
        };

        /// <summary>
        /// Tool names whose message groups are NEVER dropped by PruneMessages.
        /// These are "gate" events — user approvals that the LLM must always remember.
        /// If dropped, the LLM re-invokes the gate (e.g. infinite schema-review loop).
        ///
        /// To protect a new gate tool in the future: add its name here. No other change needed.
        /// </summary>
        private static readonly string[] GateToolNames =
        {
            "propose_schema",          // schema approval gate — result stored in _approvedSchemaJson
            "propose_plan",            // plan approval gate
            "execute_approved_schema", // DDL execution — if pruned and re-called, auto-rollback DESTROYS existing tables
            "confirm_drop_tables",     // delete confirmation gate — if pruned, LLM re-asks user whether to drop tables
        };

        /// <summary>
        /// Returns the number of messages that form one logical turn starting at startIdx
        /// (one assistant/model message + all its associated result messages).
        ///
        /// Anthropic / Gemini: all results are bundled into a single user message → always 2.
        /// OpenAI: each tool result is a separate "tool"-role message → count consecutively.
        ///
        /// Getting this right is critical: dropping an assistant message without dropping ALL
        /// its result messages leaves orphaned tool_result entries that cause API errors.
        /// </summary>
        private static int GetGroupSize(List<JObject> messages, int startIdx, EmLLMProvider provider)
        {
            if (provider != EmLLMProvider.OpenAI)
                return startIdx + 1 < messages.Count ? 2 : 1;

            // OpenAI: count all consecutive "tool"-role messages that immediately follow
            int count = 1;
            while (startIdx + count < messages.Count &&
                   string.Equals(messages[startIdx + count]["role"]?.ToString(),
                       "tool", StringComparison.Ordinal))
                count++;
            return count;
        }

        /// <summary>
        /// Returns true if any message in group [startIdx, startIdx+groupSize)
        /// references a gate tool by name — the whole group must be kept intact.
        /// Checking all messages in the group catches both the tool_use call (assistant)
        /// and the Gemini function-response echo (user), preventing split-pair drops.
        /// </summary>
        private static bool IsGateGroup(List<JObject> messages, int startIdx, int groupSize)
        {
            int end = Math.Min(startIdx + groupSize, messages.Count);
            for (int k = startIdx; k < end; k++)
            {
                var msgStr = messages[k].ToString(Newtonsoft.Json.Formatting.None);
                foreach (var name in GateToolNames)
                    if (msgStr.Contains($"\"{name}\""))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if any message in the group references an ephemeral (re-runnable) tool.
        /// These groups are dropped first during pruning — their results are cheap to re-fetch.
        /// </summary>
        private static bool IsEphemeralGroup(List<JObject> messages, int startIdx, int groupSize)
        {
            int end = Math.Min(startIdx + groupSize, messages.Count);
            for (int k = startIdx; k < end; k++)
            {
                var msgStr = messages[k].ToString(Newtonsoft.Json.Formatting.None);
                foreach (var name in EphemeralToolNames)
                    if (msgStr.Contains($"\"{name}\""))
                        return true;
            }
            return false;
        }

        /// <summary>
        /// Maximum chars for a single tool result sent to the LLM.
        /// Large schemas / entity lists are trimmed with a note.
        /// Override via appSettings "Agent.MaxToolResultChars". Default 10000.
        /// </summary>
        private static int MaxToolResultChars
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxToolResultChars");
                return int.TryParse(raw, out int v) ? Math.Max(2000, v) : 10000;
            }
        }

        /// <summary>
        /// Token budget (input tokens) before the sliding window kicks in.
        /// ~4 chars per token (rough English estimate).
        /// Override via appSettings "Agent.TokenBudget". Default 120000.
        /// </summary>
        private static int TokenBudget
        {
            get
            {
                var raw = AppConfig.Get("Agent.TokenBudget");
                return int.TryParse(raw, out int v) ? Math.Max(20000, v) : 120000;
            }
        }

        /// <summary>
        /// How many recent conversation turns (user+assistant pairs) to keep
        /// when seeding the agent from stored ConversationHistory.
        /// Override via appSettings "Agent.MaxHistoryTurns". Default 4.
        /// </summary>
        private static int MaxHistoryTurns
        {
            get
            {
                var raw = AppConfig.Get("Agent.MaxHistoryTurns");
                return int.TryParse(raw, out int v) ? Math.Max(1, v) : 4;
            }
        }

        /// <summary>
        /// Cap a tool result at MaxToolResultChars before it is added to the
        /// LLM context. Appends a note so the model knows data was trimmed.
        /// </summary>
        private static string CapToolResult(string result)
        {
            if (result == null) return null;
            int max = MaxToolResultChars;
            if (result.Length <= max) return result;
            int kept = max;
            int dropped = result.Length - kept;
            return result.Substring(0, kept)
                + $"\n[... {dropped} chars truncated — ask me to fetch specific details if needed ...]";
        }

        /// <summary>
        /// Rough token estimator: ~4 chars per token (good enough for budget checks).
        /// </summary>
        private static int EstimateTokens(string systemPrompt, List<JObject> messages)
        {
            int chars = (systemPrompt?.Length ?? 0);
            foreach (var m in messages)
                chars += m.ToString(Newtonsoft.Json.Formatting.None).Length;
            return chars / 4;
        }

        /// <summary>
        /// Sliding-window pruner: when estimated token count exceeds TokenBudget,
        /// drops the oldest logical turn group (assistant + all its results) while keeping
        /// messages[0] (the original user request) and all gate groups (GateToolNames).
        ///
        /// Design:
        ///   - Uses GetGroupSize to atomically identify and drop the full group for a turn.
        ///     For Anthropic/Gemini results are bundled → group = 2 messages.
        ///     For OpenAI results are separate "tool" messages → group = 1 + N tool messages.
        ///     Dropping partial groups (e.g. assistant without its tool_result) causes
        ///     API format errors (Anthropic: orphaned tool_use; OpenAI: missing tool response).
        ///   - Uses IsGateGroup to skip any group whose messages reference a gate tool.
        ///     Gate groups (propose_schema, propose_plan) represent user approvals; pruning
        ///     them causes the LLM to re-invoke the gate showing the approval UI again.
        ///   - If only gate groups remain and the budget is still exceeded, pruning stops.
        ///     In practice this is safe: gate messages are small and the system prompt already
        ///     instructs the LLM not to re-invoke them after Confirmed:true.
        ///
        /// To add a new protected gate tool: add its name to GateToolNames above.
        /// </summary>
        private static List<JObject> PruneMessages(
            List<JObject> messages, string systemPrompt, EmLLMProvider provider)
        {
            if (messages.Count <= 4) return messages;

            int budget = TokenBudget;
            while (messages.Count > 4 && EstimateTokens(systemPrompt, messages) > budget)
            {
                // Pass 1: prefer dropping ephemeral (re-runnable, high-volume) groups first.
                // This preserves substantive build history while still freeing token budget.
                int dropIdx = -1, dropCount = 0;
                int i = 1;
                while (i < messages.Count - 1)
                {
                    int groupSize = GetGroupSize(messages, i, provider);
                    if (!IsGateGroup(messages, i, groupSize) && IsEphemeralGroup(messages, i, groupSize))
                    {
                        dropIdx   = i;
                        dropCount = groupSize;
                        break;
                    }
                    i += groupSize;
                }

                // Pass 2: if no ephemeral group found, fall back to oldest non-gate group.
                if (dropIdx < 0)
                {
                    i = 1;
                    while (i < messages.Count - 1)
                    {
                        int groupSize = GetGroupSize(messages, i, provider);
                        if (!IsGateGroup(messages, i, groupSize))
                        {
                            dropIdx   = i;
                            dropCount = groupSize;
                            break;
                        }
                        i += groupSize;
                    }
                }

                if (dropIdx < 0) break; // only gate groups remain — stop pruning

                for (int d = 0; d < dropCount && dropIdx < messages.Count; d++)
                    messages.RemoveAt(dropIdx);
            }

            return messages;
        }

        /// <summary>
        /// Trims ConversationHistory to the last MaxHistoryTurns turns
        /// (1 turn = 1 user message + 1 assistant message = 2 entries).
        /// </summary>
        private static List<AppBuilderAgentMessageDto> TrimConversationHistory(
            List<AppBuilderAgentMessageDto> history)
        {
            if (history == null || history.Count == 0) return history;
            int maxMessages = MaxHistoryTurns * 2; // each turn = user + assistant
            if (history.Count <= maxMessages) return history;
            return history.Skip(history.Count - maxMessages).ToList();
        }
    }
}
