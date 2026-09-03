using System;
using System.Threading.Tasks;
using APP.Components.EntityDto;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Callback contract for GenericAgentEngine — same shape as AgentCallbacks
    /// but typed for generic multi-skill use.
    /// </summary>
    public sealed class GenericAgentCallbacks
    {
        /// <summary>Fired for each streamed text token from the LLM.</summary>
        public Func<string, Task> OnToken { get; set; }

        /// <summary>Fired for each agent step (thinking / tool_call / tool_result).</summary>
        public Func<AgentStepEvent, Task> OnStep { get; set; }

        /// <summary>Fired once when the run completes with the final text.</summary>
        public Func<string, Task> OnDone { get; set; }

        /// <summary>Fired on unrecoverable error with the error message.</summary>
        public Func<string, Task> OnError { get; set; }

        /// <summary>
        /// Optional: blocks the agent until the user approves or rejects the plan.
        /// Only invoked when CapabilityFlags has the PlanGate bit (4).
        /// Return true = approved, false = rejected.
        /// </summary>
        public Func<AgentPlanEvent, Task<bool>> OnPlanReady { get; set; }

        /// <summary>
        /// Optional: blocks the agent until the user reviews the schema.
        /// Only invoked when CapabilityFlags has the SchemaGate bit (8).
        /// </summary>
        public Func<AgentSchemaEvent, Task<AgentSchemaResponse>> OnSchemaReady { get; set; }
    }
}
