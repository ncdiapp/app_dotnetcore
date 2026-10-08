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

        public Task<string> List(
            CancellationToken ct,
            AgentToolContext context = null,
            int? dataSourceId = null,
            string schema = null,
            int skip = 0,
            int take = 50)
        {
            _ = ct;
            _ = schema;
            // AI register only (published). Optional dataSourceId; else all DS / session default hint.
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.ListForAgentJson(ds, skip, take));
        }

        public Task<string> Search(
            CancellationToken ct,
            AgentToolContext context = null,
            string query = null,
            int? dataSourceId = null,
            int take = 30)
        {
            _ = ct;
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.SearchForAgentJson(ds, query, take));
        }

        public Task<string> Detail(
            CancellationToken ct,
            AgentToolContext context = null,
            string procedureName = null,
            string schema = null,
            int? dataSourceId = null)
        {
            _ = ct;
            var ds = ResolveOptionalDs(dataSourceId, context);
            return Task.FromResult(AppStoredProcedureRegisterBL.DetailForAgentJson(ds, procedureName, schema));
        }

        public Task<string> Execute(
            CancellationToken ct,
            AgentToolContext context = null,
            string procedureName = null,
            string argsJson = null,
            string schema = null,
            int? dataSourceId = null)
        {
            _ = ct;
            if (!TryResolveDs(dataSourceId, context, out var ds, out var err))
                return Task.FromResult(err);
            return Task.FromResult(StoredProcedureExecuteBL.ExecuteJson(ds, procedureName, schema, argsJson));
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
