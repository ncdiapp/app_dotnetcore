using System;
using System.Linq;
using System.Threading.Tasks;
using APP.Components.Dto;
using APP.Framework.Plugin;
using APP.LBL.EntityClasses;
using App.BL;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Tools for the open transaction form. UI mutations wait on the form host.
    /// ListAppFiles reads AppFile on the server.
    /// </summary>
    public class FormHostPlugin
    {
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("get_form_state",
            "Read live values for named fields only. fields is required: JSON array of db names or labels from HostContextJson.")]
        public Task<string> GetFormState(
            AgentToolContext context,
            [AgentParam("Unit id or display name.")] string unitId = null,
            [AgentParam("Required JSON array of db names or labels to read.", isRequired: true)] string fields = null,
            [AgentParam("Optional row key.")] string rowKey = null,
            [AgentParam("JSON object of db or label to value.")] string match = null,
            [AgentParam("Max rows, default 20.")] string take = null,
            [AgentParam("Parent grid row for a subgrid.")] string parentRowKey = null) =>
            AskHost(context, "get_form_state", new { unitId, fields, rowKey, match, take, parentRowKey });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("describe_action", "Detail for one linked search, link target, or command, including the next hop on a link target.")]
        public Task<string> DescribeAction(
            AgentToolContext context,
            [AgentParam("linkedSearch | linkTarget | command", isRequired: true)] string kind = null,
            [AgentParam("Action id", isRequired: true)] string id = null) =>
            AskHost(context, "describe_action", new { kind, id });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("set_fields", "Set header or sibling 1-1 fields on the open form. Does not save. fieldsJson is db or label to value.")]
        public Task<string> SetFields(
            AgentToolContext context,
            [AgentParam("JSON object of field to value", isRequired: true)] string fieldsJson = null,
            [AgentParam("List Edit row key.")] string rowKey = null) =>
            AskHost(context, "set_fields", new { fieldsJson, rowKey });

        /// <summary>
        /// Add/update/delete grid rows on the posted FormData (Calculation round trip).
        /// One add call carries every new row. One update with no rowKey/match changes every row.
        /// </summary>
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("update_grid",
            "Add/update/delete rows on a form grid. Does not save. For update, pass match to change every matching row in one call.")]
        public Task<string> UpdateGrid(
            AgentToolContext context,
            [AgentParam("Grid unitId or display name from HostContextJson.units.")] string unitId = null,
            [AgentParam("add | update | delete", isRequired: true)] string op = null,
            [AgentParam("Optional single row key.")] string rowKey = null,
            [AgentParam("JSON object or array of row field values.")] string fieldsJson = null,
            [AgentParam("JSON object to select rows, e.g. {\"FabricCode\":\"Woven ABC123\"}.")] string match = null,
            [AgentParam("Parent grid row for a subgrid.")] string parentRowKey = null) =>
            AskHost(context, "update_grid", new { unitId, op, rowKey, fieldsJson, match, parentRowKey });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("open_linked_search", "Open the form's existing Linked Search popup. Returns after the user confirms or cancels.")]
        public Task<string> OpenLinkedSearch(
            AgentToolContext context,
            [AgentParam("Linked search id", isRequired: true)] string linkedSearchId = null,
            [AgentParam("Host unit id.")] string unitId = null,
            [AgentParam("Grid or list row the search starts from.")] string rowKey = null) =>
            AskHost(context, "open_linked_search", new { linkedSearchId, unitId, rowKey });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("open_link_target", "Open a configured Link Target using the existing popup or tab. Returns after the user finishes or cancels.")]
        public Task<string> OpenLinkTarget(
            AgentToolContext context,
            [AgentParam("Link target id", isRequired: true)] string linkTargetId = null,
            [AgentParam("Host unit id.")] string unitId = null,
            [AgentParam("Grid or list row.")] string rowKey = null) =>
            AskHost(context, "open_link_target", new { linkTargetId, unitId, rowKey });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("run_command", "Run a form command. Save, delete, and server commands require confirmed=true after ask_user. Refresh may omit confirmed.")]
        public Task<string> RunCommand(
            AgentToolContext context,
            [AgentParam("Command id.")] string commandId = null,
            [AgentParam("Save, Delete, Refresh, or the command display name.")] string commandName = null,
            [AgentParam("true after the user confirmed a save, delete, or server command.")] string confirmed = null) =>
            AskHost(context, "run_command", new { commandId, commandName, confirmed });

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("list_app_files",
            "List AppFile rows (FileId and name) for the current user. Then render cards and set_fields the FileId onto the image field.")]
        public Task<string> ListAppFiles(
            AgentToolContext context,
            [AgentParam("Optional name filter.")] string keyword = null,
            [AgentParam("Max rows, default 30, max 50.")] string take = null)
        {
            try
            {
                var limit = 30;
                if (int.TryParse(take, out var parsed) && parsed > 0)
                    limit = Math.Min(parsed, 50);
                var needle = (keyword ?? "").Trim();
                var rows = AppFileBL.RetrieveMyOwnAllAppFileEntity();
                var list = rows
                    .Cast<AppFileEntity>()
                    .Select(f => new { fileId = f.FileId, fileCode = f.FileCode })
                    .Where(f => needle.Length == 0
                        || (f.fileCode != null && f.fileCode.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Take(limit)
                    .ToList();
                return Task.FromResult(JsonConvert.SerializeObject(new { ok = true, files = list }));
            }
            catch (Exception ex)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { ok = false, error = ex.Message }));
            }
        }

        private static async Task<string> AskHost(AgentToolContext context, string toolName, object args)
        {
            var callbacks = AgentHitlBridge.Resolve(context?.ChatSessionKey);
            if (callbacks?.OnHostTool == null)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "This tool only runs inside an open transaction form."
                });
            }

            var evt = new FormHostToolEvent
            {
                RequestId = Guid.NewGuid().ToString("N"),
                ToolName = toolName,
                ArgsJson = JsonConvert.SerializeObject(args)
            };
            try
            {
                var result = await callbacks.OnHostTool(evt).ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result)
                    ? JsonConvert.SerializeObject(new { ok = false, error = "Empty host result." })
                    : result;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message });
            }
        }
    }
}
