using System.Collections.Concurrent;
using APP.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

// End-to-end host: same pipeline shape as AppAI.Web (root rewrite, PathBase /appai, authentication) with a FAKE
// session bridge instead of AppAI's login, so the whole OAuth flow can be driven over HTTP.
var b = WebApplication.CreateBuilder(args);
b.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["OAuth:Enabled"] = "true",
    ["OAuth:PublicBaseUrl"] = "http://localhost:5097/appai",
    ["OAuth:ConsentPageUrl"] = "http://localhost:5097/appai/consent-page",
    ["OAuth:RegistrationEnabled"] = "true",
    ["OAuth:AgentTokenMinutes"] = "60",
});
b.Services.AddSingleton<FakeBridge>();
b.Services.AddSingleton<IOAuthIdentityBridge>(sp => sp.GetRequiredService<FakeBridge>());
b.Services.AddAppOAuth(b.Configuration, @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;Initial Catalog=AppMasterDB", isDevelopment: true);
b.Services.AddAuthentication();
var app = b.Build();

app.Use(async (c, next) => { if (c.Request.Path.StartsWithSegments("/.well-known")) c.Request.Path = new PathString("/appai") + c.Request.Path; await next(); });
app.UsePathBase("/appai");
app.UseRouting();
app.UseAuthentication();
app.MapAppOAuthDiscovery();
app.MapAppOAuthEndpoints();

// No seeding here on purpose: OAuthBootstrapService (started with the host) must create the "mcp" scope itself.

app.MapGet("/consent-page", () => "consent page stub");

// Fake web sign-in: the "session" is the X-Test-User header  userId:companyId:name
OAuthWebUser? Current(HttpContext c)
{
    var v = c.Request.Headers["X-Test-User"].ToString().Split(':');
    return v.Length == 3 && int.TryParse(v[0], out var u) && int.TryParse(v[1], out var co) ? new OAuthWebUser(u, co, v[2]) : null;
}
app.MapPost("/webapi/OAuthConsent/Describe", async (HttpContext c, OAuthConsentService s, UrlBody b) =>
    Current(c) is null ? Results.Unauthorized() : await Safe(async () => Results.Json(await s.DescribeAsync(b.AuthorizeUrl, default))));
app.MapPost("/webapi/OAuthConsent/Approve", async (HttpContext c, OAuthConsentService s, UrlBody b) =>
    Current(c) is not { } u ? Results.Unauthorized() : await Safe(async () => Results.Json(new { redirectUrl = await s.ApproveAsync(u, b.AuthorizeUrl, default) })));
app.MapPost("/webapi/OAuthConsent/Deny", async (HttpContext c, OAuthConsentService s, UrlBody b) =>
    Current(c) is null ? Results.Unauthorized() : await Safe(async () => Results.Json(new { redirectUrl = await s.DenyAsync(b.AuthorizeUrl, default) })));
app.MapGet("/webapi/OAuthAdmin/Connections", async (HttpContext c, OAuthConnectionAdmin a) =>
    Current(c) is not { } u ? Results.Unauthorized() : Results.Json(await a.ListAsync(u.CompanyId, null, default)));
app.MapPost("/webapi/OAuthAdmin/Revoke", async (HttpContext c, OAuthConnectionAdmin a, string id) =>
    Current(c) is not { } u ? Results.Unauthorized() : Results.Json(new { revoked = await a.RevokeAsync(id, u.CompanyId, null, default) }));
app.MapPost("/webapi/Agent/MintToken", (HttpContext c, AgentTokenService t) => Current(c) is not { } u ? Results.Unauthorized() : Results.Json(new { token = t.Mint(u, 5) }));
app.MapPost("/fake/deactivate", (FakeBridge br, int u) => { br.InactiveUsers[u] = true; return Results.Ok(); });
app.MapPost("/maintenance", async (IEnumerable<IHostedService> hs) => { await hs.OfType<OAuthMaintenanceService>().First().RunOnceAsync(default); return Results.Ok(); });

// Stand-in for the gateway's /mcp middleware: Bearer through OAuthBearerAuthenticator, 401 + challenge otherwise.
app.MapGet("/mcp", async (HttpContext c, OAuthBearerAuthenticator auth, OAuthChallenge challenge, FakeBridge bridge) =>
{
    var header = c.Request.Headers.Authorization.ToString();
    var bearer = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header[7..].Trim() : "";
    int u = 0, co = 0;
    if (bearer.Length > 0 && await auth.TryAuthenticateAsync(c, bearer, (user, company, session) => { u = user; co = company; }))
        return Results.Json(new { user = u, company = co, registered = bridge.LastRegistered });
    c.Response.Headers.WWWAuthenticate = challenge.Header(credentialRejected: bearer.Length > 0);
    return Results.StatusCode(401);
});
app.Run("http://localhost:5097");

static async Task<IResult> Safe(Func<Task<IResult>> f)
{
    try { return await f(); }
    catch (OAuthConsentException ex) { return Results.Json(new { error = ex.Message }, statusCode: 400); }
}

record UrlBody(string AuthorizeUrl);

sealed class FakeBridge : IOAuthIdentityBridge
{
    public record Session(int UserId, int CompanyId, DateTimeOffset Expires);
    public readonly ConcurrentDictionary<string, Session> Sessions = new();
    public readonly ConcurrentDictionary<int, bool> InactiveUsers = new();
    public string? LastRegistered;
    public OAuthWebUser? GetCurrentWebUser() => null;
    public string CreateGrantSession(int userId, int companyId, DateTimeOffset expiresUtc)
    { var id = Guid.NewGuid().ToString(); Sessions[id] = new Session(userId, companyId, expiresUtc); return id; }
    public void DeleteGrantSession(string id) => Sessions.TryRemove(id, out _);
    public bool IsGrantSessionValid(string id, int userId, int companyId) =>
        Sessions.TryGetValue(id, out var s) && s.UserId == userId && s.CompanyId == companyId && s.Expires > DateTimeOffset.UtcNow && !InactiveUsers.ContainsKey(userId);
    public bool TryRegisterGrantIdentity(string id, int userId, int companyId)
    { var ok = IsGrantSessionValid(id, userId, companyId); if (ok) LastRegistered = userId + "/" + companyId; return ok; }
}
