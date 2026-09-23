using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using APP.Components.Dto.Document;
using Google.Cloud.DocumentAI.V1;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json.Linq;

namespace App.BL.Document;

public interface IPdfTechPackExtractor
{
    Task<PdfTechPackExtractionResultDto> ExtractAsync(
        PdfTechPackExtractionRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extracts a PDF tech pack into two explicit outputs:
/// PureData (Document AI JSON/text/tables/entities) and Images (page/figure artifacts).
///
/// Credentials are obtained through Google Application Default Credentials. The service
/// account should be attached to the hosting workload or supplied through GOOGLE_APPLICATION_CREDENTIALS;
/// no private key belongs in tenant configuration.
/// </summary>
public sealed class PdfTechPackExtractor : IPdfTechPackExtractor
{
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _processorId;
    private readonly string _bucket;
    private readonly TimeSpan _pollTimeout;

    public PdfTechPackExtractor(Microsoft.Extensions.Configuration.IConfiguration configuration)
        : this(
            Required(configuration["Google:DocumentAI:ProjectId"], "Google:DocumentAI:ProjectId"),
            configuration["Google:DocumentAI:Location"] ?? "us",
            Required(configuration["Google:DocumentAI:ProcessorId"], "Google:DocumentAI:ProcessorId"),
            Required(configuration["Google:DocumentAI:Bucket"], "Google:DocumentAI:Bucket"),
            ParseTimeout(configuration["Google:DocumentAI:PollTimeoutMinutes"]))
    {
    }

    public PdfTechPackExtractor(
        string projectId,
        string location,
        string processorId,
        string bucket,
        int pollTimeoutMinutes = 90)
    {
        _projectId = Required(projectId, "Google:DocumentAI:ProjectId");
        _location = string.IsNullOrWhiteSpace(location) ? "us" : location.Trim();
        _processorId = Required(processorId, "Google:DocumentAI:ProcessorId");
        _bucket = Required(bucket, "Google:DocumentAI:Bucket");
        _pollTimeout = TimeSpan.FromMinutes(Math.Clamp(pollTimeoutMinutes, 1, 240));
    }

    public async Task<PdfTechPackExtractionResultDto> ExtractAsync(
        PdfTechPackExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var jobId = Guid.NewGuid().ToString("N");
        var inputObject = $"document-ai/input/{request.CompanyId}/{jobId}/{SanitizeFileName(request.FileName)}";
        var outputPrefix = $"document-ai/output/{request.CompanyId}/{jobId}/";
        var inputUri = $"gs://{_bucket}/{inputObject}";
        var outputUri = $"gs://{_bucket}/{outputPrefix}";
        var storage = await StorageClient.CreateAsync();
        var outputObjects = new List<string>();

        try
        {
            await using (var input = new MemoryStream(request.PdfBytes, writable: false))
            {
                await storage.UploadObjectAsync(_bucket, inputObject, "application/pdf", input,
                    cancellationToken: cancellationToken);
            }

            var processorName = $"projects/{_projectId}/locations/{_location}/processors/{_processorId}";
            var client = await new DocumentProcessorServiceClientBuilder
            {
                Endpoint = $"{_location}-documentai.googleapis.com"
            }.BuildAsync(cancellationToken: cancellationToken);

            var batchRequest = new BatchProcessRequest
            {
                Name = processorName,
                InputDocuments = new BatchDocumentsInputConfig
                {
                    GcsDocuments = new GcsDocuments
                    {
                        Documents =
                        {
                            new GcsDocument { GcsUri = inputUri, MimeType = "application/pdf" }
                        }
                    }
                },
                DocumentOutputConfig = new DocumentOutputConfig
                {
                    GcsOutputConfig = new DocumentOutputConfig.Types.GcsOutputConfig
                    {
                        GcsUri = outputUri
                    }
                }
            };

            var operation = client.BatchProcessDocuments(batchRequest);
            var pollTask = operation.PollUntilCompletedAsync();
            var completed = await Task.WhenAny(
                pollTask,
                Task.Delay(_pollTimeout, cancellationToken));
            if (completed != pollTask)
                throw new TimeoutException($"Document AI batch processing did not complete within {_pollTimeout.TotalMinutes:0} minutes.");
            await pollTask;

            var documents = new List<JObject>();
            await foreach (var item in storage.ListObjectsAsync(_bucket, outputPrefix)
                .WithCancellation(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(item.Name) || !item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                outputObjects.Add(item.Name);
                await using var jsonStream = new MemoryStream();
                await storage.DownloadObjectAsync(_bucket, item.Name, jsonStream,
                    cancellationToken: cancellationToken);
                var json = System.Text.Encoding.UTF8.GetString(jsonStream.ToArray());
                var parsed = JObject.Parse(json);
                documents.Add(parsed["document"] as JObject ?? parsed);
            }

            if (documents.Count == 0)
                throw new InvalidOperationException("Document AI completed without a JSON output document.");

            var result = BuildResult(jobId, request.FileName, request.SessionKey, request.CompanyId, documents);
            result.Status = "Completed";
            return result;
        }
        finally
        {
            // Temporary cloud artifacts are deleted after extraction. The normalized data and
            // image files remain in the tenant's Generic Agent file area.
            try { await storage.DeleteObjectAsync(_bucket, inputObject, cancellationToken: CancellationToken.None); }
            catch { /* cleanup must not hide the extraction result */ }

            foreach (var outputObject in outputObjects)
            {
                try { await storage.DeleteObjectAsync(_bucket, outputObject, cancellationToken: CancellationToken.None); }
                catch { /* cleanup must not hide the extraction result */ }
            }
        }
    }

    private static PdfTechPackExtractionResultDto BuildResult(
        string jobId,
        string fileName,
        string sessionKey,
        int companyId,
        List<JObject> documents)
    {
        var pureData = new JObject
        {
            ["schemaVersion"] = "1.0",
            ["sourceFileName"] = fileName,
            ["documents"] = new JArray()
        };
        var images = new List<PdfTechPackImageDto>();
        var pageCount = 0;

        foreach (var document in documents)
        {
            var purePages = document["pages"]?.DeepClone() as JArray ?? new JArray();
            foreach (var purePage in purePages.OfType<JObject>())
                purePage.Remove("image");
            var normalized = new JObject
            {
                ["text"] = document["text"]?.DeepClone() ?? JValue.CreateNull(),
                ["pages"] = purePages,
                ["entities"] = document["entities"]?.DeepClone() ?? new JArray(),
                ["tables"] = ExtractTables(document)
            };
            ((JArray)pureData["documents"]!).Add(normalized);

            var pages = document["pages"] as JArray;
            if (pages == null) continue;
            pageCount += pages.Count;
            for (var index = 0; index < pages.Count; index++)
            {
                var pageNumber = index + 1;
                var page = pages[index] as JObject;
                var image = page?["image"] as JObject;
                var content = image?["content"]?.Value<string>();
                var mimeType = image?["mimeType"]?.Value<string>() ?? "image/png";
                var relativePath = string.Empty;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    var bytes = Convert.FromBase64String(content);
                    var extension = mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                    relativePath = GenericAgentFileBL.WriteBytes(
                        sessionKey,
                        $"output/pdf-images/{jobId}/page-{pageNumber:0000}.{extension}",
                        bytes,
                        companyId);
                }

                images.Add(new PdfTechPackImageDto
                {
                    PageNumber = pageNumber,
                    MimeType = mimeType,
                    RelativePath = relativePath,
                    SourceUri = image?["gcsUri"]?.Value<string>(),
                    ImageText = page?["layout"]?["textAnchor"]?.ToString()
                });
            }
        }

        return new PdfTechPackExtractionResultDto
        {
            JobId = jobId,
            SourceFileName = fileName,
            PageCount = pageCount,
            PureData = pureData,
            Images = images
        };
    }

    private static JArray ExtractTables(JObject document)
    {
        var tables = new JArray();
        foreach (var page in document["pages"] as JArray ?? new JArray())
        {
            foreach (var table in page["tables"] as JArray ?? new JArray())
                tables.Add(table.DeepClone());
        }
        return tables;
    }

    private static void Validate(PdfTechPackExtractionRequest request)
    {
        if (request.CompanyId <= 0) throw new ArgumentException("CompanyId is required.");
        if (string.IsNullOrWhiteSpace(request.SessionKey)) throw new ArgumentException("SessionKey is required.");
        if (request.PdfBytes.Length == 0) throw new ArgumentException("PDF content is required.");
        if (request.PdfBytes.Length > 100L * 1024 * 1024) throw new InvalidOperationException("PDF exceeds the 100 MB limit.");
    }

    private static string Required(string? value, string key) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Missing configuration: {key}")
            : value.Trim();

    private static int ParseTimeout(string? value) =>
        int.TryParse(value, out var minutes) ? minutes : 90;

    private static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "tech-pack.pdf" : safe.Replace(' ', '_');
    }
}
