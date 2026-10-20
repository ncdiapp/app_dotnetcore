using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Playwright;
using Newtonsoft.Json.Linq;
using ShapeCrawler;
using A = DocumentFormat.OpenXml.Drawing;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Dashboard/KPI export to PPTX (chart + grid as fitted images) and XLSX (data sheet + chart PNGs).
    /// </summary>
    public static class DashboardOffice
    {
        private const int SlideW = 960;
        private const int SlideH = 540;
        private const int Margin = 20;
        private const int TitleH = 36;

        /// <summary>
        /// Title slide, then chart slides, then grid slides.
        /// Grid: same page-fit fonts as PDF (shrink to fit width); tall tables paginate vertically (no vertical squash).
        /// </summary>
        public static async Task<byte[]> WritePptxAsync(string html, string title, CancellationToken ct = default)
        {
            var panels = await CapturePanelsAsync(html, ct).ConfigureAwait(false);
            if (panels.Count == 0)
                panels.Add(new PanelShot("Dashboard", PlaceholderPng(), false));

            var maxW = SlideW - Margin * 2;
            var maxH = SlideH - Margin * 2 - TitleH - 8;

            var pres = new Presentation(draft =>
            {
                draft.Slide(slide =>
                {
                    slide.Shape(box =>
                    {
                        box.X(Margin).Y(180).Width(maxW).Height(80);
                        box.TextBox(string.IsNullOrWhiteSpace(title) ? "Dashboard" : title);
                        box.Font(font =>
                        {
                            font.Size(28);
                            font.Bold();
                        });
                    });
                });

                foreach (var panel in panels)
                {
                    var label = panel.Title;
                    var bytes = panel.Png;
                    var isGrid = panel.IsGrid;
                    draft.Slide(slide =>
                    {
                        slide.Shape(box =>
                        {
                            box.X(Margin).Y(Margin).Width(maxW).Height(TitleH);
                            box.TextBox(label ?? "");
                            box.Font(font => font.Size(16));
                        });
                        int w, h, x, y;
                        if (isGrid)
                        {
                            // Width fills slide; height follows aspect (slice already ≤ maxH) — never squash.
                            var size = ImagePixels.Read(bytes);
                            if (size.Width <= 0) size = (maxW, maxH);
                            var scale = maxW / (double)size.Width;
                            w = maxW;
                            h = Math.Max(1, (int)Math.Round(size.Height * scale));
                            if (h > maxH) h = maxH;
                            x = Margin;
                            y = Margin + TitleH + 8;
                        }
                        else
                        {
                            var fitted = ImagePixels.Fit(bytes, maxW, maxH);
                            w = fitted.Width;
                            h = fitted.Height;
                            x = Margin + Math.Max(0, (maxW - w) / 2);
                            y = Margin + TitleH + 8;
                        }
                        slide.Picture(pic =>
                        {
                            pic.X(x).Y(y).Width(w).Height(h);
                            pic.Image(new MemoryStream(bytes));
                        });
                    });
                }
            });

            using var ms = new MemoryStream();
            pres.SlideWidth = SlideW;
            pres.SlideHeight = SlideH;
            pres.Save(ms);
            pres.Dispose();
            return ms.ToArray();
        }

        private sealed record PanelShot(string Title, byte[] Png, bool IsGrid);

        /// <summary>Sheet "Document" (data) from ExcelForm; optional "Charts" sheet with PNG images.</summary>
        public static byte[] WriteXlsx(JArray rows, string html, string title)
        {
            var columns = new List<string>();
            var tableRows = new List<List<FormCell>>();
            if (rows != null && rows.Count > 0 && rows[0] is JObject first)
            {
                foreach (var p in first.Properties())
                    columns.Add(p.Name);
                var n = Math.Min(rows.Count, UiRenderSpec.MaxTableRows);
                for (var i = 0; i < n; i++)
                {
                    if (rows[i] is not JObject row) continue;
                    var cells = new List<FormCell>();
                    foreach (var c in columns)
                        cells.Add(new FormCell { Text = CellText(row[c]) });
                    tableRows.Add(cells);
                }
            }

            var doc = FormDocument.FromTable(
                string.IsNullOrWhiteSpace(title) ? "Data" : title,
                columns,
                tableRows);
            var dataBytes = ExcelForm.Write(doc);
            var charts = ExtractChartPngs(html);
            if (charts.Count == 0)
                return dataBytes;

            using var ms = new MemoryStream();
            ms.Write(dataBytes, 0, dataBytes.Length);
            ms.Position = 0;
            using (var workbook = SpreadsheetDocument.Open(ms, true))
            {
                var wb = workbook.WorkbookPart;
                var sheets = wb.Workbook.Sheets ?? wb.Workbook.AppendChild(new Sheets());
                uint nextId = 1;
                foreach (var existing in sheets.Elements<Sheet>())
                {
                    if (existing.SheetId != null && existing.SheetId.Value >= nextId)
                        nextId = existing.SheetId.Value + 1;
                }

                var chartWs = wb.AddNewPart<WorksheetPart>();
                var data = new SheetData();
                uint row = 1;
                var pictures = new List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)>();
                foreach (var chart in charts)
                {
                    var r = new Row { RowIndex = row };
                    r.Append(new Cell
                    {
                        CellReference = "A" + row.ToString(CultureInfo.InvariantCulture),
                        DataType = CellValues.InlineString,
                        InlineString = new InlineString(new Text(chart.Title ?? "Chart"))
                    });
                    data.Append(r);
                    var box = ImagePixels.Fit(chart.Png, 640, 320);
                    // EMUs: 9525 ≈ 1 CSS px
                    pictures.Add((1, row + 1, chart.Png, box.Width * 9525L, box.Height * 9525L));
                    row += 18;
                }
                chartWs.Worksheet = new Worksheet(data);
                AddPictures(chartWs, pictures);
                chartWs.Worksheet.Save();
                sheets.Append(new Sheet
                {
                    Id = wb.GetIdOfPart(chartWs),
                    SheetId = nextId,
                    Name = "Charts"
                });
                wb.Workbook.Save();
            }
            return ms.ToArray();
        }

        public static List<(string Title, byte[] Png)> ExtractChartPngs(string html)
        {
            var list = new List<(string, byte[])>();
            if (string.IsNullOrWhiteSpace(html)) return list;

            // Match chart panels: title then img data URI
            var panelRe = new Regex(
                @"<section[^>]*class=""[^""]*chart-panel[^""]*""[^>]*>[\s\S]*?<h2[^>]*class=""[^""]*chart-title[^""]*""[^>]*>(.*?)</h2>[\s\S]*?<img[^>]+src=""data:image/png;base64,([A-Za-z0-9+/=]+)""",
                RegexOptions.IgnoreCase);
            foreach (Match m in panelRe.Matches(html))
            {
                try
                {
                    var title = System.Net.WebUtility.HtmlDecode(
                        Regex.Replace(m.Groups[1].Value, "<.*?>", "").Trim());
                    var bytes = Convert.FromBase64String(m.Groups[2].Value);
                    if (bytes.Length > 0)
                        list.Add((string.IsNullOrWhiteSpace(title) ? "Chart" : title, bytes));
                }
                catch { /* skip bad */ }
            }

            if (list.Count == 0)
            {
                var imgRe = new Regex(@"data:image/png;base64,([A-Za-z0-9+/=]+)", RegexOptions.IgnoreCase);
                var i = 0;
                foreach (Match m in imgRe.Matches(html))
                {
                    try
                    {
                        var bytes = Convert.FromBase64String(m.Groups[1].Value);
                        if (bytes.Length > 80)
                            list.Add(("Chart " + (++i).ToString(CultureInfo.InvariantCulture), bytes));
                    }
                    catch { /* skip */ }
                    if (list.Count >= ChartRasterize.MaxCharts) break;
                }
            }
            return list;
        }

        private static async Task<List<PanelShot>> CapturePanelsAsync(string html, CancellationToken ct)
        {
            var list = new List<PanelShot>();
            if (string.IsNullOrWhiteSpace(html)) return list;

            var contentW = SlideW - Margin * 2;
            var contentH = SlideH - Margin * 2 - TitleH - 8;

            using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            }).ConfigureAwait(false);
            var page = await browser.NewPageAsync().ConfigureAwait(false);
            // Viewport = slide content width so page-fit fonts match PDF width logic
            await page.SetViewportSizeAsync(contentW + Margin * 2, 1200).ConfigureAwait(false);
            var printHtml = HtmlForm.ForDashboardPrint(html);
            await page.SetContentAsync(printHtml, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);
            await page.EvaluateAsync(
                @"(w) => {
                    document.body.style.margin = '0';
                    document.body.style.padding = '0';
                    var ui = document.querySelector('.agent-ui');
                    if (ui) { ui.style.padding = '8px'; ui.style.maxWidth = w + 'px'; ui.style.width = w + 'px'; }
                    document.querySelectorAll('.data-table').forEach(function(el) {
                        el.style.width = w + 'px';
                        el.style.maxWidth = w + 'px';
                        el.style.overflow = 'visible';
                    });
                }",
                contentW).ConfigureAwait(false);

            var chartCount = await page.Locator("section.chart-panel").CountAsync().ConfigureAwait(false);
            for (var i = 0; i < chartCount && i < ChartRasterize.MaxCharts; i++)
            {
                ct.ThrowIfCancellationRequested();
                var loc = page.Locator("section.chart-panel").Nth(i);
                var title = await loc.Locator(".chart-title").InnerTextAsync().ConfigureAwait(false);
                var bytes = await loc.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                list.Add(new PanelShot(string.IsNullOrWhiteSpace(title) ? "Chart" : title.Trim(), bytes, false));
            }

            var tableCount = await page.Locator("section.data-table").CountAsync().ConfigureAwait(false);
            for (var i = 0; i < tableCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                var loc = page.Locator("section.data-table").Nth(i);
                var title = "Data";
                try
                {
                    var h2 = loc.Locator("h2");
                    if (await h2.CountAsync().ConfigureAwait(false) > 0)
                        title = (await h2.First.InnerTextAsync().ConfigureAwait(false)).Trim();
                }
                catch { /* keep Data */ }
                if (string.IsNullOrWhiteSpace(title)) title = "Data";

                var box = await loc.BoundingBoxAsync().ConfigureAwait(false);
                if (box == null || box.Width < 1 || box.Height < 1)
                {
                    var bytes = await loc.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                    list.Add(new PanelShot(title, bytes, true));
                    continue;
                }

                // Vertical pagination: clip strips of contentH; each strip keeps full page width (no squash).
                var totalH = box.Height;
                var pageIndex = 0;
                var pages = Math.Max(1, (int)Math.Ceiling(totalH / contentH));
                for (double yOff = 0; yOff < totalH - 0.5; yOff += contentH)
                {
                    ct.ThrowIfCancellationRequested();
                    pageIndex++;
                    var sliceH = Math.Min(contentH, totalH - yOff);
                    if (sliceH < 1) break;
                    var clip = new Clip
                    {
                        X = (float)box.X,
                        Y = (float)(box.Y + yOff),
                        Width = (float)box.Width,
                        Height = (float)sliceH
                    };
                    var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        Clip = clip
                    }).ConfigureAwait(false);
                    var label = pages > 1
                        ? title + " (" + pageIndex.ToString(CultureInfo.InvariantCulture) + "/" + pages.ToString(CultureInfo.InvariantCulture) + ")"
                        : title;
                    list.Add(new PanelShot(label, bytes, true));
                }
            }

            return list;
        }

        private static void AddPictures(WorksheetPart worksheetPart, List<(int Column, uint Row, byte[] Bytes, long Cx, long Cy)> pictures)
        {
            if (pictures == null || pictures.Count == 0) return;
            var drawings = worksheetPart.AddNewPart<DrawingsPart>();
            var drawing = new Xdr.WorksheetDrawing();
            uint index = 1;
            foreach (var picture in pictures)
            {
                var part = drawings.AddImagePart(ImagePartType.Png);
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
                        new Xdr.NonVisualDrawingProperties { Id = index + 1, Name = "Chart" + index.ToString(CultureInfo.InvariantCulture) },
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

        private static string CellText(JToken v)
        {
            if (v == null || v.Type == JTokenType.Null) return "";
            if (v.Type == JTokenType.String) return v.Value<string>() ?? "";
            return v.ToString();
        }

        private static byte[] PlaceholderPng()
        {
            // 1x1 transparent PNG
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        }
    }
}
