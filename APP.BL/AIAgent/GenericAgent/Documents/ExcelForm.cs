using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// One worksheet. Title and sections are merged rows. Field pairs use four cells. Grids sit under their section.
    /// Style indexes are the read-back contract.
    /// </summary>
    public static class ExcelForm
    {
        public const uint StyleTitle = 1;
        public const uint StyleSection = 2;
        public const uint StyleNested = 3;
        public const uint StyleLabel = 4;
        public const uint StyleValue = 5;
        public const uint StyleHeader = 6;
        public const uint StyleCell = 7;
        public const uint StyleNumber = 8;
        public const uint StyleNotes = 9;

        public static byte[] Write(FormDocument doc)
        {
            var span = doc.ColumnSpan();
            using var ms = new MemoryStream();
            using (var workbook = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
            {
                var wb = workbook.AddWorkbookPart();
                wb.Workbook = new Workbook();
                var styles = wb.AddNewPart<WorkbookStylesPart>();
                styles.Stylesheet = BuildStyles();
                styles.Stylesheet.Save();

                var ws = wb.AddNewPart<WorksheetPart>();
                var data = new SheetData();
                var merges = new MergeCells();
                uint row = 1;

                if (!string.IsNullOrEmpty(doc.Title))
                {
                    WriteMerged(data, merges, row, span, doc.Title, StyleTitle);
                    row++;
                    WriteBlank(data, row, span);
                    row++;
                }
                row = WriteFields(data, row, doc.Fields);
                foreach (var section in doc.Sections)
                    row = WriteSection(data, merges, row, span, section);
                if (!string.IsNullOrEmpty(doc.Notes))
                {
                    WriteBlank(data, row, span);
                    row++;
                    WriteMerged(data, merges, row, span, doc.Notes, StyleNotes);
                }

                var worksheet = new Worksheet();
                worksheet.Append(BuildColumns(span));
                worksheet.Append(data);
                if (merges.ChildElements.Count > 0)
                    worksheet.Append(merges);
                ws.Worksheet = worksheet;
                ws.Worksheet.Save();

                var sheets = wb.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = wb.GetIdOfPart(ws),
                    SheetId = 1,
                    Name = "Document"
                });
                wb.Workbook.Save();
            }
            return ms.ToArray();
        }

        public static FormDocument ReadXls(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes ?? Array.Empty<byte>());
            var file = GemBox.Spreadsheet.ExcelFile.Load(ms, GemBox.Spreadsheet.LoadOptions.XlsDefault);
            var sheet = file.Worksheets[0];
            var used = sheet.GetUsedCellRange(true);
            if (used == null)
                return new FormDocument();
            var columns = new List<string>();
            var rows = new List<List<FormCell>>();
            for (var c = 0; c < used.Width && c < DocumentStyles.MaxColumns; c++)
                columns.Add(sheet.Cells[used.FirstRowIndex, used.FirstColumnIndex + c].Value?.ToString() ?? "");
            for (var r = 1; r < used.Height && rows.Count < DocumentStyles.MaxDataRows; r++)
            {
                var row = new List<FormCell>();
                for (var c = 0; c < columns.Count; c++)
                {
                    var value = sheet.Cells[used.FirstRowIndex + r, used.FirstColumnIndex + c].Value;
                    var text = value?.ToString() ?? "";
                    row.Add(new FormCell
                    {
                        Text = text,
                        IsNumber = value is double || value is int || value is decimal || value is float
                    });
                }
                rows.Add(row);
            }
            return FormDocument.FromTable(sheet.Name ?? "", columns, rows);
        }

        public static FormDocument Read(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes ?? Array.Empty<byte>());
            using var workbook = SpreadsheetDocument.Open(ms, false);
            var ws = workbook.WorkbookPart.WorksheetParts.First();
            var sheet = ws.Worksheet;
            var rows = sheet.GetFirstChild<SheetData>().Elements<Row>().ToList();
            var doc = new FormDocument();
            FormSection current = null;
            FormSection parent = null;
            FormTable table = null;

            foreach (var row in rows)
            {
                var cells = ReadRow(row);
                if (cells.Count == 0) continue;
                var style = cells[0].Style;
                var text = cells[0].Text ?? "";
                if (style == StyleTitle)
                {
                    doc.Title = text;
                    table = null;
                    continue;
                }
                if (style == StyleNotes)
                {
                    doc.Notes = text;
                    table = null;
                    continue;
                }
                if (style == StyleSection || style == StyleNested)
                {
                    var level = style == StyleNested ? 3 : 2;
                    var section = new FormSection { Title = text, Level = level };
                    if (level >= 3 && parent != null)
                        parent.Sections.Add(section);
                    else
                    {
                        doc.Sections.Add(section);
                        parent = section;
                    }
                    current = section;
                    table = null;
                    continue;
                }
                if (style == StyleHeader)
                {
                    var section = EnsureSection(doc, ref current);
                    table = new FormTable();
                    foreach (var cell in cells)
                        table.Columns.Add(cell.Text ?? "");
                    section.Tables.Add(table);
                    continue;
                }
                if (table != null && (style == StyleCell || style == StyleNumber))
                {
                    var rowCells = new List<FormCell>();
                    for (var i = 0; i < table.Columns.Count; i++)
                    {
                        var found = cells.FirstOrDefault(c => c.Column == i + 1);
                        rowCells.Add(new FormCell
                        {
                            Text = found.Text ?? "",
                            IsNumber = found.Style == StyleNumber
                        });
                    }
                    table.Rows.Add(rowCells);
                    continue;
                }
                if (style == StyleLabel || style == StyleValue)
                {
                    table = null;
                    var fields = current == null ? doc.Fields : current.Fields;
                    for (var i = 0; i + 1 < cells.Count; i += 2)
                    {
                        if (cells[i].Style != StyleLabel) continue;
                        var label = cells[i].Text ?? "";
                        var value = cells[i + 1].Text ?? "";
                        if (label.Length == 0 && value.Length == 0) continue;
                        fields.Add(new FormField { Label = label, Value = value, IsNumber = cells[i + 1].Style == StyleNumber });
                    }
                }
            }
            return doc;
        }

        private static FormSection EnsureSection(FormDocument doc, ref FormSection current)
        {
            if (current != null) return current;
            current = new FormSection { Level = 2 };
            doc.Sections.Add(current);
            return current;
        }

        private static uint WriteSection(SheetData data, MergeCells merges, uint row, int span, FormSection section)
        {
            if (!string.IsNullOrEmpty(section.Title))
            {
                WriteBlank(data, row, span);
                row++;
                var style = section.Level >= 3 ? StyleNested : StyleSection;
                WriteMerged(data, merges, row, span, section.Title, style);
                row++;
            }
            row = WriteFields(data, row, section.Fields);
            foreach (var table in section.Tables)
            {
                if (!string.IsNullOrEmpty(table.Title))
                {
                    WriteMerged(data, merges, row, span, table.Title, StyleNested);
                    row++;
                }
                var header = table.Columns.Select(c => new FormCell { Text = c }).ToList();
                WriteCells(data, row, header, StyleHeader, true);
                row++;
                foreach (var cells in table.Rows)
                {
                    WriteCells(data, row, cells, StyleCell, false);
                    row++;
                }
            }
            foreach (var child in section.Sections)
                row = WriteSection(data, merges, row, span, child);
            return row;
        }

        private static uint WriteFields(SheetData data, uint row, List<FormField> fields)
        {
            if (fields == null || fields.Count == 0) return row;
            for (var i = 0; i < fields.Count; i += 2)
            {
                var line = new List<(string text, uint style)>();
                line.Add((fields[i].Label ?? "", StyleLabel));
                line.Add((fields[i].Value ?? "", fields[i].IsNumber ? StyleNumber : StyleValue));
                if (i + 1 < fields.Count)
                {
                    line.Add((fields[i + 1].Label ?? "", StyleLabel));
                    line.Add((fields[i + 1].Value ?? "", fields[i + 1].IsNumber ? StyleNumber : StyleValue));
                }
                var xrow = new Row { RowIndex = row };
                for (var c = 0; c < line.Count; c++)
                    xrow.Append(Inline(c + 1, row, line[c].text, line[c].style));
                data.Append(xrow);
                row++;
            }
            return row;
        }

        private static void WriteCells(SheetData data, uint row, List<FormCell> cells, uint headerStyle, bool header)
        {
            var xrow = new Row { RowIndex = row };
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i] ?? new FormCell();
                var style = header ? headerStyle : (cell.IsNumber ? StyleNumber : StyleCell);
                xrow.Append(Inline(i + 1, row, cell.Text, style));
            }
            data.Append(xrow);
        }

        private static void WriteMerged(SheetData data, MergeCells merges, uint row, int span, string text, uint style)
        {
            var xrow = new Row { RowIndex = row };
            xrow.Append(Inline(1, row, text, style));
            data.Append(xrow);
            if (span > 1)
                merges.Append(new MergeCell { Reference = "A" + row.ToString(CultureInfo.InvariantCulture) + ":" + ColumnName(span) + row.ToString(CultureInfo.InvariantCulture) });
        }

        private static void WriteBlank(SheetData data, uint row, int span)
        {
            var xrow = new Row { RowIndex = row, Height = 8, CustomHeight = true };
            xrow.Append(Inline(1, row, "", 0));
            data.Append(xrow);
        }

        private static Cell Inline(int col, uint row, string text, uint style)
        {
            var cell = new Cell
            {
                CellReference = ColumnName(col) + row.ToString(CultureInfo.InvariantCulture),
                DataType = CellValues.InlineString,
                StyleIndex = style
            };
            cell.Append(new InlineString(new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
            return cell;
        }

        private static List<(int Column, string Text, uint Style)> ReadRow(Row row)
        {
            var list = new List<(int, string, uint)>();
            foreach (var cell in row.Elements<Cell>())
            {
                var col = ColumnIndex(cell.CellReference?.Value);
                if (col <= 0) continue;
                list.Add((col, CellText(cell), cell.StyleIndex?.Value ?? 0));
            }
            list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return list;
        }

        private static string CellText(Cell cell)
        {
            if (cell.InlineString != null)
                return cell.InlineString.Text?.Text ?? cell.InlineString.InnerText ?? "";
            return cell.CellValue?.Text ?? "";
        }

        private static int ColumnIndex(string reference)
        {
            if (string.IsNullOrEmpty(reference)) return 0;
            var n = 0;
            foreach (var ch in reference)
            {
                if (ch < 'A' || ch > 'Z') break;
                n = n * 26 + (ch - 'A' + 1);
            }
            return n;
        }

        private static string ColumnName(int col)
        {
            var s = "";
            while (col > 0)
            {
                col--;
                s = (char)('A' + (col % 26)) + s;
                col /= 26;
            }
            return s;
        }

        private static Columns BuildColumns(int span)
        {
            var columns = new Columns();
            for (var i = 1; i <= span; i++)
            {
                columns.Append(new Column
                {
                    Min = (uint)i,
                    Max = (uint)i,
                    Width = i % 2 == 1 ? 22 : 28,
                    CustomWidth = true
                });
            }
            return columns;
        }

        private static Stylesheet BuildStyles()
        {
            var fonts = new Fonts(
                Font(10, false, "1a1a1a"),
                Font(18, true, "1a1a1a"),
                Font(13, true, "1a1a1a"),
                Font(12, true, "1a1a1a"),
                Font(10, true, "555555"),
                Font(10, false, "1a1a1a"),
                Font(10, true, "ffffff"),
                Font(10, false, "1a1a1a"),
                Font(10, false, "1a1a1a"),
                Font(10, false, "333333"))
            { Count = 10 };

            var fills = new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                Solid("f0f2f4"),
                Solid("3d4f5f"))
            { Count = 4 };

            var borders = new Borders(
                new Border(),
                Box())
            { Count = 2 };

            var formats = new CellFormats(
                new CellFormat(),
                Format(1, 0, 0, HorizontalAlignmentValues.Center),
                Format(2, 2, 0, HorizontalAlignmentValues.Left),
                Format(3, 0, 0, HorizontalAlignmentValues.Left),
                Format(4, 0, 0, HorizontalAlignmentValues.Left),
                Format(5, 0, 0, HorizontalAlignmentValues.Left),
                Format(6, 3, 1, HorizontalAlignmentValues.Left),
                Format(7, 0, 1, HorizontalAlignmentValues.Left),
                Format(8, 0, 1, HorizontalAlignmentValues.Right),
                Format(9, 0, 0, HorizontalAlignmentValues.Left))
            { Count = 10 };

            return new Stylesheet(fonts, fills, borders, formats);
        }

        private static Font Font(double size, bool bold, string rgb)
        {
            var font = new Font(
                new FontSize { Val = size },
                new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FF" + rgb },
                new FontName { Val = DocumentStyles.FontFamily });
            if (bold) font.Append(new Bold());
            return font;
        }

        private static Fill Solid(string rgb)
        {
            return new Fill(new PatternFill(
                new ForegroundColor { Rgb = "FF" + rgb })
            { PatternType = PatternValues.Solid });
        }

        private static Border Box()
        {
            var color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FFdddddd" };
            return new Border(
                new LeftBorder { Style = BorderStyleValues.Thin, Color = (DocumentFormat.OpenXml.Spreadsheet.Color)color.CloneNode(true) },
                new RightBorder { Style = BorderStyleValues.Thin, Color = (DocumentFormat.OpenXml.Spreadsheet.Color)color.CloneNode(true) },
                new TopBorder { Style = BorderStyleValues.Thin, Color = (DocumentFormat.OpenXml.Spreadsheet.Color)color.CloneNode(true) },
                new BottomBorder { Style = BorderStyleValues.Thin, Color = (DocumentFormat.OpenXml.Spreadsheet.Color)color.CloneNode(true) });
        }

        private static CellFormat Format(uint font, uint fill, uint border, HorizontalAlignmentValues align)
        {
            return new CellFormat
            {
                FontId = font,
                FillId = fill,
                BorderId = border,
                ApplyFont = true,
                ApplyFill = fill > 0,
                ApplyBorder = border > 0,
                ApplyAlignment = true,
                Alignment = new Alignment { Horizontal = align, Vertical = VerticalAlignmentValues.Center, WrapText = true }
            };
        }
    }
}
