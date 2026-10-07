using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.ApiProvider;
using APP.Framework.Plugin;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn tools for API Management (App API Provider + 3rd-party).
    /// ToolConfig:
    /// {"TypeName":"App.BL.AIAgent.GenericAgent.Plugins.ApiProviderPlugin","MethodName":"Search|Detail|Execute"}
    /// </summary>
    public class ApiProviderPlugin
    {
        public Task<string> Search(
            CancellationToken ct,
            AgentToolContext context = null,
            string query = null,
            string providerName = null,
            int take = 30)
        {
            _ = ct;
            _ = context;
            return Task.FromResult(ApiProviderCatalogBL.SearchJson(query, providerName, take));
        }

        public Task<string> Detail(
            CancellationToken ct,
            AgentToolContext context = null,
            string actionCode = null)
        {
            _ = ct;
            _ = context;
            return Task.FromResult(ApiProviderCatalogBL.DetailJson(actionCode));
        }

        public Task<string> Execute(
            CancellationToken ct,
            AgentToolContext context = null,
            string actionCode = null,
            string bodyJson = null,
            bool confirmed = false)
        {
            _ = ct;
            return ApiProviderExecuteBL.ExecuteAsync(actionCode, bodyJson, confirmed, context);
        }
    }
}
