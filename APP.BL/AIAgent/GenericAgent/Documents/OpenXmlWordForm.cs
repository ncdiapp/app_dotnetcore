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
            using var ms = new MemoryStream();
            using (var word = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
            {
                var main = word.AddMainDocumentPart();
                var body = new Body();
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
                var images = new ImageWriter(main);
                if (!string.IsNullOrEmpty(doc.Title))
                    body.Append(Paragraph(doc.Title, true, "32"));
                AppendFields(body, images, doc.Fields);
                foreach (var section in doc.Sections)
                    AppendSection(body, images, section);
                if (!string.IsNullOrEmpty(doc.Notes))
                    body.Append(Paragraph(doc.Notes, false, "20"));
                body.Append(PageSetup());
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
                body.Append(Paragraph(section.Title, true, "26"));
            AppendFields(body, images, section.Fields);
            foreach (var table in section.Tables)
            {
                if (!string.IsNullOrEmpty(table.Title))
                    body.Append(Paragraph(table.Title, true, "22"));
                body.Append(DataTable(images, table));
            }
            foreach (var child in section.Sections)
                AppendSection(body, images, child);
        }

        private static void AppendFields(Body body, ImageWriter images, List<FormField> fields)
        {
            if (fields == null || fields.Count == 0) return;
            var table = new Table(TableProps(2));
            foreach (var field in fields)
            {
                var row = new TableRow();
                row.Append(TextCell(field.Label ?? "", true));
                row.Append(ValueCell(images, field.Value, null));
                table.Append(row);
            }
            body.Append(table);
        }

        private static Table DataTable(ImageWriter images, FormTable source)
        {
            var columns = Math.Max(1, source.Columns?.Count ?? 1);
            var table = new Table(TableProps(columns));
            var header = new TableRow();
            foreach (var column in source.Columns ?? new List<string>())
                header.Append(TextCell(column ?? "", true));
            table.Append(header);
            foreach (var line in source.Rows ?? new List<List<FormCell>>())
            {
                var row = new TableRow();
                for (var i = 0; i < columns; i++)
                {
                    var cell = i < line.Count ? line[i] : null;
                    row.Append(ValueCell(images, cell?.Text, cell?.ImageBytes));
                }
                table.Append(row);
            }
            return table;
        }

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

        private static TableCell TextCell(string text, bool bold)
        {
            return new TableCell(
                new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto, Width = "0" }),
                Paragraph(text, bold, "18"));
        }

        private static TableCell ValueCell(ImageWriter images, string text, byte[] image)
        {
            var cell = new TableCell(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto, Width = "0" }));
            if (image != null && image.Length > 0)
            {
                var fit = ImagePixels.Fit(image, 120, 90);
                cell.Append(new Paragraph(new Run(images.Drawing(image, fit.Width * 9525L, fit.Height * 9525L))));
            }
            else
                cell.Append(Paragraph(text ?? "", false, "18"));
            return cell;
        }

        private static TableProperties TableProps(int columns)
        {
            return new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "DDDDDD" }));
        }

        private static Paragraph Paragraph(string text, bool bold, string halfPoints)
            => Paragraph(text, bold, halfPoints, "0");

        private static Paragraph Paragraph(string text, bool bold, string halfPoints, string afterTwips)
        {
            var props = new RunProperties(
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                new FontSize { Val = halfPoints });
            if (bold) props.Append(new Bold());
            return new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = afterTwips, Line = "240", LineRule = LineSpacingRuleValues.Auto }),
                new Run(props, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static SectionProperties PageSetup()
        {
            return new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin { Top = 850, Bottom = 850, Left = 850, Right = 850, Header = 720, Footer = 720, Gutter = 0 });
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
