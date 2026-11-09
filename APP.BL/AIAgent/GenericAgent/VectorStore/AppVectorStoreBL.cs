using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using App.BL.AIAgent.GenericAgent.Memory;
using Newtonsoft.Json.Linq;

namespace App.BL.AIAgent.GenericAgent.VectorStore
{
    /// <summary>
    /// Persist vectors as VARBINARY; search in-process (cosine). SQL 2025 native VECTOR_DISTANCE is a later acceleration.
    /// </summary>
    public static class AppVectorStoreBL
    {
        private static readonly ConcurrentDictionary<string, List<AppVectorEmbeddingRow>> Cache = new();

        public static string EntityCollectionName(int entityInfoId) => "entity-" + entityInfoId;

        public static string EntitySourceKey(int entityInfoId) => entityInfoId.ToString(CultureInfo.InvariantCulture);

        public static string ParseEntitySourceKeyFromName(string name)
        {
            var n = (name ?? "").Trim();
            if (n.StartsWith("entity-", StringComparison.OrdinalIgnoreCase) && n.Length > 7)
                return n.Substring(7);
            return n;
        }

        public static AppVectorCollectionRow GetCollection(int dataSourceId, string name, VectorModelKey model)
        {
            if (model == null || string.IsNullOrWhiteSpace(name)) return null;
            try
            {
                var fixture = AppVectorDb.Fixture(dataSourceId);
                if (fixture == null) return null;
                var dt = fixture.RetriveDataTable(
                    @"SELECT TOP 1 Id, Name, Kind, SourceKey, Dimension, ModelProvider, EmbeddingModel, ContentFingerprint, UpdatedAt, CreatedAt
                      FROM dbo.AppVectorCollection
                      WHERE ModelProvider=@P AND EmbeddingModel=@M
                        AND (
                            (Kind=@K AND SourceKey=@S)
                            OR Name=@N
                        )",
                    AppVectorDb.Params(fixture,
                        ("@N", name), ("@K", AppVectorCollectionKind.Entity),
                        ("@S", ParseEntitySourceKeyFromName(name)),
                        ("@P", model.Provider), ("@M", model.EmbeddingModel)));
                if (dt == null || dt.Rows.Count == 0) return null;
                return MapCollection(dt.Rows[0]);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(GetCollection));
                return null;
            }
        }

