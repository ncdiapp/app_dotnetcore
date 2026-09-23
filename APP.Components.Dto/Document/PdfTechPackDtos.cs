using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace APP.Components.Dto.Document;

public sealed class PdfTechPackExtractionResultDto
{
    public string JobId { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string Status { get; set; } = "Completed";
    public int PageCount { get; set; }
    public string PureDataPath { get; set; } = string.Empty;
    public JObject PureData { get; set; } = new JObject();
    public List<PdfTechPackImageDto> Images { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public sealed class PdfTechPackImageDto
{
    public int PageNumber { get; set; }
    public string MimeType { get; set; } = "image/png";
    public string RelativePath { get; set; } = string.Empty;
    public string? SourceUri { get; set; }
    public string? ImageText { get; set; }
}

public sealed class PdfTechPackExtractionRequest
{
    public int CompanyId { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
}
