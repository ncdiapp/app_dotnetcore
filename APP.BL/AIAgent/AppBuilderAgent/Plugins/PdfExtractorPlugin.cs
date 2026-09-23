using System;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using App.BL.Document;
using APP.Components.Dto.Document;
using APP.Framework;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AppBuilderAgent.Plugins;

/// <summary>
/// Generic-agent BuiltIn wrapper for the Google Service/PdfExtractor library tool.
/// The input PDF must already be in the current GenericAgent session file area.
/// </summary>
public sealed class PdfExtractorPlugin
{
    [AgentTool("PdfExtractor",
        "Extract a PDF tech pack into pure structured data and image artifacts using Google Document AI. " +
        "The PDF must be uploaded to the current agent session first. Pass its relative file path, for example source/Bugaboo Coat.pdf.")]
    public async Task<string> Extract(
        AgentToolContext context,
        CancellationToken ct,
        string path)
    {
        try
        {
            if (context == null || context.CompanyId <= 0 || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                return JsonConvert.SerializeObject(new { Error = "Agent company and chat session context are required." });
            if (string.IsNullOrWhiteSpace(path))
                return JsonConvert.SerializeObject(new { Error = "path is required." });
            if (!path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return JsonConvert.SerializeObject(new { Error = "path must point to a PDF file." });

            var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, path, context.CompanyId);
            var extractor = new PdfTechPackExtractor(
                AppConfig.Get("Google:DocumentAI:ProjectId"),
                AppConfig.Get("Google:DocumentAI:Location") ?? "us",
                AppConfig.Get("Google:DocumentAI:ProcessorId"),
                AppConfig.Get("Google:DocumentAI:Bucket"),
                ParseTimeout(AppConfig.Get("Google:DocumentAI:PollTimeoutMinutes")));

            var extraction = await extractor.ExtractAsync(new PdfTechPackExtractionRequest
            {
                CompanyId = context.CompanyId,
                SessionKey = context.ChatSessionKey,
                FileName = path,
                PdfBytes = bytes,
                Configuration = PdfTechPackExtractor.ResolveTenantConfiguration()
            }, ct).ConfigureAwait(false);

            return JsonConvert.SerializeObject(new
            {
                extraction.JobId,
                extraction.Status,
                extraction.SourceFileName,
                extraction.PageCount,
                extraction.PureDataPath,
                Images = extraction.Images,
                extraction.Warnings,
                NextStep = "Read PureDataPath with file_read, then use the structured data to build or validate the application."
            });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { Error = ex.Message });
        }
    }

    private static int ParseTimeout(string value) =>
        int.TryParse(value, out var minutes) ? minutes : 90;
}
