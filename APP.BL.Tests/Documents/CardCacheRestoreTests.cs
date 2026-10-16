using System.Linq;
using App.BL.AIAgent.GenericAgent.DataAnalyze;
using Newtonsoft.Json.Linq;
using Xunit;

namespace APP.BL.Tests.Documents;

public class CardCacheRestoreTests
{
    [Fact]
    public void Dropped_image_column_is_restored_for_the_same_rows()
    {
        var session = "card-restore-images";
        AgentDataAnalyzeCacheBL.ClearSession(session);
        var cached = JArray.Parse("""
            [
              {"ReferenceId":"1545","Sketch":"/api/resources/Company_1/Image/a.png","Article":"MC-PD-50-J001"},
              {"ReferenceId":"1563","Sketch":"/api/resources/Company_1/Image/b.png","Article":"N/A"},
              {"ReferenceId":"1566","Sketch":"/api/resources/Company_1/Image/c.png","Article":"MC-ETP-24-079"}
            ]
            """);
        AgentDataAnalyzeCacheBL.Put(session, "style_search", cached);

        var copied = JArray.Parse("""
            [
              {"ReferenceId":"1545","Article":"MC-PD-50-J001"},
              {"ReferenceId":"1563","Article":"N/A"},
              {"ReferenceId":"1566","Article":"MC-ETP-24-079"}
            ]
            """);
        Assert.True(AgentDataAnalyzeCacheBL.TryRestoreRows(session, copied, false, out var restored, out var name));
        Assert.Equal(3, restored.Count);
        Assert.Contains("Sketch", restored[0].Children().Select(t => ((JProperty)t).Name));
        Assert.Contains("/api/resources/", restored[1]["Sketch"]?.ToString());
        Assert.False(string.IsNullOrEmpty(name));
    }

    [Fact]
    public void Short_renamed_summary_exports_the_full_cached_card_set()
    {
        var session = "card-restore-summary";
        AgentDataAnalyzeCacheBL.ClearSession(session);
        var cached = new JArray();
        for (var i = 0; i < 6; i++)
        {
            cached.Add(new JObject
            {
                ["ReferenceId"] = (1540 + i).ToString(),
                ["Sketch"] = "/api/files/image/" + (100 + i),
                ["Description"] = "Style " + i
            });
        }
        AgentDataAnalyzeCacheBL.Put(session, "style_search", cached);

        var summary = JArray.Parse("""
            [
              {"Ref":"1540","Style":"Style 0"},
              {"Ref":"1541","Style":"Style 1"}
            ]
            """);
        Assert.True(AgentDataAnalyzeCacheBL.TryRestoreRows(session, summary, true, out var restored, out _));
        Assert.Equal(2, restored.Count);
        Assert.Equal("/api/files/image/100", restored[0]["Sketch"]?.ToString());

        AgentDataAnalyzeCacheBL.RememberCardRender(session, cached, "Top 20");
        Assert.True(AgentDataAnalyzeCacheBL.TryGetCardRender(session, out var panel, out var title));
        Assert.Equal(6, panel.Count);
        Assert.Equal("Top 20", title);
        Assert.Equal(2, AgentDataAnalyzeCacheBL.TakeFirst(panel, 2).Count);
    }
}
