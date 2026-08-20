using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AppDataIntegrationAgent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Controllers;

/// <summary>
/// MCP HTTP endpoint for Cursor Cloud Agents. Authenticated by the per-session MCP bearer token, not AppAI login.
/// </summary>
[ApiController]
[Route("webapi/[controller]/[action]")]
public class AppDataIntegrationAgentMcpController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Invoke(CancellationToken cancellationToken)
    {
        var token = ReadBearer();
        var session = AppDataIntegrationAgentSessionStore.GetByMcpToken(token);
        if (session == null)
            return Unauthorized(new { error = "Invalid MCP token." });

        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            body = await reader.ReadToEndAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(body))
            return Ok(new { jsonrpc = "2.0", result = new { } });

        AppDataIntegrationAgentContext.Current = session;
        try
        {
            var trimmed = body.TrimStart();
            if (trimmed.StartsWith("["))
            {
                var arr = JArray.Parse(body);
                var results = new JArray();
                foreach (var item in arr)
                {
                    var obj = item as JObject;
                    if (obj == null) continue;
                    var handled = await AppDataIntegrationAgentMcpBL.HandleJsonRpcAsync(obj, cancellationToken).ConfigureAwait(false);
                    if (handled != null)
                        results.Add(JToken.FromObject(handled));
                }
                return Content(results.ToString(Formatting.None), "application/json");
            }

            var request = JObject.Parse(body);
            var response = await AppDataIntegrationAgentMcpBL.HandleJsonRpcAsync(request, cancellationToken).ConfigureAwait(false);
            if (response == null)
                return NoContent();
            return Content(JsonConvert.SerializeObject(response), "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { jsonrpc = "2.0", error = new { code = -32000, message = ex.Message } });
        }
        finally
        {
            AppDataIntegrationAgentContext.Current = null;
        }
    }

    private string ReadBearer()
    {
        var header = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(header)) return "";
        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return header.Substring(prefix.Length).Trim();
        return header.Trim();
    }
}
