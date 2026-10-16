using App.BL.AIAgent.GenericAgent;
using APP.OAuth;
using Microsoft.Extensions.Options;

namespace AppAI.Web.Mcp;

public static class OAuthAgentWiring
{
    /// <summary>
    /// Tells the in-app agent which MCP servers must be called as the signed-in user (OAuth:AgentTokenServerUrls) and how
    /// to mint that user's token. Does nothing unless OAuth is enabled and at least one URL is configured, so an agent
    /// keeps using its registered headers everywhere else.
    /// </summary>
    public static void UseAppOAuthAgentTokens(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<OAuthOptions>>().Value;
        if (!options.Enabled || options.AgentTokenServerUrls.Length == 0) return;

        var tokens = app.Services.GetRequiredService<AgentTokenService>();
        McpConnectionHelper.UserTokenServerUrls = new HashSet<string>(options.AgentTokenServerUrls.Select(u => u.Trim()), StringComparer.OrdinalIgnoreCase);
        McpConnectionHelper.UserTokenFactory = (userId, companyId) => tokens.Mint(new OAuthWebUser(userId, companyId, string.Empty));
        app.Logger.LogInformation("Agent MCP servers called as the signed-in user: {Urls}", string.Join(", ", McpConnectionHelper.UserTokenServerUrls));
    }
}
