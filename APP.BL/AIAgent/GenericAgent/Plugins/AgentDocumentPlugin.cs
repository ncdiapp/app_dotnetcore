using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using APP.Framework.Plugin;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.Plugins
{
    /// <summary>
    /// BuiltIn document tools. The model sends a form tree. Writers apply DocumentStyles.
    /// Files land in the chat AgentOutput folder.
    /// </summary>
    public class AgentDocumentPlugin
    {
        public Task<string> ExportHtml(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".html", false);

        public Task<string> ExportCsv(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".csv", false);

        public Task<string> ExportExcel(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".xlsx", false);

        public Task<string> ExportDocx(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".docx", true);

        public Task<string> ExportPdf(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".pdf", true);

        public Task<string> ExportPptx(AgentToolContext context, CancellationToken ct, string path, string content = null, string sourcePath = null, string dataset_name = null, string layout = null, string records = null, bool preview = false, int cardsPerRow = 0, int cardRows = 0, int limit = 0)
            => Export(context, ct, path, content, sourcePath, dataset_name, layout, records, preview, cardsPerRow, cardRows, limit, ".pptx", true);

        public Task<string> ConvertDocxToPdf(AgentToolContext context, CancellationToken ct, string sourcePath, string path)
        {
            try
            {
                RequireSession(context);
                sourcePath = Normalize(sourcePath);
                if (!sourcePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("sourcePath must be a .docx file.");
                var docx = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, sourcePath, context.CompanyId, context.SkillKey);
                var pdf = LibreOfficeConvert.Convert(docx, ".docx", "pdf");
                path = WithExtension(string.IsNullOrWhiteSpace(path) ? Path.GetFileNameWithoutExtension(sourcePath) : path, ".pdf");
                return Task.FromResult(Saved(context, path, pdf, 0, false, null, "libreoffice", sourcePath));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

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
        {
            try
            {
                RequireSession(context);
                path = Normalize(path);
                var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                var html = System.Text.Encoding.UTF8.GetString(LibreOfficeConvert.Convert(bytes, ".docx", "html"));
                return Task.FromResult(PreviewJson(path, HtmlForm.Read(html)));
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        public Task<string> ReadPdf(AgentToolContext context, CancellationToken ct, string path)
        {
            try
            {
                RequireSession(context);
                path = Normalize(path);
                var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, path, context.CompanyId, context.SkillKey);
                var read = PdfText.Read(bytes);
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

        private const int PreviewInlineMax = 16000;

        private static async Task<string> Export(
            AgentToolContext context,
            CancellationToken ct,
            string path,
            string content,
            string sourcePath,
            string datasetName,
            string layout,
            string records,
            bool preview,
            int cardsPerRow,
            int cardRows,
            int rowLimit,
            string extension,
            bool paged)
        {
            try
            {
                RequireSession(context);
                var mode = string.IsNullOrWhiteSpace(layout) ? "form" : layout.Trim().ToLowerInvariant();
                if (mode != "form" && mode != "table" && mode != "cards")
                    throw new InvalidOperationException("layout must be form, table, or cards.");

                if (mode == "cards")
                {
                    var sheet = LoadCards(context, content, sourcePath, datasetName, records, cardsPerRow, paged ? cardRows : 0, rowLimit);
                    await CardImageLoader.AttachAsync(sheet, context.CompanyId, ct).ConfigureAwait(false);
                    var cardHtml = HtmlForm.WriteCards(sheet, preview || extension == ".pdf" || extension == ".docx" || extension == ".pptx");
                    if (preview)
                        return PreviewHtml(context, path, HtmlForm.ForPrint(cardHtml), sheet.CardCount, sheet.CacheLimitReached, sheet.MissingImages, "playwright");
                    path = WithExtension(path, extension);
                    var cardBytes = await WriteCardBytes(extension, sheet, cardHtml, ct).ConfigureAwait(false);
                    return Saved(context, path, cardBytes, sheet.CardCount, sheet.CacheLimitReached, sheet.MissingImages, EngineFor(extension));
                }

                var doc = LoadDocument(context, content, sourcePath, datasetName);
                var html = HtmlForm.Write(doc);
                if (preview)
                    return PreviewHtml(context, path, HtmlForm.ForPrint(html), doc.DataRowCount(), false, null, "playwright");
                path = WithExtension(path, extension);
                var bytes = await WriteFormBytes(extension, doc, html, context.CompanyId, ct).ConfigureAwait(false);
                return Saved(context, path, bytes, doc.DataRowCount(), false, null, EngineFor(extension));
            }
            catch (Exception ex)
            {
                return await Fail(Explain(ex)).ConfigureAwait(false);
            }
        }

        private static string Explain(Exception ex)
        {
            var message = ex?.Message ?? "";
            if (message.IndexOf("Free version limitation", StringComparison.OrdinalIgnoreCase) >= 0)
                return "PDF and Word are on the GemBox free key, which stops at 20 paragraphs. A card page is larger than that. Set GemBox:DocumentLicense, or the environment variable GemBox__DocumentLicense, to the paid key and restart the web app.";
            return message;
        }

        private static string EngineFor(string extension)
        {
            if (extension == ".pdf") return "playwright";
            if (extension == ".docx") return "libreoffice";
            if (extension == ".pptx") return "shapecrawler";
            if (extension == ".xlsx") return "openxml";
            if (extension == ".html") return "html";
            return "csv";
        }

        private static async Task<byte[]> WriteFormBytes(string extension, FormDocument doc, string html, int companyId, CancellationToken ct)
        {
            if (extension == ".html")
                return System.Text.Encoding.UTF8.GetBytes(html);
            if (extension == ".csv")
                return CsvForm.Write(doc);
            if (extension == ".xlsx")
            {
                await CardImageLoader.AttachAsync(doc, companyId, ct).ConfigureAwait(false);
                return ExcelForm.Write(doc);
            }
            if (extension == ".pdf")
                return await AppReportPdfService.RenderHtmlToPdfAsync(HtmlForm.ForPrint(html)).ConfigureAwait(false);
            if (extension == ".docx")
                return LibreOfficeConvert.Convert(System.Text.Encoding.UTF8.GetBytes(HtmlForm.ForPrint(html)), ".html", "docx");
            if (extension == ".pptx")
                return PptxForm.Write(doc, TemplatePaint.FromHtml(html));
            throw new InvalidOperationException("Unsupported export.");
        }

        private static async Task<byte[]> WriteCardBytes(string extension, CardSheet sheet, string html, CancellationToken ct)
        {
            if (extension == ".csv")
                return CsvForm.Write(sheet.AsFlatTable());
            if (extension == ".html")
                return System.Text.Encoding.UTF8.GetBytes(HtmlForm.WriteCards(sheet));
            if (extension == ".xlsx")
                return ExcelForm.WriteCards(sheet);
            if (extension == ".pdf")
                return await AppReportPdfService.RenderHtmlToPdfAsync(HtmlForm.ForPrint(html)).ConfigureAwait(false);
            if (extension == ".docx")
                return LibreOfficeConvert.Convert(System.Text.Encoding.UTF8.GetBytes(HtmlForm.ForPrint(html)), ".html", "docx");
            if (extension == ".pptx")
                return PptxForm.WriteCards(sheet, TemplatePaint.FromHtml(html));
            throw new InvalidOperationException("Unsupported export.");
        }

        private static string Saved(AgentToolContext context, string path, byte[] bytes, int rowCount, bool cacheLimitReached, List<string> missingImages, string engine, string sourcePath = null)
        {
            var relative = GenericAgentFileBL.WriteBytes(context.ChatSessionKey, path, bytes, context.CompanyId);
            return JsonConvert.SerializeObject(new
            {
                ok = true,
                relativePath = relative,
                rowCount,
                bytes = bytes.Length,
                cacheLimitReached,
                missingImages = missingImages ?? new List<string>(),
                engine,
                template = DocumentStyles.BuiltInTemplateName,
                sourcePath
            });
        }

        private static string PreviewHtml(AgentToolContext context, string path, string html, int rowCount, bool cacheLimitReached, List<string> missingImages, string engine)
        {
            if ((html ?? "").Length <= PreviewInlineMax)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    preview = true,
                    rowCount,
                    cacheLimitReached,
                    missingImages = missingImages ?? new List<string>(),
                    engine,
                    template = DocumentStyles.BuiltInTemplateName,
                    html
                });
            }
            var bytes = System.Text.Encoding.UTF8.GetBytes(html ?? "");
            var relative = GenericAgentFileBL.WriteBytes(context.ChatSessionKey, WithExtension(path, ".html"), bytes, context.CompanyId);
            return JsonConvert.SerializeObject(new
            {
                ok = true,
                preview = true,
                relativePath = relative,
                rowCount,
                bytes = bytes.Length,
                cacheLimitReached,
                missingImages = missingImages ?? new List<string>(),
                engine,
                template = DocumentStyles.BuiltInTemplateName
            });
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

        private static FormDocument LoadDocument(AgentToolContext context, string content, string sourcePath, string datasetName)
        {
            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                var rows = CacheRows(context, datasetName, out _);
                return TableFromRows(datasetName.Trim(), rows);
            }
            var loaded = Load(context, content, sourcePath);
            return ExpandFlatTable(context, loaded);
        }

        private static FormDocument ExpandFlatTable(AgentToolContext context, FormDocument doc)
        {
            if (doc == null || doc.Fields.Count > 0 || !string.IsNullOrEmpty(doc.Notes) || doc.Sections.Count != 1)
                return doc;
            var section = doc.Sections[0];
            if (section.Fields.Count > 0 || section.Sections.Count > 0 || section.Tables.Count != 1)
                return doc;
            var table = section.Tables[0];
            var inline = new JArray();
            foreach (var row in table.Rows)
            {
                var obj = new JObject();
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var cell = i < row.Count ? row[i] : null;
                    obj[table.Columns[i]] = cell?.Text ?? "";
                }
                inline.Add(obj);
            }
            var expanded = CacheRowExpand.Expand(context.ChatSessionKey, inline);
            if (expanded == null || expanded.Count == inline.Count)
                return doc;
            return TableFromRows(string.IsNullOrEmpty(doc.Title) ? section.Title : doc.Title, expanded);
        }

        private static CardSheet LoadCards(AgentToolContext context, string content, string sourcePath, string datasetName, string records, int cardsPerRow, int cardRows, int rowLimit)
        {
            JArray rows = null;
            var title = "Cards";
            var limit = false;
            var fromDataset = false;
            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                rows = CacheRows(context, datasetName, out limit);
                title = datasetName.Trim();
                fromDataset = true;
            }
            else if (!string.IsNullOrWhiteSpace(records))
                rows = CardSheet.ReadRecords(records);
            else if (!string.IsNullOrWhiteSpace(content))
                rows = CardSheet.ReadRecords(content);
            else if (AgentDataAnalyzeCacheBL.TryGetCardRender(context.ChatSessionKey, out var panel, out var panelTitle)
                && panel != null
                && panel.Count > 0)
            {
                rows = panel;
                if (!string.IsNullOrWhiteSpace(panelTitle))
                    title = panelTitle;
            }
            else if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                var bytes = GenericAgentFileBL.ReadBytes(context.ChatSessionKey, Normalize(sourcePath), context.CompanyId, context.SkillKey);
                rows = CardSheet.ReadRecords(System.Text.Encoding.UTF8.GetString(bytes));
                title = Path.GetFileNameWithoutExtension(sourcePath);
            }
            else
                throw new InvalidOperationException("Pass dataset_name, records, content, or sourcePath.");

            if (!fromDataset)
                rows = CacheRowExpand.Expand(context.ChatSessionKey, rows);
            if (rowLimit > 0)
                rows = AgentDataAnalyzeCacheBL.TakeFirst(rows, rowLimit);

            var sheet = CardSheet.FromRows(title, rows, cardsPerRow, cardRows);
            sheet.CacheLimitReached = limit;
            return sheet;
        }

        private static JArray RestoreCardRows(AgentToolContext context, JArray rows, ref string title)
        {
            if (context == null || string.IsNullOrWhiteSpace(context.ChatSessionKey) || rows == null)
                return rows;
            if (!AgentDataAnalyzeCacheBL.TryRestoreRows(context.ChatSessionKey, rows, true, out var restored, out var name))
                return rows;
            title = name;
            return restored;
        }

        private static JArray CacheRows(AgentToolContext context, string datasetName, out bool limitReached)
        {
            if (!AgentDataAnalyzeCacheBL.TryExportDataJson(context.ChatSessionKey, datasetName.Trim(), out var dataJson, out _, out var rowCount))
                throw new InvalidOperationException("dataset_name '" + datasetName + "' not found in this chat session.");
            limitReached = rowCount >= AgentDataAnalyzeCacheBL.MaxCachedRows;
            return JArray.Parse(dataJson);
        }

        private static FormDocument TableFromRows(string title, JArray rows)
        {
            var columns = new List<string>();
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in rows)
            {
                if (token is not JObject obj) continue;
                foreach (var prop in obj.Properties())
                {
                    if (seen.Add(prop.Name))
                        columns.Add(prop.Name);
                }
            }
            var data = new List<List<FormCell>>();
            foreach (var token in rows)
            {
                var obj = token as JObject;
                var line = new List<FormCell>();
                foreach (var name in columns)
                {
                    var cell = obj?[name];
                    var text = cell == null || cell.Type == JTokenType.Null ? "" : cell.ToString();
                    line.Add(new FormCell { Text = text, IsNumber = cell != null && (cell.Type == JTokenType.Integer || cell.Type == JTokenType.Float) });
                }
                data.Add(line);
            }
            return FormDocument.FromTable(title, columns, data);
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
