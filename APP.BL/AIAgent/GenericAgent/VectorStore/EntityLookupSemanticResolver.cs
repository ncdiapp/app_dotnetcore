using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using App.BL.AIAgent.GenericAgent.Memory;
using APP.Components.Dto;
using APP.Framework;

namespace App.BL.AIAgent.GenericAgent.VectorStore
{
    /// <summary>
    /// Exact lookup first (caller), then vector Top-1 with per-model similarity threshold.
    /// Lazy train covers only the entity being resolved.
    /// </summary>
    public static class EntityLookupSemanticResolver
    {
        public static object ResolveExact(List<LookupItemDto> items, string text)
        {
            if (items == null || items.Count == 0 || string.IsNullOrWhiteSpace(text)) return null;
            var n = text.Trim();
            var ck = Compact(n);
            object One(IEnumerable<LookupItemDto> hits)
            {
                var list = hits.Where(i => i?.Id != null).ToList();
                if (list.Count == 1) return list[0].Id;
                var exact = list.Where(i => string.Equals((i.Display ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)).ToList();
                return exact.Count == 1 ? exact[0].Id : null;
            }
            var byId = One(items.Where(i => string.Equals(Convert.ToString(i?.Id, CultureInfo.InvariantCulture), n, StringComparison.OrdinalIgnoreCase)));
            if (byId != null) return byId;
            var byDisplay = One(items.Where(i => string.Equals((i?.Display ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)
                || Compact(i?.Display) == ck));
            if (byDisplay != null) return byDisplay;
            var byFirst = One(items.Where(i => string.Equals((i?.FirstField ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)));
            if (byFirst != null) return byFirst;
            var byLast = One(items.Where(i => string.Equals((i?.LastField ?? "").Trim(), n, StringComparison.OrdinalIgnoreCase)));
            if (byLast != null) return byLast;
            return One(items.Where(i =>
            {
                var parts = (i?.Display ?? "").Split('|');
                return parts.Any(p => string.Equals(p.Trim(), n, StringComparison.OrdinalIgnoreCase) || Compact(p) == ck);
            }));
        }

        public static object Resolve(int entityInfoId, string text, List<LookupItemDto> items, AppClientIdentity identity)
        {
            var exact = ResolveExact(items, text);
            if (exact != null) return exact;
            if (entityInfoId <= 0 || string.IsNullOrWhiteSpace(text)) return null;

            var model = EntityVectorTrainingBL.ResolveModel(identity);
            if (model == null) return null;

            var ds = identity.DataSourceId;
            EntityVectorTrainingBL.EnsureOneEntity(identity, ds, entityInfoId, items);

            var query = EmbeddingClient.EmbedAsync(text, identity).ConfigureAwait(false).GetAwaiter().GetResult();
            if (query == null || query.Vector == null || query.Vector.Length == 0) return null;
            if (!string.Equals(query.Model, model.FullModel, StringComparison.OrdinalIgnoreCase))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(
                    "Embedding model mismatch train={0} query={1}", model.FullModel, query.Model);
                return null;
            }

            var hit = AppVectorStoreBL.SearchTop1(ds, AppVectorStoreBL.EntityCollectionName(entityInfoId), model, query.Vector);
            if (hit == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Info(
                    "[VectorStore] no hit entity={0} text={1}", entityInfoId, Trunc(text));
                return null;
            }

            var threshold = EmbeddingClient.RecallThreshold(model.FullModel);
            if (hit.Score < threshold)
            {
                NLog.LogManager.GetCurrentClassLogger().Info(
                    "[VectorStore] reject entity={0} score={1:0.000} threshold={2:0.00} doc={3} text={4}",
                    entityInfoId, hit.Score, threshold, Trunc(hit.Document), Trunc(text));
                return null;
            }

            NLog.LogManager.GetCurrentClassLogger().Info(
                "[VectorStore] hit entity={0} score={1:0.000} id={2} doc={3} text={4}",
                entityInfoId, hit.Score, hit.IdFromMetadata, Trunc(hit.Document), Trunc(text));

            if (string.IsNullOrWhiteSpace(hit.IdFromMetadata)) return null;
            var fromList = items?.FirstOrDefault(i =>
                string.Equals(Convert.ToString(i?.Id, CultureInfo.InvariantCulture), hit.IdFromMetadata, StringComparison.OrdinalIgnoreCase));
            return fromList?.Id ?? (object)hit.IdFromMetadata;
        }

        static string Compact(object value) =>
            System.Text.RegularExpressions.Regex.Replace(
                (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Trim().ToLowerInvariant(),
                "[^a-z0-9]", "");

        static string Trunc(string s)
        {
            s ??= "";
            return s.Length <= 80 ? s : s.Substring(0, 80);
        }
    }
}
