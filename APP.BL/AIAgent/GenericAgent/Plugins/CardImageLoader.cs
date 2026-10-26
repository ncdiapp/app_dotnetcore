using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.TenantBusiness;
using APP.LBL.EntityClasses;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// Turns a card/grid image field into bytes. App file ids are read from FileRepository.
    /// An http(s) image URL is downloaded. A missing picture is recorded and does not fail the file.
    /// Batches FileRepository entity lookups and loads images with bounded concurrency
    /// (500 serial DB/disk hits were the main Print/PDF delay — not a re-query of Result rows).
    /// </summary>
    internal static class CardImageLoader
    {
        private const int MaxBytes = 2_000_000;
        private const int MaxConcurrency = 8;
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        private sealed class WorkItem
        {
            public FormCell Cell;
            public CardField Field;
            public string Text;
            public string ColName;
            public bool FlaggedIsImage;
            public int? FileId;
            public string HttpUrl;
        }

        public static async Task AttachAsync(FormDocument doc, int companyId, CancellationToken ct)
        {
            if (doc == null) return;
            var work = new List<WorkItem>();
            CollectDocWork(doc, work);
            await AttachWorkAsync(work, companyId, missing: null, ct).ConfigureAwait(false);
        }

        public static async Task AttachAsync(CardSheet sheet, int companyId, CancellationToken ct)
        {
            if (sheet == null) return;
            var work = new List<WorkItem>();
            foreach (var card in sheet.Cards)
            {
                foreach (var field in card)
                {
                    if (field == null || field.ImageBytes != null) continue;
                    if (!field.IsImage && !CardSheet.IsRenderableImage(field.Value, field.Label, field.IsImage))
                        continue;
                    work.Add(new WorkItem
                    {
                        Field = field,
                        Text = field.Value,
                        ColName = field.Label,
                        FlaggedIsImage = field.IsImage
                    });
                }
            }
            await AttachWorkAsync(work, companyId, sheet.MissingImages, ct).ConfigureAwait(false);
        }

        private static void CollectDocWork(FormDocument doc, List<WorkItem> work)
        {
            if (doc?.Sections == null) return;
            foreach (var section in doc.Sections)
                CollectSectionWork(section, work);
        }

        private static void CollectSectionWork(FormSection section, List<WorkItem> work)
        {
            if (section == null) return;
            foreach (var table in section.Tables)
            {
                var cols = table?.Columns;
                if (table?.Rows == null) continue;
                foreach (var row in table.Rows)
                {
                    for (var i = 0; i < row.Count; i++)
                    {
                        var cell = row[i];
                        var colName = cols != null && i < cols.Count ? cols[i] : null;
                        if (cell == null || cell.ImageBytes != null) continue;
                        if (!cell.IsImage && !CardSheet.IsRenderableImage(cell.Text, colName, cell.IsImage))
                            continue;
                        work.Add(new WorkItem
                        {
                            Cell = cell,
                            Text = cell.Text,
                            ColName = colName,
                            FlaggedIsImage = cell.IsImage
                        });
                    }
                }
            }
            foreach (var child in section.Sections)
                CollectSectionWork(child, work);
        }

        private static async Task AttachWorkAsync(
            List<WorkItem> work, int companyId, List<string> missing, CancellationToken ct)
        {
            if (work == null || work.Count == 0) return;

            foreach (var item in work)
                Classify(item);

            // One DB round-trip for all app file ids (was N× RetrieveMultiple per image).
            var idSet = new HashSet<int>();
            foreach (var item in work)
            {
                if (item.FileId is int id && id > 0)
                    idSet.Add(id);
            }
            Dictionary<int, AppFileEntity> entities = null;
            if (idSet.Count > 0 && companyId > 0)
            {
                try
                {
                    var list = AppFileBL.RetrieveMultipleFileEntityByIds(idSet.ToList(), true);
                    entities = new Dictionary<int, AppFileEntity>();
                    if (list != null)
                    {
                        foreach (AppFileEntity e in list)
                        {
                            if (e != null && !entities.ContainsKey(e.FileId))
                                entities[e.FileId] = e;
                        }
                    }
                }
                catch
                {
                    entities = null;
                }
            }

            var company = companyId > 0 ? "Company_" + companyId.ToString() : null;
            await RunBoundedAsync(work, MaxConcurrency, async item =>
            {
                byte[] bytes = null;
                try
                {
                    if (item.FileId is int fid && fid > 0 && entities != null
                        && entities.TryGetValue(fid, out var ent) && company != null)
                        bytes = ReadEntityBytes(ent, company);
                    if (bytes == null && !string.IsNullOrEmpty(item.HttpUrl))
                        bytes = await DownloadAsync(item.HttpUrl, ct).ConfigureAwait(false);
                    if (bytes == null)
                        bytes = await LoadAsync(item.Text, companyId, ct, item.ColName, item.FlaggedIsImage)
                            .ConfigureAwait(false);
                }
                catch
                {
                    bytes = null;
                }

                if (bytes != null && IsRaster(bytes))
                {
                    if (item.Cell != null)
                    {
                        item.Cell.ImageBytes = bytes;
                        item.Cell.IsImage = true;
                    }
                    if (item.Field != null)
                    {
                        item.Field.ImageBytes = bytes;
                        item.Field.IsImage = true;
                    }
                    return;
                }

                if (missing != null)
                {
                    var label = item.ColName;
                    if (string.IsNullOrEmpty(label)) label = item.Text;
                    lock (missing) missing.Add(label);
                }
            }, ct).ConfigureAwait(false);
        }

        private static void Classify(WorkItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Text)) return;
            var text = item.Text.Trim().Trim('"', '\'');
            item.Text = text;
            if (!CardSheet.IsRenderableImage(text, item.ColName, item.FlaggedIsImage)
                && !LooksLikeAppImageUrl(text))
                return;

            if (LooksLikeAppImageUrl(text))
            {
                try
                {
                    if (AppFileBL.TryResolveImageFileIdForSave(text, out var resolved)
                        && resolved is int parsed && parsed > 0)
                    {
                        item.FileId = parsed;
                        return;
                    }
                }
                catch { /* fall through */ }
            }

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                item.HttpUrl = text;
                return;
            }

            if (int.TryParse(text, out var fileId) && fileId > 0
                && (item.FlaggedIsImage || CardSheet.IsImageFieldName(item.ColName)))
            {
                item.FileId = fileId;
                return;
            }

            try
            {
                if (AppFileBL.TryResolveImageFileIdForSave(text, out var resolved)
                    && resolved is int parsed && parsed > 0)
                    item.FileId = parsed;
            }
            catch { /* ignore */ }
        }

        private static byte[] ReadEntityBytes(AppFileEntity entity, string company)
        {
            if (entity == null || string.IsNullOrEmpty(company)) return null;
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
                return ReadRepository(direct);
            }
            return null;
        }

        private static async Task RunBoundedAsync<T>(
            IReadOnlyList<T> items, int maxConcurrency, Func<T, Task> body, CancellationToken ct)
        {
            if (items == null || items.Count == 0) return;
            using var gate = new SemaphoreSlim(Math.Max(1, maxConcurrency));
            var tasks = new Task[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                tasks[i] = Task.Run(async () =>
                {
                    await gate.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        await body(item).ConfigureAwait(false);
                    }
                    finally
                    {
                        gate.Release();
                    }
                }, ct);
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
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
                catch { /* fall through */ }
            }

            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return await DownloadAsync(text, ct).ConfigureAwait(false);

            int fileId;
            if (int.TryParse(text, out fileId) && fileId > 0
                && (flaggedIsImage || CardSheet.IsImageFieldName(fieldName)))
            {
                if (companyId <= 0) return null;
                return ReadAppFile(companyId, fileId);
            }

            if (!int.TryParse(text, out fileId))
            {
                try
                {
                    if (!AppFileBL.TryResolveImageFileIdForSave(text, out var resolved)
                        || !(resolved is int parsed) || parsed <= 0)
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
            var list = AppFileBL.RetrieveMultipleFileEntityByIds(new List<int> { fileId }, true);
            if (list == null || list.Count == 0) return null;
            return ReadEntityBytes(list[0], "Company_" + companyId.ToString());
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
