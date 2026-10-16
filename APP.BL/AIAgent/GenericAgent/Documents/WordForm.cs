using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GemBox.Document;
using GemBox.Document.Tables;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Word and PDF from the style table. PDF is this same model saved as PDF.
    /// </summary>
    public static class WordForm
    {
        public const string StyleTitle = "FormTitle";
        public const string StyleSection = "FormSection";
        public const string StyleNested = "FormSection3";
        public const string StyleLabel = "FormLabel";
        public const string StyleValue = "FormValue";
        public const string StyleHeader = "FormTableHeader";
        public const string StyleCell = "FormTableCell";
        public const string StyleNumber = "FormTableNumber";
        public const string StyleNotes = "FormNotes";

        public static byte[] WriteDocx(FormDocument doc) => Save(doc, SaveOptions.DocxDefault);

        public static byte[] WritePdf(FormDocument doc) => Save(doc, SaveOptions.PdfDefault);

        public static FormDocument ReadDocx(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes ?? Array.Empty<byte>());
            var model = DocumentModel.Load(ms, LoadOptions.DocxDefault);
            var doc = new FormDocument();
            FormSection current = null;
            FormSection parent = null;
            foreach (var section in model.Sections)
            {
                foreach (var block in section.Blocks)
                {
                    if (block is Paragraph paragraph)
                    {
                        var name = StyleName(paragraph);
                        var text = paragraph.Content.ToString().Trim();
                        if (name == StyleTitle) doc.Title = text;
                        else if (name == StyleNotes) doc.Notes = text;
                        continue;
                    }
                    if (block is not Table table || table.Rows.Count == 0) continue;
                    var first = FirstParagraph(table.Rows[0].Cells[0]);
                    var style = first == null ? "" : StyleName(first);
                    if (style == StyleSection || style == StyleNested)
                    {
                        var level = style == StyleNested ? 3 : 2;
                        var formSection = new FormSection
                        {
                            Title = first.Content.ToString().Trim(),
                            Level = level
                        };
                        if (level >= 3 && parent != null)
                            parent.Sections.Add(formSection);
                        else
                        {
                            doc.Sections.Add(formSection);
                            parent = formSection;
                        }
                        current = formSection;
                        continue;
                    }
                    if (style == StyleLabel || style == StyleValue)
                    {
                        var fields = current == null ? doc.Fields : current.Fields;
                        foreach (var row in table.Rows)
                        {
                            for (var i = 0; i + 1 < row.Cells.Count; i += 2)
                            {
                                var label = CellText(row.Cells[i]);
                                var value = CellText(row.Cells[i + 1]);
                                if (label.Length == 0 && value.Length == 0) continue;
                                fields.Add(new FormField { Label = label, Value = value });
                            }
                        }
                        continue;
                    }
                    if (style == StyleHeader || style == StyleCell || style == StyleNumber)
                    {
                        var owner = current ?? new FormSection { Level = 2 };
                        if (current == null)
                        {
                            current = owner;
                            doc.Sections.Add(owner);
                        }
                        owner.Tables.Add(ReadGrid(table));
                    }
                }
            }
            return doc;
        }

        public static (int PageCount, string Text) ReadPdf(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes ?? Array.Empty<byte>());
            var model = DocumentModel.Load(ms, LoadOptions.PdfDefault);
            var text = model.Content.ToString() ?? "";
            if (text.Length > DocumentStyles.PdfTextCap)
                text = text.Substring(0, DocumentStyles.PdfTextCap);
            var pages = 1;
            try
            {
                pages = model.GetPaginator().Pages.Count;
            }
            catch
            {
                pages = 1;
            }
            if (pages < 1) pages = 1;
            return (pages, text);
        }

        private static byte[] Save(FormDocument doc, SaveOptions options)
        {
            var model = Build(doc);
            using var ms = new MemoryStream();
            model.Save(ms, options);
            return ms.ToArray();
        }

        private static DocumentModel Build(FormDocument doc)
        {
            var model = new DocumentModel();
            AddStyles(model);
            var section = new Section(model);
            model.Sections.Add(section);
            if (!string.IsNullOrEmpty(doc.Title))
            {
                section.Blocks.Add(Paragraph(model, doc.Title, StyleTitle, HorizontalAlignment.Center));
                section.Blocks.Add(new Paragraph(model));
            }
            if (doc.Fields.Count > 0)
                section.Blocks.Add(FieldTable(model, doc.Fields));
            foreach (var child in doc.Sections)
                WriteSection(model, section, child);
            if (!string.IsNullOrEmpty(doc.Notes))
            {
                section.Blocks.Add(new Paragraph(model));
                section.Blocks.Add(Paragraph(model, doc.Notes, StyleNotes, HorizontalAlignment.Left));
            }
            return model;
        }

        private static void WriteSection(DocumentModel model, Section section, FormSection form)
        {
            if (!string.IsNullOrEmpty(form.Title))
            {
                section.Blocks.Add(new Paragraph(model));
                var style = form.Level >= 3 ? StyleNested : StyleSection;
                section.Blocks.Add(Band(model, form.Title, style));
            }
            if (form.Fields.Count > 0)
                section.Blocks.Add(FieldTable(model, form.Fields));
            foreach (var table in form.Tables)
            {
                if (!string.IsNullOrEmpty(table.Title))
                    section.Blocks.Add(Band(model, table.Title, StyleNested));
                section.Blocks.Add(Grid(model, table));
            }
            foreach (var child in form.Sections)
                WriteSection(model, section, child);
        }

        private static void AddStyles(DocumentModel model)
        {
            Style(model, StyleTitle, 18, true, 0x1a1a1a, HorizontalAlignment.Center);
            Style(model, StyleSection, 13, true, 0x1a1a1a, HorizontalAlignment.Left);
            Style(model, StyleNested, 12, true, 0x1a1a1a, HorizontalAlignment.Left);
            Style(model, StyleLabel, 10, true, 0x555555, HorizontalAlignment.Left);
            Style(model, StyleValue, 10, false, 0x1a1a1a, HorizontalAlignment.Left);
            Style(model, StyleHeader, 10, true, 0xffffff, HorizontalAlignment.Left);
            Style(model, StyleCell, 10, false, 0x1a1a1a, HorizontalAlignment.Left);
            Style(model, StyleNumber, 10, false, 0x1a1a1a, HorizontalAlignment.Right);
            Style(model, StyleNotes, 10, false, 0x333333, HorizontalAlignment.Left);
        }

        private static void Style(DocumentModel model, string name, double size, bool bold, int rgb, HorizontalAlignment align)
        {
            var style = new ParagraphStyle(name);
            style.CharacterFormat.FontName = DocumentStyles.FontFamily;
            style.CharacterFormat.Size = size;
            style.CharacterFormat.Bold = bold;
            style.CharacterFormat.FontColor = Rgb(rgb);
            style.ParagraphFormat.Alignment = align;
            style.ParagraphFormat.SpaceBefore = 0;
            style.ParagraphFormat.SpaceAfter = 0;
            model.Styles.Add(style);
        }

        private static Paragraph Paragraph(DocumentModel model, string text, string style, HorizontalAlignment align)
        {
            var run = new Run(model, text ?? "");
            var paragraph = new Paragraph(model, run);
            paragraph.ParagraphFormat.Style = (ParagraphStyle)model.Styles[style];
            paragraph.ParagraphFormat.Alignment = align;
            return paragraph;
        }

        private static Table Band(DocumentModel model, string text, string style)
        {
            var table = new Table(model);
            table.TableFormat.PreferredWidth = new TableWidth(100, TableWidthUnit.Percentage);
            var cell = new TableCell(model, Paragraph(model, text, style, HorizontalAlignment.Left));
            if (style == StyleSection)
                cell.CellFormat.BackgroundColor = Rgb(0xf0f2f4);
            table.Rows.Add(new TableRow(model, cell));
            return table;
        }

        private static Table FieldTable(DocumentModel model, List<FormField> fields)
        {
            var table = new Table(model);
            table.TableFormat.PreferredWidth = new TableWidth(100, TableWidthUnit.Percentage);
            for (var i = 0; i < fields.Count; i += 2)
            {
                var row = new TableRow(model);
                row.Cells.Add(TextCell(model, fields[i].Label, StyleLabel, false));
                row.Cells.Add(TextCell(model, fields[i].Value, fields[i].IsNumber ? StyleNumber : StyleValue, false));
                if (i + 1 < fields.Count)
                {
                    row.Cells.Add(TextCell(model, fields[i + 1].Label, StyleLabel, false));
                    row.Cells.Add(TextCell(model, fields[i + 1].Value, fields[i + 1].IsNumber ? StyleNumber : StyleValue, false));
                }
                else
                {
                    row.Cells.Add(TextCell(model, "", StyleLabel, false));
                    row.Cells.Add(TextCell(model, "", StyleValue, false));
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private static Table Grid(DocumentModel model, FormTable form)
        {
            var table = new Table(model);
            table.TableFormat.PreferredWidth = new TableWidth(100, TableWidthUnit.Percentage);
            var header = new TableRow(model);
            foreach (var col in form.Columns)
            {
                var cell = TextCell(model, col, StyleHeader, true);
                cell.CellFormat.BackgroundColor = Rgb(0x3d4f5f);
                header.Cells.Add(cell);
            }
            table.Rows.Add(header);
            foreach (var row in form.Rows)
            {
                var line = new TableRow(model);
                for (var i = 0; i < form.Columns.Count; i++)
                {
                    var cell = i < row.Count ? row[i] : new FormCell();
                    line.Cells.Add(TextCell(model, cell.Text, cell.IsNumber ? StyleNumber : StyleCell, true));
                }
                table.Rows.Add(line);
            }
            return table;
        }

        private static TableCell TextCell(DocumentModel model, string text, string style, bool border)
        {
            var cell = new TableCell(model, Paragraph(model, text, style, AlignmentOf(model, style)));
            if (border)
                cell.CellFormat.Borders.SetBorders(MultipleBorderTypes.Outside, BorderStyle.Single, Rgb(0xdddddd), 0.5);
            return cell;
        }

        private static HorizontalAlignment AlignmentOf(DocumentModel model, string style)
        {
            var paragraph = model.Styles[style] as ParagraphStyle;
            return paragraph == null ? HorizontalAlignment.Left : paragraph.ParagraphFormat.Alignment;
        }

        private static FormTable ReadGrid(Table table)
        {
            var grid = new FormTable();
            var first = true;
            foreach (var row in table.Rows)
            {
                if (first)
                {
                    foreach (var cell in row.Cells)
                        grid.Columns.Add(CellText(cell));
                    first = false;
                    continue;
                }
                var cells = new List<FormCell>();
                foreach (var cell in row.Cells)
                {
                    var paragraph = FirstParagraph(cell);
                    var style = paragraph == null ? "" : StyleName(paragraph);
                    cells.Add(new FormCell
                    {
                        Text = CellText(cell),
                        IsNumber = style == StyleNumber
                    });
                }
                grid.Rows.Add(cells);
            }
            return grid;
        }

        private static string CellText(TableCell cell)
        {
            return string.Join(" ", cell.Blocks.OfType<Paragraph>().Select(p => p.Content.ToString().Trim())).Trim();
        }

        private static Paragraph FirstParagraph(TableCell cell)
        {
            return cell.Blocks.OfType<Paragraph>().FirstOrDefault();
        }

        private static string StyleName(Paragraph paragraph)
        {
            return paragraph.ParagraphFormat.Style?.Name ?? "";
        }

        private static Color Rgb(int hex)
        {
            return new Color((byte)((hex >> 16) & 255), (byte)((hex >> 8) & 255), (byte)(hex & 255));
        }
    }
}