        public static List<AppVectorCollectionRow> ListEntityCollections(int dataSourceId, VectorModelKey model)
        {
            var list = new List<AppVectorCollectionRow>();
            if (model == null) return list;
            try
            {
                var fixture = AppVectorDb.Fixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    @"SELECT Id, Name, Kind, SourceKey, Dimension, ModelProvider, EmbeddingModel, ContentFingerprint, UpdatedAt, CreatedAt
                      FROM dbo.AppVectorCollection
                      WHERE ModelProvider=@P AND EmbeddingModel=@M AND Kind=@K",
                    AppVectorDb.Params(fixture,
                        ("@P", model.Provider), ("@M", model.EmbeddingModel),
                        ("@K", AppVectorCollectionKind.Entity)));
                if (dt == null) return list;
                foreach (DataRow row in dt.Rows)
                    list.Add(MapCollection(row));
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(ListEntityCollections));
            }
            return list;
        }

        public static int ReplaceCollection(
            int dataSourceId,
            string name,
            VectorModelKey model,
            int dimension,
            string fingerprint,
            IReadOnlyList<(string Document, float[] Vector, string Metadata)> rows)
        {
            if (model == null || string.IsNullOrWhiteSpace(name) || rows == null) return 0;
            try
            {
                var fixture = AppVectorDb.Fixture(dataSourceId);
                if (fixture == null) return 0;
                var existing = GetCollection(dataSourceId, name, model);
                int collectionId;
                if (existing == null)
                {
                    fixture.ExecuteNonQueryResult(
                        @"INSERT INTO dbo.AppVectorCollection (Name, Kind, SourceKey, Dimension, ModelProvider, EmbeddingModel, ContentFingerprint, UpdatedAt, CreatedAt)
                          VALUES (@N, @K, @S, @D, @P, @M, @F, GETUTCDATE(), GETUTCDATE())",
                        AppVectorDb.Params(fixture,
                            ("@N", name),
                            ("@K", AppVectorCollectionKind.Entity),
                            ("@S", ParseEntitySourceKeyFromName(name)),
                            ("@D", dimension), ("@P", model.Provider),
                            ("@M", model.EmbeddingModel), ("@F", fingerprint)));
                    existing = GetCollection(dataSourceId, name, model);
                    if (existing == null) return 0;
                    collectionId = existing.Id;
                }
                else
                {
                    collectionId = existing.Id;
                    fixture.ExecuteNonQueryResult(
                        @"UPDATE dbo.AppVectorCollection
                          SET Dimension=@D, ContentFingerprint=@F, Kind=@K, SourceKey=@S, UpdatedAt=GETUTCDATE()
                          WHERE Id=@Id",
                        AppVectorDb.Params(fixture,
                            ("@D", dimension), ("@F", fingerprint),
                            ("@K", AppVectorCollectionKind.Entity),
                            ("@S", ParseEntitySourceKeyFromName(name)),
                            ("@Id", collectionId)));
                    fixture.ExecuteNonQueryResult(
                        "DELETE FROM dbo.AppVectorEmbedding WHERE CollectionId=@Id",
                        AppVectorDb.Params(fixture, ("@Id", collectionId)));
                }

                foreach (var row in rows)
                {
                    if (row.Vector == null || row.Vector.Length == 0) continue;
                    fixture.ExecuteNonQueryResult(
                        @"INSERT INTO dbo.AppVectorEmbedding
                            (CollectionId, Document, VectorData, Dimension, Metadata, ModelProvider, EmbeddingModel, CreatedAt)
                          VALUES (@C, @Doc, CONVERT(VARBINARY(MAX), @V), @Dim, @Meta, @P, @M, GETUTCDATE())",
                        AppVectorDb.Params(fixture,
                            ("@C", collectionId),
                            ("@Doc", row.Document ?? ""),
                            ("@V", EmbeddingClient.ToBytes(row.Vector)),
                            ("@Dim", row.Vector.Length),
                            ("@Meta", row.Metadata),
                            ("@P", model.Provider),
                            ("@M", model.EmbeddingModel)));
                }
                InvalidateCache(dataSourceId, collectionId);
                return collectionId;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(ReplaceCollection));
                return 0;
            }
        }

        public static AppVectorSearchHit SearchTop1(int dataSourceId, string name, VectorModelKey model, float[] query)
        {
            if (query == null || query.Length == 0 || model == null) return null;
            var collection = GetCollection(dataSourceId, name, model);
            if (collection == null) return null;
            var rows = LoadEmbeddings(dataSourceId, collection.Id);
            AppVectorEmbeddingRow best = null;
            var bestScore = double.MinValue;
            foreach (var row in rows)
            {
                if (row.Vector == null || row.Vector.Length != query.Length) continue;
                if (!string.Equals(row.ModelProvider, model.Provider, StringComparison.Ordinal)) continue;
                if (!string.Equals(row.EmbeddingModel, model.EmbeddingModel, StringComparison.OrdinalIgnoreCase)) continue;
                var score = EmbeddingClient.Cosine(query, row.Vector);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = row;
                }
            }
            if (best == null) return null;
            return new AppVectorSearchHit
            {
                Document = best.Document,
                Metadata = best.Metadata,
                Score = bestScore,
                IdFromMetadata = ParseMetadataId(best.Metadata)
            };
        }

        public static string ParseMetadataId(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata)) return null;
            try
            {
                var obj = JObject.Parse(metadata);
                var token = obj["Id"] ?? obj["id"];
                return token == null || token.Type == JTokenType.Null
                    ? null
                    : Convert.ToString(token, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static List<AppVectorEmbeddingRow> LoadEmbeddings(int dataSourceId, int collectionId)
        {
            var key = CacheKey(dataSourceId, collectionId);
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var list = new List<AppVectorEmbeddingRow>();
            try
            {
                var fixture = AppVectorDb.Fixture(dataSourceId);
                if (fixture == null) return list;
                var dt = fixture.RetriveDataTable(
                    @"SELECT Id, CollectionId, Document, VectorData, Dimension, Metadata, ModelProvider, EmbeddingModel
                      FROM dbo.AppVectorEmbedding WHERE CollectionId=@C",
                    AppVectorDb.Params(fixture, ("@C", collectionId)));
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        list.Add(new AppVectorEmbeddingRow
                        {
                            Id = Convert.ToInt32(row["Id"]),
                            CollectionId = Convert.ToInt32(row["CollectionId"]),
                            Document = row["Document"]?.ToString() ?? "",
                            Vector = row["VectorData"] is byte[] b ? EmbeddingClient.FromBytes(b) : Array.Empty<float>(),
                            Dimension = Convert.ToInt32(row["Dimension"]),
                            Metadata = row["Metadata"] == DBNull.Value ? null : row["Metadata"].ToString(),
                            ModelProvider = row["ModelProvider"]?.ToString() ?? "",
                            EmbeddingModel = row["EmbeddingModel"]?.ToString() ?? ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(LoadEmbeddings));
            }
            Cache[key] = list;
            return list;
        }

        private static void InvalidateCache(int dataSourceId, int collectionId) =>
            Cache.TryRemove(CacheKey(dataSourceId, collectionId), out _);

        private static string CacheKey(int dataSourceId, int collectionId) => dataSourceId + ":" + collectionId;

        private static AppVectorCollectionRow MapCollection(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            Name = row["Name"]?.ToString() ?? "",
            Kind = row.Table.Columns.Contains("Kind") && row["Kind"] != DBNull.Value ? row["Kind"].ToString() : AppVectorCollectionKind.Entity,
            SourceKey = row.Table.Columns.Contains("SourceKey") && row["SourceKey"] != DBNull.Value ? row["SourceKey"].ToString() : null,
            Dimension = Convert.ToInt32(row["Dimension"]),
            ModelProvider = row["ModelProvider"]?.ToString() ?? "",
            EmbeddingModel = row["EmbeddingModel"]?.ToString() ?? "",
            ContentFingerprint = row["ContentFingerprint"] == DBNull.Value ? null : row["ContentFingerprint"].ToString(),
            UpdatedAt = row["UpdatedAt"] is DateTime u ? u : default,
            CreatedAt = row["CreatedAt"] is DateTime c ? c : default
        };
    }
}
