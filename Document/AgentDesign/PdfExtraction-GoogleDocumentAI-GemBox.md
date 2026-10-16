# PDF Tech Pack Extraction: Google Document AI and GemBox

## Purpose

`PdfExtractor` uses two complementary technologies to process PDF tech packs:

1. **Google Document AI** extracts structured document information.
2. **GemBox.Pdf** extracts embedded bitmap images from the original PDF.

The two tools have different responsibilities. Google Document AI is the primary document-understanding service; GemBox is the local PDF image-extraction fallback.

## Responsibilities

### Google Document AI

Google Document AI Layout Parser is used for:

- OCR and document text
- Table structure
- Layout blocks
- Image annotations and image descriptions, when returned by the processor
- Structured tech-pack data such as product information, BOM, trims, care instructions, and construction notes

The current client uses the Google Cloud Document AI `v1beta3` library and sends the PDF through Google Cloud Storage batch processing.

The request enables:

```text
EnableImageExtraction = true
EnableImageAnnotation = true
EnableTableAnnotation = true
ReturnImages = true
```

If the configured processor version rejects these optional layout settings with `InvalidArgument`, the extractor automatically retries the same PDF without optional `ProcessOptions`. This allows text and table extraction to complete.

### GemBox.Pdf

GemBox.Pdf runs locally against the original PDF after Document AI processing completes. It scans the complete PDF content tree using:

```csharp
page.Content.Elements.All()
```

This includes images nested inside PDF Form XObjects. Each `PdfImageContent` is saved as a PNG file.

GemBox is required because a PDF can contain embedded bitmap images that Google Document AI does not classify as semantic image blocks. Document AI is a document-understanding service; it does not export every low-level PDF image object.

## Output Naming

Image filenames identify the extraction source.

### Google Document AI images

```text
output/pdf-images/{jobId}/layout-image-0001.png
```

Possible Google image sources include:

- `dataUri`
- `blobAssetId`
- `gcsUri`

When Document AI returns a `gcsUri`, the extractor downloads the object from Google Cloud Storage and writes it to the Agent Files area.

### GemBox images

```text
output/pdf-images/{jobId}/embedded-page-0001-0001.png
```

For example:

```text
embedded-page-0001-0001.png
embedded-page-0001-0002.png
embedded-page-0002-0003.png
```

The page number is included in the filename. These files are created by GemBox.Pdf.

## Pure Data Output

The normalized Document AI result is saved as:

```text
output/pdf-extraction/{jobId}/pure-data.json
```

The file can contain:

- Source file name
- Document text
- Pages
- Entities
- Tables
- `documentLayout`
- Layout image metadata and descriptions, when returned by Document AI

The image files are stored separately under `output/pdf-images/{jobId}/`.

## Configuration

All Google Document AI settings are resolved from the tenant's `AppTenantSetting` records.

| SetupCode | Purpose |
|---|---|
| `GoogleDocumentAIProjectId` | Google Cloud project ID |
| `GoogleDocumentAILocation` | Processor location, for example `us` |
| `GoogleDocumentAIProcessorId` | Layout Parser processor ID |
| `GoogleDocumentAIBucket` | Google Cloud Storage bucket used for temporary input/output |
| `GoogleDocumentAIPollTimeoutMinutes` | Batch operation timeout |
| `GoogleDocumentAICredentialJson` | Server-side path to the tenant service-account JSON file, or encrypted credential JSON |

The service-account file must exist on the backend server. A local developer path is not valid unless the backend is running on that same machine.

GemBox PDF licensing is configured separately through application configuration or an environment variable:

```powershell
$env:GemBox__PdfLicense="YOUR-GEMBOX-PDF-LICENSE"
```

The application reads this value as:

```text
GemBox:PdfLicense
```

The backend must be restarted after changing the license environment variable.

## End-to-End Flow

```text
PDF uploaded to PdfExtractor
        |
        v
Upload temporary PDF to tenant Google Cloud Storage bucket
        |
        v
Google Document AI Layout Parser batch processing
        |
        +--> Structured text, tables, entities, and layout JSON
        |
        +--> Optional Document AI image blocks
        |
        v
Download Document AI output and normalize pure-data.json
        |
        v
GemBox.Pdf scans original PDF content tree
        |
        v
Save embedded bitmap images as embedded-page-*.png
        |
        v
Delete temporary Google Cloud Storage input/output objects
```

The normalized JSON and Agent Files remain available after temporary Google Cloud Storage objects are deleted.

## Example Result

For `JPAU-SP00-40-0036.pdf`:

- Google Document AI extracted the 13-page product, BOM, colorway, care, and construction data.
- GemBox.Pdf generated files with the `embedded-page-` prefix.
- The visible image artifacts were therefore extracted by GemBox, not Google Document AI.

## Troubleshooting

### `InvalidArgument` from Document AI

The extractor first sends the optional image and layout settings. If the processor rejects them, it retries without optional `ProcessOptions`. If the retry also fails, check:

1. The processor ID and location.
2. The active Layout Parser processor version.
3. Service-account permissions for Document AI and Cloud Storage.
4. PDF validity, encryption, page count, and MIME type.

### Only `pure-data.json` is generated

Check the image directory and filename prefix:

- `layout-image-*` means Document AI returned and persisted an image.
- `embedded-page-*` means GemBox extracted an embedded PDF bitmap.
- No image files means neither service produced a persistable image artifact.

### GemBox produces no images

Possible reasons include:

- The GemBox PDF license is missing or limited.
- The PDF contains only vector paths or page-rendered artwork.
- The image is represented through an unsupported PDF feature.

The extractor now uses `Elements.All()` to include nested Form XObjects. If images are still missing, the next fallback is page rasterization or crop-based rendering using Document AI bounding boxes.

## Source Code

Primary implementation:

```text
APP.BL/Document/PdfTechPackExtractor.cs
```

Google Document AI package:

```text
Google.Cloud.DocumentAI.V1Beta3
```

PDF image package:

```text
GemBox.Pdf
```
