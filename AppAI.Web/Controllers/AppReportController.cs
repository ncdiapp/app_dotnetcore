using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.BL;
using APP.Components.EntityDto;
using APP.Framework;
using AppAI.Web.Controllers.Base;
using AppAI.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AppAI.Web.Controllers;

/// <summary>
/// Print engine endpoints: PDF generation, HTML preview, token discovery, print request store.
/// </summary>
[Route("webapi/[controller]/[action]")]
public class AppReportController : SecureBaseController
{
    // ── PDF generation ────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfRequest request)
    {
        if (request?.Sections == null || request.Sections.Count == 0)
            return BadRequest("No report sections specified.");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var userId = (int)(ServerContext.Instance.CurrentUid ?? 0);

        byte[] pdfBytes = await AppReportPdfService.GeneratePdfAsync(request);
        sw.Stop();

        // Log each section
        foreach (var s in request.Sections)
        {
            try
            {
                AppReportTemplateBL.WriteLog(
                    reportId: s.ReportId,
                    requestId: null,
                    mainReferenceId: s.ReferenceId ?? request.MainReferenceId,
                    requestedBy: userId,
                    durationMs: (int)sw.ElapsedMilliseconds,
                    pageCount: 0,
                    clientIp: HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            catch { }
        }

        return File(pdfBytes, "application/pdf", "report.pdf");
    }

    // ── HTML preview (for template designer live preview) ─────────────────────

    [HttpPost]
    public IActionResult PreviewHtml([FromBody] PreviewHtmlRequest request)
    {
        if (request?.ReportId == null)
            return BadRequest("ReportId required.");

        var template = AppReportTemplateBL.GetByReportId(request.ReportId);

        // When no saved template exists but caller supplies a full HTML override,
        // build a minimal in-memory template so the preview still works.
        if (template == null)
        {
            if (string.IsNullOrEmpty(request.TemplateHtmlOverride))
                return NotFound("Report template not found.");

            template = new APP.Components.EntityDto.AppReportTemplateDto
            {
                ReportId     = request.ReportId,
                TemplateHtml = request.TemplateHtmlOverride,
                DataSpName   = request.DataSpNameOverride,
                PageSize     = "A4",
                Orientation  = "portrait",
                MarginMm     = 15,
            };
        }
        else if (!string.IsNullOrEmpty(request.TemplateHtmlOverride))
        {
            template.TemplateHtml = request.TemplateHtmlOverride;
        }

        var context = AppReportTemplateService.FetchData(
            template,
            request.MainReferenceId,
            request.MasterReferenceId,
            request.ExtraParams);

        string html = AppReportTemplateService.RenderTemplate(template.TemplateHtml, template, context);
        return Content(html, "text/html");
    }

    // ── Token discovery (for template designer token picker) ─────────────────

    [HttpGet]
    public IActionResult GetTokens(int reportId)
    {
        var template = AppReportTemplateBL.GetByReportId(reportId);
        if (template == null)
            return Ok(new List<TokenDescriptor>());

        var tokens = AppReportTemplateService.GetAvailableTokens(template);
        return Ok(tokens);
    }

    /// <summary>
    /// Token discovery from the designer's current (unsaved) config.
    /// Accepts the raw ExtraParamConfig JSON so sampleJson for API sources
    /// is included even before the template is saved.
    /// </summary>
    [HttpPost]
    public IActionResult GetTokensFromConfig([FromBody] GetTokensFromConfigRequest request)
    {
        var template = new APP.Components.EntityDto.AppReportTemplateDto
        {
            DataSpName       = request.DataSpName,
            ExtraParamConfig = request.ExtraParamConfig,
        };
        var tokens = AppReportTemplateService.GetAvailableTokens(template);
        return Ok(tokens);
    }

    // ── PDF preview (for template designer — opens inline in browser) ────────

    [HttpPost]
    public async Task<IActionResult> PreviewPdf([FromBody] PreviewPdfRequest request)
    {
        if (request?.ReportId == null)
            return BadRequest("ReportId required.");

        var template = AppReportTemplateBL.GetByReportId(request.ReportId);

        if (template == null)
        {
            if (string.IsNullOrEmpty(request.TemplateHtmlOverride))
                return NotFound("Report template not found.");

            template = new APP.Components.EntityDto.AppReportTemplateDto
            {
                ReportId     = request.ReportId,
                TemplateHtml = request.TemplateHtmlOverride,
                DataSpName   = request.DataSpNameOverride,
                PageSize     = request.PageSize ?? "A4",
                Orientation  = request.Orientation ?? "portrait",
                MarginMm     = request.MarginMm ?? 15,
            };
        }
        else
        {
            if (!string.IsNullOrEmpty(request.TemplateHtmlOverride))
                template.TemplateHtml = request.TemplateHtmlOverride;
            if (!string.IsNullOrEmpty(request.PageSize))
                template.PageSize = request.PageSize;
            if (!string.IsNullOrEmpty(request.Orientation))
                template.Orientation = request.Orientation;
            if (request.MarginMm.HasValue)
                template.MarginMm = request.MarginMm.Value;
        }

        var context = AppReportTemplateService.FetchData(
            template,
            request.MainReferenceId,
            request.MasterReferenceId,
            request.ExtraParams);

        string body    = AppReportTemplateService.RenderTemplate(template.TemplateHtml, template, context);
        string fullHtml = $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>{body}</body></html>";

        byte[] pdfBytes = await AppReportPdfService.RenderHtmlToPdfAsync(
            fullHtml,
            template.PageSize    ?? "A4",
            template.Orientation ?? "portrait",
            template.MarginMm);

        // No fileDownloadName → Content-Disposition: inline → browser renders the PDF
        return File(pdfBytes, "application/pdf");
    }

    // ── Print request store (for batch / search-view print) ──────────────────

    [HttpPost]
    public IActionResult CreateRequest([FromBody] CreatePrintRequestDto dto)
    {
        if (dto == null) return BadRequest();
        int userId = (int)(ServerContext.Instance.CurrentUid ?? 0);

        int requestId = AppReportTemplateBL.CreateRequest(
            dto.ReportId,
            dto.MainReferenceId,
            dto.MasterReferenceId,
            dto.MultipleReferenceIds,
            dto.ParameterMapping,
            userId);

        return Ok(new { RequestId = requestId });
    }

    // ── One-time print token (internal Playwright auth) ───────────────────────

    [HttpGet]
    public IActionResult ValidatePrintToken([FromQuery] string token)
    {
        var tokenSvc = HttpContext.RequestServices.GetService<PrintTokenService>();
        if (tokenSvc == null) return StatusCode(500);

        if (!tokenSvc.ValidateToken(token, out _, out string printParam))
            return Unauthorized();

        return Ok(new { Valid = true, PrintParam = printParam });
    }
}

public class PreviewHtmlRequest
{
    public int    ReportId              { get; set; }
    public int    MainReferenceId       { get; set; }
    public int?   MasterReferenceId     { get; set; }
    public string TemplateHtmlOverride  { get; set; }
    public string DataSpNameOverride    { get; set; }
    public Dictionary<string, string> ExtraParams { get; set; }
}

public class PreviewPdfRequest : PreviewHtmlRequest
{
    public string PageSize    { get; set; }
    public string Orientation { get; set; }
    public int?   MarginMm    { get; set; }
}

public class CreatePrintRequestDto
{
    public int    ReportId              { get; set; }
    public int    MainReferenceId       { get; set; }
    public int?   MasterReferenceId     { get; set; }
    public string MultipleReferenceIds  { get; set; }
    public string ParameterMapping      { get; set; }
}

public class GetTokensFromConfigRequest
{
    public string ExtraParamConfig { get; set; }
    public string DataSpName       { get; set; }
}
