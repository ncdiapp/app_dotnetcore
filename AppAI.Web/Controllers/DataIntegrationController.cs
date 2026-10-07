using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using App.BL;
using AppAI.Web.Controllers.Base;
using ExchangeBL;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// Public App API Provider invoke endpoint.
/// Legacy URL: /webapi/DataIntegration/{ActionCode}
/// </summary>
[Route("webapi/DataIntegration")]
public class DataIntegrationController : SecureBaseController
{
    [HttpGet("Test")]
    public string Test()
    {
        return " Echo from App DataExchangeApi 111";
    }

    /// <summary>GET /webapi/DataIntegration/{actionName}</summary>
    [HttpGet("{actionName}")]
    public async Task<IActionResult> GetByActionName(string actionName)
    {
        return await ExecuteGet(actionName);
    }

    /// <summary>Legacy method-style route kept for compatibility.</summary>
    [HttpGet("GetAsync")]
    public async Task<IActionResult> GetAsync([FromQuery] string actionName)
    {
        return await ExecuteGet(actionName);
    }

    /// <summary>POST /webapi/DataIntegration/{actionName}</summary>
    [HttpPost("{actionName}")]
    public async Task<IActionResult> PostByActionName(string actionName)
    {
        return await ExecutePost(actionName);
    }

    [HttpPost("PostAsync")]
    public async Task<IActionResult> PostAsync([FromQuery] string actionName)
    {
        return await ExecutePost(actionName);
    }

    private async Task<IActionResult> ExecuteGet(string actionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return BadRequest("There is no action name");

            var queryParameters = Request.Query
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                .ToList();

            if (queryParameters.Any(kv => kv.Key.Equals("wsdl", StringComparison.InvariantCultureIgnoreCase)))
            {
                var exampleData = DataExchangeWithoutJsonSchemaBL.GetSample(actionName);
                if (!string.IsNullOrWhiteSpace(exampleData))
                    return Content(exampleData, "application/json");
                return Content("{}", "application/json");
            }

            var responseStream = await DataExchangeWithoutJsonSchemaBL.GetAsync(actionName, queryParameters);
            if (responseStream is MemoryStream ms)
                return File(ms.ToArray(), "application/json");

            return Content(responseStream?.ToString() ?? "", "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task<IActionResult> ExecutePost(string actionName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return BadRequest("There is no action name");

            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
                return BadRequest("There is no post data");

            var queryParameters = Request.Query
                .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value.ToString()))
                .ToList();

            var responseJson = DataExchangeWithoutJsonSchemaBL.ExecuteApiOperationSaveCommand(actionName, requestBody, queryParameters);
            return Content(responseJson ?? "", "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
