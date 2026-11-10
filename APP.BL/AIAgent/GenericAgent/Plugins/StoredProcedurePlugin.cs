using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.StoredProcedure;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tools for discovering and executing tenant DataSource stored procedures.
    /// ToolConfig examples:
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"List"}
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Search"}
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Detail"}
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.StoredProcedurePlugin","MethodName":"Execute"}
    /// </summary>
    public class StoredProcedurePlugin
    {
        private readonly int? _dataSourceId;

        public StoredProcedurePlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("stored_procedure_list",
            "List AI-registered stored procedures (published) for a DataSource from AppStoredProcedureRegister. Optional dataSourceId; omit for session default. Prefer stored_procedure_search for keyword discovery.")]
        public Task<string> List(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("Optional DataSourceRegisterId; omit for session default.")] int? dataSourceId = null,
            [AgentParam("Optional schema filter (e.g. dbo).")] string schema = null,
            [AgentParam("Offset (default 0).")] int skip = 0,
            [AgentParam("Page size (default 50, max 200).")] int take = 50)
        {
            _ = ct;
            _ = schema;
            // AI register only (published). Optional dataSourceId; else all DS / session default hint.
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.ListForAgentJson(ds, skip, take));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("stored_procedure_search",
            "Search AI-registered stored procedures by keyword. Only IsPublishedToAgent rows. Then stored_procedure_detail → stored_procedure_execute.")]
        public Task<string> Search(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("Keyword (procedure name fragment or business term)", isRequired: true)] string query = null,
            [AgentParam("Optional DataSourceRegisterId; omit for session default.")] int? dataSourceId = null,
            [AgentParam("Max hits (default 30, max 100).")] int take = 30)
        {
            _ = ct;
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.SearchForAgentJson(ds, query, take));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("stored_procedure_detail",
            "Get one AI-registered stored procedure: description, input parameters JSON, output columns, sample JSON. Call before stored_procedure_execute.")]
        public Task<string> Detail(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("Procedure name or schema.name", isRequired: true)] string procedureName = null,
            [AgentParam("Optional schema if not embedded in procedureName.")] string schema = null,
            [AgentParam("Optional DataSourceRegisterId; omit for session default.")] int? dataSourceId = null)
        {
            _ = ct;
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.DetailForAgentJson(ds, procedureName, schema));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("stored_procedure_execute",
            "Execute a stored procedure on the chosen DataSource. Required: procedureName. Pass argsJson as a JSON object of parameter names to values.")]
        public Task<string> Execute(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("Procedure name or schema.name", isRequired: true)] string procedureName = null,
            [AgentParam("JSON object of SP arguments, e.g. {\"TabId\":123,\"ReferenceId\":1001}")] string argsJson = null,
            [AgentParam("Optional schema.")] string schema = null,
            [AgentParam("Optional DataSourceRegisterId; omit for session default.")] int? dataSourceId = null)
        {
            _ = ct;
            if (!TryResolveDs(dataSourceId, context, out var ds, out var err))
                return Task.FromResult(err);
            return Task.FromResult(StoredProcedureExecuteBL.ExecuteJson(ds, procedureName, schema, argsJson, context));
        }

        /// <summary>Optional DS for register search/list/detail (null = all published registers).</summary>
        private int? ResolveOptionalDs(int? dataSourceId, AgentToolContext context)
        {
            if (dataSourceId is > 0) return dataSourceId.Value;
            if (_dataSourceId is > 0) return _dataSourceId.Value;
            if (context != null && context.DataSourceId > 0) return context.DataSourceId;
            return null;
        }

        private bool TryResolveDs(int? dataSourceId, AgentToolContext context, out int ds, out string errorJson)
        {
            ds = dataSourceId is > 0
                ? dataSourceId.Value
                : (_dataSourceId is > 0 ? _dataSourceId.Value
                    : (context != null && context.DataSourceId > 0 ? context.DataSourceId : 0));

            if (ds <= 0)
            {
                errorJson = JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = "No data source configured. Pass dataSourceId (DataSourceRegisterId) or use a session with a default DataSource."
                }, Formatting.Indented);
                return false;
            }

            errorJson = null;
            return true;
        }
    }
}
