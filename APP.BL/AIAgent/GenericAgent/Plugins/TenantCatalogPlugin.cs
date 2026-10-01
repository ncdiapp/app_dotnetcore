using System.Threading;
using System.Threading.Tasks;
using App.BL.TenantBusiness;
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
        public Task<string> ListTenantDataSources(CancellationToken ct, int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.ListTenantDataSourcesJson(targetCompanyId));
        }

        public Task<string> ListTenantSaasApplications(CancellationToken ct, int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.ListTenantSaasApplicationsJson(targetCompanyId));
        }

        /// <param name="dataSourceRegisterId">Tenant AppDataSourceRegister id (never a connection string).</param>
        public Task<string> TestDataSourceConnection(
            CancellationToken ct,
            int dataSourceRegisterId = 0,
            int? targetCompanyId = null)
        {
            _ = ct;
            return Task.FromResult(TenantCatalogBL.TestDataSourceConnectionJson(dataSourceRegisterId, targetCompanyId));
        }
    }
}
