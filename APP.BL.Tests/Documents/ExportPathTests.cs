using System;
using System.Collections.Generic;
using System.Linq;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json.Linq;

namespace APP.BL.Tests.Documents;

public class ExportPathTests
{
    [Fact]
    public void Print_html_keeps_data_role_and_adds_page_css()
    {
        var html = HtmlForm.ForPrint(HtmlForm.Write(FormDocument.Parse("""
            {"title":"Sales Order","fields":[{"label":"Order No","value":"SO-1"}],"sections":[{"title":"Lines","tables":[{"columns":["Item","Qty"],"rows":[["Widget",2]]}]}]}
            """)));
        Assert.Contains("data-role=\"title\"", html);
        Assert.Contains("data-role=\"grid\"", html);
        Assert.Contains("@page{size:A4 portrait;margin:15mm}", html);
        Assert.DoesNotContain("<style>", HtmlForm.Write(FormDocument.Parse("""
            {"title":"Sales Order","fields":[{"label":"Order No","value":"SO-1"}]}
            """)));
    }

    [Fact]
    public void Card_print_writes_a_page_break_between_pages()
    {
        var rows = new JArray();
        for (var i = 0; i < 4; i++)
            rows.Add(new JObject { ["Name"] = "Row " + i });
        var sheet = CardSheet.FromRows("Cards", rows, 1, 2);
        var html = HtmlForm.WriteCards(sheet, true);
        Assert.Contains("page-break-before:always", html);
        Assert.Contains("height:121mm", html);
        Assert.Contains("mcp-card-shell", html);
        Assert.Contains("mcp-card-hero", html);
        Assert.Contains("object-fit:cover", html);
        Assert.Contains("border-radius:6px", html);
        Assert.Contains("background:#ffffff", html);
        Assert.Contains("card-page-num", html);
        Assert.DoesNotContain("(1/2)", html); // page fraction not in title
        Assert.DoesNotContain("page-break-before:always", HtmlForm.WriteCards(sheet));
    }

    [Fact]
    public void Short_content_expands_to_the_matching_cache()
    {
        var session = "export-expand-" + System.Guid.NewGuid().ToString("N");
        var full = new JArray();
        for (var i = 1; i <= 12; i++)
            full.Add(new JObject { ["Order"] = "SO-" + i, ["City"] = "Boston" });
        AgentDataAnalyzeCacheBL.PutNamed(session, "orders", full, "test");
        var shortRows = new JArray
        {
            new JObject { ["Order"] = "SO-1" },
            new JObject { ["Order"] = "SO-2" }
        };
        var expanded = CacheRowExpand.Expand(session, shortRows);
        Assert.Equal(12, expanded.Count);
        Assert.Equal("Boston", expanded[11]["City"]?.ToString());
    }

    [Fact]
    public void Two_matching_caches_do_not_write()
    {
        var session = "export-expand-" + System.Guid.NewGuid().ToString("N");
        var a = new JArray { new JObject { ["Order"] = "SO-1" }, new JObject { ["Order"] = "SO-2" }, new JObject { ["Order"] = "SO-3" } };
        var b = new JArray { new JObject { ["Order"] = "SO-1", ["Extra"] = "x" }, new JObject { ["Order"] = "SO-2" }, new JObject { ["Order"] = "SO-9" } };
        AgentDataAnalyzeCacheBL.PutNamed(session, "one", a, "test");
        AgentDataAnalyzeCacheBL.PutNamed(session, "two", b, "test");
        var inline = new JArray { new JObject { ["Order"] = "SO-1" } };
        var ex = Assert.Throws<System.InvalidOperationException>(() => CacheRowExpand.Expand(session, inline));
        Assert.Contains("dataset_name", ex.Message);
    }

    [Fact]
    public void Title_paint_reads_the_template_font_size()
    {
        var html = "<td data-role=\"title\" style=\"font-size:18pt;font-weight:bold;color:#1a1a1a;\">Sales Order</td>";
        var paint = TemplatePaint.FromHtml(html);
        Assert.Equal(18, paint.TitlePt);
        Assert.True(paint.TitleBold);
    }

