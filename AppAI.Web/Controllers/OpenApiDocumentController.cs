using App.BL;
using AppAI.Web.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[Route("webapi/OpenApiDocument/[action]")]
public class OpenApiDocumentController : SecureBaseController
{
    [HttpGet]
    public IActionResult List()
    {
        return Ok(AppOpenApiDocumentBL.List());
    }

    [HttpGet]
    public IActionResult Get(int id)
    {
        var doc = AppOpenApiDocumentBL.Get(id);
        if (doc == null)
            return NotFound();
        return Ok(doc);
    }

    [HttpGet]
    public IActionResult ListSelectableApis()
    {
        return Ok(AppOpenApiDocumentBL.ListSelectableApis());
    }

    [HttpPost]
    public IActionResult Save([FromBody] AppOpenApiDocumentBL.SaveRequest request)
    {
        try
        {
            return Ok(AppOpenApiDocumentBL.Save(request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        AppOpenApiDocumentBL.Delete(id);
        return Ok();
    }

    [HttpPost]
    public IActionResult SetPublished(int id, bool published)
    {
        try
        {
            return Ok(AppOpenApiDocumentBL.SetPublished(id, published));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    public IActionResult Regenerate(int id)
    {
        try
        {
            return Ok(AppOpenApiDocumentBL.Regenerate(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public IActionResult Download(int id)
    {
        var root = $"{Request.Scheme}://{Request.Host}";
        var json = AppOpenApiDocumentBL.ReadForDownload(id, root);
        if (string.IsNullOrWhiteSpace(json))
            return NotFound();
        var doc = AppOpenApiDocumentBL.Get(id);
        var fileName = (doc?.Code ?? "openapi") + ".json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }
}
