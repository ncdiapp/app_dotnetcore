using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using ShapeCrawler;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// PPTX from TemplateContent merged HTML via Playwright screenshots (visual deck).
    /// </summary>
    public static class HtmlOfficeExport
    {
        private const int SlideW = 960;
        private const int SlideH = 540;
        private const int Margin = 16;
        private const int TitleH = 28;
        private const int MaxSlides = 40;

        public static async Task<byte[]> WritePptxFromHtmlAsync(string html, string title, CancellationToken ct = default)
        {
            html = AgentUiCssInline.PrepareExportHtml(html ?? "", addPrintExtras: true);
            var shots = await CaptureCardOrPageShotsAsync(html, ct).ConfigureAwait(false);
            if (shots.Count == 0)
                shots.Add(("Slide", PlaceholderPng()));

            var maxW = SlideW - Margin * 2;
            var maxH = SlideH - Margin * 2 - TitleH - 8;
            var pageTitle = string.IsNullOrWhiteSpace(title) ? "Result" : title.Trim();

            var pres = new Presentation(draft =>
            {
                draft.Slide(slide =>
                {
                    slide.Shape(box =>
                    {
                        box.X(Margin).Y(200).Width(maxW).Height(60);
                        box.TextBox(pageTitle);
                        box.Font(font =>
                        {
                            font.Size(24);
                            font.Bold();
                        });
                    });
                });

                foreach (var (label, png) in shots)
                {
                    draft.Slide(slide =>
                    {
                        slide.Shape(box =>
                        {
                            box.X(Margin).Y(Margin).Width(maxW).Height(TitleH);
                            box.TextBox(string.IsNullOrWhiteSpace(label) ? pageTitle : label);
                            box.Font(font => font.Size(14));
                        });
                        var fitted = ImagePixels.Fit(png, maxW, maxH);
                        var w = fitted.Width;
                        var h = fitted.Height;
                        var x = Margin + Math.Max(0, (maxW - w) / 2);
                        var y = Margin + TitleH + 8;
                        slide.Picture(pic =>
                        {
                            pic.X(x).Y(y).Width(w).Height(h);
                            pic.Image(new MemoryStream(png));
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

        private static async Task<List<(string Title, byte[] Png)>> CaptureCardOrPageShotsAsync(string html, CancellationToken ct)
        {
            var list = new List<(string, byte[])>();
            using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            }).ConfigureAwait(false);
            var page = await browser.NewPageAsync().ConfigureAwait(false);
            await page.SetViewportSizeAsync(1100, 1400).ConfigureAwait(false);
            await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);

            // layout=page export: optional Grid section first, then fitted card pages.
            var gridLoc = page.Locator("#export-grid");
            if (await gridLoc.CountAsync().ConfigureAwait(false) > 0)
            {
                ct.ThrowIfCancellationRequested();
                var gridPng = await gridLoc.First.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                list.Add(("Grid view", gridPng));
            }

            // Fitted Print/Export HTML: one .card-page per R×C page.
            var pageCount = await page.Locator(".card-page").CountAsync().ConfigureAwait(false);
            if (pageCount > 0)
            {
                var n = Math.Min(pageCount, MaxSlides - list.Count);
                for (var i = 0; i < n; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var loc = page.Locator(".card-page").Nth(i);
                    var label = "";
                    try { label = await loc.Locator("[data-role='title']").First.InnerTextAsync().ConfigureAwait(false); }
                    catch { /* ignore */ }
                    var bytes = await loc.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                    list.Add((string.IsNullOrWhiteSpace(label) ? "Cards " + (i + 1) : label.Trim(), bytes));
                }
                if (list.Count > 0) return list;
            }

            // Never one-slide-per-card (ignores 2x2/3x4). Prefer board root if present.
            var boardLoc = page.Locator(".agent-cards-root");
            if (await boardLoc.CountAsync().ConfigureAwait(false) > 0)
            {
                var pngBoard = await boardLoc.First.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                list.Add(("Page", pngBoard));
                return list;
            }

            var root = page.Locator("body").First;
            var png = await root.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
            list.Add(("Page", png));
            return list;
        }

        private static byte[] PlaceholderPng()
        {
            // 1x1 PNG
            return Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        }
    }
}
