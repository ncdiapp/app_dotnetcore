using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.BL.AppBuilderAgent;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework.Communication;
using Newtonsoft.Json;

namespace App.BL.WorkflowAutomationAgent.Plugins
{
    /// <summary>
    /// Provides save_workflow and explain_workflow tools.
    /// </summary>
    public class WorkflowSaverPlugin
    {
        private readonly int _transactionId;
        private readonly AppClientIdentity? _identity;

        public WorkflowSaverPlugin(int transactionId, AppClientIdentity? identity)
        {
            _transactionId = transactionId;
            _identity      = identity;
        }

        [AgentFunction("save_workflow",
            "Persists all pending workflow changes to the database and refreshes the editor. " +
            "Call this after all create_task / update_task / delete_task operations are complete.")]
        public Task<string> SaveWorkflow()
        {
            try
            {
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(_transactionId, null);
                if (dto == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Could not load workflow " + _transactionId }));

                var saveResult = AppTransactionCommandBL.SaveOneWorkflowAutomation(dto);
                if (saveResult.ValidationResult.HasErrors)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Save failed: " + string.Join("; ", saveResult.ValidationResult.Items.Select(i => i.Message)) }));

                return Task.FromResult(JsonConvert.SerializeObject(new { Saved = true, TransactionId = _transactionId }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "save_workflow failed: " + ex.Message }));
            }
        }

        [AgentFunction("explain_workflow",
            "Returns a human-readable plain-English explanation of what the workflow does: each task in order, what it performs, and how they connect. " +
            "Use this when the user asks 'what does this workflow do?' or 'summarize this workflow'.")]
        public Task<string> ExplainWorkflow()
        {
            try
            {
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(_transactionId, null);
                AppTransactionCommandBL.SyncronizeWorkflowCommandNodeTreeFromActionList(dto);

                var tree = dto?.WorkflowCommandNodeTree;
                if (tree == null || tree.Count == 0)
                    return Task.FromResult("This workflow is currently empty — no tasks have been added yet.");

                var lines = new List<string>();
                lines.Add(string.Format("Workflow: \"{0}\" ({1} tasks)\n", dto.TransactionName, tree.Count));

                for (int i = 0; i < tree.Count; i++)
                {
                    var t = tree[i];
                    string actionName = GetActionTypeName(t.ActionType);
                    string detail = "";

                    if (t.ActionType == 42 && t.ActionAttribute?.SqlStatement != null)
                        detail = " | SQL: " + t.ActionAttribute.SqlStatement.Trim().Replace("\n", " ").Substring(0, Math.Min(120, t.ActionAttribute.SqlStatement.Trim().Length)) + (t.ActionAttribute.SqlStatement.Length > 120 ? "..." : "");
                    else if ((t.ActionType == 83 || t.ActionType == 84 || t.ActionType == 85 || t.ActionType == 86) && t.ActionAttribute?.IntegrationConfigJson != null)
                        detail = " | Config: " + t.ActionAttribute.IntegrationConfigJson;

                    string notes = !string.IsNullOrWhiteSpace(t.Description) ? " — " + t.Description : "";
                    lines.Add(string.Format("  Step {0}: [{1}] {2}{3}{4}", i + 1, actionName, t.Name, notes, detail));
                }

                return Task.FromResult(string.Join("\n", lines));
            }
            catch (Exception ex)
            {
                return Task.FromResult("explain_workflow failed: " + ex.Message);
            }
        }

        private static string GetActionTypeName(int? actionType)
        {
            if (!actionType.HasValue) return "Unknown";
            switch (actionType.Value)
            {
                case 42:  return "Execute SQL";
                case 49:  return "Save";
                case 50:  return "Refresh";
                case 59:  return "Call API";
                case 60:  return "Send Email";
                case 61:  return "Send SMS";
                case 62:  return "Send Push Notification";
                case 63:  return "Send Message";
                case 66:  return "Import JSON";
                case 67:  return "Import Excel";
                case 68:  return "Execute Exe";
                case 83:  return "Shopify API";
                case 84:  return "Google Sheets API";
                case 85:  return "Netsuite API";
                case 86:  return "External REST API";
                default:  return "ActionType " + actionType.Value;
            }
        }
    }
}
