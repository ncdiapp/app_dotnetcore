using System.Drawing;
using System.Text;
using App.BL;
using APP.Components.Dto;
using APP.Components.EntityDto;
using APP.Framework;
using ExchangeBL;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace AppAI.Web.Endpoints;

/// <summary>
/// Minimal API endpoints replacing legacy .ashx handlers and .aspx pages.
/// See MigrationPLan.md Phase 4 table for the full mapping.
/// </summary>
public static class LegacyEndpoints
{
    public static void Map(WebApplication app)
    {
        // ── Keep-alive / warmup (replaces AppServerWakeUp.aspx) ──────────────
        app.MapGet("/api/server/warmup", () => Results.Ok(new { status = "alive", utc = DateTime.UtcNow }))
           .WithName("ServerWarmup")
           .AllowAnonymous();

        // ── Server metadata (replaces AppServerSource.aspx) ──────────────────
        app.MapGet("/api/server/source", () => Results.Ok(new { version = "10.0" }))
           .WithName("ServerSource");

        // ── QR code display (replaces QRDisplay.aspx) ────────────────────────
        app.MapGet("/api/qr/{data}", (string data) =>
        {
            try
            {
                string[] paramArray = AppSaasAccountUserBL.DecryptParamString(data);
                string url = paramArray?.Length > 0
                    ? ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(paramArray[0]).Trim()
                    : data;
                string description = paramArray?.Length >= 2
                    ? ControlTypeValueConverter.ConvertValueToStringWithDefaultEmptyString(paramArray[1]).Trim()
                    : string.Empty;

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] pngBytes = qrCode.GetGraphic(20);
                string imgDataUrl = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";

                string html = $$"""
                    <!DOCTYPE html>
                    <html><head><meta charset="utf-8"><title>QR Code</title>
                    <style>body{font-family:sans-serif;text-align:center;padding:40px}</style>
                    </head><body>
                    <img src="{{imgDataUrl}}" alt="QR Code" style="max-width:300px"/>
                    {{(string.IsNullOrEmpty(description) ? "" : $"<p>{System.Net.WebUtility.HtmlEncode(description)}</p>")}}
                    </body></html>
                    """;
                return Results.Content(html, "text/html");
            }
            catch
            {
                return Results.Content("<html><body>Invalid Link</body></html>", "text/html");
            }
        }).WithName("QrDisplay").AllowAnonymous();

