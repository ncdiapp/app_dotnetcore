using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using App.BL.AIAgent.GenericAgent.Documents;
using App.BL.AIAgent.GenericAgent.Plugins;
using GemBox.Document;
using Newtonsoft.Json.Linq;

namespace APP.BL.Tests.Documents;

public class FormDocumentTests
{
    private const string Sample = """
        {
          "title": "Sales Order",
          "fields": [
            { "label": "Order No", "value": "SO-1" },
            { "label": "Customer", "value": "Acme, \"North\"" }
          ],
          "sections": [
            {
              "title": "Lines",
              "tables": [
                { "columns": ["Item", "Qty"], "rows": [["Widget", 2], ["A, B", 3]] }
              ],
              "sections": [
                { "title": "Ship", "fields": [{ "label": "City", "value": "Boston" }] }
              ]
            }
          ],
          "notes": "Ship this week."
        }
        """;

    public FormDocumentTests()
    {
        ComponentInfo.SetLicense("FREE-LIMITED-KEY");
        GemBox.Spreadsheet.SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
    }

    [Fact]
    public void Parse_assigns_levels_and_numeric_cells()
    {
        var doc = FormDocument.Parse(Sample);
        Assert.Equal("Sales Order", doc.Title);
        Assert.Equal(2, doc.Fields.Count);
        Assert.Equal(2, doc.Sections[0].Level);
        Assert.Equal(3, doc.Sections[0].Sections[0].Level);
        Assert.True(doc.Sections[0].Tables[0].Rows[0][1].IsNumber);
        Assert.Equal(2, doc.DataRowCount());
    }

    [Fact]
    public void Parse_rejects_50001_rows()
    {
        var rows = string.Join(",", Enumerable.Range(0, 50001).Select(i => "[" + i + "]"));
        var json = "{\"columns\":[\"N\"],\"rows\":[" + rows + "]}";
        var ex = Assert.Throws<InvalidOperationException>(() => FormDocument.Parse(json));
        Assert.Contains("50000", ex.Message);
    }

