using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.BL.AIAgent.GenericAgent
{
    /// <summary>
    /// Caches get_database_schema dumps by DataSourceRegisterId (process TTL)
    /// and tracks which data sources were loaded in a ChatSession (for prompt inject).
    /// </summary>
    public static class AgentSchemaCacheBL
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

        private sealed class Entry
        {
            public string Text { get; init; }
            public DateTime ExpiresUtc { get; init; }
        }

        private static readonly ConcurrentDictionary<int, Entry> ByDataSource
            = new();

        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> SessionLoaded
            = new(StringComparer.OrdinalIgnoreCase);

        public static async Task<string> GetOrLoadAsync(int dataSourceId, Func<Task<string>> loader)
        {
            if (dataSourceId <= 0)
                return await loader().ConfigureAwait(false);

            if (ByDataSource.TryGetValue(dataSourceId, out var hit)
                && hit != null
                && hit.ExpiresUtc > DateTime.UtcNow
                && !string.IsNullOrEmpty(hit.Text))
            {
                return hit.Text;
            }

            var text = await loader().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text) && !LooksLikeError(text))
            {
                ByDataSource[dataSourceId] = new Entry
                {
                    Text = text,
                    ExpiresUtc = DateTime.UtcNow.Add(Ttl)
                };
            }
            return text;
        }

        public static void MarkSessionLoaded(string chatSessionKey, int dataSourceId)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey) || dataSourceId <= 0) return;
            var bag = SessionLoaded.GetOrAdd(
                chatSessionKey.Trim(),
                _ => new ConcurrentDictionary<int, byte>());
            bag[dataSourceId] = 1;
        }

        public static IReadOnlyList<int> GetSessionLoadedDataSourceIds(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return Array.Empty<int>();
            if (!SessionLoaded.TryGetValue(chatSessionKey.Trim(), out var bag) || bag.Count == 0)
                return Array.Empty<int>();
            return bag.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>Inject into Instructions when this chat already loaded schema for one or more DSs.</summary>
        public static string BuildSessionPromptHint(string chatSessionKey)
        {
            var ids = GetSessionLoadedDataSourceIds(chatSessionKey);
            if (ids.Count == 0) return null;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("## Database schema already loaded (this chat)");
            sb.Append("Schema for DataSourceId=");
            sb.Append(string.Join(", ", ids));
            sb.AppendLine(" was retrieved earlier in this chat session and is cached.");
            sb.AppendLine("Do NOT call get_database_schema again for these data sources unless the user switches to a different database or explicitly asks to refresh schema.");
            sb.AppendLine("Reuse table/column names from prior tool results and your analysis so far.");
            return sb.ToString();
        }

        public static void InvalidateDataSource(int dataSourceId)
        {
            if (dataSourceId <= 0) return;
            ByDataSource.TryRemove(dataSourceId, out _);
        }

        public static void ClearSession(string chatSessionKey)
        {
            if (string.IsNullOrWhiteSpace(chatSessionKey)) return;
            SessionLoaded.TryRemove(chatSessionKey.Trim(), out _);
        }

        private static bool LooksLikeError(string text)
        {
            var t = text.TrimStart();
            return t.StartsWith("{\"Error\"", StringComparison.OrdinalIgnoreCase)
                   || t.StartsWith("{\"error\"", StringComparison.OrdinalIgnoreCase);
        }
    }
}
