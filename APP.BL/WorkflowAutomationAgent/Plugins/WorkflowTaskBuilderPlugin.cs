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
    /// Provides create_task, update_task, and delete_task operations on the workflow.
    ///
    /// IMPORTANT: Each tool freshly loads the transaction from the database so it always
    /// operates on the latest state. After each write the transaction is re-saved.
    ///
    /// The agent MUST call propose_workflow_changes (WfAgentConfirmPlugin) before calling
    /// any tool in this plugin — the system prompt enforces this.
    /// </summary>
    public class WorkflowTaskBuilderPlugin
    {
        private readonly int _transactionId;
        private readonly AppClientIdentity? _identity;

        public WorkflowTaskBuilderPlugin(int transactionId, AppClientIdentity? identity)
        {
            _transactionId = transactionId;
            _identity      = identity;
        }

        [AgentFunction("create_task",
            "Creates a new operation task and appends it to the workflow. " +
            "MUST be preceded by an approved propose_workflow_changes call. " +
            "Returns the created task's Id, Name, and SortOrder.")]
        public Task<string> CreateTask(
            [AgentParam("Task name — shown in the editor and logs.", isRequired: true)]
            string name,
            [AgentParam("Action type ID: 42=Execute SQL, 49=Save, 50=Refresh, 59=CallAPI, 60=SendEmail, 61=SendSMS, 66=ImportJSON, 67=ImportExcel, 68=ExternalExe, 83=Shopify, 84=GoogleSheets, 85=Netsuite, 86=REST.", isRequired: true)]
            int actionType,
            [AgentParam("Full SQL statement — required when actionType=42. Write complete, valid SQL.")]
            string sqlStatement = null,
            [AgentParam("JSON config for external integration types 83-86 (e.g. {\"shopUrl\":\"...\",\"apiKey\":\"...\",\"resource\":\"Orders\",\"operation\":\"Get\"}).")]
            string integrationConfigJson = null,
            [AgentParam("Notes/description for the task — shown in the editor.")]
            string notes = null,
            [AgentParam("Sort order. If omitted the task is appended after all existing tasks.")]
            int? sortOrder = null)
        {
            try
            {
                // 1. Load the latest workflow state
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(_transactionId, null);
                if (dto?.CommandActionList == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Could not load workflow transaction " + _transactionId }));

                // 2. Find the root command (ActionType=200)
                var rootCommand = dto.CommandActionList.FirstOrDefault(o =>
                    o.ActionAttribute != null && o.ActionAttribute.IsWorkflowRootCommand);
                if (rootCommand == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Workflow root command not found. Save the workflow first." }));

                // 3. Determine sort order
                var childList = rootCommand.ActionAttribute.ChildActionList ?? new List<ChildTransactionCommandDto>();
                int newSort = sortOrder ?? (childList.Count > 0 ? childList.Max(c => c.Sort ?? 0) + 1 : 1);

                // 4. Build the new command DTO
                var newCmd = new AppProjectWorkFlowActionExDto
                {
                    CommandTransactionId = (int)dto.Id,
                    Name         = name,
                    ActionType   = actionType,
                    ActionFlowOrder = newSort,
                    Description  = notes,
                    ActionAttribute = new AppActionAttributeDto
                    {
                        LinkToUI             = false,
                        IsLogCommandStartEnd = false,
                        IsLogErrorDetails    = true,
                        SqlStatement         = sqlStatement,
                        IntegrationConfigJson = integrationConfigJson
                    }
                };

                // 5. Save the new command entity (gets a new Id)
                var createResult = AppTransactionCommandBL.CreateOneTransactionCommand(newCmd);
                if (createResult.ValidationResult.HasErrors || createResult.Object == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Failed to create task: " + string.Join("; ", createResult.ValidationResult.Items.Select(i => i.Message)) }));

                var savedCmd = createResult.Object;
                int newCmdId = savedCmd.Id != null ? (int)savedCmd.Id : 0;

                // 6. Add to root's ChildActionList and CommandActionList, then save
                childList.Add(new ChildTransactionCommandDto
                {
                    Sort           = newSort,
                    CommandId      = newCmdId,
                    CommandDisplay = name
                });
                rootCommand.ActionAttribute.ChildActionList = childList;
                dto.CommandActionList.Add(savedCmd);

                var saveResult = AppTransactionCommandBL.SaveOneWorkflowAutomation(dto);
                if (saveResult.ValidationResult.HasErrors)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Task created but save failed: " + string.Join("; ", saveResult.ValidationResult.Items.Select(i => i.Message)) }));

                return Task.FromResult(JsonConvert.SerializeObject(new WfAgentTaskResult
                {
                    TaskId     = newCmdId,
                    Name       = name,
                    ActionType = actionType,
                    SortOrder  = newSort
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "create_task failed: " + ex.Message }));
            }
        }

        [AgentFunction("update_task",
            "Updates one or more properties of an existing workflow task. " +
            "Only supply the fields you want to change — omitted parameters are left unchanged. " +
            "MUST be preceded by an approved propose_workflow_changes call.")]
        public Task<string> UpdateTask(
            [AgentParam("The TaskId returned by get_workflow_state or create_task.", isRequired: true)]
            int taskId,
            [AgentParam("New task name.")]
            string name = null,
            [AgentParam("New action type ID (see create_task for the list).")]
            int? actionType = null,
            [AgentParam("New SQL statement (for actionType=42).")]
            string sqlStatement = null,
            [AgentParam("New JSON config (for external integration types 83-86).")]
            string integrationConfigJson = null,
            [AgentParam("New notes/description.")]
            string notes = null,
            [AgentParam("New sort order.")]
            int? sortOrder = null)
        {
            try
            {
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(_transactionId, null);
                if (dto?.CommandActionList == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Could not load workflow." }));

                var cmd = dto.CommandActionList.FirstOrDefault(c => c.Id != null && (int)c.Id == taskId);
                if (cmd == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Task " + taskId + " not found in this workflow." }));

                if (name != null)     cmd.Name        = name;
                if (actionType != null) cmd.ActionType = actionType;
                if (notes != null)    cmd.Description = notes;
                if (sortOrder != null) cmd.ActionFlowOrder = sortOrder;

                if (cmd.ActionAttribute == null)
                    cmd.ActionAttribute = new AppActionAttributeDto();

                if (sqlStatement != null)        cmd.ActionAttribute.SqlStatement = sqlStatement;
                if (integrationConfigJson != null) cmd.ActionAttribute.IntegrationConfigJson = integrationConfigJson;

                var saveResult = AppTransactionCommandBL.SaveOneWorkflowAutomation(dto);
                if (saveResult.ValidationResult.HasErrors)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Update failed: " + string.Join("; ", saveResult.ValidationResult.Items.Select(i => i.Message)) }));

                return Task.FromResult(JsonConvert.SerializeObject(new WfAgentTaskResult
                {
                    TaskId     = taskId,
                    Name       = cmd.Name,
                    ActionType = cmd.ActionType ?? 0,
                    SortOrder  = cmd.ActionFlowOrder ?? 0
                }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "update_task failed: " + ex.Message }));
            }
        }

        [AgentFunction("delete_task",
            "Removes a task from the workflow and deletes it from the database. " +
            "MUST be preceded by an approved propose_workflow_changes call.")]
        public Task<string> DeleteTask(
            [AgentParam("The TaskId of the task to delete.", isRequired: true)]
            int taskId)
        {
            try
            {
                var dto = AppTransactionBL.GetHierarchyTranscationFromDatabase(_transactionId, null);
                if (dto?.CommandActionList == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Could not load workflow." }));

                var cmd = dto.CommandActionList.FirstOrDefault(c => c.Id != null && (int)c.Id == taskId);
                if (cmd == null)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Task " + taskId + " not found." }));

                string deletedName = cmd.Name;

                // Remove from CommandActionList
                dto.CommandActionList.Remove(cmd);

                // Remove from root's ChildActionList
                var rootCommand = dto.CommandActionList.FirstOrDefault(o =>
                    o.ActionAttribute != null && o.ActionAttribute.IsWorkflowRootCommand);
                if (rootCommand?.ActionAttribute?.ChildActionList != null)
                {
                    var ref_ = rootCommand.ActionAttribute.ChildActionList.FirstOrDefault(c => c.CommandId == taskId);
                    if (ref_ != null)
                        rootCommand.ActionAttribute.ChildActionList.Remove(ref_);
                }

                var saveResult = AppTransactionCommandBL.SaveOneWorkflowAutomation(dto);
                if (saveResult.ValidationResult.HasErrors)
                    return Task.FromResult(JsonConvert.SerializeObject(new { Error = "Delete failed: " + string.Join("; ", saveResult.ValidationResult.Items.Select(i => i.Message)) }));

                return Task.FromResult(JsonConvert.SerializeObject(new { Deleted = true, TaskId = taskId, Name = deletedName }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { Error = "delete_task failed: " + ex.Message }));
            }
        }
    }
}
