using App.BL;
using APP.Components.EntityDto;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

// Public endpoint — no session required.
// Returns tenant branding info resolved from the HTTP Host header.
[ApiController]
[Route("webapi/[controller]/[action]")]
public class TenantController : ControllerBase
{
    [HttpGet]
    public AppTenantInfoDto Info()
    {
        // Prefer X-Forwarded-Host when sitting behind nginx/proxy; fall back to direct Host.
        var host = HttpContext.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                ?? HttpContext.Request.Host.Host
                ?? string.Empty;

        return AppDataSourceRegisterBL.GetTenantInfoByHost(host);
    }
}
