using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn document tools. The model sends a form tree. Writers apply DocumentStyles.
    /// Files land in the chat AgentOutput folder.
    /// </summary>
    public class AgentDocumentPlugin
    {
        public Task<string> ExportHtml(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".html", doc => System.Text.Encoding.UTF8.GetBytes(Documents.HtmlForm.Write(doc)));

        public Task<string> ExportCsv(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".csv", Documents.CsvForm.Write);

        public Task<string> ExportExcel(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".xlsx", Documents.ExcelForm.Write);

        public Task<string> ExportDocx(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".docx", Documents.WordForm.WriteDocx);

        public Task<string> ExportPdf(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".pdf", Documents.WordForm.WritePdf);

        public Task<string> ExportPptx(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null)
            => Export(context, path, content, sourcePath, ".pptx", Documents.PptxForm.Write);

        public Task<string> ReadHtml(AgentToolContext context, CancellationToken ct, string path)
            => Read(context, path, bytes => Documents.HtmlForm.Read(System.Text.Encoding.UTF8.GetString(bytes)));

        public Task<string> ReadCsv(AgentToolContext context, CancellationToken ct, string path)
            => Read(context, path, Documents.CsvForm.Read);

        public Task<string> ReadExcel(AgentToolContext context, CancellationToken ct, string path)
        {
            try
            {
                RequireSession(context);
                path = Normalize(path);
                var full = GenericAgentFileBL.Resolve(context.ChatSessionKey, path, context.CompanyId);
                if (!File.Exists(full))
                    return Fail("FILE_NOT_FOUND");
                var bytes = File.ReadAllBytes(full);
                FormDocumentLike(path, bytes, out var doc);
                return Task.FromResult(PreviewJson(path, doc));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        public Task<string> ReadDocx(AgentToolContext context, CancellationToken ct, string path)
            => Read(context, path, Documents.WordForm.ReadDocx);

        public Task<string> ReadPdf(AgentToolContext context, CancellationToken ct, string path)
        {
            try
            {
                RequireSession(context);
                path = Normalize(path);
                var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                var read = Documents.WordForm.ReadPdf(bytes);
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    relativePath = path,
                    pageCount = read.PageCount,
                    text = read.Text
                }));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        public Task<string> ReadPptx(AgentToolContext context, CancellationToken ct, string path)
            => Read(context, path, Documents.PptxForm.Read);

        private static void FormDocumentLike(string path, byte[] bytes, out Documents.FormDocument doc)
        {
            if (path.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
                && !path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                doc = Documents.ExcelForm.ReadXls(bytes);
                return;
            }
            doc = Documents.ExcelForm.Read(bytes);
        }

        private static Task<string> Export(
            AgentToolContext context,
            string path,
            string content,
            string sourcePath,
            string extension,
            Func<Documents.FormDocument, byte[]> write)
        {
            try
            {
                RequireSession(context);
                var doc = Load(context, content, sourcePath);
                path = WithExtension(path, extension);
                var bytes = write(doc);
                var relative = GenericAgentFileBL.WriteBytes(context.ChatSessionKey, path, bytes, context.CompanyId);
                return Task.FromResult(JsonConvert.SerializeObject(new
                {
                    ok = true,
                    relativePath = relative,
                    rowCount = doc.DataRowCount(),
                    bytes = bytes.Length
                }));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        private static Task<string> Read(AgentToolContext context, string path, Func<byte[], Documents.FormDocument> read)
        {
            try
            {
                RequireSession(context);
                path = Normalize(path);
                var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                return Task.FromResult(PreviewJson(path, read(bytes)));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        private static string PreviewJson(string path, Documents.FormDocument doc)
        {
            var total = doc.DataRowCount();
            var preview = doc.Preview(Documents.DocumentStyles.MaxPreviewRows, out var shown, out var truncated);
            return JsonConvert.SerializeObject(new
            {
                ok = true,
                relativePath = path,
                rowCount = total,
                previewRowCount = shown,
                truncated,
                document = preview
            });
        }

        internal static Documents.FormDocument Load(AgentToolContext context, string content, string sourcePath)
        {
            var hasContent = !string.IsNullOrWhiteSpace(content);
            var hasSource = !string.IsNullOrWhiteSpace(sourcePath);
            if (hasContent == hasSource)
                throw new InvalidOperationException("Pass content or sourcePath, not both and not neither.");
            if (hasContent)
                return Documents.FormDocument.Parse(content);
            sourcePath = Normalize(sourcePath);
            var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, sourcePath, context.CompanyId, context.SkillKey);
            var ext = Path.GetExtension(sourcePath);
            if (string.Equals(ext, ".json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase))
                return Documents.FormDocument.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            if (string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
                return Documents.CsvForm.Read(bytes);
            if (string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return Documents.ExcelForm.Read(bytes);
            if (string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase))
                return Documents.HtmlForm.Read(System.Text.Encoding.UTF8.GetString(bytes));
            throw new InvalidOperationException("sourcePath must be .json, .csv, .xlsx, or .html.");
        }

        private static string WithExtension(string path, string extension)
        {
            path = Normalize(path);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("path is required.");
            var current = Path.GetExtension(path);
            if (string.IsNullOrEmpty(current))
                path += extension;
            else if (!string.Equals(current, extension, StringComparison.OrdinalIgnoreCase))
                path = path.Substring(0, path.Length - current.Length) + extension;
            return StampExportPath(PlaceInOutputFolder(path), DateTime.Now);
        }

        /// <summary>A bare file name is saved under output/. A path that already names a folder is left as given.</summary>
        public static string PlaceInOutputFolder(string path)
        {
            var norm = (path ?? "").Replace('\\', '/').TrimStart('/');
            if (norm.StartsWith("output/", StringComparison.OrdinalIgnoreCase))
                return norm;
            if (norm.IndexOf('/') >= 0)
                return norm;
            return "output/" + norm;
        }

        /// <summary>Inserts _yyyyMMdd_HHmmss before the extension so each export is a new file.</summary>
        public static string StampExportPath(string path, DateTime when)
        {
            var ext = Path.GetExtension(path ?? "");
            var stem = string.IsNullOrEmpty(ext) ? (path ?? "") : path.Substring(0, path.Length - ext.Length);
            var stamp = when.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            return stem + "_" + stamp + ext;
        }

        private static string Normalize(string path)
        {
            return GenericAgentExcelFileHelper.NormalizeRelativePath(path);
        }

        private static void RequireSession(AgentToolContext context)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey))
                throw new InvalidOperationException("ChatSessionKey is required for agent file tools.");
            if (context.CompanyId <= 0)
                throw new InvalidOperationException("CompanyId is required for agent file tools.");
        }

        private static Task<string> Fail(string error)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new { ok = false, error }));
        }
    }
}
