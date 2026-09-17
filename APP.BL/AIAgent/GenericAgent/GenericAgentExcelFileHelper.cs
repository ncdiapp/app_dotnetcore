using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GemBox.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenXmlCell = DocumentFormat.OpenXml.Spreadsheet.Cell;
using OpenXmlRow = DocumentFormat.OpenXml.Spreadsheet.Row;
using OpenXmlSheet = DocumentFormat.OpenXml.Spreadsheet.Sheet;
using OpenXmlSheets = DocumentFormat.OpenXml.Spreadsheet.Sheets;
using OpenXmlWorksheet = DocumentFormat.OpenXml.Spreadsheet.Worksheet;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Excel helpers for agent file tools.
    /// Shared LLM parsing + in-memory sheet model; I/O backend is switched by <see cref="UseOpenXml"/>.
    /// </summary>
    public static class GenericAgentExcelFileHelper
    {
        public const int MaxReadRows = 500;
        public const int MaxWriteRows = 5000;

        /// <summary>
        /// One-line backend switch for agent Excel I/O:
        /// false = GemBox.Spreadsheet (default);
        /// true  = DocumentFormat.OpenXml (no free 5-sheet / 150-row cap).
        /// </summary>
        public const bool UseOpenXml = false;

        private const string XlsOpenXmlMessage =
            "OpenXml backend requires .xlsx. Set UseOpenXml=false for .xls, or use a .xlsx path.";

        public static bool IsExcelPath(string relativePath)
        {
            var path = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(path)) return false;
            var ext = Path.GetExtension(path);
            return string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Strip quotes/backticks the LLM often wraps around paths; normalize slashes.
        /// </summary>
        public static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return relativePath;
            var s = relativePath.Trim().Trim('"', '\'', '`');
            s = s.Replace('\\', '/').Trim();
            while (s.StartsWith("./", StringComparison.Ordinal)) s = s.Substring(2);
            return s;
        }

        /// <summary>
        /// If the agent asked for Excel (path or structured content) but used .txt/.csv / no extension,
        /// coerce to .xlsx so we never silently WriteText a "log.xlsx" request as UTF-8 text.
        /// </summary>
        public static string CoerceExcelWritePath(string relativePath, string content)
        {
            var path = NormalizeRelativePath(relativePath) ?? "";
            var wantsExcel = IsExcelPath(path) || LooksLikeExcelWriteContent(content);
            if (!wantsExcel) return path;

            if (IsExcelPath(path)) return path;

            var ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext)
                || string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".log", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(ext))
                    return path.TrimEnd('/') + ".xlsx";
                return Path.ChangeExtension(path, ".xlsx")?.Replace('\\', '/') ?? (path + ".xlsx");
            }

            return path.TrimEnd('/') + ".xlsx";
        }

        /// <summary>
        /// True when content is JSON excel write spec (mode/sheet/headers/rows) — not plain prose.
        /// </summary>
        public static bool LooksLikeExcelWriteContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            var t = content.Trim();
            if (!t.StartsWith("{")) return false;
            try
            {
                var obj = JObject.Parse(t);
                return obj["rows"] != null
                    || obj["row"] != null
                    || obj["headers"] != null
                    || obj["sheet"] != null
                    || obj["sheetName"] != null
                    || (obj["mode"] != null && (
                        string.Equals(obj.Value<string>("mode"), "append", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(obj.Value<string>("mode"), "overwrite", StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                return false;
            }
        }

        public static void AssertLooksLikeExcelFile(string fullPath)
        {
            if (!File.Exists(fullPath))
                throw new InvalidOperationException("Excel save did not create a file: " + fullPath);
            var ext = Path.GetExtension(fullPath);
            if (string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                // XLSX is a ZIP package — must start with PK
                using var fs = File.OpenRead(fullPath);
                if (fs.Length < 4)
                    throw new InvalidOperationException("Excel file is empty after save.");
                int b0 = fs.ReadByte(), b1 = fs.ReadByte();
                if (b0 != 'P' || b1 != 'K')
                    throw new InvalidOperationException(
                        "File was not saved as real XLSX (missing ZIP signature). Path may have been written as text earlier; delete it and retry.");
            }
        }

        public static object ReadForAgent(string fullPath, int maxRows = MaxReadRows)
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Excel file not found.", fullPath);

            AssertSupportedExtension(fullPath);

            var workbook = LoadWorkbook(fullPath);
            var sheetNames = workbook.Sheets.Select(s => s.Name).ToList();
            var sheets = workbook.Sheets.Select(s => SheetToDto(s, maxRows)).ToList();
            var first = sheets.Count > 0 ? sheets[0] : null;

            return new
            {
                Format = "excel",
                Engine = UseOpenXml ? "OpenXml" : "GemBox",
                SheetNames = sheetNames,
                SheetCount = sheetNames.Count,
                Sheets = sheets,
                // Backward-compatible: first sheet flattened at top level
                Sheet = sheetNames.Count > 0 ? sheetNames[0] : null,
                FirstSheet = first
            };
        }

        private static object SheetToDto(SheetModel sheet, int maxRows)
        {
            var headers = sheet.Headers.ToArray();
            var take = Math.Min(Math.Max(0, maxRows), sheet.Rows.Count);
            var rows = new List<object[]>(take);
            for (int i = 0; i < take; i++)
            {
                var src = sheet.Rows[i];
                var cells = new object[headers.Length];
                for (int c = 0; c < headers.Length; c++)
                    cells[c] = c < src.Count ? (src[c] ?? "") : "";
                rows.Add(cells);
            }

            return new
            {
                Name = sheet.Name,
                Headers = headers,
                Rows = rows,
                RowCount = sheet.Rows.Count,
                Truncated = sheet.Rows.Count > take
            };
        }

        /// <summary>
        /// Writes Excel from agent content without destroying unrelated sheets.
        /// Modes:
        ///   append          — append rows to sheet(s); create sheet if missing; keep other sheets
        ///   overwrite       — same as overwrite_sheet (legacy name)
        ///   overwrite_sheet — replace only the named sheet(s); keep other sheets
        ///   replace_file    — ONLY mode that creates a brand-new workbook (explicit wipe)
        /// Preferred JSON (single sheet):
        ///   {"mode":"overwrite_sheet","sheet":"SheetA","headers":["A","B"],"rows":[["v1","v2"]]}
        /// Multi-sheet:
        ///   {"mode":"overwrite_sheet","sheets":[{"name":"SheetA","headers":[...],"rows":[...]},{"name":"SheetB",...}]}
        /// </summary>
        public static object WriteFromAgentContent(string fullPath, string content)
        {
            AssertSupportedExtension(fullPath);

            var spec = ParseWriteSpec(content);
            var mode = NormalizeMode(spec.Mode);
            var sheetWrites = ExpandSheetWrites(spec);

            // LLM may dump "Sheet: Name" + markdown "| ... |" as rows on ONE worksheet.
            // Rescue into real multi-sheet writes when possible; otherwise reject.
            sheetWrites = RescueMarkdownDumpToSheets(sheetWrites, content);
            if (sheetWrites.Count > 1 && mode == "append")
                mode = "overwrite_sheet"; // creating/updating several sheets must not append onto one sheet

            if (sheetWrites.Count == 0)
                throw new ArgumentException(
                    "Excel write produced no sheets/rows. Use JSON {mode,sheets:[{name,headers,rows},...]} — one worksheet object per sheet name.");

            foreach (var sw in sheetWrites)
            {
                if (sw.Rows.Count > MaxWriteRows)
                    throw new InvalidOperationException($"Excel write exceeds row limit ({MaxWriteRows}) on sheet '{sw.Name}'.");
            }

            RejectMarkdownDumpedAsCells(sheetWrites);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

            WorkbookModel workbook;
            bool createdNew = false;

            if (File.Exists(fullPath) && mode != "replace_file")
            {
                workbook = LoadWorkbook(fullPath);
            }
            else
            {
                createdNew = true;
                workbook = new WorkbookModel();
            }

            var written = new List<object>();
            var writtenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sw in sheetWrites)
            {
                var name = SanitizeSheetName(sw.Name);
                if (mode == "append")
                {
                    var sheet = FindOrAddSheet(workbook, name);
                    var headers = ResolveHeaders(sw.Headers, sw.Rows, sheet, replace: false);
                    AppendRows(sheet, headers, sw.Rows);
                    written.Add(new { Sheet = sheet.Name, Mode = "append", RowsWritten = sw.Rows.Count, Headers = headers });
                    writtenNames.Add(sheet.Name);
                }
                else
                {
                    // overwrite_sheet / replace_file (per sheet)
                    var sheet = PrepareSheetForOverwrite(workbook, name);
                    var headers = ResolveHeaders(sw.Headers, sw.Rows, null, replace: true);
                    sheet.Headers = headers;
                    sheet.Rows = NormalizeRows(headers.Count, sw.Rows);
                    written.Add(new { Sheet = sheet.Name, Mode = mode, RowsWritten = sw.Rows.Count, Headers = headers });
                    writtenNames.Add(sheet.Name);
                }
            }

            // Drop leftover default empty "Sheet1" when we created a new file and wrote other names
            if (createdNew)
                TryRemoveUnusedDefaultSheet(workbook, writtenNames);

            if (workbook.Sheets.Count == 0)
                workbook.Sheets.Add(new SheetModel { Name = "Sheet1" });

            SaveWorkbook(fullPath, workbook);
            AssertLooksLikeExcelFile(fullPath);

            var allNames = workbook.Sheets.Select(s => s.Name).ToList();
            if (sheetWrites.Count > 1 && allNames.Count < sheetWrites.Count)
                throw new InvalidOperationException(
                    $"Expected {sheetWrites.Count} sheets but workbook has {allNames.Count}: {string.Join(", ", allNames)}");

            return new
            {
                Ok = true,
                Format = Path.GetExtension(fullPath)?.TrimStart('.').ToLowerInvariant() == "xls" ? "xls" : "xlsx",
                Engine = UseOpenXml ? "OpenXml" : "GemBox",
                RelativePathHint = Path.GetFileName(fullPath),
                Mode = createdNew ? "create" : mode,
                SheetCount = allNames.Count,
                SheetNames = allNames,
                RequestedSheetCount = sheetWrites.Count,
                Written = written,
                SheetsCreatedOrUpdated = writtenNames.OrderBy(n => n).ToList()
            };
        }

        private static void AssertSupportedExtension(string fullPath)
        {
            var ext = Path.GetExtension(fullPath);
            if (string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
                return;
            if (string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                if (UseOpenXml)
                    throw new ArgumentException(XlsOpenXmlMessage);
                return;
            }
            throw new ArgumentException(
                UseOpenXml
                    ? "OpenXml backend requires a .xlsx path."
                    : "Agent Excel tools require a .xlsx or .xls path.");
        }

        /// <summary>
        /// Detect rows like "Sheet: Name" / markdown "| col | ..." dumped into one sheet and split into real SheetWrites.
        /// </summary>
        private static List<SheetWrite> RescueMarkdownDumpToSheets(List<SheetWrite> sheetWrites, string rawContent)
        {
            if (sheetWrites == null || sheetWrites.Count == 0)
                return sheetWrites ?? new List<SheetWrite>();

            // Already proper multi-sheet with real cell grids (no pipe-markdown rows)
            if (sheetWrites.Count > 1 && !sheetWrites.Any(LooksLikeMarkdownDumpSheet))
                return sheetWrites;

            if (sheetWrites.Count == 1 && !LooksLikeMarkdownDumpSheet(sheetWrites[0]))
            {
                // Try raw content if it contains Sheet: sections
                var fromRaw = ParseSheetSectionsFromText(rawContent);
                if (fromRaw.Count > 1)
                    return fromRaw;
                return sheetWrites;
            }

            // Flatten dumped rows/cells into text lines
            var lines = new List<string>();
            foreach (var sw in sheetWrites)
            {
                foreach (var row in sw.Rows)
                {
                    if (row == null || row.Count == 0) continue;
                    if (row.Count == 1)
                        lines.Add(row[0] ?? "");
                    else if (row.All(c => (c ?? "").Contains('|')))
                        lines.Add(string.Join(" ", row));
                    else
                        lines.Add("| " + string.Join(" | ", row) + " |");
                }
            }

            var rescued = ParseSheetSectionsFromLines(lines);
            if (rescued.Count > 0)
                return rescued;

            // Single markdown table without Sheet: labels → keep as one structured sheet
            var oneTable = ParseMarkdownTable(lines);
            if (oneTable.HasValue && oneTable.Value.Rows.Count > 0)
            {
                return new List<SheetWrite>
                {
                    new SheetWrite
                    {
                        Name = string.IsNullOrWhiteSpace(sheetWrites[0].Name) ? "Sheet1" : sheetWrites[0].Name,
                        Headers = oneTable.Value.Headers,
                        Rows = oneTable.Value.Rows
                    }
                };
            }

            return sheetWrites;
        }

        private static bool LooksLikeMarkdownDumpSheet(SheetWrite sw)
        {
            if (sw?.Rows == null || sw.Rows.Count == 0) return false;
            int pipeRows = 0;
            int sheetLabels = 0;
            foreach (var row in sw.Rows)
            {
                if (row == null || row.Count == 0) continue;
                var joined = string.Join(" ", row);
                if (joined.IndexOf('|') >= 0) pipeRows++;
                if (Regex.IsMatch(joined, @"^\s*Sheet\s*:\s*\S+", RegexOptions.IgnoreCase))
                    sheetLabels++;
                if (Regex.IsMatch(joined, @"^\s*Sheets\s*:", RegexOptions.IgnoreCase))
                    sheetLabels++;
            }
            return sheetLabels >= 1 || pipeRows >= 3;
        }

        private static void RejectMarkdownDumpedAsCells(List<SheetWrite> sheetWrites)
        {
            foreach (var sw in sheetWrites)
            {
                if (!LooksLikeMarkdownDumpSheet(sw)) continue;
                // Still looks like markdown after rescue → hard fail
                throw new ArgumentException(
                    "Refusing to write markdown/pipe-table text into Excel cells (that creates one messy sheet). " +
                    "Call file_write with JSON like: " +
                    "{\"mode\":\"overwrite_sheet\",\"sheets\":[" +
                    "{\"name\":\"SheetA\",\"headers\":[\"Col1\",\"Col2\"],\"rows\":[[\"a\",\"b\"]]}," +
                    "{\"name\":\"SheetB\",\"headers\":[\"Col1\",\"Col2\"],\"rows\":[[\"c\",\"d\"]]}" +
                    "]}. Each sheet name MUST be its own worksheet object — not rows of markdown on one sheet.");
            }
        }

        private static List<SheetWrite> ParseSheetSectionsFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<SheetWrite>();
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0)
                .ToList();
            return ParseSheetSectionsFromLines(lines);
        }

        private static List<SheetWrite> ParseSheetSectionsFromLines(List<string> lines)
        {
            var result = new List<SheetWrite>();
            if (lines == null || lines.Count == 0) return result;

            string currentName = null;
            var buf = new List<string>();

            void Flush()
            {
                if (string.IsNullOrWhiteSpace(currentName) || buf.Count == 0) { buf.Clear(); return; }
                var table = ParseMarkdownTable(buf);
                if (table.HasValue && (table.Value.Headers.Count > 0 || table.Value.Rows.Count > 0))
                {
                    result.Add(new SheetWrite
                    {
                        Name = SanitizeSheetName(currentName),
                        Headers = table.Value.Headers,
                        Rows = table.Value.Rows
                    });
                }
                buf.Clear();
            }

            foreach (var line in lines)
            {
                var m = Regex.Match(line, @"^\s*Sheet\s*:\s*(.+?)\s*$", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    Flush();
                    currentName = m.Groups[1].Value.Trim();
                    continue;
                }
                if (Regex.IsMatch(line, @"^\s*Sheets\s*:", RegexOptions.IgnoreCase))
                    continue; // index line, skip
                if (currentName != null)
                    buf.Add(line);
            }
            Flush();
            return result;
        }

        private static (List<string> Headers, List<List<string>> Rows)? ParseMarkdownTable(List<string> lines)
        {
            var tableLines = lines
                .Where(l => l.Trim().StartsWith("|"))
                .Where(l => !Regex.IsMatch(l.Trim(), @"^\|?\s*:?-{3,}"))
                .Select(l => l.Trim())
                .ToList();
            // also skip pure separator rows like |---|---|
            tableLines = tableLines.Where(l => !Regex.IsMatch(l.Replace(" ", ""), @"^\|?[-:|]+\|?$")).ToList();
            if (tableLines.Count == 0) return null;

            List<string> SplitMd(string line) =>
                line.Trim().Trim('|').Split('|')
                    .Select(c => c.Trim())
                    .ToList();

            var headers = SplitMd(tableLines[0]);
            var rows = new List<List<string>>();
            for (int i = 1; i < tableLines.Count; i++)
            {
                var cells = SplitMd(tableLines[i]);
                // pad/truncate to header width
                while (cells.Count < headers.Count) cells.Add("");
                if (cells.Count > headers.Count) cells = cells.Take(headers.Count).ToList();
                if (cells.All(string.IsNullOrWhiteSpace)) continue;
                rows.Add(cells);
            }
            return (headers, rows);
        }

        private static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sheet1";
            var s = name.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            s = s.Replace('[', '(').Replace(']', ')').Replace('*', '_').Replace('?', '_').Replace(':', ' ').Replace('/', '-').Replace('\\', '-');
            if (s.Length > 31) s = s.Substring(0, 31);
            return string.IsNullOrWhiteSpace(s) ? "Sheet1" : s;
        }

        private static string NormalizeMode(string mode)
        {
            var m = (mode ?? "append").Trim().ToLowerInvariant();
            return m switch
            {
                "overwrite" => "overwrite_sheet",
                "overwrite_sheet" => "overwrite_sheet",
                "replace" => "replace_file",
                "replace_file" => "replace_file",
                "create" => "replace_file",
                _ => "append"
            };
        }

        private static List<SheetWrite> ExpandSheetWrites(WriteSpec spec)
        {
            if (spec.Sheets != null && spec.Sheets.Count > 0)
                return spec.Sheets;

            if (spec.Rows.Count == 0 && (spec.Headers == null || spec.Headers.Count == 0))
                return new List<SheetWrite>();

            return new List<SheetWrite>
            {
                new SheetWrite
                {
                    Name = string.IsNullOrWhiteSpace(spec.Sheet) ? "Sheet1" : spec.Sheet.Trim(),
                    Headers = spec.Headers ?? new List<string>(),
                    Rows = spec.Rows ?? new List<List<string>>()
                }
            };
        }

        private static void AppendRows(SheetModel sheet, List<string> headers, List<List<string>> rows)
        {
            if (IsSheetEmpty(sheet))
            {
                sheet.Headers = headers;
                sheet.Rows = NormalizeRows(headers.Count, rows);
                return;
            }

            // Align to resolved headers (may widen existing sheet)
            if (sheet.Headers == null || sheet.Headers.Count == 0)
                sheet.Headers = headers;
            else if (headers.Count > sheet.Headers.Count)
            {
                while (sheet.Headers.Count < headers.Count)
                    sheet.Headers.Add(headers[sheet.Headers.Count]);
                foreach (var row in sheet.Rows)
                    while (row.Count < headers.Count) row.Add("");
            }

            sheet.Rows.AddRange(NormalizeRows(sheet.Headers.Count, rows));
        }

        private static SheetModel PrepareSheetForOverwrite(WorkbookModel workbook, string sheetName)
        {
            var name = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName.Trim();
            var existing = workbook.Sheets.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Name = name;
                existing.Headers = new List<string>();
                existing.Rows = new List<List<string>>();
                return existing;
            }

            var sheet = new SheetModel { Name = name };
            workbook.Sheets.Add(sheet);
            return sheet;
        }

        private static void TryRemoveUnusedDefaultSheet(WorkbookModel workbook, HashSet<string> keepNames)
        {
            if (workbook.Sheets.Count <= 1) return;
            var victim = workbook.Sheets.FirstOrDefault(s =>
                !keepNames.Contains(s.Name)
                && string.Equals(s.Name, "Sheet1", StringComparison.OrdinalIgnoreCase)
                && IsSheetEmpty(s));
            if (victim != null)
                workbook.Sheets.Remove(victim);
        }

        private static WriteSpec ParseWriteSpec(string content)
        {
            var spec = new WriteSpec
            {
                Mode = "append",
                Sheet = "Sheet1",
                Headers = new List<string>(),
                Rows = new List<List<string>>(),
                Sheets = new List<SheetWrite>()
            };

            if (string.IsNullOrWhiteSpace(content))
                return spec;

            var trimmed = content.Trim();
            if (trimmed.StartsWith("{"))
            {
                try
                {
                    var obj = JObject.Parse(trimmed);
                    var mode = obj.Value<string>("mode");
                    if (!string.IsNullOrWhiteSpace(mode))
                        spec.Mode = mode.Trim();
                    var sheet = obj.Value<string>("sheet") ?? obj.Value<string>("sheetName");
                    if (!string.IsNullOrWhiteSpace(sheet))
                        spec.Sheet = sheet.Trim();

                    var sheetsTok = obj["sheets"] as JArray;
                    if (sheetsTok != null)
                    {
                        foreach (var s in sheetsTok)
                        {
                            if (s is not JObject so) continue;
                            var sw = new SheetWrite
                            {
                                Name = (so.Value<string>("name") ?? so.Value<string>("sheet") ?? "Sheet1").Trim(),
                                Headers = new List<string>(),
                                Rows = new List<List<string>>()
                            };
                            if (so["headers"] is JArray hh)
                                sw.Headers = hh.Select(t => t?.ToString() ?? "").ToList();
                            if (so["rows"] is JArray rr)
                                foreach (var r in rr)
                                    sw.Rows.Add(RowFromToken(r));
                            spec.Sheets.Add(sw);
                        }
                        return spec;
                    }

                    var headersTok = obj["headers"] as JArray;
                    if (headersTok != null)
                        spec.Headers = headersTok.Select(t => t?.ToString() ?? "").ToList();

                    var rowsTok = obj["rows"] as JArray;
                    if (rowsTok != null)
                    {
                        foreach (var r in rowsTok)
                            spec.Rows.Add(RowFromToken(r));
                    }
                    else if (obj["row"] != null)
                    {
                        spec.Rows.Add(RowFromToken(obj["row"]));
                    }
                    else if (obj["values"] != null)
                    {
                        spec.Rows.Add(RowFromToken(obj["values"]));
                    }
                    else
                    {
                        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            { "mode", "sheet", "sheetName", "headers", "rows", "row", "values", "sheets" };
                        var props = obj.Properties().Where(p => !skip.Contains(p.Name)).ToList();
                        if (props.Count > 0)
                        {
                            if (spec.Headers.Count == 0)
                                spec.Headers = props.Select(p => p.Name).ToList();
                            spec.Rows.Add(props.Select(p => p.Value?.ToString() ?? "").ToList());
                        }
                    }

                    return spec;
                }
                catch (JsonException)
                {
                    // fall through to plain text
                }
            }

            // Plain text / CSV / TSV
            var lines = trimmed.Replace("\r\n", "\n").Replace('\r', '\n')
                .Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0)
                .ToList();

            if (lines.Count == 1 && lines[0].IndexOf('\t') < 0 && CountCommasOutsideQuotes(lines[0]) == 0)
            {
                spec.Headers = new List<string> { "Message" };
                spec.Rows.Add(new List<string> { lines[0] });
                return spec;
            }

            char sep = lines.Any(l => l.Contains('\t')) ? '\t' : ',';
            foreach (var line in lines)
                spec.Rows.Add(SplitCsvLine(line, sep));

            return spec;
        }

        private static List<string> RowFromToken(JToken token)
        {
            if (token == null) return new List<string>();
            if (token is JArray arr)
                return arr.Select(t => t?.ToString() ?? "").ToList();
            if (token is JObject o)
                return o.Properties().Select(p => p.Value?.ToString() ?? "").ToList();
            return new List<string> { token.ToString() };
        }

        private static List<string> ResolveHeaders(
            List<string> requested,
            List<List<string>> rows,
            SheetModel existing,
            bool replace)
        {
            if (requested != null && requested.Count > 0)
                return requested;

            if (!replace && existing != null && existing.Headers != null && existing.Headers.Count > 0)
                return existing.Headers.ToList();

            var width = rows.Count == 0 ? 1 : rows.Max(r => r.Count);
            if (width <= 1)
                return new List<string> { "Message" };

            return Enumerable.Range(1, width).Select(i => "Column" + i).ToList();
        }

        private static List<List<string>> NormalizeRows(int colCount, List<List<string>> rows)
        {
            var result = new List<List<string>>(rows?.Count ?? 0);
            if (rows == null) return result;
            foreach (var row in rows)
            {
                var cells = new List<string>(colCount);
                for (int i = 0; i < colCount; i++)
                    cells.Add(i < (row?.Count ?? 0) ? (row[i] ?? "") : "");
                result.Add(cells);
            }
            return result;
        }

        private static bool IsSheetEmpty(SheetModel sheet) =>
            sheet == null
            || ((sheet.Headers == null || sheet.Headers.Count == 0)
                && (sheet.Rows == null || sheet.Rows.Count == 0));

        private static SheetModel FindOrAddSheet(WorkbookModel workbook, string sheetName)
        {
            var name = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName.Trim();
            var existing = workbook.Sheets.FirstOrDefault(s =>
                string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return existing;

            if (workbook.Sheets.Count > 0 && string.Equals(name, "Sheet1", StringComparison.OrdinalIgnoreCase))
                return workbook.Sheets[0];

            var sheet = new SheetModel { Name = name };
            workbook.Sheets.Add(sheet);
            return sheet;
        }

        // ── Backend dispatch (UseOpenXml) ─────────────────────────────────────

        private static WorkbookModel LoadWorkbook(string fullPath) =>
            UseOpenXml ? LoadWorkbookOpenXml(fullPath) : LoadWorkbookGemBox(fullPath);

        private static void SaveWorkbook(string fullPath, WorkbookModel workbook)
        {
            if (UseOpenXml)
                SaveWorkbookOpenXml(fullPath, workbook);
            else
                SaveWorkbookGemBox(fullPath, workbook);
        }

        private static void EnsureGemBoxLicense()
        {
            // Same key used elsewhere in APP.BL. If invalid for GemBox 47, runtime stays on
            // whatever Program.cs set (often FREE-LIMITED-KEY → max 5 sheets). Flip UseOpenXml=true to bypass.
            try { SpreadsheetInfo.SetLicense("E1H5-CMM5-01EP-4OKK"); }
            catch { /* second SetLicense is ignored; invalid key throws on first ExcelFile use */ }
        }

        private static WorkbookModel LoadWorkbookGemBox(string fullPath)
        {
            EnsureGemBoxLicense();
            var model = new WorkbookModel();
            using var stm = File.OpenRead(fullPath);
            var ext = Path.GetExtension(fullPath) ?? "";
            ExcelFile ef;
            try
            {
                if (ext.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) && !ext.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    ef = ExcelFile.Load(stm, LoadOptions.XlsDefault);
                else
                    ef = ExcelFile.Load(stm, LoadOptions.XlsxDefault);
            }
            catch (FreeLimitReachedException ex)
            {
                throw new InvalidOperationException(
                    "GemBox free limit on load (max 5 sheets / 150 rows). Set UseOpenXml=true in GenericAgentExcelFileHelper, or configure a valid GemBox:SpreadsheetLicense.",
                    ex);
            }

            foreach (ExcelWorksheet ws in ef.Worksheets)
                model.Sheets.Add(ReadSheetGemBox(ws));
            return model;
        }

        private static SheetModel ReadSheetGemBox(ExcelWorksheet ws)
        {
            var sheet = new SheetModel { Name = ws.Name ?? "Sheet1" };
            int lastRow = FindLastUsedRowGemBox(ws);
            int lastCol = FindLastUsedColumnGemBox(ws);
            if (lastRow < 0 || lastCol < 0)
                return sheet;

            int colCount = lastCol + 1;
            var headers = new List<string>(colCount);
            for (int c = 0; c < colCount; c++)
            {
                var header = ws.Cells[0, c].Value?.ToString();
                var name = string.IsNullOrWhiteSpace(header) ? "Column" + (c + 1) : header;
                var baseName = name;
                int n = 2;
                while (headers.Contains(name, StringComparer.OrdinalIgnoreCase))
                    name = baseName + "_" + n++;
                headers.Add(name);
            }

            var rows = new List<List<string>>();
            for (int r = 1; r <= lastRow; r++)
            {
                var vals = new List<string>(colCount);
                bool any = false;
                for (int c = 0; c < colCount; c++)
                {
                    var v = ws.Cells[r, c].Value?.ToString() ?? "";
                    vals.Add(v);
                    if (!string.IsNullOrWhiteSpace(v)) any = true;
                }
                if (any) rows.Add(vals);
            }

            if (rows.Count == 0 && lastRow == 0)
            {
                headers = Enumerable.Range(1, colCount).Select(i => "Column" + i).ToList();
                var vals = new List<string>(colCount);
                for (int c = 0; c < colCount; c++)
                    vals.Add(ws.Cells[0, c].Value?.ToString() ?? "");
                rows.Add(vals);
            }

            sheet.Headers = headers;
            sheet.Rows = rows;
            return sheet;
        }

        private static int FindLastUsedRowGemBox(ExcelWorksheet ws)
        {
            const int scanMax = 10000;
            for (int r = scanMax; r >= 0; r--)
            {
                for (int c = 0; c < 50; c++)
                {
                    var v = ws.Cells[r, c].Value;
                    if (v != null && !string.IsNullOrWhiteSpace(v.ToString()))
                        return r;
                }
            }
            return -1;
        }

        private static int FindLastUsedColumnGemBox(ExcelWorksheet ws)
        {
            const int scanMax = 100;
            int last = -1;
            int lastRow = Math.Max(0, FindLastUsedRowGemBox(ws));
            for (int r = 0; r <= lastRow; r++)
            {
                for (int c = 0; c < scanMax; c++)
                {
                    var v = ws.Cells[r, c].Value;
                    if (v != null && !string.IsNullOrWhiteSpace(v.ToString()))
                        last = Math.Max(last, c);
                }
            }
            return last;
        }

        private static void SaveWorkbookGemBox(string fullPath, WorkbookModel workbook)
        {
            EnsureGemBoxLicense();

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in workbook.Sheets)
            {
                s.Name = MakeUniqueSheetName(SanitizeSheetName(s.Name), used);
                used.Add(s.Name);
            }

            var ef = new ExcelFile();
            // Remove default empty sheet after we add real ones
            foreach (var sheetModel in workbook.Sheets)
            {
                var ws = ef.Worksheets.Add(sheetModel.Name);
                var headers = sheetModel.Headers ?? new List<string>();
                var rows = sheetModel.Rows ?? new List<List<string>>();
                int colCount = headers.Count;
                if (colCount == 0 && rows.Count > 0)
                    colCount = rows.Max(r => r?.Count ?? 0);

                for (int c = 0; c < headers.Count; c++)
                    ws.Cells[0, c].Value = headers[c] ?? "";

                int startRow = headers.Count > 0 ? 1 : 0;
                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    int width = colCount > 0 ? colCount : (row?.Count ?? 0);
                    for (int c = 0; c < width; c++)
                        ws.Cells[startRow + r, c].Value = c < (row?.Count ?? 0) ? (row[c] ?? "") : "";
                }
            }

            // Drop GemBox's initial blank worksheet if we added named sheets
            if (ef.Worksheets.Count > 1)
            {
                var first = ef.Worksheets[0];
                bool firstIsDefaultEmpty =
                    string.Equals(first.Name, "Sheet1", StringComparison.OrdinalIgnoreCase)
                    && FindLastUsedRowGemBox(first) < 0
                    && !workbook.Sheets.Any(s => string.Equals(s.Name, first.Name, StringComparison.OrdinalIgnoreCase));
                if (firstIsDefaultEmpty)
                    ef.Worksheets.Remove(0);
            }

            var ext = Path.GetExtension(fullPath) ?? "";
            try
            {
                if (ext.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) && !ext.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    ef.Save(fullPath, SaveOptions.XlsDefault);
                else
                    ef.Save(fullPath, SaveOptions.XlsxDefault);
            }
            catch (FreeLimitReachedException ex)
            {
                throw new InvalidOperationException(
                    "GemBox free limit on save (max 5 sheets / 150 rows). Set UseOpenXml=true in GenericAgentExcelFileHelper, or configure a valid GemBox:SpreadsheetLicense. " +
                    $"Attempted {workbook.Sheets.Count} sheet(s): {string.Join(", ", workbook.Sheets.Select(s => s.Name))}.",
                    ex);
            }
        }

        // ── Open XML load / save ──────────────────────────────────────────────

        private static WorkbookModel LoadWorkbookOpenXml(string fullPath)
        {
            var model = new WorkbookModel();
            using var doc = SpreadsheetDocument.Open(fullPath, false);
            var wbPart = doc.WorkbookPart
                ?? throw new InvalidOperationException("Excel file has no workbook part: " + fullPath);

            var sst = wbPart.SharedStringTablePart?.SharedStringTable;
            var sheets = wbPart.Workbook?.Sheets?.Elements<OpenXmlSheet>().ToList()
                ?? new List<OpenXmlSheet>();

            foreach (var sheet in sheets)
            {
                var name = sheet.Name?.Value ?? "Sheet1";
                if (sheet.Id?.Value == null) continue;
                var wsPart = (WorksheetPart)wbPart.GetPartById(sheet.Id.Value);
                model.Sheets.Add(ReadSheetOpenXml(wsPart, name, sst));
            }

            return model;
        }

        private static SheetModel ReadSheetOpenXml(WorksheetPart wsPart, string name, SharedStringTable sst)
        {
            var sheet = new SheetModel { Name = name };
            var sheetData = wsPart.Worksheet?.GetFirstChild<SheetData>();
            if (sheetData == null) return sheet;

            var grid = new Dictionary<int, Dictionary<int, string>>();
            int maxCol = -1;
            int maxRow = -1;

            foreach (var row in sheetData.Elements<OpenXmlRow>())
            {
                if (row.RowIndex == null) continue;
                int r = (int)row.RowIndex.Value - 1; // 0-based
                if (r < 0) continue;
                maxRow = Math.Max(maxRow, r);

                foreach (var cell in row.Elements<OpenXmlCell>())
                {
                    int c = ColumnIndexFromReference(cell.CellReference?.Value);
                    if (c < 0) continue;
                    maxCol = Math.Max(maxCol, c);
                    if (!grid.TryGetValue(r, out var cols))
                    {
                        cols = new Dictionary<int, string>();
                        grid[r] = cols;
                    }
                    cols[c] = GetCellText(cell, sst);
                }
            }

            if (maxRow < 0 || maxCol < 0)
                return sheet;

            int colCount = maxCol + 1;
            string CellAt(int r, int c)
            {
                if (grid.TryGetValue(r, out var cols) && cols.TryGetValue(c, out var v))
                    return v ?? "";
                return "";
            }

            // Header row (row 0)
            var headers = new List<string>(colCount);
            for (int c = 0; c < colCount; c++)
            {
                var header = CellAt(0, c);
                var colName = string.IsNullOrWhiteSpace(header) ? "Column" + (c + 1) : header;
                var baseName = colName;
                int n = 2;
                while (headers.Contains(colName, StringComparer.OrdinalIgnoreCase))
                    colName = baseName + "_" + n++;
                headers.Add(colName);
            }

            // Data rows (from row 1)
            var rows = new List<List<string>>();
            for (int r = 1; r <= maxRow; r++)
            {
                var vals = new List<string>(colCount);
                bool any = false;
                for (int c = 0; c < colCount; c++)
                {
                    var v = CellAt(r, c);
                    vals.Add(v);
                    if (!string.IsNullOrWhiteSpace(v)) any = true;
                }
                if (any) rows.Add(vals);
            }

            // Single-row file: treat as data with Column1..N
            if (rows.Count == 0 && maxRow == 0)
            {
                headers = Enumerable.Range(1, colCount).Select(i => "Column" + i).ToList();
                var vals = new List<string>(colCount);
                for (int c = 0; c < colCount; c++)
                    vals.Add(CellAt(0, c));
                rows.Add(vals);
            }

            sheet.Headers = headers;
            sheet.Rows = rows;
            return sheet;
        }

        private static string GetCellText(OpenXmlCell cell, SharedStringTable sst)
        {
            if (cell == null) return "";

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (sst != null && int.TryParse(cell.InnerText, out int idx))
                {
                    var item = sst.Elements<SharedStringItem>().ElementAtOrDefault(idx);
                    return item?.InnerText ?? "";
                }
                return cell.InnerText ?? "";
            }

            if (cell.DataType != null && cell.DataType.Value == CellValues.InlineString)
                return cell.InlineString?.Text?.Text
                    ?? cell.InlineString?.InnerText
                    ?? "";

            if (cell.CellValue != null)
                return cell.CellValue.Text ?? "";

            return cell.InnerText ?? "";
        }

        private static int ColumnIndexFromReference(string cellRef)
        {
            if (string.IsNullOrEmpty(cellRef)) return -1;
            int col = 0;
            foreach (char ch in cellRef)
            {
                if (ch >= 'A' && ch <= 'Z')
                    col = col * 26 + (ch - 'A' + 1);
                else if (ch >= 'a' && ch <= 'z')
                    col = col * 26 + (ch - 'a' + 1);
                else
                    break;
            }
            return col - 1; // 0-based; -1 if no letters
        }

        private static void SaveWorkbookOpenXml(string fullPath, WorkbookModel workbook)
        {
            // Ensure unique sheet names within Excel limits
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in workbook.Sheets)
            {
                s.Name = MakeUniqueSheetName(SanitizeSheetName(s.Name), used);
                used.Add(s.Name);
            }

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            using var doc = SpreadsheetDocument.Create(fullPath, SpreadsheetDocumentType.Workbook);
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var sheets = new OpenXmlSheets();
            wbPart.Workbook.Append(sheets);

            uint sheetId = 1;
            foreach (var sheetModel in workbook.Sheets)
            {
                var wsPart = wbPart.AddNewPart<WorksheetPart>();
                wsPart.Worksheet = BuildWorksheetOpenXml(sheetModel);
                sheets.Append(new OpenXmlSheet
                {
                    Id = wbPart.GetIdOfPart(wsPart),
                    SheetId = sheetId++,
                    Name = sheetModel.Name
                });
            }

            wbPart.Workbook.Save();
        }

        private static OpenXmlWorksheet BuildWorksheetOpenXml(SheetModel sheet)
        {
            var sheetData = new SheetData();
            var headers = sheet.Headers ?? new List<string>();
            var rows = sheet.Rows ?? new List<List<string>>();
            int colCount = headers.Count;
            if (colCount == 0 && rows.Count > 0)
                colCount = rows.Max(r => r?.Count ?? 0);

            uint rowIndex = 1;
            if (headers.Count > 0)
            {
                sheetData.Append(BuildRowOpenXml(rowIndex++, headers));
            }

            foreach (var row in rows)
            {
                var cells = new List<string>(colCount);
                for (int c = 0; c < colCount; c++)
                    cells.Add(c < (row?.Count ?? 0) ? (row[c] ?? "") : "");
                if (headers.Count == 0 && colCount == 0 && row != null)
                    cells = row.Select(x => x ?? "").ToList();
                sheetData.Append(BuildRowOpenXml(rowIndex++, cells));
            }

            return new OpenXmlWorksheet(sheetData);
        }

        private static OpenXmlRow BuildRowOpenXml(uint rowIndex, List<string> values)
        {
            var row = new OpenXmlRow { RowIndex = rowIndex };
            for (int c = 0; c < values.Count; c++)
            {
                var text = values[c] ?? "";
                var cell = new OpenXmlCell
                {
                    CellReference = ColumnNameFromIndex(c) + rowIndex,
                    DataType = CellValues.InlineString,
                    InlineString = new InlineString(new Text(text))
                };
                row.Append(cell);
            }
            return row;
        }

        private static string ColumnNameFromIndex(int zeroBasedIndex)
        {
            int n = zeroBasedIndex + 1;
            var sb = new StringBuilder();
            while (n > 0)
            {
                n--;
                sb.Insert(0, (char)('A' + (n % 26)));
                n /= 26;
            }
            return sb.ToString();
        }

        private static string MakeUniqueSheetName(string name, HashSet<string> used)
        {
            if (!used.Contains(name)) return name;
            for (int i = 2; i < 1000; i++)
            {
                var suffix = "_" + i;
                var baseName = name.Length + suffix.Length > 31
                    ? name.Substring(0, Math.Max(1, 31 - suffix.Length))
                    : name;
                var candidate = baseName + suffix;
                if (!used.Contains(candidate)) return candidate;
            }
            return name.Substring(0, Math.Min(28, name.Length)) + "_" + Guid.NewGuid().ToString("N").Substring(0, 2);
        }

        private static int CountCommasOutsideQuotes(string line)
        {
            int n = 0;
            bool inQ = false;
            foreach (var ch in line)
            {
                if (ch == '"') inQ = !inQ;
                else if (ch == ',' && !inQ) n++;
            }
            return n;
        }

        private static List<string> SplitCsvLine(string line, char sep)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else inQ = !inQ;
                }
                else if (ch == sep && !inQ)
                {
                    list.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(ch);
            }
            list.Add(sb.ToString());
            return list;
        }

        private sealed class WorkbookModel
        {
            public List<SheetModel> Sheets { get; set; } = new List<SheetModel>();
        }

        private sealed class SheetModel
        {
            public string Name { get; set; }
            public List<string> Headers { get; set; } = new List<string>();
            public List<List<string>> Rows { get; set; } = new List<List<string>>();
        }

        private sealed class WriteSpec
        {
            public string Mode { get; set; }
            public string Sheet { get; set; }
            public List<string> Headers { get; set; }
            public List<List<string>> Rows { get; set; }
            public List<SheetWrite> Sheets { get; set; }
        }

        private sealed class SheetWrite
        {
            public string Name { get; set; }
            public List<string> Headers { get; set; }
            public List<List<string>> Rows { get; set; }
        }
    }
}
