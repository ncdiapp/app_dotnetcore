namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Built-in MVC-style Views. Materialized into TemplateContent (see <see cref="UiTemplateContentMaterialize"/>).
    /// Interactive control LAYOUTs: table #gridHost, chart #chartHost, kpi Scriban items — React mounts Wijmo / hosts HTML.
    /// </summary>
    public static class UiDefaultViewTemplates
    {
        public const string FormDefault = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
.section{margin:0 0 16px;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:12px}
.section-title{font-size:13px;font-weight:700;margin:0 0 8px}
.field{display:flex;gap:8px;margin:4px 0;font-size:10pt}
.label{min-width:120px;color:#555;font-weight:700}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
{{ for section in sections }}
<div class=""section"" data-role=""section"">
<div class=""section-title"" data-role=""label"">{{ section.title }}</div>
{{ for field in section.fields }}
<div class=""field""><span class=""label"" data-role=""label"">{{ field.label }}</span><span data-role=""value"">{{ field.value }}</span></div>
{{ end }}
</div>
{{ end }}
</body>
</html>";

        public const string PageDefault = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<!-- page LAYOUT: platform stacks table + cards sections; edit chrome/CSS here -->
<div data-role=""page""></div>
</body>
</html>";

        /// <summary>None-interactive kpi/dashboard page shell (compose via UiDashboardHtml when code_default).</summary>
        public const string KpiDashboardStub = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<!-- dashboard page shell — platform stacks kpi/chart/grid controls -->
<div data-role=""dashboard""></div>
</body>
</html>";

        /// <summary>Interactive KPI control LAYOUT (designable). Bound via Scriban items.</summary>
        public const string KpiInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<link rel=""stylesheet"" href=""/agent-ui/agent-kpi.css""/>
</head>
<body>
<div class=""agent-kpi-root"" data-role=""kpi-root"">
<section class=""agent-kpi-row"" data-role=""kpi"">
{{ for item in items }}
<div class=""agent-kpi-card"" data-role=""kpi-card"" data-kpi-field=""{{ item.field }}"">
<div class=""agent-kpi-label"" data-role=""label"">{{ item.label }}</div>
<div class=""agent-kpi-value"" data-role=""value"">{{ item.value }}</div>
</div>
{{ end }}
</section>
</div>
</body>
</html>";

        /// <summary>Interactive chart control LAYOUT — keep id=""chartHost"" for React Wijmo mount.</summary>
        public const string ChartInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
.agent-chart-wrap{min-width:0;max-width:100%;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:8px;box-sizing:border-box}
#chartHost{width:100%;min-height:280px;max-width:100%;}
</style>
</head>
<body>
<div class=""agent-chart-wrap"" data-role=""chart"">
<div id=""chartHost""></div>
</div>
</body>
</html>";

        public const string ChartStub = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<!-- chart LAYOUT — none-interactive / export bind via UiDashboardHtml -->
<div data-role=""chart""></div>
</body>
</html>";

        public const string TableNoneInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
table{border-collapse:collapse;width:100%;background:#fff}
th,td{border:1px solid #dddddd;padding:4px 8px;font-size:10pt;vertical-align:top}
th{background:#3d4f5f;color:#ffffff;font-weight:700;text-align:left}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<div data-role=""grid-scroll"" style=""max-height:min(80vh,800px);overflow:auto"">
<table data-role=""grid"">
<thead><tr>
{{ for col in columns }}
<th data-role=""label"">{{ col }}</th>
{{ end }}
</tr></thead>
<tbody>
{{ for row in rows }}
<tr>
{{ for cell in row }}
<td data-role=""value"">{{ cell.text }}</td>
{{ end }}
</tr>
{{ end }}
</tbody>
</table>
</div>
</body>
</html>";

        /// <summary>
        /// Cards LAYOUT — uses public/agent-ui/agent-cards.css (Tailwind-equivalent utilities).
        /// </summary>
        public const string CardsNoneInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<link rel=""stylesheet"" href=""/agent-ui/agent-cards.css""/>
</head>
<body>
<div class=""agent-cards-root"" data-role=""cards-root"">
<div class=""agent-cards-title"" data-role=""title"">{{ title }}</div>
<div class=""agent-cards-grid"" data-role=""cards"" style=""--cards-per-row:{{ cards_per_row }}"">
{{ for card in cards }}
<article class=""agent-card"" data-role=""card"">
<div class=""agent-card-title"" data-role=""title"">{{ card.title }}</div>
<dl class=""agent-card-fields"">
{{ for field in card.fields }}
<div class=""agent-card-row"">
<dt class=""agent-card-label"" data-role=""label"">{{ field.label }}</dt>
{{ if field.is_image && field.image_src }}
<dd class=""agent-card-value"" data-role=""value"">
<img class=""agent-card-image"" data-role=""image"" src=""{{ field.image_src }}"" alt=""{{ field.label }}""/>
</dd>
{{ else }}
<dd class=""agent-card-value"" data-role=""value"">{{ field.value }}</dd>
{{ end }}
</div>
{{ end }}
</dl>
</article>
{{ end }}
</div>
</div>
</body>
</html>";

        /// <summary>
        /// Interactive table LAYOUT shell for TemplateContent customize.
        /// Keep id="gridHost" so Wijmo FlexGrid boot injects; edit chrome/CSS freely.
        /// </summary>
        public const string TableInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Segoe UI,Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a;max-width:100%;box-sizing:border-box}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<div class=""agent-wijmo-grid-wrap"" style=""max-height:min(70vh,720px);overflow:visible;background:#fff;border:1px solid #e2e5e9;border-radius:8px;padding:8px;min-height:200px;box-sizing:border-box;max-width:100%;position:relative;z-index:1;"">
<div id=""gridHost"" style=""width:100%;height:min(70vh,720px);max-width:100%;""></div>
</div>
</body>
</html>";
    }
}
