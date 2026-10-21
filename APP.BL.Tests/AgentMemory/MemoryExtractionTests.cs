using App.BL.AIAgent.GenericAgent.Memory;
using Newtonsoft.Json.Linq;

namespace APP.BL.Tests.AgentMemory;

public class MemoryExtractionParserTests
{
    private static readonly string[] UserTexts =
    {
        "Please always show dates as yyyy-MM-dd in reports.",
        "Remember that my ERP is BlueCherry UDEV."
    };

    private static string Json(string facts, string episode = @"{""summary"":""Built a report."",""topics"":[""report""],""worth_keeping"":true}") =>
        $@"{{""episode"":{episode},""facts"":{facts}}}";

    [Fact]
    public void Accepts_a_fact_whose_evidence_is_quoted_from_a_user_message()
    {
        var r = MemoryExtractionParser.Parse(Json(
            @"[{""key"":""Date Format"",""value"":""yyyy-MM-dd"",""category"":""preference"",""evidence"":""always show dates as yyyy-MM-dd"",""confidence"":0.9}]"),
            UserTexts);

        Assert.True(r.ParsedOk);
        var f = Assert.Single(r.Facts);
        Assert.Equal("date_format", f.Key);
        Assert.Equal("preference", f.Category);
        Assert.False(f.Explicit);
        Assert.Equal("Built a report.", r.EpisodeSummary);
        Assert.Equal("report", r.Topics);
    }

    [Fact]
    public void Evidence_match_ignores_case_whitespace_and_surrounding_quotes()
    {
        var r = MemoryExtractionParser.Parse(Json(
            @"[{""key"":""erp"",""value"":""BlueCherry UDEV"",""category"":""entity"",""evidence"":""\""REMEMBER  that my ERP is BlueCherry UDEV.\"""",""confidence"":0.8}]"),
            UserTexts);

        var f = Assert.Single(r.Facts);
        Assert.True(f.Explicit);
    }

    [Fact]
    public void Drops_a_fact_whose_evidence_is_not_in_a_user_message()
    {
        var r = MemoryExtractionParser.Parse(Json(
            @"[{""key"":""x"",""value"":""y"",""category"":""note"",""evidence"":""the assistant said so"",""confidence"":0.9}]"), UserTexts);

        Assert.Empty(r.Facts);
        Assert.Contains(r.Dropped, d => d.Contains("evidence not found"));
    }

    [Fact]
    public void Drops_secrets_low_confidence_and_missing_evidence()
    {
        var r = MemoryExtractionParser.Parse(Json(
            @"[{""key"":""k1"",""value"":""sk-abcdefghijklmnopqrstuv"",""evidence"":""Remember that my ERP is BlueCherry UDEV."",""confidence"":0.9},
               {""key"":""k2"",""value"":""v"",""evidence"":""Remember that my ERP is BlueCherry UDEV."",""confidence"":0.2},
               {""key"":""k3"",""value"":""v"",""evidence"":"""",""confidence"":0.9}]"), UserTexts);

        Assert.Empty(r.Facts);
        Assert.Equal(3, r.Dropped.Count(d => d.StartsWith("fact")));
    }

    [Fact]
    public void Strips_code_fences_and_surrounding_prose()
    {
        var r = MemoryExtractionParser.Parse("Here you go:\n```json\n" + Json("[]") + "\n```", UserTexts);

        Assert.True(r.ParsedOk);
        Assert.Equal("Built a report.", r.EpisodeSummary);
    }

    [Fact]
    public void Invalid_json_is_reported_not_thrown()
    {
        var r = MemoryExtractionParser.Parse("not json at all", UserTexts);

        Assert.False(r.ParsedOk);
        Assert.NotEmpty(r.Dropped);
    }

    [Fact]
    public void Episode_marked_not_worth_keeping_or_containing_a_secret_is_dropped()
    {
        var notWorth = MemoryExtractionParser.Parse(Json("[]", @"{""summary"":""hi"",""worth_keeping"":false}"), UserTexts);
        var secret = MemoryExtractionParser.Parse(Json("[]", @"{""summary"":""The password is hunter2222 for the db."",""worth_keeping"":true}"), UserTexts);

        Assert.Equal("", notWorth.EpisodeSummary);
        Assert.Equal("", secret.EpisodeSummary);
    }

    [Fact]
    public void Prompt_includes_known_facts_and_keeps_the_most_recent_turns_within_the_cap()
    {
        var turns = Enumerable.Range(0, 400).Select(i => ("user", $"turn-{i} " + new string('a', 200))).ToList();
        var prompt = MemoryExtractionParser.BuildPrompt(turns,
            new[] { new AgentMemoryFactDto { FactKey = "date_format", FactValue = "yyyy-MM-dd" } });

        Assert.Contains("- date_format: yyyy-MM-dd", prompt);
        Assert.Contains("turn-399 ", prompt);
        Assert.DoesNotContain("turn-0 ", prompt);
    }
}

