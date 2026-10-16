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
        {
            var slides = Plan(doc);
            var pres = new Presentation(draft =>
            {
                foreach (var spec in slides)
                    draft.Slide(slide => Fill(slide, spec));
            });

            using var ms = new MemoryStream();
            pres.Save(ms);
            pres.Dispose();
            return ms.ToArray();
        }

        public static byte[] WriteCards(CardSheet sheet)
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
                        slide.Shape(box =>
                        {
                            box.X(36).Y(24).Width(640).Height(36);
                            box.TextBox(title);
                            box.Font(font => { font.Size(20); font.Bold(); });
                        });
                        slide.TableShape(shape =>
                        {
                            shape.X(36).Y(72);
                            shape.Table(table =>
                            {
                                table.Columns(perRow);
                                for (var r = 0; r < pageRows; r++)
                                {
                                    table.Row(row =>
                                    {
                                        for (var c = 0; c < perRow; c++)
                                        {
                                            var index = page * pageSize + r * perRow + c;
                                            var text = index < sheet.Cards.Count ? CardText(sheet.Cards[index]) : "";
                                            row.Cell(cell => cell.TextBox(text));
                                        }
                                    });
                                }
                            });
                        });
                        for (var r = 0; r < pageRows; r++)
                        {
                            for (var c = 0; c < perRow; c++)
                            {
                                var index = page * pageSize + r * perRow + c;
                                if (index >= sheet.Cards.Count) continue;
                                byte[] image = null;
                                foreach (var field in sheet.Cards[index])
                                {
                                    if (field.ImageBytes != null && field.ImageBytes.Length > 0)
                                    {
                                        image = field.ImageBytes;
                                        break;
                                    }
                                }
                                if (image == null) continue;
                                var cardW = Math.Max(40, 640 / perRow);
                                var cardH = Math.Max(48, 360 / pageRows);
                                var px = 40 + c * cardW;
                                var py = 78 + r * cardH;
                                var bytes = image;
                                slide.Picture(pic =>
                                {
                                    pic.X(px).Y(py).Width(120).Height(90);
                                    pic.Image(new MemoryStream(bytes));
                                });
                            }
                        }
                    });
                }
            });
            using var ms = new MemoryStream();
            pres.Save(ms);
            pres.Dispose();
            return ms.ToArray();
        }

        private static string CardText(List<CardField> fields)
        {
            var lines = new List<string>();
            foreach (var field in fields)
            {
                if (field.IsImage || (field.ImageBytes != null && field.ImageBytes.Length > 0))
                    lines.Add(field.Label ?? "");
                else
                    lines.Add((field.Label ?? "") + ": " + (field.Value ?? ""));
            }
            return string.Join("\n", lines);
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

        private static void Fill(DraftSlide slide, SlideSpec spec)
        {
            slide.Shape(box =>
            {
                box.X(36).Y(24).Width(640).Height(spec.Kind == SlideKind.Title ? 80 : 36);
                box.TextBox(spec.Title ?? "");
                box.Font(font =>
                {
                    font.Size(spec.Kind == SlideKind.Title ? 28 : 20);
                    font.Bold();
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
