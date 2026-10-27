using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Word file written with Open XML when LibreOffice is not installed.
    /// It keeps the card order (picture, then fields) and the picture's aspect ratio.
    /// </summary>
    public static class OpenXmlWordForm
    {
        public static byte[] Write(FormDocument doc)
        {
            doc ??= new FormDocument();
            // Wide Result grids match PDF better on landscape (same idea as DashboardPrintCss).
            var landscape = doc.ColumnSpan() >= 8;
            using var ms = new MemoryStream();
            using (var word = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                var main = word.AddMainDocumentPart();
                var body = new Body();
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
                var images = new ImageWriter(main);
                if (!string.IsNullOrEmpty(doc.Title))
                    body.Append(DocTitleParagraph(doc.Title));
                AppendFields(body, images, doc.Fields);
                foreach (var section in doc.Sections)
                    AppendSection(body, images, section);
                if (!string.IsNullOrEmpty(doc.Notes))
                    body.Append(Paragraph(doc.Notes, false, "18"));
                body.Append(PageSetup(landscape));
                main.Document.Save();
            }
            return ms.ToArray();
        }

        public static byte[] WriteCards(CardSheet sheet)
        {
            sheet ??= new CardSheet();
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
            var pageRows = CardSheet.ClampPageRows(sheet.CardRows);
            var pageSize = Math.Max(1, pageRows * perRow);
            var pages = Math.Max(1, (int)Math.Ceiling(sheet.Cards.Count / (double)pageSize));
            if (sheet.Cards.Count == 0) pages = 1;
            using var ms = new MemoryStream();
            using (var word = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                var main = word.AddMainDocumentPart();
                var body = new Body();
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
                var images = new ImageWriter(main);
                var rowMm = CardPageFit.RowMm(pageRows);
                var cardMm = CardPageFit.CardMm(perRow);
                for (var p = 0; p < pages; p++)
                {
                    if (p > 0)
                    {
                        body.Append(new Paragraph(
                            new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "0", Line = "20", LineRule = LineSpacingRuleValues.Exact }),
                            new Run(new Break { Type = BreakValues.Page })));
                    }
                    var title = string.IsNullOrEmpty(sheet.Title) ? "Cards" : sheet.Title;
                    if (pages > 1)
                        title = title + " (" + (p + 1).ToString(CultureInfo.InvariantCulture) + "/" + pages.ToString(CultureInfo.InvariantCulture) + ")";
                    var table = new Table(TableProps(perRow));
                    var titleRow = new TableRow(new TableRowProperties(
                        new TableRowHeight { Val = (uint)Math.Max(200, Math.Round(CardPageFit.TitleMm * 56.7)), HeightType = HeightRuleValues.Exact },
                        new CantSplit()));
                    titleRow.Append(new TableCell(
                        new TableCellProperties(
                            new GridSpan { Val = perRow },
                            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }),
                        Paragraph(title, true, "28", "0")));
                    table.Append(titleRow);
                    for (var r = 0; r < pageRows; r++)
                    {
                        var rowTwips = (uint)Math.Max(200, Math.Round(rowMm * 56.7));
                        var row = new TableRow(new TableRowProperties(
                            new TableRowHeight { Val = rowTwips, HeightType = HeightRuleValues.Exact },
                            new CantSplit()));
                        for (var c = 0; c < perRow; c++)
                        {
                            var index = p * pageSize + r * perRow + c;
                            var cell = new TableCell(CardCellProps(perRow));
                            if (index < sheet.Cards.Count)
                                FillCard(cell, images, sheet.Cards[index], cardMm, rowMm);
                            else
                                cell.Append(Paragraph("", false, "16"));
                            row.Append(cell);
                        }
                        table.Append(row);
                    }
                    body.Append(table);
                }
                body.Append(PageSetup());
                main.Document.Save();
            }
            return ms.ToArray();
        }

        private static void FillCard(TableCell cell, ImageWriter images, List<CardField> fields, double cardMm, double rowMm)
        {
            byte[] image = null;
            string imageLabel = "";
            var lines = new List<CardField>();
            foreach (var field in fields)
            {
                if (image == null && field.ImageBytes != null && field.ImageBytes.Length > 0)
                {
                    image = field.ImageBytes;
                    imageLabel = field.Label ?? "";
                    continue;
                }
                if (field.IsImage && image == null)
                    continue;
                lines.Add(field);
            }
            if (image != null)
            {
                var slot = CardPageFit.Measure(rowMm, cardMm, Math.Max(1, lines.Count), true);
                if (!string.IsNullOrEmpty(imageLabel))
                    cell.Append(Paragraph(imageLabel, false, (slot.FontPx * 2).ToString(CultureInfo.InvariantCulture), Twips(slot.GapMm).ToString(CultureInfo.InvariantCulture)));
                var maxW = Math.Max(40, (int)((cardMm - slot.PadMm * 2) * 96 / 25.4));
                var maxH = Math.Max(24, (int)(slot.ImageMm * 96 / 25.4));
                var fit = ImagePixels.Fit(image, maxW, maxH);
                cell.Append(new Paragraph(new Run(images.Drawing(image, fit.Width * 9525L, fit.Height * 9525L))));
                WriteFields(cell, lines, slot);
                return;
            }
            var textSlot = CardPageFit.Measure(rowMm, cardMm, Math.Max(1, lines.Count), false);
            WriteFields(cell, lines, textSlot);
        }

        private static void WriteFields(TableCell cell, List<CardField> lines, CardPageFit.Slot slot)
        {
            var halfPoints = (slot.FontPx * 2).ToString(CultureInfo.InvariantCulture);
            var after = Twips(slot.GapMm).ToString(CultureInfo.InvariantCulture);
            if (lines.Count == 0)
            {
                cell.Append(Paragraph("", false, halfPoints));
                return;
            }
            foreach (var field in lines)
            {
                var value = string.IsNullOrWhiteSpace(field.Value) ? "\u2014" : field.Value;
                cell.Append(Paragraph((field.Label ?? "") + "    " + value, false, halfPoints, after));
            }
        }

        private static int Twips(double mm) => Math.Max(20, (int)Math.Round(mm * 56.7));

        private static void AppendSection(Body body, ImageWriter images, FormSection section)
        {
            if (!string.IsNullOrEmpty(section.Title))
                body.Append(Paragraph(section.Title, true, "22", "80", "3D4F5F"));
            AppendFields(body, images, section.Fields);
            foreach (var table in section.Tables)
            {
                if (!string.IsNullOrEmpty(table.Title))
                    body.Append(Paragraph(table.Title, true, "20", "60", "3D4F5F"));
                body.Append(DataTable(images, table));
            }
            foreach (var child in section.Sections)
                AppendSection(body, images, child);
        }

        private static void AppendFields(Body body, ImageWriter images, List<FormField> fields)
        {
            if (fields == null || fields.Count == 0) return;
            var table = new Table(GridTableProps());
            foreach (var field in fields)
            {
                var row = new TableRow();
                row.Append(GridHeaderCell(field.Label ?? "", 2500));
                row.Append(GridBodyCell(images, field.Value, null, null, 2500, rightAlign: false));
                table.Append(row);
            }
            body.Append(table);
        }

        /// <summary>PDF-like Result grid: light header, full cell borders, weighted column widths.</summary>
        private static Table DataTable(ImageWriter images, FormTable source)
        {
            var columns = Math.Max(1, source.Columns?.Count ?? 1);
            var widths = ColumnPctWidths(source, columns);
            var table = new Table(GridTableProps());
            table.Append(ColumnGrid(widths));

            var header = new TableRow();
            for (var hi = 0; hi < columns; hi++)
            {
                string label = "";
                if (source.Columns != null && hi < source.Columns.Count)
                {
                    var field = source.Columns[hi];
                    label = source.DisplayHeaders != null && hi < source.DisplayHeaders.Count
                            && !string.IsNullOrWhiteSpace(source.DisplayHeaders[hi])
                        ? source.DisplayHeaders[hi]
                        : field;
                }
                header.Append(GridHeaderCell(label ?? "", widths[hi]));
            }
            table.Append(header);

            foreach (var line in source.Rows ?? new List<List<FormCell>>())
            {
                var exportLine = FormGridCellPolish.FormatExportRow(line, source.Formats);
                var row = new TableRow();
                for (var i = 0; i < columns; i++)
                {
                    var cell = i < exportLine.Count ? exportLine[i] : null;
                    var fmt = source.Formats != null && i < source.Formats.Count ? source.Formats[i] : null;
                    var right = cell != null && (cell.IsNumber
                        || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(fmt), "delta", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(fmt), "percent", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(fmt), "number", StringComparison.OrdinalIgnoreCase));
                    var plain = cell == null ? "" : FormGridCellPolish.FormatPlainText(cell.Text, fmt);
                    if (cell != null && (cell.ImageBytes != null && cell.ImageBytes.Length > 0 || cell.IsImage))
                        plain = cell.Text;
                    row.Append(GridBodyCell(images, plain, cell?.ImageBytes, fmt, widths[i], right));
                }
                table.Append(row);
            }
            return table;
        }

        private static TableGrid ColumnGrid(int[] widths)
        {
            var grid = new TableGrid();
            // Pct widths are relative; grid cols use twips approx for layout engines that read them.
            foreach (var w in widths)
                grid.Append(new GridColumn { Width = Math.Max(200, w * 2).ToString(CultureInfo.InvariantCulture) });
            return grid;
        }

        /// <summary>Weight image / name cols; keep empty timeline cols from dominating the page.</summary>
        private static int[] ColumnPctWidths(FormTable source, int columns)
        {
            var weights = new double[columns];
            for (var i = 0; i < columns; i++)
            {
                var name = source.Columns != null && i < source.Columns.Count ? source.Columns[i] : "";
                var fmt = source.Formats != null && i < source.Formats.Count ? source.Formats[i] : null;
                if (CardSheet.IsImageFieldName(name)
                    || string.Equals(AiPolishedRenderSpec.NormalizeColumnFormat(fmt), "image", StringComparison.OrdinalIgnoreCase))
                    weights[i] = 1.6;
                else if (LooksLikeIdColumn(name))
                    weights[i] = 0.7;
                else if (LooksLikeNameColumn(name))
                    weights[i] = 1.5;
                else if (LooksLikeDateOrMilestoneColumn(name))
                    weights[i] = 0.85;
                else
                    weights[i] = 1.0;
            }
            var sum = weights.Sum();
            if (sum <= 0) sum = columns;
            var pcts = new int[columns];
            var assigned = 0;
            for (var i = 0; i < columns; i++)
            {
                if (i == columns - 1)
                    pcts[i] = Math.Max(200, 5000 - assigned);
                else
                {
                    pcts[i] = Math.Max(200, (int)Math.Round(5000 * weights[i] / sum));
                    assigned += pcts[i];
                }
            }
            return pcts;
        }

        private static bool LooksLikeIdColumn(string name)
            => !string.IsNullOrWhiteSpace(name)
               && (name.Equals("Ref ID", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("RefId", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith(" Id", StringComparison.OrdinalIgnoreCase)
                   || name.EndsWith("_Id", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("Style No", StringComparison.OrdinalIgnoreCase)
                   || name.Equals("StyleNo", StringComparison.OrdinalIgnoreCase));

        private static bool LooksLikeNameColumn(string name)
            => !string.IsNullOrWhiteSpace(name)
               && (name.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0);

        private static bool LooksLikeDateOrMilestoneColumn(string name)
            => !string.IsNullOrWhiteSpace(name)
               && (name.IndexOf("date", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("actual", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("delta", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("proto", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("comp", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.StartsWith("PP", StringComparison.OrdinalIgnoreCase));

        private static TableCellProperties CardCellProps(int perRow)
        {
            return new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = (5000 / perRow).ToString(CultureInfo.InvariantCulture) },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
                new TableCellMargin(
                    new TopMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                    new LeftMargin { Width = "140", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                    new RightMargin { Width = "140", Type = TableWidthUnitValues.Dxa }),
                new TableCellBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "E6E8EB" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "E6E8EB" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "E6E8EB" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "E6E8EB" }));
        }

        private static TableCell GridHeaderCell(string text, int pctWidth)
        {
            var props = new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = pctWidth.ToString(CultureInfo.InvariantCulture) },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "F1F5F9", Color = "auto" },
                CellMargins(),
                CellBorderBox("CBD5E1"));
            return new TableCell(props, Paragraph(text ?? "", true, "17", "0", "1E293B"));
        }

        private static TableCell GridBodyCell(
            ImageWriter images, string text, byte[] image, string format, int pctWidth, bool rightAlign)
        {
            var props = new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = pctWidth.ToString(CultureInfo.InvariantCulture) },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                CellMargins(),
                CellBorderBox("E2E8F0"));
            var cell = new TableCell(props);
            if (image != null && image.Length > 0)
            {
                var fit = ImagePixels.Fit(image, 100, 72);
                cell.Append(new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
                    new Run(images.Drawing(image, fit.Width * 9525L, fit.Height * 9525L))));
                return cell;
            }

            var fmt = AiPolishedRenderSpec.NormalizeColumnFormat(format);
            string color = "1A1A1A";
            var display = text ?? "";
            if (string.Equals(fmt, "delta", StringComparison.OrdinalIgnoreCase)
                && FormGridCellPolish.TryParseSignedNumber(text, out var n))
            {
                display = FormGridCellPolish.FormatPlainText(text, "delta");
                color = n < 0 ? "DC2626" : n > 0 ? "16A34A" : "6B7280";
            }
            else if (string.Equals(fmt, "percent", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(fmt, "date", StringComparison.OrdinalIgnoreCase))
            {
                display = FormGridCellPolish.FormatPlainText(text, fmt);
            }

            cell.Append(Paragraph(display, false, "17", "0", color, rightAlign));
            return cell;
        }

        private static TableCellMargin CellMargins()
            => new TableCellMargin(
                new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new LeftMargin { Width = "60", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "60", Type = TableWidthUnitValues.Dxa });

        private static TableCellBorders CellBorderBox(string hex)
            => new TableCellBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = hex },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = hex },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = hex },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = hex });

        private static TableProperties GridTableProps()
        {
            return new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableLayout { Type = TableLayoutValues.Fixed },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "CBD5E1" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "E2E8F0" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "E2E8F0" }));
        }

        private static TableProperties TableProps(int columns)
            => GridTableProps();

        /// <summary>Document title — match PDF TitleCss (~14pt slate).</summary>
        private static Paragraph DocTitleParagraph(string text)
        {
            var props = new RunProperties(
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                new FontSize { Val = "28" },
                new FontSizeComplexScript { Val = "28" },
                new Color { Val = "3D4F5F" },
                new Bold());
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { Before = "0", After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto }),
                new Run(props, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static Paragraph Paragraph(string text, bool bold, string halfPoints)
            => Paragraph(text, bold, halfPoints, "0", "1A1A1A");

        private static Paragraph Paragraph(string text, bool bold, string halfPoints, string afterTwips)
            => Paragraph(text, bold, halfPoints, afterTwips, "1A1A1A");

        private static Paragraph Paragraph(
            string text, bool bold, string halfPoints, string afterTwips, string colorHex, bool rightAlign = false)
        {
            var props = new RunProperties(
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                new FontSize { Val = halfPoints },
                new FontSizeComplexScript { Val = halfPoints },
                new Color { Val = string.IsNullOrWhiteSpace(colorHex) ? "1A1A1A" : colorHex.TrimStart('#') });
            if (bold) props.Append(new Bold());
            var pPr = new ParagraphProperties(
                new SpacingBetweenLines { Before = "20", After = afterTwips, Line = "240", LineRule = LineSpacingRuleValues.Auto });
            if (rightAlign)
                pPr.Append(new Justification { Val = JustificationValues.Right });
            return new Paragraph(
                pPr,
                new Run(props, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static SectionProperties PageSetup(bool landscape = false)
        {
            // A4: 11906×16838 twips. Landscape matches wide PDF Result grids.
            var size = landscape
                ? new PageSize { Width = 16838, Height = 11906, Orient = PageOrientationValues.Landscape }
                : new PageSize { Width = 11906, Height = 16838 };
            var margin = landscape
                ? new PageMargin { Top = 567, Bottom = 567, Left = 567, Right = 567, Header = 360, Footer = 360, Gutter = 0 }
                : new PageMargin { Top = 720, Bottom = 720, Left = 720, Right = 720, Header = 720, Footer = 720, Gutter = 0 };
            return new SectionProperties(size, margin);
        }

        private sealed class ImageWriter
        {
            private readonly MainDocumentPart _main;
            private uint _id = 1;

            public ImageWriter(MainDocumentPart main) => _main = main;

            public Drawing Drawing(byte[] bytes, long cx, long cy)
            {
                var part = _main.AddImagePart(ImageType(bytes));
                using (var stream = new MemoryStream(bytes))
                    part.FeedData(stream);
                var rel = _main.GetIdOfPart(part);
                var id = _id++;
                if (cx < 1) cx = 1;
                if (cy < 1) cy = 1;
                return new Drawing(
                    new DW.Inline(
                        new DW.Extent { Cx = cx, Cy = cy },
                        new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                        new DW.DocProperties { Id = id, Name = "Picture" + id.ToString(CultureInfo.InvariantCulture) },
                        new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties { Id = id, Name = "Picture" + id.ToString(CultureInfo.InvariantCulture) },
                                        new PIC.NonVisualPictureDrawingProperties()),
                                    new PIC.BlipFill(new A.Blip { Embed = rel }, new A.Stretch(new A.FillRectangle())),
                                    new PIC.ShapeProperties(
                                        new A.Transform2D(new A.Offset { X = 0, Y = 0 }, new A.Extents { Cx = cx, Cy = cy }),
                                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                    { DistanceFromTop = 0, DistanceFromBottom = 0, DistanceFromLeft = 0, DistanceFromRight = 0 });
            }

            private static PartTypeInfo ImageType(byte[] bytes)
            {
                if (bytes != null && bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return ImagePartType.Jpeg;
                if (bytes != null && bytes.Length >= 3 && bytes[0] == (byte)'G') return ImagePartType.Gif;
                if (bytes != null && bytes.Length >= 2 && bytes[0] == (byte)'B') return ImagePartType.Bmp;
                return ImagePartType.Png;
            }
        }
    }
}
