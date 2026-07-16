using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using APP.Components.EntityDto;
using Microsoft.Playwright;

namespace App.BL
{
    public static class AppReportPdfService
    {
        /// <summary>
        /// Renders one or more report sections into a single PDF.
        /// Each section is separated by a CSS page-break.
        /// </summary>
        public static async Task<byte[]> GeneratePdfAsync(GeneratePdfRequest request)
        {
            var html = BuildCombinedHtml(request);
            return await RenderHtmlToPdfAsync(html, request.PageSize, request.Orientation, request.MarginMm);
        }

        /// <summary>Builds a single HTML document from all requested report sections.</summary>
        public static string BuildCombinedHtml(GeneratePdfRequest request)
        {
            var sections = new System.Text.StringBuilder();

            foreach (var section in request.Sections)
            {
                var template = AppReportTemplateBL.GetByReportId(section.ReportId);
                if (template == null) continue;

                var context = AppReportTemplateService.FetchData(
                    template,
                    section.ReferenceId ?? request.MainReferenceId,
                    request.MasterReferenceId,
                    request.ExtraParams);

                string rendered = AppReportTemplateService.RenderTemplate(
                    template.TemplateHtml, template, context);

                if (sections.Length > 0)
                    sections.Append("<div style=\"page-break-after:always\"></div>");

                sections.Append(rendered);
            }

            string pageSize = request.PageSize ?? "A4";
            string orient   = request.Orientation ?? "portrait";
            int    margin   = request.MarginMm > 0 ? request.MarginMm : 15;
            string pageRule = $"@page{{size:{pageSize} {orient};margin:{margin}mm}}";

            // PDF reset: zero out UA body margin and enforce border-box so that
            // table widths and padding don't cause column overflow/overlap in print.
            const string reset =
                "*, *::before, *::after{box-sizing:border-box}" +
                "body{margin:0;padding:0;font-family:Arial,sans-serif}" +
                "table{border-collapse:collapse;max-width:100%}" +
                "img{max-width:100%;height:auto;display:block}" +
                "td,th{word-wrap:break-word;overflow-wrap:break-word}";

            return $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>{pageRule}{reset}</style></head><body>{sections}</body></html>";
        }

        /// <summary>Uses Playwright headless Chromium to render HTML → PDF bytes.</summary>
        public static async Task<byte[]> RenderHtmlToPdfAsync(
            string html,
            string pageSize    = "A4",
            string orientation = "portrait",
            int    marginMm    = 15)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                // --no-sandbox is required when running as root or in containers/Linux servers
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" },
            });
            var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new PageSetContentOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            string margin = $"{marginMm}mm";
            return await page.PdfAsync(new PagePdfOptions
            {
                Format          = pageSize ?? "A4",
                Landscape       = string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase),
                PrintBackground = true,
                Margin = new Margin
                {
                    Top    = margin,
                    Bottom = margin,
                    Left   = margin,
                    Right  = margin,
                },
            });
        }
    }

    public class GeneratePdfRequest
    {
        public int                          MainReferenceId   { get; set; }
        public int?                         MasterReferenceId { get; set; }
        public List<ReportSectionRef>       Sections          { get; set; } = new();
        public string                       PageSize          { get; set; } = "A4";
        public string                       Orientation       { get; set; } = "portrait";
        public int                          MarginMm          { get; set; } = 15;
        public Dictionary<string, string>   ExtraParams       { get; set; }
    }

    public class ReportSectionRef
    {
        public int  ReportId    { get; set; }
        public int? ReferenceId { get; set; }
    }
}
