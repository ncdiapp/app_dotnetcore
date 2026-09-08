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

        public async Task<string> GetDatabaseSchema(CancellationToken ct)
        {
            if (!_dataSourceId.HasValue || _dataSourceId.Value == 0)
                return JsonConvert.SerializeObject(new { Error = "No data source configured for this agent." });
            try
            {
                var tables = await AppDbGenieBL.GetSchemaContextAsync(_dataSourceId.Value).ConfigureAwait(false);
                var text = AppDbGenieBL.FormatSchemaContext(tables);
                return string.IsNullOrWhiteSpace(text)
                    ? JsonConvert.SerializeObject(new { Result = "No tables found in the connected database." })
                    : text;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message });
            }
        }
    }
}
