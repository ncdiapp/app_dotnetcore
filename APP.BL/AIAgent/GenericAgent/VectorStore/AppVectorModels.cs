using System;

namespace App.BL.AIAgent.GenericAgent.VectorStore
{
    public static class AppVectorCollectionKind
    {
        public const string Entity = "entity";
        public const string Help = "help";
        public const string User = "user";
        public const string Rag = "rag";
        public const string Image = "image";
    }

    public sealed class AppVectorCollectionRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Kind { get; set; } = AppVectorCollectionKind.Entity;
        public string SourceKey { get; set; }
        public int Dimension { get; set; }
        public string ModelProvider { get; set; } = "";
        public string EmbeddingModel { get; set; } = "";
        public string ContentFingerprint { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class AppVectorEmbeddingRow
    {
        public int Id { get; set; }
        public int CollectionId { get; set; }
        public string Document { get; set; } = "";
        public float[] Vector { get; set; } = Array.Empty<float>();
        public int Dimension { get; set; }
        public string Metadata { get; set; }
        public string ModelProvider { get; set; } = "";
        public string EmbeddingModel { get; set; } = "";
    }

    public sealed class AppVectorSearchHit
    {
        public string Document { get; set; } = "";
        public string Metadata { get; set; }
        public double Score { get; set; }
        public string IdFromMetadata { get; set; }
    }

    public sealed class VectorModelKey : IEquatable<VectorModelKey>
    {
        public string Provider { get; }
        public string EmbeddingModel { get; }
        /// <summary>Same form as EmbeddingClient: "provider:model".</summary>
        public string FullModel { get; }

        public VectorModelKey(string provider, string embeddingModel)
        {
            Provider = (provider ?? "").Trim().ToLowerInvariant();
            EmbeddingModel = (embeddingModel ?? "").Trim();
            FullModel = string.IsNullOrEmpty(Provider)
                ? EmbeddingModel
                : Provider + ":" + EmbeddingModel;
        }

        public static VectorModelKey FromFullModel(string fullModel)
        {
            var s = (fullModel ?? "").Trim();
            var i = s.IndexOf(':');
            if (i <= 0) return new VectorModelKey("", s);
            return new VectorModelKey(s.Substring(0, i), s.Substring(i + 1));
        }

        public bool Equals(VectorModelKey other) =>
            other != null
            && string.Equals(Provider, other.Provider, StringComparison.Ordinal)
            && string.Equals(EmbeddingModel, other.EmbeddingModel, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => Equals(obj as VectorModelKey);

        public override int GetHashCode() =>
            HashCode.Combine(Provider, EmbeddingModel?.ToLowerInvariant());
    }
}
