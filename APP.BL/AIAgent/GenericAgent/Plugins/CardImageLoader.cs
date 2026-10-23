using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.TenantBusiness;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Turns a card/grid image field into bytes. App file ids are read from FileRepository.
    /// An http(s) image URL is downloaded. A missing picture is recorded and does not fail the file.
    /// </summary>
    internal static class CardImageLoader
    {
        private const int MaxBytes = 2_000_000;
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public static async Task AttachAsync(FormDocument doc, int companyId, CancellationToken ct)
        {
            if (doc == null) return;
            foreach (var section in doc.Sections)
                await AttachSectionAsync(section, companyId, ct).ConfigureAwait(false);
        }

        private static async Task AttachSectionAsync(FormSection section, int companyId, CancellationToken ct)
        {
            if (section == null) return;
            foreach (var table in section.Tables)
            {
                var cols = table?.Columns;
                foreach (var row in table.Rows)
                {
                    for (var i = 0; i < row.Count; i++)
                    {
                        var cell = row[i];
                        var colName = cols != null && i < cols.Count ? cols[i] : null;
                        if (cell == null || cell.ImageBytes != null) continue;
                        if (!cell.IsImage && !CardSheet.IsRenderableImage(cell.Text, colName, cell.IsImage))
                            continue;
                        byte[] bytes = null;
                        try
                        {
                            bytes = await LoadAsync(cell.Text, companyId, ct, colName, cell.IsImage).ConfigureAwait(false);
                        }
                        catch
                        {
                            bytes = null;
                        }
                        if (bytes != null && IsRaster(bytes))
                        {
                            cell.ImageBytes = bytes;
                            cell.IsImage = true;
                        }
                    }
                }
            }
            foreach (var child in section.Sections)
                await AttachSectionAsync(child, companyId, ct).ConfigureAwait(false);
        }

        public static async Task AttachAsync(CardSheet sheet, int companyId, CancellationToken ct)
        {
            if (sheet == null) return;
            foreach (var card in sheet.Cards)
            {
                foreach (var field in card)
                {
                    if (field == null || field.ImageBytes != null) continue;
                    if (!field.IsImage && !CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage))
                        continue;
                    byte[] bytes = null;
                    try
                    {
                        bytes = await LoadAsync(field.Value, companyId, ct, field.Label, field.IsImage).ConfigureAwait(false);
                    }
                    catch
                    {
                        bytes = null;
                    }
                    if (bytes == null || !IsRaster(bytes))
                    {
                        sheet.MissingImages.Add(string.IsNullOrEmpty(field.Label) ? field.Value : field.Label);
                        continue;
                    }
                    field.ImageBytes = bytes;
                    field.IsImage = true;
                }
            }
        }

        public static string Mime(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3) return "application/octet-stream";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
            if (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F') return "image/gif";
            if (bytes[0] == (byte)'B' && bytes[1] == (byte)'M') return "image/bmp";
            return "application/octet-stream";
        }

        private static async Task<byte[]> LoadAsync(
            string value, int companyId, CancellationToken ct, string fieldName = null, bool flaggedIsImage = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var text = value.Trim().Trim('"', '\'');
            if (!CardSheet.IsRenderableImage(text, fieldName, flaggedIsImage)
                && !LooksLikeAppImageUrl(text))
                return null;

            // Prefer FileRepository for app /api/files|resources URLs (absolute or relative).
            if (LooksLikeAppImageUrl(text))
            {
                try
                {
                    if (AppFileBL.TryResolveImageFileIdForSave(text, out var resolved)
                        && resolved is int parsed && parsed > 0 && companyId > 0)
                    {
                        var fromRepo = ReadAppFile(companyId, parsed);
                        if (fromRepo != null) return fromRepo;
                    }
                }
                catch
                {
                    // Fall through to HTTP download for absolute URLs.
                }
            }

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return await DownloadAsync(text, ct).ConfigureAwait(false);
            }

            int fileId;
            if (int.TryParse(text, out fileId) && fileId > 0
                && (flaggedIsImage || CardSheet.IsImageFieldName(fieldName)))
            {
                if (companyId <= 0) return null;
                return ReadAppFile(companyId, fileId);
            }

            if (!int.TryParse(text, out fileId))
            {
                object resolved;
                try
                {
                    if (!AppFileBL.TryResolveImageFileIdForSave(text, out resolved) || !(resolved is int parsed) || parsed <= 0)
                        return null;
                    fileId = parsed;
                }
                catch
                {
                    return null;
                }
            }
            if (fileId <= 0 || companyId <= 0) return null;
            return ReadAppFile(companyId, fileId);
        }

        private static bool LooksLikeAppImageUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var path = text.Trim();
            var scheme = path.IndexOf("://", StringComparison.Ordinal);
            if (scheme >= 0)
            {
                var hostSlash = path.IndexOf('/', scheme + 3);
                path = hostSlash >= 0 ? path.Substring(hostSlash) : path;
            }
            return path.IndexOf("/api/files/", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("/api/resources/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static byte[] ReadAppFile(int companyId, int fileId)
        {
            var list = AppFileBL.RetrieveMultipleFileEntityByIds(new System.Collections.Generic.List<int> { fileId }, true);
            if (list == null || list.Count == 0) return null;
            var entity = list[0];
            var company = "Company_" + companyId.ToString();
            var relative = AppFileBL.BuildCompanyImageResourcePath(company, entity.RegularImageFilepath)
                ?? AppFileBL.BuildCompanyImageResourcePath(company, entity.OriginalFilePath)
                ?? AppFileBL.BuildCompanyImageResourcePath(company, entity.ThumbnailFilePath);
            if (!string.IsNullOrEmpty(relative))
            {
                var fromRepo = ReadRepository(relative);
                if (fromRepo != null) return fromRepo;
            }
            if (!string.IsNullOrEmpty(entity.OriginalFilePath))
            {
                var direct = entity.OriginalFilePath.TrimStart('/', '\\').Replace('\\', '/');
                var fromOriginal = ReadRepository(direct);
                if (fromOriginal != null) return fromOriginal;
            }
            return null;
        }

        private static byte[] ReadRepository(string relative)
        {
            var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileRepository"));
            var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
            if (!File.Exists(full)) return null;
            var info = new FileInfo(full);
            if (info.Length <= 0 || info.Length > MaxBytes) return null;
            return File.ReadAllBytes(full);
        }

        private static async Task<byte[]> DownloadAsync(string url, CancellationToken ct)
        {
            using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            var length = response.Content.Headers.ContentLength;
            if (length.HasValue && length.Value > MaxBytes) return null;
            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            if (bytes.Length == 0 || bytes.Length > MaxBytes) return null;
            return bytes;
        }

        private static bool IsRaster(byte[] bytes)
        {
            var mime = Mime(bytes);
            return mime == "image/jpeg" || mime == "image/png" || mime == "image/gif" || mime == "image/bmp";
        }
    }
}
