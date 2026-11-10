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
        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("api-provider-search",
            "Search published APIs in API Management (App API Provider + all 3rd-party). Matches ActionCode, description, provider name, input parameter names, and sample/output keys. "
            + "Then call api-provider-detail with actionCode.")]
        public Task<string> Search(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("Keyword: API name, description term, parameter, or output field", isRequired: true)] string query = null,
            [AgentParam("Optional provider name filter when user specifies a provider.")] string providerName = null,
            [AgentParam("Max hits (default 30, max 100).")] int take = 30)
        {
            _ = ct;
            _ = context;
            return Task.FromResult(ApiProviderCatalogBL.SearchJson(query, providerName, take));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("api-provider-detail",
            "Get one published API by actionCode: description, HTTP method, URL, input parameters, sanitized config, sample response, schema. Required: actionCode from api-provider-search.")]
        public Task<string> Detail(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("API ActionCode (route key under /webapi/DataIntegration/{actionCode})", isRequired: true)] string actionCode = null)
        {
            _ = ct;
            _ = context;
            return Task.FromResult(ApiProviderCatalogBL.DetailJson(actionCode));
        }

        // Managed Agent: description/params also in AppAgentLibraryTool; attribute documents the BuiltIn contract.
        [AgentTool("api-provider-execute",
            "Execute a published API via DataIntegration. Required: actionCode. Pass bodyJson as the POST JSON payload. "
            + "Write/mutating APIs require user confirmation or confirmed=true after approval.")]
        public Task<string> Execute(
            CancellationToken ct,
            AgentToolContext context = null,
            [AgentParam("API ActionCode", isRequired: true)] string actionCode = null,
            [AgentParam("JSON object payload for POST, e.g. {\"args\":{\"tabid\":1}}")] string bodyJson = null,
            [AgentParam("Required true for write APIs after user approval, or to skip ask_user when already approved.")] bool confirmed = false)
        {
            _ = ct;
            return ApiProviderExecuteBL.ExecuteAsync(actionCode, bodyJson, confirmed, context);
        }
    }
}