        // ── iCal export (replaces AppCalendarIcs.ashx) ───────────────────────
        // GET /api/calendar/{rootValueId}.ics?tid={transactionId}
        app.MapGet("/api/calendar/{rootValueId}.ics", (HttpContext ctx, int rootValueId, [FromQuery] int? tid) =>
        {
            if (!ValidateSession(ctx))
                return Results.Unauthorized();

            try
            {
                System.Net.Mail.Attachment? attachment = null;
                if (tid.HasValue)
                    attachment = EmailHelper.GenerateICSAttahment(tid.Value, rootValueId);

                string icsContent;
                if (attachment != null)
                {
                    using var reader = new StreamReader(attachment.ContentStream);
                    icsContent = reader.ReadToEnd();
                }
                else
                {
                    // Fallback: generate a placeholder event for the requested id
                    var placeholder = new List<ReservationDto>
                    {
                        new() { Summary = $"Event {rootValueId}", BeginDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddHours(1), Location = "" }
                    };
                    icsContent = EmailHelper.ConvertReserVationtoIcsContent(placeholder);
                }

                return Results.File(Encoding.UTF8.GetBytes(icsContent), "text/calendar", $"calendar-{rootValueId}.ics");
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        }).WithName("CalendarIcs");

        // ── File upload (replaces DataImage.aspx + DataImage.ashx) ──────────────
        // POST /api/files/upload?CallingFrom=...&TargetFolderId=...&UpdateFileId=...&TransactionId=...
        //   &IsNeedOcrProcess=...&ImportSettingDataSetId=...&DataSourceRegisterId=...
        //   &ApiOperationId=...&CommandId=...&FormDataGuid=...
        app.MapPost("/api/files/upload", async (HttpContext ctx,
            [FromServices] AppAI.Web.Services.IOcrService ocrService,
            [FromQuery] string? CallingFrom,
            [FromQuery] int? TransactionId,
            [FromQuery] int? TargetFolderId,
            [FromQuery] int? UpdateFileId,
            [FromQuery] bool? IsNeedOcrProcess,
            [FromQuery] int? ImportSettingDataSetId,
            [FromQuery] int? DataSourceRegisterId,
            [FromQuery] int? ApiOperationId,
            [FromQuery] int? CommandId,
            [FromQuery] string? FormDataGuid) =>
        {
            if (!ValidateSession(ctx))
                return Results.Unauthorized();

            if (!ctx.Request.HasFormContentType || ctx.Request.Form.Files.Count == 0)
                return Results.BadRequest("No file in request.");

            try
            {
                var formFile = ctx.Request.Form.Files[0];

                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    await formFile.CopyToAsync(ms);
                    fileBytes = ms.ToArray();
                }

                string fileName = formFile.FileName;
                string extension = Path.GetExtension(fileName);
                var docType = DocumentHelper.GetDocumentTypeByExtensionName(extension);
                var resultDict = new Dictionary<string, object?>();

                // ── UploadCompanyBackgrundImage ────────────────────────────────
                if (string.Equals(CallingFrom, EmCallingFrom.UploadCompanyBackgrundImage.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string folderPath = AppCompanyBL.GetCurrentCompanyBackgroundImageFolderPath();
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                        await File.WriteAllBytesAsync(Path.Combine(folderPath, fileName), fileBytes);
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = fileName });
                    }
                    catch (Exception ex)
                    {
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = "Upload failed. " + ex.Message });
                    }
                }

                // ── UploadCompanyLogoImage ─────────────────────────────────────
                if (string.Equals(CallingFrom, EmCallingFrom.UploadCompanyLogoImage.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string folderPath = AppCompanyBL.GetCurrentCompanyLogoImageFolderPath();
                        if (!Directory.Exists(folderPath))
                            Directory.CreateDirectory(folderPath);
                        else
                            foreach (var f in new DirectoryInfo(folderPath).GetFiles()) f.Delete();
                        await File.WriteAllBytesAsync(Path.Combine(folderPath, fileName), fileBytes);
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = fileName });
                    }
                    catch (Exception ex)
                    {
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = "Upload failed. " + ex.Message });
                    }
                }

                // ── UploadFileToWebSiteFolder ──────────────────────────────────
                // CallingFrom format: "UploadFileToWebSiteFolder|{esiteId}|{relativePath}"
                if (CallingFrom != null && CallingFrom.StartsWith(EmCallingFrom.UploadFileToWebSiteFolder.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        string[] spts = CallingFrom.Split('|');
                        var esiteId = ControlTypeValueConverter.ConvertValueToInt(spts.Length > 1 ? spts[1] : null);
                        string extractPath = AppEsiteFileBL.GetWebSiteBasePath(esiteId);
                        string relPath = spts.Length > 2 ? spts[2].Replace("/", "\\") : string.Empty;
                        if (relPath.StartsWith("\\")) relPath = relPath[1..];
                        if (relPath.StartsWith("src\\")) relPath = relPath[4..];
                        string destPath = Path.Combine(extractPath, relPath, fileName);
                        await File.WriteAllBytesAsync(destPath, fileBytes);
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = relPath });
                    }
                    catch (Exception ex)
                    {
                        return Results.Json(new Dictionary<string, object?> { ["ResultMessage"] = "Upload failed. " + ex.Message });
                    }
                }

                // ── ImportExcelToDatabase ──────────────────────────────────────
                if (string.Equals(CallingFrom, EmCallingFrom.ImportExcelToDatabase.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var docType2 = DocumentHelper.GetDocumentTypeByExtensionName(Path.GetExtension(fileName));
                    var rd = new Dictionary<string, object?>();
                    if (docType2 == EmAppDocumentType.EXCEL)
                    {
                        try
                        {
                            string tableName = ExcelImportExportBL.ImportExcelContentToDbTable(fileBytes, fileName, ImportSettingDataSetId, DataSourceRegisterId);
                            rd["ResultMessage"] = "Import successfully";
                            rd["ExcelUploadTableName"] = tableName;
                        }
                        catch (Exception ex) { rd["ResultMessage"] = "Import Failed. " + ex.Message; }
                    }
                    else
                    {
                        rd["ResultMessage"] = "Invalid excel file format";
                    }
                    return Results.Json(rd);
                }

                // ── APIOperationTesting ────────────────────────────────────────
                if (string.Equals(CallingFrom, EmCallingFrom.APIOperationTesting.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var rd = new Dictionary<string, object?>();
                    try
                    {
                        if (ApiOperationId.HasValue)
                        {
                            var opDto = AppIntergrationSettingBL.RetrieveOneAppIntergrationSettingParameterExDto(ApiOperationId.Value);
                            opDto.PayloadFile = new AppFileDto { FileCode = fileName, FileContent = fileBytes };
                            var testResult = DataExchangeSettingBL.GenerateSampleJsonDataFromApiConfig(opDto);
                            rd["ResultMessage"] = testResult.IsSuccessful ? "Upload successful." : "Api call failed.\n" + testResult.ValidationResult.LocalizedResult;
                        }
                        else
                        {
                            rd["ResultMessage"] = "Upload failed. Unknown ApiOperationId.";
                        }
                    }
                    catch (Exception ex) { rd["ResultMessage"] = "Upload failed. " + ex.Message; }
                    return Results.Json(rd);
                }

                // ── UploadFileByTransactionCommand ─────────────────────────────
                if (string.Equals(CallingFrom, EmCallingFrom.UploadFileByTransactionCommand.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var rd = new Dictionary<string, object?>();
                    try
                    {
                        if (TransactionId.HasValue && CommandId.HasValue)
                        {
                            var txExDto = AppCacheManagerBL.GetOnetHierarchyTranscationFromCache(TransactionId.Value);
                            if (txExDto?.TransactionOrganizedType == (int)EmTransactionOrganizedType.MasterDetail
                                && !string.IsNullOrWhiteSpace(FormDataGuid))
                            {
                                var formData = AppCacheManagerBL.GetMasterDetailFormDataFromCacheByGuid(FormDataGuid);
                                if (formData != null)
                                {
                                    AppCacheManagerBL.RemoveOneKeyFromDictGuidAndAppMasterDetailDto(FormDataGuid);
                                    formData.TransactionCommandId = CommandId.Value;
                                    formData.UploadedFileDto = new AppFileDto { FileCode = fileName, FileContent = fileBytes };
                                    var cmdResult = AppTransactionCommandBL.ExecuteTransactionRootCommand(formData);
                                    if (cmdResult.IsSuccessfulWithResult)
                                    {
                                        rd["FormData"] = cmdResult.Object.FormData;
                                        rd["ResultMessage"] = "Upload successful.";
                                    }
                                    else
                                    {
                                        rd["ResultMessage"] = "Api call failed.\n" + cmdResult.ValidationResult.LocalizedResult;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { rd["ResultMessage"] = "Upload failed. " + ex.Message; }
                    return Results.Json(rd);
                }

                // ── Excel upload path ──────────────────────────────────────────
                bool isExcel = string.Equals(CallingFrom, EmCallingFrom.ExcelUploadPorcess.ToString(), StringComparison.OrdinalIgnoreCase)
                            || string.Equals(CallingFrom, EmCallingFrom.UploadWebSiteZipPackage.ToString(), StringComparison.OrdinalIgnoreCase);

                if (isExcel)
                {
                    if (docType == EmAppDocumentType.EXCEL)
                    {
                        string tmpPath = AppCompanyBL.GetMyCompanyTempPath()
                                       + DocumentInfoDto.AppTemp + Guid.NewGuid();
                        Directory.CreateDirectory(Path.GetDirectoryName(tmpPath)!);
                        await File.WriteAllBytesAsync(tmpPath, fileBytes);
                        resultDict[DocumentInfoDto.FileOrgPath] = tmpPath;
                    }
                    else
                    {
                        resultDict[DocumentInfoDto.ExcelUploadTableName] = "";
                    }
                    return Results.Json(resultDict);
                }

                // ── Image / general file upload path ──────────────────────────
                if (fileName.Equals("image.jpg", StringComparison.OrdinalIgnoreCase))
                    fileName = AppSecurityUserBL.CurrentUserEntity.UserName
                             + " " + DateTime.UtcNow.ToShortDateString()
                             + " " + DateTime.UtcNow.ToLongTimeString() + ".jpg";

                // Resolve update-file link from embedded name encoding
                var idAndName = AppFileBL.GetFileIdAndNamePairFromFileIdNameStrng(fileName);
                if (idAndName.Key.HasValue && !string.IsNullOrWhiteSpace(idAndName.Value))
                {
                    UpdateFileId ??= idAndName.Key;
                    fileName = idAndName.Value;
                }

                extension = Path.GetExtension(fileName);
                docType = DocumentHelper.GetDocumentTypeByExtensionName(extension);

                string imagePath = AppCompanyBL.GetMyCompanyImagePath();
                string originalPath = imagePath + DocumentInfoDto.ImageOriginalSizeLocation + Guid.NewGuid();
                string thumbnailPath = imagePath + DocumentInfoDto.ImageThumbnailLocation + Guid.NewGuid();
                string regularPath = imagePath + DocumentInfoDto.ImageRegularSizeLocation + Guid.NewGuid();

                Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
                Directory.CreateDirectory(Path.GetDirectoryName(thumbnailPath)!);
                Directory.CreateDirectory(Path.GetDirectoryName(regularPath)!);

                var newDto = new AppFileExDto
                {
                    FileType = (int)docType,
                    OriginalFilePath = originalPath,
                    FileCode = fileName,
                    Extension = extension,
                };

                if (TargetFolderId.HasValue)
                    newDto.FolderId = TargetFolderId.Value;

                // Resolve InitialFileId for versioning
                if (UpdateFileId.HasValue)
                {
                    if (AppFileBL.CheckUploadEditFilePermission(UpdateFileId))
                    {
                        var existing = AppFileBL.RetrieveMultipleFileSimpleDtoByIds(new List<int> { UpdateFileId.Value });
                        if (existing.Count == 1)
                            newDto.InitialFileId = existing[0].InitialFileId ?? UpdateFileId.Value;
                    }
                    else
                    {
                        return Results.Json(new { error = "Invalid Request" }, statusCode: 403);
                    }
                }
                else
                {
                    if (!TargetFolderId.HasValue)
                        TargetFolderId = AppTenantSettingBL.GetIntValue(EmTenantSettings.PublicFileFolderId);

                    if (TargetFolderId.HasValue)
                    {
                        var existing = AppFileBL.RetrieveOneFileByCreatdByAndFileName(fileName, TargetFolderId.Value);
                        if (existing != null)
                            newDto.InitialFileId = existing.InitialFileId ?? (int)existing.Id;
                    }
                }

                // Write image files with resize; store documents in DB
                bool isImage = docType is EmAppDocumentType.JPG or EmAppDocumentType.GIF
                                         or EmAppDocumentType.PNG or EmAppDocumentType.BMP
                                         or EmAppDocumentType.TIF;

                bool isDocument = docType is EmAppDocumentType.PDF or EmAppDocumentType.EXCEL
                                            or EmAppDocumentType.WORD or EmAppDocumentType.TXT
                                            or EmAppDocumentType.PPT;

                if (isImage)
                {
                    byte[]? thumbnail = MakeThumbnail(fileBytes);
                    byte[]? regular = MakeRegularSize(fileBytes);

                    if (thumbnail != null)
                    {
                        await File.WriteAllBytesAsync(thumbnailPath, thumbnail);
                        newDto.ThumbnailFilePath = thumbnailPath;
                    }
                    if (regular != null)
                    {
                        await File.WriteAllBytesAsync(regularPath, regular);
                        newDto.RegularImageFilepath = regularPath;
                    }
                    await File.WriteAllBytesAsync(originalPath, fileBytes);

                    if (IsNeedOcrProcess == true)
                    {
                        var ocrMime = extension.TrimStart('.').ToLowerInvariant() switch
                        {
                            "jpg" or "jpeg" => "image/jpeg",
                            "png" => "image/png",
                            "gif" => "image/gif",
                            "bmp" => "image/bmp",
                            "tif" or "tiff" => "image/tiff",
                            _ => "image/jpeg"
                        };
                        var ocrText = await ocrService.ExtractTextAsync(fileBytes, ocrMime, ctx.RequestAborted);
                        if (!string.IsNullOrEmpty(ocrText))
                        {
                            resultDict["OcrText"] = ocrText;
                            if (string.IsNullOrEmpty(newDto.Description))
                                newDto.Description = ocrText.Length > 100 ? ocrText[..100] : ocrText;
                        }
                    }
                }
                else if (isDocument)
                {
                    newDto.FileContent = fileBytes;
                }
                else
                {
                    await File.WriteAllBytesAsync(originalPath, fileBytes);
                }

                // Strip absolute prefix to store relative paths in DB
                newDto.OriginalFilePath = StripPrefix(newDto.OriginalFilePath, imagePath);
                if (!string.IsNullOrEmpty(newDto.RegularImageFilepath))
                    newDto.RegularImageFilepath = StripPrefix(newDto.RegularImageFilepath, imagePath);
                if (!string.IsNullOrEmpty(newDto.ThumbnailFilePath))
                    newDto.ThumbnailFilePath = StripPrefix(newDto.ThumbnailFilePath, imagePath);

                var saveResult = AppFileBL.SaveOneAppFileEntityDto(newDto);
                resultDict[DocumentInfoDto.QryFileId] = saveResult.Object?.Id;
                return Results.Json(resultDict);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 500);
            }
        }).WithName("FileUpload").DisableAntiforgery();

        // ── Image serving ─────────────────────────────────────────────────────
        app.MapGet("/api/files/image/{id:int}", (HttpContext ctx, int id) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            var dto = AppFileBL.RetrieveOneOrgAppFileExDto(id);
            if (dto == null) return Results.NotFound();
            string path = AppCompanyBL.GetMyCompanyImagePath() + dto.OriginalFilePath;
            if (!File.Exists(path)) return Results.NotFound();
            return Results.File(File.ReadAllBytes(path), GetMimeType(dto.Extension ?? ""), Path.GetFileName(path));
        }).WithName("FileImage");

        app.MapGet("/api/files/thumbnail/{id:int}", (HttpContext ctx, int id) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            var dto = AppFileBL.RetrieveOneOrgAppFileExDto(id);
            if (dto == null) return Results.NotFound();
            string path = AppCompanyBL.GetMyCompanyImagePath()
                        + (dto.ThumbnailFilePath ?? dto.OriginalFilePath ?? "");
            if (!File.Exists(path)) return Results.NotFound();
            return Results.File(File.ReadAllBytes(path), GetMimeType(dto.Extension ?? ""), Path.GetFileName(path));
        }).WithName("FileThumbnail");

        // Regular (medium) size image; falls back to original when no regular render exists.
        app.MapGet("/api/files/regular/{id:int}", (HttpContext ctx, int id) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            var dto = AppFileBL.RetrieveOneOrgAppFileExDto(id);
            if (dto == null) return Results.NotFound();
            string path = AppCompanyBL.GetMyCompanyImagePath()
                        + (dto.RegularImageFilepath ?? dto.OriginalFilePath ?? dto.ThumbnailFilePath ?? "");
            if (!File.Exists(path)) return Results.NotFound();
            return Results.File(File.ReadAllBytes(path), GetMimeType(dto.Extension ?? ""), Path.GetFileName(path));
        }).WithName("FileRegular");

        app.MapGet("/api/files/latest/{id:int}", (HttpContext ctx, int id) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            var dto = AppFileBL.RetrieveOneLatestAppFileExDto(id);
            if (dto == null) return Results.NotFound();
            return ServeFileDto(dto);
        }).WithName("FileLatest");

        app.MapGet("/api/files/stream/{id:int}", (HttpContext ctx, int id) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            var dto = AppFileBL.RetrieveOneOrgAppFileExDto(id);
            if (dto == null) return Results.NotFound();
            return ServeFileDto(dto, forceDownload: true);
        }).WithName("FileStream");

        // ── Resource handler (replaces AppResourceHandler.ashx) ──────────────
        // Serves files from {AppBaseDir}/FileRepository/{*path} — used for logo/background images.
        app.MapGet("/api/resources/{*path}", (string path) =>
        {
            if (string.IsNullOrWhiteSpace(path)) return Results.BadRequest();
            string safePath = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "FileRepository", path));
            // Guard against path traversal
            string repoRoot = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileRepository"));
            if (!safePath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest();
            if (!File.Exists(safePath)) return Results.NotFound();
            string ext = Path.GetExtension(safePath);
            return Results.File(File.ReadAllBytes(safePath), GetMimeType(ext));
        }).WithName("ResourceHandler").AllowAnonymous();

        // ── Admin SQL (replaces AppScript.aspx — admin-only) ─────────────────
        app.MapPost("/api/admin/script", (HttpContext ctx, AdminScriptRequest body) =>
        {
            if (!ValidateSession(ctx)) return Results.Unauthorized();
            if (!AppSecurityUserBL.IsAdminUser())
                return Results.Forbid();
            if (string.IsNullOrWhiteSpace(body?.Sql))
                return Results.BadRequest("sql is required.");
            try
            {
                var db = new DBInteractionBase(ServerContext.Instance.CurrentUserDbConnectionString);
                db.ExecuteNonQuery(body.Sql);
                return Results.Ok(new { result = "OK" });
            }
            catch (Exception ex)
            {
                return Results.Ok(new { result = "error", message = ex.Message });
            }
        }).WithName("AdminScript").RequireAuthorization();

        // ── Audio UI (replaces AudioRecordAndPlay.aspx) ──────────────────────
        // Served as a static SPA page — redirect to React app route
        app.MapGet("/audio", () => Results.Redirect("/#/audio"))
           .WithName("AudioPage").AllowAnonymous();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // ── Image resize helpers (mirrors DataImage.ashx logic) ──────────────────

    private static byte[]? MakeThumbnail(byte[] imageBytes)
    {
        try
        {
            using var stream = ImageResizeHelper.ByteArrayToStream(imageBytes);
            stream.Position = 0;
            using var img = Image.FromStream(stream);
            return new ImageResizeHelper(img, new Size(70, 80)).GetNewResolution();
        }
        catch { return null; }
    }

    private static byte[]? MakeRegularSize(byte[] imageBytes)
    {
        try
        {
            using var img = StreamHelper.ByteArrayToImage(imageBytes);
            int w = img.Size.Width, h = img.Size.Height;
            if (w < 640 || h < 480) return imageBytes;
            var size = new Size((int)(w * 0.618), (int)(h * 0.618));
            return new ImageResizeHelper(img, size).GetNewResolution();
        }
        catch { return null; }
    }

    private static bool ValidateSession(HttpContext ctx)
    {
        var sessionId = ctx.Request.Headers[ServerContext.CurrentUserSessionIdToken].FirstOrDefault()
                     ?? ctx.Request.Cookies[ServerContext.CurrentUserSessionIdToken]
                     ?? ctx.Request.Query[ServerContext.CurrentUserSessionIdToken].FirstOrDefault()
                     ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        var anonymous = AppCacheManagerBL.GetAllCompnayAnoymouToken();
        if (anonymous.Contains(sessionId)) return false;
        AppSaasUserSessionMgtBL.ViladateSessionIdAndCompanyIdRegisterIdentity(sessionId);
        return true;
    }

    private static IResult ServeFileDto(AppFileExDto dto, bool forceDownload = false)
    {
        string mime = GetMimeType(dto.Extension ?? "");
        string fileName = dto.FileCode ?? "file";

        if (dto.FileContent is { Length: > 0 })
        {
            return forceDownload
                ? Results.File(dto.FileContent, mime, fileName)
                : Results.File(dto.FileContent, mime);
        }

        string diskPath = AppCompanyBL.GetMyCompanyImagePath() + (dto.OriginalFilePath ?? "");
        if (!File.Exists(diskPath)) return Results.NotFound();
        return forceDownload
            ? Results.File(File.ReadAllBytes(diskPath), mime, fileName)
            : Results.File(File.ReadAllBytes(diskPath), mime);
    }

    private static string StripPrefix(string path, string prefix)
        => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? path[prefix.Length..]
            : path;

    private static string GetMimeType(string extension) => extension.TrimStart('.').ToLowerInvariant() switch
    {
        "jpg" or "jpeg" => "image/jpeg",
        "png"           => "image/png",
        "gif"           => "image/gif",
        "bmp"           => "image/bmp",
        "tif" or "tiff" => "image/tiff",
        "svg"           => "image/svg+xml",
        "pdf"           => "application/pdf",
        "xlsx" or "xls" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "docx" or "doc" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "pptx" or "ppt" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "txt"           => "text/plain",
        "zip"           => "application/zip",
        "mp4"           => "video/mp4",
        _               => "application/octet-stream",
    };

    private sealed record AdminScriptRequest(string? Sql);
}

/// <summary>Mirrors the EmCallingFrom enum used in the old DataImage.aspx/ashx.</summary>
internal enum EmCallingFrom
{
    Image = 1,
    File = 2,
    ExcelUploadPorcess = 3,
    UserSync = 4,
    UploadWebSiteZipPackage = 5,
    ImportExcelToDatabase = 6,
    UploadFileToWebSiteFolder = 7,
    UploadBase64StringFile = 8,
    UploadCompanyBackgrundImage = 9,
    UploadCompanyLogoImage = 10,
    WorkflowDataModleSettingId = 11,
    APIOperationTesting = 12,
    UploadFileByTransactionCommand = 13,
}
