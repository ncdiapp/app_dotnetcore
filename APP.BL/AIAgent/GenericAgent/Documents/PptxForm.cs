using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShapeCrawler;
using ShapeCrawler.Presentations;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// One slide per grid chunk (no orphan title-only first slide). Grids split after
    /// <see cref="DocumentStyles.PptRowsPerSlide"/> rows (fewer when Sketch/image columns are present).
    /// </summary>
    public static class PptxForm
    {
        private const int SlideW = 960;
        private const int SlideH = 540;
        private const int Margin = 36;
        private const int ContentW = SlideW - Margin * 2;
        /// <summary>Data rows per slide when grid has image columns (tall rows).</summary>
        private const int PptImageRowsPerSlide = 4;

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
            pres.SlideWidth = SlideW;
            pres.SlideHeight = SlideH;
            try { ApplyGridLayout(pres, slides); }
            catch { /* keep draft table if ShapeCrawler layout API fails */ }

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
            const int titleH = 32;
            var title = (spec.Title ?? "").Trim();
            if (!string.IsNullOrEmpty(title))
            {
                slide.Shape(box =>
                {
                    box.X(Margin).Y(16).Width(ContentW).Height(titleH);
                    box.TextBox(title);
                    box.Font(font =>
                    {
                        var titlePt = paint == null ? 18 : Math.Max(12, paint.TitlePt);
                        font.Size(titlePt);
                        if (paint == null || paint.TitleBold) font.Bold();
                    });
                });
            }
            if (spec.Table == null || spec.Table.Columns.Count == 0) return;
            var y = string.IsNullOrEmpty(title) ? 24 : 56;
            slide.TableShape(shape =>
            {
                shape.X(Margin).Y(y);
                shape.Table(table =>
                {
                    table.Columns(spec.Table.Columns.Count);
                    table.Row(row =>
                    {
                        for (var hi = 0; hi < spec.Table.Columns.Count; hi++)
                        {
                            var field = spec.Table.Columns[hi];
                            var label = spec.Table.DisplayHeaders != null && hi < spec.Table.DisplayHeaders.Count
                                        && !string.IsNullOrWhiteSpace(spec.Table.DisplayHeaders[hi])
                                ? spec.Table.DisplayHeaders[hi]
                                : field;
                            row.Cell(cell => cell.TextBox(label ?? ""));
                        }
                    });
                    foreach (var line in spec.Table.Rows)
                    {
                        var exportLine = FormGridCellPolish.FormatExportRow(line, spec.Table.Formats);
                        table.Row(row =>
                        {
                            for (var c = 0; c < spec.Table.Columns.Count; c++)
                            {
                                // Never dump image URL into PPT cells — wraps tall rows and squeezes columns.
                                // Bytes are painted in ApplyGridLayout via cell Fill.SetPicture.
                                var text = CellTextForPpt(spec.Table, c, c < exportLine.Count ? exportLine[c] : null);
                                row.Cell(cell => cell.TextBox(text));
                            }
                        });
                    }
                });
            });
        }

        private static string CellTextForPpt(FormTable table, int col, FormCell cell)
        {
            if (IsImageCell(table, col, cell)) return "";
            return cell?.Text ?? "";
        }

        private static bool IsImageCell(FormTable table, int col, FormCell cell)
        {
            if (cell?.ImageBytes != null && cell.ImageBytes.Length > 0) return true;
            if (cell?.IsImage == true) return true;
            var colName = table?.Columns != null && col < table.Columns.Count ? table.Columns[col] : null;
            if (CardSheet.IsImageFieldName(colName)) return true;
            var fmt = table?.Formats != null && col < table.Formats.Count ? table.Formats[col] : null;
            return string.Equals(
                AiPolishedRenderSpec.NormalizeColumnFormat(fmt),
                "image",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TableHasImageColumn(FormTable table)
        {
            if (table?.Columns == null) return false;
            for (var c = 0; c < table.Columns.Count; c++)
            {
                if (CardSheet.IsImageFieldName(table.Columns[c])) return true;
                var fmt = table.Formats != null && c < table.Formats.Count ? table.Formats[c] : null;
                if (string.Equals(
                        AiPolishedRenderSpec.NormalizeColumnFormat(fmt),
                        "image",
                        StringComparison.OrdinalIgnoreCase))
                    return true;
                if (table.Rows == null) continue;
                foreach (var row in table.Rows)
                {
                    if (row == null || c >= row.Count) continue;
                    if (IsImageCell(table, c, row[c])) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Stretch table to slide content width, size image columns, embed ImageBytes as cell picture fills.
        /// Pictures are letterboxed to the cell box first — Fill.SetPicture stretch-fills and would
        /// otherwise distort Sketch / product photos.
        /// </summary>
        private static void ApplyGridLayout(Presentation presentation, List<SlideSpec> slides)
        {
            if (presentation?.Slides == null || slides == null) return;
            var n = Math.Min(presentation.Slides.Count, slides.Count);
            for (var i = 0; i < n; i++)
            {
                var spec = slides[i];
                if (spec?.Table == null || spec.Table.Columns == null || spec.Table.Columns.Count == 0)
                    continue;
                var slide = presentation.Slides[i];
                IShape tableShape = null;
                foreach (var shape in slide.Shapes)
                {
                    if (shape?.Table != null)
                    {
                        tableShape = shape;
                        break;
                    }
                }
                if (tableShape?.Table == null) continue;

                // Title text box: full content width (draft used ContentW already; widen any leftover 640pt box).
                foreach (var shape in slide.Shapes)
                {
                    if (shape == null || shape.Table != null || shape.Picture != null) continue;
                    if (shape.Y <= 40 && shape.Width < ContentW - 20)
                        shape.Width = ContentW;
                }

                tableShape.X = Margin;
                tableShape.Width = ContentW;

                var table = tableShape.Table;
                var colCount = table.Columns.Count;
                if (colCount <= 0) continue;

                var imageCols = new HashSet<int>();
                for (var c = 0; c < colCount; c++)
                {
                    if (c < spec.Table.Columns.Count && IsImageCell(spec.Table, c, null))
                        imageCols.Add(c);
                    if (spec.Table.Rows == null) continue;
                    foreach (var row in spec.Table.Rows)
                    {
                        if (row == null || c >= row.Count) continue;
                        if (IsImageCell(spec.Table, c, row[c]))
                            imageCols.Add(c);
                    }
                }

                const int imageColW = 90;
                const int imageRowH = 78;
                var otherCount = Math.Max(1, colCount - imageCols.Count);
                var usedByImages = imageCols.Count * imageColW;
                if (usedByImages >= ContentW - otherCount * 40)
                {
                    // Too many image cols — share content width evenly.
                    var even = Math.Max(40, ContentW / colCount);
                    for (var c = 0; c < colCount; c++)
                        table.Columns[c].Width = even;
                }
                else
                {
                    var otherW = Math.Max(48, (ContentW - usedByImages) / otherCount);
                    var assigned = 0;
                    for (var c = 0; c < colCount; c++)
                    {
                        var w = imageCols.Contains(c) ? imageColW : otherW;
                        if (c == colCount - 1)
                            w = Math.Max(40, ContentW - assigned);
                        table.Columns[c].Width = w;
                        assigned += w;
                    }
                }

                if (table.Rows.Count > 0)
                    table.Rows[0].Height = 22;

                // Pass 1: row heights (needed so letterbox box matches final cell size).
                for (var r = 0; r < (spec.Table.Rows?.Count ?? 0); r++)
                {
                    var tableRowIdx = r + 1;
                    if (tableRowIdx >= table.Rows.Count) break;
                    var line = spec.Table.Rows[r];
                    var hasPic = false;
                    for (var c = 0; c < colCount && c < (line?.Count ?? 0); c++)
                    {
                        var src = line[c];
                        if (src?.ImageBytes != null && src.ImageBytes.Length > 0)
                            hasPic = true;
                    }
                    table.Rows[tableRowIdx].Height = hasPic || imageCols.Count > 0 ? imageRowH : 18;
                }

                // Pass 2: letterbox → SetPicture (stretch fill keeps aspect because bitmap matches cell ratio).
                for (var r = 0; r < (spec.Table.Rows?.Count ?? 0); r++)
                {
                    var tableRowIdx = r + 1;
                    if (tableRowIdx >= table.Rows.Count) break;
                    var line = spec.Table.Rows[r];
                    var rowH = Math.Max(1, (int)Math.Round(table.Rows[tableRowIdx].Height));
                    for (var c = 0; c < colCount && c < (line?.Count ?? 0); c++)
                    {
                        var src = line[c];
                        if (src?.ImageBytes == null || src.ImageBytes.Length == 0) continue;
                        try
                        {
                            var pptCell = table[tableRowIdx, c];
                            if (pptCell?.TextBox != null)
                                pptCell.TextBox.SetText("");
                            if (pptCell?.Fill == null) continue;
                            var cellW = Math.Max(1, (int)Math.Round(table.Columns[c].Width));
                            // ~2× pt for sharper embeds; ratio must match the cell.
                            var boxW = Math.Max(16, cellW * 2);
                            var boxH = Math.Max(16, rowH * 2);
                            var padded = ImagePixels.ContainPng(src.ImageBytes, boxW, boxH);
                            using var stream = new MemoryStream(padded);
                            pptCell.Fill.SetPicture(stream);
                        }
                        catch
                        {
                            /* leave blank cell */
                        }
                    }
                }
            }
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
            var docTitle = string.IsNullOrWhiteSpace(doc?.Title) ? "Result" : doc.Title.Trim();
            var slides = new List<SlideSpec>();
            // Do NOT emit a title-only first slide — it wastes a full page with just "Result".
            if (doc?.Fields != null && doc.Fields.Count > 0)
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = docTitle,
                    Table = FieldsTable(doc.Fields)
                });
            }
            if (doc?.Sections != null)
            {
                foreach (var section in doc.Sections)
                    PlanSection(slides, section, docTitle);
            }
            if (!string.IsNullOrEmpty(doc?.Notes))
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
            if (slides.Count == 0)
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = docTitle,
                    Table = new FormTable { Columns = new List<string> { "Message" }, Rows = new List<List<FormCell>>() }
                });
            }
            ApplySlideTitles(slides, docTitle);
            return slides;
        }

        /// <summary>Fill blank slide headers with doc title; add (n/m) when multiple slides.</summary>
        private static void ApplySlideTitles(List<SlideSpec> slides, string docTitle)
        {
            if (slides == null || slides.Count == 0) return;
            var total = slides.Count;
            for (var i = 0; i < total; i++)
            {
                var baseTitle = string.IsNullOrWhiteSpace(slides[i].Title) ? docTitle : slides[i].Title.Trim();
                slides[i].Title = total > 1
                    ? baseTitle + " (" + (i + 1).ToString() + "/" + total.ToString() + ")"
                    : baseTitle;
            }
        }

        private static void PlanSection(List<SlideSpec> slides, FormSection section, string docTitle)
        {
            if (section == null) return;
            var sectionTitle = FirstNonEmpty(section.Title, docTitle);
            if (section.Fields != null && section.Fields.Count > 0)
            {
                slides.Add(new SlideSpec
                {
                    Kind = SlideKind.Section,
                    Title = sectionTitle,
                    Table = FieldsTable(section.Fields)
                });
            }
            foreach (var table in section.Tables ?? new List<FormTable>())
            {
                if (table == null) continue;
                var offset = 0;
                var rowsPer = TableHasImageColumn(table)
                    ? PptImageRowsPerSlide
                    : DocumentStyles.PptRowsPerSlide;
                var tableTitle = FirstNonEmpty(table.Title, sectionTitle, docTitle);
                while (offset < table.Rows.Count || (offset == 0 && table.Columns.Count > 0))
                {
                    var take = Math.Min(rowsPer, Math.Max(0, table.Rows.Count - offset));
                    slides.Add(new SlideSpec
                    {
                        Kind = SlideKind.Section,
                        Title = tableTitle,
                        Table = new FormTable
                        {
                            Columns = table.Columns.ToList(),
                            DisplayHeaders = table.DisplayHeaders?.ToList(),
                            Formats = table.Formats?.ToList(),
                            Rows = table.Rows.Skip(offset).Take(take).Select(r => r.ToList()).ToList()
                        }
                    });
                    if (table.Rows.Count == 0) break;
                    offset += take;
                }
            }
            foreach (var child in section.Sections ?? new List<FormSection>())
                PlanSection(slides, child, docTitle);
        }

        private static string FirstNonEmpty(params string[] parts)
        {
            if (parts == null) return "Result";
            foreach (var p in parts)
            {
                if (!string.IsNullOrWhiteSpace(p)) return p.Trim();
            }
            return "Result";
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
