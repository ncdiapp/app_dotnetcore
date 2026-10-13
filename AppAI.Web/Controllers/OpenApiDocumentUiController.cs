using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppAI.Web.Controllers;

/// <summary>
/// Swagger UI page and the script files already shipped in Swashbuckle. No CDN.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("openapi-ui")]
public class OpenApiDocumentUiController : ControllerBase
{
    private const string ResourcePrefix = "Swashbuckle.AspNetCore.SwaggerUI.node_modules.swagger_ui_dist.";

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["swagger-ui-bundle.js"] = "text/javascript; charset=utf-8",
        ["swagger-ui-standalone-preset.js"] = "text/javascript; charset=utf-8",
        ["swagger-ui.css"] = "text/css; charset=utf-8",
    };

    [HttpGet("")]
    public ContentResult Page()
    {
        return Content(PageHtml, "text/html; charset=utf-8");
    }

    [HttpGet("asset")]
    public IActionResult Asset([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !ContentTypes.TryGetValue(name, out var contentType))
            return NotFound();

        var assembly = Assembly.Load("Swashbuckle.AspNetCore.SwaggerUI");
        var stream = assembly.GetManifestResourceStream(ResourcePrefix + name);
        if (stream == null)
            return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(stream, contentType);
    }

    private const string PageHtml = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>OpenAPI</title>
    <link rel="stylesheet" href="/appai/openapi-ui/asset?name=swagger-ui.css" />
    <style>body { margin: 0; }</style>
</head>
<body>
    <div id="swagger-ui"></div>
    <script src="/appai/openapi-ui/asset?name=swagger-ui-bundle.js"></script>
    <script src="/appai/openapi-ui/asset?name=swagger-ui-standalone-preset.js"></script>
    <script>
        (function () {
            var params = new URLSearchParams(window.location.search);
            var spec = params.get('spec') || '';
            var hash = new URLSearchParams((window.location.hash || '').replace(/^#/, ''));
            var sid = hash.get('sid') || '';
            var allowed = false;
            try {
                var url = new URL(spec);
                var path = url.pathname || '';
                var sameOrigin = url.origin === window.location.origin;
                var published = /^\/openapi-doc\/[a-z0-9-]+$/i.test(path);
                var draft = /^\/appai\/webapi\/OpenApiDocument\/Spec$/i.test(path);
                allowed = sameOrigin && (published || draft);
                spec = url.toString();
            } catch (e) {
                allowed = false;
            }
            if (!allowed) {
                document.body.textContent = 'This page only loads an OpenAPI document from this server.';
                return;
            }
            window.ui = SwaggerUIBundle({
                url: spec,
                dom_id: '#swagger-ui',
                deepLinking: false,
                requestInterceptor: function (req) {
                    if (sid) req.headers['CurrentUserSessionId'] = sid;
                    return req;
                },
                presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                layout: 'StandaloneLayout'
            });
        })();
    </script>
</body>
</html>
""";
}