public class MemorySecretScannerTests
{
    [Theory]
    [InlineData("Bearer abcdefghijklmnop1234")]
    [InlineData("my api key is sk-abcdefghijklmnopqrstuvwx")]
    [InlineData("password: Hunter2222")]
    [InlineData("-----BEGIN RSA PRIVATE KEY-----")]
    [InlineData("Server=db;Database=x;User Id=sa;Password=abc123;")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9abcdef1234567890ABCDEF12")]
    public void Flags_credential_like_text(string text) => Assert.True(MemorySecretScanner.ContainsSecret(text));

    [Theory]
    [InlineData("Dates as yyyy-MM-dd")]
    [InlineData("ERP is BlueCherry UDEV")]
    [InlineData("Prefers short answers")]
    [InlineData("")]
    public void Allows_normal_facts(string text) => Assert.False(MemorySecretScanner.ContainsSecret(text));
}

public class AgentMemoryFactFormattingTests
{
    [Theory]
    [InlineData("Date Format!", "date_format")]
    [InlineData("  ERP--name ", "erp_name")]
    [InlineData("", "")]
    public void NormalizeKey_makes_snake_case(string input, string expected) =>
        Assert.Equal(expected, AgentMemoryFactBL.NormalizeKey(input));

    [Theory]
    [InlineData("Rule", "rule")]
    [InlineData("unknown", "note")]
    [InlineData(null, "note")]
    public void NormalizeCategory_falls_back_to_note(string? input, string expected) =>
        Assert.Equal(expected, AgentMemoryFactBL.NormalizeCategory(input!));

    [Fact]
    public void Prompt_block_orders_rules_first_and_respects_the_cap()
    {
        var facts = new List<AgentMemoryFactDto>
        {
            new() { FactKey = "a_note", FactValue = "n", Category = "note", UpdatedAt = DateTime.UtcNow },
            new() { FactKey = "a_rule", FactValue = "r", Category = "rule", UpdatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { FactKey = "a_pref", FactValue = "p", Category = "preference", UpdatedAt = DateTime.UtcNow }
        };

        var block = AgentMemoryFactBL.FormatPromptBlock(facts, 1500);
        Assert.True(block.IndexOf("a_rule") < block.IndexOf("a_pref"));
        Assert.True(block.IndexOf("a_pref") < block.IndexOf("a_note"));

        var tiny = AgentMemoryFactBL.FormatPromptBlock(facts, 10);
        Assert.Equal("", tiny);
        Assert.Equal("", AgentMemoryFactBL.FormatPromptBlock(new List<AgentMemoryFactDto>(), 1500));
    }
}

public class EmbeddingMathTests
{
    [Fact]
    public void Bytes_round_trip_and_cosine_of_identical_unit_vectors_is_one()
    {
        var v = new[] { 0.6f, 0.8f, 0f };
        var back = EmbeddingClient.FromBytes(EmbeddingClient.ToBytes(v));

        Assert.Equal(v, back);
        Assert.Equal(1.0, EmbeddingClient.Cosine(v, back), 5);
        Assert.Equal(0.0, EmbeddingClient.Cosine(v, new[] { 0f, 0f, 1f }), 5);
        Assert.Equal(0.0, EmbeddingClient.Cosine(v, new[] { 1f }), 5);
    }

    [Fact]
    public void Recall_threshold_is_lower_for_openai_vectors()
    {
        Assert.True(EmbeddingClient.RecallThreshold("openai:text-embedding-3-small") < EmbeddingClient.RecallThreshold("gemini:x"));
    }
}

public class AgentMemoryInjectorQueryTests
{
    private static List<JObject> History(string assistant) => new()
    {
        new JObject { ["role"] = "user", ["content"] = "q" },
        new JObject { ["role"] = "assistant", ["content"] = assistant }
    };

    [Theory]
    [InlineData("hi")]
    [InlineData("thanks")]
    [InlineData("[session_start]")]
    [InlineData("ok.")]
    public void Trivial_messages_do_not_trigger_recall(string msg) =>
        Assert.Null(AgentMemoryInjector.BuildQuery(msg, History("earlier answer")));

    [Fact]
    public void Long_message_is_used_as_is_and_short_follow_up_gets_the_previous_reply()
    {
        var longMsg = "Publish the available PLM styles for season SS27 to the ERP";
        Assert.Equal(longMsg, AgentMemoryInjector.BuildQuery(longMsg, History("irrelevant")));

        var followUp = AgentMemoryInjector.BuildQuery("do that again please", History("I exported the style list."));
        Assert.Contains("I exported the style list.", followUp);
        Assert.EndsWith("do that again please", followUp);
    }
}
