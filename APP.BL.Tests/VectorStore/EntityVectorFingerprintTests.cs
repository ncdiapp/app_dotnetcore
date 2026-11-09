using App.BL.AIAgent.GenericAgent.Memory;
using App.BL.AIAgent.GenericAgent.VectorStore;
using APP.Components.Dto;

namespace APP.BL.Tests.VectorStore;

public class EntityVectorFingerprintTests
{
    [Fact]
    public void Fingerprint_is_stable_across_item_order()
    {
        var a = new List<LookupItemDto>
        {
            new() { Id = 2, Display = "China" },
            new() { Id = 1, Display = "Canada" }
        };
        var b = new List<LookupItemDto>
        {
            new() { Id = 1, Display = "Canada" },
            new() { Id = 2, Display = "China" }
        };
        Assert.Equal(EntityVectorTrainingBL.Fingerprint(a), EntityVectorTrainingBL.Fingerprint(b));
    }

    [Fact]
    public void Fingerprint_changes_when_display_changes()
    {
        var a = new List<LookupItemDto> { new() { Id = 1, Display = "Canada" } };
        var b = new List<LookupItemDto> { new() { Id = 1, Display = "Kanada" } };
        Assert.NotEqual(EntityVectorTrainingBL.Fingerprint(a), EntityVectorTrainingBL.Fingerprint(b));
    }

    [Fact]
    public void ParseMetadataId_reads_string_id()
    {
        Assert.Equal("ok", AppVectorStoreBL.ParseMetadataId("{\"Id\":\"ok\"}"));
        Assert.Equal("2", AppVectorStoreBL.ParseMetadataId("{\"Id\":2}"));
        Assert.Null(AppVectorStoreBL.ParseMetadataId("not-json"));
    }

    [Fact]
    public void Exact_match_still_wins_before_vector()
    {
        var items = new List<LookupItemDto>
        {
            new() { Id = 1, Display = "Canada" },
            new() { Id = 2, Display = "China" }
        };
        Assert.Equal(2, EntityLookupSemanticResolver.ResolveExact(items, "China"));
        Assert.Equal(2, EntityLookupSemanticResolver.ResolveExact(items, "china"));
        Assert.Null(EntityLookupSemanticResolver.ResolveExact(items, "中国CN"));
    }

    [Fact]
    public void Collection_name_is_entity_prefix()
    {
        Assert.Equal("entity-1163", AppVectorStoreBL.EntityCollectionName(1163));
        Assert.Equal("1163", AppVectorStoreBL.EntitySourceKey(1163));
        Assert.Equal("1163", AppVectorStoreBL.ParseEntitySourceKeyFromName("entity-1163"));
        Assert.Equal("entity", AppVectorCollectionKind.Entity);
    }

    [Fact]
    public void Model_key_round_trips_full_model_string()
    {
        var k = VectorModelKey.FromFullModel("openai:text-embedding-3-small");
        Assert.Equal("openai", k.Provider);
        Assert.Equal("text-embedding-3-small", k.EmbeddingModel);
        Assert.Equal("openai:text-embedding-3-small", k.FullModel);
        Assert.True(EmbeddingClient.RecallThreshold(k.FullModel) < EmbeddingClient.RecallThreshold("gemini:gemini-embedding-001"));
    }
}
