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
        public Task<string> GetFormState(AgentToolContext context, string unitId = null, string fields = null, string rowKey = null, string match = null, string take = null, string parentRowKey = null) =>
            AskHost(context, "get_form_state", new { unitId, fields, rowKey, match, take, parentRowKey });

        public Task<string> DescribeAction(AgentToolContext context, string kind = null, string id = null) =>
            AskHost(context, "describe_action", new { kind, id });

        public Task<string> SetFields(AgentToolContext context, string fieldsJson = null, string rowKey = null) =>
            AskHost(context, "set_fields", new { fieldsJson, rowKey });

        /// <summary>
        /// Add/update/delete grid rows on the posted FormData (Calculation round trip).
        /// One add call carries every new row. One update with no rowKey/match changes every row.
        /// </summary>
        public Task<string> UpdateGrid(AgentToolContext context, string unitId = null, string op = null, string rowKey = null, string fieldsJson = null, string match = null, string parentRowKey = null) =>
            AskHost(context, "update_grid", new { unitId, op, rowKey, fieldsJson, match, parentRowKey });

        public Task<string> OpenLinkedSearch(AgentToolContext context, string linkedSearchId = null, string unitId = null, string rowKey = null) =>
            AskHost(context, "open_linked_search", new { linkedSearchId, unitId, rowKey });

        public Task<string> OpenLinkTarget(AgentToolContext context, string linkTargetId = null, string unitId = null, string rowKey = null) =>
            AskHost(context, "open_link_target", new { linkTargetId, unitId, rowKey });

        public Task<string> RunCommand(AgentToolContext context, string commandId = null, string commandName = null, string confirmed = null) =>
            AskHost(context, "run_command", new { commandId, commandName, confirmed });

        public Task<string> ListAppFiles(AgentToolContext context, string keyword = null, string take = null)
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
