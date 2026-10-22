using System.Linq;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using App.BL.AIAgent.GenericAgent.Documents;
using Newtonsoft.Json.Linq;
using Xunit;

namespace APP.BL.Tests.Documents;

public class AiPolishedRenderSpecTests
{
    [Fact]
    public void TryParse_rejects_result_rows_payload()
    {
        var json = """{"layout":"cards","rows":[{"a":1}],"bindings":{"fields":[{"name":"a","role":"body"}]}}""";
        Assert.False(AiPolishedRenderSpec.TryParse(json, out _, out var err));
        Assert.Contains("must not include result", err);
    }

    [Fact]
    public void Heuristics_pick_image_and_limit_fields()
    {
        var rows = JArray.Parse("""
            [
              {"Product_Code":"INS01","Description":"Shoe","StyleStatus":"Approved","Image1Link":"/api/resources/x/image/1","Extra1":"1","Extra2":"2","Extra3":"3","Extra4":"4","Extra5":"5","Extra6":"6"}
            ]
            """);
        var cols = ((JObject)rows[0]).Properties().Select(p => p.Name).ToList();
        var spec = AiRenderPolishHeuristics.Build("cards", "Gallery", cols, rows);
        Assert.Equal("Image1Link", spec.Bindings.PrimaryImageField);
        Assert.True(spec.Bindings.Fields.Count <= AiPolishedRenderSpec.MaxDisplayFields);
        Assert.Contains(spec.Bindings.Fields, f => f.Name == "Product_Code" && f.Role == "idLink");
        Assert.Contains(spec.Bindings.FilterFields, f => f.Control == "search");
    }

    [Fact]
    public void CardSheet_FromRows_uses_polish_field_selection()
    {
        var rows = JArray.Parse("""
            [
              {"Product_Code":"A","Description":"D","StyleStatus":"Approved","Noise":"x","Image1Link":"http://example.com/a.png"}
            ]
            """);
        var cols = ((JObject)rows[0]).Properties().Select(p => p.Name).ToList();
        var polish = AiRenderPolishHeuristics.Build("cards", "T", cols, rows);
        var sheet = CardSheet.FromRows("T", rows, 4, 4, polish);
        Assert.Single(sheet.Cards);
        var labels = sheet.Cards[0].Select(f => f.Label).ToList();
        Assert.Contains("Image1Link", labels);
        Assert.DoesNotContain("Noise", labels);
        Assert.True(sheet.Cards[0].Count <= AiPolishedRenderSpec.MaxDisplayFields + 1);
    }

    [Fact]
    public void Session_polish_cache_put_get_and_invalidate_on_dataset_put()
    {
        var session = "polish-cache-test-" + System.Guid.NewGuid().ToString("N");
        AgentDataAnalyzeCacheBL.ClearSession(session);
        var rows = JArray.Parse("""[{"Id":"1","Name":"n","Status":"Ok"}]""");
        AgentDataAnalyzeCacheBL.PutNamed(session, "ds1", rows);

        var polish = AiRenderPolishHeuristics.Build("cards", "T",
            new[] { "Id", "Name", "Status" }, rows);
        AgentAiPolishCacheBL.Put(session, "ds1", "cards", polish);
        Assert.True(AgentAiPolishCacheBL.TryGet(session, "ds1", "cards", out var got));
        Assert.NotNull(got);

        AgentDataAnalyzeCacheBL.PutNamed(session, "ds1", rows);
        Assert.False(AgentAiPolishCacheBL.TryGet(session, "ds1", "cards", out _));
    }

    [Fact]
    public void CacheUiRenderPolish_rejects_rows_via_TryParse()
    {
        Assert.False(AiPolishedRenderSpec.TryParse(
            """{"layout":"cards","data":[{"x":1}]}""", out _, out var err));
        Assert.Contains("must not include result", err);
    }
}
