using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace App.BL.AIAgent.GenericAgent.Documents
{
    /// <summary>
    /// Replaces .chart-surface[data-rasterize] SVG panels with PNG data-URI images via Playwright.
    /// </summary>
    public static class ChartRasterize
    {
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(2, 2);
        public const int MaxCharts = UiRenderSpec.MaxCharts;

        public static async Task<(string Html, List<string> Missing)> EmbedChartPngsAsync(string html, CancellationToken ct = default)
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(html) || html.IndexOf("data-rasterize", StringComparison.OrdinalIgnoreCase) < 0)
                return (html, missing);

            await Gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
                }).ConfigureAwait(false);
                var page = await browser.NewPageAsync().ConfigureAwait(false);
                await page.SetContentAsync(html, new PageSetContentOptions { WaitUntil = WaitUntilState.DOMContentLoaded }).ConfigureAwait(false);

                var count = await page.Locator(".chart-surface[data-rasterize]").CountAsync().ConfigureAwait(false);
                if (count > MaxCharts) count = MaxCharts;

                for (var i = 0; i < count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var locator = page.Locator(".chart-surface[data-rasterize]").Nth(0);
                    try
                    {
                        var bytes = await locator.ScreenshotAsync(new LocatorScreenshotOptions { Type = ScreenshotType.Png }).ConfigureAwait(false);
                        var dataUri = "data:image/png;base64," + Convert.ToBase64String(bytes);
                        await locator.EvaluateAsync(
                            @"(el, uri) => {
                                el.innerHTML = '';
                                var img = document.createElement('img');
                                img.alt = 'chart';
                                img.src = uri;
                                img.style.maxWidth = '100%';
                                img.style.height = 'auto';
                                img.style.display = 'block';
                                el.appendChild(img);
                                el.removeAttribute('data-rasterize');
                            }",
                            dataUri).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        missing.Add("chart-" + (i + 1) + ": " + ex.Message);
                        try
                        {
                            await locator.EvaluateAsync("el => el.removeAttribute('data-rasterize')").ConfigureAwait(false);
                        }
                        catch { /* ignore */ }
                    }
                }

                var result = await page.ContentAsync().ConfigureAwait(false);
                return (result ?? html, missing);
            }
            catch (Exception ex)
            {
                missing.Add("rasterize: " + ex.Message);
                return (html, missing);
            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
