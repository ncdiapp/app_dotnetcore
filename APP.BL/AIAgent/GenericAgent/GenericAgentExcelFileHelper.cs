using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using GemBox.Spreadsheet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Excel (.xlsx / .xls) helpers for agent file tools.
    /// Reuses GemBox (same stack as AppImportExportExcelToDataTableBL / FileProcessTools).
    /// LLM-facing content is tabular JSON / plain lines — never raw binary.
    /// </summary>
    public static class GenericAgentExcelFileHelper
    {
        public const int MaxReadRows = 500;
        public const int MaxWriteRows = 5000;

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

            var ext = Path.GetExtension(fullPath);
            using var stm = File.OpenRead(fullPath);
            var ef = LoadExcel(stm, ext);
            var ws = ef.Worksheets.Count > 0 ? ef.Worksheets[0] : null;
            if (ws == null)
                return new { Format = "excel", Sheet = (string)null, Headers = Array.Empty<string>(), Rows = Array.Empty<object[]>(), RowCount = 0, Truncated = false };

            var table = ExtractSheet(ws);
            var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToArray();
            var take = Math.Min(Math.Max(1, maxRows), table.Rows.Count);
            var rows = new List<object[]>(take);
            for (int i = 0; i < take; i++)
            {
                var dr = table.Rows[i];
                var cells = new object[headers.Length];
                for (int c = 0; c < headers.Length; c++)
                    cells[c] = dr[c] == DBNull.Value ? "" : dr[c]?.ToString() ?? "";
                rows.Add(cells);
            }

            return new
            {
                Format = "excel",
                Sheet = ws.Name,
                Headers = headers,
                Rows = rows,
                RowCount = table.Rows.Count,
                Truncated = table.Rows.Count > take
            };
        }

        /// <summary>
        /// Writes Excel from agent content.
        /// Preferred JSON:
        ///   {"mode":"append"|"overwrite","sheet":"Log","headers":["A","B"],"rows":[["v1","v2"]]}
        /// Also accepts plain text: one line = one row (single Message column), or CSV/TSV lines.
        /// </summary>
        public static object WriteFromAgentContent(string fullPath, string content)
        {
            var spec = ParseWriteSpec(content);
            if (spec.Rows.Count == 0 && spec.Mode != "overwrite")
                throw new ArgumentException("Excel write content produced no rows. Use JSON {mode,sheet,headers,rows} or plain/CSV lines.");

            if (spec.Rows.Count > MaxWriteRows)
                throw new InvalidOperationException($"Excel write exceeds row limit ({MaxWriteRows}).");

            var ext = Path.GetExtension(fullPath);
            var isXlsx = !string.Equals(ext, ".xls", StringComparison.OrdinalIgnoreCase);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");

            ExcelFile ef;
            ExcelWorksheet ws;
            bool createdNew = false;

            if (File.Exists(fullPath) && string.Equals(spec.Mode, "append", StringComparison.OrdinalIgnoreCase))
            {
                using var stm = File.OpenRead(fullPath);
                ef = LoadExcel(stm, ext);
                ws = FindOrAddSheet(ef, spec.Sheet);
            }
            else
            {
                createdNew = true;
                ef = new ExcelFile();
                ws = ef.Worksheets.Add(string.IsNullOrWhiteSpace(spec.Sheet) ? "Sheet1" : spec.Sheet.Trim());
            }

            var existing = ExtractSheet(ws);
            var headers = ResolveHeaders(spec.Headers, spec.Rows, existing, createdNew || string.Equals(spec.Mode, "overwrite", StringComparison.OrdinalIgnoreCase));

            if (createdNew || string.Equals(spec.Mode, "overwrite", StringComparison.OrdinalIgnoreCase))
            {
                // Clear sheet by replacing workbook sheet content via InsertDataTable
                if (!createdNew)
                {
                    // GemBox: clear used range simply by creating a fresh workbook/sheet when overwrite
                    ef = new ExcelFile();
                    ws = ef.Worksheets.Add(string.IsNullOrWhiteSpace(spec.Sheet) ? "Sheet1" : spec.Sheet.Trim());
                }

                var dt = BuildTable(headers, spec.Rows);
                ws.InsertDataTable(dt, new InsertDataTableOptions
                {
                    ColumnHeaders = true,
                    StartRow = 0,
                    StartColumn = 0
                });
            }
            else
            {
                // Append: ensure header row exists; write after last used row
                int startRow = 0;
                if (ws.Rows.Count == 0 || IsSheetEmpty(ws))
                {
                    for (int c = 0; c < headers.Count; c++)
                        ws.Cells[0, c].Value = headers[c];
                    startRow = 1;
                }
                else
                {
                    startRow = FindLastUsedRow(ws) + 1;
                    // If first row looks empty of headers but we have data, still append at end
                }

                foreach (var row in spec.Rows)
                {
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var val = c < row.Count ? row[c] : "";
                        ws.Cells[startRow, c].Value = val;
                    }
                    startRow++;
                }
            }

            if (isXlsx)
                ef.Save(fullPath, SaveOptions.XlsxDefault);
            else
                ef.Save(fullPath, SaveOptions.XlsDefault);

            AssertLooksLikeExcelFile(fullPath);

            return new
            {
                Ok = true,
                Format = isXlsx ? "xlsx" : "xls",
                RelativePathHint = Path.GetFileName(fullPath),
                Mode = createdNew ? "create" : spec.Mode,
                Sheet = ws.Name,
                RowsWritten = spec.Rows.Count,
                Headers = headers
            };
        }

        private static WriteSpec ParseWriteSpec(string content)
        {
            var spec = new WriteSpec
            {
                Mode = "append",
                Sheet = "Sheet1",
                Headers = new List<string>(),
                Rows = new List<List<string>>()
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
                        // Flat object → one row by header order or property values
                        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            { "mode", "sheet", "sheetName", "headers", "rows", "row", "values" };
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
                // Single free-text log line → one Message column
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
            DataTable existing,
            bool replace)
        {
            if (requested != null && requested.Count > 0)
                return requested;

            if (!replace && existing != null && existing.Columns.Count > 0)
                return existing.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            var width = rows.Count == 0 ? 1 : rows.Max(r => r.Count);
            if (width <= 1)
                return new List<string> { "Message" };

            return Enumerable.Range(1, width).Select(i => "Column" + i).ToList();
        }

        private static DataTable BuildTable(List<string> headers, List<List<string>> rows)
        {
            var dt = new DataTable();
            foreach (var h in headers)
                dt.Columns.Add(string.IsNullOrWhiteSpace(h) ? "Column" : h);

            foreach (var row in rows)
            {
                var vals = new object[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                    vals[i] = i < row.Count ? row[i] : "";
                dt.Rows.Add(vals);
            }
            return dt;
        }

        private static DataTable ExtractSheet(ExcelWorksheet ws)
        {
            // Used range approximation: scan for last non-empty cell
            int lastRow = FindLastUsedRow(ws);
            int lastCol = FindLastUsedColumn(ws);
            if (lastRow < 0 || lastCol < 0)
                return new DataTable();

            int colCount = lastCol + 1;
            var dt = new DataTable();
            for (int c = 0; c < colCount; c++)
            {
                var header = ws.Cells[0, c].Value?.ToString();
                var name = string.IsNullOrWhiteSpace(header) ? "Column" + (c + 1) : header;
                // unique column names
                var baseName = name;
                int n = 2;
                while (dt.Columns.Contains(name))
                    name = baseName + "_" + n++;
                dt.Columns.Add(name);
            }

            // If only one row and it was used as headers with no data, return headers-only empty
            for (int r = 1; r <= lastRow; r++)
            {
                var vals = new object[colCount];
                bool any = false;
                for (int c = 0; c < colCount; c++)
                {
                    var v = ws.Cells[r, c].Value?.ToString() ?? "";
                    vals[c] = v;
                    if (!string.IsNullOrWhiteSpace(v)) any = true;
                }
                if (any) dt.Rows.Add(vals);
            }

            // No header row case: if row 0 has data and we treated it as headers but there were no further rows,
            // and headers look like data — still OK for LLM preview.
            if (dt.Rows.Count == 0 && lastRow == 0)
            {
                // Single row file: expose that row as data with Column1..N
                dt = new DataTable();
                for (int c = 0; c < colCount; c++)
                    dt.Columns.Add("Column" + (c + 1));
                var vals = new object[colCount];
                for (int c = 0; c < colCount; c++)
                    vals[c] = ws.Cells[0, c].Value?.ToString() ?? "";
                dt.Rows.Add(vals);
            }

            return dt;
        }

        private static int FindLastUsedRow(ExcelWorksheet ws)
        {
            // Walk from a generous max downward — agent logs are small
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

        private static int FindLastUsedColumn(ExcelWorksheet ws)
        {
            const int scanMax = 100;
            int last = -1;
            int lastRow = Math.Max(0, FindLastUsedRow(ws));
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

        private static bool IsSheetEmpty(ExcelWorksheet ws) => FindLastUsedRow(ws) < 0;

        private static ExcelWorksheet FindOrAddSheet(ExcelFile ef, string sheetName)
        {
            var name = string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName.Trim();
            foreach (ExcelWorksheet ws in ef.Worksheets)
            {
                if (string.Equals(ws.Name, name, StringComparison.OrdinalIgnoreCase))
                    return ws;
            }
            if (ef.Worksheets.Count > 0 && string.Equals(name, "Sheet1", StringComparison.OrdinalIgnoreCase))
                return ef.Worksheets[0];
            return ef.Worksheets.Add(name);
        }

        private static ExcelFile LoadExcel(Stream stm, string extension)
        {
            var ext = extension ?? "";
            if (ext.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return ExcelFile.Load(stm, LoadOptions.XlsxDefault);
            if (ext.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                return ExcelFile.Load(stm, LoadOptions.XlsDefault);
            return ExcelFile.Load(stm, LoadOptions.XlsxDefault);
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

        private sealed class WriteSpec
        {
            public string Mode { get; set; }
            public string Sheet { get; set; }
            public List<string> Headers { get; set; }
            public List<List<string>> Rows { get; set; }
        }
    }
}
