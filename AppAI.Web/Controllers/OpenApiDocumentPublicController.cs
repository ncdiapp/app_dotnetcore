using App.BL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// Published OpenAPI Document JSON. Anonymous for now; close this route later.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("openapi-doc")]
public class OpenApiDocumentPublicController : ControllerBase
{
    [HttpGet("{code}")]
    public IActionResult Get(string code)
    {
        var root = $"{Request.Scheme}://{Request.Host}";
        var json = AppOpenApiDocumentBL.ReadPublished(code, root);
        if (string.IsNullOrWhiteSpace(json))
            return NotFound();
        return Content(json, "application/json");
    }
}
