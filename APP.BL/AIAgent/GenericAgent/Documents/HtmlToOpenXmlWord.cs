using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Editable Word from merged TemplateContent HTML (data-role structure). Not page screenshots.
    /// </summary>
    public static class HtmlToOpenXmlWord
    {
        public static byte[] WriteCardsHtml(string html, CardSheet sheetFallback = null)
        {
            try
            {
                var doc = new HtmlParser().ParseDocument(html ?? "");
                var cards = doc.QuerySelectorAll("[data-role='card'], article.agent-card").ToList();
                if (cards.Count == 0)
                {
                    if (sheetFallback != null)
                        return OpenXmlWordForm.WriteCards(sheetFallback);
                    throw new InvalidOperationException("No data-role=card nodes in merged HTML.");
                }

                var perRow = ParseCardsPerRow(html, sheetFallback);
                var title = doc.QuerySelector("[data-role='title'], .agent-cards-title")?.TextContent?.Trim()
                            ?? sheetFallback?.Title
                            ?? "Cards";

                using var ms = new MemoryStream();
                using (var word = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
                {
                    var main = word.AddMainDocumentPart();
                    var body = new Body();
                    main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
                    var images = new ImageWriter(main);
                    body.Append(P(title, bold: true, halfPoints: "28"));

                    var table = new Table(TableProps());
                    for (var i = 0; i < cards.Count; i += perRow)
                    {
                        var row = new TableRow();
                        for (var c = 0; c < perRow; c++)
                        {
                            var idx = i + c;
                            var cell = new TableCell(CardCellProps(perRow));
                            if (idx < cards.Count)
                                FillCardFromElement(cell, images, cards[idx]);
                            else
                                cell.Append(P("", false, "18"));
                            row.Append(cell);
                        }
                        table.Append(row);
                    }
                    body.Append(table);
                    body.Append(PageSetup());
                    main.Document.Save();
                }
                return ms.ToArray();
            }
            catch
            {
                if (sheetFallback != null)
                    return OpenXmlWordForm.WriteCards(sheetFallback);
                throw;
            }
        }

        private static void FillCardFromElement(TableCell cell, ImageWriter images, IElement card)
        {
            var cardTitle = card.QuerySelector("[data-role='title'], .agent-card-title")?.TextContent?.Trim();
            if (!string.IsNullOrEmpty(cardTitle))
                cell.Append(P(cardTitle, bold: true, halfPoints: "20"));

            var rows = card.QuerySelectorAll(".agent-card-row, [data-role='label']").ToList();
            // Prefer structured rows
            var fieldRows = card.QuerySelectorAll(".agent-card-row").ToList();
            if (fieldRows.Count > 0)
            {
                foreach (var fr in fieldRows)
                {
                    var label = fr.QuerySelector("[data-role='label'], .agent-card-label")?.TextContent?.Trim() ?? "";
                    var img = fr.QuerySelector("img[data-role='image'], img.agent-card-image, img");
                    if (img != null)
                    {
                        if (!string.IsNullOrEmpty(label))
                            cell.Append(P(label, false, "16"));
                        AppendImg(cell, images, img.GetAttribute("src"));
                        continue;
                    }
                    var value = fr.QuerySelector("[data-role='value'], .agent-card-value")?.TextContent?.Trim() ?? "";
                    cell.Append(P(
                        (string.IsNullOrEmpty(label) ? "" : label + "    ") + value,
                        false, "18"));
                }
                return;
            }

            // Fallback: any img + text
            foreach (var img in card.QuerySelectorAll("img"))
                AppendImg(cell, images, img.GetAttribute("src"));
            var text = Regex.Replace(card.TextContent ?? "", @"\s+", " ").Trim();
            if (!string.IsNullOrEmpty(text) && text != cardTitle)
                cell.Append(P(text, false, "18"));
        }

        private static void AppendImg(TableCell cell, ImageWriter images, string src)
        {
            var bytes = DecodeImageSrc(src);
            if (bytes == null || bytes.Length == 0) return;
            var fit = ImagePixels.Fit(bytes, 160, 120);
            cell.Append(new Paragraph(new Run(images.Drawing(bytes, fit.Width * 9525L, fit.Height * 9525L))));
        }

        private static byte[] DecodeImageSrc(string src)
        {
            if (string.IsNullOrWhiteSpace(src)) return null;
            src = src.Trim();
            var m = Regex.Match(src, @"^data:image/[^;]+;base64,(.+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (m.Success)
            {
                try { return Convert.FromBase64String(m.Groups[1].Value.Trim()); }
                catch { return null; }
            }
            return null; // remote URLs: CardImageLoader should have inlined data-URI
        }

        private static int ParseCardsPerRow(string html, CardSheet sheet)
        {
            if (sheet != null && sheet.CardsPerRow > 0)
                return CardSheet.ClampPerRow(sheet.CardsPerRow);
            var m = Regex.Match(html ?? "", @"--cards-per-row\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
                return CardSheet.ClampPerRow(n);
            return 3;
        }

        private static TableCellProperties CardCellProps(int perRow) =>
            new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = (5000 / perRow).ToString(CultureInfo.InvariantCulture) },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
                new TableCellMargin(
                    new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                    new LeftMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                    new RightMargin { Width = "120", Type = TableWidthUnitValues.Dxa }),
                new TableCellBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "E5E7EB" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "E5E7EB" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "E5E7EB" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "E5E7EB" }));

        private static TableProperties TableProps() =>
            new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil }));

        private static Paragraph P(string text, bool bold, string halfPoints)
        {
            var props = new RunProperties(
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                new FontSize { Val = halfPoints });
            if (bold) props.Append(new Bold());
            return new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { Before = "40", After = "40", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
                new Run(props, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static SectionProperties PageSetup() =>
            new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin { Top = 850, Bottom = 850, Left = 850, Right = 850, Header = 720, Footer = 720, Gutter = 0 });

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
                        new DW.DocProperties { Id = id, Name = "Picture" + id },
                        new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                        new A.Graphic(
                            new A.GraphicData(
                                new PIC.Picture(
                                    new PIC.NonVisualPictureProperties(
                                        new PIC.NonVisualDrawingProperties { Id = id, Name = "Picture" + id },
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
                return ImagePartType.Png;
            }
        }
    }
}