    [Fact]
    public void Html_is_tables_and_inline_styles_and_round_trips()
    {
        var html = HtmlForm.Write(FormDocument.Parse(Sample));
        Assert.DoesNotContain("display:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("flex", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("font-size:18pt", html);
        Assert.Contains("text-align:center", html);
        Assert.Contains("<tr>", html);
        var back = HtmlForm.Read(html);
        Assert.Equal("Sales Order", back.Title);
        Assert.Equal("SO-1", back.Fields[0].Value);
        Assert.Equal("Acme, \"North\"", back.Fields[1].Value);
        Assert.Equal("Lines", back.Sections[0].Title);
        Assert.Equal("Widget", back.Sections[0].Tables[0].Rows[0][0].Text);
        Assert.True(back.Sections[0].Tables[0].Rows[0][1].IsNumber);
        Assert.Equal("Boston", back.Sections[0].Sections[0].Fields[0].Value);
        Assert.Equal("Ship this week.", back.Notes);
    }

    [Fact]
    public void Csv_round_trips_commas_and_quotes()
    {
        var bytes = CsvForm.Write(FormDocument.Parse(Sample));
        Assert.Equal(0xEF, bytes[0]);
        var back = CsvForm.Read(bytes);
        Assert.Equal("Sales Order", back.Title);
        Assert.Equal("Acme, \"North\"", back.Fields[1].Value);
        Assert.Equal("A, B", back.Sections[0].Tables[0].Rows[1][0].Text);
        Assert.Equal("Ship this week.", back.Notes);
    }

    [Fact]
    public void Excel_round_trips_title_pairs_and_grid()
    {
        var bytes = ExcelForm.Write(FormDocument.Parse(Sample));
        var back = ExcelForm.Read(bytes);
        Assert.Equal("Sales Order", back.Title);
        Assert.Equal("SO-1", back.Fields[0].Value);
        Assert.Equal("Acme, \"North\"", back.Fields[1].Value);
        Assert.Equal("Lines", back.Sections[0].Title);
        Assert.Equal("Widget", back.Sections[0].Tables[0].Rows[0][0].Text);
        Assert.True(back.Sections[0].Tables[0].Rows[0][1].IsNumber);
        Assert.Equal("Ship this week.", back.Notes);
    }

    [Fact]
    public void Word_round_trips_and_pdf_has_title()
    {
        var doc = FormDocument.Parse("""
            {"title":"Sales Order","fields":[{"label":"Order No","value":"SO-1"}],"sections":[{"title":"Lines","tables":[{"columns":["Item","Qty"],"rows":[["Widget",2]]}]}]}
            """);
        var back = WordForm.ReadDocx(WordForm.WriteDocx(doc));
        Assert.Equal("Sales Order", back.Title);
        Assert.Equal("SO-1", back.Fields[0].Value);
        Assert.Equal("Widget", back.Sections[0].Tables[0].Rows[0][0].Text);
        var pdf = WordForm.ReadPdf(WordForm.WritePdf(doc));
        Assert.Contains("Sales Order", pdf.Text);
        Assert.True(pdf.PageCount >= 1);
    }

    [Fact]
    public void Pptx_round_trips_title_and_grid()
    {
        var bytes = PptxForm.Write(FormDocument.Parse(Sample));
        var back = PptxForm.Read(bytes);
        Assert.Equal("Sales Order", back.Title);
        Assert.Contains(back.Sections, s => s.Tables.Any(t => t.Rows.Any(r => r.Any(c => c.Text == "Widget"))));
    }

    [Fact]
    public void StampExportPath_puts_time_before_extension()
    {
        var when = new DateTime(2026, 10, 16, 9, 34, 5);
        Assert.Equal("hello_world_20261016_093405.docx", AgentDocumentPlugin.StampExportPath("hello_world.docx", when));
        Assert.Equal("out/hello_world_20261016_093405.pdf", AgentDocumentPlugin.StampExportPath("out/hello_world.pdf", when));
        Assert.Equal("output/hello_world.docx", AgentDocumentPlugin.PlaceInOutputFolder("hello_world.docx"));
        Assert.Equal("output/hello_world.docx", AgentDocumentPlugin.PlaceInOutputFolder("output/hello_world.docx"));
        Assert.Equal("reports/hello_world.docx", AgentDocumentPlugin.PlaceInOutputFolder("reports/hello_world.docx"));
    }

    [Fact]
    public void Cards_wrap_html_by_cards_per_row_and_keep_images()
    {
        var rows = JArray.Parse("[{\"Name\":\"Widget\",\"Photo\":\"https://cdn.test/a.png\",\"Division_186\":\"DoD;01\"},{\"Name\":\"Bolt\",\"Photo\":\"n/a\"}]");
        var sheet = CardSheet.FromRows("Items", rows, 9, 0);
        Assert.Equal(6, sheet.CardsPerRow);
        Assert.Equal(3, sheet.CardRows);
        // Heuristic polish: Photo is primaryImage when URL is displayable; "n/a" stays blank (no broken img).
        Assert.Contains(sheet.Cards[0], f => f.IsImage && f.Label == "Photo");
        Assert.DoesNotContain(sheet.Cards[1], f => f.IsImage);
        sheet.CardsPerRow = 2;
        var html = HtmlForm.WriteCards(sheet);
        Assert.Contains("data-doc=\"cards\"", html);
        Assert.Contains("<img ", html);
        Assert.Contains("min-height:120px", html);
        Assert.Contains("Widget", html);
        Assert.DoesNotContain("display:none", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Card_picture_bytes_are_embedded_in_office_files()
    {
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var sheet = CardSheet.FromRows("Item", JArray.Parse("[{\"Photo\":\"12\",\"Name\":\"Widget\"}]"), 1, 1);
        sheet.Cards[0][0].ImageBytes = png;
        var html = HtmlForm.WriteCards(sheet);
        Assert.Contains("data:image/png;base64,", html);
        var wrapped = CardSheet.FromRows("Item", JArray.Parse("[{\"Note\":\"line1\\nline2\\tmore\"}]"), 1, 1);
        Assert.True(WordForm.WriteCardPdf(wrapped).Length > 100);
        Assert.Contains("media", ZipNames(WordForm.WriteCardDocx(sheet)));
        Assert.True(WordForm.WriteCardPdf(sheet).Length > 100);
        Assert.Contains("media", ZipNames(ExcelForm.WriteCards(sheet)));
        var pptx = PptxForm.WriteCards(sheet);
        using (var ms = new MemoryStream(pptx))
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
        {
            var slide = zip.GetEntry("ppt/slides/slide1.xml");
            using var reader = new StreamReader(slide!.Open());
            var xml = reader.ReadToEnd();
            var picAt = xml.IndexOf("<p:pic", StringComparison.Ordinal);
            Assert.True(picAt > 0);
            var picY = OffY(xml, picAt);
            var textAt = xml.IndexOf("name=\"Text Box\"", picAt, StringComparison.Ordinal);
            Assert.True(textAt > picAt);
            Assert.True(picY < OffY(xml, textAt));
        }
    }

    private static long OffY(string xml, int from)
    {
        var off = xml.IndexOf("<a:off ", from, StringComparison.Ordinal);
        var y = xml.IndexOf("y=\"", off, StringComparison.Ordinal) + 3;
        var end = xml.IndexOf('"', y);
        return long.Parse(xml.Substring(y, end - y));
    }

    private static string ZipNames(byte[] zip)
    {
        using var ms = new MemoryStream(zip);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        return string.Join("\n", archive.Entries.Select(e => e.FullName));
    }
}
