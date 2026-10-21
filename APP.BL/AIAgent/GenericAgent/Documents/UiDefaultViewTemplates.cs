namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Built-in MVC-style Views used when TemplateContent key is empty / {{code_default}},
    /// and as the seed text written into DB (real HTML, not a marker-only map).
    /// Interactive Result grids use app Wijmo via React — these Views are for
    /// none-interactive Result / export twins.
    /// </summary>
    public static class UiDefaultViewTemplates
    {
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

        public const string CardsNoneInteractive = @"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8""/>
<title>{{ title }}</title>
<style>
body{margin:0;padding:12px;font-family:Arial,Helvetica,sans-serif;background:#f7f8fa;color:#1a1a1a}
h1{margin:0 0 12px;font-size:18px;font-weight:700;text-align:center}
.cards{display:flex;flex-wrap:wrap;gap:12px;align-items:stretch}
.card{background:#fff;border:1px solid #e6e8eb;border-radius:8px;padding:12px 14px;box-sizing:border-box;flex:1 1 calc(100%/{{ cards_per_row }} - 12px);min-width:180px;max-width:100%}
.card-title{font-size:13px;font-weight:700;margin:0 0 8px}
.field{font-size:10pt;margin:2px 0}
.label{color:#555;font-weight:700;margin-right:6px}
</style>
</head>
<body>
<h1 data-role=""title"">{{ title }}</h1>
<div class=""cards"" data-role=""cards"">
{{ for card in cards }}
<div class=""card"" data-role=""card"">
<div class=""card-title"" data-role=""title"">{{ card.title }}</div>
{{ for field in card.fields }}
<div class=""field""><span class=""label"" data-role=""label"">{{ field.label }}</span><span data-role=""value"">{{ field.value }}</span></div>
{{ end }}
</div>
{{ end }}
</div>
</body>
</html>";

        /// <summary>Interactive table View marker: Result host mounts app Wijmo (React); this HTML is export twin only.</summary>
        public const string TableInteractiveHostHint = @"<!DOCTYPE html>
<html><head><meta charset=""utf-8""/><title>{{ title }}</title></head>
<body>
<!-- interactive table: Result uses app Wijmo (license.js / @mescius). This twin is for export. -->
<p data-role=""title"">{{ title }}</p>
<p>Interactive grid is shown in the Result panel (application Wijmo).</p>
</body></html>";
    }
}
