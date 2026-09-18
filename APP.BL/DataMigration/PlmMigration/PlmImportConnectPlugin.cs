using System.Threading;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace APP.BL.DataMigration.PlmMigration
{
    /// <summary>
    /// Phase-1 BuiltIn wrappers for PLM Data Import Connect &amp; Discover.
    /// Wizard and GenericAgent both reach these via <see cref="App.BL.TenantBusiness.AppAgentToolEngine.Dispatch"/>.
    /// Later phases move bodies into ExternalDll; this façade stays the stable tool surface.
    /// </summary>
    public static class PlmImportConnectPlugin
    {
        [AgentTool("test_plm_connection",
            "Test a legacy PLM SQL Server connection string. Returns IsSuccess, ServerVersion, DatabaseName.")]
        public static Task<string> TestPlmConnection(
            [AgentParam("PLM SQL Server connection string", true)] string connectionString,
            [AgentParam("Optional target company id override")] int? targetCompanyId,
            AgentToolContext context,
            CancellationToken ct)
        {
            var result = PlmMigrationBL.TestPlmConnection(new PlmConnectionTestRequestDto
            {
                ConnectionString = connectionString,
                TargetCompanyId = targetCompanyId
            });
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }

        [AgentTool("discover_plm_data_sources",
            "Connect to PLM and discover pdmDataSource rows (PLM/ERP/DW/…). May register tenant data sources. Requires admin.")]
        public static Task<string> DiscoverPlmDataSources(
            [AgentParam("PLM SQL Server connection string", true)] string plmConnectionString,
            [AgentParam("Optional SaasApplicationId for the import target app")] int? saasApplicationId,
            [AgentParam("Optional target company id override")] int? targetCompanyId,
            [AgentParam("Optional existing AppPlmImportSession.SessionId")] int? sessionId,
            AgentToolContext context,
            CancellationToken ct)
        {
            var result = PlmMigrationBL.DiscoverPlmDataSources(new PlmDiscoverDataSourcesRequestDto
            {
                PlmConnectionString = plmConnectionString,
                SaasApplicationId = saasApplicationId,
                TargetCompanyId = targetCompanyId,
                SessionId = sessionId
            });
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }

        [AgentTool("get_plm_import_session",
            "Get the active PLM Data Import session for the current (or target) company.")]
        public static Task<string> GetPlmImportSession(
            [AgentParam("Optional target company id override")] int? targetCompanyId,
            AgentToolContext context,
            CancellationToken ct)
        {
            var result = PlmMigrationBL.GetActiveImportSession(targetCompanyId);
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }

        [AgentTool("save_plm_import_session",
            "Save / upsert the PLM Data Import wizard session (step state, encrypted PLM connection, discovery JSON). Pass sessionJson as PlmImportSessionDto.")]
        public static Task<string> SavePlmImportSession(
            [AgentParam("JSON of PlmImportSessionDto", true)] string sessionJson,
            AgentToolContext context,
            CancellationToken ct)
        {
            var dto = string.IsNullOrWhiteSpace(sessionJson)
                ? null
                : JsonConvert.DeserializeObject<PlmImportSessionDto>(sessionJson);
            var result = PlmMigrationBL.SaveImportSession(dto);
            return Task.FromResult(JsonConvert.SerializeObject(result));
        }
    }
}
