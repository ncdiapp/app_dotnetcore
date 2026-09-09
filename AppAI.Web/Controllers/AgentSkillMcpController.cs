using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using App.BL.CursorCloudAgent;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppAI.Web.Controllers;

/// <summary>
/// MCP HTTP endpoint for Agent Management CursorCloudAgents runtime.
/// Authenticated by per-session MCP bearer token (not AppAI login).
/// Tools come from AppAgentToolRegister for the session SkillKey.
/// </summary>
[Route("webapi/[controller]/[action]")]
[ApiController]
public class AgentSkillMcpController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Invoke(CancellationToken cancellationToken)
    {
        var token = ExtractBearer();
        if (string.IsNullOrWhiteSpace(token))
            return Unauthorized(new { error = "Missing MCP bearer token." });

        var session = CursorCloudAgentSessionStore.GetByMcpToken(token);
        if (session == null)
            return Unauthorized(new { error = "Invalid MCP token." });

        CursorCloudAgentContext.Current = session;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
                return BadRequest(new { error = "Empty body." });

            if (body.TrimStart().StartsWith("["))
            {
                var arr = JArray.Parse(body);
                var results = new JArray();
                foreach (var item in arr)
                {
                    if (item is not JObject obj) continue;
                    var handled = await AgentSkillMcpBL.HandleJsonRpcAsync(obj, cancellationToken).ConfigureAwait(false);
                    if (handled != null)
                        results.Add(JToken.FromObject(handled));
                }
                return Content(results.ToString(Formatting.None), "application/json");
            }

            var request = JObject.Parse(body);
            var response = await AgentSkillMcpBL.HandleJsonRpcAsync(request, cancellationToken).ConfigureAwait(false);
            if (response == null)
                return NoContent();
            return Content(JsonConvert.SerializeObject(response), "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        finally
        {
            CursorCloudAgentContext.Current = null;
        }
    }

    private string ExtractBearer()
    {
        var h = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(h)) return null;
        if (h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return h.Substring(7).Trim();
        return h.Trim();
    }
}
