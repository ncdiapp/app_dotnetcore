using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL.AppBuilderAgent;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.WorkflowAutomationAgent.Plugins
{
    /// <summary>
    /// propose_workflow_changes — the gate that prevents the agent from creating, modifying,
    /// or deleting workflow tasks without user approval.
    ///
    /// The agent is instructed (via system prompt) to ALWAYS call propose_workflow_changes before
    /// any create_task, update_task, or delete_task call. This plugin:
    ///   1. Builds a WfAgentPlanEvent from the LLM's arguments
    ///   2. Calls the OnPlanReady callback (wired by the controller to enqueue a "plan" event
    ///      and register a TaskCompletionSource on the session)
    ///   3. Blocks (awaits) until the user responds via POST /ConfirmPlan
    ///   4. Returns "approved" or "rejected:feedback" to the LLM
    ///
    /// If OnPlanReady is null (no callback wired), the tool auto-approves.
    /// </summary>
    public class WfAgentConfirmPlugin
    {
        private readonly Func<WfAgentPlanEvent, Task<bool>> _onPlanReady;

        public WfAgentConfirmPlugin(Func<WfAgentPlanEvent, Task<bool>> onPlanReady)
        {
            _onPlanReady = onPlanReady;
        }

        [AgentFunction("propose_workflow_changes",
            "REQUIRED GATE — MUST call this BEFORE create_task, update_task, or delete_task. " +
            "Presents a summary of all planned changes to the user and waits for their approval. " +
            "Returns 'approved' if the user approves — then proceed with the task operations. " +
            "Returns 'rejected: <feedback>' if the user rejects — revise the plan based on feedback and call propose_workflow_changes again. " +
            "Never skip this gate for any operation that creates, modifies, or deletes workflow tasks.")]
        public async Task<string> ProposeWorkflowChanges(
            [AgentParam("Plain-text summary of all changes: which tasks will be created/modified/deleted and why.", isRequired: true)]
            string summary,
            [AgentParam("Comma-separated names of tasks to CREATE, e.g. 'Import JSON,Clear Staging,Save Record'.")]
            string tasksToCreate = null,
            [AgentParam("Comma-separated names of existing tasks to MODIFY.")]
            string tasksToModify = null,
            [AgentParam("Comma-separated names of tasks to DELETE.")]
            string tasksToDelete = null)
        {
            var planEvent = new WfAgentPlanEvent
            {
                PlanSummary   = summary,
                TasksToCreate = ParseCsv(tasksToCreate),
                TasksToModify = ParseCsv(tasksToModify),
                TasksToDelete = ParseCsv(tasksToDelete)
            };

            // Auto-approve if no callback wired (e.g. unit tests)
            if (_onPlanReady == null)
                return "approved (auto — no confirmation callback wired)";

            bool approved;
            try
            {
                approved = await _onPlanReady(planEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "approved (confirmation callback error — proceeding: " + ex.Message + ")";
            }

            if (approved)
                return "approved";

            return "rejected — user declined the plan. Ask the user what they would like to change, then revise and call propose_workflow_changes again before executing any task operations.";
        }

        private static List<string> ParseCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return new List<string>();

            return csv
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
    }
}
