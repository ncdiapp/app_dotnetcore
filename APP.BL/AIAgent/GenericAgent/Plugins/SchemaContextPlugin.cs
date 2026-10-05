using System;
using System.Threading;
using System.Threading.Tasks;
using App.BL.DbGenie;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tool that returns the tenant database schema on demand.
    /// Results are cached by DataSourceRegisterId (1h TTL); chat session marks
    /// loaded DS ids so GenericAgentEngine can inject a "do not re-fetch" hint.
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
        public async Task<string> GetDatabaseSchema(
            CancellationToken ct,
            AgentToolContext context = null,
            int? dataSourceId = null)
        {
            var ds = dataSourceId is > 0 ? dataSourceId : _dataSourceId;
            if (!ds.HasValue || ds.Value == 0)
                return JsonConvert.SerializeObject(new { Error = "No data source configured for this agent." });

            try
            {
                var text = await AgentSchemaCacheBL.GetOrLoadAsync(ds.Value, async () =>
                {
                    var tables = await AppDbGenieBL.GetSchemaContextAsync(ds.Value).ConfigureAwait(false);
                    var body = AppDbGenieBL.FormatSchemaContext(tables);
                    if (string.IsNullOrWhiteSpace(body))
                        return JsonConvert.SerializeObject(new { DataSourceId = ds, Result = "No tables found in the connected database." });
                    return $"DataSourceId={ds}\n{body}";
                }).ConfigureAwait(false);

                if (context != null && !string.IsNullOrWhiteSpace(context.ChatSessionKey)
                    && !LooksLikeError(text))
                {
                    AgentSchemaCacheBL.MarkSessionLoaded(context.ChatSessionKey, ds.Value);
                }

                return text;
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { Error = ex.Message, DataSourceId = ds });
            }
        }

        private static bool LooksLikeError(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            var t = text.TrimStart();
            return t.StartsWith("{\"Error\"", StringComparison.OrdinalIgnoreCase)
                   || t.StartsWith("{\"error\"", StringComparison.OrdinalIgnoreCase);
        }
    }
}
