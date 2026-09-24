using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent;
using APP.Components.Dto.Document;
using APP.Framework;
using Google.Cloud.DocumentAI.V1Beta3;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using GemBox.Pdf;
using GemBox.Pdf.Content;
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
/// All Document AI settings, including the tenant credential file path or encrypted
/// credential JSON, are resolved from AppTenantSetting for the current tenant.
/// </summary>
public sealed class PdfTechPackExtractor : IPdfTechPackExtractor
{
    public PdfTechPackExtractor()
    {
    }

    public async Task<PdfTechPackExtractionResultDto> ExtractAsync(
        PdfTechPackExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var configuration = request.Configuration ?? ResolveTenantConfiguration();
        var projectId = Required(configuration.ProjectId, "GoogleDocumentAIProjectId");
        var location = Required(configuration.Location, "GoogleDocumentAILocation");
        var processorId = Required(configuration.ProcessorId, "GoogleDocumentAIProcessorId");
        var bucket = Required(configuration.Bucket, "GoogleDocumentAIBucket");
        var pollTimeoutMinutes = configuration.PollTimeoutMinutes.GetValueOrDefault();
        if (pollTimeoutMinutes < 1 || pollTimeoutMinutes > 240)
            throw new InvalidOperationException("GoogleDocumentAIPollTimeoutMinutes must be between 1 and 240.");
        var pollTimeout = TimeSpan.FromMinutes(pollTimeoutMinutes);

        var jobId = Guid.NewGuid().ToString("N");
        var inputObject = $"document-ai/input/{request.CompanyId}/{jobId}/{SanitizeFileName(request.FileName)}";
        var outputPrefix = $"document-ai/output/{request.CompanyId}/{jobId}/";
        var inputUri = $"gs://{bucket}/{inputObject}";
        var outputUri = $"gs://{bucket}/{outputPrefix}";
        var credentialJson = configuration.CredentialJson;
        if (string.IsNullOrWhiteSpace(credentialJson) && !string.IsNullOrWhiteSpace(configuration.CredentialFilePath))
            credentialJson = File.ReadAllText(ResolveCredentialFilePath(configuration.CredentialFilePath));

        if (string.IsNullOrWhiteSpace(credentialJson))
            throw new InvalidOperationException("Missing configuration: GoogleDocumentAICredentialJson");

        var credential = GoogleCredential.FromJson(credentialJson);
        var storage = await new StorageClientBuilder { Credential = credential }.BuildAsync();
        var outputObjects = new List<string>();

        try
        {
            await using (var input = new MemoryStream(request.PdfBytes, writable: false))
            {
            await storage.UploadObjectAsync(bucket, inputObject, "application/pdf", input,
                    cancellationToken: cancellationToken);
            }

            var processorName = $"projects/{projectId}/locations/{location}/processors/{processorId}";
            var client = await new DocumentProcessorServiceClientBuilder
            {
                Endpoint = $"{location}-documentai.googleapis.com",
                GoogleCredential = credential
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
                },
                ProcessOptions = new ProcessOptions
                {
                    LayoutConfig = new ProcessOptions.Types.LayoutConfig
                    {
                        EnableImageExtraction = true,
                        EnableImageAnnotation = true,
                        EnableTableAnnotation = true,
                        ReturnImages = true
                    }
                }
            };

            var operation = client.BatchProcessDocuments(batchRequest);
            var pollTask = operation.PollUntilCompletedAsync();
            var completed = await Task.WhenAny(
                pollTask,
                Task.Delay(pollTimeout, cancellationToken));
            if (completed != pollTask)
                throw new TimeoutException($"Document AI batch processing did not complete within {pollTimeout.TotalMinutes:0} minutes.");
            await pollTask;

            var documents = new List<JObject>();
            await foreach (var item in storage.ListObjectsAsync(bucket, outputPrefix)
                .WithCancellation(cancellationToken))
            {
                if (string.IsNullOrWhiteSpace(item.Name) || !item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                outputObjects.Add(item.Name);
                await using var jsonStream = new MemoryStream();
                await storage.DownloadObjectAsync(bucket, item.Name, jsonStream,
                    cancellationToken: cancellationToken);
                var json = System.Text.Encoding.UTF8.GetString(jsonStream.ToArray());
                var parsed = JObject.Parse(json);
                var nestedDocument = GetToken(parsed, "document") as JObject;
                if (nestedDocument != null)
                {
                    documents.Add(nestedDocument);
                    continue;
                }

                if (GetToken(parsed, "documents") is JArray documentArray)
                {
                    foreach (var documentItem in documentArray.OfType<JObject>())
                        documents.Add(documentItem);
                    continue;
                }

                documents.Add(parsed);
            }

            if (documents.Count == 0)
                throw new InvalidOperationException("Document AI completed without a JSON output document.");

            var result = await BuildResultAsync(
                jobId,
                request.FileName,
                request.SessionKey,
                request.CompanyId,
                bucket,
                storage,
                documents,
                cancellationToken);
            // GemBox fallback enabled for testing embedded bitmap images in the source PDF.
            ExtractEmbeddedPdfImages(request.PdfBytes, jobId, request.SessionKey, request.CompanyId, result);
            result.PureDataPath = $"output/pdf-extraction/{jobId}/pure-data.json";
            GenericAgentFileBL.WriteText(
                request.SessionKey,
                result.PureDataPath,
                result.PureData.ToString(Newtonsoft.Json.Formatting.None),
                request.CompanyId);
            result.Status = "Completed";
            return result;
        }
        finally
        {
            // Temporary cloud artifacts are deleted after extraction. The normalized data and
            // image files remain in the tenant's Generic Agent file area.
            try { await storage.DeleteObjectAsync(bucket, inputObject, cancellationToken: CancellationToken.None); }
            catch { /* cleanup must not hide the extraction result */ }

            foreach (var outputObject in outputObjects)
            {
                try { await storage.DeleteObjectAsync(bucket, outputObject, cancellationToken: CancellationToken.None); }
                catch { /* cleanup must not hide the extraction result */ }
            }
        }
    }

