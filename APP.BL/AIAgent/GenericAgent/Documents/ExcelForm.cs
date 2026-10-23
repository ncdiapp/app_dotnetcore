using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using A = DocumentFormat.OpenXml.Drawing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

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
                var pictures = new List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)>();
                var imageColumns = new HashSet<int>();
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
                    row = WriteSection(data, merges, row, span, section, pictures, imageColumns);
                if (!string.IsNullOrEmpty(doc.Notes))
                {
                    WriteBlank(data, row, span);
                    row++;
                    WriteMerged(data, merges, row, span, doc.Notes, StyleNotes);
                }

                var worksheet = new Worksheet();
                worksheet.Append(BuildColumns(span, imageColumns));
                worksheet.Append(data);
                if (merges.ChildElements.Count > 0)
                    worksheet.Append(merges);
                ws.Worksheet = worksheet;
                if (pictures.Count > 0)
                    AddCardPictures(ws, pictures);
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

        /// <summary>One sheet. Each row of the sheet holds cardsPerRow cards. cardRows is ignored.</summary>
        public static byte[] WriteCards(CardSheet sheet)
        {
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
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
                var pictures = new List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)>();
                uint row = 1;
                if (!string.IsNullOrEmpty(sheet.Title))
                {
                    var title = new Row { RowIndex = row };
                    title.Append(Inline(1, row, sheet.Title, StyleTitle));
                    ApplyHeight(title, ContentHeight(sheet.Title, 13, ColumnWidth(1) * perRow));
                    data.Append(title);
                    row++;
                }
                for (var i = 0; i < sheet.Cards.Count; i += perRow)
                {
                    var slice = new List<List<CardField>>();
                    var fieldCount = 0;
                    for (var c = 0; c < perRow && i + c < sheet.Cards.Count; c++)
                    {
                        slice.Add(sheet.Cards[i + c]);
                        fieldCount = Math.Max(fieldCount, sheet.Cards[i + c].Count);
                    }
                    for (var f = 0; f < fieldCount; f++)
                    {
                        var xrow = new Row { RowIndex = row };
                        double height = 0;
                        for (var c = 0; c < slice.Count; c++)
                        {
                            var field = f < slice[c].Count ? slice[c][f] : new CardField();
                            var hasPicture = field.ImageBytes != null && field.ImageBytes.Length > 0;
                            var imageSlot = hasPicture || field.IsImage;
                            var text = hasPicture || field.IsImage
                                ? (field.Label ?? "")
                                : (string.IsNullOrEmpty(field.Label) ? field.Value : field.Label + ": " + field.Value);
                            xrow.Append(Inline(c + 1, row, text, field.IsImage ? StyleValue : StyleCell));
                            if (hasPicture)
                            {
                                var box = ImagePixels.Fit(field.ImageBytes, 110, 98);
                                pictures.Add((c + 1, row, field.ImageBytes, box.Width * 10000L, box.Height * 10000L));
                                height = Math.Max(height, box.Height / 1.27d + 6);
                            }
                            else
                                height = Math.Max(height, imageSlot ? 72 : ContentHeight(text, 10, ColumnWidth(c + 1)));
                        }
                        ApplyHeight(xrow, height);
                        data.Append(xrow);
                        row++;
                    }
                    WriteBlank(data, row, perRow);
                    row++;
                }

                var worksheet = new Worksheet();
                worksheet.Append(BuildColumns(Math.Max(perRow, 1)));
                worksheet.Append(data);
                ws.Worksheet = worksheet;
                if (pictures.Count > 0)
                    AddCardPictures(ws, pictures);
                ws.Worksheet.Save();
                var sheets = wb.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet { Id = wb.GetIdOfPart(ws), SheetId = 1, Name = "Cards" });
                wb.Workbook.Save();
            }
            return ms.ToArray();
        }

        private static void AddCardPictures(WorksheetPart worksheetPart, List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)> pictures)
        {
            var drawings = worksheetPart.AddNewPart<DrawingsPart>();
            var drawing = new Xdr.WorksheetDrawing();
            uint index = 1;
            foreach (var picture in pictures)
            {
                var part = drawings.AddImagePart(PickImagePart(picture.Bytes));
                using (var stream = new MemoryStream(picture.Bytes))
                    part.FeedData(stream);
                var cx = picture.Cx > 0 ? picture.Cx : 800000;
                var cy = picture.Cy > 0 ? picture.Cy : 800000;
                var marker = new Xdr.FromMarker(
                    new Xdr.ColumnId((picture.Column - 1).ToString(CultureInfo.InvariantCulture)),
                    new Xdr.ColumnOffset("60000"),
                    new Xdr.RowId((picture.Row - 1).ToString(CultureInfo.InvariantCulture)),
                    new Xdr.RowOffset("40000"));
                var extent = new Xdr.Extent { Cx = cx, Cy = cy };
                var blip = new A.Blip { Embed = drawings.GetIdOfPart(part) };
                var pic = new Xdr.Picture(
                    new Xdr.NonVisualPictureProperties(
                        new Xdr.NonVisualDrawingProperties { Id = index + 1, Name = "Card" + index.ToString(CultureInfo.InvariantCulture) },
                        new Xdr.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true })),
                    new Xdr.BlipFill(blip, new A.Stretch(new A.FillRectangle())),
                    new Xdr.ShapeProperties(
                        new A.Transform2D(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = cx, Cy = cy }),
                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));
                drawing.Append(new Xdr.OneCellAnchor(marker, extent, pic, new Xdr.ClientData()));
                index++;
            }
            drawings.WorksheetDrawing = drawing;
            drawings.WorksheetDrawing.Save();
            worksheetPart.Worksheet.Append(new Drawing { Id = worksheetPart.GetIdOfPart(drawings) });
        }

        private static PartTypeInfo PickImagePart(byte[] bytes)
        {
            if (bytes != null && bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
                return ImagePartType.Jpeg;
            if (bytes != null && bytes.Length >= 3 && bytes[0] == (byte)'G')
                return ImagePartType.Gif;
            if (bytes != null && bytes.Length >= 2 && bytes[0] == (byte)'B')
                return ImagePartType.Bmp;
            return ImagePartType.Png;
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

        private static uint WriteSection(SheetData data, MergeCells merges, uint row, int span, FormSection section, List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)> pictures, HashSet<int> imageColumns)
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
                var headerLabels = new List<string>();
                for (var hi = 0; hi < table.Columns.Count; hi++)
                {
                    var label = table.DisplayHeaders != null && hi < table.DisplayHeaders.Count
                                && !string.IsNullOrWhiteSpace(table.DisplayHeaders[hi])
                        ? table.DisplayHeaders[hi]
                        : table.Columns[hi];
                    headerLabels.Add(label ?? "");
                }
                var header = headerLabels.Select(c => new FormCell { Text = c }).ToList();
                WriteCells(data, row, header, StyleHeader, true, pictures, imageColumns);
                row++;
                foreach (var cells in table.Rows)
                {
                    var exportCells = FormGridCellPolish.FormatExportRow(cells, table.Formats);
                    WriteCells(data, row, exportCells, StyleCell, false, pictures, imageColumns);
                    row++;
                }
            }
            foreach (var child in section.Sections)
                row = WriteSection(data, merges, row, span, child, pictures, imageColumns);
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
                double height = 0;
                for (var c = 0; c < line.Count; c++)
                {
                    xrow.Append(Inline(c + 1, row, line[c].text, line[c].style));
                    height = Math.Max(height, ContentHeight(line[c].text, FontPt(line[c].style), ColumnWidth(c + 1)));
                }
                ApplyHeight(xrow, height);
                data.Append(xrow);
                row++;
            }
            return row;
        }

        private static void WriteCells(SheetData data, uint row, List<FormCell> cells, uint headerStyle, bool header, List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)> pictures, HashSet<int> imageColumns)
        {
            var xrow = new Row { RowIndex = row };
            double height = 0;
            for (var i = 0; i < cells.Count; i++)
            {
                var cell = cells[i] ?? new FormCell();
                var style = header ? headerStyle : (cell.IsNumber ? StyleNumber : StyleCell);
                var picture = !header && cell.ImageBytes != null && cell.ImageBytes.Length > 0;
                var text = picture ? "" : cell.Text;
                xrow.Append(Inline(i + 1, row, text, style));
                if (picture)
                {
                    var box = ImagePixels.Fit(cell.ImageBytes, 110, 98);
                    pictures.Add((i + 1, row, cell.ImageBytes, box.Width * 10000L, box.Height * 10000L));
                    imageColumns.Add(i + 1);
                    height = Math.Max(height, box.Height / 1.27d + 6);
                }
                else
                    height = Math.Max(height, ContentHeight(text, FontPt(style), ColumnWidth(i + 1, imageColumns.Contains(i + 1))));
            }
            ApplyHeight(xrow, height);
            data.Append(xrow);
        }

        private static void WriteMerged(SheetData data, MergeCells merges, uint row, int span, string text, uint style)
        {
            var xrow = new Row { RowIndex = row };
            xrow.Append(Inline(1, row, text, style));
            var width = 0d;
            for (var i = 1; i <= span; i++) width += ColumnWidth(i);
            ApplyHeight(xrow, ContentHeight(text, FontPt(style), width));
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

        private static double FontPt(uint style)
        {
            if (style == StyleTitle || style == StyleSection) return 13;
            if (style == StyleNested) return 12;
            return 10;
        }

        private static double ColumnWidth(int col, bool image)
        {
            if (image) return 18;
            return col % 2 == 1 ? 22 : 28;
        }

        private static double ColumnWidth(int col)
        {
            return ColumnWidth(col, false);
        }

        /// <summary>Points. Counts wrapped lines from column width so the row is tall enough for the font.</summary>
        private static double ContentHeight(string text, double fontPt, double widthChars)
        {
            var charsPerLine = Math.Max(1d, widthChars * (10d / Math.Max(fontPt, 1)));
            var lines = 0;
            foreach (var part in (text ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                if (part.Length == 0)
                {
                    lines++;
                    continue;
                }
                var units = 0;
                foreach (var ch in part)
                    units += ch > 255 ? 2 : 1;
                lines += (int)Math.Ceiling(units / charsPerLine);
            }
            if (lines < 1) lines = 1;
            return lines * (fontPt * 1.45) + 4;
        }

        private static void ApplyHeight(Row row, double points)
        {
            row.Height = points;
            row.CustomHeight = true;
        }

        private static Columns BuildColumns(int span)
        {
            return BuildColumns(span, null);
        }

        private static Columns BuildColumns(int span, HashSet<int> imageColumns)
        {
            var columns = new Columns();
            for (var i = 1; i <= span; i++)
            {
                var image = imageColumns != null && imageColumns.Contains(i);
                columns.Append(new Column
                {
                    Min = (uint)i,
                    Max = (uint)i,
                    Width = image ? 18 : (i % 2 == 1 ? 22 : 28),
                    CustomWidth = true
                });
            }
            return columns;
        }

        private static Stylesheet BuildStyles()
        {
            var fonts = new Fonts(
                Font(10, false, "1a1a1a"),
                Font(13, true, "1a1a1a"),
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
