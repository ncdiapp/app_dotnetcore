using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
using APP.Components.Dto;
using APP.Framework;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    /// <summary>
    /// Shared entry for PLM Import tools: Wizard HTTP and GenericAgent both call
    /// <see cref="AppAgentToolEngine.Dispatch"/> with these ToolConfigs.
    /// </summary>
    public static class PlmMigrationToolDispatch
    {
        public const string LibraryKey = "integration-plm-import";
        public const string DefaultSkillKey = "plm-data-import";

        public const string TypeName = "APP.BL.DataMigration.PlmMigration.PlmImportConnectPlugin";

        public static readonly string TestConnectionConfig =
            $"{{\"TypeName\":\"{TypeName}\",\"MethodName\":\"TestPlmConnection\"}}";

        public static readonly string DiscoverDataSourcesConfig =
            $"{{\"TypeName\":\"{TypeName}\",\"MethodName\":\"DiscoverPlmDataSources\"}}";

        public static readonly string GetImportSessionConfig =
            $"{{\"TypeName\":\"{TypeName}\",\"MethodName\":\"GetPlmImportSession\"}}";

        public static readonly string SaveImportSessionConfig =
            $"{{\"TypeName\":\"{TypeName}\",\"MethodName\":\"SavePlmImportSession\"}}";

        /// <summary>Build context from the current HTTP / overridden identity.</summary>
        public static AgentToolContext FromCurrentIdentity(string skillKey = DefaultSkillKey)
        {
            AppClientIdentity? identity = null;
            var raw = ServerContext.Instance?.CurrnetClientIdentity;
            if (raw is AppClientIdentity aci)
                identity = aci;

            return new AgentToolContext
            {
                ConnectionString = identity.HasValue ? identity.Value.CurrentUserDbConnectionString ?? "" : "",
                DatabaseName     = identity.HasValue ? identity.Value.CurrentUserDataBaseName ?? "" : "",
                SessionId        = Guid.NewGuid().ToString("N"),
                UserSessionId    = identity.HasValue ? identity.Value.SessionId?.ToString() ?? "" : "",
                SkillKey         = skillKey ?? DefaultSkillKey,
                UserId           = identity.HasValue && identity.Value.UserId != null
                    ? Convert.ToInt32(identity.Value.UserId) : 0,
                CompanyId        = identity.HasValue && identity.Value.CurrentWorkingCompanyId != null
                    ? Convert.ToInt32(identity.Value.CurrentWorkingCompanyId) : 0,
                DataSourceId     = identity.HasValue ? identity.Value.DataSourceId : 0,
                IsDeterministic  = false,
                WorkflowId       = "",
                ChatSessionKey   = ""
            };
        }

        public static Task<string> DispatchAsync(
            string toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext context = null,
            CancellationToken ct = default)
        {
            return AppAgentToolEngine.Dispatch(
                "BuiltIn",
                toolConfig,
                args ?? new Dictionary<string, string>(),
                context ?? FromCurrentIdentity(),
                ct);
        }

        public static string Dispatch(
            string toolConfig,
            IReadOnlyDictionary<string, string> args,
            AgentToolContext context = null)
        {
            return DispatchAsync(toolConfig, args, context)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public static T DeserializeResult<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;
            try
            {
                var err = JsonConvert.DeserializeObject<ToolErrorEnvelope>(json);
                if (err?.Error != null && typeof(T) != typeof(ToolErrorEnvelope))
                {
                    // BuiltIn executor error shape — let caller see via throw or raw
                }
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }

        private sealed class ToolErrorEnvelope
        {
            public string Error { get; set; }
        }
    }
}
