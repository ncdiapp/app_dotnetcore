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
    <style>
        body { margin: 0; }
        .appai-provider-body { margin: 0 0 8px 16px; }
        .appai-provider-body > .opblock-tag-section > .opblock-tag { font-size: 14px; }
        .appai-provider > h3.opblock-tag { cursor: pointer; }
    </style>
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
            var tagOrder = {};
            window.ui = SwaggerUIBundle({
                url: spec,
                dom_id: '#swagger-ui',
                deepLinking: false,
                tagsSorter: function (a, b) {
                    var ai = Object.prototype.hasOwnProperty.call(tagOrder, a) ? tagOrder[a] : 1000;
                    var bi = Object.prototype.hasOwnProperty.call(tagOrder, b) ? tagOrder[b] : 1000;
                    return ai - bi;
                },
                responseInterceptor: function (res) {
                    try {
                        var body = res && (res.data || res.body);
                        if (typeof body === 'string') body = JSON.parse(body);
                        if (!body && res && typeof res.text === 'string') body = JSON.parse(res.text);
                        tagOrder = {};
                        ((body && body.tags) || []).forEach(function (tag, index) {
                            if (tag && tag.name) tagOrder[tag.name] = index;
                        });
                    } catch (e) { }
                    return res;
                },
                requestInterceptor: function (req) {
                    if (sid) req.headers['CurrentUserSessionId'] = sid;
                    return req;
                },
                onComplete: function () { nestProviderTags(); },
                presets: [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
                layout: 'StandaloneLayout'
            });

            var nesting = false;
            var separator = ' » ';
            function nestProviderTags() {
                if (nesting) return;
                var root = document.getElementById('swagger-ui');
                if (!root) return;
                var sections = Array.prototype.slice.call(root.querySelectorAll('.opblock-tag-section'));
                var groups = {};
                var groupOrder = [];
                sections.forEach(function (section) {
                    if (section.classList.contains('appai-provider')) return;
                    if (section.closest('.appai-provider-body')) return;
                    var heading = section.querySelector(':scope > h3.opblock-tag');
                    if (!heading) return;
                    var tag = heading.getAttribute('data-tag') || '';
                    var splitAt = tag.indexOf(separator);
                    if (splitAt < 0) return;
                    var provider = tag.slice(0, splitAt);
                    var typeName = tag.slice(splitAt + separator.length);
                    var label = heading.querySelector('a span');
                    if (label) label.textContent = typeName;
                    if (!groups[provider]) {
                        groups[provider] = [];
                        groupOrder.push(provider);
                    }
                    groups[provider].push(section);
                });
                if (!groupOrder.length) return;
                nesting = true;
                try {
                    groupOrder.forEach(function (provider) {
                        var list = groups[provider];
                        var first = list[0];
                        var parent = first.parentNode;
                        var sample = first.querySelector(':scope > h3.opblock-tag');
                        var header = sample.cloneNode(true);
                        header.removeAttribute('id');
                        header.setAttribute('data-tag', provider);
                        header.setAttribute('data-is-open', 'true');
                        var headerLabel = header.querySelector('a span');
                        if (headerLabel) headerLabel.textContent = provider;
                        var note = header.querySelector('small');
                        if (note) note.textContent = '';
                        var wrap = document.createElement('div');
                        wrap.className = 'opblock-tag-section is-open appai-provider';
                        var body = document.createElement('div');
                        body.className = 'appai-provider-body';
                        wrap.appendChild(header);
                        wrap.appendChild(body);
                        parent.insertBefore(wrap, first);
                        list.forEach(function (section) { body.appendChild(section); });
                        header.addEventListener('click', function (event) {
                            event.preventDefault();
                            var open = body.style.display !== 'none';
                            body.style.display = open ? 'none' : '';
                            header.setAttribute('data-is-open', open ? 'false' : 'true');
                            wrap.classList.toggle('is-open', !open);
                        });
                    });
                } finally {
                    nesting = false;
                }
            }
            var watch = new MutationObserver(function () {
                if (!nesting) nestProviderTags();
            });
            watch.observe(document.getElementById('swagger-ui'), { childList: true, subtree: true });
        })();
    </script>
</body>
</html>
""";
}
