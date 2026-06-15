using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Conversation message
    // ─────────────────────────────────────────────────────────────────────────────

    public class AppBuilderAgentMessageDto
    {
        /// <summary>"user" | "assistant"</summary>
        public string Role { get; set; }
        public string Content { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Request
    // ─────────────────────────────────────────────────────────────────────────────

    public class AppBuilderAgentRequestDto
    {
        /// <summary>Natural-language instruction from the user.</summary>
        public string UserMessage { get; set; }

        /// <summary>Target database. Null → platform default.</summary>
        public int? DataSourceRegisterId { get; set; }

        /// <summary>Schema owner, e.g. "dbo".</summary>
        public string SchemaOwner { get; set; }

        /// <summary>
        /// The SaasApplicationId (AppListMenu.MenuId) of the application package created in Step 1.
        /// Pass this after create_app_package so subsequent tools (entity data sources, transactions) are linked to the correct app.
        /// </summary>
        public int? SaasApplicationId { get; set; }

        /// <summary>Previous turns so the agent can continue the session.</summary>
        public List<AppBuilderAgentMessageDto> ConversationHistory { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Streaming events pushed via SignalR during the run
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentStepEvent
    {
        /// <summary>"thinking" | "tool_call" | "tool_result" | "error" | "plan"</summary>
        public string Type { get; set; }

        public string ToolName { get; set; }

        /// <summary>Short label shown in the step header.</summary>
        public string Description { get; set; }

        /// <summary>Full detail text shown when step is expanded.</summary>
        public string Details { get; set; }

        public bool IsSuccess { get; set; } = true;

        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Callbacks: passed by the Controller into BL so BL has zero infrastructure deps
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentCallbacks
    {
        /// <summary>Called for each agent step (thinking / tool_call / tool_result / error).</summary>
        public Func<AgentStepEvent, Task> OnStep { get; set; }

        /// <summary>Called with each streamed text token from the LLM.</summary>
        public Func<string, Task> OnToken { get; set; }

        /// <summary>Called once when the agent run completes.</summary>
        public Func<AgentDoneEvent, Task> OnDone { get; set; }

        /// <summary>Called on unrecoverable errors.</summary>
        public Func<string, Task> OnError { get; set; }

        /// <summary>
        /// Called when the agent has composed a build plan and needs user confirmation before
        /// executing any DDL (create_application / create_database_table).
        /// Return true = user approved, false = user rejected.
        /// If null, the agent proceeds without pausing (auto-approve).
        /// </summary>
        public Func<AgentPlanEvent, Task<bool>> OnPlanReady { get; set; }

        /// <summary>
        /// Called when the agent has extracted a schema and needs the user to review/edit it
        /// before any DDL is executed (propose_schema tool).
        /// Returns AgentSchemaResponse: Confirmed=true with (possibly edited) SchemaJson,
        /// or Confirmed=false with Feedback for the agent to re-plan.
        /// If null, the schema is auto-approved unchanged.
        /// </summary>
        public Func<AgentSchemaEvent, Task<AgentSchemaResponse>> OnSchemaReady { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Schema-review event: emitted by propose_schema so the user sees + edits the
    // full table/column structure before any DDL is executed
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentSchemaColumn
    {
        public string Name           { get; set; }
        public string DataType       { get; set; }
        public bool   IsPrimaryKey   { get; set; }
        public bool   IsNullable     { get; set; }
        public bool   IsAutoIncrement { get; set; }
        public int?   Length         { get; set; }
        public string DefaultValue   { get; set; }
        public string Description    { get; set; }
        /// <summary>FK target table name, null if not a foreign key.</summary>
        public string FKTargetTable  { get; set; }
        /// <summary>Relationship type: ONE_TO_MANY (composition child) or MANY_TO_ONE (lookup).</summary>
        public string RelationshipType { get; set; }
    }

    public class AgentSchemaTable
    {
        public string Name          { get; set; }
        public string Description   { get; set; }
        /// <summary>True = standalone lookup table (List Edit). False = part of the master-detail hierarchy.</summary>
        public bool   IsLookup      { get; set; }
        /// <summary>Null for master; parent table name for children/grandchildren.</summary>
        public string ParentTable   { get; set; }
        public List<AgentSchemaColumn> Columns { get; set; } = new List<AgentSchemaColumn>();
    }

    public class AgentSchemaEvent
    {
        public string Summary       { get; set; }
        /// <summary>Raw JSON of the SchemaExtractionResultDto — sent back as SchemaJson if user approves unchanged.</summary>
        public string SchemaJson    { get; set; }
        public List<AgentSchemaTable> Tables { get; set; } = new List<AgentSchemaTable>();
        /// <summary>CREATE TABLE scripts generated from the schema (preview only — not yet executed).</summary>
        public string CreateScript  { get; set; }
        public string Timestamp     { get; set; } = DateTime.UtcNow.ToString("o");
    }

    /// <summary>
    /// Returned by the frontend after the user reviews (and optionally edits) the schema.
    /// </summary>
    public class AgentSchemaResponse
    {
        public bool   Confirmed      { get; set; }
        /// <summary>
        /// JSON of the (possibly user-edited) SchemaExtractionResultDto.
        /// If null, the original schema from AgentSchemaEvent.SchemaJson is used.
        /// </summary>
        public string SchemaJson     { get; set; }
        /// <summary>Reason / change request when Confirmed=false.</summary>
        public string Feedback       { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Request body for POST /ConfirmSchema
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentConfirmSchemaRequestDto
    {
        public string SessionId      { get; set; }
        public bool   Confirmed      { get; set; }
        /// <summary>Full JSON of the edited schema. Null = use original unchanged.</summary>
        public string SchemaJson     { get; set; }
        public string Feedback       { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Plan-confirm event: emitted before DDL so the user can review and approve
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentPlanEvent
    {
        /// <summary>Human-readable description of what will be built.</summary>
        public string PlanSummary { get; set; }

        /// <summary>Database tables that will be physically created.</summary>
        public List<string> TablesToCreate { get; set; } = new List<string>();

        /// <summary>Transaction / screen names that will be generated.</summary>
        public List<string> ScreensToCreate { get; set; } = new List<string>();

        /// <summary>
        /// Database tables that will be physically DROPPED (populated by confirm_drop_tables).
        /// When non-empty the UI renders a destructive confirmation card instead of the build plan card.
        /// </summary>
        public List<string> TablesToDrop { get; set; } = new List<string>();

        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Request body for POST /ConfirmPlan
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentConfirmPlanRequestDto
    {
        public string SessionId { get; set; }

        /// <summary>true = proceed, false = reject (agent will stop or re-plan).</summary>
        public bool Confirmed { get; set; }

        /// <summary>Optional feedback when rejecting, fed back to the agent as a tool result.</summary>
        public string Feedback { get; set; }
    }

    public class AgentDoneEvent
    {
        public string FinalResponse { get; set; }
        public List<AgentCreatedTransactionDto> CreatedTransactions { get; set; } = new List<AgentCreatedTransactionDto>();
        public List<AppBuilderAgentMessageDto> UpdatedHistory { get; set; } = new List<AppBuilderAgentMessageDto>();

        /// <summary>Snapshot of what was successfully completed this run — used by the UI to offer "Resume from checkpoint".</summary>
        public AgentCheckpointDto Checkpoint { get; set; }
    }

    /// <summary>
    /// Snapshot of completed work captured at the end of each agent run.
    /// Stored in the chat session so the user can resume after fixing issues.
    /// </summary>
    public class AgentCheckpointDto
    {
        public int?   SaasApplicationId { get; set; }
        public string ApplicationName   { get; set; }
        public List<string> TablesCreated        { get; set; } = new List<string>();
        public List<AgentCreatedTransactionDto> TransactionsCreated { get; set; } = new List<AgentCreatedTransactionDto>();
        public List<string> EntitiesCreated      { get; set; } = new List<string>();
        /// <summary>
        /// Approved schema JSON from propose_schema — present only when schema was approved
        /// but execute_approved_schema has not yet succeeded.
        /// On resume the agent calls execute_approved_schema with this JSON directly.
        /// </summary>
        public string ApprovedSchemaJson { get; set; }
        /// <summary>True when the agent run completed normally. False = run was cut short or errored.</summary>
        public bool IsComplete { get; set; }
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    public class AgentCreatedTransactionDto
    {
        public int TransactionId { get; set; }
        public string Name { get; set; }
        public string TableName { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // HTTP response for POST /RunAgent (returns session ID for polling)
    // ─────────────────────────────────────────────────────────────────────────────

    public class AppBuilderAgentStartResultDto
    {
        public bool IsStarted { get; set; }
        public string SessionId { get; set; }
        public string Error { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Polling: event envelope returned by GET /PollEvents
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single event emitted by the agent, queued server-side and returned by polling.
    /// EventType: "step" | "token" | "done" | "error" | "plan" | "schema"
    /// </summary>
    public class AgentEventDto
    {
        /// <summary>EventType: "step" | "token" | "done" | "error" | "plan" | "schema"</summary>
        public string EventType { get; set; }
        public AgentStepEvent Step  { get; set; }
        public string Token         { get; set; }
        public AgentDoneEvent Done  { get; set; }
        public string Error         { get; set; }

        /// <summary>Populated when EventType = "plan". UI must call POST /ConfirmPlan to proceed.</summary>
        public AgentPlanEvent Plan  { get; set; }

        /// <summary>Populated when EventType = "schema". UI must call POST /ConfirmSchema to proceed.</summary>
        public AgentSchemaEvent Schema { get; set; }
    }

    public class AgentPollResponseDto
    {
        public List<AgentEventDto> Events { get; set; } = new List<AgentEventDto>();
        public bool SessionExists { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Internal: LLM tool-use response structures used by AppBuilderAgentBL
    // ─────────────────────────────────────────────────────────────────────────────

    public class AgentToolCallDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string InputJson { get; set; }
    }

    public class AgentLLMResponseDto
    {
        public bool IsSuccess { get; set; }
        public string StopReason { get; set; }   // "end_turn" | "tool_use" | "stop" | "tool_calls"
        public string TextContent { get; set; }
        public List<AgentToolCallDto> ToolCalls { get; set; } = new List<AgentToolCallDto>();
        public string Error { get; set; }

        /// <summary>Raw assistant message object to add back into the conversation (provider-specific).</summary>
        public object AssistantMessageRaw { get; set; }
    }
}
