using System;
using System.Threading;
using System.Threading.Tasks;
using App.BL.DbGenie;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tool that returns the tenant database schema on demand.
    /// Replaces the old InjectSchema capability flag (bit 32) which injected
    /// the schema unconditionally on every session start.
    ///
    /// ToolConfig: {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.SchemaContextPlugin","MethodName":"GetDatabaseSchema"}
    /// BuiltInToolExecutor injects context.DataSourceId via the int? constructor.
    /// </summary>
    public class SchemaContextPlugin
    {
        private readonly int? _dataSourceId;

        public SchemaContextPlugin(int? dataSourceId = null)
        {
            _dataSourceId = dataSourceId;
        }

        /// <param name="dataSourceId">Optional DataSourceRegisterId. When omitted or 0, uses the session default.</param>
        public async Task<string> GetDatabaseSchema(CancellationToken ct, int? dataSourceId = null)
        {
            var ds = dataSourceId is > 0 ? dataSourceId : _dataSourceId;
            if (!ds.HasValue || ds.Value == 0)
                return JsonConvert.SerializeObject(new { Error = "No data source configured for this agent." });
            try
            {
                var tables = await AppDbGenieBL.GetSchemaContextAsync(ds.Value).ConfigureAwait(false);
                var text = AppDbGenieBL.FormatSchemaContext(tables);
                if (string.IsNullOrWhiteSpace(text))
                    return JsonConvert.SerializeObject(new { DataSourceId = ds, Result = "No tables found in the connected database." });
                // Prefix so the LLM knows which DS the dump came from when probing multiple DBs.
                return $"DataSourceId={ds}\n{text}";
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message, DataSourceId = ds });
            }
        }
    }
}
