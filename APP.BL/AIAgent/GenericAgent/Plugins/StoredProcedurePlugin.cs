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
            if (!TryResolveDs(dataSourceId, context, out var ds, out var err))
                return Task.FromResult(err);
            return Task.FromResult(StoredProcedureCatalogBL.ListJson(ds, schema, skip, take));
        }

        public Task<string> Search(
            CancellationToken ct,
            AgentToolContext context = null,
            string query = null,
            int? dataSourceId = null,
            int take = 30)
        {
            _ = ct;
            if (!TryResolveDs(dataSourceId, context, out var ds, out var err))
                return Task.FromResult(err);
            return Task.FromResult(StoredProcedureCatalogBL.SearchJson(ds, query, take));
        }

        public Task<string> Detail(
            CancellationToken ct,
            AgentToolContext context = null,
            string procedureName = null,
            string schema = null,
            int? dataSourceId = null)
        {
            _ = ct;
            if (!TryResolveDs(dataSourceId, context, out var ds, out var err))
                return Task.FromResult(err);
            return Task.FromResult(StoredProcedureCatalogBL.DetailJson(ds, procedureName, schema));
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
