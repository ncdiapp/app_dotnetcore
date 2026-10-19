using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShapeCrawler;
using ShapeCrawler.Presentations;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// One title slide, then one slide per section. Grids split after <see cref="DocumentStyles.PptRowsPerSlide"/> rows.
    /// </summary>
    public static class PptxForm
    {
        public static byte[] Write(FormDocument doc)
            => Write(doc, null);

        public static byte[] Write(FormDocument doc, TemplatePaint paint)
        {
            var slides = Plan(doc);
            var pres = new Presentation(draft =>
            {
                foreach (var spec in slides)
                    draft.Slide(slide => Fill(slide, spec, paint));
            });

            using var ms = new MemoryStream();
            pres.Save(ms);
            pres.Dispose();
            return ms.ToArray();
        }

        public static byte[] WriteCards(CardSheet sheet)
            => WriteCards(sheet, null);

        public static byte[] WriteCards(CardSheet sheet, TemplatePaint paint)
        {
            var perRow = CardSheet.ClampPerRow(sheet.CardsPerRow);
            var pageRows = CardSheet.ClampPageRows(sheet.CardRows);
            var pageSize = pageRows * perRow;
            var pages = Math.Max(1, (int)Math.Ceiling(sheet.Cards.Count / (double)Math.Max(1, pageSize)));
            if (sheet.Cards.Count == 0) pages = 1;
            var pres = new Presentation(draft =>
            {
                for (var p = 0; p < pages; p++)
                {
                    var page = p;
                    draft.Slide(slide =>
                    {
                        var title = string.IsNullOrEmpty(sheet.Title) ? "Cards" : sheet.Title;
                        if (pages > 1) title = title + " (" + (page + 1).ToString() + "/" + pages.ToString() + ")";
                        const int slideW = 960;
                        const int slideH = 540;
                        const int margin = 16;
                        var titleH = 28;
                        var top = margin + titleH + 8;
                        var contentW = slideW - margin * 2;
                        var contentH = slideH - top - margin;
                        var cardW = Math.Max(40, contentW / perRow);
                        var cardH = Math.Max(48, contentH / pageRows);
                        var extraW = contentW - cardW * perRow;
                        var extraH = contentH - cardH * pageRows;
                        slide.Shape(box =>
                        {
                            box.X(margin).Y(margin).Width(contentW).Height(titleH);
                            box.TextBox(title);
                            box.Font(font =>
                            {
                                font.Size(paint == null ? 20 : paint.TitlePt);
                                if (paint == null || paint.TitleBold) font.Bold();
                            });
                        });
                        for (var r = 0; r < pageRows; r++)
                        {
                            for (var c = 0; c < perRow; c++)
                            {
                                var index = page * pageSize + r * perRow + c;
                                if (index >= sheet.Cards.Count) continue;
                                var slotW = cardW + (c == perRow - 1 ? extraW : 0);
                                var slotH = cardH + (r == pageRows - 1 ? extraH : 0);
                                var x = margin + c * cardW;
                                var y = top + r * cardH;
                                var pad = 6;
                                byte[] image = null;
                                string imageLabel = "";
                                var lines = new List<string>();
                                foreach (var field in sheet.Cards[index])
                                {
                                    var picture = field.ImageBytes != null && field.ImageBytes.Length > 0;
                                    if (picture && image == null)
                                    {
                                        image = field.ImageBytes;
                                        imageLabel = field.Label ?? "";
                                        continue;
                                    }
                                    if (field.IsImage && image == null)
                                        continue;
                                    var value = string.IsNullOrWhiteSpace(field.Value) ? "\u2014" : field.Value;
                                    lines.Add((field.Label ?? "") + "    " + value);
                                }
                                var bytes = image;
                                slide.RectangleShape(rect =>
                                {
                                    rect.X(x + 2).Y(y + 2).Width(Math.Max(8, slotW - 4)).Height(Math.Max(8, slotH - 4));
                                    rect.SolidFill(fill => fill.Color("FFFFFF"));
                                });
                                var cursor = y + pad;
                                var innerLeft = x + pad;
                                var innerW = Math.Max(40, slotW - pad * 2);
                                if (bytes != null)
                                {
                                    var label = imageLabel;
                                    var labelY = cursor;
                                    slide.Shape(box =>
                                    {
                                        box.X(innerLeft).Y(labelY).Width(innerW).Height(14);
                                        box.TextBox(label);
                                        box.Font(font => font.Size(10));
                                    });
                                    cursor += 16;
                                    var rowMm = slotH * 25.4 / 72.0;
                                    var cardMm = slotW * 25.4 / 72.0;
                                    var fittedSlot = CardPageFit.Measure(rowMm, cardMm, Math.Max(1, lines.Count), true);
                                    var maxImgH = Math.Max(24, (int)(fittedSlot.ImageMm * 72 / 25.4));
                                    var fitted = ImagePixels.Fit(bytes, innerW, maxImgH);
                                    var ix = innerLeft + Math.Max(0, (innerW - fitted.Width) / 2);
                                    var iy = cursor;
                                    var iw = fitted.Width;
                                    var ih = fitted.Height;
                                    var picBytes = bytes;
                                    slide.Picture(pic =>
                                    {
                                        pic.X(ix).Y(iy).Width(iw).Height(ih);
                                        pic.Image(new MemoryStream(picBytes));
                                    });
                                    cursor += ih + 6;
                                }
                                var textTop = cursor;
                                var textH = Math.Max(20, y + slotH - pad - textTop);
                                var fittedText = CardPageFit.Measure(slotH * 25.4 / 72.0, slotW * 25.4 / 72.0, Math.Max(1, lines.Count), bytes != null);
                                var fontPt = fittedText.FontPx;
                                var body = string.Join("\n", lines);
                                slide.Shape(box =>
                                {
                                    box.X(innerLeft).Y(textTop).Width(innerW).Height(textH);
                                    box.TextBox(body);
                                    box.Font(font => font.Size(fontPt));
                                });
                            }
                        }
                    });
                }
            });
            using var ms = new MemoryStream();
            pres.SlideWidth = 960;
            pres.SlideHeight = 540;
            pres.Save(ms);
            pres.Dispose();
        return ms.ToArray();
    }

    public static FormDocument Read(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes ?? Array.Empty<byte>());
            var pres = new Presentation(ms);
            var doc = new FormDocument();
            var first = true;
            foreach (var slide in pres.Slides)
            {
                var texts = new List<string>();
                var tables = new List<FormTable>();
                foreach (var shape in slide.Shapes)
                {
                    if (shape.Table != null)
                        tables.Add(ReadTable(shape.Table));
                    else if (shape.TextBox != null)
                    {
                        var text = shape.TextBox.Text?.Trim();
                        if (!string.IsNullOrEmpty(text))
                            texts.Add(text);
                    }
                }
                if (first)
                {
                    doc.Title = texts.FirstOrDefault() ?? "";
                    first = false;
                    if (tables.Count == 0 && texts.Count <= 1)
                        continue;
                }
                var section = new FormSection
                {
                    Title = texts.FirstOrDefault() ?? "",
                    Level = 2,
                    Tables = tables
                };
                if (texts.Count > 1)
                {
                    for (var i = 1; i < texts.Count; i++)
                        section.Fields.Add(ParsePair(texts[i]));
                }
                doc.Sections.Add(section);
            }
            pres.Dispose();
            return doc;
        }

        private static void Fill(DraftSlide slide, SlideSpec spec, TemplatePaint paint)
        {
            slide.Shape(box =>
            {
                box.X(36).Y(24).Width(640).Height(spec.Kind == SlideKind.Title ? 80 : 36);
                box.TextBox(spec.Title ?? "");
                box.Font(font =>
                {
                    var titlePt = paint == null
                        ? (spec.Kind == SlideKind.Title ? 28 : 20)
                        : (spec.Kind == SlideKind.Title ? paint.TitlePt : Math.Max(10, paint.TitlePt - 4));
                    font.Size(titlePt);
                    if (paint == null || paint.TitleBold) font.Bold();
                });
            });
            if (spec.Table == null || spec.Table.Columns.Count == 0) return;
            var y = spec.Kind == SlideKind.Title ? 120 : 72;
            slide.TableShape(shape =>
            {
                shape.X(36).Y(y);
                shape.Table(table =>
                {
                    table.Columns(spec.Table.Columns.Count);
                    table.Row(row =>
                    {
                        foreach (var col in spec.Table.Columns)
                            row.Cell(cell => cell.TextBox(col ?? ""));
                    });
                    foreach (var line in spec.Table.Rows)
                    {
                        table.Row(row =>
                        {
                            for (var c = 0; c < spec.Table.Columns.Count; c++)
                            {
                                var text = c < line.Count ? line[c].Text : "";
                                row.Cell(cell => cell.TextBox(text ?? ""));
                            }
                        });
                    }
                });
            });
        }

        private static FormTable ReadTable(ITable table)
        {
            var grid = new FormTable();
            var first = true;
            foreach (var row in table.Rows)
            {
                var cells = new List<FormCell>();
                foreach (var cell in row.Cells)
                    cells.Add(new FormCell { Text = cell.TextBox?.Text?.Trim() ?? "" });
                if (first)
                {
                    grid.Columns = cells.Select(c => c.Text).ToList();
                    first = false;
                }
                else grid.Rows.Add(cells);
            }
            return grid;
        }

        private static FormField ParsePair(string text)
        {
            var idx = text.IndexOf(':');
            if (idx < 0) return new FormField { Value = text };
            return new FormField
            {
                Label = text.Substring(0, idx).Trim(),
                Value = text.Substring(idx + 1).Trim()
            };
        }

        private static List<SlideSpec> Plan(FormDocument doc)
        {
            var slides = new List<SlideSpec>
            {
                new SlideSpec { Kind = SlideKind.Title, Title = doc.Title ?? "" }
            };
            if (doc.Fields.Count > 0)
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = doc.Title ?? "",
                    Table = FieldsTable(doc.Fields)
                });
            }
            foreach (var section in doc.Sections)
                PlanSection(slides, section);
            if (!string.IsNullOrEmpty(doc.Notes))
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = "Notes",
                    Table = new FormTable
                    {
                        Columns = new List<string> { "Notes" },
                        Rows = new List<List<FormCell>> { new List<FormCell> { new FormCell { Text = doc.Notes } } }
                    }
                });
            }
            return slides;
        }

        private static void PlanSection(List<SlideSpec> slides, FormSection section)
        {
            if (section.Fields.Count > 0)
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = section.Title,
                    Table = FieldsTable(section.Fields)
                });
            }
            foreach (var table in section.Tables)
            {
                var offset = 0;
                while (offset < table.Rows.Count || (offset == 0 && table.Columns.Count > 0))
                {
                    var take = Math.Min(DocumentStyles.PptRowsPerSlide, Math.Max(0, table.Rows.Count - offset));
                    slides.Add(new SlideSpec
                    {
                        Kind = SlideKind.Section,
                        Title = string.IsNullOrEmpty(table.Title) ? section.Title : table.Title,
                        Table = new FormTable
                        {
                            Columns = table.Columns.ToList(),
                            Rows = table.Rows.Skip(offset).Take(take).Select(r => r.ToList()).ToList()
                        }
                    });
                    if (table.Rows.Count == 0) break;
                    offset += take;
                }
            }
            foreach (var child in section.Sections)
                PlanSection(slides, child);
        }

        private static FormTable FieldsTable(List<FormField> fields)
        {
            return new FormTable
            {
                Columns = new List<string> { "Field", "Value" },
                Rows = fields.Select(f => new List<FormCell>
                {
                    new FormCell { Text = f.Label },
                    new FormCell { Text = f.Value, IsNumber = f.IsNumber }
                }).ToList()
            };
        }

        private enum SlideKind { Title, Section }

        private sealed class SlideSpec
        {
            public SlideKind Kind;
            public string Title = "";
            public FormTable Table;
        }
    }
}
