using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APP.Components.EntityDto
{
    // ─────────────────────────────────────────────────────────────────────────────
    // Request
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentRequestDto
    {
        /// <summary>Natural-language instruction from the user.</summary>
        public string UserMessage { get; set; }

        /// <summary>The TransactionId of the workflow being edited.</summary>
        public int TransactionId { get; set; }

        /// <summary>Previous turns so the agent can continue the session.</summary>
        public List<AppBuilderAgentMessageDto> ConversationHistory { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Task result (includes optional canvas position for future visual editor)
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentTaskResult
    {
        public int    TaskId     { get; set; }
        public string Name       { get; set; }
        public int    ActionType { get; set; }
        public int    SortOrder  { get; set; }

        /// <summary>Future n8n-style canvas: optional X position.</summary>
        public float? PositionX  { get; set; }

        /// <summary>Future n8n-style canvas: optional Y position.</summary>
        public float? PositionY  { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Done event
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentDoneEvent
    {
        public string FinalResponse { get; set; }
        public List<AppBuilderAgentMessageDto> UpdatedHistory { get; set; } = new List<AppBuilderAgentMessageDto>();
        public List<WfAgentTaskResult> CreatedOrModifiedTasks { get; set; } = new List<WfAgentTaskResult>();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Plan-confirm event: emitted by propose_workflow_changes before any writes
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentPlanEvent
    {
        /// <summary>Human-readable description of all changes the agent will make.</summary>
        public string PlanSummary { get; set; }

        /// <summary>Names of new tasks the agent will create.</summary>
        public List<string> TasksToCreate { get; set; } = new List<string>();

        /// <summary>Names of existing tasks the agent will modify.</summary>
        public List<string> TasksToModify { get; set; } = new List<string>();

        /// <summary>Names of tasks the agent will delete.</summary>
        public List<string> TasksToDelete { get; set; } = new List<string>();

        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Request body for POST /WorkflowAutomationAgent/ConfirmPlan
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentConfirmPlanRequestDto
    {
        public string SessionId  { get; set; }

        /// <summary>true = proceed, false = reject (agent will revise based on feedback).</summary>
        public bool IsApproved { get; set; }

        /// <summary>Optional feedback when rejecting; fed back to the agent as a tool result.</summary>
        public string Feedback { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Callbacks: passed by the Controller into BL so BL has zero infrastructure deps
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentCallbacks
    {
        /// <summary>Called for each agent step (thinking / tool_call / tool_result / error).</summary>
        public Func<AgentStepEvent, Task> OnStep { get; set; }

        /// <summary>Called with each streamed text token from the LLM.</summary>
        public Func<string, Task> OnToken { get; set; }

        /// <summary>Called once when the agent run completes.</summary>
        public Func<WfAgentDoneEvent, Task> OnDone { get; set; }

        /// <summary>Called on unrecoverable errors.</summary>
        public Func<string, Task> OnError { get; set; }

        /// <summary>
        /// Called when the agent has proposed workflow changes and needs user confirmation
        /// before executing any create_task / update_task / delete_task.
        /// Return true = approved, false = rejected (agent will revise).
        /// If null, auto-approve.
        /// </summary>
        public Func<WfAgentPlanEvent, Task<bool>> OnPlanReady { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Polling event envelope  (EventType: "step"|"token"|"done"|"error"|"plan")
    // ─────────────────────────────────────────────────────────────────────────────

    public class WfAgentEventDto
    {
        /// <summary>"step" | "token" | "done" | "error" | "plan"</summary>
        public string EventType      { get; set; }
        public AgentStepEvent Step   { get; set; }
        public string Token          { get; set; }
        public WfAgentDoneEvent Done { get; set; }
        public string Error          { get; set; }

        /// <summary>Populated when EventType = "plan". UI must call POST /ConfirmPlan to proceed.</summary>
        public WfAgentPlanEvent Plan { get; set; }
    }

    public class WfAgentPollResponseDto
    {
        public List<WfAgentEventDto> Events { get; set; } = new List<WfAgentEventDto>();
        public bool SessionExists { get; set; }
    }
}
