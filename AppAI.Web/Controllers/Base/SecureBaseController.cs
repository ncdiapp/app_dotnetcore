using AppAI.Web.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers.Base;

/// <summary>
/// Base for all secure controllers. Replaces SecureBaseWebApiController from .NET 4.8.
/// SessionValidationFilter (applied via ServiceFilter) enforces session-token auth
/// before every action — equivalent to the old Initialize() override.
/// </summary>
[ApiController]
[ServiceFilter(typeof(SessionValidationFilter))]
public abstract class SecureBaseController : ControllerBase
{
}
