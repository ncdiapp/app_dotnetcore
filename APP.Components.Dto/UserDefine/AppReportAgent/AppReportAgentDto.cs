using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.EntityDto;

namespace APP.Components.EntityDto
{
    // ─── Request ────────────────────────────────────────────────────────────────

    public class AppReportAgentMessageDto
    {
        public string Role    { get; set; }  // "user" | "assistant"
        public string Content { get; set; }
    }

    public class AppReportAgentRequestDto
    {
        public string UserMessage              { get; set; }
        /// <summary>
        /// Optional — required only when the agent needs to build a new search
        /// (falls back to create_search). Null = report-only mode.
        /// </summary>
        public int?   DataSourceRegisterId    { get; set; }
        public List<AppReportAgentMessageDto> ConversationHistory { get; set; }
    }

    // ─── Result carried in the done event ───────────────────────────────────────

    /// <summary>
    /// The search result produced by execute_report — sent to the React client
    /// so it can render GridView / PivotView / GanttView without a second API call.
    /// </summary>
    public class AgentReportResultEvent
    {
        public int    SearchDefinitionId { get; set; }
        public string SearchName         { get; set; }
        /// <summary>"grid" | "pivot" | "gantt"</summary>
        public string ViewType           { get; set; }
        /// <summary>Full SearchDto (serialised as dynamic object for the React client).</summary>
        public object SearchDto          { get; set; }
        /// <summary>ReferenceViewDto (serialised as dynamic object).</summary>
        public object ViewDto            { get; set; }
        /// <summary>SearchResultRowList items.</summary>
        public List<object> SearchResultRows { get; set; }
        public int    RowCount           { get; set; }
        public DateTime Timestamp        { get; set; }
    }

    public class AgentReportDoneEvent
    {
        public string FinalResponse  { get; set; }
        public AgentReportResultEvent ReportResult { get; set; }
        public List<AppReportAgentMessageDto> UpdatedHistory { get; set; }
        public DateTime Timestamp    { get; set; }
    }

    // ─── Polling event envelope ──────────────────────────────────────────────────

    public class AppReportAgentEventDto
    {
        /// <summary>"step" | "token" | "done" | "error"</summary>
        public string EventType { get; set; }

        /// <summary>Present when EventType = "step".</summary>
        public AgentStepEvent Step  { get; set; }

        /// <summary>Present when EventType = "token" (streaming text chunk).</summary>
        public string Token         { get; set; }

        /// <summary>Present when EventType = "done".</summary>
        public AgentReportDoneEvent Done { get; set; }

        /// <summary>Present when EventType = "error".</summary>
        public string Error         { get; set; }
    }

    public class AppReportAgentPollResponseDto
    {
        public List<AppReportAgentEventDto> Events { get; set; }
        public bool SessionExists { get; set; }
    }

    public class AppReportAgentStartResultDto
    {
        public bool   IsStarted { get; set; }
        public string SessionId { get; set; }
        public string Error     { get; set; }
    }

    // ─── Callbacks wired by the controller ──────────────────────────────────────

    public class ReportAgentCallbacks
    {
        public Func<AgentStepEvent,         Task> OnStep  { get; set; }
        public Func<string,                 Task> OnToken { get; set; }
        public Func<AgentReportDoneEvent,   Task> OnDone  { get; set; }
        public Func<string,                 Task> OnError { get; set; }
    }
}
