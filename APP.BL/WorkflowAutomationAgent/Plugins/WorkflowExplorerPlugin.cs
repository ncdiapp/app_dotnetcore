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
    /// Provides the agent with read-only access to the current workflow state.
    /// ActionAttribute is stored as JSON in FormulaExpression column:
    ///   AppProjectWorkFlowActionConverter.cs → DeserializeObject&lt;AppActionAttributeDto&gt;(entity.FormulaExpression)
    /// The task tree is populated by SyncronizeWorkflowCommandNodeTreeFromActionList into
    ///   AppTransactionExDto.WorkflowCommandNodeTree (List&lt;AppProjectWorkFlowActionDto&gt;).
    /// </summary>
    public class WorkflowExplorerPlugin
    {
        private readonly int _transactionId;
        private readonly AppClientIdentity? _identity;

        public WorkflowExplorerPlugin(int transactionId, AppClientIdentity? identity)
        {
            _transactionId = transactionId;
            _identity      = identity;
        }

        [AgentFunction("get_workflow_state",
            "Returns the full current state of the workflow: task count, task names, action types, SQL statements, sort order, and notes. " +
            "Call this first when the user asks to modify an existing workflow, or to understand what tasks are already present before proposing changes.")]
        public Task<string> GetWorkflowState(
            [AgentParam("The transactionId of the workflow to inspect. If omitted, uses the workflow currently open in the editor.", isRequired: false)]
            int? transactionId = null)
        {
            try
            {
                int tid = transactionId ?? _transactionId;
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(tid, null);
                AppTransactionCommandBL.SyncronizeWorkflowCommandNodeTreeFromActionList(dto);

                var tree = dto?.WorkflowCommandNodeTree;
                if (tree == null || tree.Count == 0)
                    return Task.FromResult(JsonConvert.SerializeObject(new { TransactionId = tid, WorkflowName = dto?.TransactionName, TaskCount = 0, Tasks = new object[0], Note = "Workflow is empty — no tasks yet." }));

                var tasks = tree.Select((t, i) => new
                {
                    Id             = t.Id,
                    Name           = t.Name,
                    ActionType     = t.ActionType,
                    ActionTypeName = GetActionTypeName(t.ActionType),
                    SortOrder      = t.ActionFlowOrder ?? (i + 1),
                    SqlStatement   = t.ActionAttribute?.SqlStatement,
                    Notes          = t.Description,
                    IntegrationConfig = t.ActionAttribute?.IntegrationConfigJson
                }).ToList();

                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    TransactionId = tid,
                    WorkflowName  = dto.TransactionName,
                    TaskCount     = tasks.Count,
                    Tasks         = tasks
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Failed to load workflow: " + ex.Message }));
            }
        }

        private static string GetActionTypeName(int? actionType)
        {
            if (!actionType.HasValue) return "Unknown";
            switch (actionType.Value)
            {
                case 42:  return "Execute SQL Statement";
                case 49:  return "Save";
                case 50:  return "Refresh";
                case 59:  return "Call API Operation";
                case 60:  return "Send Email";
                case 61:  return "Send SMS";
                case 62:  return "Send Push Notification";
                case 63:  return "Send Message";
                case 66:  return "Import JSON";
                case 67:  return "Import Excel";
                case 68:  return "Execute External Exe";
                case 83:  return "Call Shopify API";
                case 84:  return "Call Google Sheets API";
                case 85:  return "Call Netsuite API";
                case 86:  return "Call External REST API";
                case 200: return "Composition Command (Root)";
                default:  return "ActionType " + actionType.Value;
            }
        }
    }
}
