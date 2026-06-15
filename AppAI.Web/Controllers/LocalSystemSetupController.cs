using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

[ApiController]
[Route("webapi/[controller]/[action]")]
public class LocalSystemSetupController : ControllerBase
{
    [HttpGet]
    public void AppDataSourceRegister()
    {
        if (IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback))
        {
            // CRUD AppDataSourceRegister

        }

    }

    public void AppSetup()
    {
        if (IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress ?? IPAddress.Loopback))
        {
            // CRUD AppDataSourceRegister

        }

    }
}