    public static PdfTechPackConfiguration ResolveTenantConfiguration()
    {
        var identity = (APP.Components.Dto.AppClientIdentity?)APP.Framework.ServerContext.Instance.CurrnetClientIdentity;
        var credentialSetting = identity.HasValue
            ? DecryptCredential(AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAICredentialJson, identity.Value))
            : null;
        var isCredentialJson = credentialSetting?.TrimStart().StartsWith("{", StringComparison.Ordinal) == true;
        return new PdfTechPackConfiguration
        {
            ProjectId = identity.HasValue ? AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAIProjectId, identity.Value) : null,
            Location = identity.HasValue ? AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAILocation, identity.Value) : null,
            ProcessorId = identity.HasValue ? AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAIProcessorId, identity.Value) : null,
            Bucket = identity.HasValue ? AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAIBucket, identity.Value) : null,
            PollTimeoutMinutes = int.TryParse(identity.HasValue ? AppTenantSettingBL.GetStringValue(APP.Components.Dto.EmTenantSettings.GoogleDocumentAIPollTimeoutMinutes, identity.Value) : null, out var timeout)
                ? timeout : null
            ,CredentialJson = isCredentialJson ? credentialSetting : null
            ,CredentialFilePath = isCredentialJson ? null : credentialSetting
        };
    }

    private static string? DecryptCredential(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : AppConnectionStringEncryptionBL.Decrypt(value);

    private static string ResolveCredentialFilePath(string configuredPath)
    {
        if (!Path.IsPathRooted(configuredPath))
            throw new InvalidOperationException("GoogleDocumentAICredentialJson must contain an absolute server file path.");

        var candidate = Path.GetFullPath(configuredPath);
        if (!File.Exists(candidate))
            throw new FileNotFoundException("Tenant Google credential file was not found.", candidate);
        return candidate;
    }

    private static async Task<PdfTechPackExtractionResultDto> BuildResultAsync(
        string jobId,
        string fileName,
        string sessionKey,
        int companyId,
        string bucket,
        StorageClient storage,
        List<JObject> documents,
        CancellationToken cancellationToken)
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
            var purePages = GetToken(document, "pages")?.DeepClone() as JArray ?? new JArray();
            foreach (var purePage in purePages.OfType<JObject>())
                purePage.Remove("image");
            var layout = GetToken(document, "documentLayout")?.DeepClone();
            var layoutText = ExtractLayoutText(layout);
            var normalized = new JObject
            {
                ["text"] = GetToken(document, "text")?.DeepClone()
                    ?? (!string.IsNullOrWhiteSpace(layoutText) ? new JValue(layoutText) : JValue.CreateNull()),
                ["pages"] = purePages,
                ["entities"] = GetToken(document, "entities")?.DeepClone() ?? new JArray(),
                ["tables"] = ExtractTables(document, layout),
                ["documentLayout"] = layout ?? JValue.CreateNull()
            };
            ((JArray)pureData["documents"]!).Add(normalized);

            var pages = GetToken(document, "pages") as JArray;
            if (pages != null)
            {
                pageCount += pages.Count;
                for (var index = 0; index < pages.Count; index++)
                {
                    var pageNumber = index + 1;
                    var page = pages[index] as JObject;
                    var image = GetToken(page, "image") as JObject;
                    var content = GetToken(image, "content")?.Value<string>();
                    var mimeType = GetToken(image, "mimeType")?.Value<string>() ?? "image/png";
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
                        SourceUri = GetToken(image, "gcsUri")?.Value<string>(),
                        ImageText = GetToken(GetToken(page, "layout") as JObject, "textAnchor")?.ToString()
                    });
                }
            }

            pageCount = Math.Max(pageCount, await AddLayoutImagesAsync(
                document,
                layout,
                jobId,
                sessionKey,
                companyId,
                bucket,
                storage,
                images,
                pageCount,
                cancellationToken));
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

    private static JArray ExtractTables(JObject document, JToken? layout)
    {
        var tables = new JArray();
        foreach (var page in (GetToken(document, "pages") as JArray) ?? new JArray())
        {
            foreach (var table in (GetToken(page as JObject, "tables") as JArray) ?? new JArray())
                tables.Add(table.DeepClone());
        }

        foreach (var tableBlock in FindLayoutChildren(layout, "tableBlock"))
            tables.Add(tableBlock.DeepClone());
        return tables;
    }

    private static string ExtractLayoutText(JToken? layout)
    {
        var text = FindLayoutChildren(layout, "textBlock")
            .Select(block => GetToken(block, "text")?.Value<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value));
        return string.Join(Environment.NewLine, text);
    }

    private static async Task<int> AddLayoutImagesAsync(
        JObject document,
        JToken? layout,
        string jobId,
        string sessionKey,
        int companyId,
        string bucket,
        StorageClient storage,
        List<PdfTechPackImageDto> images,
        int pageCount,
        CancellationToken cancellationToken)
    {
        foreach (var block in FindLayoutBlocks(layout, "imageBlock"))
        {
            var image = GetToken(block, "imageBlock") as JObject;
            if (image == null) continue;

            var pageNumber = GetToken(GetToken(block, "pageSpan") as JObject, "pageStart")?.Value<int>() ?? 0;
            pageCount = Math.Max(pageCount, pageNumber);
            var mimeType = GetToken(image, "mimeType")?.Value<string>() ?? "image/png";
            var dataUri = GetToken(image, "dataUri")?.Value<string>();
            var blobAssetId = GetToken(image, "blobAssetId")?.Value<string>();
            var relativePath = string.Empty;

            var base64 = dataUri;
            if (string.IsNullOrWhiteSpace(base64) && !string.IsNullOrWhiteSpace(blobAssetId))
            {
                var asset = (GetToken(document, "blobAssets") as JArray ?? new JArray())
                    .OfType<JObject>()
                    .FirstOrDefault(item => string.Equals(
                        GetToken(item, "assetId")?.Value<string>(),
                        blobAssetId,
                        StringComparison.Ordinal));
                base64 = GetToken(asset, "content")?.Value<string>();
                mimeType = GetToken(asset, "mimeType")?.Value<string>() ?? mimeType;
            }

            if (!string.IsNullOrWhiteSpace(base64))
            {
                var comma = base64.IndexOf(',');
                var encoded = comma >= 0 ? base64.Substring(comma + 1) : base64;
                var bytes = Convert.FromBase64String(encoded);
                var extension = mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                relativePath = GenericAgentFileBL.WriteBytes(
                    sessionKey,
                    $"output/pdf-images/{jobId}/layout-image-{images.Count + 1:0000}.{extension}",
                    bytes,
                    companyId);
            }
            else
            {
                var sourceUri = GetToken(image, "gcsUri")?.Value<string>();
                if (!string.IsNullOrWhiteSpace(sourceUri))
                {
                    var (sourceBucket, objectName) = ParseGcsUri(sourceUri, bucket);
                    await using var imageStream = new MemoryStream();
                    await storage.DownloadObjectAsync(
                        sourceBucket,
                        objectName,
                        imageStream,
                        cancellationToken: cancellationToken);
                    var extension = mimeType.Contains("jpeg", StringComparison.OrdinalIgnoreCase) ? "jpg" : "png";
                    relativePath = GenericAgentFileBL.WriteBytes(
                        sessionKey,
                        $"output/pdf-images/{jobId}/layout-image-{images.Count + 1:0000}.{extension}",
                        imageStream.ToArray(),
                        companyId);
                }
            }

            images.Add(new PdfTechPackImageDto
            {
                PageNumber = pageNumber,
                MimeType = mimeType,
                RelativePath = relativePath,
                SourceUri = GetToken(image, "gcsUri")?.Value<string>(),
                ImageText = GetToken(image, "imageText")?.Value<string>()
                    ?? GetToken(GetToken(image, "annotations") as JObject, "description")?.Value<string>()
            });
        }

        return pageCount;
    }

    private static (string Bucket, string ObjectName) ParseGcsUri(string uri, string fallbackBucket)
    {
        const string prefix = "gs://";
        if (!uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Document AI returned an unsupported image URI: {uri}");

        var path = uri.Substring(prefix.Length);
        var separator = path.IndexOf('/');
        if (separator <= 0 || separator == path.Length - 1)
            throw new InvalidOperationException($"Document AI returned an invalid image URI: {uri}");

        return (path[..separator], path[(separator + 1)..]);
    }

    private static void ExtractEmbeddedPdfImages(
        byte[] pdfBytes,
        string jobId,
        string sessionKey,
        int companyId,
        PdfTechPackExtractionResultDto result)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes, writable: false);
            var pdf = PdfDocument.Load(stream);
            result.PageCount = Math.Max(result.PageCount, pdf.Pages.Count);

            for (var pageIndex = 0; pageIndex < pdf.Pages.Count; pageIndex++)
            {
                // All() walks the complete content tree, including images nested in
                // Form XObjects. Walking only First/Next misses those images.
                foreach (var element in pdf.Pages[pageIndex].Content.Elements.All())
                {
                    if (element is PdfImageContent imageContent)
                    {
                        using var imageStream = new MemoryStream();
                        imageContent.Save(imageStream, new ImageSaveOptions(ImageSaveFormat.Png));
                        var relativePath = GenericAgentFileBL.WriteBytes(
                            sessionKey,
                            $"output/pdf-images/{jobId}/embedded-page-{pageIndex + 1:0000}-{result.Images.Count + 1:0000}.png",
                            imageStream.ToArray(),
                            companyId);
                        result.Images.Add(new PdfTechPackImageDto
                        {
                            PageNumber = pageIndex + 1,
                            MimeType = "image/png",
                            RelativePath = relativePath,
                            ImageText = "Embedded PDF image"
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Embedded PDF image extraction was unavailable: {ex.Message}");
        }
    }

    private static IEnumerable<JObject> FindLayoutBlocks(JToken? token, string childName)
    {
        if (token == null) yield break;
        if (token is JObject obj)
        {
            if (GetToken(obj, childName) is JObject)
                yield return obj;
            foreach (var child in obj.Properties().Select(property => property.Value))
            {
                foreach (var found in FindLayoutBlocks(child, childName))
                    yield return found;
            }
        }
        else if (token is JArray array)
        {
            foreach (var child in array)
            {
                foreach (var found in FindLayoutBlocks(child, childName))
                    yield return found;
            }
        }
    }

    private static IEnumerable<JObject> FindLayoutChildren(JToken? token, string childName)
    {
        foreach (var block in FindLayoutBlocks(token, childName))
        {
            if (GetToken(block, childName) is JObject child)
                yield return child;
        }
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

    private static JToken? GetToken(JObject? source, string name)
    {
        if (source == null) return null;
        var exact = source[name];
        if (exact != null) return exact;
        return source.Properties()
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "tech-pack.pdf" : safe.Replace(' ', '_');
    }
}
