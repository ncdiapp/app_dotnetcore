using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Platform BuiltIn catalog tools: data sources, SaaS applications, connection smoke-test.
    /// ToolConfig examples:
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"ListTenantDataSources"}
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"ListTenantSaasApplications"}
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.TenantCatalogPlugin","MethodName":"TestDataSourceConnection"}
    /// </summary>
    public class TenantCatalogPlugin
    {
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("list_tenant_data_sources",
            "List DataSourceRegisterId + name + databaseName for the current tenant company. Use for ask_user to pick PLM / PLMDW / ERP registers. Never returns connection strings.")]
        public Task<string> ListTenantDataSources(
            CancellationToken ct,
            [AgentParam("Optional target company id.")] int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.ListTenantDataSourcesJson(targetCompanyId));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("list_tenant_saas_applications",
            "List SaasApplicationId + ApplicationName for Gate-0 ask_user DDL. Slim list only (no TX/Search tree). Prefer over list_applications for Connect.")]
        public Task<string> ListTenantSaasApplications(
            CancellationToken ct,
            [AgentParam("Optional target company id.")] int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.ListTenantSaasApplicationsJson(targetCompanyId));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("test_data_source_connection",
            "Test a tenant AppDataSourceRegister by id (open SQL connection). Never pass a connection string. Returns isSuccess, dataSourceName, databaseName, serverVersion.")]
        public Task<string> TestDataSourceConnection(
            CancellationToken ct,
            [AgentParam("Tenant AppDataSourceRegister id.", isRequired: true)] int dataSourceRegisterId = 0,
            [AgentParam("Optional target company id.")] int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.TestDataSourceConnectionJson(dataSourceRegisterId, targetCompanyId));
        }
    }
}
