using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using App.BL.AIAgent.GenericAgent.Memory;
using APP.Components.Dto;
using APP.Framework;
using Newtonsoft.Json;

namespace App.BL.AIAgent.GenericAgent.VectorStore
{
    public static class EntityVectorTrainingBL
    {
        public const int EmbedBatchSize = 100;

        public static string Fingerprint(IEnumerable<LookupItemDto> items)
        {
            var parts = (items ?? Enumerable.Empty<LookupItemDto>())
                .Select(i => (Convert.ToString(i?.Id, CultureInfo.InvariantCulture) ?? "") + "\t" + (i?.Display ?? "").Replace("\r\n", "\n").Replace('\r', '\n'))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", parts));
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        public static List<int> ListReferencedEntityIds(int dataSourceId)
        {
            var ids = new HashSet<int>();
            try
            {
                var fixture = AppVectorDb.Fixture(dataSourceId);
                if (fixture == null) return ids.ToList();
                AddIds(ids, fixture.RetriveDataTable(
                    "SELECT DISTINCT EntityId FROM dbo.AppTransactionField WHERE EntityId IS NOT NULL AND EntityId > 0",
                    AppVectorDb.Params(fixture)));
                AddIds(ids, fixture.RetriveDataTable(
                    "SELECT DISTINCT EntityId FROM dbo.AppSearchField WHERE EntityId IS NOT NULL AND EntityId > 0",
                    AppVectorDb.Params(fixture)));
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, nameof(ListReferencedEntityIds));
            }
            if (ids.Count == 0)
            {
                try
                {
                    var all = AppEntityInfoBL.RetrieveAllAppEntityInfoDto();
                    if (all != null)
                    {
                        foreach (var e in all)
                        {
                            if (e?.Id != null && int.TryParse(Convert.ToString(e.Id, CultureInfo.InvariantCulture), out var id) && id > 0)
                        ids.Add(id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, "fallback all EntityInfo for vector train");
                }
            }
            return ids.OrderBy(i => i).ToList();
        }

        /// <summary>
        /// Agent-path lazy train: only the entity being resolved. Do not call
        /// <see cref="EnsureAllReferencedEntities"/> from a request (embedding token cost).
        /// </summary>
        public static void EnsureOneEntity(AppClientIdentity identity, int dataSourceId, int entityId, List<LookupItemDto> items, CancellationToken ct = default)
        {
            if (entityId <= 0) return;
            var model = ResolveModel(identity);
            if (model == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("Entity vector train skipped — no embedding provider");
                return;
            }
            items ??= new List<LookupItemDto>();
            var name = AppVectorStoreBL.EntityCollectionName(entityId);
            var fp = Fingerprint(items);
            var existing = AppVectorStoreBL.GetCollection(dataSourceId, name, model);
            if (existing != null && string.Equals(existing.ContentFingerprint, fp, StringComparison.OrdinalIgnoreCase))
                return;
            TrainEntity(identity, dataSourceId, entityId, items, fp, model, ct);
        }

        /// <summary>
        /// Full-tenant rebuild. Do not call from Agent request paths — high embedding token cost.
        /// Reserved for a future admin/background job.
        /// </summary>
        public static void EnsureAllReferencedEntities(AppClientIdentity identity, int dataSourceId, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            var model = ResolveModel(identity);
            if (model == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("Entity vector train skipped — no embedding provider");
                return;
            }
            var entityIds = ListReferencedEntityIds(dataSourceId);
            var existing = AppVectorStoreBL.ListEntityCollections(dataSourceId, model)
                .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var trained = 0;
            foreach (var entityId in entityIds)
            {
                ct.ThrowIfCancellationRequested();
                var name = AppVectorStoreBL.EntityCollectionName(entityId);
                List<LookupItemDto> items;
                try { items = AppEntityInfoBL.GetLookupItemList(entityId, "") ?? new List<LookupItemDto>(); }
                catch { items = new List<LookupItemDto>(); }
                var fp = Fingerprint(items);
                if (existing.TryGetValue(name, out var col)
                    && string.Equals(col.ContentFingerprint, fp, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (TrainEntity(identity, dataSourceId, entityId, items, fp, model, ct))
                    trained++;
            }
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[VectorStore] lazy-all-missing ds={0} entities={1} trained={2} ms={3} model={4}",
                dataSourceId, entityIds.Count, trained, sw.ElapsedMilliseconds, model.FullModel);
        }

        public static bool TrainEntity(
            AppClientIdentity identity,
            int dataSourceId,
            int entityId,
            List<LookupItemDto> items,
            string fingerprint,
            VectorModelKey model,
            CancellationToken ct)
        {
            items ??= new List<LookupItemDto>();
            var docs = new List<(string Document, string Metadata)>();
            foreach (var item in items)
            {
                if (item?.Id == null) continue;
                var display = (item.Display ?? "").Trim();
                if (display.Length == 0)
                    display = Convert.ToString(item.Id, CultureInfo.InvariantCulture) ?? "";
                if (display.Length == 0) continue;
                var idStr = Convert.ToString(item.Id, CultureInfo.InvariantCulture) ?? "";
                docs.Add((display, JsonConvert.SerializeObject(new { Id = idStr })));
            }

            var name = AppVectorStoreBL.EntityCollectionName(entityId);
            if (docs.Count == 0)
            {
                AppVectorStoreBL.ReplaceCollection(dataSourceId, name, model, 0, fingerprint, Array.Empty<(string, float[], string)>());
                return true;
            }

            var vectors = new List<(string Document, float[] Vector, string Metadata)>(docs.Count);
            for (var i = 0; i < docs.Count; i += EmbedBatchSize)
            {
                ct.ThrowIfCancellationRequested();
                var chunk = docs.Skip(i).Take(EmbedBatchSize).ToList();
                var embedded = EmbeddingClient.EmbedManyAsync(chunk.Select(d => d.Document).ToList(), identity, ct)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                if (embedded == null || embedded.Count != chunk.Count)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn("Embed batch failed for entity {0}", entityId);
                    return false;
                }
                for (var j = 0; j < chunk.Count; j++)
                    vectors.Add((chunk[j].Document, embedded[j].Vector, chunk[j].Metadata));
            }

            var dim = vectors[0].Vector?.Length ?? 0;
            return AppVectorStoreBL.ReplaceCollection(dataSourceId, name, model, dim, fingerprint, vectors) > 0;
        }

        public static VectorModelKey ResolveModel(AppClientIdentity identity)
        {
            var full = EmbeddingClient.ResolveModelId(identity);
            return string.IsNullOrWhiteSpace(full) ? null : VectorModelKey.FromFullModel(full);
        }

        private static void AddIds(HashSet<int> ids, System.Data.DataTable dt)
        {
            if (dt == null) return;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                if (row[0] == DBNull.Value) continue;
                if (int.TryParse(Convert.ToString(row[0], CultureInfo.InvariantCulture), out var id) && id > 0)
                    ids.Add(id);
            }
        }
    }
}
