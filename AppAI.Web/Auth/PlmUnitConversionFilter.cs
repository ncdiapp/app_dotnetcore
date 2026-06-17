using Microsoft.AspNetCore.Mvc.Filters;

namespace AppAI.Web.Auth;

/// <summary>
/// Reads X-PLM-Unit request header (CM | INCH) and stores it in PlmUnitContext for
/// use by POM/QC endpoints. The actual field-level conversion is done by POM controllers
/// via PlmUnitContext helpers — not by this filter directly — because measurement fields
/// are endpoint-specific and not discoverable generically from the model.
///
/// Apply with [ServiceFilter(typeof(PlmUnitConversionFilter))] on POM controllers.
/// </summary>
public class PlmUnitConversionFilter : IActionFilter
{
    public const string HeaderName = "X-PLM-Unit";

    private readonly PlmUnitContext _unitContext;

    public PlmUnitConversionFilter(PlmUnitContext unitContext)
    {
        _unitContext = unitContext;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var raw = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault() ?? string.Empty;
        _unitContext.RequestUnit = raw.Equals("INCH", StringComparison.OrdinalIgnoreCase)
            ? PlmUnit.Inch
            : PlmUnit.Cm;
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
