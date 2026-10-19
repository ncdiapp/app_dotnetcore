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
}