    [Fact]
    public void OpenXml_word_cards_keep_a_picture_without_libreoffice()
    {
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var sheet = CardSheet.FromRows("Style", new JArray { new JObject { ["Sketch"] = "pic", ["Name"] = "Jacket" } }, 2, 1);
        sheet.Cards[0][0].ImageBytes = png;
        var bytes = OpenXmlWordForm.WriteCards(sheet);
        using var ms = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
        Assert.Contains(zip.Entries, e => e.FullName == "word/document.xml");
        Assert.Contains(zip.Entries, e => e.FullName.Contains("media/"));
        var four = CardSheet.FromRows("Style", new JArray
        {
            new JObject { ["Name"] = "A" },
            new JObject { ["Name"] = "B" },
            new JObject { ["Name"] = "C" },
            new JObject { ["Name"] = "D" }
        }, 2, 1);
        var paged = OpenXmlWordForm.WriteCards(four);
        using var pagedMs = new System.IO.MemoryStream(paged);
        using var pagedZip = new System.IO.Compression.ZipArchive(pagedMs, System.IO.Compression.ZipArchiveMode.Read);
        using var reader = new System.IO.StreamReader(pagedZip.GetEntry("word/document.xml")!.Open());
        var xml = reader.ReadToEnd();
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(xml, "w:type=\"page\"").Count);
        Assert.Contains("w:hRule=\"exact\"", xml);

        var grid = CardSheet.FromRows("Style", new JArray
        {
            new JObject { ["Name"] = "A" },
            new JObject { ["Name"] = "B" },
            new JObject { ["Name"] = "C" },
            new JObject { ["Name"] = "D" }
        }, 2, 1);
        grid.ApplyPageGrid("2x2", 0);
        Assert.Equal(2, grid.CardsPerRow);
        Assert.Equal(2, grid.CardRows);
        var packed = OpenXmlWordForm.WriteCards(grid);
        using var packedMs = new System.IO.MemoryStream(packed);
        using var packedZip = new System.IO.Compression.ZipArchive(packedMs, System.IO.Compression.ZipArchiveMode.Read);
        using var packedReader = new System.IO.StreamReader(packedZip.GetEntry("word/document.xml")!.Open());
        var packedXml = packedReader.ReadToEnd();
        Assert.Equal(0, System.Text.RegularExpressions.Regex.Matches(packedXml, "w:type=\"page\"").Count);
        var html = HtmlForm.WriteCards(grid, true);
        Assert.DoesNotContain("page-break-before:always", html);
        Assert.Contains("class=\"card-page\"", html);
    }

    [Fact]
    public void Page_export_html_stretches_cards_to_full_width()
    {
        var rows = new JArray();
        for (var i = 0; i < 3; i++)
            rows.Add(new JObject { ["Name"] = "Item " + i, ["WideCol"] = new string('x', 80) });
        var sheet = CardSheet.FromRows("Styles", rows, 2, 2);
        sheet.ApplyPageGrid("2x2", 0);
        var doc = sheet.AsFlatTable();
        var html = UiTemplateMerge.ComposePageExportHtml(doc, sheet, "Styles");
        Assert.Contains("id=\"export-grid\"", html);
        Assert.Contains("id=\"export-cards\"", html);
        Assert.Contains("#export-cards .card-page{width:100%!important", html);
        Assert.Contains("Grid view", html);
        Assert.Contains("Card view", html);
        Assert.Contains("data-cols=\"2\"", html);
        Assert.Contains(".data-table[data-cols=\"2\"]", html);
        Assert.Contains("table-layout:fixed!important", html);
        // Must not inject global tr{page-break-inside:avoid} (blank first page).
        Assert.DoesNotContain("thead{display:table-header-group}tr{page-break-inside:avoid}", html);
    }

    [Fact]
    public void FormDocument_ProjectColumns_keeps_order()
    {
        var doc = FormDocument.FromTable("T", new[] { "A", "B", "C", "D" }, new List<List<FormCell>>
        {
            new() { new FormCell { Text = "1" }, new FormCell { Text = "2" }, new FormCell { Text = "3" }, new FormCell { Text = "4" } }
        });
        var projected = doc.ProjectColumns(new[] { "C", "A" });
        Assert.Equal(2, projected.FirstGridColumnCount());
        var cols = projected.Sections[0].Tables[0].Columns;
        Assert.Equal(new[] { "C", "A" }, cols);
    }

    [Fact]
    public void Pptx_cards_honor_page_grid_slides()
    {
        var rows = new JArray();
        for (var i = 0; i < 5; i++)
            rows.Add(new JObject { ["Name"] = "Item " + i, ["Status"] = "Ok" });
        var sheet = CardSheet.FromRows("Styles", rows, 0, 0);
        sheet.ApplyPageGrid("2x2", 0);
        Assert.Equal(2, sheet.CardsPerRow);
        Assert.Equal(2, sheet.CardRows);
        Assert.Equal(4, sheet.PageSize);

        var bytes = PptxForm.WriteCards(sheet);
        using var ms = new System.IO.MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
        var slideCount = zip.Entries.Count(e =>
            e.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase)
            && e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        // 5 cards @ 2×2 → 2 slides (4 + 1)
        Assert.Equal(2, slideCount);
    }
}
