using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins
{
    /// <summary>
    /// propose_plan — the gate that prevents the agent from running DDL without user approval.
    ///
    /// The agent is instructed (via system prompt) to ALWAYS call propose_plan before any
    /// create_application or create_database_table call. This plugin:
    ///   1. Builds an AgentPlanEvent from the LLM's arguments
    ///   2. Calls the OnPlanReady callback (wired by the controller to enqueue a "plan" event
    ///      and register a TaskCompletionSource on the session)
    ///   3. Blocks (awaits) until the user responds via POST /ConfirmPlan
    ///   4. Returns {Confirmed:true} or {Confirmed:false,Reason:...} to the LLM
    ///
    /// If OnPlanReady is null (no callback wired), the tool auto-approves — backward-compatible
    /// with callers that have not wired the new callback.
    /// </summary>
    public class PlanConfirmPlugin
    {
        private readonly Func<AgentPlanEvent, Task<bool>> _onPlanReady;

        public PlanConfirmPlugin(Func<AgentPlanEvent, Task<bool>> onPlanReady)
        {
            _onPlanReady = onPlanReady;
        }

        [AgentFunction("propose_plan",
            "REQUIRED GATE — call this BEFORE create_application or create_database_table. " +
            "Presents a summary of what will be built to the user and waits for their approval. " +
            "Returns {Confirmed:true} if the user approves — then proceed with building. " +
            "Returns {Confirmed:false,Reason:...} if the user rejects — adjust the plan and call propose_plan again. " +
            "Never skip this step for any operation that creates or modifies database tables.")]
        public async Task<string> ProposePlan(
            [AgentParam("Plain-text summary of what will be created: which tables, fields, relationships, and screens.", isRequired: true)]
            string planSummary,
            [AgentParam("Comma-separated list of database table names that will be physically created, e.g. 'Order,OrderItem,Customer'.")]
            string tablesToCreate = null,
            [AgentParam("Comma-separated list of transaction/screen names that will be generated, e.g. 'Order Management,Customer List'.")]
            string screensToCreate = null)
        {
            var planEvent = new AgentPlanEvent
            {
                PlanSummary    = planSummary,
                TablesToCreate = ParseCsv(tablesToCreate),
                ScreensToCreate = ParseCsv(screensToCreate)
            };

            // Auto-approve if no callback is wired (e.g. unit tests, legacy callers)
            if (_onPlanReady == null)
                return JsonConvert.SerializeObject(new { Confirmed = true, Note = "Auto-approved (no confirmation callback wired)" });

            bool confirmed;
            try
            {
                confirmed = await _onPlanReady(planEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If the callback throws (e.g. session expired), auto-approve to avoid deadlock
                confirmed = true;
                return JsonConvert.SerializeObject(new
                {
                    Confirmed = confirmed,
                    Note = "Confirmation callback error — proceeding: " + ex.Message
                });
            }

            if (confirmed)
                return JsonConvert.SerializeObject(new { Confirmed = true });

            return JsonConvert.SerializeObject(new
            {
                Confirmed = false,
                Reason    = "User rejected the plan. Re-read the user's latest message for adjustments, revise the plan, and call propose_plan again before building."
            });
        }

        [AgentFunction("confirm_drop_tables",
            "Ask the user whether the physical database tables for a deleted application should be DROPped. " +
            "Call this AFTER propose_plan confirms the deletion intent, but BEFORE calling delete_application. " +
            "Pass the list of table names that would be dropped. " +
            "Returns {DropTables:true} if the user wants the tables removed, {DropTables:false} to keep them. " +
            "Pass the returned DropTables value as dropDatabaseTables to delete_application.")]
        public async Task<string> ConfirmDropTables(
            [AgentParam("Comma-separated list of database table names that will be dropped if the user confirms.", isRequired: true)]
            string tableNames)
        {
            var planEvent = new AgentPlanEvent
            {
                PlanSummary   = "The following database tables can be permanently dropped from the database. Do you want to delete them?",
                TablesToDrop  = ParseCsv(tableNames)
            };

            if (_onPlanReady == null)
                return JsonConvert.SerializeObject(new { DropTables = false, Note = "No confirmation callback — tables will NOT be dropped by default." });

            bool drop;
            try
            {
                drop = await _onPlanReady(planEvent).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { DropTables = false, Note = "Callback error — tables will NOT be dropped: " + ex.Message });
            }

            return JsonConvert.SerializeObject(new { DropTables = drop });
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
