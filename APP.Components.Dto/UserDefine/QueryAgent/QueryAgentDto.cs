using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Request
    // ─────────────────────────────────────────────────────────────────────────────

    public class QueryAgentRequestDto
    {
        /// <summary>Natural-language instruction from the user.</summary>
        public string UserMessage { get; set; }

        /// <summary>The data source register ID to query against.</summary>
        public int DataSourceId { get; set; }

        /// <summary>Table/view names the user has selected in the left panel.</summary>
        public List<string> SelectedTables { get; set; } = new List<string>();

        /// <summary>Previous turns so the agent can continue the session.</summary>
        public List<AppBuilderAgentMessageDto> ConversationHistory { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Done event
    // ─────────────────────────────────────────────────────────────────────────────

    public class QueryAgentDoneEvent
    {
        /// <summary>Full final response text from the agent.</summary>
        public string FinalResponse { get; set; }

        /// <summary>The SQL query extracted from the final response (```sql block). May be null.</summary>
        public string GeneratedQuery { get; set; }

        public List<AppBuilderAgentMessageDto> UpdatedHistory { get; set; } = new List<AppBuilderAgentMessageDto>();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Callbacks: passed by the Controller into BL
    // ─────────────────────────────────────────────────────────────────────────────

    public class QueryAgentCallbacks
    {
        public Func<AgentStepEvent, Task> OnStep  { get; set; }
        public Func<string, Task>         OnToken { get; set; }
        public Func<QueryAgentDoneEvent, Task> OnDone  { get; set; }
        public Func<string, Task>         OnError { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Polling event envelope  (EventType: "step"|"token"|"done"|"error")
    // ─────────────────────────────────────────────────────────────────────────────

    public class QueryAgentEventDto
    {
        /// <summary>"step" | "token" | "done" | "error"</summary>
        public string EventType           { get; set; }
        public AgentStepEvent Step        { get; set; }
        public string Token               { get; set; }
        public QueryAgentDoneEvent Done   { get; set; }
        public string Error               { get; set; }
    }

    public class QueryAgentPollResponseDto
    {
        public List<QueryAgentEventDto> Events { get; set; } = new List<QueryAgentEventDto>();
        public bool SessionExists { get; set; }
    }
}
